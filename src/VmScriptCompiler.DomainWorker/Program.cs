using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using VmScriptCompiler.Core;

Console.InputEncoding = new UTF8Encoding(false);
Console.OutputEncoding = new UTF8Encoding(false);

var explicitRoot = Option(args, "--repository-root");
var repositoryRoot = RepositoryLocator.Find(explicitRoot);
var outputRoot = Option(args, "--output-root") ?? Path.Combine(repositoryRoot, "outputs");
var worker = new DomainWorker(new CompilerFacade(repositoryRoot), outputRoot);
await worker.RunAsync(Console.In, Console.Out);
return;

static string? Option(string[] values, string name) =>
    values.SkipWhile(x => !string.Equals(x, name, StringComparison.Ordinal)).Skip(1).FirstOrDefault();

internal sealed class DomainWorker(CompilerFacade compiler, string outputRoot)
{
    private const string ProtocolVersion = "1.0";
    private static readonly JsonSerializerOptions ProtocolJson = new(JsonDefaults.Options)
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    private bool _shutdownRequested;
    private readonly string _repositoryRoot = Path.GetFullPath(compiler.RepositoryRoot);
    private readonly string _outputRoot = Path.GetFullPath(outputRoot);

    public async Task RunAsync(TextReader input, TextWriter output)
    {
        while (!_shutdownRequested && await input.ReadLineAsync() is { } line)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            object response;
            JsonElement id = default;
            try
            {
                using var request = JsonDocument.Parse(line.TrimStart('\uFEFF'));
                if (request.RootElement.ValueKind != JsonValueKind.Object)
                    throw new CompilerException("INVALID_REQUEST", "Worker request must be a JSON object.");
                id = request.RootElement.TryGetProperty("id", out var requestId) ? requestId.Clone() : default;
                response = Success(id, Handle(request.RootElement));
            }
            catch (CompilerException error)
            {
                response = Failure(id, error.Code, error.Message, error.Details);
            }
            catch (JsonException error)
            {
                response = Failure(id, "INVALID_JSON", error.Message);
            }
            catch (Exception error)
            {
                await Console.Error.WriteLineAsync(error.ToString());
                response = Failure(id, "UNEXPECTED_ERROR", error.Message);
            }

            await output.WriteLineAsync(JsonSerializer.Serialize(response, ProtocolJson));
            await output.FlushAsync();
        }
    }

    private object Handle(JsonElement request)
    {
        var command = RequiredString(request, "command");
        var arguments = request.TryGetProperty("arguments", out var value) && value.ValueKind == JsonValueKind.Object
            ? value
            : EmptyObject();

        return command switch
        {
            "initialize" => Initialize(),
            "detect_environment" => compiler.DetectEnvironment(),
            "inspect_solution" => InspectSolution(RequiredString(arguments, "file")),
            "query_capability" => QueryCapability(OptionalString(arguments, "query"), OptionalString(arguments, "vmVersion") ?? "4.4.0"),
            "validate_requirement" => WithRequirement(arguments, compiler.Plan),
            "plan_solution" => WithRequirement(arguments, compiler.Plan),
            "build_solution" => WithRequirement(arguments, path => Build(path, RequiredString(arguments, "output"))),
            "patch_solution" => WithRequirement(arguments, path => Patch(
                RequiredString(arguments, "baseSolution"),
                path,
                RequiredString(arguments, "output"))),
            "validate_solution" => compiler.ValidateSolution(ExistingFile(RequiredString(arguments, "file"), "SOLUTION_FILE_NOT_FOUND", allowExternalSolution: true)),
            "read_build_report" => ReadBuildReport(RequiredString(arguments, "file")),
            "shutdown" => Shutdown(),
            _ => throw new CompilerException("COMMAND_NOT_FOUND", "Unknown domain command: " + command)
        };
    }

    private object Initialize() => new
    {
        protocolVersion = ProtocolVersion,
        product = "vm-script-solution-agent",
        productVersion = ProductInfo.Version,
        compilerRoot = compiler.RepositoryRoot,
        commands = new[]
        {
            "detect_environment", "inspect_solution", "query_capability",
            "validate_requirement", "plan_solution", "build_solution",
            "patch_solution", "validate_solution", "read_build_report", "shutdown"
        },
        invariants = new[]
        {
            "AI_NEVER_WRITES_SOL",
            "PATCH_NEVER_OVERWRITES_INPUT",
            "SUCCESS_REQUIRES_DETERMINISTIC_VALIDATION"
        }
    };

    private object InspectSolution(string file)
    {
        file = ExistingFile(file, "SOLUTION_FILE_NOT_FOUND", allowExternalSolution: true);
        SolArchiveValidator.ValidateVm44EntryNames(file);
        var parser = new ParserClient(Path.Combine(compiler.RepositoryRoot, "tools", "vm-solution-parser", "VMSolutionParser.Cli.exe"));
        var inspect = parser.Inspect(file);
        if (inspect.ExitCode != 0)
            throw new CompilerException("SOL_PARSE_FAILED", string.IsNullOrWhiteSpace(inspect.StandardError) ? inspect.StandardOutput : inspect.StandardError);
        return new
        {
            ok = true,
            file,
            sha256 = Hash(file),
            inspect = inspect.StandardOutput
        };
    }

    private object QueryCapability(string? query, string vmVersion)
    {
        var directory = Path.Combine(compiler.RepositoryRoot, "resources", "vm", vmVersion);
        if (!Directory.Exists(directory))
            throw new CompilerException("VM_RESOURCE_VERSION_UNSUPPORTED", "VM resource version is not available: " + vmVersion);

        using var types = JsonDocument.Parse(File.ReadAllText(Path.Combine(directory, "type-system.json")));
        using var api = JsonDocument.Parse(File.ReadAllText(Path.Combine(directory, "api-catalog.json")));
        var normalized = query?.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return new
            {
                vmVersion,
                typeSystem = types.RootElement.Clone(),
                apiCatalog = api.RootElement.Clone()
            };
        }

        var matches = new List<JsonElement>();
        CollectMatches(types.RootElement, normalized, matches);
        CollectMatches(api.RootElement, normalized, matches);
        return new
        {
            vmVersion,
            query = normalized,
            matches = matches.Take(100).ToArray(),
            truncated = matches.Count > 100
        };
    }

    private static void CollectMatches(JsonElement value, string query, List<JsonElement> matches)
    {
        switch (value.ValueKind)
        {
            case JsonValueKind.Object:
                if (value.GetRawText().Contains(query, StringComparison.OrdinalIgnoreCase))
                    matches.Add(value.Clone());
                else
                    foreach (var property in value.EnumerateObject()) CollectMatches(property.Value, query, matches);
                break;
            case JsonValueKind.Array:
                foreach (var item in value.EnumerateArray())
                    if (item.GetRawText().Contains(query, StringComparison.OrdinalIgnoreCase)) matches.Add(item.Clone());
                break;
        }
    }

    private static object WithRequirement(JsonElement arguments, Func<string, object> action)
    {
        if (!arguments.TryGetProperty("requirement", out var requirement) || requirement.ValueKind != JsonValueKind.Object)
            throw new CompilerException("INVALID_ARGUMENT", "Missing object argument: requirement");

        var temporary = Path.Combine(Path.GetTempPath(), "vm-script-domain-" + Guid.NewGuid().ToString("N") + ".json");
        try
        {
            File.WriteAllText(temporary, requirement.GetRawText(), new UTF8Encoding(false));
            return action(temporary);
        }
        finally
        {
            if (File.Exists(temporary)) File.Delete(temporary);
        }
    }

    private object Build(string specificationFile, string output)
    {
        var result = compiler.Build(specificationFile, FullDirectory(output));
        return BuildResult(result);
    }

    private object Patch(string baseSolution, string specificationFile, string output)
    {
        baseSolution = ExistingFile(baseSolution, "SOLUTION_FILE_NOT_FOUND", allowExternalSolution: true);
        var before = Hash(baseSolution);
        var result = compiler.Patch(baseSolution, specificationFile, FullDirectory(output));
        var after = Hash(baseSolution);
        if (!string.Equals(before, after, StringComparison.Ordinal))
            throw new CompilerException("BASE_SOLUTION_MODIFIED", "Patch modified the input SOL, which is forbidden.");
        return new
        {
            ok = true,
            result.TaskDirectory,
            result.SolutionFile,
            result.ReportFile,
            parseExitCode = result.Parse.ExitCode,
            inspectExitCode = result.Inspect.ExitCode,
            defaultPersistenceNotices = result.DefaultPersistenceNotices,
            offlineValidation = OfflineValidation(result),
            baseSolution,
            baseSolutionSha256 = before,
            inputPreserved = true
        };
    }

    private static object BuildResult(BuildResult result) => new
    {
        ok = true,
        result.TaskDirectory,
        result.SolutionFile,
        result.ReportFile,
        parseExitCode = result.Parse.ExitCode,
        inspectExitCode = result.Inspect.ExitCode,
        defaultPersistenceNotices = result.DefaultPersistenceNotices,
        offlineValidation = OfflineValidation(result)
    };

    private static object OfflineValidation(BuildResult result)
    {
        var parseFile = Path.Combine(result.TaskDirectory, "validation", "parse-result.json");
        JsonElement parse = default;
        if (File.Exists(parseFile))
        {
            using var document = JsonDocument.Parse(File.ReadAllText(parseFile));
            parse = document.RootElement.Clone();
        }
        var inspectFile = Path.Combine(result.TaskDirectory, "validation", "inspect-result.json");
        var inspect = File.Exists(inspectFile) ? File.ReadAllText(inspectFile) : result.Inspect.StandardOutput;
        return new { ok = result.Parse.ExitCode == 0 && result.Inspect.ExitCode == 0, parseResult = parse, inspectOutput = inspect };
    }

    private object ReadBuildReport(string file)
    {
        file = ExistingFile(file, "BUILD_REPORT_NOT_FOUND");
        var taskDirectory = Path.GetDirectoryName(file)!;
        return new
        {
            ok = true,
            file,
            content = File.ReadAllText(file),
            artifacts = Directory.EnumerateFiles(taskDirectory, "*", SearchOption.AllDirectories)
                .Select(path => Path.GetRelativePath(taskDirectory, path).Replace('\\', '/'))
                .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                .ToArray()
        };
    }

    private object Shutdown()
    {
        _shutdownRequested = true;
        return new { acknowledged = true };
    }

    private static object Success(JsonElement id, object result) => new
    {
        id = IdValue(id),
        ok = true,
        result,
        timestampUtc = DateTime.UtcNow
    };

    private static object Failure(JsonElement id, string code, string message, object? details = null) => new
    {
        id = IdValue(id),
        ok = false,
        error = new { code, message, details },
        timestampUtc = DateTime.UtcNow
    };

    private static object? IdValue(JsonElement id) => id.ValueKind switch
    {
        JsonValueKind.String => id.GetString(),
        JsonValueKind.Number when id.TryGetInt64(out var number) => number,
        JsonValueKind.True => true,
        JsonValueKind.False => false,
        _ => null
    };

    private static JsonElement EmptyObject()
    {
        using var document = JsonDocument.Parse("{}");
        return document.RootElement.Clone();
    }

    private static string RequiredString(JsonElement value, string name)
    {
        if (value.ValueKind == JsonValueKind.Object &&
            value.TryGetProperty(name, out var property) &&
            property.ValueKind == JsonValueKind.String &&
            !string.IsNullOrWhiteSpace(property.GetString()))
            return property.GetString()!;
        throw new CompilerException("INVALID_ARGUMENT", "Missing string argument: " + name);
    }

    private static string? OptionalString(JsonElement value, string name) =>
        value.ValueKind == JsonValueKind.Object &&
        value.TryGetProperty(name, out var property) &&
        property.ValueKind == JsonValueKind.String
            ? property.GetString()
            : null;

    private string ExistingFile(string path, string code, bool allowExternalSolution = false)
    {
        path = Path.GetFullPath(path);
        if (!File.Exists(path)) throw new CompilerException(code, "File does not exist: " + path);
        if (!allowExternalSolution && !IsWithinAllowedRoot(path))
            throw new CompilerException("PATH_OUTSIDE_ALLOWED_ROOT", "Worker can only access files under the configured repository or output root: " + path);
        if (allowExternalSolution && !string.Equals(Path.GetExtension(path), ".sol", StringComparison.OrdinalIgnoreCase))
            throw new CompilerException("SOLUTION_FILE_INVALID", "External solution access is limited to .sol files: " + path);
        return path;
    }

    private string FullDirectory(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) throw new CompilerException("INVALID_ARGUMENT", "Output directory is required.");
        path = Path.GetFullPath(path);
        if (!IsWithin(path, _outputRoot))
            throw new CompilerException("OUTPUT_PATH_OUTSIDE_ROOT", "Build output must stay under the configured Agent output root: " + path);
        return path;
    }

    private bool IsWithinAllowedRoot(string path) => IsWithin(path, _repositoryRoot) || IsWithin(path, _outputRoot);

    private static bool IsWithin(string path, string root)
    {
        var normalizedPath = Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var normalizedRoot = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        return string.Equals(normalizedPath, normalizedRoot, StringComparison.OrdinalIgnoreCase) ||
            normalizedPath.StartsWith(normalizedRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) ||
            normalizedPath.StartsWith(normalizedRoot + Path.AltDirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
    }

    private static string Hash(string file) =>
        Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(file)));
}
