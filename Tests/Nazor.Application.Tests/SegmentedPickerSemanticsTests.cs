using Nazor.Accessibility;
using Nazor.Controls;
using Nazor.Graphics;
using Nazor.Graphics.Text;
using Nazor.Input;
using Nazor.Presentation;
using Nazor.Rendering;
using Nazor.Skia;
using Nazor.Testing;

namespace Nazor.Application.Tests;

public sealed class SegmentedPickerSemanticsTests
{
    [Fact]
    public void A_date_picker_exposes_working_expand_collapse_without_committing_its_draft() =>
        Verify<DateOnly, SegmentedDatePicker>(new(2024, 1, 31), new(2024, 2, 29), "month");

    [Fact]
    public void A_time_picker_exposes_working_expand_collapse_without_committing_its_draft() =>
        Verify<TimeOnly, SegmentedTimePicker>(new(9, 30), new(9, 31), "minutes");

    private static void Verify<TValue, TPicker>(TValue value, TValue edited, string columnName)
        where TValue : struct where TPicker : SegmentedPickerBase<TValue>
    {
        using var host = new TestHost();
        using var graphics = new SkiaRenderer(new FontOptions
        {
            PrivateFontFiles = [Path.Combine(AppContext.BaseDirectory, "Fonts", "FiraSans-Regular.ttf")],
            UseSystemFonts = false, DefaultFamily = "Fira Sans",
        });
        using var scene = new Scene(host.Renderer, graphics.Fonts, graphics.Shaper) { Viewport = new Size(800, 800) };
        scene.Styles.Theme = Nazor.Themes.Themes.Basic;
        scene.Transitions.ReducedMotion = true;
        using var input = new InputManager(host.Renderer, scene);
        using var semantics = new SemanticTree(host.Renderer, scene, input);
        var changes = new List<TValue?>();
        var root = host.Render<TPicker>(ParameterView.FromDictionary(new Dictionary<string, object?>
        {
            [nameof(Control.AccessibleName)] = "Appointment",
            [nameof(Control.Style)] = "margin-top: 260px; width: 300px;",
            [nameof(SegmentedPickerBase<TValue>.Value)] = value,
            [nameof(SegmentedPickerBase<TValue>.ValueChanged)] = EventCallback.Factory.Create<TValue?>(new object(), changes.Add),
        }));
        var picker = (TPicker)root.Component;
        Frame();
        var element = root.Single(typeof(TPicker).Name);
        var node = semantics.Find(element)!;
        Assert.Equal(SemanticRole.ComboBox, node.Role);
        Assert.True(semantics.SetExpanded(node, true)); Frame();
        Assert.True(picker.Opened);
        var column = root.Elements("SegmentedPickerColumn").Single(part => Equals(part.Attributes.GetValueOrDefault("column"), columnName));
        var selected = column.Elements().Single(part => part.Attributes.GetValueOrDefault("current") is true);
        input.Focus(selected, visible: true);
        input.KeyDown("ArrowDown", KeyModifiers.None, false); input.KeyUp("ArrowDown", KeyModifiers.None); Frame();
        Assert.Equal(edited, picker.DraftValue);
        Assert.True(semantics.SetExpanded(semantics.Find(element)!, false)); Frame();
        Assert.False(picker.Opened);
        Assert.Equal(value, picker.CurrentValue);
        Assert.Empty(changes);
        Assert.Same(element, input.Focused);

        void Frame() { host.RunPending(); scene.Update(); host.RunPending(); scene.Update(); semantics.Update(); }
    }
}
