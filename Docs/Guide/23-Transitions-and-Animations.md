# 23. Transitions and animations

## Transitions

```css
Button {
    transition: background-color 150ms ease-out, transform 180ms ease;
    &:hover { background-color: #eee; transform: translateY(-2px); }
}
```

`transition` accepts none or comma-separated entries. An entry specifies a property (all when omitted), duration (first time), easing, and delay (second time), in flexible order. Duration/delay default to zero and cannot be negative. A shorthand property targets its animatable longhands. When entries overlap, the last matching entry wins.

Easing supports linear, ease, ease-in, ease-out, ease-in-out, and `cubic-bezier(x1, y1, x2, y2)` with x coordinates between 0 and 1. Steps and the CSS `linear()` function are unsupported.

The Gallery's NazorLeaf showcase, **Schede, percorsi ed espansori → Movimento**, provides four replayable lanes: 120ms touch, 500ms opening, 550ms spring indicator and 900ms window appearance. Replay remounts the animated region so every lane starts together, including after an interrupted run. Reduced motion displays the final positions immediately.

A transition starts when a computed value changes and the new style enables that property. The initial style does not transition. An interrupted transition starts from the currently visible value; reversing can shorten its duration. Removing the element cancels its transitions.

`CheckBox` retains separate `CheckBoxCheck` and `CheckBoxMixed` vector children inside `CheckBoxMark`. Its theme trims the inactive stroke to zero instead of removing its element, so checked, unchecked and mixed transitions can reverse from their current visible state. Fluent also scales the glyphs on hover/press while the frame keeps its dimensions. Basic applies the destination immediately. Disabled checkbox glyphs and reduced-motion preferences apply their destination without a transition. The bound checked/mixed value and accessibility state always change immediately. In a global application stylesheet, style the mark for its frame/padding and its two glyph children for `stroke-trim-end`, `stroke-width` or `transform`; keep the state selectors that hide the inactive glyph.

Animatable properties include colors, opacity, compatible dimensions/spacings, flex sizing, borders/radii, outlines, shadows, object position, transforms, and transform origin; consult the [property table](19-NSS-Property-Reference.md). Keyword values such as auto/none/intrinsic size cannot interpolate with arbitrary lengths and change immediately. Font-size, line-height, and letter-spacing do not animate.

Colors use premultiplied alpha. Mixed lengths/percentages can interpolate through a calc expression. Compatible shadow lists pair entries with transparent zero-sized padding; inset and outer shadows do not interpolate with each other. Compatible transform lists interpolate function by function; otherwise matrices are decomposed for interpolation.

## Keyframe animations

```css
@keyframes appear {
    from { opacity: 0; transform: translateY(8px); }
    to { opacity: 1; transform: none; }
}

.notice { animation: appear 200ms ease-out both; }
```

`@keyframes` must be top-level, outside rules and media queries. Frames accept from/to or percentage lists between 0% and 100%. Frames contain animatable declarations and may include `animation-timing-function` for the interval beginning there. A missing endpoint uses the element's computed value. Custom properties and nested rules are invalid in frames.

`animation` accepts comma-separated entries with a case-sensitive name, duration, delay (including negative), easing, iteration count or infinite, direction, fill mode, and running/paused state. Directions are normal/reverse/alternate/alternate-reverse; fill modes are none/forwards/backwards/both. There are no animation longhands; rewrite the shorthand.

The name is searched in the element's component sheet, then window, application, and theme levels. The last matching definition in a sheet wins. A newly introduced name starts an animation; retaining its name preserves its timeline while timing or paused state changes. Removing it cancels the animation. When several animations affect one property, the last wins; an active animation wins over a transition.

Without forwards fill, completion returns to the computed value. With forwards fill, it retains the final reached frame, which depends on direction and iteration count. Backwards fill applies the starting frame during the delay.

## Retained exits

`Presence` keeps its child content after `Present` becomes false for `ExitDuration`, exposing `PresenceState.IsExiting`. Zero removes it immediately. `OnExited` runs once when retention completes, including a zero-duration close; reopening cancels a pending completion. Durations must be between zero and `4294967294ms`. Disposal cancels the pending exit. Custom `Presence` content owns its input, accessibility and focus policy.

Shared overlay primitives apply that policy automatically: retained surfaces carry `exiting=true`, are disabled and semantically hidden, and reject pointer interaction. Their base `overlay-exit-duration` selects retention, while `[exiting=true]` selects the exit appearance. `OverlayContainer.ExitDuration` can override the duration. `InlineMessage` composes Collapsible as `InlineMessagePresence` with naturally measured `InlineMessagePresenceContent`; the inner message keeps its visual parts and caller attributes. Configure its retention and height/spacing transitions on the viewport. Closed actions become unavailable immediately, and reopening reverses the current viewport height while preserving the same message element. NazorLeaf collapses the complete box over 350ms and fades its content over 200ms without animating natural padding; Basic and Fluent remain immediate by default. Pre-release custom selectors must account for the measurement wrappers, and existing inline-message exit durations move to `InlineMessagePresence`. The control no longer implements IDisposable because its child Collapsible owns the motion lifetime. Explicit tooltip containers and implicit tooltips use the same retained lifetime. A reduced-motion preference change completes an active exit immediately. [Closing motion and native surface ownership](11-Commands-Menus-and-Popups.md#closing-motion)

Custom retained surfaces can mark their element `exit-presence=true` and obtain `IOverlayMotion` through `ElementNode.GetFeature<IOverlayMotion>()` after scene attachment. The provider is owned by that scene, supplies the current reduced-motion flag and theme duration, and signals preference changes; detach event subscriptions when disposing the component. The provider feature is cleared when the element leaves the scene or the scene is disposed. Keep the presentation dependency behind this neutral Core contract.

## Measured disclosure motion

`Collapsible` separates its clipped viewport from the natural-size `{As}Content`. It publishes `--nz-collapsible-height` and uses a fixed viewport fallback between zero and that measured height, so a theme can transition compatible lengths instead of `auto`. Put padding and borders on the content part. The content keeps measuring naturally during clipping, width changes and asynchronous content updates.

```css
ExpanderPanel, AccordionItemPanel {
    transition: height 240ms ease-out;
    overlay-exit-duration: 240ms;
}
```

This example supplies custom height timing. A theme can instead override `--nz-collapsible-viewport` to retain the full natural height while the content translates inside the clip. The root exposes `expanded`, `exiting`, `closed` and initial `measuring`; `{As}Content[revealed=true]` marks an open region after its first measured layout has been armed. Keeping the initial content translated until that measurement prevents a first-open flash. Existing or reopened content transitions directly from its current transform.

Fluent uses this translated-content model: opening takes 333ms, closing movement takes 167ms, and the full clip remains until the 167ms Down or 200ms Up close phase ends. Up starts at positive natural height; Down starts at negative height. The layout releases the panel after the close phase rather than shrinking text or clipping with an animated height. A natural-height change during closing retargets the transform and restarts the same bounded exit lifetime; the configured retention interval begins again after the last actual height change. There is no separate disclosure timer.

`Expander.ExpandDirection` supports `Down` and `Up`; `AccordionItem` inherits it. Horizontal expansion is not supplied. The chevron keeps one `chevron-down` glyph and themes rotate it for direction/state. Collapsing focused content returns focus to the expander or collapsible-group header; custom compositions can set `Collapsible.ReturnFocus`. Other focus stays in place.

`Collapsible.KeepMounted` and `Expander.KeepMounted` default to false. False retains closing content until exit completion and then disposes it. True preserves the same disabled, semantically hidden subtree after the close phase, without `display:none`, so natural measurement and local editor state survive. `Presence.KeepMounted` provides the underlying lifetime: it renders the same child even after `OnExited` runs; the caller owns closed input/accessibility policy. Initial closed kept content does not count as an exit. Reduced motion completes an active close immediately and preserves only explicitly kept content.

NazorLeaf keeps the reference's 400ms body entrance and 450ms spring chevron, with immediate collapse. Its entrance runs on actual open even when content is kept mounted. Body padding follows the 50px leading inset when a header has leading content; unadorned bodies keep 18px.

## Content and layout motion

Import `Nazor.Controls.Primitives` for reusable content lifetimes and measured movement. These APIs use the existing NSS transition/keyframe engine and the current reduced-motion preference.

`ContentTransition` replaces its subtree when `ContentKey` changes. An unchanged key updates the existing subtree normally. `Transition` requests `None`, `Entrance`, `Slide`, `DrillIn` or `DrillOut`; `Direction` is `Forward` or `Backward`. Themes style `{As}Layer` through `current`, `exiting`, `transition` and `direction`; `As` defaults to `ContentTransition`. The region forms a clipped positioning context; outgoing layers are absolute, disabled and semantically hidden. Set base `overlay-exit-duration` to retain at most one outgoing subtree, or supply `ExitDuration`. A newer replacement discards the oldest outgoing layer. First content is immediate.

Capture immutable display data in the `ChildContent` fragment when an outgoing layer must preserve the previous appearance. Local component state remains alive until the layer is removed; service subscriptions also remain alive until disposal. Focus inside removed content moves to `InitialFocus`, the first focusable current descendant, or the region; external focus stays unchanged. Reduced motion removes outgoing content immediately. `Frame` and `Calendar` use this primitive, including outgoing calendar zoom grids.

`LayoutTransition` keeps a stable subtree mounted while its natural box relocates or resizes. Its NSS `transition: transform ...` controls motion. Layout reaches the destination immediately; a visual transform starts from the current sampled box. Retargeting a moving element continues from its visible geometry. The wrapper reserves `transform: none` and a top-left origin; put authored transforms inside its content. Scrolling, translated ancestors and axis-aligned scaled ancestors are accounted for without changing scroll offsets or component identity. The same transform participates in painting, hit testing and eligible compositor snapshots. Disabled, suspended and reduced motion reach the destination immediately.

For a connected destination, call `source.CaptureOrigin()` on an attached `LayoutTransition` before removing the source, then assign that snapshot to the destination's `From`. A snapshot is consumed at most once in the same scene and retains geometry only. Supply the destination content yourself; no pixels or live source component are copied. Capture returns null for an unlaid-out, hidden, empty or detached source. Rotation, skew, singular ancestor transforms and cross-scene origins are rejected explicitly when connected motion is requested. Animated clip morphs, cross-window transfers and automatic shared-element name matching are outside this contract.

`ItemsTransition<TItem>` applies stable keys to a collection. Supply `Items`, required `ItemKey` and optional `ItemTemplate`. Keys must be non-null and unique. Reordering preserves component identity and uses measured layout movement. Removed items stay at their last measured box outside normal flow, disabled and semantically hidden; focus moves to an adjacent survivor or the empty collection. Reintroducing a retained key cancels removal and reuses its subtree. `MaximumRetainedItems` bounds outgoing items to 32 by default (valid range zero to 1024); older removals are disposed first. The collection forms a column by default; use `Style` to choose another layout. Themes style `ItemsTransitionItem` for relocation and `ItemsTransitionContent[entering=true]` / `[exiting=true]` for entrance/removal, with base `overlay-exit-duration` on the collection. The content layer separates appearance transforms from the wrapper's geometry transform.

The [Gallery motion example](../../Samples/Nazor.Samples.Gallery/Examples/Navigation/TransitionsSample.nazor) demonstrates page replacement, collection add/remove/reorder and a connected destination. Basic provides immediate movement. Fluent supplies page entrance/slide/drill timelines, calendar zoom with separate outgoing/incoming phases, and collection motion. Motion endpoints remain usable without animation.

## Motion preferences and cost

Set the appearance preference after `Build()` and before `Run()`, or update it on the UI thread while the app is running.

```csharp
app.Appearance.ReducedMotion = true;
```

Use `true` to reduce motion, `false` to allow it explicitly, or `null` to follow the platform preference. Media queries and animation behavior share that effective preference. With reduced motion, transitions apply immediately and finite forwards animations use their final value. Design the base style so content is understandable without animation.

Hidden/minimized windows do not request animation frames. On return, keyframe time reflects elapsed time. Dimension/spacing animations recalculate layout; transform/opacity/color and rectangular clip changes avoid that layout cost. Use `clip-path: inset(...)` to reveal content without scaling text; its [supported subset](19-NSS-Property-Reference.md#rectangular-clipping) includes animated hit testing. Prefer a transform for purely visual movement.

With a native GPU surface, a system-clock animation or transition limited to transform, transform origin and opacity can run on the compositor. Layout, mixed paint effects, smooth scrolling, scrollbar motion and manual-clock work use the UI pipeline. CPU rendering also uses the UI pipeline. [Rendering architecture](31-Architecture-and-Extension-Points.md#pipeline-and-trees)

`TransitionEngine.Start` is a limited imperative bridge for sampled values such as measured layout relocation: supply an animatable property, current value, destination and a `TransitionDefinition`. It replaces that property's transition, respects reduced/suspended motion and leaves keyframe precedence intact. Timings must be nonnegative with a combined maximum of `4294967294ms`; easing values must be finite with x control points between zero and one. The computed fallback must match the destination. Removing an entrance keyframe while enabling an exit transition starts from the sampled entrance value, avoiding an interruption flash. Component animation start/end events, steps easing and general storyboard composition/timeline APIs are not supplied. Use `ManualAnimationClock` for deterministic verification rather than timing sleeps.

API: [Nazor.Animation](../ApiReferences/Nazor.Animation.md), [Nazor.Style](../ApiReferences/Nazor.Style.md).

Fluent `Dialog` uses independent ContentDialog tracks: scale 1.05 to 1 over 250ms on entry, scale 1 to 1.05 over 167ms on exit, and linear 83ms opacity for both the surface and scrim. Scale uses `cubic-bezier(0,0,0,1)`. The outgoing modal barrier stays until 167ms; reopening cancels removal and starts the entry tracks again. Reduced motion skips these tracks and retention. Values follow the upstream WinUI source resources; the installed Gallery timing has not been measured.

## Numeric presentation

`NumberPresenter` in `Nazor.Controls.Primitives` presents a finite bounded scalar through a typed `NumberPresentationState` child context. It starts at `Minimum`, samples the currently displayed value before a new target, and preserves the same physical element and keyed children. Set `As` when embedding it as a control part. `Value` remains the application's target; `Presentation.Value` and the child context contain the displayed sample. Targets clamp to the finite `Minimum`/`Maximum` range. Both bounds and their difference must be finite.

```nazor
@using Nazor.Controls.Primitives

<NumberPresenter Value="fraction">
    <ChildContent Context="sample"><TextBlock>@($"{sample.Value:P0}")</TextBlock></ChildContent>
</NumberPresenter>
@code { private double fraction = 0.42; }
@style {
    NumberPresenter { number-motion: linear-increase; number-motion-duration: 2s; }
}
```

The mode is `none` by default. `linear` moves either direction at the configured duration per scalar unit; `linear-increase` applies decreases immediately. With `2s`, moving from 0.25 to 0.5 takes 500ms. `exponential` removes `number-motion-decay` of the remaining distance per normalized 60Hz reference frame. A decay of 0.14 reproduces 14% convergence at 60Hz and uses elapsed scene time at other render cadences. `number-motion-tolerance` snaps after the preceding reference-frame residual falls below its nonnegative absolute tolerance; zero uses floating-point convergence. Zero/one decay and zero linear duration are immediate. A computed linear lifetime above 4294967294ms is rejected.

`number-boundary-tolerance` controls displayed `AtStart`/`AtEnd`, capped at half the range. The part publishes `--nz-presented-number` at full round-trip precision, the two `number-at-start`/`number-at-end` flags, and actual `number-target`/`number-minimum`/`number-maximum` attributes. Source data and accessible values belong to the containing control; do not replace them with an animated sample.

Reduced motion, scene suspension and hidden owners apply the target immediately. Removal/disposal cancels further presentation callbacks; scene disposal releases its retained owners. The scene schedules frames only while a presentation moves. Number presentation needs scene updates and cannot run as a detached compositor-only timeline.

`Spinner` uses this primitive as its existing `SpinnerIndicator` part. Accessibility reports the actual range immediately. Fluent uses source-derived 2s-per-unit increases and immediate decreases, with the generated ProgressRing's determinate endpoint mapping. Its indeterminate arc rotates 900 degrees over 2s, growing to a half-circle then shrinking. NazorLeaf uses 14% convergence per normalized 60Hz frame, 0.0005 snap tolerance and 0.003 boundary tolerance; its percentage, track gaps and vector completion mark follow the displayed sample together. These are upstream-source/reference-derived policies; equivalence to the installed WinUI build still requires pinned motion captures.

`Tabs` publishes `indicator-ready` only after its indicator has a positive measured box. Fluent navigation strips then animate indicator position/size for 250ms; initial placement is direct. Document sheets hide the navigation indicator. Opt-in reordered headers reuse measured layout translation, with reduced motion resolved by the shared engine; keyed panel state remains intact.

## Fluent overlay family motion

| Family | Entry | Retained exit |
| --- | --- | --- |
| Menu/context menu, popover/hover card and calendar flyout | 50-unit travel from the actual arranged side, 333ms easing plus 83ms opacity | 150ms opacity |
| Select/ComboBox | Existing 250ms directional clip reveal | 150ms opacity |
| Tooltip | 250ms opacity, without translation | 150ms opacity |
| Drawer | From its configured window edge, 333ms | 200ms edge travel/opacity; scrim fades separately |
| Dialog, AlertDialog, DialogContainer | 1.05-to-1 scale250ms plus83ms opacity | Scale167ms plus83ms opacity; separate scrim |

Flyout direction and the 50-unit distance follow upstream `FlyoutBase` placement logic; tooltip fade-only behavior follows its template. Durations here are explicit theme candidates awaiting installed native capture, rather than verified WinUiGallery timing. Drawer is a portable edge-surface treatment. Dialog, AlertDialog and generic DialogContainer share ContentDialog scale250ms/opacity83ms entry and scale167ms/opacity83ms exit, with current-value reversal; TeachingTip retains its separate motion contract.

Reentered surfaces suppress initial entry keyframes and retarget transitions from their current values. `reentered` lasts until that retained surface fully closes. Explicit and implicit tooltips follow this same rule; a new implicit tooltip target has fresh identity. Input and accessibility are disabled immediately on exit, and modal drawer barriers remain until disposal. Reduced-motion changes end retention immediately through the shared host preference bridge.

## NazorLeaf interaction motion

NazorLeaf assigns timing per interaction: ordinary button press properties use 80ms, while slider thumb width/height changes use 200ms. Slider deformation changes actual dimensions around a fixed track center; it does not scale value labels or animate the value coordinate. Interrupting a press/hover retargets the current size. Reduced motion applies the destination immediately.

NazorLeaf dialog motion uses a 550ms corner/translation/scale entry and a 180ms retained fade/scale exit. Its keyframe specifies the initial corners and uses each surface's computed radius as the destination, so custom styling remains authoritative. Interruptions sample current values; retained reopen metadata suppresses a second entry animation. Reduced motion skips these visual phases. Native top-level window frames use their host policy.
The scrim has its own 300ms entry, continues that timeline through an interrupted close/reopen, and remains at full opacity until body removal; no separate scrim exit fade is added.

## Feedback motion

ToastHost reuses ItemsTransition<ToastEntry> for keyed lifetime and bounded outgoing visuals. `PreserveExtentDuringExit` optionally reserves captured outgoing vertical extents without putting removals back into normal flow. `RenderedCountChanged` reports current plus retained items after rendering, including zero after the final exit; this lets a containing overlay finish without a second presentation list or timer policy. ToastHost keeps its empty carrier mounted but hidden and non-overlay.

NazorLeaf tooltips separate 150ms opacity from 300ms transform, with retained current-value reopen and a hidden description after exit. Toasts separate 200ms opacity from 550ms transform, using 160% of their own border-box height plus a -3 degree rotation. Source dimensions drive travel, including multiline notifications. The queue owns expiry, focus/pointer pause, action callbacks, promotion and announcements; visual retention never re-announces a dismissed entry. Reduced motion completes outgoing lifetime immediately.

## Badge count replacement

Badge renders a count in a BadgeValue element keyed by the actual integer. Changing 100 to 101 replaces that value even while both show 99+; ordinary parent renders keep the same value element and its animation. The decorated ChildContent and its focused editor remain mounted. Removal of the previous value is immediate, so rapid changes retain one value and need no timer or second transition engine.

Basic and Fluent apply the new count immediately. NazorLeaf animates BadgeValue[changed=true] from translateY(90%)/opacity 0 over 350ms using cubic-bezier(.16,1,.3,1), clipped by the indicator. This is independent of the indicator's 450ms entrance pop. Initial values do not roll; returning from a hidden zero after a count change can roll. Reduced motion finishes the decorative change immediately, while accessibility always exposes the latest actual uncapped count once, never animation samples. Explicit AccessibleName remains authoritative.

---

[Guide index](README.md) · [API reference](../API.md)
