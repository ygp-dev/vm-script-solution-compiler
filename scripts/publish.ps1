param([switch]$SkipTests, [switch]$SkipAgentInstall)
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$dist = Join-Path $root 'dist'
$nodeRoot = Join-Path $root '.runtime\node-v22.19.0-win-x64'
$node = Join-Path $nodeRoot 'node.exe'
$npm = Join-Path $nodeRoot 'npm.cmd'

foreach ($required in @($node, $npm, (Join-Path $root 'agent\package-lock.json'))) {
    if (-not (Test-Path -LiteralPath $required -PathType Leaf)) { throw "Release prerequisite is missing: $required" }
}

& dotnet restore (Join-Path $root 'VmScriptCompiler.sln') -r win-x64
if ($LASTEXITCODE -ne 0) { throw 'win-x64 restore failed.' }
& dotnet build (Join-Path $root 'VmScriptCompiler.sln') -c Release --no-restore
if ($LASTEXITCODE -ne 0) { throw 'Release build failed.' }
& $npm run build --prefix (Join-Path $root 'agent')
if ($LASTEXITCODE -ne 0) { throw 'Pi Agent build failed.' }

if (-not $SkipTests) {
    foreach ($test in @(
        'run-full-smoke.ps1','run-m2-smoke.ps1','run-m6-smoke.ps1','run-m8-smoke.ps1',
        'run-m9-smoke.ps1','run-m10-smoke.ps1','run-m11-smoke.ps1','run-m12-smoke.ps1','run-m13-smoke.ps1',
        'run-audit-smoke.ps1','run-domain-worker-smoke.ps1','run-agent-domain-smoke.ps1',
        'run-agent-point-sort-smoke.ps1','run-desktop-agent-smoke.ps1','run-community-knowledge-smoke.ps1'
    )) {
        & (Join-Path $root ('tests\' + $test)) -SkipBuild
        if ($LASTEXITCODE -ne 0) { throw "Test failed: $test" }
    }
}

$products = @(
    @{ Name='Cli'; Project='src\VmScriptCompiler.Cli\VmScriptCompiler.Cli.csproj'; Exe='VmScriptCompiler.Cli.exe'; Assembly='VmScriptCompiler.Cli.dll' },
    @{ Name='Desktop'; Project='src\VmScriptCompiler.Desktop\VmScriptCompiler.Desktop.csproj'; Exe='vm-script-compiler-desktop.exe'; Assembly='vm-script-compiler-desktop.dll' },
    @{ Name='Mcp'; Project='src\VmScriptCompiler.Mcp\VmScriptCompiler.Mcp.csproj'; Exe='vm-script-compiler-mcp.exe'; Assembly='vm-script-compiler-mcp.dll' }
)

New-Item -ItemType Directory -Force -Path $dist | Out-Null
foreach ($backup in Get-ChildItem -LiteralPath $dist -Directory -Filter '_release-backup-*' -ErrorAction SilentlyContinue) {
    if ([IO.Path]::GetDirectoryName($backup.FullName) -ne [IO.Path]::GetFullPath($dist).TrimEnd('\')) {
        throw "Unexpected release backup path: $($backup.FullName)"
    }
    try { Remove-Item -LiteralPath $backup.FullName -Recurse -Force -ErrorAction Stop }
    catch { Write-Warning "A running previous release is still retained at $($backup.FullName)" }
}
foreach ($obsolete in @('Agent','Desktop-final','Desktop-fixed5','Desktop-fixed6')) {
    $path = Join-Path $dist $obsolete
    if (Test-Path -LiteralPath $path) { Remove-Item -LiteralPath $path -Recurse -Force }
}

foreach ($product in $products) {
    $output = Join-Path $dist $product.Name
    $staging = $output + '.staging'
    if (Test-Path -LiteralPath $staging) { Remove-Item -LiteralPath $staging -Recurse -Force }
    & dotnet publish (Join-Path $root $product.Project) -c Release -r win-x64 --self-contained true --no-restore -o $staging
    if ($LASTEXITCODE -ne 0) { throw "Publish failed: $($product.Name)" }

    if ($product.Name -eq 'Desktop') {
        $worker = Join-Path $staging 'worker'
        & dotnet publish (Join-Path $root 'src\VmScriptCompiler.DomainWorker\VmScriptCompiler.DomainWorker.csproj') `
            -c Release -r win-x64 --self-contained true --no-restore -o $worker
        if ($LASTEXITCODE -ne 0) { throw 'Domain Worker publish failed.' }

        $agentPayload = Join-Path $staging 'agent'
        New-Item -ItemType Directory -Force -Path $agentPayload | Out-Null
        Copy-Item -LiteralPath (Join-Path $root 'agent\package.json') -Destination $agentPayload
        Copy-Item -LiteralPath (Join-Path $root 'agent\package-lock.json') -Destination $agentPayload
        Copy-Item -LiteralPath (Join-Path $root 'agent\dist') -Destination $agentPayload -Recurse
        Copy-Item -LiteralPath (Join-Path $root 'agent\resources') -Destination $agentPayload -Recurse
        Push-Location $agentPayload
        try {
            if ($SkipAgentInstall) {
                $localNodeModules = Join-Path $root 'agent\node_modules'
                if (-not (Test-Path -LiteralPath $localNodeModules -PathType Container)) {
                    throw 'SkipAgentInstall requested but the repository Agent node_modules directory is missing.'
                }
                # Use the already-resolved repository dependency tree when the
                # release machine is intentionally offline; the lockfile and
                # subsequent audit still govern the payload.
                Copy-Item -LiteralPath $localNodeModules -Destination (Join-Path $agentPayload 'node_modules') -Recurse -Force
            } else {
                & $npm ci --omit=dev --ignore-scripts --audit=false
                if ($LASTEXITCODE -ne 0) { throw 'Production Pi Agent dependency install failed.' }
            }
            # Release packaging must be reproducible in the VM/offline build
            # environment; the lockfile cache is the authoritative audit input.
            & $npm audit --omit=dev --audit-level=high --offline
            if ($LASTEXITCODE -ne 0) { throw 'Production Pi Agent dependency audit failed.' }
        }
        finally { Pop-Location }

        $nodeModules = Join-Path $agentPayload 'node_modules'
        foreach ($unusedProvider in Get-ChildItem -LiteralPath $nodeModules -Recurse -Directory -Filter '@mistralai' -ErrorAction SilentlyContinue) {
            if (-not $unusedProvider.FullName.StartsWith($nodeModules + '\', [StringComparison]::OrdinalIgnoreCase)) {
                throw "Unexpected provider directory outside Agent payload: $($unusedProvider.FullName)"
            }
            Remove-Item -LiteralPath $unusedProvider.FullName -Recurse -Force
        }
        foreach ($developmentFile in Get-ChildItem -LiteralPath $nodeModules -Recurse -File -ErrorAction SilentlyContinue |
            Where-Object { $_.Name.EndsWith('.map', [StringComparison]::OrdinalIgnoreCase) -or
                           $_.Name.EndsWith('.ts', [StringComparison]::OrdinalIgnoreCase) }) {
            [IO.File]::Delete($developmentFile.FullName)
        }

        $runtime = Join-Path $staging 'runtime'
        New-Item -ItemType Directory -Force -Path $runtime | Out-Null
        Copy-Item -LiteralPath $node -Destination (Join-Path $runtime 'node.exe')
    }

    foreach ($required in @(
        (Join-Path $staging $product.Exe),
        (Join-Path $staging $product.Assembly),
        (Join-Path $staging 'schemas\requirement.schema.json'),
        (Join-Path $staging 'resources\vm\4.4.0\manifest.json'),
        (Join-Path $staging 'tools\vm-solution-parser\VMSolutionParser.Cli.exe'),
        (Join-Path $staging 'USAGE.md')
    )) {
        if (-not (Test-Path -LiteralPath $required -PathType Leaf)) { throw "Published file is missing: $required" }
    }
    if ($product.Name -eq 'Desktop') {
        foreach ($required in @(
            (Join-Path $staging 'runtime\node.exe'),
            (Join-Path $staging 'agent\dist\main.js'),
            (Join-Path $staging 'agent\resources\SYSTEM.md'),
            (Join-Path $staging 'worker\vm-script-domain-worker.exe')
        )) {
            if (-not (Test-Path -LiteralPath $required -PathType Leaf)) { throw "Desktop Agent payload is missing: $required" }
        }
    }
    $solFiles = @(
        Get-ChildItem $staging -File -Filter *.sol
        Get-ChildItem (Join-Path $staging 'resources') -Recurse -File -Filter *.sol
        if ($product.Name -eq 'Desktop') {
            Get-ChildItem (Join-Path $staging 'agent\dist') -Recurse -File -Filter *.sol
            Get-ChildItem (Join-Path $staging 'agent\resources') -Recurse -File -Filter *.sol
            Get-ChildItem (Join-Path $staging 'worker\resources') -Recurse -File -Filter *.sol
        }
    )
    if ($solFiles.Count -ne 0) {
        throw "Published product contains SOL files: $($product.Name)"
    }

    if (Test-Path -LiteralPath $output) {
        $backup = Join-Path $dist ("_release-backup-" + $product.Name + "-" + [DateTime]::UtcNow.ToString('yyyyMMddHHmmssfff'))
        Move-Item -LiteralPath $output -Destination $backup
        try { Remove-Item -LiteralPath $backup -Recurse -Force -ErrorAction Stop }
        catch { Write-Warning "Previous $($product.Name) payload remains in use and was retained at $backup" }
    }
    Move-Item -LiteralPath $staging -Destination $output
}

$manifestScript = Join-Path $root 'scripts\write_release_manifest_fast.mjs'
& $node $manifestScript $dist | Out-Null
if ($LASTEXITCODE -ne 0) { throw 'Release manifest generation failed.' }
$releaseSmoke = & (Join-Path $root 'tests\run-release-smoke.ps1') | ConvertFrom-Json
if (-not $releaseSmoke.ok) { throw 'Standalone release smoke failed.' }
Get-Content -LiteralPath (Join-Path $dist 'release-manifest.json') -Raw -Encoding UTF8
