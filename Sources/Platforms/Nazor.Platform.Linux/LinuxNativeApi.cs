using System.Runtime.InteropServices;
using Nazor.Graphics;

namespace Nazor.Platform.Linux;

internal sealed unsafe class LinuxNativeApi : IDisposable
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct Event
    {
        internal uint Type, Flags;
        internal nint Window;
        internal double X, Y;
        internal int A, B, C, D;
        internal nint Text;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct BufferView
    {
        internal nint Pixels;
        internal int Width, Height, Stride;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct PopupPlacement
    {
        internal int X, Y, Width, Height, AnchorX, AnchorY, AnchorWidth, AnchorHeight, Shadow, Side, Alignment, Offset;
        internal uint Flags, Serial;
    }

    private readonly nint library;
    internal readonly delegate* unmanaged[Cdecl]<nint, byte*, int> ForeignParent;
    internal readonly delegate* unmanaged[Cdecl]<nint, PopupPlacement*, nint> PopupCreate;
    internal readonly delegate* unmanaged[Cdecl]<nint, PopupPlacement*, int> PopupPlace;
    internal readonly delegate* unmanaged[Cdecl]<nint, int, int, int, int, void> PopupInputRegion;
    internal readonly delegate* unmanaged[Cdecl]<nint, int, int, int, int, PixelRect*, int> PopupWorkArea;
    internal bool HasPopups => PopupCreate != null && PopupPlace != null && PopupWorkArea != null && PopupInputRegion != null;
    internal readonly delegate* unmanaged[Cdecl]<uint> nz_abi_version;
    internal readonly delegate* unmanaged[Cdecl]<nint, int> DecorationSupport;
    internal readonly delegate* unmanaged[Cdecl]<nint, int> DecorationMode;
    internal readonly delegate* unmanaged[Cdecl]<nuint> nz_event_size;
    internal readonly delegate* unmanaged[Cdecl]<byte*, nuint, nint> nz_connect;
    internal readonly delegate* unmanaged[Cdecl]<nint, void> nz_disconnect;
    internal readonly delegate* unmanaged[Cdecl]<nint, nint> nz_error;
    internal readonly delegate* unmanaged[Cdecl]<nint, int, int> nz_dispatch;
    internal readonly delegate* unmanaged[Cdecl]<nint, void> nz_wake;
    internal readonly delegate* unmanaged[Cdecl]<nint, Event*, int> _nz_next_event;
    internal readonly delegate* unmanaged[Cdecl]<nint, byte*, byte*, int, int, nint, int, int, nint> _nz_window_create;
    internal readonly delegate* unmanaged[Cdecl]<nint, void> nz_window_destroy;
    internal readonly delegate* unmanaged[Cdecl]<nint, int, void> nz_window_show;
    internal readonly delegate* unmanaged[Cdecl]<nint, byte*, void> _nz_window_title;
    internal readonly delegate* unmanaged[Cdecl]<nint, int, void> nz_window_state;
    internal readonly delegate* unmanaged[Cdecl]<nint, int, void> nz_window_titlebar;
    internal readonly delegate* unmanaged[Cdecl]<nint, int, int, int, void> nz_window_operations;
    internal readonly delegate* unmanaged[Cdecl]<nint, int, int, void> X11Functions;
    internal readonly delegate* unmanaged[Cdecl]<nint, int> X11AboveSupported;
    internal readonly delegate* unmanaged[Cdecl]<nint, int, void> X11Above;
    internal readonly delegate* unmanaged[Cdecl]<nint, int, int, void> nz_window_resize;
    internal readonly delegate* unmanaged[Cdecl]<nint, void> nz_window_activate;
    internal readonly delegate* unmanaged[Cdecl]<nint, void> nz_window_move;
    internal readonly delegate* unmanaged[Cdecl]<nint, uint, int, int, int> WindowMenu;
    internal readonly delegate* unmanaged[Cdecl]<nint, uint, void> nz_window_interactive_resize;
    internal readonly delegate* unmanaged[Cdecl]<nint, uint, uint, void> WaylandResize;
    internal readonly delegate* unmanaged[Cdecl]<nint, int, void> OuterResize;
    internal readonly delegate* unmanaged[Cdecl]<nint, float*, int> X11Corners;
    internal readonly delegate* unmanaged[Cdecl]<nint, byte*, void> _nz_window_cursor;
    internal readonly delegate* unmanaged[Cdecl]<nint, int, int, int, int, int, void> nz_window_text_input;
    internal readonly delegate* unmanaged[Cdecl]<nint, byte*, int, int, int, void> _nz_window_surrounding;
    private readonly delegate* unmanaged[Cdecl]<nint, int, int, int, int, int, int, int, byte*, int, int, void> waylandTextContext;
    internal bool HasWaylandTextContext => waylandTextContext != null;
    internal readonly delegate* unmanaged[Cdecl]<nint, uint> nz_window_capabilities;
    internal readonly delegate* unmanaged[Cdecl]<nint, int, int, BufferView*, int> _nz_window_acquire;
    internal readonly delegate* unmanaged[Cdecl]<nint, float, int, void> nz_window_present;
    internal readonly delegate* unmanaged[Cdecl]<nint, int> nz_window_buffer_is_current;
    internal readonly delegate* unmanaged[Cdecl]<nint, void> nz_window_discard_buffer;
    internal readonly delegate* unmanaged[Cdecl]<nint, float, int, PixelRect*, int, void> nz_window_present_damage;
    internal readonly delegate* unmanaged[Cdecl]<nint, nint> nz_window_native_display;
    internal readonly delegate* unmanaged[Cdecl]<nint, nint> nz_window_native_surface;
    internal readonly delegate* unmanaged[Cdecl]<nint, nint> RenderSurface;
    internal readonly delegate* unmanaged[Cdecl]<nint, void> nz_window_gpu_prepare;
    internal readonly delegate* unmanaged[Cdecl]<nint, void> WaylandGpuStart;
    internal readonly delegate* unmanaged[Cdecl]<nint, int, int, int> WaylandGpuPrepare;
    internal readonly delegate* unmanaged[Cdecl]<nint, void> WaylandGpuPresented;
    internal bool HasWaylandGpuPresentation => WaylandGpuStart != null && WaylandGpuPrepare != null && WaylandGpuPresented != null;
    internal readonly delegate* unmanaged[Cdecl]<nint, void> nz_window_cancel_frame;
    internal readonly delegate* unmanaged[Cdecl]<nint, byte*, int, int, int> nz_window_icon;
    private readonly delegate* unmanaged[Cdecl]<nint, nint, int, int, int, float, int> frameShadow;
    private readonly delegate* unmanaged[Cdecl]<nint, nint, int, int, int, int, int> waylandShadow;
    internal readonly delegate* unmanaged[Cdecl]<nint, int, int> nz_window_blur;
    internal readonly delegate* unmanaged[Cdecl]<nint, byte*, int> _nz_clipboard_set;
    internal readonly delegate* unmanaged[Cdecl]<nint, int> nz_clipboard_request;
    internal readonly delegate* unmanaged[Cdecl]<nint, void> ClipboardCancel;
    internal readonly delegate* unmanaged[Cdecl]<nint, int, void> PointerCapture;
    internal readonly delegate* unmanaged[Cdecl]<nint, int> NativeVisual;
    private readonly delegate* unmanaged[Cdecl]<nint, int> argbSupported;
    private readonly delegate* unmanaged[Cdecl]<nint, byte*, byte*, int, int, nint, int, int, nint> createArgbWindow;
    internal LinuxNativeApi(string name)
    {
        var runtimes = Path.Combine(AppContext.BaseDirectory, "runtimes");
        var rid = NativeAssetRid(RuntimeInformation.RuntimeIdentifier, RuntimeInformation.ProcessArchitecture);
        var path = Path.Combine(runtimes, rid, "native", name);
        if (File.Exists(path)) library = NativeLibrary.Load(path);
        else
        {
            if (!File.Exists(Path.Combine(AppContext.BaseDirectory, name)) && Directory.Exists(runtimes) && Directory.EnumerateFiles(runtimes, name, SearchOption.AllDirectories).Any())
                throw new DllNotFoundException($"The Nazor native library for '{rid}' is missing: {path}");
            library = NativeLibrary.Load(name, typeof(LinuxNativeApi).Assembly, null);
        }
        try
        {
            nz_abi_version = (delegate* unmanaged[Cdecl]<uint>)NativeLibrary.GetExport(library, "nz_abi_version");
            nz_event_size = (delegate* unmanaged[Cdecl]<nuint>)NativeLibrary.GetExport(library, "nz_event_size");
            nz_connect = (delegate* unmanaged[Cdecl]<byte*, nuint, nint>)NativeLibrary.GetExport(library, "nz_connect");
            nz_disconnect = (delegate* unmanaged[Cdecl]<nint, void>)NativeLibrary.GetExport(library, "nz_disconnect");
            nz_error = (delegate* unmanaged[Cdecl]<nint, nint>)NativeLibrary.GetExport(library, "nz_error");
            nz_dispatch = (delegate* unmanaged[Cdecl]<nint, int, int>)NativeLibrary.GetExport(library, "nz_dispatch");
            nz_wake = (delegate* unmanaged[Cdecl]<nint, void>)NativeLibrary.GetExport(library, "nz_wake");
            _nz_next_event = (delegate* unmanaged[Cdecl]<nint, Event*, int>)NativeLibrary.GetExport(library, "nz_next_event");
            _nz_window_create = (delegate* unmanaged[Cdecl]<nint, byte*, byte*, int, int, nint, int, int, nint>)NativeLibrary.GetExport(library, "nz_window_create");
            NativeLibrary.TryGetExport(library, "nz_window_foreign_parent", out var foreignParent);
            ForeignParent = (delegate* unmanaged[Cdecl]<nint, byte*, int>)foreignParent;
            nz_window_destroy = (delegate* unmanaged[Cdecl]<nint, void>)NativeLibrary.GetExport(library, "nz_window_destroy");
            nz_window_show = (delegate* unmanaged[Cdecl]<nint, int, void>)NativeLibrary.GetExport(library, "nz_window_show");
            _nz_window_title = (delegate* unmanaged[Cdecl]<nint, byte*, void>)NativeLibrary.GetExport(library, "nz_window_title");
            nz_window_state = (delegate* unmanaged[Cdecl]<nint, int, void>)NativeLibrary.GetExport(library, "nz_window_state");
            DecorationSupport = (delegate* unmanaged[Cdecl]<nint, int>)NativeLibrary.GetExport(library, "nz_decoration_support");
            DecorationMode = (delegate* unmanaged[Cdecl]<nint, int>)NativeLibrary.GetExport(library, "nz_window_decoration_mode");
            nz_window_titlebar = (delegate* unmanaged[Cdecl]<nint, int, void>)NativeLibrary.GetExport(library, "nz_window_titlebar");
            nz_window_operations = (delegate* unmanaged[Cdecl]<nint, int, int, int, void>)NativeLibrary.GetExport(library, "nz_window_operations");
            NativeLibrary.TryGetExport(library, "nz_window_x11_functions", out var functions);
            X11Functions = (delegate* unmanaged[Cdecl]<nint, int, int, void>)functions;
            NativeLibrary.TryGetExport(library, "nz_x11_above_supported", out var aboveSupported);
            X11AboveSupported = (delegate* unmanaged[Cdecl]<nint, int>)aboveSupported;
            NativeLibrary.TryGetExport(library, "nz_window_x11_above", out var above);
            X11Above = (delegate* unmanaged[Cdecl]<nint, int, void>)above;
            nz_window_resize = (delegate* unmanaged[Cdecl]<nint, int, int, void>)NativeLibrary.GetExport(library, "nz_window_resize");
            nz_window_activate = (delegate* unmanaged[Cdecl]<nint, void>)NativeLibrary.GetExport(library, "nz_window_activate");
            nz_window_move = (delegate* unmanaged[Cdecl]<nint, void>)NativeLibrary.GetExport(library, "nz_window_move");
            NativeLibrary.TryGetExport(library, "nz_window_menu", out var menu);
            WindowMenu = (delegate* unmanaged[Cdecl]<nint, uint, int, int, int>)menu;
            nz_window_interactive_resize = (delegate* unmanaged[Cdecl]<nint, uint, void>)NativeLibrary.GetExport(library, "nz_window_interactive_resize");
            NativeLibrary.TryGetExport(library, "nz_window_wayland_resize", out var waylandResize);
            WaylandResize = (delegate* unmanaged[Cdecl]<nint, uint, uint, void>)waylandResize;
            NativeLibrary.TryGetExport(library, "nz_window_outer_resize", out var outerResize);
            OuterResize = (delegate* unmanaged[Cdecl]<nint, int, void>)outerResize;
            NativeLibrary.TryGetExport(library, "nz_window_x11_corners", out var corners);
            X11Corners = (delegate* unmanaged[Cdecl]<nint, float*, int>)corners;
            _nz_window_cursor = (delegate* unmanaged[Cdecl]<nint, byte*, void>)NativeLibrary.GetExport(library, "nz_window_cursor");
            nz_window_text_input = (delegate* unmanaged[Cdecl]<nint, int, int, int, int, int, void>)NativeLibrary.GetExport(library, "nz_window_text_input");
            _nz_window_surrounding = (delegate* unmanaged[Cdecl]<nint, byte*, int, int, int, void>)NativeLibrary.GetExport(library, "nz_window_surrounding");
            NativeLibrary.TryGetExport(library, "nz_window_wayland_text_context", out var textContext);
            waylandTextContext = (delegate* unmanaged[Cdecl]<nint, int, int, int, int, int, int, int, byte*, int, int, void>)textContext;
            nz_window_capabilities = (delegate* unmanaged[Cdecl]<nint, uint>)NativeLibrary.GetExport(library, "nz_window_capabilities");
            _nz_window_acquire = (delegate* unmanaged[Cdecl]<nint, int, int, BufferView*, int>)NativeLibrary.GetExport(library, "nz_window_acquire");
            nz_window_present = (delegate* unmanaged[Cdecl]<nint, float, int, void>)NativeLibrary.GetExport(library, "nz_window_present");
            nz_window_buffer_is_current = (delegate* unmanaged[Cdecl]<nint, int>)NativeLibrary.GetExport(library, "nz_window_buffer_is_current");
            nz_window_discard_buffer = (delegate* unmanaged[Cdecl]<nint, void>)NativeLibrary.GetExport(library, "nz_window_discard_buffer");
            nz_window_present_damage = (delegate* unmanaged[Cdecl]<nint, float, int, PixelRect*, int, void>)NativeLibrary.GetExport(library, "nz_window_present_damage");
            nz_window_native_display = (delegate* unmanaged[Cdecl]<nint, nint>)NativeLibrary.GetExport(library, "nz_window_native_display");
            nz_window_native_surface = (delegate* unmanaged[Cdecl]<nint, nint>)NativeLibrary.GetExport(library, "nz_window_native_surface");
            NativeLibrary.TryGetExport(library, "nz_window_render_surface", out var renderSurface);
            RenderSurface = (delegate* unmanaged[Cdecl]<nint, nint>)renderSurface;
            nz_window_gpu_prepare = (delegate* unmanaged[Cdecl]<nint, void>)NativeLibrary.GetExport(library, "nz_window_gpu_prepare");
            NativeLibrary.TryGetExport(library, "nz_window_wayland_gpu_start", out var waylandGpuStart);
            WaylandGpuStart = (delegate* unmanaged[Cdecl]<nint, void>)waylandGpuStart;
            NativeLibrary.TryGetExport(library, "nz_window_wayland_gpu_prepare", out var waylandGpuPrepare);
            WaylandGpuPrepare = (delegate* unmanaged[Cdecl]<nint, int, int, int>)waylandGpuPrepare;
            NativeLibrary.TryGetExport(library, "nz_window_wayland_gpu_presented", out var waylandGpuPresented);
            WaylandGpuPresented = (delegate* unmanaged[Cdecl]<nint, void>)waylandGpuPresented;
            nz_window_cancel_frame = (delegate* unmanaged[Cdecl]<nint, void>)NativeLibrary.GetExport(library, "nz_window_cancel_frame");
            nz_window_icon = (delegate* unmanaged[Cdecl]<nint, byte*, int, int, int>)NativeLibrary.GetExport(library, "nz_window_icon");
            NativeLibrary.TryGetExport(library, "nz_window_frame_shadow", out var shadow);
            frameShadow = (delegate* unmanaged[Cdecl]<nint, nint, int, int, int, float, int>)shadow;
            NativeLibrary.TryGetExport(library, "nz_window_wayland_shadow", out var legacyShadow);
            waylandShadow = (delegate* unmanaged[Cdecl]<nint, nint, int, int, int, int, int>)legacyShadow;
            nz_window_blur = (delegate* unmanaged[Cdecl]<nint, int, int>)NativeLibrary.GetExport(library, "nz_window_blur");
            _nz_clipboard_set = (delegate* unmanaged[Cdecl]<nint, byte*, int>)NativeLibrary.GetExport(library, "nz_clipboard_set");
            nz_clipboard_request = (delegate* unmanaged[Cdecl]<nint, int>)NativeLibrary.GetExport(library, "nz_clipboard_request");
            NativeLibrary.TryGetExport(library, "nz_clipboard_cancel", out var clipboardCancel);
            ClipboardCancel = (delegate* unmanaged[Cdecl]<nint, void>)clipboardCancel;
            NativeLibrary.TryGetExport(library, "nz_window_pointer_capture", out var capture);
            PointerCapture = (delegate* unmanaged[Cdecl]<nint, int, void>)capture;
            NativeLibrary.TryGetExport(library, "nz_window_native_visual", out var visual);
            NativeVisual = (delegate* unmanaged[Cdecl]<nint, int>)visual;
            NativeLibrary.TryGetExport(library, "nz_popup_create", out var popupCreate);
            PopupCreate = (delegate* unmanaged[Cdecl]<nint, PopupPlacement*, nint>)popupCreate;
            NativeLibrary.TryGetExport(library, "nz_popup_place", out var popupPlace);
            PopupPlace = (delegate* unmanaged[Cdecl]<nint, PopupPlacement*, int>)popupPlace;
            NativeLibrary.TryGetExport(library, "nz_popup_input_region", out var popupInput);
            PopupInputRegion = (delegate* unmanaged[Cdecl]<nint, int, int, int, int, void>)popupInput;
            NativeLibrary.TryGetExport(library, "nz_popup_work_area", out var popupWorkArea);
            PopupWorkArea = (delegate* unmanaged[Cdecl]<nint, int, int, int, int, PixelRect*, int>)popupWorkArea;
            NativeLibrary.TryGetExport(library, "nz_argb_supported", out var argb);
            argbSupported = (delegate* unmanaged[Cdecl]<nint, int>)argb;
            NativeLibrary.TryGetExport(library, "nz_window_create_argb", out var createArgb);
            createArgbWindow = (delegate* unmanaged[Cdecl]<nint, byte*, byte*, int, int, nint, int, int, nint>)createArgb;
        }
        catch { NativeLibrary.Free(library); throw; }
    }
    internal int nz_next_event(nint connection, out Event value)
    {
        value = default;
        fixed (Event* valuePointer = &value)
        { return _nz_next_event(connection, valuePointer); }
    }
    internal bool SupportsArgb(nint connection) => argbSupported != null && createArgbWindow != null && argbSupported(connection) != 0;

    internal static string NativeAssetRid(string runtimeIdentifier, Architecture architecture)
    {
        var family = runtimeIdentifier.Contains("musl", StringComparison.Ordinal) || runtimeIdentifier.StartsWith("alpine", StringComparison.Ordinal)
            ? "linux-musl" : "linux";
        return family + "-" + architecture.ToString().ToLowerInvariant();
    }
    internal bool HasFrameShadow => frameShadow != null || waylandShadow != null;

    internal int PresentFrameShadow(nint window, nint pixels, int width, int height, int margin, float scale) => frameShadow != null
        ? frameShadow(window, pixels, width, height, margin, scale)
        : waylandShadow(window, pixels, width, height, margin, (int)scale);

    internal nint nz_window_create(nint connection, string title, string appId, int width, int height, nint parent, int customTitlebar, int borderless, bool argb)
    {
        var create = argb ? createArgbWindow : _nz_window_create;
        var titleBytes = System.Text.Encoding.UTF8.GetBytes(title + "\0");
        var appIdBytes = System.Text.Encoding.UTF8.GetBytes(appId + "\0");
        fixed (byte* titlePointer = titleBytes)
        fixed (byte* appIdPointer = appIdBytes)
        { return create(connection, titlePointer, appIdPointer, width, height, parent, customTitlebar, borderless); }
    }
    internal void nz_window_title(nint window, string title)
    {
        var titleBytes = System.Text.Encoding.UTF8.GetBytes(title + "\0");
        fixed (byte* titlePointer = titleBytes)
        { _nz_window_title(window, titlePointer); }
    }
    internal void nz_window_cursor(nint window, string name)
    {
        var nameBytes = System.Text.Encoding.UTF8.GetBytes(name + "\0");
        fixed (byte* namePointer = nameBytes)
        { _nz_window_cursor(window, namePointer); }
    }
    internal void nz_window_surrounding(nint window, string text, int cursor, int anchor, int password)
    {
        var textBytes = System.Text.Encoding.UTF8.GetBytes(text + "\0");
        fixed (byte* textPointer = textBytes)
        { _nz_window_surrounding(window, textPointer, cursor, anchor, password); }
    }
    internal void UpdateWaylandTextContext(nint window, bool enabled, int field, bool password, int x, int y, int width, int height,
        string? text, int cursor, int anchor)
    {
        var textBytes = text is null ? null : System.Text.Encoding.UTF8.GetBytes(text + "\0");
        fixed (byte* textPointer = textBytes)
            waylandTextContext(window, enabled ? 1 : 0, field, password ? 1 : 0, x, y, width, height, textPointer, cursor, anchor);
    }
    internal int nz_window_acquire(nint window, int width, int height, out BufferView view)
    {
        view = default;
        fixed (BufferView* viewPointer = &view)
        { return _nz_window_acquire(window, width, height, viewPointer); }
    }
    internal int nz_clipboard_set(nint connection, string text)
    {
        var textBytes = System.Text.Encoding.UTF8.GetBytes(text + "\0");
        fixed (byte* textPointer = textBytes)
        { return _nz_clipboard_set(connection, textPointer); }
    }
    public void Dispose() => NativeLibrary.Free(library);
}
