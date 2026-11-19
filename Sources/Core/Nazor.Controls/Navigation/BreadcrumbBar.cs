using Nazor.Controls.Primitives;
using Nazor.Input;
using Nazor.Rendering;

namespace Nazor.Controls;

/// <summary>Describes a requested navigation to one node of a breadcrumb path.</summary>
/// <typeparam name="TItem">The path node type.</typeparam>
/// <param name="item">The application-owned node.</param>
/// <param name="index">Its zero-based position in the current path.</param>
public sealed class BreadcrumbItemInvokedEventArgs<TItem>(TItem item, int index) : EventArgs
{
    /// <summary>Gets the application-owned node.</summary>
    public TItem Item { get; } = item;

    /// <summary>Gets its zero-based position in the path at invocation.</summary>
    public int Index { get; } = index;
}

/// <summary>
/// Shows an application-owned path, keeping its trailing nodes visible and moving leading nodes into a menu when
/// space is limited. Arrow keys, Home and End move within one header Tab stop; invoking a node reports its item and
/// index without changing the path or navigating implicitly. The final node is marked as the current location.
/// Parts are BreadcrumbSlot, BreadcrumbItemContent and BreadcrumbSeparator, with Button and MenuButton actions.
/// </summary>
/// <typeparam name="TItem">The path node type.</typeparam>
public sealed class BreadcrumbBar<TItem> : Control
{
    private readonly RovingFocusController focus = new() { Orientation = Orientation.Horizontal };
    private readonly Dictionary<object, double> widths = [];
    private readonly Dictionary<object, Button> buttons = [];
    private object[] keys = [];
    private object? activeKey;
    private MenuButton? overflowButton;
    private double available = double.PositiveInfinity, overflowWidth = 32;
    private int hidden;
    private bool focusOverflow, repairFocus;

    /// <summary>Gets or sets the complete path in ancestor-to-current order; the last item is the current location.</summary>
    [Parameter] public IReadOnlyList<TItem> Items { get; set; } = [];

    /// <summary>Gets or sets the stable unique key of a node; its index is used when null. Null or duplicate explicit keys are rejected.</summary>
    [Parameter] public Func<TItem, object>? ItemKey { get; set; }

    /// <summary>Gets or sets a node's text and accessible name; ToString is used when null.</summary>
    [Parameter] public Func<TItem, string>? ItemText { get; set; }

    /// <summary>Gets or sets decorative label content, used in both the header and overflow menu; embedded interactive controls are not supported.</summary>
    [Parameter] public RenderFragment<TItem>? ItemTemplate { get; set; }

    /// <summary>Gets or sets the navigation callback, including invocations of the current node. The application updates Items or its route.</summary>
    [Parameter] public EventCallback<BreadcrumbItemInvokedEventArgs<TItem>> OnItemInvoked { get; set; }

    /// <summary>Gets or sets whether navigation and the overflow menu reject interaction.</summary>
    [Parameter] public bool Disabled { get; set; }

    /// <summary>Gets how many leading nodes are currently available through the overflow menu.</summary>
    public int HiddenItemCount => hidden;

    /// <summary>Gets the root element name.</summary>
    protected override string ElementName => "BreadcrumbBar";

    /// <summary>Validates keys and preserves focus identity as the application's path changes.</summary>
    protected override void OnParametersSet()
    {
        ArgumentNullException.ThrowIfNull(Items);
        var next = new object[Items.Count];
        var unique = new HashSet<object>();
        for (var index = 0; index < Items.Count; index++)
        {
            var key = ItemKey is null ? index : ItemKey(Items[index]) ?? throw new ArgumentException("Breadcrumb keys cannot be null.", nameof(ItemKey));
            if (!unique.Add(key)) throw new ArgumentException("Breadcrumb keys must be unique.", nameof(ItemKey));
            next[index] = key;
        }
        if (activeKey is not null && !unique.Contains(activeKey))
        {
            repairFocus = Element.Element?.States.HasFlag(ElementStates.FocusWithin) == true;
            var previous = Array.IndexOf(keys, activeKey);
            activeKey = next.Length == 0 ? null : next[Math.Clamp(previous, 0, next.Length - 1)];
        }
        keys = next;
        foreach (var key in widths.Keys.Where(key => !unique.Contains(key)).ToArray()) widths.Remove(key);
        foreach (var key in buttons.Keys.Where(key => !unique.Contains(key)).ToArray()) buttons.Remove(key);
        hidden = Math.Min(hidden, Math.Max(0, Items.Count - 1));
    }

    /// <summary>Adds the named group, measured width and scoped keyboard navigation.</summary>
    /// <param name="builder">The builder receiving attributes.</param>
    protected override void AddAttributes(RenderTreeBuilder builder)
    {
        builder.AddAttribute(10, "role", "group");
        if (AccessibleName is null) builder.AddAttribute(11, "accessible-name", ControlStrings.Get("Breadcrumb"));
        builder.AddAttribute(12, "disabled", Disabled);
        builder.AddAttribute(13, "overflow-count", hidden);
        builder.AddAttribute(14, "__internal_onresize#breadcrumb", EventCallback.Factory.Create<ResizeEventArgs>(this, e =>
        {
            if (double.IsFinite(e.ContentWidth) && e.ContentWidth >= 0 && available != e.ContentWidth) { available = e.ContentWidth; StateHasChanged(); }
        }));
        builder.AddAttribute(15, "__internal_onkeydown#breadcrumb", EventCallback.Factory.Create<KeyboardEventArgs>(this, KeyDownAsync));
    }

    /// <summary>Renders one measured label per path node and a bounded-to-hidden-nodes overflow menu.</summary>
    /// <param name="builder">The builder receiving content.</param>
    protected override void AddContent(RenderTreeBuilder builder)
    {
        var active = activeKey is null ? -1 : Array.IndexOf(keys, activeKey) + 1;
        focus.ActiveIndex = focusOverflow && hidden > 0 ? 0 : active;
        var stop = focus.TabStop(Items.Count + 1, Skipped);
        builder.OpenComponent<MenuButton>(1000);
        builder.AddComponentParameter(1001, nameof(Control.Class), "breadcrumb-overflow");
        builder.AddComponentParameter(1002, nameof(Control.AccessibleName), ControlStrings.Get("More"));
        builder.AddComponentParameter(1003, nameof(MenuButton.ShowChevron), false);
        builder.AddComponentParameter(1004, nameof(MenuButton.Disabled), Disabled || hidden == 0);
        builder.AddComponentParameter(1005, nameof(Control.TabIndex), stop == 0 ? 0 : -1);
        builder.AddComponentParameter(1006, nameof(Control.Style), hidden == 0 ? "position: absolute; opacity: 0; pointer-events: none" : null);
        builder.AddComponentParameter(1007, nameof(MenuButton.ChildContent), (RenderFragment)(content => content.AddContent(0, "…")));
        builder.AddComponentParameter(1008, nameof(MenuButton.Menu), (RenderFragment)RenderOverflow);
        builder.AddComponentParameter(1009, nameof(Control.AdditionalAttributes), new Dictionary<string, object?>
        {
            ["semantic-hidden"] = hidden == 0,
            ["__internal_onresize#breadcrumb"] = EventCallback.Factory.Create<ResizeEventArgs>(this, e =>
            {
                if (e.Width > 0 && double.IsFinite(e.Width) && overflowWidth != e.Width) { overflowWidth = e.Width; StateHasChanged(); }
            }),
            ["__internal_onfocus#breadcrumb"] = EventCallback.Factory.Create<FocusEventArgs>(this, () => { focusOverflow = true; StateHasChanged(); }),
        });
        builder.AddComponentReferenceCapture(1010, component => overflowButton = (MenuButton)component);
        builder.CloseComponent();
        for (var index = 0; index < Items.Count; index++) RenderNode(builder, index, stop);
    }

    private void RenderNode(RenderTreeBuilder builder, int index, int stop)
    {
        var key = keys[index];
        var collapsed = index < hidden;
        builder.OpenElement(1100, "BreadcrumbSlot");
        builder.SetKey(key);
        builder.AddAttribute(1101, "collapsed", collapsed);
        builder.AddAttribute(1102, "current", index == Items.Count - 1);
        builder.AddAttribute(1103, "semantic-hidden", collapsed);
        builder.AddAttribute(1104, "disabled", collapsed);
        builder.OpenElement(1110, "BreadcrumbItemContent");
        builder.AddAttribute(1111, "__internal_onresize#breadcrumb", EventCallback.Factory.Create<ResizeEventArgs>(this, e =>
        {
            if (e.Width >= 0 && double.IsFinite(e.Width) && (!widths.TryGetValue(key, out var previous) || previous != e.Width))
            { widths[key] = e.Width; StateHasChanged(); }
        }));
        builder.OpenComponent<Button>(1120);
        builder.AddComponentParameter(1121, nameof(Control.AccessibleName), Text(index));
        builder.AddComponentParameter(1122, nameof(Control.AccessibleDescription), index == Items.Count - 1 ? ControlStrings.Get("CurrentLocation") : null);
        builder.AddComponentParameter(1123, nameof(Button.Disabled), Disabled || collapsed);
        builder.AddComponentParameter(1124, nameof(Control.TabIndex), stop == index + 1 ? 0 : -1);
        builder.AddComponentParameter(1125, nameof(Button.OnClick), EventCallback.Factory.Create<PointerEventArgs>(this, () => InvokeAsync(index)));
        builder.AddComponentParameter(1126, nameof(Button.ChildContent), (RenderFragment)(content => Label(content, index)));
        builder.AddComponentParameter(1127, nameof(Control.AdditionalAttributes), new Dictionary<string, object?>
        {
            ["breadcrumb-index"] = index,
            ["__internal_onfocus#breadcrumb"] = EventCallback.Factory.Create<FocusEventArgs>(this, () => { activeKey = key; focusOverflow = false; StateHasChanged(); }),
        });
        builder.AddComponentParameter(1128, nameof(Control.ItemPosition), new LogicalItemPosition(this, index + 1, Items.Count));
        builder.AddComponentParameter(1129, nameof(Control.Tooltip), Text(index));
        builder.AddComponentReferenceCapture(1130, component => buttons[key] = (Button)component);
        builder.CloseComponent();
        if (index < Items.Count - 1)
        {
            builder.OpenElement(1130, "BreadcrumbSeparator");
            builder.AddAttribute(1131, "glyph", "chevron-right");
            builder.AddAttribute(1132, "semantic-hidden", true);
            builder.CloseElement();
        }
        builder.CloseElement();
        builder.CloseElement();
    }

    private void RenderOverflow(RenderTreeBuilder builder)
    {
        for (var index = 0; index < hidden; index++)
        {
            var position = index;
            builder.OpenComponent<MenuItem>(0);
            builder.SetKey(keys[index]);
            builder.AddComponentParameter(1, nameof(Control.AccessibleName), Text(index));
            builder.AddComponentParameter(2, nameof(MenuItem.ChildContent), (RenderFragment)(content => Label(content, position)));
            builder.AddComponentParameter(3, nameof(MenuItem.OnClick), EventCallback.Factory.Create<PointerEventArgs>(this, () => InvokeAsync(position)));
            builder.CloseComponent();
        }
    }

    private void Label(RenderTreeBuilder builder, int index)
    {
        if (ItemTemplate is null) builder.AddContent(0, Text(index));
        else builder.AddContent(1, ItemTemplate(Items[index]));
    }

    private string Text(int index) => ItemText?.Invoke(Items[index]) ?? Items[index]?.ToString() ?? "";
    private bool Skipped(int index) => Disabled || (index == 0 ? hidden == 0 : index <= hidden);

    /// <summary>Requests navigation to the current item at an index. The path is not mutated.</summary>
    /// <param name="index">A zero-based position in Items.</param>
    /// <returns>A task completing after the application handles the request; disabled controls do nothing.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The index is outside the current path.</exception>
    public Task InvokeAsync(int index)
    {
        if ((uint)index >= (uint)Items.Count) throw new ArgumentOutOfRangeException(nameof(index));
        if (Disabled || Element.Element?.IsRemoved == true || ControlTree.Ancestors(Element.Element).Any(node => node.IsDisabled)) return Task.CompletedTask;
        return OnItemInvoked.InvokeAsync(new(Items[index], index));
    }

    /// <summary>Reconciles measured overflow, then repairs focus after path or width changes.</summary>
    /// <param name="firstRender">Whether this is the first render.</param>
    /// <returns>A task completing after any focus repair.</returns>
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        var next = 0;
        if (Items.Count > 1 && keys.All(widths.ContainsKey) && keys.Sum(key => widths[key]) > available)
        {
            var used = overflowWidth;
            next = Items.Count - 1;
            for (var index = Items.Count - 1; index >= 1; index--)
            {
                if (index < Items.Count - 1 && used + widths[keys[index]] > available) break;
                used += widths[keys[index]];
                next = index;
            }
        }
        if (next != hidden)
        {
            if (next == 0 && focusOverflow && Element.Element?.States.HasFlag(ElementStates.FocusWithin) == true)
            { repairFocus = true; focusOverflow = false; }
            if (activeKey is not null && Array.IndexOf(keys, activeKey) < next && Element.Element?.States.HasFlag(ElementStates.FocusWithin) == true)
            { repairFocus = true; focusOverflow = true; }
            hidden = next;
            StateHasChanged();
            return;
        }
        if (!repairFocus || Disabled) return;
        repairFocus = false;
        var target = focusOverflow && hidden > 0 ? overflowButton?.Element
            : activeKey is not null && Array.IndexOf(keys, activeKey) >= hidden ? buttons.GetValueOrDefault(activeKey)?.Element
            : hidden > 0 ? overflowButton?.Element
            : keys.Length > 0 ? buttons.GetValueOrDefault(keys[0])?.Element : null;
        if (target is { } element) await element.FocusAsync(preventScroll: true);
    }

    private async Task KeyDownAsync(KeyboardEventArgs e)
    {
        if (e.Handled || Disabled || Element.Element is not { } root) return;
        var focused = ControlTree.Descendants(root).FirstOrDefault(node => node.States.HasFlag(ElementStates.Focus));
        if (focused is null) return;
        for (var node = focused.ParentElement; node is not null && node != root; node = node.ParentElement)
            if (node.Attributes.GetValueOrDefault("overlay") is true || node.Name == "BreadcrumbBar") return;
        var current = ReferenceEquals(focused, overflowButton?.Element.Element) ? 0 : Array.FindIndex(keys, key => ReferenceEquals(focused, buttons.GetValueOrDefault(key)?.Element.Element)) + 1;
        if (current == 0 && !ReferenceEquals(focused, overflowButton?.Element.Element)) return;
        var target = focus.Move(e, current, Items.Count + 1, Skipped, index => index == 0 ? ControlStrings.Get("More") : Text(index - 1));
        if (e.Modifiers == KeyModifiers.None && e.Key is "ArrowLeft" or "ArrowRight" or "Home" or "End") e.Handled = true;
        if (target is not { } next) return;
        e.Handled = true;
        var reference = next == 0 ? overflowButton?.Element : buttons.GetValueOrDefault(keys[next - 1])?.Element;
        if (reference is { } element) await element.FocusAsync(preventScroll: true);
    }
}
