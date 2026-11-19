namespace Nazor.LanguageServer.Features;

internal readonly record struct NssBlock(int Start, int Open, int End)
{
    public string Prelude(string text) => string.Join(' ', text[Start..Open].Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
}

internal static class NssBlocks
{
    public static List<NssBlock> At(string text, int from, int to)
    {
        var blocks = new List<NssBlock>();
        var i = from;
        while (i < to)
        {
            if (char.IsWhiteSpace(text[i])) { i++; continue; }
            if (text[i] == '/' && i + 1 < to && text[i + 1] == '*')
            {
                var close = text.IndexOf("*/", i + 2, StringComparison.Ordinal);
                i = close < 0 ? to : close + 2;
                continue;
            }
            var start = i;
            var quote = '\0';
            for (; i < to && (quote != '\0' || text[i] is not ('{' or ';' or '}')); i++)
            {
                if (quote != '\0')
                {
                    if (text[i] == '\\') i++;
                    else if (text[i] == quote) quote = '\0';
                }
                else if (text[i] is '"' or '\'') quote = text[i];
                else if (text[i] == '/' && i + 1 < to && text[i + 1] == '*')
                {
                    var close = text.IndexOf("*/", i + 2, StringComparison.Ordinal);
                    i = (close < 0 ? to : close + 2) - 1;
                }
            }
            if (i >= to) break;
            if (text[i] is ';' or '}') { i++; continue; }
            var end = NazorLanguage.CloseNss(text, i);
            if (end >= to) break;
            blocks.Add(new(start, i, end + 1));
            i = end + 1;
        }
        return blocks;
    }
}
