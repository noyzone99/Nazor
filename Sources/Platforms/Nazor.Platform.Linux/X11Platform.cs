namespace Nazor.Platform.Linux;

/// <summary>Owns an X11 display connection, native windows and the desktop event loop.</summary>
public sealed class X11Platform : LinuxDesktopPlatform
{
    /// <summary>Connects to the X server selected by DISPLAY.</summary>
    public X11Platform() : base("X11", "libnazor-x11.so") { }
}
