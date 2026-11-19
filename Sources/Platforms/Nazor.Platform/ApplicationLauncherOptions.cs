namespace Nazor;

/// <summary>Independent runtime features of the application's taskbar or Dock entry.</summary>
/// <param name="SupportsJumpList">Whether runtime launcher tasks can be published.</param>
/// <param name="SupportsBadgeCount">Whether a numeric badge can be requested.</param>
/// <param name="SupportsBadgeIcon">Whether a custom image badge can be requested.</param>
public readonly record struct ApplicationLauncherCapabilities(
    bool SupportsJumpList, bool SupportsBadgeCount, bool SupportsBadgeIcon);

/// <summary>A launcher task that starts the current application with individual command-line arguments.</summary>
/// <param name="Title">The nonempty, localized task label, without NUL characters.</param>
/// <param name="Arguments">Individual arguments, including empty strings when needed. No argument may contain NUL.</param>
public sealed record JumpListTask(string Title, IReadOnlyList<string> Arguments);
