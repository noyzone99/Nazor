using Nazor.Controls.Tests.Foundation.Fixtures;
using Nazor.Input;

namespace Nazor.Controls.Tests.Foundation;

[Collection("Controls")]
public sealed class KeyboardListNavigationTests
{
    [Theory]
    [InlineData("Listbox", "ListboxOption", false)]
    [InlineData("ListViewViewport", "ListViewItem", false)]
    [InlineData("TreeViewViewport", "TreeViewItem", false)]
    [InlineData("GridViewViewport", "GridViewItem", true)]
    public void Lists_reveal_keyboard_targets_and_stop_at_enabled_ends(string viewportName, string rowName, bool grid)
    {
        using var h = new ControlHarness();
        h.Render<KeyboardListsProbe>();
        var viewport = h.One(viewportName);
        var first = h.All(rowName).First(row => row.Attributes.GetValueOrDefault("disabled") is not true);
        h.Focus(first);
        h.Press("Home", grid ? KeyModifiers.Control : KeyModifiers.None);
        h.Press("ArrowUp");
        Assert.Same(first, h.Focused);
        h.Press("End", grid ? KeyModifiers.Control : KeyModifiers.None);
        var last = h.Focused!;
        Assert.Equal(rowName, last.Name);
        Assert.Equal("18", last.Text());
        AssertVisible(h, viewport, last);
        h.Press(grid ? "ArrowRight" : "ArrowDown");
        Assert.Same(last, h.Focused);
        h.Press("Home", grid ? KeyModifiers.Control : KeyModifiers.None);
        Assert.Equal(rowName, h.Focused!.Name);
        AssertVisible(h, viewport, h.Focused);
    }

    [Fact]
    public void Inline_suggestions_scroll_when_opened_at_the_last_item()
    {
        using var h = new ControlHarness();
        h.Render<KeyboardListsProbe>();
        var editor = h.One("InlineComboInput");
        h.Focus(editor);
        h.Press("ArrowUp");
        var last = h.All("InlineComboOption").Last();
        Assert.Equal(true, last.Attributes["current"]);
        AssertVisible(h, h.One("InlineComboPopup"), last);
        h.Press("ArrowDown");
        Assert.Equal(true, last.Attributes["current"]);
        Assert.Same(editor, h.Focused);
    }

    [Fact]
    public void Select_keeps_the_focused_option_visible_and_stops_at_both_ends()
    {
        using var h = new ControlHarness();
        h.Render<DropDownProbe>();
        h.Focus(h.One("Select"));
        h.Press("Enter");
        var options = h.All("SelectOption").ToArray();
        var popup = h.One("SelectPopup");
        h.Press("End");
        Assert.Same(options[^1], h.Focused);
        AssertVisible(h, popup, options[^1]);
        h.Press("ArrowDown");
        Assert.Same(options[^1], h.Focused);
        h.Press("Home");
        AssertVisible(h, popup, options[0]);
        h.Press("ArrowUp");
        Assert.Same(options[0], h.Focused);
        for (var i = 1; i < options.Length; i++)
        {
            h.Press("ArrowDown");
            AssertVisible(h, popup, options[i]);
        }
        h.Press("Enter");
        h.Press("Enter");
        AssertVisible(h, h.One("SelectPopup"), h.Focused!);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Combo_keeps_the_current_suggestion_visible_without_moving_editor_focus(bool openFromEnd)
    {
        using var h = new ControlHarness();
        h.Render<DropDownProbe>();
        var editor = h.One("ComboBoxInput");
        h.Focus(editor);
        h.Press(openFromEnd ? "ArrowUp" : "ArrowDown");
        var options = h.All("ComboBoxOption").ToArray();
        var popup = h.One("ComboBoxPopup");
        var first = openFromEnd ? options.Length - 1 : 0;
        Assert.Equal(true, options[first].Attributes["current"]);
        AssertVisible(h, popup, options[first]);
        for (var i = first; i < options.Length - 1; i++)
        {
            h.Press("ArrowDown");
            AssertVisible(h, popup, options[i + 1]);
        }
        h.Press("ArrowDown");
        Assert.Equal(true, options[^1].Attributes["current"]);
        for (var i = options.Length - 1; i > 0; i--)
        {
            h.Press("ArrowUp");
            AssertVisible(h, popup, options[i - 1]);
        }
        h.Press("ArrowUp");
        Assert.Equal(true, options[0].Attributes["current"]);
        Assert.Same(editor, h.Focused);
    }

    [Fact]
    public void Menu_keeps_the_focused_item_visible_and_stops_at_both_ends()
    {
        using var h = new ControlHarness(width: 320, height: 120);
        h.Render<MenuSizingProbe>();
        h.Click(h.One("Button"));
        var menu = h.One("Menu");
        var items = h.All("MenuItem").ToArray();
        h.Press("End");
        AssertVisible(h, menu, items[^1]);
        h.Press("ArrowDown");
        Assert.Same(items[^1], h.Focused);
        h.Press("Home");
        AssertVisible(h, menu, items[0]);
        h.Press("ArrowUp");
        Assert.Same(items[0], h.Focused);
    }

    private static void AssertVisible(ControlHarness h, ElementNode viewport, ElementNode item)
    {
        var outer = h.Bounds(viewport);
        var inner = h.Bounds(item);
        Assert.True(inner.Y >= outer.Y - 0.1f && inner.Bottom <= outer.Bottom + 0.1f,
            $"Item {inner} must be inside viewport {outer}.");
    }
}
