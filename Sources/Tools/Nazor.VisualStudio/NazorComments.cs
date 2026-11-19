#nullable disable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Nazor.VisualStudio
{
    internal enum NazorRegion
    {
        Markup,
        CSharp,
        Style,
        Comment,
    }

    internal static class NazorRegions
    {
        private static readonly Regex Block = new Regex(@"\G@(?:(?<name>code|functions|style)\s*)?\{", RegexOptions.CultureInvariant);

        public static NazorRegion At(string text, int offset)
        {
            offset = Math.Max(0, Math.Min(offset, text.Length));
            var i = 0;
            while (i < offset)
            {
                if (string.CompareOrdinal(text, i, "<!--", 0, 4) == 0)
                {
                    var end = text.IndexOf("-->", i + 4, StringComparison.Ordinal);
                    if (end < 0 || offset < end + 3) return NazorRegion.Comment;
                    i = end + 3;
                    continue;
                }
                if (string.CompareOrdinal(text, i, "@*", 0, 2) == 0)
                {
                    var end = text.IndexOf("*@", i + 2, StringComparison.Ordinal);
                    if (end < 0 || offset < end + 2) return NazorRegion.Comment;
                    i = end + 2;
                    continue;
                }
                if (text[i] == '@' && Block.Match(text, i) is Match block && block.Success)
                {
                    var open = i + block.Length - 1;
                    var style = block.Groups["name"].Value == "style";
                    var close = style ? CloseStyle(text, open) : CloseCSharp(text, open);
                    if (offset > open && offset <= close) return style ? NazorRegion.Style : NazorRegion.CSharp;
                    i = close + 1;
                    continue;
                }
                i++;
            }
            return NazorRegion.Markup;
        }

        private static int CloseCSharp(string text, int open)
        {
            var masked = Nazor.Authoring.CodeIndentation.Mask(text.Substring(open));
            var depth = 0;
            for (var i = 0; i < masked.Length; i++)
            {
                if (masked[i] == '{') depth++;
                else if (masked[i] == '}' && --depth == 0) return open + i;
            }
            return text.Length;
        }

        private static int CloseStyle(string text, int open)
        {
            var depth = 0;
            for (var i = open; i < text.Length; i++)
            {
                var c = text[i];
                if (c == '/' && i + 1 < text.Length && text[i + 1] == '*')
                {
                    var end = text.IndexOf("*/", i + 2, StringComparison.Ordinal);
                    i = end < 0 ? text.Length : end + 1;
                }
                else if (c == '"' || c == '\'')
                {
                    for (i++; i < text.Length && text[i] != c; i++)
                        if (text[i] == '\\') i++;
                }
                else if (c == '{') depth++;
                else if (c == '}' && --depth == 0) return i;
            }
            return text.Length;
        }
    }

    internal struct CommentChange
    {
        public CommentChange(int start, int length, string text)
        {
            Start = start;
            Length = length;
            Text = text;
        }

        public int Start { get; }
        public int Length { get; }
        public string Text { get; }
    }

    internal enum CommentCommand
    {
        Comment,
        Uncomment,
        ToggleLine,
        ToggleBlock,
    }

    internal static class NazorComments
    {
        public static List<CommentChange> Edit(string text, int selectionStart, int selectionEnd, bool component, CommentCommand command)
        {
            if (selectionEnd < selectionStart)
            {
                var swap = selectionStart;
                selectionStart = selectionEnd;
                selectionEnd = swap;
            }
            var lines = Lines(text, selectionStart, selectionEnd);
            var first = FirstNonBlank(text, lines);
            var region = component ? NazorRegions.At(text, first) : NazorRegion.Style;
            var lineComments = region == NazorRegion.CSharp && command != CommentCommand.ToggleBlock;
            string open, close;
            if (region == NazorRegion.CSharp || region == NazorRegion.Style) { open = "/*"; close = "*/"; }
            else { open = "@*"; close = "*@"; }

            if (lineComments)
            {
                var commented = lines.Where(l => !IsBlank(text, l)).All(l => string.CompareOrdinal(text, Indent(text, l), "//", 0, 2) == 0);
                var uncomment = command == CommentCommand.Uncomment || command == CommentCommand.ToggleLine && commented;
                if (!uncomment) return CommentLines(text, lines);
                var removed = Uncomment(text, lines);
                if (removed.Count != 0 || command != CommentCommand.Uncomment) return removed;
                var block = Enclosing(text, first, LastNonBlank(text, lines), "/*", "*/");
                return block.HasValue ? RemoveDelimiters(text, block.Value.Start, block.Value.End, "/*", "*/") : removed;
            }

            int start, end;
            if (selectionStart != selectionEnd && !CoversLines(text, lines, selectionStart, selectionEnd))
            {
                start = selectionStart;
                end = selectionEnd;
            }
            else
            {
                start = first;
                end = LastNonBlank(text, lines);
            }
            var surrounding = (selectionStart == selectionEnd ? Enclosing(text, selectionStart, selectionEnd, open, close) : null) ?? Enclosing(text, start, end, open, close);
            var removeBlock = command == CommentCommand.Uncomment || command != CommentCommand.Comment && surrounding.HasValue;
            if (removeBlock)
                return surrounding.HasValue ? RemoveDelimiters(text, surrounding.Value.Start, surrounding.Value.End, open, close) : new List<CommentChange>();
            if (command == CommentCommand.Uncomment || end <= start || region == NazorRegion.Comment || surrounding.HasValue) return new List<CommentChange>();
            return new List<CommentChange> { new CommentChange(start, 0, open + " "), new CommentChange(end, 0, " " + close) };
        }

        private static List<int> Lines(string text, int start, int end)
        {
            var lines = new List<int> { LineStart(text, start) };
            if (end > start && end > 0 && text[end - 1] == '\n') end--;
            for (var i = lines[0]; i < end; i++)
                if (text[i] == '\n' && i + 1 <= end && i + 1 < text.Length) lines.Add(i + 1);
            return lines;
        }

        private static int LineStart(string text, int offset)
        {
            while (offset > 0 && text[offset - 1] != '\n') offset--;
            return offset;
        }

        private static int LineEnd(string text, int lineStart)
        {
            var end = lineStart;
            while (end < text.Length && text[end] != '\n' && text[end] != '\r') end++;
            return end;
        }

        private static int Indent(string text, int lineStart)
        {
            var i = lineStart;
            while (i < text.Length && (text[i] == ' ' || text[i] == '\t')) i++;
            return i;
        }

        private static bool IsBlank(string text, int lineStart) => Indent(text, lineStart) == LineEnd(text, lineStart);

        private static int FirstNonBlank(string text, List<int> lines)
        {
            foreach (var line in lines)
                if (!IsBlank(text, line)) return Indent(text, line);
            return Indent(text, lines[0]);
        }

        private static int LastNonBlank(string text, List<int> lines)
        {
            for (var i = lines.Count - 1; i >= 0; i--)
            {
                if (IsBlank(text, lines[i])) continue;
                var end = LineEnd(text, lines[i]);
                while (end > lines[i] && (text[end - 1] == ' ' || text[end - 1] == '\t')) end--;
                return end;
            }
            return Indent(text, lines[0]);
        }

        private static bool CoversLines(string text, List<int> lines, int start, int end) =>
            start <= Indent(text, lines[0]) && end >= LastNonBlank(text, lines);

        private static List<CommentChange> CommentLines(string text, List<int> lines)
        {
            var filled = lines.Where(l => !IsBlank(text, l)).ToList();
            if (filled.Count == 0) return new List<CommentChange>();
            var column = filled.Min(l => Indent(text, l) - l);
            return filled.Select(l => new CommentChange(l + column, 0, "//")).ToList();
        }

        private static List<CommentChange> Uncomment(string text, List<int> lines)
        {
            var changes = new List<CommentChange>();
            foreach (var line in lines)
            {
                var at = Indent(text, line);
                if (string.CompareOrdinal(text, at, "//", 0, 2) == 0) changes.Add(new CommentChange(at, 2, ""));
            }
            return changes;
        }

        private static (int Start, int End)? Enclosing(string text, int start, int end, string open, string close)
        {
            if (end - start >= open.Length + close.Length &&
                string.CompareOrdinal(text, start, open, 0, open.Length) == 0 &&
                string.CompareOrdinal(text, end - close.Length, close, 0, close.Length) == 0)
                return (start, end);
            if (text.Length == 0) return null;
            var opened = text.LastIndexOf(open, Math.Min(start + open.Length - 1, text.Length - 1), StringComparison.Ordinal);
            if (opened < 0) return null;
            var closed = text.IndexOf(close, opened + open.Length, StringComparison.Ordinal);
            if (closed < 0 || closed + close.Length < end) return null;
            return (opened, closed + close.Length);
        }

        private static List<CommentChange> RemoveDelimiters(string text, int start, int end, string open, string close)
        {
            var innerStart = start + open.Length;
            var innerEnd = end - close.Length;
            var openLength = open.Length + (innerStart < innerEnd && text[innerStart] == ' ' ? 1 : 0);
            var closeLength = close.Length + (innerEnd - 1 >= start + openLength && text[innerEnd - 1] == ' ' ? 1 : 0);
            return new List<CommentChange> { new CommentChange(start, openLength, ""), new CommentChange(end - closeLength, closeLength, "") };
        }
    }
}
