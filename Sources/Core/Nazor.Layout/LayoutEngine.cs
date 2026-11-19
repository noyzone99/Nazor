using Nazor.Graphics;
using Nazor.Style;

namespace Nazor.Layout;

/// <summary>
/// Flexbox (CSS Flexible Box Layout §9, single- and multi-line, with order) plus block stacking, as summarised in
/// Docs/Guide/20-Layout-and-Scrolling.md. Sizes are border-box (box-sizing: border-box by default). <c>min-*: auto</c> on
/// flex items is the content-based minimum; scroll containers use 0. Flexible lengths are resolved with the freeze
/// loop of §9.7.
/// </summary>
public static partial class LayoutEngine
{
    [ThreadStatic] private static int computations;

    /// <summary>
    /// Lays out <paramref name="root" /> as the viewport and snaps edges to the pixel grid of
    /// <paramref name="scale" />. Returns how many node layouts or measurements were computed (not served from
    /// caches).
    /// </summary>
    /// <param name="root">The root (LayoutNode).</param>
    /// <param name="viewport">The viewport dimensions used to evaluate the media query.</param>
    /// <param name="scale">The number of physical pixels per logical unit.</param>
    /// <returns>The resulting int value.</returns>
    public static int Compute(LayoutNode root, Size viewport, float scale = 1)
    {
        computations = 0;
        var box = BoxModel.Resolve(root.Style, viewport.Width, viewport.Height);
        root.Padding = box.Padding;
        root.Border = box.Border;
        root.Margin = Thickness.Zero;
        root.Frame = new Rect(0, 0, viewport.Width, viewport.Height);
        Layout(root, box, viewport.Width, viewport.Height, AvailableSpace.Definite(viewport.Width), AvailableSpace.Definite(viewport.Height), perform: true);
        Finish(root, Point.Zero, scale);
        return computations;
    }

    private static void Finish(LayoutNode node, Point origin, float scale)
    {
        var absolute = node.Frame.Offset(origin.X, origin.Y);
        node.AbsoluteFrame = absolute;
        float Snap(float value) => MathF.Round(value * scale) / scale;
        var x0 = Snap(absolute.X);
        var y0 = Snap(absolute.Y);
        node.RoundedFrame = new Rect(x0, y0, Snap(absolute.Right) - x0, Snap(absolute.Bottom) - y0);
        if (node.ArrangeChildren is { } arrange)
        {
            var frames = arrange(node);
            if (frames.Count != node.Children.Count) throw new InvalidOperationException("Child arrangement must return one frame per retained child.");
            for (var index = 0; index < frames.Count; index++)
            {
                var frame = frames[index];
                if (!float.IsFinite(frame.X) || !float.IsFinite(frame.Y) || !float.IsFinite(frame.Width) || !float.IsFinite(frame.Height) || frame.Width < 0 || frame.Height < 0)
                    throw new InvalidOperationException("Child arrangement returned invalid geometry.");
                var child = node.Children[index];
                child.Frame = frame;
                var childBox = BoxModel.Resolve(child.Style, frame.Width, frame.Height);
                child.Padding = childBox.Padding;
                child.Border = childBox.Border;
                child.Margin = childBox.Margin;
            }
        }
        node.ClearDirty();
        foreach (var child in node.Children)
        {

            if (child.Style.Position == Position.Absolute && child.Style.Display != Display.None) LayoutAbsolute(child, node);
            Finish(child, absolute.Position, scale);
        }
    }

    private static void LayoutAbsolute(LayoutNode child, LayoutNode parent)
    {
        var block = parent;
        while (block.Parent is not null && block.Style.Position == Position.Static && !block.Style.Transformed) block = block.Parent;
        var container = block.AbsoluteFrame.Deflate(block.Border + block.Style.ScrollbarGutter);
        var style = child.Style;
        var box = ChildBox(child, container.Width, container.Height,
            AvailableSpace.Definite(container.Width - (Inset(style.Left, container.Width) ?? 0) - (Inset(style.Right, container.Width) ?? 0)));
        child.Padding = box.Padding;
        child.Border = box.Border;
        var left = Inset(style.Left, container.Width);
        var right = Inset(style.Right, container.Width);
        var top = Inset(style.Top, container.Height);
        var bottom = Inset(style.Bottom, container.Height);
        var margin = box.Margin;
        var availableWidth = Math.Max(0, container.Width - (left ?? 0) - (right ?? 0) - margin.Horizontal);
        var availableHeight = Math.Max(0, container.Height - (top ?? 0) - (bottom ?? 0) - margin.Vertical);

        float width;
        if (box.Width is { } fixedWidth) width = box.ClampWidth(fixedWidth);
        else if (left is not null && right is not null) width = box.ClampWidth(availableWidth);
        else
        {

            var maxContent = Layout(child, box, null, null, AvailableSpace.MaxContent, AvailableSpace.Definite(availableHeight), perform: false).Width;
            var minContent = Layout(child, box, null, null, AvailableSpace.MinContent, AvailableSpace.Definite(availableHeight), perform: false).Width;
            width = box.ClampWidth(Math.Min(maxContent, Math.Max(minContent, availableWidth)));
        }
        float height;
        if (box.Height is { } fixedHeight) height = box.ClampHeight(fixedHeight);
        else if (box.AspectRatio > 0 && !box.IntrinsicHeight) height = box.ClampHeight(box.HeightFor(width));
        else if (top is not null && bottom is not null && !box.IntrinsicHeight) height = box.ClampHeight(availableHeight);
        else height = Layout(child, box, width, null, AvailableSpace.Definite(width), AvailableSpace.Definite(availableHeight), perform: false).Height;

        var (marginLeft, marginRight) = AutoMargins(margin.Left, margin.Right, box.AutoMarginLeft, box.AutoMarginRight, left is not null && right is not null,
            container.Width - (left ?? 0) - (right ?? 0) - width - margin.Horizontal);
        var (marginTop, marginBottom) = AutoMargins(margin.Top, margin.Bottom, box.AutoMarginTop, box.AutoMarginBottom, top is not null && bottom is not null,
            container.Height - (top ?? 0) - (bottom ?? 0) - height - margin.Vertical);
        var outer = new Size(marginLeft + width + marginRight, marginTop + height + marginBottom);
        var origin = (left is null && right is null) || (top is null && bottom is null) ? StaticPosition(child, parent, outer) : Point.Zero;
        var x = left is { } l ? container.X + l + marginLeft : right is { } r ? container.Right - r - marginRight - width : origin.X + marginLeft;
        var y = top is { } t ? container.Y + t + marginTop : bottom is { } b ? container.Bottom - b - marginBottom - height : origin.Y + marginTop;

        child.Margin = new Thickness(marginLeft, marginTop, marginRight, marginBottom);
        child.Frame = new Rect(x - parent.AbsoluteFrame.X, y - parent.AbsoluteFrame.Y, width, height);
        Layout(child, box, width, height, AvailableSpace.Definite(width), AvailableSpace.Definite(height), perform: true);
    }

    private static (float Start, float End) AutoMargins(float start, float end, bool autoStart, bool autoEnd, bool bothInsets, float free)
    {
        if (!bothInsets || (!autoStart && !autoEnd)) return (start, end);
        if (autoStart && autoEnd) return free >= 0 ? (free / 2, free / 2) : (0, free);
        return autoStart ? (free, end) : (start, free);
    }

    private static Point StaticPosition(LayoutNode child, LayoutNode parent, Size outer)
    {
        var content = parent.AbsoluteFrame.Deflate(parent.Padding + parent.Border);
        var style = parent.Style;

        if (style.Display == Display.Grid) return parent.AbsoluteFrame.Deflate(parent.Border).Position;
        if (style.Display != Display.Flex)
        {
            var y = content.Y;
            foreach (var sibling in parent.Children)
            {
                if (sibling == child) break;
                if (sibling.Style.Display == Display.None || sibling.Style.Position == Position.Absolute) continue;
                y = parent.AbsoluteFrame.Y + sibling.Frame.Bottom + sibling.Margin.Bottom;
            }
            return new Point(content.X, y);
        }
        var row = style.FlexDirection is FlexDirection.Row or FlexDirection.RowReverse;
        var mainFree = row ? content.Width - outer.Width : content.Height - outer.Height;
        var crossFree = row ? content.Height - outer.Height : content.Width - outer.Width;
        var main = style.JustifyContent switch
        {
            JustifyContent.FlexEnd => mainFree,
            JustifyContent.Center or JustifyContent.SpaceAround or JustifyContent.SpaceEvenly => mainFree / 2,
            _ => 0,
        };
        if (style.FlexDirection is FlexDirection.RowReverse or FlexDirection.ColumnReverse) main = mainFree - main;
        var align = child.Style.AlignSelf == AlignSelf.Auto ? (AlignSelf)((int)style.AlignItems + 1) : child.Style.AlignSelf;
        var cross = align switch
        {
            AlignSelf.FlexEnd => crossFree,
            AlignSelf.Center => crossFree / 2,
            _ => 0,
        };
        if (style.FlexWrap == FlexWrap.WrapReverse) cross = crossFree - cross;
        return row ? new Point(content.X + main, content.Y + cross) : new Point(content.X + cross, content.Y + main);
    }

    private static BoxModel ChildBox(LayoutNode child, float? containingWidth, float? containingHeight, AvailableSpace availableWidth)
    {
        var box = BoxModel.Resolve(child.Style, containingWidth, containingHeight);
        var style = child.Style;
        if (!style.Width.IsIntrinsic && !style.MinWidth.IsIntrinsic && !style.MaxWidth.IsIntrinsic) return box;
        var margins = box.Margin.Horizontal;
        var width = style.Width.IsIntrinsic ? IntrinsicWidth(child, box, style.Width.Kind, availableWidth, margins) : box.Width;
        var min = style.MinWidth.IsIntrinsic ? IntrinsicWidth(child, box, style.MinWidth.Kind, availableWidth, margins) : box.MinWidth;
        var max = style.MaxWidth.IsIntrinsic ? IntrinsicWidth(child, box, style.MaxWidth.Kind, availableWidth, margins) : box.MaxWidth;
        return (box with { Width = width, MinWidth = min, MaxWidth = max }).TransferRatio();
    }

    private static float IntrinsicWidth(LayoutNode node, BoxModel box, SizeKind kind, AvailableSpace available, float margins)
    {
        float Measure(AvailableSpace space) => Layout(node, box, null, null, space, AvailableSpace.MaxContent, perform: false).Width;
        return kind switch
        {
            SizeKind.MinContent => Measure(AvailableSpace.MinContent),
            SizeKind.MaxContent => Measure(AvailableSpace.MaxContent),
            _ => available.Kind switch
            {
                AvailableKind.Definite => Math.Min(Measure(AvailableSpace.MaxContent), Math.Max(Measure(AvailableSpace.MinContent), available.Value - margins)),
                AvailableKind.MinContent => Measure(AvailableSpace.MinContent),
                _ => Measure(AvailableSpace.MaxContent),
            },
        };
    }

    private static (float X, float Y) RelativeOffset(LayoutStyle style, float? width, float? height)
    {
        var dx = Inset(style.Left, width) ?? -Inset(style.Right, width) ?? 0;
        var dy = Inset(style.Top, height) ?? -Inset(style.Bottom, height) ?? 0;
        return (dx, dy);
    }

    private static float? Inset(SizeValue value, float? basis) =>
        value.Kind == SizeKind.Fixed && (!value.Length.IsPercent || basis is not null) ? value.Length.At(basis ?? 0) : null;

    private static Size Layout(LayoutNode node, BoxModel box, float? knownWidth, float? knownHeight,
        AvailableSpace availableWidth, AvailableSpace availableHeight, bool perform, bool definiteWidth = true, bool definiteHeight = true)
    {
        definiteWidth = knownWidth is null ? box.Width is not null : definiteWidth || box.Width is not null;
        definiteHeight = knownHeight is null ? box.Height is not null : definiteHeight || box.Height is not null;
        var width = knownWidth ?? (box.Width is { } specifiedWidth ? box.ClampWidth(specifiedWidth) : (float?)null);
        var height = knownHeight ?? (box.Height is { } specifiedHeight ? box.ClampHeight(specifiedHeight) : (float?)null);

        if (box.AspectRatio > 0)
        {
            if (width is { } w && height is null && !box.IntrinsicHeight)
            {
                height = box.ClampHeight(box.HeightFor(w));
                definiteHeight = definiteWidth;
            }
            else if (height is { } h && width is null)
            {
                width = box.ClampWidth(box.WidthFor(h));
                definiteWidth = definiteHeight;
            }
        }
        var key = new CacheKey(width, height, availableWidth, availableHeight, box, definiteWidth, definiteHeight);

        if (!perform || (!node.IsDirty && node.LastLayout == key))
        {
            foreach (var (cachedKey, size) in node.MeasureCache)
                if (cachedKey == key) return size;
        }

        computations++;
        var result = (node.Style.Display == Display.Grid || node.Style.TablePart == "table") ? LayoutGrid(node, box, width, height, availableWidth, availableHeight, perform, definiteWidth, definiteHeight) :
            node.Children.Count == 0 || node.ArrangeChildren is not null ? LayoutLeaf(node, box, width, height, availableWidth, availableHeight) :
            LayoutContainer(node, box, width, height, availableWidth, availableHeight, perform, definiteWidth, definiteHeight);
        if (node.MeasureCache.Count >= 8) node.MeasureCache.RemoveAt(0);
        node.MeasureCache.Add((key, result));
        if (perform) node.LastLayout = key;
        return result;
    }

    private static Size LayoutLeaf(LayoutNode node, BoxModel box, float? width, float? height, AvailableSpace availableWidth, AvailableSpace availableHeight)
    {
        var edges = box.Padding + box.Border;
        if (node.Measure is { } measure && (width is null || height is null))
        {
            var content = measure(
                width.Map(w => Math.Max(0, w - edges.Horizontal)), height.Map(h => Math.Max(0, h - edges.Vertical)),
                availableWidth.Shrink(edges.Horizontal), availableHeight.Shrink(edges.Vertical));
            if (!float.IsFinite(content.Width) || !float.IsFinite(content.Height) || content.Width < 0 || content.Height < 0)
                throw new InvalidOperationException($"The measure function of {node.Tag} returned an invalid size {content}.");
            width ??= box.ClampWidth(content.Width + edges.Horizontal);
            height ??= box.ClampHeight(content.Height + edges.Vertical);
        }
        return new Size(width ?? box.ClampWidth(edges.Horizontal), height ?? box.ClampHeight(edges.Vertical));
    }

    private sealed class Item
    {
        public required LayoutNode Node;
        public required BoxModel Box;
        public float Base;
        public float Hypothetical;
        public float Target;
        public float MinMain;
        public float MaxMain;
        public bool Frozen;
        public float Main;
        public float Cross;
        public float MarginMainStart;
        public float MarginMainEnd;
        public float MarginCrossStart;
        public float MarginCrossEnd;
        public float Grow;
        public float Shrink;
        public float Edges;
        public bool CrossDefinite;
        public AlignSelf Align;
        public float OuterMain => Main + MarginMainStart + MarginMainEnd;
    }

    private static Size LayoutContainer(LayoutNode node, BoxModel box, float? width, float? height,
        AvailableSpace availableWidth, AvailableSpace availableHeight, bool perform, bool definiteWidth, bool definiteHeight)
    {
        var style = node.Style;
        var block = style.Display != Display.Flex;
        var row = !block && style.FlexDirection is FlexDirection.Row or FlexDirection.RowReverse;
        var reverse = !block && style.FlexDirection is FlexDirection.RowReverse or FlexDirection.ColumnReverse;
        var wrap = !block && style.FlexWrap != FlexWrap.NoWrap;
        var edges = box.Padding + box.Border;
        var innerWidth = width.Map(w => Math.Max(0, w - edges.Horizontal));
        var innerHeight = height.Map(h => Math.Max(0, h - edges.Vertical));

        var percentWidth = definiteWidth ? innerWidth : null;
        var percentHeight = definiteHeight ? innerHeight : null;
        var innerMain = row ? innerWidth : innerHeight;
        var innerCross = row ? innerHeight : innerWidth;
        var mainDefinite = row ? definiteWidth && innerWidth is not null : definiteHeight && innerHeight is not null;
        var crossDefinite = row ? definiteHeight && innerHeight is not null : definiteWidth && innerWidth is not null;
        var mainEdges = row ? edges.Horizontal : edges.Vertical;
        var crossEdges = row ? edges.Vertical : edges.Horizontal;
        var availableMain = Inner(row ? availableWidth : availableHeight, innerMain, mainEdges);
        var availableCross = Inner(row ? availableHeight : availableWidth, innerCross, crossEdges);
        var gapLength = row ? style.ColumnGap : style.RowGap;
        var gap = block ? 0 : gapLength.At((row ? percentWidth : percentHeight) ?? 0);

        var crossGap = wrap ? (row ? style.RowGap : style.ColumnGap).At((row ? percentHeight : percentWidth) ?? 0) : 0;

        var items = new List<Item>();
        foreach (var child in node.Children)
        {
            if (child.Style.Display == Display.None)
            {
                child.Frame = Rect.Empty;
                continue;
            }

            if (child.Style.Position == Position.Absolute) continue;
            var childBox = ChildBox(child, percentWidth, percentHeight, row ? availableMain : availableCross);
            child.Padding = childBox.Padding;
            child.Border = childBox.Border;
            child.Margin = childBox.Margin;
            var item = new Item
            {
                Node = child,
                Box = childBox,
                Grow = block ? 0 : child.Style.FlexGrow,
                Shrink = block ? 0 : child.Style.FlexShrink,

                Align = block ? (child.Style.IsReplaced ? AlignSelf.FlexStart : AlignSelf.Stretch)
                    : child.Style.AlignSelf == AlignSelf.Auto ? (AlignSelf)((int)style.AlignItems + 1) : child.Style.AlignSelf,
                Edges = row ? childBox.Padding.Horizontal + childBox.Border.Horizontal : childBox.Padding.Vertical + childBox.Border.Vertical,
                MarginMainStart = row ? childBox.Margin.Left : childBox.Margin.Top,
                MarginMainEnd = row ? childBox.Margin.Right : childBox.Margin.Bottom,
                MarginCrossStart = row ? childBox.Margin.Top : childBox.Margin.Left,
                MarginCrossEnd = row ? childBox.Margin.Bottom : childBox.Margin.Right,
            };
            ComputeBase(item, row, mainDefinite ? innerMain : null, innerCross, crossDefinite, availableMain, availableCross);
            items.Add(item);
        }

        if (!block && items.Any(i => i.Node.Style.Order != 0)) items = [.. items.OrderBy(i => i.Node.Style.Order)];

        var limit = wrap ? WrapLimit(box, row, innerMain, availableMain, mainEdges) : float.PositiveInfinity;
        var lines = wrap ? CollectLines(items, gap, limit) : [items];

        var containerMain = innerMain ?? Math.Max(0, ClampInner(lines.Count > 1 ? Math.Max(limit, lines.Max(l => LineMain(l, gap))) : LineMain(lines[0], gap),
            row ? box.MinWidth : box.MinHeight, row ? box.MaxWidth : box.MaxHeight, mainEdges));
        foreach (var line in lines) ResolveFlexibleLengths(line, containerMain, Gaps(line.Count, gap));

        foreach (var item in items)
        {
            var preferredCross = row ? item.Box.Height : item.Box.Width;
            if (preferredCross is { } fixedCross)
            {
                item.Cross = row ? item.Box.ClampHeight(fixedCross) : item.Box.ClampWidth(fixedCross);
                continue;
            }
            var size = Layout(item.Node, item.Box, row ? item.Main : null, row ? null : item.Main,
                row ? AvailableSpace.Definite(item.Main) : availableCross.Shrink(item.MarginCrossStart + item.MarginCrossEnd),
                row ? availableCross.Shrink(item.MarginCrossStart + item.MarginCrossEnd) : AvailableSpace.Definite(item.Main), perform: false,
                definiteWidth: !row || mainDefinite, definiteHeight: row || mainDefinite);
            item.Cross = row ? size.Height : size.Width;
        }
        var crossMin = row ? box.MinHeight : box.MinWidth;
        var crossMax = row ? box.MaxHeight : box.MaxWidth;
        var lineCross = new float[lines.Count];
        for (var l = 0; l < lines.Count; l++)
        {
            var content = lines[l].Select(i => i.Cross + i.MarginCrossStart + i.MarginCrossEnd).DefaultIfEmpty(0).Max();

            lineCross[l] = wrap ? content : innerCross ?? Math.Max(0, ClampInner(content, crossMin, crossMax, crossEdges));
        }
        var containerCross = wrap
            ? innerCross ?? Math.Max(0, ClampInner(lineCross.Sum() + Gaps(lines.Count, crossGap), crossMin, crossMax, crossEdges))
            : lineCross[0];

        var finalWidth = width ?? box.ClampWidth((row ? containerMain : containerCross) + edges.Horizontal);
        var finalHeight = height ?? box.ClampHeight((row ? containerCross : containerMain) + edges.Vertical);
        if (!perform) return new Size(finalWidth, finalHeight);

        var usedMain = row ? finalWidth - edges.Horizontal : finalHeight - edges.Vertical;
        var usedCross = row ? finalHeight - edges.Vertical : finalWidth - edges.Horizontal;
        if (!wrap) lineCross[0] = usedCross;
        var wrapReverse = wrap && style.FlexWrap == FlexWrap.WrapReverse;
        var lineStarts = AlignLines(lineCross, style.AlignContent, usedCross, crossGap, wrapReverse);
        for (var l = 0; l < lines.Count; l++)
        {

            foreach (var item in lines[l])
            {
                var autoCross = row ? item.Box.AutoHeight : item.Box.AutoWidth;
                var autoMargins = row ? item.Box.AutoMarginTop || item.Box.AutoMarginBottom : item.Box.AutoMarginLeft || item.Box.AutoMarginRight;
                item.CrossDefinite = !autoCross;
                if (item.Align == AlignSelf.Stretch && autoCross && !autoMargins)
                {
                    var stretched = Math.Max(0, lineCross[l] - item.MarginCrossStart - item.MarginCrossEnd);
                    item.Cross = row ? item.Box.ClampHeight(stretched) : item.Box.ClampWidth(stretched);
                    item.CrossDefinite = wrap || crossDefinite;
                }
            }
            PlaceLine(lines[l], style, block, row, reverse, wrapReverse, usedMain, lineStarts[l], lineCross[l], gap, edges, mainDefinite, percentWidth, percentHeight);
        }
        return new Size(finalWidth, finalHeight);
    }

    private static float Gaps(int count, float gap) => count > 1 ? gap * (count - 1) : 0;

    private static float LineMain(List<Item> line, float gap) => line.Sum(i => i.Hypothetical + i.MarginMainStart + i.MarginMainEnd) + Gaps(line.Count, gap);

    private static float WrapLimit(BoxModel box, bool row, float? innerMain, AvailableSpace availableMain, float mainEdges)
    {
        if (innerMain is { } main) return main;
        var max = (row ? box.MaxWidth : box.MaxHeight) - mainEdges;
        if (!row) return max;
        return availableMain.Kind switch
        {
            AvailableKind.Definite => Math.Min(availableMain.Value, max),
            AvailableKind.MinContent => 0,
            _ => max,
        };
    }

    private static List<List<Item>> CollectLines(List<Item> items, float gap, float limit)
    {
        var lines = new List<List<Item>>();
        var current = new List<Item>();
        var used = 0f;
        foreach (var item in items)
        {
            var outer = item.Hypothetical + item.MarginMainStart + item.MarginMainEnd;

            if (current.Count > 0 && used + gap + outer > limit + 1e-3f)
            {
                lines.Add(current);
                current = [];
                used = 0;
            }
            used += (current.Count > 0 ? gap : 0) + outer;
            current.Add(item);
        }
        lines.Add(current);
        return lines;
    }

    private static float[] AlignLines(float[] lineCross, AlignContent align, float usedCross, float gap, bool wrapReverse)
    {
        var count = lineCross.Length;
        var free = usedCross - lineCross.Sum() - Gaps(count, gap);
        var start = 0f;
        var between = gap;
        switch (align)
        {
            case AlignContent.Normal or AlignContent.Stretch when free > 0:
                for (var l = 0; l < count; l++) lineCross[l] += free / count;
                break;
            case AlignContent.FlexEnd:
                start = free;
                break;
            case AlignContent.Center:
                start = free / 2;
                break;
            case AlignContent.SpaceBetween when free > 0 && count > 1:
                between += free / (count - 1);
                break;
            case AlignContent.SpaceAround when free > 0:
                between += free / count;
                start = free / count / 2;
                break;
            case AlignContent.SpaceEvenly when free > 0:
                between += free / (count + 1);
                start = free / (count + 1);
                break;
        }
        var starts = new float[count];
        var position = start;
        for (var l = 0; l < count; l++)
        {
            starts[l] = wrapReverse ? usedCross - position - lineCross[l] : position;
            position += lineCross[l] + between;
        }
        return starts;
    }

    private static AvailableSpace Inner(AvailableSpace available, float? inner, float edges) =>
        inner is { } value ? AvailableSpace.Definite(value) : available.Shrink(edges);

    private static float ClampInner(float inner, float? min, float max, float edges)
    {
        var outer = inner + edges;
        if (outer > max) outer = max;
        if (min is { } m && outer < m) outer = m;
        return outer - edges;
    }

    private static void ComputeBase(Item item, bool row, float? innerMain, float? innerCross, bool crossDefinite, AvailableSpace availableMain, AvailableSpace availableCross)
    {
        var box = item.Box;
        var style = item.Node.Style;
        float? basis = null;
        if (style.FlexBasis.Kind == SizeKind.Fixed && (!style.FlexBasis.Length.IsPercent || innerMain is not null))
            basis = box.SizeFromStyle(style.FlexBasis.Length.At(innerMain ?? 0), row);
        else if (style.FlexBasis.Kind == SizeKind.Auto)
            basis = row ? box.Width : box.Height;

        else if (style.FlexBasis.IsIntrinsic && row)
            basis = IntrinsicWidth(item.Node, box with { Width = null }, style.FlexBasis.Kind, availableMain, item.MarginMainStart + item.MarginMainEnd);
        if (basis is null)
        {
            var crossKnown = CrossForMeasure(item, row, innerCross);
            var size = Layout(item.Node, box, row ? null : crossKnown, row ? crossKnown : null,
                row ? (availableMain.IsDefinite ? AvailableSpace.MaxContent : availableMain) : availableCross.Shrink(item.MarginCrossStart + item.MarginCrossEnd),
                row ? availableCross.Shrink(item.MarginCrossStart + item.MarginCrossEnd) : (availableMain.IsDefinite ? AvailableSpace.MaxContent : availableMain),
                perform: false, definiteWidth: row || crossDefinite, definiteHeight: !row || crossDefinite);
            basis = row ? size.Width : size.Height;
        }
        item.Base = basis.Value;
        item.MaxMain = row ? box.MaxWidth : box.MaxHeight;
        item.MinMain = (row ? box.MinWidth : box.MinHeight) ?? AutomaticMinimum(item, row, innerCross);
        if (item.MinMain > item.MaxMain) item.MaxMain = item.MinMain;
        item.Hypothetical = Math.Clamp(item.Base, item.MinMain, item.MaxMain);
    }

    private static float? CrossForMeasure(Item item, bool row, float? innerCross)
    {
        var preferred = row ? item.Box.Height : item.Box.Width;
        if (preferred is not null) return preferred;
        if (item.Align == AlignSelf.Stretch && innerCross is { } cross && (row ? item.Box.AutoHeight : item.Box.AutoWidth))
        {
            // A stretched item is measured at the size it will get, so its min and max cross sizes apply: text measured
            // wider than its max-width would report too few lines.
            var stretched = Math.Max(0, cross - item.MarginCrossStart - item.MarginCrossEnd);
            return row ? item.Box.ClampHeight(stretched) : item.Box.ClampWidth(stretched);
        }
        return null;
    }

    private static float AutomaticMinimum(Item item, bool row, float? innerCross)
    {
        var overflow = row ? item.Node.Style.OverflowX : item.Node.Style.OverflowY;
        if (overflow != Overflow.Visible) return 0;
        var crossKnown = CrossForMeasure(item, row, innerCross);
        var size = Layout(item.Node, item.Box, row ? null : crossKnown, row ? crossKnown : null,
            row ? AvailableSpace.MinContent : AvailableSpace.MaxContent, row ? AvailableSpace.MaxContent : AvailableSpace.MinContent, perform: false);
        var content = row ? size.Width : size.Height;
        var specified = row ? item.Box.Width : item.Box.Height;
        var minimum = specified is { } s ? Math.Min(s, content) : content;
        return Math.Min(minimum, row ? item.Box.MaxWidth : item.Box.MaxHeight);
    }

    private static void ResolveFlexibleLengths(List<Item> items, float containerMain, float gaps)
    {
        var sumHypothetical = items.Sum(i => i.Hypothetical + i.MarginMainStart + i.MarginMainEnd) + gaps;
        var growing = sumHypothetical < containerMain;
        foreach (var item in items)
        {
            item.Target = item.Base;
            item.Frozen = false;
            var factor = growing ? item.Grow : item.Shrink;
            if (factor == 0 || (growing && item.Base > item.Hypothetical) || (!growing && item.Base < item.Hypothetical))
            {
                item.Frozen = true;
                item.Target = item.Hypothetical;
            }
        }
        float FreeSpace() => containerMain - gaps - items.Sum(i => (i.Frozen ? i.Target : i.Base) + i.MarginMainStart + i.MarginMainEnd);
        var initialFree = FreeSpace();
        for (var guard = 0; guard <= items.Count && items.Any(i => !i.Frozen); guard++)
        {
            var active = items.Where(i => !i.Frozen).ToArray();
            var free = FreeSpace();
            var factorSum = active.Sum(i => growing ? i.Grow : i.Shrink);
            if (factorSum < 1 && Math.Abs(initialFree * factorSum) < Math.Abs(free)) free = initialFree * factorSum;
            if (free != 0)
            {
                if (growing)
                {
                    foreach (var item in active) item.Target = item.Base + free * (item.Grow / factorSum);
                }
                else
                {
                    var scaledSum = active.Sum(i => i.Shrink * InnerBase(i));
                    foreach (var item in active)
                        item.Target = scaledSum == 0 ? item.Base : item.Base - Math.Abs(free) * (item.Shrink * InnerBase(item) / scaledSum);
                }
            }
            else
            {
                foreach (var item in active) item.Target = item.Base;
            }
            var totalViolation = 0f;
            var violations = new float[active.Length];
            for (var i = 0; i < active.Length; i++)
            {
                var item = active[i];
                var floor = Math.Max(item.MinMain, MainEdges(item));
                var clamped = Math.Clamp(item.Target, floor, Math.Max(floor, item.MaxMain));
                violations[i] = clamped - item.Target;
                totalViolation += violations[i];
                item.Target = clamped;
            }
            for (var i = 0; i < active.Length; i++)
            {
                if (totalViolation == 0 || (totalViolation > 0 && violations[i] > 0) || (totalViolation < 0 && violations[i] < 0))
                    active[i].Frozen = true;
            }
        }
        foreach (var item in items) item.Main = item.Target;

        static float InnerBase(Item item) => Math.Max(0, item.Base - MainEdges(item));
    }

    private static float MainEdges(Item item) => item.Edges;

    private static void PlaceLine(List<Item> items, LayoutStyle style, bool block, bool row, bool reverse, bool wrapReverse,
        float usedMain, float lineStart, float lineCross, float gap, Thickness edges, bool mainDefinite, float? percentWidth, float? percentHeight)
    {
        var free = usedMain - items.Sum(i => i.OuterMain) - Gaps(items.Count, gap);
        var autoMargins = items.Sum(i => (row ? (i.Box.AutoMarginLeft ? 1 : 0) + (i.Box.AutoMarginRight ? 1 : 0) : (i.Box.AutoMarginTop ? 1 : 0) + (i.Box.AutoMarginBottom ? 1 : 0)));
        var start = 0f;
        var between = gap;
        if (free > 0 && autoMargins > 0)
        {
            var share = free / autoMargins;
            foreach (var item in items)
            {
                if (row ? item.Box.AutoMarginLeft : item.Box.AutoMarginTop) item.MarginMainStart = share;
                if (row ? item.Box.AutoMarginRight : item.Box.AutoMarginBottom) item.MarginMainEnd = share;
            }
            free = 0;
        }
        var justify = block ? JustifyContent.FlexStart : style.JustifyContent;
        var count = items.Count;
        switch (justify)
        {
            case JustifyContent.FlexEnd:
                start = free;
                break;
            case JustifyContent.Center:
                start = free / 2;
                break;
            case JustifyContent.SpaceBetween when free > 0 && count > 1:
                between += free / (count - 1);
                break;
            case JustifyContent.SpaceAround when free > 0:
                between += free / count;
                start = free / count / 2;
                break;
            case JustifyContent.SpaceEvenly when free > 0:
                between += free / (count + 1);
                start = free / (count + 1);
                break;
        }
        var position = start;
        foreach (var item in items)
        {
            var mainOffset = reverse ? usedMain - position - item.OuterMain : position;
            var crossFree = lineCross - item.Cross - item.MarginCrossStart - item.MarginCrossEnd;
            var autoStart = row ? item.Box.AutoMarginTop : item.Box.AutoMarginLeft;
            var autoEnd = row ? item.Box.AutoMarginBottom : item.Box.AutoMarginRight;
            float crossOffset;

            if (autoStart || autoEnd) crossOffset = autoStart ? autoEnd ? crossFree / 2 : crossFree : 0;
            else
            {
                crossOffset = item.Align switch
                {
                    AlignSelf.FlexEnd => crossFree,
                    AlignSelf.Center => crossFree / 2,
                    _ => 0,
                };
                if (wrapReverse) crossOffset = crossFree - crossOffset;
            }
            var x = edges.Left + (row ? mainOffset + item.MarginMainStart : lineStart + crossOffset + item.MarginCrossStart);
            var y = edges.Top + (row ? lineStart + crossOffset + item.MarginCrossStart : mainOffset + item.MarginMainStart);
            var w = row ? item.Main : item.Cross;
            var h = row ? item.Cross : item.Main;
            if (item.Node.Style.Position == Position.Relative)
            {
                var (dx, dy) = RelativeOffset(item.Node.Style, percentWidth, percentHeight);
                x += dx;
                y += dy;
            }
            item.Node.Frame = new Rect(x, y, w, h);
            item.Node.Margin = row
                ? new Thickness(item.MarginMainStart, item.MarginCrossStart, item.MarginMainEnd, item.MarginCrossEnd)
                : new Thickness(item.MarginCrossStart, item.MarginMainStart, item.MarginCrossEnd, item.MarginMainEnd);

            Layout(item.Node, item.Box, w, h, AvailableSpace.Definite(w), AvailableSpace.Definite(h), perform: true,
                definiteWidth: row ? mainDefinite : item.CrossDefinite, definiteHeight: row ? item.CrossDefinite : mainDefinite);
            position += item.OuterMain + between;
        }
    }
}

internal static class NullableExtensions
{
    public static float? Map(this float? value, Func<float, float> map) => value is { } v ? map(v) : null;
}

internal readonly record struct BoxModel(Thickness Padding, Thickness Border, Thickness Margin,
    bool AutoMarginLeft, bool AutoMarginTop, bool AutoMarginRight, bool AutoMarginBottom,
    float? Width, float? Height, float? MinWidth, float? MinHeight, float MaxWidth, float MaxHeight, bool ContentBox,
    float AspectRatio = 0, bool IntrinsicHeight = false, Thickness Gutter = default)
{
    public bool AutoWidth => Width is null;

    public bool AutoHeight => Height is null && AspectRatio <= 0 && !IntrinsicHeight;

    public BoxModel TransferRatio()
    {
        if (AspectRatio <= 0) return this;
        if (Width is { } width && Height is null && !IntrinsicHeight) return this with { Height = ClampHeight(HeightFor(width)) };
        if (Height is { } height && Width is null) return this with { Width = ClampWidth(WidthFor(height)) };
        return this;
    }

    public float HeightFor(float width) => ContentBox
        ? Math.Max(0, width - CssEdges.Horizontal) / AspectRatio + CssEdges.Vertical
        : width / AspectRatio;

    public float WidthFor(float height) => ContentBox
        ? Math.Max(0, height - CssEdges.Vertical) * AspectRatio + CssEdges.Horizontal
        : height * AspectRatio;

    public static BoxModel Resolve(LayoutStyle style, float? containingWidth, float? containingHeight)
    {

        var basis = containingWidth ?? 0;
        var padding = new Thickness(style.PaddingLeft.At(basis), style.PaddingTop.At(basis), style.PaddingRight.At(basis), style.PaddingBottom.At(basis)) + style.ContentInsets + style.ScrollbarGutter;
        var border = style.Border;
        float Margin(SizeValue value) => value.Kind == SizeKind.Fixed ? value.Length.At(basis) : 0;
        var margin = new Thickness(Margin(style.MarginLeft), Margin(style.MarginTop), Margin(style.MarginRight), Margin(style.MarginBottom));
        var contentBox = style.BoxSizing == BoxSizing.ContentBox;
        var horizontal = padding.Horizontal + border.Horizontal - style.ScrollbarGutter.Horizontal;
        var vertical = padding.Vertical + border.Vertical - style.ScrollbarGutter.Vertical;
        float? Size(SizeValue value, float? containing, float edges)
        {
            if (value.Kind != SizeKind.Fixed) return null;
            if (value.Length.IsPercent && containing is null) return null;
            var resolved = value.Length.At(containing ?? 0);
            return contentBox ? resolved + edges : Math.Max(resolved, edges);
        }
        float Max(SizeValue value, float? containing, float edges) => Size(value, containing, edges) ?? float.PositiveInfinity;
        return new BoxModel(padding, border, margin,
            style.MarginLeft.Kind == SizeKind.Auto, style.MarginTop.Kind == SizeKind.Auto, style.MarginRight.Kind == SizeKind.Auto, style.MarginBottom.Kind == SizeKind.Auto,
            Size(style.Width, containingWidth, horizontal), Size(style.Height, containingHeight, vertical),
            Size(style.MinWidth, containingWidth, horizontal), Size(style.MinHeight, containingHeight, vertical),
            Max(style.MaxWidth, containingWidth, horizontal), Max(style.MaxHeight, containingHeight, vertical), contentBox,
            (float)style.AspectRatio.Ratio, style.Height.IsIntrinsic, style.ScrollbarGutter).TransferRatio();
    }

    public float ClampWidth(float value) => Clamp(value, MinWidth, MaxWidth, Padding.Horizontal + Border.Horizontal);

    public float ClampHeight(float value) => Clamp(value, MinHeight, MaxHeight, Padding.Vertical + Border.Vertical);

    public float SizeFromStyle(float value, bool row)
    {
        var edges = row ? CssEdges.Horizontal : CssEdges.Vertical;
        return ContentBox ? value + edges : value;
    }

    private Thickness CssEdges => new(Padding.Left + Border.Left - Gutter.Left, Padding.Top + Border.Top - Gutter.Top,
        Padding.Right + Border.Right - Gutter.Right, Padding.Bottom + Border.Bottom - Gutter.Bottom);

    private static float Clamp(float value, float? min, float max, float edges)
    {
        if (value > max) value = max;
        if (min is { } m && value < m) value = m;
        return Math.Max(value, edges);
    }
}

internal static class LengthExtensions
{
    public static float At(this Length length, float basis) => (float)length.Resolve(basis);
}
