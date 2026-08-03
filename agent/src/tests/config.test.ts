import test from "node:test";
import assert from "node:assert/strict";
import os from "node:os";
import path from "node:path";
import { fileURLToPath } from "node:url";
import { loadConfiguration } from "../config.js";

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

test("Agent defaults to an early-compaction 80K working window", () => {
  const previous = process.env.VM_SCRIPT_AI_CONTEXT_WINDOW;
  delete process.env.VM_SCRIPT_AI_CONTEXT_WINDOW;
  try {
    const config = loadConfiguration([
      "--repository-root", repositoryRoot,
      "--worker", workerPath,
      "--data-directory", path.join(os.tmpdir(), "vm-agent-config-test"),
    ]);
    assert.equal(config.contextWindow, 80_000);
  } finally {
    if (previous === undefined) delete process.env.VM_SCRIPT_AI_CONTEXT_WINDOW;
    else process.env.VM_SCRIPT_AI_CONTEXT_WINDOW = previous;
  }
});
