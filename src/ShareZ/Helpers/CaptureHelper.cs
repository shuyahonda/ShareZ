using System.Runtime.InteropServices;
using Windows.Graphics.Capture;

namespace ShareZ.Helpers;

public static class CaptureHelper
{
    [ComImport]
    [Guid("3628E81B-3CAC-4C60-B7F4-23CE0E0C3356")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IGraphicsCaptureItemInterop
    {
        IntPtr CreateForWindow(
            [In] IntPtr window,
            [In] ref Guid iid,
            out IntPtr result);

        IntPtr CreateForMonitor(
            [In] IntPtr monitor,
            [In] ref Guid iid,
            out IntPtr result);
    }

    [DllImport("user32.dll")]
    private static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr lprcClip,
        MonitorEnumProc lpfnEnum, IntPtr dwData);

    private delegate bool MonitorEnumProc(IntPtr hMonitor, IntPtr hdcMonitor,
        ref RECT lprcMonitor, IntPtr dwData);

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left, Top, Right, Bottom;
    }

    public static GraphicsCaptureItem? CreateItemForWindow(IntPtr hwnd)
    {
        try
        {
            var factory = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow!);
            var interop = GraphicsCaptureItem.As<IGraphicsCaptureItemInterop>();
            var itemGuid = typeof(GraphicsCaptureItem).GUID;
            interop.CreateForWindow(hwnd, ref itemGuid, out var itemPtr);
            var item = GraphicsCaptureItem.FromAbi(itemPtr);
            return item;
        }
        catch
        {
            return null;
        }
    }

    public static GraphicsCaptureItem? CreateItemForMonitor(IntPtr hMonitor)
    {
        try
        {
            var interop = GraphicsCaptureItem.As<IGraphicsCaptureItemInterop>();
            var itemGuid = typeof(GraphicsCaptureItem).GUID;
            interop.CreateForMonitor(hMonitor, ref itemGuid, out var itemPtr);
            var item = GraphicsCaptureItem.FromAbi(itemPtr);
            return item;
        }
        catch
        {
            return null;
        }
    }

    public static IntPtr GetPrimaryMonitorHandle()
    {
        IntPtr primaryMonitor = IntPtr.Zero;

        EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, (IntPtr hMonitor, IntPtr hdcMonitor, ref RECT lprcMonitor, IntPtr dwData) =>
        {
            if (primaryMonitor == IntPtr.Zero)
            {
                primaryMonitor = hMonitor;
            }
            return true;
        }, IntPtr.Zero);

        return primaryMonitor;
    }

    public static List<IntPtr> GetAllMonitorHandles()
    {
        var monitors = new List<IntPtr>();

        EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, (IntPtr hMonitor, IntPtr hdcMonitor, ref RECT lprcMonitor, IntPtr dwData) =>
        {
            monitors.Add(hMonitor);
            return true;
        }, IntPtr.Zero);

        return monitors;
    }

    public static List<WindowInfo> GetVisibleWindows()
    {
        var windows = new List<WindowInfo>();
        var shellWindow = Interop.NativeMethods.GetShellWindow();

        Interop.NativeMethods.EnumWindows((hwnd, lParam) =>
        {
            if (hwnd == shellWindow) return true;
            if (!Interop.NativeMethods.IsWindowVisible(hwnd)) return true;

            int length = Interop.NativeMethods.GetWindowTextLength(hwnd);
            if (length == 0) return true;

            var titleChars = new char[length + 1];
            Interop.NativeMethods.GetWindowText(hwnd, titleChars, titleChars.Length);
            var title = new string(titleChars, 0, length);

            Interop.NativeMethods.GetWindowRect(hwnd, out var rect);

            windows.Add(new WindowInfo
            {
                Handle = hwnd,
                Title = title,
                Bounds = new Windows.Foundation.Rect(rect.Left, rect.Top, rect.Width, rect.Height)
            });

            return true;
        }, IntPtr.Zero);

        return windows;
    }
}

public class WindowInfo
{
    public IntPtr Handle { get; set; }
    public string Title { get; set; } = string.Empty;
    public Windows.Foundation.Rect Bounds { get; set; }
}
