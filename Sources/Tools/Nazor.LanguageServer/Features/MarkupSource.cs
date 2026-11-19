using Nazor.Compiler;

namespace Nazor.LanguageServer.Features;

internal static class MarkupSource
{
    public static MarkupTag[] Tags(string text, string path)
    {
        var spans = NazorCompiler.MarkupTagSpans(text, path);
        return NazorLanguage.Tags(text).Where(t => spans.ContainsKey(t.Start)).Select(t => t with
        { End = spans[t.Start], SelfClosing = spans[t.Start] >= 2 && text.AsSpan(spans[t.Start] - 2, 2).SequenceEqual("/>") }).ToArray();
    }

    public static IEnumerable<KeyValuePair<string, string?>> Attributes(string source, bool assignedOnly = false)
    {
        var i = 0;
        while (i < source.Length)
        {
            while (i < source.Length && (char.IsWhiteSpace(source[i]) || source[i] == '/')) i++;
            var start = i;
            while (i < source.Length && (char.IsLetterOrDigit(source[i]) || source[i] is '_' or '@' or ':' or '-' or '.')) i++;
            if (start == i) yield break;
            var name = source[start..i];
            while (i < source.Length && char.IsWhiteSpace(source[i])) i++;
            var value = "true";
            var assigned = i < source.Length && source[i] == '=';
            if (assigned)
            {
                i++;
                while (i < source.Length && char.IsWhiteSpace(source[i])) i++;
                if (i < source.Length && source[i] is '\'' or '"')
                {
                    var quote = source[i++];
                    var first = source.IndexOf(quote, i);
                    var end = first < 0 ? source.Length : source.AsSpan(i, first - i).Contains('@') ? CSharpLexer.ValueEnd(source, i, quote) : first;
                    value = source[i..end];
                    i = Math.Min(end + 1, source.Length);
                }
                else
                {
                    start = i;
                    while (i < source.Length && !char.IsWhiteSpace(source[i]) && source[i] is not ('>' or '/')) i++;
                    value = source[start..i];
                }
            }
            if (!assignedOnly || assigned) yield return new(name, value.Contains('@') ? null : System.Net.WebUtility.HtmlDecode(value));
        }
    }
}
