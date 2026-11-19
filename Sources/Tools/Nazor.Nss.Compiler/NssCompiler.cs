using Nazor.Rendering;
using Nazor.Style;
using Nazor.Style.Syntax;

namespace Nazor.Nss;

public sealed record NssSource(string Path, string Text, int StartLine = 1, int StartColumn = 1);

public sealed record NssDiagnostic(string Id, string Path, int Line, int Column, string Message, bool IsError = true, int Length = 0)
{
    public override string ToString() => $"{Path}({Line},{Column}): {(IsError ? "error" : "warning")} {Id}: {Message}";
}

public sealed record NssCompilation(StyleSheet StyleSheet, IReadOnlyList<NssDiagnostic> Diagnostics)
{
    public bool Success => Diagnostics.All(d => !d.IsError);
}

public static class NssCompiler
{
    public static NssCompilation Compile(string name, IEnumerable<NssSource> sources, Func<string, string?>? idType = null)
    {
        var context = new Context { IdType = idType };
        foreach (var source in sources)
        {
            context.Source = source;
            var tokens = StyleTokenizer.Tokenize(source.Text);
            new Parser(tokens, context).ParseStylesheet();
        }
        var rules = context.Rules.Select(rule => ResolveUrls(name, rule)).ToList();
        return new NssCompilation(new StyleSheet(name, rules, context.Keyframes), context.Diagnostics);
    }

    public static NssCompilation Compile(string name, string text, string path = "inline.nss") => Compile(name, [new NssSource(path, text)]);

    /// <summary>Resolves the enclosing selectors, including nesting, in an incomplete editor buffer.</summary>
    public static IReadOnlyList<Selector> SelectorsAt(string text, int offset)
    {
        IReadOnlyList<Selector> selectors = [];
        offset = Math.Clamp(offset, 0, text.Length);
        var context = new Context
        {
            Source = new("completion.nss", text[..offset] + "color: initial;"),
            Declaration = (token, end, enclosing) => { if (token.Start <= offset && end >= offset) selectors = enclosing; },
        };
        new Parser(StyleTokenizer.Tokenize(context.Source.Text), context).ParseStylesheet();
        return selectors;
    }

    public static string ResolveUrl(string sheet, string url)
    {
        if (url.StartsWith('/')) return url.TrimStart('/');
        if (Uri.TryCreate(url, UriKind.Absolute, out var absolute) && absolute.Scheme is "http" or "https" or "file") return url;
        var folder = Path.GetDirectoryName(sheet.Replace('\\', '/'))?.Replace('\\', '/') ?? "";
        var parts = new List<string>();
        foreach (var part in (folder.Length == 0 ? url : folder + "/" + url).Replace('\\', '/').Split('/'))
        {
            if (part is "" or ".") continue;
            if (part == ".." && parts.Count > 0 && parts[^1] != "..") parts.RemoveAt(parts.Count - 1);
            else parts.Add(part);
        }
        return string.Join('/', parts);
    }

    private static StyleRule ResolveUrls(string sheet, StyleRule rule)
    {
        if (!rule.Declarations.Any(d => d is ValueDeclaration { Value: ValueList<ImageValue> images } && images.Items.Any(i => i is UrlImage))) return rule;
        var declarations = rule.Declarations.Select(declaration => declaration is ValueDeclaration { Value: ValueList<ImageValue> images } value
            ? new ValueDeclaration(value.Property, new ValueList<ImageValue>(images.Items.Select(i => i is UrlImage url ? new UrlImage(ResolveUrl(sheet, url.Url)) : i)))
            : declaration).ToArray();
        return new StyleRule(rule.Selector, rule.Media, declarations, rule.Order);
    }

    internal sealed class Context
    {
        public NssSource Source = null!;
        public List<StyleRule> Rules { get; } = [];
        public List<KeyframesRule> Keyframes { get; } = [];
        public List<NssDiagnostic> Diagnostics { get; } = [];
        public Func<string, string?>? IdType;
        public Action<StyleToken, int, IReadOnlyList<Selector>>? Declaration;
        public bool InHas;

        public void Error(string id, StyleToken token, string message, bool isError = true)
        {
            var (line, column) = Position(token.Start);
            Diagnostics.Add(new NssDiagnostic(id, Source.Path, line, column, message, isError, Length: Math.Max(token.Length, 0)));
        }

        private (int Line, int Column) Position(int offset)
        {
            var text = Source.Text;
            offset = Math.Clamp(offset, 0, text.Length);
            var line = 0;
            var lineStart = 0;
            for (var i = 0; i < offset; i++)
            {
                if (text[i] == '\n' || (text[i] == '\r' && (i + 1 >= text.Length || text[i + 1] != '\n')))
                {
                    line++;
                    lineStart = i + 1;
                }
            }
            return line == 0 ? (Source.StartLine, Source.StartColumn + offset) : (Source.StartLine + line, offset - lineStart + 1);
        }
    }

    private sealed class SkipException : Exception;

    private sealed class Parser(List<StyleToken> tokens, Context context)
    {
        private int position;

        private StyleToken Current => tokens[position];

        public void ParseStylesheet()
        {
            while (true)
            {
                SkipWhitespace();
                if (Current.Kind == StyleTokenKind.EndOfFile) return;
                if (Current.Kind == StyleTokenKind.RightBrace)
                {
                    context.Error("NZS100", Current, "Unexpected '}'.");
                    position++;
                    continue;
                }
                try
                {
                    if (Current.Kind == StyleTokenKind.AtKeyword) ParseAtRule(parents: null, media: null);
                    else ParseRule(parents: null, media: null);
                }
                catch (SkipException)
                {
                }
            }
        }

        private void ParseRule(IReadOnlyList<Selector>? parents, MediaQuery? media)
        {
            var preludeStart = position;
            var prelude = ReadUntil(StyleTokenKind.LeftBrace);
            if (Current.Kind != StyleTokenKind.LeftBrace)
            {
                context.Error("NZS100", tokens[preludeStart], "Expected '{' after the selector.");
                throw new SkipException();
            }
            position++;
            IReadOnlyList<Selector> selectors;
            try
            {
                selectors = SelectorParser.Parse(prelude, parents, context);
            }
            catch (SkipException)
            {
                SkipBlock();
                throw;
            }
            ParseBlock(selectors, media);
        }

        private void SkipBlock()
        {
            var depth = 1;
            for (; Current.Kind != StyleTokenKind.EndOfFile; position++)
            {
                if (Current.Kind == StyleTokenKind.LeftBrace) depth++;
                else if (Current.Kind == StyleTokenKind.RightBrace && --depth == 0)
                {
                    position++;
                    return;
                }
            }
        }

        private void ParseBlock(IReadOnlyList<Selector>? selectors, MediaQuery? media)
        {
            var declarations = new List<StyleDeclaration>();
            var ruleIndex = context.Rules.Count;
            while (true)
            {
                SkipWhitespace();
                var token = Current;
                if (token.Kind == StyleTokenKind.EndOfFile)
                {
                    context.Error("NZS100", token, "Missing '}' at the end of the block.");
                    break;
                }
                if (token.Kind == StyleTokenKind.RightBrace)
                {
                    position++;
                    break;
                }
                if (token.Kind == StyleTokenKind.Semicolon)
                {
                    position++;
                    continue;
                }
                try
                {
                    if (token.Kind == StyleTokenKind.AtKeyword)
                    {
                        if (selectors is null) ParseAtRule(null, media);
                        else ParseAtRule(selectors, media);
                    }
                    else if (IsNestedRule())
                    {
                        if (selectors is null) ParseRule(null, media);
                        else ParseRule(selectors, media);
                    }
                    else if (selectors is null)
                    {
                        context.Error("NZS100", token, "Declarations must be inside a rule.");
                        ReadUntil(StyleTokenKind.Semicolon);
                    }
                    else ParseDeclaration(declarations, selectors);
                }
                catch (SkipException)
                {
                }
            }
            if (selectors is null || declarations.Count == 0) return;
            var own = selectors.Select(selector => new StyleRule(selector, media, declarations, 0));
            context.Rules.InsertRange(ruleIndex, own);
            Renumber();
        }

        private void Renumber()
        {
            for (var i = 0; i < context.Rules.Count; i++)
            {
                var rule = context.Rules[i];
                if (rule.Order != i) context.Rules[i] = new StyleRule(rule.Selector, rule.Media, rule.Declarations, i);
            }
        }

        private bool IsNestedRule()
        {
            var depth = 0;
            for (var i = position; i < tokens.Count; i++)
            {
                switch (tokens[i].Kind)
                {
                    case StyleTokenKind.LeftParen or StyleTokenKind.Function or StyleTokenKind.LeftBracket:
                        depth++;
                        break;
                    case StyleTokenKind.RightParen or StyleTokenKind.RightBracket:
                        depth--;
                        break;
                    case StyleTokenKind.LeftBrace when depth == 0:
                        return true;
                    case StyleTokenKind.Semicolon or StyleTokenKind.RightBrace or StyleTokenKind.EndOfFile when depth <= 0:
                        return false;
                }
            }
            return false;
        }

        private void ParseDeclaration(List<StyleDeclaration> declarations, IReadOnlyList<Selector>? selectors = null)
        {
            var nameToken = Current;
            if (nameToken.Kind != StyleTokenKind.Ident)
            {
                context.Error("NZS100", nameToken, $"Expected a property name, found '{nameToken}'.");
                SkipDeclaration();
                return;
            }
            position++;
            SkipWhitespace();
            if (Current.Kind != StyleTokenKind.Colon)
            {
                context.Error("NZS100", Current, $"Expected ':' after '{nameToken.Value}'.");
                SkipDeclaration();
                return;
            }
            position++;
            var value = ReadDeclarationValue();
            if (Current.Kind == StyleTokenKind.Semicolon) position++;
            context.Declaration?.Invoke(nameToken, Current.Start, selectors ?? []);
            if (selectors is not null)
            {
                if (!NssApplicability.AppliesTo(nameToken.Value, selectors, context.IdType))
                    context.Error("NZS112", nameToken, $"Property '{nameToken.Value}' does not apply to the component selected by '{string.Join(", ", selectors)}'.", isError: false);
            }
            DeclarationCompiler.Compile(nameToken, value, declarations, context);
        }

        private List<StyleToken> ReadDeclarationValue()
        {
            var value = new List<StyleToken>();
            var depth = 0;
            for (; Current.Kind != StyleTokenKind.EndOfFile; position++)
            {
                var token = Current;
                if (depth == 0 && token.Kind is StyleTokenKind.Semicolon or StyleTokenKind.RightBrace) break;
                if (token.Kind is StyleTokenKind.Function or StyleTokenKind.LeftParen) depth++;
                else if (token.Kind == StyleTokenKind.RightParen) depth--;
                value.Add(token);
            }
            while (value.Count > 0 && value[^1].Kind == StyleTokenKind.Whitespace) value.RemoveAt(value.Count - 1);
            while (value.Count > 0 && value[0].Kind == StyleTokenKind.Whitespace) value.RemoveAt(0);
            return value;
        }

        private void SkipDeclaration()
        {
            ReadDeclarationValue();
            if (Current.Kind == StyleTokenKind.Semicolon) position++;
        }

        private void ParseAtRule(IReadOnlyList<Selector>? parents, MediaQuery? media)
        {
            var keyword = Current;
            position++;
            if (keyword.Value.Equals("keyframes", StringComparison.OrdinalIgnoreCase))
            {
                if (parents is not null || media is not null)
                {
                    context.Error("NZS104", keyword, "@keyframes must be at the top level of the stylesheet, outside rules and @media.");
                    SkipAtRule();
                    return;
                }
                ParseKeyframes(keyword);
                return;
            }
            if (!keyword.Value.Equals("media", StringComparison.OrdinalIgnoreCase))
            {
                context.Error("NZS104", keyword, $"At-rule '@{keyword.Value}' is not supported by NSS.");
                SkipAtRule();
                return;
            }
            var prelude = ReadUntil(StyleTokenKind.LeftBrace);
            if (Current.Kind != StyleTokenKind.LeftBrace)
            {
                context.Error("NZS100", keyword, "Expected '{' after the media query.");
                throw new SkipException();
            }
            position++;
            MediaQuery? query;
            try
            {
                query = MediaQueryParser.Parse(prelude, keyword, context);
            }
            catch (SkipException)
            {
                SkipBlock();
                throw;
            }
            var combined = media is null ? query : query is null ? media : media.And(query);
            ParseBlock(parents, combined);
        }

        private void ParseKeyframes(StyleToken keyword)
        {
            var prelude = ReadUntil(StyleTokenKind.LeftBrace).Where(t => t.Kind != StyleTokenKind.Whitespace).ToList();
            if (Current.Kind != StyleTokenKind.LeftBrace)
            {
                context.Error("NZS100", keyword, "Expected '{' after the name of the @keyframes rule.");
                throw new SkipException();
            }
            position++;
            if (prelude is not [{ Kind: StyleTokenKind.Ident or StyleTokenKind.String } nameToken] ||
                (nameToken.Kind == StyleTokenKind.Ident && nameToken.Value.ToLowerInvariant() is "none" or "initial" or "inherit" or "unset"))
            {
                context.Error("NZS108", prelude.Count > 0 ? prelude[0] : keyword, "Expected the name of the @keyframes rule, an identifier other than none.");
                SkipBlock();
                return;
            }
            var frames = new List<Keyframe>();
            while (true)
            {
                SkipWhitespace();
                if (Current.Kind == StyleTokenKind.EndOfFile)
                {
                    context.Error("NZS100", Current, "Missing '}' at the end of the @keyframes rule.");
                    break;
                }
                if (Current.Kind == StyleTokenKind.RightBrace)
                {
                    position++;
                    break;
                }
                var selectorStart = Current;
                var selectors = ReadUntil(StyleTokenKind.LeftBrace, StyleTokenKind.Semicolon);
                if (Current.Kind != StyleTokenKind.LeftBrace)
                {
                    context.Error("NZS108", selectorStart, "Expected a keyframe: from, to or a percentage, then '{'.");
                    if (Current.Kind == StyleTokenKind.Semicolon) position++;
                    continue;
                }
                position++;
                var offsets = KeyframeOffsets(selectors, selectorStart);
                var (declarations, easing) = ParseKeyframeBlock();
                if (offsets is null) continue;
                frames.AddRange(offsets.Select(offset => new Keyframe(offset, declarations, easing)));
            }
            context.Keyframes.Add(new KeyframesRule(nameToken.Value, [.. frames.OrderBy(f => f.Offset)]));
        }

        private List<double>? KeyframeOffsets(List<StyleToken> selectors, StyleToken start)
        {
            var offsets = new List<double>();
            foreach (var part in new TokenReader(selectors).SplitByComma())
            {
                var token = part.Next();
                double? offset = token switch
                {
                    { Kind: StyleTokenKind.Ident } when token.Value.Equals("from", StringComparison.OrdinalIgnoreCase) => 0,
                    { Kind: StyleTokenKind.Ident } when token.Value.Equals("to", StringComparison.OrdinalIgnoreCase) => 1,
                    { Kind: StyleTokenKind.Percentage, Number: >= 0 and <= 100 } => token.Number / 100,
                    _ => null,
                };
                if (offset is null || !part.AtEnd)
                {
                    context.Error("NZS108", token.Kind == StyleTokenKind.EndOfFile ? start : token, "A keyframe is from, to or a percentage between 0% and 100%.");
                    return null;
                }
                offsets.Add(offset.Value);
            }
            return offsets;
        }

        private (List<StyleDeclaration> Declarations, EasingFunction? Easing) ParseKeyframeBlock()
        {
            var declarations = new List<StyleDeclaration>();
            EasingFunction? easing = null;
            while (true)
            {
                SkipWhitespace();
                var token = Current;
                if (token.Kind == StyleTokenKind.EndOfFile)
                {
                    context.Error("NZS100", token, "Missing '}' at the end of the keyframe.");
                    break;
                }
                if (token.Kind == StyleTokenKind.RightBrace)
                {
                    position++;
                    break;
                }
                if (token.Kind == StyleTokenKind.Semicolon)
                {
                    position++;
                    continue;
                }
                if (token.Kind == StyleTokenKind.AtKeyword || IsNestedRule())
                {
                    context.Error("NZS108", token, "A keyframe has only declarations: no nested rules or at-rules.");
                    if (token.Kind == StyleTokenKind.AtKeyword) position++;
                    ReadUntil(StyleTokenKind.LeftBrace);
                    if (Current.Kind == StyleTokenKind.LeftBrace)
                    {
                        position++;
                        SkipBlock();
                    }
                    continue;
                }
                if (token.Kind == StyleTokenKind.Ident && token.Value.Equals("animation-timing-function", StringComparison.OrdinalIgnoreCase))
                {
                    position++;
                    SkipWhitespace();
                    if (Current.Kind == StyleTokenKind.Colon) position++;
                    var value = ReadDeclarationValue();
                    if (Current.Kind == StyleTokenKind.Semicolon) position++;
                    try
                    {
                        var reader = new TokenReader(value);
                        easing = ValueParsers.ParseEasing(reader);
                        reader.ExpectEnd();
                    }
                    catch (StyleValueException exception)
                    {
                        context.Error("NZS102", exception.Token.Kind == StyleTokenKind.EndOfFile ? token : exception.Token, $"Invalid value for 'animation-timing-function': {exception.Message}");
                    }
                    continue;
                }
                var before = declarations.Count;
                ParseDeclaration(declarations);
                var added = declarations.GetRange(before, declarations.Count - before);
                declarations.RemoveRange(before, added.Count);
                var kept = added.Where(Animatable).ToList();
                if (added.Count > 0 && kept.Count == 0)
                    context.Error("NZS108", token, added[0] is CustomPropertyDeclaration
                        ? "Custom properties cannot be animated in @keyframes."
                        : $"'{token.Value}' cannot be animated: @keyframes takes animatable properties only.");
                declarations.AddRange(kept);
            }
            return (declarations, easing);
        }

        private static bool Animatable(StyleDeclaration declaration) => declaration switch
        {
            ValueDeclaration value => value.Property.Animatable,
            KeywordDeclaration keyword => keyword.Property.Animatable,
            PendingDeclaration pending => StyleProperties.TryGetShorthand(pending.PropertyName, out var shorthand)
                ? shorthand.Longhands.Any(l => l.Animatable)
                : StyleProperties.TryGet(pending.PropertyName, out var property) && property.Animatable,
            _ => false,
        };

        private void SkipAtRule()
        {
            ReadUntil(StyleTokenKind.LeftBrace, StyleTokenKind.Semicolon);
            if (Current.Kind == StyleTokenKind.Semicolon)
            {
                position++;
                return;
            }
            if (Current.Kind != StyleTokenKind.LeftBrace) return;
            var depth = 0;
            for (; Current.Kind != StyleTokenKind.EndOfFile; position++)
            {
                if (Current.Kind == StyleTokenKind.LeftBrace) depth++;
                else if (Current.Kind == StyleTokenKind.RightBrace && --depth == 0)
                {
                    position++;
                    return;
                }
            }
        }

        private List<StyleToken> ReadUntil(params StyleTokenKind[] stops)
        {
            var result = new List<StyleToken>();
            for (; Current.Kind != StyleTokenKind.EndOfFile && !stops.Contains(Current.Kind); position++)
            {
                if (Current.Kind == StyleTokenKind.RightBrace) break;
                result.Add(Current);
            }
            return result;
        }

        private void SkipWhitespace()
        {
            while (Current.Kind == StyleTokenKind.Whitespace) position++;
        }
    }

    private static class SelectorParser
    {
        private sealed class Part
        {
            public string? Type;
            public string? Id;
            public List<string> Classes = [];
            public ElementStates States;
            public List<AttributeSelector> Attributes = [];
            public List<PseudoSelector> Pseudos = [];
            public PseudoElement PseudoElement;
            public bool Nesting;
            public bool HasPart => Pseudos.Any(p => p is PartSelector);
            public bool HasSimple => Type is not null || Id is not null || Classes.Count > 0 || States != ElementStates.None || Attributes.Count > 0 || Pseudos.Count > 0;
        }

        public static IReadOnlyList<Selector> Parse(List<StyleToken> prelude, IReadOnlyList<Selector>? parents, Context context)
        {
            var result = new List<Selector>();
            foreach (var group in Split(prelude))
            {
                if (group.Count == 0)
                {
                    context.Error("NZS103", prelude.Count > 0 ? prelude[0] : default, "Empty selector.");
                    throw new SkipException();
                }
                var (parts, combinators, leading) = ParseComplex(group, context);
                foreach (var selector in Resolve(parts, combinators, leading, parents, group[0], context))
                {
                    if (selector.Compounds.Take(selector.Compounds.Count - 1).Any(p => p.PseudoElement != PseudoElement.None || p.Pseudos.Any(s => s is PartSelector)))
                    {
                        context.Error("NZS103", group[0], "A pseudo-element must end the selector.");
                        throw new SkipException();
                    }
                    if (selector.Combinators.Count(c => c == Combinator.Deep) > 1)
                    {
                        context.Error("NZS103", group[0], "Use one ::deep per expanded selector.");
                        throw new SkipException();
                    }
                    result.Add(selector);
                }
            }
            return result;
        }

        private static IEnumerable<List<StyleToken>> Split(List<StyleToken> prelude)
        {
            var current = new List<StyleToken>();
            var depth = 0;
            foreach (var token in prelude)
            {
                if (token.Kind is StyleTokenKind.Function or StyleTokenKind.LeftParen) depth++;
                else if (token.Kind == StyleTokenKind.RightParen) depth--;
                if (token.Kind == StyleTokenKind.Comma && depth == 0)
                {
                    yield return Trim(current);
                    current = [];
                }
                else current.Add(token);
            }
            yield return Trim(current);
        }

        private static List<StyleToken> Trim(List<StyleToken> tokens)
        {
            var start = tokens.FindIndex(t => t.Kind != StyleTokenKind.Whitespace);
            if (start < 0) return [];
            var end = tokens.FindLastIndex(t => t.Kind != StyleTokenKind.Whitespace);
            return tokens.GetRange(start, end - start + 1);
        }

        private static Combinator? CombinatorOf(StyleToken token) =>
            token.IsDelim('>') ? Combinator.Child : token.IsDelim('+') ? Combinator.NextSibling : token.IsDelim('~') ? Combinator.SubsequentSibling : null;

        private static (List<Part> Parts, List<Combinator> Combinators, Combinator? Leading) ParseComplex(List<StyleToken> tokens, Context context)
        {
            var parts = new List<Part>();
            var combinators = new List<Combinator>();
            Combinator? leading = null;
            Part? current = null;
            Combinator? pending = null;
            for (var i = 0; i < tokens.Count; i++)
            {
                var token = tokens[i];
                if (current?.HasPart == true && token.Kind is not (StyleTokenKind.Whitespace or StyleTokenKind.Colon))
                {
                    context.Error("NZS103", token, "Only interaction/control states and :state() may follow ::part().");
                    throw new SkipException();
                }
                if (current is { PseudoElement: not PseudoElement.None } && token.Kind != StyleTokenKind.Whitespace)
                {
                    context.Error("NZS103", token, "A pseudo-element must end the selector.");
                    throw new SkipException();
                }
                if (token.Kind == StyleTokenKind.Whitespace)
                {
                    if (current is not null) { parts.Add(current); current = null; pending ??= Combinator.Descendant; }
                    continue;
                }
                if (token.Kind == StyleTokenKind.Colon && i + 2 < tokens.Count &&
                    tokens[i + 1].Kind == StyleTokenKind.Colon && tokens[i + 2].Kind == StyleTokenKind.Ident && tokens[i + 2].Value.Equals("deep", StringComparison.OrdinalIgnoreCase))
                {
                    if (pending is not null && pending != Combinator.Descendant || combinators.Contains(Combinator.Deep))
                    {
                        context.Error("NZS103", token, "Use one ::deep between an anchor and its descendant selector.");
                        throw new SkipException();
                    }
                    if (current is not null) { parts.Add(current); current = null; }
                    if (parts.Count == 0) leading = Combinator.Deep;
                    pending = Combinator.Deep;
                    i += 2;
                    continue;
                }
                if (CombinatorOf(token) is { } combinator)
                {
                    if (pending == Combinator.Deep)
                    {
                        context.Error("NZS103", token, "::deep must be followed by a descendant selector.");
                        throw new SkipException();
                    }
                    if (current is not null) { parts.Add(current); current = null; }
                    if (parts.Count == 0) leading = combinator;
                    pending = combinator;
                    continue;
                }
                if (current is null)
                {
                    if (parts.Count > 0) combinators.Add(pending ?? Combinator.Descendant);
                    pending = null;
                    current = new Part();
                }
                switch (token.Kind)
                {
                    case StyleTokenKind.Ident:
                        if (token.Value == "Window")
                        {
                            context.Error("NZS109", token, "The Window type selector is no longer supported. Use :root to select the current window.");
                            throw new SkipException();
                        }
                        if (current.HasSimple || current.Nesting)
                        {
                            context.Error("NZS103", token, $"Type selector '{token.Value}' must come first in a compound selector.");
                            throw new SkipException();
                        }
                        current.Type = token.Value;
                        break;
                    case StyleTokenKind.Delim when token.Value == "*":
                        if (current.HasSimple || current.Nesting)
                        {
                            context.Error("NZS103", token, "'*' must come first in a compound selector.");
                            throw new SkipException();
                        }
                        break;
                    case StyleTokenKind.Delim when token.Value == "&":
                        if (current.HasSimple || current.Nesting)
                        {
                            context.Error("NZS103", token, "'&' must come first in a compound selector.");
                            throw new SkipException();
                        }
                        current.Nesting = true;
                        break;
                    case StyleTokenKind.Delim when token.Value == "." && i + 1 < tokens.Count && tokens[i + 1].Kind == StyleTokenKind.Ident:
                        current.Classes.Add(tokens[++i].Value);
                        break;
                    case StyleTokenKind.Hash:
                        if (current.Id is not null)
                        {
                            context.Error("NZS103", token, "A compound selector can have only one identifier.");
                            throw new SkipException();
                        }
                        current.Id = token.Value;
                        break;
                    case StyleTokenKind.Colon:
                        i = ParsePseudo(tokens, i, current, context);
                        break;
                    case StyleTokenKind.LeftBracket:
                        i = ParseAttribute(tokens, i, current, context);
                        break;
                    default:
                        context.Error("NZS103", token, $"Unexpected '{token}' in selector.");
                        throw new SkipException();
                }
            }
            if (current is not null) parts.Add(current);
            if (CombinatorOf(tokens[^1]) is not null || pending == Combinator.Deep)
            {
                context.Error("NZS103", tokens[^1], "A selector cannot end with a combinator.");
                throw new SkipException();
            }
            return (parts, combinators, leading);
        }

        private static int ParsePseudo(List<StyleToken> tokens, int index, Part current, Context context)
        {
            var token = tokens[index];
            var next = index + 1 < tokens.Count ? tokens[index + 1] : default;
            if (current.HasPart && !(next.Kind == StyleTokenKind.Ident && (PseudoClasses.TryGet(next.Value, out _) || PseudoClasses.Structure(next.Value) is ControlStateSelector) ||
                next.Is(StyleTokenKind.Function, "state")))
            {
                context.Error("NZS103", next, "Only interaction/control states and :state() may follow ::part().");
                throw new SkipException();
            }
            if (next.Kind == StyleTokenKind.Colon && index + 2 < tokens.Count && tokens[index + 2].Is(StyleTokenKind.Function, "part"))
            {
                var (arguments, end) = Arguments(tokens, index + 2, context);
                if (current.Type is null || current.Id is not null || current.Classes.Count != 0 || current.Attributes.Count != 0 || current.Pseudos.Count != 0 || current.States != ElementStates.None || current.Nesting ||
                    arguments.Count != 1 || arguments[0].Kind != StyleTokenKind.Ident || !CustomStateSet.IsValidName(arguments[0].Value))
                {
                    context.Error("NZS103", next, "Use Component::part(name), with a simple component name and one unquoted part identifier; put target states after the part.");
                    throw new SkipException();
                }
                current.Pseudos.Add(new PartSelector(current.Type, arguments[0].Value));
                current.Type = null;
                return end;
            }
            if (next.Kind == StyleTokenKind.Ident)
            {
                if (PseudoClasses.TryGet(next.Value, out var state)) current.States |= state;
                else if (PseudoClasses.Structure(next.Value) is { } structure) current.Pseudos.Add(structure);
                else Unsupported(next);
                return index + 1;
            }
            if (next.Kind == StyleTokenKind.Function)
            {
                var name = next.Value.ToLowerInvariant();
                var (arguments, end) = Arguments(tokens, index + 1, context);
                switch (name)
                {
                    case "not" or "is" or "where":
                        var selectors = ArgumentSelectors(arguments, next, context);
                        current.Pseudos.Add(new LogicalSelector(name switch { "not" => LogicalPseudo.Not, "is" => LogicalPseudo.Is, _ => LogicalPseudo.Where },
                            new ValueList<Selector>(selectors)));
                        return end;
                    case "nth-child" or "nth-last-child" or "nth-of-type" or "nth-last-of-type":
                        var of = -1;
                        var depth = 0;
                        for (var i = 0; i < arguments.Count; i++)
                        {
                            if (depth == 0 && arguments[i].Is(StyleTokenKind.Ident, "of")) { of = i; break; }
                            if (arguments[i].Kind is StyleTokenKind.Function or StyleTokenKind.LeftParen) depth++;
                            else if (arguments[i].Kind == StyleTokenKind.RightParen) depth--;
                        }
                        if (of >= 0 && name.EndsWith("of-type", StringComparison.Ordinal))
                        {
                            context.Error("NZS103", next, "The 'of' selector filter is supported only by :nth-child() and :nth-last-child().");
                            throw new SkipException();
                        }
                        var (a, b) = ParseNth(of < 0 ? arguments : Trim(arguments.Take(of).ToList()), next, context);
                        var filter = of < 0 ? null : new ValueList<Selector>(ArgumentSelectors(Trim(arguments.Skip(of + 1).ToList()), next, context));
                        current.Pseudos.Add(new NthSelector(a, b, name.Contains("last", StringComparison.Ordinal), name.EndsWith("of-type", StringComparison.Ordinal),
                            $":{name}({Nth(a, b)})") { Filter = filter });
                        return end;
                    case "nth-item":
                        var (itemA, itemB) = ParseNth(arguments, next, context);
                        current.Pseudos.Add(new ItemPositionSelector(itemA, itemB, false, $":nth-item({Nth(itemA, itemB)})"));
                        return end;
                    case "state":
                        if (arguments.Count != 1 || arguments[0].Kind != StyleTokenKind.Ident || !CustomStateSet.IsValidName(arguments[0].Value))
                        {
                            context.Error("NZS103", next, ":state() requires one simple custom state identifier.");
                            throw new SkipException();
                        }
                        current.Pseudos.Add(new NamedStateSelector(arguments[0].Value));
                        return end;
                    case "has":
                        if (context.InHas)
                        {
                            context.Error("NZS103", next, ":has() cannot be nested inside :has().");
                            throw new SkipException();
                        }
                        var relatives = new List<RelativeSelector>();
                        context.InHas = true;
                        try
                        {
                            foreach (var group in Split(arguments))
                            {
                                if (group.Count == 0)
                                {
                                    context.Error("NZS103", next, ":has() needs a relative selector.");
                                    throw new SkipException();
                                }
                                var (parts, joins, leading) = ParseComplex(group, context);
                                if (leading == Combinator.Deep || joins.Contains(Combinator.Deep) || parts.Any(p => p.Nesting || p.HasPart || p.PseudoElement != PseudoElement.None))
                                {
                                    context.Error("NZS103", group[0], "'&', ::deep and pseudo-elements are not supported inside :has().");
                                    throw new SkipException();
                                }
                                relatives.Add(new RelativeSelector(leading ?? Combinator.Descendant, new Selector(parts.Select(Compound).ToList(), joins)));
                            }
                        }
                        finally { context.InHas = false; }
                        current.Pseudos.Add(new HasSelector(new ValueList<RelativeSelector>(relatives)));
                        return end;
                }
                Unsupported(next);
            }
            if (next.Kind == StyleTokenKind.Colon)
            {
                if (index + 2 < tokens.Count && tokens[index + 2].Kind == StyleTokenKind.Ident &&
                    Enum.TryParse<PseudoElement>(tokens[index + 2].Value, true, out var target) &&
                    target != PseudoElement.None && Enum.IsDefined(target))
                {
                    current.PseudoElement = target;
                    return index + 2;
                }
                context.Error("NZS103", token, "Supported pseudo-elements are ::before, ::after, ::placeholder and ::selection.");
                throw new SkipException();
            }
            Unsupported(next.Kind == StyleTokenKind.EndOfFile ? token : next);
            return index;

            void Unsupported(StyleToken name)
            {
                context.Error("NZS107", name, $"Pseudo-class ':{name.Value}' is not supported; use " +
                    string.Join(", ", PseudoClasses.Names.Select(p => ":" + p.Name).Concat(PseudoClasses.Structural.Select(p => ":" + p))
                        .Concat(ControlStateSelector.Names.Select(p => ":" + p))
                        .Concat(PseudoClasses.Functional.Select(p => ":" + p + "()"))) + ".");
                throw new SkipException();
            }
        }

        private static List<Selector> ArgumentSelectors(List<StyleToken> arguments, StyleToken function, Context context)
        {
            var selectors = new List<Selector>();
            foreach (var group in Split(arguments))
            {
                if (group.Count == 0)
                {
                    context.Error("NZS103", function, $":{function.Value}() needs a selector.");
                    throw new SkipException();
                }
                var (parts, joins, leading) = ParseComplex(group, context);
                if (leading is not null || parts.Any(p => p.Nesting || p.HasPart || p.PseudoElement != PseudoElement.None) || joins.Contains(Combinator.Deep))
                {
                    context.Error("NZS103", group[0], $"'&', ::deep, pseudo-elements and leading combinators are not supported inside :{function.Value}().");
                    throw new SkipException();
                }
                selectors.Add(new Selector(parts.Select(Compound).ToList(), joins));
            }
            return selectors;
        }

        private static (List<StyleToken> Arguments, int End) Arguments(List<StyleToken> tokens, int index, Context context)
        {
            var depth = 0;
            for (var i = index + 1; i < tokens.Count; i++)
            {
                if (tokens[i].Kind is StyleTokenKind.Function or StyleTokenKind.LeftParen) depth++;
                else if (tokens[i].Kind == StyleTokenKind.RightParen)
                {
                    if (depth == 0) return (Trim(tokens.GetRange(index + 1, i - index - 1)), i);
                    depth--;
                }
            }
            context.Error("NZS103", tokens[index], $"Missing ')' after :{tokens[index].Value}(.");
            throw new SkipException();
        }

        private static (int A, int B) ParseNth(List<StyleToken> arguments, StyleToken function, Context context)
        {
            var text = new System.Text.StringBuilder();
            StyleToken? previous = null;
            foreach (var token in arguments)
            {
                if (token.Kind == StyleTokenKind.Whitespace) continue;
                var signed = previous is { } p && !p.IsDelim('+') && !p.IsDelim('-');
                text.Append(token.Kind switch
                {
                    StyleTokenKind.Number when signed && token.Number >= 0 => "+" + token,
                    _ => token.ToString(),
                });
                previous = token;
            }
            var value = text.ToString().ToLowerInvariant();
            if (value == "odd") return (2, 1);
            if (value == "even") return (2, 0);
            var match = System.Text.RegularExpressions.Regex.Match(value, @"^(?:(?<a>[+-]?\d*)n(?<b>[+-]\d+)?|(?<only>[+-]?\d+))$");
            if (!match.Success)
            {
                context.Error("NZS103", arguments.Count > 0 ? arguments[0] : function, $"Expected odd, even or An+B in :{function.Value}(), found '{value}'.");
                throw new SkipException();
            }
            if (match.Groups["only"].Success) return (0, Number(match.Groups["only"].Value));
            var a = match.Groups["a"].Value switch
            {
                "" or "+" => 1,
                "-" => -1,
                var digits => Number(digits),
            };
            var b = match.Groups["b"].Success ? Number(match.Groups["b"].Value) : 0;
            return (a, b);

            int Number(string digits)
            {
                if (int.TryParse(digits, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out var number)) return number;
                context.Error("NZS103", function, "An+B coefficients must fit a signed 32-bit integer.");
                throw new SkipException();
            }
        }

        private static string Nth(int a, int b) => a == 0 ? b.ToString(System.Globalization.CultureInfo.InvariantCulture)
            : (a == 1 ? "" : a == -1 ? "-" : a.ToString(System.Globalization.CultureInfo.InvariantCulture)) + "n" + (b == 0 ? "" : b > 0 ? "+" + b : b.ToString(System.Globalization.CultureInfo.InvariantCulture));

        private static int ParseAttribute(List<StyleToken> tokens, int index, Part current, Context context)
        {
            var end = tokens.FindIndex(index, t => t.Kind == StyleTokenKind.RightBracket);
            if (end < 0)
            {
                context.Error("NZS103", tokens[index], "Missing ']' in the attribute selector.");
                throw new SkipException();
            }
            var inside = tokens.GetRange(index + 1, end - index - 1).Where(t => t.Kind != StyleTokenKind.Whitespace).ToList();
            if (inside.Count == 0 || inside[0].Kind != StyleTokenKind.Ident)
            {
                context.Error("NZS103", inside.Count > 0 ? inside[0] : tokens[index], "Expected an attribute name.");
                throw new SkipException();
            }
            var name = inside[0].Value;
            if (inside.Count == 1)
            {
                current.Attributes.Add(new AttributeSelector(name, AttributeMatch.Exists, null));
                return end;
            }
            var position = 1;
            var match = AttributeMatch.Equals;
            if (!inside[position].IsDelim('='))
            {
                match = inside[position].Value switch
                {
                    "~" => AttributeMatch.Includes,
                    "|" => AttributeMatch.DashMatch,
                    "^" => AttributeMatch.Prefix,
                    "$" => AttributeMatch.Suffix,
                    "*" => AttributeMatch.Substring,
                    _ => (AttributeMatch)255,
                };
                if ((byte)match == 255 || inside[position].Kind != StyleTokenKind.Delim || position + 1 >= inside.Count || !inside[position + 1].IsDelim('='))
                {
                    context.Error("NZS103", inside[position], "Expected =, ~=, |=, ^=, $= or *= in the attribute selector.");
                    throw new SkipException();
                }
                position++;
            }
            position++;
            if (position >= inside.Count || inside[position].Kind is not (StyleTokenKind.Ident or StyleTokenKind.String or StyleTokenKind.Number))
            {
                context.Error("NZS103", position < inside.Count ? inside[position] : inside[^1], "Expected a value in the attribute selector.");
                throw new SkipException();
            }
            var value = inside[position].Kind == StyleTokenKind.Number ? inside[position].ToString() : inside[position].Value;
            position++;
            var ignoreCase = false;
            if (position < inside.Count && inside[position].Kind == StyleTokenKind.Ident && inside[position].Value.ToLowerInvariant() is "i" or "s")
            {
                ignoreCase = inside[position].Value.Equals("i", StringComparison.OrdinalIgnoreCase);
                position++;
            }
            if (position < inside.Count)
            {
                context.Error("NZS103", inside[position], $"Unexpected '{inside[position]}' in the attribute selector.");
                throw new SkipException();
            }
            current.Attributes.Add(new AttributeSelector(name, match, value, ignoreCase));
            return end;
        }

        private static IEnumerable<Selector> Resolve(List<Part> parts, List<Combinator> combinators, Combinator? leading,
            IReadOnlyList<Selector>? parents, StyleToken first, Context context)
        {
            var nesting = parts.Any(p => p.Nesting);
            if (parents is null)
            {
                if (nesting || leading is not null && leading != Combinator.Deep)
                {
                    context.Error("NZS106", first, nesting ? "'&' can only be used in a nested rule." : "A top-level selector cannot start with a combinator.");
                    throw new SkipException();
                }
                if (leading == Combinator.Deep)
                {
                    parts.Insert(0, new Part());
                    combinators.Insert(0, Combinator.Deep);
                }
                yield return Build(parts.Select(Compound).ToList(), combinators);
                yield break;
            }
            foreach (var parent in parents)
            {
                if (parent.Subject.PseudoElement != PseudoElement.None)
                {
                    context.Error("NZS103", first, "Rules cannot be nested inside a pseudo-element rule.");
                    throw new SkipException();
                }
                var compounds = new List<CompoundSelector>();
                var joins = new List<Combinator>();
                if (!nesting)
                {
                    compounds.AddRange(parent.Compounds);
                    joins.AddRange(parent.Combinators);
                    joins.Add(leading ?? Combinator.Descendant);
                    compounds.AddRange(parts.Select(Compound));
                    joins.AddRange(combinators);
                }
                else
                {
                    for (var i = 0; i < parts.Count; i++)
                    {
                        if (i > 0) joins.Add(combinators[i - 1]);
                        if (!parts[i].Nesting)
                        {
                            compounds.Add(Compound(parts[i]));
                            continue;
                        }
                        compounds.AddRange(parent.Compounds.Take(parent.Compounds.Count - 1));
                        joins.AddRange(parent.Combinators);
                        compounds.Add(Merge(parent.Subject, parts[i]));
                    }
                }
                yield return Build(compounds, joins);
            }
        }

        private static Selector Build(List<CompoundSelector> compounds, List<Combinator> combinators) => new(compounds, combinators);

        private static CompoundSelector Compound(Part part) =>
            new(part.Type, part.Id, part.Classes.ToArray(), part.States, part.Attributes.ToArray(), part.Pseudos.ToArray(), part.PseudoElement);

        private static CompoundSelector Merge(CompoundSelector parent, Part part) =>
            new(parent.Type, part.Id ?? parent.Id, parent.Classes.Concat(part.Classes).Distinct(StringComparer.Ordinal).ToArray(), parent.States | part.States,
                parent.Attributes.Concat(part.Attributes).ToArray(), parent.Pseudos.Concat(part.Pseudos).ToArray(),
                part.PseudoElement == PseudoElement.None ? parent.PseudoElement : part.PseudoElement);
    }

    private static class MediaQueryParser
    {
        public static MediaQuery? Parse(List<StyleToken> prelude, StyleToken keyword, Context context)
        {
            var conditions = new List<MediaCondition>();
            var reader = new TokenReader(prelude);
            foreach (var part in reader.SplitByComma())
            {
                var condition = MediaCondition.Always;
                var first = true;
                while (!part.AtEnd)
                {
                    if (!first && !part.TryConsumeIdent("and"))
                    {
                        context.Error("NZS104", part.Peek(), "Expected 'and' between media features.");
                        throw new SkipException();
                    }
                    first = false;
                    var open = part.Next();
                    if (open.Kind != StyleTokenKind.LeftParen)
                    {
                        context.Error("NZS104", open, open.Kind == StyleTokenKind.Ident
                            ? $"Media type '{open.Value}' is not supported: use width conditions such as (max-width: 720px)."
                            : "Expected a media feature in parentheses.");
                        throw new SkipException();
                    }
                    var feature = part.Next();
                    if (feature.Kind != StyleTokenKind.Ident)
                    {
                        context.Error("NZS104", feature, "Expected a media feature name.");
                        throw new SkipException();
                    }
                    var name = feature.Value.ToLowerInvariant();
                    if (name is not ("min-width" or "max-width" or "min-height" or "max-height" or "prefers-color-scheme" or "prefers-reduced-motion" or
                        "forced-colors" or "prefers-contrast" or "platform" or "density"))
                        Unsupported(feature, context);
                    if (name is "prefers-reduced-motion" or "forced-colors" or "prefers-contrast" && part.TryConsume(StyleTokenKind.RightParen))
                    {
                        condition = condition.And(name switch
                        {
                            "prefers-reduced-motion" => new MediaCondition(null, null, null, null, PrefersReducedMotion: true),
                            "forced-colors" => new MediaCondition(null, null, null, null, ForcedColors: true),
                            _ => new MediaCondition(null, null, null, null, PrefersMoreContrast: true),
                        });
                        continue;
                    }
                    if (!part.TryConsume(StyleTokenKind.Colon))
                    {
                        context.Error("NZS104", part.Peek(), "Expected ':' after the media feature.");
                        throw new SkipException();
                    }
                    var featureCondition = name switch
                    {
                        "density" => new MediaCondition(null, null, null, null, Density: Keyword(part, context,
                            ("compact", ControlDensity.Compact), ("touch", ControlDensity.Touch))),
                        "platform" => new MediaCondition(null, null, null, null, Platform: Keyword(part, context,
                            ("windows", PlatformKind.Windows), ("linux", PlatformKind.Linux), ("macos", PlatformKind.MacOS), ("android", PlatformKind.Android), ("ios", PlatformKind.IOS))),
                        "prefers-color-scheme" => new MediaCondition(null, null, null, null, PrefersColorScheme: Keyword(part, context, ("light", ColorScheme.Light), ("dark", ColorScheme.Dark))),
                        "prefers-reduced-motion" => new MediaCondition(null, null, null, null, PrefersReducedMotion: Keyword(part, context, ("reduce", true), ("no-preference", false))),
                        "forced-colors" => new MediaCondition(null, null, null, null, ForcedColors: Keyword(part, context, ("active", true), ("none", false))),
                        "prefers-contrast" => Keyword(part, context, ("more", (bool?)true), ("no-preference", false), ("less", null)) is { } more
                            ? new MediaCondition(null, null, null, null, PrefersMoreContrast: more)
                            : MediaCondition.Never,
                        _ => Size(name, Length(part, context)),
                    };
                    if (!part.TryConsume(StyleTokenKind.RightParen))
                    {
                        context.Error("NZS104", part.Peek(), "Expected ')'.");
                        throw new SkipException();
                    }
                    condition = condition.And(featureCondition);
                }
                conditions.Add(condition);
            }
            if (conditions.Count == 0)
            {
                context.Error("NZS104", keyword, "Expected a media query.");
                throw new SkipException();
            }
            return new MediaQuery(conditions);
        }

        private static MediaCondition Size(string name, double value) => name switch
        {
            "min-width" => new MediaCondition(value, null, null, null),
            "max-width" => new MediaCondition(null, value, null, null),
            "min-height" => new MediaCondition(null, null, value, null),
            _ => new MediaCondition(null, null, null, value),
        };

        private static double Length(TokenReader part, Context context)
        {
            try
            {
                var token = part.Peek();
                var length = ValueParsers.ParseLength(part, allowPercent: false);
                var font = StyleProperties.FontSize.Initial.Value;
                Length Absolute(Length l) => l.Unit switch
                {
                    LengthUnit.Em or LengthUnit.Rem => Style.Length.Px(l.Value * font),
                    LengthUnit.Ch or LengthUnit.Ex => throw TokenReader.Error("Font metric units require an element context: media queries support px, em or rem.", token),
                    _ => l,
                };
                length = length.Expression is { } expression ? expression.Map(Absolute) : Absolute(length);
                if (length.Unit != LengthUnit.Px) throw TokenReader.Error("Viewport units are not allowed in media queries: use px, em or rem.", token);
                return length.Value;
            }
            catch (StyleValueException exception)
            {
                context.Error("NZS104", exception.Token, exception.Message);
                throw new SkipException();
            }
        }

        private static T Keyword<T>(TokenReader part, Context context, params (string Name, T Value)[] keywords)
        {
            var token = part.Next();
            foreach (var (name, value) in keywords)
                if (token.Is(StyleTokenKind.Ident, name)) return value;
            context.Error("NZS104", token, $"Expected {string.Join(" or ", keywords.Select(k => k.Name))}.");
            throw new SkipException();
        }

        private static MediaCondition Unsupported(StyleToken feature, Context context)
        {
            context.Error("NZS104", feature,
                $"Media feature '{feature.Value}' is not supported: use min-width, max-width, min-height, max-height, prefers-color-scheme, prefers-reduced-motion, forced-colors, prefers-contrast, platform or density.");
            throw new SkipException();
        }
    }
}
