using Nazor.Graphics;
using Nazor.Rendering;

namespace Nazor.Presentation;

internal readonly record struct OverlayPlacementRequest(Rect Anchor, Rect Bounds, float Shadow, string Side, string Alignment, float Offset);

public sealed partial class Scene
{
    internal Func<Rect, Rect?>? NativePopupWorkArea { get; set; }
    internal Func<ElementNode, OverlayPlacementRequest, Rect?>? ArrangeNativePopup { get; set; }
    internal Action? NativePopupLayoutStarting { get; set; }

    private Rect WindowViewport => new(0, 0, Viewport.Width, Viewport.Height);

    private bool OffersNativePopup(ElementNode element) =>
        ArrangeNativePopup is not null && element.Attributes.GetValueOrDefault("modal") is not true;

    internal Rect? GetNativePopupBounds(ElementNode overlay) => overlay.GetFeature<ElementLayout>()?.NativePopupBounds;

    internal Rect GetElementViewport(ElementNode element)
    {
        for (ElementNode? ancestor = element; ancestor is not null; ancestor = ancestor.ParentElement)
            if (IsOverlay(ancestor)) return GetNativePopupBounds(ancestor) ?? WindowViewport;
        return WindowViewport;
    }

    internal DisplayList BuildNativePopupList(ElementNode overlay, Rect bounds)
    {
        var outer = bounds.Inflate(overlay.GetFeature<ElementLayout>()!.NativePopupShadow);
        var builder = new DisplayListBuilder();
        builder.Add(new PushTranslate(-outer.X, -outer.Y));
        inks.Clear();
        building = true;
        try
        {
            inView = outer;
            Paint(overlay, builder);
            builder.Add(Pop.Instance);
            return builder.Build(new(outer.Width, outer.Height));
        }
        finally
        {
            building = false;
            inks.Clear();
        }
    }
}
