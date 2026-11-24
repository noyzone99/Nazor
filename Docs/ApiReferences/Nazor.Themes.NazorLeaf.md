# Nazor.Themes.NazorLeaf

Automatic virtualized List/Tree rows follow 46px Compact/54px Touch metrics; DataGrid includes a 2px internal gap in 48/56px row pitch. Explicit application heights override density. Surviving realized rows relocate with a 320ms spring and stable Tree chevrons rotate over 450ms; removal retention is available through the shared ItemsTransition primitive.

Reference the matching alpha release and register its bundled private fonts on the application builder. After building the application, select the theme through `app.Appearance.Theme`. [Theme package versions](../Guide/16-Styling-and-Themes.md#theme-package-versions)

Selected TimePicker rows center their values and place the seed 3 logical pixels inside the row's leading edge. This spacing is scoped to time options; ordinary menu seeds keep their own geometry.

```csharp
using Nazor;
using Nazor.Themes.NazorLeaf;

var builder = NazorApplication.CreateBuilder(args);
builder.Application.UseMainWindow<App>();
builder.Fonts.AddNazorLeafFonts();
using var app = builder.Build();
app.Appearance.Theme = NazorLeafTheme.Theme;
return app.Run();
```

The example uses an `App` component with `@window`. The theme combines Basic with the NazorLeaf rules. The package supplies Geist, Geist Mono, Bricolage Grotesque, the NazorLeaf Icons OpenType font, and their license notices under `Themes/NazorLeaf` in build and publish output. `AddNazorLeafFonts()` registers all four fonts. See the [theming guide](../Guide/16-Styling-and-Themes.md) for stylesheet precedence.

`ControlDensity.Touch` selects 42px controls/menus, 54px rows and 15px body text, with larger toggle/list/table targets; compact uses 34/34/46/14. Set `app.Appearance.ControlDensity`, or override an individual `IWindow.ControlDensity`. Automatic follows the host hint and falls back to compact when unavailable. [Density contracts](../Guide/16-Styling-and-Themes.md#control-density)

`LeafSymbol` implements the shared `IFontSymbol` contract. Use `<Icon Symbol="@LeafSymbol.Search" />` after importing `Nazor.Themes.NazorLeaf`; symbols select their font with any theme. Raw `Glyph` codes remain available with `font-family: "NazorLeaf Icons"`, which the NazorLeaf theme supplies by default for `Icon`. The icon font and original artwork are MIT-licensed; retain `LICENSE-NazorLeaf-Icons.txt` and `SOURCE-NazorLeaf-Icons.txt` with the font. Text fonts retain their Open Font License notices. [Icon usage](../Guide/22-Images-Icons-and-Drawing.md#typed-nazorleaf-symbols)

Neutral overlay scroll thumbs widen under the pointer and use the seed color while dragged. Forced colors use Basic's system-colored scrollbar. [Scrollbar styling](../Guide/20-Layout-and-Scrolling.md#scrolling)

The generated icon font preserves hollow and overlapping SVG regions on both DirectWrite and FreeType. Its closed contours use consistent nonzero winding, including nested holes. Icon mappings and source artwork are unchanged.

Menu motion separates the 500ms clip from 120ms opacity and follows four actual arranged corners. Submenus use a 60ms hover delay and 450ms reveal; closing retains the surface for 220ms with 220ms clip/180ms opacity. Retained reopen reverses current properties; reduced motion skips delays and retention. [Menu timing](../Guide/11-Commands-Menus-and-Popups.md#menu-timing-and-interrupted-closing)

Menus, menu bars, clickable settings rows, ListView, DataGrid and gallery tiles use one scoped moving hover mark (320ms geometry, 200ms opacity). First entry snaps, leaving fades, and scrolling stays in the owning viewport. Frozen grid cells retain opaque selection-compatible hover backing; static Table has no automatic tracker. Reduced motion and forced colors suppress decorative motion. [Hover composition](../Guide/10-Text-Input-and-Data-Controls.md#decorative-group-hover-highlights)

Leaf buttons, toggle buttons and menu buttons show a 600ms radial click sheen from the pointer position. Keyboard/accessibility activation starts at the center. Repeated presses restart one retained decorative pseudo element rather than accumulating spans, keeping the rendering footprint bounded; it settles transparent after the animation. The rounded button clips the effect. Disabled/busy, reduced-motion and forced-color states suppress it. Basic and Fluent do not draw this Leaf effect.

The signatures below describe the public and protected types and members declared by this package. Inherited members are documented with the declaring type.

## `LeafSymbol`

Namespace `Nazor.Themes.NazorLeaf`. [Source](../../Sources/Themes/Nazor.Themes.NazorLeaf/LeafSymbol.cs)

Named icons from the packaged `NazorLeaf Icons` font, generated from the theme's 55 original SVGs. Register the font with `builder.Fonts.AddNazorLeafFonts()` before startup, then use `<Icon Symbol="@LeafSymbol.Plus" />` with any theme. The default value has an empty glyph and is invalid as an active symbol. NSS controls color and displayed size; source artwork uses a 24 by 24 grid.

| Declaration | Contract |
| --- | --- |
| `public readonly struct LeafSymbol : IFontSymbol` | A NazorLeaf glyph with its matching private font family. |
| `public LeafSymbol()` | Creates the empty default value. |
| `public string FontFamily { get; }` | Internal font family name, `NazorLeaf Icons`. |
| `public string Glyph { get; }` | Unicode glyph text; empty for the default value. |
<!-- leaf-symbol-catalog:start -->
| `public static LeafSymbol ArrowDown { get; }` | NazorLeaf icon, glyph `\uE000`. |
| `public static LeafSymbol At { get; }` | NazorLeaf icon, glyph `\uE033`. |
| `public static LeafSymbol Back { get; }` | NazorLeaf icon, glyph `\uE001`. |
| `public static LeafSymbol Bell { get; }` | NazorLeaf icon, glyph `\uE002`. |
| `public static LeafSymbol Chat { get; }` | NazorLeaf icon, glyph `\uE032`. |
| `public static LeafSymbol Check { get; }` | NazorLeaf icon, glyph `\uE004`. |
| `public static LeafSymbol ChevronDown { get; }` | NazorLeaf icon, glyph `\uE009`. |
| `public static LeafSymbol ChevronLeft { get; }` | NazorLeaf icon, glyph `\uE003`. |
| `public static LeafSymbol ChevronRight { get; }` | NazorLeaf icon, glyph `\uE021`. |
| `public static LeafSymbol Close { get; }` | NazorLeaf icon, glyph `\uE005`. |
| `public static LeafSymbol Copy { get; }` | NazorLeaf icon, glyph `\uE006`. |
| `public static LeafSymbol Cursor { get; }` | NazorLeaf icon, glyph `\uE007`. |
| `public static LeafSymbol Cut { get; }` | NazorLeaf icon, glyph `\uE008`. |
| `public static LeafSymbol Dock { get; }` | NazorLeaf icon, glyph `\uE036`. |
| `public static LeafSymbol Dot { get; }` | NazorLeaf icon, glyph `\uE035`. |
| `public static LeafSymbol Download { get; }` | NazorLeaf icon, glyph `\uE00A`. |
| `public static LeafSymbol Error { get; }` | NazorLeaf icon, glyph `\uE00B`. |
| `public static LeafSymbol Eye { get; }` | NazorLeaf icon, glyph `\uE00C`. |
| `public static LeafSymbol File { get; }` | NazorLeaf icon, glyph `\uE00D`. |
| `public static LeafSymbol Folder { get; }` | NazorLeaf icon, glyph `\uE00E`. |
| `public static LeafSymbol Forward { get; }` | NazorLeaf icon, glyph `\uE00F`. |
| `public static LeafSymbol Globe { get; }` | NazorLeaf icon, glyph `\uE010`. |
| `public static LeafSymbol Home { get; }` | NazorLeaf icon, glyph `\uE011`. |
| `public static LeafSymbol Image { get; }` | NazorLeaf icon, glyph `\uE012`. |
| `public static LeafSymbol Info { get; }` | NazorLeaf icon, glyph `\uE013`. |
| `public static LeafSymbol Layers { get; }` | NazorLeaf icon, glyph `\uE014`. |
| `public static LeafSymbol Leaf { get; }` | NazorLeaf icon, glyph `\uE015`. |
| `public static LeafSymbol List { get; }` | NazorLeaf icon, glyph `\uE016`. |
| `public static LeafSymbol Mail { get; }` | NazorLeaf icon, glyph `\uE031`. |
| `public static LeafSymbol Maximize { get; }` | NazorLeaf icon, glyph `\uE017`. |
| `public static LeafSymbol Minimize { get; }` | NazorLeaf icon, glyph `\uE018`. |
| `public static LeafSymbol More { get; }` | NazorLeaf icon, glyph `\uE019`. |
| `public static LeafSymbol Motion { get; }` | NazorLeaf icon, glyph `\uE01A`. |
| `public static LeafSymbol Palette { get; }` | NazorLeaf icon, glyph `\uE01B`. |
| `public static LeafSymbol Paste { get; }` | NazorLeaf icon, glyph `\uE01C`. |
| `public static LeafSymbol Pause { get; }` | NazorLeaf icon, glyph `\uE034`. |
| `public static LeafSymbol Pen { get; }` | NazorLeaf icon, glyph `\uE01D`. |
| `public static LeafSymbol Pin { get; }` | NazorLeaf icon, glyph `\uE01E`. |
| `public static LeafSymbol Plus { get; }` | NazorLeaf icon, glyph `\uE01F`. |
| `public static LeafSymbol Redo { get; }` | NazorLeaf icon, glyph `\uE020`. |
| `public static LeafSymbol Search { get; }` | NazorLeaf icon, glyph `\uE022`. |
| `public static LeafSymbol Share { get; }` | NazorLeaf icon, glyph `\uE023`. |
| `public static LeafSymbol Sidebar { get; }` | NazorLeaf icon, glyph `\uE024`. |
| `public static LeafSymbol Sliders { get; }` | NazorLeaf icon, glyph `\uE025`. |
| `public static LeafSymbol Star { get; }` | NazorLeaf icon, glyph `\uE026`. |
| `public static LeafSymbol StarFilled { get; }` | NazorLeaf icon, glyph `\uE030`. |
| `public static LeafSymbol Table { get; }` | NazorLeaf icon, glyph `\uE027`. |
| `public static LeafSymbol Tabs { get; }` | NazorLeaf icon, glyph `\uE028`. |
| `public static LeafSymbol TextBox { get; }` | NazorLeaf icon, glyph `\uE029`. |
| `public static LeafSymbol Toggle { get; }` | NazorLeaf icon, glyph `\uE02A`. |
| `public static LeafSymbol Trash { get; }` | NazorLeaf icon, glyph `\uE02B`. |
| `public static LeafSymbol Typography { get; }` | NazorLeaf icon, glyph `\uE02C`. |
| `public static LeafSymbol Undo { get; }` | NazorLeaf icon, glyph `\uE02D`. |
| `public static LeafSymbol Warning { get; }` | NazorLeaf icon, glyph `\uE02E`. |
| `public static LeafSymbol Window { get; }` | NazorLeaf icon, glyph `\uE02F`. |
<!-- leaf-symbol-catalog:end -->

## `LeafMark`

Namespace `Nazor.Themes.NazorLeaf`. [Source](../../Sources/Themes/Nazor.Themes.NazorLeaf/Components/LeafMark.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed class LeafMark : Control` | The four-leaf mark of Nazor: four leaves in a two-by-two grid, in two tones of the seed color. It is the brand of an application (in the title bar, an about page or a splash screen) and, with `Nazor.Themes.NazorLeaf.LeafMark.IsLoading`, the loading indicator of the Leaf theme, whose leaves light up in turn while the mark slowly turns.<br>The element `LeafMark` contains four `LeafMarkPetal` parts with the `petal` attribute (1 to 4, in reading order) and carries the `loading` attribute. The mark is decorative unless it has an `Nazor.Controls.Control.AccessibleName`; while loading it is a `progressbar` named "Loading". Its size comes from `Nazor.Themes.NazorLeaf.LeafMark.Size`, or from the `--leaf-mark-size` custom property (28px by default). |
| `protected override void OnParametersSet()` | Validates the size. |
| `protected override void AddAttributes(RenderTreeBuilder builder)` | Adds the role, the loading state and the accessible name.<br>`builder`. The builder that receives the render frames. |
| `protected override void AddContent(RenderTreeBuilder builder)` | Adds the four leaves.<br>`builder`. The builder that receives the render frames. |
| `public LeafMark()` |  |
| `public bool IsLoading { get; set; }` | `[Parameter]`<br>Gets or sets whether the mark is a loading indicator. |
| `public double Size { get; set; }` | `[Parameter]`<br>Gets or sets the side of the mark in logical pixels; 0 uses the theme size. |
| `protected override string ElementName { get; }` | Gets the NSS element name emitted by LeafMark. |
| `protected override string? ElementStyle { get; }` | Gets the inline declarations, with the explicit size when one is set. |

## `LeafWindowButtons`

Namespace `Nazor.Themes.NazorLeaf`. [Source](../../Sources/Themes/Nazor.Themes.NazorLeaf/Components/LeafWindowButtons.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed class LeafWindowButtons : Control` | The leaf lights of the Leaf theme: minimize, maximize (restore while maximized) and close drawn by Nazor as three small leaves that take the colors of the four-leaf mark when the pointer rests on the group. They follow the system placement of the caption buttons (`Nazor.Controls.WindowCommands.Layout`) for one `Nazor.Themes.NazorLeaf.LeafWindowButtons.Side`, so a desktop that puts the buttons on the left gets them on the left, and they disappear while the window shows the system's own buttons (Windows and macOS), where the system draws them.<br>The element `LeafWindowButtons` (`group` role, `side` and `maximized` attributes) contains one `LeafWindowButton` per button, with the `kind` attribute (`minimize`, `maximize`, `restore` or `close`). Each button holds the `LeafWindowButtonLeaf` and its `LeafWindowButtonGlyph`, drawn by `LeafWindowButtonStroke` parts (`stroke` = `bar`, `rising`, `falling` or `corner`, with `at` = `top-end` or `bottom-start` for the corner). The buttons have localized names and tooltips and, like native caption buttons, are not Tab stops unless `Nazor.Themes.NazorLeaf.LeafWindowButtons.IsTabStop` is set. The maximize button is a native Snap Layouts area on Windows. |
| `public static IReadOnlyList<WindowButton> ButtonsFor(WindowButtonsLayout layout, LeafWindowButtonsSide side)` | Gets the buttons drawn for a side of a caption layout: the layout's own buttons, or minimize, maximize and close on the right when the layout has none on either side.<br>`layout`. The system caption layout.<br>`side`. The side of the title bar.<br>**Returns**. The buttons in physical left-to-right order, possibly none. |
| `protected override void OnParametersSet()` | Validates the parameters. |
| `protected override void BuildRenderTree(RenderTreeBuilder builder)` | Renders the buttons inside a `Nazor.Controls.WindowCommands` that follows the window.<br>`builder`. The builder that receives the render frames. |
| `protected override void AddAttributes(RenderTreeBuilder builder)` | Adds the group role, its name, the side and the window state.<br>`builder`. The builder that receives the render frames. |
| `protected override void AddContent(RenderTreeBuilder builder)` | Adds the buttons.<br>`builder`. The builder that receives the render frames. |
| `public LeafWindowButtons()` |  |
| `public LeafWindowButtonsSide Side { get; set; }` | `[Parameter]`<br>Gets or sets the side whose system buttons are drawn; the right side by default. |
| `public IReadOnlyList<WindowButton>? Buttons { get; set; }` | `[Parameter]`<br>Gets or sets the buttons in physical left-to-right order, instead of the system layout of `Nazor.Themes.NazorLeaf.LeafWindowButtons.Side`. |
| `public bool HideWithSystemButtons { get; set; }` | `[Parameter]`<br>Gets or sets whether the buttons are hidden while the window shows the system caption buttons; true by default. |
| `public bool IsTabStop { get; set; }` | `[Parameter]`<br>Gets or sets whether the buttons are reached with Tab; false by default, as native caption buttons. |
| `protected override string ElementName { get; }` | Gets the NSS element name emitted by LeafWindowButtons. |

## `LeafWindowButtonsSide`

Namespace `Nazor.Themes.NazorLeaf`. [Source](../../Sources/Themes/Nazor.Themes.NazorLeaf/Components/LeafWindowButtons.cs)

| Declaration | Contract |
| --- | --- |
| `public enum LeafWindowButtonsSide` | The side of the title bar on which `Nazor.Themes.NazorLeaf.LeafWindowButtons` take their buttons from the system layout. |
| `Right = 0` | The buttons the system places on the right; minimize, maximize and close when the system has no setting. |
| `Left = 1` | The buttons the system places on the left, as with `button-layout` on GNOME or on macOS. |

## `StyleSheets`

Namespace `Nazor.Themes.NazorLeaf`.

| Declaration | Contract |
| --- | --- |
| `public static class StyleSheets` | Stylesheets compiled from the standalone .nss files of the project. |
| `public static StyleSheet NazorLeaf { get; }` | Gets the stylesheet compiled from `NazorLeaf.nss`. |


<a id="type-leaftitlebar"></a>

### `LeafTitlebar`

Namespace `Nazor.Themes.NazorLeaf`.

The Leaf title-bar surface with its mark, display title and optional content slots. Decoration policy and the selected caption-button provider belong to the window. The element is TitleBar[source="nazor"] with the leaf class. Caption groups are supplied by the selected window configuration; this presenter does not select controls or inspect the operating system.

| Declaration | Contract |
| --- | --- |
| `public sealed class LeafTitlebar : ComponentBase` | The Leaf title-bar surface with its mark, display title and optional content slots. Decoration policy and the selected caption-button provider belong to the window. The element is TitleBar[source="nazor"] with the leaf class. Caption groups are supplied by the selected window configuration; this presenter does not select controls or inspect the operating system. |
| `protected override void OnParametersSet()` | Validates the height variant without changing the window's requested decorations. |
| `protected override void BuildRenderTree(RenderTreeBuilder builder)` | Renders a `Nazor.Controls.TitleBar` declared where this title bar is declared, with the Leaf parts in its slots. |
| `public LeafTitlebar()` | Creates a new instance. |
| `public IWindow Window { get; set; }` | Gets or sets the window that owns this title bar, supplied automatically. |
| `public RenderFragment? Leading { get; set; }` | Gets or sets content at the leading edge, after the left leaf lights, such as a back button. |
| `public RenderFragment? AppIcon { get; set; }` | Gets or sets the application icon, used instead of the four-leaf mark. |
| `public bool ShowMark { get; set; }` | Gets or sets whether the four-leaf mark is shown when no `Nazor.Themes.NazorLeaf.LeafTitlebar.AppIcon` is set; true by default. |
| `public string? Title { get; set; }` | Gets or sets the title text, often the window title. |
| `public RenderFragment? TitleContent { get; set; }` | Gets or sets rich title content, used instead of `Nazor.Themes.NazorLeaf.LeafTitlebar.Title`. |
| `public string? Subtitle { get; set; }` | Gets or sets a secondary text after the title, such as the document or the number of items. |
| `public RenderFragment? Center { get; set; }` | Gets or sets content in the middle that takes the free space, such as a search box. |
| `public RenderFragment? Trailing { get; set; }` | Gets or sets content at the trailing edge, before the right leaf lights, such as an account button. |
| `public RenderFragment? ChildContent { get; set; }` | Gets or sets content after the center, before the trailing content. |
| `public TitleBarHeight Height { get; set; }` | Gets or sets the height variant. |
| `public string? Class { get; set; }` | Gets or sets additional space-separated NSS classes of the title bar. |
| `public string? AccessibleName { get; set; }` | Gets or sets the accessible name of the title bar; the title by default. |

<a id="type-nazorleaftheme"></a>

### `NazorLeafTheme`

Namespace `Nazor.Themes.NazorLeaf`.

NazorLeaf, the Nazor visual language: two wide corners on one diagonal and two nearly sharp ones on every shape, a small leaf (the seed) that marks the selection, filled elements that cast their own color, a uniform window material and the four-leaf mark (`Nazor.Themes.NazorLeaf.LeafMark`) as brand and loading indicator. Surfaces are tinted by the seed color, so every application gets its own shade. Select the theme with app.Appearance.Theme = NazorLeafTheme.Theme and register its fonts before Build with builder.Fonts.AddNazorLeafFonts(); text can fall back to system fonts, while typed icons require the private icon font. The theme sits on top of Basic, so a control part that NazorLeaf does not draw keeps its Basic look. Retune the seed with the --leaf-primary, --leaf-on-primary, --leaf-pc, --leaf-on-pc and --leaf-ink custom properties of :root, in both color schemes. The Spinner is drawn as the proportional ring: a full value stroke over a thinner track, with the percentage in medium and large rings and a check at 100%. Badges are leaves with a 2 px cutout in --leaf-badge-cutout (the pane by default); set it to the color behind decorated content, such as a toolbar or dock. Slider thumb gaps reveal the surface underneath, including gradients and images; no surface-color token is required.

| Declaration | Contract |
| --- | --- |
| `public static class NazorLeafTheme` | NazorLeaf, the Nazor visual language: two wide corners on one diagonal and two nearly sharp ones on every shape, a small leaf (the seed) that marks the selection, filled elements that cast their own color, a uniform window material and the four-leaf mark (`Nazor.Themes.NazorLeaf.LeafMark`) as brand and loading indicator. Surfaces are tinted by the seed color, so every application gets its own shade. Select the theme with app.Appearance.Theme = NazorLeafTheme.Theme and register its fonts before Build with builder.Fonts.AddNazorLeafFonts(); text can fall back to system fonts, while typed icons require the private icon font. The theme sits on top of Basic, so a control part that NazorLeaf does not draw keeps its Basic look. Retune the seed with the --leaf-primary, --leaf-on-primary, --leaf-pc, --leaf-on-pc and --leaf-ink custom properties of :root, in both color schemes. The Spinner is drawn as the proportional ring: a full value stroke over a thinner track, with the percentage in medium and large rings and a check at 100%. Badges are leaves with a 2 px cutout in --leaf-badge-cutout (the pane by default); set it to the color behind decorated content, such as a toolbar or dock. Slider thumb gaps reveal the surface underneath, including gradients and images; no surface-color token is required. |
| `public const string AssetDirectory = "Themes/NazorLeaf"` | The folder, relative to the application directory, that holds the theme fonts and their notices. |
| `public static FontOptions AddNazorLeafFonts(this FontOptions fonts)` | Registers the fonts of the NazorLeaf theme as private families. The same options, for chaining. |
| `public static StyleSheet Rules { get; }` | The NazorLeaf rules alone, without the Basic rules underneath. |
| `public static StyleSheet Theme { get; }` | The NazorLeaf theme: Basic with the NazorLeaf rules on top. |
| `public static IReadOnlyList<string> FontFiles { get; }` | The font files of the theme, relative to the application directory: Geist for the interface, Geist Mono for code and shortcuts, Bricolage Grotesque for titles, and NazorLeaf Icons for typed symbols. Text fonts are variable. |

See [title bars and platform integration](../Guide/14-Title-Bars-and-Platform-Integration.md) for complete configuration policies, capability checks, explicit fallback and User content lifecycle.

## Document tabs

Navigation retains the recessed strip and moving seed. Document presentation uses pane-colored sheets, leaf corners and existing color tokens, retaining shared add/close/reorder/drag-out behavior. The design HTML does not supply a document-tab variant; this is a coherent theme extension, not measured reference parity. [Tab contracts](../Guide/12-Navigation.md#section-and-document-tabs)

Ordinary Button/ToggleButton/MenuButton presses use 80ms for each existing visual property while retaining its normal easing curve; icon-only actions retain separate 150ms color/background and 250ms transform timing. Slider and RangeSlider thumbs use real 6-to-10px narrow-axis press deformation and 24-to-28px cross-axis hover deformation over 200ms, including vertical and labeled variants. Percentage translation holds the thumb center on the track without scaling its labels. Value positions remain direct, so deformation timing does not add dragging lag. Interrupted dimension transitions continue from the visible size. Reduced motion applies targets immediately. Switch travel/width/radius timing remains 500/250/400ms.

Inline messages collapse their naturally measured flow box and sibling spacing over 350ms and fade over 200ms. Their `InlineMessagePresence` viewport owns exit retention; natural content padding remains stable during clipping. Interrupted close/reopen retains the same message and reverses its visible height. Reduced motion removes closed content immediately.

LeafSymbol.StarFilled (U+E030) is a filled form of the original MIT-licensed Star path; all 48 existing mappings remain stable. Bind ToggleButton.IsPressed and choose Star/StarFilled through Icon.Symbol to publish the selected artwork explicitly. Pressed icon-only toggles animate their icons with a 450ms scale/rotation pop; reduced motion, forced colors, disabled and busy states suppress that decorative animation.

Determinate Spinner follows the reference's 14% convergence per normalized 60Hz frame, 0.0005 fraction snap and 0.003 start/end boundaries. Percentage text, 7% track gaps and the vector SpinnerCheck follow the displayed sample while accessibility retains the target. Add class `leaf-ring` for the reference's rounded leaf path. Geometry scales at 18/40/56/84 logical units, including mini button rings; the completion stroke is 2.4/24 of its part. The font PUA completion mark has been removed. [Numeric presentation](../Guide/23-Transitions-and-Animations.md#numeric-presentation)

DialogContainer, Dialog and AlertDialog enter over 550ms from 60/8/60/8 corner radii with 16px vertical travel and 0.96 scale, then settle to their computed surface radius. Caller radius overrides remain the final destination. Closing retains the surface for 180ms, fading and shrinking to 0.97 scale. Closing during entry and reopening continue from the current visible properties instead of replaying entry. Reduced motion applies final values and removes closing surfaces immediately. This affects window-local dialog surfaces; native top-level frame policy is unchanged.
 The scrim has its own 300ms entry, continues that timeline through an interrupted close/reopen, and remains at full opacity until body removal; no separate scrim exit fade is added.

NavigationView adapts the shared hierarchy to leaf corners, pane surfaces and seed-shaped moving selection. This ready-control composition is a coherent extension: the supplied HTML showcase does not define this complete generic navigation API. [Navigation contract](../Guide/12-Navigation.md#adaptive-application-navigation)

Tooltips use 150ms opacity and 300ms travel/scale phases, preserving the current values on retained reopen. Toasts use 200ms opacity and 550ms travel/rotation phases in both directions; travel is 160% of each toast's own height. Their scoped keyed collection retains at most 32 outgoing notifications, reserving the last measured extent to keep the bottom anchor stable, then hides the non-overlay empty carrier. Reduced motion finishes exits immediately. The Leaf showcase places notifications bottom-center with 24px clearance and requests 4200ms durations, preserving its existing Undo/pause/announcement behavior. SDK ToastOptions.Duration remains five seconds unless the caller chooses another value.

ColorPicker reuses the shared six-plane HSV editor, numeric/hex drafts, opacity and comparison swatches. Its frame, thumb markers and tracks use the seed corner and ink tokens, with the normal Leaf focus ring. Alpha checkerboards show actual compositing. This ready picker is a theme adaptation beyond the supplied HTML inventory; it does not claim an unprovided native design reference.

The original Mail, Chat, At, Pause, Dot and Dock symbols use appended codepoints U+E031 through U+E036. All 49 earlier mappings, including StarFilled U+E030, remain stable. Use the typed LeafSymbol values with Icon.Symbol; the source SVGs and bundled font retain the existing MIT notice.

The added reference paths use the existing catalog regular stroke of 1.8 units. The HTML default is 1.7 units with distinct tile/badge stroke overrides; the packaged font does not claim those optical weights are identical.

Disclosure chevrons keep the supplied SVG geometry and proportional stroke: the 24-unit source stroke of 1.7 scales to 14px content, yielding 0.9916667 logical pixels. Expander and accordion arrows retain that weight through rotation, reduced motion and forced colors.

Badge.Icon uses the registered typed symbol in a square 14/18/22px indicator for small/medium/large sizes. BadgeIndicatorContent supports decorative custom markup without changing the decorated target. Changed raw counts remount one BadgeValue and roll 350ms from 90% own-height travel/opacity 0, independently of the 450ms indicator pop; reduced motion and forced colors suppress the roll. Supply AccessibleName for icon/custom status. The showcase now uses the original typed Mail/Chat/At/Pause/Dot/Dock symbols instead of unrelated shapes or text glyph substitutions. In-app badges remain distinct from platform launcher APIs.
