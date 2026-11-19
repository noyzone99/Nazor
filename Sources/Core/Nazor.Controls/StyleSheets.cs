using Nazor.Style;

namespace Nazor.Controls;

/// <summary>The stylesheets compiled from the themes of <c>Nazor.Controls</c>.</summary>
public static partial class StyleSheets
{
    /// <summary>An empty theme for applications that supply every control style themselves.</summary>
    public static StyleSheet None => StyleSheet.Empty;

    /// <summary>The theme used when an application selects none; currently <see cref="Basic" />.</summary>
    public static StyleSheet Default => Basic;
}
