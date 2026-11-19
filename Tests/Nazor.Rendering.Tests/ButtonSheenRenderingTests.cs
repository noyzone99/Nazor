using Nazor.Animation;
using Nazor.Controls;
using Nazor.Graphics;
using Nazor.Input;
using Nazor.Rendering;
using Nazor.Style;
using Nazor.Testing;
using Nazor.Themes.NazorLeaf;

namespace Nazor.Rendering.Tests;

public sealed class ButtonSheenPage : ComponentBase
{
    public bool Disabled { get; set; }
    public void Refresh() => StateHasChanged();
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "Container");
        builder.AddAttribute(1, "style", "width: 240px; height: 120px; background: #18364f");
        builder.OpenComponent<Button>(10);
        builder.AddComponentParameter(11, nameof(Button.Style), "width: 160px; height: 60px; margin: 20px");
        builder.AddComponentParameter(12, nameof(Button.Disabled), Disabled);
        builder.AddComponentParameter(13, nameof(Button.ChildContent), (RenderFragment)(content => content.AddContent(0, "Press")));
        builder.CloseComponent();
        builder.CloseElement();
    }
}

public sealed class ButtonSheenRenderingTests
{
    [Fact]
    public void Leaf_sheen_restarts_one_clipped_decorative_box_during_an_active_600ms_animation_and_settles_transparent()
    {
        var clock = new ManualAnimationClock();
        using var h = new RenderHarness(clock: clock);
        using var input = new InputManager(h.Host.Renderer, h.Scene);
        h.Scene.Styles.Theme = NazorLeafTheme.Theme;
        h.Scene.Transitions.ReducedMotion = false;
        h.Scene.Styles.Preferences = new MediaPreferences(ColorScheme.Light, false);
        var root = h.Host.Render<ButtonSheenPage>();
        void Frame() { h.Host.RunPending(); h.Render(240, 120, 1); h.Host.RunPending(); h.Render(240, 120, 1); }
        Frame();
        var button = root.Single("Button");
        var authored = button.Children.ToArray();
        Assert.Null(h.Scene.Styles.GetGeneratedElement(button, PseudoElement.After));
        var bounds = h.Scene.GetBounds(button);
        var point = new Point(bounds.X + 40, bounds.Y + 20);
        input.PointerPressed(point, PointerButton.Primary, KeyModifiers.None);
        Frame();
        var sheen = Assert.IsType<ElementNode>(h.Scene.Styles.GetGeneratedElement(button, PseudoElement.After));
        double Opacity() => h.Scene.Transitions.GetValue(sheen, StyleProperties.Opacity, h.Scene.Styles.GetStyle(sheen).Opacity);
        Assert.InRange(Opacity(), .99, 1);
        Assert.Equal(PointerEvents.None, h.Scene.Styles.GetStyle(sheen).Get(StyleProperties.PointerEvents));
        Assert.Contains(h.Scene.BuildDisplayList().Commands.OfType<PushClip>(), clip =>
            clip.Rect == h.Scene.GetLayoutNode(button)!.RoundedFrame && clip.Radius.TopLeft.X > 0);
        input.PointerReleased(point, PointerButton.Primary, KeyModifiers.None);
        clock.Advance(250);
        Frame();
        Assert.InRange(Opacity(), .001, .99);
        var replay = button.Attributes["press-replay"];
        input.PointerPressed(point, PointerButton.Primary, KeyModifiers.None);
        Frame();
        Assert.NotEqual(replay, button.Attributes["press-replay"]);
        Assert.Same(sheen, h.Scene.Styles.GetGeneratedElement(button, PseudoElement.After));
        Assert.InRange(Opacity(), .99, 1);
        input.PointerReleased(point, PointerButton.Primary, KeyModifiers.None);
        clock.Advance(610);
        Frame();
        Assert.Equal(0, Opacity());
        Assert.Equal(authored, button.Children);
        Assert.False(Assert.Single(h.Scene.Transitions.GetAnimations(sheen)).IsMoving(clock.Now));
        ((ButtonSheenPage)root.Component).Disabled = true;
        ((ButtonSheenPage)root.Component).Refresh();
        Frame();
        Assert.Null(h.Scene.Styles.GetGeneratedElement(button, PseudoElement.After));
    }

    [Theory]
    [InlineData("basic", false, false)]
    [InlineData("fluent", false, false)]
    [InlineData("leaf", true, false)]
    [InlineData("leaf", false, true)]
    public void Other_themes_reduced_motion_and_forced_colors_do_not_generate_a_sheen(string theme, bool reduced, bool forced)
    {
        using var h = new RenderHarness();
        using var input = new InputManager(h.Host.Renderer, h.Scene);
        h.Scene.Styles.Theme = theme switch { "leaf" => NazorLeafTheme.Theme, "fluent" => Nazor.Themes.Fluent.Themes.Fluent, _ => Nazor.Themes.Themes.Basic };
        h.Scene.Styles.Preferences = new MediaPreferences(ColorScheme.Light, reduced, forced);
        var root = h.Host.Render<ButtonSheenPage>();
        h.Render(240, 120, 1);
        var button = root.Single("Button");
        Assert.True(input.Activate(button));
        h.Host.RunPending();
        h.Render(240, 120, 1);
        Assert.Null(h.Scene.Styles.GetGeneratedElement(button, PseudoElement.After));
    }
}
