using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace VmScriptCompiler.Core;

public sealed class ScriptPrecompiler(string vmRoot, DotNetDependencyResolution? dependencyResolution = null)
{
    private readonly string _vmRoot = vmRoot;
    private readonly DotNetDependencyResolution _dependencyResolution = dependencyResolution ?? DotNetDependencyResolution.Empty;

    public void Validate(IReadOnlyList<ScriptRequirement> requirements, IReadOnlyList<ModuleScriptArtifact> modules, GlobalScriptArtifact? global, string validationDirectory)
    {
        var reports = new List<object>();
        foreach (var module in modules)
        {
            var requirement = requirements.Single(x => x.Id == module.Id);
            if (module.Carrier == "csharp-module") reports.Add(CompileModule(requirement, module.SourceFile, validationDirectory));
            else reports.Add(CompilePython(requirement, module.SourceFile));
        }
        if (global is not null) reports.Add(CompileGlobal(global, validationDirectory));
        File.WriteAllText(Path.Combine(validationDirectory, "script-precompile.json"), JsonSerializer.Serialize(new { ok = true, scripts = reports }, JsonDefaults.Options));
    }

    private object CompileModule(ScriptRequirement requirement, string source, string validationDirectory)
    {
        var methods = Path.Combine(_vmRoot, "Applications", "Module(sp)", "x64", "Logic", "ShellModule", "Script.Methods.dll");
        RequireFile(methods, "SCRIPT_METHODS_ASSEMBLY_MISSING");
        var references = new List<string> { methods };
        foreach (var dependency in requirement.Dependencies.Where(x => x.Kind == "dotnet-assembly"))
        {
            var reference = _dependencyResolution.Scripts.Count == 0
                ? ResolveModuleReference(dependency)
                : _dependencyResolution.ResolveCompilerReference(requirement.Id, dependency);
            references.Add(ResolveFrameworkReferenceIfNeeded(reference));
        }
        var companion = Path.Combine(validationDirectory, requirement.Id + ".properties.cs");
        File.WriteAllText(companion, PropertyCompanion(requirement), new UTF8Encoding(false));
        var result = CompileCSharp(requirement.Id, [source, companion], references, validationDirectory);
        return new { id = requirement.Id, carrier = requirement.Carrier, compiler = "Framework64 csc", dependencies = requirement.Dependencies.Select(x => x.Name), result.ExitCode, result.StandardOutput, result.StandardError };
    }

    private object CompileGlobal(GlobalScriptArtifact global, string validationDirectory)
    {
        var references = global.References.Select(ResolveGlobalReference).Where(x => x is not null).Cast<string>().Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        var result = CompileCSharp("global-csharp", [global.SourceFile], references, validationDirectory);
        return new { id = "global-csharp", carrier = "global-csharp", compiler = "Framework64 csc", dependencies = global.References, result.ExitCode, result.StandardOutput, result.StandardError };
    }

    private string? ResolveGlobalReference(string name)
    {
        if (name.Equals("mscorlib.dll", StringComparison.OrdinalIgnoreCase)) return null;
        if (name.StartsWith("System", StringComparison.OrdinalIgnoreCase)) return ResolveFrameworkReference(name);
        var applications = Path.Combine(_vmRoot, "Applications");
        string[] candidates = name switch
        {
            "VM.GlobalScript.Methods.dll" or "iMVS-6000PlatformSDKCS.dll" or "Apps.Json.dll" => [Path.Combine(applications, "GlobalScript", name)],
            "VM.Core.dll" or "VMControls.BaseInterface.dll" or "VMControls.Interface.dll" or "VMControls.RenderInterface.dll" => [Path.Combine(applications, "myLibs", name)],
            "VM.PlatformSDKCS.dll" => [Path.Combine(applications, "PublicFile", "x64", name)],
            "ImageSourceModuleCs.dll" => [Path.Combine(applications, "Module(sp)", "x64", "Collection", "ImageSourceModule", name)],
            "IMVSFastFeatureMatchModuCs.dll" => [Path.Combine(applications, "Module(sp)", "x64", "Location", "IMVSFastFeatureMatchModu", name), Path.Combine(applications, "Module(sp)", "x64", "Logic", "ShellModule", "DLL", name)],
            _ => Array.Empty<string>()
        };
        var path = candidates.FirstOrDefault(File.Exists);
        if (path is null) throw new CompilerException("GLOBAL_SCRIPT_REFERENCE_MISSING", "GlobalScript baseline reference is not available in its verified VM 4.4 location: " + name);
        return path;
    }

    private ProcessResult CompileCSharp(string id, IReadOnlyList<string> sources, IReadOnlyList<string> referenceAssemblies, string validationDirectory)
    {
        var csc = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Microsoft.NET", "Framework64", "v4.0.30319", "csc.exe");
        RequireFile(csc, "CSHARP_COMPILER_MISSING");
        var output = Path.Combine(validationDirectory, id + ".precompile.dll");
        // Framework csc consumes csc.rsp, whose framework references are relative.
        // Pin its working directory so a self-contained Desktop folder cannot shadow
        // System.Drawing.dll (or another framework assembly) with a .NET 8 facade.
        var info = new ProcessStartInfo(csc)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = Path.GetDirectoryName(csc)!
        };
        foreach (var argument in new[] { "/nologo", "/target:library", "/platform:x64", "/out:" + output }) info.ArgumentList.Add(argument);
        var references = new[] { "System.dll", "System.Core.dll", "System.Drawing.dll", "System.Windows.Forms.dll" }
            .Select(ResolveFrameworkReference)
            .Concat(referenceAssemblies.Select(ResolveFrameworkReferenceIfNeeded))
            .Distinct(StringComparer.OrdinalIgnoreCase);
        foreach (var reference in references) info.ArgumentList.Add("/reference:" + reference);
        foreach (var source in sources) info.ArgumentList.Add(source);
        try
        {
            var result = Run(info);
            if (result.ExitCode != 0) throw new CompilerException("SCRIPT_PRECOMPILE_FAILED", id + " C# precompile failed: " + result.StandardOutput + result.StandardError);
            return result;
        }
        finally { if (File.Exists(output)) File.Delete(output); }
    }

    private object CompilePython(ScriptRequirement requirement, string source)
    {
        var python = Path.Combine(_vmRoot, "Applications", "ModuleProxy", "x64", "python.exe");
        RequireFile(python, "VM_PYTHON_MISSING");
        foreach (var dependency in requirement.Dependencies.Where(x => x.Kind == "python-package"))
        {
            var probe = new ProcessStartInfo(python) { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false, CreateNoWindow = true };
            probe.ArgumentList.Add("-c");
            probe.ArgumentList.Add("import importlib.util,sys; sys.exit(0 if importlib.util.find_spec(sys.argv[1]) is not None else 2)");
            probe.ArgumentList.Add(dependency.Name);
            var probeResult = Run(probe);
            if (probeResult.ExitCode != 0) throw new CompilerException("PYTHON_DEPENDENCY_MISSING", "VM Python cannot import declared package: " + dependency.Name);
        }
        var info = new ProcessStartInfo(python) { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false, CreateNoWindow = true };
        info.ArgumentList.Add("-c");
        info.ArgumentList.Add("import ast,pathlib,sys; ast.parse(pathlib.Path(sys.argv[1]).read_text(encoding='utf-8'))");
        info.ArgumentList.Add(source);
        var result = Run(info);
        if (result.ExitCode != 0) throw new CompilerException("SCRIPT_PRECOMPILE_FAILED", requirement.Id + " Python syntax check failed: " + result.StandardOutput + result.StandardError);
        return new { id = requirement.Id, carrier = "python-module", compiler = python, dependencies = requirement.Dependencies.Select(x => x.Name), result.ExitCode, result.StandardOutput, result.StandardError };
    }

    private string ResolveModuleReference(DependencyRequirement dependency)
    {
        if (!string.IsNullOrWhiteSpace(dependency.Path))
        {
            var explicitPath = Path.GetFullPath(dependency.Path);
            RequireFile(explicitPath, "DEPENDENCY_FILE_MISSING");
            return explicitPath;
        }
        if (dependency.Name.StartsWith("System.", StringComparison.OrdinalIgnoreCase)) return ResolveFrameworkReference(dependency.Name);
        var shellDll = Path.Combine(_vmRoot, "Applications", "Module(sp)", "x64", "Logic", "ShellModule", "DLL", dependency.Name);
        RequireFile(shellDll, "DEPENDENCY_FILE_MISSING");
        return shellDll;
    }

    private static string ResolveFrameworkReferenceIfNeeded(string reference)
    {
        if (Path.IsPathRooted(reference)) return reference;
        return Path.GetFileName(reference).StartsWith("System", StringComparison.OrdinalIgnoreCase)
            ? ResolveFrameworkReference(reference)
            : reference;
    }

    private static string ResolveFrameworkReference(string name)
    {
        var fileName = Path.GetFileName(name);
        var framework64 = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.Windows),
            "Microsoft.NET", "Framework64", "v4.0.30319", fileName);
        RequireFile(framework64, "CSHARP_FRAMEWORK_REFERENCE_MISSING");
        return framework64;
    }

    private static string PropertyCompanion(ScriptRequirement requirement)
    {
        var code = new StringBuilder("using Script.Methods;\npublic partial class UserScript\n{\n");
        foreach (var input in requirement.Inputs) code.Append("    public ").Append(CSharpType(input.Type)).Append(' ').Append(input.Name).Append(" { get { return default(").Append(CSharpType(input.Type)).Append("); } }\n");
        foreach (var output in requirement.Outputs) code.Append("    public ").Append(CSharpType(output.Type)).Append(' ').Append(output.Name).Append(" { set { } }\n");
        return code.Append("}\n").ToString();
    }

    private static string CSharpType(string type) => type switch
    {
        "bool" => "int", "int" => "int", "int[]" => "int[]", "float" => "float", "float[]" => "float[]",
        "string" => "string", "string[]" => "string[]", "byte" or "pointset" => "byte[]", "image" => "ImageData",
        "roibox" => "RoiboxData", "roibox[]" => "RoiboxData[]", "roiannulus" => "AnnulusData[]", "roipolygon" => "PolygonData[]",
        "point" => "PointData[]", "line" => "LineData[]", "fixture" => "FixtureData[]", "circle" => "CircleData[]",
        "rect" => "RectData[]", "ellipse" => "EllipseData[]",
        _ => throw new CompilerException("VM_TYPE_UNSUPPORTED", "No C# property type for: " + type)
    };

    private static ProcessResult Run(ProcessStartInfo info)
    {
        using var process = Process.Start(info) ?? throw new CompilerException("PRECOMPILER_START_FAILED", "Cannot start script precompiler.");
        var stdoutTask = process.StandardOutput.ReadToEndAsync();
        var stderrTask = process.StandardError.ReadToEndAsync();
        if (!process.WaitForExit(120000))
        {
            try { process.Kill(true); } catch { }
            throw new CompilerException("PRECOMPILER_TIMEOUT", "Script precompiler did not finish within two minutes.");
        }
        var stdout = stdoutTask.GetAwaiter().GetResult();
        var stderr = stderrTask.GetAwaiter().GetResult();
        return new(process.ExitCode, stdout, stderr);
    }

    private static void RequireFile(string file, string code) { if (!File.Exists(file)) throw new CompilerException(code, "Required precompile dependency is missing: " + file); }
}
