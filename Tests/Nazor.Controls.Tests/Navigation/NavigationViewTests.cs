using Nazor.Controls.Tests.Navigation.Fixtures;
using Nazor.Input;
using Nazor.Style;

namespace Nazor.Controls.Tests.Navigation;

[Collection("Controls")]
public sealed class NavigationViewTests : IDisposable
{
    private readonly ControlHarness h = new(width: 1300, height: 500);
    private NavigationViewProbe Probe => h.Component<NavigationViewProbe>();
    public void Dispose() => h.Dispose();
    private void Show(Action<NavigationViewProbe>? setup = null)
    {
        h.Render<NavigationViewProbe>(); setup?.Invoke(Probe); Probe.Refresh(); Frame();
    }
    private void Frame() { for (var pass = 0; pass < 5; pass++) h.Frame(); }
    private ElementNode TreeItem(string text) => h.All("TreeViewItem").Single(element => element.Attributes.GetValueOrDefault("accessible-name") is string name && name == text);
    private ElementNode RootItem(string text) => h.All("NavigationViewRootItem").Single(element => element.Attributes.GetValueOrDefault("accessible-name") is string name && name == text);
    private ElementNode Input(string id) => h.Root.Elements().Single(element => element.Id == id).Elements().Single(element => element.Name == "TextFieldInput");
    private void SetMode(NavigationViewPaneDisplayMode mode) { Probe.Mode = mode; Probe.Refresh(); Frame(); }

    [Theory]
    [InlineData(1100, NavigationViewPaneDisplayMode.Left, 320)]
    [InlineData(1008, NavigationViewPaneDisplayMode.Left, 320)]
    [InlineData(1007, NavigationViewPaneDisplayMode.Compact, 48)]
    [InlineData(641, NavigationViewPaneDisplayMode.Compact, 48)]
    [InlineData(640, NavigationViewPaneDisplayMode.Minimal, 0)]
    [InlineData(500, NavigationViewPaneDisplayMode.Minimal, 0)]
    public void Auto_uses_actual_available_width_and_reserves_only_the_resolved_pane_extent(double width, NavigationViewPaneDisplayMode expected, double pane)
    {
        Show(probe => probe.Width = width);
        Assert.Equal(expected, Probe.View.EffectivePaneDisplayMode);
        var content = h.Bounds(h.One("SplitViewContent"));
        var root = h.Bounds(h.One("NavigationView"));
        Assert.InRange(content.X - root.X, pane - 1, pane + 1);
        Assert.Equal(expected == NavigationViewPaneDisplayMode.Left, Probe.View.PaneOpen);
    }

    [Fact]
    public void Content_pane_slots_and_expanded_hierarchy_keep_identity_across_modes()
    {
        Show();
        var main = Input("main-editor"); var pane = Input("pane-editor");
        var tree = h.One("TreeView");
        h.Click(TreeItem("Projects").Elements().Single(element => element.Name == "TreeViewExpander")); Frame();
        var expanded = TreeItem("Projects");
        Assert.True(expanded.Attributes.GetValueOrDefault("expanded") is true);
        Assert.NotNull(TreeItem("Active projects"));
        SetMode(NavigationViewPaneDisplayMode.Top);
        Assert.Same(main, Input("main-editor")); Assert.Same(pane, Input("pane-editor"));
        Assert.Equal(Display.None, h.Style(h.One("NavigationViewTreeHost")).Display);
        Assert.False(h.Input.Activate(expanded));
        Assert.True(h.One("NavigationViewTreeHost").Attributes.GetValueOrDefault("semantic-hidden") is true);
        SetMode(NavigationViewPaneDisplayMode.Left);
        Assert.Same(tree, h.One("TreeView"));
        Assert.True(TreeItem("Projects").Attributes.GetValueOrDefault("expanded") is true);
        Assert.Same(main, Input("main-editor"));
    }

    [Fact]
    public void Pointer_and_Enter_invoke_application_routes_once_and_modified_click_cannot_clear_the_route()
    {
        Show(); h.Click(TreeItem("Home")); Frame();
        Assert.Equal(new[] { "home" }, Probe.Invoked);
        Assert.Equal(new[] { "home" }, Probe.Selections);
        Assert.Equal(new[] { "invoke:home", "selection:home" }, Probe.Events);
        h.Click(TreeItem("Home"), KeyModifiers.Control); Frame();
        Assert.Equal("home", Probe.View.CurrentItem?.Key);
        Assert.True(TreeItem("Home").Attributes.GetValueOrDefault("selected") is true);
        h.Focus(TreeItem("Settings")); h.Press("Enter"); Frame();
        Assert.Equal(new[] { "home", "home", "settings" }, Probe.Invoked);
        Assert.Equal("settings", Probe.Selected?.Key);
    }

    [Theory]
    [InlineData("escape")]
    [InlineData("scrim")]
    [InlineData("toggle")]
    public async Task Covering_pane_dismissal_returns_hidden_navigation_focus_to_its_toggle(string action)
    {
        Show(probe => probe.Width = 500); await Probe.View.SetPaneOpenAsync(true); Frame();
        h.Focus(TreeItem("Projects"));
        switch (action)
        {
            case "escape": h.Press("Escape"); break;
            case "scrim": h.Click(h.One("SplitViewScrim")); break;
            case "toggle": h.Click(h.One("NavigationViewToggle")); break;
        }
        Frame(); Assert.False(Probe.View.PaneOpen);
        Assert.Same(h.One("NavigationViewToggle"), h.Focused);
        Assert.False(h.Input.Activate(TreeItem("Home")));
    }

    [Fact]
    public async Task Routing_and_keyboard_focus_leaving_the_pane_preserve_application_content_focus()
    {
        Show(probe => { probe.Width = 500; probe.FocusRouteContent = true; });
        await Probe.View.SetPaneOpenAsync(true); Frame(); h.Click(TreeItem("Home")); Frame();
        Assert.False(Probe.View.PaneOpen); Assert.Same(Input("main-editor"), h.Focused);
        await Probe.View.SetPaneOpenAsync(true); Frame(); h.Focus(TreeItem("Projects"));
        h.Focus(Input("main-editor")); Frame();
        Assert.False(Probe.View.PaneOpen); Assert.Same(Input("main-editor"), h.Focused);
    }

    [Fact]
    public void Top_navigation_reveals_keyboard_targets_at_narrow_width_and_opens_nested_menu_items()
    {
        Show(probe =>
        {
            probe.Mode = NavigationViewPaneDisplayMode.Top; probe.Width = 280;
            for (var index = 0; index < 12; index++) probe.Items.Add(new("extra-" + index, "Additional section " + index));
        });
        h.Focus(RootItem("Home")); h.Press("End"); Frame();
        Assert.Same(RootItem("Additional section 11"), h.Focused);
        var visible = h.Bounds(h.One("NavigationViewRootScroller")); var item = h.Bounds(h.Focused!);
        Assert.True(item.X >= visible.X - 1 && item.Right <= visible.Right + 1, $"Item {item} must fit root viewport {visible}.");
        h.Focus(RootItem("Projects")); h.Press("ArrowDown"); Frame();
        Assert.NotEmpty(h.All("Menu"));
        var active = h.All("MenuItem").Single(element => element.Text() == "Active projects");
        h.Click(active); Frame();
        Assert.Equal("active", Probe.Selected?.Key); Assert.Equal(new[] { "active" }, Probe.Invoked);
    }

    [Fact]
    public void Header_and_nested_editor_arrow_keys_do_not_navigate_or_invoke_routes()
    {
        Show(); h.Focus(Input("header-editor")); h.Press("ArrowRight"); h.Press("ArrowDown");
        Assert.Empty(Probe.Selections); Assert.Empty(Probe.Invoked);
        SetMode(NavigationViewPaneDisplayMode.Top);
        h.Focus(Input("header-editor")); h.Press("ArrowRight");
        Assert.Same(Input("header-editor"), h.Focused);
    }

    [Fact]
    public void Switching_focused_last_item_to_narrow_top_reveals_its_root_without_scrolling_a_visible_outer_host()
    {
        Show(probe =>
        {
            probe.Mode = NavigationViewPaneDisplayMode.Left; probe.Width = 280; probe.OuterScroll = true;
            for (var index = 0; index < 12; index++) probe.Items.Add(new("extra-" + index, "Additional section " + index));
        });
        Probe.Selected = Probe.Items[^1]; Probe.Refresh(); Frame();
        var outer = h.One("NavigationProbeScroller");
        Assert.True(h.Scene.ScrollTo(outer, null, 80, immediate: true)); Frame();
        h.Focus(TreeItem("Additional section 11"));
        SetMode(NavigationViewPaneDisplayMode.Top);
        var target = RootItem("Additional section 11");
        Assert.Same(target, h.Focused);
        var visible = h.Bounds(h.One("NavigationViewRootScroller")); var bounds = h.Bounds(target);
        Assert.True(bounds.X >= visible.X - 1 && bounds.Right <= visible.Right + 1, $"Target {bounds} must fit {visible}.");
        Assert.Equal(80, h.Scene.GetScrollState(outer)!.OffsetY);
    }

    [Fact]
    public void Application_header_belongs_to_content_below_top_pane_and_keeps_editor_identity()
    {
        Show(probe => probe.Mode = NavigationViewPaneDisplayMode.Top);
        var editor = Input("header-editor"); var main = Input("main-editor");
        var header = h.One("NavigationViewHeader");
        var pane = h.Bounds(h.One("SplitViewPane"));
        Assert.InRange(h.Bounds(header).Y - pane.Bottom, -.1, .1);
        Assert.InRange(h.Bounds(header).Height, 51.9, 52.1);
        Assert.True(h.Bounds(h.One("NavigationViewContent")).Y >= h.Bounds(header).Bottom);
        SetMode(NavigationViewPaneDisplayMode.Left);
        Assert.Same(header, h.One("NavigationViewHeader"));
        Assert.Same(editor, Input("header-editor")); Assert.Same(main, Input("main-editor"));
        Assert.InRange(h.Bounds(header).X - h.Bounds(h.One("NavigationViewContent")).X, -.1, .1);
        h.Focus(Input("pane-editor")); SetMode(NavigationViewPaneDisplayMode.Top);
        Assert.Same(Input("pane-editor"), h.Focused);
    }

    [Fact]
    public void Stable_keys_preserve_expansion_when_application_item_instances_are_replaced()
    {
        Show(); h.Click(TreeItem("Projects").Elements().Single(element => element.Name == "TreeViewExpander")); Frame();
        Probe.Items[1] = new("projects", "Renamed projects", new NavigationViewProbe.Item("active", "Active projects")); Probe.Refresh(); Frame();
        Assert.True(TreeItem("Renamed projects").Attributes.GetValueOrDefault("expanded") is true);
        Assert.NotNull(TreeItem("Active projects"));
    }

    [Fact]
    public void Application_clear_and_removed_current_items_normalize_silently_without_replacing_content()
    {
        Show(); h.Click(TreeItem("Home")); var main = Input("main-editor");
        Probe.Selected = null; Probe.Refresh(); Frame();
        Assert.Null(Probe.View.CurrentItem);
        Assert.DoesNotContain(h.All("TreeViewItem"), row => row.Attributes.GetValueOrDefault("selected") is true);
        h.Click(TreeItem("Home")); Probe.Items.RemoveAt(0); Probe.Refresh(); Frame();
        Assert.Null(Probe.View.CurrentItem);
        Assert.Equal(new[] { "home", "home" }, Probe.Invoked);
        Assert.Same(main, Input("main-editor"));
    }

    [Fact]
    public void Disabled_items_and_hidden_panes_cannot_invoke_routes_while_main_content_remains_active()
    {
        Show(probe => probe.Items[0].Disabled = true); h.Click(TreeItem("Home"));
        Assert.Empty(Probe.Invoked);
        var settings = TreeItem("Settings");
        Probe.Disabled = true; Probe.Refresh(); Frame();
        Assert.False(h.Input.Activate(settings));
        h.Focus(Input("main-editor")); Assert.Same(Input("main-editor"), h.Focused);
        Probe.Disabled = false; SetMode(NavigationViewPaneDisplayMode.Minimal);
        Assert.False(h.Input.Activate(settings));
        Assert.True(h.One("NavigationViewPane").Attributes.GetValueOrDefault("semantic-hidden") is true);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Duplicate_or_cyclic_navigation_keys_are_rejected_before_rendering(bool cycle)
    {
        Show();
        if (cycle) Probe.Items[1].Children.Add(Probe.Items[1]); else Probe.Items.Add(new("home", "Duplicate"));
        Assert.Throws<InvalidOperationException>(() => { Probe.Refresh(); h.Frame(); });
    }
}
