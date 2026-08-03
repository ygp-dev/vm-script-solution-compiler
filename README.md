# VM Script Solution Compiler

面向 VisionMaster 4.4 的确定性脚本方案编译器。自然语言由 Agent 转换为 Requirement IR；只有 Compiler Core 可以生成或 Patch SOL，AI 不直接修改二进制。

## 项目

- `VmScriptCompiler.Core`：环境、schema/语义验证、任务目录、资源校验、三类脚本生成、SOL Create/Patch、parse/inspect 和报告。
- `VmScriptCompiler.Cli`：`env`、`plan`、`build`、`patch`、`inspect`、`validate`。
- `VmScriptCompiler.DomainWorker`：Agent 内部使用的窄 JSONL 协议，只暴露确定性 VM 领域能力。
- `agent/`：基于 Pi 的有状态 VM 领域 Agent，负责多轮推理、会话、恢复和完成条件。
- `VmScriptCompiler.Mcp`：9 个确定性 stdio MCP 工具，作为 Codex 等外部客户端的适配器。
- `VmScriptCompiler.Desktop`：面向普通用户的 WPF 主产品，内置 Pi Agent、Node 运行时和 Domain Worker。
- `VmScriptCompiler.Agent`：旧的一次性 Prompt→IR 兼容项目；停止扩展，不进入正式发布包。

支持 GlobalScript C#、ShellModule C# 和 PyShellModule Python。模块默认互不连接，只有 Requirement 顶层 `connections` 会建立执行连接。

## 构建与验证

```powershell
dotnet build VmScriptCompiler.sln -c Release
tests\run-full-smoke.ps1 -SkipBuild
tests\run-m2-smoke.ps1 -SkipBuild
tests\run-m6-smoke.ps1 -SkipBuild
tests\run-m8-smoke.ps1 -SkipBuild
tests\run-m9-smoke.ps1 -SkipBuild
tests\run-m10-smoke.ps1 -SkipBuild
tests\run-m11-smoke.ps1 -SkipBuild
tests\run-m12-smoke.ps1 -SkipBuild
tests\run-audit-smoke.ps1 -SkipBuild
tests\run-domain-worker-smoke.ps1 -SkipBuild
tests\run-agent-domain-smoke.ps1 -SkipBuild
tests\run-agent-point-sort-smoke.ps1 -SkipBuild
tests\run-desktop-agent-smoke.ps1 -SkipBuild
tests\run-release-smoke.ps1
```

## CLI

```powershell
dotnet src\VmScriptCompiler.Cli\bin\Release\net8.0\VmScriptCompiler.Cli.dll env
dotnet src\VmScriptCompiler.Cli\bin\Release\net8.0\VmScriptCompiler.Cli.dll plan --spec requirement.json
dotnet src\VmScriptCompiler.Cli\bin\Release\net8.0\VmScriptCompiler.Cli.dll build --spec requirement.json --output outputs\build
dotnet src\VmScriptCompiler.Cli\bin\Release\net8.0\VmScriptCompiler.Cli.dll patch --base business.sol --spec requirement.json --output outputs\patch
```

Requirement schema 位于 `schemas/requirement.schema.json`，类型和 API 证据位于 `resources/vm/4.4.0/type-system.json` 与 `api-catalog.json`。

## 桌面领域 Agent

开发运行：

```powershell
.\.runtime\node-v22.19.0-win-x64\npm.cmd run build --prefix agent
dotnet run --project src\VmScriptCompiler.Desktop -c Release
```

正式入口为 `dist\Desktop\vm-script-compiler-desktop.exe`。桌面端提供会话列表、流式 Agent 消息、工具执行卡片、Create/Patch、基底 SOL 选择、结果索引、停止执行和用户 VM 实机确认。普通用户只需在“配置”中填写：

- Provider：OpenAI Responses 或 OpenAI-compatible Chat Completions；
- API 基础地址：例如 `https://api.openai.com/v1`，不要填写 `/responses`；
- 模型 ID；
- API Key。

API Key 只以 Windows 当前用户 DPAPI 密文保存，不写入会话、Requirement、报告或发布包。Agent 只维护 Requirement IR 和任务状态，SOL 始终由 Domain Worker 中的 Core 生成。

`vm_update_requirement` 直接向模型暴露与项目 Schema 对齐的强类型参数，System Prompt 同时加载当前 `requirement.schema.json` 和 Python Create 示例。单个用户回合最多自动校验 3 次、同一错误最多连续 2 次；同一个任务跨回合累计最多 6 个 Requirement 版本，避免无效 IR 和 API 猜测循环。

DXF 预览使用 Core 内置的确定性 `dxfRender` 操作，不由模型编写渲染源码。默认尺寸为 1920×1080，运行时输入可修改最终尺寸；支持 Line、Circle、Arc、Ellipse、Polyline2D/3D、Spline、Insert/Block。无可绘制实体或纯白输出会明确失败，像素转换仅保留最终 RGB 缓冲和单行临时缓冲。C# 预编译错误通过 CLI、MCP、Domain Worker 和 Agent 返回结构化诊断数组。

Create 可直接生成纯脚本 SOL；Patch 会先检查业务 SOL 并保证输入文件 SHA-256 不变。自动成功状态最高为 `offline-validated`，只有用户在 VM 中实际确认后才记录为 `user-validated`。

执行 `scripts\publish.ps1` 会构建、运行完整回归，并发布 `dist\Cli`、`dist\Desktop`、`dist\Mcp` 三个 Windows x64 产品。Desktop 是完整 Agent 桌面包，内部携带固定 Node 22.19.0、Pi 0.82.1、生产依赖和自包含 Domain Worker；可整体复制到源码目录外运行。发布包不包含 SOL 模板。

C# ShellModule 将程序集明确分为 `system`、`vm-sdk`（VM 二次开发 DLL）、`operator-sdk`（MVD 算子 DLL）和 `third-party`。已验证的 VM/算子 DLL 直接按实机 SOL 证据写入类型 6/4；外部算子或第三方托管 DLL 需显式声明 `path + referenceType: 4`。构建会校验角色、程序集 Identity、版本、CLR 目标、x64/AnyCPU、递归依赖和 SHA-256；VM SDK 不允许被外部文件覆盖，其他 VM 目录外文件进入 `dependencies` 部署包但不会被程序静默安装。详见 `docs/13-DLL引用校验与部署.md` 和 `examples/vm-and-operator-sdk/`。

## 运行验证边界

生成结果会执行项目内 parser/inspect，并使用 VM 4.4 方法程序集离线预编译 C#、使用 VM 随附 Python 做语法检查。Codex 未打开 VM；2026-07-15 用户已确认三类脚本、基础端口、四个默认值和保存重开正常。2026-07-17 用户进一步确认复杂端口绑定正常，并确认移除不受 UI 支持的 Circle 端口后的 V4 正常；`GetVarCircle/SetVarCircle` 变量 API 继续保留。当前 manifest 已无待验证项。

完整资料索引和验收状态见 `docs/00-资料同步说明.md`、`docs/07-完整实现与验收状态.md`、`docs/10-新增资料分析与能力矩阵.md`。
