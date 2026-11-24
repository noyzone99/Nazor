using Nazor.Graphics.Text;
using Nazor.Input;
using Nazor.Skia;
using Nazor.Style;
using Nazor.Testing;
using Nazor.Themes.NazorLeaf;
using GalleryApp = Nazor.Samples.Gallery.App;

namespace Nazor.Application.Tests;

[Collection("Windows")]
[System.Runtime.Versioning.SupportedOSPlatform("windows")]
public sealed class NazorLeafSamplesTests
{
    [Fact]
    public void Every_gallery_theme_preserves_the_search_caption_and_its_editor()
    {
        using var harness = new ApplicationHarness(() => ApplicationTestStartup.Run<GalleryApp>(Settings(GalleryApp.LeafTheme)));
        var window = harness.Window<GalleryApp>();
        try
        {
            WaitForFrame(window);
            var editor = ApplicationHarness.On(window, () => window.Root.Elements().Single(e => e.Id == "gallery-caption-search-input"));
            ApplicationHarness.On(window, () =>
            {
                window.Input.Focus(editor, visible: false);
                window.Input.TextInput("button");
                return true;
            });
            foreach (var theme in new[] { "Basic", "Fluent", "NazorLeaf", "None" })
            {
                ApplicationHarness.On(window, () =>
                {
                    window.Input.Activate(window.Root.Elements().Single(e => e.Id == "gallery-theme"));
                    window.Scene.Update();
                    window.Input.Activate(window.Root.Elements("SelectOption").Single(e => e.Text() == theme));
                    window.Scene.Update();
                    Assert.Same(editor, window.Root.Elements().Single(e => e.Id == "gallery-caption-search-input"));
                    Assert.Equal("button", editor.Attributes.GetValueOrDefault("value"));
                    Assert.Equal(WindowChromeOwner.User, window.Chrome.TitleBar);
                    Assert.DoesNotContain(window.Root.Elements(), e => e.Id == "gallery-search");
                    var aligned = theme is "NazorLeaf" or "Fluent";
                    var title = window.Root.Single("TitleBarTitle");
                    var mark = window.Root.Elements().Single(e => e.Class == "titlebar-mark");
                    Assert.Equal(aligned ? 16 : 12, window.Scene.GetEffectiveValue(title, StyleProperties.FontSize).Value);
                    Assert.Equal(aligned ? 32 : 16, window.Scene.GetEffectiveValue(mark, StyleProperties.Width).Length.Value);
                    var field = window.Scene.GetBounds(window.Root.Elements().Single(e => e.Id == "gallery-caption-search"));
                    Assert.True(field.Width > 150);
                    Assert.True(field.Height >= 26);
                    if (aligned)
                    {
                        var markBounds = window.Scene.GetBounds(mark);
                        Assert.Equal(14, window.Scene.GetEffectiveValue(editor, StyleProperties.FontSize).Value);
                        Assert.InRange(window.Scene.GetBounds(title).X - markBounds.Right, 11, 13);
                        Assert.InRange(field.X - markBounds.X, 254, 258);
                        Assert.InRange(field.Height, 31, 33);
                    }
                    var point = new Nazor.Graphics.Point(field.X + 2, field.Y + field.Height / 2);
                    Assert.Equal(WindowRegion.Client, window.Input.RegionAt(point));
                    window.Input.PointerPressed(point, PointerButton.Primary, KeyModifiers.None);
                    window.Input.PointerReleased(point, PointerButton.Primary, KeyModifiers.None);
                    Assert.Same(editor, window.Input.Focused);
                    Assert.Empty(window.Exceptions);
                    return true;
                });
            }
        }
        finally { Close(window); }
        Assert.True(harness.WaitForExit());
    }

    [Fact]
    public void The_gallery_previews_the_slider_value_and_restyles_the_window_on_commit()
    {
        using var harness = new ApplicationHarness(() => ApplicationTestStartup.Run<GalleryApp>(Settings(GalleryApp.LeafTheme)));
        var window = harness.Window<GalleryApp>();
        try
        {
            WaitForFrame(window);
            OpenShowcase(window);
            var (slider, originalStyle) = ApplicationHarness.On(window, () =>
                (window.Root.Elements("Slider").First(), window.Style));
            var value = new ChangeEventArgs { Value = 75d };
            ApplicationHarness.Result(ApplicationHarness.On(window, async () =>
            {
                await window.Renderer.DispatchEventAsync(slider, "oninput", value);
                Assert.Equal(75d, slider.Attributes.GetValueOrDefault("range-value"));
                Assert.Equal(originalStyle, window.Style);
                await window.Renderer.DispatchEventAsync(slider, "onchange", value);
                Assert.NotEqual(originalStyle, window.Style);
                var radius = window.Root.Elements("Slider").Single(element => element.Attributes.GetValueOrDefault("accessible-name") is "Raggio della foglia");
                var radiusValue = new ChangeEventArgs { Value = 18d };
                await window.Renderer.DispatchEventAsync(radius, "oninput", radiusValue);
                await window.Renderer.DispatchEventAsync(radius, "onchange", radiusValue);
                Assert.Contains("--leaf-r-ctl: 18px 3px 18px 3px", window.Style);
                Assert.Contains("--leaf-r-ovl: 26px 4px 26px 4px", window.Style);
                Assert.Contains("--leaf-r-pane: 36px 6px 36px 6px", window.Style);
                Assert.Contains("--leaf-r-win: 46px 8px 46px 8px", window.Style);
                Assert.Equal(WindowDecorations.Native(WindowTitleBarContent.User, WindowCaptionButtons.Native), window.AppliedDecorations!.Configuration);
                return true;
            }));
        }
        finally { Close(window); }
        Assert.True(harness.WaitForExit());
    }

    [Fact]
    public void The_native_gallery_scrolls_to_the_feedback_from_its_rail()
    {
        using var harness = new ApplicationHarness(() => ApplicationTestStartup.Run<GalleryApp>(Settings(GalleryApp.LeafTheme)));
        var window = harness.Window<GalleryApp>();
        try
        {
            WaitForFrame(window);
            ApplicationHarness.On(window, () =>
            {
                var root = window.Root.Single("Window");
                Assert.Equal(WindowDecorations.Native(WindowTitleBarContent.User, WindowCaptionButtons.Native), window.AppliedDecorations!.Configuration);
                Assert.Equal(BackgroundMode.Default, window.BackgroundMode);
                Assert.Equal(CommonBackdrop.None, window.Backdrop);
                Assert.DoesNotContain(window.Root.Elements(), element => element.Id == "gallery-glass");
                Assert.Equal(255, window.Scene.GetEffectiveValue(root, StyleProperties.BackgroundColor).A);
                return true;
            });
            // Every theme retains the application search caption; Fluent adds Mica.
            var captioned = WindowDecorations.Native(WindowTitleBarContent.User, WindowCaptionButtons.Native);
            foreach (var theme in new[] { "Basic", "Fluent", "NazorLeaf" })
            {
                var fluent = theme == "Fluent";
                ApplicationHarness.On(window, () =>
                {
                    Assert.True(window.Input.Activate(window.Root.Elements("Select").Single(element => element.Id == "gallery-theme")));
                    window.Scene.Update();
                    Assert.True(window.Input.Activate(window.Root.Elements("SelectOption").Single(element => element.Text() == theme)));
                    window.Scene.Update();
                    return true;
                });
                Assert.True(SpinWait.SpinUntil(() => ApplicationHarness.On(window, () =>
                {
                    window.Scene.Update();
                    return window.AppliedDecorations?.Configuration == captioned
                        && window.Root.Elements().Any(element => element.Id == "gallery-caption-search");
                }), TimeSpan.FromSeconds(5)), theme);
                ApplicationHarness.On(window, () =>
                {
                    Assert.Equal(BackgroundMode.Default, window.BackgroundMode);
                    Assert.Equal(fluent ? CommonBackdrop.Solid : CommonBackdrop.None, window.Backdrop.Preset);
                    Assert.Equal(WindowChromeOwner.User, window.Chrome.TitleBar);
                    Assert.DoesNotContain(window.Root.Elements(), element => element.Id == "gallery-search");
                    if (!fluent || !window.AppliedBackdrop.IsActive)
                        Assert.Equal(255, window.Scene.GetEffectiveValue(window.Root.Single("Window"), StyleProperties.BackgroundColor).A);
                    Assert.Empty(window.Exceptions);
                    return true;
                });
                if (fluent) Save(window, "gallery-fluent");
            }
            Save(window, "gallery");
            OpenShowcase(window);
            ApplicationHarness.On(window, () =>
            {
                var rail = window.Root.Elements().Single(element => element.Class == "demo-rail");
                var feedback = rail.Elements().Single(element => element.Name == "Button" && element.Elements().Any(text => text.Name == "TextBlock" && text.Text() == "Feedback"));
                Assert.True(window.Input.Activate(feedback));
                window.Scene.Update();
                var progress = window.Root.Elements("ProgressBar").Single(element => element.Attributes.GetValueOrDefault("range-value") is 64d);
                var pane = window.Root.Elements("ScrollView").Single(element => element.Class == "demo-scroll");
                var visible = window.Scene.GetBounds(pane);
                Assert.InRange(window.Scene.GetBounds(progress).Y, visible.Y, visible.Bottom);
                Assert.Empty(window.Exceptions);
                return true;
            });
            Save(window, "feedback");
            ApplicationHarness.On(window, () =>
            {
                Assert.True(window.Input.Activate(window.Root.Elements("Button").Single(element => element.Text() == "Back to catalog")));
                Assert.Contains(window.Root.Elements("Button"), element => element.Id == "gallery-showcase");
                return true;
            });
        }
        finally { Close(window); }
        Assert.True(harness.WaitForExit());
    }

    private static ApplicationRuntimeSettings Settings(StyleSheet theme) => new()
    {
        Activate = false,
        Theme = theme,
        ColorScheme = ColorScheme.Light,
        HighContrast = false,
        ReducedMotion = true,
        Fonts = new FontOptions { PrivateFontFiles = [.. NazorLeafTheme.FontFiles.Select(file => Path.Combine(AppContext.BaseDirectory, file))] },
    };

    private static void WaitForFrame(NazorWindow window) =>
        Assert.True(SpinWait.SpinUntil(() => ApplicationHarness.On(window, () => window.FrameCount > 0), TimeSpan.FromSeconds(5)));

    private static void OpenShowcase(NazorWindow window) => ApplicationHarness.On(window, () =>
    {
        Assert.True(window.Input.Activate(window.Root.Elements("Button").Single(element => element.Id == "gallery-showcase")));
        window.Scene.Update();
        return true;
    });

    private static void Save(NazorWindow window, string name)
    {
        var images = ApplicationHarness.On(window, () =>
        {
            window.Scene.BuildDisplayList();
            return window.Scene.WhenImagesLoaded();
        });
        images.WaitAsync(TimeSpan.FromSeconds(10)).GetAwaiter().GetResult();
        var frame = ApplicationHarness.Result(ApplicationHarness.On(window, window.CaptureFrameAsync));
        var directory = Path.GetFullPath("../../../../../artifacts/nazorleaf-previews", AppContext.BaseDirectory);
        Directory.CreateDirectory(directory);
        using var renderer = new SkiaRenderer();
        File.WriteAllBytes(Path.Combine(directory, name + ".png"), renderer.EncodePng(frame));
    }

    private static void Close(NazorWindow window) => ApplicationHarness.On(window, () => { window.Close(); return true; });
}
