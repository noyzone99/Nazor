# API reference

The reference describes the public and protected framework APIs in Nazor `0.1.0-alpha`. The package pages document declarations, usage contracts, and available source links. The managed runtime and theme packages contain .NET 8/9/10 assets. Applications can target .NET 8 or later; .NET 11 selects the compatible .NET 10 assets. The DevTools Protocol page identifies the declarations available on .NET Standard 2.0.

For concepts, examples, and recommended usage, read the [developer guide](Guide/README.md). Start with the [package map](Guide/31-Architecture-and-Extension-Points.md#runtime-package-map) when choosing a reference.

## Packages

| Package | Reference |
| --- | --- |
| `Nazor.Accessibility` | [Nazor.Accessibility](ApiReferences/Nazor.Accessibility.md) |
| `Nazor.Animation` | [Nazor.Animation](ApiReferences/Nazor.Animation.md) |
| `Nazor.Application` | [Nazor.Application](ApiReferences/Nazor.Application.md), including [application launcher counts and icons](ApiReferences/Nazor.Application.md#type-iapplicationlauncher) |
| `Nazor.Controls` | [Nazor.Controls](ApiReferences/Nazor.Controls.md) |
| `Nazor.Core` | [Nazor.Core](ApiReferences/Nazor.Core.md) |
| `Nazor.Diagnostics` | [Nazor.Diagnostics](ApiReferences/Nazor.Diagnostics.md) |
| `Nazor.DevTools.Agent` | [Nazor.DevTools.Agent](ApiReferences/Nazor.DevTools.Agent.md) — optional window attachment |
| `Nazor.DevTools.Protocol` | [Nazor.DevTools.Protocol](ApiReferences/Nazor.DevTools.Protocol.md) — optional .NET contracts and transport |
| `Nazor.Graphics` | [Nazor.Graphics](ApiReferences/Nazor.Graphics.md) |
| `Nazor.Hosting.Desktop` | [Nazor.Hosting.Desktop](ApiReferences/Nazor.Hosting.Desktop.md) |
| `Nazor.Hosting.Desktop.Linux` | [Shared desktop hosting API](ApiReferences/Nazor.Hosting.Desktop.md) — Linux backend only |
| `Nazor.Hosting.Desktop.Windows` | [Shared desktop hosting API](ApiReferences/Nazor.Hosting.Desktop.md) — Windows backend only |
| `Nazor.Hosting.Windows` | [Nazor.Hosting.Windows](ApiReferences/Nazor.Hosting.Windows.md) |
| `Nazor.Input` | [Nazor.Input](ApiReferences/Nazor.Input.md) |
| `Nazor.Layout` | [Nazor.Layout](ApiReferences/Nazor.Layout.md) |
| `Nazor.Platform` | [Nazor.Platform](ApiReferences/Nazor.Platform.md) |
| `Nazor.Platform.Windows` | [Nazor.Platform.Windows](ApiReferences/Nazor.Platform.Windows.md) |
| `Nazor.Platform.Linux` | [Nazor.Platform.Linux](ApiReferences/Nazor.Platform.Linux.md) |
| `Nazor.Presentation` | [Nazor.Presentation](ApiReferences/Nazor.Presentation.md) |
| `Nazor.Renderer.Skia` | [Nazor.Renderer.Skia](ApiReferences/Nazor.Renderer.Skia.md) |
| `Nazor.Style` | [Nazor.Style](ApiReferences/Nazor.Style.md) |
| `Nazor.Themes.Fluent` | [Nazor.Themes.Fluent](ApiReferences/Nazor.Themes.Fluent.md) — optional theme, private icon fonts and typed symbols |
| `Nazor.Themes.NazorLeaf` | [Nazor.Themes.NazorLeaf](ApiReferences/Nazor.Themes.NazorLeaf.md) — optional theme, its controls, private fonts and typed icons |

## API usage notes

`SearchField<TItem>` provides localized no-results content by default, `EmptyText` and `EmptyContent` overrides, customizable `Placeholder`, and optional query highlighting through `HighlightMatches`, `HighlightTemplate` and `RenderHighlightedText`. See [search suggestions](Guide/10-Text-Input-and-Data-Controls.md#search-with-typed-suggestions).

`TitleBar`, `UserTitleBar` and `DragRegion` retain client input for custom pointer handlers, tooltips and compound field frames while empty areas remain draggable. Pointer-handler focus requests survive default routing. See [title-bar input](Guide/14-Title-Bars-and-Platform-Integration.md#compose-a-title-bar) and [`InputManager.RegionAt`](ApiReferences/Nazor.Input.md#regionat).

The Windows adapter also configures native passthrough regions so actual desktop input reaches application title-bar controls instead of being intercepted by Windows App SDK's input sink. See [Windows integration](ApiReferences/Nazor.Platform.Windows.md).

`Span` and semantic inline text controls share shaping, wrapping, selection, baseline and link-fragment behavior in running text. Low-level `TextRun`, styled `ShapedText`, source-ranged `DrawText` and `LayoutNode.ArrangeChildren` provide the reusable text/geometry contracts. See [running text and limits](Guide/10-Text-Input-and-Data-Controls.md#format-running-text).

Badge.Icon and IndicatorContent provide typed/custom status visuals while ChildContent remains the decorated target. Keyed BadgeValue replacements support theme-owned count roll without remounting editors; accessibility exposes the actual uncapped count. [Badge contracts](ApiReferences/Nazor.Controls.md#badge)

`ItemsTransition<TItem>.PreserveExtentDuringExit` and RenderedCountChanged support bottom-anchored retained collections without a second lifetime model. ToastHost composes them for bounded outgoing notification visuals while the queue owns lifecycle and announcements. See [feedback motion](Guide/23-Transitions-and-Animations.md#feedback-motion).

`InlineMessage` composes a measured Collapsible viewport for retained height/spacing collapse and close/reopen reversal. Custom exit styles target `InlineMessagePresence`; the inner message keeps its original element and caller attributes. See [retained exits and migration](Guide/23-Transitions-and-Animations.md#retained-exits).

Menu motion uses factual popup-origin corner states, theme submenu-open-delay and an optional HoverIntent timer provider. Shared retained reopen metadata preserves current animation values. [Contracts](Guide/11-Commands-Menus-and-Popups.md#menu-timing-and-interrupted-closing)

`HoverTracker` composes scoped pointer highlights with `Indicator`; `Virtualizer<TItem>.OverlayContent` keeps decorations outside item realization. Scroll events retain their original source. [Composition and limitations](Guide/10-Text-Input-and-Data-Controls.md#decorative-group-hover-highlights)

`ControlDensity` supplies application/window density overrides and a host touch-input hint. NSS `(density: touch)`/`(density: compact)` tests the resolved choice; NazorLeaf maps it to shared control/menu/row/text tokens. [Control density](Guide/16-Styling-and-Themes.md#control-density)

ListView/GridView/TreeView support opt-in application-owned reorder requests, and virtualized row metrics follow theme density. See [collection contracts and migration](Guide/10-Text-Input-and-Data-Controls.md#display-and-select-collections).

Overlay focus reveal, wheel chaining and keyboard scrolling stop at the active surface rather than scrolling its logical owner. See [overlay scrolling](Guide/11-Commands-Menus-and-Popups.md#popovers-and-dialogs) and the `Scene`/`InputManager` contracts in [Nazor.Presentation](ApiReferences/Nazor.Presentation.md) and [Nazor.Input](ApiReferences/Nazor.Input.md).

`Expander.ExpandDirection` chooses content above or below its header; `KeepMounted` preserves closed local state. `Collapsible` measures natural content, exposes retained close phases and supports height or clipped-translation motion. See [measured disclosure motion](Guide/23-Transitions-and-Animations.md#measured-disclosure-motion).

`Tabs.Presentation` distinguishes navigation from document sheets. Document width modes, add/close shortcuts and opt-in keyed reorder/drag-out requests use application-owned models. [Tab behavior and migration](Guide/12-Navigation.md#section-and-document-tabs)

`ContentTransition`, `LayoutTransition` and keyed `ItemsTransition<TItem>` provide page lifetime, measured relocation and connected destination geometry without replacing local component state. `Frame` reports Back/Forward direction; page guards can be scoped to the exact history entry. See [content and layout motion](Guide/23-Transitions-and-Animations.md#content-and-layout-motion).

`Calendar` header keyboard order is title, previous, next. Themes control its visual order and choose localized `CalendarWeekdayAbbreviated` or `CalendarWeekdayShortest` parts. See [choosing dates](Guide/10-Text-Input-and-Data-Controls.md#choose-dates).

`ElementStates.PopupAbove`, `PopupLeft` and `PopupRight` with NSS `:popup-above`, `:popup-left` and `:popup-right` expose final anchored placement, including native flips; Fluent select/combo reveal animations use the resulting edge. See [resolved popup styling](Guide/17-Selectors-and-Media-Queries.md#visual-selectors).

Checkbox vector parts `CheckBoxCheck` and `CheckBoxMixed` persist across state changes. Stroke/scale transitions and reversals affect paint while semantic state updates immediately; reduced motion applies destinations directly. See [checkbox motion and theming](Guide/23-Transitions-and-Animations.md#transitions).

Built-in compound text editors draw keyboard focus on their field frame; standalone editable primitives and independent actions retain their own cues. See [text input focus styling](Guide/10-Text-Input-and-Data-Controls.md#choose-an-editor).

The gallery demonstrates native single/multiple file and folder selection, named glob filters and cancellation preservation through existing `FileField.Pick` and `IFileDialogService` APIs. See [picking files and folders](Guide/10-Text-Input-and-Data-Controls.md#pick-files-and-folders).

`SegmentedDatePicker` and `SegmentedTimePicker` share a nullable committed value, separate draft columns and Confirm/Cancel lifecycle through `SegmentedPickerBase<TValue>`. See [segmented date/time usage](Guide/10-Text-Input-and-Data-Controls.md#choose-segmented-dates-and-times) and the [picker contracts](ApiReferences/Nazor.Controls.md#segmentedpickerbasetvalue).

`ColorPicker` supplies a composed RGBA editor with six rectangular HSV planes, numeric/hex drafts and opacity preview. Hex keeps CSS #rrggbbaa order in every theme. See [color editing](Guide/10-Text-Input-and-Data-Controls.md#choose-a-color) and [axis composition](Guide/37-Semantic-Primitives.md#rectangular-color-composition).

Existing slider APIs expose vertical orientation, visual ticks and interactive step snapping; the gallery now demonstrates their combinations. See [numeric range controls](Guide/10-Text-Input-and-Data-Controls.md#adjust-numeric-ranges).

`Switch` previews a captured thumb drag separately from its committed value/accessibility state. Release commits the nearest end once; Escape, lost capture and state changes cancel immediately. See [switch interaction and travel](Guide/10-Text-Input-and-Data-Controls.md#toggle-a-setting).

`Dial.WheelChanges` enables focus-preserving, bounded wheel adjustment with fractional-notch/precision-pixel handling. It defaults to false. See [range input behavior](Guide/10-Text-Input-and-Data-Controls.md#adjust-numeric-ranges).

`FlipView<TItem>` browses one realized item with a custom template, bounded navigation and localized position semantics. See [one-item browsing](Guide/10-Text-Input-and-Data-Controls.md#browse-one-item-at-a-time) and its [member contracts](ApiReferences/Nazor.Controls.md#flipviewtitem).

`NumberField.AcceptsExpression` explicitly enables bounded, culture-aware arithmetic input. It defaults to false; existing numeric editors need no migration. See [numeric expression grammar and commit behavior](Guide/10-Text-Input-and-Data-Controls.md#choose-an-editor).

`SpinButtonPlacement` now uses semantic `Compact`/`Inline` names in place of `Stacked`/`Sides`. Themes control their geometry, including Fluent's focused Compact flyout. See [numeric editor migration](Guide/10-Text-Input-and-Data-Controls.md#choose-an-editor).

`SearchField<TItem>.IconPlacement` selects `SearchIconPlacement.ThemeDefault`, `Start` or `End` independently of scopes and clear actions. The reusable `Combobox<TItem>.InputAdornment` slot places an adornment directly beside its input for theme ordering. See [search field placement](Guide/10-Text-Input-and-Data-Controls.md#choose-from-known-items).

`SearchField<TItem>` provides typed/custom suggestions, cancellable asynchronous providers and separate `TextEdited`, `SuggestionChosen` and `QuerySubmitted` events. The nongeneric type is replaced by `SearchField<string>` for string searches. See [search contracts and migration](Guide/10-Text-Input-and-Data-Controls.md#search-with-typed-suggestions).

Fluent search actions use compact search/dismiss symbols inside wider action areas. See [field presentation](Guide/10-Text-Input-and-Data-Controls.md#choose-an-editor).

`Combobox<TItem>.IsItemSelected` separates committed selection from the current suggestion; ComboBox supplies it from Value. [Choice-list behavior](Guide/10-Text-Input-and-Data-Controls.md#choose-an-editor).

Fluent Calendar and DatePicker retain their footprint while zooming to months/years and use circular period selection. [Calendar geometry](Guide/10-Text-Input-and-Data-Controls.md#choose-dates).

`DropDownPlacement.Center` overlays a choice list on its field, with selected-row alignment and existing viewport/native-work-area sizing and scroll limits. [Placement configuration](Guide/10-Text-Input-and-Data-Controls.md#choose-from-known-items).

`OverlayContainer.ExitDuration` and `OnExited` coordinate theme-controlled closing surfaces through `Presence`. `IOverlayMotion` supplies current host motion preferences and NSS exit durations without a controls-to-presentation dependency. See [closing motion](Guide/11-Commands-Menus-and-Popups.md#closing-motion) and [retained exits](Guide/23-Transitions-and-Animations.md#retained-exits).

NSS pseudo-elements use `PseudoElement`, `CompoundSelector.PseudoElement`, `StyleEngine.GetPseudoStyle`, `StyleEngine.GetGeneratedElement`, and `StyleProperties.Content` in [Nazor.Style](ApiReferences/Nazor.Style.md). See [generated content and editable text styling](Guide/17-Selectors-and-Media-Queries.md#pseudo-elements) for supported syntax and limits.

`ControlStateSelector` resolves explicit role/editor/field metadata, including IME-aware placeholder state through `ElementStates.Composing`. See the [control-state selector contract](ApiReferences/Nazor.Style.md#type-controlstateselector).

Window decoration selection uses complete `WindowDecorationPolicy` configurations with capability checks: `Require` is strict; `Prefer` tries explicit preferences before complete native and Nazor defaults. See [title bars and platform integration](Guide/14-Title-Bars-and-Platform-Integration.md) for native, Nazor and User providers and runtime changes.

Windows launcher badge styling includes application fonts, layered backgrounds, per-side borders, mathematical positioning, static transforms and alpha masks. See the [badge property reference](Guide/19-NSS-Property-Reference.md#windows-launcher-badges) and [platform snapshot contract](ApiReferences/Nazor.Platform.md#type-windowsbadgeappearance).

The [Linux desktop reference](ApiReferences/Nazor.Platform.Linux.md#desktop-hosting) describes Xwayland resize outset behavior and its separation from client layout and rendering dimensions.

The standard Nazor title bar uses `IWindow.EffectiveIcon`: an explicit `Window.Icon`, otherwise the backend's application icon. Linux desktop supplies the embedded Nazor application tile; Windows resolves the executable icon. See [per-window icons](Guide/13-Windows-and-Dialogs.md#per-window-icons).

`IWindow.NativeHandle` exposes a borrowed, backend-described `NativeWindowHandle`; see [native window interop](Guide/13-Windows-and-Dialogs.md#native-window-interop) for lifetime and dispatcher requirements.

Window roots keep their caption fixed while `WindowContent` defaults to automatic scrolling; see [scrolling defaults and overrides](Guide/20-Layout-and-Scrolling.md#scrolling).

App-launched DevTools use an exported native owner token and close with their source window; see [DevTools lifetime](DevTools.md).

Dependency versions are selected per framework to preserve .NET 8 support while using current packages on .NET 10/11; see [framework-specific dependencies](Guide/28-Libraries-Packaging-and-Deployment.md#framework-specific-dependency-versions).

The Razor/NSS compiler and editor language server run on .NET 10 LTS independently of the application's target. Their bundled Roslyn/Razor services support C# 15 and follow the application's evaluated language version, conditional symbols and references; see [build tools and application targets](Guide/03-Projects-and-Builds.md#build-tools-and-application-targets).

The SDK properties `NazorApplicationId`, `NazorWindowsAppManifest` and `NazorLinuxDesktopEntry` are documented in [application identity and platform files](Guide/03-Projects-and-Builds.md#application-identity). The application ID is embedded as assembly metadata and consumed by the Linux and unpackaged Windows backends without renaming the assembly.

For supported build and tooling contracts, see [projects and builds](Guide/03-Projects-and-Builds.md), [editors and hot reload](Guide/26-Editors-and-Hot-Reload.md), and the [samples](../Samples/README.md). The [NSS reference](Guide/19-NSS-Property-Reference.md) documents stylesheet syntax and property applicability. The [standalone DevTools guide](DevTools.md) describes F12 and IDE launch, manual connection, and retained state. The [wire protocol](DevTools-Protocol.md) documents the interoperable contract for all DevTools clients.

[`Icon.Symbol`](ApiReferences/Nazor.Controls.md#icon) accepts the renderer-independent [`IFontSymbol`](ApiReferences/Nazor.Core.md#ifontsymbol) contract and immutable `FontSymbol` pairs. The Fluent package adds native-preferred `WinUISymbol` alternatives while preserving the fixed portable `FluentSymbol` catalog. The [icon guide](Guide/22-Images-Icons-and-Drawing.md#typed-fluent-symbols) covers packaged Fluent symbols, raw font glyphs and SVG masks.

The [popup guide](Guide/11-Commands-Menus-and-Popups.md#popup-surfaces-outside-the-window) describes per-window external popup opt-in and capability reporting. Select and ComboBox placement, preferred visible item counts, Basic suggestion styling, editable placeholders and bounded keyboard navigation with automatic scrolling are defined in the [controls reference](ApiReferences/Nazor.Controls.md).

The [selector guide](Guide/17-Selectors-and-Media-Queries.md) defines control states, filtered physical positions and relative `:has()` matching. Their compiled contracts are in [Nazor.Style](ApiReferences/Nazor.Style.md).

[CustomStateSet](ApiReferences/Nazor.Core.md#type-customstateset) publishes immutable named states; `Control.CustomStates` targets the control root and [NamedStateSelector](ApiReferences/Nazor.Style.md#type-namedstateselector) matches them.

[ValidationFeedback](ApiReferences/Nazor.Core.md#type-validationfeedback) controls interaction/submission eligibility for `:user-valid` and `:user-invalid`; [Form](ApiReferences/Nazor.Controls.md#form) applies it before submit/reset callbacks.

[StylePartSet](ApiReferences/Nazor.Style.md#type-stylepartset) exposes existing physical elements through component-qualified `::part(name)`. `Control.Parts` publishes declarations; `TextFieldBase.InputParts` explicitly forwards the built-in `input` part.

[LogicalItemPosition](ApiReferences/Nazor.Core.md#type-logicalitemposition) supports logical collection selectors independently of realization; [ComponentRootSelector](ApiReferences/Nazor.Style.md#type-componentrootselector) uses scoped declaration/instance roots.

`DataRow<TItem>.ItemPosition` preserves global provider ordinals/counts through asynchronous paging and local grouping. See [provider-backed DataGrid positions](Guide/10-Text-Input-and-Data-Controls.md#provider-backed-datagrid-positions) and the [Controls API](ApiReferences/Nazor.Controls.md#datarowtitem).

[`Virtualizer<TItem>`](ApiReferences/Nazor.Controls.md#virtualizertitem) keeps realized rows when scroll notifications preserve the range or measurements preserve item sizes. See [collection usage](Guide/10-Text-Input-and-Data-Controls.md#display-and-select-collections).

[`StyleEngine.TotalRecomputedCount`](ApiReferences/Nazor.Style.md#totalrecomputedcount) includes explicit and lazy updates over an engine's lifetime. Differences between two readings measure style work across input and frame processing; `LastRecomputedCount` describes only the most recent update.

`Dialog` defaults to theme-owned `DialogSize.Auto` and no implicit Enter/accent action. Set nullable `DefaultButton` to select one; explicit button overrides must leave at most one effective default. See [window-local dialogs and migration](Guide/13-Windows-and-Dialogs.md#window-local-dialogs).

`PipsPager` selects a page through a bounded dot window, independently of content. Bind its zero-based selection to `FlipView` or another presenter. [Pager composition](Guide/12-Navigation.md#dot-page-navigation)

[`NumberPresenter`](ApiReferences/Nazor.Controls.md#numberpresenter) separates displayed scalar motion from actual data/accessibility. `Spinner` uses it for coordinated ring, percentage, gap and completion presentation; `ring-shape` and unitless numeric calculations support reusable rounded-path progress. See [numeric presentation](Guide/23-Transitions-and-Animations.md#numeric-presentation).

`Button.RepeatInterval` and `Pressable.RepeatInterval` provide repeat-button behavior through existing controls: initial press action, configurable delay/interval, held keyboard/pointer cleanup and UI-dispatched TimeProvider timers. [Held actions](Guide/11-Commands-Menus-and-Popups.md#held-repeat-actions)

`BreadcrumbBar<TItem>` presents an application-owned path with measured ancestor overflow, keyed header identity and typed invocation requests. [Breadcrumb navigation](Guide/12-Navigation.md#breadcrumb-paths)

`NavigationView<TItem>` composes adaptive panes, stable keyed hierarchy, child menus and measured selection while the application owns routing/content. [Adaptive application navigation](Guide/12-Navigation.md#adaptive-application-navigation)

Ordered `filter` and `backdrop-filter` share `FilterValue`; low-level drawing uses copied `ImageFilter` chains. See [effects and pre-release migration](Guide/22-Images-Icons-and-Drawing.md#element-backdrop-filters).

Element/background blending and explicit isolation use shared `BlendMode`/`PushBlend` contracts. See [color blending](Guide/22-Images-Icons-and-Drawing.md#color-blending-and-isolation).

`font-feature-settings` and opt-in `font-synthesis` reach style-aware shaping, fallback, immutable text-style identity and raster caches. See [OpenType features and synthetic styles](Guide/22-Images-Icons-and-Drawing.md#opentype-features-and-synthetic-styles).
