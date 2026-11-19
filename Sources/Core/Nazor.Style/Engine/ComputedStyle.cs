using Nazor.Style.Syntax;

namespace Nazor.Style;

/// <summary>
/// The computed values of an element: one value per property in <see cref="StyleProperties.All" />, plus the
/// inherited custom properties (already substituted). Percentages are kept, because their basis is known only to
/// layout; <c>currentColor</c> is resolved, and so are em, rem and viewport units.
/// </summary>
public sealed class ComputedStyle
{
    private static readonly IReadOnlyDictionary<string, IReadOnlyList<StyleToken>> NoCustomProperties =
        new Dictionary<string, IReadOnlyList<StyleToken>>(StringComparer.Ordinal);

    private static readonly StyleProperty[] InheritedProperties = [.. StyleProperties.All.Where(p => p.Inherited)];

    private readonly object[] values;
    private readonly int overrideId;
    private readonly object? overrideValue;

    internal ComputedStyle(object[] values, IReadOnlyDictionary<string, IReadOnlyList<StyleToken>> customProperties, double rootFontSize, int overrideId = -1, object? overrideValue = null)
    {
        this.values = values;
        CustomProperties = customProperties;
        RootFontSize = rootFontSize;
        this.overrideId = overrideId;
        this.overrideValue = overrideValue;
    }

    internal static ComputedStyle Create(object[] values, IReadOnlyDictionary<string, IReadOnlyList<StyleToken>> customProperties, double rootFontSize, ComputedStyle? previous)
    {
        var changed = -1;
        if (previous is not null)
        {
            for (var i = 0; i < values.Length; i++)
            {
                if (Equals(values[i], previous.values[i])) continue;
                if (changed >= 0) return new((object[])values.Clone(), customProperties, rootFontSize);
                changed = i;
            }
            return new(previous.values, customProperties, rootFontSize, changed, changed < 0 ? null : values[changed]);
        }
        return new((object[])values.Clone(), customProperties, rootFontSize);
    }

    private object ValueAt(int id) => id == overrideId ? overrideValue! : values[id];

    /// <summary>
    /// Every property at its initial value (currentColor resolved to the initial color), with no custom properties.
    /// </summary>
    public static ComputedStyle Initial { get; } = new(StyleProperties.All
        .Select(p => p.InitialValue is Color { IsCurrentColor: true } ? StyleProperties.Color.Initial : p.InitialValue).ToArray(), NoCustomProperties,
        StyleProperties.FontSize.Initial.Value);

    /// <summary>Gets the cascaded custom-property token sequences indexed by their names.</summary>
    public IReadOnlyDictionary<string, IReadOnlyList<StyleToken>> CustomProperties { get; }

    internal ComputedStyle WithCustomProperties(IReadOnlyDictionary<string, IReadOnlyList<StyleToken>> customProperties) =>
        new(values, customProperties, RootFontSize, overrideId, overrideValue);

    /// <summary>Gets the computed value for the specified style property.</summary>
    /// <param name="property">The style property to read or update.</param>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <returns>The computed property value in its declared CLR representation.</returns>
    public T Get<T>(StyleProperty<T> property) where T : notnull => (T)ValueAt(property.Id);

    /// <summary>Gets the computed value for the specified style property.</summary>
    /// <param name="property">The style property to read or update.</param>
    /// <returns>The computed property value in its declared CLR representation.</returns>
    public object Get(StyleProperty property) => ValueAt(property.Id);

    /// <summary>Gets the computed layout display mode.</summary>
    public Display Display => Get(StyleProperties.Display);

    /// <summary>Gets the computed foreground color.</summary>
    public Color Color => Get(StyleProperties.Color);

    /// <summary>Gets the computed background color.</summary>
    public Color BackgroundColor => Get(StyleProperties.BackgroundColor);

    /// <summary>Gets the computed opacity multiplier.</summary>
    public double Opacity => Get(StyleProperties.Opacity);

    /// <summary>The computed font size in px, the basis of em.</summary>
    public double FontSize => Get(StyleProperties.FontSize).Value;

    /// <summary>The font size of the root element in px, the basis of rem.</summary>
    public double RootFontSize { get; }

    /// <summary>Union of the effects of the properties whose values differ.</summary>
    /// <param name="other">The value to compare with this instance.</param>
    /// <returns>The resulting StyleEffects value.</returns>
    public StyleEffects Compare(ComputedStyle other)
    {
        var effects = StyleEffects.None;
        for (var i = 0; i < StyleProperties.All.Count; i++)
        {
            var property = StyleProperties.All[i];
            if (!Equals(ValueAt(property.Id), other.ValueAt(property.Id))) effects |= property.Effects == StyleEffects.None ? StyleEffects.Paint : property.Effects;
        }
        return effects;
    }

    internal bool CustomPropertiesEqual(ComputedStyle other)
    {
        if (ReferenceEquals(CustomProperties, other.CustomProperties)) return true;
        if (CustomProperties.Count != other.CustomProperties.Count) return false;
        foreach (var (name, value) in CustomProperties)
            if (!other.CustomProperties.TryGetValue(name, out var tokens) ||
                (!TokensEqual(tokens, value) && StyleToken.Serialize(tokens) != StyleToken.Serialize(value))) return false;
        return true;
    }

    private static bool TokensEqual(IReadOnlyList<StyleToken> left, IReadOnlyList<StyleToken> right)
    {
        if (ReferenceEquals(left, right)) return true;
        if (left.Count != right.Count) return false;
        for (var i = 0; i < left.Count; i++)
            if (left[i].Kind != right[i].Kind || left[i].Value != right[i].Value || left[i].Number != right[i].Number) return false;
        return true;
    }

    internal bool InheritedEqual(ComputedStyle other)
    {
        if (RootFontSize != other.RootFontSize || !CustomPropertiesEqual(other)) return false;
        foreach (var property in InheritedProperties)
            if (!Equals(ValueAt(property.Id), other.ValueAt(property.Id))) return false;
        return true;
    }

    /// <summary>
    /// Values that differ from the initial ones, in registry order, then custom properties; for tests and
    /// diagnostics.
    /// </summary>
    /// <returns>The resulting string value.</returns>
    public string Describe()
    {
        var parts = new List<string>();
        foreach (var property in StyleProperties.All)
        {
            var value = ValueAt(property.Id);
            if (!Equals(value, Initial.values[property.Id])) parts.Add($"{property.Name}: {value}");
        }
        foreach (var (name, tokens) in CustomProperties.OrderBy(p => p.Key, StringComparer.Ordinal)) parts.Add($"{name}: {StyleToken.Serialize(tokens)}");
        return string.Join("; ", parts);
    }

    /// <summary>Returns a textual representation of this value.</summary>
    /// <returns>The textual representation of this value.</returns>
    public override string ToString() => Describe();
}
