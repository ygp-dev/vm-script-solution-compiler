param([switch] $SkipBuild)

$ErrorActionPreference = 'Stop'
$root = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
if (-not $SkipBuild) {
    & dotnet build (Join-Path $root 'VmScriptCompiler.sln') -c Release
    if ($LASTEXITCODE -ne 0) { throw 'dotnet build failed.' }
}

$cli = Join-Path $root 'src\VmScriptCompiler.Cli\bin\Release\net8.0\VmScriptCompiler.Cli.dll'
$agent = Join-Path $root 'src\VmScriptCompiler.Agent\bin\Release\net8.0\vm-script-agent.dll'
$fixtures = Join-Path $root 'tests\fixtures'
$temp = Join-Path ([IO.Path]::GetTempPath()) ('vm-script-compiler-full-test-' + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $temp | Out-Null

function Build-Fixture([string] $name, [string] $folder) {
    $value = & dotnet $cli build --spec (Join-Path $fixtures $name) --output (Join-Path $temp $folder) | ConvertFrom-Json
    if ($LASTEXITCODE -ne 0 -or -not $value.ok) { throw "Build failed for $name." }
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $archive = [IO.Compression.ZipFile]::OpenRead($value.solution)
    try { if (@($archive.Entries | Where-Object { $_.FullName.Contains('/') }).Count -gt 0) { throw "$name contains VM-incompatible forward-slash ZIP entries." } }
    finally { $archive.Dispose() }
    return $value
}

function Assert-Blocked([string] $fixture, [string] $code) {
    $previousPreference = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'
    $text = (& dotnet $cli build --spec (Join-Path $fixtures $fixture) --output (Join-Path $temp ('blocked-' + [guid]::NewGuid().ToString('N'))) 2>&1) -join "`n"
    $exitCode = $LASTEXITCODE
    $ErrorActionPreference = $previousPreference
    if ($exitCode -eq 0 -or $text -notmatch [regex]::Escape($code)) { throw "$fixture was not blocked with $code. Output: $text" }
}

try {
    $environment = & dotnet $cli env | ConvertFrom-Json
    if (-not $environment.Found -or $environment.Version -ne '4.4.0') { throw 'E01 environment detection failed.' }

    $global = Build-Fixture 'm2-global-create.json' 'global'
    $shell = Build-Fixture 'm3-shell-create.json' 'shell'
    $python = Build-Fixture 'm4-python-create.json' 'python'
    $defaults = Build-Fixture 'runtime-direct-sol-proof.json' 'input-defaults'
    $mixed = Build-Fixture 'm3-two-shell-create.json' 'two-shell'
    $connected = Build-Fixture 'm7-explicit-connection-create.json' 'explicit-connection'
    $allTypes = Build-Fixture 'm7-vm-types-create.json' 'vm-types'
    $generatedOps = Build-Fixture 'm7-generated-operations-create.json' 'generated-operations'

    $shellParse = Get-Content -Raw (Join-Path $shell.TaskDirectory 'validation\parse-result.json') | ConvertFrom-Json
    $shellModule = @($shellParse.solution.procedures.modules)[0]
    $shellContract = Get-Content -Raw (Join-Path $shell.TaskDirectory 'script-contract.json') | ConvertFrom-Json
    $shellReport = Get-Content -Raw (Join-Path $shell.TaskDirectory 'build-report.md')
    if ($shellContract.scripts[0].shellReferences.mode -ne 'vm-implicit-defaults' -or -not $shellReport.Contains('ShellRefrences payload: `not emitted; VM default references remain implicit`')) { throw 'Implicit ShellModule DLL reference status is not traceable.' }
    $shellLengths = @{}; $shellModule.binaryParams | ForEach-Object { $shellLengths[$_.name] = $_.valueLen }
    if ($shellModule.name -ne 'ShellModule' -or $shellLengths.Input -ne 504 -or $shellLengths.Output -ne 963 -or $shellLengths.DynamicInData -ne 785 -or $shellLengths.DynamicOutData -ne 1761) { throw 'ShellModule VM 4.4 DynamicIO mapping does not match the runtime-saved sample.' }

    $pythonParse = Get-Content -Raw (Join-Path $python.TaskDirectory 'validation\parse-result.json') | ConvertFrom-Json
    $pythonModule = @($pythonParse.solution.procedures.modules | Where-Object name -eq 'PyShellModule')[0]
    $pythonSource = @($pythonModule.binaryParams | Where-Object name -eq 'ShellContent')[0].parsed
    $pythonLengths = @{}; $pythonModule.binaryParams | ForEach-Object { $pythonLengths[$_.name] = $_.valueLen }
    if ($pythonSource -notmatch 'if a is None' -or $pythonSource -notmatch 'moduleVar.Sum' -or $pythonLengths.Input -ne 499 -or $pythonLengths.Output -ne 739 -or $pythonLengths.DynamicInData -ne 781 -or $pythonLengths.DynamicOutData -ne 1759) { throw 'PyShellModule VM 4.4 DynamicIO mapping or null-input fallback does not match the expected contract.' }

    $defaultsParse = Get-Content -Raw (Join-Path $defaults.TaskDirectory 'validation\parse-result.json') | ConvertFrom-Json
    $defaultRelations = @($defaultsParse.solution.procedures.modules.subscriptions.relationString)
    foreach ($expected in @(
        '0 . %SolGeneratedPythonA% . 0 . 101 . 1 . 0 . All . 1',
        '0 . %SolGeneratedPythonB% . 0 . 202 . 1 . 0 . All . 1',
        '1 . %SolGeneratedCSharpA% . 0 . 1.25 . 1 . 0 . All . 1',
        '1 . %SolGeneratedCSharpB% . 0 . 2.5 . 1 . 0 . All . 1')) {
        if ($defaultRelations -notcontains $expected) { throw "Input default was not written to ModuleSubscribe: $expected" }
    }

    Assert-Blocked 'e05-python-third-party.json' 'PYTHON_DEPENDENCY_MISSING'
    Assert-Blocked 'e07-create-external-module.json' 'EXTERNAL_MODULE_NOT_AVAILABLE'
    Assert-Blocked 'e08-io-type-mismatch.json' 'IO_TYPE_MISMATCH'

    $mixedParse = Get-Content -Raw (Join-Path $mixed.TaskDirectory 'validation\parse-result.json') | ConvertFrom-Json
    $modules = @($mixedParse.solution.procedures.modules)
    if ($modules.Count -ne 2 -or @($modules[0].connections.followModules).Count -ne 0 -or @($modules[1].connections.frontModules).Count -ne 0) { throw 'Independent script modules were unexpectedly connected.' }

    $connectedParse = Get-Content -Raw (Join-Path $connected.TaskDirectory 'validation\parse-result.json') | ConvertFrom-Json
    $connectedModules = @($connectedParse.solution.procedures.modules)
    $connectedA = @($connectedModules | Where-Object displayName -eq '独立脚本A')[0]
    $connectedB = @($connectedModules | Where-Object displayName -eq '独立脚本B')[0]
    if (@($connectedA.connections.followModules).Count -ne 1 -or @($connectedB.connections.frontModules).Count -ne 1) { throw 'Explicit script connection was not generated.' }

    $typeChanges = Get-Content -Raw (Join-Path $allTypes.TaskDirectory 'validation\module-changes.json') | ConvertFrom-Json
    $typeValues = ($typeChanges.changes.value -join "`n")
    foreach ($vmType in @('int[]','float[]','string[]','byte','IMAGE','ROIBOX','ROIBOX[]','ROIANNULUS','ROIPOLYGON','POINT','LINE','FIXTURE','Rect','ELLIPSE','pointset')) {
        if (-not $typeValues.Contains('>' + $vmType + '<')) { throw "VM native IO type was not emitted: $vmType" }
    }

    $generatedCSharp = Get-Content -Raw (Join-Path $generatedOps.TaskDirectory 'generated\generated-csharp.cs')
    $generatedPython = Get-Content -Raw (Join-Path $generatedOps.TaskDirectory 'generated\generated-python.py')
    $generatedGlobal = Get-Content -Raw (Join-Path $generatedOps.TaskDirectory 'generated\GlobalScript.cs')
    if ($generatedCSharp -notmatch 'Sum\s*=\s*\(A\s*\+\s*B\)' -or $generatedCSharp -notmatch 'GetVarPoint' -or $generatedCSharp -notmatch 'SetVarInt') { throw 'Typed C# operation generator failed.' }
    if ($generatedPython -notmatch "moduleVar\.Echo\s*=\s*\(moduleVar\.Text if moduleVar\.Text is not None else 'VM'\)") { throw 'Python operation generator or Requirement default fallback failed.' }
    if ($generatedGlobal -notmatch 'InitSDK\(\)' -or $generatedGlobal -notmatch 'ExecuteProcessOnce\("流程1"\)') { throw 'GlobalScript operation generator failed.' }

    $repeat = Build-Fixture 'm3-shell-create.json' 'shell-repeat'
    $repeatParse = Get-Content -Raw (Join-Path $repeat.TaskDirectory 'validation\parse-result.json') | ConvertFrom-Json
    $firstShape = $shellParse.solution | ConvertTo-Json -Depth 100 -Compress
    $secondShape = $repeatParse.solution | ConvertTo-Json -Depth 100 -Compress
    if ($firstShape -ne $secondShape) { throw 'E09 repeated builds are not structurally equivalent.' }

    $base = Join-Path $temp 'business.sol'
    & (Join-Path $root 'scripts\materialize_script_base.ps1') -OutputFile $base | Out-Null
    $baseHash = (Get-FileHash -LiteralPath $base -Algorithm SHA256).Hash
    $patch = & dotnet $cli patch --base $base --spec (Join-Path $fixtures 'm3-shell-patch.json') --output (Join-Path $temp 'patch') | ConvertFrom-Json
    if (-not $patch.ok -or (Get-FileHash -LiteralPath $base -Algorithm SHA256).Hash -ne $baseHash) { throw 'E10 Patch input protection failed.' }
    $patchParse = Get-Content -Raw (Join-Path $patch.TaskDirectory 'validation\parse-result.json') | ConvertFrom-Json
    if (@($patchParse.solution.procedures.modules).Count -ne 3) { throw 'Patch did not preserve base modules and add the script.' }

    $agentPlan = & dotnet $agent plan --prompt '生成一个 Python 脚本，在流程1中输入整数 A 和 B，输出 Sum=A+B' | ConvertFrom-Json
    if ($agentPlan.scripts[0].carrier -ne 'python-module' -or $agentPlan.scripts[0].outputs[0].name -ne 'Sum') { throw 'M5 Agent did not produce the expected Requirement IR.' }
    $agentBuild = & dotnet $agent build --prompt '生成一个 Python 脚本，在流程1中输入整数 A 和 B，输出 Sum=A+B' --output (Join-Path $temp 'agent') | ConvertFrom-Json
    if (-not $agentBuild.ok -or -not (Test-Path -LiteralPath $agentBuild.SolutionFile)) { throw 'M5 Agent build failed.' }
    $agentSource = Get-Content -Raw (Get-ChildItem -LiteralPath (Join-Path $agentBuild.TaskDirectory 'generated') -Filter '*.py' | Select-Object -First 1).FullName
    if ($agentSource -notmatch 'moduleVar\.Sum\s*=.*moduleVar\.A if moduleVar\.A is not None else 0.*moduleVar\.B if moduleVar\.B is not None else 0') { throw 'M5 Agent Python arithmetic/default fallback was not generated from typed expression IR.' }
    $agentCSharp = & dotnet $agent build --prompt '生成一个 C# 脚本，在流程1中输入整数 A 和 B，输出 Sum=A+B' --output (Join-Path $temp 'agent-csharp') | ConvertFrom-Json
    if (-not $agentCSharp.ok) { throw 'M5 Agent C# build failed.' }
    $agentCSharpSource = Get-Content -Raw (Get-ChildItem -LiteralPath (Join-Path $agentCSharp.TaskDirectory 'generated') -Filter '*.cs' | Select-Object -First 1).FullName
    if ($agentCSharpSource -notmatch 'Sum\s*=\s*\(?\s*A\s*\+\s*B\s*\)?' -or $agentCSharpSource -match '\bdynamic\b|\bInput\.|\bOutput\.') { throw 'M5 Agent C# did not use VM generated partial IO properties.' }

    [PSCustomObject]@{
        ok = $true; environment = $true; globalCreate = $true; shellDynamicIo = $true; pythonDynamicIo = $true; inputDefaultStructure = $true;
        independentModules = $true; explicitConnections = $true; vmNativeTypes = $true; typedGenerators = $true; implicitShellReferences = $true; patchPreservedInput = $true; deterministicStructure = $true;
        guardedPythonDependency = $true; guardedExternalModule = $true; guardedIoMismatch = $true; agent = $true; agentCSharp = $true
    } | ConvertTo-Json
}
finally {
    if (Test-Path -LiteralPath $temp) { Remove-Item -LiteralPath $temp -Recurse -Force }
}
