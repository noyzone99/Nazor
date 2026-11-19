using System.Runtime.InteropServices;
using Nazor.Graphics;
using SkiaSharp;
using static Nazor.Platform.Windows.Win32;

namespace Nazor.Platform.Windows;

internal sealed unsafe partial class Win32Window
{
    private const int HTLEFT = 10, HTRIGHT = 11, HTBOTTOM = 15, HTBOTTOMLEFT = 16, HTBOTTOMRIGHT = 17;
    private const uint WM_WINDOWPOSCHANGED = 0x0047, WM_ENTERSIZEMOVE = 0x0231, GW_HWNDNEXT = 2;
    private const uint WS_POPUP = 0x80000000, WS_EX_LAYERED = 0x00080000, WS_EX_NOACTIVATE = 0x08000000;
    private const uint SWP_SHOWWINDOW = 0x0040, ULW_ALPHA = 2;
    private const int RGN_DIFF = 4;
    private const byte AC_SRC_ALPHA = 1;

    private float? resizeInset, resizeCornerLength;
    private float resizeOutset;
    private nint resizeWindow;
    private (int Width, int Height, int Margin)? resizeRegion;
    private CornerRadius configuredFrameRadius;
    private IReadOnlyList<DrawShadow> frameShadows = [];
    private (int Width, int Height, uint Dpi, CornerRadius Radius)? frameRegion;
    private (int Width, int Height, uint Dpi)? shadowFrame;
    private int shadowMargin;
    private nint shadowWindow;
    private bool updatingClientFrame;

    public bool UsesClientFrameAppearance => clientFrame;
    public bool DrawClientFrame => UsesClientFrameAppearance && !IsClosed && State == WindowState.Normal;
    public CornerRadius ClientFrameRadius => DrawClientFrame ? WindowFrameGeometry.NormalizeRadius(configuredFrameRadius, ClientSize) : default;
    public Rect ResizeGripBounds => showResizeGrip && !IsClosed && Capabilities.CanResize && State == WindowState.Normal
        ? WindowFrameGeometry.ResizeGripBounds(ClientSize) : default;

    public void SetClientFrameAppearance(CornerRadius radius, IReadOnlyList<DrawShadow> shadows)
    {
        if (!UsesClientFrameAppearance || configuredFrameRadius == radius && frameShadows.SequenceEqual(shadows)) return;
        configuredFrameRadius = radius;
        frameShadows = shadows;
        shadowFrame = null;
        UpdateClientFrame();
    }

    public void SetFrameResizeGeometry(float? inset, float? outset, float? cornerLength)
    {
        var outer = Math.Clamp(outset ?? 0, 0, 128);
        if ((resizeInset, resizeOutset, resizeCornerLength) == (inset, outer, cornerLength)) return;
        resizeInset = inset;
        resizeOutset = outer;
        resizeCornerLength = cornerLength;
        UpdateClientFrame();
    }

    private nint ClientResizeHit(Point point, POINT physicalPoint)
    {
        if (clientFrame || resizeInset is not null)
        {
            return WindowFrameGeometry.ResizeEdgeAt(ClientSize, point, resizeInset ?? 4, clientFrame ? resizeOutset : 0, resizeCornerLength) switch
            {
                1 => HTTOP,
                2 => HTBOTTOM,
                4 => HTLEFT,
                8 => HTRIGHT,
                5 => HTTOPLEFT,
                6 => HTBOTTOMLEFT,
                9 => HTTOPRIGHT,
                10 => HTBOTTOMRIGHT,
                _ => 0,
            };
        }

        if (physicalPoint.Y >= FrameThickness) return 0;
        var corner = resizeCornerLength is { } length
            ? Math.Min(length * Scale, bufferWidth / 2f)
            : GetSystemMetricsForDpi(SM_CXSIZEFRAME, dpi) + GetSystemMetricsForDpi(SM_CXPADDEDBORDER, dpi);
        return physicalPoint.X < corner ? HTTOPLEFT : physicalPoint.X >= bufferWidth - corner ? HTTOPRIGHT : HTTOP;
    }

    private void UpdateClientFrame()
    {
        if (!UsesClientFrameAppearance || hwnd == 0 || IsClosed || updatingClientFrame) return;
        updatingClientFrame = true;
        try
        {
            if (!DrawClientFrame || !GetWindowRect(hwnd, out var bounds))
            {
                ApplyFrameRegion(0, 0, default);
                HideClientFrameShadow();
                HideResizeOutset();
                return;
            }

            var width = bounds.right - bounds.left;
            var height = bounds.bottom - bounds.top;
            var size = new Size(width / Scale, height / Scale);
            var radius = WindowFrameGeometry.NormalizeRadius(configuredFrameRadius, size);
            ApplyFrameRegion(width, height, radius);
            UpdateResizeOutset(bounds);
            if (frameShadows.Count == 0 || !IsWindowVisible(hwnd)) HideClientFrameShadow();
            else UpdateClientFrameShadow(bounds, size, radius);
        }
        finally { updatingClientFrame = false; }
    }

    private void UpdateResizeOutset(RECT bounds)
    {
        if (resizeOutset <= 0 || !Capabilities.CanResize || !IsWindowVisible(hwnd)) { HideResizeOutset(); return; }
        var margin = (int)Math.Ceiling(resizeOutset * Scale);
        var width = bounds.right - bounds.left;
        var height = bounds.bottom - bounds.top;
        if (resizeWindow == 0)
        {
            fixed (char* name = "STATIC")
                resizeWindow = CreateWindowExW(WS_EX_LAYERED | WS_EX_NOACTIVATE | WS_EX_TOOLWINDOW, name, null,
                    WS_POPUP | 4, 0, 0, 0, 0, hwnd, 0, Win32Platform.Instance, 0);
            if (resizeWindow == 0 || !SetPropW(resizeWindow, "UIA_WindowVisibilityOverridden", 2) || !SetLayeredWindowAttributes(resizeWindow, 0, 1, 2) ||
                !SetWindowSubclass(resizeWindow, &ResizeOutsetProcedure, 2, (nuint)GCHandle.ToIntPtr(self)))
                throw new InvalidOperationException("Cannot create the outer resize input window.");
        }
        if (resizeRegion != (width, height, margin))
        {
            var outer = CreateRectRgn(0, 0, width + 2 * margin, height + 2 * margin);
            var inner = CreateRectRgn(margin, margin, width + margin, height + margin);
            try
            {
                if (outer == 0 || inner == 0 || CombineRgn(outer, outer, inner, RGN_DIFF) == 0 || SetWindowRgn(resizeWindow, outer, true) == 0)
                    throw new InvalidOperationException("Cannot configure the outer resize input region.");
                outer = 0;
                resizeRegion = (width, height, margin);
            }
            finally { if (inner != 0) DeleteObject(inner); if (outer != 0) DeleteObject(outer); }
        }
        if (!SetWindowPos(resizeWindow, hwnd, bounds.left - margin, bounds.top - margin,
            width + 2 * margin, height + 2 * margin, SWP_NOACTIVATE | SWP_SHOWWINDOW))
            throw new InvalidOperationException("Cannot position the outer resize input window.");
    }

    private void HideResizeOutset()
    {
        if (resizeWindow != 0 && IsWindowVisible(resizeWindow)) ShowWindow(resizeWindow, SW_HIDE);
    }

    private void DestroyResizeOutset()
    {
        if (resizeWindow != 0) DestroyWindow(resizeWindow);
        resizeWindow = 0;
        resizeRegion = null;
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvStdcall)])]
    private static nint ResizeOutsetProcedure(nint handle, uint message, nint wParam, nint lParam, nuint id, nuint data)
    {
        try
        {
            var window = (Win32Window)GCHandle.FromIntPtr((nint)data).Target!;
            if (message == WM_NCHITTEST)
            {
                if (!window.DrawClientFrame || !window.Capabilities.CanResize) return -1;
                var hit = window.ClientResizeHit(window.ClientPointFromScreen(lParam), default);
                return hit == 0 ? -1 : hit;
            }
            if (message == WM_NCLBUTTONDOWN && window.DrawClientFrame && window.Capabilities.CanResize)
                return SendResizeMessage(window.hwnd, message, wParam, lParam);
            if (message == 0x0021) return 3;
            if (message == WM_NCDESTROY)
            {
                RemovePropW(handle, "UIA_WindowVisibilityOverridden");
                RemoveWindowSubclass(handle, &ResizeOutsetProcedure, id);
                window.resizeWindow = 0;
                window.resizeRegion = null;
            }
        }
        catch (Exception exception) { Console.Error.WriteLine($"Nazor: outer resize input failed: {exception}"); }
        return DefSubclassProc(handle, message, wParam, lParam);
    }

    [LibraryImport("user32.dll", EntryPoint = "SendMessageW")]
    private static partial nint SendResizeMessage(nint window, uint message, nint wParam, nint lParam);
    [LibraryImport("user32.dll")][return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool SetLayeredWindowAttributes(nint window, uint color, byte alpha, uint flags);

    private void ApplyFrameRegion(int width, int height, CornerRadius radius)
    {
        if (radius.IsZero)
        {
            if (frameRegion is not null && SetWindowRgn(hwnd, 0, true) != 0) frameRegion = null;
            return;
        }

        var geometry = (width, height, dpi, radius);
        if (frameRegion == geometry) return;
        var region = CreateRectRgn(0, 0, width, height);
        if (region == 0) return;
        if (RoundCorner(region, width, height, radius.TopLeft, right: false, bottom: false) &&
            RoundCorner(region, width, height, radius.TopRight, right: true, bottom: false) &&
            RoundCorner(region, width, height, radius.BottomRight, right: true, bottom: true) &&
            RoundCorner(region, width, height, radius.BottomLeft, right: false, bottom: true) && SetWindowRgn(hwnd, region, true) != 0)
            frameRegion = geometry;
        else DeleteObject(region);
    }

    private bool RoundCorner(nint frame, int width, int height, Radius radius, bool right, bool bottom)
    {
        var x = radius.X * Scale;
        var y = radius.Y * Scale;
        if (x <= 0 || y <= 0) return true;
        var rows = (int)Math.Ceiling(y);
        var start = 0;
        var previous = -1;
        for (var row = 0; row <= rows; row++)
        {
            var distance = Math.Max(0, 1 - (row + 1) / y);
            var cut = row == rows ? 0 : (int)Math.Floor(x * (1 - Math.Sqrt(1 - distance * distance)));
            if (cut == previous) continue;
            if (previous > 0)
            {
                var left = right ? width - previous : 0;
                var top = bottom ? height - row : start;
                var region = CreateRectRgn(left, top, left + previous, top + row - start);
                if (region == 0) return false;
                try { if (CombineRgn(frame, frame, region, RGN_DIFF) == 0) return false; }
                finally { DeleteObject(region); }
            }
            previous = cut;
            start = row;
        }
        return true;
    }

    private void UpdateClientFrameShadow(RECT bounds, Size size, CornerRadius radius)
    {
        var geometry = (bounds.right - bounds.left, bounds.bottom - bounds.top, dpi);
        if (shadowFrame != geometry)
        {
            using var bitmap = WindowFrameShadow.Create(size, Scale, radius, frameShadows, out var margin);
            if (bitmap is null)
            {
                HideClientFrameShadow();
                return;
            }

            shadowMargin = (int)Math.Ceiling(margin * Scale);
            if (shadowWindow == 0)
            {
                fixed (char* className = "STATIC")
                    shadowWindow = CreateWindowExW(WS_EX_LAYERED | WS_EX_NOACTIVATE | WS_EX_TOOLWINDOW | WS_EX_TRANSPARENT, className,
                        null, WS_POPUP, 0, 0, 0, 0, 0, 0, Win32Platform.Instance, 0);
            }
            if (shadowWindow == 0 || !PresentShadowBitmap(bitmap, bounds.left - shadowMargin, bounds.top - shadowMargin))
            {
                HideClientFrameShadow();
                return;
            }
            shadowFrame = geometry;
        }

        var positionFlags = SWP_NOSIZE | SWP_NOACTIVATE | SWP_SHOWWINDOW;
        if (GetWindow(hwnd, GW_HWNDNEXT) == shadowWindow) positionFlags |= SWP_NOZORDER;
        SetWindowPos(shadowWindow, hwnd, bounds.left - shadowMargin, bounds.top - shadowMargin, 0, 0, positionFlags);
    }

    private bool PresentShadowBitmap(SKBitmap bitmap, int x, int y) =>
        PresentLayeredPixels(shadowWindow, (byte*)bitmap.GetPixels(), bitmap.Width, bitmap.Height, bitmap.RowBytes, x, y);

    private static bool PresentLayeredPixels(nint window, byte* pixels, int width, int height, int rowBytes, int x, int y)
    {
        var header = new BITMAPINFOHEADER
        {
            biSize = (uint)sizeof(BITMAPINFOHEADER), biWidth = width, biHeight = -height,
            biPlanes = 1, biBitCount = 32,
        };
        var dc = CreateCompatibleDC(0);
        if (dc == 0) return false;
        var dib = CreateDIBSection(dc, &header, 0, out var bits, 0, 0);
        nint previous = 0;
        try
        {
            if (dib == 0 || bits == 0) return false;
            previous = SelectObject(dc, dib);
            if (previous == 0) return false;
            var stride = width * 4;
            for (var row = 0; row < height; row++)
                Buffer.MemoryCopy(pixels + row * rowBytes, (byte*)bits + row * stride, stride, stride);
            var position = new POINT { X = x, Y = y };
            var size = new LayeredSize { Width = width, Height = height };
            var origin = new POINT();
            var blend = new LayeredBlend { SourceConstantAlpha = 255, AlphaFormat = AC_SRC_ALPHA };
            return UpdateLayeredWindow(window, 0, in position, in size, dc, in origin, 0, in blend, ULW_ALPHA);
        }
        finally
        {
            if (previous != 0) SelectObject(dc, previous);
            if (dib != 0) DeleteObject(dib);
            DeleteDC(dc);
        }
    }

    private void HideClientFrameShadow()
    {
        if (shadowWindow != 0 && IsWindowVisible(shadowWindow)) ShowWindow(shadowWindow, SW_HIDE);
    }

    private void DestroyClientFrameShadow()
    {
        if (shadowWindow == 0) return;
        DestroyWindow(shadowWindow);
        shadowWindow = 0;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct LayeredSize { public int Width, Height; }

    [StructLayout(LayoutKind.Sequential)]
    private struct LayeredBlend { public byte BlendOperation, BlendFlags, SourceConstantAlpha, AlphaFormat; }

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool UpdateLayeredWindow(nint window, nint destination, in POINT position, in LayeredSize size,
        nint source, in POINT origin, uint colorKey, in LayeredBlend blend, uint flags);

    [LibraryImport("user32.dll")]
    private static partial int SetWindowRgn(nint window, nint region, [MarshalAs(UnmanagedType.Bool)] bool redraw);

    [LibraryImport("gdi32.dll")] private static partial nint CreateCompatibleDC(nint dc);
    [LibraryImport("gdi32.dll")] private static partial nint SelectObject(nint dc, nint obj);
    [LibraryImport("gdi32.dll")] private static partial nint CreateRectRgn(int left, int top, int right, int bottom);
    [LibraryImport("gdi32.dll")] private static partial int CombineRgn(nint destination, nint first, nint second, int mode);
}
