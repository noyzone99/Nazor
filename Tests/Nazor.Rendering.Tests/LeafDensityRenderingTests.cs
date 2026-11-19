using Nazor.Controls;
using Nazor.Input;
using Nazor.Rendering;
using Nazor.Style;
using Nazor.Testing;
using Nazor.Themes.NazorLeaf;

namespace Nazor.Rendering.Tests;

public sealed class LeafDensityPage : ComponentBase
{
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "Container");
        builder.AddAttribute(1, "id", "density-body");
        builder.AddAttribute(2, "style", "display: flex; flex-direction: column; align-items: flex-start; gap: 8px");
        Add<Button>(10, "density-button");
        Add<TextField>(20, "density-field");
        Add<Switch>(30, "density-switch");
        Add<CheckBox>(40, "density-check");
        Add<MenuItem>(50, "density-menu");
        Add<ActionRow>(60, "density-row");
        builder.CloseElement();

        void Add<T>(int sequence, string id) where T : Control
        {
            builder.OpenComponent<T>(sequence);
            builder.AddComponentParameter(sequence + 1, nameof(Control.Id), id);
            if (typeof(ContentControl).IsAssignableFrom(typeof(T)))
                builder.AddComponentParameter(sequence + 2, nameof(ContentControl.ChildContent), (RenderFragment)(content => content.AddContent(0, "Density")));
            builder.CloseComponent();
        }
    }
}

public sealed class LeafDensityRenderingTests
{
    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public void Explicit_collection_row_pitch_wins_over_touch_density(bool tree, bool grid)
    {
        using var h = new RenderHarness();
        using var input = new InputManager(h.Host.Renderer, h.Scene);
        h.Scene.Styles.Theme = NazorLeafTheme.Theme;
        h.Scene.Styles.Preferences = new MediaPreferences(ColorScheme.Light, true, Density: ControlDensity.Touch);
        var root = h.Host.Render<DensityCollectionPage>(ParameterView.FromDictionary(new Dictionary<string, object?>
        {
            [nameof(DensityCollectionPage.Tree)] = tree,
            [nameof(DensityCollectionPage.Grid)] = grid,
            [nameof(DensityCollectionPage.RowPitch)] = 32d,
        }));
        for (var pass = 0; pass < 4; pass++) { h.Host.RunPending(); h.Render(400, 280, 1); }
        var rows = root.Elements(grid ? "DataGridRow" : tree ? "TreeViewItem" : "ListViewItem").Select(h.Scene.GetBounds).OrderBy(row => row.Y).ToArray();
        for (var index = 1; index < rows.Length; index++)
        {
            Assert.Equal(32, rows[index].Y - rows[index - 1].Y, 1);
            Assert.True(rows[index - 1].Bottom <= rows[index].Y + .1);
        }
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public void Default_virtualized_collection_rows_follow_live_density_without_overlap_and_keep_realized_bounds_inside_the_scroll_extent(bool tree, bool grid)
    {
        using var h = new RenderHarness();
        using var input = new InputManager(h.Host.Renderer, h.Scene);
        h.Scene.Styles.Theme = NazorLeafTheme.Theme;
        h.Scene.Styles.Preferences = new MediaPreferences(ColorScheme.Light, true, Density: ControlDensity.Compact);
        var root = h.Host.Render<DensityCollectionPage>(ParameterView.FromDictionary(new Dictionary<string, object?> { [nameof(DensityCollectionPage.Tree)] = tree, [nameof(DensityCollectionPage.Grid)] = grid }));
        var rowName = grid ? "DataGridRow" : tree ? "TreeViewItem" : "ListViewItem";
        var viewportName = grid ? "DataGridBody" : tree ? "TreeViewViewport" : "ListViewViewport";
        void Frame() { for (var pass = 0; pass < 4; pass++) { h.Host.RunPending(); h.Render(400, 280, 1); } }
        foreach (var density in new[] { ControlDensity.Compact, ControlDensity.Touch, ControlDensity.Compact })
        {
            h.Scene.Styles.Preferences = h.Scene.Styles.Preferences with { Density = density };
            Frame();
            var rows = root.Elements(rowName).Select(h.Scene.GetBounds).OrderBy(bounds => bounds.Y).ToArray();
            Assert.True(rows.Length > 1);
            for (var index = 1; index < rows.Length; index++)
                Assert.True(rows[index].Y >= rows[index - 1].Bottom - .1f, $"{rowName} {density}: previous {rows[index - 1]}, next {rows[index]}.");
            if (density == ControlDensity.Touch) Assert.All(rows, bounds => Assert.True(bounds.Height >= 54));
            var viewport = root.Single(viewportName);
            var state = h.Scene.GetScrollState(viewport)!;
            var origin = h.Scene.GetBounds(viewport).Y;
            Assert.True(state.ContentHeight >= rows[^1].Bottom - origin + state.OffsetY - .1f,
                $"{rowName} {density}: content {state.ContentHeight}; last realized bottom {rows[^1].Bottom - origin + state.OffsetY}.");
            Assert.InRange(rows.Length, 2, 40);
        }
    }

    [Theory]
    [InlineData(ColorScheme.Light, false)]
    [InlineData(ColorScheme.Dark, false)]
    [InlineData(ColorScheme.Light, true)]
    public void Touch_density_updates_real_fields_buttons_menus_rows_toggles_and_body_text_without_recreating_them(ColorScheme scheme, bool forcedColors)
    {
        using var h = new RenderHarness();
        h.Scene.Styles.Theme = NazorLeafTheme.Theme;
        h.Scene.Styles.Preferences = new MediaPreferences(scheme, true, forcedColors, Density: ControlDensity.Compact);
        var root = h.Host.Render<LeafDensityPage>();
        void Frame() { h.Host.RunPending(); h.Render(600, 600, 1); }
        ElementNode ById(string id) => root.Elements().Single(element => element.Id == "density-" + id);
        Frame();
        var button = ById("button");
        Assert.Equal(34, h.Scene.GetBounds(button).Height);
        Assert.Equal(34, h.Scene.GetBounds(ById("field")).Height);
        Assert.Equal(34, h.Scene.GetBounds(ById("menu")).Height);
        Assert.Equal(58, h.Scene.GetBounds(ById("row")).Height);
        h.Scene.Styles.Preferences = h.Scene.Styles.Preferences with { Density = ControlDensity.Touch };
        Frame();
        Assert.Same(button, ById("button"));
        foreach (var id in new[] { "button", "field", "menu", "switch", "check" })
            Assert.True(h.Scene.GetBounds(ById(id)).Height >= 42, id);
        Assert.Equal(66, h.Scene.GetBounds(ById("row")).Height);
        Assert.Equal(15, h.Scene.Styles.GetStyle(ById("body")).Get(StyleProperties.FontSize).Value);
        h.Scene.Styles.Preferences = h.Scene.Styles.Preferences with { Density = ControlDensity.Compact };
        Frame();
        Assert.Equal(34, h.Scene.GetBounds(button).Height);
        Assert.Equal(58, h.Scene.GetBounds(ById("row")).Height);
        Assert.Equal(14, h.Scene.Styles.GetStyle(ById("body")).Get(StyleProperties.FontSize).Value);
    }
}

public sealed class DensityCollectionPage : ComponentBase
{
    [Parameter] public bool Tree { get; set; }
    [Parameter] public bool Grid { get; set; }
    [Parameter] public double? RowPitch { get; set; }
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        var items = Enumerable.Range(0, 60).Select(index => "Row " + index).ToArray();
        if (Grid)
        {
            builder.OpenComponent<DataGrid<string>>(20);
            builder.AddComponentParameter(21, nameof(DataGrid<string>.Items), items);
            builder.AddComponentParameter(22, nameof(DataGrid<string>.Style), "width: 300px; height: 240px");
            builder.AddComponentParameter(24, nameof(DataGrid<string>.RowHeight), RowPitch);
            builder.AddComponentParameter(23, nameof(DataGrid<string>.ChildContent), (RenderFragment)(columns =>
            {
                columns.OpenComponent<DataGridColumn<string>>(0);
                columns.AddComponentParameter(1, nameof(DataGridColumn<string>.Key), "value");
                columns.AddComponentParameter(2, nameof(DataGridColumn<string>.Header), "Item");
                columns.AddComponentParameter(3, nameof(DataGridColumn<string>.CellTemplate), (RenderFragment<string>)(value => content => content.AddContent(0, value)));
                columns.CloseComponent();
            }));
            builder.CloseComponent();
        }
        else if (Tree)
        {
            builder.OpenComponent<TreeView<string>>(0);
            builder.AddComponentParameter(1, nameof(TreeView<string>.Roots), items);
            builder.AddComponentParameter(2, nameof(TreeView<string>.Style), "width: 300px; height: 240px");
            builder.AddComponentParameter(3, nameof(TreeView<string>.ItemHeight), RowPitch);
            builder.CloseComponent();
        }
        else
        {
            builder.OpenComponent<ListView<string>>(10);
            builder.AddComponentParameter(11, nameof(ListView<string>.Items), items);
            builder.AddComponentParameter(12, nameof(ListView<string>.Style), "width: 300px; height: 240px");
            builder.AddComponentParameter(13, nameof(ListView<string>.ItemHeight), RowPitch);
            builder.CloseComponent();
        }
    }
}
