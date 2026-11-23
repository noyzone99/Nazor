using Nazor.Style;

namespace Nazor.Themes.Windows7;

/// <summary>Provides Windows 7 Aero-inspired control styles.</summary>
public static class Windows7Theme
{
    /// <summary>Gets the Windows 7 stylesheet over Basic.</summary>
    public static StyleSheet Theme { get; } = StyleSheet.Combine(global::Nazor.Themes.Themes.Basic, StyleSheets.Windows7);
}
