using System.Runtime.InteropServices;
using System.Text;
using Nazor.Controls;
using Nazor.Graphics;
using Nazor.Platform;
using Nazor.Rendering;
using Nazor.Skia;

namespace Nazor.Application.Tests;
[Collection("Windows")]
[System.Runtime.Versioning.SupportedOSPlatform("windows")]
public sealed partial class WindowIconPropertyTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Root_icon_updates_independently_and_null_restores_the_application_icon(bool custom)
    {
        using var first = new WindowHarness(custom ? typeof(CustomIconPropertyWindow) : typeof(IconPropertyWindow));
        using var second = new WindowHarness(typeof(IconPropertyWindow));
        first.WaitUntil(w => Icon(w, 0) != 0 && Icon(w, 1) != 0, "initial window icons");
        second.WaitUntil(w => Icon(w, 0) != 0, "second window icon");
        var other = second.Read(w => Icon(w, 0));
        var initial = first.Read(w => Icon(w, 0));
        first.Read(w =>
        {
            Assert.True(w.Capabilities.CanSetWindowIcon);
            Assert.Same(IconPropertyWindow.Red, ((IWindow)w).Icon);
            Assert.Equal(new Color(255, 0, 0, 255), ReadIcon(Icon(w, 0)).GetPixel(0, 0));
            ((IconPropertyWindow)w.Root.Component).SetIcon(IconPropertyWindow.Blue);
            return true;
        });
        first.WaitUntil(w => Icon(w, 0) != initial, "changed icon");
        Assert.Equal(new Color(0, 0, 255, 255), first.Read(w => ReadIcon(Icon(w, 1)).GetPixel(0, 0)));
        Assert.Equal(other, second.Read(w => Icon(w, 0)));
        first.Read(w =>
        {
            ((IconPropertyWindow)w.Root.Component).SetIcon(null);
            return true;
        });
        Assert.Equal(0, first.Read(w => Icon(w, 0)));
        Assert.Equal(0, first.Read(w => Icon(w, 1)));
        Assert.NotEqual(0, first.Read(w => GetClassLongPtrW(w.PlatformWindow.Handle, -14)));
        Assert.Empty(first.Read(w => w.Exceptions.ToArray()));
    }

    [Fact]
    public void IWindow_setter_survives_unrelated_renders_and_accepts_png_and_ico_resources()
    {
        using var harness = new WindowHarness(typeof(IconPropertyWindow));
        harness.WaitUntil(w => Icon(w, 0) != 0, "initial icon");
        using var renderer = new SkiaRenderer();
        var png = renderer.EncodePng(new RenderedImage(1, 1, [0, 255, 0, 255]));
        var source = ImageSource.FromBytes(png);
        var previous = harness.Read(w => Icon(w, 0));
        harness.Read(w =>
        {
            ((IWindow)w).Icon = source;
            return true;
        });
        harness.WaitUntil(w => Icon(w, 0) != previous, "PNG icon");
        harness.Read(w =>
        {
            var applied = Icon(w, 0);
            ((IconPropertyWindow)w.Root.Component).ChangeTitle();
            Assert.Same(source, w.Icon);
            Assert.Equal(applied, Icon(w, 0));
            Assert.Equal(new Color(0, 255, 0, 255), ReadIcon(applied).GetPixel(0, 0));
            return true;
        });
        previous = harness.Read(w => Icon(w, 0));
        harness.Read(w =>
        {
            w.Icon = ImageSource.FromResource(typeof(WindowIconPropertyTests).Assembly, "Nazor.Tests.Icon.ico");
            return true;
        });
        harness.WaitUntil(w => Icon(w, 0) != previous, "embedded ICO icon");
        Assert.Empty(harness.Read(w => w.Exceptions.ToArray()));
    }

    [Fact]
    public void File_icon_uses_dpi_variants_and_native_icons_have_the_requested_pixel_sizes()
    {
        var directory = Path.Combine(Path.GetTempPath(), "nazor-icons-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            var path = Path.Combine(directory, "icon.svg");
            File.WriteAllBytes(path, IconPropertyWindow.RedData);
            File.WriteAllBytes(Path.Combine(directory, "icon@2x.svg"), IconPropertyWindow.BlueData);
            using var harness = new WindowHarness(typeof(IconPropertyWindow));
            var initial = harness.Read(w => Icon(w, 0));
            harness.Read(w =>
            {
                w.Icon = path;
                return true;
            });
            harness.WaitUntil(w => Icon(w, 0) != 0 && Icon(w, 0) != initial, "file icon");
            harness.Read(w =>
            {
                var dpi = GetDpiForWindow(w.PlatformWindow.Handle);
                Assert.Equal(GetSystemMetricsForDpi(49, dpi), ReadIcon(Icon(w, 0)).Width);
                Assert.Equal(GetSystemMetricsForDpi(11, dpi), ReadIcon(Icon(w, 1)).Width);
                var expected = w.PlatformWindow.Scale <= 1 ? new Color(255, 0, 0, 255) : new Color(0, 0, 255, 255);
                Assert.Equal(expected, ReadIcon(Icon(w, 0)).GetPixel(0, 0));
                return true;
            });
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void Transparent_non_square_images_are_centered_without_stretching()
    {
        using var harness = new WindowHarness(typeof(IconPropertyWindow));
        harness.WaitUntil(w => Icon(w, 0) != 0, "initial icon");
        using var renderer = new SkiaRenderer();
        var png = renderer.EncodePng(new RenderedImage(2, 1, [0, 0, 128, 128, 0, 0, 128, 128]));
        var previous = harness.Read(w => Icon(w, 0));
        harness.Read(w =>
        {
            w.Icon = ImageSource.FromBytes(png);
            return true;
        });
        harness.WaitUntil(w => Icon(w, 0) != previous, "transparent icon");
        var bitmap = harness.Read(w => ReadIcon(Icon(w, 0)));
        Assert.Equal(0, bitmap.GetPixel(bitmap.Width / 2, 0).A);
        Assert.Equal(new Color(255, 0, 0, 128), bitmap.GetPixel(bitmap.Width / 2, bitmap.Height / 2));
    }

    [Fact]
    public void Invalid_images_report_an_error_and_preserve_the_last_applied_icon()
    {
        using var harness = new WindowHarness(typeof(IconPropertyWindow));
        harness.WaitUntil(w => Icon(w, 0) != 0, "initial icon");
        var original = harness.Read(w => Icon(w, 0));
        harness.Read(w =>
        {
            w.Icon = ImageSource.FromBytes([1, 2, 3]);
            return true;
        });
        harness.WaitUntil(w => w.Exceptions.Count != 0, "invalid image error");
        Assert.IsType<InvalidDataException>(Assert.Single(harness.Read(w => w.Exceptions.ToArray())));
        Assert.Equal(original, harness.Read(w => Icon(w, 0)));
        harness.Read(w =>
        {
            w.Icon = null;
            return true;
        });
        Assert.Equal(0, harness.Read(w => Icon(w, 0)));
    }

    [Fact]
    public void Replacing_and_closing_windows_releases_native_icons()
    {
        using var harness = new WindowHarness(typeof(IconPropertyWindow));
        harness.WaitUntil(w => Icon(w, 0) != 0, "initial icon");
        nint removed = 0;
        for (var i = 0; i < 8; i++)
        {
            removed = harness.Read(w => Icon(w, 0));
            harness.Read(w =>
            {
                w.Icon = ImageSource.FromBytes(IconPropertyWindow.BlueData);
                return true;
            });
            harness.WaitUntil(w => Icon(w, 0) != removed, "replacement icon");
            Assert.False(GetIconInfo(removed, out _));
        }

        var last = harness.Read(w => Icon(w, 0));
        harness.Close();
        Assert.False(GetIconInfo(last, out _));
    }

    private static nint Icon(NazorWindow window, int size) => SendMessageW(window.PlatformWindow.Handle, 0x007F, size, 0);
    private static unsafe RenderedImage ReadIcon(nint icon)
    {
        Assert.True(GetIconInfo(icon, out var info));
        var dc = GetDC(0);
        try
        {
            Assert.NotEqual(0, GetObjectW(info.Color, Marshal.SizeOf<BITMAP>(), out var bitmap));
            var pixels = new byte[bitmap.Width * bitmap.Height * 4];
            var header = new Nazor.Platform.Windows.Win32.BITMAPINFOHEADER
            {
                biSize = (uint)Marshal.SizeOf<Nazor.Platform.Windows.Win32.BITMAPINFOHEADER>(),
                biWidth = bitmap.Width,
                biHeight = -bitmap.Height,
                biPlanes = 1,
                biBitCount = 32,
            };
            fixed (byte* data = pixels)
                Assert.Equal(bitmap.Height, GetDIBits(dc, info.Color, 0, (uint)bitmap.Height, data, &header, 0));
            return new RenderedImage(bitmap.Width, bitmap.Height, pixels);
        }
        finally
        {
            ReleaseDC(0, dc);
            DeleteObject(info.Color);
            DeleteObject(info.Mask);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct ICONINFO
    {
        public int IsIcon;
        public uint X, Y;
        public nint Mask, Color;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct BITMAP
    {
        public int Type, Width, Height, WidthBytes;
        public ushort Planes, BitsPerPixel;
        public nint Bits;
    }

    [LibraryImport("user32.dll")]
    private static partial nint SendMessageW(nint window, uint message, nint wParam, nint lParam);
    [LibraryImport("user32.dll")]
    private static partial nint GetClassLongPtrW(nint window, int index);
    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool GetIconInfo(nint icon, out ICONINFO info);
    [LibraryImport("user32.dll")]
    private static partial uint GetDpiForWindow(nint window);
    [LibraryImport("user32.dll")]
    private static partial int GetSystemMetricsForDpi(int index, uint dpi);
    [LibraryImport("user32.dll")]
    private static partial nint GetDC(nint window);
    [LibraryImport("user32.dll")]
    private static partial int ReleaseDC(nint window, nint dc);
    [LibraryImport("gdi32.dll")]
    private static partial int GetObjectW(nint bitmap, int size, out BITMAP data);
    [LibraryImport("gdi32.dll")]
    private static unsafe partial int GetDIBits(nint dc, nint bitmap, uint start, uint lines, void* bits, Nazor.Platform.Windows.Win32.BITMAPINFOHEADER* info, uint usage);
    [LibraryImport("gdi32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool DeleteObject(nint bitmap);
}

public class IconPropertyWindow : WindowComponentBase
{
    internal static readonly byte[] RedData = Encoding.UTF8.GetBytes("<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"32\" height=\"32\"><rect width=\"32\" height=\"32\" fill=\"red\"/></svg>");
    internal static readonly byte[] BlueData = Encoding.UTF8.GetBytes("<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"32\" height=\"32\"><rect width=\"32\" height=\"32\" fill=\"blue\"/></svg>");
    internal static readonly ImageSource Red = ImageSource.FromBytes(RedData);
    internal static readonly ImageSource Blue = ImageSource.FromBytes(BlueData);
    private ImageSource? source = Red;
    private string title = "Icon property";
    public void SetIcon(ImageSource? value)
    {
        source = value;
        Window.Icon = value;
        StateHasChanged();
    }

    public void ChangeTitle()
    {
        title += "!";
        Window.Title = title;
        StateHasChanged();
    }

    protected override void BuildWindowContent(RenderTreeBuilder builder)
    {
        if (this is CustomIconPropertyWindow)
            builder.AddContent(4, (RenderFragment)(b =>
            {
                b.OpenElement(0, "TitleBar");
                b.CloseElement();
            }));
    }

    protected override void OnInitialized()
    {
        Window.Icon = source;
        Window.Title = title;
        Window.Decorations = WindowDecorationPolicy.Require(this is CustomIconPropertyWindow ? DecorationTestExtensions.ClientTitleBar() : WindowDecorations.Native());
    }
}

public sealed class CustomIconPropertyWindow : IconPropertyWindow;
