namespace Nazor.Platform.Linux;

/// <summary>Selects a Linux desktop display protocol. DRM/KMS uses its separate explicit platform host.</summary>
public enum LinuxDesktopBackend
{
    /// <summary>Selects a protocol from NAZOR_LINUX_BACKEND, the published libraries and session; falls back from Wayland to X11 only when both are published and no environment override is set.</summary>
    Auto,
    /// <summary>Connects to a Wayland compositor.</summary>
    Wayland,
    /// <summary>Connects to an X11 display server.</summary>
    X11
}
