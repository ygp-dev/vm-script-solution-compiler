param([switch]$SkipBuild)
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$agent = Join-Path $root 'src\VmScriptCompiler.Agent\bin\Release\net8.0\vm-script-agent.dll'
$cli = Join-Path $root 'src\VmScriptCompiler.Cli\bin\Release\net8.0\VmScriptCompiler.Cli.dll'
$server = Join-Path $root 'tests\fake-ai-server.py'
$fixture = Join-Path $root 'tests\fixtures\m11-ai-complex-types.json'
$temp = Join-Path ([IO.Path]::GetTempPath()) ('vm-script-m11-' + [guid]::NewGuid().ToString('N'))
$process = $null
try {
    if (-not $SkipBuild) { & dotnet build (Join-Path $root 'VmScriptCompiler.sln') -c Release --no-restore | Out-Host }
    New-Item -ItemType Directory -Path $temp | Out-Null
    $environment = & dotnet $cli env | ConvertFrom-Json
    $python = Join-Path $environment.vmRoot 'Applications\ModuleProxy\x64\python.exe'
    $port = Get-Random -Minimum 31000 -Maximum 45000
    $ready = Join-Path $temp 'ready.txt'
    $process = Start-Process -FilePath $python -ArgumentList @($server, $port, $fixture, $ready) -PassThru -WindowStyle Hidden
    $deadline = [DateTime]::UtcNow.AddSeconds(15)
    while (-not (Test-Path $ready) -and [DateTime]::UtcNow -lt $deadline) { Start-Sleep -Milliseconds 100 }
    if (-not (Test-Path $ready)) { throw 'Fake AI provider did not start.' }
    $env:VM_SCRIPT_AI_ENDPOINT = "http://127.0.0.1:$port/v1"
    $env:VM_SCRIPT_AI_MODEL = 'offline-test-model'
    $result = & dotnet $agent build --provider openai-compatible --prompt 'Create an image and point pass-through module.' --output (Join-Path $temp 'build') | ConvertFrom-Json
    if (-not $result.ok -or -not (Test-Path $result.solutionFile)) { throw 'OpenAI-compatible Agent build failed.' }
    $types = @($result.requirement.scripts.inputs.type) + @($result.requirement.scripts.outputs.type)
    foreach ($type in @('image','point')) { if ($types -notcontains $type) { throw "AI Requirement lost complex type: $type" } }
    $precompile = Get-Content -Raw (Join-Path $result.taskDirectory 'validation\script-precompile.json') -Encoding UTF8 | ConvertFrom-Json
    if (-not $precompile.ok -or @($precompile.scripts | Where-Object exitCode -ne 0).Count -ne 0) { throw 'AI complex-type script did not precompile.' }
    $normalized = & dotnet $agent plan --provider openai-compatible --prompt 'normalize external DLL' | ConvertFrom-Json
    $dependency = @($normalized.scripts[0].dependencies)[0]
    if ($dependency.referenceType -ne 4 -or $dependency.path -ne 'C:\missing\External.Sample.dll') { throw 'AI external DLL referenceType was not normalized to 4.' }
    $contract = & dotnet $agent plan --provider openai-compatible --prompt 'normalize C# contract' | ConvertFrom-Json
    $contractSource = [string]$contract.scripts[0].source
    if ($contractSource -notmatch 'using Script\.Methods;' -or $contractSource -notmatch 'public partial class UserScript : ScriptMethods, IProcessMethods') { throw 'AI C# ShellModule contract was not normalized.' }
    $oldPreference = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'
    $unsupported = & dotnet $agent plan --provider local --prompt 'Perform an unknown vision operation.' 2>&1 | Out-String
    $unsupportedExit = $LASTEXITCODE
    $ErrorActionPreference = $oldPreference
    if ($unsupportedExit -eq 0 -or $unsupported -notmatch 'LOCAL_PROMPT_UNSUPPORTED') { throw 'Unknown local prompt produced a false-success empty script.' }
    $global:LASTEXITCODE = 0
    [pscustomobject]@{ ok=$true; openAiCompatible=$true; evidencePrompt=$true; requirementValidated=$true; externalReferenceNormalized=$true; csharpContractNormalized=$true; complexTypes=@('image','point'); coreBuild=$true; offlinePrecompile=$true; guardedEmptyScript=$true } | ConvertTo-Json
}
finally {
    Remove-Item Env:VM_SCRIPT_AI_ENDPOINT -ErrorAction SilentlyContinue
    Remove-Item Env:VM_SCRIPT_AI_MODEL -ErrorAction SilentlyContinue
    if ($null -ne $process -and -not $process.HasExited) { Stop-Process -Id $process.Id -Force }
    if (Test-Path $temp) { Remove-Item $temp -Recurse -Force }
}
