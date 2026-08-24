param([switch]$SkipBuild)

$ErrorActionPreference = 'Stop'
$root = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$resourceRoot = Join-Path $root 'resources\vm\4.4.0'
$promptPath = Join-Path $resourceRoot 'agent-prompt-knowledge.json'
$corpusPath = Join-Path $resourceRoot 'agent-knowledge.json'
$manifestPath = Join-Path $resourceRoot 'manifest.json'

if (-not (Test-Path -LiteralPath $promptPath -PathType Leaf)) { throw 'Compact Agent prompt knowledge is missing.' }
if (-not (Test-Path -LiteralPath $corpusPath -PathType Leaf)) { throw 'On-demand Agent knowledge corpus is missing.' }
if (Test-Path -LiteralPath (Join-Path $resourceRoot 'script-tutor-knowledge.json')) { throw 'The old full skill runtime payload is still present.' }

$prompt = Get-Content -LiteralPath $promptPath -Raw -Encoding UTF8 | ConvertFrom-Json
$corpus = Get-Content -LiteralPath $corpusPath -Raw -Encoding UTF8 | ConvertFrom-Json
if ($prompt.source.skillName -ne 'vm-script-tutor' -or $prompt.source.skillVersion -ne '1.2') { throw 'The compact synchronized skill version is incorrect.' }
if ($prompt.source.rawSkillEmbedded -ne $false) { throw 'The raw SKILL.md must not be embedded in the Agent prompt resource.' }
if (@($prompt.reviewRules).Count -lt 6) { throw 'Compact Agent review rules are incomplete.' }
if (@($corpus.entries | Where-Object { $_.kind -eq 'faq' }).Count -lt 29) { throw 'FAQ entries were not synchronized.' }
if (@($corpus.entries | Where-Object { $_.kind -eq 'reference-solution' }).Count -lt 14) { throw 'Reference SOL scripts were not synchronized.' }
$promptText = Get-Content -LiteralPath $promptPath -Raw -Encoding UTF8
foreach ($forbidden in @('SKILL.md','sourceInventory','preGenerationWorkflow','artifactDelivery','uiReport')) {
    if ($promptText.Contains($forbidden)) { throw "Full skill/workflow field leaked into compact prompt: $forbidden" }
}

$manifest = Get-Content -LiteralPath $manifestPath -Raw -Encoding UTF8 | ConvertFrom-Json
foreach ($file in @($promptPath, $corpusPath)) {
    $relative = $file.Substring($resourceRoot.Length + 1).Replace('\','/')
    $actualHash = (Get-FileHash -LiteralPath $file -Algorithm SHA256).Hash.ToUpperInvariant()
    if ([string]$manifest.hashes.$relative -ne $actualHash) { throw "Agent knowledge SHA-256 does not match the resource manifest: $relative" }
}

$agentRoot = Join-Path $root 'agent'
$node = Join-Path $root '.runtime\node-v22.19.0-win-x64\node.exe'
if (-not (Test-Path -LiteralPath $node -PathType Leaf)) { $node = (Get-Command node -ErrorAction Stop).Source }
if (-not $SkipBuild) {
    & $node (Join-Path $agentRoot 'node_modules\typescript\bin\tsc') '-p' (Join-Path $agentRoot 'tsconfig.json')
    if ($LASTEXITCODE -ne 0) { throw 'Agent TypeScript build failed.' }
}

$promptCheck = @'
import { buildVmSystemPrompt } from './agent/dist/system-prompt.js';
import { searchProjectKnowledge } from './agent/dist/knowledge.js';
const config = { repositoryRoot: process.cwd(), agentRoot: process.cwd() + '/agent' };
const prompt = buildVmSystemPrompt(config);
for (const term of ["agent-prompt-knowledge.json", "vm_search_project_knowledge", "UserProperty.cs", "AlgorithmTab.xml", "C# 5.0", "errorStatus"]) {
  if (!prompt.includes(term)) throw new Error("Prompt is missing compact Agent knowledge: " + term);
}
for (const forbidden of ["SKILL.md", "sourceInventory", "preGenerationWorkflow", "script-tutor-knowledge.json"]) {
  if (prompt.includes(forbidden)) throw new Error("Full skill content leaked into prompt: " + forbidden);
}
const result = searchProjectKnowledge(process.cwd(), "ImageData Height", "faq", 3);
if (result.matches.length === 0) throw new Error("On-demand knowledge search returned no FAQ result.");
console.log(JSON.stringify({ ok: true, promptLength: prompt.length, faqMatches: result.matches.length }));
'@
Push-Location $root
try { $promptCheck | & $node --input-type=module - }
finally { Pop-Location }
if ($LASTEXITCODE -ne 0) { throw 'Agent prompt or on-demand knowledge search failed.' }

[pscustomobject]@{
    ok = $true
    skillVersion = $prompt.source.skillVersion
    faqEntries = @($corpus.entries | Where-Object { $_.kind -eq 'faq' }).Count
    referenceSolutionEntries = @($corpus.entries | Where-Object { $_.kind -eq 'reference-solution' }).Count
    tutorLookupEntries = @($corpus.entries | Where-Object { $_.kind -in @('tutor-reference','tutor-example') }).Count
    rawSkillInPrompt = $false
    promptLoaded = $true
    searchLoaded = $true
} | ConvertTo-Json -Compress
