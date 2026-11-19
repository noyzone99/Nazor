namespace Nazor;

/// <summary>Chooses a theme's control spacing and interaction target sizes independently of the window width.</summary>
public enum ControlDensity
{
    /// <summary>Inherits the application choice, then follows the host's touch-input preference; unavailable preferences use compact density.</summary>
    Automatic,

    /// <summary>Uses the theme's compact desktop dimensions.</summary>
    Compact,

    /// <summary>Uses the theme's larger touch targets, text and row spacing.</summary>
    Touch,
}
