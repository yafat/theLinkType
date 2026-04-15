using System;
using System.Runtime.InteropServices;
using theLinkType.Win32;

namespace theLinkType.Services;

public sealed class KeyboardHookService
{
    private IntPtr _hookId = IntPtr.Zero;
    private readonly NativeMethods.LowLevelKeyboardProc _proc;
    private bool _rightAltDown;

    public event EventHandler? RightAltPressed;
    public event EventHandler? RightAltReleased;

    public KeyboardHookService()
    {
        _proc = HookCallback;
    }

    public void Start()
    {
        if (_hookId != IntPtr.Zero) return;

        _hookId = NativeMethods.SetWindowsHookEx(
            NativeMethods.WH_KEYBOARD_LL,
            _proc,
            NativeMethods.GetCurrentModuleHandle(),
            0);

        if (_hookId == IntPtr.Zero)
        {
            throw new InvalidOperationException("Failed to install keyboard hook.");
        }
    }

    public void Stop()
    {
        if (_hookId == IntPtr.Zero) return;

        NativeMethods.UnhookWindowsHookEx(_hookId);
        _hookId = IntPtr.Zero;
        _rightAltDown = false;
    }

    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0)
        {
            var kb = Marshal.PtrToStructure<NativeMethods.KBDLLHOOKSTRUCT>(lParam);
            bool isRightAlt = kb.vkCode == NativeMethods.VK_RMENU;
            bool isLeftAlt = kb.vkCode == NativeMethods.VK_LMENU;
            int msg = wParam.ToInt32();

            if (isLeftAlt)
            {
                if ((msg == NativeMethods.WM_KEYDOWN || msg == NativeMethods.WM_SYSKEYDOWN) && !_rightAltDown)
                {
                    _rightAltDown = true;
                    RightAltPressed?.Invoke(this, EventArgs.Empty);
                }
                else if ((msg == NativeMethods.WM_KEYUP || msg == NativeMethods.WM_SYSKEYUP) && _rightAltDown)
                {
                    _rightAltDown = false;
                    RightAltReleased?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        return NativeMethods.CallNextHookEx(_hookId, nCode, wParam, lParam);
    }
}