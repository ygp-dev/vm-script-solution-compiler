using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows;
using VMControls.WPF;

internal static class Program
{
    private const string VmRoot = @"C:\Program Files\VisionMaster4.4.0";

    [STAThread]
    private static int Main()
    {
        var stage = "startup";
        using (var watchdog = new Timer(_ =>
        {
            Console.Error.WriteLine("watchdogTimeoutStage=" + stage);
            Environment.Exit(124);
        }, null, TimeSpan.FromSeconds(20), Timeout.InfiniteTimeSpan))
        {
            var applications = Path.Combine(VmRoot, "Applications");
            var myLibs = Path.Combine(applications, "myLibs");
            var publicX64 = Path.Combine(applications, "PublicFile", "x64");
            Environment.SetEnvironmentVariable("PATH", string.Join(";", myLibs, publicX64, Environment.GetEnvironmentVariable("PATH")));
            AppDomain.CurrentDomain.AssemblyResolve += (_, args) => Resolve(new AssemblyName(args.Name), applications, myLibs, publicX64);
            try
            {
                stage = "construct-application";
                var application = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
                Console.WriteLine("targetFramework=" + typeof(MainViewControl).Assembly.GetCustomAttribute<System.Runtime.Versioning.TargetFrameworkAttribute>()?.FrameworkName);
                stage = "construct-control";
                var control = new MainViewControl();
                Console.WriteLine("created=" + control.GetType().FullName);
                var window = new Window { Content = control, Width = 1024, Height = 768, ShowInTaskbar = false, WindowStyle = WindowStyle.None, Opacity = 0, Left = -32000, Top = -32000 };
                stage = "show-window";
                window.Show();
                stage = "apply-template";
                control.ApplyTemplate();
                Console.WriteLine("visualTreeLoaded=" + control.IsLoaded);
                stage = "close-window";
                window.Close();
                application.Shutdown();
                return 0;
            }
            catch (Exception error)
            {
                Console.Error.WriteLine(error);
                return 1;
            }
        }
    }

    private static Assembly Resolve(AssemblyName name, string applications, params string[] directories)
    {
        foreach (var directory in directories)
        {
            var file = Path.Combine(directory, name.Name + ".dll");
            if (File.Exists(file)) return Assembly.LoadFrom(file);
        }
        var recursive = Directory.EnumerateFiles(applications, name.Name + ".dll", SearchOption.AllDirectories)
            .OrderBy(path => path.IndexOf("\\x86\\", StringComparison.OrdinalIgnoreCase) >= 0 ? 1 : 0)
            .ThenBy(path => path.Length)
            .FirstOrDefault();
        return recursive == null ? null : Assembly.LoadFrom(recursive);
    }
}
