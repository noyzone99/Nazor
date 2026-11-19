using Nazor.Graphics.Text;
using System.Diagnostics;
using Nazor.Accessibility;
using Nazor.Animation;
using Nazor.Graphics;
using Nazor.Input;
using Nazor.Platform;
using Nazor.Presentation;
using Nazor.Skia;
using Nazor.Rendering;
using Nazor.Style;
using Microsoft.Extensions.Logging;

namespace Nazor;

internal sealed record ApplicationRuntimeSettings
{
    public RenderingMode? Rendering { get; init; }
    public IReadOnlyList<Type> TrayItems { get; init; } = [];
    public WindowOpenOptions? Opening { get; init; }
    public bool? DeclaredWindow { get; init; }
    public bool SingleView { get; init; }
    public ApplicationAppearance? Appearance { get; init; }
    public string Title { get; set; } = "Nazor";
    public double Width { get; set; } = 800;
    public double Height { get; set; } = 600;
    public bool IsMinimizable { get; set; } = true;
    public bool IsMaximizable { get; set; } = true;
    public bool IsResizable { get; set; } = true;
    public bool FullScreenEnabled { get; set; } = true;
    /// <summary>Keeps the window above non-topmost windows.</summary>
    public bool IsAlwaysOnTop { get; set; }
    /// <summary>Creates a window without decorations or frame interactions.</summary>
    public WindowDecorationPolicy? Decorations { get; set; }
    /// <summary>Requests taskbar participation.</summary>
    public bool IsVisibleInTaskbar { get; set; } = true;
    public StyleSheet? Theme { get; set; }
    public StyleSheet? StyleSheet { get; set; }
    public IServiceProvider? Services { get; set; }
    public IPlatformHost? PlatformHost { get; set; }
    public FontOptions Fonts { get; set; } = new();
    public bool? ReducedMotion { get; set; }
    public ColorScheme ColorScheme { get; set; } = ColorScheme.System;
    public bool? HighContrast { get; set; }
    public ControlDensity ControlDensity { get; set; }
    public System.Globalization.CultureInfo? Culture { get; set; }
    public System.Globalization.CultureInfo? UICulture { get; set; }
    public IReadOnlyDictionary<string, Nazor.Graphics.Color>? SystemColors { get; set; }
    public ShutdownMode ShutdownMode { get; set; }
    public bool Activate { get; set; } = true;
    public IAnimationClock? AnimationClock { get; set; }
    public bool? DevChannel { get; set; }
    public Action<NazorWindow>? ConfigureWindow { get; init; }
}
