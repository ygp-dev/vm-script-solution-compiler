using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Win32;

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
            var match = Regex.Match(part, "^(.*?VisionMaster(?:\\d+(?:\\.\\d+){0,3})?)(?:\\\\.*)?$", RegexOptions.IgnoreCase);
            if (match.Success) candidates.Add(match.Groups[1].Value);
        }
        foreach (var parent in new[] { Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86) }
            .Where(Directory.Exists).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            try
            {
                candidates.AddRange(Directory.EnumerateDirectories(parent, "VisionMaster*", SearchOption.TopDirectoryOnly));
            }
            catch (UnauthorizedAccessException) { }
            catch (IOException) { }
        }
        candidates.AddRange(RegistryInstallLocations());
        foreach (var candidate in candidates.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            string root;
            try { root = Path.GetFullPath(candidate!.Trim().Trim('"').TrimEnd('\\')); } catch { continue; }
            var applications = Path.Combine(root, "Applications");
            if (!Directory.Exists(applications) && !File.Exists(Path.Combine(root, "VisionMaster.exe"))) continue;
            var globalScript = Path.Combine(root, "Applications", "GlobalScript");
            var version = Regex.Match(Path.GetFileName(root), "VisionMaster(.+)$", RegexOptions.IgnoreCase).Groups[1].Value;
            return new(true, null, root, string.IsNullOrEmpty(version) ? null : version, Path.Combine(root, "Development"), Path.Combine(root, "MVDAlgorithmSDK"), Path.Combine(root, "Applications"), globalScript, Directory.Exists(globalScript));
        }
        return new(false, "VM_HOME_NOT_FOUND", null, null, null, null, null, null, false);
    }

    private static IEnumerable<string> RegistryInstallLocations()
    {
        if (!OperatingSystem.IsWindows()) yield break;
        foreach (var hive in new[] { RegistryHive.LocalMachine, RegistryHive.CurrentUser })
        foreach (var view in new[] { RegistryView.Registry64, RegistryView.Registry32 })
        {
            RegistryKey? root = null;
            try
            {
                using var baseKey = RegistryKey.OpenBaseKey(hive, view);
                root = baseKey.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall");
            }
            catch (System.Security.SecurityException) { }
            catch (UnauthorizedAccessException) { }
            if (root is null) continue;
            using (root)
            {
                string[] names;
                try { names = root.GetSubKeyNames(); }
                catch (System.Security.SecurityException) { continue; }
                catch (UnauthorizedAccessException) { continue; }
                foreach (var name in names)
                {
                    RegistryKey? entry;
                    try { entry = root.OpenSubKey(name); }
                    catch (System.Security.SecurityException) { continue; }
                    catch (UnauthorizedAccessException) { continue; }
                    using (entry)
                    {
                        if (entry?.GetValue("DisplayName") is not string displayName || !displayName.Contains("VisionMaster", StringComparison.OrdinalIgnoreCase)) continue;
                        if (entry.GetValue("InstallLocation") is string location && !string.IsNullOrWhiteSpace(location)) yield return location;
                    }
                }
            }
        }
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
