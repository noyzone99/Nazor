using System.Text;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Nazor.Compiler;
using Nazor.LanguageServer.Protocol;

namespace Nazor.LanguageServer.Features;

internal enum NazorContextKind
{
    Content,
    Directive,
    TagName,
    AttributeName,
    AttributeValue,
    Style,
    CSharp,
    Comment,
}

internal sealed record MarkupTag(int Start, int End, string Name, int NameStart, bool IsEnd, bool SelfClosing);

internal sealed record NazorContext(NazorContextKind Kind, int WordStart, int WordEnd)
{
    public string? Tag { get; init; }
    public int TagStart { get; init; }
    public IReadOnlyList<string> Attributes { get; init; } = [];
    public string? Attribute { get; init; }
    public int Start { get; init; }
    public int End { get; init; }
}

internal static partial class NazorLanguage
{
    private static readonly (string Name, string Description)[] Directives =
    [
        ("code", "@code { … }: members of the component class."),
        ("inject", "@inject Type Name: a service of the application, injected into a property."),
        ("using", "@using Namespace: a namespace for the markup and the code of the component."),
        ("namespace", "@namespace Name: the namespace of the component."),
        ("inherits", "@inherits Type: the base class of the component."),
        ("implements", "@implements Interface: an interface the component implements."),
        ("typeparam", "@typeparam T: a type parameter of the component."),
        ("attribute", "@attribute [Attribute]: an attribute of the component class."),
        ("page", "@page \"/ordini/{Id:int}\": an address of the page, shown by a Frame (route values become parameters)."),
        ("style", "@style { … }: NSS scoped to the component."),
        ("window", "@window (Type Name, Type Optional = defaultValue): a native window definition. Configure its title and frame through Window."),
        ("if", "@if (condition) { … }"),
        ("foreach", "@foreach (var item in items) { … }"),
        ("for", "@for (var i = 0; i < n; i++) { … }"),
        ("while", "@while (condition) { … }"),
        ("switch", "@switch (value) { … }"),
    ];

    public static NazorContext Classify(string text, int offset)
    {
        offset = Math.Clamp(offset, 0, text.Length);
        var parameterLine = text.LastIndexOf('\n', Math.Max(0, offset - 1)) + 1;
        var parameterPrefix = text[parameterLine..offset];
        var windowPrefix = parameterPrefix.TrimStart();
        var windowStart = parameterLine + parameterPrefix.Length - windowPrefix.Length;
        var i = 0;
        while (i < offset)
        {
            if (At(text, i, "<!--"))
            {
                var end = text.IndexOf("-->", i + 4, StringComparison.Ordinal);
                if (end < 0 || offset < end + 3) return new(NazorContextKind.Comment, offset, offset);
                i = end + 3;
                continue;
            }
            if (At(text, i, "@*"))
            {
                var end = text.IndexOf("*@", i + 2, StringComparison.Ordinal);
                if (end < 0 || offset <= end + 1) return new(NazorContextKind.Comment, offset, offset);
                i = end + 2;
                continue;
            }
            if (i == windowStart && windowPrefix.StartsWith("@window", StringComparison.Ordinal) && windowPrefix.Length > 7 &&
                (char.IsWhiteSpace(windowPrefix[7]) || windowPrefix[7] == '(') && parameterPrefix.IndexOf('(') is var parameterOpen && parameterOpen >= 0)
            {
                var close = CSharpLexer.Parenthesis(text, parameterLine + parameterOpen);
                if (offset <= close) return new(NazorContextKind.CSharp, offset, offset) { Start = parameterLine + parameterOpen + 1, End = close };
            }
            if (text[i] == '@' && BlockDirective().Match(text, i) is { Success: true, Index: var index } block && index == i)
            {
                var open = i + block.Length - 1;
                var isStyle = block.Groups["name"].Value == "style";
                var close = isStyle ? CloseNss(text, open) : CloseCSharp(text, open);
                if (offset > open && offset <= close)
                {
                    if (!isStyle) return new(NazorContextKind.CSharp, offset, offset) { Start = open + 1, End = close };
                    return new(NazorContextKind.Style, offset, offset) { Start = open + 1, End = close };
                }
                i = close + 1;
                continue;
            }
            if (text[i] == '<' && (i + 1 >= offset || char.IsAsciiLetter(text[i + 1]) || text[i + 1] == '/'))
            {
                var (context, next) = Tag(text, i, offset);
                if (context is not null) return context;
                i = next;
                continue;
            }
            i++;
        }
        var lineStart = offset;
        while (lineStart > 0 && text[lineStart - 1] is not ('\r' or '\n')) lineStart--;
        if (DirectivePrefix().Match(text[lineStart..offset]) is { Success: true } directive)
            return new(NazorContextKind.Directive, offset - directive.Groups["word"].Length, WordEnd(text, offset));
        return new(NazorContextKind.Content, offset, offset);
    }

    private static (NazorContext? Context, int Next) Tag(string text, int start, int offset)
    {
        var j = start + 1;
        if (j < text.Length && text[j] == '/') j++;
        var nameStart = j;
        while (j < text.Length && IsTagNameCharacter(text[j])) j++;
        var name = text[nameStart..j];
        if (offset <= j) return (new(NazorContextKind.TagName, nameStart, j) { Tag = name, TagStart = start }, j);

        var attributes = new List<string>();
        var quote = '\0';
        var valueStart = 0;
        string? attribute = null;
        while (true)
        {
            if (j >= offset && quote != '\0')
            {
                var (wordStart, wordEnd) = NssLanguage.Word(text, offset);
                var valueEnd = text.IndexOf(quote, j);
                return (new(NazorContextKind.AttributeValue, wordStart, wordEnd)
                {
                    Tag = name, TagStart = start, Attribute = attribute, Attributes = attributes,
                    Start = valueStart, End = valueEnd < 0 ? text.Length : valueEnd,
                }, j);
            }
            if (j >= offset)
            {
                var wordStart = offset;
                while (wordStart > nameStart && IsAttributeNameCharacter(text[wordStart - 1])) wordStart--;
                var wordEnd = offset;
                while (wordEnd < text.Length && IsAttributeNameCharacter(text[wordEnd])) wordEnd++;
                return (new(NazorContextKind.AttributeName, wordStart, wordEnd) { Tag = name, TagStart = start, Attributes = attributes }, j);
            }
            var c = text[j];
            if (quote != '\0')
            {
                if (c == quote) quote = '\0';
                j++;
                continue;
            }
            if (c is '"' or '\'') { quote = c; valueStart = j + 1; }
            else if (c == '>') return (null, j + 1);
            else if (char.IsAsciiLetter(c) || c == '@')
            {
                var attributeStart = j;
                while (j < text.Length && IsAttributeNameCharacter(text[j]) && j < offset) j++;
                attribute = text[attributeStart..j];
                if (j < offset) attributes.Add(attribute);
                continue;
            }
            j++;
        }
    }

    public static List<CompletionItem> Complete(string text, int offset, NazorCatalog catalog, bool visualStudio = false, string? styleSheet = null,
        Func<NssSelectorNames>? selectorNames = null)
    {
        var context = Classify(text, offset);
        if (context.Kind == NazorContextKind.Style)
            return NssLanguage.Complete(text[context.Start..context.End], offset - context.Start, catalog.Components, text, o => o + context.Start, visualStudio,
                selectorNames ?? (() => NssSelectorNames.FromMarkup(text, "Component.nazor")));

        var range = TextPositions.Range(text, context.WordStart, context.WordEnd);
        var items = new List<CompletionItem>();
        void Add(string label, int kind, string? detail, string? documentation, int image, JsonObject? description) =>
            items.Add(new(label, kind)
            {
                Detail = detail,
                Documentation = documentation is null ? null : new MarkupContent(documentation),
                TextEdit = new TextEdit(range, label),
                VsIcon = visualStudio ? VsElements.Image(image) : null,
                VsDescription = visualStudio ? description : null,
            });

        switch (context.Kind)
        {
            case NazorContextKind.TagName:
                if (ParentOf(Tags(text), context.TagStart) is { } parent)
                    foreach (var parentComponent in catalog.Find(parent.Name))
                        foreach (var parameter in parentComponent.Parameters.Where(p => p.Kind == NazorParameterKind.ChildContent))
                            Add(parameter.Name, CompletionItemKind.Property, parameter.TypeName, parameter.Documentation, 2436, PropertyDescription(parentComponent, parameter));
                foreach (var component in catalog.Components.DistinctBy(c => c.TagName))
                    Add(component.TagName, CompletionItemKind.Class, component.TypeName, ComponentDocumentation(component), 473, TypeDescription(component));
                break;
            case NazorContextKind.AttributeName when text[context.WordStart..offset] is { } written && written.StartsWith('@') && written.IndexOf(':') is > 0 and var colon:
            {
                var directive = written[..colon];
                var parameterRange = TextPositions.Range(text, context.WordStart + colon + 1, context.WordEnd);
                foreach (var (name, type, documentation) in DirectiveParameters(catalog, context.Tag!, directive).Where(p => !context.Attributes.Contains(directive + ":" + p.Name)))
                    items.Add(new(name, CompletionItemKind.Property)
                    {
                        Detail = type,
                        Documentation = new MarkupContent(documentation),
                        TextEdit = new TextEdit(parameterRange, name),
                        VsIcon = visualStudio ? VsElements.Image(2436) : null,
                        VsDescription = visualStudio ? VsElements.Text([VsElements.Run("keyword", type), VsElements.Run("text", " " + directive + ":" + name + "\n" + documentation)]) : null,
                    });
                break;
            }
            case NazorContextKind.AttributeName:
                var typed = text[context.WordStart..offset];
                if (ChildContent(text, context, catalog) is { Parameter.TypeName: var childType } && childType.Contains('<') && !context.Attributes.Contains("Context"))
                    Add("Context", CompletionItemKind.Property, "string", "Names the parameter of this template.", 2436,
                        VsElements.Text([VsElements.Run("keyword", "string"), VsElements.Run("property name", " Context")]));
                if (!typed.StartsWith('@') && catalog.Find(context.Tag!).FirstOrDefault() is { } target)
                {
                    foreach (var parameter in target.Parameters.Where(p => p.Kind != NazorParameterKind.ChildContent && !context.Attributes.Contains(p.Name)))
                    {
                        var callback = parameter.Kind == NazorParameterKind.EventCallback;
                        Add(parameter.Name, callback ? CompletionItemKind.Event : CompletionItemKind.Property, parameter.TypeName, parameter.Documentation,
                            callback ? 1152 : 2436, PropertyDescription(target, parameter));
                    }
                }
                foreach (var directive in catalog.DirectiveAttributesFor(context.Tag!).Where(a => !context.Attributes.Contains(a.Name)))
                    Add(directive.Name, directive.Name.StartsWith("@on", StringComparison.Ordinal) ? CompletionItemKind.Event : CompletionItemKind.Keyword, "directive attribute",
                        directive.Documentation, 1589, VsElements.Text([VsElements.Run("text", directive.Documentation ?? directive.Name)]));
                break;
            case NazorContextKind.AttributeValue when context.Attribute is "Class" or "class":
                var valueStart = text.LastIndexOf('=', Math.Max(0, context.WordStart - 1)) + 1;
                if (text[valueStart..offset].Contains('@')) break;
                var assigned = text[valueStart..context.WordStart].TrimStart(' ', '\t', '\r', '\n', '\"', '\'').Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
                foreach (var name in ClassNames(text).Concat(NssLanguage.ClassNames(styleSheet ?? "")).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal)
                    .Where(name => !assigned.Contains(name, StringComparer.Ordinal)))
                    Add(name, CompletionItemKind.Value, "class of the component's styles", null, 762, VsElements.Text([VsElements.Run("text", "." + name)]));
                break;
            case NazorContextKind.Directive:
                foreach (var (name, description) in Directives)
                    Add(name, CompletionItemKind.Keyword, description, null, 1589, VsElements.Text([VsElements.Run("keyword", "@" + name), VsElements.Run("text", " - " + description)]));
                break;
        }
        return items;
    }

    public static IEnumerable<(string Name, string Type, string Documentation)> DirectiveParameters(NazorCatalog catalog, string tag, string directive)
    {
        var attributes = catalog.DirectiveAttributesFor(tag).ToList();
        if (directive.StartsWith("@bind-", StringComparison.Ordinal))
        {
            if (!attributes.Any(a => a.Name == directive)) yield break;
            yield return ("after", "Action | Func<Task>", "Runs after the bound value has been updated.");
            yield return ("get", "value", "The value shown, with :set instead of a two-way binding.");
            yield return ("set", "Action<T> | Func<T, Task>", "Receives the new value instead of assigning it, with :get.");
            yield break;
        }
        foreach (var attribute in attributes.Where(a => a.Name.StartsWith(directive + ":", StringComparison.Ordinal)))
            yield return (attribute.Name[(directive.Length + 1)..], "bool", attribute.Documentation ?? attribute.Name);
    }

    private static JsonObject TypeDescription(NazorComponentInfo component)
    {
        var runs = new List<JsonObject> { VsElements.Run("keyword", "class"), VsElements.Run("text", " ") };
        if (component.Namespace is { Length: > 0 } ns)
        {
            foreach (var part in ns.Split('.'))
            {
                runs.Add(VsElements.Run("namespace name", part));
                runs.Add(VsElements.Run("punctuation", "."));
            }
        }
        var name = component.TypeName[(component.TypeName.LastIndexOf('.') + 1)..];
        var generic = name.IndexOf('<');
        runs.Add(VsElements.Run("class name", generic < 0 ? name : name[..generic]));
        if (generic >= 0)
        {
            runs.Add(VsElements.Run("punctuation", "<"));
            var parameters = name[(generic + 1)..].TrimEnd('>').Split(',');
            for (var i = 0; i < parameters.Length; i++)
            {
                if (i > 0) runs.Add(VsElements.Run("punctuation", ", "));
                runs.Add(VsElements.Run("type parameter name", parameters[i].Trim()));
            }
            runs.Add(VsElements.Run("punctuation", ">"));
        }
        if (component.Documentation is { } documentation) runs.Add(VsElements.Run("text", "\n" + documentation));
        return VsElements.Text(runs);
    }

    private static JsonObject PropertyDescription(NazorComponentInfo component, NazorParameterInfo parameter)
    {
        var type = parameter.TypeName;
        var runs = new List<JsonObject>
        {
            VsElements.Run(CSharpKeywords.Contains(type.TrimEnd('?')) ? "keyword" : "class name", type),
            VsElements.Run("text", " "),
            VsElements.Run("class name", component.TagName),
            VsElements.Run("punctuation", "."),
            VsElements.Run("property name", parameter.Name),
            VsElements.Run("text", " "),
            VsElements.Run("punctuation", "{"),
            VsElements.Run("text", " "),
            VsElements.Run("keyword", "get"),
            VsElements.Run("punctuation", ";"),
            VsElements.Run("text", " "),
            VsElements.Run("keyword", "set"),
            VsElements.Run("punctuation", ";"),
            VsElements.Run("text", " "),
            VsElements.Run("punctuation", "}"),
        };
        if (parameter.Documentation is { } documentation) runs.Add(VsElements.Run("text", "\n" + documentation));
        return VsElements.Text(runs);
    }

    private static readonly HashSet<string> CSharpKeywords = ["bool", "byte", "char", "decimal", "double", "float", "int", "long", "object", "sbyte", "short", "string", "uint", "ulong", "ushort"];

    public static (string Markdown, int Start, int End)? Hover(string text, int offset, NazorCatalog catalog)
    {
        var context = Classify(text, offset);
        switch (context.Kind)
        {
            case NazorContextKind.Style:
                return NssLanguage.Hover(text[context.Start..context.End], offset - context.Start) is { } nss
                    ? (nss.Markdown, nss.Start + context.Start, nss.End + context.Start)
                    : null;
            case NazorContextKind.TagName:
                if (ChildContent(text, context, catalog) is { } child)
                    return ($"**{child.Parameter.Name}**: `{child.Parameter.TypeName}`" + (child.Parameter.Documentation is null ? "" : "\n\n" + child.Parameter.Documentation), context.WordStart, context.WordEnd);
                var component = catalog.Find(context.Tag!).FirstOrDefault();
                return component is null ? null : (ComponentDocumentation(component), context.WordStart, context.WordEnd);
            case NazorContextKind.AttributeName:
                var name = text[context.WordStart..context.WordEnd];
                if (catalog.Find(context.Tag!).SelectMany(c => c.Parameters).FirstOrDefault(p => p.Name == name) is { } parameter)
                {
                    var markdown = $"**{parameter.Name}**: `{parameter.TypeName}`" + (parameter.Documentation is null ? "" : "\n\n" + parameter.Documentation);
                    return (markdown, context.WordStart, context.WordEnd);
                }
                if (catalog.DirectiveAttributesFor(context.Tag!).FirstOrDefault(a => a.Name == name) is { } directive)
                    return ($"**{directive.Name}**" + (directive.Documentation is null ? "" : "\n\n" + directive.Documentation), context.WordStart, context.WordEnd);
                if (name.IndexOf(':') is > 0 and var separator &&
                    DirectiveParameters(catalog, context.Tag!, name[..separator]).FirstOrDefault(p => p.Name == name[(separator + 1)..]) is { Name: not null } bound)
                    return ($"**{name}**: `{bound.Type}`\n\n{bound.Documentation}", context.WordStart, context.WordEnd);
                return null;
            default:
                return null;
        }
    }

    public static JsonObject? RichHover(string text, int offset, NazorCatalog catalog)
    {
        var context = Classify(text, offset);
        JsonObject? description = null;
        var image = 2436;
        if (context.Kind == NazorContextKind.TagName && ChildContent(text, context, catalog) is { } child)
            description = PropertyDescription(child.Component, child.Parameter);
        else if (catalog.Find(context.Tag ?? "").FirstOrDefault() is { } component)
        {
            if (context.Kind == NazorContextKind.TagName) { description = TypeDescription(component); image = 473; }
            else if (context.Kind == NazorContextKind.AttributeName && component.Parameters.FirstOrDefault(p => p.Name == text[context.WordStart..context.WordEnd]) is { } parameter)
                description = PropertyDescription(component, parameter);
        }
        return description is null ? null : VsElements.Container(VsElements.Stacked | VsElements.VerticalPadding,
            [VsElements.Container(VsElements.Wrapped, [VsElements.Image(image), description])]);
    }

    public static (NazorComponentInfo Component, NazorParameterInfo Parameter)? ChildContent(string text, NazorContext context, NazorCatalog catalog)
    {
        var tags = Tags(text);
        var position = context.TagStart;
        if (tags.FirstOrDefault(t => t.Start == position) is { IsEnd: true } end && StartOf(tags, end) is { } start) position = start.Start;
        if (ParentOf(tags, position) is not { } parent) return null;
        foreach (var component in catalog.Find(parent.Name))
            if (component.Parameters.FirstOrDefault(p => p.Kind == NazorParameterKind.ChildContent && p.Name == context.Tag) is { } parameter)
                return (component, parameter);
        return null;
    }

    private static MarkupTag? ParentOf(IReadOnlyList<MarkupTag> tags, int offset)
    {
        var open = new List<MarkupTag>();
        foreach (var tag in tags)
        {
            if (tag.Start >= offset) break;
            if (tag.SelfClosing) continue;
            if (!tag.IsEnd) open.Add(tag);
            else if (open.FindLastIndex(t => t.Name == tag.Name) is var index and >= 0) open.RemoveRange(index, open.Count - index);
        }
        return open.LastOrDefault();
    }

    public static List<MarkupTag> Tags(string text)
    {
        var tags = new List<MarkupTag>();
        var i = 0;
        while (i < text.Length)
        {
            if (At(text, i, "<!--"))
            {
                var end = text.IndexOf("-->", i + 4, StringComparison.Ordinal);
                i = end < 0 ? text.Length : end + 3;
                continue;
            }
            if (At(text, i, "@*"))
            {
                var end = text.IndexOf("*@", i + 2, StringComparison.Ordinal);
                i = end < 0 ? text.Length : end + 2;
                continue;
            }
            if (text[i] == '@' && BlockDirective().Match(text, i) is { Success: true, Index: var index } block && index == i)
            {
                var open = i + block.Length - 1;
                i = (block.Groups["name"].Value == "style" ? CloseNss(text, open) : CloseCSharp(text, open)) + 1;
                continue;
            }
            if (text[i] == '<' && i + 1 < text.Length && (char.IsAsciiLetter(text[i + 1]) || text[i + 1] == '/'))
            {
                var isEnd = text[i + 1] == '/';
                var nameStart = isEnd ? i + 2 : i + 1;
                var j = nameStart;
                while (j < text.Length && IsTagNameCharacter(text[j])) j++;
                var nameEnd = j;
                var quote = '\0';
                for (; j < text.Length; j++)
                {
                    var c = text[j];
                    if (quote != '\0') { if (c == quote) quote = '\0'; }
                    else if (c is '"' or '\'') quote = c;
                    else if (c is '>' or '<') break;
                }
                var closed = j < text.Length && text[j] == '>';
                var tagEnd = closed ? j + 1 : j;
                if (nameEnd > nameStart)
                    tags.Add(new(i, tagEnd, text[nameStart..nameEnd], nameStart, isEnd, closed && text[j - 1] == '/'));
                i = Math.Max(tagEnd, i + 1);
                continue;
            }
            i++;
        }
        return tags;
    }

    public static MarkupTag? StartOf(IReadOnlyList<MarkupTag> tags, MarkupTag end)
    {
        var open = new List<MarkupTag>();
        foreach (var tag in tags)
        {
            if (tag.Start >= end.Start) break;
            if (tag.SelfClosing) continue;
            if (!tag.IsEnd) open.Add(tag);
            else if (open.FindLastIndex(t => t.Name == tag.Name) is var index and >= 0) open.RemoveRange(index, open.Count - index);
        }
        var match = open.FindLastIndex(t => t.Name == end.Name);
        return match < 0 ? null : open[match];
    }

    public static MarkupTag? EndOf(IReadOnlyList<MarkupTag> tags, MarkupTag start) =>
        tags.Where(t => t.IsEnd && t.Name == start.Name && t.Start > start.Start).FirstOrDefault(t => StartOf(tags, t) == start);

    public static string ComponentDocumentation(NazorComponentInfo component)
    {
        var builder = new StringBuilder($"**{component.TagName}** (`{component.TypeName}`)");
        if (component.Documentation is not null) builder.Append("\n\n").Append(component.Documentation);
        if (component.Parameters.Count != 0)
        {
            builder.Append("\n\nParameters:\n");
            foreach (var parameter in component.Parameters) builder.Append("\n- `").Append(parameter.Name).Append("`: `").Append(parameter.TypeName).Append('`');
        }
        return builder.ToString();
    }

    public static IEnumerable<(int Start, int End)> StyleBlocks(string text)
    {
        for (var match = AnyBlockDirective().Match(text); match.Success; match = match.NextMatch())
        {
            if (match.Groups["name"].Value != "style") continue;
            var open = match.Index + match.Length - 1;
            if (Classify(text, open + 1) is not { Kind: NazorContextKind.Style } context || context.Start != open + 1) continue;
            yield return (open + 1, CloseNss(text, open));
        }
    }

    private static IEnumerable<string> ClassNames(string text) => StyleBlocks(text)
        .SelectMany(block => NssLanguage.ClassNames(text[block.Start..block.End]))
        .Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal);

    internal static int CloseNss(string text, int open)
    {
        var depth = 0;
        var quote = '\0';
        var comment = false;
        for (var i = open; i < text.Length; i++)
        {
            var c = text[i];
            var next = i + 1 < text.Length ? text[i + 1] : '\0';
            if (comment)
            {
                if (c == '*' && next == '/') { comment = false; i++; }
                continue;
            }
            if (quote != '\0')
            {
                if (c == '\\') i++;
                else if (c == quote) quote = '\0';
                continue;
            }
            if (c == '/' && next == '*') { comment = true; i++; }
            else if (c is '"' or '\'') quote = c;
            else if (c == '{') depth++;
            else if (c == '}' && --depth == 0) return i;
        }
        return text.Length;
    }

    internal static int CloseCSharp(string text, int open)
    {
        var depth = 0;
        for (var i = open; i < text.Length; i++)
        {
            var c = text[i];
            var next = i + 1 < text.Length ? text[i + 1] : '\0';
            if (c == '/' && next == '/')
            {
                while (i < text.Length && text[i] is not ('\r' or '\n')) i++;
            }
            else if (c == '/' && next == '*')
            {
                var end = text.IndexOf("*/", i + 2, StringComparison.Ordinal);
                i = end < 0 ? text.Length : end + 1;
            }
            else if (c is '"' or '\'')
            {
                var verbatim = i > 0 && text[i - 1] == '@';
                for (i++; i < text.Length && text[i] != c; i++)
                {
                    if (text[i] == '\\' && !verbatim) i++;
                    else if (text[i] is '\r' or '\n' && !verbatim) break;
                }
            }
            else if (c == '{') depth++;
            else if (c == '}' && --depth == 0) return i;
        }
        return text.Length;
    }

    private static int WordEnd(string text, int offset)
    {
        while (offset < text.Length && char.IsAsciiLetter(text[offset])) offset++;
        return offset;
    }

    private static bool At(string text, int index, string value) => string.CompareOrdinal(text, index, value, 0, value.Length) == 0;

    private static bool IsTagNameCharacter(char c) => char.IsAsciiLetterOrDigit(c) || c is '_' or '.';

    private static bool IsAttributeNameCharacter(char c) => char.IsAsciiLetterOrDigit(c) || c is '_' or '-' or '@' or ':' or '.';

    [GeneratedRegex(@"\G@(?<name>code|functions|style)\s*\{")]
    private static partial Regex BlockDirective();

    [GeneratedRegex(@"@(?<name>code|functions|style)\s*\{")]
    private static partial Regex AnyBlockDirective();

    [GeneratedRegex(@"^\s*@(?<word>[a-z]*)$")]
    private static partial Regex DirectivePrefix();

    [GeneratedRegex(@"\.(?<name>[A-Za-z_-][\w-]*)")]
    private static partial Regex ClassSelector();
}
