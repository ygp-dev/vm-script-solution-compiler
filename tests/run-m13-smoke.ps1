param([switch]$SkipBuild)
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$cli = Join-Path $root 'src\VmScriptCompiler.Cli\bin\Release\net8.0\VmScriptCompiler.Cli.dll'
$fixture = Join-Path $root 'tests\fixtures\m13-dxf-render.json'
$dxf = Get-ChildItem (Join-Path $root 'docs\SOL') -Recurse -Filter '*.dxf' | Select-Object -First 1 -ExpandProperty FullName
$vmShell = 'C:\Program Files\VisionMaster4.4.0\Applications\Module(sp)\x64\Logic\ShellModule'
$scriptMethods = Join-Path $vmShell 'Script.Methods.dll'
$netDxf = Join-Path $vmShell 'DLL\netDxf.dll'
$frameworkCsc = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$temp = Join-Path ([IO.Path]::GetTempPath()) ('vm-script-m13-' + [guid]::NewGuid().ToString('N'))
try {
    if (-not $SkipBuild) { & dotnet build (Join-Path $root 'VmScriptCompiler.sln') -c Release --no-restore | Out-Host }
    foreach ($file in @($dxf, $scriptMethods, $netDxf, $frameworkCsc)) { if (-not (Test-Path -LiteralPath $file)) { throw "Required M13 fixture/runtime file is missing: $file" } }
    New-Item -ItemType Directory -Path $temp | Out-Null
    $built = & dotnet $cli build --spec $fixture --output (Join-Path $temp 'build') | ConvertFrom-Json
    if (-not $built.ok) { throw 'Deterministic DXF build failed.' }
    $precompile = Get-Content -Raw (Join-Path $built.taskDirectory 'validation\script-precompile.json') -Encoding UTF8 | ConvertFrom-Json
    if (-not $precompile.ok -or @($precompile.scripts | Where-Object exitCode -ne 0).Count -ne 0) { throw 'Generated DXF source did not precompile with VM Framework64 references.' }
    $source = Join-Path $built.taskDirectory 'generated\dxf-render.cs'
    $sourceText = Get-Content -Raw $source -Encoding UTF8
    foreach ($entity in @('Polyline2D','Polyline3D','Spline','Ellipse','Insert','Circle','Arc','Line')) {
        if (-not $sourceText.Contains('name == "' + $entity + '"')) { throw "Generated renderer is missing $entity support." }
    }
    if (-not $sourceText.Contains('DXF_NO_DRAWABLE_ENTITIES') -or -not $sourceText.Contains('DXF_RENDER_WHITE_IMAGE')) { throw 'No-entity/white-image guards are missing.' }
    if ([regex]::Matches($sourceText, 'new byte\[checked\(rowBytes \* height\)\]').Count -ne 1 -or $sourceText.Contains('new System.Drawing.Bitmap(bitmap')) { throw 'Renderer reintroduced a duplicate full-image buffer.' }

    $properties = Join-Path $temp 'UserScript.Properties.cs'
    @'
public partial class UserScript
{
    public string DxfPath { get; set; }
    public int PreviewWidth { get; set; }
    public int PreviewHeight { get; set; }
    public Script.Methods.ImageData DxfImage { get; set; }
    public int Success { get; set; }
    public string ErrorMessage { get; set; }
    public int EntityCount { get; set; }
    public int RenderedEntityCount { get; set; }
}
'@ | Set-Content -LiteralPath $properties -Encoding UTF8
    $assembly = Join-Path $temp 'DxfRenderer.dll'
    & $frameworkCsc /nologo /target:library "/out:$assembly" "/reference:$scriptMethods" "/reference:$netDxf" /reference:System.Drawing.dll $source $properties | Out-Host
    if ($LASTEXITCODE -ne 0) { throw 'Runtime renderer harness compilation failed.' }
    $fixtureSource = Join-Path $temp 'DxfFixture.cs'
    @'
using netDxf;
using netDxf.Blocks;
using netDxf.Entities;
public static class DxfFixture
{
    public static void Main(string[] args)
    {
        var document = new DxfDocument();
        document.Entities.Add(new Ellipse(new Vector2(0, 0), 40, 20));
        var block = new Block("InsertedBlock", new EntityObject[] { new Line(new Vector2(0, 0), new Vector2(20, 10)) });
        document.Entities.Add(new Insert(block, new Vector2(60, 20)));
        document.Save(args[0]);
        new DxfDocument().Save(args[1]);
    }
}
'@ | Set-Content -LiteralPath $fixtureSource -Encoding UTF8
    $fixtureExe = Join-Path $temp 'DxfFixture.exe'
    & $frameworkCsc /nologo /target:exe "/out:$fixtureExe" "/reference:$netDxf" $fixtureSource | Out-Host
    if ($LASTEXITCODE -ne 0) { throw 'Synthetic Ellipse/Insert DXF fixture compilation failed.' }
    Copy-Item -LiteralPath $netDxf -Destination (Join-Path $temp 'netDxf.dll')
    $syntheticDxf = Join-Path $temp 'ellipse-insert.dxf'
    $emptyDxf = Join-Path $temp 'empty.dxf'
    & $fixtureExe $syntheticDxf $emptyDxf
    if ($LASTEXITCODE -ne 0 -or -not (Test-Path -LiteralPath $syntheticDxf) -or -not (Test-Path -LiteralPath $emptyDxf)) { throw 'Synthetic DXF fixture generation failed.' }

    $runtime = Join-Path $temp 'runtime.ps1'
    @'
param($Assembly, $ScriptMethods, $NetDxf, $Dxf, $EmptyDxf, $SyntheticDxf)
$ErrorActionPreference = 'Stop'
[Reflection.Assembly]::LoadFrom($ScriptMethods) | Out-Null
[Reflection.Assembly]::LoadFrom($NetDxf) | Out-Null
$loaded = [Reflection.Assembly]::LoadFrom($Assembly)
$type = $loaded.GetType('VmDxfRenderer', $true)
$method = $type.GetMethod('TryRender', [Reflection.BindingFlags]'Static,Public')
$arguments = [object[]]@($Dxf, 1920, 1080, $null, $null, 0, 0)
$ok = [bool]$method.Invoke($null, $arguments)
$image = $arguments[3]
$buffer = if ($null -eq $image) { $null } else { $image.Buffer }
$nonWhite = 0
if ($null -ne $buffer) {
    for ($i = 0; $i -lt $buffer.Length; $i += 3) {
        if ($buffer[$i] -ne 255 -or $buffer[$i + 1] -ne 255 -or $buffer[$i + 2] -ne 255) { $nonWhite++ }
    }
}
$emptyArguments = [object[]]@($EmptyDxf, 1920, 1080, $null, $null, 0, 0)
$emptyOk = [bool]$method.Invoke($null, $emptyArguments)
$syntheticArguments = [object[]]@($SyntheticDxf, 800, 600, $null, $null, 0, 0)
$syntheticOk = [bool]$method.Invoke($null, $syntheticArguments)
[pscustomobject]@{
    ok = $ok
    error = [string]$arguments[4]
    entityCount = [int]$arguments[5]
    renderedCount = [int]$arguments[6]
    width = if ($null -eq $image) { 0 } else { [int]$image.Width }
    height = if ($null -eq $image) { 0 } else { [int]$image.Height }
    bufferLength = if ($null -eq $buffer) { 0 } else { $buffer.Length }
    nonWhitePixels = $nonWhite
    emptyOk = $emptyOk
    emptyError = [string]$emptyArguments[4]
    emptyImageIsNull = $null -eq $emptyArguments[3]
    syntheticOk = $syntheticOk
    syntheticError = [string]$syntheticArguments[4]
    syntheticEntityCount = [int]$syntheticArguments[5]
    syntheticRenderedCount = [int]$syntheticArguments[6]
} | ConvertTo-Json -Compress
'@ | Set-Content -LiteralPath $runtime -Encoding UTF8
    $runtimeResult = & powershell.exe -NoProfile -ExecutionPolicy Bypass -File $runtime -Assembly $assembly -ScriptMethods $scriptMethods -NetDxf $netDxf -Dxf $dxf -EmptyDxf $emptyDxf -SyntheticDxf $syntheticDxf | ConvertFrom-Json
    if (-not $runtimeResult.ok) { throw "DXF runtime render failed: $($runtimeResult.error)" }
    if ($runtimeResult.entityCount -ne 22 -or $runtimeResult.renderedCount -lt 22) { throw "Unexpected entity counts: $($runtimeResult.entityCount)/$($runtimeResult.renderedCount)" }
    if ($runtimeResult.width -ne 1920 -or $runtimeResult.height -ne 1080 -or $runtimeResult.bufferLength -ne (1920 * 1080 * 3) -or $runtimeResult.nonWhitePixels -le 0) { throw 'DXF renderer returned an empty, white, or incorrectly sized image.' }
    if ($runtimeResult.emptyOk -or -not $runtimeResult.emptyImageIsNull -or $runtimeResult.emptyError -notmatch '^DXF_NO_DRAWABLE_ENTITIES:') { throw 'Empty DXF did not return an explicit no-drawable-entities failure.' }
    if (-not $runtimeResult.syntheticOk -or $runtimeResult.syntheticEntityCount -ne 2 -or $runtimeResult.syntheticRenderedCount -ne 2) { throw "Ellipse/Insert runtime render failed: $($runtimeResult.syntheticError)" }

    $bad = Get-Content -Raw $fixture -Encoding UTF8 | ConvertFrom-Json
    $bad.scripts[0].inputs[1].default = 5472
    $badFile = Join-Path $temp 'bad-default.json'
    $bad | ConvertTo-Json -Depth 30 | Set-Content -LiteralPath $badFile -Encoding UTF8
    $badPlan = & dotnet $cli plan --spec $badFile | ConvertFrom-Json
    if ($badPlan.ok -or @($badPlan.issues | Where-Object code -eq 'DXF_RENDER_DEFAULT_SIZE_INVALID').Count -ne 1) { throw 'Non-1920 default preview width was not rejected.' }

    $compileBad = Get-Content -Raw $fixture -Encoding UTF8 | ConvertFrom-Json
    $compileBad.task.name = 'm13-structured-compile-error'
    $compileBad.scripts[0].operations = @()
    $compileBad.scripts[0].outputs[1].type = 'int'
    $compileBad.scripts[0] | Add-Member -NotePropertyName source -NotePropertyValue "using Script.Methods; public partial class UserScript : ScriptMethods, IProcessMethods { public void Init() {} public bool Process() { netDxf.TypeThatDoesNotExist value = null; return value == null; } }"
    $compileBadFile = Join-Path $temp 'compile-bad.json'
    $compileBad | ConvertTo-Json -Depth 30 | Set-Content -LiteralPath $compileBadFile -Encoding UTF8
    $oldPreference = $ErrorActionPreference; $ErrorActionPreference = 'Continue'
    $compileBadText = (& dotnet $cli build --spec $compileBadFile --output (Join-Path $temp 'compile-bad') 2>&1) -join "`n"
    $compileBadExit = $LASTEXITCODE
    $ErrorActionPreference = $oldPreference
    if ($compileBadExit -eq 0) { throw 'Invalid C# source unexpectedly compiled.' }
    $compileError = $compileBadText | ConvertFrom-Json
    if ($compileError.error -ne 'SCRIPT_PRECOMPILE_FAILED' -or $compileError.details.stage -ne 'csharp-precompile' -or @($compileError.details.diagnostics).Count -lt 1 -or [string]::IsNullOrWhiteSpace($compileError.details.diagnostics[0].code) -or [string]::IsNullOrWhiteSpace($compileError.details.diagnostics[0].category)) { throw 'C# compiler error was not returned as structured diagnostics.' }
    $global:LASTEXITCODE = 0

    [pscustomobject]@{
        ok = $true
        deterministicOperation = $true
        offlinePrecompile = $true
        entityCount = $runtimeResult.entityCount
        renderedCount = $runtimeResult.renderedCount
        imageSize = "1920x1080"
        nonWhitePixels = $runtimeResult.nonWhitePixels
        singleFinalPixelBuffer = $true
        guardedNoDrawableEntity = $true
        configurableRuntimeDimensions = $true
        structuredCompileDiagnostics = $true
    } | ConvertTo-Json
}
finally { if (Test-Path -LiteralPath $temp) { Remove-Item -LiteralPath $temp -Recurse -Force } }
