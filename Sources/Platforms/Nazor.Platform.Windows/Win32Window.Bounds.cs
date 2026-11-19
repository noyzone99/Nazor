using Nazor.Graphics;
using static Nazor.Platform.Windows.Win32;

namespace Nazor.Platform.Windows;

internal sealed unsafe partial class Win32Window
{
    private double? minWidth, minHeight, maxWidth, maxHeight;

    public (int X, int Y)? Position
    {
        get
        {
            if (hwnd == 0 || IsClosed) return null;
            if (!fullScreen && !IsZoomed(hwnd) && !IsIconic(hwnd))
                return GetWindowRect(hwnd, out var bounds) ? (bounds.left, bounds.top) : null;
            var placement = NormalPlacement();
            var offset = WorkAreaOffset(placement.rcNormalPosition);
            return (placement.rcNormalPosition.left + offset.X, placement.rcNormalPosition.top + offset.Y);
        }
    }

    private POINT WorkAreaOffset(RECT bounds)
        => (FrameExStyle & WS_EX_TOOLWINDOW) != 0 ? default : MonitorWorkAreaOffset(bounds);

    private POINT MonitorWorkAreaOffset(RECT bounds)
    {
        var monitor = new MONITORINFO { cbSize = (uint)sizeof(MONITORINFO) };
        return GetMonitorInfo(MonitorFromRect(bounds, MONITOR_DEFAULTTONEAREST), ref monitor)
            ? new POINT { X = monitor.rcWork.left - monitor.rcMonitor.left, Y = monitor.rcWork.top - monitor.rcMonitor.top } : default;
    }

    private WINDOWPLACEMENT NormalPlacement()
    {
        if (fullScreen) return beforeFullScreen;
        var placement = new WINDOWPLACEMENT { length = (uint)sizeof(WINDOWPLACEMENT) };
        if (!GetWindowPlacement(hwnd, ref placement)) throw new InvalidOperationException("Cannot read normal window placement.");
        return placement;
    }

    public void Move(int? x, int? y)
    {
        if (hwnd == 0 || IsClosed) throw new ObjectDisposedException(nameof(Win32Window));
        if (x is null && y is null) return;
        var position = Position!.Value;
        if (!fullScreen && !IsZoomed(hwnd) && !IsIconic(hwnd))
        {
            GetWindowRect(hwnd, out var outerBounds);
            _ = checked((x ?? position.X) + (outerBounds.right - outerBounds.left));
            _ = checked((y ?? position.Y) + (outerBounds.bottom - outerBounds.top));
            if (!SetWindowPos(hwnd, 0, x ?? position.X, y ?? position.Y, 0, 0, SWP_NOSIZE | SWP_NOZORDER | SWP_NOACTIVATE))
                throw new InvalidOperationException("Cannot move window.");
            return;
        }
        var placement = NormalPlacement();
        var bounds = placement.rcNormalPosition;
        var width = bounds.right - bounds.left;
        var height = bounds.bottom - bounds.top;
        bounds.left = x ?? position.X;
        bounds.top = y ?? position.Y;
        bounds.right = checked(bounds.left + width);
        bounds.bottom = checked(bounds.top + height);
        var offset = WorkAreaOffset(bounds);
        bounds.left -= offset.X; bounds.right -= offset.X;
        bounds.top -= offset.Y; bounds.bottom -= offset.Y;
        placement.rcNormalPosition = bounds;
        if (fullScreen) beforeFullScreen = placement;
        else if (!SetWindowPlacement(hwnd, placement)) throw new InvalidOperationException("Cannot update restore position.");
    }

    public void SetSizeLimits(double? minimumWidth, double? minimumHeight, double? maximumWidth, double? maximumHeight)
    {
        foreach (var value in new[] { minimumWidth, minimumHeight, maximumWidth, maximumHeight })
            if (value.HasValue && (!double.IsFinite(value.Value) || value < 1 || value > 32768))
                throw new ArgumentOutOfRangeException(nameof(minimumWidth));
        if (minimumWidth > maximumWidth || minimumHeight > maximumHeight) throw new ArgumentException("Invalid window size limits.");
        if ((minWidth, minHeight, maxWidth, maxHeight) == (minimumWidth, minimumHeight, maximumWidth, maximumHeight)) return;
        (minWidth, minHeight, maxWidth, maxHeight) = (minimumWidth, minimumHeight, maximumWidth, maximumHeight);
        var normal = GetPlacement()?.LogicalClientSize ?? ClientSize;
        Resize(normal);
        if (IsZoomed(hwnd) && !fullScreen) UpdateMaximizedBounds();
    }

    private void UpdateMaximizedBounds()
    {
        var info = new MINMAXINFO();
        DefWindowProcW(hwnd, WM_GETMINMAXINFO, 0, (nint)(&info));
        ApplySizeLimits(hwnd, &info, updateMaximized: true);
        var monitor = new MONITORINFO { cbSize = (uint)sizeof(MONITORINFO) };
        if (GetMonitorInfo(MonitorFromWindow(hwnd, MONITOR_DEFAULTTONEAREST), ref monitor))
            SetWindowPos(hwnd, 0, monitor.rcMonitor.left + info.ptMaxPosition.X, monitor.rcMonitor.top + info.ptMaxPosition.Y,
                info.ptMaxSize.X, info.ptMaxSize.Y, SWP_NOZORDER | SWP_NOACTIVATE);
    }

    private Size Constrain(Size size) => new(
        (float)Math.Clamp(size.Width, minWidth ?? 1, maxWidth ?? 32768),
        (float)Math.Clamp(size.Height, minHeight ?? 1, maxHeight ?? 32768));

    private RECT ClientFrame(uint targetDpi)
    {
        var frame = new RECT();
        if (!clientFrame) AdjustWindowRectExForDpi(ref frame, fullScreen ? (uint)savedStyle : (uint)GetWindowLongPtrW(hwnd, -16), false, FrameExStyle, targetDpi);
        if (extended) frame.top = 0;
        return frame;
    }

    private void ApplySizeLimits(nint window, MINMAXINFO* info, bool updateMaximized = false)
    {
        if (fullScreen) return;
        var targetDpi = Math.Max(96, GetDpiForWindow(window));
        var frame = ClientFrame(targetDpi);
        var scale = targetDpi / 96d;
        int Width(double value) => checked((int)Math.Round(value * scale) + frame.right - frame.left);
        int Height(double value) => checked((int)Math.Round(value * scale) + frame.bottom - frame.top);
        if (minWidth is { } minW) info->ptMinTrackSize.X = Width(minW);
        if (minHeight is { } minH) info->ptMinTrackSize.Y = Height(minH);
        if (maxWidth is { } maxW) info->ptMaxTrackSize.X = Width(maxW);
        if (maxHeight is { } maxH) info->ptMaxTrackSize.Y = Height(maxH);
        if (maxWidth is null && maxHeight is null && !updateMaximized) return;
        var monitor = new MONITORINFO { cbSize = (uint)sizeof(MONITORINFO) };
        if (!GetMonitorInfo(MonitorFromWindow(window, MONITOR_DEFAULTTONEAREST), ref monitor)) return;
        info->ptMaxPosition.X = monitor.rcWork.left - monitor.rcMonitor.left + frame.left;
        info->ptMaxPosition.Y = monitor.rcWork.top - monitor.rcMonitor.top + frame.top;
        var availableWidth = monitor.rcWork.right - monitor.rcWork.left + frame.right - frame.left;
        var availableHeight = monitor.rcWork.bottom - monitor.rcWork.top + frame.bottom - frame.top;
        info->ptMaxSize.X = maxWidth is { } w ? Math.Min(availableWidth, Width(w)) : availableWidth;
        info->ptMaxSize.Y = maxHeight is { } h ? Math.Min(availableHeight, Height(h)) : availableHeight;
    }
}
