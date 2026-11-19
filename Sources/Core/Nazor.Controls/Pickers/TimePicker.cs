using System.Globalization;
using System.Text.RegularExpressions;
using Nazor.Controls.Primitives;
using Nazor.Input;
using Nazor.Rendering;

namespace Nazor.Controls;

/// <summary>Formatting and lenient parsing of times typed in a culture, shared by the time pickers.</summary>
public static partial class TimeText
{
    /// <summary>Tells whether a culture writes times with a 24-hour clock.</summary>
    /// <param name="culture">The culture.</param>
    /// <returns>true for a 24-hour clock, false for a 12-hour clock with a period designator.</returns>
    public static bool Uses24Hour(CultureInfo culture)
    {
        ArgumentNullException.ThrowIfNull(culture);
        return !culture.DateTimeFormat.ShortTimePattern.Contains('h');
    }

    /// <summary>Gets the designators of the two halves of the day, falling back to AM and PM when the culture has none.</summary>
    /// <param name="culture">The culture.</param>
    /// <returns>The morning and afternoon designators.</returns>
    public static (string Am, string Pm) Designators(CultureInfo culture)
    {
        ArgumentNullException.ThrowIfNull(culture);
        var info = culture.DateTimeFormat;
        return (string.IsNullOrWhiteSpace(info.AMDesignator) ? "AM" : info.AMDesignator, string.IsNullOrWhiteSpace(info.PMDesignator) ? "PM" : info.PMDesignator);
    }

    /// <summary>Formats a time with the culture's separator, on a 24-hour or 12-hour clock.</summary>
    /// <param name="time">The time.</param>
    /// <param name="culture">The culture.</param>
    /// <param name="use24Hour">Whether to use a 24-hour clock.</param>
    /// <param name="seconds">Whether to include the seconds.</param>
    /// <returns>The text, for example <c>14:05</c> or <c>2:05 PM</c>.</returns>
    public static string Format(TimeOnly time, CultureInfo culture, bool use24Hour, bool seconds)
    {
        ArgumentNullException.ThrowIfNull(culture);
        var separator = culture.DateTimeFormat.TimeSeparator;
        var tail = separator + time.Minute.ToString("00", culture) + (seconds ? separator + time.Second.ToString("00", culture) : "");
        if (use24Hour) return time.Hour.ToString("00", culture) + tail;
        var (am, pm) = Designators(culture);
        var hour = time.Hour % 12 == 0 ? 12 : time.Hour % 12;
        return hour.ToString(culture) + tail + " " + (time.Hour < 12 ? am : pm);
    }

    /// <summary>
    /// Parses a time typed by the user: hours with optional minutes and seconds separated by the culture's separator, a
    /// colon or a period (or written together, as <c>930</c> or <c>1430</c>), optionally followed by a period designator
    /// (the culture's, AM, PM, A or P in any case); then the culture's general rules.
    /// </summary>
    /// <param name="text">The text to parse.</param>
    /// <param name="culture">The culture of the text.</param>
    /// <param name="time">The parsed time.</param>
    /// <returns>true if the text is a time.</returns>
    public static bool TryParse(string? text, CultureInfo culture, out TimeOnly time)
    {
        ArgumentNullException.ThrowIfNull(culture);
        time = default;
        if (string.IsNullOrWhiteSpace(text)) return false;
        var trimmed = text.Trim();
        var (am, pm) = Designators(culture);
        var separator = culture.DateTimeFormat.TimeSeparator;
        var normalized = separator is ":" or "." ? trimmed : trimmed.Replace(separator, ":");
        bool? afternoon = null;
        foreach (var (designator, isPm) in new[] { (pm, true), (am, false), ("PM", true), ("AM", false), ("P", true), ("A", false) })
        {
            if (!normalized.EndsWith(designator, StringComparison.CurrentCultureIgnoreCase)) continue;
            afternoon = isPm;
            normalized = normalized[..^designator.Length].TrimEnd(' ', '.');
            break;
        }
        var match = TimePattern().Match(normalized);
        if (match.Success)
        {
            var hour = int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
            var minute = match.Groups[2].Success ? int.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture) : 0;
            var second = match.Groups[3].Success ? int.Parse(match.Groups[3].Value, CultureInfo.InvariantCulture) : 0;
            if (afternoon is { } isPm)
            {
                if (hour is < 1 or > 12) return false;
                hour = hour % 12 + (isPm ? 12 : 0);
            }
            if (hour > 23 || minute > 59 || second > 59) return false;
            time = new TimeOnly(hour, minute, second);
            return true;
        }
        return afternoon is null && TimeOnly.TryParse(trimmed, culture, DateTimeStyles.AllowWhiteSpaces, out time);
    }

    [GeneratedRegex(@"^\s*(\d{1,2})(?:[:.\s]?(\d{2}))?(?:[:.](\d{2}))?\s*$", RegexOptions.CultureInvariant)]
    private static partial Regex TimePattern();
}

/// <summary>How a <see cref="TimePicker" /> lays out its popup, written as the popup's <c>presentation</c> attribute.</summary>
public enum TimePickerPresentation
{
    /// <summary>One scrolling column for the hours, one for the minutes, optionally one for the seconds and one for AM and PM.</summary>
    Columns,
    /// <summary>A single list of times every <see cref="TimePicker.MinuteIncrement" /> minutes.</summary>
    List,
}

/// <summary>
/// A time field: the <c>TimePickerInput</c> editor parses times typed in <see cref="PickerFieldBase{TValue}.Culture" />
/// (24-hour or 12-hour as the culture writes them, or as <see cref="Use24Hour" /> says) and the <c>TimePickerButton</c>
/// opens the <c>TimePickerPopup</c>. <see cref="PickerFieldBase{TValue}.Value" /> (<c>@bind-Value</c>) is the chosen time.
/// With the <see cref="TimePickerPresentation.Columns" /> presentation the popup holds <c>TimePickerColumns</c>, a
/// <c>TimePickerColumn</c> list (with a <c>column</c> attribute: <c>hours</c>, <c>minutes</c>, <c>seconds</c>, <c>period</c>)
/// per part of the time, whose <c>TimePickerOption</c>s change the value as the arrows move; Left and Right move between
/// columns, Enter closes. With <see cref="TimePickerPresentation.List" /> it holds one <c>TimePickerList</c> of
/// <c>TimePickerOption</c>s every <see cref="MinuteIncrement" /> minutes, where Enter, Space or a click choose and close.
/// Times outside <see cref="Minimum" /> and <see cref="Maximum" /> are disabled, and typed ones mark the field invalid.
/// </summary>
public sealed class TimePicker : PickerFieldBase<TimeOnly>
{
    private ElementReference columnsElement;

    /// <summary>Gets or sets whether the clock has 24 hours; null follows the culture.</summary>
    [Parameter] public bool? Use24Hour { get; set; }

    /// <summary>Gets or sets the step of the minutes offered in the popup, from 1 to 60.</summary>
    [Parameter] public int MinuteIncrement { get; set; } = 1;

    /// <summary>Gets or sets whether the seconds are shown and offered.</summary>
    [Parameter] public bool ShowSeconds { get; set; }

    /// <summary>Gets or sets the earliest time that can be chosen, or null for no limit.</summary>
    [Parameter] public TimeOnly? Minimum { get; set; }

    /// <summary>Gets or sets the latest time that can be chosen, or null for no limit.</summary>
    [Parameter] public TimeOnly? Maximum { get; set; }

    /// <summary>Gets or sets how the popup lays out the times.</summary>
    [Parameter] public TimePickerPresentation Presentation { get; set; }

    /// <summary>Gets the NSS element name emitted by TimePicker.</summary>
    protected override string ElementName => "TimePicker";

    /// <summary>Gets the name of the button and of the popup.</summary>
    protected override string ButtonLabel => ControlStrings.Get("ChooseTime");

    /// <summary>Gets whether the clock in use has 24 hours.</summary>
    public bool Is24Hour => Use24Hour ?? TimeText.Uses24Hour(EffectiveCulture);

    /// <summary>Gets a time pattern shown as placeholder.</summary>
    protected override string? PatternHint
    {
        get
        {
            var separator = EffectiveCulture.DateTimeFormat.TimeSeparator;
            var (am, pm) = TimeText.Designators(EffectiveCulture);
            return (Is24Hour ? "hh" : "h") + separator + "mm" + (ShowSeconds ? separator + "ss" : "") + (Is24Hour ? "" : " " + am + "/" + pm);
        }
    }

    /// <summary>Gets the culture and the clock options, which rewrite the text when they change.</summary>
    protected override string FormatKey => EffectiveCulture.Name + "|" + Is24Hour + "|" + ShowSeconds;

    /// <summary>Validates the parameters.</summary>
    protected override void OnParametersSet()
    {
        if (MinuteIncrement is < 1 or > 60) throw new ArgumentOutOfRangeException(nameof(MinuteIncrement));
        if (!Enum.IsDefined(Presentation)) throw new ArgumentOutOfRangeException(nameof(Presentation));
        if (Minimum is { } min && Maximum is { } max && max < min) throw new ArgumentOutOfRangeException(nameof(Maximum));
        base.OnParametersSet();
    }

    /// <summary>Formats a time on the clock in use.</summary>
    /// <param name="value">The time.</param>
    /// <returns>The text.</returns>
    protected override string FormatValue(TimeOnly value) => TimeText.Format(value, EffectiveCulture, Is24Hour, ShowSeconds);

    /// <summary>Parses a typed time.</summary>
    /// <param name="text">The text.</param>
    /// <param name="value">The time.</param>
    /// <returns>true if the text is a time.</returns>
    protected override bool TryParseText(string text, out TimeOnly value)
    {
        if (!TimeText.TryParse(text, EffectiveCulture, out value)) return false;
        if (!ShowSeconds) value = new TimeOnly(value.Hour, value.Minute);
        return true;
    }

    /// <summary>Tells whether a time is within the limits.</summary>
    /// <param name="value">The time.</param>
    /// <returns>true if it can be chosen.</returns>
    protected override bool IsAllowed(TimeOnly value) => (Minimum is not { } min || value >= min) && (Maximum is not { } max || value <= max);

    private TimeOnly Base => CurrentValue ?? Minimum ?? TimeOnly.MinValue;

    private bool AnyAllowed(TimeOnly from, TimeOnly to) => (Maximum is not { } max || from <= max) && (Minimum is not { } min || to >= min);

    /// <summary>Renders the columns or the list.</summary>
    /// <param name="builder">The builder positioned inside the popup.</param>
    protected override void RenderPopup(RenderTreeBuilder builder)
    {
        if (Presentation == TimePickerPresentation.List)
        {
            RenderList(builder);
            return;
        }
        var time = Base;
        builder.OpenElement(0, "TimePickerColumns");
        builder.AddAttribute(1, "role", "group");
        builder.AddAttribute(2, "presentation", "columns");
        builder.AddAttribute(3, "__internal_onkeydown#timecolumns", EventCallback.Factory.Create<KeyboardEventArgs>(this, ColumnsKeyDownAsync));
        builder.AddElementReferenceCapture(4, reference => columnsElement = reference);
        var hours = Is24Hour ? Enumerable.Range(0, 24) : Enumerable.Range(1, 12);
        var shownHour = Is24Hour ? time.Hour : time.Hour % 12 == 0 ? 12 : time.Hour % 12;
        Column(builder, 10, "hours", ControlStrings.Get("Hours"), shownHour, hours,
            h => Is24Hour ? h.ToString("00", EffectiveCulture) : h.ToString(EffectiveCulture), h => !AnyAllowed(new TimeOnly(ToHour(h, time), 0), new TimeOnly(ToHour(h, time), 59, 59)),
            h => new TimeOnly(ToHour(h, time), time.Minute, time.Second));
        var minutes = Enumerable.Range(0, (59 / MinuteIncrement) + 1).Select(i => i * MinuteIncrement);
        if (time.Minute % MinuteIncrement != 0) minutes = minutes.Append(time.Minute).Order();
        Column(builder, 20, "minutes", ControlStrings.Get("Minutes"), time.Minute, minutes,
            m => m.ToString("00", EffectiveCulture), m => !AnyAllowed(new TimeOnly(time.Hour, m, 0), new TimeOnly(time.Hour, m, 59)), m => new TimeOnly(time.Hour, m, time.Second));
        if (ShowSeconds)
            Column(builder, 30, "seconds", ControlStrings.Get("Seconds"), time.Second, Enumerable.Range(0, 60),
                s => s.ToString("00", EffectiveCulture), s => !IsAllowed(new TimeOnly(time.Hour, time.Minute, s)), s => new TimeOnly(time.Hour, time.Minute, s));
        if (!Is24Hour)
        {
            var (am, pm) = TimeText.Designators(EffectiveCulture);
            Column(builder, 40, "period", ControlStrings.Get("AmPm"), time.Hour < 12 ? 0 : 1, [0, 1], p => p == 0 ? am : pm,
                p => !AnyAllowed(new TimeOnly(p * 12, 0), new TimeOnly(p * 12 + 11, 59, 59)), p => new TimeOnly(time.Hour % 12 + 12 * p, time.Minute, time.Second));
        }
        builder.CloseElement();
    }

    private int ToHour(int shown, TimeOnly time) => Is24Hour ? shown : shown % 12 + (time.Hour >= 12 ? 12 : 0);

    private void Column(RenderTreeBuilder builder, int sequence, string name, string label, int selected, IEnumerable<int> values,
        Func<int, string> text, Func<int, bool> disabled, Func<int, TimeOnly> apply)
    {
        builder.OpenRegion(sequence);
        builder.OpenComponent<Listbox<int>>(0);
        builder.AddComponentParameter(1, nameof(Listbox<int>.As), "TimePickerColumn");
        builder.AddComponentParameter(2, nameof(Control.AccessibleName), label);
        builder.AddComponentParameter(3, nameof(Listbox<int>.Value), selected);
        builder.AddComponentParameter(4, nameof(Listbox<int>.SelectionFollowsFocus), true);
        builder.AddComponentParameter(5, nameof(Listbox<int>.ValueChanged), EventCallback.Factory.Create<int>(this, v => ChooseAsync(apply(v), close: false)));
        builder.AddComponentParameter(6, nameof(Listbox<int>.OnActivate), EventCallback.Factory.Create<int>(this, CloseAsync));
        builder.AddComponentParameter(7, nameof(Control.AdditionalAttributes), new Dictionary<string, object?> { ["column"] = name });
        var list = values.ToList();
        builder.AddComponentParameter(8, nameof(Listbox<int>.ChildContent), (RenderFragment)(content =>
        {
            foreach (var item in list)
            {
                content.OpenComponent<ListboxOption<int>>(0);
                content.SetKey(item);
                content.AddComponentParameter(1, nameof(ListboxOption<int>.As), "TimePickerOption");
                content.AddComponentParameter(2, nameof(ListboxOption<int>.Value), item);
                content.AddComponentParameter(3, nameof(ListboxOption<int>.Disabled), disabled(item));
                content.AddComponentParameter(4, nameof(ListboxOption<int>.ChildContent), (RenderFragment)(b => b.AddContent(0, text(item))));
                content.CloseComponent();
            }
        }));
        builder.CloseComponent();
        builder.CloseRegion();
    }

    private void RenderList(RenderTreeBuilder builder)
    {
        var times = new List<TimeOnly>();
        for (var minutes = 0; minutes < 24 * 60; minutes += MinuteIncrement)
        {
            var time = new TimeOnly(minutes / 60, minutes % 60);
            if (IsAllowed(time)) times.Add(time);
        }
        var current = CurrentValue;
        if (current is { } value && !times.Contains(value)) times = [.. times.Append(value).Order()];
        builder.OpenComponent<Listbox<TimeOnly>>(100);
        builder.AddComponentParameter(101, nameof(Listbox<TimeOnly>.As), "TimePickerList");
        builder.AddComponentParameter(102, nameof(Control.AccessibleName), ButtonLabel);
        builder.AddComponentParameter(103, nameof(Listbox<TimeOnly>.Value), current ?? default);
        builder.AddComponentParameter(104, nameof(Listbox<TimeOnly>.ValueChanged), EventCallback.Factory.Create<TimeOnly>(this, t => ChooseAsync(t, close: true)));
        builder.AddComponentParameter(105, nameof(Control.AdditionalAttributes), new Dictionary<string, object?> { ["presentation"] = "list" });
        builder.AddComponentParameter(106, nameof(Listbox<TimeOnly>.ChildContent), (RenderFragment)(content =>
        {
            foreach (var time in times)
            {
                content.OpenComponent<ListboxOption<TimeOnly>>(0);
                content.SetKey(time);
                content.AddComponentParameter(1, nameof(ListboxOption<TimeOnly>.As), "TimePickerOption");
                content.AddComponentParameter(2, nameof(ListboxOption<TimeOnly>.Value), time);
                content.AddComponentParameter(3, nameof(ListboxOption<TimeOnly>.ChildContent), (RenderFragment)(b => b.AddContent(0, FormatValue(time))));
                content.CloseComponent();
            }
        }));
        builder.CloseComponent();
    }

    private Task ChooseAsync(TimeOnly time, bool close)
    {
        if (!IsAllowed(time))
        {
            if (Minimum is { } min && time < min) time = min;
            else if (Maximum is { } max && time > max) time = max;
        }
        return SelectAsync(time, close);
    }

    private async Task ColumnsKeyDownAsync(KeyboardEventArgs e)
    {
        if (e.Handled || e.Key is not ("ArrowLeft" or "ArrowRight") || e.Modifiers != KeyModifiers.None) return;
        if (columnsElement.Element is not { } group) return;
        var focused = ControlTree.Descendants(group).FirstOrDefault(n => (n.States & ElementStates.Focus) != 0);
        if (focused is null) return;
        var columns = ControlTree.Descendants(group).Where(n => n.Name == "TimePickerColumn").ToList();
        var index = columns.FindIndex(c => ControlTree.Ancestors(focused).Contains(c));
        var target = index + (e.Key == "ArrowRight" ? 1 : -1);
        if (index < 0 || target < 0 || target >= columns.Count) return;
        e.Handled = true;
        var stop = ControlTree.Descendants(columns[target]).FirstOrDefault(n => n.Attributes.GetValueOrDefault("tabindex") is 0);
        if (stop is not null) await new ElementReference(stop).FocusAsync();
    }
}
