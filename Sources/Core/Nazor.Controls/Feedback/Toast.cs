using Nazor.Controls.Primitives;
using Nazor.Input;
using Nazor.Rendering;

namespace Nazor.Controls;

/// <summary>The corner or edge of the window at which a <see cref="ToastHost" /> stacks its notifications.</summary>
public enum ToastPlacement
{
    /// <summary>The top corner at the start of the reading direction (<c>top-start</c>).</summary>
    TopStart,
    /// <summary>The middle of the top edge (<c>top-center</c>).</summary>
    TopCenter,
    /// <summary>The top corner at the end of the reading direction (<c>top-end</c>).</summary>
    TopEnd,
    /// <summary>The bottom corner at the start of the reading direction (<c>bottom-start</c>).</summary>
    BottomStart,
    /// <summary>The middle of the bottom edge (<c>bottom-center</c>).</summary>
    BottomCenter,
    /// <summary>The bottom corner at the end of the reading direction (<c>bottom-end</c>).</summary>
    BottomEnd,
}

/// <summary>
/// The look of one notification, usually rendered by a <see cref="ToastHost" /> for each entry of its
/// <see cref="ToastQueue" />. The parts are <c>ToastMark</c> (a mark for the <see cref="Severity" />), <c>ToastBody</c>
/// with <c>ToastTitle</c>, <c>ToastMessage</c> and the content between the tags, the <c>ToastAction</c> button
/// (<see cref="ActionText" />), the <c>ToastClose</c> button (<see cref="Dismissible" />) and the <c>ToastTimer</c>
/// part, which publishes the remaining time as <c>--nz-toast-remaining</c> (1 to 0) while the toast counts down.
/// <see cref="OnHoldChanged" /> reports when the pointer or the keyboard focus arrives on the toast and when both have
/// left, so the host can pause the countdown; the element has the <c>paused</c> attribute meanwhile.
/// </summary>
public sealed class Toast : ContentControl
{
    private bool hovered;
    private bool focused;

    /// <summary>Gets or sets the importance of the notification.</summary>
    [Parameter] public Severity Severity { get; set; }

    /// <summary>Gets or sets the bold title shown before the message.</summary>
    [Parameter] public string? Title { get; set; }

    /// <summary>Gets or sets the main text.</summary>
    [Parameter] public string? Message { get; set; }

    /// <summary>Gets or sets the text of the action button, such as Undo, or null for none.</summary>
    [Parameter] public string? ActionText { get; set; }

    /// <summary>Gets or sets the callback invoked when the action button is pressed.</summary>
    [Parameter] public EventCallback OnAction { get; set; }

    /// <summary>Gets or sets whether a close button is shown; true by default.</summary>
    [Parameter] public bool Dismissible { get; set; } = true;

    /// <summary>Gets or sets the callback invoked when the close button is pressed.</summary>
    [Parameter] public EventCallback OnClose { get; set; }

    /// <summary>Gets or sets the remaining fraction of the display time, from 1 to 0, or null when the toast does not expire.</summary>
    [Parameter] public double? RemainingFraction { get; set; }

    /// <summary>Gets or sets whether the countdown is suspended.</summary>
    [Parameter] public bool IsPaused { get; set; }

    /// <summary>
    /// Gets or sets the callback invoked with true when the pointer or the keyboard focus arrives on the toast and with
    /// false when both have left it.
    /// </summary>
    [Parameter] public EventCallback<bool> OnHoldChanged { get; set; }

    /// <summary>Gets whether the pointer or the keyboard focus is on the toast.</summary>
    public bool IsHeld => hovered || focused;

    /// <summary>Gets the NSS element name emitted by Toast.</summary>
    protected override string ElementName => "Toast";

    /// <summary>Validates the parameters.</summary>
    protected override void OnParametersSet()
    {
        FeedbackParts.Validate(Severity, nameof(Severity));
        if (RemainingFraction is { } fraction && (!double.IsFinite(fraction) || fraction < 0 || fraction > 1))
            throw new ArgumentOutOfRangeException(nameof(RemainingFraction), "The remaining fraction is between 0 and 1.");
    }

    /// <summary>Adds the role, the name, the severity and the hold tracking.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void AddAttributes(RenderTreeBuilder builder)
    {
        builder.AddAttribute(10, "role", "group");
        builder.AddAttribute(11, "severity", Vocabulary.Name(Severity));
        builder.AddAttribute(12, "paused", IsPaused);
        if (AccessibleName is null) builder.AddAttribute(13, "accessible-name", Title ?? Message);
        if (AccessibleDescription is null && Title is not null) builder.AddAttribute(14, "accessible-description", Message);
        builder.AddAttribute(15, "__internal_onpointerenter#toast", EventCallback.Factory.Create<PointerEventArgs>(this, () => SetHeldAsync(true, focused)));
        builder.AddAttribute(16, "__internal_onpointerleave#toast", EventCallback.Factory.Create<PointerEventArgs>(this, () => SetHeldAsync(false, focused)));
        builder.AddAttribute(17, "__internal_onfocusin#toast", EventCallback.Factory.Create<FocusEventArgs>(this, () => SetHeldAsync(hovered, true)));
        builder.AddAttribute(18, "__internal_onfocusout#toast", EventCallback.Factory.Create<FocusEventArgs>(this, FocusOutAsync));
    }

    /// <summary>Adds the mark, the body, the buttons and the timer.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void AddContent(RenderTreeBuilder builder)
    {
        FeedbackParts.Mark(builder, 1000, "ToastMark", Severity);
        builder.OpenElement(1010, "ToastBody");
        if (Title is not null)
        {
            builder.OpenElement(1011, "ToastTitle");
            builder.AddContent(1012, Title);
            builder.CloseElement();
        }
        if (Message is not null)
        {
            builder.OpenElement(1013, "ToastMessage");
            builder.AddContent(1014, Message);
            builder.CloseElement();
        }
        builder.AddContent(1015, ChildContent);
        builder.CloseElement();
        if (ActionText is not null) FeedbackParts.Button(builder, 1020, "ToastAction", ActionText, null, this, () => OnAction.InvokeAsync());
        if (Dismissible) FeedbackParts.CloseButton(builder, 1030, "ToastClose", this, () => OnClose.InvokeAsync());
        if (RemainingFraction is { } remaining)
        {
            builder.OpenElement(1040, "ToastTimer");
            builder.AddAttribute(1041, "semantic-hidden", true);
            builder.AddAttribute(1042, "paused", IsPaused);
            builder.AddAttribute(1043, "style", "--nz-toast-remaining: " + Css.Number(remaining));
            builder.CloseElement();
        }
    }

    private Task FocusOutAsync(FocusEventArgs e)
    {
        if (e.RelatedTarget is { } next && Element.Element is { } self && ControlTree.Ancestors(next).Contains(self)) return Task.CompletedTask;
        return SetHeldAsync(hovered, false);
    }

    private async Task SetHeldAsync(bool pointer, bool focus)
    {
        var was = IsHeld;
        hovered = pointer;
        focused = focus;
        if (was != IsHeld) await OnHoldChanged.InvokeAsync(IsHeld);
    }
}

/// <summary>
/// Shows the toast notifications of a <see cref="ToastQueue" /> stacked at a corner of the window, above everything
/// else, and gives the queue to its content: any descendant can declare <c>[CascadingParameter] ToastQueue Toasts</c>
/// and call <c>Toasts.Show(...)</c>. Place it once around the content of a window. The host drives the countdowns
/// (pausing them while the pointer or the focus is on a toast), removes expired toasts, runs the action of a toast
/// before closing it, and announces each new toast through a live region (errors at once). The element
/// <c>ToastHost</c> holds the content; the overlay part <c>ToastHostRegion</c> (placement <c>none</c>, with the
/// <c>toast-placement</c> attribute) is positioned by NSS and uses a bounded <see cref="ItemsTransition{ToastEntry}" /> collection for current and outgoing <see cref="Toast" /> entries; the
/// <c>ToastHostAnnouncer</c> part is the hidden live region.
/// </summary>
public sealed class ToastHost : ContentControl, IDisposable
{
    private static readonly TimeSpan Tick = TimeSpan.FromMilliseconds(250);
    private ToastQueue? own;
    private ToastQueue? observed;
    private ITimer? timer;
    private long announcedId;
    private ToastEntry? announcement;
    private bool disposed;
    private int renderedToastCount;

    /// <summary>Gets or sets the queue to show; null uses a queue owned by the host.</summary>
    [Parameter] public ToastQueue? Queue { get; set; }

    /// <summary>
    /// Gets or sets the clock used for the countdown timers and for the host's own queue, or null for the system clock.
    /// When <see cref="Queue" /> is set, pass the same clock the queue was created with.
    /// </summary>
    [Parameter] public TimeProvider? Time { get; set; }

    /// <summary>Gets or sets where the notifications are stacked.</summary>
    [Parameter] public ToastPlacement Placement { get; set; } = ToastPlacement.BottomEnd;

    /// <summary>Gets or sets how many notifications are on screen at once; the others wait. 3 by default.</summary>
    [Parameter] public int MaxVisible { get; set; } = 3;

    /// <summary>Gets the queue shown by the host.</summary>
    public ToastQueue Toasts => observed ?? throw new InvalidOperationException("The host has not been rendered yet.");

    private TimeProvider Clock => Time ?? TimeProvider.System;

    /// <summary>Gets the NSS element name emitted by ToastHost.</summary>
    protected override string ElementName => "ToastHost";

    /// <summary>Validates the parameters and follows the queue.</summary>
    protected override void OnParametersSet()
    {
        if (!Enum.IsDefined(Placement)) throw new ArgumentOutOfRangeException(nameof(Placement));
        if (MaxVisible < 1) throw new ArgumentOutOfRangeException(nameof(MaxVisible), "At least one toast must be visible.");
        var queue = Queue ?? (own ??= new ToastQueue(Time));
        if (!ReferenceEquals(queue, observed))
        {
            if (observed is not null) observed.Changed -= QueueChanged;
            observed = queue;
            observed.Changed += QueueChanged;
        }
        if (queue.MaxVisible != MaxVisible) queue.MaxVisible = MaxVisible;
        Announce();
        Schedule();
    }

    /// <summary>Adds the placement attribute.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void AddAttributes(RenderTreeBuilder builder) => builder.AddAttribute(10, "toast-placement", PlacementName(Placement));

    /// <summary>Adds the cascading queue, the live region and a keyed overlay retaining bounded noninteractive outgoing toasts. An empty carrier remains mounted but hidden and is not an overlay.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void AddContent(RenderTreeBuilder builder)
    {
        var queue = Toasts;
        builder.OpenComponent<CascadingValue<ToastQueue>>(1000);
        builder.AddComponentParameter(1001, "Value", queue);
        builder.AddComponentParameter(1002, "ChildContent", ChildContent);
        builder.CloseComponent();
        builder.OpenComponent<LiveRegion>(1010);
        builder.AddComponentParameter(1011, nameof(LiveRegion.As), "ToastHostAnnouncer");
        builder.AddComponentParameter(1012, nameof(LiveRegion.Politeness), announcement?.Options.Severity == Severity.Error ? LivePoliteness.Assertive : LivePoliteness.Polite);
        builder.AddComponentParameter(1013, nameof(LiveRegion.ChildContent), (RenderFragment)RenderAnnouncement);
        builder.CloseComponent();
        var visible = queue.Visible;
        var shown = visible.Count > 0 || renderedToastCount > 0;
        builder.OpenElement(1020, "ToastHostRegion");
        builder.AddAttribute(1021, "overlay", shown);
        builder.AddAttribute(1022, "placement", "none");
        builder.AddAttribute(1023, "light-dismiss", false);
        builder.AddAttribute(1024, "escape-dismiss", false);
        builder.AddAttribute(1025, "toast-placement", PlacementName(Placement));
        builder.AddAttribute(1026, "role", "group");
        builder.AddAttribute(1027, "accessible-name", ControlStrings.Get("Notifications"));
        builder.AddAttribute(1028, "semantic-hidden", visible.Count == 0);
        builder.AddAttribute(1029, "disabled", visible.Count == 0);
        builder.AddAttribute(1030, "style", shown ? null : "display: none");
        builder.OpenComponent<ItemsTransition<ToastEntry>>(1040);
        builder.AddComponentParameter(1041, nameof(ItemsTransition<ToastEntry>.Items), visible);
        builder.AddComponentParameter(1042, nameof(ItemsTransition<ToastEntry>.ItemKey), (Func<ToastEntry, object>)(entry => entry));
        builder.AddComponentParameter(1043, nameof(ItemsTransition<ToastEntry>.MaximumRetainedItems), 32);
        builder.AddComponentParameter(1044, nameof(ItemsTransition<ToastEntry>.PreserveExtentDuringExit), true);
        builder.AddComponentParameter(1045, nameof(ItemsTransition<ToastEntry>.RenderedCountChanged), EventCallback.Factory.Create<int>(this, RenderedToastsChanged));
        builder.AddComponentParameter(1046, nameof(ItemsTransition<ToastEntry>.ItemTemplate), (RenderFragment<ToastEntry>)(entry => content => RenderToast(content, entry)));
        builder.CloseComponent();
        builder.CloseElement();
    }

    private void RenderedToastsChanged(int count)
    {
        if (renderedToastCount == count) return;
        renderedToastCount = count;
        StateHasChanged();
    }

    private void RenderToast(RenderTreeBuilder builder, ToastEntry entry)
    {
        var clock = Clock;
        var options = entry.Options;
        builder.OpenComponent<Toast>(0);
        builder.AddComponentParameter(1, nameof(Toast.Severity), options.Severity);
        builder.AddComponentParameter(2, nameof(Toast.Title), options.Title);
        builder.AddComponentParameter(3, nameof(Toast.Message), options.Message);
        builder.AddComponentParameter(4, nameof(Toast.ActionText), options.ActionText);
        builder.AddComponentParameter(5, nameof(Toast.OnAction), EventCallback.Factory.Create(this, () => RunActionAsync(entry)));
        builder.AddComponentParameter(6, nameof(Toast.Dismissible), options.Dismissible);
        builder.AddComponentParameter(7, nameof(Toast.OnClose), EventCallback.Factory.Create(this, () => Toasts.Dismiss(entry)));
        builder.AddComponentParameter(8, nameof(Toast.RemainingFraction), Fraction(entry, clock.GetUtcNow()));
        builder.AddComponentParameter(9, nameof(Toast.IsPaused), entry.IsPaused);
        builder.AddComponentParameter(10, nameof(Toast.OnHoldChanged), EventCallback.Factory.Create<bool>(this, held => Hold(entry, held)));
        var countdownStyle = ReferenceEquals(clock, TimeProvider.System) && entry.CountdownDuration is { } duration && options.Duration is { } total
            ? $"--nz-toast-countdown-start: {Css.Number(duration / total)}; --nz-toast-countdown-duration: {Css.Number(duration.TotalMilliseconds)}ms"
            : null;
        builder.AddComponentParameter(11, nameof(Toast.Style), countdownStyle);
        builder.AddComponentParameter(12, "countdown", countdownStyle is not null);
        builder.CloseComponent();
    }

    private void RenderAnnouncement(RenderTreeBuilder builder)
    {
        if (announcement is not { } entry) return;
        builder.OpenElement(0, "ToastHostAnnouncement");
        builder.SetKey(entry.Id);
        builder.AddContent(1, entry.Options.Title is { } title ? title + ": " + entry.Options.Message : entry.Options.Message);
        builder.CloseElement();
    }

    private static double? Fraction(ToastEntry entry, DateTimeOffset now) =>
        entry.Options.Duration is { } total && entry.Remaining(now) is { } left ? Math.Clamp(left / total, 0, 1) : null;

    private void Hold(ToastEntry entry, bool held)
    {
        if (!Toasts.Visible.Contains(entry)) return;
        if (held) Toasts.Pause(entry);
        else Toasts.Resume(entry);
    }

    private async Task RunActionAsync(ToastEntry entry)
    {
        if (entry.Options.Action is { } action) await action();
        Toasts.Dismiss(entry);
    }

    private void QueueChanged() => _ = InvokeAsync(() =>
    {
        if (disposed) return;
        Announce();
        Schedule();
        StateHasChanged();
    });

    private void Announce()
    {
        var newest = observed?.Visible.Where(entry => entry.Id > announcedId).LastOrDefault();
        if (newest is null) return;
        announcedId = newest.Id;
        announcement = newest;
    }

    private void Schedule()
    {
        timer?.Dispose();
        timer = null;
        if (disposed || observed?.NextDeadline is not { } deadline) return;
        var due = deadline - Clock.GetUtcNow();
        if (due < TimeSpan.Zero) due = TimeSpan.Zero;
        if (due > Tick) due = Tick;
        timer = Clock.CreateTimer(_ => _ = InvokeAsync(Elapsed), null, due, Timeout.InfiniteTimeSpan);
    }

    private void Elapsed()
    {
        if (disposed || observed is null) return;
        if (observed.Expire()) return;
        Schedule();
        StateHasChanged();
    }

    private static string PlacementName(ToastPlacement placement) => placement switch
    {
        ToastPlacement.TopStart => "top-start", ToastPlacement.TopCenter => "top-center", ToastPlacement.TopEnd => "top-end",
        ToastPlacement.BottomStart => "bottom-start", ToastPlacement.BottomCenter => "bottom-center", _ => "bottom-end",
    };

    /// <summary>Stops following the queue and the countdown timer.</summary>
    public void Dispose()
    {
        disposed = true;
        if (observed is not null) observed.Changed -= QueueChanged;
        timer?.Dispose();
        timer = null;
    }
}
