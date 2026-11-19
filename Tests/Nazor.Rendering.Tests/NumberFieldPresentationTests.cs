using Nazor.Controls;
using Nazor.Graphics;
using Nazor.Input;
using Nazor.Style;
using Nazor.Testing;
using Nazor.Themes.NazorLeaf;

namespace Nazor.Rendering.Tests;

public sealed class NumberFieldPresentationPage : ComponentBase
{
    [Parameter] public SpinButtonPlacement Placement { get; set; }
    [Parameter] public bool ReadOnly { get; set; }
    [Parameter] public bool Disabled { get; set; }
    public NumberField Field { get; private set; } = null!;
    public int OutsideClicks { get; private set; }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "Container");
        builder.AddAttribute(1, "style", "padding: 24px; display: flex; flex-direction: column; gap: 20px;");
        builder.OpenComponent<NumberField>(10);
        builder.AddComponentParameter(11, nameof(NumberField.Id), "quantity");
        builder.AddComponentParameter(12, nameof(NumberField.Value), (double?)1);
        builder.AddComponentParameter(13, nameof(NumberField.Minimum), 0d);
        builder.AddComponentParameter(14, nameof(NumberField.Maximum), 2d);
        builder.AddComponentParameter(15, nameof(NumberField.SpinButtons), Placement);
        builder.AddComponentParameter(16, nameof(NumberField.ReadOnly), ReadOnly);
        builder.AddComponentParameter(17, nameof(NumberField.Disabled), Disabled);
        builder.AddComponentParameter(18, nameof(NumberField.Style), "width: 240px;");
        builder.AddComponentReferenceCapture(19, component => Field = (NumberField)component);
        builder.CloseComponent();
        builder.OpenComponent<Button>(20);
        builder.AddComponentParameter(21, nameof(Button.Id), "outside");
        builder.AddComponentParameter(22, nameof(Button.OnClick), EventCallback.Factory.Create<PointerEventArgs>(this, () => OutsideClicks++));
        builder.AddComponentParameter(23, nameof(Button.ChildContent), (RenderFragment)(text => text.AddContent(0, "Outside")));
        builder.CloseComponent();
        builder.CloseElement();
    }
}

public sealed class NumberFieldPresentationTests
{
    [Theory]
    [InlineData("basic")]
    [InlineData("fluent")]
    [InlineData("leaf")]
    public void Inline_and_compact_modes_use_the_theme_layout_without_stealing_editor_focus(string theme)
    {
        using var harness = new RenderHarness();
        using var input = new InputManager(harness.Host.Renderer, harness.Scene);
        harness.Scene.Styles.Theme = Theme(theme);
        harness.Scene.Styles.Preferences = new MediaPreferences(ColorScheme.Light, ReducedMotion: true);
        var root = harness.Host.Render<NumberFieldPresentationPage>();
        var page = (NumberFieldPresentationPage)root.Component;
        Frame();
        var editor = root.Elements("NumberFieldInput").Single();
        input.Focus(editor, visible: true);
        Frame();
        var compact = root.Elements("NumberFieldSpinButtons").Single();
        var toggle = root.Elements("NumberFieldSpinToggle").Single();
        var popup = root.Elements("NumberFieldSpinPopup").Single();
        Assert.Equal(theme != "fluent", harness.Scene.IsRendered(compact));
        Assert.Equal(theme == "fluent", harness.Scene.IsRendered(toggle));
        Assert.Equal(theme == "fluent", harness.Scene.IsRendered(popup));
        Assert.Equal(theme == "fluent", harness.Scene.Overlays.Contains(popup));
        var increment = root.Elements("NumberFieldIncrement").Single(harness.Scene.IsRendered);
        Click(increment);
        Assert.Equal(2d, page.Field.CurrentValue);
        Assert.Same(editor, input.Focused);
        Assert.True(increment.IsDisabled);
        var decrement = root.Elements("NumberFieldDecrement").Single(harness.Scene.IsRendered);
        var decrementBounds = harness.Scene.GetBounds(decrement);
        input.PointerPressed(new Point(decrementBounds.X + decrementBounds.Width / 2, decrementBounds.Y + decrementBounds.Height / 2),
            PointerButton.Primary, KeyModifiers.None);
        Frame();
        Assert.True(decrement.States.HasFlag(ElementStates.Pressed));
        Assert.Same(editor, input.Focused);
        Assert.Equal(1d, page.Field.CurrentValue);
        input.PointerCanceled();
        Frame();
        Assert.False(decrement.States.HasFlag(ElementStates.Pressed));
        Assert.Same(editor, input.Focused);
        Click(increment);
        Assert.Equal(2d, page.Field.CurrentValue);

        if (theme == "fluent")
        {
            Key("Escape");
            Assert.Empty(harness.Scene.Overlays);
            Assert.Same(editor, input.Focused);
            Key("ArrowDown");
            Assert.Empty(harness.Scene.Overlays);
            Assert.Equal(1d, page.Field.CurrentValue);
            Click(toggle);
            Assert.Contains(root.Elements("NumberFieldSpinPopup").Single(), harness.Scene.Overlays);
        }

        Click(root.Elements("Button").Single(button => button.Id == "outside"));
        Assert.Empty(harness.Scene.Overlays);
        if (theme == "fluent") Click(root.Elements("Button").Single(button => button.Id == "outside"));
        Assert.Equal(1, page.OutsideClicks);
        input.Focus(editor, visible: true);
        Frame();
        Assert.Equal(theme == "fluent", harness.Scene.Overlays.Any());

        harness.Host.Renderer.RemoveRootComponent(root);
        root = harness.Host.Render<NumberFieldPresentationPage>(ParameterView.FromDictionary(new Dictionary<string, object?>
        {
            [nameof(NumberFieldPresentationPage.Placement)] = SpinButtonPlacement.Inline,
        }));
        Frame();
        Assert.Empty(harness.Scene.Overlays);
        var inputBounds = harness.Scene.GetBounds(root.Elements("NumberFieldInput").Single());
        var up = harness.Scene.GetBounds(root.Elements("NumberFieldIncrement").Single());
        var down = harness.Scene.GetBounds(root.Elements("NumberFieldDecrement").Single());
        if (theme == "fluent")
        {
            Assert.True(inputBounds.Right <= up.X);
            Assert.True(up.Right <= down.X);
        }
        else
        {
            Assert.True(down.Right <= inputBounds.X);
            Assert.True(inputBounds.Right <= up.X);
        }

        harness.Host.Renderer.RemoveRootComponent(root);
        root = harness.Host.Render<NumberFieldPresentationPage>(ParameterView.FromDictionary(new Dictionary<string, object?>
        {
            [nameof(NumberFieldPresentationPage.Placement)] = SpinButtonPlacement.Hidden,
        }));
        Frame();
        input.Focus(root.Elements("NumberFieldInput").Single(), visible: true);
        Frame();
        Assert.Empty(root.Elements("NumberFieldIncrement"));
        Assert.Empty(root.Elements("NumberFieldDecrement"));
        Assert.Empty(root.Elements("NumberFieldSpinToggle"));
        Assert.Empty(harness.Scene.Overlays);

        void Frame()
        {
            harness.Host.RunPending();
            harness.Render(400, 240, 1);
            harness.Host.RunPending();
            harness.Render(400, 240, 1);
        }
        void Click(ElementNode element)
        {
            var bounds = harness.Scene.GetBounds(element);
            var point = new Point(bounds.X + bounds.Width / 2, bounds.Y + bounds.Height / 2);
            input.PointerPressed(point, PointerButton.Primary, KeyModifiers.None);
            input.PointerReleased(point, PointerButton.Primary, KeyModifiers.None);
            Frame();
        }
        void Key(string key)
        {
            input.KeyDown(key, KeyModifiers.None, false);
            input.KeyUp(key, KeyModifiers.None);
            Frame();
        }
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public void Read_only_or_disabled_fields_do_not_open_compact_flyouts(bool readOnly, bool disabled)
    {
        using var harness = new RenderHarness();
        using var input = new InputManager(harness.Host.Renderer, harness.Scene);
        harness.Scene.Styles.Theme = Theme("fluent");
        var root = harness.Host.Render<NumberFieldPresentationPage>(ParameterView.FromDictionary(new Dictionary<string, object?>
        {
            [nameof(NumberFieldPresentationPage.ReadOnly)] = readOnly,
            [nameof(NumberFieldPresentationPage.Disabled)] = disabled,
        }));
        harness.Render(400, 240, 1);
        input.Focus(root.Elements("NumberFieldInput").Single(), visible: true);
        harness.Host.RunPending();
        harness.Render(400, 240, 1);
        Assert.Empty(harness.Scene.Overlays);
    }

    private static StyleSheet Theme(string theme) => theme switch
    {
        "fluent" => Nazor.Themes.Fluent.Themes.Fluent,
        "leaf" => NazorLeafTheme.Theme,
        _ => Nazor.Themes.Themes.Basic,
    };
}
