using Nazor.Style;

namespace Nazor.Themes.Windows9x;

/// <summary>Provides Windows 95 and 98-inspired control styles.</summary>
public static class Windows9xTheme
{
    /// <summary>Gets the Windows 9x stylesheet over Basic.</summary>
    public static StyleSheet Theme { get; } = StyleSheet.Combine(global::Nazor.Themes.Themes.Basic, StyleSheets.Windows9x);
}
