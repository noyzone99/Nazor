using Nazor.Controls.Tests.Navigation.Fixtures;
using Nazor.Input;
using Nazor.Style;

namespace Nazor.Controls.Tests.Navigation;

[Collection("Controls")]
public sealed class TabsTests : IDisposable
{
    private readonly ControlHarness h = new();

    public void Dispose() => h.Dispose();

    private TabsProbe Render(Action<TabsProbe>? setup = null)
    {
        h.Render<TabsProbe>();
        var probe = h.Component<TabsProbe>();
        if (setup is not null)
        {
            setup(probe);
            probe.Refresh();
            h.Frame();
        }
        return probe;
    }

    [Fact]
    public void Tabs_render_a_tablist_and_the_selected_panel_labeled_by_its_tab()
    {
        Render();
        var list = h.One("TabsList");
        Assert.Equal("tablist", list.Attributes["role"]);
        var tabs = h.All("Tab").ToList();
        Assert.Equal(4, tabs.Count);
        Assert.All(tabs, tab => Assert.Equal("tab", tab.Attributes["role"]));
        Assert.Equal(1, tabs[0].Attributes["position-in-set"]);
        Assert.Equal(4, tabs[0].Attributes["size-of-set"]);
        Assert.Equal(true, tabs[0].Attributes["selected"]);
        Assert.Equal(0, tabs[0].Attributes["tabindex"]);
        Assert.Equal(-1, tabs[1].Attributes["tabindex"]);
        Assert.Equal(true, tabs[2].Attributes["disabled"]);
        var panel = h.One("TabsPanel");
        Assert.Equal("tabpanel", panel.Attributes["role"]);
        Assert.Equal(tabs[0].Attributes["id"], panel.Attributes["labeled-by"]);
        Assert.Single(h.All("TabBadge"));
        Assert.Contains(h.Root.Elements(), e => e.Class == "p-general");
        Assert.DoesNotContain(h.Root.Elements(), e => e.Class == "p-colors");
        Assert.Equal("top", h.One("Tabs").Attributes["placement"]);
        Assert.Equal("horizontal", h.One("Tabs").Attributes["orientation"]);
    }

    [Fact]
    public void Clicking_a_tab_selects_it_and_updates_both_bindings()
    {
        var probe = Render();
        h.Click(h.ByClass("t-colors"));
        Assert.Equal(1, probe.Index);
        Assert.Equal("colors", probe.Key);
        Assert.Contains(h.Root.Elements(), e => e.Class == "p-colors");
        Assert.DoesNotContain(h.Root.Elements(), e => e.Class == "p-general");
        h.Click(h.ByClass("t-disabled"));
        Assert.Equal(1, probe.Index);
    }

    [Fact]
    public void Arrow_keys_move_between_enabled_tabs_and_select_them()
    {
        var probe = Render();
        h.Focus(h.ByClass("t-general"));
        h.Press("ArrowRight");
        Assert.Equal("colors", probe.Key);
        h.Press("ArrowRight");
        Assert.Equal("files", probe.Key);
        Assert.Equal("t-files", h.Focused?.Class);
        h.Press("Home");
        Assert.Equal("general", probe.Key);
        h.Press("ArrowLeft");
        Assert.Equal("files", probe.Key);
    }

    [Fact]
    public void Control_tab_and_page_keys_cycle_from_inside_the_panel_skipping_disabled_tabs()
    {
        var probe = Render(p => p.Extra = ["files", "more"]);
        h.Focus(h.ByClass("t-general"));
        h.Press("Tab", KeyModifiers.Control);
        Assert.Equal(1, probe.Index);
        Assert.Equal("t-colors", h.Focused?.Class);
        h.Press("PageDown", KeyModifiers.Control);
        Assert.Equal(3, probe.Index);
        h.Press("PageUp", KeyModifiers.Control);
        Assert.Equal(1, probe.Index);
        h.Press("Tab", KeyModifiers.Control | KeyModifiers.Shift);
        Assert.Equal(0, probe.Index);
        h.Press("Tab", KeyModifiers.Control | KeyModifiers.Shift);
        Assert.Equal(4, probe.Index);
    }

    [Fact]
    public void Setting_the_key_or_the_index_from_the_owner_selects_the_tab()
    {
        var probe = Render(p => p.Key = "files");
        Assert.Equal(true, h.ByClass("t-files").Attributes["selected"]);
        probe.Key = null;
        probe.Index = 1;
        probe.Refresh();
        h.Frame();
        Assert.Equal(true, h.ByClass("t-colors").Attributes["selected"]);
    }

    [Fact]
    public void A_disabled_or_missing_selection_falls_back_to_the_first_enabled_tab()
    {
        Render(p => p.Index = 2);
        Assert.Equal(true, h.ByClass("t-general").Attributes["selected"]);
        var probe = h.Component<TabsProbe>();
        probe.Index = 42;
        probe.Refresh();
        h.Frame();
        Assert.Equal(true, h.ByClass("t-general").Attributes["selected"]);
    }

    [Fact]
    public void Closable_tabs_raise_close_from_their_close_part_and_from_delete()
    {
        var probe = Render();
        var close = h.ByClass("t-files").Elements().Single(e => e.Name == "TabClose");
        Assert.Equal("Close tab", close.Attributes["accessible-name"]);
        h.Click(close);
        Assert.Equal(["files"], probe.Closed);
        Assert.Equal(0, probe.Index);
        h.Click(h.ByClass("t-files"));
        h.Press("Delete");
        Assert.Equal(["files", "files"], probe.Closed);
        h.Focus(h.ByClass("t-general"));
        h.Press("Delete");
        Assert.Equal(2, probe.Closed.Count);
    }

    [Fact]
    public void Panels_render_lazily_and_stay_mounted_hidden_with_keep_mounted()
    {
        var probe = Render(p => p.KeepMounted = true);
        Assert.Single(h.All("TabsPanel"));
        h.Click(h.ByClass("t-colors"));
        var panels = h.All("TabsPanel").ToList();
        Assert.Equal(2, panels.Count);
        Assert.Equal(false, panels[0].Attributes["selected"]);
        Assert.Equal(Display.None, h.Style(panels[0]).Display);
        Assert.NotNull(h.ByClass("p-general"));
        probe.KeepMounted = false;
        probe.Refresh();
        h.Frame();
        Assert.Single(h.All("TabsPanel"));
    }

    [Fact]
    public void The_indicator_follows_the_selected_tab()
    {
        Render();
        var indicator = h.One("TabsIndicator");
        Assert.Equal(true, indicator.Attributes["semantic-hidden"]);
        var tab = h.Bounds(h.ByClass("t-general"));
        Assert.Equal(tab.X, h.Bounds(indicator).X, 1);
        Assert.Equal(tab.Width, h.Bounds(indicator).Width, 1);
        Assert.Equal(tab.Bottom, h.Bounds(indicator).Bottom, 1);
        h.Click(h.ByClass("t-colors"));
        h.Frame();
        var colors = h.Bounds(h.ByClass("t-colors"));
        Assert.Equal(colors.X, h.Bounds(h.One("TabsIndicator")).X, 1);
    }

    [Fact]
    public void Start_placement_lays_the_tabs_out_vertically_before_the_panel()
    {
        Render(p => p.Placement = TabsPlacement.Start);
        Assert.Equal("vertical", h.One("TabsList").Attributes["orientation"]);
        var general = h.Bounds(h.ByClass("t-general"));
        var colors = h.Bounds(h.ByClass("t-colors"));
        Assert.True(colors.Y >= general.Bottom - 0.5);
        Assert.True(h.Bounds(h.One("TabsPanel")).X >= h.Bounds(h.One("TabsHeader")).Right - 0.5);
        h.Focus(h.ByClass("t-general"));
        h.Press("ArrowDown");
        Assert.Equal("colors", h.Component<TabsProbe>().Key);
    }

    [Fact]
    public void Bottom_placement_puts_the_header_after_the_panel()
    {
        Render(p => p.Placement = TabsPlacement.Bottom);
        Assert.True(h.Bounds(h.One("TabsHeader")).Y >= h.Bounds(h.One("TabsPanel")).Bottom - 0.5);
    }

    [Fact]
    public void A_narrow_scrolling_list_shows_scroll_buttons_and_scrolls()
    {
        Render(p =>
        {
            p.Width = 200;
            p.Extra = ["alpha", "bravo", "charlie", "delta"];
        });
        h.Frame();
        Assert.Equal(true, h.One("TabsHeader").Attributes["overflowing"]);
        var back = h.One("TabsScrollBack");
        Assert.Equal(true, back.Attributes["disabled"]);
        h.Click(h.One("TabsScrollForward"));
        h.Frame();
        Assert.Equal(false, h.One("TabsScrollBack").Attributes["disabled"]);
    }

    [Fact]
    public void Selecting_a_tab_out_of_view_scrolls_it_into_view()
    {
        var probe = Render(p =>
        {
            p.Width = 200;
            p.Extra = ["alpha", "bravo", "charlie", "delta"];
        });
        h.Frame();
        probe.Key = "delta";
        probe.Refresh();
        h.Frame();
        h.Frame();
        var tab = h.Bounds(h.ByClass("t-delta"));
        var scroller = h.Bounds(h.One("TabsScroller"));
        Assert.True(tab.Right <= scroller.Right + 0.5, $"{tab} {scroller}");
    }

    [Fact]
    public void The_menu_overflow_moves_hidden_tabs_into_a_more_menu_and_keeps_the_selected_one()
    {
        var probe = Render(p =>
        {
            p.Width = 260;
            p.Overflow = TabsOverflow.Menu;
            p.Extra = ["alpha", "bravo", "charlie", "delta"];
        });
        h.Frame();
        var visible = h.All("Tab").Count();
        Assert.True(visible < 7);
        var more = h.One("TabsOverflow");
        Assert.Equal("More options", more.Attributes["accessible-name"]);
        h.Click(more);
        Assert.Single(h.All("Menu"));
        var items = h.All("MenuItem").ToList();
        Assert.Equal(7 - visible, items.Count);
        h.Click(items[^1]);
        Assert.Equal("delta", probe.Key);
        h.Frame();
        Assert.Contains(h.All("Tab"), tab => tab.Class == "t-delta");
        Assert.Empty(h.All("Menu"));
        var indicator = h.Bounds(h.One("TabsIndicator"));
        Assert.Equal(h.Bounds(h.ByClass("t-delta")).X, indicator.X, 1);
    }

    [Fact]
    public void The_selected_tab_uses_the_selection_colors()
    {
        Render();
        var selected = h.Style(h.ByClass("t-general"));
        var other = h.Style(h.ByClass("t-colors"));
        Assert.NotEqual(selected.Get(Nazor.Style.StyleProperties.BackgroundColor), other.Get(Nazor.Style.StyleProperties.BackgroundColor));
    }

    [Fact]
    public void Invalid_parameters_are_rejected()
    {
        Assert.ThrowsAny<Exception>(() => Render(p => p.Placement = (TabsPlacement)42));
    }
}
