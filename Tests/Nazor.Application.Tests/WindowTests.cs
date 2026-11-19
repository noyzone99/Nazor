using Nazor.Graphics;
using Nazor.Platform.Windows;
using Nazor.Rendering;
using Nazor.Samples.Counter;
using Nazor.Testing;
using ColorScheme = Nazor.Style.ColorScheme;
using MediaPreferences = Nazor.Style.MediaPreferences;

namespace Nazor.Application.Tests;

[Collection("Windows")]
[System.Runtime.Versioning.SupportedOSPlatform("windows")]
public sealed class WindowTests
{
    private static readonly ApplicationRuntimeSettings CounterOptions = new() { Theme = Nazor.Themes.Themes.Basic, Title = "Nazor test — Counter", Width = 320, Height = 200 };

    private static string CounterText(NazorWindow window) =>
        window.Root.Elements("TextBlock").Single().Text();

    private static Point ButtonCenter(NazorWindow window)
    {
        var bounds = window.Scene.GetBounds(window.Root.Single("Button"));
        return new Point(bounds.X + bounds.Width / 2, bounds.Y + bounds.Height / 2);
    }

    [Fact]
    public void Window_uses_per_monitor_dpi_and_the_requested_logical_size()
    {
        using var harness = new WindowHarness(typeof(Counter), CounterOptions);
        Assert.True(Win32Platform.IsPerMonitorDpiAware);
        var (size, scale) = harness.Read(w => (w.PlatformWindow.ClientSize, w.PlatformWindow.Scale));
        Assert.True(scale >= 1);
        Assert.Equal(320, size.Width, 0.6f * 2 / scale);
        Assert.Equal(200, size.Height, 0.6f * 2 / scale);
        Assert.Equal("Nazor test — Counter", harness.Read(w => w.PlatformWindow.Title));
        Assert.Equal(new NativeWindowHandle(harness.Handle, "HWND"), harness.Read(w => ((IWindow)w).NativeHandle));
    }

    [Fact]
    public void Mouse_click_updates_the_counter_and_draws_a_new_frame()
    {
        using var harness = new WindowHarness(typeof(Counter), CounterOptions);
        harness.WaitUntil(w => w.FrameCount > 0, "first frame");
        var center = harness.Read(ButtonCenter);
        var frames = harness.Read(w => w.FrameCount);

        harness.Click(center);

        harness.WaitUntil(w => CounterText(w) == "Count: 1", "the click handler");
        harness.WaitUntil(w => w.FrameCount > frames, "a frame after the click");
        Assert.Empty(harness.Read(w => w.Exceptions));
    }

    [Fact]
    public void Keyboard_focus_and_space_activate_the_button()
    {
        using var harness = new WindowHarness(typeof(Counter), CounterOptions);
        harness.WaitUntil(w => w.FrameCount > 0, "first frame");

        harness.Key(0x09, down: true);
        harness.Key(0x09, down: false);
        harness.WaitUntil(w => w.Input.Focused?.Name == "Button", "focus on the button");
        Assert.True(harness.Read(w => w.Input.Focused!.States.HasFlag(ElementStates.FocusVisible)));

        harness.Key(0x20, down: true);
        harness.Key(0x20, down: false);
        harness.WaitUntil(w => CounterText(w) == "Count: 1", "keyboard activation");
        harness.Key(0x0D, down: true);
        harness.Key(0x0D, down: false);
        harness.WaitUntil(w => CounterText(w) == "Count: 2", "Enter activation");
    }

    [Fact]
    public void Idle_window_draws_no_frames_and_hover_draws_one()
    {
        using var harness = new WindowHarness(typeof(Counter), CounterOptions);
        harness.WaitUntil(w => w.FrameCount > 0, "first frame");
        Thread.Sleep(300);
        var settled = harness.Read(w => w.FrameCount);
        Thread.Sleep(600);
        Assert.Equal(settled, harness.Read(w => w.FrameCount));

        var center = harness.Read(ButtonCenter);
        harness.Mouse(Platform.Windows.Win32.WM_MOUSEMOVE, center);
        harness.WaitUntil(w => w.FrameCount > settled, "a frame for the hover state");
        Thread.Sleep(300);
        var afterHover = harness.Read(w => w.FrameCount);
        Thread.Sleep(600);
        Assert.Equal(afterHover, harness.Read(w => w.FrameCount));
    }

    [Theory]
    [InlineData(RenderingMode.Cpu)]
    [InlineData(RenderingMode.Gpu)]
    public async Task Presented_pixels_match_the_scene(RenderingMode mode)
    {
        NativeDesktop.RequireUnlocked();
        using var harness = new WindowHarness(typeof(Counter), CounterOptions with
        {
            ColorScheme = ColorScheme.Light, Rendering = mode == RenderingMode.Gpu ? RenderingMode.Auto : mode,
            IsAlwaysOnTop = true,
        });
        harness.WaitUntil(w => w.FrameCount > 0, "first frame");
        if (mode == RenderingMode.Gpu && harness.Read(w => w.Rendering.Actual) != RenderingMode.Gpu)
            Assert.Skip("Hardware GPU unavailable.");
        var (button, scale) = harness.Read(w => (w.Scene.GetBounds(w.Root.Single("Button")), w.PlatformWindow.Scale));
        var probe = ((int)((button.X + 6) * scale), (int)((button.Y + button.Height / 2) * scale));
        var expected = await harness.Read(w => w.CaptureFrameAsync());
        var backgroundProbe = (X: expected.Width / 2, Y: expected.Height - 10);
        var background = expected.GetPixel(backgroundProbe.X, backgroundProbe.Y);
        var image = harness.Read(w => ScreenCapture.CaptureDesktopWindow(w.PlatformWindow.Handle));
        for (var attempt = 0; attempt < 100 && (image.GetPixel(probe.Item1, probe.Item2) != new Color(0x30, 0x30, 0x30, 255) ||
            image.GetPixel(backgroundProbe.X, backgroundProbe.Y) != background); attempt++)
        {
            Thread.Sleep(20);
            image = harness.Read(w => ScreenCapture.CaptureDesktopWindow(w.PlatformWindow.Handle));
        }
        var artifacts = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../artifacts/window"));
        Directory.CreateDirectory(artifacts);
        using (var png = new Nazor.Skia.SkiaRenderer())
            File.WriteAllBytes(Path.Combine(artifacts, "counter-window.png"), png.EncodePng(image));

        Assert.Equal(new Color(0x30, 0x30, 0x30, 255), image.GetPixel(probe.Item1, probe.Item2));
        Assert.Equal((expected.Width, expected.Height), (image.Width, image.Height));
        Assert.Equal(background, image.GetPixel(backgroundProbe.X, backgroundProbe.Y));
    }

    [Fact]
    public void A_transition_draws_paced_frames_and_then_stops()
    {
        using var harness = new WindowHarness(typeof(Counter), CounterOptions with { ReducedMotion = false });
        harness.WaitUntil(w => w.FrameCount > 0, "first frame");
        Thread.Sleep(200);
        var before = harness.Read(w => w.FrameCount);

        var started = DateTime.UtcNow;
        harness.Read(w =>
        {
            w.Root.Single("Button").SetState(ElementStates.Hover, true);
            w.RequestFrame();
            return true;
        });
        harness.WaitUntil(w => w.FrameCount > before && !w.Scene.Transitions.IsRunning, "the end of the transition");
        var elapsed = DateTime.UtcNow - started;
        var frames = harness.Read(w => w.FrameCount) - before;
        Thread.Sleep(400);
        var afterwards = harness.Read(w => w.FrameCount) - before;
        Console.WriteLine($"Counter hover transition: {frames} frames in {elapsed.TotalMilliseconds:F0} ms");

        Assert.InRange(elapsed.TotalMilliseconds, 180, 2000);
        Assert.InRange(frames, 4, 40);
        Assert.Equal(frames, afterwards);
        Assert.Equal(Style.Color.FromRgb(0, 0x78, 0xd4), harness.Read(w => w.Scene.GetEffectiveValue(w.Root.Single("Button"), Style.StyleProperties.BackgroundColor)));
    }

    [Fact]
    public void A_minimized_window_does_not_animate()
    {
        using var harness = new WindowHarness(typeof(Counter), CounterOptions with { ReducedMotion = false });
        harness.WaitUntil(w => w.FrameCount > 0, "first frame");
        Win32.ShowWindow(harness.Handle, 7);
        harness.WaitUntil(w => w.Scene.Transitions.Suspended, "the window hidden");
        harness.Read(w =>
        {
            w.Root.Single("Button").SetState(ElementStates.Hover, true);
            w.Scene.Update();
            return true;
        });
        Assert.False(harness.Read(w => w.Scene.Transitions.IsRunning));

        Win32.ShowWindow(harness.Handle, Win32.SW_SHOWNOACTIVATE);
        harness.WaitUntil(w => !w.Scene.Transitions.Suspended, "the window shown again");
        harness.Read(w =>
        {
            w.Root.Single("Button").SetState(ElementStates.Hover, false);
            w.Scene.Update();
            return true;
        });
        Assert.True(harness.Read(w => w.Scene.Transitions.IsRunning));
    }

    [Fact]
    public void Typing_in_a_text_box_recomposes_surrogate_pairs()
    {
        using var harness = new WindowHarness(typeof(Form), new ApplicationRuntimeSettings { Width = 320, Height = 240 });
        harness.WaitUntil(w => w.FrameCount > 0, "first frame");
        var box = harness.Read(w => w.Scene.GetContentBounds(w.Root.Elements("TextFieldInput").Single(e => e.Id == "free-input")));
        harness.Click(new Point(box.X + 4, box.Y + box.Height / 2));
        harness.WaitUntil(w => w.Input.FocusedTextBox?.Id == "free-input", "focus in the text box");

        const string typed = "Ciao 😀!";
        foreach (var c in typed) harness.Character(c);
        harness.WaitUntil(w => FocusedValue(w) == typed, "typed text");
        for (var i = 0; i < 2; i++)
        {
            harness.Key(0x08, down: true);
            harness.Key(0x08, down: false);
        }
        harness.WaitUntil(w => FocusedValue(w) == "Ciao ", "the emoji deleted as one grapheme");
        Assert.NotNull(harness.Read(w => w.Input.GetTextCaretRect()));
        Assert.Empty(harness.Read(w => w.Exceptions));
    }

    private static string? FocusedValue(NazorWindow window) =>
        window.Input.FocusedTextBox?.Attributes.GetValueOrDefault("value") as string;

    [Fact]
    public void System_clipboard_round_trips_unicode_text()
    {
        Assert.SkipUnless(Environment.GetEnvironmentVariable("NAZOR_CLIPBOARD_TESTS") == "1",
            "Set NAZOR_CLIPBOARD_TESTS=1 to run the system clipboard round trip.");
        using var harness = new WindowHarness(typeof(Form), new ApplicationRuntimeSettings { Width = 320, Height = 240 });
        harness.WaitUntil(w => w.FrameCount > 0, "first frame");
        var saved = harness.Read(w => w.PlatformWindow.Clipboard.GetText());
        const string text = "Nazor è 😀";
        try
        {
            Assert.True(harness.Read(w => w.PlatformWindow.Clipboard.SetText(text)));
            Assert.Equal(text, harness.Read(w => w.PlatformWindow.Clipboard.GetText()));
        }
        finally
        {
            if (saved is not null) harness.Read(w => w.PlatformWindow.Clipboard.SetText(saved));
        }
    }

    [Fact]
    public unsafe void Color_scheme_and_reduced_motion_follow_windows_unless_the_options_say()
    {
        using (var system = new WindowHarness(typeof(Counter), CounterOptions))
        {
            using var personalize = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            var dark = personalize?.GetValue("AppsUseLightTheme") is int light && light == 0;
            var (preferences, reduced, contrast) = system.Read(w => (w.Scene.Styles.Preferences, w.PlatformWindow.PrefersReducedMotion, w.PlatformWindow.PrefersHighContrast));
            Assert.Equal((dark ? ColorScheme.Dark : ColorScheme.Light, reduced, contrast), (preferences.ColorScheme, preferences.ReducedMotion, preferences.ForcedColors));
        }

        using var forced = new WindowHarness(typeof(Counter), CounterOptions with { ColorScheme = ColorScheme.Dark, ReducedMotion = true });
        Assert.Equal((ColorScheme.Dark, true), forced.Read(w => (w.Scene.Styles.Preferences.ColorScheme, w.Scene.Styles.Preferences.ReducedMotion)));
        forced.WaitUntil(w => w.FrameCount > 0, "first frame");
        var frames = forced.Read(w => w.FrameCount);
        forced.Read(w =>
        {
            fixed (char* name = "ImmersiveColorSet")
                ((Win32Window)w.PlatformWindow).OnMessage(forced.Handle, Win32.WM_SETTINGCHANGE, 0, (nint)name);
            return true;
        });
        forced.WaitUntil(w => w.FrameCount > frames, "a frame after the settings changed");
        Assert.Equal((ColorScheme.Dark, true), forced.Read(w => (w.Scene.Styles.Preferences.ColorScheme, w.Scene.Styles.Preferences.ReducedMotion)));
        Assert.Empty(forced.Read(w => w.Exceptions));
    }

    [Fact]
    public unsafe void A_contrast_theme_draws_with_the_system_colors_and_follows_their_changes()
    {
        var colors = new Dictionary<string, Color> { ["Canvas"] = Color.Black, ["CanvasText"] = new Color(255, 255, 0, 255) };
        using var harness = new WindowHarness(typeof(Counter), CounterOptions with { HighContrast = true, SystemColors = colors });
        harness.WaitUntil(w => w.FrameCount > 0, "first frame");
        var (forced, surface, text) = harness.Read(w =>
        {
            var style = w.Scene.Styles.GetStyle(w.Root.Elements().First());
            string Variable(string name) => Nazor.Style.Syntax.StyleToken.Serialize(style.CustomProperties[name]);
            return (w.Scene.Styles.Preferences.ForcedColors, Variable("--nz-surface"), Variable("--nz-on-surface"));
        });
        Assert.True(forced);
        Assert.Equal("#000000", surface);
        Assert.Equal("#ffff00", text);

        var frames = harness.Read(w => w.FrameCount);
        harness.Read(w =>
        {
            ((Win32Window)w.PlatformWindow).OnMessage(harness.Handle, Win32.WM_SYSCOLORCHANGE, 0, 0);
            return true;
        });
        harness.WaitUntil(w => w.FrameCount > frames, "a frame after the system colors changed");
        Assert.Empty(harness.Read(w => w.Exceptions));
    }

    [Fact]
    public unsafe void The_accent_of_windows_colors_the_theme_and_follows_its_changes()
    {
        using (var system = new WindowHarness(typeof(Counter), CounterOptions))
        {
            system.WaitUntil(w => w.FrameCount > 0, "first frame");
            var (colors, contrast, accent) = system.Read(w => (w.PlatformWindow.SystemColors, w.PlatformWindow.PrefersHighContrast,
                Nazor.Style.Syntax.StyleToken.Serialize(w.Scene.Styles.GetStyle(w.Root.Elements().First()).CustomProperties["--nz-accent"])));
            using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\Accent");
            if (!contrast && key?.GetValue("AccentPalette") is byte[] { Length: 32 } palette)
            {
                Color Shade(int index) => new(palette[index * 4], palette[index * 4 + 1], palette[index * 4 + 2], 255);
                Assert.Equal((Shade(3), Shade(0), Shade(1), Shade(4), Shade(6)),
                    (colors["AccentColor"], colors["AccentLight3"], colors["AccentLight2"], colors["AccentDark1"], colors["AccentDark3"]));
                Assert.False(colors.ContainsKey("AccentColorText"));
                var shade = Shade(3);
                Assert.Equal($"#{shade.R:x2}{shade.G:x2}{shade.B:x2}", accent);
            }

            var frames = system.Read(w => w.FrameCount);
            system.Read(w =>
            {
                ((Win32Window)w.PlatformWindow).OnMessage(system.Handle, Win32.WM_DWMCOLORIZATIONCOLORCHANGED, 0, 0);
                return true;
            });
            system.WaitUntil(w => w.FrameCount > frames, "a frame after the accent changed");
            Assert.Empty(system.Read(w => w.Exceptions));
        }

        var yellow = new Dictionary<string, Color> { ["AccentColor"] = new Color(0xff, 0xb9, 0x00, 255) };
        using var options = new WindowHarness(typeof(Counter), CounterOptions with { SystemColors = yellow });
        var colorsOfOptions = options.Read(w => w.Scene.Styles.Preferences.Colors!);
        Assert.Equal((Nazor.Style.Color.FromRgb(0xff, 0xb9, 0x00), Nazor.Style.Color.Black), (colorsOfOptions.AccentColor, colorsOfOptions.AccentColorText));
    }

    [Fact]
    public void Partial_backend_palettes_follow_the_requested_appearance_and_keep_semantic_roles()
    {
        var colors = new Dictionary<string, Color>
        {
            ["AccentColor"] = new Color(0x10, 0x7c, 0x10, 255),
            ["Tooltip"] = Color.Black, ["TooltipText"] = Color.White,
        };
        using var harness = new WindowHarness(typeof(Counter), CounterOptions with { ColorScheme = ColorScheme.Dark, HighContrast = false, SystemColors = colors });
        var dark = harness.Read(w => w.Scene.Styles.Preferences.Colors!);
        Assert.Equal(Nazor.Style.SystemColors.Resolve(scheme: ColorScheme.Dark, profile: SystemColorProfile.Windows).Canvas, dark.Canvas);
        Assert.Equal(Nazor.Style.Color.Black, dark["Tooltip"]);
        Assert.Equal(Nazor.Style.Color.White, dark["TooltipText"]);
        Assert.True(dark.AccentLight1.G > dark.AccentColor.G);
        harness.Read(w => { w.SetColorScheme(ColorScheme.Light); return true; });
        var light = harness.Read(w => w.Scene.Styles.Preferences.Colors!);
        Assert.NotEqual(dark.Canvas, light.Canvas);
        Assert.Equal(dark.AccentColor, light.AccentColor);
        Assert.Equal(dark["Tooltip"], light["Tooltip"]);
    }

    [Fact]
    public void An_image_decoded_off_the_ui_thread_asks_for_a_frame()
    {
        var path = Path.Combine(Path.GetTempPath(), $"nazor-window-{Guid.NewGuid():N}.png");
        using (var skia = new Nazor.Skia.SkiaRenderer())
            File.WriteAllBytes(path, skia.EncodePng(new RenderedImage(30, 12, Enumerable.Repeat((byte)200, 30 * 12 * 4).ToArray())));
        try
        {
            Picture.Path = path;
            using var harness = new WindowHarness(typeof(Picture), CounterOptions);
            harness.WaitUntil(w => w.Scene.GetBounds(w.Root.Single("Image")).Width == 30, "the image laid out at its natural size");
            Assert.Empty(harness.Read(w => w.Exceptions));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Resize_updates_viewport_and_layout()
    {
        using var harness = new WindowHarness(typeof(Counter), CounterOptions);
        harness.WaitUntil(w => w.FrameCount > 0, "first frame");
        harness.Read(w =>
        {
            w.PlatformWindow.Resize(new Size(500, 260));
            return true;
        });
        harness.WaitUntil(w => Math.Abs(w.Scene.Viewport.Width - 500) < 1, "the new viewport");
        var frames = harness.Read(w => w.FrameCount);
        harness.WaitUntil(w => w.FrameCount > frames || w.Scene.GetBounds(w.Root.Single("Container")).Width > 490, "a frame at the new size");
        harness.WaitUntil(w => Math.Abs(w.Scene.GetBounds(w.Root.Single("Container")).Width - w.Scene.Viewport.Width) < 0.01f, "container stretched to the new width");
        Assert.Empty(harness.Read(w => w.Exceptions));
    }

    [Fact]
    public void Closing_disposes_components_and_repeated_windows_do_not_leak_native_objects()
    {
        using (var warmUp = new WindowHarness(typeof(Counter), CounterOptions))
            warmUp.WaitUntil(w => w.FrameCount > 0, "first frame");
        var before = WindowHarness.GuiResources();

        for (var i = 0; i < 5; i++)
        {
            var harness = new WindowHarness(typeof(Counter), CounterOptions);
            harness.WaitUntil(w => w.FrameCount > 0, "first frame");
            var renderer = harness.Read(w => w.Renderer);
            harness.Close();
            Assert.True(harness.HasExited);
            Assert.Equal(0, renderer.ComponentCount);
        }

        var after = WindowHarness.GuiResources();
        Assert.True(after.Gdi <= before.Gdi + 2, $"GDI objects {before.Gdi} -> {after.Gdi}");
        Assert.True(after.User <= before.User + 2, $"USER objects {before.User} -> {after.User}");
    }
}

[CollectionDefinition("Windows", DisableParallelization = true)]
public sealed class WindowsCollection;
