using Nazor.Graphics;
using Nazor.Style;

namespace Nazor.Layout.Tests;

public sealed class IntrinsicSizeTests
{
    private static SizeValue Px(float value) => SizeValue.Fixed(Length.Px(value));

    private static LayoutNode Node(LayoutStyle style, params LayoutNode[] children)
    {
        var node = new LayoutNode { Style = style };
        foreach (var child in children) node.Add(child);
        return node;
    }

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

    private static readonly AspectRatio Wide = new(2);

    [Fact]
    public void A_stretched_width_gives_the_height()
    {
        var box = Node(new LayoutStyle { AspectRatio = Wide });
        Root(new LayoutStyle(), 300, 500, box);
        Assert.Equal(new Rect(0, 0, 300, 150), box.Frame);
    }

    [Fact]
    public void A_fixed_side_gives_the_other_and_both_fixed_ignore_the_ratio()
    {
        var byHeight = Node(new LayoutStyle { Height = Px(40), AspectRatio = Wide });
        var both = Node(new LayoutStyle { Width = Px(30), Height = Px(40), AspectRatio = Wide });
        Root(new LayoutStyle { Display = Display.Flex, AlignItems = AlignItems.FlexStart }, 300, 500, byHeight, both);
        Assert.Equal(new Size(80, 40), byHeight.Frame.Size);
        Assert.Equal(new Size(30, 40), both.Frame.Size);
    }

    [Fact]
    public void Flex_items_keep_the_ratio_on_the_cross_axis_instead_of_stretching()
    {
        var item = Node(new LayoutStyle { FlexGrow = 1, AspectRatio = new AspectRatio(4) });
        var other = Node(new LayoutStyle { FlexGrow = 1 });
        Root(new LayoutStyle { Display = Display.Flex }, 200, 300, item, other);
        Assert.Equal(new Rect(0, 0, 100, 25), item.Frame);
        Assert.Equal(300, other.Frame.Height);
    }

    [Fact]
    public void The_ratio_is_of_the_content_box_with_content_box_sizing()
    {
        var box = Node(new LayoutStyle { AspectRatio = Wide, BoxSizing = BoxSizing.ContentBox, PaddingLeft = Length.Px(10), PaddingRight = Length.Px(10), PaddingTop = Length.Px(5) });
        Root(new LayoutStyle(), 220, 500, box);
        Assert.Equal(new Size(220, 105), box.Frame.Size);
    }

    [Fact]
    public void Grid_items_with_a_ratio_are_square_tiles()
    {
        var tiles = Enumerable.Range(0, 3).Select(_ => Node(new LayoutStyle { AspectRatio = new AspectRatio(1) })).ToArray();
        Root(new LayoutStyle
        {
            Display = Display.Grid,
            GridTemplateColumns = new GridTrackList([TrackSize.Fr(1), TrackSize.Fr(1), TrackSize.Fr(1)]),
            ColumnGap = Length.Px(15),
        }, 300, 500, tiles);
        Assert.All(tiles, t => Assert.Equal(new Size(90, 90), t.Frame.Size));
    }

    [Fact]
    public void Absolute_boxes_take_the_height_from_the_ratio()
    {
        var overlay = Node(new LayoutStyle { Position = Position.Absolute, Left = Px(0), Right = Px(0), Top = Px(0), AspectRatio = Wide });
        Root(new LayoutStyle(), 200, 500, Node(new LayoutStyle { Position = Position.Relative, Height = Px(50) }, overlay));
        Assert.Equal(new Rect(0, 0, 200, 100), overlay.Frame);
    }

    [Theory]
    [InlineData(SizeKind.MinContent, 10f)]
    [InlineData(SizeKind.MaxContent, 80f)]
    [InlineData(SizeKind.FitContent, 50f)]
    public void Width_keywords_measure_the_content(SizeKind kind, float expected)
    {
        var box = Node(new LayoutStyle { Width = new SizeValue(kind, Length.Zero) }, Text(8));
        Root(new LayoutStyle(), 50, 500, box);
        Assert.Equal(expected, box.Frame.Width);
    }

    [Fact]
    public void Max_content_overflows_and_fit_content_stops_at_the_available_width()
    {
        var max = Node(new LayoutStyle { Width = SizeValue.MaxContent }, Text(20));
        var fit = Node(new LayoutStyle { Width = SizeValue.FitContent }, Text(20));
        Root(new LayoutStyle { Display = Display.Flex, FlexDirection = FlexDirection.Column, PaddingLeft = Length.Px(20) }, 120, 500, max, fit);
        Assert.Equal(200, max.Frame.Width);
        Assert.Equal(100, fit.Frame.Width);
        Assert.Equal(20, fit.Frame.Height);
    }

    [Fact]
    public void Min_and_max_width_keywords_bound_a_flexible_width()
    {
        var atLeast = Node(new LayoutStyle { FlexGrow = 1, FlexBasis = Px(0), MinWidth = SizeValue.MaxContent }, Text(6));
        var other = Node(new LayoutStyle { FlexGrow = 1, FlexBasis = Px(0) });
        Root(new LayoutStyle { Display = Display.Flex }, 100, 50, atLeast, other);
        Assert.Equal(60, atLeast.Frame.Width);

        var atMost = Node(new LayoutStyle { MaxWidth = SizeValue.MaxContent }, Text(3));
        Root(new LayoutStyle(), 100, 50, atMost);
        Assert.Equal(30, atMost.Frame.Width);
    }

    [Fact]
    public void Flex_basis_keywords_are_widths_in_a_row()
    {
        var narrow = Node(new LayoutStyle { FlexBasis = SizeValue.MinContent, FlexShrink = 0 }, Text(5));
        var wide = Node(new LayoutStyle { FlexBasis = SizeValue.MaxContent, FlexShrink = 0 }, Text(5));
        Root(new LayoutStyle { Display = Display.Flex }, 300, 50, narrow, wide);
        Assert.Equal(10, narrow.Frame.Width);
        Assert.Equal(50, wide.Frame.Width);
    }

    [Fact]
    public void A_height_keyword_is_the_height_of_the_content_and_does_not_stretch()
    {
        var item = Node(new LayoutStyle { Height = SizeValue.MaxContent }, Text(2));
        var stretched = Node(new LayoutStyle(), Text(2));
        Root(new LayoutStyle { Display = Display.Flex }, 300, 80, item, stretched);
        Assert.Equal(10, item.Frame.Height);
        Assert.Equal(80, stretched.Frame.Height);
    }
}
