import assert from "node:assert/strict";
import fs from "node:fs";
import os from "node:os";
import path from "node:path";
import test from "node:test";
import { VmAgentRuntime } from "../runtime.js";

test("Agent history cleanup deletes only listed session files", async () => {
  const root = fs.mkdtempSync(path.join(os.tmpdir(), "vm-agent-history-"));
  const sessionsDirectory = path.join(root, "sessions");
  fs.mkdirSync(sessionsDirectory);
  const first = path.join(sessionsDirectory, "first.jsonl");
  const second = path.join(sessionsDirectory, "second.jsonl");
  fs.writeFileSync(first, "{}\n");
  fs.writeFileSync(second, "{}\n");

  const runtime = {
    config: { repositoryRoot: root, sessionsDirectory },
    ensureIdle() {},
    listSessions: async () => [{ path: first }, { path: second }],
    replaceSession: async () => {},
  } as unknown as VmAgentRuntime;

  try {
    const deleted = await VmAgentRuntime.prototype.clearSessions.call(runtime);
    assert.equal(deleted, 2);
    assert.equal(fs.existsSync(first), false);
    assert.equal(fs.existsSync(second), false);
  } finally {
    fs.rmSync(root, { recursive: true, force: true });
  }
});

test("Agent history cleanup rejects paths outside the session directory", async () => {
  const root = fs.mkdtempSync(path.join(os.tmpdir(), "vm-agent-history-boundary-"));
  const sessionsDirectory = path.join(root, "sessions");
  const outside = path.join(root, "outside.jsonl");
  fs.mkdirSync(sessionsDirectory);
  fs.writeFileSync(outside, "{}\n");

  const runtime = {
    config: { repositoryRoot: root, sessionsDirectory },
    ensureIdle() {},
    listSessions: async () => [{ path: outside }],
    replaceSession: async () => {},
  } as unknown as VmAgentRuntime;

  try {
    await assert.rejects(
      VmAgentRuntime.prototype.clearSessions.call(runtime),
      (error: unknown) =>
        error instanceof Error &&
        (error as Error & { code?: string }).code === "SESSION_PATH_INVALID",
    );
    assert.equal(fs.existsSync(outside), true);
  } finally {
    fs.rmSync(root, { recursive: true, force: true });
  }
});
