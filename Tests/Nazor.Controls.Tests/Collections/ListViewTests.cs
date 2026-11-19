using Nazor.Controls.Tests.Collections.Fixtures;
using Nazor.Input;
using Nazor.Style;

namespace Nazor.Controls.Tests.Collections;

[Collection("Controls")]
public sealed class ListViewTests : IDisposable
{
    private readonly ControlHarness h = new();

    public void Dispose() => h.Dispose();

    private ElementNode List(string name) => h.ByClass(name);

    private List<ElementNode> Rows(string list) => [.. List(list).Elements().Where(e => e.Name == "ListViewItem")];

    private ElementNode Row(string list, int index) => Rows(list).Single(e => (int)e.Attributes["index"]! == index);

    private void Refresh(IRefreshable probe)
    {
        h.Host.Renderer.Dispatcher.InvokeAsync(probe.Refresh);
        h.Frame();
    }

    [Fact]
    public void The_list_exposes_a_listbox_of_options_with_positions()
    {
        h.Render<ListViewProbe>();
        var list = List("fruits");
        Assert.Equal("listbox", list.Attributes["role"]);
        Assert.Equal("Fruits", list.Attributes["accessible-name"]);
        Assert.Equal(false, list.Attributes["multiselectable"]);
        var rows = Rows("fruits");
        Assert.Equal(6, rows.Count);
        Assert.All(rows, row => Assert.Equal("option", row.Attributes["role"]));
        Assert.Equal(2, rows[1].Attributes["position-in-set"]);
        Assert.Equal(6, rows[1].Attributes["size-of-set"]);
        Assert.Equal("Apricot", rows[1].Elements().Single(e => e.Name == "ListViewItemContent").Text());
        Assert.Equal(true, rows[4].Attributes["disabled"]);
        Assert.Equal(true, List("numbers").Attributes["multiselectable"]);
    }

    [Fact]
    public void The_list_is_a_single_tab_stop_on_the_first_row()
    {
        h.Render<ListViewProbe>();
        var rows = Rows("fruits");
        Assert.Equal(0, rows[0].Attributes["tabindex"]);
        Assert.All(rows.Skip(1), row => Assert.Equal(-1, row.Attributes["tabindex"]));
        Assert.Equal(false, List("fruits").Attributes["focusable"]);
    }

    [Fact]
    public void A_click_selects_the_row_and_binds_the_item()
    {
        h.Render<ListViewProbe>();
        var probe = h.Component<ListViewProbe>();
        h.Click(Row("fruits", 2));
        Assert.Equal("Banana", probe.Selected);
        Assert.Equal(true, Row("fruits", 2).Attributes["selected"]);
        Assert.Equal(true, Row("fruits", 2).Attributes["current"]);
        Assert.Equal(0, Row("fruits", 2).Attributes["tabindex"]);
        Assert.Same(Row("fruits", 2), h.Focused);
    }

    [Fact]
    public void Disabled_rows_cannot_be_selected()
    {
        h.Render<ListViewProbe>();
        var probe = h.Component<ListViewProbe>();
        h.Click(Row("fruits", 4));
        Assert.Null(probe.Selected);
    }

    [Fact]
    public void The_arrows_move_the_current_row_and_the_selection_skipping_disabled_rows()
    {
        h.Render<ListViewProbe>();
        var probe = h.Component<ListViewProbe>();
        h.Focus(Row("fruits", 0));
        h.Press("ArrowDown");
        Assert.Equal("Apricot", probe.Selected);
        Assert.Same(Row("fruits", 1), h.Focused);
        h.Press("End");
        Assert.Equal("Elderberry", probe.Selected);
        h.Press("ArrowUp");
        Assert.Equal("Cherry", probe.Selected);
        h.Press("Home");
        Assert.Equal("Apple", probe.Selected);
        Assert.Equal(true, Row("fruits", 0).Attributes["current"]);
    }

    [Fact]
    public void Ctrl_with_an_arrow_moves_without_selecting_and_space_selects()
    {
        h.Render<ListViewProbe>();
        var probe = h.Component<ListViewProbe>();
        h.Click(Row("fruits", 0));
        h.Press("ArrowDown", KeyModifiers.Control);
        Assert.Equal("Apple", probe.Selected);
        Assert.Equal(true, Row("fruits", 1).Attributes["current"]);
        Assert.Same(Row("fruits", 1), h.Focused);
        h.Press(" ");
        Assert.Equal("Apricot", probe.Selected);
    }

    [Fact]
    public void Typing_jumps_to_the_next_matching_row()
    {
        h.Render<ListViewProbe>();
        var probe = h.Component<ListViewProbe>();
        h.Focus(Row("fruits", 0));
        h.Press("c");
        Assert.Equal("Cherry", probe.Selected);
        h.Press("h");
        Assert.Equal("Cherry", probe.Selected);
        h.Press("x");
        Assert.Equal("Cherry", probe.Selected);
    }

    [Fact]
    public void Typing_the_same_letter_cycles_through_the_matching_rows()
    {
        h.Render<ListViewProbe>();
        var probe = h.Component<ListViewProbe>();
        h.Focus(Row("fruits", 0));
        h.Press("a");
        Assert.Equal("Apricot", probe.Selected);
        h.Press("a");
        Assert.Equal("Apple", probe.Selected);
    }

    [Fact]
    public void Enter_and_a_double_click_activate_the_row()
    {
        h.Render<ListViewProbe>();
        var probe = h.Component<ListViewProbe>();
        h.Focus(Row("fruits", 0));
        h.Press("ArrowDown");
        h.Press("Enter");
        Assert.Equal("Apricot", probe.Activated);
        var row = Row("fruits", 3);
        var bounds = h.Bounds(row);
        var point = new Nazor.Graphics.Point(bounds.X + 5, bounds.Y + 5);
        h.Input.PointerMoved(point, KeyModifiers.None);
        h.Input.PointerPressed(point, PointerButton.Primary, KeyModifiers.None);
        h.Input.PointerReleased(point, PointerButton.Primary, KeyModifiers.None);
        h.Input.PointerDoubleClicked(point, PointerButton.Primary, KeyModifiers.None);
        h.Frame();
        Assert.Equal("Cherry", probe.Activated);
    }

    [Fact]
    public void A_bound_selection_set_by_the_parent_is_shown()
    {
        h.Render<ListViewProbe>();
        var probe = h.Component<ListViewProbe>();
        probe.Selected = "Cherry";
        Refresh(probe);
        Assert.Equal(true, Row("fruits", 3).Attributes["selected"]);
        Assert.Equal(0, Row("fruits", 3).Attributes["tabindex"]);
        Assert.Equal(false, Row("fruits", 0).Attributes["selected"]);
    }

    [Fact]
    public void A_hundred_thousand_items_realize_only_the_visible_rows()
    {
        h.Render<ListViewProbe>();
        Assert.InRange(Rows("numbers").Count, 4, 16);
        h.Input.ScrollTo(List("numbers").Elements().First(e => e.Name == "ListViewViewport"), null, 20 * 5000);
        h.Frame();
        var first = Rows("numbers").Min(e => (int)e.Attributes["index"]!);
        Assert.InRange(first, 4990, 5000);
        Assert.InRange(Rows("numbers").Count, 4, 16);
        foreach (var row in Rows("numbers"))
        {
            var position = Assert.IsType<LogicalItemPosition>(row.Attributes["item-position"]);
            Assert.Equal((int)row.Attributes["index"]! + 1, position.Position);
            Assert.Equal(10_000, position.Count);
            Assert.Equal(position.Position % 2 == 0, new ItemPositionSelector(2, 0, false, ":nth-item(even)").Matches(row));
        }
        Assert.All(List("numbers").Elements().Where(e => e.Name.EndsWith("Spacer", StringComparison.Ordinal)), spacer => Assert.False(new ItemPositionSelector(1, 0, false, ":nth-item(n)").Matches(spacer)));
    }

    [Fact]
    public void Grouped_logical_positions_restart_per_group_and_exclude_headers()
    {
        h.Render<ListViewProbe>();
        var positions = Rows("people").Select(row => Assert.IsType<LogicalItemPosition>(row.Attributes["item-position"])).ToArray();
        Assert.Equal([1, 2, 1, 2, 1], positions.Select(p => p.Position));
        Assert.Equal([2, 2, 2, 2, 1], positions.Select(p => p.Count));
        Assert.Equal(positions[0].Sequence, positions[1].Sequence);
        Assert.NotEqual(positions[0].Sequence, positions[2].Sequence);
        Assert.All(List("people").Elements().Where(e => e.Name == "ListViewSectionHeader"), header => Assert.False(header.Attributes.ContainsKey("item-position")));
    }

    [Fact]
    public void Multiple_selection_extends_with_shift_toggles_with_ctrl_and_selects_all()
    {
        h.Render<ListViewProbe>();
        var probe = h.Component<ListViewProbe>();
        h.Click(Row("numbers", 1));
        h.Click(Row("numbers", 3), KeyModifiers.Shift);
        Assert.Equal([1, 2, 3], probe.Picked);
        h.Click(Row("numbers", 5), KeyModifiers.Control);
        Assert.Equal([1, 2, 3, 5], probe.Picked);
        h.Click(Row("numbers", 2), KeyModifiers.Control);
        Assert.Equal([1, 3, 5], probe.Picked);
        h.Press("ArrowDown", KeyModifiers.Shift);
        Assert.Equal([2, 3], probe.Picked);
        h.Press("a", KeyModifiers.Control);
        Assert.Equal(10_000, probe.Picked.Count);
    }

    [Fact]
    public void Page_down_moves_by_a_screen_and_keeps_the_current_row_visible()
    {
        h.Render<ListViewProbe>();
        var probe = h.Component<ListViewProbe>();
        h.Click(Row("numbers", 0));
        h.Press("PageDown");
        var picked = Assert.Single(probe.Picked);
        Assert.InRange(picked, 3, 6);
        h.Press("End");
        Assert.Equal([9_999], probe.Picked);
        var last = Row("numbers", 9_999);
        Assert.Same(last, h.Focused);
        var viewport = h.Bounds(List("numbers").Elements().First(e => e.Name == "ListViewViewport"));
        Assert.True(h.Bounds(last).Bottom <= viewport.Bottom + 0.5);
    }

    [Fact]
    public void Check_boxes_toggle_rows_and_the_arrows_do_not_select()
    {
        h.Render<ListViewProbe>();
        var probe = h.Component<ListViewProbe>();
        probe.Checks = true;
        Refresh(probe);
        Assert.Equal(Rows("numbers").Count, List("numbers").Elements().Count(e => e.Name == "ListViewItemCheck"));
        h.Click(Row("numbers", 0));
        h.Click(Row("numbers", 2));
        Assert.Equal([0, 2], probe.Picked);
        h.Press("ArrowDown");
        Assert.Equal([0, 2], probe.Picked);
        h.Press(" ");
        Assert.Equal([0, 2, 3], probe.Picked);
        var check = Row("numbers", 3).Elements().Single(e => e.Name == "ListViewItemCheck");
        Assert.Equal("check", check.Attributes["glyph"]);
        Assert.Equal(true, check.Attributes["semantic-hidden"]);
    }

    [Fact]
    public void Groups_add_section_headers_and_positions_within_each_group()
    {
        h.Render<ListViewProbe>();
        var probe = h.Component<ListViewProbe>();
        var headers = List("people").Elements().Where(e => e.Name == "ListViewSectionHeader").Select(e => e.Text()).ToList();
        Assert.Equal(["Design", "Engineering", "Sales"], headers);
        var carla = Rows("people").Single(e => e.Text() == "Carla");
        Assert.Equal(2, carla.Attributes["position-in-set"]);
        Assert.Equal(2, carla.Attributes["size-of-set"]);
        Assert.Equal("Design", carla.Attributes["accessible-description"]);
        Assert.Equal("Carla", carla.Attributes["accessible-name"]);
        h.Focus(Rows("people")[0]);
        h.Press("ArrowDown");
        h.Press("ArrowDown");
        Assert.Equal("Bruno", probe.Chosen?.Name);
    }

    [Fact]
    public void An_empty_list_shows_its_empty_content()
    {
        h.Render<ListViewLoadingProbe>();
        var empty = List("empty");
        Assert.Equal(true, empty.Attributes["empty"]);
        Assert.Equal("Nothing here", empty.Elements().Single(e => e.Name == "ListViewEmpty").Text());
        Assert.DoesNotContain(empty.Elements(), e => e.Name == "ListViewViewport");
    }

    [Fact]
    public void Reaching_the_end_requests_more_items_once_per_count()
    {
        h.Render<ListViewLoadingProbe>();
        var probe = h.Component<ListViewLoadingProbe>();
        Assert.Equal(0, probe.Requests);
        probe.Stop = true;
        h.Input.ScrollTo(List("paged").Elements().First(e => e.Name == "ListViewViewport"), null, 10_000);
        h.Frame();
        Assert.Equal(1, probe.Requests);
        h.Input.ScrollTo(List("paged").Elements().First(e => e.Name == "ListViewViewport"), null, 0);
        h.Frame();
        h.Input.ScrollTo(List("paged").Elements().First(e => e.Name == "ListViewViewport"), null, 10_000);
        h.Frame();
        Assert.Equal(1, probe.Requests);
        probe.Stop = false;
        probe.Paged.AddRange(Enumerable.Range(20, 20));
        Refresh(probe);
        h.Input.ScrollTo(List("paged").Elements().First(e => e.Name == "ListViewViewport"), null, 10_000);
        h.Frame();
        Assert.Equal(2, probe.Requests);
        Assert.True(probe.Paged.Count >= 60);
    }

    [Fact]
    public void The_loading_part_announces_more_items()
    {
        h.Render<ListViewLoadingProbe>();
        var probe = h.Component<ListViewLoadingProbe>();
        probe.Loading = true;
        Refresh(probe);
        var loading = List("paged").Elements().Single(e => e.Name == "ListViewLoadingMore");
        Assert.Equal("status", loading.Attributes["role"]);
        Assert.Equal("Loading", loading.Text());
    }

    [Fact]
    public void The_basic_theme_paints_selected_rows_with_the_selection_color()
    {
        h.Render<ListViewProbe>();
        h.Click(Row("fruits", 1));
        Assert.Equal(SystemColors.Resolve().Highlight, h.Style(Row("fruits", 1)).BackgroundColor);
        Assert.NotEqual(SystemColors.Resolve().Highlight, h.Style(Row("fruits", 0)).BackgroundColor);
        Assert.Equal(20, h.Bounds(Row("fruits", 0)).Height, 1);
    }
    [Fact]
    public void Scrolling_the_focused_row_away_keeps_the_focus_in_the_list()
    {
        h.Render<ListViewProbe>();
        var probe = h.Component<ListViewProbe>();
        h.Click(Row("numbers", 0));
        h.Input.ScrollTo(List("numbers").Elements().First(e => e.Name == "ListViewViewport"), null, 20 * 5000);
        h.Frame();
        Assert.Same(List("numbers"), h.Focused);
        Assert.Equal(0, List("numbers").Attributes["tabindex"]);
        h.Press("ArrowDown");
        Assert.Equal([1], probe.Picked);
        Assert.Same(Row("numbers", 1), h.Focused);
        Assert.Equal(false, List("numbers").Attributes["focusable"]);
    }

    [Fact]
    public void Tabbing_into_a_list_whose_current_row_is_scrolled_away_brings_it_back()
    {
        h.Render<ListViewProbe>();
        h.Click(Row("numbers", 2));
        h.Focus(Row("fruits", 0));
        h.Input.ScrollTo(List("numbers").Elements().First(e => e.Name == "ListViewViewport"), null, 20 * 5000);
        h.Frame();
        Assert.Equal(0, List("numbers").Attributes["tabindex"]);
        h.Focus(List("numbers"));
        h.Frame();
        Assert.Same(Row("numbers", 2), h.Focused);
    }
}
