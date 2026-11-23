@media (forced-colors: none) {
    :root {
        font-family: "Adwaita Sans", Inter, Cantarell, sans-serif;
        font-size: 14.667px;
        --nz-surface: #fafafb;
        --nz-on-surface: #323237;
        --nz-control: color-mix(in srgb, var(--nz-on-surface) 10%, transparent);
        --nz-on-control: var(--nz-on-surface);
        --nz-border: rgba(0, 0, 6, 0.12);
        --nz-field: #ffffff;
        --nz-on-field: var(--nz-on-surface);
        --nz-accent: #3584e4;
        --nz-on-accent: #ffffff;
        --nz-selection: color-mix(in srgb, var(--nz-accent) 25%, transparent);
        --nz-on-selection: var(--nz-on-surface);
        --nz-muted: #66666d;
        --nz-link: #0461be;
        --adwaita-header: #ffffff;
        --adwaita-sidebar: #ebebed;
        --adwaita-popover: #ffffff;
        --adwaita-dialog: #fafafb;
        --adwaita-hover: color-mix(in srgb, var(--nz-on-surface) 15%, transparent);
        --adwaita-pressed: color-mix(in srgb, var(--nz-on-surface) 30%, transparent);
        --adwaita-trough: color-mix(in srgb, var(--nz-on-surface) 15%, transparent);
        --adwaita-thumb: #ffffff;
        --adwaita-focus: color-mix(in srgb, var(--nz-accent) 60%, transparent);
        --adwaita-action: #2268bf;
        --adwaita-action-hover: #2870c9;
        --adwaita-action-pressed: #195aab;
        --adwaita-scrollbar-edge: rgba(255, 255, 255, 0.6);
        scrollbar-visibility: autohide;
        scrollbar-placement: overlay;
        scrollbar-buttons: none;
        scrollbar-width: 16px;
        scrollbar-inset: 0;
        scrollbar-radius: 999px;
        scrollbar-min-thumb-length: 40px;
        scrollbar-thumb-width: 3px;
        scrollbar-thumb-expanded-width: 8px;
        scrollbar-thumb-color: color-mix(in srgb, var(--nz-on-surface) 20%, transparent);
        scrollbar-thumb-hover-color: color-mix(in srgb, var(--nz-on-surface) 40%, transparent);
        scrollbar-thumb-active-color: color-mix(in srgb, var(--nz-on-surface) 60%, transparent);
        scrollbar-thumb-border-width: 1px;
        scrollbar-thumb-border-color: var(--adwaita-scrollbar-edge);
        scrollbar-track-hover-color: color-mix(in srgb, var(--nz-on-surface) 10%, transparent);
        scrollbar-fade-in-duration: 0ms;
        scrollbar-fade-out-duration: 1000ms;
        scrollbar-fade-out-delay: 1500ms;
        scrollbar-wheel-timeout: 500ms;
        scrollbar-fade-timing-function: linear;
    }

    @media (prefers-color-scheme: dark) {
        :root {
            --nz-surface: #222226;
            --nz-on-surface: #ffffff;
            --nz-border: rgba(0, 0, 6, 0.36);
            --nz-field: #1d1d20;
            --nz-muted: #adadb5;
            --nz-link: #81d0ff;
            --adwaita-header: #2e2e32;
            --adwaita-sidebar: #2e2e32;
            --adwaita-popover: #36363a;
            --adwaita-dialog: #36363a;
            --adwaita-thumb: #d3d3d4;
            --adwaita-scrollbar-edge: rgba(0, 0, 12, 0.57);
        }

        Button[intent="danger"]:not([appearance="accent"], :disabled), MenuItem[intent="danger"]:not(:disabled),
        FieldError, LabelRequiredMark { color: #ff938c; }
    }

    :focus-visible { outline: 2px solid var(--adwaita-focus); outline-offset: 1px; }
    :disabled { color: var(--nz-muted); }
    CodeBlock { font-family: "Adwaita Mono", monospace; }
    TitleBar, Toolbar, MenuBar { background: var(--adwaita-header); }
    TitleBar {
        min-height: max(var(--nz-window-controls-height, 46px), 46px);
        padding: 0 6px;
        gap: 6px;
        border-bottom: 1px solid var(--nz-border);
    }
    TitleBarTitle { font-weight: 700; }
    CaptionButtons { align-items: center; gap: 3px; padding: 0 6px; }
    CaptionButtonsButton { width: 24px; min-height: 24px; height: 24px; border-radius: 50%; background: var(--nz-control); }
    CaptionButtonsButton:hover:not(:disabled), CaptionButtonsButton[kind="close"]:hover:not(:disabled) { background: var(--adwaita-hover); color: var(--nz-on-surface); }
    CaptionButtonsButton:active:not(:disabled), CaptionButtonsButton[kind="close"]:active:not(:disabled) { background: var(--adwaita-pressed); color: var(--nz-on-surface); }
    SplitViewPane, NavigationViewPane { background: var(--adwaita-sidebar); }

    Button, ToggleButton { min-height: 34px; padding: 4px 10px; border-radius: 9px; font-weight: 700; }
    Button:where([appearance="standard"]:not([default], :disabled, [busy])),
    ToggleButton:where([appearance="standard"]:not(:disabled, [pressed], [busy])) { border-color: transparent; }
    Button:hover:where([appearance="standard"]:not([default], :disabled)),
    ToggleButton:hover:where([appearance="standard"]:not(:disabled, [pressed])) {
        background: var(--adwaita-hover);
        border-color: transparent;
    }
    Button:active:where([appearance="standard"]:not([default], :disabled)),
    ToggleButton:active:where([appearance="standard"]:not(:disabled)),
    ToggleButton[pressed]:where([appearance="standard"]:not(:disabled)) {
        background: var(--adwaita-pressed);
        border-color: transparent;
        color: var(--nz-on-control);
    }
    Button[default]:where([appearance="standard"][intent="accent"]:not(:disabled)),
    :is(Button, ToggleButton)[appearance="accent"][intent="accent"]:where(:not(:disabled)) {
        background: var(--adwaita-action);
        border-color: transparent;
    }
    Button[default]:hover:where([appearance="standard"][intent="accent"]:not(:disabled)),
    :is(Button, ToggleButton)[appearance="accent"][intent="accent"]:hover:where(:not(:disabled)) {
        background: var(--adwaita-action-hover);
        border-color: transparent;
    }
    Button[default]:active:where([appearance="standard"][intent="accent"]:not(:disabled)),
    :is(Button, ToggleButton)[appearance="accent"][intent="accent"]:active:where(:not(:disabled)) {
        background: var(--adwaita-action-pressed);
        border-color: transparent;
    }

    TextField, TextArea, Select, NumberField, ComboBox, SearchField, PasswordField, TokenField,
    MaskedField, ShortcutField, SegmentedDatePicker, SegmentedTimePicker { border-radius: 9px; }
    TextField, Select, NumberField, ComboBox, SearchField, PasswordField, TokenField,
    MaskedField, ShortcutField, SegmentedDatePicker, SegmentedTimePicker { min-height: 34px; padding-left: 9px; padding-right: 9px; }
    TextField:where([appearance="outline"]:not([invalid], :disabled)),
    TextArea:where([appearance="outline"]:not([invalid], :disabled)),
    Select:where([appearance="outline"]:not([invalid], :disabled)),
    NumberField:where([appearance="outline"]:not([invalid], :disabled)),
    ComboBox:where([appearance="outline"]:not([invalid], :disabled)),
    SearchField:where([appearance="outline"]:not([invalid], :disabled)),
    PasswordField:where([appearance="outline"]:not([invalid], :disabled)),
    TokenField:where([appearance="outline"]:not([invalid], :disabled)),
    MaskedField:where([appearance="outline"]:not([invalid], :disabled)),
    ShortcutField:where([appearance="outline"]:not([invalid], :disabled)) {
        background: var(--nz-control);
        border-color: transparent;
    }

    CheckBoxMark, RadioButtonMark { width: 18px; height: 18px; background: var(--nz-control); }
    CheckBoxMark { border-radius: 5px; }
    SliderTrack, RangeSliderTrack, ProgressBarTrack { background: var(--adwaita-trough); border-color: transparent; border-radius: 999px; }
    SliderFill, RangeSliderFill, ProgressBarFill { border-radius: 999px; }
    Slider > SliderTrack, Slider > SliderTicks, RangeSliderRail, RangeSliderTicks { left: 10px; right: 10px; }
    SliderThumb, RangeSliderThumb {
        top: 1px;
        width: 20px;
        height: 20px;
        border-radius: 50%;
        border-color: transparent;
        background: var(--adwaita-thumb);
        box-shadow: 0 2px 4px rgba(0, 0, 6, 0.2);
    }
    SliderThumb { left: calc(var(--nz-slider-position) * (100% - 20px)); }
    RangeSliderThumb { left: calc(var(--nz-range-thumb-position) * 100% - 10px); }
    SliderThumb > SliderValueLabel, RangeSliderThumb > RangeSliderValueLabel { bottom: 23px; left: -20px; }
    Slider[orientation="vertical"] > SliderTrack, Slider[orientation="vertical"] > SliderTicks,
    RangeSlider[orientation="vertical"] > RangeSliderRail, RangeSlider[orientation="vertical"] > RangeSliderTicks { top: 10px; bottom: 10px; }
    Slider[orientation="vertical"] > SliderThumb { top: auto; left: 1px; bottom: calc(var(--nz-slider-position) * (100% - 20px)); }
    RangeSlider[orientation="vertical"] > RangeSliderRail > RangeSliderHandle > RangeSliderThumb {
        top: auto;
        left: 1px;
        bottom: calc(var(--nz-range-thumb-position) * 100% - 10px);
    }
    Slider[orientation="vertical"] > SliderThumb > SliderValueLabel,
    RangeSlider[orientation="vertical"] > RangeSliderRail > RangeSliderHandle > RangeSliderThumb > RangeSliderValueLabel { bottom: auto; top: 1px; left: 24px; }
    RangeSliderHandle:focus-visible > RangeSliderThumb { outline: 2px solid var(--adwaita-focus); outline-offset: 1px; }
    ProgressBar { min-width: 150px; }
    ProgressBarTrack { height: 8px; border-width: 0; }

    Switch[size="medium"] { --nz-switch-inset: 3px; --nz-switch-thumb-width: 20px; --nz-switch-thumb-height: 20px; }
    Switch[size="medium"] > SwitchTrack { height: 26px; min-width: 50px; }
    SwitchTrack { border-color: transparent; background: var(--adwaita-trough); }
    SwitchThumb { width: var(--nz-switch-thumb-width); height: var(--nz-switch-thumb-height); background: var(--adwaita-thumb); box-shadow: 0 2px 4px rgba(0, 0, 6, 0.2); }
    Switch:hover:where(:not(:disabled, :checked, [invalid])) > SwitchTrack {
        background: color-mix(in srgb, var(--nz-on-surface) 20%, transparent);
        border-color: transparent;
    }

    Card, Expander, SegmentedControl { border-radius: 12px; }
    Card:where([appearance="standard"]:not([selected], :disabled)) { background: var(--nz-field); }
    ListViewItem, TreeViewItem, GridViewItem { border-radius: 6px; }
    Tab, Segment, MenuItem, MenuBarItem, MenuCheckItem, MenuRadioItem, Submenu, SelectOption,
    ComboBoxOption, SearchFieldOption { border-radius: 9px; }
    Tabs[presentation="document"] Tab { min-height: 34px; border: 0; border-radius: 9px; margin: 6px 2px; }
    Tabs[presentation="document"] Tab[selected] { background: var(--nz-control); color: var(--nz-on-surface); }

    Menu, ContextMenu, SelectPopup, ComboBoxPopup, SearchFieldPopup, Popover, PopoverContainer,
    Tooltip, TooltipContainer, Toast, Dialog, DialogContainer, AlertDialog, CommandPalette {
        background: var(--adwaita-popover);
        border-color: var(--nz-border);
        border-radius: 15px;
        box-shadow: 0 2px 4px rgba(0, 0, 6, 0.12), 0 8px 24px rgba(0, 0, 6, 0.18);
    }
    Menu, ContextMenu, SelectPopup, ComboBoxPopup, SearchFieldPopup { padding: 6px; }
    MenuItem, MenuCheckItem, MenuRadioItem, Submenu, SelectOption, ComboBoxOption, SearchFieldOption { min-height: 32px; padding: 4px 12px; }
    :is(MenuItem, MenuCheckItem, MenuRadioItem, Submenu, SelectOption):is(:hover, :focus):where(:not(:disabled)),
    Submenu[expanded]:where(:not(:disabled)),
    :is(ComboBoxOption, SearchFieldOption):is(:hover, [current]):where(:not(:disabled)) {
        background: var(--nz-control);
        color: var(--nz-on-surface);
    }
    MenuItem[intent="danger"]:is(:hover, :focus):where(:not(:disabled)) { color: var(--nz-on-surface); }
    MenuItem:is(:hover, :focus):not(:disabled) > MenuItemDescription { color: inherit; }
    Dialog, DialogContainer, AlertDialog { background: var(--adwaita-dialog); padding: 24px; }
    DialogTitle, AlertDialogTitle { font-weight: 700; }
    Dialog:where([size="fullscreen"], [dialog-placement]) { border-radius: 0; }
}
