using System.Text;
using Nazor.Graphics;
using Nazor.Graphics.Text;
using Nazor.Layout;
using Nazor.Style;
using GraphicsSize = Nazor.Graphics.Size;

namespace Nazor.Presentation;

internal sealed class TextContent(IFontProvider fonts, ITextShaper shaper, ShapingCache? shapes = null)
{
    private const float WidthTolerance = 0.01f;
    private static readonly StyleProperty[] textProperties = [StyleProperties.FontFamily, StyleProperties.FontSize, StyleProperties.FontWeight,
        StyleProperties.FontStyle, StyleProperties.FontOpticalSizing, StyleProperties.FontVariationSettings, StyleProperties.FontFeatureSettings, StyleProperties.FontSynthesis,
        StyleProperties.LineHeight, StyleProperties.LetterSpacing, StyleProperties.OverflowWrap,
        StyleProperties.WhiteSpace, StyleProperties.TextTransform, StyleProperties.FontSymbol];
    private readonly List<(float? MaxWidth, TextLayout Layout)> cache = [];
    private (float Width, TextLayout Layout)? ellipsized;
    private int fontVersion = -1;

    private (string Raw, ComputedStyle? Computed, bool Editable, IReadOnlyList<FontSymbol>? Symbols, bool SymbolText) given;

    private ShapedText? shaped;
    private DrawText? drawing;
    private TextRun[]? inlineRuns;

    public DrawText GetDrawing(TextLayout layout, Point origin, Nazor.Graphics.Color color, TextAlignment alignment = TextAlignment.Start, float width = 0, TextDecoration? decoration = null)
    {
        if (drawing is { } current && ReferenceEquals(current.Layout, layout) && current.Origin == origin && current.Color == color &&
            current.Alignment == alignment && current.Width.Equals(width) && current.Decoration == decoration) return current;
        return drawing = new(layout, origin, color, alignment, width, decoration);
    }

    public string Text { get; private set; } = "";
    public TextStyle Style { get; private set; } = new(["Segoe UI"], 14);
    public bool Wrap { get; private set; } = true;

    public bool Update(string rawText, ComputedStyle computed, bool editable = false, IReadOnlyList<FontSymbol>? symbols = null, bool symbolText = true)
    {
        if (inlineRuns is not null) { inlineRuns = null; given = default; }
        if (editable == given.Editable && symbolText == given.SymbolText && ReferenceEquals(symbols, given.Symbols) && fontVersion == fonts.Version &&
            (ReferenceEquals(rawText, given.Raw) || rawText == given.Raw) && given.Computed is { } previous && SameTextStyle(previous, computed))
        {
            given = (rawText, computed, editable, symbols, symbolText);
            return false;
        }
        given = (rawText, computed, editable, symbols, symbolText);
        var whiteSpace = computed.Get(StyleProperties.WhiteSpace);
        var text = whiteSpace switch
        {
            WhiteSpace.Pre or WhiteSpace.PreWrap => rawText,
            WhiteSpace.PreLine => string.Join('\n', rawText.Replace("\r\n", "\n").Split('\n').Select(Collapse)),
            _ => Collapse(rawText),
        };
        if (!editable) text = Transform(text, computed.Get(StyleProperties.TextTransform));
        var style = ToTextStyle(computed);
        var candidates = symbols ?? computed.Get(StyleProperties.FontSymbol).Items;
        if (symbolText && !editable && candidates.Count > 0)
        {
            if (FontSymbolResolution.Resolve(candidates, style, fonts) is { } selected)
            {
                text = selected.Glyph;
                style = style with { FontFamilies = new[] { selected.FontFamily } };
            }
            else text = "";
        }
        var wrap = whiteSpace is WhiteSpace.Normal or WhiteSpace.PreWrap or WhiteSpace.PreLine;
        if (text == Text && style.Equals(Style) && wrap == Wrap && fontVersion == fonts.Version) return false;
        Text = text;
        Style = style;
        Wrap = wrap;
        fontVersion = fonts.Version;
        cache.Clear();
        ellipsized = null;
        shaped = null;
        drawing = null;
        return true;
    }

    public bool UpdateRuns(IReadOnlyList<TextRun> runs, ComputedStyle computed)
    {
        var style = ToTextStyle(computed);
        var wrap = computed.Get(StyleProperties.WhiteSpace) is WhiteSpace.Normal or WhiteSpace.PreWrap or WhiteSpace.PreLine;
        if (inlineRuns is not null && inlineRuns.SequenceEqual(runs) && style.Equals(Style) && wrap == Wrap && fontVersion == fonts.Version) return false;
        inlineRuns = runs.ToArray();
        Style = style;
        Wrap = wrap;
        Text = string.Concat(inlineRuns.Select(run => run.Text));
        fontVersion = fonts.Version;
        shaped = new ShapedText(inlineRuns, Style, fonts, shaper);
        cache.Clear();
        ellipsized = null;
        drawing = null;
        given = default;
        return true;
    }

    private static bool SameTextStyle(ComputedStyle left, ComputedStyle right)
    {
        if (ReferenceEquals(left, right)) return true;
        foreach (var property in textProperties)
            if (!Equals(left.Get(property), right.Get(property))) return false;
        return true;
    }

    public TextLayout GetEllipsized(float width)
    {
        var full = Get(width);
        if (full.Lines.All(l => l.Width <= width + WidthTolerance)) return full;
        if (ellipsized is { } cached && cached.Width == width) return cached.Layout;
        var ellipsis = new TextLayout("…", Style, null, fonts, shaper, wrap: false).Width;
        var text = new StringBuilder();
        foreach (var line in full.Lines)
        {
            if (text.Length > 0) text.Append('\n');
            var content = Text[line.Start..line.ContentEnd];
            if (line.Width <= width + WidthTolerance)
            {
                text.Append(content);
                continue;
            }

            var single = new TextLayout(content, Style, null, fonts, shaper, wrap: false);
            var keep = 0;
            for (var index = single.NextCaretIndex(0); index <= content.Length && single.GetCaretX(index) + ellipsis <= width + WidthTolerance; index = single.NextCaretIndex(index))
            {
                keep = index;
                if (index == content.Length) break;
            }
            text.Append(content.AsSpan(0, keep).TrimEnd()).Append('…');
        }
        var layout = new TextLayout(text.ToString(), Style, null, fonts, shaper, wrap: false);
        ellipsized = (width, layout);
        return layout;
    }

    public GraphicsSize Measure(float? knownWidth, float? knownHeight, AvailableSpace availableWidth, AvailableSpace availableHeight)
    {
        var unwrapped = Get(null);
        float width;
        TextLayout layout;
        if (knownWidth is { } known)
        {
            width = known;
            layout = Get(known);
        }
        else if (availableWidth.Kind == AvailableKind.MinContent)
        {
            width = unwrapped.MinContentWidth;
            layout = Get(width);
        }
        else if (availableWidth.Kind == AvailableKind.MaxContent || unwrapped.MaxContentWidth <= availableWidth.Value)
        {
            width = unwrapped.MaxContentWidth;
            layout = unwrapped;
        }
        else
        {
            layout = Get(availableWidth.Value);
            width = Math.Min(availableWidth.Value, layout.Width);
        }
        return new GraphicsSize(width, knownHeight ?? layout.Height);
    }

    public TextLayout Get(float? maxWidth)
    {
        if (!Wrap) maxWidth = null;

        else if (maxWidth is { } limit && limit + WidthTolerance >= Shaped.MaxContentWidth) maxWidth = null;
        foreach (var (width, layout) in cache)
            if (width == maxWidth) return layout;
        var created = new TextLayout(Shaped, maxWidth, Wrap);
        if (cache.Count >= 6) cache.RemoveAt(0);
        cache.Add((maxWidth, created));
        return created;
    }

    private ShapedText Shaped
    {
        get
        {
            if (shaped is { } current && current.FontVersion == fonts.Version) return current;
            return shaped = inlineRuns is not null ? new ShapedText(inlineRuns, Style, fonts, shaper)
                : shapes?.Get(Text, Style) ?? new ShapedText(Text, Style, fonts, shaper);
        }
    }

    private const float MinimumFontSize = 0.01f;

    public static TextStyle ToTextStyle(ComputedStyle style)
    {
        var lineHeight = style.Get(StyleProperties.LineHeight);
        // font-size: 0 is valid NSS (a theme can hide a part's text this way), but font backends need a positive size,
        // so the text is shaped at a negligible size that takes no visible space.
        var size = Math.Max((float)style.Get(StyleProperties.FontSize).Value, MinimumFontSize);
        return new TextStyle(style.Get(StyleProperties.FontFamily).Items, size, style.Get(StyleProperties.FontWeight),
            style.Get(StyleProperties.FontStyle) == FontStyle.Italic,
            lineHeight.Kind switch
            {
                Nazor.Style.LineHeightKind.Number => Graphics.Text.LineHeightKind.Multiplier,
                Nazor.Style.LineHeightKind.Length => Graphics.Text.LineHeightKind.Fixed,
                _ => Graphics.Text.LineHeightKind.Normal,
            },
            lineHeight.Kind switch
            {
                Nazor.Style.LineHeightKind.Number => (float)lineHeight.Number,
                Nazor.Style.LineHeightKind.Length => (float)lineHeight.Length.Value,
                _ => 0,
            },
            (float)style.Get(StyleProperties.LetterSpacing).Value,
            style.Get(StyleProperties.OverflowWrap) switch
            {
                OverflowWrap.BreakWord => BreakWords.WhenOverflowing,
                OverflowWrap.Anywhere => BreakWords.Anywhere,
                _ => BreakWords.Never,
            })
        {
            OpticalSizing = style.Get(StyleProperties.FontOpticalSizing) switch
            {
                Nazor.Style.FontOpticalSizing.Auto => Graphics.Text.FontOpticalSizing.Auto,
                Nazor.Style.FontOpticalSizing.None => Graphics.Text.FontOpticalSizing.None,
                _ => Graphics.Text.FontOpticalSizing.Native,
            },
            Synthesis = (Graphics.Text.FontSynthesis)(byte)style.Get(StyleProperties.FontSynthesis),
            FontFeatures = style.Get(StyleProperties.FontFeatureSettings).Items.GroupBy(feature => feature.Tag,StringComparer.Ordinal).ToDictionary(group => group.Key,group => group.Last().Value,StringComparer.Ordinal),
            FontVariations = style.Get(StyleProperties.FontVariationSettings).Items.GroupBy(axis => axis.Tag, StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.Last().Value, StringComparer.Ordinal),
        };
    }

    internal static string Transform(string text, TextTransform transform) => transform switch
    {
        TextTransform.Uppercase => text.ToUpperInvariant(),
        TextTransform.Lowercase => text.ToLowerInvariant(),
        TextTransform.Capitalize => Capitalize(text),
        _ => text,
    };

    private static string Capitalize(string text)
    {
        var builder = new StringBuilder(text);
        var start = true;
        for (var i = 0; i < builder.Length; i++)
        {
            var c = builder[i];
            if (char.IsLetter(c))
            {
                if (start) builder[i] = char.ToUpperInvariant(c);
                start = false;
            }
            else if (char.IsWhiteSpace(c) || char.IsPunctuation(c) && c is not ('\'' or '’')) start = true;
        }
        return builder.ToString();
    }

    private static string Collapse(string text)
    {
        var canonical = true;
        for (var i = 0; i < text.Length; i++)
        {
            var c = text[i];
            if (char.IsWhiteSpace(c) && (c != ' ' || i == 0 || i == text.Length - 1 || text[i - 1] == ' '))
            {
                canonical = false;
                break;
            }
        }
        if (canonical) return text;
        var builder = new StringBuilder(text.Length);
        var space = false;
        foreach (var c in text)
        {
            if (char.IsWhiteSpace(c))
            {
                space = builder.Length > 0;
                continue;
            }
            if (space) builder.Append(' ');
            space = false;
            builder.Append(c);
        }
        return builder.ToString();
    }
}
