using System.Text.Json;
using System.Text.Json.Serialization;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace VmScriptCompiler.Desktop;

internal sealed class DesktopStateStore
{
    private static readonly byte[] SecretEntropy = Encoding.UTF8.GetBytes("VM Script Compiler Desktop Settings v1");
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly string _stateDirectory;
    private readonly string _historyFile;
    private readonly string _artifactIndexFile;

    public DesktopStateStore(string? stateDirectory = null)
    {
        var local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        _stateDirectory = stateDirectory ?? Path.Combine(local, "VM Script Compiler");
        SettingsFile = Path.Combine(_stateDirectory, "desktop-settings.json");
        _historyFile = Path.Combine(_stateDirectory, "recent-conversations.json");
        _artifactIndexFile = Path.Combine(_stateDirectory, "artifact-index.json");
    }

    public string SettingsFile { get; }
    public string StateDirectory => _stateDirectory;

    public string ProtectSecret(string value)
    {
        if (string.IsNullOrEmpty(value)) return "";
        var protectedBytes = ProtectedData.Protect(Encoding.UTF8.GetBytes(value), SecretEntropy, DataProtectionScope.CurrentUser);
        return Convert.ToBase64String(protectedBytes);
    }

    public string UnprotectSecret(string protectedValue)
    {
        if (string.IsNullOrWhiteSpace(protectedValue)) return "";
        try
        {
            var bytes = Convert.FromBase64String(protectedValue);
            return Encoding.UTF8.GetString(ProtectedData.Unprotect(bytes, SecretEntropy, DataProtectionScope.CurrentUser));
        }
        catch (FormatException) { return ""; }
        catch (CryptographicException) { return ""; }
    }

    public static void RunSmokeTest()
    {
        var root = Path.Combine(Path.GetTempPath(), "vm-script-desktop-state-" + Guid.NewGuid().ToString("N"));
        try
        {
            var state = Path.Combine(root, "state");
            var output = Path.Combine(root, "outputs", "20260723000000000-smoke");
            Directory.CreateDirectory(Path.Combine(output, "generated"));
            File.WriteAllText(Path.Combine(output, "result.sol"), "smoke");
            File.WriteAllText(Path.Combine(output, "build-report.md"), "# smoke artifact");
            File.WriteAllText(Path.Combine(output, "generated", "smoke.cs"), "// searchable-token");

            var store = new DesktopStateStore(state);
            var settings = store.LoadSettings(Path.Combine(root, "outputs"));
            settings.AiModel = "smoke-model";
            settings.EncryptedApiKey = store.ProtectSecret("smoke-secret-value");
            store.SaveSettings(settings);
            if (store.LoadSettings("unused").AiModel != "smoke-model")
                throw new InvalidOperationException("Desktop settings round-trip failed.");
            if (store.UnprotectSecret(store.LoadSettings("unused").EncryptedApiKey) != "smoke-secret-value")
                throw new InvalidOperationException("Desktop encrypted API key round-trip failed.");
            if (File.ReadAllText(store.SettingsFile).Contains("smoke-secret-value", StringComparison.Ordinal))
                throw new InvalidOperationException("Desktop settings contain a plaintext API key.");

            store.AddConversation(new ConversationRecord(
                "smoke", DateTime.UtcNow, "生成方案", "create", "smoke prompt", "local",
                true, "ok", output, Path.Combine(output, "result.sol"),
                Path.Combine(output, "build-report.md"), null, null), 10);
            if (store.LoadConversations().Count != 1)
                throw new InvalidOperationException("Desktop conversation round-trip failed.");

            var artifacts = store.RefreshArtifactIndex(Path.Combine(root, "outputs"));
            if (artifacts.Count != 3 || !artifacts.Any(x => x.SearchText.Contains("searchable-token", StringComparison.Ordinal)))
                throw new InvalidOperationException("Desktop artifact indexing failed.");
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
    }

    public DesktopSettings LoadSettings(string defaultOutputDirectory)
    {
        Directory.CreateDirectory(_stateDirectory);
        var settings = Read<DesktopSettings>(SettingsFile) ?? new DesktopSettings();
        if (settings.Version < 2)
        {
            settings.Version = 2;
            if (string.IsNullOrWhiteSpace(settings.AiProvider) || settings.AiProvider == "local")
                settings.AiProvider = "openai-responses";
            if (string.IsNullOrWhiteSpace(settings.AiEndpoint))
                settings.AiEndpoint = "https://api.openai.com/v1";
            if (string.IsNullOrWhiteSpace(settings.ThinkingLevel))
                settings.ThinkingLevel = "high";
        }
        if (string.IsNullOrWhiteSpace(settings.OutputDirectory))
            settings.OutputDirectory = defaultOutputDirectory;
        settings.MaxRecentConversations = Math.Clamp(settings.MaxRecentConversations, 10, 500);
        SaveSettings(settings);
        return settings;
    }

    public void SaveSettings(DesktopSettings settings)
    {
        Directory.CreateDirectory(_stateDirectory);
        settings.MaxRecentConversations = Math.Clamp(settings.MaxRecentConversations, 10, 500);
        WriteAtomic(SettingsFile, settings);
    }

    public IReadOnlyList<ConversationRecord> LoadConversations()
        => (Read<List<ConversationRecord>>(_historyFile) ?? []).OrderByDescending(x => x.TimestampUtc).ToArray();

    public void AddConversation(ConversationRecord record, int maximum)
    {
        var history = LoadConversations().ToList();
        history.Insert(0, record);
        history = history
            .GroupBy(x => x.Id, StringComparer.OrdinalIgnoreCase)
            .Select(x => x.First())
            .OrderByDescending(x => x.TimestampUtc)
            .Take(Math.Clamp(maximum, 10, 500))
            .ToList();
        WriteAtomic(_historyFile, history);
    }

    public IReadOnlyList<ArtifactRecord> LoadArtifactIndex()
        => (Read<List<ArtifactRecord>>(_artifactIndexFile) ?? []).OrderByDescending(x => x.ModifiedUtc).ToArray();

    public IReadOnlyList<ArtifactRecord> RefreshArtifactIndex(string outputDirectory)
    {
        var artifacts = new List<ArtifactRecord>();
        if (Directory.Exists(outputDirectory))
        {
            foreach (var file in EnumerateFilesSafe(outputDirectory))
            {
                var kind = ClassifyArtifact(file);
                if (kind is null) continue;
                try
                {
                    var info = new FileInfo(file);
                    artifacts.Add(new ArtifactRecord(
                        Path.GetFullPath(file),
                        kind,
                        FindTaskDirectory(info.Directory, outputDirectory),
                        info.LastWriteTimeUtc,
                        info.Length,
                        BuildSearchText(info, kind)));
                }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }
            }
        }

        var ordered = artifacts.OrderByDescending(x => x.ModifiedUtc).ToList();
        WriteAtomic(_artifactIndexFile, ordered);
        return ordered;
    }

    private static IEnumerable<string> EnumerateFilesSafe(string root)
    {
        var pending = new Stack<string>();
        pending.Push(root);
        while (pending.Count > 0)
        {
            var directory = pending.Pop();
            IEnumerable<string> directories = [];
            IEnumerable<string> files = [];
            try
            {
                directories = Directory.EnumerateDirectories(directory)
                    .Where(x => !Path.GetFileName(x).StartsWith("_release-backup", StringComparison.OrdinalIgnoreCase));
                files = Directory.EnumerateFiles(directory);
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }

            foreach (var file in files) yield return file;
            foreach (var child in directories) pending.Push(child);
        }
    }

    private static string? ClassifyArtifact(string file)
    {
        var name = Path.GetFileName(file);
        var extension = Path.GetExtension(file);
        if (extension.Equals(".sol", StringComparison.OrdinalIgnoreCase)) return "SOL 方案";
        if (name.Equals("build-report.md", StringComparison.OrdinalIgnoreCase)) return "构建报告";
        if (name.Equals("script-contract.json", StringComparison.OrdinalIgnoreCase)) return "脚本契约";
        if (name.Equals("requirement.json", StringComparison.OrdinalIgnoreCase) ||
            name.EndsWith(".requirement.json", StringComparison.OrdinalIgnoreCase)) return "Requirement";
        if (name.Equals("dependency-manifest.json", StringComparison.OrdinalIgnoreCase) ||
            name.Equals("deploy-to-vm.ps1", StringComparison.OrdinalIgnoreCase)) return "DLL 部署";
        if ((extension.Equals(".cs", StringComparison.OrdinalIgnoreCase) ||
             extension.Equals(".py", StringComparison.OrdinalIgnoreCase)) &&
            file.Contains($"{Path.DirectorySeparatorChar}generated{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
            return "生成脚本";
        return null;
    }

    private static string FindTaskDirectory(DirectoryInfo? directory, string outputRoot)
    {
        var root = Path.GetFullPath(outputRoot).TrimEnd(Path.DirectorySeparatorChar);
        while (directory is not null && directory.FullName.StartsWith(root, StringComparison.OrdinalIgnoreCase))
        {
            if (File.Exists(Path.Combine(directory.FullName, "result.sol")) ||
                File.Exists(Path.Combine(directory.FullName, "build-report.md")) ||
                Directory.Exists(Path.Combine(directory.FullName, "generated")))
                return directory.FullName;
            directory = directory.Parent;
        }
        return Path.GetDirectoryName(root) ?? root;
    }

    private static string BuildSearchText(FileInfo file, string kind)
    {
        var text = $"{file.Name}\n{file.FullName}\n{kind}";
        if (file.Length > 512 * 1024) return text;
        var extension = file.Extension;
        if (!extension.Equals(".json", StringComparison.OrdinalIgnoreCase) &&
            !extension.Equals(".md", StringComparison.OrdinalIgnoreCase) &&
            !extension.Equals(".cs", StringComparison.OrdinalIgnoreCase) &&
            !extension.Equals(".py", StringComparison.OrdinalIgnoreCase))
            return text;
        try { return text + "\n" + File.ReadAllText(file.FullName); }
        catch { return text; }
    }

    private static T? Read<T>(string file)
    {
        if (!File.Exists(file)) return default;
        try { return JsonSerializer.Deserialize<T>(File.ReadAllText(file), JsonOptions); }
        catch (JsonException) { return default; }
        catch (IOException) { return default; }
    }

    private static void WriteAtomic<T>(string file, T value)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(file)!);
        var temporary = file + ".tmp";
        File.WriteAllText(temporary, JsonSerializer.Serialize(value, JsonOptions));
        File.Move(temporary, file, true);
    }
}

internal sealed class DesktopSettings
{
    public int Version { get; set; } = 2;
    public string OutputDirectory { get; set; } = "";
    public string AiProvider { get; set; } = "openai-responses";
    public string AiEndpoint { get; set; } = "https://api.openai.com/v1";
    public string AiModel { get; set; } = "";
    public string ThinkingLevel { get; set; } = "high";
    public string EncryptedApiKey { get; set; } = "";
    public int MaxRecentConversations { get; set; } = 100;
}

internal sealed record ConversationRecord(
    string Id,
    DateTime TimestampUtc,
    string Action,
    string Mode,
    string Prompt,
    string Provider,
    bool Succeeded,
    string Status,
    string? TaskDirectory,
    string? SolutionFile,
    string? ReportFile,
    string? ErrorCode,
    string? ErrorMessage)
{
    [JsonIgnore] public string DisplayTime => TimestampUtc.ToLocalTime().ToString("yyyy-MM-dd HH:mm");
    [JsonIgnore] public string DisplayTitle
    {
        get
        {
            var compact = string.Join(" ", Prompt.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
            if (compact.Length > 64) compact = compact[..64] + "…";
            return $"{DisplayTime} · {(Succeeded ? "成功" : "失败")} · {compact}";
        }
    }
}

internal sealed record ArtifactRecord(
    string FilePath,
    string Kind,
    string TaskDirectory,
    DateTime ModifiedUtc,
    long Size,
    string SearchText)
{
    [JsonIgnore] public string FileName => Path.GetFileName(FilePath);
    [JsonIgnore] public string DisplayTime => ModifiedUtc.ToLocalTime().ToString("yyyy-MM-dd HH:mm");
    [JsonIgnore] public string DisplayTitle => $"{Kind} · {FileName}";
    [JsonIgnore] public string DisplayDetail => $"{DisplayTime} · {FormatSize(Size)} · {TaskDirectory}";

    private static string FormatSize(long size) => size switch
    {
        >= 1024 * 1024 => $"{size / 1024d / 1024d:0.##} MB",
        >= 1024 => $"{size / 1024d:0.##} KB",
        _ => $"{size} B"
    };
}
