using Nazor.Controls;
using Nazor.Graphics;
using Nazor.Input;
using Nazor.Nss;
using Nazor.Style;
using Nazor.Testing;
using Nazor.Themes.NazorLeaf;

namespace Nazor.Rendering.Tests;

public sealed class DialAppearancePage : ComponentBase
{
    private bool disabled;
    public Dial Dial { get; private set; } = null!;
    public void Disable() { disabled = true; StateHasChanged(); }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "Surface");
        builder.AddAttribute(1, "style", "padding: 24px; background: Canvas;");
        builder.OpenComponent<Dial>(10);
        builder.AddComponentParameter(11, nameof(Dial.Value), 50d);
        builder.AddComponentParameter(12, nameof(Dial.ShowValue), false);
        builder.AddComponentParameter(13, nameof(Dial.Disabled), disabled);
        builder.AddComponentReferenceCapture(14, component => Dial = (Dial)component);
        builder.CloseComponent();
        builder.CloseElement();
    }
}

public sealed class DialAppearanceRenderingTests
{
    public static IEnumerable<object[]> Presentations()
    {
        foreach (var theme in new[] { "basic", "fluent", "leaf" })
        foreach (var scheme in new[] { ColorScheme.Light, ColorScheme.Dark })
        foreach (var scale in new[] { 1f, 1.5f, 2f })
        foreach (var forcedColors in new[] { false, true })
            yield return [theme, scheme, scale, forcedColors];
    }

    [Theory]
    [MemberData(nameof(Presentations))]
    public void Hover_uses_theme_tokens_and_keyboard_focus_follows_the_circular_pointer(string theme, ColorScheme scheme, float scale, bool forcedColors)
    {
        using var harness = new RenderHarness();
        using var input = new InputManager(harness.Host.Renderer, harness.Scene);
        harness.Scene.Styles.Theme = theme switch
        {
            "fluent" => Nazor.Themes.Fluent.Themes.Fluent,
            "leaf" => NazorLeafTheme.Theme,
            _ => Nazor.Themes.Themes.Basic,
        };
        var colors = SystemColors.Default with
        {
            Canvas = Style.Color.Black, CanvasText = Style.Color.White,
            ButtonFace = Style.Color.Black, ButtonText = Style.Color.White,
            ButtonBorder = Style.Color.White,
            Highlight = Style.Color.FromRgb(0xf4, 0xdd, 0x5e),
            LinkText = Style.Color.FromRgb(0x41, 0xdc, 0xec),
        };
        harness.Scene.Styles.Preferences = new MediaPreferences(scheme, ReducedMotion: true, ForcedColors: forcedColors, Colors: forcedColors ? colors : null);
        var application = NssCompiler.Compile("DialAppearance", """
            :root {
                --nz-accent: #cc3366; --nz-on-surface: #000000;
                --nz-stroke-bottom: #2477b1; --nz-control-hover: #c2e4ee;
                --nz-focus: #b72480; --nz-focus-inner: #ffffff;
                --leaf-ink: #286520; --leaf-focus-ring: #abb747;
            }
            """);
        Assert.True(application.Success, string.Join("\n", application.Diagnostics));
        if (!forcedColors) harness.Scene.Styles.Application = application.StyleSheet;
        var root = harness.Host.Render<DialAppearancePage>();
        var dial = root.Elements("Dial").Single();
        var pointer = root.Elements("DialPointer").Single();
        var idle = Frame();
        var bounds = harness.Scene.GetBounds(dial);
        var center = new Point(bounds.X + bounds.Width / 2, bounds.Y + bounds.Height / 2);

        input.PointerMoved(center, KeyModifiers.None);
        var hovered = Frame();
        var hoverStyle = harness.Scene.GetStyle(pointer);
        if (theme == "leaf")
            Assert.Contains(hoverStyle.Get(StyleProperties.BoxShadow).Items,
                shadow => shadow.Inset && shadow.Color == (forcedColors ? colors.LinkText : Style.Color.FromRgb(0x28, 0x65, 0x20)));
        else
        {
            var expected = theme == "fluent"
                ? forcedColors ? colors.ButtonText : Style.Color.FromRgb(0x24, 0x77, 0xb1)
                : forcedColors ? colors.Highlight : Style.Color.FromRgb(0xcc, 0x33, 0x66);
            Assert.Equal(expected,
                hoverStyle.Get(StyleProperties.BorderTopColor));
            if (theme == "fluent") Assert.Equal(forcedColors ? colors.ButtonFace : Style.Color.FromRgb(0xc2, 0xe4, 0xee), hoverStyle.Get(StyleProperties.BackgroundColor));
        }
        if (!forcedColors || theme is "basic" or "leaf") Assert.True(ChangedPixels(idle, hovered, bounds) > 0);
        if (theme == "fluent")
        {
            input.PointerPressed(center, PointerButton.Primary, KeyModifiers.None);
            Frame();
            Assert.Equal(forcedColors ? colors.Highlight : Style.Color.FromRgb(0xcc, 0x33, 0x66),
                harness.Scene.GetStyle(pointer).Get(StyleProperties.BorderTopColor));
            input.PointerCanceled();
            Frame();
            Assert.False(((DialAppearancePage)root.Component).Dial.IsDragging);
        }

        input.PointerMoved(new Point(240, 150), KeyModifiers.None);
        input.Focus(null, visible: false);
        Assert.False(dial.States.HasFlag(ElementStates.FocusVisible));
        idle = Frame();
        input.Focus(dial, visible: true);
        Assert.True(dial.States.HasFlag(ElementStates.FocusVisible));
        var focused = Frame();
        Assert.Equal(BorderStyle.None, harness.Scene.GetStyle(dial).Get(StyleProperties.OutlineStyle));
        var focusStyle = harness.Scene.GetStyle(pointer);
        if (theme == "leaf" && !forcedColors)
        {
            Assert.Equal(BorderStyle.None, focusStyle.Get(StyleProperties.OutlineStyle));
            Assert.Contains(focusStyle.Get(StyleProperties.BoxShadow).Items,
                shadow => !shadow.Inset && shadow.Color == Style.Color.FromRgb(0xab, 0xb7, 0x47));
        }
        else
        {
            Assert.Equal(BorderStyle.Solid, focusStyle.Get(StyleProperties.OutlineStyle));
            var expected = theme == "fluent" || theme == "leaf"
                ? forcedColors ? colors.CanvasText : Style.Color.FromRgb(0xb7, 0x24, 0x80)
                : forcedColors ? colors.Highlight : Style.Color.FromRgb(0xcc, 0x33, 0x66);
            Assert.Equal(expected,
                focusStyle.Get(StyleProperties.OutlineColor));
        }
        Assert.True(ChangedPixels(idle, focused, bounds) > 0);
        var pointerBounds = harness.Scene.GetBounds(pointer);
        var pointerRadius = pointerBounds.Width / 2;
        var focusPixels = 0;
        for (var y = (int)MathF.Floor((bounds.Y - 6) * scale); y < MathF.Ceiling((bounds.Bottom + 6) * scale); y++)
        for (var x = (int)MathF.Floor((bounds.X - 6) * scale); x < MathF.Ceiling((bounds.Right + 6) * scale); x++)
        {
            if (idle.GetPixel(x, y) == focused.GetPixel(x, y)) continue;
            var dx = (x + 0.5f) / scale - center.X;
            var dy = (y + 0.5f) / scale - center.Y;
            Assert.InRange(MathF.Sqrt(dx * dx + dy * dy), pointerRadius - 3, pointerRadius + 7);
            focusPixels++;
        }
        Assert.True(focusPixels > 0);

        input.Focus(null, visible: false);
        ((DialAppearancePage)root.Component).Disable();
        var disabled = Frame();
        input.PointerMoved(center, KeyModifiers.None);
        var disabledHover = Frame();
        Assert.Equal(0, ChangedPixels(disabled, disabledHover, bounds));

        RenderedImage Frame()
        {
            harness.Host.RunPending();
            return harness.Render(280, 180, scale);
        }

        int ChangedPixels(RenderedImage before, RenderedImage after, Rect area)
        {
            var count = 0;
            for (var y = (int)MathF.Floor(area.Y * scale); y < MathF.Ceiling(area.Bottom * scale); y++)
            for (var x = (int)MathF.Floor(area.X * scale); x < MathF.Ceiling(area.Right * scale); x++)
                if (before.GetPixel(x, y) != after.GetPixel(x, y)) count++;
            return count;
        }
    }
}
