using System.Buffers.Binary;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml;

namespace VmScriptCompiler.Core;

public sealed record TranslationRow(string Id, string Kind, string Original, string Translated);
public sealed record TranslationResult(string Language, string Solution, string Table, IReadOnlyList<TranslationRow> Rows);

public static class SolutionTranslationWriter
{
    public static TranslationResult Write(string source, string mappingFile, string output)
    {
        source = Path.GetFullPath(source);
        output = Path.GetFullPath(output);
        if (string.Equals(source, output, StringComparison.OrdinalIgnoreCase)) throw new CompilerException("TRANSLATION_OUTPUT_INVALID", "翻译结果必须写入新 SOL。 ");
        using var mapping = JsonDocument.Parse(File.ReadAllText(mappingFile));
        var root = mapping.RootElement;
        var language = root.GetProperty("language").GetString();
        if (string.IsNullOrWhiteSpace(language)) throw new CompilerException("TRANSLATION_LANGUAGE_MISSING", "缺少目标语言。");
        var translations = root.GetProperty("translations");
        var inventory = SolutionTranslationInventory.Read(source);
        if (inventory.OpaqueEntries.Count > 0) throw new CompilerException("TRANSLATION_OPAQUE_UI", "有未识别的界面数据，无法保证完整翻译: " + string.Join(", ", inventory.OpaqueEntries));
        var rows = new List<TranslationRow>();
        foreach (var item in inventory.Items)
        {
            var mapped = translations.TryGetProperty(item.Id, out var value) && value.ValueKind == JsonValueKind.String
                ? value.GetString() : null;
            if (string.IsNullOrWhiteSpace(mapped) && item.Kind == "frontend" && !Regex.IsMatch(item.Source, "[\u4e00-\u9fff]"))
                mapped = item.Source;
            if (string.IsNullOrWhiteSpace(mapped)) throw new CompilerException("TRANSLATION_MISSING", "缺少译文: " + item.Id);
            var translated = mapped;
            var placeholders = Regex.Matches(item.Source, @"\{\d+\}").Select(x => x.Value).ToArray();
            if (!placeholders.SequenceEqual(Regex.Matches(translated, @"\{\d+\}").Select(x => x.Value)))
                throw new CompilerException("TRANSLATION_PLACEHOLDER_CHANGED", "译文改变了格式占位符: " + item.Id);
            var variables = Regex.Matches(item.Source, @"%[^%\r\n]+%").Select(x => x.Value).ToArray();
            if (!variables.SequenceEqual(Regex.Matches(translated, @"%[^%\r\n]+%").Select(x => x.Value)))
                throw new CompilerException("TRANSLATION_VARIABLE_CHANGED", "译文改变了变量标记: " + item.Id);
            var frontendFormats = Regex.Matches(item.Source, @"<%[^>]+>").Select(x => x.Value).ToArray();
            if (!frontendFormats.SequenceEqual(Regex.Matches(translated, @"<%[^>]+>").Select(x => x.Value)))
                throw new CompilerException("TRANSLATION_FORMAT_CHANGED", "译文改变了前端显示格式: " + item.Id);
            rows.Add(new(item.Id, item.Kind, item.Source, translated));
        }
        var byId = rows.ToDictionary(x => x.Id, x => x.Translated, StringComparer.Ordinal);
        var terms = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var row in rows.Where(x => x.Kind is "module" or "procedure"))
            if (row.Original != row.Translated) terms[row.Original] = row.Translated;
        foreach (var property in translations.EnumerateObject().Where(x => x.Name.StartsWith("term:", StringComparison.Ordinal)))
        {
            var sourceText = property.Name["term:".Length..];
            var translated = property.Value.GetString();
            if (string.IsNullOrWhiteSpace(sourceText) || string.IsNullOrWhiteSpace(translated))
                throw new CompilerException("TRANSLATION_TERM_INVALID", "术语映射不能为空。");
            if (terms.TryGetValue(sourceText, out var existing) && existing != translated)
                throw new CompilerException("TRANSLATION_TERM_CONFLICT", "术语译文冲突: " + sourceText);
            terms[sourceText] = translated;
        }
        Directory.CreateDirectory(Path.GetDirectoryName(output)!);
        File.Copy(source, output, false);
        try
        {
            using (var archive = SolArchiveAccess.OpenForUpdate(output))
            {
                foreach (var entry in archive.Entries.ToArray())
                {
                    var path = entry.FullName.Replace('\\', '/');
                    byte[]? replacement = path == "SolutionFile/VmServer.xml" ? RewriteServer(entry, byId, terms)
                        : path.Contains("/UiParamData/", StringComparison.Ordinal) ? RewriteUiParameters(entry, path, byId, terms)
                        : RewriteXmlUi(entry, path, byId);
                    if (replacement is null) continue;
                    var name = entry.FullName;
                    entry.Delete();
                    using var stream = archive.CreateEntry(name, CompressionLevel.Optimal).Open();
                    stream.Write(replacement);
                }
            }
            ModuleFrameCompatibility.RewriteTextParameters(output, terms);
            VmSolArchiveWriter.Repack(source, output);
            var actual = SolutionTranslationInventory.Read(output).Items.ToDictionary(x => x.Id, x => x.Source, StringComparer.Ordinal);
            foreach (var row in rows.Where(x => x.Kind != "term"))
                if (!actual.TryGetValue(row.Id, out var value) || value != row.Translated)
                    throw new CompilerException("TRANSLATION_VERIFY_FAILED", "写回后译文不一致: " + row.Id);
            var table = Path.ChangeExtension(output, ".translation.csv");
            using (var writer = new StreamWriter(table, false, new UTF8Encoding(true)))
            {
                writer.WriteLine("Id,Kind,Original,Translated");
                foreach (var row in rows) writer.WriteLine(string.Join(',', new[] { row.Id, row.Kind, row.Original, row.Translated }.Select(Csv)));
            }
            return new(language, output, table, rows);
        }
        catch { File.Delete(output); throw; }
    }

    private static string Csv(string value) => "\"" + value.Replace("\"", "\"\"") + "\"";
    private static byte[] RewriteServer(ZipArchiveEntry entry, Dictionary<string, string> values, IReadOnlyDictionary<string, string> terms)
    {
        string originalXml;
        using (var input = new StreamReader(entry.Open(), new UTF8Encoding(false, true), detectEncodingFromByteOrderMarks: true))
            originalXml = input.ReadToEnd();
        var document = new XmlDocument { PreserveWhitespace = true };
        document.LoadXml(originalXml);
        foreach (XmlElement element in document.SelectNodes("/Root/ProceduresInfo/ProcedureBase/Procedure")!)
            if (values.TryGetValue("procedure:" + element.GetAttribute("Index"), out var translated)) element.SetAttribute("DisplayName", translated);
        foreach (XmlElement element in document.SelectNodes("/Root/ModulesInfo/ModuleBase/Module")!)
            if (values.TryGetValue("module:" + element.GetAttribute("Index"), out var translated)) element.SetAttribute("DisplayName", translated);
        var procedures = document.SelectNodes("/Root/ProceduresInfo/ProcedureBase/Procedure")!.OfType<XmlElement>().ToArray();
        if (procedures.Select(x => x.GetAttribute("DisplayName")).Distinct(StringComparer.OrdinalIgnoreCase).Count() != procedures.Length)
            throw new CompilerException("TRANSLATION_NAME_COLLISION", "翻译后流程名称重复。");
        var modules = document.SelectNodes("/Root/ModulesInfo/ModuleBase/Module")!.OfType<XmlElement>().ToDictionary(x => x.GetAttribute("Index"));
        foreach (XmlElement inside in document.SelectNodes("/Root/ProceduresInfo/ProcedureInsideModules/Procedure")!)
        {
            var names = inside.ChildNodes.OfType<XmlElement>().Select(x => modules.GetValueOrDefault(x.GetAttribute("Index"))?.GetAttribute("DisplayName")).Where(x => x is not null).ToArray();
            if (names.Distinct(StringComparer.OrdinalIgnoreCase).Count() != names.Length)
                throw new CompilerException("TRANSLATION_NAME_COLLISION", "翻译后同一流程内的模块名称重复。");
        }
        var rewritten = ModuleFrameCompatibility.ReplaceTerms(document.OuterXml, terms);
        document.LoadXml(rewritten);
        // Preserve VM's original declaration and whitespace; the native parser is
        // sensitive to a full XmlDocument reformat of VmServer.xml.
        var originalDeclaration = Regex.Match(originalXml, @"^<\?xml[^>]*\?>").Value;
        var result = document.OuterXml;
        var rewrittenDeclaration = Regex.Match(result, @"^<\?xml[^>]*\?>").Value;
        if (originalDeclaration.Length > 0 && rewrittenDeclaration.Length > 0)
            result = originalDeclaration + result[rewrittenDeclaration.Length..];
        return new UTF8Encoding(false).GetBytes(result);
    }

    private static byte[]? RewriteUiParameters(ZipArchiveEntry entry, string path, Dictionary<string, string> values, IReadOnlyDictionary<string, string> terms)
    {
        using var input = entry.Open();
        using var memory = new MemoryStream();
        input.CopyTo(memory);
        var data = memory.ToArray();
        if (data.Length < 12 || BinaryPrimitives.ReadUInt32BigEndian(data) != 0x66553322) return null;
        var count = BinaryPrimitives.ReadInt32BigEndian(data.AsSpan(8));
        var offset = 12;
        var pairs = new List<(byte[] Key, byte[] Value)>();
        var changed = false;
        for (var index = 0; index < count; index++)
        {
            var keyLength = BinaryPrimitives.ReadInt32BigEndian(data.AsSpan(offset)); offset += 4;
            var key = data.AsSpan(offset, keyLength).ToArray(); offset += keyLength;
            var originalKey = Encoding.UTF8.GetString(key);
            var updatedKey = ModuleFrameCompatibility.ReplaceTerms(originalKey, terms);
            if (updatedKey != originalKey) { key = Encoding.UTF8.GetBytes(updatedKey); changed = true; }
            var valueLength = BinaryPrimitives.ReadInt32BigEndian(data.AsSpan(offset)); offset += 4;
            var value = data.AsSpan(offset, valueLength).ToArray(); offset += valueLength;
            if (originalKey == "DESIGNERBUFFER")
            {
                value = FrontendDocumentTranslation.Rewrite(path, value, values, terms);
                changed = true;
            }
            else if (values.TryGetValue($"ui-param:{path}:{index}", out var translated))
            {
                value = Encoding.UTF8.GetBytes(translated + (value.Length > 0 && value[^1] == 0 ? "\0" : ""));
                changed = true;
            }
            else
            {
                var original = Encoding.UTF8.GetString(value);
                var updated = ModuleFrameCompatibility.ReplaceTerms(original, terms);
                if (updated != original) { value = Encoding.UTF8.GetBytes(updated); changed = true; }
            }
            pairs.Add((key, value));
        }
        if (!changed) return null;
        using var result = new MemoryStream();
        result.Write(data, 0, 12);
        foreach (var pair in pairs)
        {
            WriteInt(result, pair.Key.Length); result.Write(pair.Key);
            WriteInt(result, pair.Value.Length); result.Write(pair.Value);
        }
        return result.ToArray();
    }

    private static void WriteInt(Stream stream, int value)
    {
        Span<byte> bytes = stackalloc byte[4];
        BinaryPrimitives.WriteInt32BigEndian(bytes, value);
        stream.Write(bytes);
    }

    private static byte[]? RewriteXmlUi(ZipArchiveEntry entry, string path, Dictionary<string, string> values)
    {
        if (!values.Keys.Any(x => x.StartsWith("ui:" + path + ":", StringComparison.Ordinal) || x.StartsWith("ui-text:" + path + ":", StringComparison.Ordinal))) return null;
        var document = new XmlDocument();
        using (var input = entry.Open()) document.Load(input);
        var index = 0;
        foreach (XmlAttribute attribute in document.SelectNodes("//@*")!)
            if (attribute.Name.Equals("Text", StringComparison.OrdinalIgnoreCase) || attribute.Name.Equals("Label", StringComparison.OrdinalIgnoreCase) || attribute.Name.Equals("Caption", StringComparison.OrdinalIgnoreCase) || attribute.Name.Equals("ButtonText", StringComparison.OrdinalIgnoreCase) || attribute.Name.Equals("Content", StringComparison.OrdinalIgnoreCase) || attribute.Name.Equals("Title", StringComparison.OrdinalIgnoreCase))
                if (values.TryGetValue($"ui:{path}:{index++}", out var translated)) attribute.Value = translated;
        var textIndex = 0;
        foreach (XmlElement element in document.SelectNodes("//*[local-name()='Text' or local-name()='Label' or local-name()='Caption' or local-name()='ButtonText' or local-name()='Content' or local-name()='Title']")!)
            if (!element.ChildNodes.OfType<XmlElement>().Any() && values.TryGetValue($"ui-text:{path}:{textIndex++}", out var translated)) element.InnerText = translated;
        return new UTF8Encoding(false).GetBytes(document.OuterXml);
    }
}
