using Nazor.Style;

namespace Nazor.Themes.Cde;

/// <summary>Provides Common Desktop Environment-inspired control styles.</summary>
public static class CdeTheme
{
    /// <summary>Gets the CDE stylesheet over Basic.</summary>
    public static StyleSheet Theme { get; } = StyleSheet.Combine(global::Nazor.Themes.Themes.Basic, StyleSheets.Cde);
}
