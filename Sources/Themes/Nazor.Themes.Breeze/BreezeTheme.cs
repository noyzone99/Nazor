using Nazor.Style;

namespace Nazor.Themes.Breeze;

/// <summary>Provides KDE Breeze-inspired control styles.</summary>
public static class BreezeTheme
{
    /// <summary>Gets the Breeze stylesheet over Basic.</summary>
    public static StyleSheet Theme { get; } = StyleSheet.Combine(global::Nazor.Themes.Themes.Basic, StyleSheets.Breeze);
}
