param([switch]$SkipBuild)
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$node = Join-Path $root '.runtime\node-v22.19.0-win-x64\node.exe'
$desktop = Join-Path $root 'src\VmScriptCompiler.Desktop\bin\Release\net8.0-windows\vm-script-compiler-desktop.dll'
if (-not $SkipBuild) {
    & npm run build --prefix (Join-Path $root 'agent')
    if ($LASTEXITCODE -ne 0) { throw 'Pi Agent build failed.' }
    & dotnet build (Join-Path $root 'VmScriptCompiler.sln') -c Release
    if ($LASTEXITCODE -ne 0) { throw 'dotnet build failed.' }
}

$temp = Join-Path ([IO.Path]::GetTempPath()) ('vm-script-desktop-agent-' + [guid]::NewGuid().ToString('N'))
$ready = Join-Path $temp 'ready'
$result = Join-Path $temp 'desktop-result.json'
$serverOut = Join-Path $temp 'server.out.log'
$serverError = Join-Path $temp 'server.error.log'
New-Item -ItemType Directory -Force -Path $temp | Out-Null
$listener = [Net.Sockets.TcpListener]::new([Net.IPAddress]::Loopback, 0)
$listener.Start()
$port = ([Net.IPEndPoint]$listener.LocalEndpoint).Port
$listener.Stop()
$server = $null
$previous = @{
    Provider = $env:VM_SCRIPT_AI_PROVIDER; Endpoint = $env:VM_SCRIPT_AI_ENDPOINT
    Model = $env:VM_SCRIPT_AI_MODEL; Key = $env:VM_SCRIPT_AI_API_KEY
    Home = $env:VM_SCRIPT_COMPILER_HOME; Worker = $env:VM_SCRIPT_DOMAIN_WORKER
    Output = $env:VM_SCRIPT_OUTPUT_DIRECTORY; Result = $env:VM_SCRIPT_DESKTOP_SMOKE_RESULT
}
try {
    $arguments = @(
        "`"$(Join-Path $root 'tests\fake-agent-responses-server.mjs')`""
        $port
        "`"$(Join-Path $root 'tests\fixtures\m3-shell-create.json')`""
        "`"$(Join-Path $temp 'outputs')`""
        "`"$ready`""
    )
    $server = Start-Process $node -WindowStyle Hidden -PassThru -ArgumentList $arguments -RedirectStandardOutput $serverOut -RedirectStandardError $serverError
    $deadline = [DateTime]::UtcNow.AddSeconds(15)
    while (-not (Test-Path -LiteralPath $ready) -and [DateTime]::UtcNow -lt $deadline) { Start-Sleep -Milliseconds 100 }
    if (-not (Test-Path -LiteralPath $ready)) { throw 'Fake Responses server did not start.' }

    $env:VM_SCRIPT_AI_PROVIDER = 'openai-responses'
    $env:VM_SCRIPT_AI_ENDPOINT = "http://127.0.0.1:$port/v1"
    $env:VM_SCRIPT_AI_MODEL = 'desktop-agent-model'
    $env:VM_SCRIPT_AI_API_KEY = 'offline-agent-key'
    $env:VM_SCRIPT_COMPILER_HOME = $root
    $env:VM_SCRIPT_DOMAIN_WORKER = Join-Path $root 'src\VmScriptCompiler.DomainWorker\bin\Release\net8.0\vm-script-domain-worker.dll'
    $env:VM_SCRIPT_OUTPUT_DIRECTORY = Join-Path $temp 'outputs'
    $env:VM_SCRIPT_DESKTOP_SMOKE_RESULT = $result

    & dotnet $desktop --agent-smoke-test
    if ($LASTEXITCODE -ne 0) {
        if (Test-Path -LiteralPath $result) { Get-Content -LiteralPath $result -Raw | Write-Error }
        throw 'Desktop Agent smoke process failed.'
    }
    $smoke = Get-Content -LiteralPath $result -Raw -Encoding UTF8 | ConvertFrom-Json
    if (-not $smoke.ok -or $smoke.phase -ne 'offline-validated' -or -not (Test-Path -LiteralPath $smoke.solution)) {
        throw 'Desktop Agent did not complete the domain workflow.'
    }
    if ($smoke.tools.Count -ne 2 -or
        $smoke.tools -notcontains 'vm_update_requirement' -or
        $smoke.tools -notcontains 'vm_compile_solution') {
        throw 'Desktop simple Create did not use the two-step fast path.'
    }
    $sessions = Get-ChildItem $temp -Recurse -File -Filter '*.jsonl' | Get-Content -Raw -Encoding UTF8
    if ($sessions -match 'offline-agent-key') { throw 'API Key leaked into Desktop Agent session.' }
    $smoke | ConvertTo-Json -Depth 5 -Compress
}
finally {
    $env:VM_SCRIPT_AI_PROVIDER = $previous.Provider; $env:VM_SCRIPT_AI_ENDPOINT = $previous.Endpoint
    $env:VM_SCRIPT_AI_MODEL = $previous.Model; $env:VM_SCRIPT_AI_API_KEY = $previous.Key
    $env:VM_SCRIPT_COMPILER_HOME = $previous.Home; $env:VM_SCRIPT_DOMAIN_WORKER = $previous.Worker
    $env:VM_SCRIPT_OUTPUT_DIRECTORY = $previous.Output; $env:VM_SCRIPT_DESKTOP_SMOKE_RESULT = $previous.Result
    if ($server -and -not $server.HasExited) { $server.Kill() }
    if (Test-Path -LiteralPath $temp) { Remove-Item -LiteralPath $temp -Recurse -Force }
}
