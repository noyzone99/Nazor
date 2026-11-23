@media (forced-colors: none) {
    :root {
        font-family: "Noto Sans", "Segoe UI", sans-serif;
        font-size: 13.333px;
        --nz-surface: #eff0f1;
        --nz-on-surface: #232629;
        --nz-control: #fcfcfc;
        --nz-on-control: #232629;
        --nz-border: #8d9297;
        --nz-field: #ffffff;
        --nz-on-field: #232629;
        --nz-accent: #3daee9;
        --nz-on-accent: #102a3a;
        --nz-selection: #3daee9;
        --nz-on-selection: #102a3a;
        --nz-link: #17668d;
        --nz-muted: #5e646b;
        --breeze-header: #dee0e2;
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
            --nz-border: #767d83;
            --nz-field: #141618;
            --nz-on-field: #fcfcfc;
            --nz-link: #67bfff;
            --nz-muted: #a1a9b1;
            --breeze-header: #292c30;
            --breeze-focus: #3daee9;
        }

        Button[intent="danger"]:not([appearance="accent"], :disabled), MenuItem[intent="danger"]:not(:disabled),
        FieldError, LabelRequiredMark { color: #ff929a; }
    }

    :focus-visible { outline: 2px solid var(--breeze-focus); outline-offset: 1px; }
    :disabled { color: var(--nz-muted); }
    MenuItem[intent="danger"]:is(:hover, :focus):where(:not(:disabled)) { color: var(--nz-on-selection); }
    MenuItem:is(:hover, :focus):not(:disabled) > MenuItemDescription { color: inherit; }
    TitleBar, Toolbar, MenuBar, TableHeaderCell { background: var(--breeze-header); }
    TitleBar, Toolbar { border-bottom: 1px solid var(--nz-border); }

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
