import http from "node:http";
import fs from "node:fs";
import path from "node:path";

const [, , portValue, fixtureFile, outputValue, readyFile] = process.argv;
const port = Number.parseInt(portValue, 10);
const fixture = JSON.parse(fs.readFileSync(fixtureFile, "utf8"));
const outputDirectory = path.resolve(outputValue);
let requestCount = 0;

function functionCall(index, name, args) {
  return {
    type: "function_call",
    id: `fc_${index}_${name}`,
    call_id: `call_${index}_${name}`,
    name,
    arguments: JSON.stringify(args),
    status: "completed",
  };
}

function message(index, text) {
  return {
    type: "message",
    id: `msg_${index}`,
    role: "assistant",
    status: "completed",
    content: [{ type: "output_text", text, annotations: [] }],
  };
}

function scriptedOutput(index) {
  if (index === 1) return [functionCall(index, "vm_update_requirement", { requirement: fixture })];
  if (index === 2) return [functionCall(index, "vm_compile_solution", {})];
  return [message(index, "SOL passed deterministic offline validation. VisionMaster runtime behavior still requires user confirmation.")];
}

const server = http.createServer((request, response) => {
  const chunks = [];
  request.on("data", (chunk) => chunks.push(chunk));
  request.on("end", () => {
    requestCount += 1;
    let body;
    try {
      body = JSON.parse(Buffer.concat(chunks).toString("utf8"));
    } catch {
      response.writeHead(400).end("invalid json");
      return;
    }
    const toolNames = new Set(
      (body.tools ?? [])
        .filter((tool) => tool.type === "function")
        .map((tool) => tool.name),
    );
    const requiredTools = [
      "vm_inspect_solution",
      "vm_update_requirement",
      "vm_compile_solution",
      "vm_read_build_report",
      "vm_record_user_validation",
    ];
    const updateTool = (body.tools ?? []).find(
      (tool) => tool.type === "function" && tool.name === "vm_update_requirement",
    );
    const updateSchema = JSON.stringify(updateTool?.parameters ?? {});
    const requestText = JSON.stringify(body);
    const valid =
      request.url?.endsWith("/v1/responses") &&
      body.stream === true &&
      Array.isArray(body.input) &&
      request.headers.authorization === "Bearer offline-agent-key" &&
      requiredTools.every((name) => toolNames.has(name)) &&
      updateSchema.includes('"source"') &&
      updateSchema.includes('"operations"') &&
      updateSchema.includes('"once"') &&
      updateSchema.includes('"python-module"') &&
      requestText.includes("VM Script Compile Requirement") &&
      requestText.includes("C# Create") &&
      requestText.includes("Python Create");
    if (!valid) {
      response.writeHead(400).end("invalid agent Responses request");
      return;
    }

    const items = scriptedOutput(requestCount);
    const responseId = `resp_agent_${requestCount}`;
    const events = [{
      type: "response.created",
      response: { id: responseId, status: "in_progress", output: [] },
    }];
    items.forEach((item, outputIndex) => {
      events.push({ type: "response.output_item.added", output_index: outputIndex, item });
      events.push({ type: "response.output_item.done", output_index: outputIndex, item });
    });
    events.push({
      type: "response.completed",
      response: {
        id: responseId,
        status: "completed",
        output: items,
        usage: {
          input_tokens: 100,
          output_tokens: 20,
          total_tokens: 120,
          input_tokens_details: { cached_tokens: 0 },
          output_tokens_details: { reasoning_tokens: 0 },
        },
      },
    });
    const payload = `${events.map((event) => `data: ${JSON.stringify(event)}\n\n`).join("")}data: [DONE]\n\n`;
    response.writeHead(200, {
      "Content-Type": "text/event-stream; charset=utf-8",
      "Cache-Control": "no-cache",
      "Content-Length": Buffer.byteLength(payload),
      Connection: "close",
    });
    response.end(payload);
    if (requestCount >= 3) server.close();
  });
});

server.listen(port, "127.0.0.1", () => {
  fs.writeFileSync(readyFile, "ready", "ascii");
});
