namespace Nazor.Style;

/// <summary>How wheel, keyboard and programmatic scroll requests move the content.</summary>
public enum ScrollBehavior : byte
{
    /// <summary>Applies the requested offset immediately.</summary>
    Auto,
    /// <summary>Interpolates to the requested offset using the configured duration and easing.</summary>
    Smooth,
}
