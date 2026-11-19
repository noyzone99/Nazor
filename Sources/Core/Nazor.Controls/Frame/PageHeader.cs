using Nazor.Rendering;

namespace Nazor.Controls;

/// <summary>
/// The header of a page: an optional <see cref="Breadcrumb" /> above, then <see cref="Leading" /> content (such as a
/// back button), the <see cref="Title" /> as a heading (<see cref="HeadingLevel" />) with its <see cref="Subtitle" />, and
/// the page's <see cref="Actions" /> at the trailing edge; content between the tags follows below, such as tabs or
/// filters. The parts are <c>PageHeaderBreadcrumb</c>, <c>PageHeaderMain</c>, <c>PageHeaderLeading</c>,
/// <c>PageHeaderHeading</c> (with <c>PageHeaderTitle</c> and <c>PageHeaderSubtitle</c>), <c>PageHeaderActions</c> and
/// <c>PageHeaderContent</c>. <see cref="IsCompact" /> writes the <c>compact</c> attribute, which themes draw smaller, for
/// example once the page has scrolled.
/// </summary>
public sealed class PageHeader : ContentControl
{
    /// <summary>Gets or sets the title of the page.</summary>
    [Parameter] public string? Title { get; set; }

    /// <summary>Gets or sets rich title content, used instead of <see cref="Title" />.</summary>
    [Parameter] public RenderFragment? TitleContent { get; set; }

    /// <summary>Gets or sets a secondary line under the title.</summary>
    [Parameter] public string? Subtitle { get; set; }

    /// <summary>Gets or sets the commands of the page, at the trailing edge.</summary>
    [Parameter] public RenderFragment? Actions { get; set; }

    /// <summary>Gets or sets the path to the page shown above the title, such as a breadcrumb.</summary>
    [Parameter] public RenderFragment? Breadcrumb { get; set; }

    /// <summary>Gets or sets content before the title, such as a back button or an avatar.</summary>
    [Parameter] public RenderFragment? Leading { get; set; }

    /// <summary>Gets or sets the heading level of the title, from 1 to 6; 1 by default.</summary>
    [Parameter] public int HeadingLevel { get; set; } = 1;

    /// <summary>Gets or sets whether the header is drawn in its compact form.</summary>
    [Parameter] public bool IsCompact { get; set; }

    /// <summary>Gets the NSS element name emitted by PageHeader.</summary>
    protected override string ElementName => "PageHeader";

    /// <summary>Validates the heading level.</summary>
    protected override void OnParametersSet()
    {
        if (HeadingLevel is < 1 or > 6) throw new ArgumentOutOfRangeException(nameof(HeadingLevel));
    }

    /// <summary>Adds the compact state.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void AddAttributes(RenderTreeBuilder builder) => builder.AddAttribute(10, "compact", IsCompact);

    /// <summary>Adds the breadcrumb, the main row and the content below.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void AddContent(RenderTreeBuilder builder)
    {
        if (Breadcrumb is not null)
        {
            builder.OpenElement(1000, "PageHeaderBreadcrumb");
            builder.AddContent(1001, Breadcrumb);
            builder.CloseElement();
        }
        builder.OpenElement(1010, "PageHeaderMain");
        if (Leading is not null)
        {
            builder.OpenElement(1011, "PageHeaderLeading");
            builder.AddContent(1012, Leading);
            builder.CloseElement();
        }
        builder.OpenElement(1020, "PageHeaderHeading");
        if (Title is not null || TitleContent is not null)
        {
            builder.OpenElement(1021, "PageHeaderTitle");
            builder.AddAttribute(1022, "role", "heading");
            builder.AddAttribute(1023, "level", HeadingLevel);
            if (TitleContent is not null) builder.AddContent(1024, TitleContent);
            else builder.AddContent(1025, Title);
            builder.CloseElement();
        }
        if (Subtitle is not null)
        {
            builder.OpenElement(1026, "PageHeaderSubtitle");
            builder.AddContent(1027, Subtitle);
            builder.CloseElement();
        }
        builder.CloseElement();
        if (Actions is not null)
        {
            builder.OpenElement(1030, "PageHeaderActions");
            builder.AddContent(1031, Actions);
            builder.CloseElement();
        }
        builder.CloseElement();
        if (ChildContent is not null)
        {
            builder.OpenElement(1040, "PageHeaderContent");
            builder.AddContent(1041, ChildContent);
            builder.CloseElement();
        }
    }
}
