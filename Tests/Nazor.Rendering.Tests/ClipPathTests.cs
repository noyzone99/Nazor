using System.Runtime.InteropServices;
using Nazor.Animation;
using Nazor.Controls;
using Nazor.Graphics;
using Nazor.Nss;
using Nazor.Rendering;
using Nazor.Style;
using Nazor.Testing;
using Color = Nazor.Graphics.Color;

namespace Nazor.Rendering.Tests;

public sealed class ClipPathProbe : ComponentBase
{
    public string Style { get; set; } = "";
    public void Refresh() => StateHasChanged();

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "Container");
        builder.AddAttribute(1, "style", "width: 180px; height: 140px; padding: 20px; background: white;");
        builder.OpenElement(2, "Box");
        builder.AddAttribute(3, "style", "position: relative; width: 100px; height: 60px; background: red; border: 4px solid blue; border-radius: 6px; outline: 2px solid black; box-shadow: 0 0 0 4px black;" + Style);
        builder.AddContent(4, "Text");
        builder.OpenElement(5, "Child");
        builder.AddAttribute(6, "style", "position: absolute; left: 90px; top: 10px; width: 30px; height: 20px; background: #00aa00;");
        builder.CloseElement();
        builder.CloseElement();
        builder.CloseElement();
    }
}

public sealed class ClipPathTests
{
    [Theory]
    [InlineData(1f, false)]
    [InlineData(1.25f, false)]
    [InlineData(1.5f, false)]
    [InlineData(2f, true)]
    public void Insets_clip_all_ink_and_hit_testing_without_changing_layout(float scale, bool translated)
    {
        using var harness = new RenderHarness();
        harness.Scene.Styles.Theme = StyleSheets.Basic;
        var root = harness.Host.Render<ClipPathProbe>();
        var probe = (ClipPathProbe)root.Component;
        probe.Style = "clip-path: inset(25% 10px);" + (translated ? "transform: translateX(10px);" : "");
        probe.Refresh();
        harness.Host.RunPending();
        var image = harness.Render(180, 140, scale);
        var box = root.Single("Box");
        var frame = harness.Scene.GetBounds(box);
        Assert.Equal(new Size(100, 60), frame.Size);
        var clip = new Rect(frame.X + 10, frame.Y + 15, 80, 30);
        Assert.Same(box, harness.Scene.HitTest(new Point(frame.X + 40, frame.Y + 30)));
        Assert.NotSame(box, harness.Scene.HitTest(new Point(frame.X + 40, frame.Y + 5)));
        Assert.NotSame(root.Single("Child"), harness.Scene.HitTest(new Point(frame.Right + 5, frame.Y + 15)));
        for (var y = 0; y < image.Height; y++)
        for (var x = 0; x < image.Width; x++)
            if (!clip.Inflate(1).Contains(new Point((x + 0.5f) / scale, (y + 0.5f) / scale)))
                Assert.Equal(Color.White, image.GetPixel(x, y));
        var localClip = translated ? clip.Offset(-10, 0) : clip;
        Assert.Contains(harness.Scene.BuildDisplayList().Commands, command => command is PushClip c && c.Rect == localClip);
    }

    [Theory]
    [InlineData("inset(60% 0)", false)]
    [InlineData("inset(0 60%)", false)]
    [InlineData("inset(-10px)", true)]
    public void Empty_clips_hide_descendants_and_negative_insets_allow_overflow(string value, bool visible)
    {
        using var harness = new RenderHarness();
        harness.Scene.Styles.Theme = StyleSheets.Basic;
        var root = harness.Host.Render<ClipPathProbe>();
        var probe = (ClipPathProbe)root.Component;
        probe.Style = "clip-path: " + value;
        probe.Refresh();
        harness.Host.RunPending();
        var image = harness.Render(180, 140, 1);
        var box = harness.Scene.GetBounds(root.Single("Box"));
        var point = new Point(box.Right + 5, box.Y + 15);
        Assert.Equal(visible, harness.Scene.HitTest(point) == root.Single("Child"));
        Assert.Equal(visible ? new Color(0, 170, 0, 255) : Color.White, image.GetPixel((int)point.X, (int)point.Y));
    }

    [Fact]
    public void Transitions_interpolate_insets_and_treat_none_discretely()
    {
        var clock = new ManualAnimationClock();
        using var harness = new RenderHarness(clock: clock);
        harness.Scene.Styles.Theme = StyleSheets.Basic;
        harness.Scene.Transitions.ReducedMotion = false;
        var root = harness.Host.Render<ClipPathProbe>();
        var probe = (ClipPathProbe)root.Component;
        Show("inset(0)");
        Show("inset(50% 20px)");
        clock.Advance(100);
        harness.Render(180, 140, 1);
        var clip = harness.Scene.GetEffectiveValue(root.Single("Box"), StyleProperties.ClipPath);
        Assert.Equal(15, clip.Top.Resolve(60));
        Assert.Equal(10, clip.Right.Resolve(100));
        Assert.Equal(0, harness.Scene.LastUpdate.LayoutComputations);
        Show("none");
        Assert.True(harness.Scene.GetEffectiveValue(root.Single("Box"), StyleProperties.ClipPath).IsNone);
        Assert.False(harness.Scene.Transitions.IsRunning);

        void Show(string clip)
        {
            probe.Style = $"transition: clip-path 200ms linear; clip-path: {clip};";
            probe.Refresh();
            harness.Host.RunPending();
            harness.Render(180, 140, 1);
        }
    }

    [Theory]
    [InlineData(1f)]
    [InlineData(1.25f)]
    [InlineData(1.5f)]
    [InlineData(2f)]
    public void Animated_clip_damage_repaints_exactly_like_a_full_frame(float scale)
    {
        var clock = new ManualAnimationClock();
        using var harness = new RenderHarness(clock: clock);
        harness.Scene.Styles.Theme = StyleSheets.Basic;
        harness.Scene.Transitions.ReducedMotion = false;
        var result = NssCompiler.Compile("clip", "@keyframes reveal { from { clip-path: inset(50% 0 round 30px / 15px); } to { clip-path: inset(0 round 6px); } }");
        Assert.True(result.Success);
        harness.Scene.Styles.Application = result.StyleSheet;
        var root = harness.Host.Render<ClipPathProbe>();
        var probe = (ClipPathProbe)root.Component;
        probe.Style = "animation: reveal 200ms linear forwards; opacity: 0.75;";
        probe.Refresh();
        harness.Host.RunPending();
        var priorImage = harness.Render(180, 140, scale);
        var priorList = harness.Scene.BuildDisplayList();
        foreach (var delta in new[] { 50, 50, 100 })
        {
            clock.Advance(delta);
            var expected = harness.Render(180, 140, scale);
            var list = harness.Scene.BuildDisplayList();
            var damage = DisplayListDamage.ToPixels(DisplayListDamage.Compute(priorList, list, scale)!, scale, expected.Width, expected.Height);
            var buffer = priorImage.Pixels;
            var pinned = GCHandle.Alloc(buffer, GCHandleType.Pinned);
            try
            {
                harness.Graphics.Render(list, pinned.AddrOfPinnedObject(), expected.Width, expected.Height, expected.Stride, scale,
                    harness.Scene.Background, damage);
            }
            finally { pinned.Free(); }
            Assert.True(expected.Pixels.SequenceEqual(buffer));
            priorImage = expected;
            priorList = list;
            Assert.Equal(0, harness.Scene.LastUpdate.LayoutComputations);
        }
    }
}
