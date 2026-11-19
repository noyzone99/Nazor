namespace Nazor.Graphics;

/// <summary>An immutable snapshot of a visual transform and group opacity animation, safe for a render worker.</summary>
public abstract class CompositionTimeline
{
    /// <summary>Gets the monotonic Stopwatch timestamp of the snapshot.</summary>
    public abstract long CapturedAt { get; }

    /// <summary>Gets the remaining animation duration, including delay; MaxValue represents unbounded repetition.</summary>
    public abstract TimeSpan Duration { get; }

    /// <summary>Samples this detached timeline without accessing UI objects.</summary>
    /// <param name="elapsed">The elapsed time since capture.</param>
    /// <returns>The visual transform and clamped group opacity.</returns>
    public abstract (Matrix Transform, float Opacity) Sample(TimeSpan elapsed);
}

/// <summary>Pushes a detached animated transform and opacity; Pop restores the previous state.</summary>
/// <param name="Timeline">The immutable, render-thread-safe animation snapshot.</param>
public sealed record PushComposition(CompositionTimeline Timeline) : DrawCommand;
