using Nazor.Graphics;
using Nazor.Style;

namespace Nazor.Layout.Tests;

public sealed class PositionLayoutTests
{
    private static SizeValue Px(float value) => SizeValue.Fixed(Length.Px(value));

    private static SizeValue Percent(float value) => SizeValue.Fixed(Length.Percent(value));

    private static LayoutNode Node(LayoutStyle style, params LayoutNode[] children)
    {
        var node = new LayoutNode { Style = style };
        foreach (var child in children) node.Add(child);
        return node;
    }

    private static LayoutNode Box(float width, float height) => Node(new LayoutStyle { Width = Px(width), Height = Px(height) });

    private static LayoutNode Text(int words) => new()
    {
        Measure = (knownWidth, knownHeight, availableWidth, _) =>
        {
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

    private static readonly LayoutStyle Row = new() { Display = Display.Flex };
    private static readonly LayoutStyle Relative = new() { Position = Position.Relative };

    [Fact]
    public void Relative_boxes_move_after_layout_without_moving_their_siblings()
    {
        var moved = Node(new LayoutStyle { Width = Px(20), Height = Px(10), Position = Position.Relative, Left = Px(5), Top = Px(-3) });
        var next = Box(20, 10);
        Root(Row, 100, 50, moved, next);
        Assert.Equal(new Rect(5, -3, 20, 10), moved.Frame);
        Assert.Equal(20, next.Frame.X);
    }

    [Fact]
    public void Left_wins_over_right_top_over_bottom_and_percentages_need_a_definite_size()
    {
        var both = Node(new LayoutStyle { Width = Px(10), Height = Px(10), Position = Position.Relative, Left = Px(4), Right = Px(50), Bottom = Px(6) });
        var percent = Node(new LayoutStyle { Width = Px(10), Height = Px(10), Position = Position.Relative, Left = Percent(10), Top = Percent(50) });
        Root(new LayoutStyle { Display = Display.Flex, AlignItems = AlignItems.FlexStart }, 200, 100, both, percent);
        Assert.Equal(new Rect(4, -6, 10, 10), both.Frame);
        Assert.Equal(new Rect(30, 50, 10, 10), percent.Frame);

        var inside = Node(new LayoutStyle { Width = Px(10), Height = Px(10), Position = Position.Relative, Top = Percent(50) });
        Root(new LayoutStyle { Display = Display.Flex, AlignItems = AlignItems.FlexStart }, 200, 100, Node(new LayoutStyle(), inside));
        Assert.Equal(0, inside.Frame.Y);
    }

    [Fact]
    public void Absolute_boxes_leave_the_flow()
    {
        var a = Box(20, 10);
        var absolute = Node(new LayoutStyle { Width = Px(30), Height = Px(30), Position = Position.Absolute });
        var b = Box(20, 10);
        Root(Row with { ColumnGap = Length.Px(5) }, 100, 50, a, absolute, b);
        Assert.Equal(25, b.Frame.X);
        Assert.Equal(new Size(30, 30), absolute.Frame.Size);
    }

    [Fact]
    public void Insets_place_the_box_against_the_padding_box_of_the_positioned_parent()
    {
        var child = Node(new LayoutStyle { Width = Px(50), Height = Px(20), Position = Position.Absolute, Top = Px(5), Left = Px(6) });
        var corner = Node(new LayoutStyle { Width = Px(10), Height = Px(10), Position = Position.Absolute, Right = Px(0), Bottom = Px(0) });
        var parent = Node(Relative with { Width = Px(200), Height = Px(100), PaddingLeft = Length.Px(10), PaddingTop = Length.Px(10), Border = new Thickness(2, 2, 2, 2) }, child, corner);
        Root(new LayoutStyle(), 300, 300, parent);

        Assert.Equal(new Rect(8, 7, 50, 20), child.Frame);
        Assert.Equal(new Rect(188, 88, 10, 10), corner.Frame);
    }

    [Fact]
    public void Two_insets_stretch_an_auto_size_and_center_with_auto_margins()
    {
        var stretched = Node(new LayoutStyle { Position = Position.Absolute, Left = Px(10), Right = Px(20), Top = Px(0), Bottom = Px(30) });
        var centered = Node(new LayoutStyle
        {
            Width = Px(40), Height = Px(20), Position = Position.Absolute, Left = Px(0), Right = Px(0), Top = Px(0), Bottom = Px(0),
            MarginLeft = SizeValue.Auto, MarginRight = SizeValue.Auto, MarginTop = SizeValue.Auto, MarginBottom = SizeValue.Auto,
        });
        Root(new LayoutStyle(), 200, 100, Node(Relative with { Height = Px(100) }, stretched, centered));

        Assert.Equal(new Rect(10, 0, 170, 70), stretched.Frame);
        Assert.Equal(new Rect(80, 40, 40, 20), centered.Frame);
        Assert.Equal(new Thickness(80, 40, 80, 40), centered.Margin);
    }

    [Fact]
    public void An_auto_width_shrinks_to_fit_the_content()
    {
        var fits = Node(new LayoutStyle { Position = Position.Absolute, Left = Px(0) }, Text(5));
        var wraps = Node(new LayoutStyle { Position = Position.Absolute, Left = Px(170) }, Text(5));
        Root(new LayoutStyle(), 200, 100, Node(Relative with { Height = Px(100) }, fits, wraps));

        Assert.Equal(new Size(50, 10), fits.Frame.Size);
        Assert.Equal(new Size(30, 20), wraps.Frame.Size);
    }

    [Fact]
    public void The_containing_block_is_the_nearest_positioned_ancestor_or_the_viewport()
    {
        var child = Node(new LayoutStyle { Width = Px(10), Height = Px(10), Position = Position.Absolute, Left = Px(0), Top = Px(0) });
        var staticParent = Node(new LayoutStyle { MarginLeft = Px(30), MarginTop = Px(40), Height = Px(50) }, child);
        var positioned = Node(Relative with { MarginLeft = Px(5), Height = Px(200) }, staticParent);
        Root(new LayoutStyle(), 300, 300, positioned);
        Assert.Equal(new Rect(5, 0, 10, 10), child.AbsoluteFrame);
        Assert.Equal(new Rect(-30, -40, 10, 10), child.Frame);

        var corner = Node(new LayoutStyle { Width = Px(10), Height = Px(10), Position = Position.Absolute, Right = Px(0), Bottom = Px(0) });
        Root(new LayoutStyle(), 300, 200, Node(new LayoutStyle { Height = Px(20) }, Node(new LayoutStyle(), corner)));
        Assert.Equal(new Rect(290, 190, 10, 10), corner.AbsoluteFrame);
    }

    [Fact]
    public void Percentages_of_insets_and_sizes_use_the_containing_block()
    {
        var child = Node(new LayoutStyle { Width = Percent(50), Height = Percent(25), Position = Position.Absolute, Left = Percent(10), Top = Percent(50) });
        Root(new LayoutStyle(), 300, 300, Node(Relative with { Width = Px(200), Height = Px(80) }, child));
        Assert.Equal(new Rect(20, 40, 100, 20), child.Frame);
    }

    [Fact]
    public void Without_insets_the_box_stays_at_its_static_position()
    {
        var centered = Node(new LayoutStyle { Width = Px(20), Height = Px(10), Position = Position.Absolute });
        Root(new LayoutStyle(), 200, 200, Node(new LayoutStyle
        {
            Display = Display.Flex, JustifyContent = JustifyContent.Center, AlignItems = AlignItems.Center, Height = Px(100), Position = Position.Relative,
        }, Box(50, 50), centered));
        Assert.Equal(new Rect(90, 45, 20, 10), centered.Frame);

        var below = Node(new LayoutStyle { Width = Px(20), Height = Px(10), Position = Position.Absolute, Left = Px(7) });
        Root(new LayoutStyle(), 200, 200, Node(new LayoutStyle { PaddingTop = Length.Px(3) }, Box(50, 30), below, Box(50, 30)));
        Assert.Equal(new Rect(7, 33, 20, 10), below.Frame);
    }

    [Fact]
    public void A_second_layout_of_an_unchanged_tree_computes_nothing()
    {
        var absolute = Node(new LayoutStyle { Position = Position.Absolute, Left = Px(10), Top = Px(10) }, Text(4));
        var root = Node(new LayoutStyle(), Node(Relative with { Height = Px(100) }, absolute, Box(10, 10)));
        LayoutEngine.Compute(root, new Size(200, 200));
        Assert.Equal(0, LayoutEngine.Compute(root, new Size(200, 200)));
        Assert.Equal(new Rect(10, 10, 40, 10), absolute.Frame);
    }
}
