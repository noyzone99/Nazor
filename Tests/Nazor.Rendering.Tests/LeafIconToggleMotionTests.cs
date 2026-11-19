using Nazor.Animation;
using Nazor.Controls;
using Nazor.Graphics;
using Nazor.Input;
using Nazor.Rendering;
using Nazor.Skia;
using Nazor.Style;
using Nazor.Testing;
using Nazor.Themes.NazorLeaf;

namespace Nazor.Rendering.Tests;

public sealed class LeafIconTogglePage : ComponentBase
{
    [Parameter] public bool Pressed { get; set; }
    [Parameter] public bool Disabled { get; set; }
    public int Changes { get; private set; }
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "Page"); builder.AddAttribute(1, "style", "padding: 32px;");
        builder.OpenComponent<ToggleButton>(10);
        builder.AddComponentParameter(11, nameof(ToggleButton.IsPressed), Pressed);
        builder.AddComponentParameter(12, nameof(ToggleButton.IsPressedChanged), EventCallback.Factory.Create<bool>(this, value => { Pressed = value; Changes++; }));
        builder.AddComponentParameter(13, nameof(ToggleButton.Disabled), Disabled);
        builder.AddComponentParameter(14, nameof(ToggleButton.AccessibleName), "Favorite");
        builder.AddComponentParameter(15, nameof(ToggleButton.Leading), (RenderFragment)(content =>
        {
            content.OpenComponent<Icon>(0);
            content.AddComponentParameter(1, nameof(Icon.Symbol), Pressed ? LeafSymbol.StarFilled : LeafSymbol.Star);
            content.CloseComponent();
        }));
        builder.CloseComponent(); builder.CloseElement();
    }
}

public sealed class LeafIconToggleMotionTests
{
    [Fact]
    public async Task Filled_star_adds_real_center_ink_with_the_same_private_family_and_keeps_the_existing_star_mapping()
    {
        using var h = new RenderHarness();
        Register(h);
        Assert.Equal("\uE026", LeafSymbol.Star.Glyph);
        Assert.Equal("\uE030", LeafSymbol.StarFilled.Glyph);
        var root = h.Host.Render<Icon>(IconParameters(LeafSymbol.Star));
        var outline = h.Render(96, 96, 1);
        await root.Component.SetParametersAsync(IconParameters(LeafSymbol.StarFilled)); h.Host.RunPending();
        var filled = h.Render(96, 96, 1);
        Assert.Equal(Nazor.Graphics.Color.White, outline.GetPixel(48, 48));
        Assert.True(filled.GetPixel(48, 48).R < 30);
        var drawing = Assert.Single(h.Scene.BuildDisplayList().Commands.OfType<DrawText>());
        Assert.Equal("NazorLeaf Icons", Assert.Single(drawing.Layout.Shaped.Runs).Face.FamilyName);
        Assert.All(drawing.Layout.Shaped.Runs, run => Assert.All(run.Glyphs, glyph => Assert.NotEqual((ushort)0, glyph)));
        Assert.True(Ink(filled) > Ink(outline));
        static long Ink(RenderedImage image)
        {
            long ink=0;
            for (var y=0;y<image.Height;y++) for (var x=0;x<image.Width;x++) ink+=255-image.GetPixel(x,y).R;
            return ink;
        }
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public void Bound_artwork_tracks_semantic_pressed_state_and_pop_restarts_only_on_a_new_enabled_selection(bool reduced, bool forced)
    {
        var clock = new ManualAnimationClock();
        using var h = new RenderHarness(clock: clock);
        using var input = new InputManager(h.Host.Renderer, h.Scene);
        Register(h); h.Scene.Styles.Theme = NazorLeafTheme.Theme; h.Scene.Transitions.ReducedMotion = reduced;
        h.Scene.Styles.Preferences = h.Scene.Styles.Preferences with { ReducedMotion = reduced, ForcedColors = forced };
        var root = h.Host.Render<LeafIconTogglePage>();
        var page = (LeafIconTogglePage)root.Component; Frame();
        var toggle = root.Single("ToggleButton"); var icon = root.Single("Icon");
        Assert.Equal(LeafSymbol.Star.Glyph, icon.Text());
        Assert.True(input.Activate(toggle)); Frame();
        Assert.True(page.Pressed); Assert.Equal(true, toggle.Attributes["pressed"]);
        Assert.Equal("Favorite", toggle.Attributes["accessible-name"]);
        Assert.Equal(LeafSymbol.StarFilled.Glyph, icon.Text());
        if (reduced || forced) Assert.Empty(h.Scene.Transitions.GetAnimations(icon));
        else
        {
            var animation = Assert.Single(h.Scene.Transitions.GetAnimations(icon));
            Assert.Equal("leaf-icon-pop", animation.Definition.Name);
            Assert.Equal(TimeSpan.FromMilliseconds(450), animation.Definition.Duration);
        }
        clock.Advance(500); Frame();
        Assert.True(input.Activate(toggle)); Frame();
        Assert.False(page.Pressed); Assert.Equal(LeafSymbol.Star.Glyph, icon.Text());
        Assert.Empty(h.Scene.Transitions.GetAnimations(icon));
        Assert.True(input.Activate(toggle)); Frame();
        Assert.True(page.Pressed); Assert.Equal(3, page.Changes);
        if (!reduced && !forced) Assert.Equal(clock.Now, Assert.Single(h.Scene.Transitions.GetAnimations(icon)).Trigger);
        void Frame() { h.Host.RunPending(); h.Render(160, 140, 1); h.Host.RunPending(); h.Render(160, 140, 1); }
    }

    [Fact]
    public void Disabled_selected_icon_keeps_its_artwork_and_rejects_activation_without_decorative_motion()
    {
        using var h = new RenderHarness(); using var input = new InputManager(h.Host.Renderer, h.Scene);
        Register(h); h.Scene.Styles.Theme = NazorLeafTheme.Theme; h.Scene.Transitions.ReducedMotion = false;
        var root = h.Host.Render<LeafIconTogglePage>(ParameterView.FromDictionary(new Dictionary<string, object?>
        {
            [nameof(LeafIconTogglePage.Pressed)] = true, [nameof(LeafIconTogglePage.Disabled)] = true,
        }));
        h.Host.RunPending(); h.Render(160, 140, 1);
        Assert.False(input.Activate(root.Single("ToggleButton")));
        Assert.Equal(0, ((LeafIconTogglePage)root.Component).Changes);
        Assert.Equal(LeafSymbol.StarFilled.Glyph, root.Single("Icon").Text());
        Assert.Empty(h.Scene.Transitions.GetAnimations(root.Single("Icon")));
    }

    private static void Register(RenderHarness h) => ((SkiaFontProvider)h.Graphics.Fonts).Register(Path.Combine(AppContext.BaseDirectory, NazorLeafTheme.AssetDirectory, "NazorLeafIcons.otf"));
    private static ParameterView IconParameters(LeafSymbol symbol) => ParameterView.FromDictionary(new Dictionary<string, object?>
    {
        [nameof(Icon.Symbol)] = symbol, [nameof(Icon.Style)] = "font-size: 96px; line-height: 1; color: black; background: white;",
    });
}
