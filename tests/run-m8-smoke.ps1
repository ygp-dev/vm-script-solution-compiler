param([switch]$SkipBuild)
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$cli = Join-Path $root 'src\VmScriptCompiler.Cli\bin\Release\net8.0\VmScriptCompiler.Cli.dll'
$fixtures = Join-Path $root 'tests\fixtures'
$temp = Join-Path ([IO.Path]::GetTempPath()) ('vm-script-m8-' + [guid]::NewGuid().ToString('N'))
try {
    if (-not $SkipBuild) { & dotnet build (Join-Path $root 'VmScriptCompiler.sln') -c Release --no-restore | Out-Host }
    New-Item -ItemType Directory -Path $temp | Out-Null

    $typed = & dotnet $cli build --spec (Join-Path $fixtures 'm8-typed-api-operations.json') --output (Join-Path $temp 'typed') | ConvertFrom-Json
    if (-not $typed.ok) { throw 'Typed API build failed.' }
    $cs = Get-Content (Join-Path $typed.taskDirectory 'generated\typed-api.cs') -Raw -Encoding UTF8
    foreach ($method in @('GetVarInt','GetVarFloat','GetVarString','GetVarByte','GetVarImage','GetVarBox','GetVarAnnulus','GetVarPolygon','GetVarPoint','GetVarLine','GetVarFixture','GetVarCircle','GetVarRect','GetVarEllipse','GetVarPointset','SetVarInt','SetVarFloat','SetVarString','SetVarByte','SetVarImage','SetVarBox','SetVarAnnulus','SetVarPolygon','SetVarPoint','SetVarLine','SetVarFixture','SetVarCircle','SetVarRect','SetVarEllipse','SetVarPointset')) {
        if (-not $cs.Contains($method + '(')) { throw "Typed VM method was not generated: $method" }
    }
    foreach ($fragment in @('EnabledEcho = ((Enabled != 0)) ? 1 : 0','int[] vIntValues','int vInt =','RoiboxData[] vBoxValues','RoiboxData vBox =','new RoiboxData[] { Box }','GetArrayValue("Floats")','GetParamValue("IntValue"','if ((IntValue > 0))','BytesToPointset(Pointset')) {
        if (-not $cs.Contains($fragment)) { throw "Typed C# semantic fragment missing: $fragment" }
    }
    $global = Get-Content (Join-Path $typed.taskDirectory 'generated\GlobalScript.cs') -Raw -Encoding UTF8
    foreach ($fragment in @('SetScriptContinuousExecuteInterval(50U)','StartGlobalCommunicate()','SendCommDeviceData(','SetInputInt("intX"','VmSolution.SaveAs(','VmSolution.Load(','ExecuteProcessOnce(','ContinuousExecuteProcess(','StopProcessExecute(')) {
        if (-not $global.Contains($fragment)) { throw "Global control API fragment missing: $fragment" }
    }
    $python = Get-Content (Join-Path $typed.taskDirectory 'generated\typed-python.py') -Raw -Encoding UTF8
    if (-not $python.Contains('moduleVar.Values if moduleVar.Values is not None else [1, 2, 3]')) { throw 'Python array default fallback was not generated.' }
    $precompile = Get-Content (Join-Path $typed.taskDirectory 'validation\script-precompile.json') -Raw -Encoding UTF8 | ConvertFrom-Json
    if (-not $precompile.ok -or @($precompile.scripts).Count -ne 3 -or @($precompile.scripts | Where-Object exitCode -ne 0).Count -ne 0) { throw 'Offline VM assembly/Python precompile did not validate all three carriers.' }
    $typedParse = Get-Content (Join-Path $typed.taskDirectory 'validation\parse-result.json') -Raw -Encoding UTF8 | ConvertFrom-Json
    $typedModule = @($typedParse.solution.procedures.modules | Where-Object name -eq 'ShellModule')[0]
    $changes = Get-Content (Join-Path $typed.taskDirectory 'validation\module-changes.json') -Raw -Encoding UTF8 | ConvertFrom-Json
    $inputPayload = @($changes.changes | Where-Object paramName -eq 'Input' | Where-Object value -like '*ROIBOX*')[0].value
    $outputPayload = @($changes.changes | Where-Object paramName -eq 'Output' | Where-Object value -like '*ROIBOX*')[0].value
    $layoutEvidence = Get-Content (Join-Path $root 'resources\vm\4.4.0\csharp-io-layout-evidence.json') -Raw -Encoding UTF8 | ConvertFrom-Json
    [xml]$inputXml = $inputPayload.TrimEnd([char]0)
    [xml]$outputXml = $outputPayload.TrimEnd([char]0)
    $actualInputTypes = @($inputXml.ArrayOfModuleParamItem.ModuleParamItem | ForEach-Object { [string]$_.ValueType })
    $actualOutputTypes = @($outputXml.ArrayOfModuleParamItem.ModuleParamItem | ForEach-Object { [string]$_.ValueType })
    $actualInputFields = @($inputXml.ArrayOfModuleParamItem.ModuleParamItem | ForEach-Object { [regex]::Matches([string]$_.StructName, '%[^%]+%').Count })
    $actualOutputFields = @($outputXml.ArrayOfModuleParamItem.ModuleParamItem | ForEach-Object { [regex]::Matches([string]$_.StructName, '%[^%]+%').Count })
    if (($actualInputTypes -join '|') -ne (@($layoutEvidence.input.valueTypes) -join '|') -or ($actualOutputTypes -join '|') -ne (@($layoutEvidence.output.valueTypes) -join '|')) { throw 'VM C# IO ValueType sequence differs from saved-layout evidence.' }
    if (($actualInputFields -join '|') -ne (@($layoutEvidence.input.structFieldCounts) -join '|') -or ($actualOutputFields -join '|') -ne (@($layoutEvidence.output.structFieldCounts) -join '|')) { throw 'VM C# IO StructName field sequence differs from saved-layout evidence.' }
    foreach ($vmType in @('int','int[]','float','float[]','string','string[]','byte','IMAGE','ROIBOX','ROIBOX[]','ROIANNULUS','ROIPOLYGON','POINT','LINE','FIXTURE','Rect','ELLIPSE','pointset')) {
        if (-not $inputPayload.Contains("<ValueType>$vmType</ValueType>") -or -not $outputPayload.Contains("<ValueType>$vmType</ValueType>")) { throw "VM port type was not emitted to Input and Output: $vmType" }
    }
    foreach ($fragment in @(("<StructName>%Image0%`r%ImageWidth0%"),("<StructName>%roicenterx0%`r%roicentery0%"))) {
        if (-not $inputPayload.Contains($fragment)) { throw "VM complex StructName backing fields were not emitted: $fragment" }
    }
    $dynamicPayload = (@($changes.changes | Where-Object paramName -in @('DynamicInData','DynamicOutData')).value -join "`n")
    foreach ($vmType in @('IMAGE','ROIBOX','ROIANNULUS','ROIPOLYGON','POINT','LINE','FIXTURE','Rect','ELLIPSE')) {
        if (-not $dynamicPayload.Contains('Style="' + $vmType + '"')) { throw "VM DynamicIO combination was not emitted: $vmType" }
    }
    foreach ($backing in @('%Image0%','%ImageWidth0%','%roicenterx0%','%DetectAnnulusCenterX0%','%BlindPolygonPointNum0%')) {
        if (-not $dynamicPayload.Contains('Name="' + $backing + '"')) { throw "VM DynamicIO backing filter was not emitted: $backing" }
    }
    if ($dynamicPayload.Contains('<Filter Name="%Image%"') -or $dynamicPayload.Contains('<Filter Name="%Box%"')) { throw 'Complex logical ports must be DynamicIO combinations, not direct filters.' }
    [xml]$dynamicInputXml = [string](@($changes.changes | Where-Object paramName -eq 'DynamicInData')[0].value).TrimEnd([char]0)
    [xml]$dynamicOutputXml = [string](@($changes.changes | Where-Object paramName -eq 'DynamicOutData')[0].value).TrimEnd([char]0)
    $actualInputStyles = @($dynamicInputXml.SelectNodes('//Combination') | ForEach-Object { $_.Style })
    $actualOutputStyles = @($dynamicOutputXml.SelectNodes('//Combination') | ForEach-Object { $_.Style })
    if (($actualInputStyles -join '|') -ne (@($layoutEvidence.input.dynamicCombinationPreorder) -join '|') -or ($actualOutputStyles -join '|') -ne (@($layoutEvidence.output.dynamicCombinationPreorder) -join '|')) { throw 'VM C# DynamicIO combination tree differs from saved-layout evidence.' }
    if (@($dynamicInputXml.SelectNodes('/ParamRoot/Categorys/Category/Items/*')).Count -ne $layoutEvidence.input.dynamicTopLevelCount -or @($dynamicOutputXml.SelectNodes('/ParamRoot/Categorys/Category/Items/*')).Count -ne $layoutEvidence.output.dynamicTopLevelCount) { throw 'VM C# DynamicIO top-level port count differs from saved-layout evidence.' }
    $circlePortGuard = & dotnet $cli plan --spec (Join-Path $fixtures 'e14-csharp-circle-port-unsupported.json') | ConvertFrom-Json
    if ($circlePortGuard.ok -or @($circlePortGuard.issues | Where-Object code -eq 'CSHARP_PORT_TYPE_UNSUPPORTED').Count -ne 1) { throw 'VM 4.4 unsupported Circle script port was not blocked.' }
    $assemblyGuid = @($typedModule.binaryParams | Where-Object name -eq 'AssemblyGuid')[0]
    if ($null -eq $assemblyGuid -or $assemblyGuid.valueLen -ne $layoutEvidence.assemblyGuidLength) { throw 'C# AssemblyGuid was not materialized.' }
    $uiNames = @($typedModule.uiParams | ForEach-Object name)
    foreach ($uiName in @($layoutEvidence.requiredUiParameters)) {
        if ($uiName -notin $uiNames) { throw "C# UiParamData object mapping was not emitted: $uiName" }
    }
    foreach ($parameter in @('Input','Output')) {
        $readBack = @($typedModule.binaryParams | Where-Object name -eq $parameter)[0]
        if ($null -eq $readBack -or $readBack.valueLen -lt 1000) { throw "Parser did not read back the complete-sized $parameter binary parameter." }
    }

    $base = Join-Path $root ('docs\SOL\' + [char]0x57FA + [char]0x7840 + '.sol')
    $baseHash = (Get-FileHash $base -Algorithm SHA256).Hash
    $patched = & dotnet $cli patch --base $base --spec (Join-Path $fixtures 'm8-classic-clear-patch.json') --output (Join-Path $temp 'patch') | ConvertFrom-Json
    if (-not $patched.ok -or (Get-FileHash $base -Algorithm SHA256).Hash -ne $baseHash) { throw 'Version 4 Patch failed or modified the base SOL.' }
    $baseParse = Get-Content (Join-Path $patched.taskDirectory 'validation\base-structural-parse.json') -Raw -Encoding UTF8 | ConvertFrom-Json
    $resultParse = Get-Content (Join-Path $patched.taskDirectory 'validation\parse-result.json') -Raw -Encoding UTF8 | ConvertFrom-Json
    if (@($resultParse.solution.warnings).Count -ne @($baseParse.solution.warnings).Count) { throw 'Version 4 Patch introduced structural warnings.' }
    if (@($resultParse.solution.procedures.modules).Count -ne (@($baseParse.solution.procedures.modules).Count + 1)) { throw 'Version 4 Patch did not preserve base modules and add exactly one script.' }
    $clearSource = Get-Content (Join-Path $patched.taskDirectory 'generated\clear-calibration.cs') -Raw -Encoding UTF8
    if (-not $clearSource.Contains('if ((ClearTrigger == 1))') -or $clearSource -notmatch 'GetModule\(.+\)\.SetValue\("Clear"') { throw 'Classic Clear operation was not generated from structured IR.' }

    $previousPreference = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'
    $bad = & dotnet $cli patch --base $base --spec (Join-Path $fixtures 'e09-patch-module-parameter-missing.json') --output (Join-Path $temp 'bad') 2>&1 | Out-String
    $ErrorActionPreference = $previousPreference
    if ($LASTEXITCODE -eq 0 -or $bad -notmatch 'MODULE_PARAMETER_NOT_FOUND') { throw 'Unknown Patch module parameter was not blocked.' }
    $global:LASTEXITCODE = 0

    [pscustomobject]@{
        ok = $true; typedVmMethods = 30; scalarVariableSemantics = $true; structuredConditions = $true;
        pythonArrayDefaultFallback = $true; globalControlApis = $true; selectableVmPortTypes = 18; circleVariableApi = $true; offlinePrecompile = $true; version4Patch = $true;
        savedLayoutEvidence = $true; guardedCirclePort = $true; baseUnchanged = $true; warningDelta = 0; guardedModuleParameter = $true
    } | ConvertTo-Json
}
finally {
    if (Test-Path $temp) { Remove-Item $temp -Recurse -Force }
}
