using Nazor.Graphics.Text;
using Nazor.Skia;
using Nazor.Style;
using Nazor.Testing;
using Nazor.Themes.Adwaita;
using Nazor.Themes.NazorLeaf;
using GalleryApp = Nazor.Samples.Gallery.App;

namespace Nazor.Application.Tests;

[Collection("Windows")]
[System.Runtime.Versioning.SupportedOSPlatform("windows")]
public sealed class GalleryThemePreviewTests
{
    // Opt-in review captures of the desktop-inspired themes; set NAZOR_THEME_PREVIEWS=1 to write artifacts/theme-previews.
    [Fact]
    public void Desktop_themes_render_the_gallery_with_their_own_frame()
    {
        Assert.SkipUnless(Environment.GetEnvironmentVariable("NAZOR_THEME_PREVIEWS") == "1", "Set NAZOR_THEME_PREVIEWS=1 to write theme previews.");
        using var harness = new ApplicationHarness(() => ApplicationTestStartup.Run<GalleryApp>(Settings()));
        var window = harness.Window<GalleryApp>();
        try
        {
            WaitForFrame(window);
            var only = Environment.GetEnvironmentVariable("NAZOR_THEME_PREVIEWS_ONLY");
            var themes = new[] { "Windows 9x", "Windows XP", "Windows 7", "CDE", "Adwaita", "Breeze" }
                .Where(theme => string.IsNullOrEmpty(only) || only.Split(',').Contains(theme)).ToArray();
            foreach (var theme in themes)
            {
                foreach (var scheme in theme is "Adwaita" or "Breeze" or "CDE" ? new[] { "Light", "Dark" } : ["Light"])
                {
                    Choose(window, "gallery-theme", theme);
                    Choose(window, "gallery-scheme", scheme);
                    Settle(window);
                    var name = $"{theme.Replace(' ', '-').ToLowerInvariant()}-{scheme.ToLowerInvariant()}";
                    Click(window, "gallery-home");
                    Settle(window);
                    Save(window, name + "-home");
                    Click(window, "nav-button");
                    Settle(window);
                    Save(window, name + "-button");
                    Click(window, "nav-text-field");
                    Settle(window);
                    Save(window, name + "-fields");
                    Click(window, "nav-tabs");
                    Settle(window);
                    Save(window, name + "-tabs");
                    ApplicationHarness.On(window, () => { Assert.Empty(window.Exceptions); return true; });
                }
            }
        }
        finally { Close(window); }
        Assert.True(harness.WaitForExit());
    }

    private static ApplicationRuntimeSettings Settings() => new()
    {
        Activate = false,
        Theme = GalleryApp.LeafTheme,
        ColorScheme = ColorScheme.Light,
        HighContrast = false,
        ReducedMotion = true,
        Fonts = new FontOptions
        {
            PrivateFontFiles = [.. NazorLeafTheme.FontFiles.Concat(AdwaitaTheme.FontFiles).Concat(Nazor.Themes.Fluent.Themes.FontFiles).Select(file => Path.Combine(AppContext.BaseDirectory, file))],
        },
    };

    private static void Choose(NazorWindow window, string select, string option) => ApplicationHarness.On(window, () =>
    {
        window.Input.Activate(window.Root.Elements().Single(e => e.Id == select));
        window.Scene.Update();
        window.Input.Activate(window.Root.Elements("SelectOption").Single(e => e.Text() == option));
        window.Scene.Update();
        return true;
    });

    private static void Click(NazorWindow window, string id) => ApplicationHarness.On(window, () =>
    {
        var target = window.Root.Elements().FirstOrDefault(e => e.Id == id);
        if (target is not null) window.Input.Activate(target);
        window.Scene.Update();
        return true;
    });

    private static void Settle(NazorWindow window)
    {
        // Captures show the active window, as a reviewer sees the focused application.
        ApplicationHarness.On(window, () =>
        {
            ((Nazor.Platform.IWindowHost)window).OnActivationChanged(false);
            ((Nazor.Platform.IWindowHost)window).OnActivationChanged(true);
            return true;
        });
        // Decoration changes and theme fonts settle over a few frames.
        var frames = ApplicationHarness.On(window, () => window.FrameCount);
        SpinWait.SpinUntil(() => ApplicationHarness.On(window, () => window.FrameCount) > frames + 2, TimeSpan.FromSeconds(2));
        Thread.Sleep(150);
    }

    private static void WaitForFrame(NazorWindow window) =>
        Assert.True(SpinWait.SpinUntil(() => ApplicationHarness.On(window, () => window.FrameCount > 0), TimeSpan.FromSeconds(5)));

    private static void Save(NazorWindow window, string name)
    {
        var images = ApplicationHarness.On(window, () =>
        {
            window.Scene.Update();
            window.Scene.BuildDisplayList();
            return window.Scene.WhenImagesLoaded();
        });
        images.WaitAsync(TimeSpan.FromSeconds(10)).GetAwaiter().GetResult();
        var frame = ApplicationHarness.Result(ApplicationHarness.On(window, window.CaptureFrameAsync));
        var directory = Path.GetFullPath("../../../../../artifacts/theme-previews", AppContext.BaseDirectory);
        Directory.CreateDirectory(directory);
        using var renderer = new SkiaRenderer();
        File.WriteAllBytes(Path.Combine(directory, name + ".png"), renderer.EncodePng(frame));
    }

    private static void Close(NazorWindow window) => ApplicationHarness.On(window, () => { window.Close(); return true; });
}
