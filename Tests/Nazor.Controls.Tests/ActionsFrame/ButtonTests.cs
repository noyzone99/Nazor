using Nazor.Controls.Tests.ActionsFrame.Fixtures;
using Nazor.Input;
using Nazor.Style;

namespace Nazor.Controls.Tests.ActionsFrame;

[Collection("Controls")]
public sealed class ButtonGroupTests : IDisposable
{
    private readonly ControlHarness h = new();

    public void Dispose() => h.Dispose();

    [Fact]
    public void Members_get_their_position_in_order()
    {
        h.Render<ButtonGroupProbe>();
        Assert.Equal("group", h.ByClass("row").Attributes["role"]);
        Assert.Equal("horizontal", h.ByClass("row").Attributes["orientation"]);
        Assert.Equal("first", h.ByClass("left").Attributes["position"]);
        Assert.Equal("middle", h.ByClass("center").Attributes["position"]);
        Assert.Equal("last", h.ByClass("right").Attributes["position"]);
        Assert.Equal("only", h.ByClass("alone").Attributes["position"]);
        Assert.False(h.ByClass("outside").Attributes.ContainsKey("position") && h.ByClass("outside").Attributes["position"] is not null);
    }

    [Fact]
    public void Positions_follow_buttons_added_and_removed()
    {
        h.Render<ButtonGroupProbe>();
        var probe = h.Component<ButtonGroupProbe>();
        probe.ShowMiddle = false;
        probe.Refresh();
        h.Frame();
        Assert.Equal("first", h.ByClass("left").Attributes["position"]);
        Assert.Equal("last", h.ByClass("right").Attributes["position"]);
        probe.ShowMiddle = true;
        probe.Refresh();
        h.Frame();
        Assert.Equal("middle", h.ByClass("center").Attributes["position"]);
        Assert.Equal("last", h.ByClass("right").Attributes["position"]);
    }

    [Fact]
    public void Members_inherit_the_appearance_and_size_they_leave_at_default()
    {
        h.Render<ButtonGroupProbe>();
        var inherits = h.ByClass("inherits");
        Assert.Equal("accent", inherits.Attributes["appearance"]);
        Assert.Equal("small", inherits.Attributes["size"]);
        Assert.Equal("first", inherits.Attributes["position"]);
        var own = h.ByClass("own");
        Assert.Equal("outline", own.Attributes["appearance"]);
        Assert.Equal("large", own.Attributes["size"]);
        Assert.Equal("vertical", h.ByClass("column").Attributes["orientation"]);
        Assert.Equal("standard", h.ByClass("left").Attributes["appearance"]);
    }

    [Fact]
    public void Menu_and_split_buttons_join_a_group()
    {
        h.Render<ButtonGroupProbe>();
        Assert.Equal("first", h.ByClass("mixed-button").Attributes["position"]);
        Assert.Equal("middle", h.ByClass("mixed-menu").Attributes["position"]);
        Assert.Equal("last", h.ByClass("mixed-split").Attributes["position"]);
    }

    [Fact]
    public void A_disabled_group_disables_its_members()
    {
        h.Render<ButtonGroupProbe>();
        var probe = h.Component<ButtonGroupProbe>();
        probe.GroupDisabled = true;
        probe.Refresh();
        h.Frame();
        Assert.True(h.ByClass("inherits").IsDisabled);
        h.Click(h.ByClass("left"));
        Assert.Equal(1, probe.Clicks);
    }

    [Fact]
    public void The_basic_theme_joins_the_borders_of_the_members()
    {
        h.Render<ButtonGroupProbe>();
        var left = h.Bounds(h.ByClass("left"));
        var center = h.Bounds(h.ByClass("center"));
        Assert.Equal(left.Right - 1, center.X, 1);
        Assert.Equal(left.Y, center.Y, 1);
        var top = h.Bounds(h.ByClass("inherits"));
        var bottom = h.Bounds(h.ByClass("own"));
        Assert.Equal(top.Bottom - 1, bottom.Y, 1);
    }
}

[Collection("Controls")]
public sealed class SplitButtonTests : IDisposable
{
    private readonly ControlHarness h = new();

    public void Dispose() => h.Dispose();

    private ElementNode Part(string owner, string name) => h.ByClass(owner).Elements().Single(e => e.Name == name);

    [Fact]
    public void It_renders_its_parts_and_vocabulary()
    {
        h.Render<SplitButtonProbe>();
        var paste = h.ByClass("paste");
        Assert.Equal("splitbutton", paste.Attributes["role"]);
        Assert.Equal(true, paste.Attributes["focusable"]);
        Assert.Equal(false, paste.Attributes["expanded"]);
        Assert.Equal(["SplitButtonPrimary", "SplitButtonDivider", "SplitButtonTrigger"], paste.Children.OfType<ElementNode>().Select(e => e.Name));
        Assert.Equal("Paste", Part("paste", "SplitButtonLabel").Text());
        Assert.Single(Part("paste", "SplitButtonPrimary").Elements(), e => e.Name == "SplitButtonLeading");
        Assert.Equal(true, Part("paste", "SplitButtonTrigger").Attributes["expander"]);
        Assert.Equal("chevron-down", Part("paste", "SplitButtonChevron").Attributes["glyph"]);
        Assert.Equal("More options", Part("paste", "SplitButtonTrigger").Attributes["tooltip"]);
        var color = h.ByClass("color");
        Assert.Equal("accent", color.Attributes["appearance"]);
        Assert.Equal("danger", color.Attributes["intent"]);
        Assert.Equal("large", color.Attributes["size"]);
        Assert.Equal("Highlight colors", Part("color", "SplitButtonTrigger").Attributes["tooltip"]);
    }

    [Fact]
    public void The_primary_part_runs_the_action_and_the_trigger_opens_the_menu()
    {
        h.Render<SplitButtonProbe>();
        var probe = h.Component<SplitButtonProbe>();
        h.Click(Part("paste", "SplitButtonPrimary"));
        Assert.Equal(["paste"], probe.Log);
        Assert.Empty(h.All("Menu"));
        h.Click(Part("paste", "SplitButtonTrigger"));
        Assert.True(probe.Open);
        Assert.Equal(true, h.ByClass("paste").Attributes["expanded"]);
        Assert.Equal("menu", h.One("Menu").Attributes["role"]);
        h.Click(h.ByClass("merge"));
        Assert.Equal(["paste", "merge"], probe.Log);
        Assert.False(probe.Open);
        Assert.Empty(h.All("Menu"));
        Assert.Equal(h.ByClass("paste"), h.Focused);
    }

    [Fact]
    public void Keyboard_runs_the_action_and_opens_the_menu()
    {
        h.Render<SplitButtonProbe>();
        var probe = h.Component<SplitButtonProbe>();
        h.Focus(h.ByClass("paste"));
        h.Press("Enter");
        h.Press(" ");
        Assert.Equal(["paste", "paste"], probe.Log);
        h.Press("ArrowDown", KeyModifiers.Alt);
        Assert.True(probe.Open);
        h.Press("Escape");
        Assert.Empty(h.All("Menu"));
        Assert.False(probe.Open);
        h.Focus(h.ByClass("paste"));
        h.Press("F4");
        Assert.True(probe.Open);
        Assert.Equal(h.ByClass("keep-text"), h.Focused);
    }

    [Fact]
    public void A_toggle_split_button_keeps_its_pressed_state()
    {
        h.Render<SplitButtonProbe>();
        var probe = h.Component<SplitButtonProbe>();
        Assert.Equal(false, h.ByClass("color").Attributes["pressed"]);
        Assert.Equal(true, h.ByClass("color").Attributes["toggle"]);
        h.Click(Part("color", "SplitButtonPrimary"));
        Assert.True(probe.Highlight);
        Assert.Equal(true, h.ByClass("color").Attributes["pressed"]);
        h.Click(Part("color", "SplitButtonTrigger"));
        Assert.True(probe.Highlight);
        h.Click(h.ByClass("yellow"));
        Assert.Contains("yellow", probe.Log);
        probe.Highlight = false;
        probe.Refresh();
        h.Frame();
        Assert.Equal(false, h.ByClass("color").Attributes["pressed"]);
    }

    [Fact]
    public void A_disabled_split_button_ignores_activation()
    {
        h.Render<SplitButtonProbe>();
        var probe = h.Component<SplitButtonProbe>();
        h.Click(Part("off", "SplitButtonPrimary"));
        h.Click(Part("off", "SplitButtonTrigger"));
        Assert.Empty(probe.Log);
        Assert.Empty(h.All("Menu"));
        Assert.True(h.ByClass("off").IsDisabled);
        Assert.Equal(false, h.ByClass("off").Attributes["focusable"]);
    }

    [Fact]
    public void A_command_supplies_the_label_tooltip_and_action()
    {
        h.Render<SplitButtonProbe>();
        var probe = h.Component<SplitButtonProbe>();
        Assert.Equal("Save", Part("command", "SplitButtonLabel").Text());
        Assert.Equal("Save", h.ByClass("command").Attributes["accessible-name"]);
        Assert.Contains("Ctrl+S", (string)h.ByClass("command").Attributes["tooltip"]!);
        h.Click(Part("command", "SplitButtonPrimary"));
        Assert.Equal(["save"], probe.Log);
        probe.SaveCommand.IsEnabled = false;
        h.Frame();
        Assert.True(h.ByClass("command").IsDisabled);
    }

    [Fact]
    public void The_basic_theme_highlights_the_open_trigger()
    {
        h.Render<SplitButtonProbe>();
        var closed = h.Style(Part("paste", "SplitButtonTrigger")).BackgroundColor;
        h.Click(Part("paste", "SplitButtonTrigger"));
        Assert.Equal(SystemColors.Resolve().Highlight, h.Style(Part("paste", "SplitButtonTrigger")).BackgroundColor);
        Assert.NotEqual(closed, h.Style(Part("paste", "SplitButtonTrigger")).BackgroundColor);
    }
}

[Collection("Controls")]
public sealed class MenuButtonTests : IDisposable
{
    private readonly ControlHarness h = new();

    public void Dispose() => h.Dispose();

    [Fact]
    public void It_looks_like_a_button_with_a_chevron()
    {
        h.Render<MenuButtonProbe>();
        var sort = h.ByClass("sort");
        Assert.Equal("button", sort.Attributes["role"]);
        Assert.Equal("outline", sort.Attributes["appearance"]);
        Assert.Equal("small", sort.Attributes["size"]);
        Assert.Equal(false, sort.Attributes["expanded"]);
        Assert.Equal(["ButtonLeading", "ButtonLabel", "MenuButtonChevron"], sort.Children.OfType<ElementNode>().Select(e => e.Name));
        Assert.Equal("chevron-down", sort.Elements().Single(e => e.Name == "MenuButtonChevron").Attributes["glyph"]);
        Assert.Equal("chevron-up", h.ByClass("up").Elements().Single(e => e.Name == "MenuButtonChevron").Attributes["glyph"]);
    }

    [Fact]
    public void A_click_opens_the_menu_and_choosing_an_item_closes_it()
    {
        h.Render<MenuButtonProbe>();
        var probe = h.Component<MenuButtonProbe>();
        h.Click(h.ByClass("sort"));
        Assert.True(probe.Open);
        Assert.Equal(1, probe.Clicks);
        Assert.Equal(true, h.ByClass("sort").Attributes["expanded"]);
        Assert.Equal("Sort by", h.One("Menu").Attributes["accessible-name"]);
        h.Click(h.ByClass("descending"));
        Assert.True(probe.Descending);
        Assert.True(probe.Open);
        h.Click(h.ByClass("by-date"));
        Assert.Equal("date", probe.Sort);
        Assert.False(probe.Open);
        Assert.Empty(h.All("Menu"));
        Assert.Equal(false, h.ByClass("sort").Attributes["expanded"]);
        Assert.Equal(h.ByClass("sort"), h.Focused);
    }

    [Fact]
    public void A_second_click_closes_the_menu()
    {
        h.Render<MenuButtonProbe>();
        var probe = h.Component<MenuButtonProbe>();
        h.Focus(h.ByClass("sort"));
        h.Press("Enter");
        Assert.True(probe.Open);
        probe.Open = false;
        probe.Refresh();
        h.Frame();
        Assert.Empty(h.All("Menu"));
    }

    [Fact]
    public void Arrow_down_alt_arrow_down_and_f4_open_the_menu()
    {
        h.Render<MenuButtonProbe>();
        var probe = h.Component<MenuButtonProbe>();
        foreach (var (key, modifiers) in new[] { ("ArrowDown", KeyModifiers.None), ("ArrowDown", KeyModifiers.Alt), ("F4", KeyModifiers.None) })
        {
            h.Focus(h.ByClass("sort"));
            h.Press(key, modifiers);
            Assert.True(probe.Open);
            Assert.Equal(h.ByClass("by-name"), h.Focused);
            h.Press("Escape");
            Assert.False(probe.Open);
            Assert.Equal(h.ByClass("sort"), h.Focused);
        }
    }

    [Fact]
    public void Arrow_down_moves_through_a_vertical_toolbar_instead_of_opening()
    {
        h.Render<MenuButtonProbe>();
        h.Focus(h.ByClass("in-vertical"));
        h.Press("ArrowDown");
        Assert.Equal(h.ByClass("after-vertical"), h.Focused);
        Assert.Empty(h.All("Menu"));
    }

    [Fact]
    public void Disabled_and_chevronless_variants()
    {
        h.Render<MenuButtonProbe>();
        var probe = h.Component<MenuButtonProbe>();
        h.Click(h.ByClass("off"));
        Assert.Empty(h.All("Menu"));
        Assert.True(h.ByClass("off").IsDisabled);
        probe.ShowChevron = false;
        probe.Refresh();
        h.Frame();
        Assert.DoesNotContain(h.ByClass("up").Elements(), e => e.Name == "MenuButtonChevron");
        h.Click(h.ByClass("up"));
        Assert.Equal("top-end", h.One("Menu").Attributes["placement"]);
    }

    [Fact]
    public void The_basic_theme_marks_the_open_button()
    {
        h.Render<MenuButtonProbe>();
        h.Click(h.ByClass("sort"));
        Assert.Equal(SystemColors.Resolve().Highlight, h.Style(h.ByClass("sort")).BackgroundColor);
    }
}

[Collection("Controls")]
public sealed class LinkTests : IDisposable
{
    private readonly ControlHarness h = new();
    private readonly Action<Uri> previous = Launcher.Open;
    private readonly List<Uri> opened = [];

    public LinkTests() => Launcher.Open = opened.Add;

    public void Dispose()
    {
        Launcher.Open = previous;
        h.Dispose();
    }

    [Fact]
    public void Activation_opens_web_addresses_and_marks_the_link_visited()
    {
        h.Render<LinkProbe>();
        var docs = h.ByClass("docs");
        Assert.Equal("link", docs.Attributes["role"]);
        Assert.Equal(true, docs.Attributes["focusable"]);
        Assert.Equal(false, docs.Attributes["visited"]);
        Assert.Equal("https://example.com/docs", docs.Attributes["tooltip"]);
        h.Click(docs);
        Assert.Equal([new Uri("https://example.com/docs")], opened);
        Assert.Equal(true, h.ByClass("docs").Attributes["visited"]);
        Assert.True(h.Component<LinkProbe>() is not null);
    }

    [Fact]
    public void Keyboard_activates_the_link()
    {
        h.Render<LinkProbe>();
        var probe = h.Component<LinkProbe>();
        h.Focus(h.ByClass("handler"));
        h.Press("Enter");
        h.Press(" ");
        Assert.Equal(2, probe.Clicks);
        Assert.Empty(opened);
        Assert.Equal(true, h.ByClass("handler").Attributes["visited"]);
    }

    [Fact]
    public void Only_absolute_http_https_and_mailto_addresses_are_opened()
    {
        h.Render<LinkProbe>();
        h.Click(h.ByClass("unsafe"));
        h.Click(h.ByClass("relative"));
        Assert.Empty(opened);
        h.Click(h.ByClass("external"));
        Assert.Equal("mailto", Assert.Single(opened).Scheme);
        Assert.True(Launcher.CanOpen(new Uri("http://example.com")));
        Assert.False(Launcher.CanOpen(new Uri("ftp://example.com")));
        Assert.False(Launcher.CanOpen(null));
        Assert.False(Launcher.TryOpen(new Uri("file:///c:/x")));
    }

    [Fact]
    public void An_external_link_has_a_mark_and_a_description()
    {
        h.Render<LinkProbe>();
        var external = h.ByClass("external");
        var mark = external.Elements().Single(e => e.Name == "LinkExternalMark");
        Assert.Equal(true, mark.Attributes["semantic-hidden"]);
        Assert.Equal(true, external.Attributes["external"]);
        Assert.Equal("Opens outside the application", external.Attributes["accessible-description"]);
        Assert.DoesNotContain(h.ByClass("docs").Elements(), e => e.Name == "LinkExternalMark");
    }

    [Fact]
    public void A_link_without_content_shows_its_address_and_a_disabled_link_does_nothing()
    {
        h.Render<LinkProbe>();
        Assert.Equal("https://example.com/", h.ByClass("bare").Text());
        h.Click(h.ByClass("off"));
        Assert.Empty(opened);
        Assert.Equal(0, h.Component<LinkProbe>().Clicks);
        Assert.Equal(false, h.ByClass("off").Attributes["focusable"]);
    }

    [Fact]
    public void The_basic_theme_draws_links_in_the_link_color()
    {
        h.Render<LinkProbe>();
        Assert.Equal(SystemColors.Resolve().LinkText, h.Style(h.ByClass("docs")).Color);
        Assert.NotEqual(h.Style(h.ByClass("docs")).Color, h.Style(h.ByClass("off")).Color);
    }
}
