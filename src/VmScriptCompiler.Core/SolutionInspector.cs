using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Xml;

namespace VmScriptCompiler.Core;

public sealed record InspectedPort(
    string Name,
    string? Type,
    bool? IsForce,
    bool? IsShow,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? PersistedDefault,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] InspectedBinding? Binding);

public sealed record InspectedBinding(int TargetModuleId, string TargetParameter, string IndexMode, string RelationString);

public sealed record InspectedScript(
    string Procedure,
    string Module,
    int ModuleId,
    string ModuleType,
    string? Carrier,
    bool IsEnabled,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? Version,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? Source,
    int SourceLength,
    IReadOnlyList<InspectedPort> Inputs,
    IReadOnlyList<InspectedPort> Outputs);

public sealed record InspectedModule(string FullPath, string Name, string DisplayName, int ModuleId, bool IsEnabled, int ParameterCount);

public sealed record InspectedGlobalScript(string? Version, int ScriptLength, bool HasPassword, IReadOnlyList<string> References);

public sealed record SolutionDigest(
    IReadOnlyList<string> Procedures,
    IReadOnlyList<InspectedModule> Modules,
    IReadOnlyList<InspectedScript> Scripts,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] InspectedGlobalScript? GlobalScript,
    IReadOnlyList<string> Warnings,
    bool SourceTruncated);

/// <summary>
/// Distils a structural <c>parse</c> result into the subset an agent needs before patching: the script
/// source already stored in the SOL, the declared ports, and — critically — the values a user saved in
/// VM. <c>inspect --list</c> reports only module paths and parameter counts, so an agent working from it
/// alone cannot see prior edits and will regenerate invented defaults over them.
/// </summary>
public static class SolutionInspector
{
    /// <summary>Per-script source budget. Long scripts are truncated so one inspect cannot flood the model context.</summary>
    public const int DefaultSourceLimit = 20000;

    private static readonly IReadOnlyDictionary<string, string> CarrierByModuleType = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["ShellModule"] = "csharp-module",
        ["PyShellModule"] = "python-module",
        ["AsShellModule"] = "as-module"
    };

    public static SolutionDigest FromParseResult(JsonElement root, int sourceLimit = DefaultSourceLimit)
    {
        if (!root.TryGetProperty("solution", out var solution))
            throw new CompilerException("SOL_PARSE_FAILED", "Parse result does not contain a solution section.");

        var procedures = new List<string>();
        var modules = new List<InspectedModule>();
        var scripts = new List<InspectedScript>();
        var truncated = false;

        foreach (var procedure in Array(solution, "procedures"))
        {
            var procedureName = String(procedure, "displayName") ?? "";
            procedures.Add(procedureName);
            foreach (var module in Array(procedure, "modules"))
            {
                var moduleType = String(module, "name") ?? "";
                var displayName = String(module, "displayName") ?? "";
                var moduleId = Int(module, "moduleId") ?? -1;
                var isEnabled = Bool(module, "isEnabled") ?? true;
                modules.Add(new InspectedModule(
                    String(module, "fullPath") ?? (procedureName + "." + displayName),
                    moduleType, displayName, moduleId, isEnabled,
                    Array(module, "algoriParams").Count() + Array(module, "binaryParams").Count()));

                if (!CarrierByModuleType.TryGetValue(moduleType, out var carrier)) continue;

                var subscriptions = ReadSubscriptions(module, moduleId);
                var source = String(module, "scriptText");
                var sourceLength = source?.Length ?? 0;
                if (source is not null && source.Length > sourceLimit)
                {
                    source = source[..sourceLimit];
                    truncated = true;
                }
                scripts.Add(new InspectedScript(
                    procedureName, displayName, moduleId, moduleType, carrier, isEnabled,
                    BinaryParameter(module, "Version"),
                    source, sourceLength,
                    ReadPorts(module, "Input", subscriptions),
                    ReadPorts(module, "Output", subscriptions)));
            }
        }

        InspectedGlobalScript? globalScript = null;
        if (solution.TryGetProperty("globalScript", out var global) && global.ValueKind == JsonValueKind.Object)
            globalScript = new InspectedGlobalScript(
                String(global, "version"), Int(global, "scriptLength") ?? 0, Bool(global, "hasPassword") ?? false,
                Array(global, "references").Select(x => x.ValueKind == JsonValueKind.String ? x.GetString()! : x.GetRawText()).ToArray());

        var warnings = Array(solution, "warnings")
            .Where(x => x.ValueKind == JsonValueKind.String)
            .Select(x => x.GetString()!)
            .ToArray();

        return new SolutionDigest(procedures, modules, scripts, globalScript, warnings, truncated);
    }

    /// <summary>
    /// Maps <c>%Port%</c> to the relation VM saved for it. A relation is either a literal default the
    /// user typed in VM (segment 5 is <c>1</c>, value in segment 4) or a binding to another module's
    /// output (segment 5 is <c>0</c>). Both must survive a patch, so both are surfaced.
    /// </summary>
    private static Dictionary<string, (string? Literal, InspectedBinding? Binding)> ReadSubscriptions(JsonElement module, int moduleId)
    {
        var result = new Dictionary<string, (string?, InspectedBinding?)>(StringComparer.Ordinal);
        foreach (var subscription in Array(module, "subscriptions"))
        {
            var parameter = String(subscription, "paramName");
            if (string.IsNullOrEmpty(parameter)) continue;
            var name = parameter.Trim('%');
            var relation = String(subscription, "relationString") ?? "";
            var parts = relation.Split(" . ", StringSplitOptions.None);
            var ownedByModule = parts.Length == 8 && parts[0] == moduleId.ToString(CultureInfo.InvariantCulture);

            if (ownedByModule && parts[4] == "1")
            {
                result[name] = (parts[3], null);
                continue;
            }
            if (Bool(subscription, "isBound") == true)
            {
                result[name] = (null, new InspectedBinding(
                    Int(subscription, "targetModuleId") ?? -1,
                    String(subscription, "targetParamName") ?? "",
                    String(subscription, "indexMode") ?? "",
                    relation));
            }
        }
        return result;
    }

    private static IReadOnlyList<InspectedPort> ReadPorts(
        JsonElement module, string parameterName, Dictionary<string, (string? Literal, InspectedBinding? Binding)> subscriptions)
    {
        var xml = BinaryParameter(module, parameterName);
        if (string.IsNullOrWhiteSpace(xml)) return [];
        XmlDocument document = new();
        try { document.LoadXml(xml); }
        catch (XmlException) { return []; }

        var ports = new List<InspectedPort>();
        foreach (var item in document.SelectNodes("/ArrayOfModuleParamItem/ModuleParamItem")?.OfType<XmlElement>() ?? [])
        {
            var rawName = item.SelectSingleNode("Name")?.InnerText;
            if (string.IsNullOrWhiteSpace(rawName)) continue;
            var name = rawName.Trim('%');
            subscriptions.TryGetValue(name, out var saved);
            ports.Add(new InspectedPort(
                name,
                item.SelectSingleNode("ValueType")?.InnerText,
                ParseBool(item.SelectSingleNode("IsForce")?.InnerText),
                ParseBool(item.SelectSingleNode("IsShow")?.InnerText),
                saved.Literal,
                saved.Binding));
        }
        return ports;
    }

    /// <summary>
    /// Reads a binary parameter in full. The parser's <c>parsed</c> field is a truncated preview, so the
    /// untruncated <c>rawDataB64</c> payload wins whenever it is present; VM pads these blobs with NULs.
    /// </summary>
    private static string? BinaryParameter(JsonElement module, string name)
    {
        if (Array(module, "binaryParams").FirstOrDefault(x => String(x, "name") == name) is not { ValueKind: JsonValueKind.Object } parameter)
            return null;
        if (String(parameter, "rawDataB64") is { Length: > 0 } encoded)
        {
            try { return System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(encoded)).TrimEnd('\0'); }
            catch (FormatException) { /* fall through to the preview */ }
        }
        return String(parameter, "parsed");
    }

    private static bool? ParseBool(string? value) => bool.TryParse(value, out var result) ? result : null;
    private static IEnumerable<JsonElement> Array(JsonElement element, string name) =>
        element.ValueKind == JsonValueKind.Object && element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.Array
            ? value.EnumerateArray() : [];
    private static string? String(JsonElement element, string name) =>
        element.ValueKind == JsonValueKind.Object && element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;
    private static int? Int(JsonElement element, string name) =>
        element.ValueKind == JsonValueKind.Object && element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var result) ? result : null;
    private static bool? Bool(JsonElement element, string name) =>
        element.ValueKind == JsonValueKind.Object && element.TryGetProperty(name, out var value) && value.ValueKind is JsonValueKind.True or JsonValueKind.False ? value.GetBoolean() : null;
}
