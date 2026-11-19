namespace Nazor.Rendering;

/// <summary>An immutable, case-sensitive snapshot of explicitly published NSS custom states. Publish it as the custom-states element attribute.</summary>
public sealed class CustomStateSet : IEquatable<CustomStateSet>
{
    private readonly string[] names;

    /// <summary>Creates a state snapshot, removing duplicate names.</summary>
    /// <param name="names">Simple NSS identifiers; names are case-sensitive.</param>
    /// <exception cref="ArgumentNullException">names is null.</exception>
    /// <exception cref="ArgumentException">A name is not a valid custom state identifier.</exception>
    public CustomStateSet(IEnumerable<string> names)
    {
        ArgumentNullException.ThrowIfNull(names);
        this.names = names.Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray();
        if (this.names.Any(name => !IsValidName(name))) throw new ArgumentException("Custom states require simple NSS identifiers.", nameof(names));
        Names = Array.AsReadOnly(this.names);
    }

    /// <summary>Gets the empty state snapshot.</summary>
    public static CustomStateSet Empty { get; } = new([]);

    /// <summary>Gets the sorted, distinct published names.</summary>
    public IReadOnlyList<string> Names { get; }

    /// <summary>Tests a case-sensitive published name.</summary>
    /// <param name="name">The name to test.</param>
    /// <returns>Whether the name is present.</returns>
    public bool Contains(string name) => Array.BinarySearch(names, name, StringComparer.Ordinal) >= 0;

    /// <summary>Returns a snapshot with a state added or removed; the original snapshot is unchanged.</summary>
    /// <param name="name">A simple NSS identifier.</param>
    /// <param name="active">Whether the state should be present.</param>
    /// <returns>This snapshot when unchanged, otherwise a new snapshot.</returns>
    /// <exception cref="ArgumentException">name is not a valid custom state identifier.</exception>
    public CustomStateSet With(string name, bool active = true)
    {
        if (!IsValidName(name)) throw new ArgumentException("Custom states require a simple NSS identifier.", nameof(name));
        if (Contains(name) == active) return this;
        return new(active ? names.Append(name) : names.Where(value => value != name));
    }

    /// <summary>Tests the supported identifier form: a letter or underscore followed by letters, digits, hyphens or underscores.</summary>
    /// <param name="name">The candidate identifier.</param>
    /// <returns>Whether the identifier can be published and used in :state().</returns>
    public static bool IsValidName(string? name) => !string.IsNullOrEmpty(name) && (char.IsLetter(name[0]) || name[0] == '_') &&
        name.All(c => char.IsLetterOrDigit(c) || c is '_' or '-');

    /// <summary>Compares the complete published state snapshot.</summary>
    /// <param name="other">The other snapshot.</param>
    /// <returns>Whether both snapshots publish the same names.</returns>
    public bool Equals(CustomStateSet? other) => other is not null && names.AsSpan().SequenceEqual(other.names);

    /// <summary>Compares the complete published state snapshot.</summary>
    /// <param name="obj">The other value.</param>
    /// <returns>Whether the other value is an equal snapshot.</returns>
    public override bool Equals(object? obj) => obj is CustomStateSet other && Equals(other);

    /// <summary>Hashes the published names.</summary>
    /// <returns>The value hash.</returns>
    public override int GetHashCode()
    {
        var hash = new HashCode();
        foreach (var name in names) hash.Add(name, StringComparer.Ordinal);
        return hash.ToHashCode();
    }

    /// <summary>Returns the names separated by spaces for diagnostics.</summary>
    /// <returns>The published names.</returns>
    public override string ToString() => string.Join(" ", names);
}
