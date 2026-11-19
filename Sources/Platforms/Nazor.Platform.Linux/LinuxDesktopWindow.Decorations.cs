namespace Nazor.Platform.Linux;

internal sealed unsafe partial class LinuxDesktopWindow
{
    private WindowDecorations? decorations;
    private WindowDecorations? acceptedDecorations;
    private TaskCompletionSource<WindowDecorations>? decorationCompletion;

    private WindowDecorationCapabilities CurrentDecorationSupport
    {
        get
        {
            var support = platform.DecorationSupport;
            if (State == WindowState.FullScreen)
                return support with
                {
                    Current = acceptedDecorations,
                    FrameChanges = WindowSupportStatus.Unsupported,
                    NativeFrame = acceptedDecorations?.HasNativeTitleBar == true ? WindowSupportStatus.Supported : support.NativeFrame,
                    NativeTitleBar = acceptedDecorations?.HasNativeTitleBar == true ? WindowSupportStatus.Supported : support.NativeTitleBar,
                    NazorFrame = acceptedDecorations?.Frame == WindowFrameOwner.Nazor ? WindowSupportStatus.Supported : support.NazorFrame,
                };
            var mode = IsClosed ? 0 : Native.DecorationMode(Handle);
            if (decorations?.HasNativeTitleBar == true && mode != 0 && (decorationCompletion is null || decorationCompletion.Task.IsCompleted))
                support = support with
                {
                    NativeFrame = mode == 2 ? WindowSupportStatus.Supported : WindowSupportStatus.Unsupported,
                    NativeTitleBar = mode == 2 ? WindowSupportStatus.Supported : WindowSupportStatus.Unsupported,
                };
            if (decorations?.Frame == WindowFrameOwner.Nazor && mode != 0 && (decorationCompletion is null || decorationCompletion.Task.IsCompleted))
                support = support with { NazorFrame = mode == 1 ? WindowSupportStatus.Supported : WindowSupportStatus.Unsupported };
            return support with { Current = acceptedDecorations };
        }
    }

    private WindowChrome DecorationChrome
    {
        get
        {
            var mode = Native.DecorationMode(Handle);
            if (mode == 0) return new(WindowChromeOwner.Unknown, WindowChromeOwner.Unknown, WindowChromeOwner.Unknown) { TitleBar = WindowChromeOwner.Unknown };
            if (mode == 2) return new(WindowChromeOwner.System, WindowChromeOwner.System, WindowChromeOwner.System) { TitleBar = WindowChromeOwner.System };
            var configured = decorations!;
            return new(WindowChromeOwner.Nazor,
                configured.CaptionButtons switch { WindowCaptionButtons.Nazor => WindowChromeOwner.Nazor, WindowCaptionButtons.User => WindowChromeOwner.User, _ => WindowChromeOwner.None },
                titleBarMenuMode == WindowTitleBarMenuMode.Nazor ? WindowChromeOwner.Nazor : titleBarMenuMode == WindowTitleBarMenuMode.System && Native.WindowMenu != null ? WindowChromeOwner.System : WindowChromeOwner.None)
            { TitleBar = configured.TitleBar switch { WindowTitleBarContent.Nazor => WindowChromeOwner.Nazor, WindowTitleBarContent.User => WindowChromeOwner.User, _ => WindowChromeOwner.None } };
        }
    }

    public ValueTask<WindowDecorations> SetDecorationsAsync(WindowDecorations configuration, CancellationToken cancellationToken = default)
    {
        Access();
        cancellationToken.ThrowIfCancellationRequested();
        if (decorationCompletion is not null) throw new InvalidOperationException("A native decoration negotiation is already pending.");
        var support = CurrentDecorationSupport.GetChangeSupport(configuration);
        if (support.Status == WindowSupportStatus.Unsupported) throw new PlatformNotSupportedException(support.Reason);
        if (State == WindowState.FullScreen && acceptedDecorations?.Frame == configuration.Frame)
        {
            decorations = acceptedDecorations = configuration;
            customTitlebar = !configuration.HasNativeTitleBar;
            host.OnCapabilitiesChanged();
            Invalidate();
            return new(configuration);
        }
        var previousInset = HeaderInset;
        decorations = configuration;
        customTitlebar = !configuration.HasNativeTitleBar;
        softwarePresented = false;
        Native.nz_window_titlebar(Handle, customTitlebar ? 1 : 0);
        ResizeForHeaderChange(previousInset);
        var completion = decorationCompletion = new(TaskCreationOptions.RunContinuationsAsynchronously);
        CompleteDecorationNegotiation(false);
        Invalidate();
        return LinuxDecorationNegotiation.WaitAsync(completion, cancellationToken,
            () => { if (ReferenceEquals(decorationCompletion, completion)) decorationCompletion = null; });
    }

    private void CompleteDecorationNegotiation(bool rejectMismatch = true)
    {
        if (decorationCompletion is not { } completion || completion.Task.IsCompleted || decorations is not { } requested || IsClosed) return;
        var mode = Native.DecorationMode(Handle);
        if (mode == 0) return;
        serverDecorations = mode == 2;
        if (serverDecorations == (requested.Frame == WindowFrameOwner.Native))
        {
            acceptedDecorations = requested;
            completion.TrySetResult(requested);
        }
        else if (rejectMismatch && !platform.IsX11) completion.TrySetException(new PlatformNotSupportedException("The desktop selected a different decoration owner than requested."));
    }
}

internal static class LinuxDecorationNegotiation
{
    internal static async ValueTask<WindowDecorations> WaitAsync(TaskCompletionSource<WindowDecorations> completion, CancellationToken cancellationToken, Action completed)
    {
        try { return await completion.Task.WaitAsync(TimeSpan.FromSeconds(3), cancellationToken); }
        catch (TimeoutException) { throw new PlatformNotSupportedException("The desktop did not confirm the requested decoration ownership."); }
        finally { completed(); }
    }
}
