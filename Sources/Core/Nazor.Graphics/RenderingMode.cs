namespace Nazor.Graphics;

/// <summary>Selects hardware rendering, software rendering, or hardware with an initialization fallback.</summary>
public enum RenderingMode
{
    /// <summary>Uses a hardware GPU when available and otherwise uses the CPU.</summary>
    Auto,
    /// <summary>Requires a hardware GPU; initialization failure is an error.</summary>
    Gpu,
    /// <summary>Uses the software rasterizer without creating a GPU context.</summary>
    Cpu,
}

/// <summary>Describes the requested renderer and the renderer actually selected for a window.</summary>
/// <param name="Requested">The startup preference.</param>
/// <param name="Actual">The selected GPU or CPU mode.</param>
/// <param name="Backend">The graphics API or software implementation.</param>
/// <param name="Device">The device reported by the graphics driver, when available.</param>
/// <param name="FallbackReason">The reason automatic GPU initialization failed, when applicable.</param>
public sealed record RenderingInfo(RenderingMode Requested, RenderingMode Actual, string Backend, string? Device = null, string? FallbackReason = null);
