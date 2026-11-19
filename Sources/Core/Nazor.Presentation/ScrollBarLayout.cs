using Nazor.Graphics;
using Nazor.Layout;
using Nazor.Rendering;
using Nazor.Style;

namespace Nazor.Presentation;

public sealed partial class Scene
{
    private Size scrollBarLayoutViewport;
    private readonly List<LayoutNode> scrollBarContainers = [];

    private static bool ReservesScrollBars(ComputedStyle style) =>
        style.Get(StyleProperties.ScrollbarPlacement) == ScrollbarPlacement.Reserve &&
        style.Get(StyleProperties.ScrollbarVisibility) == ScrollbarVisibility.Visible;

    private float ScrollBarExtent(ElementNode element, ComputedStyle style)
    {
        var width = (float)Effective(element, style, StyleProperties.ScrollbarWidth).Resolve(0);
        return width <= 0 ? 0 : width + 2 * (float)Effective(element, style, StyleProperties.ScrollbarInset).Resolve(0);
    }

    private Thickness RetainedScrollBarGutter(ElementNode element, ComputedStyle style)
    {
        if (!ReservesScrollBars(style) || element.GetFeature<ElementLayout>() is not { } layout) return Thickness.Zero;
        var previous = layout.Node.Style.ScrollbarGutter;
        var extent = ScrollBarExtent(element, style);
        return new Thickness(0, 0, previous.Right > 0 ? extent : 0, previous.Bottom > 0 ? extent : 0);
    }

    private int LayoutWithScrollBars(bool reset)
    {
        var containers = scrollBarContainers;
        containers.Clear();
        CollectScrollContainers(root, containers);
        if (reset || scrollBarLayoutViewport != Viewport)
            foreach (var node in containers)
                if (!node.Style.ScrollbarGutter.IsZero) node.Style = node.Style with { ScrollbarGutter = Thickness.Zero };
        scrollBarLayoutViewport = Viewport;
        var layouts = LayoutEngine.Compute(root, Viewport, Scale);
        for (var pass = 0; pass < containers.Count * 2; pass++)
        {
            var changed = false;
            foreach (var node in containers)
            {
                var element = (ElementNode)node.Tag!;
                var style = element.GetFeature<ElementLayout>()!.Style!;
                var extent = ScrollBarExtent(element, style);
                var width = node.Frame.Width - node.Border.Horizontal;
                var height = node.Frame.Height - node.Border.Vertical;
                var gutter = node.Style.ScrollbarGutter;
                var (right, bottom) = ContentExtent(node);
                var contentWidth = right + node.Padding.Right - gutter.Right - node.Border.Left;
                var contentHeight = bottom + node.Padding.Bottom - gutter.Bottom - node.Border.Top;
                var vertical = style.Get(StyleProperties.OverflowY) is Overflow.Auto or Overflow.Scroll &&
                    contentHeight > height - gutter.Bottom + 0.01f;
                var horizontal = style.Get(StyleProperties.OverflowX) is Overflow.Auto or Overflow.Scroll &&
                    contentWidth > width - gutter.Right + 0.01f;
                var next = new Thickness(0, 0,
                    width >= extent && extent > 0 && (gutter.Right > 0 || vertical) ? extent : 0,
                    height >= extent && extent > 0 && (gutter.Bottom > 0 || horizontal) ? extent : 0);
                if (next == gutter) continue;
                node.Style = node.Style with { ScrollbarGutter = next };
                changed = true;
            }
            if (!changed) break;
            layouts += LayoutEngine.Compute(root, Viewport, Scale);
        }
        containers.Clear();
        return layouts;
    }

    private static void CollectScrollContainers(LayoutNode node, List<LayoutNode> containers)
    {
        if (node.Tag is ElementNode element && element.GetFeature<ElementLayout>()?.Style is { } style && ReservesScrollBars(style) &&
            (style.Get(StyleProperties.OverflowX) is Overflow.Auto or Overflow.Scroll ||
             style.Get(StyleProperties.OverflowY) is Overflow.Auto or Overflow.Scroll)) containers.Add(node);
        var children = node.Children;
        for (var i = 0; i < children.Count; i++) CollectScrollContainers(children[i], containers);
    }

    private static Rect ScrollViewport(LayoutNode node) => node.RoundedFrame.Deflate(node.Border + node.Style.ScrollbarGutter);
}
