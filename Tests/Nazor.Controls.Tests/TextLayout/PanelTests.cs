using Nazor.Controls.Tests.TextLayout.Fixtures;
using Nazor.Graphics;
using Nazor.Style;
using Color = Nazor.Style.Color;

namespace Nazor.Controls.Tests.TextLayout;

[Collection("Controls")]
public sealed class PanelTests : IDisposable
{
    private readonly ControlHarness h = new(width: 800, height: 600);

    public void Dispose() => h.Dispose();

    private PanelProbe Probe() => h.Component<PanelProbe>();

    private void Refresh()
    {
        h.Host.Renderer.Dispatcher.InvokeAsync(Probe().Refresh);
        h.Frame();
    }

    private ElementNode Header(string item) => Nodes.Part(h.ByClass(item), "AccordionItemHeader");

    [Fact]
    public void An_accordion_in_single_mode_keeps_one_item_expanded()
    {
        h.Render<PanelProbe>();
        var accordion = h.ByClass("accordion");
        Assert.Equal("group", accordion.Attributes["role"]);
        Assert.Equal("single", accordion.Attributes["expand-mode"]);
        Assert.Equal(["AccordionItemHeader", "AccordionItemPanel"], h.ByClass("a1").Children.OfType<ElementNode>().Select(e => e.Name));
        h.Click(Header("a1"));
        Assert.Equal(["one"], Probe().Open);
        Assert.Single(h.Root.Elements(), e => e.Class == "body1");
        h.Click(Header("a3"));
        Assert.Equal(["three"], Probe().Open);
        Assert.DoesNotContain(h.Root.Elements(), e => e.Class == "body1");
        Assert.Equal(["one:True", "one:False", "three:True"], Probe().Reports);
        h.Click(Header("a3"));
        Assert.Empty(Probe().Open);
    }

    [Fact]
    public void An_accordion_in_multiple_mode_expands_items_independently_and_skips_disabled_ones()
    {
        h.Render<PanelProbe>();
        Probe().Mode = AccordionExpandMode.Multiple;
        Refresh();
        h.Click(Header("a1"));
        h.Click(Header("a3"));
        h.Click(Header("a2"));
        Assert.Equal(["one", "three"], Probe().Open);
        Assert.Equal(true, Header("a2").Attributes["disabled"]);
    }

    [Fact]
    public void An_accordion_that_requires_one_item_expands_the_first_and_keeps_the_last()
    {
        h.Render<PanelProbe>();
        Probe().RequireOne = true;
        Refresh();
        Assert.Equal(["one"], Probe().Open);
        h.Click(Header("a1"));
        Assert.Equal(["one"], Probe().Open);
        Assert.Equal(true, h.ByClass("a1").Attributes["expanded"]);
        h.Click(Header("a3"));
        Assert.Equal(["three"], Probe().Open);
    }

    [Fact]
    public void An_accordion_follows_values_from_the_parent()
    {
        h.Render<PanelProbe>();
        Probe().Open = ["three"];
        Refresh();
        Assert.Equal(true, h.ByClass("a3").Attributes["expanded"]);
        Assert.Equal(true, Header("a3").Attributes["expanded"]);
        Assert.Equal(false, h.ByClass("a1").Attributes["expanded"]);
        Probe().Open = ["one", "three"];
        Refresh();
        Assert.Equal(true, h.ByClass("a1").Attributes["expanded"]);
        Assert.Equal(false, h.ByClass("a3").Attributes["expanded"]);
    }

    [Fact]
    public void Arrow_keys_move_between_enabled_accordion_headers()
    {
        h.Render<PanelProbe>();
        h.Focus(Header("a1"));
        h.Press("ArrowDown");
        Assert.Equal("AccordionItemHeader", h.Focused?.Name);
        Assert.Equal("a3", h.Focused?.ParentElement?.Class);
        h.Press("ArrowDown");
        Assert.Equal("a1", h.Focused?.ParentElement?.Class);
        h.Press("ArrowUp");
        Assert.Equal("a3", h.Focused?.ParentElement?.Class);
        h.Press("Home");
        Assert.Equal("a1", h.Focused?.ParentElement?.Class);
        h.Press("End");
        Assert.Equal("a3", h.Focused?.ParentElement?.Class);
        Assert.Empty(Probe().Open);
    }

    [Fact]
    public void A_disabled_accordion_rejects_every_item()
    {
        h.Render<PanelProbe>();
        Probe().AccordionDisabled = true;
        Refresh();
        h.Click(Header("a1"));
        Assert.Empty(Probe().Open);
        Assert.Equal(false, Header("a1").Attributes["focusable"]);
    }

    [Fact]
    public void A_split_view_places_the_pane_beside_the_content_with_a_resize_handle()
    {
        h.Render<PanelProbe>();
        var split = h.ByClass("split");
        Assert.Equal(["SplitViewPane", "ResizeHandle", "SplitViewContent"], split.Children.OfType<ElementNode>().Select(e => e.Name));
        Assert.Equal("start", split.Attributes["placement"]);
        Assert.Equal("inline", split.Attributes["display-mode"]);
        var pane = h.Bounds(split.Children.OfType<ElementNode>().First());
        var content = h.Bounds(split.Children.OfType<ElementNode>().Last());
        Assert.Equal(200, pane.Width, 1);
        Assert.True(content.X >= pane.Right);
        Assert.Equal("group", split.Children.OfType<ElementNode>().First().Attributes["role"]);
        var handle = split.Children.OfType<ElementNode>().ElementAt(1);
        Assert.Equal("splitter", handle.Attributes["role"]);
        Assert.Equal("Resize pane", handle.Attributes["accessible-name"]);
        Assert.Equal("right", handle.Attributes["edge"]);
        Assert.Equal(200.0, handle.Attributes["range-value"]);
    }

    [Fact]
    public void The_resize_handle_resizes_the_pane_with_keys_and_the_pointer_within_the_limits()
    {
        h.Render<PanelProbe>();
        var handle = h.One("ResizeHandle");
        h.Focus(handle);
        h.Press("ArrowRight");
        Assert.Equal(210, Probe().Size, 1);
        h.Press("End");
        Assert.Equal(300, Probe().Size, 1);
        h.Press("ArrowRight");
        Assert.Equal(300, Probe().Size, 1);
        h.Press("Home");
        Assert.Equal(100, Probe().Size, 1);
        h.Frame();
        var bounds = h.Bounds(h.One("ResizeHandle"));
        var start = new Point(bounds.X + bounds.Width / 2, bounds.Y + bounds.Height / 2);
        h.Input.PointerMoved(start, Input.KeyModifiers.None);
        h.Input.PointerPressed(start, Input.PointerButton.Primary, Input.KeyModifiers.None);
        h.Input.PointerMoved(new Point(start.X + 50, start.Y), Input.KeyModifiers.None);
        h.Input.PointerReleased(new Point(start.X + 50, start.Y), Input.PointerButton.Primary, Input.KeyModifiers.None);
        h.Frame();
        Assert.Equal(150, Probe().Size, 1);
        Assert.Equal(150, h.Bounds(h.One("SplitViewPane")).Width, 1);
    }

    [Fact]
    public void A_pane_at_the_end_or_in_a_vertical_split_view_moves_the_handle_edge()
    {
        h.Render<PanelProbe>();
        Probe().Placement = SplitViewPanePlacement.End;
        Refresh();
        var split = h.ByClass("split");
        Assert.Equal(["SplitViewContent", "ResizeHandle", "SplitViewPane"], split.Children.OfType<ElementNode>().Select(e => e.Name));
        Assert.Equal("left", h.One("ResizeHandle").Attributes["edge"]);
        Assert.Equal(h.Bounds(split).Right, h.Bounds(h.One("SplitViewPane")).Right, 1);
        h.Focus(h.One("ResizeHandle"));
        h.Press("ArrowLeft");
        Assert.Equal(210, Probe().Size, 1);
        Probe().Orientation = Orientation.Vertical;
        Refresh();
        Assert.Equal("top", h.One("ResizeHandle").Attributes["edge"]);
        Assert.Equal(210, h.Bounds(h.One("SplitViewPane")).Height, 1);
    }

    [Fact]
    public void A_closed_inline_pane_disappears_and_an_overlay_pane_covers_the_content_until_dismissed()
    {
        h.Render<PanelProbe>();
        Probe().PaneOpen = false;
        Refresh();
        Assert.Equal(["SplitViewContent"], h.ByClass("split").Children.OfType<ElementNode>().Select(e => e.Name));
        Assert.Equal(false, h.ByClass("split").Attributes["pane-open"]);
        Probe().Mode2 = SplitViewDisplayMode.Overlay;
        Probe().PaneOpen = true;
        Refresh();
        var names = h.ByClass("split").Children.OfType<ElementNode>().Select(e => e.Name).ToArray();
        Assert.Equal(["SplitViewPane", "SplitViewContent", "SplitViewScrim"], names);
        Assert.Equal(h.Bounds(h.ByClass("split")).X, h.Bounds(h.One("SplitViewContent")).X, 1);
        Assert.Equal(Position.Absolute, h.Style(h.One("SplitViewPane")).Get(StyleProperties.Position));
        h.Click(h.One("SplitViewScrim"));
        Assert.False(Probe().PaneOpen);
        Assert.Equal(0, Probe().MainClicks);
        Probe().PaneOpen = true;
        Refresh();
        h.Focus(h.ByClass("pane-button"));
        h.Press("Escape");
        Assert.False(Probe().PaneOpen);
    }

    [Fact]
    public void A_compact_pane_keeps_a_narrow_strip_while_closed()
    {
        h.Render<PanelProbe>();
        Probe().Mode2 = SplitViewDisplayMode.Compact;
        Probe().PaneOpen = false;
        Refresh();
        var pane = h.One("SplitViewPane");
        Assert.Equal(true, pane.Attributes["compact"]);
        Assert.Equal(40, h.Bounds(pane).Width, 1);
        Assert.Equal(h.Bounds(h.ByClass("split")).X + 40, h.Bounds(h.One("SplitViewContent")).X, 1);
        h.Click(h.ByClass("main-button"));
        Assert.Equal(1, Probe().MainClicks);
        Probe().PaneOpen = true;
        Refresh();
        Assert.Equal(false, h.One("SplitViewPane").Attributes["compact"]);
        Assert.Equal(200, h.Bounds(h.One("SplitViewPane")).Width, 1);
        Assert.Equal(h.Bounds(h.ByClass("split")).X + 40, h.Bounds(h.One("SplitViewContent")).X, 1);
        Assert.Empty(h.All("ResizeHandle"));
        Assert.Single(h.All("SplitViewScrim"));
    }

    [Fact]
    public void A_split_view_rejects_inconsistent_limits()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => h.Render<BadSplitProbe>());
    }

    [Fact]
    public void The_panels_render_with_the_basic_theme()
    {
        h.Render<PanelProbe>();
        h.Click(Header("a1"));
        Assert.True(h.Image().Width > 0);
        Assert.NotEqual(Color.Transparent, h.Style(Header("a1")).BackgroundColor);
        Assert.Equal(0, h.Style(h.ByClass("a3")).Get(StyleProperties.BorderTopWidth).Value, 1);
    }
}
