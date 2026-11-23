using Nazor.Graphics.Text;
using Nazor.Style;

namespace Nazor.Themes.Adwaita;

/// <summary>Provides control styles based on Libadwaita 1.10 and registers the packaged Adwaita fonts.</summary>
public static class AdwaitaTheme
{
    /// <summary>The application-relative directory containing the fonts and their license and source notices.</summary>
    public const string AssetDirectory = "Themes/Adwaita";

    /// <summary>The Adwaita Sans and Adwaita Mono font paths, relative to the application directory.</summary>
    public static IReadOnlyList<string> FontFiles { get; } =
    [
        $"{AssetDirectory}/AdwaitaSans-Regular.ttf",
        $"{AssetDirectory}/AdwaitaSans-Italic.ttf",
        $"{AssetDirectory}/AdwaitaMono-Regular.ttf",
        $"{AssetDirectory}/AdwaitaMono-Italic.ttf",
        $"{AssetDirectory}/AdwaitaMono-Bold.ttf",
        $"{AssetDirectory}/AdwaitaMono-BoldItalic.ttf",
    ];

    /// <summary>Registers the packaged Adwaita faces as private fonts without adding duplicate paths.</summary>
    /// <param name="fonts">The font options to configure before application startup.</param>
    /// <returns>The same font options, for chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="fonts" /> is null.</exception>
    public static FontOptions AddAdwaitaFonts(this FontOptions fonts)
    {
        ArgumentNullException.ThrowIfNull(fonts);
        var missing = FontFiles.Where(file => !fonts.PrivateFontFiles.Contains(file)).ToArray();
        return missing.Length == 0 ? fonts : fonts.Add(missing);
    }

    /// <summary>Gets the Adwaita stylesheet over Basic, with light and dark palettes.</summary>
    public static StyleSheet Theme { get; } = StyleSheet.Combine(global::Nazor.Themes.Themes.Basic, StyleSheets.Adwaita);
}
