using Nazor.Graphics.Text;
using Nazor.Style;
using Nazor.Rendering;

namespace Nazor.Themes.NazorLeaf;

/// <summary>
/// NazorLeaf, the Nazor visual language: two wide corners on one diagonal and two nearly sharp ones on every shape, a small
/// leaf (the seed) that marks the selection, filled elements that cast their own color, a uniform window
/// material and the four-leaf mark (<see cref="LeafMark" />) as brand and loading indicator. Surfaces are tinted by the
/// seed color, so every application gets its own shade.
/// </summary>
/// <remarks>
/// Select the theme with <c>app.Appearance.Theme = NazorLeafTheme.Theme</c> and register its fonts before
/// <c>Build</c> with <c>builder.Fonts.AddNazorLeafFonts()</c>; text can fall back to system fonts, while typed icons require the private icon font. The
/// theme sits on top of Basic, so a control part that NazorLeaf does not draw keeps its Basic look. Retune the seed with the
/// <c>--leaf-primary</c>, <c>--leaf-on-primary</c>, <c>--leaf-pc</c>, <c>--leaf-on-pc</c> and <c>--leaf-ink</c>
/// custom properties of <c>:root</c>, in both color schemes. The <c>Spinner</c> is drawn as the proportional ring: a full
/// value stroke over a thinner track, with the percentage in medium and large rings and a check at 100%. Badges are leaves
/// with a 2 px cutout in <c>--leaf-badge-cutout</c> (the pane by default); set it to the color behind decorated content,
/// such as a toolbar or dock. Slider thumb gaps reveal the surface underneath, including gradients and images;
/// no surface-color token is required.
/// </remarks>
public static class NazorLeafTheme
{
    private static StyleSheet? theme;

    /// <summary>The folder, relative to the application directory, that holds the theme fonts and their notices.</summary>
    public const string AssetDirectory = "Themes/NazorLeaf";

    /// <summary>The NazorLeaf rules alone, without the Basic rules underneath.</summary>
    public static StyleSheet Rules => StyleSheets.NazorLeaf;

    /// <summary>The NazorLeaf theme: Basic with the NazorLeaf rules on top.</summary>
    public static StyleSheet Theme => theme ??= StyleSheet.Combine(global::Nazor.Themes.Themes.Basic, StyleSheets.NazorLeaf)
        .WithWindowDecorations(new(TitleBarPresenter, CaptionButtonsPresenter));

    private static RenderFragment TitleBarPresenter(WindowDecorationContext context) => builder =>
    {
        context.OpenComponent<LeafTitlebar>(builder, 0);
        builder.AddComponentParameter(1, nameof(LeafTitlebar.Title), context.Window.Title);
        builder.CloseComponent();
    };

    private static RenderFragment CaptionButtonsPresenter(WindowDecorationContext context) => builder =>
    {
        var side = context.IsLeft ? LeafWindowButtonsSide.Left : LeafWindowButtonsSide.Right;
        if (LeafWindowButtons.ButtonsFor(context.Window.SystemButtonsLayout, side).Count == 0) return;
        context.OpenComponent<LeafWindowButtons>(builder, 0);
        builder.AddComponentParameter(1, nameof(LeafWindowButtons.Side), side);
        builder.CloseComponent();
    };

    /// <summary>
    /// The font files of the theme, relative to the application directory: Geist for the interface, Geist Mono for
    /// code and shortcuts, Bricolage Grotesque for titles, and NazorLeaf Icons for typed symbols. Text fonts are variable.
    /// </summary>
    public static IReadOnlyList<string> FontFiles { get; } =
        [$"{AssetDirectory}/Geist.ttf", $"{AssetDirectory}/GeistMono.ttf", $"{AssetDirectory}/BricolageGrotesque.ttf", $"{AssetDirectory}/NazorLeafIcons.otf"];

    /// <summary>Registers the fonts of the NazorLeaf theme as private families.</summary>
    /// <param name="fonts">The font options of the application builder.</param>
    /// <returns>The same options, for chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="fonts" /> is null.</exception>
    public static FontOptions AddNazorLeafFonts(this FontOptions fonts)
    {
        ArgumentNullException.ThrowIfNull(fonts);
        var missing = FontFiles.Where(file => !fonts.PrivateFontFiles.Contains(file)).ToArray();
        return missing.Length == 0 ? fonts : fonts.Add(missing);
    }
}
