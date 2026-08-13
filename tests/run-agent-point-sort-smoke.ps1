param([switch]$SkipBuild)
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$prompt = [Text.Encoding]::UTF8.GetString(
    [Convert]::FromBase64String('5YaZ5LiA5LiqcHl0aG9u6ISa5pysIOeCuembhuaOkuW6j+eahA=='))
$result = & (Join-Path $PSScriptRoot 'run-agent-domain-smoke.ps1') `
    -SkipBuild:$SkipBuild `
    -FixtureFile (Join-Path $root 'tests\fixtures\agent-python-point-sort.json') `
    -PromptText $prompt `
    -ExpectedCarrier 'python-module' `
    -ExpectedSourceFragments @('sorted(range(count)', 'moduleVar.OutX', 'moduleVar.OutY', 'moduleVar.OriginalIndex') |
    ConvertFrom-Json

if (-not $result.ok -or $result.phase -ne 'offline-validated') {
    throw 'Python point-sort Agent workflow did not reach offline-validated.'
}
if ($result.carrier -ne 'python-module' -or -not $result.generatedSourceVerified) {
    throw 'Python point-sort Agent generated the wrong carrier.'
}
[pscustomobject]@{
    ok = $true
    phase = $result.phase
    carrier = $result.carrier
    solution = $result.solution
    generatedSourceVerified = $result.generatedSourceVerified
    sessionBytes = $result.sessionBytes
    tools = $result.tools
} | ConvertTo-Json -Depth 5 -Compress

exit 0
