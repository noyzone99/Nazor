using Nazor.Controls;
using Nazor.Graphics;
using Nazor.Input;
using Nazor.Style;
using Nazor.Testing;
using Nazor.Themes.NazorLeaf;

namespace Nazor.Rendering.Tests;

public sealed class TimePickerSeedPage : ComponentBase
{
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenComponent<Container>(0);
        builder.AddComponentParameter(1, nameof(Container.Style), "display: flex; flex-direction: column; align-items: flex-start; padding: 24px;");
        builder.AddComponentParameter(2, nameof(Container.ChildContent), (RenderFragment)(content =>
        {
            content.OpenComponent<TimePicker>(0);
            content.AddComponentParameter(1, nameof(TimePicker.IsOpen), true);
            content.AddComponentParameter(2, nameof(TimePicker.Value), new TimeOnly(9, 30));
            content.AddComponentParameter(3, nameof(TimePicker.Use24Hour), true);
            content.AddComponentParameter(4, nameof(TimePicker.MinuteIncrement), 15);
            content.CloseComponent();
        }));
        builder.CloseComponent();
    }
}

public sealed class TimePickerSeedRenderingTests
{
    [Theory]
    [InlineData(1f)]
    [InlineData(1.5f)]
    [InlineData(2f)]
    public void Selected_time_seeds_have_an_inset_and_align_with_the_centered_values(float scale)
    {
        using var harness = new RenderHarness();
        using var input = new InputManager(harness.Host.Renderer, harness.Scene);
        harness.Scene.Styles.Theme = NazorLeafTheme.Theme;
        harness.Scene.Styles.Preferences = new MediaPreferences(ColorScheme.Light, ReducedMotion: true);
        var root = harness.Host.Render<TimePickerSeedPage>();
        harness.Render(360, 320, scale);
        harness.Host.RunPending();
        harness.Render(360, 320, scale);
        var selected = root.Elements("TimePickerOption").Where(option => option.Attributes.GetValueOrDefault("selected") is true).ToArray();
        Assert.Equal(2, selected.Length);
        foreach (var option in selected) input.BringIntoView(option);
        harness.Render(360, 320, scale);
        var commands = harness.Scene.BuildDisplayList().Commands;

        foreach (var option in selected)
        {
            var row = harness.Scene.GetBounds(option);
            var seed = harness.Scene.Styles.GetGeneratedElement(option, PseudoElement.Before)!;
            var marker = harness.Scene.GetBounds(seed);
            Assert.InRange(marker.X - row.X, 3 - 1 / scale, 3 + 1 / scale);
            Assert.InRange(MathF.Abs(marker.Y + marker.Height / 2 - row.Y - row.Height / 2), 0, 1 / scale);
            var text = commands.OfType<DrawText>().Single(drawing => drawing.Layout.Text == option.Text());
            var origin = harness.Scene.GetTransform(option).Transform(text.Origin);
            var lineCenter = origin.Y + text.Layout.Lines[0].Height / 2;
            Assert.InRange(MathF.Abs(lineCenter - row.Y - row.Height / 2), 0, 1 / scale);
        }
    }
}
