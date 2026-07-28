# DLL 引用校验与部署

## ShellModule 引用模型

C# ShellModule 的 DLL 引用由 Requirement 的 `dependencies` 声明，编译器把直接引用写入模块二进制参数 `ShellRefrences`。VM 4.4 已确认的类型为：

- `0`：系统程序集；
- `3`：`Script.Methods.dll`；
- `4`：用户/第三方托管程序集；
- `6`：VM 程序集。

已收录在 `resources/vm/4.4.0/shell-reference-catalog.json` 的程序集不需要重复填写 `referenceType`。项目外的自定义 DLL 必须同时给出 `path` 和 `referenceType: 4`，编译器不会猜测引用类型。

Requirement 还可声明面向用户的 `role`，编译器会与项目内实机 SOL 证据交叉校验：

- `system`：系统和脚本基础程序集，对应类型 0/3；
- `vm-sdk`：`VM*.dll`、`VMControls*.dll` 等 VM 二次开发 SDK，对应类型 6；
- `operator-sdk`：`MVD*.dll` 等算子/算法 SDK，对应类型 4；
- `third-party`：`netDxf.dll` 等第三方或普通自研托管库，对应类型 4。

不能只按文件名前缀盲猜。目录中的具体映射来自项目内已保存 SOL；声明角色与已验证角色不一致时返回 `DEPENDENCY_ROLE_MISMATCH`。VM SDK 必须从当前 VM 安装的运行目录加载，不能用外部同名文件打包覆盖；算子 SDK 和第三方库若来自外部路径，则进入显式部署包。

未声明任何 .NET DLL 时，编译器与 VM 正常新建脚本保持一致，不额外创建 `ShellRefrences` 参数；`mscorlib.dll`、`System.dll`、`System.Core.dll`、`System.Windows.Forms.dll` 和 `Script.Methods.dll` 由 VM 作为基础引用处理。只要声明了一个显式 DLL，写入的 `ShellRefrences` 就会同时包含这些基础项和全部声明项。构建契约中的 `shellReferences.mode` 与报告中的 `ShellRefrences payload` 会明确区分这两种情况。

```json
{
  "kind": "dotnet-assembly",
  "name": "MyAlgorithm.dll",
  "role": "operator-sdk",
  "path": "./libs/MyAlgorithm.dll",
  "version": "1.2.0.0",
  "architecture": "x64",
  "referenceType": 4
}
```

相对 `path` 以 Requirement JSON 所在目录为基准，而不是以 EXE 当前目录为基准。

## 构建时检查

编译器对直接 DLL 和非框架递归依赖执行以下检查：

- 文件名与程序集 Identity 名称一致；
- 声明版本与程序集版本一致；
- 文件是可供 C# 引用的托管程序集；
- x64 VM 不接受 x86 程序集；AnyCPU 可以在 x64 进程中使用；
- 显式外部 DLL 必须属于 CLR 4 `.NET Framework`，拒绝 .NET Core、.NET 5+ 等运行时目标；
- 外部 DLL 的非框架递归依赖必须能在同目录或 VM 已知运行目录中解析，并逐项检查 CLR metadata、目标框架、架构与版本；VM 安装内的厂商程序集记录直接引用图，但其私有依赖由 VM 自身应用探测管理，不会被编译器复制或覆盖；
- 多个脚本引用同名同哈希的外部 DLL 时，部署脚本只复制一次；同名但内容不同的 DLL 会以 `DEPENDENCY_FILE_CONFLICT` 阻断，避免部署时静默覆盖；
- 最终源码使用 Windows Framework64 `csc.exe` 和全部直接引用进行离线预编译；
- 最终 SOL 再读回 `ShellRefrences`，确认名称已写入。

程序集版本按四段式规范化后比较，缺失的 Build/Revision 段视为 `0`。例如 Requirement 的 `2023.11.10` 与程序集元数据 `2023.11.10.0` 等价；实际数字不同的版本仍返回 `DEPENDENCY_VERSION_MISMATCH`。

`ShellRefrences` 必须处于模块参数的规范槽位 `Output > ShellRefrences > ShellContent`。把它追加到参数列表末尾时，本地解析器虽然能够读回，VM 4.4 预编译器却不会加载对应程序集。该规则已由 VM 保存样本确认，并于 2026-07-20 通过 `System.Drawing` 与 `netDxf` 实机回归。

Desktop 是 .NET 8 自包含程序，但 VM 4.4 C# 脚本属于 .NET Framework CLR 4。离线预编译器将工作目录固定到 Framework64 编译器目录，并使用其中框架程序集的绝对路径，防止 Desktop 发布目录中的 `System.Drawing.dll` 门面程序集遮蔽 VM 所需的 Framework `System.Drawing.dll`。脚本应声明 `System.Drawing.dll`，不得声明 `System.Drawing.Common`。

VM 4.4 的 `Script.Methods.ImageData` 没有接收 `Bitmap` 的构造函数。正确载荷由无参 `ImageData` 的 `Buffer`、`Width`、`Height` 和 `PixelFormat` 组成。编译器会把显式源码中的简单 `new ImageData(bitmapVariable)` 机械转换为 24-bit Bitmap 到 VM `RGB24` 字节缓冲区的确定性转换，避免 AI 生成代码误用 .NET 图像对象构造方式。

每个程序集的版本、目标框架、架构、SHA-256、递归引用、来源和 VM 可见性写入 `validation/dependency-manifest.json`，同时进入 `script-contract.json` 和 `build-report.md`。

可直接构建 `examples/vm-and-operator-sdk/requirement.json`，其中源码会真实访问 `VM.Core.VmDataType` 与 `VisionDesigner.PositionFix.CPositionFixTool`，用于同时验证二次开发 DLL 和算子 DLL，而不是只把名称写进 SOL。

## 外部 DLL 部署

SOL 不嵌入第三方 DLL。若显式 `path` 不在 VM 运行目录，构建任务会生成：

```text
dependencies/
├─ <script-id>/
│  ├─ Direct.dll
│  └─ Transitive.dll
├─ manifest.json
└─ deploy-to-vm.ps1
```

编译器和 Desktop 不会自动修改 VisionMaster 安装目录。用户确认 DLL 来源和许可后，可显式以管理员权限运行 `deploy-to-vm.ps1`，把已校验文件复制到当前 VM 4.4 的 ShellModule DLL 目录。部署前未完成时，构建报告会明确列为 `DLL deployment required`，不能把离线预编译成功等同于 VM 运行时已可加载。

## Python 与 GlobalScript 边界

Python 包仍由 VM 自带 Python 的 `importlib.util.find_spec` 探测，不使用 `ShellRefrences`。GlobalScript 使用另一套 `ScriptRefences/refrencesType` 分类；当前只使用 VM 4.4 官方基线引用，未取得同版本证据的自定义 GlobalScript DLL 继续由 `REFERENCE_TYPE_UNCONFIRMED` 阻断。
