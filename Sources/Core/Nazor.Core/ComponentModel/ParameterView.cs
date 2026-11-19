using System.Collections;
using System.Collections.Concurrent;
using System.Reflection;

namespace Nazor;

/// <summary>A parameter name and value supplied to a component.</summary>
/// <param name="Name">The name used to identify the parameter, property, resource, or registration.</param>
/// <param name="Value">The numeric value or payload represented by this record.</param>
public readonly record struct ParameterValue(string Name, object? Value)
{
    internal bool Cascading { get; init; }
}

/// <summary>
/// The parameters supplied to a component. Names are matched to <see cref="ParameterAttribute" /> properties
/// case-insensitively, so the markup attribute <c>@onclick</c> sets <c>OnClick</c>.
/// </summary>
public readonly struct ParameterView : IEnumerable<ParameterValue>
{
    private readonly ParameterValue[]? values;

    private ParameterView(ParameterValue[] values) => this.values = values;

    /// <summary>Gets a parameter view containing no values.</summary>
    public static ParameterView Empty => default;

    /// <summary>Gets the number of parameters in this view.</summary>
    public int Count => values?.Length ?? 0;

    /// <summary>Creates a parameter view over the supplied dictionary of parameter names and values.</summary>
    /// <param name="parameters">The parameter names and values to supply to the component.</param>
    /// <returns>A parameter view containing a snapshot of the supplied names and values.</returns>
    public static ParameterView FromDictionary(IReadOnlyDictionary<string, object?> parameters) =>
        new(parameters.Select(p => new ParameterValue(p.Key, p.Value)).ToArray());

    internal static ParameterView FromValues(ParameterValue[] values) => new(values);

    /// <summary>Looks up a named parameter and returns whether it was present.</summary>
    /// <param name="name">The name used to identify the parameter, property, resource, or registration.</param>
    /// <param name="value">When this method returns, contains the value produced by the operation.</param>
    /// <typeparam name="TValue">The type of the component, callback, or cascading value.</typeparam>
    /// <returns>true if the named parameter exists; otherwise, false.</returns>
    public bool TryGetValue<TValue>(string name, out TValue value)
    {
        foreach (var parameter in values ?? [])
        {
            if (string.Equals(parameter.Name, name, StringComparison.OrdinalIgnoreCase))
            {
                value = (TValue)parameter.Value!;
                return true;
            }
        }
        value = default!;
        return false;
    }

    /// <summary>
    /// Gets the named parameter converted to the requested type, or the type's default value if absent.
    /// </summary>
    /// <param name="name">The name used to identify the parameter, property, resource, or registration.</param>
    /// <typeparam name="TValue">The type of the component, callback, or cascading value.</typeparam>
    /// <returns>The parameter value cast to the requested type, or the type's default value if absent.</returns>
    public TValue? GetValueOrDefault<TValue>(string name) => TryGetValue<TValue>(name, out var value) ? value : default;

    /// <summary>Copies the parameter names and values to a dictionary.</summary>
    /// <returns>A dictionary of the parameter names and values, using case-insensitive name matching.</returns>
    public IReadOnlyDictionary<string, object?> ToDictionary()
    {
        var result = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        foreach (var parameter in values ?? []) result[parameter.Name] = parameter.Value;
        return result;
    }

    /// <summary>
    /// Assigns each parameter to the matching <see cref="ParameterAttribute" /> property of
    /// <paramref name="target" />.
    /// </summary>
    /// <param name="target">The component, element, or navigation entry targeted by the operation.</param>
    /// <exception cref="ArgumentNullException">target is null.</exception>
    [System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessage("Trimming", "IL2072", Justification = "Nazor.Sdk preserves component and parameter value metadata in components.xml.")]
    public void SetParameterProperties(object target)
    {
        ArgumentNullException.ThrowIfNull(target);
        var properties = ParameterProperties.For(target.GetType());
        var captures = properties.Values.Where(p => p.GetCustomAttribute<ParameterAttribute>(true)?.CaptureUnmatchedValues == true).ToArray();
        if (captures.Length > 1)
            throw new InvalidOperationException($"Component '{target.GetType().FullName}' can declare only one parameter with CaptureUnmatchedValues.");
        var capture = captures.SingleOrDefault();
        if (capture is not null && !capture.PropertyType.IsAssignableFrom(typeof(Dictionary<string, object?>)))
            throw new InvalidOperationException($"Capture parameter '{capture.Name}' must accept a Dictionary<string, object?>.");
        Dictionary<string, object?>? unmatched = null;
        foreach (var parameter in values ?? [])
        {
            if (!properties.TryGetValue(parameter.Name, out var property))
            {
                if (capture is not null)
                {
                    (unmatched ??= new(StringComparer.Ordinal))[parameter.Name] = parameter.Value;
                    continue;
                }
                throw new InvalidOperationException(
                    $"Object of type '{target.GetType().FullName}' does not have a parameter matching the name '{parameter.Name}'.");
            }
            try
            {
                if (property.GetCustomAttribute<CascadingParameterAttribute>(true) is not null && !parameter.Cascading)
                    throw new InvalidOperationException($"Cascading parameter '{property.Name}' cannot be set as a direct parameter.");
                property.SetValue(target, parameter.Value is null && property.PropertyType.IsValueType
                    ? Activator.CreateInstance(property.PropertyType) : parameter.Value);
            }
            catch (Exception exception) when (exception is ArgumentException or InvalidCastException)
            {
                throw new InvalidOperationException(
                    $"Unable to set parameter '{property.Name}' on object of type '{target.GetType().FullName}': " +
                    $"a value of type '{parameter.Value?.GetType().FullName ?? "null"}' is not assignable to '{property.PropertyType.FullName}'.",
                    exception);
            }
        }
        if (unmatched is not null)
        {
            if ((values ?? []).Any(p => string.Equals(p.Name, capture!.Name, StringComparison.OrdinalIgnoreCase)))
                throw new InvalidOperationException($"Cannot supply '{capture!.Name}' together with unmatched parameters.");
            capture!.SetValue(target, unmatched);
        }
        else if (capture is not null && !(values ?? []).Any(p => string.Equals(p.Name, capture.Name, StringComparison.OrdinalIgnoreCase)))
            capture.SetValue(target, null);
    }

    /// <summary>Enumerates the parameter names and values in this view.</summary>
    /// <returns>An enumerator over the collection's ordered values.</returns>
    public IEnumerator<ParameterValue> GetEnumerator() => ((IEnumerable<ParameterValue>)(values ?? [])).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    internal static void ClearCache() => ParameterProperties.Cache.Clear();

    private static class ParameterProperties
    {
        public static readonly ConcurrentDictionary<Type, Dictionary<string, PropertyInfo>> Cache = new();

        [System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessage("Trimming", "IL2070", Justification = "Nazor.Sdk preserves all component properties, including inherited nonpublic properties.")]
        [System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessage("Trimming", "IL2075", Justification = "Nazor.Sdk preserves all component base types in components.xml.")]
        public static Dictionary<string, PropertyInfo> For(Type type) => Cache.GetOrAdd(type, static type =>
        {
            var result = new Dictionary<string, PropertyInfo>(StringComparer.OrdinalIgnoreCase);
            for (var current = type; current is not null && current != typeof(object); current = current.BaseType)
            {
                foreach (var property in current.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
                {
                    var direct = property.GetCustomAttribute<ParameterAttribute>(inherit: true);
                    var cascading = property.GetCustomAttribute<CascadingParameterAttribute>(inherit: true);
                    if (direct is not null && cascading is not null)
                        throw new InvalidOperationException($"Property '{property.Name}' cannot be both a direct and a cascading parameter.");
                    if (direct is not null && (property.GetMethod?.IsPublic != true || property.SetMethod?.IsPublic != true)) continue;
                    if ((direct is null && cascading is null) || property.SetMethod is null) continue;
                    if (!result.TryAdd(property.Name, property) && result[property.Name].DeclaringType == current)
                        throw new InvalidOperationException(
                            $"Component '{type.FullName}' declares more than one parameter named '{property.Name}' (names are case-insensitive).");
                }
            }
            return result;
        });
    }
}
