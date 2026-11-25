using System.IO.Compression;
using System.Buffers.Binary;
using System.Text;
using System.Xml;

namespace VmScriptCompiler.Core;

public sealed record TranslationItem(string Id, string Kind, string Source, string Location);
public sealed record TranslationInventory(IReadOnlyList<TranslationItem> Items, IReadOnlyList<string> OpaqueEntries);

/// <summary>Finds user-visible text in a SOL without treating VM identifiers as translatable text.</summary>
public static class SolutionTranslationInventory
{
    public static TranslationInventory Read(string solutionFile)
    {
        SolArchiveValidator.ValidateVm44EntryNames(solutionFile);
        using var archive = ZipFile.OpenRead(solutionFile);
        var items = new List<TranslationItem>();
        var opaque = new List<string>();
        foreach (var entry in archive.Entries)
        {
            var path = entry.FullName.Replace('\\', '/');
            if (path == "SolutionFile/MoudleFrame")
            {
                using var input = entry.Open();
                using var frameBuffer = new MemoryStream();
                input.CopyTo(frameBuffer);
                foreach (var term in ModuleFrameCompatibility.ReadTranslatableTerms(frameBuffer.ToArray()))
                    Add(items, "term", path, term, term);
                continue;
            }
            if (path == "SolutionFile/VmServer.xml")
            {
                var document = new XmlDocument();
                using var stream = entry.Open();
                document.Load(stream);
                foreach (XmlElement procedure in document.SelectNodes("/Root/ProceduresInfo/ProcedureBase/Procedure")!)
                    Add(items, "procedure", path, procedure.GetAttribute("Index"), procedure.GetAttribute("DisplayName"));
                foreach (XmlElement module in document.SelectNodes("/Root/ModulesInfo/ModuleBase/Module")!)
                    Add(items, "module", path, module.GetAttribute("Index"), module.GetAttribute("DisplayName"));
                continue;
            }
            if (path.Contains("/UiParamData/", StringComparison.Ordinal))
            {
                if (path.Contains("GlobalScript", StringComparison.OrdinalIgnoreCase)) continue;
                using var stream = entry.Open();
                using var memory = new MemoryStream();
                stream.CopyTo(memory);
                if (!ReadUiParameters(path, memory.ToArray(), items)) opaque.Add(path);
                continue;
            }
            if (!path.StartsWith("SolutionFile/", StringComparison.Ordinal))
            {
                continue;
            }
            if (entry.Length == 0 || entry.Length > 8_000_000) continue;
            using var data = entry.Open();
            using var buffer = new MemoryStream();
            data.CopyTo(buffer);
            var bytes = buffer.ToArray();
            var encoding = bytes.Length >= 2 && bytes[0] == 0xff && bytes[1] == 0xfe ? Encoding.Unicode : Encoding.UTF8;
            var text = encoding.GetString(bytes);
            if (!text.TrimStart('\uFEFF', ' ', '\r', '\n', '\t').StartsWith('<'))
            {
                if (IsUiPath(path)) opaque.Add(path);
                continue;
            }
            var documentText = new XmlDocument();
            try { documentText.LoadXml(text); } catch (XmlException) { if (IsUiPath(path)) opaque.Add(path); continue; }
            var attributes = documentText.SelectNodes("//@*");
            if (attributes is null) continue;
            var index = 0;
            foreach (XmlAttribute attribute in attributes)
            {
                if (!IsUiTextName(attribute.Name)) continue;
                Add(items, "ui", path, $"{path}:{index++}", attribute.Value);
            }
            var textIndex = 0;
            foreach (XmlElement element in documentText.SelectNodes("//*[local-name()='Text' or local-name()='Label' or local-name()='Caption' or local-name()='ButtonText' or local-name()='Content' or local-name()='Title']")!)
                if (!element.ChildNodes.OfType<XmlElement>().Any()) Add(items, "ui-text", path, $"{path}:{textIndex++}", element.InnerText);
        }
        return new(items, opaque);
    }

    private static bool IsUiTextName(string name) => name.Equals("Text", StringComparison.OrdinalIgnoreCase)
        || name.Equals("Label", StringComparison.OrdinalIgnoreCase)
        || name.Equals("Caption", StringComparison.OrdinalIgnoreCase)
        || name.Equals("ButtonText", StringComparison.OrdinalIgnoreCase)
        || name.Equals("Content", StringComparison.OrdinalIgnoreCase)
        || name.Equals("Title", StringComparison.OrdinalIgnoreCase);

    private static bool ReadUiParameters(string path, byte[] data, List<TranslationItem> items)
    {
        if (data.Length < 12 || BinaryPrimitives.ReadUInt32BigEndian(data) != 0x66553322 || BinaryPrimitives.ReadInt32BigEndian(data.AsSpan(4)) != 1) return false;
        var count = BinaryPrimitives.ReadInt32BigEndian(data.AsSpan(8));
        if (count < 0 || count > 100_000) return false;
        var offset = 12;
        for (var index = 0; index < count; index++)
        {
            if (offset + 4 > data.Length) return false;
            var keyLength = BinaryPrimitives.ReadInt32BigEndian(data.AsSpan(offset)); offset += 4;
            if (keyLength < 0 || offset + keyLength + 4 > data.Length) return false;
            var key = Encoding.UTF8.GetString(data, offset, keyLength); offset += keyLength;
            var valueLength = BinaryPrimitives.ReadInt32BigEndian(data.AsSpan(offset)); offset += 4;
            if (valueLength < 0 || offset + valueLength > data.Length) return false;
            if (key == "DESIGNERBUFFER" &&
                !FrontendDocumentTranslation.Read(path, data.AsSpan(offset, valueLength).ToArray(), items)) return false;
            if (IsUiTextKey(key) && valueLength > 0)
            {
                var value = Encoding.UTF8.GetString(data, offset, valueLength).TrimEnd('\0');
                Add(items, "ui-param", path, $"{path}:{index}", value);
            }
            offset += valueLength;
        }
        return offset == data.Length;
    }

    private static bool IsUiTextKey(string key) => key.EndsWith(".Content.Value", StringComparison.OrdinalIgnoreCase)
        || key.EndsWith(".Text", StringComparison.OrdinalIgnoreCase)
        || key.EndsWith(".Label", StringComparison.OrdinalIgnoreCase)
        || key.EndsWith(".Caption", StringComparison.OrdinalIgnoreCase)
        || key.EndsWith(".ButtonText", StringComparison.OrdinalIgnoreCase)
        || key.EndsWith(".Title", StringComparison.OrdinalIgnoreCase)
        || key.EndsWith(".Value", StringComparison.OrdinalIgnoreCase)
            && (key.Contains("Button", StringComparison.OrdinalIgnoreCase)
                || key.Contains("Label", StringComparison.OrdinalIgnoreCase)
                || key.Contains("Caption", StringComparison.OrdinalIgnoreCase)
                || key.Contains("Title", StringComparison.OrdinalIgnoreCase)
                || key.Contains("Text", StringComparison.OrdinalIgnoreCase));

    private static bool IsUiPath(string path) => path.Contains("Interface", StringComparison.OrdinalIgnoreCase)
        || path.Contains("Screen", StringComparison.OrdinalIgnoreCase)
        || path.Contains("Layout", StringComparison.OrdinalIgnoreCase)
        || path.Contains("View", StringComparison.OrdinalIgnoreCase)
        || path.Contains("HMI", StringComparison.OrdinalIgnoreCase)
        || path.Contains("Canvas", StringComparison.OrdinalIgnoreCase)
        || path.Contains("Widget", StringComparison.OrdinalIgnoreCase);

    private static void Add(List<TranslationItem> items, string kind, string path, string key, string value)
    {
        if (!string.IsNullOrWhiteSpace(value)) items.Add(new($"{kind}:{key}", kind, value, path));
    }
}
