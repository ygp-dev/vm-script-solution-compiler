using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Nodes;
using VmScriptCompiler.Core;

Console.InputEncoding = new System.Text.UTF8Encoding(false);
Console.OutputEncoding = new System.Text.UTF8Encoding(false);
var explicitRoot = args.SkipWhile(x => x != "--repository-root").Skip(1).FirstOrDefault();
var server = new McpServer(new CompilerFacade(RepositoryLocator.Find(explicitRoot)));
await server.RunAsync(Console.In, Console.Out);

internal sealed class McpServer(CompilerFacade compiler)
{
    private static readonly JsonSerializerOptions ProtocolJson = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
    public async Task RunAsync(TextReader input, TextWriter output)
    {
        while (await input.ReadLineAsync() is { } line)
        {
            line = line.TrimStart('\uFEFF');
            var jsonStart = line.IndexOf('{');
            if (jsonStart is > 0 and <= 3) line = line[jsonStart..];
            if (string.IsNullOrWhiteSpace(line)) continue;
            JsonObject? response;
            try
            {
                using var request = JsonDocument.Parse(line);
                response = Handle(request.RootElement);
            }
            catch (JsonException ex) { response = Error(null, -32700, "Parse error", ex.Message); }
            catch (Exception ex) { response = Error(null, -32603, "Internal error", ex.Message); }
            if (response is null) continue;
            await output.WriteLineAsync(response.ToJsonString(ProtocolJson));
            await output.FlushAsync();
        }
    }

    private JsonObject? Handle(JsonElement request)
    {
        if (request.ValueKind != JsonValueKind.Object || !request.TryGetProperty("jsonrpc", out var jsonRpc) || jsonRpc.GetString() != "2.0")
            return Error(null, -32600, "Invalid Request", "Expected a JSON-RPC 2.0 request object.");
        var hasId = request.TryGetProperty("id", out var idElement);
        JsonNode? id = hasId ? JsonNode.Parse(idElement.GetRawText()) : null;
        var method = request.TryGetProperty("method", out var methodElement) ? methodElement.GetString() : null;
        if (!hasId && method?.StartsWith("notifications/", StringComparison.Ordinal) == true) return null;
        return method switch
        {
            "initialize" => Result(id, Initialize(request)),
            "ping" => Result(id, new JsonObject()),
            "tools/list" => Result(id, new JsonObject { ["tools"] = ToolDefinitions() }),
            "tools/call" => Result(id, CallTool(request)),
            _ => Error(id, -32601, "Method not found", method ?? "(missing)")
        };
    }

    private static JsonObject Initialize(JsonElement request)
    {
        var requested = request.TryGetProperty("params", out var parameters) && parameters.TryGetProperty("protocolVersion", out var version) ? version.GetString() : null;
        return new JsonObject {
            ["protocolVersion"] = requested ?? "2024-11-05",
            ["capabilities"] = new JsonObject { ["tools"] = new JsonObject { ["listChanged"] = false } },
            ["serverInfo"] = new JsonObject { ["name"] = "vm-script-solution-compiler", ["version"] = ProductInfo.Version }
        };
    }

    private JsonObject CallTool(JsonElement request)
    {
        if (!request.TryGetProperty("params", out var parameters)) return ToolError("INVALID_ARGUMENT", "Missing params.");
        var name = parameters.TryGetProperty("name", out var nameElement) ? nameElement.GetString() : null;
        var arguments = parameters.TryGetProperty("arguments", out var argumentsElement) ? argumentsElement : default;
        try
        {
            object result = name switch
            {
                "detect_environment" => compiler.DetectEnvironment(),
                "inspect_solution" => InspectSolution(Required(arguments, "file")),
                "query_capability" => QueryCapability(Optional(arguments, "query"), Optional(arguments, "vmVersion") ?? "4.4.0"),
                "validate_requirement" => compiler.Plan(Required(arguments, "spec")),
                "plan_solution" => compiler.Plan(Required(arguments, "spec")),
                "build_solution" => Build(Required(arguments, "spec"), Required(arguments, "output")),
                "patch_solution" => Patch(Required(arguments, "baseSolution"), Required(arguments, "spec"), Required(arguments, "output")),
                "validate_solution" => compiler.ValidateSolution(Required(arguments, "file")),
                "read_build_report" => ReadBuildReport(Required(arguments, "file")),
                _ => throw new CompilerException("TOOL_NOT_FOUND", "Unknown tool: " + name)
            };
            return ToolResult(result);
        }
        catch (CompilerException ex) { return ToolError(ex.Code, ex.Message, ex.Details); }
        catch (Exception ex) { return ToolError("UNEXPECTED_ERROR", ex.Message); }
    }

    private object InspectSolution(string file)
    {
        file = ExistingFile(file, "SOLUTION_FILE_NOT_FOUND");
        SolArchiveValidator.ValidateVm44EntryNames(file);
        var parser = new ParserClient(Path.Combine(compiler.RepositoryRoot, "tools", "vm-solution-parser", "VMSolutionParser.Cli.exe"));
        var inspect = parser.Inspect(file);
        if (inspect.ExitCode != 0)
            throw new CompilerException("SOL_PARSE_FAILED", string.IsNullOrWhiteSpace(inspect.StandardError) ? inspect.StandardOutput : inspect.StandardError);
        return new { ok = true, file, sha256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(file))), inspect = inspect.StandardOutput };
    }

    private object QueryCapability(string? query, string vmVersion)
    {
        var directory = Path.Combine(compiler.RepositoryRoot, "resources", "vm", vmVersion);
        if (!Directory.Exists(directory))
            throw new CompilerException("VM_RESOURCE_VERSION_UNSUPPORTED", "VM resource version is not available: " + vmVersion);
        using var types = JsonDocument.Parse(File.ReadAllText(Path.Combine(directory, "type-system.json")));
        using var api = JsonDocument.Parse(File.ReadAllText(Path.Combine(directory, "api-catalog.json")));
        if (string.IsNullOrWhiteSpace(query))
            return new { vmVersion, typeSystem = types.RootElement.Clone(), apiCatalog = api.RootElement.Clone() };
        var matches = new List<JsonElement>();
        CollectMatches(types.RootElement, query, matches);
        CollectMatches(api.RootElement, query, matches);
        return new { vmVersion, query, matches = matches.Take(100).ToArray(), truncated = matches.Count > 100 };
    }

    private static void CollectMatches(JsonElement value, string query, List<JsonElement> matches)
    {
        switch (value.ValueKind)
        {
            case JsonValueKind.Object:
                if (value.GetRawText().Contains(query, StringComparison.OrdinalIgnoreCase)) matches.Add(value.Clone());
                else foreach (var property in value.EnumerateObject()) CollectMatches(property.Value, query, matches);
                break;
            case JsonValueKind.Array:
                foreach (var item in value.EnumerateArray())
                    if (item.GetRawText().Contains(query, StringComparison.OrdinalIgnoreCase)) matches.Add(item.Clone());
                break;
        }
    }

    private object Build(string spec, string output)
    {
        var result = compiler.Build(spec, output);
        return new { ok = true, result.TaskDirectory, result.SolutionFile, result.ReportFile, parseExitCode = result.Parse.ExitCode, inspectExitCode = result.Inspect.ExitCode };
    }

    private object Patch(string baseSolution, string spec, string output)
    {
        baseSolution = ExistingFile(baseSolution, "SOLUTION_FILE_NOT_FOUND");
        var before = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(baseSolution)));
        var result = compiler.Patch(baseSolution, spec, output);
        var after = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(baseSolution)));
        if (!string.Equals(before, after, StringComparison.Ordinal))
            throw new CompilerException("BASE_SOLUTION_MODIFIED", "Patch modified the input SOL, which is forbidden.");
        return new { ok = true, result.TaskDirectory, result.SolutionFile, result.ReportFile, parseExitCode = result.Parse.ExitCode, inspectExitCode = result.Inspect.ExitCode, baseSolution, baseSolutionSha256 = before, inputPreserved = true };
    }

    private static object ReadBuildReport(string file)
    {
        file = ExistingFile(file, "BUILD_REPORT_NOT_FOUND");
        var taskDirectory = Path.GetDirectoryName(file)!;
        return new {
            ok = true,
            file,
            content = File.ReadAllText(file),
            artifacts = Directory.EnumerateFiles(taskDirectory, "*", SearchOption.AllDirectories)
                .Select(path => Path.GetRelativePath(taskDirectory, path).Replace('\\', '/'))
                .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                .ToArray()
        };
    }

    private static string Required(JsonElement arguments, string name)
    {
        if (arguments.ValueKind == JsonValueKind.Object && arguments.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(value.GetString())) return value.GetString()!;
        throw new CompilerException("INVALID_ARGUMENT", "Missing string argument: " + name);
    }

    private static string? Optional(JsonElement arguments, string name) =>
        arguments.ValueKind == JsonValueKind.Object &&
        arguments.TryGetProperty(name, out var value) &&
        value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    private static string ExistingFile(string path, string code)
    {
        path = Path.GetFullPath(path);
        if (!File.Exists(path)) throw new CompilerException(code, "File does not exist: " + path);
        return path;
    }

    private static JsonObject ToolResult(object value) => new() { ["content"] = new JsonArray(new JsonObject { ["type"] = "text", ["text"] = JsonSerializer.Serialize(value, JsonDefaults.Options) }), ["isError"] = false };
    private static JsonObject ToolError(string code, string message, object? details = null) => new() { ["content"] = new JsonArray(new JsonObject { ["type"] = "text", ["text"] = JsonSerializer.Serialize(new { ok = false, error = code, message, details }, JsonDefaults.Options) }), ["isError"] = true };
    private static JsonObject Result(JsonNode? id, JsonNode result) => new() { ["jsonrpc"] = "2.0", ["id"] = id?.DeepClone(), ["result"] = result };
    private static JsonObject Error(JsonNode? id, int code, string message, string data) => new() { ["jsonrpc"] = "2.0", ["id"] = id?.DeepClone(), ["error"] = new JsonObject { ["code"] = code, ["message"] = message, ["data"] = data } };

    private static JsonArray ToolDefinitions() => new(
        Tool("detect_environment", "Detect the local VisionMaster environment.", new JsonObject()),
        Tool("inspect_solution", "Inspect a SOL and return its structure and SHA-256 without modifying it.", Properties(("file", "string")), "file"),
        Tool("query_capability", "Query project-local VM type and API capability evidence.", Properties(("query", "string"), ("vmVersion", "string"))),
        Tool("validate_requirement", "Validate a Requirement IR file without building a SOL.", Properties(("spec", "string")), "spec"),
        Tool("plan_solution", "Validate a Requirement IR file and return deterministic build actions.", Properties(("spec", "string")), "spec"),
        Tool("build_solution", "Build a new script solution from Requirement IR.", Properties(("spec", "string"), ("output", "string")), "spec", "output"),
        Tool("patch_solution", "Patch a copied business solution without modifying the input SOL.", Properties(("baseSolution", "string"), ("spec", "string"), ("output", "string")), "baseSolution", "spec", "output"),
        Tool("validate_solution", "Parse and structurally validate a SOL file.", Properties(("file", "string")), "file"),
        Tool("read_build_report", "Read a deterministic build report and enumerate its artifacts.", Properties(("file", "string")), "file")
    );

    private static JsonObject Tool(string name, string description, JsonObject properties, params string[] required) => new() {
        ["name"] = name, ["description"] = description,
        ["inputSchema"] = new JsonObject { ["type"] = "object", ["properties"] = properties, ["required"] = new JsonArray(required.Select(x => (JsonNode?)JsonValue.Create(x)).ToArray()), ["additionalProperties"] = false }
    };
    private static JsonObject Properties(params (string Name, string Type)[] items) { var result = new JsonObject(); foreach (var item in items) result[item.Name] = new JsonObject { ["type"] = item.Type }; return result; }
}
