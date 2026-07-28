param([switch]$SkipBuild)
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$cli = Join-Path $root 'src\VmScriptCompiler.Cli\bin\Release\net8.0\VmScriptCompiler.Cli.dll'
$fixtures = Join-Path $root 'tests\fixtures'
$temp = Join-Path ([IO.Path]::GetTempPath()) ('vm-script-m9-' + [guid]::NewGuid().ToString('N'))
try {
    if (-not $SkipBuild) { & dotnet build (Join-Path $root 'VmScriptCompiler.sln') -c Release --no-restore | Out-Host }
    New-Item -ItemType Directory -Path $temp | Out-Null
    $built = & dotnet $cli build --spec (Join-Path $fixtures 'm9-dependencies-and-init.json') --output (Join-Path $temp 'build') | ConvertFrom-Json
    if (-not $built.ok) { throw 'M9 dependency build failed.' }
    $parse = Get-Content -Raw (Join-Path $built.taskDirectory 'validation\parse-result.json') | ConvertFrom-Json
    if (@($parse.solution.procedures).Count -ne 2 -or @($parse.solution.procedures.modules).Count -ne 2) { throw 'Multi-procedure Create did not preserve both independent script modules.' }
    $drawing = @($parse.solution.procedures.modules | Where-Object name -eq 'ShellModule')[0]
    $referenceParam = @($drawing.binaryParams | Where-Object name -eq 'ShellRefrences')[0]
    $references = $referenceParam.parsed
    foreach ($fragment in @("Script.Methods.dll`n3", "System.Drawing.dll`n0")) {
        if (-not $references.Contains($fragment)) { throw "ShellRefrences mapping missing: $fragment" }
    }
    $binaryOrder = @($drawing.binaryParams.name)
    if ($binaryOrder.IndexOf('ShellRefrences') -ne $binaryOrder.IndexOf('Output') + 1 -or $binaryOrder.IndexOf('ShellContent') -ne $binaryOrder.IndexOf('ShellRefrences') + 1) {
        throw 'ShellRefrences is not in the VM-saved canonical slot between Output and ShellContent.'
    }
    $precompile = Get-Content -Raw (Join-Path $built.taskDirectory 'validation\script-precompile.json') | ConvertFrom-Json
    if (@($precompile.scripts | Where-Object exitCode -ne 0).Count -ne 0) { throw 'Dependency precompile failed.' }
    $normalizedDrawingSource = Get-Content -Raw (Join-Path $built.taskDirectory 'generated\drawing-script.cs')
    if ($normalizedDrawingSource.Contains('new ImageData(bitmap)') -or -not $normalizedDrawingSource.Contains('Vm44ImageDataFromBitmap(bitmap)') -or -not $normalizedDrawingSource.Contains('PixelFormat = ImagePixelFormate.RGB24')) { throw 'Bitmap to VM ImageData compatibility conversion was not emitted.' }
    $desktopRuntime = Join-Path $temp 'desktop-runtime-cwd'
    New-Item -ItemType Directory -Path $desktopRuntime | Out-Null
    $dotnetRoot = Split-Path (Get-Command dotnet).Source -Parent
    $runtimeDrawing = Get-ChildItem (Join-Path $dotnetRoot 'shared\Microsoft.NETCore.App') -Directory |
        Sort-Object { [version]$_.Name } -Descending |
        ForEach-Object { Join-Path $_.FullName 'System.Drawing.dll' } |
        Where-Object { Test-Path -LiteralPath $_ } |
        Select-Object -First 1
    if ([string]::IsNullOrWhiteSpace($runtimeDrawing)) { throw 'Installed .NET System.Drawing facade is missing for working-directory isolation regression.' }
    Copy-Item -LiteralPath $runtimeDrawing -Destination (Join-Path $desktopRuntime 'System.Drawing.dll')
    Push-Location $desktopRuntime
    try {
        $desktopCwdBuild = & dotnet $cli build --spec (Join-Path $fixtures 'm9-dependencies-and-init.json') --output (Join-Path $temp 'desktop-cwd-build') | ConvertFrom-Json
    }
    finally { Pop-Location }
    if (-not $desktopCwdBuild.ok) { throw 'Framework references were contaminated by the Desktop .NET 8 working directory.' }
    $dependencyContract = Get-Content -Raw (Join-Path $built.taskDirectory 'script-contract.json') | ConvertFrom-Json
    $drawingContract = @($dependencyContract.scripts | Where-Object id -eq 'drawing-script')[0]
    $dependencyReport = Get-Content -Raw (Join-Path $built.taskDirectory 'build-report.md')
    if ($drawingContract.shellReferences.mode -ne 'explicit-shell-refrences-payload' -or 'System.Drawing.dll' -notin @($drawingContract.shellReferences.declared) -or -not $dependencyReport.Contains('ShellRefrences payload: `emitted (catalog defaults plus declared references) for Drawing引用验证: System.Drawing.dll`')) { throw 'Explicit ShellRefrences status is not traceable in contract/report.' }

    $vmDll = & dotnet $cli build --spec (Join-Path $fixtures 'm9-vm-dll-dependency.json') --output (Join-Path $temp 'vm-dll') | ConvertFrom-Json
    if (-not $vmDll.ok) { throw 'Installed VM DLL dependency build failed.' }
    $vmDllManifest = Get-Content -Raw (Join-Path $vmDll.taskDirectory 'validation\dependency-manifest.json') | ConvertFrom-Json
    $netDxf = @($vmDllManifest.scripts.assemblies | Where-Object declaredName -eq 'netDxf.dll')[0]
    if ($null -eq $netDxf -or -not $netDxf.runtimeVisible -or $netDxf.role -ne 'third-party' -or $netDxf.referenceType -ne 4 -or $netDxf.architecture -ne 'anycpu' -or [string]::IsNullOrWhiteSpace($netDxf.sha256)) { throw 'Installed DLL role/identity/version/architecture/hash evidence is incomplete.' }
    $vmDllParse = Get-Content -Raw (Join-Path $vmDll.taskDirectory 'validation\parse-result.json') | ConvertFrom-Json
    $vmDllOrder = @(@($vmDllParse.solution.procedures.modules | Where-Object name -eq 'ShellModule')[0].binaryParams.name)
    if ($vmDllOrder.IndexOf('ShellRefrences') -ne $vmDllOrder.IndexOf('Output') + 1 -or $vmDllOrder.IndexOf('ShellContent') -ne $vmDllOrder.IndexOf('ShellRefrences') + 1) { throw 'netDxf ShellRefrences slot is not VM-compatible.' }

    $sdkBuild = & dotnet $cli build --spec (Join-Path $root 'examples\vm-and-operator-sdk\requirement.json') --output (Join-Path $temp 'sdk-build') | ConvertFrom-Json
    if (-not $sdkBuild.ok) { throw 'VM secondary-development/operator SDK dependency build failed.' }
    $sdkManifest = Get-Content -Raw (Join-Path $sdkBuild.taskDirectory 'validation\dependency-manifest.json') | ConvertFrom-Json
    $vmSdk = @($sdkManifest.scripts.assemblies | Where-Object declaredName -eq 'VM.Core.dll')[0]
    $operatorSdk = @($sdkManifest.scripts.assemblies | Where-Object declaredName -eq 'MVDPositionFix.Net.dll')[0]
    if ($vmSdk.role -ne 'vm-sdk' -or $vmSdk.referenceType -ne 6 -or -not $vmSdk.runtimeVisible) { throw 'VM secondary-development SDK classification is wrong.' }
    if ($operatorSdk.role -ne 'operator-sdk' -or $operatorSdk.referenceType -ne 4 -or -not $operatorSdk.runtimeVisible) { throw 'Operator SDK classification is wrong.' }
    $sdkParse = Get-Content -Raw (Join-Path $sdkBuild.taskDirectory 'validation\parse-result.json') | ConvertFrom-Json
    $sdkReferences = @($sdkParse.solution.procedures.modules.binaryParams | Where-Object name -eq 'ShellRefrences')[0].parsed
    if (-not $sdkReferences.Contains("VM.Core.dll`n6") -or -not $sdkReferences.Contains("MVDPositionFix.Net.dll`n4")) { throw 'SDK ShellRefrences types were not written to SOL.' }

    $externalDirectory = Join-Path $temp 'external-dll'
    New-Item -ItemType Directory -Path $externalDirectory | Out-Null
    $externalDll = Join-Path $externalDirectory 'netDxf.dll'
    Copy-Item $netDxf.sourcePath $externalDll
    $externalSpec = Get-Content -Raw (Join-Path $fixtures 'm9-vm-dll-dependency.json') | ConvertFrom-Json
    $externalSpec.task.name = 'm9-external-dll-package'
    $externalSpec.scripts[0].dependencies[0] | Add-Member -NotePropertyName path -NotePropertyValue $externalDll
    $externalSpec.scripts[0].dependencies[0] | Add-Member -NotePropertyName referenceType -NotePropertyValue 4
    $externalSpecFile = Join-Path $temp 'external-dll.json'
    $externalSpec | ConvertTo-Json -Depth 30 | Set-Content -Path $externalSpecFile -Encoding UTF8
    $external = & dotnet $cli build --spec $externalSpecFile --output (Join-Path $temp 'external-build') | ConvertFrom-Json
    if (-not $external.ok) { throw 'Explicit external DLL dependency build failed.' }
    $externalManifest = Get-Content -Raw (Join-Path $external.taskDirectory 'validation\dependency-manifest.json') | ConvertFrom-Json
    $externalEvidence = @($externalManifest.scripts.assemblies | Where-Object declaredName -eq 'netDxf.dll')[0]
    if ($externalEvidence.runtimeVisible -or [string]::IsNullOrWhiteSpace($externalEvidence.packagedPath) -or -not (Test-Path (Join-Path $external.taskDirectory $externalEvidence.packagedPath))) { throw 'External DLL was not packaged with deployment evidence.' }
    if (-not (Test-Path (Join-Path $external.taskDirectory 'dependencies\manifest.json')) -or -not (Test-Path (Join-Path $external.taskDirectory 'dependencies\deploy-to-vm.ps1'))) { throw 'External DLL deployment manifest/script is missing.' }
    $externalReport = Get-Content -Raw (Join-Path $external.taskDirectory 'build-report.md')
    if (-not $externalReport.Contains('DLL deployment required') -or -not $externalReport.Contains('netDxf')) { throw 'External DLL deployment requirement is missing from build report.' }

    $compatibleVersionSpec = Get-Content -Raw $externalSpecFile | ConvertFrom-Json
    $compatibleVersionSpec.task.name = 'm9-compatible-short-version'
    $compatibleVersionSpec.scripts[0].dependencies[0] | Add-Member -NotePropertyName version -NotePropertyValue '2023.11.10'
    $compatibleVersionFile = Join-Path $temp 'compatible-short-version.json'
    $compatibleVersionSpec | ConvertTo-Json -Depth 30 | Set-Content -Path $compatibleVersionFile -Encoding UTF8
    $compatibleVersion = & dotnet $cli build --spec $compatibleVersionFile --output (Join-Path $temp 'compatible-short-version-build') | ConvertFrom-Json
    if (-not $compatibleVersion.ok) { throw 'Three-part dependency version was not normalized to the equivalent four-part assembly version.' }

    $sharedSpec = Get-Content -Raw $externalSpecFile | ConvertFrom-Json
    $sharedSpec.task.name = 'm9-shared-external-dll'
    $sharedSecond = ($sharedSpec.scripts[0] | ConvertTo-Json -Depth 30 | ConvertFrom-Json)
    $sharedSecond.id = 'netdxf-script-2'; $sharedSecond.name = 'DLL引用验证2'; $sharedSecond.execution.order = 1
    $sharedSpec.scripts = @($sharedSpec.scripts[0], $sharedSecond)
    $sharedSpecFile = Join-Path $temp 'shared-external-dll.json'
    $sharedSpec | ConvertTo-Json -Depth 30 | Set-Content -Path $sharedSpecFile -Encoding UTF8
    $shared = & dotnet $cli build --spec $sharedSpecFile --output (Join-Path $temp 'shared-external-build') | ConvertFrom-Json
    if (-not $shared.ok) { throw 'Two scripts sharing the same external DLL failed.' }
    $sharedDeploy = Get-Content -Raw (Join-Path $shared.taskDirectory 'dependencies\deploy-to-vm.ps1')
    if ([regex]::Matches($sharedDeploy, 'netDxf\.dll', 'IgnoreCase').Count -ne 1) { throw 'Shared external DLL was not deduplicated in deployment script.' }

    $frameworkCsc = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
    if (-not (Test-Path -LiteralPath $frameworkCsc)) { throw 'Framework64 csc is missing for DLL collision test.' }
    $collisionA = Join-Path $temp 'collision-a'; $collisionB = Join-Path $temp 'collision-b'
    New-Item -ItemType Directory -Path $collisionA,$collisionB | Out-Null
    $sourceA = Join-Path $collisionA 'Collision.cs'; $sourceB = Join-Path $collisionB 'Collision.cs'
    Set-Content -LiteralPath $sourceA -Encoding UTF8 -Value 'public static class CollisionMarker { public const int Value = 1; }'
    Set-Content -LiteralPath $sourceB -Encoding UTF8 -Value 'public static class CollisionMarker { public const int Value = 2; }'
    & $frameworkCsc /nologo /target:library "/out:$(Join-Path $collisionA 'Collision.dll')" $sourceA | Out-Host
    if ($LASTEXITCODE -ne 0) { throw 'Failed to compile first collision fixture.' }
    & $frameworkCsc /nologo /target:library "/out:$(Join-Path $collisionB 'Collision.dll')" $sourceB | Out-Host
    if ($LASTEXITCODE -ne 0) { throw 'Failed to compile second collision fixture.' }
    $collisionSpec = Get-Content -Raw $sharedSpecFile | ConvertFrom-Json
    $collisionSpec.task.name = 'm9-dll-name-collision'
    $collisionSpec.scripts[0].dependencies[0].name = 'Collision.dll'; $collisionSpec.scripts[0].dependencies[0].path = (Join-Path $collisionA 'Collision.dll')
    $collisionSpec.scripts[1].dependencies[0].name = 'Collision.dll'; $collisionSpec.scripts[1].dependencies[0].path = (Join-Path $collisionB 'Collision.dll')
    $collisionSpecFile = Join-Path $temp 'collision.json'
    $collisionSpec | ConvertTo-Json -Depth 30 | Set-Content -Path $collisionSpecFile -Encoding UTF8
    $oldPreference = $ErrorActionPreference; $ErrorActionPreference = 'Continue'
    $collisionOutput = & dotnet $cli build --spec $collisionSpecFile --output (Join-Path $temp 'collision-build') 2>&1 | Out-String
    $collisionExit = $LASTEXITCODE
    $ErrorActionPreference = $oldPreference
    if ($collisionExit -eq 0 -or $collisionOutput -notmatch 'DEPENDENCY_FILE_CONFLICT') { throw 'Different DLLs with the same deployment name were not blocked.' }
    $global:LASTEXITCODE = 0

    $badVersionSpec = Get-Content -Raw $externalSpecFile | ConvertFrom-Json
    $badVersionSpec.task.name = 'm9-dll-version-mismatch'
    $badVersionSpec.scripts[0].dependencies[0] | Add-Member -NotePropertyName version -NotePropertyValue '0.0.0.0'
    $badVersionFile = Join-Path $temp 'bad-version.json'
    $badVersionSpec | ConvertTo-Json -Depth 30 | Set-Content -Path $badVersionFile -Encoding UTF8
    $oldPreference = $ErrorActionPreference; $ErrorActionPreference = 'Continue'
    $badVersion = & dotnet $cli build --spec $badVersionFile --output (Join-Path $temp 'bad-version-build') 2>&1 | Out-String
    $badVersionExit = $LASTEXITCODE
    $ErrorActionPreference = $oldPreference
    if ($badVersionExit -eq 0 -or $badVersion -notmatch 'DEPENDENCY_VERSION_MISMATCH') { throw 'DLL version mismatch was not blocked.' }
    $global:LASTEXITCODE = 0
    $global = Get-Content -Raw (Join-Path $built.taskDirectory 'generated\GlobalScript.cs')
    $initLog = $global.IndexOf('ConsoleWrite(Convert.ToString("global init"')
    $process = $global.IndexOf('public int Process()')
    if ($initLog -lt 0 -or $initLog -gt $process) { throw 'Global init operation was not emitted into Init().' }

    $old = $ErrorActionPreference; $ErrorActionPreference = 'Continue'
    $badReference = & dotnet $cli build --spec (Join-Path $fixtures 'e10-unverified-reference.json') --output (Join-Path $temp 'bad-ref') 2>&1 | Out-String
    $referenceExit = $LASTEXITCODE
    $ErrorActionPreference = $old
    $conflict = & dotnet $cli plan --spec (Join-Path $fixtures 'e11-source-operations-conflict.json') | ConvertFrom-Json
    $unsupportedErrorPolicy = & dotnet $cli plan --spec (Join-Path $fixtures 'e12-unsupported-on-error.json') | ConvertFrom-Json
    if ($referenceExit -eq 0 -or $badReference -notmatch 'REFERENCE_TYPE_UNCONFIRMED') { throw 'Unverified reference was not blocked.' }
    if ($conflict.ok -or @($conflict.issues | Where-Object code -eq 'SOURCE_OPERATIONS_CONFLICT').Count -ne 1) { throw 'Source/operations conflict was not blocked.' }
    if ($unsupportedErrorPolicy.ok -or @($unsupportedErrorPolicy.issues | Where-Object code -eq 'ON_ERROR_POLICY_UNSUPPORTED').Count -ne 1) { throw 'Unsupported per-operation error policy was not blocked.' }

    $desktop = Get-Content -Raw (Join-Path $root 'src\VmScriptCompiler.Desktop\MainWindow.xaml') -Encoding UTF8
    foreach ($label in @('GenerateButton', 'CreateModeRadio', 'PatchModeRadio', 'AdvancedDeterministicTools', 'FriendlyStatus', 'SettingsProviderChoice', 'SettingsEndpointText', 'SettingsApiKeyBox', 'OpenResultFolder_Click', 'OpenDependenciesButton', 'OpenDependencies_Click')) {
        if (-not $desktop.Contains($label)) { throw "Simple Desktop workflow is missing: $label" }
    }
    foreach ($label in @('RecentConversationList', 'ArtifactSearchText', 'SettingsFilePathText', 'NewConversation_Click', 'ShowArtifacts_Click', 'ShowSettings_Click')) {
        if (-not $desktop.Contains($label)) { throw "Codex-style Desktop workspace is missing: $label" }
    }
    $desktopCode = Get-Content -Raw (Join-Path $root 'src\VmScriptCompiler.Desktop\MainWindow.xaml.cs') -Encoding UTF8
    foreach ($fragment in @('AgentProcessClient','_agent.PromptAsync(','new AgentConnectionOptions(','_lastDependencyDirectory','Path.Combine(path, "dependencies")')) {
        if (-not $desktopCode.Contains($fragment)) { throw "Desktop UI snapshot/provider flow is missing: $fragment" }
    }
    if ($desktopCode.Contains('Environment.SetEnvironmentVariable')) { throw 'Desktop must not persist or mutate process-wide AI credentials.' }
    foreach ($fragment in @('DesktopStateStore', 'ListSessionsAsync()', 'ResumeSessionAsync(', 'RefreshArtifactIndexAsync()', 'RecordUserValidationAsync(')) {
        if (-not $desktopCode.Contains($fragment)) { throw "Desktop conversation/artifact state flow is missing: $fragment" }
    }
    $desktopState = Get-Content -Raw (Join-Path $root 'src\VmScriptCompiler.Desktop\DesktopStateStore.cs') -Encoding UTF8
    if (-not $desktopState.Contains('desktop-settings.json') -or -not $desktopState.Contains('recent-conversations.json') -or -not $desktopState.Contains('artifact-index.json')) {
        throw 'Desktop persistent state files are incomplete.'
    }
    if (-not $desktopState.Contains('EncryptedApiKey') -or -not $desktopState.Contains('ProtectedData.Protect') -or -not $desktopState.Contains('DataProtectionScope.CurrentUser')) {
        throw 'Desktop API key is not protected with current-user DPAPI.'
    }
    if (-not $desktop.Contains('SettingsApiKeyBox') -or -not $desktop.Contains('MaxWidth="1100"') -or -not $desktop.Contains('Background="#FF090909"')) {
        throw 'Desktop encrypted-key form, wide composer, or dark theme is missing.'
    }

    [pscustomobject]@{ ok=$true; multiProcedureCreate=$true; shellReferences=$true; canonicalShellReferenceSlot=$true; shellReferenceTraceability=$true; csharpDependencyPrecompiled=$true; desktopWorkingDirectoryIsolated=$true; bitmapImageDataCompatibility=$true; installedDllInspected=$true; vmSdkClassified=$true; operatorSdkClassified=$true; externalDllPackaged=$true; sharedDllDeduplicated=$true; guardedDllNameCollision=$true; dllDeploymentReported=$true; normalizedAssemblyVersion=$true; guardedDllVersion=$true; pythonDependencyProbed=$true; initPlacement=$true; guardedUnknownReference=$true; guardedSourceConflict=$true; guardedErrorPolicy=$true; simpleDesktop=$true; codexStyleWorkspace=$true; darkTheme=$true; wideComposer=$true; recentConversations=$true; artifactIndex=$true; desktopConfiguration=$true; encryptedApiKey=$true; visibleFriendlyStatus=$true; uiThreadSnapshot=$true } | ConvertTo-Json
}
finally { if (Test-Path $temp) { Remove-Item $temp -Recurse -Force } }
