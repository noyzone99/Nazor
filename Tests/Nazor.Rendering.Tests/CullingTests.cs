using Nazor.Controls;
using Nazor.Graphics;
using Nazor.Nss;
using Nazor.Style;
using Nazor.Testing;
using Color = Nazor.Graphics.Color;

namespace Nazor.Rendering.Tests;

public sealed class CullingPage : ComponentBase
{
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenComponent<Container>(0, typeof(CullingPage));
        builder.AddComponentParameter(1, "Class", "page");
        builder.AddAttribute(2, "ChildContent", (RenderFragment)(page =>
        {
            foreach (var (name, index) in new[] { "far", "near", "parent", "moved", "shadowed" }.Select((n, i) => (n, i)))
            {
                page.OpenComponent<Container>(10 + index * 10, typeof(CullingPage));
                page.AddComponentParameter(11 + index * 10, "Class", name);
                if (name == "parent")
                    page.AddAttribute(12 + index * 10, "ChildContent", (RenderFragment)(parent =>
                    {
                        parent.OpenComponent<Container>(0, typeof(CullingPage));
                        parent.AddComponentParameter(1, "Class", "kid");
                        parent.CloseComponent();
                    }));
                page.CloseComponent();
            }
            page.OpenComponent<TextBlock>(90, typeof(CullingPage));
            page.AddComponentParameter(91, "Class", "wide");
            page.AddAttribute(92, "ChildContent", (RenderFragment)(text => text.AddContent(0, "Testo lungo che esce dalla sua scatola")));
            page.CloseComponent();
            page.OpenComponent<Container>(100, typeof(CullingPage));
            page.AddComponentParameter(101, "Class", "list");
            page.AddAttribute(102, "ChildContent", (RenderFragment)(list =>
            {
                for (var i = 0; i < 10; i++)
                {
                    list.OpenComponent<Container>(0, typeof(CullingPage));
                    list.SetKey(i);
                    list.AddComponentParameter(1, "Class", "item");
                    list.CloseComponent();
                }
            }));
            page.CloseComponent();
        }));
        builder.CloseComponent();
    }
}

[Collection("StyleRegistry")]
public sealed class CullingTests : IDisposable
{
    private const string Sheet = """
        .page { width: 400px; height: 300px; }
        .far { position: absolute; left: 10px; top: 1000px; width: 20px; height: 20px; background: #ff0000; }
        .near { position: absolute; left: 10px; top: 10px; width: 20px; height: 20px; background: #00ff00; }
        .parent { position: absolute; left: 50px; top: 500px; width: 20px; height: 20px; background: #0000ff; }
        .kid { position: relative; top: -450px; width: 20px; height: 20px; background: #ffff00; }
        .moved { position: absolute; left: 90px; top: 400px; width: 20px; height: 20px; background: #ff00ff; transform: translateY(-300px); }
        .shadowed { position: absolute; left: 130px; top: 305px; width: 20px; height: 20px; background: #00ffff; box-shadow: 0 -10px 10px #000000; }
        .list { position: absolute; left: 200px; top: 0; width: 100px; height: 100px; overflow-y: auto; }
        .wide { position: absolute; left: -60px; top: 200px; width: 40px; white-space: nowrap; }
        .item { height: 50px; flex-shrink: 0; background: #808080; }
        """;

    private readonly RenderHarness harness = new();

    public CullingTests()
    {
        var sheet = NssCompiler.Compile("CullingPage", Sheet).StyleSheet;
        StyleRegistry.RegisterScoped(typeof(CullingPage), () => sheet);
        harness.Host.Render<CullingPage>();
        harness.Render(400, 300, 1);
    }

    public void Dispose() => harness.Dispose();

    private List<Color> Fills() => [.. harness.Scene.BuildDisplayList().Commands.OfType<FillRectangle>().Select(f => f.Color)];

    private static Color Rgb(uint rgb) => new((byte)(rgb >> 16), (byte)(rgb >> 8), (byte)rgb, 255);

    [Fact]
    public void Boxes_out_of_the_window_are_not_drawn()
    {
        var fills = Fills();
        Assert.Contains(Rgb(0x00ff00), fills);
        Assert.DoesNotContain(Rgb(0xff0000), fills);
        Assert.DoesNotContain(Rgb(0x0000ff), fills);
        Assert.Contains(Rgb(0xffff00), fills);
    }

    [Fact]
    public void Transforms_and_shadows_count_where_they_draw()
    {
        var fills = Fills();
        Assert.Contains(Rgb(0xff00ff), fills);
        Assert.Contains(Rgb(0x00ffff), fills);
        Assert.Contains(harness.Scene.BuildDisplayList().Commands.OfType<DrawShadow>(), s => s.Color == Rgb(0x000000));
    }

    [Fact]
    public void Text_past_the_end_of_its_box_counts_where_it_draws()
    {
        var text = Assert.Single(harness.Scene.BuildDisplayList().Commands.OfType<DrawText>());
        Assert.True(text.Origin.X < 0);
    }

    [Fact]
    public void A_scroll_viewer_draws_the_items_in_its_viewport()
    {
        Assert.Equal(2, Fills().Count(c => c == Rgb(0x808080)));
        var list = harness.Host.Renderer.RootComponents.Single().Elements().Single(e => e.Attributes.GetValueOrDefault("class") is "list");
        harness.Scene.ScrollBy(list, 75);
        harness.Render(400, 300, 1);
        Assert.Equal(3, Fills().Count(c => c == Rgb(0x808080)));
    }
}
