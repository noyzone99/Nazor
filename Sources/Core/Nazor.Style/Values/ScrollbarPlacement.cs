namespace Nazor.Style;

/// <summary>Controls whether a visible scrollbar overlays content or reserves space beside it.</summary>
public enum ScrollbarPlacement : byte
{
    /// <summary>Draws above the content without changing its available size.</summary>
    Overlay,

    /// <summary>Reserves space beside overflowing content. Autohide always uses overlay placement.</summary>
    Reserve,
}
