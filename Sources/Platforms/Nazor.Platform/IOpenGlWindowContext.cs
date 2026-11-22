namespace Nazor.Platform;

/// <summary>
/// An OpenGL window context prepared by the UI owner. Initialization, graphics operations and disposal run
/// exclusively on the rendering thread; the native window must outlive disposal. Other graphics APIs can use separate contracts.
/// </summary>
public interface IOpenGlWindowContext : IDisposable
{
    /// <summary>Initializes native resources on the rendering thread before MakeCurrent is called.</summary>
    void Initialize() { }

    /// <summary>Releases display driver references and UI-owned presentation resources after the render worker has released the context.</summary>
    /// <remarks>Called by the UI owner, including failed initialization. The shared worker lives until process exit.</remarks>
    void ReleaseDisplay() { }

    /// <summary>Whether this context implements OpenGL ES rather than desktop OpenGL.</summary>
    bool IsEmbedded { get; }

    /// <summary>Whether Present waits for display synchronization, allowing an immediate next animation frame.</summary>
    bool IsPresentationSynchronized => false;

    /// <summary>Resolves a graphics entry point for the current context.</summary>
    /// <param name="name">The native function name.</param>
    /// <returns>The entry point, or zero when unavailable.</returns>
    nint GetProcAddress(string name);

    /// <summary>Makes the default window framebuffer current and resizes it to physical pixel dimensions.</summary>
    /// <param name="width">The framebuffer width.</param>
    /// <param name="height">The framebuffer height.</param>
    void MakeCurrent(int width, int height);

    /// <summary>Presents the default framebuffer, synchronized to the display when supported by the driver.</summary>
    void Present();
}

internal interface IConditionalOpenGlPresentation
{
    bool TryPresent();
}

/// <summary>A recognized failure to initialize or use a native graphics device.</summary>
public sealed class GraphicsDeviceException : Exception
{
    /// <summary>Creates a device error with its native failure description.</summary>
    /// <param name="message">The device failure.</param>
    /// <param name="innerException">The underlying native error, if available.</param>
    public GraphicsDeviceException(string message, Exception? innerException = null) : base(message, innerException) { }
}
