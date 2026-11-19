using Nazor.Animation;
using Nazor.Controls;
using Nazor.Graphics;
using Nazor.Input;
using Nazor.Style;
using Nazor.Testing;
using Color = Nazor.Graphics.Color;

namespace Nazor.Rendering.Tests;

public sealed class GlyphStrokeProbe : ComponentBase
{
    public string Glyph { get; set; } = "check";
    public string StrokeStyle { get; set; } = "";
    public void Refresh() => StateHasChanged();

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "Container");
        builder.AddAttribute(1, "style", "width: 80px; height: 80px; position: relative;");
        builder.OpenElement(2, "Glyph");
        builder.AddAttribute(3, "glyph", Glyph);
        builder.AddAttribute(4, "style", "width: 20px; height: 16px; color: black;" + StrokeStyle);
        builder.CloseElement();
        builder.CloseElement();
    }
}

public sealed class StrokeCheckBoxProbe : ComponentBase
{
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenComponent<CheckBox>(0);
        builder.CloseComponent();
    }
}

public sealed class GlyphStrokeTests
{
    private static DrawPolyline Stroke(RenderHarness harness) => Assert.Single(harness.Scene.BuildDisplayList().Commands.OfType<DrawPolyline>());

    [Theory]
    [InlineData("check")]
    [InlineData("chevron-down")]
    [InlineData("chevron-up")]
    [InlineData("chevron-left")]
    [InlineData("chevron-right")]
    public void Explicit_and_percentage_widths_preserve_the_path_and_zero_keeps_automatic_width(string glyph)
    {
        using var harness = new RenderHarness();
        harness.Scene.Styles.Theme = StyleSheets.Basic;
        var root = harness.Host.Render<GlyphStrokeProbe>();
        var probe = (GlyphStrokeProbe)root.Component;
        probe.Glyph = glyph;
        probe.Refresh();
        harness.Host.RunPending();
        harness.Render(80, 80, 1);
        var original = Stroke(harness);
        foreach (var (style, width) in new[] { ("stroke-width: 0;", original.Thickness), ("stroke-width: 0%;", original.Thickness),
            ("stroke-width: calc(1px - 1px);", original.Thickness), ("stroke-width: 0.75px;", 0.75f), ("stroke-width: 10%;", 1.6f) })
        {
            probe.StrokeStyle = style;
            probe.Refresh();
            harness.Host.RunPending();
            harness.Render(80, 80, 1);
            var actual = Stroke(harness);
            Assert.Equal(original.Points, actual.Points);
            Assert.Equal(width, actual.Thickness, 4);
        }
    }

    [Fact]
    public void Trimming_a_chevron_preserves_the_corner_between_both_cut_segments()
    {
        using var harness = new RenderHarness();
        harness.Scene.Styles.Theme = StyleSheets.Basic;
        var root = harness.Host.Render<GlyphStrokeProbe>();
        var probe = (GlyphStrokeProbe)root.Component;
        probe.Glyph = "chevron-down";
        probe.StrokeStyle = "stroke-trim-start: 0.25; stroke-trim-end: 0.75;";
        probe.Refresh();
        harness.Host.RunPending();
        harness.Render(80, 80, 1);
        Assert.Equal(new[] { new Point(8, 8), new Point(10, 10), new Point(12, 8) }, Stroke(harness).Points);
    }

    [Fact]
    public void Check_trim_uses_length_rather_than_vertex_count_and_keeps_the_bend()
    {
        using var harness = new RenderHarness();
        harness.Scene.Styles.Theme = StyleSheets.Basic;
        var root = harness.Host.Render<GlyphStrokeProbe>();
        var probe = (GlyphStrokeProbe)root.Component;
        harness.Render(80, 80, 1);
        var full = Stroke(harness).Points;
        static double Distance(Point a, Point b) => Math.Sqrt(Math.Pow(b.X - a.X, 2) + Math.Pow(b.Y - a.Y, 2));
        var length = Distance(full[0], full[1]) + Distance(full[1], full[2]);
        probe.StrokeStyle = "stroke-trim-end: 0.5;";
        probe.Refresh();
        harness.Host.RunPending();
        harness.Render(80, 80, 1);
        var half = Stroke(harness).Points;
        Assert.Equal(3, half.Count);
        Assert.Equal(full[0], half[0]);
        Assert.Equal(full[1], half[1]);
        Assert.Equal(length / 2, Distance(half[0], half[1]) + Distance(half[1], half[2]), 4);
    }

    [Theory]
    [InlineData("stroke-trim-end: 0;")]
    [InlineData("stroke-trim-start: 1;")]
    [InlineData("stroke-trim-start: 0.8; stroke-trim-end: 0.2;")]
    [InlineData("stroke-trim-start: 0.5; stroke-trim-end: 0.5;")]
    public void Empty_or_reversed_intervals_do_not_draw_a_dot(string style)
    {
        using var harness = new RenderHarness();
        harness.Scene.Styles.Theme = StyleSheets.Basic;
        var root = harness.Host.Render<GlyphStrokeProbe>();
        var probe = (GlyphStrokeProbe)root.Component;
        probe.StrokeStyle = style;
        probe.Refresh();
        harness.Host.RunPending();
        harness.Render(80, 80, 1);
        Assert.Empty(harness.Scene.BuildDisplayList().Commands.OfType<DrawPolyline>());
    }

    [Theory]
    [InlineData(1f)]
    [InlineData(1.25f)]
    [InlineData(1.5f)]
    [InlineData(2f)]
    public void A_wide_stroke_remains_visible_when_its_element_is_outside_the_viewport(float scale)
    {
        using var harness = new RenderHarness();
        harness.Scene.Styles.Theme = StyleSheets.Basic;
        var root = harness.Host.Render<GlyphStrokeProbe>();
        var probe = (GlyphStrokeProbe)root.Component;
        probe.StrokeStyle = "position: absolute; left: 80px; top: 20px; stroke-width: 40px;";
        probe.Refresh();
        harness.Host.RunPending();
        var image = harness.Render(80, 80, scale);
        Assert.Equal(40, Stroke(harness).Thickness);
        Assert.Equal(Color.Black, image.GetPixel((int)(75 * scale), (int)(30 * scale)));
    }
}
