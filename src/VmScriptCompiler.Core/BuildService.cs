using System.Text.Json;

namespace VmScriptCompiler.Core;

public sealed class BuildService(string repositoryRoot)
{
    private readonly string _repositoryRoot = Path.GetFullPath(repositoryRoot);

    public BuildResult Build(string specificationFile, string outputDirectory) => Execute(specificationFile, outputDirectory, "create", null);
    public BuildResult Patch(string baseSolution, string specificationFile, string outputDirectory) => Execute(specificationFile, outputDirectory, "patch", baseSolution);

    private BuildResult Execute(string specificationFile, string outputDirectory, string expectedMode, string? baseSolution)
    {
        var loaded = RequirementLoader.Load(specificationFile);
        var requirement = loaded.Requirement;
        var validation = loaded.Validation;
        if (requirement.Task.Mode != expectedMode) throw new CompilerException("COMPILE_MODE_MISMATCH", $"Command requires {expectedMode} mode but Requirement uses {requirement.Task.Mode}.");
        ValidateScriptSet(requirement.Scripts);
        string? resolvedBaseSolution = null;
        if (expectedMode == "patch")
        {
            var specificationDirectory = Path.GetDirectoryName(Path.GetFullPath(specificationFile))!;
            var declared = requirement.Task.BaseSolution;
            var declaredPath = string.IsNullOrWhiteSpace(declared) ? null : Path.GetFullPath(Path.IsPathRooted(declared) ? declared : Path.Combine(specificationDirectory, declared));
            var suppliedPath = Path.GetFullPath(baseSolution ?? declaredPath ?? throw new CompilerException("BASE_SOLUTION_NOT_FOUND", "Patch requires a command base SOL or task.baseSolution."));
            if (declaredPath is not null && !string.Equals(declaredPath, suppliedPath, StringComparison.OrdinalIgnoreCase))
                throw new CompilerException("BASE_SOLUTION_MISMATCH", $"Patch command base SOL does not match task.baseSolution. Command: {suppliedPath}; Requirement: {declaredPath}.");
            resolvedBaseSolution = suppliedPath;
        }

        var resources = new ResourceManager(_repositoryRoot);
        var manifest = resources.LoadAndValidate(requirement.Task.VmVersion);
        var environment = new EnvironmentDetector().Detect();
        if (!environment.Found) throw new CompilerException(environment.ErrorCode!, "VisionMaster installation was not detected.");
        if (!string.Equals(environment.Version, requirement.Task.VmVersion, StringComparison.Ordinal)) throw new CompilerException("VM_VERSION_MISMATCH", $"Detected VM {environment.Version}; Requirement requests {requirement.Task.VmVersion}.");

        var taskId = MakeTaskId(requirement.Task.Name);
        var taskDirectory = Path.Combine(Path.GetFullPath(outputDirectory), taskId);
        if (Directory.Exists(taskDirectory)) throw new CompilerException("TASK_DIRECTORY_EXISTS", "Task directory already exists: " + taskDirectory);
        var validationDirectory = Path.Combine(taskDirectory, "validation");
        var generatedDirectory = Path.Combine(taskDirectory, "generated");
        Directory.CreateDirectory(validationDirectory);
        Directory.CreateDirectory(generatedDirectory);
        File.Copy(specificationFile, Path.Combine(taskDirectory, "requirement.json"));
        var working = Path.Combine(taskDirectory, "working-input.sol");
        var solution = Path.Combine(taskDirectory, "result.sol");
        string? temporaryTemplate = null;
        var buildSucceeded = false;
        try
        {
            var dependencyResolution = new DotNetDependencyResolver(_repositoryRoot, environment.VmRoot!)
                .Resolve(requirement.Scripts, specificationFile, taskDirectory, validationDirectory);

            var parser = new ParserClient(Path.Combine(_repositoryRoot, "tools", "vm-solution-parser", "VMSolutionParser.Cli.exe"));
        var baselineWarnings = new HashSet<string>(StringComparer.Ordinal);
        if (expectedMode == "create") resources.Materialize(requirement.Task.VmVersion, working);
        else
        {
            var source = resolvedBaseSolution!;
            if (!File.Exists(source)) throw new CompilerException("BASE_SOLUTION_NOT_FOUND", "Patch base SOL does not exist: " + source);
            SolArchiveValidator.ValidateVm44EntryNames(source);
            File.Copy(source, working);
            var baselineParseFile = Path.Combine(validationDirectory, "base-structural-parse.json");
            var baselineParse = parser.Parse(working, baselineParseFile);
            if (baselineParse.ExitCode != 0) throw new CompilerException("SOL_PARSE_FAILED", "Patch base SOL failed structural parsing.");
            using var baselineDocument = JsonDocument.Parse(File.ReadAllText(baselineParseFile));
            if (baselineDocument.RootElement.GetProperty("solution").TryGetProperty("warnings", out var warnings) && warnings.ValueKind == JsonValueKind.Array)
                foreach (var warning in warnings.EnumerateArray()) baselineWarnings.Add(warning.GetString()!);
        }

        var template = expectedMode == "create" ? working : resources.Materialize(requirement.Task.VmVersion, Path.Combine(taskDirectory, "module-template.sol"));
        temporaryTemplate = template == working ? null : template;
        var moduleCompiler = new ModuleScriptCompiler(_repositoryRoot, parser);
        var moduleResult = moduleCompiler.Compile(working, solution, template, requirement.Scripts, requirement.Connections, expectedMode, generatedDirectory, validationDirectory);

        GlobalScriptArtifact? globalArtifact = null;
        var global = requirement.Scripts.SingleOrDefault(x => x.Carrier == "global-csharp");
        if (global is not null) globalArtifact = new GlobalScriptCompiler(_repositoryRoot).Compile(solution, generatedDirectory, requirement.Task.VmVersion, global);
        VmServerDefaultWriter.Apply(solution, requirement.Scripts);
        new ScriptPrecompiler(environment.VmRoot!, dependencyResolution).Validate(requirement.Scripts, moduleResult.Artifacts, globalArtifact, validationDirectory);

        WritePlan(taskDirectory, expectedMode, requirement.Scripts);
        WriteContract(taskDirectory, expectedMode, requirement.Scripts, moduleResult.Artifacts, globalArtifact, dependencyResolution, manifest.RuntimeValidated, manifest.RuntimeValidationPending);

        var parseFile = Path.Combine(validationDirectory, "parse-result.json");
        var parse = parser.Parse(solution, parseFile);
        var inspect = parser.Inspect(solution);
        File.WriteAllText(Path.Combine(validationDirectory, "inspect-result.json"), inspect.StandardOutput);
        if (parse.ExitCode != 0 || inspect.ExitCode != 0) throw new CompilerException("SOL_PARSE_FAILED", "Built SOL failed parse/inspect validation.");
        SolArchiveValidator.ValidateVm44EntryNames(solution);
        ValidateBuiltSolution(parseFile, requirement, expectedMode, baselineWarnings);

        var report = Path.Combine(taskDirectory, "build-report.md");
        File.WriteAllText(report, Report(taskId, expectedMode, environment, manifest, specificationFile, resolvedBaseSolution, solution, parse, inspect, requirement.Scripts, dependencyResolution));
        buildSucceeded = true;
        return new(taskDirectory, solution, report, validation, parse, inspect);
        }
        finally
        {
            SafeDelete(working);
            if (temporaryTemplate is not null) SafeDelete(temporaryTemplate);
            if (!buildSucceeded) SafeDelete(solution);
        }
    }

    private static void ValidateScriptSet(IReadOnlyList<ScriptRequirement> scripts)
    {
        if (scripts.Count(x => x.Carrier == "global-csharp") > 1) throw new CompilerException("MULTIPLE_GLOBAL_SCRIPTS", "VM 4.4 supports one GlobalScript_0 carrier.");
        foreach (var global in scripts.Where(x => x.Carrier == "global-csharp"))
        {
            if (global.Dependencies.Count > 0) throw new CompilerException("REFERENCE_TYPE_UNCONFIRMED", "GlobalScript references require a verified refrencesType mapping.");
        }
    }

    private static void ValidateBuiltSolution(string parseFile, CompileRequirement requirement, string mode, HashSet<string> baselineWarnings)
    {
        using var document = JsonDocument.Parse(File.ReadAllText(parseFile));
        var solution = document.RootElement.GetProperty("solution");
        if (solution.TryGetProperty("warnings", out var warnings) && warnings.ValueKind == JsonValueKind.Array)
        {
            var newWarnings = warnings.EnumerateArray().Select(x => x.GetString()!).Where(x => !baselineWarnings.Contains(x)).ToArray();
            if (newWarnings.Length > 0) throw new CompilerException("SOL_VALIDATION_FAILED", "Parser returned new structural warnings: " + string.Join("; ", newWarnings));
        }
        var modules = solution.GetProperty("procedures").EnumerateArray()
            .SelectMany(p => p.GetProperty("modules").EnumerateArray().Select(m => new
            {
                Procedure = p.GetProperty("displayName").GetString(),
                Name = m.GetProperty("displayName").GetString(),
                Type = m.GetProperty("name").GetString(),
                ModuleId = m.GetProperty("moduleId").GetInt32(),
                Json = m.Clone()
            })).ToArray();
        foreach (var script in requirement.Scripts.Where(x => x.Carrier is "csharp-module" or "python-module"))
        {
            var expectedType = script.Carrier == "python-module" ? "PyShellModule" : "ShellModule";
            var module = modules.SingleOrDefault(x => x.Procedure == script.Procedure && x.Name == script.Name && x.Type == expectedType);
            if (module is null)
                throw new CompilerException("SOL_VALIDATION_FAILED", "Expected script module is missing: " + script.Procedure + "." + script.Name);
            var relations = module.Json.GetProperty("subscriptions").EnumerateArray()
                .Select(x => x.GetProperty("relationString").GetString()).ToHashSet(StringComparer.Ordinal);
            foreach (var input in script.Inputs)
            {
                if (!VmServerDefaultWriter.TryFormatPersistedDefault(input, out var value)) continue;
                var expectedRelation = $"{module.ModuleId} . %{input.Name}% . 0 . {value} . 1 . 0 . All . 1";
                if (!relations.Contains(expectedRelation))
                    throw new CompilerException("SOL_DEFAULT_VALIDATION_FAILED", "Input default relation is missing or changed: " + script.Procedure + "." + script.Name + "." + input.Name);
            }
            if (script.Carrier == "csharp-module")
            {
                var referencePayload = module.Json.GetProperty("binaryParams").EnumerateArray()
                    .SingleOrDefault(x => x.GetProperty("name").GetString() == "ShellRefrences");
                foreach (var dependency in script.Dependencies.Where(x => x.Kind == "dotnet-assembly"))
                {
                    if (referencePayload.ValueKind == JsonValueKind.Undefined || !referencePayload.GetProperty("parsed").GetString()!.Contains(dependency.Name + "\n", StringComparison.OrdinalIgnoreCase))
                        throw new CompilerException("SOL_REFERENCE_VALIDATION_FAILED", "ShellRefrences is missing declared assembly: " + dependency.Name);
                }
            }
        }
        if (mode == "create")
        {
            var expected = requirement.Scripts.Count(x => x.Carrier is "csharp-module" or "python-module");
            if (modules.Length != expected) throw new CompilerException("SOL_VALIDATION_FAILED", $"Create result contains {modules.Length} modules; expected {expected}.");
        }
        if (requirement.Scripts.Any(x => x.Carrier == "global-csharp") && solution.GetProperty("globalScript").ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
            throw new CompilerException("SOL_VALIDATION_FAILED", "GlobalScript payload is missing.");
        var procedureNames = solution.GetProperty("procedures").EnumerateArray().Select(x => x.GetProperty("displayName").GetString()).ToHashSet(StringComparer.Ordinal);
        foreach (var operation in requirement.Scripts.Where(x => x.Carrier == "global-csharp").SelectMany(x => x.Operations)
            .Where(x => x.Kind is "runProcedure" or "continuousProcedure" or "stopProcedure" or "setProcedureInput"))
            if (!procedureNames.Contains(operation.Procedure))
                throw new CompilerException("PROCEDURE_NOT_FOUND", "GlobalScript operation references a procedure missing from the final SOL: " + operation.Procedure);
    }

    private static void WritePlan(string taskDirectory, string mode, IReadOnlyList<ScriptRequirement> scripts)
    {
        var actions = new List<string> { "validateResources", mode == "create" ? "materializeScriptBase" : "copyBaseSolution" };
        if (scripts.Any(x => x.Carrier is "csharp-module" or "python-module")) actions.AddRange(["configureScriptModules", "writeModuleIo", "writeCSharpObjectBindings", "writeAndVerifyInputDefaults", "rebuildModuleFrame"]);
        if (scripts.SelectMany(x => x.Dependencies).Any()) actions.Add("validateAndWriteDependencies");
        if (scripts.Any(x => x.Carrier == "global-csharp")) actions.Add("writeGlobalScript");
        actions.AddRange(["offlineScriptPrecompile", "parseAndValidate", "inspectSolution"]);
        File.WriteAllText(Path.Combine(taskDirectory, "build-plan.json"), JsonSerializer.Serialize(new { actions = actions.Select(x => new { kind = x }) }, JsonDefaults.Options));
    }

    private static void WriteContract(string taskDirectory, string mode, IReadOnlyList<ScriptRequirement> requirements, IReadOnlyList<ModuleScriptArtifact> modules, GlobalScriptArtifact? global, DotNetDependencyResolution dependencyResolution, bool runtimeValidated, IReadOnlyList<string> runtimeValidationPending)
    {
        var scripts = new List<object>();
        if (global is not null) scripts.Add(new { carrier = "global-csharp", source = "generated/GlobalScript.cs", solEntry = global.CarrierFile, references = global.References });
        scripts.AddRange(modules.Select(x =>
        {
            var requirement = requirements.Single(r => r.Id == x.Id);
            var dotNetDependencies = requirement.Dependencies.Where(d => d.Kind == "dotnet-assembly").Select(d => d.Name).ToArray();
            return (object)new { x.Id, x.Carrier, x.Procedure, x.Name, source = "generated/" + Path.GetFileName(x.SourceFile), inputs = x.Inputs, outputs = x.Outputs, dependencies = requirement.Dependencies,
                shellReferences = new { mode = dotNetDependencies.Length == 0 ? "vm-implicit-defaults" : "explicit-shell-refrences-payload", declared = dotNetDependencies } };
        }));
        var contract = new { mode, framework = ".NET Framework 4.6.1", architecture = "x64", scripts, dependencyResolution = dependencyResolution.Scripts, sourcePrecompiled = true, precompileEnvironment = "VM 4.4 method assemblies and bundled Python parser", ioMapping = "VM-4.4 saved-sample logical ports + StructName backing fields + DynamicIO combinations + UiParamData object mappings", inputDefaults = "int/float ModuleSubscribe mapping confirmed by VM-saved round trip; bool/string structurally encoded; Python also emits deterministic None fallback", runtimeValidated, runtimeValidation = new { baselineValidated = runtimeValidated, pending = runtimeValidationPending } };
        File.WriteAllText(Path.Combine(taskDirectory, "script-contract.json"), JsonSerializer.Serialize(contract, JsonDefaults.Options));
    }

    private static string MakeTaskId(string name) => $"{DateTimeOffset.UtcNow:yyyyMMddHHmmssfff}-{string.Concat(name.Select(c => char.IsLetterOrDigit(c) || c is '-' or '_' ? c : '-')).Trim('-')}";
    private static void SafeDelete(string path) { if (File.Exists(path)) File.Delete(path); }
    private static string Report(string taskId, string mode, VmEnvironment environment, ResourceManifest manifest, string spec, string? baseSolution, string sol, ProcessResult parse, ProcessResult inspect, IReadOnlyList<ScriptRequirement> scripts, DotNetDependencyResolution dependencyResolution)
    {
        var assemblies = dependencyResolution.Scripts.SelectMany(x => x.Assemblies).ToArray();
        var deployment = assemblies.Where(x => !x.RuntimeVisible).Select(x => x.Name + " -> " + x.DeploymentTarget).DefaultIfEmpty("none");
        var explicitShellReferences = scripts.Where(x => x.Carrier == "csharp-module")
            .Select(x => new { x.Name, Dependencies = x.Dependencies.Where(d => d.Kind == "dotnet-assembly").Select(d => d.Name).ToArray() })
            .Where(x => x.Dependencies.Length > 0).Select(x => x.Name + ": " + string.Join(", ", x.Dependencies)).ToArray();
        var shellReferenceStatus = explicitShellReferences.Length == 0
            ? "not emitted; VM default references remain implicit"
            : "emitted (catalog defaults plus declared references) for " + string.Join("; ", explicitShellReferences);
        var pendingRuntime = manifest.RuntimeValidationPending.Count == 0 ? "none" : string.Join("; ", manifest.RuntimeValidationPending);
        var dllValidation = assemblies.Length == 0 ? "none" : string.Join(", ", assemblies.Select(x => $"{x.Name} role={x.Role} refType={x.ReferenceType} {x.AssemblyVersion} {x.Architecture} SHA-256={x.Sha256 ?? "framework"}"));
        var lines = new List<string>
        {
            "# VM Script Solution Compiler build report", "",
            $"- Task: `{taskId}`",
            "- Compiler phase: `complete`",
            $"- Compiler version: `{ProductInfo.Version}`",
            $"- Mode: `{mode}`",
            $"- Requirement SHA-256: `{ResourceManager.Hash(spec)}`",
            $"- Base SOL: `{baseSolution ?? "none"}`",
            $"- Base SOL SHA-256: `{(baseSolution is null ? "none" : ResourceManager.Hash(baseSolution))}`",
            $"- Result SHA-256: `{ResourceManager.Hash(sol)}`",
            "- Determinism: Requirement semantics, generated source, module structure, and compiler AssemblyGuid are deterministic; task id and ZIP entry timestamps are build-specific, so byte hashes may differ across equivalent builds",
            $"- VM root: `{environment.VmRoot}`",
            $"- VM version: `{environment.Version}`",
            $"- VM architecture: `x64`",
            $"- Platform SDK root: `{environment.Development}`",
            $"- Algorithm SDK root: `{environment.AlgorithmSdk}`",
            $"- GlobalScript available: `{environment.GlobalScriptAvailable}`",
            $"- Resource version: `{manifest.ResourceVersion}`",
            $"- Resource VM version: `{manifest.VisionMasterVersion}`",
            $"- Script carriers: `{string.Join(", ", scripts.Select(x => x.Carrier))}`",
            $"- Declared dependencies: `{string.Join(", ", scripts.SelectMany(x => x.Dependencies).Select(x => x.Name).DefaultIfEmpty("none"))}`",
            $"- ShellRefrences payload: `{shellReferenceStatus}`",
            $"- DLL validation: `{dllValidation}`",
            $"- DLL deployment required: `{string.Join("; ", deployment)}`",
            "- Dependency manifest: `validation/dependency-manifest.json` (external files are packaged under `dependencies/<script-id>/`; when deployment is needed, run `dependencies/deploy-to-vm.ps1` explicitly as an administrator)",
            "- Dependency safety: the compiler and Desktop never modify the VM installation automatically",
            "- Dependency validation: assembly role, verified/user-declared ShellRefrences type, managed identity, version, architecture, target framework, direct reference graph, runtime visibility, and VM Python package probe",
            "- Script precompile: passed against VM 4.4 method assemblies / bundled Python syntax parser; C# port properties are a generated contract and still require VM precompile confirmation",
            $"- Parser exit code: `{parse.ExitCode}`",
            $"- Inspect exit code: `{inspect.ExitCode}`",
            "- DynamicIO validation: complex C# ports use VM-saved uppercase logical types, CR-separated `StructName` backing fields, nested DynamicIO `Combination` nodes, array type metadata, image object mappings, and a non-empty AssemblyGuid",
            "- Input defaults: int/float `ModuleSubscribe` round-trip confirmed; bool/string structurally encoded; Python also has deterministic `None` fallback",
            $"- VM runtime baseline validation: `{(manifest.RuntimeValidated ? "passed by user verification in VisionMaster 4.4" : "pending user verification")}`",
            $"- VM runtime validation pending: `{pendingRuntime}`", ""
        };
        return string.Join(Environment.NewLine, lines);
    }
}
