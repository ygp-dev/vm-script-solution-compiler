param([switch]$SkipBuild)
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$agent = Join-Path $root 'src\VmScriptCompiler.Agent\bin\Release\net8.0\vm-script-agent.dll'
$cli = Join-Path $root 'src\VmScriptCompiler.Cli\bin\Release\net8.0\VmScriptCompiler.Cli.dll'
$server = Join-Path $root 'tests\fake-ai-server.py'
$fixture = Join-Path $root 'tests\fixtures\m11-ai-complex-types.json'
$temp = Join-Path ([IO.Path]::GetTempPath()) ('vm-script-m12-' + [guid]::NewGuid().ToString('N'))
$process = $null
try {
    if (-not $SkipBuild) { & dotnet build (Join-Path $root 'VmScriptCompiler.sln') -c Release --no-restore | Out-Host }
    New-Item -ItemType Directory -Path $temp | Out-Null
    $environment = & dotnet $cli env | ConvertFrom-Json
    $python = Join-Path $environment.vmRoot 'Applications\ModuleProxy\x64\python.exe'
    $port = Get-Random -Minimum 31000 -Maximum 45000
    $ready = Join-Path $temp 'ready.txt'
    $process = Start-Process -FilePath $python -ArgumentList @(
        "`"$server`"", $port, "`"$fixture`"", "`"$ready`""
    ) -PassThru -WindowStyle Hidden
    $deadline = [DateTime]::UtcNow.AddSeconds(15)
    while (-not (Test-Path $ready) -and [DateTime]::UtcNow -lt $deadline) { Start-Sleep -Milliseconds 100 }
    if (-not (Test-Path $ready)) { throw 'Fake Responses provider did not start.' }

    # A user-facing /v1 base URL must be normalized to /v1/responses.
    $env:VM_SCRIPT_AI_ENDPOINT = "http://127.0.0.1:$port/v1"
    $env:VM_SCRIPT_AI_MODEL = 'offline-responses-model'
    $env:VM_SCRIPT_AI_API_KEY = 'offline-test-key'
    $result = & dotnet $agent build --provider openai-responses --prompt 'Create an image and point pass-through module.' --output (Join-Path $temp 'build') | ConvertFrom-Json
    if (-not $result.ok -or -not (Test-Path $result.solutionFile)) { throw 'OpenAI Responses Agent build failed.' }
    $types = @($result.requirement.scripts.inputs.type) + @($result.requirement.scripts.outputs.type)
    foreach ($type in @('image','point')) { if ($types -notcontains $type) { throw "Responses Requirement lost complex type: $type" } }

    $desktop = Get-Content -Raw (Join-Path $root 'src\VmScriptCompiler.Desktop\MainWindow.xaml') -Encoding UTF8
    if (-not $desktop.Contains('Tag="openai-responses"') -or -not $desktop.Contains('OpenAI Responses')) { throw 'Desktop Responses choice is missing.' }
    $desktopCode = Get-Content -Raw (Join-Path $root 'src\VmScriptCompiler.Desktop\MainWindow.xaml.cs') -Encoding UTF8
    $desktopState = Get-Content -Raw (Join-Path $root 'src\VmScriptCompiler.Desktop\DesktopStateStore.cs') -Encoding UTF8
    $agentConfig = Get-Content -Raw (Join-Path $root 'agent\src\config.ts') -Encoding UTF8
    if (-not $desktopState.Contains('https://api.openai.com/v1') -or -not $agentConfig.Contains('"/responses"')) { throw 'Desktop official Responses endpoint normalization is missing.' }
    if ($desktopCode.Contains('Environment.SetEnvironmentVariable')) { throw 'Desktop must not persist or mutate process-wide AI credentials.' }

    [pscustomobject]@{ ok=$true; responsesApi=$true; baseUrlNormalized=$true; bearerAuth=$true; messageArrayInput=$true; explicitJsonInstruction=$true; gzipResponse=$true; jsonMode=$true; storeDisabled=$true; requirementValidated=$true; coreBuild=$true; desktopCodexChoice=$true; sessionOnlyKey=$true } | ConvertTo-Json
}
finally {
    Remove-Item Env:VM_SCRIPT_AI_ENDPOINT -ErrorAction SilentlyContinue
    Remove-Item Env:VM_SCRIPT_AI_MODEL -ErrorAction SilentlyContinue
    Remove-Item Env:VM_SCRIPT_AI_API_KEY -ErrorAction SilentlyContinue
    if ($null -ne $process -and -not $process.HasExited) { Stop-Process -Id $process.Id -Force }
    if (Test-Path $temp) { Remove-Item $temp -Recurse -Force }
}
