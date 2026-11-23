# 19. NSS property reference

This catalog covers all **242 longhand properties** and **38 shorthands** registered by the current style engine. The Initial column gives the property default before themes and component rules apply. The Basic theme overrides some of these values.

A grammar uses `|` for alternatives, `||` for unordered alternatives, `?` for optional content, `+` for one or more, `#` for comma-separated repetition, and `{m,n}` for bounded repetition. Angle-bracket names denote value types. Inherited and animatable columns describe the longhand descriptor; compatible endpoints are still required for interpolation.

See [syntax and values](18-NSS-Syntax-and-Values.md), [layout](20-Layout-and-Scrolling.md), [drawing](22-Images-Icons-and-Drawing.md), and [motion](23-Transitions-and-Animations.md) for behavior beyond the grammar.

## Scope and applicability

Selector scope determines which elements a rule can match. Global sheets have no component-owner boundary. A component's inline `@style` and `.nazor.nss` sidecar are scoped to the component by default. An element's `Style` value applies declarations directly to that element. See [stylesheet sources](16-Styling-and-Themes.md) and [component boundaries](17-Selectors-and-Media-Queries.md#component-boundaries) for the cascade and how to select across component boundaries.

Applicability determines where a matched declaration can affect behavior. The tables describe physical elements and control parts. Inheritance passes an inherited value to descendants through the element tree. For example, set `scrollbar-width` on a window root to supply an inherited value, and set `overflow-y: auto` on the element that should scroll. Platform-specific declarations still require the named backend and native capability. An applicability warning (`NZS112`) checks selectors whose subjects can be identified statically. Class selectors and custom components can resolve to different physical elements at runtime.

| Property group | Target and conditions |
| --- | --- |
| [Box and layout](#box-and-layout) | Element boxes. `flex-direction` and `flex-wrap` require a flex container. `flex-grow`, `flex-shrink`, and `flex-basis` affect flex items. `align-items`, `align-content`, and `justify-content` affect flex or grid containers. `align-self` affects flex or grid items. `order` reorders flex and grid items. `row-gap` and `column-gap` require flex or grid. `top`, `right`, `bottom`, and `left` require non-static positioning. |
| [Grid](#grid) | `grid-template-*`, `grid-auto-*`, and `justify-items` target a grid container. The grid line properties and `justify-self` target its grid items. |
| [Scrolling](#scrolling) and [scrollbars](#scrollbars) | The element that scrolls. Inherited declarations can supply defaults from ancestors. Scrollbar drawing requires a scrollable axis and overflowing content. Fade and reveal options apply to `autohide`, and button settings apply when scrollbar buttons are visible. |
| [Decoration and interaction](#decoration-and-interaction) | Element boxes, with additional conditions for outlines, images, glyphs, and text selection described below. |
| [Typography](#typography) | Elements that lay out or draw text. Inherited properties can be set on their ancestors. |
| [Motion](#motion) | Elements whose supported property values change. Keyframe animations require a matching keyframe name. |
| [Windows native properties](#windows-native-properties) and [launcher badges](#windows-launcher-badges) | Native window root (`:root`), subject to the Win32 conditions in each section. |
| [Window frame and resize](#window-frame-and-resize) | Native window root (`:root`) on supported desktop backends. |

## Generated content

| Property | Accepted syntax | Initial | Inherited | Animatable |
| --- | --- | --- | --- | --- |
| `content` | `normal \| none \| <string>+` | `normal` | No | No |

Applies only to `::before` and `::after`. Adjacent quoted strings concatenate, including `""` for a decorative box; `normal` and `none` suppress it. It does not replace ordinary element content. See [pseudo-elements](17-Selectors-and-Media-Queries.md#pseudo-elements) for layout, editable-text styling and accessibility limits. `::selection` accepts only `color` and `background-color`; `::placeholder` accepts font/text properties and opacity.

## Box and layout

| Property | Accepted syntax | Initial | Inherited | Animatable |
| --- | --- | --- | --- | --- |
| `display` | `block \| inline \| flex \| grid \| none` | `block` | No | No |
| `flex-direction` | `row \| row-reverse \| column \| column-reverse` | `row` | No | No |
| `flex-wrap` | `nowrap \| wrap \| wrap-reverse` | `nowrap` | No | No |
| `justify-content` | `normal \| stretch \| flex-start \| start \| flex-end \| end \| center \| space-between \| space-around \| space-evenly` | `normal` | No | No |
| `align-content` | `normal \| stretch \| flex-start \| start \| flex-end \| end \| center \| space-between \| space-around \| space-evenly` | `normal` | No | No |
| `align-items` | `normal \| stretch \| flex-start \| start \| flex-end \| end \| center` | `stretch` | No | No |
| `align-self` | `auto \| normal \| stretch \| flex-start \| start \| flex-end \| end \| center` | `auto` | No | No |
| `flex-grow` | `<number>` | `0` | No | Yes |
| `flex-shrink` | `<number>` | `1` | No | Yes |
| `flex-basis` | `auto \| content \| <length-percentage> \| min-content \| max-content \| fit-content` | `auto` | No | Yes |
| `order` | `<integer>` | `0` | No | No |
| `row-gap` | `normal \| <length-percentage>` | `0px` | No | Yes |
| `column-gap` | `normal \| <length-percentage>` | `0px` | No | Yes |
| `width` | `auto \| <length-percentage> \| min-content \| max-content \| fit-content` | `auto` | No | Yes |
| `height` | `auto \| <length-percentage> \| min-content \| max-content \| fit-content` | `auto` | No | Yes |
| `min-width` | `auto \| <length-percentage> \| min-content \| max-content \| fit-content` | `auto` | No | Yes |
| `min-height` | `auto \| <length-percentage>` | `auto` | No | Yes |
| `max-width` | `none \| <length-percentage> \| min-content \| max-content \| fit-content` | `none` | No | Yes |
| `max-height` | `none \| <length-percentage>` | `none` | No | Yes |
| `padding-top` | `<length-percentage>` | `0px` | No | Yes |
| `padding-right` | `<length-percentage>` | `0px` | No | Yes |
| `padding-bottom` | `<length-percentage>` | `0px` | No | Yes |
| `padding-left` | `<length-percentage>` | `0px` | No | Yes |
| `margin-top` | `auto \| <length-percentage>` | `0px` | No | Yes |
| `margin-right` | `auto \| <length-percentage>` | `0px` | No | Yes |
| `margin-bottom` | `auto \| <length-percentage>` | `0px` | No | Yes |
| `margin-left` | `auto \| <length-percentage>` | `0px` | No | Yes |
| `border-top-width` | `thin \| medium \| thick \| <length>` | `0px` | No | Yes |
| `border-right-width` | `thin \| medium \| thick \| <length>` | `0px` | No | Yes |
| `border-bottom-width` | `thin \| medium \| thick \| <length>` | `0px` | No | Yes |
| `border-left-width` | `thin \| medium \| thick \| <length>` | `0px` | No | Yes |
| `box-sizing` | `border-box \| content-box` | `border-box` | No | No |
| `overflow-x` | `visible \| hidden \| scroll \| auto` | `visible` | No | No |
| `aspect-ratio` | `auto \| <ratio>` | `auto` | No | No |
| `overflow-y` | `visible \| hidden \| scroll \| auto` | `visible` | No | No |
| `position` | `static \| relative \| absolute` | `static` | No | No |
| `top` | `auto \| <length-percentage>` | `auto` | No | Yes |
| `right` | `auto \| <length-percentage>` | `auto` | No | Yes |
| `bottom` | `auto \| <length-percentage>` | `auto` | No | Yes |
| `left` | `auto \| <length-percentage>` | `auto` | No | Yes |
| `z-index` | `auto \| <integer>` | `auto` | No | No |

## Grid

| Property | Accepted syntax | Initial | Inherited | Animatable |
| --- | --- | --- | --- | --- |
| `grid-template-columns` | `none \| <track-list>` | `none` | No | No |
| `grid-template-rows` | `none \| <track-list>` | `none` | No | No |
| `grid-template-areas` | `none \| <string>+` | `none` | No | No |
| `grid-auto-columns` | `<track-size>+` | `auto` | No | No |
| `grid-auto-rows` | `<track-size>+` | `auto` | No | No |
| `grid-auto-flow` | `[row \| column] \|\| dense` | `row` | No | No |
| `grid-row-start` | `auto \| <integer> \| span <integer> \| <custom-ident>` | `auto` | No | No |
| `grid-row-end` | `auto \| <integer> \| span <integer> \| <custom-ident>` | `auto` | No | No |
| `grid-column-start` | `auto \| <integer> \| span <integer> \| <custom-ident>` | `auto` | No | No |
| `grid-column-end` | `auto \| <integer> \| span <integer> \| <custom-ident>` | `auto` | No | No |
| `justify-items` | `normal \| stretch \| flex-start \| start \| flex-end \| end \| center` | `stretch` | No | No |
| `justify-self` | `auto \| normal \| stretch \| flex-start \| start \| flex-end \| end \| center` | `auto` | No | No |

## Scrolling

| Property | Accepted syntax | Initial | Inherited | Animatable |
| --- | --- | --- | --- | --- |
| `scroll-behavior` | `auto \| smooth` | `auto` | Yes | No |
| `scroll-duration` | `<time>` | `180ms` | Yes | No |
| `scroll-timing-function` | `linear \| ease \| ease-in \| ease-out \| ease-in-out \| <easing-function>` | `ease-out` | Yes | No |
| `scroll-wheel-step` | `auto \| <length> \| <number> lines` | `auto` | Yes | No |

Wheel distances accept fractional values, such as `24.5px` or `1.5 lines` (`1.5lines` is also accepted). A line is the scroll container's computed line height; distances never snap offsets to rows or a grid. `auto` follows the backend's system distance/page setting; a configured distance overrides it. Zero consumes wheel detents without moving. Precise pixel input and thumb dragging stay direct and do not use the wheel step. Reduced motion bypasses smooth interpolation. See [scrolling behavior](20-Layout-and-Scrolling.md#smooth-scrolling).

## Scrollbars

All scrollbar properties inherit and apply to both overflowing axes. See [scrolling](20-Layout-and-Scrolling.md#scrolling) for proximity, dragging and global/component examples.

| Property | Accepted syntax | Initial | Inherited | Animatable |
| --- | --- | --- | --- | --- |
| `scrollbar-visibility` | `none \| visible \| autohide` | `visible` | Yes | No |
| `scrollbar-width` | `<length>` | `4px` | Yes | Yes |
| `scrollbar-thumb-color` | `<color>` | `#808080b0` | Yes | Yes |
| `scrollbar-thumb-width` | `auto \| <length>` | `auto` (uses scrollbar width) | Yes | Yes |
| `scrollbar-thumb-expanded-width` | `auto \| <length>` | `auto` (uses resting thumb width) | Yes | Yes |
| `scrollbar-thumb-hover-color` | `auto \| <color>` | `auto` (uses thumb color) | Yes | No |
| `scrollbar-thumb-active-color` | `auto \| <color>` | `auto` (uses thumb color) | Yes | No |
| `scrollbar-thumb-border-width` | `<length>` | `0px` | Yes | Yes |
| `scrollbar-thumb-border-color` | `<color>` | `currentColor` | Yes | Yes |
| `scrollbar-thumb-border-highlight-color` | `auto \| <color>` | `auto` (uses thumb border color) | Yes | No |
| `scrollbar-track-color` | `<color>` | `transparent` | Yes | Yes |
| `scrollbar-track-hover-color` | `auto \| <color>` | `auto` (uses track color) | Yes | No |
| `scrollbar-radius` | `<length>` | `2px` | Yes | Yes |
| `scrollbar-inset` | `<length>` | `2px` | Yes | Yes |
| `scrollbar-min-thumb-length` | `<length>` | `24px` | Yes | Yes |
| `scrollbar-opacity` | `<number>` | `1` | Yes | Yes |
| `scrollbar-fade-in-duration` | `<time>` | `120ms` | Yes | No |
| `scrollbar-fade-out-duration` | `<time>` | `200ms` | Yes | No |
| `scrollbar-fade-out-delay` | `<time>` | `300ms` | Yes | No |
| `scrollbar-fade-timing-function` | `linear \| ease \| ease-in \| ease-out \| ease-in-out \| <easing-function>` | `ease-out` | Yes | No |
| `scrollbar-wheel-timeout` | `<time>` | `600ms` | Yes | No |
| `scrollbar-reveal-distance` | `<length>` | `12px` | Yes | No |
| `scrollbar-placement` | `overlay \| reserve` | `overlay` | Yes | No |
| `scrollbar-track-radius` | `auto \| <length>` | `auto` (uses `scrollbar-radius`) | Yes | Yes |
| `scrollbar-track-border-width` | `<length>` | `0px` | Yes | Yes |
| `scrollbar-track-border-color` | `<color>` | `currentColor` | Yes | Yes |
| `scrollbar-buttons` | `none \| visible` | `none` | Yes | No |
| `scrollbar-button-length` | `<length>` | `16px` | Yes | Yes |
| `scrollbar-button-color` | `<color>` | `currentColor` | Yes | Yes |
| `scrollbar-button-background` | `<color>` | `transparent` | Yes | Yes |
| `scrollbar-button-hover-background` | `<color>` | `#80808040` | Yes | Yes |
| `scrollbar-button-active-background` | `<color>` | `#80808080` | Yes | Yes |
| `scrollbar-button-disabled-color` | `<color>` | `#80808080` | Yes | Yes |
| `scrollbar-button-radius` | `<length>` | `2px` | Yes | Yes |
| `scrollbar-button-step` | `<length>` | `40px` | Yes | No |
| `scrollbar-button-repeat-delay` | `<time>` | `400ms` | Yes | No |
| `scrollbar-button-repeat-interval` | `<time>` | `50ms` | Yes | No |

## Decoration and interaction

| Property | Accepted syntax | Initial | Inherited | Animatable |
| --- | --- | --- | --- | --- |
| `background-image` | `none \| [<url> \| <gradient>]#` | `none` | No | No |
| `background-clip` | `border-box \| padding-box` | `border-box` | No | No |
| `border-image-source` | `none \| <gradient>` | `none` | No | No |
| `outline-inner-width` | `<length>` | `0px` | No | Yes |
| `outline-inner-color` | `<color>` | `transparent` | No | Yes |
| `stroke-width` | `<length-percentage>` | `0px` | No | Yes |
| `stroke-radius` | `<length-percentage>` | `0px` | No | Yes |
| `ring-shape` | `circle \| border-box` | `circle` | No | No |
| `number-motion` | `none \| linear \| linear-increase \| exponential` | `none` | No | No |
| `number-motion-duration` | `<time>` | `0ms` | No | No |
| `number-motion-decay` | `<number>` | `0` | No | No |
| `number-motion-tolerance` | `<number>` | `0` | No | No |
| `number-boundary-tolerance` | `<number>` | `0` | No | No |
| `stroke-trim-start` | `<number>` | `0` | No | Yes |
| `stroke-trim-end` | `<number>` | `1` | No | Yes |
| `background-position` | `[[left \| center \| right \| top \| bottom \| <length-percentage>]{1,2}]#` | `0% 0%` | No | No |
| `background-size` | `[cover \| contain \| [auto \| <length-percentage>]{1,2}]#` | `auto auto` | No | No |
| `background-repeat` | `[repeat \| no-repeat \| repeat-x \| repeat-y]#` | `repeat` | No | No |
| `background-color` | `<color>` | `transparent` | No | Yes |
| `border-top-color` | `<color>` | `currentColor` | No | Yes |
| `border-right-color` | `<color>` | `currentColor` | No | Yes |
| `border-bottom-color` | `<color>` | `currentColor` | No | Yes |
| `border-left-color` | `<color>` | `currentColor` | No | Yes |
| `border-top-style` | `none \| solid \| dashed \| dotted` | `none` | No | No |
| `border-right-style` | `none \| solid \| dashed \| dotted` | `none` | No | No |
| `border-bottom-style` | `none \| solid \| dashed \| dotted` | `none` | No | No |
| `border-left-style` | `none \| solid \| dashed \| dotted` | `none` | No | No |
| `border-top-left-radius` | `<length-percentage>{1,2}` | `0px` | No | Yes |
| `border-top-right-radius` | `<length-percentage>{1,2}` | `0px` | No | Yes |
| `border-bottom-right-radius` | `<length-percentage>{1,2}` | `0px` | No | Yes |
| `border-bottom-left-radius` | `<length-percentage>{1,2}` | `0px` | No | Yes |
| `outline-width` | `thin \| medium \| thick \| <length>` | `3px` | No | Yes |
| `outline-style` | `none \| solid \| dashed \| dotted` | `none` | No | No |
| `outline-color` | `<color>` | `currentColor` | No | Yes |
| `outline-offset` | `<length>` | `0px` | No | Yes |
| `box-shadow` | `none \| [inset? && <length>{2,4} && <color>?]#` | `none` | No | Yes |
| `mix-blend-mode` | `normal \| multiply \| screen \| overlay \| darken \| lighten \| color-dodge \| color-burn \| hard-light \| soft-light \| difference \| exclusion \| hue \| saturation \| color \| luminosity` | `normal` | No | No |
| `isolation` | `auto \| isolate` | `auto` | No | No |
| `background-blend-mode` | `[normal \| multiply \| screen \| overlay \| darken \| lighten \| color-dodge \| color-burn \| hard-light \| soft-light \| difference \| exclusion \| hue \| saturation \| color \| luminosity]#` | `normal` | No | No |
| `filter` | `none \| <filter-function-list>` | `none` | No | No |
| `backdrop-filter` | `none \| <filter-function-list>` | `none` | No | No |
| `opacity` | `<number>` | `1` | No | Yes |
| `object-fit` | `fill \| contain \| cover \| none \| scale-down` | `fill` | No | No |
| `object-position` | `[left \| center \| right \| top \| bottom \| <length-percentage>]{1,2}` | `50% 50%` | No | Yes |
| `transform` | `none \| <transform-function>+` | `none` | No | Yes |
| `transform-origin` | `[left \| center \| right \| top \| bottom \| <length-percentage>]{1,2}` | `50% 50%` | No | Yes |
| `clip-path` | `none \| inset(<length-percentage>{1,4} [round <border-radius>]?)` | `none` | No | Yes |
| `visibility` | `visible \| hidden` | `visible` | Yes | No |
| `pointer-events` | `auto \| none` | `auto` | Yes | No |
| `user-select` | `auto \| none \| text \| all` | `auto` | Yes | No |
| `cursor` | `default \| pointer \| text \| col-resize \| ew-resize \| row-resize \| ns-resize \| move \| not-allowed \| wait \| progress \| help \| crosshair \| nwse-resize \| nesw-resize` | `default` | Yes | No |

`object-fit` and `object-position` apply to elements that display a loaded image, such as `Image` or `Icon` with a `Source`. A font icon uses the typography properties. `background-position`, `background-size`, and `background-repeat` configure background image layers. The border image needs a painted border. `outline-inner-width` and `outline-inner-color` add the inner ring when the primary outline is visible. `user-select` affects text on controls or parts that enable selection. The backend supplies the available cursor shapes.

### Rectangular clipping

`clip-path: inset(...)` clips the element's background, border, shadows, outline, text and ordinary descendants without changing layout or scrolling. Insets use the border box. Top and bottom percentages resolve against its height, and left and right percentages resolve against its width. One to four space-separated values expand like padding. Relative units, `calc()`, `min()`, `max()`, and `clamp()` are supported. Negative insets expand the visible area. An empty resulting rectangle draws nothing and accepts no pointer hits. Hit testing uses the same animated clip after inverse transforms. Separately hosted overlays have their own clipping boundary.

Add `round` followed by the border-radius shorthand for rounded corners, for example `clip-path: inset(10px 20px round 16px 4px 16px 4px / 12px 3px 12px 3px)`. One to four nonnegative radii expand clockwise like border-radius; `/` separates horizontal and vertical axes. Radius percentages resolve against the original border box, then all radii scale down proportionally if adjacent corners would overlap in the inset rectangle. A corner with either axis zero is square.

Inset lengths and corner radii interpolate in transitions and keyframes, including mixed pixel/percentage values. Interrupted transitions retarget from the current visible shape. `none` disables clipping and changes discretely; `inset(0)` still clips overflowing ink to the border box. Circle, ellipse, polygon, path, URL masks and geometry-box suffixes are unsupported and fail compilation. Existing rounded element borders still apply to their own paint. The same shared scene and rounded clip command work on every backend.

```css
@keyframes reveal {
    from { clip-path: inset(0 0 100%); }
    to { clip-path: inset(0); }
}

Panel { animation: reveal 250ms cubic-bezier(0, 0, 0, 1); }
```

The example reveals the box downward from its fixed top edge, keeps text at its final size, and returns to the unclipped base style on completion. Reduced motion uses the base style immediately. Fluent applies this pattern to `SelectPopup` and `ComboBoxPopup`. The popup styles use resolved placement states to choose their reveal direction.

`<number>` properties accept finite unitless `calc()`, `min()`, `max()` and `clamp()` expressions, including `var()` expansion. Number calculations reject lengths, percentages, division by zero and results outside that property's range. For example, `stroke-trim-start: min(1, calc(var(--progress) + 0.07))` trims a normalized gap without a control-specific property.

### Vector glyph strokes

`stroke-width`, `stroke-trim-start` and `stroke-trim-end` apply to the built-in check, chevron, minus and ring vector glyphs on every backend. They affect painting and animation without changing layout or hit targets. They do not change font icons, text glyphs, image content or ordinary element borders.

For checks and chevrons, trim fractions refer to the total polyline length, including both segments. The renderer keeps any intervening corner and interpolates the two cut endpoints along their segments. The default interval `0` to `1` draws the complete glyph. An end at or below the start draws nothing. Caps and joins are round. Zero `stroke-width` selects automatic thickness. A percentage resolves against the smaller dimension of the glyph's content box. Minus glyphs, such as the mixed-state check box mark and the clear buttons of fields, trim along their single segment. `stroke-radius` applies to rings, and ring trimming is relative to the control's progress fraction.

`ring-shape: circle` keeps the existing content-box circle. `border-box` strokes a rounded border rectangle using `border-radius`, beginning on the top edge nearest its center and running clockwise by cumulative path length. Adjacent radii scale proportionally when they would overlap. Zero `stroke-radius` insets the centerline by half the stroke width; an explicit radius uses `min(width,height)/2 - radius` as a nonnegative inset. For 48-unit leaf geometry, `stroke-radius: 41.666667%` and `border-radius: 37.5% 8.333333% 37.5% 8.333333%` produce the reference path from (24,4) clockwise. Ordinary circle glyphs in Dial and field controls keep their previous geometry.

To replace a built-in glyph with a font icon, a theme can set `stroke-trim-end: 0` on the part, which draws nothing, and draw the icon with `::before`. NazorLeaf does this for the clear, reveal and search parts of its fields.

For example, a theme can reveal a checkbox stroke on selection:

```css
CheckBox > CheckBoxMark {
    stroke-width: 1.2px;
    stroke-trim-end: 0;
    transition: stroke-trim-end 200ms ease-out;
}

CheckBox:checked > CheckBoxMark { stroke-trim-end: 1; }
```

The standard checkbox removes its check glyph when unchecked, so unchecking hides the stroke immediately. Reduced motion applies the final state without drawing intermediate frames. A stroke can extend beyond the element's box; culling and display-list damage include its width, while explicit overflow clipping still applies.

Circular borders with a uniform solid color and thickness use an antialiased closed ring path in the raster renderer. With `background-clip: padding-box`, an adjacent circular fill and border of the same color share one coverage mask to avoid a translucent seam. The renderer selects this path from the geometry. Gradient, elliptical, asymmetric, dashed, and dotted borders use separate paths, as do borders that consume the inner circle.

## Typography

| Property | Accepted syntax | Initial | Inherited | Animatable |
| --- | --- | --- | --- | --- |
| `color` | `<color>` | `#000000` | Yes | Yes |
| `font-family` | `<family-name>#` | `"Segoe UI"` | Yes | No |
| `font-symbol` | `none \| [<string> <string>]#` | `none` | No | No |
| `font-size` | `<length-percentage>` | `14px` | Yes | No |
| `font-weight` | `normal \| bold \| <number>` | `400` | Yes | No |
| `font-style` | `normal \| italic` | `normal` | Yes | No |
| `font-optical-sizing` | `auto \| none \| native` | `auto` | Yes | No |
| `font-feature-settings` | `normal \| [<string> [on \| off \| <integer>]?]#` | `normal` | Yes | No |
| `font-synthesis` | `none \| [weight \|\| style]` | `none` | Yes | No |
| `font-variation-settings` | `normal \| [<string> <number>]#` | `normal` | Yes | No |
| `line-height` | `normal \| <number> \| <length-percentage>` | `normal` | Yes | No |
| `text-align` | `start \| end \| left \| right \| center` | `start` | Yes | No |
| `overflow-wrap` | `normal \| break-word \| anywhere` | `normal` | Yes | No |
| `text-transform` | `none \| uppercase \| lowercase \| capitalize` | `none` | Yes | No |
| `letter-spacing` | `normal \| <length>` | `0px` | Yes | No |
| `text-decoration-line` | `none \| [underline \|\| overline \|\| line-through]` | `none` | Yes | No |
| `text-decoration-style` | `solid \| double \| dotted \| dashed \| wavy` | `solid` | Yes | No |
| `text-decoration-color` | `<color>` | `currentColor` | Yes | Yes |
| `text-decoration-thickness` | `auto \| <length>` | `auto` | Yes | No |
| `text-overflow` | `clip \| ellipsis` | `clip` | No | No |
| `white-space` | `normal \| nowrap \| pre \| pre-wrap \| pre-line` | `normal` | Yes | No |

`text-overflow: ellipsis` applies when a non-editable text element has non-visible horizontal overflow, such as `overflow-x: hidden`, and text exceeds the available width.

`font-size: 0` is valid. The text takes no visible space, so a theme can hide a part's text and draw a replacement in `::before` with its own font size.

`font-symbol` pairs each quoted font family with its equivalent quoted glyph text. The provider must match the exact family and cover every scalar. It tries up to 32 pairs in order, draws no symbol ink when none is covered, and uses ordinary text for `none`. It is ignored by editable text. Typed `Icon.Symbol` metadata takes priority. Generated boxes still require `content`. See [native-preferred icons](22-Images-Icons-and-Drawing.md#native-preferred-winui-symbols).

## Motion

| Property | Accepted syntax | Initial | Inherited | Animatable |
| --- | --- | --- | --- | --- |
| `transition` | `none \| [[all \| <property>] \|\| <time> \|\| <easing-function> \|\| <time>]#` | `none` | No | No |
| `overlay-exit-duration` | `<time>` | `0ms` | No | No |
| `submenu-open-delay` | `<time>` | `250ms` | No | No |
| `animation` | `none \| [<time> \|\| <easing-function> \|\| <time> \|\| [infinite \| <number>] \|\| [normal \| reverse \| alternate \| alternate-reverse] \|\| [none \| forwards \| backwards \| both] \|\| [running \| paused] \|\| <keyframes-name>]#` | `none` | No | No |

`Collapsible` also uses `overlay-exit-duration` on its root, including when `KeepMounted=true`. Its `--nz-collapsible-height` custom property holds natural content height; `--nz-collapsible-viewport` optionally overrides the inline viewport fallback for clipped-translation themes. These are component custom properties, not additional NSS properties. [Measured disclosure motion](23-Transitions-and-Animations.md#measured-disclosure-motion)

`ContentTransition`, `FrameTransition`, `CalendarTransition` and `ItemsTransition` also use this retention property for their noninteractive outgoing content. It does not determine their incoming animation duration.

`overlay-exit-duration` applies to overlay surfaces and surfaces marked `exit-presence=true`, including the measured `InlineMessagePresence` viewport around an inline message. Set it on the normal surface or viewport, before closing. It retains the outgoing content; it does not create an animation. Values are nonnegative finite `ms`/`s` times, up to `4294967294ms`; negative or larger intervals are compile errors. Reduced motion forces immediate removal. The default preserves immediate closing for themes without exit motion. See [closing motion](11-Commands-Menus-and-Popups.md#closing-motion).

## Windows native properties

| Property | Accepted syntax | Initial | Inherited | Animatable |
| --- | --- | --- | --- | --- |
| `-windows-title-bar-height` | `standard \| tall` | `standard` | No | No |
| `-windows-title-bar-background-color` | `auto \| <color>` | `auto` | No | No |
| `-windows-title-bar-foreground-color` | `auto \| <color>` | `auto` | No | No |
| `-windows-title-bar-inactive-background-color` | `auto \| <color>` | `auto` | No | No |
| `-windows-title-bar-inactive-foreground-color` | `auto \| <color>` | `auto` | No | No |
| `-windows-title-bar-button-background-color` | `auto \| <color>` | `auto` | No | No |
| `-windows-title-bar-button-foreground-color` | `auto \| <color>` | `auto` | No | No |
| `-windows-title-bar-button-inactive-background-color` | `auto \| <color>` | `auto` | No | No |
| `-windows-title-bar-button-inactive-foreground-color` | `auto \| <color>` | `auto` | No | No |
| `-windows-title-bar-button-hover-background-color` | `auto \| <color>` | `auto` | No | No |
| `-windows-title-bar-button-hover-foreground-color` | `auto \| <color>` | `auto` | No | No |
| `-windows-title-bar-button-pressed-background-color` | `auto \| <color>` | `auto` | No | No |
| `-windows-title-bar-button-pressed-foreground-color` | `auto \| <color>` | `auto` | No | No |

All thirteen `-windows-title-bar-*` properties apply to `:root` on Windows 11. The twelve colors map to `AppWindowTitleBar`, including inactive, hover and pressed states. `auto` clears the explicit native color. The normal NSS cascade resolves declarations using `currentColor`, system colors, and custom properties. Button backgrounds can be transparent. Windows controls native text alpha and the close button's hover and pressed background.

`-windows-title-bar-height: standard | tall` requests the integrated custom title bar height. Use `min-height: var(--nz-window-controls-height, 32px)` on `TitleBar` to follow the measured height. In custom mode, button colors and height configure native controls, and ordinary styles on `TitleBar` draw application content. The optional Fluent theme styles native buttons on `:root[client-titlebar]`.

Native title-bar settings are declared on `:root` with the `-windows-title-bar-*` properties listed above. The measured `--nz-window-controls-height` custom property reports the available control height for layout; it is an output value, not a native setting.

## Window frame and resize

| Property | Accepted syntax | Initial | Inherited | Animatable |
| --- | --- | --- | --- | --- |
| `-window-frame-border-width` | `thin \| medium \| thick \| <length>` | `0px` | No | No |
| `-window-frame-border-style` | `none \| solid \| dashed \| dotted` | `none` | No | No |
| `-window-frame-border-color` | `<color>` | `currentColor` | No | No |
| `-window-frame-border-top-left-radius` | `auto \| <length>` | `auto` | No | No |
| `-window-frame-border-top-right-radius` | `auto \| <length>` | `auto` | No | No |
| `-window-frame-border-bottom-right-radius` | `auto \| <length>` | `auto` | No | No |
| `-window-frame-border-bottom-left-radius` | `auto \| <length>` | `auto` | No | No |
| `-window-frame-box-shadow` | `auto \| none \| [<length>{2,4} && <color>?]#` | `auto` | No | No |
| `-window-frame-resize-inset` | `auto \| <length>` | `auto` | No | No |
| `-window-frame-resize-outset` | `auto \| <length>` | `auto` | No | No |
| `-window-frame-resize-corner-length` | `auto \| <length>` | `auto` | No | No |
| `-window-resize-grip` | `auto \| none` | `none` | No | No |
| `-window-resize-grip-color` | `<color>` | `currentColor` | No | No |

Set these properties on `:root`. Frame appearance applies while Nazor draws the frame. On Windows, this means a window using a Nazor external frame in its normal state. On Wayland and X11, it means a normal, untiled surface with client-side decorations. On X11, rounded corners require Shape support or an ARGB window. Exterior shadows require an available shadow surface and an X11 compositing manager when applicable. The frame border is painted inside the visible edge and leaves the client layout unchanged.

The radius shorthand starts at the top left and proceeds clockwise. A single value sets all four corners, two values alternate across diagonals, and three values repeat the top-right radius at the bottom left. The shadow accepts a comma-separated list of outer shadows. `auto` uses a 10px radius and `0px 5px 24px 0px #00000058` shadow for a framed Wayland window. Other Nazor frames default to square corners and no Nazor shadow.

Resize inset and outset are logical distances inward and outward from the visible frame edge. Zero disables the corresponding Nazor band. Corner length controls how far the diagonal hit zone extends along each edge. With `auto`, Linux uses a 4px inner band, no outer band, and diagonal hits where the edge bands meet. An explicit Linux outer band requires a supporting Wayland or X11 bridge; X11 also requires X Shape input regions. On Windows, a Nazor-owned frame supports an explicit outer band through a nonactivating native input companion; `auto` uses a 4px inner band and no additional outer band. Windows and Linux limit the explicit outer band to 128 logical pixels. A native Windows frame retains its Win32 outer sizing regions. Resize areas are independent of the frame border, radius and shadow, and disappear when interactive resizing is disabled. Runtime stylesheet changes, including NSS Hot Reload, update these properties without recreating the window.

On composited X11, including Xwayland, the outer band belongs to the window's own native surface and does not add client layout space. See [X11 frame behavior](35-Linux-X11.md#system-dependencies-and-behavior) for compositing and placement requirements.

The resize grip draws inside the client area of a resizable desktop window in its normal state. It also works with a system title bar. A transparent grip color suppresses both its drawing and hit target. [Resize grip and frame edges](14-Title-Bars-and-Platform-Integration.md#resize-grip-and-frame-edges)

```css
:root {
    -window-frame-border: 1px solid var(--nz-accent);
    -window-frame-border-radius: 12px;
    -window-frame-box-shadow: 0 6px 24px #00000040;
}

:root:window-inactive { -window-frame-box-shadow: none; }
```

## Shorthands

Shorthands expand to longhands during compilation, or after custom-property substitution. Omitted pieces reset their corresponding longhands rather than preserving an earlier declaration.

| Shorthand | Accepted syntax | Longhands |
| --- | --- | --- |
| `-window-frame-border` | `[thin \| medium \| thick \| <length>] \|\| [none \| solid \| dashed \| dotted] \|\| <color>` | `-window-frame-border-width`, `-window-frame-border-style`, `-window-frame-border-color` |
| `-window-frame-border-radius` | `auto \| <length>{1,4}` | `-window-frame-border-top-left-radius`, `-window-frame-border-top-right-radius`, `-window-frame-border-bottom-right-radius`, `-window-frame-border-bottom-left-radius` |
| `padding` | `<length-percentage>{1,4}` | `padding-top`, `padding-right`, `padding-bottom`, `padding-left` |
| `margin` | `[auto \| <length-percentage>]{1,4}` | `margin-top`, `margin-right`, `margin-bottom`, `margin-left` |
| `border-width` | `[thin \| medium \| thick \| <length>]{1,4}` | `border-top-width`, `border-right-width`, `border-bottom-width`, `border-left-width` |
| `border-style` | `[none \| solid \| dashed \| dotted]{1,4}` | `border-top-style`, `border-right-style`, `border-bottom-style`, `border-left-style` |
| `border-color` | `<color>{1,4}` | `border-top-color`, `border-right-color`, `border-bottom-color`, `border-left-color` |
| `border-radius` | `<length-percentage>{1,4} [/ <length-percentage>{1,4}]?` | `border-top-left-radius`, `border-top-right-radius`, `border-bottom-right-radius`, `border-bottom-left-radius` |
| `border-top` | `[thin \| medium \| thick \| <length>] \|\| [none \| solid \| dashed \| dotted] \|\| <color>` | `border-top-width`, `border-top-style`, `border-top-color` |
| `border-right` | `[thin \| medium \| thick \| <length>] \|\| [none \| solid \| dashed \| dotted] \|\| <color>` | `border-right-width`, `border-right-style`, `border-right-color` |
| `border-bottom` | `[thin \| medium \| thick \| <length>] \|\| [none \| solid \| dashed \| dotted] \|\| <color>` | `border-bottom-width`, `border-bottom-style`, `border-bottom-color` |
| `border-left` | `[thin \| medium \| thick \| <length>] \|\| [none \| solid \| dashed \| dotted] \|\| <color>` | `border-left-width`, `border-left-style`, `border-left-color` |
| `outline` | `[thin \| medium \| thick \| <length>] \|\| [none \| solid \| dashed \| dotted] \|\| <color>` | `outline-width`, `outline-style`, `outline-color` |
| `inset` | `[auto \| <length-percentage>]{1,4}` | `top`, `right`, `bottom`, `left` |
| `gap` | `[normal \| <length-percentage>]{1,2}` | `row-gap`, `column-gap` |
| `overflow` | `[visible \| hidden \| scroll \| auto]{1,2}` | `overflow-x`, `overflow-y` |
| `background` | `[<bg-layer> ,]* [[<image> \| none] \|\| <position> [/ <bg-size>]? \|\| [repeat \| no-repeat \| repeat-x \| repeat-y] \|\| <color>]` | `background-color`, `background-image`, `background-position`, `background-size`, `background-repeat` |
| `flex` | `none \| auto \| [<number>{1,2} \|\| <flex-basis>]` | `flex-grow`, `flex-shrink`, `flex-basis` |
| `grid-row` | `<grid-line> [/ <grid-line>]?` | `grid-row-start`, `grid-row-end` |
| `grid-column` | `<grid-line> [/ <grid-line>]?` | `grid-column-start`, `grid-column-end` |
| `grid-area` | `<grid-line> [/ <grid-line>]{0,3}` | `grid-row-start`, `grid-column-start`, `grid-row-end`, `grid-column-end` |
| `place-items` | `[normal \| stretch \| flex-start \| start \| flex-end \| end \| center]{1,2}` | `align-items`, `justify-items` |
| `place-self` | `[auto \| normal \| stretch \| flex-start \| start \| flex-end \| end \| center]{1,2}` | `align-self`, `justify-self` |
| `place-content` | `[normal \| stretch \| flex-start \| start \| flex-end \| end \| center \| space-between \| space-around \| space-evenly] [normal \| stretch \| flex-start \| start \| flex-end \| end \| center \| space-between \| space-around \| space-evenly]?` | `align-content`, `justify-content` |
| `text-decoration` | `[none \| [underline \|\| overline \|\| line-through]] \|\| [solid \| double \| dotted \| dashed \| wavy] \|\| <color> \|\| [auto \| <length>]` | `text-decoration-line`, `text-decoration-style`, `text-decoration-color`, `text-decoration-thickness` |
| `flex-flow` | `[row \| row-reverse \| column \| column-reverse] \|\| [nowrap \| wrap \| wrap-reverse]` | `flex-direction`, `flex-wrap` |
| `border` | `[thin \| medium \| thick \| <length>] \|\| [none \| solid \| dashed \| dotted] \|\| <color>` | `border-top-width`, `border-right-width`, `border-bottom-width`, `border-left-width`, `border-top-style`, `border-right-style`, `border-bottom-style`, `border-left-style`, `border-top-color`, `border-right-color`, `border-bottom-color`, `border-left-color` |
| `-windows-badge-padding` | `<length-percentage>{1,4}` | `-windows-badge-padding-top`, `-windows-badge-padding-right`, `-windows-badge-padding-bottom`, `-windows-badge-padding-left` |
| `-windows-badge-border-radius` | `<length-percentage>{1,4} [/ <length-percentage>{1,4}]?` | `-windows-badge-border-top-left-radius`, `-windows-badge-border-top-right-radius`, `-windows-badge-border-bottom-right-radius`, `-windows-badge-border-bottom-left-radius` |
| `-windows-badge-border-width` | `[thin \| medium \| thick \| <length>]{1,4}` | `-windows-badge-border-top-width`, `-windows-badge-border-right-width`, `-windows-badge-border-bottom-width`, `-windows-badge-border-left-width` |
| `-windows-badge-border-style` | `[none \| solid \| dashed \| dotted]{1,4}` | `-windows-badge-border-top-style`, `-windows-badge-border-right-style`, `-windows-badge-border-bottom-style`, `-windows-badge-border-left-style` |
| `-windows-badge-border-color` | `<color>{1,4}` | `-windows-badge-border-top-color`, `-windows-badge-border-right-color`, `-windows-badge-border-bottom-color`, `-windows-badge-border-left-color` |
| `-windows-badge-border` | `[thin \| medium \| thick \| <length>] \|\| [none \| solid \| dashed \| dotted] \|\| <color>` | `-windows-badge-border-top-width`, `-windows-badge-border-right-width`, `-windows-badge-border-bottom-width`, `-windows-badge-border-left-width`, `-windows-badge-border-top-style`, `-windows-badge-border-right-style`, `-windows-badge-border-bottom-style`, `-windows-badge-border-left-style`, `-windows-badge-border-top-color`, `-windows-badge-border-right-color`, `-windows-badge-border-bottom-color`, `-windows-badge-border-left-color` |
| `-windows-badge-border-top` | `[thin \| medium \| thick \| <length>] \|\| [none \| solid \| dashed \| dotted] \|\| <color>` | `-windows-badge-border-top-width`, `-windows-badge-border-top-style`, `-windows-badge-border-top-color` |
| `-windows-badge-border-right` | `[thin \| medium \| thick \| <length>] \|\| [none \| solid \| dashed \| dotted] \|\| <color>` | `-windows-badge-border-right-width`, `-windows-badge-border-right-style`, `-windows-badge-border-right-color` |
| `-windows-badge-border-bottom` | `[thin \| medium \| thick \| <length>] \|\| [none \| solid \| dashed \| dotted] \|\| <color>` | `-windows-badge-border-bottom-width`, `-windows-badge-border-bottom-style`, `-windows-badge-border-bottom-color` |
| `-windows-badge-border-left` | `[thin \| medium \| thick \| <length>] \|\| [none \| solid \| dashed \| dotted] \|\| <color>` | `-windows-badge-border-left-width`, `-windows-badge-border-left-style`, `-windows-badge-border-left-color` |
| `-windows-badge-background` | `[<bg-layer> ,]* [[<image> \| none] \|\| <position> [/ <bg-size>]? \|\| [repeat \| no-repeat \| repeat-x \| repeat-y] \|\| <color>]` | `-windows-badge-background-color`, `-windows-badge-background-image`, `-windows-badge-background-position`, `-windows-badge-background-size`, `-windows-badge-background-repeat` |

## Windows launcher badges

All `-windows-badge-*` properties target the native window root on Windows, including when a class selector matches that root. Nazor composes one launcher badge using the computed style and font provider of the window publishing it. See the [launcher service](15-Tray-and-System-Notifications.md#styling-windows-badges) for application ownership and an example.

Nazor composes a square overlay of 16 × 16 logical pixels, scaled for the publishing window's DPI. `size`, `width` and `height` set a centered container constrained to that area. Offsets move it within the overlay; ink outside the overlay is clipped. These properties cannot change the overlay position or reserved area chosen by Windows. A zero container dimension draws nothing without clearing the retained badge request. All 53 longhands are non-inherited and non-animatable.

| Property | Accepted syntax | Initial | Inherited | Animatable |
| --- | --- | --- | --- | --- |
| `-windows-badge-border-top-width` | `thin \| medium \| thick \| <length>` | `0px` | No | No |
| `-windows-badge-border-top-style` | `none \| solid \| dashed \| dotted` | `Solid` | No | No |
| `-windows-badge-border-top-color` | `<color>` | `currentColor` | No | No |
| `-windows-badge-border-right-width` | `thin \| medium \| thick \| <length>` | `0px` | No | No |
| `-windows-badge-border-right-style` | `none \| solid \| dashed \| dotted` | `Solid` | No | No |
| `-windows-badge-border-right-color` | `<color>` | `currentColor` | No | No |
| `-windows-badge-border-bottom-width` | `thin \| medium \| thick \| <length>` | `0px` | No | No |
| `-windows-badge-border-bottom-style` | `none \| solid \| dashed \| dotted` | `Solid` | No | No |
| `-windows-badge-border-bottom-color` | `<color>` | `currentColor` | No | No |
| `-windows-badge-border-left-width` | `thin \| medium \| thick \| <length>` | `0px` | No | No |
| `-windows-badge-border-left-style` | `none \| solid \| dashed \| dotted` | `Solid` | No | No |
| `-windows-badge-border-left-color` | `<color>` | `currentColor` | No | No |
| `-windows-badge-background-image` | `none \| [<url> \| <gradient>]#` | `none` | No | No |
| `-windows-badge-background-position` | `[[left \| center \| right \| top \| bottom \| <length-percentage>]{1,2}]#` | `0% 0%` | No | No |
| `-windows-badge-background-size` | `[cover \| contain \| [auto \| <length-percentage>]{1,2}]#` | `auto` | No | No |
| `-windows-badge-background-repeat` | `[repeat \| no-repeat \| repeat-x \| repeat-y]#` | `repeat` | No | No |
| `-windows-badge-background-clip` | `border-box \| padding-box` | `border-box` | No | No |
| `-windows-badge-transform` | `none \| <transform-function>+` | `none` | No | No |
| `-windows-badge-transform-origin` | `[left \| center \| right \| top \| bottom \| <length-percentage>]{1,2}` | `50% 50%` | No | No |
| `-windows-badge-mask-image` | `none \| <url>` | `none` | No | No |
| `-windows-badge-background-color` | `auto \| <color>` | `auto` | No | No |
| `-windows-badge-color` | `<color>` | `white` | No | No |
| `-windows-badge-size` | `<length-percentage>` | `16px` | No | No |
| `-windows-badge-width` | `auto \| <length-percentage>` | `auto` | No | No |
| `-windows-badge-height` | `auto \| <length-percentage>` | `auto` | No | No |
| `-windows-badge-offset-x` | `<length-percentage>` | `0px` | No | No |
| `-windows-badge-offset-y` | `<length-percentage>` | `0px` | No | No |
| `-windows-badge-opacity` | `<number>` | `1` | No | No |
| `-windows-badge-padding-top` | `<length-percentage>` | `0px` | No | No |
| `-windows-badge-padding-right` | `<length-percentage>` | `0px` | No | No |
| `-windows-badge-padding-bottom` | `<length-percentage>` | `0px` | No | No |
| `-windows-badge-padding-left` | `<length-percentage>` | `0px` | No | No |
| `-windows-badge-border-top-left-radius` | `<length-percentage>{1,2}` | `50%` | No | No |
| `-windows-badge-border-top-right-radius` | `<length-percentage>{1,2}` | `50%` | No | No |
| `-windows-badge-border-bottom-right-radius` | `<length-percentage>{1,2}` | `50%` | No | No |
| `-windows-badge-border-bottom-left-radius` | `<length-percentage>{1,2}` | `50%` | No | No |
| `-windows-badge-font-family` | `<family-name>#` | `"Segoe UI"` | No | No |
| `-windows-badge-font-size` | `<length-percentage>` | `11.2px` | No | No |
| `-windows-badge-font-weight` | `normal \| bold \| <number>` | `700` | No | No |
| `-windows-badge-font-style` | `normal \| italic` | `normal` | No | No |
| `-windows-badge-letter-spacing` | `normal \| <length>` | `0px` | No | No |
| `-windows-badge-text-align` | `start \| end \| left \| right \| center` | `center` | No | No |
| `-windows-badge-vertical-align` | `top \| middle \| bottom` | `middle` | No | No |
| `-windows-badge-text-fit` | `shrink \| none` | `shrink` | No | No |
| `-windows-badge-text-stroke-width` | `<length>` | `0px` | No | No |
| `-windows-badge-text-stroke-color` | `<color>` | `currentColor` | No | No |
| `-windows-badge-image-background-color` | `auto \| <color>` | `auto` | No | No |
| `-windows-badge-image-fit` | `fill \| contain \| cover \| none \| scale-down` | `contain` | No | No |
| `-windows-badge-image-position` | `[left \| center \| right \| top \| bottom \| <length-percentage>]{1,2}` | `50% 50%` | No | No |
| `-windows-badge-image-opacity` | `<number>` | `1` | No | No |
| `-windows-badge-image-clip` | `none \| border-box` | `none` | No | No |
| `-windows-badge-box-shadow` | `none \| [inset? && <length>{2,4} && <color>?]#` | `none` | No | No |
| `-windows-badge-text-shadow` | `none \| [<length>{2,3} && <color>?]#` | `none` | No | No |

The `-windows-badge-padding` shorthand takes one to four lengths/percentages in top/right/bottom/left order. `-windows-badge-border-width`, `-windows-badge-border-style` and `-windows-badge-border-color` are shorthands taking one to four values in the same order. The `-windows-badge-border` shorthand sets width, style and color on all sides; `-windows-badge-border-top/right/bottom/left` target one side. Omitted parts reset to the ordinary border shorthand defaults, so specify `solid`, `dashed` or `dotted` for a visible border. A `none` side has no content inset. Side widths clamp to half the corresponding container dimension. Rounded side joins use the ordinary NSS border painter.

The `-windows-badge-border-radius` shorthand takes one to four radii clockwise from top-left and an optional `/` with vertical radii. The four radius longhands accept horizontal and optional vertical radii. Radius percentages use the container width and height and oversized radii shrink to fit.

Percentages for size, dimensions and offsets use the 16px overlay. Horizontal padding uses container width; vertical padding and font size use container height. Relative length units and math functions follow ordinary NSS computed-style rules: `em` and `rem` refer to the Window's computed font and root font, not the badge font. Border and text stroke widths are lengths; letter spacing also allows negatives. `currentColor`, including in gradients and shadows, resolves to the owning Window's `color`. Set `-windows-badge-color` independently for the count foreground. Opacities clamp to 0–1; complete badge opacity multiplies image opacity and all other ink.

The automatic background is red for counts and transparent for icons. An explicit container background applies to both; an explicit image background overrides this base color only while showing an icon. Decorative `-windows-badge-background-image` layers paint above the base color, first listed layer on top, before inset shadows, borders and count/icon content. They use the ordinary NSS URL/linear/radial-gradient, size, position and repeat syntax; shorter layer lists repeat cyclically. `background-clip: border-box` uses the rounded container; `padding-box` clips both color and layers inside the border. The `-windows-badge-background` shorthand resets color, image, position, size and repeat together; omitted color becomes transparent and does not reset clipping. Decorative URL images load asynchronously through the owning window's decoder and cache; pending or failed layers contribute no ink. Failures are diagnosed. The code-supplied image remains the `ImageSource` passed to `SetBadgeIcon`, separately fitted inside the content box.

Count text uses the owning application's font provider, including private fonts, theme fonts, configured fallbacks, weight, italic style and supported variable-font sizing. Runtime font registrations invalidate the badge snapshot on the next requested frame. `text-fit: shrink` reduces font size and spacing without enlarging the configured font. `text-fit: none` retains the configured size and clips excess ink to the content box. Counts are drawn in full without an automatic cap or abbreviation; the accessible description contains the supplied count. Applications choose any limit before publication. Text outlines and shadows affect counts only. Container shadows support multiple shadows, inset/spread and nonnegative blur. Text shadows support offsets, optional nonnegative blur and color; inset and spread are rejected.

Image fitting preserves aspect ratio with `contain` or `cover`; `fill` stretches. `none` uses the selected prepared variant's size divided by the target DPI scale; `scale-down` uses the smaller of that size and `contain`. Image position resolves its full expression against free space after fitting, including negative free space when `cover` crops. Percentages align the image; lengths offset it. `calc()`, `min()`, `max()` and `clamp()` retain their ordinary mathematical behavior. Images are cropped to the content box inside padding and borders. `image-clip: border-box` additionally clips to the rounded container; `none` leaves image corners square.

`-windows-badge-transform` accepts the ordinary static 2D transform functions and their ordering. Its origin is relative to the container's border box. It transforms the complete badge, including shadows, content and mask, before the fixed overlay clips the final ink. It does not change layout dimensions or request animation.

`-windows-badge-mask-image` accepts `none` or one raster/SVG `url()` image. The image's alpha, regardless of RGB color, masks the complete composition, including shadows and borders. It stretches over the container's untransformed bounds, then follows the badge transform. SVG paths can supply arbitrary theme shapes without a new shape parser. Pending or failed masks hide the badge; `none` removes the mask. Backgrounds and masks use existing image-source resolution and packaged assets; failures are diagnosed. Theme-specific geometry and licensed assets belong to their separate theme packages.

## Value constraints and shorthand rules

- Padding/margin/border-width/style/color and inset use top, right, bottom, left expansion: one value for all, two for vertical/horizontal, three for top/horizontal/bottom, four for all sides.
- Border radius starts at top-left and proceeds clockwise; `/` separates horizontal and vertical radii. Oversized radii shrink proportionally to fit.
- `gap` is row-gap then column-gap; one value applies to both. `overflow` is x then y. Place shorthands are align then justify.
- `flex: none` expands to `0 0 auto` and `flex: auto` to `1 1 auto`. Grow and shrink are nonnegative. A number-only form uses a `0%` basis. A length, percentage, or intrinsic basis can appear alone or alongside the numbers, as in `flex: 120px`, `flex: min-content`, and `flex: 1 max-content 2`. When using `auto` as a basis with numbers, put it after the first number, as in `flex: 2 auto`.
- Border shorthand needs a visible style. Width keywords thin/medium/thick mean 1/3/5 logical pixels; a none style suppresses layout width. Outline width starts at medium (3px), but its initial style is none.
- `background` supports layered images/gradients, position, `/ size`, repeat, and a color in the final layer. Unspecified parts return to their initial values.
- Grid lines cannot be zero. A span needs a positive count. Named areas must be rectangular. `fr` is not a minimum track size; auto-repeat needs fixed-size-compatible tracks. `fit-content(length)` is a grid-track function, distinct from the intrinsic size keyword.
- Position/object/transform origins use one or two values. Three/four-value edge-offset positions are unsupported.
- Opacity is clamped to 0–1; font weight accepts 1–1000. Nonnegative lengths are required except where offsets/margins or other documented grammars allow negatives.
- Shadow grammar contains optional inset, x/y offset, optional blur/spread, and color; blur is nonnegative. Gradient and transform grammar is described in the drawing chapter.
- Text decoration is inherited in NSS, with the declaring color retained. This models propagation differently from browser CSS.
- Every property accepts inherit/initial/unset. Important, revert, font/all/grid shorthands, logical border properties, transition longhands, and animation longhands are unsupported.

`ValueDeclaration.ToString()` uses NSS keyword spelling for enum declarations, including space-separated decoration-line flags and combined `column dense` grid flow. Recompiling these declarations preserves their typed values; diagnostic output no longer uses .NET PascalCase/comma-separated enum names for these properties.

The runtime reference for descriptor types and value representations is [Nazor.Style](../ApiReferences/Nazor.Style.md). Unsupported features are summarized in [support and limitations](32-Support-and-Limitations.md).

The `number-motion*` and `number-boundary-tolerance` properties configure [numeric presentation](23-Transitions-and-Animations.md#numeric-presentation). They do not interpolate themselves or change accessibility. Linear duration is per scalar unit; exponential decay uses a normalized 60Hz reference cadence.

---

[Guide index](README.md) · [API reference](../API.md)

`submenu-open-delay` controls pointer-hover intent for Submenu, using the containing menu's motion provider. It accepts nonnegative finite ms/s intervals up to 4294967294ms. Reduced motion resolves the effective delay to zero; keyboard, click and explicit opening stay immediate. Basic and Fluent keep the default 250ms; NazorLeaf requests 60ms.
