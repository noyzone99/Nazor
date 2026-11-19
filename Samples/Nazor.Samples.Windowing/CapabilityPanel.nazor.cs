namespace Nazor.Samples.Windowing;

public partial class CapabilityPanel
{
    protected override void OnInitialized()
    {
        CurrentWindow.Changed += Refresh;
        CurrentWindow.EnvironmentChanged += Refresh;
        CurrentWindow.SystemButtonsChanged += Refresh;
    }

    private void Refresh() => StateHasChanged();
    private static string Value<T>(T? value) where T : struct => value?.ToString() ?? "Not set";
    private static string Buttons(IReadOnlyList<WindowButton> buttons) => buttons.Count == 0 ? "None" : string.Join(", ", buttons);
    private static string Operation(WindowOperationSupport support, bool allowed, bool effective) =>
        $"backend: {support.IsSupported}; app allows: {allowed}; effective: {effective}; native restriction: {support.Restriction}";

    private static (string Name, bool Value)[] OtherCapabilities(WindowCapabilities capabilities) =>
    [
        ("Observe minimized state", capabilities.CanObserveMinimized),
        ("Restore from minimized", capabilities.CanRestoreFromMinimized),
        ("Set screen position", capabilities.CanSetScreenPosition),
        ("Set individual window icon", capabilities.CanSetWindowIcon),
        ("Keep window always on top", capabilities.CanSetAlwaysOnTop),
        ("Use a transparent background", capabilities.CanUseTransparentBackground),
        ("Change taskbar visibility", capabilities.CanSetTaskbarVisibility),
        ("Show popups outside the window", capabilities.CanShowPopupsOutsideWindow),
    ];

    public void Dispose()
    {
        CurrentWindow.Changed -= Refresh;
        CurrentWindow.EnvironmentChanged -= Refresh;
        CurrentWindow.SystemButtonsChanged -= Refresh;
    }
}
