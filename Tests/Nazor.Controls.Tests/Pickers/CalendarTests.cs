using System.Globalization;
using Nazor.Controls.Primitives;
using Nazor.Controls.Tests.Pickers.Fixtures;
using Nazor.Input;

namespace Nazor.Controls.Tests.Pickers;

[Collection("Controls")]
public sealed class CalendarTests : IDisposable
{
    private readonly ControlHarness h = new(width: 900, height: 900);

    public void Dispose() => h.Dispose();

    private IEnumerable<ElementNode> In(string className, string name) => h.ByClass(className).Elements().Where(e => e.Name == name);

    private ElementNode Day(string className, int day, int? month = null) =>
        In(className, "CalendarDay").Single(e => e.Attributes.GetValueOrDefault("role") is "option" && e.Text() == day.ToString(CultureInfo.InvariantCulture)
            && (month is null || e.Attributes.GetValueOrDefault("accessible-name") is string name && name.Contains(CultureInfo.GetCultureInfo("it-IT").DateTimeFormat.MonthNames[month.Value - 1])));

    private ElementNode Current(string className) => In(className, "CalendarDay").Concat(In(className, "CalendarItem")).Single(e => e.Attributes.GetValueOrDefault("current") is true);

    private void Refresh(Action<CalendarProbe> change)
    {
        var probe = h.Component<CalendarProbe>();
        h.Host.Renderer.Dispatcher.InvokeAsync(() => { change(probe); probe.Refresh(); });
        h.Frame();
    }

    [Fact]
    public void The_month_grid_follows_the_culture_and_marks_today_the_value_and_unavailable_days()
    {
        h.Render<CalendarProbe>();
        var weekdays = In("single", "CalendarWeekdayAbbreviated").Select(e => e.Text()).ToList();
        Assert.Equal(7, weekdays.Count);
        Assert.Equal("lun", weekdays[0]);
        Assert.Equal("Marzo 2026", In("single", "CalendarTitle").Single().Text());
        var first = In("single", "CalendarDay").First();
        Assert.Equal(true, first.Attributes["outside"]);
        Assert.Equal("23", first.Text());
        var selected = Day("single", 18);
        Assert.Equal(true, selected.Attributes["selected"]);
        Assert.Equal(true, selected.Attributes["current"]);
        Assert.Equal(0, selected.Attributes["tabindex"]);
        Assert.Equal("mercoledì 18 marzo 2026", selected.Attributes["accessible-name"]);
        Assert.Equal(18, selected.Attributes["position-in-set"]);
        Assert.Equal(31, selected.Attributes["size-of-set"]);
        Assert.Equal(true, Day("single", 10).Attributes["today"]);
        Assert.Equal(true, Day("single", 19).Attributes["unavailable"]);
        Assert.Equal(true, Day("single", 21).Attributes["weekend"]);
        Assert.Equal("listbox", In("single", "CalendarGrid").Single().Attributes["role"]);
        Assert.Equal("calendar", h.ByClass("single").Attributes["role"]);
        Assert.Single(In("single", "CalendarDay"), e => e.Attributes.GetValueOrDefault("tabindex") is 0);
    }

    [Fact]
    public void A_us_calendar_starts_on_sunday()
    {
        h.Render<CalendarProbe>();
        Refresh(p => p.Culture = CultureInfo.GetCultureInfo("en-US"));
        Assert.Equal("Sun", In("single", "CalendarWeekdayAbbreviated").First().Text());
        Assert.Equal("March 2026", In("single", "CalendarTitle").Single().Text());
        Assert.Equal("Wednesday, March 18, 2026", Day("single", 18).Attributes["accessible-name"]);
        Assert.Equal("1", In("single", "CalendarDay").First().Text());
    }

    [Fact]
    public void Clicking_a_day_selects_it_and_ignores_unavailable_days()
    {
        h.Render<CalendarProbe>();
        var probe = h.Component<CalendarProbe>();
        h.Click(Day("single", 5));
        Assert.Equal(new DateOnly(2026, 3, 5), probe.Value);
        Assert.Equal(true, Day("single", 5).Attributes["selected"]);
        h.Click(Day("single", 19));
        Assert.Equal(new DateOnly(2026, 3, 5), probe.Value);
    }

    [Fact]
    public void Arrows_move_by_day_and_week_and_enter_selects()
    {
        h.Render<CalendarProbe>();
        var probe = h.Component<CalendarProbe>();
        h.Focus(Day("single", 18));
        h.Press("ArrowRight");
        Assert.Equal("19", Current("single").Text());
        Assert.Same(Current("single"), h.Focused);
        h.Press("ArrowDown");
        Assert.Equal("26", Current("single").Text());
        h.Press("ArrowLeft");
        h.Press("ArrowUp");
        Assert.Equal("18", Current("single").Text());
        h.Press("Home");
        Assert.Equal("16", Current("single").Text());
        h.Press("End");
        Assert.Equal("22", Current("single").Text());
        h.Press("Enter");
        Assert.Equal(new DateOnly(2026, 3, 22), probe.Value);
        h.Press("ArrowLeft");
        h.Press("ArrowLeft");
        h.Press("ArrowLeft");
        h.Press(" ");
        Assert.Equal(new DateOnly(2026, 3, 22), probe.Value);
    }

    [Fact]
    public void Page_keys_change_month_and_year_and_moving_past_the_month_shows_the_next()
    {
        h.Render<CalendarProbe>();
        h.Focus(Day("single", 18));
        h.Press("PageDown");
        Assert.Equal("Aprile 2026", In("single", "CalendarTitle").Single().Text());
        Assert.Equal("18", Current("single").Text());
        h.Press("PageUp", KeyModifiers.Shift);
        Assert.Equal("Aprile 2025", In("single", "CalendarTitle").Single().Text());
        h.Press("End", KeyModifiers.Control);
        Assert.Equal("30", Current("single").Text());
        h.Press("ArrowRight");
        Assert.Equal("Maggio 2025", In("single", "CalendarTitle").Single().Text());
        Assert.Equal("1", Current("single").Text());
        Assert.Same(Current("single"), h.Focused);
    }

    [Fact]
    public void Limits_clamp_keyboard_movement_and_disable_navigation_buttons()
    {
        h.Render<CalendarProbe>();
        Refresh(p => { p.Minimum = new DateOnly(2026, 3, 3); p.Maximum = new DateOnly(2026, 3, 25); });
        Assert.True(In("single", "CalendarPrevious").Single().IsDisabled);
        Assert.True(In("single", "CalendarNext").Single().IsDisabled);
        Assert.Equal(true, Day("single", 2).Attributes["unavailable"]);
        h.Focus(Day("single", 18));
        h.Press("PageDown");
        Assert.Equal("25", Current("single").Text());
        h.Press("PageUp");
        Assert.Equal("3", Current("single").Text());
        h.Press("ArrowUp");
        Assert.Equal("3", Current("single").Text());
        h.Click(Day("single", 2));
        Assert.Equal(new DateOnly(2026, 3, 18), h.Component<CalendarProbe>().Value);
    }

    [Fact]
    public void The_navigation_buttons_move_by_month()
    {
        h.Render<CalendarProbe>();
        h.Click(In("single", "CalendarNext").Single());
        Assert.Equal("Aprile 2026", In("single", "CalendarTitle").Single().Text());
        Assert.Equal("Next month", In("single", "CalendarNext").Single().Attributes["accessible-name"]);
        h.Click(In("single", "CalendarPrevious").Single());
        h.Click(In("single", "CalendarPrevious").Single());
        Assert.Equal("Febbraio 2026", In("single", "CalendarTitle").Single().Text());
    }

    [Fact]
    public void The_title_zooms_out_to_months_and_years_and_items_zoom_back_in()
    {
        h.Render<CalendarProbe>();
        h.Click(In("single", "CalendarTitle").Single());
        Assert.Equal("year", h.ByClass("single").Attributes["view"]);
        var months = In("single", "CalendarItem").ToList();
        Assert.Equal(12, months.Count);
        Assert.Equal("mar", months[2].Text());
        Assert.Equal(true, months[2].Attributes["selected"]);
        Assert.Equal(true, months[2].Attributes["current"]);
        Assert.Same(months[2], h.Focused);
        h.Press("ArrowDown");
        Assert.Equal("lug", Current("single").Text());
        h.Click(In("single", "CalendarTitle").Single());
        Assert.Equal("decade", h.ByClass("single").Attributes["view"]);
        var years = In("single", "CalendarItem").ToList();
        Assert.Equal(12, years.Count);
        Assert.Equal(true, years[0].Attributes["outside"]);
        Assert.Equal("2020–2029", In("single", "CalendarTitle").Single().Text());
        Assert.True(In("single", "CalendarTitle").Single().IsDisabled);
        h.Press("ArrowRight");
        Assert.Equal("2027", Current("single").Text());
        h.Press("Enter");
        Assert.Equal("year", h.ByClass("single").Attributes["view"]);
        h.Click(In("single", "CalendarItem").ToList()[0]);
        Assert.Equal("month", h.ByClass("single").Attributes["view"]);
        Assert.Equal("Gennaio 2027", In("single", "CalendarTitle").Single().Text());
        Assert.Equal(new DateOnly(2026, 3, 18), h.Component<CalendarProbe>().Value);
    }

    [Fact]
    public void Week_numbers_follow_the_culture_rule()
    {
        h.Render<CalendarProbe>();
        Refresh(p => p.WeekNumbers = true);
        var numbers = In("single", "CalendarWeekNumber").Select(e => e.Text()).ToList();
        Assert.Equal(6, numbers.Count);
        Assert.Equal("9", numbers[0]);
        Assert.Equal(true, h.ByClass("single").Attributes["week-numbers"]);
        Assert.Single(In("single", "CalendarWeekHeader"));
    }

    [Fact]
    public void A_range_is_chosen_with_two_clicks_across_two_months_and_previews_while_choosing()
    {
        h.Render<CalendarProbe>();
        var probe = h.Component<CalendarProbe>();
        Assert.Equal(2, In("range", "CalendarMonth").Count());
        Assert.Equal("Marzo 2026 – Aprile 2026", In("range", "CalendarTitle").Single().Text());
        Assert.All(In("range", "CalendarDay").Where(d => d.Attributes.GetValueOrDefault("outside") is true), d => Assert.Equal(true, d.Attributes["empty"]));
        h.Click(Day("range", 28, 3));
        Assert.Equal(new DateOnly(2026, 3, 28), probe.Start);
        Assert.Null(probe.End);
        h.Focus(Day("range", 2, 4));
        Assert.Equal(true, Day("range", 30, 3).Attributes["range-preview"]);
        h.Click(Day("range", 3, 4));
        Assert.Equal(new DateOnly(2026, 4, 3), probe.End);
        Assert.Equal(true, Day("range", 28, 3).Attributes["range-start"]);
        Assert.Equal(true, Day("range", 31, 3).Attributes["range-middle"]);
        Assert.Equal(true, Day("range", 3, 4).Attributes["range-end"]);
        Assert.Equal(true, Day("range", 1, 4).Attributes["selected"]);
        Assert.Equal(false, Day("range", 4, 4).Attributes["selected"]);
        h.Click(Day("range", 10, 4));
        Assert.Equal(new DateOnly(2026, 4, 10), probe.Start);
        Assert.Null(probe.End);
        h.Click(Day("range", 5, 4));
        Assert.Equal(new DateOnly(2026, 4, 5), probe.Start);
        Assert.Equal(new DateOnly(2026, 4, 10), probe.End);
    }

    [Fact]
    public void Keyboard_movement_in_two_months_shifts_the_view_by_one_month()
    {
        h.Render<CalendarProbe>();
        h.Focus(Day("range", 10, 3));
        h.Press("PageDown");
        Assert.Equal("Marzo 2026 – Aprile 2026", In("range", "CalendarTitle").Single().Text());
        h.Press("PageDown");
        Assert.Equal("Aprile 2026 – Maggio 2026", In("range", "CalendarTitle").Single().Text());
        Assert.Equal("10", Current("range").Text());
    }

    [Fact]
    public void Multiple_selection_toggles_dates()
    {
        h.Render<CalendarProbe>();
        var probe = h.Component<CalendarProbe>();
        h.Click(Day("multiple", 4));
        h.Click(Day("multiple", 2));
        Assert.Equal([new DateOnly(2026, 3, 2), new DateOnly(2026, 3, 4)], probe.Dates);
        Assert.Equal(true, In("multiple", "CalendarGrid").Single().Attributes["multiselectable"]);
        h.Click(Day("multiple", 4));
        Assert.Equal([new DateOnly(2026, 3, 2)], probe.Dates);
    }

    [Fact]
    public void External_value_changes_show_their_month()
    {
        h.Render<CalendarProbe>();
        Refresh(p => p.Value = new DateOnly(2027, 7, 4));
        Assert.Equal("Luglio 2027", In("single", "CalendarTitle").Single().Text());
        Assert.Equal(true, Day("single", 4).Attributes["selected"]);
    }

    [Fact]
    public void Basic_marks_the_selected_day_with_the_selection_color()
    {
        h.Render<CalendarProbe>();
        var selected = h.Style(Day("single", 18));
        var plain = h.Style(Day("single", 17));
        Assert.NotEqual(plain.BackgroundColor, selected.BackgroundColor);
        Assert.True(h.Bounds(Day("single", 18)).Width >= 20);
        Assert.Equal(h.Bounds(Day("single", 18)).Y, h.Bounds(Day("single", 17)).Y, 1);
    }

    [Fact]
    public void The_model_moves_and_clamps_dates()
    {
        var model = new CalendarModel { Culture = CultureInfo.GetCultureInfo("it-IT"), Maximum = new DateOnly(2026, 2, 10) };
        Assert.Equal(DayOfWeek.Monday, model.WeekStart);
        Assert.Equal(new DateOnly(2026, 2, 10), model.Move(new DateOnly(2026, 1, 31), CalendarStep.Month, 1));
        Assert.Equal(new DateOnly(2026, 2, 3), model.Move(new DateOnly(2026, 1, 27), CalendarStep.Week, 1));
    }
}
