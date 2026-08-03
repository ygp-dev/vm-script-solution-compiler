import fs from "node:fs";
import os from "node:os";
import path from "node:path";
import { fileURLToPath } from "node:url";
export type AgentThinkingLevel = "minimal" | "low" | "medium" | "high" | "xhigh" | "max";

export interface AgentConfiguration {
  repositoryRoot: string;
  agentRoot: string;
  dataDirectory: string;
  sessionsDirectory: string;
  workerPath: string;
  outputDirectory: string;
  provider: "openai-responses" | "openai-compatible";
  endpoint: string;
  model: string;
  apiKey: string;
  thinkingLevel: AgentThinkingLevel;
  contextWindow: number;
  maxTokens: number;
}

export function loadConfiguration(args: string[]): AgentConfiguration {
  const moduleDirectory = path.dirname(fileURLToPath(import.meta.url));
  const agentRoot = findAgentRoot(moduleDirectory);
  const repositoryRoot = findRepositoryRoot(
    option(args, "--repository-root") ??
    process.env.VM_SCRIPT_COMPILER_HOME ??
    path.resolve(agentRoot, ".."),
  );
  const dataDirectory = path.resolve(
    option(args, "--data-directory") ??
    process.env.VM_SCRIPT_AGENT_DATA ??
    path.join(process.env.LOCALAPPDATA ?? path.join(os.homedir(), ".vm-script-agent"), "VM Script Compiler", "agent"),
  );
  const providerValue = (
    option(args, "--provider") ??
    process.env.VM_SCRIPT_AI_PROVIDER ??
    "openai-responses"
  ).toLowerCase();
  const provider = providerValue === "openai-compatible"
    ? "openai-compatible"
    : "openai-responses";
  const endpoint = normalizeEndpoint(
    option(args, "--endpoint") ??
    process.env.VM_SCRIPT_AI_ENDPOINT ??
    "https://api.openai.com/v1",
    provider,
  );
  const model = option(args, "--model") ?? process.env.VM_SCRIPT_AI_MODEL ?? "";
  const apiKey = process.env.VM_SCRIPT_AI_API_KEY ?? "";
  const thinkingLevel = parseThinkingLevel(
    option(args, "--thinking") ??
    process.env.VM_SCRIPT_AI_REASONING_EFFORT ??
    "high",
  );
  const outputDirectory = path.resolve(
    option(args, "--output") ??
    process.env.VM_SCRIPT_OUTPUT_DIRECTORY ??
    path.join(os.homedir(), "Documents", "VM Script Compiler", "outputs"),
  );

  return {
    repositoryRoot,
    agentRoot,
    dataDirectory,
    sessionsDirectory: path.join(dataDirectory, "sessions"),
    workerPath: resolveWorkerPath(
      option(args, "--worker") ?? process.env.VM_SCRIPT_DOMAIN_WORKER,
      repositoryRoot,
      agentRoot,
    ),
    outputDirectory,
    provider,
    endpoint,
    model,
    apiKey,
    thinkingLevel,
    // Pi compacts at contextWindow - 16K. Keep long source-repair sessions
    // below roughly 64K tokens instead of letting them grow beyond 100K.
    contextWindow: positiveInteger(process.env.VM_SCRIPT_AI_CONTEXT_WINDOW, 80_000),
    maxTokens: positiveInteger(process.env.VM_SCRIPT_AI_MAX_TOKENS, 32_768),
  };
}

function option(args: string[], name: string): string | undefined {
  const index = args.indexOf(name);
  return index >= 0 ? args[index + 1] : undefined;
}

function findAgentRoot(start: string): string {
  for (let directory = path.resolve(start); ; directory = path.dirname(directory)) {
    if (fs.existsSync(path.join(directory, "package.json")) &&
        fs.existsSync(path.join(directory, "resources", "SYSTEM.md"))) return directory;
    const parent = path.dirname(directory);
    if (parent === directory) break;
  }
  return path.resolve(start, "..");
}

function findRepositoryRoot(start: string): string {
  for (let directory = path.resolve(start); ; directory = path.dirname(directory)) {
    if (fs.existsSync(path.join(directory, "schemas", "requirement.schema.json")) &&
        fs.existsSync(path.join(directory, "resources", "vm", "4.4.0", "manifest.json"))) return directory;
    const parent = path.dirname(directory);
    if (parent === directory) break;
  }
  throw new Error(`Cannot locate VM Script Compiler repository from ${start}.`);
}

function resolveWorkerPath(
  explicit: string | undefined,
  repositoryRoot: string,
  agentRoot: string,
): string {
  const candidates = [
    explicit,
    path.join(agentRoot, "worker", "vm-script-domain-worker.exe"),
    path.join(repositoryRoot, "worker", "vm-script-domain-worker.exe"),
    path.join(
      repositoryRoot,
      "src",
      "VmScriptCompiler.DomainWorker",
      "bin",
      "Release",
      "net8.0",
      "vm-script-domain-worker.dll",
    ),
  ].filter((value): value is string => Boolean(value));
  const found = candidates.find((candidate) => fs.existsSync(path.resolve(candidate)));
  if (!found) throw new Error(`Cannot locate vm-script-domain-worker. Tried: ${candidates.join(", ")}`);
  return path.resolve(found);
}

function normalizeEndpoint(
  value: string,
  provider: AgentConfiguration["provider"],
): string {
  let endpoint = value.trim().replace(/\/+$/, "");
  const suffix = provider === "openai-responses" ? "/responses" : "/chat/completions";
  if (endpoint.toLowerCase().endsWith(suffix)) endpoint = endpoint.slice(0, -suffix.length);
  return endpoint;
}

function parseThinkingLevel(value: string): AgentThinkingLevel {
  const normalized = value.toLowerCase();
  if (["minimal", "low", "medium", "high", "xhigh", "max"].includes(normalized)) {
    return normalized as AgentThinkingLevel;
  }
  return "high";
}

function positiveInteger(value: string | undefined, fallback: number): number {
  const parsed = Number.parseInt(value ?? "", 10);
  return Number.isFinite(parsed) && parsed > 0 ? parsed : fallback;
}
