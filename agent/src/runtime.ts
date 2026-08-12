import fs from "node:fs";
import path from "node:path";
import {
  type AgentSession,
  type AgentSessionEvent,
} from "../node_modules/@earendil-works/pi-coding-agent/dist/core/agent-session.js";
import { DefaultResourceLoader } from "../node_modules/@earendil-works/pi-coding-agent/dist/core/resource-loader.js";
import {
  SessionManager,
  type SessionInfo,
} from "../node_modules/@earendil-works/pi-coding-agent/dist/core/session-manager.js";
import { createAgentSession } from "../node_modules/@earendil-works/pi-coding-agent/dist/core/sdk.js";
import type { Model } from "@earendil-works/pi-ai";
import type { ModelRuntime } from "../node_modules/@earendil-works/pi-coding-agent/dist/core/model-runtime.js";
import type { AgentConfiguration } from "./config.js";
import { VmDomainState } from "./domain/state.js";
import type { TaskContextInput, VmTaskState } from "./domain/types.js";
import { DomainWorkerClient } from "./tools/domain-worker-client.js";
import { createVmTools } from "./tools/vm-tools.js";
import { buildVmSystemPrompt } from "./system-prompt.js";

export interface AgentRuntimeEvent {
  type: "pi" | "domain" | "diagnostic";
  sessionId?: string;
  event?: AgentSessionEvent;
  state?: VmTaskState;
  message?: string;
}

export class VmAgentRuntime {
  private session?: AgentSession;
  private sessionManager?: SessionManager;
  private domain?: VmDomainState;
  private unsubscribe?: () => void;
  private interrupted = false;
  private continuationActive = false;
  private readonly listeners = new Set<(event: AgentRuntimeEvent) => void>();
  private readonly worker: DomainWorkerClient;

  constructor(
    private readonly config: AgentConfiguration,
    private readonly modelRuntime: ModelRuntime,
    private readonly model: Model<any>,
  ) {
    fs.mkdirSync(config.dataDirectory, { recursive: true });
    fs.mkdirSync(config.sessionsDirectory, { recursive: true });
    fs.mkdirSync(config.outputDirectory, { recursive: true });
    this.worker = new DomainWorkerClient({
      workerPath: config.workerPath,
      repositoryRoot: config.repositoryRoot,
      outputDirectory: config.outputDirectory,
      onDiagnostic: (message) => this.emit({ type: "diagnostic", message }),
    });
  }

  async initialize(): Promise<void> {
    await this.worker.start();
    await this.replaceSession(SessionManager.continueRecent(
      this.config.repositoryRoot,
      this.config.sessionsDirectory,
    ));
  }

  subscribe(listener: (event: AgentRuntimeEvent) => void): () => void {
    this.listeners.add(listener);
    return () => this.listeners.delete(listener);
  }

  snapshot(): {
    sessionId: string;
    sessionFile?: string;
    isStreaming: boolean;
    canContinue: boolean;
    runStatus: "idle" | "running" | "interrupted";
    state: VmTaskState;
    messages: unknown[];
    model: { provider: string; id: string };
    outputDirectory: string;
  } {
    const session = this.requireSession();
    return {
      sessionId: session.sessionId,
      sessionFile: session.sessionFile,
      isStreaming: session.isStreaming || this.continuationActive,
      canContinue: this.canContinue(),
      runStatus: session.isStreaming || this.continuationActive
        ? "running"
        : this.canContinue()
          ? "interrupted"
          : "idle",
      state: this.requireDomain().snapshot(),
      messages: session.messages,
      model: { provider: this.model.provider, id: this.model.id },
      outputDirectory: this.config.outputDirectory,
    };
  }

  async prompt(text: string, context: TaskContextInput = {}): Promise<void> {
    const session = this.requireSession();
    this.interrupted = false;
    if (!text.trim()) throw new Error("消息不能为空。");
    const state = this.requireDomain();
    state.setTaskContext(context);
    this.emit({ type: "domain", sessionId: session.sessionId, state: state.snapshot() });

    const snapshot = state.snapshot();
    const contextLines = [
      "[VM_TASK_CONTEXT]",
      `mode=${snapshot.intent}`,
      `baseSolution=${snapshot.baseSolution ?? ""}`,
      `outputDirectory=${snapshot.outputDirectory ?? this.config.outputDirectory}`,
      `vmVersion=${snapshot.targetVmVersion}`,
      "[/VM_TASK_CONTEXT]",
    ];
    await session.prompt(`${contextLines.join("\n")}\n\n${text}`);
  }

  async continue(text?: string): Promise<void> {
    const session = this.requireSession();
    if (session.isStreaming || this.continuationActive) {
      throw Object.assign(new Error("Agent 正在执行，不能重复继续。"), { code: "AGENT_BUSY" });
    }
    if (!this.canContinue()) {
      throw Object.assign(new Error("当前会话没有可继续的中断任务。"), { code: "CONTINUE_NOT_AVAILABLE" });
    }

    this.continuationActive = true;
    this.interrupted = false;
    try {
      await session.prompt(
        text?.trim() ||
          "继续当前 VM Script Compiler 任务。不要重新创建任务，也不要重复已经成功的工具调用；从上次中断的位置检查当前 Requirement、构建状态和已有产物，然后完成剩余工作。",
      );
    } catch (error) {
      this.interrupted = true;
      throw error;
    } finally {
      this.continuationActive = false;
    }
  }

  async steer(text: string): Promise<void> {
    await this.requireSession().steer(text);
  }

  async followUp(text: string): Promise<void> {
    await this.requireSession().followUp(text);
  }

  async abort(): Promise<void> {
    const session = this.requireSession();
    if (!session.isStreaming) return;
    this.interrupted = true;
    await session.abort();
    this.emit({
      type: "domain",
      sessionId: session.sessionId,
      state: this.requireDomain().snapshot(),
    });
  }

  async newSession(): Promise<void> {
    this.ensureIdle();
    this.interrupted = false;
    this.continuationActive = false;
    await this.replaceSession(SessionManager.create(
      this.config.repositoryRoot,
      this.config.sessionsDirectory,
    ));
  }

  async resumeSession(file: string): Promise<void> {
    this.ensureIdle();
    await this.replaceSession(SessionManager.open(
      path.resolve(file),
      this.config.sessionsDirectory,
      this.config.repositoryRoot,
    ));
  }

  async listSessions(): Promise<SessionInfo[]> {
    return await SessionManager.list(this.config.repositoryRoot, this.config.sessionsDirectory);
  }

  async clearSessions(): Promise<number> {
    this.ensureIdle();
    const sessions = await this.listSessions();
    const sessionsRoot = path.resolve(this.config.sessionsDirectory);
    const files = sessions.map((session) => path.resolve(session.path));
    for (const file of files) {
      const relative = path.relative(sessionsRoot, file);
      if (relative === ".." || relative.startsWith(".." + path.sep) || path.isAbsolute(relative)) {
        throw Object.assign(
          new Error(`Refusing to delete a session outside the Agent session directory: ${file}`),
          { code: "SESSION_PATH_INVALID" },
        );
      }
    }

    await this.replaceSession(SessionManager.create(
      this.config.repositoryRoot,
      this.config.sessionsDirectory,
    ));

    let deleted = 0;
    for (const file of files) {
      try {
        fs.unlinkSync(file);
        deleted++;
      } catch (error) {
        if ((error as NodeJS.ErrnoException).code !== "ENOENT") throw error;
      }
    }
    return deleted;
  }

  recordUserValidation(note: string): VmTaskState {
    const state = this.requireDomain();
    state.recordUserValidation(note);
    const snapshot = state.snapshot();
    this.emit({ type: "domain", sessionId: this.requireSession().sessionId, state: snapshot });
    return snapshot;
  }

  async dispose(): Promise<void> {
    this.unsubscribe?.();
    this.unsubscribe = undefined;
    this.session?.dispose();
    this.session = undefined;
    await this.worker.dispose();
  }

  private async replaceSession(sessionManager: SessionManager): Promise<void> {
    this.unsubscribe?.();
    this.unsubscribe = undefined;
    this.session?.dispose();

    const domain = new VmDomainState(sessionManager);
    const tools = createVmTools(domain, this.worker);
    const primaryToolNames = new Set([
      "vm_inspect_solution",
      "vm_update_requirement",
      "vm_compile_solution",
      "vm_read_build_report",
      "vm_record_user_validation",
    ]);
    const primaryTools = tools.filter((tool) => primaryToolNames.has(tool.name));
    const systemPrompt = buildVmSystemPrompt(this.config);
    const resourceLoader = new DefaultResourceLoader({
      cwd: this.config.repositoryRoot,
      agentDir: this.config.dataDirectory,
      additionalSkillPaths: [path.join(this.config.agentRoot, "resources", "skills")],
      additionalPromptTemplatePaths: [path.join(this.config.agentRoot, "resources", "prompts")],
      noExtensions: true,
      noContextFiles: true,
      systemPrompt,
    });
    await resourceLoader.reload();
    const created = await createAgentSession({
      cwd: this.config.repositoryRoot,
      agentDir: this.config.dataDirectory,
      modelRuntime: this.modelRuntime,
      model: this.model,
      thinkingLevel: this.config.thinkingLevel,
      sessionManager,
      resourceLoader,
      noTools: "builtin",
      tools: primaryTools.map((tool) => tool.name),
      customTools: primaryTools,
    });

    this.sessionManager = sessionManager;
    this.domain = domain;
    this.session = created.session;
    this.interrupted = hasContinuationCandidate(created.session);
    this.continuationActive = false;
    this.unsubscribe = created.session.subscribe((event) => {
      this.emit({ type: "pi", sessionId: created.session.sessionId, event });
      if (event.type === "tool_execution_end") {
        this.emit({
          type: "domain",
          sessionId: created.session.sessionId,
          state: domain.snapshot(),
        });
      }
    });
    this.emit({
      type: "domain",
      sessionId: created.session.sessionId,
      state: domain.snapshot(),
    });
  }

  private requireSession(): AgentSession {
    if (!this.session) throw new Error("Agent Runtime 尚未初始化。");
    return this.session;
  }

  private requireDomain(): VmDomainState {
    if (!this.domain) throw new Error("VM 领域状态尚未初始化。");
    return this.domain;
  }

  private canContinue(): boolean {
    if (this.continuationActive) return false;
    if (this.interrupted) return true;
    return isContinuationMessage(this.session?.messages.at(-1));
  }

  private ensureIdle(): void {
    if (this.continuationActive) throw Object.assign(new Error("Agent 正在继续执行。"), { code: "AGENT_BUSY" });
    if (this.requireSession().isStreaming) throw new Error("Agent 正在执行，不能切换会话。");
  }

  private emit(event: AgentRuntimeEvent): void {
    for (const listener of this.listeners) listener(event);
  }
}

function hasContinuationCandidate(session: AgentSession): boolean {
  return isContinuationMessage(session.messages.at(-1));
}

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === "object" && value !== null;
}

function isContinuationMessage(value: unknown): boolean {
  return isRecord(value) &&
    value.role === "assistant" &&
    (value.stopReason === "aborted" || value.stopReason === "error");
}
