using Nazor.DevTools.Fixtures;
using Nazor.DevTools.Protocol;
using Nazor.Diagnostics;
using Nazor.Graphics;
using Nazor.Input;
using Nazor.Presentation;
using Nazor.Skia;
using Nazor.Testing;

namespace Nazor.DevTools.Agent.Tests;

public sealed class DetailExporterTests
{
    [Fact]
    public void Materialized_debugger_roots_use_real_component_instances_and_release_them_on_agent_disposal()
    {
        using var host = new Harness();
        var directory = Path.Combine(Path.GetTempPath(), "ndt-" + Guid.NewGuid().ToString("N")[..12]);
        using var agent = new DevToolsAgent(host.Session, directory);
        var window = DebuggerRoots.Windows.Single(w => w.SessionId == host.Session.SessionId);
        var end = DateTime.UtcNow.AddSeconds(5);
        while (window.TreeJson.Length == 0 && DateTime.UtcNow < end) { host.Pump(); Thread.Sleep(5); }
        var tree = DevToolsJson.Read<TreeSnapshot>(System.Text.Json.Nodes.JsonNode.Parse(window.TreeJson)!);
        Assert.NotEmpty(tree.Components);
        Assert.Equal(tree.Components.Length, window.Components.Length);
        Assert.Equal(window.TreeJson, string.Concat(window.TreeChunks));
        foreach (var component in window.Components)
            Assert.Same(((Nazor.Rendering.ComponentNode)host.Session.NodeOf(int.Parse(component.Id))!).Component, component.Instance);
        agent.Dispose(); host.Pump();
        Assert.DoesNotContain(window, DebuggerRoots.Windows);
        Assert.Empty(window.Components); Assert.Empty(window.TreeJson); Assert.Empty(window.TreeChunks);
    }

    [Fact]
    public async Task Traces_preserve_winners_origins_inline_and_inactive_media_as_detached_values()
    {
        StyleDetails captured;
        using (var host = new Harness())
        {
            var trace = host.Session.Trace(host.Piece)!;
            captured = host.Exporter.Styles(host.Target, host.Piece.ToString());
            Assert.Equal(trace.Values.Single(v => v.Property == "color").Value, captured.Computed.Single(v => v.Name == "color").Value);
            Assert.Contains(captured.Rules, r => r.Level == "theme" && r.Declarations.Any(d => d.Name == "color" && d.Winning!.Length == 0));
            Assert.Contains(captured.Rules, r => r.MediaMatches == false && r.Media is not null);
            Assert.Contains(captured.Rules, r => r.Selector == "element.style" && r.Level == "inline");
            Assert.Contains(captured.Computed, v => v.Origin == "initial");
            Assert.Contains(captured.Computed, v => v.Origin == "inherited");
            Assert.Contains(captured.Computed, v => v.Name == "padding-top" && v.Value == "4px" && v.Origin == "rule");
        }
        var json = await DetailExporter.SerializeAsync(captured, TestContext.Current.CancellationToken);
        Assert.NotEmpty(DevToolsJson.Read<StyleDetails>(json).Rules);
    }

    [Fact]
    public void Geometry_copies_all_transformed_boxes_and_target_scale_without_fabricating_hidden_geometry()
    {
        using var host = new Harness();
        host.Scene.Scale = 1.5f;
        var original = host.Session.Box(host.Piece)!;
        var captured = host.Exporter.Geometry(host.Target, host.Piece.ToString());
        Assert.Equal(new WireRect(original.BorderBox.X, original.BorderBox.Y, original.BorderBox.Width, original.BorderBox.Height), captured.BorderBox);
        Assert.Equal(new WireRect(original.ContentBox.X, original.ContentBox.Y, original.ContentBox.Width, original.ContentBox.Height), captured.ContentBox);
        Assert.Equal(1.5, captured.Scale);
        Assert.Equal(4, captured.Margin.Top);
        Assert.Equal(2, captured.Border.Top);
        Assert.Equal(4, captured.Padding.Top);
        var hidden = host.Session.CaptureTree().Nodes.First(n => n.Name == "Hidden");
        Assert.Equal(DevToolsErrors.Unavailable, Assert.Throws<DevToolsException>(() => host.Exporter.Geometry(host.Target, hidden.Id.ToString())).Code);
        Assert.Equal(DevToolsErrors.Unavailable, Assert.Throws<DevToolsException>(() => host.Exporter.Styles(host.Target, "missing")).Code);
        Assert.Equal(DevToolsErrors.TargetPaused, Assert.Throws<DevToolsException>(() => host.Exporter.Styles(host.Target with { Generation = 2, State = "paused", StopId = "stop" }, host.Piece.ToString())).Code);
    }

    [Fact]
    public Task Reading_history_does_not_record_frames_and_copies_only_measured_counts_and_damage()
    {
        using var host = new Harness();
        Assert.Equal(DevToolsErrors.Unavailable, Assert.Throws<DevToolsException>(() => host.Exporter.Frames(host.Target)).Code);
        PixelRect[] damage = [new(1, 2, 30, 40)];
        for (var number = 1; number <= 125; number++) host.Session.RecordFrame(number, FrameCause.Explicit | FrameCause.Interaction,
            TimeSpan.FromMilliseconds(5), TimeSpan.FromMilliseconds(1), TimeSpan.FromMilliseconds(1), TimeSpan.FromMilliseconds(1), TimeSpan.FromMilliseconds(0.5), TimeSpan.FromMilliseconds(1), 2, 3, damage);
        var captures = host.Exporter.Frames(host.Target);
        Assert.Equal(120, captures.Length); Assert.Equal(6, captures[0].Number);
        Assert.Equal(2, captures[0].StylesRecomputed); Assert.Equal(3, captures[0].LayoutCount);
        Assert.False(captures[0].HasNodeCount); Assert.False(captures[0].HasPaintCount);
        Assert.Equal(0.5, captures[0].Timings.OverlayMs); Assert.Contains("Interaction", captures[0].Causes);
        captures[0].Damage[0] = new(100, 100, 1, 1); damage[0] = new(200, 200, 1, 1);
        Assert.Equal(1, host.Exporter.Frames(host.Target)[0].Damage[0].X);
        Assert.Equal(125, host.Session.Frames[^1].Number);
        return DetailExporter.SerializeAsync(new FrameBatch(DetailExporter.Stamp(host.Target), 0, 1, host.Exporter.Frames(host.Target)), TestContext.Current.CancellationToken);
    }

    private sealed class Harness : IDisposable
    {
        private readonly TestHost host = new();
        private readonly SkiaRenderer graphics = new();
        private readonly InputManager input;
        internal Scene Scene { get; }
        internal DevToolsSession Session { get; }
        internal DetailExporter Exporter { get; }
        internal TargetState Target { get; }
        internal int Piece { get; }
        internal void Pump() => host.RunPending();
        internal Harness()
        {
            Scene = new(host.Renderer, graphics.Fonts, graphics.Shaper) { Viewport = new Size(320, 200) };
            Scene.Styles.Theme = InspectionStyles.Theme(); Scene.Styles.Application = InspectionStyles.Application();
            input = new(host.Renderer, Scene); Session = new(host.Renderer, Scene, input);
            host.Render<TreePage>();
            Piece = Session.CaptureTree().Nodes.First(n => n.Name == "Piece").Id;
            Target = new(new(42, "process", "runtime", "app", "window", Session.SessionId), 1, "running", null);
            Exporter = new(Session);
        }
        public void Dispose() { Session.Dispose(); input.Dispose(); Scene.Dispose(); graphics.Dispose(); host.Dispose(); }
    }
}
