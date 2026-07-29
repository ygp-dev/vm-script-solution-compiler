import { defineTool, type ToolDefinition } from "@earendil-works/pi-coding-agent";
import { Type } from "typebox";
import { VmDomainState } from "../domain/state.js";
import { DomainWorkerClient, DomainWorkerError } from "./domain-worker-client.js";
import { RequirementToolSchema } from "./requirement-tool-schema.js";

interface ToolDetails {
  ok: boolean;
  state: unknown;
  result?: unknown;
  error?: { code: string; message: string };
}

function output(details: ToolDetails) {
  return {
    content: [{ type: "text" as const, text: JSON.stringify(details) }],
    details,
  };
}

function compactState(state: ReturnType<VmDomainState["snapshot"]>) {
  return {
    intent: state.intent,
    baseSolution: state.baseSolution,
    outputDirectory: state.outputDirectory,
    targetVmVersion: state.targetVmVersion,
    phase: state.phase,
    requirementRevision: state.requirementRevision,
    unresolvedQuestions: state.unresolvedQuestions,
    artifacts: state.artifacts,
    lastCompilerError: state.lastCompilerError,
    lastTool: state.lastTool,
    completion: state.completion,
    requirementRetry: state.requirementRetry,
  };
}

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === "object" && value !== null && !Array.isArray(value);
}

function stringValue(value: unknown, name: string): string | undefined {
  return isRecord(value) && typeof value[name] === "string" ? value[name] : undefined;
}

function canonicalCapabilityQuery(query: string | undefined): string {
  const value = (query ?? "").trim().toLowerCase();
  if (!value) return "";
  if (/(global.?csharp|globalscript|userglobal)/i.test(value)) return "global-csharp";
  if (/(csharp|c#|shellmodule|scriptmethods|iprocessmethods)/i.test(value)) return "csharp-module";
  if (/(python|pyshell|iohelper)/i.test(value)) return "python-module";
  if (/netdxf/i.test(value)) return "netDxf";
  if (/(external.?dll|user.?assembly)/i.test(value)) return "user-assembly";
  return value;
}

function compactBuildResult(result: unknown) {
  return {
    ok: isRecord(result) && result.ok === true,
    taskDirectory: stringValue(result, "taskDirectory"),
    solutionFile: stringValue(result, "solutionFile"),
    reportFile: stringValue(result, "reportFile"),
    inputPreserved: isRecord(result) ? result.inputPreserved : undefined,
  };
}

function compactSolutionValidation(result: unknown) {
  return {
    ok: isRecord(result) && result.ok === true,
    parseOk: isRecord(result) && isRecord(result.parseResult),
    inspectOk: isRecord(result) && typeof result.inspectOutput === "string",
  };
}

function normalizeRequirement(requirement: Record<string, unknown>): Record<string, unknown> {
  const normalized = structuredClone(requirement);
  if (!Array.isArray(normalized.scripts)) return normalized;
  for (const value of normalized.scripts) {
    if (!isRecord(value) || value.carrier !== "csharp-module") continue;
    if (typeof value.source === "string" && value.source.trim()) {
      let source = value.source
        .replace(/using\s+(?:VM|Vm)\.Script\.Methods\s*;/g, "using Script.Methods;")
        .replace(/\b(?:VM|Vm)\.Script\.Methods\./g, "");
      if (!/using\s+Script\.Methods\s*;/.test(source)) {
        source = `using Script.Methods;\n${source}`;
      }
      value.source = source;
    }
    if (Array.isArray(value.dependencies)) {
      value.dependencies = value.dependencies.filter((dependency) => {
        const text = JSON.stringify(dependency).toLowerCase();
        return !text.includes("script.methods") && !text.includes("vm.script.methods");
      });
    }
  }
  return normalized;
}

async function executeDomainTool(
  name: string,
  state: VmDomainState,
  action: () => Promise<unknown>,
  apply: (result: unknown) => void,
): Promise<ReturnType<typeof output>> {
  try {
    const result = await action();
    apply(result);
    state.completeTool(name);
    return output({ ok: true, result, state: compactState(state.snapshot()) });
  } catch (error) {
    const code = error instanceof DomainWorkerError ? error.code : "DOMAIN_TOOL_FAILED";
    const message = error instanceof Error ? error.message : String(error);
    state.recordError(code, message);
    throw new DomainWorkerError(code, message);
  }
}

export function createVmTools(
  state: VmDomainState,
  worker: DomainWorkerClient,
): ToolDefinition[] {
  const capabilityCache = new Map<string, unknown>();
  return [
    defineTool({
      name: "vm_detect_environment",
      label: "检测 VM 环境",
      description: "检测本机 VisionMaster、SDK、GlobalScript 和编译资源。开始生成任务时应优先调用。",
      promptSnippet: "检测本机 VisionMaster 及编译环境。",
      parameters: Type.Object({}, { additionalProperties: false }),
      executionMode: "parallel",
      execute: async (_id, _params, signal) => {
        state.beginTool("vm_detect_environment");
        return executeDomainTool(
          "vm_detect_environment",
          state,
          () => worker.call("detect_environment", {}, signal),
          (result) => state.recordEnvironment(result),
        );
      },
    }),
    defineTool({
      name: "vm_inspect_solution",
      label: "检查 SOL",
      description: "检查已有 SOL 的流程、模块、脚本和结构。Patch 前必须调用，不得猜测模块或参数。",
      promptSnippet: "检查已有 SOL 的确定性结构。",
      parameters: Type.Object({
        file: Type.String({ description: "基底 SOL 的绝对路径" }),
      }, { additionalProperties: false }),
      executionMode: "parallel",
      execute: async (_id, params, signal) => {
        state.beginTool("vm_inspect_solution", "inspecting");
        return executeDomainTool(
          "vm_inspect_solution",
          state,
          () => worker.call("inspect_solution", { file: params.file }, signal),
          (result) => state.recordSolutionInspection(params.file, result),
        );
      },
    }),
    defineTool({
      name: "vm_query_capability",
      label: "查询 VM 能力",
      description: "查询项目内已验证的端口类型、载体、操作、DLL 或 VM API 证据。不能从常识猜测 VM 能力。",
      promptSnippet: "查询项目内已验证的 VM 4.4 能力证据。",
      parameters: Type.Object({
        query: Type.Optional(Type.String({ description: "例如 circle、python、netDxf、setModuleValue" })),
        vmVersion: Type.Optional(Type.String({ description: "默认 4.4.0" })),
      }, { additionalProperties: false }),
      executionMode: "parallel",
      execute: async (_id, params, signal) => {
        const query = canonicalCapabilityQuery(params.query);
        state.beginTool("vm_query_capability");
        if (capabilityCache.has(query)) {
          state.completeTool("vm_query_capability");
          return output({
            ok: true,
            result: {
              cached: true,
              query,
              message: "This VM capability evidence is already present. Continue the workflow without re-querying.",
            },
            state: compactState(state.snapshot()),
          });
        }
        return executeDomainTool(
          "vm_query_capability",
          state,
          () => worker.call("query_capability", {
            query,
            vmVersion: params.vmVersion ?? "4.4.0",
          }, signal),
          (result) => {
            capabilityCache.set(query, result);
            state.recordCapability(query, result);
          },
        );
      },
    }),
    defineTool({
      name: "vm_update_requirement",
      label: "更新 Requirement",
      description: "创建或修订完整 Requirement IR。普通计算优先使用 operations 并省略 source；csharp-module 的 Script.Methods 是 VM 内置引用，绝不能放入 dependencies。提交后直接调用 vm_compile_solution。",
      promptSnippet: "创建或修订 Requirement IR 草稿。",
      parameters: Type.Object({
        requirement: RequirementToolSchema,
      }, { additionalProperties: false }),
      executionMode: "sequential",
      execute: async (_id, params) => {
        state.assertRequirementRetryAllowed();
        try {
          state.beginTool("vm_update_requirement", "drafting");
          state.recordRequirement(normalizeRequirement(params.requirement as Record<string, unknown>));
          state.completeTool("vm_update_requirement");
          return output({
            ok: true,
            state: compactState(state.snapshot()),
            result: { revision: state.snapshot().requirementRevision },
          });
        } catch (error) {
          const message = error instanceof Error ? error.message : String(error);
          state.recordError("REQUIREMENT_DRAFT_INVALID", message);
          throw error;
        }
      },
    }),
    defineTool({
      name: "vm_compile_solution",
      label: "生成并验证 SOL",
      description: "首选的一步式确定性编译工具。使用当前 Requirement，自动完成校验、Create/Patch 构建和独立离线验证；不要再分别调用 plan/build/patch/validate 工具。",
      promptSnippet: "一次完成 Requirement 校验、SOL 构建和独立离线验证。",
      parameters: Type.Object({}, { additionalProperties: false }),
      executionMode: "sequential",
      execute: async (_id, _params, signal) => {
        state.assertRequirementRetryAllowed();
        const requirement = state.requireDraft();
        state.beginTool("vm_compile_solution", "requirement-validating");
        try {
          const validation = await worker.call<Record<string, unknown>>(
            "validate_requirement",
            { requirement },
            signal,
          );
          state.recordRequirementValidation(validation);
          if (validation.ok !== true) {
            state.completeTool("vm_compile_solution");
            return output({
              ok: false,
              result: {
                stage: "requirement-validation",
                issues: Array.isArray(validation.issues) ? validation.issues : [],
              },
              state: compactState(state.snapshot()),
            });
          }

          const snapshot = state.snapshot();
          const outputDirectory = snapshot.outputDirectory;
          if (!outputDirectory) throw new Error("生成任务缺少输出目录。");
          let build: unknown;
          if (snapshot.intent === "patch") {
            const baseSolution = snapshot.baseSolution;
            if (!baseSolution) throw new Error("Patch 模式缺少基底 SOL。");
            const inspected = snapshot.capabilityEvidence.some((evidence) =>
              evidence.kind === "solution" &&
              isRecord(evidence.data) &&
              stringValue(evidence.data, "file")?.toLowerCase() === baseSolution.toLowerCase()
            );
            if (!inspected) throw new Error("Patch 前必须先检查当前基底 SOL。");
            build = await worker.call(
              "patch_solution",
              { baseSolution, requirement, output: outputDirectory },
              signal,
            );
          } else if (snapshot.intent === "create") {
            build = await worker.call(
              "build_solution",
              { requirement, output: outputDirectory },
              signal,
            );
          } else {
            throw new Error("Requirement task.mode 必须是 create 或 patch。");
          }
          state.recordBuild(build);

          const solutionFile = stringValue(build, "solutionFile");
          if (!solutionFile) throw new Error("确定性编译器没有返回 SOL 路径。");
          const offline = await worker.call<Record<string, unknown>>(
            "validate_solution",
            { file: solutionFile },
            signal,
          );
          state.recordOfflineValidation(solutionFile, offline);
          state.completeTool("vm_compile_solution");
          return output({
            ok: true,
            result: {
              build: compactBuildResult(build),
              offlineValidation: compactSolutionValidation(offline),
            },
            state: compactState(state.snapshot()),
          });
        } catch (error) {
          const code = error instanceof DomainWorkerError ? error.code : "DOMAIN_TOOL_FAILED";
          const message = error instanceof Error ? error.message : String(error);
          state.recordError(code, message);
          throw new DomainWorkerError(code, message);
        }
      },
    }),
    defineTool({
      name: "vm_validate_requirement",
      label: "校验 Requirement",
      description: "使用 C# Core 对当前 Requirement Draft 执行 schema 和 VM 语义校验。",
      promptSnippet: "确定性校验当前 Requirement Draft。",
      parameters: Type.Object({}, { additionalProperties: false }),
      executionMode: "sequential",
      execute: async (_id, _params, signal) => {
        state.assertRequirementRetryAllowed();
        const requirement = state.requireDraft();
        state.beginTool("vm_validate_requirement", "requirement-validating");
        return executeDomainTool(
          "vm_validate_requirement",
          state,
          () => worker.call("validate_requirement", { requirement }, signal),
          (result) => state.recordRequirementValidation(result),
        );
      },
    }),
    defineTool({
      name: "vm_plan_solution",
      label: "规划 SOL",
      description: "为已校验 Requirement 输出确定性编译动作。写入前调用。",
      promptSnippet: "生成确定性 SOL 编译计划。",
      parameters: Type.Object({}, { additionalProperties: false }),
      executionMode: "sequential",
      execute: async (_id, _params, signal) => {
        const requirement = state.requireValidatedDraft();
        state.beginTool("vm_plan_solution");
        return executeDomainTool(
          "vm_plan_solution",
          state,
          () => worker.call("plan_solution", { requirement }, signal),
          (result) => state.recordPlan(result),
        );
      },
    }),
    defineTool({
      name: "vm_build_solution",
      label: "创建 SOL",
      description: "依据已校验的 Create Requirement 生成全新 SOL。模型不能使用其他方式写 SOL。",
      promptSnippet: "通过确定性 C# Core 创建 SOL。",
      parameters: Type.Object({
        output: Type.String({ description: "输出根目录的绝对路径" }),
      }, { additionalProperties: false }),
      executionMode: "sequential",
      execute: async (_id, params, signal) => {
        const requirement = state.requireValidatedDraft("create");
        state.beginTool("vm_build_solution", "building");
        return executeDomainTool(
          "vm_build_solution",
          state,
          () => worker.call("build_solution", { requirement, output: params.output }, signal),
          (result) => state.recordBuild(result),
        );
      },
    }),
    defineTool({
      name: "vm_patch_solution",
      label: "补丁 SOL",
      description: "依据已校验的 Patch Requirement 生成新的 SOL，绝不覆盖基底。Patch 前必须 inspect。",
      promptSnippet: "通过确定性 C# Core 补丁 SOL，不覆盖输入。",
      parameters: Type.Object({
        baseSolution: Type.String({ description: "基底 SOL 的绝对路径" }),
        output: Type.String({ description: "输出根目录的绝对路径" }),
      }, { additionalProperties: false }),
      executionMode: "sequential",
      execute: async (_id, params, signal) => {
        const snapshot = state.snapshot();
        const requirement = state.requireValidatedDraft("patch");
        if (!snapshot.capabilityEvidence.some((evidence) => evidence.kind === "solution")) {
          throw new Error("Patch 前必须先调用 vm_inspect_solution。");
        }
        if (snapshot.baseSolution && snapshot.baseSolution.toLowerCase() !== params.baseSolution.toLowerCase()) {
          throw new Error("Patch 工具的 baseSolution 与已检查的基底 SOL 不一致。");
        }
        state.beginTool("vm_patch_solution", "building");
        return executeDomainTool(
          "vm_patch_solution",
          state,
          () => worker.call("patch_solution", {
            baseSolution: params.baseSolution,
            requirement,
            output: params.output,
          }, signal),
          (result) => state.recordBuild(result),
        );
      },
    }),
    defineTool({
      name: "vm_validate_solution",
      label: "验证 SOL",
      description: "对生成结果执行独立 parse 和 inspect。只有该工具通过，任务才达到 offline-validated。",
      promptSnippet: "独立验证生成的 SOL。",
      parameters: Type.Object({
        file: Type.Optional(Type.String({ description: "默认使用当前会话最新生成的 SOL" })),
      }, { additionalProperties: false }),
      executionMode: "sequential",
      execute: async (_id, params, signal) => {
        const file = params.file ?? state.latestSolution();
        if (!file) throw new Error("没有可验证的 SOL。");
        state.beginTool("vm_validate_solution", "offline-validating");
        return executeDomainTool(
          "vm_validate_solution",
          state,
          () => worker.call("validate_solution", { file }, signal),
          (result) => state.recordOfflineValidation(file, result),
        );
      },
    }),
    defineTool({
      name: "vm_read_build_report",
      label: "读取构建报告",
      description: "读取构建报告及产物列表，用于诊断失败或交付证据。",
      promptSnippet: "读取 VM 构建报告和产物列表。",
      parameters: Type.Object({
        file: Type.String({ description: "build-report.md 的绝对路径" }),
      }, { additionalProperties: false }),
      executionMode: "parallel",
      execute: async (_id, params, signal) => {
        state.beginTool("vm_read_build_report");
        return executeDomainTool(
          "vm_read_build_report",
          state,
          () => worker.call("read_build_report", { file: params.file }, signal),
          (result) => state.recordReport(params.file, result),
        );
      },
    }),
    defineTool({
      name: "vm_record_user_validation",
      label: "记录 VM 验收",
      description: "仅在用户明确报告已在 VisionMaster 中验证后调用。不能由模型自行推断。",
      promptSnippet: "记录用户明确提供的 VM 实机验收结果。",
      parameters: Type.Object({
        note: Type.String({ description: "用户明确报告的验证内容" }),
      }, { additionalProperties: false }),
      executionMode: "sequential",
      execute: async (_id, params) => {
        try {
          state.beginTool("vm_record_user_validation");
          state.recordUserValidation(params.note);
          state.completeTool("vm_record_user_validation");
          return output({ ok: true, state: compactState(state.snapshot()), result: { recorded: true } });
        } catch (error) {
          const message = error instanceof Error ? error.message : String(error);
          state.recordError("USER_VALIDATION_INVALID", message, "requires-user-choice");
          throw error;
        }
      },
    }),
  ];
}
