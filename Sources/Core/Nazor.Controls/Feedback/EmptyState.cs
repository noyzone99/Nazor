using Nazor.Rendering;

namespace Nazor.Controls;

/// <summary>
/// What a view shows when it has nothing to show: no search results, an empty folder, a first run. The parts are
/// <c>EmptyStateIllustration</c> (<see cref="Illustration" />), <c>EmptyStateTitle</c> (a heading of
/// <see cref="TitleLevel" />), <c>EmptyStateDescription</c> (<see cref="Description" />), the content between the tags
/// and <c>EmptyStateActions</c> (<see cref="Actions" />). The element is a group named by its title.
/// </summary>
public sealed class EmptyState : ContentControl
{
    private readonly string titleId = UniqueId.Create("empty-title");

    /// <summary>Gets or sets a picture above the title, such as an <see cref="Image" /> or a large <see cref="Icon" />.</summary>
    [Parameter] public RenderFragment? Illustration { get; set; }

    /// <summary>Gets or sets the title, which also names the group.</summary>
    [Parameter] public string? Title { get; set; }

    /// <summary>Gets or sets the heading level of the title, from 1 to 6; 2 by default.</summary>
    [Parameter] public int TitleLevel { get; set; } = 2;

    /// <summary>Gets or sets the explanation below the title, such as what to do next.</summary>
    [Parameter] public RenderFragment? Description { get; set; }

    /// <summary>Gets or sets the actions that leave the empty state, such as Create or Clear filters.</summary>
    [Parameter] public RenderFragment? Actions { get; set; }

    /// <summary>Gets the NSS element name emitted by EmptyState.</summary>
    protected override string ElementName => "EmptyState";

    /// <summary>Validates the heading level.</summary>
    protected override void OnParametersSet()
    {
        if (TitleLevel is < 1 or > 6) throw new ArgumentOutOfRangeException(nameof(TitleLevel), "A heading level is between 1 and 6.");
    }

    /// <summary>Adds the group role and its name.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void AddAttributes(RenderTreeBuilder builder)
    {
        builder.AddAttribute(10, "role", "group");
        if (AccessibleName is null && LabeledBy is null && Title is not null) builder.AddAttribute(11, "labeled-by", titleId);
    }

    /// <summary>Adds the illustration, the title, the description, the content and the actions.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void AddContent(RenderTreeBuilder builder)
    {
        if (Illustration is not null)
        {
            builder.OpenElement(1000, "EmptyStateIllustration");
            builder.AddContent(1001, Illustration);
            builder.CloseElement();
        }
        if (Title is not null)
        {
            builder.OpenElement(1010, "EmptyStateTitle");
            builder.AddAttribute(1011, "id", titleId);
            builder.AddAttribute(1012, "role", "heading");
            builder.AddAttribute(1013, "level", TitleLevel);
            builder.AddContent(1014, Title);
            builder.CloseElement();
        }
        if (Description is not null)
        {
            builder.OpenElement(1020, "EmptyStateDescription");
            builder.AddAttribute(1021, "role", "text");
            builder.AddContent(1022, Description);
            builder.CloseElement();
        }
        builder.AddContent(1030, ChildContent);
        if (Actions is not null)
        {
            builder.OpenElement(1040, "EmptyStateActions");
            builder.AddContent(1041, Actions);
            builder.CloseElement();
        }
    }
}
