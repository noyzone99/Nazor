using Nazor.Graphics;

namespace Nazor.Platform;

/// <summary>Native Windows title bar colors. Null restores the system color; custom title bars use only button colors.</summary>
public sealed record WindowsTitleBarColors
{
    /// <summary>Active title bar background.</summary>
    public Color? BackgroundColor { get; init; }
    /// <summary>Active title text.</summary>
    public Color? ForegroundColor { get; init; }
    /// <summary>Inactive title bar background.</summary>
    public Color? InactiveBackgroundColor { get; init; }
    /// <summary>Inactive title text.</summary>
    public Color? InactiveForegroundColor { get; init; }
    /// <summary>Active caption button background.</summary>
    public Color? ButtonBackgroundColor { get; init; }
    /// <summary>Active caption button foreground.</summary>
    public Color? ButtonForegroundColor { get; init; }
    /// <summary>Inactive caption button background.</summary>
    public Color? ButtonInactiveBackgroundColor { get; init; }
    /// <summary>Inactive caption button foreground.</summary>
    public Color? ButtonInactiveForegroundColor { get; init; }
    /// <summary>Hovered caption button background; Windows retains the close button color.</summary>
    public Color? ButtonHoverBackgroundColor { get; init; }
    /// <summary>Hovered caption button foreground.</summary>
    public Color? ButtonHoverForegroundColor { get; init; }
    /// <summary>Pressed caption button background; Windows retains the close button color.</summary>
    public Color? ButtonPressedBackgroundColor { get; init; }
    /// <summary>Pressed caption button foreground.</summary>
    public Color? ButtonPressedForegroundColor { get; init; }
}
