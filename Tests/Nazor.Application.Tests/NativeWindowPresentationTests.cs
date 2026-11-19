using System.Runtime.InteropServices;
using Nazor.Controls;
using Nazor.Graphics;
using Nazor.Platform;
using Nazor.Platform.Windows;
using Nazor.Rendering;
using Nazor.Testing;
using static Nazor.Platform.Windows.Win32;

namespace Nazor.Application.Tests;

[Collection("Windows")]
[System.Runtime.Versioning.SupportedOSPlatform("windows")]
public sealed unsafe partial class NativeWindowPresentationTests
{
    [DllImport("user32.dll")]
    private static extern nint WindowFromPoint(POINT point);

    [Fact]
    public void Nazor_frame_outer_resize_tracks_live_styles_and_operation_permissions()
    {
        NativeDesktop.RequireUnlocked();
        using var harness = new WindowHarness(typeof(WindowPresentationTests.CanvasWindow), new() { Rendering = RenderingMode.Cpu });
        harness.Read(window =>
        {
            window.Width = 320;
            window.Height = 200;
            window.IsAlwaysOnTop = true;
            window.IsResizable = true;
            window.Style = "-window-frame-resize-inset: 0; -window-frame-resize-outset: 8px; -window-frame-border-radius: 0; -window-frame-box-shadow: none";
            Assert.True(SetWindowPos(harness.Handle, 0, 100, 100, 0, 0, SWP_NOSIZE | SWP_NOZORDER | SWP_NOACTIVATE));
            return true;
        });
        harness.WaitUntil(window => window.FrameCount > 1, "outer resize frame");
        nint Outset(int distance, bool enabled) => harness.Read(window =>
        {
            var point = new POINT { X = -(int)(distance * window.Scene.Scale), Y = (int)(50 * window.Scene.Scale) };
            Assert.True(ClientToScreen(harness.Handle, ref point));
            var target = WindowFromPoint(point);
            if (!enabled) { Assert.NotEqual(10, (int)SendMessageW(target, WM_NCHITTEST, 0, (nint)(((long)point.Y << 16) | (ushort)point.X))); return target; }
            Assert.NotEqual(harness.Handle, target);
            Assert.NotEqual(0, target);
            Assert.Equal((nint)10, SendMessageW(target, WM_NCHITTEST, 0, (nint)(((long)point.Y << 16) | (ushort)point.X)));
            return target;
        });
        var companion = Outset(4, true);
        var before = harness.Read(window =>
        {
            window.Style = "-window-frame-resize-inset: 0; -window-frame-resize-outset: 16px; -window-frame-border-radius: 24px; -window-frame-box-shadow: 0 2px 6px #00000044";
            return window.FrameCount;
        });
        harness.WaitUntil(window => window.FrameCount > before, "updated outer resize geometry");
        Assert.Equal(companion, Outset(12, true));
        harness.Read(window => { window.IsResizable = false; return true; });
        harness.WaitUntil(_ => !IsWindowVisible(companion), "disabled outer resize");
        Outset(4, false);
        harness.Read(window => { window.IsResizable = true; return true; });
        harness.WaitUntil(_ => IsWindowVisible(companion), "restored outer resize");
        Assert.Equal(companion, Outset(12, true));
        harness.Read(window => { window.ApplyDecorations(WindowDecorations.Native()); return true; });
        Assert.False(IsWindowVisible(companion));
        harness.Read(window => { window.ApplyDecorations(WindowDecorations.Nazor(WindowTitleBarContent.None, WindowCaptionButtons.None)); return true; });
        harness.WaitUntil(_ => IsWindowVisible(companion), "restored Nazor frame resize");
        harness.Read(window => { window.Style = "-window-frame-resize-outset: 0"; return true; });
        harness.WaitUntil(_ => !IsWindowVisible(companion), "zero outer resize");
        Assert.Empty(harness.Read(window => window.Exceptions.ToArray()));
    }

    [Theory]
    [InlineData(1.25f)]
    [InlineData(10f)]
    [InlineData(24f)]
    [InlineData(24.5f)]
    [InlineData(64.75f)]
    public void Rounded_native_region_preserves_the_antialiased_client_border(float radius)
    {
        using var harness = new WindowHarness(typeof(WindowPresentationTests.CanvasWindow), new() { Rendering = RenderingMode.Cpu });
        harness.Read(window =>
        {
            window.Style = FormattableString.Invariant($"-window-frame-border-radius: {radius}px; -window-frame-border-width: 2px; -window-frame-border-style: solid; -window-frame-border-color: red; -window-frame-box-shadow: none");
            return true;
        });
        harness.WaitUntil(window => window.PlatformWindow.ClientFrameRadius.TopLeft.X == radius, "rounded frame");
        harness.Read(window =>
        {
            var size = window.PlatformWindow.ClientSize;
            var border = Assert.IsType<DrawBorder>(((IWindowHost)window).GetClientFrameBorder(size));
            using var renderer = new Skia.SkiaRenderer();
            var reference = renderer.RenderToImage(new DisplayList(size, [border]), window.Scene.Scale, Color.Transparent);
            var region = CreateRectRgn(0, 0, 0, 0);
            try
            {
                Assert.True(GetWindowRgn(harness.Handle, region) > 0);
                var lost = new List<(int X, int Y)>();
                for (var y = 0; y < reference.Height; y++)
                for (var x = 0; x < reference.Width; x++)
                    if (reference.GetPixel(x, y).A >= 64 && !PtInRegion(region, x, y)) lost.Add((x, y));
                Assert.True(lost.Count == 0, $"The native region clips {lost.Count} border pixels: {string.Join(", ", lost.Take(8))}");
            }
            finally { DeleteObject(region); }
            return true;
        });
    }

    [LibraryImport("user32.dll")]
    private static partial nint SendMessageW(nint hwnd, uint message, nint wParam, nint lParam);

    [LibraryImport("user32.dll")] private static partial int GetWindowRgn(nint hwnd, nint region);
    [LibraryImport("gdi32.dll")] private static partial nint CreateRectRgn(int left, int top, int right, int bottom);
    [LibraryImport("gdi32.dll")][return: MarshalAs(UnmanagedType.Bool)] private static partial bool PtInRegion(nint region, int x, int y);
    [LibraryImport("gdi32.dll")][return: MarshalAs(UnmanagedType.Bool)] private static partial bool DeleteObject(nint obj);

    [Fact]
    public void Transparent_background_composes_with_the_desktop_and_keeps_native_caption_buttons()
    {
        Assert.SkipUnless(OperatingSystem.IsWindowsVersionAtLeast(10, 0, 22000), "Integrated caption buttons require AppWindow.");
        using var behind = new WindowHarness(typeof(WindowPresentationTests.EmptyWindow), new() { Rendering = RenderingMode.Cpu });
        behind.Read(window =>
        {
            window.IsAlwaysOnTop = true;
            window.Style = "background: #00ff00";
            window.Width = 600;
            window.Height = 400;
            Assert.True(SetWindowPos(behind.Handle, 0, 20, 20, 0, 0, SWP_NOSIZE | SWP_NOACTIVATE));
            return true;
        });
        using var front = new WindowHarness(typeof(TransparentCaptionWindow), new() { ColorScheme = Style.ColorScheme.Dark });
        var hasForeground = front.Read(window =>
        {
            window.IsAlwaysOnTop = true;
            Assert.True(window.Capabilities.CanUseTransparentBackground);
            Assert.Equal(BackgroundMode.Transparent, window.BackgroundMode);
            window.Backdrop = CommonBackdrop.Blur;
            Assert.False(window.AppliedBackdrop.IsActive);
            Assert.Throws<InvalidOperationException>(() => window.BackgroundMode = BackgroundMode.Default);
            Assert.True(SetWindowPos(front.Handle, 0, 70, 70, 0, 0, SWP_NOSIZE | SWP_NOACTIVATE));
            Assert.True(SetWindowPos(behind.Handle, front.Handle, 20, 20, 0, 0, SWP_NOSIZE | SWP_NOACTIVATE));
            window.Activate();
            return GetForegroundWindow() == front.Handle;
        });
        Assert.SkipUnless(hasForeground, "Desktop composition capture requires foreground permission.");

        void AssertPresented()
        {
            var buttons = front.Read(window =>
            {
                var scale = window.PlatformWindow.Scale;
                var width = (int)MathF.Round(window.SystemButtonsSize.Width * scale);
                Assert.True(GetClientRect(front.Handle, out var client));
                var maximize = new POINT { X = client.right - width / 2, Y = (int)(window.SystemButtonsSize.Height * scale / 2) };
                Assert.True(ClientToScreen(front.Handle, ref maximize));
                var coordinates = (nint)(((long)maximize.Y << 16) | (ushort)maximize.X);
                Assert.Equal((nint)HTMAXBUTTON, SendMessageW(front.Handle, WM_NCHITTEST, 0, coordinates));
                return width;
            });
            Assert.True(buttons > 0, "Expected the native caption buttons.");
            (Graphics.Color Clear, Graphics.Color Translucent, Graphics.Color Solid, Graphics.Color Caption, Graphics.Color LeftCaption, bool Glyph) sampled = default;
            var matched = SpinWait.SpinUntil(() => front.Read(_ =>
            {
                var image = ScreenCapture.CaptureDesktopWindow(front.Handle);
                var clear = image.GetPixel(image.Width / 6, image.Height / 2);
                var translucent = image.GetPixel(image.Width / 2, image.Height / 2);
                var solid = image.GetPixel(image.Width * 5 / 6, image.Height / 2);
                var caption = image.GetPixel(image.Width - buttons + 4, 4);
                var glyph = Enumerable.Range(image.Width - buttons + 8, buttons / 4)
                    .Any(x => Enumerable.Range(9, 18).Any(y => image.GetPixel(x, y).R >= 200));
                sampled = (clear, translucent, solid, caption, image.GetPixel(10, 4), glyph);
                return clear is { R: 0, G: 255, B: 0 } &&
                    translucent.R is >= 115 and <= 145 && translucent.G is >= 115 and <= 145 && translucent.B == 0 &&
                    solid is { R: 255, G: 0, B: 0 } &&
                    caption.R is >= 8 and <= 16 && caption.G is >= 155 and <= 170 && caption.B is >= 54 and <= 66 && glyph;
            }), TimeSpan.FromSeconds(3));
            Assert.True(matched, $"Expected desktop alpha blending and native glyphs over the custom caption. Samples: {sampled}");
        }

        AssertPresented();
        front.Read(window => { window.Width = 350; window.Height = 240; return true; });
        AssertPresented();
    }

    public sealed class TransparentCaptionWindow : WindowComponentBase
    {
        protected override void OnInitialized()
        {
            Window.BackgroundMode = BackgroundMode.Transparent;
            Window.Decorations = WindowDecorationPolicy.Require(DecorationTestExtensions.ClientTitleBar(WindowTitleBarContent.User));
            Window.Width = 300;
            Window.Height = 200;
        }

        protected override void BuildWindowContent(RenderTreeBuilder builder)
        {
            builder.OpenComponent<UserTitleBar>(0, typeof(TransparentCaptionWindow), this);
            builder.AddComponentParameter(1, nameof(TitleBar.Style), "background: #17487880");
            builder.CloseComponent();
            builder.OpenElement(2, "Panel");
            builder.AddAttribute(3, "style", "flex: 1; background: linear-gradient(to right, transparent 0%, transparent 33%, #ff000080 33%, #ff000080 66%, red 66%, red 100%)");
            builder.CloseElement();
        }
    }

    [Fact]
    public void Borderless_has_no_native_frame_hit_regions_or_resize_grip_and_can_resize_from_code()
    {
        using var harness = new WindowHarness(typeof(WindowPresentationTests.CanvasWindow));
        harness.Read(window =>
        {
            var hwnd = window.PlatformWindow.Handle;
            Assert.Equal(0, (long)GetWindowLongPtrW(hwnd, -16) & (WS_CAPTION | 0x40000u));
            Assert.True(GetWindowRect(hwnd, out var outer));
            Assert.True(GetClientRect(hwnd, out var client));
            Assert.Equal(client.right, outer.right - outer.left);
            Assert.Equal(client.bottom, outer.bottom - outer.top);
            foreach (var (x, y) in new[] { (0, 0), (client.right - 1, 0), (0, client.bottom - 1), (client.right - 1, client.bottom - 1), (client.right / 2, 1) })
            {
                var point = new POINT { X = x, Y = y };
                Assert.True(ClientToScreen(hwnd, ref point));
                var packed = (nint)(((long)point.Y << 16) | (ushort)point.X);
                Assert.Equal((nint)HTCLIENT, SendMessageW(hwnd, WM_NCHITTEST, 0, packed));
            }
            if (OperatingSystem.IsWindowsVersionAtLeast(10, 0, 22000))
            {
                var corner = 0;
                Assert.True(DwmGetWindowAttribute(hwnd, 33, &corner, sizeof(int)) >= 0);
                Assert.Equal(1, corner);
            }
            window.Style = "-window-resize-grip: auto; -windows-title-bar-height: tall; -windows-title-bar-button-background-color: red";
            window.RequestFrame();
            RedrawWindow(hwnd, null, 0, 0x101);
            Assert.Empty(window.Root.Elements("TitleBar"));
            Assert.Equal(default, window.SystemButtonsMetrics);
            Assert.False(window.Capabilities.CanResize);
            window.IsResizable = false;
            window.IsResizable = true;
            Assert.True(window.Capabilities.CanResize);
            window.Width = 321;
            window.Height = 173;
            Assert.InRange(window.Width, 320, 322);
            Assert.InRange(window.Height, 172, 174);
            Assert.Throws<InvalidOperationException>(() => window.Decorations = WindowDecorationPolicy.Default);
            Assert.Empty(window.Exceptions);
            return true;
        });
    }

    [Fact]
    public void Borderless_frame_applies_explicit_corners_and_tracks_its_shadow()
    {
        using var harness = new WindowHarness(typeof(WindowPresentationTests.CanvasWindow));
        harness.Read(window =>
        {
            var hwnd = window.PlatformWindow.Handle;
            var region = CreateRectRgn(0, 0, 0, 0);
            Assert.NotEqual(0, region);
            try
            {
                Assert.Equal(0, GetWindowRgn(hwnd, region));
                window.Style = "-window-frame-border-radius: 24px 0px 0px 0px; -window-frame-box-shadow: 0 2px 8px #00000088";
                window.RequestFrame();
                Assert.True(RedrawWindow(hwnd, null, 0, 0x101));

                Assert.True(GetClientRect(hwnd, out var client));
                Assert.NotEqual(0, GetWindowRgn(hwnd, region));
                Assert.False(PtInRegion(region, 1, 1));
                Assert.True(PtInRegion(region, client.right - 2, 1));
                Assert.False(window.Capabilities.CanResize);

                var shadow = GetWindow(hwnd, 2);
                Assert.True(IsWindowVisible(shadow));
                const long layeredAndTransparent = 0x80020;
                Assert.Equal(layeredAndTransparent, (long)GetWindowLongPtrW(shadow, -20) & layeredAndTransparent);
                Assert.True(GetWindowRect(shadow, out var before));
                Assert.True(GetWindowRect(hwnd, out var bounds));
                Assert.True(SetWindowPos(hwnd, 0, bounds.left + 12, bounds.top + 8, 0, 0, SWP_NOSIZE | SWP_NOZORDER | SWP_NOACTIVATE));
                Assert.True(GetWindowRect(shadow, out var after));
                Assert.Equal(12, after.left - before.left);
                Assert.Equal(8, after.top - before.top);

                window.Style = null;
                window.RequestFrame();
                Assert.True(RedrawWindow(hwnd, null, 0, 0x101));
                Assert.Equal(0, GetWindowRgn(hwnd, region));
                Assert.False(IsWindowVisible(shadow));
            }
            finally { DeleteObject(region); }
            Assert.Empty(window.Exceptions);
            return true;
        });
    }

    [Theory]
    [InlineData(false, false, false)]
    [InlineData(true, false, false)]
    [InlineData(false, true, false)]
    [InlineData(true, true, false)]
    [InlineData(false, false, true)]
    [InlineData(true, false, true)]
    public void Topmost_and_taskbar_styles_apply_initially_and_survive_fullscreen_transitions(bool initial, bool custom, bool borderless)
    {
        using var harness = new WindowHarness(borderless ? typeof(WindowPresentationTests.CanvasWindow) : custom ? typeof(SwitchableWindow) : typeof(WindowPresentationTests.EmptyWindow), new()
        { IsAlwaysOnTop = initial, IsVisibleInTaskbar = !initial });
        using var foregroundWindow = new WindowHarness(typeof(WindowPresentationTests.EmptyWindow));
        var hasForegroundPermission = foregroundWindow.Read(window =>
        {
            window.Activate();
            return GetForegroundWindow() == window.PlatformWindow.Handle;
        });
        Assert.SkipUnless(hasForegroundPermission, "Native z-order verification requires foreground permission; a second window keeps the subject in the background.");
        var changed = 0;
        var original = harness.Read(window =>
        {
            window.CapabilitiesChanged += () => changed++;
            return (Foreground: GetForegroundWindow(), Size: window.PlatformWindow.ClientSize);
        });
        void WaitStyles(bool top, bool taskbar) => harness.WaitUntil(window =>
        {
            var style = (long)GetWindowLongPtrW(window.PlatformWindow.Handle, -20);
            return top == ((style & WS_EX_TOPMOST) != 0) && !taskbar == ((style & WS_EX_TOOLWINDOW) != 0);
        }, $"topmost={top}, taskbar={taskbar}");
        WaitStyles(initial, !initial);
        harness.Read(window =>
        {
            window.IsAlwaysOnTop = !initial;
            window.IsVisibleInTaskbar = initial;
            return true;
        });
        WaitStyles(!initial, initial);
        harness.Read(window =>
        {
            Assert.Equal(original.Size, window.PlatformWindow.ClientSize);
            Assert.Equal(original.Foreground, GetForegroundWindow());
            Assert.Equal(0, changed);
            window.EnterFullScreen();
            window.IsAlwaysOnTop = initial;
            window.IsVisibleInTaskbar = !initial;
            window.ExitFullScreen();
            return true;
        });
        WaitStyles(initial, !initial);
        harness.Read(window =>
        {
            window.IsResizable = false;
            window.IsResizable = true;
            if (!borderless)
            {
                window.ApplyDecorations((!custom) ? DecorationTestExtensions.ClientTitleBar() : WindowDecorations.Native());
                window.ApplyDecorations((custom) ? DecorationTestExtensions.ClientTitleBar() : WindowDecorations.Native());
            }
            else window.IsResizable = false;
            return true;
        });
        WaitStyles(initial, !initial);
        harness.Read(window =>
        {
            window.Hide();
            window.IsVisibleInTaskbar = initial;
            Assert.False(IsWindowVisible(window.PlatformWindow.Handle));
            window.Show(false);
            window.Maximize();
            window.IsVisibleInTaskbar = !initial;
            Assert.Equal(WindowState.Maximized, window.State);
            window.Minimize();
            window.IsVisibleInTaskbar = initial;
            Assert.Equal(WindowState.Minimized, window.State);
            window.Restore();
            if (borderless)
            {
                Assert.Equal(0, (long)GetWindowLongPtrW(window.PlatformWindow.Handle, -16) & (WS_CAPTION | 0x40000u));
                Assert.Equal(default, window.SystemButtonsMetrics);
            }
            Assert.Empty(window.Exceptions);
            return true;
        });
        WaitStyles(initial, initial);
    }
}
