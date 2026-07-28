using System.Globalization;
using System.IO.Compression;
using System.Buffers.Binary;
using System.Text;
using System.Text.Json;
using System.Xml;

namespace VmScriptCompiler.Core;

public static class VmServerDefaultWriter
{
    private const string VmServerEntry = "SolutionFile/VmServer.xml";

    public static void Apply(string solutionFile, IReadOnlyList<ScriptRequirement> scripts)
    {
        var modules = scripts.Where(x => x.Carrier is "csharp-module" or "python-module").ToArray();
        if (modules.Length == 0) return;

        using var archive = ZipFile.Open(solutionFile, ZipArchiveMode.Update);
        var entry = archive.Entries.FirstOrDefault(x => Normalize(x.FullName) == VmServerEntry)
            ?? throw new CompilerException("VM_SERVER_ENTRY_MISSING", "SOL 中缺少 SolutionFile/VmServer.xml。");
        var entryName = entry.FullName;
        var document = new XmlDocument { PreserveWhitespace = true };
        using (var input = entry.Open()) document.Load(input);

        var modulesInfo = document.SelectSingleNode("/Root/ModulesInfo")
            ?? throw new CompilerException("VM_SERVER_XML_INVALID", "VmServer.xml 缺少 ModulesInfo。");
        var subscriptions = modulesInfo.SelectSingleNode("ModuleSubscribe");
        if (subscriptions is null)
        {
            subscriptions = document.CreateElement("ModuleSubscribe");
            modulesInfo.AppendChild(subscriptions);
        }

        foreach (var script in modules)
        {
            var moduleIndex = FindModuleIndex(document, script.Procedure!, script.Name);
            if (script.Carrier == "csharp-module") WriteCSharpUiParameters(archive, moduleIndex, script);
            // Replacing an existing script must not retain stale literal defaults for removed ports
            // or for ports whose new Requirement intentionally omits a default.
            foreach (var existing in subscriptions.ChildNodes.OfType<XmlElement>()
                .Where(x => IsPersistedDefault(x.GetAttribute("Relation"), moduleIndex)).ToArray())
                subscriptions.RemoveChild(existing);
            foreach (var port in script.Inputs.Where(HasDefault))
            {
                if (!TryFormatPersistedDefault(port, out var value)) continue;
                var paramName = "%" + port.Name + "%";
                foreach (var existing in subscriptions.ChildNodes.OfType<XmlElement>()
                    .Where(x => IsSameInput(x.GetAttribute("Relation"), moduleIndex, paramName)).ToArray())
                    subscriptions.RemoveChild(existing);
                var element = document.CreateElement("Subscribe");
                element.SetAttribute("Relation", $"{moduleIndex} . {paramName} . 0 . {value} . 1 . 0 . All . 1");
                subscriptions.AppendChild(element);
            }
        }

        entry.Delete();
        var replacement = archive.CreateEntry(entryName, CompressionLevel.Optimal);
        using var output = replacement.Open();
        using var writer = XmlWriter.Create(output, new XmlWriterSettings
        {
            Encoding = new UTF8Encoding(false),
            Indent = true,
            IndentChars = "    ",
            NewLineChars = "\n",
            NewLineHandling = NewLineHandling.Replace
        });
        document.Save(writer);
    }

    private static void WriteCSharpUiParameters(ZipArchive archive, int moduleIndex, ScriptRequirement script)
    {
        var entry = archive.Entries.SingleOrDefault(x => Normalize(x.FullName).StartsWith($"SolutionFile/UiParamData/_{moduleIndex}+", StringComparison.Ordinal));
        if (entry is null) throw new CompilerException("UI_PARAM_ENTRY_MISSING", "SOL 中缺少脚本模块 UiParamData: " + script.Procedure + "." + script.Name);
        var entryName = entry.FullName;
        byte[] bytes;
        using (var input = entry.Open())
        using (var memory = new MemoryStream()) { input.CopyTo(memory); bytes = memory.ToArray(); }
        var values = ReadUiParameters(bytes);
        var preserved = values.Where(x => x.Key is "GUID" or "IsReady" or "Position").ToList();
        var layout = Vm44CSharpIoLayout.Create(script.Inputs, script.Outputs);

        AddString(preserved, "[ParamRoot]_%Data Record%Type", "datarecord");
        AddString(preserved, "[ParamRoot]_%Data Record%IsDisplay", "True");
        AddString(preserved, "DynamicObject_%Data Record%_[Child]", "Content");

        var recordFilters = layout.Outputs.SelectMany(DataRecordFilters).ToArray();
        AddString(preserved, "[ParamRoot]_%Data Record%.Content.Value",
            string.Join(',', recordFilters.Select((x, i) => x.Name.Trim('%') + ":{" + i.ToString(CultureInfo.InvariantCulture) + "}")));
        AddString(preserved, "[ParamRoot]_%Data Record%.Content.Mapping", string.Join(',', recordFilters.Select(x => x.Name)));

        var dynamicObjects = new List<string> { "%Data Record%" };
        foreach (var image in layout.Outputs.Where(x => x.Port.Type == "image"))
        {
            var objectName = "%" + image.Port.Name + "%";
            var filters = Vm44CSharpIoLayout.Flatten(image.Dynamic).ToArray();
            dynamicObjects.Add(objectName);
            AddString(preserved, "[ParamRoot]_" + objectName + "Type", "image");
            AddString(preserved, "[ParamRoot]_" + objectName + "IsDisplay", "True");
            AddString(preserved, "[ParamRoot]_" + objectName + "okcolor", "#66ff00");
            AddString(preserved, "[ParamRoot]_" + objectName + "ngcolor", "#ff0000");
            AddString(preserved, "[ParamRoot]_" + objectName + "opacity", "1");
            AddString(preserved, "[ParamRoot]_" + objectName + ".Image.Mapping", filters[0].Name);
            AddString(preserved, "[ParamRoot]_" + objectName + ".Width.Mapping", filters[1].Name);
            AddString(preserved, "[ParamRoot]_" + objectName + ".Height.Mapping", filters[2].Name);
            AddString(preserved, "[ParamRoot]_" + objectName + ".PixelFormat.Mapping", filters[3].Name);
            AddString(preserved, "DynamicObject_" + objectName + "_[Child]", "Image;Width;Height;PixelFormat");
            AddString(preserved, "RelateIO_" + objectName, image.LogicalName);
            AddString(preserved, "ObjectExternInfos_" + objectName, "RelateIO");
        }
        AddString(preserved, "DynamicObject", string.Join(';', dynamicObjects));

        AddArrayTypes(preserved, "InputValueTypes", layout.Inputs);
        AddArrayTypes(preserved, "OutputValueTypes", layout.Outputs);

        var replacementBytes = WriteUiParameters(preserved);
        entry.Delete();
        var replacement = archive.CreateEntry(entryName, CompressionLevel.Optimal);
        using var output = replacement.Open();
        output.Write(replacementBytes);
    }

    private static IEnumerable<VmIoFilter> DataRecordFilters(VmPortBinding binding)
    {
        var filters = Vm44CSharpIoLayout.Flatten(binding.Dynamic).ToArray();
        if (binding.Port.Type != "image") return filters;
        return filters.Where((_, index) => index is 1 or 2);
    }

    private static void AddArrayTypes(List<KeyValuePair<string, byte[]>> values, string key, IReadOnlyList<VmPortBinding> ports)
    {
        var mapping = string.Join(',', ports.Where(x => x.Port.Type.EndsWith("[]", StringComparison.Ordinal))
            .Select(x => x.LogicalName + ":" + x.ValueType));
        if (mapping.Length > 0) AddString(values, key, mapping);
    }

    private static void AddString(List<KeyValuePair<string, byte[]>> values, string key, string value) =>
        values.Add(new(key, Encoding.UTF8.GetBytes(value + "\0")));

    private static List<KeyValuePair<string, byte[]>> ReadUiParameters(byte[] bytes)
    {
        if (bytes.Length < 12 || BinaryPrimitives.ReadUInt32BigEndian(bytes.AsSpan(0, 4)) != 0x66553322 || BinaryPrimitives.ReadInt32BigEndian(bytes.AsSpan(4, 4)) != 1)
            throw new CompilerException("UI_PARAM_FORMAT_INVALID", "ShellModule UiParamData header is invalid.");
        var count = BinaryPrimitives.ReadInt32BigEndian(bytes.AsSpan(8, 4));
        var offset = 12;
        var values = new List<KeyValuePair<string, byte[]>>(count);
        for (var index = 0; index < count; index++)
        {
            if (offset + 4 > bytes.Length) throw new CompilerException("UI_PARAM_FORMAT_INVALID", "UiParamData key length is truncated.");
            var keyLength = BinaryPrimitives.ReadInt32BigEndian(bytes.AsSpan(offset, 4)); offset += 4;
            if (keyLength < 0 || offset + keyLength + 4 > bytes.Length) throw new CompilerException("UI_PARAM_FORMAT_INVALID", "UiParamData key is invalid.");
            var key = Encoding.UTF8.GetString(bytes, offset, keyLength); offset += keyLength;
            var valueLength = BinaryPrimitives.ReadInt32BigEndian(bytes.AsSpan(offset, 4)); offset += 4;
            if (valueLength < 0 || offset + valueLength > bytes.Length) throw new CompilerException("UI_PARAM_FORMAT_INVALID", "UiParamData value is invalid.");
            values.Add(new(key, bytes.AsSpan(offset, valueLength).ToArray())); offset += valueLength;
        }
        return values;
    }

    private static byte[] WriteUiParameters(IReadOnlyList<KeyValuePair<string, byte[]>> values)
    {
        var encodedKeys = values.Select(x => Encoding.UTF8.GetBytes(x.Key)).ToArray();
        var result = new byte[12 + values.Select((x, i) => 8 + encodedKeys[i].Length + x.Value.Length).Sum()];
        BinaryPrimitives.WriteUInt32BigEndian(result.AsSpan(0, 4), 0x66553322);
        BinaryPrimitives.WriteInt32BigEndian(result.AsSpan(4, 4), 1);
        BinaryPrimitives.WriteInt32BigEndian(result.AsSpan(8, 4), values.Count);
        var offset = 12;
        for (var index = 0; index < values.Count; index++)
        {
            BinaryPrimitives.WriteInt32BigEndian(result.AsSpan(offset, 4), encodedKeys[index].Length); offset += 4;
            encodedKeys[index].CopyTo(result, offset); offset += encodedKeys[index].Length;
            BinaryPrimitives.WriteInt32BigEndian(result.AsSpan(offset, 4), values[index].Value.Length); offset += 4;
            values[index].Value.CopyTo(result, offset); offset += values[index].Value.Length;
        }
        return result;
    }

    private static int FindModuleIndex(XmlDocument document, string procedureName, string moduleName)
    {
        var procedure = document.SelectNodes("/Root/ProceduresInfo/ProcedureBase/Procedure")!.OfType<XmlElement>()
            .SingleOrDefault(x => x.GetAttribute("DisplayName") == procedureName)
            ?? throw new CompilerException("PROCEDURE_NOT_FOUND", "VmServer.xml 中未找到流程: " + procedureName);
        var procedureIndex = procedure.GetAttribute("Index");
        var inside = document.SelectNodes("/Root/ProceduresInfo/ProcedureInsideModules/Procedure")!.OfType<XmlElement>()
            .SingleOrDefault(x => x.GetAttribute("Index") == procedureIndex)?.ChildNodes.OfType<XmlElement>()
            .Select(x => x.GetAttribute("Index")).ToHashSet(StringComparer.Ordinal) ?? [];
        var matches = document.SelectNodes("/Root/ModulesInfo/ModuleBase/Module")!.OfType<XmlElement>()
            .Where(x => x.GetAttribute("DisplayName") == moduleName && inside.Contains(x.GetAttribute("Index"))).ToArray();
        if (matches.Length != 1 || !int.TryParse(matches[0].GetAttribute("Index"), NumberStyles.Integer, CultureInfo.InvariantCulture, out var index))
            throw new CompilerException("MODULE_NOT_FOUND", "VmServer.xml 中无法唯一定位模块: " + procedureName + "." + moduleName);
        return index;
    }

    private static bool HasDefault(IoRequirement port) => port.DefaultValue.ValueKind is not JsonValueKind.Undefined and not JsonValueKind.Null;
    internal static bool TryFormatPersistedDefault(IoRequirement port, out string value)
    {
        value = "";
        if (!HasDefault(port)) return false;
        switch (port.Type)
        {
            case "int" when port.DefaultValue.TryGetInt64(out var integer):
                value = integer.ToString(CultureInfo.InvariantCulture);
                return true;
            case "float" when port.DefaultValue.TryGetDouble(out var floating):
                value = floating.ToString("R", CultureInfo.InvariantCulture);
                return true;
            case "bool":
                value = port.DefaultValue.GetBoolean() ? "1" : "0";
                return true;
            case "string" when !(port.DefaultValue.GetString() ?? "").Contains(" . ", StringComparison.Ordinal):
                value = port.DefaultValue.GetString() ?? "";
                return true;
            default:
                return false;
        }
    }

    private static bool IsSameInput(string relation, int moduleIndex, string paramName)
    {
        var parts = relation.Split(" . ", StringSplitOptions.None);
        return parts.Length >= 2 && parts[0] == moduleIndex.ToString(CultureInfo.InvariantCulture) && parts[1] == paramName;
    }

    private static bool IsPersistedDefault(string relation, int moduleIndex)
    {
        var parts = relation.Split(" . ", StringSplitOptions.None);
        return parts.Length == 8 && parts[0] == moduleIndex.ToString(CultureInfo.InvariantCulture)
            && parts[1].StartsWith('%') && parts[1].EndsWith('%') && parts[2] == "0"
            && parts[4] == "1" && parts[5] == "0" && parts[6] == "All" && parts[7] == "1";
    }

    private static string Normalize(string path) => path.Replace('\\', '/').TrimStart('/');
}
