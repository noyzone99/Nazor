using Nazor.Controls;
using Nazor.Graphics;
using Nazor.Rendering;
using Nazor.Samples.Counter;
using Nazor.Testing;

namespace Nazor.Rendering.Tests;

public sealed class CounterRenderingTests
{
    private static readonly Color Dark = new(0x30, 0x30, 0x30, 255);

    [Fact]
    public void Counter_geometry_follows_its_styles()
    {
        using var harness = new RenderHarness();
        harness.Host.Render<Counter>();
        harness.Render(320, 200, 1);

        Assert.Equal("""
            Container.counter [0, 0, 320x83]
              TextBlock [16, 16, 288x17]
              Button [16, 41, 288x26]
                ButtonLabel [128, 45, 64x17]

            """.ReplaceLineEndings("\n"), harness.Scene.DescribeLayout());
    }

    [Theory]
    [InlineData(320, 200, 1f)]
    [InlineData(320, 200, 1.5f)]
    [InlineData(320, 200, 2f)]
    [InlineData(200, 120, 1f)]
    [InlineData(200, 120, 1.5f)]
    [InlineData(200, 120, 2f)]
    public void Counter_images_match_the_references(float width, float height, float scale)
    {
        using var harness = new RenderHarness();
        harness.Host.Render<Counter>();
        var image = harness.Render(width, height, scale);

        Assert.Equal(((int)MathF.Ceiling(width * scale), (int)MathF.Ceiling(height * scale)), (image.Width, image.Height));
        ImageAssert.MatchesBaseline(harness.Graphics, image, $"counter-{width}x{height}@{scale:0.#}x");
    }

    [Theory]
    [InlineData(1f)]
    [InlineData(1.25f)]
    [InlineData(1.5f)]
    [InlineData(2f)]
    public void Text_measurement_does_not_depend_on_the_scale(float scale)
    {
        using var harness = new RenderHarness();
        harness.Host.Render<Counter>();
        harness.Render(320, 200, scale);
        var text = harness.Scene.BuildDisplayList().Commands.OfType<DrawText>().First(t => t.Layout.Text == "Count: 0");

        using var reference = new RenderHarness();
        reference.Host.Render<Counter>();
        reference.Render(320, 200, 1);
        var expected = reference.Scene.BuildDisplayList().Commands.OfType<DrawText>().First(t => t.Layout.Text == "Count: 0");

        Assert.Equal(expected.Layout.Width, text.Layout.Width);
        Assert.Equal(expected.Layout.Height, text.Layout.Height);
        Assert.Equal(text.Layout.Width, text.Layout.Runs.Sum(r => r.Width), 3);
    }

    [Theory]
    [InlineData(1f)]
    [InlineData(1.5f)]
    [InlineData(2f)]
    public void Edges_land_on_physical_pixels_and_colors_come_from_the_scoped_styles(float scale)
    {
        using var harness = new RenderHarness();
        var root = harness.Host.Render<Counter>();
        var image = harness.Render(320, 200, scale);
        var button = harness.Scene.GetBounds(root.Single("Button"));

        foreach (var edge in new[] { button.X, button.Y, button.Right, button.Bottom })
            Assert.Equal(MathF.Round(edge * scale), edge * scale, 3);
        Assert.Equal(Dark, image.GetPixel((int)((button.X + 6) * scale), (int)((button.Y + button.Height / 2) * scale)));
        Assert.Equal(Color.White, image.GetPixel((int)(5 * scale), (int)(150 * scale)));
    }

    [Fact]
    public void Text_pixels_are_drawn_where_the_text_is_measured()
    {
        using var harness = new RenderHarness();
        var root = harness.Host.Render<Counter>();
        var image = harness.Render(320, 200, 2);
        var text = harness.Scene.BuildDisplayList().Commands.OfType<DrawText>().Single(t => t.Layout.Text == "Count: 0");
        var box = new Rect(text.Origin.X, text.Origin.Y, text.Layout.Width, text.Layout.Height);

        int Ink(Rect area)
        {
            var count = 0;
            for (var y = (int)(area.Y * 2); y < (int)(area.Bottom * 2); y++)
                for (var x = (int)(area.X * 2); x < (int)(area.Right * 2); x++)
                    if (image.GetPixel(x, y).R < 128) count++;
            return count;
        }

        Assert.True(Ink(box) > 50, "No ink inside the measured text box.");
        Assert.Equal(0, Ink(new Rect(box.Right + 2, box.Y, 60, box.Height)));
        Assert.Same(root.Single("TextBlock"), harness.Scene.HitTest(new Point(box.X + 2, box.Y + box.Height / 2)));
    }

    [Fact]
    public async Task Hover_and_click_update_the_rendered_surface()
    {
        using var harness = new RenderHarness();
        var root = harness.Host.Render<Counter>();
        harness.Render(320, 200, 1);
        var buttonElement = root.Single("Button");
        var bounds = harness.Scene.GetBounds(buttonElement);
        var probe = new Point(bounds.X + 6, bounds.Y + bounds.Height / 2);
        Assert.Same(buttonElement, harness.Scene.HitTest(probe));

        buttonElement.SetState(ElementStates.Hover, true);
        var hovered = harness.Render(320, 200, 1);
        Assert.Equal(new Color(0, 0x78, 0xd4, 255), hovered.GetPixel((int)probe.X, (int)probe.Y));

        await harness.Host.ClickAsync(buttonElement);
        harness.Render(320, 200, 1);
        Assert.Contains(harness.Scene.BuildDisplayList().Commands.OfType<DrawText>(), t => t.Layout.Text == "Count: 1");
    }

    [Fact]
    public void Long_text_wraps_to_the_available_width_and_grows_its_box()
    {
        using var harness = new RenderHarness();
        var root = harness.Host.Render<Paragraph>();
        harness.Render(400, 300, 1);
        var wide = harness.Scene.GetBounds(root.Single("TextBlock"));
        harness.Render(120, 300, 1);
        var narrow = harness.Scene.GetBounds(root.Single("TextBlock"));
        var text = harness.Scene.BuildDisplayList().Commands.OfType<DrawText>().Single();

        Assert.Equal(400, wide.Width);
        Assert.Equal(120, narrow.Width);
        Assert.True(narrow.Height > wide.Height * 2, $"{narrow.Height} vs {wide.Height}");
        Assert.All(text.Layout.Lines, line => Assert.True(line.Width <= 120.01f));
        Assert.Equal(narrow.Height, text.Layout.Height, 3);
    }

    public sealed class Paragraph : ComponentBase
    {
        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenComponent<TextBlock>(0, typeof(Paragraph));
            builder.AddAttribute(1, "ChildContent", (RenderFragment)(b => b.AddContent(2, "La misura del testo è la stessa del disegno: più righe quando lo spazio è poco.")));
            builder.CloseComponent();
        }
    }
}
