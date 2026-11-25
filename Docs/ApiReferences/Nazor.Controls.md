# Nazor.Controls

The gallery's FileField example uses the current window's `IFileDialogService` for single/multiple files and folders, with named glob filters. See [picker integration and cancellation](../Guide/10-Text-Input-and-Data-Controls.md#pick-files-and-folders).

The signatures below describe the public and protected types and members declared by this package. Inherited members are documented with the declaring type.

The SDK references this package. Application controls use the `Nazor.Controls` namespace. Reusable behavior and composition APIs use `Nazor.Controls.Primitives`. See the [control catalog](../Guide/09-Control-Catalog.md), the [composition guide](../Guide/36-Advanced-Controls-and-Composition.md), and the [NSS reference](../Guide/19-NSS-Property-Reference.md).

[Application controls](#application-controls) · [Reusable primitives](#reusable-primitives) · [Built-in theme](#built-in-theme)

## Application controls

### `Accordion`

Namespace `Nazor.Controls`. [Source](../../Sources/Core/Nazor.Controls/Layout/Accordion.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed class Accordion : ContentControl` | A stack of `Nazor.Controls.AccordionItem` expanders that open one at a time (`Nazor.Controls.Accordion.ExpandMode`) or independently. `Nazor.Controls.Accordion.ExpandedValues` holds the `Nazor.Controls.AccordionItem.Value` of the expanded items and supports `@bind-ExpandedValues`; with `Nazor.Controls.Accordion.RequireOne` one item always stays expanded. Up and Down move the focus between the item headers, Home and End to the first and last. The element is `Accordion`, a group with the `expand-mode` and `disabled` attributes. |
| `protected override void OnParametersSet()` | Validates the mode and adopts expanded values changed by the parent. |
| `protected override Task OnAfterRenderAsync(bool firstRender)` | Expands the first enabled item when one is required and none is expanded.<br>`firstRender`. Whether this is the first render.<br>**Returns**. A task that completes when a required expansion has been reported. |
| `protected override void AddAttributes(RenderTreeBuilder builder)` | Adds the group role, the mode and the disabled state.<br>`builder`. The builder that receives the render frames. |
| `protected override void AddContent(RenderTreeBuilder builder)` | Supplies the accordion to its items.<br>`builder`. The builder that receives the render frames. |
| `public Task ToggleAsync(string value)` | Expands an item when collapsed and collapses it when expanded, as a click on its header does.<br>`value`. The value of the item.<br>**Returns**. A task that completes when the change has been reported. |
| `public Task SetExpandedAsync(string value, bool expand)` | Expands or collapses an item as the user does, keeping the mode and the required item, and reports the change.<br>`value`. The value of the item.<br>`expand`. Whether the item should be expanded.<br>**Returns**. A task that completes when the change has been reported. |
| `public Accordion()` |  |
| `public IReadOnlyCollection<string>? ExpandedValues { get; set; }` | `[Parameter]`<br>Gets or sets the values of the expanded items. |
| `public EventCallback<IReadOnlyCollection<string>> ExpandedValuesChanged { get; set; }` | `[Parameter]`<br>Gets or sets the callback invoked when the user expands or collapses items. |
| `public AccordionExpandMode ExpandMode { get; set; }` | `[Parameter]`<br>Gets or sets how many items can be expanded together. |
| `public bool RequireOne { get; set; }` | `[Parameter]`<br>Gets or sets whether one item always stays expanded: the expanded item cannot be collapsed by the user, and the first enabled item expands when none is. |
| `public bool Disabled { get; set; }` | `[Parameter]`<br>Gets or sets whether every item rejects interaction. |
| `public IReadOnlyList<string> Expanded { get; }` | Gets the values of the expanded items in the order of the items. |
| `protected override string ElementName { get; }` | Gets the NSS element name emitted by Accordion. |

### `AccordionExpandMode`

Namespace `Nazor.Controls`. [Source](../../Sources/Core/Nazor.Controls/Layout/Accordion.cs)

| Declaration | Contract |
| --- | --- |
| `public enum AccordionExpandMode` | How many items of an `Nazor.Controls.Accordion` can be expanded together. |
| `Single = 0` | Expanding an item collapses the others. |
| `Multiple = 1` | Items expand and collapse independently. |

### `AccordionItem`

Namespace `Nazor.Controls`. [Source](../../Sources/Core/Nazor.Controls/Layout/Accordion.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed class AccordionItem : Expander, IDisposable` | An item of an `Nazor.Controls.Accordion`: an `Nazor.Controls.Expander` whose expansion belongs to the accordion and is identified by `Nazor.Controls.AccordionItem.Value`. Its parts are named `AccordionItemHeader`, `AccordionItemChevron`, `AccordionItemPanel` and so on. `Nazor.Controls.Expander.IsExpanded` is ignored; `Nazor.Controls.Expander.IsExpandedChanged` still reports the user's changes to this item. |
| `protected override void OnParametersSet()` | Joins the enclosing accordion. |
| `protected override void AddHeaderAttributes(RenderTreeBuilder builder)` | Adds the arrow-key movement between headers.<br>`builder`. The builder that receives the render frames. |
| `public override Task SetExpandedAsync(bool value)` | Expands the item through the accordion.<br>`value`. Whether the item should be expanded.<br>**Returns**. A task that completes when the change has been reported. |
| `public void Dispose()` | Leaves the accordion. |
| `public AccordionItem()` |  |
| `public string Value { get; set; }` | `[Parameter]` Initial value `""`.<br>Gets or sets the value that identifies the item in `Nazor.Controls.Accordion.ExpandedValues`. |
| `public Accordion? Owner { get; set; }` | `[CascadingParameter]`<br>Gets or sets the enclosing accordion, supplied automatically. |
| `public override bool Expanded { get; }` | Gets whether the accordion expands this item. |
| `protected override bool IsInactive { get; }` | Gets whether the item or its accordion rejects interaction. |
| `protected override string ElementName { get; }` | Gets the NSS element name emitted by AccordionItem. |

### `ActionBar`

Namespace `Nazor.Controls`. [Source](../../Sources/Core/Nazor.Controls/Actions/ActionBar.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed class ActionBar : ContentControl` | A strip of commands along the edge of a pane, such as Cancel and Save at the bottom of a settings page or the selection commands under a list: `Nazor.Controls.ActionBar.Start`, `Nazor.Controls.ActionBar.Center` and `Nazor.Controls.ActionBar.End` slots (`ActionBarStart`, `ActionBarCenter`, `ActionBarEnd`) keep their content at the leading edge, in the middle and at the trailing edge; content between the tags is added to the end slot. Like a `Nazor.Controls.Toolbar` it has the `toolbar` role, so it is one Tab stop with arrow navigation between its controls. |
| `protected override void AddAttributes(RenderTreeBuilder builder)` | Adds the toolbar role.<br>`builder`. The builder that receives the render frames. |
| `protected override void AddContent(RenderTreeBuilder builder)` | Adds the three slots.<br>`builder`. The builder that receives the render frames. |
| `public ActionBar()` |  |
| `public RenderFragment? Start { get; set; }` | `[Parameter]`<br>Gets or sets the content at the leading edge. |
| `public RenderFragment? Center { get; set; }` | `[Parameter]`<br>Gets or sets the content in the middle. |
| `public RenderFragment? End { get; set; }` | `[Parameter]`<br>Gets or sets the content at the trailing edge, before the content between the tags. |
| `protected override string ElementName { get; }` | Gets the NSS element name emitted by ActionBar. |

### `ActionRow`

Namespace `Nazor.Controls`. [Source](../../Sources/Core/Nazor.Controls/Collections/ActionRow.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed class ActionRow : ContentControl, IDisposable` | A settings-style row: `Nazor.Controls.ActionRow.Leading` content such as an icon, a `Nazor.Controls.ActionRow.Title` with an optional `Nazor.Controls.ActionRow.Subtitle`, `Nazor.Controls.ActionRow.Trailing` content such as a switch or a value, and an optional `Nazor.Controls.ActionRow.Chevron` for rows that open another page. A row is either clickable (`Nazor.Controls.ActionRow.IsClickable` or an `Nazor.Controls.ActionRow.OnClick` handler: it is a button activated by a click, Enter or Space), or it forwards a click anywhere on it to the first focusable control of its trailing content (`Nazor.Controls.ActionRow.ActivatesTrailing`), as a label does for its check box, or it is plain. Put rows in a `Nazor.Controls.RowGroup`. The parts are `ActionRowLeading`, `ActionRowText` (with `ActionRowTitle`, `ActionRowSubtitle` and the `ChildContent`), `ActionRowTrailing` and `ActionRowChevron`. |
| `protected override void OnParametersSet()` | Validates the parameters and joins the enclosing group. |
| `protected override void AddAttributes(RenderTreeBuilder builder)` | Adds the role, the states, the activation and the accessible name.<br>`builder`. The builder that receives the render frames. |
| `protected override void AddContent(RenderTreeBuilder builder)` | Adds the leading content, the text, the trailing content and the chevron.<br>`builder`. The builder that receives the render frames. |
| `public void Dispose()` | Leaves the enclosing group. |
| `public ActionRow()` |  |
| `public string? Title { get; set; }` | `[Parameter]`<br>Gets or sets the main text of the row. |
| `public string? Subtitle { get; set; }` | `[Parameter]`<br>Gets or sets the secondary text shown below the title. |
| `public RenderFragment? Leading { get; set; }` | `[Parameter]`<br>Gets or sets the content before the text, such as an icon or an avatar. |
| `public RenderFragment? Trailing { get; set; }` | `[Parameter]`<br>Gets or sets the content after the text, such as a switch, a check box, a value or a button. |
| `public bool Chevron { get; set; }` | `[Parameter]`<br>Gets or sets whether a chevron shows that the row opens something. |
| `public bool IsClickable { get; set; }` | `[Parameter]`<br>Gets or sets whether the whole row is a button; it is implied by an `Nazor.Controls.ActionRow.OnClick` handler. |
| `public EventCallback<PointerEventArgs> OnClick { get; set; }` | `[Parameter]`<br>Gets or sets the callback invoked when a clickable row is activated by a click, Enter or Space. |
| `public bool ActivatesTrailing { get; set; }` | `[Parameter]`<br>Gets or sets whether a click anywhere on the row activates the first focusable control of `Nazor.Controls.ActionRow.Trailing`, for example toggling its switch; the control itself stays the keyboard stop. |
| `public bool Disabled { get; set; }` | `[Parameter]`<br>Gets or sets whether the row and its content reject interaction. |
| `public RowGroup? Group { get; set; }` | `[CascadingParameter]`<br>Gets or sets the enclosing group, supplied automatically. |
| `protected override string ElementName { get; }` | Gets the element name. |

### `AlertDialog`

Namespace `Nazor.Controls`. [Source](../../Sources/Core/Nazor.Controls/Overlays/AlertDialog.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed class AlertDialog : DialogBase` | A modal decision, such as "Delete 3 files?": the `alertdialog` role, an optional `Nazor.Controls.AlertDialog.Severity` mark, the `Nazor.Controls.AlertDialog.Title` and the `Nazor.Controls.AlertDialog.Message` (or child content), optional `Nazor.Controls.AlertDialog.Details` behind a Show details toggle, an optional "don't ask again" check box (`Nazor.Controls.AlertDialog.IsSuppressed`) and the `Nazor.Controls.AlertDialog.Actions`, usually `Nazor.Controls.DialogButton` with roles. A press outside never dismisses it; Escape runs the Cancel action and does nothing when there is none. With `Nazor.Controls.AlertDialog.PreferCancel` the Cancel action receives the focus, as destructive questions should. Parts: `AlertDialogScrim`, `AlertDialog`, `AlertDialogSeverity` (`severity`), `AlertDialogContent`, `AlertDialogTitle`, `AlertDialogMessage`, `AlertDialogDetailsToggle`, `AlertDialogDetailsToggleChevron`, `AlertDialogDetails` (a collapsible with `AlertDialogDetailsContent`), `AlertDialogSuppress` and `AlertDialogFooter`. |
| `protected override void Validate()` | Validates the severity and synchronizes the "don't ask again" state. |
| `protected override void OnOpenChanged(bool value)` | Collapses the details when the dialog opens again.<br>`value`. The new open state. |
| `protected override ElementReference? DefaultInitialFocus()` | Focuses the default action, then the first action, unless the Cancel action is preferred.<br>**Returns**. The element to focus, or null to let the container choose. |
| `protected override void BuildContent(RenderTreeBuilder builder)` | Renders the severity mark, the texts, the details, the "don't ask again" box and the actions.<br>`builder`. The builder that receives the render frames. |
| `public AlertDialog()` |  |
| `public Severity? Severity { get; set; }` | `[Parameter]`<br>Gets or sets the importance shown by the severity mark, or null for no mark. |
| `public string? Title { get; set; }` | `[Parameter]`<br>Gets or sets the title, which names the dialog. |
| `public string? Message { get; set; }` | `[Parameter]`<br>Gets or sets the message, which describes the dialog. |
| `public RenderFragment? ChildContent { get; set; }` | `[Parameter]`<br>Gets or sets rich message content, shown after `Nazor.Controls.AlertDialog.Message`. |
| `public string? Details { get; set; }` | `[Parameter]`<br>Gets or sets technical details shown behind a toggle. |
| `public RenderFragment? DetailsContent { get; set; }` | `[Parameter]`<br>Gets or sets rich details content, used instead of `Nazor.Controls.AlertDialog.Details`. |
| `public bool IsSuppressed { get; set; }` | `[Parameter]`<br>Gets or sets whether the "don't ask again" box is checked. |
| `public EventCallback<bool> IsSuppressedChanged { get; set; }` | `[Parameter]`<br>Gets or sets the callback invoked when the user checks or clears the "don't ask again" box. |
| `public string? SuppressText { get; set; }` | `[Parameter]`<br>Gets or sets the text of the "don't ask again" box; the box is shown when this is set or the box is bound. |
| `public RenderFragment? Actions { get; set; }` | `[Parameter]`<br>Gets or sets the actions, usually `Nazor.Controls.DialogButton` with roles. |
| `public bool PreferCancel { get; set; }` | `[Parameter]`<br>Gets or sets whether the Cancel action receives the focus on open. |
| `public bool Suppressed { get; }` | Gets whether the "don't ask again" box is currently checked. |
| `protected override string ElementName { get; }` | Gets the NSS element name emitted by AlertDialog. |
| `protected override string Role { get; }` | Gets the alert dialog role. |
| `protected override bool FocusCancel { get; }` | Gets whether the Cancel action receives the focus. |
| `protected override bool DismissWithoutCancel { get; }` | Gets false: an alert dialog closes only through its actions. |
| `protected override bool HasTitle { get; }` | Gets whether the dialog has a title part. |
| `protected override bool HasDescription { get; }` | Gets whether the dialog has a message part. |

### `Avatar`

Namespace `Nazor.Controls`. [Source](../../Sources/Core/Nazor.Controls/Feedback/Avatar.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed class Avatar : Control, IDisposable` | The picture of a person or an entity. It shows the `Nazor.Controls.Avatar.Source` image (`AvatarImage`); while the image loads, when it fails or without one it shows the initials (`AvatarInitials`) of `Nazor.Controls.Avatar.Name` or `Nazor.Controls.Avatar.Initials`, or else the generic `AvatarFallback` part. `Nazor.Controls.Avatar.Presence` adds the `AvatarPresence` part and `Nazor.Controls.Avatar.Badge` the `AvatarBadge` part at a corner. The element is an image named by `Nazor.Controls.Avatar.Name` and the presence ("Ada Lovelace, online"); it has the `size`, `shape` and `presence` attributes, and `Nazor.Controls.Avatar.PixelSize` sets an explicit size through `--nz-avatar-size`. Inside an `Nazor.Controls.AvatarGroup` it takes the group's size and may be hidden by the group's limit. |
| `protected override void OnInitialized()` | Joins the enclosing group. |
| `protected override void OnParametersSet()` | Validates the parameters. |
| `protected override void BuildRenderTree(RenderTreeBuilder builder)` | Renders nothing when the enclosing group hides this avatar.<br>`builder`. The builder that receives the render frames. |
| `protected override void AddAttributes(RenderTreeBuilder builder)` | Adds the image role, the name and the look attributes.<br>`builder`. The builder that receives the render frames. |
| `protected override void AddContent(RenderTreeBuilder builder)` | Adds the image, the initials or fallback, the presence and the badge.<br>`builder`. The builder that receives the render frames. |
| `public static string? InitialsOf(string? name)` | Computes up to two initials from a name: the first letters of its first and last words.<br>`name`. The name, such as "Ada Lovelace".<br>**Returns**. The initials in upper case, such as "AL", or null when the name has no letters or digits. |
| `public void Dispose()` | Leaves the enclosing group. |
| `public Avatar()` |  |
| `public ImageSource? Source { get; set; }` | `[Parameter]`<br>Gets or sets the picture, or null to show initials. |
| `public string? Name { get; set; }` | `[Parameter]`<br>Gets or sets the name of the person, used for the accessible name and the initials. |
| `public string? Initials { get; set; }` | `[Parameter]`<br>Gets or sets the initials shown instead of those computed from `Nazor.Controls.Avatar.Name`. |
| `public AvatarPresence Presence { get; set; }` | `[Parameter]`<br>Gets or sets the availability shown at a corner. |
| `public RenderFragment? Badge { get; set; }` | `[Parameter]`<br>Gets or sets content shown at the top corner, usually a standalone `Nazor.Controls.Badge`. |
| `public ControlSize Size { get; set; }` | `[Parameter]` Initial value `ControlSize.Medium`.<br>Gets or sets the size step; inside an `Nazor.Controls.AvatarGroup` the group's size is used. |
| `public double? PixelSize { get; set; }` | `[Parameter]`<br>Gets or sets an explicit width and height in logical pixels, or null to use `Nazor.Controls.Avatar.Size`. |
| `public Shape Shape { get; set; }` | `[Parameter]` Initial value `Shape.Circle`.<br>Gets or sets the outline; a circle by default, or `Nazor.Controls.Shape.Square`. |
| `public AvatarGroup? Group { get; set; }` | `[CascadingParameter]`<br>Gets or sets the enclosing group, supplied automatically. |
| `public string? DisplayInitials { get; }` | Gets the initials shown when there is no picture, or null when there are none. |
| `protected override string ElementName { get; }` | Gets the NSS element name emitted by Avatar. |
| `protected override string? ElementStyle { get; }` | Gets the explicit size variable and the caller's declarations. |

### `AvatarGroup`

Namespace `Nazor.Controls`. [Source](../../Sources/Core/Nazor.Controls/Feedback/Avatar.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed class AvatarGroup : ContentControl` | A row of overlapping `Nazor.Controls.Avatar` controls, such as the members of a conversation. Only the first `Nazor.Controls.AvatarGroup.Max` avatars are shown; the `AvatarGroupOverflow` part counts the others ("+3"). Each avatar gets the group's size and a `position` attribute (`first`, `middle`, `last`) so themes can overlap them. The element is a group named by the number of people, unless `Nazor.Controls.Control.AccessibleName` is set. |
| `protected override void OnParametersSet()` | Validates the parameters and updates the avatars after a change of limit or size. |
| `protected override void AddAttributes(RenderTreeBuilder builder)` | Adds the group role, its name and the size.<br>`builder`. The builder that receives the render frames. |
| `protected override void AddContent(RenderTreeBuilder builder)` | Adds the avatars and the overflow count.<br>`builder`. The builder that receives the render frames. |
| `public AvatarGroup()` |  |
| `public int? Max { get; set; }` | `[Parameter]`<br>Gets or sets the largest number of avatars shown, or null to show them all. |
| `public ControlSize Size { get; set; }` | `[Parameter]` Initial value `ControlSize.Medium`.<br>Gets or sets the size step of every avatar in the group. |
| `public int Count { get; }` | Gets the number of avatars in the group. |
| `public int HiddenCount { get; }` | Gets the number of avatars hidden by `Nazor.Controls.AvatarGroup.Max`. |
| `protected override string ElementName { get; }` | Gets the NSS element name emitted by AvatarGroup. |

### `AvatarPresence`

Namespace `Nazor.Controls`. [Source](../../Sources/Core/Nazor.Controls/Feedback/Avatar.cs)

| Declaration | Contract |
| --- | --- |
| `public enum AvatarPresence` | The availability of the person an `Nazor.Controls.Avatar` represents, written as the `presence` attribute. |
| `None = 0` | No availability is shown. |
| `Online = 1` | Available (`online`). |
| `Away = 2` | Away from the device (`away`). |
| `Busy = 3` | Busy or not to be disturbed (`busy`). |
| `Offline = 4` | Not connected (`offline`). |

### `Badge`

Namespace `Nazor.Controls`. [Source](../../Sources/Core/Nazor.Controls/Feedback/Badge.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed class Badge : ContentControl` | A standalone indicator or a corner decoration around ChildContent. Precedence is Dot, Count, IndicatorContent, Icon, then Text; zero Count still hides the indicator unless ShowZero is true. BadgeIndicator is the single semantic owner. Its name is explicit AccessibleName, actual uncapped localized count, new-activity text for dot/icon/custom content, or ordinary Text. |
| `protected override void OnParametersSet()` | Validates parameters and tracks changes of the actual count key. |
| `protected override void AddAttributes(RenderTreeBuilder builder)` | Adds look, placement and indicator-kind attributes.<br>`builder`. The builder that receives the render frames. |
| `protected override void AddContent(RenderTreeBuilder builder)` | Adds stable decorated content and the count/icon/custom/text indicator; changed counts replace only BadgeValue.<br>`builder`. The builder that receives the render frames. |
| `public Badge()` |  |
| `public int? Count { get; set; }` | `[Parameter]`<br>Gets or sets the number shown, or null for a dot or a text badge. |
| `public int Max { get; set; }` | `[Parameter]` Initial value `99`.<br>Gets or sets the largest number shown as is; larger counts show as "{Max}+". 99 by default. |
| `public bool ShowZero { get; set; }` | `[Parameter]`<br>Gets or sets whether a zero count still shows the indicator. |
| `public bool Dot { get; set; }` | `[Parameter]`<br>Gets or sets whether the indicator is a dot without text, such as an unread mark. |
| `public string? Text { get; set; }` | `[Parameter]`<br>Gets or sets a short text shown instead of a count, such as "New" or "Beta". |
| `public IFontSymbol? Icon { get; set; }` | `[Parameter]` Typed decorative glyph and family, rendered through Icon.Symbol in BadgeIcon when Dot, Count and IndicatorContent are absent. Takes precedence over Text. Supply AccessibleName describing the status. Active symbol validation follows Icon; an inactive symbol is not rendered. |
| `public RenderFragment? IndicatorContent { get; set; }` | `[Parameter]` Decorative content in BadgeIndicatorContent when Dot and Count are absent, before Icon/Text. The visual subtree is disabled, rejects pointer/Tab interaction and is hidden from accessibility; supply AccessibleName for the indicator owner. ChildContent continues to mean the decorated target. |
| `public Intent Intent { get; set; }` | `[Parameter]` Initial value `Intent.Accent`.<br>Gets or sets the meaning of the badge's color; accent by default. |
| `public BadgePosition Position { get; set; }` | `[Parameter]`<br>Gets or sets the corner of the decorated content at which the indicator sits. |
| `public ControlSize Size { get; set; }` | `[Parameter]` Initial value `ControlSize.Medium`.<br>Gets or sets the size step of the indicator. |
| `public string? DisplayText { get; }` | Gets visual count/text, or null for dot, typed icon or custom indicator content. A hidden zero count still has its formatted text. |
| `public bool IsIndicatorVisible { get; }` | Gets whether the indicator is shown. |
| `protected override string ElementName { get; }` | Gets the NSS element name emitted by Badge. |

Parts: BadgeContent, BadgeIndicator, BadgeValue, BadgeIcon and BadgeIndicatorContent. The root publishes indicator-kind, and the indicator publishes content-kind; values are dot/count/content/icon/text. Typed icons additionally set icon=true. BadgeValue is keyed by the raw count and carries changed=true after a count change; capped 100→101 replaces the value even though both display 99+. Unrelated parent renders preserve the value element and decorated target/editor identity and focus. The old value is removed immediately; no second lifetime or outgoing layer is retained. Basic/Fluent keep replacement immediate; NazorLeaf supplies the 350ms roll, with reduced motion applying current state immediately.

Pre-release migration: a capped badge now exposes its actual count to accessibility (150 new rather than 99+ new). Explicit AccessibleName still wins. Replace raw private-use Text plus font-family hacks with Icon; move custom indicator markup into IndicatorContent, leaving ChildContent for the decorated target. Count text now has a BadgeValue wrapper for theme selectors.

### `BadgePosition`

Namespace `Nazor.Controls`. [Source](../../Sources/Core/Nazor.Controls/Feedback/Badge.cs)

| Declaration | Contract |
| --- | --- |
| `public enum BadgePosition` | The corner of the decorated content at which a `Nazor.Controls.Badge` indicator sits. |
| `TopEnd = 0` | The top corner at the end of the reading direction (top right in left-to-right text). |
| `TopStart = 1` | The top corner at the start of the reading direction. |
| `BottomEnd = 2` | The bottom corner at the end of the reading direction. |
| `BottomStart = 3` | The bottom corner at the start of the reading direction. |

### `Banner`

Namespace `Nazor.Controls`. [Source](../../Sources/Core/Nazor.Controls/Feedback/InlineMessage.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed class Banner : ContentControl` | A message strip across the top of a window or a page about the whole application, such as being offline or an available update. The parts are `BannerMark` (a mark for the `Nazor.Controls.Banner.Severity`), `BannerMessage` (the content between the tags), the `BannerAction` button (with `Nazor.Controls.Banner.ActionText`, raising `Nazor.Controls.Banner.OnAction`) and, with `Nazor.Controls.Banner.Dismissible`, the `BannerClose` button. `Nazor.Controls.Banner.IsOpen` supports `@bind-IsOpen`. Like `Nazor.Controls.InlineMessage` it is announced when shown, errors at once. |
| `protected override void OnParametersSet()` | Validates the severity and synchronizes the open state. |
| `protected override void BuildRenderTree(RenderTreeBuilder builder)` | Renders the banner only while it is open.<br>`builder`. The builder that receives the render frames. |
| `protected override void AddAttributes(RenderTreeBuilder builder)` | Adds the status role, the severity and the live politeness.<br>`builder`. The builder that receives the render frames. |
| `protected override void AddContent(RenderTreeBuilder builder)` | Adds the mark, the message, the action and the close button.<br>`builder`. The builder that receives the render frames. |
| `public Task CloseAsync()` | Hides the banner as the close button does and reports the change.<br>**Returns**. A task that completes when the change has been reported. |
| `public Banner()` |  |
| `public Severity Severity { get; set; }` | `[Parameter]`<br>Gets or sets the importance of the message. |
| `public string? ActionText { get; set; }` | `[Parameter]`<br>Gets or sets the text of the action button, or null for none. |
| `public EventCallback OnAction { get; set; }` | `[Parameter]`<br>Gets or sets the callback invoked when the action button is pressed. |
| `public bool Dismissible { get; set; }` | `[Parameter]` Initial value `true`.<br>Gets or sets whether a close button is shown. |
| `public bool IsOpen { get; set; }` | `[Parameter]` Initial value `true`.<br>Gets or sets whether the banner is shown; true by default. |
| `public EventCallback<bool> IsOpenChanged { get; set; }` | `[Parameter]`<br>Gets or sets the callback invoked when the close button hides the banner. |
| `public EventCallback OnClose { get; set; }` | `[Parameter]`<br>Gets or sets the callback invoked after the close button hides the banner. |
| `public bool Opened { get; }` | Gets whether the banner is currently shown. |
| `protected override string ElementName { get; }` | Gets the NSS element name emitted by Banner. |

### `Blockquote`

Namespace `Nazor.Controls`. [Source](../../Sources/Core/Nazor.Controls/Text/Paragraph.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed class Blockquote : ContentControl` | A passage quoted from another source, optionally followed by its source (`Nazor.Controls.Blockquote.Cite` or `Nazor.Controls.Blockquote.CiteContent`). The element is `Blockquote`, a group described as a quote for assistive technologies; the parts are `BlockquoteContent` and `BlockquoteCite`. |
| `protected override void AddAttributes(RenderTreeBuilder builder)` | Adds the group role described as a quote.<br>`builder`. The builder that receives the render frames. |
| `protected override void AddContent(RenderTreeBuilder builder)` | Adds the quoted content and the source.<br>`builder`. The builder that receives the render frames. |
| `public Blockquote()` |  |
| `public string? Cite { get; set; }` | `[Parameter]`<br>Gets or sets the source of the quotation as plain text, such as an author or a title. |
| `public RenderFragment? CiteContent { get; set; }` | `[Parameter]`<br>Gets or sets the source of the quotation as content; it replaces `Nazor.Controls.Blockquote.Cite`. |
| `protected override string ElementName { get; }` | Gets the NSS element name emitted by Blockquote. |

### `Button`

Namespace `Nazor.Controls`. [Source](../../Sources/Core/Nazor.Controls/Actions/Button.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed class Button : ButtonBase` | A pressable control that runs an action: `Nazor.Controls.ButtonBase.OnClick`, a `Nazor.Controls.ButtonBase.Command`, or the submission or reset of its nearest `Nazor.Controls.Form` (`Nazor.Controls.Button.Type`). `Nazor.Controls.Button.IsDefault` makes it the button pressed by Enter in its window. With `Nazor.Controls.Button.RepeatInterval` the action repeats while the pointer holds it. |
| `protected override void OnParametersSet()` | Validates the parameters. |
| `protected override void AddAttributes(RenderTreeBuilder builder)` | Adds the button role, the default state and the activation.<br>`builder`. The builder that receives the render frames. |
| `protected override void OnDispose()` | Stops the repetition timer. |
| `public Button()` |  |
| `public ButtonType Type { get; set; }` | `[Parameter]`<br>Gets or sets the action requested in the nearest form. |
| `public bool IsDefault { get; set; }` | `[Parameter]`<br>Gets or sets whether this is the default button of the window: Enter presses it when the focused control does not use the key. Themes usually draw it with the accent appearance. |
| `public TimeSpan? RepeatInterval { get; set; }` | `[Parameter]`<br>Gets or sets the interval between repetitions while the pointer holds the button, or null for none. |
| `protected override string ElementName { get; }` | Gets the NSS element name emitted by Button. |

| Declaration | Contract |
| --- | --- |
| `public TimeSpan RepeatDelay { get; set; }` | `[Parameter]` 400ms by default. Initial action runs on press; this delay precedes the first repeated action. |
| `public TimeProvider Time { get; set; }` | `[CascadingParameter]` `TimeProvider.System` by default. Replacing the provider cancels the current hold. |

`RepeatInterval` now covers primary pointer and focused unmodified Enter/Space holds. Repetition suppresses release activation, rejects secondary pointers and stops on leave, capture/focus loss, focused Escape, disabled/busy/ancestor-disabled state or disposal. Ordinary interval-null activation stays click-based. Timer work dispatches through the component UI context; queued stale work is rejected. Intervals are positive and delays nonnegative, bounded by the timer-supported duration range. The repeat path invokes the action/command rather than form submission/reset.

### `ButtonBase`

Namespace `Nazor.Controls`. [Source](../../Sources/Core/Nazor.Controls/Actions/Button.cs)

| Declaration | Contract |
| --- | --- |
| `public abstract class ButtonBase : ContentControl, IDisposable, IButtonGroupMember` | The shared look and content of `Nazor.Controls.Button` and `Nazor.Controls.ToggleButton`: appearance, intent, size and shape, optional leading and trailing content around the label, a busy state and an optional `Nazor.Controls.ButtonBase.Command`. The parts are `ButtonLeading`, `ButtonLabel`, `ButtonTrailing` and, while busy, `ButtonSpinner`; a button without a label has the `icon-only` attribute. Inside a `Nazor.Controls.ButtonGroup` the button carries the `position` attribute (`first`, `middle`, `last` or `only`) and takes the group's appearance and size when it leaves its own at the default. |
| `protected override void OnParametersSet()` | Validates the parameters and follows the command's changes. |
| `protected override void OnAfterRender(bool firstRender)` | Lets the enclosing group place the button once it is rendered.<br>`firstRender`. Whether this is the first render. |
| `protected override void AddAttributes(RenderTreeBuilder builder)` | Adds the shared look attributes.<br>`builder`. The builder that receives the render frames. |
| `protected override void AddContent(RenderTreeBuilder builder)` | Adds the leading content, the label, the trailing content and the busy spinner.<br>`builder`. The builder that receives the render frames. |
| `protected Task InvokeActionAsync(PointerEventArgs e)` | Runs the click callback and the command.<br>`e`. The activation event.<br>**Returns**. A task that completes when the action has finished. |
| `public void Dispose()` | Stops following the command and releases timers. |
| `protected virtual void OnDispose()` | Releases resources owned by a derived button. |
| `protected ButtonBase()` |  |
| `public ControlAppearance Appearance { get; set; }` | `[Parameter]`<br>Gets or sets how strongly the button is filled. |
| `public Intent Intent { get; set; }` | `[Parameter]`<br>Gets or sets the meaning of the button's color. |
| `public ControlSize Size { get; set; }` | `[Parameter]` Initial value `ControlSize.Medium`.<br>Gets or sets the size step. |
| `public Shape Shape { get; set; }` | `[Parameter]`<br>Gets or sets the outline. |
| `public RenderFragment? Leading { get; set; }` | `[Parameter]`<br>Gets or sets content shown before the label, usually an `Nazor.Controls.Icon`. |
| `public RenderFragment? Trailing { get; set; }` | `[Parameter]`<br>Gets or sets content shown after the label, such as a chevron or a badge. |
| `public bool IsBusy { get; set; }` | `[Parameter]`<br>Gets or sets whether the button shows work in progress; it then rejects activation. |
| `public bool Disabled { get; set; }` | `[Parameter]`<br>Gets or sets whether the button rejects interaction. |
| `public Command? Command { get; set; }` | `[Parameter]`<br>Gets or sets a command that supplies the label (when there is no content), the tooltip with its shortcut, the enabled state and the action. |
| `public EventCallback<PointerEventArgs> OnClick { get; set; }` | `[Parameter]`<br>Gets or sets the callback invoked when the button is activated. |
| `protected bool IsInactive { get; }` | Gets whether the button currently rejects activation. |
| `protected virtual bool HasLabel { get; }` | Gets whether the button shows a label part: it has content or a command. |
| `protected override string? ElementStyle { get; }` | Publishes the latest press origin in local logical coordinates for theme effects. Synthetic keyboard/accessibility activation and unavailable conversion support use the center. Caller Style declarations remain last. |
| `protected string? EffectiveTooltip { get; }` | Gets the tooltip, falling back to the command's description and shortcut. |

### `ButtonGroup`

Namespace `Nazor.Controls`. [Source](../../Sources/Core/Nazor.Controls/Actions/ButtonGroup.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed class ButtonGroup : ContentControl` | Joins related buttons into one visual unit, such as the alignment buttons of an editor: every `Nazor.Controls.Button`, `Nazor.Controls.ToggleButton`, `Nazor.Controls.MenuButton` and `Nazor.Controls.SplitButton` inside it gets the `position` attribute (`first`, `middle`, `last` or `only`) in the order they appear, so themes can join their borders, and takes the group's `Nazor.Controls.ButtonGroup.Appearance` and `Nazor.Controls.ButtonGroup.Size` when it leaves its own at the default. The element has the `group` role and the `orientation` attribute; `Nazor.Controls.ButtonGroup.Disabled` disables every member. Each button stays a Tab stop; use a `Nazor.Controls.Toolbar` for a single Tab stop with arrow keys. |
| `protected override void OnParametersSet()` | Validates the parameters. |
| `protected override void AddAttributes(RenderTreeBuilder builder)` | Adds the group role, the orientation, the shared look and the disabled state.<br>`builder`. The builder that receives the render frames. |
| `protected override void AddContent(RenderTreeBuilder builder)` | Supplies the group to its buttons.<br>`builder`. The builder that receives the render frames. |
| `protected override void OnAfterRender(bool firstRender)` | Places the members after every render, as buttons may have been added, removed or moved.<br>`firstRender`. Whether this is the first render. |
| `public ButtonGroup()` |  |
| `public Orientation Orientation { get; set; }` | `[Parameter]`<br>Gets or sets the direction in which the buttons are joined. |
| `public ControlAppearance Appearance { get; set; }` | `[Parameter]`<br>Gets or sets the appearance of the members that keep the standard appearance. |
| `public ControlSize Size { get; set; }` | `[Parameter]` Initial value `ControlSize.Medium`.<br>Gets or sets the size of the members that keep the medium size. |
| `public bool Disabled { get; set; }` | `[Parameter]`<br>Gets or sets whether every member rejects interaction. |
| `protected override string ElementName { get; }` | Gets the NSS element name emitted by ButtonGroup. |

### `ButtonType`

Namespace `Nazor.Controls`. [Source](../../Sources/Core/Nazor.Controls/Actions/Button.cs)

| Declaration | Contract |
| --- | --- |
| `public enum ButtonType` | Configures the action a `Nazor.Controls.ButtonType.Button` requests from its nearest `Nazor.Controls.Form`. |
| `Button = 0` | Invokes only the button action. |
| `Submit = 1` | Requests submission of the nearest form. |
| `Reset = 2` | Requests the application-owned reset of the nearest form. |

### `Calendar`

Namespace `Nazor.Controls`. [Source](../../Sources/Core/Nazor.Controls/Pickers/Calendar.cs)

`CalendarTransition`/`CalendarTransitionLayer` retain an immutable outgoing grid snapshot for paging and zoom; `ActiveElement` excludes disabled outgoing items. The header remains mounted and alternates `motion-cycle=odd/even` for theme replay.

| Declaration | Contract |
| --- | --- |
| `public sealed class Calendar : Control` | A month calendar built on `Nazor.Controls.Primitives.CalendarModel`. It selects one date (`Nazor.Controls.Calendar.Value`), a range (`Nazor.Controls.Calendar.RangeStart` and `Nazor.Controls.Calendar.RangeEnd`) or a set of dates (`Nazor.Controls.Calendar.SelectedDates`) according to `Nazor.Controls.Calendar.SelectionMode`, all bindable. The header has `CalendarTitle` (which zooms out to the months of the year and the years of the decade), `CalendarPrevious` and `CalendarNext` in keyboard order; `Nazor.Controls.Calendar.MonthCount` months are shown side by side in `CalendarMonths`, each a `CalendarMonth` with `CalendarMonthTitle` (when there are several), `CalendarWeekdays` (`CalendarWeekHeader`, `CalendarWeekday`, with `CalendarWeekdayAbbreviated` and `CalendarWeekdayShortest` localized label parts) and a `CalendarGrid` of `CalendarWeek` rows (`CalendarWeekNumber`, `CalendarDay`). The year and decade views show a `CalendarGrid` of `CalendarRow` rows of `CalendarItem`. Each grid has the `listbox` role, named by its month, and is a single Tab stop on the focused date: the arrows move by day and week (by month or year in the other views), Page Up and Page Down by month (by year with Shift), Home and End to the ends of the week (of the month with Ctrl), Enter or Space select. Days are `option`s named by their long date, with `position-in-set` and `size-of-set` in the month, and carry the attributes `today`, `selected`, `range-start`, `range-middle`, `range-end`, `range-preview` (the dates between a range start and the focused date while the end is being chosen), `outside` (adjacent months), `unavailable`, `weekend` and `current` (the focused date). Names of months and days, the first day of the week and the week numbers come from `Nazor.Controls.Calendar.Culture`. |
| `protected override void OnParametersSet()` | Validates the parameters, configures the model and synchronizes the selection and the view. |
| `protected override void AddAttributes(RenderTreeBuilder builder)` | Adds the calendar role and its states.<br>`builder`. The builder that receives the render frames. |
| `protected override void AddContent(RenderTreeBuilder builder)` | Adds the header and the grid of the current view.<br>`builder`. The builder that receives the render frames. |
| `protected override Task OnAfterRenderAsync(bool firstRender)` | Moves the focus to the focused date after keyboard navigation.<br>`firstRender`. Whether this is the first render.<br>**Returns**. A task that completes when the focus has moved. |
| `public Task FocusActiveAsync()` | Moves the keyboard focus to the focused date.<br>**Returns**. A task that completes when the focus has moved. |
| `public Task GoToAsync(DateOnly date)` | Shows the month of a date and focuses it, as keyboard navigation would.<br>`date`. The date to show; it is clamped to the limits.<br>**Returns**. A task that completes when the change has been reported. |
| `public Task ChooseDayAsync(DateOnly date)` | Selects a date as a click on its day would, according to the selection mode.<br>`date`. The date; unavailable dates are ignored.<br>**Returns**. A task that completes when the change has been reported. |
| `public Calendar()` |  |
| `public CalendarSelectionMode SelectionMode { get; set; }` | `[Parameter]`<br>Gets or sets how many dates can be selected. |
| `public DateOnly? Value { get; set; }` | `[Parameter]`<br>Gets or sets the selected date in single selection. |
| `public EventCallback<DateOnly?> ValueChanged { get; set; }` | `[Parameter]`<br>Gets or sets the callback invoked when the user selects a date in single selection. |
| `public DateOnly? RangeStart { get; set; }` | `[Parameter]`<br>Gets or sets the first date of the range in range selection. |
| `public EventCallback<DateOnly?> RangeStartChanged { get; set; }` | `[Parameter]`<br>Gets or sets the callback invoked when the user changes the start of the range. |
| `public DateOnly? RangeEnd { get; set; }` | `[Parameter]`<br>Gets or sets the last date of the range in range selection; null while it is being chosen. |
| `public EventCallback<DateOnly?> RangeEndChanged { get; set; }` | `[Parameter]`<br>Gets or sets the callback invoked when the user changes the end of the range. |
| `public IReadOnlyCollection<DateOnly>? SelectedDates { get; set; }` | `[Parameter]`<br>Gets or sets the selected dates in multiple selection. |
| `public EventCallback<IReadOnlyCollection<DateOnly>> SelectedDatesChanged { get; set; }` | `[Parameter]`<br>Gets or sets the callback invoked with the new sorted set when the user toggles a date in multiple selection. |
| `public EventCallback<DateOnly> OnDateSelected { get; set; }` | `[Parameter]`<br>Gets or sets the callback invoked whenever the user picks an available day, in every selection mode. |
| `public DateOnly? Minimum { get; set; }` | `[Parameter]`<br>Gets or sets the earliest available date, or null for no limit. |
| `public DateOnly? Maximum { get; set; }` | `[Parameter]`<br>Gets or sets the latest available date, or null for no limit. |
| `public Func<DateOnly, bool>? IsDateUnavailable { get; set; }` | `[Parameter]`<br>Gets or sets a predicate marking dates that cannot be selected, such as holidays; they stay focusable. |
| `public CultureInfo? Culture { get; set; }` | `[Parameter]`<br>Gets or sets the culture of the names, the first day of the week and the week numbers; the current culture by default. |
| `public DayOfWeek? FirstDayOfWeek { get; set; }` | `[Parameter]`<br>Gets or sets the first day of the week; null uses the culture's. |
| `public bool ShowWeekNumbers { get; set; }` | `[Parameter]`<br>Gets or sets whether a column shows the week numbers. |
| `public bool ShowOutsideDays { get; set; }` | `[Parameter]` Initial value `true`.<br>Gets or sets whether the days of the adjacent months fill the first and last weeks; always false with several months. |
| `public DateOnly? Today { get; set; }` | `[Parameter]`<br>Gets or sets the date marked as today; null uses the computer's date. Tests and previews set it. |
| `public int MonthCount { get; set; }` | `[Parameter]` Initial value `1`.<br>Gets or sets how many months are shown side by side, 1 or 2. |
| `public CalendarViewMode View { get; set; }` | `[Parameter]`<br>Gets or sets what the calendar shows. |
| `public EventCallback<CalendarViewMode> ViewChanged { get; set; }` | `[Parameter]`<br>Gets or sets the callback invoked when the user zooms in or out. |
| `public DateOnly? DisplayMonth { get; set; }` | `[Parameter]`<br>Gets or sets any day of the first month shown; null follows the selection. |
| `public EventCallback<DateOnly?> DisplayMonthChanged { get; set; }` | `[Parameter]`<br>Gets or sets the callback invoked with the first day of the first month shown when the user navigates. |
| `public bool Disabled { get; set; }` | `[Parameter]`<br>Gets or sets whether the calendar rejects interaction. |
| `public bool ReadOnly { get; set; }` | `[Parameter]`<br>Gets or sets whether the selection cannot change while navigation still works. |
| `public CalendarViewMode CurrentView { get; }` | Gets what the calendar shows. |
| `public DateOnly ShownMonth { get; }` | Gets the first day of the first month shown. |
| `public DateOnly ActiveDate { get; }` | Gets the focused date, which holds the Tab stop. |
| `public CalendarModel Model { get; }` | Gets the date arithmetic in use. |
| `public ElementReference ActiveElement { get; }` | Gets the element of the focused date, or the calendar itself before the first render. |
| `protected override string ElementName { get; }` | Gets the NSS element name emitted by Calendar. |
| `protected override string? ElementStyle { get; }` | Supplies month-count/week-column geometry to themes before application style overrides. |

Header visual order is theme-owned; keyboard order is title/previous/next, skipping disabled controls. Weekday labels use `Culture.DateTimeFormat.AbbreviatedDayNames` and `ShortestDayNames`. A theme displays one label part and hides the other. The weekday band remains semantically hidden. [Calendar usage and theme migration](../Guide/10-Text-Input-and-Data-Controls.md#choose-dates)

### `CalendarSelectionMode`

Namespace `Nazor.Controls`. [Source](../../Sources/Core/Nazor.Controls/Pickers/Calendar.cs)

| Declaration | Contract |
| --- | --- |
| `public enum CalendarSelectionMode` | How many dates a `Nazor.Controls.Calendar` selects, written as its `selection-mode` attribute. |
| `Single = 0` | One date, in `Nazor.Controls.Calendar.Value`. |
| `Range = 1` | A range of consecutive dates, in `Nazor.Controls.Calendar.RangeStart` and `Nazor.Controls.Calendar.RangeEnd`. |
| `Multiple = 2` | Any set of dates, in `Nazor.Controls.Calendar.SelectedDates`. |
| `None = 3` | No selection: the calendar only shows and navigates dates. |

### `CalendarViewMode`

Namespace `Nazor.Controls`. [Source](../../Sources/Core/Nazor.Controls/Pickers/Calendar.cs)

| Declaration | Contract |
| --- | --- |
| `public enum CalendarViewMode` | What a `Nazor.Controls.Calendar` shows, written as its `view` attribute. |
| `Month = 0` | The days of one or more months. |
| `Year = 1` | The twelve months of a year. |
| `Decade = 2` | The years of a decade, with the last year of the previous decade and the first of the next. |

### `CaptionButton`

Namespace `Nazor.Controls`. [Source](../../Sources/Core/Nazor.Controls/Frame/CaptionButtons.cs)

| Declaration | Contract |
| --- | --- |
| `public enum CaptionButton` | A button of `Nazor.Controls.CaptionButtons`. |
| `Help = 0` | Shows help; it runs `Nazor.Controls.CaptionButtons.OnHelp`. |
| `Minimize = 1` | Minimizes the window. |
| `Maximize = 2` | Maximizes the window, or restores it when it is maximized. |
| `Close = 3` | Closes the window. |

### `CaptionButtons`

Namespace `Nazor.Controls`. [Source](../../Sources/Core/Nazor.Controls/Frame/CaptionButtons.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed class CaptionButtons : Control` | Minimize, maximize (restore while maximized) and close buttons drawn by Nazor for a custom `Nazor.Controls.TitleBar`, plus an optional help button, in the order of `Nazor.Controls.CaptionButtons.Buttons`. They follow the window through `Nazor.Controls.WindowCommands`: minimize and maximize are disabled when the window does not allow them, the maximize button shows the restore glyph and name while the window is maximized and, on Windows, is a native Snap Layouts area. The element `CaptionButtons` (`group` role, `maximized` attribute) contains one `CaptionButtonsButton` per button, with the `kind` attribute (`help`, `minimize`, `maximize`, `restore` or `close`) and a `CaptionButtonsGlyph` drawn by the theme. The buttons have localized accessible names and tooltips and, like native caption buttons, are not Tab stops unless `Nazor.Controls.CaptionButtons.IsTabStop` is set. While the window shows the system's own buttons they are hidden, unless `Nazor.Controls.CaptionButtons.HideWithSystemButtons` is false. |
| `public static IReadOnlyList<CaptionButton> FromLayout(IReadOnlyList<WindowButton> buttons)` | Converts the system's caption layout of one side into buttons, to follow the platform convention.<br>`buttons`. The system buttons of one side, such as `WindowCommands.Layout.Right`.<br>**Returns**. The same buttons in the same order. |
| `protected override void OnParametersSet()` | Validates the buttons. |
| `protected override void BuildRenderTree(RenderTreeBuilder builder)` | Renders the buttons inside a `Nazor.Controls.WindowCommands` that follows the window.<br>`builder`. The builder that receives the render frames. |
| `protected override void AddAttributes(RenderTreeBuilder builder)` | Adds the group role, its name and the window state.<br>`builder`. The builder that receives the render frames. |
| `protected override void AddContent(RenderTreeBuilder builder)` | Adds the buttons.<br>`builder`. The builder that receives the render frames. |
| `public CaptionButtons()` |  |
| `public IReadOnlyList<CaptionButton>? Buttons { get; set; }` | `[Parameter]`<br>Gets or sets the buttons in physical left-to-right order; minimize, maximize and close by default. |
| `public EventCallback<PointerEventArgs> OnHelp { get; set; }` | `[Parameter]`<br>Gets or sets the callback invoked by the help button. |
| `public bool HideWithSystemButtons { get; set; }` | `[Parameter]` Initial value `true`.<br>Gets or sets whether the buttons are hidden while the window shows the system caption buttons; true by default. |
| `public bool IsTabStop { get; set; }` | `[Parameter]`<br>Gets or sets whether the buttons are reached with Tab; false by default, as native caption buttons. |
| `protected override string ElementName { get; }` | Gets the NSS element name emitted by CaptionButtons. |

### `Card`

Namespace `Nazor.Controls`. [Source](../../Sources/Core/Nazor.Controls/Layout/Card.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed class Card : ContentControl` | A surface that groups the content about one subject, such as a product, a contact or a document. The optional `CardPreview` (a large image above the rest) is followed by the `CardHeader`, with the `CardMedia` slot (an avatar or an icon), the `CardTitle` and `CardSubtitle` lines and the `CardActions` slot; then the `CardBody` with the content between the tags and the `CardFooter`. A card is a group named by its title; a clickable card (`Nazor.Controls.Card.IsClickable` or `Nazor.Controls.Card.OnClick`) is a button activated by a click, Enter or Space, while buttons inside it keep their own actions. The element has the `appearance`, `orientation`, `clickable` and `selected` attributes. |
| `protected override void OnParametersSet()` | Validates the appearance and the orientation. |
| `protected override void AddAttributes(RenderTreeBuilder builder)` | Adds the role, the name, the look attributes and the activation.<br>`builder`. The builder that receives the render frames. |
| `protected override void AddContent(RenderTreeBuilder builder)` | Adds the preview, the header, the body and the footer.<br>`builder`. The builder that receives the render frames. |
| `public Card()` |  |
| `public RenderFragment? Preview { get; set; }` | `[Parameter]`<br>Gets or sets a large image or other content shown above the header. |
| `public RenderFragment? Media { get; set; }` | `[Parameter]`<br>Gets or sets a small image, avatar or icon shown at the start of the header. |
| `public string? Title { get; set; }` | `[Parameter]`<br>Gets or sets the title as plain text; it names the card. |
| `public RenderFragment? TitleContent { get; set; }` | `[Parameter]`<br>Gets or sets the title as content; it replaces `Nazor.Controls.Card.Title`. |
| `public string? Subtitle { get; set; }` | `[Parameter]`<br>Gets or sets a secondary line below the title as plain text; it describes the card. |
| `public RenderFragment? SubtitleContent { get; set; }` | `[Parameter]`<br>Gets or sets the secondary line as content; it replaces `Nazor.Controls.Card.Subtitle`. |
| `public RenderFragment? Actions { get; set; }` | `[Parameter]`<br>Gets or sets commands shown at the end of the header, such as a menu button. |
| `public RenderFragment? Footer { get; set; }` | `[Parameter]`<br>Gets or sets content shown at the bottom of the card, usually buttons. |
| `public CardAppearance Appearance { get; set; }` | `[Parameter]`<br>Gets or sets how strongly the card stands out. |
| `public Orientation Orientation { get; set; }` | `[Parameter]` Initial value `Orientation.Vertical`.<br>Gets or sets whether the preview is above the content (vertical) or beside it (horizontal). |
| `public bool IsClickable { get; set; }` | `[Parameter]`<br>Gets or sets whether the whole card is pressable; an `Nazor.Controls.Card.OnClick` callback also makes it so. |
| `public bool IsSelected { get; set; }` | `[Parameter]`<br>Gets or sets whether the card is drawn as selected. |
| `public bool Disabled { get; set; }` | `[Parameter]`<br>Gets or sets whether the card and its content reject interaction. |
| `public EventCallback<PointerEventArgs> OnClick { get; set; }` | `[Parameter]`<br>Gets or sets the callback invoked when a clickable card is activated. |
| `public bool Clickable { get; }` | Gets whether the card is pressable. |
| `protected override string ElementName { get; }` | Gets the NSS element name emitted by Card. |

### `CardAppearance`

Namespace `Nazor.Controls`. [Source](../../Sources/Core/Nazor.Controls/Layout/Card.cs)

| Declaration | Contract |
| --- | --- |
| `public enum CardAppearance` | How strongly a `Nazor.Controls.Card` stands out from the surface, written as the `appearance` attribute. |
| `Standard = 0` | The ordinary filled card. |
| `Outline = 1` | Only an outline, on the color of the surface. |
| `Subtle = 2` | No fill or outline until hovered or selected, for dense grids of cards. |

### `CheckBox`

Namespace `Nazor.Controls`. [Source](../../Sources/Core/Nazor.Controls/Selection/CheckBox.cs)

| Declaration | Contract |
| --- | --- |
| `public class CheckBox : ContentControl` | An independent yes-or-no choice with a label. `Nazor.Controls.CheckBox.IsChecked` supports `@bind-IsChecked`; `Nazor.Controls.CheckBox.IsIndeterminate` shows a mixed state, such as a parent of partly checked children, which activation resolves to checked. The parts are `CheckBoxMark` with persistent `CheckBoxCheck` and `CheckBoxMixed` vector glyphs, `CheckBoxLabel` and `CheckBoxDescription`. Themes reveal the glyphs with stroke trimming; checked and mixed semantics change immediately while visual transitions can finish or reverse independently. |
| `protected override void OnParametersSet()` | Synchronizes the states with the parameters. |
| `protected override void AddAttributes(RenderTreeBuilder builder)` | Adds the role, the states and the activation.<br>`builder`. The builder that receives the render frames. |
| `protected override void AddContent(RenderTreeBuilder builder)` | Adds the mark, the label and the description.<br>`builder`. The builder that receives the render frames. |
| `public virtual Task ToggleAsync()` | Changes the checked state as a user activation does and reports the change.<br>**Returns**. A task that completes when the change has been reported. |
| `public CheckBox()` |  |
| `public bool IsChecked { get; set; }` | `[Parameter]`<br>Gets or sets whether the box is checked. |
| `public EventCallback<bool> IsCheckedChanged { get; set; }` | `[Parameter]`<br>Gets or sets the callback invoked when the user changes the checked state. |
| `public bool IsIndeterminate { get; set; }` | `[Parameter]`<br>Gets or sets whether the box shows the mixed state. |
| `public EventCallback<bool> IsIndeterminateChanged { get; set; }` | `[Parameter]`<br>Gets or sets the callback invoked when activation clears the mixed state. |
| `public RenderFragment? Description { get; set; }` | `[Parameter]`<br>Gets or sets a secondary line shown below the label. |
| `public bool Disabled { get; set; }` | `[Parameter]`<br>Gets or sets whether the control rejects interaction. |
| `public ControlSize Size { get; set; }` | `[Parameter]` Initial value `ControlSize.Medium`.<br>Gets or sets the size step. |
| `public FieldContext? Field { get; set; }` | `[CascadingParameter]`<br>Gets or sets the enclosing field, supplied automatically. |
| `public bool Checked { get; }` | Gets whether the box is currently checked. |
| `public bool Indeterminate { get; }` | Gets whether the box currently shows the mixed state. |
| `protected virtual string Role { get; }` | Gets the accessible role; derived controls such as menu items replace it. |
| `protected override string ElementName { get; }` | Gets the NSS element name emitted by CheckBox. |
| `protected bool IsInactive { get; }` | Gets whether the control rejects interaction, including through its field. |

### `Chip`

Namespace `Nazor.Controls`. [Source](../../Sources/Core/Nazor.Controls/Selection/Chip.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed class Chip : ContentControl, IDisposable` | A compact element that represents a filter, a choice, an entry or an action (`Nazor.Controls.Chip.Kind`). A filter chip keeps `Nazor.Controls.Chip.IsSelected` (`@bind-IsSelected`) when it stands alone; inside a `Nazor.Controls.ChipGroup<TValue>` with a selection mode the group owns the selection through `Nazor.Controls.Chip.Value`. An input chip calls `Nazor.Controls.Chip.OnRemove` from its remove button or with Backspace or Delete. The element is `Chip` with the `kind`, `selected`, `intent`, `size` and `removable` attributes; the parts are `ChipDot` (`Nazor.Controls.Chip.ShowDot`) or `ChipLeading`, `ChipCheck`, `ChipLabel` and `ChipRemove`. |
| `protected override void OnParametersSet()` | Validates the parameters, synchronizes the state and joins the enclosing group. |
| `protected override void AddAttributes(RenderTreeBuilder builder)` | Adds the role, the states and the keyboard and pointer handling.<br>`builder`. The builder that receives the render frames. |
| `protected override void AddContent(RenderTreeBuilder builder)` | Adds the dot or the leading content, the check, the label and the remove button.<br>`builder`. The builder that receives the render frames. |
| `protected override void OnAfterRender(bool firstRender)` | Renders the remove button's name once the label is known.<br>`firstRender`. Whether this is the first render. |
| `public Task ActivateAsync(PointerEventArgs e)` | Activates the chip as a click does: changes its selection when it selects, then runs `Nazor.Controls.Chip.OnClick`.<br>`e`. The activation event.<br>**Returns**. A task that completes when the action has finished. |
| `public Task RemoveAsync()` | Removes an input chip as its remove button does: moves the focus to a neighbor when the chip had it and runs `Nazor.Controls.Chip.OnRemove`.<br>**Returns**. A task that completes when the callback has finished. |
| `public void Dispose()` | Leaves the enclosing group. |
| `public Chip()` |  |
| `public ChipKind? Kind { get; set; }` | `[Parameter]`<br>Gets or sets what the chip does; null takes it from the enclosing group: `Nazor.Controls.ChipKind.Choice` in single selection, `Nazor.Controls.ChipKind.Filter` in multiple selection, `Nazor.Controls.ChipKind.Action` otherwise. |
| `public object? Value { get; set; }` | `[Parameter]`<br>Gets or sets the value the chip stands for in a `Nazor.Controls.ChipGroup<TValue>`; it must be of the group's value type. |
| `public bool IsSelected { get; set; }` | `[Parameter]`<br>Gets or sets whether a filter chip outside a selecting group is selected. |
| `public EventCallback<bool> IsSelectedChanged { get; set; }` | `[Parameter]`<br>Gets or sets the callback invoked when the user selects or clears a filter chip outside a selecting group. |
| `public RenderFragment? Leading { get; set; }` | `[Parameter]`<br>Gets or sets content shown before the label, such as an avatar or an icon. |
| `public bool ShowDot { get; set; }` | `[Parameter]`<br>Gets or sets whether a colored dot is shown before the label, in the color of `Nazor.Controls.Chip.Intent`. |
| `public Intent Intent { get; set; }` | `[Parameter]`<br>Gets or sets the meaning of the chip's color. |
| `public ControlSize? Size { get; set; }` | `[Parameter]`<br>Gets or sets the size step; null uses the group's. |
| `public bool Disabled { get; set; }` | `[Parameter]`<br>Gets or sets whether the chip rejects interaction. |
| `public EventCallback<PointerEventArgs> OnClick { get; set; }` | `[Parameter]`<br>Gets or sets the callback invoked when the chip is activated, after any change of selection. |
| `public EventCallback OnRemove { get; set; }` | `[Parameter]`<br>Gets or sets the callback invoked when the user removes an input chip; the application removes it. |
| `public ChipKind EffectiveKind { get; }` | Gets the kind in effect, explicit or taken from the group. |
| `public bool Selected { get; }` | Gets whether the chip is currently selected. |
| `protected override string ElementName { get; }` | Gets the NSS element name emitted by Chip. |

### `ChipGroup<TValue>`

Namespace `Nazor.Controls`. [Source](../../Sources/Core/Nazor.Controls/Selection/Chip.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed class ChipGroup<TValue> : ContentControl, IChipOwner` | A wrapping set of `Nazor.Controls.Chip` controls with a single Tab stop: the arrow keys move between chips, Home and End jump to the ends. With `Nazor.Controls.Primitives.SelectionMode.Single` it is a `radiogroup` of choice chips whose `Nazor.Controls.ChipGroup<TValue>.Value` (`@bind-Value`) is the selected chip's value, and the arrow keys also select; with `Nazor.Controls.Primitives.SelectionMode.Multiple` its filter chips toggle the values in `Nazor.Controls.ChipGroup<TValue>.Values` (`@bind-Values`). `Nazor.Controls.ChipGroup<TValue>.MaxVisible` shows only the first chips and a `ChipGroupOverflow` button ("+N") that reveals the others. The element is `ChipGroup` with the `selection-mode` and `size` attributes.<br>`TValue`. The type of the chip values. |
| `protected override void OnParametersSet()` | Validates the parameters and synchronizes the selection. |
| `protected override void AddAttributes(RenderTreeBuilder builder)` | Adds the role and the keyboard handling.<br>`builder`. The builder that receives the render frames. |
| `protected override void AddContent(RenderTreeBuilder builder)` | Adds the chips and the overflow button.<br>`builder`. The builder that receives the render frames. |
| `protected override void OnAfterRender(bool firstRender)` | Refreshes the chips and the overflow button after the set of chips or the selection changed.<br>`firstRender`. Whether this is the first render. |
| `public void SetExpanded(bool value)` | Shows or hides the chips beyond `Nazor.Controls.ChipGroup<TValue>.MaxVisible`.<br>`value`. Whether every chip is shown. |
| `public ChipGroup()` |  |
| `public SelectionMode SelectionMode { get; set; }` | `[Parameter]`<br>Gets or sets whether the chips select nothing, one value or several values. |
| `public TValue? Value { get; set; }` | `[Parameter]`<br>Gets or sets the value of the selected chip in single selection. |
| `public EventCallback<TValue?> ValueChanged { get; set; }` | `[Parameter]`<br>Gets or sets the callback invoked when the user selects another chip in single selection. |
| `public IReadOnlyList<TValue>? Values { get; set; }` | `[Parameter]`<br>Gets or sets the values of the selected chips in multiple selection. |
| `public EventCallback<IReadOnlyList<TValue>> ValuesChanged { get; set; }` | `[Parameter]`<br>Gets or sets the callback invoked with the new values, in chip order, in multiple selection. |
| `public bool AllowEmpty { get; set; }` | `[Parameter]`<br>Gets or sets whether activating the selected chip in single selection clears the value. |
| `public int? MaxVisible { get; set; }` | `[Parameter]`<br>Gets or sets how many chips are shown before the overflow button, or null to show all. |
| `public ControlSize Size { get; set; }` | `[Parameter]` Initial value `ControlSize.Medium`.<br>Gets or sets the size step of the chips. |
| `public bool Disabled { get; set; }` | `[Parameter]`<br>Gets or sets whether every chip rejects interaction. |
| `public TValue? SelectedValue { get; }` | Gets the value of the selected chip in single selection. |
| `public IReadOnlyList<TValue> SelectedValues { get; }` | Gets the values of the selected chips in multiple selection. |
| `public bool IsExpanded { get; }` | Gets whether the chips beyond `Nazor.Controls.ChipGroup<TValue>.MaxVisible` are shown. |
| `protected override string ElementName { get; }` | Gets the NSS element name emitted by ChipGroup. |

### `ChipKind`

Namespace `Nazor.Controls`. [Source](../../Sources/Core/Nazor.Controls/Selection/Chip.cs)

| Declaration | Contract |
| --- | --- |
| `public enum ChipKind` | What a `Nazor.Controls.Chip` does, written as the `kind` attribute. |
| `Action = 0` | Runs an action, as a compact button (`button` role). |
| `Filter = 1` | Turns a filter on or off (`togglebutton` role), alone or in a multiple-selection `Nazor.Controls.ChipGroup<TValue>`; a selected filter shows a `ChipCheck`. |
| `Choice = 2` | One choice of a single-selection `Nazor.Controls.ChipGroup<TValue>` (`radio` role). |
| `Input = 3` | An entry the user added, such as a recipient, removed with its `ChipRemove` button or with Backspace or Delete. |

### `CodeBlock`

Namespace `Nazor.Controls`. [Source](../../Sources/Core/Nazor.Controls/Text/CodeBlock.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed class CodeBlock : ContentControl` | A block of source code or other preformatted text in a monospaced font, keeping spaces and line breaks. The text is `Nazor.Controls.CodeBlock.Code` or the content between the tags; it can be selected and copied. With `Nazor.Controls.CodeBlock.ShowLineNumbers` a column of line numbers precedes it. `Nazor.Controls.CodeBlock.Language` is written as the `language` attribute for themes and tools; the block does not color the syntax. The parts are `CodeBlockLineNumbers` (decorative) and `CodeBlockCode` (the `text` role); the element has the `wrap` attribute when long lines wrap. |
| `protected override void OnParametersSet()` | Validates the first line number. |
| `protected override void AddAttributes(RenderTreeBuilder builder)` | Adds the language, the wrapping and, when named, the group role described as code.<br>`builder`. The builder that receives the render frames. |
| `protected override void AddContent(RenderTreeBuilder builder)` | Adds the line numbers and the code.<br>`builder`. The builder that receives the render frames. |
| `protected override void OnAfterRender(bool firstRender)` | Counts the lines of content written between the tags so the line numbers match.<br>`firstRender`. Whether this is the first render. |
| `public CodeBlock()` |  |
| `public string? Code { get; set; }` | `[Parameter]`<br>Gets or sets the code; it replaces the content between the tags. Line endings are normalized. |
| `public string? Language { get; set; }` | `[Parameter]`<br>Gets or sets the language of the code, such as `csharp`, written as the `language` attribute. |
| `public bool ShowLineNumbers { get; set; }` | `[Parameter]`<br>Gets or sets whether a column of line numbers precedes the code. |
| `public int FirstLineNumber { get; set; }` | `[Parameter]` Initial value `1`.<br>Gets or sets the number of the first line. |
| `public bool Wrap { get; set; }` | `[Parameter]`<br>Gets or sets whether long lines wrap instead of scrolling horizontally. Line numbers stay aligned only with unwrapped lines. |
| `public bool IsTextSelectionEnabled { get; set; }` | `[Parameter]` Initial value `true`.<br>Gets or sets whether the code can be selected and copied; true by default. |
| `public string? Text { get; }` | Gets the normalized code text, or null when the content comes from between the tags. |
| `public int LineCount { get; }` | Gets the number of lines shown. |
| `protected override string ElementName { get; }` | Gets the NSS element name emitted by CodeBlock. |

### `ComboBox<TItem>`

Namespace `Nazor.Controls`. [Source](../../Sources/Core/Nazor.Controls/Fields/ComboBox.cs)

In Basic, `ComboBoxPopup` and `ComboBoxOption` use the same popup border, surface colors and row padding as `Select`. The `current` option uses selection colors for keyboard and pointer navigation. `ComboBoxEmpty` displays `EmptyContent` with muted text and row padding. Set `Placeholder` for a hint while the editable text is empty; see the [editable combo example](../Guide/10-Text-Input-and-Data-Controls.md#choose-from-known-items).

| Declaration | Contract |
| --- | --- |
| `public sealed class ComboBox<TItem> : Control` | An editable choice: the user types to filter `Nazor.Controls.ComboBox<TItem>.Items` or opens the whole list with the chevron or Alt+Down, then chooses with the arrows and Enter or a click. `Nazor.Controls.ComboBox<TItem>.Value` (`@bind-Value`) is the chosen item; `Nazor.Controls.ComboBox<TItem>.Text` (`@bind-Text`) is the text in the box. When the text is committed (Enter or leaving the box) an item whose text matches it is chosen; other text is kept as free text with `Nazor.Controls.ComboBox<TItem>.AllowCustomValue` (the value becomes the default) or goes back to the chosen item's text otherwise; empty text clears the value. Built on `Nazor.Controls.Primitives.Combobox<TItem>`, so the parts are `ComboBoxInput`, `ComboBoxLeading`, `ComboBoxTrailing` (holding the `ComboBoxChevron` button), `ComboBoxPopup`, `ComboBoxOption` (`current` for the highlighted one) and `ComboBoxEmpty`. Inside a `Nazor.Controls.ComboBox<TItem>.Field` it is labeled and described automatically.<br>`TItem`. The type of the items. |
| `protected override void OnParametersSet()` | Validates the parameters and synchronizes the value and the text. |
| `public string TextOf(TItem item)` | Gets the text of an item.<br>`item`. The item.<br>**Returns**. The text shown for it. |
| `public IReadOnlyList<TItem> VisibleItems()` | Gets the items shown in the list: the matches of the typed text, or all of them after the chevron opened the list.<br>**Returns**. The items. |
| `protected override void BuildRenderTree(RenderTreeBuilder builder)` | Delegates the rendering to a combo box primitive named after the control.<br>`builder`. The builder that receives the render frames. |
| `protected override void OnAfterRender(bool firstRender)` | Exposes the combo box element as the control's element.<br>`firstRender`. Whether this is the first render. |
| `public Task ToggleAsync()` | Opens the whole list when closed and closes it when open, as the chevron does.<br>**Returns**. A task that completes when the list has changed. |
| `public Task ChooseAsync(TItem item)` | Chooses an item as the user would, writes its text and reports the change.<br>`item`. The item to choose.<br>**Returns**. A task that completes when the change has been reported. |
| `public Task FocusAsync(bool preventScroll = false)` | Moves the keyboard focus to the editor.<br>`preventScroll`. Whether focusing keeps the current scroll position.<br>**Returns**. A task that completes when the focus has moved. |
| `public ComboBox()` |  |
| `public IReadOnlyList<TItem> Items { get; set; }` | `[Parameter]` Initial value `[]`.<br>Gets or sets the items offered. |
| `public TItem? Value { get; set; }` | `[Parameter]`<br>Gets or sets the chosen item. |
| `public EventCallback<TItem?> ValueChanged { get; set; }` | `[Parameter]`<br>Gets or sets the callback invoked when the user chooses another item; supports `@bind-Value`. |
| `public string? Text { get; set; }` | `[Parameter]`<br>Gets or sets the text in the box; null follows the chosen item. |
| `public EventCallback<string> TextChanged { get; set; }` | `[Parameter]`<br>Gets or sets the callback invoked on every change of the text; supports `@bind-Text`. |
| `public Func<TItem, string>? ItemText { get; set; }` | `[Parameter]`<br>Gets or sets the text of an item; `ToString` by default. |
| `public RenderFragment<TItem>? ItemTemplate { get; set; }` | `[Parameter]`<br>Gets or sets how an item is rendered in the list. |
| `public Func<TItem, string, bool>? Filter { get; set; }` | `[Parameter]`<br>Gets or sets whether an item matches the typed text; by default its text contains the text, ignoring case. |
| `public bool AllowCustomValue { get; set; }` | `[Parameter]`<br>Gets or sets whether text that matches no item is kept as a free value. |
| `public RenderFragment? EmptyContent { get; set; }` | `[Parameter]`<br>Gets or sets content shown in the list when no item matches. |
| `public bool OpenOnFocus { get; set; }` | `[Parameter]`<br>Gets or sets whether the list opens when the box receives the focus. |
| `public DropDownPlacement DropDownPlacement { get; set; }` | `[Parameter]`<br>Gets or sets the preferred position of the list relative to the field. |
| `public int? PreferredMinVisibleItems { get; set; }` | `[Parameter]`<br>Gets or sets the preferred minimum number of visible options, or null for no minimum preference. |
| `public int PreferredMaxVisibleItems { get; set; }` | `[Parameter]` Initial value `10`.<br>Gets or sets the preferred maximum number of visible options. The default is 10. |
| `public string? Placeholder { get; set; }` | `[Parameter]`<br>Gets or sets the hint shown while the text is empty. |
| `public bool ReadOnly { get; set; }` | `[Parameter]`<br>Gets or sets whether the text cannot be edited; the list can still be opened with the chevron. |
| `public bool Disabled { get; set; }` | `[Parameter]`<br>Gets or sets whether the control rejects interaction. |
| `public bool Required { get; set; }` | `[Parameter]`<br>Gets or sets whether a value is required; validation is application-owned. |
| `public bool Invalid { get; set; }` | `[Parameter]`<br>Gets or sets the externally determined invalid state. |
| `public FieldAppearance Appearance { get; set; }` | `[Parameter]`<br>Gets or sets how the frame is drawn. |
| `public ControlSize Size { get; set; }` | `[Parameter]` Initial value `ControlSize.Medium`.<br>Gets or sets the size step. |
| `public RenderFragment? Leading { get; set; }` | `[Parameter]`<br>Gets or sets content shown before the text, such as an `Nazor.Controls.Icon`. |
| `public bool AutoFocus { get; set; }` | `[Parameter]`<br>Gets or sets whether the editor takes the focus when it appears. |
| `public FieldContext? Field { get; set; }` | `[CascadingParameter]`<br>Gets or sets the enclosing field, supplied automatically. |
| `public TItem? SelectedValue { get; }` | Gets the chosen item. |
| `public string CurrentText { get; }` | Gets the text in the box. |
| `public bool Opened { get; }` | Gets whether the list is open. |
| `protected override string ElementName { get; }` | Gets the NSS element name emitted by ComboBox. |

### `CommandPalette`

Namespace `Nazor.Controls`. [Source](../../Sources/Core/Nazor.Controls/Overlays/CommandPalette.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed class CommandPalette : DialogBase` | A searchable list of every command of an application, opened near the top of the window, usually with Ctrl+K or Ctrl+Shift+P (`Nazor.Controls.CommandPalette.Gesture`): typing filters the commands by their text and description (whole words first, then any part, then letters in order), Up and Down move the current command while the focus stays in the search field, Enter or a click runs it and closes the palette, Escape closes it. Each result shows the label, the description and the shortcut. Commands come from `Nazor.Controls.CommandPalette.Commands`, optionally grouped by `Nazor.Controls.CommandPalette.GroupBy`, and from `Nazor.Controls.CommandPaletteGroup` children; disabled commands are not listed. Parts: `CommandPaletteScrim`, `CommandPalette`, the search (`CommandPaletteSearch`, `CommandPaletteSearchInput`, `CommandPaletteSearchPopup`, `CommandPaletteSearchOption` with `current`, `CommandPaletteSearchEmpty`), `CommandPaletteGroupHeader`, `CommandPaletteItem`, `CommandPaletteItemLabel`, `CommandPaletteItemDescription` and `CommandPaletteItemShortcut`. |
| `protected override void Validate()` | Validates the parameters. |
| `protected override void OnOpenChanged(bool value)` | Clears the search when the palette opens.<br>`value`. The new open state. |
| `protected override void BuildOutside(RenderTreeBuilder builder)` | Renders the groups and the shortcut, which stay while the palette is closed.<br>`builder`. The builder that receives the render frames. |
| `protected override void AddSurfaceAttributes(IDictionary<string, object?> attributes)` | Adds the accessible name and the Enter fallback.<br>`attributes`. The attributes forwarded to the surface. |
| `protected override void BuildContent(RenderTreeBuilder builder)` | Renders the search field and the matching commands.<br>`builder`. The builder that receives the render frames. |
| `public IReadOnlyList<Command> MatchingCommands()` | Gets the commands that match the current search, in display order.<br>**Returns**. The matching commands. |
| `public Task RunAsync(Command command)` | Runs a command as choosing it in the palette does, then closes the palette.<br>`command`. The command to run.<br>**Returns**. A task that completes when the command has run. |
| `public CommandPalette()` |  |
| `public IEnumerable<Command>? Commands { get; set; }` | `[Parameter]`<br>Gets or sets the commands listed by the palette. |
| `public Func<Command, string?>? GroupBy { get; set; }` | `[Parameter]`<br>Gets or sets a function that gives the group of each command of `Nazor.Controls.CommandPalette.Commands`, or null for none. |
| `public RenderFragment? ChildContent { get; set; }` | `[Parameter]`<br>Gets or sets `Nazor.Controls.CommandPaletteGroup` children that add named groups of commands. |
| `public KeyGesture? Gesture { get; set; }` | `[Parameter]`<br>Gets or sets a shortcut that opens and closes the palette while it is rendered, such as Mod+K. |
| `public string? Placeholder { get; set; }` | `[Parameter]`<br>Gets or sets the hint shown in the empty search field. |
| `public string? EmptyText { get; set; }` | `[Parameter]`<br>Gets or sets the text shown when no command matches. |
| `public int MaxResults { get; set; }` | `[Parameter]` Initial value `50`.<br>Gets or sets the most matches listed. |
| `public EventCallback<Command> OnExecute { get; set; }` | `[Parameter]`<br>Gets or sets the callback invoked after a command has run from the palette. |
| `public string Query { get; }` | Gets the current search text. |
| `protected override string ElementName { get; }` | Gets the NSS element name emitted by CommandPalette. |
| `protected override DialogPlacement SurfacePlacement { get; }` | Gets the top placement. |
| `protected override bool DismissOnOutside { get; }` | Gets true: a press outside closes the palette. |

### `CommandPaletteGroup`

Namespace `Nazor.Controls`. [Source](../../Sources/Core/Nazor.Controls/Overlays/CommandPalette.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed class CommandPaletteGroup : ComponentBase, IDisposable` | A named group of commands listed by the enclosing `Nazor.Controls.CommandPalette`, in the order of the groups. It renders nothing itself. |
| `protected override void OnParametersSet()` | Registers with the palette. |
| `protected override void BuildRenderTree(RenderTreeBuilder builder)` | Renders nothing.<br>`builder`. The builder that receives the render frames. |
| `public void Dispose()` | Leaves the palette. |
| `public CommandPaletteGroup()` |  |
| `public string Name { get; set; }` | `[Parameter]` `[EditorRequired]` Initial value `""`.<br>Gets or sets the name shown above the group. |
| `public IEnumerable<Command>? Commands { get; set; }` | `[Parameter]`<br>Gets or sets the commands of the group. |
| `public CommandPalette? Palette { get; set; }` | `[CascadingParameter]`<br>Gets or sets the enclosing palette, supplied automatically. |

### `Container`

Namespace `Nazor.Controls`. [Source](../../Sources/Core/Nazor.Controls/Layout/Container.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed class Container : ContentControl` | Groups other controls; layout and scrolling are defined by NSS (block, flex, grid and overflow). A named container or one with `overflow-x`/`overflow-y: auto` or `scroll` is exposed as an accessible group, and its scroll position follows its computed overflow. Other layout-only containers pass their children to the nearest semantic ancestor. |
| `protected override void AddAttributes(RenderTreeBuilder builder)` | Adds the group role when the container is named.<br>`builder`. The builder that receives the render frames. |
| `public Container()` |  |
| `protected override string ElementName { get; }` | Gets the NSS element name emitted by Container. |

### `ContentControl`

Namespace `Nazor.Controls`. [Source](../../Sources/Core/Nazor.Controls/Control.cs)

| Declaration | Contract |
| --- | --- |
| `public abstract class ContentControl : Control` | A control whose children come from the markup between its tags. |
| `protected override void AddContent(RenderTreeBuilder builder)` | Adds the child render frames used by ContentControl.<br>`builder`. The builder that receives the render frames. |
| `protected ContentControl()` |  |
| `public RenderFragment? ChildContent { get; set; }` | `[Parameter]`<br>Gets or sets the render fragment supplied between the component's opening and closing tags. |

### `ContextMenu`

Namespace `Nazor.Controls`. [Source](../../Sources/Core/Nazor.Controls/Menus/Menu.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed class ContextMenu : ComponentBase, IMenuOwner` | Opens a `Nazor.Controls.Menu` where the user asks for one on its `Nazor.Controls.ContextMenu.Trigger` content: a right click, Shift+F10 or the menu key (below the focused control). The trigger keeps its own left-click behavior. Call `Nazor.Controls.ContextMenu.ShowAsync(Nazor.Input.PointerEventArgs)` to open it from other code, such as a long press. |
| `protected override void BuildRenderTree(RenderTreeBuilder builder)` | Renders the trigger wrapper and the menu.<br>`builder`. The builder that receives the render frames. |
| `public Task ShowAsync(PointerEventArgs e)` | Opens the menu at the position of a pointer or keyboard request.<br>`e`. The context-menu event.<br>**Returns**. A task that completes when the menu is open. |
| `public Task CloseAsync()` | Closes the menu.<br>**Returns**. A task that completes when the menu is closed. |
| `public ContextMenu()` |  |
| `public RenderFragment? Trigger { get; set; }` | `[Parameter]`<br>Gets or sets the content that opens the menu. |
| `public RenderFragment? ChildContent { get; set; }` | `[Parameter]`<br>Gets or sets the items. |
| `public bool Disabled { get; set; }` | `[Parameter]`<br>Gets or sets whether the menu is unavailable; the system text menu is not shown either. |
| `public EventCallback<bool> IsOpenChanged { get; set; }` | `[Parameter]`<br>Gets or sets the callback invoked when the menu opens or closes. |
| `public string? Class { get; set; }` | `[Parameter]`<br>Gets or sets the space-separated NSS classes of the menu surface. |
| `public bool Opened { get; }` | Gets whether the menu is open. |

### `Control`

Namespace `Nazor.Controls`. [Source](../../Sources/Core/Nazor.Controls/Control.cs)

| Declaration | Contract |
| --- | --- |
| `public abstract class Control : ComponentBase` | Base class of the built-in controls. A control is a component that renders exactly one element named after the control; styling, layout, input and accessibility work on that element. |
| `public Task FocusAsync(bool preventScroll = false)` | Requests keyboard focus for the target element, optionally preserving scroll offsets.<br>`preventScroll`. Whether focusing should preserve the current scroll position.<br>**Returns**. A task that completes when the operation has finished. |
| `public bool CapturePointer()` | Captures pointer input for the target element until capture is released or canceled.<br>**Returns**. true if pointer capture was acquired; otherwise, false. |
| `public void ReleasePointerCapture()` | Releases pointer capture held by the target element. |
| `protected override void BuildRenderTree(RenderTreeBuilder builder)` | Writes the render frames for Control to the supplied builder.<br>`builder`. The builder that receives the render frames. |
| `protected virtual void AddAttributes(RenderTreeBuilder builder)` | Adds control-specific element attributes, with sequence numbers from 10 to 999.<br>`builder`. The builder that receives the render frames. |
| `protected virtual void AddContent(RenderTreeBuilder builder)` | Adds the element children, with sequence numbers from 1000.<br>`builder`. The builder that receives the render frames. |
| `protected Control()` |  |
| `public string? Class { get; set; }` | `[Parameter]`<br>Gets or sets the space-separated NSS classes applied to the element. |
| `public LogicalItemPosition? ItemPosition { get; set; }` | `[Parameter]` Explicit logical position on this physical root; null clears it. Does not inherit to children. |
| `public StylePartSet? Parts { get; set; }` | `[Parameter]` Explicit public part bindings on the physical root; ownership is checked against the logical tree. |
| `public CustomStateSet? CustomStates { get; set; }` | `[Parameter]` Immutable named states published on this control's physical root; independent of accessibility and input behavior. |
| `public string? Id { get; set; }` | `[Parameter]`<br>Gets or sets the element identifier used by NSS selectors and control references. |
| `public int? TabIndex { get; set; }` | `[Parameter]`<br>Gets or sets the keyboard tab order; null uses the control's default focus behavior. |
| `public IReadOnlyDictionary<string, object?>? AdditionalAttributes { get; set; }` | `[Parameter]`<br>Gets or sets unmatched markup attributes forwarded to the rendered element. |
| `public ElementReference Element { get; protected set; }` | Gets the retained element associated with this control or element reference. |
| `public string? AccessibleName { get; set; }` | `[Parameter]`<br>Name exposed to assistive technologies when the visible content is not enough. |
| `public string? AccessibleDescription { get; set; }` | `[Parameter]`<br>A longer description for assistive technologies, read after the name (UI Automation's full description and help). |
| `public string? LabeledBy { get; set; }` | `[Parameter]`<br>The `Id` of the control whose text names this one for assistive technologies, as the label before a field: `<TextBlock Id="name">Name</TextBlock> <TextField LabeledBy="name" />`. |
| `public string? DescribedBy { get; set; }` | `[Parameter]`<br>Gets or sets space-separated identifiers of elements describing this control. |
| `public string? Tooltip { get; set; }` | `[Parameter]`<br>A short text shown below the control when the pointer rests on it or the keyboard focus reaches it. |
| `public string? Style { get; set; }` | `[Parameter]`<br>NSS declarations for this control only, applied after every rule as the style attribute of HTML: `Style="@($"width: {percent}%")"`. Values are read at runtime. |
| `protected abstract string ElementName { get; }` | Element name, matched by NSS type selectors. |
| `protected virtual string? ElementStyle { get; }` | The style attribute of the element: `Nazor.Controls.Control.Style`, to which a control can add its own declarations. |
| `protected virtual int? ElementTabIndex { get; }` | Gets the tab index of the root. Composite controls can place the tab stop on a child. |

### `ControlAppearance`

Namespace `Nazor.Controls`. [Source](../../Sources/Core/Nazor.Controls/Vocabulary.cs)

| Declaration | Contract |
| --- | --- |
| `public enum ControlAppearance` | How strongly a control is filled, the shared vocabulary of buttons and similar controls. Themes draw each value in their own style; a theme without a matching style uses the nearest one. The value is written as the `appearance` attribute, for example `Button[appearance="accent"]`. |
| `Standard = 0` | The ordinary look of the control. |
| `Accent = 1` | Filled with the accent color, for the main action of a view. |
| `Tonal = 2` | A softer tint of the accent color, for important but secondary actions. |
| `Outline = 3` | Only an outline, for alternative actions. |
| `Subtle = 4` | No fill or border until hovered, for actions in toolbars, lists and dense views. |
| `Link = 5` | Drawn as a hyperlink. |

### `ControlSize`

Namespace `Nazor.Controls`. [Source](../../Sources/Core/Nazor.Controls/Vocabulary.cs)

| Declaration | Contract |
| --- | --- |
| `public enum ControlSize` | The size step of a control, written as the `size` attribute. |
| `Small = 0` | The compact size, for dense views. |
| `Medium = 1` | The ordinary size. |
| `Large = 2` | The large size, for prominent actions and touch. |

### `DataGrid<TItem>`

Namespace `Nazor.Controls`. [Source](../../Sources/Core/Nazor.Controls/Data/DataGrid.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed class DataGrid<TItem> : Control, IDisposable` | A table of items with columns declared as `Nazor.Controls.DataGridColumn<TItem>` children: rows are virtualized, so tens of thousands of items stay fast; columns sort (Shift adds a sort), resize, reorder by dragging their headers, freeze at the start and edit their cells; rows are selected (`Nazor.Controls.DataGrid<TItem>.SelectionMode`, optionally with a column of check boxes), grouped through the `Nazor.Controls.Primitives.DataView<TItem>` with headers that open and close, and can show details below. The grid is a single Tab stop: the arrows, Home, End (with Ctrl for the first and last row), Page Up and Page Down move between cells and the header; Enter or F2 edits, Space selects the row, Ctrl+A selects every row. On a header Enter or Space sorts, Alt with the arrows moves the column and Ctrl with the arrows resizes it. The parts are `DataGridHeader`, `DataGridHeaderRow`, `DataGridColumnHeader` (`sort`, `sortable`, `frozen`, `dragging`, `drop-target`) with `DataGridColumnHeaderContent`, `DataGridSortMark` and a `ResizeHandle`, `DataGridSelectionHeader`, `DataGridDetailsHeader`, `DataGridBody` (the virtualized viewport, with `DataGridBodyItem`), `DataGridRow` (`selected`, `alternate`, `expanded`), `DataGridCell` (`current`, `editable`, `editing`, `frozen`, `frozen-last`, `align`), `DataGridSelectionCell`, `DataGridDetailsCell` with `DataGridDetailsToggle`, `DataGridCellEditor`, `DataGridRowDetails`, `DataGridGroupHeader` with `DataGridGroupHeaderCell`, `DataGridGroupToggle`, `DataGridGroupKey` and `DataGridGroupCount`, `DataGridFooter`, `DataGridFooterRow`, `DataGridFooterCell`, `DataGridEmpty` and `DataGridLoading`.<br>`TItem`. The type of the items. |
| `public DataGrid()` | Creates a grid. |
| `protected override void OnParametersSet()` | Validates the parameters and writes them into the view. |
| `protected override Task OnParametersSetAsync()` | Loads the data of a provider when the request changed.<br>**Returns**. A task that completes when the data is available. |
| `protected override void AddAttributes(RenderTreeBuilder builder)` | Adds the table role, the counts and the keyboard handling.<br>`builder`. The builder that receives the render frames. |
| `protected override void AddContent(RenderTreeBuilder builder)` | Adds the header, the virtualized body, the footer and the state parts.<br>`builder`. The builder that receives the render frames. |
| `protected override Task OnAfterRenderAsync(bool firstRender)` | Refreshes hovered row geometry and focuses the pending cell or editor after rendering.<br>`firstRender`. Whether this is the first render.<br>**Returns**. A task that completes when the focus has moved. |
| `public Task SortAsync(string key, bool add = false)` | Sorts by a column as a click on its header does: ascending, then descending, then not sorted; with `add` the other sorts are kept, as with Shift.<br>`key`. The column key.<br>`add`. Whether the other sorts are kept.<br>**Returns**. A task that completes when the change has been reported. |
| `public double? GetColumnWidth(string key)` | Gets the current width of a visible column, as laid out.<br>`key`. The column key.<br>**Returns**. The width in pixels, or null when the column is not visible. |
| `public Task MoveColumnAsync(string key, int offset)` | Moves a column by a number of places among the visible columns, within the frozen or the scrolling columns.<br>`key`. The column key.<br>`offset`. The number of places; negative moves towards the start.<br>**Returns**. A task that completes when the change has been applied. |
| `public void SetDetailsOpen(TItem item, bool open)` | Opens or closes the details of an item.<br>`item`. The item.<br>`open`. Whether the details are shown. |
| `public bool BeginEdit(TItem item, string columnKey)` | Starts editing a cell, as Enter does on it.<br>`item`. The item of the row.<br>`columnKey`. The key of an editable column.<br>**Returns**. true if the edit started. |
| `public void Refresh()` | Computes the rows again, after the items changed in place. |
| `public void Dispose()` | Stops listening to the view. |
| `public IReadOnlyList<TItem>? Items { get; set; }` | `[Parameter]`<br>Gets or sets the items shown, processed in memory. |
| `public Func<DataRequest, Task<DataPage<TItem>>>? ItemsProvider { get; set; }` | `[Parameter]`<br>Gets or sets the asynchronous source of the items for server-side data, used instead of `Nazor.Controls.DataGrid<TItem>.Items`. |
| `public DataView<TItem>? View { get; set; }` | `[Parameter]`<br>Gets or sets a view shared with other components, such as a pager; when null the grid uses its own. The grid writes its items, filter, grouping, sorts and paging parameters into the view. |
| `public object? Query { get; set; }` | `[Parameter]`<br>Gets or sets the application-defined filter sent to `Nazor.Controls.DataGrid<TItem>.ItemsProvider`, such as a search text. |
| `public Func<TItem, bool>? Filter { get; set; }` | `[Parameter]`<br>Gets or sets the predicate an item must satisfy to be shown. |
| `public Func<TItem, object?>? GroupBy { get; set; }` | `[Parameter]`<br>Gets or sets the function that gives the group of an item; groups are shown under headers that open and close. |
| `public RenderFragment<DataGroup<TItem>>? GroupHeaderTemplate { get; set; }` | `[Parameter]`<br>Gets or sets the content of a group header, after the toggle; the key and the count by default. |
| `public IReadOnlyList<DataSort>? Sorts { get; set; }` | `[Parameter]`<br>Gets or sets the sorts, the most significant first, by column key. |
| `public EventCallback<IReadOnlyList<DataSort>> SortsChanged { get; set; }` | `[Parameter]`<br>Gets or sets the callback invoked when the user changes the sorts; supports `@bind-Sorts`. |
| `public int PageSize { get; set; }` | `[Parameter]`<br>Gets or sets the number of rows per page, or zero to show every row. |
| `public int Page { get; set; }` | `[Parameter]`<br>Gets or sets the zero-based page shown. |
| `public EventCallback<int> PageChanged { get; set; }` | `[Parameter]`<br>Gets or sets the callback invoked when the page changes; supports `@bind-Page`. |
| `public RenderFragment? ChildContent { get; set; }` | `[Parameter]`<br>Gets or sets the column declarations. |
| `public double? RowHeight { get; set; }` | `[Parameter]` Fixed logical row pitch, including any theme gap; null measures `DataGridRowMetric`, with a 32-unit Basic fallback. |
| `public SelectionMode SelectionMode { get; set; }` | `[Parameter]` Initial value `SelectionMode.Single`.<br>Gets or sets how many rows can be selected. |
| `public TItem? SelectedItem { get; set; }` | `[Parameter]`<br>Gets or sets the selected item in single selection, or the first selected one. |
| `public EventCallback<TItem?> SelectedItemChanged { get; set; }` | `[Parameter]`<br>Gets or sets the callback invoked when the user changes the selection; supports `@bind-SelectedItem`. |
| `public IReadOnlyList<TItem>? SelectedItems { get; set; }` | `[Parameter]`<br>Gets or sets the selected items in multiple selection. |
| `public EventCallback<IReadOnlyList<TItem>> SelectedItemsChanged { get; set; }` | `[Parameter]`<br>Gets or sets the callback invoked when the user changes the selection; supports `@bind-SelectedItems`. |
| `public bool ShowSelectionColumn { get; set; }` | `[Parameter]`<br>Gets or sets whether a first column of check boxes selects rows, with a header box that selects every row. |
| `public bool SelectionFollowsFocus { get; set; }` | `[Parameter]`<br>Gets or sets whether moving with the arrows also selects the row, in single selection. |
| `public Func<TItem, object>? ItemKey { get; set; }` | `[Parameter]`<br>Gets or sets the function that gives each item a stable identity for selection and details; the item itself when null. |
| `public RenderFragment<TItem>? RowDetailsTemplate { get; set; }` | `[Parameter]`<br>Gets or sets the content shown below a row when its details are open, with a toggle column at the start. |
| `public RenderFragment? EmptyContent { get; set; }` | `[Parameter]`<br>Gets or sets the content shown when no row passes the filter; a localized "no results" text by default. |
| `public bool Loading { get; set; }` | `[Parameter]`<br>Gets or sets whether the grid shows that data is loading, in addition to a running `Nazor.Controls.DataGrid<TItem>.ItemsProvider` request. |
| `public bool ReorderableColumns { get; set; }` | `[Parameter]` Initial value `true`.<br>Gets or sets whether the user can reorder columns by dragging their headers or with Alt and the arrows. |
| `public DataGridLines GridLines { get; set; }` | `[Parameter]` Initial value `DataGridLines.Both`.<br>Gets or sets the lines drawn between cells. |
| `public bool Striped { get; set; }` | `[Parameter]`<br>Gets or sets whether every other row is marked `alternate` for a striped look. |
| `public ControlSize Size { get; set; }` | `[Parameter]` Initial value `ControlSize.Medium`.<br>Gets or sets the size step, for density. |
| `public EventCallback<DataGridCellEdit<TItem>> OnCellEdited { get; set; }` | `[Parameter]`<br>Gets or sets the callback invoked with a committed cell edit. |
| `public EventCallback<TItem> OnRowActivated { get; set; }` | `[Parameter]`<br>Gets or sets the callback invoked when a row is activated by a double click or Enter on a cell that cannot be edited. |
| `public DataView<TItem> DataView { get; }` | Gets the view that computes the rows. |
| `public IReadOnlyList<TItem> Selection { get; }` | Gets the selected items, in display order. |
| `public IReadOnlyList<string> ColumnOrder { get; }` | Gets the keys of the visible columns, in display order. |
| `public bool IsEditing { get; }` | Gets whether a cell is being edited. |
| `protected override string ElementName { get; }` | Gets the NSS element name emitted by DataGrid. |
| `protected override string? ElementStyle { get; }` | Gets the resolved row pitch and caller style declarations. |

### `DataGridAlignment`

Namespace `Nazor.Controls`. [Source](../../Sources/Core/Nazor.Controls/Data/DataGridColumn.cs)

| Declaration | Contract |
| --- | --- |
| `public enum DataGridAlignment` | How the content of a `Nazor.Controls.DataGridColumn<TItem>` is aligned, written as the `align` attribute. |
| `Start = 0` | Aligned to the start, as text. |
| `Center = 1` | Centered, as marks and short codes. |
| `End = 2` | Aligned to the end, as numbers and amounts. |

### `DataGridCellEdit<TItem>`

Namespace `Nazor.Controls`. [Source](../../Sources/Core/Nazor.Controls/Data/DataGridColumn.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed record DataGridCellEdit<TItem> : IEquatable<DataGridCellEdit<TItem>>` | A cell value changed by the user in a `Nazor.Controls.DataGrid<TItem>`.<br>`TItem`. The type of the items.<br>`Item`. The edited item.<br>`Column`. The key of the edited column.<br>`OldValue`. The value before the edit.<br>`NewValue`. The committed value. |
| `public DataGridCellEdit(TItem Item, string Column, object? OldValue, object? NewValue)` | A cell value changed by the user in a `Nazor.Controls.DataGrid<TItem>`.<br>`TItem`. The type of the items.<br>`Item`. The edited item.<br>`Column`. The key of the edited column.<br>`OldValue`. The value before the edit.<br>`NewValue`. The committed value. |
| `public override string ToString()` |  |
| `public static bool operator !=(DataGridCellEdit<TItem>? left, DataGridCellEdit<TItem>? right)` |  |
| `public static bool operator ==(DataGridCellEdit<TItem>? left, DataGridCellEdit<TItem>? right)` |  |
| `public override int GetHashCode()` |  |
| `public override bool Equals(object? obj)` |  |
| `public bool Equals(DataGridCellEdit<TItem>? other)` |  |
| `public void Deconstruct(out TItem Item, out string Column, out object? OldValue, out object? NewValue)` |  |
| `public TItem Item { get; init; }` | The edited item. |
| `public string Column { get; init; }` | The key of the edited column. |
| `public object? OldValue { get; init; }` | The value before the edit. |
| `public object? NewValue { get; init; }` | The committed value. |

### `DataGridColumn<TItem>`

Namespace `Nazor.Controls`. [Source](../../Sources/Core/Nazor.Controls/Data/DataGridColumn.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed class DataGridColumn<TItem> : ComponentBase, IDisposable` | A column of a `Nazor.Controls.DataGrid<TItem>`, declared inside it. A column shows `Nazor.Controls.DataGridColumn<TItem>.Field` formatted with `Nazor.Controls.DataGridColumn<TItem>.Format`, or `Nazor.Controls.DataGridColumn<TItem>.CellTemplate`; its header shows `Nazor.Controls.DataGridColumn<TItem>.Header` or `Nazor.Controls.DataGridColumn<TItem>.HeaderTemplate`. Its width is `Nazor.Controls.DataGridColumn<TItem>.Width`, or a share of the remaining space by `Nazor.Controls.DataGridColumn<TItem>.Flex`, within `Nazor.Controls.DataGridColumn<TItem>.MinWidth` and `Nazor.Controls.DataGridColumn<TItem>.MaxWidth`; the user can resize it (`Nazor.Controls.DataGridColumn<TItem>.Resizable`), sort by it (`Nazor.Controls.DataGridColumn<TItem>.Sortable`) and edit its cells (`Nazor.Controls.DataGridColumn<TItem>.Editable`). Frozen columns stay at the start while the grid scrolls sideways.<br>`TItem`. The type of the items. |
| `protected override void OnParametersSet()` | Validates the parameters and joins the enclosing grid. |
| `protected override void BuildRenderTree(RenderTreeBuilder builder)` | Renders nothing; the grid renders the column.<br>`builder`. The builder that receives the render frames. |
| `public void Dispose()` | Leaves the enclosing grid. |
| `public DataGridColumn()` |  |
| `public string? Key { get; set; }` | `[Parameter]`<br>Gets or sets the stable key of the column, used by sorts and reordering; the header text when null. |
| `public string? Header { get; set; }` | `[Parameter]`<br>Gets or sets the header text, also the accessible name of the column. |
| `public RenderFragment? HeaderTemplate { get; set; }` | `[Parameter]`<br>Gets or sets the header content, replacing `Nazor.Controls.DataGridColumn<TItem>.Header` on screen. |
| `public Func<TItem, object?>? Field { get; set; }` | `[Parameter]`<br>Gets or sets the function that reads the value of the column; it is also the default sort key and edited value. |
| `public string? Format { get; set; }` | `[Parameter]`<br>Gets or sets the format string applied to values that are `System.IFormattable`, such as `N2` or `d`. |
| `public IFormatProvider? FormatProvider { get; set; }` | `[Parameter]`<br>Gets or sets the culture used to format and parse values; the current culture when null. |
| `public RenderFragment<TItem>? CellTemplate { get; set; }` | `[Parameter]`<br>Gets or sets the content of a cell, replacing the formatted value. |
| `public double? Width { get; set; }` | `[Parameter]`<br>Gets or sets the width in pixels, or null to use `Nazor.Controls.DataGridColumn<TItem>.Flex` or the default width of 120 pixels. |
| `public double MinWidth { get; set; }` | `[Parameter]` Initial value `40`.<br>Gets or sets the smallest width the column can have. |
| `public double MaxWidth { get; set; }` | `[Parameter]` Initial value `double.PositiveInfinity`.<br>Gets or sets the largest width the column can have. |
| `public double Flex { get; set; }` | `[Parameter]`<br>Gets or sets the share of the remaining width the column takes, zero for none; used when `Nazor.Controls.DataGridColumn<TItem>.Width` is null. |
| `public DataGridAlignment Alignment { get; set; }` | `[Parameter]`<br>Gets or sets how the content of the cells and of the header is aligned. |
| `public bool Sortable { get; set; }` | `[Parameter]` Initial value `true`.<br>Gets or sets whether clicking the header or pressing Enter on it sorts by the column, when it has a sort key. |
| `public Func<TItem, object?>? SortBy { get; set; }` | `[Parameter]`<br>Gets or sets the function that reads the sort key, when it differs from `Nazor.Controls.DataGridColumn<TItem>.Field`. |
| `public IComparer<object?>? Comparer { get; set; }` | `[Parameter]`<br>Gets or sets the comparer of the sort keys, or null for the default one. |
| `public bool Resizable { get; set; }` | `[Parameter]` Initial value `true`.<br>Gets or sets whether the user can change the width with the handle at the end of the header. |
| `public bool Frozen { get; set; }` | `[Parameter]`<br>Gets or sets whether the column stays at the start while the grid scrolls sideways. |
| `public bool Hidden { get; set; }` | `[Parameter]`<br>Gets or sets whether the column is not shown. |
| `public bool Editable { get; set; }` | `[Parameter]`<br>Gets or sets whether the cells can be edited with Enter, F2 or a double click. |
| `public Action<TItem, object?>? Setter { get; set; }` | `[Parameter]`<br>Gets or sets the function that stores a committed value in an item; without it only the grid's callback is invoked. |
| `public Func<string, object?>? Parse { get; set; }` | `[Parameter]`<br>Gets or sets the conversion of the text typed in the default editor to a value; by default the text is converted to the type of the current value with the column's culture. Throw `System.FormatException` to reject the text. |
| `public RenderFragment<DataGridEditContext<TItem>>? EditorTemplate { get; set; }` | `[Parameter]`<br>Gets or sets the editor of a cell, replacing the default text editor. |
| `public RenderFragment<IReadOnlyList<TItem>>? FooterTemplate { get; set; }` | `[Parameter]`<br>Gets or sets the content of the footer cell, which receives every item that passes the filter, for aggregates. |
| `public bool IsRowHeader { get; set; }` | `[Parameter]`<br>Gets or sets whether the cells of this column name their rows for assistive technologies (`rowheader`). |
| `public string? Class { get; set; }` | `[Parameter]`<br>Gets or sets NSS classes added to the header and the cells of the column. |
| `public DataGrid<TItem>? Grid { get; set; }` | `[CascadingParameter]`<br>Gets or sets the enclosing grid, supplied automatically. |

### `DataGridEditContext<TItem>`

Namespace `Nazor.Controls`. [Source](../../Sources/Core/Nazor.Controls/Data/DataGridColumn.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed class DataGridEditContext<TItem>` | The state of a cell being edited, given to `Nazor.Controls.DataGridColumn<TItem>.EditorTemplate`: the editor reads and changes `Nazor.Controls.DataGridEditContext<TItem>.Value` and calls `Nazor.Controls.DataGridEditContext<TItem>.CommitAsync` or `Nazor.Controls.DataGridEditContext<TItem>.Cancel`. Enter commits and Escape cancels from any editor; moving the focus out of the cell commits.<br>`TItem`. The type of the items. |
| `public Task CommitAsync()` | Stores `Nazor.Controls.DataGridEditContext<TItem>.Value` and ends the edit, unless `Nazor.Controls.DataGridEditContext<TItem>.Invalid` is set.<br>**Returns**. A task that completes when the change has been reported. |
| `public void Cancel()` | Ends the edit without storing anything. |
| `public TItem Item { get; }` | Gets the edited item. |
| `public string Column { get; }` | Gets the key of the edited column. |
| `public object? OldValue { get; }` | Gets the value before the edit. |
| `public object? Value { get; set; }` | Gets or sets the value that a commit stores. |
| `public bool Invalid { get; set; }` | Gets or sets whether the value is not acceptable; a commit is refused while it is set. |

### `DataGridLines`

Namespace `Nazor.Controls`. [Source](../../Sources/Core/Nazor.Controls/Data/DataGridColumn.cs)

| Declaration | Contract |
| --- | --- |
| `public enum DataGridLines` | Which lines a `Nazor.Controls.DataGrid<TItem>` draws between cells, written as the `gridlines` attribute. |
| `None = 0` | No lines. |
| `Horizontal = 1` | Lines between rows. |
| `Vertical = 2` | Lines between columns. |
| `Both = 3` | Lines between rows and between columns. |

### `DatePicker`

Namespace `Nazor.Controls`. [Source](../../Sources/Core/Nazor.Controls/Pickers/DatePicker.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed class DatePicker : PickerFieldBase<DateOnly>` | A date field: the `DatePickerInput` editor parses dates typed in `Nazor.Controls.PickerFieldBase<TValue>.Culture` with `Nazor.Controls.DatePicker.Format` (the short date by default) and the `DatePickerButton` opens the `DatePickerPopup` with a `Nazor.Controls.Calendar` on the value. `Nazor.Controls.PickerFieldBase<TValue>.Value` (`@bind-Value`) is the chosen date. Choosing a day closes the popup and returns the focus to the editor; a typed date outside `Nazor.Controls.DatePicker.Minimum`, `Nazor.Controls.DatePicker.Maximum` or rejected by `Nazor.Controls.DatePicker.IsDateUnavailable` marks the field invalid. With `Nazor.Controls.DatePicker.ShowTodayButton` the popup has a `DatePickerToday` button in its `DatePickerFooter`. The other parts are those of `Nazor.Controls.PickerFieldBase<TValue>`. |
| `protected override void OnParametersSet()` | Validates the parameters. |
| `protected override string FormatValue(DateOnly value)` | Formats a date with `Nazor.Controls.DatePicker.Format`.<br>`value`. The date.<br>**Returns**. The text. |
| `protected override bool TryParseText(string text, out DateOnly value)` | Parses a typed date.<br>`text`. The text.<br>`value`. The date.<br>**Returns**. true if the text is a date. |
| `protected override bool IsAllowed(DateOnly value)` | Tells whether a date is within the limits and available.<br>`value`. The date.<br>**Returns**. true if it can be chosen. |
| `protected override void RenderPopup(RenderTreeBuilder builder)` | Renders the calendar and the footer.<br>`builder`. The builder positioned inside the popup. |
| `public DatePicker()` |  |
| `public string Format { get; set; }` | `[Parameter]` Initial value `"d"`.<br>Gets or sets the .NET format of the date in the editor, `d` (short date) by default. |
| `public DateOnly? Minimum { get; set; }` | `[Parameter]`<br>Gets or sets the earliest date that can be chosen, or null for no limit. |
| `public DateOnly? Maximum { get; set; }` | `[Parameter]`<br>Gets or sets the latest date that can be chosen, or null for no limit. |
| `public Func<DateOnly, bool>? IsDateUnavailable { get; set; }` | `[Parameter]`<br>Gets or sets a predicate marking dates that cannot be chosen. |
| `public DayOfWeek? FirstDayOfWeek { get; set; }` | `[Parameter]`<br>Gets or sets the first day of the week in the calendar; null uses the culture's. |
| `public bool ShowWeekNumbers { get; set; }` | `[Parameter]`<br>Gets or sets whether the calendar shows week numbers. |
| `public DateOnly? Today { get; set; }` | `[Parameter]`<br>Gets or sets the date marked as today; null uses the computer's date. |
| `public bool ShowTodayButton { get; set; }` | `[Parameter]`<br>Gets or sets whether the popup has a button that chooses today. |
| `public Calendar? PopupCalendar { get; }` | Gets the calendar shown in the open popup, or null while it is closed. |
| `protected override string ElementName { get; }` | Gets the NSS element name emitted by DatePicker. |
| `protected override string ButtonLabel { get; }` | Gets the name of the button and of the popup. |
| `protected override string? PatternHint { get; }` | Gets the date pattern shown as placeholder. |
| `protected override string FormatKey { get; }` | Gets the culture and the format, which rewrite the text when they change. |

### `DateText`

Namespace `Nazor.Controls`. [Source](../../Sources/Core/Nazor.Controls/Pickers/DatePicker.cs)

| Declaration | Contract |
| --- | --- |
| `public static class DateText` | Formatting and lenient parsing of dates typed in a culture, shared by the date pickers. |
| `public static bool TryParse(string? text, CultureInfo culture, string format, out DateOnly date)` | Parses a date typed by the user: first with `format`, then with the culture's short and long patterns (also with one-digit days and months and two-digit years), then with the culture's general rules, which accept month names and a missing year.<br>`text`. The text to parse.<br>`culture`. The culture of the text.<br>`format`. The .NET format the text is displayed in, such as `d`.<br>`date`. The parsed date.<br>**Returns**. true if the text is a date. |
| `public static string Pattern(CultureInfo culture, string format)` | Gets a readable pattern of a date format, such as `dd/MM/yyyy`, for placeholders.<br>`culture`. The culture.<br>`format`. The .NET format, such as `d`.<br>**Returns**. The pattern, or the format itself when it is a custom pattern. |

### `DateTimePicker`

Namespace `Nazor.Controls`. [Source](../../Sources/Core/Nazor.Controls/Pickers/DateTimePicker.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed class DateTimePicker : Control` | A date and time field: one `DateTimePicker` frame holding a `Nazor.Controls.DatePicker` and a `Nazor.Controls.TimePicker` drawn without their own frames (`Nazor.Controls.FieldAppearance.Plain`), each with its editor and popup. `Nazor.Controls.DateTimePicker.Value` (`@bind-Value`) combines them: choosing a date without a time uses midnight (or the earliest allowed time of that day); a time typed before the date is kept until a date is chosen; clearing the date clears the value. `Nazor.Controls.DateTimePicker.Minimum` and `Nazor.Controls.DateTimePicker.Maximum` limit the dates and, on their first and last days, the times. Inside a `Nazor.Controls.DateTimePicker.Field` the date editor is the field's control and the time editor is named `Time`. |
| `protected override void OnParametersSet()` | Validates the parameters and synchronizes the value. |
| `protected override void AddAttributes(RenderTreeBuilder builder)` | Adds the frame states.<br>`builder`. The builder that receives the render frames. |
| `protected override void AddContent(RenderTreeBuilder builder)` | Adds the date and time pickers.<br>`builder`. The builder that receives the render frames. |
| `public DateTimePicker()` |  |
| `public DateTime? Value { get; set; }` | `[Parameter]`<br>Gets or sets the date and time, or null when empty. |
| `public EventCallback<DateTime?> ValueChanged { get; set; }` | `[Parameter]`<br>Gets or sets the callback invoked when the user changes the date or the time. |
| `public CultureInfo? Culture { get; set; }` | `[Parameter]`<br>Gets or sets the culture of both editors; the current culture by default. |
| `public string DateFormat { get; set; }` | `[Parameter]` Initial value `"d"`.<br>Gets or sets the .NET format of the date, `d` by default. |
| `public bool? Use24Hour { get; set; }` | `[Parameter]`<br>Gets or sets whether the clock has 24 hours; null follows the culture. |
| `public int MinuteIncrement { get; set; }` | `[Parameter]` Initial value `1`.<br>Gets or sets the step of the minutes offered, from 1 to 60. |
| `public bool ShowSeconds { get; set; }` | `[Parameter]`<br>Gets or sets whether the seconds are shown and offered. |
| `public DateTime? Minimum { get; set; }` | `[Parameter]`<br>Gets or sets the earliest moment that can be chosen, or null for no limit. |
| `public DateTime? Maximum { get; set; }` | `[Parameter]`<br>Gets or sets the latest moment that can be chosen, or null for no limit. |
| `public DateOnly? Today { get; set; }` | `[Parameter]`<br>Gets or sets the date marked as today in the calendar; null uses the computer's date. |
| `public bool Disabled { get; set; }` | `[Parameter]`<br>Gets or sets whether the picker rejects interaction. |
| `public bool ReadOnly { get; set; }` | `[Parameter]`<br>Gets or sets whether the value cannot change. |
| `public bool Required { get; set; }` | `[Parameter]`<br>Gets or sets whether a value is required; validation is application-owned. |
| `public bool Clearable { get; set; }` | `[Parameter]`<br>Gets or sets whether the date part has a clear button. |
| `public FieldAppearance Appearance { get; set; }` | `[Parameter]`<br>Gets or sets how the frame is drawn. |
| `public ControlSize Size { get; set; }` | `[Parameter]` Initial value `ControlSize.Medium`.<br>Gets or sets the size step. |
| `public FieldContext? Field { get; set; }` | `[CascadingParameter]`<br>Gets or sets the enclosing field, supplied automatically. |
| `public DateTime? CurrentValue { get; }` | Gets the current value. |
| `protected override string ElementName { get; }` | Gets the NSS element name emitted by DateTimePicker. |

### `DefinitionDescription`

Namespace `Nazor.Controls`. [Source](../../Sources/Core/Nazor.Controls/Text/List.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed class DefinitionDescription : ContentControl` | The description of the preceding `Nazor.Controls.DefinitionTerm`, with the `definition` role. |
| `protected override void AddAttributes(RenderTreeBuilder builder)` | Adds the definition role.<br>`builder`. The builder that receives the render frames. |
| `public DefinitionDescription()` |  |
| `protected override string ElementName { get; }` | Gets the NSS element name emitted by DefinitionDescription. |

### `DefinitionList`

Namespace `Nazor.Controls`. [Source](../../Sources/Core/Nazor.Controls/Text/List.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed class DefinitionList : ContentControl` | A list of terms and their descriptions, such as a glossary or the properties of a file: write `Nazor.Controls.DefinitionTerm` and `Nazor.Controls.DefinitionDescription` children in order; a term can have several descriptions. The element has the `definitionlist` role and the `layout` attribute. |
| `protected override void OnParametersSet()` | Validates the layout. |
| `protected override void AddAttributes(RenderTreeBuilder builder)` | Adds the definition list role and the layout.<br>`builder`. The builder that receives the render frames. |
| `public DefinitionList()` |  |
| `public DefinitionListLayout Layout { get; set; }` | `[Parameter]`<br>Gets or sets how terms and descriptions are arranged. |
| `protected override string ElementName { get; }` | Gets the NSS element name emitted by DefinitionList. |

### `DefinitionListLayout`

Namespace `Nazor.Controls`. [Source](../../Sources/Core/Nazor.Controls/Text/List.cs)

| Declaration | Contract |
| --- | --- |
| `public enum DefinitionListLayout` | How a `Nazor.Controls.DefinitionList` arranges its terms and descriptions, written as the `layout` attribute. |
| `Stacked = 0` | Each description below its term, indented. |
| `Inline = 1` | Terms in a first column and descriptions beside them, as a property sheet. |

### `DefinitionTerm`

Namespace `Nazor.Controls`. [Source](../../Sources/Core/Nazor.Controls/Text/List.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed class DefinitionTerm : ContentControl` | A term of a `Nazor.Controls.DefinitionList`, with the `term` role. |
| `protected override void AddAttributes(RenderTreeBuilder builder)` | Adds the term role.<br>`builder`. The builder that receives the render frames. |
| `public DefinitionTerm()` |  |
| `protected override string ElementName { get; }` | Gets the NSS element name emitted by DefinitionTerm. |

### `Dial`

Namespace `Nazor.Controls`. [Source](../../Sources/Core/Nazor.Controls/Selection/Dial.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed class Dial : Control` | A knob that chooses a number between `Nazor.Controls.Dial.Minimum` and `Nazor.Controls.Dial.Maximum` along an arc that starts at `Nazor.Controls.Dial.StartAngle` and spans `Nazor.Controls.Dial.SweepAngle` degrees (clockwise from twelve o'clock). Dragging the pointer's handle turns the knob to follow the pointer around the center; dragging anywhere else on the dial changes the value with the distance moved (up or right increases), `Nazor.Controls.Dial.DragDistance` logical units covering the whole range. The keyboard works as on a `Nazor.Controls.Slider`. `Nazor.Controls.Dial.Value` (`@bind-Value`) and `oninput` follow the value, `onchange` comes when the user lets go. The element has the `slider` role, announced as a dial; the parts are `DialTrack` and `DialFill` (ring glyphs: the whole arc and the part up to the value), `DialTicks` with `DialTick`, `DialPointer` with its `DialPointerHandle`, and `DialValue`. The variables are `--nz-dial-angle` (the pointer's rotation, for example `transform: rotate(var(--nz-dial-angle))`), `--nz-dial-start-angle`, `--nz-dial-fraction` and, on each tick, `--nz-dial-tick-angle`. |
| `protected override void OnParametersSet()` | Validates the parameters and synchronizes the value. |
| `protected override void AddAttributes(RenderTreeBuilder builder)` | Adds the slider role, the range and the pointer handling.<br>`builder`. The builder that receives the render frames. |
| `protected override void AddContent(RenderTreeBuilder builder)` | Adds the track, the fill, the ticks, the pointer and the value.<br>`builder`. The builder that receives the render frames. |
| `public double ValueAtAngle(double angle)` | Computes the value at a pointer angle, as dragging the handle does.<br>`angle`. The angle in degrees clockwise from twelve o'clock.<br>**Returns**. The value, snapped to the step; angles outside the arc give the nearer end. |
| `public Dial()` |  |
| `public bool WheelChanges { get; set; }` | `[Parameter]` Default false. Unmodified vertical wheel gestures over an enabled dial adjust without taking focus. One notch or 48 logical pixels changes by Step; fractional notches accumulate. With Step zero, one notch changes continuously by 1% of the range. Qualifying gestures are consumed at bounds; unsupported/modified/horizontal-only gestures pass through. Effective changes report ValueChanged, input and change once. |
| `public double Value { get; set; }` | `[Parameter]`<br>Gets or sets the value. |
| `public EventCallback<double> ValueChanged { get; set; }` | `[Parameter]`<br>Gets or sets the callback invoked while the user changes the value. |
| `public double Minimum { get; set; }` | `[Parameter]`<br>Gets or sets the lowest value. |
| `public double Maximum { get; set; }` | `[Parameter]` Initial value `100`.<br>Gets or sets the highest value. |
| `public double Step { get; set; }` | `[Parameter]` Initial value `1`.<br>Gets or sets the change made by an arrow key; values are multiples of it from the minimum, or any value when zero. |
| `public double LargeStep { get; set; }` | `[Parameter]` Initial value `10`.<br>Gets or sets the change made by Page Up and Page Down. |
| `public double StartAngle { get; set; }` | `[Parameter]` Initial value `-135`.<br>Gets or sets the angle of the minimum, in degrees clockwise from twelve o'clock. |
| `public double SweepAngle { get; set; }` | `[Parameter]` Initial value `270`.<br>Gets or sets the angle between the minimum and the maximum, in degrees, from 1 to 360. |
| `public double TickFrequency { get; set; }` | `[Parameter]`<br>Gets or sets the distance between tick marks, in value units, or zero for none. |
| `public bool ShowValue { get; set; }` | `[Parameter]` Initial value `true`.<br>Gets or sets whether the value is written in the middle of the dial. |
| `public string? Format { get; set; }` | `[Parameter]`<br>Gets or sets how the value is written, as `Nazor.Controls.Slider.Format`. |
| `public double DragDistance { get; set; }` | `[Parameter]` Initial value `200`.<br>Gets or sets the pointer distance, in logical units, that covers the whole range when dragging outside the handle. |
| `public ControlSize Size { get; set; }` | `[Parameter]` Initial value `ControlSize.Medium`.<br>Gets or sets the size step. |
| `public bool Disabled { get; set; }` | `[Parameter]`<br>Gets or sets whether the dial rejects interaction. |
| `public FieldContext? Field { get; set; }` | `[CascadingParameter]`<br>Gets or sets the enclosing field, supplied automatically. |
| `public double CurrentValue { get; }` | Gets the current value, kept within the range. |
| `public double Angle { get; }` | Gets the pointer's angle for the current value, in degrees clockwise from twelve o'clock. |
| `public bool IsDragging { get; }` | Gets whether the user is dragging the dial. |
| `protected override string ElementName { get; }` | Gets the NSS element name emitted by Dial. |
| `protected override string? ElementStyle { get; }` | Gets the angle variables and the caller's declarations. |

### `Dialog`

Namespace `Nazor.Controls`. [Source](../../Sources/Core/Nazor.Controls/Overlays/Dialog.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed class Dialog : DialogBase` | A window-local dialog: modal by default, with a header (`Nazor.Controls.Dialog.Leading` content such as a severity mark, the `Nazor.Controls.Dialog.Title` that names the dialog, a `Nazor.Controls.Dialog.Subtitle` that describes it, and a localized close button), a scrolling body (the child content) and a `Nazor.Controls.Dialog.Footer` for the `Nazor.Controls.DialogButton` actions, ordered by role by the theme. Escape and the close button run the Cancel action when there is one, otherwise they dismiss the dialog. Parts: `DialogScrim`, `Dialog` (`size`, `dialog-placement`), `DialogHeader`, `DialogLeading`, `DialogHeading`, `DialogTitle`, `DialogSubtitle`, `DialogClose`, `DialogBody` and `DialogFooter`. |
| `protected override void Validate()` | Validates the size, placement and optional default action role. |
| `protected override void AddSurfaceAttributes(IDictionary<string, object?> attributes)` | Adds the size attribute.<br>`attributes`. The attributes forwarded to the surface. |
| `protected override ElementReference? DefaultInitialFocus()` | Focuses the first control of the body, then the enabled default action or another footer control, before the close button.<br>**Returns**. The element to focus, or null. |
| `protected override void BuildContent(RenderTreeBuilder builder)` | Renders the header, the body and the footer.<br>`builder`. The builder that receives the render frames. |
| `public Dialog()` |  |
| `public string? Title { get; set; }` | `[Parameter]`<br>Gets or sets the title, which names the dialog. |
| `public RenderFragment? TitleContent { get; set; }` | `[Parameter]`<br>Gets or sets rich title content, used instead of `Nazor.Controls.Dialog.Title`. |
| `public string? Subtitle { get; set; }` | `[Parameter]`<br>Gets or sets a line below the title, which describes the dialog. |
| `public RenderFragment? Leading { get; set; }` | `[Parameter]`<br>Gets or sets content shown before the title, such as a severity mark or an icon. |
| `public RenderFragment? ChildContent { get; set; }` | `[Parameter]`<br>Gets or sets the body of the dialog. |
| `public RenderFragment? Footer { get; set; }` | `[Parameter]`<br>Gets or sets the footer, usually `Nazor.Controls.DialogButton` actions. |
| `public DialogSize Size { get; set; }` | `[Parameter]` Initial value `DialogSize.Auto`.<br>Gets or sets the sizing policy; Auto follows the theme, and the other values select explicit width steps. |
| `public DialogButtonRole? DefaultButton { get; set; }` | `[Parameter]`<br>Gets or sets the role used as the dialog's Enter action and automatic accent button; null selects no implicit default. A button's explicit `IsDefault` overrides its own role policy. Multiple effective defaults are invalid. Content that handles Enter suppresses the action; focused buttons handle Enter normally. Initial focus prefers body controls, then the enabled default action, then another footer control. |
| `public DialogPlacement Placement { get; set; }` | `[Parameter]`<br>Gets or sets where the dialog appears; `Nazor.Controls.DialogSize.FullScreen` always covers the window. |
| `public bool Modal { get; set; }` | `[Parameter]` Initial value `true`.<br>Gets or sets whether the dialog confines interaction and the focus and shows a scrim. |
| `public bool ShowCloseButton { get; set; }` | `[Parameter]` Initial value `true`.<br>Gets or sets whether the header shows a close button. |
| `public bool CloseOnOutside { get; set; }` | `[Parameter]`<br>Gets or sets whether a press outside the dialog dismisses it. |
| `protected override string ElementName { get; }` | Gets the NSS element name emitted by Dialog. |
| `protected override DialogPlacement SurfacePlacement { get; }` | Gets the placement, full screen for `Nazor.Controls.DialogSize.FullScreen`. |
| `protected override bool IsModal { get; }` | Gets whether the dialog is modal. |
| `protected override bool DismissOnOutside { get; }` | Gets whether a press outside dismisses the dialog. |
| `protected override bool HasTitle { get; }` | Gets whether the dialog has a title part. |
| `protected override bool HasDescription { get; }` | Gets whether the dialog has a subtitle part. |
| `protected override DialogButtonRole? DefaultActionRole { get; }` | Gets the explicitly selected default role; null means no implicit default action. |

### `DialogBase`

Namespace `Nazor.Controls`. [Source](../../Sources/Core/Nazor.Controls/Overlays/DialogBase.cs)

| Declaration | Contract |
| --- | --- |
| `public abstract class DialogBase : ComponentBase` | The shared behavior of the window-local dialogs (`Nazor.Controls.Dialog`, `Nazor.Controls.AlertDialog`, `Nazor.Controls.Drawer`, `Nazor.Controls.CommandPalette`), all built on `Nazor.Controls.Primitives.DialogContainer`: an open state that supports `@bind-IsOpen`, the `Nazor.Controls.DialogButton` actions that close it and report their role through `Nazor.Controls.DialogBase.OnAction`, and dismissal routed through the Cancel action: Escape and the close button run the enabled `Nazor.Controls.DialogButtonRole.Cancel` button when there is one, and otherwise invoke `Nazor.Controls.DialogBase.OnDismiss` and close when the dialog allows it. |
| `protected override void OnParametersSet()` | Synchronizes the open state and validates the parameters. |
| `protected override void OnAfterRender(bool firstRender)` | Rejects an open dialog with more than one effective default action after its buttons have registered.<br>`firstRender`. Whether this is the first render. |
| `protected virtual void Validate()` | Validates the parameters of a derived dialog. |
| `protected override void BuildRenderTree(RenderTreeBuilder builder)` | Renders the content kept while the dialog is closed and, while open, the dialog surface while open or retained for exit.<br>`builder`. The builder that receives the render frames. |
| `protected virtual void BuildOutside(RenderTreeBuilder builder)` | Renders content that stays while the dialog is closed, such as a shortcut that opens it.<br>`builder`. The builder that receives the render frames. |
| `protected virtual void AddSurfaceAttributes(IDictionary<string, object?> attributes)` | Adds attributes of the surface, such as the size, to the attributes forwarded to it.<br>`attributes`. The attributes forwarded to the surface. |
| `protected abstract void BuildContent(RenderTreeBuilder builder)` | Renders the parts of the dialog inside the surface.<br>`builder`. The builder that receives the render frames. |
| `protected virtual ElementReference? DefaultInitialFocus()` | Chooses the element focused on open when `Nazor.Controls.DialogBase.InitialFocus` is null.<br>**Returns**. The element to focus, or null to let the container choose. |
| `protected static ElementReference? FirstFocusable(params ElementReference[] regions)` | Finds the element marked `autofocus` or, failing that, the first element in Tab order inside regions.<br>`regions`. The regions searched in order.<br>**Returns**. The element to focus, or null. |
| `public Task OpenAsync()` | Opens the dialog.<br>**Returns**. A task that completes when the change has been reported. |
| `public Task CloseAsync()` | Closes the dialog without reporting an action.<br>**Returns**. A task that completes when the change has been reported. |
| `public Task DismissAsync()` | Dismisses the dialog as Escape does: runs the enabled Cancel action when there is one, otherwise invokes `Nazor.Controls.DialogBase.OnDismiss` and closes when the dialog allows dismissal.<br>**Returns**. A task that completes when the dialog has handled the dismissal. |
| `protected Task SetOpenAsync(bool value)` | Changes the open state and reports it.<br>`value`. The new open state.<br>**Returns**. A task that completes when the change has been reported. |
| `protected virtual void OnOpenChanged(bool value)` | Lets a derived dialog reset its state when it opens or closes.<br>`value`. The new open state. |
| `protected DialogBase()` |  |
| `public bool IsOpen { get; set; }` | `[Parameter]`<br>Gets or sets whether the dialog is open. |
| `public EventCallback<bool> IsOpenChanged { get; set; }` | `[Parameter]`<br>Gets or sets the callback invoked when the dialog opens or closes from inside. |
| `public bool CloseOnEscape { get; set; }` | `[Parameter]` Initial value `true`.<br>Gets or sets whether Escape dismisses the dialog. |
| `public EventCallback OnDismiss { get; set; }` | `[Parameter]`<br>Gets or sets the callback invoked when the dialog is dismissed without an action. |
| `public EventCallback<DialogButtonRole> OnAction { get; set; }` | `[Parameter]`<br>Gets or sets the callback invoked with the role of the `Nazor.Controls.DialogButton` that closed the dialog. |
| `public ElementReference? ReturnFocus { get; set; }` | `[Parameter]`<br>Gets or sets the element focused when the dialog closes; null uses the one focused before it opened. |
| `public Func<ElementReference?>? InitialFocus { get; set; }` | `[Parameter]`<br>Gets or sets a callback that chooses the element focused when the dialog opens. |
| `public string? Class { get; set; }` | `[Parameter]`<br>Gets or sets the space-separated NSS classes of the dialog surface while open or retained for exit. |
| `public string? Id { get; set; }` | `[Parameter]`<br>Gets or sets the identifier of the dialog surface while open or retained for exit. |
| `public string? Style { get; set; }` | `[Parameter]`<br>Gets or sets NSS declarations for the dialog surface only. |
| `public string? AccessibleName { get; set; }` | `[Parameter]`<br>Gets or sets the accessible name; the title names the dialog when null. |
| `public string? AccessibleDescription { get; set; }` | `[Parameter]`<br>Gets or sets a longer description for assistive technologies. |
| `public string? LabeledBy { get; set; }` | `[Parameter]`<br>Gets or sets the identifier of the element that names the dialog. |
| `public string? DescribedBy { get; set; }` | `[Parameter]`<br>Gets or sets space-separated identifiers of the elements that describe the dialog. |
| `public IReadOnlyDictionary<string, object?>? AdditionalAttributes { get; set; }` | `[Parameter]`<br>Gets or sets unmatched attributes forwarded to the dialog surface while open or retained for exit. |
| `public bool Opened { get; }` | Gets whether the dialog is open. |
| `protected string TitleId { get; }` | Gets the identifier given to the title part, which names the dialog. |
| `protected string DescriptionId { get; }` | Gets the identifier given to the description part. |
| `protected abstract string ElementName { get; }` | Gets the element name of the surface; parts are named after it. |
| `protected virtual string Role { get; }` | Gets the accessible role of the surface. |
| `protected virtual DialogPlacement SurfacePlacement { get; }` | Gets where the surface appears. |
| `protected virtual bool IsModal { get; }` | Gets whether the dialog confines interaction and the focus. |
| `protected virtual bool? ScrimVisible { get; }` | Gets whether a scrim covers the window, or null to follow `Nazor.Controls.DialogBase.IsModal`. |
| `protected virtual bool DismissOnOutside { get; }` | Gets whether a press outside dismisses the dialog. |
| `protected virtual bool FocusCancel { get; }` | Gets whether the Cancel action receives the focus on open. |
| `protected virtual bool DismissWithoutCancel { get; }` | Gets whether the dialog closes on dismissal when it has no Cancel action. |
| `protected virtual bool HasTitle { get; }` | Gets whether a title part is rendered, so the dialog is labeled by it. |
| `protected virtual bool HasDescription { get; }` | Gets whether a description part is rendered, so the dialog is described by it. |
| `protected virtual DialogButtonRole? DefaultActionRole { get; }` | Gets the role used as the implicit Enter action; Primary by default. Dialog overrides this with its explicit nullable DefaultButton selection. |
| `protected virtual string? SurfaceStyle { get; }` | Gets the style attribute of the surface. |
| `protected bool HasCancel { get; }` | Gets whether the dialog has an enabled Cancel action. |

### `DialogButton`

Namespace `Nazor.Controls`. [Source](../../Sources/Core/Nazor.Controls/Overlays/DialogBase.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed class DialogButton : ContentControl, IDisposable` | An action in the footer of a dialog: a `Nazor.Controls.Button` inside a `DialogButton` element that carries the `action-role` attribute, so themes order the actions (Basic puts Help first and the primary action last), and the `dialog-cancel` mark for the Cancel action. Activation runs `Nazor.Controls.DialogButton.OnClick` and the `Nazor.Controls.DialogButton.Command`, then closes the enclosing dialog and reports the role through its `OnAction`. An action follows the enclosing dialog's default-action policy; the effective default uses the accent appearance unless `Appearance` overrides it. A destructive action uses the danger intent. Without content the button shows the role's usual label (OK, Cancel, Help). |
| `protected override void OnParametersSet()` | Validates the parameters, follows the command and registers with the dialog. |
| `protected override void OnAfterRender(bool firstRender)` | Validates the enclosing dialog's default action after all actions have registered, including an independently updated button.<br>`firstRender`. Whether this is the first render. |
| `protected override void AddAttributes(RenderTreeBuilder builder)` | Adds the role and the cancel mark.<br>`builder`. The builder that receives the render frames. |
| `protected override void AddContent(RenderTreeBuilder builder)` | Adds the button.<br>`builder`. The builder that receives the render frames. |
| `public Task ActivateAsync(PointerEventArgs e)` | Runs the action as a click does and closes the dialog unless `Nazor.Controls.DialogButton.CloseOnClick` is false.<br>`e`. The activation event.<br>**Returns**. A task that completes when the action has run. |
| `public void Dispose()` | Stops following the command and leaves the dialog. |
| `public DialogButton()` |  |
| `public DialogButtonRole Role { get; set; }` | `[Parameter]`<br>Gets or sets the meaning of the action. |
| `public EventCallback<PointerEventArgs> OnClick { get; set; }` | `[Parameter]`<br>Gets or sets the callback invoked when the action runs. |
| `public Command? Command { get; set; }` | `[Parameter]`<br>Gets or sets a command that supplies the label, the enabled state and the work. |
| `public bool Disabled { get; set; }` | `[Parameter]`<br>Gets or sets whether the action rejects interaction. |
| `public bool IsBusy { get; set; }` | `[Parameter]`<br>Gets or sets whether the action shows work in progress; it then rejects activation. |
| `public bool? CloseOnClick { get; set; }` | `[Parameter]`<br>Gets or sets whether running the action closes the dialog; null closes for every role except Help. |
| `public bool? IsDefault { get; set; }` | `[Parameter]`<br>Gets or sets whether this is the default button pressed by Enter; null follows the enclosing dialog's policy, or uses Primary outside a dialog. A dialog rejects multiple effective defaults. |
| `public ControlAppearance? Appearance { get; set; }` | `[Parameter]`<br>Gets or sets the appearance; null uses the accent appearance for the effective default action and the standard one otherwise. |
| `public Intent? Intent { get; set; }` | `[Parameter]`<br>Gets or sets the intent; null uses the danger intent for destructive actions. |
| `public RenderFragment? Leading { get; set; }` | `[Parameter]`<br>Gets or sets content shown before the label. |
| `public DialogBase? Dialog { get; set; }` | `[CascadingParameter]`<br>Gets or sets the enclosing dialog, supplied automatically. |
| `protected override string ElementName { get; }` | Gets the NSS element name emitted by DialogButton. |

### `DialogButtonRole`

Namespace `Nazor.Controls`. [Source](../../Sources/Core/Nazor.Controls/Overlays/DialogBase.cs)

| Declaration | Contract |
| --- | --- |
| `public enum DialogButtonRole` | The meaning of a `Nazor.Controls.DialogButton` in the footer of a dialog, written as the `action-role` attribute (`primary`, `secondary`, `cancel`, `destructive`, `help`) so that each theme orders the buttons by its platform's convention. |
| `Secondary = 0` | An alternative action, such as Don't Save. |
| `Primary = 1` | The main action, such as Save; the dialog's default-action policy or an explicit IsDefault selects its Enter behavior. |
| `Cancel = 2` | The action that abandons the dialog; Escape and the close button use it. |
| `Destructive = 3` | An action that destroys data, such as Delete; drawn with the danger intent. |
| `Help = 4` | An action that explains the dialog without closing it. |

### `DialogSize`

Namespace `Nazor.Controls`. [Source](../../Sources/Core/Nazor.Controls/Overlays/Dialog.cs)

| Declaration | Contract |
| --- | --- |
| `public enum DialogSize` | The sizing policy of a `Nazor.Controls.Dialog`, written as the `size` attribute. |
| `Auto = 4` | The theme's automatic sizing policy; Fluent measures and clamps centered content while Basic and NazorLeaf use their ordinary width. |
| `Small = 0` | A narrow dialog, for a short question or a single field. |
| `Medium = 1` | The ordinary width. |
| `Large = 2` | A wide dialog, for forms and lists. |
| `FullScreen = 3` | Covering the whole window. |

### `Drawer`

Namespace `Nazor.Controls`. [Source](../../Sources/Core/Nazor.Controls/Overlays/Drawer.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed class Drawer : DialogBase` | A panel that slides over the window from one edge: a side drawer (`Nazor.Controls.DrawerPlacement.Start`, `Nazor.Controls.DrawerPlacement.End`) or a sheet (`Nazor.Controls.DrawerPlacement.Top`, `Nazor.Controls.DrawerPlacement.Bottom`) with a header (`Nazor.Controls.Drawer.Title` or `Nazor.Controls.Drawer.Header` and a close button), a scrolling body and a `Nazor.Controls.Drawer.Footer`. A bottom sheet shows a grab `DrawerHandle` by default. `Nazor.Controls.Drawer.Size` sets the width of a side drawer or the height of a sheet. Modal by default with a scrim that dismisses it when pressed. Parts: `DrawerScrim`, `Drawer` (`side`, `dialog-placement`), `DrawerHandle`, `DrawerHeader`, `DrawerTitle`, `DrawerClose`, `DrawerBody` and `DrawerFooter`. |
| `protected override void Validate()` | Validates the placement and the size. |
| `protected override void AddSurfaceAttributes(IDictionary<string, object?> attributes)` | Adds the side attribute.<br>`attributes`. The attributes forwarded to the surface. |
| `protected override ElementReference? DefaultInitialFocus()` | Focuses the first control of the body, then of the footer, before the close button.<br>**Returns**. The element to focus, or null. |
| `protected override void BuildContent(RenderTreeBuilder builder)` | Renders the handle, the header, the body and the footer.<br>`builder`. The builder that receives the render frames. |
| `public Drawer()` |  |
| `public DrawerPlacement Placement { get; set; }` | `[Parameter]` Initial value `DrawerPlacement.End`.<br>Gets or sets the edge the drawer is attached to. |
| `public string? Title { get; set; }` | `[Parameter]`<br>Gets or sets the title, which names the drawer. |
| `public RenderFragment? Header { get; set; }` | `[Parameter]`<br>Gets or sets rich header content, used instead of `Nazor.Controls.Drawer.Title`. |
| `public RenderFragment? ChildContent { get; set; }` | `[Parameter]`<br>Gets or sets the body of the drawer. |
| `public RenderFragment? Footer { get; set; }` | `[Parameter]`<br>Gets or sets the footer, such as `Nazor.Controls.DialogButton` actions. |
| `public double? Size { get; set; }` | `[Parameter]`<br>Gets or sets the width of a side drawer or the height of a sheet, in logical units; null uses the theme's size. |
| `public bool Modal { get; set; }` | `[Parameter]` Initial value `true`.<br>Gets or sets whether the drawer confines interaction and the focus and shows a scrim. |
| `public bool CloseOnOutside { get; set; }` | `[Parameter]` Initial value `true`.<br>Gets or sets whether a press outside the drawer dismisses it. |
| `public bool ShowCloseButton { get; set; }` | `[Parameter]` Initial value `true`.<br>Gets or sets whether the header shows a close button. |
| `public bool? ShowHandle { get; set; }` | `[Parameter]`<br>Gets or sets whether a grab handle is shown; null shows it on bottom sheets. |
| `protected override string ElementName { get; }` | Gets the NSS element name emitted by Drawer. |
| `protected override DialogPlacement SurfacePlacement { get; }` | Gets the dialog placement of the edge. |
| `protected override bool IsModal { get; }` | Gets whether the drawer is modal. |
| `protected override bool DismissOnOutside { get; }` | Gets whether a press outside dismisses the drawer. |
| `protected override bool HasTitle { get; }` | Gets whether the drawer has a title part. |
| `protected override string? SurfaceStyle { get; }` | Gets the size declaration and the caller's declarations. |

### `DrawerPlacement`

Namespace `Nazor.Controls`. [Source](../../Sources/Core/Nazor.Controls/Overlays/Drawer.cs)

| Declaration | Contract |
| --- | --- |
| `public enum DrawerPlacement` | The window edge a `Nazor.Controls.Drawer` is attached to, written as the `side` attribute. |
| `Start = 0` | The leading edge, as a navigation drawer. |
| `End = 1` | The trailing edge, as a details or settings panel. |
| `Top = 2` | The top edge. |
| `Bottom = 3` | The bottom edge, as a bottom sheet. |

### `DropDownPlacement`

Namespace `Nazor.Controls`. [Source](../../Sources/Core/Nazor.Controls/Fields/DropDownPlacement.cs)

| Declaration | Contract |
| --- | --- |
| `public enum DropDownPlacement` | The preferred vertical position of a choice list relative to its field. |
| `Auto = 0` | Uses the space available above and below the field, preferring below when the list fits. |
| `Top = 1` | Prefers opening above the field. |
| `Bottom = 2` | Prefers opening below the field. |
| `Center = 3` | Overlays the field, aligning the selected option where possible or centering the list when unset. Window/work-area bounds can shift and constrain it. |

### `EmptyState`

Namespace `Nazor.Controls`. [Source](../../Sources/Core/Nazor.Controls/Feedback/EmptyState.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed class EmptyState : ContentControl` | What a view shows when it has nothing to show: no search results, an empty folder, a first run. The parts are `EmptyStateIllustration` (`Nazor.Controls.EmptyState.Illustration`), `EmptyStateTitle` (a heading of `Nazor.Controls.EmptyState.TitleLevel`), `EmptyStateDescription` (`Nazor.Controls.EmptyState.Description`), the content between the tags and `EmptyStateActions` (`Nazor.Controls.EmptyState.Actions`). The element is a group named by its title. |
| `protected override void OnParametersSet()` | Validates the heading level. |
| `protected override void AddAttributes(RenderTreeBuilder builder)` | Adds the group role and its name.<br>`builder`. The builder that receives the render frames. |
| `protected override void AddContent(RenderTreeBuilder builder)` | Adds the illustration, the title, the description, the content and the actions.<br>`builder`. The builder that receives the render frames. |
| `public EmptyState()` |  |
| `public RenderFragment? Illustration { get; set; }` | `[Parameter]`<br>Gets or sets a picture above the title, such as an `Nazor.Controls.Image` or a large `Nazor.Controls.Icon`. |
| `public string? Title { get; set; }` | `[Parameter]`<br>Gets or sets the title, which also names the group. |
| `public int TitleLevel { get; set; }` | `[Parameter]` Initial value `2`.<br>Gets or sets the heading level of the title, from 1 to 6; 2 by default. |
| `public RenderFragment? Description { get; set; }` | `[Parameter]`<br>Gets or sets the explanation below the title, such as what to do next. |
| `public RenderFragment? Actions { get; set; }` | `[Parameter]`<br>Gets or sets the actions that leave the empty state, such as Create or Clear filters. |
| `protected override string ElementName { get; }` | Gets the NSS element name emitted by EmptyState. |

### `EventHandlers`

Namespace `Nazor.Controls`. [Source](../../Sources/Core/Nazor.Controls/Control.cs)

| Declaration | Contract |
| --- | --- |
| `public static class EventHandlers` | Directive event attributes available on Nazor controls, for example `@onclick`. |

### `ExpandDirection`

Namespace `Nazor.Controls`. [Source](../../Sources/Core/Nazor.Controls/Layout/Expander.cs)

| Declaration | Contract |
| --- | --- |
| `public enum ExpandDirection` | Side of the header where disclosure content appears. |
| `Down = 0` | Content below the header. |
| `Up = 1` | Content above the header. |

### `Expander`

Namespace `Nazor.Controls`. [Source](../../Sources/Core/Nazor.Controls/Layout/Expander.cs)

| Declaration | Contract |
| --- | --- |
| `public class Expander : ContentControl` | A header that shows and hides the content below it, such as advanced settings: a click on the header, Enter or Space toggle it, and `Nazor.Controls.Expander.IsExpanded` supports `@bind-IsExpanded`. The header shows `Nazor.Controls.Expander.Leading` content, the title (`Nazor.Controls.Expander.Header` or `Nazor.Controls.Expander.HeaderContent`) with an optional `Nazor.Controls.Expander.Subtitle`, `Nazor.Controls.Expander.Trailing` content and a chevron. The parts are `{Name}Header` (a button with the `expanded` state that controls the panel), `{Name}Leading`, `{Name}HeaderText`, `{Name}Title`, `{Name}Subtitle`, `{Name}Trailing`, `{Name}Chevron` (a `chevron-down` or `chevron-up` glyph) and the `Nazor.Controls.Primitives.Collapsible``{Name}Panel` with its `{Name}PanelContent`, where `{Name}` is `Expander` (or `AccordionItem` in an accordion). The element has the `expanded` and `disabled` attributes. |
| `protected override void OnParametersSet()` | Synchronizes the expanded state with the parameter. |
| `protected override void AddAttributes(RenderTreeBuilder builder)` | Adds the expanded and disabled states.<br>`builder`. The builder that receives the render frames. |
| `protected override void AddContent(RenderTreeBuilder builder)` | Adds the header and the collapsible panel.<br>`builder`. The builder that receives the render frames. |
| `protected virtual void AddHeaderAttributes(RenderTreeBuilder builder)` | Adds attributes of a derived control to the header element, with sequence numbers 1008.<br>`builder`. The builder that receives the render frames. |
| `public virtual Task ToggleAsync()` | Expands the content when collapsed and collapses it when expanded, as a click on the header does, returning hidden descendant focus to the header.<br>**Returns**. A task that completes when the change has been reported. |
| `public virtual Task SetExpandedAsync(bool value)` | Expands or collapses the content as the user does and reports the change.<br>`value`. Whether the content should be shown.<br>**Returns**. A task that completes when the change has been reported. |
| `public Expander()` |  |
| `public string? Header { get; set; }` | `[Parameter]`<br>Gets or sets the title of the header as plain text. |
| `public RenderFragment? HeaderContent { get; set; }` | `[Parameter]`<br>Gets or sets the title of the header as content; it replaces `Nazor.Controls.Expander.Header`. |
| `public string? Subtitle { get; set; }` | `[Parameter]`<br>Gets or sets a secondary line below the title. |
| `public RenderFragment? Leading { get; set; }` | `[Parameter]`<br>Gets or sets content shown at the start of the header, usually an `Nazor.Controls.Icon`. |
| `public RenderFragment? Trailing { get; set; }` | `[Parameter]`<br>Gets or sets content shown at the end of the header before the chevron, such as a value or a switch; its own controls keep their actions. |
| `public ExpandDirection ExpandDirection { get; set; }` | `[Parameter]` Content side; `Down` by default. Invalid enum values throw. |
| `public bool KeepMounted { get; set; }` | `[Parameter]` False by default; true preserves disabled, semantically hidden closed content and local state after exit completion. |
| `public bool IsExpanded { get; set; }` | `[Parameter]`<br>Gets or sets whether the content is shown. |
| `public EventCallback<bool> IsExpandedChanged { get; set; }` | `[Parameter]`<br>Gets or sets the callback invoked when the user expands or collapses the content. |
| `public bool Disabled { get; set; }` | `[Parameter]`<br>Gets or sets whether the header rejects interaction; the content is disabled too. |
| `public virtual bool Expanded { get; }` | Gets whether the content is currently shown. |
| `public string HeaderId { get; }` | Gets the identifier of the header, which names the panel. |
| `public ElementReference HeaderElement { get; }` | Gets a reference to the header element, the focus target of the expander. |
| `protected virtual bool IsInactive { get; }` | Gets whether the expander currently rejects interaction. |
| `protected override string ElementName { get; }` | Gets the NSS element name emitted by Expander. |

The chevron part retains the same `chevron-down` glyph; theme rotation expresses expanded state and direction. `AccordionItem` inherits direction and kept-content behavior. Parts `{Name}Panel` / `{Name}PanelContent` follow the Collapsible close-phase and measured-reveal contract.

### `Field`

Namespace `Nazor.Controls`. [Source](../../Sources/Core/Nazor.Controls/Fields/Field.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed class Field : ContentControl` | Gathers a control with its label, description and error, and connects them for assistive technologies: the label names the control, the description and the error describe it, a click on the label activates it. The field owns no validation; set `Nazor.Controls.Field.Invalid` and `Nazor.Controls.Field.Error` from the application. The parts are a `Nazor.Controls.Label` (with its required mark), `FieldControl`, `FieldDescription` and `FieldError`. |
| `protected override void OnParametersSet()` | Updates the shared context. |
| `protected override void AddAttributes(RenderTreeBuilder builder)` | Adds the field markers and states.<br>`builder`. The builder that receives the render frames. |
| `protected override void AddContent(RenderTreeBuilder builder)` | Adds the label, the control, the description and the error.<br>`builder`. The builder that receives the render frames. |
| `public Field()` |  |
| `public RenderFragment? Label { get; set; }` | `[Parameter]`<br>Gets or sets the label content. |
| `public string? LabelText { get; set; }` | `[Parameter]`<br>Gets or sets the label text, used when `Nazor.Controls.Field.Label` is not set. |
| `public RenderFragment? Description { get; set; }` | `[Parameter]`<br>Gets or sets help shown with the control. |
| `public RenderFragment? Error { get; set; }` | `[Parameter]`<br>Gets or sets the error shown while `Nazor.Controls.Field.Invalid` is true. |
| `public bool Required { get; set; }` | `[Parameter]`<br>Gets or sets whether a value is required; the label shows a mark. |
| `public bool Invalid { get; set; }` | `[Parameter]`<br>Gets or sets whether the application considers the value invalid. |
| `public bool Disabled { get; set; }` | `[Parameter]`<br>Gets or sets whether the field and its control are disabled. |
| `public FieldLayout? Layout { get; set; }` | `[Parameter]`<br>Gets or sets where the label is placed; null uses the enclosing `Nazor.Controls.FormLayout`'s default. |
| `public FormLayout? Form { get; set; }` | `[CascadingParameter]`<br>Gets or sets the enclosing form layout, supplied automatically. |
| `public FieldContext Context { get; }` | Gets the context shared with the control. |
| `protected override string ElementName { get; }` | Gets the NSS element name emitted by Field. |

### `FieldAppearance`

Namespace `Nazor.Controls`. [Source](../../Sources/Core/Nazor.Controls/Fields/TextField.cs)

| Declaration | Contract |
| --- | --- |
| `public enum FieldAppearance` | How a text field draws its frame, written as the `appearance` attribute of fields. |
| `Outline = 0` | A full outline around the field. |
| `Filled = 1` | A tinted background without outline. |
| `Underline = 2` | Only a line under the text. |
| `Plain = 3` | No frame, for fields embedded in other surfaces such as a grid cell. |

### `FieldContext`

Namespace `Nazor.Controls`. [Source](../../Sources/Core/Nazor.Controls/Fields/Field.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed class FieldContext` | What a `Nazor.Controls.Field` tells the control inside it: the identifiers that connect the control to the label, the description and the error, and the required, invalid and disabled states. Controls of this library read it automatically; a custom control can receive it as a cascading parameter and apply `Nazor.Controls.FieldContext.ApplyTo(Nazor.Rendering.RenderTreeBuilder,System.Int32,System.String,System.String)`. |
| `public void ApplyTo(RenderTreeBuilder builder, int sequence, string? labeledBy = null, string? describedBy = null)` | Writes the labeling and description relationships on a control's root element, unless the control set its own; the control combines `Nazor.Controls.FieldContext.Required` and `Nazor.Controls.FieldContext.Invalid` with its own states.<br>`builder`. The builder positioned on the control's element.<br>`sequence`. The first of two sequence numbers to use.<br>`labeledBy`. The control's own labeling target, which takes precedence.<br>`describedBy`. The control's own description targets, which take precedence. |
| `public string Id { get; }` | Gets the stable identifier of the field; the parts' identifiers derive from it. |
| `public string ControlId { get; }` | Gets the identifier given to the field's main control when it has none of its own. |
| `public string LabelId { get; }` | Gets the identifier of the label part. |
| `public string DescriptionId { get; }` | Gets the identifier of the description part. |
| `public string ErrorId { get; }` | Gets the identifier of the error part. |
| `public bool HasLabel { get; }` | Gets whether the field has a label. |
| `public bool HasDescription { get; }` | Gets whether the field has a description. |
| `public bool HasError { get; }` | Gets whether the field shows an error. |
| `public bool Required { get; }` | Gets whether the field requires a value. |
| `public bool Invalid { get; }` | Gets whether the application considers the value invalid. |
| `public bool Disabled { get; }` | Gets whether the field is disabled. |
| `public string? DescribedBy { get; }` | Gets the space-separated identifiers of the description and the error, or null. |

### `FieldLayout`

Namespace `Nazor.Controls`. [Source](../../Sources/Core/Nazor.Controls/Fields/Field.cs)

| Declaration | Contract |
| --- | --- |
| `public enum FieldLayout` | Where a `Nazor.Controls.Field` places its label. |
| `Stacked = 0` | The label above the control. |
| `Inline = 1` | The label beside the control, in a column shared by a `Nazor.Controls.FormLayout`. |

### `Figure`

Namespace `Nazor.Controls`. [Source](../../Sources/Core/Nazor.Controls/Text/Figure.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed class Figure : ContentControl` | Self-contained content referenced from the text, such as an image, a chart, a code listing or a table, with an optional `Nazor.Controls.FigureCaption` before or after it. The element is `Figure`, a group described as a figure and named by its caption for assistive technologies. |
| `protected override void AddAttributes(RenderTreeBuilder builder)` | Adds the group role described as a figure and the caption relationship.<br>`builder`. The builder that receives the render frames. |
| `protected override void AddContent(RenderTreeBuilder builder)` | Supplies the figure to its caption.<br>`builder`. The builder that receives the render frames. |
| `public Figure()` |  |
| `public string? CaptionId { get; }` | Gets the identifier of the caption that names the figure, or null without a caption. |
| `protected override string ElementName { get; }` | Gets the NSS element name emitted by Figure. |

### `FigureCaption`

Namespace `Nazor.Controls`. [Source](../../Sources/Core/Nazor.Controls/Text/Figure.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed class FigureCaption : ContentControl, IDisposable` | The caption of the enclosing `Nazor.Controls.FigureCaption.Figure`, which it names for assistive technologies. Place it before or after the figure content. The element is `FigureCaption` with the `text` role. |
| `protected override void OnParametersSet()` | Names the enclosing figure. |
| `protected override void AddAttributes(RenderTreeBuilder builder)` | Adds the identity and the text role.<br>`builder`. The builder that receives the render frames. |
| `public void Dispose()` | Stops naming the figure. |
| `public FigureCaption()` |  |
| `public Figure? Figure { get; set; }` | `[CascadingParameter]`<br>Gets or sets the enclosing figure, supplied automatically. |
| `protected override string ElementName { get; }` | Gets the NSS element name emitted by FigureCaption. |

### `FileField`

Namespace `Nazor.Controls`. [Source](../../Sources/Core/Nazor.Controls/Fields/FileField.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed class FileField : Control` | A field showing the chosen file paths with a Browse button. The controls cannot open native dialogs, so the application supplies `Nazor.Controls.FileField.Pick` (for example with the file dialogs of Nazor.Application) and the field shows what it returns: `Nazor.Controls.FileField.Paths` (`@bind-Paths`) lists the chosen paths, one unless `Nazor.Controls.FileField.Multiple`. The Browse button is the field's Tab stop and is named by the enclosing `Nazor.Controls.FileField.Field`; Delete or Backspace on it clear the paths, as the clear button does. The parts are `FileFieldValue` (with `placeholder` when empty, the file names or "n files", the full paths as tooltip), `FileFieldClear` and `FileFieldBrowse` (with `busy` while the picker is open). |
| `protected override void OnParametersSet()` | Validates the parameters and synchronizes the paths. |
| `public string? DisplayText()` | Gets the text shown for the chosen paths, or null when there are none.<br>**Returns**. The text. |
| `protected override void AddAttributes(RenderTreeBuilder builder)` | Adds the frame states.<br>`builder`. The builder that receives the render frames. |
| `protected override void AddContent(RenderTreeBuilder builder)` | Adds the value, the clear button and the Browse button.<br>`builder`. The builder that receives the render frames. |
| `public Task BrowseAsync()` | Opens the application's picker as the Browse button does and keeps what it returns.<br>**Returns**. A task that completes when the picker has closed and the change has been reported. |
| `public Task ClearAsync()` | Clears the chosen paths as the clear button does.<br>**Returns**. A task that completes when the change has been reported. |
| `public FileField()` |  |
| `public IReadOnlyList<string>? Paths { get; set; }` | `[Parameter]`<br>Gets or sets the chosen paths. |
| `public EventCallback<IReadOnlyList<string>> PathsChanged { get; set; }` | `[Parameter]`<br>Gets or sets the callback invoked when the user picks or clears paths; supports `@bind-Paths`. |
| `public Func<Task<IReadOnlyList<string>?>>? Pick { get; set; }` | `[Parameter]`<br>Gets or sets the application's picker: it shows a dialog and returns the chosen paths, or null when the user cancels. Without it the Browse button is disabled. |
| `public bool Multiple { get; set; }` | `[Parameter]`<br>Gets or sets whether several paths can be chosen; otherwise only the first path returned is kept. |
| `public bool ShowFullPath { get; set; }` | `[Parameter]`<br>Gets or sets whether full paths are shown instead of file names. |
| `public string? Placeholder { get; set; }` | `[Parameter]`<br>Gets or sets the text shown while no file is chosen; "No file selected" by default. |
| `public string? BrowseText { get; set; }` | `[Parameter]`<br>Gets or sets the text of the Browse button; "Browse" by default. |
| `public bool Clearable { get; set; }` | `[Parameter]` Initial value `true`.<br>Gets or sets whether a button clears the chosen paths; true by default. |
| `public bool Disabled { get; set; }` | `[Parameter]`<br>Gets or sets whether the field rejects interaction. |
| `public bool Required { get; set; }` | `[Parameter]`<br>Gets or sets whether a file is required; validation is application-owned. |
| `public bool Invalid { get; set; }` | `[Parameter]`<br>Gets or sets the externally determined invalid state. |
| `public FieldAppearance Appearance { get; set; }` | `[Parameter]`<br>Gets or sets how the frame is drawn. |
| `public ControlSize Size { get; set; }` | `[Parameter]` Initial value `ControlSize.Medium`.<br>Gets or sets the size step. |
| `public FieldContext? Field { get; set; }` | `[CascadingParameter]`<br>Gets or sets the enclosing field, supplied automatically. |
| `public IReadOnlyList<string> CurrentPaths { get; }` | Gets the chosen paths. |
| `public bool IsPicking { get; }` | Gets whether the picker is open. |
| `protected override string ElementName { get; }` | Gets the NSS element name emitted by FileField. |

### `FlipView<TItem>`

Namespace `Nazor.Controls`. [Source](../../Sources/Core/Nazor.Controls/Navigation/FlipView.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed class FlipView<TItem> : Control` | Browses a collection one item at a time. `SelectedIndex` (`@bind-SelectedIndex`) is zero-based; the first item is selected by default and `CurrentIndex` is -1 when empty. Only the selected `ItemTemplate` is realized, so navigating away disposes its local component state; keep persistent state in the data model. Selection follows the index when items are reordered and is clamped when items are removed. Left/Right, Home/End navigate while the FlipView itself has focus; descendant controls keep their keyboard behavior. Previous/Next buttons and an optional horizontal primary-pointer swipe navigate without wrapping or autoplay. Swipes starting on interactive descendants are ignored. Parts are `FlipViewViewport`, `FlipViewItem`, `FlipViewPrevious`, `FlipViewNext` and the polite position announcement `FlipViewStatus`.<br>`TItem`. The type of the displayed items. |
| `public FlipView()` | Creates an instance with the documented default parameter values. |
| `public IReadOnlyList<TItem> Items { get; set; }` | `[Parameter]`<br>Gets or sets the items in display order. The collection cannot be null. Initial value `[]`. |
| `public RenderFragment<TItem>? ItemTemplate { get; set; }` | `[Parameter]`<br>Gets or sets the selected item's content; null displays its string representation. |
| `public int SelectedIndex { get; set; }` | `[Parameter]`<br>Gets or sets the selected zero-based index, clamped to the available items; an empty collection has index -1. |
| `public EventCallback<int> SelectedIndexChanged { get; set; }` | `[Parameter]`<br>Gets or sets the callback invoked once when user navigation selects a different item. Parameter or collection normalization does not invoke it. |
| `public bool Disabled { get; set; }` | `[Parameter]`<br>Gets or sets whether the control rejects navigation and interaction with its content. |
| `public bool SwipeEnabled { get; set; }` | `[Parameter]`<br>Gets or sets whether a horizontal primary-pointer drag on noninteractive content can select an adjacent item; true by default. Initial value `true`. |
| `public int CurrentIndex { get; }` | Gets the selected index, or -1 while there are no items. |
| `protected override string ElementName { get; }` | Gets the NSS element name emitted by FlipView. |
| `protected override void OnParametersSet()` | Validates the collection, synchronizes selection and cancels unavailable gestures. |
| `protected override void AddAttributes(RenderTreeBuilder builder)` | Adds group semantics, interaction states and keyboard navigation.<br>`builder`. The builder receiving the attributes. |
| `protected override void AddContent(RenderTreeBuilder builder)` | Renders the current item, navigation buttons and localized position announcement.<br>`builder`. The builder receiving the content. |
| `protected override void OnAfterRender(bool firstRender)` | Returns focus to the FlipView after focused content is removed or a navigation button reaches its bound.<br>`firstRender`. Whether this is the first render. |
| `public Task SelectAsync(int position)` | Selects an available index and reports the change; disabled, repeated or out-of-range requests do nothing.<br>`position`. The requested zero-based index.<br>**Returns**. A task completed after selection callbacks and input/change events have been reported. |
| `public Task PreviousAsync()` | Selects the previous item, stopping at the first one.<br>**Returns**. A task completed after the selection change has been reported. |
| `public Task NextAsync()` | Selects the next item, stopping at the last one.<br>**Returns**. A task completed after the selection change has been reported. |

`FlipView<TItem>` renders only the selected template. Navigating away disposes that template's components; keep persistent edits in the item model. Selection is index-based, so reordering preserves the index rather than tracking item identity. Empty collections report `CurrentIndex == -1`; normalizing parameters or a shrinking collection does not raise `SelectedIndexChanged`. There is no wrapping, autoplay or built-in slide transition.

The focusable group exposes a localized carousel role description; its selected item has one-based position/count metadata and `FlipViewStatus` announces position politely. Root-only Left/Right/Home/End navigation preserves descendant editors and already-handled keys. Previous/Next actions have localized names and stop at the bounds. `SwipeEnabled` permits a horizontal primary-pointer drag of at least 36 logical units on noninteractive content, with capture checks; this also works for hosts translating touch into primary-pointer events, without assuming native touch classification.

### `FloatingActionButton`

Namespace `Nazor.Controls`. [Source](../../Sources/Core/Nazor.Controls/Actions/FloatingActionButton.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed class FloatingActionButton : ButtonBase` | The main action of a view floating above its content, such as Compose in a mail application: an icon (`Nazor.Controls.ButtonBase.Leading`) and, when there is content, a label that makes it extended (the `extended` attribute). It runs `Nazor.Controls.ButtonBase.OnClick` and the optional `Nazor.Controls.ButtonBase.Command`; without content the command's text is its accessible name and tooltip instead of a label. The `placement` attribute (`bottom-end`, `bottom-start`, `top-end`, `top-start`) tells themes which corner of the nearest positioned container it floats in. It has the button role and the parts of `Nazor.Controls.ButtonBase`. |
| `protected override void OnParametersSet()` | Validates the parameters. |
| `protected override void AddAttributes(RenderTreeBuilder builder)` | Adds the button role, the extended state, the placement and the activation.<br>`builder`. The builder that receives the render frames. |
| `public FloatingActionButton()` |  |
| `public FloatingActionButtonPlacement Placement { get; set; }` | `[Parameter]`<br>Gets or sets the corner where the button floats. |
| `protected override string ElementName { get; }` | Gets the NSS element name emitted by FloatingActionButton. |
| `protected override bool HasLabel { get; }` | Gets whether a label is shown: with content, or with a command and no icon. |

### `FloatingActionButtonPlacement`

Namespace `Nazor.Controls`. [Source](../../Sources/Core/Nazor.Controls/Actions/FloatingActionButton.cs)

| Declaration | Contract |
| --- | --- |
| `public enum FloatingActionButtonPlacement` | The corner of its positioned container where a `Nazor.Controls.FloatingActionButton` floats. |
| `BottomEnd = 0` | The bottom corner on the trailing side, written as `bottom-end`. |
| `BottomStart = 1` | The bottom corner on the leading side, written as `bottom-start`. |
| `TopEnd = 2` | The top corner on the trailing side, written as `top-end`. |
| `TopStart = 3` | The top corner on the leading side, written as `top-start`. |

### `Form`

Namespace `Nazor.Controls`. [Source](../../Sources/Core/Nazor.Controls/Fields/Field.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed class Form : ContentControl` | A submission scope: Enter in a field, or a `Nazor.Controls.Button` with `Nazor.Controls.ButtonType.Submit`, calls `Nazor.Controls.Form.OnSubmit` when no default button handles it; a reset button calls `Nazor.Controls.Form.OnReset`. The application owns the model, the validation and the reset of the values. |
| `protected override void AddAttributes(RenderTreeBuilder builder)` | Adds the form role and the scoped callbacks.<br>`builder`. The builder that receives the render frames. |
| `public Task SubmitAsync()` | Enables feedback on this form before requesting submission; validation and values remain application-owned.<br>**Returns**. A task that completes when the callback has finished. |
| `public Task ResetAsync()` | Clears feedback eligibility, excluding nested forms, before requesting the application-owned reset of values and validity.<br>**Returns**. A task that completes when the callback has finished. |
| `public Form()` |  |
| `public EventCallback OnSubmit { get; set; }` | `[Parameter]`<br>Gets or sets the submission callback. |
| `public EventCallback OnReset { get; set; }` | `[Parameter]`<br>Gets or sets the callback requesting the application-owned reset of the values. |
| `protected override string ElementName { get; }` | Gets the NSS element name emitted by Form. |

### `FormLayout`

Namespace `Nazor.Controls`. [Source](../../Sources/Core/Nazor.Controls/Fields/Field.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed class FormLayout : ContentControl` | Lays out a column of `Nazor.Controls.Field` controls. Inline fields share the label column, whose width is `Nazor.Controls.FormLayout.LabelWidth` (the `--nz-form-label-width` variable); stacked fields span the whole row. |
| `protected override void OnParametersSet()` | Validates the label width. |
| `protected override void AddAttributes(RenderTreeBuilder builder)` | Adds the default layout of the fields.<br>`builder`. The builder that receives the render frames. |
| `protected override void AddContent(RenderTreeBuilder builder)` | Supplies the default layout to the fields.<br>`builder`. The builder that receives the render frames. |
| `public FormLayout()` |  |
| `public double? LabelWidth { get; set; }` | `[Parameter]`<br>Gets or sets the width of the label column of inline fields, in logical units, or null for the theme's. |
| `public FieldLayout DefaultLayout { get; set; }` | `[Parameter]`<br>Gets or sets where the fields inside place their labels unless they choose otherwise. |
| `protected override string ElementName { get; }` | Gets the NSS element name emitted by FormLayout. |
| `protected override string? ElementStyle { get; }` | Gets the label column width variable and the caller's declarations. |

### `Frame`

Namespace `Nazor.Controls`. [Source](../../Sources/Core/Nazor.Controls/Navigation/Frame.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed class Frame : Control, IDisposable` | Shows the page of the current place of the window's `Nazor.NavigationManager`: the page whose `@page` route matches the address, with the route values as its parameters, or the page reached by type. A new place creates a new page. Pages are looked for in `Nazor.Controls.Frame.Assemblies` (the application by default); without one, `Nazor.Controls.Frame.NotFound` shows. Alt+Left and Alt+Right, the Back and Forward keys and the side buttons of the mouse go back and forward. `FrameTransition` and `FrameTransitionLayer` retain at most one outgoing page, disabled and semantically hidden until the theme exit finishes. Local state and subscriptions live until disposal; use owner-scoped page guards. Focus moves only when outgoing content held it. |
| `protected override void OnInitialized()` | Initializes component state before the first parameter update completes. |
| `protected override void OnParametersSet()` | Updates component state after parameter values have been assigned. |
| `protected override void AddContent(RenderTreeBuilder builder)` | Adds the child render frames used by Frame.<br>`builder`. The builder that receives the render frames. |
| `public void Dispose()` | Releases the resources and event subscriptions owned by this instance. |
| `public Frame()` |  |
| `public NavigationManager Navigation { get; set; }` | `[Inject]`<br>Gets or sets the injected navigation manager for this window. |
| `public KeyBindings Keys { get; set; }` | `[Inject]`<br>Gets or sets the injected keyboard command registry. |
| `public IEnumerable<Assembly>? Assemblies { get; set; }` | `[Parameter]`<br>Where the pages are; the entry assembly of the application by default. |
| `public ContentTransitionKind Transition { get; set; }` | `[Parameter]` Page motion request; `Entrance` by default. Back navigation reverses direction and drill kind. The first page is immediate. |
| `public RenderFragment? NotFound { get; set; }` | `[Parameter]`<br>What shows for an address without a page. |
| `protected override string ElementName { get; }` | Gets the NSS element name emitted by Frame. |

### `GridView<TItem>`

Namespace `Nazor.Controls`. [Source](../../Sources/Core/Nazor.Controls/Collections/GridView.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed class GridView<TItem> : Control` | A virtualized grid of tiles, such as files, photos or products: as many columns of `Nazor.Controls.GridView<TItem>.ItemWidth` as fit in its width, rows of `Nazor.Controls.GridView<TItem>.ItemHeight`, and only the visible rows realized. It is a single Tab stop: the four arrows move in two dimensions, Home and End go to the ends of the row (with Ctrl to the ends of the grid), Page Up and Page Down move by a screen, typing finds a tile, Space toggles, Ctrl+A selects all, Shift extends and Ctrl moves without selecting; Enter or a double click raise `Nazor.Controls.GridView<TItem>.OnItemActivated`. `Nazor.Controls.GridView<TItem>.SelectedItem` and `Nazor.Controls.GridView<TItem>.SelectedItems` support `@bind-`. The element `GridView` (role `listbox`) holds the scrolling `GridViewViewport` (a grid whose columns are written in its style; it needs a height, given by the theme or a class) with the tiles `GridViewItem` (role `option`, with `GridViewItemCheck` and `GridViewItemContent`); `GridViewEmpty` replaces it when there are no items.<br>`TItem`. The type of the items. |
| `public GridView()` | Creates a grid view. |
| `protected override void OnParametersSet()` | Validates the parameters and synchronizes the items and the selection. |
| `public void Refresh()` | Reads the items again, after the list passed to `Nazor.Controls.GridView<TItem>.Items` was changed in place. |
| `public bool ScrollIntoView(TItem item)` | Scrolls so that an item is visible and makes it the current tile.<br>`item`. The item.<br>**Returns**. true if the item is in the grid. |
| `protected override void AddAttributes(RenderTreeBuilder builder)` | Adds the list role, the selection mode, the size listener and the keyboard handling.<br>`builder`. The builder that receives the render frames. |
| `protected override void AddContent(RenderTreeBuilder builder)` | Adds the viewport with the realized tiles, or the empty content.<br>`builder`. The builder that receives the render frames. |
| `protected override Task OnAfterRenderAsync(bool firstRender)` | Moves the focus to the current tile once it is realized.<br>`firstRender`. Whether this is the first render.<br>**Returns**. A task that completes when the work is done. |
| `public IReadOnlyList<TItem> Items { get; set; }` | `[Parameter]` Initial value `[]`.<br>Gets or sets the items, laid out from left to right and from top to bottom. |
| `public RenderFragment<TItem>? ItemTemplate { get; set; }` | `[Parameter]`<br>Gets or sets how each tile is rendered; `Nazor.Controls.GridView<TItem>.ItemText` is shown when null. |
| `public Func<TItem, string>? ItemText { get; set; }` | `[Parameter]`<br>Gets or sets the text of an item, shown without a template, matched by typing and used as the accessible name; `ToString` is used when null. |
| `public Func<TItem, object>? ItemKey { get; set; }` | `[Parameter]`<br>Gets or sets a function that gives each item a stable key for selection and rendering; the item itself is used when null. |
| `public Func<TItem, bool>? ItemDisabled { get; set; }` | `[Parameter]`<br>Gets or sets a function that tells whether an item is disabled: it is skipped by the keyboard and cannot be selected. |
| `public double ItemWidth { get; set; }` | `[Parameter]` Initial value `120`.<br>Gets or sets the minimum width of a column; the columns share the remaining width. |
| `public double ItemHeight { get; set; }` | `[Parameter]` Initial value `120`.<br>Gets or sets the height of a row of tiles. |
| `public SelectionMode SelectionMode { get; set; }` | `[Parameter]` Initial value `SelectionMode.Single`.<br>Gets or sets how many items can be selected. |
| `public TItem? SelectedItem { get; set; }` | `[Parameter]`<br>Gets or sets the selected item in single selection. |
| `public EventCallback<TItem?> SelectedItemChanged { get; set; }` | `[Parameter]`<br>Gets or sets the callback invoked when the user selects another item in single selection. |
| `public IReadOnlyCollection<TItem>? SelectedItems { get; set; }` | `[Parameter]`<br>Gets or sets the selected items in multiple selection. |
| `public EventCallback<IReadOnlyCollection<TItem>> SelectedItemsChanged { get; set; }` | `[Parameter]`<br>Gets or sets the callback invoked with the selected items, in display order, when the user changes a multiple selection. |
| `public bool ShowCheckBoxes { get; set; }` | `[Parameter]`<br>Gets or sets whether tiles show a check mark in multiple selection; a click then toggles the tile. |
| `public EventCallback<TItem> OnItemActivated { get; set; }` | `[Parameter]`<br>Gets or sets the callback invoked when a tile is activated with Enter or a double click. |
| `public bool CanReorderItems { get; set; }` | `[Parameter]` Opt-in primary drag or Ctrl/Meta+Shift+arrows reordering within the tile collection; requires `OnItemReorder`. |
| `public EventCallback<ItemReorderRequest<TItem>> OnItemReorder { get; set; }` | `[Parameter]` Requests one move; the application supplies updated items. Selection remains keyed to the same item. |
| `public RenderFragment? EmptyContent { get; set; }` | `[Parameter]`<br>Gets or sets the content shown when there are no items; a localized "No results" when null. |
| `public EventCallback<VirtualRange> OnRangeChanged { get; set; }` | `[Parameter]`<br>Gets or sets the callback invoked when the realized tiles change, for example to load more data near the end. |
| `public bool Disabled { get; set; }` | `[Parameter]`<br>Gets or sets whether the grid rejects interaction. |
| `public int Columns { get; }` | Gets the number of columns, computed from the width. |
| `public int CurrentIndex { get; }` | Gets the index of the current tile, or -1. |
| `protected override string ElementName { get; }` | Gets the element name. |
| `protected override string? ElementStyle { get; }` | Gets the tile size variables and the caller's declarations. |

### `GroupBox`

Namespace `Nazor.Controls`. [Source](../../Sources/Core/Nazor.Controls/Layout/GroupBox.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed class GroupBox : ContentControl` | A titled frame around related controls, such as the options of a dialog: a group named by its header, the replacement of HTML `fieldset` and `legend`. `Nazor.Controls.GroupBox.Disabled` disables everything inside. With `Nazor.Controls.GroupBox.IsCheckable` the header is a check box (`Nazor.Controls.GroupBox.IsChecked`, `@bind-IsChecked`) that enables the content only while checked; with `Nazor.Controls.GroupBox.IsCollapsible` a header button shows and hides the content (`Nazor.Controls.GroupBox.IsExpanded`, `@bind-IsExpanded`). The parts are `GroupBoxHeader`, holding the `GroupBoxTitle` or the `CheckBox` and, when collapsible, the `GroupBoxToggle` button with its `GroupBoxChevron`, and the `Nazor.Controls.Primitives.Collapsible``GroupBoxPanel` with its `GroupBoxPanelContent`. The element has the `checkable`, `checked`, `collapsible`, `expanded` and `disabled` attributes. |
| `protected override void OnParametersSet()` | Synchronizes the checked and expanded states. |
| `protected override void AddAttributes(RenderTreeBuilder builder)` | Adds the group role, the name and the states.<br>`builder`. The builder that receives the render frames. |
| `protected override void AddContent(RenderTreeBuilder builder)` | Adds the header and the panel.<br>`builder`. The builder that receives the render frames. |
| `public Task SetExpandedAsync(bool value)` | Shows or hides the content of a collapsible group as the user does and reports the change.<br>`value`. Whether the content should be shown.<br>**Returns**. A task that completes when the change has been reported. |
| `public GroupBox()` |  |
| `public string? Header { get; set; }` | `[Parameter]`<br>Gets or sets the title as plain text. |
| `public RenderFragment? HeaderContent { get; set; }` | `[Parameter]`<br>Gets or sets the title as content; it replaces `Nazor.Controls.GroupBox.Header`. |
| `public bool IsCheckable { get; set; }` | `[Parameter]`<br>Gets or sets whether the header is a check box that enables and disables the content. |
| `public bool IsChecked { get; set; }` | `[Parameter]` Initial value `true`.<br>Gets or sets whether the header check box is checked; true by default. |
| `public EventCallback<bool> IsCheckedChanged { get; set; }` | `[Parameter]`<br>Gets or sets the callback invoked when the user checks or clears the header check box. |
| `public bool IsCollapsible { get; set; }` | `[Parameter]`<br>Gets or sets whether a header button shows and hides the content. |
| `public bool IsExpanded { get; set; }` | `[Parameter]` Initial value `true`.<br>Gets or sets whether the content of a collapsible group is shown; true by default. |
| `public EventCallback<bool> IsExpandedChanged { get; set; }` | `[Parameter]`<br>Gets or sets the callback invoked when the user shows or hides the content. |
| `public bool Disabled { get; set; }` | `[Parameter]`<br>Gets or sets whether the group and everything inside reject interaction. |
| `public bool Checked { get; }` | Gets whether the header check box is currently checked. |
| `public bool Expanded { get; }` | Gets whether the content is currently shown. |
| `public bool IsContentDisabled { get; }` | Gets whether the content currently rejects interaction. |
| `protected override string ElementName { get; }` | Gets the NSS element name emitted by GroupBox. |

### `GroupPosition`

Namespace `Nazor.Controls`. [Source](../../Sources/Core/Nazor.Controls/Vocabulary.cs)

| Declaration | Contract |
| --- | --- |
| `public enum GroupPosition` | The position of a control within a joined group, written as the `position` attribute. |
| `Only = 0` | The only member of the group. |
| `First = 1` | The first member. |
| `Middle = 2` | A member between the first and the last. |
| `Last = 3` | The last member. |

### `Heading`

Namespace `Nazor.Controls`. [Source](../../Sources/Core/Nazor.Controls/Text/TextBlock.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed class Heading : ContentControl` | A heading of the document outline, from level 1 (the page title) to level 6. |
| `protected override void OnParametersSet()` | Validates the heading level. |
| `protected override void AddAttributes(RenderTreeBuilder builder)` | Adds the heading role and level.<br>`builder`. The builder that receives the render frames. |
| `public Heading()` |  |
| `public int Level { get; set; }` | `[Parameter]` Initial value `1`.<br>Gets or sets the heading level, from one through six. |
| `protected override string ElementName { get; }` | Gets the NSS element name emitted by Heading. |

### `IMenuOwner`

Namespace `Nazor.Controls`. [Source](../../Sources/Core/Nazor.Controls/Menus/Menu.cs)

| Declaration | Contract |
| --- | --- |
| `public interface IMenuOwner` | A surface that menu items close when they run; implemented by menus and the controls that own one. |
| `Task CloseAsync()` | Closes the menu and every menu that contains it.<br>**Returns**. A task that completes when the menus are closed. |

### `Icon`

Namespace `Nazor.Controls`. [Source](../../Sources/Core/Nazor.Controls/Media/Image.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed class Icon : Control` | A monochrome icon in the current `color`, 1em square by default. Precedence is `Source`, `Symbol`, then `Glyph`. Decorative unless `AccessibleName` is set; name the containing action when possible. |
| `protected override void AddAttributes(RenderTreeBuilder builder)` | Adds the tinted image source and the image role when the icon is named.<br>`builder`. The builder that receives the render frames. |
| `protected override void AddContent(RenderTreeBuilder builder)` | Adds the active symbol's glyph or raw glyph text when no image source is set.<br>`builder`. The builder that receives the render frames. |
| `public Icon()` |  |
| `public string? Glyph { get; set; }` | `[Parameter]` Raw glyph text, used when both `Source` and `Symbol` are null. The font is selected through NSS. |
| `public IFontSymbol? Symbol { get; set; }` | `[Parameter]` Typed glyph/family and optional ordered alternatives, used when `Source` is null. Exact provider family and full Unicode coverage select a paired alternative; no covered pair draws no symbol ink. Typed symbols override NSS font-symbol remapping; color and size remain styleable. Single-font symbols preserve ordinary family behavior. Register fonts before startup. |
| `public ImageSource? Source { get; set; }` | `[Parameter]` Image mask drawn in the current color, taking precedence over `Symbol` and `Glyph`. |
| `protected override string ElementName { get; }` | Gets the NSS element name emitted by Icon. |
| `protected override string? ElementStyle { get; }` | Caller styles followed by the active symbol's literal font family. Otherwise returns caller styles unchanged. |
| `protected override void OnParametersSet()` | Validates the active symbol. Throws `ArgumentException` for blank glyphs/families or families containing control characters. An inactive symbol is ignored when `Source` is set. |

### `Image`

Namespace `Nazor.Controls`. [Source](../../Sources/Core/Nazor.Controls/Media/Image.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed class Image : Control` | Shows an image from a file, an embedded resource, a stream, bytes or a URI (a string converts: `Source="images/logo.png"`). The image loads and decodes off the UI thread; meanwhile the element is empty, then it takes the natural size of the image unless NSS sizes it, and `object-fit` and `object-position` place it in the box. `Nazor.Controls.Image.Alt` is the text for assistive technologies: an image without it is decorative and hidden from them. |
| `protected override void AddAttributes(RenderTreeBuilder builder)` | Adds the image source, its alternative text and the load events.<br>`builder`. The builder that receives the render frames. |
| `public Image()` |  |
| `public ImageSource? Source { get; set; }` | `[Parameter]`<br>Gets or sets the file, URI, resource, or byte source to display. |
| `public string? Alt { get; set; }` | `[Parameter]`<br>Gets or sets the text alternative; empty or null for a decorative image. |
| `public EventCallback<ImageLoadEventArgs> OnLoad { get; set; }` | `[Parameter]`<br>Gets or sets the callback invoked when the image was loaded and decoded, with its natural size. |
| `public EventCallback<ImageErrorEventArgs> OnError { get; set; }` | `[Parameter]`<br>Gets or sets the callback invoked when the image could not be read or decoded; the element stays empty. |
| `protected override string ElementName { get; }` | Gets the NSS element name emitted by Image. |

### `InlineMessage`

Namespace `Nazor.Controls`. [Source](../../Sources/Core/Nazor.Controls/Feedback/InlineMessage.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed class InlineMessage : ContentControl` | A message about the surrounding content, such as a form-wide error or a note above a list, in the flow of the page. The parts are `InlineMessageMark` (a mark for the `Nazor.Controls.InlineMessage.Severity`, named for assistive technologies), `InlineMessageBody` with `InlineMessageTitle` and `InlineMessageText` (the content between the tags), `InlineMessageActions` and, with `Nazor.Controls.InlineMessage.IsClosable`, the `InlineMessageClose` button. `Nazor.Controls.InlineMessage.IsOpen` supports `@bind-IsOpen`: closing hides the message. The message is a live region, so showing it or changing its text is announced: errors at once (`assertive`), the others politely. |
| `protected override void OnParametersSet()` | Validates the parameters and synchronizes the open state. |
| `protected override void BuildRenderTree(RenderTreeBuilder builder)` | Renders the message while open and through its theme-controlled exit.<br>`builder`. The builder that receives the render frames. |
| `protected override void AddAttributes(RenderTreeBuilder builder)` | Adds the status role, the severity, the layout and the live politeness.<br>`builder`. The builder that receives the render frames. |
| `protected override void AddContent(RenderTreeBuilder builder)` | Adds the mark, the body, the actions and the close button.<br>`builder`. The builder that receives the render frames. |
| `public Task CloseAsync()` | Hides the message as the close button does and reports the change.<br>**Returns**. A task that completes when the change has been reported. |
| `public InlineMessage()` |  |
| `public Severity Severity { get; set; }` | `[Parameter]`<br>Gets or sets the importance of the message. |
| `public string? Title { get; set; }` | `[Parameter]`<br>Gets or sets the bold title shown before the message. |
| `public RenderFragment? Actions { get; set; }` | `[Parameter]`<br>Gets or sets actions related to the message, usually buttons. |
| `public bool IsClosable { get; set; }` | `[Parameter]`<br>Gets or sets whether a close button is shown. |
| `public bool IsOpen { get; set; }` | `[Parameter]` Initial value `true`.<br>Gets or sets whether the message is shown; true by default. |
| `public EventCallback<bool> IsOpenChanged { get; set; }` | `[Parameter]`<br>Gets or sets the callback invoked when the close button hides the message. |
| `public EventCallback OnClose { get; set; }` | `[Parameter]`<br>Gets or sets the callback invoked after the close button hides the message. |
| `public InlineMessageLayout Layout { get; set; }` | `[Parameter]`<br>Gets or sets how the parts are arranged. |
| `public bool Opened { get; }` | Gets whether the message is currently shown. |
| `protected override string ElementName { get; }` | Gets the NSS element name emitted by InlineMessage. |

`InlineMessagePresence` is a Collapsible viewport with naturally measured `InlineMessagePresenceContent`. Set `overlay-exit-duration` and height/spacing transitions on the viewport; keep visual padding on the inner `InlineMessage`. The same message element is retained during close/reopen, with its original Class, Style, Id and Element reference. Closing disables its actions and hides it from accessibility immediately; reduced motion removes closed content immediately. When retention completes the viewport has `closed=true` and is excluded from layout.

NazorLeaf collapses the entire measured box and sibling spacing over 350ms and fades the content over 200ms. Its inner padding stays naturally measured throughout closing, so every animation frame does not restart retention. Basic and Fluent keep immediate closing unless an application configures exit motion.

Pre-release migration: selectors that depend on direct message siblings/parents must account for the two measurement parts. Move an existing `InlineMessage { overlay-exit-duration: ... }` rule to `InlineMessagePresence`. InlineMessage no longer implements IDisposable; the child Collapsible owns its motion subscription and disposal. Bindings and CloseAsync callbacks keep their existing timing.

| Declaration | Contract |
| --- | --- |
| `protected override string? ElementStyle { get; }` | Disables pointer interaction during retained exit. |

### `InlineMessageLayout`

Namespace `Nazor.Controls`. [Source](../../Sources/Core/Nazor.Controls/Feedback/InlineMessage.cs)

| Declaration | Contract |
| --- | --- |
| `public enum InlineMessageLayout` | How an `Nazor.Controls.InlineMessage` arranges its parts, written as the `layout` attribute. |
| `Compact = 0` | One line: the mark, the title, the message and the actions side by side (`compact`). |
| `Expanded = 1` | The title and the message stacked, with the actions below them (`expanded`). |

### `Intent`

Namespace `Nazor.Controls`. [Source](../../Sources/Core/Nazor.Controls/Vocabulary.cs)

| Declaration | Contract |
| --- | --- |
| `public enum Intent` | The meaning of a control's color, combined with `Nazor.Controls.ControlAppearance`. It is written as the `intent` attribute, for example `Button[intent="danger"]`. |
| `Neutral = 0` | No particular meaning. |
| `Accent = 1` | The accent color of the application. |
| `Success = 2` | A successful or positive outcome. |
| `Warning = 3` | Something that needs attention. |
| `Danger = 4` | A destructive or failed action. |

### `Kbd`

Namespace `Nazor.Controls`. [Source](../../Sources/Core/Nazor.Controls/Text/Paragraph.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed class Kbd : ContentControl` | A single key of the keyboard drawn as a key cap, such as `Ctrl` or `Enter` in an instruction. Write one `Nazor.Controls.Kbd` per key; `Nazor.Controls.ShortcutLabel` formats a whole shortcut in the notation of the platform. The element is `Kbd` with the `text` role. |
| `protected override void AddAttributes(RenderTreeBuilder builder)` | Adds the text role.<br>`builder`. The builder that receives the render frames. |
| `public Kbd()` |  |
| `protected override string ElementName { get; }` | Gets the NSS element name emitted by Kbd. |

### `Label`

Namespace `Nazor.Controls`. [Source](../../Sources/Core/Nazor.Controls/Fields/Field.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed class Label : ContentControl` | The name of a control. With `Nazor.Controls.Label.For` it names the control with that identifier; inside a `Nazor.Controls.Field` it names the field's control. A click on the label focuses the control and toggles a check box, switch or radio button, as in HTML; the label adds no Tab stop. `Nazor.Controls.Label.Required` shows a `LabelRequiredMark` part. |
| `protected override void AddAttributes(RenderTreeBuilder builder)` | Adds the label marker, its target and the activation forwarding.<br>`builder`. The builder that receives the render frames. |
| `protected override void AddContent(RenderTreeBuilder builder)` | Adds the text and the required mark.<br>`builder`. The builder that receives the render frames. |
| `public Label()` |  |
| `public string? For { get; set; }` | `[Parameter]`<br>Gets or sets the identifier of the labeled control; null uses the enclosing field's control. |
| `public bool Required { get; set; }` | `[Parameter]`<br>Gets or sets whether the label marks a required value. |
| `protected override string ElementName { get; }` | Gets the NSS element name emitted by Label. |

### `Launcher`

Namespace `Nazor.Controls`. [Source](../../Sources/Core/Nazor.Controls/Actions/Link.cs)

| Declaration | Contract |
| --- | --- |
| `public static class Launcher` | Opens addresses for `Nazor.Controls.Link`: the default program of the system, or what a test or a host sets. Only absolute `http`, `https` and `mailto` addresses are opened. |
| `public static bool CanOpen(Uri? uri)` | Gets whether an address is one the launcher opens: absolute, with the http, https or mailto scheme.<br>`uri`. The address to check.<br>**Returns**. true if `Nazor.Controls.Launcher.TryOpen(System.Uri)` would open the address; otherwise, false. |
| `public static bool TryOpen(Uri? uri)` | Opens an address with `Nazor.Controls.Launcher.Open` when `Nazor.Controls.Launcher.CanOpen(System.Uri)` accepts it.<br>`uri`. The address to open.<br>**Returns**. true if the address was handed to `Nazor.Controls.Launcher.Open`; otherwise, false. |
| `public static Action<Uri> Open { get; set; }` | Gets or sets what opens an address; by default the shell (the browser for http and https, the mail program for mailto). Set it from the UI thread, for example in tests or in a sandboxed host. |

### `Link`

Namespace `Nazor.Controls`. [Source](../../Sources/Core/Nazor.Controls/Actions/Link.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed class Link : ContentControl` | A hyperlink: activation by a click, Enter or Space runs `Nazor.Controls.Link.OnClick` and then opens `Nazor.Controls.Link.NavigateUri` through the `Nazor.Controls.Launcher` (absolute http, https and mailto addresses only). After the first activation the element has the `visited` attribute until the link is removed. With `Nazor.Controls.Link.IsExternal` a `LinkExternalMark` part follows the text and assistive technologies hear that the link opens outside the application. The element has the `link` role; the address is its tooltip unless `Nazor.Controls.Control.Tooltip` is set. |
| `protected override void AddAttributes(RenderTreeBuilder builder)` | Adds the link role, the states and the activation.<br>`builder`. The builder that receives the render frames. |
| `protected override void AddContent(RenderTreeBuilder builder)` | Adds the text and the external mark.<br>`builder`. The builder that receives the render frames. |
| `public Link()` |  |
| `public Uri? NavigateUri { get; set; }` | `[Parameter]`<br>Gets or sets the address opened when the link is activated. |
| `public EventCallback<PointerEventArgs> OnClick { get; set; }` | `[Parameter]`<br>Gets or sets the callback invoked when the link is activated, before the address is opened. |
| `public bool IsExternal { get; set; }` | `[Parameter]`<br>Gets or sets whether the link opens outside the application, which adds an external mark. |
| `public bool Disabled { get; set; }` | `[Parameter]`<br>Gets or sets whether the link rejects interaction. |
| `public bool Visited { get; }` | Gets whether the link has been activated since it was rendered. |
| `protected override string ElementName { get; }` | Gets the NSS element name emitted by Link. |

### `List`

Namespace `Nazor.Controls`. [Source](../../Sources/Core/Nazor.Controls/Text/List.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed class List : ContentControl` | A document list of `Nazor.Controls.ListItem` children, bulleted or numbered (`Nazor.Controls.List.Ordered`). Numbering starts at `Nazor.Controls.List.Start`, can count down (`Nazor.Controls.List.Reversed`) and continues from any item that sets `Nazor.Controls.ListItem.Value`. Lists nest inside items; the element has the `list` role, the `ordered`, `reversed` and `marker-style` attributes and the nesting `level` (1 for an outer list). A document list has no selection or focus of its own: use a list view for that. |
| `protected override void OnParametersSet()` | Validates the marker style and renumbers the items. |
| `protected override void AddAttributes(RenderTreeBuilder builder)` | Adds the list role and the numbering attributes.<br>`builder`. The builder that receives the render frames. |
| `protected override void AddContent(RenderTreeBuilder builder)` | Supplies the list to its items and nested lists.<br>`builder`. The builder that receives the render frames. |
| `public static string Marker(ListMarkerStyle style, int number)` | Formats the marker of an item number in a marker style.<br>`style`. The marker style; `Nazor.Controls.ListMarkerStyle.Default` formats a decimal number.<br>`number`. The item number.<br>**Returns**. The marker text, empty for `Nazor.Controls.ListMarkerStyle.None`. |
| `public List()` |  |
| `public bool Ordered { get; set; }` | `[Parameter]`<br>Gets or sets whether the items are numbered; it chooses the default marker. |
| `public int? Start { get; set; }` | `[Parameter]`<br>Gets or sets the number of the first item; null starts at one, or at the item count when reversed. |
| `public bool Reversed { get; set; }` | `[Parameter]`<br>Gets or sets whether the numbers decrease from item to item. |
| `public ListMarkerStyle MarkerStyle { get; set; }` | `[Parameter]`<br>Gets or sets the marker drawn before each item. |
| `public List? Parent { get; set; }` | `[CascadingParameter]`<br>Gets or sets the list that contains this one, supplied automatically. |
| `public int Level { get; }` | Gets the nesting level: 1 for an outer list, 2 for a list inside one of its items, and so on. |
| `public ListMarkerStyle EffectiveMarkerStyle { get; }` | Gets the marker style in effect once `Nazor.Controls.ListMarkerStyle.Default` is resolved. |
| `protected override string ElementName { get; }` | Gets the NSS element name emitted by List. |

### `ListItem`

Namespace `Nazor.Controls`. [Source](../../Sources/Core/Nazor.Controls/Text/List.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed class ListItem : ContentControl, IDisposable` | An item of a `Nazor.Controls.List`: a decorative `ListItemMarker` (the bullet or the number) and a `ListItemContent` part with any content, including a nested list. The element has the `listitem` role, its position in the list and the size of the list. |
| `protected override void OnParametersSet()` | Joins the enclosing list. |
| `protected override void OnAfterRender(bool firstRender)` | Renumbers the list once the item is laid out in its final order.<br>`firstRender`. Whether this is the first render. |
| `protected override void AddAttributes(RenderTreeBuilder builder)` | Adds the list item role and its position.<br>`builder`. The builder that receives the render frames. |
| `protected override void AddContent(RenderTreeBuilder builder)` | Adds the marker and the content.<br>`builder`. The builder that receives the render frames. |
| `public void Dispose()` | Leaves the list. |
| `public ListItem()` |  |
| `public int? Value { get; set; }` | `[Parameter]`<br>Gets or sets the number of this item in an ordered list; the following items continue from it. |
| `public List? Owner { get; set; }` | `[CascadingParameter]`<br>Gets or sets the enclosing list, supplied automatically. |
| `public int Number { get; }` | Gets the number of the item as last rendered. |
| `protected override string ElementName { get; }` | Gets the NSS element name emitted by ListItem. |

### `ListMarkerStyle`

Namespace `Nazor.Controls`. [Source](../../Sources/Core/Nazor.Controls/Text/List.cs)

| Declaration | Contract |
| --- | --- |
| `public enum ListMarkerStyle` | The marker drawn before the items of a `Nazor.Controls.List`, written as the `marker-style` attribute. |
| `Default = 0` | Decimal numbers for an ordered list; for an unordered list a disc, a circle in a nested list and a square deeper. |
| `Disc = 1` | A filled disc. |
| `Circle = 2` | An open circle. |
| `Square = 3` | A filled square. |
| `Decimal = 4` | Decimal numbers: 1, 2, 3. |
| `LowerAlpha = 5` | Lower-case letters: a, b, c, then aa after z. |
| `UpperAlpha = 6` | Upper-case letters: A, B, C, then AA after Z. |
| `LowerRoman = 7` | Lower-case Roman numerals: i, ii, iii. |
| `UpperRoman = 8` | Upper-case Roman numerals: I, II, III. |
| `None = 9` | No visible marker; the items remain a list for assistive technologies. |

### `ItemReorderRequest<TItem>`

Namespace `Nazor.Controls`. [Source](../../Sources/Core/Nazor.Controls/Collections/ItemReorderController.cs)

Authored as a C# `readonly record struct`; the table includes its compiled value-type contract and synthesized equality/deconstruction members.

| Declaration | Contract |
| --- | --- |
| `public readonly struct ItemReorderRequest<TItem> : IEquatable<ItemReorderRequest<TItem>>` | One application-owned move; OldIndex is before removal, NewIndex is the final index after removal/insertion. Tree indexes are relative to current siblings. |
| `public ItemReorderRequest()` | Creates a default request with the default item and both indexes zero. |
| `public ItemReorderRequest(TItem Item, int OldIndex, int NewIndex)` | Creates a move request for the item and its original/final indexes. |
| `public TItem Item { get; init; }` | The item to move. |
| `public int OldIndex { get; init; }` | Original index before removal. |
| `public int NewIndex { get; init; }` | Final index after removal and insertion. |
| `public override string ToString()` | Returns the record diagnostic representation. |
| `public override int GetHashCode()` | Hashes the item and both indexes with their default equality semantics. |
| `public override bool Equals(object obj)` | Tests equality with the same record type; null or another type is unequal. |
| `public bool Equals(ItemReorderRequest<TItem> other)` | Compares the item and both indexes using their default equality semantics. |
| `public static bool operator ==(ItemReorderRequest<TItem> left, ItemReorderRequest<TItem> right)` | Tests value equality. |
| `public static bool operator !=(ItemReorderRequest<TItem> left, ItemReorderRequest<TItem> right)` | Tests value inequality. |
| `public void Deconstruct(out TItem Item, out int OldIndex, out int NewIndex)` | Extracts the item and both indexes. |

### `ListView<TItem>`

Namespace `Nazor.Controls`. [Source](../../Sources/Core/Nazor.Controls/Collections/ListView.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed class ListView<TItem> : Control` | A virtualized list of `Nazor.Controls.ListView<TItem>.Items` with selection, built on `Nazor.Controls.Primitives.Virtualizer<TItem>` and `Nazor.Controls.Primitives.SelectionModel<TKey>`: only the visible rows exist, so a list of a hundred thousand items costs as much as a screenful. Each row shows `Nazor.Controls.ListView<TItem>.ItemTemplate` or `Nazor.Controls.ListView<TItem>.ItemText`. The list is a single Tab stop: the arrows, Home, End, Page Up and Page Down move the current row (marked `current`) and the selection with it, Shift extends a multiple selection, Ctrl moves without selecting, Space toggles, Ctrl+A selects all, typing jumps to a matching row and Enter or a double click raise `Nazor.Controls.ListView<TItem>.OnItemActivated`. `Nazor.Controls.ListView<TItem>.SelectedItem` (`@bind-SelectedItem`) holds a single selection and `Nazor.Controls.ListView<TItem>.SelectedItems` (`@bind-SelectedItems`) a multiple one. `Nazor.Controls.ListView<TItem>.GroupBy` adds section headers; `Nazor.Controls.ListView<TItem>.OnEndReached` and `Nazor.Controls.ListView<TItem>.IsLoadingMore` support incremental loading. The element `ListView` (role `listbox`) holds the scrolling `ListViewViewport` (which needs a height, given by the theme or a class), the rows `ListViewItem` (role `option`, with `ListViewItemCheck` and `ListViewItemContent`), `ListViewSectionHeader`, `ListViewEmpty` and `ListViewLoadingMore`.<br>`TItem`. The type of the items. |
| `public ListView()` | Creates a list view. |
| `protected override void OnParametersSet()` | Validates the parameters and synchronizes the rows and the selection. |
| `public void Refresh()` | Reads the items again, after the list passed to `Nazor.Controls.ListView<TItem>.Items` was changed in place. |
| `public bool ScrollIntoView(TItem item)` | Scrolls so that an item is visible and makes it the current row.<br>`item`. The item.<br>**Returns**. true if the item is in the list. |
| `protected override void AddAttributes(RenderTreeBuilder builder)` | Adds the list role, the selection mode and the keyboard handling.<br>`builder`. The builder that receives the render frames. |
| `protected override void AddContent(RenderTreeBuilder builder)` | Adds the viewport with the realized rows, or the empty content, and the loading part.<br>`builder`. The builder that receives the render frames. |
| `protected override Task OnAfterRenderAsync(bool firstRender)` | Moves the focus to the current row once it is realized and requests more items near the end.<br>`firstRender`. Whether this is the first render.<br>**Returns**. A task that completes when the work is done. |
| `public IReadOnlyList<TItem> Items { get; set; }` | `[Parameter]` Initial value `[]`.<br>Gets or sets the items, in display order unless `Nazor.Controls.ListView<TItem>.GroupBy` groups them. |
| `public RenderFragment<TItem>? ItemTemplate { get; set; }` | `[Parameter]`<br>Gets or sets how each item is rendered; `Nazor.Controls.ListView<TItem>.ItemText` is shown when null. |
| `public Func<TItem, string>? ItemText { get; set; }` | `[Parameter]`<br>Gets or sets the text of an item, shown without a template and matched by typing; `ToString` is used when null. With a template it also becomes the accessible name of the row. |
| `public Func<TItem, object>? ItemKey { get; set; }` | `[Parameter]`<br>Gets or sets a function that gives each item a stable key for selection and rendering; the item itself is used when null. |
| `public Func<TItem, bool>? ItemDisabled { get; set; }` | `[Parameter]`<br>Gets or sets a function that tells whether an item is disabled: it is skipped by the keyboard and cannot be selected. |
| `public double? ItemHeight { get; set; }` | `[Parameter]` Null uses the measured theme row metric; explicit values fix the row height, or provide an estimate with `MeasureItems`. Basic falls back to 32 logical units. |
| `public bool MeasureItems { get; set; }` | `[Parameter]`<br>Gets or sets whether rows are measured after layout, for rows of different heights. |
| `public SelectionMode SelectionMode { get; set; }` | `[Parameter]` Initial value `SelectionMode.Single`.<br>Gets or sets how many items can be selected. |
| `public TItem? SelectedItem { get; set; }` | `[Parameter]`<br>Gets or sets the selected item in single selection. |
| `public EventCallback<TItem?> SelectedItemChanged { get; set; }` | `[Parameter]`<br>Gets or sets the callback invoked when the user selects another item in single selection. |
| `public IReadOnlyCollection<TItem>? SelectedItems { get; set; }` | `[Parameter]`<br>Gets or sets the selected items in multiple selection. |
| `public EventCallback<IReadOnlyCollection<TItem>> SelectedItemsChanged { get; set; }` | `[Parameter]`<br>Gets or sets the callback invoked with the selected items, in display order, when the user changes a multiple selection. |
| `public bool ShowCheckBoxes { get; set; }` | `[Parameter]`<br>Gets or sets whether rows show a check mark in multiple selection; a click or Space then toggles the row and the arrows move without changing the selection, as in a checklist. |
| `public EventCallback<TItem> OnItemActivated { get; set; }` | `[Parameter]`<br>Gets or sets the callback invoked when a row is activated with Enter or a double click. |
| `public bool CanReorderItems { get; set; }` | `[Parameter]` Opt-in primary drag or Ctrl/Meta+Shift+arrows reordering within the current group; requires `OnItemReorder`. |
| `public EventCallback<ItemReorderRequest<TItem>> OnItemReorder { get; set; }` | `[Parameter]` Requests one move; the application supplies updated items. Selection remains keyed to the same item. |
| `public Func<TItem, string>? GroupBy { get; set; }` | `[Parameter]`<br>Gets or sets a function that gives the group of an item; the items are then shown by group, in the order in which each group first appears, each group under a `ListViewSectionHeader`. |
| `public RenderFragment<string>? GroupHeaderTemplate { get; set; }` | `[Parameter]`<br>Gets or sets how a section header is rendered; the group name is shown when null. |
| `public RenderFragment? EmptyContent { get; set; }` | `[Parameter]`<br>Gets or sets the content shown when there are no items; a localized "No results" when null. |
| `public EventCallback<VirtualRange> OnRangeChanged { get; set; }` | `[Parameter]`<br>Gets or sets the callback invoked when the realized rows change. |
| `public EventCallback OnEndReached { get; set; }` | `[Parameter]`<br>Gets or sets the callback invoked when the realized rows come within `Nazor.Controls.ListView<TItem>.EndThreshold` of the end, once for each number of items, to load the next page. |
| `public int EndThreshold { get; set; }` | `[Parameter]` Initial value `10`.<br>Gets or sets how many rows before the end `Nazor.Controls.ListView<TItem>.OnEndReached` is invoked. |
| `public bool IsLoadingMore { get; set; }` | `[Parameter]`<br>Gets or sets whether more items are being loaded; the `ListViewLoadingMore` part is then shown. |
| `public RenderFragment? LoadingContent { get; set; }` | `[Parameter]`<br>Gets or sets the content of the `ListViewLoadingMore` part; a localized "Loading" when null. |
| `public bool Disabled { get; set; }` | `[Parameter]`<br>Gets or sets whether the list rejects interaction. |
| `public int CurrentIndex { get; }` | Gets the index of the current row among the displayed rows, section headers included, or -1. |
| `public VirtualRange Range { get; }` | Gets the realized rows. |
| `protected override string ElementName { get; }` | Gets the element name. |
| `protected override string? ElementStyle { get; }` | Gets the row height variable and the caller's declarations. |

### `MaskedField`

Namespace `Nazor.Controls`. [Source](../../Sources/Core/Nazor.Controls/Fields/MaskedField.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed class MaskedField : Control` | A text field that accepts only text fitting `Nazor.Controls.MaskedField.Mask` (see `Nazor.Controls.Primitives.TextMask` for the mask characters). The editor is an `Nazor.Controls.Primitives.EditableText` holding the formatted text typed so far: after every edit the text is fitted into the mask again, so characters that do not fit are dropped, pasted text is filtered and literals are written as the user types (the caret moves past them), while deleting a character also deletes the literals before it. The rest of the mask follows the text in a `MaskedFieldPrompt` part, with `Nazor.Controls.MaskedField.PromptCharacter` for each empty position, while the field has the focus or some text. The engine owns the caret, so an edit in the middle that changes the formatted text moves the caret to the end. `Nazor.Controls.MaskedField.Value` (`@bind-Value`) is raw or formatted according to `Nazor.Controls.MaskedField.ValueFormat`; `Nazor.Controls.MaskedField.IsComplete` (the `complete` attribute) tells whether every required position is filled. The parts are `MaskedFieldLeading`, `MaskedFieldInput` and `MaskedFieldPrompt`. |
| `protected override void OnParametersSet()` | Parses the mask and synchronizes the text with the value. |
| `protected override void AddAttributes(RenderTreeBuilder builder)` | Adds the frame states.<br>`builder`. The builder that receives the render frames. |
| `protected override void AddContent(RenderTreeBuilder builder)` | Adds the editor and the prompt.<br>`builder`. The builder that receives the render frames. |
| `public Task SetTextAsync(string input)` | Replaces the text as typing or pasting would, fitting it into the mask, and reports the value.<br>`input`. The text to fit.<br>**Returns**. A task that completes when the change has been reported. |
| `public Task FocusAsync(bool preventScroll = false)` | Moves the keyboard focus to the editor.<br>`preventScroll`. Whether focusing keeps the current scroll position.<br>**Returns**. A task that completes when the focus has moved. |
| `public MaskedField()` |  |
| `protected override int? ElementTabIndex { get; }` |  |
| `public string Mask { get; set; }` | `[Parameter]` `[EditorRequired]` Initial value `""`.<br>Gets or sets the mask pattern, such as `(000) 000-0000`. |
| `public string? Value { get; set; }` | `[Parameter]`<br>Gets or sets the value, raw or formatted according to `Nazor.Controls.MaskedField.ValueFormat`. |
| `public EventCallback<string> ValueChanged { get; set; }` | `[Parameter]`<br>Gets or sets the callback invoked on every edit that changes the value; supports `@bind-Value`. |
| `public MaskedValueFormat ValueFormat { get; set; }` | `[Parameter]`<br>Gets or sets what `Nazor.Controls.MaskedField.Value` holds. |
| `public char PromptCharacter { get; set; }` | `[Parameter]` Initial value `'_'`.<br>Gets or sets the character shown for each empty position; an underscore by default. |
| `public bool ShowPrompt { get; set; }` | `[Parameter]` Initial value `true`.<br>Gets or sets whether the rest of the mask is shown after the text; true by default. |
| `public string? Placeholder { get; set; }` | `[Parameter]`<br>Gets or sets the hint shown while the field is empty and has no focus. |
| `public EventCallback<bool> IsCompleteChanged { get; set; }` | `[Parameter]`<br>Gets or sets the callback invoked when the mask becomes complete or incomplete. |
| `public bool ReadOnly { get; set; }` | `[Parameter]`<br>Gets or sets whether editing is blocked while selection and copying remain available. |
| `public bool Disabled { get; set; }` | `[Parameter]`<br>Gets or sets whether the field rejects interaction. |
| `public bool Required { get; set; }` | `[Parameter]`<br>Gets or sets whether a value is required; validation is application-owned. |
| `public bool Invalid { get; set; }` | `[Parameter]`<br>Gets or sets the externally determined invalid state. |
| `public FieldAppearance Appearance { get; set; }` | `[Parameter]`<br>Gets or sets how the frame is drawn. |
| `public ControlSize Size { get; set; }` | `[Parameter]` Initial value `ControlSize.Medium`.<br>Gets or sets the size step. |
| `public RenderFragment? Leading { get; set; }` | `[Parameter]`<br>Gets or sets content shown before the text, such as an `Nazor.Controls.Icon`. |
| `public bool AutoFocus { get; set; }` | `[Parameter]`<br>Gets or sets whether the editor takes the focus when it appears. |
| `public FieldContext? Field { get; set; }` | `[CascadingParameter]`<br>Gets or sets the enclosing field, supplied automatically. |
| `public string Text { get; }` | Gets the formatted text in the editor. |
| `public string RawValue { get; }` | Gets the characters typed into editable positions. |
| `public string FormattedValue { get; }` | Gets the formatted value, with the trailing literals once complete. |
| `public bool IsComplete { get; }` | Gets whether every required position is filled. |
| `public ElementReference InputElement { get; }` | Gets the editor element, which receives the focus. |
| `protected override string ElementName { get; }` | Gets the NSS element name emitted by MaskedField. |

### `MaskedValueFormat`

Namespace `Nazor.Controls`. [Source](../../Sources/Core/Nazor.Controls/Fields/MaskedField.cs)

| Declaration | Contract |
| --- | --- |
| `public enum MaskedValueFormat` | What the `Nazor.Controls.MaskedField.Value` of a `Nazor.Controls.MaskedField` holds. |
| `Raw = 0` | Only the characters typed into editable positions, such as `5551234567`. |
| `Formatted = 1` | The text with the mask's literals, such as `(555) 123-4567`. |

### `Menu`

Namespace `Nazor.Controls`. [Source](../../Sources/Core/Nazor.Controls/Menus/Menu.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed class Menu : ComponentBase, IMenuOwner` | A list of commands that opens next to an anchor: the arrows move between items, typing jumps to an item, Enter or a click runs one and closes the menu, Escape, Tab or a press outside close it, and the focus returns where it was. Arrow navigation stops at the first and last enabled commands and scrolls the focused command into view. Fill it with `Nazor.Controls.MenuItem`, `Nazor.Controls.MenuCheckItem`, `Nazor.Controls.MenuRadioGroup<TValue>`, `Nazor.Controls.Submenu`, `Nazor.Controls.MenuSection` and `Nazor.Controls.MenuSeparator`. Menu buttons, split buttons, menu bars and context menus open one; it can also be opened directly with `Nazor.Controls.Menu.IsOpen` and `Nazor.Controls.Menu.Anchor`. |
| `protected override void OnInitialized()` | Initializes the menu command hover tracker. |
| `protected override void OnAfterRender(bool firstRender)` | Refreshes hovered command geometry after item changes. `firstRender` indicates the first render. |
| `protected override void OnParametersSet()` | Synchronizes the open state. |
| `protected override void BuildRenderTree(RenderTreeBuilder builder)` | Renders the anchored menu surface while open or retained for exit.<br>`builder`. The builder that receives the render frames. |
| `public Task OpenAsync()` | Opens the menu.<br>**Returns**. A task that completes when the change has been reported. |
| `public Task CloseOwnAsync()` | Closes this menu only, leaving the menus that contain it open.<br>**Returns**. A task that completes when the change has been reported. |
| `public Task CloseAsync()` | Closes this menu, its owner and the menus that contain it.<br>**Returns**. A task that completes when the menus are closed. |
| `public Menu()` |  |
| `public bool IsOpen { get; set; }` | `[Parameter]`<br>Gets or sets whether the menu is open. |
| `public EventCallback<bool> IsOpenChanged { get; set; }` | `[Parameter]`<br>Gets or sets the callback invoked when the menu opens or closes from inside. |
| `public ElementReference? Anchor { get; set; }` | `[Parameter]`<br>Gets or sets the element the menu opens next to. |
| `public (double X, double Y)? AnchorPoint { get; set; }` | `[Parameter]`<br>Gets or sets a point in window coordinates used when there is no anchor, such as a right click. |
| `public OverlayPlacement Placement { get; set; }` | `[Parameter]`<br>Gets or sets the preferred side of the anchor. |
| `public OverlayAlignment Alignment { get; set; }` | `[Parameter]`<br>Gets or sets the alignment with the anchor. |
| `public double Offset { get; set; }` | `[Parameter]`<br>Gets or sets the distance from the anchor. |
| `public EventCallback OnDismiss { get; set; }` | `[Parameter]`<br>Gets or sets the callback invoked when the user dismisses the menu. |
| `public string? Class { get; set; }` | `[Parameter]`<br>Gets or sets the space-separated NSS classes of the menu surface. |
| `public string? AccessibleName { get; set; }` | `[Parameter]`<br>Gets or sets the accessible name of the menu. |
| `public RenderFragment? ChildContent { get; set; }` | `[Parameter]`<br>Gets or sets the items. |
| `public IMenuOwner? Parent { get; set; }` | `[CascadingParameter]`<br>Gets or sets the menu that contains this one, supplied automatically. |
| `public bool IsSubmenu { get; set; }` | `[Parameter]`<br>Gets or sets whether this menu is nested in another one. |
| `public IMenuOwner? Owner { get; set; }` | `[Parameter]`<br>Gets or sets the owner closed together with this menu, such as the button that opened it. |
| `public bool Opened { get; }` | Gets whether the menu is open. |

### `MenuBar`

Namespace `Nazor.Controls`. [Source](../../Sources/Core/Nazor.Controls/Menus/MenuBar.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed class MenuBar : ContentControl, IDisposable` | The menu bar of a window, as in desktop applications: a row of `Nazor.Controls.MenuBarItem`, each opening a `Nazor.Controls.Menu` below it. The bar is one Tab stop: Left and Right move between the items, Down, Enter and Space open a menu, Up opens it on its last item. While a menu is open, Left and Right switch to the neighboring menu and resting the pointer on another item opens that one; Escape closes the menu and returns to its item. With `Nazor.Controls.MenuBar.FocusKeys` F10 and Alt move the focus to the bar from anywhere in the window. The bar has the `menubar` role; its items carry `expanded` while their menu is open. |
| `protected override void OnInitialized()` | Initializes the bar's decorative item-hover tracker. |
| `protected override void OnAfterRender(bool firstRender)` | Refreshes hovered item geometry after changes. `firstRender` indicates the first render. |
| `protected override void OnParametersSet()` | Registers or removes the focus keys. |
| `protected override void AddAttributes(RenderTreeBuilder builder)` | Adds the menu bar role and the accessible name.<br>`builder`. The builder that receives the render frames. |
| `protected override void AddContent(RenderTreeBuilder builder)` | Supplies the bar to its items.<br>`builder`. The builder that receives the render frames. |
| `public Task FocusAsync()` | Moves the focus to the item focused last, or to the first enabled item.<br>**Returns**. A task that completes when the focus has moved. |
| `public void Dispose()` | Removes the focus keys. |
| `public MenuBar()` |  |
| `public KeyBindings Bindings { get; set; }` | `[Inject]`<br>Gets or sets the window's shortcut registry, used for the focus keys. |
| `public bool FocusKeys { get; set; }` | `[Parameter]` Initial value `true`.<br>Gets or sets whether F10 and Alt move the focus to the bar. |
| `public MenuBarItem? OpenItem { get; }` | Gets the item whose menu is open, or null. |
| `protected override string ElementName { get; }` | Gets the NSS element name emitted by MenuBar. |

### `MenuBarItem`

Namespace `Nazor.Controls`. [Source](../../Sources/Core/Nazor.Controls/Menus/MenuBar.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed class MenuBarItem : ContentControl, IMenuOwner, IDisposable` | An entry of a `Nazor.Controls.MenuBar`: its label (the child content) in the bar and, below it, the menu holding the items of `Nazor.Controls.MenuBarItem.Content`. A click toggles the menu. The item has the `menuitem` role and the `expanded` state; its menu is a `Menu` surface like the one of `Nazor.Controls.Menu`. |
| `protected override void OnParametersSet()` | Validates the slots and registers with the bar. |
| `protected override void AddAttributes(RenderTreeBuilder builder)` | Adds the role, the states and the keyboard and pointer behavior.<br>`builder`. The builder that receives the render frames. |
| `protected override void AddContent(RenderTreeBuilder builder)` | Adds the label and, while open, the menu.<br>`builder`. The builder that receives the render frames. |
| `public void Open()` | Opens the menu and closes the other menu of the bar. |
| `public Task CloseAsync()` | Closes the menu.<br>**Returns**. A completed task. |
| `public void Dispose()` | Leaves the bar. |
| `public MenuBarItem()` |  |
| `public RenderFragment? Content { get; set; }` | `[Parameter]` `[EditorRequired]`<br>Gets or sets the items of the menu. |
| `public bool Disabled { get; set; }` | `[Parameter]`<br>Gets or sets whether the entry rejects interaction. |
| `public string? MenuAccessibleName { get; set; }` | `[Parameter]`<br>Gets or sets the accessible name of the menu. When null, the text of the menu bar item label is used. |
| `public string? MenuClass { get; set; }` | `[Parameter]`<br>Gets or sets the space-separated NSS classes of the menu surface. |
| `public EventCallback<bool> IsOpenChanged { get; set; }` | `[Parameter]`<br>Gets or sets the callback invoked when the menu opens or closes. |
| `public MenuBar? Bar { get; set; }` | `[CascadingParameter]`<br>Gets or sets the enclosing menu bar, supplied automatically. |
| `public bool Opened { get; }` | Gets whether the menu is open. |
| `protected override string ElementName { get; }` | Gets the NSS element name emitted by MenuBarItem. |

### `MenuButton`

Namespace `Nazor.Controls`. [Source](../../Sources/Core/Nazor.Controls/Actions/MenuButton.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed class MenuButton : ButtonBase, IMenuOwner` | A button that opens a `Nazor.Controls.Menu` of commands, such as Sort or New ▾: a click, Enter, Space, ArrowDown, Alt+ArrowDown or F4 open the menu (`Nazor.Controls.MenuButton.Menu`) next to the button, choosing an item or dismissing the menu closes it and returns the focus to the button. It has the look of a `Nazor.Controls.Button` (appearance, intent, size, shape, leading and trailing content) followed by a `MenuButtonChevron` part, and the `expanded` attribute while the menu is open (`@bind-IsOpen`). `Nazor.Controls.ButtonBase.OnClick` runs each time the button opens or closes the menu; a `Nazor.Controls.ButtonBase.Command` supplies the label, the tooltip and the enabled state, but its action is not run. |
| `protected override void OnParametersSet()` | Validates the parameters and synchronizes the open state. |
| `protected override void AddAttributes(RenderTreeBuilder builder)` | Adds the button role, the expanded state, the activation and the opening keys.<br>`builder`. The builder that receives the render frames. |
| `protected override void AddContent(RenderTreeBuilder builder)` | Adds the button parts, the chevron and the menu.<br>`builder`. The builder that receives the render frames. |
| `public Task OpenAsync()` | Opens the menu.<br>**Returns**. A task that completes when the change has been reported. |
| `public Task CloseAsync()` | Closes the menu.<br>**Returns**. A task that completes when the change has been reported. |
| `public MenuButton()` |  |
| `public RenderFragment? Menu { get; set; }` | `[Parameter]`<br>Gets or sets the items of the menu. |
| `public bool IsOpen { get; set; }` | `[Parameter]`<br>Gets or sets whether the menu is open. |
| `public EventCallback<bool> IsOpenChanged { get; set; }` | `[Parameter]`<br>Gets or sets the callback invoked when the menu opens or closes. |
| `public OverlayPlacement Placement { get; set; }` | `[Parameter]`<br>Gets or sets the preferred side of the button on which the menu opens. |
| `public OverlayAlignment Alignment { get; set; }` | `[Parameter]`<br>Gets or sets how the menu lines up with the button. |
| `public bool ShowChevron { get; set; }` | `[Parameter]` Initial value `true`.<br>Gets or sets whether the chevron part is shown; true by default. |
| `public string? MenuClass { get; set; }` | `[Parameter]`<br>Gets or sets the space-separated NSS classes of the menu surface. |
| `public bool Opened { get; }` | Gets whether the menu is open. |
| `protected override string ElementName { get; }` | Gets the NSS element name emitted by MenuButton. |

### `MenuCheckItem`

Namespace `Nazor.Controls`. [Source](../../Sources/Core/Nazor.Controls/Menus/Menu.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed class MenuCheckItem : MenuItemBase` | A menu item with a checked state, such as View › Status Bar: activation flips `Nazor.Controls.MenuCheckItem.IsChecked` (`@bind-IsChecked`). The leading part shows a `MenuItemCheck` mark while checked. |
| `protected override void OnParametersSet()` | Synchronizes the checked state. |
| `protected override void AddItemAttributes(RenderTreeBuilder builder)` | Adds the checked state.<br>`builder`. The builder that receives the render frames. |
| `protected override void AddLeading(RenderTreeBuilder builder)` | Adds the check mark.<br>`builder`. The builder that receives the render frames. |
| `protected override Task RunAsync(PointerEventArgs e)` | Flips the checked state, reports it and runs the item.<br>`e`. The activation event.<br>**Returns**. A task that completes when the item has run. |
| `public MenuCheckItem()` |  |
| `public bool IsChecked { get; set; }` | `[Parameter]`<br>Gets or sets whether the item is checked. |
| `public EventCallback<bool> IsCheckedChanged { get; set; }` | `[Parameter]`<br>Gets or sets the callback invoked when the user changes the checked state. |
| `protected override string ElementName { get; }` | Gets the NSS element name emitted by MenuCheckItem. |
| `protected override string Role { get; }` | Gets the checkable menu item role. |

### `MenuItem`

Namespace `Nazor.Controls`. [Source](../../Sources/Core/Nazor.Controls/Menus/Menu.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed class MenuItem : MenuItemBase` | A command of a `Nazor.Controls.Menu`. |
| `public MenuItem()` |  |
| `protected override string ElementName { get; }` | Gets the NSS element name emitted by MenuItem. |
| `protected override string Role { get; }` | Gets the menu item role. |

### `MenuItemBase`

Namespace `Nazor.Controls`. [Source](../../Sources/Core/Nazor.Controls/Menus/Menu.cs)

| Declaration | Contract |
| --- | --- |
| `public abstract class MenuItemBase : ContentControl, IDisposable` | The look and behavior shared by menu items: optional leading content, the label (or the command's text), a description, the shortcut and the danger intent. The parts are `MenuItemLeading`, `MenuItemLabel`, `MenuItemDescription`, `MenuItemShortcut` and `MenuItemTrailing`; the element carries `intent`. |
| `protected virtual void OnDispose()` | Releases resources owned by a derived menu item; called by Dispose after command observation ends. |
| `protected override void OnParametersSet()` | Follows the command's changes. |
| `protected override void AddAttributes(RenderTreeBuilder builder)` | Adds the role, the states and the activation.<br>`builder`. The builder that receives the render frames. |
| `protected virtual void AddItemAttributes(RenderTreeBuilder builder)` | Adds attributes specific to the item, with sequence numbers from 20 to 899.<br>`builder`. The builder that receives the render frames. |
| `protected override void AddContent(RenderTreeBuilder builder)` | Adds the parts of the item.<br>`builder`. The builder that receives the render frames. |
| `protected virtual void AddLeading(RenderTreeBuilder builder)` | Adds the content of the leading part.<br>`builder`. The builder that receives the render frames. |
| `protected virtual void AddTrailing(RenderTreeBuilder builder)` | Adds parts after the shortcut.<br>`builder`. The builder that receives the render frames. |
| `protected virtual Task RunAsync(PointerEventArgs e)` | Runs the item: the item's own behavior, the click callback, the command, then closes the menu.<br>`e`. The activation event.<br>**Returns**. A task that completes when the item has run. |
| `public void Dispose()` | Stops following the command and releases resources owned by derived items. |
| `protected MenuItemBase()` |  |
| `public RenderFragment? Leading { get; set; }` | `[Parameter]`<br>Gets or sets content shown before the label, usually an `Nazor.Controls.Icon`. |
| `public RenderFragment? Description { get; set; }` | `[Parameter]`<br>Gets or sets a secondary line shown below the label. |
| `public KeyGesture? Shortcut { get; set; }` | `[Parameter]`<br>Gets or sets the shortcut shown at the end of the item; the command's shortcut is used when null. |
| `public Intent Intent { get; set; }` | `[Parameter]`<br>Gets or sets the meaning of the item's color, such as `Nazor.Controls.Intent.Danger` for Delete. |
| `public bool Disabled { get; set; }` | `[Parameter]`<br>Gets or sets whether the item rejects interaction. |
| `public bool CloseOnSelect { get; set; }` | `[Parameter]` Initial value `true`.<br>Gets or sets whether running the item closes the menu. |
| `public Command? Command { get; set; }` | `[Parameter]`<br>Gets or sets a command that supplies the label, the shortcut, the enabled state and the action. |
| `public EventCallback<PointerEventArgs> OnClick { get; set; }` | `[Parameter]`<br>Gets or sets the callback invoked when the item runs. |
| `public IMenuOwner? Menu { get; set; }` | `[CascadingParameter]`<br>Gets or sets the menu that contains the item, supplied automatically. |
| `protected bool IsInactive { get; }` | Gets whether the item rejects interaction. |
| `protected abstract string Role { get; }` | Gets the accessible role of the item. |

### `MenuRadioGroup<TValue>`

Namespace `Nazor.Controls`. [Source](../../Sources/Core/Nazor.Controls/Menus/Menu.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed class MenuRadioGroup<TValue> : ContentControl` | A set of exclusive `Nazor.Controls.MenuRadioItem<TValue>` choices in a menu, such as View › Sort by: `Nazor.Controls.MenuRadioGroup<TValue>.Value` (`@bind-Value`) is the checked item's value. It renders a `MenuRadioGroup` group.<br>`TValue`. The type of the values. |
| `protected override void OnParametersSet()` | Synchronizes the value. |
| `protected override void AddAttributes(RenderTreeBuilder builder)` | Adds the group role.<br>`builder`. The builder that receives the render frames. |
| `protected override void AddContent(RenderTreeBuilder builder)` | Supplies the group to its items.<br>`builder`. The builder that receives the render frames. |
| `public MenuRadioGroup()` |  |
| `public TValue? Value { get; set; }` | `[Parameter]`<br>Gets or sets the value of the checked item. |
| `public EventCallback<TValue> ValueChanged { get; set; }` | `[Parameter]`<br>Gets or sets the callback invoked when the user checks another item. |
| `protected override string ElementName { get; }` | Gets the NSS element name emitted by MenuRadioGroup. |

### `MenuRadioItem<TValue>`

Namespace `Nazor.Controls`. [Source](../../Sources/Core/Nazor.Controls/Menus/Menu.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed class MenuRadioItem<TValue> : MenuItemBase` | One choice of a `Nazor.Controls.MenuRadioGroup<TValue>`; the leading part shows a `MenuItemRadio` mark.<br>`TValue`. The type of the values. |
| `protected override void OnParametersSet()` | Requires an enclosing group. |
| `protected override void AddItemAttributes(RenderTreeBuilder builder)` | Adds the checked state.<br>`builder`. The builder that receives the render frames. |
| `protected override void AddLeading(RenderTreeBuilder builder)` | Adds the radio mark.<br>`builder`. The builder that receives the render frames. |
| `protected override Task RunAsync(PointerEventArgs e)` | Checks this item and runs it.<br>`e`. The activation event.<br>**Returns**. A task that completes when the item has run. |
| `public MenuRadioItem()` |  |
| `public TValue Value { get; set; }` | `[Parameter]` Initial value `default!`.<br>Gets or sets the value checked by this item. |
| `public MenuRadioGroup<TValue>? Group { get; set; }` | `[CascadingParameter]`<br>Gets or sets the enclosing group, supplied automatically. |
| `protected override string ElementName { get; }` | Gets the NSS element name emitted by MenuRadioItem. |
| `protected override string Role { get; }` | Gets the exclusive menu item role. |

### `MenuSection`

Namespace `Nazor.Controls`. [Source](../../Sources/Core/Nazor.Controls/Menus/Menu.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed class MenuSection : ContentControl` | A titled group of menu items; the title is a non-interactive `MenuSectionHeader` part. |
| `protected override void AddAttributes(RenderTreeBuilder builder)` | Adds the group role and the section name.<br>`builder`. The builder that receives the render frames. |
| `protected override void AddContent(RenderTreeBuilder builder)` | Adds the header and the items.<br>`builder`. The builder that receives the render frames. |
| `public MenuSection()` |  |
| `public string? Header { get; set; }` | `[Parameter]`<br>Gets or sets the title of the section. |
| `protected override string ElementName { get; }` | Gets the NSS element name emitted by MenuSection. |

### `MenuSeparator`

Namespace `Nazor.Controls`. [Source](../../Sources/Core/Nazor.Controls/Menus/Menu.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed class MenuSeparator : Control` | A line between groups of menu items. |
| `protected override void AddAttributes(RenderTreeBuilder builder)` | Adds the separator role.<br>`builder`. The builder that receives the render frames. |
| `public MenuSeparator()` |  |
| `protected override string ElementName { get; }` | Gets the NSS element name emitted by MenuSeparator. |

### `Meter`

Namespace `Nazor.Controls`. [Source](../../Sources/Core/Nazor.Controls/Feedback/Meter.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed class Meter : Control` | A read-only measurement within a known range, such as disk usage, battery charge or password strength; use a `Nazor.Controls.ProgressBar` for the completion of a task. `Nazor.Controls.Meter.Low`, `Nazor.Controls.Meter.High` and `Nazor.Controls.Meter.Optimum` split the range into regions as the HTML meter does, and the region of the value is written as the `quality` attribute (`Nazor.Controls.Meter.Quality`) so themes can color it. The parts are `MeterLabel`, `MeterValue` (with `Nazor.Controls.Meter.ShowValue`) and `MeterTrack`, which holds `MeterFill`, or with `Nazor.Controls.Meter.Segments` one `MeterSegment` per unit of capacity, each with the `filled` attribute. The element publishes `--nz-meter-fraction` (0 to 1) and the `orientation` attribute. |
| `protected override void OnParametersSet()` | Validates the range, the thresholds and the look. |
| `protected override void AddAttributes(RenderTreeBuilder builder)` | Adds the meter role, the range, the thresholds and the quality.<br>`builder`. The builder that receives the render frames. |
| `protected override void AddContent(RenderTreeBuilder builder)` | Adds the label, the value and the track with its fill or segments.<br>`builder`. The builder that receives the render frames. |
| `public Meter()` |  |
| `public double Value { get; set; }` | `[Parameter]`<br>Gets or sets the measurement, clamped to the range. |
| `public double Minimum { get; set; }` | `[Parameter]`<br>Gets or sets the lower bound of the range; 0 by default. |
| `public double Maximum { get; set; }` | `[Parameter]` Initial value `1`.<br>Gets or sets the upper bound of the range; 1 by default. |
| `public double? Low { get; set; }` | `[Parameter]`<br>Gets or sets the upper end of the low region; null uses `Nazor.Controls.Meter.Minimum`. |
| `public double? High { get; set; }` | `[Parameter]`<br>Gets or sets the lower end of the high region; null uses `Nazor.Controls.Meter.Maximum`. |
| `public double? Optimum { get; set; }` | `[Parameter]`<br>Gets or sets the preferred value; null uses the middle of the range. |
| `public int Segments { get; set; }` | `[Parameter]`<br>Gets or sets the number of discrete units drawn, such as the bars of a signal indicator; 0 is continuous. |
| `public Orientation Orientation { get; set; }` | `[Parameter]`<br>Gets or sets the direction in which the meter fills. |
| `public string? Label { get; set; }` | `[Parameter]`<br>Gets or sets the visible label, which also names the meter for assistive technologies. |
| `public bool ShowValue { get; set; }` | `[Parameter]`<br>Gets or sets whether the value part shows the measured percentage of the range. |
| `public string? ValueText { get; set; }` | `[Parameter]`<br>Gets or sets the text of the value part instead of the percentage, such as "42 GB of 256 GB". |
| `public ControlSize Size { get; set; }` | `[Parameter]` Initial value `ControlSize.Medium`.<br>Gets or sets the size step, which themes use for the thickness. |
| `public double Fraction { get; }` | Gets the measured fraction of the range, from 0 to 1. |
| `public MeterQuality Quality { get; }` | Gets the region of the current value. |
| `protected override string ElementName { get; }` | Gets the NSS element name emitted by Meter. |
| `protected override string? ElementStyle { get; }` | Gets the fraction variable and the caller's declarations. |

### `MeterQuality`

Namespace `Nazor.Controls`. [Source](../../Sources/Core/Nazor.Controls/Feedback/Meter.cs)

| Declaration | Contract |
| --- | --- |
| `public enum MeterQuality` | How good the value of a `Nazor.Controls.Meter` is, written as the `quality` attribute. |
| `Optimal = 0` | The value is in the preferred region (`optimal`). |
| `Suboptimal = 1` | The value is acceptable but outside the preferred region (`suboptimal`). |
| `Worst = 2` | The value is at the opposite end from the preferred region (`worst`). |

### `NavigationViewPaneDisplayMode`

Namespace `Nazor.Controls`. [Source](../../Sources/Core/Nazor.Controls/Navigation/NavigationView.cs)

| Declaration | Contract |
| --- | --- |
| `public enum NavigationViewPaneDisplayMode` | Application navigation pane presentation. |
| `Auto = 0` | Resolve Minimal/Compact/Left from available width and configured thresholds. |
| `Left = 1` | Expanded left pane; user collapse retains compact roots. |
| `Compact = 2` | Compact root strip; open pane covers content. |
| `Minimal = 3` | Toggle without reserved strip; open pane covers content. |
| `Top = 4` | Explicit horizontal roots and child menus above content. |

### `NavigationView<TItem>`

Namespace `Nazor.Controls`. [Source](../../Sources/Core/Nazor.Controls/Navigation/NavigationView.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed class NavigationView<TItem> : ContentControl where TItem : notnull` | Adaptive navigation composing one retained SplitView/TreeView with keyed wrappers, root buttons/menus and measured selection. Application content/routing is independent. |
| `public NavigationView()` | Creates stable hierarchy and shared selection measurement. |
| `public IReadOnlyList<TItem> Items { get; set; }` | `[Parameter]` Gets or sets top-level navigation items. |
| `public Func<TItem, IReadOnlyList<TItem>?>? Children { get; set; }` | `[Parameter]` Gets or sets known child items; null means all items are leaves. |
| `public Func<TItem, string>? ItemText { get; set; }` | `[Parameter]` Gets or sets each item's plain text, including typeahead and accessible names. |
| `public RenderFragment<TItem>? ItemIcon { get; set; }` | `[Parameter]` Gets or sets leading icon content. Without it compact roots use the first text element. |
| `public Func<TItem, object>? ItemKey { get; set; }` | `[Parameter]` Gets or sets stable, globally unique non-null item keys; item equality is used when null. |
| `public Func<TItem, bool>? ItemDisabled { get; set; }` | `[Parameter]` Gets or sets whether individual navigation items reject interaction. |
| `public TItem? SelectedItem { get; set; }` | `[Parameter]` Gets or sets the selected application item; null selects none. |
| `public EventCallback<TItem?> SelectedItemChanged { get; set; }` | `[Parameter]` Gets or sets the callback reporting a different selected application item. |
| `public EventCallback<TItem> OnItemInvoked { get; set; }` | `[Parameter]` Gets or sets the primary pointer/Enter activation callback. The application owns routing and content changes. |
| `public NavigationViewPaneDisplayMode PaneDisplayMode { get; set; }` | `[Parameter]` Gets or sets the pane presentation; Auto by default. Explicit Top does not adapt to width. |
| `public double CompactModeThresholdWidth { get; set; }` | `[Parameter]` Gets or sets the greatest automatic Minimal width; 640 by default. |
| `public double ExpandedModeThresholdWidth { get; set; }` | `[Parameter]` Gets or sets the least automatic expanded width; 1008 by default. |
| `public double OpenPaneLength { get; set; }` | `[Parameter]` Gets or sets the open pane width; 320 logical units by default. |
| `public double CompactPaneLength { get; set; }` | `[Parameter]` Gets or sets the closed compact width; 48 logical units by default. |
| `public bool IsPaneOpen { get; set; }` | `[Parameter]` Gets or sets whether the pane is open; true initially. Adaptive mode changes request the corresponding default. |
| `public EventCallback<bool> IsPaneOpenChanged { get; set; }` | `[Parameter]` Gets or sets pane changes from toggling, dismissal or adaptation. |
| `public RenderFragment? Header { get; set; }` | `[Parameter]` Gets or sets content in the persistent application header. |
| `public RenderFragment? PaneHeader { get; set; }` | `[Parameter]` Gets or sets persistent pane header content, hidden while compact/minimal is closed. |
| `public RenderFragment? PaneFooter { get; set; }` | `[Parameter]` Gets or sets persistent pane footer content, hidden while compact/minimal is closed. |
| `public bool Disabled { get; set; }` | `[Parameter]` Gets or sets whether navigation actions are unavailable. Application content remains active. |
| `public NavigationViewPaneDisplayMode EffectivePaneDisplayMode { get; }` | Resolved presentation, never Auto. |
| `public bool PaneOpen { get; }` | Current pane visibility; Top is always visible. |
| `public TItem? CurrentItem { get; }` | Current canonical item, or default when absent/disabled. |
| `public Task SetPaneOpenAsync(bool value)` | Changes/reports non-Top pane state; disabled navigation prevents opening. |
| `public Task TogglePaneAsync()` | Uses the same pane-state path. |
| `public void Refresh()` | Rebuilds in-place known hierarchy while retaining stable keys/expansion/content. |
| `protected override string ElementName { get; }` | NavigationView. |
| `protected override string? ElementStyle { get; }` | Publishes `--nz-navigation-open-width` / `--nz-navigation-compact-width`, then caller style. |
| `protected override void OnParametersSet()` | Validates dimensions, globally unique keys, acyclic bounded-depth hierarchy and state. |
| `protected override void AddAttributes(RenderTreeBuilder builder)` | Adaptive width/state and scoped Escape handling. |
| `protected override void AddContent(RenderTreeBuilder builder)` | One persistent pane/content composition across modes. |
| `protected override void OnAfterRender(bool firstRender)` | Reveals selection and the focused root, restores hidden owner focus and updates indicator geometry. |
| `protected override Task OnAfterRenderAsync(bool firstRender)` | Reports adaptive pane requests after render. |

Inherited `ChildContent` remains mounted independently of navigation across display modes. Parts: `NavigationViewChrome`, `NavigationViewToggle`, `NavigationViewHeader`, `NavigationViewPane`, `NavigationViewPaneHeader`, `NavigationViewPaneFooter`, `NavigationViewTreeHost`, `NavigationViewItemContent`, `NavigationViewItemIcon`, `NavigationViewItemLabel`, `NavigationViewRootScroller`, `NavigationViewRoots`, `NavigationViewRootItem`, `NavigationViewRootChevron`, `NavigationViewContent`, `NavigationViewTreeIndicator`, `NavigationViewRootIndicator`, plus existing SplitView/TreeView/Menu parts. Root state: `display-mode`, `pane-open`, `compact`, `tree-ready`, `roots-ready`. Root items carry selection, expansion, localized name and roving index. `ItemIcon` is decorative content. `--nz-navigation-root-viewport` is a measured internal root-strip width, not an application configuration token.

Open width defaults320, compact48, Auto thresholds640/1008, Top row48/content Header52. Tree rows retain shared virtualization/keyboard semantics and source-informed Fluent40-unit pitch. Top uses horizontal reveal instead of native overflow policy. Item invocation starts before selection notification and can repeat for an already selected item. Explicit app selection and removed-item normalization do not invoke routes. Hidden navigation is disabled/semantically hidden; main content remains active. Keys survive replacement but must remain stable and globally unique; cycles/duplicates/depth beyond128 throw. Removed wrapper expansion is pruned. [Behavior and scope](../Guide/12-Navigation.md#adaptive-application-navigation)

### `NumberField`

Namespace `Nazor.Controls`. [Source](../../Sources/Core/Nazor.Controls/Fields/NumberField.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed class NumberField : Control` | A number typed as text and stepped with buttons, keys and optionally the wheel. `Nazor.Controls.NumberField.Value` (`@bind-Value`; null when empty) changes when the text is committed with Enter or by leaving the field: the text is read in `Nazor.Controls.NumberField.Culture` (group separators and a trailing `Nazor.Controls.NumberField.Unit` are accepted), kept between `Nazor.Controls.NumberField.Minimum` and `Nazor.Controls.NumberField.Maximum` and written again with `Nazor.Controls.NumberField.Format`; text that is not a number goes back to the value, and incomplete text stays untouched while typing. Up and Down add or remove `Nazor.Controls.NumberField.Step`, Page Up and Page Down `Nazor.Controls.NumberField.LargeStep` (the engine's spin behavior), and the buttons repeat while held. The editor has the `spinbutton` role with the range for assistive technologies. Parts: `NumberFieldLeading`, `NumberFieldInput`, `NumberFieldUnit`, `NumberFieldSpinButtons`, `NumberFieldIncrement` and `NumberFieldDecrement` (disabled at the bounds). |
| `protected override void OnParametersSet()` | Validates the range and synchronizes the value and the text. |
| `public string Write(double? number)` | Writes a value in the field's format and culture.<br>`number`. The value, or null for an empty text.<br>**Returns**. The text. |
| `public bool TryRead(string input, out double? number)` | Reads text in the field's culture, accepting group separators and a trailing `Nazor.Controls.NumberField.Unit`. Evaluates arithmetic when `Nazor.Controls.NumberField.AcceptsExpression` is true. This method does not clamp or change the value.<br>`input`. The text.<br>`number`. The number read, or null for an empty text.<br>**Returns**. true if the text is empty or a finite number. |
| `protected override void AddAttributes(RenderTreeBuilder builder)` | Adds the frame states and the pointer and wheel handling.<br>`builder`. The builder that receives the render frames. |
| `protected override void AddContent(RenderTreeBuilder builder)` | Adds the side content, the buttons, the editor and the unit.<br>`builder`. The builder that receives the render frames. |
| `public Task IncrementAsync(bool large = false)` | Adds `Nazor.Controls.NumberField.Step` (or `Nazor.Controls.NumberField.LargeStep`) to the value, as the increment button does.<br>`large`. Whether to use the large step.<br>**Returns**. A task that completes when the change has been reported. |
| `public Task DecrementAsync(bool large = false)` | Subtracts `Nazor.Controls.NumberField.Step` (or `Nazor.Controls.NumberField.LargeStep`) from the value, as the decrement button does.<br>`large`. Whether to use the large step.<br>**Returns**. A task that completes when the change has been reported. |
| `public Task CommitAsync()` | Commits the current text as Enter would.<br>**Returns**. A task that completes when the value has been reported. |
| `public Task FocusAsync(bool preventScroll = false)` | Moves the keyboard focus to the editor.<br>`preventScroll`. Whether focusing keeps the current scroll position.<br>**Returns**. A task that completes when the focus has moved. |
| `public NumberField()` |  |
| `protected override int? ElementTabIndex { get; }` |  |
| `public double? Value { get; set; }` | `[Parameter]`<br>Gets or sets the value; null when the field is empty. |
| `public EventCallback<double?> ValueChanged { get; set; }` | `[Parameter]`<br>Gets or sets the callback invoked when the user commits or steps a new value; supports `@bind-Value`. |
| `public double Minimum { get; set; }` | `[Parameter]` Initial value `double.MinValue`.<br>Gets or sets the lowest value. |
| `public double Maximum { get; set; }` | `[Parameter]` Initial value `double.MaxValue`.<br>Gets or sets the highest value. |
| `public double Step { get; set; }` | `[Parameter]` Initial value `1`.<br>Gets or sets the change made by the buttons, Up, Down and the wheel; 1 by default. |
| `public double LargeStep { get; set; }` | `[Parameter]` Initial value `10`.<br>Gets or sets the change made by Page Up and Page Down; 10 by default. |
| `public string? Format { get; set; }` | `[Parameter]`<br>Gets or sets the .NET numeric format of the text, such as `N2` or `0.##`; the general format by default. |
| `public CultureInfo? Culture { get; set; }` | `[Parameter]`<br>Gets or sets the culture the number is written and read in; the current culture by default. |
| `public bool AcceptsExpression { get; set; }` | `[Parameter]`<br>Gets or sets whether committed text accepts arithmetic expressions; false by default. Operators are `^`, then `* /`, then `+ -`, evaluated left to right at equal precedence. Parentheses override precedence and unary signs precede powers. Operands use `Culture`; only finite results are accepted, and division by zero is invalid. Expressions are limited to 4096 characters and 64 nested parentheses. Enter, leaving the editor or stepping evaluates the expression, clamps the result and applies `Format` to its displayed text without rounding the stored value. Invalid text restores the previous value on commit. |
| `public string? Unit { get; set; }` | `[Parameter]`<br>Gets or sets a unit shown after the number, such as "kg"; it also describes the field to assistive technologies. |
| `public SpinButtonPlacement SpinButtons { get; set; }` | `[Parameter]`<br>Compact (default), Inline or Hidden, with theme-owned presentation. Fluent Compact keeps focus in the editor while its `NumberFieldSpinPopup` is open; Escape/outside dismiss, focus re-entry or `NumberFieldSpinToggle` reopen. ToggleUp/ToggleDown are decorative parts. Basic/NazorLeaf use an in-field Compact stack and opposite-side Inline buttons. |
| `public bool WheelChanges { get; set; }` | `[Parameter]`<br>Gets or sets whether the mouse wheel steps the value while the field has the focus. |
| `public string? Placeholder { get; set; }` | `[Parameter]`<br>Gets or sets the hint shown while the field is empty. |
| `public bool ReadOnly { get; set; }` | `[Parameter]`<br>Gets or sets whether the value cannot be changed while it can still be selected and copied. |
| `public bool Disabled { get; set; }` | `[Parameter]`<br>Gets or sets whether the field rejects interaction. |
| `public bool Required { get; set; }` | `[Parameter]`<br>Gets or sets whether a value is required; validation is application-owned. |
| `public bool Invalid { get; set; }` | `[Parameter]`<br>Gets or sets the externally determined invalid state. |
| `public FieldAppearance Appearance { get; set; }` | `[Parameter]`<br>Gets or sets how the frame is drawn. |
| `public ControlSize Size { get; set; }` | `[Parameter]` Initial value `ControlSize.Medium`.<br>Gets or sets the size step. |
| `public RenderFragment? Leading { get; set; }` | `[Parameter]`<br>Gets or sets content shown before the text, such as an `Nazor.Controls.Icon`. |
| `public bool AutoFocus { get; set; }` | `[Parameter]`<br>Gets or sets whether the editor takes the focus when it appears. |
| `public TimeSpan RepeatDelay { get; set; }` | `[Parameter]` Initial value `TimeSpan.FromMilliseconds(400)`.<br>Gets or sets how long a button must be held before it starts repeating. |
| `public TimeSpan RepeatInterval { get; set; }` | `[Parameter]` Initial value `TimeSpan.FromMilliseconds(60)`.<br>Gets or sets the interval between repetitions while a button is held. |
| `public FieldContext? Field { get; set; }` | `[CascadingParameter]`<br>Gets or sets the enclosing field, supplied automatically. |
| `public double? CurrentValue { get; }` | Gets the current value. |
| `public string Text { get; }` | Gets the current text, which may be uncommitted. |
| `public ElementReference InputElement { get; }` | Gets the editor element, which receives the focus. |
| `protected override string ElementName { get; }` | Gets the NSS element name emitted by NumberField. |

### `Orientation`

Namespace `Nazor.Controls`. [Source](../../Sources/Core/Nazor.Controls/Vocabulary.cs)

| Declaration | Contract |
| --- | --- |
| `public enum Orientation` | The direction in which a control lays out or moves, written as the `orientation` attribute. |
| `Horizontal = 0` | Left to right. |
| `Vertical = 1` | Top to bottom. |

### `PageHeader`

Namespace `Nazor.Controls`. [Source](../../Sources/Core/Nazor.Controls/Frame/PageHeader.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed class PageHeader : ContentControl` | The header of a page: an optional `Nazor.Controls.PageHeader.Breadcrumb` above, then `Nazor.Controls.PageHeader.Leading` content (such as a back button), the `Nazor.Controls.PageHeader.Title` as a heading (`Nazor.Controls.PageHeader.HeadingLevel`) with its `Nazor.Controls.PageHeader.Subtitle`, and the page's `Nazor.Controls.PageHeader.Actions` at the trailing edge; content between the tags follows below, such as tabs or filters. The parts are `PageHeaderBreadcrumb`, `PageHeaderMain`, `PageHeaderLeading`, `PageHeaderHeading` (with `PageHeaderTitle` and `PageHeaderSubtitle`), `PageHeaderActions` and `PageHeaderContent`. `Nazor.Controls.PageHeader.IsCompact` writes the `compact` attribute, which themes draw smaller, for example once the page has scrolled. |
| `protected override void OnParametersSet()` | Validates the heading level. |
| `protected override void AddAttributes(RenderTreeBuilder builder)` | Adds the compact state.<br>`builder`. The builder that receives the render frames. |
| `protected override void AddContent(RenderTreeBuilder builder)` | Adds the breadcrumb, the main row and the content below.<br>`builder`. The builder that receives the render frames. |
| `public PageHeader()` |  |
| `public string? Title { get; set; }` | `[Parameter]`<br>Gets or sets the title of the page. |
| `public RenderFragment? TitleContent { get; set; }` | `[Parameter]`<br>Gets or sets rich title content, used instead of `Nazor.Controls.PageHeader.Title`. |
| `public string? Subtitle { get; set; }` | `[Parameter]`<br>Gets or sets a secondary line under the title. |
| `public RenderFragment? Actions { get; set; }` | `[Parameter]`<br>Gets or sets the commands of the page, at the trailing edge. |
| `public RenderFragment? Breadcrumb { get; set; }` | `[Parameter]`<br>Gets or sets the path to the page shown above the title, such as a breadcrumb. |
| `public RenderFragment? Leading { get; set; }` | `[Parameter]`<br>Gets or sets content before the title, such as a back button or an avatar. |
| `public int HeadingLevel { get; set; }` | `[Parameter]` Initial value `1`.<br>Gets or sets the heading level of the title, from 1 to 6; 1 by default. |
| `public bool IsCompact { get; set; }` | `[Parameter]`<br>Gets or sets whether the header is drawn in its compact form. |
| `protected override string ElementName { get; }` | Gets the NSS element name emitted by PageHeader. |

### `Pagination`

Namespace `Nazor.Controls`. [Source](../../Sources/Core/Nazor.Controls/Collections/Pagination.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed class Pagination : Control` | Moves through the pages of a long collection: buttons for the first, previous, numbered, next and last pages, with ellipses where pages are skipped. `Nazor.Controls.Pagination.Page` (`@bind-Page`) is one-based; the number of pages is `Nazor.Controls.Pagination.PageCount`, or is computed from `Nazor.Controls.Pagination.TotalItems` and `Nazor.Controls.Pagination.PageSize` (`@bind-PageSize`, chosen among `Nazor.Controls.Pagination.PageSizes` when `Nazor.Controls.Pagination.ShowPageSize` is set). `Nazor.Controls.Pagination.SiblingCount` pages are shown on each side of the current one and `Nazor.Controls.Pagination.BoundaryCount` at each end. The element `Pagination` (role `group`, named "Pagination") holds the buttons `PaginationFirst`, `PaginationPrevious`, `PaginationPage` (the current one has `current`), `PaginationNext` and `PaginationLast`, each one a Tab stop with a localized name, the `PaginationEllipsis` parts, `PaginationSummary` and `PaginationPageSize` (with `PaginationPageSizeLabel` and a `Nazor.Controls.Select<TValue>`). |
| `public static IReadOnlyList<int?> Pages(int current, int count, int siblingCount = 1, int boundaryCount = 1)` | Computes the pages to show: the first and last `boundaryCount` pages, the current page with `siblingCount` pages on each side, and null for an ellipsis in place of the pages skipped.<br>`current`. The current page, starting from one.<br>`count`. The number of pages.<br>`siblingCount`. The pages on each side of the current one.<br>`boundaryCount`. The pages at each end.<br>**Returns**. The page numbers in order, with null for each ellipsis. |
| `protected override void OnParametersSet()` | Validates the parameters and synchronizes the page and the page size. |
| `protected override void AddAttributes(RenderTreeBuilder builder)` | Adds the group role, its localized name, the size and the disabled state.<br>`builder`. The builder that receives the render frames. |
| `protected override void AddContent(RenderTreeBuilder builder)` | Adds the navigation buttons, the pages, the summary and the page size chooser.<br>`builder`. The builder that receives the render frames. |
| `public Task GoToAsync(int target)` | Goes to a page as a user action does, reporting the change.<br>`target`. The page, clamped to the valid pages.<br>**Returns**. A task that completes when the change has been reported. |
| `public Pagination()` |  |
| `public int Page { get; set; }` | `[Parameter]` Initial value `1`.<br>Gets or sets the current page, starting from one. |
| `public EventCallback<int> PageChanged { get; set; }` | `[Parameter]`<br>Gets or sets the callback invoked when the user goes to another page. |
| `public int? PageCount { get; set; }` | `[Parameter]`<br>Gets or sets the number of pages; when null it is computed from `Nazor.Controls.Pagination.TotalItems` and `Nazor.Controls.Pagination.PageSize`. |
| `public int? TotalItems { get; set; }` | `[Parameter]`<br>Gets or sets the number of items of the collection, used with `Nazor.Controls.Pagination.PageSize` when `Nazor.Controls.Pagination.PageCount` is null. |
| `public int PageSize { get; set; }` | `[Parameter]` Initial value `10`.<br>Gets or sets the number of items per page. |
| `public EventCallback<int> PageSizeChanged { get; set; }` | `[Parameter]`<br>Gets or sets the callback invoked when the user chooses another page size. |
| `public IReadOnlyList<int>? PageSizes { get; set; }` | `[Parameter]`<br>Gets or sets the page sizes offered by `Nazor.Controls.Pagination.ShowPageSize`; 10, 25, 50 and 100 when null. |
| `public int SiblingCount { get; set; }` | `[Parameter]` Initial value `1`.<br>Gets or sets how many pages are shown on each side of the current page. |
| `public int BoundaryCount { get; set; }` | `[Parameter]` Initial value `1`.<br>Gets or sets how many pages are always shown at each end. |
| `public bool ShowFirstLast { get; set; }` | `[Parameter]`<br>Gets or sets whether the buttons for the first and the last page are shown. |
| `public bool ShowPreviousNext { get; set; }` | `[Parameter]` Initial value `true`.<br>Gets or sets whether the buttons for the previous and the next page are shown. |
| `public bool ShowPageSize { get; set; }` | `[Parameter]`<br>Gets or sets whether a drop-down list lets the user choose the page size. |
| `public bool ShowSummary { get; set; }` | `[Parameter]`<br>Gets or sets whether a localized "Page 3 of 10" summary is shown. |
| `public bool Disabled { get; set; }` | `[Parameter]`<br>Gets or sets whether the pagination rejects interaction. |
| `public ControlSize Size { get; set; }` | `[Parameter]` Initial value `ControlSize.Medium`.<br>Gets or sets the size step. |
| `public int CurrentPage { get; }` | Gets the current page. |
| `public int Count { get; }` | Gets the number of pages, at least one. |
| `protected override string ElementName { get; }` | Gets the element name. |

### `Paragraph`

Namespace `Nazor.Controls`. [Source](../../Sources/Core/Nazor.Controls/Text/Paragraph.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed class Paragraph : ContentControl` | A block of running text, such as the body of an article or the explanation in a dialog. Consecutive paragraphs and other document blocks are spaced by the theme. The element is `Paragraph` with the `text` role. |
| `protected override void AddAttributes(RenderTreeBuilder builder)` | Adds the text role and the selection behavior.<br>`builder`. The builder that receives the render frames. |
| `public Paragraph()` |  |
| `public bool IsTextSelectionEnabled { get; set; }` | `[Parameter]`<br>Gets or sets whether the text can be selected with the pointer and copied with the keyboard or the context menu. The paragraph takes the focus when clicked, but Tab does not stop there. |
| `protected override string ElementName { get; }` | Gets the NSS element name emitted by Paragraph. |

### `PasswordField`

Namespace `Nazor.Controls`. [Source](../../Sources/Core/Nazor.Controls/Fields/PasswordField.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed class PasswordField : TextFieldBase` | A text field whose characters are masked. The parts are those of a text field (`PasswordFieldLeading`, `PasswordFieldInput`, `PasswordFieldTrailing`, `PasswordFieldCounter`) plus: `PasswordFieldReveal`, a toggle button (`pressed` while the text is shown) named "Show password" or "Hide password"; it is not a Tab stop, so Alt+F8 in the editor toggles it too, as in Windows. `PasswordFieldCapsLock`, a polite status shown while the editor has the focus and Caps Lock seems on. The engine does not report the Caps Lock state, so the field infers it: a letter typed in upper case without Shift (or in lower case with Shift) means Caps Lock is on, a letter typed in the expected case means it is off, and the Caps Lock key flips a state already inferred. Until the first letter is typed the state is unknown and no warning shows. `PasswordFieldStrength`, a meter (0 to 1) computed by `Nazor.Controls.PasswordField.Strength`, with its `level` attribute (`none`, `weak`, `fair`, `good`, `strong`), a `PasswordFieldStrengthBar` track holding a `PasswordFieldStrengthFill` as wide as the score (`--nz-strength`), and a `PasswordFieldStrengthText` naming the level. |
| `public static PasswordStrengthLevel LevelOf(double score, bool empty = false)` | Gets the level of a strength score.<br>`score`. The score from 0 to 1.<br>`empty`. Whether there is no password yet.<br>**Returns**. The level. |
| `public static double EstimateStrength(string password)` | Scores a password from its length and the kinds of characters it mixes (lower case, upper case, digits, symbols): a rough estimate for forms without a dedicated policy, not a security measure.<br>`password`. The password.<br>**Returns**. A score from 0 to 1. |
| `protected override void OnParametersSet()` | Synchronizes the revealed state. |
| `protected override void AddAttributes(RenderTreeBuilder builder)` | Adds the frame states and the revealed state.<br>`builder`. The builder that receives the render frames. |
| `protected override void AddEditorParameters(RenderTreeBuilder builder)` | Adds the masking and the key tracking to the editor.<br>`builder`. The builder positioned on the editor component. |
| `protected override void AddAfterInput(RenderTreeBuilder builder)` | Adds the Caps Lock warning and the reveal button.<br>`builder`. The builder that receives the render frames. |
| `protected override void AddContent(RenderTreeBuilder builder)` | Adds the text field parts and the strength meter.<br>`builder`. The builder that receives the render frames. |
| `protected override Task TextChangedAsync(string previous, string value)` | Infers the Caps Lock state from the case of a typed letter.<br>`previous`. The text before the edit.<br>`value`. The new text.<br>**Returns**. A completed task. |
| `public Task SetRevealedAsync(bool value)` | Shows or hides the text as the reveal button would.<br>`value`. Whether the text is shown.<br>**Returns**. A task that completes when the change has been reported. |
| `public PasswordField()` |  |
| `public bool Revealable { get; set; }` | `[Parameter]` Initial value `true`.<br>Gets or sets whether the reveal button is offered; true by default. |
| `public bool Revealed { get; set; }` | `[Parameter]`<br>Gets or sets whether the text is shown instead of masked. |
| `public EventCallback<bool> RevealedChanged { get; set; }` | `[Parameter]`<br>Gets or sets the callback invoked when the user shows or hides the text; supports `@bind-Revealed`. |
| `public bool ShowCapsLockWarning { get; set; }` | `[Parameter]` Initial value `true`.<br>Gets or sets whether the Caps Lock warning can appear; true by default. |
| `public Func<string, double>? Strength { get; set; }` | `[Parameter]`<br>Gets or sets the function that scores a password from 0 (weakest) to 1 (strongest); null hides the strength meter. `Nazor.Controls.PasswordField.EstimateStrength(System.String)` is a simple default. |
| `public char MaskCharacter { get; set; }` | `[Parameter]` Initial value `'●'`.<br>Gets or sets the character drawn for each masked character. |
| `public bool IsRevealed { get; }` | Gets whether the text is currently shown. |
| `public bool? CapsLock { get; }` | Gets whether Caps Lock is inferred to be on; null while unknown. |
| `protected override string ElementName { get; }` | Gets the NSS element name emitted by PasswordField. |
| `protected override bool Multiline { get; }` | Gets that Enter does not insert a line break. |

### `PasswordStrengthLevel`

Namespace `Nazor.Controls`. [Source](../../Sources/Core/Nazor.Controls/Fields/PasswordField.cs)

| Declaration | Contract |
| --- | --- |
| `public enum PasswordStrengthLevel` | The level of a password strength score, written as the `level` attribute of the strength meter. |
| `None = 0` | No password yet. |
| `Weak = 1` | A score below 0.25. |
| `Fair = 2` | A score from 0.25 to below 0.5. |
| `Good = 3` | A score from 0.5 to below 0.75. |
| `Strong = 4` | A score of 0.75 or more. |

### `PickerFieldBase<TValue>`

Namespace `Nazor.Controls`. [Source](../../Sources/Core/Nazor.Controls/Pickers/PickerFieldBase.cs)

| Declaration | Contract |
| --- | --- |
| `public abstract class PickerFieldBase<TValue> : Control where TValue : struct` | The shared frame of the pickers that edit one value as text and offer a popup to choose it: a frame named after the control with the `appearance`, `size`, `disabled`, `readonly`, `invalid`, `empty` and `expanded` attributes, the `{name}Input` editor (role `combobox`) that parses the typed text in `Nazor.Controls.PickerFieldBase<TValue>.Culture`, an optional `{name}Clear` button, the `{name}Button` that opens the popup and the `{name}Popup` surface. Typing a text that parses updates `Nazor.Controls.PickerFieldBase<TValue>.Value` at once; a text that does not parse, or a value outside the limits, keeps the value and marks the frame and the editor `invalid` (`Nazor.Controls.PickerFieldBase<TValue>.IsTextInvalid`). Enter or leaving the editor rewrites a valid text in the display format. Alt+Down or F4 open the popup, Escape or Alt+Up close it. Inside a `Nazor.Controls.PickerFieldBase<TValue>.Field` the editor is labeled and described by the field.<br>`TValue`. The type of the value. |
| `protected abstract string FormatValue(TValue value)` | Formats a value for the editor.<br>`value`. The value.<br>**Returns**. The display text. |
| `protected abstract bool TryParseText(string text, out TValue value)` | Parses the text typed in the editor.<br>`text`. The trimmed, non-empty text.<br>`value`. The parsed value.<br>**Returns**. true if the text parses. |
| `protected virtual bool IsAllowed(TValue value)` | Tells whether a parsed value is within the limits.<br>`value`. The value.<br>**Returns**. true if the value can be chosen. |
| `protected abstract void RenderPopup(RenderTreeBuilder builder)` | Renders the content of the popup.<br>`builder`. The builder positioned inside the popup. |
| `protected override void OnParametersSet()` | Validates the shared parameters and synchronizes the value, the text and the open state. |
| `protected override void AddAttributes(RenderTreeBuilder builder)` | Adds the frame states and the keyboard handling.<br>`builder`. The builder that receives the render frames. |
| `protected override void AddContent(RenderTreeBuilder builder)` | Adds the editor, the clear button, the popup button and the popup.<br>`builder`. The builder that receives the render frames. |
| `protected virtual ElementReference? InitialFocus()` | Chooses the element focused when the popup opens: the first element holding a Tab stop inside it.<br>**Returns**. The element, or null to focus the first focusable one. |
| `public Task OpenAsync()` | Opens the popup unless the picker is disabled or read-only.<br>**Returns**. A task that completes when the change has been reported. |
| `public Task CloseAsync()` | Closes the popup.<br>**Returns**. A task that completes when the change has been reported. |
| `public Task FocusAsync(bool preventScroll = false)` | Moves the keyboard focus to the editor.<br>`preventScroll`. Whether focusing keeps the current scroll position.<br>**Returns**. A task that completes when the focus has moved. |
| `protected Task SelectAsync(TValue? next, bool close)` | Changes the value as the user would, rewrites the text and reports the change.<br>`next`. The new value, or null to clear.<br>`close`. Whether the popup closes and the focus returns to the editor.<br>**Returns**. A task that completes when the change has been reported. |
| `protected PickerFieldBase()` |  |
| `protected override int? ElementTabIndex { get; }` |  |
| `public TValue? Value { get; set; }` | `[Parameter]`<br>Gets or sets the value, or null when empty. |
| `public EventCallback<TValue?> ValueChanged { get; set; }` | `[Parameter]`<br>Gets or sets the callback invoked when the user changes the value. |
| `public CultureInfo? Culture { get; set; }` | `[Parameter]`<br>Gets or sets the culture that formats and parses the text; the current culture by default. |
| `public string? Placeholder { get; set; }` | `[Parameter]`<br>Gets or sets the hint shown while the text is empty; null shows the expected pattern. |
| `public bool Disabled { get; set; }` | `[Parameter]`<br>Gets or sets whether the picker rejects interaction. |
| `public bool ReadOnly { get; set; }` | `[Parameter]`<br>Gets or sets whether the value cannot change while it can still be read and copied. |
| `public bool Required { get; set; }` | `[Parameter]`<br>Gets or sets whether a value is required; validation is application-owned. |
| `public bool Invalid { get; set; }` | `[Parameter]`<br>Gets or sets the externally determined invalid state, combined with the parsing state. |
| `public bool Clearable { get; set; }` | `[Parameter]`<br>Gets or sets whether a button clears the value when there is one. |
| `public FieldAppearance Appearance { get; set; }` | `[Parameter]`<br>Gets or sets how the frame is drawn. |
| `public ControlSize Size { get; set; }` | `[Parameter]` Initial value `ControlSize.Medium`.<br>Gets or sets the size step. |
| `public bool IsOpen { get; set; }` | `[Parameter]`<br>Gets or sets whether the popup is open. |
| `public EventCallback<bool> IsOpenChanged { get; set; }` | `[Parameter]`<br>Gets or sets the callback invoked when the popup opens or closes. |
| `public FieldContext? Field { get; set; }` | `[CascadingParameter]`<br>Gets or sets the enclosing field, supplied automatically. |
| `public TValue? CurrentValue { get; }` | Gets the current value. |
| `public string Text { get; }` | Gets the text in the editor. |
| `public bool IsTextInvalid { get; }` | Gets whether the text in the editor does not parse to an allowed value. |
| `public bool Opened { get; }` | Gets whether the popup is open. |
| `public ElementReference InputElement { get; }` | Gets the editor element, which receives the focus. |
| `protected CultureInfo EffectiveCulture { get; }` | Gets the culture in use. |
| `protected bool IsInactive { get; }` | Gets whether the picker rejects interaction, including through its field. |
| `protected abstract string ButtonLabel { get; }` | Gets the accessible name of the button and of the popup. |
| `protected virtual string? PatternHint { get; }` | Gets the pattern shown when there is no `Nazor.Controls.PickerFieldBase<TValue>.Placeholder`. |
| `protected virtual string FormatKey { get; }` | Gets a key that changes when the display format changes, so the text is rewritten. |

### `PinField`

Namespace `Nazor.Controls`. [Source](../../Sources/Core/Nazor.Controls/Fields/PinField.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed class PinField : Control` | A short code typed one character per cell, such as a PIN or a verification code. `Nazor.Controls.PinField.Value` (`@bind-Value`) is the code typed so far, without gaps; `Nazor.Controls.PinField.OnComplete` reports it once every cell is filled. Typing fills the current cell and moves to the next one, Backspace in an empty cell clears the previous one and moves back, the arrows, Home and End move between the filled cells, and pasting fills the cells from the current one. `Nazor.Controls.PinField.Numeric` accepts digits only (letters and digits otherwise), `Nazor.Controls.PinField.Mask` hides the characters. The field is one Tab stop: Tab enters at the first empty cell. The element has the `group` role and is named by its field; the parts are `PinFieldCell` editors (with `filled` and `index`, each described as "Character n of m") and, with `Nazor.Controls.PinField.GroupSize`, `PinFieldSeparator` parts between groups of cells. |
| `protected override void OnParametersSet()` | Validates the parameters and synchronizes the code. |
| `public bool Accepts(char c)` | Gets whether a character is accepted in a cell.<br>`c`. The character.<br>**Returns**. true for digits, and for letters unless `Nazor.Controls.PinField.Numeric`. |
| `protected override void AddAttributes(RenderTreeBuilder builder)` | Adds the group role, the naming and the states.<br>`builder`. The builder that receives the render frames. |
| `protected override void AddContent(RenderTreeBuilder builder)` | Adds one editor per cell and the separators.<br>`builder`. The builder that receives the render frames. |
| `public Task SetCodeAsync(string code)` | Replaces the code as typing would, keeping only accepted characters, and reports it.<br>`code`. The new code.<br>**Returns**. A task that completes when the change has been reported. |
| `public Task ClearAsync()` | Empties every cell and reports it.<br>**Returns**. A task that completes when the change has been reported. |
| `public Task FocusAsync(bool preventScroll = false)` | Moves the keyboard focus to the entry cell.<br>`preventScroll`. Whether focusing keeps the current scroll position.<br>**Returns**. A task that completes when the focus has moved. |
| `public PinField()` |  |
| `public int Length { get; set; }` | `[Parameter]` Initial value `6`.<br>Gets or sets the number of cells; 6 by default. |
| `public string? Value { get; set; }` | `[Parameter]`<br>Gets or sets the code typed so far. |
| `public EventCallback<string> ValueChanged { get; set; }` | `[Parameter]`<br>Gets or sets the callback invoked on every change of the code; supports `@bind-Value`. |
| `public EventCallback<string> OnComplete { get; set; }` | `[Parameter]`<br>Gets or sets the callback invoked with the code when the last empty cell is filled. |
| `public bool Numeric { get; set; }` | `[Parameter]` Initial value `true`.<br>Gets or sets whether only digits are accepted; true by default, letters and digits otherwise. |
| `public bool Mask { get; set; }` | `[Parameter]`<br>Gets or sets whether the characters are hidden. |
| `public char MaskCharacter { get; set; }` | `[Parameter]` Initial value `'●'`.<br>Gets or sets the character drawn for each hidden character. |
| `public int GroupSize { get; set; }` | `[Parameter]`<br>Gets or sets the number of cells between separators, such as 3 for "123 456"; zero for none. |
| `public bool Disabled { get; set; }` | `[Parameter]`<br>Gets or sets whether the field rejects interaction. |
| `public bool Required { get; set; }` | `[Parameter]`<br>Gets or sets whether a code is required; validation is application-owned. |
| `public bool Invalid { get; set; }` | `[Parameter]`<br>Gets or sets the externally determined invalid state. |
| `public ControlSize Size { get; set; }` | `[Parameter]` Initial value `ControlSize.Medium`.<br>Gets or sets the size step. |
| `public bool AutoFocus { get; set; }` | `[Parameter]`<br>Gets or sets whether the entry cell takes the focus when it appears. |
| `public FieldContext? Field { get; set; }` | `[CascadingParameter]`<br>Gets or sets the enclosing field, supplied automatically. |
| `public string Code { get; }` | Gets the code typed so far. |
| `public bool IsComplete { get; }` | Gets whether every cell is filled. |
| `public int EntryIndex { get; }` | Gets the index of the cell the Tab key enters: the first empty one, or the last. |
| `protected override string ElementName { get; }` | Gets the NSS element name emitted by PinField. |

### `PipsPager`

Namespace `Nazor.Controls`. [Source](../../Sources/Core/Nazor.Controls/Navigation/PipsPager.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed class PipsPager : Control` | A finite page selector independent of content; one dot Tab stop with axis/Home/End navigation. |
| `public PipsPager()` | Creates a bounded five-dot pager. |
| `public int Count { get; set; }` | `[Parameter]` Number of pages, nonnegative; zero renders no dots. |
| `public int SelectedIndex { get; set; }` | `[Parameter]` Zero-based selection, clamped to pages. |
| `public EventCallback<int> SelectedIndexChanged { get; set; }` | `[Parameter]` One callback per actual user change; parameter/count normalization is silent. |
| `public int MaxVisiblePips { get; set; }` | `[Parameter]` Five by default; accepts 1–1000. Realization follows selection without growing with Count. |
| `public bool ShowNavigationButtons { get; set; }` | `[Parameter]` True by default; previous/next actions are outside dot Tab order. |
| `public Orientation Orientation { get; set; }` | `[Parameter]` Horizontal by default; Vertical uses Up/Down. |
| `public bool Disabled { get; set; }` | `[Parameter]` Rejects all page changes. |
| `public int CurrentIndex { get; }` | Normalized index; -1 when empty. |
| `public Task SelectAsync(int position)` | Clamps and selects; disabled, empty or unchanged requests do nothing. Returns after callbacks. |
| `protected override string ElementName { get; }` | PipsPager. |
| `protected override void OnParametersSet()` | Validates count/window/orientation and synchronizes selection silently. |
| `protected override void AddAttributes(RenderTreeBuilder builder)` | Localized group semantics and keyboard handler. |
| `protected override void AddContent(RenderTreeBuilder builder)` | Bounded realized dot window and optional actions. |
| `protected override void OnAfterRender(bool firstRender)` | Focuses the selected realized dot after a focused window change, without scrolling ancestors. |

Parts: `PipsPagerPrevious`, `PipsPagerItems`, `PipsPagerPip`, decorative `PipsPagerDot`, `PipsPagerNext`. Root attributes: `orientation`, `disabled`, `selected-index`, `item-count`, `empty`. Pips carry `current`, `index`, `position-in-set`, `size-of-set`, localized accessible name and roving `tabindex`. Optional arrows stop at bounds. Bind independently to a page presenter; no hard FlipView dependency, infinite-page model or native OS behavior is implied.

### `Popover`

Namespace `Nazor.Controls`. [Source](../../Sources/Core/Nazor.Controls/Overlays/Popover.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed class Popover : ComponentBase, IDisposable` | Interactive content anchored to a `Nazor.Controls.Popover.Trigger` (or to `Nazor.Controls.Popover.Anchor`): a click on the trigger toggles it, a press outside or Escape closes it and the focus returns to the trigger. With `Nazor.Controls.Popover.OpenOnHover` it is a hover card: it opens after `Nazor.Controls.Popover.HoverDelay` while the pointer rests on the trigger, or at once when the keyboard focus reaches it, and closes after `Nazor.Controls.Popover.HoverCloseDelay` once both the pointer and the focus have left the trigger and the card. `Nazor.Controls.Popover.IsOpen` supports `@bind-IsOpen`. Parts: `PopoverTrigger` (`expanded`), `Popover` (`side`), `PopoverArrow` (`placement`), `PopoverHeader`, `PopoverTitle`, `PopoverClose`, `PopoverBody` and `PopoverFooter`. |
| `protected override void OnParametersSet()` | Synchronizes the open state and validates the parameters. |
| `protected override void BuildRenderTree(RenderTreeBuilder builder)` | Renders the trigger wrapper and, while open, the surface.<br>`builder`. The builder that receives the render frames. |
| `public Task OpenAsync()` | Opens the popover.<br>**Returns**. A task that completes when the change has been reported. |
| `public Task CloseAsync()` | Closes the popover.<br>**Returns**. A task that completes when the change has been reported. |
| `public void Dispose()` | Cancels a pending hover change. |
| `public Popover()` |  |
| `public RenderFragment? Trigger { get; set; }` | `[Parameter]`<br>Gets or sets the content that opens the popover, usually a `Nazor.Controls.Button`. |
| `public ElementReference? Anchor { get; set; }` | `[Parameter]`<br>Gets or sets the element the popover is attached to when there is no `Nazor.Controls.Popover.Trigger`. |
| `public RenderFragment? ChildContent { get; set; }` | `[Parameter]`<br>Gets or sets the body of the popover. |
| `public string? Title { get; set; }` | `[Parameter]`<br>Gets or sets the title shown in the header, which names the popover. |
| `public RenderFragment? Header { get; set; }` | `[Parameter]`<br>Gets or sets rich header content, used instead of `Nazor.Controls.Popover.Title`. |
| `public RenderFragment? Footer { get; set; }` | `[Parameter]`<br>Gets or sets the footer, such as actions. |
| `public bool ShowCloseButton { get; set; }` | `[Parameter]`<br>Gets or sets whether the header shows a close button. |
| `public bool IsOpen { get; set; }` | `[Parameter]`<br>Gets or sets whether the popover is open. |
| `public EventCallback<bool> IsOpenChanged { get; set; }` | `[Parameter]`<br>Gets or sets the callback invoked when the popover opens or closes from inside. |
| `public OverlayPlacement Placement { get; set; }` | `[Parameter]`<br>Gets or sets the preferred side of the trigger. |
| `public OverlayAlignment Alignment { get; set; }` | `[Parameter]`<br>Gets or sets the alignment with the trigger. |
| `public double Offset { get; set; }` | `[Parameter]` Initial value `4`.<br>Gets or sets the distance from the trigger, in logical units. |
| `public bool ShowArrow { get; set; }` | `[Parameter]`<br>Gets or sets whether an arrow points at the trigger. |
| `public bool OpenOnHover { get; set; }` | `[Parameter]`<br>Gets or sets whether the popover is a hover card that opens on hover and focus. |
| `public TimeSpan HoverDelay { get; set; }` | `[Parameter]` Initial value `TimeSpan.FromMilliseconds(400)`.<br>Gets or sets the delay before a hover card opens. |
| `public TimeSpan HoverCloseDelay { get; set; }` | `[Parameter]` Initial value `TimeSpan.FromMilliseconds(300)`.<br>Gets or sets the delay before a hover card closes. |
| `public bool? FocusOnOpen { get; set; }` | `[Parameter]`<br>Gets or sets whether opening moves the focus into the popover; null moves it except for hover cards. |
| `public bool CloseOnOutside { get; set; }` | `[Parameter]` Initial value `true`.<br>Gets or sets whether a press outside closes the popover. |
| `public bool CloseOnEscape { get; set; }` | `[Parameter]` Initial value `true`.<br>Gets or sets whether Escape closes the popover. |
| `public EventCallback OnDismiss { get; set; }` | `[Parameter]`<br>Gets or sets the callback invoked when the user dismisses the popover. |
| `public bool Disabled { get; set; }` | `[Parameter]`<br>Gets or sets whether the trigger does not open the popover. |
| `public string Role { get; set; }` | `[Parameter]` Initial value `"dialog"`.<br>Gets or sets the accessible role of the surface, `dialog` by default. |
| `public string? Class { get; set; }` | `[Parameter]`<br>Gets or sets the space-separated NSS classes of the surface. |
| `public string? Style { get; set; }` | `[Parameter]`<br>Gets or sets NSS declarations for the surface only. |
| `public string? AccessibleName { get; set; }` | `[Parameter]`<br>Gets or sets the accessible name; the title names the popover when null. |
| `public bool Opened { get; }` | Gets whether the popover is open. |

### `ProgressBar`

Namespace `Nazor.Controls`. [Source](../../Sources/Core/Nazor.Controls/Feedback/ProgressBar.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed class ProgressBar : Control` | The progress of a task as a horizontal bar. `Nazor.Controls.ProgressBar.Value` between `Nazor.Controls.ProgressBar.Minimum` and `Nazor.Controls.ProgressBar.Maximum` is the completed work; null means the amount is unknown (the `indeterminate` attribute, and no `range-value` for assistive technologies). The parts are `ProgressBarLabel` (the visible `Nazor.Controls.ProgressBar.Label`), `ProgressBarValue` (the percentage, with `Nazor.Controls.ProgressBar.ShowValue`) and `ProgressBarTrack`, which holds `ProgressBarBuffer` (`Nazor.Controls.ProgressBar.BufferValue`) and `ProgressBarFill`, an optional second `ProgressBarPulse` for themed indeterminate motion, or with `Nazor.Controls.ProgressBar.Segments` one `ProgressBarSegment` per block, each with the `filled` attribute when the progress covers it. The element publishes the variables `--nz-progress-fraction` and `--nz-progress-buffer` (0 to 1) and the `intent` and `size` attributes. |
| `protected override void OnParametersSet()` | Validates the range, the look and the number of segments. |
| `protected override void AddAttributes(RenderTreeBuilder builder)` | Adds the progressbar role, the range and the look attributes.<br>`builder`. The builder that receives the render frames. |
| `protected override void AddContent(RenderTreeBuilder builder)` | Adds the label, the value and the track with its fill, buffer or segments.<br>`builder`. The builder that receives the render frames. |
| `public ProgressBar()` |  |
| `public double? Value { get; set; }` | `[Parameter]`<br>Gets or sets the completed work, or null when it is unknown. |
| `public double Minimum { get; set; }` | `[Parameter]`<br>Gets or sets the value of no progress; 0 by default. |
| `public double Maximum { get; set; }` | `[Parameter]` Initial value `100`.<br>Gets or sets the value of complete progress; 100 by default. |
| `public double? BufferValue { get; set; }` | `[Parameter]`<br>Gets or sets a secondary amount ahead of the value, such as the downloaded part of a video being played, or null. |
| `public string? Label { get; set; }` | `[Parameter]`<br>Gets or sets the visible label, which also names the bar for assistive technologies. |
| `public bool ShowValue { get; set; }` | `[Parameter]`<br>Gets or sets whether the value part shows the completed percentage (nothing while indeterminate). |
| `public string? ValueText { get; set; }` | `[Parameter]`<br>Gets or sets the text of the value part instead of the percentage, such as "3 of 7 files". |
| `public Intent Intent { get; set; }` | `[Parameter]` Initial value `Intent.Accent`.<br>Gets or sets the meaning of the bar's color, such as success or danger. |
| `public ControlSize Size { get; set; }` | `[Parameter]` Initial value `ControlSize.Medium`.<br>Gets or sets the size step, which themes use for the thickness. |
| `public int Segments { get; set; }` | `[Parameter]`<br>Gets or sets the number of blocks of a segmented bar; 0 draws a continuous bar. |
| `public bool IsIndeterminate { get; }` | Gets whether the amount of completed work is unknown. |
| `public double Fraction { get; }` | Gets the completed fraction from 0 to 1, or 0 while indeterminate. |
| `protected override string ElementName { get; }` | Gets the NSS element name emitted by ProgressBar. |
| `protected override string? ElementStyle { get; }` | Gets the fraction variables and the caller's declarations. |

### `PropertyGrid`

Namespace `Nazor.Controls`. [Source](../../Sources/Core/Nazor.Controls/Data/PropertyGrid.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed class PropertyGrid : Control` | Shows and edits the properties of an object: from reflection over `Nazor.Controls.PropertyGrid.Target`, or from explicit `Nazor.Controls.PropertyGrid.Items`. Properties are listed under collapsible categories or alphabetically (`Nazor.Controls.PropertyGrid.Order`), filtered by a search field, and edited in place: text fields for strings, number editors for numbers, check boxes for booleans, drop-down lists for enumerations, and custom editors through `Nazor.Controls.PropertyGrid.EditorTemplate`. The description pane shows the selected property's description. The parts are `PropertyGridToolbar`, `PropertyGridOrder` with `PropertyGridOrderOption` (`checked`), a `TextField` for the search, `PropertyGridList`, `PropertyGridCategory` (`expanded`) with `PropertyGridCategoryHeader`, `PropertyGridCategoryToggle` and `PropertyGridCategoryBody`, `PropertyGridRow` (`selected`, `readonly`, `modified`), `PropertyGridName`, `PropertyGridValue`, `PropertyGridNumber`, `PropertyGridReadOnlyValue`, `PropertyGridEmpty`, `PropertyGridDescription`, `PropertyGridDescriptionTitle` and `PropertyGridDescriptionText`. |
| `protected override void OnParametersSet()` | Validates the parameters and describes the target. |
| `protected override void AddAttributes(RenderTreeBuilder builder)` | Adds the group role and the order attribute.<br>`builder`. The builder that receives the render frames. |
| `protected override void AddContent(RenderTreeBuilder builder)` | Adds the toolbar, the properties and the description pane.<br>`builder`. The builder that receives the render frames. |
| `public Task SetValueAsync(PropertyGridItem item, object? value)` | Stores a new value in a property as an editor does and reports the change.<br>`item`. The property.<br>`value`. The new value.<br>**Returns**. A task that completes when the change has been reported. |
| `public PropertyGrid()` |  |
| `public object? Target { get; set; }` | `[Parameter]`<br>Gets or sets the object whose public properties are shown, when `Nazor.Controls.PropertyGrid.Items` is null. The setter requires preserved reflection metadata and carries `RequiresUnreferencedCode`. Use explicit `Items` and `ChoiceText` formatters for trimming and Native AOT. |
| `public IReadOnlyList<PropertyGridItem>? Items { get; set; }` | `[Parameter]`<br>Gets or sets the properties shown, replacing reflection over `Nazor.Controls.PropertyGrid.Target`. |
| `public PropertyGridOrder Order { get; set; }` | `[Parameter]`<br>Gets or sets how the properties are ordered. |
| `public EventCallback<PropertyGridOrder> OrderChanged { get; set; }` | `[Parameter]`<br>Gets or sets the callback invoked when the user changes the order; supports `@bind-Order`. |
| `public string? Search { get; set; }` | `[Parameter]`<br>Gets or sets the search text that filters properties by name, category and description. |
| `public EventCallback<string> SearchChanged { get; set; }` | `[Parameter]`<br>Gets or sets the callback invoked when the user changes the search; supports `@bind-Search`. |
| `public string? SelectedName { get; set; }` | `[Parameter]`<br>Gets or sets the name of the selected property. |
| `public EventCallback<string?> SelectedNameChanged { get; set; }` | `[Parameter]`<br>Gets or sets the callback invoked when the user selects a property; supports `@bind-SelectedName`. |
| `public bool ShowToolbar { get; set; }` | `[Parameter]` Initial value `true`.<br>Gets or sets whether the toolbar with the order choice and the search field is shown. |
| `public bool ShowDescription { get; set; }` | `[Parameter]` Initial value `true`.<br>Gets or sets whether the description pane is shown. |
| `public bool ReadOnly { get; set; }` | `[Parameter]`<br>Gets or sets whether every property is shown without editing. |
| `public RenderFragment<PropertyGridEditContext>? EditorTemplate { get; set; }` | `[Parameter]`<br>Gets or sets the editor of custom properties and of types without a built-in editor. |
| `public EventCallback<PropertyGridChange> OnPropertyChanged { get; set; }` | `[Parameter]`<br>Gets or sets the callback invoked after the user changes a value. |
| `public IReadOnlyList<PropertyGridItem> Properties { get; }` | Gets the properties shown, before searching. |
| `protected override string ElementName { get; }` | Gets the NSS element name emitted by PropertyGrid. |

### `PropertyGridChange`

Namespace `Nazor.Controls`. [Source](../../Sources/Core/Nazor.Controls/Data/PropertyGrid.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed record PropertyGridChange : IEquatable<PropertyGridChange>` | A property value changed by the user in a `Nazor.Controls.PropertyGrid`.<br>`Item`. The changed property.<br>`OldValue`. The value before the change.<br>`NewValue`. The stored value. |
| `public PropertyGridChange(PropertyGridItem Item, object? OldValue, object? NewValue)` | A property value changed by the user in a `Nazor.Controls.PropertyGrid`.<br>`Item`. The changed property.<br>`OldValue`. The value before the change.<br>`NewValue`. The stored value. |
| `public override string ToString()` |  |
| `public static bool operator !=(PropertyGridChange? left, PropertyGridChange? right)` |  |
| `public static bool operator ==(PropertyGridChange? left, PropertyGridChange? right)` |  |
| `public override int GetHashCode()` |  |
| `public override bool Equals(object? obj)` |  |
| `public bool Equals(PropertyGridChange? other)` |  |
| `public void Deconstruct(out PropertyGridItem Item, out object? OldValue, out object? NewValue)` |  |
| `public PropertyGridItem Item { get; init; }` | The changed property. |
| `public object? OldValue { get; init; }` | The value before the change. |
| `public object? NewValue { get; init; }` | The stored value. |

### `PropertyGridEditContext`

Namespace `Nazor.Controls`. [Source](../../Sources/Core/Nazor.Controls/Data/PropertyGrid.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed class PropertyGridEditContext` | The property edited by a custom editor of a `Nazor.Controls.PropertyGrid`. |
| `public Task SetValueAsync(object? value)` | Stores a new value and reports the change.<br>`value`. The new value.<br>**Returns**. A task that completes when the change has been reported. |
| `public PropertyGridItem Item { get; }` | Gets the edited property. |
| `public object? Value { get; }` | Gets the current value. |
| `public string LabelId { get; }` | Gets the identifier of the property name, for the editor's `LabeledBy`. |

### `PropertyGridEditor`

Namespace `Nazor.Controls`. [Source](../../Sources/Core/Nazor.Controls/Data/PropertyGrid.cs)

| Declaration | Contract |
| --- | --- |
| `public enum PropertyGridEditor` | The editor a `Nazor.Controls.PropertyGrid` uses for a property. |
| `Auto = 0` | Chosen from the type: a check box for booleans, a choice for enumerations, a number editor for numbers, text otherwise. |
| `Text = 1` | A text field. |
| `Number = 2` | A number editor with parsing and stepping with Up and Down. |
| `Boolean = 3` | A check box. |
| `Choice = 4` | A drop-down list of `Nazor.Controls.PropertyGridItem.Choices` or of the values of an enumeration. |
| `ReadOnly = 5` | The value as text, without editing. |
| `Custom = 6` | The item's `Nazor.Controls.PropertyGridItem.EditorTemplate` or the grid's `Nazor.Controls.PropertyGrid.EditorTemplate`. |

### `PropertyGridItem`

Namespace `Nazor.Controls`. [Source](../../Sources/Core/Nazor.Controls/Data/PropertyGrid.cs)

`FromObject` carries `RequiresUnreferencedCode`: preserve the target's public properties and enum label attributes when choosing reflection. For trim/AOT publication, provide explicit `Items` and `ChoiceText` formatters instead.

| Declaration | Contract |
| --- | --- |
| `public sealed class PropertyGridItem` | A property shown by a `Nazor.Controls.PropertyGrid`: its name, category and description, how its value is read and written, and which editor changes it. `Nazor.Controls.PropertyGridItem.FromObject(System.Object)` describes the public properties of an object. |
| `public static IReadOnlyList<PropertyGridItem> FromObject(object target)` | Describes the public instance properties of an object that can be read and are browsable, with the `System.ComponentModel.DisplayNameAttribute`, `System.ComponentModel.DescriptionAttribute`, `System.ComponentModel.CategoryAttribute`, `System.ComponentModel.ReadOnlyAttribute` and `System.ComponentModel.BrowsableAttribute` of `System.ComponentModel` when present.<br>`target`. The object to inspect.<br>**Returns**. The properties, in declaration order. |
| `public PropertyGridItem()` |  |
| `public required string Name { get; init; }` | Gets the stable name of the property. |
| `public string? DisplayName { get; init; }` | Gets the name shown and read by assistive technologies; `Nazor.Controls.PropertyGridItem.Name` when null. |
| `public string? Category { get; init; }` | Gets the category the property is listed under; a localized "Misc" when null. |
| `public string? Description { get; init; }` | Gets the description shown in the description pane when the property is selected. |
| `public Type PropertyType { get; init; }` | Gets the type of the value, which chooses the automatic editor. |
| `public required Func<object?> Getter { get; init; }` | Gets the function that reads the current value. |
| `public Action<object?>? Setter { get; init; }` | Gets the function that stores a new value, or null for a read-only property. |
| `public bool ReadOnly { get; init; }` | Gets whether the property cannot be changed even with a setter. |
| `public PropertyGridEditor Editor { get; init; }` | Gets the editor of the property. |
| `public RenderFragment<PropertyGridEditContext>? EditorTemplate { get; init; }` | Gets the content that edits the property, used by the custom editor. |
| `public IReadOnlyList<object?>? Choices { get; init; }` | Gets the values offered by the choice editor; the values of the enumeration when null. |
| `public Func<object?, string>? ChoiceText { get; init; }` | Gets the choice label formatter; ordinary value text is used when null. Supply labels explicitly for trim-safe enum customization. |
| `public double? Minimum { get; init; }` | Gets the smallest value of a number, or null for the limit of its type. |
| `public double? Maximum { get; init; }` | Gets the largest value of a number, or null for the limit of its type. |
| `public double Step { get; init; }` | Gets the change made to a number by Up and Down. |
| `public string Text { get; }` | Gets the name shown. |
| `public bool IsReadOnly { get; }` | Gets whether the value cannot be changed. |

### `PropertyGridOrder`

Namespace `Nazor.Controls`. [Source](../../Sources/Core/Nazor.Controls/Data/PropertyGrid.cs)

| Declaration | Contract |
| --- | --- |
| `public enum PropertyGridOrder` | How a `Nazor.Controls.PropertyGrid` orders its properties, written as the `order` attribute. |
| `Categorized = 0` | Under collapsible category headers, alphabetically within each category. |
| `Alphabetical = 1` | In one alphabetical list. |

### `RadioButton<TValue>`

Namespace `Nazor.Controls`. [Source](../../Sources/Core/Nazor.Controls/Selection/RadioGroup.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed class RadioButton<TValue> : ContentControl, IDisposable` | One choice of a `Nazor.Controls.RadioGroup<TValue>`: checked when its `Nazor.Controls.RadioButton<TValue>.Value` is the group's value; a click, Space or an arrow key that reaches it checks it. The parts are `RadioButtonMark` with its `RadioButtonDot`, `RadioButtonLabel` and `RadioButtonDescription`; the element has the `radio` role, the `checked` state, `position-in-set` and `size-of-set`.<br>`TValue`. The type of the value, the same as the group's. |
| `protected override void OnParametersSet()` | Joins the enclosing group. |
| `protected override void AddAttributes(RenderTreeBuilder builder)` | Adds the role, the states, the Tab stop and the activation.<br>`builder`. The builder that receives the render frames. |
| `protected override void AddContent(RenderTreeBuilder builder)` | Adds the mark, the label and the description.<br>`builder`. The builder that receives the render frames. |
| `public void Dispose()` | Leaves the group. |
| `public RadioButton()` |  |
| `public TValue Value { get; set; }` | `[Parameter]` Initial value `default!`.<br>Gets or sets the value the group takes when this radio button is checked. |
| `public RenderFragment? Description { get; set; }` | `[Parameter]`<br>Gets or sets a secondary line shown below the label. |
| `public bool Disabled { get; set; }` | `[Parameter]`<br>Gets or sets whether this radio button rejects interaction; the arrow keys skip it. |
| `public RadioGroup<TValue>? Group { get; set; }` | `[CascadingParameter]`<br>Gets or sets the enclosing group, supplied automatically. |
| `public bool IsChecked { get; }` | Gets whether this radio button is checked. |
| `protected override string ElementName { get; }` | Gets the NSS element name emitted by RadioButton. |

### `RadioGroup<TValue>`

Namespace `Nazor.Controls`. [Source](../../Sources/Core/Nazor.Controls/Selection/RadioGroup.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed class RadioGroup<TValue> : ContentControl` | A set of mutually exclusive choices: `Nazor.Controls.RadioGroup<TValue>.Value` (`@bind-Value`) is the value of the checked `Nazor.Controls.RadioButton<TValue>`. The group is a single Tab stop, on the checked radio button or on the first enabled one; the arrow keys move to the next enabled radio button and check it. Inside a `Nazor.Controls.RadioGroup<TValue>.Field` the group is the field's control: the label names it and the field's required, invalid and disabled states apply. The element is `RadioGroup` with the `radiogroup` role and the `orientation` and `size` attributes.<br>`TValue`. The type of the values of the radio buttons. |
| `protected override void OnParametersSet()` | Validates the parameters and synchronizes the checked value. |
| `protected override void AddAttributes(RenderTreeBuilder builder)` | Adds the role, the layout attributes and the field relationships.<br>`builder`. The builder that receives the render frames. |
| `protected override void AddContent(RenderTreeBuilder builder)` | Supplies the group to its radio buttons.<br>`builder`. The builder that receives the render frames. |
| `protected override void OnAfterRender(bool firstRender)` | Refreshes the radio buttons after the set of radio buttons or the checked value changed.<br>`firstRender`. Whether this is the first render. |
| `public Task SelectAsync(TValue candidate)` | Checks the radio button with a value as a user action does and reports the change.<br>`candidate`. The value to check.<br>**Returns**. A task that completes when the change has been reported. |
| `public RadioGroup()` |  |
| `public TValue? Value { get; set; }` | `[Parameter]`<br>Gets or sets the value of the checked radio button; a value no radio button has checks none. |
| `public EventCallback<TValue> ValueChanged { get; set; }` | `[Parameter]`<br>Gets or sets the callback invoked when the user checks another radio button. |
| `public bool Disabled { get; set; }` | `[Parameter]`<br>Gets or sets whether every radio button of the group rejects interaction. |
| `public Orientation Orientation { get; set; }` | `[Parameter]` Initial value `Orientation.Vertical`.<br>Gets or sets the direction in which the radio buttons are laid out; vertical by default. |
| `public ControlSize Size { get; set; }` | `[Parameter]` Initial value `ControlSize.Medium`.<br>Gets or sets the size step of the radio buttons. |
| `public FieldContext? Field { get; set; }` | `[CascadingParameter]`<br>Gets or sets the enclosing field, supplied automatically. |
| `public TValue? SelectedValue { get; }` | Gets the value of the checked radio button. |
| `protected override string ElementName { get; }` | Gets the NSS element name emitted by RadioGroup. |

### `RangeSlider`

Namespace `Nazor.Controls`. [Source](../../Sources/Core/Nazor.Controls/Selection/Slider.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed class RangeSlider : Control` | Chooses a range between `Nazor.Controls.RangeSlider.Minimum` and `Nazor.Controls.RangeSlider.Maximum` with two thumbs, such as a price filter: `Nazor.Controls.RangeSlider.LowerValue` and `Nazor.Controls.RangeSlider.UpperValue` (`@bind-LowerValue`, `@bind-UpperValue`) stay at least `Nazor.Controls.RangeSlider.MinimumRange` apart. Each thumb is its own `slider` element (`RangeSliderHandle` with the `thumb` attribute `lower` or `upper`) with the keyboard of a `Nazor.Controls.Slider`; a press on the track moves the nearer thumb. The element `RangeSlider` is a `group`; the parts are `RangeSliderRail` holding `RangeSliderTrack` with `RangeSliderFill` and the two handles, each with its `RangeSliderThumb` (and `RangeSliderValueLabel`), and `RangeSliderTicks` with `RangeSliderTick` and `RangeSliderTickLabel`. The variables `--nz-range-lower` and `--nz-range-upper` are the values' places in the range, `--nz-range-start` and `--nz-range-end` the filled part of the track from its start, and each handle has `--nz-range-thumb-position`, the thumb's place within the handle; `oninput` and `onchange` come from the handle that changed. |
| `protected override void OnParametersSet()` | Validates the parameters and synchronizes the values. |
| `protected override void AddAttributes(RenderTreeBuilder builder)` | Adds the group role, the look attributes and the field relationships.<br>`builder`. The builder that receives the render frames. |
| `protected override void AddContent(RenderTreeBuilder builder)` | Adds the rail with the track and the two handles, and the ticks.<br>`builder`. The builder that receives the render frames. |
| `public RangeSlider()` |  |
| `public double LowerValue { get; set; }` | `[Parameter]`<br>Gets or sets the lower end of the range. |
| `public EventCallback<double> LowerValueChanged { get; set; }` | `[Parameter]`<br>Gets or sets the callback invoked while the user moves the lower thumb. |
| `public double UpperValue { get; set; }` | `[Parameter]` Initial value `100`.<br>Gets or sets the upper end of the range. |
| `public EventCallback<double> UpperValueChanged { get; set; }` | `[Parameter]`<br>Gets or sets the callback invoked while the user moves the upper thumb. |
| `public double Minimum { get; set; }` | `[Parameter]`<br>Gets or sets the lowest value. |
| `public double Maximum { get; set; }` | `[Parameter]` Initial value `100`.<br>Gets or sets the highest value. |
| `public double Step { get; set; }` | `[Parameter]` Initial value `1`.<br>Gets or sets the change made by an arrow key; zero allows any value. |
| `public double LargeStep { get; set; }` | `[Parameter]` Initial value `10`.<br>Gets or sets the change made by Page Up and Page Down. |
| `public double MinimumRange { get; set; }` | `[Parameter]`<br>Gets or sets the smallest distance kept between the two values. |
| `public Orientation Orientation { get; set; }` | `[Parameter]`<br>Gets or sets the direction of the track; a vertical range slider grows upwards. |
| `public bool IsDirectionReversed { get; set; }` | `[Parameter]`<br>Gets or sets whether the minimum is at the end of the track (right, or top when vertical). |
| `public double TickFrequency { get; set; }` | `[Parameter]`<br>Gets or sets the distance between tick marks, or zero for none. |
| `public bool ShowTickLabels { get; set; }` | `[Parameter]`<br>Gets or sets whether the tick marks show their values. |
| `public ValueLabelMode ValueLabel { get; set; }` | `[Parameter]`<br>Gets or sets when the values are shown next to the thumbs. |
| `public string? Format { get; set; }` | `[Parameter]`<br>Gets or sets how the values and the tick labels are written, as `Nazor.Controls.Slider.Format`. |
| `public string? LowerLabel { get; set; }` | `[Parameter]`<br>Gets or sets the accessible name of the lower thumb; null uses a localized "Minimum". |
| `public string? UpperLabel { get; set; }` | `[Parameter]`<br>Gets or sets the accessible name of the upper thumb; null uses a localized "Maximum". |
| `public ControlSize Size { get; set; }` | `[Parameter]` Initial value `ControlSize.Medium`.<br>Gets or sets the size step. |
| `public bool Disabled { get; set; }` | `[Parameter]`<br>Gets or sets whether the range slider rejects interaction. |
| `public FieldContext? Field { get; set; }` | `[CascadingParameter]`<br>Gets or sets the enclosing field, supplied automatically. |
| `public double CurrentLowerValue { get; }` | Gets the current lower value, kept within the range. |
| `public double CurrentUpperValue { get; }` | Gets the current upper value, kept within the range and not below the lower value. |
| `protected override string ElementName { get; }` | Gets the NSS element name emitted by RangeSlider. |
| `protected override string? ElementStyle { get; }` | Gets the range variables and the caller's declarations. |

### `Rating`

Namespace `Nazor.Controls`. [Source](../../Sources/Core/Nazor.Controls/Selection/Rating.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed class Rating : Control` | A score from zero to `Nazor.Controls.Rating.Maximum` stars: `Nazor.Controls.Rating.Value` (`@bind-Value`) changes with a click on a star (on its first half with `Nazor.Controls.Rating.AllowHalf`), or with the keyboard: the arrows move by one star (half a star with `Nazor.Controls.Rating.AllowHalf`), Home clears and End gives the maximum. Hovering previews the score. The element has the `slider` role, announced as a rating, with the range attributes and a `value-text` such as "3 of 5", also used as the accessible description. Each star is a `RatingItem` with the `fill` attribute (`full`, `half` or `empty`) and, while hovering, `preview`; it holds `RatingItemSymbol` (`Nazor.Controls.Rating.EmptySymbol`), `RatingItemFill` (`Nazor.Controls.Rating.FullSymbol`, clipped by themes for a half star) and two `RatingItemZone` parts (`half` = `start` or `end`) that receive the pointer. |
| `protected override void OnParametersSet()` | Validates the parameters and synchronizes the score. |
| `protected override void AddAttributes(RenderTreeBuilder builder)` | Adds the slider role, the range, the value text and the pointer handling.<br>`builder`. The builder that receives the render frames. |
| `protected override void AddContent(RenderTreeBuilder builder)` | Adds the stars.<br>`builder`. The builder that receives the render frames. |
| `public Task SetValueAsync(double score)` | Changes the score as a user action does and reports the change.<br>`score`. The new score, rounded to whole or half stars and kept within the range.<br>**Returns**. A task that completes when the change has been reported. |
| `public Rating()` |  |
| `public double Value { get; set; }` | `[Parameter]`<br>Gets or sets the score. |
| `public EventCallback<double> ValueChanged { get; set; }` | `[Parameter]`<br>Gets or sets the callback invoked when the user changes the score. |
| `public int Maximum { get; set; }` | `[Parameter]` Initial value `5`.<br>Gets or sets the number of stars. |
| `public bool AllowHalf { get; set; }` | `[Parameter]`<br>Gets or sets whether the score can end with half a star. |
| `public bool AllowClear { get; set; }` | `[Parameter]`<br>Gets or sets whether a click on the current score clears it. |
| `public bool ReadOnly { get; set; }` | `[Parameter]`<br>Gets or sets whether the score is only shown: it stays focusable and announced but cannot change. |
| `public bool Disabled { get; set; }` | `[Parameter]`<br>Gets or sets whether the rating rejects interaction. |
| `public string FullSymbol { get; set; }` | `[Parameter]` Initial value `"★"`.<br>Gets or sets the text of a full star. |
| `public string EmptySymbol { get; set; }` | `[Parameter]` Initial value `"☆"`.<br>Gets or sets the text of an empty star. |
| `public ControlSize Size { get; set; }` | `[Parameter]` Initial value `ControlSize.Medium`.<br>Gets or sets the size step. |
| `public FieldContext? Field { get; set; }` | `[CascadingParameter]`<br>Gets or sets the enclosing field, supplied automatically. |
| `public double CurrentValue { get; }` | Gets the current score, kept within the range and rounded to whole or half stars. |
| `public double? PreviewValue { get; }` | Gets the score previewed by the pointer, or null. |
| `protected override string ElementName { get; }` | Gets the NSS element name emitted by Rating. |

### `RowGroup`

Namespace `Nazor.Controls`. [Source](../../Sources/Core/Nazor.Controls/Collections/ActionRow.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed class RowGroup : ContentControl` | A titled group of `Nazor.Controls.ActionRow` rows, as a section of a settings page: a `Nazor.Controls.RowGroup.Title`, an optional `Nazor.Controls.RowGroup.Description` and `Nazor.Controls.RowGroup.HeaderActions` above a boxed list. The element `RowGroup` (role `group`, named by its title) holds `RowGroupHeader` (with `RowGroupHeading`, `RowGroupTitle`, `RowGroupDescription` and `RowGroupActions`) and `RowGroupList` (role `list`), whose rows receive the `position` attribute (only, first, middle, last) so themes can round the ends of the box. |
| `protected override void OnInitialized()` | Initializes the list's decorative hover tracker. |
| `protected override void AddAttributes(RenderTreeBuilder builder)` | Adds the group role and its name.<br>`builder`. The builder that receives the render frames. |
| `protected override void AddContent(RenderTreeBuilder builder)` | Adds the header and the list of rows.<br>`builder`. The builder that receives the render frames. |
| `protected override void OnAfterRender(bool firstRender)` | Tells each row its position in the list once the rows are laid out.<br>`firstRender`. Whether this is the first render. |
| `public RowGroup()` |  |
| `public string? Title { get; set; }` | `[Parameter]`<br>Gets or sets the title of the group. |
| `public string? Description { get; set; }` | `[Parameter]`<br>Gets or sets the text that explains the group, below the title. |
| `public RenderFragment? HeaderActions { get; set; }` | `[Parameter]`<br>Gets or sets controls shown at the end of the header, such as an add button. |
| `public bool Disabled { get; set; }` | `[Parameter]`<br>Gets or sets whether the group and its rows reject interaction. |
| `protected override string ElementName { get; }` | Gets the element name. |

### `ScrollOrientation`

Namespace `Nazor.Controls`. [Source](../../Sources/Core/Nazor.Controls/Layout/ScrollView.cs)

| Declaration | Contract |
| --- | --- |
| `public enum ScrollOrientation` | The directions in which a `Nazor.Controls.ScrollView` scrolls, written as the `orientation` attribute. |
| `Vertical = 0` | Up and down; the content wraps to the width of the view. |
| `Horizontal = 1` | Left and right, for content wider than the view. |
| `Both = 2` | Both directions. |

### `ScrollView`

Namespace `Nazor.Controls`. [Source](../../Sources/Core/Nazor.Controls/Layout/ScrollView.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed class ScrollView : ContentControl` | A region that scrolls its content in the directions of `Nazor.Controls.ScrollView.Orientation`, with the wheel, the scroll bars, touch and, when the focus or the pointer is inside, Page Up, Page Down, Home and End. After each scroll and each layout the element tells which edges still hide content with the `can-scroll-top`, `can-scroll-bottom`, `can-scroll-left` and `can-scroll-right` attributes, and `can-scroll-start` and `can-scroll-end` along its orientation, so themes can draw shadows or fades at those edges. `Nazor.Controls.ScrollView.OnScroll` reports changes to this view; `Nazor.Controls.ScrollView.ScrollToStartAsync`, `Nazor.Controls.ScrollView.ScrollToEndAsync`, `Nazor.Controls.ScrollView.ScrollToAsync(System.Nullable{System.Double},System.Nullable{System.Double})` and `Nazor.Controls.ScrollView.ScrollIntoView(Nazor.ElementReference)` move it from code. A named view is a group for assistive technologies. |
| `protected override void OnParametersSet()` | Validates the orientation. |
| `protected override void AddAttributes(RenderTreeBuilder builder)` | Adds the orientation, the edge attributes, the focus behavior and the scroll listener.<br>`builder`. The builder that receives the render frames. |
| `public Task<bool> ScrollToAsync(double? left, double? top)` | Scrolls to an offset within the limits; null keeps that axis.<br>`left`. The horizontal offset, or null.<br>`top`. The vertical offset, or null.<br>**Returns**. A task whose result is true when the view moved. |
| `public Task<bool> ScrollToStartAsync()` | Scrolls to the beginning along the orientation: the top, the left edge, or both.<br>**Returns**. A task whose result is true when the view moved. |
| `public Task<bool> ScrollToEndAsync()` | Scrolls to the end along the orientation: the bottom, the right edge, or both.<br>**Returns**. A task whose result is true when the view moved. |
| `public bool ScrollIntoView(ElementReference target)` | Scrolls this view, and any view around it, so that an element inside it becomes visible.<br>`target`. The element to show, such as the `Nazor.Controls.Control.Element` of a control.<br>**Returns**. true when a view moved. |
| `public ScrollView()` |  |
| `public ScrollOrientation Orientation { get; set; }` | `[Parameter]`<br>Gets or sets the directions in which the content scrolls. |
| `public bool IsFocusable { get; set; }` | `[Parameter]`<br>Gets or sets whether the view itself is a Tab stop, so that keyboard users can scroll content that has nothing focusable in it. |
| `public EventCallback<ScrollEventArgs> OnScroll { get; set; }` | `[Parameter]`<br>Gets or sets the callback invoked when this view's offset, viewport or content size changes. |
| `public double ScrollLeft { get; }` | Gets the horizontal offset, in logical units. |
| `public double ScrollTop { get; }` | Gets the vertical offset, in logical units. |
| `public double ViewportWidth { get; }` | Gets the width of the visible area, or zero before the first layout. |
| `public double ViewportHeight { get; }` | Gets the height of the visible area, or zero before the first layout. |
| `public double ContentWidth { get; }` | Gets the width of the content, or zero before the first layout. |
| `public double ContentHeight { get; }` | Gets the height of the content, or zero before the first layout. |
| `public bool CanScrollTop { get; }` | Gets whether content is hidden above the visible area. |
| `public bool CanScrollBottom { get; }` | Gets whether content is hidden below the visible area. |
| `public bool CanScrollLeft { get; }` | Gets whether content is hidden left of the visible area. |
| `public bool CanScrollRight { get; }` | Gets whether content is hidden right of the visible area. |
| `public bool CanScrollStart { get; }` | Gets whether content is hidden before the visible area along the orientation (above or left). |
| `public bool CanScrollEnd { get; }` | Gets whether content is hidden after the visible area along the orientation (below or right). |
| `protected override string ElementName { get; }` | Gets the NSS element name emitted by ScrollView. |

### `SearchIconPlacement`

Namespace `Nazor.Controls`. [Source](../../Sources/Core/Nazor.Controls/Fields/SearchField.cs)

| Declaration | Contract |
| --- | --- |
| `public enum SearchIconPlacement` | Position of the single search icon independently of the scope selector and clear action. |
| `ThemeDefault = 0` | Basic/NazorLeaf place it before the editor; Fluent after the clear action. |
| `Start = 1` | Before the editor, after any scope selector. |
| `End = 2` | After the editor and clear action. |

### `SearchField<TItem>`

Namespace `Nazor.Controls`. [Source](../../Sources/Core/Nazor.Controls/Fields/SearchField.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed class SearchField<TItem> : Control, IDisposable` | A search editor with typed, templated suggestions. Text edits, suggestion choices and explicit query submissions have distinct callbacks. Enter submits the current text, choosing the highlighted suggestion first when present; clicking a suggestion only chooses it. The query icon can also submit. Asynchronous providers are canceled when superseded, dismissed or disposed, and late results are ignored even when a provider ignores cancellation. Default suggestion text highlights matching passages; an empty list shows a localized no-results label.<br>`TItem`. The type of suggestions. |
| `public SearchField()` | Creates an instance with the documented default parameter values. |
| `protected override int? ElementTabIndex { get; }` | Leaves the normal Tab stop on the editor rather than the composite root. |
| `public string? Value { get; set; }` | `[Parameter]`<br>Gets or sets the search text. |
| `public EventCallback<string> ValueChanged { get; set; }` | `[Parameter]`<br>Gets or sets the callback invoked when an edit, clear or suggestion choice changes the text; supports `@bind-Value`. |
| `public EventCallback<string> OnSearch { get; set; }` | `[Parameter]`<br>Gets or sets the callback invoked with the text on explicit query submission, after `QuerySubmitted`. |
| `public EventCallback<string> TextEdited { get; set; }` | `[Parameter]`<br>Gets or sets the callback invoked for user text edits and clearing, independently of suggestion choices and external value assignments. |
| `public EventCallback<TItem> SuggestionChosen { get; set; }` | `[Parameter]`<br>Gets or sets the callback invoked when a suggestion is chosen; choosing alone does not submit a query. |
| `public EventCallback<SearchQuery<TItem>> QuerySubmitted { get; set; }` | `[Parameter]`<br>Gets or sets the callback invoked by Enter or the query action, including a suggestion chosen by that Enter. |
| `public string? Placeholder { get; set; }` | `[Parameter]`<br>Gets or sets the hint shown while the text is empty; "Search" by default. |
| `public SearchIconPlacement IconPlacement { get; set; }` | `[Parameter]`<br>Gets or sets the search icon position; the active theme chooses its position by default. |
| `public IReadOnlyList<TItem>? Suggestions { get; set; }` | `[Parameter]`<br>Gets or sets suggestions offered when they contain the text, ignoring case. |
| `public Func<string, IReadOnlyList<TItem>>? SuggestionProvider { get; set; }` | `[Parameter]`<br>Gets or sets a function returning the suggestions for a text; it takes precedence over `Suggestions`. |
| `public Func<string, CancellationToken, Task<IReadOnlyList<TItem>>>? AsyncSuggestionProvider { get; set; }` | `[Parameter]`<br>Gets or sets an asynchronous suggestion provider, taking precedence over synchronous providers and local suggestions. Providers should observe cancellation; stale results and errors are also rejected by request identity. |
| `public Func<TItem, string>? ItemText { get; set; }` | `[Parameter]`<br>Gets or sets the text copied from a chosen suggestion and used for default rendering and local matching; `ToString` is the default. |
| `public RenderFragment<TItem>? ItemTemplate { get; set; }` | `[Parameter]`<br>Gets or sets the presentation of each suggestion, without changing the text copied on selection. |
| `public bool HighlightMatches { get; set; }` | `[Parameter]`<br>Highlights matches of the trimmed query, ignoring case in the current culture. Default `true`; custom item templates can use `RenderHighlightedText`. |
| `public RenderFragment<string>? HighlightTemplate { get; set; }` | `[Parameter]`<br>Custom inline fragment for each matched passage, preserving its original text. Null uses `Mark`. Applies to default suggestion text and `RenderHighlightedText` when highlighting is enabled. |
| `public Func<TItem, string, bool>? Filter { get; set; }` | `[Parameter]`<br>Gets or sets local matching; by default a suggestion's text contains the trimmed query, ignoring case. Providers own their filtering. |
| `public int MaxSuggestions { get; set; }` | `[Parameter]`<br>Gets or sets the largest number of suggestions shown; 8 by default. Initial value `8`. |
| `public RenderFragment? EmptyContent { get; set; }` | `[Parameter]`<br>Gets or sets content shown in the list when no suggestion matches, overriding `EmptyText` and the localized default label. |
| `public string? EmptyText { get; set; }` | `[Parameter]`<br>Text shown when no suggestion matches. Null uses the localized "No results" label; `EmptyContent` takes precedence. |
| `public RenderFragment? LoadingContent { get; set; }` | `[Parameter]`<br>Gets or sets pending-request content; the localized loading label is the default. |
| `public RenderFragment? ErrorContent { get; set; }` | `[Parameter]`<br>Gets or sets failed-request content; the localized error label is the default. Details are available through `SuggestionError`. |
| `public bool IsQueryButtonEnabled { get; set; }` | `[Parameter]`<br>Gets or sets whether the search icon is an invokable query action. Enter can submit regardless of this setting. Initial value `true`. |
| `public IReadOnlyList<string>? Scopes { get; set; }` | `[Parameter]`<br>Gets or sets the scopes the search can be limited to; null or empty hides the scope choice. |
| `public string? Scope { get; set; }` | `[Parameter]`<br>Gets or sets the chosen scope. |
| `public EventCallback<string?> ScopeChanged { get; set; }` | `[Parameter]`<br>Gets or sets the callback invoked when the user chooses another scope; supports `@bind-Scope`. |
| `public bool Disabled { get; set; }` | `[Parameter]`<br>Gets or sets whether the field rejects interaction. |
| `public FieldAppearance Appearance { get; set; }` | `[Parameter]`<br>Gets or sets how the frame is drawn. |
| `public ControlSize Size { get; set; }` | `[Parameter]`<br>Gets or sets the size step. Initial value `ControlSize.Medium`. |
| `public bool AutoFocus { get; set; }` | `[Parameter]`<br>Gets or sets whether the editor takes the focus when it appears. |
| `public FieldContext? Field { get; set; }` | `[CascadingParameter]`<br>Gets or sets the enclosing field, supplied automatically. |
| `public string Text { get; }` | Gets the current text. |
| `public string? CurrentScope { get; }` | Gets the chosen scope. |
| `public bool HasSuggestions { get; }` | Gets whether the field offers suggestions. |
| `public bool IsLoading { get; private set; }` | Gets whether an asynchronous suggestion request is queued or pending. |
| `public Exception? SuggestionError { get; private set; }` | Gets the error of the latest accepted asynchronous request, or null after a new request or dismissal. |
| `public bool Opened { get; }` | Gets whether the suggestion popup is open. |
| `protected override string ElementName { get; }` | Gets the NSS element name emitted by SearchField. |
| `protected override void OnParametersSet()` | Validates the parameters and synchronizes the text and the scope. |
| `public IReadOnlyList<TItem> CurrentSuggestions()` | Gets the suggestions offered for the current text.<br>**Returns**. The suggestions, at most `MaxSuggestions`. |
| `public string TextOf(TItem item)` | Gets the text of a suggestion.<br>`item`. The suggestion.<br>**Returns**. The text displayed and copied on selection. |
| `public RenderFragment RenderHighlightedText(string value)` | Renders inline text with nonoverlapping matches of the current trimmed query highlighted. Use inside a text block or paragraph in a custom `ItemTemplate`. Matching ignores case in the current culture and preserves the original text. Empty or culture-ignorable queries add no highlights.<br>`value`: Original suggestion text; null is rejected.<br>**Returns**. An inline fragment using `HighlightTemplate` or `Mark`, or plain text when `HighlightMatches` is false. |
| `protected override void BuildRenderTree(RenderTreeBuilder builder)` | Renders the plain search box, or delegates to a combo box when there are suggestions.<br>`builder`. The builder that receives the render frames. |
| `protected override void OnAfterRender(bool firstRender)` | Exposes the combo box element as the control's element.<br>`firstRender`. Whether this is the first render. |
| `protected override Task OnAfterRenderAsync(bool firstRender)` | Starts queued suggestion requests after rendering, so editing and dismissal remain responsive while a provider is pending.<br>`firstRender`. Whether this is the first render.<br>**Returns**. A task that completes when the queued request is accepted, rejected or canceled. |
| `protected override void AddAttributes(RenderTreeBuilder builder)` | Adds the frame states of the plain search box.<br>`builder`. The builder that receives the render frames. |
| `protected override void AddContent(RenderTreeBuilder builder)` | Adds the parts of the plain search box.<br>`builder`. The builder that receives the render frames. |
| `public Task ClearAsync()` | Clears the text as the clear button does and reports it.<br>**Returns**. A task that completes when the change has been reported. |
| `public Task SearchAsync()` | Raises `OnSearch` with the current text, as Enter does.<br>**Returns**. A task that completes when the callback has finished. |
| `public Task FocusAsync(bool preventScroll = false)` | Moves the keyboard focus to the editor.<br>`preventScroll`. Whether focusing keeps the current scroll position.<br>**Returns**. A task that completes when the focus has moved. |
| `public Task RefreshSuggestionsAsync()` | Refreshes asynchronous suggestions for the current text without opening a dismissed popup. Synchronous and local sources are refreshed on rendering.<br>**Returns**. A task that completes when the request finishes; provider errors are exposed through `SuggestionError`. |
| `public void Dispose()` | Cancels pending requests and ignores all subsequent provider completions. |

`ValueChanged` reports text edits, clear actions and suggestion text fill; external `Value` assignments do not raise it. `TextEdited` reports user edits/clear only. Pointer selection raises `SuggestionChosen` without a query. Unmodified Enter chooses a highlighted suggestion first, then submits one query with `HasChosenSuggestion == true`; Enter without a highlight and the query action submit free text with that flag false. `QuerySubmitted` precedes the text-only `OnSearch` callback.

`SearchFieldMark` is a query button by default, with a localized accessible name and pointer/accessibility invocation. Pointer query actions preserve editor focus and leave application focus changes in effect. It is not another Tab stop; Enter in the editor provides keyboard submission. Set `IsQueryButtonEnabled` false for a decorative mark. `IconPlacement` and all existing scope/clear positioning remain theme-owned. Without suggestion sources, the editor has the `searchbox` role; with sources, it composes `Combobox<TItem>` and exposes combo/listbox semantics. Parts remain `SearchFieldLeading`, `SearchFieldScope`, sibling `SearchFieldMark`, `SearchFieldInput`, `SearchFieldTrailing`, `SearchFieldClear`, `SearchFieldPopup`, `SearchFieldOption` and `SearchFieldEmpty`.

Providers own their filtering; local `Suggestions` use `Filter` or text containment in the current culture. `MaxSuggestions` must be positive and caps all result sources. Asynchronous providers take precedence over synchronous providers and local lists. A new request clears earlier results/errors and shows `LoadingContent` or localized Loading; a failed current request exposes `SuggestionError` and `ErrorContent` or localized Error. `EmptyContent` controls the no-result presentation. Pending work starts after rendering in the observed component lifecycle, so editing returns promptly. Superseding requests, dismissal, focus leaving the field, disabled/external value or configuration changes, clear/submission and disposal cancel work; request identity rejects late results/errors even if a provider ignores cancellation. `RefreshSuggestionsAsync` retries without reopening a dismissed popup.

Breaking migration: the nongeneric `SearchField` type has been replaced by `SearchField<TItem>`. Use `<SearchField TItem="string" ... />` and `SearchField<string>` in C# references, `typeof` and render calls. Existing `OnSearch` handlers now handle explicit queries only; handle `SuggestionChosen` separately when a pointer choice should act immediately. When both callbacks perform the same action, inspect `QuerySubmitted.HasChosenSuggestion` to avoid repeating the choice action for Enter.

### `SearchQuery<TItem>`

Namespace `Nazor.Controls`. [Source](../../Sources/Core/Nazor.Controls/Fields/SearchQuery.cs)

| Declaration | Contract |
| --- | --- |
| `public readonly struct SearchQuery<TItem> : IEquatable<SearchQuery<TItem>>` | Immutable record value describing one explicit query submission. `TItem` is the suggestion type. |
| `public SearchQuery()` | Creates a default submission with null text, the default suggestion and a false choice flag. |
| `public SearchQuery(string Text, TItem? ChosenSuggestion, bool HasChosenSuggestion)` | Constructs a submission: `Text` is the submitted string; `ChosenSuggestion` is the item selected by this same Enter key or its default value; `HasChosenSuggestion` distinguishes a selected default-valued item from no choice. |
| `public string Text { get; init; }` | The submitted text. |
| `public TItem? ChosenSuggestion { get; init; }` | The suggestion chosen by this submission, or the default value when none was chosen. |
| `public bool HasChosenSuggestion { get; init; }` | Whether the submission chose a highlighted suggestion. |
| `public void Deconstruct(out string Text, out TItem? ChosenSuggestion, out bool HasChosenSuggestion)` | Extracts the same text, item and choice flag as the constructor/properties. |
| `public override string ToString()` | Returns the record's diagnostic string representation. |
| `public override int GetHashCode()` | Computes a hash from the record's fields. |
| `public override bool Equals(object obj)` | Tests equality with another submission record. |
| `public bool Equals(SearchQuery<TItem> other)` | Compares text, chosen item and choice flag using their default equality semantics. |
| `public static bool operator ==(SearchQuery<TItem> left, SearchQuery<TItem> right)` | Tests value equality. |
| `public static bool operator !=(SearchQuery<TItem> left, SearchQuery<TItem> right)` | Tests value inequality. |

### `Segment<TValue>`

Namespace `Nazor.Controls`. [Source](../../Sources/Core/Nazor.Controls/Selection/SegmentedControl.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed class Segment<TValue> : ContentControl, IDisposable` | One segment of a `Nazor.Controls.SegmentedControl<TValue>`: a `radio` in single selection or a `togglebutton` in multiple selection. The element is `Segment` with the `selected` attribute (and `checked` or `pressed`), `position` (first, middle, last) and `icon-only` when it has no label; the parts are `SegmentLeading` and `SegmentLabel`.<br>`TValue`. The type of the value, the same as the control's. |
| `protected override void OnParametersSet()` | Joins the enclosing control. |
| `protected override void AddAttributes(RenderTreeBuilder builder)` | Adds the role, the states, the position and the activation.<br>`builder`. The builder that receives the render frames. |
| `protected override void AddContent(RenderTreeBuilder builder)` | Adds the leading content and the label.<br>`builder`. The builder that receives the render frames. |
| `public void Dispose()` | Leaves the enclosing control. |
| `public Segment()` |  |
| `public TValue Value { get; set; }` | `[Parameter]` Initial value `default!`.<br>Gets or sets the value this segment stands for. |
| `public RenderFragment? Leading { get; set; }` | `[Parameter]`<br>Gets or sets content shown before the label, such as an icon; give icon-only segments an `Nazor.Controls.Control.AccessibleName`. |
| `public bool Disabled { get; set; }` | `[Parameter]`<br>Gets or sets whether this segment rejects interaction; the arrow keys skip it. |
| `public SegmentedControl<TValue>? Owner { get; set; }` | `[CascadingParameter]`<br>Gets or sets the enclosing control, supplied automatically. |
| `public bool IsSelected { get; }` | Gets whether this segment is selected or pressed. |
| `protected override string ElementName { get; }` | Gets the NSS element name emitted by Segment. |

### `SegmentedControl<TValue>`

Namespace `Nazor.Controls`. [Source](../../Sources/Core/Nazor.Controls/Selection/SegmentedControl.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed class SegmentedControl<TValue> : ContentControl` | A compact row of joined choices, such as Day, Week and Month above a calendar. With `Nazor.Controls.Primitives.SelectionMode.Single` it is a `radiogroup` of `radio` segments: `Nazor.Controls.SegmentedControl<TValue>.Value` (`@bind-Value`) is the selected segment's value, the group is a single Tab stop and the arrow keys select the next enabled segment. With `Nazor.Controls.Primitives.SelectionMode.Multiple` it is a `toolbar` of toggle segments: `Nazor.Controls.SegmentedControl<TValue>.Values` (`@bind-Values`) lists the pressed ones and the arrow keys move between them. A `SegmentedControlIndicator` part follows the selected segment (the `--nz-indicator-*` variables), so themes can slide a highlight; `Nazor.Controls.SegmentedControl<TValue>.EqualWidth` gives every segment the same width (the `equal-width` attribute).<br>`TValue`. The type of the segment values. |
| `protected override void OnParametersSet()` | Validates the parameters and synchronizes the selection. |
| `protected override void AddAttributes(RenderTreeBuilder builder)` | Adds the role and the look attributes.<br>`builder`. The builder that receives the render frames. |
| `protected override void AddContent(RenderTreeBuilder builder)` | Adds the segments and the indicator.<br>`builder`. The builder that receives the render frames. |
| `protected override void OnAfterRender(bool firstRender)` | Refreshes the segments after the set of segments or the selection changed.<br>`firstRender`. Whether this is the first render. |
| `public Task ActivateAsync(TValue candidate)` | Activates the segment with a value as a user action does: selects it in single selection, presses or releases it in multiple selection, and reports the change.<br>`candidate`. The value of the segment.<br>**Returns**. A task that completes when the change has been reported. |
| `public SegmentedControl()` |  |
| `public SelectionMode SelectionMode { get; set; }` | `[Parameter]` Initial value `SelectionMode.Single`.<br>Gets or sets whether one segment (`Nazor.Controls.Primitives.SelectionMode.Single`) or any number of segments (`Nazor.Controls.Primitives.SelectionMode.Multiple`) can be selected. |
| `public TValue? Value { get; set; }` | `[Parameter]`<br>Gets or sets the value of the selected segment in single selection. |
| `public EventCallback<TValue> ValueChanged { get; set; }` | `[Parameter]`<br>Gets or sets the callback invoked when the user selects another segment in single selection. |
| `public IReadOnlyList<TValue>? Values { get; set; }` | `[Parameter]`<br>Gets or sets the values of the pressed segments in multiple selection. |
| `public EventCallback<IReadOnlyList<TValue>> ValuesChanged { get; set; }` | `[Parameter]`<br>Gets or sets the callback invoked with the new values when the user presses or releases a segment in multiple selection; the list follows the order of the segments. |
| `public bool EqualWidth { get; set; }` | `[Parameter]`<br>Gets or sets whether every segment takes the same width. |
| `public ControlSize Size { get; set; }` | `[Parameter]` Initial value `ControlSize.Medium`.<br>Gets or sets the size step. |
| `public bool Disabled { get; set; }` | `[Parameter]`<br>Gets or sets whether every segment rejects interaction. |
| `public TValue? SelectedValue { get; }` | Gets the value of the selected segment in single selection. |
| `public IReadOnlyList<TValue> SelectedValues { get; }` | Gets the values of the pressed segments in multiple selection, in segment order. |
| `protected override string ElementName { get; }` | Gets the NSS element name emitted by SegmentedControl. |

### `SegmentedDatePicker`

Namespace `Nazor.Controls`. [Source](../../Sources/Core/Nazor.Controls/Pickers/SegmentedDatePicker.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed class SegmentedDatePicker : SegmentedPickerBase<DateOnly>` | Selects a nullable Gregorian date through localized day/month/year entry segments and draft selector columns. Segment order follows the culture's short-date pattern; hidden segments keep the committed date's components, or use the bounded `Today` fallback while initially empty. Changing month/year clamps the day to the new month length and all draft changes stay within `Minimum` and `Maximum`. Confirm commits; Cancel, Escape and outside dismissal leave the original value unchanged. Non-Gregorian calendars are not supported. The popup is `SegmentedDatePickerPopup`; shared parts are `SegmentedPickerSegments`, `SegmentedPickerSegment`, `SegmentedPickerColumns`, `SegmentedPickerColumn`, `SegmentedPickerOption`, `SegmentedPickerActions`, `SegmentedPickerConfirm`, `SegmentedPickerCancel` and their semantic-hidden 16px `SegmentedPickerActionIcon`. |
| `public SegmentedDatePicker()` | Creates an instance with the documented default parameter values. |
| `public DateOnly? Minimum { get; set; }` | `[Parameter]`<br>Gets or sets the earliest allowed date, or null for the Gregorian minimum. |
| `public DateOnly? Maximum { get; set; }` | `[Parameter]`<br>Gets or sets the latest allowed date, or null for the Gregorian maximum. |
| `public DateOnly? Today { get; set; }` | `[Parameter]`<br>Gets or sets the date used to initialize an empty picker; null uses the computer's date. |
| `public bool DayVisible { get; set; }` | `[Parameter]`<br>Gets or sets whether the day segment and column are displayed. Initial value `true`. |
| `public bool MonthVisible { get; set; }` | `[Parameter]`<br>Gets or sets whether the month segment and column are displayed. Initial value `true`. |
| `public bool YearVisible { get; set; }` | `[Parameter]`<br>Gets or sets whether the year segment and column are displayed. Initial value `true`. |
| `public string DayFormat { get; set; }` | `[Parameter]`<br>Gets or sets the custom .NET day display pattern, `dd` by default. Initial value `"dd"`. |
| `public string MonthFormat { get; set; }` | `[Parameter]`<br>Gets or sets the custom .NET month display pattern, `MMMM` by default. Initial value `"MMMM"`. |
| `public string YearFormat { get; set; }` | `[Parameter]`<br>Gets or sets the custom .NET year display pattern, `yyyy` by default. Initial value `"yyyy"`. |
| `protected override string ElementName { get; }` | Gets the NSS element name emitted by SegmentedDatePicker. |
| `protected override string PickerLabel { get; }` | Gets the localized popup name. |
| `protected override IReadOnlyList<string> SegmentNames { get; }` | Gets visible date parts in culture order. |
| `protected override object ConfigurationKey { get; }` | Gets the value-equality configuration key used to cancel stale drafts. |
| `protected override void ValidateConfiguration()` | Validates date bounds, part visibility, formats and the supported calendar. |
| `protected override DateOnly CreateInitialDraft()` | Creates the bounded initial date draft.<br>**Returns**. The committed date, or today's fallback, clamped to the date bounds. |
| `protected override bool IsAllowed(DateOnly candidate)` | Tests the configured inclusive date bounds.<br>`candidate`. The date to inspect.<br>**Returns**. true when it is within bounds. |
| `protected override void RenderSegments(RenderTreeBuilder builder)` | Renders date entry segments with placeholders while unset.<br>`builder`. The builder receiving the segments. |
| `protected override void RenderColumns(RenderTreeBuilder builder)` | Renders bounded Gregorian date columns; year ranges are indexed lazily.<br>`builder`. The builder receiving the columns. |

### `SegmentedPickerBase<TValue>`

Namespace `Nazor.Controls`. [Source](../../Sources/Core/Nazor.Controls/Pickers/SegmentedPickerBase.cs)

| Declaration | Contract |
| --- | --- |
| `public abstract class SegmentedPickerBase<TValue> : Control where TValue : struct` | Shared segmented picker frame and draft-selection lifecycle. Opening snapshots the current value into a draft; column interaction updates only the draft, Confirm commits once, and Cancel, Escape or outside dismissal discard it. External value/configuration changes cancel an open draft. The field has one entry Tab stop and its popup uses bounded nine-row selector columns with keyboard, wheel and primary-pointer drag input.<br>`TValue`. The date or time value selected by the picker. |
| `protected SegmentedPickerBase()` | Creates an instance with the documented default parameter values. |
| `public TValue? Value { get; set; }` | `[Parameter]`<br>Gets or sets the committed value; null displays segment placeholders. |
| `public EventCallback<TValue?> ValueChanged { get; set; }` | `[Parameter]`<br>Gets or sets the callback invoked when confirmation changes the committed value. |
| `public CultureInfo? Culture { get; set; }` | `[Parameter]`<br>Gets or sets the display culture; null uses the current culture. |
| `public bool Disabled { get; set; }` | `[Parameter]`<br>Gets or sets whether the control rejects interaction. |
| `public bool ReadOnly { get; set; }` | `[Parameter]`<br>Gets or sets whether the value can be read and focused but not changed. |
| `public bool Required { get; set; }` | `[Parameter]`<br>Gets or sets whether a value is required; validation remains application-owned. |
| `public bool Invalid { get; set; }` | `[Parameter]`<br>Gets or sets the application-owned invalid state. |
| `public bool IsOpen { get; set; }` | `[Parameter]`<br>Gets or sets whether the popup is open; supports `@bind-IsOpen`. |
| `public EventCallback<bool> IsOpenChanged { get; set; }` | `[Parameter]`<br>Gets or sets the callback invoked when the popup opens, confirms or cancels. |
| `public FieldAppearance Appearance { get; set; }` | `[Parameter]`<br>Gets or sets the field frame appearance. |
| `public ControlSize Size { get; set; }` | `[Parameter]`<br>Gets or sets the control size. Initial value `ControlSize.Medium`. |
| `public FieldContext? Field { get; set; }` | `[CascadingParameter]`<br>Gets or sets the enclosing field, supplied automatically. |
| `public TValue? CurrentValue { get; }` | Gets the current committed value. |
| `public TValue? DraftValue { get; }` | Gets the draft while the popup is open, or null while closed. |
| `public bool Opened { get; }` | Gets whether the popup is currently open. |
| `protected TValue Draft { get; }` | Gets the draft for rendering and editing its columns. |
| `protected CultureInfo EffectiveCulture { get; }` | Gets the culture in use. |
| `protected bool IsInactive { get; }` | Gets whether the control is disabled, including through its enclosing field. |
| `protected abstract string PickerLabel { get; }` | Gets the localized accessible name of the picker popup. |
| `protected abstract IReadOnlyList<string> SegmentNames { get; }` | Gets the names of the visible segments in display order. |
| `protected abstract object ConfigurationKey { get; }` | Gets a value-equality key for configuration that invalidates an open draft. |
| `protected abstract void ValidateConfiguration()` | Validates the concrete picker configuration. |
| `protected abstract TValue CreateInitialDraft()` | Creates a valid initial draft from the committed value or a deterministic fallback.<br>**Returns**. The initial draft. |
| `protected abstract bool IsAllowed(TValue candidate)` | Tests whether a value can be committed.<br>`candidate`. The value to inspect.<br>**Returns**. true when it is allowed. |
| `protected abstract void RenderSegments(RenderTreeBuilder builder)` | Renders the entry segments.<br>`builder`. The builder receiving the segment frames. |
| `protected abstract void RenderColumns(RenderTreeBuilder builder)` | Renders the draft's selector columns.<br>`builder`. The builder receiving the column frames. |
| `protected override void OnParametersSet()` | Validates and synchronizes value/configuration, canceling stale draft edits. |
| `protected override Task OnParametersSetAsync()` | Reports cancellation caused by external value or configuration changes.<br>**Returns**. A task completed when the open-state callback has finished. |
| `protected override void AddAttributes(RenderTreeBuilder builder)` | Adds combo-box semantics, field state and entry keyboard input.<br>`builder`. The builder receiving the attributes. |
| `protected override void AddContent(RenderTreeBuilder builder)` | Renders entry segments and the draft popup with Confirm and Cancel actions.<br>`builder`. The builder receiving the content. |
| `protected override void OnAfterRender(bool firstRender)` | Tracks the active selected row so the popup can align it with the entry after its retained parts are available.<br>`firstRender`. Whether this is the first render. |
| `protected void Segment(RenderTreeBuilder builder, int sequence, string name, string label, string? text)` | Renders a clickable entry segment; only the root participates in normal Tab traversal.<br>`builder`. The builder receiving the segment.<br>`sequence`. The starting frame sequence.<br>`name`. The segment's stable name.<br>`label`. Its localized name/placeholder.<br>`text`. The committed display text, or null for the placeholder. |
| `protected void Column(RenderTreeBuilder builder, int sequence, string name, string label, int selected, IReadOnlyList<int> values, Func<int, string> format, Func<int, TValue> apply)` | Renders one bounded selector column bound to the draft.<br>`builder`. The builder receiving the column.<br>`sequence`. The starting frame sequence.<br>`name`. The segment name.<br>`label`. The localized column name.<br>`selected`. The draft's selected integer.<br>`values`. The ascending available values.<br>`format`. The value display formatter.<br>`apply`. Converts a chosen value into the next draft. |
| `protected void SetDraft(TValue next)` | Changes only the open draft when it is allowed.<br>`next`. The next draft value. |
| `public Task OpenAsync()` | Opens a draft popup unless disabled or read-only.<br>**Returns**. A task completed after the open-state callback. |
| `public Task CancelAsync()` | Discards the draft and closes the popup.<br>**Returns**. A task completed after the open-state callback. |
| `public Task ConfirmAsync()` | Commits an allowed draft once, closes the popup and reports the value change.<br>**Returns**. A task completed after open/value callbacks and input/change events. |

### `SegmentedTimePicker`

Namespace `Nazor.Controls`. [Source](../../Sources/Core/Nazor.Controls/Pickers/SegmentedTimePicker.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed class SegmentedTimePicker : SegmentedPickerBase<TimeOnly>` | Selects a nullable time through localized hour/minute and optional second/period segments. The culture determines 12/24-hour display unless `Use24Hour` overrides it. Column edits change a draft until Confirm; Cancel, Escape and outside dismissal discard the draft. Hidden seconds and an existing off-increment minute are preserved. All drafts remain within the configured inclusive time bounds; ranges crossing midnight are not supported. The popup is `SegmentedTimePickerPopup` and it shares the segmented-picker parts with `SegmentedDatePicker`. |
| `public SegmentedTimePicker()` | Creates an instance with the documented default parameter values. |
| `public bool? Use24Hour { get; set; }` | `[Parameter]`<br>Gets or sets whether the display uses a 24-hour clock; null follows the culture. |
| `public int MinuteIncrement { get; set; }` | `[Parameter]`<br>Gets or sets the minute grid offered in the popup, from 1 to 60; an existing off-grid minute remains selectable. Initial value `1`. |
| `public bool ShowSeconds { get; set; }` | `[Parameter]`<br>Gets or sets whether seconds are displayed and edited; hidden seconds retain their committed value. |
| `public TimeOnly? Minimum { get; set; }` | `[Parameter]`<br>Gets or sets the earliest allowed time, or null for midnight. |
| `public TimeOnly? Maximum { get; set; }` | `[Parameter]`<br>Gets or sets the latest allowed time, or null for the end of the day. |
| `public bool Is24Hour { get; }` | Gets whether the active display uses a 24-hour clock. |
| `protected override string ElementName { get; }` | Gets the NSS element name emitted by SegmentedTimePicker. |
| `protected override string PickerLabel { get; }` | Gets the localized popup name. |
| `protected override IReadOnlyList<string> SegmentNames { get; }` | Gets visible time parts in culture order. |
| `protected override object ConfigurationKey { get; }` | Gets the value-equality configuration key used to cancel stale drafts. |
| `protected override void ValidateConfiguration()` | Validates clock increments and inclusive time bounds. |
| `protected override TimeOnly CreateInitialDraft()` | Creates an initial draft from the committed time or the clock rounded down to the minute increment.<br>**Returns**. The bounded draft time. |
| `protected override bool IsAllowed(TimeOnly candidate)` | Tests the configured inclusive time bounds.<br>`candidate`. The time to inspect.<br>**Returns**. true when it is within bounds. |
| `protected override void RenderSegments(RenderTreeBuilder builder)` | Renders time entry segments with placeholders while unset.<br>`builder`. The builder receiving the segments. |
| `protected override void RenderColumns(RenderTreeBuilder builder)` | Renders bounded time columns without changing the committed value.<br>`builder`. The builder receiving the columns. |

Both controls inherit the nullable committed value, open state and draft lifecycle from `SegmentedPickerBase<TValue>`. Entry segments are not free-text editors. A single entry Tab stop and accessibility Expand/Collapse action open or cancel the dialog; columns have listbox/option semantics, and selected rows are kept in a bounded nine-row window. Up/Down, Page Up/Down and Home/End adjust a column; Left/Right move between columns, Enter confirms, and Escape, F4/Alt+Up, Cancel or outside dismissal discard edits. Wheel input and captured primary-pointer dragging step through rows. These are bounded selectors, without kinetic scrolling or unverified native touch classification.

Date segmentation uses the Gregorian calendar, culture date order and custom .NET part formats; all-hidden fields and non-Gregorian calendars are rejected. Time supports culture/default or explicit 12/24-hour clocks, optional seconds and minute increments 1..60; bounds cannot cross midnight. Existing off-increment minutes and hidden components/fractional ticks are preserved unless bound clamping requires a change. Programmatic values remain application-owned; opening creates a bounded draft, and only confirmation writes it back. Required and invalid states are application-owned.

The entry roots and popup surfaces are `SegmentedDatePicker`/`SegmentedDatePickerPopup` and `SegmentedTimePicker`/`SegmentedTimePickerPopup`. Shared parts are `SegmentedPickerSegments`, `SegmentedPickerSegment` (`segment`, `current`, `placeholder`), `SegmentedPickerColumns`, `SegmentedPickerColumn` (`column`), `SegmentedPickerOption` (`current`, `selected`, position/count), `SegmentedPickerSpacer`, `SegmentedPickerActions`, `SegmentedPickerConfirm`, `SegmentedPickerCancel` and their semantic-hidden 16px `SegmentedPickerActionIcon`.

### `Select<TValue>`

Namespace `Nazor.Controls`. [Source](../../Sources/Core/Nazor.Controls/Fields/Select.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed class Select<TValue> : Control` | A choice of one value from a list that opens above or below the box. `Nazor.Controls.Select<TValue>.Value` (`@bind-Value`) is the chosen value. The options are the `Nazor.Controls.SelectItem<TValue>` written inside, or `Nazor.Controls.Select<TValue>.Items` shown with `Nazor.Controls.Select<TValue>.ItemText` (`ToString` by default) or `Nazor.Controls.Select<TValue>.ItemTemplate`. A click, Space, Enter, Alt+Down or F4 open the list on the chosen value; there the arrows move, Enter or a click choose and close, Escape or a click outside close without choosing. Arrow navigation stops at the ends and scrolls the focused option into view. With the list closed the arrows, Home, End and typing change the value directly. The parts are `SelectLeading`, `SelectValue` (with `placeholder` when empty), `SelectChevron`, `SelectPopup`, `SelectGroupHeader`, `SelectOption` (`selected` for the chosen one) and its `SelectOptionCheck` mark.<br>`TValue`. The type of the values. |
| `protected override void OnParametersSet()` | Synchronizes the value and the open state. |
| `protected override void AddAttributes(RenderTreeBuilder builder)` | Adds the combo box role, the states and the keyboard handling.<br>`builder`. The builder that receives the render frames. |
| `protected override void AddContent(RenderTreeBuilder builder)` | Adds the box parts, the list and the declared items.<br>`builder`. The builder that receives the render frames. |
| `public Task OpenAsync()` | Opens the list unless disabled.<br>**Returns**. A task that completes when the change has been reported. |
| `public Task CloseAsync()` | Closes the list without changing the value.<br>**Returns**. A task that completes when the change has been reported. |
| `public Task ToggleAsync()` | Opens the list when closed and closes it when open.<br>**Returns**. A task that completes when the change has been reported. |
| `public Task SelectAsync(TValue candidate)` | Chooses a value as the user would, closes the list and reports the change.<br>`candidate`. The value to choose; it must be an enabled option.<br>**Returns**. A task that completes when the change has been reported. |
| `public Select()` |  |
| `public TValue? Value { get; set; }` | `[Parameter]`<br>Gets or sets the chosen value. |
| `public EventCallback<TValue> ValueChanged { get; set; }` | `[Parameter]`<br>Gets or sets the callback invoked when the user chooses another value. |
| `public IEnumerable<TValue>? Items { get; set; }` | `[Parameter]`<br>Gets or sets the values offered, when there are no declared items. |
| `public Func<TValue, string>? ItemText { get; set; }` | `[Parameter]`<br>Gets or sets the text of a value. |
| `public RenderFragment<TValue>? ItemTemplate { get; set; }` | `[Parameter]`<br>Gets or sets how a value is rendered in the list and in the box. |
| `public string? Placeholder { get; set; }` | `[Parameter]`<br>Gets or sets the text shown when no value is chosen. |
| `public bool Disabled { get; set; }` | `[Parameter]`<br>Gets or sets whether the control rejects interaction. |
| `public bool IsOpen { get; set; }` | `[Parameter]`<br>Gets or sets whether the list is open. |
| `public EventCallback<bool> IsOpenChanged { get; set; }` | `[Parameter]`<br>Gets or sets the callback invoked when the list opens or closes. |
| `public DropDownPlacement DropDownPlacement { get; set; }` | `[Parameter]`<br>Gets or sets the preferred position of the list relative to the field. |
| `public int? PreferredMinVisibleItems { get; set; }` | `[Parameter]`<br>Gets or sets the preferred minimum number of visible options, or null for no minimum preference. |
| `public int PreferredMaxVisibleItems { get; set; }` | `[Parameter]` Initial value `10`.<br>Gets or sets the preferred maximum number of visible options. The default is 10. |
| `public RenderFragment? Leading { get; set; }` | `[Parameter]`<br>Gets or sets content shown in the box before the value, such as an `Nazor.Controls.Icon`. |
| `public FieldAppearance Appearance { get; set; }` | `[Parameter]`<br>Gets or sets how the box is drawn. |
| `public ControlSize Size { get; set; }` | `[Parameter]` Initial value `ControlSize.Medium`.<br>Gets or sets the size step. |
| `public RenderFragment? ChildContent { get; set; }` | `[Parameter]`<br>Gets or sets the declared `Nazor.Controls.SelectItem<TValue>` options. |
| `public FieldContext? Field { get; set; }` | `[CascadingParameter]`<br>Gets or sets the enclosing field, supplied automatically. |
| `public TValue? SelectedValue { get; }` | Gets the chosen value. |
| `public bool Opened { get; }` | Gets whether the list is open. |
| `protected override string ElementName { get; }` | Gets the NSS element name emitted by Select. |

### `SelectItem<TValue>`

Namespace `Nazor.Controls`. [Source](../../Sources/Core/Nazor.Controls/Fields/Select.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed class SelectItem<TValue> : ComponentBase, IDisposable` | An option of a `Nazor.Controls.Select<TValue>`, with its content (the text of the value by default). Options with the same `Nazor.Controls.SelectItem<TValue>.Group` are listed under a header with that title.<br>`TValue`. The type of the values. |
| `protected override void OnParametersSet()` | Joins the enclosing select and refreshes it when the option changes. |
| `protected override void BuildRenderTree(RenderTreeBuilder builder)` | Renders nothing; the select renders the option.<br>`builder`. The builder that receives the render frames. |
| `public void Dispose()` | Leaves the enclosing select. |
| `public SelectItem()` |  |
| `public TValue Value { get; set; }` | `[Parameter]` Initial value `default!`.<br>Gets or sets the value chosen by this option. |
| `public bool Disabled { get; set; }` | `[Parameter]`<br>Gets or sets whether the option cannot be chosen. |
| `public string? Text { get; set; }` | `[Parameter]`<br>Gets or sets the text used for typing and, without content, shown. |
| `public string? Group { get; set; }` | `[Parameter]`<br>Gets or sets the title of the group the option belongs to. |
| `public RenderFragment? ChildContent { get; set; }` | `[Parameter]`<br>Gets or sets the content of the option. |
| `public Select<TValue>? Owner { get; set; }` | `[CascadingParameter]`<br>Gets or sets the enclosing select, supplied automatically. |

### `Separator`

Namespace `Nazor.Controls`. [Source](../../Sources/Core/Nazor.Controls/Layout/Container.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed class Separator : ContentControl` | A horizontal or vertical division between groups of content, menu items or toolbar commands, optionally with a label in the middle of the line. |
| `protected override void OnParametersSet()` | Validates the orientation. |
| `protected override void AddAttributes(RenderTreeBuilder builder)` | Adds the separator role and its orientation.<br>`builder`. The builder that receives the render frames. |
| `protected override void AddContent(RenderTreeBuilder builder)` | Adds the optional label shown in the middle of the line.<br>`builder`. The builder that receives the render frames. |
| `public Separator()` |  |
| `public Orientation Orientation { get; set; }` | `[Parameter]`<br>Gets or sets the direction of the line; a vertical separator divides content laid out in a row. |
| `protected override string ElementName { get; }` | Gets the NSS element name emitted by Separator. |

### `Severity`

Namespace `Nazor.Controls`. [Source](../../Sources/Core/Nazor.Controls/Vocabulary.cs)

| Declaration | Contract |
| --- | --- |
| `public enum Severity` | The importance of a message, written as the `severity` attribute. |
| `Info = 0` | Neutral information. |
| `Success = 1` | A successful outcome. |
| `Warning = 2` | A condition that needs attention. |
| `Error = 3` | An error or a failure. |

### `Shape`

Namespace `Nazor.Controls`. [Source](../../Sources/Core/Nazor.Controls/Vocabulary.cs)

| Declaration | Contract |
| --- | --- |
| `public enum Shape` | The outline of a control, written as the `shape` attribute. |
| `Default = 0` | The theme's usual outline. |
| `Pill = 1` | Fully rounded ends. |
| `Circle = 2` | A circle, for controls that show only an icon. |
| `Square = 3` | A square, for controls that show only an icon. |

### `ShortcutField`

Namespace `Nazor.Controls`. [Source](../../Sources/Core/Nazor.Controls/Fields/ShortcutField.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed class ShortcutField : Control` | Records a keyboard shortcut. When the recorder receives the focus (or is clicked, or activated with Enter or Space after a cancel) it starts recording and shows "Press a key combination": the next key that is not a modifier, with the modifiers held, becomes `Nazor.Controls.ShortcutField.Value` (`@bind-Value`) and recording stops. While recording, the held modifiers are shown as they are pressed, Escape cancels, and Tab or Shift+Tab without other modifiers still move the focus so the keyboard is never trapped; every other key is captured and kept from shortcuts. With `Nazor.Controls.ShortcutField.RequireModifier` a key without Control, Alt or Meta (other than F1–F24) is ignored. The parts are `ShortcutFieldRecorder` (the focusable button, `pressed` while recording), its `ShortcutFieldValue` holding a `Nazor.Controls.ShortcutLabel` or a `ShortcutFieldPrompt`, and `ShortcutFieldClear`. Delete or Backspace on the recorder when it is not recording clear the value. |
| `protected override void OnParametersSet()` | Validates the parameters and synchronizes the shortcut. |
| `public static bool IsModifierKey(string key)` | Gets whether a key name is a modifier key.<br>`key`. The key name.<br>**Returns**. true for Shift, Control, Alt, Meta and the lock keys. |
| `protected override void AddAttributes(RenderTreeBuilder builder)` | Adds the frame states.<br>`builder`. The builder that receives the render frames. |
| `protected override void AddContent(RenderTreeBuilder builder)` | Adds the recorder and the clear button.<br>`builder`. The builder that receives the render frames. |
| `public void StartRecording()` | Starts waiting for a key, as a click or the focus do. |
| `public void CancelRecording()` | Stops waiting for a key without changing the shortcut, as Escape does. |
| `public Task<bool> RecordAsync(string key, KeyModifiers modifiers)` | Records a key event as the recorder would: modifiers alone and rejected keys keep it recording.<br>`key`. The key name, as in `Nazor.Input.KeyboardEventArgs.Key`.<br>`modifiers`. The modifiers held.<br>**Returns**. true if a shortcut was recorded. |
| `public Task ClearAsync()` | Removes the shortcut as the clear button does.<br>**Returns**. A task that completes when the change has been reported. |
| `public Task FocusAsync(bool preventScroll = false)` | Moves the keyboard focus to the recorder, which starts recording.<br>`preventScroll`. Whether focusing keeps the current scroll position.<br>**Returns**. A task that completes when the focus has moved. |
| `public ShortcutField()` |  |
| `public KeyGesture? Value { get; set; }` | `[Parameter]`<br>Gets or sets the recorded shortcut; null when none. |
| `public EventCallback<KeyGesture?> ValueChanged { get; set; }` | `[Parameter]`<br>Gets or sets the callback invoked when the user records or clears a shortcut; supports `@bind-Value`. |
| `public bool RequireModifier { get; set; }` | `[Parameter]`<br>Gets or sets whether a shortcut must include Control, Alt or Meta, unless its key is a function key. |
| `public string? Placeholder { get; set; }` | `[Parameter]`<br>Gets or sets the text shown when there is no shortcut and the field is not recording; "None" by default. |
| `public bool Clearable { get; set; }` | `[Parameter]` Initial value `true`.<br>Gets or sets whether a button clears the shortcut; true by default. |
| `public bool ReadOnly { get; set; }` | `[Parameter]`<br>Gets or sets whether the shortcut cannot be changed. |
| `public bool Disabled { get; set; }` | `[Parameter]`<br>Gets or sets whether the field rejects interaction. |
| `public bool Required { get; set; }` | `[Parameter]`<br>Gets or sets whether a shortcut is required; validation is application-owned. |
| `public bool Invalid { get; set; }` | `[Parameter]`<br>Gets or sets the externally determined invalid state. |
| `public FieldAppearance Appearance { get; set; }` | `[Parameter]`<br>Gets or sets how the frame is drawn. |
| `public ControlSize Size { get; set; }` | `[Parameter]` Initial value `ControlSize.Medium`.<br>Gets or sets the size step. |
| `public FieldContext? Field { get; set; }` | `[CascadingParameter]`<br>Gets or sets the enclosing field, supplied automatically. |
| `public KeyGesture? CurrentValue { get; }` | Gets the recorded shortcut. |
| `public bool IsRecording { get; }` | Gets whether the field is waiting for a key. |
| `protected override string ElementName { get; }` | Gets the NSS element name emitted by ShortcutField. |

### `ShortcutLabel`

Namespace `Nazor.Controls`. [Source](../../Sources/Core/Nazor.Controls/Text/ShortcutLabel.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed class ShortcutLabel : Control` | Shows a keyboard shortcut in the notation of the current platform: "Ctrl+Shift+S" on Windows and Linux, "⇧⌘S" on macOS. Each key is a `ShortcutLabelKey` part, joined by `ShortcutLabelSeparator` parts where the platform writes a plus sign. Assistive technologies read the whole shortcut as one text. |
| `protected override void OnParametersSet()` | Validates the gesture. |
| `protected override void AddAttributes(RenderTreeBuilder builder)` | Adds the text role and the accessible text of the shortcut.<br>`builder`. The builder that receives the render frames. |
| `protected override void AddContent(RenderTreeBuilder builder)` | Adds one part per key.<br>`builder`. The builder that receives the render frames. |
| `public ShortcutLabel()` |  |
| `public KeyGesture Gesture { get; set; }` | `[Parameter]` `[EditorRequired]`<br>Gets or sets the shortcut to show. |
| `protected override string ElementName { get; }` | Gets the NSS element name emitted by ShortcutLabel. |

### `Skeleton`

Namespace `Nazor.Controls`. [Source](../../Sources/Core/Nazor.Controls/Feedback/Skeleton.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed class Skeleton : Control` | A placeholder in the shape of content that is still loading, such as text lines, an avatar or a picture. It is hidden from assistive technologies (announce the loading elsewhere, for example with a `Nazor.Controls.Primitives.LoadingBoundary`) and static in Basic. A text skeleton renders `Nazor.Controls.Skeleton.Lines``SkeletonLine` parts, the last with the `last` attribute. `Nazor.Controls.Skeleton.Width` and `Nazor.Controls.Skeleton.Height` are NSS lengths, such as "120px" or "40%". |
| `protected override void OnParametersSet()` | Validates the shape and the number of lines. |
| `protected override void AddAttributes(RenderTreeBuilder builder)` | Adds the shape and hides the placeholder from assistive technologies.<br>`builder`. The builder that receives the render frames. |
| `protected override void AddContent(RenderTreeBuilder builder)` | Adds the lines of a text placeholder.<br>`builder`. The builder that receives the render frames. |
| `public Skeleton()` |  |
| `public SkeletonShape Shape { get; set; }` | `[Parameter]`<br>Gets or sets the outline of the placeholder. |
| `public int Lines { get; set; }` | `[Parameter]` Initial value `1`.<br>Gets or sets the number of lines of a text placeholder; 1 by default. |
| `public string? Width { get; set; }` | `[Parameter]`<br>Gets or sets the width as an NSS length, or null for the theme's width. |
| `public string? Height { get; set; }` | `[Parameter]`<br>Gets or sets the height as an NSS length, or null for the theme's height. |
| `protected override string ElementName { get; }` | Gets the NSS element name emitted by Skeleton. |
| `protected override string? ElementStyle { get; }` | Gets the size declarations and the caller's declarations. |

### `SkeletonShape`

Namespace `Nazor.Controls`. [Source](../../Sources/Core/Nazor.Controls/Feedback/Skeleton.cs)

| Declaration | Contract |
| --- | --- |
| `public enum SkeletonShape` | The outline of a `Nazor.Controls.Skeleton` placeholder, written as the `shape` attribute. |
| `Text = 0` | Lines of text (`text`); the last of several lines is shorter. |
| `Circle = 1` | A circle, for an avatar or an icon (`circle`). |
| `Rectangle = 2` | A rectangle, for an image, a card or a control (`rectangle`). |

### `Slider`

Namespace `Nazor.Controls`. [Source](../../Sources/Core/Nazor.Controls/Selection/Slider.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed class Slider : Control` | Chooses a number between `Nazor.Controls.Slider.Minimum` and `Nazor.Controls.Slider.Maximum` by dragging a thumb along a track, clicking the track, or with the keyboard: the arrows move by `Nazor.Controls.Slider.Step`, Page Up and Page Down by `Nazor.Controls.Slider.LargeStep`, Home and End to the ends. `Nazor.Controls.Slider.Value` (`@bind-Value`) and `oninput` follow the value while it changes, `onchange` comes when the user lets go. The element has the `slider` role with the range attributes; the parts are `SliderTrack` with `SliderFill`, `SliderThumb` with `SliderValueLabel` (`Nazor.Controls.Slider.ValueLabel`), and `SliderTicks` with `SliderTick` and `SliderTickLabel`. The variable `--nz-slider-fraction` (0 to 1) is the value's place in the range and `--nz-slider-position` the thumb's place from the start of the track (left, or bottom when vertical), which differs when `Nazor.Controls.Slider.IsDirectionReversed`; each tick has `--nz-slider-tick-position`. |
| `protected override void OnParametersSet()` | Validates the parameters and synchronizes the value. |
| `protected override void AddAttributes(RenderTreeBuilder builder)` | Adds the role, the range and the field relationships.<br>`builder`. The builder that receives the render frames. |
| `protected override void AddContent(RenderTreeBuilder builder)` | Adds the track, the ticks and the thumb.<br>`builder`. The builder that receives the render frames. |
| `public Slider()` |  |
| `public double Value { get; set; }` | `[Parameter]`<br>Gets or sets the value. |
| `public EventCallback<double> ValueChanged { get; set; }` | `[Parameter]`<br>Gets or sets the callback invoked while the user changes the value. |
| `public double Minimum { get; set; }` | `[Parameter]`<br>Gets or sets the lowest value. |
| `public double Maximum { get; set; }` | `[Parameter]` Initial value `100`.<br>Gets or sets the highest value. |
| `public double Step { get; set; }` | `[Parameter]` Initial value `1`.<br>Gets or sets the change made by an arrow key; values are multiples of it from the minimum, or any value when zero. |
| `public double LargeStep { get; set; }` | `[Parameter]` Initial value `10`.<br>Gets or sets the change made by Page Up and Page Down. |
| `public Orientation Orientation { get; set; }` | `[Parameter]`<br>Gets or sets the direction of the track; a vertical slider grows upwards. |
| `public bool IsDirectionReversed { get; set; }` | `[Parameter]`<br>Gets or sets whether the minimum is at the end of the track (right, or top when vertical). |
| `public double TickFrequency { get; set; }` | `[Parameter]`<br>Gets or sets the distance between tick marks, or zero for none. |
| `public bool ShowTickLabels { get; set; }` | `[Parameter]`<br>Gets or sets whether the tick marks show their values. |
| `public ValueLabelMode ValueLabel { get; set; }` | `[Parameter]`<br>Gets or sets when the value is shown next to the thumb. |
| `public string? Format { get; set; }` | `[Parameter]`<br>Gets or sets how the value and the tick labels are written: a numeric format such as `0.0` or `P0`, or a composite format such as `{0} °C`; null writes up to two decimals. |
| `public ControlSize Size { get; set; }` | `[Parameter]` Initial value `ControlSize.Medium`.<br>Gets or sets the size step. |
| `public bool Disabled { get; set; }` | `[Parameter]`<br>Gets or sets whether the slider rejects interaction. |
| `public FieldContext? Field { get; set; }` | `[CascadingParameter]`<br>Gets or sets the enclosing field, supplied automatically. |
| `public double CurrentValue { get; }` | Gets the current value, kept within the range. |
| `protected override string ElementName { get; }` | Gets the NSS element name emitted by Slider. |
| `protected override string? ElementStyle { get; }` | Gets the fraction variables and the caller's declarations. |

### `SnapLayoutArea`

Namespace `Nazor.Controls`. [Source](../../Sources/Core/Nazor.Controls/Frame/TitleBar.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed class SnapLayoutArea : ContentControl` | Marks its area as the maximize button for the native Snap Layouts of Windows (the `snap-layout-area` attribute): resting the pointer on it shows the snap layouts. It has no commands or pointer handlers: the button inside it must still maximize the window, and on other platforms it is an ordinary container. `Nazor.Controls.CaptionButtons` marks its maximize button already. |
| `protected override void AddAttributes(RenderTreeBuilder builder)` | Adds the snap layout attribute.<br>`builder`. The builder that receives the render frames. |
| `public SnapLayoutArea()` |  |
| `protected override string ElementName { get; }` | Gets the NSS element name emitted by SnapLayoutArea. |

### `SpinButtonPlacement`

Namespace `Nazor.Controls`. [Source](../../Sources/Core/Nazor.Controls/Fields/NumberField.cs)

| Declaration | Contract |
| --- | --- |
| `public enum SpinButtonPlacement` | Where a `Nazor.Controls.NumberField` places its increment and decrement buttons, written as the `spin-buttons` attribute so that a theme can follow its platform. |
| `Compact = 0` | Basic/NazorLeaf render an in-field stacked pair. Fluent shows an anchored step-button flyout while the editor is focused, with an explicit toggle and Escape/outside dismissal. |
| `Inline = 1` | Basic/NazorLeaf place decrement before the editor and increment after it. Fluent places both buttons adjacent at the trailing edge. |
| `Hidden = 2` | No buttons; the keyboard and the wheel still step. |

### `Spinner`

Namespace `Nazor.Controls`. [Source](../../Sources/Core/Nazor.Controls/Feedback/Spinner.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed class Spinner : Control` | A ring-shaped progress indicator for work in progress, drawn with the engine's `ring` glyph. `Nazor.Controls.Spinner.Value` between `Nazor.Controls.Spinner.Minimum` and `Nazor.Controls.Spinner.Maximum` fills that fraction of the ring; null means the amount is unknown (the `indeterminate` attribute), using the theme's loading presentation. Basic keeps a quarter ring without motion. The parts are `SpinnerIndicator`, which stacks `SpinnerTrack` (the full ring behind) and `SpinnerRing` (the arc, with `ring-fraction`), and `SpinnerLabel`. A theme can display `SpinnerMark` with its four `SpinnerPetal` parts and the determinate percentage in `SpinnerValue`. `SpinnerCheck` is a styleable vector completion mark. `SpinnerIndicator` uses NumberPresenter; --nz-spinner-fraction is the actual target and --nz-presented-number is the displayed sample. Full-circle glyphs use NSS trims, with accessibility reporting the actual target immediately. Without a `Nazor.Controls.Spinner.Label` it is named "Loading". |
| `protected override void OnParametersSet()` | Validates the range and the look. |
| `protected override void AddAttributes(RenderTreeBuilder builder)` | Adds the progressbar role, actual range, look and empty/complete attributes independently of visual interpolation.<br>`builder`. The builder that receives the render frames. |
| `protected override void AddContent(RenderTreeBuilder builder)` | Adds the track, the ring and the label.<br>`builder`. The builder that receives the render frames. |
| `public Spinner()` |  |
| `public double? Value { get; set; }` | `[Parameter]`<br>Gets or sets the completed work, or null when it is unknown. |
| `public double Minimum { get; set; }` | `[Parameter]`<br>Gets or sets the value of no progress; 0 by default. |
| `public double Maximum { get; set; }` | `[Parameter]` Initial value `100`.<br>Gets or sets the value of complete progress; 100 by default. |
| `public ControlSize Size { get; set; }` | `[Parameter]` Initial value `ControlSize.Medium`.<br>Gets or sets the size step of the ring. |
| `public string? Label { get; set; }` | `[Parameter]`<br>Gets or sets the visible label shown next to the ring, which also names the spinner. |
| `public Intent Intent { get; set; }` | `[Parameter]` Initial value `Intent.Accent`.<br>Gets or sets the meaning of the ring's color. |
| `public bool IsIndeterminate { get; }` | Gets whether the amount of completed work is unknown. |
| `public double RingFraction { get; }` | Gets the fraction of the ring drawn: the completed fraction, or a quarter while indeterminate. |
| `protected override string ElementName { get; }` | Gets the NSS element name emitted by Spinner. |
| `protected override string? ElementStyle { get; }` | Gets the target fraction variable and caller declarations. |

### `SplitButton`

Namespace `Nazor.Controls`. [Source](../../Sources/Core/Nazor.Controls/Actions/SplitButton.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed class SplitButton : ContentControl, IMenuOwner, IButtonGroupMember, IDisposable` | A button in two parts, such as Paste ▾: the primary part (`SplitButtonPrimary`, with `SplitButtonLeading` and `SplitButtonLabel`) runs the main action, a `SplitButtonDivider` separates it from the trigger part (`SplitButtonTrigger` with a `SplitButtonChevron`) that opens `Nazor.Controls.SplitButton.Menu` with related commands. It is a single Tab stop with the `splitbutton` role: Enter and Space run the main action, Alt+ArrowDown and F4 open the menu. With `Nazor.Controls.SplitButton.IsToggle` the main action also flips `Nazor.Controls.SplitButton.IsPressed` (`@bind-IsPressed`), for a split button that keeps a state, such as a text color. The element carries `appearance`, `intent`, `size`, `expanded` while the menu is open, `pressed` and, inside a `Nazor.Controls.ButtonGroup`, `position`. |
| `protected override void OnParametersSet()` | Validates the parameters, synchronizes the states and follows the command. |
| `protected override void OnAfterRender(bool firstRender)` | Lets the enclosing group place the button once it is rendered.<br>`firstRender`. Whether this is the first render. |
| `protected override void AddAttributes(RenderTreeBuilder builder)` | Adds the role, the look, the states, the main action and the opening keys.<br>`builder`. The builder that receives the render frames. |
| `protected override void AddContent(RenderTreeBuilder builder)` | Adds the primary part, the divider, the trigger part and the menu.<br>`builder`. The builder that receives the render frames. |
| `public Task OpenAsync()` | Opens the menu.<br>**Returns**. A task that completes when the change has been reported. |
| `public Task CloseAsync()` | Closes the menu.<br>**Returns**. A task that completes when the change has been reported. |
| `public void Dispose()` | Stops following the command and leaves the group. |
| `public SplitButton()` |  |
| `public ControlAppearance Appearance { get; set; }` | `[Parameter]`<br>Gets or sets how strongly the button is filled. |
| `public Intent Intent { get; set; }` | `[Parameter]`<br>Gets or sets the meaning of the button's color. |
| `public ControlSize Size { get; set; }` | `[Parameter]` Initial value `ControlSize.Medium`.<br>Gets or sets the size step. |
| `public RenderFragment? Leading { get; set; }` | `[Parameter]`<br>Gets or sets content shown before the label in the primary part, usually an `Nazor.Controls.Icon`. |
| `public RenderFragment? Menu { get; set; }` | `[Parameter]`<br>Gets or sets the items of the menu opened by the trigger part. |
| `public EventCallback<PointerEventArgs> OnClick { get; set; }` | `[Parameter]`<br>Gets or sets the callback invoked when the primary part is activated. |
| `public Command? Command { get; set; }` | `[Parameter]`<br>Gets or sets a command run by the primary part; it supplies the label when there is no content, the tooltip and the enabled state. |
| `public bool Disabled { get; set; }` | `[Parameter]`<br>Gets or sets whether the button rejects interaction. |
| `public bool IsToggle { get; set; }` | `[Parameter]`<br>Gets or sets whether the primary part keeps a pressed state. |
| `public bool IsPressed { get; set; }` | `[Parameter]`<br>Gets or sets whether the primary part is pressed, when `Nazor.Controls.SplitButton.IsToggle` is true. |
| `public EventCallback<bool> IsPressedChanged { get; set; }` | `[Parameter]`<br>Gets or sets the callback invoked when the user changes the pressed state. |
| `public bool IsOpen { get; set; }` | `[Parameter]`<br>Gets or sets whether the menu is open. |
| `public EventCallback<bool> IsOpenChanged { get; set; }` | `[Parameter]`<br>Gets or sets the callback invoked when the menu opens or closes. |
| `public OverlayPlacement Placement { get; set; }` | `[Parameter]`<br>Gets or sets the preferred side of the button on which the menu opens. |
| `public OverlayAlignment Alignment { get; set; }` | `[Parameter]`<br>Gets or sets how the menu lines up with the button. |
| `public string? TriggerLabel { get; set; }` | `[Parameter]`<br>Gets or sets the tooltip of the trigger part; "More options" in the language of the interface by default. |
| `public string? MenuClass { get; set; }` | `[Parameter]`<br>Gets or sets the space-separated NSS classes of the menu surface. |
| `public bool Opened { get; }` | Gets whether the menu is open. |
| `public bool Pressed { get; }` | Gets whether the primary part is pressed. |
| `protected override string ElementName { get; }` | Gets the NSS element name emitted by SplitButton. |

### `SplitView`

Namespace `Nazor.Controls`. [Source](../../Sources/Core/Nazor.Controls/Layout/SplitView.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed class SplitView : ContentControl` | A pane beside the main content, such as a navigation pane or a list next to its details. The pane (`Nazor.Controls.SplitView.Pane`) is `Nazor.Controls.SplitView.PaneSize` wide (`@bind-PaneSize`), or high in a vertical split view; in the `Nazor.Controls.SplitViewDisplayMode.Inline` mode a `Nazor.Controls.Primitives.ResizeHandle` between pane and content resizes it by dragging or with the arrow keys (Home and End jump to `Nazor.Controls.SplitView.MinPaneSize` and `Nazor.Controls.SplitView.MaxPaneSize`). `Nazor.Controls.SplitView.IsPaneOpen` (`@bind-IsPaneOpen`) opens and closes the pane, and `Nazor.Controls.SplitView.DisplayMode` chooses whether the open pane pushes or covers the content. The parts are `SplitViewPane` (with the `compact` attribute while showing the narrow strip), `ResizeHandle`, `SplitViewContent` and, while an overlaid pane is open, `SplitViewScrim`. The element has the `orientation`, `placement`, `display-mode` and `pane-open` attributes. |
| `protected override void OnParametersSet()` | Validates the parameters and synchronizes the size and the open state. |
| `protected override void AddAttributes(RenderTreeBuilder builder)` | Adds the layout attributes and the Escape handling of a covering pane.<br>`builder`. The builder that receives the render frames. |
| `protected override void AddContent(RenderTreeBuilder builder)` | Adds the pane, the resize handle and the content in visual order.<br>`builder`. The builder that receives the render frames. |
| `public Task SetPaneOpenAsync(bool value)` | Opens or closes the pane as the user does and reports the change.<br>`value`. Whether the pane should be open.<br>**Returns**. A task that completes when the change has been reported. |
| `public Task TogglePaneAsync()` | Opens the pane when closed and closes it when open.<br>**Returns**. A task that completes when the change has been reported. |
| `public SplitView()` |  |
| `public RenderFragment? Pane { get; set; }` | `[Parameter]`<br>Gets or sets the content of the pane. |
| `public SplitViewPanePlacement PanePlacement { get; set; }` | `[Parameter]`<br>Gets or sets the side that holds the pane. |
| `public Orientation Orientation { get; set; }` | `[Parameter]`<br>Gets or sets whether pane and content sit side by side (horizontal) or one above the other (vertical). |
| `public SplitViewDisplayMode DisplayMode { get; set; }` | `[Parameter]`<br>Gets or sets how the pane shares the space with the content. |
| `public double PaneSize { get; set; }` | `[Parameter]` Initial value `280`.<br>Gets or sets the size of the open pane: its width, or its height in a vertical split view. |
| `public EventCallback<double> PaneSizeChanged { get; set; }` | `[Parameter]`<br>Gets or sets the callback invoked when the user resizes the pane. |
| `public double MinPaneSize { get; set; }` | `[Parameter]` Initial value `120`.<br>Gets or sets the smallest size the user can give the pane. |
| `public double MaxPaneSize { get; set; }` | `[Parameter]` Initial value `600`.<br>Gets or sets the largest size the user can give the pane. |
| `public double CompactPaneSize { get; set; }` | `[Parameter]` Initial value `48`.<br>Gets or sets the size of the closed pane in the compact mode. |
| `public bool IsPaneOpen { get; set; }` | `[Parameter]` Initial value `true`.<br>Gets or sets whether the pane is open; true by default. |
| `public EventCallback<bool> IsPaneOpenChanged { get; set; }` | `[Parameter]`<br>Gets or sets the callback invoked when the pane closes by a click on the content or Escape. |
| `public bool IsResizable { get; set; }` | `[Parameter]` Initial value `true`.<br>Gets or sets whether an inline pane can be resized; true by default. |
| `public double KeyboardStep { get; set; }` | `[Parameter]` Initial value `10`.<br>Gets or sets the change made by one arrow key on the resize handle. |
| `public string? PaneAccessibleName { get; set; }` | `[Parameter]`<br>Gets or sets the name of the pane for assistive technologies, which makes it a group. |
| `public string? ResizeHandleAccessibleName { get; set; }` | `[Parameter]`<br>Gets or sets the name of the resize handle for assistive technologies; a localized default otherwise. |
| `public double CurrentPaneSize { get; }` | Gets the current size of the open pane. |
| `public bool PaneOpen { get; }` | Gets whether the pane is currently open. |
| `public bool IsCompact { get; }` | Gets whether the pane currently shows the narrow strip of the compact mode. |
| `protected override string ElementName { get; }` | Gets the NSS element name emitted by SplitView. |

### `SplitViewDisplayMode`

Namespace `Nazor.Controls`. [Source](../../Sources/Core/Nazor.Controls/Layout/SplitView.cs)

| Declaration | Contract |
| --- | --- |
| `public enum SplitViewDisplayMode` | How the pane of a `Nazor.Controls.SplitView` shares the space with the content, written as the `display-mode` attribute. |
| `Inline = 0` | The open pane sits beside the content, which shrinks; a handle between them resizes it. |
| `Overlay = 1` | The open pane covers the content; a click on the content or Escape closes it. The closed pane takes no space. |
| `Compact = 2` | The closed pane stays visible as a narrow strip of `Nazor.Controls.SplitView.CompactPaneSize`, for icons; the open pane covers the content at its full size, and a click on the content or Escape closes it. |

### `SplitViewPanePlacement`

Namespace `Nazor.Controls`. [Source](../../Sources/Core/Nazor.Controls/Layout/SplitView.cs)

| Declaration | Contract |
| --- | --- |
| `public enum SplitViewPanePlacement` | Which side of a `Nazor.Controls.SplitView` holds the pane, written as the `placement` attribute. |
| `Start = 0` | The left side, or the top in a vertical split view. |
| `End = 1` | The right side, or the bottom in a vertical split view. |

### `StatusBar`

Namespace `Nazor.Controls`. [Source](../../Sources/Core/Nazor.Controls/Layout/StatusBar.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed class StatusBar : ContentControl` | The strip at the bottom of a window that shows the state of the application in `Nazor.Controls.StatusBarSection` children, such as the cursor position, the encoding or the progress of a task. It is a group named "Status bar" unless `Nazor.Controls.Control.AccessibleName` says otherwise; with `Nazor.Controls.StatusBar.Live` it becomes a status region whose changes assistive technologies announce. `Nazor.Controls.StatusBar.ShowSizeGrip` adds a decorative `StatusBarSizeGrip` part at the end. The element has the `live` attribute when live. |
| `protected override void OnParametersSet()` | Validates the politeness. |
| `protected override void AddAttributes(RenderTreeBuilder builder)` | Adds the group or status role, the default name and the live politeness.<br>`builder`. The builder that receives the render frames. |
| `protected override void AddContent(RenderTreeBuilder builder)` | Adds the sections and the size grip.<br>`builder`. The builder that receives the render frames. |
| `public StatusBar()` |  |
| `public LivePoliteness? Live { get; set; }` | `[Parameter]`<br>Gets or sets how changes are announced, or null for a bar that is not announced. |
| `public bool ShowSizeGrip { get; set; }` | `[Parameter]`<br>Gets or sets whether a size grip is drawn at the end of the bar, as a reminder that the window resizes. |
| `protected override string ElementName { get; }` | Gets the NSS element name emitted by StatusBar. |

### `StatusBarSection`

Namespace `Nazor.Controls`. [Source](../../Sources/Core/Nazor.Controls/Layout/StatusBar.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed class StatusBarSection : ContentControl` | A section of a `Nazor.Controls.StatusBar`: `Nazor.Controls.StatusBarSection.Width` wide, as wide as its content, or sharing the free space with the other flexible sections (`Nazor.Controls.StatusBarSection.IsFlexible`). Text inside it is read as text; a named section (`Nazor.Controls.Control.AccessibleName`) is a group for content such as buttons. The element has the `flexible` and `align` attributes. |
| `protected override void OnParametersSet()` | Validates the width and the alignment. |
| `protected override void AddAttributes(RenderTreeBuilder builder)` | Adds the role, the flexibility and the alignment.<br>`builder`. The builder that receives the render frames. |
| `public StatusBarSection()` |  |
| `public double? Width { get; set; }` | `[Parameter]`<br>Gets or sets the fixed width, or null to fit the content or share the free space. |
| `public bool IsFlexible { get; set; }` | `[Parameter]`<br>Gets or sets whether the section grows to share the free space of the bar. |
| `public StatusBarSectionAlignment Alignment { get; set; }` | `[Parameter]`<br>Gets or sets the alignment of the content inside the section. |
| `protected override string ElementName { get; }` | Gets the NSS element name emitted by StatusBarSection. |
| `protected override string? ElementStyle { get; }` | Gets the fixed width and the caller's declarations. |

### `StatusBarSectionAlignment`

Namespace `Nazor.Controls`. [Source](../../Sources/Core/Nazor.Controls/Layout/StatusBar.cs)

| Declaration | Contract |
| --- | --- |
| `public enum StatusBarSectionAlignment` | The alignment of the content of a `Nazor.Controls.StatusBarSection`, written as the `align` attribute. |
| `Start = 0` | At the start (left). |
| `Center = 1` | In the middle. |
| `End = 2` | At the end (right). |

### `StyleSheets`

Namespace `Nazor.Controls`. [Source](../../Sources/Core/Nazor.Controls/StyleSheets.cs)

| Declaration | Contract |
| --- | --- |
| `public static class StyleSheets` | The stylesheets compiled from the themes of `Nazor.Controls`. |
| `public static StyleSheet None { get; }` | An empty theme for applications that supply every control style themselves. |
| `public static StyleSheet Default { get; }` | The theme used when an application selects none; currently `Nazor.Controls.StyleSheets.Basic`. |
| `public static StyleSheet Basic { get; }` | Gets the stylesheet compiled from `Basic.nss`. |

### `Submenu`

Namespace `Nazor.Controls`. [Source](../../Sources/Core/Nazor.Controls/Menus/Menu.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed class Submenu : MenuItemBase` | A menu item that opens a nested `Nazor.Controls.Menu`: a click, the right arrow or resting the pointer on it opens the nested menu, the left arrow or Escape closes it and returns to the item. The item shows a `MenuItemSubmenuArrow` part; `Nazor.Controls.Submenu.Content` holds the nested items. |
| `public TimeProvider Time { get; set; }` | `[CascadingParameter]` Timer provider for pointer hover, default TimeProvider.System. A null value is rejected during parameter application. Keyboard/explicit opening is immediate. Changing providers cancels pending work. |
| `protected override void OnDispose()` | Cancels the pending hover timer when the submenu is removed. |
| `protected override void OnParametersSet()` | Validates the slots. |
| `protected override void AddItemAttributes(RenderTreeBuilder builder)` | Adds the expanded state and the opening behavior.<br>`builder`. The builder that receives the render frames. |
| `protected override void AddTrailing(RenderTreeBuilder builder)` | Adds the arrow and the nested menu.<br>`builder`. The builder that receives the render frames. |
| `protected override Task RunAsync(PointerEventArgs e)` | Opens the nested menu instead of running a command.<br>`e`. The activation event.<br>**Returns**. A task that completes when the menu is open. |
| `public Submenu()` |  |
| `public RenderFragment? Content { get; set; }` | `[Parameter]` `[EditorRequired]`<br>Gets or sets the nested items. |
| `public bool Opened { get; }` | Gets whether the nested menu is open. |
| `protected override string ElementName { get; }` | Gets the NSS element name emitted by Submenu. |
| `protected override string Role { get; }` | Gets the menu item role. |

### `Switch`

Namespace `Nazor.Controls`. [Source](../../Sources/Core/Nazor.Controls/Selection/Switch.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed class Switch : ContentControl, IDisposable` | An on/off setting that applies at once, such as Wi-Fi in a settings page: `IsOn` (`@bind-IsOn`) flips with a click or Space. Dragging the thumb previews its position and commits the nearest end on release; Escape or losing capture cancels the preview. The element has the `switch` role, is `:checked` when on and has the `size` and `label-position` attributes. The parts are `SwitchTrack` holding `SwitchOnLabel` (`OnContent`), `SwitchThumbRail` containing `SwitchThumbPosition` and `SwitchThumb`, and `SwitchOffLabel` (`OffContent`), then `SwitchLabel` and `SwitchDescription`. |
| `public Switch()` | Creates an instance with the documented default parameter values. |
| `public bool IsOn { get; set; }` | `[Parameter]`<br>Gets or sets whether the switch is on. |
| `public EventCallback<bool> IsOnChanged { get; set; }` | `[Parameter]`<br>Gets or sets the callback invoked when the user flips the switch. |
| `public RenderFragment? OnContent { get; set; }` | `[Parameter]`<br>Gets or sets content drawn inside the track while the switch is on, such as `I` or a short word. |
| `public RenderFragment? OffContent { get; set; }` | `[Parameter]`<br>Gets or sets content drawn inside the track while the switch is off, such as `O` or a short word. |
| `public RenderFragment? Description { get; set; }` | `[Parameter]`<br>Gets or sets a secondary line shown below the label. |
| `public SwitchLabelPosition LabelPosition { get; set; }` | `[Parameter]`<br>Gets or sets where the label is placed relative to the track. |
| `public ControlSize Size { get; set; }` | `[Parameter]`<br>Gets or sets the size step. Initial value `ControlSize.Medium`. |
| `public bool Disabled { get; set; }` | `[Parameter]`<br>Gets or sets whether the switch rejects interaction. |
| `public FieldContext? Field { get; set; }` | `[CascadingParameter]`<br>Gets or sets the enclosing field, supplied automatically. |
| `public bool On { get; }` | Gets whether the switch is currently on. |
| `public bool IsDragging { get; }` | Gets whether the thumb is held with pointer capture, including the movement threshold before previewing. |
| `protected override string ElementName { get; }` | Gets the NSS element name emitted by Switch. |
| `protected override string? ElementStyle { get; }` | Publishes the visual thumb fraction from zero to one without changing the committed accessible state. |
| `protected override void OnParametersSet()` | Validates the parameters and synchronizes the state. |
| `protected override void AddAttributes(RenderTreeBuilder builder)` | Adds the role, the states and the activation.<br>`builder`. The builder that receives the render frames. |
| `protected override void AddContent(RenderTreeBuilder builder)` | Adds the track with its thumb and state labels, the label and the description.<br>`builder`. The builder that receives the render frames. |
| `public Task ToggleAsync()` | Flips the switch as a user activation does and reports the change.<br>**Returns**. A task that completes when the change has been reported. |
| `public void Dispose()` | Releases capture when the control is removed. |

The bound `IsOn`, public `On`, `checked` accessibility state and value/input/change notifications remain committed while dragging. The source publishes `--nz-switch-position` as a visual fraction from zero to one; the root's `dragging` and `drag-on` attributes let themes preview each color phase without reporting a value change. A drag release selects its nearest end and emits one change only when that committed value differs.

A primary press on the thumb captures the pointer. Horizontal movement begins after four logical units; dominant vertical movement, Escape, losing pointer capture, disabling or an external value change cancel the preview. Capture loss clears it immediately, including when hover is retained; no subsequent move/release is required. A canceled drag does not consume the next independent click. Existing click/Space activation remains one toggle.

`SwitchThumbRail` supplies actual travel geometry and `SwitchThumbPosition` animates checked-state travel. The inner `SwitchThumb` owns hover/press scaling and material. Themes use the source fraction for position, keeping travel independent of scale. Track/rail/position bounds determine dragging, without theme-specific pixel constants in the control. Reduced motion applies the destination without a travel animation.

### `SwitchLabelPosition`

Namespace `Nazor.Controls`. [Source](../../Sources/Core/Nazor.Controls/Selection/Switch.cs)

| Declaration | Contract |
| --- | --- |
| `public enum SwitchLabelPosition` | Where a `Nazor.Controls.Switch` places its label, written as the `label-position` attribute. |
| `After = 0` | The label follows the switch. |
| `Before = 1` | The label precedes the switch, as in a settings list. |

### `Tab`

Namespace `Nazor.Controls`. [Source](../../Sources/Core/Nazor.Controls/Navigation/Tabs.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed class Tab : ComponentBase, IDisposable` | A page of `Nazor.Controls.Tabs`: its tab shows `Nazor.Controls.Tab.Leading`, `Nazor.Controls.Tab.Label` (or `Nazor.Controls.Tab.Header`) and `Nazor.Controls.Tab.Badge`; its content is the page. A `Nazor.Controls.Tab.Closable` tab shows a `TabClose` part that raises `Nazor.Controls.Tab.OnClose`, as does Delete on the focused tab; the owner removes the tab. |
| `protected override void OnParametersSet()` | Joins the enclosing tabs and refreshes them when the tab changes. |
| `protected override void BuildRenderTree(RenderTreeBuilder builder)` | Renders nothing; the tabs render the tab and its panel.<br>`builder`. The builder that receives the render frames. |
| `public void Dispose()` | Leaves the enclosing tabs. |
| `public Tab()` |  |
| `public string? Label { get; set; }` | `[Parameter]`<br>Gets or sets the plain-text label of the tab. |
| `public RenderFragment? Header { get; set; }` | `[Parameter]`<br>Gets or sets custom label content, used instead of `Nazor.Controls.Tab.Label`. |
| `public RenderFragment? Leading { get; set; }` | `[Parameter]`<br>Gets or sets content shown before the label, usually an `Nazor.Controls.Icon`. |
| `public RenderFragment? Badge { get; set; }` | `[Parameter]`<br>Gets or sets content shown after the label, such as a count. |
| `public string? Key { get; set; }` | `[Parameter]`<br>Gets or sets the selection key; reorder/drag-out require stable unique nonempty keys and keyed declarations. |
| `public bool Disabled { get; set; }` | `[Parameter]`<br>Gets or sets whether the tab cannot be selected; keyboard navigation skips it. |
| `public bool Closable { get; set; }` | `[Parameter]`<br>Gets or sets whether the tab shows a close part. |
| `public EventCallback OnClose { get; set; }` | `[Parameter]`<br>Gets or sets the callback invoked when the user asks to close the tab. |
| `public string? Class { get; set; }` | `[Parameter]`<br>Gets or sets NSS classes added to the tab element. |
| `public string? AccessibleName { get; set; }` | `[Parameter]`<br>Gets or sets the name of the tab for assistive technologies when the label is not text. |
| `public string? Tooltip { get; set; }` | `[Parameter]`<br>Gets or sets a short text shown when the pointer rests on the tab. |
| `public RenderFragment? ChildContent { get; set; }` | `[Parameter]`<br>Gets or sets the content of the page. |
| `public Tabs? Owner { get; set; }` | `[CascadingParameter]`<br>Gets or sets the enclosing tabs, supplied automatically. |

### `Table`

Namespace `Nazor.Controls`. [Source](../../Sources/Core/Nazor.Controls/Collections/Table.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed class Table : ContentControl` | A static document table, laid out by the engine's table layout: columns share their widths across every row, cells span columns and rows, and the table is exposed to assistive technologies with its rows, headers and cells. Its direct children are an optional `Nazor.Controls.TableHeader`, one or more `Nazor.Controls.TableBody` groups and an optional `Nazor.Controls.TableFooter`, in that order; the optional `Nazor.Controls.Table.Caption` names the table. It has no selection, sorting or virtualization: use a data grid for that. The element `Table` (role `table`) carries the `striped`, `bordered` and `dense` attributes; the caption is `TableCaption`. |
| `protected override void AddAttributes(RenderTreeBuilder builder)` | Adds the table semantics, the table layout and the style options.<br>`builder`. The builder that receives the render frames. |
| `protected override void OnAfterRender(bool firstRender)` | Checks that the direct children are row groups.<br>`firstRender`. Whether this is the first render. |
| `protected override void AddContent(RenderTreeBuilder builder)` | Adds the caption followed by the row groups.<br>`builder`. The builder that receives the render frames. |
| `public Table()` |  |
| `public RenderFragment? Caption { get; set; }` | `[Parameter]`<br>Gets or sets the visible caption, also the accessible name of the table. |
| `public bool Striped { get; set; }` | `[Parameter]`<br>Gets or sets whether every other body row is shaded, for long tables. |
| `public bool Bordered { get; set; }` | `[Parameter]`<br>Gets or sets whether every cell has a border, instead of lines between rows only. |
| `public bool Dense { get; set; }` | `[Parameter]`<br>Gets or sets whether the cells have less padding. |
| `protected override string ElementName { get; }` | Gets the element name. |

### `TableBody`

Namespace `Nazor.Controls`. [Source](../../Sources/Core/Nazor.Controls/Collections/Table.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed class TableBody : TableRowGroup` | A group of body rows of a `Nazor.Controls.Table`. The element is `TableBody`. |
| `public TableBody()` |  |
| `protected override string ElementName { get; }` | Gets the element name. |

### `TableCell`

Namespace `Nazor.Controls`. [Source](../../Sources/Core/Nazor.Controls/Collections/Table.cs)

| Declaration | Contract |
| --- | --- |
| `public class TableCell : ContentControl` | A data cell of a `Nazor.Controls.TableRow`, which can span several columns and rows. The element is `TableCell` (role `cell`). |
| `protected override void OnParametersSet()` | Validates the spans. |
| `protected override void OnAfterRender(bool firstRender)` | Checks that the cell is inside a row.<br>`firstRender`. Whether this is the first render. |
| `protected override void AddAttributes(RenderTreeBuilder builder)` | Adds the cell semantics and the spans.<br>`builder`. The builder that receives the render frames. |
| `public TableCell()` |  |
| `public int ColumnSpan { get; set; }` | `[Parameter]` Initial value `1`.<br>Gets or sets the number of columns the cell occupies, from 1 to 1000. |
| `public int RowSpan { get; set; }` | `[Parameter]` Initial value `1`.<br>Gets or sets the number of rows the cell occupies, from 1 to 65534; a span stops at the end of its row group. |
| `protected virtual string Role { get; }` | Gets the accessible role of the cell. |
| `protected override string ElementName { get; }` | Gets the element name. |

### `TableFooter`

Namespace `Nazor.Controls`. [Source](../../Sources/Core/Nazor.Controls/Collections/Table.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed class TableFooter : TableRowGroup` | The footer rows of a `Nazor.Controls.Table`, such as totals, placed after the bodies. The element is `TableFooter`. |
| `public TableFooter()` |  |
| `protected override string ElementName { get; }` | Gets the element name. |

### `TableHeader`

Namespace `Nazor.Controls`. [Source](../../Sources/Core/Nazor.Controls/Collections/Table.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed class TableHeader : TableRowGroup` | The header rows of a `Nazor.Controls.Table`, usually one row of `Nazor.Controls.TableHeaderCell` cells. The element is `TableHeader`. |
| `public TableHeader()` |  |
| `protected override string ElementName { get; }` | Gets the element name. |

### `TableHeaderCell`

Namespace `Nazor.Controls`. [Source](../../Sources/Core/Nazor.Controls/Collections/Table.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed class TableHeaderCell : TableCell` | A header cell of a `Nazor.Controls.TableRow`: it names the cells of its column or, with `Nazor.Controls.TableHeaderCell.Scope``Nazor.Controls.TableHeaderScope.Row`, of its row. The element `TableHeaderCell` has the role `columnheader` or `rowheader` and the `scope` attribute. |
| `protected override void OnParametersSet()` | Validates the scope and the spans. |
| `protected override void AddAttributes(RenderTreeBuilder builder)` | Adds the header semantics, the scope and the spans.<br>`builder`. The builder that receives the render frames. |
| `public TableHeaderCell()` |  |
| `public TableHeaderScope Scope { get; set; }` | `[Parameter]`<br>Gets or sets whether the header describes its column or its row. |
| `protected override string Role { get; }` | Gets the accessible role matching the scope. |
| `protected override string ElementName { get; }` | Gets the element name. |

### `TableHeaderScope`

Namespace `Nazor.Controls`. [Source](../../Sources/Core/Nazor.Controls/Collections/Table.cs)

| Declaration | Contract |
| --- | --- |
| `public enum TableHeaderScope` | Whether a `Nazor.Controls.TableHeaderCell` describes its column or its row. |
| `Column = 0` | The header describes the cells of its column. |
| `Row = 1` | The header describes the cells of its row. |

### `TableRow`

Namespace `Nazor.Controls`. [Source](../../Sources/Core/Nazor.Controls/Collections/Table.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed class TableRow : ContentControl` | A row of a `Nazor.Controls.TableHeader`, `Nazor.Controls.TableBody` or `Nazor.Controls.TableFooter`, holding `Nazor.Controls.TableCell` and `Nazor.Controls.TableHeaderCell` cells. The element `TableRow` (role `row`) carries `alternate` on every second row of its group, for striped tables. |
| `protected override void OnAfterRender(bool firstRender)` | Checks that the row is inside a row group and holds cells, and updates its alternation.<br>`firstRender`. Whether this is the first render. |
| `protected override void AddAttributes(RenderTreeBuilder builder)` | Adds the row semantics and the alternation.<br>`builder`. The builder that receives the render frames. |
| `public TableRow()` |  |
| `protected override string ElementName { get; }` | Gets the element name. |

### `TableRowGroup`

Namespace `Nazor.Controls`. [Source](../../Sources/Core/Nazor.Controls/Collections/Table.cs)

| Declaration | Contract |
| --- | --- |
| `public abstract class TableRowGroup : ContentControl` | A group of rows directly inside a `Nazor.Controls.Table`: `Nazor.Controls.TableHeader`, `Nazor.Controls.TableBody` or `Nazor.Controls.TableFooter`. |
| `protected override void OnAfterRender(bool firstRender)` | Checks that the group is inside a table and holds rows.<br>`firstRender`. Whether this is the first render. |
| `protected override void AddAttributes(RenderTreeBuilder builder)` | Adds the row group semantics.<br>`builder`. The builder that receives the render frames. |
| `protected TableRowGroup()` |  |

### `Tabs`

Namespace `Nazor.Controls`. [Source](../../Sources/Core/Nazor.Controls/Navigation/Tabs.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed class Tabs : Control, IDisposable` | Pages behind tabs. The `Nazor.Controls.Tab` elements written inside become tabs of a `TabsList` (role `tablist`) and the content of the selected one shows in a `TabsPanel` (role `tabpanel`, labeled by its tab). The selection is `Nazor.Controls.Tabs.SelectedIndex` (`@bind-SelectedIndex`) and, for tabs with a `Nazor.Controls.Tab.Key`, `Nazor.Controls.Tabs.SelectedKey` (`@bind-SelectedKey`). The list is one Tab stop: the arrows, Home and End move between the enabled tabs and select them, Ctrl+Tab and Ctrl+Page Down select the next tab from anywhere inside, Ctrl+Shift+Tab and Ctrl+Page Up the previous one, Delete closes a closable tab. A `TabsIndicator` follows the selected tab. Panels are rendered when first selected; with `Nazor.Controls.Tabs.KeepMounted` they stay rendered, hidden, afterwards. The parts are `TabsHeader`, `TabsScroller`, `TabsList`, `TabsScrollBack`, `TabsScrollForward`, `TabsOverflow` (the "More" button), `TabsAdd`, `TabsIndicator` and `TabsPanel`; the element carries `placement`, `orientation`, `overflow` and `size`. |
| `protected override void OnParametersSet()` | Validates the parameters and synchronizes the selection. |
| `protected override void AddAttributes(RenderTreeBuilder builder)` | Adds the layout attributes and the keyboard shortcuts.<br>`builder`. The builder that receives the render frames. |
| `protected override void AddContent(RenderTreeBuilder builder)` | Adds the header, the panels and the declarations.<br>`builder`. The builder that receives the render frames. |
| `protected override void OnAfterRender(bool firstRender)` | Scrolls the selected tab into view after the selection changed.<br>`firstRender`. Whether this is the first render. |
| `public Task SelectAsync(int position)` | Selects an enabled tab as the user would and reports the change.<br>`position`. The zero-based index of the tab.<br>**Returns**. A task that completes when the change has been reported. |
| `public Tabs()` | Creates a strip with shared captured-pointer and keyboard reorder cancellation. |
| `public RenderFragment? ChildContent { get; set; }` | `[Parameter]`<br>Gets or sets the `Nazor.Controls.Tab` declarations. |
| `public int SelectedIndex { get; set; }` | `[Parameter]`<br>Gets or sets the zero-based index of the selected tab. |
| `public EventCallback<int> SelectedIndexChanged { get; set; }` | `[Parameter]`<br>Gets or sets the callback invoked when the user selects another tab, with its index. |
| `public string? SelectedKey { get; set; }` | `[Parameter]`<br>Gets or sets the `Nazor.Controls.Tab.Key` of the selected tab; when it matches a tab it wins over `Nazor.Controls.Tabs.SelectedIndex`. |
| `public EventCallback<string?> SelectedKeyChanged { get; set; }` | `[Parameter]`<br>Gets or sets the callback invoked when the user selects another tab, with its key. |
| `public TabsPlacement Placement { get; set; }` | `[Parameter]`<br>Gets or sets where the tab list sits relative to the panels. |
| `public Orientation? Orientation { get; set; }` | `[Parameter]`<br>Gets or sets the direction of the tab list; null follows `Nazor.Controls.Tabs.Placement` (vertical at the start and end). |
| `public TabsOverflow Overflow { get; set; }` | `[Parameter]`<br>Gets or sets what happens to tabs that do not fit. |
| `public bool KeepMounted { get; set; }` | `[Parameter]`<br>Gets or sets whether panels stay rendered, hidden, after they were first selected. |
| `public ControlSize Size { get; set; }` | `[Parameter]` Initial value `ControlSize.Medium`.<br>Gets or sets the size step. |
| `public TabsPresentation Presentation { get; set; }` | `[Parameter]` Navigation by default; Document selects sheet presentation. |
| `public TabWidthMode WidthMode { get; set; }` | `[Parameter]` Equal by default; theme-owned horizontal document width behavior. |
| `public bool ShowAddButton { get; set; }` | `[Parameter]` False by default. Show the application-owned add action. |
| `public EventCallback OnAdd { get; set; }` | `[Parameter]` Add button/Ctrl+T callback; missing callback disables the button and leaves the shortcut unhandled. |
| `public bool CanReorderTabs { get; set; }` | `[Parameter]` False by default; pointer/Ctrl+Shift+arrow requests require `OnTabReorder`, stable unique nonempty keys and keyed declarations. |
| `public EventCallback<ItemReorderRequest<Tab>> OnTabReorder { get; set; }` | `[Parameter]` Application-owned model move; `NewIndex` is final after removal. |
| `public EventCallback<TabDragOutRequest> OnTabDragOut { get; set; }` | `[Parameter]` Optional release outside actual header bounds, independent of reorder. Application owns removal/window/data transfer. |
| `protected override string? ElementStyle { get; }` | Publishes actual `--nz-tabs-count`, then caller style. |
| `protected override Task OnAfterRenderAsync(bool firstRender)` | Reports indexes/keys normalized by structural reordering or removal. |
| `public void Dispose()` | Cancels captured gestures; idempotent. |
| `public int CurrentIndex { get; }` | Gets the index of the selected tab, or -1 when no tab can be selected. |
| `public IReadOnlyList<Tab> Items { get; }` | Gets the tabs in declaration order. |
| `public Orientation EffectiveOrientation { get; }` | Gets the direction of the tab list. |
| `protected override string ElementName { get; }` | Gets the NSS element name emitted by Tabs. |

Root attributes also include `presentation`, `width-mode`, `reorderable`, `dragging` and `indicator-ready`. Header rows publish `index`, `compact`, `dragging`, `drop-target` and `drop-position` (`before`/`after`). `TabsAdd` is outside the scroller with localized button semantics. Compact labels retain an accessible name/tooltip. Header drag hit testing uses realized rows and a six-unit threshold; no automatic edge pan. Disabled/removed/key-changed sources, lost capture and Escape cancel. Keyed external declaration order is synchronized without replacing retained page components. Native OS drag-and-drop/live component transfer are outside this contract.

### `TabDragOutRequest`

Namespace `Nazor.Controls`. [Source](../../Sources/Core/Nazor.Controls/Navigation/Tabs.cs)

Authored as a C# `readonly record struct`; the table includes its compiled value-type contract and synthesized equality/deconstruction members.

| Declaration | Contract |
| --- | --- |
| `public readonly struct TabDragOutRequest : IEquatable<TabDragOutRequest>` | Application-owned request after a header drag releases outside the strip. |
| `public TabDragOutRequest()` | Creates a default request with a null Tab and zero release coordinates. |
| `public TabDragOutRequest(Tab Tab, double X, double Y)` | Creates a request for the dragged declaration and logical-window release coordinates. |
| `public Tab Tab { get; init; }` | Stable dragged declaration. |
| `public double X { get; init; }` | Horizontal release coordinate in logical window units. |
| `public double Y { get; init; }` | Vertical release coordinate in logical window units. |
| `public override string ToString()` | Returns the record diagnostic representation. |
| `public override int GetHashCode()` | Hashes the tab declaration and release coordinates with their default equality semantics. |
| `public override bool Equals(object obj)` | Tests equality with the same record type; null or another type is unequal. |
| `public bool Equals(TabDragOutRequest other)` | Compares the tab declaration and release coordinates using their default equality semantics. |
| `public static bool operator ==(TabDragOutRequest left, TabDragOutRequest right)` | Tests value equality. |
| `public static bool operator !=(TabDragOutRequest left, TabDragOutRequest right)` | Tests value inequality. |
| `public void Deconstruct(out Tab Tab, out double X, out double Y)` | Extracts the tab declaration and release coordinates. |

### `TabWidthMode`

Namespace `Nazor.Controls`. [Source](../../Sources/Core/Nazor.Controls/Navigation/Tabs.cs)

| Declaration | Contract |
| --- | --- |
| `public enum TabWidthMode` | Horizontal document-header width presentation. |
| `Equal = 0` | Equal width within theme bounds. |
| `SizeToContent = 1` | Natural header width within theme bounds. |
| `Compact = 2` | Selected natural width; unselected labels/badges hidden, accessible names retained. |

### `TabsPresentation`

Namespace `Nazor.Controls`. [Source](../../Sources/Core/Nazor.Controls/Navigation/Tabs.cs)

| Declaration | Contract |
| --- | --- |
| `public enum TabsPresentation` | Relationship between tab strip and content. |
| `Navigation = 0` | Related sections of the same view. |
| `Document = 1` | Independent document sheets. |

### `TabsOverflow`

Namespace `Nazor.Controls`. [Source](../../Sources/Core/Nazor.Controls/Navigation/Tabs.cs)

| Declaration | Contract |
| --- | --- |
| `public enum TabsOverflow` | What `Nazor.Controls.Tabs` does with tabs that do not fit in the list. |
| `Scroll = 0` | The list scrolls; scroll buttons appear at its ends while some tabs are out of view. |
| `Menu = 1` | The tabs that do not fit move into a "More" menu; horizontal lists only, vertical lists scroll. |
| `None = 2` | Nothing: the list keeps its natural size and the theme decides (for example wrapping). |

### `TabsPlacement`

Namespace `Nazor.Controls`. [Source](../../Sources/Core/Nazor.Controls/Navigation/Tabs.cs)

| Declaration | Contract |
| --- | --- |
| `public enum TabsPlacement` | Where the tab list of `Nazor.Controls.Tabs` sits relative to the panels. |
| `Top = 0` | Above the panels. |
| `Bottom = 1` | Below the panels. |
| `Start = 2` | Before the panels, on the leading side, as a vertical list. |
| `End = 3` | After the panels, on the trailing side, as a vertical list. |

### `Tag`

Namespace `Nazor.Controls`. [Source](../../Sources/Core/Nazor.Controls/Feedback/Tag.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed class Tag : ContentControl` | A compact label for a status, a category or a chosen value, such as "Draft" or a recipient in a field. The label is the content between the tags (`TagLabel`); before it come either `Nazor.Controls.Tag.Leading` content (`TagLeading`) or a colored `Nazor.Controls.Tag.Dot` (`TagDot`). `Nazor.Controls.Tag.IsLive` marks something in progress (the `in-progress` attribute, announced as "in progress"). With `Nazor.Controls.Tag.Removable` a `TagRemove` button raises `Nazor.Controls.Tag.OnRemove`; Delete and Backspace on it do the same. |
| `protected override void OnParametersSet()` | Validates the look. |
| `protected override void AddAttributes(RenderTreeBuilder builder)` | Adds the look and state attributes.<br>`builder`. The builder that receives the render frames. |
| `protected override void AddContent(RenderTreeBuilder builder)` | Adds the leading content or dot, the label and the remove button.<br>`builder`. The builder that receives the render frames. |
| `public Tag()` |  |
| `public RenderFragment? Leading { get; set; }` | `[Parameter]`<br>Gets or sets content shown before the label, usually an `Nazor.Controls.Icon` or an `Nazor.Controls.Avatar`. |
| `public bool Dot { get; set; }` | `[Parameter]`<br>Gets or sets whether a dot in the tag's color is shown before the label. |
| `public Intent Intent { get; set; }` | `[Parameter]`<br>Gets or sets the meaning of the tag's color. |
| `public bool IsLive { get; set; }` | `[Parameter]`<br>Gets or sets whether the tag marks something in progress, such as a running job. |
| `public bool Removable { get; set; }` | `[Parameter]`<br>Gets or sets whether a remove button is shown. |
| `public EventCallback OnRemove { get; set; }` | `[Parameter]`<br>Gets or sets the callback invoked when the remove button is pressed. |
| `public bool Disabled { get; set; }` | `[Parameter]`<br>Gets or sets whether the tag rejects interaction. |
| `public ControlSize Size { get; set; }` | `[Parameter]` Initial value `ControlSize.Medium`.<br>Gets or sets the size step. |
| `protected override string ElementName { get; }` | Gets the NSS element name emitted by Tag. |

### `TeachingTip`

Namespace `Nazor.Controls`. [Source](../../Sources/Core/Nazor.Controls/Overlays/TeachingTip.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed class TeachingTip : ComponentBase` | A non-modal callout that points at a `Nazor.Controls.TeachingTip.Target`, or centers in its window without a target or anchor point, to introduce a feature or suggest a next step: an optional `Nazor.Controls.TeachingTip.Hero` image, a `Nazor.Controls.TeachingTip.Title` that names it, a `Nazor.Controls.TeachingTip.Subtitle`, a body (child content), `Nazor.Controls.TeachingTip.Actions` and a close button. `Nazor.Controls.TeachingTip.IsOpen` supports `@bind-IsOpen`; it stays open until the user closes it (Escape, the close button or an action), unless `Nazor.Controls.TeachingTip.CloseOnOutside` is set, and does not take the focus unless `Nazor.Controls.TeachingTip.FocusOnOpen` is set. Parts: `TeachingTip` (`side`, `targeted`, `has-hero`), `TeachingTipArrow` (`placement`), `TeachingTipHero`, `TeachingTipHeader`, `TeachingTipHeading`, `TeachingTipTitle`, `TeachingTipSubtitle`, `TeachingTipClose`, `TeachingTipBody` and `TeachingTipActions`. |
| `protected override void OnParametersSet()` | Synchronizes the open state and validates the parameters. |
| `protected override void BuildRenderTree(RenderTreeBuilder builder)` | Renders the anchored surface while open or retained for exit.<br>`builder`. The builder that receives the render frames. |
| `public Task OpenAsync()` | Opens the tip.<br>**Returns**. A task that completes when the change has been reported. |
| `public Task CloseAsync()` | Closes the tip.<br>**Returns**. A task that completes when the change has been reported. |
| `public TeachingTip()` |  |
| `public bool IsOpen { get; set; }` | `[Parameter]`<br>Gets or sets whether the tip is open. |
| `public EventCallback<bool> IsOpenChanged { get; set; }` | `[Parameter]`<br>Gets or sets the callback invoked when the tip opens or closes from inside. |
| `public ElementReference? Target { get; set; }` | `[Parameter]`<br>Gets or sets the element the tip points at. |
| `public (double X, double Y)? AnchorPoint { get; set; }` | `[Parameter]`<br>Gets or sets a point in window coordinates the tip points at when `Nazor.Controls.TeachingTip.Target` is null. |
| `public OverlayPlacement Placement { get; set; }` | `[Parameter]`<br>Gets or sets the preferred side of the target. |
| `public OverlayAlignment Alignment { get; set; }` | `[Parameter]` Initial value `OverlayAlignment.Center`.<br>Gets or sets the alignment with the target. |
| `public double Offset { get; set; }` | `[Parameter]` Initial value `8`.<br>Gets or sets the distance from the target, in logical units. |
| `public bool ShowArrow { get; set; }` | `[Parameter]` Initial value `true`.<br>Gets or sets whether an arrow points at the target or anchor point; an untargeted tip has no arrow. |
| `public RenderFragment? Hero { get; set; }` | `[Parameter]`<br>Gets or sets content shown edge to edge at the top, such as an illustration. |
| `public string? Title { get; set; }` | `[Parameter]`<br>Gets or sets the title, which names the tip. |
| `public string? Subtitle { get; set; }` | `[Parameter]`<br>Gets or sets a line below the title, which describes the tip. |
| `public RenderFragment? ChildContent { get; set; }` | `[Parameter]`<br>Gets or sets the body. |
| `public RenderFragment? Actions { get; set; }` | `[Parameter]`<br>Gets or sets the actions, such as buttons. |
| `public bool ShowCloseButton { get; set; }` | `[Parameter]` Initial value `true`.<br>Gets or sets whether the header shows a close button. |
| `public bool CloseOnOutside { get; set; }` | `[Parameter]`<br>Gets or sets whether a press outside closes the tip. |
| `public bool CloseOnEscape { get; set; }` | `[Parameter]` Initial value `true`.<br>Gets or sets whether Escape closes the tip. |
| `public bool FocusOnOpen { get; set; }` | `[Parameter]`<br>Gets or sets whether opening moves the focus into the tip. |
| `public EventCallback OnDismiss { get; set; }` | `[Parameter]`<br>Gets or sets the callback invoked when the user closes the tip with Escape, the close button or a press outside. |
| `public string? Class { get; set; }` | `[Parameter]`<br>Gets or sets the space-separated NSS classes of the surface. |
| `public string? Style { get; set; }` | `[Parameter]`<br>Gets or sets NSS declarations for the surface only. |
| `public string? AccessibleName { get; set; }` | `[Parameter]`<br>Gets or sets the accessible name; the title names the tip when null. |
| `public bool Opened { get; }` | Gets whether the tip is open. |

### `TextArea`

Namespace `Nazor.Controls`. [Source](../../Sources/Core/Nazor.Controls/Fields/TextField.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed class TextArea : TextFieldBase` | A multi-line text field where Enter inserts a line break. `Nazor.Controls.TextArea.Rows` sets the initial height in lines (the `--nz-textarea-rows` variable); with `Nazor.Controls.TextArea.Resizable` a `ResizeHandle` part lets the user change the height. The parts are `TextAreaLeading`, `TextAreaInput`, `TextAreaTrailing` and `TextAreaCounter`. |
| `protected override void OnParametersSet()` | Validates the number of lines. |
| `protected override void AddAttributes(RenderTreeBuilder builder)` | Adds the frame states and, when resizable, the height measurement.<br>`builder`. The builder that receives the render frames. |
| `protected override void AddAfterInput(RenderTreeBuilder builder)` | Adds the resize handle.<br>`builder`. The builder that receives the render frames. |
| `public TextArea()` |  |
| `public int Rows { get; set; }` | `[Parameter]` Initial value `3`.<br>Gets or sets the initial number of visible lines. |
| `public bool Resizable { get; set; }` | `[Parameter]`<br>Gets or sets whether the user can change the height with a handle. |
| `protected override string ElementName { get; }` | Gets the NSS element name emitted by TextArea. |
| `protected override bool Multiline { get; }` | Gets that Enter inserts a line break. |
| `protected override string? ElementStyle { get; }` | Gets the number of lines, the user height and the caller's declarations. |

### `TextBlock`

Namespace `Nazor.Controls`. [Source](../../Sources/Core/Nazor.Controls/Text/TextBlock.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed class TextBlock : ContentControl` | A run of text. It does not establish a heading or a field label; use `Nazor.Controls.Heading` and `Nazor.Controls.Label` for those meanings. |
| `protected override void AddAttributes(RenderTreeBuilder builder)` | Adds the text role and the selection behavior.<br>`builder`. The builder that receives the render frames. |
| `public TextBlock()` |  |
| `public bool IsTextSelectionEnabled { get; set; }` | `[Parameter]`<br>Gets or sets whether the text can be selected with the pointer (a double click selects a word, a triple click all) and copied with the keyboard or the context menu. The block takes the focus when clicked, but Tab does not stop there. |
| `protected override string ElementName { get; }` | Gets the NSS element name emitted by TextBlock. |

### `TextField`

Namespace `Nazor.Controls`. [Source](../../Sources/Core/Nazor.Controls/Fields/TextField.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed class TextField : TextFieldBase` | A single-line text field: a frame with optional leading content, prefix, the editor, suffix, a clear button, trailing content and a character counter. The parts are `TextFieldLeading`, `TextFieldPrefix`, `TextFieldInput`, `TextFieldSuffix`, `TextFieldClear`, `TextFieldTrailing` and `TextFieldCounter`. Inside a `Nazor.Controls.Field` it is labeled and described automatically. |
| `protected override void OnParametersSet()` | Validates the type. |
| `protected override void AddBeforeInput(RenderTreeBuilder builder)` | Adds the prefix.<br>`builder`. The builder that receives the render frames. |
| `protected override void AddEditorParameters(RenderTreeBuilder builder)` | Adds the input intent.<br>`builder`. The builder positioned on the editor component. |
| `protected override void AddAfterInput(RenderTreeBuilder builder)` | Adds the suffix and the clear button.<br>`builder`. The builder that receives the render frames. |
| `public TextField()` |  |
| `public TextFieldType Type { get; set; }` | `[Parameter]`<br>Gets or sets the kind of text expected, as a hint to input methods. |
| `public string? Prefix { get; set; }` | `[Parameter]`<br>Gets or sets fixed text shown before the editable text, such as a currency symbol. |
| `public string? Suffix { get; set; }` | `[Parameter]`<br>Gets or sets fixed text shown after the editable text, such as a unit. |
| `public bool Clearable { get; set; }` | `[Parameter]`<br>Gets or sets whether a button clears the text when there is some. |
| `protected override string ElementName { get; }` | Gets the NSS element name emitted by TextField. |
| `protected override bool Multiline { get; }` | Gets that Enter does not insert a line break. |

### `TextFieldBase`

Namespace `Nazor.Controls`. [Source](../../Sources/Core/Nazor.Controls/Fields/TextField.cs)

| Declaration | Contract |
| --- | --- |
| `public StylePartSet? InputParts { get; set; }` | `[Parameter]` Outer public names to which this field explicitly forwards its exposed `input` part. |
| `public abstract class TextFieldBase : Control` | The frame, side content and counter shared by single-line and multi-line text fields. The frame element is named after the control; the editor is its `{name}Input` part. |
| `protected override void OnParametersSet()` | Synchronizes the text with the parameter. |
| `protected override void AddAttributes(RenderTreeBuilder builder)` | Adds the frame states.<br>`builder`. The builder that receives the render frames. |
| `protected override void AddContent(RenderTreeBuilder builder)` | Adds the side content, the editor and the trailing parts.<br>`builder`. The builder that receives the render frames. |
| `protected virtual void AddBeforeInput(RenderTreeBuilder builder)` | Adds parts between the leading content and the editor, with sequence numbers from 1002 to 1009.<br>`builder`. The builder that receives the render frames. |
| `protected virtual void AddEditorParameters(RenderTreeBuilder builder)` | Adds extra editor parameters, with sequence numbers from 1030 to 1039.<br>`builder`. The builder positioned on the editor component. |
| `protected virtual void AddAfterInput(RenderTreeBuilder builder)` | Adds parts between the editor and the trailing content, with sequence numbers from 1050 to 1099.<br>`builder`. The builder that receives the render frames. |
| `public Task FocusAsync(bool preventScroll = false)` | Moves the keyboard focus to the editor.<br>`preventScroll`. Whether focusing keeps the current scroll position.<br>**Returns**. A task that completes when the focus has moved. |
| `public Task SetTextAsync(string value)` | Replaces the text as an edit would and reports it.<br>`value`. The new text.<br>**Returns**. A task that completes when the change has been reported. |
| `protected virtual Task TextChangedAsync(string previous, string value)` | Called after an edit changed the text and before `Nazor.Controls.TextFieldBase.ValueChanged` reports it.<br>`previous`. The text before the edit.<br>`value`. The new text.<br>**Returns**. A task that completes when the derived field has reacted. |
| `protected TextFieldBase()` |  |
| `protected override int? ElementTabIndex { get; }` |  |
| `public string? Value { get; set; }` | `[Parameter]`<br>Gets or sets the text; null shows as empty. |
| `public EventCallback<string> ValueChanged { get; set; }` | `[Parameter]`<br>Gets or sets the callback invoked on every edit; supports `@bind-Value`. |
| `public string? Placeholder { get; set; }` | `[Parameter]`<br>Gets or sets the hint shown while the text is empty; it does not replace a label. |
| `public bool ReadOnly { get; set; }` | `[Parameter]`<br>Gets or sets whether editing is blocked while selection and copying remain available. |
| `public bool Disabled { get; set; }` | `[Parameter]`<br>Gets or sets whether the field rejects interaction. |
| `public bool Required { get; set; }` | `[Parameter]`<br>Gets or sets whether a value is required; validation is application-owned. |
| `public bool Invalid { get; set; }` | `[Parameter]`<br>Gets or sets the externally determined invalid state. |
| `public int MaxLength { get; set; }` | `[Parameter]`<br>Gets or sets the maximum number of characters; zero or negative means unlimited. |
| `public bool ShowCount { get; set; }` | `[Parameter]`<br>Gets or sets whether a `{name}Counter` part shows the length, and the limit when there is one. |
| `public FieldAppearance Appearance { get; set; }` | `[Parameter]`<br>Gets or sets how the frame is drawn. |
| `public ControlSize Size { get; set; }` | `[Parameter]` Initial value `ControlSize.Medium`.<br>Gets or sets the size step. |
| `public RenderFragment? Leading { get; set; }` | `[Parameter]`<br>Gets or sets content shown before the text, such as an `Nazor.Controls.Icon`. |
| `public RenderFragment? Trailing { get; set; }` | `[Parameter]`<br>Gets or sets content shown after the text, such as a button. |
| `public bool AutoFocus { get; set; }` | `[Parameter]`<br>Gets or sets whether the editor takes the focus when it appears. |
| `public EventCallback<ChangeEventArgs> OnInput { get; set; }` | `[Parameter]`<br>Gets or sets the callback invoked with every raw edit. |
| `public EventCallback<ChangeEventArgs> OnChange { get; set; }` | `[Parameter]`<br>Gets or sets the callback invoked with the committed value. |
| `public FieldContext? Field { get; set; }` | `[CascadingParameter]`<br>Gets or sets the enclosing field, supplied automatically. |
| `public string Text { get; }` | Gets the current text. |
| `public ElementReference InputElement { get; }` | Gets the editor element, which receives the focus. |
| `protected abstract bool Multiline { get; }` | Gets whether Enter inserts a line break. |
| `protected bool IsInactive { get; }` | Gets whether the field rejects interaction, including through its field. |

### `TextFieldType`

Namespace `Nazor.Controls`. [Source](../../Sources/Core/Nazor.Controls/Fields/TextField.cs)

| Declaration | Contract |
| --- | --- |
| `public enum TextFieldType` | The kind of text a `Nazor.Controls.TextField` expects, as a hint to platform input methods. |
| `Text = 0` | Any text. |
| `Email = 1` | An email address. |
| `Url = 2` | A web address. |
| `Telephone = 3` | A telephone number. |
| `Number = 4` | Numeric text; for typed numbers with stepping use a number field. |

### `TimePicker`

Namespace `Nazor.Controls`. [Source](../../Sources/Core/Nazor.Controls/Pickers/TimePicker.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed class TimePicker : PickerFieldBase<TimeOnly>` | A time field: the `TimePickerInput` editor parses times typed in `Nazor.Controls.PickerFieldBase<TValue>.Culture` (24-hour or 12-hour as the culture writes them, or as `Nazor.Controls.TimePicker.Use24Hour` says) and the `TimePickerButton` opens the `TimePickerPopup`. `Nazor.Controls.PickerFieldBase<TValue>.Value` (`@bind-Value`) is the chosen time. With the `Nazor.Controls.TimePickerPresentation.Columns` presentation the popup holds `TimePickerColumns`, a `TimePickerColumn` list (with a `column` attribute: `hours`, `minutes`, `seconds`, `period`) per part of the time, whose `TimePickerOption`s change the value as the arrows move; Left and Right move between columns, Enter closes. With `Nazor.Controls.TimePickerPresentation.List` it holds one `TimePickerList` of `TimePickerOption`s every `Nazor.Controls.TimePicker.MinuteIncrement` minutes, where Enter, Space or a click choose and close. Times outside `Nazor.Controls.TimePicker.Minimum` and `Nazor.Controls.TimePicker.Maximum` are disabled, and typed ones mark the field invalid. |
| `protected override void OnParametersSet()` | Validates the parameters. |
| `protected override string FormatValue(TimeOnly value)` | Formats a time on the clock in use.<br>`value`. The time.<br>**Returns**. The text. |
| `protected override bool TryParseText(string text, out TimeOnly value)` | Parses a typed time.<br>`text`. The text.<br>`value`. The time.<br>**Returns**. true if the text is a time. |
| `protected override bool IsAllowed(TimeOnly value)` | Tells whether a time is within the limits.<br>`value`. The time.<br>**Returns**. true if it can be chosen. |
| `protected override void RenderPopup(RenderTreeBuilder builder)` | Renders the columns or the list.<br>`builder`. The builder positioned inside the popup. |
| `public TimePicker()` |  |
| `public bool? Use24Hour { get; set; }` | `[Parameter]`<br>Gets or sets whether the clock has 24 hours; null follows the culture. |
| `public int MinuteIncrement { get; set; }` | `[Parameter]` Initial value `1`.<br>Gets or sets the step of the minutes offered in the popup, from 1 to 60. |
| `public bool ShowSeconds { get; set; }` | `[Parameter]`<br>Gets or sets whether the seconds are shown and offered. |
| `public TimeOnly? Minimum { get; set; }` | `[Parameter]`<br>Gets or sets the earliest time that can be chosen, or null for no limit. |
| `public TimeOnly? Maximum { get; set; }` | `[Parameter]`<br>Gets or sets the latest time that can be chosen, or null for no limit. |
| `public TimePickerPresentation Presentation { get; set; }` | `[Parameter]`<br>Gets or sets how the popup lays out the times. |
| `protected override string ElementName { get; }` | Gets the NSS element name emitted by TimePicker. |
| `protected override string ButtonLabel { get; }` | Gets the name of the button and of the popup. |
| `public bool Is24Hour { get; }` | Gets whether the clock in use has 24 hours. |
| `protected override string? PatternHint { get; }` | Gets a time pattern shown as placeholder. |
| `protected override string FormatKey { get; }` | Gets the culture and the clock options, which rewrite the text when they change. |

### `TimePickerPresentation`

Namespace `Nazor.Controls`. [Source](../../Sources/Core/Nazor.Controls/Pickers/TimePicker.cs)

| Declaration | Contract |
| --- | --- |
| `public enum TimePickerPresentation` | How a `Nazor.Controls.TimePicker` lays out its popup, written as the popup's `presentation` attribute. |
| `Columns = 0` | One scrolling column for the hours, one for the minutes, optionally one for the seconds and one for AM and PM. |
| `List = 1` | A single list of times every `Nazor.Controls.TimePicker.MinuteIncrement` minutes. |

### `TimeText`

Namespace `Nazor.Controls`. [Source](../../Sources/Core/Nazor.Controls/Pickers/TimePicker.cs)

| Declaration | Contract |
| --- | --- |
| `public static class TimeText` | Formatting and lenient parsing of times typed in a culture, shared by the time pickers. |
| `public static bool Uses24Hour(CultureInfo culture)` | Tells whether a culture writes times with a 24-hour clock.<br>`culture`. The culture.<br>**Returns**. true for a 24-hour clock, false for a 12-hour clock with a period designator. |
| `public static (string Am, string Pm) Designators(CultureInfo culture)` | Gets the designators of the two halves of the day, falling back to AM and PM when the culture has none.<br>`culture`. The culture.<br>**Returns**. The morning and afternoon designators. |
| `public static string Format(TimeOnly time, CultureInfo culture, bool use24Hour, bool seconds)` | Formats a time with the culture's separator, on a 24-hour or 12-hour clock.<br>`time`. The time.<br>`culture`. The culture.<br>`use24Hour`. Whether to use a 24-hour clock.<br>`seconds`. Whether to include the seconds.<br>**Returns**. The text, for example `14:05` or `2:05 PM`. |
| `public static bool TryParse(string? text, CultureInfo culture, out TimeOnly time)` | Parses a time typed by the user: hours with optional minutes and seconds separated by the culture's separator, a colon or a period (or written together, as `930` or `1430`), optionally followed by a period designator (the culture's, AM, PM, A or P in any case); then the culture's general rules.<br>`text`. The text to parse.<br>`culture`. The culture of the text.<br>`time`. The parsed time.<br>**Returns**. true if the text is a time. |

### `TitleBarHeight`

Namespace `Nazor.Controls`. [Source](../../Sources/Core/Nazor.Controls/Frame/TitleBar.cs)

| Declaration | Contract |
| --- | --- |
| `public enum TitleBarHeight` | The height variant of a `Nazor.Controls.TitleBar`, written as the `height` attribute. |
| `Standard = 0` | The ordinary caption height, written as `standard`. |
| `Tall = 1` | A taller caption, for a search box or tabs in the title bar, written as `tall`. |

### `Toast`

Namespace `Nazor.Controls`. [Source](../../Sources/Core/Nazor.Controls/Feedback/Toast.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed class Toast : ContentControl` | The look of one notification, usually rendered by a `Nazor.Controls.ToastHost` for each entry of its `Nazor.Controls.Primitives.ToastQueue`. The parts are `ToastMark` (a mark for the `Nazor.Controls.Toast.Severity`), `ToastBody` with `ToastTitle`, `ToastMessage` and the content between the tags, the `ToastAction` button (`Nazor.Controls.Toast.ActionText`), the `ToastClose` button (`Nazor.Controls.Toast.Dismissible`) and the `ToastTimer` part, which publishes the remaining time as `--nz-toast-remaining` (1 to 0) while the toast counts down. `Nazor.Controls.Toast.OnHoldChanged` reports when the pointer or the keyboard focus arrives on the toast and when both have left, so the host can pause the countdown; the element has the `paused` attribute meanwhile. |
| `protected override void OnParametersSet()` | Validates the parameters. |
| `protected override void AddAttributes(RenderTreeBuilder builder)` | Adds the role, the name, the severity and the hold tracking.<br>`builder`. The builder that receives the render frames. |
| `protected override void AddContent(RenderTreeBuilder builder)` | Adds the mark, the body, the buttons and the timer.<br>`builder`. The builder that receives the render frames. |
| `public Toast()` |  |
| `public Severity Severity { get; set; }` | `[Parameter]`<br>Gets or sets the importance of the notification. |
| `public string? Title { get; set; }` | `[Parameter]`<br>Gets or sets the bold title shown before the message. |
| `public string? Message { get; set; }` | `[Parameter]`<br>Gets or sets the main text. |
| `public string? ActionText { get; set; }` | `[Parameter]`<br>Gets or sets the text of the action button, such as Undo, or null for none. |
| `public EventCallback OnAction { get; set; }` | `[Parameter]`<br>Gets or sets the callback invoked when the action button is pressed. |
| `public bool Dismissible { get; set; }` | `[Parameter]` Initial value `true`.<br>Gets or sets whether a close button is shown; true by default. |
| `public EventCallback OnClose { get; set; }` | `[Parameter]`<br>Gets or sets the callback invoked when the close button is pressed. |
| `public double? RemainingFraction { get; set; }` | `[Parameter]`<br>Gets or sets the remaining fraction of the display time, from 1 to 0, or null when the toast does not expire. |
| `public bool IsPaused { get; set; }` | `[Parameter]`<br>Gets or sets whether the countdown is suspended. |
| `public EventCallback<bool> OnHoldChanged { get; set; }` | `[Parameter]`<br>Gets or sets the callback invoked with true when the pointer or the keyboard focus arrives on the toast and with false when both have left it. |
| `public bool IsHeld { get; }` | Gets whether the pointer or the keyboard focus is on the toast. |
| `protected override string ElementName { get; }` | Gets the NSS element name emitted by Toast. |

### `ToastHost`

Namespace `Nazor.Controls`. [Source](../../Sources/Core/Nazor.Controls/Feedback/Toast.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed class ToastHost : ContentControl, IDisposable` | Shows the toast notifications of a `Nazor.Controls.Primitives.ToastQueue` stacked at a corner of the window, above everything else, and gives the queue to its content: any descendant can declare `[CascadingParameter] ToastQueue Toasts` and call `Toasts.Show(...)`. Place it once around the content of a window. The host drives the countdowns (pausing them while the pointer or the focus is on a toast), removes expired toasts, runs the action of a toast before closing it, and announces each new toast through a live region (errors at once). The element `ToastHost` holds the content; the overlay part `ToastHostRegion` (placement `none`, with the `toast-placement` attribute) is positioned by NSS and holds one `Nazor.Controls.Toast` per visible entry; the `ToastHostAnnouncer` part is the hidden live region. |
| `protected override void OnParametersSet()` | Validates the parameters and follows the queue. |
| `protected override void AddAttributes(RenderTreeBuilder builder)` | Adds the placement attribute.<br>`builder`. The builder that receives the render frames. |
| `protected override void AddContent(RenderTreeBuilder builder)` | Adds the content with the cascading queue, the live region and the overlay with the visible toasts.<br>`builder`. The builder that receives the render frames. |
| `public void Dispose()` | Stops following the queue and the countdown timer. |
| `public ToastHost()` |  |
| `public ToastQueue? Queue { get; set; }` | `[Parameter]`<br>Gets or sets the queue to show; null uses a queue owned by the host. |
| `public TimeProvider? Time { get; set; }` | `[Parameter]`<br>Gets or sets the clock used for the countdown timers and for the host's own queue, or null for the system clock. When `Nazor.Controls.ToastHost.Queue` is set, pass the same clock the queue was created with. |
| `public ToastPlacement Placement { get; set; }` | `[Parameter]` Initial value `ToastPlacement.BottomEnd`.<br>Gets or sets where the notifications are stacked. |
| `public int MaxVisible { get; set; }` | `[Parameter]` Initial value `3`.<br>Gets or sets how many notifications are on screen at once; the others wait. 3 by default. |
| `public ToastQueue Toasts { get; }` | Gets the queue shown by the host. |
| `protected override string ElementName { get; }` | Gets the NSS element name emitted by ToastHost. |

ToastHost composes its region through ItemsTransition<ToastEntry>, preserving reference identity across queue instances and retaining at most 32 outgoing visuals in addition to current queue entries. Outgoing actions are disabled and hidden from accessibility immediately; promotion, countdowns, pause handling, actions and announcements remain owned by ToastQueue/ToastHost. Preserved captured extents keep the bottom anchor fixed during the last exit.

Configure retention with `ToastHostRegion > ItemsTransition { overlay-exit-duration: ... }` and the outgoing appearance with `ToastHostRegion ItemsTransitionContent[exiting=true] > Toast`. The empty carrier remains mounted, disabled, semantically hidden and display:none, with overlay=false; it introduces no empty overlay or Tab stop. Basic and Fluent retain their immediate close policy unless application styles opt in.

### `ToastPlacement`

Namespace `Nazor.Controls`. [Source](../../Sources/Core/Nazor.Controls/Feedback/Toast.cs)

| Declaration | Contract |
| --- | --- |
| `public enum ToastPlacement` | The corner or edge of the window at which a `Nazor.Controls.ToastHost` stacks its notifications. |
| `TopStart = 0` | The top corner at the start of the reading direction (`top-start`). |
| `TopCenter = 1` | The middle of the top edge (`top-center`). |
| `TopEnd = 2` | The top corner at the end of the reading direction (`top-end`). |
| `BottomStart = 3` | The bottom corner at the start of the reading direction (`bottom-start`). |
| `BottomCenter = 4` | The middle of the bottom edge (`bottom-center`). |
| `BottomEnd = 5` | The bottom corner at the end of the reading direction (`bottom-end`). |

### `ToggleButton`

Namespace `Nazor.Controls`. [Source](../../Sources/Core/Nazor.Controls/Actions/Button.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed class ToggleButton : ButtonBase` | A button that keeps a pressed state, such as Bold in an editor: activation flips `Nazor.Controls.ToggleButton.IsPressed` (`@bind-IsPressed`) and runs the optional click callback and command. The element has the `pressed` attribute. |
| `protected override void OnParametersSet()` | Synchronizes the pressed state with the parameter. |
| `protected override void AddAttributes(RenderTreeBuilder builder)` | Adds the toggle role, the pressed state and the activation.<br>`builder`. The builder that receives the render frames. |
| `public ToggleButton()` |  |
| `public bool IsPressed { get; set; }` | `[Parameter]`<br>Gets or sets whether the button is pressed. |
| `public EventCallback<bool> IsPressedChanged { get; set; }` | `[Parameter]`<br>Gets or sets the callback invoked when the user changes the pressed state. |
| `public bool Pressed { get; }` | Gets whether the button is currently pressed. |
| `protected override string ElementName { get; }` | Gets the NSS element name emitted by ToggleButton. |

### `TokenField<TItem>`

Namespace `Nazor.Controls`. [Source](../../Sources/Core/Nazor.Controls/Fields/TokenField.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed class TokenField<TItem> : Control` | A field that collects several values as tokens (chips) followed by an editor. `Nazor.Controls.TokenField<TItem>.Values` (`@bind-Values`) lists the tokens. Typing filters `Nazor.Controls.TokenField<TItem>.Suggestions` (the values already chosen are left out); choosing a suggestion adds it. Enter or a `Nazor.Controls.TokenField<TItem>.Separators` character turns the typed text into a token through `Nazor.Controls.TokenField<TItem>.CreateToken` (by default the matching suggestion, or the text itself for strings), so pasting "a, b, c" adds three tokens. Backspace in an empty editor removes the last token, and each token has a remove button. With `Nazor.Controls.TokenField<TItem>.MaxTokens` the editor becomes read-only once the limit is reached (the `full` attribute). Built on `Nazor.Controls.Primitives.Combobox<TItem>`: the parts are `TokenFieldLeading` holding `TokenFieldTokens` (a list) with one `TokenFieldToken` per value (`TokenFieldTokenText` and `TokenFieldTokenRemove`), then `TokenFieldInput`, `TokenFieldPopup`, `TokenFieldOption` and `TokenFieldEmpty`.<br>`TItem`. The type of the values. |
| `protected override void OnParametersSet()` | Validates the parameters and synchronizes the values. |
| `public string TextOf(TItem item)` | Gets the text of a value.<br>`item`. The value.<br>**Returns**. Its text. |
| `public IReadOnlyList<TItem> CurrentSuggestions()` | Gets the suggestions offered for the current text.<br>**Returns**. The matching suggestions not chosen yet, at most `Nazor.Controls.TokenField<TItem>.MaxSuggestions`. |
| `protected override void BuildRenderTree(RenderTreeBuilder builder)` | Delegates the rendering to a combo box primitive named after the control.<br>`builder`. The builder that receives the render frames. |
| `protected override void OnAfterRender(bool firstRender)` | Exposes the combo box element as the control's element.<br>`firstRender`. Whether this is the first render. |
| `public Task<bool> AddAsync(TItem item)` | Adds a value as the user would, unless the field is full, read-only or already holds it.<br>`item`. The value to add.<br>**Returns**. true if the value was added. |
| `public Task RemoveAtAsync(int index)` | Removes the value at an index as its remove button does.<br>`index`. The index of the token.<br>**Returns**. A task that completes when the change has been reported. |
| `public Task<bool> CommitTextAsync()` | Turns the typed text into a token, as Enter does.<br>**Returns**. true if a token was added. |
| `public Task FocusAsync(bool preventScroll = false)` | Moves the keyboard focus to the editor.<br>`preventScroll`. Whether focusing keeps the current scroll position.<br>**Returns**. A task that completes when the focus has moved. |
| `public TokenField()` |  |
| `public IReadOnlyList<TItem>? Values { get; set; }` | `[Parameter]`<br>Gets or sets the chosen values. |
| `public EventCallback<IReadOnlyList<TItem>> ValuesChanged { get; set; }` | `[Parameter]`<br>Gets or sets the callback invoked when the user adds or removes a value; supports `@bind-Values`. |
| `public IReadOnlyList<TItem>? Suggestions { get; set; }` | `[Parameter]`<br>Gets or sets the values suggested while typing. |
| `public Func<TItem, string, bool>? Filter { get; set; }` | `[Parameter]`<br>Gets or sets whether a suggestion matches the typed text; by default its text contains the text, ignoring case. |
| `public Func<TItem, string>? ItemText { get; set; }` | `[Parameter]`<br>Gets or sets the text of a value; `ToString` by default. |
| `public RenderFragment<TItem>? ItemTemplate { get; set; }` | `[Parameter]`<br>Gets or sets how a suggestion is rendered in the list. |
| `public RenderFragment<TItem>? TokenTemplate { get; set; }` | `[Parameter]`<br>Gets or sets how the content of a token is rendered; its text by default. |
| `public Func<string, TItem?>? CreateToken { get; set; }` | `[Parameter]`<br>Gets or sets the function turning typed text into a value, or null to reject it. When not set, the suggestion whose text equals the text is used, and for string values the text itself. |
| `public string Separators { get; set; }` | `[Parameter]` Initial value `",;"`.<br>Gets or sets the characters that end a token while typing; a comma and a semicolon by default. |
| `public int MaxTokens { get; set; }` | `[Parameter]`<br>Gets or sets the largest number of tokens; zero means unlimited. |
| `public bool AllowDuplicates { get; set; }` | `[Parameter]`<br>Gets or sets whether the same value can be added more than once. |
| `public int MaxSuggestions { get; set; }` | `[Parameter]` Initial value `8`.<br>Gets or sets the largest number of suggestions shown; 8 by default. |
| `public RenderFragment? EmptyContent { get; set; }` | `[Parameter]`<br>Gets or sets content shown in the list when no suggestion matches; null closes the list instead. |
| `public string? Placeholder { get; set; }` | `[Parameter]`<br>Gets or sets the hint shown while there are no tokens and no text. |
| `public bool ReadOnly { get; set; }` | `[Parameter]`<br>Gets or sets whether the tokens cannot be changed. |
| `public bool Disabled { get; set; }` | `[Parameter]`<br>Gets or sets whether the field rejects interaction. |
| `public bool Required { get; set; }` | `[Parameter]`<br>Gets or sets whether a value is required; validation is application-owned. |
| `public bool Invalid { get; set; }` | `[Parameter]`<br>Gets or sets the externally determined invalid state. |
| `public FieldAppearance Appearance { get; set; }` | `[Parameter]`<br>Gets or sets how the frame is drawn. |
| `public ControlSize Size { get; set; }` | `[Parameter]` Initial value `ControlSize.Medium`.<br>Gets or sets the size step. |
| `public FieldContext? Field { get; set; }` | `[CascadingParameter]`<br>Gets or sets the enclosing field, supplied automatically. |
| `public IReadOnlyList<TItem> CurrentValues { get; }` | Gets the chosen values. |
| `public string Text { get; }` | Gets the text being typed. |
| `public bool IsFull { get; }` | Gets whether the limit of tokens is reached. |
| `protected override string ElementName { get; }` | Gets the NSS element name emitted by TokenField. |

### `Toolbar`

Namespace `Nazor.Controls`. [Source](../../Sources/Core/Nazor.Controls/Actions/Toolbar.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed class Toolbar : ContentControl, IDisposable` | A bar of commands, such as the formatting buttons of an editor. It is one Tab stop: the arrow keys (Up and Down when vertical), Home and End move between its items. Fill it freely with buttons and fields and organize them with `Nazor.Controls.ToolbarGroup`, `Nazor.Controls.ToolbarSeparator` and `Nazor.Controls.ToolbarSpacer`, or give it `Nazor.Controls.Toolbar.Commands`: it then renders one button per command (a toggle button for commands with a checked state) in a `ToolbarCommands` row and moves the commands that do not fit into a "More" `Nazor.Controls.MenuButton` (`ToolbarCommandsOverflow`); free content can follow the commands. Commands are not split into an overflow menu in a vertical toolbar. The element has the `toolbar` role and the `orientation`, `vertical`, `size` and `label-position` attributes, which themes use to style the items. |
| `protected override void OnParametersSet()` | Validates the parameters and follows the commands. |
| `protected override void AddAttributes(RenderTreeBuilder builder)` | Adds the toolbar role and the attributes that style the items.<br>`builder`. The builder that receives the render frames. |
| `protected override void AddContent(RenderTreeBuilder builder)` | Adds the command buttons and the free content.<br>`builder`. The builder that receives the render frames. |
| `public void Dispose()` | Stops following the commands. |
| `public Toolbar()` |  |
| `public Orientation Orientation { get; set; }` | `[Parameter]`<br>Gets or sets the direction of the bar. |
| `public ToolbarLabelPosition LabelPosition { get; set; }` | `[Parameter]`<br>Gets or sets where the items show their labels. |
| `public ControlSize Size { get; set; }` | `[Parameter]` Initial value `ControlSize.Medium`.<br>Gets or sets the size of the command buttons, also written as the `size` attribute. |
| `public IEnumerable<Command>? Commands { get; set; }` | `[Parameter]`<br>Gets or sets the commands rendered as buttons, in order; null for free content only. |
| `public RenderFragment<Command>? CommandIcon { get; set; }` | `[Parameter]`<br>Gets or sets the icon of a command's button, shown before its label; null for none. |
| `public Func<Command, int>? CommandPriority { get; set; }` | `[Parameter]`<br>Gets or sets the priority of a command; lower priorities move to the overflow menu first. |
| `public string? OverflowLabel { get; set; }` | `[Parameter]`<br>Gets or sets the name of the overflow button; "More options" in the language of the interface by default. |
| `protected override string ElementName { get; }` | Gets the NSS element name emitted by Toolbar. |

### `ToolbarGroup`

Namespace `Nazor.Controls`. [Source](../../Sources/Core/Nazor.Controls/Actions/Toolbar.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed class ToolbarGroup : ContentControl` | A named set of related items in a `Nazor.Controls.ToolbarGroup.Toolbar`, such as the alignment buttons: it has the `group` role, takes its name from `Nazor.Controls.ToolbarGroup.Label` and keeps the toolbar's single Tab stop and arrow navigation. |
| `protected override void AddAttributes(RenderTreeBuilder builder)` | Adds the group role, the name and the orientation of the toolbar.<br>`builder`. The builder that receives the render frames. |
| `public ToolbarGroup()` |  |
| `public string? Label { get; set; }` | `[Parameter]`<br>Gets or sets the name of the group for assistive technologies. |
| `protected override string ElementName { get; }` | Gets the NSS element name emitted by ToolbarGroup. |

### `ToolbarLabelPosition`

Namespace `Nazor.Controls`. [Source](../../Sources/Core/Nazor.Controls/Actions/Toolbar.cs)

| Declaration | Contract |
| --- | --- |
| `public enum ToolbarLabelPosition` | Where the items of a `Nazor.Controls.Toolbar` show their label relative to their icon. |
| `Beside = 0` | The label follows the icon, written as `beside`. |
| `Below = 1` | The label is under the icon, written as `below`. |
| `None = 2` | Only the icon is shown; the label stays the accessible name and the tooltip. Written as `none`. |

### `ToolbarSeparator`

Namespace `Nazor.Controls`. [Source](../../Sources/Core/Nazor.Controls/Actions/Toolbar.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed class ToolbarSeparator : Control` | A line between groups of items in a `Nazor.Controls.ToolbarSeparator.Toolbar`. It has the `separator` role and the `orientation` of the line, across the bar: vertical in a horizontal toolbar. |
| `protected override void AddAttributes(RenderTreeBuilder builder)` | Adds the separator role and the orientation of the line.<br>`builder`. The builder that receives the render frames. |
| `public ToolbarSeparator()` |  |
| `protected override string ElementName { get; }` | Gets the NSS element name emitted by ToolbarSeparator. |

### `ToolbarSpacer`

Namespace `Nazor.Controls`. [Source](../../Sources/Core/Nazor.Controls/Actions/Toolbar.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed class ToolbarSpacer : Control` | Empty space that pushes the following items of a `Nazor.Controls.Toolbar` or `Nazor.Controls.ActionBar` to the far end. |
| `protected override void AddAttributes(RenderTreeBuilder builder)` | Hides the spacer from assistive technologies.<br>`builder`. The builder that receives the render frames. |
| `public ToolbarSpacer()` |  |
| `protected override string ElementName { get; }` | Gets the NSS element name emitted by ToolbarSpacer. |

### `Tooltip`

Namespace `Nazor.Controls`. [Source](../../Sources/Core/Nazor.Controls/Overlays/Tooltip.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed class Tooltip : ComponentBase` | A short, non-interactive description of its `Nazor.Controls.Tooltip.Trigger`, shown while the pointer rests on it or the keyboard focus reaches it, after the window's shared tooltip delay (or `Nazor.Controls.Tooltip.Delay`), and closed by Escape. It describes the trigger for assistive technologies. Use `Nazor.Controls.Tooltip.Text` or child content, with an optional bold `Nazor.Controls.Tooltip.Title`; hovering the noninteractive content keeps it open while clicks pass through the surface; for interactive content use a `Nazor.Controls.Popover`. Parts: `TooltipTrigger`, `Tooltip`, `TooltipArrow` (`placement`), `TooltipTitle` and `TooltipText`. |
| `protected override void OnParametersSet()` | Validates the parameters. |
| `protected override void BuildRenderTree(RenderTreeBuilder builder)` | Renders the tooltip container.<br>`builder`. The builder that receives the render frames. |
| `public Tooltip()` |  |
| `public RenderFragment? Trigger { get; set; }` | `[Parameter]` `[EditorRequired]`<br>Gets or sets the content the tooltip describes. |
| `public string? Text { get; set; }` | `[Parameter]`<br>Gets or sets the text of the tooltip. |
| `public RenderFragment? ChildContent { get; set; }` | `[Parameter]`<br>Gets or sets rich, non-interactive content, shown after `Nazor.Controls.Tooltip.Text`. |
| `public string? Title { get; set; }` | `[Parameter]`<br>Gets or sets a bold first line. |
| `public OverlayPlacement Placement { get; set; }` | `[Parameter]` Initial value `OverlayPlacement.Top`.<br>Gets or sets the preferred side of the trigger. |
| `public OverlayAlignment Alignment { get; set; }` | `[Parameter]` Initial value `OverlayAlignment.Center`.<br>Gets or sets the alignment with the trigger. |
| `public double Offset { get; set; }` | `[Parameter]` Initial value `4`.<br>Gets or sets the distance from the trigger, in logical units. |
| `public TimeSpan? Delay { get; set; }` | `[Parameter]`<br>Gets or sets an opening delay; null uses the window's shared tooltip delay. |
| `public bool Disabled { get; set; }` | `[Parameter]`<br>Gets or sets whether the tooltip is not shown. |
| `public bool ShowArrow { get; set; }` | `[Parameter]`<br>Gets or sets whether an arrow points at the trigger. |
| `public string? Class { get; set; }` | `[Parameter]`<br>Gets or sets the space-separated NSS classes of the surface. |
| `public string? Id { get; set; }` | `[Parameter]`<br>Gets or sets the identifier of the surface. |
| `public string? Style { get; set; }` | `[Parameter]`<br>Gets or sets NSS declarations for the surface only. |

### `TreeView<TItem>`

Namespace `Nazor.Controls`. [Source](../../Sources/Core/Nazor.Controls/Collections/TreeView.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed class TreeView<TItem> : Control where TItem : notnull` | A virtualized hierarchy built on `Nazor.Controls.Primitives.TreeModel<TItem>` and `Nazor.Controls.Primitives.Virtualizer<TItem>`: the `Nazor.Controls.TreeView<TItem>.Roots` and their children, given by `Nazor.Controls.TreeView<TItem>.Children` or loaded the first time an item is expanded with `Nazor.Controls.TreeView<TItem>.LoadChildren`. It is a single Tab stop with the tree keyboard conventions: Up and Down move, Right expands or goes to the first child, Left collapses or goes to the parent, Home, End, Page Up and Page Down jump, typing finds an item, `*` expands every sibling, Enter (or a double click) raises `Nazor.Controls.TreeView<TItem>.OnItemActivated`. `Nazor.Controls.TreeView<TItem>.SelectedItem` and `Nazor.Controls.TreeView<TItem>.SelectedItems` support `@bind-`; with `Nazor.Controls.TreeView<TItem>.ShowCheckBoxes` every item has a check box checked in cascade (a parent is checked, unchecked or mixed according to its children) and `Nazor.Controls.TreeView<TItem>.CheckedItems` (`@bind-CheckedItems`) holds the checked leaves. The element `TreeView` (role `tree`) holds the scrolling `TreeViewViewport` and the rows `TreeViewItem` (role `treeitem`, with `level`, `expanded` and the `--nz-tree-level` variable for the indent), whose parts are `TreeViewIndentGuide`, `TreeViewExpander`, `TreeViewItemCheck`, `TreeViewItemContent` and `TreeViewLoading`; `TreeViewEmpty` replaces the viewport when there are no roots.<br>`TItem`. The type of the items. |
| `public TreeView()` | Creates a tree view. |
| `protected override void OnParametersSet()` | Validates the parameters and synchronizes the model, the selection and the checks. |
| `public Task SetExpandedAsync(TItem item, bool expanded)` | Expands or collapses an item, loading its children the first time when needed.<br>`item`. The item.<br>`expanded`. Whether the item should be expanded.<br>**Returns**. A task that completes when the children are available. |
| `public void ExpandAll()` | Expands every item whose children are known, recursively; children loaded on demand are not loaded. |
| `public void CollapseAll()` | Collapses every item. |
| `public bool Reveal(TItem item)` | Expands the ancestors of an item, makes it the current row and scrolls it into view.<br>`item`. The item.<br>**Returns**. true if the item is among the known items. |
| `protected override void AddAttributes(RenderTreeBuilder builder)` | Adds the tree role, the selection mode and the keyboard handling.<br>`builder`. The builder that receives the render frames. |
| `protected override void AddContent(RenderTreeBuilder builder)` | Adds the viewport with the realized rows, or the empty content.<br>`builder`. The builder that receives the render frames. |
| `protected override Task OnAfterRenderAsync(bool firstRender)` | Moves the focus to the current row once it is realized.<br>`firstRender`. Whether this is the first render.<br>**Returns**. A task that completes when the work is done. |
| `public IReadOnlyList<TItem> Roots { get; set; }` | `[Parameter]` Initial value `[]`.<br>Gets or sets the top-level items. |
| `public Func<TItem, IReadOnlyList<TItem>?>? Children { get; set; }` | `[Parameter]`<br>Gets or sets the function that gives an item's children. Return null for items that still need to load with `LoadChildren`. |
| `public Func<TItem, bool>? HasChildren { get; set; }` | `[Parameter]`<br>Gets or sets the function that tells whether an item has children before they are loaded. |
| `public Func<TItem, Task<IReadOnlyList<TItem>>>? LoadChildren { get; set; }` | `[Parameter]`<br>Gets or sets the function that loads an item's children the first time it is expanded; a `TreeViewLoading` part shows meanwhile. |
| `public RenderFragment<TItem>? ItemTemplate { get; set; }` | `[Parameter]`<br>Gets or sets how each item is rendered; `Nazor.Controls.TreeView<TItem>.ItemText` is shown when null. |
| `public Func<TItem, string>? ItemText { get; set; }` | `[Parameter]`<br>Gets or sets the text of an item, shown without a template and matched by typing; `ToString` is used when null. |
| `public Func<TItem, bool>? ItemDisabled { get; set; }` | `[Parameter]`<br>Gets or sets a function that tells whether an item is disabled: it is skipped by the keyboard and cannot be selected. |
| `public double? ItemHeight { get; set; }` | `[Parameter]` Null uses the measured theme row metric; explicit values fix the row height, or provide an estimate with `MeasureItems`. Basic falls back to 28 logical units. |
| `public bool MeasureItems { get; set; }` | `[Parameter]`<br>Gets or sets whether rows are measured after layout, for rows of different heights. |
| `public SelectionMode SelectionMode { get; set; }` | `[Parameter]` Initial value `SelectionMode.Single`.<br>Gets or sets how many items can be selected. |
| `public TItem? SelectedItem { get; set; }` | `[Parameter]`<br>Gets or sets the selected item in single selection. |
| `public EventCallback<TItem?> SelectedItemChanged { get; set; }` | `[Parameter]`<br>Gets or sets the callback invoked when the user selects another item in single selection. |
| `public IReadOnlyCollection<TItem>? SelectedItems { get; set; }` | `[Parameter]`<br>Gets or sets the selected items in multiple selection. |
| `public EventCallback<IReadOnlyCollection<TItem>> SelectedItemsChanged { get; set; }` | `[Parameter]`<br>Gets or sets the callback invoked with the selected visible items, in display order, when the user changes a multiple selection. |
| `public bool ShowCheckBoxes { get; set; }` | `[Parameter]`<br>Gets or sets whether every item shows a check box checked in cascade; Space toggles the current one. |
| `public IReadOnlyCollection<TItem>? CheckedItems { get; set; }` | `[Parameter]`<br>Gets or sets the checked items; checking a parent checks its known descendants, and the leaves are reported. |
| `public EventCallback<IReadOnlyCollection<TItem>> CheckedItemsChanged { get; set; }` | `[Parameter]`<br>Gets or sets the callback invoked with the checked leaves when the user changes the checks. |
| `public EventCallback<TItem> OnItemActivated { get; set; }` | `[Parameter]`<br>Gets or sets the callback invoked when an item is activated with Enter or a double click. |
| `public bool CanReorderItems { get; set; }` | `[Parameter]` Opt-in primary drag or Ctrl/Meta+Shift+arrows reordering within the current sibling collection; no reparenting; requires `OnItemReorder`. |
| `public EventCallback<ItemReorderRequest<TItem>> OnItemReorder { get; set; }` | `[Parameter]` Requests one move; the application supplies updated items. Selection remains keyed to the same item. |
| `public bool IndentGuides { get; set; }` | `[Parameter]`<br>Gets or sets whether rows show a `TreeViewIndentGuide` line for each ancestor level. |
| `public double? Indent { get; set; }` | `[Parameter]`<br>Gets or sets the indent of each level, written as the `--nz-tree-indent` variable; the theme's indent is used when null. |
| `public RenderFragment? EmptyContent { get; set; }` | `[Parameter]`<br>Gets or sets the content shown when there are no roots; a localized "No results" when null. |
| `public bool Disabled { get; set; }` | `[Parameter]`<br>Gets or sets whether the tree rejects interaction. |
| `public TreeModel<TItem> Model { get; }` | Gets the model that holds the expansion, the loaded children and the checks. |
| `public IReadOnlyList<TreeRow<TItem>> VisibleRows { get; }` | Gets the visible rows, in display order. |
| `protected override string ElementName { get; }` | Gets the element name. |
| `protected override string? ElementStyle { get; }` | Gets the indent variable and the caller's declarations. |
| `public void Refresh()` | Rereads known hierarchy after in-place sibling changes, preserving expanded/current identity. |

### `ValueLabelMode`

Namespace `Nazor.Controls`. [Source](../../Sources/Core/Nazor.Controls/Selection/Slider.cs)

| Declaration | Contract |
| --- | --- |
| `public enum ValueLabelMode` | When a slider shows its value next to the thumb, written as the `value-label` attribute. |
| `None = 0` | The value is not shown. |
| `OnFocus = 1` | The value is shown while the slider has the keyboard focus. |
| `Always = 2` | The value is always shown. |

### `WindowCommands`

Namespace `Nazor.Controls`. [Source](../../Sources/Core/Nazor.Controls/Frame/WindowCommands.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed class WindowCommands : ComponentBase, IDisposable` | The state and the caption commands of the current window, for applications that draw their own minimize, maximize and close buttons: it renders no element, only `Nazor.Controls.WindowCommands.ChildContent`, which receives this component as its context (`<WindowCommands Context="window">`) and is rendered again whenever the window's state, capabilities or system buttons change. `Nazor.Controls.CaptionButtons` is built on it. |
| `protected override void OnParametersSet()` | Subscribes to the window's changes. |
| `protected override void BuildRenderTree(RenderTreeBuilder builder)` | Renders the child content with this component as its context.<br>`builder`. The builder that receives the render frames. |
| `public Task MinimizeAsync()` | Minimizes the window when it can be minimized.<br>**Returns**. A completed task, so the method can be an event callback. |
| `public Task MaximizeAsync()` | Maximizes the window when it can be maximized.<br>**Returns**. A completed task, so the method can be an event callback. |
| `public Task RestoreAsync()` | Restores the window from the maximized, minimized or full-screen state.<br>**Returns**. A completed task, so the method can be an event callback. |
| `public Task ToggleMaximizeAsync()` | Restores a maximized window, or maximizes it when it can be maximized.<br>**Returns**. A completed task, so the method can be an event callback. |
| `public Task ToggleFullScreenAsync()` | Enters full screen, or leaves it when the window is already in full screen.<br>**Returns**. A completed task, so the method can be an event callback. |
| `public Task CloseAsync()` | Closes the window.<br>**Returns**. A completed task, so the method can be an event callback. |
| `public void Dispose()` | Stops following the window. |
| `public WindowCommands()` |  |
| `public IWindow Window { get; set; }` | `[Inject]`<br>Gets or sets the window, supplied automatically. |
| `public RenderFragment<WindowCommands>? ChildContent { get; set; }` | `[Parameter]`<br>Gets or sets the content that uses the window's state and commands. |
| `public WindowState State { get; }` | Gets the window state. |
| `public bool IsMaximized { get; }` | Gets whether the window is maximized; the maximize command then restores it. |
| `public bool IsFullScreen { get; }` | Gets whether the window is in full screen. |
| `public bool CanMinimize { get; }` | Gets whether the window can be minimized. |
| `public bool CanMaximize { get; }` | Gets whether the maximize command is available: the window can be maximized, or is maximized and can be restored. |
| `public bool CanEnterFullScreen { get; }` | Gets whether the window can enter full screen. |
| `public bool ShowsSystemButtons { get; }` | Gets whether the window shows the system's own caption buttons in its custom title bar, in which case drawing other caption buttons would duplicate them. |
| `public WindowButtonsLayout Layout { get; }` | Gets the system's placement of the caption buttons, in physical left-to-right order on each side. |

## Inline text formatting

These controls participate in the shared horizontal text flow described in [running text](../Guide/10-Text-Input-and-Data-Controls.md#format-running-text). Their appearance belongs to the selected theme. Inherited content, styling and accessibility parameters follow ContentControl and Control.


### `Span`

Namespace `Nazor.Controls`. [Source](../../Sources/Core/Nazor.Controls/Text/InlineText.cs)

| Declaration | Contract |
| --- | --- |
| `public class Span : ContentControl` | A styled segment of running text, sharing wrapping, selection and baselines with adjacent inline text. |
| `public Span()` | Creates the inline control. |
| `protected override string ElementName { get; }` | Gets the NSS element name `Span`. |


### `Strong`

Namespace `Nazor.Controls`. [Source](../../Sources/Core/Nazor.Controls/Text/InlineText.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed class Strong : Span` | An important inline text segment, normally displayed with a stronger weight. |
| `public Strong()` | Creates the inline control. |
| `protected override string ElementName { get; }` | Gets the NSS element name `Strong`. |


### `Emphasis`

Namespace `Nazor.Controls`. [Source](../../Sources/Core/Nazor.Controls/Text/InlineText.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed class Emphasis : Span` | An emphasized inline text segment, normally displayed in italic. |
| `public Emphasis()` | Creates the inline control. |
| `protected override string ElementName { get; }` | Gets the NSS element name `Emphasis`. |


### `InlineCode`

Namespace `Nazor.Controls`. [Source](../../Sources/Core/Nazor.Controls/Text/InlineText.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed class InlineCode : Span` | Inline source code with theme-owned font and decoration. Use CodeBlock for a standalone listing. |
| `public InlineCode()` | Creates the inline control. |
| `protected override string ElementName { get; }` | Gets the NSS element name `InlineCode`. |


### `Mark`

Namespace `Nazor.Controls`. [Source](../../Sources/Core/Nazor.Controls/Text/InlineText.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed class Mark : Span` | A highlighted inline passage with theme-owned colors and padding. |
| `public Mark()` | Creates the inline control. |
| `protected override string ElementName { get; }` | Gets the NSS element name `Mark`. |


### `Abbreviation`

Namespace `Nazor.Controls`. [Source](../../Sources/Core/Nazor.Controls/Text/InlineText.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed class Abbreviation : Span` | An abbreviated passage. Tooltip or AccessibleDescription supplies its expansion; a described abbreviation exposes an accessible text node without adding a keyboard focus stop. |
| `public Abbreviation()` | Creates the inline control. |
| `protected override string ElementName { get; }` | Gets the NSS element name `Abbreviation`. |
| `protected override void AddAttributes(RenderTreeBuilder builder)` | Exposes described abbreviations as accessible text without adding a keyboard focus stop.<br>`builder`. The builder receiving the render frames. |


### `Deleted`

Namespace `Nazor.Controls`. [Source](../../Sources/Core/Nazor.Controls/Text/InlineText.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed class Deleted : Span` | An inline passage removed from an earlier document version. |
| `public Deleted()` | Creates the inline control. |
| `protected override string ElementName { get; }` | Gets the NSS element name `Deleted`. |


### `Inserted`

Namespace `Nazor.Controls`. [Source](../../Sources/Core/Nazor.Controls/Text/InlineText.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed class Inserted : Span` | An inline passage added to a document. |
| `public Inserted()` | Creates the inline control. |
| `protected override string ElementName { get; }` | Gets the NSS element name `Inserted`. |


### `Subscript`

Namespace `Nazor.Controls`. [Source](../../Sources/Core/Nazor.Controls/Text/InlineText.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed class Subscript : Span` | A smaller passage lowered below the shared baseline, such as a chemical index. |
| `public Subscript()` | Creates the inline control. |
| `protected override string ElementName { get; }` | Gets the NSS element name `Subscript`. |


### `Superscript`

Namespace `Nazor.Controls`. [Source](../../Sources/Core/Nazor.Controls/Text/InlineText.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed class Superscript : Span` | A smaller passage raised above the shared baseline, such as an exponent. |
| `public Superscript()` | Creates the inline control. |
| `protected override string ElementName { get; }` | Gets the NSS element name `Superscript`. |


### `SmallText`

Namespace `Nazor.Controls`. [Source](../../Sources/Core/Nazor.Controls/Text/InlineText.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed class SmallText : Span` | A smaller passage, such as a qualification or attribution. |
| `public SmallText()` | Creates the inline control. |
| `protected override string ElementName { get; }` | Gets the NSS element name `SmallText`. |


### `Quote`

Namespace `Nazor.Controls`. [Source](../../Sources/Core/Nazor.Controls/Text/InlineText.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed class Quote : Span` | An inline quotation whose visible configurable quotation marks are included in selection, copied text and accessible text. |
| `public Quote()` | Creates the inline control. |
| `protected override string ElementName { get; }` | Gets the NSS element name `Quote`. |
| `public string OpeningQuote { get; set; }` | `[Parameter]` Initial value `“`.<br>Gets or sets the opening quotation text. |
| `public string ClosingQuote { get; set; }` | `[Parameter]` Initial value `”`.<br>Gets or sets the closing quotation text. |
| `protected override void AddContent(RenderTreeBuilder builder)` | Adds the quotation marks around the running content.<br>`builder`. The builder receiving the render frames. |


### `LineBreak`

Namespace `Nazor.Controls`. [Source](../../Sources/Core/Nazor.Controls/Text/InlineText.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed class LineBreak : Control` | A hard line break inside running text. |
| `public LineBreak()` | Creates the inline control. |
| `protected override string ElementName { get; }` | Gets the NSS element name `LineBreak`. |
| `protected override void AddAttributes(RenderTreeBuilder builder)` | Marks the element as a hard inline line break.<br>`builder`. The builder receiving the render frames. |
| `protected override void AddContent(RenderTreeBuilder builder)` | Adds the line separator to logical text.<br>`builder`. The builder receiving the render frames. |

## Reusable primitives

### `ContentTransitionKind`

Namespace `Nazor.Controls.Primitives`. [Source](../../Sources/Core/Nazor.Controls/Primitives/ContentTransition.cs)

| Declaration | Contract |
| --- | --- |
| `public enum ContentTransitionKind` | Theme-owned motion for keyed replacement. |
| `None = 0` | Immediate replacement. |
| `Entrance = 1` | Nondirectional entrance. |
| `Slide = 2` | Adjacent directional content. |
| `DrillIn = 3` | Overview to detail. |
| `DrillOut = 4` | Detail to overview. |

### `TransitionDirection`

Namespace `Nazor.Controls.Primitives`. [Source](../../Sources/Core/Nazor.Controls/Primitives/ContentTransition.cs)

| Declaration | Contract |
| --- | --- |
| `public enum TransitionDirection` | Relationship between incoming and outgoing content. |
| `Forward = 0` | Later content. |
| `Backward = 1` | Earlier content. |

### `ContentTransition`

Namespace `Nazor.Controls.Primitives`. [Source](../../Sources/Core/Nazor.Controls/Primitives/ContentTransition.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed class ContentTransition : Control, IDisposable` | Keyed replacement retaining at most one disabled, semantically hidden outgoing subtree through theme exit duration. A newer replacement drops the oldest outgoing layer; current local state is preserved for an unchanged key. |
| `public ContentTransition()` | Creates the region. |
| `public string As { get; set; }` | `[Parameter]` Element name, `ContentTransition` by default; layers are `{As}Layer`. |
| `public object? ContentKey { get; set; }` | `[Parameter]` Content identity; changed equality creates a new subtree. |
| `public RenderFragment? ChildContent { get; set; }` | `[Parameter]` Current content; capture immutable display data for the retained appearance. |
| `public ContentTransitionKind Transition { get; set; }` | `[Parameter]` Requested motion; `None` by default. |
| `public TransitionDirection Direction { get; set; }` | `[Parameter]` Requested relationship. |
| `public TimeSpan? ExitDuration { get; set; }` | `[Parameter]` Null follows NSS retention; valid non-null interval is zero to `4294967294ms`. Reduced motion forces zero. |
| `public Func<ElementReference?>? InitialFocus { get; set; }` | `[Parameter]` Resolver for hidden descendant focus; null uses the first focusable current descendant or root. External focus is unchanged. |
| `protected override string ElementName { get; }` | The chosen element name. |
| `protected override string? ElementStyle { get; }` | Clipped positioning context plus caller style. |
| `protected override void OnParametersSet()` | Validates enum, name and duration values; captures keyed changes. |
| `protected override void AddAttributes(RenderTreeBuilder builder)` | Host motion marker and programmatic focus fallback. |
| `protected override void AddContent(RenderTreeBuilder builder)` | Current-first keyed layers, with `current`, `exiting`, `transition` and `direction` attributes. Outgoing layers are absolute and noninteractive. |
| `protected override Task OnAfterRenderAsync(bool firstRender)` | Restores only focus hidden by replacement, without scrolling. |
| `public void Dispose()` | Detaches motion notifications; child Presence lifetimes cancel pending exits. |

### `LayoutTransition`

Namespace `Nazor.Controls.Primitives`. [Source](../../Sources/Core/Nazor.Controls/Primitives/LayoutTransition.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed class LayoutTransition : ContentControl` | Stable subtree whose box relocates/resizes through a sampled transform and theme `transition: transform ...`. Layout reaches its destination immediately; focus, component identity and scroll offsets remain stable. Wrapper transforms are reserved; place authored transforms inside its content. |
| `public LayoutTransition()` | Creates the wrapper. |
| `public string As { get; set; }` | `[Parameter]` Element name, `LayoutTransition` by default. |
| `public bool IsEnabled { get; set; }` | `[Parameter]` True by default. False cancels active relocation. |
| `public LayoutTransitionOrigin? From { get; set; }` | `[Parameter]` One-use connected origin in the same scene. Destination supplies content; no source pixels/component are copied. |
| `public LayoutTransitionOrigin? CaptureOrigin()` | UI-thread capture before source removal; null before layout or while hidden, empty or detached. Rotation/skew/singular transforms throw `InvalidOperationException`. |
| `protected override string ElementName { get; }` | The chosen element name. |
| `protected override string? ElementStyle { get; }` | Reserves `transform: none` and top-left origin. |
| `protected override void AddAttributes(RenderTreeBuilder builder)` | Publishes `layout-transition` and optional `layout-transition-origin`. |

### `ItemsTransition<TItem>`

Namespace `Nazor.Controls.Primitives`. [Source](../../Sources/Core/Nazor.Controls/Primitives/ItemsTransition.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed class ItemsTransition<TItem> : Control, IDisposable` | Keyed collection with stable subtrees, measured relocation and bounded noninteractive retained removals. Parts: `ItemsTransitionItem` and `ItemsTransitionContent`; the content has `entering`/`exiting` attributes. |
| `public ItemsTransition()` | Creates a column collection. |
| `public IReadOnlyList<TItem> Items { get; set; }` | `[Parameter]` Current items in display order; cannot be null. |
| `public Func<TItem, object>? ItemKey { get; set; }` | `[Parameter]` `[EditorRequired]` Stable unique non-null keys. Duplicate/null keys throw before state changes. |
| `public RenderFragment<TItem>? ItemTemplate { get; set; }` | `[Parameter]` Item content, or string representation when null. |
| `public int MaximumRetainedItems { get; set; }` | `[Parameter]` Zero to 1024; default32. Older removals are disposed first. |
| `public bool PreserveExtentDuringExit { get; set; }` | `[Parameter]` Default false. Reserves captured collection height and outgoing vertical extents as minimum height while removals remain absolutely positioned, including collection padding/borders. Prevents a bottom-anchored collection from jumping when its final normal-flow item leaves. Root measurement is enabled only while this option is true; authored Style can override the reservation. |
| `public EventCallback<int> RenderedCountChanged { get; set; }` | `[Parameter]` Reports the total current plus retained item count after the first render and subsequent count changes, on the UI thread after focus restoration. Includes zero after final exit; the existing retention bound applies. |
| `protected override string ElementName { get; }` | `ItemsTransition`. |
| `protected override string? ElementStyle { get; }` | Column positioning context plus optional outgoing minimum-height reservation and caller declarations. |
| `protected override void OnParametersSet()` | Validates keys; adopts order, revives retained identities and records removed subtrees. |
| `protected override void AddAttributes(RenderTreeBuilder builder)` | Host motion marker and empty-collection focus fallback. |
| `protected override void AddContent(RenderTreeBuilder builder)` | Stable keyed current items and outgoing items at their last measured absolute boxes. |
| `protected override Task OnAfterRenderAsync(bool firstRender)` | Restores removed descendant focus without scrolling and reports a changed current-plus-retained total after rendering. |
| `public void Dispose()` | Detaches motion notifications; child Presence lifetimes cancel exits. |

When PreserveExtentDuringExit is true, a borderless `ItemsTransitionViewport` part positions the item wrappers inside the collection padding and borders. It inherits gap and item alignment and stretches across the content width. Outgoing wrappers freeze their last current box. Default false preserves the existing direct-item structure; styles using direct-child item selectors should account for the viewport when opting in.

### `CalendarDay`

Namespace `Nazor.Controls.Primitives`. [Source](../../Sources/Core/Nazor.Controls/Primitives/CalendarModel.cs)

| Declaration | Contract |
| --- | --- |
| `public readonly struct CalendarDay : IEquatable<CalendarDay>` | One day of a `Nazor.Controls.Primitives.CalendarModel` month grid.<br>`Date`. The date.<br>`InMonth`. Whether the date belongs to the displayed month rather than to an adjacent one.<br>`Available`. Whether the date is within the limits and not excluded. |
| `public CalendarDay()` |  |
| `public CalendarDay(DateOnly Date, bool InMonth, bool Available)` | One day of a `Nazor.Controls.Primitives.CalendarModel` month grid.<br>`Date`. The date.<br>`InMonth`. Whether the date belongs to the displayed month rather than to an adjacent one.<br>`Available`. Whether the date is within the limits and not excluded. |
| `public override string ToString()` |  |
| `public static bool operator !=(CalendarDay left, CalendarDay right)` |  |
| `public static bool operator ==(CalendarDay left, CalendarDay right)` |  |
| `public override int GetHashCode()` |  |
| `public override bool Equals(object obj)` |  |
| `public bool Equals(CalendarDay other)` |  |
| `public void Deconstruct(out DateOnly Date, out bool InMonth, out bool Available)` |  |
| `public DateOnly Date { get; init; }` | The date. |
| `public bool InMonth { get; init; }` | Whether the date belongs to the displayed month rather than to an adjacent one. |
| `public bool Available { get; init; }` | Whether the date is within the limits and not excluded. |

### `CalendarModel`

Namespace `Nazor.Controls.Primitives`. [Source](../../Sources/Core/Nazor.Controls/Primitives/CalendarModel.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed class CalendarModel` | The date arithmetic behind a calendar, independent of how it is drawn: the month grid in the culture's week layout, week numbers, limits, excluded dates and keyboard movement. Calendars, date pickers and range pickers share it, and it serves custom agendas and schedulers. |
| `public bool IsAvailable(DateOnly date)` | Gets whether a date is within the limits and not excluded.<br>`date`. The date to test.<br>**Returns**. true if the date can be chosen. |
| `public DateOnly Clamp(DateOnly date)` | Clamps a date to the limits.<br>`date`. The date to clamp.<br>**Returns**. The nearest date within the limits. |
| `public int WeekNumber(DateOnly date)` | Gets the week number of a date under the culture's calendar week rule.<br>`date`. The date.<br>**Returns**. The week number. |
| `public IReadOnlyList<CalendarWeek> Month(DateOnly month)` | Builds the six-week grid that shows the month of a date, with the adjacent days that complete it.<br>`month`. Any date of the month to show.<br>**Returns**. Six weeks of seven days. |
| `public DateOnly Move(DateOnly from, CalendarStep step, int count)` | Moves a date by a number of steps and clamps it to the limits; months keep the day when possible.<br>`from`. The starting date.<br>`step`. The unit of movement.<br>`count`. The number of steps, negative to move back.<br>**Returns**. The moved and clamped date. |
| `public string ShortDayName(DayOfWeek day)` | Gets the abbreviated name of a day of the week in the culture.<br>`day`. The day of the week.<br>**Returns**. The abbreviated name. |
| `public string MonthTitle(DateOnly month)` | Gets the month and year title of a date in the culture, capitalized.<br>`month`. Any date of the month.<br>**Returns**. The title, for example "March 2026". |
| `public CalendarModel()` |  |
| `public CultureInfo Culture { get; set; }` | Gets or sets the culture that supplies the first day of the week, the week rule and the names. |
| `public DayOfWeek? FirstDayOfWeek { get; set; }` | Gets or sets the first day of the week; null uses the culture's. |
| `public DateOnly? Minimum { get; set; }` | Gets or sets the earliest available date, or null for no limit. |
| `public DateOnly? Maximum { get; set; }` | Gets or sets the latest available date, or null for no limit. |
| `public Func<DateOnly, bool>? IsExcluded { get; set; }` | Gets or sets a predicate that excludes individual dates, such as holidays. |
| `public DayOfWeek WeekStart { get; }` | Gets the first day of the week in effect. |
| `public IReadOnlyList<DayOfWeek> Weekdays { get; }` | Gets the seven days of the week in display order. |

### `CalendarStep`

Namespace `Nazor.Controls.Primitives`. [Source](../../Sources/Core/Nazor.Controls/Primitives/CalendarModel.cs)

| Declaration | Contract |
| --- | --- |
| `public enum CalendarStep` | The unit of a `Nazor.Controls.Primitives.CalendarModel.Move(System.DateOnly,Nazor.Controls.Primitives.CalendarStep,System.Int32)` step. |
| `Day = 0` | One day, as the left and right arrows. |
| `Week = 1` | One week, as the up and down arrows. |
| `Month = 2` | One month, as Page Up and Page Down. |
| `Year = 3` | One year, as Shift with Page Up and Page Down. |

### `CalendarWeek`

Namespace `Nazor.Controls.Primitives`. [Source](../../Sources/Core/Nazor.Controls/Primitives/CalendarModel.cs)

| Declaration | Contract |
| --- | --- |
| `public readonly struct CalendarWeek : IEquatable<CalendarWeek>` | One row of a `Nazor.Controls.Primitives.CalendarModel` month grid.<br>`Number`. The week number of the culture's calendar rule.<br>`Days`. The seven days of the week, starting from the culture's first day of the week. |
| `public CalendarWeek()` |  |
| `public CalendarWeek(int Number, IReadOnlyList<CalendarDay> Days)` | One row of a `Nazor.Controls.Primitives.CalendarModel` month grid.<br>`Number`. The week number of the culture's calendar rule.<br>`Days`. The seven days of the week, starting from the culture's first day of the week. |
| `public override string ToString()` |  |
| `public static bool operator !=(CalendarWeek left, CalendarWeek right)` |  |
| `public static bool operator ==(CalendarWeek left, CalendarWeek right)` |  |
| `public override int GetHashCode()` |  |
| `public override bool Equals(object obj)` |  |
| `public bool Equals(CalendarWeek other)` |  |
| `public void Deconstruct(out int Number, out IReadOnlyList<CalendarDay> Days)` |  |
| `public int Number { get; init; }` | The week number of the culture's calendar rule. |
| `public IReadOnlyList<CalendarDay> Days { get; init; }` | The seven days of the week, starting from the culture's first day of the week. |

### `Collapsible`

Namespace `Nazor.Controls.Primitives`. [Source](../../Sources/Core/Nazor.Controls/Primitives/Disclosure.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed class Collapsible : ContentControl, IDisposable` | Measured disclosure with clipped viewport and naturally measured `{As}Content`. Root states: `expanded`, `exiting`, `closed`, `measuring`; content state: `revealed`. Closed content is disabled and semantically hidden. |
| `public Collapsible()` | Creates an initially closed region. |
| `public string As { get; set; }` | `[Parameter]` Element name, `Collapsible` by default. |
| `public bool IsOpen { get; set; }` | `[Parameter]` Open state. |
| `public EventCallback<bool> IsOpenChanged { get; set; }` | `[Parameter]` Internal open-state changes. |
| `public bool KeepMounted { get; set; }` | `[Parameter]` False disposes content after exit. True keeps the same disabled/semantically hidden subtree after the close phase for local state and natural measurement. |
| `public ElementReference? ReturnFocus { get; set; }` | `[Parameter]` Focus target when closing hides focused content; null leaves ordinary removal cleanup in charge. |
| `public bool Disabled { get; set; }` | `[Parameter]` Rejects toggling. |
| `public bool Opened { get; }` | Current open state. |
| `public string ContentId { get; }` | Content identifier for controls relationships. |
| `protected override string ElementName { get; }` | Chosen element name. |
| `protected override string? ElementStyle { get; }` | Publishes `--nz-collapsible-height`; inline height uses `var(--nz-collapsible-viewport, measured-open-or-zero)` with overflow clipping, followed by caller style. |
| `protected override Task OnParametersSetAsync()` | Synchronizes state, arms first reveal and restores hidden focus. |
| `protected override void AddAttributes(RenderTreeBuilder builder)` | Publishes disclosure states, host motion marker and viewport measurement. |
| `protected override void AddContent(RenderTreeBuilder builder)` | Same naturally measured child while open, exiting or kept mounted; no second timer. |
| `public Task ToggleAsync()` | Toggles unless disabled. |
| `public Task SetOpenAsync(bool value)` | Changes state and reports it; restores only hidden descendant focus, without scrolling. |
| `public void Dispose()` | Detaches host motion notifications. |

Set base `overlay-exit-duration` for closing retention. Zero/reduced motion completes closing immediately. A natural-height change during closing retargets movement and restarts the same bounded lifetime; its configured retention interval begins again after the last height change. Viewport measurement arms initial reveal after natural height has been published. Keep padding/borders on `{As}Content` so the measured border box matches its clip.

### `ColorArea`

Namespace `Nazor.Controls.Primitives`. [Source](../../Sources/Core/Nazor.Controls/Primitives/ColorArea.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed class ColorArea : Control` | Rectangular HSV editor; `Components` selects six horizontal/vertical axis permutations. The original SaturationValue plane is unchanged. The remaining channel and alpha are preserved. Arrows move one percent or one hue degree along the screen axes (ten with Shift); Page Up/Down move ten percent or 30 hue degrees vertically. Home/End choose horizontal endpoints; Ctrl chooses vertical endpoints. Horizontal channel range and both-axis value text provide accessibility semantics. Themes read the complete `--nz-color-area-background` plus existing hue/color/x/y variables. Hue/saturation planes display full brightness while preserving the actual value. Lost capture cancels without completion. |
| `protected override void OnParametersSet()` | Validates the component enum and element name, synchronizes color and clears disabled drag state. |
| `protected override void AddAttributes(RenderTreeBuilder builder)` | Adds the slider semantics, the variables and the pointer and keyboard handling.<br>`builder`. The builder that receives the render frames. |
| `protected override void AddContent(RenderTreeBuilder builder)` | Adds the thumb.<br>`builder`. The builder that receives the render frames. |
| `public static (double Saturation, double Value) MapPoint(double x, double y, (double X, double Y, double Width, double Height) bounds)` | Maps a point to saturation and value within a box, clamping it to the box.<br>`x`. The horizontal position.<br>`y`. The vertical position.<br>`bounds`. The box of the area.<br>**Returns**. The saturation and the value, each from 0 to 1. |
| `public ColorArea()` |  |
| `public string As { get; set; }` | `[Parameter]` Initial value `"ColorArea"`.<br>Gets or sets the element name, `ColorArea` by default. |
| `public ColorSpectrumComponents Components { get; set; }` | `[Parameter]` Selected axes; SaturationValue by default. |
| `public HsvColor Color { get; set; }` | `[Parameter]` Current HSV color; the selected axes change while the remaining channel and opacity are preserved. |
| `public EventCallback<HsvColor> ColorChanged { get; set; }` | `[Parameter]`<br>Gets or sets the callback invoked on every change, while dragging too. |
| `public EventCallback<HsvColor> OnChange { get; set; }` | `[Parameter]`<br>Gets or sets the callback invoked with the final color of a drag or a key press. |
| `public bool Disabled { get; set; }` | `[Parameter]`<br>Gets or sets whether the area rejects interaction. |
| `public HsvColor CurrentColor { get; }` | Gets the current color. |
| `public bool IsDragging { get; }` | Gets whether a pointer drag is in progress. |
| `protected override string ElementName { get; }` | Gets the element name. |
| `protected override string? ElementStyle { get; }` | Gets the position and color variables and the caller's declarations. |
| `public static HsvColor MapPoint(HsvColor color, ColorSpectrumComponents components, double x, double y, (double X, double Y, double Width, double Height) bounds)` | Maps/clamps the selected channels while preserving the others. A zero-sized axis sets its selected channel to zero. Undefined combinations throw ArgumentOutOfRangeException. |

### `ColorSpectrumComponents`

Namespace `Nazor.Controls.Primitives`. [Source](../../Sources/Core/Nazor.Controls/Primitives/ColorSpectrumComponents.cs)

| Declaration | Contract |
| --- | --- |
| `public enum ColorSpectrumComponents` | Horizontal/vertical channels and their screen directions. |
| `SaturationValue = 0` | Saturation right; value up. Original ColorArea behavior. |
| `HueSaturation = 1` | Hue right; saturation up. Full-brightness display. |
| `HueValue = 2` | Hue right; value up. |
| `SaturationHue = 3` | Saturation left; hue down. Full-brightness display. |
| `ValueHue = 4` | Value left; hue down. |
| `ValueSaturation = 5` | Value left; saturation down. |

### `ColorChannel`

Namespace `Nazor.Controls.Primitives`. [Source](../../Sources/Core/Nazor.Controls/Primitives/ColorArea.cs)

| Declaration | Contract |
| --- | --- |
| `public enum ColorChannel` | A component of a color edited by a `Nazor.Controls.Primitives.ColorSlider`, written as its `channel` attribute. |
| `Hue = 0` | The hue, from 0 to 360 degrees. |
| `Saturation = 1` | The HSV saturation, from 0 to 100 percent. |
| `Value = 2` | The HSV value (brightness), from 0 to 100 percent. |
| `Lightness = 3` | The HSL lightness, from 0 to 100 percent. |
| `Red = 4` | The red channel, from 0 to 255. |
| `Green = 5` | The green channel, from 0 to 255. |
| `Blue = 6` | The blue channel, from 0 to 255. |
| `Alpha = 7` | The opacity, from 0 to 100 percent. |

### `ColorModel`

Namespace `Nazor.Controls.Primitives`. [Source](../../Sources/Core/Nazor.Controls/Primitives/ColorModel.cs)

| Declaration | Contract |
| --- | --- |
| `public static class ColorModel` | The color arithmetic behind color pickers, independent of how they are drawn: conversions between RGB (`Nazor.Style.Color`), `Nazor.Controls.Primitives.HsvColor` and `Nazor.Controls.Primitives.HslColor`, hexadecimal parsing and formatting with alpha, clamping and contrast. Conversions to RGB round each channel to the nearest byte. |
| `public static HsvColor ToHsv(Color color)` | Converts an RGB color to hue, saturation and value; gray colors get hue zero.<br>`color`. The color to convert.<br>**Returns**. The equivalent HSV color. |
| `public static Color FromHsv(HsvColor color)` | Converts a hue, saturation and value color to RGB, clamping its components first.<br>`color`. The color to convert.<br>**Returns**. The nearest RGB color. |
| `public static HslColor ToHsl(Color color)` | Converts an RGB color to hue, saturation and lightness; gray colors get hue zero.<br>`color`. The color to convert.<br>**Returns**. The equivalent HSL color. |
| `public static Color FromHsl(HslColor color)` | Converts a hue, saturation and lightness color to RGB, clamping its components first.<br>`color`. The color to convert.<br>**Returns**. The nearest RGB color. |
| `public static HsvColor ToHsv(HslColor color)` | Converts between the two cylindrical models without going through bytes, so the hue is kept exactly.<br>`color`. The HSL color.<br>**Returns**. The equivalent HSV color. |
| `public static HslColor ToHsl(HsvColor color)` | Converts between the two cylindrical models without going through bytes, so the hue is kept exactly.<br>`color`. The HSV color.<br>**Returns**. The equivalent HSL color. |
| `public static bool TryParseHex(string? text, out Color color)` | Parses a hexadecimal color: `#rgb`, `#rgba`, `#rrggbb` or `#rrggbbaa`, with or without the leading `#`, ignoring surrounding white space and case.<br>`text`. The text to parse.<br>`color`. The parsed color, or transparent black when parsing fails.<br>**Returns**. true if the text is a hexadecimal color. |
| `public static Color ParseHex(string text)` | Parses a hexadecimal color as `Nazor.Controls.Primitives.ColorModel.TryParseHex(System.String,Nazor.Style.Color@)` does.<br>`text`. The text to parse.<br>**Returns**. The parsed color.<br>**Throws** `System.FormatException`. The text is not a hexadecimal color. |
| `public static string ToHex(Color color, bool? alpha = null)` | Formats a color as lowercase hexadecimal `#rrggbb`, or `#rrggbbaa` when it is translucent.<br>`color`. The color to format.<br>`alpha`. true to always include the alpha byte, false to never include it, null to include it only when it is not opaque.<br>**Returns**. The hexadecimal text. |
| `public static string ToCss(Color color)` | Formats a color as a CSS `rgb()` or `rgba()` value, usable in NSS declarations.<br>`color`. The color to format.<br>**Returns**. The CSS text. |
| `public static Color WithAlpha(Color color, double alpha)` | Returns the color with another opacity.<br>`color`. The color.<br>`alpha`. The opacity, from 0 to 1; it is clamped.<br>**Returns**. The color with the new alpha byte. |
| `public static double Luminance(Color color)` | Computes the relative luminance of a color as defined by WCAG, ignoring alpha.<br>`color`. The color.<br>**Returns**. The luminance, from 0 for black to 1 for white. |
| `public static double Contrast(Color first, Color second)` | Computes the WCAG contrast ratio between two colors, ignoring alpha.<br>`first`. The first color.<br>`second`. The second color.<br>**Returns**. The ratio, from 1 to 21. |
| `public static double Unit(double value)` | Clamps a number into [0, 1]; non-finite numbers become zero.<br>`value`. The number.<br>**Returns**. The clamped number. |
| `public static double WrapHue(double hue)` | Wraps a hue into [0, 360); non-finite hues become zero.<br>`hue`. The hue in degrees.<br>**Returns**. The wrapped hue. |

### `ColorSlider`

Namespace `Nazor.Controls.Primitives`. [Source](../../Sources/Core/Nazor.Controls/Primitives/ColorArea.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed class ColorSlider : Control` | A one-dimensional slider over one `Nazor.Controls.Primitives.ColorSlider.Channel` of `Nazor.Controls.Primitives.ColorSlider.Color` (`@bind-Color`): hue, HSV saturation or value, HSL lightness, red, green, blue or opacity. Pointer and keyboard come from the engine's slider (arrows, Page Up and Page Down, Home and End). The element (named by `Nazor.Controls.Primitives.ColorSlider.As`) has the `slider` role and the `channel` and `orientation` attributes; its parts are `{name}Track` and `{name}Thumb`. Themes draw the track from the variables `--nz-color-slider-gradient` (a complete `linear-gradient()` of the channel along the slider), `--nz-color-slider-start` and `--nz-color-slider-end` (the colors at the ends), `--nz-color-slider-color` (the current color) and `--nz-color-slider-position` (the thumb position as a percentage along the track). |
| `public static (double Minimum, double Maximum) RangeOf(ColorChannel channel)` | Gets the lower and upper bounds of a channel as shown to the user.<br>`channel`. The channel.<br>**Returns**. The minimum and the maximum. |
| `public static double Read(HsvColor color, ColorChannel channel)` | Reads a channel of a color in the units of `Nazor.Controls.Primitives.ColorSlider.RangeOf(Nazor.Controls.Primitives.ColorChannel)`.<br>`color`. The color.<br>`channel`. The channel.<br>**Returns**. The channel value. |
| `public static HsvColor Write(HsvColor color, ColorChannel channel, double value)` | Writes a channel of a color in the units of `Nazor.Controls.Primitives.ColorSlider.RangeOf(Nazor.Controls.Primitives.ColorChannel)`, clamped; the hue is kept when an RGB change makes the color gray.<br>`color`. The color.<br>`channel`. The channel.<br>`value`. The new channel value.<br>**Returns**. The changed color. |
| `protected override void OnParametersSet()` | Validates the parameters and synchronizes the color. |
| `protected override void AddAttributes(RenderTreeBuilder builder)` | Adds the slider semantics and the change handling.<br>`builder`. The builder that receives the render frames. |
| `protected override void AddContent(RenderTreeBuilder builder)` | Adds the track and the thumb.<br>`builder`. The builder that receives the render frames. |
| `public ColorSlider()` |  |
| `public string As { get; set; }` | `[Parameter]` Initial value `"ColorSlider"`.<br>Gets or sets the element name, `ColorSlider` by default. |
| `public ColorChannel Channel { get; set; }` | `[Parameter]`<br>Gets or sets the channel the slider edits. |
| `public HsvColor Color { get; set; }` | `[Parameter]` Initial value `new(0, 1, 1)`.<br>Gets or sets the edited color. |
| `public EventCallback<HsvColor> ColorChanged { get; set; }` | `[Parameter]`<br>Gets or sets the callback invoked on every change, while dragging too. |
| `public EventCallback<HsvColor> OnChange { get; set; }` | `[Parameter]`<br>Gets or sets the callback invoked with the final color of a drag or a key press. |
| `public Orientation Orientation { get; set; }` | `[Parameter]`<br>Gets or sets the direction of the track; a vertical slider grows upwards. |
| `public bool Disabled { get; set; }` | `[Parameter]`<br>Gets or sets whether the slider rejects interaction. |
| `public HsvColor CurrentColor { get; }` | Gets the current color. |
| `protected override string ElementName { get; }` | Gets the element name. |
| `protected override string? ElementStyle { get; }` | Gets the variables of the track and the caller's declarations. |

### `Combobox<TItem>`

Namespace `Nazor.Controls.Primitives`. [Source](../../Sources/Core/Nazor.Controls/Primitives/Combobox.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed class Combobox<TItem> : Control` | A text editor with a list of suggestions that the user moves through while the focus stays in the text: typing changes `Nazor.Controls.Primitives.Combobox<TItem>.Text` (`@bind-Text`) and opens the list, Up and Down move the current suggestion, Enter or a click chooses it (`Nazor.Controls.Primitives.Combobox<TItem>.OnSelect`), Escape closes the list and then clears the text. Arrow navigation stops at the ends and scrolls the current suggestion into view. The field frame forwards primary pointer presses to the editor, including padding and decorative content. The caller filters `Nazor.Controls.Primitives.Combobox<TItem>.Items` from the text. Parts: the editor (`{name}Input`), optional `Nazor.Controls.Primitives.Combobox<TItem>.Leading` and `Nazor.Controls.Primitives.Combobox<TItem>.Trailing` content, the popup (`{name}Popup`), its options (`{name}Option`, with `current`) and `{name}Empty`. Combo boxes, searches with suggestions, token fields and command palettes are built on it.<br>`TItem`. The type of the suggestions. |
| `protected override void OnParametersSet()` | Synchronizes the text and the open state. |
| `protected override void AddAttributes(RenderTreeBuilder builder)` | Adds the expanded and disabled states and forwards frame presses to the editor.<br>`builder`. The builder that receives the render frames. |
| `protected override void AddContent(RenderTreeBuilder builder)` | Adds the editor, the side content and the popup.<br>`builder`. The builder that receives the render frames. |
| `protected override void OnAfterRender(bool firstRender)` | Reveals a keyboard-highlighted suggestion after rendering without moving editor focus.<br>`firstRender`. Whether this is the first render. |
| `public Task SetTextAsync(string value)` | Replaces the text of the editor, even when the user has typed something the owner rejects, and reports it through `Nazor.Controls.Primitives.Combobox<TItem>.TextChanged` without opening the list.<br>`value`. The new text.<br>**Returns**. A task that completes when the change has been reported. |
| `public Task ChooseAsync(int index)` | Chooses the suggestion at an index as the user would.<br>`index`. The index in `Nazor.Controls.Primitives.Combobox<TItem>.Items`.<br>**Returns**. A task that completes when the choice has been reported. |
| `public Task SetOpenAsync(bool value)` | Opens or closes the list.<br>`value`. Whether the list should be open.<br>**Returns**. A task that completes when the change has been reported. |
| `public Combobox()` |  |
| `public string As { get; set; }` | `[Parameter]` Initial value `"Combobox"`.<br>Gets or sets the element name, `Combobox` by default. |
| `public string? Text { get; set; }` | `[Parameter]`<br>Gets or sets the text of the editor. |
| `public EventCallback<string> TextChanged { get; set; }` | `[Parameter]`<br>Gets or sets the callback invoked on every edit. |
| `public IReadOnlyList<TItem> Items { get; set; }` | `[Parameter]` Initial value `[]`.<br>Gets or sets the suggestions currently offered. |
| `public Func<TItem, string>? ItemText { get; set; }` | `[Parameter]`<br>Gets or sets the text of a suggestion, used when `Nazor.Controls.Primitives.Combobox<TItem>.ItemTemplate` is null and when one is chosen. |
| `public RenderFragment<TItem>? ItemTemplate { get; set; }` | `[Parameter]`<br>Gets or sets how each suggestion is rendered. |
| `public Func<TItem, bool>? IsItemSelected { get; set; }` | `[Parameter]` Identifies committed selected suggestions independently of current hover/keyboard state. Null selects the current suggestion. |
| `public RenderFragment? EmptyContent { get; set; }` | `[Parameter]`<br>Gets or sets content shown when there are no suggestions and the list is open. |
| `public EventCallback<TItem> OnSelect { get; set; }` | `[Parameter]`<br>Gets or sets the callback invoked with the chosen suggestion. |
| `public bool FillOnSelect { get; set; }` | `[Parameter]` Initial value `true`.<br>Gets or sets whether choosing a suggestion copies its text into the editor. |
| `public bool IsOpen { get; set; }` | `[Parameter]`<br>Gets or sets whether the list is open. |
| `public EventCallback<bool> IsOpenChanged { get; set; }` | `[Parameter]`<br>Gets or sets the callback invoked when the list opens or closes. |
| `public bool OpenOnFocus { get; set; }` | `[Parameter]`<br>Gets or sets whether the list opens when the editor receives the focus. |
| `public DropDownPlacement DropDownPlacement { get; set; }` | `[Parameter]`<br>Gets or sets the preferred position of an anchored suggestion list relative to the editor. |
| `public int? PreferredMinVisibleItems { get; set; }` | `[Parameter]`<br>Gets or sets the preferred minimum number of visible suggestions, or null for no minimum preference. |
| `public int? PreferredMaxVisibleItems { get; set; }` | `[Parameter]`<br>Gets or sets the preferred maximum number of visible suggestions, or null to use the styled size. |
| `public string? Placeholder { get; set; }` | `[Parameter]`<br>Gets or sets the hint shown while the text is empty. |
| `public bool Disabled { get; set; }` | `[Parameter]`<br>Gets or sets whether the combo box rejects interaction. |
| `public bool AutoFocus { get; set; }` | `[Parameter]`<br>Gets or sets whether the editor takes the focus when it appears. |
| `public RenderFragment? Leading { get; set; }` | `[Parameter]`<br>Gets or sets content shown before the editor, such as a search mark. |
| `public RenderFragment? InputAdornment { get; set; }` | `[Parameter]`<br>Content rendered directly beside the editor, outside the Leading/Trailing wrappers. Themes can order it independently; use it for a search mark while scopes and clear actions retain their positions. |
| `public RenderFragment? Trailing { get; set; }` | `[Parameter]`<br>Gets or sets content shown after the editor, such as a chevron or a clear button. |
| `public bool Inline { get; set; }` | `[Parameter]`<br>Gets or sets whether the popup is rendered inline below the editor rather than as an anchored overlay. |
| `public IReadOnlyDictionary<string, object?>? InputAttributes { get; set; }` | `[Parameter]`<br>Gets or sets extra parameters and attributes of the editor, such as `ReadOnly`, `Required`, `Invalid` or a tagged internal event handler; they must not repeat the parameters the combo box sets itself (the text, the role, the placeholder, the disabled state, the naming and the focus behavior). |
| `public FieldContext? Field { get; set; }` | `[CascadingParameter]`<br>Gets or sets the enclosing field, supplied automatically. |
| `public int CurrentIndex { get; }` | Gets the index of the current suggestion, or -1. |
| `public bool Opened { get; }` | Gets whether the list is open. |
| `protected override string ElementName { get; }` | Gets the element name. |

The anchored and inline suggestion lists preserve editor focus on pointer presses, unless custom suggestion content explicitly changes it. Modified Enter does not choose a suggestion; unmodified Enter does. Existing Alt+Down and Alt+Up retain their explicit open/close behavior.

### `Command`

Namespace `Nazor.Controls.Primitives`. [Source](../../Sources/Core/Nazor.Controls/Primitives/Commands.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed class Command` | An action that several controls present together: a button, a menu item, a toolbar item and a command palette entry can show the same text, shortcut and enabled state, and run the same work. Change a property and every control that shows the command updates; `Nazor.Controls.Primitives.ShortcutScope` makes its `Nazor.Controls.Primitives.Command.Gesture` work from the keyboard. Every member must be used from the UI thread. |
| `public Command(string text, Func<Task> execute, KeyGesture? gesture = null)` | Creates a command.<br>`text`. The text shown by controls.<br>`execute`. The work run when the command is invoked.<br>`gesture`. The optional keyboard shortcut. |
| `public Command(string text, Action execute, KeyGesture? gesture = null)` | Creates a command that runs synchronous work.<br>`text`. The text shown by controls.<br>`execute`. The work run when the command is invoked.<br>`gesture`. The optional keyboard shortcut. |
| `public Task ExecuteAsync()` | Runs the command when it is enabled.<br>**Returns**. A task that completes when the work has finished. |
| `public Func<Task> Execute { get; }` | Gets the work run by `Nazor.Controls.Primitives.Command.ExecuteAsync`. |
| `public string Text { get; set; }` | Gets or sets the text shown by controls. |
| `public string? Description { get; set; }` | Gets or sets a longer description, shown as a tooltip or in a command palette. |
| `public KeyGesture? Gesture { get; set; }` | Gets or sets the keyboard shortcut. |
| `public bool IsEnabled { get; set; }` | Gets or sets whether the command can run; controls showing it are disabled otherwise. |
| `public bool? IsChecked { get; set; }` | Gets or sets the checked state of a toggle command, or null for an ordinary command. |
| `public event Action? Changed` | Raised after a property changes. |

### `ControllableState<T>`

Namespace `Nazor.Controls.Primitives`. [Source](../../Sources/Core/Nazor.Controls/Primitives/ControllableState.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed class ControllableState<T>` | A value that a component owns locally but that its parent can also control through a parameter, the pattern behind every `@bind-` parameter of the built-in controls. Call `Nazor.Controls.Primitives.ControllableState<T>.Sync(`0)` from `OnParametersSet`: a parameter that changed since the previous call replaces the local value, an unchanged one keeps the value the user chose. Call `Nazor.Controls.Primitives.ControllableState<T>.Set(`0)` when the user changes the value, then report it through the matching callback.<br>`T`. The type of the value.<br>`initial`. The value before the first `Nazor.Controls.Primitives.ControllableState<T>.Sync(`0)`. |
| `public ControllableState(T initial)` | A value that a component owns locally but that its parent can also control through a parameter, the pattern behind every `@bind-` parameter of the built-in controls. Call `Nazor.Controls.Primitives.ControllableState<T>.Sync(`0)` from `OnParametersSet`: a parameter that changed since the previous call replaces the local value, an unchanged one keeps the value the user chose. Call `Nazor.Controls.Primitives.ControllableState<T>.Set(`0)` when the user changes the value, then report it through the matching callback.<br>`T`. The type of the value.<br>`initial`. The value before the first `Nazor.Controls.Primitives.ControllableState<T>.Sync(`0)`. |
| `public bool Sync(T parameter)` | Adopts the parameter value when it differs from the one received at the previous call.<br>`parameter`. The current value of the component parameter.<br>**Returns**. true if the local value changed. |
| `public bool Set(T value)` | Replaces the local value after a user action.<br>`value`. The new value.<br>**Returns**. true if the value changed and should be reported. |
| `public T Value { get; }` | Gets the current value, chosen locally or received from the parameter. |

### `DataGroup<TItem>`

Namespace `Nazor.Controls.Primitives`. [Source](../../Sources/Core/Nazor.Controls/Primitives/DataView.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed class DataGroup<TItem>` | A group of items of a `Nazor.Controls.Primitives.DataView<TItem>`, formed by `Nazor.Controls.Primitives.DataView<TItem>.GroupKey`.<br>`TItem`. The type of the items. |
| `public object? Key { get; }` | Gets the key shared by the items of the group. |
| `public IReadOnlyList<TItem> Items { get; }` | Gets filtered/sorted group items across all local pages, or within the accepted provider window. Remote membership beyond that window is unknown. |
| `public int Count { get; }` | Gets the number of known group items; provider groups cover the accepted window only. |
| `public bool IsExpanded { get; }` | Gets whether the items of the group are shown. |

### `DataPage<TItem>`

Namespace `Nazor.Controls.Primitives`. [Source](../../Sources/Core/Nazor.Controls/Primitives/DataView.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed record DataPage<TItem> : IEquatable<DataPage<TItem>>` | A window of items returned by a data provider of a `Nazor.Controls.Primitives.DataView<TItem>`.<br>`TItem`. The type of the items.<br>`Items`. The requested global filtered/sorted window starting at DataRequest.Skip; membership is copied when accepted.<br>`TotalCount`. The complete count before paging. Nonempty windows must fit this count and the requested Take. |
| `public DataPage(IReadOnlyList<TItem> Items, int TotalCount)` | A window of items returned by a data provider of a `Nazor.Controls.Primitives.DataView<TItem>`.<br>`TItem`. The type of the items.<br>`Items`. The requested global filtered/sorted window starting at DataRequest.Skip; membership is copied when accepted.<br>`TotalCount`. The complete count before paging. Nonempty windows must fit this count and the requested Take. |
| `public override string ToString()` |  |
| `public static bool operator !=(DataPage<TItem>? left, DataPage<TItem>? right)` |  |
| `public static bool operator ==(DataPage<TItem>? left, DataPage<TItem>? right)` |  |
| `public override int GetHashCode()` |  |
| `public override bool Equals(object? obj)` |  |
| `public bool Equals(DataPage<TItem>? other)` |  |
| `public void Deconstruct(out IReadOnlyList<TItem> Items, out int TotalCount)` |  |
| `public IReadOnlyList<TItem> Items { get; init; }` | The requested global filtered/sorted window starting at DataRequest.Skip; membership is copied when accepted. |
| `public int TotalCount { get; init; }` | The complete count before paging. Nonempty windows must fit this count and the requested Take. |

### `DataRequest`

Namespace `Nazor.Controls.Primitives`. [Source](../../Sources/Core/Nazor.Controls/Primitives/DataView.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed record DataRequest : IEquatable<DataRequest>` | What a data provider of a `Nazor.Controls.Primitives.DataView<TItem>` is asked for: a window of the sorted, filtered items.<br>`Skip`. The number of items to skip.<br>`Take`. The number of items wanted, or null for all the remaining items.<br>`Sorts`. The sorts, the most significant first.<br>`Query`. The application-defined filter, such as a search text, from `Nazor.Controls.Primitives.DataView<TItem>.Query`.<br>`CancellationToken`. Canceled when a newer load replaces this one or request settings change. |
| `public DataRequest(int Skip, int? Take, IReadOnlyList<DataSort> Sorts, object? Query, CancellationToken CancellationToken)` | What a data provider of a `Nazor.Controls.Primitives.DataView<TItem>` is asked for: a window of the sorted, filtered items.<br>`Skip`. The number of items to skip.<br>`Take`. The number of items wanted, or null for all the remaining items.<br>`Sorts`. The sorts, the most significant first.<br>`Query`. The application-defined filter, such as a search text, from `Nazor.Controls.Primitives.DataView<TItem>.Query`.<br>`CancellationToken`. Canceled when a newer load replaces this one or request settings change. |
| `public override string ToString()` |  |
| `public static bool operator !=(DataRequest? left, DataRequest? right)` |  |
| `public static bool operator ==(DataRequest? left, DataRequest? right)` |  |
| `public override int GetHashCode()` |  |
| `public override bool Equals(object? obj)` |  |
| `public bool Equals(DataRequest? other)` |  |
| `public void Deconstruct(out int Skip, out int? Take, out IReadOnlyList<DataSort> Sorts, out object? Query, out CancellationToken CancellationToken)` |  |
| `public int Skip { get; init; }` | The number of items to skip. |
| `public int? Take { get; init; }` | The number of items wanted, or null for all the remaining items. |
| `public IReadOnlyList<DataSort> Sorts { get; init; }` | The sorts, the most significant first. |
| `public object? Query { get; init; }` | The application-defined filter, such as a search text, from `Nazor.Controls.Primitives.DataView<TItem>.Query`. |
| `public CancellationToken CancellationToken { get; init; }` | Canceled when a newer load replaces this one or request settings change. |

### `DataRow<TItem>`

Namespace `Nazor.Controls.Primitives`. [Source](../../Sources/Core/Nazor.Controls/Primitives/DataView.cs)

| Declaration | Contract |
| --- | --- |
| `public readonly struct DataRow<TItem> : IEquatable<DataRow<TItem>>` | A row of the flat projection of a `Nazor.Controls.Primitives.DataView<TItem>`: an item or a group header.<br>`TItem`. The type of the items.<br>`Kind`. Whether the row is an item or a group header.<br>`Item`. The item, or default for a group header.<br>`Index`. The index of the item in `Nazor.Controls.Primitives.DataView<TItem>.FilteredItems`, or -1 for a group header.<br>`Group`. The group of the row, or null without grouping. |
| `public DataRow()` |  |
| `public DataRow(DataRowKind Kind, TItem Item, int Index, DataGroup<TItem>? Group)` | A row of the flat projection of a `Nazor.Controls.Primitives.DataView<TItem>`: an item or a group header.<br>`TItem`. The type of the items.<br>`Kind`. Whether the row is an item or a group header.<br>`Item`. The item, or default for a group header.<br>`Index`. The index of the item in `Nazor.Controls.Primitives.DataView<TItem>.FilteredItems`, or -1 for a group header.<br>`Group`. The group of the row, or null without grouping. |
| `public override string ToString()` |  |
| `public static bool operator !=(DataRow<TItem> left, DataRow<TItem> right)` |  |
| `public static bool operator ==(DataRow<TItem> left, DataRow<TItem> right)` |  |
| `public override int GetHashCode()` |  |
| `public override bool Equals(object obj)` |  |
| `public bool Equals(DataRow<TItem> other)` |  |
| `public void Deconstruct(out DataRowKind Kind, out TItem Item, out int Index, out DataGroup<TItem>? Group)` |  |
| `public DataRowKind Kind { get; init; }` | Whether the row is an item or a group header. |
| `public TItem Item { get; init; }` | The item, or default for a group header. |
| `public int Index { get; init; }` | The index of the item in `Nazor.Controls.Primitives.DataView<TItem>.FilteredItems`, or -1 for a group header. |
| `public LogicalItemPosition ItemPosition { get; init; }` | Immutable global position/count from the accepted local/provider result; provider grouping retains response ordinals. Group headers have the invalid default value. Index keeps its FilteredItems meaning. |
| `public DataGroup<TItem>? Group { get; init; }` | The group of the row, or null without grouping. |

DataRow value equality includes ItemPosition. Manually constructed rows have invalid default position metadata unless explicitly initialized.

### `DataRowKind`

Namespace `Nazor.Controls.Primitives`. [Source](../../Sources/Core/Nazor.Controls/Primitives/DataView.cs)

| Declaration | Contract |
| --- | --- |
| `public enum DataRowKind` | The kind of a row projected by a `Nazor.Controls.Primitives.DataView<TItem>`. |
| `Item = 0` | A row that shows an item. |
| `GroupHeader = 1` | The header of a group, before its items. |

### `DataSort`

Namespace `Nazor.Controls.Primitives`. [Source](../../Sources/Core/Nazor.Controls/Primitives/DataView.cs)

| Declaration | Contract |
| --- | --- |
| `public readonly struct DataSort : IEquatable<DataSort>` | A sort applied to a `Nazor.Controls.Primitives.DataView<TItem>`, by key and direction, as sent to a data provider.<br>`Key`. The key of the sorted field or column.<br>`Direction`. The direction of the sort. |
| `public DataSort()` |  |
| `public DataSort(string Key, ListSortDirection Direction)` | A sort applied to a `Nazor.Controls.Primitives.DataView<TItem>`, by key and direction, as sent to a data provider.<br>`Key`. The key of the sorted field or column.<br>`Direction`. The direction of the sort. |
| `public override string ToString()` |  |
| `public static bool operator !=(DataSort left, DataSort right)` |  |
| `public static bool operator ==(DataSort left, DataSort right)` |  |
| `public override int GetHashCode()` |  |
| `public override bool Equals(object obj)` |  |
| `public bool Equals(DataSort other)` |  |
| `public void Deconstruct(out string Key, out ListSortDirection Direction)` |  |
| `public string Key { get; init; }` | The key of the sorted field or column. |
| `public ListSortDirection Direction { get; init; }` | The direction of the sort. |

### `DataSortDescriptor<TItem>`

Namespace `Nazor.Controls.Primitives`. [Source](../../Sources/Core/Nazor.Controls/Primitives/DataView.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed record DataSortDescriptor<TItem> : IEquatable<DataSortDescriptor<TItem>>` | A sort of a `Nazor.Controls.Primitives.DataView<TItem>` with the function that reads the sorted value of an item.<br>`TItem`. The type of the items.<br>`Key`. The key of the sorted field or column.<br>`Selector`. Reads the value compared for an item.<br>`Direction`. The direction of the sort. |
| `public DataSortDescriptor(string Key, Func<TItem, object?> Selector, ListSortDirection Direction = ListSortDirection.Ascending)` | A sort of a `Nazor.Controls.Primitives.DataView<TItem>` with the function that reads the sorted value of an item.<br>`TItem`. The type of the items.<br>`Key`. The key of the sorted field or column.<br>`Selector`. Reads the value compared for an item.<br>`Direction`. The direction of the sort. |
| `public override string ToString()` |  |
| `public static bool operator !=(DataSortDescriptor<TItem>? left, DataSortDescriptor<TItem>? right)` |  |
| `public static bool operator ==(DataSortDescriptor<TItem>? left, DataSortDescriptor<TItem>? right)` |  |
| `public override int GetHashCode()` |  |
| `public override bool Equals(object? obj)` |  |
| `public bool Equals(DataSortDescriptor<TItem>? other)` |  |
| `public void Deconstruct(out string Key, out Func<TItem, object?> Selector, out ListSortDirection Direction)` |  |
| `public string Key { get; init; }` | The key of the sorted field or column. |
| `public Func<TItem, object?> Selector { get; init; }` | Reads the value compared for an item. |
| `public ListSortDirection Direction { get; init; }` | The direction of the sort. |
| `public IComparer<object?>? Comparer { get; init; }` | Gets the comparer of the values, or null for the default comparer (nulls first, then `System.IComparable`). |

### `DataView<TItem>`

Namespace `Nazor.Controls.Primitives`. [Source](../../Sources/Core/Nazor.Controls/Primitives/DataView.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed class DataView<TItem>` | A view over a collection that a grid or a list renders: it filters, sorts by several keys (stable), groups by a key with group headers that open and close, and pages the result. The source is a list (`Nazor.Controls.Primitives.DataView<TItem>.Source`) processed in memory, or an asynchronous `Nazor.Controls.Primitives.DataView<TItem>.Provider` that receives the sorts, the `Nazor.Controls.Primitives.DataView<TItem>.Query` and the page as a `Nazor.Controls.Primitives.DataRequest` for server-side data. Results are computed lazily and cached until a property changes or `Nazor.Controls.Primitives.DataView<TItem>.Refresh` is called; `Nazor.Controls.Primitives.DataView<TItem>.Changed` reports every change of the results. Every member must be used from the UI thread.<br>`TItem`. The type of the items. |
| `public void Refresh()` | Computes the results again, after the items of `Nazor.Controls.Primitives.DataView<TItem>.Source` changed in place. |
| `public ListSortDirection? SortDirectionOf(string key)` | Gets the direction of the sort on a key, or null when the key is not sorted.<br>`key`. The key of the field or column.<br>**Returns**. The direction, or null. |
| `public int SortIndexOf(string key)` | Gets the zero-based priority of the sort on a key, or -1 when the key is not sorted.<br>`key`. The key of the field or column.<br>**Returns**. The priority, zero for the most significant sort. |
| `public void SetSort(string key, Func<TItem, object?> selector, ListSortDirection? direction, bool add = false, IComparer<object?>? comparer = null)` | Sorts by a key: alone, replacing the other sorts, or added after them with `add`. A null direction removes the sort on that key.<br>`key`. The key of the field or column.<br>`selector`. Reads the value compared for an item.<br>`direction`. The direction, or null to remove the sort.<br>`add`. Whether the other sorts are kept.<br>`comparer`. The comparer of the values, or null for the default one. |
| `public bool IsGroupExpanded(object? key)` | Gets whether the items of a group are shown; groups are expanded until collapsed.<br>`key`. The key of the group.<br>**Returns**. true if the group is expanded. |
| `public void SetGroupExpanded(object? key, bool expanded)` | Shows or hides the items of a group.<br>`key`. The key of the group.<br>`expanded`. Whether the items are shown. |
| `public void ToggleGroup(object? key)` | Shows the items of a hidden group or hides those of a shown one.<br>`key`. The key of the group. |
| `public void ExpandAllGroups()` | Shows the items of every group. |
| `public void CollapseAllGroups()` | Hides the items of every current group. |
| `public Task LoadAsync(bool force = false)` | Asks `Nazor.Controls.Primitives.DataView<TItem>.Provider` for the current page when the request changed since the last load (or always with `force`); a newer load or request-setting change cancels the previous request. Accepted item membership, offset and total form one snapshot. Obsolete results/faults are ignored; invalid windows/offset overflow report LoadError and clear the accepted window. Empty windows beyond the total are allowed. Without a provider it does nothing.<br>`force`. Whether the provider is called even when the request did not change.<br>**Returns**. A task that completes when the data is available or the request was replaced. |
| `public DataView()` |  |
| `public IReadOnlyList<TItem>? Source { get; set; }` | Gets or sets the items processed in memory; ignored while `Nazor.Controls.Primitives.DataView<TItem>.Provider` is set. |
| `public Func<DataRequest, Task<DataPage<TItem>>>? Provider { get; set; }` | Gets or sets the asynchronous source of the items for server-side data, or null to use `Nazor.Controls.Primitives.DataView<TItem>.Source`. Call `Nazor.Controls.Primitives.DataView<TItem>.LoadAsync(System.Boolean)` after changing the request. |
| `public object? Query { get; set; }` | Gets or sets the application-defined filter sent to `Nazor.Controls.Primitives.DataView<TItem>.Provider`, such as a search text. |
| `public Func<TItem, bool>? Filter { get; set; }` | Predicate for local Source only; providers receive Query instead. |
| `public IReadOnlyList<DataSortDescriptor<TItem>> Sorts { get; set; }` | Gets or sets the sorts, the most significant first; items with equal keys keep their source order. |
| `public Func<TItem, object?>? GroupKey { get; set; }` | Gets or sets the function that gives the group of an item, or null for no grouping. |
| `public IComparer<object?>? GroupComparer { get; set; }` | Gets or sets the comparer that orders the groups, or null for the default comparer. |
| `public ListSortDirection GroupDirection { get; set; }` | Gets or sets the direction in which the groups are ordered. |
| `public int PageSize { get; set; }` | Gets or sets the number of items per page, or zero to show every item on one page. |
| `public int Page { get; set; }` | The requested zero-based page. Local projection uses the last available page if necessary; providers receive the requested offset and may return an empty window beyond their total. |
| `public int TotalCount { get; }` | The global filtered count from the accepted result; provider totals remain tied to that result while another request is pending. |
| `public int PageCount { get; }` | Gets the number of pages, at least one. |
| `public IReadOnlyList<TItem> FilteredItems { get; }` | Gets every item that passes the filter in sorted order (grouped items stay together), across all pages; with a `Nazor.Controls.Primitives.DataView<TItem>.Provider`, the items of the loaded page. |
| `public IReadOnlyList<TItem> Items { get; }` | The accepted page in display order. Provider rows retain accepted positions/count while a new request is pending. |
| `public IReadOnlyList<DataGroup<TItem>> Groups { get; }` | Gets the groups, in display order; empty without grouping. |
| `public IReadOnlyList<DataRow<TItem>> Rows { get; }` | Gets the flat projection of the current page that a list or a grid renders: a header before the items of each group, and expanded item rows. Each item carries its accepted result's global ItemPosition; provider grouping may reorder display while retaining response ordinals. |
| `public bool IsLoading { get; }` | Gets whether a `Nazor.Controls.Primitives.DataView<TItem>.Provider` request is in progress. |
| `public Exception? LoadError { get; }` | Gets the error of the last `Nazor.Controls.Primitives.DataView<TItem>.Provider` request, or null when it succeeded. |
| `public bool NeedsLoad { get; }` | Gets whether the request changed since the last `Nazor.Controls.Primitives.DataView<TItem>.LoadAsync(System.Boolean)`, so the provider must be called again. |
| `public int Version { get; }` | Gets a number that changes every time the results are computed again. |
| `public event Action? Changed` | Raised after any change of the settings, the groups or the loaded data that changes the results. |

### `DialogContainer`

Namespace `Nazor.Controls.Primitives`. [Source](../../Sources/Core/Nazor.Controls/Primitives/Overlays.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed class DialogContainer : OverlayContainer` | A window-local dialog surface without built-in content or look: modal by default (the rest of the window rejects interaction and the focus stays inside), centered or attached to an edge (`Nazor.Controls.Primitives.DialogContainer.Placement`), with an optional scrim behind it (`{name}Scrim`). It focuses its first control on open and returns the focus on close. Dialogs, alerts, drawers, sheets and command palettes are built on it. Name it with `Nazor.Controls.Control.AccessibleName` or `Nazor.Controls.Control.LabeledBy`. |
| `public DialogContainer()` | Creates a dialog container that ignores presses outside it. |
| `protected override void OnParametersSet()` | Validates the placement. |
| `protected override void RenderBefore(RenderTreeBuilder builder)` | Renders the scrim before the dialog.<br>`builder`. The builder that receives the render frames. |
| `protected override void AddSurfaceAttributes(RenderTreeBuilder builder)` | Adds the modality, the placement and the dialog semantics.<br>`builder`. The builder that receives the render frames. |
| `public bool Modal { get; set; }` | `[Parameter]` Initial value `true`.<br>Gets or sets whether interaction and the focus are confined to the dialog. |
| `public DialogPlacement Placement { get; set; }` | `[Parameter]`<br>Gets or sets where the dialog appears. |
| `public bool? ShowScrim { get; set; }` | `[Parameter]`<br>Gets or sets whether a scrim covers the window behind the dialog; true by default for modal dialogs. |
| `public string Role { get; set; }` | `[Parameter]` Initial value `"dialog"`.<br>Gets or sets the accessible role: `dialog` by default, `alertdialog` for decisions. |
| `public bool PreferCancel { get; set; }` | `[Parameter]`<br>Gets or sets whether Escape focuses and uses the part marked `dialog-cancel` rather than closing. |
| `protected override string DefaultName { get; }` | Gets the default element name. |

### `DialogPlacement`

Namespace `Nazor.Controls.Primitives`. [Source](../../Sources/Core/Nazor.Controls/Primitives/Overlays.cs)

| Declaration | Contract |
| --- | --- |
| `public enum DialogPlacement` | Where a `Nazor.Controls.Primitives.DialogContainer` appears in the window. |
| `Center = 0` | Centered in the window. |
| `Top = 1` | Along the top edge, as a sheet. |
| `Bottom = 2` | Along the bottom edge, as a bottom sheet. |
| `Start = 3` | Along the leading edge, as a navigation drawer. |
| `End = 4` | Along the trailing edge, as a side panel. |
| `FullScreen = 5` | Covering the whole window. |

### `DragEventArgs`

Namespace `Nazor.Controls.Primitives`. [Source](../../Sources/Core/Nazor.Controls/Primitives/Dragging.cs)

| Declaration | Contract |
| --- | --- |
| `public readonly struct DragEventArgs : IEquatable<DragEventArgs>` | The progress of a drag reported by a `Nazor.Controls.Primitives.Draggable`.<br>`StartX`. The pointer's horizontal position when the drag started, in window coordinates.<br>`StartY`. The pointer's vertical position when the drag started.<br>`DeltaX`. The horizontal distance moved since the start.<br>`DeltaY`. The vertical distance moved since the start.<br>`Modifiers`. The modifier keys held. |
| `public DragEventArgs()` |  |
| `public DragEventArgs(double StartX, double StartY, double DeltaX, double DeltaY, KeyModifiers Modifiers)` | The progress of a drag reported by a `Nazor.Controls.Primitives.Draggable`.<br>`StartX`. The pointer's horizontal position when the drag started, in window coordinates.<br>`StartY`. The pointer's vertical position when the drag started.<br>`DeltaX`. The horizontal distance moved since the start.<br>`DeltaY`. The vertical distance moved since the start.<br>`Modifiers`. The modifier keys held. |
| `public override string ToString()` |  |
| `public static bool operator !=(DragEventArgs left, DragEventArgs right)` |  |
| `public static bool operator ==(DragEventArgs left, DragEventArgs right)` |  |
| `public override int GetHashCode()` |  |
| `public override bool Equals(object obj)` |  |
| `public bool Equals(DragEventArgs other)` |  |
| `public void Deconstruct(out double StartX, out double StartY, out double DeltaX, out double DeltaY, out KeyModifiers Modifiers)` |  |
| `public double StartX { get; init; }` | The pointer's horizontal position when the drag started, in window coordinates. |
| `public double StartY { get; init; }` | The pointer's vertical position when the drag started. |
| `public double DeltaX { get; init; }` | The horizontal distance moved since the start. |
| `public double DeltaY { get; init; }` | The vertical distance moved since the start. |
| `public KeyModifiers Modifiers { get; init; }` | The modifier keys held. |

### `Draggable`

Namespace `Nazor.Controls.Primitives`. [Source](../../Sources/Core/Nazor.Controls/Primitives/Dragging.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed class Draggable : ContentControl` | Makes content draggable without imposing a look: after the pointer moves `Nazor.Controls.Primitives.Draggable.Threshold` units with the primary button held, `Nazor.Controls.Primitives.Draggable.OnDragStart`, `Nazor.Controls.Primitives.Draggable.OnDrag` and `Nazor.Controls.Primitives.Draggable.OnDragEnd` report the movement from the start; Escape-free cancellation happens when the pointer capture is lost. Movement can be limited to one `Nazor.Controls.Primitives.Draggable.Axis`. Splitters, resize handles, sliders of custom shape, color areas and movable dialogs are built on it. The element is named by `Nazor.Controls.Primitives.Draggable.As`; it is `dragging` while a drag is in progress. |
| `protected override void OnParametersSet()` | Validates the threshold. |
| `protected override void AddAttributes(RenderTreeBuilder builder)` | Adds the pointer tracking and the dragging state.<br>`builder`. The builder that receives the render frames. |
| `public Draggable()` |  |
| `public string As { get; set; }` | `[Parameter]` Initial value `"Draggable"`.<br>Gets or sets the element name, `Draggable` by default. |
| `public Orientation? Axis { get; set; }` | `[Parameter]`<br>Gets or sets the axis the movement is limited to, or null for both. |
| `public double Threshold { get; set; }` | `[Parameter]` Initial value `3`.<br>Gets or sets the distance the pointer must move before a drag starts. |
| `public bool Disabled { get; set; }` | `[Parameter]`<br>Gets or sets whether dragging is unavailable. |
| `public EventCallback<DragEventArgs> OnDragStart { get; set; }` | `[Parameter]`<br>Gets or sets the callback invoked when the drag starts. |
| `public EventCallback<DragEventArgs> OnDrag { get; set; }` | `[Parameter]`<br>Gets or sets the callback invoked as the pointer moves. |
| `public EventCallback<DragEventArgs> OnDragEnd { get; set; }` | `[Parameter]`<br>Gets or sets the callback invoked when the pointer is released after a drag. |
| `public bool IsDragging { get; }` | Gets whether a drag is in progress. |
| `protected override string ElementName { get; }` | Gets the element name. |

### `EditableText`

Namespace `Nazor.Controls.Primitives`. [Source](../../Sources/Core/Nazor.Controls/Primitives/EditableText.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed class EditableText : Control` | A bare text editor without frame or look, the editing core of every field: caret and selection, clipboard, undo, input methods and the text context menu come from the engine. `Nazor.Controls.Primitives.EditableText.Value` supports `@bind-Value` and changes on every edit; `Nazor.Controls.Primitives.EditableText.OnChange` reports the committed value (Enter on one line, or focus loss). The element is named by `Nazor.Controls.Primitives.EditableText.As` so a field can place it among its own parts. Inside a `Nazor.Controls.Primitives.EditableText.Field` it is named and described by the field automatically. |
| `protected override void OnParametersSet()` | Synchronizes the text with the parameter. |
| `protected override void AddAttributes(RenderTreeBuilder builder)` | Adds the editing attributes.<br>`builder`. The builder that receives the render frames. |
| `protected override void AddContent(RenderTreeBuilder builder)` | Adds the displayed text, masked for passwords.<br>`builder`. The builder that receives the render frames. |
| `public Task SetTextAsync(string value)` | Replaces the text as an edit would and reports it.<br>`value`. The new text.<br>**Returns**. A task that completes when the change has been reported. |
| `public EditableText()` |  |
| `public string As { get; set; }` | `[Parameter]` Initial value `"EditableText"`.<br>Gets or sets the element name, `EditableText` by default. |
| `public string? Value { get; set; }` | `[Parameter]`<br>Gets or sets the text; null shows as empty. |
| `public EventCallback<string> ValueChanged { get; set; }` | `[Parameter]`<br>Gets or sets the callback invoked on every edit. |
| `public string? Placeholder { get; set; }` | `[Parameter]`<br>Gets or sets the hint drawn while the text is empty; it does not replace a label. |
| `public bool ReadOnly { get; set; }` | `[Parameter]`<br>Gets or sets whether editing is blocked while selection and copying remain available. |
| `public bool Disabled { get; set; }` | `[Parameter]`<br>Gets or sets whether the editor rejects interaction. |
| `public bool Required { get; set; }` | `[Parameter]`<br>Gets or sets whether a value is required; validation is application-owned. |
| `public bool Invalid { get; set; }` | `[Parameter]`<br>Gets or sets the externally determined invalid state. |
| `public int MaxLength { get; set; }` | `[Parameter]`<br>Gets or sets the maximum number of UTF-16 characters; zero or negative means unlimited. |
| `public bool Multiline { get; set; }` | `[Parameter]`<br>Gets or sets whether Enter inserts a line break. |
| `public bool Password { get; set; }` | `[Parameter]`<br>Gets or sets whether the text is masked and protected from copying and assistive technologies. |
| `public char MaskCharacter { get; set; }` | `[Parameter]` Initial value `'●'`.<br>Gets or sets the character drawn for each masked character. |
| `public bool AutoFocus { get; set; }` | `[Parameter]`<br>Gets or sets whether the editor takes the focus when it appears or when its overlay opens. |
| `public SpinRange? Spin { get; set; }` | `[Parameter]`<br>Gets or sets the range stepped by the Up, Down, Page Up and Page Down keys, for numeric editors; null disables stepping. The owner converts the text and supplies the current numeric value. |
| `public string? InputMode { get; set; }` | `[Parameter]`<br>Gets or sets the input intent hint for platform input methods, such as `number` or `email`. |
| `public string Role { get; set; }` | `[Parameter]` Initial value `"textbox"`.<br>Gets or sets the accessible role; `textbox` by default, `combobox` or `spinbutton` for owners. |
| `public EventCallback<ChangeEventArgs> OnInput { get; set; }` | `[Parameter]`<br>Gets or sets the callback invoked with every raw edit or step. |
| `public EventCallback<ChangeEventArgs> OnChange { get; set; }` | `[Parameter]`<br>Gets or sets the callback invoked with the committed value. |
| `public FieldContext? Field { get; set; }` | `[CascadingParameter]`<br>Gets or sets the enclosing field, supplied automatically. |
| `public string Text { get; }` | Gets the current text. |
| `protected override string ElementName { get; }` | Gets the element name. |

### `HoverTracker`

Namespace `Nazor.Controls.Primitives`. [Source](../../Sources/Core/Nazor.Controls/Primitives/HoverTracker.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed class HoverTracker` | Tracks one pointer-hovered item for a decorative `Indicator` inside an existing group. It retains the last box for exit fading, snaps the first entry and distinguishes later movement. All calls belong on the UI dispatcher. |
| `public HoverTracker(Action changed)` | Creates a tracker. `changed` requests a render when the active item, geometry or entry/movement state changes. |
| `public ItemBox? Target { get; private set; }` | Gets the last item's border box in the host's scrolling coordinate space, or null before a valid item was measured. Geometry uses the items' axis-aligned window bounds and the host's inverse coordinate conversion. |
| `public bool Active { get; private set; }` | Gets whether the pointer is currently over an eligible, visible, enabled item. |
| `public bool IsMoving { get; private set; }` | Gets whether this entry has already moved between items. Themes can snap initial geometry and animate subsequent movement. |
| `public IReadOnlyDictionary<string, object?> Attributes(object receiver, Func<ElementReference?> resolveHost, Func<ElementNode, bool> match)` | Creates scoped pointer, scroll and resize listeners for the group that directly contains its Indicator. Nested tracker groups own their own hover; disabled/hidden/removed items are excluded. Calling during rendering also drops a stale removed/disabled target.<br>`receiver`. The component that owns the listeners and receives rendering notifications.<br>`resolveHost`. Resolves the group's current retained element; a resolver allows references captured after its first render.<br>`match`. Identifies eligible item elements inside this group.<br>**Returns**. Attributes to add to the host before its element-reference capture. They preserve ordinary pointer activation, focus and semantics. |
| `public void Refresh()` | Updates the current item's geometry after custom layout changes, or clears activity when it is no longer eligible. The last box is retained for exit fading. |
| `public void Clear()` | Ends the current entry without forgetting its last box, allowing a theme-owned fade. It does not move focus or consume input. |

Compose one retained `Indicator` with `As="HoverIndicator"` and `Target=tracker.Target` inside the host receiving `tracker.Attributes(...)`. The attributes publish `hover-highlight`, `hover-active` and `hover-moving`, preserving ordinary input and semantics. The first entry snaps to its item before fading; later items can animate. Scroll coordinates belong to the host itself: descendant scrolls refresh geometry without replacing the host offset. Current item removal/disable/hide discards the old target; a stationary pointer may target an eligible replacement after scrolling or layout changes; call `Refresh()` after custom content changes and `Clear()` for an explicit exit. Theme effects use axis-aligned item bounds, so rotated item shapes are represented by those bounds.

### `HoverIntent`

Namespace `Nazor.Controls.Primitives`. [Source](../../Sources/Core/Nazor.Controls/Primitives/Disclosure.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed class HoverIntent : IDisposable` | Delays opening and closing driven by the pointer, as submenus, hover cards and the flyouts of a compact navigation pane do: `Nazor.Controls.Primitives.HoverIntent.Enter` schedules the open callback after `Nazor.Controls.Primitives.HoverIntent.OpenDelay`, `Nazor.Controls.Primitives.HoverIntent.Leave` schedules the close callback after `Nazor.Controls.Primitives.HoverIntent.CloseDelay`, and moving back cancels the pending change so a brief exit does not flicker. Callbacks run through the dispatcher supplied at construction. Every member must be used from the UI thread.<br>`dispatch`. Runs work on the UI thread, usually a component's `InvokeAsync`.<br>`open`. The work run to open.<br>`close`. The work run to close. |
| `public HoverIntent(Func<Func<Task>, Task> dispatch, Func<Task> open, Func<Task> close, TimeProvider? timeProvider = null)` | Delays opening and closing driven by the pointer, as submenus, hover cards and the flyouts of a compact navigation pane do: `Nazor.Controls.Primitives.HoverIntent.Enter` schedules the open callback after `Nazor.Controls.Primitives.HoverIntent.OpenDelay`, `Nazor.Controls.Primitives.HoverIntent.Leave` schedules the close callback after `Nazor.Controls.Primitives.HoverIntent.CloseDelay`, and moving back cancels the pending change so a brief exit does not flicker. Callbacks run through the dispatcher supplied at construction. Every member must be used from the UI thread.<br>`dispatch`. Runs work on the UI thread, usually a component's `InvokeAsync`.<br>`open`. The work run to open.<br>`close`. The work run to close. |
| `public void Enter()` | Schedules opening and cancels a pending close. |
| `public void Leave()` | Schedules closing and cancels a pending open. |
| `public void Cancel()` | Cancels any pending change. |
| `public void Dispose()` | Cancels any pending change. |
| `public TimeSpan OpenDelay { get; set; }` | Gets or sets the delay before opening. |
| `public TimeSpan CloseDelay { get; set; }` | Gets or sets the delay before closing. |

The optional timer provider defaults to TimeProvider.System. Timer callbacks post through the supplied dispatcher; Enter/Leave/Cancel supersede earlier generations, so canceled work cannot run later. Submenu uses this helper with its cascading Time provider and the containing menu's theme delay.

### `HslColor`

Namespace `Nazor.Controls.Primitives`. [Source](../../Sources/Core/Nazor.Controls/Primitives/ColorModel.cs)

| Declaration | Contract |
| --- | --- |
| `public readonly struct HslColor : IEquatable<HslColor>` | A color in the hue, saturation and lightness model, with straight alpha.<br>`H`. The hue in degrees, from 0 (inclusive) to 360 (exclusive).<br>`S`. The saturation, from 0 to 1.<br>`L`. The lightness, from 0 to 1.<br>`A`. The opacity, from 0 to 1. |
| `public HslColor()` |  |
| `public HslColor(double H, double S, double L, double A = 1)` | A color in the hue, saturation and lightness model, with straight alpha.<br>`H`. The hue in degrees, from 0 (inclusive) to 360 (exclusive).<br>`S`. The saturation, from 0 to 1.<br>`L`. The lightness, from 0 to 1.<br>`A`. The opacity, from 0 to 1. |
| `public HslColor Clamp()` | Returns the color with every component brought into its range; non-finite components become zero.<br>**Returns**. The clamped color, with the hue wrapped into [0, 360). |
| `public override string ToString()` |  |
| `public static bool operator !=(HslColor left, HslColor right)` |  |
| `public static bool operator ==(HslColor left, HslColor right)` |  |
| `public override int GetHashCode()` |  |
| `public override bool Equals(object obj)` |  |
| `public bool Equals(HslColor other)` |  |
| `public void Deconstruct(out double H, out double S, out double L, out double A)` |  |
| `public double H { get; init; }` | The hue in degrees, from 0 (inclusive) to 360 (exclusive). |
| `public double S { get; init; }` | The saturation, from 0 to 1. |
| `public double L { get; init; }` | The lightness, from 0 to 1. |
| `public double A { get; init; }` | The opacity, from 0 to 1. |

### `HsvColor`

Namespace `Nazor.Controls.Primitives`. [Source](../../Sources/Core/Nazor.Controls/Primitives/ColorModel.cs)

| Declaration | Contract |
| --- | --- |
| `public readonly struct HsvColor : IEquatable<HsvColor>` | A color in the hue, saturation and value (brightness) model, with straight alpha.<br>`H`. The hue in degrees, from 0 (inclusive) to 360 (exclusive).<br>`S`. The saturation, from 0 to 1.<br>`V`. The value or brightness, from 0 to 1.<br>`A`. The opacity, from 0 to 1. |
| `public HsvColor()` |  |
| `public HsvColor(double H, double S, double V, double A = 1)` | A color in the hue, saturation and value (brightness) model, with straight alpha.<br>`H`. The hue in degrees, from 0 (inclusive) to 360 (exclusive).<br>`S`. The saturation, from 0 to 1.<br>`V`. The value or brightness, from 0 to 1.<br>`A`. The opacity, from 0 to 1. |
| `public HsvColor Clamp()` | Returns the color with every component brought into its range; non-finite components become zero.<br>**Returns**. The clamped color, with the hue wrapped into [0, 360). |
| `public override string ToString()` |  |
| `public static bool operator !=(HsvColor left, HsvColor right)` |  |
| `public static bool operator ==(HsvColor left, HsvColor right)` |  |
| `public override int GetHashCode()` |  |
| `public override bool Equals(object obj)` |  |
| `public bool Equals(HsvColor other)` |  |
| `public void Deconstruct(out double H, out double S, out double V, out double A)` |  |
| `public double H { get; init; }` | The hue in degrees, from 0 (inclusive) to 360 (exclusive). |
| `public double S { get; init; }` | The saturation, from 0 to 1. |
| `public double V { get; init; }` | The value or brightness, from 0 to 1. |
| `public double A { get; init; }` | The opacity, from 0 to 1. |

### `Indicator`

Namespace `Nazor.Controls.Primitives`. [Source](../../Sources/Core/Nazor.Controls/Primitives/Measurement.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed class Indicator : Control` | A decorative mark that follows an item of its group, such as the line under the selected tab or the highlight that glides between menu items. It renders an element (`Nazor.Controls.Primitives.Indicator.As`) with the item's box as the `--nz-indicator-x`, `--nz-indicator-y`, `--nz-indicator-width` and `--nz-indicator-height` variables and the `hidden` attribute when there is no target; themes position it and may animate the change. |
| `protected override void AddAttributes(RenderTreeBuilder builder)` | Hides the indicator from assistive technologies and marks a missing target.<br>`builder`. The builder that receives the render frames. |
| `public Indicator()` |  |
| `public string As { get; set; }` | `[Parameter]` Initial value `"Indicator"`.<br>Gets or sets the element name, `Indicator` by default. |
| `public ItemBox? Target { get; set; }` | `[Parameter]`<br>Gets or sets the box to follow, or null to hide the indicator. |
| `protected override string ElementName { get; }` | Gets the element name. |
| `protected override string? ElementStyle { get; }` | Gets the position variables and the caller's declarations. |

### `IndicatorTracker<TKey>`

Namespace `Nazor.Controls.Primitives`. [Source](../../Sources/Core/Nazor.Controls/Primitives/Measurement.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed class IndicatorTracker<TKey> where TKey : notnull` | Remembers where the items of a group are, so a selection or hover indicator can follow them. An item reports its box with the `Nazor.Controls.Primitives.IndicatorTracker<TKey>.Attributes(System.Object,`0)` it adds to its element (an `onresize` listener); the group renders an `Nazor.Controls.Primitives.Indicator` with `Nazor.Controls.Primitives.IndicatorTracker<TKey>.BoxOf(`0)` of the selected or hovered key. Tabs, segmented controls, navigation panes and menus share it.<br>`TKey`. The type of the item keys.<br>`changed`. Invoked when a box changes, usually the group's `StateHasChanged`. |
| `public IndicatorTracker(Action changed)` | Remembers where the items of a group are, so a selection or hover indicator can follow them. An item reports its box with the `Nazor.Controls.Primitives.IndicatorTracker<TKey>.Attributes(System.Object,`0)` it adds to its element (an `onresize` listener); the group renders an `Nazor.Controls.Primitives.Indicator` with `Nazor.Controls.Primitives.IndicatorTracker<TKey>.BoxOf(`0)` of the selected or hovered key. Tabs, segmented controls, navigation panes and menus share it.<br>`TKey`. The type of the item keys.<br>`changed`. Invoked when a box changes, usually the group's `StateHasChanged`. |
| `public ItemBox? BoxOf(TKey? key)` | Gets the box of an item, or null before it was laid out.<br>`key`. The item key.<br>**Returns**. The box, or null. |
| `public void Report(TKey key, ResizeEventArgs e)` | Records the box of an item laid out directly inside the group.<br>`key`. The item key.<br>`e`. The resize event of the item. |
| `public void Forget(TKey key)` | Forgets an item that was removed.<br>`key`. The item key. |
| `public KeyValuePair<string, object?> Attributes(object receiver, TKey key)` | Creates the listener an item adds to its element to report its box.<br>`receiver`. The component that owns the listener.<br>`key`. The item key.<br>**Returns**. The `__internal_onresize#indicator` attribute and its callback. |

### `ItemBox`

Namespace `Nazor.Controls.Primitives`. [Source](../../Sources/Core/Nazor.Controls/Primitives/Measurement.cs)

| Declaration | Contract |
| --- | --- |
| `public readonly struct ItemBox : IEquatable<ItemBox>` | The box of an item measured by an `Nazor.Controls.Primitives.IndicatorTracker<TKey>`, relative to the group that holds it.<br>`X`. The distance from the group's left edge.<br>`Y`. The distance from the group's top edge.<br>`Width`. The width.<br>`Height`. The height. |
| `public ItemBox()` |  |
| `public ItemBox(double X, double Y, double Width, double Height)` | The box of an item measured by an `Nazor.Controls.Primitives.IndicatorTracker<TKey>`, relative to the group that holds it.<br>`X`. The distance from the group's left edge.<br>`Y`. The distance from the group's top edge.<br>`Width`. The width.<br>`Height`. The height. |
| `public override string ToString()` |  |
| `public static bool operator !=(ItemBox left, ItemBox right)` |  |
| `public static bool operator ==(ItemBox left, ItemBox right)` |  |
| `public override int GetHashCode()` |  |
| `public override bool Equals(object obj)` |  |
| `public bool Equals(ItemBox other)` |  |
| `public void Deconstruct(out double X, out double Y, out double Width, out double Height)` |  |
| `public double X { get; init; }` | The distance from the group's left edge. |
| `public double Y { get; init; }` | The distance from the group's top edge. |
| `public double Width { get; init; }` | The width. |
| `public double Height { get; init; }` | The height. |

### `KeyGesture`

Namespace `Nazor.Controls.Primitives`. [Source](../../Sources/Core/Nazor.Controls/Primitives/Commands.cs)

| Declaration | Contract |
| --- | --- |
| `public readonly struct KeyGesture : IEquatable<KeyGesture>` | A key combination such as Ctrl+S, written in the platform's notation by `Nazor.Controls.Primitives.KeyGesture.ToDisplayString`. Keys use the names of `Nazor.Input.KeyboardEventArgs.Key` ("S", "Enter", "ArrowDown", "F2", "Delete"). The `Nazor.Controls.Primitives.KeyGesture.Primary(System.String,Nazor.Input.KeyModifiers)` factory and the "Mod" prefix of `Nazor.Controls.Primitives.KeyGesture.Parse(System.String)` choose Command on macOS and Ctrl elsewhere, so one declaration follows each platform's convention.<br>`Key`. The key name.<br>`Modifiers`. The modifier keys held with it. |
| `public KeyGesture()` |  |
| `public KeyGesture(string Key, KeyModifiers Modifiers = KeyModifiers.None)` | A key combination such as Ctrl+S, written in the platform's notation by `Nazor.Controls.Primitives.KeyGesture.ToDisplayString`. Keys use the names of `Nazor.Input.KeyboardEventArgs.Key` ("S", "Enter", "ArrowDown", "F2", "Delete"). The `Nazor.Controls.Primitives.KeyGesture.Primary(System.String,Nazor.Input.KeyModifiers)` factory and the "Mod" prefix of `Nazor.Controls.Primitives.KeyGesture.Parse(System.String)` choose Command on macOS and Ctrl elsewhere, so one declaration follows each platform's convention.<br>`Key`. The key name.<br>`Modifiers`. The modifier keys held with it. |
| `public static KeyGesture Primary(string key, KeyModifiers extra = KeyModifiers.None)` | Creates a gesture with the platform's primary modifier, such as Ctrl+S or ⌘S.<br>`key`. The key name.<br>`extra`. Additional modifiers, such as Shift.<br>**Returns**. The gesture. |
| `public static KeyGesture Parse(string text)` | Parses a gesture written as modifiers and a key joined by `+`, for example "Ctrl+Shift+S", "Alt+F4" or "Mod+S" (the platform's primary modifier). Modifier names are Ctrl, Control, Shift, Alt, Option, Meta, Cmd, Command, Win, Super and Mod, in any case.<br>`text`. The gesture text.<br>**Returns**. The gesture.<br>**Throws** `System.FormatException`. The text has no key or an unknown modifier. |
| `public bool Matches(KeyboardEventArgs e)` | Gets whether a key event matches this gesture.<br>`e`. The key event.<br>**Returns**. true if the key and the modifiers match. |
| `public IReadOnlyList<string> DisplayParts()` | Gets the key labels in display order for the current platform, for example ["Ctrl", "Shift", "S"] or ["⇧", "⌘", "S"].<br>**Returns**. The labels of the modifiers and the key. |
| `public string ToDisplayString()` | Gets the gesture in the platform's notation, such as "Ctrl+Shift+S" or "⇧⌘S".<br>**Returns**. The display text. |
| `public override string ToString()` | Gets the display text of the gesture.<br>**Returns**. The display text. |
| `public static bool operator !=(KeyGesture left, KeyGesture right)` |  |
| `public static bool operator ==(KeyGesture left, KeyGesture right)` |  |
| `public override int GetHashCode()` |  |
| `public override bool Equals(object obj)` |  |
| `public bool Equals(KeyGesture other)` |  |
| `public void Deconstruct(out string Key, out KeyModifiers Modifiers)` |  |
| `public string Key { get; init; }` | The key name. |
| `public KeyModifiers Modifiers { get; init; }` | The modifier keys held with it. |
| `public static KeyModifiers PrimaryModifier { get; }` | Gets the modifier of the platform's primary shortcuts: Meta (Command) on macOS, Control elsewhere. |

### `Listbox<TValue>`

Namespace `Nazor.Controls.Primitives`. [Source](../../Sources/Core/Nazor.Controls/Primitives/Listbox.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed class Listbox<TValue> : ContentControl where TValue : notnull` | A list of options without built-in look: one Tab stop, arrows and typing move between options, Space, Enter or a click select, Shift and Ctrl extend a multiple selection. `Nazor.Controls.Primitives.Listbox<TValue>.Value` (`@bind-Value`) holds a single selection and `Nazor.Controls.Primitives.Listbox<TValue>.Values` (`@bind-Values`) a multiple one. Fill it with `Nazor.Controls.Primitives.ListboxOption<TValue>`. The element is named by `Nazor.Controls.Primitives.Listbox<TValue>.As`; options carry `selected` and `current`. Drop-down lists, pickers and custom choice lists are built on it.<br>`TValue`. The type of the option values. |
| `protected override void OnParametersSet()` | Synchronizes the selection with the parameters. |
| `protected override void AddAttributes(RenderTreeBuilder builder)` | Adds the list role and the keyboard handling.<br>`builder`. The builder that receives the render frames. |
| `protected override void AddContent(RenderTreeBuilder builder)` | Supplies the list to its options.<br>`builder`. The builder that receives the render frames. |
| `protected override void OnAfterRender(bool firstRender)` | Refreshes the options once laid out, so the Tab stop is right.<br>`firstRender`. Whether this is the first render. |
| `public Listbox()` |  |
| `public string As { get; set; }` | `[Parameter]` Initial value `"Listbox"`.<br>Gets or sets the element name, `Listbox` by default. |
| `public SelectionMode SelectionMode { get; set; }` | `[Parameter]` Initial value `SelectionMode.Single`.<br>Gets or sets how many options can be selected. |
| `public TValue? Value { get; set; }` | `[Parameter]`<br>Gets or sets the selected value in single selection. |
| `public EventCallback<TValue> ValueChanged { get; set; }` | `[Parameter]`<br>Gets or sets the callback invoked when the user selects another value in single selection. |
| `public IReadOnlyCollection<TValue>? Values { get; set; }` | `[Parameter]`<br>Gets or sets the selected values in multiple selection. |
| `public EventCallback<IReadOnlyCollection<TValue>> ValuesChanged { get; set; }` | `[Parameter]`<br>Gets or sets the callback invoked when the user changes the multiple selection. |
| `public bool SelectionFollowsFocus { get; set; }` | `[Parameter]`<br>Gets or sets whether moving with the arrows also selects, in single selection. |
| `public EventCallback<TValue> OnActivate { get; set; }` | `[Parameter]`<br>Gets or sets the callback invoked when an option is activated with Enter or a double click. |
| `public Orientation Orientation { get; set; }` | `[Parameter]` Initial value `Orientation.Vertical`.<br>Gets or sets the layout direction of the options. |
| `public bool Disabled { get; set; }` | `[Parameter]`<br>Gets or sets whether the list rejects interaction. |
| `public SelectionModel<TValue> Selection { get; }` | Gets the selection. |
| `protected override string ElementName { get; }` | Gets the element name. |

### `ListboxOption<TValue>`

Namespace `Nazor.Controls.Primitives`. [Source](../../Sources/Core/Nazor.Controls/Primitives/Listbox.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed class ListboxOption<TValue> : ContentControl, IDisposable where TValue : notnull` | An option of a `Nazor.Controls.Primitives.Listbox<TValue>`. The element is named by `Nazor.Controls.Primitives.ListboxOption<TValue>.As` (`ListboxOption` by default) and carries `selected`, `current` and `disabled`.<br>`TValue`. The type of the option values. |
| `protected override void OnParametersSet()` | Joins the enclosing list. |
| `protected override void AddAttributes(RenderTreeBuilder builder)` | Adds the option role, its states and its activation.<br>`builder`. The builder that receives the render frames. |
| `public void Dispose()` | Leaves the enclosing list. |
| `public ListboxOption()` |  |
| `public string As { get; set; }` | `[Parameter]` Initial value `"ListboxOption"`.<br>Gets or sets the element name, `ListboxOption` by default. |
| `public TValue Value { get; set; }` | `[Parameter]` `[EditorRequired]` Initial value `default!`.<br>Gets or sets the value selected by this option. |
| `public bool Disabled { get; set; }` | `[Parameter]`<br>Gets or sets whether the option is skipped and cannot be selected. |
| `public string? TextValue { get; set; }` | `[Parameter]`<br>Gets or sets the text matched by typing; the content's text is used when null. |
| `public Listbox<TValue>? Owner { get; set; }` | `[CascadingParameter]`<br>Gets or sets the enclosing list, supplied automatically. |
| `protected override string ElementName { get; }` | Gets the element name. |

### `LivePoliteness`

Namespace `Nazor.Controls.Primitives`. [Source](../../Sources/Core/Nazor.Controls/Primitives/Measurement.cs)

| Declaration | Contract |
| --- | --- |
| `public enum LivePoliteness` | How urgently a `Nazor.Controls.Primitives.LiveRegion` is announced. |
| `Polite = 0` | Announced when the user is idle. |
| `Assertive = 1` | Announced at once, interrupting; for errors and time-critical messages. |

### `LiveRegion`

Namespace `Nazor.Controls.Primitives`. [Source](../../Sources/Core/Nazor.Controls/Primitives/Measurement.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed class LiveRegion : ContentControl` | A region whose changes assistive technologies announce, such as a status line, a form error or a toast stack. Rendering new content inside it is enough to announce it. |
| `protected override void AddAttributes(RenderTreeBuilder builder)` | Adds the status role and the live politeness.<br>`builder`. The builder that receives the render frames. |
| `public LiveRegion()` |  |
| `public string As { get; set; }` | `[Parameter]` Initial value `"LiveRegion"`.<br>Gets or sets the element name, `LiveRegion` by default. |
| `public LivePoliteness Politeness { get; set; }` | `[Parameter]`<br>Gets or sets how urgently changes are announced. |
| `protected override string ElementName { get; }` | Gets the element name. |

### `LoadingBoundary`

Namespace `Nazor.Controls.Primitives`. [Source](../../Sources/Core/Nazor.Controls/Primitives/LoadingBoundary.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed class LoadingBoundary : ContentControl, IDisposable` | Shows content that loads asynchronously without flicker: while `Nazor.Controls.Primitives.LoadingBoundary.IsLoading` is true it shows nothing for `Nazor.Controls.Primitives.LoadingBoundary.Delay`, then the `Nazor.Controls.Primitives.LoadingBoundary.Loading` content (a `Nazor.Controls.Spinner` by default) for at least `Nazor.Controls.Primitives.LoadingBoundary.MinimumDuration`, so fast loads show no indicator and slow ones do not flash it. When loading is over and `Nazor.Controls.Primitives.LoadingBoundary.Error` or `Nazor.Controls.Primitives.LoadingBoundary.ErrorMessage` is set it shows the error (`Nazor.Controls.Primitives.LoadingBoundary.ErrorContent`, or the message and a retry button raising `Nazor.Controls.Primitives.LoadingBoundary.OnRetry`); otherwise the content between the tags. The parts are `LoadingBoundaryLoading`, `LoadingBoundaryError` (a live region) with `LoadingBoundaryMessage` and `LoadingBoundaryRetry`, and `LoadingBoundaryContent`. |
| `protected override void OnParametersSet()` | Validates the delays and follows the loading state. |
| `protected override void AddAttributes(RenderTreeBuilder builder)` | Adds the state and the busy attribute.<br>`builder`. The builder that receives the render frames. |
| `protected override void AddContent(RenderTreeBuilder builder)` | Adds the part of the current state.<br>`builder`. The builder that receives the render frames. |
| `public void Dispose()` | Stops the pending delay. |
| `public LoadingBoundary()` |  |
| `public bool IsLoading { get; set; }` | `[Parameter]`<br>Gets or sets whether the content is loading. |
| `public Exception? Error { get; set; }` | `[Parameter]`<br>Gets or sets the failure of the last load, or null. |
| `public string? ErrorMessage { get; set; }` | `[Parameter]`<br>Gets or sets the text shown for the failure; null uses the exception's message. |
| `public EventCallback OnRetry { get; set; }` | `[Parameter]`<br>Gets or sets the callback invoked by the retry button; without it no retry button is shown. |
| `public RenderFragment? Loading { get; set; }` | `[Parameter]`<br>Gets or sets the content shown while loading; null shows a `Nazor.Controls.Spinner`. |
| `public RenderFragment<string>? ErrorContent { get; set; }` | `[Parameter]`<br>Gets or sets the content shown for a failure, which receives the message; null shows the default error. |
| `public TimeSpan Delay { get; set; }` | `[Parameter]` Initial value `TimeSpan.FromMilliseconds(300)`.<br>Gets or sets how long loading must last before the indicator appears; 300 ms by default. |
| `public TimeSpan MinimumDuration { get; set; }` | `[Parameter]` Initial value `TimeSpan.FromMilliseconds(500)`.<br>Gets or sets how long the indicator stays once shown, even if loading ends sooner; 500 ms by default. |
| `public TimeProvider? Time { get; set; }` | `[Parameter]`<br>Gets or sets the clock used for the delays, or null for the system clock. |
| `public LoadingBoundaryState State { get; }` | Gets what the boundary currently shows. |
| `protected override string ElementName { get; }` | Gets the NSS element name emitted by LoadingBoundary. |

### `LoadingBoundaryState`

Namespace `Nazor.Controls.Primitives`. [Source](../../Sources/Core/Nazor.Controls/Primitives/LoadingBoundary.cs)

| Declaration | Contract |
| --- | --- |
| `public enum LoadingBoundaryState` | What a `Nazor.Controls.Primitives.LoadingBoundary` currently shows, written as its `state` attribute. |
| `Content = 0` | The content (`content`). |
| `Pending = 1` | Nothing, because loading started less than the delay ago (`pending`). |
| `Loading = 2` | The loading indicator (`loading`). |
| `Error = 3` | The error with its retry button (`error`). |

### `MaskResult`

Namespace `Nazor.Controls.Primitives`. [Source](../../Sources/Core/Nazor.Controls/Primitives/TextMask.cs)

| Declaration | Contract |
| --- | --- |
| `public readonly struct MaskResult : IEquatable<MaskResult>` | Text fitted into a `Nazor.Controls.Primitives.TextMask`.<br>`Text`. The formatted text as edited: the typed characters with the literals before them.<br>`Raw`. Only the characters that fill editable positions.<br>`Formatted`. The formatted text, with the trailing literals once the mask is complete.<br>`IsComplete`. Whether every required position is filled.<br>`Prompt`. The rest of the mask after the text, with a prompt character for each empty position. |
| `public MaskResult()` |  |
| `public MaskResult(string Text, string Raw, string Formatted, bool IsComplete, string Prompt)` | Text fitted into a `Nazor.Controls.Primitives.TextMask`.<br>`Text`. The formatted text as edited: the typed characters with the literals before them.<br>`Raw`. Only the characters that fill editable positions.<br>`Formatted`. The formatted text, with the trailing literals once the mask is complete.<br>`IsComplete`. Whether every required position is filled.<br>`Prompt`. The rest of the mask after the text, with a prompt character for each empty position. |
| `public override string ToString()` |  |
| `public static bool operator !=(MaskResult left, MaskResult right)` |  |
| `public static bool operator ==(MaskResult left, MaskResult right)` |  |
| `public override int GetHashCode()` |  |
| `public override bool Equals(object obj)` |  |
| `public bool Equals(MaskResult other)` |  |
| `public void Deconstruct(out string Text, out string Raw, out string Formatted, out bool IsComplete, out string Prompt)` |  |
| `public string Text { get; init; }` | The formatted text as edited: the typed characters with the literals before them. |
| `public string Raw { get; init; }` | Only the characters that fill editable positions. |
| `public string Formatted { get; init; }` | The formatted text, with the trailing literals once the mask is complete. |
| `public bool IsComplete { get; init; }` | Whether every required position is filled. |
| `public string Prompt { get; init; }` | The rest of the mask after the text, with a prompt character for each empty position. |

### `OverflowContainer<TItem>`

Namespace `Nazor.Controls.Primitives`. [Source](../../Sources/Core/Nazor.Controls/Primitives/OverflowContainer.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed class OverflowContainer<TItem> : Control` | A row of items that hands over to an overflow control the items that do not fit: the visible items are rendered with `Nazor.Controls.Primitives.OverflowContainer<TItem>.ItemTemplate`, the hidden ones are passed to `Nazor.Controls.Primitives.OverflowContainer<TItem>.OverflowTemplate`, typically a "More" menu button. Items with a lower `Nazor.Controls.Primitives.OverflowContainer<TItem>.Priority` leave first; equal priorities leave from the end. Widths are measured after layout, so the row adapts as the window or the items change. Toolbars, tab strips, breadcrumbs and chip and avatar groups are built on it. The element is named by `Nazor.Controls.Primitives.OverflowContainer<TItem>.As`; each item is wrapped in a `{name}Item` element.<br>`TItem`. The type of the items. |
| `protected override void OnParametersSet()` | Recomputes which items fit. |
| `protected override void AddAttributes(RenderTreeBuilder builder)` | Adds the role and the width listener.<br>`builder`. The builder that receives the render frames. |
| `protected override void AddContent(RenderTreeBuilder builder)` | Adds the visible items and the overflow control.<br>`builder`. The builder that receives the render frames. |
| `public OverflowContainer()` |  |
| `public string As { get; set; }` | `[Parameter]` Initial value `"OverflowContainer"`.<br>Gets or sets the element name, `OverflowContainer` by default. |
| `public IReadOnlyList<TItem> Items { get; set; }` | `[Parameter]` Initial value `[]`.<br>Gets or sets the items. |
| `public RenderFragment<TItem>? ItemTemplate { get; set; }` | `[Parameter]` `[EditorRequired]`<br>Gets or sets how a visible item is rendered. |
| `public RenderFragment<IReadOnlyList<TItem>>? OverflowTemplate { get; set; }` | `[Parameter]` `[EditorRequired]`<br>Gets or sets how the hidden items are offered, usually a menu button; rendered only when some are hidden. |
| `public Func<TItem, int>? Priority { get; set; }` | `[Parameter]`<br>Gets or sets the priority of an item; lower priorities leave first. |
| `public double Gap { get; set; }` | `[Parameter]`<br>Gets or sets the space between items, in logical units, counted when fitting. |
| `public string? Role { get; set; }` | `[Parameter]`<br>Gets or sets the accessible role of the row, or null. |
| `public Func<TItem, IEnumerable<KeyValuePair<string, object?>>?>? ItemAttributes { get; set; }` | `[Parameter]`<br>Gets or sets extra attributes of the `{name}Item` wrapper of an item, such as the listener of an `Nazor.Controls.Primitives.IndicatorTracker<TKey>` that measures the item within the row. |
| `public RenderFragment? ChildContent { get; set; }` | `[Parameter]`<br>Gets or sets content rendered inside the row after the items, such as an `Nazor.Controls.Primitives.Indicator`. |
| `public IReadOnlyList<TItem> HiddenItems { get; }` | Gets the items currently hidden, in their original order. |
| `protected override string ElementName { get; }` | Gets the element name. |

### `OverlayAlignment`

Namespace `Nazor.Controls.Primitives`. [Source](../../Sources/Core/Nazor.Controls/Primitives/Overlays.cs)

| Declaration | Contract |
| --- | --- |
| `public enum OverlayAlignment` | How an anchored overlay lines up with its anchor along the side it opens on. |
| `Start = 0` | Aligned with the anchor's left edge, or its top edge on the left and right sides. |
| `Center = 1` | Centered on the anchor. |
| `End = 2` | Aligned with the anchor's right edge, or its bottom edge on the left and right sides. |

### `OverlayContainer`

Namespace `Nazor.Controls.Primitives`. [Source](../../Sources/Core/Nazor.Controls/Primitives/Overlays.cs)

| Declaration | Contract |
| --- | --- |
| `public abstract class OverlayContainer : ContentControl, IDisposable` | The shared behavior of overlay primitives: an open state that the parent can control (`@bind-IsOpen`), dismissal by the pointer outside or by Escape reported through `Nazor.Controls.Primitives.OverlayContainer.OnDismiss`, and focus restored on close. Click, key-down and wheel events do not bubble from the surface into its logical owner. |
| `protected override void OnParametersSet()` | Synchronizes the open state. |
| `protected override void BuildRenderTree(RenderTreeBuilder builder)` | Renders the surface while open and through its noninteractive retained exit.<br>`builder`. The builder that receives the render frames. |
| `protected virtual void RenderBefore(RenderTreeBuilder builder)` | Renders elements placed before the surface, such as a scrim.<br>`builder`. The builder that receives the render frames. |
| `protected override void AddAttributes(RenderTreeBuilder builder)` | Adds the overlay, dismissal and focus attributes.<br>`builder`. The builder that receives the render frames. |
| `protected abstract void AddSurfaceAttributes(RenderTreeBuilder builder)` | Adds attributes specific to the surface, with sequence numbers from 20 to 899.<br>`builder`. The builder that receives the render frames. |
| `public Task OpenAsync()` | Opens the overlay.<br>**Returns**. A task that completes when the change has been reported. |
| `public Task CloseAsync()` | Closes the overlay.<br>**Returns**. A task that completes when the change has been reported. |
| `public Task ToggleAsync()` | Opens the overlay when closed and closes it when open.<br>**Returns**. A task that completes when the change has been reported. |
| `protected Task SetOpenAsync(bool value)` | Changes the open state and reports it.<br>`value`. The new open state.<br>**Returns**. A task that completes when the change has been reported. |
| `protected OverlayContainer()` |  |
| `public string? As { get; set; }` | `[Parameter]`<br>Gets or sets the element name, so the surface can be styled like a part. |
| `public bool IsOpen { get; set; }` | `[Parameter]`<br>Gets or sets whether the overlay is open. |
| `public EventCallback<bool> IsOpenChanged { get; set; }` | `[Parameter]`<br>Gets or sets the callback invoked when the open state changes from inside the overlay. |
| `public bool CloseOnOutside { get; set; }` | `[Parameter]` Initial value `true`.<br>Gets or sets whether a press outside the overlay dismisses it. |
| `public bool CloseOnEscape { get; set; }` | `[Parameter]` Initial value `true`.<br>Gets or sets whether Escape dismisses the overlay. |
| `public EventCallback OnDismiss { get; set; }` | `[Parameter]`<br>Gets or sets the callback invoked when the user dismisses the overlay; it then closes. |
| `public ElementReference? ReturnFocus { get; set; }` | `[Parameter]`<br>Gets or sets the element that receives the focus when the overlay closes; null uses the one focused before. |
| `public Func<ElementReference?>? InitialFocus { get; set; }` | `[Parameter]`<br>Gets or sets a callback that chooses the element focused when the overlay opens. |
| `public TimeSpan? ExitDuration { get; set; }` | `[Parameter]` Optional override of the base surface NSS `overlay-exit-duration`; zero removes immediately. Nonnegative intervals up to `4294967294ms`; reduced motion always forces zero. |
| `public EventCallback OnExited { get; set; }` | `[Parameter]` Invoked once when closing retention completes; reopening cancels the pending completion. |
| `protected override string? ElementStyle { get; }` | Appends pointer interaction suppression while exiting. |
| `public virtual void Dispose()` | Detaches motion-preference notifications. Derived overrides must call the base implementation. |
| `public bool Opened { get; }` | Gets whether the overlay is currently open. |
| `protected abstract string DefaultName { get; }` | Gets the default element name. |
| `protected override string ElementName { get; }` | Gets the element name of the surface. |

The surface emits `exiting=true`, `disabled=true` and `semantic-hidden=true` during retention and suppresses light/Escape dismissal. A modal barrier and native popup ownership remain until removal; focus returns then. A scrim receives the exit marker too. High-level dialog, menu, popover, teaching-tip, selection and picker controls retain this primitive while their open state becomes false. [Lifecycle](../Guide/11-Commands-Menus-and-Popups.md#closing-motion)

### `OverlayPlacement`

Namespace `Nazor.Controls.Primitives`. [Source](../../Sources/Core/Nazor.Controls/Primitives/Overlays.cs)

| Declaration | Contract |
| --- | --- |
| `public enum OverlayPlacement` | The side of its anchor on which an anchored overlay opens; it moves to the opposite side when there is no room. |
| `Bottom = 0` | Below the anchor. |
| `Top = 1` | Above the anchor. |
| `Right = 2` | On the right of the anchor. |
| `Left = 3` | On the left of the anchor. |

### `PopoverContainer`

Namespace `Nazor.Controls.Primitives`. [Source](../../Sources/Core/Nazor.Controls/Primitives/Overlays.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed class PopoverContainer : OverlayContainer` | An anchored, non-modal surface without built-in content or look: it opens next to `Nazor.Controls.Primitives.PopoverContainer.Anchor` (or at `Nazor.Controls.Primitives.PopoverContainer.AnchorPoint`), moves to the other side when there is no room, closes on an outside press or Escape and returns the focus. Popovers, menus, drop-down lists and pickers are built on it. With `Nazor.Controls.Primitives.PopoverContainer.ShowArrow` it renders an arrow part (`{name}Arrow`) aligned to the arranged edge and anchor center. Its `placement` attribute records the requested side; use `:popup-above`, `:popup-left` and `:popup-right` on the surface for the arranged side, including flips. Arrow margins provide clearance from rounded corners. |
| `protected override void OnParametersSet()` | Validates the placement parameters. |
| `protected override void AddSurfaceAttributes(RenderTreeBuilder builder)` | Adds the anchoring, the role and the focus behavior.<br>`builder`. The builder that receives the render frames. |
| `protected override void AddContent(RenderTreeBuilder builder)` | Adds the arrow part and the content.<br>`builder`. The builder that receives the render frames. |
| `public PopoverContainer()` |  |
| `public ElementReference? Anchor { get; set; }` | `[Parameter]`<br>Gets or sets the element the surface is attached to. |
| `public (double X, double Y)? AnchorPoint { get; set; }` | `[Parameter]`<br>Gets or sets a point in window coordinates used as the anchor when `Nazor.Controls.Primitives.PopoverContainer.Anchor` is null. |
| `public OverlayPlacement Placement { get; set; }` | `[Parameter]`<br>Gets or sets the preferred side of the anchor. |
| `public OverlayAlignment Alignment { get; set; }` | `[Parameter]`<br>Gets or sets the alignment with the anchor. |
| `public ElementReference? AlignmentTarget { get; set; }` | `[Parameter]`<br>Aligns the vertical center of a visible, laid-out retained descendant with the real anchor center. Horizontal positioning follows `Alignment`. Work-area/native constraints may move the popup; hidden, removed or unrelated targets fall back to ordinary placement. |
| `public double Offset { get; set; }` | `[Parameter]`<br>Gets or sets the distance from the anchor, in logical units. |
| `public bool MatchAnchorWidth { get; set; }` | `[Parameter]`<br>Gets or sets whether the surface is at least as wide as its anchor, as a drop-down list. |
| `public bool FocusOnOpen { get; set; }` | `[Parameter]` Initial value `true`.<br>Gets or sets whether opening moves the focus into the surface. |
| `public bool AllowAnchorInteraction { get; set; }` | `[Parameter]` Gets or sets whether a press within the retained anchor interacts with this nonmodal popup instead of light-dismissing it. Defaults to false. Active modal restrictions still apply. Enabled anchors close or submit through their own actions. |
| `public string Role { get; set; }` | `[Parameter]` Initial value `"group"`.<br>Gets or sets the accessible role of the surface; `group` by default. |
| `public bool IsMenu { get; set; }` | `[Parameter]`<br>Gets or sets whether the surface uses menu keyboard behavior (arrows between items, Tab closes). |
| `public bool IsSubmenu { get; set; }` | `[Parameter]`<br>Gets or sets whether the surface is a nested menu that closes its sibling submenus. |
| `public bool ShowArrow { get; set; }` | `[Parameter]`<br>Gets or sets whether an arrow part is rendered. |
| `public bool OriginAtArrow { get; set; }` | `[Parameter]`<br>Gets or sets whether transforms originate at the arranged arrow tip instead of the NSS transform origin; false by default. Has no effect without an arrow and an anchor. |
| `protected override string DefaultName { get; }` | Gets the default element name. |

The alignment target can be supplied after its reference becomes available. The scene uses actual layout dimensions and passes the corresponding signed offset to native positioners. The compositor retains final placement authority.

### `Presence`

Namespace `Nazor.Controls.Primitives`. [Source](../../Sources/Core/Nazor.Controls/Primitives/Disclosure.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed class Presence : ComponentBase, IDisposable` | Keeps content rendered for `Nazor.Controls.Primitives.Presence.ExitDuration` after `Nazor.Controls.Primitives.Presence.Present` becomes false, so a theme can play an exit animation: the content receives `Nazor.Controls.Primitives.PresenceState.IsExiting` and usually writes it as an `exiting` attribute. Without a duration the content is removed at once, which is what themes without motion need. |
| `protected override Task OnParametersSetAsync()` | Starts or cancels the exit. |
| `protected override void BuildRenderTree(RenderTreeBuilder builder)` | Renders the content while present or exiting.<br>`builder`. The builder that receives the render frames. |
| `public void Dispose()` | Stops a pending exit. |
| `public Presence()` |  |
| `public bool Present { get; set; }` | `[Parameter]`<br>Gets or sets whether the content is wanted. |
| `public bool KeepMounted { get; set; }` | `[Parameter]` False by default. True renders the same subtree after exit completion, with state `(IsPresent:false, IsExiting:false)`; caller owns closed input/accessibility policy. |
| `public TimeSpan ExitDuration { get; set; }` | `[Parameter]`<br>Gets or sets how long exiting content stays rendered, from zero through `4294967294ms`; reopening cancels a pending completion. |
| `public RenderFragment<PresenceState>? ChildContent { get; set; }` | `[Parameter]`<br>Gets or sets the content, which receives the presence state. |
| `public EventCallback OnExited { get; set; }` | `[Parameter]`<br>Gets or sets the callback invoked when exit retention completes, including an immediate close. |

`OnExited` still runs once when the exit phase completes with kept content. Initial closed kept content does not invoke it. Reopening cancels a pending completion; releasing `KeepMounted` after completion disposes the child.

### `PresenceState`

Namespace `Nazor.Controls.Primitives`. [Source](../../Sources/Core/Nazor.Controls/Primitives/Disclosure.cs)

| Declaration | Contract |
| --- | --- |
| `public readonly struct PresenceState : IEquatable<PresenceState>` | The state given to the content of a `Nazor.Controls.Primitives.Presence`.<br>`IsPresent`. Whether the content is wanted; false while it exits.<br>`IsExiting`. Whether the content is playing its exit before being removed. |
| `public PresenceState()` |  |
| `public PresenceState(bool IsPresent, bool IsExiting)` | The state given to the content of a `Nazor.Controls.Primitives.Presence`.<br>`IsPresent`. Whether the content is wanted; false while it exits.<br>`IsExiting`. Whether the content is playing its exit before being removed. |
| `public override string ToString()` |  |
| `public static bool operator !=(PresenceState left, PresenceState right)` |  |
| `public static bool operator ==(PresenceState left, PresenceState right)` |  |
| `public override int GetHashCode()` |  |
| `public override bool Equals(object obj)` |  |
| `public bool Equals(PresenceState other)` |  |
| `public void Deconstruct(out bool IsPresent, out bool IsExiting)` |  |
| `public bool IsPresent { get; init; }` | Whether the content is wanted; false while it exits. |
| `public bool IsExiting { get; init; }` | Whether the content is playing its exit before being removed. |

### `Pressable`

Namespace `Nazor.Controls.Primitives`. [Source](../../Sources/Core/Nazor.Controls/Primitives/Pressable.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed class Pressable : ContentControl, IDisposable` | Turns any content into something pressable, without imposing a look: a click, Enter or Space invoke `Nazor.Controls.Primitives.Pressable.OnPress`; with `Nazor.Controls.Primitives.Pressable.RepeatInterval` the action repeats while the pointer holds the element, as the arrows of a scroll bar or a number field; `Nazor.Controls.Primitives.Pressable.OnLongPress` reports a press held for `Nazor.Controls.Primitives.Pressable.LongPressDelay`, after which the release does not count as a click. The element is named by `Nazor.Controls.Primitives.Pressable.As` so it can be styled like any part. |
| `protected override void OnParametersSet()` | Validates the timing parameters. |
| `protected override void AddAttributes(RenderTreeBuilder builder)` | Adds the role, the activation and the press tracking.<br>`builder`. The builder that receives the render frames. |
| `public void Dispose()` | Stops pending repetitions. |
| `public Pressable()` |  |
| `public string As { get; set; }` | `[Parameter]` Initial value `"Pressable"`.<br>Gets or sets the element name, `Pressable` by default. |
| `public string Role { get; set; }` | `[Parameter]` Initial value `"button"`.<br>Gets or sets the accessible role, `button` by default. |
| `public EventCallback<PointerEventArgs> OnPress { get; set; }` | `[Parameter]`<br>Gets or sets the callback invoked by a click, Enter, Space and each repetition. |
| `public EventCallback<PointerEventArgs> OnLongPress { get; set; }` | `[Parameter]`<br>Gets or sets the callback invoked when the pointer stays down for `Nazor.Controls.Primitives.Pressable.LongPressDelay`. |
| `public TimeSpan LongPressDelay { get; set; }` | `[Parameter]` Initial value `TimeSpan.FromMilliseconds(500)`.<br>Gets or sets how long a press must last to count as a long press. |
| `public TimeSpan? RepeatInterval { get; set; }` | `[Parameter]`<br>Gets or sets the interval between repetitions while held, or null for no repetition. |
| `public TimeSpan RepeatDelay { get; set; }` | `[Parameter]` Initial value `TimeSpan.FromMilliseconds(400)`.<br>Gets or sets the delay before the first repetition. |
| `public bool Disabled { get; set; }` | `[Parameter]`<br>Gets or sets whether the element rejects interaction. |
| `public bool Focusable { get; set; }` | `[Parameter]` Initial value `true`.<br>Gets or sets whether the element is a Tab stop; true by default. |
| `protected override string ElementName { get; }` | Gets the element name. |

| Declaration | Contract |
| --- | --- |
| `public TimeProvider Time { get; set; }` | `[CascadingParameter]` `TimeProvider.System` by default; replacement cancels held input. |

With `RepeatInterval`, primary pointer/focused Enter/Space invokes once on press and then after `RepeatDelay` (400ms by default) at the configured interval. Release adds no action and OS key-repeat does not restart timing. Capture/leave/focus/Escape/disabled/dispose cleanup rejects queued stale work. Interactive descendants keep their own gesture. `OnLongPress` remains pointer-only and consumes release activation. Interval/delay/long-press durations are validated against nonnegative timer-supported bounds; intervals must be positive.

### `ResizeEdge`

Namespace `Nazor.Controls.Primitives`. [Source](../../Sources/Core/Nazor.Controls/Primitives/Dragging.cs)

| Declaration | Contract |
| --- | --- |
| `public enum ResizeEdge` | Which edge or corner a `Nazor.Controls.Primitives.ResizeHandle` moves. |
| `Right = 0` | The right edge: dragging right grows the width. |
| `Left = 1` | The left edge: dragging left grows the width. |
| `Bottom = 2` | The bottom edge: dragging down grows the height. |
| `Top = 3` | The top edge: dragging up grows the height. |
| `BottomRight = 4` | The bottom-right corner: both dimensions. |

### `ResizeHandle`

Namespace `Nazor.Controls.Primitives`. [Source](../../Sources/Core/Nazor.Controls/Primitives/Dragging.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed class ResizeHandle : Control` | A handle that resizes something it does not own: dragging it (or, when focused, the arrow keys) reports new sizes computed from `Nazor.Controls.Primitives.ResizeHandle.Width` and `Nazor.Controls.Primitives.ResizeHandle.Height` at the start, clamped to the limits. The caller applies them. Column borders, resizable dialogs, side panes and the grip of a text area are built on it. The element is `ResizeHandle` with an `edge` attribute, and the `splitter` role for assistive technologies. |
| `protected override void OnParametersSet()` | Validates the limits. |
| `protected override void AddAttributes(RenderTreeBuilder builder)` | Adds the role, the edge and the pointer and keyboard handling.<br>`builder`. The builder that receives the render frames. |
| `public ResizeHandle()` |  |
| `public ResizeEdge Edge { get; set; }` | `[Parameter]`<br>Gets or sets the edge or corner moved by the handle. |
| `public double Width { get; set; }` | `[Parameter]`<br>Gets or sets the current width of the resized element. |
| `public double Height { get; set; }` | `[Parameter]`<br>Gets or sets the current height of the resized element. |
| `public double MinSize { get; set; }` | `[Parameter]`<br>Gets or sets the smallest size allowed. |
| `public double MaxSize { get; set; }` | `[Parameter]` Initial value `double.PositiveInfinity`.<br>Gets or sets the largest size allowed. |
| `public double KeyboardStep { get; set; }` | `[Parameter]` Initial value `10`.<br>Gets or sets the change made by one arrow key press. |
| `public bool Disabled { get; set; }` | `[Parameter]`<br>Gets or sets whether resizing is unavailable. |
| `public EventCallback<ResizeValue> OnResize { get; set; }` | `[Parameter]`<br>Gets or sets the callback invoked with each new size. |
| `public EventCallback<ResizeValue> OnResizeEnd { get; set; }` | `[Parameter]`<br>Gets or sets the callback invoked with the final size when the pointer is released. |
| `protected override string ElementName { get; }` | Gets the NSS element name emitted by ResizeHandle. |

### `ResizeValue`

Namespace `Nazor.Controls.Primitives`. [Source](../../Sources/Core/Nazor.Controls/Primitives/Dragging.cs)

| Declaration | Contract |
| --- | --- |
| `public readonly struct ResizeValue : IEquatable<ResizeValue>` | A width and a height reported by a `Nazor.Controls.Primitives.ResizeHandle`.<br>`Width`. The new width, or the starting one when the edge does not change it.<br>`Height`. The new height, or the starting one when the edge does not change it. |
| `public ResizeValue()` |  |
| `public ResizeValue(double Width, double Height)` | A width and a height reported by a `Nazor.Controls.Primitives.ResizeHandle`.<br>`Width`. The new width, or the starting one when the edge does not change it.<br>`Height`. The new height, or the starting one when the edge does not change it. |
| `public override string ToString()` |  |
| `public static bool operator !=(ResizeValue left, ResizeValue right)` |  |
| `public static bool operator ==(ResizeValue left, ResizeValue right)` |  |
| `public override int GetHashCode()` |  |
| `public override bool Equals(object obj)` |  |
| `public bool Equals(ResizeValue other)` |  |
| `public void Deconstruct(out double Width, out double Height)` |  |
| `public double Width { get; init; }` | The new width, or the starting one when the edge does not change it. |
| `public double Height { get; init; }` | The new height, or the starting one when the edge does not change it. |

### `RovingFocusController`

Namespace `Nazor.Controls.Primitives`. [Source](../../Sources/Core/Nazor.Controls/Primitives/RovingFocus.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed class RovingFocusController` | The keyboard rules of a group with a single Tab stop: arrow keys move the focus between items along an `Nazor.Controls.Primitives.RovingFocusController.Orientation` or in a grid of `Nazor.Controls.Primitives.RovingFocusController.Columns`, Home and End jump to the ends (Ctrl with them in a grid), Page Up and Page Down move by `Nazor.Controls.Primitives.RovingFocusController.PageSize`, and typing letters jumps to the next item whose text starts with them. Disabled items are skipped. Lists, grids, calendars, segmented controls and chip groups share it; `Nazor.Controls.Primitives.RovingFocusGroup` applies it to arbitrary content. |
| `public int TabStop(int count, Func<int, bool> isDisabled)` | Gets the index of the item that holds the Tab stop.<br>`count`. The number of items.<br>`isDisabled`. Tells whether an item is disabled.<br>**Returns**. The index, or -1 when every item is disabled. |
| `public int? Move(KeyboardEventArgs e, int current, int count, Func<int, bool> isDisabled, Func<int, string?>? text = null)` | Computes the item that a key moves the focus to.<br>`e`. The key event.<br>`current`. The index of the focused item.<br>`count`. The number of items.<br>`isDisabled`. Tells whether an item is disabled.<br>`text`. Gives the text of an item for typeahead, or null.<br>**Returns**. The target index, or null when the key does not move the focus. |
| `public RovingFocusController()` |  |
| `public Orientation? Orientation { get; set; }` | Gets or sets the arrow axis; null accepts both axes, as a list that can also be read across. |
| `public int Columns { get; set; }` | Gets or sets the number of columns of a grid; zero for a single row or column. |
| `public bool Loop { get; set; }` | Gets or sets whether moving past an end continues from the other end. |
| `public bool Typeahead { get; set; }` | Gets or sets whether typing letters jumps to a matching item. |
| `public int PageSize { get; set; }` | Gets or sets how many items Page Up and Page Down move; zero ignores those keys. |
| `public bool RightToLeft { get; set; }` | Gets or sets whether the layout runs right to left, which swaps the left and right arrows. |
| `public int ActiveIndex { get; set; }` | Gets or sets the index of the item that holds the Tab stop, or -1 for the first enabled item. |

### `RovingFocusGroup`

Namespace `Nazor.Controls.Primitives`. [Source](../../Sources/Core/Nazor.Controls/Primitives/RovingFocus.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed class RovingFocusGroup : ContentControl` | Gives arbitrary content a single Tab stop with arrow-key movement between its `Nazor.Controls.Primitives.RovingFocusItem` children, following the rules of `Nazor.Controls.Primitives.RovingFocusController`. The element is named by `Nazor.Controls.Primitives.RovingFocusGroup.As` and carries `Nazor.Controls.Primitives.RovingFocusGroup.Role`, so it can be a toolbar of custom buttons, a grid of cards or a list of tiles. |
| `protected override void OnParametersSet()` | Configures the keyboard rules. |
| `protected override void AddAttributes(RenderTreeBuilder builder)` | Adds the role and the keyboard handling.<br>`builder`. The builder that receives the render frames. |
| `protected override void AddContent(RenderTreeBuilder builder)` | Supplies the group to its items.<br>`builder`. The builder that receives the render frames. |
| `protected override void OnAfterRender(bool firstRender)` | Gives the Tab stop to the right item once the items are laid out.<br>`firstRender`. Whether this is the first render. |
| `public RovingFocusGroup()` |  |
| `public string As { get; set; }` | `[Parameter]` Initial value `"RovingFocusGroup"`.<br>Gets or sets the element name, `RovingFocusGroup` by default. |
| `public string Role { get; set; }` | `[Parameter]` Initial value `"group"`.<br>Gets or sets the accessible role of the group, `group` by default. |
| `public Orientation? Orientation { get; set; }` | `[Parameter]` Initial value `Controls.Orientation.Horizontal`.<br>Gets or sets the arrow axis; null accepts both. |
| `public int Columns { get; set; }` | `[Parameter]`<br>Gets or sets the number of columns of a grid; zero for a single row or column. |
| `public bool Loop { get; set; }` | `[Parameter]`<br>Gets or sets whether moving past an end continues from the other end. |
| `public bool Typeahead { get; set; }` | `[Parameter]` Initial value `true`.<br>Gets or sets whether typing letters jumps to a matching item. |
| `protected override string ElementName { get; }` | Gets the element name. |

### `RovingFocusItem`

Namespace `Nazor.Controls.Primitives`. [Source](../../Sources/Core/Nazor.Controls/Primitives/RovingFocus.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed class RovingFocusItem : ContentControl, IDisposable` | An item of a `Nazor.Controls.Primitives.RovingFocusGroup`: it is focusable, holds the group's Tab stop when it is the active item, and becomes active when it receives the focus. The element is named by `Nazor.Controls.Primitives.RovingFocusItem.As`. |
| `protected override void OnParametersSet()` | Requires an enclosing group. |
| `public void Dispose()` | Leaves the group. |
| `protected override void AddAttributes(RenderTreeBuilder builder)` | Adds the identity, the Tab stop and the activation on focus.<br>`builder`. The builder that receives the render frames. |
| `public RovingFocusItem()` |  |
| `public string As { get; set; }` | `[Parameter]` Initial value `"RovingFocusItem"`.<br>Gets or sets the element name, `RovingFocusItem` by default. |
| `public string? Role { get; set; }` | `[Parameter]`<br>Gets or sets the accessible role of the item. |
| `public bool Disabled { get; set; }` | `[Parameter]`<br>Gets or sets whether the item is skipped and rejects interaction. |
| `public RovingFocusGroup? Group { get; set; }` | `[CascadingParameter]`<br>Gets or sets the enclosing group, supplied automatically. |
| `protected override string ElementName { get; }` | Gets the element name. |

### `SelectionMode`

Namespace `Nazor.Controls.Primitives`. [Source](../../Sources/Core/Nazor.Controls/Primitives/SelectionModel.cs)

| Declaration | Contract |
| --- | --- |
| `public enum SelectionMode` | How many items a `Nazor.Controls.Primitives.SelectionModel<TKey>` can select. |
| `None = 0` | Nothing can be selected. |
| `Single = 1` | At most one item is selected. |
| `Multiple = 2` | Any number of items can be selected, with ranges and toggles. |

### `SelectionModel<TKey>`

Namespace `Nazor.Controls.Primitives`. [Source](../../Sources/Core/Nazor.Controls/Primitives/SelectionModel.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed class SelectionModel<TKey> where TKey : notnull` | The selection of a collection, independent of how the items are drawn: single or multiple selection, a range from the anchor with Shift, a toggle with Ctrl, select all, and whether the selection may become empty. It holds keys, so the same model serves lists, trees, grids, tabs and segmented controls. The order used for ranges is supplied by the caller.<br>`TKey`. The type of the item keys. |
| `public SelectionModel(SelectionMode mode = SelectionMode.Single, IEqualityComparer<TKey>? comparer = null)` | Creates an empty selection.<br>`mode`. How many items can be selected.<br>`comparer`. The key comparer, or null for the default one. |
| `public void SetOrder(IEnumerable<TKey> keys)` | Sets the order of the current items, used for ranges and for `Nazor.Controls.Primitives.SelectionModel<TKey>.SelectAll`.<br>`keys`. The keys of every item, in display order. |
| `public bool IsSelected(TKey key)` | Gets whether a key is selected.<br>`key`. The item key.<br>**Returns**. true if the key is selected. |
| `public bool Select(TKey key)` | Replaces the selection with a single key, as a plain click or an arrow key does.<br>`key`. The item key.<br>**Returns**. true if the selection changed. |
| `public bool Toggle(TKey key)` | Adds or removes one key, as Ctrl+click does; in single mode it selects or clears that key.<br>`key`. The item key.<br>**Returns**. true if the selection changed. |
| `public bool Extend(TKey key, bool keepExisting = false)` | Selects the keys between the anchor and a key, as Shift+click does.<br>`key`. The end of the range.<br>`keepExisting`. Whether the range is added to the current selection (Ctrl+Shift).<br>**Returns**. true if the selection changed. |
| `public bool SelectAll()` | Selects every key of the current order in multiple mode.<br>**Returns**. true if the selection changed. |
| `public bool Clear()` | Removes every key from the selection.<br>**Returns**. true if the selection changed. |
| `public bool Replace(IEnumerable<TKey> keys)` | Replaces the selection with the given keys, trimmed to the selection mode, without raising user semantics.<br>`keys`. The keys to select.<br>**Returns**. true if the selection changed. |
| `public bool Prune()` | Removes keys that are no longer in the current order, after the items changed.<br>**Returns**. true if the selection changed. |
| `public SelectionMode Mode { get; set; }` | Gets or sets how many items can be selected; reducing it trims the selection. |
| `public bool AllowEmpty { get; set; }` | Gets or sets whether a user action may leave nothing selected (Ctrl+click on the only selected item). |
| `public TKey? Anchor { get; }` | Gets the key from which Shift extends a range, or default when there is none. |
| `public IReadOnlyList<TKey> SelectedKeys { get; }` | Gets the selected keys, in the order of the current items when known. |
| `public int Count { get; }` | Gets the number of selected keys. |
| `public TKey? SelectedKey { get; }` | Gets the first selected key in item order, or default when nothing is selected. |
| `public event Action? Changed` | Raised after every change of the selected keys. |

### `Shortcut`

Namespace `Nazor.Controls.Primitives`. [Source](../../Sources/Core/Nazor.Controls/Primitives/Commands.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed class Shortcut : ComponentBase, IDisposable` | A single keyboard shortcut that invokes a callback while the component is rendered. |
| `protected override void OnParametersSet()` | Registers the gesture when it changes. |
| `public void Dispose()` | Removes the registration. |
| `public Shortcut()` |  |
| `public KeyBindings Bindings { get; set; }` | `[Inject]`<br>Gets or sets the window's shortcut registry. |
| `public KeyGesture Gesture { get; set; }` | `[Parameter]` `[EditorRequired]`<br>Gets or sets the key combination, for example `KeyGesture.Parse("Mod+K")`. |
| `public EventCallback<KeyboardEventArgs> OnInvoke { get; set; }` | `[Parameter]`<br>Gets or sets the callback invoked when the keys are pressed. |
| `public bool AllowRepeat { get; set; }` | `[Parameter]`<br>Gets or sets whether holding the keys repeats the callback. |

### `ShortcutScope`

Namespace `Nazor.Controls.Primitives`. [Source](../../Sources/Core/Nazor.Controls/Primitives/Commands.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed class ShortcutScope : ComponentBase, IDisposable` | Makes the shortcuts of a set of commands work while it is rendered: each enabled `Nazor.Controls.Primitives.Command` with a `Nazor.Controls.Primitives.Command.Gesture` runs when its keys are pressed anywhere in the window, before the focused control sees them. The latest registration of a gesture wins, so a page can override an application shortcut. |
| `protected override void OnParametersSet()` | Registers the current commands. |
| `protected override void BuildRenderTree(RenderTreeBuilder builder)` | Renders the child content.<br>`builder`. The builder that receives the render frames. |
| `public void Dispose()` | Removes the registrations. |
| `public ShortcutScope()` |  |
| `public KeyBindings Bindings { get; set; }` | `[Inject]`<br>Gets or sets the window's shortcut registry. |
| `public IEnumerable<Command>? Commands { get; set; }` | `[Parameter]`<br>Gets or sets the commands whose shortcuts are active. |
| `public bool Enabled { get; set; }` | `[Parameter]` Initial value `true`.<br>Gets or sets whether the shortcuts are active; false keeps them registered but ignored. |
| `public RenderFragment? ChildContent { get; set; }` | `[Parameter]`<br>Gets or sets content rendered unchanged. |

### `SizeObserver`

Namespace `Nazor.Controls.Primitives`. [Source](../../Sources/Core/Nazor.Controls/Primitives/Measurement.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed class SizeObserver : ContentControl` | Reports the laid-out size of its content: `Nazor.Controls.Primitives.SizeObserver.OnResize` receives the width, the height and the position within the parent element after the first layout and after every change, as a `ResizeObserver`. Use it for layouts that change shape with their own space, such as a navigation pane that collapses when narrow. |
| `protected override void AddAttributes(RenderTreeBuilder builder)` | Adds the resize listener.<br>`builder`. The builder that receives the render frames. |
| `public SizeObserver()` |  |
| `public string As { get; set; }` | `[Parameter]` Initial value `"SizeObserver"`.<br>Gets or sets the element name, `SizeObserver` by default. |
| `public EventCallback<ResizeEventArgs> OnResize { get; set; }` | `[Parameter]`<br>Gets or sets the callback invoked with the new box. |
| `public double Width { get; }` | Gets the last reported width, or zero before the first layout. |
| `public double Height { get; }` | Gets the last reported height, or zero before the first layout. |
| `protected override string ElementName { get; }` | Gets the element name. |

### `SpinRange`

Namespace `Nazor.Controls.Primitives`. [Source](../../Sources/Core/Nazor.Controls/Primitives/EditableText.cs)

| Declaration | Contract |
| --- | --- |
| `public readonly struct SpinRange : IEquatable<SpinRange>` | The numeric range stepped by the keys of an `Nazor.Controls.Primitives.EditableText` with `Nazor.Controls.Primitives.EditableText.Spin`.<br>`Value`. The current numeric value of the text.<br>`Minimum`. The lower bound.<br>`Maximum`. The upper bound.<br>`Step`. The change made by Up and Down.<br>`LargeStep`. The change made by Page Up and Page Down. |
| `public SpinRange()` |  |
| `public SpinRange(double Value, double Minimum, double Maximum, double Step = 1, double LargeStep = 10)` | The numeric range stepped by the keys of an `Nazor.Controls.Primitives.EditableText` with `Nazor.Controls.Primitives.EditableText.Spin`.<br>`Value`. The current numeric value of the text.<br>`Minimum`. The lower bound.<br>`Maximum`. The upper bound.<br>`Step`. The change made by Up and Down.<br>`LargeStep`. The change made by Page Up and Page Down. |
| `public override string ToString()` |  |
| `public static bool operator !=(SpinRange left, SpinRange right)` |  |
| `public static bool operator ==(SpinRange left, SpinRange right)` |  |
| `public override int GetHashCode()` |  |
| `public override bool Equals(object obj)` |  |
| `public bool Equals(SpinRange other)` |  |
| `public void Deconstruct(out double Value, out double Minimum, out double Maximum, out double Step, out double LargeStep)` |  |
| `public double Value { get; init; }` | The current numeric value of the text. |
| `public double Minimum { get; init; }` | The lower bound. |
| `public double Maximum { get; init; }` | The upper bound. |
| `public double Step { get; init; }` | The change made by Up and Down. |
| `public double LargeStep { get; init; }` | The change made by Page Up and Page Down. |

### `TextMask`

Namespace `Nazor.Controls.Primitives`. [Source](../../Sources/Core/Nazor.Controls/Primitives/TextMask.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed class TextMask` | An input mask such as `(000) 000-0000` or `AA-0000`, and the rules that fit typed or pasted text into it. Mask characters: `0` a digit, `9` an optional digit, `A` a letter, `a` an optional letter, `*` any character, `\` makes the next character literal; every other character is a literal written by the mask. `Nazor.Controls.Primitives.TextMask.Apply(System.String,System.Char)` reads text left to right: a character that fits the next slot fills it (optional slots that it does not fit are skipped), a character equal to the next literal is kept, any other character is dropped. Literals the user did not type are written only before the next filled slot, so the text never ends with a literal the user would have to delete, and deleting the last character also removes the literals before it. |
| `public TextMask(string mask)` | Parses a mask.<br>`mask`. The mask pattern.<br>**Throws** `System.ArgumentException`. The mask is empty, has no slot or ends with an escape. |
| `public MaskResult Apply(string input, char prompt = '_')` | Fits text into the mask.<br>`input`. The typed, pasted or stored text, raw or formatted.<br>`prompt`. The character shown for each empty position in `Nazor.Controls.Primitives.MaskResult.Prompt`.<br>**Returns**. The formatted text, the raw characters, the completion and the remaining prompt. |
| `public string Pattern { get; }` | Gets the mask pattern. |
| `public int SlotCount { get; }` | Gets the number of editable positions. |
| `public int RequiredSlotCount { get; }` | Gets the number of positions that must be filled. |

### `ToastEntry`

Namespace `Nazor.Controls.Primitives`. [Source](../../Sources/Core/Nazor.Controls/Primitives/ToastQueue.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed class ToastEntry` | A notification held by a `Nazor.Controls.Primitives.ToastQueue`. |
| `public TimeSpan? Remaining(DateTimeOffset now)` | Gets the time left before the notification closes by itself, or null when it stays until dismissed. |
| `public long Id { get; }` | Gets the identifier, unique within its queue. |
| `public ToastOptions Options { get; }` | Gets the content and behavior requested for the notification. |
| `public bool IsVisible { get; }` | Gets whether the notification is on screen rather than waiting for room. |
| `public bool IsPaused { get; }` | Gets whether the countdown is suspended, because the pointer or the focus is on it or the window is inactive. |

### `ToastOptions`

Namespace `Nazor.Controls.Primitives`. [Source](../../Sources/Core/Nazor.Controls/Primitives/ToastQueue.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed record ToastOptions : IEquatable<ToastOptions>` | What a notification shows and how long it stays. |
| `public override string ToString()` |  |
| `public static bool operator !=(ToastOptions? left, ToastOptions? right)` |  |
| `public static bool operator ==(ToastOptions? left, ToastOptions? right)` |  |
| `public override int GetHashCode()` |  |
| `public override bool Equals(object? obj)` |  |
| `public bool Equals(ToastOptions? other)` |  |
| `public ToastOptions()` |  |
| `public required string Message { get; init; }` | Gets the main text, also announced to assistive technologies. |
| `public string? Title { get; init; }` | Gets an optional bold title shown before the message. |
| `public Severity Severity { get; init; }` | Gets the severity, which selects the mark and the announcement urgency. |
| `public TimeSpan? Duration { get; init; }` | Gets how long the notification stays; null keeps it until it is dismissed. Defaults to five seconds. |
| `public string? ActionText { get; init; }` | Gets the text of an optional action button, such as Undo. |
| `public Func<Task>? Action { get; init; }` | Gets the work run when the action button is pressed; the notification closes afterwards. |
| `public bool Dismissible { get; init; }` | Gets whether a close button is shown. |

### `ToastQueue`

Namespace `Nazor.Controls.Primitives`. [Source](../../Sources/Core/Nazor.Controls/Primitives/ToastQueue.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed class ToastQueue` | The queue behind toast notifications, independent of how they are drawn: a limit on the visible ones, the order of arrival, countdowns that pause while the pointer or the focus is on a notification or the window is inactive, and expiry. A region component draws `Nazor.Controls.Primitives.ToastQueue.Visible` and calls `Nazor.Controls.Primitives.ToastQueue.Expire` when `Nazor.Controls.Primitives.ToastQueue.NextDeadline` passes. Every member must be used from the UI thread.<br>`time`. The clock used for countdowns, or null for the system clock. |
| `public ToastQueue(TimeProvider? time = null)` | The queue behind toast notifications, independent of how they are drawn: a limit on the visible ones, the order of arrival, countdowns that pause while the pointer or the focus is on a notification or the window is inactive, and expiry. A region component draws `Nazor.Controls.Primitives.ToastQueue.Visible` and calls `Nazor.Controls.Primitives.ToastQueue.Expire` when `Nazor.Controls.Primitives.ToastQueue.NextDeadline` passes. Every member must be used from the UI thread.<br>`time`. The clock used for countdowns, or null for the system clock. |
| `public ToastEntry Show(ToastOptions options)` | Adds a notification; it appears at once when there is room.<br>`options`. The content and behavior of the notification.<br>**Returns**. The queued notification. |
| `public ToastEntry Show(string message, Severity severity = Severity.Info)` | Adds a plain notification with a message.<br>`message`. The text to show.<br>`severity`. The severity of the message.<br>**Returns**. The queued notification. |
| `public bool Dismiss(ToastEntry entry)` | Removes a notification, visible or waiting.<br>`entry`. The notification to remove.<br>**Returns**. true if the notification was in the queue. |
| `public void Clear()` | Removes every notification. |
| `public void Pause(ToastEntry entry)` | Suspends the countdown of a notification; calls nest and must be balanced by `Nazor.Controls.Primitives.ToastQueue.Resume(Nazor.Controls.Primitives.ToastEntry)`.<br>`entry`. The notification under the pointer or the focus. |
| `public void Resume(ToastEntry entry)` | Resumes a countdown suspended by `Nazor.Controls.Primitives.ToastQueue.Pause(Nazor.Controls.Primitives.ToastEntry)`.<br>`entry`. The notification that the pointer or the focus left. |
| `public void PauseAll()` | Suspends every countdown, for example while the window is inactive. |
| `public void ResumeAll()` | Resumes the countdowns suspended by `Nazor.Controls.Primitives.ToastQueue.PauseAll`. |
| `public bool Expire()` | Removes the visible notifications whose time is over.<br>**Returns**. true if any notification expired. |
| `public int MaxVisible { get; set; }` | Gets or sets how many notifications are on screen at once; the others wait in order. |
| `public IReadOnlyList<ToastEntry> Visible { get; }` | Gets the notifications on screen, oldest first. |
| `public int PendingCount { get; }` | Gets the number of notifications waiting for room. |
| `public DateTimeOffset? NextDeadline { get; }` | Gets the moment at which the next visible notification expires, or null when none is counting down. |
| `public event Action? Changed` | Raised after notifications are added, shown, paused, resumed or removed. |

### `TooltipContainer`

Namespace `Nazor.Controls.Primitives`. [Source](../../Sources/Core/Nazor.Controls/Primitives/Overlays.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed class TooltipContainer : ContentControl, ITooltipController, IDisposable` | A non-interactive description that appears while the pointer rests on its `Nazor.Controls.Primitives.TooltipContainer.Trigger` or the keyboard focus reaches it, using the window's shared tooltip timing, and closes with Escape. The trigger wrapper adds no Tab stop. Hovering the noninteractive content keeps it open while clicks pass through the surface. The surface (`Nazor.Controls.Primitives.TooltipContainer.As`, `TooltipContainer` by default) must not contain focusable or clickable content; use a `Nazor.Controls.Primitives.PopoverContainer` for that. |
| `protected override void OnParametersSet()` | Validates the slots. |
| `protected override void BuildRenderTree(RenderTreeBuilder builder)` | Renders the trigger wrapper and, while shown, the surface; the surface stays for the description relationship.<br>`builder`. The builder that receives the render frames. |
| `protected override void AddAttributes(RenderTreeBuilder builder)` | Adds the tooltip semantics and placement.<br>`builder`. The builder that receives the render frames. |
| `protected override void OnAfterRender(bool firstRender)` | Rejects interactive content.<br>`firstRender`. Whether this is the first render. |
| `public Task OpenAsync()` | Shows the tooltip unless it is unavailable.<br>**Returns**. A completed task. |
| `public Task CloseAsync()` | Hides the tooltip.<br>**Returns**. A completed task. |
| `public void Dispose()` | Marks the tooltip unavailable to pending timers and detaches host motion notifications. |
| `public TooltipContainer()` |  |
| `public string? As { get; set; }` | `[Parameter]`<br>Gets or sets the element name of the surface. |
| `public RenderFragment? Trigger { get; set; }` | `[Parameter]` `[EditorRequired]`<br>Gets or sets the content that shows the tooltip. |
| `public OverlayPlacement Placement { get; set; }` | `[Parameter]` Initial value `OverlayPlacement.Bottom`.<br>Gets or sets the preferred side of the trigger. |
| `public OverlayAlignment Alignment { get; set; }` | `[Parameter]`<br>Gets or sets the alignment with the trigger. |
| `public double Offset { get; set; }` | `[Parameter]`<br>Gets or sets the distance from the trigger, in logical units. |
| `public bool Disabled { get; set; }` | `[Parameter]`<br>Gets or sets whether the tooltip is unavailable. |
| `public TimeSpan? Delay { get; set; }` | `[Parameter]`<br>Gets or sets an opening delay; null uses the window's shared tooltip delay. |
| `public bool Opened { get; }` | Gets whether the tooltip is shown. |
| `protected override string ElementName { get; }` | Gets the element name of the surface. |
| `protected override string? ElementStyle { get; }` | Gets the caller's declarations, hiding the retained surface while closed. |

The visible surface remains through the theme exit and emits `exiting=true`; it is noninteractive and semantically hidden while closing. After retention a hidden surface with the same ID preserves the trigger description relationship. Reduced motion completes the visible exit immediately.

### `TreeCheckState`

Namespace `Nazor.Controls.Primitives`. [Source](../../Sources/Core/Nazor.Controls/Primitives/TreeModel.cs)

| Declaration | Contract |
| --- | --- |
| `public enum TreeCheckState` | The check state of a tree item whose children are checked in cascade. |
| `Unchecked = 0` | Neither the item nor any descendant is checked. |
| `Mixed = 1` | Some descendants are checked. |
| `Checked = 2` | The item and all its descendants are checked. |

### `TreeModel<TItem>`

Namespace `Nazor.Controls.Primitives`. [Source](../../Sources/Core/Nazor.Controls/Primitives/TreeModel.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed class TreeModel<TItem> where TItem : notnull` | The structure of a hierarchy, independent of how it is drawn: roots and children given by selectors, the expanded items, the flattened visible rows that a `Nazor.Controls.Primitives.Virtualizer<TItem>` can render, children loaded on demand, and checking in cascade, where a parent is checked, unchecked or mixed according to its children. Trees, nested navigation, property grids and grouped grids share it. Every member must be used from the UI thread.<br>`TItem`. The type of the items. |
| `public TreeModel(IEqualityComparer<TItem>? comparer = null)` | Creates a tree model.<br>`comparer`. The item comparer, or null for the default one. |
| `public IReadOnlyList<TItem> ChildrenOf(TItem item)` | Gets the children of an item that are known, without loading.<br>`item`. The item.<br>**Returns**. The children, empty when unknown. |
| `public bool CanExpand(TItem item)` | Gets whether an item can be expanded.<br>`item`. The item.<br>**Returns**. true if the item has or may have children. |
| `public bool IsExpanded(TItem item)` | Gets whether an item is expanded.<br>`item`. The item.<br>**Returns**. true if the item is expanded. |
| `public bool IsLoading(TItem item)` | Gets whether an item's children are being loaded.<br>`item`. The item.<br>**Returns**. true while loading. |
| `public Task SetExpandedAsync(TItem item, bool value)` | Expands or collapses an item, loading its children the first time when needed.<br>`item`. The item.<br>`value`. Whether the item should be expanded.<br>**Returns**. A task that completes when the children are available. |
| `public bool Reveal(TItem item)` | Expands every ancestor of an item so that it becomes visible.<br>`item`. The item to reveal.<br>**Returns**. true if the item was found among the known items. |
| `public IReadOnlyList<TItem>? PathTo(TItem item)` | Gets the chain of items from a root to an item, or null when it is not among the known items.<br>`item`. The item.<br>**Returns**. The path, ending with the item. |
| `public TItem? ParentOf(TItem item)` | Gets the parent of a visible or known item.<br>`item`. The item.<br>**Returns**. The parent, or default for a root or an unknown item. |
| `public IReadOnlyList<TreeRow<TItem>> VisibleRows()` | Flattens the visible rows: every root and the children of every expanded item, in display order.<br>**Returns**. The visible rows. |
| `public TreeCheckState CheckState(TItem item)` | Gets the check state of an item from its own state or, for a parent, from its descendants.<br>`item`. The item.<br>**Returns**. The check state. |
| `public void SetChecked(TItem item, bool value)` | Checks or unchecks an item and every known descendant.<br>`item`. The item.<br>`value`. Whether the item should be checked. |
| `public IReadOnlyCollection<TItem> CheckedLeaves()` | Gets the checked items without checked-state descendants to report, that is the checked leaves.<br>**Returns**. The checked leaves. |
| `public void CollapseAll()` | Collapses every item. |
| `public IReadOnlyList<TItem> Roots { get; set; }` | Gets or sets the top-level items. |
| `public Func<TItem, IReadOnlyList<TItem>?>? Children { get; set; }` | Gets or sets the function that gives an item's children. Return null for items that still need to load. |
| `public Func<TItem, bool>? HasChildren { get; set; }` | Gets or sets the function that tells whether an item has children before they are loaded. |
| `public Func<TItem, Task<IReadOnlyList<TItem>>>? LoadChildren { get; set; }` | Gets or sets the function that loads an item's children when it is first expanded. |
| `public event Action? Changed` | Raised after the expansion, the loaded children or the check states change. |

### `TreeRow<TItem>`

Namespace `Nazor.Controls.Primitives`. [Source](../../Sources/Core/Nazor.Controls/Primitives/TreeModel.cs)

| Declaration | Contract |
| --- | --- |
| `public readonly struct TreeRow<TItem> : IEquatable<TreeRow<TItem>>` | A visible row of a `Nazor.Controls.Primitives.TreeModel<TItem>`, in display order.<br>`TItem`. The type of the items.<br>`Item`. The item.<br>`Level`. The depth, one for the roots.<br>`Parent`. The parent item, or default for a root.<br>`HasChildren`. Whether the item can be expanded.<br>`IsExpanded`. Whether the item is expanded.<br>`PositionInSet`. The one-based position among its siblings.<br>`SizeOfSet`. The number of its siblings, itself included. |
| `public TreeRow()` |  |
| `public TreeRow(TItem Item, int Level, TItem? Parent, bool HasChildren, bool IsExpanded, int PositionInSet, int SizeOfSet)` | A visible row of a `Nazor.Controls.Primitives.TreeModel<TItem>`, in display order.<br>`TItem`. The type of the items.<br>`Item`. The item.<br>`Level`. The depth, one for the roots.<br>`Parent`. The parent item, or default for a root.<br>`HasChildren`. Whether the item can be expanded.<br>`IsExpanded`. Whether the item is expanded.<br>`PositionInSet`. The one-based position among its siblings.<br>`SizeOfSet`. The number of its siblings, itself included. |
| `public override string ToString()` |  |
| `public static bool operator !=(TreeRow<TItem> left, TreeRow<TItem> right)` |  |
| `public static bool operator ==(TreeRow<TItem> left, TreeRow<TItem> right)` |  |
| `public override int GetHashCode()` |  |
| `public override bool Equals(object obj)` |  |
| `public bool Equals(TreeRow<TItem> other)` |  |
| `public void Deconstruct(out TItem Item, out int Level, out TItem? Parent, out bool HasChildren, out bool IsExpanded, out int PositionInSet, out int SizeOfSet)` |  |
| `public TItem Item { get; init; }` | The item. |
| `public int Level { get; init; }` | The depth, one for the roots. |
| `public TItem? Parent { get; init; }` | The parent item, or default for a root. |
| `public bool HasChildren { get; init; }` | Whether the item can be expanded. |
| `public bool IsExpanded { get; init; }` | Whether the item is expanded. |
| `public int PositionInSet { get; init; }` | The one-based position among its siblings. |
| `public int SizeOfSet { get; init; }` | The number of its siblings, itself included. |

### `VirtualItem<TItem>`

Namespace `Nazor.Controls.Primitives`. [Source](../../Sources/Core/Nazor.Controls/Primitives/Virtualizer.cs)

| Declaration | Contract |
| --- | --- |
| `public readonly struct VirtualItem<TItem> : IEquatable<VirtualItem<TItem>>` | An item rendered by a `Nazor.Controls.Primitives.Virtualizer<TItem>`.<br>`TItem`. The type of the items.<br>`Item`. The item.<br>`Index`. Its position in the collection. |
| `public VirtualItem()` |  |
| `public VirtualItem(TItem Item, int Index)` | An item rendered by a `Nazor.Controls.Primitives.Virtualizer<TItem>`.<br>`TItem`. The type of the items.<br>`Item`. The item.<br>`Index`. Its position in the collection. |
| `public override string ToString()` |  |
| `public static bool operator !=(VirtualItem<TItem> left, VirtualItem<TItem> right)` |  |
| `public static bool operator ==(VirtualItem<TItem> left, VirtualItem<TItem> right)` |  |
| `public override int GetHashCode()` |  |
| `public override bool Equals(object obj)` |  |
| `public bool Equals(VirtualItem<TItem> other)` |  |
| `public void Deconstruct(out TItem Item, out int Index)` |  |
| `public TItem Item { get; init; }` | The item. |
| `public int Index { get; init; }` | Its position in the collection. |

### `VirtualRange`

Namespace `Nazor.Controls.Primitives`. [Source](../../Sources/Core/Nazor.Controls/Primitives/Virtualizer.cs)

| Declaration | Contract |
| --- | --- |
| `public readonly struct VirtualRange : IEquatable<VirtualRange>` | The items currently realized by a `Nazor.Controls.Primitives.Virtualizer<TItem>`.<br>`First`. The index of the first realized item.<br>`Last`. The index after the last realized item.<br>`Count`. The number of items in the collection. |
| `public VirtualRange()` |  |
| `public VirtualRange(int First, int Last, int Count)` | The items currently realized by a `Nazor.Controls.Primitives.Virtualizer<TItem>`.<br>`First`. The index of the first realized item.<br>`Last`. The index after the last realized item.<br>`Count`. The number of items in the collection. |
| `public override string ToString()` |  |
| `public static bool operator !=(VirtualRange left, VirtualRange right)` |  |
| `public static bool operator ==(VirtualRange left, VirtualRange right)` |  |
| `public override int GetHashCode()` |  |
| `public override bool Equals(object obj)` |  |
| `public bool Equals(VirtualRange other)` |  |
| `public void Deconstruct(out int First, out int Last, out int Count)` |  |
| `public int First { get; init; }` | The index of the first realized item. |
| `public int Last { get; init; }` | The index after the last realized item. |
| `public int Count { get; init; }` | The number of items in the collection. |

### `Virtualizer<TItem>`

Namespace `Nazor.Controls.Primitives`. [Source](../../Sources/Core/Nazor.Controls/Primitives/Virtualizer.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed class Virtualizer<TItem> : Control` | A scrolling viewport that creates elements only for the items in view, so a list or a grid of any length stays fast. Items have the size `Nazor.Controls.Primitives.Virtualizer<TItem>.ItemSize`, or, with `Nazor.Controls.Primitives.Virtualizer<TItem>.MeasureItems`, their measured size after layout (the estimate is used until then). With `Nazor.Controls.Primitives.Virtualizer<TItem>.Columns` greater than one, items fill rows of a grid. The viewport (`Nazor.Controls.Primitives.Virtualizer<TItem>.As`, `Virtualizer` by default) needs an NSS size and `overflow`; each item is wrapped in a `{name}Item` element whose position is set by spacers. Lists, trees, grids and long pickers are built on it. Scroll notifications that keep the same realized range and repeated item measurements do not rebuild the rows.<br>`TItem`. The type of the items. |
| `protected override void OnParametersSet()` | Validates the parameters and updates the realized range. |
| `protected override void AddAttributes(RenderTreeBuilder builder)` | Adds the scroll listener and the role.<br>`builder`. The builder that receives the render frames. |
| `protected override void AddContent(RenderTreeBuilder builder)` | Adds nonvirtualized overlay content followed by the spacers and realized items.<br>`builder`. The builder that receives the render frames. |
| `public bool ScrollIntoView(int index)` | Scrolls so that an item is entirely visible, the least distance needed.<br>`index`. The index of the item.<br>**Returns**. true if the viewport scrolled. |
| `public int PageSize()` | Gets how many whole items fit in the viewport along the scrolling axis, at least one.<br>**Returns**. The page size, for Page Up and Page Down. |
| `public Virtualizer()` |  |
| `public string As { get; set; }` | `[Parameter]` Initial value `"Virtualizer"`.<br>Gets or sets the element name of the viewport. |
| `public IReadOnlyList<TItem> Items { get; set; }` | `[Parameter]` Initial value `[]`.<br>Gets or sets the items. |
| `public RenderFragment<VirtualItem<TItem>>? ItemTemplate { get; set; }` | `[Parameter]` `[EditorRequired]`<br>Gets or sets how each item is rendered. |
| `public RenderFragment? OverlayContent { get; set; }` | `[Parameter]` Nonvirtualized content inside the scrolling root before spacers and item wrappers, such as an absolute decorative indicator. Remains mounted across realized-range changes and an empty list. The caller owns positioning and semantics; it is not counted or measured as an item. |
| `public double ItemSize { get; set; }` | `[Parameter]` Initial value `32`.<br>Gets or sets the size of an item (a row of a grid) along the scrolling axis, or its estimate when measured. |
| `public bool MeasureItems { get; set; }` | `[Parameter]`<br>Gets or sets whether items are measured after layout, for items of different sizes. |
| `public Orientation Orientation { get; set; }` | `[Parameter]` Initial value `Orientation.Vertical`.<br>Gets or sets the scrolling axis. |
| `public int Columns { get; set; }` | `[Parameter]` Initial value `1`.<br>Gets or sets the number of items per row of a grid; one for a list. |
| `public int Overscan { get; set; }` | `[Parameter]` Initial value `3`.<br>Gets or sets how many rows beyond the viewport are kept realized on each side. |
| `public int InitialRows { get; set; }` | `[Parameter]` Initial value `30`.<br>Gets or sets how many rows are realized before the viewport size is known. |
| `public Func<TItem, object>? ItemKey { get; set; }` | `[Parameter]`<br>Gets or sets a function that gives each item a stable key; the index is used when null. |
| `public string? Role { get; set; }` | `[Parameter]`<br>Gets or sets the accessible role of the viewport, or null. |
| `public EventCallback<VirtualRange> OnRangeChanged { get; set; }` | `[Parameter]`<br>Gets or sets the callback invoked when the realized range changes, for example to load more data near the end. |
| `public VirtualRange Range { get; }` | Gets the realized range. |
| `public double ScrollOffset { get; }` | Gets the current scroll offset along the scrolling axis. |
| `public double ViewportSize { get; }` | Gets the size of the viewport along the scrolling axis, or zero before the first layout. |
| `protected override string ElementName { get; }` | Gets the element name. |

### `VisuallyHidden`

Namespace `Nazor.Controls.Primitives`. [Source](../../Sources/Core/Nazor.Controls/Primitives/Measurement.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed class VisuallyHidden : ContentControl` | Content read by assistive technologies but not shown, such as the label of an icon-only control drawn by a theme or extra context for a screen reader. Themes hide the element (`VisuallyHidden`) without removing it. |
| `protected override void AddAttributes(RenderTreeBuilder builder)` | Adds the text role.<br>`builder`. The builder that receives the render frames. |
| `public VisuallyHidden()` |  |
| `protected override string ElementName { get; }` | Gets the NSS element name emitted by VisuallyHidden. |

## Built-in theme

### `Themes`

Namespace `Nazor.Themes`. [Source](../../Sources/Core/Nazor.Controls/Themes.cs)

| Declaration | Contract |
| --- | --- |
| `public static class Themes` | The themes included with `Nazor.Controls`. Basic is the default when an application selects no theme; None removes every theme rule so the application's own stylesheets draw the controls from scratch. |
| `public static StyleSheet Basic { get; }` | The default theme: system colors, plain one-pixel borders and the essential geometry that tells a button from text or a drop-down, without decoration, transitions or animations. |
| `public static StyleSheet None { get; }` | An empty theme for applications that supply every control style themselves. |
| `public static StyleSheet Default { get; }` | The theme used when an application selects none; currently `Nazor.Themes.Themes.Basic`. |


<a id="type-titlebar"></a>

### `TitleBar`

Namespace `Nazor.Controls`.

A composable client title-bar surface used by theme presenters and `Nazor.Controls.UserTitleBar`. It renders at its destination without selecting decoration policy or registering a window declaration. Its empty areas drag the window (window-drag), double-click maximizes it and a right click opens the system menu; interactive controls in it keep their input, including field frames, custom pointer handlers and tooltips. It reserves the space of the system caption buttons when the window shows them (system-buttons-left-inset/right-inset) and carries the maximized, fullscreen, show-system-buttons, can-minimize, can-maximize, can-resize and height attributes. Content goes into structured slots, in this order: `Nazor.Controls.TitleBar.Leading` (back or pane buttons), `Nazor.Controls.TitleBar.AppIcon`, the title (`Nazor.Controls.TitleBar.Title` or `Nazor.Controls.TitleBar.TitleContent`) with its `Nazor.Controls.TitleBar.Subtitle`, `Nazor.Controls.TitleBar.Center` (a search box or tabs, taking the free space), the content between the tags and `Nazor.Controls.TitleBar.Trailing` (account, settings or `Nazor.Controls.CaptionButtons`); a title bar with only content between the tags renders that content alone. The parts are TitleBarLeading, TitleBarAppIcon, TitleBarHeading (with TitleBarTitle and TitleBarSubtitle), TitleBarCenter and TitleBarTrailing.

| Declaration | Contract |
| --- | --- |
| `public class TitleBar : ContentControl, IDisposable` | A composable client title-bar surface used by theme presenters and `Nazor.Controls.UserTitleBar`. It renders at its destination without selecting decoration policy or registering a window declaration. Its empty areas drag the window (window-drag), double-click maximizes it and a right click opens the system menu; interactive controls in it keep their input, including field frames, custom pointer handlers and tooltips. It reserves the space of the system caption buttons when the window shows them (system-buttons-left-inset/right-inset) and carries the maximized, fullscreen, show-system-buttons, can-minimize, can-maximize, can-resize and height attributes. Content goes into structured slots, in this order: `Nazor.Controls.TitleBar.Leading` (back or pane buttons), `Nazor.Controls.TitleBar.AppIcon`, the title (`Nazor.Controls.TitleBar.Title` or `Nazor.Controls.TitleBar.TitleContent`) with its `Nazor.Controls.TitleBar.Subtitle`, `Nazor.Controls.TitleBar.Center` (a search box or tabs, taking the free space), the content between the tags and `Nazor.Controls.TitleBar.Trailing` (account, settings or `Nazor.Controls.CaptionButtons`); a title bar with only content between the tags renders that content alone. The parts are TitleBarLeading, TitleBarAppIcon, TitleBarHeading (with TitleBarTitle and TitleBarSubtitle), TitleBarCenter and TitleBarTrailing. |
| `protected override void BuildRenderTree(RenderTreeBuilder builder)` | Renders the title-bar surface at the current destination. |
| `protected override void OnParametersSet()` | Validates the parameters and follows the window's changes. |
| `protected override void AddAttributes(RenderTreeBuilder builder)` | Adds the drag region, the role, the window state and the system button insets. |
| `protected override void AddContent(RenderTreeBuilder builder)` | Adds the slots, or the content alone when no slot is used. |
| `public virtual void Dispose()` | Stops following the window. |
| `public TitleBar()` | Creates a new instance. |
| `public IWindow Window { get; set; }` | Gets or sets the window that owns this title bar, supplied automatically. |
| `public RenderFragment? Leading { get; set; }` | Gets or sets content at the leading edge, such as a back button or a pane toggle. |
| `public RenderFragment? AppIcon { get; set; }` | Gets or sets the application icon, usually an `Nazor.Controls.Image` or `Nazor.Controls.Icon`. |
| `public string? Title { get; set; }` | Gets or sets the title text, often the window title. |
| `public RenderFragment? TitleContent { get; set; }` | Gets or sets rich title content, used instead of `Nazor.Controls.TitleBar.Title`. |
| `public string? Subtitle { get; set; }` | Gets or sets a secondary text after the title, such as the document or the environment. |
| `public RenderFragment? Center { get; set; }` | Gets or sets content in the middle that takes the free space, such as a search box or tabs. |
| `public RenderFragment? Trailing { get; set; }` | Gets or sets content at the trailing edge, such as an account button or caption buttons. |
| `public TitleBarHeight Height { get; set; }` | Gets or sets the height variant. |
| `protected virtual string Source { get; }` | The source attribute used to distinguish framework composition from application content. |
| `protected override string ElementName { get; }` | Gets the NSS element name emitted by TitleBar. |
| `protected override string? ElementStyle { get; }` | Gets the inline NSS declarations, including the fixed placement at the top of the window. |

<a id="type-dragregion"></a>

### `DragRegion`

Namespace `Nazor.Controls`.

An area that drags the window, for custom title areas outside a `Nazor.Controls.TitleBar`, such as the top of a side pane that extends into the caption: its empty space moves the window (window-drag), double-click maximizes it, and interactive controls inside it keep their input. It only works where the window draws its content in the caption area supported by its selected decoration configuration.

| Declaration | Contract |
| --- | --- |
| `public sealed class DragRegion : ContentControl` | An area that drags the window, for custom title areas outside a `Nazor.Controls.TitleBar`, such as the top of a side pane that extends into the caption: its empty space moves the window (window-drag), double-click maximizes it, and interactive controls inside it keep their input. It only works where the window draws its content in the caption area supported by its selected decoration configuration. |
| `protected override void AddAttributes(RenderTreeBuilder builder)` | Adds the drag attribute. |
| `public DragRegion()` | Creates a new instance. |
| `protected override string ElementName { get; }` | Gets the NSS element name emitted by DragRegion. |

<a id="type-usercaptionbuttons"></a>

### `UserCaptionButtons`

Namespace `Nazor.Controls`.

Declares application caption-button groups independently of the title-bar provider. Declare once in the window file. The fragments mount only for WindowCaptionButtons.User. The application supplies commands, accessible names and optional Snap areas; no automatic buttons are added.

| Declaration | Contract |
| --- | --- |
| `public sealed class UserCaptionButtons : ComponentBase, IDisposable` | Declares application caption-button groups independently of the title-bar provider. Declare once in the window file. The fragments mount only for WindowCaptionButtons.User. The application supplies commands, accessible names and optional Snap areas; no automatic buttons are added. |
| `protected override void OnParametersSet()` | Validates the right-hand fragment shorthand. |
| `protected override void BuildRenderTree(RenderTreeBuilder builder)` | Registers the fragments without instantiating their content. |
| `public void Dispose()` | Removes the declaration from its window. |
| `public UserCaptionButtons()` | Creates a new instance. |
| `public RenderFragment? Left { get; set; }` | Controls on the physical left, irrespective of text direction. |
| `public RenderFragment? Right { get; set; }` | Controls on the physical right, irrespective of text direction. |
| `public RenderFragment? ChildContent { get; set; }` | Shorthand for Right; cannot be supplied together with Right. |

<a id="type-usertitlebar"></a>

### `UserTitleBar`

Namespace `Nazor.Controls`.

Declares application title-bar content in a window. The content mounts only when the selected configuration requests User. One declaration is permitted per window. Native and Nazor selections ignore it without instantiating its children. Keep persistent state outside the caption subtree.

| Declaration | Contract |
| --- | --- |
| `public sealed class UserTitleBar : TitleBar` | Declares application title-bar content in a window. The content mounts only when the selected configuration requests User. One declaration is permitted per window. Native and Nazor selections ignore it without instantiating its children. Keep persistent state outside the caption subtree. |
| `protected override void BuildRenderTree(RenderTreeBuilder builder)` | Registers the surface with its declaring window instead of rendering it in place. |
| `public override void Dispose()` | Unregisters the declaration and unsubscribes from the owning window. |
| `public UserTitleBar()` | Creates a new instance. |
| `protected override string Source { get; }` | Marks the rendered TitleBar element as user-provided. |

See [title bars and platform integration](../Guide/14-Title-Bars-and-Platform-Integration.md) for complete configuration policies, capability checks, explicit fallback and User content lifecycle.

ListView item positions are per group, GridView positions cover its supplied sequence, and TreeView positions are per parent. Virtualizer item wrappers cover its entire supplied sequence, excluding spacers. DataGrid item rows publish global filtered/sorted positions for local and provider data, excluding group headers/details. Provider positions and totals travel with the accepted window and retain server-response order through local grouping. See [logical item metadata](../Guide/17-Selectors-and-Media-Queries.md#logical-collection-positions-and-component-roots).

### `NumberPresenter`

Namespace `Nazor.Controls.Primitives`. [Source](../../Sources/Core/Nazor.Controls/Primitives/NumberPresenter.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed class NumberPresenter : Control, IDisposable` | Presents a finite bounded scalar through a typed child context and the host scene clock. NSS owns the number-motion policy. Initial motion starts at Minimum; retargeting samples the displayed value. None/reduced motion is immediate. Physical attributes: number-target, number-minimum, number-maximum, number-at-start, number-at-end; custom property --nz-presented-number. The containing control owns its actual accessible value. |
| `public NumberPresenter()` | Creates a presenter. |
| `public string As { get; set; }` | `[Parameter]` Initial value `"NumberPresenter"`. Physical element name. |
| `public double Value { get; set; }` | `[Parameter]` Finite target; presentation clamps it to the bounds. |
| `public double Minimum { get; set; }` | `[Parameter]` Finite lower bound and initial displayed value; zero by default. |
| `public double Maximum { get; set; }` | `[Parameter]` Finite upper bound; one by default. The nonnegative range must also be finite. |
| `public RenderFragment<NumberPresentationState>? ChildContent { get; set; }` | `[Parameter]` Content for each displayed sample; keep stateful child keys stable. |
| `public NumberPresentationState Presentation { get; }` | Current displayed sample; starts at Minimum before layout. |
| `protected override string ElementName { get; }` | Gets the physical element name. |
| `protected override string? ElementStyle { get; }` | Gets the presented-number variable and caller declarations. |
| `protected override void OnParametersSet()` | Validates finite bounds/target and updates the pending target. |
| `protected override void AddAttributes(RenderTreeBuilder builder)` | Adds scene marker, actual target/bounds and displayed boundary states. |
| `protected override void AddContent(RenderTreeBuilder builder)` | Renders child content with the displayed sample. |
| `public void Dispose()` | Releases its callback; the scene drops its owner on removal or the next update. |

### `NumberPresentationState`

Namespace `Nazor.Controls.Primitives`. [Source](../../Sources/Core/Nazor.Controls/Primitives/NumberPresenter.cs)

Authored as a C# `readonly record struct`; the table includes its compiled value-type contract and synthesized equality/deconstruction members.

| Declaration | Contract |
| --- | --- |
| `public readonly struct NumberPresentationState : IEquatable<NumberPresentationState>` | Displayed scalar and theme-defined boundary states, independent of the application's target. |
| `public NumberPresentationState()` | Creates a zero displayed value with both boundary flags false. |
| `public NumberPresentationState(double Value, bool AtStart, bool AtEnd)` | Creates a displayed sample and its independently supplied boundary flags. |
| `public double Value { get; init; }` | Current displayed value. |
| `public bool AtStart { get; init; }` | Within the theme's displayed start tolerance. |
| `public bool AtEnd { get; init; }` | Within the theme's displayed end tolerance. |
| `public override string ToString()` | Returns the record diagnostic representation. |
| `public override int GetHashCode()` | Hashes the displayed value and both boundary flags with their default equality semantics. |
| `public override bool Equals(object obj)` | Tests equality with the same record type; null or another type is unequal. |
| `public bool Equals(NumberPresentationState other)` | Compares the displayed value and both boundary flags using their default equality semantics. |
| `public static bool operator ==(NumberPresentationState left, NumberPresentationState right)` | Tests value equality. |
| `public static bool operator !=(NumberPresentationState left, NumberPresentationState right)` | Tests value inequality. |
| `public void Deconstruct(out double Value, out bool AtStart, out bool AtEnd)` | Extracts the displayed value and both boundary flags. |

Tooltip surfaces also publish `reentered` after reopening during retained exit, until that surface completely closes. Themes can suppress fresh entry keyframes and retarget opacity from its current value. `TooltipContainer` remains noninteractive; implicit target-keyed tooltips share this lifetime behavior.

### `BreadcrumbBar<TItem>`

Namespace `Nazor.Controls`. [Source](../../Sources/Core/Nazor.Controls/Navigation/BreadcrumbBar.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed class BreadcrumbBar<TItem> : Control` | Application-owned ancestor-to-current path with measured overflow, typed invocation and one roving header Tab stop. The final node is current. Parts: BreadcrumbSlot, BreadcrumbItemContent, BreadcrumbSeparator, Button and MenuButton. |
| `public BreadcrumbBar()` | Creates an empty path. |
| `public IReadOnlyList<TItem> Items { get; set; }` | `[Parameter]` Complete path; empty by default. |
| `public Func<TItem, object>? ItemKey { get; set; }` | `[Parameter]` Stable unique non-null keys; default index. Invalid explicit keys throw before render. |
| `public Func<TItem, string>? ItemText { get; set; }` | `[Parameter]` Display/typeahead/accessible text; default ToString. |
| `public RenderFragment<TItem>? ItemTemplate { get; set; }` | `[Parameter]` Decorative labels in header/menu; nested interactive controls unsupported. |
| `public EventCallback<BreadcrumbItemInvokedEventArgs<TItem>> OnItemInvoked { get; set; }` | `[Parameter]` Exact item/index request; application owns path/routing. |
| `public bool Disabled { get; set; }` | `[Parameter]` Rejects all activation; false by default. |
| `public int HiddenItemCount { get; }` | Number of leading nodes currently represented by overflow. |
| `public Task InvokeAsync(int index)` | Requests navigation without mutating Items; disabled/removed controls do nothing. Invalid index throws ArgumentOutOfRangeException. |
| `protected override string ElementName { get; }` | BreadcrumbBar. |
| `protected override void OnParametersSet()` | Validates keys and preserves focus identity. |
| `protected override void AddAttributes(RenderTreeBuilder builder)` | Named group, measured available width and scoped keyboard behavior. |
| `protected override void AddContent(RenderTreeBuilder builder)` | Measured labels and ordered ancestor overflow. |
| `protected override Task OnAfterRenderAsync(bool firstRender)` | Reconciles overflow and repairs focus after width/path changes. |

### `BreadcrumbItemInvokedEventArgs<TItem>`

Namespace `Nazor.Controls`. [Source](../../Sources/Core/Nazor.Controls/Navigation/BreadcrumbBar.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed class BreadcrumbItemInvokedEventArgs<TItem> : EventArgs` | Requested item and its position at invocation. |
| `public BreadcrumbItemInvokedEventArgs(TItem item, int index)` | Stores the application-owned item and zero-based position. |
| `public TItem Item { get; }` | Invoked application node. |
| `public int Index { get; }` | Zero-based position at invocation. |

## `ColorPicker`

Namespace `Nazor.Controls`. [Source](../../Sources/Core/Nazor.Controls/Pickers/ColorPicker.cs)

Ready RGBA editor composed from ColorArea, ColorSlider, TextField and Select. It does not open a system picker. `ColorChanged` reports changed bytes, including pointer previews; `OnChange` reports a completed slider/key/text action and may carry unchanged bytes when only remembered HSV changed. Parameter synchronization reports neither. Hue survives gray; hue/saturation survive black. Invalid numeric/hex drafts keep the color, remain marked for correction on Enter and restore canonical text on blur. Number input follows `Culture`; nonfinite values are invalid and finite values clamp to channel bounds. Hex uses CSS #rgb/#rgba/#rrggbb/#rrggbbaa with optional leading #, never theme-dependent byte order. Disabling alpha editing preserves the current alpha byte, including during hex edits. No circular spectrum, per-channel constrained palette, named-color dictionary or More disclosure API is included.

Parts: `ColorPickerVisuals`, `ColorPickerSpectrum`/`ColorPickerSpectrumThumb`, `ColorPickerSlider` and `ColorPickerAlphaSlider` with Track/Thumb parts, `ColorPickerPreview` with `ColorPickerSwatch` and Previous/Current layers, `ColorPickerInputs`, `ColorPickerRepresentation`, `ColorPickerChannels`, `ColorPickerChannel[channel]`, `ColorPickerHex`. Inner fields and select retain their regular parts. Root markers include orientation, representation, components, alpha-enabled and spectrum/preview/channels visibility. The group owns no Tab stop; its visible enabled controls do. Field names/descriptions apply to the group; internal field IDs and labels are independent.

| Declaration | Contract |
| --- | --- |
| `public sealed class ColorPicker : Control` | Ready rectangular RGBA color editor. |
| `public ColorPicker()` | Opaque white, RGB text, HueSaturation plane and vertical presentation; alpha editing disabled. |
| `public Color Color { get; set; }` | `[Parameter]` RGBA bytes; opaque white by default. Symbolic currentColor is rejected. |
| `public EventCallback<Color> ColorChanged { get; set; }` | `[Parameter]` Changed user color bytes, including previews; supports @bind-Color. |
| `public EventCallback<Color> OnChange { get; set; }` | `[Parameter]` Completed drag/key/valid text edit; HSV-only changes may report unchanged bytes. |
| `public ColorRepresentation Representation { get; set; }` | `[Parameter]` RGB or HSV numeric text; RGB by default. |
| `public EventCallback<ColorRepresentation> RepresentationChanged { get; set; }` | `[Parameter]` User format choice; supports @bind-Representation. |
| `public ColorSpectrumComponents SpectrumComponents { get; set; }` | `[Parameter]` The six rectangular HSV axis permutations; HueSaturation by default. |
| `public Orientation Orientation { get; set; }` | `[Parameter]` Vertical above/below presentation, or Horizontal beside text with vertical sliders; Vertical by default. |
| `public bool IsAlphaEnabled { get; set; }` | `[Parameter]` False by default; controls whether opacity can be edited, preserving existing alpha when disabled. |
| `public bool IsColorSpectrumVisible { get; set; }` | `[Parameter]` True by default; controls the spectrum. |
| `public bool IsColorSliderVisible { get; set; }` | `[Parameter]` True by default; controls the remaining-channel slider. |
| `public bool IsAlphaSliderVisible { get; set; }` | `[Parameter]` True by default; effective only when IsAlphaEnabled is true. |
| `public bool IsColorPreviewVisible { get; set; }` | `[Parameter]` True by default; controls current and optional previous comparison swatches. |
| `public bool IsColorChannelTextInputVisible { get; set; }` | `[Parameter]` True by default; controls RGB/HSV text and the representation selector. |
| `public bool IsAlphaTextInputVisible { get; set; }` | `[Parameter]` True by default; effective only when IsAlphaEnabled is true. |
| `public bool IsHexInputVisible { get; set; }` | `[Parameter]` True by default; controls CSS hexadecimal input. |
| `public Color? PreviousColor { get; set; }` | `[Parameter]` Optional read-only comparison swatch; symbolic currentColor is rejected. |
| `public CultureInfo? Culture { get; set; }` | `[Parameter]` Numeric channel culture; current culture when null. Hex is culture independent. |
| `public bool ReadOnly { get; set; }` | `[Parameter]` Blocks color editing but allows selecting/copying text and changing representation. |
| `public bool Disabled { get; set; }` | `[Parameter]` Blocks all interaction; combines with the containing Field disabled state. |
| `public FieldContext? Field { get; set; }` | `[CascadingParameter]` Enclosing field state and group labeling. |
| `public Color CurrentColor { get; }` | Current RGBA bytes, including unbound local edits. |
| `public HsvColor CurrentHsvColor { get; }` | Current model with remembered hue/saturation. |
| `public ColorRepresentation CurrentRepresentation { get; }` | Current numeric representation. |
| `protected override string ElementName { get; }` | ColorPicker. |
| `protected override int? ElementTabIndex { get; }` | No composite-root Tab stop. |
| `protected override string? ElementStyle { get; }` | Current/previous preview variables followed by caller style. |
| `protected override void OnParametersSet()` | Synchronizes state/formatting, validates enums and rejects symbolic colors. |
| `protected override void AddAttributes(RenderTreeBuilder builder)` | Group semantics and presentation/state markers. |
| `protected override void AddContent(RenderTreeBuilder builder)` | Composes primitives/fields with independent internal field labels. |

## `ColorRepresentation`

Namespace `Nazor.Controls`. [Source](../../Sources/Core/Nazor.Controls/Pickers/ColorPicker.cs)

| Declaration | Contract |
| --- | --- |
| `public enum ColorRepresentation` | Numeric text channel model. |
| `Rgb = 0` | Red, green, blue in 0–255. |
| `Hsv = 1` | Hue in degrees, saturation and value in percent. |

OverlayContainer and TooltipContainer preserve the original caller stylesheet scope through their transparent Presence wrapper. Caller-scoped rules for the surface keep applying during open, retained exit and reopen; child-part boundary rules otherwise remain unchanged.
