namespace Nazor.Style;

/// <summary>Controls the appearance of scrollbars on overflowing scroll containers.</summary>
public enum ScrollbarVisibility : byte
{
    /// <summary>Hides scrollbars while preserving wheel, keyboard and programmatic scrolling.</summary>
    None,

    /// <summary>Shows scrollbars whenever content overflows on the corresponding axis.</summary>
    Visible,

    /// <summary>Reveals scrollbars near the pointer, during thumb dragging or wheel scrolling, with configurable fades.</summary>
    AutoHide,
}
