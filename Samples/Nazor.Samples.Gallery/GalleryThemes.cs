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

    // Desktop-inspired themes present the gallery as an application of their desktop.
    internal static bool IsDesktop(string id) => id is "windows-9x" or "windows-xp" or "windows-7" or "cde" or "adwaita" or "breeze";

    internal static string DecorationKind(string id) => id switch
    {
        "adwaita" => "header-bar",
        _ when IsDesktop(id) => "themed-frame",
        _ => "application-caption",
    };

    // The classic desktops draw their own frame, title and caption buttons; GNOME places its controls in a header bar
    // with the search entry. The other themes keep the application caption over the native frame where supported.
    internal static WindowDecorationPolicy Decorations(string id) => DecorationKind(id) switch
    {
        "header-bar" => WindowDecorationPolicy.Prefer(WindowDecorations.Nazor(WindowTitleBarContent.User, WindowCaptionButtons.Nazor)),
        "themed-frame" => WindowDecorationPolicy.Prefer(WindowDecorations.Nazor(WindowTitleBarContent.Nazor, WindowCaptionButtons.Nazor)),
        _ => WindowDecorationPolicy.Prefer(
            WindowDecorations.Native(WindowTitleBarContent.User, WindowCaptionButtons.Native),
            WindowDecorations.Nazor(WindowTitleBarContent.User, WindowCaptionButtons.Nazor)),
    };

    internal static string Identify(StyleSheet? sheet) =>
        ReferenceEquals(sheet, NazorLeafTheme.Theme) ? "nazor-leaf" :
        Options.FirstOrDefault(option => ReferenceEquals(option.Sheet, sheet)).Id ?? "basic";
}
