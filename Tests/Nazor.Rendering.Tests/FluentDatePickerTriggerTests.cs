using System.Globalization;
using Nazor.Controls;
using Nazor.Graphics;
using Nazor.Input;
using Nazor.Skia;
using Nazor.Style;
using Nazor.Testing;
using Nazor.Themes.Fluent;
using Color = Nazor.Style.Color;
using FluentThemes = Nazor.Themes.Fluent.Themes;

namespace Nazor.Rendering.Tests;

public sealed class FluentDatePickerTriggerTests
{
    [Theory]
    [InlineData(ColorScheme.Light, false)]
    [InlineData(ColorScheme.Dark, false)]
    [InlineData(ColorScheme.Light, true)]
    [InlineData(ColorScheme.Dark, true)]
    public void The_date_trigger_uses_a_real_packaged_calendar_glyph_and_retains_disabled_contrast(ColorScheme scheme, bool disabled)
    {
        using var harness = new RenderHarness();
        foreach (var file in FluentThemes.FontFiles) ((SkiaFontProvider)harness.Graphics.Fonts).Register(Path.Combine(AppContext.BaseDirectory, file));
        using var input = new InputManager(harness.Host.Renderer, harness.Scene);
        harness.Scene.Styles.Theme = FluentThemes.Fluent;
        harness.Scene.Styles.Preferences = new MediaPreferences(scheme, ReducedMotion: true);
        var root = harness.Host.Render<DatePicker>(ParameterView.FromDictionary(new Dictionary<string, object?>
        {
            [nameof(DatePicker.Culture)] = CultureInfo.GetCultureInfo("en-US"),
            [nameof(DatePicker.Disabled)] = disabled,
            [nameof(DatePicker.Style)] = "width: 240px",
        }));
        harness.Render(400, 160, 1);
        var button = root.Single("DatePickerButton");
        var style = harness.Scene.GetStyle(button);
        var mark = Assert.IsType<ElementNode>(harness.Scene.Styles.GetGeneratedElement(button, PseudoElement.Before));
        Assert.Equal(FluentSymbol.CalendarLtr20Regular.Glyph, mark.Text());
        Assert.Equal(new[] { "FluentSystemIcons-Regular" }, harness.Scene.GetStyle(mark).Get(StyleProperties.FontFamily).Items);
        Assert.Equal(Length.Px(12), harness.Scene.GetStyle(mark).Get(StyleProperties.FontSize));
        Assert.Equal(0d, style.Get(StyleProperties.StrokeTrimEnd));
        Assert.Equal(32, harness.Scene.GetBounds(button).Width);
        Assert.Equal(32, harness.Scene.GetBounds(root.Single("DatePicker")).Height);
        Assert.Equal(style.Color, harness.Scene.GetStyle(mark).Color);
        Assert.Equal(0, style.Get(StyleProperties.BorderTopColor).A);
        var calendarInk = Assert.Single(harness.Scene.BuildDisplayList().Commands.OfType<DrawText>(), command => command.Layout.Text == FluentSymbol.CalendarLtr20Regular.Glyph);
        Assert.All(calendarInk.Layout.Shaped.Runs, run =>
        {
            Assert.Equal("FluentSystemIcons-Regular", run.Face.FamilyName);
            Assert.All(run.Glyphs, glyph => Assert.NotEqual((ushort)0, glyph));
        });
        if (disabled) Assert.Equal(scheme == ColorScheme.Light ? new Color(0, 0, 0, 0x5c) : new Color(255, 255, 255, 0x5d), style.Color);
    }

    [Fact]
    public void Hover_keeps_the_date_trigger_integrated_with_the_field_and_click_opens_the_calendar()
    {
        using var harness = new RenderHarness();
        using var input = new InputManager(harness.Host.Renderer, harness.Scene);
        harness.Scene.Styles.Theme = FluentThemes.Fluent;
        var root = harness.Host.Render<DatePicker>(ParameterView.FromDictionary(new Dictionary<string, object?>
        {
            [nameof(DatePicker.Value)] = new DateOnly(2026, 11, 12),
            [nameof(DatePicker.Today)] = new DateOnly(2026, 11, 12),
            [nameof(DatePicker.Style)] = "width: 240px",
        }));
        harness.Render(480, 480, 1);
        var bounds = harness.Scene.GetBounds(root.Single("DatePickerButton"));
        var point = new Point(bounds.X + bounds.Width / 2, bounds.Y + bounds.Height / 2);
        input.PointerMoved(point, KeyModifiers.None);
        harness.Render(480, 480, 1);
        Assert.Equal(0, harness.Scene.GetStyle(root.Single("DatePickerButton")).Get(StyleProperties.BorderTopColor).A);
        input.PointerPressed(point, PointerButton.Primary, KeyModifiers.None);
        input.PointerReleased(point, PointerButton.Primary, KeyModifiers.None);
        harness.Host.RunPending();
        harness.Render(480, 480, 1);
        Assert.Single(root.Elements("DatePickerPopup"));
        Assert.Single(root.Elements("Calendar"));
    }
}
