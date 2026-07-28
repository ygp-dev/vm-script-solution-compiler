using System.Windows;
using System.IO;
using System.Text.Json;

namespace VmScriptCompiler.Desktop;

public partial class App : System.Windows.Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        if (e.Args.Contains("--agent-smoke-test", StringComparer.Ordinal))
        {
            try
            {
                Task.Run(RunAgentSmokeTestAsync).GetAwaiter().GetResult();
                Shutdown(0);
            }
            catch (Exception ex)
            {
                var result = Environment.GetEnvironmentVariable("VM_SCRIPT_DESKTOP_SMOKE_RESULT")
                    ?? Path.Combine(Path.GetTempPath(), "vm-script-desktop-agent-smoke-error.txt");
                File.WriteAllText(result, JsonSerializer.Serialize(new
                {
                    ok = false,
                    error = ex is AgentClientException agent ? agent.Code : "UNEXPECTED_ERROR",
                    message = ex.Message,
                    detail = ex.ToString()
                }));
                Shutdown(1);
            }
            return;
        }
        if (e.Args.Contains("--smoke-test", StringComparer.Ordinal))
        {
            try
            {
                DesktopStateStore.RunSmokeTest();
                _ = new MainWindow();
                Shutdown(0);
            }
            catch (Exception ex)
            {
                File.WriteAllText(Path.Combine(Path.GetTempPath(), "vm-script-desktop-smoke-error.txt"), ex.ToString());
                Shutdown(1);
            }
            return;
        }
        new MainWindow().Show();
    }

    private static async Task RunAgentSmokeTestAsync()
    {
        var root = Path.Combine(Path.GetTempPath(), "vm-script-desktop-agent-" + Guid.NewGuid().ToString("N"));
        var output = Environment.GetEnvironmentVariable("VM_SCRIPT_OUTPUT_DIRECTORY") ?? Path.Combine(root, "outputs");
        var resultFile = Environment.GetEnvironmentVariable("VM_SCRIPT_DESKTOP_SMOKE_RESULT") ?? Path.Combine(root, "result.json");
        Directory.CreateDirectory(root);
        await using var client = new AgentProcessClient();
        var tools = new HashSet<string>(StringComparer.Ordinal);
        client.EventReceived += (_, message) =>
        {
            if (!message.TryGetProperty("event", out var envelope) ||
                !envelope.TryGetProperty("event", out var piEvent) ||
                !piEvent.TryGetProperty("type", out var type) ||
                type.GetString() != "tool_execution_start" ||
                !piEvent.TryGetProperty("toolName", out var toolName)) return;
            tools.Add(toolName.GetString() ?? "");
        };
        await client.StartAsync(new AgentConnectionOptions(
            Environment.GetEnvironmentVariable("VM_SCRIPT_AI_PROVIDER") ?? "openai-responses",
            Environment.GetEnvironmentVariable("VM_SCRIPT_AI_ENDPOINT") ?? throw new InvalidOperationException("Missing endpoint."),
            Environment.GetEnvironmentVariable("VM_SCRIPT_AI_MODEL") ?? throw new InvalidOperationException("Missing model."),
            Environment.GetEnvironmentVariable("VM_SCRIPT_AI_API_KEY") ?? throw new InvalidOperationException("Missing key."),
            "high",
            output,
            Path.Combine(root, "data")));
        var completed = await client.PromptAsync(
            "Create a CSharp A plus B script solution and complete deterministic offline validation.",
            "create",
            null,
            output,
            new CancellationTokenSource(TimeSpan.FromMinutes(2)).Token);
        var state = completed.GetProperty("state").GetProperty("state");
        var phase = state.GetProperty("phase").GetString();
        var solutionArtifact = state.GetProperty("artifacts").EnumerateArray()
            .LastOrDefault(item => item.GetProperty("kind").GetString() == "solution");
        var solution = solutionArtifact.ValueKind == JsonValueKind.Object &&
                       solutionArtifact.TryGetProperty("path", out var path)
            ? path.GetString()
            : null;
        if (phase != "offline-validated" || string.IsNullOrWhiteSpace(solution) || !File.Exists(solution))
            throw new InvalidOperationException(
                $"Desktop Agent client did not produce an offline-validated SOL. phase={phase}; state={state.GetRawText()}");
        File.WriteAllText(resultFile, JsonSerializer.Serialize(new
        {
            ok = true,
            phase,
            solution,
            tools = tools.OrderBy(x => x).ToArray()
        }));
    }
}
