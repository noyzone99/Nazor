using Nazor.Animation;
using Nazor.Controls;
using Nazor.Controls.Primitives;
using Nazor.Graphics;
using Nazor.Graphics.Text;
using Nazor.Input;
using Nazor.Presentation;
using Nazor.Skia;
using Nazor.Style;
using Nazor.Testing;
using Nazor.Themes.NazorLeaf;
using Color = Nazor.Graphics.Color;

namespace Nazor.Rendering.Tests;

public sealed class LeafShowcase : ComponentBase
{
    [Parameter] public bool HasBackdrop { get; set; }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "Window");
        builder.AddAttribute(1, "role", "window");
        builder.AddAttribute(2, "style", "padding: 20px; gap: 12px");
        if (HasBackdrop) builder.AddAttribute(3, "backdrop", "acrylic");
        builder.OpenComponent<CheckBox>(4);
        builder.AddComponentParameter(5, nameof(CheckBox.Id), "checked");
        builder.AddComponentParameter(6, nameof(CheckBox.IsChecked), true);
        builder.AddComponentParameter(7, nameof(CheckBox.ChildContent), (RenderFragment)(t => t.AddContent(0, "Selezionata")));
        builder.CloseComponent();
        builder.OpenComponent<Button>(10);
        builder.AddComponentParameter(11, nameof(Button.Id), "accent");
        builder.AddComponentParameter(12, nameof(Button.Appearance), ControlAppearance.Accent);
        builder.AddComponentParameter(13, nameof(Button.ChildContent), (RenderFragment)(t => t.AddContent(0, "Salva")));
        builder.CloseComponent();
        builder.OpenComponent<LeafMark>(20);
        builder.AddComponentParameter(21, nameof(LeafMark.Id), "mark");
        builder.AddComponentParameter(22, nameof(LeafMark.Size), 40d);
        builder.CloseComponent();
        builder.OpenComponent<ProgressBar>(30);
        builder.AddComponentParameter(31, nameof(ProgressBar.Label), "Compilazione");
        builder.CloseComponent();
        builder.OpenComponent<Spinner>(40);
        builder.AddComponentParameter(41, nameof(Spinner.Id), "busy");
        builder.CloseComponent();
        builder.OpenComponent<Spinner>(50);
        builder.AddComponentParameter(51, nameof(Spinner.Id), "percent");
        builder.AddComponentParameter(52, nameof(Spinner.Value), (double?)64);
        builder.CloseComponent();
        builder.CloseElement();
    }
}

public sealed class LeafTitlebarWindow : WindowComponentBase
{
    public static bool? SystemButtons;

    protected override void OnInitialized()
    {
        Window.Decorations = WindowDecorationPolicy.Require(SystemButtons == true
            ? WindowDecorations.Native(WindowTitleBarContent.Nazor, WindowCaptionButtons.Native)
            : WindowDecorations.Nazor());
    }

    protected override void BuildWindowContent(RenderTreeBuilder builder)
    {
        builder.OpenComponent<LeafTitlebar>(0, typeof(LeafTitlebarWindow));
        builder.AddComponentParameter(1, nameof(LeafTitlebar.Title), "Atelier");
        builder.AddComponentParameter(2, nameof(LeafTitlebar.Trailing), (RenderFragment)(inner =>
        {
            inner.OpenComponent<LeafWindowButtons>(0);
            inner.CloseComponent();
        }));
        builder.CloseComponent();
    }
}

public sealed class NazorLeafThemeTests : IDisposable
{
    private readonly TestHost host = new();
    private readonly SkiaRenderer graphics = new(new FontOptions
    {
        PrivateFontFiles = [.. NazorLeafTheme.FontFiles.Select(file => Path.Combine(AppContext.BaseDirectory, file))],
        UseSystemFonts = false,
        DefaultFamily = "Geist",
    });
    private readonly Scene scene;
    private readonly Nazor.Input.InputManager input;
    private readonly ManualAnimationClock clock = new();

    public NazorLeafThemeTests()
    {
        scene = new Scene(host.Renderer, graphics.Fonts, graphics.Shaper, clock: clock, images: graphics.Images);
        scene.Styles.Theme = NazorLeafTheme.Theme;
        scene.Styles.Preferences = new MediaPreferences(ColorScheme.Light, ReducedMotion: true);
        scene.Transitions.ReducedMotion = true;
        input = new Nazor.Input.InputManager(host.Renderer, scene);
    }

    public void Dispose()
    {
        input.Dispose();
        scene.Dispose();
        host.Dispose();
        graphics.Dispose();
    }

    private RenderedImage Render(float width, float height)
    {
        scene.Viewport = new Size(width, height);
        scene.Scale = 1;
        scene.Update();
        host.RunPending();
        scene.Update();
        return graphics.RenderToImage(scene.BuildDisplayList(), 1, scene.Background);
    }

    private static ElementNode Find(RootComponent root, string id) => root.Elements().First(e => Equals(e.Attributes.GetValueOrDefault("id"), id));

    [Fact]
    public void Stepping_a_number_keeps_the_value_centered_while_the_editor_has_focus()
    {
        var root = host.Render<NumberField>(ParameterView.FromDictionary(new Dictionary<string, object?>
        {
            [nameof(NumberField.Value)] = (double?)12,
            [nameof(NumberField.SpinButtons)] = SpinButtonPlacement.Inline,
        }));
        Render(260, 60);
        var before = Assert.Single(scene.BuildDisplayList().Commands.OfType<DrawText>(), text => text.Layout.Text == "12");
        Assert.Equal(TextAlignment.Center, before.Alignment);
        input.Focus(root.Single("NumberFieldInput"), visible: false);
        input.KeyDown("ArrowUp", KeyModifiers.None, false);
        input.KeyUp("ArrowUp", KeyModifiers.None);
        Render(260, 60);
        Assert.Same(root.Single("NumberFieldInput"), input.FocusedTextBox);
        var after = Assert.Single(scene.BuildDisplayList().Commands.OfType<DrawText>(), text => text.Layout.Text == "13");
        Assert.Equal(TextAlignment.Center, after.Alignment);
        Assert.Equal(before.Width, after.Width);
    }

    [Fact]
    public void A_private_variable_font_gives_each_weight_its_own_instance()
    {
        var regular = new TextLayout("Nazor Leaf", new TextStyle(["Geist"], 20, 400), null, graphics.Fonts, graphics.Shaper);
        var bold = new TextLayout("Nazor Leaf", new TextStyle(["Geist"], 20, 800), null, graphics.Fonts, graphics.Shaper);
        Assert.Equal("Geist", graphics.Fonts.Match(["Geist"], 800, false).FamilyName);
        Assert.Equal(800, graphics.Fonts.Match(["Geist"], 800, false).Weight);
        Assert.True(bold.Width > regular.Width + 2, $"bold {bold.Width} regular {regular.Width}");
        Assert.Same(graphics.Fonts.Match(["Geist"], 650, false), graphics.Fonts.Match(["Geist"], 650, false));
    }

    [Fact]
    public void The_theme_draws_the_window_material_and_the_seed_color()
    {
        var root = host.Render<LeafShowcase>();
        var image = Render(320, 240);
        var mark = scene.GetStyle(root.Elements("CheckBoxMark").Single());
        Assert.Equal(Style.Color.FromRgb(0x0b, 0x57, 0xd0), mark.Get(StyleProperties.BackgroundColor));
        Assert.Equal(Style.Color.FromRgb(0x0b, 0x57, 0xd0), scene.GetStyle(Find(root, "accent")).Get(StyleProperties.BackgroundColor));
        var tray = scene.GetStyle(root.Elements("Window").Single()).Get(StyleProperties.BackgroundColor);
        Assert.Equal(Style.Color.FromRgb(0xd1, 0xdb, 0xeb), tray);
        Assert.Empty(scene.GetStyle(root.Elements("Window").Single()).Get(StyleProperties.BackgroundImage).Items);
        Assert.Equal(320, image.Width);
    }

    [Fact]
    public void The_four_leaf_mark_has_four_leaves_in_two_tones()
    {
        var root = host.Render<LeafShowcase>();
        Render(320, 240);
        var petals = Find(root, "mark").Children.OfType<ElementNode>().ToArray();
        Assert.Equal(4, petals.Length);
        var colors = petals.Select(p => scene.GetStyle(p).Get(StyleProperties.BackgroundColor)).ToArray();
        Assert.Equal(colors[0], colors[3]);
        Assert.Equal(colors[1], colors[2]);
        Assert.NotEqual(colors[0], colors[1]);
        Assert.True(Find(root, "mark").Attributes.GetValueOrDefault("semantic-hidden") is true);
    }

    [Fact]
    public void The_loaders_keep_a_static_preview_with_reduced_motion()
    {
        var root = host.Render<LeafShowcase>();
        Render(440, 420);
        var track = root.Single("ProgressBarTrack");
        var pulse = root.Single("ProgressBarPulse");
        Assert.InRange(scene.GetBounds(pulse).Width / scene.GetBounds(track).Width, 0.15f, 0.17f);
        Assert.Equal(Display.None, scene.GetStyle(Find(root, "busy").Elements().Single(e => e.Name == "SpinnerRing")).Get(StyleProperties.Display));
        Assert.Equal(4, Find(root, "busy").Elements().Count(e => e.Name == "SpinnerPetal"));
        var percent = Find(root, "percent");
        Assert.Equal(64d, percent.Attributes["range-value"]);
        Assert.Contains("64", percent.Elements().Single(e => e.Name == "SpinnerValue").Text());
        Assert.Equal(Display.Flex, scene.GetStyle(percent.Elements().Single(e => e.Name == "SpinnerValue")).Get(StyleProperties.Display));

        scene.Styles.Theme = Nazor.Themes.Themes.Basic;
        Render(440, 420);
        Assert.Equal(Display.None, scene.GetStyle(pulse).Get(StyleProperties.Display));
        Assert.All(root.Elements("SpinnerMark"), mark => Assert.Equal(Display.None, scene.GetStyle(mark).Get(StyleProperties.Display)));
    }

    [Fact]
    public void The_progress_sweep_moves_without_relaying_out_the_window()
    {
        scene.Styles.Preferences = new MediaPreferences(ColorScheme.Light, ReducedMotion: false);
        scene.Transitions.ReducedMotion = false;
        var root = host.Render<LeafShowcase>();
        Render(440, 420);
        var pulse = root.Single("ProgressBarPulse");
        var track = scene.GetBounds(root.Single("ProgressBarTrack"));
        Assert.False(scene.Transitions.IsAnimating(pulse, StyleEffects.Layout));
        Assert.True(scene.Transitions.IsAnimating(pulse, StyleEffects.Composite));

        clock.Advance(180);
        scene.BuildDisplayList();
        var start = scene.GetBounds(pulse).X;
        Assert.InRange((start - track.X) / track.Width, -0.36f, -0.34f);
        clock.Advance(400);
        scene.BuildDisplayList();
        Assert.True(scene.GetBounds(pulse).X > start);
    }

    [Fact]
    public void The_toast_countdown_moves_between_host_ticks_and_pauses_with_the_queue()
    {
        scene.Styles.Preferences = new MediaPreferences(ColorScheme.Light, ReducedMotion: false);
        scene.Transitions.ReducedMotion = false;
        var queue = new ToastQueue();
        var entry = queue.Show("Saved");
        var root = host.Render<ToastHost>(ParameterView.FromDictionary(new Dictionary<string, object?> { [nameof(ToastHost.Queue)] = queue }));
        Render(440, 420);
        var timer = root.Single("ToastTimer");
        Assert.True(scene.Transitions.IsAnimating(timer, StyleEffects.Composite));
        Assert.False(scene.Transitions.IsAnimating(timer, StyleEffects.Layout));

        clock.Advance(600);
        scene.BuildDisplayList();
        var first = scene.GetBounds(timer).Width;
        clock.Advance(125);
        scene.BuildDisplayList();
        Assert.True(scene.GetBounds(timer).Width < first);

        queue.Pause(entry);
        Render(440, 420);
        var paused = scene.GetBounds(timer).Width;
        Assert.False(scene.Transitions.IsAnimating(timer, StyleEffects.Composite));
        clock.Advance(125);
        scene.BuildDisplayList();
        Assert.Equal(paused, scene.GetBounds(timer).Width);

        queue.Resume(entry);
        Render(440, 420);
        Assert.True(scene.Transitions.IsAnimating(timer, StyleEffects.Composite));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void The_window_tray_is_uniform_and_only_translucent_over_an_active_backdrop(bool active)
    {
        var root = host.Render<LeafShowcase>(ParameterView.FromDictionary(new Dictionary<string, object?> { [nameof(LeafShowcase.HasBackdrop)] = active }));
        var window = root.Single("Window");
        Render(440, 420);
        var style = scene.GetStyle(window);
        Assert.Empty(style.Get(StyleProperties.BackgroundImage).Items);
        if (active) Assert.InRange(style.Get(StyleProperties.BackgroundColor).A, (byte)160, (byte)190);
        else Assert.Equal((byte)255, style.Get(StyleProperties.BackgroundColor).A);
    }

    [Fact]
    public void The_title_bar_draws_the_mark_and_the_leaf_lights()
    {
        LeafTitlebarWindow.SystemButtons = false;
        var root = host.Render<LeafTitlebarWindow>();
        Render(600, 120);
        Assert.Single(root.Elements("LeafMark"));
        Assert.Single(root.Elements("TitleBarTitle"));
        Assert.Equal(["minimize", "maximize", "close"], root.Elements("LeafWindowButton").Select(b => (string?)b.Attributes["kind"]));
        Assert.Equal(WindowCaptionButtons.Nazor, host.Window.AppliedDecorations!.Configuration.CaptionButtons);
    }

    [Fact]
    public void The_title_bar_leaves_the_buttons_to_the_system_when_it_shows_its_own()
    {
        LeafTitlebarWindow.SystemButtons = true;
        var root = host.Render<LeafTitlebarWindow>();
        Render(600, 120);
        Assert.Equal(WindowCaptionButtons.Native, host.Window.AppliedDecorations!.Configuration.CaptionButtons);
        Assert.Single(root.Elements("LeafMark"));
        Assert.Empty(root.Elements("LeafWindowButton"));
    }
}
