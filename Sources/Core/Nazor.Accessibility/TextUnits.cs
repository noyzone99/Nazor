using System.Globalization;

namespace Nazor.Accessibility;

/// <summary>The units a screen reader moves by in a text, with the values of UI Automation.</summary>
public enum TextUnit
{
    /// <summary>Navigates or expands by one character.</summary>
    Character = 0,

    /// <summary>Navigates or expands by a span of consistently formatted text.</summary>
    Format = 1,

    /// <summary>Navigates or expands by one word.</summary>
    Word = 2,

    /// <summary>Navigates or expands by one displayed text line.</summary>
    Line = 3,

    /// <summary>Navigates or expands by one paragraph.</summary>
    Paragraph = 4,

    /// <summary>Navigates or expands by one page of text.</summary>
    Page = 5,

    /// <summary>Navigates or expands to the entire document.</summary>
    Document = 6,
}

/// <summary>
/// The boundaries of the units of a text and the moves between them, as the Text pattern of UI Automation asks for:
/// characters are graphemes, a word takes the spaces after it (a punctuation mark is a word of its own), lines are
/// those shown (broken at the width of the box), paragraphs end with a line break; a text without formats is one
/// format, one page, one document.
/// </summary>
public static class TextUnits
{
    /// <summary>The positions where the units start, in order, with 0 and the length of the text.</summary>
    /// <param name="text">The text to process.</param>
    /// <param name="unit">The unit used for the scroll deltas.</param>
    /// <param name="lines">The lines (IReadOnlyList&lt;(int Start, int End)&gt;).</param>
    /// <returns>The resulting int[] value.</returns>
    public static int[] Boundaries(string text, TextUnit unit, IReadOnlyList<(int Start, int End)> lines)
    {
        var boundaries = new SortedSet<int> { 0, text.Length };
        switch (unit)
        {
            case TextUnit.Character:
                for (var i = 0; i < text.Length; i += StringInfo.GetNextTextElementLength(text, i)) boundaries.Add(i);
                break;
            case TextUnit.Word:
                for (var i = 1; i < text.Length; i++)
                    if (StartsWord(text, i)) boundaries.Add(i);
                break;
            case TextUnit.Line:
                foreach (var (start, _) in lines) boundaries.Add(Math.Clamp(start, 0, text.Length));
                break;
            case TextUnit.Paragraph:
                for (var i = 0; i < text.Length; i++)
                    if (text[i] == '\n') boundaries.Add(i + 1);
                break;
        }
        return [.. boundaries];
    }

    /// <summary>The unit that contains <paramref name="position" /> (at the end of the text, the last one).</summary>
    /// <param name="boundaries">The boundaries (int[]).</param>
    /// <param name="position">The position (int).</param>
    /// <returns>The resulting (int Start, int End) value.</returns>
    public static (int Start, int End) Enclosing(int[] boundaries, int position)
    {
        var length = boundaries[^1];
        if (length == 0) return (0, 0);
        var index = Math.Min(Floor(boundaries, position), boundaries.Length - 2);
        return (boundaries[index], boundaries[index + 1]);
    }

    /// <summary>
    /// Moves a position by <paramref name="count" /> boundaries (forward when positive); a position between two
    /// boundaries reaches the next one with the first step. Returns the steps made.
    /// </summary>
    /// <param name="boundaries">The boundaries (int[]).</param>
    /// <param name="position">The position (int).</param>
    /// <param name="count">The count (int).</param>
    /// <returns>The resulting int value.</returns>
    public static int Move(int[] boundaries, ref int position, int count)
    {
        var moved = 0;
        for (; count > 0 && position < boundaries[^1]; count--, moved++) position = boundaries[Floor(boundaries, position) + 1];
        for (; count < 0 && position > 0; count++, moved--)
        {
            var index = Floor(boundaries, position);
            position = boundaries[boundaries[index] == position ? index - 1 : index];
        }
        return moved;
    }

    /// <summary>
    /// Moves a range by whole units: a range with text becomes the unit <paramref name="count" /> units after (or
    /// before) the one that contains its start; an empty range moves its position. Returns the units moved.
    /// </summary>
    /// <param name="boundaries">The boundaries (int[]).</param>
    /// <param name="start">The start offset in the UTF-16 source text.</param>
    /// <param name="end">The end offset in the UTF-16 source text.</param>
    /// <param name="count">The count (int).</param>
    /// <returns>The resulting int value.</returns>
    public static int MoveRange(int[] boundaries, ref int start, ref int end, int count)
    {
        if (count == 0) return 0;
        if (start == end)
        {
            var moved = Move(boundaries, ref start, count);
            end = start;
            return moved;
        }
        var units = boundaries.Length - 1;
        var current = Math.Min(Floor(boundaries, start), units - 1);
        var target = Math.Clamp(current + count, 0, units - 1);
        (start, end) = (boundaries[target], boundaries[target + 1]);
        return target - current;
    }

    private static int Floor(int[] boundaries, int position)
    {
        var index = Array.BinarySearch(boundaries, Math.Clamp(position, 0, boundaries[^1]));
        return index >= 0 ? index : ~index - 1;
    }

    private static bool StartsWord(string text, int i)
    {
        var (current, previous) = (text[i], text[i - 1]);
        if (char.IsWhiteSpace(current) || char.IsLowSurrogate(current) || IsMark(current)) return false;
        return char.IsWhiteSpace(previous) || !IsWordCharacter(current) || !IsWordCharacter(previous);
    }

    private static bool IsWordCharacter(char c) => char.IsLetterOrDigit(c) || c == '_' || char.IsSurrogate(c) || IsMark(c);

    private static bool IsMark(char c) => CharUnicodeInfo.GetUnicodeCategory(c) is UnicodeCategory.NonSpacingMark or UnicodeCategory.SpacingCombiningMark or UnicodeCategory.EnclosingMark;
}
