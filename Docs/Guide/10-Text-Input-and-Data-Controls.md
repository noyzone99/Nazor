# Text input and data controls

## Format running text

Use `Paragraph` or `TextBlock` for a horizontal text flow with styled inline segments. Text, `Kbd` and `Link` share wrapping, selection and baselines in prose; flex/grid containers continue to arrange their children normally. Enable `IsTextSelectionEnabled` on the paragraph to select and copy its normalized visible text across formatting boundaries.

```nazor
<Paragraph IsTextSelectionEnabled="true">
    Save <Strong>all changes</Strong> with <Kbd>Ctrl</Kbd> + <Kbd>S</Kbd>.
    <InlineCode>Theme.Seed</InlineCode> controls the <Mark>accent</Mark>.
    H<Subscript>2</Subscript>O and x<Superscript>2</Superscript> share the line.
</Paragraph>
```

`Span` supplies a general styled segment; `Strong`, `Emphasis`, `SmallText`, `Deleted` and `Inserted` express common text treatments. Use `InlineCode` within a sentence and `CodeBlock` for a listing. `Quote.OpeningQuote` and `ClosingQuote` configure visible punctuation included in copied and accessible text; set them explicitly for localized quotation conventions. `LineBreak` creates a hard line break. `Abbreviation.Tooltip` or `AccessibleDescription` provides its expansion as described accessible text without adding a Tab stop. Ordinary decorative spans are not repeated as separate accessible text children; inline links retain their action and keyboard focus. A drag that starts within an actionable Link follows its link interaction and does not start paragraph selection; start the drag in the surrounding selectable paragraph to select across links.

Themes own fonts and decoration. Text font size/weight/style, inherited line-height and relative text positioning contribute to a shared line strut. Inline backgrounds, borders and focus outlines follow the individual wrapped fragments; empty gaps inside a wrapped link's bounding rectangle do not activate it. Whitespace collapses across inline boundaries using the selected white-space mode; NBSP and explicit hard breaks are preserved.

This is bounded horizontal text-only flow. General browser bidi/RTL, inline replaced controls/images, inline-block, mixed block/inline formatting boxes, generated inline pseudo-elements, decoration slicing, rich-text editing and inline ellipsis are not supported. Keep a grapheme or ligature within one shaping/color style; partial-ligature caret/color subdivision and cross-style complex-script shaping are not provided. Border/radius decoration is cloned per line fragment. See [support limits](32-Support-and-Limitations.md#nss-and-rendering).

Low-level composition can use `TextRun`/`ShapedText` with shared UTF-16 coordinates and `DrawText.TextStart`/`TextLength` to paint source ranges without reshaping. `LayoutNode.ArrangeChildren` lets measured content retain child geometry for input and inspection; it is not an alternate general layout engine.

## Choose an editor

`TextField` and `TextArea` bind an editable string through `Value` and `ValueChanged`. Use `PasswordField` for sensitive input. Use `Field` for a label and associated description or validation error.

Keyboard focus is drawn on the compound field's frame, without a second outline around its internal text editor. Fluent uses its focused underline and NazorLeaf its focus glow; unframed appearances and high-contrast colors retain a frame cue. `DateTimePicker` owns one cue around its date/time pair. Standalone `EditableText`, PIN cells and independently focusable field buttons keep their own keyboard focus indicators. Customize the field frame's focus style rather than adding another outline to its input part.

```nazor
<Field LabelText="Account">
    <TextField @bind-Value="account" Placeholder="Account name" />
</Field>
<Field LabelText="Notes">
    <TextArea @bind-Value="notes" Rows="4" />
</Field>

@code {
    private string account = "";
    private string notes = "";
}
```

`NumberField` binds a nullable `double` and handles formatting and bounds. `DatePicker` binds a nullable `DateOnly`. In Fluent, its trailing button draws the packaged calendar icon; register the theme fonts with `AddFluentFonts()`. The field continues to accept typed dates. The pickers and numeric editor accept a culture when the application needs formatting different from the current culture. NSS `text-align` on the editable input applies during editing as well as display; caret placement, selection and pointer hit testing follow the same alignment.

`NumberField.SpinButtons` selects `Compact`, `Inline` or `Hidden`. Compact uses stacked in-field buttons in Basic/NazorLeaf and a focused-editor flyout in Fluent. Inline places buttons on opposite sides in Basic/NazorLeaf and adjacent at the trailing edge in Fluent. Fluent's Compact flyout keeps editor focus while stepping; Escape or an outside press dismisses it, and its toggle or focus re-entry opens it again. Disabled/read-only fields do not open it. The gallery offers all three modes with a bound value.

Pre-release migration: rename `SpinButtonPlacement.Stacked` to `Compact` and `Sides` to `Inline`; NSS attributes become `spin-buttons="compact"`/`"inline"`. These names describe the presentation's intent, with each theme supplying its layout.

Enable `NumberField.AcceptsExpression` to accept arithmetic; it defaults to false, so existing editors keep numeric-only parsing. Operators are `^`, then `* /`, then `+ -`; equal-precedence operators, including powers, evaluate left to right. Unary signs precede powers, and parentheses override precedence. Operands use `Culture`, including decimal/group separators and scientific notation. A trailing `Unit` applies to the complete expression. Expressions contain at most 4096 characters and 64 nested parentheses; names, functions and implicit multiplication are rejected.

Enter, focus loss or stepping evaluates pending text and clamps the result to the configured bounds. Incomplete text stays editable. Invalid input, division by zero and non-finite results restore the previous value on commit. `Format` controls the displayed precision without rounding the stored `double`. The gallery includes an opt-in checkbox and an expression example.

```nazor
<NumberField @bind-Value="quantity" Minimum="0" Maximum="100" Format="N2" AcceptsExpression="true" />
<DatePicker @bind-Value="delivery" />

@code {
    private double? quantity;
    private DateOnly? delivery;
}
```

## Choose dates

`Calendar` selects one date, several dates or a range and supports month, year and decade navigation. Header Tab order is the title, previous and next navigation, then the current date; disabled header controls are skipped. Basic and NazorLeaf display the navigation on either side of the title, while Fluent displays the left title followed by trailing up/down navigation.

The weekday band is decorative and uses the calendar culture. Each `CalendarWeekday` contains `CalendarWeekdayAbbreviated` and `CalendarWeekdayShortest` text parts. Basic and NazorLeaf show the abbreviated part; Fluent shows the shortest part, such as Su/Mo in en-US. Custom themes should display exactly one. These labels do not add semantic announcements.

Fluent's month grid uses 40px day boxes with 1px margins. Selected dates use an accent outline; Today has a separate filled indicator, including hover, focus and disabled treatments. The date popup uses the calendar edge without an extra padded border. `DatePicker` remains an editable date field; segmented date entry is a separate interaction. Interface fonts follow installed fallbacks. Calendar view changes retain one disabled outgoing grid while the incoming grid appears. Fluent supplies separate outgoing/incoming zoom phases; reduced motion switches immediately. See [content and layout motion](23-Transitions-and-Animations.md#content-and-layout-motion).

Fluent year/decade grids retain the month view's footprint, including a week-number column or two displayed months, and use 56px circular period indicators with a filled selected state. Calendar emits `--nz-calendar-month-count` and `--nz-calendar-week-columns` as theme geometry metadata; application `Style` declarations can override them. The same layout is used inside DatePicker.

Pre-release theme migration: the Calendar header's physical order is now title/previous/next to make keyboard traversal consistent. Use `order` on its existing parts to preserve a custom visual order. Weekday text now has the two child parts above; custom themes that draw weekday text should choose the appropriate part. Raw subtree-text inspection includes both labels even when one is hidden.

## Adjust numeric ranges

`Dial.WheelChanges` is opt-in. An enabled dial under the pointer consumes unmodified vertical wheel gestures without moving keyboard focus, including at its bounds. One notch or 48 logical precision-scroll pixels advances `Step`; fractional notches accumulate. With `Step="0"`, movement is continuous at 1% of the range per notch. Each effective change raises `ValueChanged`, `oninput` and `onchange`; disabled, modified or horizontal-only gestures keep normal routing. Unlike NumberField's focus-gated wheel option, the dial responds while hovered. Themes draw its keyboard cue around the circular pointer and use their own hover/pressed colors.

`Slider` and `RangeSlider` support horizontal or vertical `Orientation`, `IsDirectionReversed`, visible ticks through `TickFrequency`, optional `ShowTickLabels`, and `ValueLabel`. Set an explicit height for a vertical slider. `Step` snaps interactive values from `Minimum`; zero permits continuous adjustment. `TickFrequency` only draws marks. To snap to uniformly spaced ticks, set `Step` to the same interval. Programmatic bound values are clamped to the range without being rounded onto the step grid. `RangeSlider.MinimumRange` preserves a minimum distance between its thumbs.

The gallery's **Slider / RangeSlider** page lets users change orientation, tick labels, direction and continuous/step/tick snapping while observing bound values.

NazorLeaf's thumb gaps show the actual parent surface with every value-label mode. Hover and pressed thumb geometry stays centered for horizontal and vertical sliders; a custom surface-color token is unnecessary.

## Pick files and folders

`FileField.Pick` supplies the picker operation. Inject `IFileDialogService` in a window component and call `OpenFileAsync`, `OpenFilesAsync` or `PickFolderAsync` for single files, multiple files or a folder. Set `Multiple="true"` for multiple returned paths. Return null or an empty list on cancellation to preserve the previous selection. `FileFilter` accepts named glob patterns, such as `new FileFilter("Images", "*.png", "*.jpg")`; it is not a MIME/UTI filter contract. Folder selection does not apply file filters.

The gallery's **FileField / TokenField** example invokes the current window's service, exposes PDF/image/all-file filters and displays the resulting paths. It reports hosts without a native picker. Windows and Linux desktop adapters provide platform dialogs; headless test doubles verify invocation and cancellation without opening a system dialog. See [platform integration](14-Title-Bars-and-Platform-Integration.md).

## Choose segmented dates and times

The classic editable `TimePicker` supports columns and a list presentation. Fluent gives both inset, spaced 32px rows and a leading accent pill on selected values, independently of hover and keyboard focus. The segmented picker below remains a separate presentation.

Use `SegmentedDatePicker` and `SegmentedTimePicker` for localized segmented entries with a draft selector dialog. Existing `DatePicker` remains an editable date field with a calendar popup, and existing `TimePicker` remains an editable time field. The segmented controls share reusable framework behavior and have Basic, Fluent and NazorLeaf appearances.

```nazor
<Field LabelText="Arrival date">
    <SegmentedDatePicker @bind-Value="date" />
</Field>
<Field LabelText="Arrival time">
    <SegmentedTimePicker @bind-Value="time" Use24Hour="true" MinuteIncrement="15" />
</Field>
@code {
    private DateOnly? date;
    private TimeOnly? time;
}
```

A null value displays segment placeholders. The entry exposes accessibility Expand/Collapse; collapsing discards the draft. Open a segment by clicking it or focus the entry and press Enter, Space, F4 or Alt+Down. Left/Right select an entry part. In the popup, Up/Down, Page Up/Down and Home/End change the active column, and Left/Right move between columns. Wheel input and captured primary-pointer dragging step through selector rows. At most nine rows per column are realized, including for the full year range. This selector does not provide kinetic scrolling.

Column movement changes only `DraftValue`. Enter or Confirm commits a changed value once and raises value/input/change notifications; Cancel, Escape, F4/Alt+Up or outside dismissal leave `Value` unchanged. The selected row aligns with the entry center when work-area bounds allow; the shared overlay policy returns focus to the entry when closing. External value/configuration changes, disabling or making the control read-only cancel an open draft. `IsOpen`/`IsOpenChanged` allow controlled opening; `OpenAsync`, `CancelAsync` and `ConfirmAsync` expose the same lifecycle. `Required`/`Invalid` remain application-owned validation states.

`SegmentedDatePicker.Culture` sets the date part order and formatting. Configure `DayFormat`, `MonthFormat` and `YearFormat` with custom .NET date patterns; a single part letter is treated as a custom pattern. `DayVisible`, `MonthVisible` and `YearVisible` hide corresponding entry parts and columns; at least one must remain visible. Hidden components retain their value where bounds allow. Month/year edits clamp leap days and month lengths. `Minimum`/`Maximum` are inclusive; `Today` supplies the initial date for an unset picker, otherwise the computer's date is used. Only Gregorian calendars are supported.

`SegmentedTimePicker.Use24Hour` defaults to the culture's clock format. `ShowSeconds` adds seconds; hidden seconds and fractional ticks retain their committed value where bounds allow. `MinuteIncrement` ranges from 1 to 60. An existing off-increment minute remains available instead of being silently rounded; an unset picker starts from the computer's current time rounded down to its increment. Time bounds are inclusive and cannot cross midnight. Programmatic values display as supplied; opening clamps the draft to bounds, and confirmation applies it. Set the bound value to null in application code to clear it.

See [segmented picker examples](../../Samples/Nazor.Samples.Gallery/Examples/Pickers/SegmentedPickersSample.nazor) for culture order, formatted/hidden parts, 12/24-hour clocks, seconds and bounded times. Hosts translating touch into primary-pointer events can use the same column dragging; native touch classification is not required.

## Choose from known items

Select, ComboBox and anchored Combobox suggestions expose `DropDownPlacement`: Auto, Top, Bottom or Center. Center overlays the field, aligning a selected option with the field center where space permits; an unset selection centers the whole list. The surface shifts into the client viewport, or the monitor work area when external native popups are enabled. Native rejection falls back to the client viewport. Oversized lists retain the preferred visible-item limits, resize to complete available rows and scroll; centered opening also reveals a committed ComboBox selection. Editor focus stays in the combo editor. Fluent centered lists reveal in both vertical directions. Auto remains the default.

The [Select example](../../Samples/Nazor.Samples.Gallery/Examples/Fields/SelectSample.nazor) lets you switch all four placements; the [search/combo example](../../Samples/Nazor.Samples.Gallery/Examples/Fields/SearchComboSample.nazor) demonstrates an initially selected centered ComboBox.

`SearchField<TItem>.IconPlacement` accepts `ThemeDefault`, `Start` or `End`. Basic/NazorLeaf default to a leading search mark; Fluent defaults to a trailing packaged search symbol. The mark moves independently of the scope selector and clear button. The gallery demonstrates both overrides with suggestions and scopes; no system icon font is required for the Fluent/Leaf symbols.

Fluent search and clear actions use centered 12px symbols inside wider action areas. The clear action in TextField and SearchField uses a dismiss X, with the same spacing in plain fields and fields with suggestions; available native symbols retain packaged fallbacks.

Use `Select<TValue>` when the user chooses one known value. Pass `Items` and an optional display function, or declare `SelectItem` children. Use `ComboBox<TItem>` when users should be able to type to filter suggestions. Its `Text` is the editable string and its `Value` is the selected item. With `AllowCustomValue`, free text stays in `Text` and the selected item becomes the default value for `TItem` when the text does not match an item.

Up and Down stop at the first and last enabled options. Keyboard navigation scrolls the active option into view; opening a Select also reveals its selected option. ComboBox keeps keyboard focus in its text editor while revealing the current suggestion. The shared `Combobox` primitive supplies the same behavior to search suggestions, token suggestions, command palettes and inline lists.

ComboBox marks its committed value as selected independently of the current hovered or keyboard-highlighted suggestion. The shared `Combobox<TItem>.IsItemSelected` predicate provides this distinction; when omitted, the primitive retains selection following the current suggestion. Fluent choice lists show a leading accent pill for the committed selection and a hover/current backplate independently. Fluent Select omits the extra check glyph.

Opening a Fluent Select keeps its neutral frame. Keyboard focus uses the separate focus ring, including forced colors; expansion alone does not add an accent border.

Set `ComboBox.Placeholder` to show a hint while its editable text is empty. In Basic, the combo's suggestion popup and rows use the same border, surface colors and padding as `Select`; the current suggestion uses the selection colors during keyboard and pointer navigation. `EmptyContent` supplies the content shown when no items match.

```nazor
<Field LabelText="Departure">
    <Select TValue="string" Items="cities" @bind-Value="departure" />
</Field>
<Field LabelText="Destination">
    <Description>Type to filter the city suggestions.</Description>
    <ChildContent>
        <ComboBox TItem="string" Items="cities" @bind-Text="destinationText"
                  @bind-Value="destination" AllowCustomValue="true" Placeholder="Type a city" />
    </ChildContent>
</Field>

@code {
    private readonly string[] cities = ["Milan", "Paris", "Rome", "Vienna"];
    private string departure = "Milan";
    private string? destinationText;
    private string? destination;
}
```

## Display and select collections

Use `ListView<TItem>` for rows, `GridView<TItem>` for tiles, and `TreeView<TItem>` for a hierarchy of roots and children. Use `Table` for document content with explicit headers or `DataGrid<TItem>` for interactive tabular data. Sorting, filtering, and editing belong to the grid or its data source. Each collection control owns its keyboard navigation and selection behavior.

Give a virtualized list a constrained viewport. ListView/TreeView `ItemHeight` and DataGrid `RowHeight` default to null: a semantic-hidden theme metric supplies the virtualizer's actual row pitch, including live density changes. Basic uses 32/28/32 logical units respectively; Fluent uses 40/28/32; Leaf uses 46/46/48 in Compact and 54/54/56 in Touch. DataGrid's Leaf pitch includes its 2px gap. Explicit positive finite values override density; `MeasureItems` uses the List/Tree value as an estimate for variable content. GridView tile dimensions remain explicit. Bind `SelectedItem` for a single selection, or use `SelectedItems` with the appropriate `SelectionMode` for multiple items. Try this example using Tab to focus the list and the arrow keys to navigate it.

`Virtualizer<TItem>` reuses its rows while a scroll stays within the realized range. Repeated measurements with unchanged item sizes also preserve those rows; a changed range or item size rebuilds them. A parent render can still update item templates and parameters.

`Listbox`, `ListView`, `GridView` and `TreeView` keep the keyboard target visible and stop arrow navigation at their enabled ends. Home, End and page navigation reveal their target rows as well. GridView uses Ctrl+Home and Ctrl+End for the whole collection.

```nazor
<ListView TItem="string" Items="cities" @bind-SelectedItem="selection"
          AccessibleName="Delivery cities" ItemHeight="30" Style="height: 120px" />
<TextBlock>@(selection ?? "Choose a city")</TextBlock>

@code {
    private readonly string[] cities = ["Milan", "Paris", "Rome", "Vienna", "Turin", "Naples"];
    private string? selection;
}
```

For item templates, loading, and data providers, see the runnable [ListView](../../Samples/Nazor.Samples.Gallery/Examples/Collections/ListViewSample.nazor), [TreeView](../../Samples/Nazor.Samples.Gallery/Examples/Collections/TreeViewSample.nazor), and [DataGrid](../../Samples/Nazor.Samples.Gallery/Examples/Data/DataGridSample.nazor) examples. See the [catalog](09-Control-Catalog.md), [layout guide](20-Layout-and-Scrolling.md), and [composition guide](36-Advanced-Controls-and-Composition.md) for related controls.

Set `CanReorderItems=true` and handle `OnItemReorder` to enable a primary drag after 6 logical units or Ctrl+Shift+arrows (Meta+Shift is also accepted). Grid Up/Down moves by its current column count. `ItemReorderRequest<TItem>` contains the item, its old index and its final index after removal/insertion. Update your model and supply the new Items/Roots; the control never mutates it. Supply stable unique List/Grid keys. List moves remain within one group; Tree moves whole expanded subtrees within their existing siblings and never reparents. Tree request indexes are sibling-relative. Call TreeView.Refresh after in-place hierarchy changes. Selection/current focus follows identity without a duplicate selection event.

Escape, lost capture, disabled items/owners or stale source identity cancel a drag. Embedded editors/buttons/checks and Tree expanders keep their gestures. Drop markers describe only valid realized targets; this does not implement multi-item dragging, cross-collection transfer, native file drag/drop or edge auto-scroll. Retained realized rows relocate using the theme's transform transition; insertion/removal repositions their surviving neighbors. Virtualization does not retain removed offscreen data for exit animation; use `ItemsTransition<TItem>` when explicit bounded departing-content retention is required.

Pre-release migration: List/Tree ItemHeight and DataGrid RowHeight are now nullable. Code reading them as nonnullable numbers must provide a fallback or choose an explicit value. Custom themes can style the noninteractive ListViewRowMetric, TreeViewRowMetric and DataGridRowMetric parts. DataGrid reserves `--nz-data-row-height` for resolved pitch and `--nz-data-row-gap` for its internal theme gap.

## Browse one item at a time

`FlipView<TItem>` displays one item from `Items`. Supply `ItemTemplate` for images, cards or interactive content and bind `SelectedIndex` to the zero-based position. An empty list reports `CurrentIndex == -1`; nonempty lists clamp parameter selection to the available range. Reordering follows the index, and normalization does not emit a user-selection callback.

```nazor
<FlipView TItem="string" Items="pages" @bind-SelectedIndex="page"
          AccessibleName="Getting started" Style="width: 320px; height: 180px">
    <ItemTemplate Context="title"><Heading Level="2">@title</Heading></ItemTemplate>
</FlipView>
@code {
    private readonly string[] pages = ["Welcome", "Choose a workspace", "Start building"];
    private int page;
}
```

Only the selected template is realized. Its local component state is disposed when navigation leaves it, so store persistent edits in the item model. Previous/Next buttons stop at the bounds. Left/Right/Home/End work when the FlipView itself has focus; descendant editors keep their keys. Position/count semantics and a polite position announcement accompany selection. If focused content disappears or a focused navigation action reaches its bound, focus returns to the FlipView.

`SwipeEnabled` is true by default. A dominant horizontal primary-pointer drag of at least 36 logical units on noninteractive content selects an adjacent item; drags beginning on interactive descendants are ignored. The behavior consumes host-provided primary-pointer input, without requiring a native touch classification. There is no automatic advance, wrapping or built-in slide animation. Try [images and custom interactive content](../../Samples/Nazor.Samples.Gallery/Examples/Collections/FlipViewSample.nazor) in the gallery.

## Provider-backed DataGrid positions

`DataGrid.ItemsProvider` receives a `DataRequest` with `Skip`, optional `Take`, server-side `Sorts`, application-defined `Query` and a cancellation token. Return the requested window in global filtered/sorted order as `DataPage<TItem>(items, totalCount)`. The total is the count before paging. Local `Filter` applies to in-memory sources; a provider implements its own query/filter and sorting. A nonempty result must fit both the requested Take and its global total. An empty result beyond a shrinking total is valid. Invalid windows or an overflowing requested offset set `DataView.LoadError` and clear the accepted rows.

The view copies accepted list membership and retains the producing request's offset/total together. While another request is pending, existing rows keep their accepted positions and count. Changing provider, query, sorts, page or page size cancels an outstanding request; late results and faults are ignored even if the provider does not honor cancellation. Direct DataView users call `LoadAsync()` after changing request settings; DataGrid does this during parameter updates.

Each item row's `DataRow<TItem>.ItemPosition` contains an explicit global one-based position and count. `Index` continues to address FilteredItems, which is page-local for providers. The DataGrid publishes the typed metadata on existing item elements, enabling `:nth-item(An+B)`, `:first-item` and `:last-item` across pages and virtualized scrolling. `Striped` follows the same global ordinal. Headers, details and virtualization spacers have no item position.

For example, a request with Skip 3 returning three items out of eight yields positions 4, 5 and 6, even while page 0 is being requested. The final page yields positions 7 and 8, with only 8 matching `:last-item`. Local display grouping can reorder a provider page; positions still describe the original server response rather than inventing global group membership. Provider group counts describe only members present in the accepted window.

```css
DataGridRow:nth-item(even) { background-color: #f0f0f0; }
DataGridRow:last-item { border-bottom-width: 2px; }
```

## Search with typed suggestions

`SearchField<TItem>` keeps search text in `Value` and offers suggestions from `Suggestions`, a synchronous `SuggestionProvider`, or an `AsyncSuggestionProvider`. `ItemText` controls display matching and the text copied on choice; `ItemTemplate` can show richer content without changing that text. Local matching uses case-insensitive text containment by default, or your `Filter`; providers own their filtering. `MaxSuggestions` caps every source.

```nazor
<SearchField TItem="string" Suggestions="cities" @bind-Value="searchText"
             TextEdited="value => lastEdit = value" SuggestionChosen="value => lastChoice = value"
             QuerySubmitted="Submitted" Placeholder="Find a city" EmptyText="No matching cities" />
<TextBlock>@($"Edited: {lastEdit}; chosen: {lastChoice}; query: {lastQuery}")</TextBlock>
@code {
    private readonly string[] cities = ["Milan", "Rome", "Paris"];
    private string? searchText;
    private string lastEdit = "";
    private string lastChoice = "";
    private string lastQuery = "";
    private void Submitted(SearchQuery<string> query) => lastQuery = query.Text;
}
```

`ValueChanged` includes edits, clearing and suggestion fill. `TextEdited` covers user edits/clear only; external assignments do not report edits. Clicking a suggestion fills its text and raises `SuggestionChosen`, without submitting. Unmodified Enter on a highlighted suggestion chooses it and then submits one `SearchQuery<TItem>` with `HasChosenSuggestion` true. Enter without a highlight and clicking the query icon submit free text with that flag false. `QuerySubmitted` runs before the text-only `OnSearch` callback. Modified Enter does not choose or submit. Up/Down navigation remains on the editor, and clicking options preserves editor focus unless custom content moves it explicitly.

The query icon is invokable by pointer and accessibility actions by default. Pointer submission preserves editor focus and respects focus changes made by application callbacks. Enter in the editor supplies keyboard submission without adding another Tab stop. Set `IsQueryButtonEnabled="false"` to keep the icon decorative; Enter still submits. Scopes, clear actions and `IconPlacement` work independently.

The editor's `Placeholder` is customizable for both plain and suggestion searches. An open list with no matches shows the localized **No results** label by default. `EmptyText` replaces that label; `EmptyContent` takes precedence when richer content is needed. The empty row is not a suggestion and cannot be selected. Loading and error content still take precedence over the no-results state.

Default suggestion text highlights every nonoverlapping match of the trimmed query with `Mark`. Set `HighlightMatches="false"` to render plain text. Matching ignores case in the current culture, preserves the original Unicode text and does not change filtering or the value copied on selection. Theme-owned search rows center the text vertically; `SearchFieldOption Mark` can be styled with NSS. `HighlightTemplate` replaces each matched passage with a custom inline fragment, such as `<HighlightTemplate Context="match"><Strong>@match</Strong></HighlightTemplate>`.

`ItemTemplate` continues to own its entire presentation. Within a text block or paragraph, call `search.RenderHighlightedText(value)` to reuse the same query, enable/disable setting and highlight template for any displayed field, including model properties other than `ItemText`:

```nazor
<SearchField TItem="string" @ref="search" Suggestions="cities" Placeholder="Find a city"
             EmptyText="No matching cities">
    <ItemTemplate Context="city"><TextBlock>@(search!.RenderHighlightedText(city))</TextBlock></ItemTemplate>
    <HighlightTemplate Context="match"><Strong>@match</Strong></HighlightTemplate>
</SearchField>

@code {
    private SearchField<string>? search;
    private readonly string[] cities = ["Milan", "Rome", "Paris"];
}
```

An asynchronous provider receives `(string query, CancellationToken cancellationToken)` and returns `Task<IReadOnlyList<TItem>>`. Observe cancellation and bound provider-side work where useful. The control also rejects late results and errors when cancellation is ignored. Editing, dismissal, focus leaving the field, disabling, external values/configuration, explicit submission and disposal invalidate pending work, so an old request cannot reopen a closed list. `LoadingContent` and `ErrorContent` customize pending/error states, with localized labels by default. `IsLoading` and `SuggestionError` expose state, and `RefreshSuggestionsAsync` retries the current query without reopening a dismissed popup.

For an object model, use `<SearchField TItem="Place" ... ItemText="place => place.City">` with an `ItemTemplate`. The [typed and asynchronous example](../../Samples/Nazor.Samples.Gallery/Examples/Fields/SearchSuggestionsSample.nazor) demonstrates city/country templates, filtering model data, and a cancellation-aware provider.

Migration: replace the nongeneric `SearchField` C# type with `SearchField<string>` and write `TItem="string"` on string markup. `OnSearch` now means explicit query submission; move pointer-choice behavior into `SuggestionChosen`. When choice and query callbacks perform the same action, use `QuerySubmitted.HasChosenSuggestion` to avoid doing that action twice for Enter on a highlighted item.

## Toggle a setting

Use `Switch` for an on/off setting that applies when activated. Bind `IsOn`; clicking the switch/label or pressing Space toggles it once. The control exposes the committed switch accessibility state and keeps a single keyboard focus target.

```nazor
<Switch @bind-IsOn="wifiEnabled">Wi-Fi</Switch>
@code {
    private bool wifiEnabled;
}
```

Dragging the thumb previews a visual position without changing the bound value, `On`, accessibility `checked` state or value/input/change events. After four logical units of dominant horizontal motion, release commits the nearest end once if the value changed. Escape, dominant vertical motion, lost capture, disabling or an external value change discard the preview. Lost capture clears the preview immediately, even if hover is retained. The next independent click keeps its ordinary activation behavior.

`SwitchThumbRail` and `SwitchThumbPosition` separate actual checked-state travel from the inner `SwitchThumb`'s hover/press scaling. Themes read `--nz-switch-position` (zero to one) for travel and use `dragging`/`drag-on` to preview color phases while committed semantics stay unchanged. Reduced motion applies the selected position immediately. Pointer geometry comes from the rendered parts, so controls do not hardcode theme sizes. See the [Switch example](../../Samples/Nazor.Samples.Gallery/Examples/Selection/SwitchSample.nazor) and [member contracts](../ApiReferences/Nazor.Controls.md#switch).

## Decorative group hover highlights

`HoverTracker` in `Nazor.Controls.Primitives` supplies scoped hover geometry for the existing `Indicator` primitive. Add its `Attributes(receiver, resolveHost, match)` to a group, render an `Indicator` named `HoverIndicator` directly inside that same group and bind its `Target` to the tracker. The retained mark keeps the last geometry for exit fading, snaps initial entry and distinguishes movement to another item. Nested trackers own their own items. Disabled, hidden and removed rows are excluded. Call `Refresh()` after custom content changes; `Clear()` ends the entry without moving focus or consuming activation. Item geometry uses axis-aligned bounds.

Menus, menu bars, settings row groups, ListView and DataGrid compose the tracker in their actual content viewport; the gallery's resource tiles use the same primitive. NazorLeaf draws 320ms position/size movement and 200ms opacity; Basic and Fluent keep the mark hidden. Reduced motion applies changes immediately, and forced colors suppress the decorative mark. Static `Table` does not compose the tracker automatically; its cell/row layout remains unchanged. Opaque frozen DataGrid cells retain an opaque, selection-compatible hover fill while the shared mark moves under ordinary cells.

For custom virtualized content, `Virtualizer<TItem>.OverlayContent` renders before spacers and item wrappers, outside item realization. Use absolute positioning for a decorative overlay and mark it `semantic-hidden`; the slot itself does not impose semantics or positioning. It remains mounted through scrolling, variable-height measurements and empty items, and is excluded from item counts and measurement. Size the scrolling root so it clips the overlay with its items. `ScrollEventArgs.OriginalTarget` identifies the actual scroller even when its event bubbles or is forwarded; it is null on unrouted arguments.

## Choose a color

`ColorPicker` composes the reusable HSV area/sliders with RGB or HSV channel text, CSS hexadecimal, optional alpha and current/previous checkerboard previews:

```nazor
<ColorPicker @bind-Color="paint" IsAlphaEnabled="true" PreviousColor="previous" />
@code {
    private Nazor.Style.Color paint = new(51, 102, 153, 160);
    private readonly Nazor.Style.Color previous = new(217, 72, 102, 255);
}
```

The default rectangle edits hue horizontally and saturation vertically, showing full brightness while preserving actual brightness for the remaining-channel slider. `SpectrumComponents` selects any of the six rectangular permutations. Arrows follow screen direction, Shift increases the step, Page Up/Down move vertically and Home/End select horizontal endpoints (vertical with Ctrl). Hidden or disabled controls are outside the Tab order. `ReadOnly` leaves text selectable and permits changing the RGB/HSV representation without changing the color.

`ColorChanged` reports changed bytes, including drag previews; `OnChange` reports completion. Hue is retained when saturation becomes zero, and hue/saturation remain available when brightness becomes zero. Numeric drafts follow `Culture`, clamp finite values and commit with Enter or focus loss. Invalid drafts keep the color, remain marked on Enter and return to the canonical value when focus leaves. Hex is culture independent and always uses CSS byte order: #rgb, #rgba, #rrggbb or #rrggbbaa, with optional #. In particular #11223344 means red 17, green 34, blue 51, alpha 68 in every theme. Disabling alpha editing preserves the current alpha byte.

Use the independent spectrum/slider/preview/channel/hex visibility properties to choose a compact presentation. `Orientation.Horizontal` places visuals beside text and makes its sliders vertical. `PreviousColor` supplies a noninteractive comparison, not an undo action. The [gallery example](../../Samples/Nazor.Samples.Gallery/Examples/Pickers/ColorPickerSample.nazor) exposes these settings. The existing ColorArea/ColorSlider primitives remain available for custom controls; no native color-dialog or circular-spectrum API is implied.
