namespace Nazor.Themes.Fluent;

/// <summary>A named Regular or Filled Fluent System Icons glyph with its matching private font family.</summary>
/// <remarks>
/// Register the packaged fonts with <c>builder.Fonts.AddFluentFonts()</c> before startup.
/// Members include their design size and variant, for example <c>Add20Regular</c>. The default value is empty
/// and is not a valid active <c>Icon.Symbol</c>.
/// </remarks>
public readonly partial struct FluentSymbol : IFontSymbol
{
    private readonly string? fontFamily;
    private readonly string? glyph;

    private FluentSymbol(string fontFamily, string glyph)
    {
        this.fontFamily = fontFamily;
        this.glyph = glyph;
    }

    /// <summary>Gets the internal Regular or Filled font family; empty for the default value.</summary>
    public string FontFamily => fontFamily ?? string.Empty;

    /// <summary>Gets the Unicode glyph text, including surrogate pairs; empty for the default value.</summary>
    public string Glyph => glyph ?? string.Empty;
}
