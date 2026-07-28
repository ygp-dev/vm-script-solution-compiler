# MCP 实机验收第 1 轮

生成日期：2026-07-20。全部用例均通过本项目 MCP Server 的 `plan_solution` / `build_solution` / `patch_solution` / `validate_solution` 调用生成，没有直接调用 CLI 构建。

## 自动验证摘要

- MCP Server：1.0.0
- VM 环境：VisionMaster 4.4.0，GlobalScript 可用
- 6/6 构建成功
- 6/6 离线脚本预编译成功，失败脚本 0
- 6/6 parse/inspect 成功
- 6/6 MCP `validate_solution` 返回 `ok=true`

## VM 人工验收顺序

### 1. C# 标量、默认值与 bool 兼容

SOL：`results/01-csharp-scalars/20260720092638302-mcp-csharp-scalars/result.sol`

- 模块：`CSharp标量验证`
- 输入可见：`A=7`、`B=5`、`Enabled=1`
- 输出可见：`Sum`、`Passed`、`Message`
- 预编译无错误
- 单次运行预期：`Sum=12`、`Passed=1`、`Message=MCP CSharp OK`
- 保存、关闭、重开后默认值仍存在

### 2. Python 标量和数组默认值

SOL：`results/02-python-defaults/20260720092643679-mcp-python-defaults/result.sol`

- 模块：`Python默认值验证`
- 输入可见：`A=3`、`B=4`；VM 4.4 UI 不支持已验证的数组字面量默认值，`Values` 可显示为空
- 预编译无错误
- 单次运行预期：脚本的 None 回退使 `Sum=7`、`ValuesEcho=[1,2,3]`
- 不应出现 `NoneType` 错误

### 3. C# 复杂类型数据源绑定

SOL：`results/03-complex-bindings/20260720092647606-mcp-complex-bindings/result.sol`

- 模块：`复杂类型绑定验证`
- 输入/输出可见：Image、Point、RoiBox、Line、Rect、Ellipse
- 每个输入的数据源选择器应能看到对应 VM 对象，不应显示空类型
- 绑定上游对应数据后，输出 Echo 应能被下游数据源选择器看到
- 本例不包含 Circle 端口；VM 4.4 UI 没有可选 Circle 脚本端口

### 4. 三类脚本与依赖

SOL：`results/04-all-carriers-dependencies/20260720092651780-mcp-all-carriers-dependencies/result.sol`

- GlobalScript 可正常编译
- `CSharp依赖验证` 可编译，运行后 `ColorName=Lime`
- `Python依赖验证` 可编译，运行后 `JsonText` 包含 `{"ok": true}`
- C# 引用中可看到 `System.Drawing.dll`
- Python 使用内置 `json` 包，不需要外部部署

### 5. Patch 真实业务 SOL

SOL：`results/05-patch-ffa/20260720092657582-mcp-patch-ffa/result.sol`

- 原始基底：`D:\User\Desktop\1\ffa.sol`
- 原有 11 个业务模块应保留
- 新增模块：`MCP注入验证`
- 输入 `Value` 默认值为 11
- 单次运行预期：`Doubled=22`
- 保存重开后原业务模块、新脚本和默认值都存在

### 6. netDxf 外部 DLL

SOL：`results/06-netdxf/20260720092707716-netdxf-load-example/result.sol`

- 模块：`netDxf读取示例`
- 脚本引用列表中可见 `netDxf.dll`，引用类型为 4
- 给 `DxfPath` 填入本机存在的 DXF 文件路径
- 预编译无错误
- 读取成功预期：`Loaded=1`
- 路径不存在时预期：`Loaded=0`，脚本日志显示异常

## 反馈格式

回复例如：

```text
1 通过
2 失败：Values 默认值为空
3 失败：Point 能看到端口，但数据源列表为空
4 通过
5 通过
6 失败：编译器报错全文...
```
