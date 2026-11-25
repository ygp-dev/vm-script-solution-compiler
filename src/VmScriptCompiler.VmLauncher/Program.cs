using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using Microsoft.Win32;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        try
        {
            using (var key = Registry.CurrentUser.OpenSubKey(@"Software\ygp-dev\VM Script Solution Compiler"))
            {
                var installed = key?.GetValue("InstallLocation") as string;
                var root = string.IsNullOrWhiteSpace(installed)
                    ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "VMSC")
                    : installed;
                var agent = Path.Combine(root, "Desktop", "vm-script-compiler-desktop.exe");
                if (!File.Exists(agent))
                {
                    MessageBox.Show("未找到 VM Script Solution Compiler。请重新安装桌面程序。", "AI 助手", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                Process.Start(new ProcessStartInfo(agent)
                {
                    UseShellExecute = true,
                    WorkingDirectory = Path.GetDirectoryName(agent)
                });
            }
        }
        catch (Exception error)
        {
            MessageBox.Show(error.Message, "AI 助手", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
