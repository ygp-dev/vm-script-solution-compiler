import process from "node:process";
import { loadConfiguration } from "./config.js";
import { configureModel } from "./model.js";
import { AgentRpcHost } from "./rpc-host.js";
import { VmAgentRuntime } from "./runtime.js";

process.stdin.setEncoding("utf8");
process.stdout.setDefaultEncoding("utf8");
process.stderr.setDefaultEncoding("utf8");

try {
  assertNodeVersion();
  trace("configuration");
  const config = loadConfiguration(process.argv.slice(2));
  trace("model");
  const configuredModel = await configureModel(config);
  trace("runtime");
  const runtime = new VmAgentRuntime(config, configuredModel.runtime, configuredModel.model);
  trace("initialize");
  await runtime.initialize();
  trace("rpc");
  const host = new AgentRpcHost(runtime);
  await host.run();
} catch (error) {
  process.stderr.write(`${JSON.stringify({
    ok: false,
    error: "AGENT_STARTUP_FAILED",
    message: error instanceof Error ? error.message : String(error),
  })}\n`);
  process.exitCode = 2;
}

function trace(stage: string): void {
  if (process.env.VM_SCRIPT_AGENT_TRACE === "1") {
    process.stderr.write(`${JSON.stringify({ type: "startup_trace", stage, timestampUtc: new Date().toISOString() })}\n`);
  }
}

function assertNodeVersion(): void {
  const [major, minor] = process.versions.node.split(".").map(Number);
  if (major < 22 || (major === 22 && minor < 19)) {
    throw new Error(`Pi 0.82.1 requires Node >=22.19.0; current runtime is ${process.versions.node}.`);
  }
}
