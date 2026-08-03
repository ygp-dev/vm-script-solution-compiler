import fs from "node:fs";
import path from "node:path";
import type { AgentConfiguration } from "./config.js";

export function buildVmSystemPrompt(config: AgentConfiguration): string {
  const base = fs.readFileSync(path.join(config.agentRoot, "resources", "SYSTEM.md"), "utf8");
  const schema = fs.readFileSync(
    path.join(config.repositoryRoot, "schemas", "requirement.schema.json"),
    "utf8",
  );
  const pythonExample = fs.readFileSync(
    path.join(config.agentRoot, "resources", "requirement-examples", "python-create.json"),
    "utf8",
  );
  const csharpExample = fs.readFileSync(
    path.join(config.agentRoot, "resources", "requirement-examples", "csharp-create.json"),
    "utf8",
  );
  const globalCsharpExample = fs.readFileSync(
    path.join(config.agentRoot, "resources", "requirement-examples", "global-csharp-create.json"),
    "utf8",
  );
  const apiCatalog = fs.readFileSync(
    path.join(config.repositoryRoot, "resources", "vm", "4.4.0", "api-catalog.json"),
    "utf8",
  );
  return [
    base,
    "## Requirement 权威资料",
    "下列 Schema 是当前编译器实际使用的权威版本。不要猜字段、枚举或嵌套位置。",
    "```json",
    schema,
    "```",
    "### C# Create 最小正确示例",
    "可由 operations 表达的逻辑必须优先使用 operations，并省略 source。Script.Methods 是 VM 内置引用，dependencies 必须为空。",
    "```json",
    csharpExample,
    "```",
    "### C# 自定义 source 的固定入口契约",
    "只有 operations 无法表达且确实需要自定义源码时才使用 source。不得猜测类名或方法名；必须严格使用下面的入口形状：",
    "```csharp",
    "using Script.Methods;",
    "public partial class UserScript : ScriptMethods, IProcessMethods",
    "{",
    "    public void Init() { }",
    "    public bool Process()",
    "    {",
    "        // 通过 this.端口名 读取输入并写入输出。",
    "        return true;",
    "    }",
    "}",
    "```",
    "外部 DLL 必须使用用户明确提供或项目能力证据确认的绝对路径；不要靠更换类名、方法名反复试探入口契约。",
    "### VM 4.4 全局 C# 固定契约与方案加载示例",
    "global-csharp 与 ShellModule 完全不同。必须使用 UserGlobalScript : UserGlobalMethods, IScriptMethods、int Init() 和 int Process()；方案加载完成回调是 override int InitAfterLoadSol()。不得使用 Script.Methods、ScriptMethods、IProcessMethods、void Init()、bool Process() 或 ScriptMain()。以下示例来自项目内 VM 4.4 模板与 API 证据。",
    "```json",
    globalCsharpExample,
    "```",
    "### VM 4.4 权威能力目录",
    "下列目录由项目内已验证模板、中文样例和反编译 API 汇总。生成 VM API 调用时只能使用这里确认的载体契约与方法；如信息仍不足，再调用 vm_query_capability，不得猜测。",
    "```json",
    apiCatalog,
    "```",
    "### Python Create 最小正确示例",
    "注意：`source`、`operations`、`dependencies` 都是 script 顶层属性；`execution` 只能包含 `mode` 和 `order`，普通脚本使用 `mode: \"once\"`。",
    "```json",
    pythonExample,
    "```",
  ].join("\n\n");
}
