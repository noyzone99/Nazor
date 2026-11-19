using System.Text;
using Nazor.Graphics;
using Nazor.Graphics.Text;
using Nazor.Layout;
using Nazor.Rendering;
using Nazor.Style;
using StyleColor = Nazor.Style.Color;

namespace Nazor.Presentation;

internal sealed record InlineSegment(ElementNode Owner, ComputedStyle Style, int Start, int End, TextRun Run);
internal sealed record InlinePart(InlineFlow Flow);

internal sealed class InlineFlow(ElementNode owner, ElementLayout layout, TextContent text)
{
    public ElementNode Owner { get; } = owner;
    public ElementLayout Layout { get; } = layout;
    public TextContent Text { get; } = text;
    public List<ElementNode> Elements { get; } = [];
    public List<InlineSegment> Segments { get; } = [];
    public Dictionary<ElementNode, (int Start, int End)> Ranges { get; } = [];
    public Dictionary<ElementNode, Rect[]> Fragments { get; } = [];
    public Dictionary<ElementNode, ArrangeFunction> Arrangers { get; } = [];
    public float Scale { get; set; }

    public IReadOnlyList<Rect> Arrange(LayoutNode parent)
    {
        UpdateFragments();
        return parent.Children.Select(child => child.Tag is ElementNode element ? Bounds(element).Offset(-parent.AbsoluteFrame.X, -parent.AbsoluteFrame.Y) : Rect.Empty).ToArray();
    }

    public Rect Bounds(ElementNode element)
    {
        if (!Fragments.TryGetValue(element, out var fragments)) return Rect.Empty;
        var bounds = Rect.Empty;
        foreach (var fragment in fragments)
            bounds = bounds.IsEmpty ? fragment : new Rect(Math.Min(bounds.X, fragment.X), Math.Min(bounds.Y, fragment.Y),
                Math.Max(bounds.Right, fragment.Right) - Math.Min(bounds.X, fragment.X), Math.Max(bounds.Bottom, fragment.Bottom) - Math.Min(bounds.Y, fragment.Y));
        return bounds;
    }

    public void UpdateFragments()
    {
        var paragraph = Text.Get(Layout.Node.ContentWidth);
        var origin = Layout.Node.ContentBox.Position;
        foreach (var element in Elements)
        {
            if (!Ranges.TryGetValue(element, out var range) || element.GetFeature<ElementLayout>()?.Style is not { } style) continue;
            var runStyle = TextContent.ToTextStyle(style);
            var fragments = new List<Rect>();
            foreach (var area in paragraph.GetSelectionRects(range.Start, range.End))
            {
                var line = paragraph.Lines.First(line => line.Top == area.Y);
                var run = line.Runs.FirstOrDefault(run => run.Start < range.End && run.Start + run.Length > range.Start);
                var face = run?.Face ?? paragraph.Primary;
                var ascent = face.Ascent(runStyle.FontSize);
                var descent = face.Descent(runStyle.FontSize);
                var height = LineHeight(runStyle, face);
                var edges = Edges(style, Layout.Node.ContentWidth);
                var shift = 0f;
                for (var current = element; current != Owner && current is not null; current = current.ParentElement)
                    if (current.GetFeature<ElementLayout>()?.Style is { } ancestorStyle) shift += Shift(ancestorStyle);
                var top = line.Baseline - ascent - (height - ascent - descent) / 2 - edges.Top - shift;
                var offset = Alignment(Layout.Style!, Layout.Node.ContentBox.Width, line.Width);
                float Snap(float value) => MathF.Round(value * Scale) / Scale;
                var x = Snap(origin.X + area.X + offset);
                var y = Snap(origin.Y + top);
                fragments.Add(new Rect(x, y, Snap(origin.X + area.Right + offset) - x,
                    Snap(origin.Y + top + height + edges.Vertical) - y));
            }
            Fragments[element] = fragments.ToArray();
        }
    }

    public static float LineHeight(TextStyle style, FontFace face) => style.LineHeightKind switch
    {
        Graphics.Text.LineHeightKind.Fixed => style.LineHeight,
        Graphics.Text.LineHeightKind.Multiplier => style.LineHeight * style.FontSize,
        _ => face.Ascent(style.FontSize) + face.Descent(style.FontSize) + face.LineGap(style.FontSize),
    };

    public static float Alignment(ComputedStyle style, float width, float lineWidth) => style.Get(StyleProperties.TextAlign) switch
    {
        TextAlign.Center => (width - lineWidth) / 2,
        TextAlign.End or TextAlign.Right => width - lineWidth,
        _ => 0,
    };

    public static float Shift(ComputedStyle style) => style.Get(StyleProperties.Position) == Position.Relative
        ? -(float)style.Get(StyleProperties.Top).Length.Value : 0;

    public static Thickness Edges(ComputedStyle style, float width) => new(
        (float)(style.Get(StyleProperties.PaddingLeft).Resolve(width) + style.Get(StyleProperties.BorderLeftWidth).Value),
        (float)(style.Get(StyleProperties.PaddingTop).Resolve(width) + style.Get(StyleProperties.BorderTopWidth).Value),
        (float)(style.Get(StyleProperties.PaddingRight).Resolve(width) + style.Get(StyleProperties.BorderRightWidth).Value),
        (float)(style.Get(StyleProperties.PaddingBottom).Resolve(width) + style.Get(StyleProperties.BorderBottomWidth).Value));
}

public sealed partial class Scene
{
    private bool TrySyncInline(ElementNode element, ElementLayout layout, ComputedStyle style, IReadOnlyList<Node> children)
    {
        if (style.Display is Display.Flex or Display.Grid || !children.OfType<ElementNode>().Any() || !children.All(CanInline))
        {
            ClearInline(layout);
            return false;
        }
        layout.Text ??= new TextContent(fonts, shaper, shapes);
        var flow = layout.Inline ??= new InlineFlow(element, layout, layout.Text);
        var previous = flow.Elements.ToArray();
        flow.Elements.Clear();
        flow.Segments.Clear();
        flow.Ranges.Clear();
        flow.Scale = Scale;
        var content = new StringBuilder();
        var pendingPieces = new List<(ElementNode Owner, ComputedStyle Style, StringBuilder Text)>();
        (ElementNode Owner, ComputedStyle Style)? pending = null;
        foreach (var child in children) Collect(child, element, style);
        var pieces = pendingPieces.Select(piece => (piece.Owner, piece.Style,
            Text: TextContent.Transform(piece.Text.ToString(), piece.Style.Get(StyleProperties.TextTransform)))).ToArray();
        var rangePosition = 0;
        flow.Ranges.Clear();
        foreach (var piece in pieces)
        {
            foreach (var ancestor in AncestorsWithin(piece.Owner, element))
            {
                var start = flow.Ranges.TryGetValue(ancestor, out var existing) ? existing.Start : rangePosition;
                flow.Ranges[ancestor] = (start, rangePosition + piece.Text.Length);
            }
            rangePosition += piece.Text.Length;
        }
        foreach (var inline in flow.Elements) flow.Ranges.TryAdd(inline, (0, 0));
        var runs = new List<TextRun>();
        var position = 0;
        foreach (var piece in pieces)
        {
            var start = position;
            position += piece.Text.Length;
            var ancestors = AncestorsWithin(piece.Owner, element).ToArray();
            var leading = 0f;
            var trailing = 0f;
            var top = 0f;
            var bottom = 0f;
            var shift = 0f;
            foreach (var ancestor in ancestors)
            {
                if (!flow.Ranges.TryGetValue(ancestor, out var range)) continue;
                var ancestorStyle = GetStyle(ancestor);
                shift += InlineFlow.Shift(ancestorStyle);
                var edges = InlineFlow.Edges(ancestorStyle, Viewport.Width);
                if (range.Start == start) leading += edges.Left + (float)ancestorStyle.Get(StyleProperties.MarginLeft).Length.Value;
                if (range.End == position) trailing += edges.Right + (float)ancestorStyle.Get(StyleProperties.MarginRight).Length.Value;
                top += edges.Top;
                bottom += edges.Bottom;
            }
            var run = new TextRun(piece.Text, TextContent.ToTextStyle(piece.Style), shift,
                Math.Max(0, leading), Math.Max(0, trailing), piece.Style.Get(StyleProperties.WhiteSpace) is WhiteSpace.NoWrap or WhiteSpace.Pre, top, bottom);
            runs.Add(run);
            flow.Segments.Add(new InlineSegment(piece.Owner, piece.Style, start, position, run));
        }
        if (layout.Text.UpdateRuns(runs, style) || layout.Node.Measure is null) layout.Node.Measure = layout.Text.Measure;
        layout.AnonymousText.Clear();
        layout.Node.SetChildren(children.OfType<ElementNode>().Where(child => GetStyle(child).Display != Display.None).Select(child => child.GetFeature<ElementLayout>()!.Node).ToArray());
        SetArranger(element, layout);
        foreach (var old in previous)
            if (!flow.Elements.Contains(old)) ClearPart(old, flow);
        return true;

        bool CanInline(Node node)
        {
            if (node is TextNode) return true;
            if (node is not ElementNode child) return false;
            var childStyle = GetStyle(child);
            if (childStyle.Display == Display.None) return true;
            return childStyle.Display == Display.Inline && child.Attributes.GetValueOrDefault("source") is null &&
                child.Attributes.GetValueOrDefault("glyph") is null && !IsOverlay(child) && child.Children.All(CanInline);
        }

        void Append(char value, ElementNode owner, ComputedStyle current)
        {
            if (pendingPieces.Count > 0 && pendingPieces[^1].Owner == owner && ReferenceEquals(pendingPieces[^1].Style, current))
                pendingPieces[^1].Text.Append(value);
            else pendingPieces.Add((owner, current, new StringBuilder().Append(value)));
            content.Append(value);
        }

        void Collect(Node node, ElementNode owner, ComputedStyle current)
        {
            if (node is TextNode text)
            {
                var mode = current.Get(StyleProperties.WhiteSpace);
                var raw = text.Text.Replace("\r\n", "\n").Replace('\r', '\n');
                foreach (var character in raw)
                {
                    var collapse = mode is not (WhiteSpace.Pre or WhiteSpace.PreWrap) && character is ' ' or '\t' or '\n' or '\f';
                    if (character == '\n' && mode == WhiteSpace.PreLine)
                    {
                        pending = null; Append('\n', owner, current); continue;
                    }
                    if (collapse) { pending = (owner, current); continue; }
                    if (pending is { } space && content.Length > 0 && content[^1] != '\n') Append(' ', space.Owner, space.Style);
                    pending = null;
                    Append(character, owner, current);
                }
                return;
            }
            var child = (ElementNode)node;
            var childLayout = SyncInlineElement(child);
            if (childLayout.Style!.Display == Display.None) return;
            flow.Elements.Add(child);
            child.SetFeature(new InlinePart(flow));
            var start = content.Length;
            if (child.Attributes.GetValueOrDefault("line-break") is true) { pending = null; Append('\n', child, childLayout.Style); }
            else foreach (var nested in child.Children) Collect(nested, child, childLayout.Style);
            flow.Ranges[child] = (start, content.Length);
            childLayout.Node.SetChildren(child.Children.OfType<ElementNode>().Where(nested => GetStyle(nested).Display != Display.None).Select(nested => nested.GetFeature<ElementLayout>()!.Node).ToArray());
            SetArranger(child, childLayout);
        }

        void SetArranger(ElementNode owner, ElementLayout ownerLayout)
        {
            if (!flow.Arrangers.TryGetValue(owner, out var arrange)) flow.Arrangers[owner] = arrange = flow.Arrange;
            ownerLayout.Node.ArrangeChildren = arrange;
        }
    }

    private static IEnumerable<ElementNode> AncestorsWithin(ElementNode element, ElementNode root)
    {
        for (var current = element; current != root && current is not null; current = current.ParentElement) yield return current;
    }

    private ElementLayout SyncInlineElement(ElementNode element)
    {
        var layout = element.GetFeature<ElementLayout>();
        if (layout is null) element.SetFeature(layout = new ElementLayout { Node = new LayoutNode { Tag = element } });
        var style = GetStyle(element);
        if (!ReferenceEquals(layout.Style, style))
        {
            if (layout.Style is { } previous) Transitions.StyleChanged(element, previous, style);
            layout.Style = style;
            layout.Node.Style = ToLayoutStyle(element, style);
        }
        if (layout.Node.Measure is not null) layout.Node.Measure = null;
        ClearInline(layout);
        layout.Text = null;
        layout.AnonymousText.Clear();
        SyncBackgrounds(layout, style);
        return layout;
    }

    private static void ClearPart(ElementNode element, InlineFlow flow)
    {
        if (element.GetFeature<InlinePart>()?.Flow != flow) return;
        element.SetFeature<InlinePart>(null);
        if (element.GetFeature<ElementLayout>() is { } layout) layout.Node.ArrangeChildren = null;
        flow.Arrangers.Remove(element);
        flow.Fragments.Remove(element);
    }

    private static void ClearInline(ElementLayout layout)
    {
        if (layout.Inline is not { } flow) return;
        foreach (var child in flow.Elements) ClearPart(child, flow);
        layout.Node.ArrangeChildren = null;
        layout.Inline = null;
    }

    private bool InlineContains(ElementNode element, Point point) => element.GetFeature<InlinePart>() is not { } part ||
        part.Flow.Fragments.GetValueOrDefault(element)?.Any(rect => rect.Contains(point)) is true;

    private void PaintInline(ElementNode element, ElementLayout layout, DisplayListBuilder builder, Nazor.Graphics.Color? overrideColor = null)
    {
        if (layout.Inline is not { } flow || layout.Text is not { } text) return;
        flow.UpdateFragments();
        var paragraph = text.Get(layout.Node.ContentWidth);
        var origin = layout.Node.ContentBox.Position;
        var align = TextAlignmentOf(layout.Style!);
        if (overrideColor is null)
        {
            foreach (var inline in flow.Elements)
            {
                if (inline.GetFeature<ElementLayout>() is not { Style: { } inlineStyle } inlineLayout || inlineStyle.Get(StyleProperties.Visibility) != Visibility.Visible) continue;
                var background = ToGraphics(Effective(inline, inlineStyle, StyleProperties.BackgroundColor));
                foreach (var fragment in flow.Fragments.GetValueOrDefault(inline) ?? [])
                {
                    var opacity = (float)InlineOpacity(inline, element);
                    if (opacity < 1) builder.Add(new PushOpacity(opacity, fragment));
                    var radius = BorderRadius(inline, inlineStyle, fragment);
                    PaintShadows(Effective(inline, inlineStyle, StyleProperties.BoxShadow).Items, fragment, radius, false, builder);
                    if (!background.IsTransparent) builder.Add(new FillRectangle(fragment, radius, background));
                    PaintBackgrounds(inlineLayout, inlineStyle, fragment, radius, builder);
                    PaintShadows(Effective(inline, inlineStyle, StyleProperties.BoxShadow).Items, fragment, radius, true, builder);
                    var border = new Thickness((float)inlineStyle.Get(StyleProperties.BorderLeftWidth).Value, (float)inlineStyle.Get(StyleProperties.BorderTopWidth).Value,
                        (float)inlineStyle.Get(StyleProperties.BorderRightWidth).Value, (float)inlineStyle.Get(StyleProperties.BorderBottomWidth).Value);
                    PaintBorder(inline, inlineStyle, border, fragment, radius, builder);
                    PaintOutline(inline, inlineStyle, fragment, radius, builder);
                    if (opacity < 1) builder.Add(Pop.Instance);
                }
            }
        }
        foreach (var segment in flow.Segments)
        {
            if (segment.Style.Get(StyleProperties.Visibility) != Visibility.Visible) continue;
            var color = overrideColor ?? ToGraphics(Effective(segment.Owner, segment.Style, StyleProperties.Color));
            var opacity = InlineOpacity(segment.Owner, element);
            color = color with { A = (byte)Math.Round(color.A * opacity) };
            builder.Add(new DrawText(paragraph, origin, color, align, layout.Node.ContentBox.Width)
                { TextStart = segment.Start, TextLength = segment.End - segment.Start });
        }
        foreach (var inline in flow.Elements.Prepend(element))
        {
            var inlineStyle = GetStyle(inline);
            if (inlineStyle.Get(StyleProperties.Visibility) != Visibility.Visible || Decoration(inlineStyle, InlineOpacity(inline, element)) is not { } decoration) continue;
            var range = inline == element ? (Start: 0, End: text.Text.Length) : flow.Ranges[inline];
            builder.Add(new DrawText(paragraph, origin, Nazor.Graphics.Color.Transparent, align, layout.Node.ContentBox.Width, decoration)
                { TextStart = range.Start, TextLength = range.End - range.Start });
        }
    }

    private double InlineOpacity(ElementNode element, ElementNode root)
    {
        var opacity = 1d;
        foreach (var ancestor in AncestorsWithin(element, root)) opacity *= Effective(ancestor, GetStyle(ancestor), StyleProperties.Opacity);
        return opacity;
    }

    private void PaintInlineSelection(ElementNode element, ElementLayout layout, TextEditState edit, DisplayListBuilder builder)
    {
        PaintInline(element, layout, builder);
        if (!edit.HasSelection || layout.Text is not { } text) return;
        var paragraph = text.Get(layout.Node.ContentWidth);
        var selection = Styles.GetPseudoStyle(element, PseudoElement.Selection);
        foreach (var area in paragraph.GetSelectionRects(Math.Min(edit.SelectionStart, text.Text.Length), Math.Min(edit.SelectionEnd, text.Text.Length)))
        {
            var line = paragraph.Lines.First(line => line.Top == area.Y);
            var rect = area.Offset(layout.Node.ContentBox.X + InlineFlow.Alignment(layout.Style!, layout.Node.ContentBox.Width, line.Width), layout.Node.ContentBox.Y);
            builder.Add(new FillRectangle(rect, 0, ToGraphics(selection.BackgroundColor)));
            builder.Add(new PushClip(rect, 0));
            PaintInline(element, layout, builder, ToGraphics(selection.Color));
            builder.Add(Pop.Instance);
        }
    }
}
