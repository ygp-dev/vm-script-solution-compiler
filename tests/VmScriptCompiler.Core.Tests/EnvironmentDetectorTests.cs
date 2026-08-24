using VmScriptCompiler.Core;
using Xunit;

namespace VmScriptCompiler.Core.Tests;

/// <summary>
/// Covers the opt-in environment override that lets CI exercise Core without a VisionMaster install.
/// A malformed override must fail loudly rather than silently fall back to probing the machine, or a
/// broken CI config would quietly start testing whatever VM happens to be installed on the agent.
/// </summary>
public sealed class EnvironmentDetectorTests
{
    [Fact]
    public void BlankOverrideFallsBackToRealDetection()
    {
        Assert.False(EnvironmentDetector.TryReadAssumedEnvironment(null, out _));
        Assert.False(EnvironmentDetector.TryReadAssumedEnvironment("", out _));
        Assert.False(EnvironmentDetector.TryReadAssumedEnvironment("   ", out _));
    }

    [Fact]
    public void InlineJsonProducesEnvironmentWithDerivedPaths()
    {
        var json = """{ "vmRoot": "C:\\VM\\VisionMaster4.4.0", "version": "4.4.0" }""";
        Assert.True(EnvironmentDetector.TryReadAssumedEnvironment(json, out var environment));
        Assert.True(environment.Found);
        Assert.Null(environment.ErrorCode);
        Assert.Equal("4.4.0", environment.Version);
        Assert.Equal(Path.Combine("C:\\VM\\VisionMaster4.4.0", "Development"), environment.Development);
        Assert.Equal(Path.Combine("C:\\VM\\VisionMaster4.4.0", "MVDAlgorithmSDK"), environment.AlgorithmSdk);
        Assert.Equal(Path.Combine("C:\\VM\\VisionMaster4.4.0", "Applications"), environment.Applications);
    }

    [Fact]
    public void ExplicitPathsOverrideDerivedDefaults()
    {
        var json = """
        { "vmRoot": "C:\\VM\\VisionMaster4.4.0", "version": "4.4.0",
          "development": "D:\\dev", "globalScriptAvailable": true }
        """;
        Assert.True(EnvironmentDetector.TryReadAssumedEnvironment(json, out var environment));
        Assert.Equal("D:\\dev", environment.Development);
        Assert.True(environment.GlobalScriptAvailable);
    }

    [Fact]
    public void ReadsOverrideFromFile()
    {
        var file = Path.Combine(Path.GetTempPath(), "vm-assume-" + Guid.NewGuid().ToString("N") + ".json");
        File.WriteAllText(file, """{ "vmRoot": "C:\\VM\\VisionMaster4.4.0", "version": "4.4.0" }""");
        try
        {
            Assert.True(EnvironmentDetector.TryReadAssumedEnvironment(file, out var environment));
            Assert.Equal("4.4.0", environment.Version);
        }
        finally { File.Delete(file); }
    }

    [Fact]
    public void MissingFileFailsLoudly()
    {
        var missing = Path.Combine(Path.GetTempPath(), "vm-assume-missing-" + Guid.NewGuid().ToString("N") + ".json");
        var error = Assert.Throws<CompilerException>(() => EnvironmentDetector.TryReadAssumedEnvironment(missing, out _));
        Assert.Equal("ASSUMED_ENVIRONMENT_NOT_FOUND", error.Code);
    }

    [Fact]
    public void MalformedJsonFailsLoudly()
    {
        var error = Assert.Throws<CompilerException>(() => EnvironmentDetector.TryReadAssumedEnvironment("{ not json", out _));
        Assert.Equal("ASSUMED_ENVIRONMENT_INVALID", error.Code);
    }

    [Theory]
    [InlineData("""{ "version": "4.4.0" }""")]
    [InlineData("""{ "vmRoot": "C:\\VM\\VisionMaster4.4.0" }""")]
    public void IncompleteOverrideFailsLoudly(string json)
    {
        var error = Assert.Throws<CompilerException>(() => EnvironmentDetector.TryReadAssumedEnvironment(json, out _));
        Assert.Equal("ASSUMED_ENVIRONMENT_INVALID", error.Code);
    }
}
