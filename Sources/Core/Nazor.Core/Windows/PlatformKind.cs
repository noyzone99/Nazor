namespace Nazor;

/// <summary>Stable platform identity for backend-provided style queries. Unknown backends return null.</summary>
public enum PlatformKind
{
    /// <summary>Identifies the Microsoft Windows platform.</summary>
    Windows,

    /// <summary>Identifies the Linux platform.</summary>
    Linux,

    /// <summary>Identifies the Apple macOS platform.</summary>
    MacOS,

    /// <summary>Identifies the Android platform.</summary>
    Android,

    /// <summary>Identifies the Apple iOS platform.</summary>
    IOS,
}
