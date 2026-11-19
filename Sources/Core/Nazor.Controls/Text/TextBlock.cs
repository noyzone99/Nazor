using Nazor.Rendering;

namespace Nazor.Controls;

/// <summary>
/// A run of text. It does not establish a heading or a field label; use <see cref="Heading" /> and <see cref="Label" />
/// for those meanings.
/// </summary>
public sealed class TextBlock : ContentControl
{
    /// <summary>
    /// Gets or sets whether the text can be selected with the pointer (a double click selects a word, a triple click all)
    /// and copied with the keyboard or the context menu. The block takes the focus when clicked, but Tab does not stop there.
    /// </summary>
    [Parameter] public bool IsTextSelectionEnabled { get; set; }

    /// <summary>Gets the NSS element name emitted by TextBlock.</summary>
    protected override string ElementName => "TextBlock";

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

/// <summary>A heading of the document outline, from level 1 (the page title) to level 6.</summary>
public sealed class Heading : ContentControl
{
    /// <summary>Gets or sets the heading level, from one through six.</summary>
    [Parameter] public int Level { get; set; } = 1;

    /// <summary>Gets the NSS element name emitted by Heading.</summary>
    protected override string ElementName => "Heading";

    /// <summary>Validates the heading level.</summary>
    protected override void OnParametersSet()
    {
        if (Level is < 1 or > 6) throw new ArgumentOutOfRangeException(nameof(Level), "A heading level is between 1 and 6.");
    }

    /// <summary>Adds the heading role and level.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void AddAttributes(RenderTreeBuilder builder)
    {
        builder.AddAttribute(10, "role", "heading");
        builder.AddAttribute(11, "level", Level);
    }
}
