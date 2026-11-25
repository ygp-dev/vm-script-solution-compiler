param(
    [ValidateSet('Install', 'Uninstall')][string]$Action,
    [string]$LauncherSource,
    [string]$IconSource,
    [string]$ExtensionSource,
    [string]$VmApplications,
    [string]$StateFile,
    [switch]$DetectOnly,
    [switch]$Elevated
)

$ErrorActionPreference = 'Stop'
$productInfo = 'Tools\VMSC\vm-script-compiler-vm-launcher.exe'
$productName = 'VM Script Compiler AI 助手'

if ($Action -eq 'Uninstall' -and $StateFile -and -not (Test-Path -LiteralPath $StateFile -PathType Leaf)) {
    throw '缺少 VM 安装位置记录，无法安全移除按钮。'
}

function Find-VmApplications {
    $saved = if ($StateFile -and (Test-Path -LiteralPath $StateFile -PathType Leaf)) { (Get-Content -LiteralPath $StateFile -Raw -Encoding UTF8).Trim() } else { $null }
    $candidates = if ($VmApplications) { @($VmApplications) } elseif ($saved) { @($saved) } else { @($env:VISIONMASTER_HOME, $env:VM_HOME) }
    if (-not $VmApplications -and -not $saved) {
        foreach ($parent in @(${env:ProgramFiles}, ${env:ProgramFiles(x86)})) {
            if (-not $parent -or -not (Test-Path -LiteralPath $parent -PathType Container)) { continue }
            $candidates += Get-ChildItem -LiteralPath $parent -Directory -Filter 'VisionMaster4.4*' -ErrorAction SilentlyContinue | Select-Object -ExpandProperty FullName
        }
    }
    foreach ($candidate in $candidates | Where-Object { $_ } | Select-Object -Unique) {
        $applications = if (Test-Path -LiteralPath (Join-Path $candidate 'VisionMaster.exe') -PathType Leaf) { $candidate } else { Join-Path $candidate 'Applications' }
        if ((Test-Path -LiteralPath (Join-Path $applications 'VisionMaster.exe') -PathType Leaf) -and
            (Test-Path -LiteralPath (Join-Path $applications 'Resources\MenuItems.xml') -PathType Leaf)) {
            $version = (Get-Item -LiteralPath (Join-Path $applications 'VisionMaster.exe')).VersionInfo.ProductVersion
            if ($version -notmatch '^4\.4(?:\.|$)') { continue }
            return [IO.Path]::GetFullPath($applications)
        }
    }
    if ($Action -eq 'Install' -and -not $VmApplications -and -not $saved) {
        Add-Type -AssemblyName System.Windows.Forms
        $dialog = New-Object System.Windows.Forms.FolderBrowserDialog
        $dialog.Description = '请选择 VisionMaster 4.4 的 Applications 目录'
        if ($dialog.ShowDialog() -eq [System.Windows.Forms.DialogResult]::OK) {
            $selected = $dialog.SelectedPath
            if ((Split-Path $selected -Leaf) -ne 'Applications') { $selected = Join-Path $selected 'Applications' }
            $executable = Join-Path $selected 'VisionMaster.exe'
            if ((Test-Path -LiteralPath $executable -PathType Leaf) -and
                (Test-Path -LiteralPath (Join-Path $selected 'Resources\MenuItems.xml') -PathType Leaf) -and
                (Get-Item -LiteralPath $executable).VersionInfo.ProductVersion -match '^4\.4(?:\.|$)') {
                return [IO.Path]::GetFullPath($selected)
            }
        }
    }
    throw '未找到 VisionMaster 4.4 的 Applications 目录。请先安装 VM 4.4 Lite。'
}

function Add-TextElement([xml]$document, [System.Xml.XmlNode]$parent, [string]$name, [string]$value) {
    $element = $document.CreateElement($name)
    $element.InnerText = $value
    [void]$parent.AppendChild($element)
    return $element
}

$applications = Find-VmApplications
if ($DetectOnly) { Write-Output $applications; exit 0 }

if (-not $Elevated) {
    $identity = [Security.Principal.WindowsIdentity]::GetCurrent()
    $principal = [Security.Principal.WindowsPrincipal]::new($identity)
    if (-not $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
        $arguments = '-NoProfile -ExecutionPolicy Bypass -File "' + $PSCommandPath + '" -Action ' + $Action +
            ' -LauncherSource "' + $LauncherSource + '" -IconSource "' + $IconSource + '" -ExtensionSource "' + $ExtensionSource + '" -VmApplications "' + $VmApplications +
            '" -StateFile "' + $StateFile + '" -Elevated'
        $process = Start-Process -FilePath 'powershell.exe' -ArgumentList $arguments -Verb RunAs -WindowStyle Hidden -Wait -PassThru
        exit $process.ExitCode
    }
}

if ((Get-Process -Name VisionMaster -ErrorAction SilentlyContinue).Count -gt 0) {
    throw '请先关闭 VisionMaster，再安装或移除 VM 内的 AI 助手入口。'
}
$menu = Join-Path $applications 'Resources\MenuItems.xml'
$destination = Join-Path $applications 'Tools\VMSC'
$libraryConfig = Join-Path $applications 'myLibs\Librarys.Config'
$extensionDestination = Join-Path $applications 'myLibs\VmScriptCompiler.VmExtension.dll'
$extensionEntry = 'myLibs//VmScriptCompiler.VmExtension.dll'
if (-not (Test-Path -LiteralPath $libraryConfig -PathType Leaf)) { throw 'VM 缺少程序集加载配置 Librarys.Config。' }
[xml]$document = Get-Content -LiteralPath $menu -Raw -Encoding UTF8
$items = $document.SelectSingleNode('/TitleMenueItemData/items/TitleMenueItemData[isDisplayInMenu="false"]/items')
if ($null -eq $items) { throw 'VM 菜单结构不兼容：未找到顶部工具栏节点。' }

$existing = @($items.ChildNodes | Where-Object {
    $_.NodeType -eq [System.Xml.XmlNodeType]::Element -and
    $null -ne $_.SelectSingleNode('info') -and
    $_.SelectSingleNode('info').InnerText -eq $productInfo
})
foreach ($item in $existing) { [void]$items.RemoveChild($item) }

if ($Action -eq 'Install') {
    if (-not (Test-Path -LiteralPath $LauncherSource -PathType Leaf) -or
        -not (Test-Path -LiteralPath $IconSource -PathType Leaf) -or
        -not (Test-Path -LiteralPath $ExtensionSource -PathType Leaf)) {
        throw '安装包缺少 VM 启动器、按钮图标或扩展程序集。'
    }
    New-Item -ItemType Directory -Path $destination -Force | Out-Null
    Copy-Item -LiteralPath $LauncherSource -Destination (Join-Path $destination 'vm-script-compiler-vm-launcher.exe') -Force
    Copy-Item -LiteralPath $IconSource -Destination (Join-Path $destination 'vm-script-compiler-icon.png') -Force
    Copy-Item -LiteralPath $ExtensionSource -Destination $extensionDestination -Force
    $item = $document.CreateElement('VmscTitleMenuItemData')
    [void](Add-TextElement $document $item 'name' $productName)
    [void](Add-TextElement $document $item 'info' $productInfo)
    [void](Add-TextElement $document $item 'initMethodName' 'CheckPath')
    [void](Add-TextElement $document $item 'methodName' 'OpenExe')
    $group = $document.CreateElement('toolGroupItemInfo')
    [void](Add-TextElement $document $group 'groupName' 'TopRight')
    [void](Add-TextElement $document $group 'priority' '2')
    [void](Add-TextElement $document $group 'isExtern' '1')
    [void]$item.AppendChild($group)
    [void](Add-TextElement $document $item 'imageEnablePath' 'Tools\VMSC\vm-script-compiler-icon.png')
    [void](Add-TextElement $document $item 'imageDisablePath' 'Tools\VMSC\vm-script-compiler-icon.png')
    [void](Add-TextElement $document $item 'customItemTemplateName' 'vmscAssistantTemplate')
    [void]$items.AppendChild($item)
    if ($StateFile) { [IO.File]::WriteAllText($StateFile, $applications, [Text.UTF8Encoding]::new($true)) }
}

$libraryLines = @(Get-Content -LiteralPath $libraryConfig -Encoding UTF8)
if ($Action -eq 'Install' -and $libraryLines -notcontains $extensionEntry) {
    [IO.File]::AppendAllText($libraryConfig, [Environment]::NewLine + $extensionEntry + [Environment]::NewLine, [Text.Encoding]::UTF8)
}
if ($Action -eq 'Uninstall' -and $libraryLines -contains $extensionEntry) {
    $libraryLines = @($libraryLines | Where-Object { $_ -ne $extensionEntry })
    [IO.File]::WriteAllLines($libraryConfig, $libraryLines, [Text.Encoding]::UTF8)
}

$backup = Join-Path $applications 'Resources\MenuItems.vmsc-backup.xml'
if (-not (Test-Path -LiteralPath $backup)) { Copy-Item -LiteralPath $menu -Destination $backup }
$temporary = $menu + '.vmsc.tmp'
try {
    $document.Save($temporary)
    Move-Item -LiteralPath $temporary -Destination $menu -Force
} finally {
    if (Test-Path -LiteralPath $temporary) { Remove-Item -LiteralPath $temporary -Force }
}

if ($Action -eq 'Uninstall') {
    Remove-Item -LiteralPath (Join-Path $destination 'vm-script-compiler-vm-launcher.exe') -ErrorAction SilentlyContinue
    Remove-Item -LiteralPath (Join-Path $destination 'vm-script-compiler-icon.png') -ErrorAction SilentlyContinue
    Remove-Item -LiteralPath $extensionDestination -ErrorAction SilentlyContinue
    if ((Test-Path -LiteralPath $destination) -and @(Get-ChildItem -LiteralPath $destination -Force).Count -eq 0) {
        Remove-Item -LiteralPath $destination
    }
    if ($StateFile -and (Test-Path -LiteralPath $StateFile)) { Remove-Item -LiteralPath $StateFile }
}

[xml]$verified = Get-Content -LiteralPath $menu -Raw -Encoding UTF8
$matches = @($verified.SelectNodes('//VmscTitleMenuItemData[info="Tools\VMSC\vm-script-compiler-vm-launcher.exe"]'))
if ($Action -eq 'Install' -and $matches.Count -ne 1) { throw 'VM 按钮写入后校验失败。' }
if ($Action -eq 'Uninstall' -and $matches.Count -ne 0) { throw 'VM 按钮移除后校验失败。' }
if ($Action -eq 'Install' -and @(Get-Content -LiteralPath $libraryConfig -Encoding UTF8 | Where-Object { $_ -eq $extensionEntry }).Count -ne 1) {
    throw 'VM 扩展程序集注册校验失败。'
}
Write-Output "VM integration $Action succeeded: $applications"
