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
  return [
    base,
    "## Requirement 权威资料",
    "下列 Schema 是当前编译器实际使用的权威版本。不要猜字段、枚举或嵌套位置。",
    "```json",
    schema,
    "```",
    "### Python Create 最小正确示例",
    "注意：`source`、`operations`、`dependencies` 都是 script 顶层属性；`execution` 只能包含 `mode` 和 `order`，普通脚本使用 `mode: \"once\"`。",
    "```json",
    pythonExample,
    "```",
  ].join("\n\n");
}
