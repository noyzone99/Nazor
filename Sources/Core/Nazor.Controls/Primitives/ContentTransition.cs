using Nazor.Rendering;

namespace Nazor.Controls.Primitives;

/// <summary>The motion requested when keyed content changes. Themes own distances, durations and easing.</summary>
public enum ContentTransitionKind
{
    /// <summary>Replaces content immediately.</summary>
    None,
    /// <summary>Reveals incoming content without a directional relationship.</summary>
    Entrance,
    /// <summary>Moves between adjacent content in the requested direction.</summary>
    Slide,
    /// <summary>Moves from an overview into its detail.</summary>
    DrillIn,
    /// <summary>Moves from a detail back to its overview.</summary>
    DrillOut,
}

/// <summary>The relationship between the incoming and outgoing content.</summary>
public enum TransitionDirection
{
    /// <summary>Moves toward later content.</summary>
    Forward,
    /// <summary>Moves toward earlier content.</summary>
    Backward,
}

/// <summary>
/// Replaces keyed content while retaining at most one outgoing layer for the theme's <c>overlay-exit-duration</c>.
/// The outgoing subtree keeps its component identity, rejects input and is hidden from accessibility. A newer change
/// discards the oldest outgoing layer. An unchanged <see cref="ContentKey"/> updates the existing subtree normally.
/// Parts are <c>{As}Layer</c> with <c>current</c>, <c>exiting</c>, <c>transition</c> and <c>direction</c> attributes.
/// </summary>
public sealed class ContentTransition : Control, IDisposable
{
    private readonly List<Layer> layers = [];
    private PresenceMotion? motion;
    private long generation;
    private bool restoreFocus;

    /// <summary>Gets or sets the element name, <c>ContentTransition</c> by default.</summary>
    [Parameter] public string As { get; set; } = "ContentTransition";

    /// <summary>Gets or sets the identity of the current content. Changing it creates a new subtree.</summary>
    [Parameter] public object? ContentKey { get; set; }

    /// <summary>Gets or sets the current content. Capture immutable data when outgoing content must preserve its previous appearance.</summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>Gets or sets the motion for the next replacement; <see cref="ContentTransitionKind.None"/> by default.</summary>
    [Parameter] public ContentTransitionKind Transition { get; set; }

    /// <summary>Gets or sets the direction for the next replacement.</summary>
    [Parameter] public TransitionDirection Direction { get; set; }

    /// <summary>Gets or sets an exit-retention override; null follows the theme. Reduced motion always removes outgoing content immediately.</summary>
    [Parameter] public TimeSpan? ExitDuration { get; set; }

    /// <summary>Gets or sets a resolver for focus moved out of hidden content; null uses the first focusable descendant or the region itself.</summary>
    [Parameter] public Func<ElementReference?>? InitialFocus { get; set; }

    /// <summary>Gets the element name.</summary>
    protected override string ElementName => As;

    /// <summary>Creates a clipped positioning context while preserving the caller's declarations.</summary>
    protected override string? ElementStyle => Css.Join("display: block; position: relative; overflow: hidden", Style);

    /// <summary>Validates motion parameters and captures a keyed replacement.</summary>
    protected override void OnParametersSet()
    {
        if (string.IsNullOrWhiteSpace(As)) throw new ArgumentException("An element name is required.", nameof(As));
        if (!Enum.IsDefined(Transition)) throw new ArgumentOutOfRangeException(nameof(Transition));
        if (!Enum.IsDefined(Direction)) throw new ArgumentOutOfRangeException(nameof(Direction));
        if (ExitDuration is { } duration && (duration < TimeSpan.Zero || duration.TotalMilliseconds > uint.MaxValue - 1))
            throw new ArgumentOutOfRangeException(nameof(ExitDuration));
        var current = layers.LastOrDefault();
        if (current is not null && Equals(current.Key, ContentKey))
        {
            current.Content = ChildContent;
            return;
        }
        if (current is not null)
        {
            restoreFocus |= current.Element.Element?.States.HasFlag(ElementStates.FocusWithin) == true;
            current.Transition = Transition;
            current.Direction = Direction;
            if (layers.Count > 1) layers.RemoveAt(0);
        }
        layers.Add(new Layer(ContentKey, ++generation, ChildContent,
            current is null ? ContentTransitionKind.None : Transition, Direction));
    }

    /// <summary>Adds the theme motion marker and a programmatic focus fallback.</summary>
    /// <param name="builder">The builder receiving the attributes.</param>
    protected override void AddAttributes(RenderTreeBuilder builder)
    {
        builder.AddAttribute(10, "exit-presence", true);
        builder.AddAttribute(11, "focusable", true);
        if (TabIndex is null) builder.AddAttribute(12, "tabindex", -1);
    }

    /// <summary>Renders the current and retained outgoing subtrees without remounting the previous content.</summary>
    /// <param name="builder">The builder receiving the layers.</param>
    protected override void AddContent(RenderTreeBuilder builder)
    {
        motion ??= new(() => _ = InvokeAsync(StateHasChanged));
        var duration = motion.Duration(Element.Element, ExitDuration);
        foreach (var layer in layers.AsEnumerable().Reverse().ToArray())
        {
            var current = ReferenceEquals(layer, layers[^1]);
            builder.OpenComponent<Presence>(1000);
            builder.SetKey(layer.Generation);
            builder.AddComponentParameter(1001, nameof(Presence.Present), current);
            builder.AddComponentParameter(1002, nameof(Presence.ExitDuration), layer.Transition == ContentTransitionKind.None ? TimeSpan.Zero : duration);
            builder.AddComponentParameter(1003, nameof(Presence.OnExited), EventCallback.Factory.Create(this, () => Remove(layer)));
            builder.AddComponentParameter(1004, nameof(Presence.ChildContent), (RenderFragment<PresenceState>)(_ => content =>
            {
                content.OpenElement(0, As + "Layer");
                content.AddAttribute(1, "current", current);
                content.AddAttribute(2, "exiting", !current);
                content.AddAttribute(3, "disabled", !current);
                content.AddAttribute(4, "semantic-hidden", !current);
                content.AddAttribute(5, "transition", layer.Transition.ToString().ToLowerInvariant());
                content.AddAttribute(6, "direction", layer.Direction.ToString().ToLowerInvariant());
                content.AddAttribute(7, "style", current ? "display: block" : "display: block; position: absolute; inset: 0; pointer-events: none");
                content.AddElementReferenceCapture(8, reference => layer.Element = reference);
                content.AddContent(9, layer.Content);
                content.CloseElement();
            }));
            builder.CloseComponent();
        }
    }

    private void Remove(Layer layer)
    {
        if (layers.Count == 0 || ReferenceEquals(layer, layers[^1]) || !layers.Remove(layer)) return;
        StateHasChanged();
    }

    /// <summary>Moves hidden descendant focus into the replacement, preserving scroll offsets.</summary>
    /// <param name="firstRender">Whether this is the first render.</param>
    /// <returns>A task that completes when focus has moved.</returns>
    protected override Task OnAfterRenderAsync(bool firstRender)
    {
        if (!restoreFocus || layers.Count == 0) return Task.CompletedTask;
        restoreFocus = false;
        var current = layers[^1].Element.Element;
        var requested = InitialFocus?.Invoke()?.Element;
        var target = requested is not null && current is not null && ControlTree.Ancestors(requested).Contains(current) && ControlTree.IsFocusable(requested)
            ? requested : current is not null ? ControlTree.Descendants(current).FirstOrDefault(ControlTree.IsFocusable) : null;
        return (target ?? Element.Element) is { } element && element.GetFeature<IElementInteraction>() is { } input
            ? input.FocusAsync(element, preventScroll: true) : Task.CompletedTask;
    }

    /// <summary>Detaches current host-preference notifications; child Presence components cancel their retained exits.</summary>
    public void Dispose() => motion?.Dispose();

    private sealed class Layer(object? key, long generation, RenderFragment? content, ContentTransitionKind transition, TransitionDirection direction)
    {
        public object? Key { get; } = key;
        public long Generation { get; } = generation;
        public RenderFragment? Content { get; set; } = content;
        public ContentTransitionKind Transition { get; set; } = transition;
        public TransitionDirection Direction { get; set; } = direction;
        public ElementReference Element { get; set; }
    }
}
