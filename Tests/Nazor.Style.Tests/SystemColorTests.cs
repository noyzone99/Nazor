using Nazor.Nss;
using Nazor.Rendering;
using Nazor.Testing;

namespace Nazor.Style.Tests;

[Collection("StyleRegistry")]
public sealed class SystemColorTests : IDisposable
{
    private static readonly SystemColors Contrast = SystemColors.Default with
    {
        Canvas = Color.Black, CanvasText = Color.FromRgb(0xff, 0xff, 0x00), ButtonText = Color.FromRgb(0x00, 0xff, 0x00),
        Highlight = Color.FromRgb(0x1a, 0xeb, 0xff), GrayText = Color.FromRgb(0x3f, 0xf2, 0x3f),
    };

    private readonly TestHost host = new();
    private readonly StyleEngine engine;
    private readonly RootComponent root;

    public SystemColorTests()
    {
        engine = new StyleEngine(host.Renderer);
        root = host.Render<Nested>();
    }

    public void Dispose()
    {
        engine.Dispose();
        host.Dispose();
    }

    private void Use(string nss)
    {
        var result = NssCompiler.Compile("test", nss, "test.nss");
        Assert.True(result.Success, string.Join("\n", result.Diagnostics));
        engine.Application = result.StyleSheet;
    }

    private ComputedStyle Style(string name) => engine.GetStyle(root.Single(name));

    [Fact]
    public void Overriding_a_system_variable_changes_its_keyword_while_theme_tokens_stay_independent()
    {
        Use("Top { --nz-system-canvas: pink; background: Canvas; } Leaf { --nz-surface: red; background: var(--nz-surface); color: Canvas; }");
        var pink = Color.FromRgb(255, 192, 203);
        Assert.Equal(pink, Style("Top").BackgroundColor);
        Assert.Equal(pink, Style("Leaf").Color);
        Assert.Equal(Color.FromRgb(255, 0, 0), Style("Leaf").BackgroundColor);
        engine.Preferences = engine.Preferences with { Colors = Contrast };
        Assert.Equal(pink, Style("Top").BackgroundColor);
        Assert.Equal(pink, Style("Leaf").Color);
    }

    [Fact]
    public void System_colors_take_the_colors_of_the_window_and_follow_them()
    {
        Use("Top { background: Canvas; color: canvastext; } Leaf { border: 1px solid ButtonText; box-shadow: 0 1px 2px Highlight; }");
        Assert.Equal(SystemColors.Resolve().Canvas, Style("Top").BackgroundColor);
        Assert.Equal(SystemColors.Resolve().CanvasText, Style("Top").Color);

        engine.Preferences = engine.Preferences with { Colors = Contrast };
        Assert.Equal(Color.Black, Style("Top").BackgroundColor);
        Assert.Equal(Contrast.CanvasText, Style("Top").Color);
        Assert.Equal(Contrast.ButtonText, Style("Leaf").Get(StyleProperties.BorderTopColor));
        Assert.Equal(Contrast.Highlight, Style("Leaf").Get(StyleProperties.BoxShadow).Items[0].Color);
        Assert.Equal(3, engine.LastRecomputedCount);
    }

    [Fact]
    public void Custom_properties_and_inline_styles_name_them_too()
    {
        Use("Top { --accent: Highlight; } Middle { background: var(--accent); }");
        engine.Preferences = engine.Preferences with { Colors = Contrast };
        Assert.Equal(Contrast.Highlight, Style("Middle").BackgroundColor);

        var declarations = InlineStyle.Parse("color: GrayText; --x: Canvas");
        Assert.All(declarations, d => Assert.Contains("var(--nz-system-", d switch
        {
            PendingDeclaration pending => Syntax.StyleToken.Serialize(pending.Tokens),
            CustomPropertyDeclaration custom => Syntax.StyleToken.Serialize(custom.Tokens),
            _ => "",
        }, StringComparison.Ordinal));
    }

    [Fact]
    public void The_shades_of_the_accent_are_custom_properties_of_the_root()
    {
        Use("Top { background: var(--nz-system-accent-dark-1); color: var(--nz-system-accent-light-3); } Leaf { color: AccentColor; }");
        Assert.Equal(SystemColors.Resolve().AccentDark1, Style("Top").BackgroundColor);
        var green = SystemColors.Default with { AccentColor = Color.FromRgb(0x10, 0x7c, 0x10), AccentDark1 = Color.FromRgb(0x0e, 0x70, 0x0e), AccentLight3 = Color.FromRgb(0xa8, 0xe6, 0xa3) };
        engine.Preferences = engine.Preferences with { Colors = green };
        Assert.Equal((green.AccentDark1, green.AccentLight3), (Style("Top").BackgroundColor, Style("Top").Color));
        Assert.Equal(green.AccentColor, Style("Leaf").Color);

        Assert.Equal("--nz-system-accent-light-2", SystemColors.Property("AccentLight2"));
        Assert.Equal("--nz-system-canvas", SystemColors.Property("canvas"));
        Assert.Equal(green.AccentDark1, green["accentdark1"]);
        Assert.Null(SystemColors.Variable("AccentDark1"));
    }

    [Theory]
    [InlineData("#0078d4", "#ffffff")]
    [InlineData("#005fb8", "#ffffff")]
    [InlineData("#107c10", "#ffffff")]
    [InlineData("#60cdff", "#000000")]
    [InlineData("#ffb900", "#000000")]
    [InlineData("#767676", "#ffffff")]
    [InlineData("#808080", "#000000")]
    public void Text_on_an_accent_is_white_when_it_is_readable(string accent, string text)
    {
        var background = Color.FromRgba(Convert.ToUInt32(accent[1..] + "ff", 16));
        Assert.Equal(text, SystemColors.TextOn(background).ToString());
    }

    [Fact]
    public void Names_of_fonts_stay_names()
    {
        Use("Top { font-family: Highlight, sans-serif; }");
        Assert.Equal("Highlight", Style("Top").Get(StyleProperties.FontFamily).Items[0]);
    }

    [Theory]
    [InlineData("(forced-colors: active)", false, true)]
    [InlineData("(forced-colors)", false, true)]
    [InlineData("(forced-colors: none)", true, false)]
    [InlineData("(prefers-contrast: more)", false, true)]
    [InlineData("(prefers-contrast)", false, true)]
    [InlineData("(prefers-contrast: no-preference)", true, false)]
    [InlineData("(prefers-contrast: less)", false, false)]
    public void Contrast_themes_are_media_features(string query, bool withoutContrast, bool withContrast)
    {
        Use($"Top {{ width: 1px; }} @media {query} {{ Top {{ width: 2px; }} }}");
        Assert.Equal(withoutContrast ? 2 : 1, Width());
        engine.Preferences = engine.Preferences with { ForcedColors = true };
        Assert.Equal(withContrast ? 2 : 1, Width());

        double Width() => Style("Top").Get(StyleProperties.Width).Length.Value;
    }

    [Fact]
    public void Contrast_media_survive_the_json_of_hot_reload()
    {
        var sheet = NssCompiler.Compile("test", "@media (forced-colors: active) and (prefers-contrast: more) { Top { width: 2px; } }", "test.nss").StyleSheet;
        var read = StyleSheetJson.Deserialize(StyleSheetJson.Serialize(sheet));
        var condition = Assert.Single(read.Rules.Single().Media!.Conditions);
        Assert.Equal((true, true), (condition.ForcedColors, condition.PrefersMoreContrast));
    }
}
