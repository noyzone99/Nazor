using Nazor.Nss;
using Nazor.Rendering;
using Nazor.Testing;

namespace Nazor.Style.Tests;

public sealed class Nested : ComponentBase
{
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "Top");
        builder.OpenElement(1, "Middle");
        builder.OpenElement(2, "Leaf");
        builder.CloseElement();
        builder.CloseElement();
        builder.CloseElement();
    }
}

[Collection("StyleRegistry")]
public sealed class RelativeLengthTests : IDisposable
{
    private readonly TestHost host = new();
    private readonly StyleEngine engine;
    private readonly RootComponent root;

    public RelativeLengthTests()
    {
        engine = new StyleEngine(host.Renderer) { Viewport = new Viewport(1000, 500) };
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
    public void Clip_insets_resolve_relative_units_and_remain_non_inherited()
    {
        Use("Top { font-size: 20px; clip-path: inset(1em 10vw 5% calc(1em + 2px)); } Leaf { font-size: 10px; }");
        var clip = Style("Top").Get(StyleProperties.ClipPath);
        Assert.Equal(Length.Px(20), clip.Top);
        Assert.Equal(Length.Px(100), clip.Right);
        Assert.Equal(Length.Percent(5), clip.Bottom);
        Assert.Equal(22, clip.Left.Resolve(0));
        Assert.Equal(ClipPathValue.None, Style("Leaf").Get(StyleProperties.ClipPath));
    }

    [Fact]
    public void Rounded_clip_radius_axes_compute_relative_units_and_keep_reference_percentages()
    {
        Use("Top { font-size: 20px; clip-path: inset(0 round 1em 2vw / 10% calc(1em + 2px)); }");
        var clip = Style("Top").Get(StyleProperties.ClipPath);
        Assert.Equal(new RadiusValue(Length.Px(20), Length.Percent(10)), clip.TopLeftRadius);
        Assert.Equal(Length.Px(20), clip.TopRightRadius.X);
        Assert.Equal(22, clip.TopRightRadius.Y.Resolve(0));
        Assert.Equal(clip.TopLeftRadius, clip.BottomRightRadius);
        Assert.Equal(clip.TopRightRadius, clip.BottomLeftRadius);
    }

    [Theory]
    [InlineData("padding-left: 1.5em", "padding-left: 1.5em")]
    [InlineData("margin: 1rem 2vw 3vh 4vmin", "margin-top: 1rem; margin-right: 2vw; margin-bottom: 3vh; margin-left: 4vmin")]
    [InlineData("width: 10VMAX", "width: 10vmax")]
    [InlineData("width: calc(10px + 2 * 5px)", "width: 20px")]
    [InlineData("width: calc(100% - 2 * 8px)", "width: calc(100% - 16px)")]
    [InlineData("width: calc((100% - 10px) / 2)", "width: calc(50% - 5px)")]
    [InlineData("width: calc(50% + 50%)", "width: 100%")]
    [InlineData("width: min(50%, 300px)", "width: min(50%, 300px)")]
    [InlineData("width: max(10px, 20px)", "width: 20px")]
    [InlineData("width: clamp(200px, 50%, 600px)", "width: clamp(200px, 50%, 600px)")]
    [InlineData("width: clamp(200px, 100px, 600px)", "width: 200px")]
    [InlineData("width: calc(1em + 4px)", "width: calc(1em + 4px)")]
    [InlineData("width: calc(2 * (1rem - 2px))", "width: calc(2 * (1rem - 2px))")]
    [InlineData("width: calc(min(1em, 10px) - 1px)", "width: calc(min(1em, 10px) - 1px)")]
    [InlineData("padding-left: calc(10px - 20px)", "padding-left: 0px")]
    [InlineData("margin-left: calc(10px - 20px)", "margin-left: -10px")]
    [InlineData("font-size: 120%", "font-size: 120%")]
    [InlineData("line-height: 150%", "line-height: 150%")]
    [InlineData("box-shadow: 0 calc(1em / 2) 2em red", "box-shadow: 0px calc(1em / 2) 2em 0px #ff0000")]
    [InlineData("border-width: calc(1px + 0.1em)", "border-top-width: calc(1px + 0.1em); border-right-width: calc(1px + 0.1em); border-bottom-width: calc(1px + 0.1em); border-left-width: calc(1px + 0.1em)")]
    public void Relative_units_and_math_functions_compile_and_reduce(string declaration, string expected)
    {
        var result = NssCompiler.Compile("t", $".x {{ {declaration}; }}", "t.nss");
        Assert.True(result.Success, string.Join("\n", result.Diagnostics));
        Assert.Equal(expected, string.Join("; ", result.StyleSheet.Rules.Single().Declarations));
    }

    [Theory]
    [InlineData("width: calc(100%-10px)", "Missing an operator")]
    [InlineData("width: calc(100% -10px)", "Missing an operator")]
    [InlineData("width: calc(10px +5px)", "Missing an operator")]
    [InlineData("width: calc(10px* 2 +1px)", "Missing an operator")]
    [InlineData("width: calc(10px * 2px)", "Cannot multiply two lengths")]
    [InlineData("width: calc(10px / 2px)", "Cannot divide by a length")]
    [InlineData("width: calc(10px / (1 - 1))", "Division by zero")]
    [InlineData("width: calc(10px + 2)", "Cannot add a number and a length")]
    [InlineData("width: calc(2 * 3)", "must give a length")]
    [InlineData("width: clamp(1px, 2px)", "clamp() takes three values")]
    [InlineData("width: min(10px, 2)", "must all be lengths or all numbers")]
    [InlineData("width: calc(10pt)", "Unit 'pt' is not supported")]
    [InlineData("width: calc(red)", "Expected a number, a length or '('")]
    [InlineData("border-top-width: calc(10% + 1px)", "Percentages are not allowed here")]
    [InlineData("padding-left: -1em", "Negative values are not allowed")]
    public void Invalid_math_is_reported(string declaration, string message)
    {
        var result = NssCompiler.Compile("t", $".x {{ {declaration}; }}", "t.nss");
        Assert.Contains(result.Diagnostics, d => d.Message.Contains(message, StringComparison.Ordinal));
    }

    [Fact]
    public void Em_is_the_font_size_of_the_element_and_of_the_parent_on_font_size()
    {
        Use("""
            Top { font-size: 20px; }
            Middle { font-size: 1.5em; padding-left: 2em; }
            Leaf { font-size: 50%; margin-left: 1rem; line-height: 150%; width: calc(50% + 1em); }
            """);
        Assert.Equal(Length.Px(30), Style("Middle").Get(StyleProperties.FontSize));
        Assert.Equal(Length.Px(60), Style("Middle").Get(StyleProperties.PaddingLeft));
        var leaf = Style("Leaf");
        Assert.Equal(15, leaf.FontSize);
        Assert.Equal(SizeValue.Fixed(Length.Px(20)), leaf.Get(StyleProperties.MarginLeft));
        Assert.Equal(LineHeight.Fixed(Length.Px(22.5)), leaf.Get(StyleProperties.LineHeight));
        var width = leaf.Get(StyleProperties.Width).Length;
        Assert.True(width.IsPercent);
        Assert.Equal("calc(50% + 15px)", width.ToString());
        Assert.Equal(115, width.Resolve(200));
    }

    [Fact]
    public void Inherited_values_are_the_computed_px_not_the_em()
    {
        Use("Top { font-size: 10px; } Middle { font-size: 2em; line-height: 1.5em; }");
        Assert.Equal(20, Style("Middle").FontSize);
        Assert.Equal(20, Style("Leaf").FontSize);
        Assert.Equal(LineHeight.Fixed(Length.Px(30)), Style("Leaf").Get(StyleProperties.LineHeight));
    }

    [Fact]
    public void Rem_on_the_root_font_size_is_the_initial_font_size()
    {
        Use("Top { font-size: 2rem; } Leaf { width: 1rem; }");
        Assert.Equal(28, Style("Top").FontSize);
        Assert.Equal(SizeValue.Fixed(Length.Px(28)), Style("Leaf").Get(StyleProperties.Width));
    }

    [Fact]
    public void A_new_root_font_size_reaches_rem_below_unchanged_elements()
    {
        Use("Top:hover { font-size: 20px; } Middle { font-size: 10px; } Leaf { width: 2rem; }");
        Assert.Equal(SizeValue.Fixed(Length.Px(28)), Style("Leaf").Get(StyleProperties.Width));
        root.Single("Top").SetState(ElementStates.Hover, true);
        Assert.Equal(SizeValue.Fixed(Length.Px(40)), Style("Leaf").Get(StyleProperties.Width));
    }

    [Fact]
    public void Viewport_units_follow_the_viewport_and_recompute_only_their_elements()
    {
        Use("Leaf { width: 10vw; height: 10vh; min-width: 10vmin; max-width: 10vmax; padding-left: calc(1vw + 1px); }");
        var leaf = Style("Leaf");
        Assert.Equal(SizeValue.Fixed(Length.Px(100)), leaf.Get(StyleProperties.Width));
        Assert.Equal(SizeValue.Fixed(Length.Px(50)), leaf.Get(StyleProperties.Height));
        Assert.Equal(SizeValue.Fixed(Length.Px(50)), leaf.Get(StyleProperties.MinWidth));
        Assert.Equal(SizeValue.Fixed(Length.Px(100)), leaf.Get(StyleProperties.MaxWidth));
        Assert.Equal(Length.Px(11), leaf.Get(StyleProperties.PaddingLeft));

        engine.Viewport = new Viewport(800, 400);
        Assert.True(engine.IsDirty);
        Assert.Equal(SizeValue.Fixed(Length.Px(80)), Style("Leaf").Get(StyleProperties.Width));
        Assert.Equal(1, engine.LastRecomputedCount);

        Use("Leaf { width: 10px; }");
        engine.Update();
        engine.Viewport = new Viewport(600, 300);
        Assert.False(engine.IsDirty);
    }

    [Fact]
    public void Var_values_with_relative_units_are_converted_per_element()
    {
        Use("Top { --gap: 2em; font-size: 10px; } Middle { font-size: 20px; padding-left: var(--gap); } Leaf { padding-left: var(--gap); font-size: 5px; }");
        Assert.Equal(Length.Px(40), Style("Middle").Get(StyleProperties.PaddingLeft));
        Assert.Equal(Length.Px(10), Style("Leaf").Get(StyleProperties.PaddingLeft));
    }

    [Fact]
    public void Lengths_inside_shadows_gradients_and_tracks_are_converted()
    {
        Use("""
            Top { font-size: 10px; }
            Leaf {
                box-shadow: 1em 2em 3em red;
                background-image: radial-gradient(circle 2em at 1em 50%, red, blue 3em);
                grid-template-columns: 2em repeat(auto-fill, minmax(10vw, 1fr));
                border-top-left-radius: 1em 50%;
            }
            """);
        var leaf = Style("Leaf");
        Assert.Equal("10px 20px 30px 0px #ff0000", leaf.Get(StyleProperties.BoxShadow).ToString());
        var gradient = Assert.IsType<RadialGradient>(leaf.Get(StyleProperties.BackgroundImage).Items.Single());
        Assert.Equal((Length.Px(20), Length.Px(10)), (gradient.SizeX, gradient.CenterX));
        Assert.Equal<Length?>(Length.Px(30), gradient.Stops.Items[1].Position);
        var tracks = leaf.Get(StyleProperties.GridTemplateColumns);
        Assert.Equal(TrackSize.Fixed(Length.Px(20)), tracks.Tracks.Items[0]);
        Assert.Equal(Length.Px(100), tracks.RepeatTracks.Items[0].Min.Length);
        Assert.Equal(new RadiusValue(Length.Px(10), Length.Percent(50)), leaf.Get(StyleProperties.BorderTopLeftRadius));
    }

    [Fact]
    public void Math_inside_other_functions_keeps_its_spaces()
    {
        var result = NssCompiler.Compile("t", ".x { background-image: linear-gradient(red calc(50% - 10px), blue); }", "t.nss");
        Assert.True(result.Success, string.Join("\n", result.Diagnostics));
        var images = (ValueList<ImageValue>)((ValueDeclaration)result.StyleSheet.Rules[0].Declarations[0]).Value;
        Assert.Equal("calc(50% - 10px)", ((Gradient)images.Items[0]).Stops.Items[0].Position.ToString());
    }

    [Fact]
    public void Calculations_round_trip_through_json()
    {
        var sheet = NssCompiler.Compile("t", ".x { width: calc((100% - 2em) / 3); max-height: max(10vmin, 20px, calc(50% - 1px)); padding-left: clamp(1px, 1em, 3px); }").StyleSheet;
        var json = StyleSheetJson.Serialize(sheet);
        Assert.Contains("\"calc\":", json);
        Assert.Equal(sheet.Describe(), StyleSheetJson.Deserialize(json).Describe());
    }
}
