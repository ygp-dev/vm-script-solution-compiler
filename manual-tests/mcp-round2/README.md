# MCP 实机复测第 2 轮

本轮根据第 1 轮实机结果生成，全部通过修复后的 MCP Server `build_solution` 和 `validate_solution`。

## 2. Python 数组运行时回退

SOL：`02-python-runtime-fallback/20260720104033794-mcp-python-defaults/result.sol`

- VM 4.4 实机格式没有已验证的“数组字面量 UI 默认值”编码，因此 `Values` 在 UI 中为空是预期现象。
- 生成的 Python 脚本在 `moduleVar.Values is None` 时使用 `[1,2,3]`。
- 请不绑定 `Values`，直接单次运行。
- 预期：`Sum=7`，`ValuesEcho=[1,2,3]`，不出现 `NoneType` 错误。

## 4. System.Drawing 引用顺序修复

SOL：`04-fixed-reference-order/20260720104036945-mcp-all-carriers-dependencies/result.sol`

- `ShellRefrences` 现位于 VM 实机保存的正确槽位：`Output > ShellRefrences > ShellContent`。
- 引用包含 `System.Drawing.dll:0`。
- 预期：`CSharp依赖验证` 预编译 0 错误，不再报 `Color` 类型找不到。
- 运行预期：`ColorName=Lime`。
- GlobalScript 和 Python 模块也应正常编译。

## 6. netDxf 引用顺序修复

SOL：`06-fixed-netdxf/20260720104040859-netdxf-load-example/result.sol`

- `ShellRefrences` 现位于 `Output` 和 `ShellContent` 之间。
- 引用包含 `netDxf.dll:4`。
- 预期：预编译 0 错误，不再报 `netDxf` 命名空间找不到。
- 给 `DxfPath` 填入有效 DXF 后，运行预期 `Loaded=1`。

## 回复格式

```text
2 运行通过 / 运行错误...
4 预编译通过 / 错误...
6 预编译通过，Loaded=... / 错误...
```

## 实机验收结果

2026-07-20，用户在 VisionMaster 4.4 实机中确认本轮用例全部通过：

- Python 数组运行时回退正常，不再出现 `NoneType` 错误，`Sum=7`、`ValuesEcho=[1,2,3]`；VM UI 中数组字面量默认值仍为空，这是当前已验证格式的能力边界。
- `System.Drawing` 引用生效，C# 预编译 0 错误，运行输出 `ColorName=Lime`。
- `netDxf` 引用生效，C# 预编译 0 错误；填写有效 DXF 路径后运行输出 `Loaded=1`。

结论：`ShellRefrences` 规范槽位修复和 Python 数组运行时默认值回退均已通过 VM 4.4 实机验收。
