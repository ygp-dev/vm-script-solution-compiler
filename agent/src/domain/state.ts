import type { SessionManager } from "../../node_modules/@earendil-works/pi-coding-agent/dist/core/session-manager.js";
import type {
  CapabilityEvidence,
  DomainError,
  TaskContextInput,
  VmArtifact,
  VmTaskMode,
  VmTaskPhase,
  VmTaskState,
} from "./types.js";

const SESSION_ENTRY_TYPE = "vm-script-task-state";

function now(): string {
  return new Date().toISOString();
}

function initialState(): VmTaskState {
  const timestamp = now();
  return {
    version: 1,
    intent: "unknown",
    targetVmVersion: "4.4.0",
    phase: "idle",
    requirementRevision: 0,
    requirementRetry: {
      turnValidationAttempts: 0,
      totalValidationAttempts: 0,
      consecutiveSameFailures: 0,
      blocked: false,
    },
    confirmedFacts: [],
    unresolvedQuestions: [],
    capabilityEvidence: [],
    artifacts: [],
    completion: {
      requirementValid: false,
      solutionBuilt: false,
      offlineValidationPassed: false,
      vmRuntimeValidationRequired: true,
      userValidated: false,
    },
    createdUtc: timestamp,
    updatedUtc: timestamp,
  };
}

function clone<T>(value: T): T {
  return structuredClone(value);
}

export class VmDomainState {
  private state: VmTaskState;
  private batching = false;
  private turnTaskName?: string;

  constructor(private readonly sessionManager: SessionManager) {
    const restored = [...sessionManager.getEntries()]
      .reverse()
      .find((entry) => entry.type === "custom" && entry.customType === SESSION_ENTRY_TYPE);
    this.state = restored?.type === "custom" && restored.data
      ? normalizeRestoredState(clone(restored.data as VmTaskState))
      : initialState();
  }

  snapshot(): VmTaskState {
    return clone(this.state);
  }

  setTaskContext(input: TaskContextInput): VmTaskState {
    this.turnTaskName = undefined;
    this.state.requirementRetry.turnValidationAttempts = 0;
    this.state.requirementRetry.consecutiveSameFailures = 0;
    this.state.requirementRetry.lastFailureSignature = undefined;
    this.state.requirementRetry.blocked = false;
    this.state.unresolvedQuestions = this.state.unresolvedQuestions.filter(
      (question) => !question.startsWith("Requirement 自动修订已停止"),
    );
    const nextMode = input.mode && input.mode !== "unknown" ? input.mode : this.state.intent;
    if (nextMode !== "unknown") this.state.intent = nextMode;
    if (input.baseSolution !== undefined) this.state.baseSolution = input.baseSolution || undefined;
    if (input.outputDirectory !== undefined) this.state.outputDirectory = input.outputDirectory || undefined;
    if (this.state.intent === "patch" && !this.state.baseSolution) {
      this.state.unresolvedQuestions = unique([
        ...this.state.unresolvedQuestions,
        "Patch 模式缺少基底 SOL。",
      ]);
    } else {
      this.state.unresolvedQuestions = this.state.unresolvedQuestions.filter(
        (question) => question !== "Patch 模式缺少基底 SOL。",
      );
    }
    return this.commit();
  }

  beginTool(name: string, phase?: VmTaskPhase): VmTaskState {
    this.batching = true;
    if (phase) this.state.phase = phase;
    this.state.lastTool = { name, ok: false, timestampUtc: now() };
    return this.commit();
  }

  completeTool(name: string): VmTaskState {
    this.batching = false;
    this.state.lastTool = { name, ok: true, timestampUtc: now() };
    this.state.lastCompilerError = undefined;
    return this.commit();
  }

  recordRequirement(requirement: Record<string, unknown>): VmTaskState {
    const task = isRecord(requirement.task) ? requirement.task : undefined;
    const mode = task?.mode;
    if (mode !== "create" && mode !== "patch") {
      throw new Error("Requirement task.mode must be create or patch.");
    }
    const taskName = typeof task?.name === "string" ? task.name.trim() : "";
    if (!taskName) throw new Error("Requirement task.name cannot be empty.");
    if (this.turnTaskName && this.turnTaskName !== taskName) {
      throw new Error(
        `同一用户回合不能把任务从 ${this.turnTaskName} 改为 ${taskName}。` +
        "请修订当前任务，不要创建无关的试验方案。",
      );
    }
    this.turnTaskName ??= taskName;

    this.state.intent = mode;
    this.state.baseSolution = typeof task?.baseSolution === "string"
      ? task.baseSolution
      : this.state.baseSolution;
    this.state.targetVmVersion = typeof task?.vmVersion === "string"
      ? task.vmVersion
      : this.state.targetVmVersion;
    this.state.requirement = clone(requirement);
    this.state.requirementRevision += 1;
    this.state.phase = "drafting";
    this.state.completion.requirementValid = false;
    this.state.completion.solutionBuilt = false;
    this.state.completion.offlineValidationPassed = false;
    this.state.completion.userValidated = false;
    this.state.lastCompilerError = undefined;
    return this.commit();
  }

  recordRequirementValidation(result: unknown): VmTaskState {
    const ok = readBoolean(result, "ok");
    const issues = readValidationIssues(result);
    const signature = issues
      .map((issue) => `${issue.code}|${issue.path}`)
      .sort()
      .join(";");
    this.state.requirementRetry.turnValidationAttempts += 1;
    this.state.requirementRetry.totalValidationAttempts += 1;
    this.state.completion.requirementValid = ok;
    this.state.phase = ok ? "planned" : "drafting";
    if (!ok) {
      if (signature && signature === this.state.requirementRetry.lastFailureSignature) {
        this.state.requirementRetry.consecutiveSameFailures += 1;
      } else {
        this.state.requirementRetry.consecutiveSameFailures = 1;
        this.state.requirementRetry.lastFailureSignature = signature;
      }
      const retryBlocked =
        this.state.requirementRetry.turnValidationAttempts >= 5 ||
        this.state.requirementRetry.consecutiveSameFailures >= 3;
      this.state.requirementRetry.blocked = retryBlocked;
      if (retryBlocked) {
        this.state.phase = "blocked";
        this.state.unresolvedQuestions = unique([
          ...this.state.unresolvedQuestions,
          `Requirement 自动修订已停止：本回合校验 ${this.state.requirementRetry.turnValidationAttempts} 次，` +
          `同一错误连续 ${this.state.requirementRetry.consecutiveSameFailures} 次。`,
        ]);
      }
      this.state.unresolvedQuestions = unique([
        ...this.state.unresolvedQuestions,
        "Requirement 尚未通过确定性校验。",
      ]);
    } else {
      this.state.requirementRetry.blocked = false;
      this.state.unresolvedQuestions = this.state.unresolvedQuestions.filter(
        (question) =>
          question !== "Requirement 尚未通过确定性校验。" &&
          !question.startsWith("Requirement 自动修订已停止"),
      );
    }
    this.addEvidence({
      kind: "validation",
      summary: ok ? "Requirement 已通过确定性校验。" : "Requirement 校验失败。",
      data: result,
      timestampUtc: now(),
    });
    return this.commit();
  }

  recordPlan(result: unknown): VmTaskState {
    const ok = readBoolean(result, "ok");
    if (ok) this.state.phase = "planned";
    this.addEvidence({
      kind: "validation",
      summary: ok ? "已生成确定性构建计划。" : "无法生成确定性构建计划。",
      data: result,
      timestampUtc: now(),
    });
    return this.commit();
  }

  recordEnvironment(result: unknown): VmTaskState {
    this.addEvidence({
      kind: "environment",
      summary: readBoolean(result, "found") ? "已检测到 VisionMaster 环境。" : "未检测到可用 VisionMaster 环境。",
      data: result,
      timestampUtc: now(),
    });
    return this.commit();
  }

  recordSolutionInspection(file: string, result: unknown): VmTaskState {
    this.state.baseSolution = file;
    this.addEvidence({
      kind: "solution",
      summary: `已检查基底 SOL：${file}`,
      data: result,
      timestampUtc: now(),
    });
    return this.commit();
  }

  recordCapability(query: string | undefined, result: unknown): VmTaskState {
    this.addEvidence({
      kind: "capability",
      summary: query ? `已查询 VM 能力：${query}` : "已读取 VM 能力目录。",
      data: result,
      timestampUtc: now(),
    });
    return this.commit();
  }

  recordBuild(result: unknown): VmTaskState {
    const record = isRecord(result) ? result : {};
    this.state.phase = "built";
    this.state.completion.solutionBuilt = true;
    this.state.completion.offlineValidationPassed = false;
    this.state.completion.userValidated = false;
    this.state.requirementRetry.consecutiveSameFailures = 0;
    this.state.requirementRetry.lastFailureSignature = undefined;
    this.state.requirementRetry.blocked = false;
    this.addArtifact("task-directory", stringValue(record.taskDirectory));
    this.addArtifact("solution", stringValue(record.solutionFile));
    this.addArtifact("report", stringValue(record.reportFile));
    return this.commit();
  }

  recordOfflineValidation(file: string, result: unknown): VmTaskState {
    const ok = readBoolean(result, "ok");
    this.state.phase = ok ? "offline-validated" : "built";
    this.state.completion.offlineValidationPassed = ok;
    this.state.completion.userValidated = false;
    this.addEvidence({
      kind: "validation",
      summary: ok ? `SOL 已通过离线验证：${file}` : `SOL 离线验证失败：${file}`,
      data: result,
      timestampUtc: now(),
    });
    return this.commit();
  }

  recordReport(file: string, result: unknown): VmTaskState {
    this.addArtifact("report", file);
    this.addEvidence({
      kind: "report",
      summary: `已读取构建报告：${file}`,
      data: result,
      timestampUtc: now(),
    });
    return this.commit();
  }

  recordUserValidation(note: string): VmTaskState {
    if (!this.state.completion.offlineValidationPassed) {
      throw new Error("离线验证尚未通过，不能记录 VM 用户验收。");
    }
    if (!note.trim()) throw new Error("用户验收说明不能为空。");
    this.state.phase = "user-validated";
    this.state.completion.userValidated = true;
    this.state.confirmedFacts = unique([
      ...this.state.confirmedFacts,
      `VM 用户验收：${note.trim()}`,
    ]);
    return this.commit();
  }

  recordError(
    code: string,
    message: string,
    recoverability: DomainError["recoverability"] = recoveryFor(code),
  ): VmTaskState {
    this.batching = false;
    this.state.lastCompilerError = {
      code,
      message,
      recoverability,
      timestampUtc: now(),
    };
    if (recoverability === "automatic") {
      const signature = `${code}|${message}`;
      if (signature === this.state.requirementRetry.lastFailureSignature) {
        this.state.requirementRetry.consecutiveSameFailures += 1;
      } else {
        this.state.requirementRetry.consecutiveSameFailures = 1;
        this.state.requirementRetry.lastFailureSignature = signature;
      }
      const retryBlocked =
        this.state.requirementRetry.turnValidationAttempts >= 5 ||
        this.state.requirementRetry.consecutiveSameFailures >= 3;
      this.state.requirementRetry.blocked = retryBlocked;
      if (retryBlocked) {
        this.state.phase = "blocked";
        this.state.unresolvedQuestions = unique([
          ...this.state.unresolvedQuestions,
          `Requirement 自动修订已停止：本回合校验 ${this.state.requirementRetry.turnValidationAttempts} 次，` +
          `同一编译错误连续 ${this.state.requirementRetry.consecutiveSameFailures} 次。`,
        ]);
      }
    } else {
      this.state.phase = "blocked";
    }
    if (recoverability !== "automatic") {
      this.state.unresolvedQuestions = unique([
        ...this.state.unresolvedQuestions,
        `${code}: ${message}`,
      ]);
    }
    if (this.state.lastTool) this.state.lastTool.ok = false;
    return this.commit();
  }

  requireDraft(): Record<string, unknown> {
    if (!this.state.requirement) throw new Error("尚未建立 Requirement Draft。");
    return clone(this.state.requirement);
  }

  assertRequirementRetryAllowed(): void {
    if (!this.state.requirementRetry.blocked) return;
    const error = new Error(
      "本用户回合的 Requirement 自动修订已达到上限。请停止调用更新/校验工具，" +
      "向用户报告最后校验错误；收到用户新消息后可继续。",
    );
    Object.assign(error, { code: "REQUIREMENT_RETRY_LIMIT_EXCEEDED" });
    throw error;
  }

  requireValidatedDraft(expectedMode?: Exclude<VmTaskMode, "unknown">): Record<string, unknown> {
    if (!this.state.completion.requirementValid) {
      throw new Error("Requirement Draft 尚未通过确定性校验。");
    }
    if (expectedMode && this.state.intent !== expectedMode) {
      throw new Error(`当前 Requirement 是 ${this.state.intent}，不能执行 ${expectedMode}。`);
    }
    return this.requireDraft();
  }

  latestSolution(): string | undefined {
    return [...this.state.artifacts].reverse().find((artifact) => artifact.kind === "solution")?.path;
  }

  private addArtifact(kind: VmArtifact["kind"], path: string | undefined): void {
    if (!path) return;
    if (this.state.artifacts.some((artifact) => artifact.kind === kind && artifact.path === path)) return;
    this.state.artifacts.push({ kind, path, timestampUtc: now() });
  }

  private addEvidence(evidence: CapabilityEvidence): void {
    this.state.capabilityEvidence.push({
      ...evidence,
      data: compactEvidenceData(evidence.data),
    });
    if (this.state.capabilityEvidence.length > 50) {
      this.state.capabilityEvidence = this.state.capabilityEvidence.slice(-50);
    }
  }

  private commit(): VmTaskState {
    this.state.updatedUtc = now();
    if (!this.batching) {
      this.sessionManager.appendCustomEntry(SESSION_ENTRY_TYPE, clone(this.state));
    }
    return this.snapshot();
  }
}

function normalizeRestoredState(state: VmTaskState): VmTaskState {
  state.requirementRetry ??= {
    turnValidationAttempts: 0,
    totalValidationAttempts: 0,
    consecutiveSameFailures: 0,
    blocked: false,
  };
  return state;
}

function readValidationIssues(value: unknown): Array<{ code: string; path: string }> {
  if (!isRecord(value) || !Array.isArray(value.issues)) return [];
  return value.issues
    .filter(isRecord)
    .map((issue) => ({
      code: typeof issue.code === "string" ? issue.code : "UNKNOWN",
      path: typeof issue.path === "string" ? issue.path : "$",
    }));
}

function compactEvidenceData(value: unknown): unknown {
  if (value === undefined) return undefined;
  let json: string;
  try {
    json = JSON.stringify(value);
  } catch {
    return { compacted: true, summary: String(value) };
  }
  if (json.length <= 12_000) return value;
  if (isRecord(value)) {
    const matches = Array.isArray(value.matches)
      ? value.matches.slice(0, 10).map((match) => compactMatch(match))
      : undefined;
    return {
      compacted: true,
      originalCharacters: json.length,
      vmVersion: stringValue(value.vmVersion),
      query: stringValue(value.query),
      matchCount: Array.isArray(value.matches) ? value.matches.length : undefined,
      truncated: value.truncated === true,
      matches,
      keys: Object.keys(value),
    };
  }
  return { compacted: true, originalCharacters: json.length };
}

function compactMatch(value: unknown): unknown {
  const json = JSON.stringify(value);
  if (json.length <= 2_000) return value;
  return {
    compacted: true,
    originalCharacters: json.length,
    keys: isRecord(value) ? Object.keys(value) : [],
  };
}

function unique(values: string[]): string[] {
  return [...new Set(values)];
}

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === "object" && value !== null && !Array.isArray(value);
}

function stringValue(value: unknown): string | undefined {
  return typeof value === "string" && value ? value : undefined;
}

function readBoolean(value: unknown, property: string): boolean {
  return isRecord(value) && value[property] === true;
}

function recoveryFor(code: string): DomainError["recoverability"] {
  if ([
    "REQUIREMENT_SCHEMA_INVALID",
    "REQUIREMENT_DRAFT_INVALID",
    "SCRIPT_PRECOMPILE_FAILED",
    "SCRIPT_CONTRACT_INVALID",
    "GLOBAL_SCRIPT_CONTRACT_INVALID",
    "DEPENDENCY_VERSION_MISMATCH",
  ].includes(code)) return "automatic";
  if ([
    "PYTHON_COMPLEX_TYPE_UNCONFIRMED",
    "BOOL_SOURCE_COMPATIBILITY_REQUIRED",
    "PATCH_TARGET_MODULE_NOT_FOUND",
    "PATCH_TARGET_PARAMETER_NOT_FOUND",
  ].includes(code)) return "requires-user-choice";
  return "blocked";
}

export const VmStateEntryType = SESSION_ENTRY_TYPE;
