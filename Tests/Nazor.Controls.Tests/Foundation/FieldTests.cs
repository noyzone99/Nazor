using Nazor.Controls.Tests.Foundation.Fixtures;
using Nazor.Style;

namespace Nazor.Controls.Tests.Foundation;

[Collection("Controls")]
public sealed class FieldTests : IDisposable
{
    private readonly ControlHarness h = new();

    public void Dispose() => h.Dispose();

    private ElementNode Part(string className, string name) => h.ByClass(className).Elements().Single(e => e.Name == name);

    [Fact]
    public void Typing_updates_the_bound_value_counter_and_clear_button()
    {
        h.Render<FieldProbe>();
        var probe = h.Component<FieldProbe>();
        Assert.False(ValidationFeedback.IsEligible(Part("name", "TextFieldInput")));
        h.Focus(Part("name", "TextFieldInput"));
        h.Type("Atelier");
        Assert.True(new ControlStateSelector("user-valid").Matches(Part("name", "TextFieldInput")));
        Assert.Equal("Atelier", probe.Name);
        Assert.Equal("7/10", Part("name", "TextFieldCounter").Text());
        h.Click(Part("name", "TextFieldClear"));
        Assert.Equal("", probe.Name);
        Assert.DoesNotContain(h.ByClass("name").Elements(), e => e.Name == "TextFieldClear");
    }

    [Fact]
    public void The_editor_is_named_by_its_field_and_side_text_is_hidden_from_readers()
    {
        h.Render<FieldProbe>();
        var input = Part("name", "TextFieldInput");
        Assert.Equal(h.All("Label").First().Id, input.Attributes["labeled-by"]);
        Assert.Equal(true, input.Attributes["required"]);
        Assert.Equal("textbox", input.Attributes["role"]);
        Assert.Equal(true, Part("price", "TextFieldPrefix").Attributes["semantic-hidden"]);
        Assert.Equal("month", Part("price", "TextFieldSuffix").Text());
        Assert.Equal("Price", Part("price", "TextFieldInput").Attributes["accessible-name"]);
    }

    [Fact]
    public void A_text_area_accepts_line_breaks()
    {
        h.Render<FieldProbe>();
        var input = Part("notes", "TextAreaInput");
        Assert.Equal(true, input.Attributes["multiline"]);
        h.Focus(input);
        h.Press("End");
        h.Press("Enter");
        h.Type("b");
        Assert.Equal("a\nb", h.Component<FieldProbe>().Notes);
    }

    [Fact]
    public void A_select_opens_on_click_and_chooses_an_option()
    {
        h.Render<FieldProbe>();
        var probe = h.Component<FieldProbe>();
        h.Click(h.ByClass("city"));
        Assert.Equal(true, h.ByClass("city").Attributes["expanded"]);
        var options = h.All("SelectOption").ToList();
        Assert.Equal(4, options.Count);
        Assert.Equal(true, options[0].Attributes["selected"]);
        Assert.True(options[2].IsDisabled);
        h.Click(options[1]);
        Assert.Equal("Milan", probe.City);
        Assert.Empty(h.All("SelectPopup"));
        Assert.Equal("Milan", h.One("SelectValue").Text());
    }

    [Fact]
    public void Arrows_and_typing_change_a_closed_select_and_skip_disabled_options()
    {
        h.Render<FieldProbe>();
        var probe = h.Component<FieldProbe>();
        h.Focus(h.ByClass("city"));
        h.Press("ArrowDown");
        Assert.Equal("Milan", probe.City);
        h.Press("ArrowDown");
        Assert.Equal("Turin", probe.City);
        h.Press("Home");
        Assert.Equal("Rome", probe.City);
        h.Press("t");
        Assert.Equal("Turin", probe.City);
        h.Press("F4");
        Assert.Single(h.All("SelectPopup"));
        h.Press("Escape");
        Assert.Empty(h.All("SelectPopup"));
    }

    [Fact]
    public void A_label_with_for_activates_its_target()
    {
        h.Render<FieldProbe>();
        h.Click(h.ByClass("for-label"));
        Assert.True(h.Component<FieldProbe>().Target);
    }
}
