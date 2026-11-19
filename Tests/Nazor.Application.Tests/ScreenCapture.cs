using System.Runtime.InteropServices;
using Nazor.Graphics;

namespace Nazor.Application.Tests;

internal static unsafe partial class ScreenCapture
{
    private const uint PW_CLIENTONLY = 1, PW_RENDERFULLCONTENT = 2;

    [StructLayout(LayoutKind.Sequential)]
    private struct BITMAPINFOHEADER
    {
        public uint biSize;
        public int biWidth, biHeight;
        public ushort biPlanes, biBitCount;
        public uint biCompression, biSizeImage;
        public int biXPelsPerMeter, biYPelsPerMeter;
        public uint biClrUsed, biClrImportant;
    }

    [StructLayout(LayoutKind.Sequential)] private struct RECT { public int left, top, right, bottom; }

    [LibraryImport("user32.dll")][return: MarshalAs(UnmanagedType.Bool)] private static partial bool PrintWindow(nint hwnd, nint hdc, uint flags);
    [LibraryImport("user32.dll")][return: MarshalAs(UnmanagedType.Bool)] private static partial bool GetClientRect(nint hwnd, out RECT rc);
    [LibraryImport("user32.dll")] private static partial nint GetDC(nint hwnd);
    [LibraryImport("user32.dll")] private static partial int ReleaseDC(nint hwnd, nint hdc);
    [LibraryImport("gdi32.dll")] private static partial nint CreateCompatibleDC(nint hdc);
    [LibraryImport("gdi32.dll")] private static partial nint CreateDIBSection(nint hdc, BITMAPINFOHEADER* bmi, uint usage, void** bits, nint section, uint offset);
    [LibraryImport("gdi32.dll")] private static partial nint SelectObject(nint hdc, nint obj);
    [LibraryImport("gdi32.dll")][return: MarshalAs(UnmanagedType.Bool)] private static partial bool DeleteObject(nint obj);
    [LibraryImport("gdi32.dll")][return: MarshalAs(UnmanagedType.Bool)] private static partial bool DeleteDC(nint hdc);
    [LibraryImport("gdi32.dll")][return: MarshalAs(UnmanagedType.Bool)] private static partial bool BitBlt(nint destination, int x, int y, int width, int height, nint source, int sourceX, int sourceY, uint operation);

    public static RenderedImage Capture(nint hwnd) => Capture(hwnd, repaint: true);

    public static RenderedImage CapturePresented(nint hwnd) => Capture(hwnd, repaint: false);

    public static RenderedImage CaptureDesktopWindow(nint hwnd) => Capture(hwnd, repaint: false, desktop: true);

    public static RenderedImage CaptureDesktopFrame(nint hwnd) => Capture(hwnd, repaint: false, desktop: true, frame: true);

    private static RenderedImage Capture(nint hwnd, bool repaint, bool desktop = false, bool frame = false)
    {
        RECT rc;
        var origin = new Nazor.Platform.Windows.Win32.POINT();
        if (frame)
        {
            if (Nazor.Platform.Windows.Win32.DwmGetWindowAttribute(hwnd, 9, &rc, (uint)sizeof(RECT)) < 0)
                throw new InvalidOperationException("Cannot read the visible window frame.");
            origin = new() { X = rc.left, Y = rc.top };
        }
        else
        {
            GetClientRect(hwnd, out rc);
            if (desktop) Nazor.Platform.Windows.Win32.ClientToScreen(hwnd, ref origin);
        }
        int width = rc.right - rc.left, height = rc.bottom - rc.top;
        var source = repaint || desktop ? 0 : hwnd;
        var screen = GetDC(source);
        var memory = CreateCompatibleDC(screen);
        var header = new BITMAPINFOHEADER { biSize = (uint)sizeof(BITMAPINFOHEADER), biWidth = width, biHeight = -height, biPlanes = 1, biBitCount = 32 };
        void* bits;
        var bitmap = CreateDIBSection(memory, &header, 0, &bits, 0, 0);
        var previous = SelectObject(memory, bitmap);
        try
        {
            var captured = repaint ? PrintWindow(hwnd, memory, PW_CLIENTONLY | PW_RENDERFULLCONTENT)
                : BitBlt(memory, 0, 0, width, height, screen, origin.X, origin.Y, 0x00CC0020);
            if (!captured) throw new InvalidOperationException("Window capture failed.");
            var pixels = new byte[width * height * 4];
            Marshal.Copy((nint)bits, pixels, 0, pixels.Length);
            for (var i = 3; i < pixels.Length; i += 4) pixels[i] = 255;
            return new RenderedImage(width, height, pixels);
        }
        finally
        {
            SelectObject(memory, previous);
            DeleteObject(bitmap);
            DeleteDC(memory);
            ReleaseDC(source, screen);
        }
    }
}
