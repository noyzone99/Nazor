using System.Globalization;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Nazor.Compiler;
using Nazor.Nss;
using Nazor.LanguageServer.Protocol;
using Nazor.Style;
using Nazor.Style.Syntax;

namespace Nazor.LanguageServer.Features;

internal enum NssContextKind
{
    None,
    Selector,
    ClassName,
    Id,
    PseudoClass,
    PseudoElement,
    PropertyName,
    Value,
    CustomPropertyReference,
    AtRule,
    MediaFeature,
}

internal readonly record struct NssContext(NssContextKind Kind, int WordStart, int WordEnd, string? Property = null);

internal static partial class NssLanguage
{
    private const string DeepDescription = "Crosses component style boundaries below an element or component anchor. Includes local descendants too. Clock TextBlock stops at Clock's declarations; Clock ::deep TextBlock also reaches nested components.";
    private static readonly Dictionary<string, string> PseudoElementDescriptions = new(StringComparer.OrdinalIgnoreCase)
    {
        ["before"] = "Generates a decorative first child with content: a quoted string, adjacent strings or an empty string. normal/none suppress the box. Uses Nazor block/flex/grid layout; editable and image elements do not generate boxes.",
        ["after"] = "Generates a decorative last child with content. Inherits text styles and custom properties from its originating element; normal/none suppress the box.",
        ["placeholder"] = "Styles empty editable fields' placeholder text. Supports text/font properties and opacity, without changing the field's box or entered text.",
        ["part"] = "Selects an existing explicitly exposed element with Component::part(name). Names are case-sensitive. Nested components require explicit export; target interaction/control states and :state() may follow. Adds component plus pseudo-element specificity; creates no box.",
        ["selection"] = "Styles color and background-color of selected editable text, including multiline selections. Does not change layout or the copied value.",
    };
    private static readonly Dictionary<string, string> SystemColorProperties = SystemColors.Names.Concat(SystemColors.AccentShades)
        .Concat(SystemColors.Resolve().Palette.Keys).ToDictionary(name => SystemColors.Property(name)!, name => name, StringComparer.Ordinal);
    private static readonly string[] EasingFunctions = ["linear", "ease", "ease-in", "ease-out", "ease-in-out"];
    private static readonly Dictionary<string, string> MediaFeatures = new()
    {
        ["min-width"] = "a length compared with the width of the window.",
        ["max-width"] = "a length compared with the width of the window.",
        ["min-height"] = "a length compared with the height of the window.",
        ["max-height"] = "a length compared with the height of the window.",
        ["prefers-color-scheme"] = "`light` or `dark`, the app mode of Windows (Settings > Personalization > Colors).",
        ["prefers-reduced-motion"] = "`reduce` or `no-preference`; `reduce` when \"Show animations\" is off in Windows.",
        ["forced-colors"] = "`active` or `none`; `active` with a contrast theme of Windows, when only the system colors (`Canvas`, `ButtonText`…) are used.",
        ["prefers-contrast"] = "`more`, `no-preference` or `less`; `more` with a contrast theme of Windows (`less` never matches).",
        ["platform"] = "`windows`, `linux`, `macos`, `android` or `ios`; the window backend identity, absent when unknown.",
        ["density"] = "`compact` or `touch`; the resolved control density after window/application overrides and the host's input preference.",
    };

    private static readonly Dictionary<string, string> PseudoClassDescriptions = new()
    {
        ["hover"] = "The pointer is over the element.",
        ["active"] = "The element is being pressed.",
        ["focus"] = "The element has the keyboard focus.",
        ["focus-visible"] = "The element has the keyboard focus and should show it (focus reached with the keyboard).",
        ["disabled"] = "The element is disabled.",
        ["enabled"] = "An enabled interactive role or an element explicitly publishing boolean disabled metadata. Ordinary decorative elements do not match.",
        ["indeterminate"] = "A mixed check/toggle/tree control or an unknown progress value, published through the indeterminate attribute.",
        ["read-only"] = "The element cannot be edited as text, including disabled/read-only editors and noneditor elements.",
        ["read-write"] = "An enabled shared text editor without the readonly flag.",
        ["placeholder-shown"] = "A text editor with a nonempty placeholder and an empty value, while no IME composition is being shown.",
        ["user-valid"] = "Application-owned valid state after value input/change, completed value-control focus, or nearest-form submission. Reset clears eligibility; initial invalidity alone does not enable feedback.",
        ["user-invalid"] = "Application-owned invalid state after interaction or nearest-form submission. Form reset clears eligibility before invoking the application reset callback.",
        ["valid"] = "A field or validation-capable control whose application-owned invalid state is false. No value validation is inferred.",
        ["invalid"] = "A field or validation-capable control whose own or enclosing field's invalid state is true.",
        ["required"] = "A field or validation-capable control requiring a value, from its own metadata or enclosing Field.",
        ["optional"] = "A field or validation-capable control without the required state.",
        ["checked"] = "The element is checked (CheckBox).",
        ["wheel-scrolling"] = "The scroll container is receiving wheel input, including its scrollbar-wheel-timeout after the last event.",
        ["popup-above"] = "The anchored overlay was arranged above its anchor, including a native compositor's placement. Use for visual animation origins, not popup sizing or positioning.",
        ["popup-left"] = "The anchored overlay was arranged to the left of its anchor, including a native compositor's placement. Use for callout tails and animation origins, not popup sizing or positioning.",
        ["popup-right"] = "The anchored overlay was arranged to the right of its anchor, including a native compositor's placement. Use for callout tails and animation origins, not popup sizing or positioning.",
        ["popup-origin-right"] = "The arranged overlay's right edge is nearer its anchor than its left edge; ties follow end alignment. Combines with popup-origin-bottom for four-corner animation origins.",
        ["popup-origin-bottom"] = "The arranged overlay's bottom edge is nearer its anchor than its top edge; ties follow end alignment. Uses actual arranged/native-configured bounds.",
        ["window-active"] = "The root Window is active.",
        ["window-inactive"] = "The root Window is inactive.",
        ["focus-within"] = "The element or one of its descendants has the keyboard focus.",
        ["first-child"] = "The first element among its siblings.",
        ["last-child"] = "The last element among its siblings.",
        ["only-child"] = "The only element among its siblings.",
        ["first-of-type"] = "The first element of its type among its siblings.",
        ["last-of-type"] = "The last element of its type among its siblings.",
        ["only-of-type"] = "The only element of its type among its siblings.",
        ["nth-item"] = "Matches an explicit one-based logical collection position (An+B), including unrealized items. Requires typed item-position metadata; does not count physical siblings or enumerate data.",
        ["first-item"] = "The explicit logical position is one. Spacers and headers without item-position metadata do not match.",
        ["last-item"] = "The explicit logical position equals its complete known count, including unrealized items.",
        ["component-root"] = "A physical root declared by the current scoped component instance. Multiple roots may match; nested/projected content behind a physical container does not. Has no global/inline meaning and creates no host box.",
        ["empty"] = "The element has no children and no text.",
        ["root"] = "The top element (the window).",
        ["not"] = "None of the selectors in parentheses matches the element; specificity of the most specific.",
        ["is"] = "One of the selectors in parentheses matches the element; specificity of the most specific.",
        ["state"] = "Matches one case-sensitive name in the typed custom-states snapshot, published through Control.CustomStates or an explicit retained element attribute. States are not inherited and do not change accessibility/input behavior.",
        ["has"] = "Matches an origin with a relative descendant, child (>) or following sibling (+ or ~). Uses the greatest argument specificity. Nested :has(), pseudo-elements and ::deep arguments are rejected; component scope is preserved.",
        ["where"] = "One of the selectors in parentheses matches the element; no specificity.",
        ["nth-child"] = "The element's physical sibling position is An+B (odd, even, 3, 2n+1…). An optional 'of S' selector list counts only matching siblings and adds the greatest filter specificity. This is not a virtualized collection index.",
        ["nth-last-child"] = "As :nth-child(), counting from the last sibling, including an optional 'of S' filter.",
        ["nth-of-type"] = "As :nth-child(), among the siblings of the same type.",
        ["nth-last-of-type"] = "As :nth-of-type(), counting from the last sibling.",
    };

    public static NssContext Classify(string text, int offset)
    {
        offset = Math.Clamp(offset, 0, text.Length);
        var depth = 0;
        var segmentStart = 0;
        var quote = '\0';
        var comment = false;
        var brackets = 0;
        var keyframes = 0;
        for (var i = 0; i < offset; i++)
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
            else if (c == '[') brackets++;
            else if (c == ']') brackets = Math.Max(0, brackets - 1);
            else if (c == '{')
            {
                depth++;
                if (text[segmentStart..i].TrimStart().StartsWith("@keyframes", StringComparison.OrdinalIgnoreCase)) keyframes = depth;
                segmentStart = i + 1;
            }
            else if (c == '}') { depth = Math.Max(0, depth - 1); if (depth < keyframes) keyframes = 0; segmentStart = i + 1; }
            else if (c == ';') segmentStart = i + 1;
        }
        var (wordStart, wordEnd) = Word(text, offset);
        if (comment || quote != '\0' || brackets > 0) return new(NssContextKind.None, wordStart, wordEnd);

        var segment = StyleToken.Serialize(StyleTokenizer.Tokenize(text[segmentStart..offset]));
        if (segment.StartsWith('@'))
        {
            if (AtKeyword().IsMatch(segment)) return new(NssContextKind.AtRule, wordStart, wordEnd);
            var open = segment.LastIndexOf('(');
            var inFeature = open > segment.LastIndexOf(')') && segment.IndexOf(':', open) < 0;
            return new(inFeature ? NssContextKind.MediaFeature : NssContextKind.None, wordStart, wordEnd);
        }
        if (depth > 0)
        {
            var colon = segment.IndexOf(':');
            if (colon >= 0)
            {
                var name = segment[..colon].Trim();
                if (IsPropertyName(name))
                {
                    var value = segment[(colon + 1)..];
                    if (VarReference().Match(value) is { Success: true } reference)
                        return new(NssContextKind.CustomPropertyReference, offset - reference.Groups["name"].Length, Math.Max(wordEnd, offset), name);
                    return new(NssContextKind.Value, wordStart, wordEnd, name.ToLowerInvariant());
                }
            }
            else if (segment.Length == 0 || PropertyPrefix().IsMatch(segment) &&
                (!char.IsUpper(segment[0]) || StyleProperties.All.Any(p => p.Name.StartsWith(segment, StringComparison.OrdinalIgnoreCase)) ||
                    StyleProperties.AllShorthands.Any(p => p.Name.StartsWith(segment, StringComparison.OrdinalIgnoreCase))))
            {
                return new(NssContextKind.PropertyName, wordStart, wordEnd);
            }
        }
        if (keyframes > 0) return new(NssContextKind.None, wordStart, wordEnd);
        var functions = new Stack<(string Name, bool Selector)>();
        if (segment.Contains('('))
            foreach (var token in StyleTokenizer.Tokenize(segment))
            {
                if (token.Kind is StyleTokenKind.Function or StyleTokenKind.LeftParen)
                {
                    var name = token.Value.ToLowerInvariant();
                    functions.Push((name, name is "not" or "is" or "where" or "has"));
                }
                else if (token.Kind == StyleTokenKind.RightParen && functions.Count > 0) functions.Pop();
                else if (token.Is(StyleTokenKind.Ident, "of") && functions.TryPeek(out var function) && function.Name is "nth-child" or "nth-last-child")
                {
                    functions.Pop();
                    functions.Push((function.Name, true));
                }
            }
        if (functions.Any(f => !f.Selector)) return new(NssContextKind.None, wordStart, wordEnd);
        var before = wordStart > 0 ? text[wordStart - 1] : '\0';
        return before switch
        {
            ':' when wordStart > 1 && text[wordStart - 2] == ':' => new(NssContextKind.PseudoElement, wordStart, wordEnd),
            ':' => new(NssContextKind.PseudoClass, wordStart, wordEnd),
            '.' => new(NssContextKind.ClassName, wordStart, wordEnd),
            '#' => new(NssContextKind.Id, wordStart, wordEnd),
            _ => new(NssContextKind.Selector, wordStart, wordEnd),
        };
    }

    public static List<CompletionItem> Complete(string text, int offset, IReadOnlyList<NazorComponentInfo> components, string document, Func<int, int> map, bool visualStudio = false,
        Func<NssSelectorNames>? selectorNames = null)
    {
        var context = Classify(text, offset);
        var range = TextPositions.Range(document, map(context.WordStart), map(context.WordEnd));
        var items = new List<CompletionItem>();
        List<NssVariable>? variables = null;
        IReadOnlyList<Selector>? targets = null;
        NssSelectorNames? names = null;
        string? IdType(string id)
        {
            var type = (names ??= selectorNames?.Invoke())?.IdTypes.GetValueOrDefault(id);
            var matches = components.Where(c => c.TagName == type || c.TypeName == type).Select(c => c.TypeName).Distinct().ToArray();
            return matches.Length == 0 ? type : matches.Length == 1 ? matches[0] : null;
        }
        bool Applicable(string name) => NssApplicability.AppliesTo(name, targets ??= NssCompiler.SelectorsAt(text, context.WordStart), IdType);
        string? VariableDocumentation(string name) => NssVariables.Describe(text, name, offset, variables ??= NssVariables.Read(text)) is { } description
            ? $"**{name}**\n\n{description}" : PropertyDocumentation(name, heading: true);
        void Add(string label, int kind, string? detail = null, string? documentation = null, string? sort = null) =>
            items.Add(new(label, kind)
            {
                Detail = detail,
                Documentation = documentation is null ? null : new MarkupContent(documentation),
                TextEdit = new TextEdit(range, label),
                SortText = sort,
                Data = kind == CompletionItemKind.Color && NssColors.Parse(label) is { } color
                    ? new JsonObject { ["nssColor"] = NssColors.Presentations(color)[0] } : null,
            });

        switch (context.Kind)
        {
            case NssContextKind.ClassName:
            case NssContextKind.Id:
                var selectorLabels = selectorNames?.Invoke();
                if (selectorLabels is null) break;
                var isClass = context.Kind == NssContextKind.ClassName;
                var prefix = text[context.WordStart..offset];
                foreach (var name in (isClass ? selectorLabels.Classes : selectorLabels.Ids).Where(n => n.StartsWith(prefix, StringComparison.Ordinal)))
                {
                    var label = (isClass ? "." : "#") + name;
                    items.Add(new(label, isClass ? CompletionItemKind.Class : CompletionItemKind.Variable)
                    {
                        Detail = (selectorLabels.Global ? "project markup: " : "component markup: ") + (isClass ? "class" : "ID"),
                        FilterText = label,
                        TextEdit = new(TextPositions.Range(document, map(context.WordStart - 1), map(context.WordEnd)), label),
                    });
                }
                break;
            case NssContextKind.PropertyName:
                foreach (var property in StyleProperties.All.Where(p => Applicable(p.Name)))
                    Add(property.Name, CompletionItemKind.Property, property.Syntax, PropertyDocumentation(property.Name));
                foreach (var shorthand in StyleProperties.AllShorthands.Where(p => Applicable(p.Name)))
                    Add(shorthand.Name, CompletionItemKind.Property, shorthand.Syntax, PropertyDocumentation(shorthand.Name));
                foreach (var custom in CustomProperties(variables ??= NssVariables.Read(text))) Add(custom, CompletionItemKind.Variable, "custom property", VariableDocumentation(custom));
                if (context.WordStart == offset) AddSelectors();
                break;
            case NssContextKind.Value:
                if (!TryGetSyntax(context.Property!, out var keywords, out var types)) break;
                foreach (var keyword in keywords.Concat(ValueKeywords(types)).Distinct(StringComparer.Ordinal)) Add(keyword, CompletionItemKind.Value, context.Property, sort: "0" + keyword);
                if (text[..context.WordStart].TrimEnd().EndsWith(':'))
                    foreach (var keyword in StyleProperties.GlobalKeywords) Add(keyword, CompletionItemKind.Keyword, "every property", sort: "2" + keyword);
                if (types.Any(t => t is "color" or "gradient" or "image" or "bg-layer"))
                {
                    foreach (var color in StyleProperties.ColorNames) Add(color, CompletionItemKind.Color, "color", sort: "1" + color);
                    foreach (var color in SystemColors.Names) Add(color, CompletionItemKind.Color, "dynamic system color", SystemColorDocumentation(color), sort: "1" + color);
                    foreach (var function in (string[])["rgb", "rgba", "hsl", "hsla", "color-mix"]) Add(function + "()", CompletionItemKind.Function, "color", sort: "1" + function);
                }
                if (types.Contains("easing-function"))
                {
                    foreach (var easing in EasingFunctions) Add(easing, CompletionItemKind.Value, "timing function", sort: "0" + easing);
                    Add("cubic-bezier()", CompletionItemKind.Function, "timing function", sort: "1cubic-bezier");
                }
                if (types.Contains("property"))
                    foreach (var property in StyleProperties.All.Where(p => p.Animatable && Applicable(p.Name))) Add(property.Name, CompletionItemKind.Property, "animatable property", sort: "1" + property.Name);
                if (types.Contains("keyframes-name"))
                    foreach (var name in KeyframeName().Matches(text).Select(m => m.Groups["name"].Value).Distinct(StringComparer.Ordinal)) Add(name, CompletionItemKind.Value, "keyframes");
                foreach (var function in Functions(types)) Add(function + "()", CompletionItemKind.Function, context.Property, sort: "1" + function);
                Add("var()", CompletionItemKind.Function, "the value of a custom property", sort: "3var");
                break;
            case NssContextKind.CustomPropertyReference:
                foreach (var custom in CustomProperties(variables ??= NssVariables.Read(text)).Concat(SystemColorProperties.Keys).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal))
                    Add(custom, CompletionItemKind.Variable, SystemColorProperties.ContainsKey(custom) ? "system palette variable" : "custom property", VariableDocumentation(custom));
                break;
            case NssContextKind.PseudoClass:
                foreach (var name in PseudoClasses.All) Add(name, CompletionItemKind.Keyword, "pseudo-class", PseudoClassDescriptions.GetValueOrDefault(name));
                AddDeep();
                break;
            case NssContextKind.PseudoElement:
                AddDeep();
                AddPseudoElements();
                break;
            case NssContextKind.Selector:
                AddSelectors();
                break;
            case NssContextKind.AtRule:
                Add("media", CompletionItemKind.Keyword, "@media (min-width: …) { … }");
                Add("keyframes", CompletionItemKind.Keyword, "@keyframes name { from { … } to { … } }");
                break;
            case NssContextKind.MediaFeature:
                foreach (var (feature, description) in MediaFeatures) Add(feature, CompletionItemKind.Property, "media feature: " + description);
                break;
        }
        return [.. items.Select(i => i.Data?["nssColor"]?.GetValue<string>() is { } hex && NssColors.Parse(hex) is { } color
            ? i with
            {
                Documentation = visualStudio ? null : new MarkupContent(hex, "plaintext"),
                Detail = null,
                VsIcon = visualStudio ? VsElements.Image(NssCompletionImages.Catalog, NssCompletionImages.Id(color)) : null,
            } : i)];

        void AddSelectors()
        {
            foreach (var component in components.Where(c => c.TagName != "Window").DistinctBy(c => c.TagName))
                Add(component.TagName, CompletionItemKind.Class, component.TypeName, NazorLanguage.ComponentDocumentation(component));
            Add(":root", CompletionItemKind.Keyword, "current window", PseudoClassDescriptions["root"]);
            AddDeep();
            AddPseudoElements();
        }

        void AddPseudoElements()
        {
            var start = context.WordStart;
            while (start > 0 && context.WordStart - start < 2 && text[start - 1] == ':') start--;
            foreach (var (name, description) in PseudoElementDescriptions)
                items.Add(new("::" + name, CompletionItemKind.Keyword)
                {
                    Detail = "pseudo-element",
                    Documentation = new MarkupContent(description),
                    FilterText = start < context.WordStart ? text[start..context.WordStart] + name : "::" + name,
                    TextEdit = new(TextPositions.Range(document, map(start), map(context.WordEnd)), "::" + name),
                });
        }

        void AddDeep()
        {
            var start = context.WordStart;
            while (start > 0 && context.WordStart - start < 2 && text[start - 1] == ':') start--;
            items.Add(new("::deep", CompletionItemKind.Keyword)
            {
                Detail = "cross component style boundaries",
                Documentation = new MarkupContent(DeepDescription),
                FilterText = start < context.WordStart ? text[start..context.WordStart] + "deep" : "::deep",
                TextEdit = new(TextPositions.Range(document, map(start), map(context.WordEnd)), "::deep "),
            });
        }
    }

    private static IEnumerable<string> Functions(IReadOnlyList<string> types)
    {
        if (types.Any(t => t is "length" or "length-percentage" or "track-list" or "track-size" or "bg-layer" or "image" or "gradient" or "position" or "bg-size" or "transform-function" or "inset-rect"))
            foreach (var name in (string[])["calc", "min", "max", "clamp"]) yield return name;
        if (types.Any(t => t is "url" or "image" or "bg-layer")) yield return "url";
        if (types.Any(t => t is "gradient" or "image" or "bg-layer"))
            foreach (var name in (string[])["linear-gradient", "radial-gradient", "conic-gradient", "repeating-linear-gradient", "repeating-radial-gradient", "repeating-conic-gradient"]) yield return name;
        if (types.Contains("transform-function"))
            foreach (var name in (string[])["translate", "translateX", "translateY", "scale", "scaleX", "scaleY", "rotate", "skew", "skewX", "skewY", "matrix"]) yield return name;
        if (types.Contains("inset-rect")) yield return "inset";
        if (types.Contains("filter-function-list"))
            foreach (var name in (string[])["blur", "brightness", "contrast", "grayscale", "hue-rotate", "invert", "opacity", "saturate", "sepia"]) yield return name;
        if (types.Any(t => t is "track-list" or "track-size"))
        {
            yield return "minmax";
            yield return "fit-content";
            if (types.Contains("track-list")) yield return "repeat";
        }
    }

    private static IEnumerable<string> ValueKeywords(IReadOnlyList<string> types)
    {
        if (types.Contains("inset-rect")) yield return "round";
        if (types.Any(t => t is "track-list" or "track-size"))
            foreach (var name in (string[])["auto", "min-content", "max-content"]) yield return name;
        if (types.Contains("grid-line"))
            foreach (var name in (string[])["auto", "span"]) yield return name;
        if (types.Any(t => t is "position" or "bg-layer"))
            foreach (var name in (string[])["left", "center", "right", "top", "bottom"]) yield return name;
        if (types.Any(t => t is "bg-size" or "bg-layer"))
            foreach (var name in (string[])["auto", "cover", "contain"]) yield return name;
    }

    [GeneratedRegex(@"@keyframes\s+(?<name>[\w-]+)")]
    private static partial Regex KeyframeName();

    public static IEnumerable<string> ClassNames(string text)
    {
        foreach (var block in NssBlocks.At(text, 0, text.Length))
        {
            var tokens = StyleTokenizer.Tokenize(text[block.Start..block.Open]);
            var brackets = 0;
            for (var i = 0; i + 1 < tokens.Count; i++)
            {
                if (tokens[i].Kind == StyleTokenKind.LeftBracket) brackets++;
                if (tokens[i].Kind == StyleTokenKind.RightBracket) brackets--;
                if (brackets == 0 && tokens[i].IsDelim('.') && tokens[i + 1].Kind == StyleTokenKind.Ident &&
                    tokens[i].Start + tokens[i].Length == tokens[i + 1].Start)
                    yield return tokens[i + 1].Value;
            }
            foreach (var name in ClassNames(text[(block.Open + 1)..(block.End - 1)])) yield return name;
        }
    }

    public static (string Markdown, int Start, int End)? Hover(string text, int offset)
    {
        var (start, end) = Word(text, offset);
        if (start == end) return null;
        var word = text[start..end];
        var context = Classify(text, start);
        if (StyleProperties.IsCustomProperty(word) && context.Kind is NssContextKind.PropertyName or NssContextKind.CustomPropertyReference &&
            NssVariables.Describe(text, word, start, NssVariables.Read(text)) is { } variableDescription)
            return ($"**{word}**\n\n{variableDescription}", start, end);
        string? markdown = context.Kind switch
        {
            NssContextKind.PropertyName => PropertyDocumentation(word, heading: true),
            NssContextKind.CustomPropertyReference => PropertyDocumentation(word, heading: true),
            NssContextKind.Value when SystemColors.MayName(context.Property!) && SystemColors.Variable(word) is not null => SystemColorDocumentation(word),
            NssContextKind.Value when StyleProperties.ColorNames.Contains(word.ToLowerInvariant()) => $"**{word}**: a color" + ColorDetail(word),
            NssContextKind.Value => PropertyDocumentation(context.Property!, heading: true),
            NssContextKind.PseudoClass when PseudoClassDescriptions.TryGetValue(word, out var description) => $"**:{word}**\n\n{description}",
            NssContextKind.PseudoElement when word == "deep" => $"**::deep**\n\n{DeepDescription}",
            NssContextKind.PseudoElement when PseudoElementDescriptions.TryGetValue(word, out var pseudoDescription) => $"**::{word}**\n\n{pseudoDescription}",
            NssContextKind.MediaFeature when MediaFeatures.TryGetValue(word, out var feature) => $"**{word}**: {feature}",
            _ => null,
        };
        return markdown is null ? null : (markdown, start, end);
    }

    public static string? PropertyDocumentation(string name, bool heading = false)
    {
        var builder = new StringBuilder();
        if (StyleProperties.TryGet(name, out var property))
        {
            if (heading) builder.Append("**").Append(property.Name).Append("**: ");
            builder.Append('`').Append(property.Syntax).Append("`\n\n");
            builder.Append("Initial value: `").Append(Format(property.InitialValue, property.Keywords)).Append("` · ");
            builder.Append(property.Inherited ? "inherited" : "not inherited");
            if (property.Animatable) builder.Append(" · animatable");
            if (property.Name.StartsWith("-windows-badge-", StringComparison.Ordinal))
            {
                builder.Append("\n\nWindows only. Applies to the root Window that publishes the shared application launcher badge. Ignored by other backends. The shell reserves 16 × 16 logical pixels; size and offsets affect content within that area. Percentages use the overlay or container dimensions; currentColor uses the Window color. The image is supplied by IApplicationLauncher.SetBadgeIcon.");
                builder.Append(property.Name switch
                {
                    "-windows-badge-background-color" => " Auto uses red for counts and transparency for icons.",
                    "-windows-badge-image-background-color" => " Auto uses the container background; an explicit color overrides it only for icons.",
                    "-windows-badge-font-size" => " Percentages use the container height. Text-fit shrink can reduce this size.",
                    "-windows-badge-font-family" => " Uses the owning application's fonts, including bundled private fonts and configured fallbacks.",
                    "-windows-badge-background-image" => " Decorative URL images and gradients are separate from the code-supplied badge icon. Layers paint first on top.",
                    "-windows-badge-background-clip" => " Border-box includes the border area; padding-box clips the color and background layers inside the border.",
                    "-windows-badge-image-position" => " Percentages and math expressions resolve against the free space after image fitting, including negative free space for cover.",
                    "-windows-badge-transform" => " Static transform of the complete badge, including shadows and mask. The final overlay remains 16 × 16 logical pixels.",
                    "-windows-badge-mask-image" => " One raster or SVG URL image used as an alpha mask and stretched over the container. Pending or failed masks hide the badge; none disables masking.",
                    "-windows-badge-text-shadow" => " Text shadows accept offsets and optional blur; inset and spread are rejected.",
                    "-windows-badge-image-clip" => " Border-box clips the image to the rounded container. Content is always cropped to the content box.",
                    _ => "",
                });
            }
            if (property.Name == "-windows-title-bar-height")
                builder.Append("\n\nWindows 11 only. Applies to the root Window. Requests standard or tall integrated buttons on a custom title bar. Read the measured height through --nz-window-controls-height. Ignored for system and borderless frames.");
            else if (property.Name.StartsWith("-windows-title-bar-", StringComparison.Ordinal))
                builder.Append("\n\nWindows 11 only. Applies to the root Window. Uses AppWindowTitleBar colors; auto restores the system default without inheriting TitleBar colors. Custom title bars use button colors only. Windows retains the close button hover/pressed background.");
            var description = property.Name switch
            {
                "-window-frame-border-width" => "Applies to the root Window. Sets the thickness of the outline drawn by Nazor inside the frame without changing layout or the resize area.",
                "-window-frame-border-style" => "Applies to the root Window. Sets the line style of the frame outline drawn by Nazor.",
                "-window-frame-border-color" => "Applies to the root Window. Sets the color of the frame outline drawn by Nazor.",
                "-window-frame-border-top-left-radius" or "-window-frame-border-top-right-radius" or
                    "-window-frame-border-bottom-right-radius" or "-window-frame-border-bottom-left-radius" =>
                    "Applies to the root Window. Rounds a corner of the frame drawn by Nazor where supported. Auto uses the backend default.",
                "-window-frame-box-shadow" => "Applies to the root Window. Paints an exterior shadow where the backend supports a Nazor frame. The shadow is independent of the resize area.",
                "-window-frame-resize-inset" => "Applies to the root Window. Sets the distance inside the frame edge where Nazor handles resize. Zero disables the inside band.",
                "-window-frame-resize-outset" => "Applies to the root Window. Sets the distance outside the frame edge where Nazor handles resize on backends with exterior input. Zero disables the outside band.",
                "-window-frame-resize-corner-length" => "Applies to the root Window. Sets how far along the edge a resize uses the diagonal direction. The resize bands determine the thickness.",
                "-window-resize-grip" => "Applies to the root Window. Auto shows a resize grip in the client area on supported desktop backends when the window is resizable and in its normal state.",
                "-window-resize-grip-color" => "Applies to the root Window. Sets the color of the resize grip glyph over the client background. currentColor follows the Window color.",
                "scroll-behavior" => "Auto applies scroll requests immediately. Smooth animates wheel, keyboard and programmatic requests using scroll-duration and scroll-timing-function. Precise pixel input and thumb dragging remain direct. Reduced motion disables interpolation; offsets never snap to lines.",
                "scroll-duration" => "Duration of each smooth scroll request. New wheel detents update the pending destination from the visible position. Use 0ms for immediate scrolling.",
                "overlay-exit-duration" => "Retains closing overlay and exit-presence surfaces for their exit animation. Use a nonnegative time up to 4294967294ms. The surface is noninteractive and hidden from assistive technologies while exiting; modal and native-surface ownership ends on removal. Reduced motion forces immediate removal.",
                "ring-shape" => "Selects a ring glyph's path. Circle uses its content box. Border-box follows its rounded border rectangle, starting on the top edge nearest the center and progressing clockwise by cumulative length. Border-radius supplies the corners; stroke-radius controls the inset, or zero insets half the stroke width.",
                "font-symbol" => "Ordered quoted font-family/glyph pairs for decorative text. The active provider must match the exact family and cover every Unicode scalar; missing pairs are skipped together. At most 32 pairs are supported. None uses ordinary text. If no candidate is available, no symbol ink is drawn. Noninherited; ignored by editable text.",
                "number-motion" => "Numeric presentation on NumberPresenter: none is immediate; linear uses the duration per scalar unit in either direction; linear-increase applies decreases immediately; exponential removes the configured fraction of remaining distance per normalized 60Hz frame. New targets sample the displayed value first. Reduced motion, suspension and hidden owners apply targets immediately.",
                "number-motion-duration" => "Time per scalar unit for linear numeric presentation. For example 2s takes 500ms to move from 0.25 to 0.5. Zero is immediate; total motion time must fit 4294967294ms.",
                "number-motion-decay" => "Fraction of remaining distance removed per normalized 60Hz reference frame, from 0 to 1. For example 0.14 reproduces 14% convergence at 60Hz while remaining independent of actual render cadence. Zero and one are immediate.",
                "number-motion-tolerance" => "Absolute remaining distance used to stop exponential motion; nonnegative. Snap follows the prior reference-frame residual, matching an iterative check before each update. Zero uses floating-point convergence.",
                "number-boundary-tolerance" => "Absolute displayed start/end tolerance for NumberPresenter states, capped at half its range. This changes presentation flags without changing the target or the application's accessible range value.",
                "stroke-trim-start" or "stroke-trim-end" => "Selects a fraction from 0 to 1 along a vector stroke. Accepts unitless calc(), min(), max() and clamp() with custom properties. Lengths, percentages, nonfinite results and out-of-range values are rejected.",
                "scroll-timing-function" => "Easing for smooth scrolling, including cubic-bezier(). Applies when scroll-behavior is smooth.",
                "scroll-wheel-step" => "Distance per wheel detent: auto follows system settings, a length such as 24px specifies pixels, and 3 lines uses the container's computed line-height. Fractional values are supported; lines never impose snapping. Zero consumes detents without moving. Precise pixel input is not scaled.",
                "scrollbar-visibility" => "Controls scrollbars on elements with overflow auto or scroll. None hides the bars without disabling scrolling. Autohide reveals them near the pointer, during dragging and on wheel input; override :wheel-scrolling to change wheel visibility.",
                "scrollbar-placement" => "Reserve reduces the content viewport beside visible scrollbars. Autohide always uses overlay placement.",
                "scrollbar-thumb-width" => "Resting painted thumb width. Auto uses the full scrollbar width; smaller values center the thumb within its hit area.",
                "scrollbar-thumb-expanded-width" => "Painted thumb width when the pointer is over the scrollbar or the thumb is dragged. Auto uses the resting width.",
                "scrollbar-thumb-hover-color" => "Color of the thumb under the pointer. Auto uses scrollbar-thumb-color.",
                "scrollbar-thumb-active-color" => "Color of the captured thumb during dragging. Auto uses scrollbar-thumb-color.",
                "scrollbar-thumb-border-highlight-color" => "Top and left border color for a thumb with square corners. Auto uses scrollbar-thumb-border-color.",
                "scrollbar-track-hover-color" => "Track fill when the pointer is over the scrollbar or the thumb is dragged. Auto uses scrollbar-track-color.",
                "scrollbar-radius" => "Corner radius of the thumb; also used by the track when scrollbar-track-radius is auto.",
                "scrollbar-track-radius" => "Corner radius of the track. Auto uses scrollbar-radius.",
                "scrollbar-opacity" => "Opacity of the entire scrollbar, multiplied by the autohide fade. Supports transitions and keyframe animations.",
                "scrollbar-fade-in-duration" => "Duration of the autohide reveal fade. Use 0ms for an immediate reveal.",
                "scrollbar-fade-out-duration" => "Duration of the autohide dismissal fade after scrollbar-fade-out-delay. Use 0ms for an immediate dismissal.",
                "scrollbar-fade-out-delay" => "Delay before autohide fades out after pointer, wheel and drag activity ends.",
                "scrollbar-fade-timing-function" => "Easing used by both autohide fades.",
                "scrollbar-wheel-timeout" => "How long :wheel-scrolling remains active after the last wheel event. Autohide normally reveals the bar during this state.",
                "scrollbar-reveal-distance" => "Pointer proximity distance that reveals an autohide scrollbar.",
                "scrollbar-buttons" => "Shows optional decrement and increment buttons at the ends of each scrollbar.",
                "scrollbar-button-step" => "Scroll distance for a button press and each repeated step while held.",
                "scrollbar-button-repeat-delay" => "Delay before a held scrollbar button starts repeating.",
                "scrollbar-button-repeat-interval" => "Time between repeated steps while a scrollbar button is held. Use 0ms to disable repeat.",
                _ => null,
            };
            if (description is not null) builder.Append("\n\n").Append(description);
            return builder.ToString();
        }
        if (StyleProperties.TryGetShorthand(name, out var shorthand))
        {
            if (heading) builder.Append("**").Append(shorthand.Name).Append("**: ");
            builder.Append('`').Append(shorthand.Syntax).Append("`\n\n");
            builder.Append("Shorthand for ").Append(string.Join(", ", shorthand.Longhands.Select(l => l.Name))).Append('.');
            if (shorthand.Name.StartsWith("-windows-badge-", StringComparison.Ordinal))
                builder.Append("\n\nWindows only. Applies to the root Window that publishes the shared application launcher badge.");
            if (shorthand.Name == "-window-frame-border")
                builder.Append("\n\nApplies to the root Window. Draws a frame outline inside the window bounds without changing layout or the resize area.");
            if (shorthand.Name == "-window-frame-border-radius")
                builder.Append("\n\nApplies to the root Window. Sets frame corner radii clockwise from the top-left corner. Auto uses the backend defaults.");
            return builder.ToString();
        }
        if (name.StartsWith("--nz-system-", StringComparison.Ordinal))
        {
            var alias = SystemColorProperties.TryGetValue(name, out var role) && SystemColors.Variable(role) is not null
                ? $" Also read through the `{role}` keyword; overriding this variable changes that keyword too." : "";
            return $"**{name}**: system palette custom property, read with `var({name})`. Populated from the resolved window palette when available; values may come from the backend, derivation or a desktop fallback. It inherits and can be overridden like any custom property.{alias} Prefer theme tokens such as `--nz-surface` or `--nz-accent` for application customization.";
        }
        if (name == "--nz-window-controls-height") return "**--nz-window-controls-height**: measured integrated system button height in logical px. Available for visible integrated system buttons on a custom title bar; absent otherwise, so var() can use a fallback. Scoped to this window.";
        return StyleProperties.IsCustomProperty(name) && heading ? $"**{name}**: a custom property, read with `var({name})`." : null;
    }

    private static string SystemColorDocumentation(string name) =>
        $"**{name}**: dynamic system color, equivalent to `var({SystemColors.Variable(name)})`. The resolved window palette follows system preferences and uses derived or fallback colors where needed. Redefining the variable changes the keyword too. Prefer theme tokens such as `--nz-surface` or `--nz-accent` for application customization.";

    private static bool TryGetSyntax(string name, out IReadOnlyList<string> keywords, out IReadOnlyList<string> types)
    {
        if (StyleProperties.TryGet(name, out var property))
        {
            (keywords, types) = (property.Keywords, property.ValueTypes);
            return true;
        }
        if (name == "--nz-window-controls-height")
        {
            (keywords, types) = ([], ["length"]);
            return true;
        }
        if (StyleProperties.IsCustomProperty(name))
        {
            (keywords, types) = ([], ["color", "length", "image", "track-list", "transform-function", "easing-function"]);
            return true;
        }
        if (StyleProperties.TryGetShorthand(name, out var shorthand))
        {
            (keywords, types) = (shorthand.Keywords, shorthand.ValueTypes);
            return true;
        }
        (keywords, types) = ([], []);
        return false;
    }

    private static bool IsPropertyName(string name) => PropertyName().IsMatch(name) &&
        (StyleProperties.IsCustomProperty(name) || StyleProperties.TryGet(name, out _) || StyleProperties.TryGetShorthand(name, out _));

    private static IEnumerable<string> CustomProperties(IReadOnlyList<NssVariable> declarations) =>
        declarations.Select(d => d.Name).Append("--nz-window-controls-height").Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal);

    public static (int Start, int End) Word(string text, int offset)
    {
        offset = Math.Clamp(offset, 0, text.Length);
        var start = offset;
        while (start > 0 && IsNameCharacter(text[start - 1])) start--;
        var end = offset;
        while (end < text.Length && IsNameCharacter(text[end])) end++;
        return (start, end);
    }

    private static bool IsNameCharacter(char c) => char.IsAsciiLetterOrDigit(c) || c is '_' or '-' || c >= 0x80;

    private static string Format(object value, IReadOnlyList<string> keywords) => value switch
    {
        bool enabled when keywords.Contains("auto") && keywords.Contains("none") => enabled ? "auto" : "none",
        bool enabled when keywords.Contains("visible") && keywords.Contains("none") => enabled ? "visible" : "none",
        Enum e => keywords.FirstOrDefault(k => k.Replace("-", "").Equals(e.ToString(), StringComparison.OrdinalIgnoreCase)) ?? Kebab(e.ToString()),
        double d => d.ToString("0.###", CultureInfo.InvariantCulture),
        TimeSpan time => time.TotalMilliseconds.ToString("0.###", CultureInfo.InvariantCulture) + "ms",
        int i => i.ToString(CultureInfo.InvariantCulture),
        Color { IsCurrentColor: true } => "currentcolor",
        _ => value.ToString() ?? "",
    };

    private static string Kebab(string name) => UpperCase().Replace(name, m => (m.Index == 0 ? "" : "-") + m.Value.ToLowerInvariant());

    private static string ColorDetail(string name)
    {
        try
        {
            return StyleProperties.Color.Parse(new TokenReader(StyleTokenizer.Tokenize(name))) is Color { IsCurrentColor: false } color ? $" (`{color}`)" : "";
        }
        catch (StyleValueException)
        {
            return "";
        }
    }

    [GeneratedRegex(@"^@[\w-]*$")]
    private static partial Regex AtKeyword();

    [GeneratedRegex(@"^(--?[a-z0-9-]*|[a-z][a-z0-9-]*)$", RegexOptions.IgnoreCase)]
    private static partial Regex PropertyPrefix();

    [GeneratedRegex(@"^(--[\w-]+|-?[a-z][a-z0-9-]*)$", RegexOptions.IgnoreCase)]
    private static partial Regex PropertyName();

    [GeneratedRegex(@"var\(\s*(?<name>-{0,2}[\w-]*)$")]
    private static partial Regex VarReference();

    [GeneratedRegex("[A-Z]")]
    private static partial Regex UpperCase();
}
