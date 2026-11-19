using Nazor.Graphics;
using Nazor.Graphics.Text;
using System.Runtime.CompilerServices;
using Nazor.Rendering;
using Nazor.Testing;

namespace Nazor.Rendering.Tests;

public sealed class RetainedUpdateTests
{
    [Fact]
    public void Color_updates_reuse_static_drawings_and_keep_previous_display_lists_immutable()
    {
        using var harness = new RenderHarness();
        var root = harness.Host.Render<UpdateProbe>();
        var component = (UpdateProbe)root.Component;
        component.Extra = "overflow:hidden;";
        component.Refresh();
        harness.Host.RunPending();
        harness.Render(180, 80, 1);
        var before = harness.Scene.BuildDisplayList();
        var text = Assert.Single(before.Commands.OfType<DrawText>());
        var clip = Assert.Single(before.Commands.OfType<PushClip>());
        component.Extra = "overflow:hidden;background:blue;";
        component.Refresh();
        harness.Host.RunPending();
        harness.Render(180, 80, 1);
        var after = harness.Scene.BuildDisplayList();
        Assert.Same(text, Assert.Single(after.Commands.OfType<DrawText>()));
        Assert.Same(clip, Assert.Single(after.Commands.OfType<PushClip>()));
        Assert.Equal(new Color(255, 0, 0, 255), Assert.Single(before.Commands.OfType<FillRectangle>()).Color);
        Assert.Equal(new Color(0, 0, 255, 255), Assert.Single(after.Commands.OfType<FillRectangle>()).Color);
        component.Extra = "overflow:hidden;color:lime;width:120px;";
        component.Refresh();
        harness.Host.RunPending();
        harness.Render(180, 80, 1);
        var changed = harness.Scene.BuildDisplayList();
        Assert.NotSame(text, Assert.Single(changed.Commands.OfType<DrawText>()));
        Assert.NotSame(clip, Assert.Single(changed.Commands.OfType<PushClip>()));
        Assert.Equal(Color.Black, text.Color);
    }

    [Theory]
    [InlineData("Already normalized", "Already normalized", true)]
    [InlineData("Caffè 👩‍💻", "Caffè 👩‍💻", true)]
    [InlineData("  a  b  ", "a b", false)]
    [InlineData("\ta\n b\r\n", "a b", false)]
    [InlineData("a\u00a0b\u2003c", "a b c", false)]
    [InlineData(" \r\n\t ", "", false)]
    public void Text_normalization_preserves_whitespace_rules_and_reuses_canonical_strings(string input, string expected, bool canonical)
    {
        using var harness = new RenderHarness();
        var root = harness.Host.Render<UpdateProbe>();
        ((UpdateProbe)root.Component).Text = input;
        ((UpdateProbe)root.Component).Refresh();
        harness.Host.RunPending();
        harness.Render(180, 80, 1);
        var text = harness.Scene.BuildDisplayList().Commands.OfType<DrawText>().SingleOrDefault()?.Layout.Text ?? "";
        Assert.Equal(expected, text);
        if (canonical) Assert.Same(input, text);
    }

    [Theory]
    [InlineData(0, 0, 0, "red,lime,blue")]
    [InlineData(2, -1, 1, "lime,blue,red")]
    [InlineData(-1, 2, 2, "red,lime,blue")]
    public void Paint_order_preserves_ties_and_orders_negative_and_positive_layers(int first, int second, int third, string expected)
    {
        using var harness = new RenderHarness();
        harness.Host.Render<LayerProbe>(ParameterView.FromDictionary(new Dictionary<string, object?> { ["Layers"] = new[] { first, second, third } }));
        harness.Render(80, 60, 1);
        var colors = new Dictionary<Color, string> { [new(255, 0, 0, 255)] = "red", [new(0, 255, 0, 255)] = "lime", [new(0, 0, 255, 255)] = "blue" };
        Assert.Equal(expected, string.Join(',', harness.Scene.BuildDisplayList().Commands.OfType<FillRectangle>().Select(fill => colors[fill.Color])));
    }

    private sealed class LayerProbe : ComponentBase
    {
        [Parameter] public int[] Layers { get; set; } = [];
        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenElement(0, "Parent");
            var colors = new[] { "red", "lime", "blue" };
            for (var i = 0; i < Layers.Length; i++)
            {
                builder.OpenElement(1, "Box");
                builder.AddAttribute(2, "style", $"position:absolute; width:20px; height:20px; z-index:{Layers[i]}; background:{colors[i]};");
                builder.CloseElement();
            }
            builder.CloseElement();
        }
    }

    [Fact]
    public void The_scene_releases_obsolete_shaping_and_keeps_active_text_after_collection()
    {
        using var harness = new RenderHarness();
        var root = harness.Host.Render<UpdateProbe>();
        var previous = CaptureShaping(harness);
        ((UpdateProbe)root.Component).Text = "Updated text";
        ((UpdateProbe)root.Component).Refresh();
        harness.Host.RunPending();
        harness.Render(180, 80, 1);
        var active = Assert.Single(harness.Scene.BuildDisplayList().Commands.OfType<DrawText>()).Layout.Shaped;
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        Assert.False(previous.TryGetTarget(out _));
        harness.Render(180, 80, 1);
        Assert.Same(active, Assert.Single(harness.Scene.BuildDisplayList().Commands.OfType<DrawText>()).Layout.Shaped);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference<ShapedText> CaptureShaping(RenderHarness harness)
    {
        harness.Render(180, 80, 1);
        return new(Assert.Single(harness.Scene.BuildDisplayList().Commands.OfType<DrawText>()).Layout.Shaped);
    }

    [Theory]
    [InlineData(1f)]
    [InlineData(1.5f)]
    public void Paint_only_changes_retain_geometry_and_text_and_round_trip_without_pixel_changes(float scale)
    {
        using var harness = new RenderHarness();
        var root = harness.Host.Render<UpdateProbe>();
        var component = (UpdateProbe)root.Component;
        var element = root.Single("Box");
        var initial = harness.Render(180, 80, scale);
        var geometry = harness.Scene.GetLayoutNode(element)!.Style;
        var text = Assert.Single(harness.Scene.BuildDisplayList().Commands.OfType<DrawText>()).Layout;
        component.Extra = "background: blue;";
        component.Refresh();
        harness.Host.RunPending();
        harness.Render(180, 80, scale);
        Assert.Same(geometry, harness.Scene.GetLayoutNode(element)!.Style);
        Assert.Same(text, Assert.Single(harness.Scene.BuildDisplayList().Commands.OfType<DrawText>()).Layout);
        component.Extra = "width: 120px; transform: translateX(2px); font-size: 20px;";
        component.Refresh();
        harness.Host.RunPending();
        harness.Render(180, 80, scale);
        Assert.Equal(120, harness.Scene.GetBounds(element).Width);
        Assert.True(harness.Scene.GetLayoutNode(element)!.Style.Transformed);
        Assert.NotSame(text, Assert.Single(harness.Scene.BuildDisplayList().Commands.OfType<DrawText>()).Layout);
        component.Extra = "";
        component.Refresh();
        harness.Host.RunPending();
        Assert.Equal(initial.Pixels, harness.Render(180, 80, scale).Pixels);
    }

    private sealed class UpdateProbe : ComponentBase
    {
        public string Extra { get; set; } = "";
        public string Text { get; set; } = "Retained text";
        public void Refresh() => StateHasChanged();
        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenElement(0, "Box");
            builder.AddAttribute(1, "style", "width:100px; height:50px; background:red; color:black; font-family:'Fira Sans'; font-size:14px;" + Extra);
            builder.AddContent(2, Text);
            builder.CloseElement();
        }
    }
}
