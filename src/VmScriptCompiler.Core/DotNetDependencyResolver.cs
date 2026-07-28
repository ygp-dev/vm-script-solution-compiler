using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Security.Cryptography;
using System.Text.Json;

namespace VmScriptCompiler.Core;

public sealed record AssemblyDependencyEvidence(
    string Name,
    string DeclaredName,
    int ReferenceType,
    string Role,
    string Source,
    string? SourcePath,
    string? PackagedPath,
    string? DeploymentTarget,
    bool RuntimeVisible,
    string? AssemblyVersion,
    string? ClrMetadataVersion,
    string? TargetFramework,
    string Architecture,
    string? Sha256,
    IReadOnlyList<string> AssemblyReferences);

public sealed record ScriptDependencyEvidence(string ScriptId, string ScriptName, IReadOnlyList<AssemblyDependencyEvidence> Assemblies);

public sealed class DotNetDependencyResolution(IReadOnlyList<ScriptDependencyEvidence> scripts)
{
    public IReadOnlyList<ScriptDependencyEvidence> Scripts { get; } = scripts;

    public string ResolveCompilerReference(string scriptId, DependencyRequirement dependency)
    {
        var item = Scripts.Single(x => x.ScriptId == scriptId).Assemblies.Single(x => x.Source != "transitive" && string.Equals(x.DeclaredName, dependency.Name, StringComparison.OrdinalIgnoreCase));
        return item.SourcePath ?? dependency.Name;
    }

    public static DotNetDependencyResolution Empty { get; } = new([]);
}

public sealed class DotNetDependencyResolver(string repositoryRoot, string vmRoot)
{
    private readonly string _repositoryRoot = Path.GetFullPath(repositoryRoot);
    private readonly string _vmRoot = Path.GetFullPath(vmRoot);

    public DotNetDependencyResolution Resolve(IReadOnlyList<ScriptRequirement> scripts, string specificationFile, string taskDirectory, string validationDirectory)
    {
        var catalog = LoadCatalog();
        var specificationDirectory = Path.GetDirectoryName(Path.GetFullPath(specificationFile))!;
        var results = new List<ScriptDependencyEvidence>();
        // All packaged files are ultimately deployed into one flat VM DLL directory. Track names
        // across every script so two different assemblies can never silently overwrite each other.
        var packaged = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var script in scripts.Where(x => x.Carrier == "csharp-module" && x.Dependencies.Any(d => d.Kind == "dotnet-assembly")))
        {
            var assemblies = new List<AssemblyDependencyEvidence>();
            foreach (var dependency in script.Dependencies.Where(x => x.Kind == "dotnet-assembly"))
            {
                var catalogEntry = ResolveCatalogEntry(dependency, catalog);
                var referenceType = catalogEntry.ReferenceType;
                var role = catalogEntry.Role;
                var source = ResolveDirectPath(dependency, referenceType, specificationDirectory);
                if (source is null)
                {
                    assemblies.Add(new(dependency.Name, dependency.Name, referenceType, role, "framework", null, null, null, true,
                        dependency.Version, "v4.0.30319", ".NET Framework 4.6.1 runtime reference", "anycpu", null, []));
                    continue;
                }

                var direct = Inspect(source);
                ValidateDirect(dependency, direct);
                var deploymentTarget = Path.Combine(ShellDllDirectory, Path.GetFileName(source));
                var runtimeVisible = IsRuntimeVisible(source);
                if (role == "vm-sdk" && !runtimeVisible)
                    throw new CompilerException("DEPENDENCY_VM_SDK_NOT_RUNTIME_VISIBLE", $"VM SDK assembly must come from the installed VM runtime and is never packaged as a user DLL: {dependency.Name}.");
                var packagedPath = runtimeVisible ? null : Package(script.Id, source, taskDirectory, packaged);
                var transitiveNames = new List<string>();
                var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { direct.Name };
                // VM-installed assemblies are vendor-managed and may resolve private dependencies through
                // VM application probing that is not represented by the ShellModule DLL directory. Recursively
                // package only external files; runtime assemblies are still identity-inspected and precompiled.
                if (!runtimeVisible)
                    ResolveTransitives(script.Id, source, direct.References, taskDirectory, packaged, visited, transitiveNames, assemblies);
                assemblies.Insert(0, new(direct.Name, dependency.Name, referenceType, role, runtimeVisible ? "vm-runtime" : "explicit-path",
                    source, packagedPath, runtimeVisible ? null : deploymentTarget, runtimeVisible, direct.Version, direct.ClrMetadataVersion, direct.TargetFramework,
                    direct.Architecture, direct.Sha256, direct.References.Select(x => x.Name).ToArray()));
            }
            results.Add(new(script.Id, script.Name, assemblies));
        }

        var resolution = new DotNetDependencyResolution(results);
        Directory.CreateDirectory(validationDirectory);
        var manifest = new
        {
            ok = true,
            scriptTargetFramework = ".NET Framework 4.6.1",
            acceptedDependencyRuntime = "CLR 4 .NET Framework; final loadability is verified by Framework64 csc and VM runtime visibility",
            targetArchitecture = "x64",
            runtimeDllDirectory = ShellDllDirectory,
            scripts = resolution.Scripts
        };
        var manifestJson = JsonSerializer.Serialize(manifest, JsonDefaults.Options);
        File.WriteAllText(Path.Combine(validationDirectory, "dependency-manifest.json"), manifestJson);
        if (resolution.Scripts.SelectMany(x => x.Assemblies).Any(x => x.PackagedPath is not null))
        {
            var dependencyDirectory = Path.Combine(taskDirectory, "dependencies");
            File.WriteAllText(Path.Combine(dependencyDirectory, "manifest.json"), manifestJson);
            WriteDeploymentScript(dependencyDirectory, resolution);
        }
        return resolution;
    }

    private void WriteDeploymentScript(string dependencyDirectory, DotNetDependencyResolution resolution)
    {
        static string Quote(string value) => "'" + value.Replace("'", "''", StringComparison.Ordinal) + "'";
        var files = resolution.Scripts.SelectMany(x => x.Assemblies).Where(x => x.PackagedPath is not null)
            .Select(x => x.PackagedPath!["dependencies/".Length..])
            .GroupBy(Path.GetFileName, StringComparer.OrdinalIgnoreCase).Select(x => x.First()).ToArray();
        var builder = new System.Text.StringBuilder()
            .AppendLine("param([string]$VmRoot = " + Quote(_vmRoot) + ")")
            .AppendLine("$ErrorActionPreference = 'Stop'")
            .AppendLine("$target = Join-Path $VmRoot 'Applications\\Module(sp)\\x64\\Logic\\ShellModule\\DLL'")
            .AppendLine("if (-not (Test-Path -LiteralPath $target -PathType Container)) { throw \"VM ShellModule DLL directory not found: $target\" }")
            .AppendLine("$root = Split-Path -Parent $MyInvocation.MyCommand.Path")
            .AppendLine("$files = @(");
        foreach (var file in files) builder.AppendLine("    " + Quote(file.Replace('/', '\\')));
        builder.AppendLine(")")
            .AppendLine("foreach ($file in $files) {")
            .AppendLine("    $source = Join-Path $root $file")
            .AppendLine("    Copy-Item -LiteralPath $source -Destination (Join-Path $target (Split-Path -Leaf $file)) -Force")
            .AppendLine("}")
            .AppendLine("Write-Host \"Dependencies deployed to $target\"");
        File.WriteAllText(Path.Combine(dependencyDirectory, "deploy-to-vm.ps1"), builder.ToString(), new System.Text.UTF8Encoding(false));
    }

    private void ResolveTransitives(string scriptId, string parentPath, IReadOnlyList<AssemblyReferenceInfo> references, string taskDirectory,
        Dictionary<string, string> packaged, HashSet<string> visited, List<string> transitiveNames, List<AssemblyDependencyEvidence> results)
    {
        foreach (var reference in references.Where(x => !IsFrameworkAssembly(x.Name)))
        {
            if (!visited.Add(reference.Name)) continue;
            var path = ResolveTransitivePath(parentPath, reference.Name + ".dll");
            if (path is null) throw new CompilerException("DEPENDENCY_TRANSITIVE_MISSING", $"Cannot resolve transitive assembly {reference.Name} required by {Path.GetFileName(parentPath)}.");
            var inspected = Inspect(path);
            if (inspected.Version is not null && reference.Version is not null && !VersionsEquivalent(inspected.Version, reference.Version))
                throw new CompilerException("DEPENDENCY_TRANSITIVE_VERSION_MISMATCH", $"{reference.Name} requires version {reference.Version}, but {path} is {inspected.Version}.");
            if (inspected.Architecture == "x86") throw new CompilerException("DEPENDENCY_ARCHITECTURE_MISMATCH", "x86 transitive DLL cannot run in VM x64: " + path);
            ValidateClr4(inspected, "Transitive DLL " + path);
            var runtimeVisible = IsRuntimeVisible(path);
            var packagedPath = runtimeVisible ? null : Package(scriptId, path, taskDirectory, packaged);
            results.Add(new(inspected.Name, inspected.Name + ".dll", 4, "transitive", "transitive", path, packagedPath,
                runtimeVisible ? null : Path.Combine(ShellDllDirectory, Path.GetFileName(path)), runtimeVisible, inspected.Version, inspected.ClrMetadataVersion,
                inspected.TargetFramework, inspected.Architecture, inspected.Sha256, inspected.References.Select(x => x.Name).ToArray()));
            transitiveNames.Add(inspected.Name);
            ResolveTransitives(scriptId, path, inspected.References, taskDirectory, packaged, visited, transitiveNames, results);
        }
    }

    private string? ResolveDirectPath(DependencyRequirement dependency, int referenceType, string specificationDirectory)
    {
        if (!string.IsNullOrWhiteSpace(dependency.Path))
        {
            var path = Path.IsPathRooted(dependency.Path) ? dependency.Path : Path.Combine(specificationDirectory, dependency.Path);
            path = Path.GetFullPath(path);
            RequireFile(path, "DEPENDENCY_FILE_MISSING");
            if (!string.Equals(Path.GetFileName(path), dependency.Name, StringComparison.OrdinalIgnoreCase))
                throw new CompilerException("DEPENDENCY_NAME_MISMATCH", $"Dependency name {dependency.Name} does not match file {Path.GetFileName(path)}.");
            return path;
        }
        if (referenceType == 0) return null;
        var pathFromRuntime = RuntimeSearchDirectories.Select(x => Path.Combine(x, dependency.Name)).FirstOrDefault(File.Exists);
        if (pathFromRuntime is null) throw new CompilerException("DEPENDENCY_FILE_MISSING", "Verified VM dependency is not installed: " + dependency.Name);
        return pathFromRuntime;
    }

    private string? ResolveTransitivePath(string parentPath, string fileName)
    {
        var sibling = Path.Combine(Path.GetDirectoryName(parentPath)!, fileName);
        if (File.Exists(sibling)) return sibling;
        return RuntimeSearchDirectories.Select(x => Path.Combine(x, fileName)).FirstOrDefault(File.Exists);
    }

    private void ValidateDirect(DependencyRequirement dependency, InspectedAssembly assembly)
    {
        if (!string.Equals(assembly.Name + ".dll", dependency.Name, StringComparison.OrdinalIgnoreCase))
            throw new CompilerException("DEPENDENCY_ASSEMBLY_NAME_MISMATCH", $"Declared {dependency.Name}, but assembly identity is {assembly.Name}.dll.");
        if (!string.IsNullOrWhiteSpace(dependency.Version) && !VersionsEquivalent(dependency.Version, assembly.Version))
            throw new CompilerException("DEPENDENCY_VERSION_MISMATCH", $"Declared {dependency.Name} version {dependency.Version}, actual version is {assembly.Version}.");
        if (assembly.Architecture == "x86") throw new CompilerException("DEPENDENCY_ARCHITECTURE_MISMATCH", "x86 DLL cannot run in VM x64: " + dependency.Name);
        if (dependency.Architecture == "anycpu" && assembly.Architecture != "anycpu")
            throw new CompilerException("DEPENDENCY_ARCHITECTURE_MISMATCH", $"{dependency.Name} was declared anycpu, actual architecture is {assembly.Architecture}.");
        if (dependency.Architecture == "x64" && assembly.Architecture is not ("x64" or "anycpu"))
            throw new CompilerException("DEPENDENCY_ARCHITECTURE_MISMATCH", $"{dependency.Name} is not compatible with x64 VM.");
        if (!string.IsNullOrWhiteSpace(dependency.Path)) ValidateClr4(assembly, "External DLL " + dependency.Name);
    }

    private static void ValidateClr4(InspectedAssembly assembly, string label)
    {
        if (!assembly.ClrMetadataVersion.StartsWith("v4.", StringComparison.OrdinalIgnoreCase))
            throw new CompilerException("DEPENDENCY_TARGET_FRAMEWORK_INCOMPATIBLE", $"{label} uses CLR metadata {assembly.ClrMetadataVersion}; VM 4.4 requires CLR 4 .NET Framework assemblies.");
        if (assembly.TargetFramework is { } framework && !IsClr4FrameworkCompatible(framework))
            throw new CompilerException("DEPENDENCY_TARGET_FRAMEWORK_INCOMPATIBLE", $"{label} targets {framework}; VM 4.4 requires CLR 4 .NET Framework assemblies.");
    }

    private static bool IsClr4FrameworkCompatible(string framework)
    {
        const string prefix = ".NETFramework,Version=v";
        if (!framework.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) return false;
        return Version.TryParse(framework[prefix.Length..], out var version) && version.Major == 4;
    }

    private static bool VersionsEquivalent(string? left, string? right)
    {
        if (string.IsNullOrWhiteSpace(left) || string.IsNullOrWhiteSpace(right))
            return string.Equals(left, right, StringComparison.OrdinalIgnoreCase);
        if (!Version.TryParse(left, out var leftVersion) || !Version.TryParse(right, out var rightVersion))
            return string.Equals(left, right, StringComparison.OrdinalIgnoreCase);
        static int Part(int value) => value < 0 ? 0 : value;
        return leftVersion.Major == rightVersion.Major
            && leftVersion.Minor == rightVersion.Minor
            && Part(leftVersion.Build) == Part(rightVersion.Build)
            && Part(leftVersion.Revision) == Part(rightVersion.Revision);
    }

    private static InspectedAssembly Inspect(string path)
    {
        try
        {
            using var stream = File.OpenRead(path);
            using var pe = new PEReader(stream);
            if (!pe.HasMetadata || pe.PEHeaders.CorHeader is null) throw new CompilerException("DEPENDENCY_NOT_MANAGED", "C# script reference is not a managed assembly: " + path);
            var metadata = pe.GetMetadataReader();
            if (!metadata.IsAssembly) throw new CompilerException("DEPENDENCY_NOT_MANAGED", "Managed file is not an assembly: " + path);
            var definition = metadata.GetAssemblyDefinition();
            var name = metadata.GetString(definition.Name);
            var targetFramework = ReadTargetFramework(metadata, definition);
            var references = metadata.AssemblyReferences.Select(handle =>
            {
                var item = metadata.GetAssemblyReference(handle);
                return new AssemblyReferenceInfo(metadata.GetString(item.Name), item.Version?.ToString());
            }).ToArray();
            var architecture = Architecture(pe.PEHeaders.CoffHeader.Machine, pe.PEHeaders.CorHeader.Flags);
            return new(name, definition.Version?.ToString(), metadata.MetadataVersion, targetFramework, architecture,
                Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path))), references);
        }
        catch (CompilerException) { throw; }
        catch (Exception error) { throw new CompilerException("DEPENDENCY_INSPECTION_FAILED", "Cannot inspect assembly " + path + ": " + error.Message); }
    }

    private static string Architecture(Machine machine, CorFlags flags)
    {
        if (machine is Machine.Amd64 or Machine.IA64) return "x64";
        if (machine == Machine.I386) return flags.HasFlag(CorFlags.Requires32Bit) ? "x86" : "anycpu";
        return machine.ToString().ToLowerInvariant();
    }

    private static string? ReadTargetFramework(MetadataReader metadata, AssemblyDefinition definition)
    {
        foreach (var handle in definition.GetCustomAttributes())
        {
            var attribute = metadata.GetCustomAttribute(handle);
            if (attribute.Constructor.Kind != HandleKind.MemberReference) continue;
            var member = metadata.GetMemberReference((MemberReferenceHandle)attribute.Constructor);
            if (member.Parent.Kind != HandleKind.TypeReference) continue;
            var type = metadata.GetTypeReference((TypeReferenceHandle)member.Parent);
            if (metadata.GetString(type.Name) != "TargetFrameworkAttribute" || metadata.GetString(type.Namespace) != "System.Runtime.Versioning") continue;
            var reader = metadata.GetBlobReader(attribute.Value);
            if (reader.ReadUInt16() != 1) return null;
            return reader.ReadSerializedString();
        }
        return null;
    }

    private string Package(string scriptId, string source, string taskDirectory, Dictionary<string, string> packaged)
    {
        var name = Path.GetFileName(source);
        var destinationDirectory = Path.Combine(taskDirectory, "dependencies", scriptId);
        Directory.CreateDirectory(destinationDirectory);
        var destination = Path.Combine(destinationDirectory, name);
        var hash = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(source)));
        if (packaged.TryGetValue(name, out var existingHash) && !string.Equals(existingHash, hash, StringComparison.Ordinal))
            throw new CompilerException("DEPENDENCY_FILE_CONFLICT", "Different dependency files use the same name: " + name);
        File.Copy(source, destination, true);
        packaged[name] = hash;
        return Path.GetRelativePath(taskDirectory, destination).Replace('\\', '/');
    }

    private CatalogEntry ResolveCatalogEntry(DependencyRequirement dependency, IReadOnlyDictionary<string, CatalogEntry> catalog)
    {
        if (catalog.TryGetValue(dependency.Name, out var verified))
        {
            if (dependency.ReferenceType is not null && dependency.ReferenceType != verified.ReferenceType)
                throw new CompilerException("REFERENCE_TYPE_MISMATCH", $"Declared referenceType {dependency.ReferenceType} does not match verified type {verified.ReferenceType} for {dependency.Name}.");
            if (!string.IsNullOrWhiteSpace(dependency.Role) && !string.Equals(dependency.Role, verified.Role, StringComparison.OrdinalIgnoreCase))
                throw new CompilerException("DEPENDENCY_ROLE_MISMATCH", $"Declared role {dependency.Role} does not match verified role {verified.Role} for {dependency.Name}.");
            return verified;
        }
        if (dependency.ReferenceType == 4 && !string.IsNullOrWhiteSpace(dependency.Path))
        {
            var role = dependency.Role ?? "third-party";
            if (role is not ("third-party" or "operator-sdk"))
                throw new CompilerException("DEPENDENCY_ROLE_INVALID", $"An unverified external DLL can only use role third-party or operator-sdk: {dependency.Name}.");
            return new(4, role);
        }
        throw new CompilerException("REFERENCE_TYPE_UNCONFIRMED", "External DLL requires an explicit path and referenceType 4: " + dependency.Name);
    }

    private IReadOnlyDictionary<string, CatalogEntry> LoadCatalog()
    {
        var file = Path.Combine(_repositoryRoot, "resources", "vm", "4.4.0", "shell-reference-catalog.json");
        using var document = JsonDocument.Parse(File.ReadAllText(file));
        var result = document.RootElement.GetProperty("defaultReferences").EnumerateArray()
            .ToDictionary(x => x.GetProperty("name").GetString()!, x => new CatalogEntry(x.GetProperty("referenceType").GetInt32(), x.GetProperty("role").GetString()!), StringComparer.OrdinalIgnoreCase);
        foreach (var item in document.RootElement.GetProperty("verifiedReferences").EnumerateObject())
            result[item.Name] = new(item.Value.GetProperty("referenceType").GetInt32(), item.Value.GetProperty("role").GetString()!);
        return result;
    }

    private bool IsRuntimeVisible(string path)
    {
        var full = Path.GetFullPath(path);
        return RuntimeSearchDirectories.Any(directory => IsWithin(full, directory));
    }

    private static bool IsWithin(string path, string directory)
    {
        var relative = Path.GetRelativePath(Path.GetFullPath(directory), path);
        return !Path.IsPathRooted(relative) && relative != ".." && !relative.StartsWith(".." + Path.DirectorySeparatorChar, StringComparison.Ordinal);
    }

    private string ShellDllDirectory => Path.Combine(_vmRoot, "Applications", "Module(sp)", "x64", "Logic", "ShellModule", "DLL");
    private IReadOnlyList<string> RuntimeSearchDirectories =>
    [
        ShellDllDirectory,
        Path.Combine(_vmRoot, "Applications", "Module(sp)", "x64", "Logic", "ShellModule"),
        Path.Combine(_vmRoot, "Applications", "myLibs"),
        Path.Combine(_vmRoot, "Applications", "PublicFile", "x64"),
        Path.Combine(_vmRoot, "Applications", "GlobalScript")
    ];

    private static bool IsFrameworkAssembly(string name) => name.Equals("mscorlib", StringComparison.OrdinalIgnoreCase)
        || name.Equals("netstandard", StringComparison.OrdinalIgnoreCase) || name.Equals("Microsoft.CSharp", StringComparison.OrdinalIgnoreCase)
        || name.Equals("System", StringComparison.OrdinalIgnoreCase) || name.StartsWith("System.", StringComparison.OrdinalIgnoreCase) || name.StartsWith("Presentation", StringComparison.OrdinalIgnoreCase)
        || name.Equals("WindowsBase", StringComparison.OrdinalIgnoreCase);
    private static void RequireFile(string file, string code) { if (!File.Exists(file)) throw new CompilerException(code, "Required dependency is missing: " + file); }

    private sealed record InspectedAssembly(string Name, string? Version, string ClrMetadataVersion, string? TargetFramework, string Architecture, string Sha256, IReadOnlyList<AssemblyReferenceInfo> References);
    private sealed record AssemblyReferenceInfo(string Name, string? Version);
    private sealed record CatalogEntry(int ReferenceType, string Role);
}
