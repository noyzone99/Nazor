using Nazor.Graphics;
using Nazor.Style;

namespace Nazor;

public sealed partial class NazorWindow
{
    private double width = 800, height = 600;
    private double? minWidth, minHeight, maxWidth, maxHeight;
    private int? x, y;
    private bool isAlwaysOnTop, isVisibleInTaskbar = true;
    private BackgroundMode backgroundMode;
    private BackdropSelection backdropRequest;
    private StyleSheet? windowStyleSheet;
    private bool notifyingPresentation;
    private string? rootClass, rootStyle;

    /// <summary>Space-separated NSS classes on the implicit window root.</summary>
    public string? Class { get => rootClass; set { AccessConfiguration(); if (rootClass == value) return; rootClass = value; NotifyPresentation(); } }
    /// <summary>Inline NSS declarations on the implicit window root.</summary>
    public string? Style { get => rootStyle; set { AccessConfiguration(); if (rootStyle == value) return; rootStyle = value; NotifyPresentation(); } }

    /// <summary>Logical client width; non-normal states retain the requested normal dimensions.</summary>
    public double Width { get => State == WindowState.Normal && platformWindow is not null ? platformWindow.ClientSize.Width : width; set { ValidateDimension(value); if (Width == value) return; height = Height; width = value; ResizeConfigured(); } }
    /// <summary>Logical client height; non-normal states retain the requested normal dimensions.</summary>
    public double Height { get => State == WindowState.Normal && platformWindow is not null ? platformWindow.ClientSize.Height : height; set { ValidateDimension(value); if (Height == value) return; width = Width; height = value; ResizeConfigured(); } }
    private void ValidateDimension(double value)
    {
        AccessConfiguration();
        if (!double.IsFinite(value) || value < 1 || value > 32768) throw new ArgumentOutOfRangeException(nameof(value));
    }
    /// <inheritdoc />
    public double? MinWidth { get => minWidth; set => SetSizeLimits(value, minHeight, maxWidth, maxHeight); }
    /// <inheritdoc />
    public double? MinHeight { get => minHeight; set => SetSizeLimits(minWidth, value, maxWidth, maxHeight); }
    /// <inheritdoc />
    public double? MaxWidth { get => maxWidth; set => SetSizeLimits(minWidth, minHeight, value, maxHeight); }
    /// <inheritdoc />
    public double? MaxHeight { get => maxHeight; set => SetSizeLimits(minWidth, minHeight, maxWidth, value); }
    /// <inheritdoc />
    public int? X { get => platformWindow?.Position?.X ?? x; set { AccessConfiguration(); if (X == value) return; platformWindow?.Move(value, null); x = value; NotifyPresentation(); } }
    /// <inheritdoc />
    public int? Y { get => platformWindow?.Position?.Y ?? y; set { AccessConfiguration(); if (Y == value) return; platformWindow?.Move(null, value); y = value; NotifyPresentation(); } }

    private void SetSizeLimits(double? minimumWidth, double? minimumHeight, double? maximumWidth, double? maximumHeight)
    {
        AccessConfiguration();
        foreach (var value in new[] { minimumWidth, minimumHeight, maximumWidth, maximumHeight })
            if (value.HasValue) ValidateDimension(value.Value);
        if (minimumWidth > maximumWidth || minimumHeight > maximumHeight)
            throw new ArgumentException("Minimum window dimensions cannot exceed maximum dimensions.");
        if ((minWidth, minHeight, maxWidth, maxHeight) == (minimumWidth, minimumHeight, maximumWidth, maximumHeight)) return;
        platformWindow?.SetSizeLimits(minimumWidth, minimumHeight, maximumWidth, maximumHeight);
        (minWidth, minHeight, maxWidth, maxHeight) = (minimumWidth, minimumHeight, maximumWidth, maximumHeight);
        width = Math.Clamp(Width, minWidth ?? 1, maxWidth ?? 32768);
        height = Math.Clamp(Height, minHeight ?? 1, maxHeight ?? 32768);
        NotifyPresentation();
    }
    private void ResizeConfigured()
    {
        width = Math.Clamp(width, minWidth ?? 1, maxWidth ?? 32768);
        height = Math.Clamp(height, minHeight ?? 1, maxHeight ?? 32768);
        platformWindow?.Resize(new Size((float)width, (float)height));
        NotifyPresentation();
    }
    private void AccessConfiguration() { Dispatcher.AssertAccess(); ObjectDisposedException.ThrowIf(disposed, this); }

    /// <inheritdoc />
    public bool IsAlwaysOnTop
    {
        get => isAlwaysOnTop;
        set
        {
            AccessConfiguration();
            if (isAlwaysOnTop == value) return;
            if (!Capabilities.CanSetAlwaysOnTop) throw new PlatformNotSupportedException("Always-on-top windows are unavailable.");
            platformWindow?.SetAlwaysOnTop(value);
            isAlwaysOnTop = value;
            NotifyPresentation();
        }
    }
    /// <inheritdoc />
    public BackgroundMode BackgroundMode
    {
        get => backgroundMode;
        set
        {
            AccessConfiguration();
            if (!Enum.IsDefined(value)) throw new ArgumentOutOfRangeException(nameof(value));
            if (backgroundMode == value) return;
            if (platformWindow is not null) throw new InvalidOperationException("BackgroundMode is an opening setting.");
            if (value == BackgroundMode.Transparent && !backendCapabilities.CanUseTransparentBackground)
                throw new PlatformNotSupportedException("Transparent window backgrounds are unavailable.");
            backgroundMode = value;
            NotifyPresentation();
        }
    }
    /// <inheritdoc />
    public bool IsVisibleInTaskbar
    {
        get => isVisibleInTaskbar;
        set
        {
            AccessConfiguration();
            if (isVisibleInTaskbar == value) return;
            if (!Capabilities.CanSetTaskbarVisibility) throw new PlatformNotSupportedException("Taskbar visibility is unavailable.");
            platformWindow?.SetTaskbarVisibility(value);
            isVisibleInTaskbar = value;
            NotifyPresentation();
        }
    }
    /// <summary>Requested backdrop; see AppliedBackdrop for the accepted result.</summary>
    public BackdropSelection Backdrop
    {
        get => backdropRequest;
        set { AccessConfiguration(); if (Equals(backdropRequest, value)) return; backdropRequest = value; ApplyBackdrop(); NotifyPresentation(); }
    }
    /// <summary>Stylesheet applied at the window level.</summary>
    public IStyleSheet? StyleSheet
    {
        get => windowStyleSheet;
        set
        {
            AccessConfiguration();
            if (value is not null && value is not Nazor.Style.StyleSheet) throw new ArgumentException("Supply a Nazor.Style.StyleSheet.", nameof(value));
            if (ReferenceEquals(value, windowStyleSheet)) return;
            windowStyleSheet = (StyleSheet?)value;
            windowElement?.SetAttribute("stylesheet", windowStyleSheet);
            Scene.Styles.Window = windowStyleSheet;
            NotifyPresentation();
            RequestFrame(WindowFrameCause.Explicit);
        }
    }
    /// <summary>Placement persistence key; only configurable before native creation.</summary>
    public string? PlacementKey
    {
        get => placementKey;
        set { AccessConfiguration(); if (value == placementKey) return; if (platformWindow is not null) throw new InvalidOperationException("PlacementKey is an opening setting."); placementKey = value; }
    }
    /// <summary>Explicit icon or a backend-resolved application icon source.</summary>
    public ImageSource? EffectiveIcon => icon ?? platformWindow?.ApplicationIcon;
    /// <summary>Raised on the UI thread after observable window presentation changes.</summary>
    public event Action? Changed;

    private void NotifyPresentation()
    {
        if (disposed || notifyingPresentation) return;
        notifyingPresentation = true;
        try { Changed?.Invoke(); }
        finally { notifyingPresentation = false; }
    }
}
