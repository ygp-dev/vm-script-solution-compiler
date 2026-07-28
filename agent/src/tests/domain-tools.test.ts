import test from "node:test";
import assert from "node:assert/strict";
import fs from "node:fs";
import os from "node:os";
import path from "node:path";
import { fileURLToPath } from "node:url";
import { SessionManager, type ToolDefinition } from "@earendil-works/pi-coding-agent";
import { VmDomainState } from "../domain/state.js";
import { DomainWorkerClient } from "../tools/domain-worker-client.js";
import { createVmTools } from "../tools/vm-tools.js";

const repositoryRoot = path.resolve(path.dirname(fileURLToPath(import.meta.url)), "../../..");
const workerPath = path.join(
  repositoryRoot,
  "src",
  "VmScriptCompiler.DomainWorker",
  "bin",
  "Release",
  "net8.0",
  "vm-script-domain-worker.dll",
);

test("Pi VM tools complete a deterministic Create workflow without MCP", async () => {
  const output = fs.mkdtempSync(path.join(os.tmpdir(), "vm-agent-tools-"));
  const requirement = JSON.parse(fs.readFileSync(
    path.join(repositoryRoot, "tests", "fixtures", "m3-shell-create.json"),
    "utf8",
  )) as Record<string, unknown>;
  const manager = SessionManager.inMemory(repositoryRoot);
  const state = new VmDomainState(manager);
  const worker = new DomainWorkerClient({ workerPath, repositoryRoot });
  const tools = createVmTools(state, worker);
  const updateTool = tools.find((tool) => tool.name === "vm_update_requirement");
  assert.ok(updateTool);
  const updateSchema = JSON.stringify(updateTool.parameters);
  assert.match(updateSchema, /"source"/);
  assert.match(updateSchema, /"operations"/);
  assert.match(updateSchema, /"once"/);
  assert.match(updateSchema, /"python-module"/);
  assert.doesNotMatch(updateSchema, /"requirement":\{\}/);

  try {
    await invoke(tools, "vm_detect_environment", {});
    await invoke(tools, "vm_update_requirement", { requirement });
    await invoke(tools, "vm_validate_requirement", {});
    await invoke(tools, "vm_plan_solution", {});
    await invoke(tools, "vm_build_solution", { output });
    await invoke(tools, "vm_validate_solution", {});

    const snapshot = state.snapshot();
    assert.equal(snapshot.phase, "offline-validated");
    assert.equal(snapshot.completion.requirementValid, true);
    assert.equal(snapshot.completion.solutionBuilt, true);
    assert.equal(snapshot.completion.offlineValidationPassed, true);
    assert.equal(snapshot.completion.userValidated, false);
    const solution = snapshot.artifacts.find((artifact) => artifact.kind === "solution")?.path;
    assert.ok(solution);
    assert.ok(fs.existsSync(solution));
  } finally {
    await worker.dispose();
    fs.rmSync(output, { recursive: true, force: true });
  }
});

async function invoke(
  tools: ToolDefinition[],
  name: string,
  params: Record<string, unknown>,
): Promise<unknown> {
  const tool = tools.find((candidate) => candidate.name === name);
  assert.ok(tool, `Missing tool ${name}`);
  return await tool.execute(
    `test-${name}`,
    params,
    undefined,
    undefined,
    {} as never,
  );
}
