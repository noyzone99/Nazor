using Nazor.Controls;
using Nazor.Graphics;
using Nazor.Rendering;
using Nazor.Samples.Counter;
using Nazor.Testing;

namespace Nazor.Diagnostics.Tests;

public sealed class Boxed : ComponentBase
{
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenComponent<Container>(0);
        builder.AddComponentParameter(1, "Style", "display: flex; align-items: flex-start");
        builder.AddComponentParameter(2, "ChildContent", (RenderFragment)(content =>
        {
            content.OpenComponent<Container>(0);
            content.AddComponentParameter(1, "Class", "moved");
            content.AddComponentParameter(2, "Style", "margin: 4px; padding: 3px; border: 2px solid red; width: 50px; height: 20px; transform: translateX(10px)");
            content.CloseComponent();
            content.OpenComponent<Container>(3);
            content.AddComponentParameter(4, "Class", "hidden");
            content.AddComponentParameter(5, "Style", "display: none");
            content.CloseComponent();
        }));
        builder.CloseComponent();
    }
}

public sealed class BoxTests : IDisposable
{
    private readonly DiagnosticsHarness harness = new();

    public void Dispose() => harness.Dispose();

    private DevToolsSession Session => harness.Session;

    private int Child(int index) => Session.Get(Session.Roots[0])!.Children[index];

    [Fact]
    public void The_button_of_the_counter_has_the_padding_and_border_of_the_theme()
    {
        harness.Host.Render<Counter>();
        var id = Child(1);
        var box = Session.Box(id)!;
        Assert.Equal((new Thickness(10, 2, 10, 2), new Thickness(1, 1, 1, 1), new Thickness(0, 0, 0, 0)), (box.Padding, box.Border, box.Margin));
        Assert.Equal(harness.Scene.GetBounds(Session.ElementOf(id)!), box.BorderBox);
        Assert.Equal(box.BorderBox, box.MarginBox);
        Assert.Equal(box.BorderBox.Deflate(new Thickness(1, 1, 1, 1)), box.PaddingBox);
        Assert.Equal(box.PaddingBox.Deflate(box.Padding), box.ContentBox);
        Assert.Equal(box.BorderBox, box.Physical(box.BorderBox));
    }

    [Fact]
    public void Transforms_move_every_box_and_margins_surround_the_border()
    {
        harness.Host.Render<Boxed>();
        var box = Session.Box(Child(0))!;
        Assert.Equal(new Rect(4 + 10, 4, 50, 20), box.BorderBox);
        Assert.Equal(new Rect(10, 0, 58, 28), box.MarginBox);
        Assert.Equal(new Rect(16, 6, 46, 16), box.PaddingBox);
        Assert.Equal(new Rect(19, 9, 40, 10), box.ContentBox);
        Assert.Null(Session.Box(Child(1)));
        Assert.Null(Session.Box(9999));
    }

    [Fact]
    public void Physical_boxes_follow_the_scale_of_the_window()
    {
        harness.Scene.Scale = 1.5f;
        harness.Host.Render<Boxed>();
        var box = Session.Box(Child(0))!;
        Assert.Equal(1.5f, box.Scale);
        Assert.Equal(new Rect(box.BorderBox.X * 1.5f, box.BorderBox.Y * 1.5f, box.BorderBox.Width * 1.5f, box.BorderBox.Height * 1.5f), box.Physical(box.BorderBox));
    }
}
