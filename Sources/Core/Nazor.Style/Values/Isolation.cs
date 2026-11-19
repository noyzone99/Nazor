namespace Nazor.Style;

/// <summary>Whether descendants blend only against pixels inside the element group.</summary>
public enum Isolation : byte
{
    /// <summary>No additional isolation beyond existing opacity/filter/blend groups.</summary>
    Auto,
    /// <summary>Starts an isolated transparent group for the complete element subtree.</summary>
    Isolate,
}
