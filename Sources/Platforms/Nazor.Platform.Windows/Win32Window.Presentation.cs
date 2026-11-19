using static Nazor.Platform.Windows.Win32;

namespace Nazor.Platform.Windows;

internal sealed unsafe partial class Win32Window
{
    private bool changingTaskbarVisibility;
    public void SetAlwaysOnTop(bool value)
    {
        ObjectDisposedException.ThrowIf(hwnd == 0 || IsClosed, this);
        if (!modernWindowing) modernWindowing = initializeWindowing?.Invoke() ?? false;
        // AppWindow's presenter owns this state once the Windows App SDK manages the HWND.
        if (modernWindowing) WindowsWindowing.SetAlwaysOnTop(hwnd, value);
        else if (!SetWindowPos(hwnd, value ? HWND_TOPMOST : HWND_NOTOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE))
            throw new InvalidOperationException("Cannot change window topmost status.");
        isAlwaysOnTop = value;
        if (fullScreen) savedExStyle = value ? savedExStyle | (nint)WS_EX_TOPMOST : savedExStyle & ~(nint)WS_EX_TOPMOST;
    }

    public void SetTaskbarVisibility(bool value)
    {
        ObjectDisposedException.ThrowIf(hwnd == 0 || IsClosed, this);
        if (value == IsVisibleInTaskbar) return;
        var visible = IsWindowVisible(hwnd);
        var active = GetForegroundWindow() == hwnd;
        var topmost = isAlwaysOnTop;
        var normalSize = GetPlacement()?.LogicalClientSize ?? ClientSize;
        // Explorer only re-evaluates taskbar participation when the window is shown again.
        changingTaskbarVisibility = true;
        try
        {
            if (visible) { launcher?.BeforeHide(this); ShowWindow(hwnd, SW_HIDE); }
            var current = GetWindowLongPtrW(hwnd, -20);
            var next = value ? current & ~(nint)WS_EX_TOOLWINDOW : (current & ~(nint)WS_EX_APPWINDOW) | (nint)WS_EX_TOOLWINDOW;
            SetWindowLongPtrW(hwnd, -20, next);
            if (fullScreen) savedExStyle = value ? savedExStyle & ~(nint)WS_EX_TOOLWINDOW : (savedExStyle & ~(nint)WS_EX_APPWINDOW) | (nint)WS_EX_TOOLWINDOW;
            IsVisibleInTaskbar = value;
            TaskbarReady = false;
            SetWindowPos(hwnd, 0, 0, 0, 0, 0, SWP_FRAMECHANGED | SWP_NOMOVE | SWP_NOSIZE | SWP_NOZORDER | SWP_NOACTIVATE);
            Resize(normalSize);
        }
        finally
        {
            try
            {
                if (visible) ShowWindow(hwnd, active ? 5 : 8); // SW_SHOW/SW_SHOWNA preserve the previous activation.
                SetAlwaysOnTop(topmost);
            }
            finally { changingTaskbarVisibility = false; }
        }
        launcher?.Refresh();
    }
}
