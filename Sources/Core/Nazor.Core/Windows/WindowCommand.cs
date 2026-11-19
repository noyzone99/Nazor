namespace Nazor;

/// <summary>How the backend applies an application restriction to standard native window commands.</summary>
public enum NativeRestriction
{
    /// <summary>The app controls its own commands and controls.</summary>
    None,
    /// <summary>The backend sends a hint that the window manager may choose to honor.</summary>
    Hint,
    /// <summary>The backend enforces the restriction on standard native commands for the window.</summary>
    Enforced,
}

/// <summary>Backend support for an operation, independently of application policy.</summary>
public readonly record struct WindowOperationSupport(bool IsSupported, NativeRestriction Restriction = NativeRestriction.None);

/// <summary>A command requested for a window.</summary>
public enum WindowCommand
{
    /// <summary>Requests minimization.</summary>
    Minimize,
    /// <summary>Requests maximization.</summary>
    Maximize,
    /// <summary>Restores from a non-normal state.</summary>
    Restore,
    /// <summary>Requests entry into fullscreen.</summary>
    EnterFullScreen,
    /// <summary>Requests a return from fullscreen.</summary>
    ExitFullScreen,
}

/// <summary>The outcome of checking and dispatching a window command.</summary>
public enum WindowCommandResult
{
    /// <summary>There is no open native window.</summary>
    Unavailable,
    /// <summary>The backend does not support the command in the current state.</summary>
    Unsupported,
    /// <summary>The app policy disallows the command.</summary>
    DisabledByPolicy,
    /// <summary>The last reported state already matches the command.</summary>
    AlreadyInState,
    /// <summary>The request was sent to the backend. Observe StateChanged for the actual state.</summary>
    Dispatched,
}

/// <summary>The menu used for title bar clicks handled by Nazor.</summary>
public enum WindowTitleBarMenuMode
{
    /// <summary>Uses the system menu when the backend can open it.</summary>
    System,
    /// <summary>Opens a Nazor menu for clicks handled by the backend.</summary>
    Nazor,
    /// <summary>Suppresses the menu for clicks handled by the backend.</summary>
    None,
}

/// <summary>The owner of part of a window's decoration.</summary>
public enum WindowChromeOwner
{
    /// <summary>The backend does not provide this part.</summary>
    None,
    /// <summary>The desktop or native window API provides this part.</summary>
    System,
    /// <summary>The framework provides this part.</summary>
    Nazor,
    /// <summary>The application provides the selected caption content.</summary>
    User,
    /// <summary>The backend has not confirmed the owner.</summary>
    Unknown,
}

/// <summary>The actual owner of the caption, caption buttons and title bar context menu.</summary>
public readonly record struct WindowChrome(
    WindowChromeOwner Frame, WindowChromeOwner CaptionButtons, WindowChromeOwner TitleBarMenu)
{
    /// <summary>The actual title-bar owner, independently of the external frame and buttons.</summary>
    public WindowChromeOwner TitleBar { get; init; }
}
