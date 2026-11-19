using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using static Nazor.Platform.Linux.DrmNative;

namespace Nazor.Platform.Linux;

internal sealed unsafe class DrmDisplay : IDisposable
{
    private readonly DrmSession session;
    private readonly int deviceId;
    private readonly uint connector, crtc;
    private readonly Mode mode;
    private readonly Crtc previous;
    private readonly uint[] previousConnectors;
    private readonly FlipHandler flipHandler;
    private bool modeset, waitingForFlip, disposed;
    private readonly List<DumbBuffer> software = [];
    private int nextSoftware;
    internal int Fd { get; }
    internal int Width => mode.HDisplay;
    internal int Height => mode.VDisplay;

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void FlipHandler(int fd, uint sequence, uint seconds, uint microseconds, nint user);

    internal DrmDisplay(DrmSession session, DrmOptions options)
    {
        this.session = session;
        flipHandler = (_, _, _, _, _) => waitingForFlip = false;
        var paths = options.DevicePath is { } path ? [path] : Directory.Exists("/dev/dri")
            ? Directory.GetFiles("/dev/dri", "card*").Order(StringComparer.Ordinal).ToArray() : [];
        foreach (var candidate in paths)
        {
            var opened = session.OpenDevice(candidate);
            if (opened.Id < 0) continue;
            var resources = drmModeGetResources(opened.Fd);
            try
            {
                if (resources == null) continue;
                for (var index = 0; index < resources->CountConnectors; index++)
                {
                    var output = drmModeGetConnector(opened.Fd, resources->Connectors[index]);
                    try
                    {
                        if (output == null || output->Connection != 1 || output->CountModes <= 0 ||
                            options.ConnectorId is { } requested && requested != output->Id) continue;
                        var selectedCrtc = SelectCrtc(opened.Fd, resources, output);
                        if (selectedCrtc == 0) continue;
                        var saved = drmModeGetCrtc(opened.Fd, selectedCrtc);
                        if (saved == null) continue;
                        try { previous = *saved; }
                        finally { drmModeFreeCrtc(saved); }
                        var modes = new ReadOnlySpan<Mode>(output->Modes, output->CountModes).ToArray();
                        mode = modes.OrderByDescending(value => (value.Type & 8) != 0)
                            .ThenByDescending(value => (long)value.HDisplay * value.VDisplay)
                            .ThenByDescending(value => value.VRefresh).First();
                        if (Width == 0 || Height == 0) continue;
                        previousConnectors = FindConnectors(opened.Fd, resources, selectedCrtc);
                        connector = output->Id; crtc = selectedCrtc; Fd = opened.Fd; deviceId = opened.Id;
                        return;
                    }
                    finally { if (output != null) drmModeFreeConnector(output); }
                }
            }
            finally
            {
                if (resources != null) drmModeFreeResources(resources);
                if (connector == 0) session.CloseDevice(opened.Id);
            }
        }
        throw new InvalidOperationException("No accessible connected DRM output was found. Select a primary card/connector and an active local seat.");
    }

    private static uint SelectCrtc(int fd, Resources* resources, Connector* connector)
    {
        for (var index = 0; index < connector->CountEncoders; index++)
        {
            var encoder = drmModeGetEncoder(fd, connector->Encoders[index]);
            if (encoder == null) continue;
            try
            {
                if (encoder->CrtcId != 0) return encoder->CrtcId;
                for (var crtc = 0; crtc < Math.Min(resources->CountCrtcs, 32); crtc++)
                    if ((encoder->PossibleCrtcs & (1u << crtc)) != 0) return resources->Crtcs[crtc];
            }
            finally { drmModeFreeEncoder(encoder); }
        }
        return 0;
    }

    private static uint[] FindConnectors(int fd, Resources* resources, uint crtc)
    {
        var result = new List<uint>();
        for (var index = 0; index < resources->CountConnectors; index++)
        {
            var output = drmModeGetConnector(fd, resources->Connectors[index]);
            if (output == null) continue;
            try
            {
                var encoder = drmModeGetEncoder(fd, output->EncoderId);
                if (encoder == null) continue;
                try { if (encoder->CrtcId == crtc) result.Add(output->Id); }
                finally { drmModeFreeEncoder(encoder); }
            }
            finally { drmModeFreeConnector(output); }
        }
        return result.ToArray();
    }

    internal void Present(uint framebuffer)
    {
        if (!modeset)
        {
            var output = connector;
            var selectedMode = mode;
            if (drmModeSetCrtc(Fd, crtc, framebuffer, 0, 0, &output, 1, &selectedMode) != 0)
                throw Error("Cannot configure the DRM output");
            modeset = true;
            return;
        }
        if (waitingForFlip) throw new GraphicsDeviceException("The previous DRM page flip has not completed.");
        if (drmModePageFlip(Fd, crtc, framebuffer, 1, 0) != 0) throw Error("Cannot queue the DRM page flip");
        waitingForFlip = true;
        WaitForFlip();
    }

    private void WaitForFlip()
    {
        var deadline = Stopwatch.GetTimestamp();
        var context = new EventContext { Version = 2, PageFlipHandler = Marshal.GetFunctionPointerForDelegate(flipHandler) };
        while (waitingForFlip)
        {
            if (Stopwatch.GetElapsedTime(deadline) > TimeSpan.FromSeconds(2))
                throw new GraphicsDeviceException("The DRM page flip timed out. Check the display connection and active seat.");
            var descriptor = new PollFd { Fd = Fd, Events = 1 };
            var result = poll(&descriptor, 1, 50);
            if (result < 0 && Marshal.GetLastPInvokeError() == 4) continue;
            if (result < 0 || (descriptor.ReturnedEvents & (8 | 16 | 32)) != 0) throw Error("The DRM device is unavailable");
            if (result > 0 && drmHandleEvent(Fd, ref context) != 0) throw Error("Cannot read the DRM page flip event");
        }
    }

    internal uint RegisterBuffer(uint handle, uint pitch, uint format)
    {
        uint* handles = stackalloc uint[4] { handle, 0, 0, 0 };
        uint* pitches = stackalloc uint[4] { pitch, 0, 0, 0 };
        uint* offsets = stackalloc uint[4] { 0, 0, 0, 0 };
        if (drmModeAddFB2(Fd, (uint)Width, (uint)Height, format, handles, pitches, offsets, out var framebuffer, 0) != 0)
            throw Error("Cannot register a DRM framebuffer");
        return framebuffer;
    }

    internal void PresentSoftware(PaintTarget target)
    {
        if (software.Count == 0)
        {
            try { software.Add(new(this)); software.Add(new(this)); }
            catch { foreach (var allocated in software) allocated.Dispose(); software.Clear(); throw; }
        }
        var buffer = software[nextSoftware];
        for (var row = 0; row < Height; row++)
            NativeMemory.Copy((byte*)target.Pixels + (long)row * target.Stride, (byte*)buffer.Pixels + (long)row * buffer.Pitch, (nuint)Width * 4);
        Present(buffer.Framebuffer);
        nextSoftware = 1 - nextSoftware;
    }

    internal void Restore()
    {
        if (!modeset) return;
        fixed (uint* outputs = previousConnectors)
        {
            var savedMode = previous.Mode;
            if (drmModeSetCrtc(Fd, crtc, previous.BufferId, previous.X, previous.Y, outputs,
                previous.ModeValid != 0 ? previousConnectors.Length : 0, previous.ModeValid != 0 ? &savedMode : null) != 0)
                throw Error("Cannot restore the previous DRM display state");
        }
        modeset = false;
        if (waitingForFlip) WaitForFlip();
    }

    internal static GraphicsDeviceException Error(string message) => new($"{message}: {new Win32Exception(Marshal.GetLastPInvokeError()).Message}.");

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        try { Restore(); }
        finally
        {
            foreach (var buffer in software) buffer.Dispose();
            session.CloseDevice(deviceId);
            GC.KeepAlive(flipHandler);
        }
    }

    private sealed class DumbBuffer : IDisposable
    {
        private readonly DrmDisplay display;
        private CreateDumb allocation;
        internal nint Pixels { get; private set; }
        internal uint Pitch => allocation.Pitch;
        internal uint Framebuffer { get; private set; }

        internal DumbBuffer(DrmDisplay display)
        {
            this.display = display;
            allocation = new() { Width = (uint)display.Width, Height = (uint)display.Height, Bpp = 32 };
            try
            {
                fixed (CreateDumb* value = &allocation)
                    if (drmIoctl(display.Fd, 0xc02064b2, value) != 0) throw Error("Cannot allocate a DRM software buffer");
                var mapping = new MapDumb { Handle = allocation.Handle };
                if (drmIoctl(display.Fd, 0xc01064b3, &mapping) != 0) throw Error("Cannot map a DRM software buffer");
                Pixels = mmap(0, (nuint)allocation.Size, 3, 1, display.Fd, checked((long)mapping.Offset));
                if (Pixels == -1) { Pixels = 0; throw Error("Cannot access a DRM software buffer"); }
                Framebuffer = display.RegisterBuffer(allocation.Handle, allocation.Pitch, Xrgb8888);
            }
            catch { Dispose(); throw; }
        }

        public void Dispose()
        {
            if (Framebuffer != 0) { drmModeRmFB(display.Fd, Framebuffer); Framebuffer = 0; }
            if (Pixels != 0) { munmap(Pixels, (nuint)allocation.Size); Pixels = 0; }
            if (allocation.Handle != 0) { var handle = allocation.Handle; drmIoctl(display.Fd, 0xc00464b4, &handle); allocation.Handle = 0; }
        }
    }
}
