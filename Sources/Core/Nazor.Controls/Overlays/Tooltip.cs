using Nazor.Controls.Primitives;
using Nazor.Rendering;

namespace Nazor.Controls;

/// <summary>
/// A short, non-interactive description of its <see cref="Trigger" />, shown while the pointer rests on it or the
/// keyboard focus reaches it, after the window's shared tooltip delay (or <see cref="Delay" />), and closed by Escape.
/// It describes the trigger for assistive technologies. Use <see cref="Text" /> or child content, with an optional bold
/// <see cref="Title" />. Hovering the noninteractive content keeps it open while clicks pass through the surface;
/// for interactive content use a <see cref="Popover" />. Parts: <c>TooltipTrigger</c>,
/// <c>Tooltip</c>, <c>TooltipArrow</c> (<c>placement</c>), <c>TooltipTitle</c> and <c>TooltipText</c>.
/// </summary>
public sealed class Tooltip : ComponentBase
{
    /// <summary>Gets or sets the content the tooltip describes.</summary>
    [Parameter, EditorRequired] public RenderFragment? Trigger { get; set; }

    /// <summary>Gets or sets the text of the tooltip.</summary>
    [Parameter] public string? Text { get; set; }

    /// <summary>Gets or sets rich, non-interactive content, shown after <see cref="Text" />.</summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>Gets or sets a bold first line.</summary>
    [Parameter] public string? Title { get; set; }

    /// <summary>Gets or sets the preferred side of the trigger.</summary>
    [Parameter] public OverlayPlacement Placement { get; set; } = OverlayPlacement.Top;

    /// <summary>Gets or sets the alignment with the trigger.</summary>
    [Parameter] public OverlayAlignment Alignment { get; set; } = OverlayAlignment.Center;

    /// <summary>Gets or sets the distance from the trigger, in logical units.</summary>
    [Parameter] public double Offset { get; set; } = 4;

    /// <summary>Gets or sets an opening delay; null uses the window's shared tooltip delay.</summary>
    [Parameter] public TimeSpan? Delay { get; set; }

    /// <summary>Gets or sets whether the tooltip is not shown.</summary>
    [Parameter] public bool Disabled { get; set; }

    /// <summary>Gets or sets whether an arrow points at the trigger.</summary>
    [Parameter] public bool ShowArrow { get; set; }

    /// <summary>Gets or sets the space-separated NSS classes of the surface.</summary>
    [Parameter] public string? Class { get; set; }

    /// <summary>Gets or sets the identifier of the surface.</summary>
    [Parameter] public string? Id { get; set; }

    /// <summary>Gets or sets NSS declarations for the surface only.</summary>
    [Parameter] public string? Style { get; set; }

    /// <summary>Validates the parameters.</summary>
    protected override void OnParametersSet()
    {
        if (Trigger is null) throw new InvalidOperationException("Tooltip requires a Trigger.");
        if (Text is null && ChildContent is null && Title is null) throw new InvalidOperationException("Tooltip requires Text, Title or content.");
        if (!Enum.IsDefined(Placement)) throw new ArgumentOutOfRangeException(nameof(Placement));
        if (!Enum.IsDefined(Alignment)) throw new ArgumentOutOfRangeException(nameof(Alignment));
        if (!double.IsFinite(Offset)) throw new ArgumentOutOfRangeException(nameof(Offset));
        if (Delay is { } delay && delay < TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(Delay));
    }

    /// <summary>Renders the tooltip container.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        OpenDelegatedComponent<TooltipContainer>(builder, 0);
        builder.AddComponentParameter(1, nameof(TooltipContainer.As), "Tooltip");
        builder.AddComponentParameter(2, nameof(TooltipContainer.Trigger), Trigger);
        builder.AddComponentParameter(3, nameof(TooltipContainer.Placement), Placement);
        builder.AddComponentParameter(4, nameof(TooltipContainer.Alignment), Alignment);
        builder.AddComponentParameter(5, nameof(TooltipContainer.Offset), Offset);
        builder.AddComponentParameter(6, nameof(TooltipContainer.Delay), Delay);
        builder.AddComponentParameter(7, nameof(TooltipContainer.Disabled), Disabled);
        builder.AddComponentParameter(8, nameof(Control.Class), Class);
        builder.AddComponentParameter(9, nameof(Control.Id), Id);
        builder.AddComponentParameter(10, nameof(Control.Style), Style);
        builder.AddComponentParameter(11, nameof(TooltipContainer.ChildContent), (RenderFragment)BuildContent);
        builder.CloseComponent();
    }

    private void BuildContent(RenderTreeBuilder builder)
    {
        if (ShowArrow)
        {
            builder.OpenElement(0, "TooltipArrow");
            builder.AddAttribute(1, "placement", Placement.ToString().ToLowerInvariant());
            builder.AddAttribute(2, "semantic-hidden", true);
            builder.CloseElement();
        }
        if (Title is not null)
        {
            builder.OpenElement(10, "TooltipTitle");
            builder.AddContent(11, Title);
            builder.CloseElement();
        }
        if (Text is not null)
        {
            builder.OpenElement(20, "TooltipText");
            builder.AddContent(21, Text);
            builder.CloseElement();
        }
        builder.AddContent(30, ChildContent);
    }
}
