using Nazor.Controls.Primitives;
using Nazor.Input;
using Nazor.Rendering;

namespace Nazor.Controls;

/// <summary>
/// Interactive content anchored to a <see cref="Trigger" /> (or to <see cref="Anchor" />): a click on the trigger
/// toggles it, a press outside or Escape closes it and the focus returns to the trigger. With <see cref="OpenOnHover" />
/// it is a hover card: it opens after <see cref="HoverDelay" /> while the pointer rests on the trigger, or at once when
/// the keyboard focus reaches it, and closes after <see cref="HoverCloseDelay" /> once both the pointer and the focus
/// have left the trigger and the card. <see cref="IsOpen" /> supports <c>@bind-IsOpen</c>. Parts: <c>PopoverTrigger</c>
/// (<c>expanded</c>), <c>Popover</c> (<c>side</c>), <c>PopoverArrow</c> (<c>placement</c>), <c>PopoverHeader</c>,
/// <c>PopoverTitle</c>, <c>PopoverClose</c>, <c>PopoverBody</c> and <c>PopoverFooter</c>.
/// </summary>
public sealed class Popover : ComponentBase, IDisposable
{
    private readonly ControllableState<bool> open = new(false);
    private readonly string titleId = UniqueId.Create("popover-title");
    private HoverIntent? hover;
    private ElementReference trigger;

    /// <summary>Gets or sets the content that opens the popover, usually a <see cref="Button" />.</summary>
    [Parameter] public RenderFragment? Trigger { get; set; }

    /// <summary>Gets or sets the element the popover is attached to when there is no <see cref="Trigger" />.</summary>
    [Parameter] public ElementReference? Anchor { get; set; }

    /// <summary>Gets or sets the body of the popover.</summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>Gets or sets the title shown in the header, which names the popover.</summary>
    [Parameter] public string? Title { get; set; }

    /// <summary>Gets or sets rich header content, used instead of <see cref="Title" />.</summary>
    [Parameter] public RenderFragment? Header { get; set; }

    /// <summary>Gets or sets the footer, such as actions.</summary>
    [Parameter] public RenderFragment? Footer { get; set; }

    /// <summary>Gets or sets whether the header shows a close button.</summary>
    [Parameter] public bool ShowCloseButton { get; set; }

    /// <summary>Gets or sets whether the popover is open.</summary>
    [Parameter] public bool IsOpen { get; set; }

    /// <summary>Gets or sets the callback invoked when the popover opens or closes from inside.</summary>
    [Parameter] public EventCallback<bool> IsOpenChanged { get; set; }

    /// <summary>Gets or sets the preferred side of the trigger.</summary>
    [Parameter] public OverlayPlacement Placement { get; set; }

    /// <summary>Gets or sets the alignment with the trigger.</summary>
    [Parameter] public OverlayAlignment Alignment { get; set; }

    /// <summary>Gets or sets the distance from the trigger, in logical units.</summary>
    [Parameter] public double Offset { get; set; } = 4;

    /// <summary>Gets or sets whether an arrow points at the trigger.</summary>
    [Parameter] public bool ShowArrow { get; set; }

    /// <summary>Gets or sets whether the popover is a hover card that opens on hover and focus.</summary>
    [Parameter] public bool OpenOnHover { get; set; }

    /// <summary>Gets or sets the delay before a hover card opens.</summary>
    [Parameter] public TimeSpan HoverDelay { get; set; } = TimeSpan.FromMilliseconds(400);

    /// <summary>Gets or sets the delay before a hover card closes.</summary>
    [Parameter] public TimeSpan HoverCloseDelay { get; set; } = TimeSpan.FromMilliseconds(300);

    /// <summary>Gets or sets whether opening moves the focus into the popover; null moves it except for hover cards.</summary>
    [Parameter] public bool? FocusOnOpen { get; set; }

    /// <summary>Gets or sets whether a press outside closes the popover.</summary>
    [Parameter] public bool CloseOnOutside { get; set; } = true;

    /// <summary>Gets or sets whether Escape closes the popover.</summary>
    [Parameter] public bool CloseOnEscape { get; set; } = true;

    /// <summary>Gets or sets the callback invoked when the user dismisses the popover.</summary>
    [Parameter] public EventCallback OnDismiss { get; set; }

    /// <summary>Gets or sets whether the trigger does not open the popover.</summary>
    [Parameter] public bool Disabled { get; set; }

    /// <summary>Gets or sets the accessible role of the surface, <c>dialog</c> by default.</summary>
    [Parameter] public string Role { get; set; } = "dialog";

    /// <summary>Gets or sets the space-separated NSS classes of the surface.</summary>
    [Parameter] public string? Class { get; set; }

    /// <summary>Gets or sets NSS declarations for the surface only.</summary>
    [Parameter] public string? Style { get; set; }

    /// <summary>Gets or sets the accessible name; the title names the popover when null.</summary>
    [Parameter] public string? AccessibleName { get; set; }

    /// <summary>Gets whether the popover is open.</summary>
    public bool Opened => open.Value;

    private bool HasTitle => Title is not null || Header is not null;

    /// <summary>Synchronizes the open state and validates the parameters.</summary>
    protected override void OnParametersSet()
    {
        open.Sync(IsOpen);
        if (!Enum.IsDefined(Placement)) throw new ArgumentOutOfRangeException(nameof(Placement));
        if (!Enum.IsDefined(Alignment)) throw new ArgumentOutOfRangeException(nameof(Alignment));
        if (!double.IsFinite(Offset)) throw new ArgumentOutOfRangeException(nameof(Offset));
        if (HoverDelay < TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(HoverDelay));
        if (HoverCloseDelay < TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(HoverCloseDelay));
        if (string.IsNullOrWhiteSpace(Role)) throw new ArgumentException("A popover needs a role.", nameof(Role));
        if (Trigger is null && Anchor is null && IsOpen) throw new InvalidOperationException("Popover requires a Trigger or an Anchor.");
        hover ??= new HoverIntent(work => InvokeAsync(work), () => SetOpenAsync(true), () => SetOpenAsync(false));
        hover.OpenDelay = HoverDelay;
        hover.CloseDelay = HoverCloseDelay;
        if (Disabled) hover.Cancel();
    }

    /// <summary>Renders the trigger wrapper and, while open, the surface.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        if (Trigger is not null)
        {
            builder.OpenElement(0, "PopoverTrigger");
            builder.AddAttribute(1, "expanded", open.Value);
            builder.AddAttribute(9, "overlay-trigger", true);
            builder.AddAttribute(2, "__internal_onclick#popover", EventCallback.Factory.Create<PointerEventArgs>(this, ToggleFromTriggerAsync));
            if (OpenOnHover)
            {
                builder.AddAttribute(3, "__internal_onpointerenter#popover", EventCallback.Factory.Create<PointerEventArgs>(this, Enter));
                builder.AddAttribute(4, "__internal_onpointerleave#popover", EventCallback.Factory.Create<PointerEventArgs>(this, Leave));
                builder.AddAttribute(5, "__internal_onfocusin#popover", EventCallback.Factory.Create<FocusEventArgs>(this, FocusedAsync));
                builder.AddAttribute(6, "__internal_onfocusout#popover", EventCallback.Factory.Create<FocusEventArgs>(this, Leave));
            }
            builder.AddElementReferenceCapture(7, reference => trigger = reference);
            builder.AddContent(8, Trigger);
            builder.CloseElement();
        }
        var attributes = new Dictionary<string, object?>();
        if (OpenOnHover)
        {
            attributes["__internal_onpointerenter#popover"] = EventCallback.Factory.Create<PointerEventArgs>(this, Enter);
            attributes["__internal_onpointerleave#popover"] = EventCallback.Factory.Create<PointerEventArgs>(this, Leave);
            attributes["__internal_onfocusin#popover"] = EventCallback.Factory.Create<FocusEventArgs>(this, Enter);
            attributes["__internal_onfocusout#popover"] = EventCallback.Factory.Create<FocusEventArgs>(this, Leave);
        }
        OpenDelegatedComponent<PopoverContainer>(builder, 20);
        builder.AddComponentParameter(21, nameof(PopoverContainer.As), "Popover");
        builder.AddComponentParameter(22, nameof(PopoverContainer.IsOpen), open.Value);
        builder.AddComponentParameter(23, nameof(PopoverContainer.IsOpenChanged), EventCallback.Factory.Create<bool>(this, value => value ? Task.CompletedTask : SetOpenAsync(false)));
        builder.AddComponentParameter(24, nameof(PopoverContainer.Anchor), Anchor ?? TriggerAnchor(trigger));
        builder.AddComponentParameter(25, nameof(PopoverContainer.Placement), Placement);
        builder.AddComponentParameter(26, nameof(PopoverContainer.Alignment), Alignment);
        builder.AddComponentParameter(27, nameof(PopoverContainer.Offset), Offset);
        builder.AddComponentParameter(28, nameof(PopoverContainer.ShowArrow), ShowArrow);
        builder.AddComponentParameter(29, nameof(PopoverContainer.FocusOnOpen), FocusOnOpen ?? !OpenOnHover);
        builder.AddComponentParameter(30, nameof(PopoverContainer.Role), Role);
        builder.AddComponentParameter(31, nameof(OverlayContainer.CloseOnOutside), CloseOnOutside);
        builder.AddComponentParameter(32, nameof(OverlayContainer.CloseOnEscape), CloseOnEscape);
        builder.AddComponentParameter(33, nameof(OverlayContainer.OnDismiss), OnDismiss);
        builder.AddComponentParameter(34, nameof(Control.Class), Class);
        builder.AddComponentParameter(35, nameof(Control.Style), Style);
        builder.AddComponentParameter(36, nameof(Control.AccessibleName), AccessibleName);
        builder.AddComponentParameter(37, nameof(Control.LabeledBy), AccessibleName is null && HasTitle ? titleId : null);
        builder.AddMultipleAttributes(38, attributes);
        builder.AddComponentParameter(39, nameof(PopoverContainer.ChildContent), (RenderFragment)BuildContent);
        builder.CloseComponent();
    }

    private void BuildContent(RenderTreeBuilder builder)
    {
        if (HasTitle || ShowCloseButton)
        {
            builder.OpenElement(0, "PopoverHeader");
            if (HasTitle)
            {
                builder.OpenElement(1, "PopoverTitle");
                builder.AddAttribute(2, "id", titleId);
                builder.AddAttribute(3, "role", "text");
                if (Header is not null) builder.AddContent(4, Header);
                else builder.AddContent(5, Title);
                builder.CloseElement();
            }
            if (ShowCloseButton) OverlayParts.CloseButton(builder, 10, "PopoverClose", this, DismissAsync);
            builder.CloseElement();
        }
        OverlayParts.Part(builder, 30, "PopoverBody", ChildContent);
        if (Footer is not null) OverlayParts.Part(builder, 40, "PopoverFooter", Footer);
    }

    internal static ElementReference? TriggerAnchor(ElementReference wrapper) =>
        wrapper.Element is { } element ? new ElementReference(element.Children.OfType<ElementNode>().FirstOrDefault() ?? element) : null;

    private Task ToggleFromTriggerAsync()
    {
        if (Disabled) return Task.CompletedTask;
        hover?.Cancel();
        return SetOpenAsync(OpenOnHover || !open.Value);
    }

    private void Enter()
    {
        if (!Disabled) hover?.Enter();
    }

    private void Leave() => hover?.Leave();

    private Task FocusedAsync()
    {
        if (Disabled) return Task.CompletedTask;
        hover?.Cancel();
        return SetOpenAsync(true);
    }

    private async Task DismissAsync()
    {
        await OnDismiss.InvokeAsync();
        await SetOpenAsync(false);
    }

    /// <summary>Opens the popover.</summary>
    /// <returns>A task that completes when the change has been reported.</returns>
    public Task OpenAsync() => SetOpenAsync(true);

    /// <summary>Closes the popover.</summary>
    /// <returns>A task that completes when the change has been reported.</returns>
    public Task CloseAsync() => SetOpenAsync(false);

    private async Task SetOpenAsync(bool value)
    {
        if (value && Disabled) return;
        if (!open.Set(value)) return;
        StateHasChanged();
        await IsOpenChanged.InvokeAsync(value);
    }

    /// <summary>Cancels a pending hover change.</summary>
    public void Dispose() => hover?.Dispose();
}
