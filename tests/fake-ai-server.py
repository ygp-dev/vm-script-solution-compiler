import http.server
import gzip
import json
import pathlib
import sys

port = int(sys.argv[1])
fixture = pathlib.Path(sys.argv[2]).read_text(encoding="utf-8")
ready = pathlib.Path(sys.argv[3])

class Handler(http.server.BaseHTTPRequestHandler):
    def do_POST(self):
        body = self.rfile.read(int(self.headers.get("Content-Length", "0"))).decode("utf-8")
        request = json.loads(body)
        responses_api = "input" in request
        system = request["instructions"] if responses_api else request["messages"][0]["content"]
        user_text = json.dumps(request.get("input", request.get("messages", [])), ensure_ascii=False)
        if (
            "VM type system:" not in system
            or "compatibilityAliases" not in system
            or "API catalog:" not in system
            or "never declare a bool input or output" not in system
            or "PortName != 0" not in system
            or "condition ? 1 : 0" not in system
            or "public partial class UserScript : ScriptMethods, IProcessMethods" not in system
        ):
            self.send_response(400)
            self.end_headers()
            self.wfile.write(b'missing compiler evidence')
            return
        response_fixture = fixture
        if "normalize external DLL" in user_text:
            normalized_case = json.loads(fixture)
            normalized_case["scripts"][0]["dependencies"] = [{
                "kind": "dotnet-assembly",
                "name": "External.Sample.dll",
                "path": "C:\\missing\\External.Sample.dll",
                "architecture": "x64",
                "role": "third-party",
                "referenceType": 6,
            }]
            response_fixture = json.dumps(normalized_case, ensure_ascii=False)
        if "normalize C# contract" in user_text:
            contract_case = json.loads(fixture)
            contract_case["scripts"][0]["source"] = "public class UserScript { public void Init() { } public bool Process() { return true; } }"
            contract_case["scripts"][0]["inputs"] = []
            contract_case["scripts"][0]["outputs"] = []
            contract_case["scripts"][0]["operations"] = []
            contract_case["scripts"][0]["dependencies"] = []
            response_fixture = json.dumps(contract_case, ensure_ascii=False)
        if responses_api:
            valid = (
                self.path.endswith("/v1/responses")
                and request.get("store") is False
                and isinstance(request.get("input"), list)
                and request["input"][0].get("type") == "message"
                and request["input"][0].get("content", [])[0].get("type") == "input_text"
                and "json" in request["input"][0]["content"][0].get("text", "")
                and request.get("text", {}).get("format", {}).get("type") == "json_object"
                and self.headers.get("Authorization") == "Bearer offline-test-key"
            )
            if not valid:
                self.send_response(400)
                self.end_headers()
                self.wfile.write(b'invalid Responses API request')
                return
            response = {
                "id": "resp_offline_test",
                "status": "completed",
                "output": [
                    {"type": "reasoning", "summary": []},
                    {"type": "message", "role": "assistant", "content": [{"type": "output_text", "text": response_fixture}]},
                ],
            }
        else:
            valid = (
                self.path.endswith("/v1/chat/completions")
                and request.get("response_format", {}).get("type") == "json_object"
                and isinstance(request.get("messages"), list)
            )
            if not valid:
                self.send_response(400)
                self.end_headers()
                self.wfile.write(b'invalid Chat Completions request')
                return
            response = {"choices": [{"message": {"content": response_fixture}}]}
        data = json.dumps(response, ensure_ascii=False).encode("utf-8")
        if responses_api:
            data = gzip.compress(data)
        self.send_response(200)
        self.send_header("Content-Type", "application/json; charset=utf-8")
        if responses_api:
            self.send_header("Content-Encoding", "gzip")
        self.send_header("Content-Length", str(len(data)))
        self.end_headers()
        self.wfile.write(data)

    def log_message(self, format, *args):
        pass

server = http.server.HTTPServer(("127.0.0.1", port), Handler)
ready.write_text("ready", encoding="ascii")
server.handle_request()
server.handle_request()
server.handle_request()
server.server_close()
