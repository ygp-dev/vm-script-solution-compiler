import test from "node:test";
import assert from "node:assert/strict";
import { SessionManager } from "@earendil-works/pi-coding-agent";
import { VmDomainState, VmStateEntryType } from "../domain/state.js";

test("VM domain state persists revisions and separates offline/user validation", () => {
  const manager = SessionManager.inMemory("D:\\vm-test");
  const state = new VmDomainState(manager);
  state.setTaskContext({ mode: "create", outputDirectory: "D:\\out" });
  state.recordRequirement({
    schemaVersion: "1.0",
    task: { name: "sum", mode: "create", vmVersion: "4.4.0" },
    scripts: [],
    connections: [],
  });
  state.recordRequirementValidation({ ok: true, issues: [] });
  state.recordBuild({
    ok: true,
    taskDirectory: "D:\\out\\task",
    solutionFile: "D:\\out\\task\\result.sol",
    reportFile: "D:\\out\\task\\build-report.md",
  });

  assert.equal(state.snapshot().phase, "built");
  assert.equal(state.snapshot().completion.solutionBuilt, true);
  assert.equal(state.snapshot().completion.userValidated, false);

  state.recordOfflineValidation("D:\\out\\task\\result.sol", { ok: true });
  assert.equal(state.snapshot().phase, "offline-validated");
  assert.equal(state.snapshot().completion.userValidated, false);

  state.recordUserValidation("用户确认 VM 打开、编译和运行正常");
  assert.equal(state.snapshot().phase, "user-validated");
  assert.equal(state.snapshot().completion.userValidated, true);
  assert.ok(manager.getEntries().some(
    (entry) => entry.type === "custom" && entry.customType === VmStateEntryType,
  ));

  const restored = new VmDomainState(manager);
  assert.deepEqual(restored.snapshot(), state.snapshot());
});

test("VM domain state blocks build before deterministic requirement validation", () => {
  const state = new VmDomainState(SessionManager.inMemory());
  state.recordRequirement({
    schemaVersion: "1.0",
    task: { name: "sum", mode: "create", vmVersion: "4.4.0" },
    scripts: [],
    connections: [],
  });
  assert.throws(() => state.requireValidatedDraft("create"), /尚未通过确定性校验/);
});

test("VM domain state stops repeated Requirement validation loops", () => {
  const state = new VmDomainState(SessionManager.inMemory());
  state.setTaskContext({ mode: "create" });
  state.recordRequirement({
    schemaVersion: "1.0",
    task: { name: "bad", mode: "create", vmVersion: "4.4.0" },
    scripts: [],
  });
  const failure = {
    ok: false,
    issues: [{
      code: "REQUIREMENT_SCHEMA_INVALID",
      path: "$.scripts[0].execution.mode",
      message: "execution.mode is invalid.",
    }],
  };
  state.recordRequirementValidation(failure);
  state.recordRequirementValidation(failure);
  state.recordRequirementValidation(failure);

  assert.equal(state.snapshot().phase, "blocked");
  assert.equal(state.snapshot().requirementRetry.consecutiveSameFailures, 3);
  assert.equal(state.snapshot().requirementRetry.blocked, true);
  assert.throws(
    () => state.assertRequirementRetryAllowed(),
    /自动修订已达到上限/,
  );

  state.setTaskContext({ mode: "create" });
  assert.equal(state.snapshot().requirementRetry.blocked, false);
  assert.doesNotThrow(() => state.assertRequirementRetryAllowed());
});

test("VM domain state stops repeated compiler-error loops after valid schema checks", () => {
  const state = new VmDomainState(SessionManager.inMemory());
  state.setTaskContext({ mode: "create" });
  state.recordRequirement({
    schemaVersion: "1.0",
    task: { name: "bad-source", mode: "create", vmVersion: "4.4.0" },
    scripts: [],
  });

  for (let attempt = 0; attempt < 2; attempt += 1) {
    state.recordRequirementValidation({ ok: true, issues: [] });
    state.recordError(
      "SCRIPT_CONTRACT_INVALID",
      `Generated source attempt ${attempt} does not satisfy the csharp-module entry contract.`,
    );
  }

  assert.equal(state.snapshot().requirementRetry.turnValidationAttempts, 2);
  assert.equal(state.snapshot().requirementRetry.consecutiveSameFailures, 2);
  assert.equal(state.snapshot().requirementRetry.blocked, true);
  assert.equal(state.snapshot().phase, "blocked");
  assert.throws(() => state.assertRequirementRetryAllowed(), /自动修订已达到上限/);
});

test("VM domain state limits repeated global-script contract failures", () => {
  const state = new VmDomainState(SessionManager.inMemory());
  state.setTaskContext({ mode: "create" });
  state.recordRequirement({
    schemaVersion: "1.0",
    task: { name: "global-init", mode: "create", vmVersion: "4.4.0" },
    scripts: [],
  });

  for (let attempt = 0; attempt < 2; attempt += 1) {
    state.recordRequirementValidation({ ok: true, issues: [] });
    state.recordError(
      "GLOBAL_SCRIPT_CONTRACT_INVALID",
      "全局脚本未满足 VM 4.4 入口契约。",
    );
  }

  assert.equal(state.snapshot().requirementRetry.blocked, true);
  assert.equal(state.snapshot().requirementRetry.consecutiveSameFailures, 2);
  assert.throws(() => state.assertRequirementRetryAllowed(), /自动修订已达到上限/);
});

test("VM domain state rejects unrelated task-name experiments in one user turn", () => {
  const state = new VmDomainState(SessionManager.inMemory());
  state.setTaskContext({ mode: "create" });
  state.recordRequirement({
    schemaVersion: "1.0",
    task: { name: "global-init", mode: "create", vmVersion: "4.4.0" },
    scripts: [],
  });

  assert.throws(() => state.recordRequirement({
    schemaVersion: "1.0",
    task: { name: "test-basic", mode: "create", vmVersion: "4.4.0" },
    scripts: [],
  }), /不能把任务从 global-init 改为 test-basic/);

  state.setTaskContext({ mode: "create" });
  assert.doesNotThrow(() => state.recordRequirement({
    schemaVersion: "1.0",
    task: { name: "next-user-task", mode: "create", vmVersion: "4.4.0" },
    scripts: [],
  }));
});

test("VM domain state clears superseded compiler diagnostics on Requirement revision", () => {
  const state = new VmDomainState(SessionManager.inMemory());
  state.setTaskContext({ mode: "create" });
  const requirement = {
    schemaVersion: "1.0",
    task: { name: "dependency-fix", mode: "create", vmVersion: "4.4.0" },
    scripts: [],
  };
  state.recordRequirement(requirement);
  state.recordError(
    "DEPENDENCY_TARGET_FRAMEWORK_INCOMPATIBLE",
    "External DLL targets .NET 6.",
  );
  assert.equal(state.snapshot().unresolvedQuestions.length, 1);

  state.setTaskContext({ mode: "create" });
  state.recordRequirement(requirement);
  assert.deepEqual(state.snapshot().unresolvedQuestions, []);
});

test("VM domain state caps cumulative revisions for the same task across user turns", () => {
  const state = new VmDomainState(SessionManager.inMemory());
  const requirement = {
    schemaVersion: "1.0",
    task: { name: "same-dxf-task", mode: "create", vmVersion: "4.4.0" },
    scripts: [],
  };
  for (let revision = 0; revision < 6; revision += 1) {
    state.setTaskContext({ mode: "create" });
    state.recordRequirement(requirement);
  }
  assert.equal(state.snapshot().requirementRetry.taskRevisions, 6);
  state.setTaskContext({ mode: "create" });
  assert.throws(() => state.recordRequirement(requirement), /累计最多 6 个版本/);

  assert.doesNotThrow(() => state.recordRequirement({
    ...requirement,
    task: { ...requirement.task, name: "genuinely-new-task" },
  }));
  assert.equal(state.snapshot().requirementRetry.taskRevisions, 1);
});

test("VM domain state persists one snapshot for a successful batched tool", () => {
  const manager = SessionManager.inMemory();
  const state = new VmDomainState(manager);
  const before = manager.getEntries().length;
  state.beginTool("vm_detect_environment");
  state.recordEnvironment({ found: true, version: "4.4.0" });
  state.completeTool("vm_detect_environment");
  const custom = manager.getEntries().slice(before).filter(
    (entry) => entry.type === "custom" && entry.customType === VmStateEntryType,
  );
  assert.equal(custom.length, 1);
});
