using Nazor.Controls;
using Nazor.Graphics;
using Nazor.Style;
using Nazor.Testing;

namespace Nazor.Rendering.Tests;

public sealed class CircularDecorationProbe : ComponentBase
{
    public string Style { get; set; } = "";
    public void Refresh() => StateHasChanged();

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "Box");
        builder.AddAttribute(1, "style", "position: absolute; left: 4.15px; top: 10.05px; width: 12px; height: 12px; border-radius: 50%; background: red; border: 1px solid blue;" + Style);
        builder.CloseElement();
    }
}

public sealed class CircularDecorationTests
{
    [Theory]
    [InlineData(1f)]
    [InlineData(1.25f)]
    [InlineData(1.5f)]
    [InlineData(2f)]
    public void Circular_decorations_keep_the_unsnapped_square_without_changing_layout_or_input(float scale)
    {
        using var harness = Create("background-image: radial-gradient(circle farthest-side, white, transparent); box-shadow: 0 0 2px black; outline: 1px solid black;", out var box);
        harness.Render(40, 40, scale);
        var node = harness.Scene.GetLayoutNode(box)!;
        var raw = node.AbsoluteFrame;
        var list = harness.Scene.BuildDisplayList();
        var fill = Assert.Single(list.Commands.OfType<FillRectangle>());
        Assert.Equal(raw, fill.Rect);
        Assert.Equal(new CornerRadius(6, 6), fill.Radius);
        Assert.Equal(raw, Assert.Single(list.Commands.OfType<FillGradient>()).Rect);
        Assert.Equal(raw, Assert.Single(list.Commands.OfType<DrawShadow>()).Rect);
        var borders = list.Commands.OfType<DrawBorder>().ToArray();
        Assert.Equal(raw, borders[0].Rect);
        Assert.Equal(raw.X + raw.Width / 2, borders[1].Rect.X + borders[1].Rect.Width / 2, 4);
        Assert.Equal(raw.Y + raw.Height / 2, borders[1].Rect.Y + borders[1].Rect.Height / 2, 4);
        Assert.Equal(borders[1].Rect.Width, borders[1].Rect.Height);
        Assert.Equal(node.RoundedFrame, harness.Scene.GetBounds(box));
        Assert.Same(box, harness.Scene.HitTest(new Point(node.RoundedFrame.X + 6, node.RoundedFrame.Y + 6)));
    }

    [Theory]
    [InlineData("border-radius: 6px;", true)]
    [InlineData("border-radius: 999px;", true)]
    [InlineData("border-radius: 2px;", false)]
    [InlineData("border-radius: 50% / 60%;", false)]
    [InlineData("border-radius: 50% 2px;", false)]
    [InlineData("height: 14px;", false)]
    public void Only_square_boxes_with_uniform_circular_radii_use_the_original_geometry(string style, bool circular)
    {
        using var harness = Create(style, out var box);
        harness.Render(40, 40, 1.25f);
        var node = harness.Scene.GetLayoutNode(box)!;
        Assert.NotEqual(node.AbsoluteFrame, node.RoundedFrame);
        Assert.Equal(circular ? node.AbsoluteFrame : node.RoundedFrame,
            Assert.Single(harness.Scene.BuildDisplayList().Commands.OfType<FillRectangle>()).Rect);
    }

    [Fact]
    public void Culling_includes_the_unsnapped_circle_at_the_viewport_edge()
    {
        using var harness = Create("left: 19.85px;", out var box);
        harness.Render(20, 40, 1.25f);
        var node = harness.Scene.GetLayoutNode(box)!;
        Assert.Equal(20, node.RoundedFrame.X);
        Assert.Equal(node.AbsoluteFrame, Assert.Single(harness.Scene.BuildDisplayList().Commands.OfType<FillRectangle>()).Rect);
    }

    private static RenderHarness Create(string style, out ElementNode box)
    {
        var harness = new RenderHarness();
        harness.Scene.Styles.Theme = StyleSheets.Basic;
        var root = harness.Host.Render<CircularDecorationProbe>();
        var probe = (CircularDecorationProbe)root.Component;
        probe.Style = style;
        probe.Refresh();
        harness.Host.RunPending();
        box = root.Single("Box");
        return harness;
    }
}
