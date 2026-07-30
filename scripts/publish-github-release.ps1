param(
    [string]$Version,
    [switch]$SkipBuild,
    [switch]$Draft
)

$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
if ([string]::IsNullOrWhiteSpace($Version)) {
    [xml]$props = Get-Content -LiteralPath (Join-Path $root 'Directory.Build.props') -Raw -Encoding UTF8
    $Version = [string]$props.Project.PropertyGroup.Version
}
$tag = 'v' + $Version
$repo = 'ygp-dev/vm-script-solution-compiler'
$output = Join-Path $root ('artifacts\release\' + $tag)

if (-not $SkipBuild) {
    & (Join-Path $root 'scripts\build-installer.ps1') -Version $Version
    if ($LASTEXITCODE -ne 0) { throw 'Installer build failed.' }
}

$installer = Join-Path $output "VM-Script-Solution-Compiler-Setup-$Version-x64.exe"
$assets = @(
    $installer,
    ($installer + '.sha256'),
    (Join-Path $output 'release-manifest.json')
)
foreach ($asset in $assets) {
    if (-not (Test-Path -LiteralPath $asset -PathType Leaf)) { throw "Release asset is missing: $asset" }
}
$notes = Join-Path $root ('docs\releases\' + $tag + '.md')
if (-not (Test-Path -LiteralPath $notes -PathType Leaf)) { throw "Release notes are missing: $notes" }

$gh = 'C:\Program Files\GitHub CLI\gh.exe'
if (-not (Test-Path -LiteralPath $gh -PathType Leaf)) {
    $command = Get-Command gh.exe -ErrorAction SilentlyContinue
    if (-not $command) { throw 'GitHub CLI was not found.' }
    $gh = $command.Source
}
$releaseTags = @(& $gh release list --repo $repo --limit 100 --json tagName --jq '.[].tagName')
if ($LASTEXITCODE -ne 0) { throw 'Unable to query existing GitHub Releases.' }
if ($releaseTags -contains $tag) { throw "GitHub Release already exists: $tag" }

$arguments = @(
    'release', 'create', $tag,
    '--repo', $repo,
    '--target', 'main',
    '--title', "VM Script Solution Compiler $Version",
    '--notes-file', $notes
)
if ($Draft) { $arguments += '--draft' }
$arguments += $assets
& $gh @arguments
if ($LASTEXITCODE -ne 0) { throw 'GitHub Release creation failed.' }

& $gh release view $tag --repo $repo --json tagName,name,url,isDraft,isPrerelease,assets
if ($LASTEXITCODE -ne 0) { throw 'Unable to verify the created GitHub Release.' }
