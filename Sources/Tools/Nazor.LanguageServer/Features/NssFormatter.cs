using System.Text;

namespace Nazor.LanguageServer.Features;

internal static class NssFormatter
{
    public static string FormatDocument(string text, string indent = "    ")
    {
        var newLine = text.Contains("\r\n", StringComparison.Ordinal) ? "\r\n" : "\n";
        var formatted = Format(text, indent, 0, newLine);
        return text.EndsWith('\n') || formatted.Length < newLine.Length ? formatted : formatted[..^newLine.Length];
    }

    public static string Format(string text, string indent = "    ", int baseDepth = 0, string newLine = "\r\n")
    {
        var output = new StringBuilder();
        var depth = baseDepth;
        var statement = new StringBuilder();
        var blankLines = 0;
        var started = false;
        var opened = false;

        void Line(string content, int level)
        {
            if (started && blankLines > 0 && !opened && !content.StartsWith('}')) output.Append(newLine);
            opened = content.EndsWith('{');
            for (var k = 0; k < level; k++) output.Append(indent);
            output.Append(content).Append(newLine);
            started = true;
            blankLines = 0;
        }

        string Pending() => Collapse(statement.ToString());

        var i = 0;
        while (i < text.Length)
        {
            var c = text[i];
            if (c == '/' && i + 1 < text.Length && text[i + 1] == '*')
            {
                var end = text.IndexOf("*/", i + 2, StringComparison.Ordinal);
                end = end < 0 ? text.Length : end + 2;
                var comment = text[i..end];
                if (statement.ToString().Trim().Length == 0 && TrailsDeclaration(text, i, output))
                {
                    output.Length -= newLine.Length;
                    output.Append(' ').Append(comment).Append(newLine);
                }
                else if (statement.ToString().Trim().Length == 0) Line(comment.Trim(), depth);
                else statement.Append(comment);
                i = end;
                continue;
            }
            if (c is '"' or '\'')
            {
                var end = i + 1;
                while (end < text.Length && text[end] != c)
                {
                    if (text[end] == '\\') end++;
                    end++;
                }
                end = Math.Min(end + 1, text.Length);
                statement.Append(text, i, end - i);
                i = end;
                continue;
            }
            if (c == '{')
            {
                var selector = Pending();
                Line(selector.Length == 0 ? "{" : selector + " {", depth);
                statement.Clear();
                depth++;
                i++;
                continue;
            }
            if (c == '}')
            {
                if (Pending() is { Length: > 0 } last) Line(Declaration(last) + ";", depth);
                statement.Clear();
                depth = Math.Max(baseDepth, depth - 1);
                Line("}", depth);
                i++;
                continue;
            }
            if (c == ';')
            {
                if (Pending() is { Length: > 0 } declaration) Line(Declaration(declaration) + ";", depth);
                statement.Clear();
                i++;
                continue;
            }
            if (c is '\r' or '\n')
            {
                if (c == '\r' && i + 1 < text.Length && text[i + 1] == '\n') i++;
                if (statement.ToString().Trim().Length == 0 && PreviousLineIsBlank(text, i)) blankLines = 1;
                else statement.Append(' ');
                i++;
                continue;
            }
            statement.Append(c);
            i++;
        }
        if (Pending() is { Length: > 0 } rest) Line(rest, depth);
        return output.ToString();
    }

    private static string Declaration(string text)
    {
        var colon = ColonOutsideParentheses(text);
        if (colon < 0) return text;
        var name = text[..colon].Trim();
        if (name.Length == 0 || name.Contains(' ') || !(char.IsAsciiLetterLower(name[0]) || name[0] == '-')) return text;
        return name + ": " + text[(colon + 1)..].Trim();
    }

    private static int ColonOutsideParentheses(string text)
    {
        var depth = 0;
        var quote = '\0';
        for (var k = 0; k < text.Length; k++)
        {
            var c = text[k];
            if (quote != '\0')
            {
                if (c == '\\') k++;
                else if (c == quote) quote = '\0';
            }
            else if (c is '"' or '\'') quote = c;
            else if (c == '(') depth++;
            else if (c == ')') depth--;
            else if (c == ':' && depth == 0) return k;
        }
        return -1;
    }

    private static string Collapse(string text)
    {
        var builder = new StringBuilder(text.Length);
        var quote = '\0';
        var space = false;
        for (var k = 0; k < text.Length; k++)
        {
            var c = text[k];
            if (quote != '\0')
            {
                builder.Append(c);
                if (c == '\\' && k + 1 < text.Length) builder.Append(text[++k]);
                else if (c == quote) quote = '\0';
                continue;
            }
            if (char.IsWhiteSpace(c))
            {
                space = builder.Length > 0;
                continue;
            }
            if (space && builder.Length > 0 && builder[^1] != '(' && c is not (')' or ',')) builder.Append(' ');
            space = c == ',';
            if (c is '"' or '\'') quote = c;
            builder.Append(c);
        }
        return builder.ToString();
    }

    private static bool PreviousLineIsBlank(string text, int newLine)
    {
        var k = newLine - 1;
        if (k >= 0 && text[k] == '\r') k--;
        while (k >= 0 && text[k] is ' ' or '\t') k--;
        return k >= 0 && text[k] is '\n' or '\r';
    }

    private static bool TrailsDeclaration(string text, int comment, StringBuilder output)
    {
        var k = comment - 1;
        while (k >= 0 && text[k] is ' ' or '\t') k--;
        return k >= 0 && text[k] is ';' or '{' && output.Length > 0;
    }
}
