using Nazor.Rendering;

namespace Nazor.Style;

internal sealed class HasDependency
{
    private readonly CompoundSelector origin;
    private readonly HashSet<string> attributes = new(StringComparer.OrdinalIgnoreCase);
    private ElementStates states;
    private bool anyAttribute, following, allFollowing, siblingPositions, observesAncestors;
    private readonly bool observesText;
    public bool AffectsSiblings { get; }

    private HasDependency(CompoundSelector origin, HasSelector has, bool affectsSiblings)
    {
        this.origin = origin;
        AffectsSiblings = affectsSiblings;
        foreach (var relative in has.Selectors.Items)
        {
            var sibling = relative.Relation is Combinator.NextSibling or Combinator.SubsequentSibling;
            following |= sibling;
            allFollowing |= relative.Relation == Combinator.SubsequentSibling || sibling && relative.Selector.Compounds.Count > 1;
            siblingPositions |= sibling && relative.Selector.HasSiblingDependencies;
            observesText |= relative.Selector.ObservesText;
            Collect(relative.Selector);
        }
    }

    private void Collect(Selector selector)
    {
        foreach (var compound in selector.Compounds)
        {
            if (compound.Id is not null) attributes.Add("id");
            if (compound.Classes.Count != 0) attributes.Add("class");
            foreach (var attribute in compound.Attributes) attributes.Add(attribute.Name);
            states |= compound.States;
            if ((compound.States & ElementStates.Disabled) != 0) { attributes.Add("disabled"); observesAncestors = true; }
            foreach (var pseudo in compound.Pseudos)
            {
                switch (pseudo)
                {
                    case NamedStateSelector:
                        attributes.Add("custom-states");
                        break;
                    case ItemPositionSelector:
                        attributes.Add("item-position");
                        break;
                    case ControlStateSelector control:
                        states |= control.Dependencies;
                        attributes.UnionWith(control.AttributeDependencies);
                        observesAncestors |= control.Name is "enabled" or "read-only" or "read-write" or "valid" or "invalid" or "user-valid" or "user-invalid" or "required" or "optional";
                        break;
                    case LogicalSelector logical:
                        foreach (var inner in logical.Selectors.Items) { observesAncestors |= inner.Compounds.Count > 1; Collect(inner); }
                        break;
                    case NthSelector { Filter: { } filter }:
                        foreach (var inner in filter.Items) { observesAncestors |= inner.Compounds.Count > 1; Collect(inner); }
                        break;
                    case NthSelector or OnlyChildSelector or EmptySelector or RootSelector or ComponentRootSelector:
                        break;
                    default:
                        anyAttribute = true;
                        break;
                }
            }
        }
    }

    public bool Relevant(string? attribute, ElementStates changed, bool text) => text ? observesText :
        attribute is not null ? anyAttribute || attributes.Contains(attribute) : changed == ElementStates.None || (states & changed) != 0;

    public IEnumerable<ElementNode> AffectedOrigins(ElementNode changed, bool structure)
    {
        for (var current = changed; current is not null; current = current.ParentElement)
        {
            if (origin.MatchesIdentity(current)) yield return current;
            if (!following) continue;
            ElementNode? previous = null;
            foreach (var sibling in PseudoSelector.Siblings(current))
            {
                if (!siblingPositions && ReferenceEquals(sibling, current)) break;
                if (siblingPositions || allFollowing)
                {
                    if (origin.MatchesIdentity(sibling)) yield return sibling;
                }
                else previous = sibling;
            }
            if (!siblingPositions && !allFollowing && previous is not null && origin.MatchesIdentity(previous)) yield return previous;
        }
        if (structure && following)
            foreach (var child in changed.Children)
                if (child is ElementNode element && origin.MatchesIdentity(element)) yield return element;
        if (observesAncestors)
            foreach (var descendant in HasSelector.Descendants(changed.ParentElement ?? changed))
                if (origin.MatchesIdentity(descendant)) yield return descendant;
    }

    public static IReadOnlyList<HasDependency> Of(IEnumerable<StyleRule> rules)
    {
        var dependencies = new List<HasDependency>();
        foreach (var rule in rules) Add(rule.Selector, false);
        return dependencies;

        void Add(Selector selector, bool siblings)
        {
            for (var i = 0; i < selector.Compounds.Count; i++)
            {
                var affectsSiblings = i == selector.Compounds.Count - 1 ? siblings : selector.Combinators[i] is Combinator.NextSibling or Combinator.SubsequentSibling;
                var compound = selector.Compounds[i];
                foreach (var pseudo in compound.Pseudos)
                    if (pseudo is HasSelector has) dependencies.Add(new HasDependency(compound, has, affectsSiblings));
                    else if (pseudo is LogicalSelector logical)
                        foreach (var inner in logical.Selectors.Items) Add(inner, affectsSiblings);
                    else if (pseudo is NthSelector { Filter: { } filter })
                        foreach (var inner in filter.Items) Add(inner, true);
            }
        }
    }
}
