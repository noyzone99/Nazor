using Nazor.Graphics;

namespace Nazor;

public sealed partial class NazorWindow
{
    private WindowTitleBarMenuMode titleBarMenuMode;
    private NazorTitleBarMenu? titleBarMenu;

    /// <summary>The application's rules for window commands and interactive resize.</summary>
    public WindowOperations Operations { get => operations; set => SetOperations(value); }

    /// <summary>The menu used for title bar clicks handled by Nazor.</summary>
    public WindowTitleBarMenuMode TitleBarMenuMode
    {
        get => titleBarMenuMode;
        set
        {
            AccessConfiguration();
            if (!Enum.IsDefined(value)) throw new ArgumentOutOfRangeException(nameof(value));
            if (value == titleBarMenuMode) return;
            platformWindow?.SetTitleBarMenuMode(value);
            titleBarMenuMode = value;
            RefreshChrome();
            NotifyPresentation();
        }
    }

    /// <summary>The effective owner of the frame, its buttons and its context menu.</summary>
    public WindowChrome Chrome { get; private set; }

    /// <summary>Raised when an element of the window chrome changes owner.</summary>
    public event Action? ChromeChanged;

    private void RefreshChrome()
    {
        var chrome = platformWindow is { IsClosed: false } window ? window.Chrome : default;
        if (chrome == Chrome) return;
        Chrome = chrome;
        if (chrome.TitleBarMenu != WindowChromeOwner.Nazor) titleBarMenu?.Dismiss();
        ChromeChanged?.Invoke();
    }

    void Platform.IWindowHost.OnTitleBarMenuRequested(Point point)
    {
        if (IsClosed || Chrome.TitleBarMenu != WindowChromeOwner.Nazor) return;
        titleBarMenu ??= (NazorTitleBarMenu)Renderer.AddRootComponent<NazorTitleBarMenu>().Component;
        titleBarMenu.Open(point);
    }

    /// <summary>Whether the command can be sent with the current policy, state and backend support.</summary>
    public bool CanExecute(WindowCommand command) => CheckCommand(command) == WindowCommandResult.Dispatched;

    /// <summary>Checks and dispatches a command. StateChanged reports the actual state.</summary>
    public WindowCommandResult Request(WindowCommand command)
    {
        Dispatcher.AssertAccess();
        var result = CheckCommand(command);
        if (result != WindowCommandResult.Dispatched) return result;

        var window = platformWindow!;
        switch (command)
        {
            case WindowCommand.Minimize: window.SetState(WindowState.Minimized); break;
            case WindowCommand.Maximize: window.SetState(WindowState.Maximized); break;
            case WindowCommand.EnterFullScreen: window.SetState(WindowState.FullScreen); break;
            case WindowCommand.ExitFullScreen: window.ExitFullScreen(); break;
            case WindowCommand.Restore:
                if (window.State == WindowState.FullScreen) window.ExitFullScreen();
                else window.SetState(WindowState.Normal);
                break;
        }
        return result;
    }

    private WindowCommandResult CheckCommand(WindowCommand command)
    {
        if (!Enum.IsDefined(command)) throw new ArgumentOutOfRangeException(nameof(command));
        if (platformWindow is not { IsClosed: false } window || IsClosed) return WindowCommandResult.Unavailable;

        var state = window.State;
        switch (command)
        {
            case WindowCommand.Minimize:
                if (!operations.IsMinimizable) return WindowCommandResult.DisabledByPolicy;
                return CheckTarget(Capabilities.Minimize, state, WindowState.Minimized);
            case WindowCommand.Maximize:
                if (!operations.IsMaximizable) return WindowCommandResult.DisabledByPolicy;
                return CheckTarget(Capabilities.Maximize, state, WindowState.Maximized);
            case WindowCommand.EnterFullScreen:
                if (!operations.FullScreenEnabled) return WindowCommandResult.DisabledByPolicy;
                return CheckTarget(Capabilities.FullScreen, state, WindowState.FullScreen);
            case WindowCommand.ExitFullScreen:
                return state == WindowState.FullScreen ? WindowCommandResult.Dispatched : WindowCommandResult.AlreadyInState;
            default:
                return state switch
                {
                    WindowState.Normal => WindowCommandResult.AlreadyInState,
                    WindowState.Minimized when !Capabilities.CanRestoreFromMinimized => WindowCommandResult.Unsupported,
                    _ => WindowCommandResult.Dispatched,
                };
        }
    }

    private static WindowCommandResult CheckTarget(WindowOperationSupport support, WindowState state, WindowState target)
        => !support.IsSupported ? WindowCommandResult.Unsupported :
            state == target ? WindowCommandResult.AlreadyInState : WindowCommandResult.Dispatched;
}
