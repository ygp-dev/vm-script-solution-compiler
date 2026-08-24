using System.Text.Json;
using VmScriptCompiler.Core;
using Xunit;

namespace VmScriptCompiler.Core.Tests;

/// <summary>
/// Guards the patch-safety contract: an agent must be able to see the script source and the port values
/// a user already saved in VM. These run against a captured parse result, so they need no VM install.
/// </summary>
public sealed class SolutionInspectorTests
{
    private static JsonDocument LoadFixture(string name)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "fixtures", name);
        return JsonDocument.Parse(File.ReadAllText(path));
    }

    private static SolutionDigest Digest(string name = "shell-module-parse.json", int sourceLimit = SolutionInspector.DefaultSourceLimit)
    {
        using var document = LoadFixture(name);
        return SolutionInspector.FromParseResult(document.RootElement, sourceLimit);
    }

    [Fact]
    public void ReportsProceduresAndModules()
    {
        var digest = Digest();
        Assert.Equal(["流程1"], digest.Procedures);
        Assert.Equal(3, digest.Modules.Count);
        Assert.Contains(digest.Modules, x => x.Name == "ShellModule" && x.DisplayName == "脚本1");
    }

    [Fact]
    public void ExposesExistingScriptSource()
    {
        var script = Assert.Single(Digest().Scripts);
        Assert.Equal("csharp-module", script.Carrier);
        Assert.Equal("流程1", script.Procedure);
        Assert.Equal("脚本1", script.Module);
        Assert.NotNull(script.Source);
        Assert.Contains("using Script.Methods;", script.Source);
        Assert.Equal(1972, script.SourceLength);
    }

    [Fact]
    public void ExposesDeclaredPorts()
    {
        var script = Assert.Single(Digest().Scripts);
        Assert.NotEmpty(script.Inputs);
        Assert.NotEmpty(script.Outputs);
        var inX = Assert.Single(script.Inputs, x => x.Name == "InX");
        Assert.Equal("float[]", inX.Type);
        Assert.Contains(script.Outputs, x => x.Name == "ModuRunTime");
    }

    [Fact]
    public void SurfacesModuleBindingsSoPatchDoesNotDropThem()
    {
        var script = Assert.Single(Digest().Scripts);
        var inX = Assert.Single(script.Inputs, x => x.Name == "InX");
        Assert.NotNull(inX.Binding);
        Assert.Equal("MatchPointX", inX.Binding!.TargetParameter);
        Assert.Equal(0, inX.Binding.TargetModuleId);
    }

    [Fact]
    public void TruncatesLongSourceButReportsRealLength()
    {
        var script = Assert.Single(Digest(sourceLimit: 100).Scripts);
        Assert.Equal(100, script.Source!.Length);
        Assert.Equal(1972, script.SourceLength);
        Assert.True(Digest(sourceLimit: 100).SourceTruncated);
    }

    [Fact]
    public void RejectsParseResultWithoutSolution()
    {
        using var document = JsonDocument.Parse("""{"formatVersion":5}""");
        var error = Assert.Throws<CompilerException>(() => SolutionInspector.FromParseResult(document.RootElement));
        Assert.Equal("SOL_PARSE_FAILED", error.Code);
    }

    [Theory]
    // A literal default the user saved in VM: segment 5 is "1" and the value sits in segment 4.
    [InlineData("2 . %Threshold% . 0 . 42 . 1 . 0 . All . 1", "42", null)]
    // A binding to another module's output: segment 5 is "0".
    [InlineData("2 . %Threshold% . 0 . MatchNum . 0 . 0 . All . 1", null, "MatchNum")]
    public void DistinguishesPersistedDefaultsFromBindings(string relation, string? expectedDefault, string? expectedTarget)
    {
        var json = $$"""
        {
          "solution": {
            "procedures": [{
              "displayName": "流程1",
              "modules": [{
                "moduleId": 2, "name": "ShellModule", "displayName": "脚本1", "isEnabled": true,
                "scriptText": "",
                "binaryParams": [{
                  "name": "Input",
                  "parsed": "<?xml version=\"1.0\" encoding=\"UTF-8\"?><ArrayOfModuleParamItem><ModuleParamItem><Name>%Threshold%</Name><ValueType>int</ValueType><IsForce>true</IsForce><IsShow>true</IsShow></ModuleParamItem></ArrayOfModuleParamItem>"
                }],
                "subscriptions": [{
                  "paramName": "%Threshold%", "isBound": true, "targetModuleId": 0,
                  "targetParamName": "MatchNum", "indexMode": "All",
                  "relationString": "{{relation}}"
                }]
              }]
            }]
          }
        }
        """;
        using var document = JsonDocument.Parse(json);
        var script = Assert.Single(SolutionInspector.FromParseResult(document.RootElement).Scripts);
        var port = Assert.Single(script.Inputs);
        Assert.Equal("Threshold", port.Name);
        Assert.Equal(expectedDefault, port.PersistedDefault);
        Assert.Equal(expectedTarget, port.Binding?.TargetParameter);
    }
}
