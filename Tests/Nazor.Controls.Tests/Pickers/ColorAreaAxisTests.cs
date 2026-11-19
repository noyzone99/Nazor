using Nazor.Controls.Primitives;

namespace Nazor.Controls.Tests.Pickers;

[Collection("Controls")]
public sealed class ColorAreaAxisTests
{
    public static IEnumerable<object[]> Mappings =>
    [
        [ColorSpectrumComponents.SaturationValue, new HsvColor(180, .25, .25, .25)],
        [ColorSpectrumComponents.HueSaturation, new HsvColor(90, .25, .6, .25)],
        [ColorSpectrumComponents.HueValue, new HsvColor(90, .4, .25, .25)],
        [ColorSpectrumComponents.SaturationHue, new HsvColor(270, .75, .6, .25)],
        [ColorSpectrumComponents.ValueHue, new HsvColor(270, .4, .75, .25)],
        [ColorSpectrumComponents.ValueSaturation, new HsvColor(180, .75, .75, .25)],
    ];

    [Theory]
    [MemberData(nameof(Mappings))]
    public void Each_plane_maps_the_correct_axes_and_preserves_the_remaining_channel_and_alpha(ColorSpectrumComponents components, HsvColor expected)
    {
        var actual = ColorArea.MapPoint(new HsvColor(180, .4, .6, .25), components, 35, 95, (10, 20, 100, 100));
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void The_original_SV_overload_and_zero_sized_mapping_remain_consistent()
    {
        var initial = new HsvColor(180, .4, .6, .25);
        var actual = ColorArea.MapPoint(initial, ColorSpectrumComponents.SaturationValue, 35, 95, (10, 20, 100, 100));
        Assert.Equal(ColorArea.MapPoint(35, 95, (10, 20, 100, 100)), (actual.S, actual.V));
        Assert.Equal(new HsvColor(180, 0, 0, .25), ColorArea.MapPoint(initial, ColorSpectrumComponents.SaturationValue, 0, 0, (0, 0, 0, 0)));
        Assert.Throws<ArgumentOutOfRangeException>(() => ColorArea.MapPoint(initial, (ColorSpectrumComponents)99, 0, 0, (0, 0, 1, 1)));
    }

    public static IEnumerable<object[]> KeyboardMappings =>
    [
        [ColorSpectrumComponents.SaturationValue, ColorChannel.Saturation, 41d, ColorChannel.Value, 59d],
        [ColorSpectrumComponents.HueSaturation, ColorChannel.Hue, 181d, ColorChannel.Saturation, 39d],
        [ColorSpectrumComponents.HueValue, ColorChannel.Hue, 181d, ColorChannel.Value, 59d],
        [ColorSpectrumComponents.SaturationHue, ColorChannel.Saturation, 39d, ColorChannel.Hue, 181d],
        [ColorSpectrumComponents.ValueHue, ColorChannel.Value, 59d, ColorChannel.Hue, 181d],
        [ColorSpectrumComponents.ValueSaturation, ColorChannel.Value, 59d, ColorChannel.Saturation, 41d],
    ];

    [Theory]
    [MemberData(nameof(KeyboardMappings))]
    public void Keyboard_motion_follows_screen_direction_and_reports_one_commit_per_action(ColorSpectrumComponents components,
        ColorChannel horizontal, double afterRight, ColorChannel vertical, double afterDown)
    {
        using var h = new ControlHarness();
        h.Render<AreaProbe>();
        var probe = h.Component<AreaProbe>();
        probe.Components = components;
        probe.Refresh(); h.Frame();
        h.Focus(h.One("ColorArea"));
        h.Press("ArrowRight");
        Assert.Equal(afterRight, ColorSlider.Read(probe.Value, horizontal), 6);
        h.Press("ArrowDown");
        Assert.Equal(afterDown, ColorSlider.Read(probe.Value, vertical), 6);
        Assert.Equal(2, probe.Previews);
        Assert.Equal(2, probe.Commits);
        Assert.Equal(.25, probe.Value.A);
        Assert.Equal(ColorSlider.RangeOf(horizontal).Maximum, h.One("ColorArea").Attributes["range-max"]);
        Assert.Equal(horizontal.ToString().ToLowerInvariant(), h.One("ColorArea").Attributes["x-channel"]);
        Assert.Equal(vertical.ToString().ToLowerInvariant(), h.One("ColorArea").Attributes["y-channel"]);
        Assert.DoesNotContain("ColorAreaAxesValue", h.One("ColorArea").Attributes["value-text"]?.ToString() ?? "");
    }

    private sealed class AreaProbe : ComponentBase
    {
        public ColorSpectrumComponents Components { get; set; }
        public HsvColor Value { get; set; } = new(180, .4, .6, .25);
        public int Previews { get; private set; }
        public int Commits { get; private set; }
        public void Refresh() => StateHasChanged();

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenComponent<ColorArea>(0);
            builder.AddComponentParameter(1, nameof(ColorArea.Components), Components);
            builder.AddComponentParameter(2, nameof(ColorArea.Color), Value);
            builder.AddComponentParameter(3, nameof(ColorArea.ColorChanged), EventCallback.Factory.Create<HsvColor>(this, next => { Value = next; Previews++; }));
            builder.AddComponentParameter(4, nameof(ColorArea.OnChange), EventCallback.Factory.Create<HsvColor>(this, _ => Commits++));
            builder.AddComponentParameter(5, nameof(ColorArea.Style), "width: 100px; height: 100px");
            builder.CloseComponent();
        }
    }
}
