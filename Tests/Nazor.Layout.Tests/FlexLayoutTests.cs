using Nazor.Graphics;
using Nazor.Style;

namespace Nazor.Layout.Tests;

public sealed class FlexLayoutTests
{
    private static SizeValue Px(float value) => SizeValue.Fixed(Length.Px(value));

    private static LayoutNode Node(LayoutStyle style, params LayoutNode[] children)
    {
        var node = new LayoutNode { Style = style };
        foreach (var child in children) node.Add(child);
        return node;
    }

    private static LayoutNode Box(float width, float height, float grow = 0, float shrink = 1) =>
        Node(new LayoutStyle { Width = Px(width), Height = Px(height), FlexGrow = grow, FlexShrink = shrink });

    private static LayoutNode Text(int words, Func<int>? counter = null) => new()
    {
        Measure = (knownWidth, knownHeight, availableWidth, _) =>
        {
            counter?.Invoke();
            var maxContent = words * 10f;
            var width = knownWidth ?? availableWidth.Kind switch
            {
                AvailableKind.MinContent => 10,
                AvailableKind.MaxContent => maxContent,
                _ => Math.Min(maxContent, Math.Max(10, MathF.Floor(availableWidth.Value / 10) * 10)),
            };
            var perLine = Math.Max(1, (int)(width / 10));
            return new Size(width, knownHeight ?? MathF.Ceiling(words / (float)perLine) * 10);
        },
    };

    private static LayoutNode Root(LayoutStyle style, float width, float height, params LayoutNode[] children)
    {
        var root = Node(style, children);
        LayoutEngine.Compute(root, new Size(width, height));
        return root;
    }

    private static readonly LayoutStyle Row = new() { Display = Display.Flex, FlexDirection = FlexDirection.Row };
    private static readonly LayoutStyle Column = new() { Display = Display.Flex, FlexDirection = FlexDirection.Column };

    [Fact]
    public void Stretched_text_is_measured_at_its_maximum_width()
    {
        // Stretched to 300 the text needs one line; its max-width of 100 makes it wrap into two, and the height must follow.
        var text = Text(20);
        text.Style = new LayoutStyle { MaxWidth = Px(100) };
        Root(Column, 300, 200, text);

        Assert.Equal(new Rect(0, 0, 100, 20), text.Frame);
    }

    [Fact]
    public void Row_places_items_with_gap_and_stretches_the_cross_axis()
    {
        var a = Node(new LayoutStyle { Width = Px(50) });
        var b = Node(new LayoutStyle { Width = Px(30) });
        Root(new LayoutStyle { Display = Display.Flex, ColumnGap = Length.Px(10) }, 200, 40, a, b);

        Assert.Equal(new Rect(0, 0, 50, 40), a.Frame);
        Assert.Equal(new Rect(60, 0, 30, 40), b.Frame);
    }

    [Fact]
    public void Growing_freezes_items_that_hit_their_maximum()
    {
        var a = Node(new LayoutStyle { FlexBasis = Px(100), FlexGrow = 1, MaxWidth = Px(120) });
        var b = Node(new LayoutStyle { FlexBasis = Px(50), FlexGrow = 1 });
        var c = Node(new LayoutStyle { FlexBasis = Px(50) });
        Root(Row, 300, 10, a, b, c);

        Assert.Equal([120f, 130f, 50f], new[] { a, b, c }.Select(n => n.Frame.Width));
        Assert.Equal([0f, 120f, 250f], new[] { a, b, c }.Select(n => n.Frame.X));
    }

    [Fact]
    public void Shrinking_is_weighted_by_base_size_and_respects_minimums()
    {
        var a = Node(new LayoutStyle { FlexBasis = Px(200) });
        var b = Node(new LayoutStyle { FlexBasis = Px(100), MinWidth = Px(80) });
        Root(Row, 200, 10, a, b);

        Assert.Equal(120, a.Frame.Width, 3);
        Assert.Equal(80, b.Frame.Width, 3);
    }

    [Fact]
    public void Fractional_grow_factors_distribute_only_part_of_the_free_space()
    {
        var a = Node(new LayoutStyle { FlexBasis = Px(100), FlexGrow = 0.5f });
        Root(Row, 300, 10, a);
        Assert.Equal(200, a.Frame.Width, 3);
    }

    [Fact]
    public void Zero_basis_shares_space_equally_and_auto_basis_adds_content()
    {
        var zeroA = Node(new LayoutStyle { FlexBasis = Px(0), FlexGrow = 1 }, Text(2));
        var zeroB = Node(new LayoutStyle { FlexBasis = Px(0), FlexGrow = 1 }, Text(6));
        Root(Row, 200, 20, zeroA, zeroB);
        Assert.Equal([100f, 100f], new[] { zeroA, zeroB }.Select(n => n.Frame.Width));

        var autoA = Node(new LayoutStyle { FlexGrow = 1 }, Text(2));
        var autoB = Node(new LayoutStyle { FlexGrow = 1 }, Text(6));
        Root(Row, 200, 20, autoA, autoB);
        Assert.Equal([80f, 120f], new[] { autoA, autoB }.Select(n => n.Frame.Width));
    }

    [Theory]
    [InlineData(JustifyContent.FlexStart, new[] { 0f, 20f })]
    [InlineData(JustifyContent.FlexEnd, new[] { 60f, 80f })]
    [InlineData(JustifyContent.Center, new[] { 30f, 50f })]
    [InlineData(JustifyContent.SpaceBetween, new[] { 0f, 80f })]
    [InlineData(JustifyContent.SpaceAround, new[] { 15f, 65f })]
    [InlineData(JustifyContent.SpaceEvenly, new[] { 20f, 60f })]
    public void Justify_content_distributes_free_space(JustifyContent justify, float[] expected)
    {
        var a = Box(20, 10);
        var b = Box(20, 10);
        Root(new LayoutStyle { Display = Display.Flex, JustifyContent = justify }, 100, 10, a, b);
        Assert.Equal(expected, new[] { a.Frame.X, b.Frame.X });
    }

    [Fact]
    public void Space_distribution_falls_back_for_one_item_and_for_overflow()
    {
        var single = Box(20, 10);
        Root(new LayoutStyle { Display = Display.Flex, JustifyContent = JustifyContent.SpaceAround }, 100, 10, single);
        Assert.Equal(40, single.Frame.X);

        var wide = Box(150, 10, shrink: 0);
        Root(new LayoutStyle { Display = Display.Flex, JustifyContent = JustifyContent.SpaceBetween }, 100, 10, wide);
        Assert.Equal(0, wide.Frame.X);
    }

    [Fact]
    public void Align_items_and_align_self_position_on_the_cross_axis()
    {
        var center = Node(new LayoutStyle { Width = Px(10), Height = Px(10) });
        var end = Node(new LayoutStyle { Width = Px(10), Height = Px(10), AlignSelf = AlignSelf.FlexEnd });
        var stretched = Node(new LayoutStyle { Width = Px(10), AlignSelf = AlignSelf.Stretch, MaxHeight = Px(30) });
        var fixedHeight = Node(new LayoutStyle { Width = Px(10), Height = Px(20), AlignSelf = AlignSelf.Stretch });
        Root(new LayoutStyle { Display = Display.Flex, AlignItems = AlignItems.Center }, 100, 50, center, end, stretched, fixedHeight);

        Assert.Equal(20, center.Frame.Y);
        Assert.Equal(40, end.Frame.Y);
        Assert.Equal(new Rect(20, 0, 10, 30), stretched.Frame);
        Assert.Equal(20, fixedHeight.Frame.Height);
    }

    [Fact]
    public void Text_in_a_column_gets_its_width_first_then_its_height()
    {
        var text = Text(10);
        Root(Column, 40, 200, text);
        Assert.Equal(new Rect(0, 0, 40, 30), text.Frame);
    }

    [Fact]
    public void Indefinite_main_size_is_the_sum_of_item_sizes()
    {
        var inner = Node(new LayoutStyle { Display = Display.Flex, ColumnGap = Length.Px(5) }, Box(20, 10), Box(30, 15));
        Root(new LayoutStyle { Display = Display.Flex, FlexDirection = FlexDirection.Column, AlignItems = AlignItems.FlexStart }, 300, 300, inner);
        Assert.Equal(new Rect(0, 0, 55, 15), inner.Frame);
    }

    [Fact]
    public void Automatic_minimum_keeps_text_at_its_min_content_and_min_width_zero_allows_overflow()
    {
        var text = Node(new LayoutStyle { FlexBasis = Px(100) }, Text(3));
        var other = Box(100, 10);
        Root(Row, 50, 40, text, other);
        Assert.True(text.Frame.Width >= 10);

        var compressible = Node(new LayoutStyle { FlexBasis = Px(100), MinWidth = Px(0) }, Text(3));
        Root(Row, 50, 40, compressible, Box(100, 10));
        Assert.True(compressible.Frame.Width < 10);
    }

    [Fact]
    public void Scroll_containers_have_a_zero_automatic_minimum()
    {
        var scroll = Node(new LayoutStyle { FlexBasis = Px(100), OverflowX = Overflow.Auto }, Text(30));
        Root(Row, 50, 40, scroll, Box(50, 10));
        Assert.Equal(0, scroll.Frame.Width, 3);
    }

    [Fact]
    public void Minimum_wins_over_maximum_and_border_box_never_shrinks_below_padding()
    {
        var conflicting = Node(new LayoutStyle { Width = Px(50), MinWidth = Px(80), MaxWidth = Px(60) });
        var padded = Node(new LayoutStyle { Width = Px(10), PaddingLeft = Length.Px(10), PaddingRight = Length.Px(10) });
        Root(new LayoutStyle { Display = Display.Flex, AlignItems = AlignItems.FlexStart }, 300, 100, conflicting, padded);

        Assert.Equal(80, conflicting.Frame.Width);
        Assert.Equal(20, padded.Frame.Width);
    }

    [Fact]
    public void Content_box_sizing_adds_padding_and_border()
    {
        var node = Node(new LayoutStyle { Width = Px(100), Height = Px(20), PaddingLeft = Length.Px(5), Border = new Thickness(1, 1, 1, 1), BoxSizing = BoxSizing.ContentBox });
        Root(new LayoutStyle { Display = Display.Flex, AlignItems = AlignItems.FlexStart }, 300, 100, node);
        Assert.Equal(new Size(107, 22), node.Frame.Size);
    }

    [Fact]
    public void Percentages_resolve_against_a_definite_parent_and_are_auto_otherwise()
    {
        var half = Node(new LayoutStyle { Width = SizeValue.Fixed(Length.Percent(50)), Height = Px(10) });
        Root(new LayoutStyle { Display = Display.Flex, AlignItems = AlignItems.FlexStart, PaddingLeft = Length.Px(20) }, 220, 100, half);
        Assert.Equal(100, half.Frame.Width);

        var percentHeight = Node(new LayoutStyle { Height = SizeValue.Fixed(Length.Percent(50)) }, Text(1));
        var autoParent = Node(new LayoutStyle(), percentHeight);
        Root(new LayoutStyle { Display = Display.Flex, AlignItems = AlignItems.FlexStart }, 100, 100, autoParent);
        Assert.Equal(10, percentHeight.Frame.Height);
    }

    [Fact]
    public void Block_containers_stack_children_and_stretch_them()
    {
        var first = Text(3);
        var second = Node(new LayoutStyle { Width = Px(20), Height = Px(5) });
        var block = Node(new LayoutStyle { PaddingTop = Length.Px(2) }, first, second);
        Root(new LayoutStyle(), 100, 100, block);

        Assert.Equal(new Rect(0, 2, 100, 10), first.Frame);
        Assert.Equal(new Rect(0, 12, 20, 5), second.Frame);
        Assert.Equal(new Rect(0, 0, 100, 17), block.Frame);
    }

    [Fact]
    public void Hidden_items_take_no_space_and_get_no_gap()
    {
        var a = Box(10, 10);
        var hidden = Node(new LayoutStyle { Display = Display.None, Width = Px(50) });
        var b = Box(10, 10);
        Root(new LayoutStyle { Display = Display.Flex, ColumnGap = Length.Px(5) }, 100, 10, a, hidden, b);
        Assert.Equal(15, b.Frame.X);
        Assert.Equal(Rect.Empty, hidden.Frame);
    }

    [Fact]
    public void Auto_margins_absorb_free_space_and_reverse_directions_mirror_positions()
    {
        var left = Box(10, 10);
        var pushed = Node(new LayoutStyle { Width = Px(10), Height = Px(10), MarginLeft = SizeValue.Auto });
        Root(new LayoutStyle { Display = Display.Flex }, 100, 10, left, pushed);
        Assert.Equal(90, pushed.Frame.X);

        var first = Box(10, 10);
        var second = Box(20, 10);
        Root(new LayoutStyle { Display = Display.Flex, FlexDirection = FlexDirection.RowReverse }, 100, 10, first, second);
        Assert.Equal(90, first.Frame.X);
        Assert.Equal(70, second.Frame.X);
    }

    [Fact]
    public void Absolute_edges_snap_to_physical_pixels_without_gaps()
    {
        var items = Enumerable.Range(0, 3).Select(_ => Node(new LayoutStyle { FlexGrow = 1, FlexBasis = Px(0) })).ToArray();
        var root = Node(new LayoutStyle { Display = Display.Flex }, items);
        LayoutEngine.Compute(root, new Size(100, 10), scale: 1.25f);

        for (var i = 1; i < items.Length; i++) Assert.Equal(items[i - 1].RoundedFrame.Right, items[i].RoundedFrame.X, 4);
        foreach (var item in items)
        {
            Assert.Equal(MathF.Round(item.RoundedFrame.X * 1.25f), item.RoundedFrame.X * 1.25f, 3);
            Assert.Equal(MathF.Round(item.RoundedFrame.Right * 1.25f), item.RoundedFrame.Right * 1.25f, 3);
        }
        Assert.Equal(100, items[^1].RoundedFrame.Right, 3);
    }

    [Fact]
    public void Unchanged_trees_reuse_cached_measurements()
    {
        var calls = 0;
        var text = Text(5, () => calls++);
        var root = Node(Column, Node(Row, text));
        LayoutEngine.Compute(root, new Size(100, 100));
        var afterFirst = calls;
        LayoutEngine.Compute(root, new Size(100, 100));
        Assert.Equal(afterFirst, calls);

        text.MarkDirty();
        LayoutEngine.Compute(root, new Size(100, 100));
        Assert.True(calls > afterFirst);
    }

    [Fact]
    public void Invalid_measurements_are_rejected()
    {
        var broken = new LayoutNode { Measure = (_, _, _, _) => new Size(float.NaN, 1) };
        var exception = Assert.Throws<InvalidOperationException>(() => Root(Column, 100, 100, broken));
        Assert.Contains("invalid size", exception.Message);
    }
}
