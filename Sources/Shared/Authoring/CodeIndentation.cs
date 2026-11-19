using System;
using System.Collections.Generic;

namespace Nazor.Authoring
{
    internal static class CodeIndentation
    {
        internal static int? Desired(string text, int lineStart, int tabSize)
        {
            if (lineStart <= 0 || lineStart > text.Length) return null;
            var masked = Mask(text);
            var previousEnd = lineStart;
            while (previousEnd > 0 && (text[previousEnd - 1] == '\r' || text[previousEnd - 1] == '\n')) previousEnd--;
            var previousStart = previousEnd;
            while (previousStart > 0 && text[previousStart - 1] != '\r' && text[previousStart - 1] != '\n') previousStart--;
            if (previousEnd == previousStart) return null;
            var end = previousEnd;
            while (end > previousStart && char.IsWhiteSpace(masked[end - 1])) end--;
            var width = Width(text, previousStart, tabSize);
            if (end > previousStart && (masked[end - 1] == '{' || masked[end - 1] == '(' || masked[end - 1] == '[')) width += tabSize;
            var current = lineStart;
            while (current < text.Length && (text[current] == ' ' || text[current] == '\t')) current++;
            if (current < text.Length && (masked[current] == '}' || masked[current] == ')' || masked[current] == ']'))
            {
                var stack = new Stack<int>();
                for (var i = 0; i < lineStart; i++)
                    if (masked[i] == '{' || masked[i] == '(' || masked[i] == '[') stack.Push(i);
                    else if ((masked[i] == '}' || masked[i] == ')' || masked[i] == ']') && stack.Count > 0) stack.Pop();
                if (stack.Count > 0 && Matches(masked[stack.Peek()], masked[current]))
                {
                    var start = stack.Pop();
                    while (start > 0 && text[start - 1] != '\r' && text[start - 1] != '\n') start--;
                    width = Width(text, start, tabSize);
                }
            }
            return width;
        }

        internal static int Width(string text, int start, int tabSize)
        {
            var width = 0;
            for (var i = start; i < text.Length && (text[i] == ' ' || text[i] == '\t'); i++)
                width = text[i] == '\t' ? (width / tabSize + 1) * tabSize : width + 1;
            return width;
        }

        internal static string Indent(int width, int tabSize, bool spaces) => spaces
            ? new string(' ', width) : new string('\t', width / tabSize) + new string(' ', width % tabSize);

        internal static string Mask(string text)
        {
            var result = text.ToCharArray();
            for (var i = 0; i < text.Length; i++)
            {
                var start = i;
                var end = i;
                if (text[i] == '/' && i + 1 < text.Length && text[i + 1] == '/')
                {
                    end = i + 2;
                    while (end < text.Length && text[end] != '\r' && text[end] != '\n') end++;
                }
                else if (text[i] == '/' && i + 1 < text.Length && text[i + 1] == '*')
                {
                    end = text.IndexOf("*/", i + 2, StringComparison.Ordinal);
                    end = end < 0 ? text.Length : end + 2;
                }
                else if (Nazor.LanguageServer.Features.CSharpLexer.IsLiteralStart(text, i))
                {
                    end = Nazor.LanguageServer.Features.CSharpLexer.SkipLiteral(text, i);
                }
                if (end == start) continue;
                for (var j = start; j < end; j++) if (result[j] != '\r' && result[j] != '\n') result[j] = ' ';
                i = end - 1;
            }
            return new string(result);
        }

        private static bool Matches(char open, char close) => open == '{' && close == '}' || open == '(' && close == ')' || open == '[' && close == ']';
    }
}
