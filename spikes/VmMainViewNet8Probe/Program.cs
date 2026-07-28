using System;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;
using System.Windows;
using VMControls.WPF;

internal static class Program
{
    private const string VmRoot = @"C:\Program Files\VisionMaster4.4.0";

    [STAThread]
    private static int Main()
    {
        var stage = "startup";
        using var watchdog = new System.Threading.Timer(_ =>
        {
            Console.Error.WriteLine("watchdogTimeoutStage=" + stage);
            Environment.Exit(124);
        }, null, TimeSpan.FromSeconds(20), System.Threading.Timeout.InfiniteTimeSpan);
        var myLibs = Path.Combine(VmRoot, "Applications", "myLibs");
        var publicX64 = Path.Combine(VmRoot, "Applications", "PublicFile", "x64");
        var shellDll = Path.Combine(VmRoot, "Applications", "Module(sp)", "x64", "Logic", "ShellModule", "DLL");
        var applications = Path.Combine(VmRoot, "Applications");
        Environment.SetEnvironmentVariable("PATH", string.Join(';', new[] { myLibs, publicX64, shellDll, Environment.GetEnvironmentVariable("PATH") }));
        AssemblyLoadContext.Default.Resolving += (_, name) => Resolve(name, applications, myLibs, publicX64, shellDll);

        try
        {
            stage = "construct-application";
            var application = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
            Console.WriteLine("assembly=" + typeof(MainViewControl).Assembly.FullName);
            Console.WriteLine("targetFramework=" + typeof(MainViewControl).Assembly.GetCustomAttribute<System.Runtime.Versioning.TargetFrameworkAttribute>()?.FrameworkName);
            stage = "construct-control";
            var control = new MainViewControl();
            Console.WriteLine("created=" + control.GetType().FullName);
            Console.WriteLine("baseType=" + control.GetType().BaseType?.FullName);
            var window = new Window
            {
                Content = control,
                Width = 1024,
                Height = 768,
                ShowInTaskbar = false,
                WindowStyle = WindowStyle.None,
                Opacity = 0,
                Left = -32000,
                Top = -32000
            };
            stage = "show-window";
            window.Show();
            stage = "apply-template";
            control.ApplyTemplate();
            Console.WriteLine("visualTreeLoaded=" + control.IsLoaded);
            Console.WriteLine("actualSize=" + control.ActualWidth + "x" + control.ActualHeight);
            stage = "close-window";
            window.Close();
            stage = "shutdown-application";
            application.Shutdown();
            stage = "complete";
            return 0;
        }
        catch (Exception error)
        {
            Console.Error.WriteLine(error.ToString());
            return 1;
        }
    }

    private static Assembly? Resolve(AssemblyName name, string applications, params string[] directories)
    {
        foreach (var directory in directories)
        {
            var file = Path.Combine(directory, name.Name + ".dll");
            if (File.Exists(file)) return AssemblyLoadContext.Default.LoadFromAssemblyPath(file);
        }
        var recursive = Directory.EnumerateFiles(applications, name.Name + ".dll", SearchOption.AllDirectories)
            .OrderBy(path => path.Contains("\\x86\\", StringComparison.OrdinalIgnoreCase) ? 1 : 0)
            .ThenBy(path => path.Length)
            .FirstOrDefault();
        if (recursive is not null) return AssemblyLoadContext.Default.LoadFromAssemblyPath(recursive);
        return null;
    }
}
