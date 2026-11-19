using System.Globalization;
using Nazor.Controls.Primitives;
using Nazor.Input;
using Nazor.Rendering;

namespace Nazor.Controls;

/// <summary>How many dates a <see cref="Calendar" /> selects, written as its <c>selection-mode</c> attribute.</summary>
public enum CalendarSelectionMode
{
    /// <summary>One date, in <see cref="Calendar.Value" />.</summary>
    Single,
    /// <summary>A range of consecutive dates, in <see cref="Calendar.RangeStart" /> and <see cref="Calendar.RangeEnd" />.</summary>
    Range,
    /// <summary>Any set of dates, in <see cref="Calendar.SelectedDates" />.</summary>
    Multiple,
    /// <summary>No selection: the calendar only shows and navigates dates.</summary>
    None,
}

/// <summary>What a <see cref="Calendar" /> shows, written as its <c>view</c> attribute.</summary>
public enum CalendarViewMode
{
    /// <summary>The days of one or more months.</summary>
    Month,
    /// <summary>The twelve months of a year.</summary>
    Year,
    /// <summary>The years of a decade, with the last year of the previous decade and the first of the next.</summary>
    Decade,
}

/// <summary>
/// A month calendar built on <see cref="CalendarModel" />. It selects one date (<see cref="Value" />), a range
/// (<see cref="RangeStart" /> and <see cref="RangeEnd" />) or a set of dates (<see cref="SelectedDates" />) according to
/// <see cref="SelectionMode" />, all bindable. The header has <c>CalendarTitle</c> (which zooms out to the months of the year
/// and the years of the decade), <c>CalendarPrevious</c> and <c>CalendarNext</c> in keyboard order; <see cref="MonthCount" /> months
/// are shown side by side in <c>CalendarMonths</c>, each a <c>CalendarMonth</c> with <c>CalendarMonthTitle</c> (when
/// there are several), <c>CalendarWeekdays</c> (<c>CalendarWeekHeader</c>, <c>CalendarWeekday</c>, whose
/// <c>CalendarWeekdayAbbreviated</c> and <c>CalendarWeekdayShortest</c> labels let themes select localized text) and a
/// <c>CalendarGrid</c> of <c>CalendarWeek</c> rows (<c>CalendarWeekNumber</c>, <c>CalendarDay</c>). The year and decade
/// views show a <c>CalendarGrid</c> of <c>CalendarRow</c> rows of <c>CalendarItem</c>.
/// <c>CalendarTransition</c> and <c>CalendarTransitionLayer</c> retain one disabled outgoing grid for theme-owned paging
/// and zoom motion. The header remains mounted; <c>motion-cycle</c> alternates <c>odd</c>/<c>even</c> when its view changes.
/// <para>
/// Each grid has the <c>listbox</c> role, named by its month, and is a single Tab stop on the focused date: the arrows
/// move by day and week (by month or year in the other views), Page Up and Page Down by month (by year with Shift),
/// Home and End to the ends of the week (of the month with Ctrl), Enter or Space select. Days are <c>option</c>s named
/// by their long date, with <c>position-in-set</c> and <c>size-of-set</c> in the month, and carry the attributes
/// <c>today</c>, <c>selected</c>, <c>range-start</c>, <c>range-middle</c>, <c>range-end</c>, <c>range-preview</c> (the
/// dates between a range start and the focused date while the end is being chosen), <c>outside</c> (adjacent months),
/// <c>unavailable</c>, <c>weekend</c> and <c>current</c> (the focused date). Names of months and days, the first day of
/// the week and the week numbers come from <see cref="Culture" />.
/// </para>
/// </summary>
public sealed class Calendar : Control
{
    private readonly CalendarModel model = new();
    private readonly ControllableState<DateOnly?> value = new(null);
    private readonly ControllableState<DateOnly?> rangeStart = new(null);
    private readonly ControllableState<DateOnly?> rangeEnd = new(null);
    private readonly ControllableState<IReadOnlyCollection<DateOnly>?> dates = new(null);
    private readonly ControllableState<CalendarViewMode> view = new(CalendarViewMode.Month);
    private readonly ControllableState<DateOnly?> displayMonth = new(null);
    private DateOnly shown;
    private DateOnly active;
    private bool started;
    private bool focusPending;
    private (CalendarViewMode View, DateOnly Shown)? renderedView;
    private ContentTransitionKind transition;
    private TransitionDirection direction;
    private int viewGeneration;

    /// <summary>Gets or sets how many dates can be selected.</summary>
    [Parameter] public CalendarSelectionMode SelectionMode { get; set; }

    /// <summary>Gets or sets the selected date in single selection.</summary>
    [Parameter] public DateOnly? Value { get; set; }

    /// <summary>Gets or sets the callback invoked when the user selects a date in single selection.</summary>
    [Parameter] public EventCallback<DateOnly?> ValueChanged { get; set; }

    /// <summary>Gets or sets the first date of the range in range selection.</summary>
    [Parameter] public DateOnly? RangeStart { get; set; }

    /// <summary>Gets or sets the callback invoked when the user changes the start of the range.</summary>
    [Parameter] public EventCallback<DateOnly?> RangeStartChanged { get; set; }

    /// <summary>Gets or sets the last date of the range in range selection; null while it is being chosen.</summary>
    [Parameter] public DateOnly? RangeEnd { get; set; }

    /// <summary>Gets or sets the callback invoked when the user changes the end of the range.</summary>
    [Parameter] public EventCallback<DateOnly?> RangeEndChanged { get; set; }

    /// <summary>Gets or sets the selected dates in multiple selection.</summary>
    [Parameter] public IReadOnlyCollection<DateOnly>? SelectedDates { get; set; }

    /// <summary>Gets or sets the callback invoked with the new sorted set when the user toggles a date in multiple selection.</summary>
    [Parameter] public EventCallback<IReadOnlyCollection<DateOnly>> SelectedDatesChanged { get; set; }

    /// <summary>Gets or sets the callback invoked whenever the user picks an available day, in every selection mode.</summary>
    [Parameter] public EventCallback<DateOnly> OnDateSelected { get; set; }

    /// <summary>Gets or sets the earliest available date, or null for no limit.</summary>
    [Parameter] public DateOnly? Minimum { get; set; }

    /// <summary>Gets or sets the latest available date, or null for no limit.</summary>
    [Parameter] public DateOnly? Maximum { get; set; }

    /// <summary>Gets or sets a predicate marking dates that cannot be selected, such as holidays; they stay focusable.</summary>
    [Parameter] public Func<DateOnly, bool>? IsDateUnavailable { get; set; }

    /// <summary>Gets or sets the culture of the names, the first day of the week and the week numbers; the current culture by default.</summary>
    [Parameter] public CultureInfo? Culture { get; set; }

    /// <summary>Gets or sets the first day of the week; null uses the culture's.</summary>
    [Parameter] public DayOfWeek? FirstDayOfWeek { get; set; }

    /// <summary>Gets or sets whether a column shows the week numbers.</summary>
    [Parameter] public bool ShowWeekNumbers { get; set; }

    /// <summary>Gets or sets whether the days of the adjacent months fill the first and last weeks; always false with several months.</summary>
    [Parameter] public bool ShowOutsideDays { get; set; } = true;

    /// <summary>Gets or sets the date marked as today; null uses the computer's date. Tests and previews set it.</summary>
    [Parameter] public DateOnly? Today { get; set; }

    /// <summary>Gets or sets how many months are shown side by side, 1 or 2.</summary>
    [Parameter] public int MonthCount { get; set; } = 1;

    /// <summary>Gets or sets what the calendar shows.</summary>
    [Parameter] public CalendarViewMode View { get; set; }

    /// <summary>Gets or sets the callback invoked when the user zooms in or out.</summary>
    [Parameter] public EventCallback<CalendarViewMode> ViewChanged { get; set; }

    /// <summary>Gets or sets any day of the first month shown; null follows the selection.</summary>
    [Parameter] public DateOnly? DisplayMonth { get; set; }

    /// <summary>Gets or sets the callback invoked with the first day of the first month shown when the user navigates.</summary>
    [Parameter] public EventCallback<DateOnly?> DisplayMonthChanged { get; set; }

    /// <summary>Gets or sets whether the calendar rejects interaction.</summary>
    [Parameter] public bool Disabled { get; set; }

    /// <summary>Gets or sets whether the selection cannot change while navigation still works.</summary>
    [Parameter] public bool ReadOnly { get; set; }

    /// <summary>Gets what the calendar shows.</summary>
    public CalendarViewMode CurrentView => view.Value;

    /// <summary>Gets the first day of the first month shown.</summary>
    public DateOnly ShownMonth => shown;

    /// <summary>Gets the focused date, which holds the Tab stop.</summary>
    public DateOnly ActiveDate => active;

    /// <summary>Gets the date arithmetic in use.</summary>
    public CalendarModel Model => model;

    /// <summary>Gets the element of the focused date, or the calendar itself before the first render.</summary>
    public ElementReference ActiveElement => Element.Element is { } root
        && ControlTree.Descendants(root).FirstOrDefault(n => n.Attributes.GetValueOrDefault("current") is true && n.Attributes.GetValueOrDefault("calendar-item") is true && !n.IsDisabled) is { } item
        ? new ElementReference(item) : Element;

    /// <summary>Gets the NSS element name emitted by Calendar.</summary>
    protected override string ElementName => "Calendar";

    /// <summary>Supplies month-count and week-column geometry to themes before application style overrides.</summary>
    protected override string? ElementStyle => FormattableString.Invariant($"--nz-calendar-month-count:{MonthCount};--nz-calendar-week-columns:{(ShowWeekNumbers ? 8 : 7)};") + base.ElementStyle;

    private CultureInfo EffectiveCulture => Culture ?? CultureInfo.CurrentCulture;

    private DateOnly TodayDate => Today ?? DateOnly.FromDateTime(DateTime.Today);

    private int Months => view.Value == CalendarViewMode.Month ? MonthCount : 1;

    /// <summary>Validates the parameters, configures the model and synchronizes the selection and the view.</summary>
    protected override void OnParametersSet()
    {
        if (!Enum.IsDefined(SelectionMode)) throw new ArgumentOutOfRangeException(nameof(SelectionMode));
        if (!Enum.IsDefined(View)) throw new ArgumentOutOfRangeException(nameof(View));
        if (FirstDayOfWeek is { } first && !Enum.IsDefined(first)) throw new ArgumentOutOfRangeException(nameof(FirstDayOfWeek));
        if (MonthCount is < 1 or > 2) throw new ArgumentOutOfRangeException(nameof(MonthCount), "A calendar shows one or two months.");
        if (Minimum is { } min && Maximum is { } max && max < min) throw new ArgumentOutOfRangeException(nameof(Maximum));
        model.Culture = EffectiveCulture;
        model.FirstDayOfWeek = FirstDayOfWeek;
        model.Minimum = Minimum;
        model.Maximum = Maximum;
        model.IsExcluded = IsDateUnavailable;
        var selectionChanged = value.Sync(Value) | rangeStart.Sync(RangeStart) | rangeEnd.Sync(RangeEnd) | dates.Sync(SelectedDates);
        view.Sync(View);
        var displayChanged = displayMonth.Sync(DisplayMonth);
        if (!started || selectionChanged && Anchor() is not null)
        {
            active = model.Clamp(Anchor() ?? TodayDate);
            shown = FirstOfMonth(active);
        }
        if (displayChanged && displayMonth.Value is { } month)
        {
            shown = FirstOfMonth(month);
            if (!IsVisible(active)) active = model.Clamp(shown);
        }
        started = true;
    }

    private DateOnly? Anchor() => SelectionMode switch
    {
        CalendarSelectionMode.Single => value.Value,
        CalendarSelectionMode.Range => rangeStart.Value ?? rangeEnd.Value,
        CalendarSelectionMode.Multiple => dates.Value is { Count: > 0 } set ? set.Min() : null,
        _ => null,
    };

    /// <summary>Adds the calendar role and its states.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void AddAttributes(RenderTreeBuilder builder)
    {
        var identity = (view.Value, shown);
        if (renderedView is { } old && old != identity)
        {
            transition = old.View < view.Value ? ContentTransitionKind.DrillOut : old.View > view.Value ? ContentTransitionKind.DrillIn : ContentTransitionKind.Slide;
            direction = shown < old.Shown ? TransitionDirection.Backward : TransitionDirection.Forward;
            viewGeneration++;
        }
        renderedView = identity;
        builder.AddAttribute(10, "role", "calendar");
        builder.AddAttribute(11, "view", ViewName(view.Value));
        builder.AddAttribute(12, "selection-mode", SelectionMode.ToString().ToLowerInvariant());
        builder.AddAttribute(13, "disabled", Disabled);
        builder.AddAttribute(14, "readonly", ReadOnly);
        builder.AddAttribute(15, "month-count", Months);
        builder.AddAttribute(16, "week-numbers", ShowWeekNumbers && view.Value == CalendarViewMode.Month);
        builder.AddAttribute(17, "motion-cycle", viewGeneration == 0 ? null : viewGeneration % 2 == 0 ? "even" : "odd");
    }

    /// <summary>Adds the header and the grid of the current view.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void AddContent(RenderTreeBuilder builder)
    {
        var (previous, next) = view.Value switch
        {
            CalendarViewMode.Month => ("PreviousMonth", "NextMonth"),
            CalendarViewMode.Year => ("PreviousYear", "NextYear"),
            _ => ("PreviousDecade", "NextDecade"),
        };
        builder.OpenElement(1000, "CalendarHeader");
        builder.OpenElement(1010, "CalendarTitle");
        builder.AddAttribute(1011, "role", "button");
        builder.AddAttribute(1012, "accessible-description", ControlStrings.Get("ChangeView"));
        builder.AddAttribute(1013, "live", "polite");
        builder.AddAttribute(1014, "disabled", view.Value == CalendarViewMode.Decade);
        builder.AddAttribute(1015, "focusable", view.Value != CalendarViewMode.Decade);
        builder.AddAttribute(1016, "__default_onclick", EventCallback.Factory.Create<PointerEventArgs>(this, ZoomOutAsync));
        builder.AddContent(1017, Title());
        builder.CloseElement();
        HeaderButton(builder, 1020, "CalendarPrevious", "chevron-left", ControlStrings.Get(previous), !CanMove(-1), () => MoveAsync(-1));
        HeaderButton(builder, 1030, "CalendarNext", "chevron-right", ControlStrings.Get(next), !CanMove(1), () => MoveAsync(1));
        builder.CloseElement();
        var state = Snapshot();
        builder.OpenComponent<ContentTransition>(1100);
        builder.AddComponentParameter(1101, nameof(ContentTransition.As), "CalendarTransition");
        builder.AddComponentParameter(1102, nameof(ContentTransition.ContentKey), (view.Value, shown, Months));
        builder.AddComponentParameter(1103, nameof(ContentTransition.Transition), transition);
        builder.AddComponentParameter(1104, nameof(ContentTransition.Direction), direction);
        builder.AddComponentParameter(1105, nameof(ContentTransition.ChildContent), (RenderFragment)(content => RenderView(content, state)));
        builder.AddComponentParameter(1106, nameof(ContentTransition.InitialFocus), (Func<ElementReference?>)(() => ActiveElement));
        builder.CloseComponent();
    }

    private void RenderView(RenderTreeBuilder builder, CalendarState state)
    {
        if (state.View != CalendarViewMode.Month) { RenderItems(builder, state); return; }
        builder.OpenElement(0, "CalendarMonths");
        for (var index = 0; index < state.Months; index++)
        {
            builder.OpenRegion(1);
            RenderMonth(builder, state.Shown.AddMonths(index), index, state);
            builder.CloseRegion();
        }
        builder.CloseElement();
    }

    private CalendarState Snapshot() => new(view.Value, shown, active, Months, TodayDate, SelectionMode, value.Value,
        rangeStart.Value, rangeEnd.Value, dates.Value?.ToHashSet(), ShowWeekNumbers, ShowOutsideDays, Title(), new CalendarModel
        {
            Culture = EffectiveCulture, FirstDayOfWeek = FirstDayOfWeek, Minimum = Minimum, Maximum = Maximum, IsExcluded = IsDateUnavailable,
        });

    private sealed record CalendarState(CalendarViewMode View, DateOnly Shown, DateOnly Active, int Months, DateOnly Today,
        CalendarSelectionMode SelectionMode, DateOnly? Value, DateOnly? RangeStart, DateOnly? RangeEnd, HashSet<DateOnly>? Dates,
        bool WeekNumbers, bool OutsideDays, string Title, CalendarModel Model);

    private void HeaderButton(RenderTreeBuilder builder, int sequence, string name, string glyph, string label, bool disabled, Func<Task> action)
    {
        builder.OpenElement(sequence, name);
        builder.AddAttribute(sequence + 1, "role", "button");
        builder.AddAttribute(sequence + 2, "accessible-name", label);
        builder.AddAttribute(sequence + 3, "glyph", glyph);
        builder.AddAttribute(sequence + 4, "disabled", disabled);
        builder.AddAttribute(sequence + 5, "focusable", !disabled);
        builder.AddAttribute(sequence + 6, "__default_onclick", EventCallback.Factory.Create<PointerEventArgs>(this, action));
        builder.CloseElement();
    }

    private string Title()
    {
        var culture = EffectiveCulture;
        return view.Value switch
        {
            CalendarViewMode.Month when Months > 1 => model.MonthTitle(shown) + " – " + model.MonthTitle(shown.AddMonths(Months - 1)),
            CalendarViewMode.Month => model.MonthTitle(shown),
            CalendarViewMode.Year => shown.Year.ToString(culture),
            _ => DecadeStart(shown.Year).ToString(culture) + "–" + (DecadeStart(shown.Year) + 9).ToString(culture),
        };
    }

    private void RenderMonth(RenderTreeBuilder builder, DateOnly month, int index, CalendarState state)
    {
        var model = state.Model;
        var culture = model.Culture ?? CultureInfo.CurrentCulture;
        var weekNumbers = state.WeekNumbers;
        var outside = state.OutsideDays && state.Months == 1;
        var title = model.MonthTitle(month);
        var size = DateTime.DaysInMonth(month.Year, month.Month);
        builder.OpenElement(0, "CalendarMonth");
        builder.SetKey(month.Year * 100 + month.Month);
        builder.AddAttribute(1, "index", index);
        if (state.Months > 1)
        {
            builder.OpenElement(2, "CalendarMonthTitle");
            builder.AddAttribute(3, "semantic-hidden", true);
            builder.AddContent(4, title);
            builder.CloseElement();
        }
        builder.OpenElement(10, "CalendarWeekdays");
        builder.AddAttribute(11, "semantic-hidden", true);
        if (weekNumbers)
        {
            builder.OpenElement(12, "CalendarWeekHeader");
            builder.AddContent(13, ControlStrings.Get("Week"));
            builder.CloseElement();
        }
        foreach (var day in model.Weekdays)
        {
            builder.OpenElement(14, "CalendarWeekday");
            builder.SetKey(day);
            builder.AddAttribute(15, "weekend", IsWeekend(day));
            builder.OpenElement(16, "CalendarWeekdayAbbreviated");
            builder.AddContent(17, model.ShortDayName(day));
            builder.CloseElement();
            builder.OpenElement(18, "CalendarWeekdayShortest");
            builder.AddContent(19, culture.DateTimeFormat.ShortestDayNames[(int)day]);
            builder.CloseElement();
            builder.CloseElement();
        }
        builder.CloseElement();
        builder.OpenElement(20, "CalendarGrid");
        builder.AddAttribute(21, "role", "listbox");
        builder.AddAttribute(22, "accessible-name", title);
        builder.AddAttribute(23, "view", "month");
        builder.AddAttribute(24, "multiselectable", state.SelectionMode is CalendarSelectionMode.Multiple or CalendarSelectionMode.Range);
        builder.AddAttribute(25, "__internal_onkeydown#calendar", EventCallback.Factory.Create<KeyboardEventArgs>(this, KeyDownAsync));
        foreach (var week in model.Month(month))
        {
            builder.OpenElement(30, "CalendarWeek");
            builder.SetKey(week.Days[0].Date.DayNumber);
            if (weekNumbers)
            {
                builder.OpenElement(31, "CalendarWeekNumber");
                builder.AddAttribute(32, "semantic-hidden", true);
                builder.AddAttribute(33, "tooltip", ControlStrings.Format("WeekNumber", week.Number));
                builder.AddContent(34, week.Number.ToString(culture));
                builder.CloseElement();
            }
            foreach (var day in week.Days)
            {
                var date = day.Date;
                builder.OpenElement(40, "CalendarDay");
                builder.SetKey(date.DayNumber);
                builder.AddAttribute(41, "outside", !day.InMonth);
                builder.AddAttribute(42, "weekend", IsWeekend(date.DayOfWeek));
                builder.AddAttribute(43, "today", date == state.Today);
                builder.AddAttribute(44, "unavailable", !day.Available);
                if (!day.InMonth && !outside)
                {
                    builder.AddAttribute(45, "empty", true);
                    builder.AddAttribute(46, "semantic-hidden", true);
                    builder.CloseElement();
                    continue;
                }
                var (selected, start, middle, end, preview) = SelectionOf(date, state);
                builder.AddAttribute(47, "selected", selected);
                builder.AddAttribute(48, "range-start", start);
                builder.AddAttribute(49, "range-middle", middle);
                builder.AddAttribute(50, "range-end", end);
                builder.AddAttribute(51, "range-preview", preview);
                builder.AddAttribute(52, "calendar-item", true);
                builder.AddAttribute(53, "__default_onclick", EventCallback.Factory.Create<PointerEventArgs>(this, () => ChooseDayAsync(date)));
                if (day.InMonth)
                {
                    var current = date == state.Active;
                    builder.AddAttribute(54, "role", "option");
                    builder.AddAttribute(55, "accessible-name", date.ToString("D", culture));
                    builder.AddAttribute(56, "accessible-description", Description(date == state.Today, day.Available));
                    builder.AddAttribute(57, "current", current);
                    builder.AddAttribute(58, "focusable", true);
                    builder.AddAttribute(59, "tabindex", current ? 0 : -1);
                    builder.AddAttribute(60, "position-in-set", date.Day);
                    builder.AddAttribute(61, "size-of-set", size);
                    builder.AddAttribute(62, "__internal_onfocus#calendar", EventCallback.Factory.Create<FocusEventArgs>(this, () => Focused(date)));
                }
                else builder.AddAttribute(63, "semantic-hidden", true);
                builder.AddContent(64, date.Day.ToString(culture));
                builder.CloseElement();
            }
            builder.CloseElement();
        }
        builder.CloseElement();
        builder.CloseElement();
    }

    private static string? Description(bool today, bool available) => (today, available) switch
    {
        (true, true) => ControlStrings.Get("Today"),
        (true, false) => ControlStrings.Get("Today") + ", " + ControlStrings.Get("Unavailable"),
        (false, false) => ControlStrings.Get("Unavailable"),
        _ => null,
    };

    private bool IsWeekend(DayOfWeek day) => day is DayOfWeek.Saturday or DayOfWeek.Sunday;

    private static (bool Selected, bool Start, bool Middle, bool End, bool Preview) SelectionOf(DateOnly date, CalendarState state)
    {
        switch (state.SelectionMode)
        {
            case CalendarSelectionMode.Single:
                return (state.Value == date, false, false, false, false);
            case CalendarSelectionMode.Multiple:
                return (state.Dates?.Contains(date) == true, false, false, false, false);
            case CalendarSelectionMode.Range:
                var (start, end) = (state.RangeStart, state.RangeEnd);
                if (start is { } s && end is { } e)
                {
                    var inside = date >= s && date <= e;
                    return (inside, date == s, inside && date != s && date != e, date == e, false);
                }
                if (start is { } only)
                {
                    var (low, high) = state.Active < only ? (state.Active, only) : (only, state.Active);
                    return (date == only, date == only, false, false, date != only && date >= low && date <= high);
                }
                return default;
            default:
                return default;
        }
    }

    private void RenderItems(RenderTreeBuilder builder, CalendarState state)
    {
        var model = state.Model;
        var shown = state.Shown;
        var active = state.Active;
        var culture = model.Culture ?? CultureInfo.CurrentCulture;
        var decade = state.View == CalendarViewMode.Decade;
        var items = new List<(DateOnly Value, string Text, string Name, bool Selected, bool Current, bool Today, bool Outside, bool Unavailable)>(12);
        var today = state.Today;
        if (!decade)
        {
            for (var month = 1; month <= 12; month++)
            {
                var first = new DateOnly(shown.Year, month, 1);
                var last = first.AddDays(DateTime.DaysInMonth(first.Year, first.Month) - 1);
                items.Add((first, culture.DateTimeFormat.AbbreviatedMonthNames[month - 1], model.MonthTitle(first), Contains(first, last, state),
                    active.Year == first.Year && active.Month == month, today.Year == first.Year && today.Month == month, false, !Overlaps(first, last, model)));
            }
        }
        else
        {
            var start = DecadeStart(shown.Year);
            for (var year = start - 1; year <= start + 10; year++)
            {
                if (year < 1 || year > 9999) continue;
                var first = new DateOnly(year, 1, 1);
                var last = new DateOnly(year, 12, 31);
                items.Add((first, year.ToString(culture), year.ToString(culture), Contains(first, last, state), active.Year == year, today.Year == year,
                    year < start || year > start + 9, !Overlaps(first, last, model)));
            }
        }
        builder.OpenElement(0, "CalendarGrid");
        builder.AddAttribute(1, "role", "listbox");
        builder.AddAttribute(2, "accessible-name", state.Title);
        builder.AddAttribute(3, "view", ViewName(state.View));
        builder.AddAttribute(4, "__internal_onkeydown#calendar", EventCallback.Factory.Create<KeyboardEventArgs>(this, KeyDownAsync));
        for (var row = 0; row * 4 < items.Count; row++)
        {
            builder.OpenElement(10, "CalendarRow");
            builder.SetKey(row);
            for (var column = row * 4; column < Math.Min(items.Count, row * 4 + 4); column++)
            {
                var item = items[column];
                var target = item.Value;
                builder.OpenElement(20, "CalendarItem");
                builder.SetKey(target.DayNumber);
                builder.AddAttribute(21, "role", "option");
                builder.AddAttribute(22, "calendar-item", true);
                builder.AddAttribute(23, "accessible-name", item.Name);
                builder.AddAttribute(24, "selected", item.Selected);
                builder.AddAttribute(25, "current", item.Current);
                builder.AddAttribute(26, "today", item.Today);
                builder.AddAttribute(27, "outside", item.Outside);
                builder.AddAttribute(28, "unavailable", item.Unavailable);
                builder.AddAttribute(29, "focusable", true);
                builder.AddAttribute(30, "tabindex", item.Current ? 0 : -1);
                builder.AddAttribute(31, "position-in-set", column + 1);
                builder.AddAttribute(32, "size-of-set", items.Count);
                builder.AddAttribute(33, "__default_onclick", EventCallback.Factory.Create<PointerEventArgs>(this, () => ZoomInAsync(target)));
                builder.AddAttribute(34, "__internal_onfocus#calendar", EventCallback.Factory.Create<FocusEventArgs>(this, () => FocusedItem(target)));
                builder.AddContent(35, item.Text);
                builder.CloseElement();
            }
            builder.CloseElement();
        }
        builder.CloseElement();
    }

    private static bool Contains(DateOnly first, DateOnly last, CalendarState state) => state.SelectionMode switch
    {
        CalendarSelectionMode.Single => state.Value is { } v && v >= first && v <= last,
        CalendarSelectionMode.Multiple => state.Dates?.Any(d => d >= first && d <= last) == true,
        CalendarSelectionMode.Range => state.RangeStart is { } s && (state.RangeEnd ?? s) >= first && s <= last,
        _ => false,
    };

    /// <summary>Moves the focus to the focused date after keyboard navigation.</summary>
    /// <param name="firstRender">Whether this is the first render.</param>
    /// <returns>A task that completes when the focus has moved.</returns>
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!focusPending) return;
        focusPending = false;
        if (ActiveElement.Element is { } item && !ReferenceEquals(item, Element.Element)) await new ElementReference(item).FocusAsync();
    }

    /// <summary>Moves the keyboard focus to the focused date.</summary>
    /// <returns>A task that completes when the focus has moved.</returns>
    public Task FocusActiveAsync() => ActiveElement.FocusAsync();

    /// <summary>Shows the month of a date and focuses it, as keyboard navigation would.</summary>
    /// <param name="date">The date to show; it is clamped to the limits.</param>
    /// <returns>A task that completes when the change has been reported.</returns>
    public Task GoToAsync(DateOnly date)
    {
        var before = shown;
        Go(date);
        StateHasChanged();
        return before == shown ? Task.CompletedTask : ReportShownAsync();
    }

    private static string ViewName(CalendarViewMode mode) => mode.ToString().ToLowerInvariant();

    private static int DecadeStart(int year) => year - year % 10;

    private static DateOnly FirstOfMonth(DateOnly date) => new(date.Year, date.Month, 1);

    private DateOnly LastVisible => view.Value switch
    {
        CalendarViewMode.Month => shown.AddMonths(Months).AddDays(-1),
        CalendarViewMode.Year => new DateOnly(shown.Year, 12, 31),
        _ => new DateOnly(Math.Min(9999, DecadeStart(shown.Year) + 9), 12, 31),
    };

    private DateOnly FirstVisible => view.Value switch
    {
        CalendarViewMode.Month => shown,
        CalendarViewMode.Year => new DateOnly(shown.Year, 1, 1),
        _ => new DateOnly(Math.Max(1, DecadeStart(shown.Year)), 1, 1),
    };

    private bool IsVisible(DateOnly date) => date >= FirstVisible && date <= LastVisible;

    private bool Overlaps(DateOnly from, DateOnly to) => Overlaps(from, to, model);

    private static bool Overlaps(DateOnly from, DateOnly to, CalendarModel model) => (model.Maximum is not { } max || from <= max) && (model.Minimum is not { } min || to >= min);

    private void Go(DateOnly date)
    {
        active = model.Clamp(date);
        if (IsVisible(active)) return;
        var after = active > LastVisible;
        shown = FirstOfMonth(active);
        if (view.Value == CalendarViewMode.Month && Months > 1 && after && shown.Year * 12 + shown.Month > 1 + Months - 1) shown = shown.AddMonths(-(Months - 1));
    }

    private (DateOnly From, DateOnly To)? Period(int direction)
    {
        try
        {
            return view.Value switch
            {
                CalendarViewMode.Month => (shown.AddMonths(direction), shown.AddMonths(direction + Months).AddDays(-1)),
                CalendarViewMode.Year => (new DateOnly(shown.Year + direction, 1, 1), new DateOnly(shown.Year + direction, 12, 31)),
                _ => (new DateOnly(DecadeStart(shown.Year) + 10 * direction, 1, 1), new DateOnly(DecadeStart(shown.Year) + 10 * direction + 9, 12, 31)),
            };
        }
        catch (ArgumentOutOfRangeException)
        {
            return null;
        }
    }

    private bool CanMove(int direction) => !Disabled && Period(direction) is { } period && Overlaps(period.From, period.To);

    private async Task MoveAsync(int direction)
    {
        if (!CanMove(direction) || Period(direction) is not { } period) return;
        shown = view.Value == CalendarViewMode.Month ? period.From : new DateOnly(period.From.Year, shown.Month, 1);
        var step = view.Value switch { CalendarViewMode.Month => CalendarStep.Month, _ => CalendarStep.Year };
        var moved = model.Move(active, step, view.Value == CalendarViewMode.Decade ? 10 * direction : direction);
        if (moved < FirstVisible) moved = model.Clamp(FirstVisible);
        if (moved > LastVisible) moved = model.Clamp(LastVisible);
        active = moved;
        StateHasChanged();
        await ReportShownAsync();
    }

    private Task ReportShownAsync()
    {
        displayMonth.Set(shown);
        return DisplayMonthChanged.InvokeAsync(shown);
    }

    private async Task SetViewAsync(CalendarViewMode next)
    {
        if (!view.Set(next)) return;
        if (!IsVisible(active)) shown = FirstOfMonth(active);
        if (next == CalendarViewMode.Month) shown = FirstOfMonth(active);
        focusPending = true;
        StateHasChanged();
        await ViewChanged.InvokeAsync(next);
    }

    private Task ZoomOutAsync() => Disabled || view.Value == CalendarViewMode.Decade ? Task.CompletedTask : SetViewAsync(view.Value + 1);

    private async Task ZoomInAsync(DateOnly target)
    {
        if (Disabled) return;
        var last = view.Value == CalendarViewMode.Year ? target.AddMonths(1).AddDays(-1) : new DateOnly(target.Year, 12, 31);
        if (!Overlaps(target, last)) return;
        var month = view.Value == CalendarViewMode.Year ? target.Month : active.Month;
        active = model.Clamp(new DateOnly(target.Year, month, Math.Min(active.Day, DateTime.DaysInMonth(target.Year, month))));
        shown = FirstOfMonth(active);
        await SetViewAsync(view.Value - 1);
        await ReportShownAsync();
    }

    private void Focused(DateOnly date)
    {
        if (active == date) return;
        active = date;
        StateHasChanged();
    }

    private void FocusedItem(DateOnly target)
    {
        var date = view.Value == CalendarViewMode.Year
            ? new DateOnly(target.Year, target.Month, Math.Min(active.Day, DateTime.DaysInMonth(target.Year, target.Month)))
            : new DateOnly(target.Year, active.Month, Math.Min(active.Day, DateTime.DaysInMonth(target.Year, active.Month)));
        if (date == active) return;
        active = model.Clamp(date);
        StateHasChanged();
    }

    /// <summary>Selects a date as a click on its day would, according to the selection mode.</summary>
    /// <param name="date">The date; unavailable dates are ignored.</param>
    /// <returns>A task that completes when the change has been reported.</returns>
    public async Task ChooseDayAsync(DateOnly date)
    {
        if (Disabled || !model.IsAvailable(date)) return;
        var before = shown;
        Go(date);
        if (ReadOnly)
        {
            StateHasChanged();
            return;
        }
        switch (SelectionMode)
        {
            case CalendarSelectionMode.Single:
                if (value.Set(date))
                {
                    StateHasChanged();
                    await ValueChanged.InvokeAsync(date);
                    await Events.RaiseChangeAsync(Element, date);
                }
                break;
            case CalendarSelectionMode.Multiple:
                var set = new SortedSet<DateOnly>(dates.Value ?? []);
                if (!set.Remove(date)) set.Add(date);
                IReadOnlyCollection<DateOnly> list = [.. set];
                dates.Set(list);
                StateHasChanged();
                await SelectedDatesChanged.InvokeAsync(list);
                await Events.RaiseChangeAsync(Element, list);
                break;
            case CalendarSelectionMode.Range:
                var (start, end) = (rangeStart.Value, rangeEnd.Value);
                (DateOnly? Start, DateOnly? End) next = start is { } s && end is null ? date < s ? (date, s) : (s, date) : (date, null);
                var startChanged = rangeStart.Set(next.Start);
                var endChanged = rangeEnd.Set(next.End);
                StateHasChanged();
                if (startChanged) await RangeStartChanged.InvokeAsync(next.Start);
                if (endChanged) await RangeEndChanged.InvokeAsync(next.End);
                if (startChanged || endChanged) await Events.RaiseChangeAsync(Element, (next.Start, next.End));
                break;
            default:
                StateHasChanged();
                break;
        }
        await OnDateSelected.InvokeAsync(date);
        if (before != shown) await ReportShownAsync();
    }

    private async Task KeyDownAsync(KeyboardEventArgs e)
    {
        if (Disabled || e.Handled) return;
        var shift = (e.Modifiers & KeyModifiers.Shift) != 0;
        var control = (e.Modifiers & KeyModifiers.Control) != 0;
        if ((e.Modifiers & (KeyModifiers.Alt | KeyModifiers.Meta)) != 0) return;
        if (e.Key is "Enter" or " ")
        {
            e.Handled = true;
            if (view.Value == CalendarViewMode.Month) await ChooseDayAsync(active);
            else await ZoomInAsync(view.Value == CalendarViewMode.Year ? FirstOfMonth(active) : new DateOnly(active.Year, 1, 1));
            return;
        }
        DateOnly? target = view.Value switch
        {
            CalendarViewMode.Month => e.Key switch
            {
                "ArrowLeft" => model.Move(active, CalendarStep.Day, -1),
                "ArrowRight" => model.Move(active, CalendarStep.Day, 1),
                "ArrowUp" => model.Move(active, CalendarStep.Week, -1),
                "ArrowDown" => model.Move(active, CalendarStep.Week, 1),
                "PageUp" => model.Move(active, shift ? CalendarStep.Year : CalendarStep.Month, -1),
                "PageDown" => model.Move(active, shift ? CalendarStep.Year : CalendarStep.Month, 1),
                "Home" when control => model.Clamp(FirstOfMonth(active)),
                "End" when control => model.Clamp(FirstOfMonth(active).AddDays(DateTime.DaysInMonth(active.Year, active.Month) - 1)),
                "Home" => model.Move(active, CalendarStep.Day, -(((int)active.DayOfWeek - (int)model.WeekStart + 7) % 7)),
                "End" => model.Move(active, CalendarStep.Day, 6 - ((int)active.DayOfWeek - (int)model.WeekStart + 7) % 7),
                _ => null,
            },
            CalendarViewMode.Year => e.Key switch
            {
                "ArrowLeft" => model.Move(active, CalendarStep.Month, -1),
                "ArrowRight" => model.Move(active, CalendarStep.Month, 1),
                "ArrowUp" => model.Move(active, CalendarStep.Month, -4),
                "ArrowDown" => model.Move(active, CalendarStep.Month, 4),
                "Home" => model.Move(active, CalendarStep.Month, 1 - active.Month),
                "End" => model.Move(active, CalendarStep.Month, 12 - active.Month),
                "PageUp" => model.Move(active, CalendarStep.Year, shift ? -10 : -1),
                "PageDown" => model.Move(active, CalendarStep.Year, shift ? 10 : 1),
                _ => null,
            },
            _ => e.Key switch
            {
                "ArrowLeft" => model.Move(active, CalendarStep.Year, -1),
                "ArrowRight" => model.Move(active, CalendarStep.Year, 1),
                "ArrowUp" => model.Move(active, CalendarStep.Year, -4),
                "ArrowDown" => model.Move(active, CalendarStep.Year, 4),
                "Home" => model.Move(active, CalendarStep.Year, DecadeStart(active.Year) - active.Year),
                "End" => model.Move(active, CalendarStep.Year, DecadeStart(active.Year) + 9 - active.Year),
                "PageUp" => model.Move(active, CalendarStep.Year, -10),
                "PageDown" => model.Move(active, CalendarStep.Year, 10),
                _ => null,
            },
        };
        if (target is not { } date) return;
        e.Handled = true;
        var before = shown;
        Go(date);
        focusPending = true;
        StateHasChanged();
        if (before != shown) await ReportShownAsync();
    }
}
