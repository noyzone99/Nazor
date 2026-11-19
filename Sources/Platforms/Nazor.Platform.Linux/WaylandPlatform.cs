namespace Nazor.Platform.Linux;

/// <summary>Owns a Wayland connection and the native desktop event loop.</summary>
public sealed class WaylandPlatform : LinuxDesktopPlatform
{
    /// <summary>Connects to the compositor selected by WAYLAND_DISPLAY and XDG_RUNTIME_DIR.</summary>
    public WaylandPlatform() : base("Wayland", "libnazor-wayland.so") { }
}
