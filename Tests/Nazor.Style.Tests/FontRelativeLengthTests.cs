using Nazor.Nss;
using Nazor.Rendering;
using Nazor.Testing;

namespace Nazor.Style.Tests;

public sealed class FontRelativeLengthTests : IDisposable
{
    private readonly TestHost host = new();
    private readonly StyleEngine engine;
    private readonly RootComponent root;

    public FontRelativeLengthTests()
    {
        engine = new StyleEngine(host.Renderer);
        root = host.Render<Nested>();
    }

    public void Dispose() { engine.Dispose(); host.Dispose(); }
    private ComputedStyle Style(string name) => engine.GetStyle(root.Single(name));
    private void Use(string text)
    {
        var result = NssCompiler.Compile("font-metrics", text);
        Assert.True(result.Success, string.Join("\n", result.Diagnostics));
        engine.Application = result.StyleSheet;
    }

    [Theory]
    [InlineData("ch", LengthUnit.Ch)]
    [InlineData("ex", LengthUnit.Ex)]
    [InlineData("CH", LengthUnit.Ch)]
    [InlineData("EX", LengthUnit.Ex)]
    public void Units_round_trip_as_relative_dimensions_in_compiled_styles(string spelling, LengthUnit unit)
    {
        var result = NssCompiler.Compile("unit", $"Leaf {{ width: 2{spelling}; }}");
        Assert.True(result.Success);
        var declaration = Assert.IsType<ValueDeclaration>(Assert.Single(Assert.Single(result.StyleSheet.Rules).Declarations));
        var length = Assert.IsType<SizeValue>(declaration.Value).Length;
        Assert.Equal(unit, length.Unit);
        Assert.True(length.IsRelative);
        Assert.Equal("2" + spelling.ToLowerInvariant(), length.ToString());
        Assert.Contains("LengthUnit." + unit, CSharpEmitter.Emit(result.StyleSheet));
    }

    [Fact]
    public void Missing_metrics_independently_fall_back_to_half_the_computed_font_size()
    {
        Use("Top { font-size: 20px; width: 3ch; height: 2ex; } Leaf { font-size: 10px; width: 3ch; height: 2ex; }");
        Assert.Equal(30, Style("Top").Get(StyleProperties.Width).Length.Value);
        Assert.Equal(20, Style("Top").Get(StyleProperties.Height).Length.Value);
        Assert.Equal(15, Style("Leaf").Get(StyleProperties.Width).Length.Value);
        Assert.Equal(10, Style("Leaf").Get(StyleProperties.Height).Length.Value);
        engine.FontMetricsResolver = _ => new FontRelativeMetrics(7, null);
        Assert.Equal(21, Style("Leaf").Get(StyleProperties.Width).Length.Value);
        Assert.Equal(10, Style("Leaf").Get(StyleProperties.Height).Length.Value);
    }

    [Fact]
    public void Font_size_uses_parent_metrics_but_other_lengths_use_the_elements_computed_font()
    {
        var captured = new List<ComputedStyle>();
        engine.FontMetricsResolver = style =>
        {
            captured.Add(style);
            return style.Get(StyleProperties.FontFamily).Items[0] == "Parent" ? new FontRelativeMetrics(5, 4) : new FontRelativeMetrics(7, 6);
        };
        Use("Top { font-size: 20px; font-family: Parent; } Leaf { font-size: 2ch; font-family: Own; width: 3ch; height: 1ex; line-height: 2ex; }");
        var leaf = Style("Leaf");
        Assert.Equal(10, leaf.FontSize);
        Assert.Equal(21, leaf.Get(StyleProperties.Width).Length.Value);
        Assert.Equal(6, leaf.Get(StyleProperties.Height).Length.Value);
        Assert.Equal(LineHeight.Fixed(Length.Px(12)), leaf.Get(StyleProperties.LineHeight));
        Assert.Contains(captured, style => style.FontSize == 20 && style.Get(StyleProperties.FontFamily).Items[0] == "Parent");
        var own = Assert.Single(captured, style => style.FontSize == 10 && style.Get(StyleProperties.FontFamily).Items[0] == "Own");
        Assert.Equal(10, own.FontSize);
        Assert.Equal("Own", own.Get(StyleProperties.FontFamily).Items[0]);
    }

    [Fact]
    public void Calc_shorthands_pseudos_and_keyframes_use_the_same_measured_units()
    {
        engine.FontMetricsResolver = _ => new FontRelativeMetrics(7, 4);
        Use("Leaf { width: calc(50% + 2ch); padding: 1ex 1ch; } Leaf::before { content: 'x'; width: 2ch; } @keyframes measured { from { transform: translateX(2ch); opacity: 0; } to { transform: translateX(3ex); opacity: 1; } }");
        Assert.Equal(64, Style("Leaf").Get(StyleProperties.Width).Length.Resolve(100));
        Assert.Equal(Length.Px(4), Style("Leaf").Get(StyleProperties.PaddingTop));
        Assert.Equal(Length.Px(7), Style("Leaf").Get(StyleProperties.PaddingRight));
        Assert.Equal(14, engine.GetPseudoStyle(root.Single("Leaf"), PseudoElement.Before).Get(StyleProperties.Width).Length.Value);
        var frames = Assert.IsAssignableFrom<IReadOnlyList<ResolvedKeyframe>>(engine.ResolveKeyframes(root.Single("Leaf"), "measured"));
        Assert.Contains("14px", frames[0].Values[StyleProperties.Transform].ToString());
        Assert.Contains("12px", frames[1].Values[StyleProperties.Transform].ToString());
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(-1)]
    public void Invalid_metric_results_cannot_enter_computed_lengths(double metric)
    {
        engine.FontMetricsResolver = _ => new FontRelativeMetrics(metric, metric);
        Use("Leaf { width: 2ch; }");
        Assert.Throws<InvalidOperationException>(() => Style("Leaf"));
    }

    [Fact]
    public void Zero_metrics_are_preserved_and_replacing_the_provider_invalidates_computed_values()
    {
        engine.FontMetricsResolver = _ => new FontRelativeMetrics(0, 0);
        Use("Leaf { width: 2ch; height: 2ex; }");
        Assert.Equal(0, Style("Leaf").Get(StyleProperties.Width).Length.Value);
        engine.FontMetricsResolver = _ => new FontRelativeMetrics(9, 3);
        Assert.Equal(18, Style("Leaf").Get(StyleProperties.Width).Length.Value);
        Assert.Equal(6, Style("Leaf").Get(StyleProperties.Height).Length.Value);
    }

    [Fact]
    public void Disposing_the_style_engine_releases_its_metric_provider()
    {
        engine.FontMetricsResolver = _ => new FontRelativeMetrics(7, 4);
        engine.Dispose();
        Assert.Null(engine.FontMetricsResolver);
    }

    [Theory]
    [InlineData("2ch")]
    [InlineData("2ex")]
    [InlineData("calc(1px + 2ch)")]
    [InlineData("calc(1px + 2ex)")]
    public void Media_queries_reject_metric_units_without_a_font_context(string length)
    {
        var result = NssCompiler.Compile("metric-media", $"@media (min-width: {length}) {{ Leaf {{ opacity: 0; }} }}");
        Assert.False(result.Success);
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Id == "NZS104" && diagnostic.Message.Contains("element context"));
    }
}
