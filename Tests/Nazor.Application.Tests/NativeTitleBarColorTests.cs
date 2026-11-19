using Microsoft.UI.Windowing;
using Nazor.Testing;

namespace Nazor.Application.Tests;

[Collection("Windows")]
[System.Runtime.Versioning.SupportedOSPlatform("windows")]
public sealed class NativeTitleBarColorTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Solid_system_caption_and_buttons_have_the_requested_pixels_after_native_transitions(bool fluent)
    {
        Assert.SkipUnless(OperatingSystem.IsWindowsVersionAtLeast(10, 0, 22621), "Native materials require Windows 11 22H2.");
        using var harness = new WindowHarness(typeof(NativeAppearancePage), new()
        {
            Theme = fluent ? Nazor.Controls.StyleSheets.Basic : Nazor.Controls.StyleSheets.None,
        });
        foreach (var scheme in new[] { Nazor.Style.ColorScheme.Light, Nazor.Style.ColorScheme.Dark })
        {
            harness.Read(window => { window.SetColorScheme(scheme); window.Activate(); return true; });
            foreach (var transition in new[] { false, true })
            {
                if (transition) harness.Read(window =>
                {
                    foreach (var material in new[] { WindowsBackdrop.Mica, WindowsBackdrop.Acrylic, WindowsBackdrop.MicaAlt })
                    {
                        window.Backdrop = material;
                        window.Backdrop = CommonBackdrop.None;
                    }
                    window.ApplyDecorations((true) ? DecorationTestExtensions.ClientTitleBar(WindowTitleBarContent.Nazor) : WindowDecorations.Native());
                    window.RequestFrame();
                    Nazor.Platform.Windows.Win32.UpdateWindow(window.PlatformWindow.Handle);
                    window.ApplyDecorations((false) ? DecorationTestExtensions.ClientTitleBar(WindowTitleBarContent.Nazor) : WindowDecorations.Native());
                    window.EnterFullScreen();
                    window.ExitFullScreen();
                    window.RequestFrame();
                    Nazor.Platform.Windows.Win32.UpdateWindow(window.PlatformWindow.Handle);
                    return true;
                });
                Assert.SkipUnless(Nazor.Platform.Windows.Win32.GetForegroundWindow() == harness.Handle, "Caption pixel capture requires a foreground test window.");
                var expected = scheme == Nazor.Style.ColorScheme.Dark
                    ? new Nazor.Graphics.Color(0x1e, 0x21, 0x23, 255) : new Nazor.Graphics.Color(0xee, 0xf4, 0xf9, 255);
                Nazor.Graphics.RenderedImage? captured = null;
                var matched = SpinWait.SpinUntil(() =>
                {
                    var image = captured = ScreenCapture.CaptureDesktopFrame(harness.Handle);
                    return image.GetPixel(image.Width / 2, 12) == expected && image.GetPixel(image.Width - 75, 5) == expected;
                }, TimeSpan.FromSeconds(2));
                Assert.SkipUnless(Nazor.Platform.Windows.Win32.GetForegroundWindow() == harness.Handle, "The foreground changed during caption pixel capture.");
                if (!matched)
                {
                    var folder = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../artifacts/native-caption"));
                    Directory.CreateDirectory(folder);
                    using var graphics = new Nazor.Skia.SkiaRenderer();
                    File.WriteAllBytes(Path.Combine(folder, $"solid-{fluent}-{scheme}-{transition}.png"), graphics.EncodePng(captured!));
                }
                Assert.True(matched, $"Caption and button backgrounds must remain solid in {scheme} mode (transition: {transition}); caption={captured!.GetPixel(captured.Width / 2, 12)}, buttons={captured.GetPixel(captured.Width - 75, 5)}.");
            }
        }
        Assert.Empty(harness.Read(window => window.Exceptions.ToArray()));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public unsafe void Themes_do_not_override_the_solid_system_caption_when_leaving_a_custom_title_bar(int theme)
    {
        Assert.SkipUnless(OperatingSystem.IsWindowsVersionAtLeast(10, 0, 22000), "Title bar colors require Windows 11.");
        using var harness = new WindowHarness(typeof(SwitchableWindow), new()
        {
            Theme = theme == 0 ? Nazor.Controls.StyleSheets.None : Nazor.Controls.StyleSheets.Basic,
        });
        harness.Read(window =>
        {
            foreach (var scheme in new[] { Nazor.Style.ColorScheme.Dark, Nazor.Style.ColorScheme.Light })
            {
                window.SetColorScheme(scheme);
                foreach (var custom in new[] { true, false, true, false })
                {
                    window.ApplyDecorations((custom) ? DecorationTestExtensions.ClientTitleBar(WindowTitleBarContent.Nazor) : WindowDecorations.Native());
                    window.RequestFrame();
                    Nazor.Platform.Windows.Win32.UpdateWindow(window.PlatformWindow.Handle);
                    var bar = AppWindow.GetFromWindowId(Microsoft.UI.Win32Interop.GetWindowIdFromWindow(window.PlatformWindow.Handle)).TitleBar;
                    Assert.Equal(custom, bar.ExtendsContentIntoTitleBar);
                    if (custom) continue;
                    var dark = -1;
                    Assert.Equal(0, Nazor.Platform.Windows.Win32.DwmGetWindowAttribute(window.PlatformWindow.Handle, 20, &dark, sizeof(int)));
                    Assert.Equal(scheme == Nazor.Style.ColorScheme.Dark ? 1 : 0, dark);
                    if (OperatingSystem.IsWindowsVersionAtLeast(10, 0, 22621))
                    {
                        var backdrop = -1;
                        Assert.Equal(0, Nazor.Platform.Windows.Win32.DwmGetWindowAttribute(window.PlatformWindow.Handle, 38, &backdrop, sizeof(int)));
                        Assert.Equal(1, backdrop);
                    }
                    foreach (var name in new[] { "BackgroundColor", "InactiveBackgroundColor", "ForegroundColor", "InactiveForegroundColor",
                        "ButtonBackgroundColor", "ButtonForegroundColor", "ButtonInactiveBackgroundColor", "ButtonInactiveForegroundColor",
                        "ButtonHoverBackgroundColor", "ButtonHoverForegroundColor", "ButtonPressedBackgroundColor", "ButtonPressedForegroundColor" })
                        Assert.Null(typeof(AppWindowTitleBar).GetProperty(name)!.GetValue(bar));
                }
            }
            Assert.Empty(window.Exceptions);
            return true;
        });
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void All_colors_reach_AppWindow_and_reset_after_styles_are_removed(bool custom)
    {
        Assert.SkipUnless(OperatingSystem.IsWindowsVersionAtLeast(10, 0, 22000), "Title bar colors require Windows 11.");
        using var harness = new WindowHarness(custom ? typeof(SwitchableWindow) : typeof(NativeAppearancePage));
        harness.Read(window =>
        {
            var names = new (string Nss, string Native)[]
            {
                ("-windows-title-bar-background-color", "BackgroundColor"),
                ("-windows-title-bar-foreground-color", "ForegroundColor"),
                ("-windows-title-bar-inactive-background-color", "InactiveBackgroundColor"),
                ("-windows-title-bar-inactive-foreground-color", "InactiveForegroundColor"),
                ("-windows-title-bar-button-background-color", "ButtonBackgroundColor"),
                ("-windows-title-bar-button-foreground-color", "ButtonForegroundColor"),
                ("-windows-title-bar-button-inactive-background-color", "ButtonInactiveBackgroundColor"),
                ("-windows-title-bar-button-inactive-foreground-color", "ButtonInactiveForegroundColor"),
                ("-windows-title-bar-button-hover-background-color", "ButtonHoverBackgroundColor"),
                ("-windows-title-bar-button-hover-foreground-color", "ButtonHoverForegroundColor"),
                ("-windows-title-bar-button-pressed-background-color", "ButtonPressedBackgroundColor"),
                ("-windows-title-bar-button-pressed-foreground-color", "ButtonPressedForegroundColor"),
            };
            window.Style = string.Join("; ", names.Select(p => p.Nss + ": #123456"));
            window.RequestFrame();
            Nazor.Platform.Windows.Win32.UpdateWindow(window.PlatformWindow.Handle);
            var bar = AppWindow.GetFromWindowId(Microsoft.UI.Win32Interop.GetWindowIdFromWindow(window.PlatformWindow.Handle)).TitleBar;
            Assert.Equal(custom, bar.ExtendsContentIntoTitleBar);
            foreach (var name in names.Where(n => !custom || n.Native.StartsWith("Button", StringComparison.Ordinal)))
            {
                var actual = Assert.IsType<global::Windows.UI.Color>(typeof(AppWindowTitleBar).GetProperty(name.Native)!.GetValue(bar));
                Assert.Equal((byte)0x12, actual.R);
                Assert.Equal((byte)0x34, actual.G);
                Assert.Equal((byte)0x56, actual.B);
            }
            window.Style = "-windows-title-bar-button-foreground-color: #abcdef";
            window.RequestFrame();
            Nazor.Platform.Windows.Win32.UpdateWindow(window.PlatformWindow.Handle);
            Assert.Equal((byte)0xab, bar.ButtonForegroundColor!.Value.R);
            window.Style = string.Join("; ", names.Select(p => p.Nss + ": auto"));
            window.RequestFrame();
            Nazor.Platform.Windows.Win32.UpdateWindow(window.PlatformWindow.Handle);
            if (!custom)
            {
                foreach (var name in names)
                    Assert.Null(typeof(AppWindowTitleBar).GetProperty(name.Native)!.GetValue(bar));
            }
            else if (!window.PlatformWindow.PrefersHighContrast)
            {
                Assert.Equal((byte)0, bar.ButtonBackgroundColor!.Value.A);
                Assert.Equal((byte)0, bar.ButtonInactiveBackgroundColor!.Value.A);
            }
            Assert.Empty(window.Exceptions);
            return true;
        });
    }

    [Fact]
    public void System_caption_button_backgrounds_follow_the_bar_unless_overridden()
    {
        Assert.SkipUnless(OperatingSystem.IsWindowsVersionAtLeast(10, 0, 22000), "Title bar colors require Windows 11.");
        using var harness = new WindowHarness(typeof(NativeAppearancePage));
        harness.Read(window =>
        {
            const string barColors = "-windows-title-bar-background-color: #123456; -windows-title-bar-inactive-background-color: #654321";
            const string buttonColors = "; -windows-title-bar-button-background-color: #abcdef; -windows-title-bar-button-inactive-background-color: #fedcba";
            foreach (var (style, active, inactive) in new (string, byte, byte)[]
            {
                (barColors, 0x12, 0x65),
                (barColors + buttonColors, 0xab, 0xfe),
                (barColors, 0x12, 0x65),
            })
            {
                window.Style = style;
                window.RequestFrame();
                Nazor.Platform.Windows.Win32.UpdateWindow(window.PlatformWindow.Handle);
                var bar = AppWindow.GetFromWindowId(Microsoft.UI.Win32Interop.GetWindowIdFromWindow(window.PlatformWindow.Handle)).TitleBar;
                Assert.Equal((byte)0x12, bar.BackgroundColor!.Value.R);
                Assert.Equal((byte)0x65, bar.InactiveBackgroundColor!.Value.R);
                Assert.Equal(active, bar.ButtonBackgroundColor!.Value.R);
                Assert.Equal(inactive, bar.ButtonInactiveBackgroundColor!.Value.R);
            }
            // Windows cannot show the window through system caption buttons, so translucent backgrounds sit on the bar.
            window.Style = barColors + "; -windows-title-bar-button-background-color: transparent; -windows-title-bar-button-inactive-background-color: transparent; " +
                "-windows-title-bar-button-hover-background-color: rgba(255, 255, 255, 0.5); -windows-title-bar-button-pressed-background-color: #ff0000";
            window.RequestFrame();
            Nazor.Platform.Windows.Win32.UpdateWindow(window.PlatformWindow.Handle);
            var composed = AppWindow.GetFromWindowId(Microsoft.UI.Win32Interop.GetWindowIdFromWindow(window.PlatformWindow.Handle)).TitleBar;
            Assert.Equal(new global::Windows.UI.Color { A = 255, R = 0x12, G = 0x34, B = 0x56 }, composed.ButtonBackgroundColor);
            Assert.Equal(new global::Windows.UI.Color { A = 255, R = 0x65, G = 0x43, B = 0x21 }, composed.ButtonInactiveBackgroundColor);
            Assert.Equal(new global::Windows.UI.Color { A = 255, R = 0x89, G = 0x9a, B = 0xab }, composed.ButtonHoverBackgroundColor);
            Assert.Equal(new global::Windows.UI.Color { A = 255, R = 0xff, G = 0, B = 0 }, composed.ButtonPressedBackgroundColor);
            Assert.Empty(window.Exceptions);
            return true;
        });
    }

    [Fact]
    public void Colors_survive_switching_between_system_and_custom_captions()
    {
        Assert.SkipUnless(OperatingSystem.IsWindowsVersionAtLeast(10, 0, 22000), "Title bar colors require Windows 11.");
        using var harness = new WindowHarness(typeof(SwitchableWindow));
        harness.Read(window =>
        {
            window.Style = "-windows-title-bar-background-color: #123456; -windows-title-bar-button-foreground-color: #abcdef";
            for (var index = 0; index < 4; index++)
            {
                window.ApplyDecorations((index % 2 != 0) ? DecorationTestExtensions.ClientTitleBar(WindowTitleBarContent.Nazor) : WindowDecorations.Native());
                window.RequestFrame();
                Nazor.Platform.Windows.Win32.UpdateWindow(window.PlatformWindow.Handle);
                var bar = AppWindow.GetFromWindowId(Microsoft.UI.Win32Interop.GetWindowIdFromWindow(window.PlatformWindow.Handle)).TitleBar;
                Assert.Equal((window.AppliedDecorations?.Configuration.HasNativeTitleBar == false), bar.ExtendsContentIntoTitleBar);
                Assert.Equal((byte)0xab, bar.ButtonForegroundColor!.Value.R);
                if (!(window.AppliedDecorations?.Configuration.HasNativeTitleBar == false)) Assert.Equal((byte)0x12, bar.BackgroundColor!.Value.R);
            }
            Assert.Empty(window.Exceptions);
            return true;
        });
    }
}
