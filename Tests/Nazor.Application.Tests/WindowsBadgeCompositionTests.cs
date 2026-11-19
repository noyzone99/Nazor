using Nazor.Graphics;
using Nazor.Graphics.Text;
using Nazor.Nss;
using Nazor.Platform;
using Nazor.Platform.Windows;
using Nazor.Skia;

namespace Nazor.Application.Tests;

public sealed partial class WindowsLauncherTests
{
    [Fact]
    public void Badge_image_math_is_resolved_after_fitting_including_negative_free_space()
    {
        using var harness = new WindowHarness(typeof(SwitchableWindow));
        foreach (var (expression, basis, expected) in new (string, float, float)[]
        {
            ("min(50%, 2px)", 8, 2), ("max(50%, 2px)", 8, 4), ("clamp(1px, 50%, 3px)", 8, 3),
            ("calc(min(50%, 2px) + 1px)", 8, 3), ("calc(50% + 1px)", 8, 5),
            ("min(50%, 2px)", -16, -8), ("max(50%, -2px)", -16, -2), ("clamp(-3px, 50%, 2px)", -16, -3),
        })
        {
            SetStyle($"-windows-badge-image-position: {expression} top;");
            harness.WaitUntil(window => ((Win32Window)window.PlatformWindow).BadgeAppearance.ImagePositionResolver?.Invoke(new(basis, 0)).X == expected, expression);
        }
        SetStyle("-windows-badge-image-position: min(50%, 2px) top; -windows-badge-background-color: blue; -windows-badge-border-radius: 0;");
        harness.WaitUntil(window => ((Win32Window)window.PlatformWindow).BadgeAppearance.BackgroundColor == new Color(0, 0, 255, 255), "image math style");
        var appearance = harness.Read(window => ((Win32Window)window.PlatformWindow).BadgeAppearance);
        foreach (var scale in new[] { 1f, 1.25f, 1.5f, 2f, 3f, 4f })
        {
            var width = (int)(8 * scale);
            var height = (int)(16 * scale);
            var image = new RenderedImage(width, height, Enumerable.Range(0, width * height).SelectMany(_ => new byte[] { 0, 0, 255, 255 }).ToArray());
            var rendered = WindowsBadgeRenderer.Render(null, image, appearance, height);
            Assert.Equal((0, 0, 255, 255), Pixel(rendered, 0, height / 2));
            Assert.Equal((255, 0, 0, 255), Pixel(rendered, (int)Math.Ceiling(3 * scale), height / 2));
        }
        void SetStyle(string declarations) => harness.Read(window =>
        {
            var sheet = NssCompiler.Compile("badge math", $":root {{ {declarations} }}");
            Assert.True(sheet.Success, string.Join("\n", sheet.Diagnostics));
            window.Scene.Styles.Application = sheet.StyleSheet;
            window.RequestFrame();
            return true;
        });
    }

    [Fact]
    public void Badge_uses_private_fonts_and_refreshes_after_registration_without_republishing_equal_styles()
    {
        WindowsPlatformHost.RunOnUiThread(() =>
        {
            using var fonts = new SkiaFontProvider(new FontOptions { UseSystemFonts = false });
            var style = new WindowsBadgeAppearance { Fonts = fonts, FontFamilies = ["Fira Sans"], BackgroundColor = default(Color), FontSize = 12, FontWeight = 400 };
            var before = WindowsBadgeRenderer.Render(88, null, style, 64);
            fonts.Register(Path.Combine(AppContext.BaseDirectory, "Fonts", "FiraSans-Regular.ttf"));
            Assert.Equal("Fira Sans", fonts.Match(["Fira Sans"], 400, false).FamilyName);
            var after = WindowsBadgeRenderer.Render(88, null, style with { FontVersion = fonts.Version }, 64);
            Assert.NotEqual(before.Pixels, after.Pixels);
            var fallback = WindowsBadgeRenderer.Render(88, null, style with { FontFamilies = ["Missing badge family", "Fira Sans"] }, 64);
            Assert.Equal(after.Pixels, fallback.Pixels);
            using var platform = new Win32Platform();
            var calls = 0;
            using var launcher = new Win32ApplicationLauncher((_, _, _) => { calls++; return true; });
            using var window = new Win32Window(new Host(), new("Badge font registration", 100, 100), launcher: launcher);
            window.Show(false);
            window.OnMessage(window.Handle, Win32ApplicationLauncher.TaskbarButtonCreated, 0, 0);
            window.SetWindowsBadgeAppearance(style with { FontVersion = fonts.Version });
            Assert.True(launcher.SetBadgeCount(88));
            var previousCalls = calls;
            window.SetWindowsBadgeAppearance(style with { FontVersion = fonts.Version });
            Assert.Equal(previousCalls, calls);
            fonts.Register(Path.Combine(AppContext.BaseDirectory, "Fonts", "FiraSans-Bold.ttf"));
            window.SetWindowsBadgeAppearance(style with { FontVersion = fonts.Version });
            Assert.True(calls > previousCalls);
            return 0;
        });
    }

    [Theory]
    [InlineData(16)]
    [InlineData(20)]
    [InlineData(24)]
    [InlineData(32)]
    [InlineData(48)]
    [InlineData(64)]
    public void Badge_gradients_independent_borders_transforms_and_alpha_masks_compose_at_each_scale(int size)
    {
        var bounds = new Rect(0, 0, 16, 16);
        var background = new DisplayList(new(16, 16), [new FillGradient(bounds, default,
            new LinearGradientBrush(new(0, 0), new(16, 0), [new(0, new(255, 0, 0, 255)), new(1, new(0, 0, 255, 255))]))]);
        var style = new WindowsBadgeAppearance { Background = background, BackgroundColor = default(Color), Radius = default, FontSize = 0,
            BorderWidths = new(2, 1, 3, 4), BorderTop = new(new(0, 255, 0, 255)), BorderRight = new(new(255, 255, 255, 255)),
            BorderBottom = new(new(255, 255, 0, 255)), BorderLeft = new(new(0, 255, 255, 255)) };
        var result = WindowsBadgeRenderer.Render(1, null, style, size);
        var scale = size / 16f;
        Assert.Equal((0, 255, 0, 255), Pixel(result, size / 2, 0));
        Assert.Equal((0, 255, 255, 255), Pixel(result, 0, size / 2));
        Assert.Equal((255, 255, 255, 255), Pixel(result, size - 1, size / 2));
        Assert.Equal((255, 255, 0, 255), Pixel(result, size / 2, size - 1));
        Assert.True(Pixel(result, (int)(4 * scale), size / 2).R > Pixel(result, (int)(11 * scale), size / 2).R);
        var mask = new DisplayList(new(16, 16), [new FillRectangle(new(0, 0, 8, 16), default, new(255, 255, 255, 255))]);
        var masked = WindowsBadgeRenderer.Render(1, null, style with { Mask = mask, Transform = Matrix.Rotation(180).Then(Matrix.Translation(16, 16)), Opacity = .5f }, size);
        Assert.Equal(0, Pixel(masked, 0, size / 2).A);
        Assert.Equal(128, Pixel(masked, size - 1, size / 2).A);
        var hidden = WindowsBadgeRenderer.Render(1, null, style with { Mask = new(new(16, 16), []) }, size);
        Assert.All(hidden.Pixels, value => Assert.Equal(0, value));
    }

    [Fact]
    public void Badge_count_uses_application_glyph_fallback_when_the_preferred_font_lacks_digits()
    {
        using var fonts = new SkiaFontProvider(new FontOptions { UseSystemFonts = false });
        fonts.Register(Path.Combine(AppContext.BaseDirectory, "Fonts", "NazorLeafIcons.otf"));
        var face = fonts.Match(["Missing family"], 400, false);
        Assert.False(face.HasGlyph('8'));
        var style = new WindowsBadgeAppearance { Fonts = fonts, FontFamilies = [face.FamilyName], BackgroundColor = default(Color), FontSize = 12, FontWeight = 400 };
        var missing = WindowsBadgeRenderer.Render(88, null, style, 64);
        fonts.Register(Path.Combine(AppContext.BaseDirectory, "Fonts", "FiraSans-Regular.ttf"));
        Assert.Equal("Fira Sans", fonts.Fallback('8', face)!.FamilyName);
        var fallback = WindowsBadgeRenderer.Render(88, null, style, 64);
        Assert.NotEqual(missing.Pixels, fallback.Pixels);
        Assert.Contains(fallback.Pixels.Where((_, index) => index % 4 == 3), alpha => alpha != 0);
    }

    [Fact]
    public void Computed_badge_layers_load_SVG_masks_use_application_fonts_and_retain_equal_snapshots()
    {
        using var harness = new WindowHarness(typeof(SwitchableWindow));
        harness.Read(window =>
        {
            var sheet = NssCompiler.Compile("extended badge", """
                :root {
                    -windows-badge-background: linear-gradient(to right, red, blue) 0 0 / 100% 100% no-repeat;
                    -windows-badge-border: 1px solid white;
                    -windows-badge-border-left: 2px dashed green;
                    -windows-badge-transform: rotate(20deg) scale(.8);
                    -windows-badge-mask-image: url(Assets/launcher-mask.svg);
                }
                """);
            Assert.True(sheet.Success, string.Join("\n", sheet.Diagnostics));
            window.Scene.Styles.Application = sheet.StyleSheet;
            window.RequestFrame();
            return true;
        });
        harness.WaitUntil(window => ((Win32Window)window.PlatformWindow).BadgeAppearance.Mask?.Commands.Count > 0, "SVG badge mask");
        harness.Read(window =>
        {
            var appearance = ((Win32Window)window.PlatformWindow).BadgeAppearance;
            Assert.Same(window.Scene.Fonts, appearance.Fonts);
            Assert.Equal(2, appearance.BorderWidths!.Value.Left);
            Assert.Equal(LineStyle.Dashed, appearance.BorderLeft!.Value.Style);
            Assert.False(appearance.Transform.IsIdentity);
            Assert.Contains(appearance.Background!.Commands, command => command is FillGradient);
            window.RequestFrame();
            return true;
        });
        var previous = harness.Read(window => ((Win32Window)window.PlatformWindow).BadgeAppearance);
        harness.Read(window => { window.RequestFrame(); return true; });
        harness.Read(window =>
        {
            var current = ((Win32Window)window.PlatformWindow).BadgeAppearance;
            Assert.Same(previous.Background, current.Background);
            Assert.Same(previous.Mask, current.Mask);
            Assert.Same(previous.ImagePositionResolver, current.ImagePositionResolver);
            Assert.Empty(window.Exceptions);
            return true;
        });
    }
}
