using System;
using System.Text.RegularExpressions;
using Nazor.Authoring;
using Nazor.LanguageServer.Features;

namespace Nazor.VisualStudio
{
    internal enum EditingLanguage { Markup, CSharp, Style, Comment, Attribute }

    internal sealed class NazorEditingContext
    {
        internal EditingLanguage Language;
        internal int Start;
        internal int End;
        internal string Prefix = "";
        internal string Suffix = "";
        internal bool Directive;
        internal string Tag;
        internal int TagStart;
        internal int Anchor;

        private static readonly Regex Block = new Regex(@"\G@(?<name>code|functions|style)\s*\{", RegexOptions.CultureInvariant);
        private static readonly Regex Control = new Regex(@"\G@(?<name>if|else|for|foreach|while|switch|try|catch|finally|lock|using)\b", RegexOptions.CultureInvariant);

        internal static NazorEditingContext At(string text, int position, bool component)
        {
            if (!component) return new NazorEditingContext { Language = EditingLanguage.Style, End = text.Length };
            for (var i = 0; i < text.Length && i <= position; i++)
            {
                var comment = Starts(text, i, "@*") ? "*@" : Starts(text, i, "<!--") ? "-->" : null;
                if (comment != null)
                {
                    var end = text.IndexOf(comment, i + 2, StringComparison.Ordinal);
                    end = end < 0 ? text.Length : end + comment.Length;
                    if (position > i && position < end) return new NazorEditingContext { Language = EditingLanguage.Comment, Start = i, End = end };
                    i = end - 1;
                    continue;
                }
                if (text[i] == '<' && i + 1 < text.Length && (char.IsLetter(text[i + 1]) || text[i + 1] == '/'))
                {
                    var j = i + (text[i + 1] == '/' ? 2 : 1);
                    var nameStart = j;
                    while (j < text.Length && (char.IsLetterOrDigit(text[j]) || "_.:-".IndexOf(text[j]) >= 0)) j++;
                    var name = text.Substring(nameStart, j - nameStart);
                    while (j < text.Length && text[j] != '>')
                    {
                        if (text[j] == '"' || text[j] == '\'')
                        {
                            var quote = text[j];
                            var value = j + 1;
                            var attributeEnd = j;
                            while (attributeEnd > i && char.IsWhiteSpace(text[attributeEnd - 1])) attributeEnd--;
                            if (attributeEnd > i && text[attributeEnd - 1] == '=') attributeEnd--;
                            while (attributeEnd > i && char.IsWhiteSpace(text[attributeEnd - 1])) attributeEnd--;
                            var attributeStart = attributeEnd;
                            while (attributeStart > i && !char.IsWhiteSpace(text[attributeStart - 1]) && text[attributeStart - 1] != '<') attributeStart--;
                            var csharp = text[attributeStart] == '@' || value < text.Length && text[value] == '@';
                            var end = csharp ? CSharpLexer.ValueEnd(text, value, quote) : text.IndexOf(quote, value);
                            if (end < 0) end = text.Length;
                            if (position >= value && position <= end)
                            {
                                var start = csharp && value < text.Length && text[value] == '@' ? value + 1 : value;
                                return new NazorEditingContext { Language = csharp ? EditingLanguage.CSharp : EditingLanguage.Attribute,
                                    Start = start, End = end, Prefix = "object __value = ", Suffix = ";", Tag = name, TagStart = i, Anchor = i };
                            }
                            j = end;
                        }
                        j++;
                    }
                    if (position >= i && position <= j)
                        return new NazorEditingContext { Language = EditingLanguage.Markup, Start = i, End = j, Tag = name, TagStart = i };
                    i = j;
                    continue;
                }
                if (text[i] != '@') continue;
                var block = Block.Match(text, i);
                if (block.Success)
                {
                    var open = i + block.Length - 1;
                    var close = Close(text, open, '{', '}');
                    if (position > open && position <= close)
                        return new NazorEditingContext { Language = block.Groups["name"].Value == "style" ? EditingLanguage.Style : EditingLanguage.CSharp,
                            Start = open + 1, End = close, Prefix = "class __NazorEditing\n{", Suffix = "}", Directive = position == open + 1, Anchor = i };
                    i = close;
                    continue;
                }
                if (Starts(text, i, "@window"))
                {
                    var open = i + 7;
                    while (open < text.Length && char.IsWhiteSpace(text[open])) open++;
                    if (open < text.Length && text[open] == '(')
                    {
                        var close = Close(text, open, '(', ')');
                        if (position > open && position <= close)
                            return new NazorEditingContext { Language = EditingLanguage.CSharp, Start = open + 1, End = close,
                                Prefix = "void __Window(", Suffix = ") {}", Anchor = i };
                    }
                }
                var control = Control.Match(text, i);
                if (control.Success)
                {
                    var open = i + control.Length;
                    while (open < text.Length && char.IsWhiteSpace(text[open])) open++;
                    if (open < text.Length && text[open] == '(') open = Math.Min(text.Length, Close(text, open, '(', ')') + 1);
                    while (open < text.Length && char.IsWhiteSpace(text[open])) open++;
                    var close = open < text.Length && text[open] == '{' ? Close(text, open, '{', '}') : open;
                    if (position > i && position <= close)
                    {
                        if (open < position && open < text.Length && text[open] == '{')
                        {
                            var nested = At(text.Substring(open + 1, Math.Max(0, close - open - 1)), position - open - 1, true);
                            if (nested.Tag != null || nested.Language == EditingLanguage.Comment || nested.Language == EditingLanguage.CSharp)
                            {
                                nested.Start += open + 1; nested.End += open + 1; nested.Anchor += open + 1;
                                if (nested.Tag != null) nested.TagStart += open + 1;
                                return nested;
                            }
                        }
                        return new NazorEditingContext { Language = EditingLanguage.CSharp, Start = i + 1, End = close < text.Length ? close + 1 : close,
                            Prefix = "", Suffix = "", Anchor = i };
                    }
                }
                var expression = i + 1;
                if (expression < text.Length && text[expression] == '(')
                {
                    var close = Close(text, expression, '(', ')');
                    if (position > expression && position <= close)
                        return new NazorEditingContext { Language = EditingLanguage.CSharp, Start = expression, End = close < text.Length ? close + 1 : close,
                            Prefix = "object __value = ", Suffix = ";", Anchor = i };
                    i = close;
                }
                else if (expression < text.Length && (char.IsLetter(text[expression]) || text[expression] == '_'))
                {
                    var end = expression;
                    while (end < text.Length)
                    {
                        if (text[end] == '(' || text[end] == '[')
                        {
                            var close = Close(text, end, text[end], text[end] == '(' ? ')' : ']');
                            end = close < text.Length ? close + 1 : close;
                        }
                        else if (char.IsLetterOrDigit(text[end]) || "_.?!".IndexOf(text[end]) >= 0) end++;
                        else break;
                    }
                    if (position > expression && position <= end)
                        return new NazorEditingContext { Language = EditingLanguage.CSharp, Start = expression, End = end,
                            Prefix = "object __value = ", Suffix = ";", Anchor = i };
                }
            }
            return new NazorEditingContext { Language = EditingLanguage.Markup, End = text.Length };
        }

        internal static int Close(string text, int open, char opening, char closing)
        {
            var masked = CodeIndentation.Mask(text.Substring(open));
            var depth = 0;
            for (var i = 0; i < masked.Length; i++)
                if (masked[i] == opening) depth++;
                else if (masked[i] == closing && --depth == 0) return open + i;
            return text.Length;
        }

        internal static bool Starts(string text, int start, string value) => start >= 0 && start + value.Length <= text.Length && string.CompareOrdinal(text, start, value, 0, value.Length) == 0;

        internal static bool IsLiteral(string text, int position)
        {
            // Probe the insertion point so whitespace inside a comment/string remains distinguishable from code.
            return CodeIndentation.Mask(text.Insert(position, "{"))[position] == ' ';
        }
    }
}
