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
    state.setTaskContext({ mode: "create", outputDirectory: output });
    await invoke(tools, "vm_query_capability", { query: "ScriptMethods IProcessMethods C# source" });
    await invoke(tools, "vm_query_capability", { query: "csharp-module" });
    assert.equal(
      state.snapshot().capabilityEvidence.filter((evidence) => evidence.kind === "capability").length,
      1,
    );
    await invoke(tools, "vm_update_requirement", { requirement });
    await invoke(tools, "vm_compile_solution", {});

    const snapshot = state.snapshot();
    assert.equal(snapshot.phase, "offline-validated");
    assert.equal(snapshot.completion.requirementValid, true);
    assert.equal(snapshot.completion.solutionBuilt, true);
    assert.equal(snapshot.completion.offlineValidationPassed, true);
    assert.equal(snapshot.completion.userValidated, false);
    const solution = snapshot.artifacts.find((artifact) => artifact.kind === "solution")?.path;
    assert.ok(solution);
    assert.ok(fs.existsSync(solution));

    const patchRequirement = JSON.parse(fs.readFileSync(
      path.join(repositoryRoot, "tests", "fixtures", "m3-shell-patch.json"),
      "utf8",
    )) as Record<string, unknown>;
    state.setTaskContext({ mode: "patch", baseSolution: solution, outputDirectory: output });
    await invoke(tools, "vm_inspect_solution", { file: solution });
    await invoke(tools, "vm_update_requirement", { requirement: patchRequirement });
    await invoke(tools, "vm_compile_solution", {});
    const patched = state.snapshot().artifacts
      .filter((artifact) => artifact.kind === "solution")
      .at(-1)?.path;
    assert.ok(patched);
    assert.notEqual(patched, solution);
    assert.ok(fs.existsSync(patched));
    assert.equal(state.snapshot().phase, "offline-validated");
  } finally {
    await worker.dispose();
    fs.rmSync(output, { recursive: true, force: true });
  }
});

test("Requirement update normalizes built-in C# Script.Methods references", async () => {
  const state = new VmDomainState(SessionManager.inMemory(repositoryRoot));
  const worker = new DomainWorkerClient({ workerPath, repositoryRoot });
  const tools = createVmTools(state, worker);
  await invoke(tools, "vm_update_requirement", {
    requirement: {
      schemaVersion: "1.0",
      task: { name: "normalize-csharp", mode: "create", vmVersion: "4.4.0" },
      scripts: [{
        id: "normalize",
        carrier: "csharp-module",
        name: "Normalize",
        procedure: "流程1",
        execution: { mode: "once" },
        source: "public partial class UserScript : ScriptMethods, IProcessMethods { public void Init() {} public bool Process() { return true; } }",
        inputs: [],
        outputs: [],
        operations: [],
        dependencies: [{ name: "Script.Methods", referenceType: 4 }],
      }],
      connections: [],
    },
  });
  const requirement = state.snapshot().requirement;
  const scripts = requirement?.scripts;
  assert.ok(Array.isArray(scripts));
  const script = scripts[0] as Record<string, unknown>;
  assert.match(String(script.source), /^using Script\.Methods;/);
  assert.deepEqual(script.dependencies, []);
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
