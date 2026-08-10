param([switch]$SkipBuild)
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$resourceRoot = Join-Path $root 'resources\vm\4.4.0'
$knowledgePath = Join-Path $resourceRoot 'community-articles-knowledge.json'
$manifestPath = Join-Path $resourceRoot 'manifest.json'
$agentDll = Join-Path $root 'src\VmScriptCompiler.Agent\bin\Release\net8.0\vm-script-agent.dll'
$cliDll = Join-Path $root 'src\VmScriptCompiler.Cli\bin\Release\net8.0\VmScriptCompiler.Cli.dll'
$node = Join-Path $root '.runtime\node-v22.19.0-win-x64\node.exe'
if (-not (Test-Path -LiteralPath $node -PathType Leaf)) { $node = (Get-Command node -ErrorAction Stop).Source }
$temp = Join-Path ([IO.Path]::GetTempPath()) ('vm-script-community-knowledge-' + [guid]::NewGuid().ToString('N'))
try {
    if (-not $SkipBuild) {
        & dotnet build (Join-Path $root 'VmScriptCompiler.sln') -c Release --no-restore | Out-Host
        if ($LASTEXITCODE -ne 0) { throw 'Core/Agent build failed.' }
        & npm run build --prefix (Join-Path $root 'agent') | Out-Host
        if ($LASTEXITCODE -ne 0) { throw 'TypeScript Agent build failed.' }
    }
    foreach ($file in @($knowledgePath, $manifestPath, $agentDll, $cliDll)) {
        if (-not (Test-Path -LiteralPath $file -PathType Leaf)) { throw "Community knowledge prerequisite is missing: $file" }
    }

    $knowledge = Get-Content -LiteralPath $knowledgePath -Raw -Encoding UTF8 | ConvertFrom-Json
    if (@($knowledge.sources).Count -ne 17) { throw 'The community source index does not contain all 17 requested articles.' }
    if (@($knowledge.patterns).Count -lt 8) { throw 'The community pattern index is unexpectedly incomplete.' }
    $ids = @($knowledge.sources | ForEach-Object id)
    foreach ($pattern in @($knowledge.patterns)) {
        foreach ($sourceId in @($pattern.sourceIds)) {
            if ($ids -notcontains $sourceId) { throw "Pattern $($pattern.id) references missing source $sourceId." }
        }
    }
    $manifest = Get-Content -LiteralPath $manifestPath -Raw -Encoding UTF8 | ConvertFrom-Json
    $actualHash = (Get-FileHash -LiteralPath $knowledgePath -Algorithm SHA256).Hash.ToUpperInvariant()
    $declaredHash = [string]$manifest.hashes.'community-articles-knowledge.json'
    if ($actualHash -ne $declaredHash) { throw 'Community knowledge SHA-256 does not match the resource manifest.' }

    $promptCheck = @'
import { buildVmSystemPrompt } from './agent/dist/system-prompt.js';
const root = process.cwd();
const prompt = buildVmSystemPrompt({ repositoryRoot: root, agentRoot: root + '/agent' });
for (const term of ['community-articles-knowledge', 'OpenCvSharp', 'InputImageData', 'TMVSAffineTransformModuTool']) {
  if (!prompt.includes(term)) throw new Error('Agent system prompt is missing: ' + term);
}
'@
    $promptCheck | & $node --input-type=module -
    if ($LASTEXITCODE -ne 0) { throw 'Agent system prompt did not load the community knowledge rules.' }

    New-Item -ItemType Directory -Force -Path $temp | Out-Null
    $plan = & dotnet $agentDll plan --prompt '生成一个 C# 脚本，在流程1中输入整数 A 和 B，输出 Sum=A+B' | ConvertFrom-Json
    if (-not $plan.task -or @($plan.scripts).Count -ne 1) { throw 'Agent local Requirement plan regression detected.' }
    $envResult = & dotnet $cliDll env | ConvertFrom-Json
    if (-not $envResult.found) { throw 'CLI environment/resource validation failed.' }

    [pscustomobject]@{
        ok = $true
        sourceCount = @($knowledge.sources).Count
        patternCount = @($knowledge.patterns).Count
        manifestHash = $actualHash
        promptLoaded = $true
        agentPlan = $true
        cliResourceValidation = $true
    } | ConvertTo-Json -Depth 5
}
finally {
    if (Test-Path -LiteralPath $temp) { Remove-Item -LiteralPath $temp -Recurse -Force }
}
