using Nazor.Rendering;

namespace Nazor.Controls;

/// <summary>
/// A block of running text, such as the body of an article or the explanation in a dialog. Consecutive paragraphs and
/// other document blocks are spaced by the theme. The element is <c>Paragraph</c> with the <c>text</c> role.
/// </summary>
public sealed class Paragraph : ContentControl
{
    /// <summary>
    /// Gets or sets whether the text can be selected with the pointer and copied with the keyboard or the context menu.
    /// The paragraph takes the focus when clicked, but Tab does not stop there.
    /// </summary>
    [Parameter] public bool IsTextSelectionEnabled { get; set; }

    /// <summary>Gets the NSS element name emitted by Paragraph.</summary>
    protected override string ElementName => "Paragraph";

    /// <summary>Adds the text role and the selection behavior.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void AddAttributes(RenderTreeBuilder builder)
    {
        builder.AddAttribute(10, "role", "text");
        builder.AddAttribute(11, "selectable", IsTextSelectionEnabled);
        builder.AddAttribute(12, "focusable", IsTextSelectionEnabled);
        if (IsTextSelectionEnabled && TabIndex is null) builder.AddAttribute(13, "tabindex", -1);
    }
}

/// <summary>
/// A single key of the keyboard drawn as a key cap, such as <c>Ctrl</c> or <c>Enter</c> in an instruction. Write one
/// <see cref="Kbd" /> per key; <see cref="ShortcutLabel" /> formats a whole shortcut in the notation of the platform.
/// The element is <c>Kbd</c> with the <c>text</c> role.
/// </summary>
public sealed class Kbd : ContentControl
{
    /// <summary>Gets the NSS element name emitted by Kbd.</summary>
    protected override string ElementName => "Kbd";

    /// <summary>Adds the text role.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void AddAttributes(RenderTreeBuilder builder) => builder.AddAttribute(10, "role", "text");
}

/// <summary>
/// A passage quoted from another source, optionally followed by its source (<see cref="Cite" /> or
/// <see cref="CiteContent" />). The element is <c>Blockquote</c>, a group described as a quote for assistive
/// technologies; the parts are <c>BlockquoteContent</c> and <c>BlockquoteCite</c>.
/// </summary>
public sealed class Blockquote : ContentControl
{
    /// <summary>Gets or sets the source of the quotation as plain text, such as an author or a title.</summary>
    [Parameter] public string? Cite { get; set; }

    /// <summary>Gets or sets the source of the quotation as content; it replaces <see cref="Cite" />.</summary>
    [Parameter] public RenderFragment? CiteContent { get; set; }

    /// <summary>Gets the NSS element name emitted by Blockquote.</summary>
    protected override string ElementName => "Blockquote";

    /// <summary>Adds the group role described as a quote.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void AddAttributes(RenderTreeBuilder builder)
    {
        builder.AddAttribute(10, "role", "group");
        builder.AddAttribute(11, "role-description", ControlStrings.Get("Quote"));
    }

    /// <summary>Adds the quoted content and the source.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void AddContent(RenderTreeBuilder builder)
    {
        builder.OpenElement(1000, "BlockquoteContent");
        builder.AddContent(1001, ChildContent);
        builder.CloseElement();
        if (CiteContent is null && string.IsNullOrEmpty(Cite)) return;
        builder.OpenElement(1010, "BlockquoteCite");
        builder.AddAttribute(1011, "role", "text");
        if (CiteContent is not null) builder.AddContent(1012, CiteContent);
        else builder.AddContent(1013, Cite);
        builder.CloseElement();
    }
}
