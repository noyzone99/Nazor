using Nazor.Input;
using Nazor.Rendering;

namespace Nazor.Controls.Primitives;

/// <summary>
/// The keyboard rules of a group with a single Tab stop: arrow keys move the focus between items along an
/// <see cref="Orientation" /> or in a grid of <see cref="Columns" />, Home and End jump to the ends (Ctrl with them in a
/// grid), Page Up and Page Down move by <see cref="PageSize" />, and typing letters jumps to the next item whose text
/// starts with them. Disabled items are skipped. Lists, grids, calendars, segmented controls and chip groups share it;
/// <see cref="RovingFocusGroup" /> applies it to arbitrary content.
/// </summary>
public sealed class RovingFocusController
{
    private string typed = "";
    private DateTime typedAt;

    /// <summary>Gets or sets the arrow axis; null accepts both axes, as a list that can also be read across.</summary>
    public Orientation? Orientation { get; set; } = Controls.Orientation.Vertical;

    /// <summary>Gets or sets the number of columns of a grid; zero for a single row or column.</summary>
    public int Columns { get; set; }

    /// <summary>Gets or sets whether moving past an end continues from the other end.</summary>
    public bool Loop { get; set; }

    /// <summary>Gets or sets whether typing letters jumps to a matching item.</summary>
    public bool Typeahead { get; set; } = true;

    /// <summary>Gets or sets how many items Page Up and Page Down move; zero ignores those keys.</summary>
    public int PageSize { get; set; }

    /// <summary>Gets or sets whether the layout runs right to left, which swaps the left and right arrows.</summary>
    public bool RightToLeft { get; set; }

    /// <summary>Gets or sets the index of the item that holds the Tab stop, or -1 for the first enabled item.</summary>
    public int ActiveIndex { get; set; } = -1;

    /// <summary>Gets the index of the item that holds the Tab stop.</summary>
    /// <param name="count">The number of items.</param>
    /// <param name="isDisabled">Tells whether an item is disabled.</param>
    /// <returns>The index, or -1 when every item is disabled.</returns>
    public int TabStop(int count, Func<int, bool> isDisabled)
    {
        ArgumentNullException.ThrowIfNull(isDisabled);
        if (ActiveIndex >= 0 && ActiveIndex < count && !isDisabled(ActiveIndex)) return ActiveIndex;
        for (var i = 0; i < count; i++) if (!isDisabled(i)) return i;
        return -1;
    }

    /// <summary>Computes the item that a key moves the focus to.</summary>
    /// <param name="e">The key event.</param>
    /// <param name="current">The index of the focused item.</param>
    /// <param name="count">The number of items.</param>
    /// <param name="isDisabled">Tells whether an item is disabled.</param>
    /// <param name="text">Gives the text of an item for typeahead, or null.</param>
    /// <returns>The target index, or null when the key does not move the focus.</returns>
    public int? Move(KeyboardEventArgs e, int current, int count, Func<int, bool> isDisabled, Func<int, string?>? text = null)
    {
        ArgumentNullException.ThrowIfNull(e);
        ArgumentNullException.ThrowIfNull(isDisabled);
        if (count == 0) return null;
        var horizontal = Orientation is null or Controls.Orientation.Horizontal || Columns > 0;
        var vertical = Orientation is null or Controls.Orientation.Vertical || Columns > 0;
        var control = (e.Modifiers & (KeyModifiers.Control | KeyModifiers.Meta)) != 0;
        var row = Columns > 0 ? Columns : 1;
        var (forward, back) = RightToLeft ? ("ArrowLeft", "ArrowRight") : ("ArrowRight", "ArrowLeft");
        if (e.Modifiers is not (KeyModifiers.None or KeyModifiers.Shift) && !(control && e.Key is "Home" or "End")) return null;
        int? target = e.Key switch
        {
            _ when e.Key == forward && horizontal => Step(current, Columns > 0 ? 1 : 1, count, isDisabled),
            _ when e.Key == back && horizontal => Step(current, -1, count, isDisabled),
            "ArrowDown" when vertical => Step(current, row, count, isDisabled),
            "ArrowUp" when vertical => Step(current, -row, count, isDisabled),
            "Home" when Columns > 0 && !control => First(current - current % Columns, 1, Math.Min(count, current - current % Columns + Columns), isDisabled),
            "End" when Columns > 0 && !control => Last(current - current % Columns, Math.Min(count, current - current % Columns + Columns), isDisabled),
            "Home" => First(0, 1, count, isDisabled),
            "End" => Last(0, count, isDisabled),
            "PageDown" when PageSize > 0 => Clamp(current + PageSize, count, isDisabled, -1),
            "PageUp" when PageSize > 0 => Clamp(current - PageSize, count, isDisabled, 1),
            _ => null,
        };
        if (target is null && Typeahead && text is not null && e.Key.Length == 1 && !char.IsControl(e.Key[0]) && e.Modifiers is KeyModifiers.None or KeyModifiers.Shift)
            target = Search(e.Key, current, count, isDisabled, text);
        if (target is { } index) ActiveIndex = index;
        return target;
    }

    private int? Step(int current, int delta, int count, Func<int, bool> isDisabled)
    {
        var index = current < 0 ? (delta > 0 ? -1 : count) : current;
        for (var attempts = 0; attempts < count; attempts++)
        {
            index += delta;
            if (index < 0 || index >= count)
            {
                if (!Loop) return null;
                index = (index % count + count) % count;
            }
            if (!isDisabled(index)) return index;
        }
        return null;
    }

    private static int? First(int from, int step, int to, Func<int, bool> isDisabled)
    {
        for (var i = from; i < to; i += step) if (!isDisabled(i)) return i;
        return null;
    }

    private static int? Last(int from, int to, Func<int, bool> isDisabled)
    {
        for (var i = to - 1; i >= from; i--) if (!isDisabled(i)) return i;
        return null;
    }

    private static int? Clamp(int index, int count, Func<int, bool> isDisabled, int direction)
    {
        index = Math.Clamp(index, 0, count - 1);
        for (; index >= 0 && index < count; index += direction) if (!isDisabled(index)) return index;
        return null;
    }

    private int? Search(string key, int current, int count, Func<int, bool> isDisabled, Func<int, string?> text)
    {
        var now = DateTime.UtcNow;
        typed = now - typedAt > TimeSpan.FromMilliseconds(800) ? key : typed + key;
        typedAt = now;
        var repeated = typed.Length > 1 && typed.All(c => char.ToLowerInvariant(c) == char.ToLowerInvariant(typed[0]));
        var prefix = repeated ? typed[..1] : typed;
        var start = repeated || typed.Length == 1 ? current + 1 : Math.Max(current, 0);
        for (var offset = 0; offset < count; offset++)
        {
            var index = ((start + offset) % count + count) % count;
            if (!isDisabled(index) && text(index)?.TrimStart().StartsWith(prefix, StringComparison.CurrentCultureIgnoreCase) == true) return index;
        }
        return null;
    }
}

/// <summary>
/// Gives arbitrary content a single Tab stop with arrow-key movement between its <see cref="RovingFocusItem" /> children,
/// following the rules of <see cref="RovingFocusController" />. The element is named by <see cref="As" /> and carries
/// <see cref="Role" />, so it can be a toolbar of custom buttons, a grid of cards or a list of tiles.
/// </summary>
public sealed class RovingFocusGroup : ContentControl
{
    private readonly string groupId = UniqueId.Create("roving");
    private readonly RovingFocusController controller = new();
    private readonly List<RovingFocusItem> registered = [];
    private string? activeId;

    /// <summary>Gets or sets the element name, <c>RovingFocusGroup</c> by default.</summary>
    [Parameter] public string As { get; set; } = "RovingFocusGroup";

    /// <summary>Gets or sets the accessible role of the group, <c>group</c> by default.</summary>
    [Parameter] public string Role { get; set; } = "group";

    /// <summary>Gets or sets the arrow axis; null accepts both.</summary>
    [Parameter] public Orientation? Orientation { get; set; } = Controls.Orientation.Horizontal;

    /// <summary>Gets or sets the number of columns of a grid; zero for a single row or column.</summary>
    [Parameter] public int Columns { get; set; }

    /// <summary>Gets or sets whether moving past an end continues from the other end.</summary>
    [Parameter] public bool Loop { get; set; }

    /// <summary>Gets or sets whether typing letters jumps to a matching item.</summary>
    [Parameter] public bool Typeahead { get; set; } = true;

    /// <summary>Gets the element name.</summary>
    protected override string ElementName => As;

    internal string GroupId => groupId;

    /// <summary>Configures the keyboard rules.</summary>
    protected override void OnParametersSet()
    {
        if (Columns < 0) throw new ArgumentOutOfRangeException(nameof(Columns));
        controller.Orientation = Orientation;
        controller.Columns = Columns;
        controller.Loop = Loop;
        controller.Typeahead = Typeahead;
    }

    /// <summary>Adds the role and the keyboard handling.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void AddAttributes(RenderTreeBuilder builder)
    {
        builder.AddAttribute(10, "role", Role);
        builder.AddAttribute(11, "orientation", Orientation is { } o ? Vocabulary.Name(o) : null);
        builder.AddAttribute(12, "__internal_onkeydown#roving", EventCallback.Factory.Create<KeyboardEventArgs>(this, KeyDownAsync));
    }

    /// <summary>Supplies the group to its items.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void AddContent(RenderTreeBuilder builder)
    {
        builder.OpenComponent<CascadingValue<RovingFocusGroup>>(1000);
        builder.AddComponentParameter(1001, "Value", this);
        builder.AddComponentParameter(1002, "ChildContent", ChildContent);
        builder.CloseComponent();
    }

    private List<ElementNode> Items() => Element.Element is { } group
        ? [.. ControlTree.Descendants(group).Where(node => Equals(node.Attributes.GetValueOrDefault("roving-group"), groupId))]
        : [];

    /// <summary>Gives the Tab stop to the right item once the items are laid out.</summary>
    /// <param name="firstRender">Whether this is the first render.</param>
    protected override void OnAfterRender(bool firstRender)
    {
        if (firstRender) Refresh();
    }

    internal void Register(RovingFocusItem item) => registered.Add(item);

    internal void Unregister(RovingFocusItem item)
    {
        if (registered.Remove(item)) Refresh();
    }

    private void Refresh()
    {
        foreach (var item in registered) item.Refresh();
    }

    internal bool IsTabStop(string itemId)
    {
        var items = Items();
        if (items.Count == 0) return false;
        var active = items.FindIndex(node => Equals(node.Attributes.GetValueOrDefault("id"), activeId));
        controller.ActiveIndex = active;
        var stop = controller.TabStop(items.Count, i => items[i].IsDisabled);
        return stop >= 0 && Equals(items[stop].Attributes.GetValueOrDefault("id"), itemId);
    }

    internal void Activate(string itemId)
    {
        if (activeId == itemId) return;
        activeId = itemId;
        Refresh();
    }

    private async Task KeyDownAsync(KeyboardEventArgs e)
    {
        if (e.Handled) return;
        var items = Items();
        var current = items.FindIndex(node => (node.States & (ElementStates.Focus | ElementStates.FocusWithin)) != 0);
        if (current < 0) return;
        var target = controller.Move(e, current, items.Count, i => items[i].IsDisabled, i => ControlTree.Text(items[i]));
        if (target is not { } index || index == current) return;
        e.Handled = true;
        activeId = items[index].Attributes.GetValueOrDefault("id") as string;
        Refresh();
        await new ElementReference(items[index]).FocusAsync();
    }
}

/// <summary>
/// An item of a <see cref="RovingFocusGroup" />: it is focusable, holds the group's Tab stop when it is the active item,
/// and becomes active when it receives the focus. The element is named by <see cref="As" />.
/// </summary>
public sealed class RovingFocusItem : ContentControl, IDisposable
{
    private readonly string itemId = UniqueId.Create("roving-item");
    private RovingFocusGroup? registered;

    /// <summary>Gets or sets the element name, <c>RovingFocusItem</c> by default.</summary>
    [Parameter] public string As { get; set; } = "RovingFocusItem";

    /// <summary>Gets or sets the accessible role of the item.</summary>
    [Parameter] public string? Role { get; set; }

    /// <summary>Gets or sets whether the item is skipped and rejects interaction.</summary>
    [Parameter] public bool Disabled { get; set; }

    /// <summary>Gets or sets the enclosing group, supplied automatically.</summary>
    [CascadingParameter] public RovingFocusGroup? Group { get; set; }

    /// <summary>Gets the element name.</summary>
    protected override string ElementName => As;

    /// <summary>Requires an enclosing group.</summary>
    protected override void OnParametersSet()
    {
        if (Group is null) throw new InvalidOperationException("RovingFocusItem must be inside a RovingFocusGroup.");
        if (registered == Group) return;
        registered?.Unregister(this);
        (registered = Group).Register(this);
    }

    internal void Refresh() => StateHasChanged();

    /// <summary>Leaves the group.</summary>
    public void Dispose()
    {
        registered?.Unregister(this);
        registered = null;
    }

    /// <summary>Adds the identity, the Tab stop and the activation on focus.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void AddAttributes(RenderTreeBuilder builder)
    {
        builder.AddAttribute(10, "id", Id ?? itemId);
        builder.AddAttribute(11, "roving-group", Group!.GroupId);
        builder.AddAttribute(12, "role", Role);
        builder.AddAttribute(13, "disabled", Disabled);
        builder.AddAttribute(14, "focusable", !Disabled);
        if (TabIndex is null) builder.AddAttribute(15, "tabindex", Group.IsTabStop(Id ?? itemId) ? 0 : -1);
        builder.AddAttribute(16, "__internal_onfocus#roving", EventCallback.Factory.Create<FocusEventArgs>(this, () => Group.Activate(Id ?? itemId)));
    }
}
