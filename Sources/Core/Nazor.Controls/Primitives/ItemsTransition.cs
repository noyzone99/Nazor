using Nazor.Input;
using Nazor.Rendering;

namespace Nazor.Controls.Primitives;

/// <summary>
/// Renders a keyed collection with stable component identity, entrance parts, retained noninteractive removals and
/// <see cref="LayoutTransition"/> relocation. The collection controls order; a theme controls timing and appearance.
/// Removed items keep their last measured box outside normal flow. Parts are <c>ItemsTransitionItem</c> and
/// <c>ItemsTransitionContent</c>, with <c>entering</c> and <c>exiting</c> attributes on the content. Preserving extent adds
/// a borderless <c>ItemsTransitionViewport</c> positioning context inside the collection's padding and borders.
/// </summary>
/// <typeparam name="TItem">The type of the items.</typeparam>
public sealed class ItemsTransition<TItem> : Control, IDisposable
{
    private readonly List<Entry> entries = [];
    private PresenceMotion? motion;
    private Entry? focusTarget;
    private bool focusFallback;
    private long removalGeneration;
    private int reportedCount = -1;
    private double measuredHeight;
    private double preservedHeight;

    /// <summary>Gets or sets the current collection in display order.</summary>
    [Parameter] public IReadOnlyList<TItem> Items { get; set; } = [];

    /// <summary>Gets or sets the required stable, non-null unique identity of each item. Duplicate keys are rejected before rendering.</summary>
    [Parameter, EditorRequired] public Func<TItem, object>? ItemKey { get; set; }

    /// <summary>Gets or sets item content; null displays each item's string representation.</summary>
    [Parameter] public RenderFragment<TItem>? ItemTemplate { get; set; }

    /// <summary>Gets or sets the maximum number of retained removals, from zero to 1024; 32 by default. Older removals are disposed first.</summary>
    [Parameter] public int MaximumRetainedItems { get; set; } = 32;

    /// <summary>Gets or sets whether captured collection height and outgoing vertical extents reserve the minimum height until exit completes, including collection padding and borders. False by default; removed items remain absolutely positioned.</summary>
    [Parameter] public bool PreserveExtentDuringExit { get; set; }

    /// <summary>Gets or sets the callback reporting the total current and retained item count after the first render and subsequent count changes. Invoke and mutate components only on the UI thread.</summary>
    [Parameter] public EventCallback<int> RenderedCountChanged { get; set; }

    /// <summary>Gets the element name.</summary>
    protected override string ElementName => "ItemsTransition";

    /// <summary>Creates the column positioning context and optionally reserves the captured outgoing vertical extent.</summary>
    protected override string? ElementStyle
    {
        get
        {
            var extent = PreserveExtentDuringExit ? Math.Max(preservedHeight, entries.Where(entry => !entry.Current)
                .Select(entry => entry.Box is { } box ? Math.Max(0, box.Y + box.Height) : 0).DefaultIfEmpty(0).Max()) : 0;
            return Css.Join("display: flex; flex-direction: column; position: relative" + (extent > 0 ? "; min-height: " + Css.Px(extent) : ""), Style);
        }
    }

    /// <summary>Validates identities, preserves current items and records removed subtrees.</summary>
    protected override void OnParametersSet()
    {
        ArgumentNullException.ThrowIfNull(Items);
        ArgumentNullException.ThrowIfNull(ItemKey);
        if (MaximumRetainedItems is < 0 or > 1024) throw new ArgumentOutOfRangeException(nameof(MaximumRetainedItems));
        var keyed = new List<(object Key, TItem Item)>(Items.Count);
        var keys = new HashSet<object>();
        foreach (var item in Items)
        {
            var key = ItemKey(item) ?? throw new ArgumentException("Item keys cannot be null.", nameof(ItemKey));
            if (!keys.Add(key)) throw new ArgumentException("Item keys must be unique.", nameof(ItemKey));
            keyed.Add((key, item));
        }
        var previous = entries.Where(entry => entry.Current).ToList();
        var focused = previous.FindIndex(entry => entry.Element.Element?.States.HasFlag(ElementStates.FocusWithin) == true);
        var byKey = entries.ToDictionary(entry => entry.Key);
        var current = new List<Entry>(keyed.Count);
        foreach (var (key, item) in keyed)
        {
            if (byKey.TryGetValue(key, out var entry))
            {
                entry.Item = item;
                if (!entry.Current) entry.Entering = false;
                entry.Current = true;
            }
            else entry = new Entry(key, item);
            entry.Template = ItemTemplate;
            current.Add(entry);
        }
        var removed = entries.Where(entry => !keys.Contains(entry.Key)).ToList();
        if (PreserveExtentDuringExit && removed.Any(entry => entry.Current && entry.Box is not null))
            preservedHeight = Math.Max(preservedHeight, measuredHeight);
        foreach (var entry in removed)
        {
            if (entry.Current) entry.RemovalGeneration = ++removalGeneration;
            entry.Current = false;
        }
        if (focused >= 0 && !keys.Contains(previous[focused].Key))
        {
            focusTarget = current.Count == 0 ? null : current[Math.Min(focused, current.Count - 1)];
            focusFallback = focusTarget is null;
        }
        entries.Clear();
        entries.AddRange(current);
        entries.AddRange(removed.OrderBy(entry => entry.RemovalGeneration).TakeLast(MaximumRetainedItems));
        if (!PreserveExtentDuringExit || !entries.Any(entry => !entry.Current)) preservedHeight = 0;
    }

    /// <summary>Adds the host motion marker and a focus fallback for an empty collection.</summary>
    /// <param name="builder">The builder receiving the attributes.</param>
    protected override void AddAttributes(RenderTreeBuilder builder)
    {
        builder.AddAttribute(10, "exit-presence", true);
        builder.AddAttribute(11, "focusable", true);
        if (TabIndex is null) builder.AddAttribute(12, "tabindex", -1);
        if (PreserveExtentDuringExit)
            builder.AddAttribute(13, "__internal_onresize#items-extent", EventCallback.Factory.Create<ResizeEventArgs>(this, e => measuredHeight = e.Height));
    }

    /// <summary>Renders stable current items and bounded retained removals.</summary>
    /// <param name="builder">The builder receiving the items.</param>
    protected override void AddContent(RenderTreeBuilder builder)
    {
        motion ??= new(() => _ = InvokeAsync(StateHasChanged));
        var duration = motion.Duration(Element.Element);
        if (PreserveExtentDuringExit)
        {
            builder.OpenElement(100, "ItemsTransitionViewport");
            builder.AddAttribute(101, "style", "display: flex; flex-direction: column; position: relative; gap: inherit; align-items: inherit; align-self: stretch; flex: none; min-width: 0");
        }
        foreach (var entry in entries.ToArray())
        {
            builder.OpenComponent<Presence>(1000);
            builder.SetKey(entry.Key);
            builder.AddComponentParameter(1001, nameof(Presence.Present), entry.Current);
            builder.AddComponentParameter(1002, nameof(Presence.ExitDuration), entry.Box is null ? TimeSpan.Zero : duration);
            builder.AddComponentParameter(1003, nameof(Presence.OnExited), EventCallback.Factory.Create(this, () => Remove(entry)));
            builder.AddComponentParameter(1004, nameof(Presence.ChildContent), (RenderFragment<PresenceState>)(_ => content => RenderItem(content, entry)));
            builder.CloseComponent();
        }
        if (PreserveExtentDuringExit) builder.CloseElement();
    }

    private void RenderItem(RenderTreeBuilder builder, Entry entry)
    {
        var attributes = new Dictionary<string, object?>
        {
            ["disabled"] = !entry.Current,
            ["semantic-hidden"] = !entry.Current,
            ["exiting"] = !entry.Current,
            ["__internal_onresize#items"] = EventCallback.Factory.Create<ResizeEventArgs>(this, e =>
            {
                if (entry.Current) entry.Box = new(e.OffsetLeft, e.OffsetTop, e.Width, e.Height);
            }),
        };
        builder.OpenComponent<LayoutTransition>(0);
        builder.AddComponentParameter(1, nameof(LayoutTransition.As), "ItemsTransitionItem");
        builder.AddComponentParameter(3, nameof(Control.AdditionalAttributes), attributes);
        builder.AddComponentParameter(4, nameof(Control.Style), !entry.Current && entry.Box is { } box
            ? $"display: block; position: absolute; left: {Css.Px(box.X)}; top: {Css.Px(box.Y)}; width: {Css.Px(box.Width)}; height: {Css.Px(box.Height)}; pointer-events: none" : "display: block");
        builder.AddComponentParameter(5, nameof(LayoutTransition.ChildContent), (RenderFragment)(content =>
        {
            content.OpenElement(0, "ItemsTransitionContent");
            content.AddAttribute(1, "entering", entry.Entering);
            content.AddAttribute(2, "exiting", !entry.Current);
            content.AddAttribute(3, "style", "display: block");
            if (entry.Template is { } template) content.AddContent(4, template(entry.Item));
            else content.AddContent(5, entry.Item?.ToString());
            content.CloseElement();
        }));
        builder.AddComponentReferenceCapture(6, component => entry.Component = (LayoutTransition)component);
        builder.CloseComponent();
    }

    private void Remove(Entry entry)
    {
        if (entry.Current || !entries.Remove(entry)) return;
        if (!entries.Any(item => !item.Current)) preservedHeight = 0;
        StateHasChanged();
    }

    /// <summary>Restores removed descendant focus and reports a changed total of current and retained items.</summary>
    /// <param name="firstRender">Whether this is the first render.</param>
    /// <returns>A task completing after focus restoration and the count notification.</returns>
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (focusTarget is not null || focusFallback)
        {
            var target = focusTarget?.Element.Element is { } item
                ? ControlTree.Descendants(item).FirstOrDefault(ControlTree.IsFocusable) : null;
            focusTarget = null;
            focusFallback = false;
            if ((target ?? Element.Element) is { } element && element.GetFeature<IElementInteraction>() is { } input)
                await input.FocusAsync(element, preventScroll: true);
        }
        var count = entries.Count;
        if (reportedCount == count) return;
        reportedCount = count;
        await RenderedCountChanged.InvokeAsync(count);
    }

    /// <summary>Detaches host-preference notifications; child Presence components cancel pending exits.</summary>
    public void Dispose() => motion?.Dispose();

    private sealed class Entry(object key, TItem item)
    {
        public object Key { get; } = key;
        public TItem Item { get; set; } = item;
        public bool Current { get; set; } = true;
        public bool Entering { get; set; } = true;
        public ItemBox? Box { get; set; }
        public LayoutTransition? Component { get; set; }
        public ElementReference Element => Component?.Element ?? default;
        public RenderFragment<TItem>? Template { get; set; }
        public long RemovalGeneration { get; set; }
    }
}
