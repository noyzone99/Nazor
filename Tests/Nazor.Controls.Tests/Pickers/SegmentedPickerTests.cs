using System.Globalization;
using Nazor.Controls.Tests.Pickers.Fixtures;
using Nazor.Graphics;
using Nazor.Input;

namespace Nazor.Controls.Tests.Pickers;

[Collection("Controls")]
public sealed class SegmentedPickerTests : IDisposable
{
    private readonly ControlHarness h = new(width: 900, height: 900);
    public void Dispose() => h.Dispose();

    private SegmentedPickerProbe Render(Action<SegmentedPickerProbe>? setup = null)
    {
        h.Render<SegmentedPickerProbe>();
        var probe = h.Component<SegmentedPickerProbe>();
        if (setup is not null) { setup(probe); probe.Refresh(); h.Frame(); }
        return probe;
    }

    private ElementNode Column(string name) => h.All("SegmentedPickerColumn").Single(column => Equals(column.Attributes.GetValueOrDefault("column"), name));
    private ElementNode Selected(string name) => Column(name).Elements().Single(e => e.Name == "SegmentedPickerOption" && e.Attributes.GetValueOrDefault("current") is true);
    private void FocusColumn(string name) => h.Focus(Selected(name));

    [Theory]
    [InlineData("en-US", "month", "day", "year")]
    [InlineData("it-IT", "day", "month", "year")]
    [InlineData("ja-JP", "year", "month", "day")]
    public void Empty_date_segments_follow_culture_order_and_show_placeholders(string culture, string first, string second, string third)
    {
        Render(p => { p.Date = null; p.Culture = CultureInfo.GetCultureInfo(culture); });
        var segments = h.ByClass("seg-date").Elements().Where(e => e.Name == "SegmentedPickerSegment").ToArray();
        Assert.Equal(new[] { first, second, third }, segments.Select(e => (string)e.Attributes["segment"]!));
        Assert.All(segments, segment => Assert.Equal(true, segment.Attributes["placeholder"]));
    }

    [Fact]
    public void Date_column_changes_are_drafts_and_confirmation_commits_a_valid_leap_day_once()
    {
        var probe = Render();
        h.Focus(h.ByClass("seg-date"));
        h.Press("F4");
        FocusColumn("month");
        h.Press("ArrowDown");
        Assert.Equal(new DateOnly(2024, 2, 29), probe.DateControl.DraftValue);
        Assert.Equal(new DateOnly(2024, 1, 31), probe.Date);
        Assert.Empty(probe.DateChanges);
        h.Press("Enter");
        Assert.Equal(new DateOnly(2024, 2, 29), probe.Date);
        Assert.Equal([new DateOnly(2024, 2, 29)], probe.DateChanges);
        Assert.False(probe.DateOpen);
        Assert.Empty(h.All("SegmentedDatePickerPopup"));
        Assert.Same(h.ByClass("seg-date"), h.Focused);
    }

    [Fact]
    public void Escape_and_outside_dismissal_discard_date_drafts()
    {
        var probe = Render();
        h.Focus(h.ByClass("seg-date"));
        h.Press("F4");
        FocusColumn("month");
        h.Press("ArrowDown");
        h.Press("Escape");
        Assert.Equal(new DateOnly(2024, 1, 31), probe.Date);
        Assert.False(probe.DateOpen);
        h.Press("F4");
        Assert.Equal(probe.Date, probe.DateControl.DraftValue);
        FocusColumn("day");
        h.Press("ArrowUp");
        h.Click(h.ByClass("outside"));
        Assert.Equal(new DateOnly(2024, 1, 31), probe.Date);
        Assert.Empty(probe.DateChanges);
        Assert.False(probe.DateOpen);
    }

    [Fact]
    public void External_values_and_constraints_cancel_stale_edits_without_committing_them()
    {
        var probe = Render();
        h.Focus(h.ByClass("seg-date")); h.Press("F4");
        probe.Date = new DateOnly(2025, 6, 1);
        probe.Refresh(); h.Frame();
        Assert.False(probe.DateControl.Opened);
        Assert.False(probe.DateOpen);
        Assert.Empty(probe.DateChanges);
        h.Focus(h.ByClass("seg-date")); h.Press("F4");
        probe.MaximumDate = new DateOnly(2025, 6, 5);
        probe.Refresh(); h.Frame();
        Assert.False(probe.DateControl.Opened);
        Assert.Empty(probe.DateChanges);
    }

    [Fact]
    public void Large_year_ranges_realize_only_a_bounded_window_and_support_home_and_end()
    {
        var probe = Render();
        h.Focus(h.ByClass("seg-date")); h.Press("F4");
        Assert.InRange(h.All("SegmentedPickerOption").Count(), 1, 27);
        Assert.All(h.All("SegmentedPickerColumn"), column => Assert.Equal(9,
            column.Elements().Count(element => element.Name is "SegmentedPickerOption" or "SegmentedPickerSpacer")));
        FocusColumn("year");
        h.Press("Home");
        Assert.Equal(1, probe.DateControl.DraftValue?.Year);
        Assert.InRange(h.All("SegmentedPickerOption").Count(), 1, 27);
        h.Press("End");
        Assert.Equal(9999, probe.DateControl.DraftValue?.Year);
        Assert.InRange(h.All("SegmentedPickerOption").Count(), 1, 27);
        Assert.Equal(9999, Selected("year").Attributes["size-of-set"]);
    }

    [Fact]
    public void Hidden_date_segments_preserve_their_value_and_custom_day_formats_are_rendered()
    {
        var probe = Render(p => { p.YearVisible = false; p.DayFormat = "dd (ddd)"; });
        var segments = h.ByClass("seg-date").Elements().Where(e => e.Name == "SegmentedPickerSegment").ToArray();
        Assert.Equal(2, segments.Length);
        Assert.Contains(segments, e => e.Text() == "31 (Wed)");
        h.Focus(h.ByClass("seg-date")); h.Press("F4");
        Assert.DoesNotContain(h.All("SegmentedPickerColumn"), c => Equals(c.Attributes.GetValueOrDefault("column"), "year"));
        FocusColumn("month"); h.Press("ArrowDown"); h.Press("Enter");
        Assert.Equal(new DateOnly(2024, 2, 29), probe.Date);
    }

    [Fact]
    public void Column_capture_transfer_discards_the_old_gesture_before_recapture_and_stale_move_or_release()
    {
        var probe = Render();
        h.Focus(h.ByClass("seg-time")); h.Press("F4");
        var column = Column("minutes");
        var bounds = h.Bounds(Selected("minutes"));
        var start = new Nazor.Graphics.Point(bounds.X + bounds.Width / 2, bounds.Y + bounds.Height / 2);
        h.Input.PointerPressed(start, PointerButton.Primary, KeyModifiers.None); h.Frame();
        h.Input.PointerMoved(new(start.X, start.Y - 40), KeyModifiers.None); h.Frame();
        var draft = probe.TimeControl.DraftValue;
        Assert.Equal(new TimeOnly(9, 45), draft);
        Assert.True(new ElementReference(h.ByClass("outside")).CapturePointer());
        Assert.True(new ElementReference(column).CapturePointer());
        h.Input.PointerMoved(new(start.X, start.Y + 80), KeyModifiers.None); h.Frame();
        Assert.Equal(draft, probe.TimeControl.DraftValue);
        h.Input.PointerReleased(new(start.X, start.Y + 80), PointerButton.Primary, KeyModifiers.None); h.Frame();
        Assert.Equal(draft, probe.TimeControl.DraftValue);
        Assert.Equal(new TimeOnly(9, 30), probe.Time);
        Assert.Empty(probe.TimeChanges);
    }

    [Fact]
    public void Date_bounds_restrict_column_values_and_initial_unset_fallback()
    {
        var probe = Render(p => { p.Date = null; p.MinimumDate = new DateOnly(2026, 11, 15); p.MaximumDate = new DateOnly(2026, 11, 20); });
        h.Focus(h.ByClass("seg-date")); h.Press("F4");
        Assert.Equal(new DateOnly(2026, 11, 15), probe.DateControl.DraftValue);
        FocusColumn("day"); h.Press("End");
        Assert.Equal(new DateOnly(2026, 11, 20), probe.DateControl.DraftValue);
        h.Press("Enter");
        Assert.Equal(new DateOnly(2026, 11, 20), probe.Date);
    }

    [Fact]
    public void Changing_a_leap_year_clamps_the_day_and_left_right_move_between_columns()
    {
        var probe = Render(p => p.Date = new DateOnly(2024, 2, 29));
        h.Focus(h.ByClass("seg-date")); h.Press("F4");
        FocusColumn("year"); h.Press("ArrowDown");
        Assert.Equal(new DateOnly(2025, 2, 28), probe.DateControl.DraftValue);
        h.Press("ArrowLeft");
        Assert.True(Column("day").States.HasFlag(ElementStates.FocusWithin));
        h.Press("ArrowRight");
        Assert.True(Column("year").States.HasFlag(ElementStates.FocusWithin));
        h.Press("Escape");
        Assert.Equal(new DateOnly(2024, 2, 29), probe.Date);
    }

    [Fact]
    public void Time_columns_preserve_the_value_until_confirm_and_honor_minute_grid()
    {
        var probe = Render();
        h.Focus(h.ByClass("seg-time")); h.Press("F4");
        FocusColumn("minutes"); h.Press("ArrowDown");
        Assert.Equal(new TimeOnly(9, 45), probe.TimeControl.DraftValue);
        Assert.Equal(new TimeOnly(9, 30), probe.Time);
        Assert.Empty(probe.TimeChanges);
        h.Press("Enter");
        Assert.Equal(new TimeOnly(9, 45), probe.Time);
        Assert.Equal([new TimeOnly(9, 45)], probe.TimeChanges);
        Assert.False(probe.TimeOpen);
    }

    [Fact]
    public void Twelve_hour_period_switches_midnight_and_noon_correctly()
    {
        var probe = Render(p => { p.Time = new TimeOnly(0, 0); p.Use24 = false; });
        h.Focus(h.ByClass("seg-time")); h.Press("F4");
        Assert.Equal(12, Selected("hours").Attributes["picker-value"]);
        FocusColumn("period"); h.Press("End");
        Assert.Equal(new TimeOnly(12, 0), probe.TimeControl.DraftValue);
        h.Press("Home");
        Assert.Equal(new TimeOnly(0, 0), probe.TimeControl.DraftValue);
        h.Press("End"); h.Press("Enter");
        Assert.Equal(new TimeOnly(12, 0), probe.Time);
    }

    [Theory]
    [InlineData("en-US", false)]
    [InlineData("it-IT", true)]
    public void Clock_defaults_follow_the_culture_and_seconds_are_editable_when_shown(string culture, bool clock24)
    {
        var probe = Render(p => { p.Culture = CultureInfo.GetCultureInfo(culture); p.Use24 = null; p.ShowSeconds = true; });
        Assert.Equal(clock24, probe.TimeControl.Is24Hour);
        h.Focus(h.ByClass("seg-time")); h.Press("F4");
        Assert.Equal(clock24 ? 3 : 4, h.All("SegmentedPickerColumn").Count());
        FocusColumn("seconds"); h.Press("ArrowDown");
        Assert.Equal(1, probe.TimeControl.DraftValue?.Second);
        h.Press("Enter");
        Assert.Equal(1, probe.Time?.Second);
    }

    [Fact]
    public void Time_bounds_and_fractional_seconds_survive_edits_without_data_loss()
    {
        var original = new TimeOnly(new TimeOnly(9, 30, 12).Ticks + 5000);
        var probe = Render(p => { p.Time = original; p.MinimumTime = new TimeOnly(9, 15); p.MaximumTime = new TimeOnly(10, 30); });
        h.Focus(h.ByClass("seg-time")); h.Press("F4");
        FocusColumn("minutes"); h.Press("Home");
        Assert.Equal(new TimeOnly(new TimeOnly(9, 15, 12).Ticks + 5000), probe.TimeControl.DraftValue);
        FocusColumn("hours"); h.Press("End");
        Assert.Equal(10, probe.TimeControl.DraftValue?.Hour);
        Assert.Equal(12, probe.TimeControl.DraftValue?.Second);
        h.Press("Enter");
        Assert.True(probe.Time >= probe.MinimumTime && probe.Time <= probe.MaximumTime);
        Assert.Equal(5000, probe.Time!.Value.Ticks % TimeSpan.TicksPerSecond);
    }

    [Fact]
    public void Fractional_wheel_input_accumulates_and_cancel_action_enter_does_not_confirm()
    {
        var probe = Render();
        h.Focus(h.ByClass("seg-time")); h.Press("F4");
        var bounds = h.Bounds(Selected("minutes"));
        var point = new Point(bounds.X + bounds.Width / 2, bounds.Y + bounds.Height / 2);
        h.Input.PointerWheel(point, 0, -20, KeyModifiers.None, ScrollDeltaUnit.LogicalPixels); h.Frame();
        Assert.Equal(new TimeOnly(9, 30), probe.TimeControl.DraftValue);
        h.Input.PointerWheel(point, 0, -20, KeyModifiers.None, ScrollDeltaUnit.LogicalPixels); h.Frame();
        Assert.Equal(new TimeOnly(9, 45), probe.TimeControl.DraftValue);
        h.Focus(h.One("SegmentedPickerCancel")); h.Press("Enter");
        Assert.Equal(new TimeOnly(9, 30), probe.Time);
        Assert.Empty(probe.TimeChanges);
        Assert.False(probe.TimeOpen);
    }

    [Fact]
    public void Read_only_and_disabled_pickers_do_not_open()
    {
        var probe = Render(p => p.ReadOnly = true);
        h.Focus(h.ByClass("seg-date")); h.Press("F4");
        Assert.False(probe.DateOpen);
        probe.ReadOnly = false; probe.Disabled = true; probe.Refresh(); h.Frame();
        h.Click(h.ByClass("seg-time"));
        Assert.False(probe.TimeOpen);
        Assert.Empty(h.All("SegmentedTimePickerPopup"));
    }
}
