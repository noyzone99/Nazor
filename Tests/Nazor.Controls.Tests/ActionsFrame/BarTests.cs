using Nazor.Controls.Tests.ActionsFrame.Fixtures;
using Nazor.Input;
using Nazor.Style;

namespace Nazor.Controls.Tests.ActionsFrame;

[Collection("Controls")]
public sealed class ToolbarTests : IDisposable
{
    private readonly ControlHarness h = new();

    public void Dispose() => h.Dispose();

    [Fact]
    public void It_writes_the_role_and_the_item_styling_attributes()
    {
        h.Render<ToolbarProbe>();
        var editor = h.ByClass("editor");
        Assert.Equal("toolbar", editor.Attributes["role"]);
        Assert.Equal("horizontal", editor.Attributes["orientation"]);
        Assert.Equal(false, editor.Attributes["vertical"]);
        Assert.Equal("beside", editor.Attributes["label-position"]);
        Assert.Equal("medium", editor.Attributes["size"]);
        var vertical = h.ByClass("vertical");
        Assert.Equal(true, vertical.Attributes["vertical"]);
        Assert.Equal("vertical", vertical.Attributes["orientation"]);
        Assert.Equal("group", h.ByClass("history").Attributes["role"]);
        Assert.Equal("History", h.ByClass("history").Attributes["accessible-name"]);
        Assert.Equal("separator", h.ByClass("sep").Attributes["role"]);
        Assert.Equal("vertical", h.ByClass("sep").Attributes["orientation"]);
        Assert.Equal("horizontal", h.ByClass("vsep").Attributes["orientation"]);
        Assert.Equal("vertical", h.ByClass("vgroup").Attributes["orientation"]);
        Assert.Equal(true, h.ByClass("spacer").Attributes["semantic-hidden"]);
    }

    [Fact]
    public void The_label_position_follows_the_parameter()
    {
        h.Render<ToolbarProbe>();
        var probe = h.Component<ToolbarProbe>();
        probe.Position = ToolbarLabelPosition.Below;
        probe.Refresh();
        h.Frame();
        Assert.Equal("below", h.ByClass("editor").Attributes["label-position"]);
        var undo = h.Bounds(h.ByClass("undo").Elements().Single(e => e.Name == "ButtonLeading"));
        var label = h.Bounds(h.ByClass("undo").Elements().Single(e => e.Name == "ButtonLabel"));
        Assert.True(label.Y >= undo.Bottom - 0.5, "Below puts the label under the icon.");
        probe.Position = ToolbarLabelPosition.None;
        probe.Refresh();
        h.Frame();
        Assert.Equal(0, h.Bounds(h.ByClass("undo").Elements().Single(e => e.Name == "ButtonLabel")).Width);
        Assert.True(h.Bounds(h.ByClass("share").Elements().Single(e => e.Name == "ButtonLabel")).Width > 0);
    }

    [Fact]
    public void It_is_one_tab_stop_with_arrow_navigation()
    {
        h.Render<ToolbarProbe>();
        h.Focus(h.ByClass("undo"));
        h.Press("ArrowRight");
        Assert.Equal(h.ByClass("bold"), h.Focused);
        h.Press("End");
        Assert.Equal(h.ByClass("share"), h.Focused);
        h.Press("Home");
        Assert.Equal(h.ByClass("undo"), h.Focused);
        h.Press("Tab");
        Assert.NotNull(h.Focused);
        var editor = h.ByClass("editor");
        for (var node = h.Focused; node is not null; node = node.ParentElement) Assert.NotEqual(editor, node);
    }

    [Fact]
    public void Vertical_toolbars_use_up_and_down()
    {
        h.Render<ToolbarProbe>();
        var first = h.ByClass("vertical").Elements().First(e => e.Name == "Button");
        h.Focus(first);
        h.Press("ArrowDown");
        Assert.NotEqual(first, h.Focused);
        h.Press("ArrowUp");
        Assert.Equal(first, h.Focused);
    }

    [Fact]
    public void Commands_render_buttons_and_overflow_into_a_more_menu()
    {
        h.Render<ToolbarProbe>();
        h.Frame();
        var row = h.ByClass("commands").Elements().Single(e => e.Name == "ToolbarCommands");
        Assert.Equal(true, row.Attributes["overflowing"]);
        var visible = row.Elements().Where(e => e.Name is "Button" or "ToggleButton").Select(e => e.Text()).ToList();
        Assert.Contains("New", visible);
        Assert.DoesNotContain("Wrap", visible);
        var more = h.ByClass("ToolbarOverflowButton");
        Assert.Equal("More options", more.Attributes["accessible-name"]);
        Assert.Equal("small", more.Attributes["size"]);
        Assert.True(h.Bounds(row).Width <= 220.5);
        h.Click(more);
        var items = h.One("Menu").Elements().Where(e => e.Name is "MenuItem" or "MenuCheckItem").Select(e => e.Elements().Single(p => p.Name == "MenuItemLabel").Text()).ToList();
        Assert.Contains("Wrap", items);
        Assert.Equal(8, visible.Count + items.Count);
        h.Click(h.One("Menu").Elements().Single(e => e.Name == "MenuItem" && e.Text().Contains("Delete")));
        Assert.Contains("Delete", h.Component<ToolbarProbe>().Log);
        Assert.Empty(h.All("Menu"));
    }

    [Fact]
    public void Toggle_commands_follow_their_checked_state()
    {
        h.Render<ToolbarProbe>();
        var probe = h.Component<ToolbarProbe>();
        h.Click(h.ByClass("ToolbarOverflowButton"));
        var wrap = h.One("Menu").Elements().Single(e => e.Name == "MenuCheckItem");
        Assert.Equal(false, wrap.Attributes["checked"]);
        h.Click(wrap);
        Assert.True(probe.Wrap.IsChecked);
        Assert.Equal("wrap True", probe.Log.Last());
        h.Frame();
        h.Click(h.ByClass("ToolbarOverflowButton"));
        Assert.Equal(true, h.One("Menu").Elements().Single(e => e.Name == "MenuCheckItem").Attributes["checked"]);
    }

    [Fact]
    public void Visible_toggle_commands_are_toggle_buttons()
    {
        h.Render<ToolbarProbe>();
        var probe = h.Component<ToolbarProbe>();
        probe.Commands.RemoveRange(1, 6);
        probe.Refresh();
        h.Frame();
        h.Frame();
        var toggle = h.ByClass("commands").Elements().Single(e => e.Name == "ToggleButton");
        Assert.Equal("Wrap", toggle.Text());
        h.Click(toggle);
        Assert.True(probe.Wrap.IsChecked);
        Assert.Equal(true, h.ByClass("commands").Elements().Single(e => e.Name == "ToggleButton").Attributes["pressed"]);
        probe.Wrap.IsChecked = false;
        h.Frame();
        Assert.Equal(false, h.ByClass("commands").Elements().Single(e => e.Name == "ToggleButton").Attributes["pressed"]);
    }

    [Fact]
    public void Vertical_command_toolbars_show_every_command_and_icon_only_ones_keep_names()
    {
        h.Render<ToolbarProbe>();
        var vertical = h.ByClass("vertical").Elements().Single(e => e.Name == "ToolbarCommands");
        Assert.Equal(["Cut", "Copy"], vertical.Elements().Where(e => e.Name == "Button").Select(e => e.Text()));
        var cut = h.ByClass("icons").Elements().First(e => e.Name == "Button");
        Assert.Equal("Cut", cut.Attributes["accessible-name"]);
        Assert.Contains("Ctrl+X", (string)cut.Attributes["tooltip"]!);
        Assert.Single(cut.Elements(), e => e.Name == "ButtonLeading");
        Assert.Equal(0, h.Bounds(cut.Elements().Single(e => e.Name == "ButtonLabel")).Width);
        h.Click(cut);
        Assert.Contains("cut", h.Component<ToolbarProbe>().Log);
    }

    [Fact]
    public void Free_items_keep_their_behavior()
    {
        h.Render<ToolbarProbe>();
        var probe = h.Component<ToolbarProbe>();
        h.Click(h.ByClass("undo"));
        h.Click(h.ByClass("bold"));
        Assert.Equal(["undo"], probe.Log);
        Assert.True(probe.Bold);
        var spacer = h.Bounds(h.ByClass("spacer"));
        Assert.True(spacer.Width > 50, "The spacer takes the free space.");
    }
}

[Collection("Controls")]
public sealed class ActionBarTests : IDisposable
{
    private readonly ControlHarness h = new();

    public void Dispose() => h.Dispose();

    [Fact]
    public void Slots_are_placed_at_the_start_center_and_end()
    {
        h.Render<ActionBarProbe>();
        var bar = h.ByClass("bar");
        Assert.Equal("toolbar", bar.Attributes["role"]);
        Assert.Equal(["ActionBarStart", "ActionBarCenter", "ActionBarEnd"], bar.Children.OfType<ElementNode>().Select(e => e.Name));
        var end = bar.Elements().Single(e => e.Name == "ActionBarEnd");
        Assert.Equal(["cancel", "apply"], end.Elements().Where(e => e.Name == "Button").Select(e => e.Class));
        var barBounds = h.Bounds(bar);
        Assert.True(h.Bounds(h.ByClass("count")).X < barBounds.X + 20);
        Assert.True(h.Bounds(h.ByClass("apply")).Right > barBounds.Right - 20);
        var center = h.Bounds(h.ByClass("select-all"));
        Assert.InRange(center.X + center.Width / 2, barBounds.X + barBounds.Width / 2 - 40, barBounds.X + barBounds.Width / 2 + 40);
        Assert.Single(h.ByClass("loose").Elements().Single(e => e.Name == "ActionBarEnd").Elements(), e => e.Class == "only");
    }

    [Fact]
    public void Arrows_move_between_the_commands()
    {
        h.Render<ActionBarProbe>();
        h.Focus(h.ByClass("select-all"));
        h.Press("ArrowRight");
        Assert.Equal(h.ByClass("cancel"), h.Focused);
        h.Press("ArrowRight");
        Assert.Equal(h.ByClass("apply"), h.Focused);
    }
}

[Collection("Controls")]
public sealed class FloatingActionButtonTests : IDisposable
{
    private readonly ControlHarness h = new();

    public void Dispose() => h.Dispose();

    [Fact]
    public void It_floats_in_the_corner_of_its_container()
    {
        h.Render<ActionBarProbe>();
        var pane = h.Bounds(h.ByClass("pane"));
        var compose = h.ByClass("compose");
        Assert.Equal("bottom-end", compose.Attributes["placement"]);
        Assert.Equal(false, compose.Attributes["extended"]);
        Assert.Equal(true, compose.Attributes["icon-only"]);
        var bounds = h.Bounds(compose);
        Assert.Equal(pane.Right - 16, bounds.Right, 1);
        Assert.Equal(pane.Bottom - 16, bounds.Bottom, 1);
        var extended = h.Bounds(h.ByClass("extended"));
        Assert.Equal(pane.X + 16, extended.X, 1);
        Assert.Equal(pane.Y + 16, extended.Y, 1);
        Assert.Equal("top-start", h.ByClass("extended").Attributes["placement"]);
        Assert.Equal("bottom-start", h.ByClass("command").Attributes["placement"]);
        Assert.Equal("top-end", h.ByClass("text-command").Attributes["placement"]);
        Assert.Equal(SystemColors.Resolve().AccentColor, h.Style(compose).BackgroundColor);
    }

    [Fact]
    public void A_label_makes_it_extended()
    {
        h.Render<ActionBarProbe>();
        var extended = h.ByClass("extended");
        Assert.Equal(true, extended.Attributes["extended"]);
        Assert.Equal("large", extended.Attributes["size"]);
        Assert.Equal("New message", extended.Elements().Single(e => e.Name == "ButtonLabel").Text());
        Assert.True(h.Bounds(extended).Width > h.Bounds(h.ByClass("compose")).Width);
    }

    [Fact]
    public void It_runs_the_click_and_the_command()
    {
        h.Render<ActionBarProbe>();
        var probe = h.Component<ActionBarProbe>();
        h.Click(h.ByClass("compose"));
        h.Click(h.ByClass("extended"));
        h.Click(h.ByClass("off"));
        Assert.Equal(11, probe.Clicks);
        h.Focus(h.ByClass("command"));
        h.Press("Enter");
        Assert.Equal(1, probe.Added);
        Assert.Equal("button", h.ByClass("command").Attributes["role"]);
    }

    [Fact]
    public void A_command_names_an_icon_button_and_labels_a_text_one()
    {
        h.Render<ActionBarProbe>();
        var icon = h.ByClass("command");
        Assert.DoesNotContain(icon.Elements(), e => e.Name == "ButtonLabel");
        Assert.Equal("Add item", icon.Attributes["accessible-name"]);
        Assert.Equal("Add item", icon.Attributes["tooltip"]);
        var text = h.ByClass("text-command");
        Assert.Equal(true, text.Attributes["extended"]);
        Assert.Equal("Add item", text.Elements().Single(e => e.Name == "ButtonLabel").Text());
    }
}
