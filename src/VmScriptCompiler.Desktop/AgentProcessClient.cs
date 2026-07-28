using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.Json;
using VmScriptCompiler.Core;

namespace VmScriptCompiler.Desktop;

internal sealed record AgentConnectionOptions(
    string Provider,
    string Endpoint,
    string Model,
    string ApiKey,
    string ThinkingLevel,
    string OutputDirectory,
    string DataDirectory);

internal sealed class AgentClientException(string code, string message) : Exception(message)
{
    public string Code { get; } = code;
}

internal sealed class AgentProcessClient : IAsyncDisposable
{
    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    private readonly ConcurrentDictionary<string, TaskCompletionSource<JsonElement>> _responses = new();
    private readonly ConcurrentDictionary<string, TaskCompletionSource<JsonElement>> _runs = new();
    private readonly SemaphoreSlim _writeLock = new(1, 1);
    private Process? _process;
    private Task? _stdoutTask;
    private Task? _stderrTask;
    private int _sequence;
    private bool _disposing;

    public event EventHandler<JsonElement>? EventReceived;
    public event EventHandler<string>? DiagnosticReceived;

    public bool IsRunning => _process is { HasExited: false };
    internal int? ProcessId => _process is { HasExited: false } process ? process.Id : null;

    public async Task<JsonElement> StartAsync(AgentConnectionOptions options, CancellationToken cancellationToken = default)
    {
        await DisposeProcessAsync();
        var repositoryRoot = RepositoryLocator.Find();
        var (node, script, worker) = ResolvePayload(repositoryRoot);
        Directory.CreateDirectory(options.DataDirectory);
        Directory.CreateDirectory(options.OutputDirectory);

        var start = new ProcessStartInfo
        {
            FileName = node,
            WorkingDirectory = repositoryRoot,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardInputEncoding = new UTF8Encoding(false),
            StandardOutputEncoding = new UTF8Encoding(false),
            StandardErrorEncoding = new UTF8Encoding(false)
        };
        start.ArgumentList.Add(script);
        start.ArgumentList.Add("--repository-root");
        start.ArgumentList.Add(repositoryRoot);
        start.ArgumentList.Add("--data-directory");
        start.ArgumentList.Add(options.DataDirectory);
        start.ArgumentList.Add("--output");
        start.ArgumentList.Add(options.OutputDirectory);
        start.Environment["VM_SCRIPT_COMPILER_HOME"] = repositoryRoot;
        start.Environment["VM_SCRIPT_DOMAIN_WORKER"] = worker;
        start.Environment["VM_SCRIPT_AI_PROVIDER"] = options.Provider;
        start.Environment["VM_SCRIPT_AI_ENDPOINT"] = options.Endpoint;
        start.Environment["VM_SCRIPT_AI_MODEL"] = options.Model;
        start.Environment["VM_SCRIPT_AI_API_KEY"] = options.ApiKey;
        start.Environment["VM_SCRIPT_AI_REASONING_EFFORT"] = options.ThinkingLevel;
        start.Environment["VM_SCRIPT_OUTPUT_DIRECTORY"] = options.OutputDirectory;

        _disposing = false;
        _process = new Process { StartInfo = start, EnableRaisingEvents = true };
        _process.Exited += Process_Exited;
        if (!_process.Start()) throw new AgentClientException("AGENT_START_FAILED", "无法启动 Pi Agent Host。");
        _stdoutTask = ReadStdoutAsync(_process);
        _stderrTask = ReadStderrAsync(_process);
        return await SendAsync("initialize", new { }, cancellationToken);
    }

    public async Task<JsonElement> PromptAsync(
        string text,
        string mode,
        string? baseSolution,
        string outputDirectory,
        CancellationToken cancellationToken = default)
    {
        var id = NextId();
        var response = NewCompletion(_responses, id);
        var run = NewCompletion(_runs, id);
        await WriteAsync(new
        {
            id,
            command = "prompt",
            arguments = new { text, mode, baseSolution, outputDirectory }
        }, cancellationToken);
        var acknowledgement = await response.Task.WaitAsync(cancellationToken);
        ThrowIfFailed(acknowledgement);
        var completed = await run.Task.WaitAsync(cancellationToken);
        ThrowIfFailed(completed);
        return completed;
    }

    public Task<JsonElement> GetStateAsync(CancellationToken cancellationToken = default)
        => SendAsync("get_state", new { }, cancellationToken);

    public Task<JsonElement> ListSessionsAsync(CancellationToken cancellationToken = default)
        => SendAsync("list_sessions", new { }, cancellationToken);

    public Task<JsonElement> NewSessionAsync(CancellationToken cancellationToken = default)
        => SendAsync("new_session", new { }, cancellationToken);

    public Task<JsonElement> ResumeSessionAsync(string file, CancellationToken cancellationToken = default)
        => SendAsync("resume_session", new { file }, cancellationToken);

    public Task<JsonElement> AbortAsync(CancellationToken cancellationToken = default)
        => SendAsync("abort", new { }, cancellationToken);

    public Task<JsonElement> RecordUserValidationAsync(string note, CancellationToken cancellationToken = default)
        => SendAsync("record_user_validation", new { note }, cancellationToken);

    public async ValueTask DisposeAsync()
    {
        if (_disposing) return;
        _disposing = true;
        if (IsRunning)
        {
            using var shutdown = new CancellationTokenSource(TimeSpan.FromSeconds(2));
            try { await SendAsync("shutdown", new { }, shutdown.Token); }
            catch { }
        }
        await DisposeProcessAsync();
        _writeLock.Dispose();
    }

    public void Terminate()
    {
        _disposing = true;
        var process = _process;
        _process = null;
        FailAll(new OperationCanceledException("Desktop is closing."));
        if (process is null) return;
        try { process.StandardInput.Close(); }
        catch { }
        try
        {
            if (!process.HasExited) process.Kill(entireProcessTree: true);
        }
        catch { }
        if (_stdoutTask is not null) _ = Ignore(_stdoutTask);
        if (_stderrTask is not null) _ = Ignore(_stderrTask);
        process.Dispose();
        _stdoutTask = null;
        _stderrTask = null;
    }

    private async Task<JsonElement> SendAsync(string command, object arguments, CancellationToken cancellationToken)
    {
        var id = NextId();
        var completion = NewCompletion(_responses, id);
        await WriteAsync(new { id, command, arguments }, cancellationToken);
        var response = await completion.Task.WaitAsync(cancellationToken);
        ThrowIfFailed(response);
        return response.GetProperty("result").Clone();
    }

    private async Task WriteAsync(object value, CancellationToken cancellationToken)
    {
        var process = _process;
        if (process is null || process.HasExited)
            throw new AgentClientException("AGENT_NOT_RUNNING", "Pi Agent Host 未运行。请检查配置并重新连接。");
        var line = JsonSerializer.Serialize(value, Json);
        await _writeLock.WaitAsync(cancellationToken);
        try
        {
            await process.StandardInput.WriteLineAsync(line.AsMemory(), cancellationToken);
            await process.StandardInput.FlushAsync(cancellationToken);
        }
        finally { _writeLock.Release(); }
    }

    private async Task ReadStdoutAsync(Process process)
    {
        try
        {
            while (await process.StandardOutput.ReadLineAsync() is { } line)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                JsonElement message;
                try
                {
                    using var document = JsonDocument.Parse(line.TrimStart('\uFEFF'));
                    message = document.RootElement.Clone();
                }
                catch (JsonException error)
                {
                    DiagnosticReceived?.Invoke(this, "Agent 返回无效 JSONL：" + error.Message);
                    continue;
                }

                var type = message.TryGetProperty("type", out var typeElement) ? typeElement.GetString() : null;
                var id = message.TryGetProperty("id", out var idElement) ? Id(idElement) : null;
                if (type == "response" && id is not null && _responses.TryRemove(id, out var response))
                {
                    response.TrySetResult(message);
                    continue;
                }
                if (type == "run_completed" && id is not null && _runs.TryRemove(id, out var run))
                {
                    run.TrySetResult(message);
                    continue;
                }
                if (type == "event") EventReceived?.Invoke(this, message);
            }
        }
        catch (Exception error) when (!_disposing)
        {
            FailAll(new AgentClientException("AGENT_PROTOCOL_FAILED", error.Message));
        }
    }

    private async Task ReadStderrAsync(Process process)
    {
        while (await process.StandardError.ReadLineAsync() is { } line)
            if (!string.IsNullOrWhiteSpace(line)) DiagnosticReceived?.Invoke(this, line);
    }

    private void Process_Exited(object? sender, EventArgs e)
    {
        if (_disposing) return;
        var code = _process?.ExitCode;
        FailAll(new AgentClientException("AGENT_EXITED", $"Pi Agent Host 已退出（code={code}）。"));
    }

    private void FailAll(Exception error)
    {
        foreach (var pending in _responses.Values) pending.TrySetException(error);
        foreach (var pending in _runs.Values) pending.TrySetException(error);
        _responses.Clear();
        _runs.Clear();
    }

    private async Task DisposeProcessAsync()
    {
        var process = _process;
        _process = null;
        if (process is null) return;
        try
        {
            if (!process.HasExited)
            {
                process.StandardInput.Close();
                try
                {
                    await process.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(2));
                }
                catch (TimeoutException)
                {
                    process.Kill(entireProcessTree: true);
                    await process.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(1));
                }
            }
        }
        catch { }
        if (_stdoutTask is not null) await IgnoreWithinAsync(_stdoutTask, TimeSpan.FromSeconds(1)).ConfigureAwait(false);
        if (_stderrTask is not null) await IgnoreWithinAsync(_stderrTask, TimeSpan.FromSeconds(1)).ConfigureAwait(false);
        process.Dispose();
        _stdoutTask = null;
        _stderrTask = null;
    }

    private static async Task Ignore(Task task)
    {
        try { await task; }
        catch { }
    }

    private static async Task IgnoreWithinAsync(Task task, TimeSpan timeout)
    {
        try { await Ignore(task).WaitAsync(timeout).ConfigureAwait(false); }
        catch (TimeoutException) { }
    }

    private string NextId() => "desktop-" + Interlocked.Increment(ref _sequence);

    private static TaskCompletionSource<JsonElement> NewCompletion(
        ConcurrentDictionary<string, TaskCompletionSource<JsonElement>> dictionary,
        string id)
    {
        var completion = new TaskCompletionSource<JsonElement>(TaskCreationOptions.RunContinuationsAsynchronously);
        if (!dictionary.TryAdd(id, completion)) throw new InvalidOperationException("Duplicate Agent RPC id.");
        return completion;
    }

    private static void ThrowIfFailed(JsonElement response)
    {
        if (response.TryGetProperty("ok", out var ok) && ok.GetBoolean()) return;
        var error = response.TryGetProperty("error", out var value) ? value : default;
        var code = error.ValueKind == JsonValueKind.Object && error.TryGetProperty("code", out var codeValue)
            ? codeValue.GetString() ?? "AGENT_FAILED"
            : "AGENT_FAILED";
        var message = error.ValueKind == JsonValueKind.Object && error.TryGetProperty("message", out var messageValue)
            ? messageValue.GetString() ?? "Agent 操作失败。"
            : "Agent 操作失败。";
        throw new AgentClientException(code, message);
    }

    private static string? Id(JsonElement value) => value.ValueKind switch
    {
        JsonValueKind.String => value.GetString(),
        JsonValueKind.Number => value.GetRawText(),
        _ => null
    };

    private static (string Node, string Script, string Worker) ResolvePayload(string repositoryRoot)
    {
        var baseDirectory = AppContext.BaseDirectory;
        var node = FirstExisting(
            Path.Combine(baseDirectory, "runtime", "node.exe"),
            Path.Combine(repositoryRoot, ".runtime", "node-v22.19.0-win-x64", "node.exe"));
        var script = FirstExisting(
            Path.Combine(baseDirectory, "agent", "dist", "main.js"),
            Path.Combine(repositoryRoot, "agent", "dist", "main.js"));
        var worker = FirstExisting(
            Path.Combine(baseDirectory, "worker", "vm-script-domain-worker.exe"),
            Path.Combine(repositoryRoot, "src", "VmScriptCompiler.DomainWorker", "bin", "Release", "net8.0", "vm-script-domain-worker.dll"));
        return (node, script, worker);
    }

    private static string FirstExisting(params string[] candidates)
        => candidates.FirstOrDefault(File.Exists)
           ?? throw new AgentClientException("AGENT_PAYLOAD_MISSING", "Agent 运行组件缺失：" + string.Join("; ", candidates));
}
