using System.Globalization;
using Nazor.Rendering;

namespace Nazor.Controls;

internal static class PickerPartOrder
{
    internal static string[] Read(string pattern, Func<char, string?> part, IEnumerable<string> fallback)
    {
        var result = new List<string>();
        char quote = '\0';
        for (var i = 0; i < pattern.Length; i++)
        {
            var character = pattern[i];
            if (character == '\\') { i++; continue; }
            if (quote != '\0') { if (character == quote) quote = '\0'; continue; }
            if (character is '\'' or '"') { quote = character; continue; }
            var name = part(character);
            if (name is not null && !result.Contains(name)) result.Add(name);
        }
        foreach (var name in fallback) if (!result.Contains(name)) result.Add(name);
        return [.. result];
    }
}

/// <summary>
/// Selects a nullable Gregorian date through localized day/month/year entry segments and draft selector columns.
/// Segment order follows the culture's short-date pattern; hidden segments keep the committed date's components,
/// or use the bounded <see cref="Today" /> fallback while initially empty. Changing month/year clamps the day to
/// the new month length and all draft changes stay within <see cref="Minimum" /> and <see cref="Maximum" />.
/// Confirm commits; Cancel, Escape and outside dismissal leave the original value unchanged. Non-Gregorian calendars
/// are not supported. The popup is <c>SegmentedDatePickerPopup</c>; shared parts are <c>SegmentedPickerSegments</c>,
/// <c>SegmentedPickerSegment</c>, <c>SegmentedPickerColumns</c>, <c>SegmentedPickerColumn</c>,
/// <c>SegmentedPickerOption</c>, <c>SegmentedPickerActions</c>, <c>SegmentedPickerConfirm</c> and <c>SegmentedPickerCancel</c>.
/// </summary>
public sealed class SegmentedDatePicker : SegmentedPickerBase<DateOnly>
{
    private string[] order = ["month", "day", "year"];

    /// <summary>Gets or sets the earliest allowed date, or null for the Gregorian minimum.</summary>
    [Parameter] public DateOnly? Minimum { get; set; }
    /// <summary>Gets or sets the latest allowed date, or null for the Gregorian maximum.</summary>
    [Parameter] public DateOnly? Maximum { get; set; }
    /// <summary>Gets or sets the date used to initialize an empty picker; null uses the computer's date.</summary>
    [Parameter] public DateOnly? Today { get; set; }
    /// <summary>Gets or sets whether the day segment and column are displayed.</summary>
    [Parameter] public bool DayVisible { get; set; } = true;
    /// <summary>Gets or sets whether the month segment and column are displayed.</summary>
    [Parameter] public bool MonthVisible { get; set; } = true;
    /// <summary>Gets or sets whether the year segment and column are displayed.</summary>
    [Parameter] public bool YearVisible { get; set; } = true;
    /// <summary>Gets or sets the custom .NET day display pattern, <c>dd</c> by default.</summary>
    [Parameter] public string DayFormat { get; set; } = "dd";
    /// <summary>Gets or sets the custom .NET month display pattern, <c>MMMM</c> by default.</summary>
    [Parameter] public string MonthFormat { get; set; } = "MMMM";
    /// <summary>Gets or sets the custom .NET year display pattern, <c>yyyy</c> by default.</summary>
    [Parameter] public string YearFormat { get; set; } = "yyyy";

    /// <summary>Gets the NSS element name emitted by SegmentedDatePicker.</summary>
    protected override string ElementName => "SegmentedDatePicker";
    /// <summary>Gets the localized popup name.</summary>
    protected override string PickerLabel => ControlStrings.Get("ChooseDate");
    /// <summary>Gets visible date parts in culture order.</summary>
    protected override IReadOnlyList<string> SegmentNames => order;
    /// <summary>Gets the value-equality configuration key used to cancel stale drafts.</summary>
    protected override object ConfigurationKey => (Minimum, Maximum, Today, DayVisible, MonthVisible, YearVisible,
        DayFormat, MonthFormat, YearFormat, EffectiveCulture.Name, EffectiveCulture.DateTimeFormat.ShortDatePattern);

    /// <summary>Validates date bounds, part visibility, formats and the supported calendar.</summary>
    protected override void ValidateConfiguration()
    {
        if (Minimum is { } min && Maximum is { } max && min > max) throw new ArgumentOutOfRangeException(nameof(Maximum));
        if (!DayVisible && !MonthVisible && !YearVisible) throw new ArgumentException("At least one date segment must be visible.");
        if (EffectiveCulture.DateTimeFormat.Calendar is not GregorianCalendar) throw new NotSupportedException("SegmentedDatePicker supports Gregorian calendars.");
        if (string.IsNullOrWhiteSpace(DayFormat)) throw new ArgumentException("A day format is required.", nameof(DayFormat));
        if (string.IsNullOrWhiteSpace(MonthFormat)) throw new ArgumentException("A month format is required.", nameof(MonthFormat));
        if (string.IsNullOrWhiteSpace(YearFormat)) throw new ArgumentException("A year format is required.", nameof(YearFormat));
        var sample = new DateOnly(2024, 2, 29);
        _ = Format(sample, DayFormat); _ = Format(sample, MonthFormat); _ = Format(sample, YearFormat);
        order = PickerPartOrder.Read(EffectiveCulture.DateTimeFormat.ShortDatePattern,
            character => character switch { 'd' => "day", 'M' => "month", 'y' => "year", _ => null }, ["month", "day", "year"])
            .Where(name => name switch { "day" => DayVisible, "month" => MonthVisible, _ => YearVisible }).ToArray();
    }

    /// <summary>Creates the bounded initial date draft.</summary>
    /// <returns>The committed date, or today's fallback, clamped to the date bounds.</returns>
    protected override DateOnly CreateInitialDraft() => Clamp(CurrentValue ?? Today ?? DateOnly.FromDateTime(DateTime.Today));
    /// <summary>Tests the configured inclusive date bounds.</summary>
    /// <param name="candidate">The date to inspect.</param>
    /// <returns>true when it is within bounds.</returns>
    protected override bool IsAllowed(DateOnly candidate) => (Minimum is not { } min || candidate >= min) && (Maximum is not { } max || candidate <= max);

    private DateOnly Clamp(DateOnly date) => Minimum is { } min && date < min ? min : Maximum is { } max && date > max ? max : date;
    private DateOnly Replace(DateOnly date, int? year = null, int? month = null, int? day = null)
    {
        var nextYear = year ?? date.Year;
        var nextMonth = month ?? date.Month;
        return Clamp(new DateOnly(nextYear, nextMonth, Math.Min(day ?? date.Day, DateTime.DaysInMonth(nextYear, nextMonth))));
    }
    private string Format(DateOnly date, string pattern) => date.ToString(pattern.Length == 1 ? "%" + pattern : pattern, EffectiveCulture);
    private static string Label(string name) => ControlStrings.Get(name switch { "day" => "Day", "month" => "Month", _ => "Year" });
    private string Pattern(string name) => name switch { "day" => DayFormat, "month" => MonthFormat, _ => YearFormat };

    /// <summary>Renders date entry segments with placeholders while unset.</summary>
    /// <param name="builder">The builder receiving the segments.</param>
    protected override void RenderSegments(RenderTreeBuilder builder)
    {
        foreach (var name in order) Segment(builder, 0, name, Label(name), CurrentValue is { } date ? Format(date, Pattern(name)) : null);
    }

    /// <summary>Renders bounded Gregorian date columns; year ranges are indexed lazily.</summary>
    /// <param name="builder">The builder receiving the columns.</param>
    protected override void RenderColumns(RenderTreeBuilder builder)
    {
        var date = Draft;
        var minimum = Minimum ?? DateOnly.MinValue;
        var maximum = Maximum ?? DateOnly.MaxValue;
        foreach (var name in order)
        {
            if (name == "year")
                Column(builder, 0, name, Label(name), date.Year, new PickerIntegerRange(minimum.Year, maximum.Year),
                    year => Format(Replace(date, year: year), YearFormat), year => Replace(date, year: year));
            else if (name == "month")
                Column(builder, 0, name, Label(name), date.Month,
                    new PickerIntegerRange(date.Year == minimum.Year ? minimum.Month : 1, date.Year == maximum.Year ? maximum.Month : 12),
                    month => Format(Replace(date, month: month), MonthFormat), month => Replace(date, month: month));
            else
                Column(builder, 0, name, Label(name), date.Day,
                    new PickerIntegerRange(date.Year == minimum.Year && date.Month == minimum.Month ? minimum.Day : 1,
                        date.Year == maximum.Year && date.Month == maximum.Month ? maximum.Day : DateTime.DaysInMonth(date.Year, date.Month)),
                    day => Format(Replace(date, day: day), DayFormat), day => Replace(date, day: day));
        }
    }
}
