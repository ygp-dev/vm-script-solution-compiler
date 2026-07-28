# MCP 与 Desktop 使用说明

## 产品边界

Desktop 是普通用户使用的完整 VM 领域 Agent；MCP 是 Codex 等外部客户端用于复现、检查和确定性构建的适配器。两者共享 `VmScriptCompiler.Core`，但职责不同：

- Desktop：Pi 多轮模型循环、会话、任务状态、错误恢复、结果索引和用户验收；
- Domain Worker：Desktop Agent 内部的确定性 JSONL 能力边界；
- MCP：不含模型、不管理 Agent 会话、不提供嵌套自然语言 Provider；
- Core：唯一允许创建或修改 SOL 的实现。

AI 永不直接编辑 SOL。Patch 永不覆盖输入 SOL。

## Desktop

开发入口：

```powershell
.\.runtime\node-v22.19.0-win-x64\npm.cmd run build --prefix agent
dotnet run --project src\VmScriptCompiler.Desktop -c Release
```

发布入口：

```text
dist\Desktop\vm-script-compiler-desktop.exe
```

桌面版已经内置固定版本 Node、Pi Agent、生产依赖和自包含 Domain Worker，不要求用户另外安装 Node 或 .NET。运行机器仍需安装 VisionMaster 4.4。

典型操作：

1. 在配置页选择 `OpenAI Responses` 或 `OpenAI-compatible`。
2. 填写 API 基础地址、模型和 API Key。
3. 新建对话，选择 Create 或 Patch；Patch 时选择基底 SOL。
4. 输入自然语言需求并发送。
5. Agent 自动检查环境、维护 Requirement、规划、构建并离线验证。
6. 成功后从结果卡片或产物索引打开 `result.sol` 和报告。
7. 用户在 VM 中确认后点击“我已在 VM 验证”。

OpenAI Responses 的地址填写 API 基础地址，例如 `https://api.openai.com/v1`；程序会调用 `/responses`。OpenAI-compatible 填基础地址，程序会调用 `/chat/completions`。

API Key 使用 Windows 当前用户 DPAPI 加密后保存到本地配置。会话 JSONL、Requirement、报告和产物不记录 Key。

## MCP

开发入口：

```powershell
dotnet run --project src\VmScriptCompiler.Mcp -c Release -- --repository-root .
```

发布入口：

```text
dist\Mcp\vm-script-compiler-mcp.exe
```

MCP 采用 stdio JSON-RPC，支持 MCP `2024-11-05`。当前 9 个工具：

| 工具 | 参数 | 作用 |
|---|---|---|
| `detect_environment` | 无 | 检测 VM 安装和 GlobalScript |
| `inspect_solution` | `file` | 只读检查 SOL 并返回 SHA-256 |
| `query_capability` | 可选 `query`, `vmVersion` | 查询项目内 VM 类型/API 证据 |
| `validate_requirement` | `spec` | 只校验 Requirement 文件 |
| `plan_solution` | `spec` | 返回确定性构建动作 |
| `build_solution` | `spec`, `output` | Create 构建 |
| `patch_solution` | `baseSolution`, `spec`, `output` | 非原地 Patch 构建并校验输入哈希 |
| `validate_solution` | `file` | parse、inspect 和结构验证 |
| `read_build_report` | `file` | 读取报告并枚举任务产物 |

旧 `plan_prompt`、`build_prompt`、`patch_prompt` 已移除，避免“外层 Agent 调 MCP、MCP 内部再调用另一个模型”的嵌套 AI。

配置示例：

```json
{
  "mcpServers": {
    "vm-script-compiler": {
      "command": "D:/path/vm-script-solution-compiler/dist/Mcp/vm-script-compiler-mcp.exe"
    }
  }
}
```

修改源码后运行 `scripts\publish.ps1`，会同时更新 Desktop、Domain Worker 和 MCP，并在 `dist\release-manifest.json` 中校验三者携带的 Core 哈希一致。已运行的 MCP 进程需要由客户端重新连接后才会加载新二进制。

## 自动验证

```powershell
tests\run-m6-smoke.ps1 -SkipBuild
tests\run-desktop-agent-smoke.ps1 -SkipBuild
tests\run-release-smoke.ps1
```

测试覆盖 MCP 9 个工具、Patch 输入保护、桌面 UI 组合启动，以及 Desktop → Pi Agent → Domain Worker → 实际 SOL → 离线验证的独立发布全链路。
