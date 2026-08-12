import assert from "node:assert/strict";
import fs from "node:fs";
import http from "node:http";
import os from "node:os";
import path from "node:path";
import { spawn } from "node:child_process";
import readline from "node:readline";

const root = path.resolve(process.argv[2]);
const node = path.join(root, ".runtime", "node-v22.19.0-win-x64", "node.exe");
const agentScript = path.join(root, "agent", "dist", "main.js");
const fixture = path.join(root, "tests", "fixtures", "m3-shell-create.json");
const fakeServer = path.join(root, "tests", "fake-agent-responses-server.mjs");
const temp = fs.mkdtempSync(path.join(os.tmpdir(), "vm-script-agent-interrupt-"));
const ready = path.join(temp, "ready");
const output = path.join(temp, "outputs");
const data = path.join(temp, "data");
fs.mkdirSync(output, { recursive: true });

function wait(ms) { return new Promise((resolve) => setTimeout(resolve, ms)); }

function listenPort() {
  return new Promise((resolve, reject) => {
    const server = http.createServer();
    server.once("error", reject);
    server.listen(0, "127.0.0.1", () => {
      const port = server.address().port;
      server.close(() => resolve(port));
    });
  });
}

function send(process, request) {
  process.stdin.write(`${JSON.stringify(request)}\n`);
}

function waitFor(messages, predicate, timeoutMs = 20_000) {
  const existing = messages.find(predicate);
  if (existing) return Promise.resolve(existing);
  return new Promise((resolve, reject) => {
    const timer = setTimeout(() => {
      const diagnostics = globalThis.__interruptDiagnostics ?? {};
      const error = new Error(`Timed out waiting for Agent message. Seen: ${messages.slice(-8).map((item) => item.type).join(", ")}; agent=${diagnostics.agentErrors ?? ""}; server=${diagnostics.serverErrors ?? ""}`);
      reject(error);
    }, timeoutMs);
    const check = (message) => {
      if (!predicate(message)) return;
      clearTimeout(timer);
      resolve(message);
    };
    messages.waiters ??= [];
    messages.waiters.push(check);
  });
}

const port = await listenPort();
const server = spawn(node, [fakeServer, String(port), fixture, output, ready, "3", "5000", "0"], {
  cwd: root,
  stdio: ["ignore", "pipe", "pipe"],
});
const serverErrors = [];
server.stderr.on("data", (chunk) => serverErrors.push(String(chunk)));
globalThis.__interruptDiagnostics = { serverErrors: "", agentErrors: "" };
const deadline = Date.now() + 10_000;
while (!fs.existsSync(ready) && Date.now() < deadline) await wait(50);
assert.ok(fs.existsSync(ready), serverErrors.join("") || "fake Responses server did not start");

const agent = spawn(node, [agentScript, "--repository-root", root, "--data-directory", data, "--output", output], {
  cwd: root,
  env: {
    ...process.env,
    VM_SCRIPT_COMPILER_HOME: root,
    VM_SCRIPT_DOMAIN_WORKER: path.join(root, "src", "VmScriptCompiler.DomainWorker", "bin", "Release", "net8.0", "vm-script-domain-worker.dll"),
    VM_SCRIPT_AI_PROVIDER: "openai-responses",
    VM_SCRIPT_AI_ENDPOINT: `http://127.0.0.1:${port}/v1`,
    VM_SCRIPT_AI_MODEL: "interrupt-smoke-model",
    VM_SCRIPT_AI_API_KEY: "offline-agent-key",
  },
  stdio: ["pipe", "pipe", "pipe"],
});
const messages = [];
const lines = readline.createInterface({ input: agent.stdout });
lines.on("line", (line) => {
  if (!line.trim()) return;
  const message = JSON.parse(line);
  messages.push(message);
  for (const waiter of messages.waiters ?? []) waiter(message);
  messages.waiters = (messages.waiters ?? []).filter((waiter) => waiter !== undefined);
});
const agentErrors = [];
agent.stderr.on("data", (chunk) => agentErrors.push(String(chunk)));
globalThis.__interruptDiagnostics.agentErrors = agentErrors;
globalThis.__interruptDiagnostics.serverErrors = serverErrors;

try {
  send(agent, { id: "init", command: "initialize", arguments: {} });
  const init = await waitFor(messages, (message) => message.type === "response" && message.id === "init");
  assert.equal(init.ok, true, JSON.stringify(init));

  send(agent, {
    id: "run",
    command: "prompt",
    arguments: {
      text: "Create a CSharp A plus B script solution and complete deterministic offline validation.",
      mode: "create",
      outputDirectory: output,
    },
  });
  await waitFor(messages, (message) =>
    message.type === "event" &&
    message.event?.type === "pi" &&
    message.event.event?.type === "tool_execution_end" &&
    message.event.event?.toolName === "vm_compile_solution",
  );

  send(agent, { id: "abort", command: "abort", arguments: {} });
  const abort = await waitFor(messages, (message) => message.type === "response" && message.id === "abort");
  assert.equal(abort.ok, true, JSON.stringify(abort));
  send(agent, { id: "state-after-abort", command: "get_state", arguments: {} });
  const interrupted = await waitFor(messages, (message) => message.type === "response" && message.id === "state-after-abort");
  assert.equal(interrupted.ok, true, JSON.stringify(interrupted));
  assert.equal(interrupted.result.canContinue, true, JSON.stringify(interrupted.result));
  assert.equal(interrupted.result.runStatus, "interrupted", JSON.stringify(interrupted.result));

  send(agent, { id: "continue", command: "continue", arguments: {} });
  const continued = await waitFor(messages, (message) => message.type === "run_completed" && message.id === "continue", 30_000);
  assert.equal(continued.ok, true, JSON.stringify(continued));
  assert.equal(continued.state.canContinue, false, JSON.stringify(continued.state));
  assert.notEqual(continued.state.runStatus, "running");
  assert.equal(continued.state.state.phase, "offline-validated", JSON.stringify(continued.state.state));

  send(agent, { id: "shutdown", command: "shutdown", arguments: {} });
  const shutdown = await waitFor(messages, (message) => message.type === "response" && message.id === "shutdown");
  assert.equal(shutdown.ok, true, JSON.stringify(shutdown));
  console.log(JSON.stringify({ ok: true, interrupted: true, continued: true, phase: continued.state.state.phase }));
} finally {
  lines.close();
  if (!agent.killed) agent.kill();
  if (!server.killed) server.kill();
  fs.rmSync(temp, { recursive: true, force: true });
  if (agentErrors.length && process.exitCode) console.error(agentErrors.join(""));
}
