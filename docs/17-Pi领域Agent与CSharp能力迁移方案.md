# Pi 领域 Agent 与 C# 能力迁移方案

> 决策日期：2026-07-27  
> 状态：架构决策，取代 `16-Agent化需求分析（参考Pi）.md` 中“在 .NET 内重新实现 Agent Runtime”的建议  
> 本阶段：只冻结需求、边界与验证路线，不修改运行时代码

## 1. 决策

产品应直接使用 Pi 的 Agent Runtime、模型适配、流式事件和会话能力，但不能只做成“Pi 调用现有 MCP”的薄包装。

最终产品是一个 **VisionMaster 脚本方案领域 Agent**：

- Pi 提供通用 Agent 执行引擎；
- VM 领域包提供目标、状态、规则、知识、工具、恢复策略和完成判定；
- 现有 C# Compiler Core 提供确定性的 SOL 检查、生成、Patch、预编译和验证；
- Desktop 是领域 Agent 的用户界面；
- MCP 是给 Codex 等外部宿主使用的可选适配器，不是产品内部架构的中心。

“把现在的 C# 迁移到 Agent 中”指迁移 **能力归属、进程生命周期和发布形态**，不是把已经验证过的 SOL 二进制处理逻辑翻译成 TypeScript。

## 2. 什么才算领域 Agent

只注册以下工具并不能形成领域 Agent：

```text
通用 Pi
  └─ 调用 vm-script-compiler-mcp
```

这种结构只是通用 Agent 使用了一个 MCP Server。它不了解任务当前处于什么阶段，也不能稳定判断下一步、何时询问用户或何时可以交付。

VM 领域 Agent 必须拥有以下五类领域能力。

### 2.1 领域目标

Agent 的目标不是“调用成功一个工具”，而是交付一个满足 VM 约束的 SOL：

1. 理解 Create 或 Patch 意图；
2. 收集并确认必要条件；
3. 基于项目证据选择脚本载体和端口类型；
4. 形成可版本化 Requirement Draft；
5. 调用确定性内核生成或 Patch；
6. 读取失败证据并修订；
7. 通过 parse、inspect、预编译和产物完整性验证；
8. 明确告知哪些结果已离线证明、哪些仍需用户在 VM 中验证。

### 2.2 领域状态

每个会话都要维护 `VmTaskState`，而不是只保存聊天文本：

```json
{
  "intent": "patch",
  "baseSolution": "D:\\work\\base.sol",
  "targetVmVersion": "4.4.0",
  "phase": "requirement-validating",
  "requirementRevision": 3,
  "confirmedFacts": [],
  "unresolvedQuestions": [],
  "capabilityEvidence": [],
  "lastCompilerError": null,
  "artifacts": [],
  "completion": {
    "requirementValid": false,
    "solutionBuilt": false,
    "offlineValidationPassed": false,
    "vmRuntimeValidationRequired": true
  }
}
```

状态转换由领域规则约束。模型可以提出动作，但不能通过一段自然语言自行把任务标为成功。

### 2.3 领域策略

Agent 必须内置并执行以下策略：

- AI 不得直接编辑 SOL、ModuleFrame 或 VM 内部二进制；
- Patch 不覆盖输入 SOL；
- Patch 前必须检查目标流程、模块、参数和绑定；
- 未经项目样本验证的 Python 复杂端口不得猜测；
- VM 4.4 不存在的脚本端口类型不得伪造；
- 外部 DLL 必须验证文件、程序集名、版本、架构和部署位置；
- 预编译通过不等于 VM 实机运行通过；
- 只有确定性验证成功，Agent 才能宣告“已生成”；
- 打开 VM、运行流程和确认 VM UI 行为始终由用户完成。

### 2.4 领域恢复

Agent 必须认识稳定错误码并选择领域动作，例如：

| 错误 | Agent 动作 |
|---|---|
| `SCRIPT_PRECOMPILE_FAILED` | 读取诊断、依赖证据和生成源码，修订 Requirement 后重试 |
| `PYTHON_COMPLEX_TYPE_UNCONFIRMED` | 提供已验证替代方案，等待用户选择 |
| `DEPENDENCY_VERSION_MISMATCH` | 使用真实程序集版本修订依赖声明 |
| `SCRIPT_CONTRACT_INVALID` | 修订脚本载体或入口契约 |
| 模块或参数不存在 | 重新 inspect 基底 SOL，不允许猜名称 |
| Provider 暂时失败 | 按 Pi 的重试能力处理，不触发编译写操作 |

恢复次数必须有限；同一错误重复出现时，应展示证据并请求用户决定，不能无限循环。

### 2.5 领域完成判定

交付状态至少分为：

- `draft`：只有需求草稿；
- `planned`：Requirement 已验证并有确定性动作计划；
- `built`：SOL 已生成；
- `offline-validated`：parse、inspect、预编译和报告全部通过；
- `user-validated`：用户确认 VM 内打开、编译或运行结果；
- `blocked`：缺少样本、DLL、基底对象或用户选择。

Agent 不能把 `built` 当作 `user-validated`。

## 3. 目标架构

```text
┌─────────────────────────────────────────────────────────────┐
│ VM Script Solution Agent                                    │
│                                                             │
│  Pi Runtime                                                 │
│  ├─ model/provider、stream、tool loop、abort                 │
│  ├─ session、resume、branch、compaction                      │
│  └─ RPC                                                     │
│                                                             │
│  VM Domain Package                                          │
│  ├─ VM system policy                                        │
│  ├─ VmTaskState + phase state machine                        │
│  ├─ Requirement Draft revisions                              │
│  ├─ capability/evidence catalog                              │
│  ├─ error recovery matrix                                   │
│  ├─ artifact/completion tracking                             │
│  ├─ VM skills and prompt templates                           │
│  └─ domain tools                                             │
│                         │                                    │
│                         │ internal JSONL domain protocol      │
│                         ▼                                    │
│  C# Domain Worker                                           │
│  └─ VmScriptCompiler.Core                                   │
│     ├─ inspect / capability / requirement validation         │
│     ├─ plan / create / patch                                 │
│     ├─ dependency resolution / precompile                    │
│     └─ parse / inspect / build report                        │
└─────────────────────────────────────────────────────────────┘
              ▲                              ▲
              │ Pi RPC                       │ optional MCP
        WPF Desktop                    Codex / other clients
```

关键点：

1. VM Domain Package 与 Pi Runtime 共同构成领域 Agent；
2. C# Domain Worker 随 Agent 发布，由 Agent 启动、监控和停止；
3. Agent 内部不通过现有 MCP 调用 Core；
4. MCP 与 Desktop 都是适配器，不持有领域决策；
5. Requirement IR 仍是 AI 世界与 SOL 编译世界之间的强边界。

## 4. 为什么保留 C# Core 不会让它退化成 MCP

Agent 是否是领域 Agent，与工具后端使用 C#、TypeScript 或独立进程无关。判断标准是：

- 谁维护领域状态；
- 谁决定工作流；
- 谁执行安全策略；
- 谁理解错误并恢复；
- 谁判断任务完成。

这些职责全部位于 VM Domain Package。C# Core 只执行已验证的确定性命令，因此它相当于 Agent 的“编译器器官”，不是另一个 Agent。

将 C# 算法全部重写为 TypeScript 会带来以下问题，却不会增加 Agent 能力：

- 重做已验证的 SOL 物化和 Patch 行为；
- 重做 VM 4.4 二进制端口、默认值和绑定兼容；
- 重做 C# 预编译和 VM DLL 解析；
- 重新引入历史上已解决的 Circle、复杂 IO、netDxf、版本和绑定错误；
- 产生两套实现结果不一致的风险。

因此不进行语言重写。迁移的是产品边界：用户不再直接面对 `VmScriptCompiler.Agent.exe` 或 MCP，而是面对完整的 VM Agent。

## 5. 组件处置

| 当前组件 | 决策 | 新职责 |
|---|---|---|
| `VmScriptCompiler.Core` | 保留 | 确定性领域内核 |
| `VmScriptCompiler.Cli` | 保留 | 人工诊断、CI 和应急入口 |
| `VmScriptCompiler.Mcp` | 保留但降级 | 外部宿主适配器 |
| `VmScriptCompiler.Agent` | 停止扩展 | 兼容旧的一次性 Prompt→IR 命令，后续标记 Legacy |
| `VmScriptCompiler.Desktop` | 改造 | Pi RPC 客户端和领域状态展示 |
| 新 `agent/` 包 | 新增 | Pi Runtime + VM Domain Package |
| 新 C# Domain Worker | 新增 | 向本产品内部提供稳定 JSONL 领域命令 |

现有 MCP 中的 `plan_prompt`、`build_prompt`、`patch_prompt` 不得被新 Agent 调用。否则会形成“Pi 模型调用工具，工具内部又调用另一个模型”的嵌套 AI，导致上下文割裂、Provider 配置重复和不可预测结果。

## 6. Agent 内部工具

工具应由 VM Domain Package 注册。工具实现可以通过内部 C# Worker 调用 Core，但工具定义、结果解释和状态更新归领域 Agent。

### 6.1 第一批工具

| 工具 | 类型 | 状态影响 |
|---|---|---|
| `vm_detect_environment` | 只读 | 写入 VM/SDK 环境证据 |
| `vm_inspect_solution` | 只读 | 写入基底 SOL 摘要和对象索引 |
| `vm_query_capability` | 只读 | 写入类型、载体、DLL 能力证据 |
| `vm_update_requirement` | 内存/会话 | 产生 Requirement revision |
| `vm_validate_requirement` | 只读 | 更新校验问题和阶段 |
| `vm_plan_solution` | 只读 | 保存确定性动作计划 |
| `vm_build_solution` | 写新产物 | Create；串行执行 |
| `vm_patch_solution` | 写新产物 | Patch；串行执行且不覆盖输入 |
| `vm_validate_solution` | 只读 | 更新离线验证状态 |
| `vm_read_build_report` | 只读 | 提取失败证据 |
| `vm_list_artifacts` | 只读 | 关联会话产物 |
| `vm_record_user_validation` | 会话 | 记录用户的 VM 实机确认 |

### 6.2 默认不开放的工具

VM Agent 默认不向模型提供 Pi 的通用 `bash`、`write`、`edit`。如果维护模式确实需要，应作为独立的开发者模式，并与普通用户构建会话隔离。

模型只能通过 `vm_build_solution` 和 `vm_patch_solution` 触发 SOL 写入。

## 7. C# Domain Worker

为了让 C# 真正成为 Agent 内部能力，而不是内部再套 MCP，新增一个窄协议宿主，例如：

```text
src/VmScriptCompiler.DomainWorker/
```

它：

- 引用 `VmScriptCompiler.Core`；
- 使用 stdin/stdout 严格 LF JSONL；
- 一个请求对应一个结构化响应，可另外发送进度事件；
- 不包含模型、Prompt 或会话逻辑；
- 接受 Requirement JSON 内容，不强迫 Agent 先创建规格文件；
- 返回稳定 `ok/code/message/data`；
- stdout 只输出协议，诊断日志写 stderr；
- 由 Pi Agent 创建并保持一个工作进程，避免每个工具都重新启动；
- 崩溃后可重启，但写操作不能盲目重放；
- 路径必须规范化并限制在输入文件、任务工作区和配置的输出目录。

建议命令：

```text
initialize
detect_environment
inspect_solution
query_capability
validate_requirement
plan_solution
build_solution
patch_solution
validate_solution
read_build_report
shutdown
```

这是产品内部领域协议，不对第三方承诺兼容。外部互操作继续使用 MCP。

## 8. VM Domain Package

建议目录：

```text
agent/
├─ package.json
├─ package-lock.json
├─ src/
│  ├─ main.ts
│  ├─ domain/
│  │  ├─ vm-task-state.ts
│  │  ├─ phase-machine.ts
│  │  ├─ completion-policy.ts
│  │  ├─ recovery-policy.ts
│  │  └─ artifact-registry.ts
│  ├─ tools/
│  │  ├─ domain-worker-client.ts
│  │  └─ vm-tools.ts
│  └─ rpc/
│     └─ desktop-events.ts
├─ resources/
│  ├─ SYSTEM.md
│  ├─ skills/
│  └─ prompts/
└─ tests/
```

Pi 负责通用循环，VM Domain Package 负责限制和塑造这个循环。它不是只写一份很长的 system prompt，而是用显式状态机和 Tool Hook 执行不可违反的规则。

## 9. Desktop 迁移

Desktop 不再直接调用 `RequirementProviderFactory`。改为：

1. 启动随产品发布的 Pi Agent Host；
2. 通过 Pi RPC 发送 prompt、steer、follow-up、abort 和会话命令；
3. 显示流式消息、领域阶段、工具卡片、Requirement 修订和产物；
4. 对用户保持傻瓜式操作，默认只显示“输入、选择基底 SOL、发送、中止”；
5. 高级配置中显示 Provider、模型、地址、Key 和输出目录；
6. 用户确认 VM 结果时，调用领域动作写入当前会话。

当前 `recent-conversations.json` 应迁移为 Pi JSONL 会话索引；当前产物索引可以保留，但每个产物必须关联 `sessionId`、`requirementRevision` 和验证状态。

API Key 继续由 Windows DPAPI 或 Credential Manager 保存。Desktop 解密后只注入 Agent 子进程环境或内存，不写入会话、日志、Requirement 或构建报告。

## 10. 发布形态

普通用户不需要单独安装 Node、Pi、MCP 或 .NET SDK。Desktop 发布目录应包含：

```text
VM Script Solution Agent/
├─ VmScriptCompiler.Desktop.exe
├─ runtime/
│  ├─ node.exe
│  └─ pi-agent/
├─ worker/
│  └─ VmScriptCompiler.DomainWorker.exe
├─ compiler/
│  ├─ Core 依赖
│  ├─ parser
│  └─ resources
└─ adapters/
   └─ VmScriptCompiler.Mcp.exe
```

要求：

- 固定 Pi 和所有 JavaScript 依赖的精确版本；
- 固定 Node 运行时版本；
- 使用 lock file；
- 启动前校验 Agent、Worker、资源 manifest 和解析器；
- Agent、Worker 和 Desktop 版本写入构建报告；
- MCP 可独立发布，但 Desktop 的内部运行不依赖它。

## 11. 最小验证原型 P0

P0 不是先改完整 Desktop，而是证明“领域 Agent + C# 内核”闭环成立。

### 11.1 实现范围

1. 建立固定版本的 Pi Agent 包；
2. 建立 C# Domain Worker，复用 `CompilerFacade`；
3. 实现 `VmTaskState`、阶段机和完成策略；
4. 注册首批六个工具：
   - `vm_detect_environment`
   - `vm_inspect_solution`
   - `vm_update_requirement`
   - `vm_validate_requirement`
   - `vm_patch_solution`
   - `vm_validate_solution`
5. 添加 VM system policy 和一个 Patch Skill；
6. 使用 Pi 会话保存领域状态和工具结果；
7. 先用命令行/RPC 测通，再接 WPF。

### 11.2 验收用例

用一个已有业务 SOL 发起：

```text
给这个方案增加一个 C# 脚本，输入 A、B，输出 Sum，并设置默认值。
```

Agent 必须：

1. 自动识别为 Patch；
2. inspect 基底方案；
3. 建立 Requirement revision；
4. 校验后调用 Patch；
5. 读取并展示预编译与 inspect 结果；
6. 生成新 SOL，不修改基底；
7. 关联 report、generated source 和 result.sol；
8. 将状态停在 `offline-validated`；
9. 明确提示 VM 内可见性和运行结果仍由用户确认；
10. 用户回复“已验证通过”后，状态更新为 `user-validated`。

### 11.3 技术验收

- 至少完成两个模型 Turn 和三个领域工具调用；
- 不调用 MCP；
- 不调用第二个 AI Provider；
- 模型没有通用文件写权限；
- 中止可传递到模型请求和 Worker；
- 会话关闭后可恢复；
- 会话和日志中没有 API Key；
- Worker 异常返回稳定错误码；
- 同一写工具调用不会被自动重复执行；
- 原 SOL 哈希保持不变。

## 12. 实施顺序

### P0：领域闭环

- Pi Agent Host；
- C# Domain Worker；
- VM 状态机、策略和六个核心工具；
- Patch 验收用例；
- 会话恢复和中止。

### P1：完整 Core 能力

- Create、GlobalScript、Python、DLL、能力查询；
- build report 诊断；
- 自动恢复矩阵；
- 产物与 Requirement revisions。

### P2：Desktop Agent UI

- WPF 接 Pi RPC；
- 流式消息和工具卡片；
- 真会话替换最近记录；
- 用户验证状态；
- 简单默认界面和高级设置。

### P3：外部互操作

- MCP 工具与内部领域工具共享请求/响应 DTO；
- Codex 可直接调用确定性工具；
- 可选提供“启动/继续 VM Agent 会话”的高层 MCP 工具，但不复制 Agent Loop。

### P4：领域知识扩展

- VM Skills；
- 版本化能力证据；
- 外部 DLL 和二次开发 DLL 工作流；
- 错误案例回归库；
- 长会话压缩和分支。

## 13. 明确不做

- 不把 SOL 编译器重写成 TypeScript；
- 不让模型直接写 SOL；
- 不把现有 MCP 当作 Agent 本体；
- 不在 Agent 工具内部再次调用 AI；
- 不让 Desktop 复制 Agent Loop；
- 不用 system prompt 代替状态机和确定性策略；
- 不把“生成文件成功”宣称为“VM 实机验证成功”。

## 14. 最终组件定义

一句话定义：

> VM Script Solution Agent 是一个由 Pi 驱动、具有 VM 任务状态和领域策略、以 C# Compiler Core 为确定性执行内核、能够持续检查—规划—生成—验证—恢复—交付的领域 Agent。

各层只承担一种职责：

```text
Pi Runtime       = Agent 引擎
VM Domain Package = 领域大脑
C# Core/Worker   = 确定性执行内核
WPF Desktop      = 用户界面
MCP              = 外部适配器
```

这一定义避免了两个极端：既不是重新用 C# 仿制一套 Pi，也不是给通用 Pi 简单挂一个 MCP 就称为领域 Agent。
