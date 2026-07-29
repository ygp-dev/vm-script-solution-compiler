param(
    [switch]$SkipBuild,
    [string]$FixtureFile,
    [string]$PromptText = 'Create a CSharp A plus B script solution and complete deterministic offline validation.',
    [string]$ExpectedCarrier = 'csharp-module',
    [string[]]$ExpectedSourceFragments = @()
)
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$agent = Join-Path $root 'agent'
$node = Join-Path $root '.runtime\node-v22.19.0-win-x64\node.exe'
$worker = Join-Path $root 'src\VmScriptCompiler.DomainWorker\bin\Release\net8.0\vm-script-domain-worker.dll'
if ([string]::IsNullOrWhiteSpace($FixtureFile)) {
    $FixtureFile = Join-Path $root 'tests\fixtures\m3-shell-create.json'
}
if (-not (Test-Path -LiteralPath $node)) { throw 'Pinned Node 22.19.0 runtime is missing.' }
if (-not $SkipBuild) {
    & dotnet build (Join-Path $root 'VmScriptCompiler.sln') -c Release
    if ($LASTEXITCODE -ne 0) { throw 'dotnet build failed.' }
    & npm run build --prefix $agent
    if ($LASTEXITCODE -ne 0) { throw 'Pi Agent build failed.' }
}

$temp = Join-Path ([IO.Path]::GetTempPath()) ('vm-script-agent-domain-' + [guid]::NewGuid().ToString('N'))
$ready = Join-Path $temp 'ready'
$serverOut = Join-Path $temp 'server.out.log'
$serverError = Join-Path $temp 'server.error.log'
$agentError = Join-Path $temp 'agent.error.log'
New-Item -ItemType Directory -Force -Path $temp | Out-Null
$listener = [Net.Sockets.TcpListener]::new([Net.IPAddress]::Loopback, 0)
$listener.Start()
$port = ([Net.IPEndPoint]$listener.LocalEndpoint).Port
$listener.Stop()
$server = $null
$process = $null
$previous = @{
    Provider = $env:VM_SCRIPT_AI_PROVIDER
    Endpoint = $env:VM_SCRIPT_AI_ENDPOINT
    Model = $env:VM_SCRIPT_AI_MODEL
    Key = $env:VM_SCRIPT_AI_API_KEY
    Home = $env:VM_SCRIPT_COMPILER_HOME
    Worker = $env:VM_SCRIPT_DOMAIN_WORKER
}

try {
    $serverArguments = @(
        (Join-Path $root 'tests\fake-agent-responses-server.mjs')
        $port
        ([IO.Path]::GetFullPath($FixtureFile))
        (Join-Path $temp 'outputs')
        $ready
    )
    $server = Start-Process $node -WindowStyle Hidden -PassThru -ArgumentList $serverArguments -RedirectStandardOutput $serverOut -RedirectStandardError $serverError
    $deadline = [DateTime]::UtcNow.AddSeconds(15)
    while (-not (Test-Path -LiteralPath $ready) -and [DateTime]::UtcNow -lt $deadline) {
        Start-Sleep -Milliseconds 100
    }
    if (-not (Test-Path -LiteralPath $ready)) {
        if (Test-Path -LiteralPath $serverError) { Get-Content -LiteralPath $serverError -Raw | Write-Error }
        throw 'Fake Agent Responses server did not start.'
    }

    $env:VM_SCRIPT_AI_PROVIDER = 'openai-responses'
    $env:VM_SCRIPT_AI_ENDPOINT = "http://127.0.0.1:$port/v1"
    $env:VM_SCRIPT_AI_MODEL = 'offline-agent-model'
    $env:VM_SCRIPT_AI_API_KEY = 'offline-agent-key'
    $env:VM_SCRIPT_COMPILER_HOME = $root
    $env:VM_SCRIPT_DOMAIN_WORKER = $worker

    $start = [Diagnostics.ProcessStartInfo]::new()
    $start.FileName = $node
    $mainScript = Join-Path $agent 'dist\main.js'
    $dataDirectory = Join-Path $temp 'data'
    $outputDirectory = Join-Path $temp 'outputs'
    $start.Arguments = "`"$mainScript`" --data-directory `"$dataDirectory`" --output `"$outputDirectory`""
    $start.WorkingDirectory = $root
    $start.UseShellExecute = $false
    $start.CreateNoWindow = $true
    $start.RedirectStandardInput = $true
    $start.RedirectStandardOutput = $true
    $start.RedirectStandardError = $true
    $start.StandardOutputEncoding = [Text.UTF8Encoding]::new($false)
    $start.StandardErrorEncoding = [Text.UTF8Encoding]::new($false)
    $process = [Diagnostics.Process]::new()
    $process.StartInfo = $start
    [void]$process.Start()

    $process.StandardInput.WriteLine('{"id":"init","command":"initialize","arguments":{}}')
    $process.StandardInput.Flush()
    $init = $null
    $deadline = [DateTime]::UtcNow.AddSeconds(20)
    while ($null -eq $init -and [DateTime]::UtcNow -lt $deadline) {
        $line = $process.StandardOutput.ReadLine()
        if ($null -eq $line) { break }
        $candidate = $line | ConvertFrom-Json
        if ($candidate.type -eq 'response' -and $candidate.id -eq 'init') { $init = $candidate }
    }
    if (-not $init.ok -or $init.result.model.id -ne 'offline-agent-model') {
        if ($process.HasExited) { $process.StandardError.ReadToEnd() | Write-Error }
        throw 'Agent RPC initialize failed.'
    }

    $prompt = @{
        id = 'run'
        command = 'prompt'
        arguments = @{
            text = $PromptText
            mode = 'create'
            outputDirectory = (Join-Path $temp 'outputs')
        }
    } | ConvertTo-Json -Depth 8 -Compress
    $process.StandardInput.WriteLine($prompt)
    $process.StandardInput.Flush()

    $toolNames = [Collections.Generic.HashSet[string]]::new()
    $runCompleted = $null
    $deadline = [DateTime]::UtcNow.AddMinutes(2)
    while ($null -eq $runCompleted -and [DateTime]::UtcNow -lt $deadline) {
        $line = $process.StandardOutput.ReadLine()
        if ($null -eq $line) { break }
        $item = $line | ConvertFrom-Json
        if ($item.type -eq 'event' -and $item.event.type -eq 'pi' -and $item.event.event.type -eq 'tool_execution_start') {
            [void]$toolNames.Add([string]$item.event.event.toolName)
        }
        if ($item.type -eq 'run_completed' -and $item.id -eq 'run') { $runCompleted = $item }
    }
    if ($null -eq $runCompleted) { throw 'Agent run did not complete.' }
    if (-not $runCompleted.ok) { throw "Agent run failed: $($runCompleted.error.code) $($runCompleted.error.message)" }
    if ($runCompleted.state.state.phase -ne 'offline-validated') { throw 'Agent did not reach offline-validated.' }
    if ($runCompleted.state.state.requirement.scripts[0].carrier -ne $ExpectedCarrier) {
        throw "Agent Requirement carrier mismatch. Expected $ExpectedCarrier."
    }
    foreach ($requiredTool in @(
        'vm_update_requirement',
        'vm_compile_solution'
    )) {
        if (-not $toolNames.Contains($requiredTool)) { throw "Agent did not call $requiredTool." }
    }
    if ($toolNames.Count -ne 2) { throw "Simple Create should use exactly two tools, actual: $($toolNames -join ', ')." }
    $solution = $runCompleted.state.state.artifacts |
        Where-Object kind -eq 'solution' |
        Select-Object -Last 1 -ExpandProperty path
    if (-not (Test-Path -LiteralPath $solution)) { throw 'Agent result SOL is missing.' }
    $generatedSourceVerified = $ExpectedSourceFragments.Count -eq 0
    if ($ExpectedSourceFragments.Count -gt 0) {
        $extension = if ($ExpectedCarrier -eq 'python-module') { '*.py' } else { '*.cs' }
        $source = Get-ChildItem (Join-Path (Split-Path $solution -Parent) 'generated') -File -Filter $extension |
            Select-Object -First 1
        if ($null -eq $source) { throw "Generated source is missing for $ExpectedCarrier." }
        $sourceText = Get-Content -LiteralPath $source.FullName -Raw -Encoding UTF8
        foreach ($fragment in $ExpectedSourceFragments) {
            if (-not $sourceText.Contains($fragment)) { throw "Generated source is missing: $fragment" }
        }
        $generatedSourceVerified = $true
    }

    $process.StandardInput.WriteLine('{"id":"shutdown","command":"shutdown","arguments":{}}')
    $process.StandardInput.Flush()
    $shutdownSeen = $false
    while (-not $shutdownSeen -and -not $process.HasExited) {
        $line = $process.StandardOutput.ReadLine()
        if ($null -eq $line) { break }
        $item = $line | ConvertFrom-Json
        if ($item.type -eq 'response' -and $item.id -eq 'shutdown' -and $item.ok) { $shutdownSeen = $true }
    }
    $process.StandardInput.Close()
    if (-not $process.WaitForExit(10000)) { $process.Kill() }
    if (-not $shutdownSeen) { throw 'Agent shutdown acknowledgement is missing.' }

    $sessionFiles = @(Get-ChildItem (Join-Path $temp 'data\sessions') -Recurse -File)
    $sessionBytes = ($sessionFiles | Measure-Object Length -Sum).Sum
    $sessionText = $sessionFiles | Get-Content -Raw -Encoding UTF8
    if ($sessionText -match 'offline-agent-key') { throw 'API Key leaked into Agent session.' }
    if ($sessionBytes -gt 512KB) { throw "Agent session log is unexpectedly large: $sessionBytes bytes." }

    [pscustomobject]@{
        ok = $true
        tools = @($toolNames)
        phase = $runCompleted.state.state.phase
        carrier = $runCompleted.state.state.requirement.scripts[0].carrier
        generatedSourceVerified = $generatedSourceVerified
        sessionBytes = $sessionBytes
        solution = $solution
    } | ConvertTo-Json -Depth 5 -Compress
}
finally {
    $env:VM_SCRIPT_AI_PROVIDER = $previous.Provider
    $env:VM_SCRIPT_AI_ENDPOINT = $previous.Endpoint
    $env:VM_SCRIPT_AI_MODEL = $previous.Model
    $env:VM_SCRIPT_AI_API_KEY = $previous.Key
    $env:VM_SCRIPT_COMPILER_HOME = $previous.Home
    $env:VM_SCRIPT_DOMAIN_WORKER = $previous.Worker
    if ($process -and -not $process.HasExited) { $process.Kill() }
    if ($server -and -not $server.HasExited) { $server.Kill() }
    if (Test-Path -LiteralPath $temp) { Remove-Item -LiteralPath $temp -Recurse -Force }
}
