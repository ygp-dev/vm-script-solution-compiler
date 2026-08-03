using System.Text.Json;
using System.Text.RegularExpressions;

namespace VmScriptCompiler.Core;

public sealed class RequirementValidator
{
    private static readonly HashSet<string> Carriers = ["global-csharp", "csharp-module", "python-module"];
    private static readonly HashSet<string> Types = [
        "bool", "int", "int[]", "float", "float[]", "string", "string[]", "byte",
        "image", "roibox", "roibox[]", "roiannulus", "roipolygon", "point", "line",
        "fixture", "circle", "rect", "ellipse", "pointset"
    ];
    private static readonly HashSet<string> CSharpPortTypes = new(Types.Where(x => x != "circle"), StringComparer.Ordinal);
    private static readonly HashSet<string> ConfirmedPythonPortTypes = ["bool", "int", "int[]", "float", "float[]", "string", "string[]"];
    private static readonly HashSet<string> ExecutionModes = ["init", "once", "continuous", "callback"];
    private static readonly HashSet<string> OperationKinds = [
        "getModule", "getModuleValue", "getModuleArray", "getModuleParam", "setModuleValue", "setOutput",
        "runProcedure", "continuousProcedure", "stopProcedure", "setContinuousInterval",
        "startGlobalCommunication", "sendCommunication", "log", "sleep", "bytesToPointset",
        "setProcedureInput", "getGlobalVariable", "setGlobalVariable", "getLocalVariable", "setLocalVariable",
        "saveSolution", "loadSolution", "dxfRender"
    ];

    public RequirementValidationResult Validate(JsonDocument document)
    {
        var issues = new List<ValidationIssue>();
        var root = document.RootElement;
        Require(root.ValueKind == JsonValueKind.Object, "REQUIREMENT_SCHEMA_INVALID", "Root must be an object.", "$", issues);
        if (root.ValueKind != JsonValueKind.Object) return new(false, issues);
        CheckOnly(root, ["schemaVersion", "task", "scripts", "connections"], "$", issues);
        Require(String(root, "schemaVersion") == "1.0", "REQUIREMENT_SCHEMA_INVALID", "schemaVersion must be 1.0.", "$.schemaVersion", issues);
        ValidateTask(Property(root, "task"), issues);

        var scripts = Property(root, "scripts");
        Require(scripts is { ValueKind: JsonValueKind.Array } && scripts.Value.GetArrayLength() > 0, "REQUIREMENT_SCHEMA_INVALID", "scripts must contain at least one item.", "$.scripts", issues);
        if (scripts is { ValueKind: JsonValueKind.Array })
        {
            var ids = new HashSet<string>(StringComparer.Ordinal);
            var moduleNames = new HashSet<string>(StringComparer.Ordinal);
            var globalCount = 0;
            foreach (var (script, index) in scripts.Value.EnumerateArray().Select((x, i) => (x, i)))
            {
                ValidateScript(script, index, issues);
                var id = String(script, "id");
                if (id is not null && !ids.Add(id)) issues.Add(new("DUPLICATE_SCRIPT_ID", "Script id must be unique.", $"$.scripts[{index}].id"));
                var carrier = String(script, "carrier");
                if (carrier == "global-csharp") globalCount++;
                else if (String(script, "procedure") is { } procedure && String(script, "name") is { } name && !moduleNames.Add(procedure + "\0" + name))
                    issues.Add(new("DUPLICATE_MODULE_NAME", "Module display name must be unique within a procedure.", $"$.scripts[{index}].name"));
            }
            if (globalCount > 1) issues.Add(new("MULTIPLE_GLOBAL_SCRIPTS", "VM 4.4 supports one GlobalScript_0 carrier.", "$.scripts"));
            ValidateConnections(Property(root, "connections"), ids, scripts.Value, issues);
        }
        return new(issues.Count == 0, issues);
    }

    private static void ValidateConnections(JsonElement? connections, HashSet<string> ids, JsonElement scripts, List<ValidationIssue> issues)
    {
        if (connections is null) return;
        Require(connections is { ValueKind: JsonValueKind.Array }, "REQUIREMENT_SCHEMA_INVALID", "connections must be an array.", "$.connections", issues);
        if (connections is not { ValueKind: JsonValueKind.Array }) return;
        var scriptById = scripts.EnumerateArray().Where(x => String(x, "id") is not null).ToDictionary(x => String(x, "id")!, x => x.Clone(), StringComparer.Ordinal);
        var carriers = scriptById.ToDictionary(x => x.Key, x => String(x.Value, "carrier"), StringComparer.Ordinal);
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var (connection, index) in connections.Value.EnumerateArray().Select((x, i) => (x, i)))
        {
            var path = $"$.connections[{index}]";
            Require(connection.ValueKind == JsonValueKind.Object, "REQUIREMENT_SCHEMA_INVALID", "Connection must be an object.", path, issues);
            if (connection.ValueKind != JsonValueKind.Object) continue;
            CheckOnly(connection, ["from", "to"], path, issues);
            var from = String(connection, "from"); var to = String(connection, "to");
            Require(from is not null && ids.Contains(from), "CONNECTION_ENDPOINT_NOT_FOUND", "Connection source script does not exist.", path + ".from", issues);
            Require(to is not null && ids.Contains(to), "CONNECTION_ENDPOINT_NOT_FOUND", "Connection target script does not exist.", path + ".to", issues);
            Require(from != to, "INVALID_CONNECTION", "Connection cannot target itself.", path, issues);
            Require(from is null || !carriers.TryGetValue(from, out var fromCarrier) || fromCarrier != "global-csharp", "INVALID_CONNECTION", "GlobalScript cannot be a module connection endpoint.", path + ".from", issues);
            Require(to is null || !carriers.TryGetValue(to, out var toCarrier) || toCarrier != "global-csharp", "INVALID_CONNECTION", "GlobalScript cannot be a module connection endpoint.", path + ".to", issues);
            if (from is not null && to is not null && scriptById.TryGetValue(from, out var fromScript) && scriptById.TryGetValue(to, out var toScript))
                Require(string.Equals(String(fromScript, "procedure"), String(toScript, "procedure"), StringComparison.Ordinal), "CROSS_PROCEDURE_CONNECTION_UNSUPPORTED", "VM module connections must stay within one procedure.", path, issues);
            if (from is not null && to is not null && !seen.Add(from + "\0" + to)) issues.Add(new("DUPLICATE_CONNECTION", "Connection is duplicated.", path));
        }
    }

    private static void ValidateTask(JsonElement? task, List<ValidationIssue> issues)
    {
        Require(task is { ValueKind: JsonValueKind.Object }, "REQUIREMENT_SCHEMA_INVALID", "task must be an object.", "$.task", issues);
        if (task is not { ValueKind: JsonValueKind.Object }) return;
        CheckOnly(task.Value, ["name", "mode", "vmVersion", "baseSolution"], "$.task", issues);
        Require(!string.IsNullOrWhiteSpace(String(task.Value, "name")), "REQUIREMENT_SCHEMA_INVALID", "task.name is required.", "$.task.name", issues);
        var mode = String(task.Value, "mode");
        Require(mode is "create" or "patch", "REQUIREMENT_SCHEMA_INVALID", "task.mode must be create or patch.", "$.task.mode", issues);
        Require(Regex.IsMatch(String(task.Value, "vmVersion") ?? "", "^4\\.4(?:\\.\\d+)?$"), "REQUIREMENT_SCHEMA_INVALID", "task.vmVersion must match VM 4.4.x.", "$.task.vmVersion", issues);
    }

    private static void ValidateScript(JsonElement script, int index, List<ValidationIssue> issues)
    {
        var path = $"$.scripts[{index}]";
        Require(script.ValueKind == JsonValueKind.Object, "REQUIREMENT_SCHEMA_INVALID", "Script must be an object.", path, issues);
        if (script.ValueKind != JsonValueKind.Object) return;
        CheckOnly(script, ["id", "carrier", "name", "source", "procedure", "execution", "inputs", "outputs", "operations", "dependencies"], path, issues);
        var id = String(script, "id");
        Require(id is not null && Regex.IsMatch(id, "^[a-z0-9][a-z0-9-]*$"), "REQUIREMENT_SCHEMA_INVALID", "Script id has an invalid format.", path + ".id", issues);
        var carrier = String(script, "carrier");
        Require(carrier is not null && Carriers.Contains(carrier), "REQUIREMENT_SCHEMA_INVALID", "Script carrier is invalid.", path + ".carrier", issues);
        Require(!string.IsNullOrWhiteSpace(String(script, "name")), "REQUIREMENT_SCHEMA_INVALID", "Script name is required.", path + ".name", issues);
        Require(IsVmDisplayName(String(script, "name")), "VM_NAME_INVALID", "Script display name cannot contain a dot or control character.", path + ".name", issues);
        if (Property(script, "source") is { } source) Require(source.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(source.GetString()), "REQUIREMENT_SCHEMA_INVALID", "Script source must be a non-empty string.", path + ".source", issues);
        Require(carrier == "global-csharp" || !string.IsNullOrWhiteSpace(String(script, "procedure")), "REQUIREMENT_SCHEMA_INVALID", "Module scripts require procedure.", path + ".procedure", issues);
        if (carrier != "global-csharp") Require(IsVmDisplayName(String(script, "procedure")), "VM_NAME_INVALID", "Procedure name cannot contain a dot or control character.", path + ".procedure", issues);
        ValidateExecution(Property(script, "execution"), path + ".execution", issues);
        var hasSource = Property(script, "source") is { ValueKind: JsonValueKind.String };
        var executionMode = Property(script, "execution") is { } execution ? String(execution, "mode") : null;
        if (!hasSource && executionMode == "callback")
            issues.Add(new("CALLBACK_SOURCE_REQUIRED", "Callback scripts require explicit source because callback payload handling is carrier-specific.", path + ".execution.mode"));
        if (!hasSource && carrier == "python-module" && executionMode == "init")
            issues.Add(new("EXECUTION_MODE_UNSUPPORTED", "PyShellModule exposes Process(data), not a deterministic init entry.", path + ".execution.mode"));
        ValidatePorts(Property(script, "inputs"), path + ".inputs", carrier, issues);
        ValidatePorts(Property(script, "outputs"), path + ".outputs", carrier, issues);
        if (carrier == "global-csharp")
        {
            Require(Property(script, "inputs") is { ValueKind: JsonValueKind.Array } globalInputs && globalInputs.GetArrayLength() == 0, "GLOBAL_SCRIPT_IO_UNSUPPORTED", "GlobalScript does not have module IO ports.", path + ".inputs", issues);
            Require(Property(script, "outputs") is { ValueKind: JsonValueKind.Array } globalOutputs && globalOutputs.GetArrayLength() == 0, "GLOBAL_SCRIPT_IO_UNSUPPORTED", "GlobalScript does not have module IO ports.", path + ".outputs", issues);
        }
        if (Property(script, "inputs") is { ValueKind: JsonValueKind.Array } inputs && Property(script, "outputs") is { ValueKind: JsonValueKind.Array } outputs)
        {
            var inputNames = inputs.EnumerateArray().Select(x => String(x, "name")).Where(x => x is not null).ToHashSet(StringComparer.Ordinal);
            foreach (var name in outputs.EnumerateArray().Select(x => String(x, "name")).Where(x => x is not null))
                if (inputNames.Contains(name)) issues.Add(new("DUPLICATE_IO_NAME", "Input and output names must not collide.", path + ".outputs"));
        }
        if (hasSource && Property(script, "operations") is { ValueKind: JsonValueKind.Array } sourceOperations && sourceOperations.GetArrayLength() > 0)
            issues.Add(new("SOURCE_OPERATIONS_CONFLICT", "Use either explicit source or structured operations; operations beside source would be ignored.", path + ".operations"));
        if (hasSource && carrier is ("csharp-module" or "python-module") && new[] { Property(script, "inputs"), Property(script, "outputs") }
            .Where(x => x is { ValueKind: JsonValueKind.Array }).SelectMany(x => x!.Value.EnumerateArray()).Any(x => String(x, "type") == "bool"))
            issues.Add(new("BOOL_SOURCE_COMPATIBILITY_REQUIRED", "VM 4.4 没有原生 bool 脚本端口。显式源码的输入/输出请声明为 int，用 0/1 表示 false/true；C# 输入用 PortName != 0，输出用 condition ? 1 : 0。只有确定性 operations 脚本可以直接声明 bool，编译器会自动转换。", path));
        ValidateOperations(Property(script, "operations"), script, path + ".operations", issues);
        ValidateDependencies(Property(script, "dependencies"), carrier, path + ".dependencies", issues);
    }

    private static void ValidateExecution(JsonElement? execution, string path, List<ValidationIssue> issues)
    {
        Require(execution is { ValueKind: JsonValueKind.Object }, "REQUIREMENT_SCHEMA_INVALID", "execution must be an object.", path, issues);
        if (execution is not { ValueKind: JsonValueKind.Object }) return;
        CheckOnly(execution.Value, ["mode", "order"], path, issues);
        Require(ExecutionModes.Contains(String(execution.Value, "mode") ?? ""), "REQUIREMENT_SCHEMA_INVALID", "execution.mode is invalid.", path + ".mode", issues);
        if (Property(execution.Value, "order") is { } order) Require(order.ValueKind == JsonValueKind.Number && order.TryGetInt32(out var value) && value >= 0, "REQUIREMENT_SCHEMA_INVALID", "execution.order must be a non-negative integer.", path + ".order", issues);
    }

    private static void ValidatePorts(JsonElement? ports, string path, string? carrier, List<ValidationIssue> issues)
    {
        Require(ports is { ValueKind: JsonValueKind.Array }, "REQUIREMENT_SCHEMA_INVALID", "IO collection must be an array.", path, issues);
        if (ports is not { ValueKind: JsonValueKind.Array }) return;
        var names = new HashSet<string>(StringComparer.Ordinal);
        foreach (var (port, index) in ports.Value.EnumerateArray().Select((x, i) => (x, i)))
        {
            var itemPath = $"{path}[{index}]";
            Require(port.ValueKind == JsonValueKind.Object, "REQUIREMENT_SCHEMA_INVALID", "IO item must be an object.", itemPath, issues);
            if (port.ValueKind != JsonValueKind.Object) continue;
            CheckOnly(port, ["name", "type", "default", "required", "description"], itemPath, issues);
            var name = String(port, "name");
            Require(!string.IsNullOrWhiteSpace(name), "REQUIREMENT_SCHEMA_INVALID", "IO name is required.", itemPath + ".name", issues);
            if (carrier is "csharp-module" or "python-module") Require(IsIdentifier(name), "IDENTIFIER_INVALID", "Module IO name must be a script identifier.", itemPath + ".name", issues);
            if (name is not null && !names.Add(name)) issues.Add(new("DUPLICATE_IO_NAME", "IO name must be unique within its collection.", itemPath + ".name"));
            var type = String(port, "type");
            Require(type is not null && Types.Contains(type), "REQUIREMENT_SCHEMA_INVALID", "IO type is invalid.", itemPath + ".type", issues);
            if (carrier == "csharp-module" && type is not null && !CSharpPortTypes.Contains(type))
                issues.Add(new("CSHARP_PORT_TYPE_UNSUPPORTED", "VisionMaster 4.4 的 ShellModule 输入/输出类型列表没有 Circle。CircleData[] 只能通过 GetVarCircle/SetVarCircle 变量操作使用，不能声明为脚本端口。", itemPath + ".type"));
            if (carrier == "python-module" && type is not null && !ConfirmedPythonPortTypes.Contains(type))
                issues.Add(new("PYTHON_COMPLEX_TYPE_UNCONFIRMED", "Project-local VM 4.4 samples do not confirm Python IoHelper semantics for this port type; use csharp-module or provide a validated Python sample before enabling it.", itemPath + ".type"));
            if (type is not null && Property(port, "default") is { } defaultValue && !DefaultMatches(type, defaultValue)) issues.Add(new("IO_TYPE_MISMATCH", "IO default value does not match declared type.", itemPath + ".default"));
            if (Property(port, "required") is { } required) Require(required.ValueKind is JsonValueKind.True or JsonValueKind.False, "REQUIREMENT_SCHEMA_INVALID", "required must be boolean.", itemPath + ".required", issues);
            if (Property(port, "description") is { } description) Require(description.ValueKind == JsonValueKind.String, "REQUIREMENT_SCHEMA_INVALID", "description must be string.", itemPath + ".description", issues);
        }
    }

    private static void ValidateOperations(JsonElement? operations, JsonElement script, string path, List<ValidationIssue> issues)
    {
        if (operations is null) return;
        Require(operations is { ValueKind: JsonValueKind.Array }, "REQUIREMENT_SCHEMA_INVALID", "operations must be an array.", path, issues);
        if (operations is not { ValueKind: JsonValueKind.Array }) return;
        var carrier = String(script, "carrier");
        var hasSource = Property(script, "source") is { ValueKind: JsonValueKind.String };
        var inputTypes = Property(script, "inputs") is { ValueKind: JsonValueKind.Array } inputArray
            ? inputArray.EnumerateArray().Where(x => String(x, "name") is not null && String(x, "type") is not null).ToDictionary(x => String(x, "name")!, x => String(x, "type")!, StringComparer.Ordinal) : [];
        var outputTypes = Property(script, "outputs") is { ValueKind: JsonValueKind.Array } outputArray
            ? outputArray.EnumerateArray().Where(x => String(x, "name") is not null && String(x, "type") is not null).ToDictionary(x => String(x, "name")!, x => String(x, "type")!, StringComparer.Ordinal) : [];
        var inputs = inputTypes.Keys.ToHashSet(StringComparer.Ordinal);
        var outputs = outputTypes.Keys.ToHashSet(StringComparer.Ordinal);
        var results = new HashSet<string>(StringComparer.Ordinal);
        var resultTypes = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var (operation, index) in operations.Value.EnumerateArray().Select((x, i) => (x, i)))
        {
            var itemPath = $"{path}[{index}]";
            Require(operation.ValueKind == JsonValueKind.Object, "REQUIREMENT_SCHEMA_INVALID", "Operation must be an object.", itemPath, issues);
            if (operation.ValueKind != JsonValueKind.Object) continue;
            CheckOnly(operation, ["kind", "procedure", "module", "parameter", "result", "valueType", "value", "condition", "deviceId", "addressId", "milliseconds", "dataType", "pathInput", "widthInput", "heightInput", "imageOutput", "successOutput", "errorOutput", "entityCountOutput", "renderedCountOutput", "when", "onError"], itemPath, issues);
            Require(OperationKinds.Contains(String(operation, "kind") ?? ""), "REQUIREMENT_SCHEMA_INVALID", "Operation kind is invalid.", itemPath + ".kind", issues);
            var kind = String(operation, "kind");
            var parameter = String(operation, "parameter");
            var result = String(operation, "result");
            var valueType = String(operation, "valueType");
            if (Property(operation, "valueType") is not null) Require(valueType is not null && Types.Contains(valueType), "REQUIREMENT_SCHEMA_INVALID", "Operation valueType is invalid.", itemPath + ".valueType", issues);
            if (kind == "setOutput")
            {
                Require(parameter is not null && outputs.Contains(parameter), "OUTPUT_NOT_FOUND", "setOutput parameter must name a declared output.", itemPath + ".parameter", issues);
                if (Property(operation, "value") is { } expression)
                {
                    ValidateExpression(expression, inputs, results, itemPath + ".value", 0, issues);
                    var actual = InferExpressionType(expression, inputTypes, resultTypes);
                    if (parameter is not null && outputTypes.TryGetValue(parameter, out var expected) && actual is not null && !TypesCompatible(expected, actual))
                        issues.Add(new("EXPRESSION_TYPE_MISMATCH", $"Output {parameter} expects {expected}, expression is {actual}.", itemPath + ".value"));
                }
                else issues.Add(new("OPERATION_FIELD_REQUIRED", "setOutput requires value.", itemPath + ".value"));
            }
            if (kind is "getModule" or "setModuleValue" or "getModuleValue" or "getModuleArray" or "getModuleParam")
            {
                Require(!string.IsNullOrWhiteSpace(String(operation, "module")), "OPERATION_FIELD_REQUIRED", kind + " requires module.", itemPath + ".module", issues);
                if (kind != "getModule") Require(!string.IsNullOrWhiteSpace(parameter), "OPERATION_FIELD_REQUIRED", kind + " requires parameter.", itemPath + ".parameter", issues);
            }
            if (kind is "getModule" or "getModuleValue" or "getModuleArray" or "getModuleParam" or "getGlobalVariable" or "getLocalVariable" or "bytesToPointset")
                Require(!string.IsNullOrWhiteSpace(result), "OPERATION_FIELD_REQUIRED", kind + " requires result.", itemPath + ".result", issues);
            if (kind is "setModuleValue" or "setGlobalVariable" or "setLocalVariable")
            {
                if (Property(operation, "value") is { } expression) ValidateExpression(expression, inputs, results, itemPath + ".value", 0, issues);
                else issues.Add(new("OPERATION_FIELD_REQUIRED", kind + " requires value.", itemPath + ".value"));
            }
            if (kind is "getGlobalVariable" or "setGlobalVariable" or "getLocalVariable" or "setLocalVariable")
                Require(!string.IsNullOrWhiteSpace(parameter), "OPERATION_FIELD_REQUIRED", kind + " requires parameter.", itemPath + ".parameter", issues);
            if (!hasSource && carrier == "csharp-module" && kind is ("getGlobalVariable" or "setGlobalVariable" or "getLocalVariable" or "setLocalVariable"))
                Require(valueType is not null, "OPERATION_FIELD_REQUIRED", kind + " requires valueType for typed C# generation.", itemPath + ".valueType", issues);
            if (!hasSource && carrier == "global-csharp" && kind is ("getGlobalVariable" or "setGlobalVariable"))
                Require(valueType is "int" or "float" or "string", "OPERATION_NOT_SUPPORTED_BY_CARRIER", "GlobalScript variables support int, float and string.", itemPath + ".valueType", issues);
            if (!hasSource && carrier == "python-module" && valueType is not null && valueType is not ("bool" or "int" or "int[]" or "float" or "float[]" or "string" or "string[]" or "byte"))
                issues.Add(new("PYTHON_COMPLEX_TYPE_UNCONFIRMED", "Python IoHelper complex-type behavior is not confirmed by project samples.", itemPath + ".valueType"));
            if (kind is "runProcedure" or "continuousProcedure" or "stopProcedure") Require(!string.IsNullOrWhiteSpace(String(operation, "procedure")), "OPERATION_FIELD_REQUIRED", kind + " requires procedure.", itemPath + ".procedure", issues);
            if (kind == "setProcedureInput")
            {
                Require(!string.IsNullOrWhiteSpace(String(operation, "procedure")), "OPERATION_FIELD_REQUIRED", "setProcedureInput requires procedure.", itemPath + ".procedure", issues);
                Require(!string.IsNullOrWhiteSpace(parameter), "OPERATION_FIELD_REQUIRED", "setProcedureInput requires parameter.", itemPath + ".parameter", issues);
                Require(valueType is "int" or "float" or "string", "OPERATION_NOT_SUPPORTED_BY_CARRIER", "setProcedureInput supports int, float and string.", itemPath + ".valueType", issues);
            }
            if (kind is "setContinuousInterval" or "sleep") Require(Int(operation, "milliseconds") is >= 0, "OPERATION_FIELD_REQUIRED", kind + " requires non-negative milliseconds.", itemPath + ".milliseconds", issues);
            if (kind is "log" or "sendCommunication" or "bytesToPointset" or "setProcedureInput" or "saveSolution" or "loadSolution")
            {
                if (Property(operation, "value") is { } expression) ValidateExpression(expression, inputs, results, itemPath + ".value", 0, issues);
                else issues.Add(new("OPERATION_FIELD_REQUIRED", kind + " requires value.", itemPath + ".value"));
            }
            if (kind == "sendCommunication")
            {
                Require(Int(operation, "deviceId") is >= 0, "OPERATION_FIELD_REQUIRED", "sendCommunication requires deviceId.", itemPath + ".deviceId", issues);
                Require(String(operation, "dataType") is null or "string" or "int" or "float" or "byte", "REQUIREMENT_SCHEMA_INVALID", "sendCommunication dataType is invalid.", itemPath + ".dataType", issues);
            }
            if (kind == "dxfRender")
            {
                Require(carrier == "csharp-module", "OPERATION_NOT_SUPPORTED_BY_CARRIER", "dxfRender requires csharp-module.", itemPath + ".kind", issues);
                Require(!hasSource, "SOURCE_OPERATIONS_CONFLICT", "dxfRender is deterministic and cannot be combined with explicit source.", itemPath, issues);
                RequirePort(operation, "pathInput", "string", inputTypes, itemPath, issues);
                RequirePort(operation, "widthInput", "int", inputTypes, itemPath, issues);
                RequirePort(operation, "heightInput", "int", inputTypes, itemPath, issues);
                RequirePort(operation, "imageOutput", "image", outputTypes, itemPath, issues);
                RequirePort(operation, "successOutput", "bool", outputTypes, itemPath, issues);
                RequirePort(operation, "errorOutput", "string", outputTypes, itemPath, issues);
                RequirePort(operation, "entityCountOutput", "int", outputTypes, itemPath, issues);
                RequirePort(operation, "renderedCountOutput", "int", outputTypes, itemPath, issues);
                Require(InputDefaultInt(script, String(operation, "widthInput")) == 1920, "DXF_RENDER_DEFAULT_SIZE_INVALID", "dxfRender width input default must be 1920.", itemPath + ".widthInput", issues);
                Require(InputDefaultInt(script, String(operation, "heightInput")) == 1080, "DXF_RENDER_DEFAULT_SIZE_INVALID", "dxfRender height input default must be 1080.", itemPath + ".heightInput", issues);
                var hasNetDxf = Property(script, "dependencies") is { ValueKind: JsonValueKind.Array } dependencyArray &&
                    dependencyArray.EnumerateArray().Any(x => string.Equals(String(x, "name"), "netDxf.dll", StringComparison.OrdinalIgnoreCase) || string.Equals(String(x, "name"), "netDxf", StringComparison.OrdinalIgnoreCase));
                Require(hasNetDxf, "DXF_RENDER_DEPENDENCY_REQUIRED", "dxfRender requires a declared netDxf.dll dependency.", itemPath, issues);
            }
            if (Property(operation, "condition") is { } condition) ValidateExpression(condition, inputs, results, itemPath + ".condition", 0, issues);
            if (Property(operation, "condition") is { } typedCondition && InferExpressionType(typedCondition, inputTypes, resultTypes) is { } conditionType && conditionType != "bool")
                issues.Add(new("EXPRESSION_TYPE_MISMATCH", "Operation condition must be boolean; actual type is " + conditionType + ".", itemPath + ".condition"));
            if (!hasSource)
            {
                var supported = carrier switch {
                    "csharp-module" => kind is "getModule" or "setOutput" or "setModuleValue" or "getModuleValue" or "getModuleArray" or "getModuleParam" or "getGlobalVariable" or "setGlobalVariable" or "getLocalVariable" or "setLocalVariable" or "log" or "sleep" or "bytesToPointset" or "dxfRender",
                    "python-module" => kind is "setOutput" or "getGlobalVariable" or "setGlobalVariable" or "getLocalVariable" or "setLocalVariable" or "log" or "sleep",
                    "global-csharp" => kind is "runProcedure" or "continuousProcedure" or "stopProcedure" or "setContinuousInterval" or "startGlobalCommunication" or "sendCommunication" or "setProcedureInput" or "saveSolution" or "loadSolution" or "getGlobalVariable" or "setGlobalVariable" or "log" or "sleep",
                    _ => false
                };
                Require(supported, "OPERATION_NOT_SUPPORTED_BY_CARRIER", kind + " is not supported by the deterministic generator for " + carrier + ".", itemPath + ".kind", issues);
                Require(Property(operation, "when") is null, "CONDITION_IR_REQUIRED", "Use the structured condition expression instead of when text.", itemPath + ".when", issues);
            }
            var onError = String(operation, "onError");
            Require(onError is null or "fail" or "continue" or "set-output-failed", "REQUIREMENT_SCHEMA_INVALID", "onError is invalid.", itemPath + ".onError", issues);
            if (!hasSource && onError is "continue" or "set-output-failed")
                issues.Add(new("ON_ERROR_POLICY_UNSUPPORTED", "The deterministic generator currently supports only onError=fail; use explicit source to implement carrier-specific recovery semantics.", itemPath + ".onError"));
            if (result is not null)
            {
                Require(Regex.IsMatch(result, "^[A-Za-z_][A-Za-z0-9_]*$"), "IDENTIFIER_INVALID", "Operation result has an invalid identifier.", itemPath + ".result", issues);
                Require(!inputs.Contains(result) && !outputs.Contains(result), "RESULT_NAME_CONFLICT", "Operation result cannot use an input or output name.", itemPath + ".result", issues);
                if (!results.Add(result)) issues.Add(new("DUPLICATE_RESULT", "Operation result name must be unique and declared before use.", itemPath + ".result"));
                if (valueType is not null) resultTypes[result] = valueType;
            }
        }
    }

    private static void RequirePort(JsonElement operation, string field, string expectedType, Dictionary<string, string> ports, string path, List<ValidationIssue> issues)
    {
        var name = String(operation, field);
        Require(!string.IsNullOrWhiteSpace(name), "OPERATION_FIELD_REQUIRED", "dxfRender requires " + field + ".", path + "." + field, issues);
        if (!string.IsNullOrWhiteSpace(name))
            Require(ports.TryGetValue(name, out var actual) && actual == expectedType, "DXF_RENDER_PORT_INVALID", field + " must reference a declared " + expectedType + " port.", path + "." + field, issues);
    }

    private static int? InputDefaultInt(JsonElement script, string? name)
    {
        if (name is null || Property(script, "inputs") is not { ValueKind: JsonValueKind.Array } inputs) return null;
        foreach (var input in inputs.EnumerateArray())
            if (String(input, "name") == name && Property(input, "default") is { ValueKind: JsonValueKind.Number } value && value.TryGetInt32(out var result)) return result;
        return null;
    }

    private static string? InferExpressionType(JsonElement expression, Dictionary<string, string> inputs, Dictionary<string, string> results)
    {
        if (expression.ValueKind == JsonValueKind.String) return "string";
        if (expression.ValueKind is JsonValueKind.True or JsonValueKind.False) return "bool";
        if (expression.ValueKind == JsonValueKind.Number) return expression.TryGetInt32(out _) ? "int" : "float";
        if (expression.ValueKind == JsonValueKind.Array)
        {
            var items = expression.EnumerateArray().Select(x => InferExpressionType(x, inputs, results)).Distinct().ToArray();
            return items.Length == 1 && items[0] is "int" or "float" or "string" ? items[0] + "[]" : null;
        }
        if (expression.ValueKind != JsonValueKind.Object || !expression.TryGetProperty("kind", out var kindElement)) return null;
        return kindElement.GetString() switch
        {
            "input" when Property(expression, "name") is { ValueKind: JsonValueKind.String } name && inputs.TryGetValue(name.GetString()!, out var inputType) => inputType,
            "result" when Property(expression, "name") is { ValueKind: JsonValueKind.String } name && results.TryGetValue(name.GetString()!, out var resultType) => resultType,
            "literal" when Property(expression, "value") is { } value => InferExpressionType(value, inputs, results),
            "unary" when String(expression, "operator") == "not" => "bool",
            "unary" when Property(expression, "value") is { } value => InferExpressionType(value, inputs, results),
            "index" when Property(expression, "target") is { } target => InferExpressionType(target, inputs, results) is { } type && type.EndsWith("[]", StringComparison.Ordinal) ? type[..^2] : null,
            "binary" when String(expression, "operator") is "equal" or "notEqual" or "greaterThan" or "greaterOrEqual" or "lessThan" or "lessOrEqual" or "and" or "or" => "bool",
            "binary" when Property(expression, "left") is { } left && Property(expression, "right") is { } right => Promote(InferExpressionType(left, inputs, results), InferExpressionType(right, inputs, results), String(expression, "operator")),
            _ => null
        };
    }

    private static string? Promote(string? left, string? right, string? operation) =>
        operation == "add" && left == "string" && right == "string" ? "string" :
        left == "float" && right is "float" or "int" || right == "float" && left is "float" or "int" ? "float" :
        left == "int" && right == "int" ? "int" : null;

    private static bool TypesCompatible(string expected, string actual) => expected == actual || expected == "float" && actual == "int";

    private static void ValidateExpression(JsonElement expression, HashSet<string> inputs, HashSet<string> results, string path, int depth, List<ValidationIssue> issues)
    {
        if (depth > 16) { issues.Add(new("EXPRESSION_INVALID", "Expression nesting exceeds 16.", path)); return; }
        if (expression.ValueKind is JsonValueKind.String or JsonValueKind.Number or JsonValueKind.True or JsonValueKind.False or JsonValueKind.Null) return;
        Require(expression.ValueKind == JsonValueKind.Object, "EXPRESSION_INVALID", "Expression must be a literal or expression object.", path, issues);
        if (expression.ValueKind != JsonValueKind.Object) return;
        CheckOnly(expression, ["kind", "name", "value", "operator", "left", "right", "target", "index"], path, issues);
        var kind = String(expression, "kind");
        Require(kind is "input" or "result" or "literal" or "binary" or "unary" or "index", "EXPRESSION_INVALID", "Expression kind is invalid.", path + ".kind", issues);
        if (kind == "input") Require(String(expression, "name") is { } name && inputs.Contains(name), "INPUT_NOT_FOUND", "Expression input is not declared.", path + ".name", issues);
        if (kind == "result") Require(String(expression, "name") is { } result && results.Contains(result), "RESULT_NOT_FOUND", "Result expression must reference an earlier operation result.", path + ".name", issues);
        if (kind == "literal") Require(Property(expression, "value") is not null, "EXPRESSION_INVALID", "Literal expression requires value.", path + ".value", issues);
        if (kind == "binary")
        {
            Require(String(expression, "operator") is "add" or "subtract" or "multiply" or "divide" or "modulo" or "equal" or "notEqual" or "greaterThan" or "greaterOrEqual" or "lessThan" or "lessOrEqual" or "and" or "or", "EXPRESSION_INVALID", "Binary operator is invalid.", path + ".operator", issues);
            if (Property(expression, "left") is { } left) ValidateExpression(left, inputs, results, path + ".left", depth + 1, issues); else issues.Add(new("EXPRESSION_INVALID", "Binary expression requires left.", path + ".left"));
            if (Property(expression, "right") is { } right) ValidateExpression(right, inputs, results, path + ".right", depth + 1, issues); else issues.Add(new("EXPRESSION_INVALID", "Binary expression requires right.", path + ".right"));
        }
        if (kind == "unary")
        {
            Require(String(expression, "operator") is "not" or "negate", "EXPRESSION_INVALID", "Unary operator is invalid.", path + ".operator", issues);
            if (Property(expression, "value") is { } value) ValidateExpression(value, inputs, results, path + ".value", depth + 1, issues); else issues.Add(new("EXPRESSION_INVALID", "Unary expression requires value.", path + ".value"));
        }
        if (kind == "index")
        {
            if (Property(expression, "target") is { } target) ValidateExpression(target, inputs, results, path + ".target", depth + 1, issues); else issues.Add(new("EXPRESSION_INVALID", "Index expression requires target.", path + ".target"));
            if (Property(expression, "index") is { } index) ValidateExpression(index, inputs, results, path + ".index", depth + 1, issues); else issues.Add(new("EXPRESSION_INVALID", "Index expression requires index.", path + ".index"));
        }
    }

    private static void ValidateDependencies(JsonElement? dependencies, string? carrier, string path, List<ValidationIssue> issues)
    {
        if (dependencies is null) return;
        Require(dependencies is { ValueKind: JsonValueKind.Array }, "REQUIREMENT_SCHEMA_INVALID", "dependencies must be an array.", path, issues);
        if (dependencies is not { ValueKind: JsonValueKind.Array }) return;
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var (dependency, index) in dependencies.Value.EnumerateArray().Select((x, i) => (x, i)))
        {
            var itemPath = $"{path}[{index}]";
            Require(dependency.ValueKind == JsonValueKind.Object, "REQUIREMENT_SCHEMA_INVALID", "Dependency must be an object.", itemPath, issues);
            if (dependency.ValueKind != JsonValueKind.Object) continue;
            CheckOnly(dependency, ["kind", "name", "role", "version", "path", "architecture", "referenceType"], itemPath, issues);
            var kind = String(dependency, "kind");
            Require(kind is "dotnet-assembly" or "python-package", "REQUIREMENT_SCHEMA_INVALID", "Dependency kind is invalid.", itemPath + ".kind", issues);
            Require(carrier == "python-module" ? kind == "python-package" : kind == "dotnet-assembly", "DEPENDENCY_CARRIER_MISMATCH", "Dependency kind does not match the script carrier.", itemPath + ".kind", issues);
            Require(!string.IsNullOrWhiteSpace(String(dependency, "name")), "REQUIREMENT_SCHEMA_INVALID", "Dependency name is required.", itemPath + ".name", issues);
            if (String(dependency, "name") is { } name && !names.Add(name)) issues.Add(new("DUPLICATE_DEPENDENCY", "Dependency names must be unique within a script.", itemPath + ".name"));
            if (carrier == "global-csharp") issues.Add(new("REFERENCE_TYPE_UNCONFIRMED", "GlobalScript custom dependencies require a separately verified ScriptRefences mapping.", itemPath));
            var architecture = String(dependency, "architecture");
            Require(architecture is null or "x64" or "anycpu" or "pure-python", "REQUIREMENT_SCHEMA_INVALID", "Dependency architecture is invalid.", itemPath + ".architecture", issues);
            var role = String(dependency, "role");
            Require(role is null or "system" or "vm-sdk" or "operator-sdk" or "third-party", "REQUIREMENT_SCHEMA_INVALID", "Dependency role is invalid.", itemPath + ".role", issues);
            if (kind == "python-package") Require(role is null, "DEPENDENCY_ROLE_INVALID", "Python packages do not use a .NET assembly role.", itemPath + ".role", issues);
            var referenceType = Int(dependency, "referenceType");
            Require(referenceType is null or 0 or 3 or 4 or 6, "REQUIREMENT_SCHEMA_INVALID", "ShellRefrences referenceType is invalid.", itemPath + ".referenceType", issues);
            if (kind == "python-package") Require(referenceType is null, "DEPENDENCY_REFERENCE_TYPE_INVALID", "Python packages do not use ShellRefrences referenceType.", itemPath + ".referenceType", issues);
            if (kind == "dotnet-assembly" && referenceType is not null && referenceType != 4)
                Require(string.IsNullOrWhiteSpace(String(dependency, "path")), "DEPENDENCY_REFERENCE_TYPE_INVALID", "An explicit external DLL path can only use the VM-verified user-assembly referenceType 4.", itemPath + ".referenceType", issues);
        }
    }

    private static JsonElement? Property(JsonElement element, string name) => element.ValueKind == JsonValueKind.Object && element.TryGetProperty(name, out var value) ? value : null;
    private static bool DefaultMatches(string type, JsonElement value) => value.ValueKind == JsonValueKind.Null || type switch
    {
        "bool" => value.ValueKind is JsonValueKind.True or JsonValueKind.False,
        "int" => value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out _),
        "float" => value.ValueKind == JsonValueKind.Number,
        "string" => value.ValueKind == JsonValueKind.String,
        "int[]" => ArrayMatches(value, x => x.ValueKind == JsonValueKind.Number && x.TryGetInt32(out _)),
        "float[]" => ArrayMatches(value, x => x.ValueKind == JsonValueKind.Number),
        "string[]" => ArrayMatches(value, x => x.ValueKind == JsonValueKind.String),
        "byte" => ArrayMatches(value, x => x.ValueKind == JsonValueKind.Number && x.TryGetByte(out _)),
        "image" or "roibox" or "roibox[]" or "roiannulus" or "roipolygon" or "point" or "line" or "fixture" or "circle" or "rect" or "ellipse" or "pointset" => value.ValueKind is JsonValueKind.Object or JsonValueKind.Array,
        _ => false
    };
    private static bool ArrayMatches(JsonElement value, Func<JsonElement, bool> predicate) => value.ValueKind == JsonValueKind.Array && value.EnumerateArray().All(predicate);
    private static string? String(JsonElement element, string name) => Property(element, name) is { ValueKind: JsonValueKind.String } value ? value.GetString() : null;
    private static int? Int(JsonElement element, string name) => Property(element, name) is { ValueKind: JsonValueKind.Number } value && value.TryGetInt32(out var result) ? result : null;
    private static bool IsIdentifier(string? value) => !string.IsNullOrWhiteSpace(value) && !char.IsDigit(value[0]) && value.All(x => char.IsLetterOrDigit(x) || x == '_');
    private static bool IsVmDisplayName(string? value) => !string.IsNullOrWhiteSpace(value) && value == value.Trim() && !value.Contains('.') && value.All(x => !char.IsControl(x));
    private static void CheckOnly(JsonElement element, IEnumerable<string> allowed, string path, List<ValidationIssue> issues) { var set = allowed.ToHashSet(StringComparer.Ordinal); foreach (var property in element.EnumerateObject()) if (!set.Contains(property.Name)) issues.Add(new("REQUIREMENT_SCHEMA_INVALID", "Unsupported property: " + property.Name, path + "." + property.Name)); }
    private static void Require(bool condition, string code, string message, string path, List<ValidationIssue> issues) { if (!condition) issues.Add(new(code, message, path)); }
}
