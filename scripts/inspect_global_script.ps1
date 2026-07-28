param(
    [Parameter(Mandatory = $true)]
    [string] $SolutionFile
)

if (-not (Test-Path -LiteralPath $SolutionFile)) { throw "Solution not found: $SolutionFile" }

Add-Type -AssemblyName System.IO.Compression.FileSystem
$zip = [IO.Compression.ZipFile]::OpenRead((Resolve-Path -LiteralPath $SolutionFile))
try {
    $entry = $zip.Entries | Where-Object { $_.FullName -eq 'SolutionFile\GlobalScript_0' }
    if ($null -eq $entry) { throw 'GlobalScript_0 not found in solution.' }
    $reader = [IO.StreamReader]::new($entry.Open(), [Text.Encoding]::UTF8, $true)
    try { $raw = $reader.ReadToEnd().TrimEnd([char]0) } finally { $reader.Dispose() }
    $script = $raw | ConvertFrom-Json
    [PSCustomObject]@{
        Version = $script.Version
        ScriptLength = $script.ScriptContent.Length
        References = @($script.ScriptRefences)
        ScriptContent = $script.ScriptContent
    } | ConvertTo-Json -Depth 6
}
finally { $zip.Dispose() }
