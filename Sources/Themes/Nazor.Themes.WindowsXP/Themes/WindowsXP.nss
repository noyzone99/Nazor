/*
 * Windows XP: the Luna Blue visual style. A #ece9d8 face, Tahoma 8 pt, the glossy blue caption with rounded top
 * corners, white-bordered round caption buttons, #003c74 rounded push buttons with an orange hover glow and a blue
 * default glow, #7f9db9 field borders, green check marks and progress blocks, and #316ac5 selection.
 */
@media (forced-colors: none) {
    :root {
        font-family: Tahoma, "Segoe UI", Verdana, sans-serif;
        font-size: 11px;
        --nz-surface: #ece9d8;
        --nz-on-surface: #000000;
        --nz-control: #f4f3ee;
        --nz-on-control: #000000;
        --nz-border: #7f9db9;
        --nz-field: #ffffff;
        --nz-on-field: #000000;
        --nz-accent: #316ac5;
        --nz-on-accent: #ffffff;
        --nz-selection: #316ac5;
        --nz-on-selection: #ffffff;
        --nz-link: #0000ff;
        --nz-muted: #aca899;
        --nz-danger: #c11b17;
        --nz-success: #21a121;
        --winxp-button-edge: #003c74;
        --winxp-field-edge: #7f9db9;
        --winxp-frame: #0831d9;
        --winxp-menu-edge: #aca899;
        --winxp-mark-edge: #1c5180;
        --winxp-check: #21a121;
        --winxp-tab-edge: #919b9c;
        --winxp-group-edge: #d0d0bf;
        --winxp-info: #ffffe1;
        scrollbar-visibility: visible;
        scrollbar-placement: reserve;
        scrollbar-width: 17px;
        scrollbar-inset: 0;
        scrollbar-radius: 3px;
        scrollbar-track-radius: 0;
        scrollbar-track-color: #f7f6f2;
        scrollbar-track-border-width: 0;
        scrollbar-min-thumb-length: 16px;
        scrollbar-thumb-width: 15px;
        scrollbar-thumb-color: #c3d4fd;
        scrollbar-thumb-hover-color: #d9e5ff;
        scrollbar-thumb-active-color: #a9c1f5;
        scrollbar-thumb-border-width: 1px;
        scrollbar-thumb-border-color: #9fb5e8;
        scrollbar-buttons: visible;
        scrollbar-button-length: 17px;
        scrollbar-button-radius: 3px;
        scrollbar-button-color: #4d6185;
        scrollbar-button-disabled-color: #c1c8d8;
        scrollbar-button-background: #c3d4fd;
        scrollbar-button-hover-background: #d9e5ff;
        scrollbar-button-active-background: #a9c1f5;
        scrollbar-button-step: 20px;
    }

    :focus-visible { outline: 1px dotted #000000; outline-offset: -4px; }
    :disabled { color: var(--nz-muted); }
    TextField:focus-visible, TextArea:focus-visible, Select:focus-visible, ComboBox:focus-visible, CheckBox:focus-visible,
    RadioButton:focus-visible, Switch:focus-visible, Slider:focus-visible, Link:focus-visible, Button[appearance="link"]:focus-visible { outline-offset: 1px; }

    /* Frame and caption. */
    :root[frame-owner="nazor"][client-titlebar] {
        -window-frame-border: 3px solid var(--winxp-frame);
        -window-frame-border-radius: 8px 8px 0 0;
        -window-frame-box-shadow: 2px 3px 6px #00000055;
    }
    :root[frame-owner="nazor"][client-titlebar]:window-inactive { -window-frame-border-color: #7b97e0; }
    TitleBar {
        gap: 5px;
        padding-left: 5px;
        background: linear-gradient(180deg, #0058ee 0%, #3a93ff 4%, #288eff 6%, #127dff 8%, #036ffc 10%, #0262ee 14%, #0057e5 20%,
            #0054e3 24%, #0055eb 56%, #005bf5 66%, #026afe 76%, #0062ef 86%, #0052d6 92%, #0040ab 94%, #003092 100%);
        color: #ffffff;
    }
    TitleBar[source="nazor"][height="standard"] { min-height: 26px; }
    TitleBar[source="nazor"] TitleBarAppIcon > Image { width: 16px; height: 16px; }
    TitleBarTitle { font-family: "Trebuchet MS", Tahoma, sans-serif; font-size: 13px; font-weight: 700; }
    :root:window-inactive TitleBar {
        background: linear-gradient(180deg, #7697e7 0%, #7e9ee3 3%, #94afe8 6%, #97b4e9 8%, #82a5e4 14%, #7c9fe2 17%, #7996de 25%,
            #7b99e1 56%, #82a9e9 81%, #80a5e7 89%, #7b96e1 94%, #7a93df 97%, #abbae3 100%);
        color: #d8e4f8;
    }
    CaptionButtons { align-items: center; gap: 2px; padding: 0 5px 0 0; }
    CaptionButtonsButton, CaptionButtonsButton:hover:not(:disabled), CaptionButtonsButton:active:not(:disabled) {
        width: 21px;
        min-height: 21px;
        height: 21px;
        padding: 0;
        border: 1px solid #ffffff;
        border-radius: 3px;
        color: #ffffff;
        background: radial-gradient(circle at 90% 90%, #0054e9 0%, #2263d5 55%, #4479e4 70%, #a3bbec 90%, #ffffff 100%);
    }
    CaptionButtonsButton:hover:not(:disabled) { background: radial-gradient(circle at 90% 90%, #0b68ff 0%, #3c7cf0 55%, #5f93f7 70%, #c2d6fb 90%, #ffffff 100%); }
    CaptionButtonsButton:active:not(:disabled) { background: radial-gradient(circle at 10% 10%, #0b3db9 0%, #1d4cbe 55%, #3a64cf 70%, #8fa8e3 100%); }
    CaptionButtonsButton[kind="close"], CaptionButtonsButton[kind="close"]:hover:not(:disabled), CaptionButtonsButton[kind="close"]:active:not(:disabled) {
        margin-left: 2px;
        background: radial-gradient(circle at 90% 90%, #cc4600 0%, #dc6527 55%, #cd7546 70%, #ffccb2 90%, #ffffff 100%);
        color: #ffffff;
    }
    CaptionButtonsButton[kind="close"]:hover:not(:disabled) { background: radial-gradient(circle at 90% 90%, #e45a12 0%, #f0773b 55%, #e98b5c 70%, #ffdcca 90%, #ffffff 100%); }
    CaptionButtonsButton[kind="close"]:active:not(:disabled) { background: radial-gradient(circle at 10% 10%, #a83400 0%, #b94713 60%, #c86a3f 100%); }
    :root:window-inactive CaptionButtonsButton:not(:hover) { opacity: 0.6; }
    CaptionButtonsGlyph { stroke-width: 2px; }
    CaptionButtonsGlyph[kind="minimize"] { width: 7px; height: 3px; margin-top: 7px; margin-right: 3px; stroke-trim-end: 0; background: currentColor; }
    CaptionButtonsGlyph[kind="maximize"] { width: 10px; height: 10px; border: 1px solid currentColor; border-top-width: 3px; }
    CaptionButtonsGlyph[kind="restore"] { width: 7px; height: 7px; margin: 3px 3px 0 0; border: 1px solid currentColor; border-top-width: 2px; }
    CaptionButtonsGlyphBack { left: 2px; top: -5px; width: 7px; height: 7px; border-top-width: 2px; }
    CaptionButtonsGlyph[kind="close"] { width: 10px; height: 10px; }
    CaptionButtonsGlyphStroke { left: -2px; top: 4px; width: 14px; height: 2px; }

    /* Bars. */
    MenuBar { padding: 0 2px; border-bottom: 0; background: var(--nz-surface); }
    MenuBarItem { padding: 3px 7px; }
    MenuBarItem:hover, MenuBarItem:focus, MenuBarItem[expanded] { background: var(--nz-selection); color: var(--nz-on-selection); }
    Toolbar { background: var(--nz-surface); }
    ToolbarSeparator { margin: 3px 3px; background: #c5c2b2; border-right: 1px solid #ffffff; }
    StatusBar { min-height: 22px; border-top: 1px solid #ffffff; background: var(--nz-surface); font-size: 11px; }
    StatusBarSection + StatusBarSection { border-left: 1px solid #aca899; box-shadow: inset 1px 0 0 #ffffff; }
    StatusBarSizeGrip { background: radial-gradient(circle at 50% 50%, #aca899 0, #aca899 1px, transparent 1.5px); background-size: 4px 4px; }

    /* Push buttons. */
    Button, ToggleButton, MenuButton, SplitButton { min-height: 23px; padding: 2px 10px; border-radius: 3px; }
    Button:where([appearance="standard"]:not([icon-only], [shape="square"], [shape="circle"], [size="small"], [size="large"])) { min-width: 75px; }
    Button:where(:is([appearance="standard"], [appearance="accent"], [appearance="tonal"], [appearance="outline"]):not(:disabled)),
    ToggleButton:where(:is([appearance="standard"], [appearance="accent"], [appearance="tonal"], [appearance="outline"]):not(:disabled)),
    MenuButton:where(:not([appearance="subtle"], [appearance="link"], :disabled)) {
        border: 1px solid var(--winxp-button-edge);
        background: linear-gradient(180deg, #ffffff 0%, #fbfbf9 30%, #f2f1ea 60%, #ecebe6 86%, #d6d0c5 100%);
        color: #000000;
        box-shadow: none;
    }
    :is(Button, ToggleButton):is([appearance="accent"], [appearance="tonal"]):not(:disabled, [pressed], :active, :hover) {
        border-color: var(--winxp-button-edge);
        background: linear-gradient(180deg, #ffffff 0%, #fbfbf9 30%, #f2f1ea 60%, #ecebe6 86%, #d6d0c5 100%);
        color: #000000;
    }
    :is(Button, ToggleButton)[appearance="accent"] { font-weight: 700; }
    Button:hover:where(:not([appearance="subtle"], [appearance="link"], :disabled)),
    ToggleButton:hover:where(:not([appearance="subtle"], [appearance="link"], :disabled)),
    MenuButton:hover:where(:not([appearance="subtle"], [appearance="link"], :disabled)) {
        border-color: var(--winxp-button-edge);
        color: #000000;
        box-shadow: inset 0 2px 0 #ffecb5, inset 0 -2px 0 #f8b330, inset 2px 0 0 #fcd57b, inset -2px 0 0 #fcd57b;
    }
    /* The default button and the focused button carry the blue inner glow. */
    Button[default]:where(:not(:disabled, :hover, :active, [intent="danger"])),
    Button:focus-visible:where(:not(:disabled, :hover, :active, [appearance="subtle"], [appearance="link"])) {
        border-color: var(--winxp-button-edge);
        background: linear-gradient(180deg, #ffffff 0%, #fbfbf9 30%, #f2f1ea 60%, #ecebe6 86%, #d6d0c5 100%);
        color: #000000;
        box-shadow: inset 0 2px 0 #cee7ff, inset 0 -2px 0 #6982ee, inset 2px 0 0 #a4c4f4, inset -2px 0 0 #a4c4f4;
    }
    Button:active:where(:not([appearance="link"], :disabled)),
    ToggleButton:active:where(:not([appearance="link"], :disabled)),
    ToggleButton[pressed]:where(:not([appearance="link"], :disabled)),
    MenuButton:active:where(:not(:disabled)), MenuButton[expanded] {
        border-color: var(--winxp-button-edge);
        background: linear-gradient(180deg, #cdcac3 0%, #e3e2da 12%, #e5e4de 70%, #f2f1ec 100%);
        color: #000000;
        box-shadow: none;
    }
    Button[intent="danger"]:not(:disabled), Button[intent="danger"][appearance="accent"]:not(:disabled) { color: var(--nz-danger); }
    Button[intent="danger"][appearance="accent"]:not(:disabled, :hover, :active) {
        border-color: var(--winxp-button-edge);
        background: linear-gradient(180deg, #ffffff 0%, #fbfbf9 30%, #f2f1ea 60%, #ecebe6 86%, #d6d0c5 100%);
    }
    Button:disabled, ToggleButton:disabled, MenuButton:disabled { border-color: #c9c7ba; background: #f5f4ea; color: var(--nz-muted); }
    Button[appearance="subtle"], ToggleButton[appearance="subtle"], MenuButton[appearance="subtle"] { border-radius: 3px; }
    Button[appearance="subtle"]:hover:not(:disabled), ToggleButton[appearance="subtle"]:hover:not(:disabled) { border-color: #cecebf; background: #f6f5f0; }

    /* Fields. */
    :is(TextField, TextArea, Select, NumberField, ComboBox, SearchField, PasswordField, TokenField, MaskedField, ShortcutField,
        DateField, TimeField, FileField, SegmentedDatePicker, SegmentedTimePicker, DatePicker, TimePicker):where([appearance="outline"]) {
        min-height: 21px;
        padding: 1px 3px;
        border: 1px solid var(--winxp-field-edge);
        border-radius: 0;
        background: var(--nz-field);
    }
    :is(TextField, TextArea, Select, NumberField, ComboBox, SearchField, PasswordField, TokenField, MaskedField, ShortcutField):focus-within { border-color: var(--winxp-field-edge); }
    :is(TextField, TextArea, Select, NumberField, ComboBox, SearchField, PasswordField, TokenField, MaskedField, ShortcutField):disabled { border-color: #c9c7ba; background: #ebebe4; color: var(--nz-muted); }
    TextField[appearance="filled"], TextArea[appearance="filled"], Select[appearance="filled"] { border: 1px solid #c9c7ba; background: #f4f3ee; }
    Select { padding-right: 1px; }
    Select:focus-visible > SelectValue { background: var(--nz-selection); color: var(--nz-on-selection); }
    SelectChevron, ComboBoxChevron, NumberFieldIncrement, NumberFieldDecrement {
        border: 1px solid #b9cbf3;
        border-radius: 2px;
        background: linear-gradient(180deg, #e2eafe, #c5d5fc 60%, #b3c7f9);
        color: #4d6185;
        stroke-width: 2px;
    }
    SelectChevron, ComboBoxChevron { width: 17px; height: 17px; padding: 4px; }
    Select:hover > SelectChevron, ComboBox:hover > ComboBoxChevron, NumberFieldIncrement:hover, NumberFieldDecrement:hover {
        background: linear-gradient(180deg, #f3f7ff, #d9e5ff 60%, #c7d7fd);
    }
    Select[expanded] > SelectChevron, ComboBox[expanded] > ComboBoxChevron { background: linear-gradient(180deg, #a6bdf2, #c3d3fa); }
    NumberFieldIncrement:disabled, NumberFieldDecrement:disabled { border-color: #e2e1d8; background: #f4f3ee; color: #c9c7ba; }

    /* Check boxes and radio buttons: navy frames, shaded wells and green marks; hovering adds the orange ring. */
    CheckBoxMark, RadioButtonMark {
        width: 13px;
        height: 13px;
        border: 1px solid var(--winxp-mark-edge);
        background: linear-gradient(135deg, #dcdcd7 0%, #f4f4f0 50%, #ffffff 100%);
        color: var(--winxp-check);
        stroke-width: 2.5px;
    }
    CheckBoxMark { border-radius: 0; }
    CheckBox:is(:checked, [indeterminate]):not(:disabled) > CheckBoxMark,
    RadioButton:checked:not(:disabled) > RadioButtonMark {
        border-color: var(--winxp-mark-edge);
        background: linear-gradient(135deg, #dcdcd7 0%, #f4f4f0 50%, #ffffff 100%);
        color: var(--winxp-check);
    }
    CheckBox:hover:not(:disabled) > CheckBoxMark, RadioButton:hover:not(:disabled) > RadioButtonMark {
        border-color: var(--winxp-mark-edge);
        box-shadow: inset 0 0 0 1px #fcd57b, inset 0 0 0 2px #f8b330;
    }
    CheckBox:active:not(:disabled) > CheckBoxMark, RadioButton:active:not(:disabled) > RadioButtonMark { background: linear-gradient(135deg, #b0b0a7, #e3e3db); }
    CheckBox:disabled > CheckBoxMark, RadioButton:disabled > RadioButtonMark { border-color: #cac8bb; background: #ffffff; color: #cac8bb; }
    RadioButtonMark { border-radius: 50%; }
    RadioButtonDot { width: 5px; height: 5px; }
    RadioButton:checked:not(:disabled) > RadioButtonMark > RadioButtonDot { background: radial-gradient(circle at 35% 35%, #a2f0a2, #2db82d 60%, #158415); }

    SwitchTrack { height: 15px; border: 1px solid var(--winxp-mark-edge); border-radius: 3px; background: linear-gradient(135deg, #dcdcd7, #ffffff); }
    SwitchThumb { border-radius: 2px; background: linear-gradient(180deg, #ffffff, #d6d0c5); box-shadow: 0 0 0 1px var(--winxp-button-edge); }
    Switch:checked > SwitchTrack { border-color: var(--winxp-mark-edge); background: linear-gradient(180deg, #9be59b, #3ec43e 50%, #21a121); }

    /* Slider: a sunken groove and a raised thumb with a green foot. */
    SliderTrack, RangeSliderTrack { top: 9px; height: 4px; border: 1px solid; border-color: #9d9c99 #ffffff #ffffff #9d9c99; border-radius: 0; background: #f4f3ee; }
    SliderFill, RangeSliderFill { display: none; }
    SliderThumb, RangeSliderThumb {
        top: 1px;
        width: 11px;
        height: 21px;
        border: 1px solid #77889d;
        border-radius: 2px;
        background: linear-gradient(180deg, #ffffff, #ecebe6 70%, #d6d0c5);
        box-shadow: inset 0 -3px 0 #21a121;
    }
    SliderThumb { left: calc(var(--nz-slider-position) * (100% - 11px)); }
    Slider:hover > SliderThumb { border-color: #77889d; box-shadow: inset 0 -3px 0 #f8b330; }
    Slider[orientation="vertical"] > SliderThumb { width: 21px; height: 11px; left: 1px; box-shadow: inset -3px 0 0 #21a121; }

    /* Progress: green blocks in a white rounded trough. */
    ProgressBarTrack { height: 15px; padding: 2px; border: 1px solid #686868; border-radius: 3px; background: #ffffff; }
    ProgressBarFill:not([indeterminate]), ProgressBarSegment[filled] {
        top: 2px;
        bottom: 2px;
        left: 2px;
        background: repeating-linear-gradient(90deg, transparent 0px, transparent 1px, #ffffff 1px, #ffffff 3px, transparent 3px, transparent 9px),
            linear-gradient(180deg, #acedad 0%, #7be47d 14%, #4cda50 28%, #2ed330 42%, #42d845 57%, #76e275 71%, #8fe791 85%, #ffffff 100%);
    }
    ProgressBarFill:not([indeterminate]) { width: calc(var(--nz-progress-fraction, 0) * (100% - 4px)); }
    ProgressBar[intent="accent"] { --nz-feedback-color: #2ed330; }

    /* Lists and tables. */
    ListView, TreeView, ListBox, GridView { border: 1px solid var(--winxp-field-edge); background: var(--nz-field); }
    ListViewItem:hover, TreeViewItem:hover, GridViewItem:hover { background: transparent; color: inherit; }
    TableHeaderCell, DataGridColumnHeader {
        background: linear-gradient(180deg, #ffffff, #f6f5f0 70%, #ebeadb);
        border-bottom: 3px solid #d6d2c2;
        border-right: 1px solid #c7c5b2;
    }

    /* Tabs: rounded white-to-beige tabs; the selected tab carries the orange top line and joins the white page. */
    TabsHeader { border-bottom: 1px solid var(--winxp-tab-edge); }
    Tab, Tabs[presentation="document"] Tab {
        margin-top: 2px;
        padding: 2px 8px;
        border: 1px solid var(--winxp-tab-edge);
        border-bottom-width: 0;
        border-radius: 3px 3px 0 0;
        background: linear-gradient(180deg, #ffffff, #f3f3ef 70%, #ecebe5);
    }
    Tab:hover { border-top: 2px solid #ffc83c; background: linear-gradient(180deg, #ffffff, #f3f3ef 70%, #ecebe5); color: #000000; }
    Tab[selected]:where(:not(:disabled)) {
        margin-top: 0;
        margin-bottom: -1px;
        padding-bottom: 4px;
        border-top: 3px solid #e68b2c;
        background: #fcfcfe;
        color: #000000;
    }
    TabsIndicator { display: none; }

    /* Group boxes, cards and expanders. */
    GroupBox, Card:where([appearance="outline"]) { border: 1px solid var(--winxp-group-edge); border-radius: 4px; }
    GroupBoxHeader { color: #0046d5; font-weight: 400; }
    Card:where([appearance="standard"]), Expander, AccordionItem, SegmentedControl { border: 1px solid var(--winxp-group-edge); border-radius: 4px; background: #fcfcfe; }
    ExpanderHeader, AccordionItemHeader { background: linear-gradient(90deg, #ffffff, #c6d3f7); color: #215dc6; font-weight: 700; }
    ExpanderHeader:hover, AccordionItemHeader:hover { color: #428eff; }
    Separator { background: #d0d0bf; }

    /* Menus: white surfaces with a beige frame and #316ac5 highlight. */
    Menu, ContextMenu, SelectPopup, ComboBoxPopup, SearchFieldPopup {
        padding: 2px;
        border: 1px solid var(--winxp-menu-edge);
        border-radius: 0;
        background: #ffffff;
        box-shadow: 2px 2px 2px rgba(0, 0, 0, 0.3);
    }
    SelectPopup, ComboBoxPopup, SearchFieldPopup { padding: 0; border-color: #000000; box-shadow: none; }
    MenuItem, MenuCheckItem, MenuRadioItem, Submenu { min-height: 17px; padding: 1px 18px; }
    MenuItem[intent="danger"]:is(:hover, :focus):where(:not(:disabled)) { color: var(--nz-on-selection); }
    MenuItem:is(:hover, :focus):not(:disabled) > MenuItemDescription { color: inherit; }
    MenuSeparator { height: 1px; margin: 3px 1px; background: var(--winxp-menu-edge); }
    Popover, PopoverContainer, Toast, Dialog, DialogContainer, AlertDialog, CommandPalette {
        border: 1px solid var(--winxp-frame);
        border-radius: 4px;
        background: var(--nz-surface);
        box-shadow: 2px 3px 6px rgba(0, 0, 0, 0.35);
    }
    DialogTitle, AlertDialogTitle { color: #003399; font-family: "Trebuchet MS", Tahoma, sans-serif; }
    Tooltip, TooltipContainer { padding: 2px 4px; border: 1px solid #000000; border-radius: 0; background: var(--winxp-info); color: #000000; box-shadow: 2px 2px 2px rgba(0, 0, 0, 0.25); }
    Dialog:where([size="fullscreen"], [dialog-placement]) { border-radius: 0; }
}
