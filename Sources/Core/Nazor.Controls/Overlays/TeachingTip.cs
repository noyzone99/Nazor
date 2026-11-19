using Nazor.Controls.Primitives;
using Nazor.Rendering;

namespace Nazor.Controls;

/// <summary>
/// A non-modal callout that points at a <see cref="Target" />, or centers in its window without a target or anchor point,
/// to introduce a feature or suggest a next step: an optional
/// <see cref="Hero" /> image, a <see cref="Title" /> that names it, a <see cref="Subtitle" />, a body (child content),
/// <see cref="Actions" /> and a close button. <see cref="IsOpen" /> supports <c>@bind-IsOpen</c>; it stays open until the
/// user closes it (Escape, the close button or an action), unless <see cref="CloseOnOutside" /> is set, and does not take
/// the focus unless <see cref="FocusOnOpen" /> is set. Parts: <c>TeachingTip</c> (<c>side</c>, <c>targeted</c>, <c>has-hero</c>), <c>TeachingTipArrow</c>
/// (<c>placement</c>), <c>TeachingTipHero</c>, <c>TeachingTipHeader</c>, <c>TeachingTipHeading</c>,
/// <c>TeachingTipTitle</c>, <c>TeachingTipSubtitle</c>, <c>TeachingTipClose</c>, <c>TeachingTipBody</c> and
/// <c>TeachingTipActions</c>.
/// </summary>
public sealed class TeachingTip : ComponentBase
{
    private readonly ControllableState<bool> open = new(false);
    private readonly string titleId = UniqueId.Create("teaching-tip-title");
    private readonly string subtitleId = UniqueId.Create("teaching-tip-subtitle");

    /// <summary>Gets or sets whether the tip is open.</summary>
    [Parameter] public bool IsOpen { get; set; }

    /// <summary>Gets or sets the callback invoked when the tip opens or closes from inside.</summary>
    [Parameter] public EventCallback<bool> IsOpenChanged { get; set; }

    /// <summary>Gets or sets the element the tip points at.</summary>
    [Parameter] public ElementReference? Target { get; set; }

    /// <summary>Gets or sets a point in window coordinates the tip points at when <see cref="Target" /> is null.</summary>
    [Parameter] public (double X, double Y)? AnchorPoint { get; set; }

    /// <summary>Gets or sets the preferred side of the target.</summary>
    [Parameter] public OverlayPlacement Placement { get; set; }

    /// <summary>Gets or sets the alignment with the target.</summary>
    [Parameter] public OverlayAlignment Alignment { get; set; } = OverlayAlignment.Center;

    /// <summary>Gets or sets the distance from the target, in logical units.</summary>
    [Parameter] public double Offset { get; set; } = 8;

    /// <summary>Gets or sets whether an arrow points at the target or anchor point; an untargeted tip has no arrow.</summary>
    [Parameter] public bool ShowArrow { get; set; } = true;

    /// <summary>Gets or sets content shown edge to edge at the top, such as an illustration.</summary>
    [Parameter] public RenderFragment? Hero { get; set; }

    /// <summary>Gets or sets the title, which names the tip.</summary>
    [Parameter] public string? Title { get; set; }

    /// <summary>Gets or sets a line below the title, which describes the tip.</summary>
    [Parameter] public string? Subtitle { get; set; }

    /// <summary>Gets or sets the body.</summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>Gets or sets the actions, such as buttons.</summary>
    [Parameter] public RenderFragment? Actions { get; set; }

    /// <summary>Gets or sets whether the header shows a close button.</summary>
    [Parameter] public bool ShowCloseButton { get; set; } = true;

    /// <summary>Gets or sets whether a press outside closes the tip.</summary>
    [Parameter] public bool CloseOnOutside { get; set; }

    /// <summary>Gets or sets whether Escape closes the tip.</summary>
    [Parameter] public bool CloseOnEscape { get; set; } = true;

    /// <summary>Gets or sets whether opening moves the focus into the tip.</summary>
    [Parameter] public bool FocusOnOpen { get; set; }

    /// <summary>Gets or sets the callback invoked when the user closes the tip with Escape, the close button or a press outside.</summary>
    [Parameter] public EventCallback OnDismiss { get; set; }

    /// <summary>Gets or sets the space-separated NSS classes of the surface.</summary>
    [Parameter] public string? Class { get; set; }

    /// <summary>Gets or sets NSS declarations for the surface only.</summary>
    [Parameter] public string? Style { get; set; }

    /// <summary>Gets or sets the accessible name; the title names the tip when null.</summary>
    [Parameter] public string? AccessibleName { get; set; }

    /// <summary>Gets whether the tip is open.</summary>
    public bool Opened => open.Value;

    /// <summary>Synchronizes the open state and validates the parameters.</summary>
    protected override void OnParametersSet()
    {
        open.Sync(IsOpen);
        if (!Enum.IsDefined(Placement)) throw new ArgumentOutOfRangeException(nameof(Placement));
        if (!Enum.IsDefined(Alignment)) throw new ArgumentOutOfRangeException(nameof(Alignment));
        if (!double.IsFinite(Offset)) throw new ArgumentOutOfRangeException(nameof(Offset));
    }

    /// <summary>Renders the anchored surface while open.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        OpenDelegatedComponent<PopoverContainer>(builder, 0);
        builder.AddComponentParameter(1, nameof(PopoverContainer.As), "TeachingTip");
        builder.AddComponentParameter(2, nameof(PopoverContainer.IsOpen), open.Value);
        builder.AddComponentParameter(3, nameof(PopoverContainer.IsOpenChanged), EventCallback.Factory.Create<bool>(this, value => value ? Task.CompletedTask : SetOpenAsync(false)));
        builder.AddComponentParameter(4, nameof(PopoverContainer.Anchor), Target);
        builder.AddComponentParameter(5, nameof(PopoverContainer.AnchorPoint), AnchorPoint);
        builder.AddComponentParameter(6, nameof(PopoverContainer.Placement), Placement);
        builder.AddComponentParameter(7, nameof(PopoverContainer.Alignment), Alignment);
        builder.AddComponentParameter(8, nameof(PopoverContainer.Offset), Offset);
        builder.AddComponentParameter(9, nameof(PopoverContainer.ShowArrow), ShowArrow && (Target is not null || AnchorPoint is not null));
        builder.AddComponentParameter(10, nameof(PopoverContainer.FocusOnOpen), FocusOnOpen);
        builder.AddComponentParameter(11, nameof(PopoverContainer.Role), "dialog");
        builder.AddComponentParameter(12, nameof(OverlayContainer.CloseOnOutside), CloseOnOutside);
        builder.AddComponentParameter(13, nameof(OverlayContainer.CloseOnEscape), CloseOnEscape);
        builder.AddComponentParameter(14, nameof(OverlayContainer.OnDismiss), OnDismiss);
        builder.AddComponentParameter(15, nameof(Control.Class), Class);
        builder.AddComponentParameter(16, nameof(Control.Style), Style);
        builder.AddComponentParameter(17, nameof(Control.AccessibleName), AccessibleName);
        builder.AddComponentParameter(18, nameof(Control.LabeledBy), AccessibleName is null && Title is not null ? titleId : null);
        builder.AddComponentParameter(19, nameof(Control.DescribedBy), Subtitle is not null ? subtitleId : null);
        builder.AddComponentParameter(20, nameof(PopoverContainer.ChildContent), (RenderFragment)BuildContent);
        builder.AddComponentParameter(21, nameof(PopoverContainer.OriginAtArrow), true);
        builder.AddComponentParameter(22, nameof(Control.AdditionalAttributes), new Dictionary<string, object?>
        {
            ["targeted"] = Target is not null || AnchorPoint is not null,
            ["has-hero"] = Hero is not null,
        });
        builder.CloseComponent();
    }

    private void BuildContent(RenderTreeBuilder builder)
    {
        if (Hero is not null) OverlayParts.Part(builder, 0, "TeachingTipHero", Hero);
        if (Title is not null || Subtitle is not null || ShowCloseButton)
        {
            builder.OpenElement(10, "TeachingTipHeader");
            builder.OpenElement(11, "TeachingTipHeading");
            if (Title is not null)
            {
                builder.OpenElement(12, "TeachingTipTitle");
                builder.AddAttribute(13, "id", titleId);
                builder.AddAttribute(14, "role", "text");
                builder.AddContent(15, Title);
                builder.CloseElement();
            }
            if (Subtitle is not null)
            {
                builder.OpenElement(16, "TeachingTipSubtitle");
                builder.AddAttribute(17, "id", subtitleId);
                builder.AddAttribute(18, "role", "text");
                builder.AddContent(19, Subtitle);
                builder.CloseElement();
            }
            builder.CloseElement();
            if (ShowCloseButton) OverlayParts.CloseButton(builder, 20, "TeachingTipClose", this, DismissAsync);
            builder.CloseElement();
        }
        if (ChildContent is not null) OverlayParts.Part(builder, 40, "TeachingTipBody", ChildContent);
        if (Actions is not null) OverlayParts.Part(builder, 50, "TeachingTipActions", Actions);
    }

    private async Task DismissAsync()
    {
        await OnDismiss.InvokeAsync();
        await SetOpenAsync(false);
    }

    /// <summary>Opens the tip.</summary>
    /// <returns>A task that completes when the change has been reported.</returns>
    public Task OpenAsync() => SetOpenAsync(true);

    /// <summary>Closes the tip.</summary>
    /// <returns>A task that completes when the change has been reported.</returns>
    public Task CloseAsync() => SetOpenAsync(false);

    private async Task SetOpenAsync(bool value)
    {
        if (!open.Set(value)) return;
        StateHasChanged();
        await IsOpenChanged.InvokeAsync(value);
    }
}
