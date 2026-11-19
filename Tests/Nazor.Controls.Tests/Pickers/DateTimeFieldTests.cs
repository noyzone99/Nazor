using System.Globalization;
using Nazor.Controls.Tests.Pickers.Fixtures;
using Nazor.Input;

namespace Nazor.Controls.Tests.Pickers;

[Collection("Controls")]
public sealed class DateTimeFieldTests : IDisposable
{
    private static readonly CultureInfo Italian = CultureInfo.GetCultureInfo("it-IT");
    private static readonly CultureInfo English = CultureInfo.GetCultureInfo("en-US");
    private readonly ControlHarness h = new(width: 900, height: 900);

    public void Dispose() => h.Dispose();

    private ElementNode Part(string className, string name) => h.ByClass(className).Elements().First(e => e.Name == name);

    private DatePickerProbe Probe => h.Component<DatePickerProbe>();

    private void Refresh(Action<DatePickerProbe> change)
    {
        var probe = Probe;
        h.Host.Renderer.Dispatcher.InvokeAsync(() => { change(probe); probe.Refresh(); });
        h.Frame();
    }

    private void TypeInto(ElementNode input, string text)
    {
        h.Focus(input);
        h.Press("a", KeyModifiers.Control);
        h.Type(text);
    }

    [Fact]
    public void Typing_a_date_in_the_culture_updates_the_value_and_reformats_on_enter()
    {
        h.Render<DatePickerProbe>();
        var input = Part("date", "DatePickerInput");
        Assert.Equal("dd/MM/yyyy", input.Attributes["placeholder-text"]);
        Assert.Equal("combobox", input.Attributes["role"]);
        TypeInto(input, "5/3/26");
        Assert.Equal(new DateOnly(2026, 3, 5), Probe.Date);
        Assert.Equal(false, h.ByClass("date").Attributes["invalid"]);
        h.Press("Enter");
        Assert.Equal("05/03/2026", Part("date", "DatePickerInput").Attributes["value"]);
    }

    [Fact]
    public void An_unparsable_or_out_of_range_date_is_invalid_and_keeps_the_value()
    {
        h.Render<DatePickerProbe>();
        TypeInto(Part("date", "DatePickerInput"), "12/04/2026");
        Assert.Equal(new DateOnly(2026, 4, 12), Probe.Date);
        TypeInto(Part("date", "DatePickerInput"), "31/02/2026");
        Assert.Equal(true, h.ByClass("date").Attributes["invalid"]);
        Assert.Equal(true, Part("date", "DatePickerInput").Attributes["invalid"]);
        Assert.Equal(new DateOnly(2026, 4, 12), Probe.Date);
        TypeInto(Part("date", "DatePickerInput"), "01/01/2027");
        Assert.Equal(true, h.ByClass("date").Attributes["invalid"]);
        Assert.Equal(new DateOnly(2026, 4, 12), Probe.Date);
        TypeInto(Part("date", "DatePickerInput"), "");
        Assert.Null(Probe.Date);
        Assert.Equal(false, h.ByClass("date").Attributes["invalid"]);
    }

    [Fact]
    public void Us_dates_are_month_first()
    {
        h.Render<DatePickerProbe>();
        Refresh(p => { p.Culture = English; p.Date = new DateOnly(2026, 3, 5); });
        Assert.Equal("3/5/2026", Part("date", "DatePickerInput").Attributes["value"]);
        TypeInto(Part("date", "DatePickerInput"), "4/12/2026");
        Assert.Equal(new DateOnly(2026, 4, 12), Probe.Date);
        TypeInto(Part("date", "DatePickerInput"), "December 24, 2026");
        Assert.Equal(new DateOnly(2026, 12, 24), Probe.Date);
    }

    [Fact]
    public void Alt_down_opens_the_calendar_on_the_value_and_choosing_a_day_closes_it()
    {
        h.Render<DatePickerProbe>();
        Refresh(p => p.Date = new DateOnly(2026, 6, 15));
        h.Focus(Part("date", "DatePickerInput"));
        h.Press("ArrowDown", KeyModifiers.Alt);
        Assert.Single(h.All("DatePickerPopup"));
        Assert.Equal(true, h.ByClass("date").Attributes["expanded"]);
        Assert.Equal(true, Part("date", "DatePickerInput").Attributes["expanded"]);
        Assert.Equal("Giugno 2026", h.One("CalendarTitle").Text());
        Assert.Equal("15", h.Focused?.Text());
        h.Press("ArrowRight");
        h.Press("Enter");
        Assert.Equal(new DateOnly(2026, 6, 16), Probe.Date);
        Assert.Empty(h.All("DatePickerPopup"));
        Assert.Equal("16/06/2026", Part("date", "DatePickerInput").Attributes["value"]);
        Assert.Same(Part("date", "DatePickerInput"), h.Focused);
    }

    [Fact]
    public void The_button_toggles_the_popup_and_escape_or_outside_close_it()
    {
        h.Render<DatePickerProbe>();
        h.Click(Part("date", "DatePickerButton"));
        Assert.Single(h.All("DatePickerPopup"));
        Assert.Equal("dialog", h.One("DatePickerPopup").Attributes["role"]);
        h.Press("Escape");
        Assert.Empty(h.All("DatePickerPopup"));
        h.Click(Part("date", "DatePickerButton"));
        h.Click(h.ByClass("outside"));
        Assert.Empty(h.All("DatePickerPopup"));
    }

    [Fact]
    public void The_today_button_and_the_clear_button_change_the_value()
    {
        h.Render<DatePickerProbe>();
        h.Click(Part("date", "DatePickerButton"));
        h.Click(h.One("DatePickerToday"));
        Assert.Equal(new DateOnly(2026, 3, 10), Probe.Date);
        Assert.Empty(h.All("DatePickerPopup"));
        h.Click(Part("date", "DatePickerClear"));
        Assert.Null(Probe.Date);
    }

    [Fact]
    public void The_date_editor_is_named_by_its_field()
    {
        h.Render<DatePickerProbe>();
        var label = h.All("Label").First();
        Assert.Equal(label.Id, Part("date", "DatePickerInput").Attributes["labeled-by"]);
    }

    [Fact]
    public void Typed_times_follow_the_culture_clock()
    {
        Assert.True(TimeText.TryParse("14:30", Italian, out var time));
        Assert.Equal(new TimeOnly(14, 30), time);
        Assert.True(TimeText.TryParse("930", Italian, out time));
        Assert.Equal(new TimeOnly(9, 30), time);
        Assert.True(TimeText.TryParse("2:15 pm", English, out time));
        Assert.Equal(new TimeOnly(14, 15), time);
        Assert.True(TimeText.TryParse("12 AM", English, out time));
        Assert.Equal(new TimeOnly(0, 0), time);
        Assert.True(TimeText.TryParse("7.05", Italian, out time));
        Assert.Equal(new TimeOnly(7, 5), time);
        Assert.False(TimeText.TryParse("25:00", Italian, out _));
        Assert.False(TimeText.TryParse("13 PM", English, out _));
        Assert.False(TimeText.TryParse("abc", Italian, out _));
        Assert.True(TimeText.Uses24Hour(Italian));
        Assert.False(TimeText.Uses24Hour(English));
        Assert.Equal("2:05 PM", TimeText.Format(new TimeOnly(14, 5), English, false, false));
        Assert.Equal("14:05:09", TimeText.Format(new TimeOnly(14, 5, 9), Italian, true, true));
    }

    [Fact]
    public void Typing_a_time_updates_the_value_and_marks_out_of_range_times()
    {
        h.Render<DatePickerProbe>();
        Refresh(p => p.MinTime = new TimeOnly(8, 0));
        TypeInto(Part("time", "TimePickerInput"), "9:45");
        Assert.Equal(new TimeOnly(9, 45), Probe.Time);
        h.Press("Enter");
        Assert.Equal("09:45", Part("time", "TimePickerInput").Attributes["value"]);
        TypeInto(Part("time", "TimePickerInput"), "7:00");
        Assert.Equal(true, h.ByClass("time").Attributes["invalid"]);
        Assert.Equal(new TimeOnly(9, 45), Probe.Time);
    }

    [Fact]
    public void Time_columns_change_the_value_as_the_arrows_move_and_disable_early_hours()
    {
        h.Render<DatePickerProbe>();
        Refresh(p => { p.Time = new TimeOnly(9, 15); p.MinTime = new TimeOnly(8, 0); });
        h.Focus(Part("time", "TimePickerInput"));
        h.Press("F4");
        var columns = h.All("TimePickerColumn").ToList();
        Assert.Equal(2, columns.Count);
        Assert.Equal("hours", columns[0].Attributes["column"]);
        Assert.Equal("Hours", columns[0].Attributes["accessible-name"]);
        Assert.Equal(new[] { "00", "15", "30", "45" }, columns[1].Elements().Where(e => e.Name == "TimePickerOption").Select(e => e.Text()));
        Assert.True(columns[0].Elements().First(e => e.Name == "TimePickerOption" && e.Text() == "07").IsDisabled);
        Assert.Equal("09", h.Focused?.Text());
        h.Press("ArrowDown");
        Assert.Equal(new TimeOnly(10, 15), Probe.Time);
        h.Press("ArrowRight");
        Assert.Equal("15", h.Focused?.Text());
        h.Press("ArrowDown");
        Assert.Equal(new TimeOnly(10, 30), Probe.Time);
        Assert.Single(h.All("TimePickerPopup"));
        h.Press("Enter");
        Assert.Empty(h.All("TimePickerPopup"));
        Assert.Equal("10:30", Part("time", "TimePickerInput").Attributes["value"]);
    }

    [Fact]
    public void A_twelve_hour_picker_has_a_period_column()
    {
        h.Render<DatePickerProbe>();
        Refresh(p => { p.Use24 = false; p.Time = new TimeOnly(15, 0); });
        Assert.Equal("3:00 PM", Part("time", "TimePickerInput").Attributes["value"]);
        h.Click(Part("time", "TimePickerButton"));
        var columns = h.All("TimePickerColumn").ToList();
        Assert.Equal(3, columns.Count);
        Assert.Equal("period", columns[2].Attributes["column"]);
        h.Click(columns[2].Elements().First(e => e.Name == "TimePickerOption" && e.Text() == "AM"));
        Assert.Equal(new TimeOnly(3, 0), Probe.Time);
        h.Focus(h.All("TimePickerColumn").First().Elements().First(e => e.Name == "TimePickerOption" && e.Text() == "3"));
        h.Press("End");
        Assert.Equal(new TimeOnly(0, 0), Probe.Time);
        Assert.Equal("12:00 AM", Part("time", "TimePickerInput").Attributes["value"]);
    }

    [Fact]
    public void A_time_list_chooses_and_closes()
    {
        h.Render<DatePickerProbe>();
        h.Click(Part("time-list", "TimePickerButton"));
        var options = h.One("TimePickerList").Elements().Where(e => e.Name == "TimePickerOption").ToList();
        Assert.Equal(48, options.Count);
        Assert.Same(options[0], h.Focused);
        for (var i = 0; i < 19; i++) h.Press("ArrowDown");
        Assert.Null(Probe.ListTime);
        h.Press("Enter");
        Assert.Equal(new TimeOnly(9, 30), Probe.ListTime);
        Assert.Empty(h.All("TimePickerPopup"));
    }

    [Fact]
    public void A_date_time_picker_combines_both_parts()
    {
        h.Render<DatePickerProbe>();
        var moment = h.ByClass("moment");
        var date = moment.Elements().First(e => e.Name == "DatePickerInput");
        var time = moment.Elements().First(e => e.Name == "TimePickerInput");
        Assert.Equal("Meeting, Time", time.Attributes["accessible-name"]);
        TypeInto(time, "18:30");
        Assert.Null(Probe.Moment);
        TypeInto(date, "20/05/2026");
        Assert.Equal(new DateTime(2026, 5, 20, 18, 30, 0), Probe.Moment);
        TypeInto(time, "07:15");
        Assert.Equal(new DateTime(2026, 5, 20, 7, 15, 0), Probe.Moment);
        TypeInto(date, "");
        Assert.Null(Probe.Moment);
        Assert.Equal("plain", moment.Elements().First(e => e.Name == "DatePicker").Attributes["appearance"]);
    }
}
