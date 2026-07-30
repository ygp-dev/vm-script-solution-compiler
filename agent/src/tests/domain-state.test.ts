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

  for (let attempt = 0; attempt < 3; attempt += 1) {
    state.recordRequirementValidation({ ok: true, issues: [] });
    state.recordError(
      "SCRIPT_CONTRACT_INVALID",
      "Generated source does not satisfy the csharp-module entry contract.",
    );
  }

  assert.equal(state.snapshot().requirementRetry.turnValidationAttempts, 3);
  assert.equal(state.snapshot().requirementRetry.consecutiveSameFailures, 3);
  assert.equal(state.snapshot().requirementRetry.blocked, true);
  assert.equal(state.snapshot().phase, "blocked");
  assert.throws(() => state.assertRequirementRetryAllowed(), /自动修订已达到上限/);
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
