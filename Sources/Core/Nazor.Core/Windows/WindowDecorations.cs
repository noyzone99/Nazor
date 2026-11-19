namespace Nazor;

/// <summary>The provider of the external window frame.</summary>
public enum WindowFrameOwner
{
    /// <summary>The desktop provides the external frame.</summary>
    Native,
    /// <summary>Nazor draws the external frame using root NSS properties.</summary>
    Nazor,
}

/// <summary>The content supplied inside a client-rendered title bar.</summary>
public enum WindowTitleBarContent
{
    /// <summary>No title bar is requested.</summary>
    None,
    /// <summary>The active theme supplies the standard title bar.</summary>
    Nazor,
    /// <summary>The window's UserTitleBar declaration supplies the content.</summary>
    User,
}

/// <summary>The explicitly requested caption-button provider.</summary>
public enum WindowCaptionButtons
{
    /// <summary>No caption-button group or occupied area is requested.</summary>
    None,
    /// <summary>The desktop supplies the controls; Nazor must not imitate them.</summary>
    Native,
    /// <summary>The active theme supplies the standard controls.</summary>
    Nazor,
    /// <summary>The window's UserCaptionButtons declaration supplies the controls.</summary>
    User,
}

/// <summary>An immutable, complete window-decoration request. Backend support is checked separately.</summary>
public sealed record WindowDecorations
{
    /// <summary>The requested owner of the external frame.</summary>
    public WindowFrameOwner Frame { get; }
    /// <summary>Whether the desktop supplies the original title bar as well as its controls.</summary>
    public bool HasNativeTitleBar { get; }
    /// <summary>The client title-bar content; None for an original native title bar.</summary>
    public WindowTitleBarContent TitleBar { get; }
    /// <summary>The requested caption-button provider.</summary>
    public WindowCaptionButtons CaptionButtons { get; }

    private WindowDecorations(WindowFrameOwner frame, bool native, WindowTitleBarContent titleBar, WindowCaptionButtons buttons)
    {
        if (!Enum.IsDefined(titleBar)) throw new ArgumentOutOfRangeException(nameof(titleBar));
        if (!Enum.IsDefined(buttons)) throw new ArgumentOutOfRangeException(nameof(buttons));
        if (!native && titleBar == WindowTitleBarContent.None && buttons != WindowCaptionButtons.None)
            throw new ArgumentException("Caption buttons require a title bar.", nameof(buttons));
        Frame = frame;
        HasNativeTitleBar = native;
        TitleBar = titleBar;
        CaptionButtons = buttons;
    }

    /// <summary>Requests the original desktop frame, title bar and caption controls as one unit.</summary>
    /// <returns>The complete native configuration.</returns>
    public static WindowDecorations Native() => new(WindowFrameOwner.Native, true, WindowTitleBarContent.None, WindowCaptionButtons.Native);

    /// <summary>Requests a native external frame with client title-bar content and an explicit button provider.</summary>
    /// <param name="titleBar">The client content, or None to omit the title bar.</param>
    /// <param name="buttons">The caption-button provider.</param>
    /// <returns>The complete integration request; its availability depends on the backend.</returns>
    public static WindowDecorations Native(WindowTitleBarContent titleBar, WindowCaptionButtons buttons) => new(WindowFrameOwner.Native, false, titleBar, buttons);

    /// <summary>Requests a Nazor-drawn frame with explicit title-bar content and caption controls.</summary>
    /// <param name="titleBar">The client content, or None to omit the title bar.</param>
    /// <param name="buttons">The caption-button provider; Native requires explicit backend support.</param>
    /// <returns>The complete client-frame configuration.</returns>
    public static WindowDecorations Nazor(WindowTitleBarContent titleBar = WindowTitleBarContent.Nazor,
        WindowCaptionButtons buttons = WindowCaptionButtons.Nazor) => new(WindowFrameOwner.Nazor, false, titleBar, buttons);
}

/// <summary>An immutable ordered list of complete configurations, including desktop defaults for preferences.</summary>
public sealed class WindowDecorationPolicy
{
    /// <summary>The default desktop policy: complete native decorations, then complete Nazor decorations.</summary>
    public static WindowDecorationPolicy Default { get; } = Prefer(WindowDecorations.Native(), WindowDecorations.Nazor());
    /// <summary>The primary requested configuration.</summary>
    public WindowDecorations Primary => Options[0];
    /// <summary>All authorized configurations, in priority order; the list is an immutable snapshot.</summary>
    public IReadOnlyList<WindowDecorations> Options { get; }

    private WindowDecorationPolicy(WindowDecorations primary, WindowDecorations[] fallbacks, bool includeDefaults = false)
    {
        ArgumentNullException.ThrowIfNull(primary);
        ArgumentNullException.ThrowIfNull(fallbacks);
        if (fallbacks.Any(item => item is null)) throw new ArgumentException("Fallback configurations cannot be null.", nameof(fallbacks));
        var options = new[] { primary }.Concat(fallbacks);
        if (includeDefaults) options = options.Concat([WindowDecorations.Native(), WindowDecorations.Nazor()]).Distinct();
        Options = Array.AsReadOnly(options.ToArray());
    }

    /// <summary>Requires one exact configuration, with no implicit substitution.</summary>
    /// <param name="configuration">The required configuration.</param>
    /// <returns>The strict policy.</returns>
    public static WindowDecorationPolicy Require(WindowDecorations configuration) => new(configuration, []);

    /// <summary>Tries the supplied complete configurations in order, then complete native and Nazor decorations, keeping only the first occurrence of each configuration.</summary>
    /// <param name="primary">The first configuration to try.</param>
    /// <param name="fallbacks">Additional preferences to try before the desktop defaults.</param>
    /// <returns>The ordered policy including desktop defaults. Unsupported configurations fall back; invalid application content still fails.</returns>
    public static WindowDecorationPolicy Prefer(WindowDecorations primary, params WindowDecorations[] fallbacks) => new(primary, fallbacks, true);
}

/// <summary>A configuration accepted by a backend, distinct from temporary fullscreen visibility.</summary>
/// <param name="Configuration">The accepted complete configuration.</param>
/// <param name="OptionIndex">Zero for the primary option; higher values identify an explicit or default fallback in the policy's Options.</param>
public sealed record WindowDecorationResult(WindowDecorations Configuration, int OptionIndex);
