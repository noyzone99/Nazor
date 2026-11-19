using System.Runtime.InteropServices;
using Nazor.Controls;
using Nazor.Graphics;
using Nazor.Rendering;
using Nazor.Testing;
using static Nazor.Platform.Windows.Win32;

namespace Nazor.Application.Tests;
public sealed class NativeAppearancePage : WindowComponentBase
{
    private BackdropSelection backdrop;
    private bool grip = true;
    private string title = "Native appearance";
    public void SetBackdrop(BackdropSelection value)
    {
        backdrop = value;
        Window.Backdrop = value;
        StateHasChanged();
    }

    public void HideGrip()
    {
        grip = false;
        Window.Style = "-window-resize-grip: none";
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
    }

    protected override void OnInitialized()
    {
        Window.Title = title;
        Window.Backdrop = backdrop;
        Window.Style = "-window-resize-grip: " + (grip ? "auto" : "none");
    }
}

[Collection("Windows")]
[System.Runtime.Versioning.SupportedOSPlatform("windows")]
public sealed partial class NativeBackdropAndGripTests
{
    private const int HTBOTTOMRIGHT = 17;

    [LibraryImport("user32.dll")]
    private static partial nint SendMessageW(nint hwnd, uint message, nint wParam, nint lParam);

    private static nint GripHit(NazorWindow window)
    {
        var handle = window.PlatformWindow.Handle;
        Assert.True(GetClientRect(handle, out var client));
        var inset = (int)Math.Round(8 * window.PlatformWindow.Scale);
        return ClientHit(handle, client.right - inset, client.bottom - inset);
    }

    private static nint ClientHit(nint handle, int x, int y)
    {
        var point = new POINT { X = x, Y = y };
        Assert.True(ClientToScreen(handle, ref point));
        return SendMessageW(handle, WM_NCHITTEST, 0, (nint)(((long)point.Y << 16) | (ushort)point.X));
    }

    [Fact]
    public void Grip_cannot_enable_a_forbidden_resize_operation()
    {
        using var harness = new WindowHarness(typeof(NativeAppearancePage));
        harness.Read(window =>
        {
            var handle = window.PlatformWindow.Handle;
            bool CanGrab() => GripHit(window) == HTBOTTOMRIGHT;
            Assert.True(CanGrab());
            window.IsResizable = false;
            Assert.False(CanGrab());
            window.IsResizable = true;
            Assert.True(CanGrab());
            window.Maximize();
            Assert.False(CanGrab());
            window.Restore();
            Assert.True(CanGrab());
            window.EnterFullScreen();
            Assert.False(CanGrab());
            window.ExitFullScreen();
            Assert.True(CanGrab());
            ((NativeAppearancePage)window.Root.Component).HideGrip();
            window.RequestFrame();
            UpdateWindow(handle);
            Assert.False(CanGrab());
            Assert.True(window.Capabilities.CanResize);
            Assert.Empty(window.Exceptions);
            return true;
        });
    }

    [Fact]
    public void Embedded_NSS_colors_the_grip_on_a_custom_titlebar_window()
    {
        using var harness = new ApplicationHarness(() => ApplicationTestStartup.Run<ChromeWindow>(new() { Activate = false, Rendering = RenderingMode.Cpu }));
        var window = harness.Window<ChromeWindow>();
        ApplicationHarness.On(window, () =>
        {
            var result = Nazor.Nss.NssCompiler.Compile("grip", ":root { -window-resize-grip: auto; -window-resize-grip-color: #ff00ff; }");
            Assert.True(result.Success);
            window.Scene.Styles.Application = result.StyleSheet;
            var handle = window.PlatformWindow.Handle;
            window.RequestFrame();
            UpdateWindow(handle);
            Assert.Equal((nint)HTBOTTOMRIGHT, GripHit(window));
            var image = ScreenCapture.CapturePresented(handle);
            var size = (int)Math.Ceiling(16 * window.PlatformWindow.Scale);
            Assert.Contains(Enumerable.Range(image.Height - size, size), y =>
                Enumerable.Range(image.Width - size, size).Any(x =>
                {
                    var i = (y * image.Width + x) * 4;
                    return image.Pixels[i] > 160 && image.Pixels[i + 1] < 100 && image.Pixels[i + 2] > 160;
                }));

            window.Style = "-window-resize-grip-color: transparent";
            window.RequestFrame();
            UpdateWindow(handle);
            Assert.NotEqual((nint)HTBOTTOMRIGHT, GripHit(window));

            Assert.Empty(window.Exceptions);
            return true;
        });
    }

    [Fact]
    public void Resize_inset_expands_the_client_edge_only_with_custom_chrome()
    {
        using var harness = new WindowHarness(typeof(NativeAppearancePage));
        harness.Read(window =>
        {
            var handle = window.PlatformWindow.Handle;
            nint HitLeft()
            {
                Assert.True(GetClientRect(handle, out var client));
                return ClientHit(handle, (int)Math.Round(12 * window.PlatformWindow.Scale), client.bottom / 2);
            }

            window.Style = "-window-frame-resize-inset: 20px";
            window.RequestFrame();
            UpdateWindow(handle);
            Assert.Equal((nint)HTCLIENT, HitLeft());
            window.ApplyDecorations((true) ? DecorationTestExtensions.ClientTitleBar() : WindowDecorations.Native());
            window.RequestFrame();
            UpdateWindow(handle);
            Assert.Equal((nint)10, HitLeft());
            window.Style = "-window-frame-resize-inset: 0px";
            window.RequestFrame();
            UpdateWindow(handle);
            Assert.Equal((nint)HTCLIENT, HitLeft());
            Assert.Empty(window.Exceptions);
            return true;
        });
    }

    [Fact]
    public void Runtime_backdrop_changes_keep_the_theme_and_transparency_in_sync()
    {
        Assert.SkipUnless(OperatingSystem.IsWindowsVersionAtLeast(10, 0, 22621), "Native materials require Windows 11 22H2.");
        using var harness = new WindowHarness(typeof(NativeAppearancePage));
        harness.Read(window =>
        {
            var page = (NativeAppearancePage)window.Root.Component;
            page.SetBackdrop(CommonBackdrop.Blur);
            Assert.Equal("acrylic", window.AppliedBackdrop.Material);
            Assert.Equal(0, window.Scene.Background.A);
            page.ChangeTitle();
            Assert.Equal("acrylic", window.Root.Elements("Window").Single().Attributes["backdrop"]);
            page.SetBackdrop(CommonBackdrop.Solid);
            Assert.Equal("mica", window.AppliedBackdrop.Material);
            Assert.Equal(0, window.Scene.Background.A);
            page.SetBackdrop(CommonBackdrop.None);
            Assert.False(window.AppliedBackdrop.IsActive);
            Assert.Equal(255, window.Scene.Background.A);
            Assert.False(window.Root.Elements("Window").Single().Attributes.ContainsKey("backdrop"));
            Assert.Empty(window.Exceptions);
            return true;
        });
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public unsafe void Materials_cover_the_whole_window_and_removal_restores_a_solid_caption(bool custom, bool fluent)
    {
        Assert.SkipUnless(OperatingSystem.IsWindowsVersionAtLeast(10, 0, 22621), "Native materials require Windows 11 22H2.");
        using var harness = new WindowHarness(typeof(NativeAppearancePage), new()
        {
            Theme = fluent ? Nazor.Controls.StyleSheets.Basic : Nazor.Controls.StyleSheets.None,
        });
        harness.Read(window =>
        {
            window.ApplyDecorations((custom) ? DecorationTestExtensions.ClientTitleBar() : WindowDecorations.Native());
            foreach (var scheme in new[] { Nazor.Style.ColorScheme.Light, Nazor.Style.ColorScheme.Dark })
            {
                window.SetColorScheme(scheme);
                foreach (var (request, expectedType) in new (BackdropSelection, int)[]
                {
                    (WindowsBackdrop.Mica, 2), (CommonBackdrop.None, 1),
                    (WindowsBackdrop.Acrylic, 3), (CommonBackdrop.None, 1),
                    (WindowsBackdrop.MicaAlt, 4), (CommonBackdrop.None, 1),
                })
                {
                    window.Backdrop = request;
                    window.RequestFrame();
                    UpdateWindow(window.PlatformWindow.Handle);
                    var type = -1;
                    Assert.Equal(0, DwmGetWindowAttribute(window.PlatformWindow.Handle, DWMWA_SYSTEMBACKDROP_TYPE, &type, sizeof(int)));
                    Assert.Equal(expectedType, type);
                    Assert.Equal(expectedType != 1, window.AppliedBackdrop.IsActive);
                    Assert.Equal(expectedType == 1 ? (byte)255 : (byte)0, window.Scene.Background.A);
                    var style = window.Scene.GetStyle(window.Root.Elements("Window").Single());
                    Assert.Null(style.Get(Nazor.Style.StyleProperties.WindowsTitleBarBackgroundColor).Value);
                    Assert.Null(style.Get(Nazor.Style.StyleProperties.WindowsTitleBarInactiveBackgroundColor).Value);
                }
            }
            Assert.Empty(window.Exceptions);
            return true;
        });
    }
}
