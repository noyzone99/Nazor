using Nazor.Graphics;
using Nazor.Platform;
using Nazor.Presentation;
using Nazor.Rendering;

namespace Nazor;

public sealed partial class NazorWindow
{
    private readonly Dictionary<ElementNode, INativePopup?> nativePopups = new(ReferenceEqualityComparer.Instance);
    private bool allowPopupsOutsideWindow;

    /// <inheritdoc />
    public bool AllowPopupsOutsideWindow
    {
        get => allowPopupsOutsideWindow;
        set
        {
            AccessConfiguration();
            if (allowPopupsOutsideWindow == value) return;
            allowPopupsOutsideWindow = value;
            UpdateNativePopupPolicy();
            NotifyPresentation();
            RequestFrame();
        }
    }

    private void UpdateNativePopupPolicy()
    {
        if (platformWindow is null) return;
        var owner = allowPopupsOutsideWindow && Capabilities.CanShowPopupsOutsideWindow
            ? platformWindow as INativePopupOwner
            : null;
        if (owner is { CanShowInactivePopups: false } && !windowActive) owner = null;
        Scene.NativePopupWorkArea = owner is null ? null : owner.GetPopupWorkArea;
        Scene.ArrangeNativePopup = owner is null ? null : (overlay, placement) => PlaceNativePopup(owner, overlay, placement);
        Scene.NativePopupLayoutStarting = owner is null ? null : RemoveClosedNativePopups;
        if (owner is null) CloseNativePopups();
    }

    private Rect? PlaceNativePopup(INativePopupOwner owner, ElementNode overlay, OverlayPlacementRequest placement)
    {
        if (!isVisible || placement.Bounds.IsEmpty) return null;
        if (!nativePopups.TryGetValue(overlay, out var popup))
        {
            INativePopup? parent = null;
            var anchor = overlay.Attributes.GetValueOrDefault("anchor") as ElementNode;
            for (var element = anchor ?? overlay.ParentElement; element is not null; element = element.ParentElement)
                if (nativePopups.TryGetValue(element, out parent) && parent is not null) break;

            popup = owner.CreatePopup(this, parent, RequestFrame, () => Input.Dismiss(overlay));
            nativePopups.Add(overlay, popup);
        }
        return popup?.Place(new(placement.Anchor, placement.Bounds, placement.Shadow, placement.Side, placement.Alignment, placement.Offset,
            Grab: overlay.Attributes.GetValueOrDefault("focus-first") is true && overlay.Attributes.GetValueOrDefault("light-dismiss") is true,
            Menu: overlay.Attributes.GetValueOrDefault("menu") is true,
            LightDismiss: overlay.Attributes.GetValueOrDefault("light-dismiss") is true));
    }

    private void RemoveClosedNativePopups()
    {
        foreach (var (overlay, popup) in nativePopups.Reverse().ToArray())
        {
            var anchor = overlay.Attributes.GetValueOrDefault("anchor") as ElementNode;
            var hasAnchor = anchor is not null ? !anchor.IsRemoved && Scene.IsRendered(anchor)
                : overlay.Attributes.GetValueOrDefault("anchor-x") is double && overlay.Attributes.GetValueOrDefault("anchor-y") is double;
            if (!overlay.IsRemoved && Scene.IsOverlay(overlay) && Scene.IsRendered(overlay) && hasAnchor) continue;
            popup?.Dispose();
            nativePopups.Remove(overlay);
            if (!overlay.IsRemoved && !hasAnchor) Input.Dismiss(overlay);
        }
    }

    private void PaintNativePopups()
    {
        foreach (var (overlay, popup) in nativePopups.ToArray())
        {
            if (popup is null || Scene.GetNativePopupBounds(overlay) is not { } bounds) continue;
            popup.SetInputBounds(Scene.GetBounds(overlay));
            var list = Scene.BuildNativePopupList(overlay, bounds);
            if (popup.Present(graphics.RenderToImage(list, Scene.Scale, Color.Transparent), Scene.Scale)) continue;
            popup.Dispose();
            nativePopups[overlay] = null;
            RequestFrame();
        }
    }

    private void DismissNativePopups()
    {
        foreach (var (overlay, popup) in nativePopups.Reverse().ToArray())
            if (popup is not null && overlay.Attributes.GetValueOrDefault("light-dismiss") is true) Input.Dismiss(overlay);
    }

    private void CloseNativePopups()
    {
        foreach (var popup in nativePopups.Values.Reverse()) popup?.Dispose();
        nativePopups.Clear();
    }
}
