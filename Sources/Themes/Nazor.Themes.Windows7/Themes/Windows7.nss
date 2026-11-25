/*
 * Windows 7: the Aero visual style with the Sky frame color. A light-blue glass frame without blur, Segoe UI 9 pt,
 * #f0f0f0 dialogs, two-tone glossy push buttons that turn blue under the pointer, #abadb3 field frames, the green
 * progress bar, the light-blue item selection and the grouped caption buttons that hang from the top edge.
 */
@media (forced-colors: none) {
    :root {
        font-family: "Segoe UI", Tahoma, "Liberation Sans", sans-serif;
        font-size: 12px;
        --nz-surface: #f0f0f0;
        --nz-on-surface: #000000;
        --nz-control: #ececec;
        --nz-on-control: #000000;
        --nz-border: #707070;
        --nz-field: #ffffff;
        --nz-on-field: #000000;
        --nz-accent: #3399ff;
        --nz-on-accent: #ffffff;
        --nz-selection: #3399ff;
        --nz-on-selection: #ffffff;
        --nz-link: #0066cc;
        --nz-muted: #6d6d6d;
        --nz-danger: #c42b1c;
        --win7-glass: #9dbfe6;
        --win7-glass-edge: #5c7ea6;
        --win7-button: linear-gradient(180deg, #f2f2f2 0%, #ebebeb 50%, #dddddd 51%, #cfcfcf 100%);
        --win7-button-hover: linear-gradient(180deg, #eaf6fd 0%, #d9f0fc 50%, #bee6fd 51%, #a7d9f5 100%);
        --win7-button-pressed: linear-gradient(180deg, #e5f4fc 0%, #c4e5f6 50%, #98d1ef 51%, #68b3db 100%);
        --win7-item-hover: linear-gradient(180deg, #fafbfd, #ebf3fd);
        --win7-item-selected: linear-gradient(180deg, #dcebfc, #c1dbfc);
        scrollbar-visibility: visible;
        scrollbar-placement: reserve;
        scrollbar-width: 17px;
        scrollbar-inset: 0;
        scrollbar-radius: 3px;
        scrollbar-track-radius: 0;
        scrollbar-min-thumb-length: 18px;
        scrollbar-track-color: #ebebeb;
        scrollbar-track-hover-color: #e4e4e4;
        scrollbar-thumb-width: 15px;
        scrollbar-thumb-color: #e5e5e5;
        scrollbar-thumb-hover-color: #d8eefb;
        scrollbar-thumb-active-color: #b5dcf3;
        scrollbar-thumb-border-width: 1px;
        scrollbar-thumb-border-color: #9a9a9a;
        scrollbar-buttons: visible;
        scrollbar-button-length: 17px;
        scrollbar-button-radius: 3px;
        scrollbar-button-color: #4d4d4d;
        scrollbar-button-disabled-color: #b2b2b2;
        scrollbar-button-background: #ebebeb;
        scrollbar-button-hover-background: #d8eefb;
        scrollbar-button-active-background: #b5dcf3;
        scrollbar-button-step: 20px;
    }

    :focus-visible { outline: 1px dotted #000000; outline-offset: -4px; }
    :disabled { color: #838383; }
    TextField:focus-visible, TextArea:focus-visible, Select:focus-visible, CheckBox:focus-visible, RadioButton:focus-visible,
    Switch:focus-visible, Slider:focus-visible, Link:focus-visible, Button[appearance="link"]:focus-visible { outline-offset: 1px; }

    /* Glass frame: the caption and an 8-pixel border share the Sky glass; the caption buttons hang from the top edge. */
    :root[frame-owner="nazor"][client-titlebar] {
        -window-frame-border: 1px solid var(--win7-glass-edge);
        -window-frame-border-radius: 6px;
        -window-frame-box-shadow: 0 6px 22px #00000066, 0 0 3px #00000033;
    }
    :root[frame-owner="nazor"][client-titlebar]:window-inactive {
        -window-frame-border-color: #8ea4bd;
        -window-frame-box-shadow: 0 4px 14px #00000040;
    }
    :root[frame-owner="nazor"][client-titlebar][window-state="normal"] > WindowContent {
        padding: 0 7px 7px;
        background: linear-gradient(180deg, #b3cfee, var(--win7-glass) 160px);
        box-shadow: inset 0 0 0 1px rgba(255, 255, 255, 0.55);
    }
    :root[frame-owner="nazor"][client-titlebar]:window-inactive > WindowContent { background: #d7e4f2; }
    TitleBar {
        gap: 6px;
        padding-left: 4px;
        background: linear-gradient(110deg, transparent 0%, transparent 18%, rgba(255, 255, 255, 0.22) 21%, rgba(255, 255, 255, 0.22) 25%, transparent 29%,
                transparent 62%, rgba(255, 255, 255, 0.16) 64%, rgba(255, 255, 255, 0.16) 66%, transparent 68%),
            linear-gradient(180deg, #bcd6f1, #a6c5e9);
        color: #000000;
    }
    :root:window-inactive TitleBar { background: linear-gradient(180deg, #e3edf7, #d7e4f2); color: #4a5866; }
    TitleBar[source="nazor"][height="standard"] { min-height: 30px; align-items: flex-start; }
    TitleBar[source="nazor"] > TitleBarAppIcon, TitleBar[source="nazor"] > TitleBarHeading { align-self: center; }
    TitleBar[source="nazor"] TitleBarAppIcon > Image { width: 16px; height: 16px; }
    TitleBarTitle { font-size: 12px; font-weight: 400; }
    CaptionButtons { align-items: flex-start; align-self: flex-start; gap: 0; padding: 0 6px 0 0; }
    CaptionButtonsButton, CaptionButtonsButton:hover:not(:disabled), CaptionButtonsButton:active:not(:disabled) {
        width: 26px;
        min-height: 19px;
        height: 19px;
        padding: 0;
        border: 1px solid rgba(23, 45, 75, 0.7);
        border-top-width: 0;
        border-left-width: 0;
        border-radius: 0;
        background: linear-gradient(180deg, rgba(255, 255, 255, 0.6), rgba(255, 255, 255, 0.3) 50%, rgba(140, 175, 215, 0.35) 51%, rgba(255, 255, 255, 0.45));
        color: #ffffff;
        box-shadow: inset 0 0 0 1px rgba(255, 255, 255, 0.5);
    }
    CaptionButtonsButton[kind="minimize"] { border-left-width: 1px; border-radius: 0 0 0 4px; }
    CaptionButtonsButton:hover:not(:disabled) {
        background: linear-gradient(180deg, #b6dcfa, #7fbbe9 50%, #3d8fd7 51%, #79c6f6);
        box-shadow: inset 0 0 0 1px rgba(255, 255, 255, 0.6), 0 0 6px #8fd3ff;
    }
    CaptionButtonsButton:active:not(:disabled) { background: linear-gradient(180deg, #5c9ed6, #3a7bbf 50%, #19569e 51%, #3b85c9); }
    CaptionButtonsButton[kind="close"], CaptionButtonsButton[kind="close"]:hover:not(:disabled), CaptionButtonsButton[kind="close"]:active:not(:disabled) {
        width: 43px;
        border-radius: 0 0 4px 0;
        background: linear-gradient(180deg, #e8a493, #d67a68 50%, #c2422b 51%, #d26e4b);
        color: #ffffff;
    }
    CaptionButtonsButton[kind="close"]:hover:not(:disabled) { background: linear-gradient(180deg, #f5b6a5, #ea8b74 50%, #dc4b2c 51%, #f28c63); box-shadow: inset 0 0 0 1px rgba(255, 255, 255, 0.6), 0 0 6px #ff9c80; }
    CaptionButtonsButton[kind="close"]:active:not(:disabled) { background: linear-gradient(180deg, #c3735f, #aa4d3a 50%, #8c2410 51%, #a94a2d); }
    :root:window-inactive CaptionButtonsButton:not(:hover), :root:window-inactive CaptionButtonsButton[kind="close"]:not(:hover) {
        background: linear-gradient(180deg, rgba(255, 255, 255, 0.7), rgba(255, 255, 255, 0.4));
        color: #6b7a8c;
    }
    CaptionButtonsGlyph { stroke-width: 2px; }
    CaptionButtonsGlyph[kind="minimize"] { width: 9px; height: 3px; margin-top: 6px; stroke-trim-end: 0; background: currentColor; box-shadow: 0 0 0 1px #34495e; }
    CaptionButtonsGlyph[kind="maximize"] { width: 9px; height: 7px; border: 1px solid currentColor; border-top-width: 2px; box-shadow: 0 0 0 1px #34495e, inset 0 0 0 1px #34495e; }
    CaptionButtonsGlyph[kind="restore"] { width: 7px; height: 6px; margin: 3px 3px 0 0; border: 1px solid currentColor; border-top-width: 2px; }
    CaptionButtonsGlyph[kind="close"] { width: 11px; height: 9px; }
    CaptionButtonsGlyphStroke { left: -1px; top: 3px; width: 13px; height: 2px; box-shadow: 0 0 0 1px #4e1a10; }

    /* Bars. */
    MenuBar { padding: 1px 2px; border-bottom: 1px solid #b6bcc6; background: linear-gradient(180deg, #ffffff, #f1f5fb 40%, #dee6f0); }
    MenuBarItem { padding: 2px 7px; border: 1px solid transparent; border-radius: 3px; }
    MenuBarItem:hover, MenuBarItem:focus, MenuBarItem[expanded] { border-color: #a8d8eb; background: var(--win7-item-hover); color: #000000; }
    Toolbar { background: linear-gradient(180deg, #f5f9fd, #e9f0f8 50%, #dce6f4 51%, #dde9f7); }
    ToolbarSeparator { background: #b4c4d8; border-right: 1px solid #ffffff; }
    StatusBar { min-height: 23px; border-top: 1px solid #b6c4d5; background: linear-gradient(180deg, #f5f9fd, #e5edf6); color: #1e395b; }
    StatusBarSection + StatusBarSection { border-left: 1px solid #c3d0df; }

    /* Push buttons. */
    Button, ToggleButton, MenuButton, SplitButton { min-height: 23px; padding: 2px 10px; border-radius: 3px; }
    Button:where([appearance="standard"]:not([icon-only], [shape="square"], [shape="circle"], [size="small"], [size="large"])) { min-width: 75px; }
    Button:where(:is([appearance="standard"], [appearance="accent"], [appearance="tonal"], [appearance="outline"]):not(:disabled)),
    ToggleButton:where(:is([appearance="standard"], [appearance="accent"], [appearance="tonal"], [appearance="outline"]):not(:disabled)),
    MenuButton:where(:not([appearance="subtle"], [appearance="link"], :disabled)) {
        border: 1px solid #707070;
        background: var(--win7-button);
        color: #000000;
        box-shadow: inset 0 0 0 1px #fcfcfc;
    }
    :is(Button, ToggleButton):is([appearance="accent"], [appearance="tonal"]):not(:disabled, [pressed], :active, :hover) {
        border-color: #707070;
        background: var(--win7-button);
        color: #000000;
    }
    Button:hover:where(:not([appearance="subtle"], [appearance="link"], :disabled)),
    ToggleButton:hover:where(:not([appearance="subtle"], [appearance="link"], :disabled)),
    MenuButton:hover:where(:not([appearance="subtle"], [appearance="link"], :disabled)) {
        border-color: #3c7fb1;
        background: var(--win7-button-hover);
        color: #000000;
    }
    Button[default]:where(:not(:disabled, :hover, :active, [intent="danger"])),
    Button:focus-visible:where(:not(:disabled, :hover, :active, [appearance="subtle"], [appearance="link"])) {
        border-color: #3c7fb1;
        background: var(--win7-button);
        color: #000000;
        box-shadow: inset 0 0 0 1px #48d1f8, inset 0 0 0 2px rgba(255, 255, 255, 0.7);
    }
    Button:active:where(:not([appearance="link"], :disabled)),
    ToggleButton:active:where(:not([appearance="link"], :disabled)),
    ToggleButton[pressed]:where(:not([appearance="link"], :disabled)),
    MenuButton:active:where(:not(:disabled)), MenuButton[expanded] {
        border-color: #2c628b;
        background: var(--win7-button-pressed);
        color: #000000;
        box-shadow: inset 1px 1px 1px rgba(0, 0, 0, 0.35);
    }
    Button[intent="danger"]:not(:disabled), Button[intent="danger"][appearance="accent"]:not(:disabled) { color: var(--nz-danger); }
    Button[intent="danger"][appearance="accent"]:not(:disabled, :hover, :active) { border-color: #707070; background: var(--win7-button); }
    Button:disabled, ToggleButton:disabled, MenuButton:disabled { border-color: #adb2b5; background: #f4f4f4; color: #838383; }
    Button[appearance="subtle"]:hover:not(:disabled), ToggleButton[appearance="subtle"]:hover:not(:disabled), MenuButton[appearance="subtle"]:hover:not(:disabled) {
        border-color: #a9c4e4;
        background: var(--win7-item-hover);
    }

    /* Fields: soft gray frames that turn blue under the pointer and with the focus. Drop-down lists are buttons. */
    :is(TextField, TextArea, NumberField, ComboBox, SearchField, PasswordField, TokenField, MaskedField, ShortcutField,
        DateField, TimeField, FileField, SegmentedDatePicker, SegmentedTimePicker, DatePicker, TimePicker):where([appearance="outline"]) {
        min-height: 23px;
        padding: 1px 4px;
        border: 1px solid;
        border-color: #abadb3 #dbdfe6 #e3e9ef #e2e3ea;
        border-radius: 2px;
        background: var(--nz-field);
    }
    :is(TextField, TextArea, NumberField, ComboBox, SearchField, PasswordField, TokenField, MaskedField, ShortcutField):hover:where([appearance="outline"]:not(:disabled, :focus-within, [invalid])) {
        border-color: #5794bf #b7d5ea #c7e2f1 #c5daed;
    }
    :is(TextField, TextArea, NumberField, ComboBox, SearchField, PasswordField, TokenField, MaskedField, ShortcutField):focus-within:where([appearance="outline"]:not([invalid])) {
        border-color: #3d7bad #a4c9e3 #b7d9ed #b5cfe7;
    }
    :is(TextField, TextArea, NumberField, ComboBox, SearchField, PasswordField, TokenField, MaskedField, ShortcutField):disabled { border-color: #afafaf; background: #f4f4f4; }
    Select:where([appearance="outline"]) {
        min-height: 23px;
        padding: 1px 4px 1px 6px;
        border: 1px solid #707070;
        border-radius: 3px;
        background: var(--win7-button);
        color: #000000;
        box-shadow: inset 0 0 0 1px #fcfcfc;
    }
    Select:hover:where([appearance="outline"]:not(:disabled)) { border-color: #3c7fb1; background: var(--win7-button-hover); }
    Select[expanded]:where([appearance="outline"]) { border-color: #2c628b; background: var(--win7-button-pressed); }
    Select:disabled { border-color: #adb2b5; background: #f4f4f4; }
    SelectChevron, ComboBoxChevron { width: 14px; height: 14px; padding: 4px 3px; color: #000000; stroke-width: 1.5px; }
    ComboBox[expanded] > ComboBoxChevron, ComboBox:hover > ComboBoxChevron { border-radius: 0 2px 2px 0; background: var(--win7-button-hover); }
    NumberFieldIncrement, NumberFieldDecrement { border: 1px solid #b8bbbf; border-radius: 2px; background: var(--win7-button); }
    NumberFieldIncrement:hover, NumberFieldDecrement:hover { border-color: #3c7fb1; background: var(--win7-button-hover); }

    /* Check boxes and radio buttons: gray frames around a shaded well; hover turns them blue. */
    CheckBoxMark, RadioButtonMark {
        width: 13px;
        height: 13px;
        border: 1px solid #8e8f8f;
        background: linear-gradient(135deg, #dadada, #fbfbfb);
        color: #4b5d84;
        stroke-width: 2.5px;
        box-shadow: inset 0 0 0 2px #f4f4f4;
    }
    CheckBoxMark { border-radius: 0; }
    CheckBox:is(:checked, [indeterminate]):not(:disabled) > CheckBoxMark, RadioButton:checked:not(:disabled) > RadioButtonMark {
        border-color: #8e8f8f;
        background: linear-gradient(135deg, #dadada, #fbfbfb);
        color: #4b5d84;
    }
    CheckBox:hover:not(:disabled) > CheckBoxMark, RadioButton:hover:not(:disabled) > RadioButtonMark {
        border-color: #3c7fb1;
        background: linear-gradient(135deg, #b1dffd, #e9f7fe);
        box-shadow: inset 0 0 0 2px #def9fa;
    }
    CheckBox:active:not(:disabled) > CheckBoxMark, RadioButton:active:not(:disabled) > RadioButtonMark { border-color: #2c628b; background: linear-gradient(135deg, #7fc4ee, #e0f4fd); }
    CheckBox:disabled > CheckBoxMark, RadioButton:disabled > RadioButtonMark { border-color: #bcbcbc; background: #f4f4f4; color: #bcbcbc; }
    RadioButtonMark { border-radius: 50%; }
    RadioButtonDot { width: 5px; height: 5px; }
    RadioButton:checked:not(:disabled) > RadioButtonMark > RadioButtonDot { background: radial-gradient(circle at 40% 35%, #b6ecff, #2d8fd5 60%, #1f5c94); }

    SwitchTrack { height: 16px; border: 1px solid #8e8f8f; border-radius: 3px; background: linear-gradient(180deg, #dadada, #fbfbfb); }
    SwitchThumb { border-radius: 2px; background: var(--win7-button); box-shadow: 0 0 0 1px #707070; }
    Switch:checked > SwitchTrack { border-color: #2c628b; background: linear-gradient(180deg, #8fd0f4, #3c9ad6 50%, #1d78c1 51%, #4aa4e0); }

    /* Slider: a pale groove and a glossy thumb. */
    SliderTrack, RangeSliderTrack { top: 9px; height: 4px; border: 1px solid; border-color: #b0b0b0 #e7eaea #e7eaea #b0b0b0; border-radius: 1px; background: #e7eaea; }
    SliderFill, RangeSliderFill { display: none; }
    SliderThumb, RangeSliderThumb {
        top: 1px;
        width: 11px;
        height: 20px;
        border: 1px solid #707070;
        border-radius: 2px;
        background: var(--win7-button);
        box-shadow: inset 0 0 0 1px #fcfcfc;
    }
    SliderThumb { left: calc(var(--nz-slider-position) * (100% - 11px)); }
    Slider:hover > SliderThumb { border-color: #3c7fb1; background: var(--win7-button-hover); }
    Slider[orientation="vertical"] > SliderThumb { width: 20px; height: 11px; left: 1px; }

    /* Progress: the glossy green bar. */
    ProgressBarTrack { height: 15px; border: 1px solid #b2b2b2; border-radius: 3px; background: linear-gradient(180deg, #f3f3f3, #e2e2e2 50%, #d6d6d6 51%, #e6e6e6); box-shadow: inset 0 0 0 1px rgba(255, 255, 255, 0.7); }
    ProgressBarFill:not([indeterminate]), ProgressBarSegment[filled] { background: linear-gradient(180deg, #a0f0a8 0%, #37d64c 45%, #06b025 50%, #0aa524 75%, #2ccc46 100%); }
    ProgressBar[intent="accent"] { --nz-feedback-color: #06b025; }

    /* Lists, trees and tables use the Explorer item highlight. */
    ListView, TreeView, ListBox, GridView { border: 1px solid #828790; background: var(--nz-field); }
    ListViewItem, TreeViewItem, GridViewItem { border: 1px solid transparent; border-radius: 3px; }
    ListViewItem:hover, TreeViewItem:hover, GridViewItem:hover { border-color: #b8d6fb; background: var(--win7-item-hover); color: #000000; }
    ListViewItem[selected], TreeViewItem[selected], GridViewItem[selected] { border-color: #84acdd; background: var(--win7-item-selected); color: #000000; }
    TableHeaderCell, DataGridColumnHeader { background: linear-gradient(180deg, #ffffff, #f7f8fa 50%, #f1f2f4 51%, #eef0f3); border-right: 1px solid #e3e8f1; border-bottom: 1px solid #d5d5d5; color: #4c607a; font-weight: 400; }

    /* Tabs: glossy tabs over a white page; the selected tab rises and joins it. */
    TabsHeader { border-bottom: 1px solid #898c95; }
    Tab, Tabs[presentation="document"] Tab {
        margin-top: 2px;
        padding: 2px 8px;
        border: 1px solid #898c95;
        border-bottom-width: 0;
        border-radius: 2px 2px 0 0;
        background: var(--win7-button);
    }
    Tab:hover { border-color: #3c7fb1; background: var(--win7-button-hover); color: #000000; }
    Tab[selected]:where(:not(:disabled)) { margin-top: 0; margin-bottom: -1px; padding-bottom: 4px; border-color: #898c95; background: #ffffff; color: #000000; }
    TabsIndicator { display: none; }

    /* Group boxes, cards and expanders. */
    GroupBox, Card:where([appearance="outline"]) { border: 1px solid #d5dfe5; border-radius: 3px; box-shadow: inset 0 0 0 1px #ffffff; }
    Card:where([appearance="standard"]), Expander, AccordionItem, SegmentedControl { border: 1px solid #d5dfe5; border-radius: 3px; background: #ffffff; }
    ExpanderHeader, AccordionItemHeader { background: transparent; color: #1e3287; }
    ExpanderHeader:hover, AccordionItemHeader:hover { color: #3399ff; }
    Separator { background: #d5dfe5; }

    /* Menus: the gray surface with its icon gutter and the soft blue highlight. */
    Menu, ContextMenu {
        padding: 2px;
        border: 1px solid #979797;
        border-radius: 0;
        background: linear-gradient(90deg, #f1f1f1 0px, #f1f1f1 26px, #e2e3e3 26px, #e2e3e3 27px, #ffffff 27px, #ffffff 28px, #f0f0f0 28px);
        box-shadow: 2px 2px 3px rgba(0, 0, 0, 0.3);
    }
    MenuItem, MenuCheckItem, MenuRadioItem, Submenu { min-height: 22px; padding: 2px 20px 2px 32px; border: 1px solid transparent; border-radius: 3px; }
    :is(MenuItem, MenuCheckItem, MenuRadioItem, Submenu):is(:hover, :focus):where(:not(:disabled)), Submenu[expanded]:where(:not(:disabled)) {
        border-color: #aecff7;
        background: linear-gradient(180deg, rgba(232, 241, 251, 0.9), rgba(201, 224, 247, 0.9));
        color: #000000;
    }
    MenuItem[intent="danger"]:is(:hover, :focus):where(:not(:disabled)) { color: #000000; }
    MenuItem:is(:hover, :focus):not(:disabled) > MenuItemDescription { color: inherit; }
    MenuSeparator { height: 1px; margin: 3px 2px 3px 30px; background: #e0e0e0; box-shadow: 0 1px 0 #ffffff; }
    SelectPopup, ComboBoxPopup, SearchFieldPopup { padding: 0; border: 1px solid #646464; border-radius: 0; background: #ffffff; box-shadow: 2px 2px 3px rgba(0, 0, 0, 0.25); }
    :is(SelectOption, ComboBoxOption, SearchFieldOption):is(:hover, [current]):where(:not(:disabled)) { background: var(--nz-selection); color: var(--nz-on-selection); }
    Popover, PopoverContainer, Toast, Dialog, DialogContainer, AlertDialog, CommandPalette {
        border: 1px solid #8e9aa9;
        border-radius: 6px;
        background: var(--nz-surface);
        box-shadow: 0 5px 16px rgba(0, 0, 0, 0.35);
    }
    DialogTitle, AlertDialogTitle { color: #003399; font-weight: 400; font-size: 15px; }
    Tooltip, TooltipContainer {
        padding: 2px 5px;
        border: 1px solid #767676;
        border-radius: 3px;
        background: linear-gradient(180deg, #ffffff, #e4e5f0);
        color: #575757;
        box-shadow: 2px 2px 3px rgba(0, 0, 0, 0.25);
    }
    Dialog:where([size="fullscreen"], [dialog-placement]) { border-radius: 0; }
}
