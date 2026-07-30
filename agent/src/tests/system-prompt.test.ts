import test from "node:test";
import assert from "node:assert/strict";
import path from "node:path";
import { fileURLToPath } from "node:url";
import type { AgentConfiguration } from "../config.js";
import { buildVmSystemPrompt } from "../system-prompt.js";

const repositoryRoot = path.resolve(path.dirname(fileURLToPath(import.meta.url)), "../../..");

test("VM system prompt contains authoritative schema and C#/Python Create examples", () => {
  const config = {
    repositoryRoot,
    agentRoot: path.join(repositoryRoot, "agent"),
  } as AgentConfiguration;
  const prompt = buildVmSystemPrompt(config);
  assert.match(prompt, /VM Script Compile Requirement/);
  assert.match(prompt, /"mode": "once"/);
  assert.match(prompt, /"source": "# coding: utf-8/);
  assert.match(prompt, /"kind": "binary"/);
  assert.match(prompt, /operations 表达的逻辑必须优先使用 operations/);
  assert.match(prompt, /source.*script 顶层/s);
  assert.match(prompt, /execution.*只能包含.*mode.*order/s);
  assert.match(prompt, /public partial class UserScript : ScriptMethods, IProcessMethods/);
  assert.match(prompt, /void Init\(\)/);
  assert.match(prompt, /bool Process\(\)/);
});
