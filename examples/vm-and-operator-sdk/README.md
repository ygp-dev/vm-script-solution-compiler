# VM 二次开发 DLL 与算子 DLL 示例

运行：

```powershell
vm-script-compiler build --spec examples\vm-and-operator-sdk\requirement.json --output outputs\vm-and-operator-sdk
```

本例实际在脚本源码中访问两类 SDK 类型：

- `VM.Core.dll`：VM 二次开发 SDK，`role: vm-sdk`，VM `ShellRefrences` 类型为 `6`；
- `MVDPositionFix.Net.dll`：MVD 算子 SDK，`role: operator-sdk`，VM `ShellRefrences` 类型为 `4`。

已列入项目验证目录的 DLL 不需要填写 `path` 或 `referenceType`；编译器按本机 VM 4.4 的 ShellModule 运行目录解析，并把真实角色、引用类型、版本、架构和哈希写入 `validation/dependency-manifest.json`。

自研算子 DLL 尚未进入验证目录时，应提供真实文件：

```json
{
  "kind": "dotnet-assembly",
  "name": "MyOperator.Net.dll",
  "role": "operator-sdk",
  "path": "dll/MyOperator.Net.dll",
  "architecture": "x64",
  "referenceType": 4
}
```

VM 官方二次开发 DLL 不允许伪装成外部类型 4，也不允许由构建产物覆盖安装。它必须来自已安装 VM 的运行目录且使用项目内已验证的类型 6 映射。
