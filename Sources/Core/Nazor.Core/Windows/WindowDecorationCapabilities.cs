namespace Nazor;

/// <summary>Support known without changing the native window.</summary>
public enum WindowSupportStatus
{
    /// <summary>Negotiation or additional observations are required.</summary>
    Unknown,
    /// <summary>The environment cannot satisfy the request.</summary>
    Unsupported,
    /// <summary>The backend supports the request under the currently known conditions.</summary>
    Supported,
}

/// <summary>The support of a complete configuration, with a diagnostic explanation.</summary>
/// <param name="Status">The currently known support.</param>
/// <param name="Reason">A diagnostic message, or null when supported.</param>
public readonly record struct WindowDecorationSupport(WindowSupportStatus Status, string? Reason = null);

/// <summary>Immutable backend decoration capabilities, independent of application templates and operation policy.</summary>
public sealed record WindowDecorationCapabilities
{
    /// <summary>A backend with no known desktop-decoration support.</summary>
    public static WindowDecorationCapabilities None { get; } = new();
    /// <summary>Support for an external native frame.</summary>
    public WindowSupportStatus NativeFrame { get; init; } = WindowSupportStatus.Unsupported;
    /// <summary>Support for a frame drawn by Nazor.</summary>
    public WindowSupportStatus NazorFrame { get; init; } = WindowSupportStatus.Unsupported;
    /// <summary>Support for the original system title bar.</summary>
    public WindowSupportStatus NativeTitleBar { get; init; } = WindowSupportStatus.Unsupported;
    /// <summary>Support for client title-bar content while retaining a native external frame.</summary>
    public WindowSupportStatus ClientTitleBarOnNativeFrame { get; init; } = WindowSupportStatus.Unsupported;
    /// <summary>Support for original native caption controls inside a client title bar on a native frame.</summary>
    public WindowSupportStatus IntegratedNativeCaptionButtons { get; init; } = WindowSupportStatus.Unsupported;
    /// <summary>Support for native controls without retaining a native external frame.</summary>
    public WindowSupportStatus NativeCaptionButtonsOnNazorFrame { get; init; } = WindowSupportStatus.Unsupported;
    /// <summary>Whether the backend can change frame ownership without replacing the native window.</summary>
    public WindowSupportStatus FrameChanges { get; init; } = WindowSupportStatus.Unsupported;
    /// <summary>Whether the backend can change the caption configuration on the current frame.</summary>
    public WindowSupportStatus CaptionChanges { get; init; } = WindowSupportStatus.Unsupported;
    /// <summary>The accepted configuration used when evaluating a runtime transition.</summary>
    public WindowDecorations? Current { get; init; }

    /// <summary>Checks a complete configuration without changing the window or validating application fragments.</summary>
    /// <param name="configuration">The complete request.</param>
    /// <returns>Supported, unsupported or unknown with a diagnostic reason.</returns>
    public WindowDecorationSupport GetSupport(WindowDecorations configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        var checks = new List<WindowDecorationSupport>
        {
            new(configuration.Frame == WindowFrameOwner.Native ? NativeFrame : NazorFrame,
                configuration.Frame == WindowFrameOwner.Native ? "The native frame is unavailable." : "The Nazor frame is unavailable.")
        };
        if (configuration.HasNativeTitleBar) checks.Add(new(NativeTitleBar, "The original native title bar is unavailable."));
        else if (configuration.Frame == WindowFrameOwner.Native)
            checks.Add(new(ClientTitleBarOnNativeFrame, "Client title-bar content on a native frame is unavailable."));
        if (!configuration.HasNativeTitleBar && configuration.CaptionButtons == WindowCaptionButtons.Native)
            checks.Add(new(configuration.Frame == WindowFrameOwner.Native ? IntegratedNativeCaptionButtons : NativeCaptionButtonsOnNazorFrame,
                "Native caption buttons are unavailable on the requested frame and title bar."));
        return Combine(checks);
    }

    /// <summary>Checks the complete transition from Current without changing the native window.</summary>
    /// <param name="configuration">The requested configuration.</param>
    /// <returns>The configuration and transition support.</returns>
    public WindowDecorationSupport GetChangeSupport(WindowDecorations configuration)
    {
        var support = GetSupport(configuration);
        if (Current == configuration || Current is null) return support;
        var status = Current.Frame == configuration.Frame ? CaptionChanges : FrameChanges;
        return Combine([support, new(status, "The backend cannot change the requested decorations on an open window.")]);
    }

    internal WindowDecorationResult? Select(WindowDecorationPolicy policy, bool transition, out string reason)
    {
        ArgumentNullException.ThrowIfNull(policy);
        var reasons = new List<string>();
        for (var index = 0; index < policy.Options.Count; index++)
        {
            var support = transition ? GetChangeSupport(policy.Options[index]) : GetSupport(policy.Options[index]);
            if (support.Status != WindowSupportStatus.Unsupported)
            {
                reason = support.Reason ?? "";
                return new(policy.Options[index], index);
            }
            reasons.Add($"Option {index}: {support.Reason}");
        }
        reason = string.Join(" ", reasons);
        return null;
    }

    private static WindowDecorationSupport Combine(IEnumerable<WindowDecorationSupport> checks)
    {
        var result = new WindowDecorationSupport(WindowSupportStatus.Supported);
        foreach (var check in checks)
        {
            if (check.Status == WindowSupportStatus.Unsupported) return check;
            if (check.Status == WindowSupportStatus.Unknown) result = check;
        }
        return result;
    }
}
