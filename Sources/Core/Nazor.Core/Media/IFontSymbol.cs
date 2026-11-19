namespace Nazor;

/// <summary>A font icon's glyph and the family that gives that glyph its meaning, independent of the active theme.</summary>
/// <remarks>Register the required font before application startup. Implementations should be immutable.</remarks>
public interface IFontSymbol
{
    /// <summary>Gets the font's internal family name, not a file path or an NSS family list. Must not be blank or contain control characters.</summary>
    string FontFamily { get; }

    /// <summary>Gets the nonblank glyph text, which may contain a Unicode scalar represented by a surrogate pair.</summary>
    string Glyph { get; }

    /// <summary>Gets ordered alternatives whose glyphs have the same meaning in their respective families.</summary>
    /// <remarks>
    /// The font provider must resolve the exact family and cover the complete glyph text before an alternative is used.
    /// Resolution visits at most 32 distinct symbols in depth-first order, including this symbol; cycles are skipped.
    /// An empty list preserves the ordinary single-font symbol behavior. Implementations should return immutable lists.
    /// </remarks>
    IReadOnlyList<IFontSymbol> Fallbacks => Array.Empty<IFontSymbol>();
}
