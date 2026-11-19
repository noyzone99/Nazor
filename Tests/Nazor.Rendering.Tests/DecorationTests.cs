using Nazor.Graphics.Text;
using Nazor.Animation;
using Nazor.Controls;
using Nazor.Graphics;
using Nazor.Nss;
using Nazor.Presentation;
using Nazor.Skia;
using Nazor.Style;
using Nazor.Testing;

namespace Nazor.Rendering.Tests;

public sealed class Decorated : ComponentBase
{
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenComponent<Container>(0, typeof(Decorated));
        builder.AddComponentParameter(1, "Class", "page");
        builder.AddAttribute(2, "ChildContent", (RenderFragment)(page =>
        {
            page.OpenComponent<Container>(3, typeof(Decorated));
            page.AddComponentParameter(4, "Class", "box");
            page.CloseComponent();
            page.OpenComponent<Container>(5, typeof(Decorated));
            page.AddComponentParameter(6, "Class", "next");
            page.CloseComponent();
        }));
        builder.CloseComponent();
    }
}

internal sealed class FloatPairComparer : IEqualityComparer<(float, float)>
{
    public bool Equals((float, float) x, (float, float) y) => MathF.Abs(x.Item1 - y.Item1) < 1e-3f && MathF.Abs(x.Item2 - y.Item2) < 1e-3f;

    public int GetHashCode((float, float) obj) => 0;
}

[Collection("StyleRegistry")]
public sealed class DecorationTests : IDisposable
{
    private const string Page = """
        .page { display: flex; align-items: flex-start; padding: 20px; gap: 20px; }
        .box { width: 100px; height: 60px; background: #ffffff; }
        .next { width: 20px; height: 20px; background: #808080; }
        """;

    private readonly RenderHarness harness = new();

    public void Dispose() => harness.Dispose();

    private RenderedImage Show(string box)
    {
        var result = NssCompiler.Compile("Decorated", Page + ".box {" + box + "}");
        Assert.True(result.Success, string.Join("\n", result.Diagnostics));
        var sheet = result.StyleSheet;
        StyleRegistry.RegisterScoped(typeof(Decorated), () => sheet);
        harness.Host.Render<Decorated>();
        harness.Scene.Background = Nazor.Graphics.Color.White;
        return harness.Render(200, 100, 1);
    }

    private string Commands => harness.Scene.BuildDisplayList().Describe();

    private Rect Bounds(string name) =>
        harness.Scene.RootLayout.Children.SelectMany(Flatten).Single(n => n.Tag is ElementNode e && e.Attributes.GetValueOrDefault("class") as string == name).RoundedFrame;

    private static IEnumerable<Nazor.Layout.LayoutNode> Flatten(Nazor.Layout.LayoutNode node) => node.Children.SelectMany(Flatten).Prepend(node);

    private static bool Is(Nazor.Graphics.Color pixel, byte r, byte g, byte b) =>
        Math.Abs(pixel.R - r) < 40 && Math.Abs(pixel.G - g) < 40 && Math.Abs(pixel.B - b) < 40;

    [Fact]
    public void Each_side_has_its_own_color()
    {
        var image = Show("border-width: 10px; border-style: solid; border-color: #ff0000 #00ff00 #0000ff #000000;");
        Assert.Contains("Border [20, 20, 100x60] r=0 w=10,10,10,10 #ff0000 #00ff00 #0000ff #000000", Commands);
        Assert.True(Is(image.GetPixel(70, 24), 255, 0, 0), "top");
        Assert.True(Is(image.GetPixel(115, 50), 0, 255, 0), "right");
        Assert.True(Is(image.GetPixel(70, 75), 0, 0, 255), "bottom");
        Assert.True(Is(image.GetPixel(24, 50), 0, 0, 0), "left");
        Assert.True(Is(image.GetPixel(27, 23), 255, 0, 0));
        Assert.True(Is(image.GetPixel(23, 27), 0, 0, 0));
    }

    [Fact]
    public void A_single_side_takes_space_only_on_that_side()
    {
        var image = Show("border-bottom: 4px solid #000000;");
        Assert.Equal(new Thickness(0, 0, 0, 4), harness.Scene.RootLayout.Children.SelectMany(Flatten).Single(n => n.Tag is ElementNode { Name: "Container" } e && (string?)e.Attributes.GetValueOrDefault("class") == "box").Border);
        Assert.True(Is(image.GetPixel(70, 78), 0, 0, 0));
        Assert.True(Is(image.GetPixel(70, 21), 255, 255, 255));
    }

    [Fact]
    public void Dashed_and_dotted_borders_leave_gaps()
    {
        var dashed = Show("border: 4px dashed #000000;");
        Assert.Contains("dashed #000000", Commands);
        var top = Enumerable.Range(24, 92).Select(x => dashed.GetPixel(x, 22)).ToArray();
        Assert.Contains(top, p => Is(p, 0, 0, 0));
        Assert.Contains(top, p => Is(p, 255, 255, 255));

        var dotted = Show("border: 4px dotted #000000;");
        var left = Enumerable.Range(24, 52).Select(y => dotted.GetPixel(22, y)).ToArray();
        Assert.Contains(left, p => Is(p, 0, 0, 0));
        Assert.Contains(left, p => Is(p, 255, 255, 255));
    }

    [Fact]
    public void Each_corner_has_its_own_radius()
    {
        var image = Show("background: #000000; border-radius: 30px 0 0 0;");
        Assert.Contains("Fill [20, 20, 100x60] r=30,0,0,0 #000000", Commands);
        Assert.True(Is(image.GetPixel(22, 22), 255, 255, 255), "the top left corner is rounded");
        Assert.True(Is(image.GetPixel(118, 22), 0, 0, 0), "the top right corner is square");

        Show("background: #000000; border-radius: 50% / 10px;");
        Assert.Contains("r=50x10 ", Commands);

        Show("background: #000000; border-top-right-radius: 10px 20px; border-bottom-left-radius: 5px;");
        Assert.Contains("r=0,10x20,0,5 ", Commands);
    }

    [Fact]
    public void Inner_corners_lose_the_widths_of_their_sides()
    {
        Show("border-radius: 20px; border-style: solid; border-color: #000000; border-width: 4px 10px;");
        var border = harness.Scene.BuildDisplayList().Commands.OfType<DrawBorder>().Single();
        Assert.Equal(new Radius(10, 16), border.Radius.Deflate(border.Widths).TopLeft);
    }

    [Fact]
    public void The_outline_is_drawn_outside_the_box_without_moving_anything()
    {
        var image = Show("outline: 3px solid #0000ff; outline-offset: 2px;");
        Assert.Equal(140, Bounds("next").X);
        Assert.Contains("Border [15, 15, 110x70] r=0 w=3,3,3,3 #0000ff", Commands);
        Assert.True(Is(image.GetPixel(123, 50), 0, 0, 255), "in the ring");
        Assert.True(Is(image.GetPixel(121, 50), 255, 255, 255), "the offset is a gap");
    }

    [Fact]
    public void The_outline_follows_rounded_corners_and_survives_group_opacity()
    {
        Show("border-radius: 10px; outline: 2px solid #0000ff; outline-offset: 4px; opacity: 0.5;");
        var commands = harness.Scene.BuildDisplayList().Commands;
        Assert.Contains(commands, c => c is DrawBorder { Radius.TopLeft.X: 16 });
        Assert.Contains(commands, c => c is PushOpacity { Bounds: { X: 14, Y: 14 } });
    }

    [Fact]
    public void Outer_shadows_fall_outside_the_box_and_not_under_it()
    {
        var image = Show("background: transparent; box-shadow: 10px 10px 0 #000000;");
        Assert.Contains("Shadow [20, 20, 100x60] r=0 (10, 10) blur=0 spread=0 #000000", Commands);
        Assert.True(Is(image.GetPixel(125, 75), 0, 0, 0));
        Assert.True(Is(image.GetPixel(70, 85), 0, 0, 0));
        Assert.True(Is(image.GetPixel(100, 60), 255, 255, 255));
        Assert.True(Is(image.GetPixel(25, 85), 255, 255, 255), "the copy starts 10 to the right");
    }

    [Fact]
    public void Blur_softens_and_spread_grows_the_shadow()
    {
        var image = Show("box-shadow: 0 0 20px 4px #000000;");
        var near = image.GetPixel(70, 18);
        var far = image.GetPixel(70, 2);
        Assert.True(near.A > 0 && near.R < 200, $"dark near the box: {near}");
        Assert.True(far.R > near.R, $"lighter farther: {far} vs {near}");
    }

    [Fact]
    public void Inset_shadows_are_inside_the_padding_box_over_the_background()
    {
        var image = Show("background: #ffffff; border: 5px solid #ff0000; box-shadow: inset 8px 0 0 #0000ff;");
        var commands = Commands;
        Assert.Contains("Shadow inset [25, 25, 90x50]", commands);
        Assert.True(commands.IndexOf("Fill", StringComparison.Ordinal) < commands.IndexOf("Shadow inset", StringComparison.Ordinal));
        Assert.True(commands.IndexOf("Shadow inset", StringComparison.Ordinal) < commands.IndexOf("Border", StringComparison.Ordinal));
        Assert.True(Is(image.GetPixel(28, 50), 0, 0, 255), "a band along the left inner edge");
        Assert.True(Is(image.GetPixel(40, 50), 255, 255, 255));
        Assert.True(Is(image.GetPixel(22, 50), 255, 0, 0), "the border stays on top");
    }

    [Fact]
    public void The_first_shadow_is_on_top_and_current_color_is_the_text_color()
    {
        Show("color: #00ff00; box-shadow: 4px 4px 0 currentColor, 8px 8px 0 #000000;");
        var shadows = harness.Scene.BuildDisplayList().Commands.OfType<DrawShadow>().ToArray();
        Assert.Equal([new Nazor.Graphics.Color(0, 0, 0, 255), new Nazor.Graphics.Color(0, 255, 0, 255)], shadows.Select(s => s.Color));
    }

    [Fact]
    public void Shadows_widen_the_layer_of_a_group_opacity()
    {
        Show("opacity: 0.5; box-shadow: 0 12px 6px #000000;");
        Assert.Contains(harness.Scene.BuildDisplayList().Commands, c => c is PushOpacity { Bounds: { X: 2, Y: 2, Width: 136 } });
    }

    private GradientBrush Brush(int index = 0) => harness.Scene.BuildDisplayList().Commands.OfType<FillGradient>().ElementAt(index).Brush;

    [Fact]
    public void A_linear_gradient_runs_down_by_default()
    {
        var image = Show("background: linear-gradient(#ff0000, #0000ff);");
        var brush = Assert.IsType<LinearGradientBrush>(Brush());
        Assert.Equal(new Nazor.Graphics.Point(70, 20), brush.Start);
        Assert.Equal(new Nazor.Graphics.Point(70, 80), brush.End);
        Assert.True(image.GetPixel(70, 21).R > 230 && image.GetPixel(70, 21).B < 30, "red at the top");
        Assert.True(image.GetPixel(70, 79).B > 230 && image.GetPixel(70, 79).R < 30, "blue at the bottom");
        Assert.True(image.GetPixel(70, 50) is { R: > 100 and < 160, B: > 100 and < 160 }, "purple in the middle");
    }

    [Fact]
    public void Angles_and_corners_set_the_gradient_line()
    {
        Show("background: linear-gradient(90deg, #ff0000, #0000ff);");
        var right = Assert.IsType<LinearGradientBrush>(Brush());
        Assert.Equal((20f, 120f), (right.Start.X, right.End.X), new FloatPairComparer());

        Show("background: linear-gradient(to top right, #ff0000, #0000ff);");
        var corner = Assert.IsType<LinearGradientBrush>(Brush());
        var (dx, dy) = (corner.End.X - corner.Start.X, corner.End.Y - corner.Start.Y);
        Assert.Equal(-100f / 60f, dy / dx, 3);
        Assert.Equal(new Nazor.Graphics.Point(70, 50), new Nazor.Graphics.Point((corner.Start.X + corner.End.X) / 2, (corner.Start.Y + corner.End.Y) / 2));
    }

    [Fact]
    public void Color_stops_are_fixed_up_and_transparent_takes_its_neighbours_color()
    {
        Show("background: linear-gradient(#ff0000, #00ff00 30%, #0000ff 10%, #000000, transparent);");
        var stops = Brush().Stops;
        Assert.Equal([0f, 0.3f, 0.3f, 0.65f, 1f], stops.Select(s => MathF.Round(s.Offset, 3)));
        Assert.Equal(new Nazor.Graphics.Color(0, 0, 0, 0), stops[^1].Color);

        var image = Show("background: linear-gradient(to right, #ff0000, transparent); background-color: #ffffff;");
        var middle = image.GetPixel(70, 50);
        Assert.True(middle.R > 240 && middle.G > 100 && middle.G < 160, $"pink, not dark red: {middle}");
    }

    [Fact]
    public void Two_positions_make_hard_stripes()
    {
        var image = Show("background: linear-gradient(to right, #ff0000 50%, #0000ff 50%);");
        Assert.True(Is(image.GetPixel(68, 50), 255, 0, 0));
        Assert.True(Is(image.GetPixel(72, 50), 0, 0, 255));
    }

    [Fact]
    public void Radial_gradients_have_an_ending_shape_and_a_center()
    {
        Show("background: radial-gradient(#ffffff, #000000);");
        var ellipse = Assert.IsType<RadialGradientBrush>(Brush());
        Assert.Equal(new Nazor.Graphics.Point(70, 50), ellipse.Center);
        Assert.Equal(50 * MathF.Sqrt(2), ellipse.RadiusX, 3);
        Assert.Equal(30 * MathF.Sqrt(2), ellipse.RadiusY, 3);

        Show("background: radial-gradient(circle closest-side at 20px 10px, #ffffff, #000000);");
        var circle = Assert.IsType<RadialGradientBrush>(Brush());
        Assert.Equal(new Nazor.Graphics.Point(40, 30), circle.Center);
        Assert.Equal((10f, 10f), (circle.RadiusX, circle.RadiusY));

        Show("background: radial-gradient(40px 20% at right bottom, #ffffff, #000000);");
        var sized = Assert.IsType<RadialGradientBrush>(Brush());
        Assert.Equal(new Nazor.Graphics.Point(120, 80), sized.Center);
        Assert.Equal((40f, 12f), (sized.RadiusX, sized.RadiusY));
    }

    [Fact]
    public void Layers_paint_from_the_last_with_the_color_underneath()
    {
        Show("background: linear-gradient(#ff000080, #ff000080), radial-gradient(#00ff00, #0000ff), #ffffff;");
        var commands = Commands;
        var fill = commands.IndexOf("Fill [20, 20, 100x60] r=0 #ffffff", StringComparison.Ordinal);
        var radial = commands.IndexOf("Gradient [20, 20, 100x60] r=0 radial", StringComparison.Ordinal);
        var linear = commands.IndexOf("Gradient [20, 20, 100x60] r=0 linear", StringComparison.Ordinal);
        Assert.True(fill >= 0 && fill < radial && radial < linear, commands);
    }

    [Fact]
    public void Transitions_on_a_shorthand_animate_its_longhands()
    {
        var sheet = NssCompiler.Compile("Decorated", Page + """
            .box { border: 2px solid #000000; transition: border-color 200ms linear; }
            .box:hover { border-color: #ff0000; }
            """).StyleSheet;
        StyleRegistry.RegisterScoped(typeof(Decorated), () => sheet);
        var clock = new ManualAnimationClock();
        using var graphics = new SkiaRenderer(new FontOptions { UseSystemFonts = false, PrivateFontFiles = [Path.Combine(RenderHarness.FontDirectory, "FiraSans-Regular.ttf")], DefaultFamily = "Fira Sans" });
        using var host = new TestHost();
        using var scene = new Scene(host.Renderer, graphics.Fonts, graphics.Shaper, clock: clock) { Viewport = new Size(200, 100) };
        var root = host.Render<Decorated>();
        var box = root.Elements("Container").Single(e => (string?)e.Attributes.GetValueOrDefault("class") == "box");
        scene.Update();
        scene.BuildDisplayList();
        box.SetState(ElementStates.Hover, true);
        scene.Update();
        scene.BuildDisplayList();
        clock.Advance(100);
        scene.Update();
        scene.BuildDisplayList();
        Assert.Equal(Nazor.Style.Color.FromRgb(128, 0, 0), scene.GetEffectiveValue(box, StyleProperties.BorderLeftColor));
        Assert.Equal(Nazor.Style.Color.FromRgb(128, 0, 0), scene.GetEffectiveValue(box, StyleProperties.BorderTopColor));
    }
}
