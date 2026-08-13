import test from "node:test";
import assert from "node:assert/strict";
import path from "node:path";
import { fileURLToPath } from "node:url";
import type { AgentConfiguration } from "../config.js";
import { buildVmSystemPrompt } from "../system-prompt.js";

const repositoryRoot = path.resolve(path.dirname(fileURLToPath(import.meta.url)), "../../..");

test("VM system prompt contains authoritative schema and all carrier Create examples", () => {
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
  assert.match(prompt, /public class UserGlobalScript : UserGlobalMethods, IScriptMethods/);
  assert.match(prompt, /public override int InitAfterLoadSol\(\)/);
  assert.match(prompt, /VmSolution\.Instance\.SolutionPath/);
  assert.match(prompt, /dictProcessID\.Keys/);
  assert.match(prompt, /"lifecycle": \["InitSDK", "InitAfterLoadSol"\]/);
  assert.match(prompt, /不得使用 Script\.Methods、ScriptMethods、IProcessMethods/);
  assert.match(prompt, /同类编译错误最多连续修订 2 次/);
  assert.match(prompt, /doc\.Entities\.All/);
  assert.match(prompt, /Polyline2D/);
  assert.match(prompt, /new ImageData\(bitmapVariable\)/);
  assert.match(prompt, /确定性操作 `dxfRender`/);
  assert.match(prompt, /累计最多提交 6 个 Requirement 版本/);
  assert.match(prompt, /error\.details\.diagnostics/);
  assert.match(prompt, /算子二次开发与运行界面清图/);
  assert.match(prompt, /secondary-development-knowledge/);
  assert.match(prompt, /多图像控件没有直接清空接口/);
  assert.match(prompt, /不得从文章截图猜方法名/);
  assert.match(prompt, /InputImageData/);
  assert.match(prompt, /TMVSAffineTransformModuTool/);
  assert.match(prompt, /OpenCvSharp/);
  assert.match(prompt, /背景图写入只表示执行了一个状态修改工作流/);
  assert.match(prompt, /community-articles-knowledge/);
  assert.match(prompt, /vm-script-tutor C# 脚本开发知识/);
  assert.match(prompt, /"skillVersion": "1\.2"/);
  assert.match(prompt, /UserProperty\.cs.*只读/s);
  assert.match(prompt, /C# 5\.0/);
  assert.match(prompt, /AlgorithmTab\.xml/);
  assert.match(prompt, /errorStatus/);
  assert.match(prompt, /UI 手动配置清单/);
  assert.match(prompt, /状态机需求优先拆成显式状态/);
  assert.match(prompt, /原生 C\+\+ 类需要托管包装或 C ABI/);
  assert.match(prompt, /Python 模型和图像库不得在脚本中自动安装或下载/);
});
