using Nazor.Rendering;

namespace Nazor.Presentation;

public sealed partial class Scene
{
    private bool PublishPopupOrigins(ElementNode overlay)
    {
        var right = false;
        var bottom = false;
        if (overlay.GetFeature<ElementLayout>() is { OverlayShift: { } shift } layout && OverlayAnchor(overlay) is { } anchor)
        {
            var surface = layout.Node.RoundedFrame.Offset(shift.X, shift.Y);
            var placement = overlay.Attributes.GetValueOrDefault("placement") as string ?? "";
            var end = placement.EndsWith("-end", StringComparison.Ordinal);
            right = Nearer(Math.Abs(surface.Right - anchor.Right), Math.Abs(surface.X - anchor.X), end);
            bottom = Nearer(Math.Abs(surface.Bottom - anchor.Bottom), Math.Abs(surface.Y - anchor.Y), end);
        }
        var changed = Publish(overlay, ElementStates.PopupOriginRight, right);
        return Publish(overlay, ElementStates.PopupOriginBottom, bottom) || changed;
    }

    private static bool Nearer(float trailing, float leading, bool preferTrailing) =>
        Math.Abs(trailing - leading) < .01f ? preferTrailing : trailing < leading;

    private static bool Publish(ElementNode element, ElementStates state, bool active)
    {
        if (element.States.HasFlag(state) == active) return false;
        element.SetState(state, active);
        return true;
    }
}
