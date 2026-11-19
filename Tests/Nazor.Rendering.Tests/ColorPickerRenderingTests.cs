using Nazor.Controls;
using Nazor.Controls.Primitives;
using Nazor.Graphics;
using Nazor.Input;
using Nazor.Rendering;
using Nazor.Style;
using Nazor.Testing;
using Nazor.Themes.NazorLeaf;
using StyleColor = Nazor.Style.Color;

namespace Nazor.Rendering.Tests;

public sealed class ColorPickerRenderingTests
{
    public static IEnumerable<object[]> Planes =>
        from theme in new[] { "basic", "fluent", "leaf" }
        from scale in new[] { 1f, 1.5f, 2f }
        from components in Enum.GetValues<ColorSpectrumComponents>()
        select new object[] { theme, scale, components };

    [Theory]
    [MemberData(nameof(Planes))]
    public void Every_spectrum_plane_paints_the_HSV_color_at_the_same_positions_used_by_pointer_mapping(string theme, float scale, ColorSpectrumComponents components)
    {
        using var harness = new RenderHarness();
        Configure(harness, theme);
        var root = harness.Host.Render<PickerProbe>(Parameters(components, planeOnly: true));
        var image = harness.Render(400, 370, scale);
        var probe = (PickerProbe)root.Component;
        var box = harness.Scene.GetBounds(root.Single("ColorPickerSpectrum"));
        Assert.True(box.Width > 200 && box.Height >= 200);
        foreach (var xFraction in new[] { .125, .5, .875 })
            foreach (var yFraction in new[] { .125, .5, .875 })
            {
                var x = (int)((box.X + box.Width * xFraction) * scale);
                var y = (int)((box.Y + box.Height * yFraction) * scale);
                var logicalX = (x + .5) / scale;
                var logicalY = (y + .5) / scale;
                var expectedHsv = ColorArea.MapPoint(probe.Picker.CurrentHsvColor, components, logicalX, logicalY,
                    (box.X, box.Y, box.Width, box.Height));
                if (components is ColorSpectrumComponents.HueSaturation or ColorSpectrumComponents.SaturationHue) expectedHsv = expectedHsv with { V = 1 };
                var expected = ColorModel.FromHsv(expectedHsv with { A = 1 });
                var actual = image.GetPixel(x, y);
                Assert.InRange(Math.Abs(expected.R - actual.R), 0, 2);
                Assert.InRange(Math.Abs(expected.G - actual.G), 0, 2);
                Assert.InRange(Math.Abs(expected.B - actual.B), 0, 2);
                Assert.Equal(255, actual.A);
            }
    }

    public static IEnumerable<object[]> Layouts =>
        from theme in new[] { "basic", "fluent", "leaf" }
        from scale in new[] { 1f, 1.5f, 2f }
        from orientation in Enum.GetValues<Orientation>()
        select new object[] { theme, scale, orientation };

    [Theory]
    [MemberData(nameof(Layouts))]
    public void Complete_picker_fields_are_contained_and_slider_thumb_geometry_agrees_with_the_input_axis(string theme, float scale, Orientation orientation)
    {
        using var harness = new RenderHarness();
        Configure(harness, theme);
        var root = harness.Host.Render<PickerProbe>(Parameters(orientation: orientation));
        harness.Render(1000, 850, scale);
        var picker = harness.Scene.GetBounds(root.Single("ColorPicker"));
        foreach (var editor in root.Elements("TextFieldInput"))
        {
            var bounds = harness.Scene.GetBounds(editor);
            Assert.True(bounds.Width > 20 && bounds.Height > 10);
            Assert.InRange(bounds.X, picker.X, picker.Right);
            Assert.InRange(bounds.Right, picker.X, picker.Right);
            Assert.InRange(bounds.Y, picker.Y, picker.Bottom);
            Assert.InRange(bounds.Bottom, picker.Y, picker.Bottom);
        }
        foreach (var name in new[] { "ColorPickerSlider", "ColorPickerAlphaSlider" })
        {
            var slider = root.Single(name);
            var track = harness.Scene.GetBounds(root.Single(name + "Track"));
            var thumb = harness.Scene.GetBounds(root.Single(name + "Thumb"));
            var vertical = orientation == Orientation.Horizontal;
            Assert.Equal(vertical ? "vertical" : "horizontal", slider.Attributes["orientation"]);
            var value = (double)slider.Attributes["range-value"]!;
            var minimum = (double)slider.Attributes["range-min"]!;
            var maximum = (double)slider.Attributes["range-max"]!;
            var fraction = (value - minimum) / (maximum - minimum);
            var expected = vertical ? track.Bottom - track.Height * fraction : track.X + track.Width * fraction;
            var center = vertical ? thumb.Y + thumb.Height / 2 : thumb.X + thumb.Width / 2;
            Assert.InRange(Math.Abs(expected - center), 0, 1 / scale);
        }
        if (theme == "fluent" && orientation == Orientation.Vertical)
        {
            var spectrum = harness.Scene.GetBounds(root.Single("ColorPickerSpectrum"));
            var preview = harness.Scene.GetBounds(root.Single("ColorPickerPreview"));
            Assert.Equal(312f, picker.Width);
            Assert.Equal(256f, spectrum.Height);
            Assert.InRange(Math.Abs(preview.Width - 44), 0, 1 / scale);
            Assert.True(preview.X >= spectrum.Right);
            foreach (var channel in root.Elements("ColorPickerChannel"))
            {
                var children = channel.Children.OfType<ElementNode>().ToArray();
                var editor = children.Single(element => element.Name == "TextField");
                var label = children.Single(element => element.Name == "Label");
                Assert.True(harness.Scene.GetBounds(editor).Right <= harness.Scene.GetBounds(label).X);
            }
        }
    }

    public static IEnumerable<object[]> ThemesAndScales =>
        from theme in new[] { "basic", "fluent", "leaf" }
        from scale in new[] { 1f, 1.5f, 2f }
        select new object[] { theme, scale };

    [Theory]
    [MemberData(nameof(ThemesAndScales))]
    public void Alpha_preview_composites_the_actual_color_over_two_checkerboard_levels(string theme, float scale)
    {
        using var harness = new RenderHarness();
        Configure(harness, theme);
        var parameters = ParameterView.FromDictionary(new Dictionary<string, object?>
        {
            [nameof(PickerProbe.PreviewOnly)] = true,
            [nameof(PickerProbe.Value)] = new StyleColor(255, 0, 0, 128),
        });
        var root = harness.Host.Render<PickerProbe>(parameters);
        var image = harness.Render(400, 300, scale);
        var box = harness.Scene.GetBounds(root.Single("ColorPickerPreviewCurrent"));
        var greens = new List<byte>();
        var centerX = box.X + box.Width / 2;
        var centerY = box.Y + box.Height / 2;
        for (var x = -4; x < 4; x++)
            for (var y = -4; y < 4; y++)
            {
                var pixel = image.GetPixel((int)((centerX + x) * scale), (int)((centerY + y) * scale));
                Assert.InRange(pixel.R, 227, 255);
                Assert.InRange(pixel.G, 99, 128);
                Assert.InRange(Math.Abs(pixel.G - pixel.B), 0, 1);
                greens.Add(pixel.G);
            }
        Assert.True(greens.Max() - greens.Min() >= 20);
    }

    [Fact]
    public void The_representation_select_uses_the_shared_overlay_without_changing_the_picker_or_ancestor_scroll_extent()
    {
        using var harness = new RenderHarness();
        using var input = new InputManager(harness.Host.Renderer, harness.Scene);
        var root = harness.Host.Render<PickerProbe>(Parameters());
        harness.Render(500, 850, 1);
        var before = harness.Scene.GetBounds(root.Single("ColorPicker"));
        input.Focus(root.Single("Select"), visible: true);
        input.KeyDown("Enter", KeyModifiers.None, false);
        harness.Host.RunPending(); harness.Render(500, 850, 1);
        Assert.Single(harness.Scene.Overlays);
        Assert.Equal(before, harness.Scene.GetBounds(root.Single("ColorPicker")));
    }

    private static ParameterView Parameters(ColorSpectrumComponents components = ColorSpectrumComponents.HueSaturation,
        bool planeOnly = false, Orientation orientation = Orientation.Vertical) =>
        ParameterView.FromDictionary(new Dictionary<string, object?>
        {
            [nameof(PickerProbe.Components)] = components,
            [nameof(PickerProbe.PlaneOnly)] = planeOnly,
            [nameof(PickerProbe.Orientation)] = orientation,
        });

    private static void Configure(RenderHarness harness, string theme) => harness.Scene.Styles.Theme = theme switch
    {
        "fluent" => Nazor.Themes.Fluent.Themes.Fluent,
        "leaf" => NazorLeafTheme.Theme,
        _ => Nazor.Themes.Themes.Basic,
    };

    private sealed class PickerProbe : ComponentBase
    {
        [Parameter] public ColorSpectrumComponents Components { get; set; } = ColorSpectrumComponents.HueSaturation;
        [Parameter] public Orientation Orientation { get; set; } = Orientation.Vertical;
        [Parameter] public bool PlaneOnly { get; set; }
        [Parameter] public bool PreviewOnly { get; set; }
        [Parameter] public StyleColor Value { get; set; } = new(92, 122, 153, 128);
        public ColorPicker Picker { get; private set; } = null!;

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenElement(0, "Container");
            builder.AddAttribute(1, "style", "padding: 24px");
            builder.OpenComponent<ColorPicker>(2);
            builder.AddComponentParameter(3, nameof(ColorPicker.Color), Value);
            builder.AddComponentParameter(4, nameof(ColorPicker.SpectrumComponents), Components);
            builder.AddComponentParameter(5, nameof(ColorPicker.Orientation), Orientation);
            builder.AddComponentParameter(6, nameof(ColorPicker.IsAlphaEnabled), true);
            builder.AddComponentParameter(7, nameof(ColorPicker.IsColorSpectrumVisible), !PreviewOnly);
            builder.AddComponentParameter(8, nameof(ColorPicker.IsColorSliderVisible), !PlaneOnly && !PreviewOnly);
            builder.AddComponentParameter(9, nameof(ColorPicker.IsAlphaSliderVisible), !PlaneOnly && !PreviewOnly);
            builder.AddComponentParameter(10, nameof(ColorPicker.IsColorPreviewVisible), !PlaneOnly);
            builder.AddComponentParameter(11, nameof(ColorPicker.IsColorChannelTextInputVisible), !PlaneOnly && !PreviewOnly);
            builder.AddComponentParameter(12, nameof(ColorPicker.IsAlphaTextInputVisible), !PlaneOnly && !PreviewOnly);
            builder.AddComponentParameter(13, nameof(ColorPicker.IsHexInputVisible), !PlaneOnly && !PreviewOnly);
            builder.AddComponentReferenceCapture(14, component => Picker = (ColorPicker)component);
            builder.CloseComponent();
            builder.CloseElement();
        }
    }
}
