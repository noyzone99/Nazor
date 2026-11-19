using Nazor.Controls.Primitives;
using Nazor.Input;
using Nazor.Rendering;

namespace Nazor.Controls;

/// <summary>
/// Browses a collection one item at a time. <see cref="SelectedIndex" /> (<c>@bind-SelectedIndex</c>) is zero-based;
/// the first item is selected by default and <see cref="CurrentIndex" /> is -1 when empty. Only the selected
/// <see cref="ItemTemplate" /> is realized, so navigating away disposes its local component state; keep persistent
/// state in the data model. Selection follows the index when items are reordered and is clamped when items are removed.
/// Left/Right, Home/End navigate while the FlipView itself has focus; descendant controls keep their keyboard behavior.
/// Previous/Next buttons and an optional horizontal primary-pointer swipe navigate without wrapping or autoplay.
/// Swipes starting on interactive descendants are ignored. Parts are <c>FlipViewViewport</c>, <c>FlipViewItem</c>,
/// <c>FlipViewPrevious</c>, <c>FlipViewNext</c> and the polite position announcement <c>FlipViewStatus</c>.
/// </summary>
/// <typeparam name="TItem">The type of the displayed items.</typeparam>
public sealed class FlipView<TItem> : Control
{
    private const double SwipeThreshold = 36;
    private readonly ControllableState<int> index = new(0);
    private ElementReference viewport;
    private ElementReference item;
    private ElementReference previous;
    private ElementReference next;
    private object? shown;
    private bool restoreFocus;
    private (double X, double Y)? swipe;

    /// <summary>Gets or sets the items in display order. The collection cannot be null.</summary>
    [Parameter] public IReadOnlyList<TItem> Items { get; set; } = [];

    /// <summary>Gets or sets the selected item's content; null displays its string representation.</summary>
    [Parameter] public RenderFragment<TItem>? ItemTemplate { get; set; }

    /// <summary>Gets or sets the selected zero-based index, clamped to the available items; an empty collection has index -1.</summary>
    [Parameter] public int SelectedIndex { get; set; }

    /// <summary>Gets or sets the callback invoked once when user navigation selects a different item. Parameter or collection normalization does not invoke it.</summary>
    [Parameter] public EventCallback<int> SelectedIndexChanged { get; set; }

    /// <summary>Gets or sets whether the control rejects navigation and interaction with its content.</summary>
    [Parameter] public bool Disabled { get; set; }

    /// <summary>Gets or sets whether a horizontal primary-pointer drag on noninteractive content can select an adjacent item; true by default.</summary>
    [Parameter] public bool SwipeEnabled { get; set; } = true;

    /// <summary>Gets the selected index, or -1 while there are no items.</summary>
    public int CurrentIndex => Normalize(index.Value);

    /// <summary>Gets the NSS element name emitted by FlipView.</summary>
    protected override string ElementName => "FlipView";

    /// <summary>Validates the collection, synchronizes selection and cancels unavailable gestures.</summary>
    protected override void OnParametersSet()
    {
        ArgumentNullException.ThrowIfNull(Items);
        index.Sync(SelectedIndex);
        index.Set(Normalize(index.Value));
        if (Disabled || !SwipeEnabled || Items.Count < 2) CancelSwipe();
    }

    private int Normalize(int value) => Items.Count == 0 ? -1 : Math.Clamp(value, 0, Items.Count - 1);

    /// <summary>Adds group semantics, interaction states and keyboard navigation.</summary>
    /// <param name="builder">The builder receiving the attributes.</param>
    protected override void AddAttributes(RenderTreeBuilder builder)
    {
        builder.AddAttribute(10, "role", "group");
        builder.AddAttribute(11, "role-description", ControlStrings.Get("CarouselRole"));
        builder.AddAttribute(12, "focusable", !Disabled);
        builder.AddAttribute(13, "disabled", Disabled);
        builder.AddAttribute(14, "selected-index", CurrentIndex);
        builder.AddAttribute(15, "item-count", Items.Count);
        builder.AddAttribute(16, "empty", Items.Count == 0);
        builder.AddAttribute(17, "__internal_onkeydown#flip", EventCallback.Factory.Create<KeyboardEventArgs>(this, KeyDownAsync));
    }

    /// <summary>Renders the current item, navigation buttons and localized position announcement.</summary>
    /// <param name="builder">The builder receiving the content.</param>
    protected override void AddContent(RenderTreeBuilder builder)
    {
        var current = CurrentIndex;
        object? key = current < 0 ? null : (current, Items[current]);
        if (!Equals(shown, key))
        {
            restoreFocus |= item.Element is { } old && old.States.HasFlag(ElementStates.FocusWithin);
            CancelSwipe();
            shown = key;
        }
        restoreFocus |= previous.Element is { } back && back.States.HasFlag(ElementStates.Focus) && current <= 0;
        restoreFocus |= next.Element is { } forward && forward.States.HasFlag(ElementStates.Focus) && current >= Items.Count - 1;

        builder.OpenElement(1000, "FlipViewViewport");
        builder.AddAttribute(1002, "__internal_onpointerdown#flip", EventCallback.Factory.Create<PointerEventArgs>(this, PointerDown));
        builder.AddAttribute(1003, "__internal_onpointermove#flip", EventCallback.Factory.Create<PointerEventArgs>(this, PointerMove));
        builder.AddAttribute(1004, "__internal_onpointerup#flip", EventCallback.Factory.Create<PointerEventArgs>(this, PointerUpAsync));
        builder.AddAttribute(1005, "__internal_onpointerleave#flip", EventCallback.Factory.Create<PointerEventArgs>(this, PointerLeave));
        builder.AddAttribute(1006, "__internal_onlostpointercapture#flip", EventCallback.Factory.Create<PointerEventArgs>(this, e => { if (e.OriginalTarget == viewport.Element) swipe = null; }));
        builder.AddElementReferenceCapture(1007, reference => viewport = reference);
        if (current >= 0)
        {
            builder.OpenElement(1010, "FlipViewItem");
            builder.SetKey(key);
            builder.AddAttribute(1011, "role", "group");
            builder.AddAttribute(1012, "role-description", ControlStrings.Get("SlideRole"));
            builder.AddAttribute(1013, "accessible-name", ControlStrings.Format("Slide", current + 1, Items.Count));
            builder.AddAttribute(1014, "position-in-set", current + 1);
            builder.AddAttribute(1015, "size-of-set", Items.Count);
            builder.AddElementReferenceCapture(1016, reference => item = reference);
            if (ItemTemplate is not null) builder.AddContent(1017, ItemTemplate(Items[current]));
            else builder.AddContent(1018, Items[current]?.ToString());
            builder.CloseElement();
        }
        builder.CloseElement();
        AddButton(builder, 1100, back: true, current <= 0);
        AddButton(builder, 1200, back: false, current < 0 || current >= Items.Count - 1);
        builder.OpenElement(1300, "FlipViewStatus");
        builder.AddAttribute(1301, "role", "status");
        builder.AddAttribute(1302, "live", "polite");
        builder.AddContent(1303, current < 0 ? ControlStrings.Format("ItemCount", 0) : ControlStrings.Format("Slide", current + 1, Items.Count));
        builder.CloseElement();
    }

    private void AddButton(RenderTreeBuilder builder, int sequence, bool back, bool atEnd)
    {
        builder.OpenElement(sequence, back ? "FlipViewPrevious" : "FlipViewNext");
        builder.AddAttribute(sequence + 1, "role", "button");
        builder.AddAttribute(sequence + 2, "focusable", !Disabled && !atEnd);
        builder.AddAttribute(sequence + 3, "disabled", Disabled || atEnd);
        builder.AddAttribute(sequence + 4, "accessible-name", ControlStrings.Get(back ? "Previous" : "Next"));
        builder.AddAttribute(sequence + 5, "glyph", back ? "chevron-left" : "chevron-right");
        builder.AddAttribute(sequence + 6, "__default_onclick", EventCallback.Factory.Create<PointerEventArgs>(this, () => back ? PreviousAsync() : NextAsync()));
        builder.AddElementReferenceCapture(sequence + 7, reference => { if (back) previous = reference; else next = reference; });
        builder.CloseElement();
    }

    /// <summary>Returns focus to the FlipView after focused content is removed or a navigation button reaches its bound.</summary>
    /// <param name="firstRender">Whether this is the first render.</param>
    protected override void OnAfterRender(bool firstRender)
    {
        if (!restoreFocus) return;
        restoreFocus = false;
        if (!Disabled && Element.Element is { IsRemoved: false }) _ = Element.FocusAsync(preventScroll: true);
    }

    /// <summary>Selects an available index and reports the change; disabled, repeated or out-of-range requests do nothing.</summary>
    /// <param name="position">The requested zero-based index.</param>
    /// <returns>A task completed after selection callbacks and input/change events have been reported.</returns>
    public async Task SelectAsync(int position)
    {
        if (Disabled || position < 0 || position >= Items.Count || position == CurrentIndex) return;
        CancelSwipe();
        index.Set(position);
        StateHasChanged();
        await SelectedIndexChanged.InvokeAsync(position);
        await Events.RaiseChangeAsync(Element, position);
    }

    /// <summary>Selects the previous item, stopping at the first one.</summary>
    /// <returns>A task completed after the selection change has been reported.</returns>
    public Task PreviousAsync() => SelectAsync(CurrentIndex - 1);

    /// <summary>Selects the next item, stopping at the last one.</summary>
    /// <returns>A task completed after the selection change has been reported.</returns>
    public Task NextAsync() => SelectAsync(CurrentIndex + 1);

    private Task KeyDownAsync(KeyboardEventArgs e)
    {
        if (e.Handled || Disabled || e.Modifiers != KeyModifiers.None) return Task.CompletedTask;
        if (e.Key == "Escape" && swipe is not null)
        {
            e.Handled = true;
            CancelSwipe();
            return Task.CompletedTask;
        }
        if (Element.Element is not { } root || !root.States.HasFlag(ElementStates.Focus) || CurrentIndex < 0) return Task.CompletedTask;
        var requested = e.Key switch
        {
            "ArrowLeft" => CurrentIndex - 1,
            "ArrowRight" => CurrentIndex + 1,
            "Home" => 0,
            "End" => Items.Count - 1,
            _ => (int?)null,
        };
        if (requested is not { } position) return Task.CompletedTask;
        e.Handled = true;
        return SelectAsync(position);
    }

    private void PointerDown(PointerEventArgs e)
    {
        CancelSwipe();
        if (Disabled || !SwipeEnabled || Items.Count < 2 || e.Button != PointerButton.Primary || e.IsSynthetic || e.Modifiers != KeyModifiers.None ||
            viewport.Element is not { } surface || e.OriginalTarget is not { } target) return;
        for (ElementNode? node = target; node != surface; node = node.ParentElement)
        {
            if (node is null || ControlTree.IsFocusable(node) || ControlTree.IsFieldControl(node) ||
                node.Attributes.GetValueOrDefault("role") is "button" or "link" or "menuitem" ||
                node.Attributes.Keys.Any(name => name.Contains("onclick", StringComparison.Ordinal) || name.Contains("onpointerdown", StringComparison.Ordinal))) return;
        }
        if (viewport.CapturePointer()) swipe = (e.X, e.Y);
    }

    private void PointerMove(PointerEventArgs e)
    {
        if (swipe is null || viewport.IsPointerCaptured) return;
        swipe = null;
    }

    private void PointerLeave(PointerEventArgs e)
    {
        if (!viewport.IsPointerCaptured) swipe = null;
    }

    private Task PointerUpAsync(PointerEventArgs e)
    {
        if (swipe is not { } start) return Task.CompletedTask;
        var captured = viewport.IsPointerCaptured;
        CancelSwipe();
        if (!captured || e.Button != PointerButton.Primary || Disabled || !SwipeEnabled) return Task.CompletedTask;
        var horizontal = e.X - start.X;
        var vertical = e.Y - start.Y;
        if (Math.Abs(horizontal) < SwipeThreshold || Math.Abs(horizontal) <= Math.Abs(vertical) * 1.25) return Task.CompletedTask;
        return horizontal < 0 ? NextAsync() : PreviousAsync();
    }

    private void CancelSwipe()
    {
        swipe = null;
        if (viewport.IsPointerCaptured) viewport.ReleasePointerCapture();
    }
}
