using Nazor.Controls.Tests.Collections.Fixtures;
using Nazor.Graphics;
using Nazor.Input;
using Nazor.Rendering;

namespace Nazor.Controls.Tests.Collections;

[Collection("Controls")]
public sealed class CollectionReorderTests : IDisposable
{
    private readonly ControlHarness h = new(width: 600, height: 500);
    private ReorderProbe Probe => h.Component<ReorderProbe>();
    private string RowName => Probe.Kind switch { "grid" => "GridViewItem", "tree" => "TreeViewItem", _ => "ListViewItem" };
    public void Dispose() => h.Dispose();
    private void Show(string kind)
    {
        h.Render<ReorderProbe>();
        Probe.Kind = kind;
        Probe.Refresh(); h.Frame();
    }
    private ElementNode Row(int id) => h.All(RowName).Single(row => row.Attributes.GetValueOrDefault("accessible-name") is string name && name == "Item " + id);
    private Point At(ElementNode row, double fraction = .5)
    {
        var box = h.Bounds(row);
        return Probe.Kind == "grid" ? new(box.X + (float)(box.Width * fraction), box.Y + box.Height / 2) :
            new(box.X + box.Width / 2, box.Y + (float)(box.Height * fraction));
    }
    private void Down(Point point) { h.Input.PointerPressed(point, PointerButton.Primary, KeyModifiers.None); h.Frame(); }
    private void Move(Point point) { h.Input.PointerMoved(point, KeyModifiers.None); h.Frame(); }
    private void Up(Point point) { h.Input.PointerReleased(point, PointerButton.Primary, KeyModifiers.None); h.Frame(); }

    [Theory]
    [InlineData("list")]
    [InlineData("grid")]
    [InlineData("tree")]
    public void Keyboard_reordering_preserves_selection_and_current_item_without_an_extra_selection_event(string kind)
    {
        Show(kind);
        h.Focus(Row(2));
        h.Press(kind == "grid" ? "ArrowRight" : "ArrowDown", KeyModifiers.Control | KeyModifiers.Shift);
        var request = Assert.Single(Probe.Requests);
        Assert.Equal(2, request.Item.Id);
        Assert.Equal(1, request.OldIndex);
        Assert.Equal(2, request.NewIndex);
        Assert.Equal(new[] { 1, 3, 2, 4, 5, 6, 7, 8, 9 }, Probe.Items.Select(item => item.Id));
        Assert.Equal(2, Probe.Selected?.Id);
        Assert.Equal(0, Probe.SelectionChanges);
        Assert.True(Row(2).Attributes.GetValueOrDefault("current") is true);
        Assert.Same(Row(2), h.Input.Focused);
    }

    [Theory]
    [InlineData("list")]
    [InlineData("grid")]
    [InlineData("tree")]
    public void Pointer_drop_requests_one_final_index_and_suppresses_the_drag_release_click(string kind)
    {
        Show(kind);
        var start = At(Row(2));
        var end = At(Row(4), .8);
        Down(start); Move(end);
        Assert.True(new ElementReference(Row(2)).IsPointerCaptured);
        Assert.Empty(Probe.Requests);
        Assert.True(Row(4).Attributes.GetValueOrDefault("drop-after") is true);
        Up(end);
        var request = Assert.Single(Probe.Requests);
        Assert.Equal(1, request.OldIndex);
        Assert.Equal(3, request.NewIndex);
        Assert.Equal(new[] { 1, 3, 4, 2, 5, 6, 7, 8, 9 }, Probe.Items.Select(item => item.Id));
        Assert.Equal(0, Probe.SelectionChanges);
        Assert.Equal(0, Probe.Activations);
        h.Click(Row(5));
        Assert.Equal(5, Probe.Selected?.Id);
        Assert.Equal(1, Probe.SelectionChanges);
    }

    [Theory]
    [InlineData("list", "escape")]
    [InlineData("list", "capture")]
    [InlineData("list", "disable")]
    [InlineData("list", "mutation")]
    [InlineData("grid", "escape")]
    [InlineData("grid", "capture")]
    [InlineData("grid", "disable")]
    [InlineData("grid", "mutation")]
    [InlineData("tree", "escape")]
    [InlineData("tree", "capture")]
    [InlineData("tree", "disable")]
    [InlineData("tree", "mutation")]
    public void Canceled_or_stale_gestures_cannot_commit(string kind, string reason)
    {
        Show(kind);
        Down(At(Row(2))); var end = At(Row(4), .8); Move(end);
        switch (reason)
        {
            case "escape": h.Press("Escape"); break;
            case "capture": h.Input.PointerCanceled(clearHover: false); break;
            case "disable": Probe.Disabled = true; Probe.Refresh(); h.Frame(); break;
            case "mutation": Probe.Items.Reverse(); Probe.Refresh(); h.Frame(); break;
        }
        Up(end);
        Assert.Empty(Probe.Requests);
        Assert.False(Probe.View.Element.IsPointerCaptured);
    }

    [Theory]
    [InlineData("list")]
    [InlineData("grid")]
    [InlineData("tree")]
    public void Reordering_is_opt_in_and_interactive_descendants_keep_their_pointer_gesture(string kind)
    {
        Show(kind);
        Probe.Enabled = false; Probe.Refresh(); h.Frame();
        Down(At(Row(2))); Move(At(Row(4), .8)); Up(At(Row(4), .8));
        Assert.Empty(Probe.Requests);
        Probe.Enabled = true; Probe.Buttons = true; Probe.Refresh(); h.Frame();
        var button = h.All("Button").Single(element => element.Id == "child-2");
        var box = h.Bounds(button); var start = new Point(box.X + box.Width / 2, box.Y + box.Height / 2);
        Down(start); Move(At(Row(4), .8)); Up(At(Row(4), .8));
        Assert.Empty(Probe.Requests);
        h.Click(button);
        Assert.Equal(1, Probe.ButtonClicks);
    }

    [Fact]
    public void List_reordering_rejects_cross_group_moves_and_reports_original_source_indexes()
    {
        Show("list");
        Probe.Grouped = true; Probe.Refresh(); h.Frame();
        Down(At(Row(2))); var end = At(Row(4), .8); Move(end); Up(end);
        Assert.Empty(Probe.Requests);
        Down(At(Row(2))); end = At(Row(3), .8); Move(end); Up(end);
        var request = Assert.Single(Probe.Requests);
        Assert.Equal(1, request.OldIndex);
        Assert.Equal(2, request.NewIndex);
    }

    [Theory]
    [InlineData("list")]
    [InlineData("grid")]
    [InlineData("tree")]
    public void An_item_disabled_during_a_gesture_cancels_capture_and_cannot_commit(string kind)
    {
        Show(kind);
        var source = Row(2);
        Down(At(source)); var end = At(Row(4), .8); Move(end);
        Probe.DisabledItem = 2; Probe.Refresh(); h.Frame();
        Assert.False(new ElementReference(source).IsPointerCaptured);
        Up(end);
        Assert.Empty(Probe.Requests);
    }

    [Theory]
    [InlineData("list")]
    [InlineData("grid")]
    [InlineData("tree")]
    public void A_request_does_not_mutate_an_application_model_that_declines_the_move(string kind)
    {
        Show(kind);
        Probe.Apply = false; Probe.Refresh(); h.Frame();
        h.Focus(Row(2));
        h.Press(kind == "grid" ? "ArrowRight" : "ArrowDown", KeyModifiers.Control | KeyModifiers.Shift);
        Assert.Single(Probe.Requests);
        Assert.Equal(Enumerable.Range(1, 9), Probe.Items.Select(item => item.Id));
        Assert.Equal(2, Probe.Selected?.Id);
    }

    [Fact]
    public async Task Tree_moves_whole_expanded_subtrees_within_siblings_and_rejects_reparenting()
    {
        Show("tree");
        Probe.Items[0].Children.AddRange([new(11), new(12)]);
        Probe.Refresh(); h.Frame();
        var tree = (TreeView<ReorderEntry>)Probe.View;
        await tree.SetExpandedAsync(Probe.Items[0], true); h.Frame();
        Down(At(Row(1))); Move(At(Row(3), .8)); Up(At(Row(3), .8));
        Assert.Equal(0, Assert.Single(Probe.Requests).OldIndex);
        Assert.Equal(2, Probe.Requests[0].NewIndex);
        Assert.Equal(1, Probe.Items[2].Id);
        Assert.True(tree.Model.IsExpanded(Probe.Items[2]));
        Assert.Equal(new[] { 11, 12 }, Probe.Items[2].Children.Select(item => item.Id));
        Probe.Requests.Clear();
        Down(At(Row(11))); Move(At(Row(4), .8)); Up(At(Row(4), .8));
        Assert.Empty(Probe.Requests);
    }
}
