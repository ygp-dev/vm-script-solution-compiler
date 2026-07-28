param([switch]$SkipBuild)
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$cli = Join-Path $root 'src\VmScriptCompiler.Cli\bin\Release\net8.0\VmScriptCompiler.Cli.dll'
$fixture = Join-Path $root 'tests\fixtures\m10-output-only-csharp.json'
$temp = Join-Path ([IO.Path]::GetTempPath()) ('vm-script-m10-' + [guid]::NewGuid().ToString('N'))
try {
    if (-not $SkipBuild) { & dotnet build (Join-Path $root 'VmScriptCompiler.sln') -c Release --no-restore | Out-Host }
    New-Item -ItemType Directory -Path $temp | Out-Null
    $built = & dotnet $cli build --spec $fixture --output $temp | ConvertFrom-Json
    if (-not $built.ok) { throw 'Output-only C# build failed.' }
    $parse = Get-Content -Raw (Join-Path $built.taskDirectory 'validation\parse-result.json') -Encoding UTF8 | ConvertFrom-Json
    $module = @($parse.solution.procedures.modules)[0]
    $input = @($module.binaryParams | Where-Object name -eq 'Input')[0].parsed
    $dynamic = @($module.binaryParams | Where-Object name -eq 'DynamicInData')[0].parsed
    $source = Get-Content -Raw (Join-Path $built.taskDirectory 'generated\output-only.cs') -Encoding UTF8
    if (-not $input.Contains('<Name>%__CompilerPortAnchor%</Name>') -or -not $input.Contains('<IsShow>false</IsShow>')) { throw 'Hidden VM property-generation anchor is missing.' }
    if (-not $dynamic.Contains('__CompilerPortAnchor') -or -not $dynamic.Contains('Visible=False')) { throw 'Property anchor is visible in DynamicIO.' }
    if (-not $source.Contains('Result = 42;')) { throw 'Output-only operation was not generated.' }
    $precompile = Get-Content -Raw (Join-Path $built.taskDirectory 'validation\script-precompile.json') -Encoding UTF8 | ConvertFrom-Json
    if (-not $precompile.ok -or @($precompile.scripts | Where-Object exitCode -ne 0).Count -ne 0) { throw 'Output-only script did not precompile.' }
    $warningCount = if ($null -eq $parse.solution.warnings) { 0 } else { @($parse.solution.warnings).Count }
    if ($warningCount -ne 0) { throw 'Output-only result introduced parser warnings.' }
    $pythonGuard = & dotnet $cli plan --spec (Join-Path $root 'tests\fixtures\e13-python-complex-port.json') | ConvertFrom-Json
    if ($pythonGuard.ok -or @($pythonGuard.issues | Where-Object code -eq 'PYTHON_COMPLEX_TYPE_UNCONFIRMED').Count -ne 2) { throw 'Unconfirmed Python complex ports were not blocked.' }
    [pscustomobject]@{ ok=$true; outputOnlyCSharp=$true; hiddenPropertyAnchor=$true; dynamicIoHidden=$true; offlinePrecompile=$true; parseWarnings=$warningCount; guardedPythonComplexPorts=$true } | ConvertTo-Json
}
finally { if (Test-Path $temp) { Remove-Item $temp -Recurse -Force } }
