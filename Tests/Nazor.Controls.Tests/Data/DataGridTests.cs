using System.ComponentModel;
using Nazor.Controls.Primitives;
using Nazor.Controls.Tests.Data.Fixtures;
using Nazor.Input;

namespace Nazor.Controls.Tests.Data;

[Collection("Controls")]
public sealed class DataGridTests : IDisposable
{
    private readonly ControlHarness h = new(width: 800, height: 600);

    public void Dispose() => h.Dispose();

    private GridProbe Render()
    {
        h.Render<GridProbe>();
        return h.Component<GridProbe>();
    }

    private void Update(GridProbe probe, Action<GridProbe> change)
    {
        change(probe);
        h.Host.Renderer.Dispatcher.InvokeAsync(probe.Refresh);
        h.Frame();
    }

    private ElementNode Cell(int row, int column) =>
        h.Root.Elements().Single(e => e.Attributes.GetValueOrDefault("row-index") is int r && r == row + 2
            && e.Attributes.GetValueOrDefault("column-index") is int c && c == column + 1 && e.Attributes.GetValueOrDefault("focusable") is true);

    private ElementNode Header(string text) => h.All("DataGridColumnHeader").Single(e => Equals(e.Attributes.GetValueOrDefault("accessible-name"), text));

    private static string Text(ElementNode element) => string.Concat(Descend(element).OfType<TextNode>().Select(t => t.Text));

    private static IEnumerable<Node> Descend(Node node)
    {
        yield return node;
        if (node is ElementNode element)
            foreach (var child in element.Children)
                foreach (var nested in Descend(child)) yield return nested;
    }

    [Fact]
    public void Renders_a_table_with_header_virtualized_rows_and_accessibility_structure()
    {
        Render();
        var grid = h.One("DataGrid");
        Assert.Equal("table", grid.Attributes["role"]);
        Assert.Equal("People", grid.Attributes["accessible-name"]);
        Assert.Equal(1002, grid.Attributes["row-count"]);
        Assert.Equal(4, grid.Attributes["column-count"]);
        Assert.Equal(true, grid.Attributes["multiselectable"]);
        var headers = h.All("DataGridColumnHeader").ToList();
        Assert.Equal(["Name", "Age", "City", "Salary"], headers.Select(e => (string)e.Attributes["accessible-name"]!));
        Assert.All(headers, e => Assert.Equal("columnheader", e.Attributes["role"]));
        Assert.Equal("none", headers[0].Attributes["sort"]);
        Assert.Equal(1, headers[0].Attributes["column-index"]);
        var rows = h.All("DataGridRow").ToList();
        Assert.InRange(rows.Count, 5, 40);
        Assert.Equal("row", rows[0].Attributes["role"]);
        Assert.Equal(2, rows[0].Attributes["row-index"]);
        Assert.Equal("rowheader", Cell(0, 0).Attributes["role"]);
        Assert.Equal("cell", Cell(0, 1).Attributes["role"]);
        Assert.Equal("Person 0000", Text(Cell(0, 0)));
        Assert.Equal("1,000", Text(Cell(0, 3)).Replace('.', ','));
        Assert.Equal("Σ " + h.Component<GridProbe>().People.Sum(p => p.Age), Text(h.All("DataGridFooterCell").ElementAt(1)));
        Assert.Equal(0, Cell(0, 0).Attributes["tabindex"]);
        Assert.Equal(-1, Cell(0, 1).Attributes["tabindex"]);
        Assert.Equal(true, rows[1].Attributes["alternate"]);
    }

    [Fact]
    public void Columns_have_fixed_and_flexible_widths_and_the_header_scrolls_with_the_body()
    {
        Render();
        Assert.Equal(120, h.Bounds(Header("Name")).Width, 1);
        Assert.Equal(80, h.Bounds(Header("Age")).Width, 1);
        Assert.True(h.Bounds(Header("City")).Width > 200);
        Assert.Equal(h.Bounds(Header("Age")).X, h.Bounds(Cell(0, 1)).X, 1);
        Assert.Equal(h.Bounds(Header("Age")).Width, h.Bounds(Cell(0, 1)).Width, 1);
        Assert.Equal(32, h.Bounds(h.All("DataGridRow").First()).Height, 1);
    }

    [Fact]
    public void Basic_draws_selected_rows_with_the_selection_token()
    {
        Render();
        h.Click(Cell(1, 2));
        var row = h.All("DataGridRow").ElementAt(1);
        Assert.Equal(true, row.Attributes["selected"]);
        var plain = h.Style(h.All("DataGridRow").ElementAt(2)).BackgroundColor;
        Assert.NotEqual(plain, h.Style(row).BackgroundColor);
        h.Snapshot("datagrid");
    }

    [Fact]
    public void Clicking_headers_sorts_ascending_descending_and_off_and_shift_adds_sorts()
    {
        var probe = Render();
        h.Click(Header("Age"));
        Assert.Equal([new DataSort("age", ListSortDirection.Ascending)], probe.Sorts);
        Assert.Equal("ascending", Header("Age").Attributes["sort"]);
        Assert.NotNull(Header("Age").Attributes["accessible-description"]);
        Assert.Single(h.All("DataGridSortMark"));
        Assert.Equal("20", Text(Cell(0, 1)));
        Assert.Equal("Person 0000", Text(Cell(0, 0)));
        Assert.Equal("Person 0050", Text(Cell(1, 0)));
        h.Click(Header("Age"));
        Assert.Equal("descending", Header("Age").Attributes["sort"]);
        Assert.Equal("69", Text(Cell(0, 1)));
        h.Click(Header("City"), KeyModifiers.Shift);
        Assert.Equal(2, probe.Sorts.Count);
        Assert.Equal("ascending", Header("City").Attributes["sort"]);
        Assert.Equal(2, h.All("DataGridSortIndex").Count());
        h.Click(Header("Age"));
        h.Click(Header("Age"));
        h.Click(Header("Age"));
        Assert.Empty(probe.Sorts);
        Assert.Equal("none", Header("Age").Attributes["sort"]);
        h.Click(Header("Salary"));
        Assert.Empty(probe.Sorts);
    }

    [Fact]
    public void Sorting_is_stable_and_follows_the_bound_sorts()
    {
        var probe = Render();
        Update(probe, p => p.Sorts = [new DataSort("city", ListSortDirection.Descending)]);
        Assert.Equal("Turin", Text(Cell(0, 2)));
        Assert.Equal("Person 0002", Text(Cell(0, 0)));
        Assert.Equal("Person 0005", Text(Cell(1, 0)));
    }

    [Fact]
    public void Arrow_keys_move_a_single_tab_stop_between_cells_and_the_header()
    {
        Render();
        h.Focus(Cell(0, 0));
        h.Press("ArrowRight");
        Assert.Same(Cell(0, 1), h.Focused);
        Assert.Equal(0, Cell(0, 1).Attributes["tabindex"]);
        Assert.Equal(-1, Cell(0, 0).Attributes["tabindex"]);
        Assert.Equal(true, Cell(0, 1).Attributes["current"]);
        h.Press("ArrowDown");
        Assert.Same(Cell(1, 1), h.Focused);
        h.Press("End");
        Assert.Same(Cell(1, 3), h.Focused);
        h.Press("Home");
        Assert.Same(Cell(1, 0), h.Focused);
        h.Press("ArrowUp");
        h.Press("ArrowUp");
        Assert.Equal("Name", h.Focused?.Attributes["accessible-name"]);
        h.Press("ArrowUp");
        Assert.Equal("Name", h.Focused?.Attributes["accessible-name"]);
        h.Press("ArrowDown");
        Assert.Same(Cell(0, 0), h.Focused);
        h.Press("End", KeyModifiers.Control);
        Assert.Same(Cell(999, 3), h.Focused);
        Assert.Equal("Person 0999", Text(Cell(999, 0)));
        h.Press("Home", KeyModifiers.Control);
        Assert.Same(Cell(0, 0), h.Focused);
        h.Press("PageDown");
        Assert.True(h.Focused?.Attributes["row-index"] is int r && r > 3);
    }

    [Fact]
    public void Enter_and_space_on_a_header_sort_and_alt_or_ctrl_arrows_move_and_resize_columns()
    {
        var probe = Render();
        h.Focus(Header("Age"));
        h.Press("Enter");
        Assert.Equal("ascending", Header("Age").Attributes["sort"]);
        h.Press(" ");
        Assert.Equal("descending", Header("Age").Attributes["sort"]);
        Assert.Single(probe.Sorts);
        h.Press("ArrowLeft", KeyModifiers.Alt);
        Assert.Equal(["Age", "Name", "City", "Salary"], h.All("DataGridColumnHeader").Select(e => (string)e.Attributes["accessible-name"]!));
        Assert.Equal("Age", h.Focused?.Attributes["accessible-name"]);
        var width = h.Bounds(Header("Age")).Width;
        h.Press("ArrowRight", KeyModifiers.Control);
        Assert.Equal(width + 10, h.Bounds(Header("Age")).Width, 1);
    }

    [Fact]
    public void Clicking_rows_selects_with_ctrl_toggles_and_shift_extends()
    {
        var probe = Render();
        h.Click(Cell(2, 1));
        Assert.Equal(2, probe.Selected?.Id);
        Assert.Single(probe.Many);
        h.Click(Cell(4, 1), KeyModifiers.Control);
        Assert.Equal([2, 4], probe.Many.Select(p => p.Id));
        h.Click(Cell(6, 1), KeyModifiers.Shift);
        Assert.Equal([4, 5, 6], probe.Many.Select(p => p.Id));
        h.Click(Cell(5, 1), KeyModifiers.Control);
        Assert.Equal([4, 6], probe.Many.Select(p => p.Id));
        Assert.Equal(true, h.All("DataGridRow").ElementAt(4).Attributes["selected"]);
        Assert.Equal(false, h.All("DataGridRow").ElementAt(5).Attributes["selected"]);
    }

    [Fact]
    public void Space_selects_shift_arrows_extend_and_ctrl_a_selects_everything()
    {
        var probe = Render();
        h.Focus(Cell(1, 1));
        h.Press(" ");
        Assert.Equal([1], probe.Many.Select(p => p.Id));
        h.Press("ArrowDown", KeyModifiers.Shift);
        h.Press("ArrowDown", KeyModifiers.Shift);
        Assert.Equal([1, 2, 3], probe.Many.Select(p => p.Id));
        h.Press(" ", KeyModifiers.Control);
        Assert.Equal([1, 2], probe.Many.Select(p => p.Id));
        h.Press("a", KeyModifiers.Control);
        Assert.Equal(1000, probe.Many.Count);
    }

    [Fact]
    public void Single_and_no_selection_modes()
    {
        var probe = Render();
        Update(probe, p => p.Mode = SelectionMode.Single);
        h.Focus(Cell(0, 0));
        h.Press(" ");
        Assert.Equal(0, probe.Selected?.Id);
        h.Press("ArrowDown");
        Assert.Equal(0, probe.Selected?.Id);
        Assert.Equal(false, h.One("DataGrid").Attributes["multiselectable"]);
        h.Click(Cell(3, 1), KeyModifiers.Control);
        Assert.Equal(3, probe.Selected?.Id);
        Update(probe, p => p.Selected = p.People[7]);
        Assert.Equal(true, h.All("DataGridRow").ElementAt(7).Attributes["selected"]);
        Update(probe, p => p.Mode = SelectionMode.None);
        h.Click(Cell(2, 1));
        Assert.DoesNotContain(h.All("DataGridRow"), r => r.Attributes["selected"] is true);
    }

    [Fact]
    public void The_selection_column_has_check_boxes_and_a_mixed_select_all_box()
    {
        var probe = Render();
        Update(probe, p => p.Checks = true);
        Assert.Equal(5, h.One("DataGrid").Attributes["column-count"]);
        ElementNode All() => h.One("DataGridSelectionHeader").Children.OfType<ElementNode>().Single(e => e.Name == "CheckBox");
        Assert.Equal(-1, All().Attributes["tabindex"]);
        var boxes = h.All("DataGridSelectionCell").Select(c => c.Children.OfType<ElementNode>().Single()).ToList();
        h.Click(boxes[1]);
        h.Click(h.All("DataGridSelectionCell").ElementAt(3).Children.OfType<ElementNode>().Single());
        Assert.Equal([1, 3], probe.Many.Select(p => p.Id));
        Assert.Equal(true, All().Attributes["indeterminate"]);
        h.Click(All());
        Assert.Equal(1000, probe.Many.Count);
        Assert.Equal(true, All().Attributes["checked"]);
        h.Click(All());
        Assert.Empty(probe.Many);
        h.Focus(Cell(2, 0));
        h.Press(" ");
        h.Press("Enter");
        h.Press(" ");
        Assert.Equal([2], probe.Many.Select(p => p.Id));
    }

    [Fact]
    public void Enter_edits_a_cell_commits_with_enter_and_cancels_with_escape()
    {
        var probe = Render();
        h.Focus(Cell(0, 1));
        h.Press("Enter");
        var editor = h.One("DataGridCellEditor");
        Assert.Same(editor, h.Focused);
        Assert.Equal("20", editor.Attributes["value"]);
        Assert.Equal(true, Cell(0, 1).Attributes["editing"]);
        h.Press("End");
        h.Press("Backspace");
        h.Press("Backspace");
        h.Type("33");
        h.Press("Enter");
        Assert.Empty(h.All("DataGridCellEditor"));
        Assert.Equal(33, probe.People[0].Age);
        var edit = Assert.Single(probe.Edits);
        Assert.Equal((object)20, edit.OldValue);
        Assert.Equal((object)33, edit.NewValue);
        Assert.Equal("age", edit.Column);
        Assert.Same(Cell(0, 1), h.Focused);
        h.Press("F2");
        h.Type("x");
        h.Press("Escape");
        Assert.Empty(h.All("DataGridCellEditor"));
        Assert.Equal(33, probe.People[0].Age);
        Assert.Same(Cell(0, 1), h.Focused);
    }

    [Fact]
    public void An_unparsable_value_keeps_the_editor_open_and_invalid()
    {
        var probe = Render();
        h.Focus(Cell(0, 1));
        h.Press("F2");
        h.Type("abc");
        h.Press("Enter");
        Assert.Equal(true, h.One("DataGridCellEditor").Attributes["invalid"]);
        Assert.Empty(probe.Edits);
        h.Press("Escape");
        Assert.Empty(h.All("DataGridCellEditor"));
    }

    [Fact]
    public void Moving_the_focus_out_of_the_editor_commits_and_double_click_edits()
    {
        var probe = Render();
        h.Focus(Cell(1, 0));
        h.Press("Enter");
        h.Press("End");
        h.Type("!");
        h.Click(Cell(3, 2));
        Assert.Empty(h.All("DataGridCellEditor"));
        Assert.Equal("Person 0001!", probe.People[1].Name);
        var cell = Cell(2, 0);
        var bounds = h.Bounds(cell);
        var point = new Nazor.Graphics.Point(bounds.X + 5, bounds.Y + 5);
        h.Input.PointerMoved(point, KeyModifiers.None);
        h.Input.PointerPressed(point, PointerButton.Primary, KeyModifiers.None);
        h.Input.PointerReleased(point, PointerButton.Primary, KeyModifiers.None);
        h.Input.PointerDoubleClicked(point, PointerButton.Primary, KeyModifiers.None);
        h.Frame();
        Assert.Single(h.All("DataGridCellEditor"));
        Assert.Same(h.One("DataGridCellEditor"), h.Focused);
        h.Press("Escape");
        cell = Cell(2, 2);
        bounds = h.Bounds(cell);
        point = new Nazor.Graphics.Point(bounds.X + 5, bounds.Y + 5);
        h.Input.PointerDoubleClicked(point, PointerButton.Primary, KeyModifiers.None);
        h.Frame();
        Assert.Equal(2, probe.Activated?.Id);
    }

    [Fact]
    public void Enter_on_a_read_only_cell_activates_the_row()
    {
        var probe = Render();
        h.Focus(Cell(4, 2));
        h.Press("Enter");
        Assert.Equal(4, probe.Activated?.Id);
        Assert.Empty(h.All("DataGridCellEditor"));
    }

    [Fact]
    public void Groups_show_headers_with_counts_that_open_and_close()
    {
        var probe = Render();
        Update(probe, p => p.Group = x => x.City);
        Assert.Equal("Milan", Text(h.All("DataGridGroupKey").First()));
        Assert.Contains("333", Text(h.All("DataGridGroupCount").First()));
        Assert.Equal(true, h.All("DataGridGroupHeader").First().Attributes["expanded"]);
        Assert.Equal("row", h.All("DataGridGroupHeader").First().Attributes["role"]);
        Assert.Equal(1000 + 3 + 2, h.One("DataGrid").Attributes["row-count"]);
        h.Click(h.All("DataGridGroupHeaderCell").First());
        Assert.Equal(false, h.All("DataGridGroupHeader").First().Attributes["expanded"]);
        Assert.Equal("Rome", Text(h.All("DataGridGroupKey").ElementAt(1)));
        h.Focus(h.All("DataGridGroupHeaderCell").First());
        h.Press("ArrowRight");
        Assert.Equal(true, h.All("DataGridGroupHeader").First().Attributes["expanded"]);
        h.Press("ArrowLeft");
        Assert.Equal(false, h.All("DataGridGroupHeader").First().Attributes["expanded"]);
        h.Press("Enter");
        Assert.Equal(true, h.All("DataGridGroupHeader").First().Attributes["expanded"]);
        h.Press("ArrowDown");
        Assert.Equal("Person 0001", Text(h.Focused!.ParentElement!.Children.OfType<ElementNode>().First()));
    }

    [Fact]
    public void Row_details_open_from_the_toggle_column()
    {
        var probe = Render();
        Update(probe, p => p.Details = person => b => b.AddContent(0, "Details of " + person.Name));
        Assert.Equal(5, h.One("DataGrid").Attributes["column-count"]);
        var toggle = h.All("DataGridDetailsCell").ElementAt(1);
        Assert.Equal(false, toggle.Attributes["expanded"]);
        h.Click(toggle);
        Assert.Equal("Details of Person 0001", Text(h.One("DataGridRowDetails")));
        Assert.Equal(true, h.All("DataGridRow").ElementAt(1).Attributes["expanded"]);
        h.Focus(h.All("DataGridDetailsCell").ElementAt(1));
        h.Press(" ");
        Assert.Empty(h.All("DataGridRowDetails"));
    }

    [Fact]
    public void Filter_hidden_columns_empty_content_and_loading_state()
    {
        var probe = Render();
        Update(probe, p => p.HideSalary = true);
        Assert.Equal(3, h.All("DataGridColumnHeader").Count());
        Update(probe, p => p.Filter = x => x.City == "Turin");
        Assert.Equal(333, (int)h.One("DataGrid").Attributes["row-count"]! - 2);
        Assert.Equal("Σ " + probe.People.Where(x => x.City == "Turin").Sum(x => x.Age), Text(h.All("DataGridFooterCell").ElementAt(1)));
        Update(probe, p => p.Filter = _ => false);
        Assert.Equal("Nobody", Text(h.One("DataGridEmpty")));
        Assert.Equal(true, h.One("DataGrid").Attributes["empty"]);
        Update(probe, p => p.Busy = true);
        Assert.Equal("status", h.One("DataGridLoading").Attributes["role"]);
        Assert.Empty(h.All("DataGridEmpty"));
    }

    [Fact]
    public void Frozen_columns_stay_in_place_while_the_body_scrolls_sideways()
    {
        var probe = Render();
        Update(probe, p => p.FreezeName = true);
        Assert.Equal(true, Header("Name").Attributes["frozen-last"]);
        Assert.Equal(true, Cell(0, 0).Attributes["frozen"]);
        Assert.Equal(h.Bounds(Header("Name")).X, h.Bounds(Cell(0, 0)).X, 1);
    }

    [Fact]
    public void Dragging_the_resize_handle_changes_the_column_width()
    {
        Render();
        var handle = Header("Name").Children.OfType<ElementNode>().Single(e => e.Name == "ResizeHandle");
        Assert.Equal(-1, handle.Attributes["tabindex"]);
        var bounds = h.Bounds(handle);
        var start = new Nazor.Graphics.Point(bounds.X + bounds.Width / 2, bounds.Y + bounds.Height / 2);
        h.Input.PointerMoved(start, KeyModifiers.None);
        h.Input.PointerPressed(start, PointerButton.Primary, KeyModifiers.None);
        h.Input.PointerMoved(new Nazor.Graphics.Point(start.X + 40, start.Y), KeyModifiers.None);
        h.Input.PointerReleased(new Nazor.Graphics.Point(start.X + 40, start.Y), PointerButton.Primary, KeyModifiers.None);
        h.Frame();
        Assert.Equal(160, h.Bounds(Header("Name")).Width, 1);
        Assert.Equal(160, h.Bounds(Cell(0, 0)).Width, 1);
        Assert.DoesNotContain(Header("Salary").Children.OfType<ElementNode>(), e => e.Name == "ResizeHandle");
    }

    [Fact]
    public void Dragging_a_header_reorders_the_columns_without_sorting()
    {
        var probe = Render();
        var content = Header("Name").Children.OfType<ElementNode>().Single(e => e.Name == "DataGridColumnHeaderContent");
        var bounds = h.Bounds(content);
        var start = new Nazor.Graphics.Point(bounds.X + 10, bounds.Y + bounds.Height / 2);
        h.Input.PointerMoved(start, KeyModifiers.None);
        h.Input.PointerPressed(start, PointerButton.Primary, KeyModifiers.None);
        h.Input.PointerMoved(new Nazor.Graphics.Point(start.X + 60, start.Y), KeyModifiers.None);
        h.Input.PointerMoved(new Nazor.Graphics.Point(start.X + 110, start.Y), KeyModifiers.None);
        h.Frame();
        Assert.Equal(true, Header("Name").Attributes["dragging"]);
        Assert.Equal("after", Header("Age").Attributes["drop-target"]);
        h.Input.PointerReleased(new Nazor.Graphics.Point(start.X + 110, start.Y), PointerButton.Primary, KeyModifiers.None);
        h.Frame();
        Assert.Equal(["Age", "Name", "City", "Salary"], h.All("DataGridColumnHeader").Select(e => (string)e.Attributes["accessible-name"]!));
        Assert.Empty(probe.Sorts);
        Assert.Equal("Person 0000", Text(Cell(0, 1)));
    }
}
