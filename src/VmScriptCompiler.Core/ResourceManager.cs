using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace VmScriptCompiler.Core;

public sealed class ResourceManager(string repositoryRoot)
{
    public string RepositoryRoot { get; } = Path.GetFullPath(repositoryRoot);
    public ResourceManifest LoadAndValidate(string vmVersion)
    {
        var resourceRoot = Path.Combine(RepositoryRoot, "resources", "vm", vmVersion);
        var manifestPath = Path.Combine(resourceRoot, "manifest.json");
        if (!File.Exists(manifestPath)) throw new CompilerException("RESOURCE_MANIFEST_MISSING", "未找到 VM 资源 manifest。");
        using var document = JsonDocument.Parse(File.ReadAllText(manifestPath));
        var root = document.RootElement;
        var version = root.GetProperty("visionMasterVersion").GetString() ?? "";
        if (!string.Equals(version, vmVersion, StringComparison.Ordinal)) throw new CompilerException("VM_VERSION_MISMATCH", "需求版本与资源 manifest 不一致。");
        if (root.GetProperty("formatVersion").GetInt32() != 7 || root.GetProperty("architecture").GetString() != "x64")
            throw new CompilerException("RESOURCE_MANIFEST_INVALID", "VM 4.4 resource format must be formatVersion 7 / x64.");
        if (root.GetProperty("containsSolFiles").GetBoolean()) throw new CompilerException("RESOURCE_MANIFEST_INVALID", "正式资源不能包含 .sol 模板。");
        if (Directory.EnumerateFiles(resourceRoot, "*.sol", SearchOption.AllDirectories).Any())
            throw new CompilerException("RESOURCE_MANIFEST_INVALID", "正式资源目录实际包含 .sol 文件。");
        var hashes = root.GetProperty("hashes").EnumerateObject().ToDictionary(x => x.Name, x => x.Value.GetString() ?? "", StringComparer.OrdinalIgnoreCase);
        foreach (var (relative, expected) in hashes) {
            var file = Path.Combine(resourceRoot, relative.Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(file) || !string.Equals(Hash(file), expected, StringComparison.OrdinalIgnoreCase)) throw new CompilerException("RESOURCE_HASH_MISMATCH", "资源哈希不匹配: " + relative);
        }
        var untracked = Directory.EnumerateFiles(resourceRoot, "*", SearchOption.AllDirectories)
            .Where(x => !string.Equals(Path.GetFullPath(x), Path.GetFullPath(manifestPath), StringComparison.OrdinalIgnoreCase))
            .Select(x => Path.GetRelativePath(resourceRoot, x).Replace('\\', '/'))
            .Where(x => !hashes.ContainsKey(x)).OrderBy(x => x, StringComparer.Ordinal).ToArray();
        if (untracked.Length > 0) throw new CompilerException("RESOURCE_MANIFEST_INCOMPLETE", "资源 manifest 未覆盖文件: " + string.Join(", ", untracked));
        var scriptBase = root.GetProperty("scriptBase").GetProperty("path").GetString() ?? throw new CompilerException("RESOURCE_MANIFEST_INVALID", "缺少 scriptBase.path。");
        var runtimeValidated = root.TryGetProperty("validation", out var validation)
            && validation.TryGetProperty("vmRuntimeValidated", out var runtime)
            && runtime.ValueKind is JsonValueKind.True;
        var pending = validation.ValueKind == JsonValueKind.Object && validation.TryGetProperty("pending", out var pendingElement) && pendingElement.ValueKind == JsonValueKind.Array
            ? pendingElement.EnumerateArray().Select(x => x.GetString()).Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x!).ToArray()
            : [];
        return new(root.GetProperty("resourceVersion").GetString() ?? "", version, false, scriptBase, hashes, runtimeValidated, pending);
    }
    public string Materialize(string vmVersion, string outputFile)
    {
        var source = Path.Combine(RepositoryRoot, "resources", "vm", vmVersion, "script-base");
        var solutionFile = Path.Combine(source, "SolutionFile", "VmServer.xml");
        if (!File.Exists(solutionFile)) throw new CompilerException("SCRIPT_MODULE_TEMPLATE_MISSING", "解包脚本模板不完整。");
        Directory.CreateDirectory(Path.GetDirectoryName(outputFile)!);
        if (File.Exists(outputFile)) File.Delete(outputFile);
        using var file = new FileStream(outputFile, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None);
        using var archive = new System.IO.Compression.ZipArchive(file, System.IO.Compression.ZipArchiveMode.Create, false);
        foreach (var item in Directory.EnumerateFiles(source, "*", SearchOption.AllDirectories).OrderBy(x => x, StringComparer.Ordinal))
        {
            // VisionMaster 4.4 的 SOL 条目使用反斜杠；解析器能容忍正斜杠，但 VM 本身不能据此判定为兼容。
            var relative = Path.GetRelativePath(source, item).Replace('/', '\\');
            var entry = archive.CreateEntry(relative, System.IO.Compression.CompressionLevel.Optimal);
            using var input = File.OpenRead(item);
            using var output = entry.Open();
            input.CopyTo(output);
        }
        return outputFile;
    }
    public static string Hash(string path) => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)));
}

public sealed class ParserClient(string parserPath)
{
    // The bundled parser derives its extraction directory from the input file name. Since builds
    // intentionally use working-input.sol, parser processes must not overlap across processes on this machine.
    private static readonly Mutex ProcessGate = new(false, @"Local\VmScriptCompiler_VMSolutionParser");
    public ProcessResult Parse(string solutionFile, string outputJson)
    {
        var result = Run(["parse", "-f", solutionFile, "-o", outputJson]);
        if (result.ExitCode == 0 && File.Exists(outputJson))
        {
            var json = File.ReadAllText(outputJson);
            var normalized = System.Text.RegularExpressions.Regex.Replace(json, "(\\\"params\\\"\\s*:\\s*)\\[\\s*\\}", "$1[]", System.Text.RegularExpressions.RegexOptions.CultureInvariant);
            if (!ReferenceEquals(json, normalized) && json != normalized) File.WriteAllText(outputJson, normalized, new UTF8Encoding(false));
        }
        return result;
    }
    public ProcessResult Inspect(string solutionFile) => Run(["inspect", "-f", solutionFile, "--list"]);
    public ProcessResult Modify(string solutionFile, string changesFile, string outputFile) => Run(["modify", "-f", solutionFile, "-c", changesFile, "-o", outputFile]);
    private ProcessResult Run(IReadOnlyList<string> arguments)
    {
        var acquired = false;
        try
        {
            try { acquired = ProcessGate.WaitOne(TimeSpan.FromMinutes(5)); }
            catch (AbandonedMutexException) { acquired = true; }
            if (!acquired) throw new CompilerException("PARSER_BUSY", "VMSolutionParser is still busy after five minutes.");
            var info = new ProcessStartInfo(parserPath) { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false, CreateNoWindow = true };
            foreach (var argument in arguments) info.ArgumentList.Add(argument);
            using var process = Process.Start(info) ?? throw new CompilerException("PARSER_START_FAILED", "无法启动 VMSolutionParser。");
            var stdoutTask = process.StandardOutput.ReadToEndAsync();
            var stderrTask = process.StandardError.ReadToEndAsync();
            if (!process.WaitForExit(TimeSpan.FromMinutes(5)))
            {
                try { process.Kill(true); } catch { }
                throw new CompilerException("PARSER_TIMEOUT", "VMSolutionParser did not finish within five minutes.");
            }
            var stdout = stdoutTask.GetAwaiter().GetResult();
            var stderr = stderrTask.GetAwaiter().GetResult();
            return new(process.ExitCode, stdout, stderr);
        }
        finally { if (acquired) ProcessGate.ReleaseMutex(); }
    }
}

public static class SolArchiveValidator
{
    public static void ValidateVm44EntryNames(string solutionFile)
    {
        using var archive = System.IO.Compression.ZipFile.OpenRead(solutionFile);
        var names = archive.Entries.Select(x => x.FullName).ToArray();
        if (names.Any(x => x.Contains('/'))) throw new CompilerException("SOL_ENTRY_NAME_INCOMPATIBLE", "SOL contains forward-slash ZIP entries that are not VM 4.4 compatible.");
        foreach (var name in names)
        {
            var normalized = name.Replace('\\', '/');
            if (normalized.StartsWith('/') || normalized.Split('/', StringSplitOptions.RemoveEmptyEntries).Any(x => x == "..") || normalized.Contains(':'))
                throw new CompilerException("SOL_ENTRY_NAME_UNSAFE", "SOL contains an unsafe ZIP entry name: " + name);
        }
        if (names.Distinct(StringComparer.OrdinalIgnoreCase).Count() != names.Length)
            throw new CompilerException("SOL_ENTRY_NAME_UNSAFE", "SOL contains duplicate ZIP entry names.");
        foreach (var required in new[] { "SolutionFile\\VmServer.xml", "SolutionFile\\MoudleFrame", "SolutionFile\\GlobalScript_0" })
            if (!names.Contains(required, StringComparer.Ordinal)) throw new CompilerException("SOL_VALIDATION_FAILED", "SOL is missing required entry: " + required);
    }
}
public sealed class CompilerException(string code, string message) : Exception(message) { public string Code { get; } = code; }
