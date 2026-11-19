using System.Runtime.InteropServices;

namespace Nazor.Platform.Linux;

internal static unsafe class DrmNative
{
    internal const string Drm = "libdrm.so.2", Gbm = "libgbm.so.1", Egl = "libEGL.so.1";
    internal const uint Xrgb8888 = 0x34325258;

    [StructLayout(LayoutKind.Sequential)]
    internal struct Mode
    {
        internal uint Clock;
        internal ushort HDisplay, HSyncStart, HSyncEnd, HTotal, HSkew, VDisplay, VSyncStart, VSyncEnd, VTotal, VScan;
        internal uint VRefresh, Flags, Type;
        internal fixed byte Name[32];
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct Resources
    {
        internal int CountFbs;
        internal uint* Fbs;
        internal int CountCrtcs;
        internal uint* Crtcs;
        internal int CountConnectors;
        internal uint* Connectors;
        internal int CountEncoders;
        internal uint* Encoders;
        internal uint MinWidth, MaxWidth, MinHeight, MaxHeight;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct Connector
    {
        internal uint Id, EncoderId, Type, TypeId;
        internal int Connection;
        internal uint WidthMm, HeightMm;
        internal int Subpixel, CountModes;
        internal Mode* Modes;
        internal int CountProperties;
        internal uint* Properties;
        internal ulong* PropertyValues;
        internal int CountEncoders;
        internal uint* Encoders;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct Encoder
    {
        internal uint Id, Type, CrtcId, PossibleCrtcs, PossibleClones;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct Crtc
    {
        internal uint Id, BufferId, X, Y, Width, Height;
        internal int ModeValid;
        internal Mode Mode;
        internal int GammaSize;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct PollFd
    {
        internal int Fd;
        internal short Events, ReturnedEvents;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct EventContext
    {
        internal int Version;
        internal nint VBlankHandler, PageFlipHandler, PageFlipHandler2, SequenceHandler;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct CreateDumb
    {
        internal uint Height, Width, Bpp, Flags, Handle, Pitch;
        internal ulong Size;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct MapDumb
    {
        internal uint Handle, Padding;
        internal ulong Offset;
    }

    [DllImport(Drm, SetLastError = true)] internal static extern Resources* drmModeGetResources(int fd);
    [DllImport(Drm)] internal static extern void drmModeFreeResources(Resources* resources);
    [DllImport(Drm)] internal static extern Connector* drmModeGetConnector(int fd, uint connector);
    [DllImport(Drm)] internal static extern void drmModeFreeConnector(Connector* connector);
    [DllImport(Drm)] internal static extern Encoder* drmModeGetEncoder(int fd, uint encoder);
    [DllImport(Drm)] internal static extern void drmModeFreeEncoder(Encoder* encoder);
    [DllImport(Drm)] internal static extern Crtc* drmModeGetCrtc(int fd, uint crtc);
    [DllImport(Drm)] internal static extern void drmModeFreeCrtc(Crtc* crtc);
    [DllImport(Drm, SetLastError = true)] internal static extern int drmModeSetCrtc(int fd, uint crtc, uint fb, uint x, uint y, uint* connectors, int count, Mode* mode);
    [DllImport(Drm, SetLastError = true)] internal static extern int drmModePageFlip(int fd, uint crtc, uint fb, uint flags, nint user);
    [DllImport(Drm, SetLastError = true)] internal static extern int drmHandleEvent(int fd, ref EventContext context);
    [DllImport(Drm, SetLastError = true)] internal static extern int drmModeAddFB2(int fd, uint width, uint height, uint format, uint* handles, uint* pitches, uint* offsets, out uint fb, uint flags);
    [DllImport(Drm, SetLastError = true)] internal static extern int drmModeRmFB(int fd, uint fb);
    [DllImport(Drm, SetLastError = true)] internal static extern int drmIoctl(int fd, nuint request, void* value);
    [DllImport(Gbm)] internal static extern nint gbm_create_device(int fd);
    [DllImport(Gbm)] internal static extern void gbm_device_destroy(nint device);
    [DllImport(Gbm)] internal static extern nint gbm_surface_create(nint device, uint width, uint height, uint format, uint flags);
    [DllImport(Gbm)] internal static extern void gbm_surface_destroy(nint surface);
    [DllImport(Gbm)] internal static extern nint gbm_surface_lock_front_buffer(nint surface);
    [DllImport(Gbm)] internal static extern void gbm_surface_release_buffer(nint surface, nint buffer);
    [DllImport(Gbm)] internal static extern ulong gbm_bo_get_handle(nint buffer);
    [DllImport(Gbm)] internal static extern uint gbm_bo_get_stride(nint buffer);
    [DllImport(Gbm)] internal static extern uint gbm_bo_get_format(nint buffer);
    [DllImport(Gbm)] internal static extern void gbm_bo_set_user_data(nint buffer, nint data, nint destroy);
    [DllImport(Egl)] internal static extern nint eglGetPlatformDisplay(uint platform, nint nativeDisplay, nint* attributes);
    [DllImport(Egl)] internal static extern int eglInitialize(nint display, out int major, out int minor);
    [DllImport(Egl)] internal static extern int eglTerminate(nint display);
    [DllImport(Egl)] internal static extern int eglBindAPI(uint api);
    [DllImport(Egl)] internal static extern int eglChooseConfig(nint display, int* attributes, nint* configs, int size, out int count);
    [DllImport(Egl)] internal static extern int eglGetConfigAttrib(nint display, nint config, int attribute, out int value);
    [DllImport(Egl)] internal static extern nint eglCreateContext(nint display, nint config, nint share, int* attributes);
    [DllImport(Egl)] internal static extern int eglDestroyContext(nint display, nint context);
    [DllImport(Egl)] internal static extern nint eglCreateWindowSurface(nint display, nint config, nint window, int* attributes);
    [DllImport(Egl)] internal static extern int eglDestroySurface(nint display, nint surface);
    [DllImport(Egl)] internal static extern int eglMakeCurrent(nint display, nint draw, nint read, nint context);
    [DllImport(Egl)] internal static extern int eglSwapBuffers(nint display, nint surface);
    [DllImport(Egl)] internal static extern int eglSwapInterval(nint display, int interval);
    [DllImport(Egl)] internal static extern int eglGetError();
    [DllImport(Egl)] internal static extern int eglReleaseThread();
    [DllImport(Egl, CharSet = CharSet.Ansi)] internal static extern nint eglGetProcAddress(string name);
    [DllImport("libc", SetLastError = true)] internal static extern int poll(PollFd* fds, nuint count, int timeout);
    [DllImport("libc", SetLastError = true)] internal static extern int eventfd(uint value, int flags);
    [DllImport("libc", SetLastError = true)] internal static extern nint read(int fd, void* buffer, nuint count);
    [DllImport("libc", SetLastError = true)] internal static extern nint write(int fd, void* buffer, nuint count);
    [DllImport("libc", SetLastError = true)] internal static extern int open([MarshalAs(UnmanagedType.LPUTF8Str)] string path, int flags);
    [DllImport("libc", SetLastError = true)] internal static extern int close(int fd);
    [DllImport("libc", SetLastError = true)] internal static extern int fcntl(int fd, int command, int value);
    [DllImport("libc", SetLastError = true)] internal static extern nint mmap(nint address, nuint size, int protection, int flags, int fd, long offset);
    [DllImport("libc", SetLastError = true)] internal static extern int munmap(nint address, nuint size);
    [DllImport("libc", SetLastError = true)] internal static extern int ioctl(int fd, nuint request, ref int value);
    [DllImport("libc", EntryPoint = "ioctl", SetLastError = true)] internal static extern int ioctl_value(int fd, nuint request, int value);
    [DllImport("libc")] internal static extern int tcgetattr(int fd, void* attributes);
    [DllImport("libc")] internal static extern int tcsetattr(int fd, int action, void* attributes);
    [DllImport("libc")] internal static extern void cfmakeraw(void* attributes);
}
