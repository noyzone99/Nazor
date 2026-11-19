using Nazor.Nss;
using Nazor.Rendering;
using Nazor.Testing;

namespace Nazor.Style.Tests;

public sealed class PlatformStylePage : ComponentBase
{
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "Window");
        builder.AddAttribute(0, "role", "window");
        builder.OpenElement(1, "TitleBar");
        builder.AddAttribute(2, "class", "bar");
        builder.OpenElement(3, "Button");
        builder.CloseElement();
        builder.CloseElement();
        builder.CloseElement();
    }
}

public sealed class PlatformStyleTests
{
    [Theory]
    [InlineData("-windows-window-controls-height", "tall")]
    [InlineData("-windows-window-controls-color", "white")]
    [InlineData("-windows-window-controls-background", "transparent")]
    [InlineData("-windows-window-resize-grip", "auto")]
    public void Removed_native_aliases_are_rejected(string name, string value)
    {
        Assert.False(StyleProperties.TryGet(name, out _));
        Assert.False(NssCompiler.Compile("removed", $":root {{ {name}: {value}; }}").Success);
    }

    public static IEnumerable<object[]> NativeColors => StyleProperties.All
        .Where(p => p.Name.StartsWith("-windows-title-bar-", StringComparison.Ordinal) && p.Name.EndsWith("-color", StringComparison.Ordinal)).Select(p => new object[] { p.Name });

    [Theory]
    [MemberData(nameof(NativeColors))]
    public void Title_bar_colors_roundtrip_resolve_currentColor_and_apply_only_to_the_root_window(string name)
    {
        using var host = new TestHost();
        using var styles = new StyleEngine(host.Renderer);
        var root = host.Render<PlatformStylePage>();
        Assert.True(StyleProperties.TryGet(name, out var descriptor));
        var property = Assert.IsType<StyleProperty<NativeColor>>(descriptor);
        foreach (var value in new[] { "#123456", "auto", "currentColor", "var(--caption)" })
        {
            var sheet = Sheet($"* {{ color: red; --caption: #123456; {name}: {value}; }}");
            if (value != "var(--caption)") Assert.Contains("StyleProperties.WindowsTitleBar", CSharpEmitter.Emit(sheet));
            styles.Application = StyleSheetJson.Deserialize(StyleSheetJson.Serialize(sheet));
            var expected = value == "auto" ? NativeColor.Auto : new NativeColor(value == "currentColor" ? Color.FromRgb(255, 0, 0) : Color.FromRgb(0x12, 0x34, 0x56));
            Assert.Equal(expected, styles.GetStyle(root.Elements("Window").Single()).Get(property));
            Assert.Equal(NativeColor.Auto, styles.GetStyle(root.Elements("TitleBar").Single()).Get(property));
            Assert.Equal(NativeColor.Auto, styles.GetStyle(root.Elements("Button").Single()).Get(property));
        }
        Assert.False(NssCompiler.Compile("invalid", $":root {{ {name}: nonsense; }}").Success);
    }

    [Theory]
    [InlineData("auto", true)]
    [InlineData("none", false)]
    public void Resize_grip_values_survive_code_generation_and_json(string keyword, bool expected)
    {
        var sheet = Sheet($":root {{ -window-resize-grip: {keyword}; }}");
        var declaration = Assert.IsType<ValueDeclaration>(Assert.Single(Assert.Single(sheet.Rules).Declarations));
        Assert.Equal(expected, declaration.Value);
        Assert.Contains($"StyleProperties.WindowResizeGrip, {(expected ? "true" : "false")})", CSharpEmitter.Emit(sheet));
        var restored = StyleSheetJson.Deserialize(StyleSheetJson.Serialize(sheet));
        var restoredDeclaration = Assert.IsType<ValueDeclaration>(Assert.Single(Assert.Single(restored.Rules).Declarations));
        Assert.Same(declaration.Property, restoredDeclaration.Property);
        Assert.Equal(expected, restoredDeclaration.Value);
        Assert.False(NssCompiler.Compile("invalid", ":root { -window-resize-grip: invalid; }").Success);
    }

    private static StyleSheet Sheet(string text)
    {
        var result = NssCompiler.Compile("platform", text);
        Assert.True(result.Success, string.Join("\n", result.Diagnostics));
        return result.StyleSheet;
    }

    [Fact]
    public void Native_properties_apply_only_to_the_root_window_even_with_shared_and_class_selectors()
    {
        using var host = new TestHost();
        using var styles = new StyleEngine(host.Renderer);
        var root = host.Render<PlatformStylePage>();
        styles.Application = Sheet("""
            * { -windows-title-bar-height: tall; -windows-title-bar-button-foreground-color: red;
                -windows-title-bar-button-background-color: blue; -window-resize-grip: auto; }
            .bar { -windows-title-bar-button-foreground-color: green; }
            """);
        var window = styles.GetStyle(root.Elements("Window").Single());
        var bar = styles.GetStyle(root.Elements("TitleBar").Single());
        var button = styles.GetStyle(root.Elements("Button").Single());
        Assert.Equal(WindowsTitleBarHeight.Tall, window.Get(StyleProperties.WindowsTitleBarHeight));
        Assert.Equal(new NativeColor(Color.FromRgb(255, 0, 0)), window.Get(StyleProperties.WindowsTitleBarButtonForegroundColor));
        Assert.Equal(new NativeColor(Color.FromRgb(0, 0, 255)), window.Get(StyleProperties.WindowsTitleBarButtonBackgroundColor));
        Assert.True(window.Get(StyleProperties.WindowResizeGrip));
        foreach (var other in new[] { bar, button })
        {
            Assert.Equal(WindowsTitleBarHeight.Standard, other.Get(StyleProperties.WindowsTitleBarHeight));
            Assert.Equal(NativeColor.Auto, other.Get(StyleProperties.WindowsTitleBarButtonForegroundColor));
            Assert.Equal(NativeColor.Auto, other.Get(StyleProperties.WindowsTitleBarButtonBackgroundColor));
            Assert.False(other.Get(StyleProperties.WindowResizeGrip));
        }
    }

    [Fact]
    public void Client_frame_roundtrips_corners_and_resolves_theme_lengths_and_colors()
    {
        using var host = new TestHost();
        using var styles = new StyleEngine(host.Renderer);
        var root = host.Render<PlatformStylePage>();
        var sheet = Sheet("""
            :root {
                color: #123456;
                font-size: 20px;
                -window-frame-border: 1px dashed currentColor;
                -window-frame-border-radius: 1px 2px 3px 4px;
                -window-frame-box-shadow: 0 1em 3px currentColor;
                -window-frame-resize-inset: 0px;
                -window-resize-grip: auto;
            }
            """);
        Assert.Contains("WindowFrameShadows", CSharpEmitter.Emit(sheet));
        styles.Application = StyleSheetJson.Deserialize(StyleSheetJson.Serialize(sheet));
        var window = styles.GetStyle(root.Elements("Window").Single());
        var color = Color.FromRgb(0x12, 0x34, 0x56);
        Assert.Equal(color, window.Get(StyleProperties.WindowResizeGripColor));
        Assert.Equal(color, window.Get(StyleProperties.WindowFrameBorderColor));
        Assert.Equal(BorderStyle.Dashed, window.Get(StyleProperties.WindowFrameBorderStyle));
        Assert.Equal(SizeValue.Fixed(Length.Px(1)), window.Get(StyleProperties.WindowFrameBorderTopLeftRadius));
        Assert.Equal(SizeValue.Fixed(Length.Px(2)), window.Get(StyleProperties.WindowFrameBorderTopRightRadius));
        Assert.Equal(SizeValue.Fixed(Length.Px(3)), window.Get(StyleProperties.WindowFrameBorderBottomRightRadius));
        Assert.Equal(SizeValue.Fixed(Length.Px(4)), window.Get(StyleProperties.WindowFrameBorderBottomLeftRadius));
        Assert.Equal(SizeValue.Fixed(Length.Zero), window.Get(StyleProperties.WindowFrameResizeInset));
        Assert.Equal(SizeValue.Auto, window.Get(StyleProperties.WindowFrameResizeOutset));
        var shadow = Assert.Single(window.Get(StyleProperties.WindowFrameBoxShadow).Shadows!.Items);
        Assert.Equal(Length.Px(20), shadow.Y);
        Assert.Equal(color, shadow.Color);
    }

    [Fact]
    public void Measured_height_is_per_window_inherited_and_invalidates_only_when_changed()
    {
        using var first = new TestHost();
        using var second = new TestHost();
        using var firstStyles = new StyleEngine(first.Renderer);
        using var secondStyles = new StyleEngine(second.Renderer);
        firstStyles.Application = secondStyles.Application = Sheet("* { min-height: var(--nz-window-controls-height, 19px); }");
        var one = first.Render<PlatformStylePage>().Elements("Button").Single();
        var two = second.Render<PlatformStylePage>().Elements("Button").Single();
        firstStyles.SystemButtonsHeight = 48;
        Assert.Equal(SizeValue.Fixed(Length.Px(48)), firstStyles.GetStyle(one).Get(StyleProperties.MinHeight));
        Assert.Equal(SizeValue.Fixed(Length.Px(19)), secondStyles.GetStyle(two).Get(StyleProperties.MinHeight));
        Assert.False(firstStyles.IsDirty);
        firstStyles.SystemButtonsHeight = 48;
        Assert.False(firstStyles.IsDirty);
        firstStyles.SystemButtonsHeight = 0;
        Assert.Equal(SizeValue.Fixed(Length.Zero), firstStyles.GetStyle(one).Get(StyleProperties.MinHeight));
        firstStyles.SystemButtonsHeight = null;
        Assert.Equal(SizeValue.Fixed(Length.Px(19)), firstStyles.GetStyle(one).Get(StyleProperties.MinHeight));
        Assert.DoesNotContain("--nz-window-controls-height", firstStyles.GetStyle(one).CustomProperties.Keys);
    }

    [Theory]
    [InlineData("windows", PlatformKind.Windows)]
    [InlineData("linux", PlatformKind.Linux)]
    [InlineData("macos", PlatformKind.MacOS)]
    [InlineData("android", PlatformKind.Android)]
    [InlineData("ios", PlatformKind.IOS)]
    public void Platform_queries_roundtrip_and_rematch_only_the_backend_identity(string name, PlatformKind platform)
    {
        var sheet = Sheet($"@media (platform: {name}) and (min-width: 500px) {{ Button {{ padding: 9px; }} }}");
        Assert.Equal(sheet.Describe(), StyleSheetJson.Deserialize(StyleSheetJson.Serialize(sheet)).Describe());
        Assert.Contains($"global::Nazor.PlatformKind.{platform}", CSharpEmitter.Emit(sheet));
        using var host = new TestHost();
        using var engine = new StyleEngine(host.Renderer) { Application = sheet };
        var button = host.Render<PlatformStylePage>().Elements("Button").Single();
        Assert.Equal(Length.Zero, engine.GetStyle(button).Get(StyleProperties.PaddingTop));
        engine.Preferences = engine.Preferences with { Platform = platform };
        Assert.Equal(Length.Px(9), engine.GetStyle(button).Get(StyleProperties.PaddingTop));
        engine.Preferences = engine.Preferences with { Platform = null };
        Assert.Equal(Length.Zero, engine.GetStyle(button).Get(StyleProperties.PaddingTop));
        Assert.False(NssCompiler.Compile("invalid", "@media (platform: unknown) { Button { padding: 1px; } }").Success);
        Assert.False(new MediaCondition(null, null, null, null, Platform: PlatformKind.Windows)
            .And(new(null, null, null, null, Platform: PlatformKind.Linux)).Matches(new(800, 600), new(ColorScheme.Light, false, Platform: platform)));
    }
}
