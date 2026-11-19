using Nazor.Input;
using Nazor.Rendering;

namespace Nazor.Controls.Primitives;

/// <summary>The side of its anchor on which an anchored overlay opens; it moves to the opposite side when there is no room.</summary>
public enum OverlayPlacement
{
    /// <summary>Below the anchor.</summary>
    Bottom,
    /// <summary>Above the anchor.</summary>
    Top,
    /// <summary>On the right of the anchor.</summary>
    Right,
    /// <summary>On the left of the anchor.</summary>
    Left,
}

/// <summary>How an anchored overlay lines up with its anchor along the side it opens on.</summary>
public enum OverlayAlignment
{
    /// <summary>Aligned with the anchor's left edge, or its top edge on the left and right sides.</summary>
    Start,
    /// <summary>Centered on the anchor.</summary>
    Center,
    /// <summary>Aligned with the anchor's right edge, or its bottom edge on the left and right sides.</summary>
    End,
}

/// <summary>Where a <see cref="DialogContainer" /> appears in the window.</summary>
public enum DialogPlacement
{
    /// <summary>Centered in the window.</summary>
    Center,
    /// <summary>Along the top edge, as a sheet.</summary>
    Top,
    /// <summary>Along the bottom edge, as a bottom sheet.</summary>
    Bottom,
    /// <summary>Along the leading edge, as a navigation drawer.</summary>
    Start,
    /// <summary>Along the trailing edge, as a side panel.</summary>
    End,
    /// <summary>Covering the whole window.</summary>
    FullScreen,
}

/// <summary>
/// The shared behavior of overlay primitives: an open state that the parent can control (<c>@bind-IsOpen</c>), dismissal
/// by the pointer outside or by Escape reported through <see cref="OnDismiss" />, and focus restored on close.
/// Click, key-down and wheel events do not bubble from the surface into its logical owner.
/// </summary>
public abstract class OverlayContainer : ContentControl, IDisposable
{
    private readonly ControllableState<bool> open = new(false);
    private PresenceMotion? motion;
    private bool exiting;
    private bool reentered;

    /// <summary>Gets or sets the element name, so the surface can be styled like a part.</summary>
    [Parameter] public string? As { get; set; }

    /// <summary>Gets or sets whether the overlay is open.</summary>
    [Parameter] public bool IsOpen { get; set; }

    /// <summary>Gets or sets the callback invoked when the open state changes from inside the overlay.</summary>
    [Parameter] public EventCallback<bool> IsOpenChanged { get; set; }

    /// <summary>Gets or sets whether a press outside the overlay dismisses it.</summary>
    [Parameter] public bool CloseOnOutside { get; set; } = true;

    /// <summary>Gets or sets whether Escape dismisses the overlay.</summary>
    [Parameter] public bool CloseOnEscape { get; set; } = true;

    /// <summary>Gets or sets the callback invoked when the user dismisses the overlay; it then closes.</summary>
    [Parameter] public EventCallback OnDismiss { get; set; }

    /// <summary>Gets or sets the element that receives the focus when the overlay closes; null uses the one focused before.</summary>
    [Parameter] public ElementReference? ReturnFocus { get; set; }

    /// <summary>Gets or sets a callback that chooses the element focused when the overlay opens.</summary>
    [Parameter] public Func<ElementReference?>? InitialFocus { get; set; }

    /// <summary>Gets or sets the exit retention time; null follows the theme's overlay-exit-duration. Reduced motion always removes the surface immediately.</summary>
    [Parameter] public TimeSpan? ExitDuration { get; set; }

    /// <summary>Gets or sets the callback invoked when the closing surface's retention ends.</summary>
    [Parameter] public EventCallback OnExited { get; set; }

    /// <summary>Gets whether the overlay is currently open.</summary>
    public bool Opened => open.Value;

    /// <summary>Gets the default element name.</summary>
    protected abstract string DefaultName { get; }

    /// <summary>Gets the element name of the surface.</summary>
    protected override string ElementName => As ?? DefaultName;

    /// <summary>Synchronizes the open state.</summary>
    protected override void OnParametersSet()
    {
        if (ExitDuration is { } duration && (duration < TimeSpan.Zero || duration.TotalMilliseconds > uint.MaxValue - 1))
            throw new ArgumentOutOfRangeException(nameof(ExitDuration));
        open.Sync(IsOpen);
    }

    /// <summary>Gets the caller's declarations and disables pointer interaction while the surface exits.</summary>
    protected override string? ElementStyle => exiting ? Css.Join(Style, "pointer-events: none") : Style;

    /// <summary>Renders the surface while open and retains it for a theme-controlled, noninteractive exit.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        motion ??= new(() => _ = InvokeAsync(StateHasChanged));
        var duration = motion.Duration(Element.Element, ExitDuration);
        OpenDelegatedComponent<Presence>(builder, 0);
        builder.AddComponentParameter(1, nameof(Presence.Present), open.Value);
        builder.AddComponentParameter(2, nameof(Presence.ExitDuration), duration);
        builder.AddComponentParameter(3, nameof(Presence.OnExited), EventCallback.Factory.Create(this, ExitedAsync));
        builder.AddComponentParameter(4, nameof(Presence.ChildContent), (RenderFragment<PresenceState>)(state => content =>
        {
            if (exiting && !state.IsExiting) reentered = true;
            exiting = state.IsExiting;
            RenderBefore(content);
            content.OpenRegion(100);
            base.BuildRenderTree(content);
            content.CloseRegion();
        }));
        builder.CloseComponent();
    }

    /// <summary>Renders elements placed before the surface, such as a scrim.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected virtual void RenderBefore(RenderTreeBuilder builder)
    {
    }

    /// <summary>Adds the overlay, dismissal and focus attributes.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void AddAttributes(RenderTreeBuilder builder)
    {
        builder.AddAttribute(10, "overlay", true);
        builder.AddAttribute(11, "light-dismiss", !exiting && CloseOnOutside);
        builder.AddAttribute(12, "escape-dismiss", !exiting && CloseOnEscape);
        builder.AddAttribute(13, "__default_ondismiss", EventCallback.Factory.Create(this, DismissAsync));
        builder.AddAttribute(14, "return-focus", ReturnFocus?.Element);
        builder.AddAttribute(15, "initial-focus", InitialFocus);
        builder.AddEventStopPropagationAttribute(16, "onclick", true);
        builder.AddEventStopPropagationAttribute(17, "onkeydown", true);
        builder.AddEventStopPropagationAttribute(18, "onwheel", true);
        builder.AddAttribute(19, "exiting", exiting);
        AddSurfaceAttributes(builder);
        builder.AddAttribute(889, "reentered", reentered);
        if (exiting)
        {
            builder.AddAttribute(890, "disabled", true);
            builder.AddAttribute(891, "semantic-hidden", true);
        }
    }

    /// <summary>Adds attributes specific to the surface, with sequence numbers from 20 to 899.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected abstract void AddSurfaceAttributes(RenderTreeBuilder builder);

    /// <summary>Opens the overlay.</summary>
    /// <returns>A task that completes when the change has been reported.</returns>
    public Task OpenAsync() => SetOpenAsync(true);

    /// <summary>Closes the overlay.</summary>
    /// <returns>A task that completes when the change has been reported.</returns>
    public Task CloseAsync() => SetOpenAsync(false);

    /// <summary>Opens the overlay when closed and closes it when open.</summary>
    /// <returns>A task that completes when the change has been reported.</returns>
    public Task ToggleAsync() => SetOpenAsync(!open.Value);

    /// <summary>Changes the open state and reports it.</summary>
    /// <param name="value">The new open state.</param>
    /// <returns>A task that completes when the change has been reported.</returns>
    protected async Task SetOpenAsync(bool value)
    {
        if (!open.Set(value)) return;
        StateHasChanged();
        await IsOpenChanged.InvokeAsync(value);
    }

    private async Task DismissAsync()
    {
        if (!open.Value) return;
        await OnDismiss.InvokeAsync();
        await CloseAsync();
    }

    private async Task ExitedAsync()
    {
        exiting = false;
        reentered = false;
        await OnExited.InvokeAsync();
    }

    /// <summary>Detaches the retained surface from its host's motion-preference notifications.</summary>
    public virtual void Dispose()
    {
        motion?.Dispose();
    }
}

/// <summary>
/// An anchored, non-modal surface without built-in content or look: it opens next to <see cref="Anchor" /> (or at
/// <see cref="AnchorPoint" />), moves to the other side when there is no room, closes on an outside press or Escape and
/// returns the focus. Popovers, menus, drop-down lists and pickers are built on it. With <see cref="ShowArrow" /> it
/// renders an arrow part (<c>{name}Arrow</c>) aligned to the arranged edge and anchor center. Its <c>placement</c>
/// attribute records the requested side; use <c>:popup-above</c>, <c>:popup-left</c> and <c>:popup-right</c> on the
/// surface for the arranged side, including flips. Arrow margins provide clearance from rounded corners.
/// </summary>
public sealed class PopoverContainer : OverlayContainer
{
    /// <summary>Gets or sets the element the surface is attached to.</summary>
    [Parameter] public ElementReference? Anchor { get; set; }

    /// <summary>Gets or sets a point in window coordinates used as the anchor when <see cref="Anchor" /> is null.</summary>
    [Parameter] public (double X, double Y)? AnchorPoint { get; set; }

    /// <summary>Gets or sets the preferred side of the anchor.</summary>
    [Parameter] public OverlayPlacement Placement { get; set; }

    /// <summary>Gets or sets the alignment with the anchor.</summary>
    [Parameter] public OverlayAlignment Alignment { get; set; }

    /// <summary>Gets or sets a retained descendant whose vertical center aligns with the anchor's center. A visible, laid-out target overrides side placement; horizontal positioning still follows <see cref="Alignment" />. Removed, hidden or unrelated targets use ordinary placement. Work-area constraints can move the surface away from exact alignment.</summary>
    [Parameter] public ElementReference? AlignmentTarget { get; set; }

    /// <summary>Gets or sets the distance from the anchor, in logical units.</summary>
    [Parameter] public double Offset { get; set; }

    /// <summary>Gets or sets whether the surface is at least as wide as its anchor, as a drop-down list.</summary>
    [Parameter] public bool MatchAnchorWidth { get; set; }

    /// <summary>Gets or sets whether opening moves the focus into the surface.</summary>
    [Parameter] public bool FocusOnOpen { get; set; } = true;

    /// <summary>Gets or sets whether pressing the anchor interacts with this nonmodal popup instead of light-dismissing it. False by default. Active modal restrictions still apply; enabled anchors close or submit through their own actions.</summary>
    [Parameter] public bool AllowAnchorInteraction { get; set; }

    /// <summary>Gets or sets the accessible role of the surface; <c>group</c> by default.</summary>
    [Parameter] public string Role { get; set; } = "group";

    /// <summary>Gets or sets whether the surface uses menu keyboard behavior (arrows between items, Tab closes).</summary>
    [Parameter] public bool IsMenu { get; set; }

    /// <summary>Gets or sets whether the surface is a nested menu that closes its sibling submenus.</summary>
    [Parameter] public bool IsSubmenu { get; set; }

    /// <summary>Gets or sets whether an arrow part is rendered.</summary>
    [Parameter] public bool ShowArrow { get; set; }

    /// <summary>Gets or sets whether transforms originate at the arranged arrow tip instead of the NSS transform origin; false by default. Has no effect without an arrow and an anchor.</summary>
    [Parameter] public bool OriginAtArrow { get; set; }

    /// <summary>Gets the default element name.</summary>
    protected override string DefaultName => "PopoverContainer";

    /// <summary>Validates the placement parameters.</summary>
    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        if (!Enum.IsDefined(Placement)) throw new ArgumentOutOfRangeException(nameof(Placement));
        if (!Enum.IsDefined(Alignment)) throw new ArgumentOutOfRangeException(nameof(Alignment));
        if (!double.IsFinite(Offset)) throw new ArgumentOutOfRangeException(nameof(Offset));
        if (AnchorPoint is { } point && (!double.IsFinite(point.X) || !double.IsFinite(point.Y))) throw new ArgumentOutOfRangeException(nameof(AnchorPoint));
    }

    /// <summary>Adds the anchoring, the role and the focus behavior.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void AddSurfaceAttributes(RenderTreeBuilder builder)
    {
        builder.AddAttribute(20, "anchor", Anchor?.Element);
        if (Anchor is null && AnchorPoint is { } point)
        {
            builder.AddAttribute(21, "anchor-x", point.X);
            builder.AddAttribute(22, "anchor-y", point.Y);
        }
        builder.AddAttribute(23, "placement", PlacementText(Placement, Alignment));
        builder.AddAttribute(24, "overlay-offset", Offset == 0 ? null : Offset);
        builder.AddAttribute(25, "anchor-width", MatchAnchorWidth);
        builder.AddAttribute(26, "focus-first", FocusOnOpen);
        builder.AddAttribute(27, "role", Role);
        builder.AddAttribute(28, "menu", IsMenu);
        builder.AddAttribute(29, "submenu", IsSubmenu);
        builder.AddAttribute(30, "side", Placement.ToString().ToLowerInvariant());
        builder.AddAttribute(31, "overlay-alignment-target", AlignmentTarget?.Element);
        builder.AddAttribute(32, "anchor-interaction", AllowAnchorInteraction);
        builder.AddAttribute(33, "overlay-origin-at-arrow", OriginAtArrow);
    }

    /// <summary>Adds the arrow part and the content.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void AddContent(RenderTreeBuilder builder)
    {
        if (ShowArrow)
        {
            builder.OpenElement(1000, ElementName + "Arrow");
            builder.AddAttribute(1001, "placement", Placement.ToString().ToLowerInvariant());
            builder.AddAttribute(1002, "semantic-hidden", true);
            builder.AddAttribute(1003, "overlay-arrow", true);
            builder.CloseElement();
        }
        builder.AddContent(1010, ChildContent);
    }

    internal static string PlacementText(OverlayPlacement placement, OverlayAlignment alignment)
    {
        var side = placement.ToString().ToLowerInvariant();
        return alignment == OverlayAlignment.Start ? side : side + "-" + alignment.ToString().ToLowerInvariant();
    }
}

/// <summary>
/// A window-local dialog surface without built-in content or look: modal by default (the rest of the window rejects
/// interaction and the focus stays inside), centered or attached to an edge (<see cref="Placement" />), with an optional
/// scrim behind it (<c>{name}Scrim</c>). It focuses its first control on open and returns the focus on close.
/// Dialogs, alerts, drawers, sheets and command palettes are built on it. Name it with
/// <see cref="Control.AccessibleName" /> or <see cref="Control.LabeledBy" />.
/// </summary>
public sealed class DialogContainer : OverlayContainer
{
    /// <summary>Creates a dialog container that ignores presses outside it.</summary>
    public DialogContainer() => CloseOnOutside = false;

    /// <summary>Gets or sets whether interaction and the focus are confined to the dialog.</summary>
    [Parameter] public bool Modal { get; set; } = true;

    /// <summary>Gets or sets where the dialog appears.</summary>
    [Parameter] public DialogPlacement Placement { get; set; }

    /// <summary>Gets or sets whether a scrim covers the window behind the dialog; true by default for modal dialogs.</summary>
    [Parameter] public bool? ShowScrim { get; set; }

    /// <summary>Gets or sets the accessible role: <c>dialog</c> by default, <c>alertdialog</c> for decisions.</summary>
    [Parameter] public string Role { get; set; } = "dialog";

    /// <summary>Gets or sets whether Escape focuses and uses the part marked <c>dialog-cancel</c> rather than closing.</summary>
    [Parameter] public bool PreferCancel { get; set; }

    /// <summary>Gets the default element name.</summary>
    protected override string DefaultName => "DialogContainer";

    /// <summary>Validates the placement.</summary>
    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        if (!Enum.IsDefined(Placement)) throw new ArgumentOutOfRangeException(nameof(Placement));
        if (Role is not ("dialog" or "alertdialog")) throw new ArgumentException("A dialog role is 'dialog' or 'alertdialog'.", nameof(Role));
    }

    /// <summary>Renders the scrim before the dialog.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void RenderBefore(RenderTreeBuilder builder)
    {
        if (!(ShowScrim ?? Modal)) return;
        builder.OpenElement(0, ElementName + "Scrim");
        builder.AddAttribute(1, "overlay", true);
        builder.AddAttribute(2, "placement", "none");
        builder.AddAttribute(3, "semantic-hidden", true);
        builder.AddAttribute(4, "exiting", !Opened);
        builder.AddAttribute(5, "overlay-surface", (Func<ElementNode?>)(() => Element.Element));
        builder.CloseElement();
    }

    /// <summary>Adds the modality, the placement and the dialog semantics.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void AddSurfaceAttributes(RenderTreeBuilder builder)
    {
        builder.AddAttribute(20, "role", Role);
        builder.AddAttribute(21, "modal", Modal);
        builder.AddAttribute(22, "focus-first", true);
        builder.AddAttribute(23, "focusable", true);
        builder.AddAttribute(24, "tabindex", -1);
        builder.AddAttribute(25, "prefer-cancel", PreferCancel);
        builder.AddAttribute(26, "dialog-placement", Placement switch
        {
            DialogPlacement.Top => "top", DialogPlacement.Bottom => "bottom", DialogPlacement.Start => "start",
            DialogPlacement.End => "end", DialogPlacement.FullScreen => "fullscreen", _ => "center",
        });
        if (Placement != DialogPlacement.Center) builder.AddAttribute(27, "placement", "none");
    }
}

/// <summary>
/// A non-interactive description that appears while the pointer rests on its <see cref="Trigger" /> or the keyboard focus
/// reaches it, using the window's shared tooltip timing, and closes with Escape. The trigger wrapper adds no Tab stop.
/// Hovering the noninteractive content keeps it open while clicks pass through the surface.
/// The surface (<see cref="As" />, <c>TooltipContainer</c> by default) must not contain focusable or clickable content;
/// use a <see cref="PopoverContainer" /> for that.
/// </summary>
public sealed class TooltipContainer : ContentControl, ITooltipController, IDisposable
{
    private readonly string surfaceId = UniqueId.Create("tooltip");
    private bool open;
    private bool disposed;
    private bool retained;
    private bool exiting;
    private bool shown;
    private bool reentered;
    private PresenceMotion? motion;

    /// <summary>Gets or sets the element name of the surface.</summary>
    [Parameter] public string? As { get; set; }

    /// <summary>Gets or sets the content that shows the tooltip.</summary>
    [Parameter, EditorRequired] public RenderFragment? Trigger { get; set; }

    /// <summary>Gets or sets the preferred side of the trigger.</summary>
    [Parameter] public OverlayPlacement Placement { get; set; } = OverlayPlacement.Bottom;

    /// <summary>Gets or sets the alignment with the trigger.</summary>
    [Parameter] public OverlayAlignment Alignment { get; set; }

    /// <summary>Gets or sets the distance from the trigger, in logical units.</summary>
    [Parameter] public double Offset { get; set; }

    /// <summary>Gets or sets whether the tooltip is unavailable.</summary>
    [Parameter] public bool Disabled { get; set; }

    /// <summary>Gets or sets an opening delay; null uses the window's shared tooltip delay.</summary>
    [Parameter] public TimeSpan? Delay { get; set; }

    /// <summary>Gets whether the tooltip is shown.</summary>
    public bool Opened => open;

    private ElementReference trigger;

    /// <summary>Gets the element name of the surface.</summary>
    protected override string ElementName => As ?? "TooltipContainer";

    bool ITooltipController.IsAvailable => !disposed && !Disabled;

    ElementNode? ITooltipController.ContentElement => Element.Element;

    /// <summary>Validates the slots.</summary>
    protected override void OnParametersSet()
    {
        if (Trigger is null || ChildContent is null) throw new InvalidOperationException("TooltipContainer requires Trigger and content.");
        if (Delay is { } delay && delay < TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(Delay));
        if (Disabled) open = false;
    }

    /// <summary>Renders the trigger wrapper and, while shown, the surface; the surface stays for the description relationship.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, ElementName + "Trigger");
        builder.AddAttribute(1, "tooltip-controller", this);
        builder.AddAttribute(2, "tooltip-describes", surfaceId);
        builder.AddElementReferenceCapture(3, reference => trigger = reference);
        builder.AddContent(4, Trigger);
        builder.CloseElement();
        motion ??= new(() => _ = InvokeAsync(StateHasChanged));
        OpenDelegatedComponent<Presence>(builder, 10);
        builder.AddComponentParameter(11, nameof(Presence.Present), open);
        builder.AddComponentParameter(12, nameof(Presence.ExitDuration), motion.Duration(Element.Element));
        builder.AddComponentParameter(13, nameof(Presence.OnExited), EventCallback.Factory.Create(this, () =>
        {
            retained = false;
            reentered = false;
            StateHasChanged();
        }));
        builder.AddComponentParameter(14, nameof(Presence.ChildContent), (RenderFragment<PresenceState>)(state => content =>
        {
            shown = true;
            if (exiting && !state.IsExiting) reentered = true;
            exiting = state.IsExiting;
            base.BuildRenderTree(content);
        }));
        builder.CloseComponent();
        if (!retained)
        {
            shown = false;
            exiting = false;
            builder.OpenRegion(20);
            base.BuildRenderTree(builder);
            builder.CloseRegion();
        }
    }

    /// <summary>Adds the tooltip semantics and placement.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void AddAttributes(RenderTreeBuilder builder)
    {
        builder.AddAttribute(10, "id", Id ?? surfaceId);
        builder.AddAttribute(11, "role", "tooltip");
        builder.AddAttribute(12, "overlay", shown);
        builder.AddAttribute(13, "anchor", trigger.Element);
        builder.AddAttribute(14, "placement", PopoverContainer.PlacementText(Placement, Alignment));
        builder.AddAttribute(15, "overlay-offset", Offset == 0 ? null : Offset);
        builder.AddAttribute(16, "light-dismiss", false);
        builder.AddAttribute(17, "escape-dismiss", true);
        builder.AddAttribute(18, "tooltip-surface", true);
        builder.AddAttribute(19, "__default_ondismiss", EventCallback.Factory.Create(this, CloseAsync));
        builder.AddAttribute(20, "exiting", exiting);
        builder.AddAttribute(21, "semantic-hidden", !open);
        builder.AddAttribute(22, "exit-presence", true);
        builder.AddAttribute(23, "reentered", reentered);
    }

    /// <summary>Gets the caller's declarations, hiding the retained surface while closed.</summary>
    protected override string? ElementStyle => Css.Join(Style, shown ? "pointer-events: none" : "display: none");

    /// <summary>Rejects interactive content.</summary>
    /// <param name="firstRender">Whether this is the first render.</param>
    protected override void OnAfterRender(bool firstRender)
    {
        if (Element.Element is { } surface && ControlTree.Descendants(surface).Any(ControlTree.IsFocusable))
            throw new InvalidOperationException("Tooltip content must be noninteractive; use a PopoverContainer for interactive content.");
    }

    /// <summary>Shows the tooltip unless it is unavailable.</summary>
    /// <returns>A completed task.</returns>
    public Task OpenAsync()
    {
        if (disposed || Disabled || open) return Task.CompletedTask;
        open = true;
        retained = true;
        StateHasChanged();
        return Task.CompletedTask;
    }

    /// <summary>Hides the tooltip.</summary>
    /// <returns>A completed task.</returns>
    public Task CloseAsync()
    {
        if (disposed || !open) return Task.CompletedTask;
        open = false;
        StateHasChanged();
        return Task.CompletedTask;
    }

    /// <summary>Marks the tooltip unavailable to pending timers.</summary>
    public void Dispose()
    {
        disposed = true;
        motion?.Dispose();
    }
}
