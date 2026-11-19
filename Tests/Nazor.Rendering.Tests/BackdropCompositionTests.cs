using Nazor.Graphics;
using Nazor.Nss;
using Nazor.Rendering;
using Color = Nazor.Graphics.Color;

namespace Nazor.Rendering.Tests;

public sealed class BackdropCompositionTests
{
    [Theory]
    [InlineData("backdrop-filter")]
    [InlineData("filter")]
    public void A_moving_filter_keeps_mixed_sibling_animations_on_the_ui_frame_path(string property)
    {
        using var h = new RenderHarness();
        h.Scene.Transitions.ReducedMotion = false;
        var root = h.Host.Render<Probe>();
        var page = (Probe)root.Component;
        page.FilterProperty = property;
        page.Animated = true;
        page.Refresh(); h.Host.RunPending();
        var result = NssCompiler.Compile("backdrop-motion", """
            Glass, Ordinary { animation: drift 60s linear infinite; }
            @keyframes drift { from { transform: translateX(0px); opacity: .8; } to { transform: translateX(10px); opacity: .4; } }
            """);
        Assert.True(result.Success);
        h.Scene.Styles.Application = result.StyleSheet;
        h.Render(100, 80, 1);
        Assert.False(h.Scene.CanAnimateOnCompositor);
        var mixed = h.Scene.BuildCompositionList();
        Assert.Empty(mixed.Commands.OfType<PushComposition>());
        Assert.Single(mixed.Commands, command => property == "filter" ? command is PushFilter : command is PushBackdrop);
        Assert.NotNull(h.Scene.TimeUntilNextFrame);

        page.Filter = "none";
        page.Refresh(); h.Host.RunPending();
        h.Render(100, 80, 1);
        Assert.True(h.Scene.CanAnimateOnCompositor);
        Assert.Equal(2, h.Scene.BuildCompositionList().Commands.OfType<PushComposition>().Count());
    }

    [Theory]
    [InlineData(1f)] [InlineData(2f)]
    public void Earlier_content_changes_are_sampled_on_consecutive_scene_frames(float scale)
    {
        using var h = new RenderHarness();
        var root = h.Host.Render<Probe>();
        var page = (Probe)root.Component;
        page.EarlierColor = "red";
        page.Refresh(); h.Host.RunPending();
        var red = h.Render(100, 80, scale);
        page.EarlierColor = "blue";
        page.Refresh(); h.Host.RunPending();
        var blue = h.Render(100, 80, scale);
        var before = red.GetPixel((int)(30 * scale), (int)(40 * scale));
        var after = blue.GetPixel((int)(30 * scale), (int)(40 * scale));
        Assert.InRange(before.R, 52, 56);
        Assert.InRange(after.R, 17, 20);
        Assert.Equal(before.R, before.G); Assert.Equal(before.R, before.B);
        Assert.Equal(after.R, after.G); Assert.Equal(after.R, after.B);
        Assert.NotEqual(before, after);
    }

    [Theory]
    [InlineData(1f)] [InlineData(1.5f)] [InlineData(2f)]
    public void Scene_backdrop_opacity_applies_once_to_overlapping_and_overflowing_foreground(float scale)
    {
        using var h = new RenderHarness();
        var root = h.Host.Render<Probe>();
        var page = (Probe)root.Component;
        page.Opacity = ".5";
        page.Refresh(); h.Host.RunPending();
        var image = h.Render(100, 80, scale);
        var list = h.Scene.BuildDisplayList();
        Assert.Equal(.5f, Assert.Single(list.Commands.OfType<PushBackdrop>()).Opacity);
        Assert.Empty(list.Commands.OfType<PushOpacity>());
        page.Filter = "none"; page.Refresh(); h.Host.RunPending();
        var plain = h.Render(100, 80, scale);
        foreach (var point in new[] { new Point(15, 15), new Point(50, 40) })
        {
            var pixel = image.GetPixel((int)(point.X * scale), (int)(point.Y * scale));
            Assert.Equal(plain.GetPixel((int)(point.X * scale),(int)(point.Y * scale)),pixel);
            Assert.Equal((byte)255, pixel.G);
        }
    }

    private sealed class Probe : ComponentBase
    {
        public string EarlierColor { get; set; } = "white";
        public string Filter { get; set; } = "blur(2px) saturate(0)";
        public string FilterProperty { get; set; } = "backdrop-filter";
        public string Opacity { get; set; } = "1";
        public bool Animated { get; set; }
        public void Refresh() => StateHasChanged();

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenElement(0, "Page"); builder.AddAttribute(1, "style", "position: relative; width: 100px; height: 80px; background: white;");
            builder.OpenElement(2, "Earlier"); builder.AddAttribute(3, "style", "position: absolute; left: 0; top: 0; width: 100px; height: 80px; background:" + EarlierColor); builder.CloseElement();
            builder.OpenElement(4, "Glass"); builder.AddAttribute(5, "style", $"position: absolute; left: 20px; top: 20px; width: 60px; height: 40px; border-radius: 12px; {FilterProperty}: {Filter}; opacity: {Opacity};");
            builder.OpenElement(6, "Overflow"); builder.AddAttribute(7, "style", "position: absolute; left: -10px; top: -10px; width: 10px; height: 10px; background: lime;"); builder.CloseElement();
            builder.OpenElement(8, "First"); builder.AddAttribute(9, "style", "position: absolute; left: 20px; top: 10px; width: 20px; height: 20px; background: lime;"); builder.CloseElement();
            builder.OpenElement(10, "Second"); builder.AddAttribute(11, "style", "position: absolute; left: 25px; top: 15px; width: 20px; height: 20px; background: lime;"); builder.CloseElement();
            builder.CloseElement();
            if (Animated)
            {
                builder.OpenElement(12, "Ordinary"); builder.AddAttribute(13, "style", "position: absolute; left: 0; top: 60px; width: 10px; height: 10px; background: black;"); builder.CloseElement();
            }
            builder.CloseElement();
        }
    }
}
