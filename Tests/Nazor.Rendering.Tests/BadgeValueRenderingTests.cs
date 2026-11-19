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

public sealed class BadgeValuePage : ComponentBase
{
    public int? Count { get; set; } = 3;
    public bool IconMode { get; set; }
    public ControlSize Size { get; set; } = ControlSize.Medium;
    public void Refresh() => StateHasChanged();
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "Page"); builder.AddAttribute(1, "style", "padding: 40px;");
        builder.OpenComponent<Badge>(10);
        builder.AddComponentParameter(11, nameof(Badge.Count), IconMode ? null : Count);
        builder.AddComponentParameter(12, nameof(Badge.Icon), LeafSymbol.At);
        builder.AddComponentParameter(13, nameof(Badge.Size), Size);
        builder.AddComponentParameter(14, nameof(Badge.AccessibleName), "Mentions");
        builder.CloseComponent(); builder.CloseElement();
    }
}

public sealed class BadgeValueRenderingTests
{
    [Theory]
    [InlineData("basic", false)]
    [InlineData("basic", true)]
    [InlineData("fluent", false)]
    [InlineData("fluent", true)]
    [InlineData("leaf", false)]
    [InlineData("leaf", true)]
    public void Count_roll_is_theme_owned_restarts_only_for_new_keys_and_keeps_the_indicator(string theme, bool reduced)
    {
        var clock = new ManualAnimationClock(); using var h = new RenderHarness(clock: clock);
        using var input = new InputManager(h.Host.Renderer, h.Scene); Register(h);
        h.Scene.Styles.Theme = Theme(theme); h.Scene.Transitions.ReducedMotion = reduced;
        h.Scene.Styles.Preferences = h.Scene.Styles.Preferences with { ReducedMotion = reduced };
        var root = h.Host.Render<BadgeValuePage>(); var page = (BadgeValuePage)root.Component; Frame();
        var indicator = root.Single("BadgeIndicator"); var original = root.Single("BadgeValue");
        Assert.Empty(h.Scene.Transitions.GetAnimations(original));
        clock.Advance(500); Frame(); page.Count = 4; page.Refresh(); var start = Frame();
        var value = root.Single("BadgeValue"); Assert.NotSame(original, value); Assert.True(original.IsRemoved);
        Assert.Same(indicator, root.Single("BadgeIndicator")); Assert.True(value.Attributes.GetValueOrDefault("changed") is true);
        if (theme == "leaf" && !reduced)
        {
            var animation = Assert.Single(h.Scene.Transitions.GetAnimations(value));
            Assert.Equal("leaf-badge-roll", animation.Definition.Name);
            Assert.Equal(TimeSpan.FromMilliseconds(350), animation.Definition.Duration);
            Assert.Equal(clock.Now, animation.Trigger);
            Assert.Equal(0, h.Scene.GetEffectiveValue(value, StyleProperties.Opacity));
            clock.Advance(175); var middle = Frame();
            Assert.InRange(h.Scene.GetEffectiveValue(value, StyleProperties.Opacity), 0.01, 0.99);
            Assert.NotEqual(h.Scene.GetStyle(value).Get(StyleProperties.Transform), h.Scene.GetEffectiveValue(value, StyleProperties.Transform));
            var box = h.Scene.GetBounds(indicator); var different = 0;
            for (var y = (int)box.Y; y < box.Bottom; y++) for (var x = (int)box.X; x < box.Right; x++)
                if (start.GetPixel(x, y) != middle.GetPixel(x, y)) different++;
            Assert.True(different > 0, "The count must paint new ink while rolling into the indicator.");
            page.Refresh(); Frame(); Assert.Same(value, root.Single("BadgeValue"));
            Assert.Equal(animation.Trigger, Assert.Single(h.Scene.Transitions.GetAnimations(value)).Trigger);
            clock.Advance(175); Frame();
        }
        else Assert.Empty(h.Scene.Transitions.GetAnimations(value));
        Assert.Equal(1, h.Scene.GetEffectiveValue(value, StyleProperties.Opacity));
        Assert.Equal(h.Scene.GetStyle(value).Get(StyleProperties.Transform), h.Scene.GetEffectiveValue(value, StyleProperties.Transform));
        page.Count = 100; page.Refresh(); Frame(); var capped = root.Single("BadgeValue"); Assert.Equal("99+", capped.Text());
        page.Count = 101; page.Refresh(); Frame(); Assert.NotSame(capped, root.Single("BadgeValue")); Assert.True(capped.IsRemoved);
        Assert.Single(root.Elements("BadgeValue")); Assert.Same(indicator, root.Single("BadgeIndicator"));
        page.Count = 0; page.Refresh(); Frame(); Assert.Empty(root.Elements("BadgeValue"));
        page.Count = 1; page.Refresh(); Frame();
        Assert.Equal("1", root.Single("BadgeValue").Text());
        Assert.True(root.Single("BadgeValue").Attributes.GetValueOrDefault("changed") is true);
        if (theme == "leaf" && !reduced)
        {
            var returned = Assert.Single(h.Scene.Transitions.GetAnimations(root.Single("BadgeValue")));
            Assert.Equal("leaf-badge-roll", returned.Definition.Name); Assert.Equal(clock.Now, returned.Trigger);
            h.Scene.Transitions.ReducedMotion = true;
            h.Scene.Styles.Preferences = h.Scene.Styles.Preferences with { ReducedMotion = true };
            Frame(); Assert.Empty(h.Scene.Transitions.GetAnimations(root.Single("BadgeValue")));
            Assert.Equal(1, h.Scene.GetEffectiveValue(root.Single("BadgeValue"), StyleProperties.Opacity));
        }
        else Assert.Empty(h.Scene.Transitions.GetAnimations(root.Single("BadgeValue")));
        RenderedImage Frame() { h.Host.RunPending(); h.Render(300, 180, 1); h.Host.RunPending(); return h.Render(300, 180, 1); }
    }

    [Theory]
    [InlineData("leaf", ControlSize.Small, 14, 1f)]
    [InlineData("leaf", ControlSize.Medium, 18, 1f)]
    [InlineData("leaf", ControlSize.Large, 22, 1f)]
    [InlineData("leaf", ControlSize.Small, 14, 1.5f)]
    [InlineData("leaf", ControlSize.Medium, 18, 1.5f)]
    [InlineData("leaf", ControlSize.Large, 22, 1.5f)]
    [InlineData("basic", ControlSize.Medium, 16, 1.5f)]
    [InlineData("fluent", ControlSize.Medium, 16, 1.5f)]
    public void Typed_badge_icons_have_square_geometry_and_paint_the_registered_private_glyph(string theme, ControlSize size, int expected, float scale)
    {
        using var h = new RenderHarness(); Register(h); h.Scene.Styles.Theme = Theme(theme);
        h.Scene.Transitions.ReducedMotion = true; h.Scene.Styles.Preferences = h.Scene.Styles.Preferences with { ReducedMotion = true };
        var root = h.Host.Render<BadgeValuePage>(); var page = (BadgeValuePage)root.Component; page.IconMode = true; page.Size = size; page.Refresh();
        h.Host.RunPending(); h.Render(300, 180, scale); h.Host.RunPending(); var image = h.Render(300, 180, scale);
        var indicator = root.Single("BadgeIndicator"); var box = h.Scene.GetBounds(indicator);
        Assert.Equal(expected, box.Width); Assert.Equal(expected, box.Height);
        Assert.Equal("Mentions", indicator.Attributes["accessible-name"]);
        Assert.True(root.Single("BadgeIcon").Attributes.GetValueOrDefault("semantic-hidden") is true);
        var drawing = Assert.Single(h.Scene.BuildDisplayList().Commands.OfType<DrawText>());
        var run = Assert.Single(drawing.Layout.Shaped.Runs); Assert.Equal("NazorLeaf Icons", run.Face.FamilyName);
        Assert.All(run.Glyphs, glyph => Assert.NotEqual((ushort)0, glyph));
        var colors = new HashSet<Nazor.Graphics.Color>();
        for (var y = (int)MathF.Ceiling((box.Y + 3) * scale); y < (box.Bottom - 3) * scale; y++)
        for (var x = (int)MathF.Ceiling((box.X + 3) * scale); x < (box.Right - 3) * scale; x++) colors.Add(image.GetPixel(x, y));
        Assert.True(colors.Count > 1, "The indicator's interior must contain painted icon ink and background.");
    }

    private static StyleSheet Theme(string value) => value switch
    {
        "fluent" => Nazor.Themes.Fluent.Themes.Fluent,
        "leaf" => NazorLeafTheme.Theme,
        _ => Nazor.Themes.Themes.Basic,
    };
    private static void Register(RenderHarness h) => ((SkiaFontProvider)h.Graphics.Fonts).Register(Path.Combine(AppContext.BaseDirectory, NazorLeafTheme.AssetDirectory, "NazorLeafIcons.otf"));
}
