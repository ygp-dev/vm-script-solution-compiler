param([switch]$SkipBuild)
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
if (-not $SkipBuild) {
    & dotnet build (Join-Path $root 'VmScriptCompiler.sln') -c Release
    if ($LASTEXITCODE -ne 0) { throw 'dotnet build failed.' }
}

$worker = Join-Path $root 'src\VmScriptCompiler.DomainWorker\bin\Release\net8.0\vm-script-domain-worker.dll'
$fixture = Get-Content -LiteralPath (Join-Path $root 'tests\fixtures\m3-shell-create.json') -Raw -Encoding UTF8 | ConvertFrom-Json
$temp = Join-Path ([IO.Path]::GetTempPath()) ('vm-script-domain-worker-' + [guid]::NewGuid().ToString('N'))

try {
    $requests = @(
        @{ id='init'; command='initialize'; arguments=@{} },
        @{ id='env'; command='detect_environment'; arguments=@{} },
        @{ id='cap'; command='query_capability'; arguments=@{ query='circle'; vmVersion='4.4.0' } },
        @{ id='plan'; command='plan_solution'; arguments=@{ requirement=$fixture } },
        @{ id='build'; command='build_solution'; arguments=@{ requirement=$fixture; output=$temp } },
        @{ id='shutdown'; command='shutdown'; arguments=@{} }
    )
    $lines = $requests | ForEach-Object { $_ | ConvertTo-Json -Depth 30 -Compress }
    $responses = @($lines | & dotnet $worker --repository-root $root --output-root $temp | ForEach-Object { $_ | ConvertFrom-Json })
    if ($LASTEXITCODE -ne 0) { throw 'Domain Worker exited with an error.' }
    if ($responses.Count -ne $requests.Count) { throw "Expected $($requests.Count) responses, got $($responses.Count)." }
    if ($responses.Where({ -not $_.ok }).Count -ne 0) { throw 'Domain Worker returned a failed response.' }
    if ($responses[0].result.protocolVersion -ne '1.0') { throw 'Domain Worker protocol version mismatch.' }
    if (-not $responses[2].result.matches) { throw 'Capability query returned no evidence.' }
    if (-not $responses[3].result.ok) { throw 'Requirement plan failed.' }
    $solution = $responses[4].result.solutionFile
    if (-not (Test-Path -LiteralPath $solution)) { throw 'Domain Worker build did not create result.sol.' }
    [pscustomobject]@{ ok=$true; responses=$responses.Count; solution=$solution } | ConvertTo-Json -Compress
}
finally {
    if (Test-Path -LiteralPath $temp) { Remove-Item -LiteralPath $temp -Recurse -Force }
}
