using Nazor.Graphics;
using Nazor.Style;

namespace Nazor.Layout.Tests;

public sealed class FlexWrapTests
{
    private static SizeValue Px(float value) => SizeValue.Fixed(Length.Px(value));

    private static LayoutNode Node(LayoutStyle style, params LayoutNode[] children)
    {
        var node = new LayoutNode { Style = style };
        foreach (var child in children) node.Add(child);
        return node;
    }

    private static LayoutNode Box(float width, float height, int order = 0, float grow = 0) =>
        Node(new LayoutStyle { Width = Px(width), Height = Px(height), Order = order, FlexGrow = grow });

    private static LayoutNode Root(LayoutStyle style, float width, float height, params LayoutNode[] children)
    {
        var root = Node(style, children);
        LayoutEngine.Compute(root, new Size(width, height));
        return root;
    }

    private static readonly LayoutStyle Wrap = new() { Display = Display.Flex, FlexWrap = FlexWrap.Wrap, AlignContent = AlignContent.FlexStart };

    [Fact]
    public void Items_wrap_where_the_next_one_would_overflow_with_gaps_on_both_axes()
    {
        var items = Enumerable.Range(0, 5).Select(_ => Box(30, 10)).ToArray();
        Root(Wrap with { ColumnGap = Length.Px(5), RowGap = Length.Px(4) }, 100, 100, items);

        Assert.Equal([0f, 35f, 70f, 0f, 35f], items.Select(i => i.Frame.X));
        Assert.Equal([0f, 0f, 0f, 14f, 14f], items.Select(i => i.Frame.Y));
    }

    [Fact]
    public void Each_line_resolves_its_own_flexible_lengths_and_cross_size()
    {
        var a = Box(60, 10, grow: 1);
        var b = Box(60, 20);
        var c = Node(new LayoutStyle { Width = Px(30), FlexGrow = 1 });
        Root(Wrap, 100, 100, a, b, c);

        Assert.Equal(new Rect(0, 0, 100, 10), a.Frame);
        Assert.Equal(new Rect(0, 10, 60, 20), b.Frame);
        Assert.Equal(new Rect(60, 10, 40, 20), c.Frame);
    }

    [Theory]
    [InlineData(AlignContent.FlexStart, new[] { 0f, 10f })]
    [InlineData(AlignContent.FlexEnd, new[] { 80f, 90f })]
    [InlineData(AlignContent.Center, new[] { 40f, 50f })]
    [InlineData(AlignContent.SpaceBetween, new[] { 0f, 90f })]
    [InlineData(AlignContent.SpaceAround, new[] { 20f, 70f })]
    [InlineData(AlignContent.SpaceEvenly, new[] { 26.666666f, 63.333332f })]
    public void Align_content_distributes_the_free_cross_space_among_lines(AlignContent align, float[] expected)
    {
        var a = Box(60, 10);
        var b = Box(60, 10);
        Root(Wrap with { AlignContent = align }, 100, 100, a, b);
        Assert.Equal(expected[0], a.Frame.Y, 3);
        Assert.Equal(expected[1], b.Frame.Y, 3);
    }

    [Fact]
    public void Normal_and_stretch_grow_the_lines_and_their_stretched_items()
    {
        var a = Node(new LayoutStyle { Width = Px(60) });
        var b = Box(60, 10);
        Root(Wrap with { AlignContent = AlignContent.Normal }, 100, 100, a, b);

        Assert.Equal(new Rect(0, 0, 60, 45), a.Frame);
        Assert.Equal(new Rect(0, 45, 60, 10), b.Frame);
    }

    [Fact]
    public void Wrap_reverse_stacks_lines_from_the_cross_end_and_mirrors_alignment()
    {
        var a = Box(60, 10);
        var b = Box(60, 20);
        var c = Node(new LayoutStyle { Width = Px(30), Height = Px(10), AlignSelf = AlignSelf.FlexStart });
        Root(Wrap with { FlexWrap = FlexWrap.WrapReverse }, 100, 100, a, b, c);

        Assert.Equal(90, a.Frame.Y);
        Assert.Equal(70, b.Frame.Y);
        Assert.Equal(80, c.Frame.Y);
    }

    [Fact]
    public void Columns_wrap_at_a_definite_height_but_not_at_the_available_space()
    {
        var items = Enumerable.Range(0, 3).Select(_ => Box(20, 40)).ToArray();
        var bounded = Node(new LayoutStyle { Display = Display.Flex, FlexDirection = FlexDirection.Column, FlexWrap = FlexWrap.Wrap, Height = Px(100), AlignContent = AlignContent.FlexStart }, items);
        Root(new LayoutStyle(), 200, 300, bounded);
        Assert.Equal([0f, 40f, 0f], items.Select(i => i.Frame.Y));
        Assert.Equal([0f, 0f, 20f], items.Select(i => i.Frame.X));

        var unbounded = Enumerable.Range(0, 3).Select(_ => Box(20, 40)).ToArray();
        var column = Node(new LayoutStyle { Display = Display.Flex, FlexDirection = FlexDirection.Column, FlexWrap = FlexWrap.Wrap }, unbounded);
        Root(new LayoutStyle(), 200, 100, column);
        Assert.Equal([0f, 40f, 80f], unbounded.Select(i => i.Frame.Y));
        Assert.Equal(120, column.Frame.Height);
    }

    [Fact]
    public void An_auto_sized_container_takes_the_height_of_its_lines()
    {
        var items = Enumerable.Range(0, 3).Select(_ => Box(60, 10)).ToArray();
        var wrap = Node(Wrap with { RowGap = Length.Px(5) }, items);
        Root(new LayoutStyle(), 100, 500, wrap);
        Assert.Equal(new Rect(0, 0, 100, 40), wrap.Frame);
    }

    [Fact]
    public void A_wrapped_container_fills_the_available_width_and_its_minimum_is_its_widest_item()
    {
        var wrapped = Node(Wrap, Box(60, 10), Box(60, 10));
        Root(new LayoutStyle { Display = Display.Flex, FlexDirection = FlexDirection.Column, AlignItems = AlignItems.FlexStart }, 100, 100, wrapped);
        Assert.Equal(100, wrapped.Frame.Width);

        var shrinking = Node(Wrap with { FlexBasis = Px(500) }, Box(40, 10), Box(30, 10));
        Root(new LayoutStyle { Display = Display.Flex, AlignItems = AlignItems.FlexStart }, 10, 100, shrinking);
        Assert.Equal(40, shrinking.Frame.Width);
        Assert.Equal(20, shrinking.Frame.Height);
    }

    [Fact]
    public void Order_lays_out_items_in_order_modified_document_order()
    {
        var a = Box(10, 10, order: 2);
        var b = Box(10, 10, order: -1);
        var c = Box(10, 10, order: 2);
        var d = Box(10, 10);
        Root(new LayoutStyle { Display = Display.Flex }, 100, 10, a, b, c, d);

        Assert.Equal([20f, 0f, 30f, 10f], new[] { a, b, c, d }.Select(n => n.Frame.X));
    }

    [Fact]
    public void Order_is_ignored_by_block_containers()
    {
        var a = Box(10, 10, order: 1);
        var b = Box(10, 10);
        Root(new LayoutStyle(), 100, 100, a, b);
        Assert.Equal([0f, 10f], new[] { a.Frame.Y, b.Frame.Y });
    }
}
