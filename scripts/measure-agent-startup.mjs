import path from "node:path";
import { pathToFileURL } from "node:url";

const root = path.resolve(import.meta.dirname, "..");
const payload = path.resolve(option("--payload") ?? path.join(root, "dist", "Desktop"));
const data = path.resolve(option("--data") ?? path.join(process.env.LOCALAPPDATA ?? root, "VM Script Compiler", "agent"));
const output = path.resolve(option("--output") ?? path.join(root, "outputs"));

const started = performance.now();
const [{ loadConfiguration }, { configureModel }, { VmAgentRuntime }] = await Promise.all([
  import(pathToFileURL(path.join(payload, "agent", "dist", "config.js"))),
  import(pathToFileURL(path.join(payload, "agent", "dist", "model.js"))),
  import(pathToFileURL(path.join(payload, "agent", "dist", "runtime.js"))),
]);
const importedAt = performance.now();
const config = loadConfiguration([
  "--repository-root", payload,
  "--data-directory", data,
  "--output", output,
]);
const configuredAt = performance.now();
const configured = await configureModel(config);
const modelAt = performance.now();
const runtime = new VmAgentRuntime(config, configured.runtime, configured.model);
const constructedAt = performance.now();
await runtime.initialize();
const initializedAt = performance.now();
const snapshot = runtime.snapshot();
await runtime.dispose();
const disposedAt = performance.now();

console.log(JSON.stringify({
  ok: true,
  data,
  messages: snapshot.messages.length,
  importMs: round(importedAt - started),
  configMs: round(configuredAt - importedAt),
  modelMs: round(modelAt - configuredAt),
  initializeMs: round(initializedAt - constructedAt),
  disposeMs: round(disposedAt - initializedAt),
  totalMs: round(disposedAt - started),
}));

function option(name) {
  const index = process.argv.indexOf(name);
  return index >= 0 ? process.argv[index + 1] : undefined;
}

function round(value) {
  return Math.round(value * 100) / 100;
}
