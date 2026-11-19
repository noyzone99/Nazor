using Nazor.Controls.Tests.Foundation.Fixtures;
using Nazor.Graphics;
using Nazor.Input;

namespace Nazor.Controls.Tests.Foundation;

[Collection("Controls")]
public sealed class MenuTests : IDisposable
{
    private readonly ControlHarness h = new();

    public void Dispose() => h.Dispose();

    private void Open()
    {
        h.Render<MenuProbe>();
        h.Click(h.ByClass("opener"));
        Assert.Single(h.All("Menu"));
    }

    [Theory]
    [InlineData(180, false)]
    [InlineData(120, true)]
    public void Menus_scroll_only_when_their_content_exceeds_the_available_area(int height, bool scrolls)
    {
        using var sized = new ControlHarness(width: 320, height: height);
        sized.Render<MenuSizingProbe>();
        sized.Click(sized.One("Button"));
        var menu = sized.One("Menu");
        Assert.Equal(scrolls, (sized.Scene.GetScrollState(menu)?.MaxOffsetY ?? 0) > 0.1f);
        Assert.True(sized.Bounds(menu).Y >= 0 && sized.Bounds(menu).Bottom <= height);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(1.25)]
    [InlineData(1.5)]
    [InlineData(2)]
    public void Content_sized_menus_have_no_scrollbar_at_fractional_scales(float scale)
    {
        h.Scene.Scale = scale;
        Open();
        Assert.Equal(0, h.Scene.GetScrollState(h.One("Menu"))!.MaxOffsetY);
        Assert.Null(h.Scene.GetScrollBar(h.One("Menu"), horizontal: false));
    }

    [Fact]
    public void A_menu_opens_next_to_its_anchor_with_the_focus_on_its_first_item()
    {
        Open();
        var menu = h.One("Menu");
        Assert.Equal("menu", menu.Attributes["role"]);
        Assert.Equal(h.Bounds(h.ByClass("opener")).Bottom, h.Bounds(menu).Y, 1);
        Assert.Equal("new", h.Focused?.Class);
        Assert.Equal("Ctrl+N", h.ByClass("new").Elements().Single(e => e.Name == "ShortcutLabel").Attributes["accessible-name"]);
    }

    [Fact]
    public void Running_an_item_closes_the_menu_and_disabled_items_do_nothing()
    {
        Open();
        var probe = h.Component<MenuProbe>();
        h.Click(h.ByClass("disabled"));
        Assert.Empty(probe.Log);
        h.Click(h.ByClass("new"));
        Assert.Equal(["new"], probe.Log);
        Assert.False(probe.Open);
        Assert.Empty(h.All("Menu"));
    }

    [Fact]
    public void Check_and_radio_items_update_their_bindings()
    {
        Open();
        var probe = h.Component<MenuProbe>();
        h.Click(h.ByClass("sidebar"));
        Assert.True(probe.Sidebar);
        Assert.True(probe.Open);
        Assert.Equal("check", h.ByClass("sidebar").Elements().First().Elements().First().Attributes["glyph"]);
        h.Click(h.ByClass("zoom-200"));
        Assert.Equal("200", probe.Zoom);
        Assert.False(probe.Open);
    }

    [Fact]
    public void The_right_arrow_opens_a_submenu_and_its_items_close_every_menu()
    {
        Open();
        var probe = h.Component<MenuProbe>();
        h.Focus(h.ByClass("recent"));
        h.Press("ArrowRight");
        Assert.Equal(2, h.All("Menu").Count());
        Assert.Equal(true, h.ByClass("recent").Attributes["expanded"]);
        h.Click(h.ByClass("recent-1"));
        Assert.Equal(["recent"], probe.Log);
        Assert.Empty(h.All("Menu"));
    }

    [Fact]
    public void Escape_closes_the_menu_and_returns_the_focus()
    {
        Open();
        h.Press("Escape");
        Assert.Empty(h.All("Menu"));
        Assert.Equal("opener", h.Focused?.Class);
    }

    [Fact]
    public void A_context_menu_opens_at_the_pointer()
    {
        h.Render<MenuProbe>();
        var area = h.Bounds(h.ByClass("area"));
        var point = new Point(area.X + 10, area.Y + 5);
        h.Input.PointerPressed(point, PointerButton.Secondary, KeyModifiers.None);
        h.Input.PointerReleased(point, PointerButton.Secondary, KeyModifiers.None);
        h.Frame();
        var menu = h.One("Menu");
        Assert.Equal(point.X, h.Bounds(menu).X, 1);
        Assert.Equal(point.Y, h.Bounds(menu).Y, 1);
        h.Click(h.ByClass("copy"));
        Assert.Equal(["copy"], h.Component<MenuProbe>().Log);
        Assert.Empty(h.All("Menu"));
    }
}
