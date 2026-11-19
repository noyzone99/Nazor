using Nazor.Controls;
using Nazor.Graphics;
using Nazor.Input;
using Nazor.Nss;
using Nazor.Rendering;
using Nazor.Style;
using Nazor.Testing;

namespace Nazor.Rendering.Tests;

public sealed class TitleBarInputTests
{
    public static IEnumerable<object[]> Fields() =>
        from theme in new[] { "basic", "fluent", "leaf", "none" }
        from kind in new[] { "TextField", "TextArea", "PasswordField", "NumberField", "SearchField", "SearchSuggestions", "ComboBox", "Combobox", "MaskedField", "TokenField", "DatePicker", "TimePicker", "DateTimePicker" }
        select new object[] { theme, kind };

    [Theory]
    [MemberData(nameof(Fields))]
    public void Field_padding_focuses_and_keeps_the_editor_without_a_keyboard_focus_ring(string theme, string kind)
    {
        using var h = new RenderHarness();
        using var input = new InputManager(h.Host.Renderer, h.Scene);
        h.Scene.Styles.Theme = Theme(theme);
        h.Scene.Styles.Application = NssCompiler.Compile("caption-fields", "#field { display: flex; align-items: center; width: 280px; min-height: 40px; padding: 8px; } #field-input { display: block; flex: 1; min-width: 0; }").StyleSheet;
        var root = h.Host.Render<TitleBar>(ParameterView.FromDictionary(new Dictionary<string, object?>
        {
            [nameof(Control.Style)] = "display: flex; height: 300px;",
            [nameof(TitleBar.ChildContent)] = (RenderFragment)(b =>
            {
                b.OpenComponent<FieldFocusProbe>(0);
                b.AddComponentParameter(1, nameof(FieldFocusProbe.Kind), kind);
                b.CloseComponent();
            }),
        }));
        h.Render(800, 500, 1);
        var frame = kind == "DateTimePicker" ? root.Single("DatePicker") : root.Elements().Single(e => e.Id == "field");
        var editor = root.Elements().Single(e => e.Id == (kind == "DateTimePicker" ? "field-date-input" : "field-input"));
        var bounds = h.Scene.GetBounds(frame);
        foreach (var point in new[] { new Point(bounds.X + 2, bounds.Y + bounds.Height / 2), new Point(bounds.X + bounds.Width / 2, bounds.Y + 2), new Point(bounds.X + bounds.Width / 2, bounds.Bottom - 2) })
        {
            Assert.Equal(WindowRegion.Client, input.RegionAt(point));
            for (var repeat = 0; repeat < 2; repeat++)
            {
                input.PointerPressed(point, PointerButton.Primary, KeyModifiers.None);
                input.PointerReleased(point, PointerButton.Primary, KeyModifiers.None);
                h.Host.RunPending(); h.Scene.Update();
                Assert.Same(editor, input.Focused);
                Assert.Equal(ElementStates.None, editor.States & ElementStates.FocusVisible);
            }
        }
        var action = root.Elements().Single(e => e.Id == "action");
        var actionBounds = h.Scene.GetBounds(action);
        var actionPoint = new Point(actionBounds.X + actionBounds.Width / 2, actionBounds.Y + actionBounds.Height / 2);
        input.PointerPressed(actionPoint, PointerButton.Primary, KeyModifiers.None);
        input.PointerReleased(actionPoint, PointerButton.Primary, KeyModifiers.None);
        Assert.Same(action, input.Focused);
    }

    [Theory]
    [InlineData("onpointerdown")]
    [InlineData("onpointerup")]
    [InlineData("onpointermove")]
    [InlineData("onpointerenter")]
    [InlineData("onpointerleave")]
    [InlineData("oncontextmenu")]
    [InlineData("ondblclick")]
    [InlineData("onwheel")]
    [InlineData("__default_onpointerdown")]
    [InlineData("__internal_onpointerdown#custom")]
    [InlineData("__internal_preventDefault_onpointerdown")]
    [InlineData("__internal_stopPropagation_onpointerdown")]
    [InlineData("tooltip")]
    public void Custom_pointer_handlers_and_tooltips_keep_their_whole_surface_in_the_client(string attribute)
    {
        using var h = new RenderHarness();
        using var input = new InputManager(h.Host.Renderer, h.Scene);
        var root = h.Host.Render<TitleBar>(ParameterView.FromDictionary(new Dictionary<string, object?>
        {
            [nameof(Control.Style)] = "display: flex; height: 64px;",
            [nameof(TitleBar.ChildContent)] = (RenderFragment)(b =>
            {
                b.OpenElement(0, "CustomControl");
                b.AddAttribute(1, "style", "width: 240px; height: 32px;");
                b.AddAttribute(2, attribute, attribute == "tooltip" ? "Description" : attribute.Contains("Default_", StringComparison.Ordinal) || attribute.Contains("Propagation_", StringComparison.Ordinal)
                    ? true : attribute == "onwheel" ? EventCallback.Factory.Create<WheelEventArgs>(h.Host, _ => { }) : EventCallback.Factory.Create<PointerEventArgs>(h.Host, _ => { }));
                b.CloseElement();
            }),
        }));
        h.Render(800, 200, 1);
        var bounds = h.Scene.GetBounds(root.Single("CustomControl"));
        Assert.Equal(WindowRegion.Client, input.RegionAt(new Point(bounds.X + 2, bounds.Y + 2)));
        Assert.Equal(WindowRegion.Caption, input.RegionAt(new Point(bounds.Right + 20, bounds.Y + 2)));
    }

    [Fact]
    public void Explicit_pointer_focus_capture_and_snap_regions_keep_their_own_behavior()
    {
        using var h = new RenderHarness();
        using var input = new InputManager(h.Host.Renderer, h.Scene);
        ElementNode? external = null;
        var root = h.Host.Render<TitleBar>(ParameterView.FromDictionary(new Dictionary<string, object?>
        {
            [nameof(Control.Style)] = "display: flex; height: 64px;",
            [nameof(TitleBar.ChildContent)] = (RenderFragment)(b =>
            {
                b.OpenElement(0, "CustomControl");
                b.AddAttribute(1, "style", "width: 100px; height: 32px;");
                b.AddAttribute(2, "onpointerdown", EventCallback.Factory.Create<PointerEventArgs>(h.Host, _ => input.Focus(external, visible: false)));
                b.CloseElement();
                b.OpenElement(10, "External");
                b.AddAttribute(11, "focusable", true);
                b.AddAttribute(12, "style", "width: 100px; height: 32px;");
                b.CloseElement();
                b.OpenElement(20, "Snap");
                b.AddAttribute(21, "snap-layout-area", true);
                b.AddAttribute(22, "style", "width: 100px; height: 32px;");
                b.AddAttribute(23, "onclick", EventCallback.Factory.Create<PointerEventArgs>(h.Host, _ => { }));
                b.CloseElement();
            }),
        }));
        h.Render(800, 200, 1);
        external = root.Single("External");
        var target = root.Single("CustomControl");
        var bounds = h.Scene.GetBounds(target);
        input.PointerPressed(new Point(bounds.X + 5, bounds.Y + 5), PointerButton.Primary, KeyModifiers.None);
        Assert.Same(external, input.Focused);
        Assert.True(input.CapturePointer(target));
        Assert.Equal(WindowRegion.Client, input.RegionAt(new Point(700, 20)));
        input.PointerReleased(new Point(700, 20), PointerButton.Primary, KeyModifiers.None);
        input.ReleasePointerCapture(target);
        Assert.Equal(WindowRegion.Caption, input.RegionAt(new Point(700, 20)));
        var snap = h.Scene.GetBounds(root.Single("Snap"));
        Assert.Equal(WindowRegion.MaximizeButton, input.RegionAt(new Point(snap.X + 5, snap.Y + 5)));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Selectable_text_and_scrollable_custom_content_keep_client_input(bool scrollable)
    {
        using var h = new RenderHarness();
        using var input = new InputManager(h.Host.Renderer, h.Scene);
        var root = h.Host.Render<TitleBar>(ParameterView.FromDictionary(new Dictionary<string, object?>
        {
            [nameof(Control.Style)] = "display: flex; height: 64px;",
            [nameof(TitleBar.ChildContent)] = (RenderFragment)(b =>
            {
                b.OpenElement(0, "CustomContent");
                b.AddAttribute(1, "style", scrollable ? "width: 240px; height: 32px; overflow-y: auto;" : "width: 240px; height: 32px;");
                b.AddAttribute(2, "selectable", !scrollable);
                b.OpenElement(10, "Content");
                b.AddAttribute(11, "style", scrollable ? "height: 200px;" : "height: 24px;");
                b.AddContent(12, "Caption content");
                b.CloseElement(); b.CloseElement();
            }),
        }));
        h.Render(800, 200, 1);
        var bounds = h.Scene.GetBounds(root.Single("CustomContent"));
        Assert.Equal(WindowRegion.Client, input.RegionAt(new Point(bounds.X + 2, bounds.Y + 2)));
        Assert.Equal(WindowRegion.Caption, input.RegionAt(new Point(bounds.Right + 20, bounds.Y + 2)));
    }

    private static StyleSheet Theme(string theme) => theme switch
    {
        "fluent" => Nazor.Themes.Fluent.Themes.Fluent,
        "leaf" => Nazor.Themes.NazorLeaf.NazorLeafTheme.Theme,
        "none" => Nazor.Themes.Themes.None,
        _ => Nazor.Themes.Themes.Basic,
    };
}
