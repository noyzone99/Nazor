using System.Globalization;

namespace Nazor.Controls.Primitives;

/// <summary>One day of a <see cref="CalendarModel" /> month grid.</summary>
/// <param name="Date">The date.</param>
/// <param name="InMonth">Whether the date belongs to the displayed month rather than to an adjacent one.</param>
/// <param name="Available">Whether the date is within the limits and not excluded.</param>
public readonly record struct CalendarDay(DateOnly Date, bool InMonth, bool Available);

/// <summary>One row of a <see cref="CalendarModel" /> month grid.</summary>
/// <param name="Number">The week number of the culture's calendar rule.</param>
/// <param name="Days">The seven days of the week, starting from the culture's first day of the week.</param>
public readonly record struct CalendarWeek(int Number, IReadOnlyList<CalendarDay> Days);

/// <summary>The unit of a <see cref="CalendarModel.Move" /> step.</summary>
public enum CalendarStep
{
    /// <summary>One day, as the left and right arrows.</summary>
    Day,
    /// <summary>One week, as the up and down arrows.</summary>
    Week,
    /// <summary>One month, as Page Up and Page Down.</summary>
    Month,
    /// <summary>One year, as Shift with Page Up and Page Down.</summary>
    Year,
}

/// <summary>
/// The date arithmetic behind a calendar, independent of how it is drawn: the month grid in the culture's week layout,
/// week numbers, limits, excluded dates and keyboard movement. Calendars, date pickers and range pickers share it, and
/// it serves custom agendas and schedulers.
/// </summary>
public sealed class CalendarModel
{
    /// <summary>Gets or sets the culture that supplies the first day of the week, the week rule and the names.</summary>
    public CultureInfo Culture { get; set; } = CultureInfo.CurrentCulture;

    /// <summary>Gets or sets the first day of the week; null uses the culture's.</summary>
    public DayOfWeek? FirstDayOfWeek { get; set; }

    /// <summary>Gets or sets the earliest available date, or null for no limit.</summary>
    public DateOnly? Minimum { get; set; }

    /// <summary>Gets or sets the latest available date, or null for no limit.</summary>
    public DateOnly? Maximum { get; set; }

    /// <summary>Gets or sets a predicate that excludes individual dates, such as holidays.</summary>
    public Func<DateOnly, bool>? IsExcluded { get; set; }

    /// <summary>Gets the first day of the week in effect.</summary>
    public DayOfWeek WeekStart => FirstDayOfWeek ?? Culture.DateTimeFormat.FirstDayOfWeek;

    /// <summary>Gets the seven days of the week in display order.</summary>
    public IReadOnlyList<DayOfWeek> Weekdays => [.. Enumerable.Range(0, 7).Select(i => (DayOfWeek)(((int)WeekStart + i) % 7))];

    /// <summary>Gets whether a date is within the limits and not excluded.</summary>
    /// <param name="date">The date to test.</param>
    /// <returns>true if the date can be chosen.</returns>
    public bool IsAvailable(DateOnly date) =>
        (Minimum is not { } min || date >= min) && (Maximum is not { } max || date <= max) && IsExcluded?.Invoke(date) is not true;

    /// <summary>Clamps a date to the limits.</summary>
    /// <param name="date">The date to clamp.</param>
    /// <returns>The nearest date within the limits.</returns>
    public DateOnly Clamp(DateOnly date)
    {
        if (Minimum is { } min && date < min) return min;
        if (Maximum is { } max && date > max) return max;
        return date;
    }

    /// <summary>Gets the week number of a date under the culture's calendar week rule.</summary>
    /// <param name="date">The date.</param>
    /// <returns>The week number.</returns>
    public int WeekNumber(DateOnly date) =>
        Culture.Calendar.GetWeekOfYear(date.ToDateTime(TimeOnly.MinValue), Culture.DateTimeFormat.CalendarWeekRule, WeekStart);

    /// <summary>Builds the six-week grid that shows the month of a date, with the adjacent days that complete it.</summary>
    /// <param name="month">Any date of the month to show.</param>
    /// <returns>Six weeks of seven days.</returns>
    public IReadOnlyList<CalendarWeek> Month(DateOnly month)
    {
        var first = new DateOnly(month.Year, month.Month, 1);
        var offset = ((int)first.DayOfWeek - (int)WeekStart + 7) % 7;
        var start = first.DayNumber - offset < DateOnly.MinValue.DayNumber ? DateOnly.MinValue : first.AddDays(-offset);
        var weeks = new List<CalendarWeek>(6);
        for (var week = 0; week < 6; week++)
        {
            var days = new List<CalendarDay>(7);
            for (var day = 0; day < 7; day++)
            {
                var number = start.DayNumber + week * 7 + day;
                if (number > DateOnly.MaxValue.DayNumber) break;
                var date = DateOnly.FromDayNumber(number);
                days.Add(new CalendarDay(date, date.Month == month.Month && date.Year == month.Year, IsAvailable(date)));
            }
            if (days.Count == 0) break;
            weeks.Add(new CalendarWeek(WeekNumber(days[0].Date), days));
        }
        return weeks;
    }

    /// <summary>Moves a date by a number of steps and clamps it to the limits; months keep the day when possible.</summary>
    /// <param name="from">The starting date.</param>
    /// <param name="step">The unit of movement.</param>
    /// <param name="count">The number of steps, negative to move back.</param>
    /// <returns>The moved and clamped date.</returns>
    public DateOnly Move(DateOnly from, CalendarStep step, int count)
    {
        try
        {
            return Clamp(step switch
            {
                CalendarStep.Day => from.AddDays(count),
                CalendarStep.Week => from.AddDays(count * 7),
                CalendarStep.Month => from.AddMonths(count),
                CalendarStep.Year => from.AddYears(count),
                _ => throw new ArgumentOutOfRangeException(nameof(step)),
            });
        }
        catch (ArgumentOutOfRangeException) when (Enum.IsDefined(step))
        {
            return count < 0 ? Clamp(DateOnly.MinValue) : Clamp(DateOnly.MaxValue);
        }
    }

    /// <summary>Gets the abbreviated name of a day of the week in the culture.</summary>
    /// <param name="day">The day of the week.</param>
    /// <returns>The abbreviated name.</returns>
    public string ShortDayName(DayOfWeek day) => Culture.DateTimeFormat.AbbreviatedDayNames[(int)day];

    /// <summary>Gets the month and year title of a date in the culture, capitalized.</summary>
    /// <param name="month">Any date of the month.</param>
    /// <returns>The title, for example "March 2026".</returns>
    public string MonthTitle(DateOnly month)
    {
        var text = month.ToString("Y", Culture);
        return text.Length == 0 ? text : char.ToUpper(text[0], Culture) + text[1..];
    }
}
