using Nazor.Style;

namespace Nazor.Themes;

/// <summary>
/// The themes included with <c>Nazor.Controls</c>. Basic is the default when an application selects no theme;
/// None removes every theme rule so the application's own stylesheets draw the controls from scratch.
/// </summary>
public static class Themes
{
    /// <summary>
    /// The default theme: system colors, plain one-pixel borders and the essential geometry that tells a button from
    /// text or a drop-down, without decoration, transitions or animations.
    /// </summary>
    public static StyleSheet Basic => global::Nazor.Controls.StyleSheets.Basic;

    /// <summary>An empty theme for applications that supply every control style themselves.</summary>
    public static StyleSheet None => StyleSheet.Empty;

    /// <summary>The theme used when an application selects none; currently <see cref="Basic" />.</summary>
    public static StyleSheet Default => Basic;
}
