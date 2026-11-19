using System.Globalization;
using Nazor.Animation;
using Nazor.Controls;
using Nazor.Controls.Primitives;
using Nazor.Graphics;
using Nazor.Input;
using Nazor.Nss;
using Nazor.Style;
using Nazor.Testing;

namespace Nazor.Rendering.Tests;

public sealed class NumberPresentationPage : ComponentBase
{
    public double Value { get; set; } = 1;
    public bool Mounted { get; set; } = true;
    public bool Hidden { get; set; }
    public NumberPresenter Presenter { get; private set; } = null!;
    public void Refresh() => StateHasChanged();
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "Surface");
        builder.AddAttribute(1, "style", "width: 120px; height: 100px; background: white;");
        if (Mounted)
        {
            builder.OpenComponent<NumberPresenter>(10);
            builder.AddComponentParameter(11, nameof(NumberPresenter.As), "NumberProbe");
            builder.AddComponentParameter(12, nameof(NumberPresenter.Value), Value);
            builder.AddComponentParameter(13, nameof(NumberPresenter.Style), Hidden ? "display: none;" : null);
            builder.AddComponentParameter(14, nameof(NumberPresenter.ChildContent), (RenderFragment<NumberPresentationState>)(state => content =>
            {
                content.OpenElement(0, "Bar");
                content.AddAttribute(1, "style", "position: absolute; left: 0; top: 0; height: 10px; width: calc(var(--nz-presented-number) * 100%); background: black;");
                content.CloseElement();
                content.OpenElement(10, "NumberText");
                content.AddAttribute(11, "style", "position: absolute; left: 0; top: 12px;");
                content.AddContent(12, state.Value.ToString("F4", CultureInfo.InvariantCulture));
                content.CloseElement();
                content.OpenComponent<TextField>(20);
                content.AddComponentParameter(21, nameof(TextField.Id), "persistent");
                content.AddComponentParameter(22, nameof(TextField.Value), "Keep");
                content.AddComponentParameter(23, nameof(TextField.Style), "position: absolute; top: 36px; left: 0; width: 100px;");
                content.CloseComponent();
            }));
            builder.AddComponentReferenceCapture(15, component => Presenter = (NumberPresenter)component);
            builder.CloseComponent();
        }
        builder.CloseElement();
    }
}

public sealed class NumberPresentationTests
{
    private const string Linear = "number-motion: linear-increase; number-motion-duration: 2s;";
    private const string Exponential = "number-motion: exponential; number-motion-decay: 0.14; number-motion-tolerance: 0.0005; number-boundary-tolerance: 0.003;";

    [Fact]
    public void Linear_increases_use_time_per_unit_and_interruption_samples_the_displayed_value()
    {
        using var context = new Context(Linear);
        var editor = context.Root.Single("TextFieldInput");
        context.Input.Focus(editor, visible: true);
        context.Clock.Advance(400);
        var image = context.Frame();
        Assert.Equal(0.2, context.Page.Presenter.Presentation.Value, 8);
        Assert.Equal(new Graphics.Color(0, 0, 0, 255), image.GetPixel(15, 5));
        Assert.Equal(new Graphics.Color(255, 255, 255, 255), image.GetPixel(25, 5));
        context.Clock.Advance(200);
        context.Page.Value = 0.8;
        context.Page.Refresh();
        context.Frame();
        Assert.Equal(0.3, context.Page.Presenter.Presentation.Value, 8);
        Assert.Equal(0.8, context.Page.Presenter.Value);
        context.Clock.Advance(500);
        context.Frame();
        Assert.Equal(0.55, context.Page.Presenter.Presentation.Value, 8);
        Assert.Same(editor, context.Root.Single("TextFieldInput"));
        Assert.Same(editor, context.Input.Focused);
        context.Page.Value = 0.1;
        context.Page.Refresh();
        context.Frame();
        Assert.Equal(0.1, context.Page.Presenter.Presentation.Value);
        Assert.Null(context.Harness.Scene.TimeUntilNextFrame);
        Assert.Equal("0.1000", context.Root.Single("NumberText").Text());
    }

    [Fact]
    public void Ordinary_linear_motion_also_animates_decreases_and_stops_at_the_target()
    {
        using var context = new Context("number-motion: linear; number-motion-duration: 2s;");
        context.Clock.Advance(500);
        context.Frame();
        Assert.Equal(0.25, context.Page.Presenter.Presentation.Value);
        context.Page.Value = 0.1;
        context.Page.Refresh();
        context.Frame();
        context.Clock.Advance(150);
        context.Frame();
        Assert.Equal(0.175, context.Page.Presenter.Presentation.Value, 8);
        context.Clock.Advance(150);
        context.Frame();
        Assert.Equal(0.1, context.Page.Presenter.Presentation.Value);
        Assert.Null(context.Harness.Scene.TimeUntilNextFrame);
    }

    [Fact]
    public void Exponential_motion_matches_reference_samples_and_updates_painted_text_and_geometry()
    {
        using var context = new Context(Exponential);
        context.Clock.Advance(50);
        context.Frame();
        Assert.Equal(0.363944, context.Page.Presenter.Presentation.Value, 10);
        context.Clock.Advance(50);
        var image = context.Frame();
        Assert.Equal(0.595432764864, context.Page.Presenter.Presentation.Value, 10);
        Assert.Equal("0.5954", context.Root.Single("NumberText").Text());
        Assert.Equal(new Graphics.Color(0, 0, 0, 255), image.GetPixel(20, 5));
        Assert.Equal(new Graphics.Color(255, 255, 255, 255), image.GetPixel(70, 5));
        context.Page.Value = 0.5;
        context.Page.Refresh();
        context.Frame();
        Assert.Equal(0.595432764864, context.Page.Presenter.Presentation.Value, 10);
        context.Clock.Advance(50);
        context.Frame();
        Assert.Equal(0.5607005826883364, context.Page.Presenter.Presentation.Value, 10);
        context.Clock.Advance(2000);
        context.Frame();
        Assert.Equal(0.5, context.Page.Presenter.Presentation.Value);
        Assert.Null(context.Harness.Scene.TimeUntilNextFrame);
    }

    [Fact]
    public void Exponential_sampling_is_independent_of_render_cadence()
    {
        using var fine = new Context(Exponential);
        using var coarse = new Context(Exponential);
        foreach (var ticks in new[] { 166667L, 166666L, 166667L })
        {
            fine.Clock.Advance(TimeSpan.FromTicks(ticks));
            fine.Frame();
        }
        coarse.Clock.Advance(50);
        coarse.Frame();
        Assert.Equal(1 - Math.Pow(0.86, 3), coarse.Page.Presenter.Presentation.Value, 8);
        Assert.Equal(coarse.Page.Presenter.Presentation.Value, fine.Page.Presenter.Presentation.Value, 12);
    }

    [Fact]
    public void Reduced_motion_snap_and_removal_release_all_requested_frames()
    {
        using var context = new Context(Exponential);
        context.Clock.Advance(100);
        context.Frame();
        Assert.NotNull(context.Harness.Scene.TimeUntilNextFrame);
        context.Harness.Scene.Styles.Preferences = new MediaPreferences(ColorScheme.Light, ReducedMotion: true);
        context.Frame();
        Assert.Equal(1d, context.Page.Presenter.Presentation.Value);
        Assert.True(context.Page.Presenter.Presentation.AtEnd);
        Assert.Null(context.Harness.Scene.TimeUntilNextFrame);
        context.Harness.Scene.Styles.Preferences = new MediaPreferences(ColorScheme.Light, ReducedMotion: false);
        context.Page.Value = 0.5;
        context.Page.Refresh();
        context.Frame();
        Assert.NotNull(context.Harness.Scene.TimeUntilNextFrame);
        context.Page.Mounted = false;
        context.Page.Refresh();
        context.Harness.Host.RunPending();
        Assert.Null(context.Harness.Scene.TimeUntilNextFrame);
        context.Frame();
        Assert.Empty(context.Root.Elements("NumberProbe"));
    }

    [Fact]
    public void Hidden_owners_snap_without_background_frame_work_and_resume_from_the_visible_value()
    {
        using var context = new Context(Exponential);
        context.Page.Hidden = true;
        context.Page.Refresh();
        context.Frame();
        Assert.Equal(1d, context.Page.Presenter.Presentation.Value);
        Assert.Null(context.Harness.Scene.TimeUntilNextFrame);
        context.Page.Hidden = false;
        context.Page.Refresh();
        context.Frame();
        Assert.Equal(1d, context.Page.Presenter.Presentation.Value);
        context.Page.Value = 0.5;
        context.Page.Refresh();
        context.Frame();
        context.Clock.Advance(50);
        context.Frame();
        Assert.Equal(0.818028, context.Page.Presenter.Presentation.Value, 10);
    }

    [Theory]
    [InlineData(double.NaN, 0, 1)]
    [InlineData(double.PositiveInfinity, 0, 1)]
    [InlineData(double.NegativeInfinity, 0, 1)]
    [InlineData(0, double.NaN, 1)]
    [InlineData(0, 2, 1)]
    [InlineData(0, -double.MaxValue, double.MaxValue)]
    public void Nonfinite_targets_and_invalid_ranges_are_rejected(double value, double minimum, double maximum)
    {
        using var harness = new RenderHarness();
        Assert.Throws<ArgumentOutOfRangeException>(() => harness.Host.Render<NumberPresenter>(ParameterView.FromDictionary(new Dictionary<string, object?>
        {
            [nameof(NumberPresenter.Value)] = value,
            [nameof(NumberPresenter.Minimum)] = minimum,
            [nameof(NumberPresenter.Maximum)] = maximum,
        })));
    }

    private sealed class Context : IDisposable
    {
        public ManualAnimationClock Clock { get; } = new();
        public RenderHarness Harness { get; }
        public InputManager Input { get; }
        public RootComponent Root { get; }
        public NumberPresentationPage Page { get; }
        public Context(string rules)
        {
            Harness = new RenderHarness(clock: Clock);
            Input = new InputManager(Harness.Host.Renderer, Harness.Scene);
            Harness.Scene.Transitions.ReducedMotion = false;
            Harness.Scene.Styles.Preferences = new MediaPreferences(ColorScheme.Light, ReducedMotion: false);
            var app = NssCompiler.Compile("number-presentation", "NumberProbe { position: relative; width: 100px; height: 80px; " + rules + " }");
            Assert.True(app.Success, string.Join("\n", app.Diagnostics));
            Harness.Scene.Styles.Application = app.StyleSheet;
            Root = Harness.Host.Render<NumberPresentationPage>();
            Page = (NumberPresentationPage)Root.Component;
            Frame();
        }
        public RenderedImage Frame()
        {
            Harness.Host.RunPending();
            Harness.Render(120, 100, 1);
            Harness.Host.RunPending();
            return Harness.Render(120, 100, 1);
        }
        public void Dispose() { Input.Dispose(); Harness.Dispose(); }
    }
}
