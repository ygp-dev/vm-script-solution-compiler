param(
    [string] $Source = (Join-Path $PSScriptRoot '..\docs\SOL'),
    [string] $Output = (Join-Path $PSScriptRoot '..\knowledge\reference-solutions')
)

$ErrorActionPreference = 'Stop'
$root = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$parser = Join-Path $root 'tools\vm-solution-parser\VMSolutionParser.Cli.exe'
$sourceRoot = [IO.Path]::GetFullPath($Source)
$outputRoot = [IO.Path]::GetFullPath($Output)
$sourceOutput = Join-Path $outputRoot 'scripts'
New-Item -ItemType Directory -Force -Path $sourceOutput | Out-Null

function Safe-Name([string] $value) {
    $invalid = [IO.Path]::GetInvalidFileNameChars()
    return -join @($value.ToCharArray() | ForEach-Object { if ($invalid -contains $_) { '_' } else { $_ } })
}

function Relative-Path([string] $base, [string] $path) {
    $basePath = [IO.Path]::GetFullPath($base).TrimEnd('\')
    $fullPath = [IO.Path]::GetFullPath($path)
    if (-not $fullPath.StartsWith($basePath + '\', [StringComparison]::OrdinalIgnoreCase)) { throw "Path is outside base: $fullPath" }
    return $fullPath.Substring($basePath.Length + 1)
}

function Decode-XmlParam($param) {
    if ($null -eq $param -or [string]::IsNullOrWhiteSpace($param.rawDataB64)) { return $null }
    $bytes = [Convert]::FromBase64String($param.rawDataB64)
    while ($bytes.Length -gt 0 -and $bytes[$bytes.Length - 1] -eq 0) { $bytes = $bytes[0..($bytes.Length - 2)] }
    return [Text.Encoding]::UTF8.GetString($bytes)
}

function Read-Ports($module, [string] $name) {
    $param = @($module.binaryParams | Where-Object name -eq $name)[0]
    $xmlText = Decode-XmlParam $param
    if ([string]::IsNullOrWhiteSpace($xmlText)) { return @() }
    try {
        [xml] $xml = $xmlText
        return @($xml.ArrayOfModuleParamItem.ModuleParamItem | ForEach-Object {
            [pscustomobject]@{ name = ([string]$_.Name).Trim('%'); valueType = [string]$_.ValueType; builtIn = -not ([string]$_.Name).StartsWith('%') }
        })
    } catch { return @([pscustomobject]@{ parseError = $_.Exception.Message }) }
}

function Read-Script($module) {
    if (-not [string]::IsNullOrWhiteSpace($module.scriptText)) { return [string]$module.scriptText }
    $param = @($module.binaryParams | Where-Object name -eq 'ShellContent')[0]
    if ($null -eq $param) { return $null }
    if (-not $param.rawTruncated -and -not [string]::IsNullOrWhiteSpace($param.rawDataB64)) {
        $bytes = [Convert]::FromBase64String($param.rawDataB64)
        while ($bytes.Length -gt 0 -and $bytes[$bytes.Length - 1] -eq 0) { $bytes = $bytes[0..($bytes.Length - 2)] }
        return [Text.Encoding]::UTF8.GetString($bytes)
    }
    return [string]$param.parsed
}

$inventory = @()
$temp = Join-Path ([IO.Path]::GetTempPath()) ('vm-reference-analysis-' + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $temp | Out-Null
try {
    foreach ($sol in Get-ChildItem -LiteralPath $sourceRoot -Recurse -File -Filter '*.sol' | Sort-Object FullName) {
        $relative = Relative-Path $sourceRoot $sol.FullName
        $jsonFile = Join-Path $temp (([guid]::NewGuid().ToString('N')) + '.json')
        & $parser parse -f $sol.FullName -o $jsonFile --include-raw | Out-Null
        $exitCode = $LASTEXITCODE
        if ($exitCode -ne 0 -or -not (Test-Path $jsonFile)) {
            $inventory += [pscustomobject]@{ file=$relative; sha256=(Get-FileHash $sol.FullName -Algorithm SHA256).Hash; parseExitCode=$exitCode; scripts=@(); error='parse failed' }
            continue
        }
        try {
            $jsonText = Get-Content $jsonFile -Raw -Encoding UTF8
            # vm-sol-format v2.3.0 emits `"params": [ }` for an empty ProcedureIO list in some legacy SOLs.
            $jsonText = [regex]::Replace($jsonText, '"params"\s*:\s*\[\s*\},', '"params": [],')
            $parsed = $jsonText | ConvertFrom-Json
        }
        catch {
            $inventory += [pscustomobject]@{ file=$relative; sha256=(Get-FileHash $sol.FullName -Algorithm SHA256).Hash; parseExitCode=$exitCode; scripts=@(); error=('invalid parser JSON: ' + $_.Exception.Message) }
            continue
        }
        $scripts = @()
        foreach ($module in @($parsed.solution.procedures.modules | Where-Object { $_.name -in @('ShellModule','PyShellModule') })) {
            $source = Read-Script $module
            $extension = if ($module.name -eq 'PyShellModule') { '.py' } else { '.cs' }
            $folder = Join-Path $sourceOutput (Safe-Name ([IO.Path]::GetFileNameWithoutExtension($relative)))
            New-Item -ItemType Directory -Force -Path $folder | Out-Null
            $sourceFile = Join-Path $folder ((Safe-Name ([string]$module.fullPath).Replace('.', '_')) + $extension)
            if ($null -ne $source) { [IO.File]::WriteAllText($sourceFile, $source, [Text.UTF8Encoding]::new($false)) }
            $scripts += [pscustomobject]@{
                carrier = if ($module.name -eq 'PyShellModule') { 'python-module' } else { 'csharp-module' }
                fullPath = $module.fullPath
                inputs = @(Read-Ports $module 'Input')
                outputs = @(Read-Ports $module 'Output')
                subscriptions = @($module.subscriptions)
                sourceFile = (Relative-Path $outputRoot $sourceFile).Replace('\','/')
                sourceLength = if ($null -eq $source) { 0 } else { $source.Length }
            }
        }
        $inventory += [pscustomobject]@{
            file = $relative.Replace('\','/')
            sha256 = (Get-FileHash $sol.FullName -Algorithm SHA256).Hash
            parseExitCode = $exitCode
            warnings = @($parsed.solution.warnings)
            procedures = @($parsed.solution.procedures | ForEach-Object { [pscustomobject]@{ name=$_.displayName; modules=@($_.modules | ForEach-Object { [pscustomobject]@{ type=$_.name; name=$_.displayName; fullPath=$_.fullPath; front=@($_.connections.frontModules); follow=@($_.connections.followModules) } }) } })
            scripts = $scripts
            globalScript = $parsed.solution.globalScript
        }
    }
    $inventoryFile = Join-Path $outputRoot 'inventory.json'
    [IO.File]::WriteAllText($inventoryFile, ($inventory | ConvertTo-Json -Depth 30), [Text.UTF8Encoding]::new($false))
    [pscustomobject]@{ ok=$true; solutions=$inventory.Count; scripts=@($inventory.scripts).Count; output=$inventoryFile } | ConvertTo-Json
}
finally {
    if (Test-Path $temp) { Remove-Item -LiteralPath $temp -Recurse -Force }
}
