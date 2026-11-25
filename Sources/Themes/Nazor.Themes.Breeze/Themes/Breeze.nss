@media (forced-colors: none) {
    :root {
        font-family: "Noto Sans", "Segoe UI", sans-serif;
        font-size: 13.333px;
        --nz-surface: #eff0f1;
        --nz-on-surface: #232629;
        --nz-control: #fcfcfc;
        --nz-on-control: #232629;
        --nz-border: #b4b6b8;
        --nz-field: #ffffff;
        --nz-on-field: #232629;
        --nz-accent: #3daee9;
        --nz-on-accent: #102a3a;
        --nz-selection: #3daee9;
        --nz-on-selection: #102a3a;
        --nz-link: #17668d;
        --nz-muted: #5e646b;
        --breeze-header: #dee0e2;
        --breeze-view: #ffffff;
        --breeze-location: #f7f7f7;
        --breeze-separator: #b9bbbd;
        --breeze-frame: #c6c8c9;
        --breeze-section: #7f8285;
        --breeze-hover: color-mix(in srgb, var(--nz-control), var(--nz-accent) 15%);
        --breeze-pressed: color-mix(in srgb, var(--nz-control), var(--nz-accent) 30%);
        --breeze-focus: #1879a7;
        --breeze-trough: color-mix(in srgb, var(--nz-surface), var(--nz-on-surface) 20%);
        scrollbar-placement: reserve;
        scrollbar-visibility: visible;
        scrollbar-width: 21px;
        scrollbar-inset: 0;
        scrollbar-track-color: var(--nz-surface);
        scrollbar-radius: 4px;
        scrollbar-min-thumb-length: 20px;
        scrollbar-thumb-width: 8px;
        scrollbar-thumb-expanded-width: 8px;
        scrollbar-thumb-color: color-mix(in srgb, var(--nz-surface), var(--nz-on-surface) 22%);
        scrollbar-thumb-border-width: 1px;
        scrollbar-thumb-border-color: color-mix(in srgb, var(--nz-surface), var(--nz-on-surface) 38%);
        scrollbar-thumb-hover-color: var(--nz-accent);
        scrollbar-thumb-active-color: var(--breeze-focus);
        scrollbar-buttons: none;
    }

    @media (prefers-color-scheme: dark) {
        :root {
            --nz-surface: #202326;
            --nz-on-surface: #fcfcfc;
            --nz-control: #292c30;
            --nz-on-control: #fcfcfc;
            --nz-border: #50555a;
            --nz-field: #141618;
            --nz-on-field: #fcfcfc;
            --nz-link: #67bfff;
            --nz-muted: #a1a9b1;
            --breeze-header: #292c30;
            --breeze-view: #141618;
            --breeze-location: #1d1f22;
            --breeze-separator: #535659;
            --breeze-frame: #4c4e51;
            --breeze-section: #a1a9b1;
            --breeze-focus: #3daee9;
        }

        Button[intent="danger"]:not([appearance="accent"], :disabled), MenuItem[intent="danger"]:not(:disabled),
        FieldError, LabelRequiredMark { color: #ff929a; }
    }

    :focus-visible { outline: 2px solid var(--breeze-focus); outline-offset: 1px; }
    :disabled { color: var(--nz-muted); }
    MenuItem[intent="danger"]:is(:hover, :focus):where(:not(:disabled)) { color: var(--nz-on-selection); }
    MenuItem:is(:hover, :focus):not(:disabled) > MenuItemDescription { color: inherit; }
    :root[frame-owner="nazor"][client-titlebar] {
        -window-frame-border: 1px solid var(--breeze-frame);
        -window-frame-border-radius: 4px 4px 2px 2px;
        -window-frame-box-shadow: 0 6px 28px #0000004d, 0 1px 4px #00000026;
    }
    TitleBar, TableHeaderCell { background: var(--breeze-header); }
    Toolbar, MenuBar { background: var(--nz-surface); }
    Toolbar { border-bottom: 1px solid var(--breeze-separator); }
    TitleBarTitle { font-size: 13.333px; }
    StatusBar { border-top: 1px solid var(--breeze-separator); background: var(--nz-surface); font-size: inherit; }
    MenuBarItem { padding: 4px 8px; border-radius: 3px; }
    MenuBarItem:hover, MenuBarItem:focus, MenuBarItem[expanded] { background: color-mix(in srgb, var(--nz-accent) 20%, transparent); color: var(--nz-on-surface); }
    ListView, TreeView, ListBox, GridView { background: var(--breeze-view); }
    ListViewItem, TreeViewItem, GridViewItem { border: 1px solid transparent; border-radius: 3px; }
    ListViewItem:hover, TreeViewItem:hover, GridViewItem:hover { border-color: color-mix(in srgb, var(--nz-accent) 60%, transparent); background: color-mix(in srgb, var(--nz-accent) 15%, transparent); color: var(--nz-on-surface); }
    ListViewItem[selected], TreeViewItem[selected], GridViewItem[selected] { border-color: var(--nz-accent); background: color-mix(in srgb, var(--nz-accent) 30%, transparent); color: var(--nz-on-surface); }
    TitleBar[source="nazor"] { position: relative; }
    TitleBar[source="nazor"][height="standard"] { min-height: max(var(--nz-window-controls-height, 28px), 28px); }
    TitleBar[source="nazor"] > TitleBarHeading {
        position: absolute;
        inset: 0 104px;
        justify-content: center;
        pointer-events: none;
    }
    :root[frame-owner="nazor"]:window-inactive TitleBar { color: var(--nz-muted); }
    CaptionButtons { align-items: center; padding: 0 3px; }
    CaptionButtons { gap: 4px; padding: 0 6px; }
    CaptionButtonsButton { width: 20px; min-height: 20px; height: 20px; border-radius: 50%; background: transparent; color: var(--nz-on-surface); }
    CaptionButtonsButton:hover:not(:disabled) { background: color-mix(in srgb, var(--nz-on-surface) 18%, transparent); color: var(--nz-on-surface); }
    CaptionButtonsButton:active:not(:disabled) { background: color-mix(in srgb, var(--nz-on-surface) 30%, transparent); color: var(--nz-on-surface); }
    CaptionButtonsButton[kind="close"]:hover:not(:disabled) { background: #da4453; color: #ffffff; }
    CaptionButtonsButton[kind="close"]:active:not(:disabled) { background: #b23542; color: #ffffff; }
    CaptionButtonsGlyph[kind="close"] { width: 9px; height: 9px; }
    CaptionButtonsGlyphStroke { left: -1px; top: 4px; width: 11px; height: 1.5px; }
    CaptionButtonsGlyph[kind="minimize"], CaptionButtonsGlyph[kind="maximize"] {
        width: 7px;
        height: 7px;
        border: 0;
        border-right: 1px solid currentColor;
        border-bottom: 1px solid currentColor;
        transform: rotate(45deg);
    }
    CaptionButtonsGlyph[kind="minimize"] { margin-top: -4px; stroke-trim-end: 0; }
    CaptionButtonsGlyph[kind="maximize"] { margin-top: 4px; transform: rotate(225deg); }

    Button, ToggleButton { min-height: 30px; padding: 6px; gap: 4px; border-radius: 5px; }
    Button:where([appearance="standard"]:not([icon-only], [shape="square"], [shape="circle"], [size="small"])),
    ToggleButton:where([appearance="standard"]:not([icon-only], [shape="square"], [shape="circle"], [size="small"])) { min-width: 84px; }
    Button:is([shape="circle"], [shape="square"]):where(:not([size="small"], [size="large"])),
    ToggleButton:is([shape="circle"], [shape="square"]):where(:not([size="small"], [size="large"])) { width: 30px; }
    Button[default]:where([appearance="standard"]:not(:disabled, [intent="danger"], [intent="warning"], [intent="success"])) {
        background: color-mix(in srgb, var(--nz-control), var(--nz-accent) 20%);
        border-color: color-mix(in srgb, var(--nz-border), var(--nz-accent) 50%);
        color: var(--nz-on-control);
    }
    ToggleButton[pressed]:where([appearance="standard"]:not(:disabled)) {
        background: color-mix(in srgb, var(--nz-control), var(--nz-on-control) 12.5%);
        color: var(--nz-on-control);
    }
    Button:is(:hover, :focus-visible):where([appearance="standard"]:not(:disabled)),
    ToggleButton:is(:hover, :focus-visible):where([appearance="standard"]:not(:disabled)),
    Button[default]:is(:hover, :focus-visible):where([appearance="standard"]:not(:disabled)) { border-color: var(--nz-accent); }
    Button:active:where([appearance="standard"]:not(:disabled, [default])),
    Button[default]:active:where([appearance="standard"]:not(:disabled, [intent="danger"], [intent="warning"], [intent="success"])),
    ToggleButton:active:where([appearance="standard"]:not(:disabled)) {
        background: var(--breeze-pressed);
        color: var(--nz-on-control);
        border-color: var(--nz-accent);
    }

    TextField, TextArea, Select, NumberField, ComboBox, SearchField, PasswordField, TokenField,
    MaskedField, ShortcutField, SegmentedDatePicker, SegmentedTimePicker { border-radius: 5px; }
    TextField, Select, NumberField, ComboBox, SearchField, PasswordField, TokenField,
    MaskedField, ShortcutField, SegmentedDatePicker, SegmentedTimePicker { min-height: 30px; }
    TextField:where([appearance="outline"]), TextArea:where([appearance="outline"]), Select:where([appearance="outline"]),
    NumberField:where([appearance="outline"]), ComboBox:where([appearance="outline"]), SearchField:where([appearance="outline"]),
    PasswordField:where([appearance="outline"]), TokenField:where([appearance="outline"]),
    MaskedField:where([appearance="outline"]), ShortcutField:where([appearance="outline"]) {
        padding-left: 6px;
        padding-right: 6px;
    }
    TextField:is(:hover, :focus-within):where([appearance="outline"]:not([invalid], :disabled)),
    TextArea:is(:hover, :focus-within):where([appearance="outline"]:not([invalid], :disabled)),
    Select:is(:hover, :focus-within):where([appearance="outline"]:not([invalid], :disabled)),
    NumberField:is(:hover, :focus-within):where([appearance="outline"]:not([invalid], :disabled)),
    ComboBox:is(:hover, :focus-within):where([appearance="outline"]:not([invalid], :disabled)),
    SearchField:is(:hover, :focus-within):where([appearance="outline"]:not([invalid], :disabled)),
    PasswordField:is(:hover, :focus-within):where([appearance="outline"]:not([invalid], :disabled)),
    TokenField:is(:hover, :focus-within):where([appearance="outline"]:not([invalid], :disabled)),
    MaskedField:is(:hover, :focus-within):where([appearance="outline"]:not([invalid], :disabled)),
    ShortcutField:is(:hover, :focus-within):where([appearance="outline"]:not([invalid], :disabled)) {
        border-color: var(--nz-accent);
    }

    CheckBox, RadioButton { column-gap: 4px; }
    CheckBoxMark, RadioButtonMark { width: 20px; height: 20px; }
    CheckBoxMark { border-radius: 4px; }
    CheckBox[size="large"] > CheckBoxMark, RadioButton[size="large"] > RadioButtonMark { width: 24px; height: 24px; }
    CheckBox:is(:hover, :focus-visible):where(:not(:disabled)) > CheckBoxMark,
    RadioButton:is(:hover, :focus-visible):where(:not(:disabled)) > RadioButtonMark { border-color: var(--nz-accent); }
    RadioButton:checked:where(:not(:disabled)) > RadioButtonMark { background: var(--nz-field); border-color: var(--nz-accent); }
    RadioButton:checked:where(:not(:disabled)) > RadioButtonMark > RadioButtonDot { background: var(--breeze-focus); }
    RadioButton:where(:not([size="small"], [size="large"])) RadioButtonDot { width: 9px; height: 9px; }

    SliderTrack, RangeSliderTrack, ProgressBarTrack { border-radius: 3px; background: var(--breeze-trough); }
    SliderFill, RangeSliderFill, ProgressBarFill { border-radius: 3px; }
    ProgressBarTrack { height: 6px; border: 0; }
    SliderTrack, RangeSliderRail, SliderTicks, RangeSliderTicks { left: 10px; right: 10px; }
    SliderThumb, RangeSliderThumb {
        top: 1px;
        width: 20px;
        height: 20px;
        border-radius: 50%;
        box-shadow: 0 1px 2px rgba(0, 0, 0, 0.18);
    }
    SliderThumb { left: calc(var(--nz-slider-position) * (100% - 20px)); }
    RangeSliderThumb { left: calc(var(--nz-range-thumb-position) * 100% - 10px); }
    Slider:is(:hover, :focus-visible):where(:not(:disabled, [invalid])) > SliderThumb,
    RangeSliderHandle:is(:hover, :focus-visible):where(:not(:disabled, [invalid])) > RangeSliderThumb {
        border-color: var(--breeze-focus);
        background: var(--breeze-hover);
    }
    RangeSliderHandle:focus-visible > RangeSliderThumb { outline-color: var(--breeze-focus); }
    Slider[orientation="vertical"] > SliderTrack,
    Slider[orientation="vertical"] > SliderTicks,
    RangeSlider[orientation="vertical"] > RangeSliderRail,
    RangeSlider[orientation="vertical"] > RangeSliderTicks { top: 10px; bottom: 10px; }
    Slider[orientation="vertical"] > SliderThumb { left: 1px; bottom: calc(var(--nz-slider-position) * (100% - 20px)); }
    RangeSlider[orientation="vertical"] > RangeSliderRail > RangeSliderHandle > RangeSliderThumb {
        left: 1px;
        bottom: calc(var(--nz-range-thumb-position) * 100% - 10px);
    }
    SliderValueLabel, RangeSliderValueLabel { bottom: 23px; left: -20px; }
    Slider[orientation="vertical"] > SliderThumb > SliderValueLabel,
    RangeSlider[orientation="vertical"] > RangeSliderRail > RangeSliderHandle > RangeSliderThumb > RangeSliderValueLabel { left: 24px; }

    Tab { min-height: 30px; gap: 8px; padding: 4px 8px; border-radius: 5px; }
    Tab[selected]:where(:not(:disabled)) { background: var(--breeze-hover); color: var(--nz-on-surface); }
    Tabs:where([presentation="navigation"][orientation="horizontal"]) Tab { min-width: 80px; }
    TabsIndicator { height: 3px; top: calc(var(--nz-indicator-y, 0px) + var(--nz-indicator-height, 0px) - 3px); }
    TabsList[orientation="vertical"] > TabsIndicator { width: 3px; left: calc(var(--nz-indicator-x, 0px) + var(--nz-indicator-width, 0px) - 3px); }
    Tabs[presentation="document"] { --nz-tab-min-width: 80px; }
    Tabs[presentation="document"] Tab { min-height: 34px; border-radius: 5px 5px 0 0; }
    Tabs[presentation="document"] Tab[selected]:where(:not([drop-target="true"], :disabled)) { box-shadow: inset 0 3px 0 var(--nz-accent); }
    Card, Expander, SegmentedControl { border-radius: 5px; }
    Menu, ContextMenu, SelectPopup, ComboBoxPopup, SearchFieldPopup, Popover, PopoverContainer,
    Tooltip, TooltipContainer, Toast, Dialog, DialogContainer, AlertDialog, CommandPalette {
        border-color: var(--nz-border);
        border-radius: 4px;
        box-shadow: 0 2px 6px rgba(0, 0, 0, 0.22);
    }
    Tooltip, TooltipContainer { background: var(--breeze-header); }
    Dialog:where([size="fullscreen"], [dialog-placement]) { border-radius: 0; }
}
