param(
    [string]$Version,
    [switch]$SkipPublish,
    [string]$OutputDirectory
)

$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$props = Join-Path $root 'Directory.Build.props'
if ([string]::IsNullOrWhiteSpace($Version)) {
    [xml]$versionProps = Get-Content -LiteralPath $props -Raw -Encoding UTF8
    $Version = [string]$versionProps.Project.PropertyGroup.Version
}
if ($Version -notmatch '^\d+\.\d+\.\d+$') {
    throw "Installer version must use major.minor.patch format: $Version"
}
$version4 = $Version + '.0'
$dist = Join-Path $root 'dist'
$output = if ([string]::IsNullOrWhiteSpace($OutputDirectory)) {
    Join-Path $root ('artifacts\release\v' + $Version)
} else {
    [IO.Path]::GetFullPath($OutputDirectory)
}

if (-not $SkipPublish) {
    & (Join-Path $root 'scripts\publish.ps1')
    if ($LASTEXITCODE -ne 0) { throw 'Release publishing failed before installer creation.' }
}

$required = @(
    (Join-Path $dist 'Desktop\vm-script-compiler-desktop.exe'),
    (Join-Path $dist 'Desktop\runtime\node.exe'),
    (Join-Path $dist 'Desktop\agent\dist\main.js'),
    (Join-Path $dist 'Desktop\worker\vm-script-domain-worker.exe'),
    (Join-Path $dist 'Cli\VmScriptCompiler.Cli.exe'),
    (Join-Path $dist 'Mcp\vm-script-compiler-mcp.exe'),
    (Join-Path $dist 'release-manifest.json')
)
foreach ($file in $required) {
    if (-not (Test-Path -LiteralPath $file -PathType Leaf)) {
        throw "Installer payload is incomplete: $file"
    }
}

$manifest = Get-Content -LiteralPath (Join-Path $dist 'release-manifest.json') -Raw -Encoding UTF8 | ConvertFrom-Json
if ($manifest.version -ne $Version) {
    throw "Release manifest version $($manifest.version) does not match installer version $Version."
}
if (-not $manifest.componentSynchronization.desktopWorkerAndMcp) {
    throw 'Desktop Worker and MCP Core payloads are not synchronized.'
}
$products = @($manifest.products.name)
foreach ($name in @('Desktop', 'Cli', 'Mcp')) {
    if ($products -notcontains $name) { throw "Release manifest does not contain $name." }
}
$solFiles = @(Get-ChildItem -LiteralPath $dist -Recurse -File -Filter '*.sol')
if ($solFiles.Count -ne 0) { throw 'Installer payload contains forbidden SOL files.' }

$makensis = @(
    'C:\Program Files (x86)\NSIS\makensis.exe',
    'C:\Program Files\NSIS\makensis.exe'
) | Where-Object { Test-Path -LiteralPath $_ -PathType Leaf } | Select-Object -First 1
if (-not $makensis) {
    $command = Get-Command makensis.exe -ErrorAction SilentlyContinue
    if ($command) { $makensis = $command.Source }
}
if (-not $makensis) {
    throw 'NSIS makensis.exe was not found. Install NSIS 3.x before building the installer.'
}

New-Item -ItemType Directory -Force -Path $output | Out-Null
$installerName = "VM-Script-Solution-Compiler-Setup-$Version-x64.exe"
$installer = Join-Path $output $installerName
$payloadBytes = (Get-ChildItem -LiteralPath $dist -Recurse -File | Measure-Object -Property Length -Sum).Sum
$installSizeKb = [Math]::Ceiling($payloadBytes / 1KB)
$script = Join-Path $root 'installer\vm-script-solution-compiler.nsi'

$availableDrive = @('V:', 'W:', 'X:', 'U:', 'T:') |
    Where-Object { -not (Test-Path "$_\") } |
    Select-Object -First 1
if (-not $availableDrive) {
    throw 'No temporary drive letter is available for the NSIS source mapping.'
}

$substOutput = & subst.exe $availableDrive $dist 2>&1
if ($LASTEXITCODE -ne 0) {
    throw "Could not map $dist to $availableDrive for NSIS: $substOutput"
}

try {
    $nsisDistRoot = "$availableDrive\"
    & $makensis /V2 /INPUTCHARSET UTF8 `
        "/DVERSION=$Version" `
        "/DVERSION4=$version4" `
        "/DDISTROOT=$nsisDistRoot" `
        "/DOUTFILE=$installer" `
        "/DINSTALLSIZEKB=$installSizeKb" `
        $script
    if ($LASTEXITCODE -ne 0) { throw "NSIS compilation failed with exit code $LASTEXITCODE." }
}
finally {
    & subst.exe $availableDrive /D | Out-Null
}
if (-not (Test-Path -LiteralPath $installer -PathType Leaf)) { throw 'NSIS did not produce the installer.' }

$hash = (Get-FileHash -LiteralPath $installer -Algorithm SHA256).Hash.ToLowerInvariant()
$checksum = Join-Path $output ($installerName + '.sha256')
Set-Content -LiteralPath $checksum -Encoding ASCII -Value "$hash  $installerName"
Copy-Item -LiteralPath (Join-Path $dist 'release-manifest.json') -Destination (Join-Path $output 'release-manifest.json') -Force

[pscustomobject]@{
    ok = $true
    version = $Version
    nsisVersion = (& $makensis /VERSION)
    installer = $installer
    installerBytes = (Get-Item -LiteralPath $installer).Length
    sha256 = $hash
    checksum = $checksum
    manifest = Join-Path $output 'release-manifest.json'
    products = @('Desktop', 'Cli', 'Mcp')
    containsSolFiles = $false
} | ConvertTo-Json -Depth 5
