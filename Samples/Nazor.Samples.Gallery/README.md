# Nazor control gallery

The NazorLeaf showcase uses a horizontal section strip at widths up to 860px and a desktop rail above that width. They share the active section and retain one visible navigation Tab stop. Arrow/Home/End navigation reveals the chosen section; reduced motion makes both content and strip scrolling immediate.

The Gallery brings together live examples of Nazor controls and primitives, their Razor source, and a showcase of complete interfaces. [Download the portable Gallery](../../README.md#try-the-gallery).

## Run from source

Run these commands from the repository root. Start the DRM command from a Linux local console configured for direct display access. Use **Exit Gallery** to leave the single-view session. The buttons that open separate preview windows are disabled in this mode. [DRM/KMS prerequisites](../../Docs/Guide/34-Linux-DRM.md#runtime-dependencies-and-local-session)

```powershell
dotnet run --project Samples/Nazor.Samples.Gallery
dotnet run --project Samples/Nazor.Samples.Gallery -- --drm
```

Search by control name or browse the component families. Composition parts such as `TableHeaderCell` lead to a page that shows them in context. Each page renders a live example and displays the source that was compiled. The gallery opens in NazorLeaf. Use the sidebar to select a theme and choose System, Light or Dark.

The `--theme` option accepts `basic`, `fluent`, `nazor-leaf`, `adwaita`, `breeze`, `windows-9x`, `windows-xp`, `windows-7`, `cde` and `none`. For example, start in Breeze Dark with the following command.

```powershell
dotnet run --project Samples/Nazor.Samples.Gallery -- --theme breeze --scheme dark
```

Adwaita, Breeze and CDE follow the selected color scheme. The Windows 9x, Windows XP and Windows 7 themes use their light palettes. All six support the system's forced colors. [Desktop-inspired themes](../../Docs/Guide/16-Styling-and-Themes.md#desktop-inspired-themes)

## Inspect the Gallery with DevTools

The Debug build from source includes Nazor DevTools. In a desktop Gallery window, press **F12** to open its inspector and examine the component tree, computed styles, and frame timings. The portable Release archive ships with DevTools disabled. [Standalone DevTools](../../Docs/DevTools.md#start-and-connect)

## Package the portable release

From the repository root, publish to a fresh output directory with no runtime identifier. The archive keeps the Windows assets and both Linux desktop bridges in their runtime directories. The same application supports explicit DRM/KMS startup and requires the .NET 10 Runtime. [Portable deployment](../../Docs/Guide/28-Libraries-Packaging-and-Deployment.md) · [Native build prerequisites](../../Docs/Guide/33-Linux-Wayland.md#build-from-source)

```powershell
dotnet publish Samples/Nazor.Samples.Gallery -c Release --self-contained false -p:UseAppHost=false -o artifacts/gallery-release/publish
Compress-Archive -Path artifacts/gallery-release/publish/* -DestinationPath artifacts/gallery-release/Nazor.Gallery-portable.zip
```

Attach `Nazor.Gallery-portable.zip` to the GitHub release.

## What to explore

The ListView page demonstrates opt-in city reordering by primary drag or Ctrl/Meta+Shift+Up/Down. Its callback updates the model and preserves the selected item. TreeView uses automatic theme row metrics, and the showcase DataGrid follows application density without fixed-height overlap.

The **ScrollView** page shows both scrollbar axes in the same activity log. Move the pointer to a scrollbar to see its interactive appearance, then try the wheel, arrow buttons where present, and thumb dragging. Open **TitleBar and window controls** to see a separate window with Nazor-drawn decorations.

Every theme uses a tall application title bar with the app icon, title and component search, keeping the system caption buttons where supported and falling back to a Nazor frame otherwise. Theme changes preserve the search text and editor. NazorLeaf and Fluent share the same app mark and title spacing, with the search field aligned near the start of the caption. Fluent follows WinUI Gallery. The title bar and the navigation pane sit on Mica (`CommonBackdrop.Solid`). The content is a layer with a rounded leading corner, and the catalog cards and navigation list use the WinUI card, stroke and subtle-fill colors. Where a native frame cannot host the title bar, as on current Linux desktops, the same title bar uses a Nazor frame. Without Mica, the solid Fluent base replaces the material. Live examples have a separate source area: dark examples are recessed below the page and the source area is raised; light examples use the corresponding card fills. Forced colors use Canvas with system-color boundaries. Basic uses tonal system-color surfaces, card borders in each family's color and an accent selection bar. Each component family has a color and a packaged Fluent System Icons glyph, so the decoration looks the same on every platform. Forced colors replace these colors with system colors. These styles apply only to the sidebar, catalog and page frame. Live examples keep the theme's ordinary control appearance, without gallery overrides.

Open **NazorLeaf showcase** from the sidebar to browse the ten design sections in the same window. Basic, NazorLeaf and None retain the application search title bar with no window material. Decoration negotiation retains the supported fallback on other hosts. Dedicated window/chrome examples remain explicit demonstrations and do not change the main gallery's frame.

In the showcase menu section, tiles support keyboard activation, context menus and touch holds. Delete removes the chosen tile; its 4200ms notification restores it in its original order with Undo. The Edit menu shares that undo/redo state. Other menu commands and table/settings actions report their result through the showcase notification queue. Reading a message clears its unread indicator permanently for the current showcase session, including after another message is selected.

The showcase feedback section runs repeatable download and ring simulations. Restart cancels the previous run; changing the ring slider stops its simulation, and leaving the showcase disposes pending timers. Its notifications use a 4200ms lifetime and Undo. The destructive example composes a trash mark and a session-scoped don't-ask checkbox with AlertDialog; Cancel preserves the theme, Delete moves it to the example trash, and Undo or Restore makes it available again. The confirmation can be re-enabled from the example.

The catalog lives in `Catalog/`. The runnable examples are in `Examples/` and are embedded in the app so the displayed markup matches the compiled example. The design sections live in `Showcase/`.

Leaf menus, settings, messages, build rows and tiles share a scoped moving hover mark. Actual virtualized viewports own their marks so scrolling and item realization keep clipping and bounded rendering intact. Static Table is not automatically enrolled.

Leaf action examples include a pointer-origin 600ms click sheen; repeated presses restart the same bounded decorative effect, while keyboard/accessibility actions use the center. Reduced motion, forced colors and disabled/busy states suppress the sheen.

The NazorLeaf showcase keeps the selected section and header synchronized with scrolling and resizing. Its desktop rail uses one retained indicator with a 550ms travel/squash animation; the header compacts after 30 logical pixels of page scrolling. Reduced motion applies these changes immediately.

The Leaf showcase density selector changes the shared application setting (Automatic/Desktop/Touch), affecting field/button/menu/row sizing and body text throughout NazorLeaf. It replaces the sample-only 44px height override; automatic host preference detection is available only when a backend supplies the hint.

The **Search suggestions** page demonstrates typed city/country templates, local model filtering, distinct edit/choice/query events and a cancellable asynchronous provider with loading/error/empty content. String searches use `SearchField<string>` (`TItem="string"`); pointer choices are separate from explicit query submission. The city search uses a custom placeholder and no-results label. The typed example toggles match highlighting, replaces the default `Mark` with `Strong`, and reuses `RenderHighlightedText` for both city and country. Suggestion searches without an empty-state override use the localized no-results label in every theme.

The **Switch** example supports click/Space activation and captured thumb dragging. A drag previews travel and color, then commits on release; Escape or lost capture cancels without changing the bound setting. Reduced motion removes animated travel.

The **Segmented date and time pickers** page demonstrates culture-ordered date parts, formatted/hidden fields, 12/24-hour clocks, seconds and bounded minute grids. Changes remain drafts until Confirm; Cancel, Escape or outside dismissal retain the original value.

The **FileField / TokenField** page opens system pickers for one file, multiple files or a folder. Choose PDF, image-extension or all-file filters before opening a file picker. Cancel keeps the previous selection; hosts without a picker report that result in the example.

The **FlipView** page shows packaged images and custom interactive cards. Previous/Next, root-focused arrow keys and optional primary-pointer swipes stop at the ends; each example preserves editable data in its model while realizing only its selected template.

In the showcase's navigation section, the **Movimento** tab replays four motion lanes with the reference's distinct durations and curves. Repeated activation restarts all lanes; reduced motion shows their final positions immediately.

The **Colori** and **Forme** tabs read their displayed values from the rendered styles. Seed/scheme/custom-token changes update the labels; the radius slider scales all four radius families together while preserving the system frame.

Startup registers the Adwaita Sans and Mono faces, NazorLeaf's text and icon fonts, and the Fluent System Icons fonts. The Icon example shows typed symbols from the Fluent and NazorLeaf catalogs and a raw-code example with an explicit NSS family. The NazorLeaf showcase uses `LeafSymbol` from the package's original artwork in `Icons`. These icons remain available while switching themes. [Icon setup](../../Docs/Guide/22-Images-Icons-and-Drawing.md#typed-nazorleaf-symbols)

The **Content and layout transitions** page demonstrates keyed editable pages, collection add/remove/reorder and a connected destination captured before source removal. Local component state survives stable keys; outgoing content is unavailable immediately and is disposed after theme retention. Basic uses immediate replacement; Fluent supplies motion. Reduced motion shows final states directly.

The Dialog example includes Auto and explicit sizes plus a None/Primary/Secondary/Cancel default-action selector. Export chooses Primary explicitly; None removes the implicit Enter/accent action while body controls retain focus. Fluent Auto demonstrates content sizing and equal command widths.

The disclosure example includes an Up expander with `KeepMounted=true`; editing its local text, collapsing and reopening preserves the same child state. Fluent translates measured content inside a full-height clip, then releases the close phase; reduced motion finishes directly.

The Tabs example contrasts section navigation with keyed document sheets. Change width mode, edit retained panels, add/close pages and reorder by drag or Ctrl+Shift+arrows. Releasing outside the header reports the application-owned drag-out request without implicitly opening another window.

PipsPager demonstrates independent binding to FlipView and a vertical finite pager. Keyboard navigation, bounded realization and large-count behavior are exercised by the control tests.

Progress indicators include determinate Small/Medium/Large rings and interactive progress. The NazorLeaf showcase adds 84/40-unit leaf variants beside circles and the 18-unit button ring. Its percentage, gaps and vector completion mark follow displayed progress; accessibility keeps the actual target.

The existing hold-to-repeat Button example now demonstrates an explicit initial delay and shared pointer/Enter/Space repetition. It uses the normal Button appearance and command contract rather than a duplicate RepeatButton facade.

BreadcrumbBar demonstrates a resizable application-owned folder path, measured ancestor overflow, stable keys and typed navigation requests. The Reset action restores the complete sample path.

MenuButton/SplitButton, Toolbar, avatars/badges and messages include configurable existing-API states and working actions. The Leaf badge bars are explicitly labeled interactive illustrations; they do not certify native macOS/Linux launcher features or invoke native launcher services. Native platform support follows the public support guide.

NavigationView demonstrates adaptive/explicit pane modes, stable keyed hierarchy and application-owned route invocation while preserving a local editor. Resize or change mode to compare compact/root-menu behavior with the expanded tree.

The NazorLeaf showcase places its 4200ms notifications at bottom-center with 24px clearance. Every showcase notification helper supplies that duration; the feedback example retains its Undo action. Tooltip/toast motion uses separate opacity and geometry phases, and toast travel follows each notification's own height. Queued promotion and pointer/focus pause remain authoritative while dismissed visuals finish their bounded noninteractive exit.

The **ColorPicker** page exposes all six rectangular HSV axis combinations, alpha, horizontal/vertical presentation, section visibility and read-only/disabled states. It binds a real RGBA color and shows an independent previous swatch. Channel drafts commit on Enter/blur; hexadecimal uses explicit CSS #RRGGBBAA order in every theme. The ColorArea/ColorSlider page remains a primitive composition example.

NazorLeaf token cards use 12.5px names and 11.5px monospaced values on the measured 18.75px line grid. The [versioned typography evidence](../../Tests/Verification/Leaf-Token-Typography.json) records pinned HTML/font hashes and offline Chrome DOM baseline measurements; it is layout evidence, not a raster parity golden.

The avatar/badge page demonstrates typed Badge.Icon and decorative IndicatorContent with explicit accessible names. Leaf count changes roll in 350ms while the decorated control keeps its identity/focus; Basic/Fluent remain immediate. The Leaf showcase uses typed Mail/Chat/At/Pause/Dot/Dock artwork throughout its simulated bars. Icon font-family and square-indicator styling belong to the control/theme rather than sample classes. These in-app illustrations do not enable native launcher services.
