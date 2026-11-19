using Nazor.Controls.Tests.Navigation.Fixtures;
using Nazor.Input;
using Nazor.Rendering;

namespace Nazor.Controls.Tests.Navigation;

[Collection("Controls")]
public sealed class BreadcrumbTests : IDisposable
{
    private readonly ControlHarness h = new(width: 800, height: 360);
    private BreadcrumbProbe Probe => h.Component<BreadcrumbProbe>();
    public void Dispose() => h.Dispose();
    private void Frame() { for (var frame = 0; frame < 5; frame++) h.Frame(); }
    private void Show(double width = 320) { h.Render<BreadcrumbProbe>(); Probe.Width = width; Probe.Refresh(); Frame(); }
    private ElementNode Node(int index) => h.All("Button").Single(node => node.Attributes.GetValueOrDefault("breadcrumb-index") is int position && position == index);
    private ElementNode Overflow => h.All("MenuButton").Single(node => node.Class == "breadcrumb-overflow");

    [Fact]
    public void Width_overflow_keeps_the_tail_and_exposes_ancestors_in_an_ordered_menu_that_requests_navigation()
    {
        Show(240);
        Assert.InRange(Probe.Bar.HiddenItemCount, 1, Probe.Path.Length - 1);
        Assert.False(Node(Probe.Path.Length - 1).IsDisabled);
        Assert.All(h.All("BreadcrumbSlot").Take(Probe.Bar.HiddenItemCount), slot => Assert.True(slot.Attributes.GetValueOrDefault("semantic-hidden") is true));
        var hidden = Probe.Bar.HiddenItemCount;
        h.Click(Overflow); Frame();
        var entries = h.All("MenuItem").ToArray();
        Assert.Equal(hidden, entries.Length);
        Assert.Equal(Probe.Path.Take(hidden).Select(item => item.Label), entries.Select(node => node.Attributes["accessible-name"]));
        h.Click(entries[0]); Frame();
        var request = Assert.Single(Probe.Requests);
        Assert.Equal(0, request.Index);
        Assert.Equal("Home", request.Item.Label);
        Assert.Single(Probe.Path);
        Assert.Equal(0, Probe.Bar.HiddenItemCount);
    }

    [Fact]
    public void Header_is_one_tab_stop_and_arrows_home_end_and_reflow_repair_focus_without_navigation()
    {
        Show(740);
        var stops = h.All("Button").Where(node => node.Attributes.GetValueOrDefault("breadcrumb-index") is int && node.Attributes.GetValueOrDefault("tabindex") is 0).ToArray();
        Assert.Single(stops);
        h.Focus(Node(0));
        h.Press("End"); Frame();
        Assert.Same(Node(5), h.Input.Focused);
        h.Press("ArrowLeft"); Frame();
        Assert.Same(Node(4), h.Input.Focused);
        h.Press("Home"); Frame();
        Assert.Same(Node(0), h.Input.Focused);
        Probe.Width = 170; Probe.Refresh(); Frame();
        Assert.True(Probe.Bar.HiddenItemCount > 0);
        Assert.Same(Overflow, h.Input.Focused);
        Probe.Width = 740; Probe.Refresh(); Frame();
        Assert.Equal(0, Probe.Bar.HiddenItemCount);
        Assert.Same(Node(0), h.Input.Focused);
        Assert.Empty(Probe.Requests);
    }

    [Fact]
    public void Current_and_repeated_labels_report_the_exact_item_and_index_without_mutating_the_application_model()
    {
        Show(740);
        Probe.Path = [new(10, "Same"), new(11, "Same"), new(12, "Current")];
        Probe.Apply = false; Probe.Refresh(); Frame();
        h.Click(Node(1)); Frame();
        Assert.Equal(11, Probe.Requests[0].Item.Id);
        Assert.Equal(1, Probe.Requests[0].Index);
        h.Click(Node(2)); Frame();
        Assert.Equal(12, Probe.Requests[1].Item.Id);
        Assert.Equal(2, Probe.Requests[1].Index);
        Assert.Equal(3, Probe.Path.Length);
        Assert.Equal("Current location", Node(2).Attributes["accessible-description"]);
    }

    [Fact]
    public async Task Disabled_empty_and_removed_paths_cannot_invoke_stale_actions()
    {
        Show();
        Probe.Disabled = true; Probe.Refresh(); Frame();
        await Probe.Bar.InvokeAsync(0); Frame();
        Assert.Empty(Probe.Requests);
        Assert.False(h.Input.Activate(Node(5)));
        Probe.Disabled = false; Probe.Path = []; Probe.Refresh(); Frame();
        Assert.Empty(h.All("BreadcrumbSlot"));
        Assert.Equal(0, Probe.Bar.HiddenItemCount);
        Assert.True(Overflow.IsDisabled);
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => Probe.Bar.InvokeAsync(0));
    }

    [Fact]
    public void Removing_a_focused_tail_restores_the_nearest_surviving_node_and_preserves_retained_headers()
    {
        Show(740);
        var retained = Node(2);
        h.Focus(Node(5));
        Probe.Path = Probe.Path.Take(3).ToArray(); Probe.Refresh(); Frame();
        Assert.Same(retained, Node(2));
        Assert.Same(Node(2), h.Input.Focused);
        Assert.Empty(Probe.Requests);
    }

    [Fact]
    public void Duplicate_explicit_keys_are_rejected_before_rendering_a_conflicting_path()
    {
        Show();
        Probe.DuplicateKeys = true;
        var error = Assert.Throws<ArgumentException>(() => Probe.Refresh());
        Assert.Equal("ItemKey", error.ParamName);
    }
}
