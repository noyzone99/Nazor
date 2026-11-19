using System.Runtime.InteropServices;
using static Nazor.Platform.Windows.Win32;

namespace Nazor.Platform.Windows;

// Presents the existing Skia raster buffer below any child windows owned by the caption.
internal sealed unsafe partial class Win32AlphaSurface : IDisposable
{
    private static readonly Guid DxgiDevice = new("54ec77fa-1377-44e6-8c32-88fd5f44c84c");
    private static readonly Guid CompositionDevice = new("c37ea93a-e7aa-450d-b16f-9746cb0407f3");
    private static readonly Guid DxgiSurface = new("cafcb56c-6ac3-4889-bf47-9e23bbd260ec");
    private static readonly Guid Texture2D = new("6f15aaf2-d208-4e89-9ab4-489535d34f9c");

    private nint d3d, context, composition, target, visual, surface;
    private (int Width, int Height) size;
    private bool presented;

    internal Win32AlphaSurface(nint window)
    {
        try
        {
            const uint hardware = 1, warp = 5, bgraSupport = 0x20, sdkVersion = 7;
            if (D3D11CreateDevice(0, hardware, 0, bgraSupport, null, 0, sdkVersion, out d3d, null, out context) < 0)
                Check(D3D11CreateDevice(0, warp, 0, bgraSupport, null, 0, sdkVersion, out d3d, null, out context));
            Check(Marshal.QueryInterface(d3d, in DxgiDevice, out var dxgi));
            try { Check(DCompositionCreateDevice(dxgi, in CompositionDevice, out composition)); }
            finally { Marshal.Release(dxgi); }

            // Slots follow IDCompositionDevice and IDCompositionTarget in dcomp.h.
            var createTarget = (delegate* unmanaged[Stdcall]<nint, nint, int, nint*, int>)Slot(composition, 6);
            var createVisual = (delegate* unmanaged[Stdcall]<nint, nint*, int>)Slot(composition, 7);
            nint newTarget, newVisual;
            Check(createTarget(composition, window, 0, &newTarget));
            target = newTarget;
            Check(createVisual(composition, &newVisual));
            visual = newVisual;
            var setRoot = (delegate* unmanaged[Stdcall]<nint, nint, int>)Slot(target, 3);
            Check(setRoot(target, visual));
        }
        catch
        {
            Dispose();
            throw;
        }
    }

    internal bool Present(byte* pixels, int width, int height, bool changed)
    {
        if (presented && size == (width, height) && !changed) return false;
        presented = false;
        if (size != (width, height))
        {
            const int bgra8 = 87, premultiplied = 1;
            var createSurface = (delegate* unmanaged[Stdcall]<nint, int, int, int, int, nint*, int>)Slot(composition, 8);
            nint replacement;
            Check(createSurface(composition, width, height, bgra8, premultiplied, &replacement));
            var setContent = (delegate* unmanaged[Stdcall]<nint, nint, int>)Slot(visual, 15);
            try { Check(setContent(visual, replacement)); }
            catch { Marshal.Release(replacement); throw; }
            Release(ref surface);
            surface = replacement;
            size = (width, height);
        }

        // BeginDraw can return an offset into a texture shared by several surfaces.
        var beginDraw = (delegate* unmanaged[Stdcall]<nint, RECT*, in Guid, nint*, POINT*, int>)Slot(surface, 3);
        var endDraw = (delegate* unmanaged[Stdcall]<nint, int>)Slot(surface, 4);
        nint dxgiSurface;
        POINT offset;
        Check(beginDraw(surface, null, in DxgiSurface, &dxgiSurface, &offset));
        int endResult;
        try
        {
            Check(Marshal.QueryInterface(dxgiSurface, in Texture2D, out var texture));
            try
            {
                // ID3D11DeviceContext::UpdateSubresource and Flush in d3d11.h.
                var update = (delegate* unmanaged[Stdcall]<nint, nint, uint, Box*, byte*, uint, uint, void>)Slot(context, 48);
                var flush = (delegate* unmanaged[Stdcall]<nint, void>)Slot(context, 111);
                var box = new Box { Left = offset.X, Top = offset.Y, Right = offset.X + width, Bottom = offset.Y + height, Back = 1 };
                update(context, texture, 0, &box, pixels, (uint)(width * 4), 0);
                flush(context);
            }
            finally { Marshal.Release(texture); }
        }
        finally
        {
            Marshal.Release(dxgiSurface);
            endResult = endDraw(surface);
        }
        Check(endResult);
        var commit = (delegate* unmanaged[Stdcall]<nint, int>)Slot(composition, 3);
        Check(commit(composition));
        presented = true;
        return true;
    }

    public void Dispose()
    {
        Release(ref surface);
        Release(ref visual);
        Release(ref target);
        Release(ref composition);
        Release(ref context);
        Release(ref d3d);
    }

    private static nint Slot(nint instance, int index) => (*(nint**)instance)[index];
    private static void Check(int result) => Marshal.ThrowExceptionForHR(result);

    private static void Release(ref nint instance)
    {
        if (instance == 0) return;
        Marshal.Release(instance);
        instance = 0;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Box { public int Left, Top, Front, Right, Bottom, Back; }

    [LibraryImport("d3d11.dll")]
    private static partial int D3D11CreateDevice(nint adapter, uint driver, nint software, uint flags, uint* levels,
        uint levelCount, uint sdkVersion, out nint device, uint* level, out nint immediateContext);

    [LibraryImport("dcomp.dll")]
    private static partial int DCompositionCreateDevice(nint renderingDevice, in Guid iid, out nint device);
}
