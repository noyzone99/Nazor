using Nazor.Controls.Primitives;
using Nazor.Input;
using Nazor.Rendering;

namespace Nazor.Controls;

/// <summary>
/// A compact row of joined choices, such as Day, Week and Month above a calendar. With
/// <see cref="SelectionMode.Single" /> it is a <c>radiogroup</c> of <c>radio</c> segments: <see cref="Value" />
/// (<c>@bind-Value</c>) is the selected segment's value, the group is a single Tab stop and the arrow keys select the
/// next enabled segment. With <see cref="SelectionMode.Multiple" /> it is a <c>toolbar</c> of toggle segments:
/// <see cref="Values" /> (<c>@bind-Values</c>) lists the pressed ones and the arrow keys move between them. A
/// <c>SegmentedControlIndicator</c> part follows the selected segment (the <c>--nz-indicator-*</c> variables), so themes
/// can slide a highlight; <see cref="EqualWidth" /> gives every segment the same width (the <c>equal-width</c> attribute).
/// </summary>
/// <typeparam name="TValue">The type of the segment values.</typeparam>
[CascadingTypeParameter(nameof(TValue))]
public sealed class SegmentedControl<TValue> : ContentControl
{
    private readonly ControllableState<TValue?> value = new(default);
    private readonly ControllableState<IReadOnlyList<TValue>?> values = new(null);
    private readonly List<Segment<TValue>> segments = [];
    private IndicatorTracker<Segment<TValue>>? tracker;
    private bool membershipChanged;

    /// <summary>Gets or sets whether one segment (<see cref="SelectionMode.Single" />) or any number of segments
    /// (<see cref="SelectionMode.Multiple" />) can be selected.</summary>
    [Parameter] public SelectionMode SelectionMode { get; set; } = SelectionMode.Single;

    /// <summary>Gets or sets the value of the selected segment in single selection.</summary>
    [Parameter] public TValue? Value { get; set; }

    /// <summary>Gets or sets the callback invoked when the user selects another segment in single selection.</summary>
    [Parameter] public EventCallback<TValue> ValueChanged { get; set; }

    /// <summary>Gets or sets the values of the pressed segments in multiple selection.</summary>
    [Parameter] public IReadOnlyList<TValue>? Values { get; set; }

    /// <summary>Gets or sets the callback invoked with the new values when the user presses or releases a segment in
    /// multiple selection; the list follows the order of the segments.</summary>
    [Parameter] public EventCallback<IReadOnlyList<TValue>> ValuesChanged { get; set; }

    /// <summary>Gets or sets whether every segment takes the same width.</summary>
    [Parameter] public bool EqualWidth { get; set; }

    /// <summary>Gets or sets the size step.</summary>
    [Parameter] public ControlSize Size { get; set; } = ControlSize.Medium;

    /// <summary>Gets or sets whether every segment rejects interaction.</summary>
    [Parameter] public bool Disabled { get; set; }

    /// <summary>Gets the value of the selected segment in single selection.</summary>
    public TValue? SelectedValue => value.Value;

    /// <summary>Gets the values of the pressed segments in multiple selection, in segment order.</summary>
    public IReadOnlyList<TValue> SelectedValues => values.Value ?? [];

    /// <summary>Gets the NSS element name emitted by SegmentedControl.</summary>
    protected override string ElementName => "SegmentedControl";

    internal bool IsMultiple => SelectionMode == SelectionMode.Multiple;

    /// <summary>Validates the parameters and synchronizes the selection.</summary>
    protected override void OnParametersSet()
    {
        if (SelectionMode is not (SelectionMode.Single or SelectionMode.Multiple)) throw new ArgumentOutOfRangeException(nameof(SelectionMode));
        if (!Enum.IsDefined(Size)) throw new ArgumentOutOfRangeException(nameof(Size));
        tracker ??= new IndicatorTracker<Segment<TValue>>(StateHasChanged);
        value.Sync(Value);
        values.Sync(Values);
        membershipChanged |= segments.Count > 0;
    }

    /// <summary>Adds the role and the look attributes.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void AddAttributes(RenderTreeBuilder builder)
    {
        builder.AddAttribute(10, "role", IsMultiple ? "toolbar" : "radiogroup");
        builder.AddAttribute(11, "disabled", Disabled);
        builder.AddAttribute(12, "size", Vocabulary.Name(Size));
        builder.AddAttribute(13, "equal-width", EqualWidth);
        builder.AddAttribute(14, "selection-mode", IsMultiple ? "multiple" : "single");
        builder.AddAttribute(15, "orientation", "horizontal");
    }

    /// <summary>Adds the segments and the indicator.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void AddContent(RenderTreeBuilder builder)
    {
        builder.OpenComponent<CascadingValue<SegmentedControl<TValue>>>(1000);
        builder.AddComponentParameter(1001, "Value", this);
        builder.AddComponentParameter(1002, "IsFixed", true);
        builder.AddComponentParameter(1003, "ChildContent", ChildContent);
        builder.CloseComponent();
        var selected = IsMultiple ? null : segments.FirstOrDefault(segment => IsSelected(segment.Value));
        builder.OpenComponent<Indicator>(1010);
        builder.AddComponentParameter(1011, nameof(Indicator.As), "SegmentedControlIndicator");
        builder.AddComponentParameter(1012, nameof(Indicator.Target), selected is null ? null : tracker!.BoxOf(selected));
        builder.CloseComponent();
    }

    /// <summary>Refreshes the segments after the set of segments or the selection changed.</summary>
    /// <param name="firstRender">Whether this is the first render.</param>
    protected override void OnAfterRender(bool firstRender)
    {
        if (!membershipChanged) return;
        membershipChanged = false;
        Refresh();
    }

    internal bool IsSelected(TValue candidate) => IsMultiple
        ? SelectedValues.Contains(candidate, EqualityComparer<TValue>.Default)
        : EqualityComparer<TValue?>.Default.Equals(value.Value, candidate);

    internal KeyValuePair<string, object?> IndicatorAttribute(Segment<TValue> segment) => tracker!.Attributes(this, segment);

    internal List<Segment<TValue>> Ordered()
    {
        if (Element.Element is not { } group) return [.. segments];
        var order = ControlTree.Descendants(group).Select((node, index) => (node, index)).ToDictionary(pair => pair.node, pair => pair.index);
        return [.. segments.OrderBy(segment => segment.Element.Element is { } node && order.TryGetValue(node, out var index) ? index : int.MaxValue)];
    }

    internal bool IsTabStop(Segment<TValue> segment)
    {
        var ordered = Ordered();
        var stop = ordered.FirstOrDefault(s => !s.IsInactive && IsSelected(s.Value)) ?? ordered.FirstOrDefault(s => !s.IsInactive);
        return stop == segment;
    }

    internal (int Position, int Count) PositionOf(Segment<TValue> segment)
    {
        var ordered = Ordered();
        var index = ordered.IndexOf(segment);
        return (index < 0 ? ordered.Count : index + 1, ordered.Count);
    }

    internal void Register(Segment<TValue> segment)
    {
        if (segments.Contains(segment)) return;
        segments.Add(segment);
        membershipChanged = true;
    }

    internal void Unregister(Segment<TValue> segment)
    {
        if (!segments.Remove(segment)) return;
        tracker?.Forget(segment);
        Refresh();
        StateHasChanged();
    }

    private void Refresh()
    {
        foreach (var segment in segments.ToArray()) segment.Refresh();
    }

    /// <summary>Activates the segment with a value as a user action does: selects it in single selection, presses or
    /// releases it in multiple selection, and reports the change.</summary>
    /// <param name="candidate">The value of the segment.</param>
    /// <returns>A task that completes when the change has been reported.</returns>
    public async Task ActivateAsync(TValue candidate)
    {
        if (Disabled || Element.Element?.IsDisabled is true) return;
        if (IsMultiple)
        {
            var pressed = IsSelected(candidate);
            var next = Ordered().Select(s => s.Value).Where(v => EqualityComparer<TValue>.Default.Equals(v, candidate) ? !pressed : IsSelected(v)).ToList();
            values.Set(next);
            StateHasChanged();
            Refresh();
            await ValuesChanged.InvokeAsync(next);
            await Events.RaiseChangeAsync(Element, next);
            return;
        }
        if (!value.Set(candidate)) return;
        StateHasChanged();
        Refresh();
        await ValueChanged.InvokeAsync(candidate);
        await Events.RaiseChangeAsync(Element, candidate);
    }
}

/// <summary>
/// One segment of a <see cref="SegmentedControl{TValue}" />: a <c>radio</c> in single selection or a
/// <c>togglebutton</c> in multiple selection. The element is <c>Segment</c> with the <c>selected</c> attribute (and
/// <c>checked</c> or <c>pressed</c>), <c>position</c> (first, middle, last) and <c>icon-only</c> when it has no label;
/// the parts are <c>SegmentLeading</c> and <c>SegmentLabel</c>.
/// </summary>
/// <typeparam name="TValue">The type of the value, the same as the control's.</typeparam>
public sealed class Segment<TValue> : ContentControl, IDisposable
{
    private SegmentedControl<TValue>? registered;

    /// <summary>Gets or sets the value this segment stands for.</summary>
    [Parameter] public TValue Value { get; set; } = default!;

    /// <summary>Gets or sets content shown before the label, such as an icon; give icon-only segments an
    /// <see cref="Control.AccessibleName" />.</summary>
    [Parameter] public RenderFragment? Leading { get; set; }

    /// <summary>Gets or sets whether this segment rejects interaction; the arrow keys skip it.</summary>
    [Parameter] public bool Disabled { get; set; }

    /// <summary>Gets or sets the enclosing control, supplied automatically.</summary>
    [CascadingParameter] public SegmentedControl<TValue>? Owner { get; set; }

    /// <summary>Gets whether this segment is selected or pressed.</summary>
    public bool IsSelected => Owner?.IsSelected(Value) == true;

    internal bool IsInactive => Disabled || Owner?.Disabled == true;

    /// <summary>Gets the NSS element name emitted by Segment.</summary>
    protected override string ElementName => "Segment";

    /// <summary>Joins the enclosing control.</summary>
    protected override void OnParametersSet()
    {
        if (Owner is null) throw new InvalidOperationException("Segment must be inside a SegmentedControl of the same value type.");
        if (registered == Owner) return;
        registered?.Unregister(this);
        (registered = Owner).Register(this);
    }

    /// <summary>Adds the role, the states, the position and the activation.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void AddAttributes(RenderTreeBuilder builder)
    {
        var owner = Owner!;
        var (position, count) = owner.PositionOf(this);
        var selected = IsSelected;
        builder.AddAttribute(10, "role", owner.IsMultiple ? "togglebutton" : "radio");
        builder.AddAttribute(11, "selected", selected);
        builder.AddAttribute(12, owner.IsMultiple ? "pressed" : "checked", selected);
        builder.AddAttribute(13, "disabled", IsInactive);
        builder.AddAttribute(14, "focusable", !IsInactive);
        if (TabIndex is null && !owner.IsMultiple) builder.AddAttribute(15, "tabindex", owner.IsTabStop(this) ? 0 : -1);
        builder.AddAttribute(16, "position", Vocabulary.Name(Vocabulary.Position(position - 1, count)));
        builder.AddAttribute(17, "position-in-set", position);
        builder.AddAttribute(18, "size-of-set", count);
        builder.AddAttribute(19, "size", Vocabulary.Name(owner.Size));
        builder.AddAttribute(20, "icon-only", ChildContent is null && Leading is not null);
        builder.AddAttribute(21, "__default_onclick", EventCallback.Factory.Create<PointerEventArgs>(this, ActivateAsync));
        var indicator = owner.IndicatorAttribute(this);
        builder.AddAttribute(22, indicator.Key, indicator.Value);
    }

    /// <summary>Adds the leading content and the label.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void AddContent(RenderTreeBuilder builder)
    {
        if (Leading is not null)
        {
            builder.OpenElement(1000, "SegmentLeading");
            builder.AddContent(1001, Leading);
            builder.CloseElement();
        }
        if (ChildContent is not null)
        {
            builder.OpenElement(1010, "SegmentLabel");
            builder.AddContent(1011, ChildContent);
            builder.CloseElement();
        }
    }

    internal void Refresh() => StateHasChanged();

    private Task ActivateAsync() => IsInactive || Owner is null ? Task.CompletedTask : Owner.ActivateAsync(Value);

    /// <summary>Leaves the enclosing control.</summary>
    public void Dispose()
    {
        registered?.Unregister(this);
        registered = null;
    }
}
