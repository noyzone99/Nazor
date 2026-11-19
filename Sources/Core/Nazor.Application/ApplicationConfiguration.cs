using System.Globalization;
using Nazor.Animation;
using Nazor.Platform;

namespace Nazor;

/// <summary>Application-wide settings captured when the builder is built.</summary>
public sealed record ApplicationConfiguration
{
    /// <summary>Initial formatting culture override; null preserves CurrentCulture at startup.</summary>
    public CultureInfo? Culture { get; set; }
    /// <summary>Initial resource language override; null preserves CurrentUICulture at startup.</summary>
    public CultureInfo? UICulture { get; set; }
    /// <summary>Determines when closing windows ends the application.</summary>
    public ShutdownMode ShutdownMode { get; set; }
}

/// <summary>Backend selection captured at Build; native initialization occurs at Run.</summary>
public sealed class PlatformConfiguration
{
    /// <summary>Overrides the SDK-registered host. The host owns backend selection and UI-thread startup.</summary>
    public IPlatformHost? Host { get; set; }
}

/// <summary>Development and instrumentation settings captured at Build.</summary>
public sealed record DiagnosticsConfiguration
{
    /// <summary>Enables the development channel; null reads NAZOR_DEVCHANNEL at startup.</summary>
    public bool? DevChannel { get; set; }
    /// <summary>Optional deterministic animation clock; null uses the system clock.</summary>
    public IAnimationClock? AnimationClock { get; set; }
    /// <summary>
    /// Initializes optional instrumentation on each window's UI thread after native creation and before showing it.
    /// Captured at Build; null performs no initialization. A failure disposes the window and propagates to its caller.
    /// </summary>
    public Action<NazorWindow>? ConfigureWindow { get; set; }
}

/// <summary>Defines a native window explicitly created to host a component without a Window root.</summary>
public sealed record WindowConfiguration
{
    /// <summary>The initial native title.</summary>
    public string Title { get; set; } = "Nazor";
    /// <summary>Initial logical client width.</summary>
    public double Width { get; set; } = 800;
    /// <summary>Initial logical client height.</summary>
    public double Height { get; set; } = 600;
    /// <summary>Allows minimization.</summary>
    public bool IsMinimizable { get; set; } = true;
    /// <summary>Allows maximization.</summary>
    public bool IsMaximizable { get; set; } = true;
    /// <summary>Allows interactive resizing.</summary>
    public bool IsResizable { get; set; } = true;
    /// <summary>Allows fullscreen entry.</summary>
    public bool FullScreenEnabled { get; set; } = true;
    /// <summary>Keeps the window above non-topmost windows.</summary>
    public bool IsAlwaysOnTop { get; set; }
    /// <summary>The initial complete decoration policy; null uses the desktop native-to-Nazor default.</summary>
    public WindowDecorationPolicy? Decorations { get; set; }
    /// <summary>Requests taskbar participation.</summary>
    public bool IsVisibleInTaskbar { get; set; } = true;
    /// <summary>Activates the window when shown.</summary>
    public bool Activate { get; set; } = true;
}
