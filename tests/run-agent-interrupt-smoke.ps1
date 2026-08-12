param([switch]$SkipBuild)
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$node = Join-Path $root '.runtime\node-v22.19.0-win-x64\node.exe'
$agent = Join-Path $root 'agent'
if (-not (Test-Path -LiteralPath $node -PathType Leaf)) { throw 'Pinned Node runtime is missing.' }
if (-not $SkipBuild) {
    & npm run build --prefix $agent
    if ($LASTEXITCODE -ne 0) { throw 'Pi Agent build failed.' }
}
$script = Join-Path $root 'tests\agent-interrupt-smoke.mjs'
$output = & $node $script $root 2>&1
if ($LASTEXITCODE -ne 0) { $output | Write-Error; throw 'Agent interrupt/continue smoke failed.' }
$output
