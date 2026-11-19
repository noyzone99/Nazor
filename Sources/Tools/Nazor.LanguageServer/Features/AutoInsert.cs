namespace Nazor.LanguageServer.Features;

internal readonly record struct SnippetEdit(int Start, int End, string NewText);

internal static class AutoInsert
{
    public const string TriggerCharacters = ">=\n";

    public static SnippetEdit? OnType(string text, int offset, char character, bool component, int tabSize, bool insertSpaces)
    {
        if (offset <= 0 || offset > text.Length) return null;
        return character switch
        {
            '>' when component && text[offset - 1] == '>' => EndTag(text, offset),
            '=' when component && text[offset - 1] == '=' => Quotes(text, offset),
            '\n' or '\r' => NewLine(text, offset, component, Math.Max(tabSize, 1), insertSpaces),
            _ => null,
        };
    }

    private static SnippetEdit? EndTag(string text, int offset)
    {
        var context = NazorLanguage.Classify(text, offset - 1);
        if (context.Kind is not (NazorContextKind.TagName or NazorContextKind.AttributeName) || string.IsNullOrEmpty(context.Tag)) return null;
        if (text[context.TagStart + 1] == '/' || text[offset - 2] == '/') return null;
        var tags = NazorLanguage.Tags(text).Where(t => t.Name == context.Tag && !t.SelfClosing && t.Start != context.TagStart).ToList();
        if (tags.Count(t => t.IsEnd) > tags.Count(t => !t.IsEnd)) return null;
        return new(offset, offset, "$0</" + context.Tag + ">");
    }

    private static SnippetEdit? Quotes(string text, int offset)
    {
        if (offset >= 2 && text[offset - 2] is ' ' or '\t' or '=' or '\r' or '\n') return null;
        if (offset < text.Length && text[offset] is '"' or '\'') return null;
        var context = NazorLanguage.Classify(text, offset - 1);
        if (context.Kind != NazorContextKind.AttributeName || context.WordStart == context.WordEnd) return null;
        return new(offset, offset, "\"$0\"");
    }

    private static SnippetEdit? NewLine(string text, int offset, bool component, int tabSize, bool insertSpaces)
    {
        var lineStart = offset;
        while (lineStart > 0 && text[lineStart - 1] is not ('\r' or '\n')) lineStart--;
        if (lineStart == 0 || text.AsSpan(lineStart, offset - lineStart).ContainsAnyExcept(' ', '\t')) return null;
        var previousEnd = lineStart - 1;
        if (previousEnd > 0 && text[previousEnd] == '\n' && text[previousEnd - 1] == '\r') previousEnd--;
        var newLine = text[previousEnd..lineStart];
        var previousStart = previousEnd;
        while (previousStart > 0 && text[previousStart - 1] is not ('\r' or '\n')) previousStart--;
        var contentEnd = previousEnd;
        while (contentEnd > previousStart && text[contentEnd - 1] is ' ' or '\t') contentEnd--;
        if (contentEnd == previousStart) return null;
        if (component && NazorLanguage.Classify(text, contentEnd).Kind == NazorContextKind.Comment) return null;

        var outer = Indentation(text, previousStart, tabSize);
        var inner = outer + tabSize;
        string? closer = null;
        var masked = Nazor.Authoring.CodeIndentation.Mask(text);
        if (masked[contentEnd - 1] is '{' or '(' or '[')
            closer = masked[contentEnd - 1] switch { '{' => "}", '(' => ")", _ => "]" };
        else if (component && text[contentEnd - 1] == '>')
        {
            var tag = NazorLanguage.Classify(text, contentEnd - 1);
            if (tag.Kind is not (NazorContextKind.TagName or NazorContextKind.AttributeName) || string.IsNullOrEmpty(tag.Tag) ||
                text[tag.TagStart + 1] == '/' || text[contentEnd - 2] == '/') return null;
            closer = "</" + tag.Tag;
        }
        else if (component && NazorLanguage.Classify(text, contentEnd) is { Kind: NazorContextKind.AttributeName or NazorContextKind.TagName } open &&
                 open.TagStart >= previousStart && text[open.TagStart + 1] != '/')
        {
        }
        else if (!component || NazorLanguage.Classify(text, contentEnd).Kind is NazorContextKind.CSharp or NazorContextKind.Style)
        {
            var indent = Nazor.Authoring.CodeIndentation.Desired(text[..lineStart], lineStart, tabSize);
            if (indent is null) return null;
            var prefix = Indent(indent.Value, tabSize, insertSpaces);
            return text[lineStart..offset] == prefix ? null : new(lineStart, offset, prefix + "$0");
        }
        else return null;

        var next = offset;
        var lines = 0;
        while (next < text.Length && text[next] is ' ' or '\t' or '\r' or '\n')
        {
            if (text[next] == '\n') lines++;
            next++;
        }
        if (next == text.Length)
        {
            var prefix = Indent(inner, tabSize, insertSpaces);
            return text[lineStart..offset] == prefix ? null : new(lineStart, offset, prefix + "$0");
        }
        var closes = closer is not null && lines <= 1 && text.AsSpan(next).StartsWith(closer) &&
                     (closer.Length == 1 || next + closer.Length < text.Length && text[next + closer.Length] is '>' or ' ' or '\t' or '\r' or '\n');
        var edit = closes
            ? new SnippetEdit(contentEnd, next + 1, newLine + Indent(inner, tabSize, insertSpaces) + "$0" + newLine + Indent(outer, tabSize, insertSpaces) + text[next])
            : new SnippetEdit(contentEnd, next + 1, newLine + Indent(inner, tabSize, insertSpaces) + "$0" + text[offset..(next + 1)].TrimStart(' ', '\t'));
        var caret = edit.NewText.IndexOf("$0", StringComparison.Ordinal);
        return edit.NewText.Remove(caret, 2) == text[edit.Start..edit.End] && edit.Start + caret == offset ? null : edit;
    }

    private static int Indentation(string text, int lineStart, int tabSize)
    {
        var width = 0;
        for (var i = lineStart; i < text.Length && text[i] is ' ' or '\t'; i++)
            width = text[i] == '\t' ? (width / tabSize + 1) * tabSize : width + 1;
        return width;
    }

    private static string Indent(int width, int tabSize, bool insertSpaces) =>
        insertSpaces ? new string(' ', width) : new string('\t', width / tabSize) + new string(' ', width % tabSize);
}
