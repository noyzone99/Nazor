using Nazor.Graphics;
using Nazor.Style;

namespace Nazor.Layout.Tests;

public sealed class GridLayoutTests
{
    private static SizeValue Px(float value) => SizeValue.Fixed(Length.Px(value));

    private static TrackSize T(float px) => TrackSize.Fixed(Length.Px(px));

    private static TrackSize Fr(double value) => TrackSize.Fr(value);

    private static readonly TrackSize Auto = TrackSize.Auto;

    private static GridTrackList Tracks(params TrackSize[] tracks) => new(tracks);

    private static LayoutNode Node(LayoutStyle style, params LayoutNode[] children)
    {
        var node = new LayoutNode { Style = style };
        foreach (var child in children) node.Add(child);
        return node;
    }

    private static LayoutNode Cell(LayoutStyle? style = null) => Node(style ?? new LayoutStyle());

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

    private static LayoutStyle Grid(GridTrackList columns, GridTrackList? rows = null) =>
        new() { Display = Display.Grid, GridTemplateColumns = columns, GridTemplateRows = rows ?? GridTrackList.None };

    [Fact]
    public void Items_fill_fixed_tracks_row_by_row_with_gaps()
    {
        var cells = Enumerable.Range(0, 5).Select(_ => Cell()).ToArray();
        Root(Grid(Tracks(T(50), T(30), T(20)), Tracks(T(10), T(15))) with { ColumnGap = Length.Px(5), RowGap = Length.Px(2) }, 200, 100, cells);

        Assert.Equal(new Rect(0, 0, 50, 10), cells[0].Frame);
        Assert.Equal(new Rect(55, 0, 30, 10), cells[1].Frame);
        Assert.Equal(new Rect(90, 0, 20, 10), cells[2].Frame);
        Assert.Equal(new Rect(0, 12, 50, 15), cells[3].Frame);
        Assert.Equal(new Rect(55, 12, 30, 15), cells[4].Frame);
    }

    [Fact]
    public void Fractions_share_the_space_left_by_fixed_tracks_but_not_below_their_content()
    {
        var a = Cell();
        var b = Cell();
        var c = Cell();
        Root(Grid(Tracks(T(40), Fr(1), Fr(3))), 200, 100, a, b, c);
        Assert.Equal([40f, 40f, 120f], new[] { a, b, c }.Select(n => n.Frame.Width));

        var wide = Node(new LayoutStyle(), Box(120, 10));
        var other = Cell();
        Root(Grid(Tracks(Fr(1), Fr(1))), 200, 100, wide, other);
        Assert.Equal(120, wide.Frame.Width);
        Assert.Equal((120f, 80f), (other.Frame.X, other.Frame.Width));
    }

    [Fact]
    public void Auto_tracks_take_their_content_then_share_the_free_space()
    {
        var a = Node(new LayoutStyle(), Text(3));
        var b = Node(new LayoutStyle(), Text(5));
        Root(Grid(Tracks(Auto, Auto)), 200, 100, a, b);
        Assert.Equal([90f, 110f], new[] { a, b }.Select(n => n.Frame.Width));

        var start = Node(new LayoutStyle(), Text(3));
        var end = Node(new LayoutStyle(), Text(5));
        Root(Grid(Tracks(Auto, Auto)) with { JustifyContent = JustifyContent.FlexStart }, 200, 100, start, end);
        Assert.Equal([30f, 50f], new[] { start, end }.Select(n => n.Frame.Width));
    }

    [Fact]
    public void Justify_and_align_content_move_the_tracks()
    {
        var cell = Cell();
        Root(Grid(Tracks(T(40)), Tracks(T(20))) with { JustifyContent = JustifyContent.Center, AlignContent = AlignContent.FlexEnd }, 200, 100, cell);
        Assert.Equal(new Rect(80, 80, 40, 20), cell.Frame);

        var a = Cell();
        var b = Cell();
        Root(Grid(Tracks(T(40), T(40))) with { JustifyContent = JustifyContent.SpaceBetween }, 200, 100, a, b);
        Assert.Equal([0f, 160f], new[] { a, b }.Select(n => n.Frame.X));
    }

    [Fact]
    public void Minmax_and_fit_content_bound_the_tracks()
    {
        var a = Cell();
        var b = Cell();
        Root(Grid(Tracks(new TrackSize(TrackBreadth.Fixed(Length.Px(150)), TrackBreadth.Fr(1)), Fr(1))), 200, 100, a, b);
        Assert.Equal([150f, 50f], new[] { a, b }.Select(n => n.Frame.Width));

        var limited = Node(new LayoutStyle(), Text(20));
        var rest = Cell();
        var fitContent = new TrackSize(TrackBreadth.Auto, TrackBreadth.Fixed(Length.Px(60)), FitContent: true);
        Root(Grid(Tracks(fitContent, Fr(1))) with { AlignItems = AlignItems.FlexStart }, 300, 100, limited, rest);
        Assert.Equal(60, limited.Frame.Width);
        Assert.Equal(40, limited.Frame.Height);
    }

    [Fact]
    public void Percentages_of_tracks_use_the_definite_width()
    {
        var a = Cell();
        var b = Cell();
        Root(Grid(Tracks(TrackSize.Fixed(Length.Percent(25)), Fr(1))), 200, 100, a, b);
        Assert.Equal([50f, 150f], new[] { a, b }.Select(n => n.Frame.Width));
    }

    [Fact]
    public void Auto_fill_repeats_as_many_tracks_as_fit_and_auto_fit_collapses_the_empty_ones()
    {
        var fill = new GridTrackList(ValueList<TrackSize>.Empty, GridAutoRepeat.Fill, 0, new ValueList<TrackSize>([T(100)]));
        var cells = Enumerable.Range(0, 4).Select(_ => Cell()).ToArray();
        Root(new LayoutStyle { Display = Display.Grid, GridTemplateColumns = fill, ColumnGap = Length.Px(10), JustifyContent = JustifyContent.FlexStart }, 350, 100, cells);
        Assert.Equal([0f, 110f, 220f, 0f], cells.Select(c => c.Frame.X));
        Assert.Equal(cells[0].Frame.Bottom, cells[3].Frame.Y);

        var fit = fill with { Repeat = GridAutoRepeat.Fit };
        var only = Cell();
        var stretched = Node(new LayoutStyle { Display = Display.Grid, GridTemplateColumns = fit, ColumnGap = Length.Px(10), JustifyContent = JustifyContent.Center }, only);
        Root(new LayoutStyle(), 350, 100, stretched);
        Assert.Equal(new Rect(125, 0, 100, 0), only.Frame);
    }

    [Fact]
    public void Lines_place_items_with_spans_and_negative_numbers()
    {
        var wide = Cell(new LayoutStyle { GridColumnStart = GridLine.Line(2), GridColumnEnd = GridLine.Line(4) });
        var last = Cell(new LayoutStyle { GridColumnStart = GridLine.Line(-2), GridRowStart = GridLine.Line(2) });
        var spanning = Cell(new LayoutStyle { GridColumnStart = GridLine.Span(2), GridRowStart = GridLine.Line(3) });
        Root(Grid(Tracks(T(10), T(20), T(30)), Tracks(T(5), T(6), T(7))), 200, 100, wide, last, spanning);

        Assert.Equal(new Rect(10, 0, 50, 5), wide.Frame);
        Assert.Equal(new Rect(30, 5, 30, 6), last.Frame);
        Assert.Equal(new Rect(0, 11, 30, 7), spanning.Frame);
    }

    [Fact]
    public void Lines_before_the_explicit_grid_add_implicit_tracks_at_the_start()
    {
        var before = Cell(new LayoutStyle { GridColumnStart = GridLine.Line(-4), GridColumnEnd = GridLine.Span(1) });
        var first = Cell(new LayoutStyle { GridColumnStart = GridLine.Line(1) });
        Root(Grid(Tracks(T(30), T(40))) with { GridAutoColumns = new ValueList<TrackSize>([T(15)]), JustifyContent = JustifyContent.FlexStart }, 200, 100, before, first);
        Assert.Equal((0f, 15f), (before.Frame.X, before.Frame.Width));
        Assert.Equal(15, first.Frame.X);
    }

    [Fact]
    public void Areas_name_rectangles_of_cells()
    {
        var areas = new GridAreas(["header header", "side main", "footer footer"]);
        LayoutNode Item(string name) => Cell(new LayoutStyle
        {
            GridRowStart = GridLine.Area(name), GridRowEnd = GridLine.Area(name), GridColumnStart = GridLine.Area(name), GridColumnEnd = GridLine.Area(name),
        });
        var header = Item("header");
        var side = Item("side");
        var main = Item("main");
        var footer = Item("footer");
        Root(Grid(Tracks(T(50), Fr(1)), Tracks(T(20), Fr(1), T(10))) with { GridTemplateAreas = areas }, 200, 100, footer, main, side, header);

        Assert.Equal(new Rect(0, 0, 200, 20), header.Frame);
        Assert.Equal(new Rect(0, 20, 50, 70), side.Frame);
        Assert.Equal(new Rect(50, 20, 150, 70), main.Frame);
        Assert.Equal(new Rect(0, 90, 200, 10), footer.Frame);
    }

    [Fact]
    public void Implicit_rows_take_grid_auto_rows_and_the_column_flow_fills_columns()
    {
        var cells = Enumerable.Range(0, 3).Select(_ => Cell()).ToArray();
        Root(Grid(Tracks(T(10), T(10))) with { GridAutoRows = new ValueList<TrackSize>([T(25), T(5)]) }, 200, 100, cells);
        Assert.Equal([0f, 0f, 25f], cells.Select(c => c.Frame.Y));
        Assert.Equal([25f, 25f, 5f], cells.Select(c => c.Frame.Height));

        var byColumn = Enumerable.Range(0, 3).Select(_ => Cell()).ToArray();
        Root(new LayoutStyle
        {
            Display = Display.Grid, GridTemplateRows = Tracks(T(10), T(10)), GridAutoFlow = GridAutoFlow.Column,
            GridAutoColumns = new ValueList<TrackSize>([T(30)]), JustifyContent = JustifyContent.FlexStart,
        }, 200, 100, byColumn);
        Assert.Equal([0f, 0f, 30f], byColumn.Select(c => c.Frame.X));
        Assert.Equal([0f, 10f, 0f], byColumn.Select(c => c.Frame.Y));
    }

    [Fact]
    public void Dense_packing_fills_the_holes_left_by_wide_items()
    {
        LayoutNode Wide() => Cell(new LayoutStyle { GridColumnStart = GridLine.Span(2) });
        var a = Cell();
        var wide = Wide();
        var small = Cell();
        Root(Grid(Tracks(T(10), T(10), T(10))) with { GridAutoRows = new ValueList<TrackSize>([T(10)]) }, 100, 100, a, wide, small);
        Assert.Equal(new Point(10, 0), wide.Frame.Position);
        Assert.Equal(new Point(0, 10), small.Frame.Position);

        var a2 = Cell();
        var b2 = Cell();
        var wide2 = Wide();
        var small2 = Cell();
        Root(Grid(Tracks(T(10), T(10), T(10))) with { GridAutoRows = new ValueList<TrackSize>([T(10)]) }, 100, 100, a2, b2, wide2, small2);
        Assert.Equal(new Point(0, 10), wide2.Frame.Position);
        Assert.Equal(new Point(20, 10), small2.Frame.Position);

        var a3 = Cell();
        var b3 = Cell();
        var wide3 = Wide();
        var small3 = Cell();
        Root(Grid(Tracks(T(10), T(10), T(10))) with { GridAutoRows = new ValueList<TrackSize>([T(10)]), GridAutoFlow = GridAutoFlow.RowDense }, 100, 100, a3, b3, wide3, small3);
        Assert.Equal(new Point(20, 0), small3.Frame.Position);
    }

    [Fact]
    public void Items_align_in_their_area_and_stretch_by_default()
    {
        var stretched = Cell();
        var centered = Node(new LayoutStyle { JustifySelf = AlignSelf.Center, AlignSelf = AlignSelf.FlexEnd }, Text(2));
        var start = Node(new LayoutStyle(), Text(3));
        Root(Grid(Tracks(T(60), T(60), T(60)), Tracks(T(40))) with { JustifyItems = AlignItems.FlexStart, JustifyContent = JustifyContent.FlexStart }, 300, 100,
            Node(new LayoutStyle { JustifySelf = AlignSelf.Stretch }, stretched), centered, start);

        Assert.Equal(new Rect(0, 0, 60, 40), stretched.Parent!.Frame);
        Assert.Equal(new Rect(80, 30, 20, 10), centered.Frame);
        Assert.Equal(new Rect(120, 0, 30, 40), start.Frame);
    }

    [Fact]
    public void Auto_margins_center_an_item_in_its_area()
    {
        var item = Node(new LayoutStyle { Width = Px(20), Height = Px(10), MarginLeft = SizeValue.Auto, MarginRight = SizeValue.Auto, MarginTop = SizeValue.Auto });
        Root(Grid(Tracks(T(100)), Tracks(T(50))), 200, 100, item);
        Assert.Equal(new Rect(40, 40, 20, 10), item.Frame);
    }

    [Fact]
    public void An_auto_height_grid_is_as_tall_as_its_rows_and_text_wraps_in_its_column()
    {
        var text = Node(new LayoutStyle(), Text(10));
        var grid = Node(Grid(Tracks(T(40), Fr(1))) with { RowGap = Length.Px(5) }, text, Cell(), Box(10, 7));
        Root(new LayoutStyle(), 200, 300, grid);
        Assert.Equal(new Rect(0, 0, 40, 30), text.Frame);
        Assert.Equal(new Rect(0, 0, 200, 42), grid.Frame);
    }

    [Fact]
    public void A_shrink_to_fit_grid_takes_its_max_content_width()
    {
        var grid = Node(Grid(Tracks(Auto, T(20))), Node(new LayoutStyle(), Text(4)), Cell());
        Root(new LayoutStyle { Display = Display.Flex, FlexDirection = FlexDirection.Column, AlignItems = AlignItems.FlexStart }, 300, 100, grid);
        Assert.Equal(60, grid.Frame.Width);

        var narrow = Node(Grid(Tracks(Auto, T(20))), Node(new LayoutStyle(), Text(4)), Cell());
        Root(new LayoutStyle { Display = Display.Flex, FlexDirection = FlexDirection.Column, AlignItems = AlignItems.FlexStart }, 25, 100, narrow);
        Assert.Equal(30, narrow.Frame.Width);
    }

    [Fact]
    public void Grid_items_follow_order_in_auto_placement()
    {
        var a = Cell(new LayoutStyle { Order = 1 });
        var b = Cell();
        Root(Grid(Tracks(T(10), T(10))), 100, 100, a, b);
        Assert.Equal([10f, 0f], new[] { a.Frame.X, b.Frame.X });
    }

    [Fact]
    public void Absolute_children_of_a_grid_leave_it_alone()
    {
        var floating = Node(new LayoutStyle { Position = Position.Absolute, Right = Px(0), Top = Px(0), Width = Px(10), Height = Px(10) });
        var cell = Cell();
        Root(Grid(Tracks(T(30))) with { Position = Position.Relative }, 100, 100, floating, cell);
        Assert.Equal(new Rect(0, 0, 30, 100), cell.Frame);
        Assert.Equal(new Rect(90, 0, 10, 10), floating.Frame);
    }
}
