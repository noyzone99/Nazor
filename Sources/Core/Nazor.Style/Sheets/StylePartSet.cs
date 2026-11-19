using Nazor.Rendering;

namespace Nazor.Style;

/// <summary>An immutable declaration of public styling parts on a physical element. Bindings retain explicit component instance ownership.</summary>
public sealed class StylePartSet : IEquatable<StylePartSet>
{
    private sealed record Binding(IComponent Owner, IComponent Source, string Name)
    {
        public bool Equals(Binding? other) => other is not null && ReferenceEquals(Owner, other.Owner) && ReferenceEquals(Source, other.Source) && Name == other.Name;
        public override int GetHashCode() => HashCode.Combine(System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(Owner), System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(Source), Name);
    }
    private readonly Binding[] bindings;
    private StylePartSet(Binding[] bindings) => this.bindings = bindings;

    /// <summary>Gets the empty declaration.</summary>
    public static StylePartSet Empty { get; } = new([]);

    /// <summary>Exposes this element under a stable name owned by its declaring component.</summary>
    /// <param name="owner">The declaring component instance.</param>
    /// <param name="name">A case-sensitive simple NSS identifier.</param>
    /// <returns>An immutable declaration including the part.</returns>
    /// <exception cref="ArgumentNullException">owner is null.</exception>
    /// <exception cref="ArgumentException">name is invalid.</exception>
    public StylePartSet Expose(IComponent owner, string name)
    {
        ArgumentNullException.ThrowIfNull(owner);
        if (!CustomStateSet.IsValidName(name)) throw new ArgumentException("Expected a simple part identifier.", nameof(name));
        return Add(new Binding(owner, owner, name));
    }

    /// <summary>Explicitly forwards an already exposed local part to names and owners requested by an enclosing component. Original declaration ownership is retained.</summary>
    /// <param name="owner">The component exposing the local part.</param>
    /// <param name="name">The existing local part name.</param>
    /// <param name="exports">Requested public names created by enclosing components with Expose.</param>
    /// <returns>A declaration including the forwarded bindings.</returns>
    /// <exception cref="ArgumentNullException">owner or exports is null.</exception>
    /// <exception cref="ArgumentException">The local part has not been exposed.</exception>
    public StylePartSet Export(IComponent owner, string name, StylePartSet exports)
    {
        ArgumentNullException.ThrowIfNull(owner);
        ArgumentNullException.ThrowIfNull(exports);
        var source = bindings.FirstOrDefault(b => ReferenceEquals(b.Owner, owner) && b.Name == name)
            ?? throw new ArgumentException("The local part must be exposed before forwarding it.", nameof(name));
        var result = this;
        foreach (var export in exports.bindings) result = result.Add(new Binding(export.Owner, source.Source, export.Name));
        return result;
    }

    private StylePartSet Add(Binding binding) => bindings.Contains(binding) ? this : new([.. bindings, binding]);

    internal bool Matches(ElementNode element, string componentName, string name, Type? scope)
    {
        foreach (var binding in bindings)
        {
            if (binding.Name != name || !ReferenceEquals(element.DeclaringComponent, binding.Source) && !ReferenceEquals(element.Owner, binding.Source)) continue;
            var sourceSeen = false;
            for (var parent = element.Parent; parent is not null; parent = parent.Parent)
            {
                if (parent is not ComponentNode component) continue;
                sourceSeen |= ReferenceEquals(component.Component, binding.Source);
                if (!sourceSeen || !ReferenceEquals(component.Component, binding.Owner) || component.StyleName != componentName) continue;
                if (scope is null || SameScope(component.DeclaringScope, scope) || SameScope(component.Component.GetType(), scope)) return true;
            }
        }
        return false;

        static bool SameScope(Type? actual, Type expected) => actual == expected || actual?.IsGenericType == true && actual.GetGenericTypeDefinition() == expected;
    }

    /// <summary>Compares the ordered explicit instance/name bindings.</summary>
    /// <param name="other">The other declaration.</param>
    /// <returns>Whether the declarations are equal.</returns>
    public bool Equals(StylePartSet? other) => other is not null && bindings.AsSpan().SequenceEqual(other.bindings);

    /// <summary>Compares part declarations.</summary>
    /// <param name="obj">The other value.</param>
    /// <returns>Whether it is an equal declaration.</returns>
    public override bool Equals(object? obj) => obj is StylePartSet other && Equals(other);

    /// <summary>Hashes the ordered explicit bindings.</summary>
    /// <returns>The value hash.</returns>
    public override int GetHashCode()
    {
        var hash = new HashCode();
        foreach (var binding in bindings) hash.Add(binding);
        return hash.ToHashCode();
    }
}
