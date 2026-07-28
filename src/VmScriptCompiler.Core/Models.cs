using System.Text.Json;

namespace VmScriptCompiler.Core;

public sealed record VmEnvironment(bool Found, string? ErrorCode, string? VmRoot, string? Version, string? Development, string? AlgorithmSdk, string? Applications, string? GlobalScript, bool GlobalScriptAvailable);
public sealed record ValidationIssue(string Code, string Message, string? Path = null);
public sealed record RequirementValidationResult(bool IsValid, IReadOnlyList<ValidationIssue> Issues);
public sealed record ResourceManifest(string ResourceVersion, string VisionMasterVersion, bool ContainsSolFiles, string ScriptBasePath, IReadOnlyDictionary<string, string> Hashes, bool RuntimeValidated, IReadOnlyList<string> RuntimeValidationPending);
public sealed record ProcessResult(int ExitCode, string StandardOutput, string StandardError);
public sealed record BuildResult(string TaskDirectory, string SolutionFile, string ReportFile, RequirementValidationResult RequirementValidation, ProcessResult Parse, ProcessResult Inspect);

public static class ProductInfo
{
    public static string Version { get; } = typeof(ProductInfo).Assembly.GetName().Version?.ToString(3) ?? "unknown";
}

public static class JsonDefaults
{
    public static readonly JsonSerializerOptions Options = new() { WriteIndented = true, PropertyNameCaseInsensitive = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
}
