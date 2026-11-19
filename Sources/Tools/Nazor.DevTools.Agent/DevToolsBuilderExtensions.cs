using Nazor.DevTools;
using Nazor.Input;

namespace Nazor;

public static class DevToolsBuilderExtensions
{
    public static NazorApplicationBuilder UseDevTools(this NazorApplicationBuilder builder, string? discoveryDirectory = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.Diagnostics.ConfigureWindow += window => window.EnableDevTools(discoveryDirectory);
        return builder;
    }

    public static NazorApplicationBuilder UseDevTools(this NazorApplicationBuilder builder, DevToolsOptions options)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(options);
        options.Validate();
        builder.Diagnostics.ConfigureWindow += window => window.EnableDevTools(options);
        return builder;
    }
}

/// <summary>Development-only attachment and launch configuration, independent of the target application's key bindings.</summary>
public sealed record DevToolsOptions
{
    /// <summary>The private discovery directory, or null for the current user's default location.</summary>
    public string? DiscoveryDirectory { get; init; }
    /// <summary>The normalized shortcut key, matched without case sensitivity. Null disables keyboard activation.</summary>
    public string? ShortcutKey { get; init; } = "F12";
    /// <summary>The exact modifiers required by the shortcut.</summary>
    public KeyModifiers ShortcutModifiers { get; init; }
    /// <summary>Whether to add an inspector command to the native system menu where supported. Defaults to true.</summary>
    public bool ShowInSystemMenu { get; init; } = true;

    internal void Validate()
    {
        if (ShortcutKey is { } key && (key.Length == 0 || key.Length > 64 || key.Any(char.IsControl)))
            throw new ArgumentException("ShortcutKey must be a normalized key name of 1 to 64 characters, or null.");
        if ((ShortcutModifiers & ~(KeyModifiers.Control | KeyModifiers.Shift | KeyModifiers.Alt | KeyModifiers.Meta)) != 0)
            throw new ArgumentOutOfRangeException(nameof(ShortcutModifiers));
    }
    internal string MenuText
    {
        get
        {
            if (ShortcutKey is null) return "Open Nazor DevTools";
            var parts = new List<string>();
            if (ShortcutModifiers.HasFlag(KeyModifiers.Control)) parts.Add("Ctrl");
            if (ShortcutModifiers.HasFlag(KeyModifiers.Alt)) parts.Add("Alt");
            if (ShortcutModifiers.HasFlag(KeyModifiers.Shift)) parts.Add("Shift");
            if (ShortcutModifiers.HasFlag(KeyModifiers.Meta)) parts.Add("Win");
            parts.Add(ShortcutKey == " " ? "Space" : ShortcutKey.ToUpperInvariant());
            return "Open Nazor DevTools\t" + string.Join("+", parts);
        }
    }
}
