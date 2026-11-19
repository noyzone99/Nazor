namespace Nazor.Style;

/// <summary>An optionally rounded NSS inset clip relative to the border box, independent of layout and scrolling.</summary>
/// <param name="Top">The top inset; percentages resolve against the border-box height.</param>
/// <param name="Right">The right inset; percentages resolve against the border-box width.</param>
/// <param name="Bottom">The bottom inset; percentages resolve against the border-box height.</param>
/// <param name="Left">The left inset; percentages resolve against the border-box width.</param>
/// <param name="IsNone">Whether clipping is disabled.</param>
public readonly record struct ClipPathValue(Length Top, Length Right, Length Bottom, Length Left, bool IsNone = false)
{
    /// <summary>Disables clipping, including clipping of overflowing descendants and shadows.</summary>
    public static readonly ClipPathValue None = new(Length.Zero, Length.Zero, Length.Zero, Length.Zero, true);

    /// <summary>Gets the top-left elliptical radius. Percentage axes use the original border-box width and height; zero produces a square corner.</summary>
    public RadiusValue TopLeftRadius { get; init; }

    /// <summary>Gets the top-right elliptical radius. Percentage axes use the original border-box width and height; zero produces a square corner.</summary>
    public RadiusValue TopRightRadius { get; init; }

    /// <summary>Gets the bottom-right elliptical radius. Percentage axes use the original border-box width and height; zero produces a square corner.</summary>
    public RadiusValue BottomRightRadius { get; init; }

    /// <summary>Gets the bottom-left elliptical radius. Percentage axes use the original border-box width and height; zero produces a square corner.</summary>
    public RadiusValue BottomLeftRadius { get; init; }

    /// <summary>Returns the NSS representation, with four inset values, optional elliptical corner radii, or <c>none</c>.</summary>
    /// <returns>The NSS clip value.</returns>
    public override string ToString()
    {
        if (IsNone) return "none";
        var rounded = TopLeftRadius != RadiusValue.Zero || TopRightRadius != RadiusValue.Zero || BottomRightRadius != RadiusValue.Zero || BottomLeftRadius != RadiusValue.Zero;
        var radii = rounded ? $" round {TopLeftRadius.X} {TopRightRadius.X} {BottomRightRadius.X} {BottomLeftRadius.X}" : "";
        if (rounded && (TopLeftRadius.X != TopLeftRadius.Y || TopRightRadius.X != TopRightRadius.Y || BottomRightRadius.X != BottomRightRadius.Y || BottomLeftRadius.X != BottomLeftRadius.Y))
            radii += $" / {TopLeftRadius.Y} {TopRightRadius.Y} {BottomRightRadius.Y} {BottomLeftRadius.Y}";
        return $"inset({Top} {Right} {Bottom} {Left}{radii})";
    }
}
