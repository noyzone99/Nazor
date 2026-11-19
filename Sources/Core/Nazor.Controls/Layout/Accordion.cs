using Nazor.Input;
using Nazor.Rendering;

namespace Nazor.Controls;

/// <summary>How many items of an <see cref="Accordion" /> can be expanded together.</summary>
public enum AccordionExpandMode
{
    /// <summary>Expanding an item collapses the others.</summary>
    Single,
    /// <summary>Items expand and collapse independently.</summary>
    Multiple,
}

/// <summary>
/// A stack of <see cref="AccordionItem" /> expanders that open one at a time (<see cref="ExpandMode" />) or
/// independently. <see cref="ExpandedValues" /> holds the <see cref="AccordionItem.Value" /> of the expanded items and
/// supports <c>@bind-ExpandedValues</c>; with <see cref="RequireOne" /> one item always stays expanded. Up and Down move
/// the focus between the item headers, Home and End to the first and last. The element is <c>Accordion</c>, a group
/// with the <c>expand-mode</c> and <c>disabled</c> attributes.
/// </summary>
public sealed class Accordion : ContentControl
{
    private readonly List<AccordionItem> items = [];
    private List<string> expanded = [];
    private IReadOnlyCollection<string>? lastParameter;
    private bool synchronized;

    /// <summary>Gets or sets the values of the expanded items.</summary>
    [Parameter] public IReadOnlyCollection<string>? ExpandedValues { get; set; }

    /// <summary>Gets or sets the callback invoked when the user expands or collapses items.</summary>
    [Parameter] public EventCallback<IReadOnlyCollection<string>> ExpandedValuesChanged { get; set; }

    /// <summary>Gets or sets how many items can be expanded together.</summary>
    [Parameter] public AccordionExpandMode ExpandMode { get; set; }

    /// <summary>
    /// Gets or sets whether one item always stays expanded: the expanded item cannot be collapsed by the user, and the
    /// first enabled item expands when none is.
    /// </summary>
    [Parameter] public bool RequireOne { get; set; }

    /// <summary>Gets or sets whether every item rejects interaction.</summary>
    [Parameter] public bool Disabled { get; set; }

    /// <summary>Gets the values of the expanded items in the order of the items.</summary>
    public IReadOnlyList<string> Expanded => [.. Ordered().Select(item => item.Value).Where(expanded.Contains)];

    /// <summary>Gets the NSS element name emitted by Accordion.</summary>
    protected override string ElementName => "Accordion";

    /// <summary>Validates the mode and adopts expanded values changed by the parent.</summary>
    protected override void OnParametersSet()
    {
        if (!Enum.IsDefined(ExpandMode)) throw new ArgumentOutOfRangeException(nameof(ExpandMode));
        if (!synchronized || !SameValues(ExpandedValues, lastParameter))
        {
            expanded = [.. (ExpandedValues ?? []).Distinct()];
            if (ExpandMode == AccordionExpandMode.Single && expanded.Count > 1) expanded = [expanded[0]];
        }
        else if (ExpandMode == AccordionExpandMode.Single && expanded.Count > 1) expanded = [expanded[0]];
        lastParameter = ExpandedValues;
        synchronized = true;
        Refresh();
    }

    private static bool SameValues(IReadOnlyCollection<string>? a, IReadOnlyCollection<string>? b) =>
        ReferenceEquals(a, b) || (a ?? []).SequenceEqual(b ?? []);

    /// <summary>Expands the first enabled item when one is required and none is expanded.</summary>
    /// <param name="firstRender">Whether this is the first render.</param>
    /// <returns>A task that completes when a required expansion has been reported.</returns>
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!RequireOne || Disabled || expanded.Any(value => items.Any(item => item.Value == value))) return;
        if (Ordered().FirstOrDefault(item => !item.Disabled) is not { } first) return;
        expanded = [first.Value];
        Refresh();
        await ExpandedValuesChanged.InvokeAsync(Expanded);
    }

    /// <summary>Adds the group role, the mode and the disabled state.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void AddAttributes(RenderTreeBuilder builder)
    {
        builder.AddAttribute(10, "role", "group");
        builder.AddAttribute(11, "expand-mode", ExpandMode == AccordionExpandMode.Multiple ? "multiple" : "single");
        builder.AddAttribute(12, "disabled", Disabled);
    }

    /// <summary>Supplies the accordion to its items.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void AddContent(RenderTreeBuilder builder)
    {
        builder.OpenComponent<CascadingValue<Accordion>>(1000);
        builder.AddComponentParameter(1001, "Value", this);
        builder.AddComponentParameter(1002, "IsFixed", true);
        builder.AddComponentParameter(1003, "ChildContent", ChildContent);
        builder.CloseComponent();
    }

    internal bool IsExpanded(string value) => expanded.Contains(value);

    internal void Register(AccordionItem item)
    {
        if (items.Any(other => other != item && other.Value == item.Value))
            throw new InvalidOperationException($"The accordion already has an item with the value '{item.Value}'.");
        if (!items.Contains(item)) items.Add(item);
    }

    internal void Unregister(AccordionItem item) => items.Remove(item);

    private AccordionItem[] Ordered()
    {
        if (Element.Element is not { } element) return [.. items];
        var nodes = ControlTree.Descendants(element).ToList();
        return [.. items.OrderBy(item => item.Element.Element is { } node ? nodes.IndexOf(node) : int.MaxValue)];
    }

    private void Refresh()
    {
        foreach (var item in items.ToArray()) item.Refresh();
    }

    /// <summary>Expands an item when collapsed and collapses it when expanded, as a click on its header does.</summary>
    /// <param name="value">The value of the item.</param>
    /// <returns>A task that completes when the change has been reported.</returns>
    public Task ToggleAsync(string value) => SetExpandedAsync(value, !expanded.Contains(value));

    /// <summary>Expands or collapses an item as the user does, keeping the mode and the required item, and reports the change.</summary>
    /// <param name="value">The value of the item.</param>
    /// <param name="expand">Whether the item should be expanded.</param>
    /// <returns>A task that completes when the change has been reported.</returns>
    public async Task SetExpandedAsync(string value, bool expand)
    {
        var item = items.FirstOrDefault(candidate => candidate.Value == value);
        if (Disabled || item is null || item.Disabled || Element.Element?.IsDisabled is true || expanded.Contains(value) == expand) return;
        var before = expanded;
        if (expand) expanded = ExpandMode == AccordionExpandMode.Single ? [value] : [.. expanded, value];
        else
        {
            if (RequireOne && expanded.Count(other => items.Any(candidate => candidate.Value == other)) <= 1) return;
            expanded = [.. expanded.Where(other => other != value)];
        }
        Refresh();
        await ExpandedValuesChanged.InvokeAsync(Expanded);
        foreach (var changed in Ordered().Where(candidate => before.Contains(candidate.Value) != expanded.Contains(candidate.Value)))
            await changed.ReportAsync(expanded.Contains(changed.Value));
    }

    internal async Task KeyDownAsync(AccordionItem item, KeyboardEventArgs e)
    {
        if (e.Handled || e.Modifiers != KeyModifiers.None || e.Key is not ("ArrowUp" or "ArrowDown" or "Home" or "End")) return;
        var enabled = Ordered().Where(candidate => !candidate.Disabled && !Disabled).ToArray();
        var index = Array.IndexOf(enabled, item);
        if (enabled.Length == 0 || index < 0) return;
        e.Handled = true;
        var next = e.Key switch
        {
            "Home" => 0,
            "End" => enabled.Length - 1,
            "ArrowDown" => (index + 1) % enabled.Length,
            _ => (index - 1 + enabled.Length) % enabled.Length,
        };
        if (next != index) await enabled[next].HeaderElement.FocusAsync();
    }
}

/// <summary>
/// An item of an <see cref="Accordion" />: an <see cref="Expander" /> whose expansion belongs to the accordion and is
/// identified by <see cref="Value" />. Its parts are named <c>AccordionItemHeader</c>, <c>AccordionItemChevron</c>,
/// <c>AccordionItemPanel</c> and so on. <see cref="Expander.IsExpanded" /> is ignored; <see cref="Expander.IsExpandedChanged" />
/// still reports the user's changes to this item.
/// </summary>
public sealed class AccordionItem : Expander, IDisposable
{
    private Accordion? registered;

    /// <summary>Gets or sets the value that identifies the item in <see cref="Accordion.ExpandedValues" />.</summary>
    [Parameter] public string Value { get; set; } = "";

    /// <summary>Gets or sets the enclosing accordion, supplied automatically.</summary>
    [CascadingParameter] public Accordion? Owner { get; set; }

    /// <summary>Gets whether the accordion expands this item.</summary>
    public override bool Expanded => Owner?.IsExpanded(Value) ?? false;

    /// <summary>Gets whether the item or its accordion rejects interaction.</summary>
    protected override bool IsInactive => Disabled || Owner?.Disabled == true;

    /// <summary>Gets the NSS element name emitted by AccordionItem.</summary>
    protected override string ElementName => "AccordionItem";

    /// <summary>Joins the enclosing accordion.</summary>
    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        if (Owner is null) throw new InvalidOperationException("AccordionItem must be inside an Accordion.");
        if (string.IsNullOrEmpty(Value)) throw new ArgumentException("An accordion item needs a value.", nameof(Value));
        if (registered == Owner) return;
        registered?.Unregister(this);
        (registered = Owner).Register(this);
    }

    /// <summary>Adds the arrow-key movement between headers.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void AddHeaderAttributes(RenderTreeBuilder builder) =>
        builder.AddAttribute(1008, "__internal_onkeydown#accordion", EventCallback.Factory.Create<KeyboardEventArgs>(this, e => Owner!.KeyDownAsync(this, e)));

    /// <summary>Expands the item through the accordion.</summary>
    /// <param name="value">Whether the item should be expanded.</param>
    /// <returns>A task that completes when the change has been reported.</returns>
    public override Task SetExpandedAsync(bool value) => IsInactive ? Task.CompletedTask : Owner!.SetExpandedAsync(Value, value);

    internal async Task ReportAsync(bool value)
    {
        await IsExpandedChanged.InvokeAsync(value);
        await Events.RaiseChangeAsync(Element, value);
    }

    /// <summary>Leaves the accordion.</summary>
    public void Dispose()
    {
        registered?.Unregister(this);
        registered = null;
    }
}
