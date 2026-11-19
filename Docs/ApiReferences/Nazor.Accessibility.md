# Nazor.Accessibility

Public and protected API of the current development candidate.

Inherited members follow their declaring type. Positional record parameters also define properties with the same names. Signatures and descriptions below follow the current source; application layout and validation remain caller-owned unless a control explicitly supplies them.

## Types

- [`AnnouncementPriority`](#type-announcementpriority)
- [`LiveSetting`](#type-livesetting)
- [`RangeInfo`](#type-rangeinfo)
- [`SemanticNode`](#type-semanticnode)
- [`SemanticProperty`](#type-semanticproperty)
- [`SemanticRole`](#type-semanticrole)
- [`SemanticTree`](#type-semantictree)
- [`TextUnit`](#type-textunit)
- [`TextUnits`](#type-textunits)

<a id="type-announcementpriority"></a>

## `AnnouncementPriority`

Namespace: `Nazor.Accessibility`

```csharp
public enum AnnouncementPriority
```

How an announcement is spoken: after the current speech, or at once.

[Source](../../Sources/Core/Nazor.Accessibility/SemanticTree.cs#L235)


### `Polite`

```csharp
Polite
```

Queues the announcement after current speech.

[Source](../../Sources/Core/Nazor.Accessibility/SemanticTree.cs#L238)


### `Assertive`

```csharp
Assertive
```

Requests an immediate announcement that may interrupt current speech.

[Source](../../Sources/Core/Nazor.Accessibility/SemanticTree.cs#L241)


<a id="type-livesetting"></a>

## `LiveSetting`

Namespace: `Nazor.Accessibility`

```csharp
public enum LiveSetting
```

How a region whose content changes is announced (the `live` attribute, as `aria-live`): polite waits for the current speech, assertive interrupts it.

[Source](../../Sources/Core/Nazor.Accessibility/SemanticTree.cs#L222)


### `Off`

```csharp
Off
```

Disables automatic announcements for changes in the live region.

[Source](../../Sources/Core/Nazor.Accessibility/SemanticTree.cs#L225)


### `Polite`

```csharp
Polite
```

Announces changes when assistive technology finishes its current speech.

[Source](../../Sources/Core/Nazor.Accessibility/SemanticTree.cs#L228)


### `Assertive`

```csharp
Assertive
```

Requests an immediate announcement that may interrupt current speech.

[Source](../../Sources/Core/Nazor.Accessibility/SemanticTree.cs#L231)


<a id="type-rangeinfo"></a>

## `RangeInfo`

Namespace: `Nazor.Accessibility`

```csharp
public sealed record RangeInfo(double Minimum, double Maximum, double Value, double SmallChange, double LargeChange, bool IsReadOnly)
```

The range of a slider or a progress bar, as UI Automation reports it.

- `Minimum`: The minimum (double).

- `Maximum`: The maximum (double).

- `Value`: The numeric value or payload represented by this record.

- `SmallChange`: The small change (double).

- `LargeChange`: The large change (double).

- `IsReadOnly`: The is read only (bool).

[Source](../../Sources/Core/Nazor.Accessibility/SemanticTree.cs#L251)


<a id="type-semanticnode"></a>

## `SemanticNode`

Namespace: `Nazor.Accessibility`

```csharp
public sealed class SemanticNode
```

A node of the semantic tree: role, name, states and geometry of a control. The same element keeps the same node (and `Id`) for its whole life; a removed element detaches it.

[Source](../../Sources/Core/Nazor.Accessibility/SemanticTree.cs#L257)


### `Id`

```csharp
public int Id { get; }
```

Unique in the tree and never reused.

[Source](../../Sources/Core/Nazor.Accessibility/SemanticTree.cs#L269)


### `Element`

```csharp
public ElementNode? Element { get; internal set; }
```

The element the node describes; for the window root, the `Window` element if there is one.

[Source](../../Sources/Core/Nazor.Accessibility/SemanticTree.cs#L274)


### `Role`

```csharp
public SemanticRole Role { get; internal set; }
```

Gets the role exposed to assistive technologies.

[Source](../../Sources/Core/Nazor.Accessibility/SemanticTree.cs#L277)


### `HeadingLevel`

```csharp
public int? HeadingLevel { get; internal set; }
```

Gets the document heading level, or null for other roles.

[Source](../../Sources/Core/Nazor.Accessibility/SemanticTree.cs#L280)


### `IsRequired`

```csharp
public bool IsRequired { get; internal set; }
```

Gets whether this field requires a value.

[Source](../../Sources/Core/Nazor.Accessibility/SemanticTree.cs#L283)


### `IsInvalid`

```csharp
public bool IsInvalid { get; internal set; }
```

Gets whether this field exposes an invalid value.

[Source](../../Sources/Core/Nazor.Accessibility/SemanticTree.cs#L286)


### `RowIndex`

```csharp
public int? RowIndex { get; internal set; }
```

Gets the zero-based table row index.

[Source](../../Sources/Core/Nazor.Accessibility/SemanticTree.cs#L289)


### `ColumnIndex`

```csharp
public int? ColumnIndex { get; internal set; }
```

Gets the zero-based table column index.

[Source](../../Sources/Core/Nazor.Accessibility/SemanticTree.cs#L292)


### `RowSpan`

```csharp
public int RowSpan { get; internal set; }
```

Gets the number of table rows occupied by this cell.

[Source](../../Sources/Core/Nazor.Accessibility/SemanticTree.cs#L295)


### `ColumnSpan`

```csharp
public int ColumnSpan { get; internal set; }
```

Gets the number of table columns occupied by this cell.

[Source](../../Sources/Core/Nazor.Accessibility/SemanticTree.cs#L298)


### `RowCount`

```csharp
public int? RowCount { get; internal set; }
```

Gets the table row count.

[Source](../../Sources/Core/Nazor.Accessibility/SemanticTree.cs#L301)


### `ColumnCount`

```csharp
public int? ColumnCount { get; internal set; }
```

Gets the table column count.

[Source](../../Sources/Core/Nazor.Accessibility/SemanticTree.cs#L304)


### `Parent`

```csharp
public SemanticNode? Parent { get; internal set; }
```

Gets the parent node, or null for a root node.

[Source](../../Sources/Core/Nazor.Accessibility/SemanticTree.cs#L307)


### `Children`

```csharp
public IReadOnlyList<SemanticNode> Children { get; }
```

Gets the ordered child nodes.

[Source](../../Sources/Core/Nazor.Accessibility/SemanticTree.cs#L310)


### `Name`

```csharp
public string Name { get; internal set; }
```

The accessible name: `AccessibleName` when set, otherwise the text of the control that labels it (`LabeledBy`), the text content for buttons, check boxes and text, the title for the window; a control still without a name (a button with only an icon) takes its tooltip. A text box is never named after its value.

[Source](../../Sources/Core/Nazor.Accessibility/SemanticTree.cs#L318)


### `Description`

```csharp
public string? Description { get; internal set; }
```

A longer description than the name (`AccessibleDescription`); null when there is none.

[Source](../../Sources/Core/Nazor.Accessibility/SemanticTree.cs#L321)


### `HelpText`

```csharp
public string? HelpText { get; internal set; }
```

Help about the control: its tooltip when it is not the name, otherwise its description.

[Source](../../Sources/Core/Nazor.Accessibility/SemanticTree.cs#L324)


### `LabeledBy`

```csharp
public SemanticNode? LabeledBy { get; internal set; }
```

The node whose text names this one (`LabeledBy`, the `Id` of a text); null when there is none.

[Source](../../Sources/Core/Nazor.Accessibility/SemanticTree.cs#L329)


### `RoleDescription`

```csharp
public string? RoleDescription { get; internal set; }
```

What the control is, in words, when its role does not say it (a switch is a button for UI Automation).

[Source](../../Sources/Core/Nazor.Accessibility/SemanticTree.cs#L334)


### `PositionInSet`

```csharp
public int? PositionInSet { get; internal set; }
```

The position from 1 of an item in its set (the options of a list, the tree items of a level under the same parent, the tabs, the radio buttons of a group, the items of a menu); null for the other roles.

[Source](../../Sources/Core/Nazor.Accessibility/SemanticTree.cs#L340)


### `SizeOfSet`

```csharp
public int? SizeOfSet { get; internal set; }
```

The number of items of the set of `PositionInSet`.

[Source](../../Sources/Core/Nazor.Accessibility/SemanticTree.cs#L343)


### `TextSelection`

```csharp
public TextSelection? TextSelection { get; internal set; }
```

The selection and caret of a text box (not of a password box); null for the other roles.

[Source](../../Sources/Core/Nazor.Accessibility/SemanticTree.cs#L346)


### `CanSelectMultiple`

```csharp
public bool CanSelectMultiple { get; internal set; }
```

A list where several items can be selected (the `multiselectable` attribute).

[Source](../../Sources/Core/Nazor.Accessibility/SemanticTree.cs#L349)


### `LiveSetting`

```csharp
public LiveSetting LiveSetting { get; internal set; }
```

A live region (the `live` attribute, an info bar): its changes are announced. Without an accessible name it is named by its text, which is what is read.

[Source](../../Sources/Core/Nazor.Accessibility/SemanticTree.cs#L355)


### `AutomationId`

```csharp
public string? AutomationId { get; internal set; }
```

The control `Id`, stable across runs: the key used by UI tests.

[Source](../../Sources/Core/Nazor.Accessibility/SemanticTree.cs#L358)


### `IsEnabled`

```csharp
public bool IsEnabled { get; internal set; }
```

Gets whether the element is enabled for interaction.

[Source](../../Sources/Core/Nazor.Accessibility/SemanticTree.cs#L361)


### `IsFocusable`

```csharp
public bool IsFocusable { get; internal set; }
```

Gets whether the element can receive keyboard focus.

[Source](../../Sources/Core/Nazor.Accessibility/SemanticTree.cs#L364)


### `HasFocus`

```csharp
public bool HasFocus { get; internal set; }
```

Gets whether this element currently has keyboard focus.

[Source](../../Sources/Core/Nazor.Accessibility/SemanticTree.cs#L367)


### `IsChecked`

```csharp
public bool? IsChecked { get; internal set; }
```

The state of a check box; null for other roles.

[Source](../../Sources/Core/Nazor.Accessibility/SemanticTree.cs#L370)


### `Value`

```csharp
public string? Value { get; internal set; }
```

The value of a text box; null for other roles.

[Source](../../Sources/Core/Nazor.Accessibility/SemanticTree.cs#L373)


### `Range`

```csharp
public RangeInfo? Range { get; internal set; }
```

Minimum, maximum and value of a slider or a progress bar; null for the other roles.

[Source](../../Sources/Core/Nazor.Accessibility/SemanticTree.cs#L376)


### `IsExpanded`

```csharp
public bool? IsExpanded { get; internal set; }
```

Whether a combo box shows its list; null for the other roles.

[Source](../../Sources/Core/Nazor.Accessibility/SemanticTree.cs#L379)


### `IsPassword`

```csharp
public bool IsPassword { get; internal set; }
```

A text box that holds a password: its value is not exposed.

[Source](../../Sources/Core/Nazor.Accessibility/SemanticTree.cs#L382)


### `Level`

```csharp
public int? Level { get; internal set; }
```

The depth of a tree item, from 1; null for the other roles.

[Source](../../Sources/Core/Nazor.Accessibility/SemanticTree.cs#L385)


### `Bounds`

```csharp
public Rect Bounds { get; internal set; }
```

Bounds in logical client coordinates, scroll offsets included.

[Source](../../Sources/Core/Nazor.Accessibility/SemanticTree.cs#L388)


### `IsOffscreen`

```csharp
public bool IsOffscreen { get; internal set; }
```

True when the node is entirely outside the window or clipped by a scrolling ancestor.

[Source](../../Sources/Core/Nazor.Accessibility/SemanticTree.cs#L391)


### `Scroll`

```csharp
public ScrollState? Scroll { get; internal set; }
```

Scroll position and extent of this node's viewport when computed overflow enables scrolling; null otherwise. The window node uses its implicit content viewport.

[Source](../../Sources/Core/Nazor.Accessibility/SemanticTree.cs#L394)


### `IsDetached`

```csharp
public bool IsDetached { get; internal set; }
```

The element was removed: the node no longer belongs to the tree.

[Source](../../Sources/Core/Nazor.Accessibility/SemanticTree.cs#L399)


### `ToString`

```csharp
public override string ToString()
```

Returns a textual representation of this value.

Returns: The textual representation of this value.

[Source](../../Sources/Core/Nazor.Accessibility/SemanticTree.cs#L403)


<a id="type-semanticproperty"></a>

## `SemanticProperty`

Namespace: `Nazor.Accessibility`

```csharp
public enum SemanticProperty
```

Properties whose changes are reported by `SemanticTree.PropertyChanged`.

[Source](../../Sources/Core/Nazor.Accessibility/SemanticTree.cs#L164)


### `Name`

```csharp
Name
```

Identifies changes to the accessible name.

[Source](../../Sources/Core/Nazor.Accessibility/SemanticTree.cs#L167)


### `Description`

```csharp
Description
```

Identifies changes to descriptive help text.

[Source](../../Sources/Core/Nazor.Accessibility/SemanticTree.cs#L170)


### `IsRequired`

```csharp
IsRequired
```

Identifies changes to required-value semantics.

[Source](../../Sources/Core/Nazor.Accessibility/SemanticTree.cs#L173)


### `IsInvalid`

```csharp
IsInvalid
```

Identifies changes to invalid-value semantics.

[Source](../../Sources/Core/Nazor.Accessibility/SemanticTree.cs#L176)


### `IsEnabled`

```csharp
IsEnabled
```

Identifies changes to the enabled state.

[Source](../../Sources/Core/Nazor.Accessibility/SemanticTree.cs#L179)


### `IsFocusable`

```csharp
IsFocusable
```

Identifies changes to whether keyboard focus is supported.

[Source](../../Sources/Core/Nazor.Accessibility/SemanticTree.cs#L182)


### `IsChecked`

```csharp
IsChecked
```

Identifies changes to the checked state.

[Source](../../Sources/Core/Nazor.Accessibility/SemanticTree.cs#L185)


### `Value`

```csharp
Value
```

Identifies changes to the exposed text or control value.

[Source](../../Sources/Core/Nazor.Accessibility/SemanticTree.cs#L188)


### `IsOffscreen`

```csharp
IsOffscreen
```

Identifies changes to whether the node is outside the visible viewport.

[Source](../../Sources/Core/Nazor.Accessibility/SemanticTree.cs#L191)


### `RangeValue`

```csharp
RangeValue
```

Identifies changes to the numeric range value.

[Source](../../Sources/Core/Nazor.Accessibility/SemanticTree.cs#L194)


### `IsExpanded`

```csharp
IsExpanded
```

Identifies changes to the expanded or collapsed state.

[Source](../../Sources/Core/Nazor.Accessibility/SemanticTree.cs#L197)


### `TextSelection`

```csharp
TextSelection
```

Identifies changes to the accessible text selection.

[Source](../../Sources/Core/Nazor.Accessibility/SemanticTree.cs#L200)


### `ScrollAvailable`

```csharp
ScrollAvailable
```

Identifies changes to the availability of an NSS scroll container.

[Source](../../Sources/Core/Nazor.Accessibility/SemanticTree.cs#L203)


### `HorizontalScrollPercent`

```csharp
HorizontalScrollPercent
```

Identifies changes to horizontal scroll progress in percent, or -1 when that axis cannot scroll.

[Source](../../Sources/Core/Nazor.Accessibility/SemanticTree.cs#L205)


### `VerticalScrollPercent`

```csharp
VerticalScrollPercent
```

Identifies changes to vertical scroll progress in percent, or -1 when that axis cannot scroll.

[Source](../../Sources/Core/Nazor.Accessibility/SemanticTree.cs#L207)


### `HorizontalViewSize`

```csharp
HorizontalViewSize
```

Identifies changes to the horizontal viewport as a percentage of content width.

[Source](../../Sources/Core/Nazor.Accessibility/SemanticTree.cs#L209)


### `VerticalViewSize`

```csharp
VerticalViewSize
```

Identifies changes to the vertical viewport as a percentage of content height.

[Source](../../Sources/Core/Nazor.Accessibility/SemanticTree.cs#L211)


### `HorizontallyScrollable`

```csharp
HorizontallyScrollable
```

Identifies changes to whether content overflows the horizontal scroll viewport.

[Source](../../Sources/Core/Nazor.Accessibility/SemanticTree.cs#L213)


### `VerticallyScrollable`

```csharp
VerticallyScrollable
```

Identifies changes to whether content overflows the vertical scroll viewport.

[Source](../../Sources/Core/Nazor.Accessibility/SemanticTree.cs#L215)


<a id="type-semanticrole"></a>

## `SemanticRole`

Namespace: `Nazor.Accessibility`

```csharp
public enum SemanticRole
```

What a semantic node is for assistive technologies; controls declare it with the `role` attribute.

[Source](../../Sources/Core/Nazor.Accessibility/SemanticTree.cs#L13)


### `Window`

```csharp
Window
```

Exposes the window role to assistive technologies.

[Source](../../Sources/Core/Nazor.Accessibility/SemanticTree.cs#L16)


### `Group`

```csharp
Group
```

Exposes the group role to assistive technologies.

[Source](../../Sources/Core/Nazor.Accessibility/SemanticTree.cs#L19)


### `Text`

```csharp
Text
```

Exposes the text role to assistive technologies.

[Source](../../Sources/Core/Nazor.Accessibility/SemanticTree.cs#L22)


### `Button`

```csharp
Button
```

Exposes the button role to assistive technologies.

[Source](../../Sources/Core/Nazor.Accessibility/SemanticTree.cs#L25)


### `CheckBox`

```csharp
CheckBox
```

Exposes the check box role to assistive technologies.

[Source](../../Sources/Core/Nazor.Accessibility/SemanticTree.cs#L28)


### `TextBox`

```csharp
TextBox
```

Exposes the text box role to assistive technologies.

[Source](../../Sources/Core/Nazor.Accessibility/SemanticTree.cs#L31)


### `Image`

```csharp
Image
```

Exposes the image role to assistive technologies.

[Source](../../Sources/Core/Nazor.Accessibility/SemanticTree.cs#L34)


### `Menu`

```csharp
Menu
```

Exposes the menu role to assistive technologies.

[Source](../../Sources/Core/Nazor.Accessibility/SemanticTree.cs#L37)


### `MenuItem`

```csharp
MenuItem
```

Exposes the menu item role to assistive technologies.

[Source](../../Sources/Core/Nazor.Accessibility/SemanticTree.cs#L40)


### `ToolTip`

```csharp
ToolTip
```

Exposes the tool tip role to assistive technologies.

[Source](../../Sources/Core/Nazor.Accessibility/SemanticTree.cs#L43)


### `RadioButton`

```csharp
RadioButton
```

Exposes the radio button role to assistive technologies.

[Source](../../Sources/Core/Nazor.Accessibility/SemanticTree.cs#L46)


### `Switch`

```csharp
Switch
```

Exposes the switch role to assistive technologies.

[Source](../../Sources/Core/Nazor.Accessibility/SemanticTree.cs#L49)


### `Slider`

```csharp
Slider
```

Exposes the slider role to assistive technologies.

[Source](../../Sources/Core/Nazor.Accessibility/SemanticTree.cs#L52)


### `ProgressBar`

```csharp
ProgressBar
```

Exposes the progress bar role to assistive technologies.

[Source](../../Sources/Core/Nazor.Accessibility/SemanticTree.cs#L55)


### `ComboBox`

```csharp
ComboBox
```

Exposes the combo box role to assistive technologies.

[Source](../../Sources/Core/Nazor.Accessibility/SemanticTree.cs#L58)


### `List`

```csharp
List
```

Exposes the list role to assistive technologies.

[Source](../../Sources/Core/Nazor.Accessibility/SemanticTree.cs#L61)


### `ListItem`

```csharp
ListItem
```

Exposes the list item role to assistive technologies.

[Source](../../Sources/Core/Nazor.Accessibility/SemanticTree.cs#L64)


### `Tree`

```csharp
Tree
```

Exposes the tree role to assistive technologies.

[Source](../../Sources/Core/Nazor.Accessibility/SemanticTree.cs#L67)


### `TreeItem`

```csharp
TreeItem
```

Exposes the tree item role to assistive technologies.

[Source](../../Sources/Core/Nazor.Accessibility/SemanticTree.cs#L70)


### `TabList`

```csharp
TabList
```

Exposes the tab list role to assistive technologies.

[Source](../../Sources/Core/Nazor.Accessibility/SemanticTree.cs#L73)


### `TabItem`

```csharp
TabItem
```

Exposes the tab item role to assistive technologies.

[Source](../../Sources/Core/Nazor.Accessibility/SemanticTree.cs#L76)


### `Splitter`

```csharp
Splitter
```

Exposes the splitter role to assistive technologies.

[Source](../../Sources/Core/Nazor.Accessibility/SemanticTree.cs#L79)


### `Hyperlink`

```csharp
Hyperlink
```

Exposes the hyperlink role to assistive technologies.

[Source](../../Sources/Core/Nazor.Accessibility/SemanticTree.cs#L82)


### `Status`

```csharp
Status
```

Exposes the status role to assistive technologies.

[Source](../../Sources/Core/Nazor.Accessibility/SemanticTree.cs#L85)


### `SplitButton`

```csharp
SplitButton
```

Exposes the split button role to assistive technologies.

[Source](../../Sources/Core/Nazor.Accessibility/SemanticTree.cs#L88)


### `MenuBar`

```csharp
MenuBar
```

Exposes the menu bar role to assistive technologies.

[Source](../../Sources/Core/Nazor.Accessibility/SemanticTree.cs#L91)


### `ToolBar`

```csharp
ToolBar
```

Exposes the tool bar role to assistive technologies.

[Source](../../Sources/Core/Nazor.Accessibility/SemanticTree.cs#L94)


### `Calendar`

```csharp
Calendar
```

Exposes the calendar role to assistive technologies.

[Source](../../Sources/Core/Nazor.Accessibility/SemanticTree.cs#L97)


### `TitleBar`

```csharp
TitleBar
```

Exposes the title bar role to assistive technologies.

[Source](../../Sources/Core/Nazor.Accessibility/SemanticTree.cs#L100)


### `DocumentList`

```csharp
DocumentList
```

A document list without selection behavior.

[Source](../../Sources/Core/Nazor.Accessibility/SemanticTree.cs#L103)


### `DocumentListItem`

```csharp
DocumentListItem
```

An item in a document list.

[Source](../../Sources/Core/Nazor.Accessibility/SemanticTree.cs#L106)


### `DefinitionList`

```csharp
DefinitionList
```

A collection of terms and descriptions.

[Source](../../Sources/Core/Nazor.Accessibility/SemanticTree.cs#L109)


### `DefinitionTerm`

```csharp
DefinitionTerm
```

A term in a definition list.

[Source](../../Sources/Core/Nazor.Accessibility/SemanticTree.cs#L112)


### `DefinitionDescription`

```csharp
DefinitionDescription
```

A description associated with a term.

[Source](../../Sources/Core/Nazor.Accessibility/SemanticTree.cs#L115)


### `Heading`

```csharp
Heading
```

A document heading with a level.

[Source](../../Sources/Core/Nazor.Accessibility/SemanticTree.cs#L118)


### `Form`

```csharp
Form
```

A named form and its submission scope.

[Source](../../Sources/Core/Nazor.Accessibility/SemanticTree.cs#L121)


### `Dialog`

```csharp
Dialog
```

A window-local dialog.

[Source](../../Sources/Core/Nazor.Accessibility/SemanticTree.cs#L124)


### `AlertDialog`

```csharp
AlertDialog
```

A dialog requiring a decision or acknowledgement.

[Source](../../Sources/Core/Nazor.Accessibility/SemanticTree.cs#L127)


### `ToggleButton`

```csharp
ToggleButton
```

A button with persistent pressed state.

[Source](../../Sources/Core/Nazor.Accessibility/SemanticTree.cs#L130)


### `Meter`

```csharp
Meter
```

A read-only measurement within a range.

[Source](../../Sources/Core/Nazor.Accessibility/SemanticTree.cs#L133)


### `Table`

```csharp
Table
```

A static semantic table.

[Source](../../Sources/Core/Nazor.Accessibility/SemanticTree.cs#L136)


### `RowGroup`

```csharp
RowGroup
```

A table row group.

[Source](../../Sources/Core/Nazor.Accessibility/SemanticTree.cs#L139)


### `Row`

```csharp
Row
```

A table row.

[Source](../../Sources/Core/Nazor.Accessibility/SemanticTree.cs#L142)


### `Cell`

```csharp
Cell
```

A table data cell.

[Source](../../Sources/Core/Nazor.Accessibility/SemanticTree.cs#L145)


### `ColumnHeader`

```csharp
ColumnHeader
```

A table column header.

[Source](../../Sources/Core/Nazor.Accessibility/SemanticTree.cs#L148)


### `RowHeader`

```csharp
RowHeader
```

A table row header.

[Source](../../Sources/Core/Nazor.Accessibility/SemanticTree.cs#L151)


### `MenuCheckBox`

```csharp
MenuCheckBox
```

A checked menu command.

[Source](../../Sources/Core/Nazor.Accessibility/SemanticTree.cs#L154)


### `MenuRadio`

```csharp
MenuRadio
```

An exclusive checked menu command.

[Source](../../Sources/Core/Nazor.Accessibility/SemanticTree.cs#L157)


### `Separator`

```csharp
Separator
```

A noninteractive semantic separator.

[Source](../../Sources/Core/Nazor.Accessibility/SemanticTree.cs#L160)


<a id="type-semantictree"></a>

## `SemanticTree`

Namespace: `Nazor.Accessibility`

```csharp
public sealed class SemanticTree : IDisposable
```

The accessible view of a window, derived from the element tree, the scene geometry and the input state. Layout-only containers are skipped (their children move up to the nearest semantic ancestor), hidden subtrees (`display: none`) are not exposed and the content of buttons, check boxes, text boxes and text becomes their name. `Update` synchronises the tree and reports the differences; actions go through the input system, like keyboard and pointer input. Everything runs on the UI thread.

[Source](../../Sources/Core/Nazor.Accessibility/SemanticTree.cs#L413)


### `SemanticTree`

```csharp
public SemanticTree(Renderer renderer, Scene scene, InputManager input)
```

Initializes a new instance of SemanticTree.

- `renderer`: The renderer that owns the component tree.

- `scene`: The scene used for layout, hit testing, and drawing.

- `input`: The input manager associated with the scene.

[Source](../../Sources/Core/Nazor.Accessibility/SemanticTree.cs#L431)


### `Root`

```csharp
public SemanticNode Root { get; }
```

The window: always the same node, whatever the root components render.

[Source](../../Sources/Core/Nazor.Accessibility/SemanticTree.cs#L442)


### `Focused`

```csharp
public SemanticNode? Focused { get; private set; }
```

The node with the keyboard focus, if any.

[Source](../../Sources/Core/Nazor.Accessibility/SemanticTree.cs#L445)


### `UpdateCount`

```csharp
public int UpdateCount { get; private set; }
```

Number of synchronisations performed (diagnostics and tests).

[Source](../../Sources/Core/Nazor.Accessibility/SemanticTree.cs#L448)


### `FocusChanged`

```csharp
public event Action<SemanticNode?>? FocusChanged;
```

The focus moved to another node (or to none).

[Source](../../Sources/Core/Nazor.Accessibility/SemanticTree.cs#L451)


### `PropertyChanged`

```csharp
public event Action<SemanticNode, SemanticProperty, object?, object?>? PropertyChanged;
```

A property of an existing node changed: node, property, old value, new value.

[Source](../../Sources/Core/Nazor.Accessibility/SemanticTree.cs#L454)


### `ChildrenChanged`

```csharp
public event Action<SemanticNode>? ChildrenChanged;
```

The children of a node changed (added, removed or reordered).

[Source](../../Sources/Core/Nazor.Accessibility/SemanticTree.cs#L457)


### `NodeRemoved`

```csharp
public event Action<SemanticNode>? NodeRemoved;
```

A node left the tree; it stays detached.

[Source](../../Sources/Core/Nazor.Accessibility/SemanticTree.cs#L460)


### `Invoked`

```csharp
public event Action<SemanticNode>? Invoked;
```

A button was invoked through `Invoke`, after its handler ran.

[Source](../../Sources/Core/Nazor.Accessibility/SemanticTree.cs#L463)


### `LiveRegionChanged`

```csharp
public event Action<SemanticNode>? LiveRegionChanged;
```

A live region appeared or its text changed (not on the first synchronisation: what is there when the tree is created is not news).

[Source](../../Sources/Core/Nazor.Accessibility/SemanticTree.cs#L469)


### `Announced`

```csharp
public event Action<SemanticNode, string, AnnouncementPriority>? Announced;
```

A message to speak (`Announce`), from the node with the focus or the window.

[Source](../../Sources/Core/Nazor.Accessibility/SemanticTree.cs#L472)


### `Announce`

```csharp
public void Announce(string message, AnnouncementPriority priority = AnnouncementPriority.Polite)
```

Asks the screen reader to speak a message that is not on the screen, as "Saved" after a command: an announcement (UI Automation notification). Nothing happens without a message.

- `message`: The message (string).

- `priority`: The priority (AnnouncementPriority).

[Source](../../Sources/Core/Nazor.Accessibility/SemanticTree.cs#L480)


### `Invalidate`

```csharp
public void Invalidate()
```

Marks the tree as out of date; render batches and interaction states do it automatically.

[Source](../../Sources/Core/Nazor.Accessibility/SemanticTree.cs#L489)


### `EnsureCurrent`

```csharp
public void EnsureCurrent()
```

Synchronises the tree only if something changed since the last synchronisation.

[Source](../../Sources/Core/Nazor.Accessibility/SemanticTree.cs#L492)


### `Update`

```csharp
public void Update()
```

Synchronises the tree with the elements and raises the change events.

[Source](../../Sources/Core/Nazor.Accessibility/SemanticTree.cs#L498)


### `HitTest`

```csharp
public SemanticNode HitTest(Point point)
```

The deepest node at a point in logical client coordinates; the root if no control is there.

- `point`: The position in logical viewport coordinates.

Returns: The resulting SemanticNode value.

[Source](../../Sources/Core/Nazor.Accessibility/SemanticTree.cs#L561)


### `Find`

```csharp
public SemanticNode? Find(ElementNode element)
```

The node of an element, if the element is exposed.

- `element`: The element to inspect or update.

Returns: The resulting SemanticNode? value.

[Source](../../Sources/Core/Nazor.Accessibility/SemanticTree.cs#L572)


### `SelectedItems`

```csharp
public IReadOnlyList<SemanticNode> SelectedItems(SemanticNode container)
```

The selected items of a list, a tree, a tab list or a combo box (its open list), among the nodes that exist: a virtualized list has only the rows it created.

- `container`: The container (SemanticNode).

Returns: The resulting IReadOnlyList<SemanticNode> value.

[Source](../../Sources/Core/Nazor.Accessibility/SemanticTree.cs#L584)


### `SelectionContainer`

```csharp
public SemanticNode? SelectionContainer(SemanticNode item)
```

The list, tree, tab list or combo box an item is selected in; null for other nodes.

- `item`: The item (SemanticNode).

Returns: The resulting SemanticNode? value.

[Source](../../Sources/Core/Nazor.Accessibility/SemanticTree.cs#L599)


### `Descendants`

```csharp
public IEnumerable<SemanticNode> Descendants()
```

All nodes, depth first, root included.

Returns: The resulting IEnumerable<SemanticNode> value.

[Source](../../Sources/Core/Nazor.Accessibility/SemanticTree.cs#L613)


### `Invoke`

```csharp
public bool Invoke(SemanticNode node)
```

Presses a button. The handler runs later on the dispatcher, so the caller (an assistive technology) is never blocked; false when the node cannot be invoked.

- `node`: The JSON node or retained tree node to inspect.

Returns: true when the operation succeeds or the condition is satisfied; otherwise, false.

[Source](../../Sources/Core/Nazor.Accessibility/SemanticTree.cs#L631)


### `Toggle`

```csharp
public bool Toggle(SemanticNode node)
```

Toggles a check box now, so its new state can be read right after.

- `node`: The JSON node or retained tree node to inspect.

Returns: true when the operation succeeds or the condition is satisfied; otherwise, false.

[Source](../../Sources/Core/Nazor.Accessibility/SemanticTree.cs#L645)


### `SetRangeValue`

```csharp
public bool SetRangeValue(SemanticNode node, double value)
```

Moves a slider to a value, snapped to its step and kept in its range, as the keyboard does.

- `node`: The JSON node or retained tree node to inspect.

- `value`: The value to use.

Returns: true when the operation succeeds or the condition is satisfied; otherwise, false.

[Source](../../Sources/Core/Nazor.Accessibility/SemanticTree.cs#L651)


### `Select`

```csharp
public bool Select(SemanticNode node)
```

Checks a radio button or chooses an item of an open list, as a click does.

- `node`: The JSON node or retained tree node to inspect.

Returns: true when the operation succeeds or the condition is satisfied; otherwise, false.

[Source](../../Sources/Core/Nazor.Accessibility/SemanticTree.cs#L657)


### `TextOf`

```csharp
public string? TextOf(SemanticNode node)
```

The text of a text box whose words and lines assistive technologies read (null for a password).

- `node`: The JSON node or retained tree node to inspect.

Returns: The resulting string? value.

[Source](../../Sources/Core/Nazor.Accessibility/SemanticTree.cs#L666)


### `TextLines`

```csharp
public IReadOnlyList<(int Start, int End)> TextLines(SemanticNode node)
```

The lines of a text box as shown, broken at its width.

- `node`: The JSON node or retained tree node to inspect.

Returns: The resulting IReadOnlyList<(int Start, int End)> value.

[Source](../../Sources/Core/Nazor.Accessibility/SemanticTree.cs#L671)


### `TextBounds`

```csharp
public IReadOnlyList<Rect> TextBounds(SemanticNode node, int start, int end)
```

The rectangles of a part of the text of a text box, in logical client coordinates.

- `node`: The JSON node or retained tree node to inspect.

- `start`: The start offset in the UTF-16 source text.

- `end`: The end offset in the UTF-16 source text.

Returns: The resulting IReadOnlyList<Rect> value.

[Source](../../Sources/Core/Nazor.Accessibility/SemanticTree.cs#L679)


### `TextIndexAt`

```csharp
public int TextIndexAt(SemanticNode node, Point point)
```

The position in the text of a text box closest to a point in logical client coordinates.

- `node`: The JSON node or retained tree node to inspect.

- `point`: The position in logical viewport coordinates.

Returns: The resulting int value.

[Source](../../Sources/Core/Nazor.Accessibility/SemanticTree.cs#L686)


### `ScrollIntoView`

```csharp
public bool ScrollIntoView(SemanticNode node)
```

Scrolls the node into the visible area of its scrolling ancestors, without moving the focus.

- `node`: The JSON node or retained tree node to inspect.

Returns: true when the operation succeeds or the condition is satisfied; otherwise, false.

[Source](../../Sources/Core/Nazor.Accessibility/SemanticTree.cs#L694)


### `SelectText`

```csharp
public bool SelectText(SemanticNode node, int start, int end)
```

Selects a part of the text of a text box, as Shift+arrows do; false for other nodes.

- `node`: The JSON node or retained tree node to inspect.

- `start`: The start offset in the UTF-16 source text.

- `end`: The end offset in the UTF-16 source text.

Returns: true when the operation succeeds or the condition is satisfied; otherwise, false.

[Source](../../Sources/Core/Nazor.Accessibility/SemanticTree.cs#L701)


### `SetSelected`

```csharp
public bool SetSelected(SemanticNode node, bool selected)
```

Adds an item to the selection of a list that selects several, or removes it, as Ctrl+click does; false in other containers or when the item already is so.

- `node`: The JSON node or retained tree node to inspect.

- `selected`: The selected (bool).

Returns: true when the operation succeeds or the condition is satisfied; otherwise, false.

[Source](../../Sources/Core/Nazor.Accessibility/SemanticTree.cs#L711)


### `SetExpanded`

```csharp
public bool SetExpanded(SemanticNode node, bool expanded)
```

Opens or closes a combo box or a menu, as a click does; false when it already is so.

- `node`: The JSON node or retained tree node to inspect.

- `expanded`: The expanded (bool).

Returns: true when the operation succeeds or the condition is satisfied; otherwise, false.

[Source](../../Sources/Core/Nazor.Accessibility/SemanticTree.cs#L719)


### `SetValue`

```csharp
public bool SetValue(SemanticNode node, string value)
```

Replaces the value of a text box, as if the user typed it.

- `node`: The JSON node or retained tree node to inspect.

- `value`: The value to use.

Returns: true when the operation succeeds or the condition is satisfied; otherwise, false.

[Source](../../Sources/Core/Nazor.Accessibility/SemanticTree.cs#L729)


### `Focus`

```csharp
public bool Focus(SemanticNode node)
```

Gives the keyboard focus to a focusable node and brings it into view.

- `node`: The JSON node or retained tree node to inspect.

Returns: true when the operation succeeds or the condition is satisfied; otherwise, false.

[Source](../../Sources/Core/Nazor.Accessibility/SemanticTree.cs#L734)


### `ScrollBy`

```csharp
public bool ScrollBy(SemanticNode node, float deltaY)
```

Scrolls an enabled node with an NSS scroll capability by `deltaY` logical units.

- `node`: The JSON node or retained tree node to inspect.

- `deltaY`: The vertical scroll delta in the specified unit.

Returns: true when the operation succeeds or the condition is satisfied; otherwise, false.

[Source](../../Sources/Core/Nazor.Accessibility/SemanticTree.cs#L748)


### `ScrollBy`

```csharp
public bool ScrollBy(SemanticNode node, float deltaX, float deltaY)
```

Scrolls a node with a scroll capability on both axes, in logical units; false when the request is unchanged or unavailable.

- `node`: The JSON node or retained tree node to inspect.

- `deltaX`: The horizontal scroll delta in the specified unit.

- `deltaY`: The vertical scroll delta in the specified unit.

Returns: true when the operation succeeds or the condition is satisfied; otherwise, false.

[Source](../../Sources/Core/Nazor.Accessibility/SemanticTree.cs#L755)


### `ScrollTo`

```csharp
public bool ScrollTo(SemanticNode node, double? left, double? top)
```

Requests absolute logical offsets for an enabled scroll container; null preserves an axis. Honors NSS smooth scrolling.

- `node`: The accessible scroll container.

- `left`: Horizontal offset, or null to keep the current destination on that axis.

- `top`: Vertical offset, or null to keep the current destination on that axis.

Returns: True when a new destination is accepted; false when unchanged, detached, disabled or not scrollable.

[Source](../../Sources/Core/Nazor.Accessibility/SemanticTree.cs#L763)


### `Dismiss`

```csharp
public bool Dismiss(SemanticNode node)
```

Requests ordinary dialog dismissal, including AlertDialog cancellation rules.

[Source](../../Sources/Core/Nazor.Accessibility/SemanticTree.cs#L767)


### `ContainingTable`

```csharp
public SemanticNode? ContainingTable(SemanticNode node)
```

Gets the nearest table containing a cell or row, or the table itself.

[Source](../../Sources/Core/Nazor.Accessibility/SemanticTree.cs#L777)


### `TableCells`

```csharp
public IReadOnlyList<SemanticNode> TableCells(SemanticNode table)
```

Gets cells and header cells of this table, excluding any nested table.

[Source](../../Sources/Core/Nazor.Accessibility/SemanticTree.cs#L785)


### `TableCellAt`

```csharp
public SemanticNode? TableCellAt(SemanticNode table, int row, int column)
```

Gets the cell covering a zero-based logical coordinate, including spanned coordinates.

[Source](../../Sources/Core/Nazor.Accessibility/SemanticTree.cs#L792)


### `TableHeaders`

```csharp
public IReadOnlyList<SemanticNode> TableHeaders(SemanticNode node, bool columns)
```

Gets row or column headers overlapping the corresponding cell coordinates.

[Source](../../Sources/Core/Nazor.Accessibility/SemanticTree.cs#L796)


### `Dispose`

```csharp
public void Dispose()
```

Releases the resources and event subscriptions owned by this instance.

[Source](../../Sources/Core/Nazor.Accessibility/SemanticTree.cs#L1224)


<a id="type-textunit"></a>

## `TextUnit`

Namespace: `Nazor.Accessibility`

```csharp
public enum TextUnit
```

The units a screen reader moves by in a text, with the values of UI Automation.

[Source](../../Sources/Core/Nazor.Accessibility/TextUnits.cs#L6)


### `Character`

```csharp
Character = 0
```

Navigates or expands by one character.

[Source](../../Sources/Core/Nazor.Accessibility/TextUnits.cs#L9)


### `Format`

```csharp
Format = 1
```

Navigates or expands by a span of consistently formatted text.

[Source](../../Sources/Core/Nazor.Accessibility/TextUnits.cs#L12)


### `Word`

```csharp
Word = 2
```

Navigates or expands by one word.

[Source](../../Sources/Core/Nazor.Accessibility/TextUnits.cs#L15)


### `Line`

```csharp
Line = 3
```

Navigates or expands by one displayed text line.

[Source](../../Sources/Core/Nazor.Accessibility/TextUnits.cs#L18)


### `Paragraph`

```csharp
Paragraph = 4
```

Navigates or expands by one paragraph.

[Source](../../Sources/Core/Nazor.Accessibility/TextUnits.cs#L21)


### `Page`

```csharp
Page = 5
```

Navigates or expands by one page of text.

[Source](../../Sources/Core/Nazor.Accessibility/TextUnits.cs#L24)


### `Document`

```csharp
Document = 6
```

Navigates or expands to the entire document.

[Source](../../Sources/Core/Nazor.Accessibility/TextUnits.cs#L27)


<a id="type-textunits"></a>

## `TextUnits`

Namespace: `Nazor.Accessibility`

```csharp
public static class TextUnits
```

The boundaries of the units of a text and the moves between them, as the Text pattern of UI Automation asks for: characters are graphemes, a word takes the spaces after it (a punctuation mark is a word of its own), lines are those shown (broken at the width of the box), paragraphs end with a line break; a text without formats is one format, one page, one document.

[Source](../../Sources/Core/Nazor.Accessibility/TextUnits.cs#L36)


### `Boundaries`

```csharp
public static int[] Boundaries(string text, TextUnit unit, IReadOnlyList<(int Start, int End)> lines)
```

The positions where the units start, in order, with 0 and the length of the text.

- `text`: The text to process.

- `unit`: The unit used for the scroll deltas.

- `lines`: The lines (IReadOnlyList<(int Start, int End)>).

Returns: The resulting int[] value.

[Source](../../Sources/Core/Nazor.Accessibility/TextUnits.cs#L43)


### `Enclosing`

```csharp
public static (int Start, int End) Enclosing(int[] boundaries, int position)
```

The unit that contains `position` (at the end of the text, the last one).

- `boundaries`: The boundaries (int[]).

- `position`: The position (int).

Returns: The resulting (int Start, int End) value.

[Source](../../Sources/Core/Nazor.Accessibility/TextUnits.cs#L70)


### `Move`

```csharp
public static int Move(int[] boundaries, ref int position, int count)
```

Moves a position by `count` boundaries (forward when positive); a position between two boundaries reaches the next one with the first step. Returns the steps made.

- `boundaries`: The boundaries (int[]).

- `position`: The position (int).

- `count`: The count (int).

Returns: The resulting int value.

[Source](../../Sources/Core/Nazor.Accessibility/TextUnits.cs#L86)


### `MoveRange`

```csharp
public static int MoveRange(int[] boundaries, ref int start, ref int end, int count)
```

Moves a range by whole units: a range with text becomes the unit `count` units after (or before) the one that contains its start; an empty range moves its position. Returns the units moved.

- `boundaries`: The boundaries (int[]).

- `start`: The start offset in the UTF-16 source text.

- `end`: The end offset in the UTF-16 source text.

- `count`: The count (int).

Returns: The resulting int value.

[Source](../../Sources/Core/Nazor.Accessibility/TextUnits.cs#L107)

## API signature index

These declarations supplement the contracts above. Inherited members are documented with the declaring type.

### `AnnouncementPriority`

Namespace `Nazor.Accessibility`. [Source](../../Sources/Core/Nazor.Accessibility/SemanticTree.cs)

| Declaration | Contract |
| --- | --- |
| `Polite = 0` | Queues the announcement after current speech. |
| `Assertive = 1` | Requests an immediate announcement that may interrupt current speech. |

### `LiveSetting`

Namespace `Nazor.Accessibility`. [Source](../../Sources/Core/Nazor.Accessibility/SemanticTree.cs)

| Declaration | Contract |
| --- | --- |
| `Off = 0` | Disables automatic announcements for changes in the live region. |
| `Polite = 1` | Announces changes when assistive technology finishes its current speech. |
| `Assertive = 2` | Requests an immediate announcement that may interrupt current speech. |

### `RangeInfo`

Namespace `Nazor.Accessibility`. [Source](../../Sources/Core/Nazor.Accessibility/SemanticTree.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed record RangeInfo : IEquatable<RangeInfo>` | The range of a slider or a progress bar, as UI Automation reports it.<br>`Minimum`. The minimum (double).<br>`Maximum`. The maximum (double).<br>`Value`. The numeric value or payload represented by this record.<br>`SmallChange`. The small change (double).<br>`LargeChange`. The large change (double).<br>`IsReadOnly`. The is read only (bool). |
| `public RangeInfo(double Minimum, double Maximum, double Value, double SmallChange, double LargeChange, bool IsReadOnly)` | The range of a slider or a progress bar, as UI Automation reports it.<br>`Minimum`. The minimum (double).<br>`Maximum`. The maximum (double).<br>`Value`. The numeric value or payload represented by this record.<br>`SmallChange`. The small change (double).<br>`LargeChange`. The large change (double).<br>`IsReadOnly`. The is read only (bool). |
| `public override string ToString()` |  |
| `public static bool operator !=(RangeInfo? left, RangeInfo? right)` |  |
| `public static bool operator ==(RangeInfo? left, RangeInfo? right)` |  |
| `public override int GetHashCode()` |  |
| `public override bool Equals(object? obj)` |  |
| `public bool Equals(RangeInfo? other)` |  |
| `public void Deconstruct(out double Minimum, out double Maximum, out double Value, out double SmallChange, out double LargeChange, out bool IsReadOnly)` |  |
| `public double Minimum { get; init; }` | The minimum (double). |
| `public double Maximum { get; init; }` | The maximum (double). |
| `public double Value { get; init; }` | The numeric value or payload represented by this record. |
| `public double SmallChange { get; init; }` | The small change (double). |
| `public double LargeChange { get; init; }` | The large change (double). |
| `public bool IsReadOnly { get; init; }` | The is read only (bool). |

### `SemanticProperty`

Namespace `Nazor.Accessibility`. [Source](../../Sources/Core/Nazor.Accessibility/SemanticTree.cs)

| Declaration | Contract |
| --- | --- |
| `Name = 0` | Identifies changes to the accessible name. |
| `Description = 1` | Identifies changes to descriptive help text. |
| `IsRequired = 2` | Identifies changes to required-value semantics. |
| `IsInvalid = 3` | Identifies changes to invalid-value semantics. |
| `IsEnabled = 4` | Identifies changes to the enabled state. |
| `IsFocusable = 5` | Identifies changes to whether keyboard focus is supported. |
| `IsChecked = 6` | Identifies changes to the checked state. |
| `Value = 7` | Identifies changes to the exposed text or control value. |
| `IsOffscreen = 8` | Identifies changes to whether the node is outside the visible viewport. |
| `RangeValue = 9` | Identifies changes to the numeric range value. |
| `IsExpanded = 10` | Identifies changes to the expanded or collapsed state. |
| `TextSelection = 11` | Identifies changes to the accessible text selection. |
| `ScrollAvailable = 12` | Identifies changes to the availability of an NSS scroll container. |
| `HorizontalScrollPercent = 13` | Identifies changes to horizontal scroll progress in percent, or -1 when that axis cannot scroll. |
| `VerticalScrollPercent = 14` | Identifies changes to vertical scroll progress in percent, or -1 when that axis cannot scroll. |
| `HorizontalViewSize = 15` | Identifies changes to the horizontal viewport as a percentage of content width. |
| `VerticalViewSize = 16` | Identifies changes to the vertical viewport as a percentage of content height. |
| `HorizontallyScrollable = 17` | Identifies changes to whether content overflows the horizontal scroll viewport. |
| `VerticallyScrollable = 18` | Identifies changes to whether content overflows the vertical scroll viewport. |

### `SemanticRole`

Namespace `Nazor.Accessibility`. [Source](../../Sources/Core/Nazor.Accessibility/SemanticTree.cs)

| Declaration | Contract |
| --- | --- |
| `Window = 0` | Exposes the window role to assistive technologies. |
| `Group = 1` | Exposes the group role to assistive technologies. |
| `Text = 2` | Exposes the text role to assistive technologies. |
| `Button = 3` | Exposes the button role to assistive technologies. |
| `CheckBox = 4` | Exposes the check box role to assistive technologies. |
| `TextBox = 5` | Exposes the text box role to assistive technologies. |
| `Image = 6` | Exposes the image role to assistive technologies. |
| `Menu = 7` | Exposes the menu role to assistive technologies. |
| `MenuItem = 8` | Exposes the menu item role to assistive technologies. |
| `ToolTip = 9` | Exposes the tool tip role to assistive technologies. |
| `RadioButton = 10` | Exposes the radio button role to assistive technologies. |
| `Switch = 11` | Exposes the switch role to assistive technologies. |
| `Slider = 12` | Exposes the slider role to assistive technologies. |
| `ProgressBar = 13` | Exposes the progress bar role to assistive technologies. |
| `ComboBox = 14` | Exposes the combo box role to assistive technologies. |
| `List = 15` | Exposes the list role to assistive technologies. |
| `ListItem = 16` | Exposes the list item role to assistive technologies. |
| `Tree = 17` | Exposes the tree role to assistive technologies. |
| `TreeItem = 18` | Exposes the tree item role to assistive technologies. |
| `TabList = 19` | Exposes the tab list role to assistive technologies. |
| `TabItem = 20` | Exposes the tab item role to assistive technologies. |
| `Splitter = 21` | Exposes the splitter role to assistive technologies. |
| `Hyperlink = 22` | Exposes the hyperlink role to assistive technologies. |
| `Status = 23` | Exposes the status role to assistive technologies. |
| `SplitButton = 24` | Exposes the split button role to assistive technologies. |
| `MenuBar = 25` | Exposes the menu bar role to assistive technologies. |
| `ToolBar = 26` | Exposes the tool bar role to assistive technologies. |
| `Calendar = 27` | Exposes the calendar role to assistive technologies. |
| `TitleBar = 28` | Exposes the title bar role to assistive technologies. |
| `DocumentList = 29` | A document list without selection behavior. |
| `DocumentListItem = 30` | An item in a document list. |
| `DefinitionList = 31` | A collection of terms and descriptions. |
| `DefinitionTerm = 32` | A term in a definition list. |
| `DefinitionDescription = 33` | A description associated with a term. |
| `Heading = 34` | A document heading with a level. |
| `Form = 35` | A named form and its submission scope. |
| `Dialog = 36` | A window-local dialog. |
| `AlertDialog = 37` | A dialog requiring a decision or acknowledgement. |
| `ToggleButton = 38` | A button with persistent pressed state. |
| `Meter = 39` | A read-only measurement within a range. |
| `Table = 40` | A static semantic table. |
| `RowGroup = 41` | A table row group. |
| `Row = 42` | A table row. |
| `Cell = 43` | A table data cell. |
| `ColumnHeader = 44` | A table column header. |
| `RowHeader = 45` | A table row header. |
| `MenuCheckBox = 46` | A checked menu command. |
| `MenuRadio = 47` | An exclusive checked menu command. |
| `Separator = 48` | A noninteractive semantic separator. |

### `SemanticTree`

Namespace `Nazor.Accessibility`. [Source](../../Sources/Core/Nazor.Accessibility/SemanticTree.cs)

| Declaration | Contract |
| --- | --- |
| `public SemanticNode? Focused { get; }` | The node with the keyboard focus, if any. |
| `public int UpdateCount { get; }` | Number of synchronisations performed (diagnostics and tests). |
