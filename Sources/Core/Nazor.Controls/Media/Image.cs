using Nazor.Input;
using Nazor.Rendering;

namespace Nazor.Controls;

/// <summary>
/// Shows an image from a file, an embedded resource, a stream, bytes or a URI (a string converts:
/// <c>Source="images/logo.png"</c>). The image loads and decodes off the UI thread; meanwhile the element is empty, then it
/// takes the natural size of the image unless NSS sizes it, and <c>object-fit</c> and <c>object-position</c> place it in
/// the box. <see cref="Alt" /> is the text for assistive technologies: an image without it is decorative and hidden from them.
/// </summary>
public sealed class Image : Control
{
    /// <summary>Gets or sets the file, URI, resource, or byte source to display.</summary>
    [Parameter] public ImageSource? Source { get; set; }

    /// <summary>Gets or sets the text alternative; empty or null for a decorative image.</summary>
    [Parameter] public string? Alt { get; set; }

    /// <summary>Gets or sets the callback invoked when the image was loaded and decoded, with its natural size.</summary>
    [Parameter] public EventCallback<ImageLoadEventArgs> OnLoad { get; set; }

    /// <summary>Gets or sets the callback invoked when the image could not be read or decoded; the element stays empty.</summary>
    [Parameter] public EventCallback<ImageErrorEventArgs> OnError { get; set; }

    /// <summary>Gets the NSS element name emitted by Image.</summary>
    protected override string ElementName => "Image";

    /// <summary>Adds the image source, its alternative text and the load events.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void AddAttributes(RenderTreeBuilder builder)
    {
        builder.AddAttribute(10, "source", Source);
        builder.AddAttribute(11, "role", string.IsNullOrWhiteSpace(Alt) && AccessibleName is null ? null : "image");
        builder.AddAttribute(12, "alt", Alt);
        builder.AddAttribute(13, "onload", OnLoad);
        builder.AddAttribute(14, "onerror", OnError);
    }
}

/// <summary>
/// A monochrome icon in the current <c>color</c>, 1em square by default: a typed <see cref="Symbol" />, a raw
/// <see cref="Glyph" /> of an icon font, or an
/// image, usually an SVG, whose pixels all take the color. Basic uses a line height of one inside the icon box;
/// applications can override it. It is decorative unless it has an
/// <see cref="Control.AccessibleName" />; name the action that contains it instead when possible.
/// </summary>
public sealed class Icon : Control
{
    private IReadOnlyList<FontSymbol>? symbolCandidates;
    /// <summary>Gets or sets raw icon-font text. Used when both Source and Symbol are null; its font comes from NSS.</summary>
    [Parameter] public string? Glyph { get; set; }

    /// <summary>Gets or sets a typed glyph and font family, with optional provider-resolved alternatives. Used when Source is null, before Glyph.</summary>
    [Parameter] public IFontSymbol? Symbol { get; set; }

    /// <summary>Gets or sets an image drawn in the current color, taking precedence over Symbol and Glyph.</summary>
    [Parameter] public ImageSource? Source { get; set; }

    /// <summary>Gets the NSS element name emitted by Icon.</summary>
    protected override string ElementName => "Icon";

    /// <summary>Gets the caller's styles with the active symbol's font family applied last. Color and size remain styleable.</summary>
    protected override string? ElementStyle => Source is null && Symbol is { } symbol
        ? Css.Join(Style, "font-family: \"" + symbol.FontFamily.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"")
        : Style;

    /// <summary>Validates the active font symbol.</summary>
    /// <exception cref="ArgumentException">An active symbol has blank glyph text, a blank family, or a family containing control characters.</exception>
    protected override void OnParametersSet()
    {
        symbolCandidates = null;
        if (Source is not null || Symbol is not { } symbol) return;
        var candidates = FontSymbols.Expand(symbol);
        symbolCandidates = symbol.Fallbacks.Count > 0 ? candidates : Array.Empty<FontSymbol>();
    }

    /// <summary>Adds the tinted image source, immutable symbol candidates and the image role when the icon is named.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void AddAttributes(RenderTreeBuilder builder)
    {
        builder.AddAttribute(10, "source", Source);
        builder.AddAttribute(11, "tint", Source is not null);
        builder.AddAttribute(12, "role", AccessibleName is null ? null : "image");
        builder.AddAttribute(13, "font-symbol-candidates", symbolCandidates);
    }

    /// <summary>Adds the symbol's glyph or raw glyph text when no image source is set.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void AddContent(RenderTreeBuilder builder)
    {
        if (Source is null) builder.AddContent(1000, Symbol?.Glyph ?? Glyph);
    }
}
