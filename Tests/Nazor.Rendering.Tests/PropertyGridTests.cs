using System.ComponentModel;
using Nazor.Controls;
using Nazor.Rendering;
using Nazor.Testing;

namespace Nazor.Rendering.Tests;

public sealed class PropertyGridTests
{
    public enum ProbeMode { [Description("Automatic mode")] Automatic, Manual }

    public sealed class ProbeTarget
    {
        [DisplayName("Operating mode")]
        public ProbeMode? Mode { get; set; } = ProbeMode.Automatic;
        [Browsable(false)] public string Hidden { get; set; } = "Hidden";
    }

    [Fact]
    public void Reflected_items_preserve_nullable_enum_values_and_attribute_labels()
    {
        var target = new ProbeTarget();
        var item = Assert.Single(PropertyGridItem.FromObject(target));
        Assert.Equal("Operating mode", item.Text);
        Assert.Equal([ProbeMode.Automatic, ProbeMode.Manual], item.Choices!.Cast<ProbeMode>());
        Assert.Equal("Automatic mode", item.ChoiceText!(item.Getter()));
        item.Setter!(ProbeMode.Manual);
        Assert.Equal(ProbeMode.Manual, target.Mode);
        Assert.Equal("Manual", item.ChoiceText(item.Getter()));
        Assert.Equal("", item.ChoiceText(null));
    }

    [Fact]
    public void Explicit_items_use_the_choice_formatter_without_inspecting_a_target()
    {
        using var harness = new RenderHarness();
        var item = new PropertyGridItem
        {
            Name = "Mode", PropertyType = typeof(ProbeMode), Getter = () => ProbeMode.Automatic,
            Setter = _ => { }, ChoiceText = value => value is ProbeMode.Automatic ? "Explicit label" : "Manual",
        };
        var root = harness.Host.Render<PropertyGrid>(ParameterView.FromDictionary(new Dictionary<string, object?>
        {
            [nameof(PropertyGrid.Items)] = new[] { item },
        }));
        harness.Render(400, 300, 1);
        Assert.Contains("Explicit label", root.Text());
    }
}
