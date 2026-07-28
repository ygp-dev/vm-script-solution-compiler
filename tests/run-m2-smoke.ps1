param([switch] $SkipBuild)

$ErrorActionPreference = 'Stop'
$root = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
if (-not $SkipBuild) {
    & dotnet build (Join-Path $root 'VmScriptCompiler.sln') -c Release
    if ($LASTEXITCODE -ne 0) { throw 'dotnet build failed.' }
}

$cli = Join-Path $root 'src\VmScriptCompiler.Cli\bin\Release\net8.0\VmScriptCompiler.Cli.dll'
$parser = Join-Path $root 'tools\vm-solution-parser\VMSolutionParser.Cli.exe'
$temp = Join-Path ([IO.Path]::GetTempPath()) ('vm-script-compiler-m2-test-' + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $temp | Out-Null

try {
    $invalidPlan = & dotnet $cli plan --spec (Join-Path $root 'tests\fixtures\invalid-requirement.json') | ConvertFrom-Json
    if ($invalidPlan.ok -or @($invalidPlan.issues).Count -lt 3) { throw 'Invalid Requirement was not rejected with detailed issues.' }

    $createOutput = Join-Path $temp 'create'
    $createJson = & dotnet $cli build --spec (Join-Path $root 'tests\fixtures\m2-global-create.json') --output $createOutput | ConvertFrom-Json
    if (-not $createJson.ok) { throw 'M2 Create returned failure.' }
    foreach ($relative in @('result.sol', 'requirement.json', 'build-plan.json', 'script-contract.json', 'generated\GlobalScript.cs', 'validation\parse-result.json', 'validation\inspect-result.json', 'build-report.md')) {
        if (-not (Test-Path -LiteralPath (Join-Path $createJson.taskDirectory $relative))) { throw "Missing Create artifact: $relative" }
    }
    $createParse = Get-Content -Raw (Join-Path $createJson.taskDirectory 'validation\parse-result.json') | ConvertFrom-Json
    $createModules = @($createParse.solution.procedures | ForEach-Object { @($_.modules) }).Count
    if ($createModules -ne 0) { throw "GlobalScript Create contains $createModules flow modules." }
    if ($null -eq $createParse.solution.globalScript -or $createParse.solution.globalScript.scriptLength -le 0) { throw 'GlobalScript payload was not parsed.' }
    if ($null -ne $createParse.solution.warnings -and @($createParse.solution.warnings).Count -gt 0) { throw 'Create parse returned warnings.' }
    $contract = Get-Content -Raw (Join-Path $createJson.taskDirectory 'script-contract.json') | ConvertFrom-Json
    $report = Get-Content -Raw (Join-Path $createJson.taskDirectory 'build-report.md')
    if (-not $contract.runtimeValidated -or -not $contract.runtimeValidation.baselineValidated -or @($contract.runtimeValidation.pending).Count -ne 0 -or $report -notmatch 'runtime baseline validation.*passed by user verification' -or $report -notmatch 'runtime validation pending: `none`') { throw 'Completed manifest runtime validation status was not propagated to build artifacts.' }
    $globalPrecompile = Get-Content -Raw (Join-Path $createJson.taskDirectory 'validation\script-precompile.json') | ConvertFrom-Json
    $globalEvidence = @($globalPrecompile.scripts | Where-Object carrier -eq 'global-csharp')[0]
    foreach ($reference in @('Apps.Json.dll','VMControls.RenderInterface.dll','ImageSourceModuleCs.dll','IMVSFastFeatureMatchModuCs.dll')) {
        if ($reference -notin @($globalEvidence.dependencies)) { throw "GlobalScript baseline DLL was not included in precompile validation: $reference" }
    }

    $base = Join-Path $temp 'business.sol'
    & (Join-Path $root 'scripts\materialize_script_base.ps1') -OutputFile $base | Out-Null
    $baseHash = (Get-FileHash -LiteralPath $base -Algorithm SHA256).Hash
    $patchOutput = Join-Path $temp 'patch'
    $patchJson = & dotnet $cli patch --base $base --spec (Join-Path $root 'tests\fixtures\m2-global-patch.json') --output $patchOutput | ConvertFrom-Json
    if (-not $patchJson.ok) { throw 'M2 Patch returned failure.' }
    if ((Get-FileHash -LiteralPath $base -Algorithm SHA256).Hash -ne $baseHash) { throw 'Patch modified its input SOL.' }
    $patchParseFile = Join-Path $patchJson.taskDirectory 'validation\parse-result.json'
    $patchParse = Get-Content -Raw $patchParseFile | ConvertFrom-Json
    $patchModules = @($patchParse.solution.procedures | ForEach-Object { @($_.modules) }).Count
    if ($patchModules -ne 2) { throw "Patch did not preserve the two base modules; found $patchModules." }
    if ($null -ne $patchParse.solution.warnings -and @($patchParse.solution.warnings).Count -gt 0) { throw 'Patch parse returned warnings.' }

    [PSCustomObject]@{ ok = $true; invalidRequirementIssues = @($invalidPlan.issues).Count; createModules = $createModules; patchModules = $patchModules; baseUnchanged = $true; parseWarnings = 0; runtimeValidated = $true } | ConvertTo-Json
}
finally {
    if (Test-Path -LiteralPath $temp) { Remove-Item -LiteralPath $temp -Recurse -Force }
}
