param([switch]$SkipBuild)

$ErrorActionPreference = 'Stop'
& (Join-Path $PSScriptRoot 'run-agent-knowledge-smoke.ps1') -SkipBuild:$SkipBuild
exit $LASTEXITCODE
