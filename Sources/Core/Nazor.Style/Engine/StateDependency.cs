using Nazor.Rendering;

namespace Nazor.Style;

internal sealed record StateDependency(ElementStates States, string? Type, string? Id, IReadOnlyList<string> Classes)
{
    private const string Spaces = " \t\r\n\f";

    public static IReadOnlyList<StateDependency> Of(IEnumerable<StyleRule> rules)
    {
        var dependencies = new List<StateDependency>();
        foreach (var rule in rules) Add(rule.Selector, subject: true);
        return dependencies;

        void Add(Selector selector, bool subject)
        {
            for (var i = 0; i < selector.Compounds.Count; i++)
            {
                var compound = selector.Compounds[i];

                if (compound.States != ElementStates.None && (!subject || i < selector.Compounds.Count - 1))
                    dependencies.Add(new(compound.States, compound.Type, compound.Id, compound.Classes));

                foreach (var pseudo in compound.Pseudos)
                    if (pseudo is LogicalSelector logical)
                        foreach (var inner in logical.Selectors.Items) Add(inner, subject: false);
                    else if (pseudo is NthSelector { Filter: { } filter })
                        foreach (var inner in filter.Items) Add(inner, subject: false);
                    else if (pseudo is ControlStateSelector control && control.Dependencies != ElementStates.None)
                        dependencies.Add(new(control.Dependencies, null, null, []));
            }
        }
    }

    public bool Concerns(ElementNode element, ElementStates changed)
    {
        if ((States & changed) == 0) return false;
        if (Type is not null && !string.Equals(Type, element.Name, StringComparison.Ordinal)) return false;
        if (Id is not null && !string.Equals(Id, element.Id, StringComparison.Ordinal)) return false;
        if (Classes.Count == 0) return true;
        if (element.Class is not { } attribute) return false;
        foreach (var name in Classes)
        {
            var found = false;
            foreach (var range in attribute.AsSpan().SplitAny(Spaces))
            {
                if (!attribute.AsSpan()[range].SequenceEqual(name)) continue;
                found = true;
                break;
            }
            if (!found) return false;
        }
        return true;
    }
}
