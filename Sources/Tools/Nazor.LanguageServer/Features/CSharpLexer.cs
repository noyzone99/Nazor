using System;

namespace Nazor.LanguageServer.Features;

internal static class CSharpLexer
{
    public static int Parenthesis(string text, int open)
    {
        var depth = 0;
        for (var k = open; k < text.Length; k++)
        {
            var c = text[k];
            if (IsLiteralStart(text, k)) k = SkipLiteral(text, k) - 1;
            else if (c == '(') depth++;
            else if (c == ')' && --depth == 0) return k;
        }
        return text.Length;
    }

    public static int ValueEnd(string text, int start, char quote)
    {
        var depth = 0;
        for (var k = start; k < text.Length; k++)
        {
            var c = text[k];
            if (c == quote && depth == 0) return k;
            if (IsLiteralStart(text, k)) k = SkipLiteral(text, k) - 1;
            else if (c is '(' or '[' or '{') depth++;
            else if (c is ')' or ']' or '}') depth = Math.Max(0, depth - 1);
        }
        return text.Length;
    }

    public static bool IsLiteralStart(string text, int k)
    {
        if (text[k] is '"' or '\'') return true;
        var p = k;
        while (p < text.Length && text[p] is '$' or '@') p++;
        return p > k && p < text.Length && text[p] == '"';
    }

    public static int SkipLiteral(string text, int k)
    {
        if (text[k] == '\'')
        {
            for (k++; k < text.Length && text[k] != '\'' && text[k] is not ('\r' or '\n'); k++)
                if (text[k] == '\\') k++;
            return Math.Min(k + 1, text.Length);
        }
        var dollars = 0;
        var verbatim = false;
        while (k < text.Length && text[k] is '$' or '@')
        {
            if (text[k] == '$') dollars++;
            else verbatim = true;
            k++;
        }
        var quotes = 0;
        while (k + quotes < text.Length && text[k + quotes] == '"') quotes++;
        if (quotes >= 3)
        {
            var close = text.IndexOf(new string('"', quotes), k + quotes, StringComparison.Ordinal);
            return close < 0 ? text.Length : close + quotes;
        }
        for (k++; k < text.Length; k++)
        {
            var c = text[k];
            if (verbatim && c == '"' && k + 1 < text.Length && text[k + 1] == '"') k++;
            else if (!verbatim && c == '\\') k++;
            else if (c == '"') return k + 1;
            else if (dollars > 0 && c == '{')
            {
                if (k + 1 < text.Length && text[k + 1] == '{') k++;
                else k = Hole(text, k + 1) - 1;
            }
            else if (!verbatim && c is '\r' or '\n') return k;
        }
        return text.Length;
    }

    private static int Hole(string text, int k)
    {
        var depth = 0;
        for (; k < text.Length; k++)
        {
            var c = text[k];
            if (IsLiteralStart(text, k)) k = SkipLiteral(text, k) - 1;
            else if (c is '(' or '[' or '{') depth++;
            else if (c is ')' or ']') depth = Math.Max(0, depth - 1);
            else if (c == '}' && depth-- == 0) return k + 1;
        }
        return text.Length;
    }
}
