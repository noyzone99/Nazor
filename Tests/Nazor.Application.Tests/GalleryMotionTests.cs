using Nazor.Animation;
using Nazor.Graphics;
using Nazor.Graphics.Text;
using Nazor.Input;
using Nazor.Presentation;
using Nazor.Rendering;
using Nazor.Samples.Gallery.Showcase;
using Nazor.Skia;
using Nazor.Style;
using Nazor.Testing;
using Nazor.Themes.NazorLeaf;

namespace Nazor.Application.Tests;

public sealed class GalleryMotionTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Motion_lanes_use_distinct_timelines_replay_and_honor_reduced_motion(bool reduced)
    {
        var clock = new ManualAnimationClock();
        using var host = new TestHost();
        using var graphics = new SkiaRenderer(new FontOptions { UseSystemFonts = true });
        using var scene = new Scene(host.Renderer, graphics.Fonts, graphics.Shaper, clock: clock) { Viewport = new Size(850, 700) };
        scene.Styles.Theme = NazorLeafTheme.Theme;
        scene.Styles.Preferences = new MediaPreferences(ColorScheme.Light, ReducedMotion: reduced);
        using var input = new InputManager(host.Renderer, scene);
        var root = host.Render<NavigationSection>();
        void Frame() { host.RunPending(); scene.Update(); scene.BuildDisplayList(); }
        Frame();
        await host.ClickAsync(root.Elements().Single(element => Equals(element.Attributes.GetValueOrDefault("role"), "tab") && element.Text() == "Movimento"));
        Frame();

        ElementNode[] Seeds() => root.Elements().Where(element => element.Class == "demo-motion-seed").ToArray();
        double Position(ElementNode seed)
        {
            var lane = scene.GetContentBox(seed.ParentElement!);
            return (scene.GetBounds(seed).X - lane.X - 5) / (lane.Width - 30);
        }
        var play = root.Elements().Single(element => element.Id == "replay-motion");
        await host.ClickAsync(play);
        Frame();
        var firstRun = Seeds();
        Assert.Equal(4, firstRun.Length);
        foreach (var seed in firstRun) Assert.InRange(Position(seed), reduced ? .99 : -.01, reduced ? 1.01 : .01);
        clock.Advance(120);
        Frame();
        Assert.InRange(Position(firstRun[0]), .99, 1.01);
        if (!reduced)
        {
            Assert.InRange(Position(firstRun[1]), .1, .99);
            Assert.InRange(Position(firstRun[3]), .01, Position(firstRun[1]));
        }
        clock.Advance(1000);
        Frame();
        foreach (var seed in firstRun) Assert.InRange(Position(seed), .99, 1.01);

        await host.ClickAsync(play);
        Frame();
        var secondRun = Seeds();
        Assert.DoesNotContain(secondRun, firstRun.Contains);
        foreach (var seed in secondRun) Assert.InRange(Position(seed), reduced ? .99 : -.01, reduced ? 1.01 : .01);
        clock.Advance(1000);
        Frame();
        foreach (var seed in secondRun) Assert.InRange(Position(seed), .99, 1.01);
    }
}
