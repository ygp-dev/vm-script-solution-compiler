import { createInterface } from "node:readline";
import type { TaskContextInput } from "./domain/types.js";
import type { VmAgentRuntime } from "./runtime.js";

interface RpcRequest {
  id?: string | number;
  command?: string;
  arguments?: Record<string, unknown>;
}

export class AgentRpcHost {
  private stopping = false;
  private readonly runs = new Set<Promise<void>>();

  constructor(private readonly runtime: VmAgentRuntime) {}

  async run(): Promise<void> {
    this.runtime.subscribe((event) => this.write({ type: "event", event }));
    const input = createInterface({ input: process.stdin, crlfDelay: Infinity });
    for await (const line of input) {
      if (this.stopping || !line.trim()) continue;
      let request: RpcRequest;
      try {
        request = JSON.parse(line.replace(/^\uFEFF/, "")) as RpcRequest;
      } catch (error) {
        this.write({
          type: "response",
          id: null,
          ok: false,
          error: { code: "INVALID_JSON", message: error instanceof Error ? error.message : String(error) },
        });
        continue;
      }
      void this.dispatch(request);
    }
    await Promise.allSettled(this.runs);
  }

  private async dispatch(request: RpcRequest): Promise<void> {
    const id = request.id ?? null;
    const command = request.command ?? "";
    const args = request.arguments ?? {};
    try {
      switch (command) {
        case "initialize":
        case "get_state":
          this.success(id, this.runtime.snapshot());
          return;
        case "list_sessions":
          this.success(id, await this.runtime.listSessions());
          return;
        case "clear_sessions": {
          const deleted = await this.runtime.clearSessions();
          this.success(id, { deleted, snapshot: this.runtime.snapshot() });
          return;
        }
        case "new_session":
          await this.runtime.newSession();
          this.success(id, this.runtime.snapshot());
          return;
        case "resume_session":
          await this.runtime.resumeSession(requiredString(args, "file"));
          this.success(id, this.runtime.snapshot());
          return;
        case "prompt": {
          const context: TaskContextInput = {
            mode: optionalMode(args.mode),
            baseSolution: optionalString(args.baseSolution),
            outputDirectory: optionalString(args.outputDirectory),
          };
          const run = this.runtime.prompt(requiredString(args, "text"), context)
            .then(() => this.write({ type: "run_completed", id, ok: true, state: this.runtime.snapshot() }))
            .catch((error) => this.write({
              type: "run_completed",
              id,
              ok: false,
              error: serializeError(error),
              state: this.runtime.snapshot(),
            }))
            .finally(() => this.runs.delete(run));
          this.runs.add(run);
          this.success(id, { accepted: true, sessionId: this.runtime.snapshot().sessionId });
          return;
        }
        case "steer":
          await this.runtime.steer(requiredString(args, "text"));
          this.success(id, { accepted: true });
          return;
        case "follow_up":
          await this.runtime.followUp(requiredString(args, "text"));
          this.success(id, { accepted: true });
          return;
        case "abort":
          await this.runtime.abort();
          this.success(id, { aborted: true });
          return;
        case "record_user_validation":
          this.success(id, this.runtime.recordUserValidation(requiredString(args, "note")));
          return;
        case "shutdown":
          this.stopping = true;
          await this.runtime.dispose();
          this.success(id, { acknowledged: true });
          return;
        default:
          throw Object.assign(new Error(`Unknown Agent RPC command: ${command}`), { code: "COMMAND_NOT_FOUND" });
      }
    } catch (error) {
      this.write({ type: "response", id, ok: false, error: serializeError(error) });
    }
  }

  private success(id: string | number | null, result: unknown): void {
    this.write({ type: "response", id, ok: true, result });
  }

  private write(value: unknown): void {
    process.stdout.write(`${JSON.stringify(value, errorReplacer)}\n`);
  }
}

function requiredString(value: Record<string, unknown>, name: string): string {
  const result = value[name];
  if (typeof result !== "string" || !result.trim()) {
    throw Object.assign(new Error(`Missing string argument: ${name}`), { code: "INVALID_ARGUMENT" });
  }
  return result;
}

function optionalString(value: unknown): string | undefined {
  return typeof value === "string" ? value : undefined;
}

function optionalMode(value: unknown): TaskContextInput["mode"] {
  return value === "create" || value === "patch" || value === "unknown" ? value : undefined;
}

function serializeError(error: unknown): { code: string; message: string } {
  if (error instanceof Error) {
    const code = "code" in error && typeof error.code === "string" ? error.code : "AGENT_FAILED";
    return { code, message: error.message };
  }
  return { code: "AGENT_FAILED", message: String(error) };
}

function errorReplacer(_key: string, value: unknown): unknown {
  if (value instanceof Error) return serializeError(value);
  if (typeof value === "bigint") return value.toString();
  return value;
}
