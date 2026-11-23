@media (forced-colors: none) {
    :root {
        font-family: Tahoma, "Trebuchet MS", Arial, sans-serif;
        font-size: 13px;
        --nz-surface: #ece9dc;
        --nz-on-surface: #242d40;
        --nz-control: #f7f5eb;
        --nz-on-control: #242d40;
        --nz-border: #697e9e;
        --nz-field: #fffefa;
        --nz-on-field: #242d40;
        --nz-accent: #305cab;
        --nz-on-accent: #ffffff;
        --nz-selection: #315a9b;
        --nz-on-selection: #ffffff;
        --nz-link: #24529d;
        --nz-muted: #595c65;
        --winxp-toolbar: #e4e9f4;
        --winxp-edge: #526e9b;
        --winxp-warm-edge: #b2782b;
        scrollbar-visibility: visible;
        scrollbar-placement: reserve;
        scrollbar-width: 16px;
        scrollbar-inset: 0;
        scrollbar-radius: 3px;
        scrollbar-track-radius: 0;
        scrollbar-track-color: #e4e9f4;
        scrollbar-track-border-width: 1px;
        scrollbar-track-border-color: #b2bfd6;
        scrollbar-min-thumb-length: 20px;
        scrollbar-thumb-width: 14px;
        scrollbar-thumb-color: #a9bfdf;
        scrollbar-thumb-hover-color: #c4d6ef;
        scrollbar-thumb-active-color: #7d9eca;
        scrollbar-thumb-border-width: 1px;
        scrollbar-thumb-border-color: #637fa8;
        scrollbar-buttons: visible;
        scrollbar-button-length: 16px;
        scrollbar-button-radius: 3px;
        scrollbar-button-color: #27426f;
        scrollbar-button-disabled-color: #7d8ca5;
        scrollbar-button-background: #d3ddf0;
        scrollbar-button-hover-background: #edf2fb;
        scrollbar-button-active-background: #aabddb;
        scrollbar-button-step: 26px;
    }

    :focus-visible { outline: 2px solid #416baa; outline-offset: 1px; }
    :disabled { color: var(--nz-muted); }
    MenuItem[intent="danger"]:is(:hover, :focus):where(:not(:disabled)) { color: var(--nz-on-selection); }
    MenuItem:is(:hover, :focus):not(:disabled) > MenuItemDescription { color: inherit; }
    TitleBar {
        background: linear-gradient(180deg, #3e6ab8, #345fae 40%, #254880);
        border-bottom: 1px solid #244171;
        color: #ffffff;
    }
    TitleBarTitle { font-weight: 700; }
    :root:window-inactive TitleBar { background: linear-gradient(180deg, #536b8e, #435b7b); }
    CaptionButtons { align-items: center; gap: 3px; padding: 0 5px; }
    CaptionButtonsButton { width: 25px; min-height: 23px; }
    CaptionButtonsButton:not(:disabled) {
        border: 1px solid #244171;
        border-radius: 4px;
        background: linear-gradient(180deg, #5178be, #36588d);
        color: #ffffff;
    }
    CaptionButtonsButton:hover:not(:disabled) { background: #537da9; }
    CaptionButtonsButton:active:not(:disabled) { background: #29456f; }
    CaptionButtonsButton[kind="close"]:hover:not(:disabled) { background: #aa6043; }
    CaptionButtonsButton[kind="close"]:active:not(:disabled) { background: #7e3f2a; }
    Toolbar, MenuBar, TableHeaderCell { background: var(--winxp-toolbar); }

    Button, ToggleButton { min-height: 28px; border-radius: 5px; }
    Button:where([appearance="standard"]:not([default], :disabled, [busy])),
    ToggleButton:where([appearance="standard"]:not([pressed], :disabled, [busy])) {
        background: linear-gradient(180deg, #fffffc, #f8f6ec 58%, #e0e5f0);
        border-color: var(--winxp-edge);
        box-shadow: inset 0 1px 0 #ffffff;
    }
    Button:hover:where([appearance="standard"]:not([default], :disabled)),
    ToggleButton:hover:where([appearance="standard"]:not([pressed], :disabled)) {
        background: linear-gradient(180deg, #fffef7, #f8eaca);
        border-color: var(--winxp-warm-edge);
    }
    Button:active:where([appearance="standard"]:not([default], :disabled)),
    ToggleButton:active:where([appearance="standard"]:not(:disabled)),
    ToggleButton[pressed]:where([appearance="standard"]:not(:disabled)) {
        background: linear-gradient(180deg, #bccce7, #edf2fb);
        border-color: var(--winxp-edge);
        color: var(--nz-on-control);
        box-shadow: inset 0 1px 2px rgba(37, 72, 128, 0.3);
    }
    Button[default]:where(:is([appearance="standard"], [appearance="accent"]):not(:disabled, [intent="danger"], [intent="warning"], [intent="success"])) {
        background: linear-gradient(180deg, #426cb7, #284b91);
        border-color: #244171;
        box-shadow: inset 0 1px 0 rgba(255, 255, 255, 0.4);
    }

    TextField, TextArea, Select, NumberField, ComboBox, SearchField, PasswordField, TokenField,
    MaskedField, ShortcutField { border-radius: 3px; }
    SelectChevron, ComboBoxChevron, NumberFieldIncrement, NumberFieldDecrement {
        background: linear-gradient(180deg, #f8fbff, #d6e0f1);
        border: 1px solid #a0b0ce;
        border-radius: 2px;
    }
    SelectChevron, ComboBoxChevron { width: 17px; height: 17px; padding: 3px; }
    Select[expanded] > SelectChevron, ComboBox[expanded] > ComboBoxChevron { background: #b5c8e5; }
    NumberFieldIncrement:disabled, NumberFieldDecrement:disabled { border-color: transparent; background-image: none; }
    CheckBoxMark, SliderThumb { border-radius: 2px; }
    CheckBox:checked:not(:disabled) > CheckBoxMark,
    RadioButton:checked:not(:disabled) > RadioButtonMark { background: #e7f1db; border-color: #60864a; }
    CheckBox:checked:not(:disabled) > CheckBoxMark { color: #2f581d; }
    RadioButton:checked:not(:disabled) > RadioButtonMark > RadioButtonDot { background: #2f581d; }
    SliderThumb { background: linear-gradient(180deg, #f8fbff, #b8cce8); }
    Slider:disabled > SliderThumb { background-image: none; }

    ProgressBarTrack { border-radius: 3px; height: 11px; }
    ProgressBar[intent="accent"] { --nz-feedback-color: #3a742d; }
    ProgressBar[intent="accent"] ProgressBarFill:not([indeterminate]),
    ProgressBar[intent="accent"] ProgressBarSegment[filled] {
        background: linear-gradient(180deg, #95c971, #478b3b 52%, #31642a);
    }
    Card, Expander, SegmentedControl { border-radius: 4px; }
    Tab { border: 1px solid transparent; border-radius: 4px; }
    Tab[selected]:where(:not(:disabled)) {
        background: var(--nz-field);
        color: var(--nz-on-field);
        border-color: var(--winxp-edge);
        border-top-color: var(--winxp-warm-edge);
    }
    Menu, ContextMenu, SelectPopup, ComboBoxPopup, SearchFieldPopup, Popover, PopoverContainer,
    Toast, Dialog, DialogContainer, AlertDialog, CommandPalette {
        border-color: var(--winxp-edge);
        border-radius: 4px;
        box-shadow: 0 2px 6px rgba(36, 45, 64, 0.22);
    }
    Tooltip, TooltipContainer { background: #fff2cc; border-color: var(--winxp-edge); }
    Dialog:where([size="fullscreen"], [dialog-placement]) { border-radius: 0; }
}
