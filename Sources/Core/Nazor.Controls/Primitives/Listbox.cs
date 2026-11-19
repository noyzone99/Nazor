using Nazor.Input;
using Nazor.Rendering;

namespace Nazor.Controls.Primitives;

/// <summary>
/// A list of options without built-in look: one Tab stop, arrows and typing move between options, Space, Enter or a
/// click select, Shift and Ctrl extend a multiple selection. <see cref="Value" /> (<c>@bind-Value</c>) holds a single
/// selection and <see cref="Values" /> (<c>@bind-Values</c>) a multiple one. Fill it with
/// <see cref="ListboxOption{TValue}" />. The element is named by <see cref="As" />; options carry <c>selected</c> and
/// <c>current</c>. Drop-down lists, pickers and custom choice lists are built on it.
/// </summary>
/// <typeparam name="TValue">The type of the option values.</typeparam>
[CascadingTypeParameter(nameof(TValue))]
public sealed class Listbox<TValue> : ContentControl where TValue : notnull
{
    private readonly List<ListboxOption<TValue>> options = [];
    private readonly RovingFocusController keys = new() { PageSize = 10 };
    private SelectionModel<TValue> selection = new(SelectionMode.Single);
    private TValue? lastValue;
    private IReadOnlyCollection<TValue>? lastValues;
    private bool started;
    private int current = -1;

    /// <summary>Gets or sets the element name, <c>Listbox</c> by default.</summary>
    [Parameter] public string As { get; set; } = "Listbox";

    /// <summary>Gets or sets how many options can be selected.</summary>
    [Parameter] public SelectionMode SelectionMode { get; set; } = SelectionMode.Single;

    /// <summary>Gets or sets the selected value in single selection.</summary>
    [Parameter] public TValue? Value { get; set; }

    /// <summary>Gets or sets the callback invoked when the user selects another value in single selection.</summary>
    [Parameter] public EventCallback<TValue> ValueChanged { get; set; }

    /// <summary>Gets or sets the selected values in multiple selection.</summary>
    [Parameter] public IReadOnlyCollection<TValue>? Values { get; set; }

    /// <summary>Gets or sets the callback invoked when the user changes the multiple selection.</summary>
    [Parameter] public EventCallback<IReadOnlyCollection<TValue>> ValuesChanged { get; set; }

    /// <summary>Gets or sets whether moving with the arrows also selects, in single selection.</summary>
    [Parameter] public bool SelectionFollowsFocus { get; set; }

    /// <summary>Gets or sets the callback invoked when an option is activated with Enter or a double click.</summary>
    [Parameter] public EventCallback<TValue> OnActivate { get; set; }

    /// <summary>Gets or sets the layout direction of the options.</summary>
    [Parameter] public Orientation Orientation { get; set; } = Orientation.Vertical;

    /// <summary>Gets or sets whether the list rejects interaction.</summary>
    [Parameter] public bool Disabled { get; set; }

    /// <summary>Gets the selection.</summary>
    public SelectionModel<TValue> Selection => selection;

    /// <summary>Gets the element name.</summary>
    protected override string ElementName => As;

    /// <summary>Synchronizes the selection with the parameters.</summary>
    protected override void OnParametersSet()
    {
        if (selection.Mode != SelectionMode) selection = new SelectionModel<TValue>(SelectionMode);
        keys.Orientation = Orientation;
        if (SelectionMode == SelectionMode.Multiple)
        {
            if (!started || !ReferenceEquals(Values, lastValues)) selection.Replace(Values ?? []);
            lastValues = Values;
        }
        else
        {
            if (!started || !EqualityComparer<TValue?>.Default.Equals(Value, lastValue)) selection.Replace(Value is { } v ? [v] : []);
            lastValue = Value;
        }
        started = true;
    }

    /// <summary>Adds the list role and the keyboard handling.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void AddAttributes(RenderTreeBuilder builder)
    {
        builder.AddAttribute(10, "role", "listbox");
        builder.AddAttribute(11, "multiselectable", SelectionMode == SelectionMode.Multiple);
        builder.AddAttribute(12, "disabled", Disabled);
        builder.AddAttribute(13, "orientation", Vocabulary.Name(Orientation));
        builder.AddAttribute(14, "__internal_onkeydown#listbox", EventCallback.Factory.Create<KeyboardEventArgs>(this, KeyDownAsync));
    }

    /// <summary>Supplies the list to its options.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void AddContent(RenderTreeBuilder builder)
    {
        builder.OpenComponent<CascadingValue<Listbox<TValue>>>(1000);
        builder.AddComponentParameter(1001, "Value", this);
        builder.AddComponentParameter(1002, "IsFixed", true);
        builder.AddComponentParameter(1003, "ChildContent", ChildContent);
        builder.CloseComponent();
    }

    /// <summary>Refreshes the options once laid out, so the Tab stop is right.</summary>
    /// <param name="firstRender">Whether this is the first render.</param>
    protected override void OnAfterRender(bool firstRender)
    {
        if (firstRender) Refresh();
    }

    private List<ListboxOption<TValue>> Ordered() =>
        [.. options.OrderBy(option => option.Order)];

    internal void Register(ListboxOption<TValue> option)
    {
        options.Add(option);
        selection.SetOrder(Ordered().Select(o => o.Value));
    }

    internal void Unregister(ListboxOption<TValue> option)
    {
        if (!options.Remove(option)) return;
        selection.SetOrder(Ordered().Select(o => o.Value));
        Refresh();
    }

    internal bool IsSelected(TValue value) => selection.IsSelected(value);

    internal bool IsCurrent(ListboxOption<TValue> option) => Ordered().IndexOf(option) == current;

    internal bool IsTabStop(ListboxOption<TValue> option)
    {
        var ordered = Ordered();
        if (current < 0 || current >= ordered.Count)
        {
            var chosen = ordered.FindIndex(o => selection.IsSelected(o.Value) && !o.IsDisabled);
            keys.ActiveIndex = chosen;
        }
        else keys.ActiveIndex = current;
        var stop = keys.TabStop(ordered.Count, i => ordered[i].IsDisabled);
        return stop >= 0 && ordered[stop] == option;
    }

    internal void Focused(ListboxOption<TValue> option)
    {
        var index = Ordered().IndexOf(option);
        if (index == current) return;
        current = index;
        Refresh();
    }

    private void Refresh()
    {
        foreach (var option in options) option.Refresh();
    }

    internal async Task ClickAsync(ListboxOption<TValue> option, KeyModifiers modifiers)
    {
        if (Disabled || option.IsDisabled) return;
        current = Ordered().IndexOf(option);
        var changed = (modifiers & KeyModifiers.Shift) != 0 ? selection.Extend(option.Value, (modifiers & KeyModifiers.Control) != 0)
            : (modifiers & KeyModifiers.Control) != 0 ? selection.Toggle(option.Value)
            : SelectionMode == SelectionMode.Multiple ? selection.Select(option.Value) : selection.Select(option.Value);
        Refresh();
        if (changed) await NotifyAsync();
    }

    internal Task ActivateAsync(ListboxOption<TValue> option) =>
        Disabled || option.IsDisabled ? Task.CompletedTask : OnActivate.InvokeAsync(option.Value);

    private async Task KeyDownAsync(KeyboardEventArgs e)
    {
        if (e.Handled || Disabled) return;
        var ordered = Ordered();
        if (ordered.Count == 0) return;
        var focused = ordered.FindIndex(o => o.HasFocus);
        if (focused < 0) focused = current;
        if (e.Key is " " or "Enter" && focused >= 0)
        {
            e.Handled = true;
            var option = ordered[focused];
            if (e.Key == "Enter" && OnActivate.HasDelegate)
            {
                if (SelectionMode == SelectionMode.Single && selection.Select(option.Value)) await NotifyAsync();
                await ActivateAsync(option);
                return;
            }
            await ClickAsync(option, e.Modifiers & (KeyModifiers.Shift | KeyModifiers.Control));
            return;
        }
        if (e.Key is "a" or "A" && (e.Modifiers & KeyModifiers.Control) != 0 && SelectionMode == SelectionMode.Multiple)
        {
            e.Handled = true;
            if (selection.SelectAll()) await NotifyAsync();
            Refresh();
            return;
        }
        var target = keys.Move(e, focused, ordered.Count, i => ordered[i].IsDisabled, i => ordered[i].TypeaheadText);
        if (target is not { } index) return;
        e.Handled = true;
        current = index;
        var next = ordered[index];
        var changed = false;
        if ((e.Modifiers & KeyModifiers.Shift) != 0 && SelectionMode == SelectionMode.Multiple) changed = selection.Extend(next.Value);
        else if (SelectionMode == SelectionMode.Single && SelectionFollowsFocus) changed = selection.Select(next.Value);
        Refresh();
        await next.FocusAsync();
        if (changed) await NotifyAsync();
    }

    private async Task NotifyAsync()
    {
        StateHasChanged();
        if (SelectionMode == SelectionMode.Multiple)
        {
            lastValues = selection.SelectedKeys;
            await ValuesChanged.InvokeAsync(selection.SelectedKeys);
        }
        else if (selection.SelectedKey is { } value)
        {
            lastValue = value;
            await ValueChanged.InvokeAsync(value);
        }
        await Events.RaiseChangeAsync(Element, SelectionMode == SelectionMode.Multiple ? selection.SelectedKeys : selection.SelectedKey);
    }
}

/// <summary>
/// An option of a <see cref="Listbox{TValue}" />. The element is named by <see cref="As" /> (<c>ListboxOption</c> by
/// default) and carries <c>selected</c>, <c>current</c> and <c>disabled</c>.
/// </summary>
/// <typeparam name="TValue">The type of the option values.</typeparam>
public sealed class ListboxOption<TValue> : ContentControl, IDisposable where TValue : notnull
{
    private static long sequence;
    private Listbox<TValue>? registered;

    /// <summary>Gets or sets the element name, <c>ListboxOption</c> by default.</summary>
    [Parameter] public string As { get; set; } = "ListboxOption";

    /// <summary>Gets or sets the value selected by this option.</summary>
    [Parameter, EditorRequired] public TValue Value { get; set; } = default!;

    /// <summary>Gets or sets whether the option is skipped and cannot be selected.</summary>
    [Parameter] public bool Disabled { get; set; }

    /// <summary>Gets or sets the text matched by typing; the content's text is used when null.</summary>
    [Parameter] public string? TextValue { get; set; }

    /// <summary>Gets or sets the enclosing list, supplied automatically.</summary>
    [CascadingParameter] public Listbox<TValue>? Owner { get; set; }

    internal long Order { get; } = Interlocked.Increment(ref sequence);

    internal bool IsDisabled => Disabled || Owner?.Disabled == true;

    internal bool HasFocus => Element.Element is { } element && (element.States & ElementStates.Focus) != 0;

    internal string? TypeaheadText => TextValue ?? (Element.Element is { } element ? ControlTree.Text(element) : null);

    /// <summary>Gets the element name.</summary>
    protected override string ElementName => As;

    /// <summary>Joins the enclosing list.</summary>
    protected override void OnParametersSet()
    {
        if (Owner is null) throw new InvalidOperationException("ListboxOption must be inside a Listbox of the same value type.");
        if (registered == Owner) return;
        registered?.Unregister(this);
        (registered = Owner).Register(this);
    }

    /// <summary>Adds the option role, its states and its activation.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void AddAttributes(RenderTreeBuilder builder)
    {
        var owner = Owner!;
        builder.AddAttribute(10, "role", "option");
        builder.AddAttribute(11, "selected", owner.IsSelected(Value));
        builder.AddAttribute(12, "current", owner.IsCurrent(this));
        builder.AddAttribute(13, "disabled", IsDisabled);
        builder.AddAttribute(14, "focusable", !IsDisabled);
        if (TabIndex is null) builder.AddAttribute(15, "tabindex", owner.IsTabStop(this) ? 0 : -1);
        builder.AddAttribute(16, "__default_onclick", EventCallback.Factory.Create<PointerEventArgs>(this, e => owner.ClickAsync(this, e.IsSynthetic ? KeyModifiers.None : e.Modifiers)));
        builder.AddAttribute(17, "__internal_ondblclick", EventCallback.Factory.Create<PointerEventArgs>(this, () => owner.ActivateAsync(this)));
        builder.AddAttribute(18, "__internal_onfocus#listbox", EventCallback.Factory.Create<FocusEventArgs>(this, () => owner.Focused(this)));
    }

    internal void Refresh() => StateHasChanged();

    internal Task FocusAsync() => Element.FocusAsync();

    /// <summary>Leaves the enclosing list.</summary>
    public void Dispose()
    {
        registered?.Unregister(this);
        registered = null;
    }
}
