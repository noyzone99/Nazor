using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using Nazor.Graphics;
using Nazor.Input;
using Nazor.Platform;
using Nazor.Platform.Windows;

namespace Nazor.Application.Tests;

[Collection("Windows")]
[System.Runtime.Versioning.SupportedOSPlatform("windows")]
public sealed partial class WindowsLauncherTests
{
    [Fact]
    public void Windows_arguments_round_trip_empty_values_quotes_slashes_and_unicode()
    {
        string[] expected = ["", "plain", "with space", "a\"b", "\\", "C:\\folder with space\\", "\\\"", "日本語", "\t\r\n", "$(literal)"];
        var text = "application.exe " + Win32LauncherShell.FormatArguments(expected);
        var pointer = CommandLineToArgvW(text, out var count);
        Assert.NotEqual(0, pointer);
        try
        {
            var actual = Enumerable.Range(1, count - 1).Select(index => Marshal.PtrToStringUni(Marshal.ReadIntPtr(pointer, index * IntPtr.Size))).ToArray();
            Assert.Equal(expected, actual);
        }
        finally { LocalFree(pointer); }
    }

    [Fact]
    public void Application_badge_waits_for_taskbar_readiness_moves_between_windows_and_recovers_after_restart()
    {
        WindowsPlatformHost.RunOnUiThread(() =>
        {
            using var platform = new Win32Platform();
            var calls = new List<(nint Window, bool HasIcon, string Description)>();
            var clearedWhileVisible = new List<bool>();
            var accept = true;
            using var launcher = new Win32ApplicationLauncher((window, icon, description) =>
            {
                calls.Add((window, icon != 0, description));
                if (icon == 0) clearedWhileVisible.Add(Win32.IsWindowVisible(window));
                return accept;
            });
            Assert.True(launcher.SetBadgeCount(4));
            Assert.Empty(calls);
            using var first = new Win32Window(new Host(), new("Launcher first", 100, 100), launcher: launcher);
            using var second = new Win32Window(new Host(), new("Launcher second", 100, 100), launcher: launcher);
            first.Show(false);
            second.Show(false);
            Ready(first);
            Ready(second);
            Assert.Equal((first.Handle, true, "4"), calls[^1]);
            accept = false;
            Assert.False(launcher.SetBadgeCount(9));
            accept = true;
            launcher.Refresh();
            Assert.Equal("4", calls[^1].Description);
            first.Hide();
            Assert.True(clearedWhileVisible[^1]);
            Assert.Equal((second.Handle, true, "4"), calls[^1]);
            var icon = new RenderedImage(1, 1, [0, 0, 255, 255]);
            Assert.True(launcher.SetBadgeIcon([icon], "Attention"));
            Assert.Equal("Attention", calls[^1].Description);
            launcher.RestartTaskbar();
            calls.Clear();
            launcher.Refresh();
            Assert.Empty(calls);
            Ready(second);
            Assert.Equal((second.Handle, true, "Attention"), calls[^1]);
            Assert.True(launcher.ClearBadge());
            Assert.False(calls[^1].HasIcon);
            launcher.RestartTaskbar();
            Ready(second);
            Assert.False(calls[^1].HasIcon);
            Assert.True(launcher.SetBadgeCount(12));
            second.Dispose();
            first.Show(false);
            Ready(first);
            Assert.Equal((first.Handle, true, "12"), calls[^1]);
            return 0;
        });
        static void Ready(Win32Window window) => window.OnMessage(window.Handle, Win32ApplicationLauncher.TaskbarButtonCreated, 0, 0);
    }

    [Fact]
    public void Native_Jump_List_publishes_and_clears_tasks_under_an_isolated_identity()
    {
        WindowsPlatformHost.RunOnUiThread(() =>
        {
            using var shell = new Win32LauncherShell();
            Assert.Throws<ArgumentException>(() => shell.SetJumpList([new("Too long", [new string('a', 32767)])]));
            var appId = "Nazor.LauncherTests." + Guid.NewGuid().ToString("N");
            try
            {
                Assert.True(shell.SetJumpList([new("Open test", ["--new-window", "a b", ""]), new("Default launch", [])], appId));
                Assert.True(shell.SetJumpList([], appId));
            }
            finally
            {
                Marshal.ThrowExceptionForHR(Win32.CoCreateInstance(new("77f10cf0-3db5-4966-b520-b7c54fd35ed6"), 0, 1,
                    typeof(ICustomDestinationList).GUID, out var pointer));
                var wrappers = new StrategyBasedComWrappers();
                try
                {
                    var list = (ICustomDestinationList)wrappers.GetOrCreateObjectForComInstance(pointer, CreateObjectFlags.UniqueInstance);
                    try { list.DeleteList(appId); }
                    finally { ((ComObject)(object)list).FinalRelease(); }
                }
                finally { Marshal.Release(pointer); }
            }
            return 0;
        });
    }

    [Fact]
    public void Native_overlay_accepts_count_icon_and_clear_for_a_real_window()
    {
        WindowsPlatformHost.RunOnUiThread(() =>
        {
            using var platform = new Win32Platform();
            using var window = (Win32Window)platform.CreateWindow(new Host(), new("Native launcher", 100, 100));
            var desktop = platform.DesktopIntegration;
            window.Show(false);
            window.OnMessage(window.Handle, Win32ApplicationLauncher.TaskbarButtonCreated, 0, 0);
            Assert.Equal(new ApplicationLauncherCapabilities(true, true, true), desktop.LauncherCapabilities);
            Assert.True(desktop.SetBadgeCount(5));
            Assert.True(desktop.SetBadgeIcon([new RenderedImage(1, 1, [0, 0, 255, 255])], "Native test"));
            Assert.True(desktop.ClearBadge());
            return 0;
        });
    }

    [Fact]
    public void Counter_images_scale_and_keep_counts_above_99_distinct()
    {
        var small = Win32ApplicationLauncher.RenderCount(1, 16);
        var large = Win32ApplicationLauncher.RenderCount(100, 32);
        Assert.Equal(16 * 16 * 4, small.Pixels.Length);
        Assert.Equal(32 * 32 * 4, large.Pixels.Length);
        Assert.Contains(large.Pixels.Where((_, index) => index % 4 == 3), alpha => alpha != 0);
        Assert.NotEqual(large.Pixels, Win32ApplicationLauncher.RenderCount(101, 32).Pixels);
        var maximum = Win32ApplicationLauncher.RenderCount(int.MaxValue, 32);
        Assert.NotEqual(large.Pixels, maximum.Pixels);
        Assert.Contains(maximum.Pixels.Where((_, index) => index % 4 == 3), alpha => alpha != 0);
    }

    [Fact]
    public void Badge_container_background_border_opacity_and_size_affect_the_composed_pixels()
    {
        var style = new WindowsBadgeAppearance { Width = 12, Height = 10, Radius = default, BackgroundColor = new(0, 0, 255, 255),
            FontSize = 0, BorderWidth = 1, BorderColor = new(255, 0, 0, 255), Opacity = 0.5f };
        var rendered = WindowsBadgeRenderer.Render(1, null, style, 32);
        Assert.Equal((0, 0, 128, 128), Pixel(rendered, 16, 16));
        Assert.Equal((128, 0, 0, 128), Pixel(rendered, 4, 12));
        Assert.Equal((0, 0, 0, 0), Pixel(rendered, 0, 0));
        var empty = WindowsBadgeRenderer.Render(1, null, style with { Width = 0 }, 16);
        Assert.All(empty.Pixels, value => Assert.Equal(0, value));
    }

    [Fact]
    public void Badge_icons_keep_their_aspect_ratio_and_support_background_fit_position_and_rounded_clipping()
    {
        var source = new RenderedImage(8, 4, Enumerable.Range(0, 8 * 4).SelectMany(_ => new byte[] { 0, 0, 255, 255 }).ToArray());
        var style = new WindowsBadgeAppearance { ImageBackgroundColor = new(0, 0, 255, 255), Radius = default };
        var contained = WindowsBadgeRenderer.Render(null, source, style, 16);
        Assert.Equal((0, 0, 255, 255), Pixel(contained, 8, 1));
        Assert.Equal((255, 0, 0, 255), Pixel(contained, 8, 8));
        var top = WindowsBadgeRenderer.Render(null, source, style with { ImagePosition = default }, 16);
        Assert.Equal((255, 0, 0, 255), Pixel(top, 8, 1));
        Assert.Equal((0, 0, 255, 255), Pixel(top, 8, 14));
        var covered = WindowsBadgeRenderer.Render(null, source, style with { ImageFit = WindowsBadgeImageFit.Cover }, 16);
        Assert.Equal((255, 0, 0, 255), Pixel(covered, 1, 1));
        var rounded = WindowsBadgeRenderer.Render(null, source, style with { ImageFit = WindowsBadgeImageFit.Fill, Radius = 8, ClipImage = true }, 16);
        Assert.Equal((0, 0, 0, 0), Pixel(rounded, 0, 0));
        var faded = WindowsBadgeRenderer.Render(null, source, style with { ImageOpacity = 0 }, 16);
        Assert.Equal((0, 0, 255, 255), Pixel(faded, 8, 8));
    }

    [Fact]
    public void Badge_font_stroke_shadows_and_alignment_are_rendered_and_unchanged_styles_do_not_republish()
    {
        var baseline = WindowsBadgeRenderer.Render(12, null, new(), 64);
        var styled = WindowsBadgeRenderer.Render(12, null, new() { Width = 12, Height = 12, Color = new(0, 255, 0, 255),
            TextStrokeWidth = 0.5f, TextStrokeColor = new(0, 0, 255, 255), Italic = true, FontWeight = 400, LetterSpacing = 1,
            TextAlign = 1, VerticalAlign = 0, BorderWidth = 1, BorderStyle = LineStyle.Dotted, BorderColor = new(255, 255, 255, 255),
            Shadows = [new(default, default, new(1, 1), 1, 0, new(0, 0, 0, 255), false)],
            TextShadows = [new(default, default, new(1, 1), 1, 0, new(0, 0, 0, 255), false)] }, 64);
        Assert.NotEqual(baseline.Pixels, styled.Pixels);
        Assert.Contains(Enumerable.Range(0, 64 * 64), index => styled.Pixels[index * 4 + 1] > styled.Pixels[index * 4 + 2]);
        WindowsPlatformHost.RunOnUiThread(() =>
        {
            using var platform = new Win32Platform();
            var calls = new List<nint>();
            using var launcher = new Win32ApplicationLauncher((window, _, _) => { calls.Add(window); return true; });
            using var first = new Win32Window(new Host(), new("Styled badge first", 100, 100), launcher: launcher);
            using var second = new Win32Window(new Host(), new("Styled badge second", 100, 100), launcher: launcher);
            first.Show(false); second.Show(false);
            first.OnMessage(first.Handle, Win32ApplicationLauncher.TaskbarButtonCreated, 0, 0);
            second.OnMessage(second.Handle, Win32ApplicationLauncher.TaskbarButtonCreated, 0, 0);
            launcher.SetBadgeCount(5);
            var appearance = new WindowsBadgeAppearance { BackgroundColor = new(0, 255, 0, 255), FontFamilies = ["Arial"] };
            first.SetWindowsBadgeAppearance(appearance);
            Assert.Equal(first.Handle, calls[^1]);
            var callCount = calls.Count;
            first.SetWindowsBadgeAppearance(appearance with { FontFamilies = ["Arial"] });
            Assert.Equal(callCount, calls.Count);
            second.SetWindowsBadgeAppearance(appearance with { Width = 10 });
            first.Hide();
            Assert.Equal(second.Handle, calls[^1]);
            Assert.Equal(10, second.BadgeAppearance.Width);
            return 0;
        });
    }

    [Fact]
    public void Computed_window_badge_styles_reach_the_native_window_and_refresh_after_stylesheet_changes()
    {
        using var harness = new WindowHarness(typeof(SwitchableWindow));
        harness.Read(window =>
        {
            window.Scene.Styles.Application = Nazor.Nss.NssCompiler.Compile("badge", ":root { color: blue; font-size: 20px; -windows-badge-size: .6em; -windows-badge-color: currentColor; -windows-badge-image-position: calc(50% + 1px) top; }").StyleSheet;
            window.RequestFrame();
            return true;
        });
        harness.WaitUntil(window => ((Win32Window)window.PlatformWindow).BadgeAppearance.Width == 12, "computed badge style");
        var appearance = harness.Read(window => ((Win32Window)window.PlatformWindow).BadgeAppearance);
        Assert.Equal(new Color(0, 0, 255, 255), appearance.Color);
        Assert.Equal(new Point(.5f, 0), appearance.ImagePosition);
        Assert.Equal(new Point(1, 0), appearance.ImageOffset);
        harness.Read(window => { window.Scene.Styles.Application = Nazor.Nss.NssCompiler.Compile("updated", ":root { -windows-badge-size: 8px; }").StyleSheet; window.RequestFrame(); return true; });
        harness.WaitUntil(window => ((Win32Window)window.PlatformWindow).BadgeAppearance.Width == 8, "updated badge style");
        Assert.Empty(harness.Read(window => window.Exceptions));
    }

    private static (int R, int G, int B, int A) Pixel(RenderedImage image, int x, int y)
    {
        var offset = (y * image.Width + x) * 4;
        return (image.Pixels[offset + 2], image.Pixels[offset + 1], image.Pixels[offset], image.Pixels[offset + 3]);
    }

    private sealed class Host : IWindowHost
    {
        public void OnResized(Size size, float scale) { }
        public IReadOnlyList<PixelRect>? OnPaint(PaintTarget target) => null;
        public void OnPointerMoved(Point point, KeyModifiers modifiers) { }
        public void OnPointerPressed(Point point, PointerButton button, KeyModifiers modifiers) { }
        public void OnPointerReleased(Point point, PointerButton button, KeyModifiers modifiers) { }
        public void OnPointerLeft() { }
        public void OnPointerWheel(Point point, float notches, KeyModifiers modifiers) { }
        public void OnPointerHorizontalWheel(Point point, float notches, KeyModifiers modifiers) { }
        public void OnPointerCanceled() { }
        public bool OnKeyDown(string key, KeyModifiers modifiers, bool repeat) => false;
        public bool OnKeyUp(string key, KeyModifiers modifiers) => false;
        public void OnTextInput(string text) { }
        public void OnComposition(string text, int cursor) { }
        public void OnCompositionCommit(string text) { }
        public void OnFocusChanged(bool focused) { }
        public void OnVisibilityChanged(bool visible) { }
        public void OnSystemSettingsChanged() { }
        public bool OnClosing() => true;
        public void OnClosed() { }
        public PlatformCursor GetCursor(Point point) => PlatformCursor.Arrow;
        public Nazor.Accessibility.SemanticTree? GetSemanticTree() => null;
    }

    [LibraryImport("shell32.dll", StringMarshalling = StringMarshalling.Utf16)] private static partial nint CommandLineToArgvW(string commandLine, out int count);
    [LibraryImport("kernel32.dll")] private static partial nint LocalFree(nint pointer);
}
