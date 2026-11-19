using Nazor.Controls.Tests.Selection.Fixtures;
using Nazor.Graphics;
using Nazor.Input;
using Nazor.Testing;

namespace Nazor.Controls.Tests.Selection;

[Collection("Controls")]
public sealed class SwitchDragTests : IDisposable
{
    private readonly ControlHarness h = new();
    public void Dispose() => h.Dispose();
    private SwitchDragProbe Render()
    {
        h.Render<SwitchDragProbe>();
        return h.Component<SwitchDragProbe>();
    }
    private Point ThumbCenter()
    {
        var bounds = h.Bounds(h.One("SwitchThumbPosition"));
        return new(bounds.X + bounds.Width / 2, bounds.Y + bounds.Height / 2);
    }
    private float Travel => h.Bounds(h.One("SwitchThumbRail")).Width - h.Bounds(h.One("SwitchThumbPosition")).Width;
    private void Down(Point point) { h.Input.PointerPressed(point, PointerButton.Primary, KeyModifiers.None); h.Frame(); }
    private void Move(Point point) { h.Input.PointerMoved(point, KeyModifiers.None); h.Frame(); }
    private void Up(Point point) { h.Input.PointerReleased(point, PointerButton.Primary, KeyModifiers.None); h.Frame(); }

    [Fact]
    public void Drag_previews_without_changing_accessible_state_then_commits_once_and_keeps_the_next_click()
    {
        var probe = Render();
        var start = ThumbCenter();
        Down(start);
        var end = new Point(start.X + Travel * .8f, start.Y);
        Move(end);
        Assert.True(probe.Switch.IsDragging);
        Assert.Equal(false, h.One("Switch").Attributes["checked"]);
        Assert.Equal(true, h.One("Switch").Attributes["drag-on"]);
        Assert.Empty(probe.Changes);
        Assert.True(h.Bounds(h.One("SwitchThumbPosition")).X > start.X);
        Up(end);
        Assert.True(probe.IsOn);
        Assert.Equal([true], probe.Changes);
        Assert.Equal(1, probe.Inputs);
        Assert.Equal(1, probe.Commits);
        Assert.False(probe.Switch.IsDragging);
        h.Click(h.One("SwitchLabel"));
        Assert.Equal([true, false], probe.Changes);
    }

    [Fact]
    public void Short_drag_snaps_back_without_emitting_a_change_and_reverse_drag_turns_off()
    {
        var probe = Render();
        var start = ThumbCenter();
        Down(start);
        var end = new Point(start.X + Travel * .4f, start.Y);
        Move(end); Up(end);
        Assert.False(probe.IsOn);
        Assert.Empty(probe.Changes);
        probe.IsOn = true; probe.Refresh(); h.Frame();
        start = ThumbCenter(); Down(start);
        end = new(start.X - Travel * .8f, start.Y);
        Move(end); Up(end);
        Assert.Equal([false], probe.Changes);
    }

    [Theory]
    [InlineData("escape")]
    [InlineData("capture")]
    [InlineData("disabled")]
    [InlineData("external")]
    public void Cancellation_discards_the_preview_and_preserves_the_next_independent_activation(string reason)
    {
        var probe = Render();
        var start = ThumbCenter(); Down(start);
        var end = new Point(start.X + Travel * .8f, start.Y); Move(end);
        switch (reason)
        {
            case "escape": h.Press("Escape"); break;
            case "capture": h.Input.PointerCanceled(); h.Frame(); break;
            case "disabled": probe.Disabled = true; probe.Refresh(); h.Frame(); break;
            case "external": probe.IsOn = true; probe.Refresh(); h.Frame(); break;
        }
        Up(end);
        Assert.False(probe.Switch.IsDragging);
        Assert.Empty(probe.Changes);
        Assert.Equal(reason == "external", probe.IsOn);
        probe.Disabled = false; probe.Refresh(); h.Frame();
        h.Click(h.One("SwitchLabel"));
        Assert.Single(probe.Changes);
        Assert.Equal(reason != "external", probe.IsOn);
    }

    [Fact]
    public void A_click_on_the_thumb_and_Space_keep_existing_single_activation_behavior()
    {
        var probe = Render();
        var center = ThumbCenter(); Down(center); Up(center);
        Assert.Equal([true], probe.Changes);
        h.Press(" ");
        Assert.Equal([true, false], probe.Changes);
        Assert.Equal(2, probe.Commits);
    }

    [Fact]
    public void Vertical_movement_cancels_and_secondary_buttons_never_start_a_drag()
    {
        var probe = Render();
        var center = ThumbCenter(); Down(center);
        var end = new Point(center.X + 1, center.Y + 8); Move(end); Up(end);
        Assert.Empty(probe.Changes);
        Assert.False(probe.Switch.IsDragging);
        h.Input.PointerPressed(center, PointerButton.Secondary, KeyModifiers.None); h.Frame();
        Assert.False(probe.Switch.IsDragging);
        h.Input.PointerReleased(center, PointerButton.Secondary, KeyModifiers.None); h.Frame();
        h.Click(h.One("SwitchLabel"));
        Assert.Equal([true], probe.Changes);
    }

    [Fact]
    public void Capture_loss_without_a_leave_event_clears_the_preview_immediately()
    {
        var probe = Render();
        var center = ThumbCenter(); Down(center);
        Move(new(center.X + Travel * .8f, center.Y));
        Assert.Equal(true, h.One("Switch").Attributes["dragging"]);
        h.Input.PointerCanceled(clearHover: false);
        h.Frame();
        Assert.False(probe.Switch.IsDragging);
        Assert.Empty(probe.Changes);
        Assert.Equal(false, h.One("Switch").Attributes["dragging"]);
    }

    [Fact]
    public void Rendering_and_removing_a_switch_without_an_input_manager_does_not_require_pointer_services()
    {
        using var host = new TestHost();
        var root = host.Render<Switch>();
        Assert.False(((Switch)root.Component).IsDragging);
        host.Renderer.RemoveRootComponent(root);
        Assert.Empty(host.Renderer.RootComponents);
    }

    [Fact]
    public void Removing_the_switch_after_input_disposal_does_not_release_detached_pointer_services()
    {
        var probe = Render();
        Down(ThumbCenter());
        Assert.True(probe.Switch.IsDragging);
        h.Input.Dispose();
        Assert.False(probe.Switch.IsDragging);
        h.Host.Renderer.RemoveRootComponent(h.Root);
        Assert.Empty(h.Host.Renderer.RootComponents);
    }
}
