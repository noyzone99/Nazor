using Nazor.Controls.Primitives;
using Nazor.Rendering;

namespace Nazor.Controls;

/// <summary>The sizing policy of a <see cref="Dialog" />, written as the <c>size</c> attribute.</summary>
public enum DialogSize
{
    /// <summary>The theme's automatic sizing policy; Fluent measures and clamps centered content while Basic and NazorLeaf use their ordinary width.</summary>
    Auto = 4,
    /// <summary>A narrow dialog, for a short question or a single field.</summary>
    Small = 0,
    /// <summary>The ordinary width.</summary>
    Medium,
    /// <summary>A wide dialog, for forms and lists.</summary>
    Large,
    /// <summary>Covering the whole window.</summary>
    FullScreen,
}

/// <summary>
/// A window-local dialog: modal by default, with a header (<see cref="Leading" /> content such as a severity mark, the
/// <see cref="Title" /> that names the dialog, a <see cref="Subtitle" /> that describes it, and a localized close button),
/// a scrolling body (the child content) and a <see cref="Footer" /> for the <see cref="DialogButton" /> actions, ordered by
/// role by the theme. Escape and the close button run the Cancel action when there is one, otherwise they dismiss the
/// dialog. Parts: <c>DialogScrim</c>, <c>Dialog</c> (<c>size</c>, <c>dialog-placement</c>), <c>DialogHeader</c>,
/// <c>DialogLeading</c>, <c>DialogHeading</c>, <c>DialogTitle</c>, <c>DialogSubtitle</c>, <c>DialogClose</c>,
/// <c>DialogBody</c> and <c>DialogFooter</c>.
/// </summary>
public sealed class Dialog : DialogBase
{
    private ElementReference body;
    private ElementReference footer;

    /// <summary>Gets or sets the title, which names the dialog.</summary>
    [Parameter] public string? Title { get; set; }

    /// <summary>Gets or sets rich title content, used instead of <see cref="Title" />.</summary>
    [Parameter] public RenderFragment? TitleContent { get; set; }

    /// <summary>Gets or sets a line below the title, which describes the dialog.</summary>
    [Parameter] public string? Subtitle { get; set; }

    /// <summary>Gets or sets content shown before the title, such as a severity mark or an icon.</summary>
    [Parameter] public RenderFragment? Leading { get; set; }

    /// <summary>Gets or sets the body of the dialog.</summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>Gets or sets the footer, usually <see cref="DialogButton" /> actions.</summary>
    [Parameter] public RenderFragment? Footer { get; set; }

    /// <summary>Gets or sets the sizing policy; Auto follows the theme, and the other values select explicit width steps.</summary>
    [Parameter] public DialogSize Size { get; set; } = DialogSize.Auto;

    /// <summary>
    /// Gets or sets the role used as the dialog's Enter action and automatic accent button; null selects no implicit
    /// default. A button's explicit <see cref="DialogButton.IsDefault" /> overrides its role's policy. Multiple effective
    /// default buttons are invalid. Focused buttons still handle Enter normally, and content that handles Enter suppresses
    /// the default action. The body receives initial focus before the enabled default action or other footer controls.
    /// </summary>
    [Parameter] public DialogButtonRole? DefaultButton { get; set; }

    /// <summary>Gets or sets where the dialog appears; <see cref="DialogSize.FullScreen" /> always covers the window.</summary>
    [Parameter] public DialogPlacement Placement { get; set; }

    /// <summary>Gets or sets whether the dialog confines interaction and the focus and shows a scrim.</summary>
    [Parameter] public bool Modal { get; set; } = true;

    /// <summary>Gets or sets whether the header shows a close button.</summary>
    [Parameter] public bool ShowCloseButton { get; set; } = true;

    /// <summary>Gets or sets whether a press outside the dialog dismisses it.</summary>
    [Parameter] public bool CloseOnOutside { get; set; }

    /// <summary>Gets the NSS element name emitted by Dialog.</summary>
    protected override string ElementName => "Dialog";

    /// <summary>Gets the placement, full screen for <see cref="DialogSize.FullScreen" />.</summary>
    protected override DialogPlacement SurfacePlacement => Size == DialogSize.FullScreen ? DialogPlacement.FullScreen : Placement;

    /// <summary>Gets whether the dialog is modal.</summary>
    protected override bool IsModal => Modal;

    /// <summary>Gets whether a press outside dismisses the dialog.</summary>
    protected override bool DismissOnOutside => CloseOnOutside;

    /// <summary>Gets whether the dialog has a title part.</summary>
    protected override bool HasTitle => Title is not null || TitleContent is not null;

    /// <summary>Gets whether the dialog has a subtitle part.</summary>
    protected override bool HasDescription => Subtitle is not null;

    /// <summary>Gets the explicitly selected default role; null means no implicit default action.</summary>
    protected override DialogButtonRole? DefaultActionRole => DefaultButton;

    /// <summary>Validates the size, placement and optional default action role.</summary>
    protected override void Validate()
    {
        if (!Enum.IsDefined(Size)) throw new ArgumentOutOfRangeException(nameof(Size));
        if (!Enum.IsDefined(Placement)) throw new ArgumentOutOfRangeException(nameof(Placement));
        if (DefaultButton is { } action && !Enum.IsDefined(action)) throw new ArgumentOutOfRangeException(nameof(DefaultButton));
    }

    /// <summary>Adds the size attribute.</summary>
    /// <param name="attributes">The attributes forwarded to the surface.</param>
    protected override void AddSurfaceAttributes(IDictionary<string, object?> attributes) => attributes["size"] = SizeName(Size);

    /// <summary>Focuses the first control of the body, then the enabled default action or another footer control, before the close button.</summary>
    /// <returns>The element to focus, or null.</returns>
    protected override ElementReference? DefaultInitialFocus()
    {
        if (FirstFocusable(body) is { } content) return content;
        if (footer.Element is { IsRemoved: false } element &&
            ControlTree.Descendants(element).FirstOrDefault(action => action.Attributes.GetValueOrDefault("default") is true && ControlTree.IsFocusable(action)) is { } selected)
            return new ElementReference(selected);
        return FirstFocusable(footer);
    }

    /// <summary>Renders the header, the body and the footer.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void BuildContent(RenderTreeBuilder builder)
    {
        if (HasTitle || Subtitle is not null || Leading is not null || ShowCloseButton)
        {
            builder.OpenElement(0, "DialogHeader");
            if (Leading is not null) OverlayParts.Part(builder, 1, "DialogLeading", Leading);
            builder.OpenElement(10, "DialogHeading");
            if (HasTitle)
            {
                builder.OpenElement(11, "DialogTitle");
                builder.AddAttribute(12, "id", TitleId);
                builder.AddAttribute(13, "role", "heading");
                builder.AddAttribute(14, "level", 2);
                if (TitleContent is not null) builder.AddContent(15, TitleContent);
                else builder.AddContent(16, Title);
                builder.CloseElement();
            }
            if (Subtitle is not null)
            {
                builder.OpenElement(20, "DialogSubtitle");
                builder.AddAttribute(21, "id", DescriptionId);
                builder.AddAttribute(22, "role", "text");
                builder.AddContent(23, Subtitle);
                builder.CloseElement();
            }
            builder.CloseElement();
            if (ShowCloseButton) OverlayParts.CloseButton(builder, 30, "DialogClose", this, DismissAsync);
            builder.CloseElement();
        }
        builder.OpenElement(50, "DialogBody");
        builder.AddElementReferenceCapture(51, reference => body = reference);
        builder.AddContent(52, ChildContent);
        builder.CloseElement();
        if (Footer is null) return;
        builder.OpenElement(60, "DialogFooter");
        builder.AddElementReferenceCapture(61, reference => footer = reference);
        builder.AddContent(62, Footer);
        builder.CloseElement();
    }

    private static string SizeName(DialogSize size) => size switch
    {
        DialogSize.Auto => "auto", DialogSize.Small => "small", DialogSize.Large => "large", DialogSize.FullScreen => "fullscreen", _ => "medium",
    };
}
