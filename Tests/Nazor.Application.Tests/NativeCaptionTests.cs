using System.Runtime.InteropServices;
using Nazor.Testing;
using static Nazor.Platform.Windows.Win32;

namespace Nazor.Application.Tests;

public sealed partial class TitleBarNativeTests
{
    [Theory]
    [InlineData(false, true, 0)]
    [InlineData(true, false, 1)]
    public unsafe void Disabled_native_buttons_use_a_dimmed_glyph(bool minimize, bool maximize, int disabledIndex)
    {
        Assert.SkipUnless(OperatingSystem.IsWindowsVersionAtLeast(10, 0, 22000), "AppWindow requires Windows 11.");
        using var harness = new ApplicationHarness(() => ApplicationTestStartup.Run<ChromeWindow>(new()
        {
            Activate = false, IsMinimizable = minimize, IsMaximizable = maximize,
        }));
        var window = harness.Window<ChromeWindow>();
        var capture = ApplicationHarness.On(window, () =>
        {
            window.Style = "-windows-title-bar-height: tall; -windows-title-bar-button-background-color: transparent; -windows-title-bar-button-inactive-background-color: transparent";
            var bar = Assert.IsType<Nazor.Controls.UserTitleBar>(window.Root.Elements("TitleBar").Single().Owner);
            bar.SetParametersAsync(ParameterView.FromDictionary(new Dictionary<string, object?>
            {
                ["Style"] = "height: 32px; background: #202020; color: white",
            }));
            window.Style = "-windows-title-bar-height: standard; -windows-title-bar-button-background-color: #202020; -windows-title-bar-button-inactive-background-color: #202020";
            RedrawWindow(window.PlatformWindow.Handle, null, 0, 0x101);
            return (window.PlatformWindow.Handle, Width: window.SystemButtonsMetrics.Width * window.PlatformWindow.Scale,
                Height: window.SystemButtonsMetrics.Height * window.PlatformWindow.Scale);
        });
        var image = ScreenCapture.Capture(capture.Handle);
        byte Glyph(int index)
        {
            var start = image.Width - (int)(capture.Width * (3 - index) / 3);
            var width = (int)(capture.Width / 3);
            return Enumerable.Range(start + width / 3, width / 3)
                .SelectMany(x => Enumerable.Range((int)capture.Height / 3, (int)capture.Height / 3).Select(y => image.GetPixel(x, y).R)).Max();
        }
        var disabled = Glyph(disabledIndex);
        Assert.InRange(disabled, (byte)50, (byte)200);
    }

    [Theory]
    [InlineData(Nazor.Style.ColorScheme.Light)]
    [InlineData(Nazor.Style.ColorScheme.Dark)]
    public unsafe void Native_buttons_have_a_transparent_background_and_follow_the_color_scheme(Nazor.Style.ColorScheme scheme)
    {
        using var harness = new ApplicationHarness(() => ApplicationTestStartup.Run<ChromeWindow>(new() { Activate = true, ColorScheme = scheme, Theme = Themes.Themes.Basic }));
        var window = harness.Window<ChromeWindow>();
        Assert.SkipUnless(ApplicationHarness.On(window, () => OperatingSystem.IsWindowsVersionAtLeast(10, 0, 22000)), "Native caption appearance requires Windows 11 AppWindow.");
        var handle = ApplicationHarness.On(window, () =>
        {
            window.Style = "-windows-title-bar-height: tall";
            var bar = Assert.IsType<Nazor.Controls.UserTitleBar>(window.Root.Elements("TitleBar").Single().Owner);
            bar.SetParametersAsync(ParameterView.FromDictionary(new Dictionary<string, object?>
            {
                ["Style"] = "height: 48px; background: #174878",
            }));
            window.Activate();
            RedrawWindow(window.PlatformWindow.Handle, null, 0, 0x101);
            return window.PlatformWindow.Handle;
        });
        try
        {
            Assert.SkipUnless(GetForegroundWindow() == handle, "Native caption pixel capture requires an unobscured foreground test window.");
            var scale = ApplicationHarness.On(window, () => window.PlatformWindow.Scale);
            var width = ApplicationHarness.On(window, () => window.SystemButtonsSize.Width);
            Nazor.Graphics.RenderedImage? image = null;
            var background = new Nazor.Graphics.Color(0x17, 0x48, 0x78, 255);
            var foreground = ApplicationHarness.On(window, () =>
            {
                var color = window.Scene.GetStyle(window.Root.Elements("TitleBar").Single()).Get(Nazor.Style.StyleProperties.Color);
                return new Nazor.Graphics.Color(color.R, color.G, color.B, 255);
            });
            Assert.Equal(scheme == Nazor.Style.ColorScheme.Dark, foreground.R > 128);
            bool HasForeground(Nazor.Graphics.RenderedImage frame)
            {
                // Windows chooses the native glyph shade independently of the content color.
                bool MatchesScheme(Nazor.Graphics.Color pixel) => scheme == Nazor.Style.ColorScheme.Dark
                    ? pixel.R >= 200 && pixel.G >= 200 && pixel.B >= 200
                    : pixel.R <= 32 && pixel.G <= 32 && pixel.B <= 32;
                var left = frame.Width - (int)(width * scale);
                return Enumerable.Range(left, (int)(width * scale / 3)).Any(x =>
                    Enumerable.Range((int)(10 * scale), (int)(24 * scale)).Any(y => MatchesScheme(frame.GetPixel(x, y))));
            }
            var matched = SpinWait.SpinUntil(() =>
            {
                image = ScreenCapture.CaptureDesktopWindow(handle);
                return image.GetPixel(image.Width - (int)(width * scale) + (int)(10 * scale), (int)(8 * scale)) == background && HasForeground(image);
            }, TimeSpan.FromSeconds(3));
            var folder = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../artifacts/native-caption"));
            Directory.CreateDirectory(folder);
            using var graphics = new Nazor.Skia.SkiaRenderer();
            File.WriteAllBytes(Path.Combine(folder, $"caption-{scheme}.png"), graphics.EncodePng(image!));
            if (!matched && GetForegroundWindow() != handle)
                Assert.Skip("The desktop foreground changed during native caption pixel capture.");
            Assert.True(matched, "Native caption background or foreground did not match the requested appearance.");
            Assert.Empty(ApplicationHarness.On(window, () => window.Exceptions));
        }
        finally
        {
            ApplicationHarness.On(window, () => { window.Close(); return true; });
            Assert.True(harness.WaitForExit());
        }
    }

    [Fact]
    public unsafe void Native_height_uses_NSS_and_publishes_the_actual_logical_measurement()
    {
        using var harness = new ApplicationHarness(() => ApplicationTestStartup.Run<ChromeWindow>(new() { Activate = false }));
        var window = harness.Window<ChromeWindow>();
        ApplicationHarness.On(window, () =>
        {
            if (!OperatingSystem.IsWindowsVersionAtLeast(10, 0, 22000)) return true;
            window.Style = "-windows-title-bar-height: tall";
            var element = window.Root.Elements("TitleBar").Single();
            var bar = Assert.IsType<Nazor.Controls.UserTitleBar>(element.Owner);
            bar.SetParametersAsync(ParameterView.FromDictionary(new Dictionary<string, object?>
            {
                ["Style"] = "height: 48px; background: #174878; color: white",
            }));
            RedrawWindow(window.PlatformWindow.Handle, null, 0, 0x101  );
            Assert.Equal(48f, window.SystemButtonsSize.Height);
            Assert.Equal(48f, window.Scene.Styles.SystemButtonsHeight);
            window.Hide();
            Assert.Null(window.Scene.Styles.SystemButtonsHeight);
            window.Show(activate: false);
            Assert.Equal(48f, window.Scene.Styles.SystemButtonsHeight);
            var native = window.PlatformWindow;
            var point = new POINT { X = (int)((native.ClientSize.Width - window.SystemButtonsSize.Width / 2) * native.Scale), Y = (int)(40 * native.Scale) };
            Assert.True(ClientToScreen(native.Handle, ref point));
            Assert.Equal((nint)HTMAXBUTTON, SendMessageW(native.Handle, WM_NCHITTEST, 0, Pack(point.X, point.Y)));
            window.Style = "-windows-title-bar-height: standard";
            RedrawWindow(native.Handle, null, 0, 0x101);
            Assert.Equal(32f, window.SystemButtonsSize.Height);
            window.Style = "-windows-title-bar-height: tall";
            RedrawWindow(native.Handle, null, 0, 0x101);
            Assert.Equal(48f, window.SystemButtonsSize.Height);
            Assert.Equal(48f, window.Scene.Styles.SystemButtonsHeight);
            Assert.Empty(window.Exceptions);
            return true;
        });
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct GuiThreadInfo
    {
        public uint Size, Flags;
        public nint Active, Focus, Capture, MenuOwner, MoveSize, Caret;
        public RECT CaretBounds;
    }

    [LibraryImport("user32.dll")]
    private static partial uint GetWindowThreadProcessId(nint hwnd, out uint process);

    [LibraryImport("user32.dll")][return: MarshalAs(UnmanagedType.Bool)]
    private static unsafe partial bool GetGUIThreadInfo(uint thread, GuiThreadInfo* info);

    [LibraryImport("user32.dll")]
    private static partial uint GetMenuState(nint menu, uint command, uint flags);

    [LibraryImport("user32.dll", StringMarshalling = StringMarshalling.Utf16)]
    private static partial nint FindWindowExW(nint parent, nint after, string className, string? title);

    private static unsafe bool NativeMenuOpen(uint thread, nint owner)
    {
        var info = new GuiThreadInfo { Size = (uint)Marshal.SizeOf<GuiThreadInfo>() };
        return GetGUIThreadInfo(thread, &info) && (info.Flags & 4  ) != 0 && info.MenuOwner == owner;
    }

    private static nint FindVisibleNativeMenu(uint thread)
    {
        for (nint popup = 0; (popup = FindWindowExW(0, popup, "#32768", null)) != 0;)
            if (GetWindowThreadProcessId(popup, out _) == thread && IsWindowVisible(popup)) return popup;
        return 0;
    }

    private static unsafe string NativeMenuState(uint thread)
    {
        var info = new GuiThreadInfo { Size = (uint)Marshal.SizeOf<GuiThreadInfo>() };
        return $"read={GetGUIThreadInfo(thread, &info)}, flags={info.Flags}, menu owner={info.MenuOwner}, active={info.Active}";
    }

    [Fact]
    public unsafe void Original_caption_buttons_are_owned_by_Windows_and_can_be_toggled_and_hidden_in_full_screen()
    {
        using var harness = new ApplicationHarness(() => ApplicationTestStartup.Run<ChromeWindow>(new() { Activate = false }));
        var window = harness.Window<ChromeWindow>();
        ApplicationHarness.On(window, () =>
        {
            window.Scene.Update();
            var native = window.PlatformWindow;
            Assert.Empty(window.Root.Elements("TitleBarButton"));
            Assert.True(window.SystemButtonsSize.Width > 0);
            Assert.Empty(window.Root.Elements("TitleBarSystemArea"));
            Assert.Equal(window.SystemButtonsMetrics.RightInset,
                window.Scene.GetLayoutNode(window.Root.Elements("TitleBar").Single())!.Style.ContentInsets.Right);
            var origin = new POINT();
            Assert.True(ClientToScreen(native.Handle, ref origin));
            var width = window.SystemButtonsSize.Width * native.Scale;
            var left = origin.X + native.ClientSize.Width * native.Scale - width;
            var y = origin.Y + window.SystemButtonsSize.Height * native.Scale / 2;
            foreach (var (x, expected) in new[]
            {
                (left + width / 6, HTMINBUTTON),
                (left + width / 2, HTMAXBUTTON),
                (left + width * 5 / 6, HTCLOSE),
            })
            {
                var point = Pack((int)x, (int)y);
                Assert.Equal((nint)expected, SendMessageW(native.Handle, WM_NCHITTEST, 0, point));
            }
            var search = window.Root.Elements().Single(e => e.Class == "search");
            var searchButton = Assert.IsType<Nazor.Controls.Button>(search.Owner);
            searchButton.SetParametersAsync(ParameterView.FromDictionary(new Dictionary<string, object?>
            {
                ["AdditionalAttributes"] = new Dictionary<string, object?> { ["snap-layout-area"] = true },
            }));
            var center = Center(window, search);
            var screen = new POINT { X = (int)(center.X * native.Scale), Y = (int)(center.Y * native.Scale) };
            Assert.True(ClientToScreen(native.Handle, ref screen));
            SendMessageW(native.Handle, WM_NCMOUSEMOVE, HTMAXBUTTON, Pack(screen.X, screen.Y));
            Assert.True((search.States & Nazor.Rendering.ElementStates.Hover) != 0);
            SendMessageW(native.Handle, WM_NCMOUSELEAVE, 0, 0);
            Assert.False((search.States & Nazor.Rendering.ElementStates.Hover) != 0);
            searchButton.SetParametersAsync(ParameterView.FromDictionary(new Dictionary<string, object?> { ["AdditionalAttributes"] = null }));
            var component = (ChromeWindow)window.Root.Component;
            component.ToggleSystemButtons();
            window.Scene.Update();
            Assert.Equal((0f, 0f), window.SystemButtonsSize);
            Assert.NotEqual((nint)HTCLOSE, SendMessageW(native.Handle, WM_NCHITTEST, 0, Pack((int)(left + width * 5 / 6), (int)y)));
            Assert.Empty(window.Root.Elements("TitleBarSystemArea"));
            Assert.NotEqual((nint)0, GetSystemMenu(native.Handle, false));
            component.ToggleSystemButtons();
            window.Scene.Update();
            Assert.True(window.SystemButtonsSize.Width > 0);
            window.EnterFullScreen();
            Assert.Equal((0f, 0f), window.SystemButtonsSize);
            window.ExitFullScreen();
            window.Scene.Update();
            Assert.True(window.SystemButtonsSize.Width > 0);
            Assert.Equal((long)WS_CAPTION, (long)GetWindowLongPtrW(native.Handle, -16) & WS_CAPTION);
            window.EnterFullScreen();
            component.ToggleSystemButtons();
            window.ExitFullScreen();
            Assert.Equal((0f, 0f), window.SystemButtonsSize);
            Assert.Empty(window.Root.Elements("TitleBarSystemArea"));
            component.ToggleSystemButtons();
            window.Scene.Update();
            Assert.Empty(window.Exceptions);
            return true;
        });
        var handle = ApplicationHarness.On(window, () => window.PlatformWindow.Handle);
        var folder = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../artifacts/native-caption"));
        Directory.CreateDirectory(folder);
        using var graphics = new Nazor.Skia.SkiaRenderer();
        File.WriteAllBytes(Path.Combine(folder, "native-buttons.png"), graphics.EncodePng(ScreenCapture.Capture(handle)));
    }

    [Theory]
    [InlineData(false, false, false)]
    [InlineData(true, false, false)]
    [InlineData(false, true, false)]
    [InlineData(true, true, false)]
    [InlineData(false, false, true)]
    [InlineData(true, false, true)]
    [InlineData(false, true, true)]
    [InlineData(true, true, true)]
    [InlineData(false, false, false, false, false)]
    [InlineData(true, true, false, false, false)]
    [InlineData(false, true, false, false, true)]
    [InlineData(true, false, false, false, true)]
    [InlineData(false, false, false, true, true, Nazor.Style.ColorScheme.Light)]
    [InlineData(true, true, false, true, true, Nazor.Style.ColorScheme.Dark)]
    public void Right_click_and_Alt_Space_open_the_original_Windows_menu(bool keyboard, bool customButtons, bool maximized, bool resize = true, bool minimize = true, Nazor.Style.ColorScheme scheme = Nazor.Style.ColorScheme.System)
    {
        using var harness = new ApplicationHarness(() => ApplicationTestStartup.Run<ChromeWindow>(new() { Activate = false, IsResizable = resize, IsMaximizable = resize, IsMinimizable = minimize, ColorScheme = scheme }));
        var window = harness.Window<ChromeWindow>();
        if (scheme != Nazor.Style.ColorScheme.System)
            Assert.SkipUnless(OperatingSystem.IsWindowsVersionAtLeast(10, 0, 22000) && !ApplicationHarness.On(window, () => window.PlatformWindow.PrefersHighContrast), "Menu theme compatibility is Windows 11 only and respects high contrast.");
        var (handle, menu) = ApplicationHarness.On(window, () =>
        {
            if (customButtons) ((ChromeWindow)window.Root.Component).ToggleSystemButtons();
            if (maximized) window.Maximize();
            window.Activate();
            window.Scene.Update();
            var handle = window.PlatformWindow.Handle;
            return (handle, GetSystemMenu(handle, false));
        });
        Assert.NotEqual(0, menu);
        var thread = GetWindowThreadProcessId(handle, out _);
        Assert.SkipUnless(GetForegroundWindow() == handle, "Native system-menu verification requires a foreground test window.");
        try
        {
            if (keyboard) PostMessageW(handle, WM_SYSCOMMAND, 0xF100  , 32);
            else
            {
                var point = ApplicationHarness.On(window, () => Center(window, window.Root.Elements().Single(e => e.Class == "brand")));
                var scale = ApplicationHarness.On(window, () => window.PlatformWindow.Scale);
                var screen = new POINT { X = (int)(point.X * scale), Y = (int)(point.Y * scale) };
                Assert.True(ClientToScreen(handle, ref screen));
                PostMessageW(handle, WM_NCRBUTTONUP, HTCAPTION, Pack(screen.X, screen.Y));
            }
            // Windows can enter menu mode before the popup has initialized its command states.
            Assert.True(SpinWait.SpinUntil(() => NativeMenuOpen(thread, handle) && FindVisibleNativeMenu(thread) != 0,
                TimeSpan.FromSeconds(3)), NativeMenuState(thread));
            void AssertDisabled(uint command, bool expected)
            {
                var state = GetMenuState(menu, command, 0);
                Assert.True(((state & 3) != 0) == expected, $"System menu command 0x{command:X} has flags 0x{state:X}, expected disabled={expected}.");
            }
            AssertDisabled(0xF030, maximized || !resize);
            AssertDisabled(0xF020, !minimize);
            AssertDisabled(0xF000, maximized || !resize);
            AssertDisabled(0xF120, !maximized);
            Assert.True(ApplicationHarness.On(window, () => window.Scene.Overlays.Count == 0));
            if (scheme != Nazor.Style.ColorScheme.System)
            {
                Nazor.Graphics.RenderedImage? image = null;
                var dark = scheme == Nazor.Style.ColorScheme.Dark;
                var matched = SpinWait.SpinUntil(() =>
                {
                    var popup = FindVisibleNativeMenu(thread);
                    if (popup == 0) return false;
                    image = ScreenCapture.CaptureDesktopWindow(popup);
                    if (image.Width <= 50 || image.Height <= 50) return false;
                    var background = image.GetPixel(image.Width - 12, 12);
                    if ((background.R < 128) != dark) return false;
                    return Enumerable.Range(30, image.Width - 50).Any(x => Enumerable.Range(30, image.Height - 50)
                        .Any(y => dark ? image.GetPixel(x, y).R > 190 : image.GetPixel(x, y).R < 80));
                }, TimeSpan.FromSeconds(3));
                if (image is not null)
                {
                    var folder = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../artifacts/native-caption"));
                    Directory.CreateDirectory(folder);
                    using var graphics = new Nazor.Skia.SkiaRenderer();
                    File.WriteAllBytes(Path.Combine(folder, $"menu-{scheme}.png"), graphics.EncodePng(image));
                }
                Assert.True(matched, "The native menu did not use a contrasting foreground and background for the requested scheme.");
            }
        }
        finally
        {
            PostMessageW(handle, 0x001F  , 0, 0);
            PostMessageW(handle, WM_KEYDOWN, 0x1B  , 1);
        }
        Assert.True(SpinWait.SpinUntil(() => !NativeMenuOpen(thread, handle), TimeSpan.FromSeconds(3)));
        Assert.Empty(ApplicationHarness.On(window, () => window.Exceptions));
    }
}
