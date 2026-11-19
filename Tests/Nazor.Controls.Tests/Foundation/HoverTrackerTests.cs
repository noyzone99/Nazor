using Nazor.Controls.Tests.Foundation.Fixtures;
using Nazor.Graphics;
using Nazor.Input;

namespace Nazor.Controls.Tests.Foundation;

[Collection("Controls")]
public sealed class HoverTrackerTests : IDisposable
{
    private readonly ControlHarness h = new();
    public void Dispose() => h.Dispose();
    private HoverProbe Render() { h.Render<HoverProbe>(); return h.Component<HoverProbe>(); }
    private void Hover(string name)
    {
        var bounds = h.Bounds(h.ByClass(name));
        h.Input.PointerMoved(new(bounds.X + bounds.Width / 2, bounds.Y + 5), KeyModifiers.None);
        h.Frame();
    }

    [Fact]
    public void Initial_entry_snaps_later_rows_move_and_exit_retains_only_the_last_decorative_box()
    {
        var probe = Render();
        Hover("first");
        Assert.True(probe.Outer.Active);
        Assert.False(probe.Outer.IsMoving);
        var first = probe.Outer.Target;
        Hover("second");
        Assert.True(probe.Outer.Active);
        Assert.True(probe.Outer.IsMoving);
        Assert.True(probe.Outer.Target!.Value.Y > first!.Value.Y);
        var last = probe.Outer.Target;
        h.Input.PointerMoved(new(500, 300), KeyModifiers.None); h.Frame();
        Assert.False(probe.Outer.Active);
        Assert.False(probe.Outer.IsMoving);
        Assert.Equal(last, probe.Outer.Target);
        Hover("first");
        Assert.True(probe.Outer.Active);
        Assert.False(probe.Outer.IsMoving);
        Assert.Equal(first, probe.Outer.Target);
        h.Click(h.ByClass("first"));
        Assert.Equal(1, probe.Clicks);
        Assert.Same(h.ByClass("first"), h.Focused);
    }

    [Fact]
    public async Task Host_and_nested_scrolls_preserve_content_coordinates_and_the_original_scroll_target_when_forwarded()
    {
        var probe = Render();
        Hover("second");
        var target = probe.Outer.Target;
        Assert.True(probe.Host!.Element.ScrollTo(null, 20)); h.Frame();
        Assert.Equal(target, probe.Outer.Target);
        Assert.True(probe.Nested!.Element.ScrollTo(null, 60)); h.Frame();
        Assert.Equal(target, probe.Outer.Target);
        Assert.Contains(probe.Nested.Element.Element, probe.ScrollSources);
        var args = new ScrollEventArgs { ScrollTop = 70 };
        await h.Host.Renderer.DispatchEventAsync(probe.Nested.Element.Element!, "onscroll", args);
        await h.Host.Renderer.DispatchEventAsync(probe.Host.Element.Element!, "onscroll", args);
        h.Frame();
        Assert.Same(probe.Nested.Element.Element, args.OriginalTarget);
        Assert.Equal(target, probe.Outer.Target);
    }

    [Theory]
    [InlineData("remove")]
    [InlineData("disabled")]
    [InlineData("hidden")]
    public void Removed_disabled_and_hidden_rows_update_eligible_hover_without_consuming_actions(string change)
    {
        var probe = Render();
        Hover(change == "remove" ? "first" : "second");
        Assert.True(probe.Outer.Active);
        if (change == "remove") probe.ShowFirst = false;
        if (change == "disabled") probe.Disabled = true;
        if (change == "hidden") probe.Hidden = true;
        probe.Refresh(); h.Frame();
        if (change == "remove")
        {
            Assert.True(probe.Outer.Active);
            Assert.Equal(60, probe.Outer.Target!.Value.Height);
            Assert.Equal(0, probe.Outer.Target.Value.Y);
            Assert.DoesNotContain(h.Root.Elements(), element => element.Class?.Split(' ').Contains("first") is true);
        }
        else Assert.False(probe.Outer.Active);
        Assert.NotNull(probe.Outer.Target);
        Assert.Equal(0, probe.Clicks);
    }

    [Fact]
    public void A_nested_tracker_owns_its_rows_without_lighting_the_outer_group()
    {
        var probe = Render();
        Hover("first");
        Assert.True(probe.Host!.Element.ScrollTo(null, 90)); h.Frame();
        Hover("inner");
        Assert.True(probe.Inner.Active, $"Host {h.Bounds(h.ByClass("hover-host"))}; inner {h.Bounds(h.ByClass("inner"))}; hovered {h.Input.Hovered?.Name}/{h.Input.Hovered?.Class}.");
        Assert.False(probe.Outer.Active);
        h.Click(h.ByClass("inner"));
        Assert.Equal(1, probe.Clicks);
    }
}
