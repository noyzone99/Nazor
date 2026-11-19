namespace Nazor.Graphics.Text;

/// <summary>Font metrics in design units (ascender and descender both positive).</summary>
/// <param name="UnitsPerEm">The units per em (int).</param>
/// <param name="Ascender">The ascender (int).</param>
/// <param name="Descender">The descender (int).</param>
/// <param name="LineGap">The line gap (int).</param>
public readonly record struct FontMetrics(int UnitsPerEm, int Ascender, int Descender, int LineGap);

/// <summary>
/// A font face resolved by an <see cref="IFontProvider" />: opaque to layout, which uses only the metrics and glyph
/// coverage. The backend that created it rasterizes it and shapes text with it, so measuring and drawing use the same
/// file.
/// </summary>
public abstract class FontFace
{
    /// <summary>Gets the resolved font family name.</summary>
    public abstract string FamilyName { get; }

    /// <summary>Gets the numeric weight of the font face.</summary>
    public abstract int Weight { get; }

    /// <summary>Gets whether the font face uses italic styling.</summary>
    public abstract bool Italic { get; }

    /// <summary>Gets the font's unscaled vertical metrics and design units.</summary>
    public abstract FontMetrics Metrics { get; }

    /// <summary>Determines whether the font contains a glyph for the specified Unicode scalar value.</summary>
    /// <param name="codepoint">The Unicode scalar value of the character.</param>
    /// <returns>true if this face contains the requested character; otherwise, false.</returns>
    public abstract bool HasGlyph(int codepoint);

    /// <summary>Gets an unshaped horizontal glyph advance at the requested logical size, or null when unavailable.</summary>
    /// <param name="codepoint">The Unicode scalar value of the character.</param>
    /// <param name="size">The nonnegative logical font size.</param>
    /// <returns>The advance in logical pixels, or null when this backend does not supply the metric.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The scalar or size is invalid.</exception>
    public virtual float? GetGlyphAdvance(int codepoint, float size)
    {
        if (!System.Text.Rune.IsValid(codepoint)) throw new ArgumentOutOfRangeException(nameof(codepoint));
        if (!float.IsFinite(size) || size < 0) throw new ArgumentOutOfRangeException(nameof(size));
        return null;
    }

    /// <summary>Gets the used font x-height at the requested logical size, or null when unavailable.</summary>
    /// <param name="size">The nonnegative logical font size.</param>
    /// <returns>The x-height in logical pixels, or null when this backend does not supply it.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The size is negative or nonfinite.</exception>
    public virtual float? GetXHeight(float size)
    {
        if (!float.IsFinite(size) || size < 0) throw new ArgumentOutOfRangeException(nameof(size));
        return null;
    }

    /// <summary>Scales the font ascent to the specified logical font size.</summary>
    /// <param name="size">The logical font size used for measurement or shaping.</param>
    /// <returns>The ascent in logical units at the requested font size.</returns>
    public float Ascent(float size) => Metrics.Ascender * size / Metrics.UnitsPerEm;

    /// <summary>Scales the font descent to the specified logical font size.</summary>
    /// <param name="size">The logical font size used for measurement or shaping.</param>
    /// <returns>The descent in logical units at the requested font size.</returns>
    public float Descent(float size) => Metrics.Descender * size / Metrics.UnitsPerEm;

    /// <summary>Scales the font line gap to the specified logical font size.</summary>
    /// <param name="size">The logical font size used for measurement or shaping.</param>
    /// <returns>The line gap in logical units at the requested font size.</returns>
    public float LineGap(float size) => Metrics.LineGap * size / Metrics.UnitsPerEm;

    /// <summary>Returns a textual representation of this value.</summary>
    /// <returns>The textual representation of this value.</returns>
    public override string ToString() => $"{FamilyName} {Weight}{(Italic ? " italic" : "")}";
}

/// <summary>Resolves families to faces and finds fallback faces for characters the face lacks.</summary>
public interface IFontProvider
{
    /// <summary>The first available family of the list, or the provider's default face.</summary>
    /// <param name="families">The preferred font families, in fallback order.</param>
    /// <param name="weight">The requested numeric font weight.</param>
    /// <param name="italic">Whether an italic face is preferred.</param>
    /// <returns>The resulting FontFace value.</returns>
    FontFace Match(IReadOnlyList<string> families, int weight, bool italic);

    /// <summary>Resolves a face at the requested logical size, including optical sizing when supported.</summary>
    /// <param name="families">The preferred font families, in fallback order.</param>
    /// <param name="weight">The requested numeric font weight.</param>
    /// <param name="italic">Whether an italic face is preferred.</param>
    /// <param name="size">The font size in logical pixels.</param>
    /// <returns>The resolved font face.</returns>
    FontFace Match(IReadOnlyList<string> families, int weight, bool italic, float size) => Match(families, weight, italic);

    /// <summary>Resolves text style coordinates, including optical sizing and explicit axes when supported.</summary>
    /// <param name="style">The immutable text style to resolve.</param>
    /// <returns>The resolved font face. Providers without variation support may ignore explicit axes.</returns>
    FontFace Match(TextStyle style) => style.OpticalSizing == FontOpticalSizing.None
        ? Match(style.FontFamilies, style.FontWeight, style.Italic)
        : Match(style.FontFamilies, style.FontWeight, style.Italic, style.FontSize);

    /// <summary>A face that contains <paramref name="codepoint" />, or null when none is available.</summary>
    /// <param name="codepoint">The Unicode scalar value of the character.</param>
    /// <param name="preferred">The preferred font face before glyph fallback.</param>
    /// <returns>The resulting FontFace? value.</returns>
    FontFace? Fallback(int codepoint, FontFace preferred);

    /// <summary>Resolves glyph fallback with the requested text style coordinates when supported.</summary>
    /// <param name="codepoint">The Unicode scalar value to cover.</param>
    /// <param name="preferred">The face selected before glyph fallback.</param>
    /// <param name="style">The requested text style, including explicit variation coordinates.</param>
    /// <returns>A covering face, or null when unavailable.</returns>
    FontFace? Fallback(int codepoint, FontFace preferred, TextStyle style) => Fallback(codepoint, preferred);

    /// <summary>Increments when fonts are registered: cached layouts become stale.</summary>
    int Version { get; }
}

/// <summary>Glyphs of a text range shaped with one face, in logical units for the given font size.</summary>
public sealed class ShapedRun
{
    /// <summary>Initializes a new instance of ShapedRun.</summary>
    /// <param name="face">The font face used to shape or measure text.</param>
    /// <param name="size">The logical font size used for measurement or shaping.</param>
    /// <param name="start">The start offset in the UTF-16 source text.</param>
    /// <param name="length">The number of UTF-16 code units in the text segment.</param>
    /// <param name="glyphs">The font-specific glyph identifiers in drawing order.</param>
    /// <param name="advances">The horizontal advance of each glyph in logical units.</param>
    /// <param name="offsetsX">The horizontal offset of each glyph from its nominal position.</param>
    /// <param name="offsetsY">The vertical offset of each glyph from its nominal position.</param>
    /// <param name="clusters">The source-text UTF-16 cluster offsets associated with the glyphs.</param>
    public ShapedRun(FontFace face, float size, int start, int length, ushort[] glyphs, float[] advances, float[] offsetsX, float[] offsetsY, int[] clusters)
    {
        Face = face;
        Size = size;
        Start = start;
        Length = length;
        Glyphs = glyphs;
        Advances = advances;
        OffsetsX = offsetsX;
        OffsetsY = offsetsY;
        Clusters = clusters;
        Width = advances.Sum();
    }

    /// <summary>Gets the font face used by this glyph run.</summary>
    public FontFace Face { get; }

    /// <summary>Actually applied outline synthesis, independent of the resolved real face.</summary>
    public FontSynthesis Synthesis { get; init; }

    /// <summary>Conservative additional ink extent for synthetic styling; advances and line metrics are unchanged.</summary>
    public float InkOutset => ((Synthesis & FontSynthesis.Style) != 0 ? Math.Max(Face.Ascent(Size),Face.Descent(Size)) * .21255656f : 0) +
        ((Synthesis & FontSynthesis.Weight) != 0 ? Size / 8 : 0);

    /// <summary>Gets the logical font size used to shape the run.</summary>
    public float Size { get; }

    /// <summary>UTF-16 range of the source text covered by the run.</summary>
    public int Start { get; }

    /// <summary>Gets the length of the source text segment in UTF-16 code units.</summary>
    public int Length { get; }

    /// <summary>Gets the font-specific glyph identifiers in drawing order.</summary>
    public ushort[] Glyphs { get; }

    /// <summary>Gets the horizontal advance of each glyph in logical units.</summary>
    public float[] Advances { get; }

    /// <summary>Gets the horizontal positioning adjustment for each glyph.</summary>
    public float[] OffsetsX { get; }

    /// <summary>Gets the vertical positioning adjustment for each glyph.</summary>
    public float[] OffsetsY { get; }

    /// <summary>For each glyph, the UTF-16 index (in the source text) where its cluster starts.</summary>
    public int[] Clusters { get; }

    /// <summary>Gets the total horizontal advance of the run in logical units.</summary>
    public float Width { get; }

    /// <summary>The glyphs whose clusters fall in [start, end), without shaping again.</summary>
    /// <param name="start">The start offset in the UTF-16 source text.</param>
    /// <param name="end">The end offset in the UTF-16 source text.</param>
    /// <returns>The resulting ShapedRun value.</returns>
    public ShapedRun Slice(int start, int end)
    {
        var indices = Enumerable.Range(0, Glyphs.Length).Where(i => Clusters[i] >= start && Clusters[i] < end).ToArray();
        return new ShapedRun(Face, Size, start, end - start,
            indices.Select(i => Glyphs[i]).ToArray(), indices.Select(i => Advances[i]).ToArray(),
            indices.Select(i => OffsetsX[i]).ToArray(), indices.Select(i => OffsetsY[i]).ToArray(), indices.Select(i => Clusters[i]).ToArray()) { Synthesis = Synthesis };
    }
}

/// <summary>Shapes a UTF-16 range with a face; clusters are indices of the source string.</summary>
public interface ITextShaper
{
    /// <summary>Shapes a UTF-16 text segment into positioned glyphs using the specified font face and size.</summary>
    /// <param name="text">The text to process.</param>
    /// <param name="start">The start offset in the UTF-16 source text.</param>
    /// <param name="length">The number of UTF-16 code units in the text segment.</param>
    /// <param name="face">The font face used to shape or measure text.</param>
    /// <param name="size">The logical font size used for measurement or shaping.</param>
    /// <returns>The positioned glyph run for the source text segment.</returns>
    ShapedRun Shape(string text, int start, int length, FontFace face, float size);

    /// <summary>Shapes with explicit features and permitted synthesis. Existing shapers may delegate to the size-only path.</summary>
    /// <param name="text">The complete source text.</param>
    /// <param name="start">The UTF-16 range start.</param>
    /// <param name="length">The UTF-16 range length.</param>
    /// <param name="face">The resolved real face.</param>
    /// <param name="style">Requested features, synthesis and size.</param>
    /// <returns>The positioned glyph run.</returns>
    ShapedRun Shape(string text, int start, int length, FontFace face, TextStyle style) => Shape(text,start,length,face,style.FontSize);
}

/// <summary>Specifies the available line height kind values.</summary>
public enum LineHeightKind : byte
{
    /// <summary>Uses the font's normal line metrics.</summary>
    Normal,

    /// <summary>Multiplies the font size by a unitless line-height factor.</summary>
    Multiplier,

    /// <summary>Uses an explicitly resolved line height.</summary>
    Fixed,
}

/// <summary>Where a line may break inside a word that does not fit (CSS overflow-wrap).</summary>
public enum BreakWords : byte
{
    /// <summary>Only after spaces: a long word overflows.</summary>
    Never,

    /// <summary>
    /// Between graphemes of a word that does not fit on a line of its own; min-content stays the word.
    /// </summary>
    WhenOverflowing,

    /// <summary>As <see cref = "WhenOverflowing"/>, and min-content is the widest grapheme.</summary>
    Anywhere,
}

/// <summary>The text properties that affect shaping and line metrics (color is a paint property).</summary>
/// <param name="FontFamilies">The font families (IReadOnlyList&lt;string&gt;).</param>
/// <param name="FontSize">The font size (float).</param>
/// <param name="FontWeight">The font weight (int).</param>
/// <param name="Italic">Whether an italic face is preferred.</param>
/// <param name="LineHeightKind">The line height kind (LineHeightKind).</param>
/// <param name="LineHeight">The line height (float).</param>
/// <param name="LetterSpacing">The letter spacing (float).</param>
/// <param name="BreakWords">The break words (BreakWords).</param>
public sealed record TextStyle(IReadOnlyList<string> FontFamilies, float FontSize, int FontWeight = 400, bool Italic = false,
    LineHeightKind LineHeightKind = LineHeightKind.Normal, float LineHeight = 0, float LetterSpacing = 0, BreakWords BreakWords = BreakWords.Never)
{
    private FontSynthesis synthesis;
    private IReadOnlyDictionary<string,uint> fontFeatures = new System.Collections.ObjectModel.ReadOnlyDictionary<string,uint>(new Dictionary<string,uint>());

    /// <summary>Gets or initializes permitted synthetic styling. None is the default.</summary>
    /// <exception cref="ArgumentOutOfRangeException">Undefined flags are supplied.</exception>
    public FontSynthesis Synthesis
    {
        get => synthesis;
        init => synthesis = (value & ~(FontSynthesis.Weight | FontSynthesis.Style)) == 0 ? value : throw new ArgumentOutOfRangeException(nameof(value));
    }

    /// <summary>Gets or initializes copied, sorted, case-sensitive OpenType feature selections.</summary>
    /// <exception cref="ArgumentNullException">The dictionary is null.</exception>
    /// <exception cref="ArgumentException">A tag is not four printable ASCII characters.</exception>
    public IReadOnlyDictionary<string,uint> FontFeatures
    {
        get => fontFeatures;
        init
        {
            ArgumentNullException.ThrowIfNull(value);
            var copy = new SortedDictionary<string,uint>(StringComparer.Ordinal);
            foreach (var feature in value)
            {
                if (feature.Key is not { Length:4 } || feature.Key.Any(c => c is < ' ' or > '~')) throw new ArgumentException("Feature tags require four printable ASCII characters.",nameof(value));
                copy[feature.Key] = feature.Value;
            }
            fontFeatures = new System.Collections.ObjectModel.ReadOnlyDictionary<string,uint>(copy);
        }
    }

    private FontOpticalSizing opticalSizing;
    private IReadOnlyDictionary<string, double> fontVariations = new System.Collections.ObjectModel.ReadOnlyDictionary<string, double>(new Dictionary<string, double>());

    /// <summary>Gets or initializes automatic optical sizing. Native is the default for directly constructed graphics styles.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The mode is undefined.</exception>
    public FontOpticalSizing OpticalSizing
    {
        get => opticalSizing;
        init => opticalSizing = Enum.IsDefined(value) ? value : throw new ArgumentOutOfRangeException(nameof(value));
    }

    /// <summary>Gets or initializes explicit, case-sensitive OpenType axes. Initialization copies and sorts the input; explicit axes override automatic sizing.</summary>
    /// <exception cref="ArgumentNullException">The dictionary is null.</exception>
    /// <exception cref="ArgumentException">A tag is not four printable ASCII characters, or a coordinate is nonfinite.</exception>
    public IReadOnlyDictionary<string, double> FontVariations
    {
        get => fontVariations;
        init
        {
            ArgumentNullException.ThrowIfNull(value);
            var copy = new SortedDictionary<string, double>(StringComparer.Ordinal);
            foreach (var axis in value)
            {
                if (axis.Key is not { Length: 4 } || axis.Key.Any(c => c is < ' ' or > '~') || !double.IsFinite(axis.Value))
                    throw new ArgumentException("Variation axes require four printable ASCII characters and finite coordinates.", nameof(value));
                copy[axis.Key] = axis.Value;
            }
            fontVariations = new System.Collections.ObjectModel.ReadOnlyDictionary<string, double>(copy);
        }
    }

    /// <summary>Determines whether the supplied value is equal to this instance.</summary>
    /// <param name="other">The value to compare with this instance.</param>
    /// <returns>true if the supplied value satisfies the comparison; otherwise, false.</returns>
    public bool Equals(TextStyle? other) => other is not null && FontFamilies.SequenceEqual(other.FontFamilies) && FontSize == other.FontSize &&
        FontWeight == other.FontWeight && Italic == other.Italic && LineHeightKind == other.LineHeightKind && LineHeight == other.LineHeight &&
        LetterSpacing == other.LetterSpacing && BreakWords == other.BreakWords && OpticalSizing == other.OpticalSizing &&
        Synthesis == other.Synthesis && FontFeatures.SequenceEqual(other.FontFeatures) && FontVariations.SequenceEqual(other.FontVariations);

    /// <summary>Returns a hash code consistent with equality for this value.</summary>
    /// <returns>The hash code for this value.</returns>
    public override int GetHashCode()
    {
        var hash = new HashCode();
        foreach (var family in FontFamilies) hash.Add(family);
        hash.Add(FontSize);
        hash.Add(FontWeight);
        hash.Add(Italic);
        hash.Add(LineHeightKind);
        hash.Add(LineHeight);
        hash.Add(LetterSpacing);
        hash.Add(BreakWords);
        hash.Add(OpticalSizing); hash.Add(Synthesis);
        foreach (var feature in FontFeatures) { hash.Add(feature.Key,StringComparer.Ordinal); hash.Add(feature.Value); }
        foreach (var axis in FontVariations) { hash.Add(axis.Key, StringComparer.Ordinal); hash.Add(axis.Value); }
        return hash.ToHashCode();
    }
}
