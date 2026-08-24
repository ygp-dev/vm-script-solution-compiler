using System.Text.Json;
using System.Text.RegularExpressions;

namespace VmScriptCompiler.Core;

public sealed class EnvironmentDetector
{
    /// <summary>
    /// Opt-in override that lets CI and unit tests exercise the deterministic generation path on
    /// machines without a VisionMaster installation. The value is either inline JSON or a path to a
    /// JSON file describing a <see cref="VmEnvironment"/>. Never set in production installs.
    /// </summary>
    public const string AssumeEnvironmentVariable = "VM_SCRIPT_ASSUME_ENVIRONMENT";

    public VmEnvironment Detect()
    {
        if (TryReadAssumedEnvironment(Environment.GetEnvironmentVariable(AssumeEnvironmentVariable), out var assumed)) return assumed;
        return DetectInstalled();
    }

    /// <summary>Probes the real machine, ignoring <see cref="AssumeEnvironmentVariable"/>.</summary>
    public VmEnvironment DetectInstalled()
    {
        var candidates = new List<string?> { Environment.GetEnvironmentVariable("VISIONMASTER_HOME"), Environment.GetEnvironmentVariable("VM_HOME") };
        var sdk = Environment.GetEnvironmentVariable("MVDALGO_DEV_ENV");
        if (!string.IsNullOrWhiteSpace(sdk)) candidates.Add(Directory.GetParent(sdk)?.FullName);
        foreach (var part in (Environment.GetEnvironmentVariable("PATH") ?? "").Split(';', StringSplitOptions.RemoveEmptyEntries))
        {
            var match = Regex.Match(part, "^(.*?VisionMaster\\d+(?:\\.\\d+){1,3})(?:\\\\.*)?$", RegexOptions.IgnoreCase);
            if (match.Success) candidates.Add(match.Groups[1].Value);
        }
        foreach (var candidate in candidates.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            string root;
            try { root = Path.GetFullPath(candidate!.Trim().Trim('"').TrimEnd('\\')); } catch { continue; }
            if (!Directory.Exists(Path.Combine(root, "Development")) || !Directory.Exists(Path.Combine(root, "MVDAlgorithmSDK")) || !Directory.Exists(Path.Combine(root, "Applications"))) continue;
            var globalScript = Path.Combine(root, "Applications", "GlobalScript");
            var version = Regex.Match(Path.GetFileName(root), "VisionMaster(.+)$", RegexOptions.IgnoreCase).Groups[1].Value;
            return new(true, null, root, string.IsNullOrEmpty(version) ? null : version, Path.Combine(root, "Development"), Path.Combine(root, "MVDAlgorithmSDK"), Path.Combine(root, "Applications"), globalScript, Directory.Exists(globalScript));
        }
        return new(false, "VM_HOME_NOT_FOUND", null, null, null, null, null, null, false);
    }

    /// <summary>
    /// Parses an assumed-environment descriptor. Returns false when <paramref name="value"/> is blank,
    /// so callers fall back to real detection. Throws when the descriptor is present but unusable —
    /// a malformed override must never silently degrade into probing the machine.
    /// </summary>
    public static bool TryReadAssumedEnvironment(string? value, out VmEnvironment environment)
    {
        environment = default!;
        if (string.IsNullOrWhiteSpace(value)) return false;

        var text = value.Trim();
        if (!text.StartsWith('{'))
        {
            var file = Path.GetFullPath(text.Trim('"'));
            if (!File.Exists(file))
                throw new CompilerException("ASSUMED_ENVIRONMENT_NOT_FOUND", $"{AssumeEnvironmentVariable} points to a missing file: {file}");
            text = File.ReadAllText(file);
        }

        AssumedEnvironment? assumed;
        try { assumed = JsonSerializer.Deserialize<AssumedEnvironment>(text, JsonDefaults.Options); }
        catch (JsonException error)
        {
            throw new CompilerException("ASSUMED_ENVIRONMENT_INVALID", $"{AssumeEnvironmentVariable} is not valid JSON: {error.Message}");
        }
        if (assumed is null)
            throw new CompilerException("ASSUMED_ENVIRONMENT_INVALID", $"{AssumeEnvironmentVariable} did not describe an environment object.");
        if (string.IsNullOrWhiteSpace(assumed.VmRoot))
            throw new CompilerException("ASSUMED_ENVIRONMENT_INVALID", $"{AssumeEnvironmentVariable} requires vmRoot.");
        if (string.IsNullOrWhiteSpace(assumed.Version))
            throw new CompilerException("ASSUMED_ENVIRONMENT_INVALID", $"{AssumeEnvironmentVariable} requires version.");

        var root = Path.GetFullPath(assumed.VmRoot.Trim().Trim('"').TrimEnd('\\'));
        var globalScript = assumed.GlobalScript ?? Path.Combine(root, "Applications", "GlobalScript");
        environment = new(
            true,
            null,
            root,
            assumed.Version,
            assumed.Development ?? Path.Combine(root, "Development"),
            assumed.AlgorithmSdk ?? Path.Combine(root, "MVDAlgorithmSDK"),
            assumed.Applications ?? Path.Combine(root, "Applications"),
            globalScript,
            assumed.GlobalScriptAvailable ?? Directory.Exists(globalScript));
        return true;
    }

    private sealed record AssumedEnvironment(
        string? VmRoot,
        string? Version,
        string? Development,
        string? AlgorithmSdk,
        string? Applications,
        string? GlobalScript,
        bool? GlobalScriptAvailable);
}
