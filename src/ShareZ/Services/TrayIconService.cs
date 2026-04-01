using System.Runtime.InteropServices;
using ShareZ.Interop;

namespace ShareZ.Services;

public class TrayIconService : IDisposable
{
    private readonly IntPtr _hwnd;
    private NativeMethods.NOTIFYICONDATA _notifyIconData;
    private bool _isInitialized;

    public event EventHandler? TrayIconClicked;
    public event EventHandler? TrayIconDoubleClicked;

    public TrayIconService(IntPtr hwnd)
    {
        _hwnd = hwnd;
    }

    public void Initialize()
    {
        _notifyIconData = new NativeMethods.NOTIFYICONDATA
        {
            cbSize = (uint)Marshal.SizeOf<NativeMethods.NOTIFYICONDATA>(),
            hWnd = _hwnd,
            uID = 1,
            uFlags = NativeMethods.NIF_MESSAGE | NativeMethods.NIF_ICON | NativeMethods.NIF_TIP,
            uCallbackMessage = NativeMethods.WM_TRAYICON,
            hIcon = NativeMethods.LoadIcon(IntPtr.Zero, (IntPtr)32512), // IDI_APPLICATION
            szTip = "ShareZ - Screenshot Tool",
            szInfo = string.Empty,
            szInfoTitle = string.Empty
        };

        NativeMethods.Shell_NotifyIcon(NativeMethods.NIM_ADD, ref _notifyIconData);
        _isInitialized = true;
    }

    public void ShowBalloonNotification(string title, string message)
    {
        if (!_isInitialized) return;

        _notifyIconData.uFlags |= NativeMethods.NIF_INFO;
        _notifyIconData.szInfoTitle = title;
        _notifyIconData.szInfo = message;
        _notifyIconData.dwInfoFlags = 0x00000001; // NIIF_INFO

        NativeMethods.Shell_NotifyIcon(NativeMethods.NIM_MODIFY, ref _notifyIconData);
    }

    public void Dispose()
    {
        if (_isInitialized)
        {
            NativeMethods.Shell_NotifyIcon(NativeMethods.NIM_DELETE, ref _notifyIconData);
            _isInitialized = false;
        }
    }
}
