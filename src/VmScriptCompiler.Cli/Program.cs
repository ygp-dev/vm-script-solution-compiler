using System.Text.Json;
using VmScriptCompiler.Core;

return await Cli.Run(args);

static class Cli
{
    public static Task<int> Run(string[] args)
    {
        try
        {
            if (args.Length == 0) return Task.FromResult(Fail("USAGE", Usage()));
            var root = RepositoryLocator.Find();
            object output = args[0] switch
            {
                "env" => new EnvironmentDetector().Detect(),
                "plan" => Plan(RequiredOption(args, "--spec")),
                "build" => Build(root, RequiredOption(args, "--spec"), RequiredOption(args, "--output")),
                "patch" => Patch(root, RequiredOption(args, "--base"), RequiredOption(args, "--spec"), RequiredOption(args, "--output")),
                "inspect" => Inspect(root, RequiredOption(args, "--file")),
                "validate" => Validate(root, RequiredOption(args, "--file")),
                _ => throw new CompilerException("USAGE", Usage())
            };
            Console.WriteLine(JsonSerializer.Serialize(output, JsonDefaults.Options));
            return Task.FromResult(0);
        }
        catch (CompilerException ex) { return Task.FromResult(Fail(ex.Code, ex.Message, ex.Details)); }
        catch (Exception ex) { return Task.FromResult(Fail("UNEXPECTED_ERROR", ex.Message)); }
    }
    private static object Plan(string spec)
    {
        return new CompilerFacade(RepositoryLocator.Find()).Plan(spec);
    }
    private static object Build(string root, string spec, string output)
    {
        var result = new CompilerFacade(root).Build(spec, output);
        return new { ok = true, taskDirectory = result.TaskDirectory, solution = result.SolutionFile, report = result.ReportFile, parseExitCode = result.Parse.ExitCode, inspectExitCode = result.Inspect.ExitCode };
    }
    private static object Patch(string root, string baseSolution, string spec, string output)
    {
        var result = new CompilerFacade(root).Patch(baseSolution, spec, output);
        return new { ok = true, taskDirectory = result.TaskDirectory, solution = result.SolutionFile, report = result.ReportFile, parseExitCode = result.Parse.ExitCode, inspectExitCode = result.Inspect.ExitCode };
    }
    private static object Inspect(string root, string file)
    {
        file = Path.GetFullPath(file);
        if (!File.Exists(file)) throw new CompilerException("SOLUTION_FILE_NOT_FOUND", "SOL file does not exist: " + file);
        SolArchiveValidator.ValidateVm44EntryNames(file);
        var result = new ParserClient(Path.Combine(root, "tools", "vm-solution-parser", "VMSolutionParser.Cli.exe")).Inspect(file);
        if (result.ExitCode != 0) throw new CompilerException("SOL_PARSE_FAILED", result.StandardError);
        return new { ok = true, output = result.StandardOutput };
    }
    private static object Validate(string root, string file)
    {
        return new CompilerFacade(root).ValidateSolution(file);
    }
    private static string RequiredOption(string[] args, string name) => args.SkipWhile(x => x != name).Skip(1).FirstOrDefault() ?? throw new CompilerException("USAGE", "缺少参数 " + name);
    private static int Fail(string code, string message, object? details = null) { Console.Error.WriteLine(JsonSerializer.Serialize(new { ok = false, error = code, message, details }, JsonDefaults.Options)); return 2; }
    private static string Usage() => "用法: vm-script-compiler env|plan --spec <file>|build --spec <file> --output <dir>|patch --base <sol> --spec <file> --output <dir>|inspect --file <sol>|validate --file <sol>";
}
