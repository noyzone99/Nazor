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

public sealed class Drawer : ComponentBase
{
    public string Classes { get; set; } = "";

    public void Refresh() => StateHasChanged();

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenComponent<Container>(0, typeof(Drawer));
        builder.AddComponentParameter(1, "Class", "page");
        builder.AddAttribute(2, "ChildContent", (RenderFragment)(page =>
        {
            page.OpenComponent<Container>(3, typeof(Drawer));
            page.AddComponentParameter(4, "Class", ("panel " + Classes).Trim());
            page.CloseComponent();
            page.OpenComponent<Container>(5, typeof(Drawer));
            page.AddComponentParameter(6, "Class", "next");
            page.CloseComponent();
        }));
        builder.CloseComponent();
    }
}

[Collection("StyleRegistry")]
public sealed class MotionTransitionTests : IDisposable
{
    private readonly TestHost host = new();
    private readonly SkiaRenderer graphics = new(new FontOptions
    {
        PrivateFontFiles = [Path.Combine(RenderHarness.FontDirectory, "FiraSans-Regular.ttf")],
        UseSystemFonts = false,
        DefaultFamily = "Fira Sans",
    });
    private readonly ManualAnimationClock clock = new();
    private readonly Scene scene;
    private readonly RootComponent root;
    private readonly Drawer drawer;

    public MotionTransitionTests()
    {
        var result = NssCompiler.Compile("Drawer", """
            .page { display: flex; flex-direction: column; align-items: flex-start; width: 400px; }
            .panel {
                width: 100px; height: 20px; background: #000000;
                transition: width 200ms linear, height 200ms linear, padding-left 200ms linear, transform 200ms linear,
                    box-shadow 200ms linear, border-top-left-radius 200ms linear, margin-left 200ms linear;
                &.wide { width: 300px; }
                &.half { width: 50%; }
                &.auto { width: auto; }
                &.tall { height: 60px; }
                &.moved { transform: translateX(100px); }
                &.turned { transform: rotate(90deg); }
                &.lifted { box-shadow: 0 4px 8px rgba(0, 0, 0, 0.5); }
                &.round { border-top-left-radius: 10px; }
                &.centered { margin-left: auto; }
            }
            .next { width: 10px; height: 10px; }
            """);
        Assert.True(result.Success, string.Join("\n", result.Diagnostics));
        var sheet = result.StyleSheet;
        StyleRegistry.RegisterScoped(typeof(Drawer), () => sheet);
        scene = new Scene(host.Renderer, graphics.Fonts, graphics.Shaper, clock: clock) { Viewport = new Size(400, 300) };
        root = host.Render<Drawer>();
        drawer = (Drawer)root.Component;
        Frame();
    }

    public void Dispose()
    {
        scene.Dispose();
        host.Dispose();
        graphics.Dispose();
    }

    private ElementNode Panel => root.Elements("Container").Single(e => (e.Attributes.GetValueOrDefault("class") as string ?? "").StartsWith("panel", StringComparison.Ordinal));

    private ElementNode Next => root.Elements("Container").Single(e => e.Attributes.GetValueOrDefault("class") as string == "next");

    private DisplayList Frame()
    {
        scene.Update();
        return scene.BuildDisplayList();
    }

    private void Become(string classes)
    {
        drawer.Classes = classes;
        drawer.Refresh();
        host.RunPending();
        Frame();
    }

    private DisplayList At(double milliseconds)
    {
        clock.Advance(milliseconds);
        return Frame();
    }

    [Fact]
    public void A_width_moves_through_the_layout_and_pushes_nothing_else_afterwards()
    {
        Become("wide");
        Assert.Equal(100, scene.GetBounds(Panel).Width);
        At(100);
        Assert.Equal(200, scene.GetBounds(Panel).Width);
        Assert.Equal(SizeValue.Fixed(Length.Px(200)), scene.GetEffectiveValue(Panel, StyleProperties.Width));
        Become("wide tall");
        At(100);
        Assert.Equal(300, scene.GetBounds(Panel).Width);
        Assert.Equal(40, scene.GetBounds(Panel).Height);
        Assert.Equal(40, scene.GetBounds(Next).Y);
        At(100);
        Assert.Equal(60, scene.GetBounds(Panel).Height);
        Assert.False(scene.Transitions.IsRunning);
        Frame();
        Assert.Equal(0, scene.LastUpdate.LayoutComputations);
    }

    [Fact]
    public void Px_to_a_percentage_passes_through_calc()
    {
        Become("half");
        At(100);
        Assert.Equal("calc(25% + 50px)", scene.GetEffectiveValue(Panel, StyleProperties.Width).ToString());
        Assert.Equal(150, scene.GetBounds(Panel).Width);
        At(100);
        Assert.Equal(200, scene.GetBounds(Panel).Width);
    }

    [Fact]
    public void Auto_does_not_interpolate_with_a_length()
    {
        Become("auto");
        Assert.False(scene.Transitions.IsRunning);
        Become("centered");
        Assert.False(scene.Transitions.IsRunning);
    }

    [Fact]
    public void Transforms_move_function_by_function_and_from_none()
    {
        Become("moved");
        var push = At(50).Commands.OfType<PushTransform>().Single();
        Assert.Equal(25, push.Matrix.OffsetX, 0.01f);
        Assert.Equal("translate(25px, 0px)", scene.GetEffectiveValue(Panel, StyleProperties.Transform).ToString());
        Assert.Same(Panel, scene.HitTest(new Point(120, 10)));
        At(150);
        Assert.Equal(new Rect(100, 0, 100, 20), scene.GetBounds(Panel));
    }

    [Fact]
    public void Different_transform_lists_mix_their_matrices()
    {
        Become("moved");
        At(200);
        Become("turned");
        At(100);
        Assert.IsType<MixFunction>(scene.GetEffectiveValue(Panel, StyleProperties.Transform).Items.Single());
        var matrix = scene.BuildDisplayList().Commands.OfType<PushTransform>().Single().Matrix;
        Assert.Equal(Math.Cos(Math.PI / 4), matrix.M11, 3);
        Assert.Equal(Math.Sin(Math.PI / 4), matrix.M12, 3);
        Assert.Equal(100, matrix.Transform(new Point(50, 10)).X, 0.01f);
        Assert.Equal(10, matrix.Transform(new Point(50, 10)).Y, 0.01f);
    }

    [Fact]
    public void Shadows_grow_from_none_and_radii_round_little_by_little()
    {
        Become("lifted round");
        var commands = At(100).Commands;
        var shadow = commands.OfType<DrawShadow>().Single();
        Assert.Equal(new Point(0, 2), shadow.Offset);
        Assert.Equal(4, shadow.Blur);
        Assert.InRange(shadow.Color.A, 63, 65);
        Assert.Equal(5, commands.OfType<FillRectangle>().Single().Radius.TopLeft.X);
    }

    [Fact]
    public void Reduced_motion_jumps_to_the_destination()
    {
        scene.Transitions.ReducedMotion = true;
        Become("wide moved");
        Assert.False(scene.Transitions.IsRunning);
        Assert.Equal(new Rect(100, 0, 300, 20), scene.GetBounds(Panel));
    }

    [Fact]
    public void Matrices_interpolate_by_decomposition()
    {
        var halfway = Matrix.Interpolate(Matrix.Rotation(-45), Matrix.Rotation(45), 0.5);
        Assert.Equal(1, halfway.M11, 4);
        Assert.Equal(0, halfway.M12, 4);
        var moved = Matrix.Interpolate(Matrix.Scaling(1, 1), Matrix.Scaling(3, 2).Then(Matrix.Translation(10, 20)), 0.5);
        Assert.Equal(new Matrix(2, 0, 0, 1.5f, 5, 10), moved);
        var flipped = Matrix.Interpolate(Matrix.Identity, Matrix.Scaling(-1, -1), 0.5);
        Assert.Equal(0, flipped.M11, 4);
        Assert.Equal(1, Math.Abs(flipped.M12), 4);
    }

    [Fact]
    public void Lengths_of_different_kinds_interpolate_as_calc()
    {
        Assert.Equal(Length.Px(15), Interpolation.Lerp(Length.Px(10), Length.Px(20), 0.5));
        Assert.Equal("calc(25% + 5px)", Interpolation.Lerp(Length.Px(10), Length.Percent(50), 0.5).ToString());
        var expression = NssCompiler.Compile("t", ".x { width: min(50%, 100px); }").StyleSheet.Rules[0].Declarations[0];
        var min = ((SizeValue)((ValueDeclaration)expression).Value).Length;
        Assert.Equal(22.5, Interpolation.Lerp(Length.Px(0), min, 0.5).Resolve(90), 3);
        Assert.False(Interpolation.CanInterpolate(SizeValue.Auto, SizeValue.Fixed(Length.Px(1))));
        Assert.False(Interpolation.CanInterpolate(
            new ValueList<BoxShadow>([new BoxShadow(true, Length.Zero, Length.Zero, Length.Zero, Length.Zero, Style.Color.FromRgb(0, 0, 0))]),
            new ValueList<BoxShadow>([new BoxShadow(false, Length.Zero, Length.Zero, Length.Zero, Length.Zero, Style.Color.FromRgb(0, 0, 0))])));
    }
}
