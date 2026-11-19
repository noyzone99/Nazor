using Nazor.Controls.Primitives;
using Nazor.Input;
using Nazor.Rendering;

namespace Nazor.Controls;

/// <summary>
/// A set of mutually exclusive choices: <see cref="Value" /> (<c>@bind-Value</c>) is the value of the checked
/// <see cref="RadioButton{TValue}" />. The group is a single Tab stop, on the checked radio button or on the first enabled
/// one; the arrow keys move to the next enabled radio button and check it. Inside a <see cref="Field" /> the group is the
/// field's control: the label names it and the field's required, invalid and disabled states apply. The element is
/// <c>RadioGroup</c> with the <c>radiogroup</c> role and the <c>orientation</c> and <c>size</c> attributes.
/// </summary>
/// <typeparam name="TValue">The type of the values of the radio buttons.</typeparam>
[CascadingTypeParameter(nameof(TValue))]
public sealed class RadioGroup<TValue> : ContentControl
{
    private readonly ControllableState<TValue?> value = new(default);
    private readonly List<RadioButton<TValue>> radios = [];
    private bool membershipChanged;

    /// <summary>Gets or sets the value of the checked radio button; a value no radio button has checks none.</summary>
    [Parameter] public TValue? Value { get; set; }

    /// <summary>Gets or sets the callback invoked when the user checks another radio button.</summary>
    [Parameter] public EventCallback<TValue> ValueChanged { get; set; }

    /// <summary>Gets or sets whether every radio button of the group rejects interaction.</summary>
    [Parameter] public bool Disabled { get; set; }

    /// <summary>Gets or sets the direction in which the radio buttons are laid out; vertical by default.</summary>
    [Parameter] public Orientation Orientation { get; set; } = Orientation.Vertical;

    /// <summary>Gets or sets the size step of the radio buttons.</summary>
    [Parameter] public ControlSize Size { get; set; } = ControlSize.Medium;

    /// <summary>Gets or sets the enclosing field, supplied automatically.</summary>
    [CascadingParameter] public FieldContext? Field { get; set; }

    /// <summary>Gets the value of the checked radio button.</summary>
    public TValue? SelectedValue => value.Value;

    /// <summary>Gets the NSS element name emitted by RadioGroup.</summary>
    protected override string ElementName => "RadioGroup";

    internal bool IsInactive => Disabled || Field?.Disabled == true;

    /// <summary>Validates the parameters and synchronizes the checked value.</summary>
    protected override void OnParametersSet()
    {
        if (!Enum.IsDefined(Orientation)) throw new ArgumentOutOfRangeException(nameof(Orientation));
        if (!Enum.IsDefined(Size)) throw new ArgumentOutOfRangeException(nameof(Size));
        if (value.Sync(Value)) membershipChanged = true;
        else membershipChanged |= radios.Count > 0;
    }

    /// <summary>Adds the role, the layout attributes and the field relationships.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void AddAttributes(RenderTreeBuilder builder)
    {
        builder.AddAttribute(10, "role", "radiogroup");
        builder.AddAttribute(11, "disabled", IsInactive);
        builder.AddAttribute(12, "orientation", Vocabulary.Name(Orientation));
        builder.AddAttribute(13, "size", Vocabulary.Name(Size));
        if (Id is null && Field is { } field) builder.AddAttribute(14, "id", field.ControlId);
        builder.AddAttribute(15, "required", Field?.Required == true);
        builder.AddAttribute(16, "invalid", Field?.Invalid == true);
        Field?.ApplyTo(builder, 17, LabeledBy, DescribedBy);
    }

    /// <summary>Supplies the group to its radio buttons.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void AddContent(RenderTreeBuilder builder)
    {
        builder.OpenComponent<CascadingValue<RadioGroup<TValue>>>(1000);
        builder.AddComponentParameter(1001, "Value", this);
        builder.AddComponentParameter(1002, "IsFixed", true);
        builder.AddComponentParameter(1003, "ChildContent", ChildContent);
        builder.CloseComponent();
    }

    /// <summary>Refreshes the radio buttons after the set of radio buttons or the checked value changed.</summary>
    /// <param name="firstRender">Whether this is the first render.</param>
    protected override void OnAfterRender(bool firstRender)
    {
        if (!membershipChanged) return;
        membershipChanged = false;
        Refresh();
    }

    internal bool IsChecked(TValue candidate) => EqualityComparer<TValue?>.Default.Equals(value.Value, candidate);

    internal List<RadioButton<TValue>> Ordered()
    {
        if (Element.Element is not { } group) return [.. radios];
        var order = ControlTree.Descendants(group).Select((node, index) => (node, index)).ToDictionary(pair => pair.node, pair => pair.index);
        return [.. radios.OrderBy(radio => radio.Element.Element is { } node && order.TryGetValue(node, out var index) ? index : int.MaxValue)];
    }

    internal bool IsTabStop(RadioButton<TValue> radio)
    {
        var ordered = Ordered();
        var stop = ordered.FirstOrDefault(r => !r.IsInactive && IsChecked(r.Value)) ?? ordered.FirstOrDefault(r => !r.IsInactive);
        return stop == radio;
    }

    internal (int Position, int Count) PositionOf(RadioButton<TValue> radio)
    {
        var ordered = Ordered();
        var index = ordered.IndexOf(radio);
        return (index < 0 ? ordered.Count : index + 1, ordered.Count);
    }

    internal void Register(RadioButton<TValue> radio)
    {
        if (radios.Contains(radio)) return;
        radios.Add(radio);
        membershipChanged = true;
    }

    internal void Unregister(RadioButton<TValue> radio)
    {
        if (radios.Remove(radio)) Refresh();
    }

    private void Refresh()
    {
        foreach (var radio in radios.ToArray()) radio.Refresh();
    }

    /// <summary>Checks the radio button with a value as a user action does and reports the change.</summary>
    /// <param name="candidate">The value to check.</param>
    /// <returns>A task that completes when the change has been reported.</returns>
    public async Task SelectAsync(TValue candidate)
    {
        if (IsInactive || Element.Element?.IsDisabled is true) return;
        if (!value.Set(candidate)) return;
        StateHasChanged();
        Refresh();
        await ValueChanged.InvokeAsync(candidate);
        await Events.RaiseChangeAsync(Element, candidate);
    }
}

/// <summary>
/// One choice of a <see cref="RadioGroup{TValue}" />: checked when its <see cref="Value" /> is the group's value; a click,
/// Space or an arrow key that reaches it checks it. The parts are <c>RadioButtonMark</c> with its
/// <c>RadioButtonDot</c>, <c>RadioButtonLabel</c> and <c>RadioButtonDescription</c>; the element has the <c>radio</c>
/// role, the <c>checked</c> state, <c>position-in-set</c> and <c>size-of-set</c>.
/// </summary>
/// <typeparam name="TValue">The type of the value, the same as the group's.</typeparam>
public sealed class RadioButton<TValue> : ContentControl, IDisposable
{
    private RadioGroup<TValue>? registered;

    /// <summary>Gets or sets the value the group takes when this radio button is checked.</summary>
    [Parameter] public TValue Value { get; set; } = default!;

    /// <summary>Gets or sets a secondary line shown below the label.</summary>
    [Parameter] public RenderFragment? Description { get; set; }

    /// <summary>Gets or sets whether this radio button rejects interaction; the arrow keys skip it.</summary>
    [Parameter] public bool Disabled { get; set; }

    /// <summary>Gets or sets the enclosing group, supplied automatically.</summary>
    [CascadingParameter] public RadioGroup<TValue>? Group { get; set; }

    /// <summary>Gets whether this radio button is checked.</summary>
    public bool IsChecked => Group?.IsChecked(Value) == true;

    internal bool IsInactive => Disabled || Group?.IsInactive == true;

    /// <summary>Gets the NSS element name emitted by RadioButton.</summary>
    protected override string ElementName => "RadioButton";

    /// <summary>Joins the enclosing group.</summary>
    protected override void OnParametersSet()
    {
        if (Group is null) throw new InvalidOperationException("RadioButton must be inside a RadioGroup of the same value type.");
        if (registered == Group) return;
        registered?.Unregister(this);
        (registered = Group).Register(this);
    }

    /// <summary>Adds the role, the states, the Tab stop and the activation.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void AddAttributes(RenderTreeBuilder builder)
    {
        var (position, count) = Group!.PositionOf(this);
        builder.AddAttribute(10, "role", "radio");
        builder.AddAttribute(11, "checked", IsChecked);
        builder.AddAttribute(12, "disabled", IsInactive);
        builder.AddAttribute(13, "focusable", !IsInactive);
        if (TabIndex is null) builder.AddAttribute(14, "tabindex", Group.IsTabStop(this) ? 0 : -1);
        builder.AddAttribute(15, "size", Vocabulary.Name(Group.Size));
        builder.AddAttribute(16, "position-in-set", position);
        builder.AddAttribute(17, "size-of-set", count);
        builder.AddAttribute(18, "__default_onclick", EventCallback.Factory.Create<PointerEventArgs>(this, SelectAsync));
    }

    /// <summary>Adds the mark, the label and the description.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void AddContent(RenderTreeBuilder builder)
    {
        builder.OpenElement(1000, "RadioButtonMark");
        builder.OpenElement(1001, "RadioButtonDot");
        builder.CloseElement();
        builder.CloseElement();
        if (ChildContent is not null)
        {
            builder.OpenElement(1010, "RadioButtonLabel");
            builder.AddContent(1011, ChildContent);
            builder.CloseElement();
        }
        if (Description is not null)
        {
            builder.OpenElement(1020, "RadioButtonDescription");
            builder.AddContent(1021, Description);
            builder.CloseElement();
        }
    }

    internal void Refresh() => StateHasChanged();

    private Task SelectAsync() => IsInactive || Group is null ? Task.CompletedTask : Group.SelectAsync(Value);

    /// <summary>Leaves the group.</summary>
    public void Dispose()
    {
        registered?.Unregister(this);
        registered = null;
    }
}
