using System.Text;
using System.Text.Json;
using System.Xml;

namespace VmScriptCompiler.Core;

public sealed record ModuleScriptArtifact(string Id, string Carrier, string Procedure, string Name, string SourceFile, IReadOnlyList<string> Inputs, IReadOnlyList<string> Outputs);
public sealed record ModuleCompileResult(ProcessResult Modify, IReadOnlyList<ModuleScriptArtifact> Artifacts);

public sealed class ModuleScriptCompiler(string repositoryRoot, ParserClient parser)
{
    private readonly string _repositoryRoot = Path.GetFullPath(repositoryRoot);

    public ModuleCompileResult Compile(string inputSolution, string outputSolution, string templateSolution, IReadOnlyList<ScriptRequirement> scripts, IReadOnlyList<ConnectionRequirement> connections, string mode, string generatedDirectory, string validationDirectory, string? baselineParseFile = null)
    {
        var modules = scripts.Where(x => x.Carrier is "csharp-module" or "python-module").ToArray();
        ValidateRequirements(modules, mode, inputSolution, validationDirectory, baselineParseFile);
        var changes = new List<Dictionary<string, object?>>();
        var artifacts = new List<ModuleScriptArtifact>();

        if (mode == "create") BuildCreateChanges(modules, templateSolution, changes);
        else BuildPatchChanges(modules, templateSolution, validationDirectory, baselineParseFile, changes);

        foreach (var script in modules)
        {
            var source = ResolveSource(script);
            ValidateSource(script.Carrier, source);
            var extension = script.Carrier == "python-module" ? ".py" : ".cs";
            var sourceFile = Path.Combine(generatedDirectory, SafeName(script.Id) + extension);
            File.WriteAllText(sourceFile, source, new UTF8Encoding(false));
            var target = script.Procedure + "." + script.Name;
            // VM 4.4 GenerateUserPropertyCs returns no partial class at all when the input list is empty.
            // A hidden anchor keeps output-only C# modules compilable without exposing this VM quirk in IR/UI.
            var needsCSharpPropertyAnchor = script.Carrier == "csharp-module" && script.Inputs.Count == 0;
            var csharpLayout = script.Carrier == "csharp-module" ? Vm44CSharpIoLayout.Create(script.Inputs, script.Outputs) : null;
            AddSetBinary(changes, target, "ShellContent", source);
            AddSetBinary(changes, target, "Input", PortXml(script.Inputs, script.Carrier, false, needsCSharpPropertyAnchor, csharpLayout?.Inputs));
            AddSetBinary(changes, target, "Output", PortXml(script.Outputs, script.Carrier, true, false, csharpLayout?.Outputs));
            AddSetBinary(changes, target, "DynamicInData", DynamicXml(script.Inputs, false, needsCSharpPropertyAnchor, csharpLayout?.Inputs));
            AddSetBinary(changes, target, "DynamicOutData", DynamicXml(script.Outputs, true, false, csharpLayout?.Outputs));
            artifacts.Add(new(script.Id, script.Carrier, script.Procedure!, script.Name, sourceFile, script.Inputs.Select(x => x.Name).ToArray(), script.Outputs.Select(x => x.Name).ToArray()));
        }

        AddExplicitConnections(modules, connections, changes);

        if (changes.Count == 0) { File.Copy(inputSolution, outputSolution); return new(new(0, "No module changes required.", ""), artifacts); }
        var changesFile = Path.Combine(validationDirectory, "module-changes.json");
        File.WriteAllText(changesFile, JsonSerializer.Serialize(new { changes }, JsonDefaults.Options));
        var modify = parser.Modify(inputSolution, changesFile, outputSolution);
        if (modify.ExitCode != 0 || !File.Exists(outputSolution)) throw new CompilerException("SOL_MODIFY_FAILED", string.IsNullOrWhiteSpace(modify.StandardError) ? modify.StandardOutput : modify.StandardError);
        if (mode == "patch") ModuleFrameCompatibility.RepairVersion4WriterOutput(inputSolution, outputSolution);
        var compatibilityWrites = modules.Where(x => x.Carrier == "csharp-module")
            .SelectMany(x => new[]
            {
                new ModuleBinaryParameterWrite(x.Procedure!, x.Name, "AssemblyGuid", Encoding.UTF8.GetBytes(DeterministicAssemblyGuid(x))),
                x.Dependencies.Any(d => d.Kind == "dotnet-assembly")
                    ? new ModuleBinaryParameterWrite(x.Procedure!, x.Name, "ShellRefrences", Encoding.UTF8.GetBytes(BuildShellReferences(x)))
                    : null
            }.Where(x => x is not null).Cast<ModuleBinaryParameterWrite>()).ToArray();
        if (compatibilityWrites.Length > 0) ModuleFrameCompatibility.AddOrReplaceBinaryParameters(outputSolution, compatibilityWrites);
        var modifierReport = outputSolution + ".modify.json";
        if (File.Exists(modifierReport)) File.Move(modifierReport, Path.Combine(validationDirectory, "modify-result.json"), true);
        return new(modify, artifacts);
    }

    private static void AddExplicitConnections(ScriptRequirement[] modules, IReadOnlyList<ConnectionRequirement> connections, List<Dictionary<string, object?>> changes)
    {
        var byId = modules.ToDictionary(x => x.Id, StringComparer.Ordinal);
        foreach (var connection in connections)
        {
            if (!byId.TryGetValue(connection.From, out var from) || !byId.TryGetValue(connection.To, out var to))
                throw new CompilerException("CONNECTION_ENDPOINT_NOT_FOUND", "Explicit connection endpoint is not a module script.");
            if (!string.Equals(from.Procedure, to.Procedure, StringComparison.Ordinal))
                throw new CompilerException("CROSS_PROCEDURE_CONNECTION_UNSUPPORTED", "VM module connections must stay within one procedure.");
            changes.Add(Change("setConnection", from.Procedure + "." + from.Name, "follow", to.Procedure + "." + to.Name));
        }
    }

    private void BuildCreateChanges(ScriptRequirement[] modules, string templateSolution, List<Dictionary<string, object?>> changes)
    {
        var procedures = modules.Select(x => x.Procedure!).Distinct(StringComparer.Ordinal).ToArray();
        var firstProcedure = procedures.FirstOrDefault() ?? "流程1";
        if (firstProcedure != "流程1") changes.Add(Change("setDisplayName", "流程1", value: firstProcedure));
        foreach (var procedure in procedures.Skip(1))
            changes.Add(Change("addProcedure", "", value: procedure, templateFile: templateSolution, templateModule: "流程1"));
        ConfigureTemplateCarrier(firstProcedure, "脚本1", "ShellModule", modules.Where(x => x.Carrier == "csharp-module").ToArray(), templateSolution, changes);
        ConfigureTemplateCarrier(firstProcedure, "Python脚本1", "PyShellModule", modules.Where(x => x.Carrier == "python-module").ToArray(), templateSolution, changes);
    }

    private static void ConfigureTemplateCarrier(string firstProcedure, string templateDisplayName, string moduleType, ScriptRequirement[] scripts, string templateSolution, List<Dictionary<string, object?>> changes)
    {
        var templatePath = firstProcedure + "." + templateDisplayName;
        var first = scripts.FirstOrDefault(x => x.Procedure == firstProcedure);
        if (first is null) changes.Add(Change("deleteModule", templatePath));
        else if (first.Name != templateDisplayName) changes.Add(Change("setDisplayName", templatePath, value: first.Name));
        foreach (var script in scripts.Where(x => !ReferenceEquals(x, first)))
        {
            var sourceTemplate = moduleType == "ShellModule" ? "流程1.脚本1" : "流程1.Python脚本1";
            changes.Add(Change("addModule", script.Procedure!, moduleType, script.Name, templateSolution, sourceTemplate));
        }
    }

    private static void BuildPatchChanges(ScriptRequirement[] modules, string templateSolution, string validationDirectory, string? baselineParseFile, List<Dictionary<string, object?>> changes)
    {
        var inventoryFile = baselineParseFile ?? Path.Combine(validationDirectory, "base-parse-result.json");
        if (!File.Exists(inventoryFile)) throw new CompilerException("SOL_PARSE_FAILED", "Patch base inventory is missing.");
        using var document = JsonDocument.Parse(File.ReadAllText(inventoryFile));
        var existing = document.RootElement.GetProperty("solution").GetProperty("procedures").EnumerateArray()
            .SelectMany(x => x.GetProperty("modules").EnumerateArray())
            .ToDictionary(x => x.GetProperty("fullPath").GetString()!, x => x.GetProperty("name").GetString()!, StringComparer.Ordinal);
        foreach (var script in modules)
        {
            var type = script.Carrier == "python-module" ? "PyShellModule" : "ShellModule";
            var target = script.Procedure + "." + script.Name;
            if (existing.TryGetValue(target, out var existingType))
            {
                if (!string.Equals(existingType, type, StringComparison.Ordinal))
                    throw new CompilerException("MODULE_TYPE_MISMATCH", $"Patch target {target} already exists as {existingType}, not {type}.");
                continue;
            }
            var templateModule = script.Carrier == "python-module" ? "流程1.Python脚本1" : "流程1.脚本1";
            changes.Add(Change("addModule", script.Procedure!, type, script.Name, templateSolution, templateModule));
        }
    }

    private void ValidateRequirements(ScriptRequirement[] modules, string mode, string inputSolution, string validationDirectory, string? baselineParseFile)
    {
        var referenceCatalog = LoadShellReferenceCatalog();
        foreach (var group in modules.GroupBy(x => x.Procedure + "\0" + x.Name, StringComparer.Ordinal))
            if (group.Count() > 1) throw new CompilerException("DUPLICATE_MODULE_NAME", "Module display name must be unique within a procedure: " + group.First().Procedure + "." + group.First().Name);
        foreach (var script in modules)
        {
            foreach (var dependency in script.Dependencies.Where(x => x.Kind == "dotnet-assembly"))
                if (!referenceCatalog.Verified.ContainsKey(dependency.Name) && !(dependency.ReferenceType == 4 && !string.IsNullOrWhiteSpace(dependency.Path)))
                    throw new CompilerException("REFERENCE_TYPE_UNCONFIRMED", "No project-local VM ShellRefrences evidence for assembly: " + dependency.Name);
                else if (referenceCatalog.Verified.TryGetValue(dependency.Name, out var verified) && dependency.ReferenceType is not null && dependency.ReferenceType != verified.ReferenceType)
                    throw new CompilerException("REFERENCE_TYPE_MISMATCH", $"Declared referenceType {dependency.ReferenceType} does not match verified type {verified.ReferenceType} for {dependency.Name}.");
                else if (referenceCatalog.Verified.TryGetValue(dependency.Name, out verified) && !string.IsNullOrWhiteSpace(dependency.Role) && !string.Equals(dependency.Role, verified.Role, StringComparison.OrdinalIgnoreCase))
                    throw new CompilerException("DEPENDENCY_ROLE_MISMATCH", $"Declared role {dependency.Role} does not match verified role {verified.Role} for {dependency.Name}.");
        }
        ValidateOperationTargets(modules, mode, inputSolution, validationDirectory, baselineParseFile);
    }

    private string BuildShellReferences(ScriptRequirement script)
    {
        var catalog = LoadShellReferenceCatalog();
        var references = new List<ShellReference>(catalog.Defaults);
        var names = references.Select(x => x.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var dependency in script.Dependencies.Where(x => x.Kind == "dotnet-assembly"))
        {
            var reference = catalog.Verified.TryGetValue(dependency.Name, out var verified)
                ? verified
                : new ShellReference(dependency.Name, dependency.ReferenceType ?? throw new CompilerException("REFERENCE_TYPE_UNCONFIRMED", "Explicit external DLL requires referenceType 4: " + dependency.Name), dependency.Role ?? "third-party");
            if (names.Add(reference.Name)) references.Add(reference);
        }
        return string.Concat(references.Select(x => x.Name + "\n" + x.ReferenceType.ToString(System.Globalization.CultureInfo.InvariantCulture) + "\r"));
    }

    private ShellReferenceCatalog LoadShellReferenceCatalog()
    {
        var file = Path.Combine(_repositoryRoot, "resources", "vm", "4.4.0", "shell-reference-catalog.json");
        using var document = JsonDocument.Parse(File.ReadAllText(file));
        var defaults = document.RootElement.GetProperty("defaultReferences").EnumerateArray()
            .Select(x => new ShellReference(x.GetProperty("name").GetString()!, x.GetProperty("referenceType").GetInt32(), x.GetProperty("role").GetString()!)).ToArray();
        var verified = document.RootElement.GetProperty("verifiedReferences").EnumerateObject()
            .ToDictionary(x => x.Name, x => new ShellReference(x.Name, x.Value.GetProperty("referenceType").GetInt32(), x.Value.GetProperty("role").GetString()!), StringComparer.OrdinalIgnoreCase);
        return new(defaults, verified);
    }

    private void ValidateOperationTargets(ScriptRequirement[] modules, string mode, string inputSolution, string validationDirectory, string? baselineParseFile)
    {
        var known = modules.ToDictionary(
            x => x.Procedure + "." + x.Name,
            x => new ModuleEvidence(x.Carrier, x.Inputs.Select(p => p.Name).Concat(x.Outputs.Select(p => p.Name)).Concat(["ModuRunTime", "ModuStatus", "ResultShow"]).ToHashSet(StringComparer.Ordinal)),
            StringComparer.Ordinal);
        if (mode == "patch")
        {
            var inventoryFile = baselineParseFile ?? Path.Combine(validationDirectory, "base-parse-result.json");
            if (!File.Exists(inventoryFile))
            {
                var result = parser.Parse(inputSolution, inventoryFile);
                if (result.ExitCode != 0) throw new CompilerException("SOL_PARSE_FAILED", "Cannot inspect Patch base solution.");
            }
            using var document = JsonDocument.Parse(File.ReadAllText(inventoryFile));
            foreach (var procedure in document.RootElement.GetProperty("solution").GetProperty("procedures").EnumerateArray())
            foreach (var module in procedure.GetProperty("modules").EnumerateArray())
            {
                var parameters = new HashSet<string>(StringComparer.Ordinal);
                if (module.TryGetProperty("algoriParams", out var algorithm))
                    foreach (var parameter in algorithm.EnumerateArray()) if (parameter.TryGetProperty("name", out var name)) parameters.Add(name.GetString()!);
                if (module.TryGetProperty("binaryParams", out var binary))
                    foreach (var item in binary.EnumerateArray()) if (item.TryGetProperty("parsed", out var parsed) && parsed.ValueKind == JsonValueKind.String)
                        foreach (System.Text.RegularExpressions.Match match in System.Text.RegularExpressions.Regex.Matches(parsed.GetString()!, "(?:Name=\\\"|<Name>%?)([A-Za-z0-9_\\p{L}-]+)")) parameters.Add(match.Groups[1].Value.Trim('%'));
                known[module.GetProperty("fullPath").GetString()!] = new ModuleEvidence(module.GetProperty("name").GetString()!, parameters);
            }
        }
        var catalog = LoadModuleParameterCatalog();
        foreach (var script in modules)
        foreach (var operation in script.Operations.Where(x => x.Kind is "getModule" or "setModuleValue" or "getModuleValue" or "getModuleArray" or "getModuleParam"))
        {
            var procedure = operation.Procedure ?? script.Procedure;
            var path = operation.Module?.Contains('.') == true ? operation.Module : procedure + "." + operation.Module;
            if (string.IsNullOrWhiteSpace(operation.Module) || !known.TryGetValue(path!, out var evidence)) throw new CompilerException("EXTERNAL_MODULE_NOT_AVAILABLE", "Referenced module is not available: " + path);
            if (operation.Kind == "getModule") continue;
            var parameter = operation.Parameter!;
            var catalogParameters = catalog.TryGetValue(evidence.Type, out var values) ? values : [];
            if (!evidence.Parameters.Contains(parameter) && !catalogParameters.Contains(parameter))
                throw new CompilerException("MODULE_PARAMETER_NOT_FOUND", "Referenced module parameter is not verified: " + path + "." + parameter);
        }
    }

    private Dictionary<string, HashSet<string>> LoadModuleParameterCatalog()
    {
        var file = Path.Combine(_repositoryRoot, "resources", "vm", "4.4.0", "module-parameter-catalog.json");
        using var document = JsonDocument.Parse(File.ReadAllText(file));
        var result = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
        foreach (var module in document.RootElement.GetProperty("modules").EnumerateObject())
        {
            var values = new HashSet<string>(StringComparer.Ordinal);
            foreach (var property in new[] { "setValueActions", "setValueParameters" })
                if (module.Value.TryGetProperty(property, out var items)) foreach (var item in items.EnumerateArray()) values.Add(item.GetString()!);
            result[module.Name] = values;
        }
        return result;
    }

    private sealed record ModuleEvidence(string Type, HashSet<string> Parameters);
    private sealed record ShellReference(string Name, int ReferenceType, string Role);
    private sealed record ShellReferenceCatalog(IReadOnlyList<ShellReference> Defaults, IReadOnlyDictionary<string, ShellReference> Verified);

    private string ResolveSource(ScriptRequirement script)
    {
        if (!string.IsNullOrWhiteSpace(script.Source)) return Vm44SourceCompatibility.Normalize(script, script.Source);
        if (script.Operations.Count > 0) return DeterministicScriptGenerator.Generate(script);
        var template = script.Carrier == "python-module" ? "py-shell-module.py" : "shell-module.cs";
        return File.ReadAllText(Path.Combine(_repositoryRoot, "resources", "vm", "4.4.0", "script-templates", template), Encoding.UTF8);
    }

    private static void ValidateSource(string carrier, string source)
    {
        var valid = carrier == "python-module"
            ? source.Contains("def Process(data)", StringComparison.Ordinal) && source.Contains("INIT_MODULE_VAR", StringComparison.Ordinal)
            : source.Contains("UserScript", StringComparison.Ordinal) && source.Contains("IProcessMethods", StringComparison.Ordinal) && source.Contains("void Init()", StringComparison.Ordinal) && source.Contains("bool Process()", StringComparison.Ordinal);
        if (!valid) throw new CompilerException("SCRIPT_CONTRACT_INVALID", "Generated source does not satisfy the " + carrier + " entry contract.");
    }

    private static void AddSetBinary(List<Dictionary<string, object?>> changes, string target, string name, string value) => changes.Add(Change("setBinaryParam", target, name, value));
    private static Dictionary<string, object?> Change(string action, string target, string? paramName = null, string? value = null, string? templateFile = null, string? templateModule = null) =>
        new() { ["action"] = action, ["target"] = target, ["paramName"] = paramName, ["value"] = value, ["templateFile"] = templateFile, ["templateModule"] = templateModule };

    private static string PortXml(IReadOnlyList<IoRequirement> ports, string carrier, bool output, bool includeHiddenPropertyAnchor = false, IReadOnlyList<VmPortBinding>? bindings = null)
    {
        var items = new List<(string Name, string StructName, string Type, bool Visible)>();
        if (includeHiddenPropertyAnchor) items.Add(("%__CompilerPortAnchor%", "%__CompilerPortAnchor%", "int", false));
        if (output)
        {
            items.Add(("ModuRunTime", "ModuRunTime", "float", true));
            if (carrier == "csharp-module") items.Add(("ResultShow", "ResultShow", "string", true));
            items.Add(("ModuStatus", "ModuStatus", "int", true));
        }
        items.AddRange(bindings is null
            ? ports.Select(x => ("%" + x.Name + "%", "%" + x.Name + "%", PortVmType(x.Type), true))
            : bindings.Select(x => (x.LogicalName, string.Join('\r', x.StructNames), x.ValueType, true)));
        var builder = new StringBuilder("<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n<ArrayOfModuleParamItem>\n");
        foreach (var item in items)
        {
            var name = Escape(item.Name);
            builder.Append("    <ModuleParamItem>\n")
                .Append("        <Name>").Append(name).Append("</Name>\n")
                .Append("        <StructName>").Append(Escape(item.StructName)).Append("</StructName>\n")
                .Append("        <ValueType>").Append(item.Type).Append("</ValueType>\n")
                .Append("        <IsForce>true</IsForce>\n")
                .Append("        <IsShow>").Append(item.Visible ? "true" : "false").Append("</IsShow>\n")
                .Append("    </ModuleParamItem>\n");
        }
        builder.Append("</ArrayOfModuleParamItem>\n");
        builder.Append(carrier == "csharp-module" ? "\0\0" : "\0");
        return builder.ToString();
    }

    private static string DynamicXml(IReadOnlyList<IoRequirement> ports, bool output, bool includeHiddenPropertyAnchor = false, IReadOnlyList<VmPortBinding>? bindings = null)
    {
        const string visible = "CustomVisible=True;IsForce=False;CanSubscribe=True;IsPrefer=False;Visible=True;VisibleInResultTree=True;IsPrivate=False;IsReturnRelateValue=True;AllowAutoSubscribe=True;AutoSubscribeByOther=True;IsResultShow=False;AutoSubscribeName=";
        const string hidden = "CustomVisible=False;IsForce=False;CanSubscribe=False;IsPrefer=False;Visible=False;VisibleInResultTree=False;IsPrivate=False;IsReturnRelateValue=True;AllowAutoSubscribe=True;AutoSubscribeByOther=True;IsResultShow=False;AutoSubscribeName=";
        var filters = new List<(string Name, string Type, string Others)>();
        if (includeHiddenPropertyAnchor) filters.Add(("%__CompilerPortAnchor%", "int", hidden));
        if (output) filters.AddRange(new[] { "fArrivalTimeStampLow", "fArrivalTimeStampHigh", "fLeaveTimeStampLow", "fLeaveTimeStampHigh" }.Select(x => (x, "float", hidden)));
        if (bindings is null) filters.AddRange(ports.Select(x => ("%" + x.Name + "%", DynamicVmType(x.Type), visible)));
        var category = output ? "Output" : "Input";
        var builder = new StringBuilder("<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n<ParamRoot>\n    <Categorys>\n        <Category Name=\"")
            .Append(category).Append("\">\n            <Items>\n");
        foreach (var filter in filters)
            builder.Append("                <Filter Name=\"").Append(Escape(filter.Name)).Append("\" ValueType=\"").Append(filter.Type).Append("\" Others=\"").Append(filter.Others).Append("\"/>\n");
        if (bindings is not null)
            foreach (var binding in bindings) AppendDynamic(builder, binding.Dynamic, 16, visible);
        builder.Append("            </Items>\n        </Category>\n    </Categorys>\n</ParamRoot>\n\0");
        return builder.ToString();
    }

    private static void AppendDynamic(StringBuilder builder, VmIoFilter item, int indent, string filterOthers)
    {
        var spaces = new string(' ', indent);
        if (!item.IsCombination)
        {
            builder.Append(spaces).Append("<Filter Name=\"").Append(Escape(item.Name)).Append("\" ValueType=\"").Append(item.ValueType)
                .Append("\" Others=\"").Append(filterOthers).Append("\"/>\n");
            return;
        }
        const string combinationOthers = "CanSubscribe=True;IsPrefer=False;Visible=True;VisibleInResultTree=True;AutoSubscribeName=";
        builder.Append(spaces).Append("<Combination Name=\"").Append(Escape(item.Name)).Append("\" Style=\"").Append(item.Style)
            .Append("\" Others=\"").Append(combinationOthers).Append("\">\n")
            .Append(spaces).Append("    <Filters>\n");
        foreach (var child in item.Children ?? []) AppendDynamic(builder, child, indent + 8, filterOthers);
        builder.Append(spaces).Append("    </Filters>\n").Append(spaces).Append("</Combination>\n");
    }

    private static string PortVmType(string type) => type switch {
        "bool" => "int", "int" => "int", "int[]" => "int[]", "float" => "float", "float[]" => "float[]",
        "string" => "string", "string[]" => "string[]", "byte" => "byte", "image" => "IMAGE",
        "roibox" => "ROIBOX", "roibox[]" => "ROIBOX[]", "roiannulus" => "ROIANNULUS",
        "roipolygon" => "ROIPOLYGON", "point" => "POINT", "line" => "LINE", "fixture" => "FIXTURE",
        "rect" => "Rect", "ellipse" => "ELLIPSE", "pointset" => "pointset",
        _ => throw new CompilerException("IO_TYPE_MISMATCH", "Unsupported IO type: " + type)
    };
    private static string DeterministicAssemblyGuid(ScriptRequirement script)
    {
        var hash = System.Security.Cryptography.SHA256.HashData(Encoding.UTF8.GetBytes(script.Procedure + "\0" + script.Name + "\0" + script.Id));
        var bytes = hash.AsSpan(0, 16).ToArray();
        bytes[7] = (byte)((bytes[7] & 0x0F) | 0x50);
        bytes[8] = (byte)((bytes[8] & 0x3F) | 0x80);
        return new Guid(bytes).ToString("D");
    }
    private static string DynamicVmType(string type) => type switch {
        "bool" => "int", "int" or "int[]" => "int", "float" or "float[]" => "float",
        "string" or "string[]" => "string", "byte" => "byte", "image" => "IMAGE",
        "roibox" or "roibox[]" => "ROIBOX", "roiannulus" => "ROIANNULUS", "roipolygon" => "ROIPOLYGON",
        "point" => "POINT", "line" => "LINE", "fixture" => "FIXTURE",
        "rect" => "Rect", "ellipse" => "ELLIPSE", "pointset" => "pointset",
        _ => throw new CompilerException("IO_TYPE_MISMATCH", "Unsupported IO type: " + type)
    };
    private static string Escape(string value) => System.Security.SecurityElement.Escape(value) ?? "";
    private static string SafeName(string value) => string.Concat(value.Select(x => char.IsLetterOrDigit(x) || x is '-' or '_' ? x : '_'));
    private static bool IsPythonStandardLibrary(string name) => new[] { "math", "json", "re", "datetime", "collections", "itertools", "statistics", "random", "time" }.Contains(name, StringComparer.Ordinal);
}
