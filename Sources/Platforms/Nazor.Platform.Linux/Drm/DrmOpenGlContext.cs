using System.Runtime.InteropServices;
using static Nazor.Platform.Linux.DrmNative;

namespace Nazor.Platform.Linux;

internal sealed unsafe class DrmOpenGlContext : IOpenGlWindowContext, IConditionalOpenGlPresentation
{
    private readonly DrmDisplay output;
    private readonly Dictionary<nint, uint> framebuffers = [];
    private readonly HashSet<nint> lockedBuffers = [];
    private readonly BufferDestroyed bufferDestroyed;
    private nint device, window, display, surface, context, currentBuffer, queuedBuffer;
    private bool presentationDeferred;
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void BufferDestroyed(nint buffer, nint data);

    internal DrmOpenGlContext(DrmDisplay output)
    {
        this.output = output;
        bufferDestroyed = (buffer, _) =>
        {
            if (framebuffers.Remove(buffer, out var framebuffer)) drmModeRmFB(output.Fd, framebuffer);
        };
    }
    public bool IsEmbedded => true;
    public bool IsPresentationSynchronized => !presentationDeferred;

    public void Initialize()
    {
        try
        {
            device = gbm_create_device(output.Fd);
            if (device == 0) throw new GraphicsDeviceException("Cannot create the GBM device.");
            window = gbm_surface_create(device, (uint)output.Width, (uint)output.Height, Xrgb8888, 1 | 4);
            if (window == 0) throw new GraphicsDeviceException("Cannot create an XRGB8888 GBM scanout surface.");
            display = eglGetPlatformDisplay(0x31d7, device, null);
            if (display == 0 || eglInitialize(display, out _, out _) == 0) throw Error("Cannot initialize the EGL GBM display");
            if (eglBindAPI(0x30a0) == 0) throw Error("Cannot select OpenGL ES");
            int* attributes = stackalloc int[] { 0x3033, 4, 0x3040, 4, 0x3024, 8, 0x3023, 8, 0x3022, 8, 0x3026, 8, 0x3038 };
            nint* configurations = stackalloc nint[256];
            if (eglChooseConfig(display, attributes, configurations, 256, out var count) == 0) throw Error("Cannot enumerate EGL configurations");
            nint config = 0;
            for (var index = 0; index < count; index++)
                if (eglGetConfigAttrib(display, configurations[index], 0x302e, out var format) != 0 && (uint)format == Xrgb8888)
                { config = configurations[index]; break; }
            if (config == 0) throw new GraphicsDeviceException("No EGL configuration matches the GBM scanout format.");
            int* options = stackalloc int[] { 0x3098, 3, 0x3038 };
            context = eglCreateContext(display, config, 0, options);
            if (context == 0) { options[1] = 2; context = eglCreateContext(display, config, 0, options); }
            if (context == 0) throw Error("Cannot create the DRM OpenGL ES context");
            surface = eglCreateWindowSurface(display, config, window, null);
            if (surface == 0) throw Error("Cannot create the EGL GBM surface");
            MakeCurrent(output.Width, output.Height);
            eglSwapInterval(display, 0);
        }
        catch { Dispose(); throw; }
    }

    public nint GetProcAddress(string name) => eglGetProcAddress(name);

    public void MakeCurrent(int width, int height)
    {
        if (eglMakeCurrent(display, surface, surface, context) == 0) throw Error("Cannot make the DRM graphics context current");
    }

    public void Present() => TryPresent();

    public bool TryPresent()
    {
        presentationDeferred = true;
        if (queuedBuffer != 0)
        {
            if (output.ResumePresent(framebuffers[queuedBuffer]) != DrmPresentation.Presented) return false;
            CompletePresentation(queuedBuffer);
            queuedBuffer = 0;
        }
        if (eglSwapBuffers(display, surface) == 0) throw Error("Cannot swap the EGL GBM buffers");
        var next = gbm_surface_lock_front_buffer(window);
        if (next == 0) throw new GraphicsDeviceException("No GBM scanout buffer is available.");
        lockedBuffers.Add(next);
        if (!framebuffers.TryGetValue(next, out var framebuffer))
        {
            framebuffer = output.RegisterGbmBuffer(next);
            framebuffers.Add(next, framebuffer);
            gbm_bo_set_user_data(next, (nint)framebuffer, Marshal.GetFunctionPointerForDelegate(bufferDestroyed));
        }
        switch (output.Present(framebuffer))
        {
            case DrmPresentation.Deferred: ReleaseBuffer(next); return false;
            case DrmPresentation.Queued: queuedBuffer = next; return false;
            default:
                CompletePresentation(next);
                presentationDeferred = false;
                return true;
        }
    }

    private void CompletePresentation(nint next)
    {
        if (currentBuffer != 0) ReleaseBuffer(currentBuffer);
        currentBuffer = next;
    }

    private void ReleaseBuffer(nint buffer)
    {
        gbm_surface_release_buffer(window, buffer);
        lockedBuffers.Remove(buffer);
    }

    public void Dispose()
    {
        try { if (lockedBuffers.Count != 0) output.Restore(); }
        finally
        {
            if (display != 0)
            {
                eglMakeCurrent(display, 0, 0, 0);
                if (surface != 0) { eglDestroySurface(display, surface); surface = 0; }
                if (context != 0) { eglDestroyContext(display, context); context = 0; }
            }
            foreach (var entry in framebuffers)
            {
                gbm_bo_set_user_data(entry.Key, 0, 0);
                drmModeRmFB(output.Fd, entry.Value);
            }
            framebuffers.Clear();
            foreach (var buffer in lockedBuffers) gbm_surface_release_buffer(window, buffer);
            lockedBuffers.Clear(); currentBuffer = queuedBuffer = 0;
            if (window != 0) { gbm_surface_destroy(window); window = 0; }
            eglReleaseThread();
            GC.KeepAlive(bufferDestroyed);
        }
    }

    public void ReleaseDisplay()
    {
        if (display != 0) { eglTerminate(display); display = 0; }
        if (device != 0) { gbm_device_destroy(device); device = 0; }
    }

    private static GraphicsDeviceException Error(string message) => new($"{message} (EGL error 0x{eglGetError():X}).");
}
