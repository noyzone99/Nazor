using Microsoft.CodeAnalysis.Text;

namespace Nazor.Compiler;

public readonly record struct NazorSourceMapping(int OriginalStart, int GeneratedStart, int Length)
{
    public int OriginalEnd => OriginalStart + Length;

    public int GeneratedEnd => GeneratedStart + Length;
}

internal sealed class PositionMap
{
    private readonly List<IReadOnlyList<TextChange>> steps = [];

    public PositionMap Then(IEnumerable<TextChange> changes)
    {
        steps.Add([.. changes.OrderBy(c => c.Span.Start)]);
        return this;
    }

    public int? Map(int position)
    {
        int? current = position;
        foreach (var changes in steps)
        {
            if (current is not { } p) return null;
            var delta = 0;
            foreach (var change in changes)
            {
                if (change.Span.End <= p) delta += change.NewText!.Length - change.Span.Length;
                else if (change.Span.Start < p) return null;
                else break;
            }
            current = p + delta;
        }
        return current;
    }
}

internal static class SourceMappings
{
    private static readonly char[] LineBreaks = ['\r', '\n'];

    public static IReadOnlyList<NazorSourceMapping> Compute(string original, string generated, PositionMap map,
        IEnumerable<(int OriginalStart, int OriginalLength, int GeneratedStart, int GeneratedLength)> spans)
    {
        var result = new HashSet<NazorSourceMapping>();
        foreach (var span in spans)
        {
            if (span.OriginalLength != span.GeneratedLength || span.OriginalStart < 0 || span.OriginalStart + span.OriginalLength > original.Length) continue;
            if (map.Map(span.GeneratedStart) is not { } start || start + span.OriginalLength > generated.Length) continue;
            if (string.CompareOrdinal(generated, start, original, span.OriginalStart, span.OriginalLength) != 0)
            {
                var lineEnd = generated.IndexOfAny(LineBreaks, start);
                var found = span.OriginalLength == 0 ? -1 : generated.IndexOf(original.Substring(span.OriginalStart, span.OriginalLength), start,
                    (lineEnd < 0 ? generated.Length : lineEnd) - start, StringComparison.Ordinal);
                if (found < 0) continue;
                start = found;
            }
            result.Add(new(span.OriginalStart, start, span.OriginalLength));
        }
        return [.. result.OrderBy(m => m.OriginalStart).ThenBy(m => m.GeneratedStart)];
    }
}
