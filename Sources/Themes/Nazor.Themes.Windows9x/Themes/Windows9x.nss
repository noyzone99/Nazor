@media (forced-colors: none) {
    :root {
        font-family: "Microsoft Sans Serif", Tahoma, Arial, sans-serif;
        font-size: 12px;
        --nz-surface: #d0d2d7;
        --nz-on-surface: #20232b;
        --nz-control: #d9dbdf;
        --nz-on-control: #20232b;
        --nz-border: #626873;
        --nz-field: #fcfcfd;
        --nz-on-field: #20232b;
        --nz-accent: #30557b;
        --nz-on-accent: #ffffff;
        --nz-selection: #30557b;
        --nz-on-selection: #ffffff;
        --nz-link: #24547b;
        --nz-muted: #565c67;
        --win9x-light-edge: #f7f8fb;
        --win9x-dark-edge: #646a75;
        --win9x-inset-edge: #959ba5;
        scrollbar-visibility: visible;
        scrollbar-placement: reserve;
        scrollbar-width: 17px;
        scrollbar-inset: 0;
        scrollbar-radius: 0;
        scrollbar-min-thumb-length: 18px;
        scrollbar-track-color: #b6bec7;
        scrollbar-track-border-width: 1px;
        scrollbar-track-border-color: var(--win9x-inset-edge);
        scrollbar-thumb-color: #dce1e6;
        scrollbar-thumb-hover-color: #e6eaf0;
        scrollbar-thumb-active-color: #c1cbd5;
        scrollbar-thumb-border-width: 1px;
        scrollbar-thumb-border-color: var(--win9x-dark-edge);
        scrollbar-thumb-border-highlight-color: var(--win9x-light-edge);
        scrollbar-buttons: visible;
        scrollbar-button-length: 17px;
        scrollbar-button-radius: 0;
        scrollbar-button-color: #344755;
        scrollbar-button-disabled-color: #727d87;
        scrollbar-button-background: #d0d6df;
        scrollbar-button-hover-background: #e4e9ef;
        scrollbar-button-active-background: #a7b5c3;
        scrollbar-button-step: 24px;
    }

    :focus-visible { outline: 1px solid var(--nz-on-surface); outline-offset: 2px; }
    :disabled { color: var(--nz-muted); }
    MenuItem[intent="danger"]:is(:hover, :focus):where(:not(:disabled)) { color: var(--nz-on-selection); }
    MenuItem:is(:hover, :focus):not(:disabled) > MenuItemDescription { color: inherit; }
    TitleBar {
        background: linear-gradient(90deg, #253450, #42647e);
        color: #ffffff;
        border-bottom: 2px solid var(--win9x-dark-edge);
    }
    TitleBarTitle { font-weight: 700; }
    :root:window-inactive TitleBar { background: #677687; }
    CaptionButtons { align-items: center; gap: 2px; padding: 0 4px; }
    CaptionButtonsButton {
        width: 21px;
        min-height: 20px;
        background: var(--nz-control);
        color: var(--nz-on-control);
        border: 1px solid var(--win9x-dark-edge);
        border-top-color: var(--win9x-light-edge);
        border-left-color: var(--win9x-light-edge);
    }
    CaptionButtonsButton:hover:not(:disabled), CaptionButtonsButton[kind="close"]:hover:not(:disabled) { background: #e8ebee; color: var(--nz-on-control); }
    CaptionButtonsButton:active:not(:disabled), CaptionButtonsButton[kind="close"]:active:not(:disabled) { background: #b9c3cf; color: var(--nz-on-control); }
    Toolbar, MenuBar, TableHeaderCell { background: var(--nz-control); }
    Toolbar, MenuBar { border-bottom: 1px solid var(--win9x-dark-edge); }

    Button, ToggleButton { min-height: 25px; padding: 2px 9px; }
    Button:where([appearance="standard"]:not([icon-only], [shape="square"], [shape="circle"], [size="small"], [size="large"])) { min-width: 75px; }
    Button:where([appearance="standard"]:not([default], [shape="pill"], [shape="circle"])),
    ToggleButton:where([appearance="standard"]:not([pressed], [shape="pill"], [shape="circle"])),
    SliderThumb, Menu, SelectPopup, ComboBoxPopup, SearchFieldPopup, ContextMenu, Dialog, DialogContainer,
    AlertDialog, Popover, PopoverContainer, Toast, CommandPalette, Card:where([appearance="standard"]),
    Expander, SegmentedControl {
        border-top-color: var(--win9x-light-edge);
        border-left-color: var(--win9x-light-edge);
        border-right-color: var(--win9x-dark-edge);
        border-bottom-color: var(--win9x-dark-edge);
        box-shadow: inset -1px -1px 0 var(--win9x-inset-edge);
    }
    Button:active:where([appearance="standard"]:not([default], :disabled, [shape="pill"], [shape="circle"])),
    ToggleButton:active:where([appearance="standard"]:not(:disabled, [shape="pill"], [shape="circle"])),
    ToggleButton[pressed]:where([appearance="standard"]:not(:disabled, [shape="pill"], [shape="circle"])) {
        background: #c1c5ce;
        color: var(--nz-on-control);
        border-top-color: var(--win9x-dark-edge);
        border-left-color: var(--win9x-dark-edge);
        border-right-color: var(--win9x-light-edge);
        border-bottom-color: var(--win9x-light-edge);
        box-shadow: inset 1px 1px 0 var(--win9x-inset-edge);
    }

    Button[default]:where([appearance="standard"]:not([intent="danger"], [intent="warning"], [intent="success"], :disabled)) {
        background: #e3e7ed;
        color: var(--nz-on-control);
        border-color: #30557b;
        box-shadow: inset 0 0 0 1px var(--win9x-light-edge);
    }

    TextField:where([appearance="outline"]), TextArea:where([appearance="outline"]), Select:where([appearance="outline"]),
    NumberField:where([appearance="outline"]), ComboBox:where([appearance="outline"]), SearchField:where([appearance="outline"]),
    PasswordField:where([appearance="outline"]), TokenField:where([appearance="outline"]),
    CheckBoxMark, SliderTrack, ProgressBarTrack {
        border-top-color: var(--win9x-dark-edge);
        border-left-color: var(--win9x-dark-edge);
        border-right-color: var(--win9x-light-edge);
        border-bottom-color: var(--win9x-light-edge);
        box-shadow: inset 1px 1px 0 var(--win9x-inset-edge);
    }
    ProgressBarTrack { height: 12px; }
    ProgressBarFill:not([indeterminate]) { border-top: 1px solid rgba(255, 255, 255, 0.4); }
    SliderThumb { border-radius: 0; }
    CheckBox:checked:not(:disabled) > CheckBoxMark, CheckBox[indeterminate]:not(:disabled) > CheckBoxMark {
        background: var(--nz-field);
        color: #30455f;
        border-color: var(--win9x-dark-edge);
    }
    RadioButton:checked:not(:disabled) > RadioButtonMark { background: var(--nz-field); }
    RadioButton:checked:not(:disabled) > RadioButtonMark > RadioButtonDot { background: #30455f; }

    Tab { border: 1px solid transparent; }
    Tab[selected]:where(:not(:disabled)) {
        background: var(--nz-control);
        color: var(--nz-on-control);
        border-top-color: var(--win9x-light-edge);
        border-left-color: var(--win9x-light-edge);
        border-right-color: var(--win9x-dark-edge);
    }
    TabsIndicator { background: var(--nz-accent); }
    Tooltip, TooltipContainer { background: #fff3ce; border-color: var(--win9x-dark-edge); }
}
