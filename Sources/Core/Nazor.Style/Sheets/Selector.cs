using System.Globalization;
using System.Text;
using Nazor.Rendering;

namespace Nazor.Style;

/// <summary>A generated box or text portion selected relative to an originating element.</summary>
public enum PseudoElement : byte
{
    /// <summary>Selects the originating element itself.</summary>
    None,
    /// <summary>Generates the first decorative child box.</summary>
    Before,
    /// <summary>Generates the last decorative child box.</summary>
    After,
    /// <summary>Selects placeholder text in an editable field.</summary>
    Placeholder,
    /// <summary>Selects the foreground and background of selected editable text.</summary>
    Selection,
}

/// <summary>CSS specificity: identifiers, then classes and pseudo-classes, then types.</summary>
/// <param name="Ids">The ids (int).</param>
/// <param name="Classes">The class names required by the compound selector.</param>
/// <param name="Types">The types (int).</param>
public readonly record struct Specificity(int Ids, int Classes, int Types) : IComparable<Specificity>
{
    /// <summary>The specificity with all components set to zero.</summary>
    public static readonly Specificity Zero = new(0, 0, 0);

    /// <summary>Compares selector specificity lexicographically by identifier, class, and type counts.</summary>
    /// <param name="other">The value to compare with this instance.</param>
    /// <returns>A negative value, zero, or a positive value when this specificity is less than, equal to, or greater
    /// than the other value.</returns>
    public int CompareTo(Specificity other) =>
        Ids != other.Ids ? Ids.CompareTo(other.Ids) : Classes != other.Classes ? Classes.CompareTo(other.Classes) : Types.CompareTo(other.Types);

    /// <summary>Adds the corresponding components of the two operands.</summary>
    /// <param name="a">The a (Specificity).</param>
    /// <param name="b">The blue channel in the range from zero to 255.</param>
    /// <returns>The resulting Specificity value.</returns>
    public static Specificity operator +(Specificity a, Specificity b) => new(a.Ids + b.Ids, a.Classes + b.Classes, a.Types + b.Types);

    /// <summary>Returns the greater of two selector specificities.</summary>
    /// <param name="a">The a (Specificity).</param>
    /// <param name="b">The blue channel in the range from zero to 255.</param>
    /// <returns>The operand with the greater specificity.</returns>
    public static Specificity Max(Specificity a, Specificity b) => a.CompareTo(b) >= 0 ? a : b;

    /// <summary>Returns a textual representation of this value.</summary>
    /// <returns>The textual representation of this value.</returns>
    public override string ToString() => $"{Ids},{Classes},{Types}";
}

/// <summary>Specifies the tree relationship between adjacent compound selectors.</summary>
public enum Combinator : byte
{
    /// <summary>Matches a descendant within the current component scope.</summary>
    Descendant,

    /// <summary>Matches an immediate child.</summary>
    Child,

    /// <summary>Matches the immediately following sibling.</summary>
    NextSibling,

    /// <summary>Matches any following sibling.</summary>
    SubsequentSibling,

    /// <summary>Matches descendants across component scope boundaries.</summary>
    Deep,
}

/// <summary>Specifies how an attribute selector compares the element's attribute value.</summary>
public enum AttributeMatch : byte
{
    /// <summary>Requires the attribute to be present.</summary>
    Exists,

    /// <summary>Requires an exact attribute-value match.</summary>
    Equals,

    /// <summary>Requires a whitespace-separated word to match.</summary>
    Includes,

    /// <summary>Matches the value exactly or as a prefix followed by a hyphen.</summary>
    DashMatch,

    /// <summary>Requires the attribute value to begin with the selector value.</summary>
    Prefix,

    /// <summary>Requires the attribute value to end with the selector value.</summary>
    Suffix,

    /// <summary>Requires the attribute value to contain the selector value.</summary>
    Substring,
}

/// <summary>
/// An attribute selector. The attribute name ignores case, like HTML; an attribute set to null or <c>false</c> is
/// absent (<c>[disabled]</c> matches a disabled control); other values compare as their invariant text, with case
/// unless <see cref="IgnoreCase" /> (the <c>i</c> flag).
/// </summary>
/// <param name="Name">The name used to identify the parameter, property, resource, or registration.</param>
/// <param name="Match">The match (AttributeMatch).</param>
/// <param name="Value">The numeric value or payload represented by this record.</param>
/// <param name="IgnoreCase">The ignore case (bool).</param>
public sealed record AttributeSelector(string Name, AttributeMatch Match, string? Value, bool IgnoreCase = false)
{
    /// <summary>Tests whether the element satisfies this selector.</summary>
    /// <param name="element">The element to inspect or update.</param>
    /// <returns>true if the supplied value satisfies the comparison; otherwise, false.</returns>
    public bool Matches(ElementNode element)
    {
        string? text = null;
        var found = false;
        foreach (var (name, value) in element.AttributeValues)
        {
            if (!string.Equals(name, Name, StringComparison.OrdinalIgnoreCase) || value is null or false) continue;
            text = value switch
            {
                string s => s,
                bool b => b ? "true" : "false",
                IFormattable f => f.ToString(null, CultureInfo.InvariantCulture),
                _ => value.ToString(),
            };
            found = true;
            break;
        }
        if (!found) return false;
        if (Match == AttributeMatch.Exists) return true;
        var comparison = IgnoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        var expected = Value ?? "";
        text ??= "";
        return Match switch
        {
            AttributeMatch.Equals => string.Equals(text, expected, comparison),
            AttributeMatch.Includes => expected.Length > 0 && Includes(text, expected, comparison),
            AttributeMatch.DashMatch => string.Equals(text, expected, comparison) ||
                (text.Length > expected.Length && text[expected.Length] == '-' && text.StartsWith(expected, comparison)),
            AttributeMatch.Prefix => expected.Length > 0 && text.StartsWith(expected, comparison),
            AttributeMatch.Suffix => expected.Length > 0 && text.EndsWith(expected, comparison),
            AttributeMatch.Substring => expected.Length > 0 && text.Contains(expected, comparison),
            _ => false,
        };
    }

    private static bool Includes(string text, string expected, StringComparison comparison)
    {
        foreach (var range in text.AsSpan().SplitAny(" \t\r\n\f"))
            if (text.AsSpan()[range].Equals(expected.AsSpan(), comparison)) return true;
        return false;
    }

    /// <summary>Returns a textual representation of this value.</summary>
    /// <returns>The textual representation of this value.</returns>
    public override string ToString()
    {
        if (Match == AttributeMatch.Exists) return $"[{Name}]";
        var op = Match switch
        {
            AttributeMatch.Includes => "~=",
            AttributeMatch.DashMatch => "|=",
            AttributeMatch.Prefix => "^=",
            AttributeMatch.Suffix => "$=",
            AttributeMatch.Substring => "*=",
            _ => "=",
        };
        return $"[{Name}{op}\"{Value}\"{(IgnoreCase ? " i" : "")}]";
    }
}

/// <summary>A pseudo-class that is not an interaction state: structural or logical.</summary>
public abstract record PseudoSelector
{
    /// <summary>Tests whether the element satisfies this selector.</summary>
    /// <param name="element">The element to inspect or update.</param>
    /// <returns>true if the supplied value satisfies the comparison; otherwise, false.</returns>
    public abstract bool Matches(ElementNode element);

    internal virtual bool Matches(ElementNode element, Type? scope) => Matches(element);

    /// <summary>What the pseudo-class adds to the specificity of its compound (Selectors 4 §16).</summary>
    public virtual Specificity Specificity => new(0, 1, 0);

    internal static IEnumerable<ElementNode> Siblings(ElementNode element) =>
        (element.ParentElement?.Children ?? element.Root?.Nodes ?? []).OfType<ElementNode>();
}

/// <summary>
/// <c>:nth-child(An+B)</c> and its family: the element's 1-based position among its element siblings (or those of the
/// same type), from the end with <see cref="FromEnd" />, is A×n+B for some n ≥ 0. <c>:first-child</c> is (0, 1), <c>:last-child</c>
/// the same from the end.
/// </summary>
/// <param name="A">The step in An+B; zero matches only the B position.</param>
/// <param name="B">The additive offset in the one-based position expression.</param>
/// <param name="FromEnd">Whether positions are counted from the last eligible sibling.</param>
/// <param name="OfType">Whether only siblings of the same element type are counted.</param>
/// <param name="Name">The unfiltered NSS spelling, such as :nth-child(2n+1) or :first-child.</param>
public sealed record NthSelector(int A, int B, bool FromEnd, bool OfType, string Name) : PseudoSelector
{
    /// <summary>Gets or initializes the selector list filtering physical siblings for nth-child/nth-last-child; null counts all siblings.</summary>
    public ValueList<Selector>? Filter { get; init; }

    /// <summary>Adds a pseudo-class unit and the greatest filter specificity, if a filter is supplied.</summary>
    public override Specificity Specificity => new Specificity(0, 1, 0) +
        (Filter?.Items.Select(s => s.Specificity).Aggregate(Specificity.Zero, Specificity.Max) ?? Specificity.Zero);

    /// <summary>Tests whether the element satisfies this selector.</summary>
    /// <param name="element">The element to inspect or update.</param>
    /// <returns>true if the supplied value satisfies the comparison; otherwise, false.</returns>
    public override bool Matches(ElementNode element) => Matches(element, null);

    internal override bool Matches(ElementNode element, Type? scope)
    {
        var count = 0;
        var index = 0;
        foreach (var sibling in Siblings(element))
        {
            if (OfType && !string.Equals(sibling.Name, element.Name, StringComparison.Ordinal)) continue;
            if (Filter is { } filter && !MatchesFilter(filter, sibling, scope)) continue;
            count++;
            if (!ReferenceEquals(sibling, element)) continue;
            index = count;
            if (!FromEnd) break;
        }
        if (index == 0) return false;
        var position = FromEnd ? count - index + 1 : index;
        if (A == 0) return position == B;
        var steps = (long)position - B;
        return steps % A == 0 && steps / A >= 0;
    }

    private static bool MatchesFilter(ValueList<Selector> filter, ElementNode sibling, Type? scope)
    {
        for (var i = 0; i < filter.Items.Count; i++)
            if (scope is null ? filter.Items[i].Matches(sibling) : filter.Items[i].MatchesScoped(sibling, scope)) return true;
        return false;
    }

    /// <summary>Returns a textual representation of this value.</summary>
    /// <returns>The textual representation of this value.</returns>
    public override string ToString() => Filter is null ? Name : $"{Name.TrimEnd(')')} of {string.Join(", ", Filter.Items)})";
}

/// <summary><c>:only-child</c> and <c>:only-of-type</c>.</summary>
/// <param name="OfType">The of type (bool).</param>
public sealed record OnlyChildSelector(bool OfType) : PseudoSelector
{
    /// <summary>Tests whether the element satisfies this selector.</summary>
    /// <param name="element">The element to inspect or update.</param>
    /// <returns>true if the supplied value satisfies the comparison; otherwise, false.</returns>
    public override bool Matches(ElementNode element) =>
        Siblings(element).Count(e => !OfType || string.Equals(e.Name, element.Name, StringComparison.Ordinal)) == 1;

    /// <summary>Returns a textual representation of this value.</summary>
    /// <returns>The textual representation of this value.</returns>
    public override string ToString() => OfType ? ":only-of-type" : ":only-child";
}

/// <summary><c>:empty</c>: no element children and no text.</summary>
public sealed record EmptySelector : PseudoSelector
{
    /// <summary>Tests whether the element satisfies this selector.</summary>
    /// <param name="element">The element to inspect or update.</param>
    /// <returns>true if the supplied value satisfies the comparison; otherwise, false.</returns>
    public override bool Matches(ElementNode element) => element.Children.All(c => c is TextNode { Text.Length: 0 });

    /// <summary>Returns a textual representation of this value.</summary>
    /// <returns>The textual representation of this value.</returns>
    public override string ToString() => ":empty";
}

/// <summary><c>:root</c>: an element without a parent element (the window).</summary>
public sealed record RootSelector : PseudoSelector
{
    /// <summary>Tests whether the element satisfies this selector.</summary>
    /// <param name="element">The element to inspect or update.</param>
    /// <returns>true if the supplied value satisfies the comparison; otherwise, false.</returns>
    public override bool Matches(ElementNode element) => element.ParentElement is null;

    /// <summary>Returns a textual representation of this value.</summary>
    /// <returns>The textual representation of this value.</returns>
    public override string ToString() => ":root";
}

/// <summary>Specifies the matching and specificity behavior of a logical pseudo-class.</summary>
public enum LogicalPseudo : byte
{
    /// <summary>Matches any argument selector and uses the greatest argument specificity.</summary>
    Is,

    /// <summary>Matches when none of the argument selectors match.</summary>
    Not,

    /// <summary>Matches any argument selector with zero specificity.</summary>
    Where,
}

/// <summary>
/// <c>:is()</c>, <c>:not()</c> and <c>:where()</c> with a list of complex selectors about the same element.
/// </summary>
/// <param name="Kind">The logical pseudo represented by this value.</param>
/// <param name="Selectors">The selectors (ValueList&lt;Selector&gt;).</param>
public sealed record LogicalSelector(LogicalPseudo Kind, ValueList<Selector> Selectors) : PseudoSelector
{
    /// <summary>Tests whether the element satisfies this selector.</summary>
    /// <param name="element">The element to inspect or update.</param>
    /// <returns>true if the supplied value satisfies the comparison; otherwise, false.</returns>
    public override bool Matches(ElementNode element) => Matches(element, null);

    internal override bool Matches(ElementNode element, Type? scope)
    {
        var any = false;
        for (var i = 0; i < Selectors.Items.Count; i++)
            if (Selectors.Items[i].MatchesNested(element, scope)) { any = true; break; }
        return Kind == LogicalPseudo.Not ? !any : any;
    }

    /// <summary>Gets the selector's identifier, class, and type precedence counts.</summary>
    public override Specificity Specificity => Kind == LogicalPseudo.Where
        ? Specificity.Zero
        : Selectors.Items.Select(s => s.Specificity).Aggregate(Specificity.Zero, Specificity.Max);

    /// <summary>Returns a textual representation of this value.</summary>
    /// <returns>The textual representation of this value.</returns>
    public override string ToString() => $":{Kind.ToString().ToLowerInvariant()}({string.Join(", ", Selectors.Items)})";
}

/// <summary>
/// Conditions on a single element: type, identifier, classes, interaction states, attributes and other
/// pseudo-classes.
/// </summary>
public sealed class CompoundSelector
{
    /// <summary>Initializes a new instance of CompoundSelector.</summary>
    /// <param name="type">The type (string?).</param>
    /// <param name="id">The diagnostic identifier of the node, or null to clear the current target.</param>
    /// <param name="classes">The class names required by the compound selector.</param>
    /// <param name="states">The element interaction state flags required by the selector.</param>
    public CompoundSelector(string? type, string? id, IReadOnlyList<string> classes, ElementStates states)
        : this(type, id, classes, states, [], [])
    {
    }

    /// <summary>Initializes a new instance of CompoundSelector.</summary>
    /// <param name="type">The type (string?).</param>
    /// <param name="id">The diagnostic identifier of the node, or null to clear the current target.</param>
    /// <param name="classes">The class names required by the compound selector.</param>
    /// <param name="states">The element interaction state flags required by the selector.</param>
    /// <param name="attributes">The attributes or selector constraints to apply.</param>
    /// <param name="pseudos">The structural or logical pseudo-selectors to match.</param>
    /// <param name="pseudoElement">The terminal pseudo-element target.</param>
    public CompoundSelector(string? type, string? id, IReadOnlyList<string> classes, ElementStates states,
        IReadOnlyList<AttributeSelector> attributes, IReadOnlyList<PseudoSelector> pseudos, PseudoElement pseudoElement = PseudoElement.None)
    {
        Type = type;
        Id = id;
        Classes = classes;
        States = states;
        Attributes = attributes;
        Pseudos = pseudos;
        PseudoElement = pseudoElement;
    }

    /// <summary>Element name, or null for the universal selector.</summary>
    public string? Type { get; }

    /// <summary>Gets the required element identifier, or null when no identifier constraint is present.</summary>
    public string? Id { get; }

    /// <summary>Gets the class names used by the element or selector.</summary>
    public IReadOnlyList<string> Classes { get; }

    /// <summary>States that must all be set (pseudo-classes such as <c>:hover</c>).</summary>
    public ElementStates States { get; }

    /// <summary>Gets the element attributes or attribute-selector constraints.</summary>
    public IReadOnlyList<AttributeSelector> Attributes { get; }

    /// <summary>Structural and logical pseudo-classes (<c>:first-child</c>, <c>:not()</c>…).</summary>
    public IReadOnlyList<PseudoSelector> Pseudos { get; }

    /// <summary>Gets the terminal pseudo-element target, or <see cref="Style.PseudoElement.None" />.</summary>
    public PseudoElement PseudoElement { get; }

    /// <summary>Gets the selector's identifier, class, and type precedence counts.</summary>
    public Specificity Specificity =>
        new Specificity(Id is null ? 0 : 1, Classes.Count + Attributes.Count + PseudoClasses.Names.Count(p => (States & p.State) != 0), Type is null ? 0 : 1) +
        Pseudos.Aggregate(Specificity.Zero, (sum, pseudo) => sum + pseudo.Specificity) +
        new Specificity(0, 0, PseudoElement == PseudoElement.None ? 0 : 1);

    /// <summary>Tests whether the element satisfies this selector.</summary>
    /// <param name="element">The element to inspect or update.</param>
    /// <returns>true if the supplied value satisfies the comparison; otherwise, false.</returns>
    public bool Matches(ElementNode element) => Matches(element, null);

    internal bool Matches(ElementNode element, Type? scope)
    {
        if (!MatchesIdentity(element)) return false;
        var states = element.IsDisabled ? element.States | ElementStates.Disabled : element.States;
        if ((states & States) != States) return false;
        for (var i = 0; i < Attributes.Count; i++)
            if (!Attributes[i].Matches(element)) return false;
        for (var i = 0; i < Pseudos.Count; i++)
            if (!Pseudos[i].Matches(element, scope)) return false;
        return true;
    }

    internal bool MatchesIdentity(ElementNode element)
    {
        if (Type is not null && !string.Equals(element.Name, Type, StringComparison.Ordinal)) return false;
        if (Id is not null && !string.Equals(element.Id, Id, StringComparison.Ordinal)) return false;
        if (Classes.Count > 0)
        {
            var classAttribute = element.Class;
            if (classAttribute is null) return false;
            for (var i = 0; i < Classes.Count; i++)
                if (!HasClass(classAttribute, Classes[i])) return false;
        }
        return true;
    }

    private static bool HasClass(string classAttribute, string name)
    {
        foreach (var range in classAttribute.AsSpan().SplitAny(" \t\r\n\f"))
            if (classAttribute.AsSpan()[range].SequenceEqual(name)) return true;
        return false;
    }

    /// <summary>Returns a textual representation of this value.</summary>
    /// <returns>The textual representation of this value.</returns>
    public override string ToString()
    {
        var empty = Id is null && Classes.Count == 0 && States == ElementStates.None && Attributes.Count == 0 && Pseudos.Count == 0;
        var part = Pseudos.OfType<PartSelector>().FirstOrDefault();
        var builder = new StringBuilder(part?.ToString() ?? Type ?? (empty ? "*" : ""));
        if (Id is not null) builder.Append('#').Append(Id);
        foreach (var name in Classes) builder.Append('.').Append(name);
        foreach (var attribute in Attributes) builder.Append(attribute);
        foreach (var (state, pseudo) in PseudoClasses.Names)
            if ((States & state) != 0) builder.Append(':').Append(pseudo);
        foreach (var pseudo in Pseudos)
            if (pseudo is not PartSelector) builder.Append(pseudo);
        if (PseudoElement != PseudoElement.None) builder.Append("::").Append(PseudoElement.ToString().ToLowerInvariant());
        return builder.ToString();
    }
}

/// <summary>Pseudo-classes supported by NSS: the interaction states they test, and the others by name.</summary>
public static class PseudoClasses
{
    /// <summary>The supported interaction pseudo-class names mapped to their element state flags.</summary>
    public static readonly IReadOnlyList<(ElementStates State, string Name)> Names =
    [
        (ElementStates.Hover, "hover"),
        (ElementStates.Pressed, "active"),
        (ElementStates.Focus, "focus"),
        (ElementStates.FocusVisible, "focus-visible"),
        (ElementStates.FocusWithin, "focus-within"),
        (ElementStates.Disabled, "disabled"),
        (ElementStates.Checked, "checked"),
        (ElementStates.WheelScrolling, "wheel-scrolling"),
        (ElementStates.PopupAbove, "popup-above"),
        (ElementStates.PopupLeft, "popup-left"),
        (ElementStates.PopupRight, "popup-right"),
        (ElementStates.PopupOriginRight, "popup-origin-right"),
        (ElementStates.PopupOriginBottom, "popup-origin-bottom"),
        (ElementStates.WindowActive, "window-active"),
        (ElementStates.WindowInactive, "window-inactive"),
    ];

    /// <summary>The structural pseudo-classes without arguments.</summary>
    public static readonly IReadOnlyList<string> Structural =
        ["first-child", "last-child", "only-child", "first-of-type", "last-of-type", "only-of-type", "empty", "root", "component-root", "first-item", "last-item"];

    /// <summary>The pseudo-classes with arguments, written with their parenthesis.</summary>
    public static readonly IReadOnlyList<string> Functional =
        ["not", "is", "where", "has", "state", "nth-item", "nth-child", "nth-last-child", "nth-of-type", "nth-last-of-type"];

    /// <summary>Every name, for editors: interaction/control states, structural and functional pseudo-classes.</summary>
    public static IEnumerable<string> All => Names.Select(n => n.Name).Concat(Structural).Concat(ControlStateSelector.Names).Concat(Functional);

    /// <summary>Resolves a pseudo-class name to its corresponding element interaction state.</summary>
    /// <param name="name">The name used to identify the parameter, property, resource, or registration.</param>
    /// <param name="state">When this method returns, contains the state produced by the operation.</param>
    /// <returns>true when the operation succeeds or the condition is satisfied; otherwise, false.</returns>
    public static bool TryGet(string name, out ElementStates state)
    {
        foreach (var (candidate, pseudo) in Names)
        {
            if (string.Equals(pseudo, name, StringComparison.OrdinalIgnoreCase))
            {
                state = candidate;
                return true;
            }
        }
        state = ElementStates.None;
        return false;
    }

    /// <summary>A structural or derived control-state pseudo-class without arguments, or null.</summary>
    /// <param name="name">The name used to identify the parameter, property, resource, or registration.</param>
    /// <returns>The resulting PseudoSelector? value.</returns>
    public static PseudoSelector? Structure(string name) => name.ToLowerInvariant() switch
    {
        "first-child" => new NthSelector(0, 1, false, false, ":first-child"),
        "last-child" => new NthSelector(0, 1, true, false, ":last-child"),
        "first-of-type" => new NthSelector(0, 1, false, true, ":first-of-type"),
        "last-of-type" => new NthSelector(0, 1, true, true, ":last-of-type"),
        "only-child" => new OnlyChildSelector(false),
        "only-of-type" => new OnlyChildSelector(true),
        "empty" => new EmptySelector(),
        "root" => new RootSelector(),
        "component-root" => new ComponentRootSelector(),
        "first-item" => new ItemPositionSelector(0, 1, false, ":first-item"),
        "last-item" => new ItemPositionSelector(0, 1, true, ":last-item"),
        var control when ControlStateSelector.Names.Contains(control, StringComparer.Ordinal) => new ControlStateSelector(control),
        _ => null,
    };
}

/// <summary>
/// A complex selector: compounds from left to right joined by combinators. Matching starts from the subject (the last
/// compound), physical ancestors/siblings and explicit logical component qualifiers.
/// </summary>
public sealed class Selector : IEquatable<Selector>
{
    private readonly int deepIndex;
    /// <summary>Initializes a new instance of Selector.</summary>
    /// <param name="compounds">The compound selectors in matching order.</param>
    /// <param name="combinators">The relationships between successive compound selectors.</param>
    public Selector(IReadOnlyList<CompoundSelector> compounds, IReadOnlyList<Combinator> combinators)
    {
        if (compounds.Count == 0 || combinators.Count != compounds.Count - 1)
            throw new ArgumentException("A selector needs one combinator between each pair of compounds.");
        Compounds = compounds;
        Combinators = combinators;
        deepIndex = -1;
        for (var i = 0; i < combinators.Count; i++)
            if (combinators[i] == Combinator.Deep) { deepIndex = i; break; }
        Specificity = compounds.Aggregate(Specificity.Zero, (sum, compound) => sum + compound.Specificity);
    }

    /// <summary>Gets the compound selectors in matching order.</summary>
    public IReadOnlyList<CompoundSelector> Compounds { get; }

    /// <summary>Gets the relationships between successive compound selectors.</summary>
    public IReadOnlyList<Combinator> Combinators { get; }

    /// <summary>Gets the selector's identifier, class, and type precedence counts.</summary>
    public Specificity Specificity { get; }

    /// <summary>Gets the final compound selector describing the element being matched.</summary>
    public CompoundSelector Subject => Compounds[^1];

    /// <summary>
    /// Whether matching depends on siblings or on the presence of children (structural pseudo-classes, <c>+</c>, <c>~</c>).
    /// </summary>
    public bool IsStructural =>
        Combinators.Any(c => c is Combinator.NextSibling or Combinator.SubsequentSibling) ||
        Compounds.Any(c => c.Pseudos.Any(p => p is LogicalSelector logical ? logical.Selectors.Items.Any(s => s.IsStructural) : p is not RootSelector));

    internal bool HasSiblingDependencies => Combinators.Any(c => c is Combinator.NextSibling or Combinator.SubsequentSibling) ||
        Compounds.Any(c => c.Pseudos.Any(p => p is NthSelector { Filter: not null } ||
            p is LogicalSelector logical && logical.Selectors.Items.Any(s => s.HasSiblingDependencies)));

    internal bool ObservesText => Compounds.Any(c => c.Pseudos.Any(p => p is EmptySelector ||
        p is LogicalSelector logical && logical.Selectors.Items.Any(s => s.ObservesText) ||
        p is NthSelector { Filter: { } filter } && filter.Items.Any(s => s.ObservesText) ||
        p is HasSelector has && has.Selectors.Items.Any(s => s.Selector.ObservesText)));

    internal bool ContainsHas => Compounds.Any(c => c.Pseudos.Any(p => p is HasSelector ||
        p is LogicalSelector logical && logical.Selectors.Items.Any(s => s.ContainsHas) ||
        p is NthSelector { Filter: { } filter } && filter.Items.Any(s => s.ContainsHas)));

    /// <summary>Gets whether this selector contains a component-boundary-crossing deep combinator.</summary>
    public bool HasDeep => deepIndex >= 0;

    /// <summary>Tests whether the element satisfies this selector.</summary>
    /// <param name="element">The element to inspect or update.</param>
    /// <returns>true if the supplied value satisfies the comparison; otherwise, false.</returns>
    public bool Matches(ElementNode element) => MatchesTarget(element, PseudoElement.None);

    internal bool MatchesTarget(ElementNode element, PseudoElement target) =>
        Subject.PseudoElement == target && Matches(element, Compounds.Count - 1, null, element, true, false);

    internal bool MatchesNested(ElementNode element, Type? scope) =>
        Subject.PseudoElement == PseudoElement.None && Matches(element, Compounds.Count - 1, scope, element, true, false);

    internal bool MatchesRelative(ElementNode element, ElementNode anchor, Combinator relation, Type? scope) =>
        Subject.PseudoElement == PseudoElement.None && Matches(element, Compounds.Count - 1, scope, element,
            scope is null || SameScope(element.Scope, scope), false, anchor, relation);

    internal bool MatchesScoped(ElementNode element, Type scope, PseudoElement target = PseudoElement.None) =>
        Subject.PseudoElement == target && Matches(element, Compounds.Count - 1, scope, element, SameScope(element.Scope, scope), false);

    private bool Matches(Node node, int index, Type? scope, Node segment, bool inScope, bool deep,
        ElementNode? anchor = null, Combinator relation = Combinator.Descendant)
    {
        var compound = Compounds[index];
        if (node is ElementNode element)
        {
            if (!compound.Matches(element, deepIndex >= 0 && deepIndex < index ? null : scope)) return false;
            if (compound.Pseudos.Any(p => p is PartSelector)) inScope = true;
            if (deep) { segment = element; inScope = scope is null || SameScope(element.Scope, scope); }
        }
        else if (node is ComponentNode component)
        {

            if (compound.Type != component.StyleName || compound.Id is not null || compound.Classes.Count != 0 ||
                compound.Attributes.Count != 0 || compound.Pseudos.Count != 0 || compound.States != ElementStates.None ||
                component.HasNamedElement || !deep && !DeclaredBy(segment, component.Component)) return false;
            if (deep) inScope = scope is null;
            inScope |= scope is null || SameScope(component.DeclaringScope, scope);
            segment = component;
        }
        else return false;
        if (index == 0) return inScope && (anchor is null || Related(anchor, node, relation));
        switch (Combinators[index - 1])
        {
            case Combinator.Child:
                return node.ParentElement is { } parent && Matches(parent, index - 1, scope, segment, inScope, false, anchor, relation);
            case Combinator.NextSibling:
                return node is ElementNode next && Previous(next).FirstOrDefault() is { } previous && Matches(previous, index - 1, scope, segment, inScope, false, anchor, relation);
            case Combinator.SubsequentSibling:
                if (node is not ElementNode following) return false;
                foreach (var sibling in Previous(following))
                    if (Matches(sibling, index - 1, scope, segment, inScope, false, anchor, relation)) return true;
                return false;
            default:
                var crosses = Combinators[index - 1] == Combinator.Deep;
                for (var ancestor = node.Parent; ancestor is not null; ancestor = ancestor.Parent)
                    if (Matches(ancestor, index - 1, scope, segment, inScope, crosses, anchor, relation)) return true;
                return false;
        }
    }

    private static bool Related(ElementNode anchor, Node node, Combinator relation)
    {
        if (relation == Combinator.Child) return ReferenceEquals(node.ParentElement, anchor);
        if (relation == Combinator.Descendant)
        {
            for (var parent = node.Parent; parent is not null; parent = parent.Parent)
                if (ReferenceEquals(parent, anchor)) return true;
            return false;
        }
        if (node is not ElementNode element || !ReferenceEquals(element.ParentElement, anchor.ParentElement)) return false;
        ElementNode? previous = null;
        var found = false;
        foreach (var sibling in PseudoSelector.Siblings(element))
        {
            if (ReferenceEquals(sibling, element)) return relation == Combinator.NextSibling ? ReferenceEquals(previous, anchor) : found;
            if (ReferenceEquals(sibling, anchor)) found = true;
            previous = sibling;
        }
        return false;
    }

    private static bool SameScope(Type? first, Type second) => first is not null &&
        (first.IsGenericType ? first.GetGenericTypeDefinition() : first) == (second.IsGenericType ? second.GetGenericTypeDefinition() : second);

    private static bool DeclaredBy(Node node, IComponent component) => node switch
    {
        ElementNode element => ReferenceEquals(element.DeclaringComponent, component) || ReferenceEquals(element.Owner, component),
        ComponentNode child => ReferenceEquals(child.DeclaringComponent, component),
        _ => false,
    };

    private static IEnumerable<ElementNode> Previous(ElementNode element)
    {
        var siblings = element.ParentElement?.Children ?? element.Root?.Nodes ?? [];
        var index = 0;
        while (index < siblings.Count && !ReferenceEquals(siblings[index], element)) index++;
        if (index == siblings.Count) yield break;
        for (var i = index - 1; i >= 0; i--)
            if (siblings[i] is ElementNode previous) yield return previous;
    }

    /// <summary>Determines whether the supplied value is equal to this instance.</summary>
    /// <param name="other">The value to compare with this instance.</param>
    /// <returns>true if the supplied value satisfies the comparison; otherwise, false.</returns>
    public bool Equals(Selector? other) => other is not null && ToString() == other.ToString();

    /// <summary>Determines whether the supplied value is equal to this instance.</summary>
    /// <param name="obj">The value to compare with this instance.</param>
    /// <returns>true if the supplied value satisfies the comparison; otherwise, false.</returns>
    public override bool Equals(object? obj) => obj is Selector other && Equals(other);

    /// <summary>Returns a hash code consistent with equality for this value.</summary>
    /// <returns>The hash code for this value.</returns>
    public override int GetHashCode() => ToString().GetHashCode(StringComparison.Ordinal);

    /// <summary>Returns a textual representation of this value.</summary>
    /// <returns>The textual representation of this value.</returns>
    public override string ToString()
    {
        var builder = new StringBuilder(Compounds[0].ToString());
        for (var i = 1; i < Compounds.Count; i++)
        {
            builder.Append(Combinators[i - 1] switch
            {
                Combinator.Child => " > ",
                Combinator.NextSibling => " + ",
                Combinator.SubsequentSibling => " ~ ",
                Combinator.Deep => " ::deep ",
                _ => " ",
            });
            builder.Append(Compounds[i]);
        }
        return builder.ToString();
    }
}
