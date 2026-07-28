param(
    [string] $Chm = (Join-Path $PSScriptRoot '..\docs\SOL\VM FAQ手册(V1.12).chm'),
    [string] $Output = (Join-Path $PSScriptRoot '..\knowledge\faq-v1.12')
)

$ErrorActionPreference = 'Stop'
$chmPath = [IO.Path]::GetFullPath($Chm)
$outputRoot = [IO.Path]::GetFullPath($Output)
$extract = Join-Path ([IO.Path]::GetTempPath()) 'vm-faq-v112-extracted'
if (-not (Test-Path $extract)) {
    New-Item -ItemType Directory -Force -Path $extract | Out-Null
    $process = Start-Process -FilePath "$env:WINDIR\hh.exe" -ArgumentList @('-decompile', $extract, $chmPath) -WindowStyle Hidden -Wait -PassThru
    if ($process.ExitCode -ne 0) { throw "CHM extraction failed: $($process.ExitCode)" }
}
New-Item -ItemType Directory -Force -Path (Join-Path $outputRoot 'topics') | Out-Null

function Clean-Html([string] $html) {
    $text = [Net.WebUtility]::HtmlDecode($html)
    $text = [regex]::Replace($text, '(?is)<script.*?</script>|<style.*?</style>', ' ')
    $text = [regex]::Replace($text, '(?s)<[^>]+>', ' ')
    return [regex]::Replace($text, '\s+', ' ').Trim()
}

$topics = @()
$index = 0
$scriptWord = ([char]0x811A) + ([char]0x672C)
$globalVariableWord = ([char]0x5168) + ([char]0x5C40) + ([char]0x53D8) + ([char]0x91CF)
foreach ($file in Get-ChildItem $extract -Recurse -File -Include '*.html','*.htm' | Sort-Object FullName) {
    $html = Get-Content $file.FullName -Raw
    $plain = Clean-Html $html
    $selected = $file.Name.Contains($scriptWord) -or $file.Name.Contains($globalVariableWord) -or $plain -match 'ShellModule|GlobalScript|CurrentProcess|GetModule\s*\('
    if (-not $selected) { continue }
    $index++
    $topicFile = ('{0:D3}-{1}.txt' -f $index, ([IO.Path]::GetFileNameWithoutExtension($file.Name) -replace '[\\/:*?"<>|]', '_'))
    [IO.File]::WriteAllText((Join-Path $outputRoot ('topics\' + $topicFile)), $plain, [Text.UTF8Encoding]::new($false))
    $keywords = @('ShellModule','GlobalScript','CurrentProcess','GetModule','GetValue','SetValue','GetGlobalVariable','SetGlobalVariable','ExecuteProcess','SendCommDeviceData','OpenCV','ImageData') | Where-Object { $plain.Contains($_) }
    $topics += [pscustomobject]@{
        title = [IO.Path]::GetFileNameWithoutExtension($file.Name)
        chmPath = $file.FullName.Substring($extract.TrimEnd('\').Length + 1).Replace('\','/')
        textFile = 'topics/' + $topicFile
        keywords = @($keywords)
        length = $plain.Length
    }
}
[IO.File]::WriteAllText((Join-Path $outputRoot 'index.json'), ($topics | ConvertTo-Json -Depth 10), [Text.UTF8Encoding]::new($false))
[pscustomobject]@{ ok=$true; topics=$topics.Count; output=(Join-Path $outputRoot 'index.json') } | ConvertTo-Json
