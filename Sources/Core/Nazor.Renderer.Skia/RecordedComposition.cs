using System.Diagnostics;
using Nazor.Graphics;
using SkiaSharp;

namespace Nazor.Skia;

/// <summary>Owns immutable native drawing recordings and detached animation states for render-thread playback.</summary>
public sealed class RecordedComposition : IDisposable
{
    internal sealed record Part(SKPicture? Picture, DrawCommand? State);
    private readonly Part[] parts;
    private readonly CompositionTimeline[] timelines;

    internal RecordedComposition(Part[] parts)
    {
        this.parts = parts;
        timelines = parts.Select(part => part.State).OfType<PushComposition>().Select(push => push.Timeline).ToArray();
    }

    /// <summary>Whether this recording contains detached animation timelines.</summary>
    public bool HasAnimations => timelines.Length > 0;

    /// <summary>Whether at least one captured timeline still needs another frame.</summary>
    public bool IsAnimating => timelines.Any(timeline => Stopwatch.GetElapsedTime(timeline.CapturedAt) < timeline.Duration);

    /// <summary>Draws native recordings with the current detached transform and opacity samples.</summary>
    /// <param name="canvas">The render-thread canvas, owned by the caller.</param>
    public void Draw(SKCanvas canvas)
    {
        foreach (var part in parts)
            if (part.Picture is { } picture) canvas.DrawPicture(picture);
            else if (part.State is { } state) SkiaRenderer.ApplyState(canvas,state);
    }

    /// <summary>Releases all native picture references after playback or replacement.</summary>
    public void Dispose()
    {
        foreach (var part in parts) part.Picture?.Dispose();
    }
}
