/*
 * Windows 9x: the Windows 95/98 standard scheme. A #c0c0c0 face, two-pixel bevels (outer white and black, inner
 * #dfdfdf and #808080), white sunken fields, navy selection, the navy-to-blue active caption and 8-point sans text.
 */
@media (forced-colors: none) {
    :root {
        font-family: "Microsoft Sans Serif", "MS Sans Serif", Tahoma, Arial, sans-serif;
        font-size: 11px;
        --nz-surface: #c0c0c0;
        --nz-on-surface: #000000;
        --nz-control: #c0c0c0;
        --nz-on-control: #000000;
        --nz-border: #808080;
        --nz-field: #ffffff;
        --nz-on-field: #000000;
        --nz-accent: #000080;
        --nz-on-accent: #ffffff;
        --nz-selection: #000080;
        --nz-on-selection: #ffffff;
        --nz-link: #0000ff;
        --nz-muted: #808080;
        --nz-danger: #800000;
        --nz-success: #008000;
        --nz-warning: #808000;
        --win9x-highlight: #ffffff;
        --win9x-light: #dfdfdf;
        --win9x-shadow: #808080;
        --win9x-dark: #000000;
        --win9x-info: #ffffe1;
        scrollbar-visibility: visible;
        scrollbar-placement: reserve;
        scrollbar-width: 16px;
        scrollbar-inset: 0;
        scrollbar-radius: 0;
        scrollbar-track-radius: 0;
        scrollbar-min-thumb-length: 8px;
        scrollbar-track-color: #e3e3e3;
        scrollbar-track-border-width: 0;
        scrollbar-thumb-color: #c0c0c0;
        scrollbar-thumb-hover-color: #c0c0c0;
        scrollbar-thumb-active-color: #c0c0c0;
        scrollbar-thumb-border-width: 1px;
        scrollbar-thumb-border-color: var(--win9x-dark);
        scrollbar-thumb-border-highlight-color: var(--win9x-highlight);
        scrollbar-buttons: visible;
        scrollbar-button-length: 16px;
        scrollbar-button-radius: 0;
        scrollbar-button-color: #000000;
        scrollbar-button-disabled-color: #808080;
        scrollbar-button-background: #c0c0c0;
        scrollbar-button-hover-background: #c0c0c0;
        scrollbar-button-active-background: #c0c0c0;
        scrollbar-button-step: 16px;
    }

    :focus-visible { outline: 1px dotted var(--nz-on-surface); outline-offset: -4px; }
    :disabled { color: var(--win9x-shadow); }
    Link:focus-visible, Button[appearance="link"]:focus-visible { outline-offset: 1px; }
    TextField:focus-visible, TextArea:focus-visible, Select:focus-visible, ComboBox:focus-visible, NumberField:focus-visible,
    SearchField:focus-visible, PasswordField:focus-visible, CheckBox:focus-visible, RadioButton:focus-visible, Switch:focus-visible,
    Slider:focus-visible { outline-offset: 1px; }

    /* Frame and caption: a four-pixel raised frame, the gradient caption and 16 x 14 caption buttons. */
    :root[frame-owner="nazor"][client-titlebar] {
        -window-frame-border: 1px solid var(--win9x-dark);
        -window-frame-border-radius: 0px;
        -window-frame-box-shadow: none;
    }
    :root[frame-owner="nazor"][client-titlebar][window-state="normal"] > WindowContent {
        border: 1px solid;
        border-color: var(--win9x-light) var(--win9x-shadow) var(--win9x-shadow) var(--win9x-light);
        box-shadow: inset 1px 1px 0 var(--win9x-highlight), inset -1px -1px 0 var(--win9x-light);
        padding: 2px;
    }
    TitleBar {
        gap: 3px;
        padding-left: 3px;
        background: linear-gradient(90deg, #000080, #1084d0);
        color: #ffffff;
    }
    TitleBar[source="nazor"][height="standard"] { min-height: 18px; }
    TitleBar[source="nazor"] TitleBarAppIcon { min-width: 16px; }
    TitleBar[source="nazor"] TitleBarAppIcon > Image { width: 16px; height: 16px; }
    TitleBarTitle { font-size: 11px; font-weight: 700; }
    :root:window-inactive TitleBar { background: linear-gradient(90deg, #808080, #b5b5b5); color: #c0c0c0; }
    CaptionButtons { align-items: center; gap: 0; padding: 0 2px 0 0; }
    CaptionButtonsButton, CaptionButtonsButton:hover:not(:disabled), CaptionButtonsButton[kind="close"]:hover:not(:disabled) {
        width: 16px;
        min-height: 14px;
        height: 14px;
        padding: 0;
        background: #c0c0c0;
        color: #000000;
        border: 1px solid;
        border-color: var(--win9x-highlight) var(--win9x-dark) var(--win9x-dark) var(--win9x-highlight);
        box-shadow: inset 1px 1px 0 var(--win9x-light), inset -1px -1px 0 var(--win9x-shadow);
    }
    CaptionButtonsButton[kind="close"] { margin-left: 2px; }
    CaptionButtonsButton:active:not(:disabled), CaptionButtonsButton[kind="close"]:active:not(:disabled) {
        background: #c0c0c0;
        color: #000000;
        border-color: var(--win9x-dark) var(--win9x-highlight) var(--win9x-highlight) var(--win9x-dark);
        box-shadow: inset 1px 1px 0 var(--win9x-shadow), inset -1px -1px 0 var(--win9x-light);
    }
    CaptionButtonsButton:disabled { color: var(--win9x-shadow); }
    CaptionButtonsGlyph { stroke-width: 2px; }
    CaptionButtonsGlyph[kind="minimize"] { width: 6px; height: 2px; margin-top: 6px; stroke-trim-end: 0; background: currentColor; }
    CaptionButtonsGlyph[kind="maximize"] { width: 9px; height: 8px; border: 1px solid currentColor; border-top-width: 2px; }
    CaptionButtonsGlyph[kind="restore"] { width: 6px; height: 5px; margin: 3px 2px 0 0; border: 1px solid currentColor; border-top-width: 2px; }
    CaptionButtonsGlyphBack { left: 2px; top: -5px; width: 6px; height: 5px; border-top-width: 2px; }
    CaptionButtonsGlyph[kind="close"] { width: 8px; height: 7px; }
    CaptionButtonsGlyphStroke { left: -1px; top: 2px; width: 10px; height: 2px; }

    /* Bars. */
    MenuBar { padding: 1px 2px; border-bottom: 0; background: var(--nz-surface); }
    MenuBarItem { padding: 2px 6px; border: 1px solid transparent; }
    MenuBarItem:hover { background: transparent; color: var(--nz-on-surface); border-color: var(--win9x-highlight) var(--win9x-shadow) var(--win9x-shadow) var(--win9x-highlight); }
    MenuBarItem[expanded], MenuBarItem:focus { background: transparent; color: var(--nz-on-surface); border-color: var(--win9x-shadow) var(--win9x-highlight) var(--win9x-highlight) var(--win9x-shadow); }
    MenuBarItem:disabled { color: var(--win9x-shadow); border-color: transparent; }
    Toolbar { background: var(--nz-surface); }
    ToolbarSeparator { width: 2px; margin: 2px 3px; background: var(--win9x-shadow); border-right: 1px solid var(--win9x-highlight); }
    ToolbarSeparator[orientation="horizontal"] { width: auto; height: 2px; border-right: 0; border-bottom: 1px solid var(--win9x-highlight); }
    StatusBar { min-height: 20px; padding: 2px 2px 0; gap: 2px; border-top: 0; background: var(--nz-surface); font-size: 11px; }
    StatusBarSection, StatusBarSection + StatusBarSection {
        padding: 0 3px;
        border: 1px solid;
        border-color: var(--win9x-shadow) var(--win9x-highlight) var(--win9x-highlight) var(--win9x-shadow);
    }
    StatusBarSizeGrip {
        background: linear-gradient(135deg, transparent 50%, #ffffff 50%, #ffffff 58%, #808080 58%, #808080 70%, transparent 70%, transparent 74%, #ffffff 74%, #ffffff 82%, #808080 82%, #808080 94%, transparent 94%);
    }

    /* Raised controls. */
    Button, ToggleButton, MenuButton, SplitButton { min-height: 23px; padding: 2px 8px; border-radius: 0; }
    Button:where([appearance="standard"]:not([icon-only], [shape="square"], [shape="circle"], [size="small"], [size="large"])) { min-width: 75px; }
    Button:where(:is([appearance="standard"], [appearance="accent"], [appearance="tonal"], [appearance="outline"]):not([shape="pill"], [shape="circle"])),
    ToggleButton:where(:is([appearance="standard"], [appearance="accent"], [appearance="tonal"], [appearance="outline"]):not([pressed], [shape="pill"], [shape="circle"])),
    MenuButton:where(:not([appearance="subtle"], [appearance="link"], [expanded])),
    NumberFieldIncrement, NumberFieldDecrement, SelectChevron, ComboBoxChevron, SliderThumb, RangeSliderThumb, SwitchThumb,
    CaptionButtonsButton {
        border: 1px solid;
        border-color: var(--win9x-highlight) var(--win9x-dark) var(--win9x-dark) var(--win9x-highlight);
        box-shadow: inset 1px 1px 0 var(--win9x-light), inset -1px -1px 0 var(--win9x-shadow);
    }
    Button:where(:is([appearance="standard"], [appearance="accent"], [appearance="tonal"], [appearance="outline"]):not([intent="danger"])),
    ToggleButton:where(:is([appearance="standard"], [appearance="accent"], [appearance="tonal"], [appearance="outline"])),
    MenuButton:where(:not([appearance="subtle"], [appearance="link"])) {
        background: var(--nz-control);
        color: var(--nz-on-control);
    }
    Button:hover, ToggleButton:hover, MenuButton:hover { border-color: var(--win9x-highlight) var(--win9x-dark) var(--win9x-dark) var(--win9x-highlight); }
    Button:where([appearance="subtle"]):hover:not(:disabled), ToggleButton:where([appearance="subtle"]):hover:not(:disabled), MenuButton:where([appearance="subtle"]):hover:not(:disabled) {
        border-color: var(--win9x-highlight) var(--win9x-shadow) var(--win9x-shadow) var(--win9x-highlight);
        box-shadow: none;
    }
    Button:active:where(:not([appearance="link"], :disabled, [shape="pill"], [shape="circle"])),
    ToggleButton:active:where(:not([appearance="link"], :disabled, [shape="pill"], [shape="circle"])),
    ToggleButton[pressed]:where(:not([appearance="link"], :disabled, [shape="pill"], [shape="circle"])),
    MenuButton:active:where(:not(:disabled)), MenuButton[expanded] {
        background: var(--nz-control);
        color: var(--nz-on-control);
        border-color: var(--win9x-dark) var(--win9x-highlight) var(--win9x-highlight) var(--win9x-dark);
        box-shadow: inset 1px 1px 0 var(--win9x-shadow), inset -1px -1px 0 var(--win9x-light);
    }
    ToggleButton[pressed]:where(:not(:disabled)) {
        background: repeating-conic-gradient(#ffffff 0% 25%, #c0c0c0 0% 50%);
        background-size: 2px 2px;
    }
    Button:active > ButtonLabel, ToggleButton:active > ButtonLabel { margin: 1px -1px -1px 1px; }
    /* The default button carries an extra black frame. */
    Button[default]:where(:not([intent="danger"], [intent="warning"], [intent="success"], :disabled)) {
        background: var(--nz-control);
        color: var(--nz-on-control);
        border-color: var(--win9x-dark);
        box-shadow: inset 1px 1px 0 var(--win9x-highlight), inset -1px -1px 0 var(--win9x-shadow), inset 2px 2px 0 var(--win9x-light), inset -2px -2px 0 var(--win9x-shadow);
    }
    /* Windows 9x has no colored buttons: accent and tonal buttons keep the face color, and the accent is bold. */
    :is(Button, ToggleButton):is([appearance="accent"], [appearance="tonal"]):not(:disabled, [pressed], :active) {
        background: var(--nz-control);
        color: var(--nz-on-control);
        border-color: var(--win9x-highlight) var(--win9x-dark) var(--win9x-dark) var(--win9x-highlight);
    }
    :is(Button, ToggleButton)[appearance="accent"] { font-weight: 700; }
    Button[intent="danger"]:not(:disabled), Button[intent="danger"][appearance="accent"]:not(:disabled) { background: var(--nz-control); color: var(--nz-danger); }
    Button:disabled, ToggleButton:disabled, MenuButton:disabled {
        background: var(--nz-control);
        color: var(--win9x-shadow);
        border-color: var(--win9x-highlight) var(--win9x-dark) var(--win9x-dark) var(--win9x-highlight);
    }
    Button[size="small"], ToggleButton[size="small"] { min-height: 18px; font-size: 11px; }
    Button[size="large"], ToggleButton[size="large"] { min-height: 30px; font-size: 13px; }

    /* Sunken fields and wells. */
    :is(TextField, TextArea, Select, NumberField, ComboBox, SearchField, PasswordField, TokenField, MaskedField, ShortcutField,
        DateField, TimeField, FileField, SegmentedDatePicker, SegmentedTimePicker, DatePicker, TimePicker):where([appearance="outline"]),
    CheckBoxMark, RadioButtonMark, SliderTrack, RangeSliderTrack, ProgressBarTrack, ListView, TreeView, ListBox, GridView, DataGrid, Table[bordered] {
        border: 1px solid;
        border-color: var(--win9x-shadow) var(--win9x-highlight) var(--win9x-highlight) var(--win9x-shadow);
        box-shadow: inset 1px 1px 0 var(--win9x-dark), inset -1px -1px 0 var(--win9x-light);
        border-radius: 0;
    }
    :is(TextField, TextArea, Select, NumberField, ComboBox, SearchField, PasswordField, TokenField, MaskedField, ShortcutField):where([appearance="outline"]) {
        min-height: 21px;
        padding: 2px 3px;
        background: var(--nz-field);
    }
    :is(TextField, TextArea, Select, NumberField, ComboBox, SearchField, PasswordField, TokenField, MaskedField, ShortcutField):focus-within {
        border-color: var(--win9x-shadow) var(--win9x-highlight) var(--win9x-highlight) var(--win9x-shadow);
    }
    :is(TextField, TextArea, Select, NumberField, ComboBox, SearchField, PasswordField, TokenField, MaskedField, ShortcutField):disabled { background: var(--nz-surface); }
    TextField[appearance="filled"], TextArea[appearance="filled"], Select[appearance="filled"] { background: var(--nz-surface); border: 1px solid var(--win9x-shadow); }
    Select { padding-right: 1px; }
    Select:focus-visible > SelectValue { background: var(--nz-selection); color: var(--nz-on-selection); }
    SelectChevron, ComboBoxChevron {
        width: 16px;
        height: 17px;
        padding: 4px 3px;
        margin: -1px 0;
        background: var(--nz-control);
        color: #000000;
        stroke-width: 2px;
    }
    Select[expanded] > SelectChevron, ComboBox[expanded] > ComboBoxChevron {
        border-color: var(--win9x-shadow);
        box-shadow: none;
    }
    NumberFieldIncrement, NumberFieldDecrement { background: var(--nz-control); color: #000000; }

    CheckBoxMark { width: 13px; height: 13px; background: var(--nz-field); color: #000000; stroke-width: 2px; }
    CheckBox:checked:not(:disabled) > CheckBoxMark, CheckBox[indeterminate]:not(:disabled) > CheckBoxMark,
    CheckBox:hover:not(:disabled) > CheckBoxMark {
        background: var(--nz-field);
        color: #000000;
        border-color: var(--win9x-shadow) var(--win9x-highlight) var(--win9x-highlight) var(--win9x-shadow);
    }
    CheckBox:active:not(:disabled) > CheckBoxMark, RadioButton:active:not(:disabled) > RadioButtonMark { background: var(--nz-surface); }
    CheckBox:disabled > CheckBoxMark, RadioButton:disabled > RadioButtonMark { background: var(--nz-surface); }
    RadioButtonMark { width: 12px; height: 12px; border-radius: 50%; background: var(--nz-field); }
    RadioButton:hover > RadioButtonMark, RadioButton:checked:not(:disabled) > RadioButtonMark {
        background: var(--nz-field);
        border-color: var(--win9x-shadow) var(--win9x-highlight) var(--win9x-highlight) var(--win9x-shadow);
    }
    RadioButtonDot { width: 4px; height: 4px; }
    RadioButton:checked:not(:disabled) > RadioButtonMark > RadioButtonDot { background: #000000; }

    SliderTrack, RangeSliderTrack { top: 9px; height: 4px; background: var(--nz-surface); }
    SliderFill, RangeSliderFill { display: none; }
    SliderThumb, RangeSliderThumb { top: 1px; width: 11px; height: 21px; border-radius: 0; background: var(--nz-control); }
    SliderThumb { left: calc(var(--nz-slider-position) * (100% - 11px)); }
    Slider:hover > SliderThumb { border-color: var(--win9x-highlight) var(--win9x-dark) var(--win9x-dark) var(--win9x-highlight); }
    Slider[orientation="vertical"] > SliderThumb { width: 21px; height: 11px; left: 1px; }
    Slider:disabled > SliderThumb { background: var(--nz-surface); }

    SwitchTrack { border-radius: 0; height: 16px; min-width: 34px; background: var(--nz-field); border: 1px solid; border-color: var(--win9x-shadow) var(--win9x-highlight) var(--win9x-highlight) var(--win9x-shadow); box-shadow: inset 1px 1px 0 var(--win9x-dark); }
    SwitchThumb { width: 12px; height: 12px; border-radius: 0; background: var(--nz-control); }
    Switch:checked > SwitchTrack { background: var(--nz-selection); border-color: var(--win9x-shadow) var(--win9x-highlight) var(--win9x-highlight) var(--win9x-shadow); }
    Switch:checked > SwitchTrack > SwitchThumbRail > SwitchThumbPosition > SwitchThumb { background: var(--nz-control); }

    /* The progress bar fills with navy blocks. */
    ProgressBarTrack { height: 16px; padding: 2px; background: var(--nz-surface); }
    ProgressBarFill:not([indeterminate]), ProgressBarSegment[filled] {
        top: 2px;
        bottom: 2px;
        left: 2px;
        background: repeating-linear-gradient(90deg, #000080 0px, #000080 8px, transparent 8px, transparent 10px);
    }
    ProgressBarFill:not([indeterminate]) { width: calc(var(--nz-progress-fraction, 0) * (100% - 4px)); }

    /* Lists and selection. */
    ListView, TreeView, ListBox, GridView { background: var(--nz-field); color: var(--nz-on-field); }
    ListViewItem:hover, TreeViewItem:hover, GridViewItem:hover { background: transparent; color: inherit; }
    ListViewItem[selected], TreeViewItem[selected], GridViewItem[selected] { background: var(--nz-selection); color: var(--nz-on-selection); }
    TableHeaderCell, DataGridColumnHeader {
        background: var(--nz-control);
        border: 1px solid;
        border-color: var(--win9x-highlight) var(--win9x-dark) var(--win9x-dark) var(--win9x-highlight);
        box-shadow: inset -1px -1px 0 var(--win9x-shadow);
    }

    /* Tabs: raised tabs over a raised page; the selected tab rises two pixels and joins the page. */
    TabsHeader { border-bottom: 1px solid var(--win9x-highlight); }
    Tab, Tabs[presentation="document"] Tab {
        margin-top: 2px;
        padding: 2px 8px;
        border: 1px solid;
        border-bottom-width: 0;
        border-color: var(--win9x-highlight) var(--win9x-dark) transparent var(--win9x-highlight);
        border-radius: 3px 3px 0 0;
        box-shadow: inset -1px 0 0 var(--win9x-shadow), inset 1px 1px 0 var(--win9x-light);
        background: var(--nz-surface);
    }
    Tab:hover { background: var(--nz-surface); color: var(--nz-on-surface); }
    Tab[selected]:where(:not(:disabled)) {
        margin-top: 0;
        margin-bottom: -1px;
        padding-bottom: 4px;
        background: var(--nz-surface);
        color: var(--nz-on-surface);
    }
    TabsIndicator { display: none; }

    /* Group boxes use the etched line. */
    GroupBox, Expander, AccordionItem, Card:where([appearance="outline"]) {
        border: 1px solid var(--win9x-shadow);
        box-shadow: 1px 1px 0 var(--win9x-highlight), inset 1px 1px 0 var(--win9x-highlight);
    }
    Card:where([appearance="standard"]), SegmentedControl {
        border: 1px solid;
        border-color: var(--win9x-highlight) var(--win9x-dark) var(--win9x-dark) var(--win9x-highlight);
        box-shadow: inset 1px 1px 0 var(--win9x-light), inset -1px -1px 0 var(--win9x-shadow);
    }
    ExpanderHeader, AccordionItemHeader { background: var(--nz-surface); }
    ExpanderHeader:hover, AccordionItemHeader:hover { color: var(--nz-on-surface); }
    Separator { height: 2px; background: var(--win9x-shadow); border-bottom: 1px solid var(--win9x-highlight); }
    Separator[vertical] { width: 2px; border-bottom: 0; border-right: 1px solid var(--win9x-highlight); }

    /* Menus and popups: raised gray surfaces, navy highlight, no rounding or shadow. */
    Menu, ContextMenu, SelectPopup, ComboBoxPopup, SearchFieldPopup, Popover, PopoverContainer, Dialog, DialogContainer,
    AlertDialog, Toast, CommandPalette {
        padding: 2px;
        border: 1px solid;
        border-color: var(--win9x-light) var(--win9x-dark) var(--win9x-dark) var(--win9x-light);
        border-radius: 0;
        background: var(--nz-surface);
        box-shadow: inset 1px 1px 0 var(--win9x-highlight), inset -1px -1px 0 var(--win9x-shadow);
    }
    SelectPopup, ComboBoxPopup, SearchFieldPopup { padding: 0; border: 1px solid var(--win9x-dark); box-shadow: none; background: var(--nz-field); }
    MenuItem, MenuCheckItem, MenuRadioItem, Submenu { min-height: 17px; padding: 1px 18px 1px 18px; }
    MenuItem[intent="danger"]:is(:hover, :focus):where(:not(:disabled)) { color: var(--nz-on-selection); }
    MenuItem:is(:hover, :focus):not(:disabled) > MenuItemDescription { color: inherit; }
    MenuItem:disabled, MenuCheckItem:disabled, MenuRadioItem:disabled, Submenu:disabled { color: var(--win9x-shadow); }
    MenuSeparator { height: 2px; margin: 3px 1px; background: var(--win9x-shadow); border-bottom: 1px solid var(--win9x-highlight); }
    Tooltip, TooltipContainer { padding: 1px 3px; border: 1px solid #000000; border-radius: 0; background: var(--win9x-info); color: #000000; box-shadow: none; }
    Dialog:where([size="fullscreen"], [dialog-placement]) { border-radius: 0; }
    DialogTitle, AlertDialogTitle {
        margin: -2px -2px 0;
        padding: 2px 4px;
        background: linear-gradient(90deg, #000080, #1084d0);
        color: #ffffff;
        font-size: 11px;
        font-weight: 700;
    }
}
