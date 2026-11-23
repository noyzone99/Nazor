using Nazor.Style;
using Nazor.Themes.Adwaita;
using Nazor.Themes.Breeze;
using Nazor.Themes.Windows9x;
using Nazor.Themes.WindowsXP;
using Nazor.Themes.Windows7;
using Nazor.Themes.Cde;
using Nazor.Themes.NazorLeaf;
using BuiltInThemes = Nazor.Themes.Themes;
using FluentThemes = Nazor.Themes.Fluent.Themes;

namespace Nazor.Samples.Gallery;

internal static class GalleryThemes
{
    internal static IReadOnlyList<(string Id, string Label, StyleSheet Sheet)> Options { get; } =
    [
        ("basic", "Basic", BuiltInThemes.Basic),
        ("fluent", "Fluent", FluentThemes.Fluent),
        ("nazor-leaf", "NazorLeaf", App.LeafTheme),
        ("adwaita", "Adwaita", AdwaitaTheme.Theme),
        ("breeze", "Breeze", BreezeTheme.Theme),
        ("windows-9x", "Windows 9x", Windows9xTheme.Theme),
        ("windows-xp", "Windows XP", WindowsXPTheme.Theme),
        ("windows-7", "Windows 7", Windows7Theme.Theme),
        ("cde", "CDE", CdeTheme.Theme),
        ("none", "None", BuiltInThemes.None),
    ];

    internal static StyleSheet? Find(string? id) => Options.FirstOrDefault(option => option.Id == id).Sheet;

    internal static string Identify(StyleSheet? sheet) =>
        ReferenceEquals(sheet, NazorLeafTheme.Theme) ? "nazor-leaf" :
        Options.FirstOrDefault(option => ReferenceEquals(option.Sheet, sheet)).Id ?? "basic";
}
