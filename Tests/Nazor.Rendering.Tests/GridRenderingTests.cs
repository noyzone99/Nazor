using Nazor.Controls;
using Nazor.Graphics;
using Nazor.Nss;
using Nazor.Style;

namespace Nazor.Rendering.Tests;

public sealed class GridPage : ComponentBase
{
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenComponent<Container>(0, typeof(GridPage));
        builder.AddComponentParameter(1, "Class", "page");
        builder.AddAttribute(2, "ChildContent", (RenderFragment)(page =>
        {
            Box(page, 3, "header");
            Box(page, 6, "side");
            Box(page, 9, "main");
            page.OpenComponent<TextBlock>(12, typeof(GridPage));
            page.AddComponentParameter(13, "Class", "note");
            page.AddAttribute(14, "ChildContent", (RenderFragment)(text => text.AddContent(15, "Total")));
            page.CloseComponent();
        }));
        builder.CloseComponent();
    }

    private static void Box(RenderTreeBuilder builder, int sequence, string name)
    {
        builder.OpenComponent<Container>(sequence, typeof(GridPage));
        builder.AddComponentParameter(sequence + 1, "Class", name);
        builder.CloseComponent();
    }
}

[Collection("StyleRegistry")]
public sealed class GridRenderingTests : IDisposable
{
    private readonly RenderHarness harness = new();

    public GridRenderingTests()
    {
        var result = NssCompiler.Compile("GridPage", """
            .page {
                display: grid;
                grid-template-columns: 80px 1fr;
                grid-template-rows: 30px 1fr auto;
                grid-template-areas: "header header" "side main" "side note";
                gap: 10px;
                height: 100%;
            }
            .header { grid-area: header; background: #ff0000; }
            .side { grid-area: side; background: #00ff00; }
            .main { grid-area: main; background: #0000ff; }
            .note { grid-area: note; justify-self: end; }
            """);
        Assert.True(result.Success, string.Join("\n", result.Diagnostics));
        var sheet = result.StyleSheet;
        StyleRegistry.RegisterScoped(typeof(GridPage), () => sheet);
        harness.Host.Render<GridPage>();
    }

    public void Dispose() => harness.Dispose();

    private Rect Bounds(string name) =>
        harness.Scene.RootLayout.Children.SelectMany(Flatten).Single(n => n.Tag is ElementNode e && e.Attributes.GetValueOrDefault("class") as string == name).RoundedFrame;

    private static IEnumerable<Nazor.Layout.LayoutNode> Flatten(Nazor.Layout.LayoutNode node) => node.Children.SelectMany(Flatten).Prepend(node);

    [Fact]
    public void Areas_and_tracks_place_the_cells()
    {
        var image = harness.Render(300, 200, 1);
        Assert.Equal(new Rect(0, 0, 300, 30), Bounds("header"));
        Assert.Equal(new Rect(0, 40, 80, 160), Bounds("side"));
        var main = Bounds("main");
        var note = Bounds("note");
        Assert.Equal(new Rect(90, 40, 210, 200 - 40 - 10 - note.Height), main);
        Assert.Equal(300, note.Right, 3);
        Assert.True(note.Width < 210, $"justify-self: end keeps the text at its width, {note}");

        Assert.Equal(0xff, image.GetPixel(150, 15).R);
        Assert.Equal(0xff, image.GetPixel(40, 100).G);
        Assert.Equal(0xff, image.GetPixel(150, 60).B);
        Assert.True(image.GetPixel(85, 100) is { R: > 240, G: > 240, B: > 240 });
    }

    [Fact]
    public void The_cells_follow_the_viewport()
    {
        harness.Render(400, 300, 1.5f);
        Assert.Equal(new Rect(90, 40, 310, Bounds("main").Height), Bounds("main"));
        Assert.Equal(new Rect(0, 40, 80, 260), Bounds("side"));
    }
}
