import { spawn, type ChildProcessWithoutNullStreams } from "node:child_process";
import { createInterface, type Interface } from "node:readline";
import path from "node:path";

interface WorkerResponse<T> {
  id: string | number | null;
  ok: boolean;
  result?: T;
  error?: {
    code: string;
    message: string;
    details?: unknown;
  };
}

interface PendingCall {
  resolve(value: unknown): void;
  reject(error: Error): void;
  cleanup(): void;
}

export class DomainWorkerError extends Error {
  constructor(
    public readonly code: string,
    message: string,
    public readonly details?: unknown,
  ) {
    super(message);
    this.name = "DomainWorkerError";
  }
}

export interface DomainWorkerClientOptions {
  workerPath: string;
  repositoryRoot: string;
  onDiagnostic?: (message: string) => void;
}

export class DomainWorkerClient {
  private process?: ChildProcessWithoutNullStreams;
  private reader?: Interface;
  private sequence = 0;
  private readonly pending = new Map<string, PendingCall>();
  private closing = false;

  constructor(private readonly options: DomainWorkerClientOptions) {}

  async start(): Promise<void> {
    if (this.process && !this.process.killed) return;
    this.closing = false;
    const workerPath = path.resolve(this.options.workerPath);
    const isDll = workerPath.toLowerCase().endsWith(".dll");
    const executable = isDll ? "dotnet" : workerPath;
    const args = isDll
      ? [workerPath, "--repository-root", this.options.repositoryRoot]
      : ["--repository-root", this.options.repositoryRoot];

    const child = spawn(executable, args, {
      cwd: this.options.repositoryRoot,
      windowsHide: true,
      stdio: ["pipe", "pipe", "pipe"],
      env: {
        ...process.env,
        VM_SCRIPT_COMPILER_HOME: this.options.repositoryRoot,
      },
    });
    this.process = child;
    child.stdin.setDefaultEncoding("utf8");
    this.reader = createInterface({ input: child.stdout, crlfDelay: Infinity });
    this.reader.on("line", (line) => this.handleLine(line));
    child.stderr.setEncoding("utf8");
    child.stderr.on("data", (chunk: string) => this.options.onDiagnostic?.(chunk.trimEnd()));
    child.on("error", (error) => this.failAll(error));
    child.on("exit", (code, signal) => {
      this.reader?.close();
      this.reader = undefined;
      this.process = undefined;
      if (!this.closing) {
        this.failAll(new DomainWorkerError(
          "DOMAIN_WORKER_EXITED",
          `Domain Worker exited unexpectedly (code=${code ?? "null"}, signal=${signal ?? "null"}).`,
        ));
      }
    });

    await this.call("initialize", {});
  }

  async call<T>(
    command: string,
    argumentsValue: Record<string, unknown>,
    signal?: AbortSignal,
  ): Promise<T> {
    if (!this.process || this.process.killed) await this.start();
    const child = this.process;
    if (!child) throw new DomainWorkerError("DOMAIN_WORKER_NOT_AVAILABLE", "Domain Worker is not running.");
    if (signal?.aborted) throw signal.reason instanceof Error ? signal.reason : new Error("Operation aborted.");

    const id = `worker-${++this.sequence}`;
    return await new Promise<T>((resolve, reject) => {
      const abort = () => {
        this.terminate();
        reject(signal?.reason instanceof Error ? signal.reason : new Error("Operation aborted."));
      };
      signal?.addEventListener("abort", abort, { once: true });
      this.pending.set(id, {
        resolve: (value) => resolve(value as T),
        reject,
        cleanup: () => signal?.removeEventListener("abort", abort),
      });
      child.stdin.write(`${JSON.stringify({ id, command, arguments: argumentsValue })}\n`, "utf8", (error) => {
        if (!error) return;
        const pending = this.pending.get(id);
        this.pending.delete(id);
        pending?.cleanup();
        reject(error);
      });
    });
  }

  async dispose(): Promise<void> {
    if (!this.process) return;
    this.closing = true;
    try {
      await this.call("shutdown", {});
    } catch {
      this.terminate();
    }
  }

  private handleLine(line: string): void {
    let response: WorkerResponse<unknown>;
    try {
      response = JSON.parse(line) as WorkerResponse<unknown>;
    } catch (error) {
      this.failAll(new DomainWorkerError(
        "DOMAIN_WORKER_PROTOCOL_INVALID",
        `Domain Worker returned invalid JSONL: ${error instanceof Error ? error.message : String(error)}`,
      ));
      this.terminate();
      return;
    }
    const id = String(response.id ?? "");
    const pending = this.pending.get(id);
    if (!pending) return;
    this.pending.delete(id);
    pending.cleanup();
    if (response.ok) {
      pending.resolve(response.result);
      return;
    }
    pending.reject(new DomainWorkerError(
      response.error?.code ?? "DOMAIN_WORKER_FAILED",
      response.error?.message ?? "Domain Worker command failed.",
      response.error?.details,
    ));
  }

  private failAll(error: Error): void {
    for (const pending of this.pending.values()) {
      pending.cleanup();
      pending.reject(error);
    }
    this.pending.clear();
  }

  private terminate(): void {
    this.closing = true;
    this.reader?.close();
    this.reader = undefined;
    this.process?.kill();
    this.process = undefined;
  }
}
