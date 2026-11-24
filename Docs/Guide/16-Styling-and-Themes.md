# 16. Styling and themes

NSS (Nazor Style Sheets) is Nazor's CSS-like styling language. Stylesheets compile during the build into typed runtime data. Unsupported properties, values, and selectors produce source-positioned errors. NSS intentionally has a defined subset of CSS; browser behavior is not a fallback implementation.

## Where styles belong

| Source | Ownership and activation |
| --- | --- |
| `Card.nazor.nss` | Scoped to `Card`; collected automatically |
| Top-level `@style` blocks in `Card.nazor` | Same scope, applied after the sidecar in source order |
| Standalone `Theme.nss` | Global sheet exposed as `StyleSheets.Theme`; active only when assigned |
| A control's `Style` parameter | Runtime declarations on that element |

Start with a component sidecar for local structure and a standalone sheet for shared design tokens or application-wide rules. Inline style is useful for a value that comes from data, but it is parsed at runtime rather than checked by the build.

PIN cells center their editable line box as well as their text and masks. Basic, Fluent and NazorLeaf allow enlarged fonts to grow the cell. When overriding a cell's height, set a corresponding inner `line-height`; retain the editable block layout so caret placement and pointer text selection use the same geometry.

The Gallery's NazorLeaf color and shape tabs display values from the rendered styles, including seed, scheme and custom-token changes. Its radius slider scales the control/overlay/pane/window token family together, following the reference's 11/16/22/28 proportions and fixed small corners. These tokens style content and explicit theme frame samples; the ordinary gallery retains its system window decorations. A preview without a live scene shows the token name instead of an invented color value.

## Cascade

From weakest to strongest:

1. Control theme: `app.Appearance.Theme`, or `Nazor.Themes.Themes.Basic` when null.
2. Application sheet: `app.Appearance.StyleSheet`.
3. Window sheet: `IWindow.StyleSheet`.
4. Component sheets: sidecar followed by `@style` blocks.
5. Element inline declarations: `Style`.

A higher level wins regardless of selector specificity. Within a level, specificity wins, then source order. Between component scopes with equal specificity, the scope nearer the element wins before source order. `!important` is unsupported.

```csharp
app.Appearance.StyleSheet = MyApp.StyleSheets.Theme;
```

Replace `MyApp` with the application's root namespace. Qualifying the generated `StyleSheets` class in top-level startup avoids ambiguity with `Nazor.Controls.StyleSheets`, which is visible through the SDK's global usings.

Combine compiled sheets when they should occupy the same cascade level. Sheets are applied from left to right; a later sheet wins when selector specificity is equal. For example, keep a shared base sheet and add application overrides:

```csharp
app.Appearance.StyleSheet = StyleSheet.Combine(
    MyApp.StyleSheets.Base,
    MyApp.StyleSheets.Brand);
```

`Combine` includes `@keyframes` too, with definitions from later sheets taking precedence when names match. The combined sheet follows replacements to its source sheets, including development hot reload. Use `app.Appearance.Theme` to combine sheets at the theme level; use `app.Appearance.StyleSheet` to combine them at the application level. The application level still takes precedence over the theme level.

```razor
@window
<TextBlock>Window-specific styling</TextBlock>

@code {
    protected override void OnInitialized()
    {
        Window.StyleSheet = StyleSheets.Comfortable;
    }
}
```

For a runtime change, assign another compiled sheet, for example `Window.StyleSheet = StyleSheets.Compact`. Changing a window sheet updates that cascade level; it does not replace the application sheet or control theme.

## Scope follows the markup author

A component sheet normally selects only elements declared by that component. The parent's sheet does not automatically select a child's private markup. Projected `ChildContent` and typed template fragments remain owned by the caller. Built-in visual parts such as `CheckBoxMark` share the declaring control's ownership.

Custom properties inherit through these boundaries. Expose a token or a `Class` parameter for intentional customization. Use [component-qualified selectors and `::deep`](17-Selectors-and-Media-Queries.md#component-boundaries) for deliberate access across boundaries. A custom component does not automatically create an element bearing its class name.

## Choose a theme and a color scheme

Basic is the SDK's built-in theme. It is selected when `app.Appearance.Theme` is null and through `Nazor.Themes.Themes.Default`. Its NSS rules provide system colors, borders, dimensions, and focus cues. None is an empty stylesheet for applications that supply their own control styles.

For a new project, the [template's `--Theme` option](02-Getting-Started.md#template-options) selects Basic, Fluent, or NazorLeaf and generates the matching startup and package references.

```csharp
app.Appearance.Theme = Nazor.Themes.Themes.None;
app.Appearance.StyleSheet = MyApp.StyleSheets.Theme;
app.Appearance.ColorScheme = Nazor.Style.ColorScheme.System;
```

Set application appearance on the UI thread. Changing a theme updates existing windows and their defaults. The color scheme is independent. System follows platform preferences, while Light and Dark request a variant.

### Theme package versions

Optional themes use the same major, minor and patch numbers as the SDK and carry an alpha prerelease label. An SDK version already labeled `-alpha` or `-alpha.<qualifier>` supplies its full prerelease version. For other SDK versions, the theme package uses the matching numeric version with `-alpha`. The app template generates the matching theme reference when you choose Fluent or NazorLeaf. Basic ships with the SDK.

Install the matching alpha release of `Nazor.Themes.Fluent` and set `app.Appearance.Theme = Nazor.Themes.Fluent.Themes.Fluent` to use WinUI-inspired styling over the Basic control geometry. The theme uses WinUI 3 color tokens and follows WinUI templates for control states, metrics, corner radii and dialog layout. [Fluent package reference](../ApiReferences/Nazor.Themes.Fluent.md)

The same package includes private Fluent System Icons fonts and the typed `FluentSymbol` catalog for `Icon.Symbol`. Register them with `builder.Fonts.AddFluentFonts()` before `Build()`. The packaged icons work with any theme. Fluent decorative parts prefer exact covered Segoe Fluent Icons or Segoe MDL2 Assets glyphs and fall back to these packaged fonts, so register them whenever you select it. `WinUISymbol` exposes the same native-preferred policy for application icons; `FluentSymbol` always selects the packaged family. [Icon setup](22-Images-Icons-and-Drawing.md#typed-fluent-symbols)


### Control density

Set `app.Appearance.ControlDensity` to `ControlDensity.Compact`, `Touch` or `Automatic` on the UI thread. Automatic follows each host's `IPlatformWindow.PrefersTouchInput` hint, falling back to compact when unavailable. An individual window can set `IWindow.ControlDensity`; an explicit window choice wins over application and host updates, and Automatic restores inheritance. Density is independent of viewport width and native pointer-event classification.

NSS can test the resolved choice with `@media (density: touch)` or `(density: compact)`. Conditions support nesting, JSON hot reload and generated C# in the same way as the other media features. Basic and Fluent retain their existing metrics unless application styles use this condition. NazorLeaf switches its control/menu/row/body tokens from 34/34/46/14 to 42/42/54/15 logical pixels, with larger toggle targets and list/table rows. The gallery's density selector uses this shared application setting.

The shared host hint is available to custom and future backends. Existing desktop backends use its compact fallback; no automatic touch-device discovery or Android/iOS host verification is claimed. Application and window overrides work with these backends. For headless style evaluation, supply `MediaPreferences(..., Density: ControlDensity.Touch)` to select touch rules; Automatic/default preferences fall back to compact. The appended record density value also appears in `MediaPreferences` and `MediaCondition` deconstruction.

### NazorLeaf

Reference the matching alpha release of `Nazor.Themes.NazorLeaf`. In `Program.cs`, keep the application's namespace import for `App`, register the fonts before `Build()`, and select the theme before `Run()`.

```csharp
using Nazor.Themes.NazorLeaf;

var builder = NazorApplication.CreateBuilder(args);
builder.Fonts.AddNazorLeafFonts();
builder.Application.UseMainWindow<App>();
using var app = builder.Build();
app.Appearance.Theme = NazorLeafTheme.Theme;
return app.Run();
```

`NazorLeafTheme.Theme` layers the NazorLeaf rules over Basic. The package copies the Geist, Geist Mono, Bricolage Grotesque and NazorLeaf Icons fonts, and their notices to `Themes/NazorLeaf` in build and publish output. The same `AddNazorLeafFonts()` call registers all four fonts. Use `Icon Symbol="@LeafSymbol.Plus"` for the theme's typed icons with any active theme. Customize the theme in your application stylesheet with tokens such as `--leaf-primary`, `--leaf-on-primary`, `--leaf-pc`, `--leaf-on-pc` and `--leaf-ink`. Check each color scheme you support. A `Badge` that decorates an icon on a colored bar should set `--leaf-badge-cutout` to the bar color, so the 2 px cutout around the leaf matches. Slider thumb gaps reveal the actual surface underneath, including gradients and images; no surface-color token is required. NazorLeaf also styles the Windows taskbar overlay badge as a seed-colored leaf. Text inherits a 1.5 line height from the theme root, as in the prototype. Field buttons such as clear, reveal and the search mark draw NazorLeaf icons in `::before` and hide Basic's vector glyphs with `stroke-trim-end: 0`; anchored menus and drop-down lists open 6 px from their anchor. [NazorLeaf API](../ApiReferences/Nazor.Themes.NazorLeaf.md) · [Packaging theme assets](28-Libraries-Packaging-and-Deployment.md#theme-packages)

## Desktop-inspired themes

Reference the matching alpha release of the theme package. After building the application, assign its stylesheet to `app.Appearance.Theme`.

```csharp
using Nazor.Themes.Adwaita;

var builder = NazorApplication.CreateBuilder(args);
builder.Fonts.AddAdwaitaFonts();
builder.Application.UseMainWindow<App>();
using var app = builder.Build();
app.Appearance.Theme = AdwaitaTheme.Theme;
app.Appearance.ColorScheme = Nazor.Style.ColorScheme.Dark;
return app.Run();
```

| Theme package | Stylesheet | Palettes |
| --- | --- | --- |
| [Nazor.Themes.Adwaita](../ApiReferences/Nazor.Themes.Adwaita.md) | `AdwaitaTheme.Theme` | Light and dark |
| [Nazor.Themes.Breeze](../ApiReferences/Nazor.Themes.Breeze.md) | `BreezeTheme.Theme` | Light and dark |
| [Nazor.Themes.Windows9x](../ApiReferences/Nazor.Themes.Windows9x.md) | `Windows9xTheme.Theme` | Light |
| [Nazor.Themes.WindowsXP](../ApiReferences/Nazor.Themes.WindowsXP.md) | `WindowsXPTheme.Theme` | Light |
| [Nazor.Themes.Windows7](../ApiReferences/Nazor.Themes.Windows7.md) | `Windows7Theme.Theme` | Light |
| [Nazor.Themes.Cde](../ApiReferences/Nazor.Themes.Cde.md) | `CdeTheme.Theme` | Light and dark |

These packages layer compiled NSS over Basic. Adwaita follows libadwaita 1.10 from GNOME 51 and includes Adwaita Sans and Mono. Register those faces before building the application to use them on every host. The other desktop themes use installed faces through the font provider. In normal appearance, the light-only themes use their light palette even when the app chooses Dark. All six use Basic's system palette and control rules when forced colors are active. Window decorations follow the application's window settings.

Adwaita uses overlay scrollbars with a narrow indicator that widens under the pointer. Breeze reserves its Qt-style scrollbar gutter. The three historical Windows styles and CDE reserve gutters with end buttons. Thumb colors also respond to pointer hover and dragging. See [scrollbar styling](20-Layout-and-Scrolling.md#scrolling) to adjust these choices in your app.

Try each theme with the Gallery's theme and color scheme selectors. **ScrollView** demonstrates both scrollbar axes. **TitleBar and window controls** previews Nazor-drawn decorations. Gallery and headless tests cover control styles and interaction. Native toolkit comparisons remain part of the alpha verification. [Gallery](../../Samples/Nazor.Samples.Gallery/README.md)

## Design tokens

Leaf uses a uniform window surface by default. It becomes translucent only while a requested backdrop is actually active; otherwise it retains an opaque tinted surface. Text fields sit on that surface with a slightly darker edge and a faint drop shadow in light mode, set by `--leaf-field-edge` and `--leaf-sh-field`; dark mode keeps the plain hairline.

Override the shared properties defined in [Basic.nss](../../Sources/Core/Nazor.Controls/Themes/Basic.nss). For example, override both sides of the accent color pair to preserve contrast.

```css
:root {
    --nz-accent: #6941c6;
    --nz-on-accent: white;
}
```

Other common properties include `--nz-surface`, `--nz-on-surface`, `--nz-control`, `--nz-border`, `--nz-field` and `--nz-selection`. Verify readability and focus in light, dark and forced colors.

### Document custom properties in the editor

Place a documentation comment immediately above an NSS variable to show its description in hover and completion details:

```nss
:root {
    /** <doc>Primary accent used by actions and selected elements.</doc> */
    --app-accent: #1478d4;
}

.primary { color: var(--app-accent); }
```

The convention works in `.nss`, scoped `.nazor.nss` and inline `@style`, including unsaved edits. Multiline comments may use leading `*` characters and blank paragraphs. Descriptions are plain text; XML entities such as `&amp;` are decoded, and Markdown punctuation is escaped. Ordinary comments, empty/malformed `<doc>` blocks or an intervening comment/declaration do not attach documentation.

Lookup stays within the current stylesheet or inline block, matching existing variable completion visibility. A declaration shows its own description. References prefer the declaration in their own rule; otherwise all declarations of that name must agree. Ambiguous or undocumented variables retain generic help, including built-in system-variable descriptions. These comments do not change runtime values or the cascade.

## System palettes and backend fallbacks

Basic uses the window's resolved system palette. Its forced-colors rules keep text, selection and focus legible. Replacing the theme changes the active stylesheet while existing controls retain their state.

On Windows, normal appearance supplies the accent palette. Classic `GetSysColor` values are used for forced colors, because they do not describe modern dark application surfaces.

| Backend names | NSS access | Purpose |
| --- | --- | --- |
| CSS system names: `Canvas`, `CanvasText`, `Field`, `FieldText`, `ButtonFace`, `ButtonText`, `SelectedItem`, `SelectedItemText`, etc. | Direct color keywords or `SystemColors.Property(name)` | Standard surfaces and foregrounds |
| `AccentLight1` through `AccentLight3`, `AccentDark1` through `AccentDark3` | `var(--nz-system-accent-light-1)`, etc. | Optional native accent ramp |
| `CanvasAlternate`, `CanvasAlternateText`, `CanvasTextSecondary` | `var(--nz-system-canvas-alternate)`, etc. | Layers and secondary text |
| `ButtonHover`, `ButtonHoverText`, `ButtonPressed`, `ButtonPressedText`, `ButtonDisabled`, `ButtonDisabledText` | `var(--nz-system-button-hover)`, etc. | Paired button states |
| `FieldHover`, `FieldHoverText`, `FieldDisabled`, `FieldDisabledText` | `var(--nz-system-field-hover)`, etc. | Paired input states |
| `Header`, `HeaderText`, `Tooltip`, `TooltipText` | `var(--nz-system-tooltip-text)`, etc. | Header and tooltip pairs |
| `Focus` | `var(--nz-system-focus)` | Focus decoration |
| `Success`, `Warning`, `Error`, plus each `Background` and `Text` suffix | `var(--nz-system-success-background)`, etc. | Status decorations and message pairs |
| `InfoBackground`, `InfoText`, `Destructive`, `DestructiveText` | `var(--nz-system-info-text)`, etc. | Information messages and destructive commands |

### System color keywords and theme tokens

System color keywords are dynamic aliases, not fixed color literals. `background: Canvas` is equivalent to `background: var(--nz-system-canvas)`. The compiler preserves the reference rather than capturing the build machine's color. Palette updates apply without recompiling the stylesheet. A resolved system role can contain a native, derived or fallback color as described above.

The `--nz-system-*` namespace identifies framework palette variables, but it is not protected or read-only. They inherit and participate in the ordinary custom-property cascade. For example, overriding `--nz-system-canvas` on an element also changes `Canvas` on that element and descendants that inherit the override:

```css
.preview {
    --nz-system-canvas: pink;
    background: Canvas; /* Equivalent to var(--nz-system-canvas): pink. */
}
```

Prefer theme tokens such as `--nz-surface`, `--nz-text` and `--nz-accent` for application customization. Their defaults can reference system keywords while callers can replace the theme token independently:

```css
:root {
    --nz-surface: Canvas;
    --nz-text: CanvasText;
}

.panel {
    background: var(--nz-surface);
    color: var(--nz-text);
}

.branded-panel {
    --nz-surface: #f4efff;
    --nz-text: #241247;
}

.tooltip-preview {
    background: var(--nz-system-tooltip);
    color: var(--nz-system-tooltip-text);
}
```

An element with both `panel` and `branded-panel` classes uses the branded theme tokens; direct `Canvas`/`CanvasText` declarations still use the resolved system variables. Preserve matching foreground/background pairs, especially in forced colors. For an intentional system-palette override, use the exact variable name returned by `SystemColors.Property(name)`: standard keywords use names such as `--nz-system-canvastext`, while extra roles use kebab-case such as `--nz-system-tooltip-text`.

## Inline values

```razor
<Container Class="progress-fill" Style="@($"width: {progress}%")" />
```

Use invariant formatting for numeric style strings when the value can contain a decimal separator. Inline properties override all stylesheet rules. An inline custom property can provide data to a rule while leaving that rule's layout/appearance configurable.

Inline parsing skips unknown properties and malformed entries; invalid substituted values resolve as `unset`. Repeated identical inline strings are cached. Prefer compiled NSS for static declarations so mistakes fail the build.

A third-party theme is an ordinary compiled NSS library exposing `StyleSheets` properties. It should document supported controls, variants, fonts/assets, framework versions, and dependencies. No runtime package discovery or download is required. See [libraries](28-Libraries-Packaging-and-Deployment.md).

---

[Guide index](README.md) · [API reference](../API.md)

## Window decoration composition

A theme can associate statically registered title-bar and physical-side button presenters with its stylesheet using `WithWindowDecorations(new WindowDecorationPresenters(...))`. Each delegate receives a WindowDecorationContext containing the window, optional declaring window component and physical side. Use `context.OpenComponent<T>(builder, sequence)` to preserve the window surface's scoped styling while retaining a static component reference. Presenters render requested content; they must not change decoration policy or select native controls by operating-system identity.

StyleSheet.Combine inherits the last supplied presenter metadata. Rule-only overrides preserve earlier metadata. Basic and Fluent use common title/icon/caption composition. NazorLeaf registers its Leaf surface and leaf controls; Windows can use them too when Nazor buttons are explicitly selected. Changing Appearance.Theme replaces active Nazor presenters while preserving the decoration policy. User fragments and native title bars remain selected as requested. No optional theme is bundled into the SDK.

Each shipped theme styles the border, corner radii and shadow of a Nazor frame with a client title bar. Use `:root[frame-owner="nazor"][client-titlebar]` for those defaults. A captionless window can set its frame appearance explicitly. The desktop styles native frames. The Theming sample switches the theme on two real windows so you can compare active and inactive captions. [Theming](../../Samples/Nazor.Samples.Theming/README.md) · [Frame property reference](19-NSS-Property-Reference.md#window-frame-and-resize)
