using System.Text.Json;
using System.Text.Json.Serialization;

namespace VmScriptCompiler.Core;

public sealed class CompileRequirement
{
    public required string SchemaVersion { get; init; }
    public required TaskRequirement Task { get; init; }
    public required List<ScriptRequirement> Scripts { get; init; }
    public List<ConnectionRequirement> Connections { get; init; } = [];
}

public sealed class TaskRequirement
{
    public required string Name { get; init; }
    public required string Mode { get; init; }
    public required string VmVersion { get; init; }
    public string? BaseSolution { get; init; }
}

public sealed class ScriptRequirement
{
    public required string Id { get; init; }
    public required string Carrier { get; init; }
    public required string Name { get; init; }
    public string? Source { get; init; }
    public string? Procedure { get; init; }
    public required ExecutionRequirement Execution { get; init; }
    public required List<IoRequirement> Inputs { get; init; }
    public required List<IoRequirement> Outputs { get; init; }
    public List<OperationRequirement> Operations { get; init; } = [];
    public List<DependencyRequirement> Dependencies { get; init; } = [];
}

public sealed class ExecutionRequirement { public required string Mode { get; init; } public int? Order { get; init; } }
public sealed class ConnectionRequirement
{
    public required string From { get; init; }
    public required string To { get; init; }
}
public sealed class IoRequirement
{
    public required string Name { get; init; }
    public required string Type { get; init; }
    [JsonPropertyName("default"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] public JsonElement DefaultValue { get; init; }
    public bool Required { get; init; }
    public string? Description { get; init; }
}
public sealed class OperationRequirement
{
    public required string Kind { get; init; }
    public string? Procedure { get; init; }
    public string? Module { get; init; }
    public string? Parameter { get; init; }
    public string? Result { get; init; }
    public string? ValueType { get; init; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] public JsonElement Value { get; init; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] public JsonElement Condition { get; init; }
    public int? DeviceId { get; init; }
    public int? AddressId { get; init; }
    public int? Milliseconds { get; init; }
    public string? DataType { get; init; }
    public string? When { get; init; }
    public string? OnError { get; init; }
}
public sealed class DependencyRequirement
{
    public required string Kind { get; init; }
    public required string Name { get; init; }
    public string? Role { get; init; }
    public string? Version { get; init; }
    public string? Path { get; init; }
    public string? Architecture { get; init; }
    public int? ReferenceType { get; init; }
}

public static class RequirementLoader
{
    public static (CompileRequirement Requirement, RequirementValidationResult Validation) Load(string path)
    {
        path = Path.GetFullPath(path);
        if (!File.Exists(path)) throw new CompilerException("SPECIFICATION_FILE_NOT_FOUND", "Requirement file does not exist: " + path);
        try
        {
            using var document = JsonDocument.Parse(File.ReadAllText(path));
            var validation = new RequirementValidator().Validate(document);
            if (!validation.IsValid)
            {
                var code = validation.Issues.FirstOrDefault(x => x.Code != "REQUIREMENT_SCHEMA_INVALID")?.Code ?? "REQUIREMENT_SCHEMA_INVALID";
                throw new CompilerException(code, string.Join("; ", validation.Issues.Select(x => $"{x.Path}: {x.Message}")));
            }
            var requirement = JsonSerializer.Deserialize<CompileRequirement>(document.RootElement.GetRawText(), JsonDefaults.Options)
                ?? throw new CompilerException("REQUIREMENT_SCHEMA_INVALID", "Requirement cannot be deserialized.");
            return (requirement, validation);
        }
        catch (JsonException error) { throw new CompilerException("REQUIREMENT_SCHEMA_INVALID", "Requirement is not valid JSON: " + error.Message); }
    }
}
