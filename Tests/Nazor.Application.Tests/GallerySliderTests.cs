using Nazor.Graphics;
using Nazor.Graphics.Text;
using Nazor.Input;
using Nazor.Presentation;
using Nazor.Rendering;
using Nazor.Samples.Gallery.Examples.Selection;
using Nazor.Skia;
using Nazor.Testing;

namespace Nazor.Application.Tests;

public sealed class GallerySliderTests
{
    [Fact]
    public async Task Gallery_exercises_vertical_ticks_and_independent_continuous_step_and_tick_snapping()
    {
        using var host = new TestHost();
        using var graphics = new SkiaRenderer(new FontOptions { UseSystemFonts = true });
        using var scene = new Scene(host.Renderer, graphics.Fonts, graphics.Shaper) { Viewport = new Size(900, 600) };
        scene.Styles.Theme = Themes.Themes.Basic;
        scene.Transitions.ReducedMotion = true;
        using var input = new InputManager(host.Renderer, scene);
        var root = host.Render<SlidersSample>();
        void Frame() { host.RunPending(); scene.Update(); host.RunPending(); scene.Update(); }
        Frame();
        var slider = root.Single("Slider");
        Assert.True(input.SetRangeValue(slider, 47));
        Frame();
        Assert.Equal(45d, slider.Attributes["range-value"]);

        async Task Choose(string id, string value)
        {
            await host.ClickAsync(root.Elements("Select").Single(element => element.Id == id));
            Frame();
            await host.ClickAsync(root.Elements("SelectOption").Single(element => element.Text() == value));
            Frame();
        }

        await Choose("slider-snapping", "Continuous");
        Assert.True(input.SetRangeValue(slider, 47));
        Frame();
        Assert.Equal(47d, slider.Attributes["range-value"]);
        await Choose("slider-snapping", "Ticks");
        Assert.True(input.SetRangeValue(slider, 52));
        Frame();
        Assert.Equal(60d, slider.Attributes["range-value"]);
        Assert.Equal(7, root.Elements("SliderTick").Count());
        Assert.Equal(7, root.Elements("RangeSliderTickLabel").Count());

        await Choose("slider-orientation", "Vertical");
        foreach (var control in new[] { slider, root.Single("RangeSlider") })
        {
            Assert.Equal("vertical", control.Attributes["orientation"]);
            var bounds = scene.GetBounds(control);
            Assert.True(bounds.Height > bounds.Width);
        }
        Assert.Contains("Volume 60", root.Elements().Single(element => element.Id == "slider-values").Text());
    }
}
