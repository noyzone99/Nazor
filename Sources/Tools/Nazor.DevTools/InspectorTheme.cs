using Nazor.Themes.NazorLeaf;

namespace Nazor.DevTools;

/// <summary>
/// The inspector's look: the NazorLeaf theme for its controls, with an application sheet that adds the inspector tokens
/// (markup syntax colors, frame budgets, box-model colors) and the panel styles shared by its components.
/// </summary>
public static class InspectorTheme
{
    public static Nazor.Style.StyleSheet Theme => NazorLeafTheme.Theme;
    public static Nazor.Style.StyleSheet StyleSheet => StyleSheets.DevTools;
}
