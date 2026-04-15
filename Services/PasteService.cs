using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using theLinkType.Win32;

namespace theLinkType.Services;

public sealed class PasteService
{
    private readonly Dispatcher _dispatcher;

    public PasteService(Dispatcher dispatcher)
    {
        _dispatcher = dispatcher;
    }

    public async Task PasteTextAsync(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return;

        IDataObject? backup = null;

        await _dispatcher.InvokeAsync(() =>
        {
            try
            {
                if (Clipboard.ContainsText() || Clipboard.GetDataObject() is not null)
                {
                    backup = Clipboard.GetDataObject();
                }
            }
            catch
            {
                backup = null;
            }

            Clipboard.SetText(text);
        });

        await Task.Delay(80);
        SendCtrlV();
        await Task.Delay(150);

        if (backup is not null)
        {
            await _dispatcher.InvokeAsync(() =>
            {
                try
                {
                    Clipboard.SetDataObject(backup, true);
                }
                catch
                {
                    // Ignore clipboard restore issues.
                }
            });
        }
    }

    private static void SendCtrlV()
    {
        NativeMethods.keybd_event(NativeMethods.VK_CONTROL, 0, 0, UIntPtr.Zero);
        NativeMethods.keybd_event(NativeMethods.VK_V, 0, 0, UIntPtr.Zero);

        NativeMethods.keybd_event(NativeMethods.VK_V, 0, NativeMethods.KEYEVENTF_KEYUP, UIntPtr.Zero);
        NativeMethods.keybd_event(NativeMethods.VK_CONTROL, 0, NativeMethods.KEYEVENTF_KEYUP, UIntPtr.Zero);
    }
}