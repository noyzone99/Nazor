using Nazor.Input;
using Nazor.Rendering;

namespace Nazor.Controls.Primitives;

/// <summary>The state given to the content of a <see cref="Presence" />.</summary>
/// <param name="IsPresent">Whether the content is wanted; false while it exits.</param>
/// <param name="IsExiting">Whether the content is playing its exit before being removed.</param>
public readonly record struct PresenceState(bool IsPresent, bool IsExiting);

/// <summary>
/// Keeps content rendered for <see cref="ExitDuration" /> after <see cref="Present" /> becomes false, so a theme can play
/// an exit animation: the content receives <see cref="PresenceState.IsExiting" /> and usually writes it as an
/// <c>exiting</c> attribute. Without a duration the exit finishes at once. <see cref="KeepMounted"/> preserves the same
/// subtree after completion; the caller owns its closed input and accessibility policy.
/// </summary>
public sealed class Presence : ComponentBase, IDisposable
{
    private PresenceLifetime? lifetime;

    /// <summary>Gets or sets whether the content is wanted.</summary>
    [Parameter] public bool Present { get; set; }

    /// <summary>Gets or sets how long exiting content stays rendered.</summary>
    [Parameter] public TimeSpan ExitDuration { get; set; }

    /// <summary>Gets or sets whether the same content remains rendered after its exit finishes; false by default.</summary>
    [Parameter] public bool KeepMounted { get; set; }

    /// <summary>Gets or sets the content, which receives the presence state.</summary>
    [Parameter] public RenderFragment<PresenceState>? ChildContent { get; set; }

    /// <summary>Gets or sets the callback invoked when exit retention completes.</summary>
    [Parameter] public EventCallback OnExited { get; set; }

    /// <summary>Starts or cancels the exit.</summary>
    protected override async Task OnParametersSetAsync()
    {
        if (ExitDuration < TimeSpan.Zero || ExitDuration.TotalMilliseconds > uint.MaxValue - 1) throw new ArgumentOutOfRangeException(nameof(ExitDuration));
        lifetime ??= new(InvokeAsync, StateHasChanged, () => OnExited.InvokeAsync());
        await lifetime.UpdateAsync(Present, ExitDuration);
    }

    /// <summary>Renders the content while present, exiting or explicitly kept mounted.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        if ((KeepMounted || lifetime?.Rendered == true) && ChildContent is not null)
            builder.AddContent(0, ChildContent(new PresenceState(Present, lifetime?.Exiting == true)));
    }

    internal Task RestartExitAsync() => !Present && lifetime?.Exiting == true
        ? lifetime.UpdateAsync(false, ExitDuration, restartExit: true) : Task.CompletedTask;

    /// <summary>Stops a pending exit.</summary>
    public void Dispose() => lifetime?.Dispose();
}

/// <summary>
/// A region that opens and closes, without built-in look: <see cref="IsOpen" /> supports <c>@bind-IsOpen</c>; the content
/// is rendered inside a <c>{name}Content</c> part while open, retained for exit, or kept mounted with <see cref="KeepMounted" />.
/// Its natural height is measured independently of the clipped viewport and published as <c>--nz-collapsible-height</c>. Expanders,
/// accordions, tree nodes and collapsible groups are built on it.
/// Themes can override <c>--nz-collapsible-viewport</c> to retain a full clip while content translates. The root exposes
/// <c>expanded</c>, <c>exiting</c> and <c>closed</c>; the content exposes <c>revealed</c> after its initial measured layout.
/// </summary>
public sealed class Collapsible : ContentControl, IDisposable
{
    private readonly ControllableState<bool> open = new(false);
    private double? height;
    private bool retained;
    private PresenceMotion? motion;
    private Presence? presence;
    private bool contentRealized;
    private bool revealed;
    private bool awaitingReveal;
    private bool awaitingNaturalMeasurement;
    private double viewportHeight;

    /// <summary>Gets or sets the element name, <c>Collapsible</c> by default.</summary>
    [Parameter] public string As { get; set; } = "Collapsible";

    /// <summary>Gets or sets whether the region is open.</summary>
    [Parameter] public bool IsOpen { get; set; }

    /// <summary>Gets or sets the callback invoked when the region opens or closes from <see cref="ToggleAsync" />.</summary>
    [Parameter] public EventCallback<bool> IsOpenChanged { get; set; }

    /// <summary>Gets or sets whether closed content stays mounted and naturally measured, disabled and hidden from accessibility.</summary>
    [Parameter] public bool KeepMounted { get; set; }

    /// <summary>Gets or sets the element that receives focus when closing hides focused content; null clears focus normally.</summary>
    [Parameter] public ElementReference? ReturnFocus { get; set; }

    /// <summary>Gets or sets whether the region rejects toggling.</summary>
    [Parameter] public bool Disabled { get; set; }

    /// <summary>Gets whether the region is currently open.</summary>
    public bool Opened => open.Value;

    /// <summary>Gets the identifier of the content part, for <c>controls</c> relationships.</summary>
    public string ContentId { get; } = UniqueId.Create("collapsible");

    /// <summary>Gets the element name.</summary>
    protected override string ElementName => As;

    /// <summary>Gets the natural-height variable, overridable clipped viewport height and caller's declarations.</summary>
    protected override string? ElementStyle => Css.Join(Css.Join(
        height is { } measured ? "--nz-collapsible-height: " + Css.Px(measured) : null,
        "height: var(--nz-collapsible-viewport, " + Css.Px(open.Value && !awaitingNaturalMeasurement ? height ?? 0 : 0) + "); overflow: hidden"), Style);

    /// <summary>Synchronizes the open state.</summary>
    protected override async Task OnParametersSetAsync()
    {
        var wasOpen = open.Value;
        open.Sync(IsOpen);
        if (wasOpen != open.Value)
        {
            UpdateReveal(open.Value);
            if (!open.Value) await RestoreFocusAsync();
        }
        if (!open.Value && !retained && !KeepMounted) contentRealized = false;
    }

    /// <summary>Adds the expanded and disabled states.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void AddAttributes(RenderTreeBuilder builder)
    {
        builder.AddAttribute(10, "expanded", open.Value);
        builder.AddAttribute(11, "disabled", Disabled);
        builder.AddAttribute(12, "exit-presence", true);
        builder.AddAttribute(13, "exiting", !open.Value && retained);
        builder.AddAttribute(14, "semantic-hidden", !open.Value);
        builder.AddAttribute(15, "closed", !open.Value && !retained);
        builder.AddAttribute(16, "__internal_onresize#collapsible", EventCallback.Factory.Create<ResizeEventArgs>(this, ViewportMeasured));
        builder.AddAttribute(17, "measuring", open.Value && awaitingNaturalMeasurement);
    }

    /// <summary>Adds naturally measured content while open, exiting or kept mounted.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void AddContent(RenderTreeBuilder builder)
    {
        motion ??= new(() => _ = InvokeAsync(StateHasChanged));
        builder.OpenComponent<Presence>(1000);
        builder.AddComponentParameter(1001, nameof(Presence.Present), open.Value);
        builder.AddComponentParameter(1002, nameof(Presence.ExitDuration), motion.Duration(Element.Element));
        builder.AddComponentParameter(1003, nameof(Presence.OnExited), EventCallback.Factory.Create(this, () =>
        {
            retained = false;
            if (!KeepMounted) contentRealized = false;
            StateHasChanged();
        }));
        builder.AddComponentParameter(1004, nameof(Presence.ChildContent), (RenderFragment<PresenceState>)(_ => content =>
        {
            contentRealized = true;
            content.OpenElement(0, As + "Content");
            content.AddAttribute(1, "id", ContentId);
            content.AddAttribute(2, "expanded", open.Value);
            content.AddAttribute(3, "__internal_onresize", EventCallback.Factory.Create<ResizeEventArgs>(this, Measured));
            content.AddAttribute(4, "disabled", !open.Value);
            content.AddAttribute(5, "semantic-hidden", !open.Value);
            content.AddAttribute(6, "style", open.Value ? null : "pointer-events: none");
            content.AddAttribute(7, "revealed", open.Value && revealed);
            content.AddContent(8, ChildContent);
            content.CloseElement();
        }));
        builder.AddComponentParameter(1005, nameof(Presence.KeepMounted), KeepMounted);
        builder.AddComponentReferenceCapture(1006, component => presence = (Presence)component);
        builder.CloseComponent();
    }

    private async Task Measured(ResizeEventArgs e)
    {
        var wasAwaiting = awaitingNaturalMeasurement;
        awaitingNaturalMeasurement = false;
        if (height == e.Height)
        {
            TryReveal();
            if (wasAwaiting) StateHasChanged();
            return;
        }
        height = e.Height;
        if (!open.Value && retained && presence is not null) await presence.RestartExitAsync();
        if (e.Height == 0) TryReveal();
        StateHasChanged();
    }

    private void ViewportMeasured(ResizeEventArgs e)
    {
        viewportHeight = e.Height;
        TryReveal();
    }

    private void TryReveal()
    {
        if (!open.Value || !awaitingReveal || awaitingNaturalMeasurement || height is not { } natural || viewportHeight != natural) return;
        awaitingReveal = false;
        revealed = true;
        StateHasChanged();
    }

    private void UpdateReveal(bool value)
    {
        revealed = value && contentRealized && height is not null;
        awaitingReveal = value && !revealed;
        awaitingNaturalMeasurement = awaitingReveal;
        if (value) retained = true;
    }

    /// <summary>Opens the region when closed and closes it when open.</summary>
    /// <returns>A task that completes when the change has been reported.</returns>
    public Task ToggleAsync() => SetOpenAsync(!open.Value);

    /// <summary>Opens or closes the region and reports the change.</summary>
    /// <param name="value">Whether the region should be open.</param>
    /// <returns>A task that completes when the change has been reported.</returns>
    public async Task SetOpenAsync(bool value)
    {
        if (Disabled || !open.Set(value)) return;
        UpdateReveal(value);
        if (!value) await RestoreFocusAsync();
        StateHasChanged();
        await IsOpenChanged.InvokeAsync(value);
    }

    private Task RestoreFocusAsync() => Element.Element is { } surface && surface.States.HasFlag(ElementStates.FocusWithin) &&
        ReturnFocus?.Element is { IsRemoved: false } target && target.GetFeature<IElementInteraction>() is { } input
            ? input.FocusAsync(target, preventScroll: true) : Task.CompletedTask;

    /// <summary>Detaches the region from host motion-preference notifications.</summary>
    public void Dispose() => motion?.Dispose();
}

/// <summary>
/// Delays opening and closing driven by the pointer, as submenus, hover cards and the flyouts of a compact navigation
/// pane do: <see cref="Enter" /> schedules the open callback after <see cref="OpenDelay" />, <see cref="Leave" /> schedules
/// the close callback after <see cref="CloseDelay" />, and moving back cancels the pending change so a brief exit does not
/// flicker. Callbacks run through the dispatcher supplied at construction. Every member must be used from the UI thread.
/// </summary>
/// <param name="dispatch">Runs work on the UI thread, usually a component's <c>InvokeAsync</c>.</param>
/// <param name="open">The work run to open.</param>
/// <param name="close">The work run to close.</param>
/// <param name="timeProvider">The timer provider, or null for TimeProvider.System. Timers dispatch work through the supplied UI dispatcher.</param>
public sealed class HoverIntent(Func<Func<Task>, Task> dispatch, Func<Task> open, Func<Task> close, TimeProvider? timeProvider = null) : IDisposable
{
    private ITimer? timer;
    private int generation;

    /// <summary>Gets or sets the delay before opening.</summary>
    public TimeSpan OpenDelay { get; set; } = TimeSpan.FromMilliseconds(250);

    /// <summary>Gets or sets the delay before closing.</summary>
    public TimeSpan CloseDelay { get; set; } = TimeSpan.FromMilliseconds(300);

    /// <summary>Schedules opening and cancels a pending close.</summary>
    public void Enter() => Schedule(open, OpenDelay);

    /// <summary>Schedules closing and cancels a pending open.</summary>
    public void Leave() => Schedule(close, CloseDelay);

    /// <summary>Cancels any pending change.</summary>
    public void Cancel()
    {
        generation++;
        timer?.Dispose();
        timer = null;
    }

    private void Schedule(Func<Task> work, TimeSpan delay)
    {
        Cancel();
        var current = generation;
        if (delay <= TimeSpan.Zero)
        {
            _ = dispatch(work);
            return;
        }
        timer = (timeProvider ?? TimeProvider.System).CreateTimer(_ => dispatch(() => current == generation ? work() : Task.CompletedTask), null, delay, Timeout.InfiniteTimeSpan);
    }

    /// <summary>Cancels any pending change.</summary>
    public void Dispose() => Cancel();
}
