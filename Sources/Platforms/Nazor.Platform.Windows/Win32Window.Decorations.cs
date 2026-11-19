using static Nazor.Platform.Windows.Win32;

namespace Nazor.Platform.Windows;

internal sealed unsafe partial class Win32Window
{
    private WindowDecorations? decorations;
    private bool suppressFrameInput;
    private bool clientFrameApplied;

    internal static WindowDecorationCapabilities DecorationSupport => new()
    {
        NativeFrame = WindowSupportStatus.Supported,
        NativeTitleBar = WindowSupportStatus.Supported,
        NazorFrame = WindowSupportStatus.Supported,
        ClientTitleBarOnNativeFrame = WindowSupportStatus.Supported,
        IntegratedNativeCaptionButtons = WindowSupportStatus.Supported,
        FrameChanges = WindowSupportStatus.Supported,
        CaptionChanges = WindowSupportStatus.Supported,
    };

    public ValueTask<WindowDecorations> SetDecorationsAsync(WindowDecorations configuration, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ObjectDisposedException.ThrowIf(IsClosed, this);
        var support = DecorationSupport.GetSupport(configuration);
        if (support.Status != WindowSupportStatus.Supported) throw new PlatformNotSupportedException(support.Reason);
        if (decorations == configuration) return new(configuration);
        var size = fullScreen || IsZoomed(hwnd) || IsIconic(hwnd)
            ? GetPlacement()?.LogicalClientSize ?? ClientSize : ClientSize;
        decorations = configuration;
        suppressFrameInput = false;
        clientFrame = configuration.Frame == WindowFrameOwner.Nazor;
        extended = !configuration.HasNativeTitleBar;
        showSystemButtons = configuration.CaptionButtons == WindowCaptionButtons.Native;
        if (extended && !clientFrame && !modernWindowing) modernWindowing = initializeWindowing?.Invoke() ?? false;
        var current = fullScreen ? (uint)savedStyle : (uint)GetWindowLongPtrW(hwnd, -16);
        const uint frameMask = 0x80cf0000;
        var next = OperationsStyle((current & ~frameMask) | (clientFrame ? WS_POPUP | 0x80000u : WS_OVERLAPPEDWINDOW), operations);
        if (extended && !showSystemButtons && !modernWindowing) next &= ~WS_CAPTION;
        if (clientFrame) next &= ~WS_CAPTION;
        savedStyle = (nint)next;
        if (!fullScreen)
        {
            ConfigureTitleBar();
            SetWindowLongPtrW(hwnd, -16, (nint)next);
            SetWindowPos(hwnd, 0, 0, 0, 0, 0, SWP_FRAMECHANGED | SWP_NOMOVE | SWP_NOSIZE | SWP_NOZORDER | SWP_NOACTIVATE);
            ApplyFrame();
        }
        Resize(size);
        if (!fullScreen) host.OnResized(ClientSize, Scale);
        host.OnCapabilitiesChanged();
        Invalidate();
        return new(configuration);
    }
}
