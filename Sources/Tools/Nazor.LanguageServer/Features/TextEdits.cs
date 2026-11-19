using Nazor.LanguageServer.Protocol;

namespace Nazor.LanguageServer.Features;

internal static class TextEdits
{
    public static TextEdit[] Formatting(string before, string after, int first = 0, int last = int.MaxValue)
    {
        var edits = new List<TextEdit>();
        var a = 0;
        var b = 0;
        while (a < before.Length || b < after.Length)
        {
            if (a < before.Length && b < after.Length && before[a] == after[b]) { a++; b++; continue; }
            var startA = a;
            var startB = b;
            while (a < before.Length && char.IsWhiteSpace(before[a])) a++;
            while (b < after.Length && char.IsWhiteSpace(after[b])) b++;
            if ((a == startA && b == startB) ||
                (a < before.Length || b < after.Length) && (a == before.Length || b == after.Length || before[a] != after[b]))
                return [.. Lines(before, after, first, last).Select(edit =>
                {
                    var from = TextPositions.Offset(before, edit.Range.Start);
                    var to = TextPositions.Offset(before, edit.Range.End);
                    var replacement = edit.NewText;
                    var head = 0;
                    while (from + head < to && head < replacement.Length && before[from + head] == replacement[head]) head++;
                    var tail = 0;
                    while (to - tail > from + head && replacement.Length - tail > head && before[to - tail - 1] == replacement[^(tail + 1)]) tail++;
                    return new TextEdit(TextPositions.Range(before, from + head, to - tail), replacement[head..(replacement.Length - tail)]);
                })];
            var position = TextPositions.Position(before, startA);
            if (position.Line <= last && TextPositions.Position(before, a).Line >= first)
                edits.Add(new(TextPositions.Range(before, startA, a), after[startB..b]));
        }
        return [.. edits];
    }

    public static TextEdit? Minimal(string before, string after)
    {
        if (before == after) return null;
        var start = 0;
        while (start < before.Length && start < after.Length && before[start] == after[start]) start++;
        while (start > 0 && before[start - 1] is not '\n') start--;
        var endBefore = before.Length;
        var endAfter = after.Length;
        while (endBefore > start && endAfter > start && before[endBefore - 1] == after[endAfter - 1])
        {
            endBefore--;
            endAfter--;
        }
        return new TextEdit(TextPositions.Range(before, start, endBefore), after[start..endAfter]);
    }

    public static TextEdit[] Lines(string before, string after, int first, int last)
    {
        var a = SplitLines(before);
        var b = SplitLines(after);
        var head = 0;
        while (head < a.Count && head < b.Count && a[head].Text == b[head].Text) head++;
        var tail = 0;
        while (tail < a.Count - head && tail < b.Count - head && a[^(tail + 1)].Text == b[^(tail + 1)].Text) tail++;
        var (endA, endB) = (a.Count - tail, b.Count - tail);
        var lengths = new int[endA - head + 1, endB - head + 1];
        for (var i = endA - 1; i >= head; i--)
            for (var j = endB - 1; j >= head; j--)
                lengths[i - head, j - head] = a[i].Text == b[j].Text
                    ? lengths[i - head + 1, j - head + 1] + 1
                    : Math.Max(lengths[i - head + 1, j - head], lengths[i - head, j - head + 1]);

        var edits = new List<TextEdit>();
        var (x, y) = (head, head);
        while (x < endA || y < endB)
        {
            if (x < endA && y < endB && a[x].Text == b[y].Text)
            {
                x++;
                y++;
                continue;
            }
            var (fromX, fromY) = (x, y);
            while (x < endA || y < endB)
            {
                if (x < endA && y < endB && a[x].Text == b[y].Text) break;
                if (y >= endB || x < endA && lengths[x - head + 1, y - head] >= lengths[x - head, y - head + 1]) x++;
                else y++;
            }
            if (x - fromX == y - fromY)
            {
                for (var k = 0; k < x - fromX; k++)
                {
                    var line = fromX + k;
                    if (line < first || line > last) continue;
                    edits.Add(new TextEdit(TextPositions.Range(before, a[line].Start, a[line].Start + a[line].Text.Length), b[fromY + k].Text));
                }
                continue;
            }
            if (fromX <= last && (x > first || fromX >= first && fromX == x))
            {
                var start = fromX < a.Count ? a[fromX].Start : before.Length;
                var end = x < a.Count ? a[x].Start : before.Length;
                var text = y > fromY ? after[(fromY < b.Count ? b[fromY].Start : after.Length)..(y < b.Count ? b[y].Start : after.Length)] : "";
                edits.Add(new TextEdit(TextPositions.Range(before, start, end), text));
            }
        }
        return [.. edits];
    }

    private static List<(int Start, string Text)> SplitLines(string text)
    {
        var lines = new List<(int, string)>();
        var start = 0;
        for (var i = 0; i < text.Length; i++)
        {
            if (text[i] != '\n') continue;
            lines.Add((start, text[start..i].TrimEnd('\r')));
            start = i + 1;
        }
        if (start < text.Length) lines.Add((start, text[start..]));
        return lines;
    }
}
