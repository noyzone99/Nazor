using System.Reflection;

namespace Nazor;

internal static class WindowParameters
{
    [System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessage("Trimming", "IL2070", Justification = "Nazor.Sdk preserves component properties and their nullability metadata.")]
    internal static void Validate(Type type, ParameterView parameters)
    {
        var properties = type.GetProperties().Where(p => p.IsDefined(typeof(ParameterAttribute), true)).ToArray();
        var nullability = new NullabilityInfoContext();
        foreach (var property in properties)
        {
            var supplied = parameters.Any(p => p.Name.Equals(property.Name, StringComparison.OrdinalIgnoreCase));
            if (!supplied && property.IsDefined(typeof(EditorRequiredAttribute), true))
                throw new InvalidOperationException($"Window '{type.Name}' requires parameter '{property.Name}'.");
            foreach (var parameter in parameters.Where(p => p.Name.Equals(property.Name, StringComparison.OrdinalIgnoreCase)))
            {
                if (parameter.Value is null && (property.PropertyType.IsValueType && Nullable.GetUnderlyingType(property.PropertyType) is null ||
                    nullability.Create(property).WriteState == NullabilityState.NotNull))
                    throw new InvalidOperationException($"Window parameter '{property.Name}' does not accept null.");
                if (parameter.Value is not null && !property.PropertyType.IsInstanceOfType(parameter.Value))
                    throw new InvalidOperationException($"Window parameter '{property.Name}' requires '{property.PropertyType}'.");
            }
        }
        if (properties.Any(p => p.GetCustomAttribute<ParameterAttribute>(true)?.CaptureUnmatchedValues == true)) return;
        foreach (var parameter in parameters)
            if (!properties.Any(p => p.Name.Equals(parameter.Name, StringComparison.OrdinalIgnoreCase)))
                throw new InvalidOperationException($"Window '{type.Name}' has no parameter '{parameter.Name}'.");
    }
}
