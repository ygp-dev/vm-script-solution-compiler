import test from "node:test";
import assert from "node:assert/strict";
import { PRIMARY_VM_TOOL_NAMES } from "../runtime.js";
import { createVmTools } from "../tools/vm-tools.js";
import type { VmDomainState } from "../domain/state.js";
import type { DomainWorkerClient } from "../tools/domain-worker-client.js";

test("Agent session registers both SOL translation tools", () => {
  const available = new Set(createVmTools(
    {} as VmDomainState,
    {} as DomainWorkerClient,
  ).map((tool) => tool.name));
  for (const name of PRIMARY_VM_TOOL_NAMES) assert.ok(available.has(name), name);
  assert.ok(PRIMARY_VM_TOOL_NAMES.includes("vm_translation_inventory"));
  assert.ok(PRIMARY_VM_TOOL_NAMES.includes("vm_translate_solution"));
});
