using System.Runtime.InteropServices;
using Nazor.Graphics.Text;
using SkiaSharp;
using HB = HarfBuzzSharp;

namespace Nazor.Skia;

internal sealed class SkiaFontFace : FontFace, IDisposable
{
    private readonly HB.Face face;

    public SkiaFontFace(SKTypeface typeface)
    {
        Typeface = typeface;
        face = new HB.Face(GetTable) { UnitsPerEm = typeface.UnitsPerEm };
        Font = new HB.Font(face);
        Font.SetFunctionsOpenType();
        Font.SetScale(face.UnitsPerEm, face.UnitsPerEm);
        var variations = typeface.VariationDesignPosition;
        if (variations.Length > 0)
            Font.SetVariations(variations.Select(v => new HB.Variation { Tag = v.Axis, Value = v.Value }).ToArray());

        Font.TryGetHorizontalFontExtents(out var extents);
        Metrics = new FontMetrics(face.UnitsPerEm, extents.Ascender, -extents.Descender, extents.LineGap);
    }

    public SKTypeface Typeface { get; }
    public HB.Font Font { get; }

    public override string FamilyName => Typeface.FamilyName;
    public override int Weight => Typeface.FontWeight;
    public override bool Italic => Typeface.FontSlant != SKFontStyleSlant.Upright;
    public override FontMetrics Metrics { get; }

    public override bool HasGlyph(int codepoint) => Font.TryGetGlyph(codepoint, out _);

    public override float? GetGlyphAdvance(int codepoint, float size)
    {
        base.GetGlyphAdvance(codepoint, size);
        return Font.TryGetGlyph(codepoint, out var glyph) ? Font.GetHorizontalGlyphAdvance(glyph) * size / Metrics.UnitsPerEm : null;
    }

    public override float? GetXHeight(float size)
    {
        base.GetXHeight(size);
        if (Font.OpenTypeMetrics.TryGetPosition(HB.OpenTypeMetricsTag.XHeight, out var height) && height > 0)
            return height * size / Metrics.UnitsPerEm;
        if (Font.TryGetGlyph('x', out var glyph) && Font.TryGetGlyphExtents(glyph, out var bounds) && bounds.Height != 0)
            return (float)(Math.Abs((double)bounds.Height) * size / Metrics.UnitsPerEm);
        return null;
    }

    private HB.Blob? GetTable(HB.Face _, HB.Tag tag)
    {
        var size = Typeface.GetTableSize(tag);
        if (size <= 0) return null;
        var memory = Marshal.AllocCoTaskMem(size);
        if (!Typeface.TryGetTableData(tag, 0, size, memory))
        {
            Marshal.FreeCoTaskMem(memory);
            return null;
        }
        return new HB.Blob(memory, size, HB.MemoryMode.ReadOnly, () => Marshal.FreeCoTaskMem(memory));
    }

    public void Dispose()
    {
        Font.Dispose();
        face.Dispose();
        Typeface.Dispose();
    }
}

/// <summary>
/// Resolves faces from private font files and, optionally, from the system font manager (DirectWrite on Windows).
/// </summary>
public sealed class SkiaFontProvider : IFontProvider, IDisposable
{
    private readonly FontOptions options;
    private readonly List<SkiaFontFace> privateFaces = [];
    private readonly Dictionary<SkiaFontFace, string[]> familyAliases = [];
    private readonly Dictionary<(string Family, int Weight, bool Italic), SkiaFontFace?> systemFaces = new();
    private readonly Dictionary<(int Codepoint, int Weight, bool Italic), SkiaFontFace?> fallbacks = new();
    private readonly List<SkiaFontFace> owned = [];
    private readonly Dictionary<(SkiaFontFace Face, string Coordinates), SkiaFontFace> variationFaces = [];
    private readonly Dictionary<(SkiaFontFace Face, int Weight), SkiaFontFace> weightedFaces = [];

    /// <summary>Initializes a new instance of SkiaFontProvider.</summary>
    /// <param name="options">The options used to configure the operation.</param>
    public SkiaFontProvider(FontOptions? options = null)
    {
        this.options = options ?? new FontOptions();
        try
        {
            foreach (var file in this.options.PrivateFontFiles) Register(Path.GetFullPath(file, AppContext.BaseDirectory));
        }
        catch
        {
            Dispose();
            throw;
        }
    }

    /// <summary>Gets the font-cache version, incremented when the available font set changes.</summary>
    public int Version { get; private set; }

    /// <summary>Registers a font file as a private family; cached layouts become stale.</summary>
    /// <param name="fontFile">The font file (string).</param>
    public void Register(string fontFile)
    {
        var typeface = SKFontManager.Default.CreateTypeface(fontFile, 0)
            ?? throw new InvalidOperationException($"The font file '{fontFile}' could not be loaded.");
        var face = new SkiaFontFace(typeface);
        privateFaces.Add(face);
        familyAliases.Add(face, SkiaFontNames.TypographicFamilies(typeface));
        owned.Add(face);
        fallbacks.Clear();
        Version++;
    }

    /// <summary>Resolves a font face from the ordered family list, weight, and italic preference.</summary>
    /// <param name="families">The preferred font families, in fallback order.</param>
    /// <param name="weight">The requested numeric font weight.</param>
    /// <param name="italic">Whether an italic face is preferred.</param>
    /// <returns>The resolved font face, using configured fallbacks when necessary.</returns>
    public FontFace Match(IReadOnlyList<string> families, int weight, bool italic)
    {
        foreach (var family in families.Append(options.DefaultFamily))
        {
            if (MatchPrivate(family, weight, italic) is { } privateFace) return privateFace;
            if (options.UseSystemFonts && MatchSystem(family, weight, italic) is { } systemFace) return systemFace;
        }
        if (privateFaces.Count > 0) return Closest(privateFaces, weight, italic);
        return MatchSystem(SKTypeface.Default.FamilyName, weight, italic)
            ?? throw new InvalidOperationException("No font is available.");
    }

    /// <summary>Resolves a face with its optical-size axis matched to the requested size.</summary>
    /// <param name="families">The preferred font families, in fallback order.</param>
    /// <param name="weight">The requested numeric font weight.</param>
    /// <param name="italic">Whether an italic face is preferred.</param>
    /// <param name="size">The font size in logical pixels.</param>
    /// <returns>The resolved face with matching shaping and rasterization coordinates.</returns>
    public FontFace Match(IReadOnlyList<string> families, int weight, bool italic, float size)
    {
        return Match(new TextStyle(families, size, weight, italic));
    }

    /// <summary>Resolves a face with matching shaping, metrics and rasterization variation coordinates.</summary>
    /// <param name="style">The requested text style. Explicit axes override automatic optical sizing.</param>
    /// <returns>The resolved face, clamping supported axes and ignoring unsupported axes.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The font size is nonpositive or nonfinite.</exception>
    public FontFace Match(TextStyle style)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(style.FontSize);
        if (!float.IsFinite(style.FontSize)) throw new ArgumentOutOfRangeException(nameof(style));
        return Variations((SkiaFontFace)Match(style.FontFamilies, style.FontWeight, style.Italic), style);
    }

    private SkiaFontFace Variations(SkiaFontFace face, TextStyle style)
    {
        const uint opticalTag = 0x6f70737a;
        var axes = face.Typeface.VariationDesignParameters;
        if (axes.Length == 0) return face;
        var original = face.Typeface.VariationDesignPosition.ToDictionary(v => v.Axis, v => v.Value);
        var coordinates = new SortedDictionary<uint, float>();
        foreach (var axis in axes)
        {
            var coordinate = original.GetValueOrDefault(axis.Tag, axis.Default);
            if (axis.Tag == opticalTag && style.OpticalSizing != FontOpticalSizing.None)
                coordinate = Math.Clamp(style.OpticalSizing == FontOpticalSizing.Auto ? style.FontSize : style.FontSize * 0.75f, axis.Min, axis.Max);
            var tag = new string([(char)(axis.Tag >> 24), (char)((axis.Tag >> 16) & 255), (char)((axis.Tag >> 8) & 255), (char)(axis.Tag & 255)]);
            if (style.FontVariations.TryGetValue(tag, out var explicitValue)) coordinate = (float)Math.Clamp(explicitValue, axis.Min, axis.Max);
            coordinates.Add(axis.Tag, coordinate);
        }
        if (coordinates.All(v => v.Value == original.GetValueOrDefault(v.Key, axes.First(a => a.Tag == v.Key).Default))) return face;
        var key = (face, string.Join(";", coordinates.Select(v => v.Key.ToString(System.Globalization.CultureInfo.InvariantCulture) + "=" + v.Value.ToString("R", System.Globalization.CultureInfo.InvariantCulture))));
        if (variationFaces.TryGetValue(key, out var cached)) return cached;
        var varied = new SkiaFontFace(face.Typeface.Clone(coordinates.Select(v => new SKFontVariationPositionCoordinate { Axis = v.Key, Value = v.Value }).ToArray()));
        variationFaces.Add(key, varied);
        owned.Add(varied);
        return varied;
    }

    /// <summary>
    /// Finds a font face containing the requested character when the preferred face cannot supply it.
    /// </summary>
    /// <param name="codepoint">The Unicode scalar value of the character.</param>
    /// <param name="preferred">The preferred font face before glyph fallback.</param>
    /// <returns>A fallback face containing the character, or null if no matching face is available.</returns>
    public FontFace? Fallback(int codepoint, FontFace preferred)
        => FindFallback(codepoint, preferred.FamilyName, preferred.Weight, preferred.Italic);

    private SkiaFontFace? FindFallback(int codepoint, string family, int weight, bool italic)
    {
        var key = (codepoint, weight, italic);
        if (fallbacks.TryGetValue(key, out var cached)) return cached;
        SkiaFontFace? result = privateFaces.Where(f => f.HasGlyph(codepoint)).OrderBy(f => Distance(f, weight, italic)).FirstOrDefault();
        if (result is null && options.UseSystemFonts)
        {
            var style = new SKFontStyle(weight, (int)SKFontStyleWidth.Normal, italic ? SKFontStyleSlant.Italic : SKFontStyleSlant.Upright);
            if (SKFontManager.Default.MatchCharacter(family, style, null, codepoint) is { } typeface)
            {
                result = new SkiaFontFace(typeface);
                owned.Add(result);
            }
        }
        fallbacks[key] = result;
        return result;
    }

    /// <summary>Resolves fallback by the requested weight/style, then applies its optical sizing and explicit axes.</summary>
    /// <param name="codepoint">The Unicode scalar value to cover.</param>
    /// <param name="preferred">The face selected before glyph fallback.</param>
    /// <param name="style">The original requested text style.</param>
    /// <returns>A covering face with supported coordinates applied, or null.</returns>
    public FontFace? Fallback(int codepoint, FontFace preferred, TextStyle style)
    {
        var selection = Match(style.FontFamilies, style.FontWeight, style.Italic);
        return FindFallback(codepoint, selection.FamilyName, style.FontWeight, style.Italic) is { } fallback ? Variations(Weighted(fallback, style.FontWeight), style) : null;
    }

    private SkiaFontFace? MatchPrivate(string family, int weight, bool italic)
    {
        var candidates = privateFaces.Where(f => string.Equals(f.FamilyName, family, StringComparison.OrdinalIgnoreCase) ||
            familyAliases[f].Contains(family, StringComparer.OrdinalIgnoreCase)).ToArray();
        return candidates.Length == 0 ? null : Weighted(Closest(candidates, weight, italic), weight);
    }

    // A private variable font is one file for every weight: the requested weight becomes a position on its wght axis.
    private SkiaFontFace Weighted(SkiaFontFace face, int weight)
    {
        const uint weightTag = 0x77676874;
        var axis = face.Typeface.VariationDesignParameters.FirstOrDefault(a => a.Tag == weightTag);
        if (axis.Tag != weightTag) return face;
        var value = Math.Clamp(weight, axis.Min, axis.Max);
        var position = face.Typeface.VariationDesignPosition;
        var current = position.FirstOrDefault(v => v.Axis == weightTag);
        if ((current.Axis == weightTag ? current.Value : axis.Default) == value) return face;
        var key = (face, (int)value);
        if (weightedFaces.TryGetValue(key, out var cached)) return cached;
        var coordinates = position.Where(v => v.Axis != weightTag)
            .Append(new SKFontVariationPositionCoordinate { Axis = weightTag, Value = value }).ToArray();
        var weighted = new SkiaFontFace(face.Typeface.Clone(coordinates));
        weightedFaces.Add(key, weighted);
        owned.Add(weighted);
        return weighted;
    }

    private SkiaFontFace? MatchSystem(string family, int weight, bool italic)
    {
        var key = (family.ToLowerInvariant(), weight, italic);
        if (systemFaces.TryGetValue(key, out var cached)) return cached;
        var style = new SKFontStyle(weight, (int)SKFontStyleWidth.Normal, italic ? SKFontStyleSlant.Italic : SKFontStyleSlant.Upright);

        var typeface = SKFontManager.Default.MatchFamily(family, style);
        if (typeface is null && !italic && OperatingSystem.IsWindows() && family.Equals("Segoe UI Variable", StringComparison.OrdinalIgnoreCase))
        {
            var file = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), "SegUIVar.ttf");
            if (File.Exists(file))
            {
                typeface = SKFontManager.Default.CreateTypeface(file, 0);
                if (typeface is not null)
                {
                    var coordinates = typeface.VariationDesignPosition.Where(v => v.Axis != 0x77676874)
                        .Append(new SKFontVariationPositionCoordinate { Axis = 0x77676874, Value = weight }).ToArray();
                    var weighted = typeface.Clone(coordinates);
                    typeface.Dispose();
                    typeface = weighted;
                }
            }
        }
        SkiaFontFace? face = null;
        if (typeface is not null)
        {
            face = new SkiaFontFace(typeface);
            owned.Add(face);
        }
        systemFaces[key] = face;
        return face;
    }

    private static SkiaFontFace Closest(IEnumerable<SkiaFontFace> faces, int weight, bool italic) =>
        faces.OrderBy(f => Distance(f, weight, italic)).First();

    private static int Distance(FontFace face, int weight, bool italic) => Math.Abs(face.Weight - weight) + (face.Italic == italic ? 0 : 1000);

    /// <summary>Releases the resources and event subscriptions owned by this instance.</summary>
    public void Dispose()
    {
        foreach (var face in owned) face.Dispose();
        owned.Clear();
        privateFaces.Clear();
        familyAliases.Clear();
        systemFaces.Clear();
        fallbacks.Clear();
        variationFaces.Clear();
        weightedFaces.Clear();
    }
}

/// <summary>Shapes with HarfBuzz: UTF-16 input with the whole text as context, clusters as string indices.</summary>
public sealed class HarfBuzzTextShaper : ITextShaper
{
    /// <summary>Shapes a UTF-16 text segment into positioned glyphs using the specified font face and size.</summary>
    /// <param name="text">The text to process.</param>
    /// <param name="start">The start offset in the UTF-16 source text.</param>
    /// <param name="length">The number of UTF-16 code units in the text segment.</param>
    /// <param name="face">The font face used to shape or measure text.</param>
    /// <param name="size">The logical font size used for measurement or shaping.</param>
    /// <returns>The positioned glyph run for the source text segment.</returns>
    public ShapedRun Shape(string text, int start, int length, FontFace face, float size)
        => ShapeCore(text,start,length,face,size,[],FontSynthesis.None);

    /// <summary>Shapes explicit OpenType features and resolves opt-in outline synthesis without mutating the shared real face.</summary>
    /// <param name="text">The complete source text.</param>
    /// <param name="start">The UTF-16 range start.</param>
    /// <param name="length">The UTF-16 range length.</param>
    /// <param name="face">The resolved real face.</param>
    /// <param name="style">Requested feature selections, synthesis and logical size.</param>
    /// <returns>The positioned glyph run with applied synthesis flags.</returns>
    public ShapedRun Shape(string text,int start,int length,FontFace face,TextStyle style)
    {
        var skiaFace=(SkiaFontFace)face;
        var axes=skiaFace.Typeface.VariationDesignParameters;
        var applied=FontSynthesis.None;
        if ((style.Synthesis & FontSynthesis.Style) != 0 && style.Italic && !face.Italic && !axes.Any(axis => axis.Tag == 0x6974616c || axis.Tag == 0x736c6e74)) applied |= FontSynthesis.Style;
        if ((style.Synthesis & FontSynthesis.Weight) != 0 && style.FontWeight >= 600 && face.Weight < 600 && !axes.Any(axis => axis.Tag == 0x77676874)) applied |= FontSynthesis.Weight;
        var features=style.FontFeatures.Select(feature => new HB.Feature(HB.Tag.Parse(feature.Key),feature.Value)).ToArray();
        return ShapeCore(text,start,length,face,style.FontSize,features,applied);
    }

    private static ShapedRun ShapeCore(string text,int start,int length,FontFace face,float size,HB.Feature[] features,FontSynthesis synthesis)
    {
        var skiaFace = (SkiaFontFace)face;
        using var buffer = new HB.Buffer();
        buffer.AddUtf16(text.AsSpan(), start, length);
        buffer.Direction = HB.Direction.LeftToRight;
        buffer.GuessSegmentProperties();
        skiaFace.Font.Shape(buffer, features);
        var infos = buffer.GetGlyphInfoSpan();
        var positions = buffer.GetGlyphPositionSpan();
        var scale = size / face.Metrics.UnitsPerEm;
        var count = infos.Length;
        var glyphs = new ushort[count];
        var advances = new float[count];
        var offsetsX = new float[count];
        var offsetsY = new float[count];
        var clusters = new int[count];
        for (var i = 0; i < count; i++)
        {
            glyphs[i] = (ushort)infos[i].Codepoint;
            clusters[i] = (int)infos[i].Cluster;
            advances[i] = positions[i].XAdvance * scale;
            offsetsX[i] = positions[i].XOffset * scale;
            offsetsY[i] = positions[i].YOffset * scale;
        }
        return new ShapedRun(face, size, start, length, glyphs, advances, offsetsX, offsetsY, clusters) { Synthesis = synthesis };
    }
}
