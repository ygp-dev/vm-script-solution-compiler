param(
    [Parameter(Mandatory = $true)]
    [string] $OutputFile
)

$source = Join-Path $PSScriptRoot '..\resources\vm\4.4.0\script-base'
$source = [IO.Path]::GetFullPath($source)
if (-not (Test-Path -LiteralPath (Join-Path $source 'SolutionFile\VmServer.xml'))) {
    throw 'Bundled VM script template is incomplete.'
}

$output = [IO.Path]::GetFullPath($OutputFile)
if ([IO.Path]::GetExtension($output) -ne '.sol') { throw 'OutputFile must end with .sol.' }
$parent = Split-Path -Parent $output
if (-not (Test-Path -LiteralPath $parent)) { New-Item -ItemType Directory -Path $parent -Force | Out-Null }

Add-Type -AssemblyName System.IO.Compression
$file = [IO.File]::Open($output, [IO.FileMode]::Create, [IO.FileAccess]::ReadWrite, [IO.FileShare]::None)
try {
    $zip = New-Object IO.Compression.ZipArchive($file, [IO.Compression.ZipArchiveMode]::Create, $false)
    try {
        foreach ($item in (Get-ChildItem -LiteralPath $source -Recurse -File)) {
            $relative = $item.FullName.Substring($source.Length).TrimStart('\').Replace('/', '\')
            $entry = $zip.CreateEntry($relative, [IO.Compression.CompressionLevel]::Optimal)
            $input = [IO.File]::OpenRead($item.FullName)
            $entryStream = $entry.Open()
            try { $input.CopyTo($entryStream) }
            finally { $entryStream.Dispose(); $input.Dispose() }
        }
    }
    finally { $zip.Dispose() }
}
finally { $file.Dispose() }

[PSCustomObject]@{ ok = $true; output = $output } | ConvertTo-Json
