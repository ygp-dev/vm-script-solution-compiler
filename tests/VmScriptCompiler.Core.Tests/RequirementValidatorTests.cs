using System.Text.Json;
using VmScriptCompiler.Core;
using Xunit;

namespace VmScriptCompiler.Core.Tests;

/// <summary>
/// Covers the Requirement rules that encode hard VM 4.4 evidence. These are pure functions over JSON,
/// so they run without a VisionMaster install and guard the rules a generator change could silently drop.
/// </summary>
public sealed class RequirementValidatorTests
{
    private static RequirementValidationResult Validate(string json)
    {
        using var document = JsonDocument.Parse(json);
        return new RequirementValidator().Validate(document);
    }

    private static string Requirement(string scripts, string? connections = null, string mode = "create") => $$"""
    {
      "schemaVersion": "1.0",
      "task": { "name": "unit", "mode": "{{mode}}", "vmVersion": "4.4.0" },
      "scripts": {{scripts}}
      {{(connections is null ? "" : ", \"connections\": " + connections)}}
    }
    """;

    private static string CSharpScript(
        string id = "a", string name = "脚本1", string procedure = "流程1",
        string inputs = "[]", string outputs = "[]", string extra = "") => $$"""
    {
      "id": "{{id}}", "carrier": "csharp-module", "name": "{{name}}", "procedure": "{{procedure}}",
      "execution": { "mode": "once", "order": 0 },
      "inputs": {{inputs}}, "outputs": {{outputs}}, "operations": [], "dependencies": []
      {{extra}}
    }
    """;

    private static void AssertCode(RequirementValidationResult result, string code)
    {
        Assert.False(result.IsValid);
        Assert.Contains(result.Issues, x => x.Code == code);
    }

    [Fact]
    public void AcceptsMinimalCSharpModule()
    {
        var result = Validate(Requirement($"[{CSharpScript()}]"));
        Assert.True(result.IsValid, string.Join("; ", result.Issues.Select(x => x.Code + " " + x.Message)));
    }

    [Theory]
    [InlineData("\"1.1\"", "REQUIREMENT_SCHEMA_INVALID")]
    public void RejectsUnsupportedSchemaVersion(string version, string code)
    {
        var json = $$"""
        { "schemaVersion": {{version}}, "task": { "name": "u", "mode": "create", "vmVersion": "4.4.0" }, "scripts": [] }
        """;
        AssertCode(Validate(json), code);
    }

    [Theory]
    [InlineData("4.3.0")]
    [InlineData("4.5")]
    [InlineData("5.0.0")]
    public void RejectsNonVm44Version(string version)
    {
        var json = $$"""
        { "schemaVersion": "1.0", "task": { "name": "u", "mode": "create", "vmVersion": "{{version}}" },
          "scripts": [{{CSharpScript()}}] }
        """;
        AssertCode(Validate(json), "REQUIREMENT_SCHEMA_INVALID");
    }

    // VM 4.4's ShellModule port type list has no Circle; CircleData[] is reachable only through
    // GetVarCircle/SetVarCircle, so declaring it as a port must fail loudly.
    [Fact]
    public void RejectsCirclePortOnCSharpModule()
    {
        var inputs = """[{ "name": "C", "type": "circle" }]""";
        AssertCode(Validate(Requirement($"[{CSharpScript(inputs: inputs)}]")), "CSHARP_PORT_TYPE_UNSUPPORTED");
    }

    [Fact]
    public void AllowsCirclePortOnPythonRejectedAsUnconfirmed()
    {
        var script = """
        { "id": "p", "carrier": "python-module", "name": "脚本P", "procedure": "流程1",
          "execution": { "mode": "once" }, "inputs": [{ "name": "C", "type": "roibox" }],
          "outputs": [], "operations": [], "dependencies": [] }
        """;
        AssertCode(Validate(Requirement($"[{script}]")), "PYTHON_COMPLEX_TYPE_UNCONFIRMED");
    }

    [Fact]
    public void RejectsDuplicateScriptId()
    {
        var json = Requirement($"[{CSharpScript(id: "a", name: "脚本1")}, {CSharpScript(id: "a", name: "脚本2")}]");
        AssertCode(Validate(json), "DUPLICATE_SCRIPT_ID");
    }

    [Fact]
    public void RejectsDuplicateModuleNameWithinProcedure()
    {
        var json = Requirement($"[{CSharpScript(id: "a", name: "同名")}, {CSharpScript(id: "b", name: "同名")}]");
        AssertCode(Validate(json), "DUPLICATE_MODULE_NAME");
    }

    [Fact]
    public void AllowsSameModuleNameInDifferentProcedures()
    {
        var json = Requirement($"[{CSharpScript(id: "a", name: "同名", procedure: "流程1")}, {CSharpScript(id: "b", name: "同名", procedure: "流程2")}]");
        Assert.True(Validate(json).IsValid);
    }

    [Theory]
    [InlineData("有.点")]   // VM display names cannot contain a dot
    [InlineData(" 前导空格")]
    public void RejectsInvalidDisplayName(string name)
    {
        AssertCode(Validate(Requirement($"[{CSharpScript(name: name)}]")), "VM_NAME_INVALID");
    }

    [Fact]
    public void RejectsMoreThanOneGlobalScript()
    {
        var global = """
        { "id": "g%d", "carrier": "global-csharp", "name": "全局%d",
          "execution": { "mode": "init" }, "inputs": [], "outputs": [], "operations": [], "dependencies": [] }
        """;
        var json = Requirement($"[{global.Replace("%d", "1")}, {global.Replace("%d", "2")}]");
        AssertCode(Validate(json), "MULTIPLE_GLOBAL_SCRIPTS");
    }

    [Fact]
    public void RejectsGlobalScriptDeclaringPorts()
    {
        var global = """
        { "id": "g", "carrier": "global-csharp", "name": "全局",
          "execution": { "mode": "init" }, "inputs": [{ "name": "A", "type": "int" }],
          "outputs": [], "operations": [], "dependencies": [] }
        """;
        AssertCode(Validate(Requirement($"[{global}]")), "GLOBAL_SCRIPT_IO_UNSUPPORTED");
    }

    [Fact]
    public void RejectsConnectionAcrossProcedures()
    {
        var json = Requirement(
            $"[{CSharpScript(id: "a", name: "脚本1", procedure: "流程1")}, {CSharpScript(id: "b", name: "脚本2", procedure: "流程2")}]",
            """[{ "from": "a", "to": "b" }]""");
        AssertCode(Validate(json), "CROSS_PROCEDURE_CONNECTION_UNSUPPORTED");
    }

    [Fact]
    public void RejectsConnectionToUnknownScript()
    {
        var json = Requirement($"[{CSharpScript(id: "a")}]", """[{ "from": "a", "to": "missing" }]""");
        AssertCode(Validate(json), "CONNECTION_ENDPOINT_NOT_FOUND");
    }

    [Fact]
    public void RejectsIoDefaultThatDoesNotMatchDeclaredType()
    {
        var inputs = """[{ "name": "A", "type": "int", "default": "not-an-int" }]""";
        AssertCode(Validate(Requirement($"[{CSharpScript(inputs: inputs)}]")), "IO_TYPE_MISMATCH");
    }

    [Fact]
    public void RejectsInputAndOutputSharingAName()
    {
        var json = Requirement($"""[{CSharpScript(
            inputs: """[{ "name": "V", "type": "int" }]""",
            outputs: """[{ "name": "V", "type": "int" }]""")}]""");
        AssertCode(Validate(json), "DUPLICATE_IO_NAME");
    }

    [Fact]
    public void RejectsUnsupportedProperty()
    {
        var json = Requirement($"[{CSharpScript(extra: ", \"unknownField\": 1")}]");
        AssertCode(Validate(json), "REQUIREMENT_SCHEMA_INVALID");
    }
}
