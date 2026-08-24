import test from "node:test";
import assert from "node:assert/strict";
import path from "node:path";
import { fileURLToPath } from "node:url";
import { searchProjectKnowledge } from "../knowledge.js";

const repositoryRoot = path.resolve(path.dirname(fileURLToPath(import.meta.url)), "../../..");

test("project knowledge search returns FAQ evidence without loading the raw skill", () => {
  const result = searchProjectKnowledge(repositoryRoot, "保存图像 ImageData", "faq", 3);
  assert.ok(result.totalMatches > 0);
  assert.ok(result.matches.some((match) => match.title.includes("保存图像")));
  assert.ok(result.matches[0]?.excerpt.includes("ImageData"));
  assert.match(result.notice, /advisory reference data/);
});

test("project knowledge search returns reference SOL source and metadata", () => {
  const result = searchProjectKnowledge(repositoryRoot, "数组逆序", "reference-solution", 3);
  assert.ok(result.totalMatches > 0);
  const match = result.matches.find((item) => item.title.includes("数组逆序"));
  assert.ok(match);
  assert.equal(match.kind, "reference-solution");
  assert.ok(match.metadata);
  assert.match(match.excerpt, /UserScript|Process/);
});

test("project knowledge search can retrieve selected tutor references", () => {
  const result = searchProjectKnowledge(repositoryRoot, "AlgorithmTab.xml", "tutor-reference", 5);
  assert.ok(result.totalMatches > 0);
  assert.ok(result.matches.some((match) => match.excerpt.includes("AlgorithmTab.xml")));
});
