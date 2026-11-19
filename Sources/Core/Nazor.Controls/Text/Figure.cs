using Nazor.Rendering;

namespace Nazor.Controls;

/// <summary>
/// Self-contained content referenced from the text, such as an image, a chart, a code listing or a table, with an
/// optional <see cref="FigureCaption" /> before or after it. The element is <c>Figure</c>, a group described as a figure
/// and named by its caption for assistive technologies.
/// </summary>
public sealed class Figure : ContentControl
{
    private string? captionId;

    /// <summary>Gets the identifier of the caption that names the figure, or null without a caption.</summary>
    public string? CaptionId => captionId;

    /// <summary>Gets the NSS element name emitted by Figure.</summary>
    protected override string ElementName => "Figure";

    /// <summary>Adds the group role described as a figure and the caption relationship.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void AddAttributes(RenderTreeBuilder builder)
    {
        builder.AddAttribute(10, "role", "group");
        builder.AddAttribute(11, "role-description", ControlStrings.Get("Figure"));
        if (LabeledBy is null && AccessibleName is null && captionId is not null) builder.AddAttribute(12, "labeled-by", captionId);
    }

    /// <summary>Supplies the figure to its caption.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void AddContent(RenderTreeBuilder builder)
    {
        builder.OpenComponent<CascadingValue<Figure>>(1000);
        builder.AddComponentParameter(1001, "Value", this);
        builder.AddComponentParameter(1002, "IsFixed", true);
        builder.AddComponentParameter(1003, "ChildContent", ChildContent);
        builder.CloseComponent();
    }

    internal void SetCaption(string? id)
    {
        if (captionId == id) return;
        captionId = id;
        StateHasChanged();
    }
}

/// <summary>
/// The caption of the enclosing <see cref="Figure" />, which it names for assistive technologies. Place it before or
/// after the figure content. The element is <c>FigureCaption</c> with the <c>text</c> role.
/// </summary>
public sealed class FigureCaption : ContentControl, IDisposable
{
    private readonly string generatedId = UniqueId.Create("figure-caption");
    private Figure? registered;

    /// <summary>Gets or sets the enclosing figure, supplied automatically.</summary>
    [CascadingParameter] public Figure? Figure { get; set; }

    /// <summary>Gets the NSS element name emitted by FigureCaption.</summary>
    protected override string ElementName => "FigureCaption";

    private string CaptionId => Id ?? generatedId;

    /// <summary>Names the enclosing figure.</summary>
    protected override void OnParametersSet()
    {
        if (registered is not null && registered != Figure) registered.SetCaption(null);
        registered = Figure;
        registered?.SetCaption(CaptionId);
    }

    /// <summary>Adds the identity and the text role.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void AddAttributes(RenderTreeBuilder builder)
    {
        builder.AddAttribute(10, "id", CaptionId);
        builder.AddAttribute(11, "role", "text");
    }

    /// <summary>Stops naming the figure.</summary>
    public void Dispose()
    {
        registered?.SetCaption(null);
        registered = null;
    }
}
