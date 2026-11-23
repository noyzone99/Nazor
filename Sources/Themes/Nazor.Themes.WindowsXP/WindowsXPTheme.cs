using Nazor.Style;

namespace Nazor.Themes.WindowsXP;

/// <summary>Provides Windows XP Luna-inspired control styles.</summary>
public static class WindowsXPTheme
{
    /// <summary>Gets the Windows XP stylesheet over Basic.</summary>
    public static StyleSheet Theme { get; } = StyleSheet.Combine(global::Nazor.Themes.Themes.Basic, StyleSheets.WindowsXP);
}
