using Nazor.Controls.Primitives;
using Nazor.Controls.Tests.TextLayout.Fixtures;
using Nazor.Graphics;
using Nazor.Input;
using Nazor.Style;
using Color = Nazor.Style.Color;

namespace Nazor.Controls.Tests.TextLayout;

[Collection("Controls")]
public sealed class LayoutTests : IDisposable
{
    private readonly ControlHarness h = new(width: 640, height: 1400);

    public void Dispose() => h.Dispose();

    private LayoutProbe Probe() => h.Component<LayoutProbe>();

    private void Refresh()
    {
        h.Host.Renderer.Dispatcher.InvokeAsync(Probe().Refresh);
        h.Frame();
    }

    private void Run(Func<Task> work)
    {
        h.Host.Renderer.Dispatcher.InvokeAsync(work);
        h.Frame();
    }

    [Fact]
    public void A_scroll_view_reports_which_edges_hide_content()
    {
        h.Render<LayoutProbe>();
        var view = h.ByClass("scroller");
        Assert.Equal("group", view.Attributes["role"]);
        Assert.Equal("vertical", view.Attributes["orientation"]);
        Assert.Equal(false, view.Attributes["can-scroll-top"]);
        Assert.Equal(true, view.Attributes["can-scroll-bottom"]);
        Assert.Equal(true, view.Attributes["can-scroll-end"]);
        Assert.Equal(false, view.Attributes["can-scroll-start"]);
        Assert.Equal(false, view.Attributes["can-scroll-right"]);
        Assert.True(Probe().Scrolls > 0);
        Run(async () => await Probe().Scroller.ScrollToAsync(null, 100));
        Assert.Equal(true, view.Attributes["can-scroll-top"]);
        Assert.Equal(true, view.Attributes["can-scroll-bottom"]);
        Assert.Equal(100, Probe().Scroller.ScrollTop, 1);
        Run(async () => await Probe().Scroller.ScrollToEndAsync());
        Assert.Equal(false, view.Attributes["can-scroll-bottom"]);
        Assert.Equal(true, view.Attributes["can-scroll-start"]);
        Assert.Equal(Probe().Scroller.ContentHeight - Probe().Scroller.ViewportHeight, Probe().Scroller.ScrollTop, 1);
        Run(async () => await Probe().Scroller.ScrollToStartAsync());
        Assert.Equal(0, Probe().Scroller.ScrollTop, 1);
        Assert.Equal(false, view.Attributes["can-scroll-top"]);
    }

    [Fact]
    public void A_scroll_view_brings_a_child_into_view()
    {
        h.Render<LayoutProbe>();
        var target = h.ByClass("line20");
        var moved = false;
        h.Host.Renderer.Dispatcher.InvokeAsync(() => moved = Probe().Scroller.ScrollIntoView(new ElementReference(target)));
        h.Frame();
        Assert.True(moved);
        var view = h.Bounds(h.ByClass("scroller"));
        var line = h.Bounds(target);
        Assert.True(line.Y >= view.Y - 0.5 && line.Bottom <= view.Bottom + 0.5);
        Assert.Equal(true, h.ByClass("scroller").Attributes["can-scroll-top"]);
    }

    [Fact]
    public void A_scroll_view_without_overflow_has_no_edges_and_a_short_one_follows_content_changes()
    {
        h.Render<LayoutProbe>();
        Probe().Lines = 2;
        Refresh();
        var view = h.ByClass("scroller");
        Assert.Equal(false, view.Attributes["can-scroll-bottom"]);
        Assert.Equal(false, view.Attributes["can-scroll-end"]);
        Probe().Lines = 40;
        Refresh();
        Assert.Equal(true, view.Attributes["can-scroll-bottom"]);
    }

    [Fact]
    public void A_horizontal_scroll_view_reports_left_and_right_and_keyboard_scrolls_a_focusable_view()
    {
        h.Render<LayoutProbe>();
        var wide = h.ByClass("wide");
        Assert.Equal("horizontal", wide.Attributes["orientation"]);
        Assert.False(wide.Attributes.ContainsKey("role") && wide.Attributes["role"] is not null);
        Assert.Equal(true, wide.Attributes["can-scroll-right"]);
        Assert.Equal(true, wide.Attributes["can-scroll-end"]);
        Assert.Equal(false, wide.Attributes["can-scroll-bottom"]);
        Probe().Focusable = true;
        Refresh();
        var view = h.ByClass("scroller");
        Assert.Equal(0, view.Attributes["tabindex"]);
        h.Focus(view);
        h.Press("End");
        Assert.Equal(false, view.Attributes["can-scroll-bottom"]);
        h.Press("Home");
        Assert.Equal(false, view.Attributes["can-scroll-top"]);
    }

    [Fact]
    public void The_basic_theme_marks_the_edges_of_a_scroll_view_that_hide_content()
    {
        h.Render<LayoutProbe>();
        var style = h.Style(h.ByClass("scroller"));
        Assert.Equal(Color.Transparent, style.Get(StyleProperties.BorderTopColor));
        Assert.NotEqual(Color.Transparent, style.Get(StyleProperties.BorderBottomColor));
    }

    [Fact]
    public void A_card_shows_its_parts_and_is_named_and_described_by_its_header()
    {
        h.Render<LayoutProbe>();
        var card = h.ByClass("card");
        Assert.Equal(["CardHeader", "CardBody", "CardFooter"], card.Children.OfType<ElementNode>().Select(e => e.Name));
        var header = Nodes.Part(card, "CardHeader");
        Assert.Equal(["CardMedia", "CardTitle", "CardSubtitle", "CardActions"], header.Children.OfType<ElementNode>().Select(e => e.Name));
        Assert.Equal(Nodes.Part(card, "CardTitle").Attributes["id"], card.Attributes["labeled-by"]);
        Assert.Equal(Nodes.Part(card, "CardSubtitle").Attributes["id"], card.Attributes["described-by"]);
        Assert.Equal("standard", card.Attributes["appearance"]);
        Assert.Equal("vertical", card.Attributes["orientation"]);
        var plain = h.ByClass("plain-card");
        Assert.Equal("group", plain.Attributes["role"]);
        Assert.Equal(["CardBody"], plain.Children.OfType<ElementNode>().Select(e => e.Name));
        Assert.Equal(false, plain.Attributes["clickable"]);
        Assert.Equal(false, plain.Attributes["focusable"]);
    }

    [Fact]
    public void A_clickable_card_is_a_button_while_its_own_buttons_keep_their_actions()
    {
        h.Render<LayoutProbe>();
        var card = h.ByClass("card");
        Assert.Equal("button", card.Attributes["role"]);
        Assert.Equal(true, card.Attributes["clickable"]);
        h.Click(h.ByClass("card-text"));
        Assert.Equal(1, Probe().CardClicks);
        h.Click(h.ByClass("card-action"));
        Assert.Equal(1, Probe().ActionClicks);
        Assert.Equal(1, Probe().CardClicks);
        h.Focus(card);
        h.Press("Enter");
        h.Press(" ");
        Assert.Equal(3, Probe().CardClicks);
    }

    [Fact]
    public void A_card_shows_selection_appearance_and_rejects_clicks_when_disabled()
    {
        h.Render<LayoutProbe>();
        var card = h.ByClass("card");
        var standard = h.Style(card).BackgroundColor;
        Probe().CardAppearance = CardAppearance.Outline;
        Probe().Selected = true;
        Refresh();
        Assert.Equal("outline", card.Attributes["appearance"]);
        Assert.Equal(true, card.Attributes["selected"]);
        Assert.Equal(SystemColors.Resolve().Highlight, h.Style(card).BackgroundColor);
        Probe().Selected = false;
        Probe().CardAppearance = CardAppearance.Subtle;
        Refresh();
        Assert.Equal(Color.Transparent, h.Style(card).BackgroundColor);
        Assert.NotEqual(standard, h.Style(card).BackgroundColor);
        Probe().CardDisabled = true;
        Refresh();
        h.Click(h.ByClass("card-text"));
        Assert.Equal(0, Probe().CardClicks);
        Assert.Equal(false, card.Attributes["focusable"]);
    }

    [Fact]
    public void A_group_box_is_a_group_named_by_its_header_and_disables_its_content()
    {
        h.Render<LayoutProbe>();
        var group = h.ByClass("group");
        Assert.Equal("group", group.Attributes["role"]);
        var header = Nodes.Part(group, "GroupBoxHeader");
        Assert.Equal(header.Attributes["id"], group.Attributes["labeled-by"]);
        Assert.Equal("Proxy", Nodes.Text(header));
        Assert.Equal("GroupBoxTitle", header.Children.OfType<ElementNode>().Single().Name);
        h.Click(h.ByClass("group-button"));
        Assert.Equal(1, Probe().GroupClicks);
        Probe().GroupDisabled = true;
        Refresh();
        Assert.True(h.ByClass("group-button").IsDisabled);
        h.Click(h.ByClass("group-button"));
        Assert.Equal(1, Probe().GroupClicks);
    }

    [Fact]
    public void A_checkable_group_box_enables_its_content_only_while_checked()
    {
        h.Render<LayoutProbe>();
        var group = h.ByClass("checkable");
        var check = Nodes.Part(group, "CheckBox");
        Assert.Equal(false, check.Attributes["checked"]);
        Assert.Equal(true, group.Attributes["checkable"]);
        Assert.True(h.ByClass("proxy-button").IsDisabled);
        h.Click(h.ByClass("proxy-button"));
        Assert.Equal(0, Probe().ProxyClicks);
        h.Click(check);
        Assert.True(Probe().ProxyOn);
        Assert.Equal(true, group.Attributes["checked"]);
        Assert.False(h.ByClass("proxy-button").IsDisabled);
        h.Click(h.ByClass("proxy-button"));
        Assert.Equal(1, Probe().ProxyClicks);
        Assert.Equal("Use a proxy", Nodes.Text(Nodes.Part(group, "GroupBoxHeader")));
    }

    [Fact]
    public void A_collapsible_group_box_shows_and_hides_its_content_from_the_header_button()
    {
        h.Render<LayoutProbe>();
        var group = h.ByClass("collapsible");
        var toggle = Nodes.Part(group, "GroupBoxToggle");
        Assert.Equal("button", toggle.Attributes["role"]);
        Assert.Equal(false, toggle.Attributes["expanded"]);
        Assert.Equal(Nodes.Part(group, "GroupBoxPanel").Attributes["id"], toggle.Attributes["controls"]);
        Assert.DoesNotContain(h.Root.Elements(), e => e.Class == "advanced-text");
        h.Click(toggle);
        Assert.True(Probe().AdvancedOpen);
        Assert.Single(h.Root.Elements(), e => e.Class == "advanced-text");
        Assert.Equal("chevron-down", Nodes.Part(group, "GroupBoxChevron").Attributes["glyph"]);
        h.Focus(Nodes.Part(group, "GroupBoxToggle"));
        h.Press("Enter");
        Assert.False(Probe().AdvancedOpen);
        Assert.Equal("chevron-right", Nodes.Part(group, "GroupBoxChevron").Attributes["glyph"]);
    }

    [Fact]
    public void A_checkable_and_collapsible_group_box_has_a_named_toggle_and_a_check_box()
    {
        h.Render<LayoutProbe>();
        var header = Nodes.Part(h.ByClass("both"), "GroupBoxHeader");
        Assert.Equal(["GroupBoxToggle", "CheckBox"], header.Children.OfType<ElementNode>().Select(e => e.Name));
        Assert.Equal("Collapse", header.Children.OfType<ElementNode>().First().Attributes["accessible-name"]);
        Assert.Equal(true, Nodes.Part(h.ByClass("both"), "CheckBox").Attributes["checked"]);
    }

    [Fact]
    public void An_expander_toggles_from_its_header_and_reports_the_change()
    {
        h.Render<LayoutProbe>();
        var expander = h.ByClass("expander");
        var header = Nodes.Part(expander, "ExpanderHeader");
        Assert.Equal("button", header.Attributes["role"]);
        Assert.Equal(false, header.Attributes["expanded"]);
        Assert.Equal(["ExpanderLeading", "ExpanderHeaderText", "ExpanderTrailing", "ExpanderChevron"], header.Children.OfType<ElementNode>().Select(e => e.Name));
        Assert.Equal("More information", Nodes.Text(Nodes.Part(expander, "ExpanderSubtitle")));
        Assert.Equal("chevron-down", Nodes.Part(expander, "ExpanderChevron").Attributes["glyph"]);
        var panel = Nodes.Part(expander, "ExpanderPanel");
        Assert.Equal(panel.Attributes["id"], header.Attributes["controls"]);
        Assert.Equal(header.Attributes["id"], panel.Attributes["labeled-by"]);
        Assert.Equal("group", panel.Attributes["role"]);
        h.Click(Nodes.Part(expander, "ExpanderTitle"));
        Assert.True(Probe().Expanded);
        Assert.Equal(true, expander.Attributes["expanded"]);
        Assert.Single(h.Root.Elements(), e => e.Class == "details");
        Assert.Equal("chevron-down", Nodes.Part(expander, "ExpanderChevron").Attributes["glyph"]);
        h.Focus(Nodes.Part(expander, "ExpanderHeader"));
        h.Press(" ");
        Assert.False(Probe().Expanded);
        Assert.DoesNotContain(h.Root.Elements(), e => e.Class == "details");
    }

    [Fact]
    public void Controls_in_the_trailing_slot_do_not_toggle_the_expander_and_a_disabled_one_rejects_input()
    {
        h.Render<LayoutProbe>();
        h.Click(h.ByClass("trail"));
        Assert.Equal(1, Probe().TrailClicks);
        Assert.False(Probe().Expanded);
        Probe().Expanded = true;
        Refresh();
        Assert.Single(h.Root.Elements(), e => e.Class == "details");
        Probe().ExpanderDisabled = true;
        Refresh();
        var expander = h.ByClass("expander");
        Assert.Equal(true, expander.Attributes["disabled"]);
        Assert.Equal(false, Nodes.Part(expander, "ExpanderHeader").Attributes["focusable"]);
        h.Click(Nodes.Part(expander, "ExpanderTitle"));
        Assert.True(Probe().Expanded);
    }

    [Fact]
    public void A_status_bar_is_a_named_group_unless_live_and_sections_size_themselves()
    {
        h.Render<LayoutProbe>();
        var bar = h.ByClass("status");
        Assert.Equal("group", bar.Attributes["role"]);
        Assert.Equal("Status bar", bar.Attributes["accessible-name"]);
        Assert.False(bar.Attributes.TryGetValue("live", out var live) && live is not null);
        Assert.Equal("StatusBarSizeGrip", bar.Children.OfType<ElementNode>().Last().Name);
        Assert.Equal("text", h.ByClass("s-ready").Attributes["role"]);
        Assert.Equal("group", h.ByClass("s-named").Attributes["role"]);
        Assert.Equal(true, h.ByClass("s-ready").Attributes["flexible"]);
        Assert.Equal("end", h.ByClass("s-pos").Attributes["align"]);
        Assert.Equal(120, h.Bounds(h.ByClass("s-pos")).Width, 1);
        var total = h.Bounds(bar).Width;
        Assert.True(h.Bounds(h.ByClass("s-ready")).Width > total / 2);
        Probe().Live = LivePoliteness.Polite;
        Refresh();
        Assert.Equal("status", bar.Attributes["role"]);
        Assert.Equal("polite", bar.Attributes["live"]);
    }

    [Fact]
    public void The_layout_surfaces_render_with_the_basic_theme()
    {
        h.Render<LayoutProbe>();
        var image = h.Image();
        Assert.True(image.Width > 0);
        Assert.Equal(1, h.Style(h.ByClass("group")).Get(StyleProperties.BorderTopWidth).Value, 1);
        Assert.NotEqual(Color.Transparent, h.Style(h.ByClass("status")).BackgroundColor);
        var grip = h.Bounds(h.ByClass("status").Children.OfType<ElementNode>().Last());
        Assert.Equal(12, grip.Width, 1);
    }
}
