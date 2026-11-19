using System.Diagnostics;
using Nazor.Graphics;

namespace Nazor.Diagnostics;

/// <summary>Identifies why a frame was requested; values can be combined.</summary>
[Flags]
public enum FrameCause
{
    /// <summary>No frame cause is recorded.</summary>
    None = 0,

    /// <summary>The platform requested painting.</summary>
    Paint = 1,

    /// <summary>A component render batch changed the tree.</summary>
    RenderBatch = 2,

    /// <summary>Pointer, keyboard, or focus interaction changed visual state.</summary>
    Interaction = 4,

    /// <summary>The viewport size or scale changed.</summary>
    Resize = 8,

    /// <summary>An animation or transition requested another frame.</summary>
    Animation = 16,

    /// <summary>A hot reload update changed code or styles.</summary>
    HotReload = 32,

    /// <summary>The diagnostics overlay changed.</summary>
    DevTools = 64,

    /// <summary>Image loading changed the visual content.</summary>
    Image = 128,

    /// <summary>A system preference or appearance setting changed.</summary>
    SystemSettings = 256,

    /// <summary>Application code explicitly requested a frame.</summary>
    Explicit = 512,
}

/// <summary>A rasterized frame, with timings and counters from its actual scene update.</summary>
/// <param name="Number">The number (int).</param>
/// <param name="Elapsed">The elapsed (TimeSpan).</param>
/// <param name="Cause">The reason this frame was requested, recorded by diagnostics.</param>
/// <param name="Total">The total (TimeSpan).</param>
/// <param name="Styles">The styles (TimeSpan).</param>
/// <param name="Layout">The layout (TimeSpan).</param>
/// <param name="DisplayList">The display list (TimeSpan).</param>
/// <param name="Overlay">The overlay (TimeSpan).</param>
/// <param name="Raster">UI software rasterization or GPU recording/submission time.</param>
/// <param name="StylesRecomputed">The styles recomputed (int).</param>
/// <param name="LayoutComputations">The layout computations (int).</param>
/// <param name="Damage">The damage (IReadOnlyList&lt;PixelRect&gt;?).</param>
public sealed record FrameInfo(int Number, TimeSpan Elapsed, FrameCause Cause, TimeSpan Total, TimeSpan Styles, TimeSpan Layout,
    TimeSpan DisplayList, TimeSpan Overlay, TimeSpan Raster, int StylesRecomputed, int LayoutComputations, IReadOnlyList<PixelRect>? Damage)
{
    /// <summary>Gets the renderer selected for the UI frame, when supplied by the host.</summary>
    public RenderingInfo? Rendering { get; init; }
    /// <summary>Gets the independently numbered last completed GPU worker frame.</summary>
    public long? RenderWorkerFrame { get; init; }
    /// <summary>Gets that worker frame's CPU submission and presentation wait, excluding UI work and GPU timestamps.</summary>
    public TimeSpan? RenderWorkerDuration { get; init; }
    /// <summary>Gets the cumulative count of replaced pending GPU frames.</summary>
    public long? GpuFramesReplaced { get; init; }
}

/// <summary>Inspects a live window's component tree, visual state, and input selection.</summary>
public sealed partial class DevToolsSession
{
    private readonly Queue<FrameInfo> frames = new();
    private readonly long started = Stopwatch.GetTimestamp();

    /// <summary>The last 120 frames, oldest first. Reading them never asks the app for a frame.</summary>
    public IReadOnlyList<FrameInfo> Frames => frames.ToArray();

    /// <summary>Occurs when frame recorded is reported.</summary>
    public event Action<FrameInfo>? FrameRecorded;

    /// <summary>Records completed UI paint work before backend presentation completes. Optional GPU metrics describe the last completed worker frame.</summary>
    /// <param name="number">The number (int).</param>
    /// <param name="cause">The reason this frame was requested, recorded by diagnostics.</param>
    /// <param name="total">The total (TimeSpan).</param>
    /// <param name="styles">The styles (TimeSpan).</param>
    /// <param name="layout">The layout (TimeSpan).</param>
    /// <param name="displayList">Application display-list construction time.</param>
    /// <param name="overlay">The overlay (TimeSpan).</param>
    /// <param name="raster">Software rasterization or GPU recording and submission time on the UI thread.</param>
    /// <param name="stylesRecomputed">The styles recomputed (int).</param>
    /// <param name="layoutComputations">The layout computations (int).</param>
    /// <param name="damage">The damage (IReadOnlyList&lt;PixelRect&gt;?).</param>
    /// <param name="rendering">The requested and effective renderer, when available.</param>
    /// <param name="renderWorkerFrame">The last completed GPU worker frame number.</param>
    /// <param name="renderWorkerDuration">CPU submission plus presentation wait for that worker frame.</param>
    /// <param name="gpuFramesReplaced">The cumulative number of replaced pending GPU frames.</param>
    public void RecordFrame(int number, FrameCause cause, TimeSpan total, TimeSpan styles, TimeSpan layout,
        TimeSpan displayList, TimeSpan overlay, TimeSpan raster, int stylesRecomputed, int layoutComputations, IReadOnlyList<PixelRect>? damage,
        RenderingInfo? rendering = null, long? renderWorkerFrame = null, TimeSpan? renderWorkerDuration = null, long? gpuFramesReplaced = null)
    {
        Renderer.Dispatcher.AssertAccess();
        if (disposed) return;
        var frame = new FrameInfo(number, Stopwatch.GetElapsedTime(started), cause, total, styles, layout, displayList, overlay, raster,
            stylesRecomputed, layoutComputations, damage?.ToArray())
        {
            Rendering = rendering, RenderWorkerFrame = renderWorkerFrame, RenderWorkerDuration = renderWorkerDuration,
            GpuFramesReplaced = gpuFramesReplaced,
        };
        if (frames.Count == 120) frames.Dequeue();
        frames.Enqueue(frame);
        FrameRecorded?.Invoke(frame);
    }
}
