using Nazor.Rendering;

namespace Nazor.Style;

/// <summary>Matches one explicitly published custom state, with ordinary pseudo-class specificity.</summary>
public sealed record NamedStateSelector : PseudoSelector
{
    /// <summary>Creates a selector for a case-sensitive custom state.</summary>
    /// <param name="name">A valid CustomStateSet identifier.</param>
    /// <exception cref="ArgumentException">name is not a valid state identifier.</exception>
    public NamedStateSelector(string name)
    {
        if (!CustomStateSet.IsValidName(name)) throw new ArgumentException("Expected a custom state identifier.", nameof(name));
        Name = name;
    }

    /// <summary>Gets the case-sensitive custom state name.</summary>
    public string Name { get; }

    /// <summary>Tests the element's typed custom-states snapshot. States are not inherited.</summary>
    /// <param name="element">The physical target.</param>
    /// <returns>Whether the requested state is published.</returns>
    public override bool Matches(ElementNode element) => element.Attributes.GetValueOrDefault("custom-states") is CustomStateSet states && states.Contains(Name);

    /// <summary>Returns the NSS state function.</summary>
    /// <returns>The selector text.</returns>
    public override string ToString() => $":state({Name})";
}
