using Nazor.Graphics;
using Nazor.Input;
using Nazor.Rendering;
using Nazor.Samples.Counter;
using Nazor.Testing;

namespace Nazor.Diagnostics.Tests;

public sealed class InspectionTests : IDisposable
{
    private readonly DiagnosticsHarness harness = new();
    public void Dispose() => harness.Dispose();

    [Fact]
    public void The_overlay_draws_separate_rings_and_a_label_above_the_app()
    {
        harness.Host.Render<Boxed>();
        harness.Scene.Update();
        var id = harness.Session.Get(harness.Session.Roots[0])!.Children[0];
        var list = harness.Scene.BuildDisplayList();
        Assert.Same(list, harness.Session.Compose(list));
        harness.Session.Highlight(id);
        var overlay = harness.Session.Compose(list).Commands.Skip(list.Commands.Count).ToArray();
        var rings = overlay.OfType<DrawBorder>().ToArray();
        Assert.Equal(new Rect(10, 0, 58, 28), rings[0].Rect);
        Assert.Equal(new Thickness(4, 4, 4, 4), rings[0].Widths);
        Assert.Equal(new Rect(14, 4, 50, 20), rings[1].Rect);
        Assert.Equal(new Thickness(2, 2, 2, 2), rings[1].Widths);
        Assert.Equal(new Rect(16, 6, 46, 16), rings[2].Rect);
        Assert.Equal(new Thickness(3, 3, 3, 3), rings[2].Widths);
        Assert.Equal(new Color(0xF6, 0xB2, 0x6B, 0xA8), rings[0].Color);
        Assert.Equal(new Color(0xFF, 0xE5, 0x99, 0xA8), rings[1].Color);
        Assert.Equal(new Color(0x93, 0xC4, 0x7D, 0x8C), rings[2].Color);
        var content = overlay.OfType<FillRectangle>().First();
        Assert.Equal(new Rect(19, 9, 40, 10), content.Rect);
        Assert.Equal(new Color(0x6F, 0xA8, 0xDC, 0xA8), content.Color);
        Assert.Contains("Container.moved  50 × 20", Assert.Single(overlay.OfType<DrawText>()).Layout.Text);
        Assert.IsType<PushClip>(overlay[0]);
        Assert.IsType<Pop>(overlay[^1]);
    }

    [Fact]
    public void A_component_highlights_the_boxes_of_its_rendered_outputs_under_its_type_name()
    {
        harness.Host.Render<Nazor.DevTools.Fixtures.TreePage>();
        harness.Scene.Update();
        var data = harness.Session.CaptureTree();
        var left = data.Components.Single(c => c.Key == "left");
        var wrapper = data.Components.Single(c => c.TypeName == typeof(Nazor.DevTools.Fixtures.PassThrough).FullName);
        var list = harness.Scene.BuildDisplayList();
        harness.Session.Select(left.Id);
        Assert.Equal(left.Id, harness.Session.HighlightedId);
        var overlay = harness.Session.Compose(list).Commands.Skip(list.Commands.Count).ToArray();
        Assert.Equal(3, overlay.OfType<DrawBorder>().Count());
        Assert.Equal(harness.Session.Box(left.OutputNodes[0])!.ContentBox, overlay.OfType<FillRectangle>().First().Rect);
        Assert.StartsWith("MultiOutput  ", Assert.Single(overlay.OfType<DrawText>()).Layout.Text);
        harness.Session.Highlight(wrapper.Id);
        overlay = harness.Session.Compose(list).Commands.Skip(list.Commands.Count).ToArray();
        Assert.True(overlay.OfType<DrawBorder>().Count() >= 6);
        Assert.StartsWith("PassThrough  ", Assert.Single(overlay.OfType<DrawText>()).Layout.Text);
        var text = data.Nodes.First(n => n.Kind == NodeKind.Text);
        harness.Session.Highlight(text.Id);
        Assert.Null(harness.Session.HighlightedId);
        Assert.Same(list, harness.Session.Compose(list));
    }

    [Fact]
    public void Inspecting_selects_without_changing_hover_focus_or_activating_the_button()
    {
        harness.Host.Render<Counter>();
        harness.Scene.Update();
        var button = harness.Host.Renderer.RootComponents.Single().Single("Button");
        var bounds = harness.Scene.GetBounds(button);
        var point = new Point(bounds.X + bounds.Width / 2, bounds.Y + bounds.Height / 2);
        var selected = new List<int?>();
        harness.Session.SelectionChanged += selected.Add;
        var states = button.States;
        harness.Session.SetInspectMode(true);
        Assert.True(harness.Session.PointerMoved(point));
        Assert.True(harness.Session.PointerPressed(point, PointerButton.Primary));
        Assert.True(harness.Session.PointerReleased(point, PointerButton.Primary));
        harness.Host.RunPending();
        Assert.Equal(states, button.States);
        Assert.Null(harness.Input.Focused);
        Assert.Null(harness.Input.Hovered);
        Assert.False(harness.Session.IsInspecting);
        Assert.Equal(harness.Session.IdOf(button), Assert.Single(selected));
        Assert.Equal("Count: 0", harness.Host.Renderer.RootComponents.Single().Single("TextBlock").Text());
        Assert.False(harness.Session.PointerMoved(point));
        harness.Input.PointerPressed(point, PointerButton.Primary, KeyModifiers.None);
        harness.Input.PointerReleased(point, PointerButton.Primary, KeyModifiers.None);
        harness.Host.RunPending();
        Assert.Equal("Count: 1", harness.Host.Renderer.RootComponents.Single().Single("TextBlock").Text());
    }

    [Fact]
    public void Escape_and_a_release_after_escape_are_consumed_and_hover_is_preserved()
    {
        harness.Host.Render<Counter>();
        harness.Scene.Update();
        var button = harness.Host.Renderer.RootComponents.Single().Single("Button");
        var bounds = harness.Scene.GetBounds(button);
        var point = new Point(bounds.X + bounds.Width / 2, bounds.Y + bounds.Height / 2);
        harness.Input.PointerMoved(point, KeyModifiers.None);
        var states = button.States;
        harness.Session.SetInspectMode(true);
        harness.Session.PointerPressed(point, PointerButton.Primary);
        Assert.True(harness.Session.KeyDown("Escape"));
        Assert.True(harness.Session.KeyUp("Escape"));
        Assert.True(harness.Session.PointerReleased(point, PointerButton.Primary));
        Assert.False(harness.Session.PointerReleased(point, PointerButton.Primary));
        Assert.False(harness.Session.IsInspecting);
        Assert.Null(harness.Session.HighlightedId);
        Assert.Null(harness.Session.SelectedId);
        Assert.Equal(states, button.States);
        Assert.Same(button, harness.Input.Hovered);
    }

    [Fact]
    public void Moving_removing_and_disabling_the_highlight_damage_the_previous_area()
    {
        var root = harness.Host.Render<Toggler>();
        harness.Scene.Update();
        var id = harness.Session.Get(harness.Session.Roots[0])!.Children[0];
        var list = harness.Scene.BuildDisplayList();
        var damage = new DisplayListDamage();
        Assert.Null(damage.Next(list));
        harness.Session.Select(id);
        Assert.NotEmpty(damage.Next(harness.Session.Compose(list))!);
        var changes = 0;
        harness.Session.OverlayChanged += () => changes++;
        harness.Session.Highlight(id);
        Assert.Equal(0, changes);
        ((Toggler)root.Component).Show = false;
        ((Toggler)root.Component).Refresh();
        harness.Host.RunPending();
        Assert.Null(harness.Session.SelectedId);
        Assert.Null(harness.Session.HighlightedId);
        Assert.NotEmpty(damage.Next(harness.Session.Compose(list))!);
        Assert.Same(list, harness.Session.Compose(list));
    }

    [Fact]
    public void Starting_inspection_cancels_a_pressed_control_and_its_pointer_capture()
    {
        harness.Host.Render<Counter>();
        harness.Scene.Update();
        var button = harness.Host.Renderer.RootComponents.Single().Single("Button");
        var bounds = harness.Scene.GetBounds(button);
        var point = new Point(bounds.X + bounds.Width / 2, bounds.Y + bounds.Height / 2);
        harness.Input.PointerPressed(point, PointerButton.Primary, KeyModifiers.None);
        Assert.True(harness.Input.CapturePointer(button));
        harness.Session.SetInspectMode(true);
        Assert.Null(harness.Input.CapturedPointer);
        Assert.False(button.States.HasFlag(ElementStates.Pressed));
        Assert.Same(button, harness.Input.Hovered);
        harness.Session.PointerReleased(point, PointerButton.Primary);
        harness.Host.RunPending();
        Assert.Equal("Count: 0", harness.Host.Renderer.RootComponents.Single().Single("TextBlock").Text());
    }

    [Fact]
    public void Frame_history_is_bounded_and_keeps_a_snapshot_of_damage()
    {
        var damage = new List<PixelRect> { new(1, 2, 3, 4) };
        FrameInfo? last = null;
        harness.Session.FrameRecorded += frame => last = frame;
        for (var i = 1; i <= 125; i++)
            harness.Session.RecordFrame(i, FrameCause.RenderBatch | FrameCause.Interaction, TimeSpan.FromMilliseconds(5),
                TimeSpan.FromMilliseconds(1), TimeSpan.FromMilliseconds(1), TimeSpan.FromMilliseconds(1), TimeSpan.Zero,
                TimeSpan.FromMilliseconds(1), 2, 3, damage);
        damage.Clear();
        Assert.Equal(120, harness.Session.Frames.Count);
        Assert.Equal(6, harness.Session.Frames[0].Number);
        Assert.Equal(125, last!.Number);
        Assert.Equal(FrameCause.RenderBatch | FrameCause.Interaction, last.Cause);
        Assert.Equal(new PixelRect(1, 2, 3, 4), Assert.Single(last.Damage!));
        var snapshot = harness.Session.Frames;
        harness.Session.Dispose();
        Assert.Empty(harness.Session.Frames);
        Assert.Equal(120, snapshot.Count);
    }
}
