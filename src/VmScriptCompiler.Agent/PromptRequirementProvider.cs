using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using VmScriptCompiler.Core;

namespace VmScriptCompiler.Agent;

public interface IRequirementProvider
{
    CompileRequirement Create(string prompt, string mode, string? baseSolution);
}

public sealed record AiProviderOptions(string? Endpoint, string? Model, string? ApiKey);

public static class RequirementProviderFactory
{
    public static IRequirementProvider Create(string? name = null, AiProviderOptions? options = null)
    {
        name = string.IsNullOrWhiteSpace(name) ? Environment.GetEnvironmentVariable("VM_SCRIPT_AI_PROVIDER") ?? "local" : name;
        return name.ToLowerInvariant() switch
        {
            "local" => new LocalPromptRequirementProvider(),
            "openai-compatible" or "ai" => new OpenAiCompatibleRequirementProvider(options),
            "openai-responses" or "openai" or "codex" => new OpenAiResponsesRequirementProvider(options),
            _ => throw new CompilerException("AI_PROVIDER_UNSUPPORTED", "Unsupported Requirement provider: " + name)
        };
    }
}

public sealed class OpenAiCompatibleRequirementProvider(AiProviderOptions? options = null) : IRequirementProvider
{
    public CompileRequirement Create(string prompt, string mode, string? baseSolution)
    {
        if (string.IsNullOrWhiteSpace(prompt)) throw new CompilerException("PROMPT_REQUIRED", "Prompt cannot be empty.");
        var endpoint = NormalizeEndpoint(RequiredSetting(options?.Endpoint, "VM_SCRIPT_AI_ENDPOINT"), "chat/completions");
        var model = RequiredSetting(options?.Model, "VM_SCRIPT_AI_MODEL");
        var system = AiRequirementProtocol.BuildInstructions(mode);
        var request = new
        {
            model,
            temperature = 0,
            response_format = new { type = "json_object" },
            messages = new object[] { new { role = "system", content = system }, new { role = "user", content = prompt } }
        };
        using var client = CreateHttpClient();
        var key = string.IsNullOrWhiteSpace(options?.ApiKey) ? Environment.GetEnvironmentVariable("VM_SCRIPT_AI_API_KEY") : options.ApiKey;
        if (!string.IsNullOrWhiteSpace(key)) client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", key);
        using var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");
        using var response = client.PostAsync(endpoint, content).GetAwaiter().GetResult();
        var body = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
        if (!response.IsSuccessStatusCode) throw new CompilerException("AI_PROVIDER_FAILED", $"AI provider returned {(int)response.StatusCode}: {Limit(body)}");
        EnsureJsonResponse(body, endpoint);
        try
        {
            using var document = JsonDocument.Parse(body);
            var ir = document.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString()
                ?? throw new InvalidDataException("AI response content is empty.");
            return AiRequirementProtocol.Parse(ir, mode, baseSolution);
        }
        catch (CompilerException) { throw; }
        catch (Exception error) { throw new CompilerException("AI_RESPONSE_INVALID", "AI provider did not return valid Requirement IR: " + error.Message); }
    }

    private static string RequiredSetting(string? value, string environmentName) => !string.IsNullOrWhiteSpace(value) ? value
        : Environment.GetEnvironmentVariable(environmentName) is { Length: > 0 } environmentValue ? environmentValue
        : throw new CompilerException("AI_PROVIDER_CONFIG_MISSING", "Missing AI setting: " + environmentName);

    private static string NormalizeEndpoint(string endpoint, string operation) => AiEndpoint.Normalize(endpoint, operation);
    private static void EnsureJsonResponse(string body, string endpoint) => AiEndpoint.EnsureJson(body, endpoint);
    private static string Limit(string body) => AiEndpoint.Limit(body);

    private static HttpClient CreateHttpClient() => new(new HttpClientHandler
    {
        AutomaticDecompression = System.Net.DecompressionMethods.All,
        CheckCertificateRevocationList = false
    }) { Timeout = TimeSpan.FromSeconds(120) };
}

public sealed class OpenAiResponsesRequirementProvider(AiProviderOptions? options = null) : IRequirementProvider
{
    public const string DefaultEndpoint = "https://api.openai.com/v1/responses";

    public CompileRequirement Create(string prompt, string mode, string? baseSolution)
    {
        if (string.IsNullOrWhiteSpace(prompt)) throw new CompilerException("PROMPT_REQUIRED", "Prompt cannot be empty.");
        var endpoint = AiEndpoint.Normalize(OptionalSetting(options?.Endpoint, "VM_SCRIPT_AI_ENDPOINT") ?? DefaultEndpoint, "responses");
        var model = RequiredSetting(options?.Model, "VM_SCRIPT_AI_MODEL");
        var request = new
        {
            model,
            store = false,
            instructions = AiRequirementProtocol.BuildInstructions(mode),
            input = new object[]
            {
                new
                {
                    type = "message",
                    role = "user",
                    content = new object[] { new { type = "input_text", text = "Return json only.\n\n" + prompt } }
                }
            },
            text = new { format = new { type = "json_object" } }
        };

        using var client = CreateHttpClient();
        var key = OptionalSetting(options?.ApiKey, "VM_SCRIPT_AI_API_KEY");
        if (!string.IsNullOrWhiteSpace(key))
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", key);
        using var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");
        using var response = client.PostAsync(endpoint, content).GetAwaiter().GetResult();
        var body = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
        if (!response.IsSuccessStatusCode)
            throw new CompilerException("AI_PROVIDER_FAILED", $"OpenAI Responses provider returned {(int)response.StatusCode}: {AiEndpoint.Limit(body)}");
        AiEndpoint.EnsureJson(body, endpoint);

        try
        {
            using var document = JsonDocument.Parse(body);
            var ir = ExtractOutputText(document.RootElement);
            return AiRequirementProtocol.Parse(ir, mode, baseSolution);
        }
        catch (CompilerException) { throw; }
        catch (Exception error)
        {
            throw new CompilerException("AI_RESPONSE_INVALID", "OpenAI Responses provider did not return valid Requirement IR: " + error.Message);
        }
    }

    private static string ExtractOutputText(JsonElement root)
    {
        if (root.TryGetProperty("output_text", out var direct) && direct.ValueKind == JsonValueKind.String)
            return direct.GetString()!;

        if (!root.TryGetProperty("output", out var output) || output.ValueKind != JsonValueKind.Array)
            throw new InvalidDataException("Response does not contain an output array.");

        foreach (var item in output.EnumerateArray())
        {
            if (!item.TryGetProperty("content", out var content) || content.ValueKind != JsonValueKind.Array) continue;
            foreach (var part in content.EnumerateArray())
            {
                var type = part.TryGetProperty("type", out var typeValue) ? typeValue.GetString() : null;
                if (type == "refusal")
                {
                    var refusal = part.TryGetProperty("refusal", out var refusalValue) ? refusalValue.GetString() : "The model refused the request.";
                    throw new CompilerException("AI_PROVIDER_REFUSED", refusal ?? "The model refused the request.");
                }
                if (type == "output_text" && part.TryGetProperty("text", out var text) && text.ValueKind == JsonValueKind.String)
                    return text.GetString()!;
            }
        }
        throw new InvalidDataException("Response contains no output_text content.");
    }

    private static string RequiredSetting(string? value, string environmentName) => OptionalSetting(value, environmentName)
        ?? throw new CompilerException("AI_PROVIDER_CONFIG_MISSING", "Missing AI setting: " + environmentName);

    private static string? OptionalSetting(string? value, string environmentName) => !string.IsNullOrWhiteSpace(value)
        ? value.Trim()
        : Environment.GetEnvironmentVariable(environmentName) is { Length: > 0 } environmentValue ? environmentValue.Trim() : null;

    private static HttpClient CreateHttpClient() => new(new HttpClientHandler
    {
        AutomaticDecompression = System.Net.DecompressionMethods.All,
        CheckCertificateRevocationList = false
    }) { Timeout = TimeSpan.FromSeconds(120) };
}

internal static class AiEndpoint
{
    public static string Normalize(string endpoint, string operation)
    {
        endpoint = endpoint.Trim().TrimEnd('/');
        if (!Uri.TryCreate(endpoint, UriKind.Absolute, out var uri) || uri.Scheme is not ("http" or "https"))
            throw new CompilerException("AI_PROVIDER_CONFIG_INVALID", "AI endpoint must be an absolute HTTP(S) URL.");
        return uri.AbsolutePath.TrimEnd('/').EndsWith("/v1", StringComparison.OrdinalIgnoreCase)
            ? endpoint + "/" + operation
            : endpoint;
    }

    public static void EnsureJson(string body, string endpoint)
    {
        var trimmed = body.TrimStart();
        if (trimmed.StartsWith('<'))
            throw new CompilerException("AI_RESPONSE_INVALID", "AI endpoint returned HTML instead of JSON. Enter the API base ending in /v1 or the complete endpoint: " + endpoint);
        if (trimmed.Length > 0 && trimmed[0] == '\u001f')
            throw new CompilerException("AI_RESPONSE_INVALID", "AI endpoint returned an undecoded compressed payload. Check the provider gateway response headers: " + endpoint);
    }

    public static string Limit(string body) => body.Length <= 4096 ? body : body[..4096] + "…";
}

internal static class AiRequirementProtocol
{
    public static string BuildInstructions(string mode)
    {
        var root = RepositoryLocator.Find();
        var schema = File.ReadAllText(Path.Combine(root, "schemas", "requirement.schema.json"));
        var typeSystem = File.ReadAllText(Path.Combine(root, "resources", "vm", "4.4.0", "type-system.json"));
        var apiCatalog = File.ReadAllText(Path.Combine(root, "resources", "vm", "4.4.0", "api-catalog.json"));
        var moduleCatalog = File.ReadAllText(Path.Combine(root, "resources", "vm", "4.4.0", "module-parameter-catalog.json"));
        var referenceCatalog = File.ReadAllText(Path.Combine(root, "resources", "vm", "4.4.0", "shell-reference-catalog.json"));
        var secondaryDevelopmentKnowledge = File.ReadAllText(Path.Combine(root, "resources", "vm", "4.4.0", "secondary-development-knowledge.json"));
        var communityArticlesKnowledge = File.ReadAllText(Path.Combine(root, "resources", "vm", "4.4.0", "community-articles-knowledge.json"));
        var scriptTutorKnowledge = File.ReadAllText(Path.Combine(root, "resources", "vm", "4.4.0", "script-tutor-knowledge.json"));
        return "Convert the user request to Requirement IR JSON only. Never edit or describe SOL binary data. " +
            "Use task.mode=" + mode + ", task.vmVersion=4.4.0. Modules are independent unless connections are explicit. " +
            "Patch operations that address an existing module must preserve its exact procedure/module/parameter names. " +
            "Use operations and expression AST when supported; use source only when deterministic operations cannot express the request. Never emit source and non-empty operations in the same script. " +
            "Every explicit csharp-module source must import Script.Methods and declare exactly public partial class UserScript : ScriptMethods, IProcessMethods with public void Init() and public bool Process(). " +
            "For csharp-module or python-module with explicit source, never declare a bool input or output. VM 4.4 exposes script bool-compatible ports as int: declare type=int with default 0 or 1, read truth as PortName != 0, and write truth as condition ? 1 : 0 in C# or 1 if condition else 0 in Python. The bool IR alias is allowed only for scripts generated from deterministic operations. " +
            "Prefer only operations listed in the API catalog. Do not invent module parameter names. " +
            "Do not emit circle as a ShellModule input or output: VM 4.4 has no selectable Circle script port. CircleData[] is variable-API-only through GetVarCircle/SetVarCircle. " +
            "Classify C# assemblies with role=system, vm-sdk, operator-sdk, or third-party. VM*.dll secondary-development SDK references use the verified catalog role vm-sdk/referenceType 6; MVD*.dll operator SDK references use the verified catalog role operator-sdk/referenceType 4. " +
            "For VM 4.4 netDxf 2023.11.10 use DrawingEntities.Polylines2D with Polyline2D and DrawingEntities.Polylines3D with Polyline3D; LwPolylines, LwPolyline, Polylines, and Polyline are not available. Polyline2D vertices expose Position, while Polyline3D Vertexes are Vector3 values directly. For bitmap drawing reference System.Drawing.dll, never System.Drawing.Common. Script.Methods.ImageData has only a parameterless constructor: convert Bitmap pixels to an RGB byte array and create new ImageData { Buffer=rgbBytes, Width=width, Height=height, PixelFormat=ImagePixelFormate.RGB24 }; never call new ImageData(bitmap). " +
            "For an external C# ShellModule DLL outside the verified reference catalog, emit dotnet-assembly with its exact name, path, architecture, role=third-party (or operator-sdk for a real operator SDK), and referenceType=4; never invent another referenceType. " +
             "The community article knowledge below is advisory and version-scoped, not an API catalog. For a request to clear a multi-image display, inspect the Patch baseline first; only write a validated affine-module input image when the module, parameter, binding, image type, and writable direction are confirmed. Do not copy method names or types from an article screenshot, and do not place operator SDK types in a ShellModule unless the target reference is verified and precompiles:\n" + secondaryDevelopmentKnowledge +
             "Additional V-Community articles are summarized below as requirement-shaping evidence. Use their state-machine, file-storage, ImageData/OpenCV, Python, HALCON, camera-SDK, and native-DLL patterns only after the exact target dependencies and VM 4.4 precompile contract are confirmed. Never infer a missing API from an article that lacks an exact signature:\n" + communityArticlesKnowledge +
             "The vm-script-tutor knowledge below is for verified manual VM 2D C# ShellModule source generation and review. Apply its .NET Framework 4.6.1/C# 5.0, UserProperty.cs read-only, direct-assignment, errorStatus, namespace/reference, AlgorithmTab.xml, ambiguity-confirmation, resource-lifecycle, and UI-configuration rules when explicit source is required. It does not support 3D, Python, controller IO, UI automation, communication-protocol parsing, or VM DLL/EXE reverse engineering; do not let it override the deterministic Requirement/Core contract or invent APIs:\n" + scriptTutorKnowledge +
             "The JSON must satisfy this schema:\n" + schema + "\nVM type system:\n" + typeSystem + "\nAPI catalog:\n" + apiCatalog + "\nVerified module parameters:\n" + moduleCatalog + "\nVerified ShellModule references:\n" + referenceCatalog;
    }

    public static CompileRequirement Parse(string ir, string mode, string? baseSolution) =>
        new LocalPromptRequirementProvider().Create(NormalizeMechanicalFields(ir), mode, baseSolution);

    private static string NormalizeMechanicalFields(string ir)
    {
        var root = JsonNode.Parse(ir) ?? throw new CompilerException("AI_RESPONSE_INVALID", "AI provider returned empty Requirement IR.");
        if (root["scripts"] is not JsonArray scripts) return ir;
        foreach (var script in scripts.OfType<JsonObject>())
        {
            if (string.Equals(script["carrier"]?.GetValue<string>(), "csharp-module", StringComparison.Ordinal)
                && script["source"] is JsonValue sourceValue
                && sourceValue.TryGetValue<string>(out var source)
                && !string.IsNullOrWhiteSpace(source))
                script["source"] = NormalizeCSharpModuleContract(source);
            if (script["dependencies"] is not JsonArray dependencies) continue;
            foreach (var dependency in dependencies.OfType<JsonObject>())
            {
                if (!string.Equals(dependency["kind"]?.GetValue<string>(), "dotnet-assembly", StringComparison.Ordinal)) continue;
                if (string.IsNullOrWhiteSpace(dependency["path"]?.GetValue<string>())) continue;
                // VM 4.4 evidence confirms referenceType 4 for any explicitly deployed
                // user assembly. Provider models occasionally copy SDK catalog values
                // (for example 6) beside an external path; that combination is mechanical,
                // unambiguous, and safe to normalize before Core validates the IR.
                dependency["referenceType"] = 4;
            }
        }
        return root.ToJsonString();
    }

    private static string NormalizeCSharpModuleContract(string source)
    {
        if (!Regex.IsMatch(source, @"\busing\s+Script\.Methods\s*;"))
            source = "using Script.Methods;\n" + source;

        var declaration = new Regex(@"\bpublic\s+(?:partial\s+)?class\s+UserScript(?<inherit>\s*:\s*[^\{]+)?\s*\{");
        var match = declaration.Match(source);
        if (!match.Success) return source;
        var inherited = match.Groups["inherit"].Success
            ? match.Groups["inherit"].Value.Trim().TrimStart(':').Split(',').Select(x => x.Trim()).Where(x => x.Length > 0).ToList()
            : [];
        if (!inherited.Any(x => x.EndsWith("ScriptMethods", StringComparison.Ordinal))) inherited.Insert(0, "ScriptMethods");
        if (!inherited.Any(x => x.EndsWith("IProcessMethods", StringComparison.Ordinal))) inherited.Add("IProcessMethods");
        var replacement = "public partial class UserScript : " + string.Join(", ", inherited) + "\n{";
        return declaration.Replace(source, replacement, 1);
    }
}

public sealed class LocalPromptRequirementProvider : IRequirementProvider
{
    public CompileRequirement Create(string prompt, string mode, string? baseSolution)
    {
        if (string.IsNullOrWhiteSpace(prompt)) throw new CompilerException("PROMPT_REQUIRED", "Prompt cannot be empty.");
        if (mode == "patch" && !string.IsNullOrWhiteSpace(baseSolution)) baseSolution = Path.GetFullPath(baseSolution);
        if (prompt.TrimStart().StartsWith('{')) return FromJson(prompt, mode, baseSolution);
        var lower = prompt.ToLowerInvariant();
        var carrier = lower.Contains("python") ? "python-module" : lower.Contains("global") || prompt.Contains("全局", StringComparison.Ordinal) ? "global-csharp" : "csharp-module";
        var procedure = Regex.Match(prompt, @"流程\s*\d+").Value.Replace(" ", "");
        if (string.IsNullOrEmpty(procedure)) procedure = "流程1";
        var sum = lower.Contains("sum") || prompt.Contains("求和", StringComparison.Ordinal) || prompt.Contains("相加", StringComparison.Ordinal) || lower.Contains("a+b");
        var clear = prompt.Contains("清空", StringComparison.Ordinal) && prompt.Contains("Clear", StringComparison.OrdinalIgnoreCase);
        if (!sum && !clear)
            throw new CompilerException("LOCAL_PROMPT_UNSUPPORTED", "离线解析器无法可靠理解该需求。请配置 OpenAI-compatible AI Provider，或在高级选项中提供经过校验的 Requirement JSON；编译器不会生成空脚本冒充成功。");
        var type = lower.Contains("int") || prompt.Contains("整数", StringComparison.Ordinal) ? "int" : "float";
        var name = clear ? "清空标定点" : sum ? (carrier == "python-module" ? "Python求和" : "CSharp求和") : carrier switch { "global-csharp" => "默认全局执行", "python-module" => "Python脚本", _ => "CSharp脚本" };
        var inputs = sum ? new List<IoRequirement> { Port("A", type, true), Port("B", type, true) } : clear ? new List<IoRequirement> { Port("ClearTrigger", "int", true) } : [];
        var outputs = sum ? new List<IoRequirement> { Port("Sum", type, false) } : clear ? new List<IoRequirement> { Port("Result", "int", false) } : [];
        var operations = sum
            ? new List<OperationRequirement> { new() { Kind = "setOutput", Parameter = "Sum", Value = BinaryAdd("A", "B"), OnError = "fail" } }
            : new List<OperationRequirement> { new() { Kind = "setModuleValue", Module = FindModule(prompt) ?? "N点标1", Parameter = "Clear", Value = JsonSerializer.SerializeToElement(""), Condition = Equal("ClearTrigger", 1), OnError = "fail" } };
        string? source = null;
        var id = "agent-" + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(prompt)))[..10].ToLowerInvariant();
        var requirement = new CompileRequirement {
            SchemaVersion = "1.0",
            Task = new TaskRequirement { Name = id, Mode = mode, VmVersion = "4.4.0", BaseSolution = mode == "patch" ? baseSolution : null },
            Scripts = [new ScriptRequirement { Id = id, Carrier = carrier, Name = name, Source = source, Procedure = carrier == "global-csharp" ? null : procedure, Execution = new ExecutionRequirement { Mode = "once", Order = 0 }, Inputs = inputs, Outputs = outputs, Operations = operations, Dependencies = [] }],
            Connections = []
        };
        ValidateGenerated(requirement);
        return requirement;
    }

    private static CompileRequirement FromJson(string prompt, string mode, string? baseSolution)
    {
        using var document = JsonDocument.Parse(prompt);
        var validation = new RequirementValidator().Validate(document);
        if (!validation.IsValid)
        {
            var code = validation.Issues.FirstOrDefault(x => x.Code != "REQUIREMENT_SCHEMA_INVALID")?.Code ?? "REQUIREMENT_SCHEMA_INVALID";
            throw new CompilerException(code, string.Join("; ", validation.Issues.Select(x => x.Path + ": " + x.Message)));
        }
        var requirement = JsonSerializer.Deserialize<CompileRequirement>(prompt, JsonDefaults.Options)!;
        if (requirement.Task.Mode != mode) throw new CompilerException("COMPILE_MODE_MISMATCH", "Prompt JSON mode does not match the Agent command.");
        if (mode == "patch")
        {
            var effectiveBase = !string.IsNullOrWhiteSpace(baseSolution) ? Path.GetFullPath(baseSolution) : null;
            if (!string.IsNullOrWhiteSpace(requirement.Task.BaseSolution) && effectiveBase is not null)
            {
                var declaredBase = Path.GetFullPath(requirement.Task.BaseSolution);
                if (!string.Equals(declaredBase, effectiveBase, StringComparison.OrdinalIgnoreCase))
                    throw new CompilerException("BASE_SOLUTION_MISMATCH", $"Prompt JSON base SOL does not match the Agent command. Command: {effectiveBase}; Requirement: {declaredBase}.");
            }
            requirement = new CompileRequirement { SchemaVersion = requirement.SchemaVersion, Task = new TaskRequirement { Name = requirement.Task.Name, Mode = mode, VmVersion = requirement.Task.VmVersion, BaseSolution = effectiveBase ?? requirement.Task.BaseSolution }, Scripts = requirement.Scripts, Connections = requirement.Connections };
        }
        return requirement;
    }

    private static IoRequirement Port(string name, string type, bool required) => new() { Name = name, Type = type, Required = required, DefaultValue = JsonSerializer.SerializeToElement(type == "float" ? 0.0f : 0) };
    private static JsonElement BinaryAdd(string left, string right) => JsonSerializer.SerializeToElement(new {
        kind = "binary", @operator = "add",
        left = new { kind = "input", name = left }, right = new { kind = "input", name = right }
    });
    private static JsonElement Equal(string input, int value) => JsonSerializer.SerializeToElement(new {
        kind = "binary", @operator = "equal",
        left = new { kind = "input", name = input }, right = new { kind = "literal", value }
    });
    private static string? FindModule(string prompt) { var match = Regex.Match(prompt, @"(?:模块|流程中)\s*([\p{L}\p{N}_-]+)"); return match.Success ? match.Groups[1].Value : null; }
    private static void ValidateGenerated(CompileRequirement requirement)
    {
        var json = JsonSerializer.Serialize(requirement, new JsonSerializerOptions(JsonDefaults.Options)
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });
        using var document = JsonDocument.Parse(json);
        var validation = new RequirementValidator().Validate(document);
        if (!validation.IsValid)
        {
            var code = validation.Issues.FirstOrDefault(x => x.Code != "REQUIREMENT_SCHEMA_INVALID")?.Code ?? "REQUIREMENT_SCHEMA_INVALID";
            throw new CompilerException(code, string.Join("; ", validation.Issues.Select(x => $"{x.Path}: {x.Message}")));
        }
    }
}
