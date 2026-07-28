param([string]$DistDirectory)
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$dist = if ([string]::IsNullOrWhiteSpace($DistDirectory)) { Join-Path $root 'dist' } else { [IO.Path]::GetFullPath($DistDirectory) }
$products = @(
    @{ Name='Cli'; Exe='VmScriptCompiler.Cli.exe'; Assembly='VmScriptCompiler.Cli.dll' },
    @{ Name='Desktop'; Exe='vm-script-compiler-desktop.exe'; Assembly='vm-script-compiler-desktop.dll' },
    @{ Name='Mcp'; Exe='vm-script-compiler-mcp.exe'; Assembly='vm-script-compiler-mcp.dll' }
)
$entries = @()
foreach ($product in $products) {
    $output = Join-Path $dist $product.Name
    $exe = Join-Path $output $product.Exe
    $assembly = Join-Path $output $product.Assembly
    foreach ($required in @($exe, $assembly, (Join-Path $output 'schemas\requirement.schema.json'), (Join-Path $output 'resources\vm\4.4.0\manifest.json'), (Join-Path $output 'tools\vm-solution-parser\VMSolutionParser.Cli.exe'), (Join-Path $output 'USAGE.md'))) {
        if (-not (Test-Path -LiteralPath $required -PathType Leaf)) { throw "Published file is missing: $required" }
    }
    $integrityFiles = @(
        $exe, $assembly,
        (Join-Path $output 'VmScriptCompiler.Core.dll'),
        (Join-Path $output 'schemas\requirement.schema.json'),
        (Join-Path $output 'resources\vm\4.4.0\manifest.json'),
        (Join-Path $output 'tools\vm-solution-parser\VMSolutionParser.Cli.exe'),
        (Join-Path $output 'USAGE.md')
    )
    if ($product.Name -eq 'Desktop') {
        $integrityFiles += @(
            (Join-Path $output 'runtime\node.exe'),
            (Join-Path $output 'agent\dist\main.js'),
            (Join-Path $output 'agent\package-lock.json'),
            (Join-Path $output 'agent\resources\SYSTEM.md'),
            (Join-Path $output 'worker\vm-script-domain-worker.exe'),
            (Join-Path $output 'worker\VmScriptCompiler.Core.dll')
        )
        $integrityFiles += @(
            Get-ChildItem (Join-Path $output 'agent\dist') -Recurse -File
            Get-ChildItem (Join-Path $output 'agent\resources') -Recurse -File
        )
    }
    $payloadLines = @($integrityFiles | Sort-Object -Unique | ForEach-Object {
        $filePath = if ($_ -is [IO.FileInfo]) { $_.FullName } else { [string]$_ }
        $filePath.Substring($output.Length + 1).Replace('\','/') + "`t" +
            (Get-FileHash -LiteralPath $filePath -Algorithm SHA256).Hash
    })
    $sha = [Security.Cryptography.SHA256]::Create()
    try { $payloadHash = [BitConverter]::ToString($sha.ComputeHash([Text.Encoding]::UTF8.GetBytes(($payloadLines -join "`n")))).Replace('-','') }
    finally { $sha.Dispose() }
    $entries += [pscustomobject]@{
        name=$product.Name; entryPoint=$product.Exe; entryPointSha256=(Get-FileHash $exe -Algorithm SHA256).Hash
        applicationAssembly=$product.Assembly; applicationAssemblySha256=(Get-FileHash $assembly -Algorithm SHA256).Hash
        integrityFiles=$payloadLines.Count; integritySha256=$payloadHash
    }
}

$desktopCore = (Get-FileHash -LiteralPath (Join-Path $dist 'Desktop\VmScriptCompiler.Core.dll') -Algorithm SHA256).Hash
$workerCore = (Get-FileHash -LiteralPath (Join-Path $dist 'Desktop\worker\VmScriptCompiler.Core.dll') -Algorithm SHA256).Hash
$mcpCore = (Get-FileHash -LiteralPath (Join-Path $dist 'Mcp\VmScriptCompiler.Core.dll') -Algorithm SHA256).Hash
if ($desktopCore -ne $workerCore -or $desktopCore -ne $mcpCore) { throw 'Desktop, Domain Worker and MCP Core payloads are not synchronized.' }

$nodeVersion = & (Join-Path $dist 'Desktop\runtime\node.exe') --version
$agentPayloadFiles = @(
    Get-ChildItem (Join-Path $dist 'Desktop\agent\dist') -Recurse -File
    Get-ChildItem (Join-Path $dist 'Desktop\agent\resources') -Recurse -File
)
$agentPayloadLines = @($agentPayloadFiles | Sort-Object FullName | ForEach-Object {
    $_.FullName.Substring((Join-Path $dist 'Desktop\agent').Length + 1).Replace('\','/') +
        "`t" + (Get-FileHash $_.FullName -Algorithm SHA256).Hash
})
$agentSha = [Security.Cryptography.SHA256]::Create()
try {
    $agentPayloadHash = [BitConverter]::ToString(
        $agentSha.ComputeHash([Text.Encoding]::UTF8.GetBytes(($agentPayloadLines -join "`n")))).Replace('-','')
}
finally { $agentSha.Dispose() }
$manifest = [pscustomobject]@{
    version='1.0.0'
    runtime='win-x64 self-contained desktop + bundled Node'
    generatedUtc=[DateTime]::UtcNow.ToString('o')
    products=$entries
    architecture=[pscustomobject]@{
        primaryProduct='Desktop'
        domainAgent='Pi 0.82.1'
        nodeVersion=$nodeVersion
        deterministicWorker='vm-script-domain-worker.exe'
        mcpRole='external deterministic adapter'
        legacyAgentPublished=$false
    }
    componentSynchronization=[pscustomobject]@{
        desktopWorkerAndMcp=$true
        coreSha256=$desktopCore
        agentMainSha256=(Get-FileHash -LiteralPath (Join-Path $dist 'Desktop\agent\dist\main.js') -Algorithm SHA256).Hash
        agentPayloadFiles=$agentPayloadLines.Count
        agentPayloadSha256=$agentPayloadHash
        workerSha256=(Get-FileHash -LiteralPath (Join-Path $dist 'Desktop\worker\vm-script-domain-worker.exe') -Algorithm SHA256).Hash
        nodeSha256=(Get-FileHash -LiteralPath (Join-Path $dist 'Desktop\runtime\node.exe') -Algorithm SHA256).Hash
    }
    containsSolFiles=$false
}
$manifest | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $dist 'release-manifest.json') -Encoding UTF8
$manifest | ConvertTo-Json -Depth 8
