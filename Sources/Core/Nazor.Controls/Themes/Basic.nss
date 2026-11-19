/*
 * Basic: the default theme. System colors, one-pixel borders and the essential geometry that tells a button from
 * text or a drop-down. No decoration, transitions or animations. Every control part has at least one rule here, so
 * any other theme can rely on Basic for the parts it does not style.
 */

/*
 * Theme tokens. Every rule below reads its colors from these custom properties, so an application can retune Basic
 * (for example :root { --nz-accent: #c42b1c; }) and future themes can share the same names.
 */
:root {
    font-optical-sizing: native;
    --nz-surface: Canvas;
    --nz-on-surface: CanvasText;
    --nz-control: ButtonFace;
    --nz-on-control: ButtonText;
    --nz-border: ButtonBorder;
    --nz-field: Field;
    --nz-on-field: FieldText;
    --nz-accent: AccentColor;
    --nz-on-accent: AccentColorText;
    --nz-selection: Highlight;
    --nz-on-selection: HighlightText;
    --nz-muted: GrayText;
    --nz-link: LinkText;
    --nz-danger: #b00020;
    --nz-success: #1e7b34;
    --nz-warning: #8a5a00;
    --nz-scrim: rgba(0, 0, 0, 0.35);
    color: var(--nz-on-surface);
    background: var(--nz-surface);
}
@media (forced-colors: active) {
    :root { --nz-danger: CanvasText; --nz-success: CanvasText; --nz-warning: CanvasText; --nz-accent: Highlight; --nz-on-accent: HighlightText; --nz-scrim: transparent; }
}
:root[role=window] { height: 100%; }
:root[role=window][backdrop] { background: transparent; }
:root[role=window][transparent-background] { background: transparent; }
:focus-visible { outline: 1px solid CanvasText; outline-offset: 2px; }
:disabled { color: GrayText; }

/* Engine surfaces: window caption, built-in tooltip and text menu, message boxes. */
TitleBar { display: flex; align-items: center; gap: 4px; min-height: var(--nz-window-controls-height, 0px); }
Tooltip { padding: 2px 6px; border: 1px solid var(--nz-on-surface); background: var(--nz-surface); color: var(--nz-on-surface); max-width: 320px; }
ContextMenu { display: flex; flex-direction: column; min-width: 160px; padding: 2px; border: 1px solid var(--nz-on-surface); background: var(--nz-surface); color: var(--nz-on-surface); }
ContextMenu > MenuItem { padding: 3px 8px; }
ContextMenu > MenuItem:hover, ContextMenu > MenuItem:focus { background: var(--nz-selection); color: var(--nz-on-selection); }
MessageBoxBody { display: flex; gap: 12px; padding: 12px; }
MessageBoxButtons { display: flex; justify-content: flex-end; gap: 8px; padding: 0 12px 12px; }

/* ---- Text ---- */
Heading { display: block; font-weight: 700; }
Heading[level="1"] { font-size: 2em; }
Heading[level="2"] { font-size: 1.5em; }
Heading[level="3"] { font-size: 1.17em; }
Heading[level="4"] { font-size: 1em; }
Heading[level="5"] { font-size: 0.83em; }
Heading[level="6"] { font-size: 0.67em; }
ShortcutLabel { display: flex; align-items: center; gap: 1px; color: var(--nz-muted); white-space: pre; }
ShortcutLabelKey { padding: 0 2px; }

/* ---- Media ---- */
Icon { font-family: "Segoe Fluent Icons", "Segoe MDL2 Assets"; width: 1em; height: 1em; line-height: 1; flex-shrink: 0; }
Icon:root { line-height: 1; }

/* ---- Layout ---- */
Separator { display: flex; align-items: center; align-self: stretch; flex-shrink: 0; min-width: 1em; height: 1px; background: var(--nz-muted); }
Separator[vertical] { min-width: 0; width: 1px; height: auto; min-height: 1em; }
Separator > SeparatorLabel { padding: 0 6px; background: var(--nz-surface); color: var(--nz-muted); margin: 0 auto; }

/* ---- Actions ---- */
Button, ToggleButton { display: flex; align-items: center; justify-content: center; gap: 6px; min-height: 26px; padding: 2px 10px; border: 1px solid var(--nz-border); background: var(--nz-control); color: var(--nz-on-control); cursor: default; }
Button:hover, ToggleButton:hover { border-color: var(--nz-on-surface); }
Button:active, ToggleButton:active, ToggleButton[pressed] { background: var(--nz-selection); color: var(--nz-on-selection); }
Button[default], Button[appearance="accent"], ToggleButton[appearance="accent"] { background: var(--nz-accent); color: var(--nz-on-accent); border-color: var(--nz-accent); }
Button[appearance="tonal"], ToggleButton[appearance="tonal"] { background: var(--nz-control); border-color: var(--nz-accent); }
Button[appearance="outline"], ToggleButton[appearance="outline"] { background: transparent; }
Button[appearance="subtle"], ToggleButton[appearance="subtle"] { background: transparent; border-color: transparent; }
Button[appearance="subtle"]:hover, ToggleButton[appearance="subtle"]:hover { border-color: var(--nz-border); }
Button[appearance="link"], ToggleButton[appearance="link"] { background: transparent; border-color: transparent; color: var(--nz-link); text-decoration-line: underline; padding: 0; min-height: 0; }
Button[intent="danger"] { color: var(--nz-danger); }
Button[intent="danger"][appearance="accent"] { background: var(--nz-danger); border-color: var(--nz-danger); color: #ffffff; }
Button[intent="success"][appearance="accent"] { background: var(--nz-success); border-color: var(--nz-success); color: #ffffff; }
Button[intent="warning"][appearance="accent"] { background: var(--nz-warning); border-color: var(--nz-warning); color: #ffffff; }
Button[size="small"], ToggleButton[size="small"] { min-height: 20px; padding: 0 6px; font-size: 12px; }
Button[size="large"], ToggleButton[size="large"] { min-height: 36px; padding: 4px 16px; font-size: 16px; }
Button[shape="pill"], ToggleButton[shape="pill"] { border-radius: 999px; }
Button[shape="circle"], ToggleButton[shape="circle"] { border-radius: 50%; padding: 0; width: 26px; }
Button[shape="square"], ToggleButton[shape="square"] { padding: 0; width: 26px; }
Button[icon-only], ToggleButton[icon-only] { padding: 2px 4px; }
Button:disabled, ToggleButton:disabled { color: var(--nz-muted); border-color: var(--nz-muted); background: var(--nz-control); }
Button[busy], ToggleButton[busy] { color: var(--nz-muted); }
ButtonLeading, ButtonTrailing { display: flex; align-items: center; flex-shrink: 0; }
ButtonLabel { pointer-events: none; }
ButtonSpinner { width: 1em; height: 1em; flex-shrink: 0; stroke-width: 2px; }

/* ---- Selection ---- */
CheckBox { display: grid; grid-template-columns: max-content minmax(0px, 1fr); column-gap: 6px; align-items: center; }
CheckBoxMark { display: grid; grid-template-columns: 1fr; grid-template-rows: 1fr; width: 13px; height: 13px; border: 1px solid var(--nz-border); background: var(--nz-field); color: var(--nz-on-field); stroke-width: 2px; }
CheckBoxCheck, CheckBoxMixed { grid-column-start: 1; grid-row-start: 1; width: 100%; height: 100%; stroke-width: inherit; stroke-trim-end: 0; }
CheckBox:checked > CheckBoxMark > CheckBoxCheck, CheckBox[indeterminate] > CheckBoxMark > CheckBoxMixed { stroke-trim-end: 1; }
CheckBox:checked > CheckBoxMark, CheckBox[indeterminate] > CheckBoxMark { background: var(--nz-accent); color: var(--nz-on-accent); border-color: var(--nz-accent); }
CheckBox:disabled > CheckBoxMark { border-color: var(--nz-muted); background: var(--nz-surface); color: var(--nz-muted); }
CheckBoxDescription { grid-column-start: 2; color: var(--nz-muted); font-size: 12px; }
CheckBox[size="small"] > CheckBoxMark { width: 11px; height: 11px; }
CheckBox[size="large"] > CheckBoxMark { width: 17px; height: 17px; }

/* ---- Fields ---- */
Form { display: block; }
FormLayout { display: flex; flex-direction: column; gap: 10px; }
Field { display: flex; flex-direction: column; gap: 3px; }
Field[layout="inline"] { display: grid; grid-template-columns: var(--nz-form-label-width, 30%) minmax(0px, 1fr); column-gap: 10px; row-gap: 3px; align-items: center; }
Field[layout="inline"] > Label { text-align: right; }
Field[layout="inline"] > FieldDescription, Field[layout="inline"] > FieldError { grid-column-start: 2; }
FieldControl { display: flex; flex-direction: column; min-width: 0; }
FieldDescription { color: var(--nz-muted); font-size: 12px; }
FieldError { color: var(--nz-danger); font-size: 12px; }
Label { display: block; }
LabelRequiredMark { padding-left: 2px; color: var(--nz-danger); }
/* Without inline layout, a required label lays its text and mark out in a row so the mark stays after the text. */
Label:has(LabelRequiredMark) { display: flex; align-items: flex-start; }
Field[layout="inline"] > Label:has(LabelRequiredMark) { justify-content: flex-end; }
EditableText { display: block; min-width: 8em; min-height: 1.4em; white-space: pre; overflow-x: hidden; cursor: text; }
EditableText[multiline] { white-space: pre-wrap; overflow-y: auto; }
TextField, TextArea, Select { display: flex; align-items: center; gap: 4px; min-width: 10em; min-height: 26px; padding: 1px 4px; border: 1px solid var(--nz-border); background: var(--nz-field); color: var(--nz-on-field); }
TextField:focus-within, TextArea:focus-within, Select:focus-visible, Select[expanded] { border-color: var(--nz-accent); }
TextField[invalid], TextArea[invalid], Select[invalid] { border-color: var(--nz-danger); }
TextField:disabled, TextArea:disabled, Select:disabled { border-color: var(--nz-muted); color: var(--nz-muted); background: var(--nz-surface); }
TextField[appearance="filled"], TextArea[appearance="filled"], Select[appearance="filled"] { background: var(--nz-control); border-color: transparent; }
TextField[appearance="underline"], TextArea[appearance="underline"], Select[appearance="underline"] { border-color: transparent; border-bottom-color: var(--nz-border); background: transparent; }
TextField[appearance="plain"], TextArea[appearance="plain"], Select[appearance="plain"] { border-color: transparent; background: transparent; padding: 0; }
TextField[size="small"], Select[size="small"] { min-height: 20px; font-size: 12px; }
TextField[size="large"], Select[size="large"] { min-height: 34px; font-size: 16px; }
TextFieldInput, TextAreaInput { display: block; flex: 1; min-width: 0; white-space: pre; overflow-x: hidden; cursor: text; }
TextFieldLeading, TextFieldTrailing, TextAreaLeading, TextAreaTrailing, SelectLeading { display: flex; align-items: center; flex-shrink: 0; }
TextFieldPrefix, TextFieldSuffix, TextFieldCounter, TextAreaCounter { flex-shrink: 0; color: var(--nz-muted); }
TextFieldClear { width: 10px; height: 10px; flex-shrink: 0; stroke-width: 1.5px; color: var(--nz-muted); }
TextFieldClear:hover { color: var(--nz-on-field); }
TextArea { flex-direction: column; align-items: stretch; min-height: calc(var(--nz-textarea-rows, 3) * 1.4em + 4px); }
TextAreaInput { white-space: pre-wrap; overflow-y: auto; }
TextAreaCounter { align-self: flex-end; font-size: 12px; }
TextArea > ResizeHandle { align-self: stretch; height: 4px; }
Select { cursor: default; }
SelectValue { flex: 1; min-width: 0; overflow-x: hidden; white-space: pre; text-overflow: ellipsis; }
SelectValue[placeholder] { color: var(--nz-muted); }
SelectChevron { width: 10px; height: 10px; flex-shrink: 0; stroke-width: 1.5px; }
SelectPopup { display: flex; flex-direction: column; overflow-y: auto; padding: 2px; border: 1px solid var(--nz-on-surface); background: var(--nz-surface); color: var(--nz-on-surface); }
SelectGroupHeader { padding: 4px 6px 2px; color: var(--nz-muted); font-size: 12px; font-weight: 700; }
SelectOption { display: flex; align-items: center; gap: 6px; padding: 3px 6px; }
SelectOption:hover, SelectOption:focus { background: var(--nz-selection); color: var(--nz-on-selection); }
SelectOption:disabled { color: var(--nz-muted); background: transparent; }
SelectOptionCheck { width: 10px; height: 10px; flex-shrink: 0; stroke-width: 2px; }

/* ---- Menus ---- */
Menu { display: flex; flex-direction: column; min-width: 160px; overflow-y: auto; padding: 2px; border: 1px solid var(--nz-on-surface); background: var(--nz-surface); color: var(--nz-on-surface); }
MenuItem, MenuCheckItem, MenuRadioItem, Submenu { display: grid; grid-template-columns: 18px minmax(0px, 1fr) max-content max-content; column-gap: 6px; align-items: center; padding: 3px 6px; }
MenuItem:hover, MenuItem:focus, MenuCheckItem:hover, MenuCheckItem:focus, MenuRadioItem:hover, MenuRadioItem:focus, Submenu:hover, Submenu:focus, Submenu[expanded] { background: var(--nz-selection); color: var(--nz-on-selection); }
MenuItem:disabled, MenuCheckItem:disabled, MenuRadioItem:disabled, Submenu:disabled { background: transparent; color: var(--nz-muted); }
MenuItem[intent="danger"] { color: var(--nz-danger); }
MenuItemLeading { display: flex; align-items: center; justify-content: center; grid-column-start: 1; }
MenuItemLabel { grid-column-start: 2; }
MenuItemDescription { grid-column-start: 2; color: var(--nz-muted); font-size: 12px; }
MenuItem > ShortcutLabel, MenuCheckItem > ShortcutLabel, MenuRadioItem > ShortcutLabel { grid-column-start: 3; grid-row-start: 1; color: inherit; }
MenuItemCheck { width: 12px; height: 12px; stroke-width: 2px; }
MenuItemRadio { width: 6px; height: 6px; border-radius: 50%; }
MenuItemRadio[checked] { background: currentColor; }
MenuItemSubmenuArrow { grid-column-start: 4; grid-row-start: 1; width: 10px; height: 10px; stroke-width: 1.5px; }
MenuSeparator { height: 1px; margin: 2px 0; background: var(--nz-muted); }
MenuSection, MenuRadioGroup { display: flex; flex-direction: column; }
MenuSectionHeader { padding: 4px 6px 2px; color: var(--nz-muted); font-size: 12px; font-weight: 700; }

/* ---- Primitives ---- */
Pressable, RovingFocusItem { cursor: default; }
PopoverContainer { padding: 6px; border: 1px solid var(--nz-on-surface); background: var(--nz-surface); color: var(--nz-on-surface); }
TooltipContainer { padding: 2px 6px; border: 1px solid var(--nz-on-surface); background: var(--nz-surface); color: var(--nz-on-surface); max-width: 320px; }
DialogContainer { display: flex; flex-direction: column; max-width: 90%; max-height: 90%; padding: 12px; border: 1px solid var(--nz-on-surface); background: var(--nz-surface); color: var(--nz-on-surface); }
DialogContainerScrim { position: absolute; left: 0; top: 0; right: 0; bottom: 0; background: var(--nz-scrim); }
[dialog-placement="top"] { position: absolute; left: 0; right: 0; top: 0; max-width: none; }
[dialog-placement="bottom"] { position: absolute; left: 0; right: 0; bottom: 0; max-width: none; }
[dialog-placement="start"] { position: absolute; left: 0; top: 0; bottom: 0; max-height: none; width: 320px; }
[dialog-placement="end"] { position: absolute; right: 0; top: 0; bottom: 0; max-height: none; width: 320px; }
[dialog-placement="fullscreen"] { position: absolute; left: 0; top: 0; right: 0; bottom: 0; max-width: none; max-height: none; }
Collapsible { display: block; }
Indicator { position: absolute; left: var(--nz-indicator-x, 0px); top: var(--nz-indicator-y, 0px); width: var(--nz-indicator-width, 0px); height: var(--nz-indicator-height, 0px); pointer-events: none; }
Indicator[hidden] { display: none; }
VisuallyHidden { position: absolute; width: 1px; height: 1px; overflow-x: hidden; overflow-y: hidden; clip-path: inset(50%); white-space: pre; }
Virtualizer { display: flex; flex-direction: column; overflow-y: auto; min-height: 0; }
Virtualizer[orientation="horizontal"] { flex-direction: row; overflow-x: auto; overflow-y: hidden; }
VirtualizerItem { flex-shrink: 0; }
OverflowContainer { display: flex; align-items: center; min-width: 0; overflow-x: hidden; }
OverflowContainerItem, OverflowContainerOverflow { flex-shrink: 0; }
Draggable { cursor: default; }
ResizeHandle { flex-shrink: 0; background: transparent; }
ResizeHandle[edge="left"], ResizeHandle[edge="right"] { width: 5px; min-height: 16px; align-self: stretch; cursor: ew-resize; }
ResizeHandle[edge="top"], ResizeHandle[edge="bottom"] { height: 5px; min-width: 16px; align-self: stretch; cursor: ns-resize; }
ResizeHandle[edge="bottom-right"] { width: 12px; height: 12px; cursor: nwse-resize; }
ResizeHandle:hover, ResizeHandle[dragging] { background: var(--nz-selection); }
Listbox { display: flex; flex-direction: column; border: 1px solid var(--nz-border); background: var(--nz-field); color: var(--nz-on-field); }
Listbox[orientation="horizontal"] { flex-direction: row; }
ListboxOption { padding: 2px 6px; }
ListboxOption[selected] { background: var(--nz-selection); color: var(--nz-on-selection); }
ListboxOption:disabled { color: var(--nz-muted); }
Combobox { display: flex; align-items: center; gap: 4px; min-width: 10em; padding: 1px 4px; border: 1px solid var(--nz-border); background: var(--nz-field); color: var(--nz-on-field); }
Combobox:focus-within { border-color: var(--nz-accent); }
ComboboxInput { flex: 1; min-width: 0; }
ComboboxLeading, ComboboxTrailing { display: flex; align-items: center; flex-shrink: 0; }
ComboboxPopup, SearchFieldPopup { display: flex; flex-direction: column; max-height: 300px; overflow-y: auto; padding: 2px; border: 1px solid var(--nz-on-surface); background: var(--nz-surface); color: var(--nz-on-surface); }
ComboboxPopup[drop-down], SearchFieldPopup[drop-down] { max-height: none; }
ComboboxOption { padding: 3px 6px; }
SearchFieldOption { display: flex; align-items: center; padding: 3px 6px; }
SearchFieldOption Mark { padding: 0; font-weight: 600; }
ComboboxOption[current], SearchFieldOption[current], SearchFieldOption:hover { background: var(--nz-selection); color: var(--nz-on-selection); }
ComboboxEmpty { padding: 3px 6px; color: var(--nz-muted); }
SearchFieldEmpty { display: flex; align-items: center; min-height: 26px; padding: 3px 6px; color: var(--nz-muted); }

/* ==== Group sections: each component group adds its rules inside its own section only. ==== */

/* ---- Group: Text and layout (text-layout) ---- */

/* Text and content */
Paragraph { display: block; }
Paragraph + Paragraph, Paragraph + List, Paragraph + DefinitionList, Paragraph + Blockquote, Paragraph + CodeBlock, Paragraph + Figure, List + Paragraph, List + List, List + DefinitionList, List + Blockquote, List + CodeBlock, List + Figure, DefinitionList + Paragraph, DefinitionList + List, DefinitionList + DefinitionList, DefinitionList + Blockquote, DefinitionList + CodeBlock, DefinitionList + Figure, Blockquote + Paragraph, Blockquote + List, Blockquote + DefinitionList, Blockquote + Blockquote, Blockquote + CodeBlock, Blockquote + Figure, CodeBlock + Paragraph, CodeBlock + List, CodeBlock + DefinitionList, CodeBlock + Blockquote, CodeBlock + CodeBlock, CodeBlock + Figure, Figure + Paragraph, Figure + List, Figure + DefinitionList, Figure + Blockquote, Figure + CodeBlock, Figure + Figure { margin-top: 0.75em; }
Kbd { display: block; width: max-content; padding: 0 4px; border: 1px solid var(--nz-border); border-bottom-width: 2px; background: var(--nz-control); color: var(--nz-on-control); font-family: Consolas, "Cascadia Mono", "DejaVu Sans Mono", monospace; font-size: 0.9em; white-space: pre; }
Blockquote { display: flex; flex-direction: column; gap: 4px; padding: 2px 0 2px 10px; border-left: 3px solid var(--nz-border); }
BlockquoteContent { display: block; }
BlockquoteCite { display: block; color: var(--nz-muted); font-size: 12px; }
List { display: flex; flex-direction: column; gap: 2px; }
ListItem { display: grid; grid-template-columns: 1.2em minmax(0px, 1fr); column-gap: 4px; }
List[ordered] > ListItem, List[marker-style="decimal"] > ListItem, List[marker-style="lower-alpha"] > ListItem, List[marker-style="upper-alpha"] > ListItem { grid-template-columns: 2em minmax(0px, 1fr); }
List[marker-style="lower-roman"] > ListItem, List[marker-style="upper-roman"] > ListItem { grid-template-columns: 3em minmax(0px, 1fr); }
List[marker-style="none"] > ListItem { display: block; }
List[marker-style="none"] > ListItem > ListItemMarker { display: none; }
ListItemMarker { text-align: right; white-space: pre; }
ListItemContent { display: block; min-width: 0; }
ListItemContent > List { margin-top: 2px; }
DefinitionList { display: flex; flex-direction: column; gap: 2px; }
DefinitionTerm { display: block; font-weight: 700; }
DefinitionDescription { display: block; padding-left: 1.5em; }
DefinitionDescription + DefinitionTerm { margin-top: 4px; }
DefinitionList[layout="inline"] { display: grid; grid-template-columns: max-content minmax(0px, 1fr); column-gap: 12px; row-gap: 2px; }
DefinitionList[layout="inline"] > DefinitionTerm { grid-column-start: 1; margin-top: 0; }
DefinitionList[layout="inline"] > DefinitionDescription { grid-column-start: 2; padding-left: 0; }
CodeBlock { display: flex; align-items: flex-start; border: 1px solid var(--nz-border); background: var(--nz-field); color: var(--nz-on-field); font-family: Consolas, "Cascadia Mono", "DejaVu Sans Mono", monospace; font-size: 13px; line-height: 1.4; }
CodeBlockLineNumbers { flex-shrink: 0; align-self: stretch; padding: 6px; border-right: 1px solid var(--nz-border); color: var(--nz-muted); text-align: right; white-space: pre; user-select: none; }
CodeBlockCode { display: block; flex: 1; min-width: 0; padding: 6px 8px; white-space: pre; overflow-x: auto; cursor: text; }
CodeBlock[wrap] > CodeBlockCode { white-space: pre-wrap; overflow-x: hidden; }
Figure { display: flex; flex-direction: column; gap: 4px; }
FigureCaption { display: block; color: var(--nz-muted); font-size: 12px; }

/* Layout and surfaces */
ScrollView { display: block; min-width: 0; min-height: 0; border: 1px solid transparent; }
ScrollView[orientation="vertical"] { overflow-x: hidden; overflow-y: auto; }
ScrollView[orientation="horizontal"] { overflow-x: auto; overflow-y: hidden; }
ScrollView[orientation="both"] { overflow-x: auto; overflow-y: auto; }
ScrollView[can-scroll-top] { border-top-color: var(--nz-border); }
ScrollView[can-scroll-bottom] { border-bottom-color: var(--nz-border); }
ScrollView[can-scroll-left] { border-left-color: var(--nz-border); }
ScrollView[can-scroll-right] { border-right-color: var(--nz-border); }
Card { display: flex; flex-direction: column; min-width: 0; border: 1px solid var(--nz-border); background: var(--nz-control); color: var(--nz-on-control); overflow-x: hidden; overflow-y: hidden; }
Card[appearance="outline"] { background: transparent; color: var(--nz-on-surface); }
Card[appearance="subtle"] { background: transparent; border-color: transparent; color: var(--nz-on-surface); }
Card[orientation="horizontal"] { display: grid; grid-template-columns: max-content minmax(0px, 1fr); grid-template-areas: "preview header" "preview body" "preview footer"; align-content: start; }
Card[orientation="horizontal"] > CardPreview { grid-area: preview; }
Card[orientation="horizontal"] > CardHeader { grid-area: header; }
Card[orientation="horizontal"] > CardBody { grid-area: body; }
Card[orientation="horizontal"] > CardFooter { grid-area: footer; }
Card[clickable] { cursor: default; }
Card[clickable]:hover, Card[appearance="subtle"]:hover { border-color: var(--nz-on-surface); }
Card[clickable]:active { border-color: var(--nz-accent); }
Card[selected] { border-color: var(--nz-accent); background: var(--nz-selection); color: var(--nz-on-selection); }
Card:disabled { border-color: var(--nz-muted); color: var(--nz-muted); }
CardPreview { display: block; overflow-x: hidden; overflow-y: hidden; }
CardHeader { display: grid; grid-template-columns: max-content minmax(0px, 1fr) max-content; grid-template-areas: "media title actions" "media subtitle actions"; column-gap: 8px; align-items: center; padding: 8px 10px 0; }
CardMedia { grid-area: media; display: flex; align-items: center; }
CardTitle { grid-area: title; font-weight: 700; }
CardSubtitle { grid-area: subtitle; color: var(--nz-muted); font-size: 12px; }
Card[selected] CardSubtitle { color: inherit; }
CardActions { grid-area: actions; display: flex; align-items: center; gap: 4px; }
CardBody { display: block; padding: 8px 10px; }
CardFooter { display: flex; align-items: center; justify-content: flex-end; gap: 6px; padding: 0 10px 8px; }
GroupBox { display: flex; flex-direction: column; padding: 0 8px 8px; border: 1px solid var(--nz-border); }
GroupBoxHeader { display: flex; align-items: center; gap: 6px; padding: 4px 0; font-weight: 700; }
GroupBoxTitle { display: block; }
GroupBoxToggle { display: flex; align-items: center; gap: 4px; cursor: default; }
GroupBoxToggle:hover { color: var(--nz-accent); }
GroupBoxChevron { width: 10px; height: 10px; flex-shrink: 0; stroke-width: 1.5px; }
GroupBoxPanel { display: block; }
GroupBoxPanelContent { display: flex; flex-direction: column; gap: 6px; }
GroupBox:disabled > GroupBoxHeader, GroupBoxPanel:disabled { color: var(--nz-muted); }
Expander, AccordionItem { display: flex; flex-direction: column; border: 1px solid var(--nz-border); }
ExpanderHeader, AccordionItemHeader { display: flex; align-items: center; gap: 8px; min-height: 28px; padding: 4px 8px; background: var(--nz-control); color: var(--nz-on-control); cursor: default; }
ExpanderHeader:hover, AccordionItemHeader:hover { color: var(--nz-accent); }
ExpanderHeader:active, AccordionItemHeader:active { background: var(--nz-selection); color: var(--nz-on-selection); }
Expander:disabled > ExpanderHeader, AccordionItem:disabled > AccordionItemHeader { color: var(--nz-muted); }
ExpanderLeading, AccordionItemLeading, ExpanderTrailing, AccordionItemTrailing { display: flex; align-items: center; flex-shrink: 0; }
ExpanderHeaderText, AccordionItemHeaderText { display: flex; flex-direction: column; flex: 1; min-width: 0; }
ExpanderTitle, AccordionItemTitle { display: block; }
ExpanderSubtitle, AccordionItemSubtitle { display: block; color: var(--nz-muted); font-size: 12px; }
ExpanderChevron, AccordionItemChevron { width: 10px; height: 10px; flex-shrink: 0; stroke-width: 1.5px; }
ExpanderPanel, AccordionItemPanel { display: block; }
ExpanderPanelContent, AccordionItemPanelContent { display: block; padding: 8px; border-top: 1px solid var(--nz-border); }
Accordion { display: flex; flex-direction: column; }
AccordionItem + AccordionItem { border-top-width: 0; }
SplitView { display: flex; position: relative; min-width: 0; min-height: 0; overflow-x: hidden; overflow-y: hidden; }
SplitView[orientation="vertical"] { flex-direction: column; }
SplitViewPane { display: flex; flex-direction: column; flex-shrink: 0; min-width: 0; min-height: 0; overflow-x: hidden; overflow-y: auto; background: var(--nz-surface); color: var(--nz-on-surface); }
SplitView[orientation="horizontal"][placement="start"] > SplitViewPane { border-right: 1px solid var(--nz-border); top: 0; bottom: 0; left: 0; }
SplitView[orientation="horizontal"][placement="end"] > SplitViewPane { border-left: 1px solid var(--nz-border); top: 0; bottom: 0; right: 0; }
SplitView[orientation="vertical"][placement="start"] > SplitViewPane { border-bottom: 1px solid var(--nz-border); left: 0; right: 0; top: 0; }
SplitView[orientation="vertical"][placement="end"] > SplitViewPane { border-top: 1px solid var(--nz-border); left: 0; right: 0; bottom: 0; }
SplitView[display-mode="overlay"] > SplitViewPane, SplitView[display-mode="compact"] > SplitViewPane { position: absolute; z-index: 2; }
SplitViewContent { display: flex; flex-direction: column; flex: 1; min-width: 0; min-height: 0; }
SplitViewScrim { position: absolute; left: 0; top: 0; right: 0; bottom: 0; z-index: 1; background: transparent; }
StatusBar { display: flex; align-items: stretch; min-height: 22px; border-top: 1px solid var(--nz-border); background: var(--nz-control); color: var(--nz-on-control); font-size: 12px; }
StatusBarSection { display: flex; align-items: center; flex-shrink: 0; min-width: 0; padding: 0 8px; white-space: pre; overflow-x: hidden; text-overflow: ellipsis; }
StatusBarSection + StatusBarSection { border-left: 1px solid var(--nz-border); }
StatusBarSection[flexible] { flex: 1; flex-shrink: 1; }
StatusBarSection[align="center"] { justify-content: center; }
StatusBarSection[align="end"] { justify-content: flex-end; }
StatusBarSizeGrip { align-self: flex-end; flex-shrink: 0; width: 12px; height: 12px; cursor: nwse-resize; background: linear-gradient(135deg, transparent 55%, var(--nz-muted) 55%, var(--nz-muted) 63%, transparent 63%, transparent 75%, var(--nz-muted) 75%, var(--nz-muted) 83%, transparent 83%); }

/* ---- End of group: text-layout ---- */

/* ---- Group: Feedback (feedback) ---- */
/* Shared colors: every feedback control reads its tint from --nz-feedback-color, set by intent, severity or quality. */
ProgressBar, Spinner, Tag, BadgeIndicator { --nz-feedback-color: var(--nz-accent); --nz-on-feedback: var(--nz-on-accent); }
ProgressBar[intent="neutral"], Spinner[intent="neutral"], Tag[intent="neutral"], BadgeIndicator[intent="neutral"] { --nz-feedback-color: var(--nz-muted); --nz-on-feedback: var(--nz-surface); }
ProgressBar[intent="success"], Spinner[intent="success"], Tag[intent="success"], BadgeIndicator[intent="success"] { --nz-feedback-color: var(--nz-success); --nz-on-feedback: #ffffff; }
ProgressBar[intent="warning"], Spinner[intent="warning"], Tag[intent="warning"], BadgeIndicator[intent="warning"] { --nz-feedback-color: var(--nz-warning); --nz-on-feedback: #ffffff; }
ProgressBar[intent="danger"], Spinner[intent="danger"], Tag[intent="danger"], BadgeIndicator[intent="danger"] { --nz-feedback-color: var(--nz-danger); --nz-on-feedback: #ffffff; }
Meter { --nz-feedback-color: var(--nz-success); }
Meter[quality="suboptimal"] { --nz-feedback-color: var(--nz-warning); }
Meter[quality="worst"] { --nz-feedback-color: var(--nz-danger); }
InlineMessage, Banner, Toast { --nz-feedback-color: var(--nz-accent); }
InlineMessage[severity="success"], Banner[severity="success"], Toast[severity="success"] { --nz-feedback-color: var(--nz-success); }
InlineMessage[severity="warning"], Banner[severity="warning"], Toast[severity="warning"] { --nz-feedback-color: var(--nz-warning); }
InlineMessage[severity="error"], Banner[severity="error"], Toast[severity="error"] { --nz-feedback-color: var(--nz-danger); }

/* ProgressBar: label and value above a bordered track; indeterminate is a hatched full track, without motion. */
ProgressBar { display: grid; grid-template-columns: minmax(0px, 1fr) max-content; column-gap: 8px; row-gap: 2px; min-width: 10em; }
ProgressBarLabel { grid-column-start: 1; grid-row-start: 1; }
ProgressBarValue { grid-column-start: 2; grid-row-start: 1; color: var(--nz-muted); }
ProgressBarTrack { grid-column-start: 1; grid-column-end: 3; grid-row-start: 2; position: relative; display: flex; gap: 2px; height: 6px; border: 1px solid var(--nz-border); background: var(--nz-field); overflow: hidden; }
ProgressBarPulse, SpinnerMark, SpinnerValue { display: none; }
ProgressBar[size="small"] > ProgressBarTrack { height: 3px; }
ProgressBar[size="large"] > ProgressBarTrack { height: 10px; }
ProgressBarBuffer { position: absolute; left: 0; top: 0; bottom: 0; width: calc(var(--nz-progress-buffer, 0) * 100%); background: color-mix(in srgb, var(--nz-feedback-color) 30%, var(--nz-field)); }
ProgressBarFill { position: absolute; left: 0; top: 0; bottom: 0; width: calc(var(--nz-progress-fraction, 0) * 100%); background: var(--nz-feedback-color); }
ProgressBarFill[indeterminate] { width: 100%; background: linear-gradient(135deg, var(--nz-feedback-color) 25%, transparent 25%, transparent 50%, var(--nz-feedback-color) 50%, var(--nz-feedback-color) 75%, transparent 75%); background-size: 8px 8px; }
ProgressBar[segmented] > ProgressBarTrack { border-color: transparent; background: transparent; height: 8px; }
ProgressBarSegment { flex: 1; border: 1px solid var(--nz-border); background: var(--nz-field); }
ProgressBarSegment[filled] { background: var(--nz-feedback-color); border-color: var(--nz-feedback-color); }
ProgressBarSegment[indeterminate] { background: linear-gradient(135deg, var(--nz-feedback-color) 25%, transparent 25%, transparent 50%, var(--nz-feedback-color) 50%, var(--nz-feedback-color) 75%, transparent 75%); background-size: 8px 8px; }

/* Spinner: a muted full ring under the arc; indeterminate shows a static quarter arc. */
Spinner { display: flex; align-items: center; gap: 6px; width: fit-content; }
SpinnerIndicator { position: relative; width: 20px; height: 20px; flex-shrink: 0; }
SpinnerTrack, SpinnerRing { position: absolute; left: 0; top: 0; right: 0; bottom: 0; stroke-width: 2px; }
SpinnerTrack { color: var(--nz-border); }
SpinnerRing { color: var(--nz-feedback-color); }
Spinner[size="small"] > SpinnerIndicator { width: 14px; height: 14px; }
Spinner[size="large"] > SpinnerIndicator { width: 32px; height: 32px; }
Spinner[size="large"] > SpinnerIndicator > SpinnerTrack, Spinner[size="large"] > SpinnerIndicator > SpinnerRing { stroke-width: 3px; }
SpinnerLabel { color: var(--nz-on-surface); }

/* Meter: like the progress bar, tinted by quality; vertical meters fill from the bottom. */
Meter { display: grid; grid-template-columns: minmax(0px, 1fr) max-content; column-gap: 8px; row-gap: 2px; min-width: 10em; }
MeterLabel { grid-column-start: 1; grid-row-start: 1; }
MeterValue { grid-column-start: 2; grid-row-start: 1; color: var(--nz-muted); }
MeterTrack { grid-column-start: 1; grid-column-end: 3; grid-row-start: 2; position: relative; display: flex; gap: 2px; height: 8px; border: 1px solid var(--nz-border); background: var(--nz-field); overflow: hidden; }
Meter[size="small"] > MeterTrack { height: 4px; }
Meter[size="large"] > MeterTrack { height: 12px; }
MeterFill { position: absolute; left: 0; top: 0; bottom: 0; width: calc(var(--nz-meter-fraction, 0) * 100%); background: var(--nz-feedback-color); }
Meter[segmented] > MeterTrack { border-color: transparent; background: transparent; }
MeterSegment { flex: 1; border: 1px solid var(--nz-border); background: var(--nz-field); }
MeterSegment[filled] { background: var(--nz-feedback-color); border-color: var(--nz-feedback-color); }
Meter[orientation="vertical"] { min-width: 0; grid-template-columns: max-content; justify-items: center; }
Meter[orientation="vertical"] > MeterLabel { grid-row-start: 3; }
Meter[orientation="vertical"] > MeterValue { grid-column-start: 1; grid-row-start: 1; }
Meter[orientation="vertical"] > MeterTrack { grid-column-end: 2; width: 8px; height: 6em; flex-direction: column-reverse; }
Meter[orientation="vertical"] > MeterTrack > MeterFill { top: auto; right: 0; width: auto; height: calc(var(--nz-meter-fraction, 0) * 100%); }

/* Badge: a pill indicator; decorating badges place it on a corner of the content. */
Badge { display: flex; align-items: center; width: fit-content; }
Badge[decorating] { position: relative; }
BadgeContent { display: flex; }
BadgeIndicator { display: flex; align-items: center; justify-content: center; min-width: 16px; height: 16px; padding: 0 4px; border-radius: 999px; background: var(--nz-feedback-color); color: var(--nz-on-feedback); font-size: 11px; font-weight: 700; white-space: pre; }
BadgeIndicator[dot] { min-width: 8px; width: 8px; height: 8px; padding: 0; }
Badge[size="small"] > BadgeIndicator { min-width: 12px; height: 12px; font-size: 9px; padding: 0 2px; }
Badge[size="large"] > BadgeIndicator { min-width: 20px; height: 20px; font-size: 13px; padding: 0 6px; }
Badge[decorating] > BadgeIndicator { position: absolute; border: 1px solid var(--nz-surface); }
Badge[decorating] > BadgeIndicator[corner="top-end"] { top: -6px; right: -6px; }
Badge[decorating] > BadgeIndicator[corner="top-start"] { top: -6px; left: -6px; }
Badge[decorating] > BadgeIndicator[corner="bottom-end"] { bottom: -6px; right: -6px; }
Badge[decorating] > BadgeIndicator[corner="bottom-start"] { bottom: -6px; left: -6px; }

/* Tag: a bordered label; the dot and the colored intents use the intent color, in-progress tags a dashed border. */
Tag { display: flex; width: fit-content; align-items: center; gap: 4px; padding: 0 6px; min-height: 20px; border: 1px solid var(--nz-border); background: var(--nz-control); color: var(--nz-on-control); }
Tag[intent="accent"], Tag[intent="success"], Tag[intent="warning"], Tag[intent="danger"] { border-color: var(--nz-feedback-color); color: var(--nz-feedback-color); background: var(--nz-surface); }
Tag[in-progress] { border-style: dashed; }
Tag[size="small"] { min-height: 16px; font-size: 11px; padding: 0 4px; }
Tag[size="large"] { min-height: 26px; padding: 0 8px; }
Tag:disabled { color: var(--nz-muted); border-color: var(--nz-muted); }
TagLeading { display: flex; align-items: center; flex-shrink: 0; }
TagDot { width: 8px; height: 8px; border-radius: 50%; background: var(--nz-feedback-color); flex-shrink: 0; }
TagLabel { white-space: pre; }
TagRemove { display: flex; align-items: center; justify-content: center; width: 14px; height: 14px; color: var(--nz-muted); cursor: default; }
TagRemove:hover { color: var(--nz-on-selection); background: var(--nz-selection); }

/* Buttons inside messages, toasts and the loading boundary. */
InlineMessageClose, BannerClose, ToastClose { display: flex; align-items: center; justify-content: center; flex-shrink: 0; width: 22px; height: 22px; border: 1px solid transparent; color: var(--nz-on-surface); cursor: default; }
InlineMessageClose:hover, BannerClose:hover, ToastClose:hover { border-color: var(--nz-border); }
InlineMessageClose:active, BannerClose:active, ToastClose:active { background: var(--nz-selection); color: var(--nz-on-selection); }
BannerAction, ToastAction, LoadingBoundaryRetry { display: flex; align-items: center; justify-content: center; flex-shrink: 0; min-height: 22px; padding: 1px 8px; border: 1px solid var(--nz-border); background: var(--nz-control); color: var(--nz-on-control); cursor: default; }
BannerAction:hover, ToastAction:hover, LoadingBoundaryRetry:hover { border-color: var(--nz-on-surface); }
BannerAction:active, ToastAction:active, LoadingBoundaryRetry:active { background: var(--nz-selection); color: var(--nz-on-selection); }

/* Severity marks: a filled square with a glyph or a letter in the severity color. */
InlineMessageMark, BannerMark, ToastMark { display: flex; align-items: center; justify-content: center; flex-shrink: 0; width: 16px; height: 16px; background: var(--nz-feedback-color); color: var(--nz-surface); font-weight: 700; font-size: 12px; stroke-width: 2px; }

/* InlineMessage: a bordered box in the flow. */
InlineMessage { display: flex; align-items: center; gap: 8px; padding: 6px 8px; border: 1px solid var(--nz-feedback-color); background: var(--nz-surface); color: var(--nz-on-surface); }
InlineMessage[layout="expanded"] { display: grid; grid-template-columns: max-content minmax(0px, 1fr) max-content; column-gap: 8px; row-gap: 6px; align-items: start; }
InlineMessageBody { display: flex; flex-wrap: wrap; column-gap: 6px; flex: 1; min-width: 0; }
InlineMessage[layout="expanded"] > InlineMessageBody { flex-direction: column; grid-column-start: 2; grid-row-start: 1; }
InlineMessageTitle { font-weight: 700; }
InlineMessageText { display: block; }
InlineMessageActions { display: flex; gap: 6px; align-items: center; flex-shrink: 0; }
InlineMessage[layout="expanded"] > InlineMessageActions { grid-column-start: 2; grid-row-start: 2; }
InlineMessage[layout="expanded"] > InlineMessageClose { grid-column-start: 3; grid-row-start: 1; }

/* Banner: a full-width strip with a bottom border. */
Banner { display: flex; align-items: center; gap: 8px; padding: 4px 8px; border-bottom: 1px solid var(--nz-feedback-color); background: var(--nz-control); color: var(--nz-on-control); }
BannerMessage { flex: 1; min-width: 0; }

/* ToastHost: the content fills the host; the region is stacked at a corner of the window. */
ToastHost { display: flex; flex-direction: column; min-height: 0; }
ToastHostAnnouncer { position: absolute; width: 1px; height: 1px; overflow-x: hidden; overflow-y: hidden; clip-path: inset(50%); white-space: pre; }
ToastHostRegion { position: absolute; display: flex; flex-direction: column; gap: 8px; width: 320px; max-width: 90%; pointer-events: none; }
ToastHostRegion[toast-placement="top-start"] { left: 16px; top: 16px; }
ToastHostRegion[toast-placement="top-center"] { left: 0; right: 0; top: 16px; width: auto; max-width: none; align-items: center; }
ToastHostRegion[toast-placement="top-end"] { right: 16px; top: 16px; }
ToastHostRegion[toast-placement="bottom-start"] { left: 16px; bottom: 16px; }
ToastHostRegion[toast-placement="bottom-center"] { left: 0; right: 0; bottom: 16px; width: auto; max-width: none; align-items: center; }
ToastHostRegion[toast-placement="bottom-end"] { right: 16px; bottom: 16px; }
Toast { position: relative; display: flex; align-items: flex-start; gap: 8px; width: 320px; max-width: 100%; padding: 8px 8px 10px; border: 1px solid var(--nz-on-surface); background: var(--nz-surface); color: var(--nz-on-surface); pointer-events: auto; }
ToastBody { display: flex; flex-direction: column; flex: 1; min-width: 0; }
ToastTitle { font-weight: 700; }
ToastMessage { display: block; }
ToastTimer { position: absolute; left: 0; bottom: 0; height: 2px; width: calc(var(--nz-toast-remaining, 1) * 100%); background: var(--nz-feedback-color); }
ToastTimer[paused] { background: var(--nz-muted); }

/* EmptyState: a centered column. */
EmptyState { display: flex; flex-direction: column; align-items: center; gap: 6px; padding: 24px; text-align: center; }
EmptyStateIllustration { display: flex; justify-content: center; color: var(--nz-muted); font-size: 48px; }
EmptyStateTitle { font-weight: 700; font-size: 1.17em; }
EmptyStateDescription { color: var(--nz-muted); max-width: 40em; }
EmptyStateActions { display: flex; gap: 8px; justify-content: center; margin-top: 6px; }

/* Skeleton: static muted blocks. */
Skeleton { display: flex; flex-direction: column; gap: 6px; }
Skeleton[shape="text"] { width: 100%; }
SkeletonLine { height: 0.8em; background: color-mix(in srgb, var(--nz-muted) 25%, var(--nz-surface)); }
SkeletonLine[last] { width: 60%; }
Skeleton[shape="circle"] { width: 32px; height: 32px; border-radius: 50%; background: color-mix(in srgb, var(--nz-muted) 25%, var(--nz-surface)); }
Skeleton[shape="rectangle"] { width: 100%; height: 64px; background: color-mix(in srgb, var(--nz-muted) 25%, var(--nz-surface)); }

/* Avatar: a circle or square with the picture, the initials or a muted fallback. */
Avatar { position: relative; display: flex; align-items: center; justify-content: center; flex-shrink: 0; width: var(--nz-avatar-size, 32px); height: var(--nz-avatar-size, 32px); border: 1px solid var(--nz-border); border-radius: 50%; background: var(--nz-control); color: var(--nz-on-control); font-size: 13px; font-weight: 700; }
Avatar[size="small"] { width: var(--nz-avatar-size, 24px); height: var(--nz-avatar-size, 24px); font-size: 10px; }
Avatar[size="large"] { width: var(--nz-avatar-size, 48px); height: var(--nz-avatar-size, 48px); font-size: 19px; }
Avatar[shape="square"] { border-radius: 0; }
AvatarImage { position: absolute; left: 0; top: 0; right: 0; bottom: 0; border-radius: 50%; object-fit: cover; }
Avatar[shape="square"] > AvatarImage { border-radius: 0; }
AvatarInitials { white-space: pre; }
AvatarFallback { width: 50%; height: 50%; border-radius: 50%; background: var(--nz-muted); }
AvatarPresence { position: absolute; right: 0; bottom: 0; width: 25%; height: 25%; min-width: 6px; min-height: 6px; border: 1px solid var(--nz-surface); border-radius: 50%; background: var(--nz-muted); }
AvatarPresence[presence="online"] { background: var(--nz-success); }
AvatarPresence[presence="away"] { background: var(--nz-warning); }
AvatarPresence[presence="busy"] { background: var(--nz-danger); }
AvatarPresence[presence="offline"] { background: var(--nz-surface); border-color: var(--nz-muted); }
AvatarBadge { position: absolute; right: -4px; top: -4px; display: flex; }

/* AvatarGroup: overlapping avatars with a surface-colored ring and the overflow count. */
AvatarGroup { display: flex; align-items: center; width: fit-content; }
AvatarGroup > Avatar { border: 2px solid var(--nz-surface); }
AvatarGroup > Avatar[position="middle"], AvatarGroup > Avatar[position="last"], AvatarGroupOverflow { margin-left: -8px; }
AvatarGroupOverflow { display: flex; align-items: center; justify-content: center; width: 32px; height: 32px; border: 2px solid var(--nz-surface); border-radius: 50%; background: var(--nz-control); color: var(--nz-on-control); font-size: 12px; font-weight: 700; }
AvatarGroupOverflow[size="small"] { width: 24px; height: 24px; font-size: 10px; }
AvatarGroupOverflow[size="large"] { width: 48px; height: 48px; font-size: 16px; }

/* LoadingBoundary: the loading indicator and the error are centered. */
LoadingBoundary { display: flex; flex-direction: column; }
LoadingBoundaryContent { display: flex; flex-direction: column; }
LoadingBoundaryLoading { display: flex; justify-content: center; padding: 12px; }
LoadingBoundaryError { display: flex; flex-direction: column; align-items: center; gap: 6px; padding: 12px; color: var(--nz-danger); }
LoadingBoundaryMessage { text-align: center; }
/* ---- End of group: feedback ---- */

/* ---- Group: Actions and app frame (actions-frame) ---- */

/* Button groups: members touch and share their borders; the hovered or focused member is drawn above its neighbours. */
ButtonGroup { display: flex; align-items: stretch; }
ButtonGroup[orientation="vertical"] { flex-direction: column; }
ButtonGroup > [position] { position: relative; }
ButtonGroup > [position]:hover, ButtonGroup > [position]:focus-visible { z-index: 1; }
ButtonGroup[orientation="horizontal"] > [position="middle"], ButtonGroup[orientation="horizontal"] > [position="last"] { margin-left: -1px; }
ButtonGroup[orientation="vertical"] > [position="middle"], ButtonGroup[orientation="vertical"] > [position="last"] { margin-top: -1px; }
ButtonGroup > [position][shape="pill"] { border-radius: 0; }

/* Menu button: a button followed by its chevron. */
MenuButton { display: flex; align-items: center; justify-content: center; gap: 6px; min-height: 26px; padding: 2px 8px 2px 10px; border: 1px solid var(--nz-border); background: var(--nz-control); color: var(--nz-on-control); cursor: default; }
MenuButton:hover { border-color: var(--nz-on-surface); }
MenuButton[appearance="accent"] { background: var(--nz-accent); color: var(--nz-on-accent); border-color: var(--nz-accent); }
MenuButton[appearance="tonal"] { border-color: var(--nz-accent); }
MenuButton[appearance="outline"] { background: transparent; }
MenuButton[appearance="subtle"] { background: transparent; border-color: transparent; }
MenuButton[appearance="subtle"]:hover { border-color: var(--nz-border); }
MenuButton[appearance="link"] { background: transparent; border-color: transparent; color: var(--nz-link); text-decoration-line: underline; padding: 0; min-height: 0; }
MenuButton:active, MenuButton[expanded] { background: var(--nz-selection); color: var(--nz-on-selection); }
MenuButton[intent="danger"] { color: var(--nz-danger); }
MenuButton[size="small"] { min-height: 20px; padding: 0 4px 0 6px; font-size: 12px; }
MenuButton[size="large"] { min-height: 36px; padding: 4px 12px 4px 16px; font-size: 16px; }
MenuButton[shape="pill"] { border-radius: 999px; }
MenuButton[icon-only] { padding: 2px 4px; }
MenuButton:disabled { color: var(--nz-muted); border-color: var(--nz-muted); background: var(--nz-control); }
MenuButtonChevron { width: 10px; height: 10px; flex-shrink: 0; stroke-width: 1.5px; }

/* Split button: primary part, divider and trigger inside one bordered box. */
SplitButton { display: flex; align-items: stretch; min-height: 26px; border: 1px solid var(--nz-border); background: var(--nz-control); color: var(--nz-on-control); cursor: default; }
SplitButton:hover { border-color: var(--nz-on-surface); }
SplitButton[appearance="accent"] { background: var(--nz-accent); color: var(--nz-on-accent); border-color: var(--nz-accent); }
SplitButton[appearance="tonal"] { border-color: var(--nz-accent); }
SplitButton[appearance="outline"] { background: transparent; }
SplitButton[appearance="subtle"] { background: transparent; border-color: transparent; }
SplitButton[appearance="subtle"]:hover { border-color: var(--nz-border); }
SplitButton[intent="danger"] { color: var(--nz-danger); }
SplitButton[intent="danger"][appearance="accent"] { background: var(--nz-danger); border-color: var(--nz-danger); color: #ffffff; }
SplitButtonPrimary { display: flex; align-items: center; justify-content: center; gap: 6px; padding: 2px 10px; }
SplitButtonLeading { display: flex; align-items: center; flex-shrink: 0; }
SplitButtonDivider { width: 1px; flex-shrink: 0; background: var(--nz-border); }
SplitButton[appearance="accent"] > SplitButtonDivider { background: var(--nz-on-accent); }
SplitButtonTrigger { display: flex; align-items: center; justify-content: center; padding: 2px 6px; }
SplitButtonChevron { width: 10px; height: 10px; flex-shrink: 0; stroke-width: 1.5px; }
SplitButtonPrimary:hover, SplitButtonTrigger:hover { background: color-mix(in srgb, var(--nz-selection) 25%, transparent); }
SplitButtonPrimary:active, SplitButtonTrigger:active, SplitButton[expanded] > SplitButtonTrigger, SplitButton[pressed] > SplitButtonPrimary { background: var(--nz-selection); color: var(--nz-on-selection); }
SplitButton[size="small"] { min-height: 20px; font-size: 12px; }
SplitButton[size="small"] > SplitButtonPrimary { padding: 0 6px; }
SplitButton[size="small"] > SplitButtonTrigger { padding: 0 4px; }
SplitButton[size="large"] { min-height: 36px; font-size: 16px; }
SplitButton[size="large"] > SplitButtonPrimary { padding: 4px 16px; }
SplitButton[size="large"] > SplitButtonTrigger { padding: 4px 10px; }
SplitButton:disabled { color: var(--nz-muted); border-color: var(--nz-muted); background: var(--nz-control); }
SplitButton:disabled > SplitButtonDivider { background: var(--nz-muted); }

/* Link: underlined link color; visited links mix in the text color. */
Link { display: flex; align-items: center; gap: 2px; color: var(--nz-link); text-decoration-line: underline; cursor: pointer; }
Link:hover { text-decoration-style: dotted; }
Link:active { color: var(--nz-on-surface); }
Link[visited] { color: color-mix(in srgb, var(--nz-link) 70%, var(--nz-on-surface)); }
Link:disabled { color: var(--nz-muted); text-decoration-line: none; cursor: default; }
LinkExternalMark { font-size: 0.8em; text-decoration-line: none; }

/* Toolbar and action bar. */
Toolbar { display: flex; align-items: center; gap: 2px; padding: 2px; min-width: 0; }
Toolbar[vertical] { flex-direction: column; align-items: stretch; }
ToolbarCommands { display: flex; align-items: center; gap: 2px; flex: 1; min-width: 0; }
Toolbar[vertical] > ToolbarCommands { flex-direction: column; align-items: stretch; }
ToolbarCommandsItem, ToolbarCommandsOverflow { display: flex; flex-shrink: 0; }
ToolbarGroup { display: flex; align-items: center; gap: 2px; }
ToolbarGroup[orientation="vertical"] { flex-direction: column; align-items: stretch; }
ToolbarSeparator { flex-shrink: 0; align-self: stretch; width: 1px; margin: 2px 3px; background: var(--nz-muted); }
ToolbarSeparator[orientation="horizontal"] { width: auto; height: 1px; margin: 3px 2px; }
ToolbarSpacer { flex: 1; min-width: 8px; min-height: 8px; }
ToolbarOverflowGlyph { font-weight: 700; }
Toolbar[label-position="below"] Button, Toolbar[label-position="below"] ToggleButton, Toolbar[label-position="below"] MenuButton { flex-direction: column; gap: 2px; font-size: 12px; }
Toolbar[label-position="none"] ButtonLeading + ButtonLabel { display: none; }
ActionBar { display: flex; align-items: center; gap: 8px; padding: 6px 8px; border-top: 1px solid var(--nz-border); background: var(--nz-surface); }
ActionBarStart, ActionBarCenter, ActionBarEnd { display: flex; align-items: center; gap: 6px; }
ActionBarStart { flex: 1; justify-content: flex-start; }
ActionBarCenter { justify-content: center; }
ActionBarEnd { flex: 1; justify-content: flex-end; }

/* Floating action button: a round accent button in a corner of its positioned container. */
FloatingActionButton { display: flex; align-items: center; justify-content: center; gap: 8px; position: absolute; z-index: 10; min-width: 44px; height: 44px; padding: 0 12px; border: 1px solid var(--nz-accent); border-radius: 22px; background: var(--nz-accent); color: var(--nz-on-accent); cursor: default; font-size: 16px; }
FloatingActionButton:hover { border-color: var(--nz-on-surface); }
FloatingActionButton:active { background: var(--nz-selection); color: var(--nz-on-selection); }
FloatingActionButton:disabled { background: var(--nz-control); color: var(--nz-muted); border-color: var(--nz-muted); }
FloatingActionButton[extended] { padding: 0 16px 0 12px; }
FloatingActionButton[size="small"] { min-width: 36px; height: 36px; border-radius: 18px; font-size: 14px; }
FloatingActionButton[size="large"] { min-width: 56px; height: 56px; border-radius: 28px; font-size: 20px; }
FloatingActionButton[placement="bottom-end"] { right: 16px; bottom: 16px; }
FloatingActionButton[placement="bottom-start"] { left: 16px; bottom: 16px; }
FloatingActionButton[placement="top-end"] { right: 16px; top: 16px; }
FloatingActionButton[placement="top-start"] { left: 16px; top: 16px; }

/* Title bar slots. Without native buttons the caption is 32px tall, or 48px for the tall variant. */
TitleBar { gap: 8px; padding-left: 8px; min-height: max(var(--nz-window-controls-height, 32px), 32px); background: var(--nz-surface); color: var(--nz-on-surface); }
TitleBar[height="tall"] { min-height: max(var(--nz-window-controls-height, 48px), 48px); }
:root[transparent-background] TitleBar { background-color: transparent; }
TitleBarLeading, TitleBarAppIcon, TitleBarTrailing { display: flex; align-items: center; gap: 4px; flex-shrink: 0; align-self: stretch; }
TitleBarAppIcon { min-width: 16px; justify-content: center; }
TitleBarHeading { display: flex; align-items: center; gap: 8px; min-width: 0; flex-shrink: 1; }
TitleBarTitle { white-space: pre; overflow-x: hidden; text-overflow: ellipsis; font-size: 12px; }
TitleBarSubtitle { white-space: pre; color: var(--nz-muted); font-size: 12px; }
TitleBarCenter { display: flex; align-items: center; justify-content: center; flex: 1; min-width: 0; }
SnapLayoutArea, DragRegion { display: flex; align-items: stretch; }

/* Caption buttons: flat 46px buttons; close turns red on hover. The glyphs are drawn with borders and lines. */
CaptionButtons { display: flex; align-items: stretch; align-self: stretch; flex-shrink: 0; }
CaptionButtonsButton { display: flex; align-items: center; justify-content: center; width: 46px; min-height: 32px; background: transparent; color: var(--nz-on-surface); cursor: default; }
CaptionButtonsButton:hover { background: color-mix(in srgb, var(--nz-on-surface) 10%, transparent); }
CaptionButtonsButton:active { background: color-mix(in srgb, var(--nz-on-surface) 20%, transparent); }
CaptionButtonsButton[kind="close"]:hover, CaptionButtonsButton[kind="close"]:active { background: #c42b1c; color: #ffffff; }
CaptionButtonsButton:disabled { color: var(--nz-muted); background: transparent; }
CaptionButtonsGlyph { position: relative; width: 10px; height: 10px; flex-shrink: 0; stroke-width: 1px; }
CaptionButtonsGlyph[kind="maximize"] { border: 1px solid currentColor; }
CaptionButtonsGlyph[kind="restore"] { width: 8px; height: 8px; margin: 2px 2px 0 0; border: 1px solid currentColor; }
CaptionButtonsGlyphBack { position: absolute; left: 1px; top: -3px; width: 8px; height: 8px; border-top: 1px solid currentColor; border-right: 1px solid currentColor; }
CaptionButtonsGlyph[kind="close"] { width: 12px; height: 12px; }
CaptionButtonsGlyphStroke { position: absolute; left: -1px; top: 5px; width: 14px; height: 1px; background: currentColor; }
CaptionButtonsGlyphStroke[stroke="rising"] { transform: rotate(-45deg); }
CaptionButtonsGlyphStroke[stroke="falling"] { transform: rotate(45deg); }
CaptionButtonsGlyph[kind="help"] { width: auto; height: auto; font-size: 13px; }
@media (forced-colors: active) {
    CaptionButtonsButton[kind="close"]:hover { background: Highlight; color: HighlightText; }
}

/* Page header: title row with actions; the compact form is smaller and hides the subtitle. */
PageHeader { display: flex; flex-direction: column; gap: 6px; padding: 12px 0; }
PageHeaderBreadcrumb { display: flex; align-items: center; gap: 4px; color: var(--nz-muted); font-size: 12px; }
PageHeaderMain { display: flex; align-items: center; gap: 12px; }
PageHeaderLeading { display: flex; align-items: center; flex-shrink: 0; }
PageHeaderHeading { display: flex; flex-direction: column; gap: 2px; flex: 1; min-width: 0; }
PageHeaderTitle { font-size: 24px; font-weight: 700; }
PageHeaderSubtitle { color: var(--nz-muted); }
PageHeaderActions { display: flex; align-items: center; gap: 6px; flex-shrink: 0; }
PageHeaderContent { display: flex; flex-direction: column; }
PageHeader[compact] { gap: 2px; padding: 4px 0; }
PageHeader[compact] PageHeaderTitle { font-size: 16px; }
PageHeader[compact] PageHeaderSubtitle { display: none; }

/* ---- End of group: actions-frame ---- */

/* ---- Group: Menus and overlays (overlays) ---- */
/* Menu bar: a row of plain entries; the open entry looks like a selected menu item. */
MenuBar { display: flex; align-items: center; padding: 1px; border-bottom: 1px solid var(--nz-border); background: var(--nz-surface); color: var(--nz-on-surface); }
MenuBarItem { display: flex; align-items: center; padding: 3px 8px; cursor: default; }
MenuBarItem:hover, MenuBarItem:focus, MenuBarItem[expanded] { background: var(--nz-selection); color: var(--nz-on-selection); }
MenuBarItem:disabled { background: transparent; color: var(--nz-muted); }
MenuBarItemLabel { white-space: pre; }

/* Close buttons shared by dialogs, drawers, popovers and teaching tips. */
DialogClose, DrawerClose, PopoverClose, TeachingTipClose { display: flex; align-items: center; justify-content: center; flex-shrink: 0; width: 22px; height: 22px; border: 1px solid transparent; color: var(--nz-on-surface); cursor: default; }
DialogClose:hover, DrawerClose:hover, PopoverClose:hover, TeachingTipClose:hover { border-color: var(--nz-border); }
DialogClose:active, DrawerClose:active, PopoverClose:active, TeachingTipClose:active { background: var(--nz-selection); color: var(--nz-on-selection); }
DialogCloseMark, DrawerCloseMark, PopoverCloseMark, TeachingTipCloseMark { font-size: 16px; line-height: 1; }

/* Arrows: a square turned by 45 degrees whose inner half hides under the content of the surface. */
PopoverArrow, TeachingTipArrow, TooltipArrow { position: absolute; width: 8px; height: 8px; border: 1px solid var(--nz-on-surface); background: var(--nz-surface); transform: rotate(45deg); pointer-events: none; }
PopoverArrow[placement="bottom"], TeachingTipArrow[placement="bottom"], TooltipArrow[placement="bottom"] { top: -5px; left: 12px; }
PopoverArrow[placement="top"], TeachingTipArrow[placement="top"], TooltipArrow[placement="top"] { bottom: -5px; left: 12px; }
PopoverArrow[placement="right"], TeachingTipArrow[placement="right"], TooltipArrow[placement="right"] { left: -5px; top: 8px; }
PopoverArrow[placement="left"], TeachingTipArrow[placement="left"], TooltipArrow[placement="left"] { right: -5px; top: 8px; }
[placement="bottom-center"] > PopoverArrow, [placement="top-center"] > PopoverArrow, [placement="bottom-center"] > TeachingTipArrow, [placement="top-center"] > TeachingTipArrow, [placement="bottom-center"] > TooltipArrow, [placement="top-center"] > TooltipArrow { left: calc(50% - 5px); }
[placement="bottom-end"] > PopoverArrow, [placement="top-end"] > PopoverArrow, [placement="bottom-end"] > TeachingTipArrow, [placement="top-end"] > TeachingTipArrow, [placement="bottom-end"] > TooltipArrow, [placement="top-end"] > TooltipArrow { left: auto; right: 12px; }
[placement="left-center"] > PopoverArrow, [placement="right-center"] > PopoverArrow, [placement="left-center"] > TeachingTipArrow, [placement="right-center"] > TeachingTipArrow, [placement="left-center"] > TooltipArrow, [placement="right-center"] > TooltipArrow { top: calc(50% - 5px); }
[placement="left-end"] > PopoverArrow, [placement="right-end"] > PopoverArrow, [placement="left-end"] > TeachingTipArrow, [placement="right-end"] > TeachingTipArrow, [placement="left-end"] > TooltipArrow, [placement="right-end"] > TooltipArrow { top: auto; bottom: 8px; }

/* Popover and hover card. */
PopoverTrigger { display: flex; }
Popover { display: flex; flex-direction: column; gap: 8px; min-width: 120px; max-width: 360px; padding: 8px; border: 1px solid var(--nz-on-surface); background: var(--nz-surface); color: var(--nz-on-surface); }
PopoverHeader { display: flex; align-items: center; gap: 8px; }
PopoverTitle { flex: 1; min-width: 0; font-weight: 700; }
PopoverBody { display: flex; flex-direction: column; gap: 6px; }
PopoverFooter { display: flex; justify-content: flex-end; align-items: center; gap: 6px; }

/* Tooltip: the surface rule is shared with the built-in tooltip of the engine; these rules add its parts. */
TooltipTrigger { display: flex; }
Tooltip[tooltip-surface] { display: flex; flex-direction: column; gap: 2px; }
TooltipTitle { font-weight: 700; }

/* Teaching tip. */
TeachingTip { display: flex; flex-direction: column; gap: 8px; width: 300px; max-width: 90%; padding: 10px; border: 1px solid var(--nz-on-surface); background: var(--nz-surface); color: var(--nz-on-surface); }
TeachingTipHero { display: flex; margin: -10px -10px 0; overflow-x: hidden; overflow-y: hidden; }
TeachingTipHeader { display: flex; align-items: flex-start; gap: 8px; }
TeachingTipHeading { display: flex; flex-direction: column; gap: 2px; flex: 1; min-width: 0; }
TeachingTipTitle { font-weight: 700; }
TeachingTipSubtitle { color: var(--nz-muted); }
TeachingTipBody { display: flex; flex-direction: column; gap: 6px; }
TeachingTipActions { display: flex; justify-content: flex-end; align-items: center; gap: 6px; }

/* Scrims of the dialog family. */
DialogScrim, AlertDialogScrim, DrawerScrim, CommandPaletteScrim { position: absolute; left: 0; top: 0; right: 0; bottom: 0; background: var(--nz-scrim); }

/* Dialog: width by size; edge placements stretch along their edge. */
Dialog { --nz-dialog-title-font-size: 1.17em; --nz-dialog-title-line-height: 1.2; display: flex; flex-direction: column; gap: 12px; width: 480px; max-width: 90%; max-height: 90%; padding: 12px; border: 1px solid var(--nz-on-surface); background: var(--nz-surface); color: var(--nz-on-surface); }
Dialog[size="small"] { width: 320px; }
Dialog[size="large"] { width: 720px; }
Dialog[size="fullscreen"], Dialog[dialog-placement="top"], Dialog[dialog-placement="bottom"] { width: auto; max-width: none; }
Dialog[size="fullscreen"] { max-height: none; }
Dialog[dialog-placement="start"], Dialog[dialog-placement="end"] { max-height: none; }
DialogHeader { display: flex; align-items: flex-start; gap: 8px; }
DialogLeading { display: flex; align-items: center; flex-shrink: 0; font-size: var(--nz-dialog-title-font-size); min-height: calc(1em * var(--nz-dialog-title-line-height)); }
DialogHeading { display: flex; flex-direction: column; gap: 2px; flex: 1; min-width: 0; }
DialogTitle { font-size: var(--nz-dialog-title-font-size); line-height: var(--nz-dialog-title-line-height); font-weight: 700; }
DialogSubtitle { color: var(--nz-muted); }
DialogBody { display: flex; flex-direction: column; gap: 8px; flex: 1; min-height: 0; padding: 4px; margin: -4px; overflow-y: auto; }

/* Dialog actions: Help first and pushed to the start, then the others, the primary action last. */
DialogFooter, AlertDialogFooter, DrawerFooter { display: flex; flex-wrap: wrap; justify-content: flex-end; align-items: center; gap: 8px; }
DialogButton { display: flex; order: 1; }
DialogButton[action-role="help"] { order: 0; margin-right: auto; }
DialogButton[action-role="primary"] { order: 2; }
DialogButton > Button { flex: 1; min-width: 72px; }

/* Alert dialog: the severity mark beside the texts, the actions below both. */
AlertDialog { display: grid; grid-template-columns: max-content minmax(0px, 1fr); row-gap: 12px; width: 420px; max-width: 90%; max-height: 90%; padding: 12px; border: 1px solid var(--nz-on-surface); background: var(--nz-surface); color: var(--nz-on-surface); }
AlertDialogSeverity { grid-column: 1; grid-row: 1; display: flex; align-items: center; justify-content: center; width: 24px; height: 24px; margin-right: 12px; padding: 5px; border-radius: 50%; background: var(--nz-accent); color: var(--nz-on-accent); font-weight: 700; stroke-width: 2px; }
AlertDialogSeverity[severity="success"] { background: var(--nz-success); color: #ffffff; }
AlertDialogSeverity[severity="warning"] { background: var(--nz-warning); color: #ffffff; }
AlertDialogSeverity[severity="error"] { background: var(--nz-danger); color: #ffffff; }
AlertDialogContent { grid-column: 2; grid-row: 1; display: flex; flex-direction: column; gap: 8px; min-width: 0; min-height: 0; overflow-y: auto; }
AlertDialogTitle { font-size: 1.17em; font-weight: 700; }
AlertDialogMessage { display: flex; flex-direction: column; gap: 6px; }
AlertDialogDetailsToggle { display: flex; align-items: center; gap: 4px; align-self: flex-start; color: var(--nz-link); cursor: default; }
AlertDialogDetailsToggle:hover { text-decoration-line: underline; }
AlertDialogDetailsToggleChevron { width: 10px; height: 10px; stroke-width: 1.5px; }
AlertDialogDetails { display: block; }
AlertDialogDetailsContent { display: block; max-height: 160px; overflow-y: auto; padding: 6px; border: 1px solid var(--nz-border); background: var(--nz-field); color: var(--nz-on-field); }
AlertDialogDetailsText { display: block; white-space: pre-wrap; font-size: 12px; }
AlertDialogSuppress { display: flex; }
AlertDialogFooter { grid-column: 1 / 3; grid-row: 2; }

/* Drawer: side drawers take their width from the primitives, sheets their height from the content. */
Drawer { display: flex; flex-direction: column; gap: 10px; padding: 12px; border: 1px solid var(--nz-on-surface); background: var(--nz-surface); color: var(--nz-on-surface); }
Drawer[side="top"], Drawer[side="bottom"] { max-height: 80%; }
DrawerHandle { align-self: center; flex-shrink: 0; width: 36px; height: 4px; border-radius: 2px; background: var(--nz-muted); }
DrawerHeader { display: flex; align-items: center; gap: 8px; }
DrawerTitle { flex: 1; min-width: 0; font-size: 1.17em; font-weight: 700; }
DrawerBody { display: flex; flex-direction: column; gap: 8px; flex: 1; min-height: 0; overflow-y: auto; }

/* Command palette: a centered column near the top with the search field above the results. */
CommandPalette { display: flex; flex-direction: column; padding: 0; border: 1px solid var(--nz-on-surface); background: var(--nz-surface); color: var(--nz-on-surface); }
CommandPalette[dialog-placement="top"] { top: 10%; right: auto; left: max(5%, calc(50% - 280px)); width: min(560px, 90%); max-height: 70%; }
CommandPaletteSearch { display: flex; flex-direction: column; flex: 1; min-height: 0; }
CommandPaletteSearchInput { display: block; min-height: 1.4em; padding: 8px 10px; border-bottom: 1px solid var(--nz-border); white-space: pre; overflow-x: hidden; cursor: text; }
CommandPaletteSearchPopup { display: flex; flex-direction: column; min-height: 0; max-height: 360px; overflow-y: auto; padding: 4px; }
CommandPaletteSearchOption { display: flex; flex-direction: column; cursor: default; }
CommandPaletteSearchEmpty { padding: 8px 6px; color: var(--nz-muted); }
CommandPaletteGroupHeader { padding: 6px 6px 2px; color: var(--nz-muted); font-size: 12px; font-weight: 700; }
CommandPaletteItem { display: grid; grid-template-columns: minmax(0px, 1fr) max-content; column-gap: 12px; align-items: center; padding: 4px 6px; }
CommandPaletteSearchOption[current] > CommandPaletteItem { background: var(--nz-selection); color: var(--nz-on-selection); }
CommandPaletteItemLabel { grid-column: 1; grid-row: 1; }
CommandPaletteItemDescription { grid-column: 1; grid-row: 2; color: var(--nz-muted); font-size: 12px; }
CommandPaletteItemShortcut { grid-column: 2; grid-row: 1; }
CommandPaletteSearchOption[current] > CommandPaletteItem > CommandPaletteItemDescription, CommandPaletteSearchOption[current] > CommandPaletteItem > CommandPaletteItemShortcut { color: inherit; }

/* ---- End of group: overlays ---- */

/* ---- Group: Selection (selection) ---- */

/* Radio buttons */
RadioGroup { display: flex; flex-direction: column; gap: 4px; }
RadioGroup[orientation="horizontal"] { flex-direction: row; flex-wrap: wrap; column-gap: 14px; }
RadioButton { display: grid; grid-template-columns: max-content minmax(0px, 1fr); column-gap: 6px; align-items: center; cursor: default; }
RadioButtonMark { display: flex; align-items: center; justify-content: center; width: 13px; height: 13px; border: 1px solid var(--nz-border); border-radius: 50%; background: var(--nz-field); }
RadioButtonDot { width: 5px; height: 5px; border-radius: 50%; background: transparent; }
RadioButton:hover > RadioButtonMark { border-color: var(--nz-on-surface); }
RadioButton:checked > RadioButtonMark { border-color: var(--nz-accent); background: var(--nz-accent); }
RadioButton:checked > RadioButtonMark > RadioButtonDot { background: var(--nz-on-accent); }
RadioButton:disabled > RadioButtonMark { border-color: var(--nz-muted); background: var(--nz-surface); }
RadioButton:disabled:checked > RadioButtonMark > RadioButtonDot { background: var(--nz-muted); }
RadioGroup[invalid] > RadioButton > RadioButtonMark { border-color: var(--nz-danger); }
RadioButtonDescription { grid-column-start: 2; color: var(--nz-muted); font-size: 12px; }
RadioButton[size="small"] { font-size: 12px; }
RadioButton[size="small"] > RadioButtonMark { width: 11px; height: 11px; }
RadioButton[size="large"] { font-size: 16px; }
RadioButton[size="large"] > RadioButtonMark { width: 17px; height: 17px; }
RadioButton[size="large"] > RadioButtonMark > RadioButtonDot { width: 7px; height: 7px; }

/* Switch */
Switch { display: grid; grid-template-columns: max-content minmax(0px, 1fr); column-gap: 8px; align-items: center; cursor: default; }
Switch[label-position="before"] { grid-template-columns: minmax(0px, 1fr) max-content; }
Switch[label-position="before"] > SwitchTrack { grid-column-start: 2; grid-row-start: 1; }
Switch[label-position="before"] > SwitchLabel { grid-column-start: 1; grid-row-start: 1; }
Switch[label-position="before"] > SwitchDescription { grid-column-start: 1; }
SwitchTrack { display: flex; align-items: center; justify-content: flex-start; gap: 3px; min-width: 32px; height: 16px; padding: 0 2px; border: 1px solid var(--nz-border); border-radius: 999px; background: var(--nz-field); color: var(--nz-on-field); }
SwitchThumb { flex-shrink: 0; width: 10px; height: 10px; border-radius: 50%; background: var(--nz-on-field); }
SwitchOnLabel, SwitchOffLabel { font-size: 9px; line-height: 1; white-space: pre; }
SwitchOnLabel { display: none; margin-right: auto; padding-left: 2px; }
SwitchOffLabel { margin-left: auto; padding-right: 2px; }
Switch:hover > SwitchTrack { border-color: var(--nz-on-surface); }
Switch:checked > SwitchTrack { justify-content: flex-start; border-color: var(--nz-accent); background: var(--nz-accent); color: var(--nz-on-accent); }
Switch:checked > SwitchTrack > SwitchThumbRail > SwitchThumbPosition > SwitchThumb { background: var(--nz-on-accent); }
Switch:checked > SwitchTrack > SwitchOnLabel { display: block; }
Switch:checked > SwitchTrack > SwitchOffLabel { display: none; }
Switch:disabled > SwitchTrack { border-color: var(--nz-muted); background: var(--nz-surface); color: var(--nz-muted); }
Switch:disabled > SwitchTrack > SwitchThumbRail > SwitchThumbPosition > SwitchThumb { background: var(--nz-muted); }
Switch[invalid] > SwitchTrack { border-color: var(--nz-danger); }
SwitchDescription { grid-column-start: 2; color: var(--nz-muted); font-size: 12px; }
Switch[size="small"] { font-size: 12px; }
Switch[size="small"] > SwitchTrack { min-width: 26px; height: 13px; }
Switch[size="small"] > SwitchTrack > SwitchThumbRail > SwitchThumbPosition > SwitchThumb { width: 7px; height: 7px; }
Switch[size="large"] { font-size: 16px; }
Switch[size="large"] > SwitchTrack { min-width: 42px; height: 22px; padding: 0 3px; }
Switch[size="large"] > SwitchTrack > SwitchThumbRail > SwitchThumbPosition > SwitchThumb { width: 14px; height: 14px; }
Switch[size="large"] > SwitchTrack > SwitchOnLabel, Switch[size="large"] > SwitchTrack > SwitchOffLabel { font-size: 11px; }

/* Segmented control */
SegmentedControl { position: relative; display: flex; align-items: stretch; border: 1px solid var(--nz-border); background: var(--nz-control); color: var(--nz-on-control); }
SegmentedControl:disabled { border-color: var(--nz-muted); }
Segment { display: flex; align-items: center; justify-content: center; gap: 6px; min-height: 24px; padding: 2px 10px; border-left: 1px solid var(--nz-border); cursor: default; white-space: pre; }
Segment[position="first"], Segment[position="only"] { border-left-width: 0px; }
SegmentedControl[equal-width] > Segment { flex-grow: 1; flex-basis: 0px; }
Segment:hover { background: var(--nz-field); color: var(--nz-on-field); }
Segment[selected] { background: var(--nz-selection); color: var(--nz-on-selection); }
Segment:disabled { background: transparent; color: var(--nz-muted); }
Segment[selected]:disabled { background: var(--nz-muted); color: var(--nz-surface); }
Segment:focus-visible { outline-offset: -3px; }
Segment[icon-only] { padding: 2px 6px; }
SegmentLeading { display: flex; align-items: center; flex-shrink: 0; }
SegmentedControlIndicator { display: none; position: absolute; left: var(--nz-indicator-x, 0px); top: var(--nz-indicator-y, 0px); width: var(--nz-indicator-width, 0px); height: var(--nz-indicator-height, 0px); pointer-events: none; }
Segment[size="small"] { min-height: 18px; padding: 0 6px; font-size: 12px; }
Segment[size="large"] { min-height: 32px; padding: 4px 16px; font-size: 16px; }

/* Slider */
Slider { position: relative; display: block; min-width: 120px; height: 22px; cursor: default; }
Slider[ticks] { height: 30px; }
Slider[tick-labels] { height: 44px; }
Slider[value-label="always"] { margin-top: 16px; }
SliderTrack { position: absolute; left: 7px; right: 7px; top: 8px; height: 6px; border: 1px solid var(--nz-border); background: var(--nz-field); }
SliderFill { position: absolute; left: 0px; top: 0px; bottom: 0px; width: calc(var(--nz-slider-fraction) * 100%); background: var(--nz-accent); }
Slider[reversed] > SliderTrack > SliderFill { left: auto; right: 0px; }
SliderThumb { position: absolute; top: 4px; left: calc(var(--nz-slider-position) * (100% - 14px)); width: 14px; height: 14px; border: 1px solid var(--nz-border); background: var(--nz-control); }
Slider:hover > SliderThumb { border-color: var(--nz-on-surface); }
Slider:disabled > SliderTrack { border-color: var(--nz-muted); }
Slider:disabled > SliderTrack > SliderFill { background: var(--nz-muted); }
Slider:disabled > SliderThumb { border-color: var(--nz-muted); background: var(--nz-surface); }
Slider[invalid] > SliderTrack { border-color: var(--nz-danger); }
SliderTicks { position: absolute; left: 7px; right: 7px; top: 23px; height: 5px; }
SliderTick { position: absolute; top: 0px; left: calc(var(--nz-slider-tick-position) * 100%); width: 1px; height: 5px; background: var(--nz-muted); }
SliderTickLabel { position: absolute; top: 7px; left: -30px; width: 61px; text-align: center; font-size: 11px; color: var(--nz-muted); white-space: pre; }
SliderValueLabel { position: absolute; bottom: 17px; left: -24px; width: 60px; text-align: center; font-size: 11px; white-space: pre; }
Slider[value-label="on-focus"] > SliderThumb > SliderValueLabel { display: none; }
Slider[value-label="on-focus"]:focus > SliderThumb > SliderValueLabel { display: block; }
Slider[size="small"] > SliderTrack { top: 9px; height: 4px; }
Slider[size="large"] > SliderTrack { top: 7px; height: 8px; }
Slider[orientation="vertical"] { min-width: 0px; width: 22px; height: 140px; }
Slider[orientation="vertical"][ticks] { width: 30px; }
Slider[orientation="vertical"][tick-labels] { width: 64px; }
Slider[orientation="vertical"][value-label="always"] { margin-top: 0px; margin-right: 40px; }
Slider[orientation="vertical"] > SliderTrack { left: 8px; right: auto; top: 7px; bottom: 7px; width: 6px; height: auto; }
Slider[orientation="vertical"] > SliderTrack > SliderFill { left: 0px; right: 0px; top: auto; bottom: 0px; width: auto; height: calc(var(--nz-slider-fraction) * 100%); }
Slider[orientation="vertical"][reversed] > SliderTrack > SliderFill { top: 0px; bottom: auto; }
Slider[orientation="vertical"] > SliderThumb { top: auto; left: 4px; bottom: calc(var(--nz-slider-position) * (100% - 14px)); }
Slider[orientation="vertical"] > SliderTicks { left: 23px; right: auto; top: 7px; bottom: 7px; width: 5px; height: auto; }
Slider[orientation="vertical"] > SliderTicks > SliderTick { left: 0px; top: auto; bottom: calc(var(--nz-slider-tick-position) * 100%); width: 5px; height: 1px; }
Slider[orientation="vertical"] > SliderTicks > SliderTick > SliderTickLabel { top: -8px; left: 8px; width: 40px; text-align: left; }
Slider[orientation="vertical"] > SliderThumb > SliderValueLabel { bottom: auto; top: -2px; left: 18px; width: 40px; text-align: left; }

/* Range slider */
RangeSlider { position: relative; display: block; min-width: 120px; height: 22px; cursor: default; }
RangeSlider[ticks] { height: 30px; }
RangeSlider[tick-labels] { height: 44px; }
RangeSlider[value-label="always"] { margin-top: 16px; }
RangeSliderRail { position: absolute; left: 7px; right: 7px; top: 0px; height: 22px; }
RangeSliderTrack { position: absolute; left: 0px; right: 0px; top: 8px; height: 6px; border: 1px solid var(--nz-border); background: var(--nz-field); }
RangeSliderFill { position: absolute; top: 0px; bottom: 0px; left: calc(var(--nz-range-start) * 100%); width: calc((var(--nz-range-end) - var(--nz-range-start)) * 100%); background: var(--nz-accent); }
RangeSliderHandle { position: absolute; top: 0px; bottom: 0px; }
RangeSliderHandle[on-top] { z-index: 1; }
RangeSliderHandle:focus-visible { outline-style: none; }
RangeSliderHandleTrack { position: absolute; top: 0px; bottom: 0px; pointer-events: none; }
RangeSliderThumb { position: absolute; top: 4px; left: calc(var(--nz-range-thumb-position) * 100% - 7px); width: 14px; height: 14px; border: 1px solid var(--nz-border); background: var(--nz-control); }
RangeSliderHandle:hover > RangeSliderThumb { border-color: var(--nz-on-surface); }
RangeSliderHandle:focus-visible > RangeSliderThumb { outline: 1px solid CanvasText; outline-offset: 2px; }
RangeSlider:disabled > RangeSliderRail > RangeSliderTrack { border-color: var(--nz-muted); }
RangeSlider:disabled > RangeSliderRail > RangeSliderTrack > RangeSliderFill { background: var(--nz-muted); }
RangeSlider:disabled > RangeSliderRail > RangeSliderHandle > RangeSliderThumb { border-color: var(--nz-muted); background: var(--nz-surface); }
RangeSliderHandle[invalid] > RangeSliderThumb { border-color: var(--nz-danger); }
RangeSliderTicks { position: absolute; left: 7px; right: 7px; top: 23px; height: 5px; }
RangeSliderTick { position: absolute; top: 0px; left: calc(var(--nz-slider-tick-position) * 100%); width: 1px; height: 5px; background: var(--nz-muted); }
RangeSliderTickLabel { position: absolute; top: 7px; left: -30px; width: 61px; text-align: center; font-size: 11px; color: var(--nz-muted); white-space: pre; }
RangeSliderValueLabel { position: absolute; bottom: 17px; left: -24px; width: 60px; text-align: center; font-size: 11px; white-space: pre; }
RangeSlider[value-label="on-focus"] > RangeSliderRail > RangeSliderHandle > RangeSliderThumb > RangeSliderValueLabel { display: none; }
RangeSlider[value-label="on-focus"] > RangeSliderRail > RangeSliderHandle:focus > RangeSliderThumb > RangeSliderValueLabel { display: block; }
RangeSlider[orientation="vertical"] { min-width: 0px; width: 22px; height: 140px; }
RangeSlider[orientation="vertical"][ticks] { width: 30px; }
RangeSlider[orientation="vertical"][tick-labels] { width: 64px; }
RangeSlider[orientation="vertical"][value-label="always"] { margin-top: 0px; margin-right: 40px; }
RangeSlider[orientation="vertical"] > RangeSliderRail { left: 0px; right: auto; top: 7px; bottom: 7px; width: 22px; height: auto; }
RangeSlider[orientation="vertical"] > RangeSliderRail > RangeSliderTrack { left: 8px; right: auto; top: 0px; bottom: 0px; width: 6px; height: auto; }
RangeSlider[orientation="vertical"] > RangeSliderRail > RangeSliderTrack > RangeSliderFill { left: 0px; right: 0px; top: auto; width: auto; bottom: calc(var(--nz-range-start) * 100%); height: calc((var(--nz-range-end) - var(--nz-range-start)) * 100%); }
RangeSlider[orientation="vertical"] > RangeSliderRail > RangeSliderHandle { top: auto; left: 0px; right: 0px; }
RangeSlider[orientation="vertical"] > RangeSliderRail > RangeSliderHandle > RangeSliderHandleTrack { top: auto; left: 0px; right: 0px; }
RangeSlider[orientation="vertical"] > RangeSliderRail > RangeSliderHandle > RangeSliderThumb { top: auto; left: 4px; bottom: calc(var(--nz-range-thumb-position) * 100% - 7px); }
RangeSlider[orientation="vertical"] > RangeSliderTicks { left: 23px; right: auto; top: 7px; bottom: 7px; width: 5px; height: auto; }
RangeSlider[orientation="vertical"] > RangeSliderTicks > RangeSliderTick { left: 0px; top: auto; bottom: calc(var(--nz-slider-tick-position) * 100%); width: 5px; height: 1px; }
RangeSlider[orientation="vertical"] > RangeSliderTicks > RangeSliderTick > RangeSliderTickLabel { top: -8px; left: 8px; width: 40px; text-align: left; }
RangeSlider[orientation="vertical"] > RangeSliderRail > RangeSliderHandle > RangeSliderThumb > RangeSliderValueLabel { bottom: auto; top: -2px; left: 18px; width: 40px; text-align: left; }

/* Chips */
ChipGroup { display: flex; flex-wrap: wrap; align-items: center; gap: 6px; }
Chip, ChipGroupOverflow { display: flex; align-items: center; gap: 4px; min-height: 22px; padding: 0 8px; border: 1px solid var(--nz-border); border-radius: 999px; background: var(--nz-control); color: var(--nz-on-control); cursor: default; white-space: pre; }
Chip:hover, ChipGroupOverflow:hover { border-color: var(--nz-on-surface); }
Chip:active, ChipGroupOverflow:active { background: var(--nz-field); }
Chip[selected] { border-color: var(--nz-selection); background: var(--nz-selection); color: var(--nz-on-selection); }
Chip:disabled, ChipGroupOverflow:disabled { border-color: var(--nz-muted); background: var(--nz-surface); color: var(--nz-muted); }
Chip[hidden] { display: none; }
Chip[removable] { padding-right: 3px; }
Chip[intent="accent"] { border-color: var(--nz-accent); }
Chip[intent="success"] { border-color: var(--nz-success); }
Chip[intent="warning"] { border-color: var(--nz-warning); }
Chip[intent="danger"] { border-color: var(--nz-danger); }
ChipGroupOverflow { color: var(--nz-link); }
ChipDot { flex-shrink: 0; width: 8px; height: 8px; border-radius: 50%; background: var(--nz-muted); }
Chip[intent="accent"] > ChipDot { background: var(--nz-accent); }
Chip[intent="success"] > ChipDot { background: var(--nz-success); }
Chip[intent="warning"] > ChipDot { background: var(--nz-warning); }
Chip[intent="danger"] > ChipDot { background: var(--nz-danger); }
ChipLeading { display: flex; align-items: center; flex-shrink: 0; }
ChipCheck { flex-shrink: 0; width: 10px; height: 10px; stroke-width: 2px; }
ChipRemove { display: flex; align-items: center; justify-content: center; flex-shrink: 0; width: 16px; height: 16px; border-radius: 50%; color: var(--nz-muted); }
ChipRemove:hover { background: var(--nz-field); color: var(--nz-on-field); }
Chip[selected] > ChipRemove { color: inherit; }
Chip[size="small"], ChipGroupOverflow[size="small"] { min-height: 18px; padding: 0 6px; font-size: 12px; }
Chip[size="large"], ChipGroupOverflow[size="large"] { min-height: 30px; padding: 0 12px; font-size: 16px; }

/* Rating */
Rating { display: flex; align-items: center; gap: 2px; font-size: 18px; line-height: 1; cursor: default; }
RatingItem { position: relative; display: block; }
RatingItemSymbol { display: block; color: var(--nz-muted); white-space: pre; }
RatingItemFill { position: absolute; left: 0px; top: 0px; bottom: 0px; width: 0px; overflow-x: hidden; overflow-y: hidden; white-space: pre; color: var(--nz-accent); }
RatingItem[fill="full"] > RatingItemFill { width: 100%; }
RatingItem[fill="half"] > RatingItemFill { width: 50%; }
Rating[previewing] > RatingItem > RatingItemFill { width: 0px; color: var(--nz-on-surface); }
Rating[previewing] > RatingItem[preview="full"] > RatingItemFill { width: 100%; }
Rating[previewing] > RatingItem[preview="half"] > RatingItemFill { width: 50%; }
RatingItemZone { position: absolute; top: 0px; bottom: 0px; width: 50%; }
RatingItemZone[half="start"] { left: 0px; }
RatingItemZone[half="end"] { left: 50%; }
Rating:disabled > RatingItem > RatingItemFill { color: var(--nz-muted); }
Rating[invalid] > RatingItem > RatingItemSymbol { color: var(--nz-danger); }
Rating[size="small"] { font-size: 14px; }
Rating[size="large"] { font-size: 26px; }

/* Dial */
Dial { position: relative; display: block; flex-shrink: 0; width: 64px; height: 64px; cursor: default; }
DialTrack, DialFill { position: absolute; left: 4px; top: 4px; right: 4px; bottom: 4px; stroke-width: 4px; transform: rotate(var(--nz-dial-start-angle)); }
DialTrack { color: var(--nz-border); }
DialFill { color: var(--nz-accent); }
DialTicks { position: absolute; left: 0px; top: 0px; right: 0px; bottom: 0px; }
DialTick { position: absolute; left: calc(50% - 0.5px); top: 0px; width: 1px; height: 50%; border-top: 3px solid var(--nz-muted); transform-origin: 50% 100%; transform: rotate(var(--nz-dial-tick-angle)); }
DialPointer { position: absolute; left: 12px; top: 12px; right: 12px; bottom: 12px; border: 1px solid var(--nz-border); border-radius: 50%; background: var(--nz-control); transform: rotate(var(--nz-dial-angle)); }
DialPointerHandle { position: absolute; left: calc(50% - 2px); top: 2px; width: 4px; height: 8px; background: var(--nz-on-control); }
DialValue { position: absolute; left: 0px; right: 0px; top: 25px; height: 14px; text-align: center; font-size: 11px; line-height: 14px; white-space: pre; pointer-events: none; }
Dial:hover > DialPointer { border-color: var(--nz-accent); }
Dial:focus-visible { outline-style: none; }
Dial:focus-visible > DialPointer { outline: 1px solid var(--nz-accent); outline-offset: 2px; }
Dial[dragging] > DialPointer { border-color: var(--nz-accent); }
Dial:disabled > DialFill { color: var(--nz-muted); }
Dial:disabled > DialPointer { border-color: var(--nz-muted); background: var(--nz-surface); }
Dial:disabled > DialPointer > DialPointerHandle { background: var(--nz-muted); }
Dial[invalid] > DialTrack { color: var(--nz-danger); }
Dial[size="small"] { width: 48px; height: 48px; }
Dial[size="small"] > DialPointer { left: 10px; top: 10px; right: 10px; bottom: 10px; }
Dial[size="small"] > DialValue { top: 17px; font-size: 10px; }
Dial[size="large"] { width: 88px; height: 88px; }
Dial[size="large"] > DialPointer { left: 14px; top: 14px; right: 14px; bottom: 14px; }
Dial[size="large"] > DialValue { top: 37px; font-size: 13px; }

Switch { --nz-switch-inset: 2px; --nz-switch-thumb-width: 10px; --nz-switch-thumb-height: 10px; }
Switch[size="small"] { --nz-switch-thumb-width: 7px; --nz-switch-thumb-height: 7px; }
Switch[size="large"] { --nz-switch-inset: 3px; --nz-switch-thumb-width: 14px; --nz-switch-thumb-height: 14px; }
SwitchTrack { position: relative; }
SwitchThumbRail { position: absolute; left: var(--nz-switch-inset); right: var(--nz-switch-inset); top: 0; bottom: 0; display: flex; align-items: center; }
SwitchThumbPosition { position: relative; flex-shrink: 0; width: var(--nz-switch-thumb-width); height: var(--nz-switch-thumb-height); left: calc((100% - var(--nz-switch-thumb-width)) * var(--nz-switch-position)); transition: left 160ms ease-out; }
Switch[dragging] > SwitchTrack > SwitchThumbRail > SwitchThumbPosition { transition: none; }
Switch[dragging]:not([drag-on]) > SwitchTrack { border-color: var(--nz-border); background: var(--nz-field); color: var(--nz-on-field); }
Switch[dragging][drag-on] > SwitchTrack { border-color: var(--nz-accent); background: var(--nz-accent); color: var(--nz-on-accent); }
Switch[dragging]:not([drag-on]) > SwitchTrack > SwitchThumbRail > SwitchThumbPosition > SwitchThumb { background: var(--nz-on-field); }
Switch[dragging][drag-on] > SwitchTrack > SwitchThumbRail > SwitchThumbPosition > SwitchThumb { background: var(--nz-on-accent); }

/* ---- End of group: selection ---- */

/* ---- Group: Fields (fields) ---- */



PasswordField, NumberField, SearchField, MaskedField, FileField, ShortcutField, ComboBox, TokenField {
    display: flex; align-items: center; gap: 4px; min-width: 10em; min-height: 26px;
    padding: 1px 4px; border: 1px solid var(--nz-border); background: var(--nz-field); color: var(--nz-on-field);
}
PasswordField:focus-within, NumberField:focus-within, SearchField:focus-within, MaskedField:focus-within,
FileField:focus-within, ShortcutField:focus-within, ComboBox:focus-within, TokenField:focus-within {
    border-color: var(--nz-accent);
}
PasswordField[invalid], NumberField[invalid], SearchField[invalid], MaskedField[invalid] { border-color: var(--nz-danger); }
PasswordFieldInput, NumberFieldInput, SearchFieldInput, MaskedFieldInput, ComboBoxInput, TokenFieldInput {
    display: block; flex: 1; min-width: 0; white-space: pre; overflow-x: hidden; cursor: text;
}
PasswordFieldReveal, NumberFieldSpinButtons, SearchFieldClear, FileFieldBrowse, FileFieldClear,
ShortcutFieldClear, TokenFieldTokenRemove { flex-shrink: 0; }
PasswordFieldReveal, SearchFieldMark, SearchFieldClear, FileFieldClear, ShortcutFieldClear,
ComboBoxChevron, TokenFieldTokenRemove { width: 12px; height: 12px; stroke-width: 1.5px; }
ComboBoxLeading, ComboBoxTrailing { display: flex; align-items: center; flex-shrink: 0; }
SearchFieldLeading, SearchFieldTrailing { display: flex; align-items: center; flex-shrink: 0; }
SearchFieldTrailing { order: 2; }
SearchFieldMark { position: relative; flex-shrink: 0; order: 0; width: 16px; height: 16px; padding: 0 5px 5px 0; }
SearchFieldMark::after { content: ""; position: absolute; left: 10px; top: 10px; width: 6px; height: 1.5px; background: currentColor; transform-origin: 0 50%; transform: rotate(45deg); }
SearchField[search-icon-placement="end"] > SearchFieldMark { order: 3; }
SearchField[search-icon-placement="start"] > SearchFieldMark { order: 0; }
ComboBoxPopup { display: flex; flex-direction: column; overflow-y: auto; padding: 2px; border: 1px solid var(--nz-on-surface); background: var(--nz-surface); color: var(--nz-on-surface); }
ComboBoxOption { display: flex; align-items: center; gap: 6px; padding: 3px 6px; }
ComboBoxOption:hover, ComboBoxOption[current] { background: var(--nz-selection); color: var(--nz-on-selection); }
ComboBoxEmpty { padding: 3px 6px; color: var(--nz-muted); }
NumberFieldSpinButtons { display: flex; flex-direction: column; }
NumberFieldIncrement, NumberFieldDecrement { display: flex; align-items: center; justify-content: center; flex-shrink: 0; width: 16px; height: 12px; padding: 2px; stroke-width: 1.25px; border: 1px solid transparent; }
NumberField[spin-buttons="inline"] > NumberFieldIncrement, NumberField[spin-buttons="inline"] > NumberFieldDecrement { width: 24px; height: 24px; padding: 5px; }
NumberFieldIncrement:hover, NumberFieldDecrement:hover { background: var(--nz-control); }
NumberFieldIncrement:active, NumberFieldDecrement:active { background: var(--nz-selection); color: var(--nz-on-selection); }
NumberFieldIncrement:disabled, NumberFieldDecrement:disabled { color: var(--nz-muted); background: transparent; }
NumberFieldSpinToggle, NumberFieldSpinPopup { display: none; }
NumberFieldUnit, MaskedFieldPrompt, ShortcutFieldPrompt { color: var(--nz-muted); }
PinField, TokenFieldTokens { display: flex; align-items: center; gap: 4px; }
PinFieldCell { width: 2em; min-height: 26px; line-height: max(24px, 1.5em); text-align: center; border: 1px solid var(--nz-border); background: var(--nz-field); }
PinFieldCell:focus { border-color: var(--nz-accent); }
TokenFieldTokens { flex-wrap: wrap; }
TokenFieldToken { display: flex; align-items: center; gap: 4px; padding: 2px 4px; background: var(--nz-control); }

FlipView { position: relative; display: block; min-width: 160px; width: 320px; height: 220px; }
FlipViewViewport { width: 100%; height: 100%; overflow-x: hidden; overflow-y: hidden; }
FlipViewItem { display: flex; align-items: center; justify-content: center; width: 100%; height: 100%; min-width: 0; min-height: 0; }
FlipViewPrevious, FlipViewNext { position: absolute; top: calc(50% - 16px); display: flex; width: 26px; height: 32px; padding: 7px; border: 1px solid var(--nz-border); background: var(--nz-control); color: var(--nz-on-control); stroke-width: 1.5px; }
FlipViewPrevious { left: 4px; }
FlipViewNext { right: 4px; }
FlipViewPrevious:hover, FlipViewNext:hover { background: var(--nz-selection); color: var(--nz-on-selection); }
FlipViewPrevious:disabled, FlipViewNext:disabled { display: none; }
FlipViewStatus { position: absolute; width: 1px; height: 1px; overflow-x: hidden; overflow-y: hidden; clip-path: inset(50%); }

/* ---- End of group: fields ---- */

/* ---- Group: Pickers (pickers) ---- */

/* Calendar: a header with arrows and the zoom title, then one or two month grids of fixed-size day cells. */
CalendarPrevious { order: 0; }
CalendarTitle { order: 1; }
CalendarNext { order: 2; }
CalendarWeekdayAbbreviated { display: block; }
CalendarWeekdayShortest { display: none; }
Calendar { display: flex; flex-direction: column; gap: 4px; color: var(--nz-on-surface); }
CalendarHeader { display: flex; align-items: center; gap: 4px; }
CalendarPrevious, CalendarNext { width: 24px; height: 24px; padding: 6px; flex-shrink: 0; stroke-width: 1.5px; border: 1px solid transparent; }
CalendarPrevious:hover, CalendarNext:hover, CalendarTitle:hover { border-color: var(--nz-border); }
CalendarPrevious:disabled, CalendarNext:disabled { color: var(--nz-muted); border-color: transparent; }
CalendarTitle { flex: 1; padding: 2px 6px; text-align: center; font-weight: 700; border: 1px solid transparent; }
CalendarTitle:disabled { color: var(--nz-on-surface); border-color: transparent; }
CalendarMonths { display: flex; gap: 16px; }
CalendarMonth { display: flex; flex-direction: column; gap: 2px; }
CalendarMonthTitle { padding: 2px 0; text-align: center; font-weight: 700; }
CalendarWeekdays, CalendarWeek, CalendarRow { display: flex; }
CalendarWeekday, CalendarWeekHeader { width: 28px; text-align: center; color: var(--nz-muted); font-size: 12px; }
CalendarGrid { display: flex; flex-direction: column; }
CalendarWeekNumber { width: 28px; height: 28px; display: flex; align-items: center; justify-content: center; color: var(--nz-muted); font-size: 12px; }
CalendarDay { width: 28px; height: 28px; display: flex; align-items: center; justify-content: center; border: 1px solid transparent; cursor: default; }
CalendarDay:hover, CalendarItem:hover { border-color: var(--nz-border); }
CalendarDay[outside] { color: var(--nz-muted); }
CalendarDay[empty] { border-color: transparent; }
CalendarDay[today], CalendarItem[today] { font-weight: 700; border-color: var(--nz-accent); }
CalendarDay[range-preview] { background: var(--nz-control); }
CalendarDay[range-middle] { background: var(--nz-control); color: var(--nz-on-control); }
CalendarDay[selected], CalendarDay[range-start], CalendarDay[range-end], CalendarItem[selected] { background: var(--nz-selection); color: var(--nz-on-selection); }
CalendarDay[range-middle] { background: var(--nz-control); color: var(--nz-on-control); }
CalendarDay[unavailable], CalendarItem[unavailable] { color: var(--nz-muted); text-decoration-line: line-through; }
CalendarDay:focus-visible, CalendarItem:focus-visible { outline-offset: -2px; }
CalendarItem { width: 52px; height: 40px; display: flex; align-items: center; justify-content: center; border: 1px solid transparent; }
CalendarItem[outside] { color: var(--nz-muted); }
Calendar:disabled CalendarDay, Calendar:disabled CalendarItem { color: var(--nz-muted); background: transparent; }

/* Date, time and date-time fields share the frame of TextField. */
DatePicker, TimePicker, DateRangePicker, DateTimePicker { display: flex; align-items: center; gap: 4px; min-width: 10em; min-height: 26px; padding: 1px 4px; border: 1px solid var(--nz-border); background: var(--nz-field); color: var(--nz-on-field); }
DatePicker:focus-within, TimePicker:focus-within, DateRangePicker:focus-within, DateTimePicker:focus-within, DatePicker[expanded], TimePicker[expanded], DateRangePicker[expanded] { border-color: var(--nz-accent); }
DatePicker[invalid], TimePicker[invalid], DateRangePicker[invalid], DateTimePicker[invalid] { border-color: var(--nz-danger); }
DatePicker:disabled, TimePicker:disabled, DateRangePicker:disabled, DateTimePicker:disabled { border-color: var(--nz-muted); color: var(--nz-muted); background: var(--nz-surface); }
DatePicker[appearance="filled"], TimePicker[appearance="filled"], DateRangePicker[appearance="filled"], DateTimePicker[appearance="filled"] { background: var(--nz-control); border-color: transparent; }
DatePicker[appearance="underline"], TimePicker[appearance="underline"], DateRangePicker[appearance="underline"], DateTimePicker[appearance="underline"] { border-color: transparent; border-bottom-color: var(--nz-border); background: transparent; }
DatePicker[appearance="plain"], TimePicker[appearance="plain"], DateRangePicker[appearance="plain"], DateTimePicker[appearance="plain"] { border-color: transparent; background: transparent; padding: 0; min-width: 0; }
DatePicker[size="small"], TimePicker[size="small"], DateRangePicker[size="small"], DateTimePicker[size="small"] { min-height: 20px; font-size: 12px; }
DatePicker[size="large"], TimePicker[size="large"], DateRangePicker[size="large"], DateTimePicker[size="large"] { min-height: 34px; font-size: 16px; }
DatePickerInput, TimePickerInput, DateRangePickerStart, DateRangePickerEnd { display: block; flex: 1; min-width: 5em; white-space: pre; overflow-x: hidden; cursor: text; }
TextFieldInput:focus-visible, TextAreaInput:focus-visible, PasswordFieldInput:focus-visible, NumberFieldInput:focus-visible,
SearchFieldInput:focus-visible, MaskedFieldInput:focus-visible, ComboBoxInput:focus-visible, ComboboxInput:focus-visible,
TokenFieldInput:focus-visible, DatePickerInput:focus-visible, TimePickerInput:focus-visible { outline-style: none; }
:has(> TextFieldInput:focus-visible), :has(> TextAreaInput:focus-visible), :has(> PasswordFieldInput:focus-visible),
:has(> NumberFieldInput:focus-visible), :has(> SearchFieldInput:focus-visible), :has(> MaskedFieldInput:focus-visible),
:has(> ComboBoxInput:focus-visible), :has(> ComboboxInput:focus-visible), :has(> TokenFieldInput:focus-visible),
:has(> DatePickerInput:focus-visible), :has(> TimePickerInput:focus-visible), DateTimePicker:has(:focus-visible) {
    outline: 1px solid var(--nz-accent); outline-offset: 2px;
}
DateTimePicker > DatePicker:has(:focus-visible), DateTimePicker > TimePicker:has(:focus-visible) { outline-style: none; }
DatePickerInput[invalid], TimePickerInput[invalid], DateRangePickerStart[invalid], DateRangePickerEnd[invalid] { color: var(--nz-danger); }
DateRangePickerSeparator { flex-shrink: 0; color: var(--nz-muted); }
DatePickerClear, TimePickerClear, DateRangePickerClear { width: 10px; height: 10px; flex-shrink: 0; stroke-width: 1.5px; color: var(--nz-muted); }
DatePickerClear:hover, TimePickerClear:hover, DateRangePickerClear:hover { color: var(--nz-on-field); }
DatePickerButton, TimePickerButton, DateRangePickerButton { width: 18px; height: 18px; padding: 4px; flex-shrink: 0; stroke-width: 1.5px; border: 1px solid transparent; }
DatePickerButton:hover, TimePickerButton:hover, DateRangePickerButton:hover, DatePickerButton[expanded], TimePickerButton[expanded], DateRangePickerButton[expanded] { border-color: var(--nz-border); }
DateTimePicker > DatePicker { flex: 3; }
DateTimePicker > TimePicker { flex: 2; }
DatePickerPopup, TimePickerPopup, DateRangePickerPopup { display: flex; flex-direction: column; gap: 6px; padding: 6px; border: 1px solid var(--nz-on-surface); background: var(--nz-surface); color: var(--nz-on-surface); }
DateRangePickerPopup { flex-direction: row; align-items: flex-start; }
DatePickerFooter { display: flex; justify-content: center; border-top: 1px solid var(--nz-border); padding-top: 4px; }
DatePickerToday, DateRangePickerPreset { padding: 2px 8px; border: 1px solid transparent; color: var(--nz-link); }
DatePickerToday:hover, DateRangePickerPreset:hover { border-color: var(--nz-border); }
DatePickerToday:disabled { color: var(--nz-muted); }
DateRangePickerPresets { display: flex; flex-direction: column; min-width: 120px; padding-right: 6px; border-right: 1px solid var(--nz-border); }
DateRangePickerPreset { color: var(--nz-on-surface); }
DateRangePickerPreset[selected] { background: var(--nz-selection); color: var(--nz-on-selection); }
TimePickerColumns { display: flex; gap: 2px; }
TimePickerColumn { display: flex; flex-direction: column; width: 48px; max-height: 200px; overflow-y: auto; border: 1px solid var(--nz-border); background: var(--nz-field); color: var(--nz-on-field); }
TimePickerList { display: flex; flex-direction: column; min-width: 120px; max-height: 240px; overflow-y: auto; border: 1px solid var(--nz-border); background: var(--nz-field); color: var(--nz-on-field); }
TimePickerOption { padding: 2px 6px; text-align: center; flex-shrink: 0; }
TimePickerOption:hover { background: var(--nz-control); color: var(--nz-on-control); }
TimePickerOption[selected] { background: var(--nz-selection); color: var(--nz-on-selection); }
TimePickerOption:disabled { color: var(--nz-muted); background: transparent; }

/* ---- End of group: pickers ---- */

/* ---- Group: Collections (collections) ---- */
ListView, TreeView, GridView { display: flex; flex-direction: column; min-height: 0; height: 240px; border: 1px solid var(--nz-border); background: var(--nz-field); color: var(--nz-on-field); }
GridView { height: 320px; }
ListView:disabled, TreeView:disabled, GridView:disabled { color: var(--nz-muted); border-color: var(--nz-muted); }
ListView:focus-visible, TreeView:focus-visible, GridView:focus-visible { outline-offset: -2px; }
ListViewViewport, TreeViewViewport { display: flex; flex-direction: column; flex: 1; min-height: 0; overflow-y: auto; }
ListViewViewportItem, TreeViewViewportItem { display: flex; flex-direction: column; flex-shrink: 0; }
ListViewViewportSpacer, TreeViewViewportSpacer { flex-shrink: 0; }
ListViewItem { display: flex; align-items: center; gap: 6px; flex: 1; min-height: 0; padding: 0 6px; cursor: default; }
ListViewItem:hover, TreeViewItem:hover, GridViewItem:hover { background: var(--nz-control); color: var(--nz-on-control); }
ListViewItem[selected], TreeViewItem[selected], GridViewItem[selected] { background: var(--nz-selection); color: var(--nz-on-selection); }
ListViewItem[current], TreeViewItem[current], GridViewItem[current] { outline: 1px dotted var(--nz-muted); outline-offset: -2px; }
ListViewItem:focus-visible, TreeViewItem:focus-visible, GridViewItem:focus-visible { outline: 1px solid var(--nz-on-field); outline-offset: -2px; }
ListViewItem[selected]:focus-visible, TreeViewItem[selected]:focus-visible, GridViewItem[selected]:focus-visible { outline-color: var(--nz-on-selection); }
ListViewItem:disabled, TreeViewItem:disabled, GridViewItem:disabled { color: var(--nz-muted); background: transparent; }
ListViewItemContent, TreeViewItemContent { flex: 1; min-width: 0; overflow-x: hidden; white-space: pre; text-overflow: ellipsis; }
ListViewItemCheck, TreeViewItemCheck, GridViewItemCheck { width: 13px; height: 13px; flex-shrink: 0; border: 1px solid var(--nz-border); background: var(--nz-field); color: var(--nz-on-field); stroke-width: 2px; }
ListViewItemCheck[checked], TreeViewItemCheck[checked], TreeViewItemCheck[indeterminate], GridViewItemCheck[checked] { background: var(--nz-accent); color: var(--nz-on-accent); border-color: var(--nz-accent); }
ListViewSectionHeader { display: flex; align-items: flex-end; flex: 1; min-height: 0; padding: 0 6px 2px; border-bottom: 1px solid var(--nz-border); color: var(--nz-muted); font-size: 12px; font-weight: 700; }
ListViewEmpty, TreeViewEmpty, GridViewEmpty { display: flex; align-items: center; justify-content: center; flex: 1; padding: 12px; color: var(--nz-muted); }
ListViewLoadingMore { flex-shrink: 0; padding: 4px 6px; border-top: 1px solid var(--nz-border); color: var(--nz-muted); }
TreeViewItem { display: flex; align-items: center; gap: 4px; flex: 1; min-height: 0; position: relative; padding-left: calc(var(--nz-tree-level, 0) * var(--nz-tree-indent, 16px) + 4px); padding-right: 6px; cursor: default; }
TreeViewExpander { width: 12px; height: 12px; flex-shrink: 0; stroke-width: 1.5px; }
TreeViewIndentGuide { position: absolute; top: 0; bottom: 0; width: 1px; left: calc(var(--nz-tree-guide, 0) * var(--nz-tree-indent, 16px) + 10px); background: var(--nz-border); }
TreeViewLoading { flex-shrink: 0; color: var(--nz-muted); font-size: 12px; }
TreeViewItem[selected] > TreeViewLoading { color: var(--nz-on-selection); }
GridViewViewport { display: grid; flex: 1; min-height: 0; overflow-y: auto; align-content: start; }
GridViewViewportSpacer { grid-column: 1 / -1; }
GridViewViewportItem { display: flex; flex-direction: column; min-width: 0; }
GridViewItem { display: flex; flex-direction: column; flex: 1; min-height: 0; margin: 3px; padding: 4px; position: relative; border: 1px solid transparent; cursor: default; }
GridViewItemCheck { position: absolute; top: 4px; left: 4px; }
GridViewItemContent { display: flex; flex-direction: column; align-items: center; justify-content: center; flex: 1; min-height: 0; overflow: hidden; text-align: center; }

ActionRow { display: flex; align-items: center; gap: 10px; min-height: 40px; padding: 6px 10px; background: var(--nz-surface); color: var(--nz-on-surface); cursor: default; }
ActionRow[clickable]:hover { background: var(--nz-control); color: var(--nz-on-control); }
ActionRow[clickable]:active { background: var(--nz-selection); color: var(--nz-on-selection); }
ActionRow:focus-visible { outline-offset: -2px; }
ActionRow:disabled { color: var(--nz-muted); }
ActionRow[position="middle"], ActionRow[position="last"] { border-top: 1px solid var(--nz-border); }
ActionRowLeading, ActionRowTrailing { display: flex; align-items: center; gap: 6px; flex-shrink: 0; }
ActionRowText { display: flex; flex-direction: column; flex: 1; min-width: 0; }
ActionRowTitle { display: block; }
ActionRowSubtitle { display: block; color: var(--nz-muted); font-size: 12px; }
ActionRowChevron { width: 10px; height: 10px; flex-shrink: 0; stroke-width: 1.5px; color: var(--nz-muted); }
RowGroup { display: flex; flex-direction: column; gap: 6px; }
RowGroup:disabled { color: var(--nz-muted); }
RowGroupHeader { display: flex; align-items: flex-end; gap: 8px; }
RowGroupHeading { display: flex; flex-direction: column; flex: 1; min-width: 0; }
RowGroupTitle { display: block; font-weight: 700; }
RowGroupDescription { display: block; color: var(--nz-muted); font-size: 12px; }
RowGroupActions { display: flex; align-items: center; gap: 6px; flex-shrink: 0; }
RowGroupList { display: flex; flex-direction: column; border: 1px solid var(--nz-border); background: var(--nz-surface); }

Table { display: block; color: var(--nz-on-surface); }
TableCaption { padding: 0 0 4px; font-weight: 700; text-align: left; }
TableCell, TableHeaderCell { padding: 4px 8px; border-bottom: 1px solid var(--nz-border); text-align: left; }
TableHeaderCell { font-weight: 700; }
TableHeader > TableRow > TableHeaderCell, TableHeader > TableRow > TableCell { border-bottom-color: var(--nz-on-surface); }
TableFooter > TableRow > TableCell, TableFooter > TableRow > TableHeaderCell { font-weight: 700; border-bottom-color: transparent; border-top: 1px solid var(--nz-on-surface); }
Table[dense] TableCell, Table[dense] TableHeaderCell { padding: 1px 4px; }
Table[bordered] { border-top: 1px solid var(--nz-border); border-left: 1px solid var(--nz-border); }
Table[bordered] TableCell, Table[bordered] TableHeaderCell { border-right: 1px solid var(--nz-border); border-bottom: 1px solid var(--nz-border); }
Table[striped] TableBody > TableRow[alternate] > TableCell, Table[striped] TableBody > TableRow[alternate] > TableHeaderCell { background: var(--nz-control); }

Pagination { display: flex; align-items: center; flex-wrap: wrap; gap: 4px; }
PaginationFirst, PaginationPrevious, PaginationPage, PaginationNext, PaginationLast { display: flex; align-items: center; justify-content: center; min-width: 26px; min-height: 26px; padding: 0 6px; border: 1px solid var(--nz-border); background: var(--nz-control); color: var(--nz-on-control); cursor: default; }
PaginationFirst:hover, PaginationPrevious:hover, PaginationPage:hover, PaginationNext:hover, PaginationLast:hover { border-color: var(--nz-on-surface); }
PaginationFirst:active, PaginationPrevious:active, PaginationPage:active, PaginationNext:active, PaginationLast:active { background: var(--nz-selection); color: var(--nz-on-selection); }
PaginationPage[current] { background: var(--nz-accent); color: var(--nz-on-accent); border-color: var(--nz-accent); }
PaginationFirst:disabled, PaginationPrevious:disabled, PaginationNext:disabled, PaginationLast:disabled { color: var(--nz-muted); border-color: var(--nz-muted); }
Pagination:disabled > PaginationPage { color: var(--nz-muted); border-color: var(--nz-muted); }
Pagination:disabled > PaginationPage[current] { background: var(--nz-control); }
PaginationGlyph { width: 8px; height: 10px; flex-shrink: 0; stroke-width: 1.5px; }
PaginationEllipsis { display: flex; align-items: center; justify-content: center; min-width: 20px; color: var(--nz-muted); }
PaginationSummary { padding: 0 6px; color: var(--nz-muted); }
PaginationPageSize { display: flex; align-items: center; gap: 6px; margin-left: 8px; }
PaginationPageSizeLabel { color: var(--nz-muted); }
PaginationPageSize > Select { min-width: 5em; }
Pagination[size="small"] > PaginationFirst, Pagination[size="small"] > PaginationPrevious, Pagination[size="small"] > PaginationPage, Pagination[size="small"] > PaginationNext, Pagination[size="small"] > PaginationLast { min-width: 20px; min-height: 20px; padding: 0 4px; font-size: 12px; }
Pagination[size="large"] > PaginationFirst, Pagination[size="large"] > PaginationPrevious, Pagination[size="large"] > PaginationPage, Pagination[size="large"] > PaginationNext, Pagination[size="large"] > PaginationLast { min-width: 34px; min-height: 34px; padding: 0 10px; font-size: 16px; }
/* ---- End of group: collections ---- */

/* ---- Group: Data grids (data) ---- */
/* DataGrid */
DataGrid { display: flex; flex-direction: column; position: relative; min-height: 0; min-width: 0; border: 1px solid var(--nz-border); background: var(--nz-field); color: var(--nz-on-field); overflow-x: hidden; overflow-y: hidden; }
DataGridHeader, DataGridFooter { display: flex; flex-shrink: 0; overflow-x: hidden; overflow-y: hidden; background: var(--nz-control); color: var(--nz-on-control); }
DataGridHeader { border-bottom: 1px solid var(--nz-border); }
DataGridFooter { border-top: 1px solid var(--nz-border); }
DataGridHeaderRow, DataGridFooterRow { display: flex; flex-shrink: 0; position: relative; }
DataGridColumnHeader, DataGridSelectionHeader, DataGridDetailsHeader, DataGridFooterCell { display: flex; align-items: center; flex-shrink: 0; min-height: 28px; background: var(--nz-control); color: var(--nz-on-control); }
DataGridColumnHeader { padding-left: 6px; font-weight: 700; cursor: default; }
DataGridColumnHeader[sortable]:hover { background: var(--nz-surface); color: var(--nz-on-surface); }
DataGridColumnHeader[dragging] { opacity: 0.6; }
DataGridColumnHeader[drop-target="before"] { border-left: 2px solid var(--nz-accent); }
DataGridColumnHeader[drop-target="after"] { border-right: 2px solid var(--nz-accent); }
DataGridColumnHeaderContent { display: flex; align-items: center; gap: 4px; flex: 1; min-width: 0; align-self: stretch; }
DataGridColumnHeader[align="center"] > DataGridColumnHeaderContent { justify-content: center; }
DataGridColumnHeader[align="end"] > DataGridColumnHeaderContent { justify-content: flex-end; }
DataGridColumnHeaderText { min-width: 0; overflow-x: hidden; white-space: pre; text-overflow: ellipsis; }
DataGridSortMark { width: 8px; height: 8px; flex-shrink: 0; stroke-width: 1.5px; }
DataGridSortIndex { font-size: 10px; font-weight: 400; color: var(--nz-muted); }
DataGridColumnHeader > ResizeHandle { align-self: stretch; min-height: 0; }
DataGridSelectionHeader, DataGridDetailsHeader, DataGridSelectionCell, DataGridDetailsCell { justify-content: center; }
DataGridBody { display: flex; flex-direction: column; flex: 1; min-height: 0; overflow-x: auto; overflow-y: auto; }
DataGridBodyItem { display: flex; flex-direction: column; flex-shrink: 0; }
DataGridRow, DataGridGroupHeader { display: flex; flex-shrink: 0; }
DataGridRow[alternate] { background: var(--nz-surface); }
DataGridRow:hover { background: var(--nz-control); }
DataGridRow[selected] { background: var(--nz-selection); color: var(--nz-on-selection); }
DataGridCell, DataGridSelectionCell, DataGridDetailsCell { display: flex; align-items: center; flex-shrink: 0; min-width: 0; padding: 0 6px; overflow-x: hidden; white-space: pre; text-overflow: ellipsis; }
DataGridSelectionCell, DataGridDetailsCell { padding: 0; }
DataGridCell[align="center"] { justify-content: center; }
DataGridCell[align="end"] { justify-content: flex-end; }
DataGridCell[editing] { padding: 0 2px; }
DataGridCell:focus-visible, DataGridSelectionCell:focus-visible, DataGridDetailsCell:focus-visible, DataGridGroupHeaderCell:focus-visible, DataGridColumnHeader:focus-visible, DataGridSelectionHeader:focus-visible, DataGridDetailsHeader:focus-visible { outline: 1px solid var(--nz-on-field); outline-offset: -2px; }
DataGridRow[selected] > DataGridCell:focus-visible, DataGridRow[selected] > DataGridSelectionCell:focus-visible, DataGridRow[selected] > DataGridDetailsCell:focus-visible { outline-color: var(--nz-on-selection); }
DataGridCellEditor { display: block; flex: 1; min-width: 0; padding: 1px 3px; border: 1px solid var(--nz-accent); background: var(--nz-field); color: var(--nz-on-field); white-space: pre; overflow-x: hidden; cursor: text; }
DataGridCellEditor[invalid] { border-color: var(--nz-danger); }
DataGridDetailsToggle, DataGridGroupToggle { width: 10px; height: 10px; flex-shrink: 0; stroke-width: 1.5px; }
DataGridRowDetails { display: block; flex-shrink: 0; padding: 8px 12px; border-bottom: 1px solid var(--nz-border); background: var(--nz-surface); color: var(--nz-on-surface); }
DataGridGroupHeader { background: var(--nz-control); color: var(--nz-on-control); }
DataGridGroupHeaderCell { display: flex; align-items: center; gap: 6px; flex-shrink: 0; position: relative; padding: 0 6px; font-weight: 700; }
DataGridGroupCount { font-weight: 400; color: var(--nz-muted); }
DataGridFooterCell { padding: 0 6px; font-weight: 700; }
DataGridFooterCell[align="center"] { justify-content: center; }
DataGridFooterCell[align="end"] { justify-content: flex-end; }
[frozen] { position: relative; z-index: 1; }
DataGridRow > [frozen] { background: var(--nz-field); }
DataGridRow[alternate] > [frozen] { background: var(--nz-surface); }
DataGridRow[selected] > [frozen] { background: var(--nz-selection); }
[frozen-last] { border-right: 1px solid var(--nz-on-field); }
DataGrid[gridlines="horizontal"] DataGridRow > *, DataGrid[gridlines="both"] DataGridRow > *, DataGrid[gridlines="horizontal"] DataGridGroupHeader, DataGrid[gridlines="both"] DataGridGroupHeader { border-bottom: 1px solid var(--nz-border); }
DataGrid[gridlines="vertical"] DataGridRow > *, DataGrid[gridlines="both"] DataGridRow > *, DataGrid[gridlines="vertical"] DataGridHeaderRow > *, DataGrid[gridlines="both"] DataGridHeaderRow > *, DataGrid[gridlines="vertical"] DataGridFooterRow > *, DataGrid[gridlines="both"] DataGridFooterRow > * { border-right: 1px solid var(--nz-border); }
DataGrid[size="small"] DataGridColumnHeader { min-height: 22px; font-size: 12px; }
DataGrid[size="small"] DataGridCell { font-size: 12px; }
DataGrid[size="large"] DataGridColumnHeader { min-height: 36px; }
DataGridEmpty { display: flex; flex: 1; align-items: center; justify-content: center; min-height: 48px; padding: 12px; color: var(--nz-muted); }
DataGridLoading { position: absolute; left: 0; right: 0; bottom: 0; padding: 4px 8px; border-top: 1px solid var(--nz-border); background: var(--nz-surface); color: var(--nz-on-surface); }
/* PropertyGrid: a toolbar with the order switch and the search, then name and value columns under collapsible categories. */
PropertyGrid { display: flex; flex-direction: column; gap: 4px; min-width: 0; min-height: 0; }
PropertyGridToolbar { display: flex; align-items: center; gap: 6px; }
PropertyGridToolbar > TextField { flex: 1; min-width: 0; }
PropertyGridOrder { display: flex; flex-shrink: 0; }
PropertyGridOrderOption { padding: 2px 8px; border: 1px solid var(--nz-border); background: var(--nz-control); color: var(--nz-on-control); white-space: nowrap; }
PropertyGridOrderOption[checked] { background: var(--nz-selection); color: var(--nz-on-selection); }
PropertyGridList { display: flex; flex-direction: column; flex: 1; min-height: 0; overflow-y: auto; border: 1px solid var(--nz-border); background: var(--nz-field); color: var(--nz-on-field); }
PropertyGridCategory, PropertyGridCategoryBody { display: flex; flex-direction: column; }
PropertyGridCategoryHeader { display: flex; align-items: center; gap: 4px; min-height: 24px; padding: 0 4px; background: var(--nz-control); color: var(--nz-on-control); font-weight: 600; }
PropertyGridCategoryToggle { flex-shrink: 0; width: 12px; height: 12px; stroke-width: 1.5px; }
PropertyGridRow { display: grid; grid-template-columns: minmax(0px, 2fr) minmax(0px, 3fr); align-items: center; column-gap: 8px; min-height: 26px; padding: 1px 4px 1px 20px; }
PropertyGridRow[selected] { background: var(--nz-selection); color: var(--nz-on-selection); }
PropertyGridRow[modified] > PropertyGridName { font-weight: 600; }
PropertyGridName { overflow-x: hidden; white-space: nowrap; text-overflow: ellipsis; }
PropertyGridValue { display: flex; align-items: center; min-width: 0; }
PropertyGridValue > * { flex: 1; min-width: 0; }
PropertyGridValue > CheckBox { flex: 0 0 auto; }
PropertyGridReadOnlyValue { color: var(--nz-muted); }
PropertyGridEmpty { padding: 8px; color: var(--nz-muted); }
PropertyGridDescription { display: flex; flex-direction: column; gap: 2px; min-height: 48px; padding: 6px; border: 1px solid var(--nz-border); }
PropertyGridDescriptionTitle { font-weight: 600; }
PropertyGridDescriptionText { color: var(--nz-muted); }
/* ---- End of group: data ---- */

/* ---- Group: Navigation (navigation) ---- */
/* Tabs */
Tabs { display: flex; flex-direction: column; min-width: 0; }
Tabs[placement="start"], Tabs[placement="end"] { flex-direction: row; }
TabsHeader { display: flex; align-items: stretch; flex-shrink: 0; min-width: 0; border-bottom: 1px solid var(--nz-border); }
TabsHeader[orientation="vertical"] { flex-direction: column; min-height: 0; }
Tabs[placement="bottom"] > TabsHeader { border-bottom-width: 0; border-top: 1px solid var(--nz-border); }
Tabs[placement="start"] > TabsHeader { border-bottom-width: 0; border-right: 1px solid var(--nz-border); }
Tabs[placement="end"] > TabsHeader { border-bottom-width: 0; border-left: 1px solid var(--nz-border); }
TabsScroller { display: flex; flex: 1; min-width: 0; overflow-x: auto; overflow-y: hidden; scrollbar-visibility: none; }
TabsScroller[orientation="vertical"] { flex-direction: column; min-height: 0; overflow-x: hidden; overflow-y: auto; }
TabsList { position: relative; display: flex; flex-shrink: 0; }
TabsList[orientation="vertical"] { flex-direction: column; }
Tabs[overflow="none"] TabsList { flex-wrap: wrap; flex-shrink: 1; min-width: 0; }
Tabs[overflow="menu"] TabsList { flex: 1; flex-shrink: 1; min-width: 0; overflow-x: hidden; }
TabsListItem, TabsListOverflow { display: flex; flex-shrink: 0; }
Tab { display: flex; align-items: center; gap: 6px; padding: 4px 12px; white-space: pre; flex-shrink: 0; cursor: default; }
Tab:hover { background: var(--nz-control); color: var(--nz-on-control); }
Tab[selected] { background: var(--nz-selection); color: var(--nz-on-selection); }
Tab:disabled { background: transparent; color: var(--nz-muted); }
Tabs[size="small"] Tab { padding: 2px 8px; font-size: 12px; }
Tabs[size="large"] Tab { padding: 6px 16px; font-size: 16px; }
TabLeading, TabBadge { display: flex; align-items: center; flex-shrink: 0; }
TabBadge { padding: 0 5px; border: 1px solid currentColor; border-radius: 999px; font-size: 11px; }
TabClose { padding: 0 3px; color: inherit; }
TabClose:hover { background: var(--nz-control); color: var(--nz-on-control); }
TabsIndicator { position: absolute; left: var(--nz-indicator-x, 0px); top: calc(var(--nz-indicator-y, 0px) + var(--nz-indicator-height, 0px) - 2px); width: var(--nz-indicator-width, 0px); height: 2px; background: var(--nz-accent); pointer-events: none; }
Tabs[placement="bottom"] TabsIndicator { top: var(--nz-indicator-y, 0px); }
TabsList[orientation="vertical"] > TabsIndicator { top: var(--nz-indicator-y, 0px); left: calc(var(--nz-indicator-x, 0px) + var(--nz-indicator-width, 0px) - 2px); width: 2px; height: var(--nz-indicator-height, 0px); }
Tabs[placement="end"] TabsList[orientation="vertical"] > TabsIndicator { left: var(--nz-indicator-x, 0px); }
TabsIndicator[hidden] { display: none; }
TabsScrollBack, TabsScrollForward { width: 10px; flex-shrink: 0; padding: 0 5px; stroke-width: 1.5px; }
TabsHeader[orientation="vertical"] > TabsScrollBack, TabsHeader[orientation="vertical"] > TabsScrollForward { width: auto; height: 10px; padding: 5px 0; }
TabsScrollBack:hover, TabsScrollForward:hover { background: var(--nz-control); }
TabsScrollBack:disabled, TabsScrollForward:disabled { color: var(--nz-muted); background: transparent; }
TabsOverflow { display: flex; align-items: center; padding: 4px 8px; }
TabsOverflow:hover, TabsOverflow[expanded] { background: var(--nz-control); color: var(--nz-on-control); }
TabsOverflowGlyph { width: 10px; height: 10px; stroke-width: 1.5px; }
TabsPanel { display: block; flex: 1; min-width: 0; min-height: 0; padding: 8px; }
Frame { display: block; }

/* ---- End of group: navigation ---- */

SegmentedDatePicker, SegmentedTimePicker { display: flex; width: 296px; min-height: 28px; border: 1px solid var(--nz-border); background: var(--nz-field); color: var(--nz-on-field); }
SegmentedTimePicker { width: 242px; }
SegmentedDatePicker:focus-within, SegmentedTimePicker:focus-within { border-color: var(--nz-accent); }
SegmentedDatePicker[invalid], SegmentedTimePicker[invalid] { border-color: var(--nz-danger); }
SegmentedDatePicker:disabled, SegmentedTimePicker:disabled { color: var(--nz-muted); background: var(--nz-surface); }
SegmentedDatePicker[appearance="plain"], SegmentedTimePicker[appearance="plain"] { background: transparent; border-color: transparent; }
SegmentedDatePicker[appearance="filled"], SegmentedTimePicker[appearance="filled"] { background: var(--nz-control); }
SegmentedDatePicker[appearance="underline"], SegmentedTimePicker[appearance="underline"] { border-color: transparent; border-bottom-color: var(--nz-border); background: transparent; }
SegmentedDatePicker[size="small"], SegmentedTimePicker[size="small"] { min-height: 24px; font-size: 12px; }
SegmentedDatePicker[size="large"], SegmentedTimePicker[size="large"] { min-height: 38px; font-size: 16px; }
SegmentedPickerSegments { display: flex; flex: 1; min-width: 0; align-items: stretch; }
SegmentedPickerSegment { display: flex; align-items: center; justify-content: center; flex: 1; min-width: 0; padding: 3px 8px; border-left: 1px solid var(--nz-border); white-space: nowrap; overflow: hidden; }
SegmentedPickerSegment:first-child { border-left: 0; }
SegmentedPickerSegment[placeholder] { color: var(--nz-muted); }
SegmentedPickerSegment:hover { background: var(--nz-control); }
SegmentedPickerSegment[current]:focus { outline: 1px solid var(--nz-accent); outline-offset: -2px; }
SegmentedDatePickerPopup, SegmentedTimePickerPopup { display: flex; flex-direction: column; width: 296px; height: 398px; max-height: 100%; padding: 0; gap: 0; border: 1px solid var(--nz-border); background: var(--nz-surface); color: var(--nz-on-surface); }
SegmentedTimePickerPopup { width: 242px; }
SegmentedPickerColumns { display: flex; flex: 1; height: 355px; min-height: 0; overflow: hidden; }
SegmentedPickerColumn { display: flex; flex-direction: column; justify-content: center; flex: 1; min-width: 0; min-height: 0; overflow: hidden; border-left: 1px solid var(--nz-border); }
SegmentedPickerColumn:first-child { border-left: 0; }
SegmentedPickerOption, SegmentedPickerSpacer { display: flex; align-items: center; justify-content: center; height: 40px; min-height: 40px; flex-shrink: 0; padding: 0 8px; white-space: nowrap; overflow: hidden; }
SegmentedPickerOption:hover { background: var(--nz-control); }
SegmentedPickerOption[selected] { background: var(--nz-selection); color: var(--nz-on-selection); }
SegmentedPickerOption:focus-visible { outline: 1px solid var(--nz-on-selection); outline-offset: -3px; }
SegmentedPickerActions { display: flex; height: 41px; min-height: 41px; flex-shrink: 0; border-top: 1px solid var(--nz-border); }
SegmentedPickerConfirm, SegmentedPickerCancel { display: flex; align-items: center; justify-content: center; flex: 1; min-width: 0; margin: 2px 4px; font-size: 20px; stroke-width: 1.5px; }
SegmentedPickerConfirm:hover, SegmentedPickerCancel:hover { background: var(--nz-control); }
SegmentedPickerActionIcon { position: relative; width: 16px; height: 16px; stroke-width: 1.5px; pointer-events: none; }
SegmentedPickerCancel > SegmentedPickerActionIcon::before, SegmentedPickerCancel > SegmentedPickerActionIcon::after { content: ""; position: absolute; left: 0; top: 7px; width: 16px; height: 1px; background: currentColor; transform: rotate(45deg); }
SegmentedPickerCancel > SegmentedPickerActionIcon::after { transform: rotate(-45deg); }
SegmentedPickerConfirm:focus-visible, SegmentedPickerCancel:focus-visible { outline: 2px solid var(--nz-accent); outline-offset: -2px; }

LayoutTransition { display: block; }
HoverIndicator { display: none; }

Expander[expand-direction=up], AccordionItem[expand-direction=up] { flex-direction: column-reverse; }
ExpanderChevron, AccordionItemChevron { transform: rotate(0deg); }
Expander[expanded] > ExpanderHeader > ExpanderChevron,
AccordionItem[expanded] > AccordionItemHeader > AccordionItemChevron { transform: rotate(180deg); }
Expander[expand-direction=up] > ExpanderHeader > ExpanderChevron,
AccordionItem[expand-direction=up] > AccordionItemHeader > AccordionItemChevron { transform: rotate(180deg); }
Expander[expand-direction=up][expanded] > ExpanderHeader > ExpanderChevron,
AccordionItem[expand-direction=up][expanded] > AccordionItemHeader > AccordionItemChevron { transform: rotate(0deg); }
Expander[expand-direction=up] > ExpanderPanel > ExpanderPanelContent,
AccordionItem[expand-direction=up] > AccordionItemPanel > AccordionItemPanelContent { border-top-width: 0; border-bottom: 1px solid var(--nz-border); }
ListView, TreeView, DataGrid { position: relative; }
ListViewRowMetric, TreeViewRowMetric, DataGridRowMetric { display: block; position: absolute; left: 0; top: 0; width: 1px; height: 32px; pointer-events: none; }
TreeViewRowMetric { height: 28px; }
ListViewItem, GridViewItem, TreeViewItem { position: relative; transform: none; transform-origin: 0 0; }
TreeViewExpander { transform: rotate(0deg); }
TreeViewExpander[expanded] { transform: rotate(90deg); }
ListViewItem[dragging], GridViewItem[dragging], TreeViewItem[dragging] { opacity: 0.8; }
:is(ListViewItem, GridViewItem, TreeViewItem):is([drop-before], [drop-after])::after {
    content: ""; position: absolute; left: 0; right: 0; top: 0; height: 2px; background: var(--nz-accent); pointer-events: none;
}
:is(ListViewItem, TreeViewItem)[drop-after]::after { top: auto; bottom: 0; }
GridViewItem:is([drop-before], [drop-after])::after { width: 2px; height: auto; top: 0; bottom: 0; right: auto; }
GridViewItem[drop-after]::after { left: auto; right: 0; }

TabsAdd { display: flex; align-items: center; justify-content: center; flex-shrink: 0; width: 24px; height: 24px; margin: auto 4px; border: 1px solid transparent; font-size: 18px; line-height: 20px; }
TabsAdd:hover { background: var(--nz-control); border-color: var(--nz-border); }
TabsAdd:active { background: var(--nz-selection); color: var(--nz-on-selection); }
TabsAdd:disabled { color: var(--nz-muted); background: transparent; }
TabsAdd:focus-visible { outline: 1px solid var(--nz-accent); outline-offset: -2px; }
Tabs[presentation=document] { --nz-tab-min-width: 100px; --nz-tab-max-width: 240px; --nz-tabs-add-width: 32px; }
Tabs[presentation=document] > TabsHeader { border-bottom: 1px solid var(--nz-border); }
Tabs[presentation=document] Tab { min-height: 32px; border: 1px solid transparent; border-radius: 4px 4px 0 0; }
Tabs[presentation=document] Tab[selected] { background: var(--nz-surface); color: var(--nz-on-surface); border-color: var(--nz-border); border-bottom-color: transparent; }
Tabs[presentation=document] TabLabel { flex: 1; min-width: 0; overflow: hidden; text-overflow: ellipsis; }
Tabs[presentation=document] TabsIndicator { display: none; }
Tabs[presentation=document][orientation=horizontal] TabsScroller { flex: 0 1 auto; width: calc(var(--nz-tabs-count) * var(--nz-tab-max-width)); max-width: 100%; }
Tabs[presentation=document][orientation=horizontal] TabsHeader:has(> TabsAdd) > TabsScroller { max-width: calc(100% - var(--nz-tabs-add-width)); }
Tabs[presentation=document][orientation=horizontal][width-mode=equal] TabsList { flex: 1 0 auto; width: 100%; min-width: calc(var(--nz-tabs-count) * var(--nz-tab-min-width)); max-width: calc(var(--nz-tabs-count) * var(--nz-tab-max-width)); }
Tabs[presentation=document][orientation=horizontal][width-mode=equal] Tab { flex: 1 1 0; min-width: var(--nz-tab-min-width); max-width: var(--nz-tab-max-width); }
Tabs[presentation=document][orientation=horizontal][width-mode=sizetocontent] TabsScroller,
Tabs[presentation=document][orientation=horizontal][width-mode=compact] TabsScroller { width: max-content; }
Tabs[presentation=document][orientation=horizontal][width-mode=sizetocontent] TabsList,
Tabs[presentation=document][orientation=horizontal][width-mode=compact] TabsList { width: max-content; min-width: 0; max-width: none; }
Tabs[presentation=document][orientation=horizontal][width-mode=sizetocontent] Tab,
Tabs[presentation=document][orientation=horizontal][width-mode=compact] Tab { flex: 0 0 auto; min-width: var(--nz-tab-min-width); max-width: var(--nz-tab-max-width); }
Tabs[presentation=document] Tab[compact=true] { min-width: 0; }
Tabs[presentation=document] Tab[compact=true] > TabLabel, Tabs[presentation=document] Tab[compact=true] > TabBadge { display: none; }
Tabs[presentation=document] Tab[compact=true] > TabLeading { margin-right: 0; }
Tabs[presentation=document][overflow=menu] TabsList { flex: 1; min-width: 0; max-width: 100%; }
Tabs[presentation=document][overflow=menu][width-mode=equal] TabsListItem { flex: 1 1 0; min-width: var(--nz-tab-min-width); max-width: var(--nz-tab-max-width); }
Tabs[presentation=document][overflow=none] TabsList { min-width: 0; max-width: 100%; flex-wrap: wrap; }
Tab[dragging=true] { opacity: 0.55; }
Tab[drop-target=true][drop-position=before] { box-shadow: inset 2px 0 0 var(--nz-accent); }
Tab[drop-target=true][drop-position=after] { box-shadow: inset -2px 0 0 var(--nz-accent); }
Tabs[orientation=vertical] Tab[drop-target=true][drop-position=before] { box-shadow: inset 0 2px 0 var(--nz-accent); }
Tabs[orientation=vertical] Tab[drop-target=true][drop-position=after] { box-shadow: inset 0 -2px 0 var(--nz-accent); }

SpinnerCheck { display: none; }
SpinnerRing { stroke-trim-end: var(--nz-presented-number, var(--nz-spinner-fraction)); }
SpinnerRing[indeterminate] { stroke-trim-end: 0.25; }

PipsPager, PipsPagerItems { display: flex; align-items: center; gap: 2px; }
PipsPager[orientation=vertical], PipsPager[orientation=vertical] > PipsPagerItems { flex-direction: column; }
PipsPagerPip { display: flex; align-items: center; justify-content: center; width: 24px; height: 24px; flex-shrink: 0; border-radius: 4px; }
PipsPagerDot { width: 6px; height: 6px; border-radius: 50%; background: var(--nz-muted); pointer-events: none; }
PipsPagerPip[current] > PipsPagerDot { width: 8px; height: 8px; background: var(--nz-accent); }
PipsPagerPip:hover { background: var(--nz-control); }
PipsPagerPip:active { background: var(--nz-selection); }
PipsPagerPip:focus-visible { outline: 1px solid var(--nz-accent); outline-offset: -2px; }
PipsPagerPrevious, PipsPagerNext { width: 24px; height: 24px; padding: 7px; border-radius: 4px; flex-shrink: 0; stroke-width: 1.5px; }
PipsPagerPrevious:hover, PipsPagerNext:hover { background: var(--nz-control); }
PipsPagerPrevious:active, PipsPagerNext:active { background: var(--nz-selection); }
PipsPagerPrevious:disabled, PipsPagerNext:disabled { color: var(--nz-muted); }
PipsPager:disabled PipsPagerDot { background: var(--nz-muted); }
@media (forced-colors: active) {
    PipsPagerDot { background: CanvasText; }
    PipsPagerPip[current] > PipsPagerDot { background: Highlight; }
    PipsPagerPip:focus-visible { outline-color: Highlight; }
    PipsPager:disabled PipsPagerDot { background: GrayText; }
}
InlineMessagePresence, InlineMessagePresenceContent { display: block; min-width: 0; min-height: 0; }
InlineMessagePresence { flex-shrink: 0; }
InlineMessagePresence[closed=true] { display: none; }
BreadcrumbBar { display: flex; align-items: center; position: relative; min-width: 0; overflow-x: hidden; overflow-y: hidden; }
BreadcrumbSlot { display: block; flex: 0 0 auto; min-width: 0; }
BreadcrumbSlot[current] { flex-shrink: 1; overflow-x: hidden; overflow-y: hidden; }
BreadcrumbSlot[collapsed] { position: absolute; left: 0; top: 0; opacity: 0; pointer-events: none; }
BreadcrumbItemContent { display: flex; align-items: center; width: max-content; padding: 4px; }
BreadcrumbItemContent > Button { flex-shrink: 0; min-width: 0; margin: -4px 0; border-color: transparent; background: transparent; box-shadow: none; }
BreadcrumbItemContent > Button:hover { background: var(--nz-control); }
BreadcrumbItemContent > Button:active { background: var(--nz-selection); color: var(--nz-on-selection); }
BreadcrumbItemContent > Button:focus-visible { outline-offset: -2px; }
BreadcrumbSlot[current] Button { font-weight: 600; }
BreadcrumbSlot[current]:has(Button:focus-visible) { outline: 1px solid CanvasText; outline-offset: -2px; }
BreadcrumbSlot[current] Button:focus-visible { outline-style: none; }
BreadcrumbSeparator { flex-shrink: 0; width: 10px; height: 10px; margin-left: 2px; color: var(--nz-muted); stroke-width: 1.5px; }
MenuButton.breadcrumb-overflow { flex-shrink: 0; min-width: 28px; margin: 0; border-color: transparent; background: transparent; }
BreadcrumbBar:disabled BreadcrumbItemContent > Button { color: var(--nz-muted); background: transparent; }
NavigationView { display: flex; flex-direction: column; min-width: 0; min-height: 240px; height: 100%; }
NavigationViewChrome { display: flex; align-items: center; height: 52px; min-height: 52px; flex-shrink: 0; }
NavigationViewChrome[empty] { display: none; }
NavigationViewToggle { display: flex; align-items: center; justify-content: center; width: 40px; height: 36px; margin: 4px; flex-shrink: 0; border-radius: 4px; font-size: 18px; }
NavigationViewToggle:hover { background: var(--nz-control); }
NavigationViewToggle:active { background: var(--nz-selection); }
NavigationViewToggle:focus-visible { outline: 1px solid var(--nz-accent); outline-offset: -2px; }
NavigationViewToggle:disabled { color: var(--nz-muted); }
NavigationViewHeader { display: flex; align-items: center; min-width: 0; flex: 1; padding: 0 12px; }
NavigationView > SplitView { flex: 1; min-height: 0; width: 100%; }
NavigationView > SplitView > SplitViewPane { max-width: 100%; }
NavigationViewPane { display: flex; flex-direction: column; width: 100%; height: 100%; min-width: 0; min-height: 0; }
NavigationViewPaneHeader, NavigationViewPaneFooter { display: flex; flex-direction: column; flex-shrink: 0; padding: 8px; }
NavigationViewPaneHeader:empty, NavigationViewPaneFooter:empty { display: none; }
NavigationViewTreeHost { position: relative; flex: 1; min-width: 0; min-height: 0; overflow-x: hidden; overflow-y: hidden; }
NavigationViewTreeHost > TreeView { height: 100%; min-height: 0; }
NavigationViewTreeHost TreeViewItem { border-radius: 4px; }
NavigationViewTreeHost TreeViewItem[selected] { background: var(--nz-control); color: var(--nz-on-control); }
NavigationViewTreeHost TreeViewItem[selected]::before { content: none; }
NavigationViewTreeHost TreeViewItemContent { flex: 1; min-width: 0; order: 1; }
NavigationViewTreeHost TreeViewExpander { order: 2; margin-left: auto; }
NavigationViewItemContent { display: flex; align-items: center; min-width: 0; width: 100%; gap: 12px; }
NavigationViewItemIcon { display: flex; align-items: center; justify-content: center; flex-shrink: 0; width: 20px; height: 20px; font-size: 16px; }
NavigationViewItemLabel { flex: 1; min-width: 0; white-space: nowrap; overflow: hidden; text-overflow: ellipsis; }
NavigationViewRootScroller { display: flex; flex-direction: column; flex: 1; min-width: 0; min-height: 0; overflow-y: auto; overflow-x: hidden; scrollbar-visibility: none; }
NavigationViewRoots { position: relative; display: flex; flex-direction: column; flex-shrink: 0; }
NavigationViewRootItem { display: flex; align-items: center; gap: 8px; height: 40px; min-height: 40px; padding: 8px 12px; flex-shrink: 0; border-radius: 4px; white-space: nowrap; }
NavigationViewRootItem[selected] { background: var(--nz-control); color: var(--nz-on-control); }
NavigationViewRootItem:hover { background: var(--nz-control); }
NavigationViewRootItem:active { background: var(--nz-selection); }
NavigationViewRootItem:disabled { background: transparent; color: var(--nz-muted); }
NavigationViewRootItem:focus-visible { outline: 1px solid var(--nz-accent); outline-offset: -2px; }
NavigationViewRootChevron { width: 10px; height: 10px; flex-shrink: 0; stroke-width: 1.5px; }
NavigationView[compact] NavigationViewPaneHeader, NavigationView[compact] NavigationViewPaneFooter,
NavigationView[compact] NavigationViewRootItem > NavigationViewItemLabel,
NavigationView[compact] NavigationViewRootChevron { display: none; }
NavigationView[compact] NavigationViewRootItem { justify-content: center; padding: 8px 4px; }
NavigationViewContent { flex: 1; min-width: 0; min-height: 0; }
NavigationViewTreeIndicator, NavigationViewRootIndicator { position: absolute; width: 3px; height: 16px; left: 2px; top: 0; border-radius: 2px; background: var(--nz-accent); pointer-events: none; transform: translate(0px, calc(var(--nz-indicator-y, 0px) + var(--nz-indicator-height, 0px) / 2 - 8px)); }
NavigationViewTreeIndicator[hidden], NavigationViewRootIndicator[hidden] { display: none; }
NavigationView[display-mode=top] NavigationViewToggle { display: none; }
NavigationView[display-mode=top] NavigationViewPane { flex-direction: row; align-items: center; }
NavigationView[display-mode=top] NavigationViewRootScroller { flex-direction: row; overflow-x: auto; overflow-y: hidden; }
NavigationView[display-mode=top] NavigationViewRoots { flex-direction: row; min-width: max-content; }
NavigationView[display-mode=top] NavigationViewPaneHeader, NavigationView[display-mode=top] NavigationViewPaneFooter { flex-direction: row; }
NavigationView[display-mode=top] NavigationViewRootIndicator { left: 0; width: 16px; height: 3px; transform: translate(calc(var(--nz-indicator-x, 0px) + var(--nz-indicator-width, 0px) / 2 - 8px), calc(var(--nz-indicator-y, 0px) + var(--nz-indicator-height, 0px) - 4px)); }
@media (forced-colors: active) {
    NavigationViewRootItem[selected] { background: Highlight; color: HighlightText; }
    NavigationViewRootItem:disabled, NavigationViewToggle:disabled { color: GrayText; }
    NavigationViewTreeIndicator, NavigationViewRootIndicator { background: Highlight; }
}
ToastHostRegion > ItemsTransition { width: 100%; gap: inherit; align-items: inherit; overlay-exit-duration: 0ms; }
ToastHostRegion ItemsTransitionItem { max-width: 100%; transition: none; }
ToastHostRegion ItemsTransitionContent, ToastHostRegion ItemsTransitionContent[entering], ToastHostRegion ItemsTransitionContent[exiting] { animation: none; transition: none; transform: none; }
NavigationView { position: relative; }
NavigationViewChrome { position: absolute; left: 0; top: 0; width: 48px; height: 52px; min-height: 52px; z-index: 3; }
NavigationView[display-mode=top] NavigationViewChrome { display: none; }
NavigationViewHeader { flex: 0 0 auto; height: 52px; min-height: 52px; }
NavigationViewHeader[empty] { display: none; }
NavigationView[display-mode=minimal] NavigationViewHeader[empty] { display: flex; }
NavigationView:not([display-mode=top]) NavigationViewPaneHeader { display: flex; min-height: 52px; padding-left: 52px; }
NavigationView[compact] NavigationViewPaneHeader { display: flex; height: 52px; min-height: 52px; padding: 0; }
NavigationView[compact] NavigationViewPaneHeader * { visibility: hidden; }
NavigationView[display-mode=top] NavigationViewPaneHeader, NavigationView[display-mode=top] NavigationViewPaneFooter { padding: 0 8px; }
NavigationView[display-mode=top] NavigationViewRootScroller { min-width: min(48px, 100%); }
NavigationView[display-mode=top] NavigationViewPaneHeader, NavigationView[display-mode=top] NavigationViewPaneFooter { flex-shrink: 1; min-width: 0; overflow: hidden; }
NavigationView[display-mode=top] NavigationViewRootItem { max-width: var(--nz-navigation-root-viewport, 100%); }
ColorPicker { display: flex; flex-direction: column; width: 320px; min-width: 0; gap: 12px; }
ColorPickerVisuals, ColorPickerInputs { display: flex; flex-direction: column; min-width: 0; gap: 12px; }
ColorPickerVisuals { padding: 8px; }
ColorPickerSpectrum { position: relative; width: 100%; height: 220px; border-radius: var(--nz-radius); background: var(--nz-color-area-background); }
ColorPickerSpectrumThumb { position: absolute; left: calc(var(--nz-color-area-x) - 8px); top: calc(var(--nz-color-area-y) - 8px); width: 16px; height: 16px; border: 2px solid #ffffff; outline: 1px solid #000000; outline-offset: 0; border-radius: 50%; background: var(--nz-color-area-color); pointer-events: none; }
ColorPickerSlider, ColorPickerAlphaSlider { position: relative; width: 100%; height: 24px; padding: 6px 0; }
ColorPickerSliderTrack, ColorPickerAlphaSliderTrack { position: relative; width: 100%; height: 100%; border-radius: 6px; background: var(--nz-color-slider-gradient); }
ColorPickerSliderThumb, ColorPickerAlphaSliderThumb { position: absolute; left: calc(var(--nz-color-slider-position) - 8px); top: calc(50% - 8px); width: 16px; height: 16px; border: 2px solid #ffffff; outline: 1px solid #000000; outline-offset: 0; border-radius: 50%; background: var(--nz-color-slider-color); pointer-events: none; }
ColorPickerPreview { display: flex; gap: 8px; height: 36px; }
ColorPickerSwatch { flex: 1; min-width: 0; position: relative; border: 1px solid var(--nz-border); border-radius: var(--nz-radius); overflow-x: hidden; overflow-y: hidden; }
ColorPickerSwatch {
    background-color: #ffffff;
    background-image: linear-gradient(45deg, #c8c8c8 25%, transparent 25%), linear-gradient(-45deg, #c8c8c8 25%, transparent 25%), linear-gradient(45deg, transparent 75%, #c8c8c8 75%), linear-gradient(-45deg, transparent 75%, #c8c8c8 75%);
    background-size: 8px 8px;
    background-position: 0 0, 0 4px, 4px -4px, -4px 0;
}
ColorPickerAlphaSliderTrack {
    background-color: #ffffff;
    background-image: var(--nz-color-slider-gradient), linear-gradient(45deg, #c8c8c8 25%, transparent 25%), linear-gradient(-45deg, #c8c8c8 25%, transparent 25%), linear-gradient(45deg, transparent 75%, #c8c8c8 75%), linear-gradient(-45deg, transparent 75%, #c8c8c8 75%);
    background-size: 100% 100%, 8px 8px, 8px 8px, 8px 8px, 8px 8px;
    background-position: 0 0, 0 0, 0 4px, 4px -4px, -4px 0;
}
ColorPickerPreviewPrevious, ColorPickerPreviewCurrent { width: 100%; height: 100%; }
ColorPickerPreviewPrevious { background: var(--nz-color-picker-previous); }
ColorPickerPreviewCurrent { background: var(--nz-color-picker-color); }
ColorPickerChannels { display: grid; grid-template-columns: repeat(3, minmax(0, 1fr)); gap: 8px; }
ColorPicker[alpha-enabled] ColorPickerChannels { grid-template-columns: repeat(4, minmax(0, 1fr)); }
ColorPicker:not([channels-visible]) ColorPickerChannels { grid-template-columns: minmax(0, 1fr); }
ColorPickerChannel, ColorPickerHex { display: flex; flex-direction: column; min-width: 0; gap: 4px; }
ColorPickerChannel Label, ColorPickerHex Label { font-size: 12px; }
ColorPickerChannel TextField, ColorPickerHex TextField { width: 100%; min-width: 0; }
ColorPickerRepresentation Select { width: 100%; }
ColorPicker:disabled ColorPickerSpectrum, ColorPicker:disabled ColorPickerSlider, ColorPicker:disabled ColorPickerAlphaSlider { opacity: .55; }
ColorPicker[orientation=horizontal] { flex-direction: row; width: 640px; gap: 16px; }
ColorPicker[orientation=horizontal] ColorPickerVisuals { display: grid; grid-template-columns: minmax(0, 1fr) 24px 24px 32px; flex: 1; gap: 8px; }
ColorPicker[orientation=horizontal] ColorPickerSpectrum { grid-column: 1; grid-row: 1; }
ColorPicker[orientation=horizontal] ColorPickerSlider { grid-column: 2; grid-row: 1; }
ColorPicker[orientation=horizontal] ColorPickerAlphaSlider { grid-column: 3; grid-row: 1; }
ColorPicker[orientation=horizontal] ColorPickerPreview { grid-column: 4; grid-row: 1; flex-direction: column; height: auto; }
ColorPicker[orientation=horizontal] ColorPickerInputs { width: 220px; flex-shrink: 0; }
ColorPickerSlider[orientation=vertical], ColorPickerAlphaSlider[orientation=vertical] { width: 24px; height: 220px; padding: 0 6px; }
ColorPickerSlider[orientation=vertical] ColorPickerSliderThumb, ColorPickerAlphaSlider[orientation=vertical] ColorPickerAlphaSliderThumb { left: calc(50% - 8px); top: auto; bottom: calc(var(--nz-color-slider-position) - 8px); }
@media (forced-colors: active) {
    ColorPickerSpectrumThumb, ColorPickerSliderThumb, ColorPickerAlphaSliderThumb { border-color: Canvas; outline-color: CanvasText; }
    ColorPickerSwatch { border-color: CanvasText; }
}
NavigationView[display-mode=top] NavigationViewRootIndicator { min-height: 3px; }
NavigationView[display-mode=top] NavigationViewRootScroller { align-self: stretch; }
NavigationView[display-mode=top] NavigationViewRoots { align-items: center; }
BadgeValue { display: block; }
BadgeIcon, BadgeIndicatorContent { display: flex; align-items: center; justify-content: center; }
BadgeIcon > Icon { width: 1em; height: 1em; font-size: inherit; }
BadgeIndicator[icon] { width: 16px; min-width: 16px; padding: 0; }
Badge[size="small"] > BadgeIndicator[icon] { width: 12px; min-width: 12px; padding: 0; }
Badge[size="large"] > BadgeIndicator[icon] { width: 20px; min-width: 20px; padding: 0; }
Span, Strong, Emphasis, InlineCode, Mark, Abbreviation, Deleted, Inserted, Subscript, Superscript, SmallText, Quote, LineBreak { display: inline; }
Paragraph Link, Paragraph Kbd, TextBlock Link, TextBlock Kbd, Span Link, Span Kbd, Strong Link, Strong Kbd,
Emphasis Link, Emphasis Kbd, Mark Link, Mark Kbd, Inserted Link, Inserted Kbd, Deleted Link, Deleted Kbd, Quote Link, Quote Kbd,
ListItem Link, ListItem Kbd, DefinitionDescription Link, DefinitionDescription Kbd, InlineMessageText Link, InlineMessageText Kbd,
BlockquoteContent Link, BlockquoteContent Kbd,
LinkExternalMark { display: inline; }
Strong { font-weight: 700; }
Emphasis, Quote { font-style: italic; }
InlineCode { font-family: Consolas, "Cascadia Mono", "DejaVu Sans Mono", monospace; font-size: 0.9em; padding: 1px 4px; background: var(--nz-control); white-space: pre; }
Mark { padding: 0 3px; background: color-mix(in srgb, var(--nz-accent) 25%, transparent); }
Abbreviation { text-decoration-line: underline; text-decoration-style: dotted; }
Deleted { text-decoration-line: line-through; color: var(--nz-muted); }
Inserted { padding: 0 3px; background: color-mix(in srgb, var(--nz-success) 18%, transparent); }
Subscript, Superscript { font-size: 0.75em; position: relative; }
Subscript { top: 0.25em; }
Superscript { top: -0.4em; }
SmallText { font-size: 0.85em; color: var(--nz-muted); }
