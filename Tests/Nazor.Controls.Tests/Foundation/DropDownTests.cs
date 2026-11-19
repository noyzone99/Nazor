using Nazor.Controls.Tests.Foundation.Fixtures;
using Nazor.Graphics;
using Nazor.Input;
using Nazor.Style;

namespace Nazor.Controls.Tests.Foundation;

[Collection("Controls")]
public sealed class DropDownTests
{
    [Theory]
    [InlineData("")]
    [InlineData("Item 1")]
    public void Basic_combo_lists_match_select_styling_and_highlight_with_keyboard_and_pointer(string query)
    {
        using var h = new ControlHarness();
        h.Render<DropDownProbe>();
        h.Click(h.One("Select"));
        var selectPopup = h.Style(h.One("SelectPopup"));
        var selectOption = h.Style(h.All("SelectOption").Last());
        var selectedOption = h.Style(h.All("SelectOption").First());
        h.Press("Escape");

        var editor = h.One("ComboBoxInput");
        Assert.Equal("Type an item", editor.Attributes["placeholder-text"]);
        h.Focus(editor);
        if (query.Length == 0) h.Click(h.One("ComboBoxChevron"));
        else h.Type(query);

        var popup = h.One("ComboBoxPopup");
        var comboPopup = h.Style(popup);
        StyleProperty[] properties = [StyleProperties.Display, StyleProperties.FlexDirection, StyleProperties.OverflowY,
            StyleProperties.PaddingTop, StyleProperties.PaddingRight, StyleProperties.BorderTopWidth,
            StyleProperties.BorderTopColor, StyleProperties.BackgroundColor, StyleProperties.Color];
        foreach (var property in properties) Assert.Equal(selectPopup.Get(property), comboPopup.Get(property));
        var options = h.All("ComboBoxOption").ToArray();
        Assert.Equal(query.Length == 0 ? 20 : 10, options.Length);
        var plain = h.Style(options[0]);
        Assert.Equal(selectOption.Get(StyleProperties.PaddingTop), plain.Get(StyleProperties.PaddingTop));
        Assert.Equal(selectOption.Get(StyleProperties.PaddingLeft), plain.Get(StyleProperties.PaddingLeft));
        Assert.True(h.Scene.GetScrollState(popup)!.MaxOffsetY > 0);

        h.Press("ArrowDown");
        Assert.Equal(selectedOption.BackgroundColor, h.Style(options[0]).BackgroundColor);
        Assert.Equal(selectedOption.Color, h.Style(options[0]).Color);
        Assert.NotEqual(plain.BackgroundColor, h.Style(options[0]).BackgroundColor);
        var bounds = h.Bounds(options[1]);
        h.Input.PointerMoved(new Point(bounds.X + bounds.Width / 2, bounds.Y + bounds.Height / 2), KeyModifiers.None);
        h.Frame();
        Assert.Equal(selectedOption.BackgroundColor, h.Style(options[1]).BackgroundColor);
        Assert.Equal(plain.BackgroundColor, h.Style(options[0]).BackgroundColor);
        Assert.Same(editor, h.Focused);
    }

    [Fact]
    public void Basic_combo_styles_the_empty_filtered_list()
    {
        using var h = new ControlHarness();
        h.Render<DropDownProbe>();
        h.Focus(h.One("ComboBoxInput"));
        h.Type("No match");
        Assert.Empty(h.All("ComboBoxOption"));
        var empty = h.One("ComboBoxEmpty");
        Assert.Equal("No matching items", empty.Text());
        Assert.Equal(Length.Px(3), h.Style(empty).Get(StyleProperties.PaddingTop));
        Assert.Equal(Length.Px(6), h.Style(empty).Get(StyleProperties.PaddingLeft));
        Assert.NotEqual(h.Style(h.One("ComboBoxPopup")).Color, h.Style(empty).Color);
    }

    [Theory]
    [InlineData(0, 4)]
    [InlineData(2, 0)]
    [InlineData(5, 4)]
    public void Invalid_visible_item_preferences_are_rejected(int minimum, int maximum)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Feedback.ProgressTests.Validate(
            new Select<string> { PreferredMinVisibleItems = minimum, PreferredMaxVisibleItems = maximum }));
        Assert.Throws<ArgumentOutOfRangeException>(() => Feedback.ProgressTests.Validate(
            new ComboBox<string> { PreferredMinVisibleItems = minimum, PreferredMaxVisibleItems = maximum }));
    }

    [Fact]
    public void Automatic_select_uses_the_space_above_and_keeps_remaining_options_scrollable()
    {
        using var h = new ControlHarness(width: 300, height: 198);
        h.Render<DropDownProbe>();
        var field = h.One("Select");

        h.Click(field);

        var popup = h.One("SelectPopup");
        var bounds = h.Bounds(popup);
        Assert.Equal(h.Bounds(field).Y, bounds.Bottom, 0.01f);
        var options = h.All("SelectOption").ToArray();
        Assert.Equal(20, options.Length);
        Assert.Equal(4, options.Count(option => h.Bounds(option).Bottom <= bounds.Bottom));
        Assert.True(h.Scene.GetScrollState(popup)!.MaxOffsetY > 0);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Editable_combo_keeps_its_position_and_editor_focus_while_filtering(bool automatic)
    {
        using var h = new ControlHarness(width: 300, height: 210);
        h.Render<DropDownProbe>();
        if (automatic) h.Component<DropDownProbe>().UseAutomaticPlacement();
        var editor = h.One("ComboBoxInput");
        h.Focus(editor);
        h.Type("Item");
        Assert.Equal(h.Bounds(h.One("ComboBox")).Y, h.Bounds(h.One("ComboBoxPopup")).Bottom, 0.01f);

        h.Type(" 12");

        var popup = h.One("ComboBoxPopup");
        Assert.Equal(h.Bounds(h.One("ComboBox")).Y, h.Bounds(popup).Bottom, 0.01f);
        Assert.Single(h.All("ComboBoxOption"));
        Assert.Same(editor, h.Focused);
        h.Press("ArrowDown");
        h.Press("Enter");
        Assert.Equal("Item 12", h.Component<DropDownProbe>().Chosen);
    }

    [Theory]
    [InlineData(300, 3)]
    [InlineData(150, 1)]
    public void Group_headers_and_variable_rows_fit_the_available_space_with_a_soft_minimum(int height, int minimumVisible)
    {
        using var h = new ControlHarness(width: 300, height: height);
        h.Render<DropDownRowsProbe>();
        h.Click(h.One("Select"));
        var popup = h.One("SelectPopup");
        var bounds = h.Bounds(popup);
        var visible = h.All("SelectOption").Count(option => h.Bounds(option).Bottom <= bounds.Bottom);
        Assert.InRange(visible, minimumVisible, height == 300 ? 3 : 2);
        Assert.True(bounds.Y >= 0 && bounds.Bottom <= height);
        Assert.Equal(2, h.All("SelectGroupHeader").Count());
        Assert.True(h.Scene.GetScrollState(popup)!.MaxOffsetY > 0);
    }
}
