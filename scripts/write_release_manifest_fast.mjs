import crypto from "node:crypto";
import fs from "node:fs";
import fsp from "node:fs/promises";
import os from "node:os";
import path from "node:path";
import { spawnSync } from "node:child_process";
import { fileURLToPath } from "node:url";

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), "..");
const dist = path.resolve(root, process.argv[2] ?? "dist");
const props = await fsp.readFile(path.join(root, "Directory.Build.props"), "utf8");
const version = props.match(/<Version>([^<]+)<\/Version>/)?.[1];
if (!version) throw new Error("Directory.Build.props does not contain Version.");

async function filesUnder(directory) {
  const result = [];
  async function visit(current) {
    for (const entry of await fsp.readdir(current, { withFileTypes: true })) {
      const full = path.join(current, entry.name);
      if (entry.isDirectory()) await visit(full);
      else if (entry.isFile()) result.push(path.resolve(full));
    }
  }
  await visit(directory);
  return result;
}

async function hashFile(file) {
  const hash = crypto.createHash("sha256");
  for await (const chunk of fs.createReadStream(file)) hash.update(chunk);
  return hash.digest("hex").toUpperCase();
}

async function hashFiles(files) {
  const unique = [...new Set(files.map((file) => path.resolve(file)))];
  const values = new Map();
  let index = 0;
  const concurrency = Math.min(48, Math.max(8, (os.cpus().length || 4) * 2));
  await Promise.all(Array.from({ length: concurrency }, async () => {
    while (true) {
      const current = index++;
      if (current >= unique.length) return;
      values.set(unique[current], await hashFile(unique[current]));
    }
  }));
  return values;
}

function required(output, relative) {
  const file = path.resolve(output, relative);
  if (!fs.existsSync(file)) throw new Error(`Published file is missing: ${file}`);
  return file;
}

function relative(output, file) {
  return path.relative(output, file).split(path.sep).join("/");
}

function payloadHash(output, files, hashes) {
  const unique = [...new Set(files.map((file) => path.resolve(file)))].sort((a, b) =>
    relative(output, a).localeCompare(relative(output, b)));
  const lines = unique.map((file) => `${relative(output, file)}\t${hashes.get(file)}`);
  return { count: lines.length, sha256: crypto.createHash("sha256").update(lines.join("\n"), "utf8").digest("hex").toUpperCase() };
}

async function productFiles(name, exe, assembly) {
  const output = path.join(dist, name);
  const files = [
    required(output, exe),
    required(output, assembly),
    required(output, "VmScriptCompiler.Core.dll"),
    required(output, "schemas/requirement.schema.json"),
    required(output, "resources/vm/4.4.0/manifest.json"),
    required(output, "tools/vm-solution-parser/VMSolutionParser.Cli.exe"),
    required(output, "USAGE.md"),
  ];
  if (name === "Desktop") {
    files.push(
      required(output, "runtime/node.exe"),
      required(output, "agent/package.json"),
      required(output, "agent/package-lock.json"),
      required(output, "agent/resources/SYSTEM.md"),
      required(output, "worker/vm-script-domain-worker.exe"),
      required(output, "worker/VmScriptCompiler.Core.dll"),
      ...(await filesUnder(path.join(output, "agent/dist"))),
      ...(await filesUnder(path.join(output, "agent/resources"))),
      ...(await filesUnder(path.join(output, "agent/node_modules"))),
    );
  }
  return { name, exe, assembly, output, files };
}

const products = [
  ["Cli", "VmScriptCompiler.Cli.exe", "VmScriptCompiler.Cli.dll"],
  ["Desktop", "vm-script-compiler-desktop.exe", "vm-script-compiler-desktop.dll"],
  ["Mcp", "vm-script-compiler-mcp.exe", "vm-script-compiler-mcp.dll"],
];
const productData = await Promise.all(products.map(([name, exe, assembly]) => productFiles(name, exe, assembly)));
const allFiles = productData.flatMap((product) => product.files);
const hashes = await hashFiles(allFiles);
const entries = productData.map((product) => {
  const integrity = payloadHash(product.output, product.files, hashes);
  return {
    name: product.name,
    entryPoint: product.exe,
    entryPointSha256: hashes.get(path.resolve(product.output, product.exe)),
    applicationAssembly: product.assembly,
    applicationAssemblySha256: hashes.get(path.resolve(product.output, product.assembly)),
    integrityFiles: integrity.count,
    integritySha256: integrity.sha256,
  };
});

const desktop = path.join(dist, "Desktop");
const coreSha = hashes.get(required(desktop, "VmScriptCompiler.Core.dll"));
const workerCoreSha = hashes.get(required(desktop, "worker/VmScriptCompiler.Core.dll"));
const mcpCoreSha = hashes.get(required(path.join(dist, "Mcp"), "VmScriptCompiler.Core.dll"));
if (coreSha !== workerCoreSha || coreSha !== mcpCoreSha) throw new Error("Core payloads are not synchronized.");

const agentFiles = [
  required(desktop, "agent/package.json"),
  required(desktop, "agent/package-lock.json"),
  ...(await filesUnder(path.join(desktop, "agent/dist"))),
  ...(await filesUnder(path.join(desktop, "agent/resources"))),
  ...(await filesUnder(path.join(desktop, "agent/node_modules"))),
];
const agentLines = [...new Set(agentFiles.map((file) => path.resolve(file)))].sort().map((file) =>
  `${relative(path.join(desktop, "agent"), file)}\t${hashes.get(path.resolve(file))}`);
const agentPayloadSha = crypto.createHash("sha256").update(agentLines.join("\n"), "utf8").digest("hex").toUpperCase();
const node = required(desktop, "runtime/node.exe");
const nodeVersion = spawnSync(node, ["--version"], { encoding: "utf8" }).stdout.trim();

const manifest = {
  version,
  runtime: "win-x64 self-contained desktop + bundled Node",
  generatedUtc: new Date().toISOString(),
  products: entries,
  architecture: {
    primaryProduct: "Desktop",
    domainAgent: "Pi 0.82.1",
    nodeVersion,
    deterministicWorker: "vm-script-domain-worker.exe",
    mcpRole: "external deterministic adapter",
    legacyAgentPublished: false,
  },
  componentSynchronization: {
    desktopWorkerAndMcp: true,
    coreSha256: coreSha,
    agentMainSha256: hashes.get(required(desktop, "agent/dist/main.js")),
    agentPayloadFiles: agentLines.length,
    agentPayloadSha256: agentPayloadSha,
    workerSha256: hashes.get(required(desktop, "worker/vm-script-domain-worker.exe")),
    nodeSha256: hashes.get(node),
  },
  containsSolFiles: false,
};
await fsp.writeFile(path.join(dist, "release-manifest.json"), `${JSON.stringify(manifest, null, 2)}\n`, "utf8");
console.log(JSON.stringify(manifest, null, 2));
