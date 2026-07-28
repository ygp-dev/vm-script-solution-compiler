using System.Text.RegularExpressions;

namespace VmScriptCompiler.Core;

public sealed class EnvironmentDetector
{
    public VmEnvironment Detect()
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
}
