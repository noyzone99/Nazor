using Nazor.Graphics;

namespace Nazor.Platform;

internal readonly record struct NativePopupPlacement(Rect Anchor, Rect Bounds, float Shadow, string Side, string Alignment, float Offset,
    bool Grab, bool Menu, bool LightDismiss);

internal interface INativePopupOwner
{
    bool CanShowInactivePopups { get; }
    Rect? GetPopupWorkArea(Rect anchor);
    INativePopup CreatePopup(IWindowHost host, INativePopup? parent, Action changed, Action dismissed);
}

internal interface INativePopup : IDisposable
{
    Rect? Place(NativePopupPlacement placement);
    void SetInputBounds(Rect bounds);
    bool Present(RenderedImage image, float scale);
}
