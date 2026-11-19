using System.Text.RegularExpressions;
using Nazor.Compiler;
using Nazor.LanguageServer.Protocol;
using Nazor.Nss;
using Nazor.Style;

namespace Nazor.LanguageServer.Features;

internal sealed record NssReferenceDocument(string Path, string Text, string? TypeName = null);
internal sealed record NssReferenceLocation(string Uri, Nazor.LanguageServer.Protocol.Range Range, string Preview);
internal sealed record NssReferenceRule(int Start, int Length, string Selector, IReadOnlyList<NssReferenceLocation> References, bool Partial, bool Global);

internal static class NssReferences
{
    public static IReadOnlyList<NssReferenceRule> Query(string path, string text, IReadOnlyList<NssReferenceDocument> documents, NazorCatalog catalog)
    {
        var component = path.EndsWith(".nazor", StringComparison.OrdinalIgnoreCase);
        var owner = component ? path : path.EndsWith(".nazor.nss", StringComparison.OrdinalIgnoreCase) ? path[..^4] : null;
        var forest = new Forest(documents, catalog);
        var roots = owner is null ? documents : documents.Where(d => string.Equals(d.Path, owner, StringComparison.OrdinalIgnoreCase));
        var nodes = roots.SelectMany(d => forest.Expand(d, null, new HashSet<string>(StringComparer.OrdinalIgnoreCase))).ToArray();
        var rules = new List<NssReferenceRule>();
        foreach (var (start, end) in component ? NazorLanguage.StyleBlocks(text) : new[] { (0, text.Length) })
            Walk(start, end, []);
        return rules;

        void Walk(int from, int to, IReadOnlyList<string> parents)
        {
            foreach (var block in NssBlocks.At(text, from, to))
            {
                var prelude = block.Prelude(text);
                if (prelude.StartsWith('@') && !Regex.IsMatch(prelude, @"^@(media|supports|layer)\b")) continue;
                var context = parents.Append(prelude).ToArray();
                if (!prelude.StartsWith('@'))
                {
                    var probe = string.Concat(context.Select(p => p + " { ")) + "color: red;" + new string('}', context.Length);
                    var parsed = NssCompiler.Compile("references", probe);
                    if (parsed.Diagnostics.Count == 0 && parsed.StyleSheet.Rules.Count > 0)
                    {
                        var selectors = parsed.StyleSheet.Rules.Select(r => r.Selector).ToArray();
                        var found = new Dictionary<(string, int), NssReferenceLocation>();
                        var partial = forest.Partial;
                        foreach (var node in nodes.Where(n => !n.Boundary))
                        {
                            bool? matches = false;
                            foreach (var selector in selectors)
                            {
                                matches = Or(matches, Match(selector, selector.Compounds.Count - 1, node, owner, node,
                                    owner is null || string.Equals(node.Document.Path, owner, StringComparison.OrdinalIgnoreCase), false));
                            }
                            if (matches is null) partial = true;
                            if (matches != true) continue;
                            found.TryAdd((node.Document.Path, node.ReferenceStart), new(new Uri(node.Document.Path).AbsoluteUri,
                                TextPositions.Range(node.Document.Text, node.ReferenceStart, node.ReferenceStart + node.ReferenceLength), Line(node.Document.Text, node.ReferenceStart)));
                        }
                        var end = block.Start + text[block.Start..block.Open].TrimEnd().Length;
                        rules.Add(new(block.Start, end - block.Start, prelude,
                            [.. found.Values.OrderBy(l => l.Uri, StringComparer.Ordinal).ThenBy(l => l.Range.Start.Line).ThenBy(l => l.Range.Start.Character)], partial, owner is null));
                    }
                }
                Walk(block.Open + 1, block.End - 1, context);
            }
        }
    }

    private static string Line(string text, int at)
    {
        var start = at == 0 ? 0 : text.LastIndexOf('\n', at - 1) + 1;
        var end = text.IndexOf('\n', at);
        return text[start..(end < 0 ? text.Length : end)].TrimEnd('\r');
    }

    private sealed class Element(NssReferenceDocument document, MarkupTag tag, Element? parent, bool boundary)
    {
        public NssReferenceDocument Document { get; } = document;
        public MarkupTag Tag { get; } = tag;
        public int ReferenceStart { get; init; } = tag.NameStart;
        public int ReferenceLength { get; init; } = tag.Name.Length;
        public Element? Parent { get; } = parent;
        public bool Boundary { get; } = boundary;
        public bool WindowRoot { get; init; }
        public string? DefinitionPath { get; set; }
        public List<Element> SiblingList { get; set; } = [];
        public List<Element> Children { get; } = [];
        public Dictionary<string, string?> Attributes { get; } = new(StringComparer.OrdinalIgnoreCase);
        public bool Splat { get; set; }
    }

    private sealed class Forest(IReadOnlyList<NssReferenceDocument> documents, NazorCatalog catalog)
    {
        private int remaining = 10000;
        private readonly Dictionary<string, (MarkupTag[] Tags, int? Window)> parsed = new(StringComparer.OrdinalIgnoreCase);
        public bool Partial { get; private set; }
        public IEnumerable<Element> Expand(NssReferenceDocument document, Element? parent, IReadOnlySet<string> chain)
        {
            if (chain.Contains(document.Path) || chain.Count >= 32) { Partial = true; yield break; }
            var visiting = chain.Append(document.Path).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var stack = new List<Element>();
            var roots = new List<Element>();
            if (!parsed.TryGetValue(document.Path, out var syntax))
            {
                syntax = (MarkupSource.Tags(document.Text, document.Path), NazorCompiler.WindowDirectiveOffset(document.Text, document.Path));
                parsed[document.Path] = syntax;
            }
            if (syntax.Window is int offset)
            {
                var window = new Element(document, new(offset, offset + 7, "Window", offset + 1, false, false), parent, false)
                { ReferenceStart = offset, ReferenceLength = 7, WindowRoot = true, SiblingList = parent?.Children ?? roots };
                window.Attributes["role"] = "window";
                window.SiblingList.Add(window);
                yield return window;
                parent = window;
            }
            foreach (var tag in syntax.Tags)
            {
                if (--remaining < 0) { Partial = true; yield break; }
                if (tag.IsEnd)
                {
                    var index = stack.FindLastIndex(n => n.Tag.Name == tag.Name);
                    if (index >= 0) stack.RemoveRange(index, stack.Count - index);
                    continue;
                }
                var candidates = documents.Where(d => Path.GetFileNameWithoutExtension(d.Path) == tag.Name.Split('.').Last()).ToArray();
                if (tag.Name.Contains('.')) candidates = candidates.Where(d => d.TypeName == tag.Name).ToArray();
                var known = catalog.Find(tag.Name).ToArray();
                if (!tag.Name.Contains('.') && known.Select(c => c.TypeName).Distinct(StringComparer.Ordinal).Skip(1).Any())
                { candidates = []; Partial = true; }
                var boundary = candidates.Length > 0 || known.Any(c => c.Namespace != "Nazor.Controls" && c.TagName != "Window");
                var element = new Element(document, tag, stack.LastOrDefault() ?? parent, boundary);
                element.DefinitionPath = candidates.Length == 1 ? candidates[0].Path : null;
                element.SiblingList = element.Parent?.Children ?? roots;
                element.SiblingList.Add(element);
                var source = document.Text[(tag.NameStart + tag.Name.Length)..tag.End];
                foreach (var attribute in MarkupSource.Attributes(source))
                {
                    if (attribute.Key == "@attributes") element.Splat = true;
                    element.Attributes[attribute.Key] = attribute.Value;
                }
                yield return element;
                if (boundary)
                {
                    if (candidates.Length == 1)
                        foreach (var child in Expand(candidates[0], element, visiting)) yield return child;
                    else Partial = true;
                    if (!tag.SelfClosing) Partial = true;
                }
                if (!tag.SelfClosing) stack.Add(element);
            }
        }
    }

    private static bool? Match(Selector selector, Element node) => Match(selector, selector.Compounds.Count - 1, node, null, node, true, false);

    private static bool? Match(Selector selector, int index, Element node, string? scope, Element segment, bool inScope, bool deep)
    {
        var own = Match(selector.Compounds[index], node);
        if (node.Boundary)
        {
            if (!deep && segment.Document.Path != node.DefinitionPath) return false;
            if (deep) inScope = scope is null;
            inScope |= scope is null || string.Equals(node.Document.Path, scope, StringComparison.OrdinalIgnoreCase);
            segment = node;
        }
        else if (deep)
        {
            segment = node;
            inScope = scope is null || string.Equals(node.Document.Path, scope, StringComparison.OrdinalIgnoreCase);
        }
        if (own == false || index == 0) return And(own, inScope);
        var relation = selector.Combinators[index - 1];
        IEnumerable<Element> previous = relation switch
        {
            Combinator.Child => Parents(node).Where(p => !p.Boundary).Take(1),
            Combinator.NextSibling => Siblings(node).Reverse().SkipWhile(n => n != node).Skip(1).Take(1),
            Combinator.SubsequentSibling => Siblings(node).TakeWhile(n => n != node),
            _ => Parents(node),
        };
        bool? ancestor = false;
        foreach (var parent in previous) ancestor = Or(ancestor, Match(selector, index - 1, parent, scope, segment, inScope, relation == Combinator.Deep));
        return And(own, ancestor);
    }

    private static IEnumerable<Element> Parents(Element node)
    {
        for (var parent = node.Parent; parent is not null; parent = parent.Parent) yield return parent;
    }

    private static IEnumerable<Element> Siblings(Element node) => node.SiblingList.Where(n => !n.Boundary);

    private static bool? Match(CompoundSelector selector, Element node)
    {
        if (selector.Type is { } type && type != node.Tag.Name.Split('.').Last()) return false;
        if (node.Boundary)
            return selector.Type is not null && selector.Id is null && selector.Classes.Count == 0 && selector.Attributes.Count == 0 &&
                selector.Pseudos.Count == 0 && selector.States == 0;
        bool? result = true;
        if (selector.Id is not null) result = And(result, Attribute(node, "Id", value => value.Trim() == selector.Id));
        foreach (var required in selector.Classes)
            result = And(result, Attribute(node, "Class", value => value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Contains(required, StringComparer.Ordinal)));
        foreach (var attribute in selector.Attributes)
            result = And(result, Attribute(node, attribute.Name, value => Compare(value, attribute)));
        foreach (var pseudo in selector.Pseudos)
        {
            bool? value = null;
            if (pseudo is RootSelector) value = node.WindowRoot;
            else if (pseudo is LogicalSelector logical)
            {
                value = false;
                foreach (var alternative in logical.Selectors.Items)
                    value = Or(value, alternative.Compounds.Any(c => c.States != 0) ? null : Match(alternative, node));
                if (logical.Kind == LogicalPseudo.Not) value = value is null ? null : !value;
            }
            result = And(result, value);
        }
        return result;
    }

    private static bool? Attribute(Element node, string name, Func<string, bool> test) =>
        node.Attributes.TryGetValue(name, out var value) ? value is null ? null : test(value) : node.Splat ? null : false;

    private static bool Compare(string value, AttributeSelector selector)
    {
        var expected = selector.Value ?? "";
        var comparison = selector.IgnoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        return selector.Match switch
        {
            AttributeMatch.Exists => value != "false",
            AttributeMatch.Equals => string.Equals(value, expected, comparison),
            AttributeMatch.Includes => expected.Length > 0 && value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Any(v => string.Equals(v, expected, comparison)),
            AttributeMatch.DashMatch => string.Equals(value, expected, comparison) || value.StartsWith(expected + "-", comparison),
            AttributeMatch.Prefix => expected.Length > 0 && value.StartsWith(expected, comparison),
            AttributeMatch.Suffix => expected.Length > 0 && value.EndsWith(expected, comparison),
            AttributeMatch.Substring => expected.Length > 0 && value.Contains(expected, comparison),
            _ => false,
        };
    }

    private static bool? And(bool? first, bool? second) => first == false || second == false ? false : first is null || second is null ? null : true;
    private static bool? Or(bool? first, bool? second) => first == true || second == true ? true : first is null || second is null ? null : false;

}
