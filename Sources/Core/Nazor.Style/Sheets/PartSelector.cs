using Nazor.Rendering;

namespace Nazor.Style;

/// <summary>Selects an existing explicitly exposed physical part of a logical component instance; creates no box.</summary>
public sealed record PartSelector : PseudoSelector
{
    /// <summary>Creates a component-qualified public part selector.</summary>
    /// <param name="component">The case-sensitive simple component name.</param>
    /// <param name="name">The case-sensitive public part identifier.</param>
    /// <exception cref="ArgumentException">Either name is not a simple identifier.</exception>
    public PartSelector(string component, string name)
    {
        if (!CustomStateSet.IsValidName(component) || !CustomStateSet.IsValidName(name)) throw new ArgumentException("Expected simple component and part identifiers.");
        Component = component;
        Name = name;
    }

    /// <summary>Gets the exposing component's simple name.</summary>
    public string Component { get; }

    /// <summary>Gets the stable public part name.</summary>
    public string Name { get; }

    /// <summary>Gets the component type plus pseudo-element specificity, (0,0,2).</summary>
    public override Specificity Specificity => new(0, 0, 2);

    /// <summary>Tests explicit declaration and logical instance ownership.</summary>
    /// <param name="element">The existing physical target.</param>
    /// <returns>Whether it is exposed by a matching component.</returns>
    public override bool Matches(ElementNode element) => Matches(element, null);

    internal override bool Matches(ElementNode element, Type? scope) => element.Attributes.GetValueOrDefault("style-parts") is StylePartSet parts && parts.Matches(element, Component, Name, scope);

    /// <summary>Returns the component-qualified part selector.</summary>
    /// <returns>The NSS text.</returns>
    public override string ToString() => $"{Component}::part({Name})";
}
