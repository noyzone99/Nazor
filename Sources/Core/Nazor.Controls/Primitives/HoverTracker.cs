using Nazor.Input;
using Nazor.Rendering;

namespace Nazor.Controls.Primitives;

/// <summary>Tracks one pointer-hovered item for a decorative <see cref="Indicator" /> inside an existing group. It retains the last box for exit fading, snaps the first entry and distinguishes later movement. All calls belong on the UI dispatcher.</summary>
/// <param name="changed">Requests a render when the active item, geometry or entry/movement state changes.</param>
public sealed class HoverTracker(Action changed)
{
    private Func<ElementReference?>? host;
    private Func<ElementNode, bool>? matches;
    private ElementNode? item;
    private (double X, double Y)? pointer;
    private double scrollLeft, scrollTop;

    /// <summary>Gets the last item's border box in the host's scrolling coordinate space, or null before a valid item was measured. Geometry uses the items' axis-aligned window bounds and the host's inverse coordinate conversion.</summary>
    public ItemBox? Target { get; private set; }

    /// <summary>Gets whether the pointer is currently over an eligible, visible, enabled item.</summary>
    public bool Active { get; private set; }

    /// <summary>Gets whether this entry has already moved between items. Themes can snap initial geometry and animate subsequent movement.</summary>
    public bool IsMoving { get; private set; }

    /// <summary>Creates scoped pointer, scroll and resize listeners for the group that directly contains its Indicator. Nested tracker groups own their own hover; disabled/hidden/removed items are excluded. Calling during rendering also drops a stale removed/disabled target.</summary>
    /// <param name="receiver">The component that owns the listeners and receives rendering notifications.</param>
    /// <param name="resolveHost">Resolves the group's current retained element; a resolver allows references captured after its first render.</param>
    /// <param name="match">Identifies eligible item elements inside this group.</param>
    /// <returns>Attributes to add to the host before its element-reference capture. They preserve ordinary pointer activation, focus and semantics.</returns>
    public IReadOnlyDictionary<string, object?> Attributes(object receiver, Func<ElementReference?> resolveHost, Func<ElementNode, bool> match)
    {
        ArgumentNullException.ThrowIfNull(receiver);
        ArgumentNullException.ThrowIfNull(resolveHost);
        ArgumentNullException.ThrowIfNull(match);
        host = resolveHost;
        matches = match;
        RefreshCore(notify: false);
        return new Dictionary<string, object?>
        {
            ["hover-highlight"] = true,
            ["hover-active"] = Active,
            ["hover-moving"] = IsMoving,
            ["__internal_onpointerenter#hover-highlight"] = EventCallback.Factory.Create<PointerEventArgs>(receiver, Track),
            ["__internal_onpointermove#hover-highlight"] = EventCallback.Factory.Create<PointerEventArgs>(receiver, Track),
            ["__internal_onpointerleave#hover-highlight"] = EventCallback.Factory.Create<PointerEventArgs>(receiver, e => { if (e.OriginalTarget == host?.Invoke()?.Element) Clear(); }),
            ["__internal_onscroll#hover-highlight"] = EventCallback.Factory.Create<ScrollEventArgs>(receiver, Scrolled),
            ["__internal_onresize#hover-highlight"] = EventCallback.Factory.Create<ResizeEventArgs>(receiver, _ => Refresh()),
        };
    }

    /// <summary>Updates the current item's geometry after custom layout changes, or clears activity when it is no longer eligible. The last box is retained for exit fading.</summary>
    public void Refresh() => RefreshCore(notify: true);

    /// <summary>Ends the current entry without forgetting its last box, allowing a theme-owned fade. It does not move focus or consume input.</summary>
    public void Clear()
    {
        pointer = null;
        item = null;
        if (!Active && !IsMoving) return;
        Active = false;
        IsMoving = false;
        changed();
    }

    private void Track(PointerEventArgs e)
    {
        if (e.IsSynthetic || host?.Invoke() is not { Element: { IsRemoved: false } boundary }) return;
        pointer = (e.X, e.Y);
        ElementNode? candidate = null;
        var node = e.OriginalTarget;
        if (node == boundary) node = ControlTree.Descendants(boundary).LastOrDefault(element =>
            (element.States & ElementStates.Hover) != 0 && matches?.Invoke(element) is true);
        for (; node is not null && node != boundary; node = node.ParentElement)
        {
            if (node.Attributes.GetValueOrDefault("hover-highlight") is true) { Clear(); return; }
            if (candidate is null && !node.IsDisabled && matches?.Invoke(node) is true) candidate = node;
        }
        if (node != boundary || candidate is null)
        {
            Clear();
            if (node == boundary) pointer = (e.X, e.Y);
            return;
        }
        var moved = Active && item != candidate;
        var before = (Active, IsMoving, Target);
        item = candidate;
        IsMoving |= moved;
        RefreshCore(notify: false);
        if (before != (Active, IsMoving, Target)) changed();
    }

    private void Scrolled(ScrollEventArgs e)
    {
        if (e.OriginalTarget == host?.Invoke()?.Element)
        {
            scrollLeft = e.ScrollLeft;
            scrollTop = e.ScrollTop;
        }
        if (pointer is { } point && host?.Invoke() is { Element: { } boundary } owner &&
            owner.GetBounds() is { } viewport && point.X >= viewport.X && point.X <= viewport.X + viewport.Width && point.Y >= viewport.Y && point.Y <= viewport.Y + viewport.Height)
        {
            var candidate = ControlTree.Descendants(boundary).LastOrDefault(element =>
                !element.IsDisabled && matches?.Invoke(element) is true && InScope(element, boundary) &&
                new ElementReference(element).GetBounds() is { } bounds && point.X >= bounds.X && point.X <= bounds.X + bounds.Width && point.Y >= bounds.Y && point.Y <= bounds.Y + bounds.Height);
            if (candidate is null) Clear();
            else if (candidate != item)
            {
                IsMoving |= Active;
                item = candidate;
                changed();
            }
        }
        Refresh();
    }

    private static bool InScope(ElementNode element, ElementNode boundary)
    {
        for (var node = element.ParentElement; node is not null; node = node.ParentElement)
        {
            if (node == boundary) return true;
            if (node.Attributes.GetValueOrDefault("hover-highlight") is true) return false;
        }
        return false;
    }

    private void RefreshCore(bool notify)
    {
        var before = (Active, IsMoving, Target);
        if (item is { IsRemoved: false, IsDisabled: false } current && matches?.Invoke(current) is true &&
            host?.Invoke() is { Element: { IsRemoved: false } } owner && owner.GetBounds() is not null &&
            new ElementReference(current).GetBounds() is { Width: > 0, Height: > 0 } bounds &&
            owner.GetLocalPoint(bounds.X, bounds.Y) is { } start &&
            owner.GetLocalPoint(bounds.X + bounds.Width, bounds.Y + bounds.Height) is { } end)
        {
            Target = new(Math.Min(start.X, end.X) + scrollLeft, Math.Min(start.Y, end.Y) + scrollTop,
                Math.Abs(end.X - start.X), Math.Abs(end.Y - start.Y));
            Active = true;
        }
        else
        {
            item = null;
            Active = false;
            IsMoving = false;
        }
        if (notify && before != (Active, IsMoving, Target)) changed();
    }
}
