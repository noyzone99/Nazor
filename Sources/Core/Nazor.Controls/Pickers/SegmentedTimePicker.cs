using Nazor.Rendering;

namespace Nazor.Controls;

/// <summary>
/// Selects a nullable time through localized hour/minute and optional second/period segments. The culture determines
/// 12/24-hour display unless <see cref="Use24Hour" /> overrides it. Column edits change a draft until Confirm;
/// Cancel, Escape and outside dismissal discard the draft. Hidden seconds and an existing off-increment minute are
/// preserved. All drafts remain within the configured inclusive time bounds; ranges crossing midnight are not supported.
/// The popup is <c>SegmentedTimePickerPopup</c> and it shares the segmented-picker parts with <see cref="SegmentedDatePicker" />.
/// </summary>
public sealed class SegmentedTimePicker : SegmentedPickerBase<TimeOnly>
{
    private string[] order = ["hours", "minutes"];

    /// <summary>Gets or sets whether the display uses a 24-hour clock; null follows the culture.</summary>
    [Parameter] public bool? Use24Hour { get; set; }
    /// <summary>Gets or sets the minute grid offered in the popup, from 1 to 60; an existing off-grid minute remains selectable.</summary>
    [Parameter] public int MinuteIncrement { get; set; } = 1;
    /// <summary>Gets or sets whether seconds are displayed and edited; hidden seconds retain their committed value.</summary>
    [Parameter] public bool ShowSeconds { get; set; }
    /// <summary>Gets or sets the earliest allowed time, or null for midnight.</summary>
    [Parameter] public TimeOnly? Minimum { get; set; }
    /// <summary>Gets or sets the latest allowed time, or null for the end of the day.</summary>
    [Parameter] public TimeOnly? Maximum { get; set; }
    /// <summary>Gets whether the active display uses a 24-hour clock.</summary>
    public bool Is24Hour => Use24Hour ?? TimeText.Uses24Hour(EffectiveCulture);
    /// <summary>Gets the NSS element name emitted by SegmentedTimePicker.</summary>
    protected override string ElementName => "SegmentedTimePicker";
    /// <summary>Gets the localized popup name.</summary>
    protected override string PickerLabel => ControlStrings.Get("ChooseTime");
    /// <summary>Gets visible time parts in culture order.</summary>
    protected override IReadOnlyList<string> SegmentNames => order;
    /// <summary>Gets the value-equality configuration key used to cancel stale drafts.</summary>
    protected override object ConfigurationKey => (Minimum, Maximum, Use24Hour, MinuteIncrement, ShowSeconds,
        EffectiveCulture.Name, EffectiveCulture.DateTimeFormat.ShortTimePattern);

    /// <summary>Validates clock increments and inclusive time bounds.</summary>
    protected override void ValidateConfiguration()
    {
        if (MinuteIncrement is < 1 or > 60) throw new ArgumentOutOfRangeException(nameof(MinuteIncrement));
        if (Minimum is { } min && Maximum is { } max && min > max) throw new ArgumentOutOfRangeException(nameof(Maximum));
        var names = PickerPartOrder.Read(EffectiveCulture.DateTimeFormat.ShortTimePattern,
            character => character switch { 'h' or 'H' => "hours", 'm' => "minutes", 's' => "seconds", 't' => "period", _ => null }, ["hours", "minutes"])
            .Where(name => (name != "period" || !Is24Hour) && (name != "seconds" || ShowSeconds)).ToList();
        if (ShowSeconds && !names.Contains("seconds")) names.Insert(names.IndexOf("minutes") + 1, "seconds");
        if (!Is24Hour && !names.Contains("period")) names.Add("period");
        order = [.. names];
    }

    /// <summary>Creates an initial draft from the committed time or the clock rounded down to the minute increment.</summary>
    /// <returns>The bounded draft time.</returns>
    protected override TimeOnly CreateInitialDraft()
    {
        var now = TimeOnly.FromDateTime(DateTime.Now);
        return Clamp(CurrentValue ?? new TimeOnly(now.Hour, now.Minute / MinuteIncrement * MinuteIncrement, 0));
    }
    /// <summary>Tests the configured inclusive time bounds.</summary>
    /// <param name="candidate">The time to inspect.</param>
    /// <returns>true when it is within bounds.</returns>
    protected override bool IsAllowed(TimeOnly candidate) => (Minimum is not { } min || candidate >= min) && (Maximum is not { } max || candidate <= max);

    private TimeOnly Clamp(TimeOnly time) => Minimum is { } min && time < min ? min : Maximum is { } max && time > max ? max : time;
    private bool AnyAllowed(TimeOnly first, TimeOnly last) => (Minimum is not { } min || last >= min) && (Maximum is not { } max || first <= max);
    private static TimeOnly WithParts(TimeOnly time, int? hour = null, int? minute = null, int? second = null) =>
        new(new TimeOnly(hour ?? time.Hour, minute ?? time.Minute, second ?? time.Second).Ticks + time.Ticks % TimeSpan.TicksPerSecond);
    private static TimeOnly End(int hour, int minute) => new(new TimeOnly(hour, minute, 59).Ticks + TimeSpan.TicksPerSecond - 1);
    private int ToHour(int value, TimeOnly time) => Is24Hour ? value : value % 12 + (time.Hour >= 12 ? 12 : 0);
    private int ShownHour(TimeOnly time) => Is24Hour ? time.Hour : time.Hour % 12 == 0 ? 12 : time.Hour % 12;
    private string Label(string name) => ControlStrings.Get(name switch { "hours" => "Hours", "minutes" => "Minutes", "seconds" => "Seconds", _ => "AmPm" });
    private string Text(TimeOnly time, string name)
    {
        var (am, pm) = TimeText.Designators(EffectiveCulture);
        return name switch
        {
            "hours" => ShownHour(time).ToString(Is24Hour ? "00" : "0", EffectiveCulture),
            "minutes" => time.Minute.ToString("00", EffectiveCulture),
            "seconds" => time.Second.ToString("00", EffectiveCulture),
            _ => time.Hour < 12 ? am : pm,
        };
    }

    /// <summary>Renders time entry segments with placeholders while unset.</summary>
    /// <param name="builder">The builder receiving the segments.</param>
    protected override void RenderSegments(RenderTreeBuilder builder)
    {
        foreach (var name in order) Segment(builder, 0, name, Label(name), CurrentValue is { } time ? Text(time, name) : null);
    }

    /// <summary>Renders bounded time columns without changing the committed value.</summary>
    /// <param name="builder">The builder receiving the columns.</param>
    protected override void RenderColumns(RenderTreeBuilder builder)
    {
        var time = Draft;
        var (am, pm) = TimeText.Designators(EffectiveCulture);
        foreach (var name in order)
        {
            if (name == "hours")
            {
                var hours = (Is24Hour ? Enumerable.Range(0, 24) : Enumerable.Range(1, 12))
                    .Where(hour => AnyAllowed(new TimeOnly(ToHour(hour, time), 0), End(ToHour(hour, time), 59))).ToArray();
                Column(builder, 0, name, Label(name), ShownHour(time), hours,
                    hour => hour.ToString(Is24Hour ? "00" : "0", EffectiveCulture), hour => Clamp(WithParts(time, hour: ToHour(hour, time))));
            }
            else if (name == "minutes")
            {
                var minutes = Enumerable.Range(0, 60).Where(minute => minute % MinuteIncrement == 0 || minute == time.Minute)
                    .Where(minute => AnyAllowed(new TimeOnly(time.Hour, minute, 0), End(time.Hour, minute))).ToArray();
                Column(builder, 0, name, Label(name), time.Minute, minutes, minute => minute.ToString("00", EffectiveCulture),
                    minute => Clamp(WithParts(time, minute: minute)));
            }
            else if (name == "seconds")
            {
                var seconds = Enumerable.Range(0, 60).Where(second => IsAllowed(WithParts(time, second: second))).ToArray();
                Column(builder, 0, name, Label(name), time.Second, seconds, second => second.ToString("00", EffectiveCulture), second => WithParts(time, second: second));
            }
            else
            {
                var periods = new[] { 0, 1 }.Where(period => AnyAllowed(new TimeOnly(period * 12, 0), End(period * 12 + 11, 59))).ToArray();
                Column(builder, 0, name, Label(name), time.Hour < 12 ? 0 : 1, periods, period => period == 0 ? am : pm,
                    period => Clamp(WithParts(time, hour: time.Hour % 12 + period * 12)));
            }
        }
    }
}
