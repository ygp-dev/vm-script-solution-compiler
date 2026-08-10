using System.Text.Json;
using System.Text.Json.Serialization;
using VmScriptCompiler.Agent;
using VmScriptCompiler.Core;

try
{
    if (args.Length == 0) throw new CompilerException("USAGE", "Usage: vm-script-agent plan|build|patch --prompt <text> [--base <sol>] [--output <dir>]");
    var command = args[0];
    var prompt = Required(args, "--prompt");
    var baseSolution = Option(args, "--base");
    var mode = command == "patch" ? "patch" : "create";
    if (command == "patch" && string.IsNullOrWhiteSpace(baseSolution)) throw new CompilerException("USAGE", "patch requires --base.");
    var provider = RequirementProviderFactory.Create(Option(args, "--provider"));
    var requirement = provider.Create(prompt, mode, baseSolution);
    var json = JsonSerializer.Serialize(requirement, new JsonSerializerOptions { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull });
    if (command == "plan") { Console.WriteLine(json); return 0; }
    if (command is not ("build" or "patch")) throw new CompilerException("USAGE", "Unknown Agent command: " + command);
    var output = Required(args, "--output");
    var temporary = Path.Combine(Path.GetTempPath(), "vm-script-agent-" + Guid.NewGuid().ToString("N") + ".json");
    try
    {
        File.WriteAllText(temporary, json);
        var compiler = new CompilerFacade(RepositoryLocator.Find());
        var result = command == "patch" ? compiler.Patch(baseSolution!, temporary, output) : compiler.Build(temporary, output);
        Console.WriteLine(JsonSerializer.Serialize(new { ok = true, requirement, result.TaskDirectory, result.SolutionFile, result.ReportFile, result.DefaultPersistenceNotices }, JsonDefaults.Options));
    }
    finally { if (File.Exists(temporary)) File.Delete(temporary); }
    return 0;
}
catch (CompilerException ex) { Console.Error.WriteLine(JsonSerializer.Serialize(new { ok = false, error = ex.Code, message = ex.Message }, JsonDefaults.Options)); return 2; }
catch (Exception ex) { Console.Error.WriteLine(JsonSerializer.Serialize(new { ok = false, error = "UNEXPECTED_ERROR", message = ex.Message }, JsonDefaults.Options)); return 2; }

static string Required(string[] values, string name) => Option(values, name) ?? throw new CompilerException("USAGE", "Missing option: " + name);
static string? Option(string[] values, string name) => values.SkipWhile(x => x != name).Skip(1).FirstOrDefault();
