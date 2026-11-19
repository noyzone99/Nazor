namespace Nazor;

/// <summary>Portable presets. The backend chooses its own material or an opaque fallback.</summary>
public enum CommonBackdrop
{
    /// <summary>Requests no client-area backdrop material; system decorations retain their native appearance.</summary>
    None,

    /// <summary>Requests Acrylic on Windows or background blur on Linux, when supported.</summary>
    Blur,

    /// <summary>Requests Mica on Windows or no compositor effect on Linux.</summary>
    Solid
}

/// <summary>Windows materials; describing one does not load Windows libraries.</summary>
public enum WindowsBackdrop
{
    /// <summary>Requests the Windows Mica material.</summary>
    Mica,

    /// <summary>Requests the Windows Mica Alt material.</summary>
    MicaAlt,

    /// <summary>Requests the Windows Acrylic material.</summary>
    Acrylic
}

/// <summary>Linux materials; availability depends on the compositor's capabilities.</summary>
public enum LinuxBackdrop
{
    /// <summary>Requests the compositor's blur-behind effect.</summary>
    Blur
}

/// <summary>An immutable request, resolved by the native window rather than by the page.</summary>
public readonly record struct BackdropSelection
{
    /// <summary>Gets the optional portable backdrop preset.</summary>
    public CommonBackdrop? Preset { get; }

    /// <summary>Gets the optional Windows-specific backdrop material.</summary>
    public WindowsBackdrop? Windows { get; }

    /// <summary>Gets the optional Linux-specific backdrop material.</summary>
    public LinuxBackdrop? Linux { get; }

    /// <summary>Gets the portable backdrop fallback used when a platform-specific material is unavailable.</summary>
    public CommonBackdrop Default { get; }

    internal BackdropSelection(CommonBackdrop? preset, WindowsBackdrop? windows, LinuxBackdrop? linux, CommonBackdrop fallback)
    {
        if (preset is { } p && !Enum.IsDefined(p)) throw new ArgumentOutOfRangeException(nameof(preset));
        if (windows is { } w && !Enum.IsDefined(w)) throw new ArgumentOutOfRangeException(nameof(windows));
        if (linux is { } l && !Enum.IsDefined(l)) throw new ArgumentOutOfRangeException(nameof(linux));
        if (!Enum.IsDefined(fallback)) throw new ArgumentOutOfRangeException(nameof(fallback));
        Preset = preset;
        Windows = windows;
        Linux = linux;
        Default = fallback;
    }

    /// <summary>Converts a CommonBackdrop value to BackdropSelection.</summary>
    /// <param name="value">The value to use.</param>
    /// <returns>The resulting BackdropSelection value.</returns>
    public static implicit operator BackdropSelection(CommonBackdrop value) => new(value, null, null, CommonBackdrop.None);

    /// <summary>Converts a WindowsBackdrop value to BackdropSelection.</summary>
    /// <param name="value">The value to use.</param>
    /// <returns>The resulting BackdropSelection value.</returns>
    public static implicit operator BackdropSelection(WindowsBackdrop value) => new(null, value, null, CommonBackdrop.None);

    /// <summary>Converts a LinuxBackdrop value to BackdropSelection.</summary>
    /// <param name="value">The value to use.</param>
    /// <returns>The resulting BackdropSelection value.</returns>
    public static implicit operator BackdropSelection(LinuxBackdrop value) => new(null, null, value, CommonBackdrop.None);
}

/// <summary>Provides predefined backdrop selections for common and platform-specific window materials.</summary>
public static class Backdrop
{
    /// <summary>Override selected mappings of a preset. Omitted backends inherit the preset.</summary>
    /// <param name="preset">The preset (CommonBackdrop?).</param>
    /// <param name="Windows">The windows (WindowsBackdrop?).</param>
    /// <param name="Linux">The optional Linux material override.</param>
    /// <param name="Default">The default (CommonBackdrop).</param>
    /// <returns>The resulting BackdropSelection value.</returns>
    public static BackdropSelection ForPlatform(CommonBackdrop? preset = null,
        WindowsBackdrop? Windows = null, LinuxBackdrop? Linux = null, CommonBackdrop Default = CommonBackdrop.None) =>
        new(preset, Windows, Linux, Default);
}

/// <summary>What the backend accepted. Material is a diagnostic identifier, not a rendering guarantee.</summary>
/// <param name="IsActive">The is active (bool).</param>
/// <param name="Material">The material (string?).</param>
public readonly record struct BackdropResult(bool IsActive, string? Material = null);
