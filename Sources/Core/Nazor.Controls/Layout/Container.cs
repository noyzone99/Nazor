using Nazor.Rendering;

namespace Nazor.Controls;

/// <summary>
/// Groups other controls; layout and scrolling are defined by NSS (block, flex, grid and overflow). A named container
/// or one with <c>overflow-x</c>/<c>overflow-y: auto</c> or <c>scroll</c> is exposed as an accessible group, and its
/// scroll position follows its computed overflow. Other layout-only containers pass their children to the nearest
/// semantic ancestor.
/// </summary>
public sealed class Container : ContentControl
{
    /// <summary>Gets the NSS element name emitted by Container.</summary>
    protected override string ElementName => "Container";

    /// <summary>Adds the group role when the container is named.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void AddAttributes(RenderTreeBuilder builder) =>
        builder.AddAttribute(10, "role", AccessibleName is null ? null : "group");
}

/// <summary>
/// A horizontal or vertical division between groups of content, menu items or toolbar commands, optionally with a label
/// in the middle of the line.
/// </summary>
public sealed class Separator : ContentControl
{
    /// <summary>Gets or sets the direction of the line; a vertical separator divides content laid out in a row.</summary>
    [Parameter] public Orientation Orientation { get; set; }

    /// <summary>Gets the NSS element name emitted by Separator.</summary>
    protected override string ElementName => "Separator";

    /// <summary>Validates the orientation.</summary>
    protected override void OnParametersSet()
    {
        if (!Enum.IsDefined(Orientation)) throw new ArgumentOutOfRangeException(nameof(Orientation));
    }

    /// <summary>Adds the separator role and its orientation.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void AddAttributes(RenderTreeBuilder builder)
    {
        builder.AddAttribute(10, "role", "separator");
        builder.AddAttribute(11, "orientation", Vocabulary.Name(Orientation));
        builder.AddAttribute(12, "vertical", Orientation == Orientation.Vertical);
    }

    /// <summary>Adds the optional label shown in the middle of the line.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void AddContent(RenderTreeBuilder builder)
    {
        if (ChildContent is null) return;
        builder.OpenElement(1000, "SeparatorLabel");
        builder.AddContent(1001, ChildContent);
        builder.CloseElement();
    }
}
