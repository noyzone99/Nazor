using Nazor.Controls.Primitives;
using Nazor.Input;
using Nazor.Rendering;

namespace Nazor.Controls;

/// <summary>
/// A bounded window of dots for selecting a page independently of its content. Bind <see cref="SelectedIndex"/>
/// to the same application state as a FlipView or another page presenter. The group has one dot Tab stop;
/// axis arrows, Home and End select and focus a page. Parts are <c>PipsPagerPrevious</c>, <c>PipsPagerItems</c>,
/// <c>PipsPagerPip</c>, <c>PipsPagerDot</c> and <c>PipsPagerNext</c>.
/// </summary>
public sealed class PipsPager : Control
{
    private readonly ControllableState<int> index = new(0);
    private readonly Dictionary<int, ElementReference> pips = [];
    private readonly RovingFocusController roving = new() { Typeahead = false };
    private bool focusSelection;

    /// <summary>Gets or sets the number of pages; zero renders no dots.</summary>
    [Parameter] public int Count { get; set; }

    /// <summary>Gets or sets the selected zero-based page index, clamped to the available pages.</summary>
    [Parameter] public int SelectedIndex { get; set; }

    /// <summary>Gets or sets the callback invoked once per user selection change. Parameter normalization does not invoke it.</summary>
    [Parameter] public EventCallback<int> SelectedIndexChanged { get; set; }

    /// <summary>Gets or sets the maximum realized dot count, from one to 1000; five by default. The window follows selection.</summary>
    [Parameter] public int MaxVisiblePips { get; set; } = 5;

    /// <summary>Gets or sets whether previous and next actions are shown; true by default. They are outside the dot Tab order.</summary>
    [Parameter] public bool ShowNavigationButtons { get; set; } = true;

    /// <summary>Gets or sets the dot axis; Horizontal by default.</summary>
    [Parameter] public Orientation Orientation { get; set; }

    /// <summary>Gets or sets whether page selection is unavailable.</summary>
    [Parameter] public bool Disabled { get; set; }

    /// <summary>Gets the normalized selected page, or -1 for an empty pager.</summary>
    public int CurrentIndex => Normalize(index.Value);

    /// <summary>Gets the element name emitted by the pager.</summary>
    protected override string ElementName => "PipsPager";

    private int Normalize(int value) => Count == 0 ? -1 : Math.Clamp(value, 0, Count - 1);
    private int VisibleCount => Math.Min(Count, MaxVisiblePips);
    private int FirstVisible => Math.Clamp(CurrentIndex - VisibleCount / 2, 0, Count - VisibleCount);

    /// <summary>Validates bounded realization and synchronizes the selection without reporting a user change.</summary>
    protected override void OnParametersSet()
    {
        ArgumentOutOfRangeException.ThrowIfNegative(Count);
        if (MaxVisiblePips is < 1 or > 1000) throw new ArgumentOutOfRangeException(nameof(MaxVisiblePips));
        if (!Enum.IsDefined(Orientation)) throw new ArgumentOutOfRangeException(nameof(Orientation));
        index.Sync(SelectedIndex);
        index.Set(Normalize(index.Value));
        roving.Orientation = Orientation;
    }

    /// <summary>Adds localized group semantics, the selection and the keyboard handler.</summary>
    /// <param name="builder">The builder receiving attributes.</param>
    protected override void AddAttributes(RenderTreeBuilder builder)
    {
        builder.AddAttribute(10, "role", "group");
        if (AccessibleName is null && LabeledBy is null) builder.AddAttribute(11, "accessible-name", ControlStrings.Get("Pagination"));
        builder.AddAttribute(12, "orientation", Vocabulary.Name(Orientation));
        builder.AddAttribute(13, "disabled", Disabled);
        builder.AddAttribute(14, "selected-index", CurrentIndex);
        builder.AddAttribute(15, "item-count", Count);
        builder.AddAttribute(16, "empty", Count == 0);
        builder.AddAttribute(17, "__internal_onkeydown#pips", EventCallback.Factory.Create<KeyboardEventArgs>(this, KeyDownAsync));
    }

    /// <summary>Renders only the selected dot window and optional bounded navigation actions.</summary>
    /// <param name="builder">The builder receiving content.</param>
    protected override void AddContent(RenderTreeBuilder builder)
    {
        var first = FirstVisible;
        var last = first + VisibleCount;
        focusSelection |= pips.Any(pair => pair.Value.Element is { } element && element.States.HasFlag(ElementStates.Focus) &&
            (pair.Key < first || pair.Key >= last));
        foreach (var key in pips.Keys.Where(key => key < first || key >= last).ToArray()) pips.Remove(key);
        if (ShowNavigationButtons) Step(builder, 1000, previous: true);
        builder.OpenElement(1020, "PipsPagerItems");
        for (var position = first; position < last; position++)
        {
            var value = position;
            builder.OpenElement(1030, "PipsPagerPip");
            builder.SetKey(value);
            builder.AddAttribute(1031, "role", "button");
            builder.AddAttribute(1032, "focusable", !Disabled);
            builder.AddAttribute(1033, "disabled", Disabled);
            builder.AddAttribute(1034, "tabindex", value == CurrentIndex ? 0 : -1);
            builder.AddAttribute(1035, "current", value == CurrentIndex);
            builder.AddAttribute(1036, "position-in-set", value + 1);
            builder.AddAttribute(1037, "size-of-set", Count);
            builder.AddAttribute(1038, "accessible-name", ControlStrings.Format("PageOf", value + 1, Count));
            builder.AddAttribute(1039, "index", value);
            builder.AddAttribute(1040, "__default_onclick", EventCallback.Factory.Create<PointerEventArgs>(this, () => SelectAsync(value)));
            builder.AddElementReferenceCapture(1041, reference => pips[value] = reference);
            builder.OpenElement(1042, "PipsPagerDot");
            builder.AddAttribute(1043, "semantic-hidden", true);
            builder.CloseElement();
            builder.CloseElement();
        }
        builder.CloseElement();
        if (ShowNavigationButtons) Step(builder, 1060, previous: false);
    }

    private void Step(RenderTreeBuilder builder, int sequence, bool previous)
    {
        builder.OpenElement(sequence, previous ? "PipsPagerPrevious" : "PipsPagerNext");
        builder.AddAttribute(sequence + 1, "role", "button");
        builder.AddAttribute(sequence + 2, "accessible-name", ControlStrings.Get(previous ? "PreviousPage" : "NextPage"));
        builder.AddAttribute(sequence + 3, "disabled", Disabled || Count == 0 || (previous ? CurrentIndex <= 0 : CurrentIndex >= Count - 1));
        builder.AddAttribute(sequence + 4, "focusable", false);
        builder.AddAttribute(sequence + 5, "glyph", (previous, Orientation) switch
        {
            (true, Controls.Orientation.Vertical) => "chevron-up", (false, Controls.Orientation.Vertical) => "chevron-down",
            (true, _) => "chevron-left", _ => "chevron-right",
        });
        builder.AddAttribute(sequence + 6, "__default_onclick", EventCallback.Factory.Create<PointerEventArgs>(this,
            () => SelectAsync(CurrentIndex + (previous ? -1 : 1))));
        builder.CloseElement();
    }

    /// <summary>Moves focus to the selected dot after its window changes, without scrolling an ancestor.</summary>
    /// <param name="firstRender">Whether this is the first render.</param>
    protected override void OnAfterRender(bool firstRender)
    {
        if (!focusSelection) return;
        focusSelection = false;
        if (Disabled) return;
        if (pips.GetValueOrDefault(CurrentIndex).Element is { } target && target.GetFeature<IElementInteraction>() is { } input)
            _ = input.FocusAsync(target, preventScroll: true);
    }

    /// <summary>Selects a bounded page and reports one change; disabled, empty and unchanged requests do nothing.</summary>
    /// <param name="position">The requested zero-based page index.</param>
    /// <returns>A task completing after selection callbacks.</returns>
    public async Task SelectAsync(int position)
    {
        if (Disabled || Count == 0) return;
        var next = Normalize(position);
        if (next == CurrentIndex) return;
        index.Set(next);
        focusSelection = true;
        StateHasChanged();
        await SelectedIndexChanged.InvokeAsync(next);
        await Events.RaiseChangeAsync(Element, next);
    }

    private async Task KeyDownAsync(KeyboardEventArgs e)
    {
        if (Disabled || e.Handled || Count == 0) return;
        var target = roving.Move(e, CurrentIndex, Count, _ => false);
        if (target is not { } value) return;
        e.Handled = true;
        await SelectAsync(value);
    }
}
