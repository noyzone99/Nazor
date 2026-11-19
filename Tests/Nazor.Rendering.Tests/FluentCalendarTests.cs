using System.Globalization;
using Nazor.Controls;
using Nazor.Graphics;
using Nazor.Input;
using Nazor.Skia;
using Nazor.Style;
using Nazor.Testing;
using Nazor.Themes.NazorLeaf;
using Calendar = Nazor.Controls.Calendar;
using Color = Nazor.Style.Color;
using FluentThemes = Nazor.Themes.Fluent.Themes;

namespace Nazor.Rendering.Tests;

public sealed class FluentCalendarTests
{
    private static ParameterView Parameters(bool disabled = false, bool selectedToday = false, string culture = "en-US", CalendarViewMode view = CalendarViewMode.Month) =>
        ParameterView.FromDictionary(new Dictionary<string, object?>
        {
            [nameof(Calendar.Culture)] = CultureInfo.GetCultureInfo(culture),
            [nameof(Calendar.FirstDayOfWeek)] = DayOfWeek.Sunday,
            [nameof(Calendar.DisplayMonth)] = new DateOnly(2026, 11, 1),
            [nameof(Calendar.Value)] = new DateOnly(2026, 11, selectedToday ? 12 : 15),
            [nameof(Calendar.Today)] = new DateOnly(2026, 11, 12),
            [nameof(Calendar.IsDateUnavailable)] = (Func<DateOnly, bool>)(date => date == new DateOnly(2026, 11, 20)),
            [nameof(Calendar.Disabled)] = disabled,
            [nameof(Calendar.View)] = view,
        });

    private static void Configure(RenderHarness harness, ColorScheme scheme = ColorScheme.Light, bool forcedColors = false)
    {
        harness.Scene.Styles.Theme = FluentThemes.Fluent;
        harness.Scene.Styles.Preferences = new MediaPreferences(scheme, ReducedMotion: true, ForcedColors: forcedColors, Colors: SystemColors.Default);
        foreach (var file in FluentThemes.FontFiles) ((SkiaFontProvider)harness.Graphics.Fonts).Register(Path.Combine(AppContext.BaseDirectory, file));
    }

    private static ElementNode Day(RootComponent root, int number) => root.Elements("CalendarDay").Single(element =>
        element.Attributes.GetValueOrDefault("role") is "option" && element.Text() == number.ToString(CultureInfo.InvariantCulture));

    [Theory]
    [InlineData(1f)]
    [InlineData(1.5f)]
    [InlineData(2f)]
    public void The_month_surface_has_the_reference_cell_pitch_and_left_header_with_trailing_navigation(float scale)
    {
        using var harness = new RenderHarness();
        using var input = new InputManager(harness.Host.Renderer, harness.Scene);
        Configure(harness);
        var root = harness.Host.Render<Calendar>(Parameters());
        harness.Render(480, 480, scale);
        var calendar = harness.Scene.GetBounds(root.Single("Calendar"));
        var title = harness.Scene.GetBounds(root.Single("CalendarTitle"));
        var previous = harness.Scene.GetBounds(root.Single("CalendarPrevious"));
        var next = harness.Scene.GetBounds(root.Single("CalendarNext"));
        var first = harness.Scene.GetBounds(Day(root, 1));
        var second = harness.Scene.GetBounds(Day(root, 2));
        var weekday = harness.Scene.GetBounds(root.Elements("CalendarWeekday").First());
        Assert.InRange(calendar.Width, 300 - 1 / scale, 300 + 1 / scale);
        Assert.InRange(calendar.Height, 353 - 1 / scale, 353 + 1 / scale);
        Assert.Equal(40f, first.Width, 4);
        Assert.Equal(40f, first.Height, 4);
        Assert.InRange(second.X - first.X, 42 - 1 / scale, 42 + 1 / scale);
        Assert.InRange(first.X + first.Width / 2 - weekday.X - weekday.Width / 2, -1 / scale, 1 / scale);
        Assert.True(title.Right < previous.X && previous.Right < next.X);
        Assert.Equal(TextAlign.Start, harness.Scene.GetStyle(root.Single("CalendarTitle")).Get(StyleProperties.TextAlign));
        Assert.Equal(6, root.Elements("CalendarWeek").Count());
        var previousGlyph = harness.Scene.Styles.GetGeneratedElement(root.Single("CalendarPrevious"), PseudoElement.Before);
        var nextGlyph = harness.Scene.Styles.GetGeneratedElement(root.Single("CalendarNext"), PseudoElement.Before);
        Assert.Equal("\uE2BB", previousGlyph?.Text());
        Assert.Equal("\uF25F", nextGlyph?.Text());
    }

    [Theory]
    [InlineData(ColorScheme.Light)]
    [InlineData(ColorScheme.Dark)]
    public void Selection_today_hover_press_outside_blackout_and_focus_have_distinct_states(ColorScheme scheme)
    {
        using var harness = new RenderHarness();
        using var input = new InputManager(harness.Host.Renderer, harness.Scene);
        Configure(harness, scheme);
        var root = harness.Host.Render<Calendar>(Parameters());
        harness.Render(480, 480, 1);
        var ordinary = Day(root, 13);
        var selected = Day(root, 15);
        var today = Day(root, 12);
        var unavailable = Day(root, 20);
        var outside = root.Elements("CalendarDay").First(element => element.Attributes.GetValueOrDefault("outside") is true);
        var accent = scheme == ColorScheme.Light ? SystemColors.Default.AccentDark1 : SystemColors.Default.AccentLight2;
        Assert.Equal(0, harness.Scene.GetStyle(ordinary).BackgroundColor.A);
        Assert.Equal(0, harness.Scene.GetStyle(selected).BackgroundColor.A);
        Assert.Equal(accent, harness.Scene.GetStyle(selected).Get(StyleProperties.BorderTopColor));
        Assert.Equal(scheme == ColorScheme.Light ? SystemColors.Default.AccentDark2 : SystemColors.Default.AccentLight3, harness.Scene.GetStyle(selected).Color);
        Assert.Equal(accent, harness.Scene.GetStyle(today).BackgroundColor);
        Assert.Equal(400, harness.Scene.GetStyle(today).Get(StyleProperties.FontWeight));
        Assert.NotEqual(harness.Scene.GetStyle(ordinary).Color, harness.Scene.GetStyle(outside).Color);
        Assert.Equal(harness.Scene.GetStyle(ordinary).Color, harness.Scene.GetStyle(unavailable).Color);
        Assert.Equal(TextDecorationLine.LineThrough, harness.Scene.GetStyle(unavailable).Get(StyleProperties.TextDecorationLine));
        Hover(ordinary);
        var hover = harness.Scene.GetStyle(ordinary).BackgroundColor;
        Assert.NotEqual(0, hover.A);
        ordinary.SetState(ElementStates.Pressed, true);
        harness.Render(480, 480, 1);
        Assert.NotEqual(hover, harness.Scene.GetStyle(ordinary).BackgroundColor);
        ordinary.SetState(ElementStates.Pressed, false);
        Hover(today);
        Assert.NotEqual(accent, harness.Scene.GetStyle(today).BackgroundColor);
        input.Focus(ordinary, visible: true);
        harness.Render(480, 480, 1);
        Assert.Equal(Length.Px(2), harness.Scene.GetStyle(ordinary).Get(StyleProperties.OutlineWidth));
        Assert.Equal(Length.Px(1), harness.Scene.GetStyle(ordinary).Get(StyleProperties.OutlineInnerWidth));

        void Hover(ElementNode element)
        {
            var bounds = harness.Scene.GetBounds(element);
            input.PointerMoved(new Point(bounds.X + bounds.Width / 2, bounds.Y + bounds.Height / 2), KeyModifiers.None);
            harness.Render(480, 480, 1);
        }
    }

    [Theory]
    [InlineData(ColorScheme.Light)]
    [InlineData(ColorScheme.Dark)]
    public void Selected_today_has_an_inner_ring_and_disabled_dates_keep_their_state_distinction(ColorScheme scheme)
    {
        using var harness = new RenderHarness();
        using var input = new InputManager(harness.Host.Renderer, harness.Scene);
        Configure(harness, scheme);
        var root = harness.Host.Render<Calendar>(Parameters(selectedToday: true));
        harness.Render(480, 480, 1);
        Assert.Single(harness.Scene.GetStyle(Day(root, 12)).Get(StyleProperties.BoxShadow).Items);
        using var disabledHarness = new RenderHarness();
        using var disabledInput = new InputManager(disabledHarness.Host.Renderer, disabledHarness.Scene);
        Configure(disabledHarness, scheme);
        var disabled = disabledHarness.Host.Render<Calendar>(Parameters(disabled: true));
        disabledHarness.Render(480, 480, 1);
        Assert.False(disabledInput.Activate(Day(disabled, 13)));
        Assert.Equal(0, disabledHarness.Scene.GetStyle(Day(disabled, 13)).BackgroundColor.A);
        Assert.NotEqual(0, disabledHarness.Scene.GetStyle(Day(disabled, 12)).BackgroundColor.A);
        Assert.Equal(disabledHarness.Scene.GetStyle(Day(disabled, 15)).Get(StyleProperties.BorderTopColor), disabledHarness.Scene.GetStyle(Day(disabled, 12)).BackgroundColor);
    }

    [Theory]
    [InlineData("en-US")]
    [InlineData("it-IT")]
    [InlineData("fr-FR")]
    public void The_fluent_weekday_label_uses_the_cultures_shortest_names(string culture)
    {
        using var harness = new RenderHarness();
        Configure(harness);
        var root = harness.Host.Render<Calendar>(Parameters(culture: culture));
        harness.Render(480, 480, 1);
        var labels = root.Elements("CalendarWeekdayShortest").ToArray();
        Assert.Equal(CultureInfo.GetCultureInfo(culture).DateTimeFormat.ShortestDayNames, labels.Select(element => element.Text()));
        Assert.All(labels, element => Assert.NotEqual(Display.None, harness.Scene.GetStyle(element).Display));
        Assert.All(root.Elements("CalendarWeekdayAbbreviated"), element => Assert.Equal(Display.None, harness.Scene.GetStyle(element).Display));
        Assert.True(root.Single("CalendarWeekdays").Attributes.GetValueOrDefault("semantic-hidden") is true);
    }

    [Theory]
    [InlineData("basic")]
    [InlineData("leaf")]
    [InlineData("fluent")]
    public void Keyboard_header_order_is_title_previous_next_independently_of_visual_order(string theme)
    {
        using var harness = new RenderHarness();
        using var input = new InputManager(harness.Host.Renderer, harness.Scene);
        harness.Scene.Styles.Theme = theme switch { "fluent" => FluentThemes.Fluent, "leaf" => NazorLeafTheme.Theme, _ => Nazor.Themes.Themes.Basic };
        var root = harness.Host.Render<Calendar>(Parameters());
        harness.Render(480, 480, 1);
        input.KeyDown("Tab", KeyModifiers.None, false);
        Assert.Same(root.Single("CalendarTitle"), input.Focused);
        input.KeyDown("Tab", KeyModifiers.None, false);
        Assert.Same(root.Single("CalendarPrevious"), input.Focused);
        input.KeyDown("Tab", KeyModifiers.None, false);
        Assert.Same(root.Single("CalendarNext"), input.Focused);
        var title = harness.Scene.GetBounds(root.Single("CalendarTitle"));
        var previous = harness.Scene.GetBounds(root.Single("CalendarPrevious"));
        Assert.Equal(theme == "fluent", title.X < previous.X);
        Assert.All(root.Elements("CalendarWeekdayShortest"), element => Assert.Equal(theme == "fluent", harness.Scene.GetStyle(element).Display != Display.None));
        Assert.All(root.Elements("CalendarWeekdayAbbreviated"), element => Assert.Equal(theme != "fluent", harness.Scene.GetStyle(element).Display != Display.None));
    }

    [Theory]
    [InlineData(CalendarViewMode.Month)]
    [InlineData(CalendarViewMode.Year)]
    [InlineData(CalendarViewMode.Decade)]
    public void Header_tab_navigation_skips_the_title_when_there_is_no_larger_view(CalendarViewMode view)
    {
        using var harness = new RenderHarness();
        using var input = new InputManager(harness.Host.Renderer, harness.Scene);
        Configure(harness);
        var root = harness.Host.Render<Calendar>(Parameters(view: view));
        harness.Render(480, 480, 1);
        input.KeyDown("Tab", KeyModifiers.None, false);
        Assert.Same(root.Single(view == CalendarViewMode.Decade ? "CalendarPrevious" : "CalendarTitle"), input.Focused);
    }

    [Fact]
    public void Forced_colors_preserve_today_selection_and_disabled_contrast()
    {
        using var harness = new RenderHarness();
        Configure(harness, forcedColors: true);
        var root = harness.Host.Render<Calendar>(Parameters());
        harness.Render(480, 480, 1);
        Assert.Equal(SystemColors.Default.Canvas, harness.Scene.GetStyle(root.Single("Calendar")).BackgroundColor);
        Assert.Equal(SystemColors.Default.Highlight, harness.Scene.GetStyle(Day(root, 12)).BackgroundColor);
        Assert.Equal(SystemColors.Default.HighlightText, harness.Scene.GetStyle(Day(root, 12)).Color);
        Assert.Equal(SystemColors.Default.Highlight, harness.Scene.GetStyle(Day(root, 15)).Get(StyleProperties.BorderTopColor));
        Assert.Equal(SystemColors.Default.CanvasText, harness.Scene.GetStyle(Day(root, 15)).Color);
        Assert.Equal(SystemColors.Default.GrayText, harness.Scene.GetStyle(Day(root, 20)).Color);
        var outside = root.Elements("CalendarDay").First(element => element.Attributes.GetValueOrDefault("outside") is true);
        Assert.Equal(SystemColors.Default.LinkText, harness.Scene.GetStyle(outside).Color);
    }

    [Fact]
    public void The_date_popup_has_one_calendar_border_without_extra_presenter_padding()
    {
        using var harness = new RenderHarness();
        using var input = new InputManager(harness.Host.Renderer, harness.Scene);
        Configure(harness);
        var root = harness.Host.Render<DatePicker>(ParameterView.FromDictionary(new Dictionary<string, object?>
        {
            [nameof(DatePicker.Value)] = new DateOnly(2026, 11, 12),
            [nameof(DatePicker.Today)] = new DateOnly(2026, 11, 12),
            [nameof(DatePicker.IsOpen)] = true,
        }));
        harness.Render(480, 480, 1);
        harness.Host.RunPending();
        harness.Render(480, 480, 1);
        var popup = root.Single("DatePickerPopup");
        var calendar = root.Single("Calendar");
        Assert.Equal(Length.Zero, harness.Scene.GetStyle(popup).Get(StyleProperties.PaddingTop));
        Assert.Equal(Length.Zero, harness.Scene.GetStyle(popup).Get(StyleProperties.BorderTopWidth));
        Assert.Equal(Length.Px(8), harness.Scene.GetStyle(calendar).Get(StyleProperties.BorderTopLeftRadius).X);
        Assert.Equal(harness.Scene.GetBounds(calendar).Width, harness.Scene.GetBounds(popup).Width);
    }
}
