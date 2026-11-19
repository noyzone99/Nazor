using Nazor.Animation;
using Nazor.Controls;
using Nazor.Graphics;
using Nazor.Input;
using Nazor.Style;
using Nazor.Testing;
using Color = Nazor.Style.Color;
using FluentThemes = Nazor.Themes.Fluent.Themes;

namespace Nazor.Rendering.Tests;

public sealed class FluentThemeTests
{
    [Theory]
    [InlineData(ColorScheme.Light, false, 0xf3)]
    [InlineData(ColorScheme.Dark, false, 0x20)]
    [InlineData(ColorScheme.Dark, true, 0x05)]
    public void Current_controls_use_the_Fluent_palette(ColorScheme scheme, bool forcedColors, byte surface)
    {
        using var harness = new RenderHarness();
        using var input = new InputManager(harness.Host.Renderer, harness.Scene);
        var colors = SystemColors.Default with { Canvas = Color.FromRgb(5, 5, 5) };
        harness.Scene.Styles.Theme = FluentThemes.Fluent;
        harness.Scene.Styles.Preferences = new MediaPreferences(scheme, ReducedMotion: true, ForcedColors: forcedColors, Colors: colors);
        var root = harness.Host.Render<ThemedControls>();
        harness.Render(320, 320, 1);

        var accent = forcedColors ? colors.Highlight : scheme == ColorScheme.Light ? colors.AccentDark1 : colors.AccentLight2;
        Assert.Equal(Color.FromRgb(surface, surface, surface), Style("Window").BackgroundColor);
        Assert.Equal(accent, Style("CheckBoxMark").BackgroundColor);
        Assert.Equal(Length.Px(4), Style("CheckBoxMark").Get(StyleProperties.BorderTopLeftRadius).X);
        Assert.Equal(Length.Px(4), Style("TextField").Get(StyleProperties.BorderTopLeftRadius).X);
        var defaultButton = root.Elements("Button").Single(element => Equals(element.Attributes.GetValueOrDefault("default"), true));
        Assert.Equal(accent, harness.Scene.GetStyle(defaultButton).BackgroundColor);

        ComputedStyle Style(string name) => harness.Scene.GetStyle(root.Elements(name).Single());
    }

    [Theory]
    [InlineData(ColorScheme.Light, 0x0f, 0xf3, 0xff)]
    [InlineData(ColorScheme.Dark, 0x12, 0x20, 0x2b)]
    public void Controls_follow_the_WinUI_templates(ColorScheme scheme, byte strokeAlpha, byte surface, byte dialog)
    {
        using var harness = new RenderHarness();
        using var input = new InputManager(harness.Host.Renderer, harness.Scene);
        harness.Scene.Styles.Theme = FluentThemes.Fluent;
        harness.Scene.Styles.Preferences = new MediaPreferences(scheme, ReducedMotion: true, Colors: SystemColors.Default);
        harness.Scene.Transitions.ReducedMotion = true;
        var root = harness.Host.Render<FluentTemplateControls>();
        var image = harness.Render(480, 640, 1);
        var accent = scheme == ColorScheme.Light ? SystemColors.Default.AccentDark1 : SystemColors.Default.AccentLight2;
        var stroke = scheme == ColorScheme.Light ? new Color(0, 0, 0, strokeAlpha) : new Color(255, 255, 255, strokeAlpha);

        // A checked ToggleButton is an accent button; disabled buttons keep the ordinary hairline, accent ones none.
        Assert.Equal(accent, Style("ToggleButton").BackgroundColor);
        var disabled = root.Elements("Button").Where(e => Equals(e.Attributes.GetValueOrDefault("disabled"), true)).ToArray();
        Assert.Equal(stroke, harness.Scene.GetStyle(disabled.Single(e => !Equals(e.Attributes.GetValueOrDefault("default"), true))).Get(StyleProperties.BorderTopColor));
        Assert.Equal(0, harness.Scene.GetStyle(disabled.Single(e => Equals(e.Attributes.GetValueOrDefault("default"), true))).Get(StyleProperties.BorderTopColor).A);

        // Slider thumb 20px with a 12px accent core, 12px radio glyph, 3px progress indicator.
        Assert.Equal(Length.Px(20), Style("SliderThumb").Get(StyleProperties.Width).Length);
        Assert.Equal(Length.Px(4), Style("SliderThumb").Get(StyleProperties.BorderTopWidth));
        Assert.Equal(Length.Px(12), root.Elements("RadioButtonDot").Select(e => harness.Scene.GetStyle(e).Get(StyleProperties.Width).Length).Distinct().Single());
        Assert.Equal(Length.Px(3), Style("ProgressBarTrack").Get(StyleProperties.Height).Length);

        // In-page surfaces use ControlCornerRadius; the Expander header is 48px high.
        Assert.Equal(Length.Px(4), Style("Expander").Get(StyleProperties.BorderTopLeftRadius).X);
        Assert.Equal(Length.Px(48), Style("ExpanderHeader").Get(StyleProperties.MinHeight).Length);

        // Tooltip caption text; ContentDialog content over LayerFillColorAlt and commands over the base fill.
        Assert.Equal(Length.Px(12), Style("Tooltip").Get(StyleProperties.FontSize));
        Assert.Equal(Color.FromRgb(dialog, dialog, dialog), Style("Dialog").BackgroundColor);
        Assert.Equal(Color.FromRgb(surface, surface, surface), Style("DialogFooter").BackgroundColor);

        // The Segoe MDL2 text is hidden in favor of the packaged Fluent System Icons glyph.
        Assert.Equal(Color.Transparent, Style("MessageBoxIcon").Get(StyleProperties.Color));
        Assert.Equal(Length.Px(24), Style("MessageBoxIcon").Get(StyleProperties.Width).Length);

        // The selection pill sits on the leading edge, vertically centered, not across the full row height.
        var item = harness.Scene.GetBounds(root.Elements("ListViewItem").Single());
        Assert.Equal(Rgb(accent), Rgb(image.GetPixel((int)item.X + 1, (int)(item.Y + item.Height / 2))));
        Assert.NotEqual(Rgb(accent), Rgb(image.GetPixel((int)item.X + 1, (int)item.Y + 1)));

        static (byte, byte, byte) Rgb(dynamic color) => ((byte)color.R, (byte)color.G, (byte)color.B);

        ComputedStyle Style(string name) => harness.Scene.GetStyle(root.Elements(name).First());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void The_editable_combo_uses_the_Fluent_popup_and_respects_reduced_motion(bool reducedMotion)
    {
        var clock = new ManualAnimationClock();
        using var harness = new RenderHarness(clock: clock);
        using var input = new InputManager(harness.Host.Renderer, harness.Scene);
        harness.Scene.Styles.Theme = FluentThemes.Fluent;
        harness.Scene.Styles.Preferences = new MediaPreferences(ColorScheme.Dark, ReducedMotion: reducedMotion);
        harness.Scene.Transitions.ReducedMotion = reducedMotion;
        var root = harness.Host.Render<ComboBox<string>>(ParameterView.FromDictionary(
            new Dictionary<string, object?> { [nameof(ComboBox<string>.Items)] = new[] { "Alpha", "Beta" } }));
        harness.Render(320, 240, 1);

        var bounds = harness.Scene.GetBounds(root.Elements("ComboBoxChevron").Single());
        var point = new Point(bounds.X + bounds.Width / 2, bounds.Y + bounds.Height / 2);
        input.PointerPressed(point, PointerButton.Primary, KeyModifiers.None);
        input.PointerReleased(point, PointerButton.Primary, KeyModifiers.None);
        harness.Host.RunPending();
        harness.Render(320, 240, 1);

        var popup = root.Elements("ComboBoxPopup").Single();
        var style = harness.Scene.GetStyle(popup);
        Assert.Equal(Color.FromRgb(0x28, 0x28, 0x28), style.BackgroundColor);
        Assert.Equal(Length.Px(8), style.Get(StyleProperties.BorderTopLeftRadius).X);
        Assert.Equal(2, root.Elements("ComboBoxOption").Count());
        Assert.Equal(reducedMotion, harness.Scene.GetEffectiveValue(popup, StyleProperties.ClipPath).IsNone);

        clock.Advance(250);
        harness.Render(320, 240, 1);
        Assert.True(harness.Scene.GetEffectiveValue(popup, StyleProperties.ClipPath).IsNone);
    }
}

/// <summary>Controls and raw parts whose Fluent rules follow specific WinUI templates.</summary>
public sealed class FluentTemplateControls : ComponentBase
{
    private static RenderFragment Text(string text) => builder => builder.AddContent(0, text);

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "Window");
        builder.AddAttribute(1, "role", "window");
        builder.AddAttribute(2, "style", "padding: 12px; gap: 8px");
        builder.AddContent(3, (RenderFragment)(page =>
        {
            page.OpenComponent<ToggleButton>(0);
            page.AddComponentParameter(1, "IsPressed", true);
            page.AddComponentParameter(2, "ChildContent", Text("Grassetto"));
            page.CloseComponent();
            page.OpenComponent<Button>(10);
            page.AddComponentParameter(11, "Disabled", true);
            page.AddComponentParameter(12, "ChildContent", Text("Elimina"));
            page.CloseComponent();
            page.OpenComponent<Button>(20);
            page.AddComponentParameter(21, "IsDefault", true);
            page.AddComponentParameter(22, "Disabled", true);
            page.AddComponentParameter(23, "ChildContent", Text("Conferma"));
            page.CloseComponent();
            page.OpenComponent<Slider>(30);
            page.AddComponentParameter(31, "Value", 40.0);
            page.CloseComponent();
            page.OpenComponent<RadioGroup<int>>(40);
            page.AddComponentParameter(41, "Value", 1);
            page.AddComponentParameter(42, "ChildContent", (RenderFragment)(group =>
            {
                group.OpenComponent<RadioButton<int>>(0);
                group.AddComponentParameter(1, "Value", 1);
                group.AddComponentParameter(2, "ChildContent", Text("Giornaliero"));
                group.CloseComponent();
            }));
            page.CloseComponent();
            page.OpenComponent<ProgressBar>(50);
            page.AddComponentParameter(51, "Value", (double?)60);
            page.CloseComponent();
            page.OpenComponent<Expander>(60);
            page.AddComponentParameter(61, "Header", "Avanzate");
            page.CloseComponent();
            page.OpenElement(70, "Tooltip");
            page.AddContent(71, "Suggerimento");
            page.CloseElement();
            page.OpenElement(80, "Dialog");
            page.AddContent(81, (RenderFragment)(dialog =>
            {
                dialog.OpenElement(0, "DialogTitle");
                dialog.AddContent(1, "Salvare?");
                dialog.CloseElement();
                dialog.OpenElement(2, "DialogFooter");
                dialog.AddContent(3, "Comandi");
                dialog.CloseElement();
            }));
            page.CloseElement();
            page.OpenElement(90, "MessageBoxIcon");
            page.AddAttribute(91, "icon", "warning");
            page.AddContent(92, "");
            page.CloseElement();
            page.OpenElement(100, "ListViewItem");
            page.AddAttribute(101, "selected", true);
            page.AddAttribute(102, "style", "flex: none; width: 200px; height: 36px");
            page.AddContent(103, "Riga selezionata");
            page.CloseElement();
        }));
        builder.CloseElement();
    }
}
