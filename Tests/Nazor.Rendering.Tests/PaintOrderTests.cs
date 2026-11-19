using Nazor.Controls;
using Nazor.Nss;
using Nazor.Rendering;
using Nazor.Style;

namespace Nazor.Rendering.Tests;

public sealed class Overlapping : ComponentBase
{
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenComponent<Container>(0, typeof(Overlapping));
        builder.AddComponentParameter(1, "Class", "row");
        builder.AddAttribute(2, "ChildContent", (RenderFragment)(row =>
        {
            Box(row, 3, "first");
            Box(row, 6, "second");
            Box(row, 9, "third");
        }));
        builder.CloseComponent();
    }

    private static void Box(RenderTreeBuilder builder, int sequence, string name)
    {
        builder.OpenComponent<Container>(sequence, typeof(Overlapping));
        builder.AddComponentParameter(sequence + 1, "Class", name);
        builder.CloseComponent();
    }
}

[Collection("StyleRegistry")]
public sealed class PaintOrderTests : IDisposable
{
    private const string Boxes = """
        .row { display: flex; padding: 10px; }
        .first, .second, .third { width: 100px; height: 100px; flex-shrink: 0; }
        .second, .third { margin-left: -50px; }
        .first { background: #ff0000; }
        .second { background: #00ff00; }
        .third { background: #0000ff; }
        """;

    private readonly RenderHarness harness = new();

    public void Dispose() => harness.Dispose();

    private void Show(string extra)
    {
        var sheet = NssCompiler.Compile("Overlapping", Boxes + extra).StyleSheet;
        StyleRegistry.RegisterScoped(typeof(Overlapping), () => sheet);
        harness.Host.Render<Overlapping>();
    }

    private string HitClass(float x, float y) =>
        harness.Scene.HitTest(new Nazor.Graphics.Point(x, y)) is { } hit ? (string)hit.Attributes["class"]! : "";

    [Fact]
    public void Later_items_paint_on_top_of_earlier_ones()
    {
        Show("");
        var image = harness.Render(300, 120, 1);
        Assert.Equal(0xff, image.GetPixel(80, 50).G);
        Assert.Equal(0xff, image.GetPixel(130, 50).B);
        Assert.Equal("second", HitClass(80, 50));
        Assert.Equal("third", HitClass(130, 50));
    }

    [Fact]
    public void Order_moves_an_item_in_the_layout_and_in_the_painting()
    {
        Show(".first { order: 1; }");
        var image = harness.Render(300, 120, 1);
        Assert.Equal(0xff, image.GetPixel(130, 50).R);
        Assert.Equal("first", HitClass(130, 50));
        Assert.Equal("third", HitClass(80, 50));
    }

    [Fact]
    public void Positioned_boxes_paint_after_the_boxes_in_the_flow()
    {
        Show(".first { position: relative; }");
        var image = harness.Render(300, 120, 1);
        Assert.Equal(0xff, image.GetPixel(80, 50).R);
        Assert.Equal("first", HitClass(80, 50));
        Assert.Equal("third", HitClass(130, 50));
    }

    [Fact]
    public void Z_index_orders_siblings_negative_values_below_the_flow()
    {
        Show(".first { position: relative; z-index: 2; } .third { position: relative; z-index: -1; }");
        var image = harness.Render(300, 120, 1);
        Assert.Equal(0xff, image.GetPixel(80, 50).R);
        Assert.Equal(0xff, image.GetPixel(130, 50).G);
        Assert.Equal("first", HitClass(80, 50));
        Assert.Equal("second", HitClass(130, 50));
    }

    [Fact]
    public void Z_index_applies_to_flex_items_without_position()
    {
        Show(".first { z-index: 1; }");
        harness.Render(300, 120, 1);
        Assert.Equal("first", HitClass(80, 50));
    }

    [Fact]
    public void Pointer_events_none_lets_the_pointer_through_to_what_is_underneath()
    {
        Show(".third { pointer-events: none; }");
        harness.Render(300, 120, 1);
        Assert.Equal("second", HitClass(130, 50));
        Assert.Equal("row", HitClass(200, 50));
    }

    [Fact]
    public void Children_with_pointer_events_auto_still_take_the_pointer()
    {
        Show(".row { pointer-events: none; } .second { pointer-events: auto; }");
        harness.Render(300, 120, 1);
        Assert.Equal("second", HitClass(80, 50));
        Assert.Equal("", HitClass(30, 50));
    }

    [Fact]
    public void Absolute_boxes_paint_over_their_siblings_where_their_insets_put_them()
    {
        Show(".row { position: relative; } .second { position: absolute; right: 0; bottom: 0; width: 40px; height: 40px; margin: 0; }");
        var image = harness.Render(300, 120, 1);
        Assert.Equal(0xff, image.GetPixel(280, 100).G);
        Assert.Equal("second", HitClass(280, 100));
        Assert.Equal("third", HitClass(80, 50));
    }
}
