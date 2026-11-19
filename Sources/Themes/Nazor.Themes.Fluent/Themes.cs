using Nazor.Style;
using Nazor.Graphics.Text;

namespace Nazor.Themes.Fluent;

/// <summary>Optional Fluent theme resources.</summary>
public static class Themes
{
    /// <summary>The application-relative directory containing the icon fonts and their license notices.</summary>
    public const string AssetDirectory = "Themes/Fluent";

    /// <summary>The Regular and Filled icon font paths, relative to the application directory.</summary>
    public static IReadOnlyList<string> FontFiles { get; } = Array.AsReadOnly(new[]
    {
        $"{AssetDirectory}/FluentSystemIcons-Regular.ttf",
        $"{AssetDirectory}/FluentSystemIcons-Filled.ttf",
    });

    /// <summary>Registers the packaged Fluent System Icons fonts as private families, without duplicate paths.</summary>
    /// <param name="fonts">The font options to configure before application startup.</param>
    /// <returns>The same options for chaining.</returns>
    /// <exception cref="ArgumentNullException">The font options are null.</exception>
    public static FontOptions AddFluentFonts(this FontOptions fonts)
    {
        ArgumentNullException.ThrowIfNull(fonts);
        var missing = FontFiles.Where(file => !fonts.PrivateFontFiles.Contains(file)).ToArray();
        return missing.Length == 0 ? fonts : fonts.Add(missing);
    }

    /// <summary>The Fluent control theme, layered over Basic for the current control geometry.</summary>
    public static StyleSheet Fluent => Cache.Fluent;

    private static class Cache
    {
        internal static readonly StyleSheet Fluent = StyleSheet.Combine(global::Nazor.Themes.Themes.Basic, StyleSheets.Fluent);
    }
}
