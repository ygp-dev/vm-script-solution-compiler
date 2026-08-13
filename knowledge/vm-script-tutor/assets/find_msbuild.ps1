<#
.SYNOPSIS
    查找当前系统中可用的 MSBuild.exe 路径（优先返回最新版本）
.DESCRIPTION
    通过 vswhere、注册表、常见安装目录等多种方式定位 MSBuild.exe，
    返回最高版本的 MSBuild 完整路径。不执行任何编译操作。
.OUTPUTS
    输出 MSBuild.exe 的完整路径字符串。如果未找到，则输出错误信息并返回退出码 1。
.EXAMPLE
    .\find_msbuild.ps1
    输出：C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe
#>

function Get-MSBuildPath {
    $candidates = @()

    # 方法1：vswhere (Visual Studio 2017+)
    $vswhere = "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe"
    if (Test-Path $vswhere) {
        $output = & $vswhere -latest -prerelease -products * -requires Microsoft.Component.MSBuild -find MSBuild\**\Bin\MSBuild.exe
        if ($output) {
            $candidates += @($output)[0]  # 强制数组语义，避免单行输出时取到首字符
        }
    }

    # 方法2：注册表 ToolsVersions (4.0, 14.0, 15.0, 17.0 等)
    $toolsVersions = @("4.0", "14.0", "15.0", "17.0")
    foreach ($ver in $toolsVersions) {
        $regPath = "HKLM:\SOFTWARE\Microsoft\MSBuild\ToolsVersions\$ver"
        if (Test-Path $regPath) {
            $toolsPath = (Get-ItemProperty -Path $regPath -Name MSBuildToolsPath -ErrorAction SilentlyContinue).MSBuildToolsPath
            if ($toolsPath -and (Test-Path "$toolsPath\MSBuild.exe")) {
                $candidates += "$toolsPath\MSBuild.exe"
            }
        }
    }

    # 方法3：常见 VS 安装目录 (扩展至更多版本)
    $patterns = @(
        "${env:ProgramFiles}\Microsoft Visual Studio\2022\*\MSBuild\Current\Bin\MSBuild.exe",
        "${env:ProgramFiles(x86)}\Microsoft Visual Studio\2019\*\MSBuild\Current\Bin\MSBuild.exe",
        "${env:ProgramFiles(x86)}\Microsoft Visual Studio\2017\*\MSBuild\15.0\Bin\MSBuild.exe",
        "${env:ProgramFiles(x86)}\MSBuild\14.0\Bin\MSBuild.exe",
        "${env:ProgramFiles(x86)}\MSBuild\12.0\Bin\MSBuild.exe"
    )
    foreach ($pattern in $patterns) {
        $found = Get-ChildItem $pattern -ErrorAction SilentlyContinue
        if ($found) {
            $candidates += $found[0].FullName
        }
    }

    # 方法4：PATH 环境变量
    $whereMsbuild = Get-Command msbuild -ErrorAction SilentlyContinue
    if ($whereMsbuild) {
        $candidates += $whereMsbuild.Source
    }

    # 去重并验证存在性
    $candidates = $candidates | Select-Object -Unique | Where-Object { Test-Path $_ }

    if ($candidates.Count -eq 0) {
        Write-Error "未找到 MSBuild.exe。请安装 Visual Studio 或 Build Tools。"
        exit 1
    }

    # 获取每个候选路径的版本号，选择最高的
    $versioned = @()
    foreach ($path in $candidates) {
        # 捕获版本输出，例如 "MSBuild 版本 17.8.3.12345"
        $verOutput = & $path -version 2>&1 | Out-String
        if ($verOutput -match '(\d+\.\d+\.\d+\.\d+)') {
            $version = [Version]$matches[1]
        }
        else {
            $version = [Version]'0.0.0.0'
        }
        $versioned += [PSCustomObject]@{ Path = $path; Version = $version }
    }

    $best = $versioned | Sort-Object Version -Descending | Select-Object -First 1
    return $best.Path
}

# 主逻辑：输出路径到 stdout
$msbuildPath = Get-MSBuildPath
if ($msbuildPath) {
    Write-Output $msbuildPath
    exit 0
}
else {
    exit 1
}