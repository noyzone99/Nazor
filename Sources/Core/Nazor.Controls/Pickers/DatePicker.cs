using System.Globalization;
using Nazor.Controls.Primitives;
using Nazor.Input;
using Nazor.Rendering;

namespace Nazor.Controls;

/// <summary>Formatting and lenient parsing of dates typed in a culture, shared by the date pickers.</summary>
public static class DateText
{
    /// <summary>
    /// Parses a date typed by the user: first with <paramref name="format" />, then with the culture's short and long
    /// patterns (also with one-digit days and months and two-digit years), then with the culture's general rules, which
    /// accept month names and a missing year.
    /// </summary>
    /// <param name="text">The text to parse.</param>
    /// <param name="culture">The culture of the text.</param>
    /// <param name="format">The .NET format the text is displayed in, such as <c>d</c>.</param>
    /// <param name="date">The parsed date.</param>
    /// <returns>true if the text is a date.</returns>
    public static bool TryParse(string? text, CultureInfo culture, string format, out DateOnly date)
    {
        ArgumentNullException.ThrowIfNull(culture);
        date = default;
        if (string.IsNullOrWhiteSpace(text)) return false;
        var trimmed = text.Trim();
        var info = culture.DateTimeFormat;
        var shortPattern = info.ShortDatePattern;
        var relaxed = shortPattern.Replace("dd", "d").Replace("MM", "M");
        string[] formats = [format, "d", "D", shortPattern, relaxed, relaxed.Replace("yyyy", "yy"), shortPattern.Replace("yyyy", "yy"), info.LongDatePattern];
        if (DateOnly.TryParseExact(trimmed, formats.Where(f => !string.IsNullOrEmpty(f)).Distinct().ToArray(), culture, DateTimeStyles.AllowWhiteSpaces, out date)) return true;
        return DateOnly.TryParse(trimmed, culture, DateTimeStyles.AllowWhiteSpaces, out date);
    }

    /// <summary>Gets a readable pattern of a date format, such as <c>dd/MM/yyyy</c>, for placeholders.</summary>
    /// <param name="culture">The culture.</param>
    /// <param name="format">The .NET format, such as <c>d</c>.</param>
    /// <returns>The pattern, or the format itself when it is a custom pattern.</returns>
    public static string Pattern(CultureInfo culture, string format)
    {
        ArgumentNullException.ThrowIfNull(culture);
        return format switch
        {
            "d" => culture.DateTimeFormat.ShortDatePattern,
            "D" => culture.DateTimeFormat.LongDatePattern,
            _ => format,
        };
    }
}

/// <summary>
/// A date field: the <c>DatePickerInput</c> editor parses dates typed in <see cref="PickerFieldBase{TValue}.Culture" />
/// with <see cref="Format" /> (the short date by default) and the <c>DatePickerButton</c> opens the
/// <c>DatePickerPopup</c> with a <see cref="Calendar" /> on the value. <see cref="PickerFieldBase{TValue}.Value" />
/// (<c>@bind-Value</c>) is the chosen date. Choosing a day closes the popup and returns the focus to the editor; a typed
/// date outside <see cref="Minimum" />, <see cref="Maximum" /> or rejected by <see cref="IsDateUnavailable" /> marks the
/// field invalid. With <see cref="ShowTodayButton" /> the popup has a <c>DatePickerToday</c> button in its
/// <c>DatePickerFooter</c>. The other parts are those of <see cref="PickerFieldBase{TValue}" />.
/// </summary>
public sealed class DatePicker : PickerFieldBase<DateOnly>
{
    private Calendar? calendar;

    /// <summary>Gets or sets the .NET format of the date in the editor, <c>d</c> (short date) by default.</summary>
    [Parameter] public string Format { get; set; } = "d";

    /// <summary>Gets or sets the earliest date that can be chosen, or null for no limit.</summary>
    [Parameter] public DateOnly? Minimum { get; set; }

    /// <summary>Gets or sets the latest date that can be chosen, or null for no limit.</summary>
    [Parameter] public DateOnly? Maximum { get; set; }

    /// <summary>Gets or sets a predicate marking dates that cannot be chosen.</summary>
    [Parameter] public Func<DateOnly, bool>? IsDateUnavailable { get; set; }

    /// <summary>Gets or sets the first day of the week in the calendar; null uses the culture's.</summary>
    [Parameter] public DayOfWeek? FirstDayOfWeek { get; set; }

    /// <summary>Gets or sets whether the calendar shows week numbers.</summary>
    [Parameter] public bool ShowWeekNumbers { get; set; }

    /// <summary>Gets or sets the date marked as today; null uses the computer's date.</summary>
    [Parameter] public DateOnly? Today { get; set; }

    /// <summary>Gets or sets whether the popup has a button that chooses today.</summary>
    [Parameter] public bool ShowTodayButton { get; set; }

    /// <summary>Gets the calendar shown in the open popup, or null while it is closed.</summary>
    public Calendar? PopupCalendar => Opened ? calendar : null;

    /// <summary>Gets the NSS element name emitted by DatePicker.</summary>
    protected override string ElementName => "DatePicker";

    /// <summary>Gets the name of the button and of the popup.</summary>
    protected override string ButtonLabel => ControlStrings.Get("ChooseDate");

    /// <summary>Gets the date pattern shown as placeholder.</summary>
    protected override string? PatternHint => DateText.Pattern(EffectiveCulture, Format);

    /// <summary>Gets the culture and the format, which rewrite the text when they change.</summary>
    protected override string FormatKey => EffectiveCulture.Name + "|" + Format;

    private DateOnly TodayDate => Today ?? DateOnly.FromDateTime(DateTime.Today);

    /// <summary>Validates the parameters.</summary>
    protected override void OnParametersSet()
    {
        if (string.IsNullOrWhiteSpace(Format)) throw new ArgumentException("A date format is required.", nameof(Format));
        if (Minimum is { } min && Maximum is { } max && max < min) throw new ArgumentOutOfRangeException(nameof(Maximum));
        base.OnParametersSet();
    }

    /// <summary>Formats a date with <see cref="Format" />.</summary>
    /// <param name="value">The date.</param>
    /// <returns>The text.</returns>
    protected override string FormatValue(DateOnly value) => value.ToString(Format, EffectiveCulture);

    /// <summary>Parses a typed date.</summary>
    /// <param name="text">The text.</param>
    /// <param name="value">The date.</param>
    /// <returns>true if the text is a date.</returns>
    protected override bool TryParseText(string text, out DateOnly value) => DateText.TryParse(text, EffectiveCulture, Format, out value);

    /// <summary>Tells whether a date is within the limits and available.</summary>
    /// <param name="value">The date.</param>
    /// <returns>true if it can be chosen.</returns>
    protected override bool IsAllowed(DateOnly value) =>
        (Minimum is not { } min || value >= min) && (Maximum is not { } max || value <= max) && IsDateUnavailable?.Invoke(value) is not true;

    /// <summary>Renders the calendar and the footer.</summary>
    /// <param name="builder">The builder positioned inside the popup.</param>
    protected override void RenderPopup(RenderTreeBuilder builder)
    {
        builder.OpenComponent<Calendar>(0);
        builder.AddComponentParameter(1, nameof(Calendar.Value), CurrentValue);
        builder.AddComponentParameter(2, nameof(Calendar.ValueChanged), EventCallback.Factory.Create<DateOnly?>(this, date => SelectAsync(date, close: true)));
        builder.AddComponentParameter(3, nameof(Calendar.Minimum), Minimum);
        builder.AddComponentParameter(4, nameof(Calendar.Maximum), Maximum);
        builder.AddComponentParameter(5, nameof(Calendar.IsDateUnavailable), IsDateUnavailable);
        builder.AddComponentParameter(6, nameof(Calendar.Culture), Culture);
        builder.AddComponentParameter(7, nameof(Calendar.FirstDayOfWeek), FirstDayOfWeek);
        builder.AddComponentParameter(8, nameof(Calendar.ShowWeekNumbers), ShowWeekNumbers);
        builder.AddComponentParameter(9, nameof(Calendar.Today), Today);
        builder.AddComponentReferenceCapture(10, component => calendar = (Calendar)component);
        builder.CloseComponent();
        if (!ShowTodayButton) return;
        builder.OpenElement(20, "DatePickerFooter");
        builder.OpenElement(21, "DatePickerToday");
        builder.AddAttribute(22, "role", "button");
        builder.AddAttribute(23, "focusable", IsAllowed(TodayDate));
        builder.AddAttribute(24, "disabled", !IsAllowed(TodayDate));
        builder.AddAttribute(25, "__default_onclick", EventCallback.Factory.Create<PointerEventArgs>(this, () => SelectAsync(TodayDate, close: true)));
        builder.AddContent(26, ControlStrings.Get("Today"));
        builder.CloseElement();
        builder.CloseElement();
    }
}
