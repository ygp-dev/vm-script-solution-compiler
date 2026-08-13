param([switch]$SkipBuild)
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$cli = Join-Path $root 'src\VmScriptCompiler.Cli\bin\Release\net8.0\VmScriptCompiler.Cli.dll'
$fixtures = Join-Path $root 'tests\fixtures'
$temp = Join-Path ([IO.Path]::GetTempPath()) ('vm-script-audit-' + [guid]::NewGuid().ToString('N'))
try {
    if (-not $SkipBuild) { & dotnet build (Join-Path $root 'VmScriptCompiler.sln') -c Release --no-restore | Out-Host }
    New-Item -ItemType Directory -Path $temp | Out-Null

    $plan = & dotnet $cli plan --spec (Join-Path $fixtures 'm3-shell-create.json') | ConvertFrom-Json
    if (-not $plan.ok -or 'configureScriptModules' -notin @($plan.actions) -or 'writeGlobalScript' -in @($plan.actions)) { throw 'Plan actions do not reflect the actual script carriers.' }

    $invalidJson = Join-Path $temp 'invalid-json.json'
    Set-Content -LiteralPath $invalidJson -Encoding UTF8 -Value '{ invalid'
    $invalidPlan = & dotnet $cli plan --spec $invalidJson | ConvertFrom-Json
    if ($invalidPlan.ok -or @($invalidPlan.issues | Where-Object code -eq 'REQUIREMENT_SCHEMA_INVALID').Count -ne 1) { throw 'Malformed JSON did not produce a stable Requirement validation issue.' }

    $explicitBoolSpec = @{
        schemaVersion='1.0'; task=@{name='explicit-bool-guard';mode='create';vmVersion='4.4.0'}
        scripts=@(@{
            id='explicit-bool';carrier='csharp-module';name='ExplicitBool';procedure='Procedure1'
            source='using Script.Methods; public partial class UserScript : ScriptMethods, IProcessMethods { public void Init() {} public bool Process() { Result = Enabled; return true; } }'
            execution=@{mode='once'};inputs=@(@{name='Enabled';type='bool';default=$false});outputs=@(@{name='Result';type='bool'});operations=@();dependencies=@()
        });connections=@()
    }
    $explicitBoolFile = Join-Path $temp 'explicit-bool.json'
    $explicitBoolSpec | ConvertTo-Json -Depth 20 | Set-Content -LiteralPath $explicitBoolFile -Encoding UTF8
    $explicitBoolPlan = & dotnet $cli plan --spec $explicitBoolFile | ConvertFrom-Json
    if ($explicitBoolPlan.ok -or @($explicitBoolPlan.issues | Where-Object code -eq 'BOOL_SOURCE_COMPATIBILITY_REQUIRED').Count -ne 1) { throw 'Explicit-source bool ports were not guarded.' }

    $base = Join-Path $temp 'business.sol'
    & (Join-Path $root 'scripts\materialize_script_base.ps1') -OutputFile $base | Out-Null
    $baseHash = (Get-FileHash $base -Algorithm SHA256).Hash
    $procedureName = ([string][char]0x6D41) + [char]0x7A0B + '1'
    $scriptName = ([string][char]0x811A) + [char]0x672C + '1'
    $replacementSpec = @{
        schemaVersion = '1.0'
        task = @{ name='audit-replace-script'; mode='patch'; vmVersion='4.4.0' }
        scripts = @(@{
            id='replace-shell'; carrier='csharp-module'; name=$scriptName; procedure=$procedureName
            execution=@{mode='once'}
            inputs=@(@{name='A';type='int';default=7})
            outputs=@(@{name='Result';type='int'})
            operations=@(@{kind='setOutput';parameter='Result';value=@{kind='input';name='A'}})
            dependencies=@()
        })
        connections=@()
    }
    $replacementFile = Join-Path $temp 'replacement.json'
    $replacementSpec | ConvertTo-Json -Depth 30 | Set-Content -LiteralPath $replacementFile -Encoding UTF8
    $patched = & dotnet $cli patch --base $base --spec $replacementFile --output (Join-Path $temp 'replace-output') | ConvertFrom-Json
    if (-not $patched.ok -or (Get-FileHash $base -Algorithm SHA256).Hash -ne $baseHash) { throw 'Replacing a script failed or modified the base SOL.' }
    $patchedParse = Get-Content -Raw (Join-Path $patched.taskDirectory 'validation\parse-result.json') | ConvertFrom-Json
    $modules = @($patchedParse.solution.procedures.modules)
    if ($modules.Count -ne 2 -or @($modules | Where-Object { $_.displayName -eq $scriptName -and $_.name -eq 'ShellModule' }).Count -ne 1) { throw 'Patch did not replace the existing script module in place.' }
    $shell = @($modules | Where-Object { $_.displayName -eq $scriptName })[0]
    $source = @($shell.binaryParams | Where-Object name -eq 'ShellContent')[0].parsed
    if ($source -notmatch 'Result = A') { throw 'Replacement source was not written to the existing module.' }
    if ('1 . %A% . 0 . 7 . 1 . 0 . All . 1' -notin @($shell.subscriptions.relationString)) { throw 'Replacement input default was not persisted.' }
    $report = Get-Content -Raw $patched.report
    foreach ($fragment in @('Compiler version:', 'Base SOL SHA-256:', 'Platform SDK root:', 'Algorithm SDK root:')) { if (-not $report.Contains($fragment)) { throw "Build report is missing audit field: $fragment" } }

    $mismatchSpec = Get-Content -Raw $replacementFile | ConvertFrom-Json
    $mismatchSpec.task | Add-Member -NotePropertyName baseSolution -NotePropertyValue (Join-Path $temp 'different.sol')
    $mismatchFile = Join-Path $temp 'mismatch.json'
    $mismatchSpec | ConvertTo-Json -Depth 30 | Set-Content -LiteralPath $mismatchFile -Encoding UTF8
    $old = $ErrorActionPreference; $ErrorActionPreference = 'Continue'
    $mismatchOutput = & dotnet $cli patch --base $base --spec $mismatchFile --output (Join-Path $temp 'mismatch-output') 2>&1 | Out-String
    $mismatchExit = $LASTEXITCODE; $ErrorActionPreference = $old
    if ($mismatchExit -eq 0 -or $mismatchOutput -notmatch 'BASE_SOLUTION_MISMATCH') { throw 'Conflicting Patch base paths were not blocked.' }
    $global:LASTEXITCODE = 0

    $validated = & dotnet $cli validate --file $patched.solution | ConvertFrom-Json
    if (-not $validated.ok -or [string]::IsNullOrWhiteSpace($validated.inspectOutput)) { throw 'Validate did not run both parser and inspect.' }

    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $forwardSlashSol = Join-Path $temp 'forward-slash.sol'
    $inputArchive = [IO.Compression.ZipFile]::OpenRead($patched.solution)
    $outputStream = [IO.File]::Open($forwardSlashSol, [IO.FileMode]::Create)
    $outputArchive = New-Object IO.Compression.ZipArchive($outputStream, [IO.Compression.ZipArchiveMode]::Create)
    try {
        foreach ($entry in $inputArchive.Entries) {
            $copy = $outputArchive.CreateEntry($entry.FullName.Replace('\','/'))
            $sourceStream = $entry.Open(); $targetStream = $copy.Open()
            try { $sourceStream.CopyTo($targetStream) } finally { $targetStream.Dispose(); $sourceStream.Dispose() }
        }
    }
    finally { $outputArchive.Dispose(); $outputStream.Dispose(); $inputArchive.Dispose() }
    $old = $ErrorActionPreference; $ErrorActionPreference = 'Continue'
    $invalidSolOutput = & dotnet $cli validate --file $forwardSlashSol 2>&1 | Out-String
    $invalidSolExit = $LASTEXITCODE; $ErrorActionPreference = $old
    if ($invalidSolExit -eq 0 -or $invalidSolOutput -notmatch 'SOL_ENTRY_NAME_INCOMPATIBLE') { throw 'Validate accepted VM-incompatible ZIP entry names.' }
    $global:LASTEXITCODE = 0

    $resourceRoot = Join-Path $root 'resources\vm\4.4.0'
    $manifest = Get-Content -Raw -Encoding UTF8 (Join-Path $resourceRoot 'manifest.json') | ConvertFrom-Json
    $tracked = @($manifest.hashes.psobject.Properties.Name)
    $untracked = @(Get-ChildItem $resourceRoot -Recurse -File | Where-Object Name -ne 'manifest.json' | ForEach-Object { $_.FullName.Substring($resourceRoot.Length + 1).Replace('\','/') } | Where-Object { $_ -notin $tracked })
    if ($untracked.Count -ne 0) { throw 'Resource manifest has untracked files: ' + ($untracked -join ', ') }
    if ((Get-ChildItem (Join-Path $root 'dist') -Recurse -File -Filter *.sol -ErrorAction SilentlyContinue | Measure-Object).Count -ne 0) { throw 'Published products contain SOL files.' }

    $scriptTutor = Get-Content -Raw -Encoding UTF8 (Join-Path $resourceRoot 'script-tutor-knowledge.json') | ConvertFrom-Json
    if ($scriptTutor.scope.skillName -ne 'vm-script-tutor' -or $scriptTutor.scope.skillVersion -ne '1.2') { throw 'Synchronized vm-script-tutor knowledge is invalid.' }
    [pscustomobject]@{ok=$true; accuratePlan=$true; stableInvalidJson=$true; explicitSourceBoolGuard=$true; patchReplace=$true; patchInputProtected=$true; patchBaseMismatchGuard=$true; completeReport=$true; validateParseInspectArchive=$true; resourceManifestComplete=$true; scriptTutorKnowledge=$true; distContainsNoSol=$true} | ConvertTo-Json
}
finally { if (Test-Path $temp) { Remove-Item $temp -Recurse -Force } }
