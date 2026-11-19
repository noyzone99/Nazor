namespace Nazor.Style;

/// <summary>
/// How the style of an element came to be (<see cref="StyleEngine.Trace" />): the rules that select it in cascade
/// order, the declarations of its style attribute, and every computed value with where it comes from. Plain data, for
/// developer tools.
/// </summary>
/// <param name="Rules">The style rules in cascade source order.</param>
/// <param name="Inline">The inline (IReadOnlyList&lt;TracedDeclaration&gt;).</param>
/// <param name="Values">The values (IReadOnlyList&lt;TracedValue&gt;).</param>
/// <param name="CustomProperties">The custom properties (IReadOnlyList&lt;TracedValue&gt;).</param>
public sealed record StyleTrace(IReadOnlyList<TracedRule> Rules, IReadOnlyList<TracedDeclaration> Inline, IReadOnlyList<TracedValue> Values,
    IReadOnlyList<TracedValue> CustomProperties);

/// <summary>
/// A rule whose selector matches the element, at its level of the cascade. A rule whose <c>@media</c> does not match
/// now is listed too, without effect (<see cref="MediaMatches" /> false).
/// </summary>
/// <param name="Level">The level (StyleLevel).</param>
/// <param name="Sheet">The stylesheet to serialize.</param>
/// <param name="Selector">The selector (string).</param>
/// <param name="Specificity">The specificity (Specificity).</param>
/// <param name="Media">The media (string?).</param>
/// <param name="MediaMatches">The media matches (bool).</param>
/// <param name="Order">The order (int).</param>
/// <param name="Declarations">The declarations assigned by the rule or keyframe.</param>
public sealed record TracedRule(StyleLevel Level, string Sheet, string Selector, Specificity Specificity, string? Media, bool MediaMatches, int Order,
    IReadOnlyList<TracedDeclaration> Declarations);

/// <summary>
/// A declaration of a rule or of the style attribute: the longhands it sets (a shorthand with <c>var()</c> sets
/// several) and those for which it is the declaration that wins.
/// </summary>
/// <param name="Property">The style property targeted by the value or declaration.</param>
/// <param name="Value">The numeric value or payload represented by this record.</param>
/// <param name="Longhands">The longhands (IReadOnlyList&lt;string&gt;).</param>
/// <param name="Winning">The winning (IReadOnlyList&lt;string&gt;).</param>
public sealed record TracedDeclaration(string Property, string Value, IReadOnlyList<string> Longhands, IReadOnlyList<string> Winning)
{
    /// <summary>A later declaration (or a more specific rule) sets every longhand of this one.</summary>
    public bool IsOverridden => Winning.Count == 0;
}

/// <summary>Where a computed value comes from.</summary>
public enum ValueSource
{
    /// <summary>A declaration of a rule (<see cref = "TracedValue.Rule"/>).</summary>
    Rule,

    /// <summary>A declaration of the style attribute.</summary>
    Inline,

    /// <summary>Not declared: the value of the parent, for an inherited property.</summary>
    Inherited,

    /// <summary>Not declared: the initial value of the property.</summary>
    Initial,
}

/// <summary>
/// A computed value with its source: the index of the rule in <see cref="StyleTrace.Rules" /> and of the declaration
/// in it (or in <see cref="StyleTrace.Inline" />) that set it.
/// </summary>
/// <param name="Property">The style property targeted by the value or declaration.</param>
/// <param name="Value">The numeric value or payload represented by this record.</param>
/// <param name="Source">The stylesheet source identity or input content.</param>
/// <param name="Rule">The rule (int?).</param>
/// <param name="Declaration">The declaration (int?).</param>
public sealed record TracedValue(string Property, string Value, ValueSource Source, int? Rule = null, int? Declaration = null);
