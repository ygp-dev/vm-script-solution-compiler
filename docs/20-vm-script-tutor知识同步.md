# vm-script-tutor 知识同步

`knowledge/vm-script-tutor/` 是 VisionMaster 2D C# 脚本 skill 的原始维护目录。当前版本为 1.2。

为让编译器 Agent 可以稳定消费，项目将它整理为：

- `resources/vm/4.4.0/agent-prompt-knowledge.json`：注入 Agent 的紧凑高置信度规则、类型/版本兼容、模块参数查询、程序集策略、异常处理和静态审查要求。
- `resources/vm/4.4.0/agent-knowledge.json`：按需检索的 FAQ、参考方案、选定 tutor 示例和二级知识条目；由 `vm_search_project_knowledge` 提供只读证据。
- `agent/src/system-prompt.ts`：新版 Pi Agent 启动时加载。
- `src/VmScriptCompiler.Agent/PromptRequirementProvider.cs`：旧版兼容 Agent 生成外部模型提示时加载。
- `tests/run-agent-knowledge-smoke.ps1`：校验紧凑 JSON、manifest SHA-256、按需检索和新版 Agent 提示词加载。

## 规则优先级

skill 同步内容用于人工 C# ShellModule 脚本的生成、审查和配置提示，不会直接扩展确定性编译能力。发生冲突时按以下顺序处理：

1. 用户确认的 `UserProperty.cs`、`csproj`、`AlgorithmTab.xml` 和目标预编译诊断。
2. `resources/vm/4.4.0/` 中已验证的 VM 4.4 能力目录、模板和实机证据。
3. `agent-prompt-knowledge.json` 与 `agent-knowledge.json` 中的整理内容，以及 `knowledge/vm-script-tutor/references/`、`examples/` 中被选入的条目。
4. 社区文章和经验模式，仅用于需求拆解与风险提示。

该 skill 只覆盖 2D VisionMaster C# ShellModule；3D、Python 脚本模块、控制器 IO、UI 自动化、通信协议解析以及 VM 安装目录 DLL/EXE 反编译仍不在同步范围内。
