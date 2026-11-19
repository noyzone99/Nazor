# 17. Selectors and media queries

## Visual selectors

`:popup-above`, `:popup-left` and `:popup-right` match an anchored overlay whose final arranged border box is above, to the left or to the right of its anchor. It reflects viewport/native placement rather than the requested `placement` attribute. Presentation publishes the state before the first visible frame, with one bounded style/layout refresh when necessary. Use it to select a reveal edge without changing the authored placement or restarting an animation under a new name.

`:popup-origin-right` and `:popup-origin-bottom` combine to select four reveal corners from the surface edges nearest the anchor edges, including native-configured bounds and pointer-menu clamping. Equal distances follow end alignment. These factual states style motion without changing placement; they do not replace the side predicates.

| Selector | Example and meaning |
| --- | --- |
| Type | `Button`; case-sensitive element name |
| Universal | `*`; no specificity |
| Class | `.card.selected`; matches whitespace-separated `Class` values |
| ID | `#status`; matches `Id` |
| Attribute | `[disabled]`, `[data-state=ready]`, `[role^="tab" i]` |
| Descendant | `.page Button` |
| Child | `.toolbar > Button` |
| Adjacent sibling | `.label + TextField` |
| Following sibling | `.heading ~ .note` |
| List | `Button, TextField`; each selector retains its own specificity |

Attribute operators are `=`, `~=`, `|=`, `^=`, `$=`, and `*=`. Attribute names compare without case sensitivity; values can use the `i` flag. Null and false values count as absent. Supply custom attributes through [captured-attribute dictionaries](06-Parameters-Events-and-Binding.md#captured-attributes).

In a compound selector, the type, `*`, or `&` comes first, followed by class, ID, attribute, and pseudo-class selectors. Siblings are physical elements sharing a parent. Custom components can contribute several siblings without a wrapper.

## State and structural pseudo-classes

Supported state selectors are `:hover`, `:active`, `:focus`, `:focus-visible`, `:focus-within`, `:disabled`, `:checked`, `:wheel-scrolling`, `:window-active`, and `:window-inactive`. Use `:root:window-active` and `:root:window-inactive` for window activation. On Wayland these follow the compositor's decoration state.

An `@window` root also has `window-state` (`normal`, `maximized`, `minimized`, or `fullscreen`) and `frame-owner` (`none`, `nazor`, or `system`) attributes. Use selectors such as `:root[window-state="maximized"]` and `:root[frame-owner="nazor"]` to match the effective backend state and frame ownership.

Hover includes ancestors. Focus-visible corresponds to keyboard, programmatic, or accessibility focus. Wheel-scrolling matches the container receiving wheel input for its configured idle timeout. See [scrolling](20-Layout-and-Scrolling.md#scrolling). Match a control's checked state on the control and style the visual part with a child selector.

Control-state pseudo-classes use explicit retained metadata:

| Selector | Applicability and meaning |
| --- | --- |
| `:enabled` | Interactive roles such as button, link, checkbox, radio, switch, editor, list/option, tree/item, tab and menu item, or an element explicitly publishing a boolean `disabled` attribute, while neither it nor an ancestor disables interaction. Ordinary decorative elements do not match. |
| `:indeterminate` | Check/toggle/menu/tree controls publishing `indeterminate: true`, or a progress bar with an unknown value. This does not infer radio-group state from arbitrary siblings. |
| `:read-write` | An enabled shared text editor, identified by [text editor roles](../ApiReferences/Nazor.Core.md#texteditorroles), without `readonly: true`. |
| `:read-only` | The complement of `:read-write`, including noneditor elements. |
| `:placeholder-shown` | An editor with an empty `value` and nonempty `placeholder-text`, while no IME preedit is displayed. Attribute presence alone is insufficient. |
| `:valid`, `:invalid` | A value control or `Field`, based on the application-owned `invalid` flag on the element or its nearest enclosing `Field`. |
| `:required`, `:optional` | The same value controls and fields, based on their own or nearest field's `required` flag. |

Value controls include text/search/number editors, checkboxes, switches, radio groups/radios, combo boxes, sliders, listboxes, trees and toggles. A custom value control can publish `validation-control: true` through captured attributes. Required/invalid metadata follows the existing field and accessibility contract: disabled/read-only controls retain that application-owned state. `:valid` does not perform validation or assume that an empty required value is invalid. The application supplies the result. Use state combinations such as `:invalid:disabled` when needed.

```css
TextFieldInput:read-write { border-color: FieldText; }
TextFieldInput:placeholder-shown { font-style: italic; }
Field:invalid { border-color: red; }
CheckBox:indeterminate > CheckBoxMark { color: Highlight; }
```

These selectors add ordinary pseudo-class specificity. Role, value, readonly, required, invalid, disabled and field updates refresh matching; IME start/cancellation/commit also refreshes `:placeholder-shown`. Custom editors use the same role/value/placeholder metadata and shared input implementation; selectors do not inspect arbitrary CLR properties.

```css
CheckBox:checked > CheckBoxMark { color: white; }
```

Structural selectors are `:first-child`, `:last-child`, `:only-child`, the corresponding `-of-type` forms, `:nth-child()`, `:nth-last-child()`, `:nth-of-type()`, `:nth-last-of-type()`, `:empty`, and `:root`. Index expressions support integers, odd/even, and `An+B`. The child and last-child functions also accept an `of <selector-list>` filter.

`:is()`, `:not()`, and `:where()` accept selector lists, including combinators. `:is()`/`:not()` take the most specific argument; `:where()` adds no specificity. `&`, `::deep`, and pseudo-elements are not supported inside these logical functions.

Use `:root` for the current window in inline, scoped, and global sheets. Combine it with state or attribute selectors such as `:root:hover` and `:root[backdrop]`. An `@window` component places its authored content inside a `WindowContent` element, so use a descendant selector such as `:root .page` to reach a `<Container Class="page">` in the window. The C# `Window` API configures the window. A `Window` type selector reports `NZS109`. `.Window` and `#Window` are valid class and ID selectors.

`:root` has pseudo-class specificity `(0,1,0)`, so it ties with a single class rule and source order decides; IDs still take precedence. To deliberately use zero specificity, write `:where(:root)`.

`:root` keeps its structural meaning for isolated component rendering. Built-in themes use `:root[role=window]` for window-only surface/layout rules; the application host already supplies that role on window roots. General root tokens remain available to isolated controls. Editor window references require an actual `@window` declaration.

Specificity compares `(IDs, classes/attributes/pseudo-classes, types/pseudo-elements)` lexicographically, after nesting expansion. Cascade level is considered first.

## Filtered sibling positions

Use `:nth-child(An+B of S)` or `:nth-last-child(An+B of S)` to count only physical element siblings matching at least one selector in `S`. The element being selected must itself match that filter. Counts are one-based; the last-child form counts backwards. The filter contributes its greatest selector specificity in addition to the pseudo-class's `(0,1,0)`, even when a less-specific branch matches. `:where()` can remove filter specificity.

```css
.row:nth-child(even of .row:not([hidden])) { background-color: #eeeeee; }
.row:nth-last-child(1 of .row.selected, .row:focus) { border-bottom: 2px solid Highlight; }
```

Filters accept ordinary selector lists and combinators, including logical and control-state pseudo-classes. `&`, `::deep`, generated pseudo-elements, empty list branches and leading combinators are rejected inside a filter. The `-of-type` functions do not accept `of S`.

In scoped styles, `S` follows ordinary component ownership; use an explicit component qualifier inside `S` when selecting private declarations. An outer `::deep` before the filtered subject removes that boundary for the filter too. Attribute/state changes affecting filter membership and inserted/removed/moved siblings update all affected positions. Changes to text observed by `:empty` also refresh membership.

These are rendered sibling positions, including spacer or hidden elements if `S` includes them. They are not indices in application data after sorting/filtering, and cannot determine logical positions in a virtualized collection.

## Logical collection positions and component roots

`:nth-item(An+B)`, `:first-item` and `:last-item` consume a typed `Nazor.Rendering.LogicalItemPosition` from the `item-position` attribute. Unlike `:nth-child`, they do not count realized siblings, spacers or arbitrary `index` attributes. The control supplies an explicit sequence identity, a one-based position after filtering/sorting, and the complete known count. The style engine never enumerates application data. No metadata, an invalid default value, or an unknown count matches none of these selectors. `:nth-item` accepts integers, odd/even and An+B; `of S` is unsupported. Each adds `(0,1,0)` specificity.

`Virtualizer` item wrappers publish their index in the complete supplied sequence, excluding spacers. `ListViewItem` positions restart in each group and exclude headers. `GridViewItem` uses the full supplied item list; `TreeViewItem` uses its sibling sequence under the same logical parent, independent of expanded descendants. `DataGridRow` uses global filtered/sorted item positions across local and provider pages; headers and details are excluded. Provider rows carry the accepted request's offset and returned total, so a pending page/query/sort change does not relabel older visible rows. Local grouping retains each item's original provider-response position, including duplicates. See [provider result contracts](10-Text-Input-and-Data-Controls.md#provider-backed-datagrid-positions). Internal virtualizer wrappers describe their own supplied sequence, which can include group templates; target the documented item element for collection-specific meaning.

Counts describe the current supplied sequence, including currently loaded incremental items; they do not assert completion of a remote source.

Custom controls can set `Control.ItemPosition` or publish the typed attribute. A sequence can use a stable owner/group key; nested collections publish their own sequence identities. Publish updated metadata after sorting/filtering and every reassignment of a recycled node. The count must be known and position must be in 1..count; the constructor rejects other values. The metadata does not inherit and changes refresh ordinary and relational matching. Use `:nth-item(even)` for alternating rows or `:last-item` for a final-item edge without depending on the visible window. Existing attributes such as DataGrid's `alternate` still describe their own control options.

`:component-root` selects physical roots declared by the current scoped component instance, including multiple outputs of different types. It creates no host box, and matches nothing in global/inline sheets. Nested authored elements, child-component internals and projected elements behind a physical container are excluded; empty output has no root. The component's declaration/instance ownership is used, rather than merely finding an element of the same type. A wrapperless child does not itself create a root element. Child-list/reparenting updates refresh matching. Use this for an owner's root appearance across different root element types; component qualifiers still select explicitly entered declarations, and `:root` retains window/tree-root semantics.

## Validation feedback

`:user-valid` and `:user-invalid` combine the same applicability and application-owned validity as `:valid`/`:invalid` with feedback eligibility. Initial invalidity alone matches neither user selector. Eligibility begins after an `oninput`/`onchange` event or a completed focus cycle (`onblur`) on a value control, or an attempt to submit the nearest `Form`. This includes semantic input actions such as accessibility text replacement. Changing bound values or validity through ordinary parameters does not itself enable feedback. A field shares interaction eligibility with its controls; a nested form uses its own submission state.

`Form.SubmitAsync()` and its button/Enter routes enable feedback before invoking `OnSubmit`; they do not validate, reject submission, or change values. Eligibility persists through subsequent edits, so feedback can switch from user-invalid to user-valid. `Form.ResetAsync()` clears eligibility before `OnReset`, including controls and fields but excluding nested forms. The application still resets its model and validity. A custom control can call `ValidationFeedback.MarkInteracted(element)` after its interaction and `ValidationFeedback.Reset(element)` when resetting or reassigning a recycled physical target. Disabled controls do not acquire eligibility through input; already published validity/eligibility is retained when disabled or readonly. Focus loss caused by programmatic focus movement also completes a focus cycle.

```css
TextFieldInput:user-invalid { border-color: red; }
Field:has(:user-invalid) { background: #fff0f0; }
```

## Custom states

Use `:state(loading)` for a component's explicit boolean state contract. `Control.CustomStates` accepts an immutable `Nazor.Rendering.CustomStateSet`; custom renderers publish the same object through the `custom-states` element attribute. For example, `CustomStates="@states"` uses `private CustomStateSet states = CustomStateSet.Empty.With("loading");` and `.card:state(loading)` selects that root. Replace the snapshot and request a render to publish a change: `states = states.With("loading", false); StateHasChanged();`.

Names are case-sensitive simple identifiers: a letter or underscore, followed by letters, digits, hyphens or underscores. `:state()` requires one unquoted identifier and adds `(0,1,0)` specificity. Names are deduplicated; invalid names throw. State is not inherited; components with multiple outputs explicitly publish it on each chosen physical target. Logical components without a box do not implicitly acquire a target. Equal snapshots do not produce attribute changes. Replacing/removing the declaration clears obsolete states, including when recycled nodes represent another item; authors must supply the new item's snapshot on every render.

Custom states separate a stable boolean styling contract from an application's CSS classes and arbitrary attribute values. Existing class/attribute selectors remain useful for those purposes. No CLR getters or reflection are involved, and snapshots need no event subscriptions. A custom `selected` or `expanded` state does not change semantics: publish the corresponding role/checked/expanded accessibility attributes and input behavior from the same application state. States work in logical functions, filtered sibling positions and relational conditions.

## Relational selectors

`:has()` tests relative selector lists anchored to the originating physical element. `.field:has(> TextFieldInput:invalid)` matches a field containing an invalid direct child; `.section:has(+ .section)` matches a section followed by another section. An omitted leading combinator means a descendant. `>`, `+`, and `~` select children, the next sibling, and any following sibling. Complex relative selectors and comma alternatives are supported. The origin itself is not its own descendant.

Specificity is the greatest argument specificity, without an extra pseudo-class unit. `:where()` inside an argument still contributes zero. Nested `:has()`, pseudo-elements, `&`, and `::deep` inside arguments are rejected. Scoped arguments retain their owner's boundary; explicit logical component qualifiers can enter a child declaration, and an outer `::deep` removes the boundary for the crossed segment. Logical component qualifiers have no box on which to evaluate `:has()`.

Relevant attribute/state/text/child-list changes refresh ancestor and preceding-sibling origins and their dependents. Prefer a specific origin and a direct relationship, such as `.card:has(> .busy)`, on large or embedded trees. Descendant matching can inspect an entire subtree; nested complex conditions and sibling positions require broader invalidation. Dirty style updates still traverse the retained tree, even when only a small branch needs recomputation. An idle `Update()` does no tree traversal. Editor references mark runtime-only conditions as partial rather than claiming statically proven matches.

## Pseudo-elements

`::before`, `::after`, `::placeholder`, and `::selection` select a box or text portion relative to an originating element. Put the pseudo-element last, after any state selectors: `.notice:hover::before`. Each adds one type specificity unit. A rule for a pseudo-element never styles its originating element. Component scope, `::deep`, media queries, custom properties, source order and cascade levels apply normally. Use `&::before` inside an element rule; rules cannot be nested inside a pseudo-element rule.

`::before` and `::after` generate decorative first and last child boxes when `content` contains quoted strings. Adjacent strings concatenate; `content: ""` creates an empty decorative box. `normal`, `none`, and the initial value suppress generation. Boxes inherit text styles and custom properties from the origin and use the usual block, flex, grid and positioned layout. Nazor does not implement CSS inline formatting. Editable fields and replaced image elements do not generate these boxes. Native window properties do not apply to generated boxes.

```css
.notice { display: flex; gap: 8px; }
.notice::before { content: ""; width: 4px; background-color: currentColor; }
.notice::after { content: " " "(optional)"; font-style: italic; }
```

Generated boxes are outside authored children: they do not affect `:empty`, child/sibling indices, component references, values or copied text. Pointer hits target their originating element. They are decorative and excluded from accessibility semantics; provide essential labels and instructions through actual content and accessible names. Content images, `attr()`, counters, quotes and content replacement on ordinary elements are unsupported.

`::placeholder` applies to the editable element carrying `placeholder-text`, including `EditableText` and the input parts of text, password, number and composite fields. It supports font/text layout properties, `color`, text decoration and `opacity`; box/background properties are ignored. It appears only while the value and IME composition are empty. Target the documented input part when a control wraps its editor.

```css
TextFieldInput::placeholder { color: GrayText; font-style: italic; opacity: 0.8; }
TextFieldInput::selection { color: HighlightText; background-color: Highlight; }
```

`::selection` supports `color` and `background-color` on selected editable text, including password masks, wrapped lines and aligned text. It preserves editing, copied values and caret styling. With no declaration it uses the field's text color and the standard active/inactive highlight. An explicitly transparent background disables the highlight fill. This first implementation inherits from the originating element; it does not implement CSS highlight inheritance from ancestor selections or selection in noneditable text. `::first-line`, `::first-letter`, `::marker`, and `::backdrop` remain unsupported.

## Public component parts

`TextField::part(input):focus` styles the existing editable element exposed by a text field. `TextField`, `TextArea` and other `TextFieldBase` controls expose the stable `input` name. This creates no decorative box and does not require knowing the private editor element name. Existing theme tokens continue to inherit and participate in the normal cascade.

Custom components publish `Nazor.Style.StylePartSet` through `Control.Parts` or the `style-parts` physical element attribute. A component creates a declaration with `StylePartSet.Empty.Expose(this, "action")` and publishes it on the chosen element/control. Names are case-sensitive simple identifiers with the same form as custom state names. One name may select multiple outputs; a component producing no elements exposes no target. Instance ownership is checked against both the target declaration/producer and the logical ancestor chain. Projected content keeps its producing/declaring ownership rather than becoming the receiver's private part.

Parts of nested components remain private unless forwarded explicitly. A wrapper can pass `StylePartSet.Empty.Expose(this, "query")` to a text field's `InputParts`; the field forwards its known `input` part, making it available as `Wrapper::part(query)`. Custom authors accept a `StylePartSet` parameter and call `localParts.Export(this, "local-name", requestedParts)` on an already exposed local binding. Each boundary chooses whether and which part to export; no name or hierarchy is inferred. Replacing/removing declarations refreshes styling and old bindings disappear with removed nodes.

The supported form is a simple component name followed by one `::part(name)`, optionally followed by interaction/control pseudo-classes or `:state(name)`. States refer to the target element. Host classes/attributes/states before `::part`, structural/logical suffixes, chained parts, generated pseudo-elements and descendants after a part are rejected. Parts are also rejected inside logical, relational and position-filter arguments. A component type plus a part adds `(0,0,2)` specificity; each trailing state adds its usual specificity.

Preceding combinators, for example `.form TextField::part(input)`, describe the physical target's ancestry/siblings; they do not create a logical host box. In a scoped sheet, the exposing component must be declared by that scope, or be the owner itself. Private nested component parts require export; an outer `::deep` deliberately removes that boundary for the crossed segment. Ordinary component qualifiers, public parts, theme tokens and deep selectors are complementary contracts. Use tokens for broad theming and documented parts for targeted appearance.

## Component boundaries

Scoped rules normally stop at child-component markup. A logical component qualifier selects elements declared by matching custom component instances:

```css
Clock TextBlock { color: #444; }
Clock Details TextBlock { font-weight: bold; }
Clock ::deep TextBlock { background: #eee; }
.page ::deep .heading { color: #6941c6; }
```

`Clock TextBlock` reaches text declared by Clock, stopping at custom components it composes. `Clock Details TextBlock` explicitly enters Details. A qualifier creates no visual box; `Clock { ... }` alone cannot style a box that does not exist. Qualifiers use simple case-sensitive component names without generic arity; components sharing a simple name share the qualifier.

`::deep` crosses descendant component boundaries. In a scoped sheet its anchor must belong to the sheet owner: `.page ::deep .heading` only crosses below the owner's `.page`. A bare `::deep .heading` needs an owner-declared visual ancestor. A custom qualifier can be an explicit anchor. Allow one `::deep` per expanded selector, followed by a descendant selector. Global sheets have no owner-scope restriction.

Fragment ownership remains lexical: markup passed into a child still belongs to the caller. Descendant qualification does not change that ownership. Prefer a documented token or component parameter when it expresses the customization without depending on private markup.

Selector scope determines which elements a rule can match. A declaration on a matched element still depends on the property's target and runtime conditions. For example, a global rule can reach every window root, and `-window-resize-grip` shows a grip only on a resizable desktop window in its normal state. See [property applicability](19-NSS-Property-Reference.md#scope-and-applicability).

## Nesting

```css
.card {
    padding: 12px;
    TextBlock { color: #333; }
    > Button { margin-top: 8px; }
    &.selected { border: 1px solid #6941c6; }
    &:hover { background: #f5f5f5; }
    .dark & { color: white; }
}
```

Unqualified nested selectors become descendants. `&` substitutes the complete parent selector and can appear after another selector. Nested lists expand as a Cartesian product. Declarations in a block form a rule before its nested rules, regardless of their textual position. Top-level `&` or an initial top-level combinator is invalid.

## Media queries

```css
.page { display: grid; grid-template-columns: 1fr 1fr; gap: 24px; }
@media (max-width: 720px) {
    .page { grid-template-columns: 1fr; gap: 12px; }
}
@media (prefers-color-scheme: dark) {
    :root { --page-surface: #202020; }
}
@media (forced-colors: active) {
    .notice { background: Canvas; color: CanvasText; border: 1px solid Highlight; }
}
```

| Feature | Supported values |
| --- | --- |
| `min-width`, `max-width`, `min-height`, `max-height` | Inclusive logical client dimensions in px/em/rem or supported length expressions; no percentages/viewport units |
| `prefers-color-scheme` | `light`, `dark`; effective application scheme |
| `prefers-reduced-motion` | `reduce`, `no-preference`; bare feature means reduce |
| `forced-colors` | `active`, `none`; bare feature means active |
| `prefers-contrast` | `more`, `no-preference`, `less`; Windows does not report less |
| `platform` | `windows`, `linux`, `macos`, `android`, `ios`; compares the scene's backend identity |

Use `and` for conjunction and commas for alternatives. Nested queries combine with AND. Conditions can appear at the top level or inside a rule. In media dimensions, em/rem use the initial 14px font, since there is no element context.

Platform queries use the scene's backend identity. Win32 matches `windows`. Wayland, X11, and DRM match `linux`. A headless scene with no backend identity matches no platform. The [support guide](32-Support-and-Limitations.md) lists available backends. Media types (`screen`, `print`), `only`/`not`, range syntax, orientation/resolution queries, and window-manager queries are unsupported.

NSS supports only `@media` and top-level `@keyframes` at-rules. See [animation](23-Transitions-and-Animations.md) for keyframe placement constraints.

---

[Guide index](README.md) · [API reference](../API.md)
