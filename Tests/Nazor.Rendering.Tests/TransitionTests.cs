using Nazor.Graphics.Text;
using Nazor.Animation;
using Nazor.Controls;
using Nazor.Graphics;
using Nazor.Input;
using Nazor.Nss;
using Nazor.Presentation;
using Nazor.Rendering;
using Nazor.Skia;
using Nazor.Style;
using Nazor.Testing;
using StyleColor = Nazor.Style.Color;

namespace Nazor.Rendering.Tests;

public sealed class Swatches : ComponentBase
{
    public bool Warm { get; set; }
    public bool Slow { get; set; }
    public bool Disabled { get; set; }
    public bool Visible { get; set; } = true;

    public void Refresh() => StateHasChanged();

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenComponent<Container>(0, typeof(Swatches));
        builder.AddComponentParameter(1, "Class", "page");
        builder.AddAttribute(2, "ChildContent", (RenderFragment)(page =>
        {
            if (!Visible) return;
            page.OpenComponent<Button>(3, typeof(Swatches));
            page.AddComponentParameter(4, "Class", "swatch" + (Warm ? " warm" : "") + (Slow ? " slow" : ""));
            page.AddComponentParameter(5, "Disabled", Disabled);
            page.AddAttribute(6, "ChildContent", (RenderFragment)(c => c.AddContent(7, "Campione")));
            page.CloseComponent();
        }));
        builder.CloseComponent();
    }
}

[Collection("StyleRegistry")]
public sealed class TransitionTests : IDisposable
{
    private static readonly StyleSheet Sheet = NssCompiler.Compile("Swatches", """
        .page { padding: 10px; }
        .swatch {
            background: #000000;
            border: none;
            transition: background 200ms linear, opacity 100ms linear;
            &:hover { background: #ffffff; }
            &.warm { background: #ff0000; }
            &:disabled { opacity: 0.2; background: #000000; }
            &.slow { transition: background 100ms linear 300ms; }
        }
        """).StyleSheet;

    private readonly TestHost host = new();
    private readonly SkiaRenderer graphics = new(new FontOptions
    {
        PrivateFontFiles = [Path.Combine(RenderHarness.FontDirectory, "FiraSans-Regular.ttf")],
        UseSystemFonts = false,
        DefaultFamily = "Fira Sans",
    });
    private readonly ManualAnimationClock clock = new();
    private readonly Scene scene;
    private readonly InputManager input;
    private readonly RootComponent root;
    private readonly Swatches swatches;

    public TransitionTests()
    {
        StyleRegistry.RegisterScoped(typeof(Swatches), () => Sheet);
        scene = new Scene(host.Renderer, graphics.Fonts, graphics.Shaper, clock: clock) { Viewport = new Size(200, 100) };
        scene.Styles.Theme = Nazor.Themes.Themes.Basic;
        input = new InputManager(host.Renderer, scene);
        root = host.Render<Swatches>();
        swatches = (Swatches)root.Component;
        Frame();
    }

    public void Dispose()
    {
        input.Dispose();
        scene.Dispose();
        host.Dispose();
        graphics.Dispose();
    }

    private ElementNode Swatch => root.Single("Button");

    private DisplayList Frame()
    {
        scene.Update();
        return scene.BuildDisplayList();
    }

    private StyleColor Background()
    {
        Frame();
        return scene.GetEffectiveValue(Swatch, StyleProperties.BackgroundColor);
    }

    private static StyleColor Gray(byte value) => StyleColor.FromRgb(value, value, value);

    private void Hover(bool value) => Swatch.SetState(ElementStates.Hover, value);

    [Fact]
    public void Hover_moves_the_background_and_the_scheduler_stops_at_the_end()
    {
        Assert.Equal(Gray(0), Background());
        Hover(true);
        Assert.Equal(Gray(0), Background());
        Assert.Equal(TimeSpan.Zero, scene.Transitions.TimeUntilNextFrame);

        clock.Advance(100);
        Assert.Equal(Gray(128), Background());
        Assert.Equal(Gray(255), scene.GetStyle(Swatch).BackgroundColor);

        clock.Advance(100);
        Assert.Equal(Gray(255), Background());
        Assert.False(scene.Transitions.IsRunning);
        Assert.Null(scene.Transitions.TimeUntilNextFrame);
    }

    [Fact]
    public void Leaving_midway_reverses_from_the_visible_value_in_proportionally_less_time()
    {
        Hover(true);
        Background();
        clock.Advance(50);
        Assert.Equal(Gray(64), Background());

        Hover(false);
        Assert.Equal(Gray(64), Background());
        var back = scene.Transitions.Find(Swatch, StyleProperties.BackgroundColor)!;
        Assert.Equal(TimeSpan.FromMilliseconds(50), back.Duration);
        clock.Advance(25);
        Assert.Equal(Gray(32), Background());
        clock.Advance(25);
        Assert.Equal(Gray(0), Background());
        Assert.False(scene.Transitions.IsRunning);
    }

    [Fact]
    public void A_new_destination_midway_starts_from_the_visible_value_with_the_full_duration()
    {
        Hover(true);
        Background();
        clock.Advance(100);
        Assert.Equal(Gray(128), Background());

        swatches.Warm = true;
        swatches.Refresh();
        Assert.Equal(Gray(128), Background());
        Assert.Equal(TimeSpan.FromMilliseconds(200), scene.Transitions.Find(Swatch, StyleProperties.BackgroundColor)!.Duration);
        clock.Advance(100);
        Assert.Equal(new StyleColor(192, 64, 64, 255), Background());
        clock.Advance(100);
        Assert.Equal(StyleColor.FromRgb(255, 0, 0), Background());
    }

    [Fact]
    public void Opacity_follows_the_disabled_state_and_is_drawn_as_a_layer()
    {
        swatches.Disabled = true;
        swatches.Refresh();
        Frame();
        clock.Advance(50);
        var commands = Frame().Commands;
        Assert.Equal(0.6, scene.GetEffectiveValue(Swatch, StyleProperties.Opacity), 3);
        Assert.Equal(0.6f, commands.OfType<PushOpacity>().Single().Opacity, 3);
        clock.Advance(50);
        Frame();
        Assert.Equal(0.2, scene.GetEffectiveValue(Swatch, StyleProperties.Opacity), 3);
    }

    [Fact]
    public void Removing_the_element_cancels_its_transitions()
    {
        Hover(true);
        Background();
        Assert.Equal(1, scene.Transitions.Count);
        swatches.Visible = false;
        swatches.Refresh();
        Frame();
        Assert.Equal(0, scene.Transitions.Count);
        Assert.Null(scene.Transitions.TimeUntilNextFrame);
    }

    [Fact]
    public void Reduced_motion_and_hidden_windows_show_the_destination_at_once()
    {
        Hover(true);
        Background();
        clock.Advance(50);
        scene.Transitions.ReducedMotion = true;
        Assert.Equal(Gray(255), Background());
        Hover(false);
        Assert.Equal(Gray(0), Background());
        Assert.False(scene.Transitions.IsRunning);

        scene.Transitions.ReducedMotion = false;
        scene.Transitions.Suspended = true;
        Hover(true);
        Assert.Equal(Gray(255), Background());
        scene.Transitions.Suspended = false;
        Hover(false);
        Assert.Equal(Gray(255), Background());
        Assert.True(scene.Transitions.IsRunning);
    }

    [Fact]
    public void A_delay_postpones_the_change_and_the_next_frame()
    {
        swatches.Slow = true;
        swatches.Refresh();
        Frame();
        Hover(true);
        Assert.Equal(Gray(0), Background());
        Assert.Equal(TimeSpan.FromMilliseconds(300), scene.Transitions.TimeUntilNextFrame);
        clock.Advance(299);
        Assert.Equal(Gray(0), Background());
        clock.Advance(51);
        Assert.Equal(Gray(128), Background());
    }

    [Fact]
    public void The_drawn_pixels_follow_the_transition()
    {
        Hover(true);
        Background();
        clock.Advance(100);
        Frame();
        var bounds = scene.GetBounds(Swatch);
        var image = graphics.RenderToImage(scene.BuildDisplayList(), 1, scene.Background);
        Assert.Equal(new Graphics.Color(128, 128, 128, 255), image.GetPixel((int)bounds.X + 3, (int)bounds.Y + 3));
    }

    [Theory]
    [InlineData(0.5, 0.802403)]
    [InlineData(0.25, 0.408511)]
    [InlineData(0.75, 0.960459)]
    public void Ease_matches_the_css_curve(double input, double expected) =>
        Assert.Equal(expected, Easing.Evaluate(EasingFunction.Ease, input), 4);

    [Fact]
    public void Easing_presets_and_premultiplied_colors()
    {
        Assert.Equal(0.5, Easing.Evaluate(EasingFunction.EaseInOut, 0.5), 6);
        Assert.Equal(0.3, Easing.Evaluate(EasingFunction.Linear, 0.3), 6);
        Assert.True(Easing.Evaluate(EasingFunction.EaseIn, 0.5) < 0.5);
        Assert.True(Easing.Evaluate(EasingFunction.EaseOut, 0.5) > 0.5);
        Assert.Equal(new StyleColor(255, 0, 0, 128), Interpolation.Lerp(StyleColor.Transparent, StyleColor.FromRgb(255, 0, 0), 0.5));
    }
}
