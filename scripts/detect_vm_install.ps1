param()

$candidates = New-Object System.Collections.Generic.List[string]

foreach ($name in @('VISIONMASTER_HOME', 'VM_HOME')) {
    $value = [Environment]::GetEnvironmentVariable($name)
    if (-not [string]::IsNullOrWhiteSpace($value)) { $candidates.Add($value) }
}

if (-not [string]::IsNullOrWhiteSpace($env:MVDALGO_DEV_ENV)) {
    $candidates.Add((Split-Path -Parent $env:MVDALGO_DEV_ENV))
}

foreach ($part in ($env:Path -split ';')) {
    if ($part -match '^(.*?VisionMaster\d+(?:\.\d+){1,3})(?:\\.*)?$') {
        $candidates.Add($Matches[1])
    }
}

$resolved = $null
foreach ($candidate in ($candidates | Select-Object -Unique)) {
    if ([string]::IsNullOrWhiteSpace($candidate)) { continue }
    try { $full = [IO.Path]::GetFullPath($candidate.Trim('"').TrimEnd('\')) } catch { continue }
    $valid = (Test-Path -LiteralPath (Join-Path $full 'Development')) -and
             (Test-Path -LiteralPath (Join-Path $full 'MVDAlgorithmSDK')) -and
             (Test-Path -LiteralPath (Join-Path $full 'Applications'))
    if ($valid) { $resolved = $full; break }
}

if ($null -eq $resolved) {
    [PSCustomObject]@{
        ok = $false
        error = 'VM_HOME_NOT_FOUND'
        message = 'Set VISIONMASTER_HOME to the VisionMaster installation root.'
    } | ConvertTo-Json
    exit 2
}

$version = if ((Split-Path -Leaf $resolved) -match 'VisionMaster(.+)$') { $Matches[1] } else { $null }
$globalScript = Join-Path $resolved 'Applications\GlobalScript'
[PSCustomObject]@{
    ok = $true
    vmRoot = $resolved
    version = $version
    development = Join-Path $resolved 'Development'
    algorithmSdk = Join-Path $resolved 'MVDAlgorithmSDK'
    applications = Join-Path $resolved 'Applications'
    globalScript = $globalScript
    globalScriptAvailable = Test-Path -LiteralPath $globalScript
} | ConvertTo-Json
