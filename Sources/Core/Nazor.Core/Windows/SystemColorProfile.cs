namespace Nazor;

/// <summary>The fallback palette used when a backend cannot supply a system color. Profiles are approximations, not native theme implementations.</summary>
public enum SystemColorProfile
{
    /// <summary>A desktop-independent Nazor palette.</summary>
    Generic,
    /// <summary>A Windows light or dark palette.</summary>
    Windows,
    /// <summary>A KDE Breeze light or dark palette.</summary>
    KDE,
    /// <summary>A GNOME Adwaita light or dark palette.</summary>
    GNOME,
    /// <summary>A macOS light or dark palette.</summary>
    MacOS,
    /// <summary>An Android light or dark palette.</summary>
    Android,
    /// <summary>An iOS light or dark palette.</summary>
    IOS,
}
