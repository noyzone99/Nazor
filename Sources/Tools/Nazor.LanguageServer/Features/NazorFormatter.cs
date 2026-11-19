using System.Text;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Nazor.LanguageServer.Features;

internal static partial class NazorFormatter
{
    private static readonly Lazy<AdhocWorkspace> Workspace = new(() => new AdhocWorkspace(MefHostServices.Create(MefHostServices.DefaultAssemblies)));

    public static string Format(string text, int tabSize = 4, bool insertSpaces = true)
    {
        var indent = insertSpaces ? new string(' ', tabSize) : "\t";
        var newLine = text.Contains("\r\n", StringComparison.Ordinal) ? "\r\n" : "\n";
        text = NormalizeTags(text.Replace("\r\n", "\n"));
        var output = new StringBuilder();
        var depth = 0;
        var inTag = false;
        var lines = Lines(text);
        var lineCount = text.EndsWith('\n') ? lines.Count - 1 : lines.Count;
        for (var index = 0; index < lineCount; index++)
        {
            var line = lines[index].Trim();
            if (line.Length == 0)
            {
                output.Append(newLine);
                continue;
            }

            var block = Block().Match(line);
            if (block.Success && !inTag)
            {
                var start = Offset(lines, index) + lines[index].IndexOf('@', StringComparison.Ordinal);
                var open = text.IndexOf('{', start);
                var style = block.Groups["name"].Value == "style";
                var close = style ? NazorLanguage.CloseNss(text, open) : NazorLanguage.CloseCSharp(text, open);
                if (close < text.Length && LineAfter(text, close).Trim().Length == 0)
                {
                    var content = text[(open + 1)..close];
                    Append(output, indent, depth, $"@{block.Groups["name"].Value} {{", newLine);
                    var formatted = style
                        ? NssFormatter.Format(content, indent, depth + 1, newLine)
                        : FormatMembers(content, indent, depth + 1, tabSize, insertSpaces, newLine);
                    output.Append(formatted);
                    Append(output, indent, depth, "}", newLine);
                    index = LineIndex(lines, close);
                    continue;
                }
            }

            if (line.StartsWith("@*", StringComparison.Ordinal))
            {
                var start = Offset(lines, index) + lines[index].IndexOf("@*", StringComparison.Ordinal);
                var end = text.IndexOf("*@", start + 2, StringComparison.Ordinal);
                var last = end < 0 ? lineCount - 1 : LineIndex(lines, end);
                Append(output, indent, depth, line, newLine);
                for (var k = index + 1; k <= last; k++) output.Append(lines[k].TrimEnd()).Append(newLine);
                index = last;
                continue;
            }

            var leadingCloses = LeadingCloses(line);
            if (!inTag) depth = Math.Max(0, depth - leadingCloses);
            Append(output, indent, inTag ? depth + 1 : depth, line, newLine);
            var (opens, closes, endsInTag, tagEnded) = Count(line, inTag);
            if (inTag && tagEnded) inTag = false;
            depth = Math.Max(0, depth + opens - (closes - (inTag ? 0 : leadingCloses)));
            inTag = endsInTag;
        }
        var result = output.ToString();
        if (!text.EndsWith('\n') && result.EndsWith(newLine, StringComparison.Ordinal)) result = result[..^newLine.Length];
        return result;
    }

    internal static string NormalizeTags(string text)
    {
        var output = new StringBuilder(text.Length);
        var copied = 0;
        foreach (var tag in NazorLanguage.Tags(text))
        {
            if (tag.Start < copied || ReadTag(text, tag.Start) is not { } normalized) continue;
            output.Append(text, copied, tag.Start - copied).Append(normalized.Text);
            copied = normalized.End;
        }
        return output.Append(text, copied, text.Length - copied).ToString();
    }

    private static (string Text, int End)? ReadTag(string text, int start)
    {
        var builder = new StringBuilder();
        var j = start + 1;
        var closing = j < text.Length && text[j] == '/';
        if (closing) j++;
        builder.Append(closing ? "</" : "<");
        var nameStart = j;
        while (j < text.Length && (char.IsAsciiLetterOrDigit(text[j]) || text[j] is '_' or '.' or ':' or '-')) j++;
        if (j == nameStart) return null;
        builder.Append(text, nameStart, j - nameStart);
        while (true)
        {
            var spaceStart = j;
            while (j < text.Length && text[j] is ' ' or '\t' or '\r' or '\n') j++;
            var space = text[spaceStart..j];
            if (j >= text.Length) return null;
            if (text[j] == '>')
            {
                if (space.Contains('\n')) builder.Append(space);
                return (builder.Append('>').ToString(), j + 1);
            }
            if (text[j] == '/' && j + 1 < text.Length && text[j + 1] == '>')
            {
                if (closing) return null;
                if (space.Contains('\n')) builder.Append(space);
                return (builder.Append("/>").ToString(), j + 2);
            }
            if (closing || space.Length == 0 || !(char.IsAsciiLetter(text[j]) || text[j] is '_' or '@')) return null;
            builder.Append(space.Contains('\n') ? space : " ");
            var attributeStart = j;
            while (j < text.Length && (char.IsAsciiLetterOrDigit(text[j]) || text[j] is '_' or '-' or '@' or ':' or '.')) j++;
            var name = text[attributeStart..j];
            builder.Append(name);
            var afterName = j;
            while (j < text.Length && text[j] is ' ' or '\t') j++;
            if (j >= text.Length || text[j] != '=')
            {
                j = afterName;
                continue;
            }
            j++;
            while (j < text.Length && text[j] is ' ' or '\t') j++;
            if (j >= text.Length) return null;
            builder.Append('=');
            var quote = text[j];
            if (quote is not ('"' or '\''))
            {
                var valueStart = j;
                while (j < text.Length && text[j] is not (' ' or '\t' or '\r' or '\n' or '>') && !(text[j] == '/' && j + 1 < text.Length && text[j + 1] == '>')) j++;
                builder.Append(text, valueStart, j - valueStart);
                continue;
            }
            var value = j + 1;
            var csharp = name.StartsWith('@') || (value < text.Length && text[value] == '@' && !(value + 1 < text.Length && text[value + 1] == '@'));
            var close = csharp ? CSharpLexer.ValueEnd(text, value, quote) : text.IndexOf(quote, value);
            if (close < 0 || close >= text.Length) return null;
            builder.Append(text, j, close + 1 - j);
            j = close + 1;
        }
    }

    private static string FormatMembers(string content, string indent, int depth, int tabSize, bool insertSpaces, string newLine)
    {
        if (content.Trim().Length == 0) return "";
        const string Header = "class __NazorFormat\n{\n";
        var tree = CSharpSyntaxTree.ParseText(Header + content + "\n}", new CSharpParseOptions(LanguageVersion.Preview));
        var workspace = Workspace.Value;
        var options = workspace.Options
            .WithChangedOption(FormattingOptions.IndentationSize, LanguageNames.CSharp, tabSize)
            .WithChangedOption(FormattingOptions.TabSize, LanguageNames.CSharp, tabSize)
            .WithChangedOption(FormattingOptions.UseTabs, LanguageNames.CSharp, !insertSpaces)
            .WithChangedOption(FormattingOptions.NewLine, LanguageNames.CSharp, newLine);
        var formatted = Formatter.Format(tree.GetRoot(), workspace, options).ToFullString();
        var body = Lines(formatted);
        var first = body.FindIndex(l => l.Trim() == "{") + 1;
        var last = body.FindLastIndex(l => l.Trim() == "}");
        if (first <= 0 || last < first) return content;
        var builder = new StringBuilder();
        var members = body.GetRange(first, last - first);
        while (members.Count > 0 && members[0].Trim().Length == 0) members.RemoveAt(0);
        while (members.Count > 0 && members[^1].Trim().Length == 0) members.RemoveAt(members.Count - 1);
        var classIndent = insertSpaces ? new string(' ', tabSize) : "\t";
        foreach (var member in members)
        {
            var trimmed = member.TrimEnd();
            if (trimmed.Length == 0)
            {
                builder.Append(newLine);
                continue;
            }
            if (trimmed.StartsWith(classIndent, StringComparison.Ordinal)) trimmed = trimmed[classIndent.Length..];
            for (var k = 0; k < depth; k++) builder.Append(indent);
            builder.Append(trimmed).Append(newLine);
        }
        return builder.ToString();
    }

    private static (int Opens, int Closes, bool EndsInTag, bool TagEnded) Count(string line, bool inTag)
    {
        var opens = 0;
        var closes = 0;
        var tagEnded = false;
        var quote = '\0';
        var tagOpensElement = inTag;
        for (var i = 0; i < line.Length; i++)
        {
            var c = line[i];
            if (inTag)
            {
                if (quote != '\0')
                {
                    if (c == quote) quote = '\0';
                    continue;
                }
                if (c is '"' or '\'') quote = c;
                else if (c == '/' && i + 1 < line.Length && line[i + 1] == '>')
                {
                    inTag = false;
                    tagEnded = true;
                    tagOpensElement = false;
                    i++;
                }
                else if (c == '>')
                {
                    inTag = false;
                    tagEnded = true;
                    if (tagOpensElement) opens++;
                    tagOpensElement = false;
                }
                continue;
            }
            if (c == '<' && i + 1 < line.Length && line[i + 1] == '/')
            {
                closes++;
                var end = line.IndexOf('>', i);
                if (end < 0) break;
                i = end;
                continue;
            }
            if (c == '<' && i + 1 < line.Length && char.IsAsciiLetter(line[i + 1]))
            {
                inTag = true;
                tagOpensElement = true;
                continue;
            }
            if (c == '{') opens++;
            else if (c == '}') closes++;
        }
        return (opens, closes, inTag, tagEnded);
    }

    private static int LeadingCloses(string line)
    {
        var count = 0;
        var i = 0;
        while (i < line.Length)
        {
            if (line[i] == '}')
            {
                count++;
                i++;
            }
            else if (line.AsSpan(i).StartsWith("</") && line.IndexOf('>', i) is var end and > 0)
            {
                count++;
                i = end + 1;
            }
            else break;
            while (i < line.Length && line[i] is ' ' or '\t') i++;
        }
        return count;
    }

    private static void Append(StringBuilder output, string indent, int depth, string line, string newLine)
    {
        for (var k = 0; k < depth; k++) output.Append(indent);
        output.Append(line).Append(newLine);
    }

    private static List<string> Lines(string text) => [.. text.Replace("\r\n", "\n").Split('\n').Select(l => l.TrimEnd('\r'))];

    private static int Offset(List<string> lines, int index)
    {
        var offset = 0;
        for (var k = 0; k < index; k++) offset += lines[k].Length + 1;
        return offset;
    }

    private static int LineIndex(List<string> lines, int offset)
    {
        var position = 0;
        for (var k = 0; k < lines.Count; k++)
        {
            position += lines[k].Length + 1;
            if (offset < position) return k;
        }
        return lines.Count - 1;
    }

    private static string LineAfter(string text, int position)
    {
        var end = text.IndexOfAny(['\r', '\n'], position + 1);
        return text[(position + 1)..(end < 0 ? text.Length : end)];
    }

    [GeneratedRegex(@"^@(?<name>code|functions|style)\s*\{\s*$")]
    private static partial Regex Block();
}
