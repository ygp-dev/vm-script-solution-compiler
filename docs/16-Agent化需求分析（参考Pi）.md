# VM Script Solution Agent 需求分析（参考 Pi）

> 架构决策更新：本文对领域需求的分析仍然有效，但第 5、8、10 节中“在 .NET 内重建 Agent Runtime”的建议已被 [17-Pi领域Agent与CSharp能力迁移方案.md](./17-Pi领域Agent与CSharp能力迁移方案.md) 取代。新决策是直接复用 Pi Runtime，同时把现有 C# Compiler Core 作为领域 Agent 的确定性执行内核；MCP 只保留为可选外部适配器。
>
> 分析日期：2026-07-27  
> 参考项目：[earendil-works/pi](https://github.com/earendil-works/pi)、[pi.dev](https://pi.dev/)  
> 本阶段范围：需求与架构分析，不修改 Compiler Core、Agent、MCP 或 Desktop 代码。

## 1. 结论

产品应从“带 AI 输入框的 SOL 编译器”升级为“面向 VisionMaster 脚本方案的领域 Agent”。

目标不是复制 Pi 的 TypeScript/TUI 实现，也不是让模型直接获得文件和 SOL 的任意写权限，而是借鉴 Pi 的分层：

1. 可持续运行的 Agent Loop；
2. 结构化、可流式展示的工具调用；
3. 可恢复、可分支、可压缩的会话；
4. 模型与 Provider 解耦；
5. 上下文、技能、提示模板和扩展资源分层；
6. UI、CLI、RPC/MCP 复用同一个 Agent Runtime；
7. 所有 SOL 修改仍必须经过 Requirement IR 和确定性 Compiler Core。

产品定位建议改名为：

**VM Script Solution Agent**

`VmScriptCompiler.Core` 继续作为确定性编译内核；“Compiler”是 Agent 的核心工具，不再代表完整产品。

## 2. Pi 的可借鉴能力

### 2.1 Agent Core

Pi 将底层 Agent 定义为有状态运行时，而不是一次 HTTP 请求。核心能力包括：

- 消息状态；
- 模型和 thinking level；
- 工具注册与参数验证；
- 模型输出流；
- 工具调用循环；
- 工具执行前后 Hook；
- 并行或顺序工具执行；
- 中止、等待空闲、继续执行；
- steering 和 follow-up 队列；
- Agent/Turn/Message/Tool 级事件流。

对本项目的意义：生成失败后，Agent 应能读取错误、修正 Requirement，再次验证或构建，而不是把错误直接丢给用户终止会话。

### 2.2 Coding Agent Session

Pi 的会话不是简单的聊天列表，而是 JSONL 事件树：

- 自动保存；
- 按工作目录组织；
- 恢复最近会话；
- 会话命名；
- 从历史节点继续；
- fork/clone；
- 对话压缩；
- 分支摘要；
- 保存模型切换、工具结果和扩展事件。

对本项目的意义：当前 `recent-conversations.json` 只能记录一次请求结果，不能继续上下文、修正失败或追踪多轮构建。它应升级为真正的 Agent Session。

### 2.3 Resource Loader

Pi 将可变能力拆为：

- `AGENTS.md`：项目约束；
- `SYSTEM.md` / `APPEND_SYSTEM.md`：系统提示；
- Skills：按需加载的能力说明；
- Prompt Templates：可复用命令；
- Extensions：工具、命令、事件和 UI 扩展；
- Packages：上述资源的分发单元。

对本项目的意义：VM 版本知识、端口类型、经典解决方案、DLL 部署规则、Python 能力边界不应无限堆入一个系统提示字符串。它们应按任务渐进加载。

### 2.4 Headless/RPC

Pi 的交互式 UI、JSON 事件模式和 RPC 模式共用同一 Session Runtime。RPC 使用严格 JSONL，并输出消息、工具、重试、压缩等事件。

对本项目的意义：Desktop、CLI、MCP 不应分别实现提示词逻辑。应增加统一的 Agent Runtime 和事件协议：

- Desktop 直接订阅 Runtime 事件；
- CLI 支持交互模式和一次性模式；
- RPC 供非 .NET UI、自动化或 IDE 接入；
- MCP 保留为外部确定性工具入口。

### 2.5 最小核心与扩展性

Pi 尽量保持核心精简，把工作流差异交给扩展。

本项目应借鉴“能力分层”，但不能照搬 Pi 的所有安全取舍。Pi 默认使用启动用户的权限，且不内置权限弹窗；本产品涉及业务 SOL、外部 DLL 和 VM 运行环境，应内置领域权限与风险确认。

## 3. 当前产品现状

### 3.1 已有优势

- 确定性 `VmScriptCompiler.Core` 已成熟；
- Create/Patch、预编译、parse、inspect 和报告链完整；
- Requirement IR、schema、类型系统和资源 manifest 已存在；
- Agent 坚持“AI 只生成 IR，不直接编辑 SOL”；
- MCP 已提供 8 个稳定工具；
- Desktop 已有 Provider 配置、最近请求和产物索引；
- 输入 SOL 不原地覆盖；
- 不确定 VM 能力会以稳定错误码阻断；
- C# DLL、Python 依赖和 VM 版本均有安全门禁。

这些能力应保留并成为 Agent 工具层，而不是重写。

### 3.2 当前 Agent 的本质

`VmScriptCompiler.Agent` 当前是一次性适配器：

```text
Prompt
  → 单次 Provider 请求
  → Requirement IR
  → 单次 Core Build/Patch
  → 成功或错误
```

它没有：

- 多轮消息上下文；
- Agent Loop；
- 模型可调用的工具；
- 流式输出；
- 工具事件；
- 自动错误修复；
- 澄清问题；
- 中止与继续；
- 会话恢复与分支；
- 上下文压缩；
- 项目级 Agent 配置；
- Skills/Prompt Templates；
- Provider 动态模型列表；
- 统一 RPC。

### 3.3 Desktop 的限制

Desktop 虽然采用对话布局，但目前仍是“表单式构建器”：

- 对话记录是构建日志，不是消息树；
- 点击历史只恢复提示词；
- 模型不能先检查 SOL 再决定如何 Patch；
- 用户看不到模型思考之外的结构化工具步骤；
- 失败后不能在同一会话中自动诊断和修复；
- 执行中不能 steer、follow-up 或 abort；
- 没有分支、恢复、压缩和会话导入导出。

## 4. 产品目标

用户应能用自然语言持续协作，例如：

```text
用户：给这个业务 SOL 增加一个 Python 点排序脚本。
Agent：先检查 SOL 和 VM Python 类型能力。
Agent → inspect_solution
Agent → inspect_capability
Agent：Python Point 端口尚未被项目样本确认。可以：
       1. 改用 C# PointData[]；
       2. Python 使用 X/Y float[]；
       3. 导入一个 VM 保存的 Python Point 样本建立证据。
用户：用 C#，按 X 再按 Y。
Agent → draft_requirement
Agent → validate_requirement
Agent → patch_solution
Agent → validate_solution
Agent：已生成，原方案未覆盖，报告与结果在……
```

Agent 必须能“检查—计划—澄清—执行—验证—修复—交付”，而不是只做“提示词转 JSON”。

## 5. 目标架构

```text
Desktop / Interactive CLI / Print CLI / RPC
                    │
                    ▼
        VmScriptCompiler.Agent.Runtime
        ├─ AgentSession
        ├─ AgentLoop
        ├─ EventStream
        ├─ ToolRegistry
        ├─ PolicyHooks
        ├─ Context/Skill Loader
        ├─ Compaction
        └─ Retry/Recovery
             │              │
             ▼              ▼
      Model Providers    Domain Tools
                         ├─ Compiler Core
                         ├─ SOL Inspector
                         ├─ Capability Catalog
                         ├─ Artifact Index
                         └─ User Confirmation
                              │
                              ▼
                    Requirement IR → SOL
```

建议新增项目：

```text
src/
├─ VmScriptCompiler.Agent.Abstractions/
├─ VmScriptCompiler.Agent.Runtime/
├─ VmScriptCompiler.Agent.Tools/
├─ VmScriptCompiler.Agent.Providers/
├─ VmScriptCompiler.Agent.Storage/
├─ VmScriptCompiler.Agent.Rpc/
├─ VmScriptCompiler.Agent/          # CLI 宿主
├─ VmScriptCompiler.Desktop/        # WPF 宿主
├─ VmScriptCompiler.Mcp/
└─ VmScriptCompiler.Core/
```

初期也可先在现有 `VmScriptCompiler.Agent` 内按目录分层，接口稳定后再拆项目。

## 6. 核心功能需求

### FR-01 多轮 Agent Loop（P0）

Agent 必须反复执行：

```text
构造上下文 → 调用模型 → 流式接收响应
→ 若有工具调用则验证/执行 → 写入工具结果
→ 再次调用模型 → 直到完成、阻断、询问或中止
```

约束：

- 默认最大 Turn 数可配置；
- 工具参数必须通过 JSON Schema；
- 工具异常必须以结构化 Tool Result 返回模型；
- 不得因为模型文本声称成功而标记成功；
- 只有确定性验证通过才可宣布 SOL 生成成功。

### FR-02 Agent 事件流（P0）

统一事件至少包括：

- `agent_started`
- `agent_completed`
- `agent_failed`
- `turn_started`
- `turn_completed`
- `message_started`
- `message_delta`
- `message_completed`
- `tool_started`
- `tool_progress`
- `tool_completed`
- `tool_failed`
- `user_input_required`
- `retry_scheduled`
- `compaction_started`
- `compaction_completed`
- `session_updated`

所有宿主只消费该事件流，不解析 Provider 私有响应。

### FR-03 领域工具（P0）

首版工具清单：

| 工具 | 读写 | 说明 |
|---|---|---|
| `detect_environment` | 只读 | 检测 VM、SDK、Python |
| `inspect_solution` | 只读 | 读取流程、模块、脚本、端口和绑定 |
| `inspect_capability` | 只读 | 查询 VM 类型、Python/C#、DLL 证据 |
| `search_project_knowledge` | 只读 | 检索项目内验证资料和错误码 |
| `draft_requirement` | 内存 | 生成/更新 Requirement IR 草稿 |
| `validate_requirement` | 只读 | schema 和语义校验 |
| `plan_solution` | 只读 | 输出确定性动作 |
| `build_solution` | 写输出目录 | Create |
| `patch_solution` | 写输出目录 | Patch，不覆盖输入 |
| `validate_solution` | 只读 | parse/inspect/结构校验 |
| `read_build_report` | 只读 | 获取构建证据与失败原因 |
| `list_artifacts` | 只读 | 查询会话关联产物 |
| `request_user_input` | 交互 | 请求会改变方案行为的选择 |

`build_solution` 和 `patch_solution` 必须是顺序工具；只读工具可并行。

### FR-04 Requirement 草稿状态（P0）

会话应持有一个可版本化的 Requirement Draft：

- 每次模型修改产生新 revision；
- 保存模型原始 Tool Call 和归一化后的 IR；
- Core Validator 的问题与 revision 绑定；
- 构建产物记录使用的 revision；
- 用户可以查看最终 IR，但普通操作不要求理解 IR。

### FR-05 自动修复循环（P0）

对以下可恢复错误，Agent 可读取错误并修订 IR 后重试：

- schema 字段缺失；
- C# 入口契约机械性错误；
- Provider 错误地产生的可归一化字段；
- 名称需要从 inspect 结果中选择；
- 端口类型可由已验证 catalog 唯一确定；
- 预编译中明确且安全的 using/引用问题。

对以下错误必须询问或阻断：

- Python 复杂类型无样本；
- 未确认模块参数；
- 外部 DLL 路径、版本或架构不明确；
- Create/Patch 模式会改变用户意图；
- 多个同名模块；
- 会覆盖、部署或执行外部状态；
- VM 实机行为无法离线确认。

每类错误的最大自动重试次数必须配置，避免循环。

### FR-06 澄清问题（P0）

Agent 可以暂停并向用户提出一个或多个短问题。问题必须包含：

- 缺失信息；
- 可选方案；
- 每个方案的行为差异；
- 推荐项及理由；
- 当前 Requirement Draft 和会话保持不丢失。

### FR-07 会话持久化（P0）

会话使用追加式 JSONL，最少保存：

- session id、名称、cwd、创建/更新时间；
- parent entry id；
- user/assistant/tool 消息；
- Provider、模型、thinking level；
- Requirement revision；
- 工具输入摘要、输出摘要、错误码；
- 构建产物路径与哈希；
- compaction 和 branch summary；
- 中止、重试和用户确认事件。

敏感字段不得进入会话：

- API Key；
- SOL 密码；
- OAuth token；
- 未脱敏凭据。

### FR-08 会话操作（P1）

- 新建；
- 恢复最近；
- 搜索；
- 重命名；
- 删除到回收站；
- 从历史用户消息 fork；
- clone；
- 导出 JSONL/Markdown；
- 导入；
- 查看会话树；
- 关联产物跟随分支。

### FR-09 上下文管理（P1）

上下文来源：

1. 产品系统提示；
2. VM 版本 manifest；
3. 当前任务相关的类型和 API catalog；
4. 当前 SOL inspect 摘要；
5. 当前 Requirement Draft；
6. 最近消息；
7. 按需加载的 Skill；
8. 压缩摘要。

不得每轮无条件注入全部 schema、所有 API catalog、所有模块参数和完整 SOL parse JSON。

当上下文接近模型窗口时自动压缩，摘要必须保留：

- 用户目标；
- 已确认的选择；
- 当前基底 SOL；
- Requirement revision；
- 工具执行结果；
- 错误与已尝试修复；
- 产物路径；
- 未完成事项。

### FR-10 VM Skills（P1）

建议内置：

```text
skills/
├─ create-script-solution/
├─ patch-business-solution/
├─ csharp-module/
├─ python-module/
├─ global-script/
├─ vm-dynamic-io/
├─ external-dll/
├─ netdxf/
├─ vm-sdk/
└─ diagnose-build-error/
```

Skill 只提供知识和工作流，不绕过 Core 工具。

### FR-11 Prompt Templates（P1）

内置模板：

- `/create-csharp`
- `/create-python`
- `/patch-script`
- `/add-dll`
- `/diagnose`
- `/validate`
- `/explain-solution`

模板可以收集参数后生成普通用户消息。

### FR-12 Provider 与模型（P1）

Provider 接口应支持：

- 流式文本；
- 流式工具调用；
- Responses API；
- Chat Completions；
- 自定义兼容 Provider；
- 模型上下文窗口；
- thinking/reasoning 级别；
- token usage；
- 取消；
- transient error 分类；
- retry-after；
- 模型能力声明。

首版继续支持：

- OpenAI Responses；
- OpenAI-compatible Chat Completions。

后续再增加 OAuth 和更多 Provider。不要为了模仿 Pi 一次性接入大量模型服务。

### FR-13 配置分层（P1）

建议：

```text
%LOCALAPPDATA%\VM Script Compiler\agent-settings.json
<workspace>\.vm-script-agent\settings.json
```

项目配置覆盖全局配置，但不能覆盖全局安全下限。

配置包括：

- Provider、模型、endpoint；
- thinking level；
- 最大 turn/retry；
- 自动压缩阈值；
- 输出目录；
- 会话目录；
- 启用的 Skills/Tools；
- 主题；
- 项目信任。

API Key 使用 Windows DPAPI 或 Credential Manager。

### FR-14 项目信任与权限（P0）

与 Pi 不同，本产品必须内置领域安全策略。

权限分级：

| 操作 | 默认策略 |
|---|---|
| 环境、catalog、SOL inspect | 自动允许 |
| Requirement 草稿/验证 | 自动允许 |
| Create 输出到用户配置目录 | 自动允许 |
| Patch 到新目录 | 自动允许，但显示基底和目标 |
| 覆盖文件 | 禁止 |
| 修改 VM 安装目录 | 禁止 |
| 部署 DLL | 每次确认 |
| 执行部署脚本 | 每次确认 |
| 打开/运行 VM | 用户主动操作 |
| 网络下载依赖 | 禁止或明确确认 |
| 加载项目级扩展 | 首次信任确认 |

### FR-15 Desktop Agent UI（P0）

保留当前 Codex 风格布局，改造成真正的 Agent UI：

- 左侧：会话搜索、新建、恢复、重命名、状态；
- 中间：用户/Agent 消息流；
- 工具调用以可折叠卡片显示；
- 工具卡片显示开始、进度、成功/失败、耗时；
- Requirement 校验问题显示为结构化列表；
- 产物卡片显示 SOL、报告、源码和 DLL 部署包；
- 底部：多行输入、附件/Base SOL、发送、中止；
- 执行期间允许 steering/follow-up；
- `user_input_required` 显示按钮式选择；
- Provider、模型、thinking level 在会话级可切换；
- 错误后提供“让 Agent 修复”“修改需求”“查看详情”。

### FR-16 CLI 与 RPC（P1）

CLI：

```powershell
vm-script-agent
vm-script-agent -p "创建一个 C# 求和脚本"
vm-script-agent --continue
vm-script-agent --resume
vm-script-agent --session <id>
vm-script-agent --no-session
```

RPC：

- 严格 LF JSONL；
- command/response 带 correlation id；
- Agent events 持续输出；
- 支持 prompt、steer、follow-up、abort、get_state、list_sessions、resume；
- Desktop 可直接引用 .NET Runtime，不必通过子进程；RPC 面向其他宿主。

### FR-17 可观测性（P1）

每次 Agent Run 记录：

- run/turn/tool id；
- Provider、模型；
- 首 token 延迟；
- 总耗时；
- token usage；
- 工具耗时；
- 重试次数；
- Requirement revision；
- 最终结果；
- Compiler error code；
- 用户确认记录。

日志和会话都必须脱敏。

## 7. 非功能需求

### NFR-01 确定性边界

- 模型不能直接修改 SOL；
- 模型不能直接输出 ModuleFrame/XML 写入命令；
- 所有写操作必须通过 Core；
- 最终成功必须以 Core 验证结果为准。

### NFR-02 响应性

- 首个流式事件目标小于 500 ms（不含 Provider 首 token）；
- UI 线程不执行网络、预编译或 SOL 操作；
- tool progress 至少能显示阶段变化；
- abort 后立即停止后续 Turn，并尽力取消 Provider/工具。

### NFR-03 恢复能力

- 进程崩溃后会话 JSONL 保持可读；
- 追加记录必须原子化；
- 不完整事件可识别并标记 interrupted；
- 已完成产物不能因会话恢复被删除。

### NFR-04 兼容性

- Windows x64；
- .NET 8 Agent/Desktop；
- VM 4.4 资源与生成脚本目标框架保持现状；
- 现有 CLI、MCP 工具和 Requirement schema 保持兼容。

### NFR-05 测试

- Agent Loop 使用 Fake Provider；
- 工具调用序列可快照；
- 流式事件顺序可验证；
- 自动修复有最大次数；
- 会话恢复、fork、compaction 可回放；
- 不调用真实模型即可覆盖绝大多数测试；
- 真实 Provider 测试单独标记并需要显式 Key。

## 8. 不应照搬 Pi 的部分

1. **不直接依赖 Pi Runtime**  
   当前产品为 C#/.NET/Windows，Core 和 Desktop 已稳定。直接嵌入 Node/TypeScript 会增加发布体积、跨进程协议和故障面。应借鉴接口与行为，在 .NET 中实现领域 Runtime。

2. **不提供通用 bash/write/edit 作为默认工具**  
   Agent 的任务是生成 VM 脚本方案，默认只开放领域工具。通用文件或命令工具会破坏确定性安全边界。

3. **不取消权限策略**  
   DLL 部署、VM 安装目录和外部程序必须有明确策略。

4. **不一次性实现任意扩展代码执行**  
   首版先做只读 Skills 和 Prompt Templates。可执行 Extensions 在项目签名、信任和隔离方案成熟后再做。

5. **不把所有知识塞进系统提示**  
   当前 `BuildInstructions()` 拼接整个 schema/catalog 的方式需要改为按需上下文与工具查询。

6. **不把 MCP 当成内部 Agent Loop**  
   内部直接调用 .NET Tool 接口；MCP 是外部互操作适配器。两者共享工具定义和实现。

## 9. 数据契约草案

### Agent Event

```json
{
  "eventId": "evt_...",
  "sessionId": "ses_...",
  "runId": "run_...",
  "turnId": "turn_...",
  "timestampUtc": "2026-07-27T00:00:00Z",
  "type": "tool_completed",
  "payload": {
    "toolCallId": "tool_...",
    "toolName": "validate_requirement",
    "ok": false,
    "errorCode": "PYTHON_COMPLEX_TYPE_UNCONFIRMED",
    "summary": "Python Point 端口缺少 VM 4.4 样本"
  }
}
```

### Session Entry

```json
{
  "id": "entry_...",
  "parentId": "entry_...",
  "sessionId": "ses_...",
  "kind": "tool-result",
  "createdUtc": "2026-07-27T00:00:00Z",
  "runId": "run_...",
  "data": {}
}
```

### Tool Result

```json
{
  "ok": false,
  "code": "PYTHON_COMPLEX_TYPE_UNCONFIRMED",
  "message": "Python Point 端口尚无已验证样本。",
  "recoverability": "requires-user-choice",
  "suggestions": [
    {
      "id": "use-csharp",
      "label": "改用 C# PointData[]"
    },
    {
      "id": "split-xy",
      "label": "Python 使用 X/Y float[]"
    }
  ],
  "details": {}
}
```

## 10. 实施路线

### A0：需求与协议冻结

- 冻结 Agent Event、Session Entry、Tool Contract；
- 定义自动修复与必须询问矩阵；
- 定义权限策略；
- 不改 Core 行为。

验收：协议文档和 Fake Provider 测试设计评审通过。

### A1：Agent Runtime 最小闭环（P0）

- Agent Loop；
- OpenAI Responses 流式工具调用；
- 6 个首批工具：
  `detect_environment`、`inspect_solution`、`validate_requirement`、
  `plan_solution`、`build_solution`、`patch_solution`；
- 事件流；
- abort；
- 最大 turn/retry；
- 内存会话。

验收：Fake Provider 能完成 Create、Patch、一次自动修复和一次用户询问。

### A2：持久会话与 Desktop

- JSONL Session Store；
- 恢复、重命名、搜索；
- Desktop 消息流和工具卡片；
- 产物关联；
- 发送/中止；
- Provider/模型会话配置。

验收：关闭 Desktop 后恢复同一会话并继续修复失败任务。

### A3：上下文工程

- Resource Loader；
- VM Skills；
- Prompt Templates；
- 按需 catalog 工具；
- compaction；
- 分支/fork。

验收：长会话压缩后仍保留基底 SOL、用户选择、IR revision 和产物。

### A4：CLI/RPC 与扩展

- 交互 CLI；
- print 模式；
- RPC JSONL；
- project settings/trust；
- 只读扩展资源；
- MCP 与 Agent Tool 共享契约。

验收：第三方客户端可通过 RPC 完整驱动一个 Patch 会话。

### A5：增强 Provider 与安全

- Chat Completions 流式工具；
- OAuth/更多 Provider；
- Credential Manager；
- DLL 部署确认；
- 可执行扩展签名/信任设计；
- 会话导入导出。

## 11. 首版 Agent 完成定义

以下条件全部满足，才可称为 Agent：

1. 模型能基于工具结果连续执行多个 Turn；
2. 用户能看到流式消息和工具状态；
3. 失败后能自动修复或提出明确选择；
4. 会话可保存、恢复并继续；
5. 用户可以中止；
6. 每次构建关联 Requirement revision 和产物；
7. Agent 不直接编辑 SOL；
8. 输入 SOL 不覆盖；
9. Provider、UI、CLI 不复制 Core 逻辑；
10. Fake Provider 自动测试可重放完整 Agent 行为。

## 12. 待确认的产品决策

进入实现前需要冻结：

1. 产品显示名是否改为“VM Script Solution Agent”；
2. 首版是否只保留 OpenAI Responses + OpenAI-compatible；
3. Desktop 是否优先于交互 CLI；
4. 会话首版采用 JSONL，还是直接 SQLite；
5. 是否首版实现 fork/tree，或放到 A3；
6. 项目级 Skills 是否允许用户编辑；
7. DLL 部署是否只生成命令，还是 Agent 可以在确认后执行；
8. 是否保留当前一次性 `vm-script-agent build/patch` 命令作为兼容入口。

## 13. 参考资料

- Pi repository: https://github.com/earendil-works/pi
- Pi Agent Core: https://github.com/earendil-works/pi/tree/main/packages/agent
- Pi documentation: https://pi.dev/docs/latest/
- Sessions: https://pi.dev/docs/latest/sessions
- SDK: https://pi.dev/docs/latest/sdk
- RPC: https://pi.dev/docs/latest/rpc
- Extensions: https://pi.dev/docs/latest/extensions
- Settings: https://pi.dev/docs/latest/settings
- Compaction: https://pi.dev/docs/latest/compaction
