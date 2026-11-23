namespace Nazor.Style;

/// <summary>An explicit color or the default selected by the consuming property.</summary>
/// <param name="Value">Explicit color; null means auto.</param>
public readonly record struct NativeColor(Color? Value)
{
    /// <summary>Uses the automatic color for the property.</summary>
    public static NativeColor Auto => default;
    /// <inheritdoc />
    public override string ToString() => Value?.ToString() ?? "auto";
}
