using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;

namespace VmScriptCompiler.Desktop;

/// <summary>
/// Owns the custom-chrome behaviour for a borderless window: the dark DWM caption, drag/double-click
/// handling on the fake title bar, and the WM_GETMINMAXINFO hook that keeps a maximized window inside
/// the monitor work area instead of covering the taskbar.
/// </summary>
internal sealed class WindowChromeController(Window window)
{
    private const int DwmwaUseImmersiveDarkMode = 20;
    private const int DwmwaUseImmersiveDarkModeLegacy = 19;
    private const int DwmwaCaptionColor = 35;
    private const int DwmwaTextColor = 36;
    private const int WmGetMinMaxInfo = 0x0024;
    private const uint MonitorDefaultToNearest = 0x00000002;

    /// <summary>DWM COLORREF is stored as 0x00BBGGRR, matching the app's #0B1118 palette.</summary>
    private const uint CaptionColor = 0x0018110Bu;
    private const uint CaptionTextColor = 0x00FBF7F4u;

    private HwndSource? _source;

    public void AttachSource()
    {
        ApplyDarkTitleBar();
        _source = PresentationSource.FromVisual(window) as HwndSource;
        _source?.AddHook(WindowMessageHook);
    }

    public void DetachSource()
    {
        _source?.RemoveHook(WindowMessageHook);
        _source = null;
    }

    public void Toggle() =>
        window.WindowState = window.WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;

    public void Minimize() => window.WindowState = WindowState.Minimized;

    public void Close() => window.Close();

    /// <summary>Glyph for the maximize/restore button in the current window state.</summary>
    public string StateGlyph => window.WindowState == WindowState.Maximized ? "❐" : "□";

    /// <summary>Handles a click on the fake title bar: double-click toggles, single-click drags.</summary>
    public void HandleTitleBarClick(MouseButtonEventArgs e)
    {
        if (e.ChangedButton != MouseButton.Left) return;
        if (e.ClickCount == 2)
        {
            Toggle();
            return;
        }
        if (window.WindowState == WindowState.Maximized) return;
        try { window.DragMove(); }
        catch (InvalidOperationException) { }
    }

    public void ApplyDarkTitleBar()
    {
        if (!OperatingSystem.IsWindows()) return;
        var hwnd = new WindowInteropHelper(window).Handle;
        if (hwnd == IntPtr.Zero) return;

        try
        {
            var darkMode = 1;
            if (SetDwmWindowAttributeInt(hwnd, DwmwaUseImmersiveDarkMode, ref darkMode, Marshal.SizeOf<int>()) != 0)
                _ = SetDwmWindowAttributeInt(hwnd, DwmwaUseImmersiveDarkModeLegacy, ref darkMode, Marshal.SizeOf<int>());

            var captionColor = CaptionColor;
            var textColor = CaptionTextColor;
            _ = SetDwmWindowAttributeColor(hwnd, DwmwaCaptionColor, ref captionColor, Marshal.SizeOf<uint>());
            _ = SetDwmWindowAttributeColor(hwnd, DwmwaTextColor, ref textColor, Marshal.SizeOf<uint>());
        }
        catch (DllNotFoundException) { }
        catch (EntryPointNotFoundException) { }
    }

    private static nint WindowMessageHook(nint hwnd, int message, nint wParam, nint lParam, ref bool handled)
    {
        if (message != WmGetMinMaxInfo || lParam == IntPtr.Zero) return IntPtr.Zero;

        var monitor = MonitorFromWindow(hwnd, MonitorDefaultToNearest);
        if (monitor == IntPtr.Zero) return IntPtr.Zero;

        var info = new NativeMonitorInfo { Size = Marshal.SizeOf<NativeMonitorInfo>() };
        if (!GetMonitorInfo(monitor, ref info)) return IntPtr.Zero;

        var minMax = Marshal.PtrToStructure<NativeMinMaxInfo>(lParam);
        minMax.MaxPosition.X = info.Work.Left - info.Monitor.Left;
        minMax.MaxPosition.Y = info.Work.Top - info.Monitor.Top;
        minMax.MaxSize.X = info.Work.Right - info.Work.Left;
        minMax.MaxSize.Y = info.Work.Bottom - info.Work.Top;
        Marshal.StructureToPtr(minMax, lParam, false);
        handled = true;
        return IntPtr.Zero;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativePoint
    {
        public int X;
        public int Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeRect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeMinMaxInfo
    {
        public NativePoint Reserved;
        public NativePoint MaxSize;
        public NativePoint MaxPosition;
        public NativePoint MinTrackSize;
        public NativePoint MaxTrackSize;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private struct NativeMonitorInfo
    {
        public int Size;
        public NativeRect Monitor;
        public NativeRect Work;
        public uint Flags;
    }

    [DllImport("dwmapi.dll", EntryPoint = "DwmSetWindowAttribute", PreserveSig = true)]
    private static extern int SetDwmWindowAttributeInt(nint hwnd, int attribute, ref int value, int valueSize);

    [DllImport("dwmapi.dll", EntryPoint = "DwmSetWindowAttribute", PreserveSig = true)]
    private static extern int SetDwmWindowAttributeColor(nint hwnd, int attribute, ref uint value, int valueSize);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern nint MonitorFromWindow(nint hwnd, uint flags);

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    private static extern bool GetMonitorInfo(nint monitor, ref NativeMonitorInfo info);
}
