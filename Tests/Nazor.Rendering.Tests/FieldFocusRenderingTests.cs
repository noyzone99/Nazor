using Nazor.Controls;
using Nazor.Controls.Primitives;
using Nazor.Input;
using Nazor.Style;
using Nazor.Testing;
using Nazor.Themes.NazorLeaf;

namespace Nazor.Rendering.Tests;

public sealed class FieldFocusProbe : ComponentBase
{
    [Parameter] public string Kind { get; set; } = "TextField";
    [Parameter] public FieldAppearance Appearance { get; set; }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        var type = Kind switch
        {
            "TextField" => typeof(TextField),
            "TextArea" => typeof(TextArea),
            "PasswordField" => typeof(PasswordField),
            "NumberField" => typeof(NumberField),
            "SearchField" or "SearchSuggestions" => typeof(SearchField<string>),
            "ComboBox" => typeof(ComboBox<string>),
            "Combobox" => typeof(Combobox<string>),
            "MaskedField" => typeof(MaskedField),
            "TokenField" => typeof(TokenField<string>),
            "DatePicker" => typeof(DatePicker),
            "TimePicker" => typeof(TimePicker),
            "DateTimePicker" => typeof(DateTimePicker),
            _ => throw new ArgumentOutOfRangeException(nameof(Kind)),
        };
        builder.OpenElement(0, "Container");
        builder.AddAttribute(1, "style", "padding: 12px; display: flex; flex-direction: column; gap: 12px");
        builder.OpenComponent(10, type);
        builder.AddComponentParameter(11, nameof(Control.Id), "field");
        if (type != typeof(Combobox<string>)) builder.AddComponentParameter(12, "Appearance", Appearance);
        if (type == typeof(MaskedField)) builder.AddComponentParameter(13, nameof(MaskedField.Mask), "0000");
        if (Kind == "SearchSuggestions") builder.AddComponentParameter(14, nameof(SearchField<string>.Suggestions), new[] { "Rome" });
        builder.CloseComponent();
        builder.OpenComponent<EditableText>(20);
        builder.AddComponentParameter(21, nameof(Control.Id), "standalone");
        builder.CloseComponent();
        builder.OpenComponent<PinField>(30);
        builder.AddComponentParameter(31, nameof(Control.Id), "pin");
        builder.AddComponentParameter(32, nameof(PinField.Length), 1);
        builder.CloseComponent();
        builder.OpenComponent<TextField>(40);
        builder.AddComponentParameter(41, nameof(Control.Id), "with-action");
        builder.AddComponentParameter(42, nameof(TextField.Trailing), (RenderFragment)(trailing =>
        {
            trailing.OpenComponent<Button>(0);
            trailing.AddComponentParameter(1, nameof(Control.Id), "action");
            trailing.AddComponentParameter(2, nameof(Button.ChildContent), (RenderFragment)(text => text.AddContent(0, "Action")));
            trailing.CloseComponent();
        }));
        builder.CloseComponent();
        builder.CloseElement();
    }
}

public sealed class FieldFocusRenderingTests
{
    public static IEnumerable<object[]> Fields()
    {
        string[] kinds = ["TextField", "TextArea", "PasswordField", "NumberField", "SearchField", "SearchSuggestions", "ComboBox", "Combobox", "MaskedField", "TokenField", "DatePicker", "TimePicker", "DateTimePicker"];
        foreach (var theme in new[] { "basic", "fluent", "leaf" })
        foreach (var kind in kinds)
        {
            yield return [theme, kind, FieldAppearance.Outline, false];
            yield return [theme, kind, FieldAppearance.Outline, true];
            if (kind is "TextField" or "DateTimePicker")
            foreach (var appearance in new[] { FieldAppearance.Plain, FieldAppearance.Filled, FieldAppearance.Underline })
            foreach (var forcedColors in new[] { false, true })
                yield return [theme, kind, appearance, forcedColors];
        }
    }

    [Theory]
    [MemberData(nameof(Fields))]
    public void Tab_focus_is_drawn_on_the_field_frame_instead_of_its_editor(string theme, string kind, FieldAppearance appearance, bool forcedColors)
    {
        using var harness = new RenderHarness();
        using var input = new InputManager(harness.Host.Renderer, harness.Scene);
        harness.Scene.Styles.Theme = Theme(theme);
        harness.Scene.Styles.Preferences = new MediaPreferences(ColorScheme.Light, ReducedMotion: true, ForcedColors: forcedColors);
        var root = harness.Host.Render<FieldFocusProbe>(ParameterView.FromDictionary(new Dictionary<string, object?>
        {
            [nameof(FieldFocusProbe.Kind)] = kind,
            [nameof(FieldFocusProbe.Appearance)] = appearance,
        }));
        harness.Render(420, 420, 1);
        var frame = root.Elements(kind == "SearchSuggestions" ? "SearchField" : kind).Single(e => e.Id == "field");
        var before = harness.Scene.GetStyle(frame);

        Tab();
        AssertFieldFocus();
        if (kind == "DateTimePicker")
        {
            for (var i = 0; i < 5 && input.Focused?.Name != "TimePickerInput"; i++) Tab();
            Assert.Equal("TimePickerInput", input.Focused?.Name);
            AssertFieldFocus();
        }

        void Tab()
        {
            input.KeyDown("Tab", KeyModifiers.None, false);
            input.KeyUp("Tab", KeyModifiers.None);
            harness.Render(420, 420, 1);
        }

        void AssertFieldFocus()
        {
            Assert.NotNull(input.Focused);
            Assert.EndsWith("Input", input.Focused.Name);
            Assert.True(input.Focused.States.HasFlag(ElementStates.FocusVisible));
            Assert.Equal(BorderStyle.None, harness.Scene.GetStyle(input.Focused).Get(StyleProperties.OutlineStyle));
            Assert.True(frame.States.HasFlag(ElementStates.FocusWithin));
            var focused = harness.Scene.GetStyle(frame);
            Assert.True(focused.Get(StyleProperties.OutlineStyle) != BorderStyle.None ||
                !focused.Get(StyleProperties.BoxShadow).Equals(before.Get(StyleProperties.BoxShadow)) ||
                focused.Get(StyleProperties.BorderBottomColor) != before.Get(StyleProperties.BorderBottomColor),
                $"No frame focus cue for {theme}/{kind}/{appearance}, forced colors {forcedColors}.");
            if (kind == "DateTimePicker")
                Assert.Equal(BorderStyle.None, harness.Scene.GetStyle(input.Focused.ParentElement!).Get(StyleProperties.OutlineStyle));
        }
    }

    [Theory]
    [InlineData("basic")]
    [InlineData("fluent")]
    [InlineData("leaf")]
    public void Standalone_editors_pin_cells_and_field_actions_keep_their_keyboard_focus_cues(string theme)
    {
        using var harness = new RenderHarness();
        using var input = new InputManager(harness.Host.Renderer, harness.Scene);
        harness.Scene.Styles.Theme = Theme(theme);
        harness.Scene.Styles.Preferences = new MediaPreferences(ColorScheme.Light, ReducedMotion: true);
        var root = harness.Host.Render<FieldFocusProbe>();
        harness.Render(420, 420, 1);
        var seen = new HashSet<string>();
        for (var i = 0; i < 8 && seen.Count < 3; i++)
        {
            input.KeyDown("Tab", KeyModifiers.None, false);
            input.KeyUp("Tab", KeyModifiers.None);
            harness.Render(420, 420, 1);
            var target = input.Focused!;
            if (target.Id is not ("standalone" or "action") && target.Name != "PinFieldCell") continue;
            Assert.True(target.States.HasFlag(ElementStates.FocusVisible));
            var style = harness.Scene.GetStyle(target);
            Assert.True(style.Get(StyleProperties.OutlineStyle) != BorderStyle.None || style.Get(StyleProperties.BoxShadow).Items.Count > 1);
            seen.Add(target.Name);
        }
        Assert.Equal(3, seen.Count);
        Assert.Contains("EditableText", seen);
        Assert.Contains("PinFieldCell", seen);
        Assert.Contains("Button", seen);
    }

    private static StyleSheet Theme(string theme) => theme switch
    {
        "basic" => Nazor.Themes.Themes.Basic,
        "fluent" => Nazor.Themes.Fluent.Themes.Fluent,
        "leaf" => NazorLeafTheme.Theme,
        _ => throw new ArgumentOutOfRangeException(nameof(theme)),
    };
}
