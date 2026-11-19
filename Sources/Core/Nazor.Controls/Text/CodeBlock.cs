using System.Globalization;
using Nazor.Rendering;

namespace Nazor.Controls;

/// <summary>
/// A block of source code or other preformatted text in a monospaced font, keeping spaces and line breaks. The text is
/// <see cref="Code" /> or the content between the tags; it can be selected and copied. With
/// <see cref="ShowLineNumbers" /> a column of line numbers precedes it. <see cref="Language" /> is written as the
/// <c>language</c> attribute for themes and tools; the block does not color the syntax. The parts are
/// <c>CodeBlockLineNumbers</c> (decorative) and <c>CodeBlockCode</c> (the <c>text</c> role); the element has the
/// <c>wrap</c> attribute when long lines wrap.
/// </summary>
public sealed class CodeBlock : ContentControl
{
    private int measuredLines;

    /// <summary>Gets or sets the code; it replaces the content between the tags. Line endings are normalized.</summary>
    [Parameter] public string? Code { get; set; }

    /// <summary>Gets or sets the language of the code, such as <c>csharp</c>, written as the <c>language</c> attribute.</summary>
    [Parameter] public string? Language { get; set; }

    /// <summary>Gets or sets whether a column of line numbers precedes the code.</summary>
    [Parameter] public bool ShowLineNumbers { get; set; }

    /// <summary>Gets or sets the number of the first line.</summary>
    [Parameter] public int FirstLineNumber { get; set; } = 1;

    /// <summary>
    /// Gets or sets whether long lines wrap instead of scrolling horizontally. Line numbers stay aligned only with
    /// unwrapped lines.
    /// </summary>
    [Parameter] public bool Wrap { get; set; }

    /// <summary>Gets or sets whether the code can be selected and copied; true by default.</summary>
    [Parameter] public bool IsTextSelectionEnabled { get; set; } = true;

    /// <summary>Gets the normalized code text, or null when the content comes from between the tags.</summary>
    public string? Text => Code is null ? null : Normalize(Code);

    /// <summary>Gets the number of lines shown.</summary>
    public int LineCount => Text is { } text ? text.Split('\n').Length : Math.Max(1, measuredLines);

    /// <summary>Gets the NSS element name emitted by CodeBlock.</summary>
    protected override string ElementName => "CodeBlock";

    /// <summary>Validates the first line number.</summary>
    protected override void OnParametersSet()
    {
        if (FirstLineNumber < 0) throw new ArgumentOutOfRangeException(nameof(FirstLineNumber), "Line numbers start at zero or more.");
    }

    /// <summary>Adds the language, the wrapping and, when named, the group role described as code.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void AddAttributes(RenderTreeBuilder builder)
    {
        builder.AddAttribute(10, "language", string.IsNullOrWhiteSpace(Language) ? null : Language);
        builder.AddAttribute(11, "wrap", Wrap);
        builder.AddAttribute(12, "line-numbers", ShowLineNumbers);
        if (AccessibleName is null && LabeledBy is null) return;
        builder.AddAttribute(13, "role", "group");
        builder.AddAttribute(14, "role-description", ControlStrings.Get("Code"));
    }

    /// <summary>Adds the line numbers and the code.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void AddContent(RenderTreeBuilder builder)
    {
        if (ShowLineNumbers)
        {
            builder.OpenElement(1000, "CodeBlockLineNumbers");
            builder.AddAttribute(1001, "semantic-hidden", true);
            builder.AddContent(1002, string.Join("\n", Enumerable.Range(FirstLineNumber, LineCount).Select(n => n.ToString(CultureInfo.InvariantCulture))));
            builder.CloseElement();
        }
        builder.OpenElement(1010, "CodeBlockCode");
        builder.AddAttribute(1011, "role", "text");
        builder.AddAttribute(1012, "selectable", IsTextSelectionEnabled);
        builder.AddAttribute(1013, "focusable", IsTextSelectionEnabled);
        if (IsTextSelectionEnabled) builder.AddAttribute(1014, "tabindex", -1);
        if (Text is { } text) builder.AddContent(1015, text);
        else builder.AddContent(1016, ChildContent);
        builder.CloseElement();
    }

    /// <summary>Counts the lines of content written between the tags so the line numbers match.</summary>
    /// <param name="firstRender">Whether this is the first render.</param>
    protected override void OnAfterRender(bool firstRender)
    {
        if (!ShowLineNumbers || Code is not null || Element.Element is not { } element) return;
        var code = element.Children.OfType<ElementNode>().FirstOrDefault(node => node.Name == "CodeBlockCode");
        var lines = code is null ? 1 : Normalize(ControlTree.Text(code) ?? "").Split('\n').Length;
        if (lines == measuredLines) return;
        measuredLines = lines;
        StateHasChanged();
    }

    private static string Normalize(string code)
    {
        code = code.Replace("\r\n", "\n").Replace('\r', '\n');
        return code.EndsWith('\n') ? code[..^1] : code;
    }
}
