# netDxf 外部 DLL 使用示例

`requirement.json` 创建一个 C# ShellModule：输入 `DxfPath`，使用 `DxfDocument.Load` 读取文件，输出 `Loaded`。

当前 VM 4.4 已安装且项目 catalog 已验证 `netDxf.dll`，因此 Requirement 只需声明名称和架构：

```json
{
  "kind": "dotnet-assembly",
  "name": "netDxf.dll",
  "architecture": "anycpu"
}
```

编译器会把 VM 基础引用和 `netDxf.dll` 写入 `ShellRefrences`，并使用实际 DLL 做 Framework64 离线预编译。

对于 catalog 中没有、且尚未部署到 VM 的自定义 DLL，改为：

```json
{
  "kind": "dotnet-assembly",
  "name": "MyLibrary.dll",
  "path": ".\\libs\\MyLibrary.dll",
  "version": "1.2.0.0",
  "architecture": "x64",
  "referenceType": 4
}
```

相对路径以 Requirement 所在目录为准。构建会校验程序集身份、版本、框架、架构和递归依赖，并输出 `dependencies` 部署包；Desktop 中点击“打开 DLL 部署包”，确认后以管理员权限运行 `deploy-to-vm.ps1`。程序不会自动修改 VM 安装目录。

构建命令：

```powershell
dotnet .\src\VmScriptCompiler.Cli\bin\Release\net8.0\VmScriptCompiler.Cli.dll build `
  --spec .\examples\netdxf\requirement.json `
  --output .\outputs\netdxf-example
```
