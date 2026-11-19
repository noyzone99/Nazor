using Nazor.Rendering;

namespace Nazor.Controls;

/// <summary>A styled segment of running text. In a text flow it shares wrapping, selection and baselines with adjacent text and inline controls.</summary>
public class Span : ContentControl
{
    /// <summary>Gets the NSS element name emitted by Span.</summary>
    protected override string ElementName => "Span";
}

/// <summary>A semantically important inline text segment; themes normally use a stronger weight.</summary>
public sealed class Strong : Span
{
    /// <summary>Gets the NSS element name emitted by Strong.</summary>
    protected override string ElementName => "Strong";
}

/// <summary>An emphasized inline text segment; themes normally use italic text.</summary>
public sealed class Emphasis : Span
{
    /// <summary>Gets the NSS element name emitted by Emphasis.</summary>
    protected override string ElementName => "Emphasis";
}

/// <summary>Inline source code with theme-owned font and decoration; use CodeBlock for a standalone code listing.</summary>
public sealed class InlineCode : Span
{
    /// <summary>Gets the NSS element name emitted by InlineCode.</summary>
    protected override string ElementName => "InlineCode";
}

/// <summary>A highlighted inline passage whose colors and padding are controlled by the theme.</summary>
public sealed class Mark : Span
{
    /// <summary>Gets the NSS element name emitted by Mark.</summary>
    protected override string ElementName => "Mark";
}

/// <summary>An abbreviated inline passage. Set Tooltip or AccessibleDescription to provide its expansion.</summary>
public sealed class Abbreviation : Span
{
    /// <summary>Gets the NSS element name emitted by Abbreviation.</summary>
    protected override string ElementName => "Abbreviation";

    /// <summary>Exposes described abbreviations as accessible text without adding a keyboard focus stop.</summary>
    /// <param name="builder">The builder receiving the render frames.</param>
    protected override void AddAttributes(RenderTreeBuilder builder)
    {
        if (!string.IsNullOrWhiteSpace(AccessibleDescription) || !string.IsNullOrWhiteSpace(Tooltip)) builder.AddAttribute(10, "role", "text");
    }
}

/// <summary>An inline passage removed from an earlier version of a document.</summary>
public sealed class Deleted : Span
{
    /// <summary>Gets the NSS element name emitted by Deleted.</summary>
    protected override string ElementName => "Deleted";
}

/// <summary>An inline passage added to a document.</summary>
public sealed class Inserted : Span
{
    /// <summary>Gets the NSS element name emitted by Inserted.</summary>
    protected override string ElementName => "Inserted";
}

/// <summary>A smaller inline passage lowered below the shared baseline, such as a chemical index.</summary>
public sealed class Subscript : Span
{
    /// <summary>Gets the NSS element name emitted by Subscript.</summary>
    protected override string ElementName => "Subscript";
}

/// <summary>A smaller inline passage raised above the shared baseline, such as an exponent.</summary>
public sealed class Superscript : Span
{
    /// <summary>Gets the NSS element name emitted by Superscript.</summary>
    protected override string ElementName => "Superscript";
}

/// <summary>A smaller inline passage, such as a qualification or attribution.</summary>
public sealed class SmallText : Span
{
    /// <summary>Gets the NSS element name emitted by SmallText.</summary>
    protected override string ElementName => "SmallText";
}

/// <summary>An inline quotation, with configurable visible quotation marks included in selection and accessible text.</summary>
public sealed class Quote : Span
{
    /// <summary>Gets or sets the opening quotation text; the default is a left double quotation mark.</summary>
    [Parameter] public string OpeningQuote { get; set; } = "“";

    /// <summary>Gets or sets the closing quotation text; the default is a right double quotation mark.</summary>
    [Parameter] public string ClosingQuote { get; set; } = "”";

    /// <summary>Gets the NSS element name emitted by Quote.</summary>
    protected override string ElementName => "Quote";

    /// <summary>Adds the quotation marks around the running content.</summary>
    /// <param name="builder">The builder receiving the render frames.</param>
    protected override void AddContent(RenderTreeBuilder builder)
    {
        builder.AddContent(1000, OpeningQuote);
        builder.AddContent(1001, ChildContent);
        builder.AddContent(1002, ClosingQuote);
    }
}

/// <summary>Forces a line break inside running text.</summary>
public sealed class LineBreak : Control
{
    /// <summary>Gets the NSS element name emitted by LineBreak.</summary>
    protected override string ElementName => "LineBreak";

    /// <summary>Marks the element as a hard inline line break.</summary>
    /// <param name="builder">The builder receiving the render frames.</param>
    protected override void AddAttributes(RenderTreeBuilder builder) => builder.AddAttribute(10, "line-break", true);

    /// <summary>Adds the line separator to the document's logical text.</summary>
    /// <param name="builder">The builder receiving the render frames.</param>
    protected override void AddContent(RenderTreeBuilder builder) => builder.AddContent(1000, "\n");
}
