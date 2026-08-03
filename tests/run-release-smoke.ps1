$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
[xml]$versionProps = Get-Content -LiteralPath (Join-Path $root 'Directory.Build.props') -Raw -Encoding UTF8
$expectedVersion = [string]$versionProps.Project.PropertyGroup.Version
$desktopRoot = Join-Path $root 'dist\Desktop'
$desktop = Join-Path $desktopRoot 'vm-script-compiler-desktop.exe'
$node = Join-Path $desktopRoot 'runtime\node.exe'
$temp = Join-Path ([IO.Path]::GetTempPath()) ('vm-release-standalone-' + [guid]::NewGuid().ToString('N'))
$old = @{
    Home=$env:VM_SCRIPT_COMPILER_HOME; Provider=$env:VM_SCRIPT_AI_PROVIDER
    Endpoint=$env:VM_SCRIPT_AI_ENDPOINT; Model=$env:VM_SCRIPT_AI_MODEL
    Key=$env:VM_SCRIPT_AI_API_KEY; Worker=$env:VM_SCRIPT_DOMAIN_WORKER
    Output=$env:VM_SCRIPT_OUTPUT_DIRECTORY; Result=$env:VM_SCRIPT_DESKTOP_SMOKE_RESULT
}
$server = $null
try {
    New-Item -ItemType Directory -Path $temp | Out-Null
    Remove-Item Env:VM_SCRIPT_COMPILER_HOME -ErrorAction SilentlyContinue
    Push-Location $temp
    try {
        $cli = & (Join-Path $root 'dist\Cli\VmScriptCompiler.Cli.exe') env | ConvertFrom-Json
        if (-not $cli.found) { throw 'Standalone CLI environment detection failed.' }

        $desktopSmoke = Start-Process $desktop -WindowStyle Hidden -PassThru -Wait -ArgumentList '--smoke-test'
        if ($desktopSmoke.ExitCode -ne 0) { throw 'Standalone Desktop composition smoke failed.' }

        $ready = Join-Path $temp 'ready'
        $result = Join-Path $temp 'desktop-agent-result.json'
        $serverOut = Join-Path $temp 'server.out.log'
        $serverError = Join-Path $temp 'server.error.log'
        $listener = [Net.Sockets.TcpListener]::new([Net.IPAddress]::Loopback, 0)
        $listener.Start()
        $port = ([Net.IPEndPoint]$listener.LocalEndpoint).Port
        $listener.Stop()
        $server = Start-Process $node -WindowStyle Hidden -PassThru -ArgumentList @(
            (Join-Path $root 'tests\fake-agent-responses-server.mjs'),
            $port,
            (Join-Path $root 'tests\fixtures\m3-shell-create.json'),
            (Join-Path $temp 'outputs'),
            $ready
        ) -RedirectStandardOutput $serverOut -RedirectStandardError $serverError
        $deadline = [DateTime]::UtcNow.AddSeconds(15)
        while (-not (Test-Path -LiteralPath $ready) -and [DateTime]::UtcNow -lt $deadline) { Start-Sleep -Milliseconds 100 }
        if (-not (Test-Path -LiteralPath $ready)) { throw 'Standalone fake Responses server did not start.' }

        $env:VM_SCRIPT_COMPILER_HOME = $desktopRoot
        $env:VM_SCRIPT_AI_PROVIDER = 'openai-responses'
        $env:VM_SCRIPT_AI_ENDPOINT = "http://127.0.0.1:$port/v1"
        $env:VM_SCRIPT_AI_MODEL = 'standalone-agent-model'
        $env:VM_SCRIPT_AI_API_KEY = 'offline-agent-key'
        Remove-Item Env:VM_SCRIPT_DOMAIN_WORKER -ErrorAction SilentlyContinue
        $env:VM_SCRIPT_OUTPUT_DIRECTORY = Join-Path $temp 'outputs'
        $env:VM_SCRIPT_DESKTOP_SMOKE_RESULT = $result
        $desktopAgentSmoke = Start-Process $desktop -WindowStyle Hidden -PassThru -Wait -ArgumentList '--agent-smoke-test'
        if ($desktopAgentSmoke.ExitCode -ne 0) {
            if (Test-Path -LiteralPath $result) { Get-Content -LiteralPath $result -Raw | Write-Error }
            throw 'Standalone Desktop Agent smoke failed.'
        }
        $agent = Get-Content -LiteralPath $result -Raw -Encoding UTF8 | ConvertFrom-Json
        if (-not $agent.ok -or $agent.phase -ne 'offline-validated' -or -not (Test-Path -LiteralPath $agent.solution)) {
            throw 'Standalone Desktop did not produce an offline-validated SOL.'
        }

        $request = '{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2024-11-05","capabilities":{},"clientInfo":{"name":"standalone-audit","version":"1"}}}'
        $response = $request | & (Join-Path $root 'dist\Mcp\vm-script-compiler-mcp.exe') | ConvertFrom-Json
        if ($response.result.serverInfo.name -ne 'vm-script-solution-compiler' -or $response.result.serverInfo.version -ne $expectedVersion) {
            throw 'Standalone MCP initialize failed.'
        }

        $manifest = Get-Content -Raw -Encoding UTF8 (Join-Path $root 'dist\release-manifest.json') | ConvertFrom-Json
        foreach ($product in $manifest.products) {
            $directory = Join-Path (Join-Path $root 'dist') $product.name
            $assembly = Join-Path $directory $product.applicationAssembly
            if ((Get-FileHash $assembly -Algorithm SHA256).Hash -ne $product.applicationAssemblySha256) { throw "Release assembly hash mismatch: $($product.name)" }
            if (-not (Test-Path (Join-Path $directory 'resources\vm\4.4.0\manifest.json')) -or
                -not (Test-Path (Join-Path $directory 'schemas\requirement.schema.json')) -or
                -not (Test-Path (Join-Path $directory 'tools\vm-solution-parser\VMSolutionParser.Cli.exe'))) {
                throw "Standalone payload is incomplete: $($product.name)"
            }
        }
        foreach ($required in @('runtime\node.exe','agent\dist\main.js','agent\resources\SYSTEM.md','worker\vm-script-domain-worker.exe')) {
            if (-not (Test-Path -LiteralPath (Join-Path $desktopRoot $required))) { throw "Desktop Agent component missing: $required" }
        }
        foreach ($required in @(
            'agent\dist\tools\requirement-tool-schema.js',
            'agent\dist\system-prompt.js',
            'agent\resources\requirement-examples\python-create.json',
            'agent\resources\requirement-examples\global-csharp-create.json'
        )) {
            if (-not (Test-Path -LiteralPath (Join-Path $desktopRoot $required))) {
                throw "Desktop Requirement guidance component missing: $required"
            }
        }
        $schemaTool = Get-Content -LiteralPath (Join-Path $desktopRoot 'agent\dist\tools\requirement-tool-schema.js') -Raw -Encoding UTF8
        if (-not $schemaTool.Contains('python-module') -or -not $schemaTool.Contains('once') -or -not $schemaTool.Contains('source')) {
            throw 'Published Desktop Requirement tool schema is incomplete.'
        }
        $desktopCore = (Get-FileHash (Join-Path $desktopRoot 'VmScriptCompiler.Core.dll') -Algorithm SHA256).Hash
        $workerCore = (Get-FileHash (Join-Path $desktopRoot 'worker\VmScriptCompiler.Core.dll') -Algorithm SHA256).Hash
        $mcpCore = (Get-FileHash (Join-Path $root 'dist\Mcp\VmScriptCompiler.Core.dll') -Algorithm SHA256).Hash
        if ($desktopCore -ne $workerCore -or $desktopCore -ne $mcpCore) { throw 'Shared deterministic Core payloads are not synchronized.' }
        if (-not $manifest.componentSynchronization.desktopWorkerAndMcp -or
            $manifest.componentSynchronization.coreSha256 -ne $desktopCore -or
            [string]::IsNullOrWhiteSpace($manifest.componentSynchronization.agentPayloadSha256) -or
            $manifest.architecture.primaryProduct -ne 'Desktop' -or
            $manifest.architecture.legacyAgentPublished) {
            throw 'Release architecture manifest is invalid.'
        }
        $solFiles = @(
            Get-ChildItem (Join-Path $root 'dist\Cli\resources') -Recurse -File -Filter *.sol
            Get-ChildItem (Join-Path $root 'dist\Mcp\resources') -Recurse -File -Filter *.sol
            Get-ChildItem (Join-Path $root 'dist\Desktop\resources') -Recurse -File -Filter *.sol
            Get-ChildItem (Join-Path $root 'dist\Desktop\agent\dist') -Recurse -File -Filter *.sol
            Get-ChildItem (Join-Path $root 'dist\Desktop\agent\resources') -Recurse -File -Filter *.sol
            Get-ChildItem (Join-Path $root 'dist\Desktop\worker\resources') -Recurse -File -Filter *.sol
        )
        if ($solFiles.Count -ne 0) {
            throw 'Release payload contains SOL files.'
        }

        [pscustomobject]@{
            ok=$true; cli=$true; desktop=$true; desktopAgent=$true
            phase=$agent.phase; mcpVersion=$response.result.serverInfo.version
            synchronized=$true; containsSolFiles=$false
        } | ConvertTo-Json
    }
    finally { Pop-Location }
}
finally {
    $env:VM_SCRIPT_COMPILER_HOME=$old.Home; $env:VM_SCRIPT_AI_PROVIDER=$old.Provider
    $env:VM_SCRIPT_AI_ENDPOINT=$old.Endpoint; $env:VM_SCRIPT_AI_MODEL=$old.Model
    $env:VM_SCRIPT_AI_API_KEY=$old.Key; $env:VM_SCRIPT_DOMAIN_WORKER=$old.Worker
    $env:VM_SCRIPT_OUTPUT_DIRECTORY=$old.Output; $env:VM_SCRIPT_DESKTOP_SMOKE_RESULT=$old.Result
    if ($server -and -not $server.HasExited) { $server.Kill() }
    for ($attempt = 0; $attempt -lt 10 -and (Test-Path $temp); $attempt++) {
        try { Remove-Item -LiteralPath $temp -Recurse -Force -ErrorAction Stop }
        catch { Start-Sleep -Milliseconds 200 }
    }
    if (Test-Path $temp) { Write-Warning "Temporary release-smoke directory is still locked: $temp" }
}
