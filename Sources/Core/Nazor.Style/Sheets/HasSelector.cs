using Nazor.Rendering;

namespace Nazor.Style;

/// <summary>A selector connected to its originating element by a leading physical-tree relationship.</summary>
/// <param name="Relation">Descendant, Child, NextSibling or SubsequentSibling.</param>
/// <param name="Selector">The selector following the leading relationship.</param>
public sealed record RelativeSelector(Combinator Relation, Selector Selector)
{
    /// <summary>Returns the relative NSS selector, including an explicit leading combinator when needed.</summary>
    /// <returns>The relative selector text.</returns>
    public override string ToString() => (Relation switch
    {
        Combinator.Descendant => "",
        Combinator.Child => "> ",
        Combinator.NextSibling => "+ ",
        Combinator.SubsequentSibling => "~ ",
        _ => throw new InvalidOperationException("Unsupported relative-selector relationship."),
    }) + Selector;
}

/// <summary>Matches when at least one relative selector has a matching element connected to the origin. Nested has selectors and pseudo-elements are unsupported.</summary>
/// <param name="Selectors">The nonempty list of relative selectors.</param>
public sealed record HasSelector(ValueList<RelativeSelector> Selectors) : PseudoSelector
{
    /// <summary>Gets the greatest relative argument specificity without adding a pseudo-class unit.</summary>
    public override Specificity Specificity => Selectors.Items.Select(s => s.Selector.Specificity).Aggregate(Specificity.Zero, Specificity.Max);

    /// <summary>Tests descendant, child or following-sibling relationships from the element.</summary>
    /// <param name="element">The originating element.</param>
    /// <returns>Whether any relative selector matches.</returns>
    public override bool Matches(ElementNode element) => Matches(element, null);

    internal override bool Matches(ElementNode element, Type? scope)
    {
        foreach (var relative in Selectors.Items)
            foreach (var candidate in Candidates(element, relative))
                if (relative.Selector.MatchesRelative(candidate, element, relative.Relation, scope)) return true;
        return false;
    }

    private static IEnumerable<ElementNode> Candidates(ElementNode origin, RelativeSelector relative)
    {
        if (relative.Relation == Combinator.Descendant)
        {
            foreach (var descendant in Descendants(origin)) yield return descendant;
            yield break;
        }
        if (relative.Relation == Combinator.Child)
        {
            if (relative.Selector.Compounds.Count > 1)
            {
                foreach (var descendant in Descendants(origin)) yield return descendant;
            }
            else
                foreach (var child in origin.Children)
                    if (child is ElementNode element) yield return element;
            yield break;
        }
        var found = false;
        foreach (var sibling in Siblings(origin))
        {
            if (ReferenceEquals(sibling, origin)) { found = true; continue; }
            if (!found) continue;
            yield return sibling;
            if (relative.Selector.Compounds.Count > 1)
                foreach (var descendant in Descendants(sibling)) yield return descendant;
            else if (relative.Relation == Combinator.NextSibling) yield break;
        }
    }

    internal static IEnumerable<ElementNode> Descendants(ElementNode origin)
    {
        var stack = new Stack<(ElementNode Parent, int Next)>();
        stack.Push((origin, 0));
        while (stack.TryPop(out var entry))
        {
            var children = entry.Parent.Children;
            for (var i = entry.Next; i < children.Count; i++)
            {
                if (children[i] is not ElementNode child) continue;
                stack.Push((entry.Parent, i + 1));
                stack.Push((child, 0));
                yield return child;
                break;
            }
        }
    }

    /// <summary>Returns the has function with its relative selector list.</summary>
    /// <returns>The NSS selector text.</returns>
    public override string ToString() => $":has({string.Join(", ", Selectors.Items)})";
}
