namespace Nazor.Platform;

/// <summary>Independent placement functions offered by a backend.</summary>
/// <param name="CanRestoreSize">The can restore size (bool).</param>
/// <param name="CanRestorePosition">The can restore position (bool).</param>
/// <param name="CanSelectScreen">The can select screen (bool).</param>
/// <param name="CanCenter">The can center (bool).</param>
public readonly record struct WindowPlacementCapabilities(bool CanRestoreSize, bool CanRestorePosition,
    bool CanSelectScreen, bool CanCenter);

/// <summary>Coordinate representation stored with placements so other backends do not reinterpret pixels.</summary>
public enum WindowPlacementCoordinates
{
    /// <summary>Stores native Windows window bounds in work-area physical pixels.</summary>
    WindowsWorkAreaPixels,

    /// <summary>Stores placement using portable logical client coordinates.</summary>
    LogicalClient
}
