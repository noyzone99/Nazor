using System.Globalization;
using System.Text;
using System.Text.Json.Nodes;
using Nazor.DevTools.Protocol;
using Nazor.Diagnostics;
using Nazor.Graphics;
using Nazor.Style;

namespace Nazor.DevTools;

public sealed class DetailExporter(DevToolsSession session)
{
    public StyleDetails Styles(TargetState target, string elementId)
    {
        Check(target);
        var id = Element(elementId);
        session.Scene.Update();
        var trace = session.Trace(id) ?? throw Unavailable("Style tracing is unavailable for this node.");
        if (trace.Rules.Count > 256 || trace.Rules.Sum(r => r.Declarations.Count) + trace.Inline.Count > 2048)
            throw Unavailable("The complete trace exceeds the detail budget.");
        var rules = trace.Rules.Select(r => new Protocol.StyleRule(r.Selector, r.Sheet, null, r.Declarations.Select(Declaration).ToArray())
        {
            Level = r.Level.ToString().ToLowerInvariant(), Order = r.Order,
            Specificity = $"{r.Specificity.Ids},{r.Specificity.Classes},{r.Specificity.Types}", Media = r.Media, MediaMatches = r.MediaMatches
        }).ToList();
        if (trace.Inline.Count > 0) rules.Add(new("element.style", null, null, trace.Inline.Select(Declaration).ToArray())
            { Level = "inline", Order = 0, Specificity = "inline", MediaMatches = true });
        return new(Stamp(target), elementId, trace.Values.Select(Value).ToArray(), rules.ToArray())
            { CustomProperties = trace.CustomProperties.Select(Value).ToArray() };
    }

    public GeometryDetails Geometry(TargetState target, string elementId)
    {
        Check(target);
        var box = session.Box(Element(elementId)) ?? throw Unavailable("No rendered box is available for this node.");
        return new(Stamp(target), elementId, Rect(box.BorderBox), Edges(box.Margin), Edges(box.Border), Edges(box.Padding), box.Scale)
            { MarginBox = Rect(box.MarginBox), PaddingBox = Rect(box.PaddingBox), ContentBox = Rect(box.ContentBox) };
    }

    public FrameRecord[] Frames(TargetState target)
    {
        Check(target);
        var frames = session.Frames;
        if (frames.Count == 0) throw Unavailable("No completed target frame has been recorded.");
        if (frames.Any(f => f.Damage?.Count > 128)) throw Unavailable("The complete frame history exceeds the damage budget.");
        return frames.Select(f => new FrameRecord(f.Number,
            Enum.GetValues<FrameCause>().Where(c => c != FrameCause.None && f.Cause.HasFlag(c)).Select(c => c.ToString()).ToArray(),
            new(f.Styles.TotalMilliseconds, f.Layout.TotalMilliseconds, f.DisplayList.TotalMilliseconds, f.Raster.TotalMilliseconds, f.Total.TotalMilliseconds)
                { OverlayMs = f.Overlay.TotalMilliseconds, ElapsedMs = f.Elapsed.TotalMilliseconds },
            0, f.LayoutComputations, 0, f.Damage?.Select(r => new WireRect(r.X, r.Y, r.Width, r.Height)).ToArray() ?? [])
            {
                StylesRecomputed = f.StylesRecomputed, HasNodeCount = false, HasPaintCount = false, HasDamage = f.Damage is not null,
                Renderer = f.Rendering?.Actual.ToString().ToUpperInvariant(), RequestedRenderer = f.Rendering?.Requested.ToString().ToUpperInvariant(),
                GraphicsBackend = f.Rendering?.Backend, GraphicsDevice = f.Rendering?.Device, FallbackReason = f.Rendering?.FallbackReason,
                RenderWorkerFrame = f.RenderWorkerFrame, RenderWorkerMs = f.RenderWorkerDuration?.TotalMilliseconds, GpuFramesReplaced = f.GpuFramesReplaced,
            }).ToArray();
    }

    public static CaptureStamp Stamp(TargetState target) => new(target.Identity.SessionId, target.Generation, "live", null, DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture));
    public static Task<JsonNode> SerializeAsync<T>(T value, CancellationToken token = default) => Task.Run(() =>
    {
        token.ThrowIfCancellationRequested();
        JsonNode data;
        try { data = DevToolsJson.Write(value); }
        catch (DevToolsException error) when (error.Code == DevToolsErrors.InvalidParams) { throw Unavailable("The complete details exceed the wire contract."); }
        if (Encoding.UTF8.GetByteCount(data.ToJsonString()) > DevToolsProtocol.MaxMessageBytes - 4096) throw Unavailable("The complete details exceed the wire budget.");
        return data;
    }, token);

    private void Check(TargetState target)
    {
        session.Renderer.Dispatcher.AssertAccess();
        if (target.Identity.SessionId != session.SessionId) throw new DevToolsException(DevToolsErrors.StaleSession, "The capture belongs to another session.");
        if (target.State == "paused" || target.StopId is not null) throw new DevToolsException(DevToolsErrors.TargetPaused, "Live detail collection is unavailable while paused.");
        if (target.State != "running") throw Unavailable("The target is not running.");
    }
    private int Element(string id) => int.TryParse(id, NumberStyles.None, CultureInfo.InvariantCulture, out var number) && session.ElementOf(number) is not null
        ? number : throw Unavailable("The selected element is unavailable.");
    private static PropertyValue Declaration(TracedDeclaration d) => new(d.Property, d.Value, d.IsOverridden ? "overridden" : "winning")
        { Longhands = d.Longhands.ToArray(), Winning = d.Winning.ToArray() };
    private static PropertyValue Value(TracedValue v) => new(v.Property, v.Value, v.Source.ToString().ToLowerInvariant()) { Rule = v.Rule, Declaration = v.Declaration };
    private static WireRect Rect(Rect r) => new(r.X, r.Y, r.Width, r.Height);
    private static WireEdges Edges(Thickness e) => new(e.Top, e.Right, e.Bottom, e.Left);
    private static DevToolsException Unavailable(string message) => new(DevToolsErrors.Unavailable, message);
}
