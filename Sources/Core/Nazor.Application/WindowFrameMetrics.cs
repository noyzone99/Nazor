using Nazor.Graphics;

namespace Nazor;

/// <summary>Completion of backend submission for a UI frame; does not establish physical display scanout.</summary>
/// <param name="UiFrame">The UI frame whose drawing was submitted.</param>
/// <param name="Renderer">The effective renderer.</param>
/// <param name="RenderWorkerFrame">The independent GPU worker frame number, or null for software.</param>
/// <param name="BackendDuration">CPU backend submission and presentation wait, excluding UI drawing and GPU execution timestamps.</param>
public sealed record PresentedFrame(int UiFrame, RenderingMode Renderer, long? RenderWorkerFrame, TimeSpan BackendDuration);

/// <summary>Identifies the combined reasons for a window frame when an instrumentation integration is attached.</summary>
[Flags]
public enum WindowFrameCause
{
    /// <summary>No cause is recorded.</summary>
    None = 0,
    /// <summary>The backend requested painting.</summary>
    Paint = 1,
    /// <summary>A component render batch changed the tree.</summary>
    RenderBatch = 2,
    /// <summary>Pointer, keyboard or focus interaction changed visual state.</summary>
    Interaction = 4,
    /// <summary>The viewport size or scale changed.</summary>
    Resize = 8,
    /// <summary>An animation or transition requested another frame.</summary>
    Animation = 16,
    /// <summary>A Hot Reload update changed code or styles.</summary>
    HotReload = 32,
    /// <summary>An optional overlay changed.</summary>
    Overlay = 64,
    /// <summary>Image loading changed the visual content.</summary>
    Image = 128,
    /// <summary>System appearance or preferences changed.</summary>
    SystemSettings = 256,
    /// <summary>Application code explicitly requested a frame.</summary>
    Explicit = 512,
}

/// <summary>Completed UI paint work. GPU raster slots measure recording/submission, not GPU execution.</summary>
/// <param name="Number">The window's frame number.</param>
/// <param name="Cause">The combined reasons for the frame.</param>
/// <param name="Total">Total frame work including window bookkeeping.</param>
/// <param name="Styles">Style resolution time.</param>
/// <param name="Layout">Layout time.</param>
/// <param name="DisplayList">Application display-list construction time.</param>
/// <param name="Overlay">Instrumentation overlay composition time.</param>
/// <param name="Raster">Software rasterization or GPU recording/submission time on the UI thread.</param>
/// <param name="StylesRecomputed">Styles recomputed before overlay/accessibility queries.</param>
/// <param name="LayoutComputations">Layout computations before overlay/accessibility queries.</param>
/// <param name="Damage">Physical damage rectangles, or null for a full repaint. Copy before retaining them.</param>
public readonly record struct WindowFrameMetrics(int Number, WindowFrameCause Cause, TimeSpan Total, TimeSpan Styles,
    TimeSpan Layout, TimeSpan DisplayList, TimeSpan Overlay, TimeSpan Raster, int StylesRecomputed,
    int LayoutComputations, IReadOnlyList<PixelRect>? Damage)
{
    /// <summary>Gets the renderer used by this UI frame, when reported by a native host.</summary>
    public RenderingInfo? Rendering { get; init; }
    /// <summary>Gets the last completed GPU worker frame, independently of the UI frame number.</summary>
    public long? RenderWorkerFrame { get; init; }
    /// <summary>Gets CPU command submission plus presentation wait for RenderWorkerFrame; not GPU execution time.</summary>
    public TimeSpan? RenderWorkerDuration { get; init; }
    /// <summary>Gets the cumulative number of pending GPU frames replaced by newer complete frames.</summary>
    public long? GpuFramesReplaced { get; init; }
}
