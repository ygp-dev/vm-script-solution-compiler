import test from "node:test";
import assert from "node:assert/strict";
import { configuredReasoning } from "../model.js";

test("DeepSeek compatible endpoint enables its native thinking controls", () => {
  const result = configuredReasoning({
    provider: "openai-compatible",
    endpoint: "https://api.deepseek.com",
  });
  assert.equal(result.reasoning, true);
  assert.equal(result.thinkingLevelMap?.low, "high");
  assert.equal(result.thinkingLevelMap?.high, "high");
  assert.equal(result.thinkingLevelMap?.xhigh, "max");
});

test("generic compatible endpoint does not assume reasoning parameter support", () => {
  assert.deepEqual(configuredReasoning({
    provider: "openai-compatible",
    endpoint: "https://example.test/v1",
  }), { reasoning: false });
});
