using System.Globalization;

namespace Nazor.Graphics.Text;

/// <summary>
/// A line of a <see cref="TextLayout" />; coordinates are relative to the layout's top-left corner.
/// </summary>
public sealed class TextLine
{
    internal TextLine(int start, int end, int contentEnd, IReadOnlyList<ShapedRun> runs, float width, float top, float height, float ascent, float descent, float baseline)
    {
        Start = start;
        End = end;
        ContentEnd = contentEnd;
        Runs = runs;
        Width = width;
        Top = top;
        Height = height;
        Ascent = ascent;
        Descent = descent;
        Baseline = baseline;
    }

    /// <summary>UTF-16 range of the line, including trailing spaces and the line break.</summary>
    public int Start { get; }

    /// <summary>Gets the exclusive UTF-16 end offset of the line's source text.</summary>
    public int End { get; }

    /// <summary>End of the visible content: trailing spaces hang outside the line width.</summary>
    public int ContentEnd { get; }

    /// <summary>Gets the shaped glyph runs displayed on this line.</summary>
    public IReadOnlyList<ShapedRun> Runs { get; }

    /// <summary>Gets the line width in logical units.</summary>
    public float Width { get; }

    /// <summary>Gets the line's top offset within the text layout in logical units.</summary>
    public float Top { get; }

    /// <summary>Gets the line's logical height.</summary>
    public float Height { get; }

    /// <summary>Gets the logical distance from the line's top to its baseline.</summary>
    public float Ascent { get; }

    /// <summary>Gets the logical distance below the baseline.</summary>
    public float Descent { get; }

    /// <summary>Distance of the baseline from the top of the layout.</summary>
    public float Baseline { get; }
}

/// <summary>
/// Text shaped once for a style: its runs of glyphs, the advance of every character and its widths without wrapping
/// (max-content) and of its widest unbreakable word (min-content). Layouts of any width break it into lines without
/// shaping it again. Characters missing from the primary face use fallback faces, chosen per grapheme.
/// </summary>
public sealed class ShapedText
{
    private readonly float[] advances;
    private readonly (int Start, int End, TextRun Run)[] styled = [];

    /// <summary>Initializes a new instance of ShapedText.</summary>
    /// <param name="text">The text to process.</param>
    /// <param name="style">The style (TextStyle).</param>
    /// <param name="fonts">The provider used to resolve font faces and fallback glyphs.</param>
    /// <param name="shaper">The service that converts text into positioned glyphs.</param>
    public ShapedText(string text, TextStyle style, IFontProvider fonts, ITextShaper shaper)
    {
        Text = text;
        Style = style;
        FontVersion = fonts.Version;
        Primary = fonts.Match(style);
        advances = new float[text.Length + 1];
        var runs = Shape(text, fonts, shaper);
        foreach (var run in runs)
            for (var g = 0; g < run.Glyphs.Length; g++) advances[run.Clusters[g]] += run.Advances[g];
        Runs = runs;
        (MaxContentWidth, MinContentWidth) = Widths();
    }

    /// <summary>Shapes styled segments into one shared UTF-16 coordinate space without losing kerning across equal shaping styles.</summary>
    /// <param name="runs">The styled segments, concatenated in source order. The collection is copied.</param>
    /// <param name="style">The parent line strut and wrapping policy.</param>
    /// <param name="fonts">The provider used to resolve font faces and fallback glyphs.</param>
    /// <param name="shaper">The service that converts text into positioned glyphs.</param>
    public ShapedText(IReadOnlyList<TextRun> runs, TextStyle style, IFontProvider fonts, ITextShaper shaper)
        : this(Concatenate(runs), style, fonts, shaper)
    {
        var source = runs.ToArray();
        var ranges = new List<(int Start, int End, TextRun Run)>();
        var offset = 0;
        foreach (var run in source)
        {
            ArgumentNullException.ThrowIfNull(run);
            ArgumentNullException.ThrowIfNull(run.Text);
            ArgumentNullException.ThrowIfNull(run.Style);
            if (!float.IsFinite(run.BaselineShift) || !float.IsFinite(run.LeadingAdvance) || !float.IsFinite(run.TrailingAdvance) ||
                !float.IsFinite(run.TopInset) || !float.IsFinite(run.BottomInset) || run.LeadingAdvance < 0 || run.TrailingAdvance < 0 || run.TopInset < 0 || run.BottomInset < 0)
                throw new ArgumentOutOfRangeException(nameof(runs), "Inline advances must be nonnegative and all geometry must be finite.");
            ranges.Add((offset, offset + run.Text.Length, run));
            offset += run.Text.Length;
        }
        styled = ranges.ToArray();
        var result = new List<ShapedRun>();
        for (var index = 0; index < source.Length;)
        {
            var first = index;
            var start = ranges[index].Start;
            var current = source[index];
            while (index + 1 < source.Length && current.Style.Equals(source[index + 1].Style) &&
                current.BaselineShift == source[index + 1].BaselineShift && source[index].TrailingAdvance == 0 && source[index + 1].LeadingAdvance == 0) index++;
            var end = ranges[index].End;
            var part = new ShapedText(Text[start..end], current.Style, fonts, shaper);
            for (var runIndex = 0; runIndex < part.Runs.Count; runIndex++)
            {
                var run = part.Runs[runIndex];
                var widths = run.Advances.ToArray();
                var x = run.OffsetsX.ToArray();
                if (widths.Length > 0)
                {
                    if (runIndex == 0) { widths[0] += source[first].LeadingAdvance; x[0] += source[first].LeadingAdvance; }
                    if (runIndex == part.Runs.Count - 1) widths[^1] += source[index].TrailingAdvance;
                }
                result.Add(new ShapedRun(run.Face, run.Size, run.Start + start, run.Length, run.Glyphs, widths,
                    x, run.OffsetsY.Select(value => value + current.BaselineShift).ToArray(), run.Clusters.Select(value => value + start).ToArray()) { Synthesis = run.Synthesis });
            }
            index++;
        }
        Runs = result;
        advances = new float[Text.Length + 1];
        foreach (var run in Runs)
            for (var glyph = 0; glyph < run.Glyphs.Length; glyph++) advances[run.Clusters[glyph]] += run.Advances[glyph];
        (MaxContentWidth, MinContentWidth) = Widths();
    }

    private static string Concatenate(IReadOnlyList<TextRun> runs)
    {
        ArgumentNullException.ThrowIfNull(runs);
        foreach (var run in runs)
        {
            ArgumentNullException.ThrowIfNull(run);
            ArgumentNullException.ThrowIfNull(run.Text);
        }
        return string.Concat(runs.Select(run => run.Text));
    }

    private (float Maximum, float Minimum) Widths()
    {
        var maximum = float.NegativeInfinity;
        foreach (var paragraph in Paragraphs()) maximum = Math.Max(maximum, Measure(paragraph.Start, TrimEnd(paragraph.Start, paragraph.End)));
        float? minimum = null;
        foreach (var word in Words())
        {
            if (Style.BreakWords == BreakWords.Anywhere)
            {
                var start = word.Start;
                foreach (var grapheme in Graphemes(word.Start, word.End))
                {
                    var end = grapheme.Start + grapheme.Length;
                    if (end < word.End && !CanBreak(end)) continue;
                    var width = Measure(start, end);
                    minimum = minimum is { } previous ? Math.Max(previous, width) : width;
                    start = end;
                }
            }
            else
            {
                var width = Measure(word.Start, word.End);
                minimum = minimum is { } previous ? Math.Max(previous, width) : width;
            }
        }
        return (maximum, minimum ?? 0);
    }

    internal IReadOnlyList<(int Start, int End, TextRun Run)> StyledRanges => styled;

    internal bool CanBreak(int index) => !styled.Any(range => range.Run.NoWrap && index > range.Start && index < range.End) &&
        !(styled.Any(range => range.Run.NoWrap && range.End == index) && styled.Any(range => range.Run.NoWrap && range.Start == index));

    /// <summary>Gets the text represented by this object.</summary>
    public string Text { get; }

    /// <summary>Gets the text style used for shaping and font resolution.</summary>
    public TextStyle Style { get; }

    /// <summary>Gets the font-provider version used to shape this text.</summary>
    public int FontVersion { get; }

    /// <summary>Gets the primary resolved font face for the text.</summary>
    public FontFace Primary { get; }

    /// <summary>The shaped runs of the whole text, before line breaking.</summary>
    public IReadOnlyList<ShapedRun> Runs { get; }

    /// <summary>Width without wrapping: the widest paragraph.</summary>
    public float MaxContentWidth { get; }

    /// <summary>Width of the widest unbreakable word.</summary>
    public float MinContentWidth { get; }

    internal float Measure(int start, int end)
    {
        var width = 0f;
        for (var i = Math.Max(0, start); i < end && i < Text.Length; i++) width += advances[i];
        return width;
    }

    private List<ShapedRun> Shape(string text, IFontProvider fonts, ITextShaper shaper)
    {
        var runs = new List<ShapedRun>();
        var runStart = 0;
        FontFace? runFace = null;
        var fallbacks = new Dictionary<int, FontFace>();
        foreach (var (start, length) in Graphemes(0, text.Length))
        {
            var face = FaceFor(text, start, runFace ?? Primary);
            if (runFace is not null && face != runFace)
            {
                runs.Add(shaper.Shape(text, runStart, start - runStart, runFace, Style));
                runStart = start;
            }
            runFace = face;
        }
        if (runFace is not null && runStart < text.Length) runs.Add(shaper.Shape(text, runStart, text.Length - runStart, runFace, Style));

        if (Style.LetterSpacing != 0)
        {
            for (var r = 0; r < runs.Count; r++)
            {
                var run = runs[r];
                var spaced = run.Advances.ToArray();
                for (var g = 0; g < spaced.Length; g++)
                    if (g == spaced.Length - 1 || run.Clusters[g + 1] != run.Clusters[g]) spaced[g] += Style.LetterSpacing;
                runs[r] = new ShapedRun(run.Face, run.Size, run.Start, run.Length, run.Glyphs, spaced, run.OffsetsX, run.OffsetsY, run.Clusters) { Synthesis = run.Synthesis };
            }
        }
        return runs;

        FontFace FaceFor(string value, int index, FontFace current)
        {
            var codepoint = char.IsHighSurrogate(value[index]) && index + 1 < value.Length && char.IsLowSurrogate(value[index + 1])
                ? char.ConvertToUtf32(value[index], value[index + 1])
                : value[index];

            if (char.IsWhiteSpace(value[index]) || char.IsControl(value[index])) return current;
            if (Primary.HasGlyph(codepoint)) return Primary;
            if (current.HasGlyph(codepoint)) return current;
            if (!fallbacks.TryGetValue(codepoint, out var fallback))
                fallbacks[codepoint] = fallback = fonts.Fallback(codepoint, Primary, Style) ?? Primary;
            return fallback;
        }
    }

    internal int TrimEnd(int start, int end)
    {
        while (end > start && char.IsWhiteSpace(Text[end - 1])) end--;
        return end;
    }

    internal IEnumerable<(int Start, int End, int BreakEnd)> ParagraphsWithBreaks()
    {
        var start = 0;
        for (var i = 0; i < Text.Length; i++)
        {
            if (Text[i] != '\n') continue;
            var end = i > start && Text[i - 1] == '\r' ? i - 1 : i;
            yield return (start, end, i + 1);
            start = i + 1;
        }
        yield return (start, Text.Length, Text.Length);
    }

    internal IEnumerable<(int Start, int End)> Paragraphs()
    {
        foreach (var paragraph in ParagraphsWithBreaks()) yield return (paragraph.Start, paragraph.End);
    }

    internal IEnumerable<(int WordStart, int WordEnd, int SegmentEnd)> Segments(int start, int end)
    {
        var i = start;
        while (i < end)
        {
            var wordStart = i;
            while (i < end && (Text[i] != ' ' || !CanBreak(i))) i++;
            var wordEnd = i;
            while (i < end && Text[i] == ' ' && CanBreak(i)) i++;
            yield return (wordStart, wordEnd, i);
        }
    }

    internal IEnumerable<(int Start, int End)> Words()
    {
        foreach (var paragraph in ParagraphsWithBreaks())
            foreach (var segment in Segments(paragraph.Start, paragraph.End))
                if (segment.WordEnd > segment.WordStart) yield return (segment.WordStart, segment.WordEnd);
    }

    internal IEnumerable<(int Start, int Length)> Graphemes(int start, int end)
    {
        var i = start;
        while (i < end)
        {
            var length = Math.Max(1, StringInfo.GetNextTextElementLength(Text, i));
            length = Math.Min(length, end - i);
            yield return (i, length);
            i += length;
        }
    }
}

/// <summary>
/// Shaped text broken into lines; the same instance serves measuring, drawing and hit-testing, so what is measured is
/// what is drawn. Lines break at '\n' and, when a maximum width is given and wrapping is allowed, after spaces. Only
/// left-to-right text layout is supported.
/// </summary>
public sealed class TextLayout
{
    private readonly ShapedText shaped;

    /// <summary>Initializes a new instance of TextLayout.</summary>
    /// <param name="text">The text to process.</param>
    /// <param name="style">The style (TextStyle).</param>
    /// <param name="maxWidth">The optional maximum logical width available for text layout.</param>
    /// <param name="fonts">The provider used to resolve font faces and fallback glyphs.</param>
    /// <param name="shaper">The service that converts text into positioned glyphs.</param>
    /// <param name="wrap">Whether text may wrap onto additional lines.</param>
    public TextLayout(string text, TextStyle style, float? maxWidth, IFontProvider fonts, ITextShaper shaper, bool wrap = true)
        : this(new ShapedText(text, style, fonts, shaper), maxWidth, wrap)
    {
    }

    /// <summary>A layout of text already shaped: only the lines are broken for this width.</summary>
    /// <param name="shaped">The shaped (ShapedText).</param>
    /// <param name="maxWidth">The optional maximum logical width available for text layout.</param>
    /// <param name="wrap">Whether text may wrap onto additional lines.</param>
    public TextLayout(ShapedText shaped, float? maxWidth, bool wrap = true)
    {
        this.shaped = shaped;
        MaxWidth = maxWidth;
        Lines = BreakLines(shaped.Runs, wrap ? maxWidth : null);
        Width = Lines.Count == 0 ? 0 : Lines.Max(l => l.Width);
        Height = Lines.Count == 0 ? 0 : Lines[^1].Top + Lines[^1].Height;
    }

    /// <summary>The shaped text, shared by the layouts of other widths.</summary>
    public ShapedText Shaped => shaped;

    /// <summary>Gets the text represented by this object.</summary>
    public string Text => shaped.Text;

    /// <summary>Gets the text style used for shaping, line breaking, and alignment.</summary>
    public TextStyle Style => shaped.Style;

    /// <summary>Gets the optional logical width constraint used to wrap the text.</summary>
    public float? MaxWidth { get; }

    /// <summary>Gets the font-provider version used to shape this text.</summary>
    public int FontVersion => shaped.FontVersion;

    /// <summary>Gets the primary resolved font face for the text.</summary>
    public FontFace Primary => shaped.Primary;

    /// <summary>The shaped runs of the whole text, before line breaking.</summary>
    public IReadOnlyList<ShapedRun> Runs => shaped.Runs;

    /// <summary>Gets the ordered laid-out text lines.</summary>
    public IReadOnlyList<TextLine> Lines { get; }

    /// <summary>Width of the widest line, trailing spaces excluded.</summary>
    public float Width { get; }

    /// <summary>Gets the total logical height of the laid-out text.</summary>
    public float Height { get; }

    /// <summary>Width without wrapping: the widest paragraph.</summary>
    public float MaxContentWidth => shaped.MaxContentWidth;

    /// <summary>Width of the widest unbreakable word.</summary>
    public float MinContentWidth => shaped.MinContentWidth;

    /// <summary>Gets the baseline offset of the first text line.</summary>
    public float Baseline => Lines.Count == 0 ? 0 : Lines[0].Baseline;

    /// <summary>Gets the logical bounds occupied by the text layout.</summary>
    public Size Size => new(Width, Height);

    /// <summary>Conservative extra ink extent for the synthesized runs, without changing layout advances.</summary>
    public float InkOutset => Runs.Count == 0 ? 0 : Runs.Max(run => run.InkOutset);

    /// <summary>The x offset of the caret before <paramref name="index" /> on its line.</summary>
    /// <param name="index">The zero-based collection position.</param>
    /// <returns>The resulting float value.</returns>
    public float GetCaretX(int index)
    {
        var line = LineAt(index);
        return Measure(line.Start, Math.Clamp(index, line.Start, line.End));
    }

    /// <summary>The caret rectangle (zero width) at <paramref name="index" />.</summary>
    /// <param name="index">The zero-based collection position.</param>
    /// <returns>The resulting Rect value.</returns>
    public Rect GetCaretRect(int index)
    {
        var line = LineAt(index);
        var x = GetCaretX(index);
        return new Rect(x, line.Baseline - line.Ascent, 0, line.Ascent + line.Descent);
    }

    /// <summary>The caret index closest to <paramref name="point" />, at grapheme boundaries.</summary>
    /// <param name="point">The position in logical viewport coordinates.</param>
    /// <returns>The resulting int value.</returns>
    public int HitTest(Point point)
    {
        var line = Lines.FirstOrDefault(l => point.Y < l.Top + l.Height) ?? Lines[^1];
        var x = 0f;
        foreach (var (start, length) in Graphemes(line.Start, line.ContentEnd))
        {
            var width = Measure(start, start + length);
            if (point.X < x + width / 2) return start;
            x += width;
        }
        return line.ContentEnd;
    }

    /// <summary>One rectangle per line covering [start, end).</summary>
    /// <param name="start">The start offset in the UTF-16 source text.</param>
    /// <param name="end">The end offset in the UTF-16 source text.</param>
    /// <returns>The resulting IEnumerable&lt;Rect&gt; value.</returns>
    public IEnumerable<Rect> GetSelectionRects(int start, int end)
    {
        if (end < start) (start, end) = (end, start);
        foreach (var line in Lines)
        {
            var s = Math.Max(start, line.Start);
            var e = Math.Min(end, line.End);
            if (e <= s) continue;
            var x0 = Measure(line.Start, s);
            var x1 = Measure(line.Start, e);
            if (x1 > x0) yield return new Rect(x0, line.Top, x1 - x0, line.Height);
        }
    }

    /// <summary>The next caret position after <paramref name="index" /> (next grapheme boundary).</summary>
    /// <param name="index">The zero-based collection position.</param>
    /// <returns>The resulting int value.</returns>
    public int NextCaretIndex(int index) => index >= Text.Length ? Text.Length : index + StringInfo.GetNextTextElementLength(Text, index);

    /// <summary>The previous caret position before <paramref name="index" />.</summary>
    /// <param name="index">The zero-based collection position.</param>
    /// <returns>The resulting int value.</returns>
    public int PreviousCaretIndex(int index)
    {
        if (index <= 0) return 0;
        var previous = 0;
        foreach (var (start, _) in Graphemes(0, Text.Length))
        {
            if (start >= index) break;
            previous = start;
        }
        return previous;
    }

    /// <summary>
    /// The line of the caret before <paramref name="index" />: a line break or a wrap starts the next line.
    /// </summary>
    /// <param name="index">The zero-based collection position.</param>
    /// <returns>The resulting TextLine value.</returns>
    public TextLine LineAt(int index)
    {
        foreach (var line in Lines)
            if (index < line.End || line == Lines[^1]) return line;
        return Lines[^1];
    }

    private float Measure(int start, int end) => shaped.Measure(start, end);

    private int TrimEnd(int start, int end) => shaped.TrimEnd(start, end);

    private IEnumerable<(int Start, int End, int BreakEnd)> ParagraphsWithBreaks() => shaped.ParagraphsWithBreaks();

    private IEnumerable<(int WordStart, int WordEnd, int SegmentEnd)> Segments(int start, int end) => shaped.Segments(start, end);

    private IEnumerable<(int Start, int Length)> Graphemes(int start, int end) => shaped.Graphemes(start, end);

    private List<TextLine> BreakLines(IReadOnlyList<ShapedRun> runs, float? maxWidth)
    {
        var lines = new List<TextLine>();
        var top = 0f;
        foreach (var (paragraphStart, paragraphEnd, breakEnd) in ParagraphsWithBreaks())
        {
            var lineStart = paragraphStart;
            if (maxWidth is { } limit && paragraphEnd > paragraphStart)
            {

                foreach (var (wordStart, wordEnd, _) in Segments(paragraphStart, paragraphEnd))
                {
                    if (wordStart > lineStart && Measure(lineStart, wordEnd) > limit + 0.01f)
                    {
                        lines.Add(Line(runs, lineStart, wordStart, ref top));
                        lineStart = wordStart;
                    }
                    if (Style.BreakWords == BreakWords.Never || Measure(lineStart, wordEnd) <= limit + 0.01f) continue;
                    foreach (var (graphemeStart, graphemeLength) in Graphemes(lineStart, wordEnd))
                    {
                        if (graphemeStart > lineStart && shaped.CanBreak(graphemeStart) && Measure(lineStart, graphemeStart + graphemeLength) > limit + 0.01f)
                        {
                            lines.Add(Line(runs, lineStart, graphemeStart, ref top));
                            lineStart = graphemeStart;
                        }
                    }
                }
            }
            lines.Add(Line(runs, lineStart, paragraphEnd, ref top, breakEnd));
        }
        return lines;
    }

    private TextLine Line(IReadOnlyList<ShapedRun> runs, int start, int end, ref float top, int? breakEnd = null)
    {
        var contentEnd = TrimEnd(start, end);
        var count = 0;
        var complete = true;
        foreach (var run in runs)
        {
            if (run.Start < end && run.Start + run.Length > start) count++;
            if (run.Start < start || run.Start + run.Length > end) complete = false;
        }
        IReadOnlyList<ShapedRun> lineRuns = runs;
        if (!complete || count != runs.Count)
        {
            var sliced = new ShapedRun[count];
            var index = 0;
            foreach (var run in runs)
                if (run.Start < end && run.Start + run.Length > start)
                    sliced[index++] = run.Start >= start && run.Start + run.Length <= end ? run : run.Slice(Math.Max(start, run.Start), Math.Min(end, run.Start + run.Length));
            lineRuns = sliced;
        }
        var size = Style.FontSize;
        var ascent = Primary.Ascent(size);
        var descent = Primary.Descent(size);
        foreach (var run in shaped.StyledRanges.Count == 0 ? lineRuns : [])
        {
            if (run.Face == Primary) continue;
            ascent = Math.Max(ascent, run.Face.Ascent(size));
            descent = Math.Max(descent, run.Face.Descent(size));
        }
        if (shaped.StyledRanges.Count > 0)
        {
            static float Height(TextStyle style, FontFace face) => style.LineHeightKind switch
            {
                LineHeightKind.Fixed => style.LineHeight,
                LineHeightKind.Multiplier => style.LineHeight * style.FontSize,
                _ => face.Ascent(style.FontSize) + face.Descent(style.FontSize) + face.LineGap(style.FontSize),
            };
            var strutHeight = Height(Style, Primary);
            var upper = (strutHeight - ascent - descent) / 2 + ascent;
            var lower = strutHeight - upper;
            foreach (var range in shaped.StyledRanges)
            {
                if (range.Start >= end || range.End <= start) continue;
                var rangeStyle = range.Run.Style;
                foreach (var face in lineRuns.Where(run => run.Start < range.End && run.Start + run.Length > range.Start).Select(run => run.Face).Distinct())
                {
                    var rangeAscent = face.Ascent(rangeStyle.FontSize);
                    var rangeDescent = face.Descent(rangeStyle.FontSize);
                    var leading = (Height(rangeStyle, face) - rangeAscent - rangeDescent) / 2;
                    upper = Math.Max(upper, rangeAscent + leading + range.Run.BaselineShift + range.Run.TopInset);
                    lower = Math.Max(lower, rangeDescent + leading - range.Run.BaselineShift + range.Run.BottomInset);
                    ascent = Math.Max(ascent, rangeAscent + range.Run.BaselineShift);
                    descent = Math.Max(descent, rangeDescent - range.Run.BaselineShift);
                }
            }
            var richLine = new TextLine(start, breakEnd ?? end, contentEnd, lineRuns, Measure(start, contentEnd), top, upper + lower, ascent, descent, top + upper);
            top += richLine.Height;
            return richLine;
        }
        var contentHeight = ascent + descent;
        var height = Style.LineHeightKind switch
        {
            LineHeightKind.Fixed => Style.LineHeight,
            LineHeightKind.Multiplier => Style.LineHeight * size,
            _ => contentHeight + Primary.LineGap(size),
        };
        var halfLeading = (height - contentHeight) / 2;
        var line = new TextLine(start, breakEnd ?? end, contentEnd, lineRuns, Measure(start, contentEnd), top, height, ascent, descent, top + halfLeading + ascent);
        top += height;
        return line;
    }
}
