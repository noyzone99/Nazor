using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;

namespace Nazor.Compiler;

public sealed record StyleSource(string Path, int Offset, string Text, int Line = 1, int Column = 1);
public sealed record ExtractedSource(string Razor, IReadOnlyList<StyleSource> Styles);

public static class StyleExtractor
{
    public static ExtractedSource Extract(string source, string path) => Extract(source, path, null);

    public static ExtractedSource Extract(string source, string path, ICollection<NazorDiagnostic>? errors)
    {
        var masked = source.ToCharArray();
        var styles = new List<StyleSource>();
        var text = SourceText.From(source);
        var position = 0;
        while ((position = source.IndexOf("@style", position, StringComparison.Ordinal)) >= 0)
        {
            var start = position;
            position += "@style".Length;
            if (position < source.Length && SyntaxFacts.IsIdentifierPartCharacter(source[position])) continue;
            var lineStart = text.Lines.GetLineFromPosition(start).Start;
            if (source.AsSpan(lineStart, start - lineStart).ContainsAnyExcept(' ', '\t')) continue;
            var context = RazorSdkAdapter.ClassifyStyle(new string(masked, 0, position), source[position..], path, start);
            if (context == StyleDirectiveContext.None) continue;
            if (context == StyleDirectiveContext.Nested)
            {
                Report(errors, Error(source, path, start, "NZS003", "@style must be at component top level, outside markup and control-flow blocks."));
                continue;
            }
            var open = position;
            while (open < source.Length && char.IsWhiteSpace(source[open])) open++;
            if (open == source.Length || source[open] != '{')
            {
                Report(errors, Error(source, path, start, "NZS001", "Expected '{' after @style."));
                Mask(masked, start, position - 1);
                continue;
            }
            int end;
            if (FindClose(source, open) is { } close) end = close;
            else
            {
                Report(errors, Error(source, path, open, "NZS002", "Unterminated @style block, string or comment."));
                end = RecoveryEnd(source, open) - 1;
            }
            var begin = text.Lines.GetLinePosition(open + 1);
            styles.Add(new(path, open + 1, source[(open + 1)..Math.Max(end, open + 1)], begin.Line + 1, begin.Character + 1));
            Mask(masked, start, end);
            position = end + 1;
        }
        return new(new string(masked), styles);
    }

    private static void Report(ICollection<NazorDiagnostic>? errors, NazorCompilationException error)
    {
        if (errors is null) throw error;
        foreach (var diagnostic in error.Diagnostics) errors.Add(diagnostic);
    }

    private static void Mask(char[] masked, int start, int end)
    {
        for (var i = start; i <= end; i++)
            if (masked[i] is not ('\r' or '\n' or '\u0085' or '\u2028' or '\u2029')) masked[i] = ' ';
    }

    private static int RecoveryEnd(string source, int open)
    {
        for (var i = source.IndexOf('\n', open); i >= 0 && i + 1 < source.Length; i = source.IndexOf('\n', i + 1))
            if (source[i + 1] == '<' || source[i + 1] == '@' && i + 2 < source.Length && char.IsLetter(source[i + 2])) return i + 1;
        return source.Length;
    }

    private static int? FindClose(string source, int open)
    {
        var depth = 1;
        var quote = '\0';
        var comment = false;
        for (var i = open + 1; i < source.Length; i++)
        {
            var c = source[i];
            if (comment)
            {
                if (c == '*' && i + 1 < source.Length && source[i + 1] == '/') { comment = false; i++; }
                continue;
            }
            if (quote != '\0')
            {
                if (c == '\\') i++;
                else if (c == quote) quote = '\0';
                continue;
            }
            if (c == '/' && i + 1 < source.Length && source[i + 1] == '*') { comment = true; i++; }
            else if (c is '\'' or '"') quote = c;
            else if (c == '\\') i++;
            else if (c == '{') depth++;
            else if (c == '}' && --depth == 0) return i;
        }
        return null;
    }

    private static NazorCompilationException Error(string source, string path, int offset, string code, string message)
    {
        var position = SourceText.From(source).Lines.GetLinePosition(offset);
        return new([new NazorDiagnostic(code, NazorDiagnosticSeverity.Error, path, position.Line + 1, position.Character + 1, message)]);
    }
}
