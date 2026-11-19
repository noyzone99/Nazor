using System.Runtime.InteropServices;

namespace Nazor.Platform.Windows;

internal sealed unsafe class WglWindowContext : IOpenGlWindowContext
{
    private readonly nint window;
    private readonly Action releaseSurface;
    private nint dc, context, library;
    public bool IsEmbedded => false;
    public bool IsPresentationSynchronized { get; private set; }

    internal WglWindowContext(nint window, Action releaseSurface)
    {
        this.window = window;
        this.releaseSurface = releaseSurface;
    }

    public void Initialize()
    {
        try
        {
            dc = GetDC(window);
            if (dc == 0) throw new GraphicsDeviceException("Cannot acquire the OpenGL window device context.");
            var format = new PixelFormatDescriptor
            {
                Size = (ushort)sizeof(PixelFormatDescriptor), Version = 1,
                Flags = 0x00000004 | 0x00000020 | 0x00000001,
                ColorBits = 32, AlphaBits = 8, StencilBits = 8,
            };
            var selected = ChoosePixelFormat(dc, ref format);
            if (selected == 0 || !SetPixelFormat(dc, selected, ref format))
                throw new GraphicsDeviceException("Cannot select an OpenGL window pixel format.");
            context = wglCreateContext(dc);
            if (context == 0 || !wglMakeCurrent(dc, context)) throw new GraphicsDeviceException("Cannot initialize a Windows OpenGL context.");
            library = NativeLibrary.Load("opengl32.dll");
            var interval = GetProcAddress("wglSwapIntervalEXT");
            IsPresentationSynchronized = interval != 0 && ((delegate* unmanaged[Stdcall]<int, int>)interval)(1) != 0;
        }
        catch { Dispose(); throw; }
    }

    public nint GetProcAddress(string name)
    {
        var address = wglGetProcAddress(name);
        return address is 0 or 1 or 2 or 3 or -1
            ? NativeLibrary.TryGetExport(library, name, out var exported) ? exported : 0
            : address;
    }

    public void MakeCurrent(int width, int height)
    {
        if (!wglMakeCurrent(dc, context)) throw new GraphicsDeviceException("The Windows OpenGL context is no longer available.");
    }

    public void Present()
    {
        if (!SwapBuffers(dc)) throw new GraphicsDeviceException("The Windows OpenGL framebuffer could not be presented.");
    }

    public void Dispose()
    {
        if (context != 0) { wglMakeCurrent(0, 0); wglDeleteContext(context); context = 0; }
        if (dc != 0) { ReleaseDC(window, dc); dc = 0; }
        if (library != 0) { NativeLibrary.Free(library); library = 0; }
    }

    public void ReleaseDisplay() => releaseSurface();

    [StructLayout(LayoutKind.Sequential)]
    private struct PixelFormatDescriptor
    {
        internal ushort Size, Version;
        internal uint Flags;
        internal byte PixelType, ColorBits, RedBits, RedShift, GreenBits, GreenShift, BlueBits, BlueShift,
            AlphaBits, AlphaShift, AccumBits, AccumRedBits, AccumGreenBits, AccumBlueBits, AccumAlphaBits,
            DepthBits, StencilBits, AuxBuffers, LayerType, Reserved;
        internal uint LayerMask, VisibleMask, DamageMask;
    }

    [DllImport("user32.dll")] private static extern nint GetDC(nint window);
    [DllImport("user32.dll")] private static extern int ReleaseDC(nint window, nint dc);
    [DllImport("gdi32.dll")] private static extern int ChoosePixelFormat(nint dc, ref PixelFormatDescriptor format);
    [DllImport("gdi32.dll")][return: MarshalAs(UnmanagedType.Bool)] private static extern bool SetPixelFormat(nint dc, int index, ref PixelFormatDescriptor format);
    [DllImport("gdi32.dll")][return: MarshalAs(UnmanagedType.Bool)] private static extern bool SwapBuffers(nint dc);
    [DllImport("opengl32.dll")] private static extern nint wglCreateContext(nint dc);
    [DllImport("opengl32.dll")][return: MarshalAs(UnmanagedType.Bool)] private static extern bool wglDeleteContext(nint context);
    [DllImport("opengl32.dll")][return: MarshalAs(UnmanagedType.Bool)] private static extern bool wglMakeCurrent(nint dc, nint context);
    [DllImport("opengl32.dll", CharSet = CharSet.Ansi)] private static extern nint wglGetProcAddress(string name);
}
