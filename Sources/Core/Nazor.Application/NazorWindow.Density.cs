namespace Nazor;

public sealed partial class NazorWindow
{
    /// <summary>Gets or sets the window-local density override on the UI dispatcher. Automatic inherits the application choice and then the host preference; explicit choices survive later application and host updates.</summary>
    public ControlDensity ControlDensity
    {
        get => densityOverride;
        set
        {
            Dispatcher.AssertAccess();
            if (!Enum.IsDefined(value)) throw new ArgumentOutOfRangeException(nameof(value));
            if (densityOverride == value) return;
            densityOverride = value;
            if (platformWindow is null) return;
            ApplyPreferences();
            NotifyPresentation();
            RequestFrame(WindowFrameCause.SystemSettings);
        }
    }

    private ControlDensity ResolvedDensity => densityOverride != ControlDensity.Automatic ? densityOverride :
        applicationDensity != ControlDensity.Automatic ? applicationDensity :
        PlatformWindow.PrefersTouchInput ? ControlDensity.Touch : ControlDensity.Compact;
}
