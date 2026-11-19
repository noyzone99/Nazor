using Nazor.Controls.Tests.Overlays.Fixtures;
using Nazor.Graphics;
using Nazor.Input;
using Nazor.Style;

namespace Nazor.Controls.Tests.Overlays;

[Collection("Controls")]
public sealed class MenuBarTests : IDisposable
{
    private readonly ControlHarness h = new();

    public void Dispose() => h.Dispose();

    private MenuBarProbe Render()
    {
        h.Render<MenuBarProbe>();
        return h.Component<MenuBarProbe>();
    }

    private void Hover(ElementNode element)
    {
        var bounds = h.Bounds(element);
        h.Input.PointerMoved(new Point(bounds.X + bounds.Width / 2, bounds.Y + bounds.Height / 2), KeyModifiers.None);
        h.Frame();
    }

    private ElementNode? OpenMenu => h.All("Menu").SingleOrDefault();

    [Fact]
    public void The_bar_and_its_items_expose_menu_roles_and_states()
    {
        Render();
        var bar = h.One("MenuBar");
        Assert.Equal("menubar", bar.Attributes["role"]);
        Assert.Equal("Main", bar.Attributes["accessible-name"]);
        Assert.All(h.All("MenuBarItem"), item => Assert.Equal("menuitem", item.Attributes["role"]));
        Assert.Equal(false, h.ByClass("file").Attributes["expanded"]);
        Assert.Equal(true, h.ByClass("tools").Attributes["disabled"]);
        Assert.Equal("File", h.ByClass("file").Elements().Single(e => e.Name == "MenuBarItemLabel").Text());
    }

    [Fact]
    public void The_bar_is_a_single_tab_stop_with_arrow_navigation_that_skips_disabled_items()
    {
        Render();
        h.Focus(h.ByClass("before"));
        h.Press("Tab");
        Assert.Equal("file", h.Focused?.Class);
        h.Press("ArrowRight");
        Assert.Equal("edit", h.Focused?.Class);
        h.Press("ArrowRight");
        Assert.Equal("help", h.Focused?.Class);
        h.Press("ArrowLeft");
        Assert.Equal("edit", h.Focused?.Class);
        h.Press("Tab");
        Assert.Equal("after", h.Focused?.Class);
        Assert.Null(OpenMenu);
    }

    [Fact]
    public void Down_opens_the_menu_on_its_first_item_and_up_on_its_last()
    {
        Render();
        h.Focus(h.ByClass("file"));
        h.Press("ArrowDown");
        Assert.NotNull(OpenMenu);
        Assert.Equal("menu", OpenMenu!.Attributes["role"]);
        Assert.Equal(true, h.ByClass("file").Attributes["expanded"]);
        Assert.Equal("new", h.Focused?.Class);
        Assert.Equal(h.Bounds(h.ByClass("file")).Bottom, h.Bounds(OpenMenu).Y, 1);
        h.Press("Escape");
        Assert.Null(OpenMenu);
        Assert.Equal("file", h.Focused?.Class);
        h.Press("ArrowUp");
        Assert.Equal("exit", h.Focused?.Class);
    }

    [Fact]
    public void Enter_and_space_open_the_menu()
    {
        Render();
        h.Focus(h.ByClass("edit"));
        h.Press("Enter");
        Assert.Equal("undo", h.Focused?.Class);
        h.Press("Escape");
        Assert.Equal("edit", h.Focused?.Class);
        h.Press(" ");
        Assert.Equal("undo", h.Focused?.Class);
    }

    [Fact]
    public void Left_and_right_switch_menus_while_one_is_open()
    {
        var probe = Render();
        h.Focus(h.ByClass("file"));
        h.Press("ArrowDown");
        h.Press("ArrowDown");
        Assert.Equal("open", h.Focused?.Class);
        h.Press("ArrowRight");
        Assert.Single(h.All("Menu"));
        Assert.Equal(true, h.ByClass("edit").Attributes["expanded"]);
        Assert.Equal(false, h.ByClass("file").Attributes["expanded"]);
        Assert.Equal("undo", h.Focused?.Class);
        Assert.True(probe.EditOpen);
        h.Press("ArrowRight");
        Assert.Equal(true, h.ByClass("help").Attributes["expanded"]);
        Assert.Equal("about", h.Focused?.Class);
        h.Press("ArrowRight");
        Assert.Equal(true, h.ByClass("file").Attributes["expanded"]);
        Assert.Equal("new", h.Focused?.Class);
        h.Press("ArrowLeft");
        Assert.Equal(true, h.ByClass("help").Attributes["expanded"]);
        Assert.False(probe.EditOpen);
    }

    [Fact]
    public void The_right_arrow_on_a_submenu_item_opens_the_submenu_instead_of_switching()
    {
        var probe = Render();
        h.Focus(h.ByClass("file"));
        h.Press("ArrowDown");
        h.Focus(h.ByClass("recent"));
        h.Press("ArrowRight");
        Assert.Equal(2, h.All("Menu").Count());
        Assert.Equal(true, h.ByClass("file").Attributes["expanded"]);
        h.Click(h.ByClass("recent-1"));
        Assert.Equal(["recent"], probe.Log);
        Assert.Empty(h.All("Menu"));
        Assert.Equal(false, h.ByClass("file").Attributes["expanded"]);
    }

    [Fact]
    public void A_click_toggles_a_menu_and_running_an_item_closes_it()
    {
        var probe = Render();
        h.Click(h.ByClass("edit"));
        Assert.NotNull(OpenMenu);
        h.Click(h.ByClass("redo"));
        Assert.Equal(["redo"], probe.Log);
        Assert.Null(OpenMenu);
        Assert.Equal("edit", h.Focused?.Class);
        h.Click(h.ByClass("tools"));
        Assert.Null(OpenMenu);
    }

    [Fact]
    public void Hovering_another_item_while_a_menu_is_open_switches_menus()
    {
        Render();
        Hover(h.ByClass("edit"));
        Assert.Null(OpenMenu);
        h.Click(h.ByClass("file"));
        Assert.Equal(true, h.ByClass("file").Attributes["expanded"]);
        Hover(h.ByClass("edit"));
        Assert.Single(h.All("Menu"));
        Assert.Equal(true, h.ByClass("edit").Attributes["expanded"]);
        Assert.Equal(false, h.ByClass("file").Attributes["expanded"]);
        Hover(h.ByClass("tools"));
        Assert.Equal(true, h.ByClass("edit").Attributes["expanded"]);
    }

    [Fact]
    public void F10_and_alt_move_the_focus_to_the_bar()
    {
        Render();
        h.Focus(h.ByClass("after"));
        h.Press("F10");
        Assert.Equal("file", h.Focused?.Class);
        h.Press("ArrowRight");
        h.Focus(h.ByClass("before"));
        h.Press("Alt", KeyModifiers.Alt);
        Assert.Equal("edit", h.Focused?.Class);
    }

    [Fact]
    public void The_basic_theme_highlights_the_open_item()
    {
        Render();
        h.Click(h.ByClass("file"));
        Assert.Equal(SystemColors.Resolve().Highlight, h.Style(h.ByClass("file")).BackgroundColor);
        Assert.NotEqual(h.Style(h.ByClass("file")).BackgroundColor, h.Style(h.ByClass("help")).BackgroundColor);
    }
}
