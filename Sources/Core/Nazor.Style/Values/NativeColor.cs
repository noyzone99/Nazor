namespace Nazor.Style;

/// <summary>A native decoration color, or auto to restore the operating system default.</summary>
/// <param name="Value">Explicit color; null means auto.</param>
public readonly record struct NativeColor(Color? Value)
{
    /// <summary>Uses the operating system color.</summary>
    public static NativeColor Auto => default;
    /// <inheritdoc />
    public override string ToString() => Value?.ToString() ?? "auto";
}
