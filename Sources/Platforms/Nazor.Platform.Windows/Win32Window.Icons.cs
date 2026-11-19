using System.ComponentModel;
using System.Runtime.InteropServices;
using Nazor.Graphics;
using static Nazor.Platform.Windows.Win32;

namespace Nazor.Platform.Windows;

internal sealed unsafe partial class Win32Window
{
    private nint smallIcon;
    private nint largeIcon;
    private static readonly Lazy<ImageSource?> applicationIcon = new(ReadApplicationIcon);
    public ImageSource? ApplicationIcon => applicationIcon.Value;

    private static ImageSource? ReadApplicationIcon()
    {
        var module = Win32Platform.Instance;
        var group = Resource(module, 32512, 14);
        if (group is null || group.Length < 6) return null;
        var count = BitConverter.ToUInt16(group, 4);
        if (count == 0 || group.Length < 6 + count * 14) return null;
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);
        writer.Write((ushort)0); writer.Write((ushort)1); writer.Write(count);
        var data = new List<byte[]>();
        var offset = 6 + count * 16;
        for (var index = 0; index < count; index++)
        {
            var entry = 6 + index * 14;
            var image = Resource(module, BitConverter.ToUInt16(group, entry + 12), 3);
            if (image is null) return null;
            writer.Write(group, entry, 8);
            writer.Write(image.Length);
            writer.Write(offset);
            offset = checked(offset + image.Length);
            data.Add(image);
        }
        foreach (var image in data) writer.Write(image);
        return ImageSource.FromBytes(stream.ToArray());
    }

    private static byte[]? Resource(nint module, nint name, nint type)
    {
        var resource = FindResourceW(module, name, type);
        if (resource == 0) return null;
        var size = SizeofResource(module, resource);
        var address = LockResource(LoadResource(module, resource));
        if (address == 0 || size == 0 || size > 16 * 1024 * 1024) return null;
        var bytes = new byte[size];
        Marshal.Copy(address, bytes, 0, bytes.Length);
        return bytes;
    }

    [LibraryImport("kernel32.dll")] private static partial nint FindResourceW(nint module, nint name, nint type);
    [LibraryImport("kernel32.dll")] private static partial uint SizeofResource(nint module, nint resource);
    [LibraryImport("kernel32.dll")] private static partial nint LoadResource(nint module, nint resource);
    [LibraryImport("kernel32.dll")] private static partial nint LockResource(nint resource);

    public void SetIcon(IReadOnlyList<RenderedImage>? images)
    {
        if (hwnd == 0 || IsClosed) return;
        if (images is { Count: 0 }) throw new ArgumentException("Supply icon images or null to restore the application icon.", nameof(images));
        if (images is not null)
            foreach (var image in images)
                if (image.Width <= 0 || image.Height <= 0 || image.Pixels.LongLength != (long)image.Width * image.Height * 4)
                    throw new ArgumentException("Icon images must contain packed BGRA pixels with positive dimensions.", nameof(images));

        nint small = 0, large = 0;
        try
        {
            if (images is not null)
            {
                small = CreateIcon(SelectImage(images, GetSystemMetricsForDpi(49, dpi)));
                large = CreateIcon(SelectImage(images, GetSystemMetricsForDpi(11, dpi)));
            }
            SendMessageW(hwnd, 0x0080 /* WM_SETICON */, 0, small);
            SendMessageW(hwnd, 0x0080 /* WM_SETICON */, 1, large);
            ReleaseIcons();
            smallIcon = small;
            largeIcon = large;
            small = large = 0;
        }
        finally
        {
            if (small != 0) DestroyIcon(small);
            if (large != 0) DestroyIcon(large);
        }
    }

    private static RenderedImage SelectImage(IReadOnlyList<RenderedImage> images, int size) =>
        images.OrderBy(image => Math.Abs(image.Width - size) + Math.Abs(image.Height - size))
            .ThenByDescending(image => image.Width).First();

    internal static nint CreateIcon(RenderedImage image)
    {
        var header = new BITMAPINFOHEADER
        {
            biSize = (uint)sizeof(BITMAPINFOHEADER), biWidth = image.Width, biHeight = -image.Height,
            biPlanes = 1, biBitCount = 32,
        };
        var color = CreateDIBSection(0, &header, 0, out var pixels, 0, 0);
        if (color == 0) throw new Win32Exception(Marshal.GetLastPInvokeError(), "Could not create the window icon bitmap.");
        nint mask = 0;
        try
        {
            Marshal.Copy(image.Pixels, 0, pixels, image.Pixels.Length);
            var maskBytes = new byte[checked(((image.Width + 15) / 16) * 2 * image.Height)];
            fixed (byte* bits = maskBytes) mask = CreateBitmap(image.Width, image.Height, 1, 1, bits);
            if (mask == 0) throw new Win32Exception(Marshal.GetLastPInvokeError(), "Could not create the window icon mask.");
            var info = new ICONINFO { IsIcon = 1, Mask = mask, Color = color };
            var icon = CreateIconIndirect(ref info);
            return icon != 0 ? icon : throw new Win32Exception(Marshal.GetLastPInvokeError(), "Could not create the window icon.");
        }
        finally
        {
            if (mask != 0) DeleteObject(mask);
            DeleteObject(color);
        }
    }

    private void ReleaseIcons()
    {
        if (smallIcon != 0) DestroyIcon(smallIcon);
        if (largeIcon != 0) DestroyIcon(largeIcon);
        smallIcon = largeIcon = 0;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct ICONINFO
    {
        public int IsIcon;
        public uint HotspotX, HotspotY;
        public nint Mask, Color;
    }

    [LibraryImport("user32.dll")] private static partial nint SendMessageW(nint window, uint message, nint wParam, nint lParam);
    [LibraryImport("user32.dll", SetLastError = true)] private static partial nint CreateIconIndirect(ref ICONINFO info);
    [LibraryImport("user32.dll")][return: MarshalAs(UnmanagedType.Bool)] private static partial bool DestroyIcon(nint icon);
    [LibraryImport("gdi32.dll", SetLastError = true)] private static partial nint CreateDIBSection(nint dc, BITMAPINFOHEADER* header, uint usage, out nint bits, nint section, uint offset);
    [LibraryImport("gdi32.dll", SetLastError = true)] private static partial nint CreateBitmap(int width, int height, uint planes, uint bitsPerPixel, void* bits);
    [LibraryImport("gdi32.dll")][return: MarshalAs(UnmanagedType.Bool)] private static partial bool DeleteObject(nint bitmap);
}
