using System.Text.Json;

namespace VmScriptCompiler.Core;

public sealed record PlanResult(bool Ok, IReadOnlyList<ValidationIssue> Issues, IReadOnlyList<string> Actions);
public sealed record SolutionValidationResult(bool Ok, JsonElement ParseResult, string InspectOutput);

public sealed class CompilerFacade(string repositoryRoot)
{
    public string RepositoryRoot { get; } = Path.GetFullPath(repositoryRoot);

    public VmEnvironment DetectEnvironment() => new EnvironmentDetector().Detect();

    public PlanResult Plan(string specificationFile)
    {
        specificationFile = Path.GetFullPath(specificationFile);
        if (!File.Exists(specificationFile)) throw new CompilerException("SPECIFICATION_FILE_NOT_FOUND", "Requirement file does not exist: " + specificationFile);
        CompileRequirement requirement;
        RequirementValidationResult validation;
        try
        {
            using var document = JsonDocument.Parse(File.ReadAllText(specificationFile));
            validation = new RequirementValidator().Validate(document);
            if (!validation.IsValid) return new(false, validation.Issues, []);
            requirement = JsonSerializer.Deserialize<CompileRequirement>(document.RootElement.GetRawText(), JsonDefaults.Options)
                ?? throw new CompilerException("REQUIREMENT_SCHEMA_INVALID", "Requirement cannot be deserialized.");
        }
        catch (JsonException error)
        {
            return new(false, [new("REQUIREMENT_SCHEMA_INVALID", "Requirement is not valid JSON: " + error.Message, "$")], []);
        }
        var actions = new List<string> { "validateRequirement", "validateResources", "detectEnvironment" };
        actions.Add(requirement.Task.Mode == "patch" ? "copyAndInspectBaseSolution" : "materializeScriptBase");
        if (requirement.Scripts.Any(x => x.Carrier is "csharp-module" or "python-module"))
            actions.AddRange(["configureScriptModules", "writeModuleIo", "writeCSharpObjectBindings", "writeAndVerifyInputDefaults", "rebuildModuleFrame"]);
        if (requirement.Connections.Count > 0) actions.Add("writeExplicitConnections");
        if (requirement.Scripts.SelectMany(x => x.Dependencies).Any()) actions.Add("validateAndWriteDependencies");
        if (requirement.Scripts.Any(x => x.Carrier == "global-csharp")) actions.Add("writeGlobalScript");
        actions.AddRange(["offlineScriptPrecompile", "parseAndValidate", "inspectSolution", "writeContractAndReport"]);
        return new(true, validation.Issues, actions);
    }

    public BuildResult Build(string specificationFile, string outputDirectory) => new BuildService(RepositoryRoot).Build(Path.GetFullPath(specificationFile), Path.GetFullPath(outputDirectory));
    public BuildResult Patch(string baseSolution, string specificationFile, string outputDirectory) => new BuildService(RepositoryRoot).Patch(Path.GetFullPath(baseSolution), Path.GetFullPath(specificationFile), Path.GetFullPath(outputDirectory));

    public SolutionValidationResult ValidateSolution(string solutionFile)
    {
        solutionFile = Path.GetFullPath(solutionFile);
        if (!File.Exists(solutionFile)) throw new CompilerException("SOLUTION_FILE_NOT_FOUND", "SOL file does not exist: " + solutionFile);
        var temporary = Path.Combine(Path.GetTempPath(), "vm-script-compiler-" + Guid.NewGuid().ToString("N") + ".json");
        try
        {
            SolArchiveValidator.ValidateVm44EntryNames(solutionFile);
            var parser = new ParserClient(Path.Combine(RepositoryRoot, "tools", "vm-solution-parser", "VMSolutionParser.Cli.exe"));
            var result = parser.Parse(solutionFile, temporary);
            if (result.ExitCode != 0) throw new CompilerException("SOL_PARSE_FAILED", string.IsNullOrWhiteSpace(result.StandardError) ? result.StandardOutput : result.StandardError);
            var inspect = parser.Inspect(solutionFile);
            if (inspect.ExitCode != 0) throw new CompilerException("SOL_VALIDATION_FAILED", string.IsNullOrWhiteSpace(inspect.StandardError) ? inspect.StandardOutput : inspect.StandardError);
            using var document = JsonDocument.Parse(File.ReadAllText(temporary));
            var solution = document.RootElement.GetProperty("solution");
            if (solution.TryGetProperty("warnings", out var warnings) && warnings.ValueKind == JsonValueKind.Array && warnings.GetArrayLength() > 0)
                throw new CompilerException("SOL_VALIDATION_FAILED", "Parser returned structural warnings: " + string.Join("; ", warnings.EnumerateArray().Select(x => x.GetString())));
            return new(true, document.RootElement.Clone(), inspect.StandardOutput);
        }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }
}

public static class RepositoryLocator
{
    public static string Find(string? explicitRoot = null)
    {
        foreach (var candidate in new[] { explicitRoot, Environment.GetEnvironmentVariable("VM_SCRIPT_COMPILER_HOME"), Directory.GetCurrentDirectory(), AppContext.BaseDirectory })
        {
            if (string.IsNullOrWhiteSpace(candidate)) continue;
            for (var directory = new DirectoryInfo(Path.GetFullPath(candidate)); directory is not null; directory = directory.Parent)
                if (IsRepositoryRoot(directory.FullName)) return directory.FullName;
        }
        throw new CompilerException("COMPILER_HOME_NOT_FOUND", "Cannot locate VM Script Solution Compiler resources. Set VM_SCRIPT_COMPILER_HOME.");
    }

    private static bool IsRepositoryRoot(string path) =>
        File.Exists(Path.Combine(path, "schemas", "requirement.schema.json")) &&
        File.Exists(Path.Combine(path, "resources", "vm", "4.4.0", "manifest.json")) &&
        File.Exists(Path.Combine(path, "tools", "vm-solution-parser", "VMSolutionParser.Cli.exe"));
}
