param(
    [Parameter(Mandatory=$true)][string]$Installer,
    [string]$Version
)

$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
if ([string]::IsNullOrWhiteSpace($Version)) {
    [xml]$versionProps = Get-Content -LiteralPath (Join-Path $root 'Directory.Build.props') -Raw -Encoding UTF8
    $Version = [string]$versionProps.Project.PropertyGroup.Version
}
$installerPath = [IO.Path]::GetFullPath($Installer)
if (-not (Test-Path -LiteralPath $installerPath -PathType Leaf)) { throw "Installer not found: $installerPath" }
$testRoot = Join-Path ([IO.Path]::GetTempPath()) ('vm-script-installer-' + [Guid]::NewGuid().ToString('N'))
$installed = Join-Path $testRoot 'app'
$oldHome = $env:VM_SCRIPT_COMPILER_HOME

try {
    New-Item -ItemType Directory -Force -Path $testRoot | Out-Null
    $install = Start-Process -FilePath $installerPath -WindowStyle Hidden -PassThru -Wait -ArgumentList @('/S', "/D=$installed")
    if ($install.ExitCode -ne 0) { throw "Silent installer failed with exit code $($install.ExitCode)." }

    $desktop = Join-Path $installed 'Desktop\vm-script-compiler-desktop.exe'
    $cli = Join-Path $installed 'Cli\VmScriptCompiler.Cli.exe'
    $mcp = Join-Path $installed 'Mcp\vm-script-compiler-mcp.exe'
    $uninstaller = Join-Path $installed 'Uninstall.exe'
    foreach ($file in @(
        $desktop, $cli, $mcp, $uninstaller,
        (Join-Path $installed 'Desktop\runtime\node.exe'),
        (Join-Path $installed 'Desktop\agent\dist\main.js'),
        (Join-Path $installed 'Desktop\worker\vm-script-domain-worker.exe'),
        (Join-Path $installed 'release-manifest.json')
    )) {
        if (-not (Test-Path -LiteralPath $file -PathType Leaf)) { throw "Installed payload is incomplete: $file" }
    }

    Remove-Item Env:VM_SCRIPT_COMPILER_HOME -ErrorAction SilentlyContinue
    $desktopSmoke = Start-Process -FilePath $desktop -WindowStyle Hidden -PassThru -Wait -ArgumentList '--smoke-test'
    if ($desktopSmoke.ExitCode -ne 0) { throw 'Installed Desktop smoke failed.' }
    $environment = & $cli env | ConvertFrom-Json
    if (-not $environment.found) { throw 'Installed CLI environment detection failed.' }
    $request = '{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2024-11-05","capabilities":{},"clientInfo":{"name":"installer-smoke","version":"1"}}}'
    $mcpResponse = $request | & $mcp | ConvertFrom-Json
    if ($mcpResponse.result.serverInfo.name -ne 'vm-script-solution-compiler' -or
        $mcpResponse.result.serverInfo.version -ne $Version) {
        throw 'Installed MCP initialize failed.'
    }

    $manifest = Get-Content -LiteralPath (Join-Path $installed 'release-manifest.json') -Raw -Encoding UTF8 | ConvertFrom-Json
    if (-not $manifest.componentSynchronization.desktopWorkerAndMcp -or $manifest.version -ne $Version) {
        throw 'Installed release manifest is invalid.'
    }
    foreach ($product in $manifest.products) {
        $directory = Join-Path $installed $product.name
        $assembly = Join-Path $directory $product.applicationAssembly
        if ((Get-FileHash -LiteralPath $assembly -Algorithm SHA256).Hash -ne $product.applicationAssemblySha256) {
            throw "Installed assembly hash mismatch: $($product.name)"
        }
    }
    if (@(Get-ChildItem -LiteralPath $installed -Recurse -File -Filter '*.sol').Count -ne 0) {
        throw 'Installed payload contains forbidden SOL files.'
    }

    $uninstall = Start-Process -FilePath $uninstaller -WindowStyle Hidden -PassThru -Wait -ArgumentList '/S'
    if ($uninstall.ExitCode -ne 0) { throw "Silent uninstaller failed with exit code $($uninstall.ExitCode)." }
    $deadline = [DateTime]::UtcNow.AddSeconds(30)
    while ((Test-Path -LiteralPath $installed) -and [DateTime]::UtcNow -lt $deadline) {
        Start-Sleep -Milliseconds 200
    }
    if (Test-Path -LiteralPath $installed) { throw 'Uninstaller did not remove the installation directory.' }

    [pscustomobject]@{
        ok = $true
        version = $Version
        desktop = $true
        cli = $true
        mcp = $true
        synchronized = $true
        containsSolFiles = $false
        uninstall = $true
    } | ConvertTo-Json
}
finally {
    $env:VM_SCRIPT_COMPILER_HOME = $oldHome
    if (Test-Path -LiteralPath $testRoot) {
        Remove-Item -LiteralPath $testRoot -Recurse -Force -ErrorAction SilentlyContinue
    }
}
