@media (forced-colors: none) {
    :root {
        font-family: "Segoe UI", "Liberation Sans", sans-serif;
        font-size: 13px;
        --nz-surface: #edf2f7;
        --nz-on-surface: #1d3043;
        --nz-control: #f0f3f6;
        --nz-on-control: #1d3043;
        --nz-border: #8698a9;
        --nz-field: #fcfdff;
        --nz-on-field: #1d3043;
        --nz-accent: #1d618b;
        --nz-on-accent: #ffffff;
        --nz-selection: #cde9f9;
        --nz-on-selection: #20394c;
        --nz-link: #245c86;
        --nz-muted: #566575;
        --win7-bright-edge: #faffff;
        --win7-hover-edge: #4187ae;
        --win7-overlay-edge: #9babba;
        scrollbar-visibility: visible;
        scrollbar-placement: reserve;
        scrollbar-width: 16px;
        scrollbar-inset: 0;
        scrollbar-radius: 3px;
        scrollbar-track-radius: 0;
        scrollbar-min-thumb-length: 22px;
        scrollbar-track-color: #e6edf3;
        scrollbar-track-hover-color: #dceaf3;
        scrollbar-thumb-width: 12px;
        scrollbar-thumb-color: #c4d5e0;
        scrollbar-thumb-hover-color: #98cadd;
        scrollbar-thumb-active-color: #77adc8;
        scrollbar-thumb-border-width: 1px;
        scrollbar-thumb-border-color: #728c9f;
        scrollbar-buttons: visible;
        scrollbar-button-length: 15px;
        scrollbar-button-radius: 3px;
        scrollbar-button-color: #39576b;
        scrollbar-button-disabled-color: #7e94a4;
        scrollbar-button-background: #d7e5ee;
        scrollbar-button-hover-background: #c0e0ef;
        scrollbar-button-active-background: #a4cada;
        scrollbar-button-step: 26px;
    }

    :focus-visible { outline: 2px solid #297ca8; outline-offset: 1px; }
    :disabled { color: var(--nz-muted); }
    MenuItem[intent="danger"]:is(:hover, :focus):where(:not(:disabled)) { color: var(--nz-on-selection); }
    MenuItem:is(:hover, :focus):not(:disabled) > MenuItemDescription { color: inherit; }
    :root[frame-owner="nazor"][client-titlebar] {
        -window-frame-border: 1px solid #638397;
        -window-frame-border-radius: 8px;
        -window-frame-box-shadow: 0 7px 20px #16334855, 0 1px 5px #16334833;
    }
    :root[frame-owner="nazor"][client-titlebar]:window-inactive {
        -window-frame-border-color: #9daeba;
        -window-frame-box-shadow: 0 4px 12px #26394d38;
    }
    TitleBar {
        background: linear-gradient(115deg, #f8fcff, #dcecf7 42%, #b8d7e9 74%, #e0eef8);
        border-bottom: 1px solid var(--win7-overlay-edge);
        box-shadow: inset 0 1px 0 var(--win7-bright-edge);
        color: #233750;
    }
    :root[frame-owner="nazor"][backdrop]:window-active TitleBar {
        background: linear-gradient(115deg, #f8fcffed, #dcecf7bc 42%, #b8d7e9ae 74%, #e0eef8da);
    }
    :root:window-inactive TitleBar { background: linear-gradient(115deg, #f8fafc, #e1e6eb); }
    CaptionButtons { align-items: center; gap: 2px; padding: 0 5px; }
    CaptionButtonsButton { width: 29px; min-height: 23px; }
    CaptionButtonsButton:not(:disabled) {
        border: 1px solid var(--win7-overlay-edge);
        border-radius: 3px;
        background: linear-gradient(180deg, #f7fcff, #d0e4ef);
        color: #233750;
    }
    CaptionButtonsButton:hover:not(:disabled) { background: #d0ecfa; border-color: var(--win7-hover-edge); }
    CaptionButtonsButton:active:not(:disabled) { background: #afd7ea; }
    CaptionButtonsButton[kind="close"]:not(:disabled) { background: linear-gradient(180deg, #a75855, #843f41); color: white; }
    CaptionButtonsButton[kind="close"]:hover:not(:disabled) { background: #a54948; }
    CaptionButtonsButton[kind="close"]:active:not(:disabled) { background: #7f3037; }
    Toolbar, MenuBar, TableHeaderCell {
        background: linear-gradient(180deg, #f8fbff, #e0e9f2);
        border-bottom: 1px solid var(--win7-overlay-edge);
    }

    Button, ToggleButton { min-height: 28px; border-radius: 4px; }
    Button:where([appearance="standard"]:not([default], :disabled, [busy])),
    ToggleButton:where([appearance="standard"]:not([pressed], :disabled, [busy])) {
        background: linear-gradient(180deg, #feffff, #edf2f6 55%, #dbe3eb);
        box-shadow: inset 0 1px 0 var(--win7-bright-edge);
    }
    Button:hover:where([appearance="standard"]:not([default], :disabled)),
    ToggleButton:hover:where([appearance="standard"]:not([pressed], :disabled)) {
        background: linear-gradient(180deg, #fbfeff, #cceaf9);
        border-color: var(--win7-hover-edge);
    }
    Button:active:where([appearance="standard"]:not([default], :disabled)),
    ToggleButton:active:where([appearance="standard"]:not(:disabled)),
    ToggleButton[pressed]:where([appearance="standard"]:not(:disabled)) {
        background: linear-gradient(180deg, #add6ec, #e2f4ff);
        color: var(--nz-on-control);
        border-color: var(--win7-hover-edge);
        box-shadow: inset 0 1px 2px rgba(48, 94, 122, 0.26);
    }
    Button[default]:where(:is([appearance="standard"], [appearance="accent"]):not(:disabled, [intent="danger"], [intent="warning"], [intent="success"])) {
        background: linear-gradient(180deg, #367ca2, #1d577e);
        border-color: #235671;
        box-shadow: inset 0 1px 0 rgba(255, 255, 255, 0.5);
    }

    TextField, TextArea, Select, NumberField, ComboBox, SearchField, PasswordField, TokenField,
    MaskedField, ShortcutField { border-radius: 2px; }
    :is(TextField, TextArea, Select, NumberField, ComboBox, SearchField, PasswordField, TokenField, MaskedField, ShortcutField):hover:where([appearance="outline"]:not(:disabled, :focus-within, [invalid])) {
        border-color: var(--win7-hover-edge);
    }
    SelectChevron, ComboBoxChevron { width: 14px; height: 14px; padding: 2px; }
    Select[expanded] > SelectChevron, ComboBox[expanded] > ComboBoxChevron { border-radius: 2px; background: var(--nz-selection); }
    CheckBoxMark, SliderThumb { border-radius: 2px; }
    CheckBox:hover:not(:disabled) > CheckBoxMark, RadioButton:hover:not(:disabled) > RadioButtonMark {
        border-color: var(--win7-hover-edge);
    }
    SliderThumb { background: linear-gradient(180deg, #fbfdff, #b9cedd); }
    Slider:disabled > SliderThumb { background-image: none; }
    ProgressBarTrack { border-radius: 3px; height: 9px; }
    ProgressBar[intent="accent"] ProgressBarFill:not([indeterminate]) {
        background: linear-gradient(180deg, #73c2e6, #25668f);
    }

    Tab { border: 1px solid transparent; border-radius: 4px; }
    Tab[selected]:where(:not(:disabled)) {
        border-color: var(--win7-overlay-edge);
        box-shadow: inset 0 1px 0 var(--win7-bright-edge);
    }
    Card, Expander, SegmentedControl { border-radius: 4px; }
    Menu, ContextMenu, SelectPopup, ComboBoxPopup, SearchFieldPopup, Popover, PopoverContainer,
    Tooltip, TooltipContainer, Toast, Dialog, DialogContainer, AlertDialog, CommandPalette {
        background: var(--nz-field);
        border-color: var(--win7-overlay-edge);
        border-radius: 6px;
        box-shadow: 0 3px 12px rgba(37, 70, 99, 0.25), inset 0 1px 0 var(--win7-bright-edge);
    }
    Dialog:where([size="fullscreen"], [dialog-placement]) { border-radius: 0; }
}
