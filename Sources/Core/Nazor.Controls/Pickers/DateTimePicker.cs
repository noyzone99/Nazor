using System.Globalization;
using Nazor.Controls.Primitives;
using Nazor.Rendering;

namespace Nazor.Controls;

/// <summary>
/// A date and time field: one <c>DateTimePicker</c> frame holding a <see cref="DatePicker" /> and a
/// <see cref="TimePicker" /> drawn without their own frames (<see cref="FieldAppearance.Plain" />), each with its editor
/// and popup. <see cref="Value" /> (<c>@bind-Value</c>) combines them: choosing a date without a time uses midnight
/// (or the earliest allowed time of that day); a time typed before the date is kept until a date is chosen; clearing
/// the date clears the value. <see cref="Minimum" /> and <see cref="Maximum" /> limit the dates and, on their first and
/// last days, the times. Inside a <see cref="Field" /> the date editor is the field's control and the time editor is
/// named <c>Time</c>.
/// </summary>
public sealed class DateTimePicker : Control
{
    private readonly ControllableState<DateTime?> value = new(null);
    private readonly string fallbackId = UniqueId.Create("datetime");
    private TimeOnly? pendingTime;

    /// <summary>Gets or sets the date and time, or null when empty.</summary>
    [Parameter] public DateTime? Value { get; set; }

    /// <summary>Gets or sets the callback invoked when the user changes the date or the time.</summary>
    [Parameter] public EventCallback<DateTime?> ValueChanged { get; set; }

    /// <summary>Gets or sets the culture of both editors; the current culture by default.</summary>
    [Parameter] public CultureInfo? Culture { get; set; }

    /// <summary>Gets or sets the .NET format of the date, <c>d</c> by default.</summary>
    [Parameter] public string DateFormat { get; set; } = "d";

    /// <summary>Gets or sets whether the clock has 24 hours; null follows the culture.</summary>
    [Parameter] public bool? Use24Hour { get; set; }

    /// <summary>Gets or sets the step of the minutes offered, from 1 to 60.</summary>
    [Parameter] public int MinuteIncrement { get; set; } = 1;

    /// <summary>Gets or sets whether the seconds are shown and offered.</summary>
    [Parameter] public bool ShowSeconds { get; set; }

    /// <summary>Gets or sets the earliest moment that can be chosen, or null for no limit.</summary>
    [Parameter] public DateTime? Minimum { get; set; }

    /// <summary>Gets or sets the latest moment that can be chosen, or null for no limit.</summary>
    [Parameter] public DateTime? Maximum { get; set; }

    /// <summary>Gets or sets the date marked as today in the calendar; null uses the computer's date.</summary>
    [Parameter] public DateOnly? Today { get; set; }

    /// <summary>Gets or sets whether the picker rejects interaction.</summary>
    [Parameter] public bool Disabled { get; set; }

    /// <summary>Gets or sets whether the value cannot change.</summary>
    [Parameter] public bool ReadOnly { get; set; }

    /// <summary>Gets or sets whether a value is required; validation is application-owned.</summary>
    [Parameter] public bool Required { get; set; }

    /// <summary>Gets or sets whether the date part has a clear button.</summary>
    [Parameter] public bool Clearable { get; set; }

    /// <summary>Gets or sets how the frame is drawn.</summary>
    [Parameter] public FieldAppearance Appearance { get; set; }

    /// <summary>Gets or sets the size step.</summary>
    [Parameter] public ControlSize Size { get; set; } = ControlSize.Medium;

    /// <summary>Gets or sets the enclosing field, supplied automatically.</summary>
    [CascadingParameter] public FieldContext? Field { get; set; }

    /// <summary>Gets the current value.</summary>
    public DateTime? CurrentValue => value.Value;

    /// <summary>Gets the NSS element name emitted by DateTimePicker.</summary>
    protected override string ElementName => "DateTimePicker";

    private bool IsInactive => Disabled || Field?.Disabled == true;

    /// <summary>Validates the parameters and synchronizes the value.</summary>
    protected override void OnParametersSet()
    {
        if (!Enum.IsDefined(Appearance)) throw new ArgumentOutOfRangeException(nameof(Appearance));
        if (!Enum.IsDefined(Size)) throw new ArgumentOutOfRangeException(nameof(Size));
        if (Minimum is { } min && Maximum is { } max && max < min) throw new ArgumentOutOfRangeException(nameof(Maximum));
        if (value.Sync(Value) && value.Value is not null) pendingTime = null;
    }

    /// <summary>Adds the frame states.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void AddAttributes(RenderTreeBuilder builder)
    {
        builder.AddAttribute(10, "role", "group");
        builder.AddAttribute(11, "appearance", Appearance.ToString().ToLowerInvariant());
        builder.AddAttribute(12, "size", Vocabulary.Name(Size));
        builder.AddAttribute(13, "disabled", IsInactive);
        builder.AddAttribute(14, "readonly", ReadOnly);
        builder.AddAttribute(15, "invalid", Field?.Invalid == true);
        builder.AddAttribute(16, "empty", value.Value is null);
        if (AccessibleName is null) Field?.ApplyTo(builder, 17, LabeledBy, DescribedBy);
    }

    /// <summary>Adds the date and time pickers.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void AddContent(RenderTreeBuilder builder)
    {
        var date = value.Value is { } current ? DateOnly.FromDateTime(current) : (DateOnly?)null;
        var time = value.Value is { } moment ? TimeOnly.FromDateTime(moment) : pendingTime;
        builder.OpenComponent<DatePicker>(1000);
        builder.AddComponentParameter(1001, nameof(DatePicker.Value), date);
        builder.AddComponentParameter(1002, nameof(DatePicker.ValueChanged), EventCallback.Factory.Create<DateOnly?>(this, DateChangedAsync));
        builder.AddComponentParameter(1003, nameof(DatePicker.Culture), Culture);
        builder.AddComponentParameter(1004, nameof(DatePicker.Format), DateFormat);
        builder.AddComponentParameter(1005, nameof(DatePicker.Minimum), Minimum is { } min ? DateOnly.FromDateTime(min) : null);
        builder.AddComponentParameter(1006, nameof(DatePicker.Maximum), Maximum is { } max ? DateOnly.FromDateTime(max) : null);
        builder.AddComponentParameter(1007, nameof(DatePicker.Today), Today);
        builder.AddComponentParameter(1008, nameof(DatePicker.Disabled), Disabled);
        builder.AddComponentParameter(1009, nameof(DatePicker.ReadOnly), ReadOnly);
        builder.AddComponentParameter(1010, nameof(DatePicker.Required), Required);
        builder.AddComponentParameter(1011, nameof(DatePicker.Clearable), Clearable);
        builder.AddComponentParameter(1012, nameof(DatePicker.Appearance), FieldAppearance.Plain);
        builder.AddComponentParameter(1013, nameof(DatePicker.Size), Size);
        builder.AddComponentParameter(1014, nameof(Control.AccessibleName), AccessibleName is null ? null : AccessibleName + ", " + ControlStrings.Get("Date"));
        builder.AddComponentParameter(1015, nameof(Control.Id), Id is null ? null : Id + "-date");
        builder.AddComponentParameter(1016, nameof(Control.LabeledBy), LabeledBy);
        builder.AddComponentParameter(1017, nameof(Control.DescribedBy), DescribedBy);
        builder.CloseComponent();
        builder.OpenComponent<TimePicker>(1100);
        builder.AddComponentParameter(1101, nameof(TimePicker.Value), time);
        builder.AddComponentParameter(1102, nameof(TimePicker.ValueChanged), EventCallback.Factory.Create<TimeOnly?>(this, TimeChangedAsync));
        builder.AddComponentParameter(1103, nameof(TimePicker.Culture), Culture);
        builder.AddComponentParameter(1104, nameof(TimePicker.Use24Hour), Use24Hour);
        builder.AddComponentParameter(1105, nameof(TimePicker.MinuteIncrement), MinuteIncrement);
        builder.AddComponentParameter(1106, nameof(TimePicker.ShowSeconds), ShowSeconds);
        builder.AddComponentParameter(1107, nameof(TimePicker.Minimum), Minimum is { } low && date == DateOnly.FromDateTime(low) ? TimeOnly.FromDateTime(low) : null);
        builder.AddComponentParameter(1108, nameof(TimePicker.Maximum), Maximum is { } high && date == DateOnly.FromDateTime(high) ? TimeOnly.FromDateTime(high) : null);
        builder.AddComponentParameter(1109, nameof(TimePicker.Disabled), Disabled);
        builder.AddComponentParameter(1110, nameof(TimePicker.ReadOnly), ReadOnly);
        builder.AddComponentParameter(1111, nameof(TimePicker.Appearance), FieldAppearance.Plain);
        builder.AddComponentParameter(1112, nameof(TimePicker.Size), Size);
        builder.AddComponentParameter(1113, nameof(Control.AccessibleName), AccessibleName is null ? ControlStrings.Get("Time") : AccessibleName + ", " + ControlStrings.Get("Time"));
        builder.AddComponentParameter(1114, nameof(Control.Id), (Id ?? Field?.Id ?? fallbackId) + "-time");
        builder.CloseComponent();
    }

    private TimeOnly EarliestTime(DateOnly date) => Minimum is { } min && DateOnly.FromDateTime(min) == date ? TimeOnly.FromDateTime(min) : TimeOnly.MinValue;

    private async Task DateChangedAsync(DateOnly? date)
    {
        var time = value.Value is { } current ? TimeOnly.FromDateTime(current) : pendingTime;
        DateTime? next = date is { } d ? d.ToDateTime(time is { } t && IsWithin(d.ToDateTime(t)) ? t : EarliestTime(d)) : null;
        if (date is null && value.Value is { } previous) pendingTime = TimeOnly.FromDateTime(previous);
        await ReportAsync(next);
    }

    private async Task TimeChangedAsync(TimeOnly? time)
    {
        if (value.Value is not { } current)
        {
            pendingTime = time;
            StateHasChanged();
            return;
        }
        var date = DateOnly.FromDateTime(current);
        await ReportAsync(date.ToDateTime(time ?? TimeOnly.MinValue));
    }

    private bool IsWithin(DateTime moment) => (Minimum is not { } min || moment >= min) && (Maximum is not { } max || moment <= max);

    private async Task ReportAsync(DateTime? next)
    {
        if (next is not null) pendingTime = null;
        if (!value.Set(next))
        {
            StateHasChanged();
            return;
        }
        StateHasChanged();
        await ValueChanged.InvokeAsync(next);
        await Events.RaiseChangeAsync(Element, next);
    }
}
