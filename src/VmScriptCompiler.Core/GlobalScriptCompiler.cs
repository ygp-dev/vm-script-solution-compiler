using System.IO.Compression;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace VmScriptCompiler.Core;

public sealed record GlobalScriptArtifact(string SourceFile, string CarrierFile, IReadOnlyList<string> References);

public sealed class GlobalScriptCompiler(string repositoryRoot)
{
    private readonly string _repositoryRoot = Path.GetFullPath(repositoryRoot);

    public GlobalScriptArtifact Compile(string solutionFile, string generatedDirectory, string vmVersion, ScriptRequirement? requirement = null)
    {
        Directory.CreateDirectory(generatedDirectory);
        var sourceTemplate = Path.Combine(_repositoryRoot, "resources", "vm", vmVersion, "global-script", "GlobalScript.txt");
        var baselineCarrier = Path.Combine(_repositoryRoot, "resources", "vm", vmVersion, "script-base", "SolutionFile", "GlobalScript_0");
        if (!File.Exists(sourceTemplate) || !File.Exists(baselineCarrier))
            throw new CompilerException("GLOBAL_SCRIPT_TEMPLATE_MISSING", "全局脚本模板或基线载荷缺失。");

        var source = string.IsNullOrWhiteSpace(requirement?.Source)
            ? requirement is { Operations.Count: > 0 } ? DeterministicScriptGenerator.Generate(requirement) : File.ReadAllText(sourceTemplate, Encoding.UTF8)
            : requirement.Source!;
        ValidateSourceContract(source);
        var sourceFile = Path.Combine(generatedDirectory, "GlobalScript.cs");
        File.WriteAllText(sourceFile, source, new UTF8Encoding(false));

        var baseline = ReadCarrier(baselineCarrier);
        var carrier = ReadCarrierFromSolution(solutionFile);
        carrier["Version"] = baseline["Version"]?.DeepClone();
        carrier["ScriptContent"] = source;
        carrier["ScriptPassword"] = "";
        carrier["ScriptRefences"] = MergeReferences(carrier["ScriptRefences"] as JsonArray, baseline["ScriptRefences"] as JsonArray);
        WriteCarrier(solutionFile, carrier);

        var references = carrier["ScriptRefences"]?.AsArray()
            .Select(x => x?["Name"]?.GetValue<string>())
            .Where(x => !string.IsNullOrWhiteSpace(x)).Cast<string>().ToArray() ?? [];
        return new(sourceFile, "SolutionFile/GlobalScript_0", references);
    }

    private static void ValidateSourceContract(string source)
    {
        if (!source.Contains("UserGlobalScript", StringComparison.Ordinal) ||
            !source.Contains("UserGlobalMethods", StringComparison.Ordinal) ||
            !source.Contains("IScriptMethods", StringComparison.Ordinal) ||
            !source.Contains("int Init()", StringComparison.Ordinal) ||
            !source.Contains("int Process()", StringComparison.Ordinal))
            throw new CompilerException("GLOBAL_SCRIPT_CONTRACT_INVALID", "全局脚本未满足 VM 4.4 入口契约。");
    }

    private static JsonObject ReadCarrier(string path)
    {
        var bytes = File.ReadAllBytes(path);
        var length = bytes.Length > 0 && bytes[^1] == 0 ? bytes.Length - 1 : bytes.Length;
        return JsonNode.Parse(bytes.AsSpan(0, length))?.AsObject()
            ?? throw new CompilerException("GLOBAL_SCRIPT_TEMPLATE_INVALID", "无法解析 GlobalScript_0。");
    }

    private static JsonObject ReadCarrierFromSolution(string solutionFile)
    {
        using var archive = ZipFile.OpenRead(solutionFile);
        var entry = archive.Entries.FirstOrDefault(x => Normalize(x.FullName) == "SolutionFile/GlobalScript_0")
            ?? throw new CompilerException("GLOBAL_SCRIPT_ENTRY_MISSING", "SOL 中缺少 SolutionFile/GlobalScript_0。");
        using var stream = entry.Open();
        using var memory = new MemoryStream();
        stream.CopyTo(memory);
        var bytes = memory.ToArray();
        var length = bytes.Length > 0 && bytes[^1] == 0 ? bytes.Length - 1 : bytes.Length;
        return JsonNode.Parse(bytes.AsSpan(0, length))?.AsObject()
            ?? throw new CompilerException("GLOBAL_SCRIPT_TEMPLATE_INVALID", "无法解析 SOL 中的 GlobalScript_0。");
    }

    private static JsonArray MergeReferences(JsonArray? existing, JsonArray? baseline)
    {
        var result = new JsonArray();
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in (existing ?? []).Concat(baseline ?? []))
        {
            var name = item?["Name"]?.GetValue<string>();
            if (!string.IsNullOrWhiteSpace(name) && names.Add(name)) result.Add(item!.DeepClone());
        }
        return result;
    }

    private static void WriteCarrier(string solutionFile, JsonObject payload)
    {
        using var archive = ZipFile.Open(solutionFile, ZipArchiveMode.Update);
        var entry = archive.Entries.FirstOrDefault(x => Normalize(x.FullName) == "SolutionFile/GlobalScript_0")
            ?? throw new CompilerException("GLOBAL_SCRIPT_ENTRY_MISSING", "SOL 中缺少 SolutionFile/GlobalScript_0。");
        var entryName = entry.FullName;
        entry.Delete();
        // 保留原 SOL 的 VM 兼容条目分隔符，不能把反斜杠静默改成正斜杠。
        var replacement = archive.CreateEntry(entryName, CompressionLevel.Optimal);
        using var stream = replacement.Open();
        var json = JsonSerializer.SerializeToUtf8Bytes(payload, JsonDefaults.Options);
        stream.Write(json);
        stream.WriteByte(0);
    }

    private static string Normalize(string path) => path.Replace('\\', '/').TrimStart('/');
}
