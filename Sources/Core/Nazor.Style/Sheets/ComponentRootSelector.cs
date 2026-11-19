using Nazor.Rendering;

namespace Nazor.Style;

/// <summary>Matches physical roots declared by the current scoped stylesheet's component instance. Has no global/inline meaning.</summary>
public sealed record ComponentRootSelector : PseudoSelector
{
    /// <summary>Returns false without a component stylesheet scope.</summary>
    /// <param name="element">The physical target.</param>
    /// <returns>False; the style engine supplies scope internally.</returns>
    public override bool Matches(ElementNode element) => false;

    internal override bool Matches(ElementNode element, Type? scope)
    {
        if (scope is null || element.Scope is not { } declared || declared != scope && (!declared.IsGenericType || declared.GetGenericTypeDefinition() != scope)) return false;
        for (var parent = element.Parent; parent is not null; parent = parent.Parent)
        {
            if (parent is ElementNode) return false;
            if (parent is ComponentNode component && ReferenceEquals(component.Component, element.DeclaringComponent)) return true;
        }
        return false;
    }

    /// <summary>Returns the NSS name.</summary>
    /// <returns>The component-root pseudo-class.</returns>
    public override string ToString() => ":component-root";
}
