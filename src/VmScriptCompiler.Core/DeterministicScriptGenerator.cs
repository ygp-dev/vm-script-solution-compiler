using System.Globalization;
using System.Text;
using System.Text.Json;

namespace VmScriptCompiler.Core;

public static class DeterministicScriptGenerator
{
    public static string Generate(ScriptRequirement script) => script.Carrier switch
    {
        "csharp-module" => CSharpModule(script),
        "python-module" => PythonModule(script),
        "global-csharp" => GlobalCSharp(script),
        _ => throw new CompilerException("SCRIPT_CARRIER_UNSUPPORTED", "Unsupported script carrier: " + script.Carrier)
    };

    private static string CSharpModule(ScriptRequirement script)
    {
        var body = new StringBuilder();
        foreach (var operation in script.Operations) body.AppendLine("            " + WithCSharpCondition(script, operation, CSharpModuleOperation(script, operation)));
        var initBody = script.Execution.Mode == "init" ? body.ToString() : "";
        var processBody = script.Execution.Mode == "init" ? "" : body.ToString();
        return "using System;\nusing System.Globalization;\nusing System.Threading;\nusing Script.Methods;\n\npublic partial class UserScript : ScriptMethods, IProcessMethods\n{\n    public void Init()\n    {\n        try\n        {\n" + initBody + "        }\n        catch (Exception error)\n        {\n            ConsoleWrite(error.ToString());\n        }\n    }\n\n    public bool Process()\n    {\n        try\n        {\n" + processBody + "            return true;\n        }\n        catch (Exception error)\n        {\n            ConsoleWrite(error.ToString());\n            return false;\n        }\n    }\n}\n";
    }

    private static string PythonModule(ScriptRequirement script)
    {
        var body = new StringBuilder();
        foreach (var operation in script.Operations) body.AppendLine("        " + WithPythonCondition(script, operation, PythonOperation(script, operation)));
        return "# coding: utf-8\nimport time\nfrom ioHelper import *\n\ndef Process(data) -> int:\n    moduleVar = IoHelper(data, INIT_MODULE_VAR)\n    globalVar = IoHelper(data, INIT_GLOBAL_VAR)\n    localVar = IoHelper(data, INIT_LOCAL_VAR)\n    try:\n" + (body.Length == 0 ? "        pass\n" : body.ToString()) + "    except BaseException as error:\n        PrintMsg(error)\n        return -1\n    return 0\n";
    }

    private static string GlobalCSharp(ScriptRequirement script)
    {
        var body = new StringBuilder();
        foreach (var operation in script.Operations) body.AppendLine("            " + WithCSharpCondition(operation, GlobalOperation(operation)));
        var initBody = script.Execution.Mode == "init" ? body.ToString() : "";
        var processBody = script.Execution.Mode == "init" ? "" : body.ToString();
        return "using System;\nusing System.Globalization;\nusing VM.GlobalScript.Methods;\nusing VM.Core;\nusing VM.PlatformSDKCS;\n\npublic class UserGlobalScript : UserGlobalMethods, IScriptMethods\n{\n    public int Init()\n    {\n        try\n        {\n            int initResult = InitSDK();\n            if (initResult != 0) return initResult;\n" + initBody + "            return 0;\n        }\n        catch (Exception error)\n        {\n            ConsoleWrite(error.ToString());\n            return -1;\n        }\n    }\n\n    public int Process()\n    {\n        try\n        {\n" + processBody + "            return 0;\n        }\n        catch (Exception error)\n        {\n            ConsoleWrite(error.ToString());\n            return -1;\n        }\n    }\n}\n";
    }

    private static string CSharpModuleOperation(ScriptRequirement script, OperationRequirement operation) => operation.Kind switch
    {
        "getModule" => "ModuleBase " + Identifier(operation.Result, "result") + " = CurrentProcess.GetModule(" + Cs(operation.Module) + ");",
        "setOutput" => CSharpSetOutput(script, operation),
        "setModuleValue" => "CurrentProcess.GetModule(" + Cs(operation.Module) + ").SetValue(" + Cs(operation.Parameter) + ", Convert.ToString(" + CSharpExpression(script, operation.Value) + ", CultureInfo.InvariantCulture));",
        "getModuleValue" => CSharpModuleGet(operation, false),
        "getModuleArray" => CSharpModuleGet(operation, true),
        "getModuleParam" => "string " + Identifier(operation.Result, "result") + " = string.Empty; CurrentProcess.GetModule(" + Cs(operation.Module) + ").GetParamValue(" + Cs(operation.Parameter) + ", ref " + Identifier(operation.Result, "result") + ");",
        "getGlobalVariable" => CSharpVariableGet("GlobalVariableModule", operation),
        "setGlobalVariable" => CSharpVariableSet("GlobalVariableModule", operation, script),
        "getLocalVariable" => CSharpVariableGet("LocalVariable", operation),
        "setLocalVariable" => CSharpVariableSet("LocalVariable", operation, script),
        "log" => "ConsoleWrite(Convert.ToString(" + CSharpExpression(script, operation.Value) + ", CultureInfo.InvariantCulture));",
        "sleep" => "Sleep(" + operation.Milliseconds!.Value.ToString(CultureInfo.InvariantCulture) + ");",
        "bytesToPointset" => "ContourPointData[] " + Identifier(operation.Result, "result") + " = null; BytesToPointset(" + CSharpExpression(script, operation.Value) + ", ref " + Identifier(operation.Result, "result") + ");",
        _ => throw new CompilerException("OPERATION_NOT_SUPPORTED_BY_CARRIER", operation.Kind + " is not supported by csharp-module.")
    };

    private static string CSharpSetOutput(ScriptRequirement script, OperationRequirement operation)
    {
        var name = Identifier(operation.Parameter, "output");
        var value = CSharpExpression(script, operation.Value);
        return script.Outputs.Single(x => x.Name == operation.Parameter).Type == "bool"
            ? name + " = (" + value + ") ? 1 : 0;"
            : name + " = " + value + ";";
    }

    private static string CSharpModuleGet(OperationRequirement operation, bool array)
    {
        var result = Identifier(operation.Result, "result");
        var access = "CurrentProcess.GetModule(" + Cs(operation.Module) + ")." + (array ? "GetArrayValue" : "GetValue") + "(" + Cs(operation.Parameter) + ")";
        return operation.ValueType switch
        {
            "int" => "int " + result + " = Convert.ToInt32(" + access + ", CultureInfo.InvariantCulture);",
            "float" => "float " + result + " = Convert.ToSingle(" + access + ", CultureInfo.InvariantCulture);",
            "string" => "string " + result + " = Convert.ToString(" + access + ", CultureInfo.InvariantCulture);",
            "bool" => "bool " + result + " = Convert.ToBoolean(" + access + ", CultureInfo.InvariantCulture);",
            null => "object " + result + " = " + access + ";",
            _ => CSharpType(operation.ValueType) + " " + result + " = " + access + " as " + CSharpType(operation.ValueType) + ";"
        };
    }

    private static string CSharpVariableGet(string owner, OperationRequirement operation)
    {
        var result = Identifier(operation.Result, "result");
        var name = Cs(operation.Parameter);
        return operation.ValueType switch
        {
            "int" => ScalarVariableGet(owner, name, result, "int", "GetVarInt", "0"),
            "int[]" => "int[] " + result + " = null; " + owner + ".GetVarInt(" + name + ", ref " + result + ");",
            "float" => ScalarVariableGet(owner, name, result, "float", "GetVarFloat", "0f"),
            "float[]" => "float[] " + result + " = null; " + owner + ".GetVarFloat(" + name + ", ref " + result + ");",
            "string" => ScalarVariableGet(owner, name, result, "string", "GetVarString", "string.Empty"),
            "string[]" => "string[] " + result + " = null; " + owner + ".GetVarString(" + name + ", ref " + result + ");",
            "byte" => "byte[] " + result + " = null; " + owner + ".GetVarByte(" + name + ", ref " + result + ");",
            "image" => "ImageData " + result + " = null; " + owner + ".GetVarImage(" + name + ", ref " + result + ");",
            "point" => GeometryGet(owner, operation, result, "PointData[]", "GetVarPoint"),
            "roibox" => ScalarGeometryGet(owner, operation, result, "RoiboxData", "GetVarBox"),
            "roibox[]" => GeometryGet(owner, operation, result, "RoiboxData[]", "GetVarBox"),
            "roiannulus" => GeometryGet(owner, operation, result, "AnnulusData[]", "GetVarAnnulus"),
            "roipolygon" => GeometryGet(owner, operation, result, "PolygonData[]", "GetVarPolygon"),
            "line" => GeometryGet(owner, operation, result, "LineData[]", "GetVarLine"),
            "fixture" => GeometryGet(owner, operation, result, "FixtureData[]", "GetVarFixture"),
            "circle" => GeometryGet(owner, operation, result, "CircleData[]", "GetVarCircle"),
            "rect" => GeometryGet(owner, operation, result, "RectData[]", "GetVarRect"),
            "ellipse" => GeometryGet(owner, operation, result, "EllipseData[]", "GetVarEllipse"),
            "pointset" => "byte[] " + result + " = null; " + owner + ".GetVarPointset(" + name + ", ref " + result + ");",
            _ => "object " + result + " = " + owner + ".GetValue(" + name + ");"
        };
    }

    private static string GeometryGet(string owner, OperationRequirement operation, string result, string csharpType, string method) =>
        csharpType + " " + result + " = null; " + owner + "." + method + "(" + Cs(operation.Parameter) + ", ref " + result + ");";

    private static string CSharpVariableSet(string owner, OperationRequirement operation, ScriptRequirement script)
    {
        var expression = CSharpExpression(script, operation.Value);
        var name = Cs(operation.Parameter);
        return operation.ValueType switch
        {
            "int" => owner + ".SetVarInt(" + name + ", new int[] { Convert.ToInt32(" + expression + ", CultureInfo.InvariantCulture) });",
            "int[]" => owner + ".SetVarInt(" + name + ", " + expression + ");",
            "float" => owner + ".SetVarFloat(" + name + ", new float[] { Convert.ToSingle(" + expression + ", CultureInfo.InvariantCulture) });",
            "float[]" => owner + ".SetVarFloat(" + name + ", " + expression + ");",
            "string" => owner + ".SetVarString(" + name + ", new string[] { Convert.ToString(" + expression + ", CultureInfo.InvariantCulture) });",
            "string[]" => owner + ".SetVarString(" + name + ", " + expression + ");",
            "byte" => owner + ".SetVarByte(" + name + ", " + expression + ");",
            "image" => owner + ".SetVarImage(" + name + ", " + expression + ");",
            "point" => owner + ".SetVarPoint(" + name + ", " + expression + ");",
            "roibox" => owner + ".SetVarBox(" + name + ", new RoiboxData[] { " + expression + " });",
            "roibox[]" => owner + ".SetVarBox(" + name + ", " + expression + ");",
            "roiannulus" => owner + ".SetVarAnnulus(" + name + ", " + expression + ");",
            "roipolygon" => owner + ".SetVarPolygon(" + name + ", " + expression + ");",
            "line" => owner + ".SetVarLine(" + name + ", " + expression + ");",
            "fixture" => owner + ".SetVarFixture(" + name + ", " + expression + ");",
            "circle" => owner + ".SetVarCircle(" + name + ", " + expression + ");",
            "rect" => owner + ".SetVarRect(" + name + ", " + expression + ");",
            "ellipse" => owner + ".SetVarEllipse(" + name + ", " + expression + ");",
            "pointset" => owner + ".SetVarPointset(" + name + ", " + expression + ");",
            _ => owner + ".SetValue(" + name + ", Convert.ToString(" + expression + ", CultureInfo.InvariantCulture));"
        };
    }

    private static string PythonOperation(ScriptRequirement script, OperationRequirement operation) => operation.Kind switch
    {
        "setOutput" => PythonSetOutput(script, operation),
        "getGlobalVariable" => Identifier(operation.Result, "result") + " = globalVar.GetValue(" + Py(operation.Parameter) + ")",
        "setGlobalVariable" => "globalVar.SetValue(" + Py(operation.Parameter) + ", " + PythonExpression(script, operation.Value) + ")",
        "getLocalVariable" => Identifier(operation.Result, "result") + " = localVar.GetValue(" + Py(operation.Parameter) + ")",
        "setLocalVariable" => "localVar.SetValue(" + Py(operation.Parameter) + ", " + PythonExpression(script, operation.Value) + ")",
        "log" => "PrintMsg(" + PythonExpression(script, operation.Value) + ")",
        "sleep" => "time.sleep(" + (operation.Milliseconds!.Value / 1000d).ToString("R", CultureInfo.InvariantCulture) + ")",
        _ => throw new CompilerException("OPERATION_NOT_SUPPORTED_BY_CARRIER", operation.Kind + " is not supported by python-module.")
    };

    private static string PythonSetOutput(ScriptRequirement script, OperationRequirement operation)
    {
        var value = PythonExpression(script, operation.Value);
        return "moduleVar." + Identifier(operation.Parameter, "output") + " = " +
            (script.Outputs.Single(x => x.Name == operation.Parameter).Type == "bool" ? "(1 if " + value + " else 0)" : value);
    }

    private static string ScalarVariableGet(string owner, string name, string result, string type, string method, string fallback) =>
        type + "[] " + result + "Values = null; " + owner + "." + method + "(" + name + ", ref " + result + "Values); " + type + " " + result + " = " + result + "Values != null && " + result + "Values.Length > 0 ? " + result + "Values[0] : " + fallback + ";";

    private static string ScalarGeometryGet(string owner, OperationRequirement operation, string result, string type, string method) =>
        type + "[] " + result + "Values = null; " + owner + "." + method + "(" + Cs(operation.Parameter) + ", ref " + result + "Values); " + type + " " + result + " = " + result + "Values != null && " + result + "Values.Length > 0 ? " + result + "Values[0] : null;";

    private static string GlobalOperation(OperationRequirement operation) => operation.Kind switch
    {
        "runProcedure" => "ExecuteProcessOnce(" + Cs(operation.Procedure) + ");",
        "continuousProcedure" => "ContinuousExecuteProcess(" + Cs(operation.Procedure) + ");",
        "stopProcedure" => "StopProcessExecute(" + Cs(operation.Procedure) + ", " + (operation.Milliseconds ?? 500).ToString(CultureInfo.InvariantCulture) + "U);",
        "setContinuousInterval" => "SetScriptContinuousExecuteInterval(" + operation.Milliseconds!.Value.ToString(CultureInfo.InvariantCulture) + "U);",
        "startGlobalCommunication" => "StartGlobalCommunicate();",
        "sendCommunication" => GlobalSend(operation),
        "setProcedureInput" => GlobalSetProcedureInput(operation),
        "saveSolution" => "VmSolution.SaveAs(Convert.ToString(" + CSharpExpression(operation.Value) + ", CultureInfo.InvariantCulture));",
        "loadSolution" => "VmSolution.Load(Convert.ToString(" + CSharpExpression(operation.Value) + ", CultureInfo.InvariantCulture));",
        "setGlobalVariable" => GlobalSet(operation),
        "getGlobalVariable" => GlobalGet(operation),
        "log" => "ConsoleWrite(Convert.ToString(" + CSharpExpression(operation.Value) + ", CultureInfo.InvariantCulture));",
        "sleep" => "System.Threading.Thread.Sleep(" + operation.Milliseconds!.Value.ToString(CultureInfo.InvariantCulture) + ");",
        _ => throw new CompilerException("OPERATION_NOT_SUPPORTED_BY_CARRIER", operation.Kind + " is not supported by global-csharp.")
    };

    private static string GlobalSend(OperationRequirement operation)
    {
        var dataType = operation.DataType ?? "string";
        var enumName = dataType switch { "int" => "IntType", "float" => "FloatType", "byte" => "ByteType", _ => "StringType" };
        var value = CSharpExpression(operation.Value);
        if (dataType != "byte") value = "Convert.ToString(" + value + ", CultureInfo.InvariantCulture)";
        return "SendCommDeviceData(" + value + ", " + operation.DeviceId!.Value.ToString(CultureInfo.InvariantCulture) + ", " + (operation.AddressId ?? -1).ToString(CultureInfo.InvariantCulture) + ", VM.GlobalScript.Methods.DataType." + enumName + ");";
    }

    private static string GlobalSetProcedureInput(OperationRequirement operation)
    {
        var owner = "((VmProcedure)VmSolution.Instance[" + Cs(operation.Procedure) + "]).ModuParams";
        var value = CSharpExpression(operation.Value);
        return operation.ValueType switch
        {
            "int" => owner + ".SetInputInt(" + Cs(operation.Parameter) + ", new int[] { Convert.ToInt32(" + value + ", CultureInfo.InvariantCulture) });",
            "float" => owner + ".SetInputFloat(" + Cs(operation.Parameter) + ", new float[] { Convert.ToSingle(" + value + ", CultureInfo.InvariantCulture) });",
            "string" => owner + ".SetInputString(" + Cs(operation.Parameter) + ", new InputStringData[] { new InputStringData { strValue = Convert.ToString(" + value + ", CultureInfo.InvariantCulture) } });",
            _ => throw new CompilerException("OPERATION_NOT_SUPPORTED_BY_CARRIER", "setProcedureInput supports int, float and string deterministic values.")
        };
    }

    private static string GlobalSet(OperationRequirement operation) => operation.ValueType switch
    {
        "int" => "SetGlobalVariableIntValue(" + Cs(operation.Parameter) + ", Convert.ToInt32(" + CSharpExpression(operation.Value) + ", CultureInfo.InvariantCulture));",
        "float" => "SetGlobalVariableFloatValue(" + Cs(operation.Parameter) + ", Convert.ToSingle(" + CSharpExpression(operation.Value) + ", CultureInfo.InvariantCulture));",
        _ => "SetGlobalVariableStringValue(" + Cs(operation.Parameter) + ", Convert.ToString(" + CSharpExpression(operation.Value) + ", CultureInfo.InvariantCulture));"
    };

    private static string GlobalGet(OperationRequirement operation)
    {
        var result = Identifier(operation.Result, "result");
        return operation.ValueType switch
        {
            "int" => "int " + result + " = 0; GetGlobalVariableIntValue(" + Cs(operation.Parameter) + ", ref " + result + ");",
            "float" => "float " + result + " = 0f; GetGlobalVariableFloatValue(" + Cs(operation.Parameter) + ", ref " + result + ");",
            _ => "string " + result + " = string.Empty; GetGlobalVariableStringValue(" + Cs(operation.Parameter) + ", ref " + result + ");"
        };
    }

    private static string CSharpExpression(JsonElement value) => Expression(value, true);
    private static string CSharpExpression(ScriptRequirement script, JsonElement value) => Expression(value, true, script);
    private static string PythonExpression(ScriptRequirement script, JsonElement value) => Expression(value, false, script);
    private static string Expression(JsonElement value, bool csharp, ScriptRequirement? script = null)
    {
        if (value.ValueKind != JsonValueKind.Object || !value.TryGetProperty("kind", out var kindElement)) return Literal(value, csharp);
        var kind = kindElement.GetString();
        return kind switch
        {
            "input" => InputExpression(Property(value, "name"), csharp, script),
            "result" => Identifier(Property(value, "name"), "result"),
            "literal" when value.TryGetProperty("value", out var literal) => Literal(literal, csharp),
            "binary" => "(" + Expression(value.GetProperty("left"), csharp, script) + " " + Operator(Property(value, "operator"), csharp) + " " + Expression(value.GetProperty("right"), csharp, script) + ")",
            "unary" => "(" + UnaryOperator(Property(value, "operator"), csharp) + Expression(value.GetProperty("value"), csharp, script) + ")",
            "index" => Expression(value.GetProperty("target"), csharp, script) + "[" + Expression(value.GetProperty("index"), csharp, script) + "]",
            _ => throw new CompilerException("EXPRESSION_INVALID", "Unsupported expression kind: " + kind)
        };
    }

    private static string Operator(string? value, bool csharp) => value switch
    {
        "add" => "+", "subtract" => "-", "multiply" => "*", "divide" => "/", "modulo" => "%", "equal" => "==", "notEqual" => "!=",
        "greaterThan" => ">", "greaterOrEqual" => ">=", "lessThan" => "<", "lessOrEqual" => "<=",
        "and" => csharp ? "&&" : "and", "or" => csharp ? "||" : "or",
        _ => throw new CompilerException("EXPRESSION_INVALID", "Unsupported binary operator: " + value)
    };

    private static string UnaryOperator(string? value, bool csharp) => value switch
    {
        "not" => csharp ? "!" : "not ", "negate" => "-",
        _ => throw new CompilerException("EXPRESSION_INVALID", "Unsupported unary operator: " + value)
    };

    private static string WithCSharpCondition(OperationRequirement operation, string statement) =>
        operation.Condition.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null ? statement : "if (" + CSharpExpression(operation.Condition) + ") { " + statement + " }";

    private static string WithCSharpCondition(ScriptRequirement script, OperationRequirement operation, string statement) =>
        operation.Condition.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null ? statement : "if (" + CSharpExpression(script, operation.Condition) + ") { " + statement + " }";

    private static string WithPythonCondition(ScriptRequirement script, OperationRequirement operation, string statement) =>
        operation.Condition.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null ? statement : "if " + PythonExpression(script, operation.Condition) + ":\n            " + statement.Replace("\n", "\n            ", StringComparison.Ordinal);

    private static string InputExpression(string? name, bool csharp, ScriptRequirement? script)
    {
        var identifier = Identifier(name, "input");
        var port = script?.Inputs.SingleOrDefault(x => x.Name == name);
        if (csharp) return port?.Type == "bool" ? "(" + identifier + " != 0)" : identifier;
        var access = "moduleVar." + identifier;
        if (port?.Type == "bool")
        {
            if (port.DefaultValue.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null) return "bool(" + access + ")";
            return "(" + access + " if " + access + " is not None else " + (port.DefaultValue.ValueKind == JsonValueKind.True ? "1" : "0") + ") != 0";
        }
        if (port is null || port.DefaultValue.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null) return access;
        if (!PythonDefaultSupported(port.DefaultValue)) return access;
        return "(" + access + " if " + access + " is not None else " + Literal(port.DefaultValue, false) + ")";
    }

    private static bool PythonDefaultSupported(JsonElement value) => value.ValueKind is JsonValueKind.String or JsonValueKind.Number or JsonValueKind.True or JsonValueKind.False or JsonValueKind.Null
        || value.ValueKind == JsonValueKind.Array && value.EnumerateArray().All(PythonDefaultSupported);

    private static string CSharpType(string? type) => type switch
    {
        "int[]" => "int[]", "float[]" => "float[]", "string[]" => "string[]", "byte" or "pointset" => "byte[]",
        "image" => "ImageData", "roibox" => "RoiboxData", "roibox[]" => "RoiboxData[]",
        "roiannulus" => "AnnulusData[]", "roipolygon" => "PolygonData[]", "point" => "PointData[]",
        "line" => "LineData[]", "fixture" => "FixtureData[]", "circle" => "CircleData[]",
        "rect" => "RectData[]", "ellipse" => "EllipseData[]",
        _ => throw new CompilerException("VM_TYPE_UNSUPPORTED", "No C# type mapping for: " + type)
    };

    private static string Literal(JsonElement value, bool csharp) => value.ValueKind switch
    {
        JsonValueKind.String => csharp ? Cs(value.GetString()) : Py(value.GetString()),
        JsonValueKind.Number => value.GetRawText(),
        JsonValueKind.True => csharp ? "true" : "True",
        JsonValueKind.False => csharp ? "false" : "False",
        JsonValueKind.Null or JsonValueKind.Undefined => csharp ? "null" : "None",
        JsonValueKind.Array when !csharp => "[" + string.Join(", ", value.EnumerateArray().Select(x => Literal(x, false))) + "]",
        _ => throw new CompilerException("EXPRESSION_INVALID", "Complex literal requires a carrier-specific operation.")
    };

    private static string? Property(JsonElement value, string name) => value.TryGetProperty(name, out var property) && property.ValueKind == JsonValueKind.String ? property.GetString() : null;
    private static string Identifier(string? value, string role) => !string.IsNullOrWhiteSpace(value) && value.All(x => char.IsLetterOrDigit(x) || x == '_') && !char.IsDigit(value[0]) ? value : throw new CompilerException("IDENTIFIER_INVALID", "Invalid " + role + " identifier: " + value);
    private static string Cs(string? value) => "\"" + (value ?? throw new CompilerException("OPERATION_FIELD_REQUIRED", "Operation string field is required.")).Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";
    private static string Py(string? value) => "'" + (value ?? throw new CompilerException("OPERATION_FIELD_REQUIRED", "Operation string field is required.")).Replace("\\", "\\\\").Replace("'", "\\'") + "'";
}
