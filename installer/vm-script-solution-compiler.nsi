Unicode True
ManifestDPIAware True
ManifestSupportedOS all
RequestExecutionLevel user
SetCompressor /SOLID lzma
; makensis is a 32-bit process. A solid LZMA pass over the ~365 MB payload (self-contained .NET for
; three products, the pinned Node runtime and the agent node_modules) exhausts its address space at a
; 64 MB dictionary and aborts with "deflateInit() failed". 32 MB compresses this payload nearly as well
; and leaves headroom as the agent dependency tree grows.
SetCompressorDictSize 32

!include "MUI2.nsh"
!include "LogicLib.nsh"
!include "FileFunc.nsh"

!ifndef VERSION
  !define VERSION "1.0.8"
!endif
!ifndef VERSION4
  !define VERSION4 "1.0.8.0"
!endif
!ifndef DISTROOT
  !error "DISTROOT must point to the validated dist directory."
!endif
!ifndef OUTFILE
  !define OUTFILE "VM-Script-Solution-Compiler-Setup-${VERSION}-x64.exe"
!endif
!ifndef INSTALLSIZEKB
  !define INSTALLSIZEKB 0
!endif

!define PRODUCT_NAME "VM Script Solution Compiler"
!define PRODUCT_PUBLISHER "ygp-dev"
!define PRODUCT_WEB_SITE "https://github.com/ygp-dev/vm-script-solution-compiler"
!define PRODUCT_EXE "vm-script-compiler-desktop.exe"
!define UNINSTALL_KEY "Software\Microsoft\Windows\CurrentVersion\Uninstall\VM Script Solution Compiler"
!define APP_PATHS_KEY "Software\Microsoft\Windows\CurrentVersion\App Paths"

Name "${PRODUCT_NAME}"
Caption "${PRODUCT_NAME} ${VERSION}"
BrandingText "${PRODUCT_NAME}"
OutFile "${OUTFILE}"
InstallDir "$LOCALAPPDATA\VMSC"
InstallDirRegKey HKCU "Software\ygp-dev\VM Script Solution Compiler" "InstallLocation"
ShowInstDetails show
ShowUninstDetails show

VIProductVersion "${VERSION4}"
VIAddVersionKey /LANG=2052 "ProductName" "${PRODUCT_NAME}"
VIAddVersionKey /LANG=2052 "CompanyName" "${PRODUCT_PUBLISHER}"
VIAddVersionKey /LANG=2052 "FileDescription" "${PRODUCT_NAME} Windows x64 安装程序"
VIAddVersionKey /LANG=2052 "FileVersion" "${VERSION}"
VIAddVersionKey /LANG=2052 "ProductVersion" "${VERSION}"
VIAddVersionKey /LANG=2052 "LegalCopyright" "Copyright (c) ${PRODUCT_PUBLISHER}"

!define MUI_ABORTWARNING
!define MUI_ICON "${__FILEDIR__}\..\assets\branding\vm-script-compiler.ico"
!define MUI_UNICON "${__FILEDIR__}\..\assets\branding\vm-script-compiler.ico"
!define MUI_FINISHPAGE_RUN "$INSTDIR\Desktop\${PRODUCT_EXE}"
!define MUI_FINISHPAGE_RUN_TEXT "启动 ${PRODUCT_NAME}"
!define MUI_FINISHPAGE_LINK "打开 GitHub 项目主页"
!define MUI_FINISHPAGE_LINK_LOCATION "${PRODUCT_WEB_SITE}"

!insertmacro MUI_PAGE_WELCOME
!insertmacro MUI_PAGE_DIRECTORY
!insertmacro MUI_PAGE_COMPONENTS
!insertmacro MUI_PAGE_INSTFILES
!insertmacro MUI_PAGE_FINISH

!insertmacro MUI_UNPAGE_CONFIRM
!insertmacro MUI_UNPAGE_INSTFILES
!insertmacro MUI_UNPAGE_FINISH

!insertmacro MUI_LANGUAGE "SimpChinese"
!insertmacro MUI_LANGUAGE "English"

Function .onInit
  FindWindow $0 "" "VM Script Solution Agent"
  ${If} $0 != 0
    IfSilent silent_running interactive_running
    interactive_running:
      MessageBox MB_ICONEXCLAMATION|MB_OK "请先关闭正在运行的 VM Script Solution Agent，再重新安装。"
      Abort
    silent_running:
      SetErrorLevel 2
      Abort
  ${EndIf}
FunctionEnd

Section "完整产品" SEC_CORE
  SectionIn RO
  SetShellVarContext current
  SetOverwrite on

  SetOutPath "$INSTDIR\Desktop"
  File /r "${DISTROOT}\Desktop\*"

  SetOutPath "$INSTDIR\Cli"
  File /r "${DISTROOT}\Cli\*"

  SetOutPath "$INSTDIR\Mcp"
  File /r "${DISTROOT}\Mcp\*"

  IfFileExists "$INSTDIR\Cli\VmScriptCompiler.Cli.exe" payload_cli_ok payload_missing
payload_cli_ok:
  IfFileExists "$INSTDIR\Mcp\vm-script-compiler-mcp.exe" payload_mcp_ok payload_missing
payload_mcp_ok:
  Goto payload_verified
payload_missing:
  MessageBox MB_ICONSTOP|MB_OK "安装包缺少 CLI 或 MCP 组件，安装已中止。请重新下载完整安装包。"
  Abort
payload_verified:

  SetOutPath "$INSTDIR"
  File "${DISTROOT}\release-manifest.json"
  WriteUninstaller "$INSTDIR\Uninstall.exe"

  CreateDirectory "$SMPROGRAMS\VM Script Solution Compiler"
  CreateShortcut "$SMPROGRAMS\VM Script Solution Compiler\VM Script Solution Compiler.lnk" \
    "$INSTDIR\Desktop\${PRODUCT_EXE}" "" "$INSTDIR\Desktop\${PRODUCT_EXE}" 0
  CreateShortcut "$SMPROGRAMS\VM Script Solution Compiler\VM Script Solution Compiler CLI.lnk" \
    "$INSTDIR\Cli\VmScriptCompiler.Cli.exe" "" "$INSTDIR\Cli\VmScriptCompiler.Cli.exe" 0
  CreateShortcut "$SMPROGRAMS\VM Script Solution Compiler\VM Script Solution Compiler MCP.lnk" \
    "$INSTDIR\Mcp\vm-script-compiler-mcp.exe" "" "$INSTDIR\Mcp\vm-script-compiler-mcp.exe" 0
  CreateShortcut "$SMPROGRAMS\VM Script Solution Compiler\安装目录.lnk" "$INSTDIR"
  CreateShortcut "$SMPROGRAMS\VM Script Solution Compiler\使用说明.lnk" "$INSTDIR\Desktop\USAGE.md"
  CreateShortcut "$SMPROGRAMS\VM Script Solution Compiler\卸载.lnk" "$INSTDIR\Uninstall.exe"
  CreateShortcut "$DESKTOP\VM Script Solution Compiler.lnk" \
    "$INSTDIR\Desktop\${PRODUCT_EXE}" "" "$INSTDIR\Desktop\${PRODUCT_EXE}" 0

  WriteRegStr HKCU "Software\ygp-dev\VM Script Solution Compiler" "InstallLocation" "$INSTDIR"
  WriteRegStr HKCU "Software\ygp-dev\VM Script Solution Compiler" "Version" "${VERSION}"

  WriteRegStr HKCU "${APP_PATHS_KEY}\vm-script-compiler-desktop.exe" "" "$INSTDIR\Desktop\${PRODUCT_EXE}"
  WriteRegStr HKCU "${APP_PATHS_KEY}\vm-script-compiler-desktop.exe" "Path" "$INSTDIR\Desktop"
  WriteRegStr HKCU "${APP_PATHS_KEY}\VmScriptCompiler.Cli.exe" "" "$INSTDIR\Cli\VmScriptCompiler.Cli.exe"
  WriteRegStr HKCU "${APP_PATHS_KEY}\VmScriptCompiler.Cli.exe" "Path" "$INSTDIR\Cli"
  WriteRegStr HKCU "${APP_PATHS_KEY}\vm-script-compiler-mcp.exe" "" "$INSTDIR\Mcp\vm-script-compiler-mcp.exe"
  WriteRegStr HKCU "${APP_PATHS_KEY}\vm-script-compiler-mcp.exe" "Path" "$INSTDIR\Mcp"

  WriteRegStr HKCU "${UNINSTALL_KEY}" "DisplayName" "${PRODUCT_NAME}"
  WriteRegStr HKCU "${UNINSTALL_KEY}" "DisplayVersion" "${VERSION}"
  WriteRegStr HKCU "${UNINSTALL_KEY}" "Publisher" "${PRODUCT_PUBLISHER}"
  WriteRegStr HKCU "${UNINSTALL_KEY}" "URLInfoAbout" "${PRODUCT_WEB_SITE}"
  WriteRegStr HKCU "${UNINSTALL_KEY}" "DisplayIcon" "$INSTDIR\Desktop\${PRODUCT_EXE}"
  WriteRegStr HKCU "${UNINSTALL_KEY}" "InstallLocation" "$INSTDIR"
  WriteRegStr HKCU "${UNINSTALL_KEY}" "UninstallString" '"$INSTDIR\Uninstall.exe"'
  WriteRegStr HKCU "${UNINSTALL_KEY}" "QuietUninstallString" '"$INSTDIR\Uninstall.exe" /S'
  WriteRegDWORD HKCU "${UNINSTALL_KEY}" "NoModify" 1
  WriteRegDWORD HKCU "${UNINSTALL_KEY}" "NoRepair" 1
  WriteRegDWORD HKCU "${UNINSTALL_KEY}" "EstimatedSize" ${INSTALLSIZEKB}
SectionEnd

Section /o "安装到 VisionMaster 4.4 Lite（添加 AI 助手按钮）" SEC_VM
  SetOutPath "$INSTDIR\VmIntegration"
  File "${DISTROOT}\VmIntegration\vm-script-compiler-vm-launcher.exe"
  File "${DISTROOT}\VmIntegration\vm-script-compiler-icon.png"
  File "${DISTROOT}\VmIntegration\VmScriptCompiler.VmExtension.dll"
  File "${DISTROOT}\VmIntegration\vm-integration.ps1"
  nsExec::ExecToLog '"$SYSDIR\WindowsPowerShell\v1.0\powershell.exe" -NoProfile -ExecutionPolicy Bypass -File "$INSTDIR\VmIntegration\vm-integration.ps1" -Action Install -LauncherSource "$INSTDIR\VmIntegration\vm-script-compiler-vm-launcher.exe" -IconSource "$INSTDIR\VmIntegration\vm-script-compiler-icon.png" -ExtensionSource "$INSTDIR\VmIntegration\VmScriptCompiler.VmExtension.dll" -StateFile "$INSTDIR\VmIntegration\vm-applications.txt"'
  Pop $0
  ${If} $0 != 0
    MessageBox MB_ICONSTOP|MB_OK "VisionMaster 集成失败。请确认已安装 VM 4.4 Lite、关闭 VM，并同意管理员权限请求。"
    Abort
  ${EndIf}
  WriteRegDWORD HKCU "Software\ygp-dev\VM Script Solution Compiler" "VmIntegrationInstalled" 1
SectionEnd

Section "-同步 VM 安装选项" SEC_VM_SYNC
  SectionGetFlags ${SEC_VM} $0
  IntOp $0 $0 & 1
  ${If} $0 == 0
    ReadRegDWORD $0 HKCU "Software\ygp-dev\VM Script Solution Compiler" "VmIntegrationInstalled"
    ${If} $0 == 1
      nsExec::ExecToLog '"$SYSDIR\WindowsPowerShell\v1.0\powershell.exe" -NoProfile -ExecutionPolicy Bypass -File "$INSTDIR\VmIntegration\vm-integration.ps1" -Action Uninstall -StateFile "$INSTDIR\VmIntegration\vm-applications.txt"'
      Pop $0
      ${If} $0 != 0
        MessageBox MB_ICONSTOP|MB_OK "移除已有 VisionMaster 按钮失败。请关闭 VM 后重新安装。"
        Abort
      ${EndIf}
      DeleteRegValue HKCU "Software\ygp-dev\VM Script Solution Compiler" "VmIntegrationInstalled"
    ${EndIf}
  ${EndIf}
SectionEnd

Section "Uninstall"
  SetShellVarContext current
  ReadRegDWORD $0 HKCU "Software\ygp-dev\VM Script Solution Compiler" "VmIntegrationInstalled"
  ${If} $0 == 1
    nsExec::ExecToLog '"$SYSDIR\WindowsPowerShell\v1.0\powershell.exe" -NoProfile -ExecutionPolicy Bypass -File "$INSTDIR\VmIntegration\vm-integration.ps1" -Action Uninstall -StateFile "$INSTDIR\VmIntegration\vm-applications.txt"'
    Pop $0
    ${If} $0 != 0
      MessageBox MB_ICONSTOP|MB_OK "移除 VisionMaster 按钮失败。请先关闭 VM，再重新卸载。"
      Abort
    ${EndIf}
  ${EndIf}
  Delete "$DESKTOP\VM Script Solution Compiler.lnk"
  RMDir /r "$SMPROGRAMS\VM Script Solution Compiler"

  DeleteRegKey HKCU "${APP_PATHS_KEY}\vm-script-compiler-desktop.exe"
  DeleteRegKey HKCU "${APP_PATHS_KEY}\VmScriptCompiler.Cli.exe"
  DeleteRegKey HKCU "${APP_PATHS_KEY}\vm-script-compiler-mcp.exe"
  DeleteRegKey HKCU "${UNINSTALL_KEY}"
  DeleteRegKey HKCU "Software\ygp-dev\VM Script Solution Compiler"

  RMDir /r "$INSTDIR"
SectionEnd
