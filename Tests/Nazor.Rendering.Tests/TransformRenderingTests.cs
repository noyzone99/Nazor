using Nazor.Controls;
using Nazor.Graphics;
using Nazor.Nss;
using Nazor.Style;
using Nazor.Testing;

namespace Nazor.Rendering.Tests;

public sealed class TransformPage : ComponentBase
{
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenComponent<Container>(0, typeof(TransformPage));
        builder.AddComponentParameter(1, "Class", "stage");
        builder.AddAttribute(2, "ChildContent", (RenderFragment)(stage =>
        {
            Box(stage, 3, "turned");
            Box(stage, 6, "after");
            stage.OpenComponent<Container>(9, typeof(TransformPage));
            stage.AddComponentParameter(10, "Class", "holder");
            stage.AddAttribute(11, "ChildContent", (RenderFragment)(holder => Box(holder, 12, "badge")));
            stage.CloseComponent();
        }));
        builder.CloseComponent();
    }

    private static void Box(RenderTreeBuilder builder, int sequence, string name)
    {
        builder.OpenComponent<Container>(sequence, typeof(TransformPage));
        builder.AddComponentParameter(sequence + 1, "Class", name);
        builder.CloseComponent();
    }
}

[Collection("StyleRegistry")]
public sealed class TransformRenderingTests : IDisposable
{
    private readonly RenderHarness harness = new();
    private readonly Nazor.Rendering.RootComponent root;

    public TransformRenderingTests()
    {
        Use("");
        root = harness.Host.Render<TransformPage>();
    }

    public void Dispose() => harness.Dispose();

    private void Use(string extra)
    {
        var result = NssCompiler.Compile("TransformPage", """
            .stage { display: flex; flex-direction: column; padding: 20px; gap: 10px; align-items: flex-start; background: #ffffff; }
            .turned { width: 100px; height: 40px; background: #ff0000; }
            .after { width: 100px; height: 40px; background: #0000ff; }
            .holder { width: 200px; height: 50px; }
            .badge { position: absolute; right: 0; top: 0; width: 20px; height: 20px; background: #00ff00; }
            """ + extra);
        Assert.True(result.Success, string.Join("\n", result.Diagnostics));
        var sheet = result.StyleSheet;
        StyleRegistry.RegisterScoped(typeof(TransformPage), () => sheet);
        harness.Scene.Styles.InvalidateAll();
    }

    private ElementNode Element(string name) =>
        root.Elements("Container").Single(e => e.Attributes.GetValueOrDefault("class") as string == name);

    private static void Near(Rect expected, Rect actual)
    {
        Assert.Equal(expected.X, actual.X, 0.01f);
        Assert.Equal(expected.Y, actual.Y, 0.01f);
        Assert.Equal(expected.Width, actual.Width, 0.01f);
        Assert.Equal(expected.Height, actual.Height, 0.01f);
    }

    [Fact]
    public void A_rotation_turns_the_box_about_its_center_and_draws_inside_a_transform()
    {
        Use(".turned { transform: rotate(90deg); }");
        var image = harness.Render(300, 300, 1);
        Assert.Equal(new Rect(20, 70, 100, 40), harness.Scene.GetBounds(Element("after")));
        Near(new Rect(50, -10, 40, 100), harness.Scene.GetBounds(Element("turned")));
        var commands = harness.Scene.BuildDisplayList().Commands;
        var push = commands.OfType<PushTransform>().Single();
        Assert.Equal(new Point(90, -10), push.Matrix.Transform(new Point(20, 20)), new PointComparer());
        Assert.Equal(new Graphics.Color(255, 0, 0, 255), image.GetPixel(70, 5));
        Assert.Equal(new Graphics.Color(255, 255, 255, 255), image.GetPixel(25, 25));
    }

    [Fact]
    public void Translations_resolve_percentages_against_the_box_and_hit_testing_follows()
    {
        Use(".turned { transform: translate(50%, 10px); }");
        harness.Render(300, 300, 1);
        Assert.Equal(new Rect(70, 30, 100, 40), harness.Scene.GetBounds(Element("turned")));
        Assert.Same(Element("turned"), harness.Scene.HitTest(new Point(160, 65)));
        Assert.NotSame(Element("turned"), harness.Scene.HitTest(new Point(30, 25)));
    }

    [Fact]
    public void A_scale_of_zero_hides_the_box_from_the_pointer()
    {
        Use(".turned { transform: scale(0); }");
        harness.Render(300, 300, 1);
        Assert.NotSame(Element("turned"), harness.Scene.HitTest(new Point(70, 40)));
        Assert.Equal(0, harness.Scene.GetBounds(Element("turned")).Width);
    }

    [Fact]
    public void A_composition_recording_keeps_content_that_can_enter_the_viewport()
    {
        Use("""
            .turned { animation: enter 5s linear both; }
            @keyframes enter { from { transform: translateX(-200px); } to { transform: translateX(0px); } }
            .after { transform: translateX(-200px); }
            """);
        harness.Scene.Transitions.ReducedMotion = false;
        harness.Scene.Viewport = new Size(300, 300);
        harness.Scene.Update();

        var commands = harness.Scene.BuildCompositionList().Commands;
        Assert.Single(commands.OfType<PushComposition>());
        Assert.Contains(commands.OfType<FillRectangle>(), fill => fill.Color == new Graphics.Color(255, 0, 0, 255));
        Assert.DoesNotContain(commands.OfType<FillRectangle>(), fill => fill.Color == new Graphics.Color(0, 0, 255, 255));
    }

    [Fact]
    public void The_origin_and_the_order_of_the_functions_follow_css()
    {
        Use(".turned { transform-origin: left top; transform: translateX(10px) rotate(90deg); }");
        harness.Render(300, 300, 1);
        Near(new Rect(-10, 20, 40, 100), harness.Scene.GetBounds(Element("turned")));
        Use(".turned { transform-origin: left top; transform: rotate(90deg) translateX(10px); }");
        harness.Render(300, 300, 1);
        Near(new Rect(-20, 30, 40, 100), harness.Scene.GetBounds(Element("turned")));
    }

    [Fact]
    public void A_transformed_box_paints_over_the_boxes_that_follow_it()
    {
        Use(".turned { transform: translateY(30px); }");
        var image = harness.Render(300, 300, 1);
        Assert.Equal(new Graphics.Color(255, 0, 0, 255), image.GetPixel(70, 85));
        Assert.Same(Element("turned"), harness.Scene.HitTest(new Point(70, 85)));
        Assert.Same(Element("after"), harness.Scene.HitTest(new Point(70, 105)));
    }

    [Fact]
    public void A_transformed_box_is_the_containing_block_of_absolute_descendants()
    {
        harness.Render(300, 300, 1);
        Assert.Equal(new Rect(280, 0, 20, 20), harness.Scene.GetBounds(Element("badge")));
        Use(".holder { transform: translateX(5px); }");
        harness.Render(300, 300, 1);
        Assert.Equal(new Rect(205, 120, 20, 20), harness.Scene.GetBounds(Element("badge")));
    }

    [Fact]
    public void Group_opacity_covers_the_transformed_children()
    {
        Use(".stage { opacity: 0.5; } .turned { transform: translateX(400px); }");
        harness.Render(300, 300, 1);
        var layer = harness.Scene.BuildDisplayList().Commands.OfType<PushOpacity>().Single();
        Assert.True(layer.Bounds.Right >= 520, $"{layer.Bounds}");
    }

    private sealed class PointComparer : IEqualityComparer<Point>
    {
        public bool Equals(Point a, Point b) => Math.Abs(a.X - b.X) < 0.01f && Math.Abs(a.Y - b.Y) < 0.01f;

        public int GetHashCode(Point point) => 0;
    }
}
