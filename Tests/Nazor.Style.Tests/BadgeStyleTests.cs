using Nazor.Nss;
using Nazor.Testing;

namespace Nazor.Style.Tests;

public sealed class BadgeStyleTests
{
    private const string Source = """
        * {
            color: #123456; font-size: 20px; --badge: blue;
            -windows-badge-background-color: var(--badge);
            -windows-badge-background-image: linear-gradient(to right, currentColor, blue), url(icon.png);
            -windows-badge-background-position: min(50%, 2px) top, center;
            -windows-badge-background-size: 8px 100%, contain;
            -windows-badge-background-repeat: repeat-x, no-repeat;
            -windows-badge-background-clip: padding-box;
            -windows-badge-color: currentColor;
            -windows-badge-size: 0.7em;
            -windows-badge-width: 80%; -windows-badge-height: auto;
            -windows-badge-offset-x: -1px; -windows-badge-offset-y: 2%;
            -windows-badge-padding: 1px 2px 3px 4px;
            -windows-badge-border: 1px dashed currentColor;
            -windows-badge-border-left: 2px dotted red;
            -windows-badge-border-radius: 1px 2px 3px 4px / 5px 6px 7px 8px;
            -windows-badge-opacity: .8;
            -windows-badge-font-family: "Segoe UI", sans-serif;
            -windows-badge-font-size: 70%; -windows-badge-font-weight: 600;
            -windows-badge-font-style: italic; -windows-badge-letter-spacing: -0.2px;
            -windows-badge-text-align: right; -windows-badge-vertical-align: bottom;
            -windows-badge-text-fit: none; -windows-badge-text-stroke-width: .2px;
            -windows-badge-text-stroke-color: red;
            -windows-badge-box-shadow: 1px 2px 3px currentColor, inset 0 0 1px blue;
            -windows-badge-text-shadow: 0 1px 1px currentColor;
            -windows-badge-image-background-color: white;
            -windows-badge-image-fit: cover;
            -windows-badge-image-position: calc(50% + 1px) top;
            -windows-badge-image-opacity: .5; -windows-badge-image-clip: border-box;
            -windows-badge-transform: translate(10%, 1px) rotate(10deg) scale(.9);
            -windows-badge-transform-origin: left top;
            -windows-badge-mask-image: url(mask.svg);
        }
        """;

    [Fact]
    public void Badge_properties_roundtrip_generate_code_and_resolve_only_on_the_root_window()
    {
        var compiled = NssCompiler.Compile("badges", Source);
        Assert.True(compiled.Success, string.Join("\n", compiled.Diagnostics));
        var sheet = compiled.StyleSheet;
        var restored = StyleSheetJson.Deserialize(StyleSheetJson.Serialize(sheet));
        Assert.Equal(sheet.Describe(), restored.Describe());
        var emitted = CSharpEmitter.Emit(restored);
        foreach (var property in StyleProperties.All.Where(p => p.Name.StartsWith("-windows-badge-", StringComparison.Ordinal)))
        {
            Assert.False(property.Inherited);
            Assert.False(property.Animatable);
            Assert.False(StyleProperties.AppliesTo(property, "Window", false));
            Assert.Contains("StyleProperties.WindowsBadge", emitted);
        }
        using var host = new TestHost();
        using var styles = new StyleEngine(host.Renderer) { Application = restored };
        var root = host.Render<PlatformStylePage>();
        var window = styles.GetStyle(root.Elements("Window").Single());
        Assert.Equal(new NativeColor(Color.FromRgb(0, 0, 255)), window.Get(StyleProperties.WindowsBadgeBackgroundColor));
        Assert.Equal(Color.FromRgb(0x12, 0x34, 0x56), window.Get(StyleProperties.WindowsBadgeColor));
        Assert.Equal(Length.Px(14), window.Get(StyleProperties.WindowsBadgeSize));
        Assert.Equal(Length.Px(4), window.Get(StyleProperties.WindowsBadgePaddingLeft));
        Assert.Equal(Length.Px(2), window.Get(StyleProperties.WindowsBadgeBorderLeftWidth));
        Assert.Equal(BorderStyle.Dotted, window.Get(StyleProperties.WindowsBadgeBorderLeftStyle));
        Assert.Equal(Color.FromRgb(255, 0, 0), window.Get(StyleProperties.WindowsBadgeBorderLeftColor));
        Assert.Equal(Length.Px(1), window.Get(StyleProperties.WindowsBadgeBorderTopWidth));
        Assert.Equal(2, window.Get(StyleProperties.WindowsBadgeBackgroundImage).Items.Count);
        Assert.Equal(3, window.Get(StyleProperties.WindowsBadgeTransform).Items.Count);
        Assert.IsType<UrlImage>(Assert.Single(window.Get(StyleProperties.WindowsBadgeMaskImage).Items));
        Assert.Equal(new RadiusValue(Length.Px(3), Length.Px(7)), window.Get(StyleProperties.WindowsBadgeBorderBottomRightRadius));
        Assert.Equal(Color.FromRgb(0x12, 0x34, 0x56), window.Get(StyleProperties.WindowsBadgeBoxShadow).Items[0].Color);
        Assert.Equal(Color.FromRgb(0x12, 0x34, 0x56), window.Get(StyleProperties.WindowsBadgeTextShadow).Items[0].Color);
        foreach (var name in new[] { "TitleBar", "Button" })
        {
            var other = styles.GetStyle(root.Elements(name).Single());
            Assert.Equal(NativeColor.Auto, other.Get(StyleProperties.WindowsBadgeBackgroundColor));
            Assert.Equal(Length.Px(16), other.Get(StyleProperties.WindowsBadgeSize));
        }
        styles.Application = NssCompiler.Compile("updated", ":root { -windows-badge-size: 12px; }").StyleSheet;
        Assert.Equal(Length.Px(12), styles.GetStyle(root.Elements("Window").Single()).Get(StyleProperties.WindowsBadgeSize));
    }

    [Theory]
    [InlineData("size", "-1px")]
    [InlineData("padding", "-1px")]
    [InlineData("font-weight", "boldish")]
    [InlineData("text-shadow", "inset 0 0 red")]
    [InlineData("text-shadow", "0 0 1px 2px red")]
    [InlineData("text-shadow", "0 0 1px 0 red")]
    [InlineData("image-fit", "stretch")]
    [InlineData("image-background-color", "url(icon.png)")]
    [InlineData("image-clip", "circle")]
    [InlineData("mask-image", "linear-gradient(red, blue)")]
    [InlineData("mask-image", "url(a.svg), url(b.svg)")]
    [InlineData("border-left-width", "-1px")]
    [InlineData("background-clip", "content-box")]
    [InlineData("transform", "rotate(1px)")]
    public void Invalid_badge_values_report_compiler_diagnostics(string suffix, string value) =>
        Assert.False(NssCompiler.Compile("invalid", $":root {{ -windows-badge-{suffix}: {value}; }}").Success);

    [Fact]
    public void Badge_border_and_background_shorthands_obey_cascade_order_and_reset_omitted_parts()
    {
        var sheet = NssCompiler.Compile("badge shorthand", """
            :root {
                -windows-badge-border-left: 4px dotted red;
                -windows-badge-border: 1px solid blue;
                -windows-badge-border-width: 1px 2px 3px 4px;
                -windows-badge-border-color: red green blue white;
                -windows-badge-background-image: url(old.png);
                -windows-badge-background: linear-gradient(red, blue) center / contain no-repeat green;
            }
            """);
        Assert.True(sheet.Success, string.Join("\n", sheet.Diagnostics));
        using var host = new TestHost();
        using var engine = new StyleEngine(host.Renderer) { Application = sheet.StyleSheet };
        var root = host.Render<PlatformStylePage>();
        var style = engine.GetStyle(root.Elements("Window").Single());
        Assert.Equal(Length.Px(4), style.Get(StyleProperties.WindowsBadgeBorderLeftWidth));
        Assert.Equal(BorderStyle.Solid, style.Get(StyleProperties.WindowsBadgeBorderLeftStyle));
        Assert.Equal(Color.White, style.Get(StyleProperties.WindowsBadgeBorderLeftColor));
        Assert.Equal(new NativeColor(Color.FromRgb(0, 128, 0)), style.Get(StyleProperties.WindowsBadgeBackgroundColor));
        Assert.IsType<LinearGradient>(Assert.Single(style.Get(StyleProperties.WindowsBadgeBackgroundImage).Items));
    }
}
