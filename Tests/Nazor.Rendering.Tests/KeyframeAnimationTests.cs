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

public sealed class Stage : ComponentBase
{
    public string Classes { get; set; } = "";
    public bool Shown { get; set; } = true;

    public void Refresh() => StateHasChanged();

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenComponent<Container>(0, typeof(Stage));
        builder.AddComponentParameter(1, "Class", "page");
        builder.AddAttribute(2, "ChildContent", (RenderFragment)(page =>
        {
            if (!Shown) return;
            page.OpenComponent<Container>(3, typeof(Stage));
            page.AddComponentParameter(4, "Class", ("box " + Classes).Trim());
            page.CloseComponent();
        }));
        builder.CloseComponent();
    }
}

[Collection("StyleRegistry")]
public sealed class KeyframeAnimationTests : IDisposable
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
    private RootComponent root = null!;
    private Stage stage = null!;

    public KeyframeAnimationTests()
    {
        var result = NssCompiler.Compile("Stage", """
            @keyframes fade { from { opacity: 0; } }
            @keyframes spin { from { transform: rotate(0deg); } to { transform: rotate(360deg); } }
            @keyframes grow { from { width: 100px; } to { width: 200px; } }
            @keyframes dip { 50% { opacity: 0.2; } }
            @keyframes eased { from { opacity: 0; animation-timing-function: ease-in; } to { opacity: 1; } }
            .page { display: flex; flex-direction: column; align-items: flex-start; }
            .box {
                width: 50px; height: 20px; background: #000000;
                &.fade { animation: fade 200ms linear; }
                &.spin { animation: spin 1000ms linear infinite; }
                &.grow { animation: grow 100ms linear 2 alternate forwards; }
                &.late { animation: fade 100ms linear 50ms; }
                &.late-backwards { animation: fade 100ms linear 50ms backwards; }
                &.dip { animation: dip 100ms linear; }
                &.eased { animation: eased 100ms linear; }
                &.spin-paused { animation: spin 1000ms linear infinite paused; }
                &.spin-slow { animation: spin 2000ms linear infinite; }
                &.stay { animation: grow 100ms linear forwards; }
            }
            """);
        Assert.True(result.Success, string.Join("\n", result.Diagnostics));
        var sheet = result.StyleSheet;
        StyleRegistry.RegisterScoped(typeof(Stage), () => sheet);
        scene = new Scene(host.Renderer, graphics.Fonts, graphics.Shaper, clock: clock) { Viewport = new Size(400, 300) };
    }

    public void Dispose()
    {
        scene.Dispose();
        host.Dispose();
        graphics.Dispose();
    }

    private void Show(string classes)
    {
        root = host.Render<Stage>();
        stage = (Stage)root.Component;
        stage.Classes = classes;
        stage.Refresh();
        host.RunPending();
        Frame();
    }

    private ElementNode Box => root.Elements("Container").Single(e => (e.Attributes.GetValueOrDefault("class") as string ?? "").StartsWith("box", StringComparison.Ordinal));

    private DisplayList Frame()
    {
        scene.Update();
        return scene.BuildDisplayList();
    }

    private void At(double milliseconds)
    {
        clock.Advance(milliseconds);
        Frame();
    }

    private void Become(string classes)
    {
        stage.Classes = classes;
        stage.Refresh();
        host.RunPending();
        Frame();
    }

    private double Opacity => scene.GetEffectiveValue(Box, StyleProperties.Opacity);

    [Fact]
    public void An_element_that_appears_fades_in_and_the_frames_stop_at_the_end()
    {
        Show("fade");
        Assert.Equal(0, Opacity);
        Assert.Equal(TimeSpan.Zero, scene.Transitions.TimeUntilNextFrame);
        At(100);
        Assert.Equal(0.5, Opacity, 6);
        At(100);
        Assert.Equal(1, Opacity);
        Assert.False(scene.Transitions.IsRunning);
        Assert.Null(scene.Transitions.TimeUntilNextFrame);
    }

    [Fact]
    public void An_infinite_rotation_keeps_turning()
    {
        Show("spin");
        At(250);
        Assert.Equal("rotate(90deg)", scene.GetEffectiveValue(Box, StyleProperties.Transform).ToString());
        At(1000);
        Assert.Equal("rotate(90deg)", scene.GetEffectiveValue(Box, StyleProperties.Transform).ToString());
        Assert.True(scene.Transitions.IsRunning);
        Assert.Equal(TimeSpan.Zero, scene.Transitions.TimeUntilNextFrame);
        var push = scene.BuildDisplayList().Commands.OfType<PushTransform>().Single();
        Assert.Equal(0, push.Matrix.M11, 4);
    }

    [Fact]
    public void Iterations_alternate_through_the_layout_and_forwards_keeps_the_end()
    {
        Show("grow");
        At(50);
        Assert.Equal(150, scene.GetBounds(Box).Width);
        At(75);
        Assert.Equal(175, scene.GetBounds(Box).Width);
        At(100);
        Assert.Equal(100, scene.GetBounds(Box).Width);
        Assert.False(scene.Transitions.IsRunning);
    }

    [Fact]
    public void A_delay_shows_the_computed_value_unless_the_fill_is_backwards()
    {
        Show("late");
        Assert.Equal(1, Opacity);
        Assert.Equal(TimeSpan.FromMilliseconds(50), scene.Transitions.TimeUntilNextFrame);
        At(75);
        Assert.Equal(0.25, Opacity, 6);
        Become("late-backwards");
        Assert.Equal(0.25, Opacity, 6);
        Show("late-backwards");
        Assert.Equal(0, Opacity);
    }

    [Fact]
    public void Keyframes_without_ends_and_with_their_own_timing_function()
    {
        Show("dip");
        At(25);
        Assert.Equal(0.6, Opacity, 6);
        Show("eased");
        At(50);
        Assert.Equal(Easing.Evaluate(EasingFunction.EaseIn, 0.5), Opacity, 6);
    }

    [Fact]
    public void Pausing_freezes_the_timeline_and_a_new_duration_keeps_it()
    {
        Show("spin");
        At(250);
        Become("spin-paused");
        At(500);
        Assert.Equal("rotate(90deg)", scene.GetEffectiveValue(Box, StyleProperties.Transform).ToString());
        Assert.False(scene.Transitions.IsRunning);
        Become("spin");
        At(250);
        Assert.Equal("rotate(180deg)", scene.GetEffectiveValue(Box, StyleProperties.Transform).ToString());
        Become("spin-slow");
        Assert.Equal("rotate(90deg)", scene.GetEffectiveValue(Box, StyleProperties.Transform).ToString());
    }

    [Fact]
    public void Reduced_motion_shows_only_the_end_of_a_forwards_fill()
    {
        scene.Transitions.ReducedMotion = true;
        Show("spin");
        Assert.Equal("none", scene.GetEffectiveValue(Box, StyleProperties.Transform).ToString());
        Assert.False(scene.Transitions.IsRunning);
        Assert.Null(scene.Transitions.TimeUntilNextFrame);
        Show("stay");
        Assert.Equal(200, scene.GetBounds(Box).Width);
    }

    [Fact]
    public void Removing_the_element_cancels_its_animations()
    {
        Show("spin");
        At(100);
        stage.Shown = false;
        stage.Refresh();
        host.RunPending();
        Frame();
        Assert.False(scene.Transitions.HasAnimations);
        Assert.False(scene.Transitions.IsRunning);
    }
}
