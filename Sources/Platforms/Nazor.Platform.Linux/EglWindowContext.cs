using System.Runtime.InteropServices;

namespace Nazor.Platform.Linux;

internal sealed unsafe class EglWindowContext : IOpenGlWindowContext
{
    private static readonly Lock displayGate = new();
    private static readonly Dictionary<nint, (nint Display, int Users)> displays = [];
    private readonly nint nativeDisplay;
    private readonly nint nativeWindow;
    private readonly bool isX11;
    private readonly LinuxNativeApi native;
    private nint display, window, surface, context, config;
    private int invalidDrawable;
    public bool IsEmbedded => true;

    internal EglWindowContext(nint nativeWindow, LinuxNativeApi native, bool isX11)
    {
        this.nativeWindow = nativeWindow;
        this.native = native;
        this.isX11 = isX11;
        nativeDisplay = native.nz_window_native_display(nativeWindow);
    }

    public void Initialize()
    {
        try
        {
            if (isX11 && native.NativeVisual == null) throw new GraphicsDeviceException("The X11 native bridge does not expose its EGL visual. Rebuild matching native assets.");
            lock (displayGate)
            {
                if (displays.TryGetValue(nativeDisplay, out var shared))
                {
                    display = shared.Display;
                    displays[nativeDisplay] = (display, shared.Users + 1);
                }
                else
                {
                    var initialized = eglGetPlatformDisplay(isX11 ? 0x31D5u : 0x31D8u, nativeDisplay, null);
                    if (initialized == 0) throw Error("Cannot acquire an EGL Wayland display");
                    if (eglInitialize(initialized, out _, out _) == 0)
                    {
                        var error = Error("Cannot initialize an EGL Wayland display");
                        eglTerminate(initialized);
                        throw error;
                    }
                    display = initialized;
                    displays.Add(nativeDisplay, (display, 1));
                }
            }
            if (eglBindAPI(0x30A0) == 0) throw Error("Cannot select the OpenGL ES API");
            int* attributes = stackalloc int[] { 0x3033, 4, 0x3040, 4, 0x3024, 8, 0x3023, 8, 0x3022, 8, 0x3021, isX11 ? 0 : 8, 0x3026, 8, 0x3038, 0x3038, 0x3038 };
            if (isX11) { attributes[14] = 0x302E; attributes[15] = native.NativeVisual(nativeWindow); }
            if (eglChooseConfig(display, attributes, out config, 1, out var count) == 0 || count == 0)
                throw Error("No RGBA8 EGL window configuration is available");
            int* options = stackalloc int[] { 0x3098, 3, 0x3038 };
            context = eglCreateContext(display, config, 0, options);
            if (context == 0) { options[1] = 2; context = eglCreateContext(display, config, 0, options); }
            if (context == 0) throw Error("Cannot create an OpenGL ES context");
            window = native.nz_window_native_surface(nativeWindow);
            if (isX11 && native.RenderSurface != null) window = native.RenderSurface(nativeWindow);
            else if (!isX11) window = wl_egl_window_create(window, 1, 1);
            if (window == 0) throw new GraphicsDeviceException("Cannot create the Wayland EGL window.");
            surface = eglCreateWindowSurface(display, config, window, null);
            if (surface == 0) throw Error("Cannot create the EGL window surface");
            MakeCurrent(1, 1);
            eglSwapInterval(display, 0);
        }
        catch { Dispose(); throw; }
    }

    public nint GetProcAddress(string name) => eglGetProcAddress(name);

    internal void InvalidateDrawable() => Interlocked.Exchange(ref invalidDrawable, 1);

    public void MakeCurrent(int width, int height)
    {
        var rebound = false;
        if (!isX11) wl_egl_window_resize(window, width, height, 0, 0);
        else if (Interlocked.Exchange(ref invalidDrawable, 0) != 0)
        {
            // The window manager may replace the redirected drawable when it reparents the XID.
            if (eglMakeCurrent(display, 0, 0, 0) == 0) throw Error("Cannot release the previous EGL window surface");
            if (eglDestroySurface(display, surface) == 0) throw Error("Cannot destroy the previous EGL window surface");
            surface = eglCreateWindowSurface(display, config, window, null);
            if (surface == 0) throw Error("Cannot recreate the EGL window surface");
            rebound = true;
        }
        if (eglMakeCurrent(display, surface, surface, context) == 0) throw Error("The EGL window context is no longer available");
        if (rebound) eglSwapInterval(display, 0);
    }

    public void Present()
    {
        if (eglSwapBuffers(display, surface) == 0) throw Error("The EGL window framebuffer could not be presented");
    }

    public void Dispose()
    {
        if (display != 0)
        {
            eglMakeCurrent(display, 0, 0, 0);
            if (surface != 0) { eglDestroySurface(display, surface); surface = 0; }
            if (context != 0) { eglDestroyContext(display, context); context = 0; }
        }
        if (window != 0) { if (!isX11) wl_egl_window_destroy(window); window = 0; }
        eglReleaseThread();
    }

    public void ReleaseDisplay()
    {
        if (display != 0)
        {
            lock (displayGate)
            {
                var shared = displays[nativeDisplay];
                if (shared.Users == 1) { displays.Remove(nativeDisplay); eglTerminate(display); }
                else displays[nativeDisplay] = (display, shared.Users - 1);
            }
            display = 0;
        }
    }

    private static GraphicsDeviceException Error(string operation) => new($"{operation} (EGL error 0x{eglGetError():X}).");

    [DllImport("libEGL.so.1")] private static extern nint eglGetPlatformDisplay(uint platform, nint display, nint* attributes);
    [DllImport("libEGL.so.1")] private static extern int eglInitialize(nint display, out int major, out int minor);
    [DllImport("libEGL.so.1")] private static extern int eglTerminate(nint display);
    [DllImport("libEGL.so.1")] private static extern int eglBindAPI(uint api);
    [DllImport("libEGL.so.1")] private static extern int eglChooseConfig(nint display, int* attributes, out nint config, int size, out int count);
    [DllImport("libEGL.so.1")] private static extern nint eglCreateContext(nint display, nint config, nint share, int* attributes);
    [DllImport("libEGL.so.1")] private static extern int eglDestroyContext(nint display, nint context);
    [DllImport("libEGL.so.1")] private static extern nint eglCreateWindowSurface(nint display, nint config, nint window, int* attributes);
    [DllImport("libEGL.so.1")] private static extern int eglDestroySurface(nint display, nint surface);
    [DllImport("libEGL.so.1")] private static extern int eglMakeCurrent(nint display, nint draw, nint read, nint context);
    [DllImport("libEGL.so.1")] private static extern int eglSwapBuffers(nint display, nint surface);
    [DllImport("libEGL.so.1")] private static extern int eglSwapInterval(nint display, int interval);
    [DllImport("libEGL.so.1")] private static extern int eglGetError();
    [DllImport("libEGL.so.1")] private static extern int eglReleaseThread();
    [DllImport("libEGL.so.1", CharSet = CharSet.Ansi)] private static extern nint eglGetProcAddress(string name);
    [DllImport("libwayland-egl.so.1")] private static extern nint wl_egl_window_create(nint surface, int width, int height);
    [DllImport("libwayland-egl.so.1")] private static extern void wl_egl_window_resize(nint window, int width, int height, int dx, int dy);
    [DllImport("libwayland-egl.so.1")] private static extern void wl_egl_window_destroy(nint window);
}
