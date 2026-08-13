param([switch]$SkipBuild)

$ErrorActionPreference = 'Stop'
$root = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$resourceRoot = Join-Path $root 'resources\vm\4.4.0'
$knowledgePath = Join-Path $resourceRoot 'script-tutor-knowledge.json'
$manifestPath = Join-Path $resourceRoot 'manifest.json'
$temp = Join-Path ([IO.Path]::GetTempPath()) ('vm-script-tutor-knowledge-' + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $temp | Out-Null

try {
    if (-not (Test-Path -LiteralPath $knowledgePath -PathType Leaf)) { throw 'Structured vm-script-tutor knowledge is missing.' }
    $knowledge = Get-Content -LiteralPath $knowledgePath -Raw -Encoding UTF8 | ConvertFrom-Json
    if ($knowledge.scope.skillName -ne 'vm-script-tutor' -or $knowledge.scope.skillVersion -ne '1.2') { throw 'The synchronized skill version is incorrect.' }
    if (@($knowledge.staticReviewChecklist).Count -lt 10) { throw 'The synchronized static review checklist is incomplete.' }
    if (@($knowledge.hardBoundaries | Where-Object { $_ -match 'UserProperty\.cs' }).Count -eq 0) { throw 'UserProperty.cs read-only rule is missing.' }
    if (-not @($knowledge.runtimeAndLanguage.blockedCSharpFeatures) -contains 'out var') { throw 'C# 5 compatibility rule is missing.' }
    if ($knowledge.moduleParameterWorkflow.lookup.algorithmTabPath -notmatch 'AlgorithmTab\.xml') { throw 'AlgorithmTab lookup workflow is missing.' }
    if ([string]::IsNullOrWhiteSpace([string]$knowledge.errorAndDebugPolicy.debugDefault)) { throw 'Default debug-output policy is missing.' }

    $manifest = Get-Content -LiteralPath $manifestPath -Raw -Encoding UTF8 | ConvertFrom-Json
    $actualHash = (Get-FileHash -LiteralPath $knowledgePath -Algorithm SHA256).Hash.ToUpperInvariant()
    if ([string]$manifest.hashes.'script-tutor-knowledge.json' -ne $actualHash) { throw 'Structured skill knowledge SHA-256 does not match the resource manifest.' }

    $agentRoot = Join-Path $root 'agent'
    $node = Join-Path $root '.runtime\node-v22.19.0-win-x64\node.exe'
    if (-not (Test-Path -LiteralPath $node -PathType Leaf)) { $node = (Get-Command node -ErrorAction Stop).Source }
    if (-not $SkipBuild) {
        if (-not (Test-Path -LiteralPath $node -PathType Leaf)) { throw "Bundled Node runtime is missing: $node" }
        & $node (Join-Path $agentRoot 'node_modules\typescript\bin\tsc') '-p' (Join-Path $agentRoot 'tsconfig.json')
        if ($LASTEXITCODE -ne 0) { throw 'Agent TypeScript build failed.' }
    }

    $promptCheck = @'
import { buildVmSystemPrompt } from './agent/dist/system-prompt.js';
const prompt = buildVmSystemPrompt({ repositoryRoot: process.cwd(), agentRoot: process.cwd() + '/agent' });
for (const term of ["vm-script-tutor", "UserProperty.cs", "AlgorithmTab.xml", "C# 5.0", "errorStatus"]) {
  if (!prompt.includes(term)) throw new Error("Prompt is missing synchronized skill knowledge: " + term);
}
console.log(JSON.stringify({ ok: true, promptLength: prompt.length }));
'@
    Push-Location $root
    try { $promptCheck | & $node --input-type=module - }
    finally { Pop-Location }
    if ($LASTEXITCODE -ne 0) { throw 'Agent system prompt did not load vm-script-tutor knowledge.' }

    [pscustomobject]@{
        ok = $true
        skillVersion = $knowledge.scope.skillVersion
        checklistCount = @($knowledge.staticReviewChecklist).Count
        manifestHash = $actualHash
        promptLoaded = $true
    } | ConvertTo-Json -Compress
}
finally {
    if (Test-Path -LiteralPath $temp) { Remove-Item -LiteralPath $temp -Recurse -Force }
}
