using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using static Nazor.Platform.Linux.DrmNative;

namespace Nazor.Platform.Linux;

internal enum DrmPresentation { Deferred, Queued, Presented }

internal sealed unsafe class DrmDisplay : IDisposable
{
    private readonly DrmSession session;
    private readonly int deviceId;
    private readonly uint connector, crtc;
    private readonly Mode mode;
    private readonly Crtc previous;
    private readonly uint[] previousConnectors;
    private readonly FlipHandler flipHandler;
    private bool modeset, hasPresented, disposed;
    private nint nextFlip, pendingFlip;
    private readonly List<DumbBuffer> software = [];
    private DumbBuffer? queuedSoftware;
    private int nextSoftware;
    internal int Fd { get; }
    internal int Width => mode.HDisplay;
    internal int Height => mode.VDisplay;

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void FlipHandler(int fd, uint sequence, uint seconds, uint microseconds, nint user);

    internal DrmDisplay(DrmSession session, DrmOptions options)
    {
        this.session = session;
        flipHandler = (_, _, _, _, flip) => { if (flip == pendingFlip) pendingFlip = 0; };
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

    internal DrmPresentation Present(uint framebuffer, bool watchSeat = false)
    {
        if (!session.Active) return DrmPresentation.Deferred;
        if (!modeset)
        {
            var output = connector;
            var selectedMode = mode;
            if (drmModeSetCrtc(Fd, crtc, framebuffer, 0, 0, &output, 1, &selectedMode) != 0)
            {
                if (hasPresented && Marshal.GetLastPInvokeError() == AccessDenied) return DrmPresentation.Deferred;
                throw Error("Cannot configure the DRM output");
            }
            modeset = hasPresented = true;
            pendingFlip = 0;
            return DrmPresentation.Presented;
        }
        if (pendingFlip != 0) throw new GraphicsDeviceException("The previous DRM page flip has not completed.");
        nextFlip = nextFlip == nint.MaxValue ? 1 : nextFlip + 1;
        if (drmModePageFlip(Fd, crtc, framebuffer, 1, nextFlip) != 0)
        {
            if (Marshal.GetLastPInvokeError() == AccessDenied) return DrmPresentation.Deferred;
            throw Error("Cannot queue the DRM page flip");
        }
        pendingFlip = nextFlip;
        return WaitForFlip(watchSeat);
    }

    internal DrmPresentation ResumePresent(uint framebuffer, bool watchSeat = false) =>
        pendingFlip != 0 ? WaitForFlip(watchSeat) : Present(framebuffer, watchSeat);

    private DrmPresentation WaitForFlip(bool watchSeat)
    {
        var deadline = Stopwatch.GetTimestamp();
        var context = new EventContext { Version = 2, PageFlipHandler = Marshal.GetFunctionPointerForDelegate(flipHandler) };
        PollFd* descriptors = stackalloc PollFd[2]
        {
            new() { Fd = Fd, Events = 1 },
            new() { Fd = watchSeat ? session.Fd : -1, Events = 1 }
        };
        while (pendingFlip != 0)
        {
            if (!session.Active) return DrmPresentation.Queued;
            if (Stopwatch.GetElapsedTime(deadline) > TimeSpan.FromSeconds(2))
                throw new GraphicsDeviceException("The DRM page flip timed out. Check the display connection and active seat.");
            var result = poll(descriptors, 2, 50);
            if (result < 0 && Marshal.GetLastPInvokeError() == 4) continue;
            if (result < 0 || (descriptors[0].ReturnedEvents & (8 | 16 | 32)) != 0) throw Error("The DRM device is unavailable");
            if ((descriptors[0].ReturnedEvents & 1) != 0 && drmHandleEvent(Fd, ref context) != 0) throw Error("Cannot read the DRM page flip event");
            if (pendingFlip != 0 && descriptors[1].ReturnedEvents != 0) return DrmPresentation.Queued;
        }
        return DrmPresentation.Presented;
    }

    private uint RegisterBuffer(uint handle, uint pitch, uint format, uint offset = 0)
    {
        uint* handles = stackalloc uint[4] { handle, 0, 0, 0 };
        uint* pitches = stackalloc uint[4] { pitch, 0, 0, 0 };
        uint* offsets = stackalloc uint[4] { offset, 0, 0, 0 };
        if (drmModeAddFB2(Fd, (uint)Width, (uint)Height, format, handles, pitches, offsets, out var framebuffer, 0) != 0)
            throw Error("Cannot register a DRM framebuffer");
        return framebuffer;
    }

    internal uint RegisterGbmBuffer(nint buffer)
    {
        var format = gbm_bo_get_format(buffer);
        ulong modifier;
        int planes;
        try
        {
            modifier = gbm_bo_get_modifier(buffer);
            planes = gbm_bo_get_plane_count(buffer);
        }
        catch (EntryPointNotFoundException)
        {
            return RegisterBuffer((uint)gbm_bo_get_handle(buffer), gbm_bo_get_stride(buffer), format);
        }
        if (planes is < 1 or > 4 || (planes != 1 && modifier is 0 or NoFormatModifier))
            throw new GraphicsDeviceException("The GBM scanout buffer has an unsupported plane layout.");
        if (modifier == NoFormatModifier)
            return RegisterBuffer((uint)gbm_bo_get_handle(buffer), gbm_bo_get_stride(buffer), format, gbm_bo_get_offset(buffer, 0));

        uint* handles = stackalloc uint[4] { 0, 0, 0, 0 };
        uint* pitches = stackalloc uint[4] { 0, 0, 0, 0 };
        uint* offsets = stackalloc uint[4] { 0, 0, 0, 0 };
        ulong* modifiers = stackalloc ulong[4] { 0, 0, 0, 0 };
        for (var plane = 0; plane < planes; plane++)
        {
            handles[plane] = (uint)(planes == 1 ? gbm_bo_get_handle(buffer) : gbm_bo_get_handle_for_plane(buffer, plane));
            pitches[plane] = planes == 1 ? gbm_bo_get_stride(buffer) : gbm_bo_get_stride_for_plane(buffer, plane);
            offsets[plane] = gbm_bo_get_offset(buffer, plane);
            modifiers[plane] = modifier;
            if (handles[plane] is 0 or uint.MaxValue || pitches[plane] == 0)
                throw new GraphicsDeviceException("Cannot read the GBM scanout buffer plane.");
        }

        try
        {
            if (drmModeAddFB2WithModifiers(Fd, (uint)Width, (uint)Height, format, handles, pitches, offsets, modifiers, out var framebuffer, FramebufferModifiers) == 0)
                return framebuffer;
            if (modifier == 0 && Marshal.GetLastPInvokeError() is InvalidArgument or OperationNotSupported)
                return RegisterBuffer(handles[0], pitches[0], format, offsets[0]);
        }
        catch (EntryPointNotFoundException error)
        {
            if (modifier == 0) return RegisterBuffer(handles[0], pitches[0], format, offsets[0]);
            throw new GraphicsDeviceException("The installed libdrm cannot register GBM scanout modifiers.", error);
        }
        throw Error("Cannot register the GBM scanout framebuffer");
    }

    internal bool PresentSoftware(PaintTarget target)
    {
        if (!session.Active) return false;
        if (software.Count == 0)
        {
            try { software.Add(new(this)); software.Add(new(this)); }
            catch { foreach (var allocated in software) allocated.Dispose(); software.Clear(); throw; }
        }
        if (queuedSoftware is { } queued)
        {
            if (ResumePresent(queued.Framebuffer, watchSeat: true) != DrmPresentation.Presented) return false;
            nextSoftware = 1 - nextSoftware;
            queuedSoftware = null;
        }
        var buffer = software[nextSoftware];
        for (var row = 0; row < Height; row++)
            NativeMemory.Copy((byte*)target.Pixels + (long)row * target.Stride, (byte*)buffer.Pixels + (long)row * buffer.Pitch, (nuint)Width * 4);
        var result = Present(buffer.Framebuffer, watchSeat: true);
        if (result == DrmPresentation.Queued) queuedSoftware = buffer;
        if (result != DrmPresentation.Presented) return false;
        nextSoftware = 1 - nextSoftware;
        return true;
    }

    internal void Suspend() => Restore(allowLostMaster: true);

    internal void Restore() => Restore(allowLostMaster: false);

    private void Restore(bool allowLostMaster)
    {
        if (!modeset) return;
        fixed (uint* outputs = previousConnectors)
        {
            var savedMode = previous.Mode;
            if (drmModeSetCrtc(Fd, crtc, previous.BufferId, previous.X, previous.Y, outputs,
                previous.ModeValid != 0 ? previousConnectors.Length : 0, previous.ModeValid != 0 ? &savedMode : null) != 0 &&
                !(allowLostMaster && Marshal.GetLastPInvokeError() == AccessDenied))
                throw Error("Cannot restore the previous DRM display state");
        }
        modeset = false;
        pendingFlip = 0;
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
