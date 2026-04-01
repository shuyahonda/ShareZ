using System.Runtime.InteropServices;
using ShareZ.Interop;
using ShareZ.Models;
using Windows.System;

namespace ShareZ.Services;

public class HotkeyService : IDisposable
{
    private readonly IntPtr _hwnd;
    private readonly Dictionary<int, Action> _hotkeyActions = new();
    private int _nextId = 1;
    private IntPtr _oldWndProc;
    private NativeMethods.WndProcDelegate? _wndProcDelegate;

    public event EventHandler<string>? HotkeyPressed;

    public HotkeyService(IntPtr hwnd)
    {
        _hwnd = hwnd;
        SubclassWindow();
    }

    private void SubclassWindow()
    {
        _wndProcDelegate = WndProc;
        var newWndProc = Marshal.GetFunctionPointerForDelegate(_wndProcDelegate);
        _oldWndProc = NativeMethods.SetWindowLongPtr(_hwnd, NativeMethods.GWL_WNDPROC, newWndProc);
    }

    private IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
    {
        if (msg == NativeMethods.WM_HOTKEY)
        {
            int id = wParam.ToInt32();
            if (_hotkeyActions.TryGetValue(id, out var action))
            {
                action.Invoke();
            }
        }
        return NativeMethods.CallWindowProc(_oldWndProc, hWnd, msg, wParam, lParam);
    }

    public void RegisterDefaultHotkeys(AppSettings settings)
    {
        foreach (var hotkey in settings.Hotkeys.Where(h => h.IsEnabled))
        {
            RegisterHotkey(hotkey.Modifiers, hotkey.Key, () =>
            {
                HotkeyPressed?.Invoke(this, hotkey.Action);
            });
        }
    }

    public int RegisterHotkey(VirtualKeyModifiers modifiers, VirtualKey key, Action action)
    {
        int id = _nextId++;
        uint fsModifiers = 0;

        if (modifiers.HasFlag(VirtualKeyModifiers.Menu)) fsModifiers |= 0x0001; // MOD_ALT
        if (modifiers.HasFlag(VirtualKeyModifiers.Control)) fsModifiers |= 0x0002; // MOD_CONTROL
        if (modifiers.HasFlag(VirtualKeyModifiers.Shift)) fsModifiers |= 0x0004; // MOD_SHIFT
        if (modifiers.HasFlag(VirtualKeyModifiers.Windows)) fsModifiers |= 0x0008; // MOD_WIN

        if (NativeMethods.RegisterHotKey(_hwnd, id, fsModifiers, (uint)key))
        {
            _hotkeyActions[id] = action;
            return id;
        }

        return -1;
    }

    public void UnregisterHotkey(int id)
    {
        NativeMethods.UnregisterHotKey(_hwnd, id);
        _hotkeyActions.Remove(id);
    }

    public void UnregisterAll()
    {
        foreach (var id in _hotkeyActions.Keys.ToList())
        {
            UnregisterHotKey(id);
        }
    }

    private void UnregisterHotKey(int id)
    {
        NativeMethods.UnregisterHotKey(_hwnd, id);
        _hotkeyActions.Remove(id);
    }

    public void Dispose()
    {
        UnregisterAll();
        if (_oldWndProc != IntPtr.Zero)
        {
            NativeMethods.SetWindowLongPtr(_hwnd, NativeMethods.GWL_WNDPROC, _oldWndProc);
        }
    }
}
