@media (forced-colors: none) {
    :root {
        font-family: Helvetica, "Liberation Sans", Arial, sans-serif;
        font-size: 13px;
        --nz-surface: #c6b2a8;
        --nz-on-surface: #25201e;
        --nz-control: var(--nz-surface);
        --nz-on-control: #25201e;
        --nz-border: #6a564e;
        --nz-field: #686f82;
        --nz-on-field: #ffffff;
        --nz-accent: #1d5e71;
        --nz-on-accent: #ffffff;
        --nz-selection: #285d6e;
        --nz-on-selection: #ffffff;
        --nz-link: #134a66;
        --nz-muted: #51403b;
        --nz-danger: #8c0f1b;
        --cde-title: #eda870;
        --cde-on-title: #2a1e19;
        --cde-title-hover-shade: white;
        --cde-inactive-title: #999999;
        --cde-on-inactive-title: #202020;
        --cde-panel: #4992a7;
        --cde-on-panel: #10232b;
        --cde-light-edge: #e7ded9;
        --cde-dark-edge: #6e605a;
        --cde-pressed: #b69e92;
        --cde-on-pressed: #25201e;
        --cde-focus: #164b69;
        --cde-scrollbar-trough: #a8988f;
        scrollbar-visibility: visible;
        scrollbar-placement: reserve;
        scrollbar-width: 15px;
        scrollbar-inset: 0;
        scrollbar-radius: 0;
        scrollbar-min-thumb-length: 6px;
        scrollbar-track-color: var(--cde-scrollbar-trough);
        scrollbar-track-border-width: 1px;
        scrollbar-track-border-color: var(--cde-dark-edge);
        scrollbar-thumb-color: var(--nz-control);
        scrollbar-thumb-hover-color: var(--nz-control);
        scrollbar-thumb-active-color: var(--cde-pressed);
        scrollbar-thumb-border-width: 2px;
        scrollbar-thumb-border-color: var(--cde-dark-edge);
        scrollbar-thumb-border-highlight-color: var(--cde-light-edge);
        scrollbar-buttons: visible;
        scrollbar-button-length: 15px;
        scrollbar-button-radius: 0;
        scrollbar-button-color: var(--nz-on-control);
        scrollbar-button-disabled-color: var(--nz-muted);
        scrollbar-button-background: var(--nz-control);
        scrollbar-button-hover-background: var(--nz-control);
        scrollbar-button-active-background: var(--cde-pressed);
        scrollbar-button-step: 16px;
        scrollbar-button-repeat-delay: 250ms;
        scrollbar-button-repeat-interval: 50ms;
    }

    @media (prefers-color-scheme: dark) {
        :root {
            --nz-surface: #828585;
            --nz-on-surface: #151616;
            --nz-on-control: #151616;
            --nz-border: #414544;
            --nz-field: #896d6d;
            --nz-accent: #395951;
            --nz-selection: #454f4c;
            --nz-link: #102420;
            --nz-muted: #1c2322;
            --cde-title: #87b4b0;
            --cde-on-title: #162a28;
            --cde-inactive-title: #767279;
            --cde-on-inactive-title: #ffffff;
            --cde-panel: #636f6d;
            --cde-on-panel: #ffffff;
            --cde-light-edge: #c5c7c7;
            --cde-dark-edge: #414544;
            --cde-pressed: #737575;
            --cde-on-pressed: #ffffff;
            --cde-focus: #133b36;
            --cde-scrollbar-trough: #6f7171;
        }

        Button[intent="danger"]:not([appearance="accent"], :disabled),
        FieldError, LabelRequiredMark { color: #390008; }
        MenuItem[intent="danger"]:not(:disabled) { color: #ffd9d8; }
    }

    :focus-visible { outline: 2px solid var(--cde-focus); outline-offset: 2px; }
    :disabled { color: var(--nz-muted); }
    MenuItem[intent="danger"]:is(:hover, :focus):where(:not(:disabled)) { color: var(--nz-on-selection); }
    MenuItem:is(:hover, :focus):not(:disabled) > MenuItemDescription { color: inherit; }
    TitleBar {
        background: var(--cde-title);
        color: var(--cde-on-title);
        border-top: 2px solid var(--cde-light-edge);
        border-bottom: 2px solid var(--cde-dark-edge);
    }
    :root:window-inactive {
        --cde-title: var(--cde-inactive-title);
        --cde-on-title: var(--cde-on-inactive-title);
        --cde-title-hover-shade: black;
    }
    TitleBarTitle { font-size: 13px; font-weight: 700; }
    CaptionButtons { align-items: center; gap: 2px; padding: 0 4px; }
    CaptionButtonsButton {
        width: 22px;
        min-height: 22px;
        background: var(--cde-title);
        color: var(--cde-on-title);
        border: 2px solid var(--cde-dark-edge);
        border-top-color: var(--cde-light-edge);
        border-left-color: var(--cde-light-edge);
    }
    CaptionButtonsButton:hover:not(:disabled),
    CaptionButtonsButton[kind="close"]:hover:not(:disabled) {
        background: color-mix(in srgb, var(--cde-title) 85%, var(--cde-title-hover-shade));
        color: var(--cde-on-title);
    }
    CaptionButtonsButton:active:not(:disabled),
    CaptionButtonsButton[kind="close"]:active:not(:disabled) {
        background: var(--cde-pressed);
        color: var(--cde-on-pressed);
        border-color: var(--cde-light-edge);
        border-top-color: var(--cde-dark-edge);
        border-left-color: var(--cde-dark-edge);
    }
    CaptionButtonsButton:disabled, CaptionButtonsButton[kind="close"]:disabled:is(:hover, :active) {
        background: var(--cde-title);
        color: var(--cde-on-title);
        opacity: 0.5;
    }
    Toolbar, MenuBar, TableHeaderCell { background: var(--cde-panel); color: var(--cde-on-panel); }
    Toolbar, MenuBar { border-bottom: 2px solid var(--cde-dark-edge); }

    Button, ToggleButton { min-height: 28px; padding: 2px 10px; }
    Button:where([appearance="standard"]:not([default], [shape="pill"], [shape="circle"])),
    Button[default]:where(:is([appearance="standard"], [appearance="accent"]):not(:disabled, [shape="pill"], [shape="circle"], [intent="danger"], [intent="warning"], [intent="success"])),
    ToggleButton:where([appearance="standard"]:not([pressed], [shape="pill"], [shape="circle"])),
    SliderThumb, Menu, SelectPopup, ComboBoxPopup, SearchFieldPopup, ContextMenu, Dialog, DialogContainer,
    AlertDialog, Popover, PopoverContainer, Toast, CommandPalette, Card:where([appearance="standard"]),
    Expander, SegmentedControl {
        border-width: 2px;
        border-top-color: var(--cde-light-edge);
        border-left-color: var(--cde-light-edge);
        border-right-color: var(--cde-dark-edge);
        border-bottom-color: var(--cde-dark-edge);
    }
    Button[default]:where(:is([appearance="standard"], [appearance="accent"]):not(:disabled, [shape="pill"], [shape="circle"], [intent="danger"], [intent="warning"], [intent="success"])) {
        background: var(--nz-control);
        color: var(--nz-on-control);
        box-shadow: 0 0 0 1px var(--nz-on-control);
    }
    Button:active:where([appearance="standard"]:not(:disabled, [shape="pill"], [shape="circle"])),
    Button[default]:active:where([appearance="accent"]:not(:disabled, [shape="pill"], [shape="circle"], [intent="danger"], [intent="warning"], [intent="success"])),
    ToggleButton:active:where([appearance="standard"]:not(:disabled, [shape="pill"], [shape="circle"])),
    ToggleButton[pressed]:where([appearance="standard"]:not(:disabled, [shape="pill"], [shape="circle"])) {
        background: var(--cde-pressed);
        color: var(--cde-on-pressed);
        border-width: 2px;
        border-top-color: var(--cde-dark-edge);
        border-left-color: var(--cde-dark-edge);
        border-right-color: var(--cde-light-edge);
        border-bottom-color: var(--cde-light-edge);
    }

    :is(TextField, TextArea, Select, NumberField, ComboBox, SearchField, PasswordField, TokenField,
        DateField, TimeField, MaskedField, ShortcutField):where([appearance="outline"]),
    CheckBoxMark, RadioButtonMark, RadioButton:hover:not(:disabled) > RadioButtonMark,
    SwitchTrack, SliderTrack, ProgressBarTrack {
        border-width: 2px;
        border-top-color: var(--cde-dark-edge);
        border-left-color: var(--cde-dark-edge);
        border-right-color: var(--cde-light-edge);
        border-bottom-color: var(--cde-light-edge);
    }
    :is(TextField, TextArea, Select, NumberField, ComboBox, SearchField, PasswordField, TokenField,
        DateField, TimeField, MaskedField, ShortcutField):where([appearance="outline"]:not(:disabled)) {
        --nz-muted: #fcfcfc;
    }
    :is(TextField, TextArea, Select, NumberField, ComboBox, SearchField, PasswordField, TokenField,
        DateField, TimeField, MaskedField, ShortcutField):is([appearance="plain"], [appearance="filled"], [appearance="underline"]):not(:disabled) {
        color: var(--nz-on-control);
    }
    CheckBoxMark { width: 16px; height: 16px; }
    RadioButtonMark { width: 12px; height: 12px; margin: 2px; border-radius: 0; transform: rotate(45deg); }
    RadioButtonDot { width: 4px; height: 4px; border-radius: 0; }
    CheckBox:is(:checked, [indeterminate]):not(:disabled) > CheckBoxMark,
    RadioButton:checked:not(:disabled) > RadioButtonMark {
        background: var(--cde-pressed);
        color: var(--cde-on-pressed);
        border-color: var(--cde-light-edge);
        border-top-color: var(--cde-dark-edge);
        border-left-color: var(--cde-dark-edge);
    }
    RadioButton:checked:not(:disabled) > RadioButtonMark > RadioButtonDot { background: var(--cde-on-pressed); }
    SwitchTrack, SwitchThumb { border-radius: 0; }
    ProgressBarTrack { height: 11px; }
    Menu, ContextMenu, Tooltip, TooltipContainer {
        background: var(--cde-panel);
        color: var(--cde-on-panel);
        --nz-muted: var(--cde-on-panel);
    }
    MenuItem:disabled, MenuCheckItem:disabled, MenuRadioItem:disabled, Submenu:disabled { opacity: 0.6; }
    MenuItemDescription { color: inherit; }
    Dialog, DialogContainer { padding: 10px; gap: 10px; }
    AlertDialog { padding: 10px; row-gap: 10px; }
    DialogFooter, AlertDialogFooter { gap: 10px; }
    DialogButton > Button { min-width: 76px; }
    Tab, Tabs[presentation="document"] Tab { border: 2px solid transparent; border-radius: 0; }
    Tab[selected]:where(:not(:disabled)) {
        background: var(--nz-control);
        color: var(--nz-on-control);
        border-top-color: var(--cde-light-edge);
        border-left-color: var(--cde-light-edge);
        border-right-color: var(--cde-dark-edge);
    }
}
