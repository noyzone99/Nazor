using System.Globalization;
using Nazor.Controls.Primitives;
using Nazor.Controls.Tests.Pickers.Fixtures;
using Nazor.Input;
using StyleColor = Nazor.Style.Color;

namespace Nazor.Controls.Tests.Pickers;

[Collection("Controls")]
public sealed class ColorPickerTests : IDisposable
{
    private readonly ControlHarness h = new(width: 700, height: 900);
    private ColorPickerProbe Probe => h.Component<ColorPickerProbe>();
    public void Dispose() => h.Dispose();

    private void Show(Action<ColorPickerProbe>? setup = null)
    {
        h.Render<ColorPickerProbe>(); setup?.Invoke(Probe); Probe.Refresh(); h.Frame();
    }

    private ElementNode Channel(string name) => h.All("TextFieldInput").Single(input =>
        input.ParentElement?.ParentElement is { Name: "ColorPickerChannel" } channel && Equals(channel.Attributes["channel"], name));

    private ElementNode Hex => h.All("TextFieldInput").Single(input => input.ParentElement?.ParentElement?.Name == "ColorPickerHex");

    private void Type(ElementNode editor, string text)
    {
        h.Focus(editor); h.Press("a", KeyModifiers.Control);
        if (text.Length == 0) h.Press("Backspace"); else h.Type(text);
    }

    [Fact]
    public void Defaults_compose_real_named_controls_with_unique_labels_and_alpha_semantics()
    {
        Show();
        Assert.Equal("group", h.One("ColorPicker").Attributes["role"]);
        Assert.Equal("huesaturation", h.One("ColorPickerSpectrum").Attributes["components"]);
        Assert.Equal("value", h.One("ColorPickerSlider").Attributes["channel"]);
        Assert.Equal("alpha", h.One("ColorPickerAlphaSlider").Attributes["channel"]);
        Assert.Equal("Opacity", Channel("alpha").Attributes["accessible-name"]);
        Assert.Equal("#33669980", Hex.Attributes["value"]);
        var ids = h.Root.Elements().Select(element => element.Attributes.GetValueOrDefault("id")).OfType<string>().ToArray();
        Assert.Equal(ids.Distinct().Count(), ids.Length);
        foreach (var label in h.All("Label")) Assert.Contains(ids, id => Equals(label.Attributes["label-for"], id));
        Assert.Empty(Probe.Changes);
        Assert.Empty(Probe.Commits);
    }

    [Fact]
    public void RGB_drafts_commit_once_on_Enter_and_keep_all_other_channels()
    {
        Show();
        Type(Channel("green"), "200");
        Assert.Equal(new StyleColor(51, 102, 153, 128), Probe.Value);
        Assert.Empty(Probe.Changes);
        h.Press("Enter");
        Assert.Equal(new StyleColor(51, 200, 153, 128), Probe.Value);
        Assert.Single(Probe.Changes);
        Assert.Single(Probe.Commits);
        Assert.Equal("200", Channel("green").Attributes["value"]);
        h.Focus(Channel("blue"));
        Assert.Single(Probe.Changes);
        Assert.Single(Probe.Commits);
    }

    [Theory]
    [InlineData("")]
    [InlineData("NaN")]
    [InlineData("1e999")]
    [InlineData("1 / 0")]
    public void Invalid_numeric_drafts_keep_the_color_and_restore_when_focus_leaves(string draft)
    {
        Show();
        Type(Channel("red"), draft); h.Press("Enter");
        Assert.Equal(new StyleColor(51, 102, 153, 128), Probe.Value);
        Assert.True(Channel("red").Attributes["invalid"] is true);
        Assert.Empty(Probe.Changes);
        Assert.Empty(Probe.Commits);
        h.Focus(Channel("green"));
        Assert.Equal("51", Channel("red").Attributes["value"]);
        Assert.False(Channel("red").Attributes["invalid"] is true);
    }

    [Fact]
    public void Channels_clamp_and_HSV_text_uses_the_requested_number_culture()
    {
        Show(probe => { probe.Representation = ColorRepresentation.Hsv; probe.Culture = CultureInfo.GetCultureInfo("it-IT"); });
        Type(Channel("saturation"), "12,5"); h.Press("Enter");
        Assert.Equal(.125, Probe.Picker.CurrentHsvColor.S, 6);
        Assert.Equal("12,5", Channel("saturation").Attributes["value"]);
        Type(Channel("value"), "900"); h.Press("Enter");
        Assert.Equal(1, Probe.Picker.CurrentHsvColor.V);
        Assert.Equal("100", Channel("value").Attributes["value"]);
        Assert.Equal(128, Probe.Value.A);
    }

    [Theory]
    [InlineData("#11223344", 17, 34, 51, 68)]
    [InlineData("0f08", 0, 255, 0, 136)]
    [InlineData("#abc", 170, 187, 204, 255)]
    public void Hex_uses_explicit_CSS_RGBA_order_and_normalizes_after_commit(string hex, byte red, byte green, byte blue, byte alpha)
    {
        Show(); Type(Hex, hex); h.Press("Enter");
        Assert.Equal(new StyleColor(red, green, blue, alpha), Probe.Value);
        Assert.Equal(ColorModel.ToHex(Probe.Value, true), Hex.Attributes["value"]);
        Assert.Single(Probe.Changes);
        Assert.Single(Probe.Commits);
    }

    [Fact]
    public void Invalid_hex_is_retained_for_correction_then_restored_on_blur()
    {
        Show(); Type(Hex, "#gg0000"); h.Press("Enter");
        Assert.True(Hex.Attributes["invalid"] is true);
        Assert.Empty(Probe.Changes);
        h.Focus(Channel("red"));
        Assert.Equal("#33669980", Hex.Attributes["value"]);
        Assert.False(Hex.Attributes["invalid"] is true);
    }

    [Fact]
    public void Disabling_alpha_hides_its_controls_and_preserves_the_existing_alpha_during_other_edits()
    {
        Show(probe => probe.Alpha = false);
        Assert.Empty(h.All("ColorPickerAlphaSlider"));
        Assert.DoesNotContain(h.All("ColorPickerChannel"), element => Equals(element.Attributes["channel"], "alpha"));
        Type(Hex, "#ff000001"); h.Press("Enter");
        Assert.Equal(new StyleColor(255, 0, 0, 128), Probe.Value);
        Assert.Equal("#ff0000", Hex.Attributes["value"]);
    }

    [Fact]
    public void Spectrum_keyboard_changes_preview_and_commit_once_while_unbound_state_survives_unrelated_renders()
    {
        Show(probe => probe.Controlled = false);
        h.Focus(h.One("ColorPickerSpectrum")); h.Press("ArrowRight");
        Assert.Equal(211, Probe.Picker.CurrentHsvColor.H, 6);
        Assert.Single(Probe.Changes);
        Assert.Single(Probe.Commits);
        var local = Probe.Picker.CurrentColor;
        Assert.NotEqual(Probe.Value, local);
        Probe.Refresh(); h.Frame(); Assert.Equal(local, Probe.Picker.CurrentColor);
        Probe.Value = new(255, 0, 0, 255); Probe.Refresh(); h.Frame();
        Assert.Equal(Probe.Value, Probe.Picker.CurrentColor);
        Assert.Equal(0, Probe.Picker.CurrentHsvColor.H);
        Assert.Single(Probe.Changes);
    }

    [Fact]
    public void Hue_is_retained_through_gray_and_hue_saturation_through_black()
    {
        Show(probe => probe.Representation = ColorRepresentation.Hsv);
        Type(Channel("saturation"), "0"); h.Press("Enter");
        Assert.Equal(210, Probe.Picker.CurrentHsvColor.H, 6);
        Type(Channel("saturation"), "80"); h.Press("Enter");
        Type(Channel("value"), "0"); h.Press("Enter");
        Assert.Equal(210, Probe.Picker.CurrentHsvColor.H, 6);
        Assert.Equal(.8, Probe.Picker.CurrentHsvColor.S, 6);
        Type(Channel("value"), "100"); h.Press("Enter");
        Assert.Equal(210, Probe.Picker.CurrentHsvColor.H, 6);
        Assert.Equal(.8, Probe.Picker.CurrentHsvColor.S, 6);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Disabled_and_read_only_pickers_reject_color_changes(bool readOnly)
    {
        Show(probe => { probe.ReadOnly = readOnly; probe.Disabled = !readOnly; });
        var before = Probe.Value;
        h.Focus(h.One("ColorPickerSpectrum")); h.Press("ArrowRight");
        h.Focus(Hex); h.Press("a", KeyModifiers.Control); h.Type("#ff0000"); h.Press("Enter");
        Assert.Equal(before, Probe.Value);
        Assert.Empty(Probe.Changes);
        Assert.Empty(Probe.Commits);
        Assert.Equal(readOnly, Hex.Attributes["readonly"]);
        Assert.Equal(!readOnly, Hex.Attributes["disabled"]);
    }

    [Fact]
    public void A_Field_names_the_picker_and_internal_inputs_keep_independent_ids_and_names()
    {
        Show(probe => probe.InField = true);
        Assert.Equal(h.One("Field").Attributes["labeled-by"], h.One("ColorPicker").Attributes["labeled-by"]);
        var ids = h.All("TextFieldInput").Select(element => element.Attributes["id"]).ToArray();
        Assert.Equal(ids.Length, ids.Distinct().Count());
        Assert.Equal("Red", Channel("red").Attributes["accessible-name"]);
        Probe.Disabled = true; Probe.Refresh(); h.Frame();
        Assert.True(h.One("ColorPickerSpectrum").IsDisabled);
        Assert.All(h.All("TextFieldInput"), element => Assert.True(element.IsDisabled));
    }

    [Fact]
    public void Hidden_sections_have_no_focusable_children()
    {
        Show(probe => { probe.Visuals = false; probe.Inputs = false; probe.Hex = false; });
        Assert.Empty(h.All("ColorPickerSpectrum"));
        Assert.Empty(h.All("TextFieldInput"));
        Assert.Empty(h.All("Select"));
        h.Press("Tab"); Assert.Null(h.Focused);
    }
}
