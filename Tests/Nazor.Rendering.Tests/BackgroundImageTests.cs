using Nazor.Controls;
using Nazor.Graphics;
using Nazor.Nss;
using Nazor.Style;
using Nazor.Testing;

namespace Nazor.Rendering.Tests;

public sealed class Tiles : ComponentBase
{
    public string[] Boxes { get; set; } = [];

    public void Refresh() => StateHasChanged();

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenComponent<Container>(0, typeof(Tiles));
        builder.AddComponentParameter(1, "Class", "page");
        builder.AddAttribute(2, "ChildContent", (RenderFragment)(page =>
        {
            for (var i = 0; i < Boxes.Length; i++)
            {
                page.OpenComponent<Container>(3, typeof(Tiles));
                page.SetKey(i);
                page.AddComponentParameter(4, "Class", Boxes[i]);
                page.CloseComponent();
            }
        }));
        builder.CloseComponent();
    }
}

[Collection("StyleRegistry")]
public sealed class BackgroundImageTests : IDisposable
{
    private readonly RenderHarness harness = new();
    private readonly DirectoryInfo folder = Directory.CreateDirectory(Path.Combine(AppContext.BaseDirectory, $"test-images-{Guid.NewGuid():N}"));
    private RootComponent root = null!;

    public void Dispose()
    {
        harness.Dispose();
        folder.Delete(recursive: true);
    }

    private string Png(string name, int width, int height)
    {
        var pixels = new byte[width * height * 4];
        for (var i = 0; i < width * height; i++)
            (pixels[i * 4], pixels[i * 4 + 1], pixels[i * 4 + 2], pixels[i * 4 + 3]) = i % width < width / 2 ? ((byte)0, (byte)0, (byte)255, (byte)255) : ((byte)255, (byte)0, (byte)0, (byte)255);
        File.WriteAllBytes(Path.Combine(folder.FullName, name), harness.Graphics.EncodePng(new RenderedImage(width, height, pixels)));
        return $"{folder.Name}/{name}";
    }

    private RenderedImage Show(string nss, params string[] boxes)
    {
        var result = NssCompiler.Compile("Tiles", ".page { display: flex; flex-direction: column; align-items: flex-start; background: #ffffff; } " + nss);
        Assert.True(result.Success, string.Join("\n", result.Diagnostics));
        var sheet = result.StyleSheet;
        StyleRegistry.RegisterScoped(typeof(Tiles), () => sheet);
        root = harness.Host.Render<Tiles>();
        ((Tiles)root.Component).Boxes = boxes;
        ((Tiles)root.Component).Refresh();
        harness.Host.RunPending();
        harness.Render(200, 200, 1);
        Assert.True(harness.Scene.WhenImagesLoaded().Wait(TimeSpan.FromSeconds(10)));
        return harness.Render(200, 200, 1);
    }

    private IEnumerable<T> Commands<T>() => harness.Scene.BuildDisplayList().Commands.OfType<T>();

    [Fact]
    public void An_image_tiles_its_box_from_the_top_left_corner_by_default()
    {
        var url = Png("tile.png", 4, 2);
        var image = Show($$""".box { width: 20px; height: 10px; background-image: url({{url}}); }""", "box");
        var fill = Commands<FillImage>().Single();
        Assert.Equal((new Rect(0, 0, 20, 10), new Rect(0, 0, 4, 2), true, true), (fill.Area, fill.Tile, fill.RepeatX, fill.RepeatY));
        Assert.Equal(image.GetPixel(1, 1), image.GetPixel(5, 5));
        Assert.Equal(new Graphics.Color(255, 0, 0, 255), image.GetPixel(1, 1));
        Assert.Equal(new Graphics.Color(0, 0, 255, 255), image.GetPixel(7, 1));
    }

    [Theory]
    [InlineData("cover", 0, -10, 40, 40)]
    [InlineData("contain", 10, 0, 20, 20)]
    [InlineData("10px auto", 15, 5, 10, 10)]
    [InlineData("50% 50%", 10, 5, 20, 10)]
    public void Sizes_keep_the_proportions_and_positions_center(string size, float x, float y, float width, float height)
    {
        var url = Png("square.png", 8, 8);
        Show($$""".box { width: 40px; height: 20px; background: url("{{url}}") center / {{size}} no-repeat; }""", "box");
        var fill = Commands<FillImage>().Single();
        Assert.Equal(new Rect(x, y, width, height), fill.Tile);
        Assert.False(fill.RepeatX || fill.RepeatY);
    }

    [Fact]
    public void Layers_are_drawn_first_on_top_and_lists_repeat()
    {
        var url = Png("layer.png", 2, 2);
        Show($$""".box { width: 20px; height: 20px; background: url({{url}}) right bottom no-repeat, linear-gradient(red, blue) #00ff00; border-radius: 4px; }""", "box");
        var commands = harness.Scene.BuildDisplayList().Commands;
        var fill = commands.OfType<FillRectangle>().Single(f => f.Color == new Graphics.Color(0, 255, 0, 255));
        var gradient = commands.OfType<FillGradient>().Single();
        var image = commands.OfType<FillImage>().Single();
        Assert.True(commands.ToList().IndexOf(fill) < commands.ToList().IndexOf(gradient));
        Assert.True(commands.ToList().IndexOf(gradient) < commands.ToList().IndexOf(image));
        Assert.Equal(new Rect(18, 18, 2, 2), image.Tile);
        Assert.Equal(4, image.Radius.TopLeft.X);
        Assert.Equal(new Rect(0, 0, 20, 20), gradient.Rect);
    }

    [Fact]
    public void A_smaller_gradient_repeats_in_cells_clipped_to_the_box()
    {
        Show(".box { width: 30px; height: 20px; background: linear-gradient(red, blue) 0 0 / 10px 10px repeat-x; }", "box");
        var cells = Commands<FillGradient>().Select(g => g.Rect).ToArray();
        Assert.Equal([new Rect(0, 0, 10, 10), new Rect(10, 0, 10, 10), new Rect(20, 0, 10, 10)], cells);
        Assert.Single(Commands<PushClip>());
    }

    [Fact]
    public void An_svg_background_and_a_missing_one()
    {
        File.WriteAllText(Path.Combine(folder.FullName, "dot.svg"), """<svg width="10" height="10"><circle cx="5" cy="5" r="5" fill="#0000ff"/></svg>""");
        var image = Show($$""".box { width: 10px; height: 10px; background: url({{folder.Name}}/dot.svg) no-repeat; } .missing { width: 10px; height: 10px; background-image: url(none.png); }""",
            "box", "missing");
        Assert.True(Commands<FillImage>().Single().Image.IsVector);
        Assert.Equal(new Graphics.Color(0, 0, 255, 255), image.GetPixel(5, 5));
    }
}
