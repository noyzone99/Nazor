using Nazor.Input;
using Nazor.Rendering;

namespace Nazor.Controls;

/// <summary>How strongly a <see cref="Card" /> stands out from the surface, written as the <c>appearance</c> attribute.</summary>
public enum CardAppearance
{
    /// <summary>The ordinary filled card.</summary>
    Standard,
    /// <summary>Only an outline, on the color of the surface.</summary>
    Outline,
    /// <summary>No fill or outline until hovered or selected, for dense grids of cards.</summary>
    Subtle,
}

/// <summary>
/// A surface that groups the content about one subject, such as a product, a contact or a document. The optional
/// <c>CardPreview</c> (a large image above the rest) is followed by the <c>CardHeader</c>, with the <c>CardMedia</c>
/// slot (an avatar or an icon), the <c>CardTitle</c> and <c>CardSubtitle</c> lines and the <c>CardActions</c> slot; then
/// the <c>CardBody</c> with the content between the tags and the <c>CardFooter</c>. A card is a group named by its
/// title; a clickable card (<see cref="IsClickable" /> or <see cref="OnClick" />) is a button activated by a click,
/// Enter or Space, while buttons inside it keep their own actions. The element has the <c>appearance</c>,
/// <c>orientation</c>, <c>clickable</c> and <c>selected</c> attributes.
/// </summary>
public sealed class Card : ContentControl
{
    private readonly string titleId = UniqueId.Create("card-title");
    private readonly string subtitleId = UniqueId.Create("card-subtitle");

    /// <summary>Gets or sets a large image or other content shown above the header.</summary>
    [Parameter] public RenderFragment? Preview { get; set; }

    /// <summary>Gets or sets a small image, avatar or icon shown at the start of the header.</summary>
    [Parameter] public RenderFragment? Media { get; set; }

    /// <summary>Gets or sets the title as plain text; it names the card.</summary>
    [Parameter] public string? Title { get; set; }

    /// <summary>Gets or sets the title as content; it replaces <see cref="Title" />.</summary>
    [Parameter] public RenderFragment? TitleContent { get; set; }

    /// <summary>Gets or sets a secondary line below the title as plain text; it describes the card.</summary>
    [Parameter] public string? Subtitle { get; set; }

    /// <summary>Gets or sets the secondary line as content; it replaces <see cref="Subtitle" />.</summary>
    [Parameter] public RenderFragment? SubtitleContent { get; set; }

    /// <summary>Gets or sets commands shown at the end of the header, such as a menu button.</summary>
    [Parameter] public RenderFragment? Actions { get; set; }

    /// <summary>Gets or sets content shown at the bottom of the card, usually buttons.</summary>
    [Parameter] public RenderFragment? Footer { get; set; }

    /// <summary>Gets or sets how strongly the card stands out.</summary>
    [Parameter] public CardAppearance Appearance { get; set; }

    /// <summary>Gets or sets whether the preview is above the content (vertical) or beside it (horizontal).</summary>
    [Parameter] public Orientation Orientation { get; set; } = Orientation.Vertical;

    /// <summary>Gets or sets whether the whole card is pressable; an <see cref="OnClick" /> callback also makes it so.</summary>
    [Parameter] public bool IsClickable { get; set; }

    /// <summary>Gets or sets whether the card is drawn as selected.</summary>
    [Parameter] public bool IsSelected { get; set; }

    /// <summary>Gets or sets whether the card and its content reject interaction.</summary>
    [Parameter] public bool Disabled { get; set; }

    /// <summary>Gets or sets the callback invoked when a clickable card is activated.</summary>
    [Parameter] public EventCallback<PointerEventArgs> OnClick { get; set; }

    /// <summary>Gets whether the card is pressable.</summary>
    public bool Clickable => IsClickable || OnClick.HasDelegate;

    private bool HasTitle => TitleContent is not null || !string.IsNullOrEmpty(Title);

    private bool HasSubtitle => SubtitleContent is not null || !string.IsNullOrEmpty(Subtitle);

    /// <summary>Gets the NSS element name emitted by Card.</summary>
    protected override string ElementName => "Card";

    /// <summary>Validates the appearance and the orientation.</summary>
    protected override void OnParametersSet()
    {
        if (!Enum.IsDefined(Appearance)) throw new ArgumentOutOfRangeException(nameof(Appearance));
        if (!Enum.IsDefined(Orientation)) throw new ArgumentOutOfRangeException(nameof(Orientation));
    }

    /// <summary>Adds the role, the name, the look attributes and the activation.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void AddAttributes(RenderTreeBuilder builder)
    {
        builder.AddAttribute(10, "role", Clickable ? "button" : "group");
        builder.AddAttribute(11, "appearance", Appearance switch { CardAppearance.Outline => "outline", CardAppearance.Subtle => "subtle", _ => "standard" });
        builder.AddAttribute(12, "orientation", Vocabulary.Name(Orientation));
        builder.AddAttribute(13, "clickable", Clickable);
        builder.AddAttribute(14, "selected", IsSelected);
        builder.AddAttribute(15, "disabled", Disabled);
        builder.AddAttribute(16, "focusable", Clickable && !Disabled);
        if (AccessibleName is null && LabeledBy is null && HasTitle) builder.AddAttribute(17, "labeled-by", titleId);
        if (AccessibleDescription is null && DescribedBy is null && HasSubtitle) builder.AddAttribute(18, "described-by", subtitleId);
        if (Clickable) builder.AddAttribute(19, "__default_onclick", EventCallback.Factory.Create<PointerEventArgs>(this, ActivateAsync));
    }

    /// <summary>Adds the preview, the header, the body and the footer.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void AddContent(RenderTreeBuilder builder)
    {
        Part(builder, 1000, "CardPreview", Preview);
        if (Media is not null || HasTitle || HasSubtitle || Actions is not null)
        {
            builder.OpenElement(1010, "CardHeader");
            Part(builder, 1020, "CardMedia", Media);
            if (HasTitle)
            {
                builder.OpenElement(1030, "CardTitle");
                builder.AddAttribute(1031, "id", titleId);
                builder.AddAttribute(1032, "role", "text");
                if (TitleContent is not null) builder.AddContent(1033, TitleContent);
                else builder.AddContent(1034, Title);
                builder.CloseElement();
            }
            if (HasSubtitle)
            {
                builder.OpenElement(1040, "CardSubtitle");
                builder.AddAttribute(1041, "id", subtitleId);
                builder.AddAttribute(1042, "role", "text");
                if (SubtitleContent is not null) builder.AddContent(1043, SubtitleContent);
                else builder.AddContent(1044, Subtitle);
                builder.CloseElement();
            }
            Part(builder, 1050, "CardActions", Actions);
            builder.CloseElement();
        }
        Part(builder, 1060, "CardBody", ChildContent);
        Part(builder, 1070, "CardFooter", Footer);
    }

    private static void Part(RenderTreeBuilder builder, int sequence, string name, RenderFragment? content)
    {
        if (content is null) return;
        builder.OpenElement(sequence, name);
        builder.AddContent(sequence + 1, content);
        builder.CloseElement();
    }

    private Task ActivateAsync(PointerEventArgs e) =>
        Disabled || Element.Element?.IsDisabled is true ? Task.CompletedTask : OnClick.InvokeAsync(e);
}
