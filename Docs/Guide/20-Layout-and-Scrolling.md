# 20. Layout and scrolling

Nazor supports block layout, flex with wrapping, grid, and relative/absolute positioning. Dimensions are logical UI units. `onresize` reports the natural border/content boxes and parent-relative offsets before physical-pixel snapping. Sizes remain stable when animated margins or positions move unchanged content across pixel boundaries. Painting, hit testing and Scene.GetBounds retain their pixel-grid geometry. `box-sizing` defaults to `border-box`: declared size includes padding and border. A border needs a visible style as well as a width; `border: 1px #ccc` does not imply `solid`.

## Block and flex

Block layout stacks children vertically and stretches them horizontally. It does not collapse margins, apply flex growth, or use gaps. Auto cross-axis margins can center a child.

Flex controls main-axis direction, growth/shrinkage, basis, gaps, and alignment:

```css
.shell { display: flex; flex-direction: column; height: 100%; }
.toolbar { display: flex; align-items: center; flex-wrap: wrap; gap: 8px; }
.spacer { flex: 1; }
.content { flex: 1; min-height: 0; }
```

`flex: 1` expands to `1 1 0%`; `auto` is `1 1 auto`; `none` is `0 0 auto`. Auto margins absorb free space before alignment. A flex item's automatic minimum can be its content size. Set `min-height: 0` on a shrinking vertical content wrapper and `min-width: 0` where long horizontal content must shrink.

With wrap enabled, each line independently distributes main-axis space. `align-content` distributes the lines in the cross axis; `wrap-reverse` reverses line stacking. Column wrapping requires a defined height or max-height. `order` changes layout/paint order for flex/grid but does not change Tab or accessibility order. Prefer source order that already matches the intended reading order. Baseline alignment is unsupported.

## Grid

```css
.dashboard {
    display: grid;
    grid-template-columns: 240px minmax(0px, 1fr);
    grid-template-rows: auto 1fr auto;
    grid-template-areas: "header header" "navigation main" "footer footer";
    gap: 16px;
}
.header { grid-area: header; }
.navigation { grid-area: navigation; }
.main { grid-area: main; min-width: 0; min-height: 0; }
.footer { grid-area: footer; }
```

Tracks accept lengths, percentages, fr, auto, min/max-content, `minmax`, and `fit-content(length)`. `fr` shares space remaining after non-flexible tracks and gaps; its default minimum can still be content-based, so `minmax(0px, 1fr)` is useful for constrained content.

`repeat(N, tracks)` repeats a fixed count. `repeat(auto-fill, ...)` and `auto-fit` fill available space using fixed-size-compatible tracks; auto-fit collapses empty repetitions. Automatic repetition is allowed once per track list. Named lines, subgrid, and `grid`/`grid-template` shorthands are unsupported.

Place children with numbered lines (including negative lines from the end), spans, or named areas. Line zero is invalid. Areas must be rectangular and rows must have equal cell counts. `grid-auto-flow: row` or column fills unassigned cells; optional dense mode fills holes. Extra tracks use `grid-auto-rows`/columns. Alignment uses justify/align items/self and content properties.

## Intrinsic sizes and ratios

`min-content` measures the narrowest width respecting available breaks; `max-content` measures unwrapped content; `fit-content` clamps to available space between them. They are supported for width/min-width/max-width and flex-basis; height uses content height, while min/max-height do not accept these keywords.

`aspect-ratio` supplies an automatic dimension from the other dimension. If both dimensions are definite, the ratio does not override them. In a grid, a stretched width with `aspect-ratio: 1` can produce square tiles. Min/max constraints still apply.

Percentages need a definite reference dimension. Height percentages do not automatically create a height when every ancestor sizes from content. Give full-window layouts an explicit height/flex chain rather than relying on an indefinite percentage.

## Positioning

Relative positioning offsets an element after normal layout and leaves its original space occupied. Left wins over right, top over bottom. Absolute positioning removes the element from normal flow and gaps; its containing block is the nearest positioned ancestor (or a relevant transformed ancestor), otherwise the window.

```css
.card { position: relative; }
.badge { position: absolute; top: 8px; right: 8px; }
```

Opposite offsets with an automatic size stretch the element. Fixed dimensions and auto margins can center it between offsets. Without offsets it uses a static-position calculation; without a specified width it can shrink to fit. Absolute content still scrolls and clips with its physical ancestors. Fixed and sticky positioning are unsupported.

## Scrolling

The implicit `WindowContent` viewport defaults to `overflow: auto` on both axes, including without a theme. Scrollbars appear when layout content exceeds the available client area and disappear when it fits again. The title bar stays outside this viewport. PageUp/PageDown and Home/End scroll the window content even before any pointer or control-focus activity. Accessibility exposes this scroll viewport on the window node, preserving direct content navigation without an extra unnamed group. Children that shrink or wrap to fit do not cause overflow; use `flex-shrink: 0` or a minimum size when content must retain its dimensions.

Configure the window viewport in an application or window-local stylesheet. For example, `WindowContent { overflow-x: hidden; overflow-y: auto; }` allows only vertical scrolling. Set `overflow: hidden` to clip the content, or `overflow: visible` (also `initial`) to restore the previous non-scrolling behavior. Do not set overflow on `Window` to scroll the title bar.

For independent or nested regions, set overflow on the element that owns each scroll viewport. Use NSS on a `Container` for a simple scrolling region.

```razor
<Container Class="strip" TabIndex="0" AccessibleName="Results">
    <Container Class="strip-content">...</Container>
</Container>
```

```css
.strip { overflow-x: auto; overflow-y: hidden; }
.strip-content { display: flex; }
.strip-content > .tile { flex-shrink: 0; width: 240px; }
```

Auto and scroll both show an overlaid scrollbar only when content overflows. Drag either thumb with the primary mouse button to scroll, including outside the container while the pointer is captured. Clicking the track pages in that direction. A non-visible overflow value on either axis clips both axes at the inner border, including rounded corners. Scroll extent includes child boxes/margins and non-clipping descendants; painted text overflow does not itself enlarge it.

Scrollbar properties are inherited: place defaults on `:root` in a global stylesheet, then override them on a container or in a component's `.nazor.nss` stylesheet using the normal NSS cascade and component scope.

```css
:root {
    scrollbar-visibility: autohide;
    scrollbar-width: 8px;
    scrollbar-thumb-color: #808080b0;
    scrollbar-track-color: transparent;
    scrollbar-radius: 4px;
    scrollbar-inset: 2px;
    scrollbar-min-thumb-length: 24px;
}

.results { scrollbar-visibility: visible; scrollbar-thumb-color: #0078d4; }
.preview { scrollbar-visibility: none; }
```

`visible` (the default) shows the overflowing axes. `none` immediately hides their scrollbar without disabling scrolling. `autohide` reveals each axis near the pointer, while dragging, or during wheel scrolling. The default proximity is 12 logical pixels inside the viewport edge, configurable through `scrollbar-reveal-distance` and at least width plus inset. Scrollbars overlay content by default. Width, inset, radius and minimum thumb length accept nonnegative lengths; thumbs are clamped to the available track. A zero width suppresses scrollbar painting and pointer interaction.

### Placement, track and end buttons

`scrollbar-placement: overlay` draws over the content. `reserve` allocates a gutter beside each overflowing axis, reducing the content viewport without increasing the container's authored outer size. Each gutter is `scrollbar-width + 2 * scrollbar-inset`. Content reflows, percentage widths and absolute positioning use the reduced space, and content is clipped before the gutter. One gutter can cause overflow on the other axis; the layout resolves both. Gutters disappear when content fits or scrollbar visibility is `none`.

**Autohide always uses overlay placement**, even when a global or component rule specifies `reserve`. This avoids layout shifts when the scrollbar appears or fades. Explicit `visible` visibility is required for reserved placement.

Track color, radius and border are independent of the thumb. `scrollbar-track-radius: auto` uses `scrollbar-radius`. End buttons are optional (`scrollbar-buttons: none` by default). Visible buttons scroll in the corresponding direction, clamp at the endpoints, and use their disabled arrow color when they cannot scroll further. Buttons capture the pointer and repeat while held over the pressed button; moving away pauses repeat, and releasing or canceling stops it. A zero repeat interval disables repetition. For very short bars, each button is clamped to one third of the available scrollbar length so the thumb still has room.

```css
.results {
    scrollbar-visibility: visible;
    scrollbar-placement: reserve;
    scrollbar-width: 14px;
    scrollbar-track-color: #f0f0f0;
    scrollbar-track-radius: 4px;
    scrollbar-track-border-width: 1px;
    scrollbar-track-border-color: #c0c0c0;
    scrollbar-buttons: visible;
    scrollbar-button-length: 16px;
    scrollbar-button-radius: 3px;
    scrollbar-button-color: #333333;
    scrollbar-button-background: #eeeeee;
    scrollbar-button-hover-background: #dddddd;
    scrollbar-button-active-background: #cccccc;
    scrollbar-button-disabled-color: #999999;
    scrollbar-button-step: 40px;
    scrollbar-button-repeat-delay: 400ms;
    scrollbar-button-repeat-interval: 50ms;
}
```

These properties inherit, so the same rules can be global defaults on `:root` or component-specific overrides. Track and button colors, radii, border width and button length support the same NSS transitions and keyframes as the thumb.

### Autohide and wheel animations

Autohide fades in over 120ms and fades out over 200ms after a 300ms hide delay, using `ease-out`. Reversals continue from the current opacity. Configure these globally or per component; zero durations and zero hide delay disable the fade. Reduced-motion preferences skip fades and their delay automatically.

```css
:root {
    scrollbar-fade-in-duration: 120ms;
    scrollbar-fade-out-duration: 200ms;
    scrollbar-fade-out-delay: 300ms;
    scrollbar-fade-timing-function: ease-out;
    scrollbar-wheel-timeout: 600ms;
    scrollbar-reveal-distance: 12px;
}

/* Suppress the default wheel reveal, even when the pointer is near the bar. */
.preview:wheel-scrolling { scrollbar-visibility: none; }
```

`:wheel-scrolling` matches only the container receiving wheel input and remains active until `scrollbar-wheel-timeout` after its last event. Repeated wheel input restarts the timeout. Wheel events at a scroll limit also activate the state; nested scrolling marks the ancestor that actually scrolls. Prevented and zero-delta wheel events do not activate it. A `none` visibility stays hidden unless an NSS rule overrides it. Programmatic, keyboard and thumb scrolling do not activate this wheel-specific state.

Width, colors, radius, inset, minimum thumb length and `scrollbar-opacity` support normal `transition` and `@keyframes` rules. These animate the scrollbar independently of its container. The effective `scrollbar-opacity` multiplies the autohide fade; visibility `none` suppresses painting and interaction immediately.

```css
.results { transition: scrollbar-width 180ms ease-out; }
.results:wheel-scrolling {
    scrollbar-width: 8px;
    animation: wheel-scrollbar 250ms ease-out both;
}
@keyframes wheel-scrollbar {
    from { scrollbar-opacity: 0.3; scrollbar-thumb-color: #808080b0; }
    to { scrollbar-opacity: 1; scrollbar-thumb-color: #0078d4; }
}
```

The vertical wheel scrolls the nearest capable ancestor; horizontal wheel or Shift+wheel scrolls horizontally. PageUp/PageDown/Home/End scroll vertically, or horizontally for a horizontal-only surface. Focus can bring a child into view on both axes.

`@onscroll` supplies offsets and viewport/content dimensions. `Element.ScrollTo(left, top)` changes offsets programmatically. A scrolling container does not virtualize children; use `ListView` or `TreeView` for large collections.

Scrolling is also an accessibility capability derived from computed overflow. An ordinary `Container` with `overflow-x` or `overflow-y` set to `auto` or `scroll` exposes its scroll position, viewport/content extent and scroll actions automatically, even without an accessible name. Hidden scrollbars do not disable this capability; fitting content reports non-scrollable axes until it grows. Named containers retain their group identity when overflow is changed. Controls with an existing role, such as lists, keep it while gaining the scroll capability. Windows exposes UI Automation ScrollPattern, and Linux exposes AT-SPI scroll/page actions. Set `AccessibleName` for meaningful regions and `TabIndex="0"` when the container itself should be a keyboard tab stop; its descendants may also receive focus.

### ScrollView

Use `ScrollView` for a named region with directional scrolling and scroll state. `Orientation` defaults to `Vertical` and also accepts `Horizontal` and `Both`. The Basic theme sets the overflow axes. When you supply all control styles, configure `overflow-x` and `overflow-y` to match the intended orientation.

```razor
<ScrollView Class="log" AccessibleName="Activity log" IsFocusable="true">
    <Container Class="entries">
        @foreach (var entry in Enumerable.Range(1, 20))
        {
            <TextBlock>Activity @entry</TextBlock>
        }
    </Container>
</ScrollView>

@style {
    .log { height: 160px; min-height: 0; }
    .entries { display: flex; flex-direction: column; gap: 8px; }
}
```

The constrained height makes the list scroll while surrounding content stays fixed. `IsFocusable` puts the view in the Tab order so the user can scroll with Page Up, Page Down, Home and End even without focusable rows. In a flex layout, constrain the parent and allow the view to shrink with `min-height: 0` or `min-width: 0` as appropriate.

`OnScroll` reports changes to the current view's offset, viewport or content size. Nested scroll views report their own changes independently. With an `@ref`, use `ScrollToAsync(left, top)` for an offset, `ScrollToStartAsync()` or `ScrollToEndAsync()` for an edge, and `ScrollIntoView(target)` for a child element reference. Use `can-scroll-start` and `can-scroll-end` attributes in NSS to indicate hidden content. `ScrollView` renders its children normally. For a large collection, choose a virtualizing control from the catalog. [ScrollView API](../ApiReferences/Nazor.Controls.md#scrollview) · [Control catalog](09-Control-Catalog.md)

## Smooth scrolling

All scroll containers support animated scrolling through NSS, globally or within a component:

```css
:root {
    scroll-behavior: smooth;
    scroll-duration: 180ms;
    scroll-timing-function: ease-out;
    scroll-wheel-step: 24px;
}
.results { overflow: auto; }
.reader { scroll-wheel-step: 3 lines; }
.immediate { scroll-behavior: auto; }
```

These four properties inherit. The defaults are immediate scrolling (`auto`), `180ms`, `ease-out`, and the system wheel distance (`auto`). Length steps may use px, relative lengths or calculations; a line step uses the container's computed `line-height` (font metrics for `normal`). Fractional pixels, detents and line counts are supported. A line count defines how far one detent moves, without aligning the final position to text rows. Zero disables detent movement on that container; precise pixel input remains available.

Smooth scrolling applies to wheel detents, scroll keys, scrollbar track/end-button requests and `Element.ScrollTo`. Repeated requests accumulate towards the pending destination; reversing direction starts from the visible position. Scroll positions remain fractional throughout. A child at its pending limit lets further wheel input reach its scrollable parent.

Precise pixel input and thumb dragging follow the input directly, cancel pending animation and preserve arbitrary offsets. `Scene.StopScrolling(container)` also stops at the current interpolated position. Focus-driven bring-into-view stays immediate. Reduced motion, `scroll-duration: 0ms` and `scroll-behavior: auto` apply the destination immediately. `@onscroll` continues to report actual visible offsets during animation.

## Responsive composition

Use media queries on client dimensions, flex wrapping, and grid track changes. Avoid absolute positions for ordinary forms and toolbars. Verify at narrow widths and several DPI scales. Borders align to physical pixels, while layout remains in logical coordinates.

Examples: [Theming](../../Samples/Nazor.Samples.Theming/README.md), [Tasks scrollbar playground](../../Samples/Nazor.Samples.Tasks/README.md#verify-scrollbar-behavior-in-the-playground), [Tasks view styles](../../Samples/Nazor.Samples.Tasks/Views/TasksView.nazor.nss). API: [Nazor.Layout](../ApiReferences/Nazor.Layout.md).

---

[Guide index](README.md) · [API reference](../API.md)
