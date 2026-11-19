# Commands, menus, and overlays

Fluent dropdown menus and choice lists use a small 2px gap between entries, including entries inside menu sections and radio groups. Popup sizing includes these gaps when limiting the visible options.

`Button` invokes its click callback. `ButtonType.Submit` and `ButtonType.Reset` send the corresponding request to the nearest `Form`. `IsDefault` enables Enter activation in the current form or dialog when the focused editor does not consume it. `Link` exposes link semantics for an application callback or an allowed absolute URI. `Toolbar` groups commands and supports arrow-key navigation.

Dialog leading content aligns with the first title line, including wrapped subtitles and titles. Themes share `--nz-dialog-title-font-size` and the unitless `--nz-dialog-title-line-height` between title and leading-content geometry. Override both tokens when changing title metrics so icons remain aligned at enlarged font sizes.

An overlay hidden with `display: none`, including through an ancestor, is inactive: it does not open a native surface, take focus, intercept dismissal or leave a modal scrim behind. Showing it again initializes its focus normally. This allows themes to offer alternate popup presentations without invisible input surfaces.

## Menus

The [menu buttons example](../../Samples/Nazor.Samples.Gallery/Examples/Actions/MenuButtonsSample.nazor) includes an independent checked SplitButton primary action and its color-choice menu. Changing the menu choice does not toggle the primary action. The [toolbar example](../../Samples/Nazor.Samples.Gallery/Examples/Actions/ToolbarSample.nazor) includes real `Commands`, a configurable width/label policy, high-priority Save, checked Wrap and a dynamically disabled Delete; try commands inside and outside the measured overflow menu.

Give `MenuBarItem` a label in `ChildContent` and menu items in `Content`. The label text names the opened menu for assistive technologies. Set `MenuAccessibleName` to provide a different name or to label a menu opened by an icon. `MenuButton` and `SplitButton` accept items in their `Menu` slot. `MenuCheckItem` and `MenuRadioItem<TValue>` expose checked state to assistive technologies. Set `CloseOnSelect` to false for a choice that should keep the menu open.

```nazor
<MenuBar>
    <MenuBarItem>
        <ChildContent>View</ChildContent>
        <Content>
            <MenuCheckItem @bind-IsChecked="showDetails" CloseOnSelect="false">Show details</MenuCheckItem>
            <MenuSeparator />
            <Submenu>
                <ChildContent>Export</ChildContent>
                <Content><MenuItem OnClick="ExportCsv">CSV</MenuItem></Content>
            </Submenu>
        </Content>
    </MenuBarItem>
</MenuBar>
<TextBlock>@status</TextBlock>

@code {
    private bool showDetails;
    private string status = "Ready";
    private void ExportCsv() => status = "CSV export requested";
}
```

`ContextMenu` opens on secondary click or a context-menu keyboard request on its trigger. Use `ShowAsync` when the application handles the request separately. Up and Down navigate enabled commands and stop at the first and last command. Keyboard navigation scrolls the focused command into view, including Home and End. Enter and Space activate commands, and Escape dismisses the current menu level.

Use a menu bar for window-wide commands, a `MenuButton` for one control's choices, and a `SplitButton` when the most common action deserves its own activation target. Use a context menu when commands act on a particular item. See the [menu button](../../Samples/Nazor.Samples.Gallery/Examples/Actions/MenuButtonsSample.nazor), [menu bar](../../Samples/Nazor.Samples.Gallery/Examples/Overlays/MenuBarSample.nazor), and [context menu](../../Samples/Nazor.Samples.Gallery/Examples/Overlays/ContextMenuSample.nazor) examples.

## Popovers and dialogs

For custom nonmodal compositions, `PopoverContainer.AllowAnchorInteraction` allows the anchor to receive a first press while its popup is open. It defaults to false and never bypasses an active modal scope. `Combobox<TItem>` enables it so editor, clear, query and chevron actions work without a dismiss-and-reopen cycle. Its own actions decide whether to close the popup; an ordinary `Select` retains its existing light-dismiss behavior.

`TeachingTip` introduces a feature with optional `Hero`, title, subtitle, body and `Actions`. Set `Target` or `AnchorPoint` for a callout; leave both null for a centered tip without a tail. Tips remain open on outside presses unless `CloseOnOutside` is true, and `FocusOnOpen` controls focus entry. Fluent clips hero corners inside its border and animates expansion/contraction from the arranged tail. Scrolling body fields retain horizontal space for focus ink.

`PopoverContainer.ShowArrow` opts a custom surface into arranged arrow geometry: the arrow follows flips and viewport/native constraints and keeps its margins clear of rounded corners. Set `OriginAtArrow="true"` to scale/rotate from its tip; it defaults to false, and TeachingTip enables it. Use the resolved popup selectors for tail border styling.

`Popover` anchors nonmodal content to a trigger or explicit element. Its options control placement, focus and dismissal. `Dialog` renders a surface inside the current window, with a title and an optional footer of `DialogButton` actions. `AlertDialog` is modal. Escape runs a Cancel action when one is available. Use `Tooltip` for noninteractive help.

```nazor
<Popover Title="Details">
    <Trigger><Button>Details</Button></Trigger>
    <ChildContent><TextBlock>Additional contextual information.</TextBlock></ChildContent>
</Popover>

<Button OnClick="() => open = true">Review</Button>
<Dialog @bind-IsOpen="open" Title="Review details" DefaultButton="DialogButtonRole.Primary">
    <ChildContent><TextBlock>Check the information before continuing.</TextBlock></ChildContent>
    <Footer><DialogButton Role="DialogButtonRole.Primary">Done</DialogButton></Footer>
</Dialog>
@code { private bool open; }
```

Overlay elements retain their stylesheet scope. An anchored surface keeps its control's `Offset` from the anchor and adds its NSS margin on the side facing the anchor. For example, `SelectPopup { margin: 6px; }` opens a drop-down list 6 px below its field, or 6 px above it when it flips; the space also counts against the room available to the list. Use `IWindowService` to open a separate native window. The [Gallery overlays](../../Samples/Nazor.Samples.Gallery/Examples/Overlays) include runnable examples.

Focus navigation and requests to reveal an overlay's content scroll only containers inside that surface. Wheel scrolling can pass from a nested scrolling region to the surface, but stops there even when the list is short or already at its limit. Page Up, Page Down, Home and End also stay within that boundary. Overlay primitives stop click, key-down and wheel events from bubbling into their logical owner. A modal dialog blocks wheel interaction with its background; outside a nonmodal popover, the page remains scrollable.

The scrolling `DialogBody` reserves 4 logical pixels around its content for focus outlines and shadows, with compensating margins that preserve field alignment. Its header and footer remain outside that scrolling region. Applications drawing wider focus effects can increase the body's padding and matching negative margin without disabling its overflow clipping.

For a selector that opens over its entry, set `PopoverContainer.AlignmentTarget` to a retained descendant such as its selected row. Its vertical center aligns with the real anchor center; `Alignment` controls the horizontal axis. Hidden, removed or unrelated targets use ordinary placement. Work-area constraints can move the popup away from exact alignment. Native hosts receive the corresponding signed offset; the compositor retains final placement authority. The [segmented pickers](10-Text-Input-and-Data-Controls.md#choose-segmented-dates-and-times) use this bridge.

### Closing motion

Closing changes the control's open state immediately. A theme can keep the outgoing surface visible for its exit animation with `overlay-exit-duration`; its default is `0ms`. Set the duration on the normal surface selector, then style `[exiting=true]` for the reverse animation. The duration must cover the exit animation, including any delay. The `OverlayContainer.ExitDuration` parameter optionally overrides the theme; `OnExited` runs once when retention completes. Reopening cancels that completion and resumes the same surface.

```css
Dialog { overlay-exit-duration: 180ms; }
Dialog[exiting=true] { animation: close-dialog 180ms ease-in both; }
@keyframes close-dialog { from { opacity: 1; } to { opacity: 0; } }
```

This example supplies a custom fade; the duration property itself does not create an animation. Dialogs, menus, popovers, teaching tips, selection lists and picker fields keep their shared primitive mounted through closing. The outgoing surface has `exiting=true`, rejects interaction and is hidden from assistive technologies. A dialog's scrim receives the same exit marker. A modal barrier remains until removal, suppressing window keyboard actions and shortcuts as well as background input; focus returns after removal. Reopening restores content focus while preserving the original return target.

Native popup ownership follows the retained surface. If a concrete anchor disappears or becomes hidden, its outgoing surface stops being presented immediately instead of reappearing at a fallback position. Reduced motion ends any active exit immediately, including when the preference changes during closing. `TooltipContainer` keeps a hidden description after its visible exit; implicit `Tooltip` descriptions use the same retention lifetime. `InlineMessage` uses a measured `InlineMessagePresence` viewport for its theme duration, collapses its complete flow box and makes closing actions unavailable. See [retained exits](23-Transitions-and-Animations.md#retained-exits).

### Popup surfaces outside the window

Set `Window.AllowPopupsOutsideWindow = true` on the UI dispatcher to allow menus, drop-down lists and anchored popovers to use native popup surfaces. The setting defaults to false. `Window.Capabilities.CanShowPopupsOutsideWindow` reports backend support independently of the setting. Windows supports owned popup windows. Wayland uses `xdg_popup`. X11 requires an ARGB visual and a compositing manager. The runtime uses the in-window overlay when a native surface is unavailable. Dialogs and single-view DRM overlays use the current viewport.

Popup components keep their logical owner, stylesheet scope, NSS borders, corner radii and shadows. These transparent popup surfaces use raster presentation. An open popup follows its anchor as the owner moves. Outside interaction and Escape follow the control's dismissal policy. A menu closes when a title-bar drag begins, while a persistent popover can remain attached.

Menus and choice lists that close on outside interaction also close when their owner is deactivated or begins an interactive move. Linux tracks focus across the owner and its popup surfaces, including nested menus. On Linux, a persistent popover uses the owner's internal overlay while that owner is inactive and restores its external surface on activation. This keeps persistent content in the owner's window layer while another application is active. Native popup stacking within the active desktop remains controlled by the compositor or window manager.

Basic menus use their content height up to the applicable visible area. They acquire a vertical scrollbar when the content exceeds that area. The area is the window viewport for internal menus, the monitor work area on Windows/X11 for external menus, or the configured popup size supplied by the Wayland compositor.

For an external drop-down in automatic placement, Nazor prefers below whenever it fits in the display work area, even across the owner edge. Windows and X11 measure display space. Wayland sends the placement preference to the compositor and fits the list to the configured geometry. For an internal list, placement uses the window viewport. [Choice list placement](09-Control-Catalog.md#forms-and-editors)

Try opening Review with the keyboard. A modal dialog keeps focus inside the surface. Escape dismisses this dialog and returns focus to the previous control when it is still available. Set `InitialFocus` and `ReturnFocus` when your workflow has explicit focus targets. Use `AlertDialog` for a decision that needs modal attention, and an `InlineMessage` for feedback that can stay beside the affected field. See the [dialog example](../../Samples/Nazor.Samples.Gallery/Examples/Overlays/DialogSample.nazor), [alert dialog example](../../Samples/Nazor.Samples.Gallery/Examples/Overlays/AlertDialogSample.nazor), and [keyboard and focus guide](24-Input-and-Accessibility.md#focus-shortcuts-and-pointer-capture).

A generic `Dialog` has no implicit Enter/accent action. Set its nullable `DefaultButton` explicitly; body controls keep initial focus and content that uses Enter suppresses the action. See [window-local dialog sizing, focus and migration](13-Windows-and-Dialogs.md#window-local-dialogs).

### Menu timing and interrupted closing

NazorLeaf menus reveal from the nearest arranged corner, including pointer menus clamped against viewport edges and native-configured popup rectangles. The clip opens over 500ms while opacity completes in 120ms. Pointer-opened submenus wait 60ms and reveal over 450ms; keyboard/click activation skips the hover delay. `submenu-open-delay` supplies the theme value through the containing overlay's motion provider. `HoverIntent` accepts an optional TimeProvider; Submenu uses a cascading TimeProvider for deterministic host/test scheduling. Leaving, disabling, removing or closing the owner prevents a late submenu from opening.

Closing uses the shared presence lifetime, retaining the noninteractive surface for 220ms while clip and opacity close over 220ms and 180ms. Entry keyframes are sampled into exit transitions. Reopening retained content publishes `reentered=true`, so themes use the current transition value instead of restarting an entry keyframe at a collapsed origin. The metadata resets after a full exit; a later fresh surface gets a normal entry. Reduced motion opens and removes immediately. No menu-specific modal/focus/native-surface lifetime was added.

## Held repeat actions

The existing `Button` and low-level `Pressable` provide repeat-button composition; a separate facade is unnecessary. Set `RepeatInterval` to opt in and `RepeatDelay` for the initial wait (400ms by default). Ordinary Button activation stays click-based when the interval is null. With repetition enabled, a primary pointer press or focused unmodified Enter/Space fires the first action immediately, then repeats after the delay at each interval. Releasing ends the hold without another activation. OS key-repeat events do not restart the configured timer.

```razor
<Button RepeatInterval="TimeSpan.FromMilliseconds(100)"
        RepeatDelay="TimeSpan.FromMilliseconds(400)" OnClick="Increment">Hold to increment</Button>
```

Secondary pointers and interactive descendants do not start a held gesture. Leaving the surface, Escape while focused, lost capture, focus loss, disabled/busy state, ancestor disabling, timer-provider replacement and disposal cancel pending work. Queued stale timer callbacks are ignored. Callback handlers run through the component UI dispatcher; application code still owns asynchronous action state. Repeating actions invoke the callback/command; they do not repeatedly submit or reset a form.

Both controls accept a cascading `TimeProvider Time`, defaulting to `TimeProvider.System`; use `CascadingValue<TimeProvider>` to supply a deterministic clock in tests or a host timer provider. Intervals must be positive; delays may be zero. Durations must fit the underlying timer's nonnegative supported range (at most 4,294,967,294ms). Changing the provider cancels the existing hold. `Pressable.OnLongPress` remains pointer-only, fires after `LongPressDelay`, and consumes release activation.

Migration: prior repeat behavior was documented for pointers. Configured buttons now repeat while focused Enter/Space is held, and stop immediately when disabled, busy, canceled or disposed. Code relying on an extra release callback or secondary-pointer repeat should move that action into its explicit callback instead. [Runnable button sample](../../Samples/Nazor.Samples.Gallery/Examples/Actions/ButtonSample.nazor)

Fluent overlay families use distinct motion treatments: arranged-side flyout travel for menus/popovers, the existing directional clip reveal for selects, opacity-only tooltips, and edge travel for drawers. Exits use the retained noninteractive lifecycle. Reopening before disposal samples the current visual rather than resetting a fresh entrance. Implicit tooltip surfaces have target identity; moving to another target creates a fresh surface, while returning to the same exiting target retains its fade. Installed WinUiGallery motion acceptance remains separate from these source-informed treatments.

Tooltip content remains noninteractive and lets pointer clicks pass through. Moving the pointer from a trigger onto its tooltip keeps the description open; leaving both dismisses it unless keyboard focus owns the tooltip. This also applies to triggers in a custom title bar.

ToastHost retains outgoing notifications through a scoped ItemsTransition collection. The queue promotes waiting entries immediately while outgoing actions become unavailable; the hidden empty carrier stops being an overlay after the last exit. Retained visuals do not extend countdowns or produce a second announcement. [Feedback motion and style selectors](23-Transitions-and-Animations.md#feedback-motion)

OverlayContainer and TooltipContainer preserve the original caller stylesheet scope through their transparent Presence wrapper. Caller-scoped rules for the surface keep applying during open, retained exit and reopen; child-part boundary rules otherwise remain unchanged.
