import { Type } from "typebox";

function literalUnion<const T extends readonly string[]>(values: T) {
  return Type.Union(values.map((value) => Type.Literal(value)));
}

const PortType = literalUnion([
  "bool", "int", "int[]", "float", "float[]", "string", "string[]", "byte",
  "image", "roibox", "roibox[]", "roiannulus", "roipolygon", "point", "line",
  "fixture", "rect", "ellipse", "pointset",
] as const);

const Io = Type.Object({
  name: Type.String({ minLength: 1 }),
  type: PortType,
  default: Type.Optional(Type.Any()),
  required: Type.Optional(Type.Boolean()),
  description: Type.Optional(Type.String()),
}, { additionalProperties: false });

const Operation = Type.Object({
  kind: literalUnion([
    "getModule", "getModuleArray", "getModuleParam", "setOutput", "setModuleValue",
    "getModuleValue", "runProcedure", "continuousProcedure", "stopProcedure",
    "setContinuousInterval", "startGlobalCommunication", "sendCommunication",
    "log", "sleep", "bytesToPointset", "setProcedureInput", "getGlobalVariable",
    "setGlobalVariable", "getLocalVariable", "setLocalVariable", "saveSolution",
    "loadSolution",
  ] as const),
  procedure: Type.Optional(Type.String()),
  module: Type.Optional(Type.String()),
  parameter: Type.Optional(Type.String()),
  result: Type.Optional(Type.String({ pattern: "^[A-Za-z_][A-Za-z0-9_]*$" })),
  valueType: Type.Optional(Type.String()),
  value: Type.Optional(Type.Any()),
  condition: Type.Optional(Type.Any()),
  deviceId: Type.Optional(Type.Integer({ minimum: 0 })),
  addressId: Type.Optional(Type.Integer({ minimum: -1 })),
  milliseconds: Type.Optional(Type.Integer({ minimum: 0 })),
  dataType: Type.Optional(literalUnion(["string", "int", "float", "byte"] as const)),
  when: Type.Optional(Type.String()),
  onError: Type.Optional(literalUnion(["fail", "continue", "set-output-failed"] as const)),
}, { additionalProperties: false });

const Dependency = Type.Object({
  kind: literalUnion(["dotnet-assembly", "python-package"] as const),
  name: Type.String({ minLength: 1 }),
  role: Type.Optional(literalUnion(["system", "vm-sdk", "operator-sdk", "third-party"] as const)),
  version: Type.Optional(Type.String()),
  path: Type.Optional(Type.String()),
  architecture: Type.Optional(literalUnion(["x64", "anycpu", "pure-python"] as const)),
  referenceType: Type.Optional(Type.Union([
    Type.Literal(0), Type.Literal(3), Type.Literal(4), Type.Literal(6),
  ])),
}, { additionalProperties: false });

const Script = Type.Object({
  id: Type.String({
    pattern: "^[a-z0-9][a-z0-9-]*$",
    description: "稳定的小写 ASCII id，例如 point-sort-python；不要使用中文、下划线或大写字母。",
  }),
  carrier: literalUnion(["global-csharp", "csharp-module", "python-module"] as const),
  name: Type.String({ minLength: 1 }),
  source: Type.Optional(Type.String({
    minLength: 1,
    description: "完整 C# 或 Python 源码。source 是 script 顶层属性，绝不能放入 execution。",
  })),
  procedure: Type.Optional(Type.String({
    description: "csharp-module/python-module 必填，例如 流程1。",
  })),
  execution: Type.Object({
    mode: literalUnion(["init", "once", "continuous", "callback"] as const),
    order: Type.Optional(Type.Integer({ minimum: 0 })),
  }, {
    additionalProperties: false,
    description: "execution 只能包含 mode/order；普通脚本使用 mode=once。",
  }),
  inputs: Type.Array(Io),
  outputs: Type.Array(Io),
  operations: Type.Optional(Type.Array(Operation, {
    description: "确定性操作列表。operations 是 script 顶层属性。",
  })),
  dependencies: Type.Optional(Type.Array(Dependency)),
}, { additionalProperties: false });

export const RequirementToolSchema = Type.Object({
  schemaVersion: Type.Literal("1.0"),
  task: Type.Object({
    name: Type.String({ minLength: 1 }),
    mode: literalUnion(["create", "patch"] as const),
    vmVersion: Type.String({ pattern: "^4\\.4(?:\\.\\d+)?$" }),
    baseSolution: Type.Optional(Type.String({ minLength: 1 })),
  }, { additionalProperties: false }),
  scripts: Type.Array(Script, { minItems: 1 }),
  connections: Type.Optional(Type.Array(Type.Object({
    from: Type.String({ pattern: "^[a-z0-9][a-z0-9-]*$" }),
    to: Type.String({ pattern: "^[a-z0-9][a-z0-9-]*$" }),
  }, { additionalProperties: false }))),
}, {
  additionalProperties: false,
  description: "完整 VM Script Requirement IR。字段结构与 schemas/requirement.schema.json 一致。",
});
