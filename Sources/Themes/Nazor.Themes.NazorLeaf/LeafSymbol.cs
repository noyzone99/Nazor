namespace Nazor.Themes.NazorLeaf;

/// <summary>A named icon from the packaged NazorLeaf Icons font, usable with any theme through Icon.Symbol.</summary>
/// <remarks>Register the font with <c>builder.Fonts.AddNazorLeafFonts()</c>. The default value has an empty glyph and cannot be an active symbol.</remarks>
public readonly partial struct LeafSymbol : IFontSymbol
{
    private readonly string? glyph;

    private LeafSymbol(string glyph) => this.glyph = glyph;

    /// <summary>Gets the internal family name of the private icon font: NazorLeaf Icons.</summary>
    public string FontFamily => "NazorLeaf Icons";

    /// <summary>Gets the Unicode glyph text; empty for the default value.</summary>
    public string Glyph => glyph ?? string.Empty;
}
