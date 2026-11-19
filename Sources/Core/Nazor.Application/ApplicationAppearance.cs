using Nazor.Style;

namespace Nazor;

/// <summary>Live application-wide appearance. Configure after Build, then change on the UI thread while running.</summary>
public interface IApplicationAppearance
{
    /// <summary>Control theme shared by all windows; null selects Basic. Use Themes.Themes.None for no theme rules.</summary>
    StyleSheet? Theme { get; set; }
    /// <summary>Application stylesheet shared by all windows.</summary>
    StyleSheet? StyleSheet { get; set; }
    /// <summary>Global color scheme; System follows the operating system.</summary>
    ColorScheme ColorScheme { get; set; }
    /// <summary>Animation preference; null follows the operating system.</summary>
    bool? ReducedMotion { get; set; }
    /// <summary>Contrast preference; null follows the operating system.</summary>
    bool? HighContrast { get; set; }
    /// <summary>Application-wide control density. Automatic follows each window host's touch-input preference; an explicit choice overrides that preference. Themes decide the dimensions.</summary>
    ControlDensity ControlDensity { get; set; }
}

internal sealed class ApplicationAppearance : IApplicationAppearance
{
    private UIDispatcher? dispatcher;
    private StyleSheet? theme;
    private StyleSheet? styleSheet;
    private ColorScheme colorScheme;
    private bool? reducedMotion;
    private bool? highContrast;
    private ControlDensity controlDensity;

    public StyleSheet? Theme { get => theme; set => Set(ref theme, value); }
    public StyleSheet? StyleSheet { get => styleSheet; set => Set(ref styleSheet, value); }
    public ColorScheme ColorScheme
    {
        get => colorScheme;
        set
        {
            if (!Enum.IsDefined(value)) throw new ArgumentOutOfRangeException(nameof(value));
            Set(ref colorScheme, value);
        }
    }
    public bool? ReducedMotion { get => reducedMotion; set => Set(ref reducedMotion, value); }
    public bool? HighContrast { get => highContrast; set => Set(ref highContrast, value); }
    public ControlDensity ControlDensity
    {
        get => controlDensity;
        set
        {
            if (!Enum.IsDefined(value)) throw new ArgumentOutOfRangeException(nameof(value));
            Set(ref controlDensity, value);
        }
    }

    internal event Action? Changed;
    internal void Attach(UIDispatcher value) => dispatcher = value;
    internal void Detach() => dispatcher = null;
    private void Set<T>(ref T field, T value)
    {
        dispatcher?.AssertAccess();
        if (EqualityComparer<T>.Default.Equals(field, value)) return;
        field = value;
        Changed?.Invoke();
    }
}
