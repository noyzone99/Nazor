using Nazor.Graphics;
using Nazor.Rendering;
using Nazor.Style;

namespace Nazor.Presentation;

public sealed partial class Scene
{
    private bool FitDropDowns()
    {
        var changed = false;
        foreach (var overlay in overlays)
        {
            if (overlay.Attributes.GetValueOrDefault("drop-down") is not string ||
                overlay.GetFeature<ElementLayout>() is not { } layout || OverlayAnchor(overlay) is not { } anchor) continue;
            changed |= FitDropDown(overlay, layout, anchor, OffersNativePopup(overlay) ? NativePopupWorkArea?.Invoke(anchor) : WindowViewport);
        }
        return changed;
    }

    private static bool FitDropDown(ElementNode overlay, ElementLayout layout, Rect anchor, Rect? area)
    {
        var node = layout.Node;
        var maximum = Math.Max(1, overlay.Attributes.GetValueOrDefault("preferred-max-visible-items") as int? ?? int.MaxValue);
        var minimum = Math.Max(1, overlay.Attributes.GetValueOrDefault("preferred-min-visible-items") as int? ?? 1);
        var options = node.Children
            .Where(child => child.Tag is ElementNode element && element.Attributes.GetValueOrDefault("role") is "option" &&
                child.Style.Display != Display.None)
            .Select(child => child.Frame.Bottom + child.Margin.Bottom + node.Padding.Bottom + node.Border.Bottom)
            .ToArray();
        var desired = maximum < options.Length ? Math.Min(node.Frame.Height, options[maximum - 1]) : node.Frame.Height;
        var target = options.Length > 0 ? Math.Min(desired, options[Math.Min(minimum, options.Length) - 1]) : desired;
        // The space between the anchor and the list (offset plus the facing margin) is not available to the list.
        var offset = overlay.Attributes.GetValueOrDefault("overlay-offset") is double value && double.IsFinite(value) ? (float)value : 0;
        var above = area is { } visible ? Math.Clamp(anchor.Y - visible.Y - offset - node.Margin.Bottom, 0, visible.Height) : float.PositiveInfinity;
        var below = area is { } bounds ? Math.Clamp(bounds.Bottom - anchor.Bottom - offset - node.Margin.Top, 0, bounds.Height) : float.PositiveInfinity;
        var preference = overlay.Attributes.GetValueOrDefault("drop-down") as string;
        var centered = preference == "center";
        var opensAbove = preference switch
        {
            "top" => above > 0 && (above >= target || below < target),
            "bottom" => below < target && (above >= target || below == 0),
            _ => below < desired && (above >= desired || above > below),
        };
        if (!centered && layout.DropDownAbove is { } previous && layout.DropDownAnchor == anchor && layout.DropDownArea == area &&
            layout.DropDownPreference == preference && (previous ? above : below) >= target)
            opensAbove = previous;
        layout.DropDownAbove = centered ? null : opensAbove;
        layout.DropDownAnchor = anchor;
        layout.DropDownArea = area;
        layout.DropDownPreference = preference;
        var available = centered ? area is { } full ? Math.Max(0,full.Height-node.Margin.Vertical) : float.PositiveInfinity : opensAbove ? above : below;
        available = Math.Min(desired, available);
        var complete = options.LastOrDefault(height => height <= available + 0.01f);
        if (available < desired && complete > 0) available = complete;
        return ConstrainPopup(layout, area?.Width ?? float.PositiveInfinity, available);
    }

    private static bool ConstrainPopup(ElementLayout layout, float width, float height)
    {
        var node = layout.Node;
        var style = node.Style;
        var contentBox = style.BoxSizing == BoxSizing.ContentBox;
        if (width < node.Frame.Width - 0.01f)
            style = style with { MinWidth = SizeValue.Auto, MaxWidth = SizeValue.Fixed(Length.Px(Math.Max(0, width - (contentBox ? node.Padding.Horizontal + node.Border.Horizontal : 0)))) };
        if (height < node.Frame.Height - 0.01f)
            style = style with { MinHeight = SizeValue.Auto, MaxHeight = SizeValue.Fixed(Length.Px(Math.Max(0, height - (contentBox ? node.Padding.Vertical + node.Border.Vertical : 0)))) };
        if (style == node.Style) return false;
        node.Style = style;
        layout.PopupConstrained = true;
        return true;
    }
}
