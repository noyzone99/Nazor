:root {
    font-family: "Segoe UI Variable", "Segoe UI Variable Text", "Segoe UI", "Noto Sans", "DejaVu Sans", sans-serif;
    font-size: 14px;
    line-height: normal;
    --winui-text-fill-color-primary: #000000e4;
    --winui-text-fill-color-secondary: #0000009e;
    --winui-text-fill-color-tertiary: #00000072;
    --winui-text-fill-color-disabled: #0000005c;
    --winui-text-fill-color-inverse: #ffffff;
    --winui-accent-text-fill-color-disabled: #0000005c;
    --winui-text-on-accent-fill-color-selected-text: #ffffff;
    --winui-text-on-accent-fill-color-primary: #ffffff;
    --winui-text-on-accent-fill-color-secondary: #ffffffb3;
    --winui-text-on-accent-fill-color-disabled: #ffffff;
    --winui-control-fill-color-default: #ffffffb3;
    --winui-control-fill-color-secondary: #f9f9f980;
    --winui-control-fill-color-tertiary: #f9f9f94d;
    --winui-control-fill-color-quarternary: #f3f3f3c2;
    --winui-control-fill-color-disabled: #f9f9f94d;
    --winui-control-fill-color-transparent: #ffffff00;
    --winui-control-fill-color-input-active: #ffffff;
    --winui-control-strong-fill-color-default: #00000072;
    --winui-control-strong-fill-color-disabled: #00000051;
    --winui-control-solid-fill-color-default: #ffffff;
    --winui-subtle-fill-color-transparent: #ffffff00;
    --winui-subtle-fill-color-secondary: #00000009;
    --winui-subtle-fill-color-tertiary: #00000006;
    --winui-subtle-fill-color-disabled: #ffffff00;
    --winui-control-alt-fill-color-transparent: #ffffff00;
    --winui-control-alt-fill-color-secondary: #00000006;
    --winui-control-alt-fill-color-tertiary: #0000000f;
    --winui-control-alt-fill-color-quarternary: #00000018;
    --winui-control-alt-fill-color-disabled: #ffffff00;
    --winui-control-on-image-fill-color-default: #ffffffc9;
    --winui-control-on-image-fill-color-secondary: #f3f3f3;
    --winui-control-on-image-fill-color-tertiary: #ebebeb;
    --winui-control-on-image-fill-color-disabled: #ffffff00;
    --winui-accent-fill-color-disabled: #00000037;
    --winui-control-stroke-color-default: #0000000f;
    --winui-control-stroke-color-secondary: #00000029;
    --winui-control-stroke-color-on-accent-default: #ffffff14;
    --winui-control-stroke-color-on-accent-secondary: #00000066;
    --winui-control-stroke-color-on-accent-tertiary: #00000037;
    --winui-control-stroke-color-on-accent-disabled: #0000000f;
    --winui-control-stroke-color-for-strong-fill-when-on-image: #ffffff59;
    --winui-card-stroke-color-default: #0000000f;
    --winui-card-stroke-color-default-solid: #ebebeb;
    --winui-control-strong-stroke-color-default: #00000072;
    --winui-control-strong-stroke-color-disabled: #00000037;
    --winui-surface-stroke-color-default: #75757566;
    --winui-surface-stroke-color-flyout: #0000000f;
    --winui-surface-stroke-color-inverse: #ffffff15;
    --winui-divider-stroke-color-default: #0000000f;
    --winui-focus-stroke-color-outer: #000000e4;
    --winui-focus-stroke-color-inner: #ffffffb3;
    --winui-card-background-fill-color-default: #ffffffb3;
    --winui-card-background-fill-color-secondary: #f6f6f680;
    --winui-card-background-fill-color-tertiary: #ffffff;
    --winui-smoke-fill-color-default: #0000004d;
    --winui-layer-fill-color-default: #ffffff80;
    --winui-layer-fill-color-alt: #ffffff;
    --winui-layer-on-acrylic-fill-color-default: #ffffff40;
    --winui-layer-on-accent-acrylic-fill-color-default: #ffffff40;
    --winui-layer-on-mica-base-alt-fill-color-default: #ffffffb3;
    --winui-layer-on-mica-base-alt-fill-color-secondary: #0000000a;
    --winui-layer-on-mica-base-alt-fill-color-tertiary: #f9f9f9;
    --winui-layer-on-mica-base-alt-fill-color-transparent: #00000000;
    --winui-solid-background-fill-color-base: #f3f3f3;
    --winui-solid-background-fill-color-secondary: #eeeeee;
    --winui-solid-background-fill-color-tertiary: #f9f9f9;
    --winui-solid-background-fill-color-quarternary: #ffffff;
    --winui-solid-background-fill-color-quinary: #fdfdfd;
    --winui-solid-background-fill-color-senary: #ffffff;
    --winui-solid-background-fill-color-transparent: #f3f3f300;
    --winui-solid-background-fill-color-base-alt: #dadada;
    --winui-system-fill-color-success: #0f7b0f;
    --winui-system-fill-color-caution: #9d5d00;
    --winui-system-fill-color-critical: #c42b1c;
    --winui-system-fill-color-neutral: #00000072;
    --winui-system-fill-color-solid-neutral: #8a8a8a;
    --winui-system-fill-color-attention-background: #f6f6f680;
    --winui-system-fill-color-success-background: #dff6dd;
    --winui-system-fill-color-caution-background: #fff4ce;
    --winui-system-fill-color-critical-background: #fde7e9;
    --winui-system-fill-color-neutral-background: #00000006;
    --winui-system-fill-color-solid-attention-background: #f7f7f7;
    --winui-system-fill-color-solid-neutral-background: #f3f3f3;

    /* Basic supplies geometry for the current controls and reads these semantic tokens. */
    --nz-on-surface: var(--nz-text);
    --nz-on-control: var(--nz-control-text);
    --nz-on-field: var(--nz-field-text);
    --nz-on-selection: var(--nz-selection-text);
    --nz-border: var(--nz-stroke);
    --nz-muted: var(--nz-text-disabled);
    --nz-link: var(--nz-accent-text);
    --nz-danger: var(--nz-error);
    --nz-scrim: var(--winui-smoke-fill-color-default);

    --nz-accent: var(--nz-system-accent-dark-1);
    --nz-selection: var(--winui-subtle-fill-color-secondary);
    --nz-selection-hover: var(--winui-subtle-fill-color-tertiary);
    --nz-selection-text: var(--winui-text-fill-color-primary);
    --nz-tooltip: #f9f9f9;
    --nz-tooltip-text: var(--winui-text-fill-color-primary);
    --nz-header: var(--winui-solid-background-fill-color-base);
    --nz-header-text: var(--winui-text-fill-color-primary);
    --nz-accent-hover: color-mix(in srgb, var(--nz-accent) 90%, transparent);
    --nz-accent-pressed: color-mix(in srgb, var(--nz-accent) 80%, transparent);
    --nz-accent-disabled: var(--winui-accent-fill-color-disabled);
    --nz-on-accent: var(--winui-text-on-accent-fill-color-primary);
    --nz-on-accent-pressed: var(--winui-text-on-accent-fill-color-secondary);
    --nz-on-accent-disabled: var(--winui-text-on-accent-fill-color-disabled);
    --nz-text: var(--winui-text-fill-color-primary);
    --nz-text-secondary: var(--winui-text-fill-color-secondary);
    --nz-text-disabled: var(--winui-text-fill-color-disabled);
    --nz-surface: var(--winui-solid-background-fill-color-base);
    --nz-layer: var(--winui-solid-background-fill-color-tertiary);
    --nz-layer-text: var(--winui-text-fill-color-primary);
    --nz-control: var(--winui-control-fill-color-default);
    --nz-control-text: var(--winui-text-fill-color-primary);
    --nz-control-hover: var(--winui-control-fill-color-secondary);
    --nz-control-hover-text: var(--winui-text-fill-color-primary);
    --nz-control-pressed: var(--winui-control-fill-color-tertiary);
    --nz-control-pressed-text: var(--winui-text-fill-color-secondary);
    --nz-control-disabled: var(--winui-control-fill-color-disabled);
    --nz-control-disabled-text: var(--winui-text-fill-color-disabled);
    --nz-control-alt: var(--winui-control-alt-fill-color-secondary);
    --nz-control-alt-hover: var(--winui-control-alt-fill-color-tertiary);
    --nz-control-alt-pressed: var(--winui-control-alt-fill-color-quarternary);
    --nz-field: var(--winui-control-fill-color-default);
    --nz-field-text: var(--winui-text-fill-color-primary);
    --nz-field-hover: var(--winui-control-fill-color-secondary);
    --nz-field-hover-text: var(--winui-text-fill-color-primary);
    --nz-field-disabled: var(--winui-control-fill-color-disabled);
    --nz-field-disabled-text: var(--winui-text-fill-color-disabled);
    --nz-input-focus: var(--winui-control-fill-color-input-active);
    --nz-control-solid: var(--winui-control-solid-fill-color-default);
    --nz-stroke: var(--winui-control-stroke-color-default);
    --nz-stroke-bottom: var(--winui-control-stroke-color-secondary);
    --nz-stroke-strong: var(--winui-control-strong-stroke-color-default);
    --nz-stroke-disabled: var(--winui-control-strong-stroke-color-disabled);
    /* WinUI keeps the ordinary hairline on disabled buttons and fields and drops the border of disabled accent fills. */
    --nz-control-stroke-disabled: var(--winui-control-stroke-color-default);
    --nz-accent-stroke-disabled: transparent;
    --nz-card: var(--winui-card-background-fill-color-default);
    --nz-card-secondary: var(--winui-card-background-fill-color-secondary);
    --nz-card-stroke: var(--winui-card-stroke-color-default);
    /* ContentDialog paints LayerFillColorAlt over SolidBackgroundFillColorBase; this is the opaque result. */
    --nz-dialog: #ffffff;
    --nz-strong-fill-disabled: var(--winui-control-strong-fill-color-disabled);
    --nz-stroke-overlay: var(--winui-surface-stroke-color-flyout);
    --nz-subtle-hover: var(--winui-subtle-fill-color-secondary);
    --nz-subtle-pressed: var(--winui-subtle-fill-color-tertiary);
    --nz-focus: var(--winui-focus-stroke-color-outer);
    --nz-focus-inner: var(--winui-focus-stroke-color-inner);
    --nz-shadow: rgba(0, 0, 0, 0.14);
    --nz-shadow-ambient: rgba(0, 0, 0, 0.12);
    --nz-accent-text: var(--nz-system-accent-dark-2);
    --nz-success: var(--winui-system-fill-color-success);
    --nz-warning: var(--winui-system-fill-color-caution);
    --nz-error: var(--winui-system-fill-color-critical);
    --nz-info-background: var(--winui-system-fill-color-solid-attention-background);
    --nz-info-text: var(--nz-text);
    --nz-success-background: var(--winui-system-fill-color-success-background);
    --nz-success-text: var(--nz-text);
    --nz-warning-background: var(--winui-system-fill-color-caution-background);
    --nz-warning-text: var(--nz-text);
    --nz-error-background: var(--winui-system-fill-color-critical-background);
    --nz-error-text: var(--nz-text);
    --nz-close-hover: #c42b1c;
    --nz-close-pressed: #c42b1ce6;
    --nz-on-close: #ffffff;
    --nz-field-elevation: linear-gradient(to top, var(--nz-stroke-strong) 1px, var(--nz-stroke) 2px);
    --nz-circle-elevation: linear-gradient(to bottom, var(--nz-stroke) 50%, var(--nz-stroke-bottom) 70%);
    --nz-elevation: linear-gradient(to top, var(--nz-stroke-bottom) 0.99px, var(--nz-stroke) 3px);
    --nz-accent-elevation: linear-gradient(to top, var(--winui-control-stroke-color-on-accent-secondary) 0.99px, var(--winui-control-stroke-color-on-accent-default) 3px);
}

@media (prefers-color-scheme: dark) {
    :root {
        --winui-text-fill-color-primary: #ffffff;
        --winui-text-fill-color-secondary: #ffffffc5;
        --winui-text-fill-color-tertiary: #ffffff87;
        --winui-text-fill-color-disabled: #ffffff5d;
        --winui-text-fill-color-inverse: #000000e4;
        --winui-accent-text-fill-color-disabled: #ffffff5d;
        --winui-text-on-accent-fill-color-selected-text: #ffffff;
        --winui-text-on-accent-fill-color-primary: #000000;
        --winui-text-on-accent-fill-color-secondary: #00000080;
        --winui-text-on-accent-fill-color-disabled: #ffffff87;
        --winui-control-fill-color-default: #ffffff0f;
        --winui-control-fill-color-secondary: #ffffff15;
        --winui-control-fill-color-tertiary: #ffffff08;
        --winui-control-fill-color-quarternary: #ffffff0f;
        --winui-control-fill-color-disabled: #ffffff0b;
        --winui-control-fill-color-transparent: #ffffff00;
        --winui-control-fill-color-input-active: #1e1e1eb3;
        --winui-control-strong-fill-color-default: #ffffff8b;
        --winui-control-strong-fill-color-disabled: #ffffff3f;
        --winui-control-solid-fill-color-default: #454545;
        --winui-subtle-fill-color-transparent: #ffffff00;
        --winui-subtle-fill-color-secondary: #ffffff0f;
        --winui-subtle-fill-color-tertiary: #ffffff0a;
        --winui-subtle-fill-color-disabled: #ffffff00;
        --winui-control-alt-fill-color-transparent: #ffffff00;
        --winui-control-alt-fill-color-secondary: #00000019;
        --winui-control-alt-fill-color-tertiary: #ffffff0b;
        --winui-control-alt-fill-color-quarternary: #ffffff12;
        --winui-control-alt-fill-color-disabled: #ffffff00;
        --winui-control-on-image-fill-color-default: #1c1c1cb3;
        --winui-control-on-image-fill-color-secondary: #1a1a1a;
        --winui-control-on-image-fill-color-tertiary: #131313;
        --winui-control-on-image-fill-color-disabled: #1e1e1e;
        --winui-accent-fill-color-disabled: #ffffff28;
        --winui-control-stroke-color-default: #ffffff12;
        --winui-control-stroke-color-secondary: #ffffff18;
        --winui-control-stroke-color-on-accent-default: #ffffff14;
        --winui-control-stroke-color-on-accent-secondary: #00000023;
        --winui-control-stroke-color-on-accent-tertiary: #00000037;
        --winui-control-stroke-color-on-accent-disabled: #00000033;
        --winui-control-stroke-color-for-strong-fill-when-on-image: #0000006b;
        --winui-card-stroke-color-default: #00000019;
        --winui-card-stroke-color-default-solid: #1c1c1c;
        --winui-control-strong-stroke-color-default: #ffffff8b;
        --winui-control-strong-stroke-color-disabled: #ffffff28;
        --winui-surface-stroke-color-default: #75757566;
        --winui-surface-stroke-color-flyout: #00000033;
        --winui-surface-stroke-color-inverse: #0000000f;
        --winui-divider-stroke-color-default: #ffffff15;
        --winui-focus-stroke-color-outer: #ffffff;
        --winui-focus-stroke-color-inner: #000000b3;
        --winui-card-background-fill-color-default: #ffffff0d;
        --winui-card-background-fill-color-secondary: #ffffff08;
        --winui-card-background-fill-color-tertiary: #ffffff12;
        --winui-smoke-fill-color-default: #0000004d;
        --winui-layer-fill-color-default: #3a3a3a4c;
        --winui-layer-fill-color-alt: #ffffff0d;
        --winui-layer-on-acrylic-fill-color-default: #ffffff09;
        --winui-layer-on-accent-acrylic-fill-color-default: #ffffff09;
        --winui-layer-on-mica-base-alt-fill-color-default: #3a3a3a73;
        --winui-layer-on-mica-base-alt-fill-color-secondary: #ffffff0f;
        --winui-layer-on-mica-base-alt-fill-color-tertiary: #2c2c2c;
        --winui-layer-on-mica-base-alt-fill-color-transparent: #ffffff00;
        --winui-solid-background-fill-color-base: #202020;
        --winui-solid-background-fill-color-secondary: #1c1c1c;
        --winui-solid-background-fill-color-tertiary: #282828;
        --winui-solid-background-fill-color-quarternary: #2c2c2c;
        --winui-solid-background-fill-color-quinary: #333333;
        --winui-solid-background-fill-color-senary: #373737;
        --winui-solid-background-fill-color-transparent: #20202000;
        --winui-solid-background-fill-color-base-alt: #0a0a0a;
        --winui-system-fill-color-success: #6ccb5f;
        --winui-system-fill-color-caution: #fce100;
        --winui-system-fill-color-critical: #ff99a4;
        --winui-system-fill-color-neutral: #ffffff8b;
        --winui-system-fill-color-solid-neutral: #9d9d9d;
        --winui-system-fill-color-attention-background: #ffffff08;
        --winui-system-fill-color-success-background: #393d1b;
        --winui-system-fill-color-caution-background: #433519;
        --winui-system-fill-color-critical-background: #442726;
        --winui-system-fill-color-neutral-background: #ffffff08;
        --winui-system-fill-color-solid-attention-background: #2e2e2e;
        --winui-system-fill-color-solid-neutral-background: #2e2e2e;
        --nz-accent: var(--nz-system-accent-light-2);
        --nz-accent-text: var(--nz-system-accent-light-3);
        --nz-tooltip: #2c2c2c;
        --nz-dialog: #2b2b2b;
        --nz-shadow: rgba(0, 0, 0, 0.28);
        --nz-shadow-ambient: rgba(0, 0, 0, 0.24);
    }
}

@media (forced-colors: active) {
    :root {
        --nz-scrim: transparent;
        --nz-layer-text: CanvasText;
        --nz-control-text: ButtonText;
        --nz-control-hover-text: ButtonText;
        --nz-control-pressed-text: ButtonText;
        --nz-control-disabled-text: GrayText;
        --nz-field: Field;
        --nz-field-text: FieldText;
        --nz-field-hover: Field;
        --nz-field-hover-text: FieldText;
        --nz-field-disabled: Canvas;
        --nz-field-disabled-text: GrayText;
        --nz-tooltip: Canvas;
        --nz-tooltip-text: CanvasText;
        --nz-header: Canvas;
        --nz-header-text: CanvasText;
        --nz-info-text: CanvasText;
        --nz-success-text: CanvasText;
        --nz-warning-text: CanvasText;
        --nz-error-text: CanvasText;
        --nz-accent: Highlight;
        --nz-selection: Highlight;
        --nz-selection-hover: Highlight;
        --nz-selection-text: HighlightText;
        --nz-accent-hover: Highlight;
        --nz-accent-pressed: Highlight;
        --nz-accent-disabled: GrayText;
        --nz-on-accent: HighlightText;
        --nz-on-accent-disabled: Canvas;
        --nz-on-accent-pressed: HighlightText;
        --nz-focus-inner: Canvas;
        --nz-stroke-disabled: GrayText;
        --nz-control-stroke-disabled: GrayText;
        --nz-accent-stroke-disabled: GrayText;
        --nz-card: Canvas;
        --nz-card-secondary: Canvas;
        --nz-card-stroke: CanvasText;
        --nz-dialog: Canvas;
        --nz-shadow-ambient: transparent;
        --nz-control-alt-pressed: Canvas;
        --nz-elevation: none;
        --nz-field-elevation: none;
        --nz-circle-elevation: none;
        --nz-accent-elevation: none;
        --nz-text: CanvasText;
        --nz-text-secondary: CanvasText;
        --nz-text-disabled: GrayText;
        --nz-surface: Canvas;
        --nz-layer: Canvas;
        --nz-control: ButtonFace;
        --nz-control-hover: ButtonFace;
        --nz-control-pressed: ButtonFace;
        --nz-control-disabled: Canvas;
        --nz-control-alt: Canvas;
        --nz-control-alt-hover: Canvas;
        --nz-input-focus: Field;
        --nz-control-solid: ButtonFace;
        --nz-stroke: ButtonText;
        --nz-strong-fill-disabled: GrayText;
        --nz-stroke-bottom: ButtonText;
        --nz-stroke-strong: ButtonText;
        --nz-stroke-overlay: CanvasText;
        --nz-subtle-hover: transparent;
        --nz-subtle-pressed: transparent;
        --nz-focus: CanvasText;
        --nz-shadow: transparent;
        --nz-accent-text: LinkText;
        --nz-success: CanvasText;
        --nz-warning: CanvasText;
        --nz-error: CanvasText;
        --nz-info-background: Canvas;
        --nz-success-background: Canvas;
        --nz-warning-background: Canvas;
        --nz-error-background: Canvas;
        --nz-close-hover: Highlight;
        --nz-close-pressed: Highlight;
        --nz-on-close: HighlightText;
    }
}

:root[client-titlebar] {
    -windows-title-bar-button-foreground-color: var(--nz-header-text);
    -windows-title-bar-button-hover-foreground-color: var(--nz-header-text);
    -windows-title-bar-button-pressed-foreground-color: var(--nz-text-secondary);
    -windows-title-bar-button-inactive-foreground-color: auto;
    -windows-title-bar-button-background-color: transparent;
    -windows-title-bar-button-inactive-background-color: transparent;
    -windows-title-bar-button-hover-background-color: var(--nz-subtle-hover);
    -windows-title-bar-button-pressed-background-color: var(--nz-subtle-pressed);
}

/* WinUI caption buttons use the subtle fills; only close turns red. */
CaptionButtonsButton:hover { background: var(--nz-subtle-hover); }
CaptionButtonsButton:active { background: var(--nz-subtle-pressed); color: var(--nz-text-secondary); }
CaptionButtonsButton[kind="close"]:hover { background: var(--nz-close-hover); color: var(--nz-on-close); }
CaptionButtonsButton[kind="close"]:active { background: var(--nz-close-pressed); color: var(--nz-on-close); }

TitleBar {
    display: flex;
    align-items: center;
    background: var(--nz-header);
    color: var(--nz-header-text);
}

:root[backdrop] TitleBar, :root[transparent-background] TitleBar { background: transparent; }

MessageBoxBody {
    display: flex;
    align-items: flex-start;
    gap: 16px;
    padding: 24px;
    background: var(--nz-dialog);
    color: var(--nz-layer-text);
    --nz-text: var(--nz-layer-text);

    > TextBlock { flex: 1; min-width: 0; }
}

MessageBoxIcon {
    position: relative;
    flex-shrink: 0;
    width: 24px;
    height: 24px;
    overflow-x: hidden;
    overflow-y: hidden;
    font-size: 24px;
    line-height: 1;
    color: transparent;

    &::before {
        position: absolute;
        left: 0;
        top: 0;
        width: 24px;
        height: 24px;
        content: "";
        font-family: "FluentSystemIcons-Filled";
        font-size: 24px;
        line-height: 1;
        color: var(--nz-accent);
    }
    &[icon="warning"]::before { content: ""; color: var(--nz-warning); }
    &[icon="error"]::before { content: ""; color: var(--nz-error); }
    &[icon="question"]::before { content: ""; }
}

MessageBoxButtons {
    display: flex;
    justify-content: flex-end;
    gap: 8px;
    padding: 24px;
    border-top: 1px solid var(--nz-card-stroke);
    background: var(--nz-surface);

    > Button { flex: 1; min-width: 96px; }
}
/* Typography: the WinUI type ramp, semibold rather than bold. Controls keep the natural Segoe UI line height, as
   WinUI does outside the TextBlock styles. */
Heading { font-weight: 600; }
Heading[level="1"] { font-size: 28px; line-height: 36px; }
Heading[level="2"] { font-size: 20px; line-height: 28px; }
Heading[level="3"] { font-size: 18px; line-height: 24px; }
Heading[level="4"] { font-size: 14px; line-height: 20px; }
Heading[level="5"], Heading[level="6"] { font-size: 12px; line-height: 16px; }
PageHeaderTitle { font-size: 28px; line-height: 36px; font-weight: 600; }
PageHeader[compact] PageHeaderTitle { font-size: 20px; line-height: 28px; }

/* Actions */
:focus-visible {
    outline: 2px solid var(--nz-focus);
    outline-offset: 1px;
    outline-inner-width: 1px;
    outline-inner-color: var(--nz-focus-inner);
}

:disabled { color: var(--nz-muted); }

Button, ToggleButton, MenuButton, SplitButton {
    min-height: 32px;
    border-radius: 4px;
    transition: background-color 83ms linear;
}

/* WinUI ButtonPadding; sizes, shapes and icon-only buttons keep their more specific Basic padding. */
Button, ToggleButton { padding: 5px 11px 6px; }

Button[shape="square"], ToggleButton[shape="square"] { border-radius: 0; }
Button[size="medium"][shape="circle"], ToggleButton[size="medium"][shape="circle"],
Button[size="medium"][shape="square"], ToggleButton[size="medium"][shape="square"] { width: 32px; }

Button[appearance="standard"], ToggleButton[appearance="standard"],
MenuButton[appearance="standard"], SplitButton[appearance="standard"] {
    border-bottom-color: var(--nz-stroke-bottom);
    border-image-source: var(--nz-elevation);
    background-clip: padding-box;

    &:hover { background: var(--nz-control-hover); border-color: var(--nz-stroke-bottom); }
    &:active { background: var(--nz-control-pressed); border-color: var(--nz-stroke); border-image-source: none; }
    &:disabled { background: var(--nz-control-disabled); color: var(--nz-control-disabled-text); border-color: var(--nz-control-stroke-disabled); border-image-source: none; }
}

Button[default], Button[appearance="accent"], ToggleButton[appearance="accent"],
MenuButton[appearance="accent"], SplitButton[appearance="accent"] {
    border-image-source: var(--nz-accent-elevation);

    &:disabled { background: var(--nz-accent-disabled); color: var(--nz-on-accent-disabled); border-color: var(--nz-accent-stroke-disabled); border-image-source: none; }
}

Button[default]:not([intent="danger"]):not([intent="success"]):not([intent="warning"]),
Button[appearance="accent"][intent="neutral"], Button[appearance="accent"][intent="accent"],
ToggleButton[appearance="accent"], MenuButton[appearance="accent"], SplitButton[appearance="accent"][intent="neutral"] {
    &:hover { background: var(--nz-accent-hover); color: var(--nz-on-accent); border-color: var(--nz-accent-hover); }
    &:active { background: var(--nz-accent-pressed); color: var(--nz-on-accent-pressed); border-image-source: none; }
    &:disabled { background: var(--nz-accent-disabled); color: var(--nz-on-accent-disabled); border-color: var(--nz-accent-stroke-disabled); border-image-source: none; }
}

/* A checked WinUI ToggleButton is an accent button. */
ToggleButton[pressed][appearance="standard"] {
    background: var(--nz-accent);
    color: var(--nz-on-accent);
    border-color: var(--nz-accent);
    border-image-source: var(--nz-accent-elevation);

    &:hover { background: var(--nz-accent-hover); color: var(--nz-on-accent); border-color: var(--nz-accent-hover); }
    &:active { background: var(--nz-accent-pressed); color: var(--nz-on-accent-pressed); border-color: transparent; border-image-source: none; }
    &:disabled { background: var(--nz-accent-disabled); color: var(--nz-on-accent-disabled); border-color: var(--nz-accent-stroke-disabled); border-image-source: none; }
}

/* An open drop-down button keeps its pressed look while the menu is shown. */
MenuButton[expanded][appearance="standard"] {
    background: var(--nz-control-pressed);
    border-color: var(--nz-stroke);
    border-image-source: none;

    &:disabled { background: var(--nz-control-disabled); color: var(--nz-control-disabled-text); border-color: var(--nz-control-stroke-disabled); }
}

SplitButtonPrimary { border-radius: 3px 0 0 3px; }
SplitButtonTrigger { border-radius: 0 3px 3px 0; }
MenuButtonChevron, SplitButtonChevron, SelectChevron, ComboBoxChevron { width: 14px; height: 14px; stroke-width: 1px; }
Select:active > SelectChevron, ComboBoxChevron:active, MenuButton:active > MenuButtonChevron, SplitButtonTrigger:active > SplitButtonChevron {
    transform: translateY(1.875px);
    transition: transform 150ms cubic-bezier(0.167, 0.167, 0.65, 1);
}
Select[expanded] > SelectChevron, ComboBox[expanded] ComboBoxChevron,
MenuButton[expanded] > MenuButtonChevron, SplitButton[expanded] SplitButtonChevron {
    animation: nz-chevron-release 300ms linear;
}

@keyframes nz-chevron-release {
    from { transform: translateY(1.875px); animation-timing-function: cubic-bezier(0.55, 0, 0.75, 1); }
    27.5% { transform: translateY(-0.75px); animation-timing-function: cubic-bezier(0.35, 0, 0, 1); }
    to { transform: translateY(0); }
}

MenuBarItem, Tab, TabClose, TabsOverflow { border-radius: 4px; }
MenuBarItem:hover, Tab:hover, TabClose:hover, TabsOverflow:hover { background: var(--nz-subtle-hover); }
TabsIndicator { border-radius: 2px; }

/* Fields */
TextField, TextArea, ComboBox, PasswordField, NumberField, SearchField, MaskedField, FileField,
ShortcutField, TokenField, DatePicker, TimePicker, DateRangePicker, DateTimePicker {
    border-radius: 4px;
    min-height: 32px;
    /* WinUI TextControlThemePadding; the narrower end leaves room for the clear button. */
    padding: 5px 6px 6px 10px;

    &[appearance="outline"] {
        border-color: var(--nz-stroke);
        border-bottom-color: var(--nz-stroke-strong);
        border-image-source: var(--nz-field-elevation);
        background-clip: padding-box;

        &:hover { background: var(--nz-field-hover); }
        &:focus-within {
            background: var(--nz-input-focus);
            border-bottom-color: var(--nz-accent);
            border-image-source: none;
            box-shadow: inset 0 -1px 0 var(--nz-accent);
            outline-style: none;
        }
        &:disabled {
            background: var(--nz-field-disabled);
            color: var(--nz-field-disabled-text);
            border-color: var(--nz-control-stroke-disabled);
            border-image-source: none;
            box-shadow: none;
        }
    }

    &[appearance="plain"] { border-color: transparent; background: transparent; padding: 0; }
    &[appearance="underline"] { border-color: transparent; border-bottom-color: var(--nz-stroke-strong); background: transparent; border-radius: 0; }
    &[appearance="filled"] { border-color: transparent; background: var(--nz-control); }
    &[appearance="underline"]:focus-within, &[appearance="filled"]:focus-within { border-bottom-color: var(--nz-accent); outline-style: none; }
    &[invalid] { border-color: var(--nz-danger); border-image-source: none; }
    &[appearance][invalid]:focus-within { border-bottom-color: var(--nz-danger); box-shadow: inset 0 -1px 0 var(--nz-danger); }
    &[size="small"] { min-height: 24px; padding-top: 0; padding-bottom: 0; }
}

TextArea { min-height: calc(var(--nz-textarea-rows, 3) * 1.4em + 8px); }
ComboBoxLeading, ComboBoxTrailing, SearchFieldLeading, SearchFieldTrailing, TokenFieldLeading {
    display: flex;
    align-items: center;
    flex-shrink: 0;
}
SearchFieldMark { order: 3; width: 28px; height: 20px; padding: 0; color: var(--nz-text-secondary); stroke-trim-end: 0; border-radius: 4px; }
SearchField[search-icon-placement="start"] > SearchFieldMark { order: 0; }
SearchFieldMark::before { content: ""; position: absolute; left: 0; top: 0; width: 100%; height: 100%; display: flex; align-items: center; justify-content: center; font-family: "FluentSystemIcons-Regular"; font-size: 12px; line-height: 12px; pointer-events: none; }
SearchFieldMark::after { content: none; }
SearchFieldClear, TextFieldClear { position: relative; width: 28px; height: 20px; padding: 0; stroke-trim-end: 0; color: var(--nz-text-secondary); border-radius: 4px; }
SearchFieldClear::before, TextFieldClear::before { content: ""; font-symbol: "Segoe Fluent Icons" "", "Segoe MDL2 Assets" "", "FluentSystemIcons-Regular" ""; position: absolute; left: 0; top: 0; width: 100%; height: 100%; display: flex; align-items: center; justify-content: center; font-family: "FluentSystemIcons-Regular"; font-size: 12px; line-height: 12px; pointer-events: none; }
SearchFieldMark[role="button"]:hover, SearchFieldClear:hover, TextFieldClear:hover { background: var(--nz-subtle-hover); color: var(--nz-text); }
SearchFieldMark[role="button"]:active, SearchFieldClear:active, TextFieldClear:active { background: var(--nz-subtle-pressed); }
FlipViewPrevious, FlipViewNext { border-radius: 4px; border-color: var(--nz-stroke-strong); }
FlipViewPrevious:hover, FlipViewNext:hover { background: var(--nz-control-hover); color: var(--nz-on-control); }
PinFieldCell {
    width: 2em;
    min-height: 32px;
    line-height: max(30px, 1.5em);
    text-align: center;
    border: 1px solid var(--nz-stroke-strong);
    border-radius: 4px;
    background: var(--nz-field);
    color: var(--nz-on-field);
}
PinFieldCell:focus { border-color: var(--nz-accent); }
PinFieldCell[invalid] { border-color: var(--nz-danger); }
PinFieldCell:disabled { color: var(--nz-muted); border-color: var(--nz-stroke-disabled); }
NumberField[spin-buttons="inline"] > NumberFieldIncrement { order: 1; }
NumberField[spin-buttons="inline"] > NumberFieldDecrement { order: 2; }
NumberField[spin-buttons="inline"] > NumberFieldIncrement, NumberField[spin-buttons="inline"] > NumberFieldDecrement {
    width: 32px; height: 24px; padding: 5px 8px; border: 0; border-radius: 4px;
}
NumberField[spin-buttons="compact"] > NumberFieldSpinButtons { display: none; }
NumberFieldSpinToggle { display: flex; flex-direction: column; align-items: center; justify-content: center; flex-shrink: 0; width: 16px; height: 24px; color: var(--nz-text-secondary); }
NumberFieldSpinToggleUp, NumberFieldSpinToggleDown { width: 10px; height: 6px; stroke-width: 1px; }
NumberFieldSpinToggle:disabled { color: var(--nz-text-disabled); }
NumberFieldSpinPopup {
    display: flex; flex-direction: column; gap: 4px; padding: 6px; margin-left: -21px;
    border: 1px solid var(--nz-stroke-overlay); border-radius: 8px; background: var(--nz-layer); color: var(--nz-layer-text);
    box-shadow: 0 0 2px var(--nz-shadow-ambient), 0 8px 16px var(--nz-shadow);
}
NumberFieldSpinPopup > NumberFieldIncrement, NumberFieldSpinPopup > NumberFieldDecrement { width: 36px; height: 36px; padding: 10px; border: 0; border-radius: 4px; }
NumberFieldIncrement:hover, NumberFieldDecrement:hover { background: var(--nz-control-hover); }
NumberFieldIncrement:active, NumberFieldDecrement:active { background: var(--nz-control-pressed); color: var(--nz-text-secondary); }
NumberFieldIncrement:disabled, NumberFieldDecrement:disabled { color: var(--nz-text-disabled); background: transparent; }
TokenFieldToken { border-radius: 4px; }

Select {
    min-height: 32px;
    border-radius: 4px;
    padding: 3px 10px;
    transition: background-color 83ms linear;

    &[appearance="outline"] {
        background: var(--nz-control);
        border-color: var(--nz-stroke);
        border-bottom-color: var(--nz-stroke-bottom);
        border-image-source: var(--nz-elevation);
        background-clip: padding-box;
        &:hover { background: var(--nz-control-hover); }
        &:active, &[expanded] { background: var(--nz-control-pressed); border-image-source: none; }
        &:disabled { background: var(--nz-control-disabled); border-color: var(--nz-control-stroke-disabled); border-image-source: none; }
    }
    &[invalid] { border-color: var(--nz-danger); border-image-source: none; }
}

DatePicker:not([appearance="plain"]) { padding-right: 0; }
DatePickerButton {
    position: relative;
    width: 32px;
    height: 18px;
    padding: 0;
    border-color: transparent;
    background: transparent;
    color: var(--nz-text-secondary);
    stroke-trim-end: 0;
}
DatePickerButton:hover, DatePickerButton[expanded] { border-color: transparent; background: transparent; }
DatePickerButton::before {
    content: "";
    position: absolute;
    left: 0;
    top: 0;
    width: 100%;
    height: 100%;
    display: flex;
    align-items: center;
    justify-content: center;
    font-family: "FluentSystemIcons-Regular";
    font-size: 12px;
    line-height: 12px;
    color: inherit;
    pointer-events: none;
}
DatePicker:disabled > DatePickerButton, DatePickerButton:disabled { color: var(--nz-text-disabled); }

/* Overlay parts are styled directly because they can be rendered in the popup layer. */
Menu, ContextMenu, SelectPopup, ComboBoxPopup, SearchFieldPopup, TokenFieldPopup,
ComboboxPopup, DatePickerPopup, TimePickerPopup, DateRangePickerPopup,
Popover, PopoverContainer, HoverCard, TeachingTip, Dialog, DialogContainer, AlertDialog,
Drawer, CommandPalette, Tooltip, TooltipContainer, Toast {
    border-color: var(--nz-stroke-overlay);
    border-radius: 8px;
    background: var(--nz-layer);
    color: var(--nz-layer-text);
    /* Fluent shadow16: an ambient and a key layer, darker in the dark scheme. */
    box-shadow: 0 0 2px var(--nz-shadow-ambient), 0 8px 16px var(--nz-shadow);
}

TeachingTip {
    width: fit-content; min-width: min(320px, calc(100% - 24px)); max-width: min(336px, calc(100% - 24px));
    min-height: 40px; max-height: min(520px, calc(100% - 24px)); padding: 12px; gap: 0;
    border: 1px solid var(--nz-stroke-overlay); border-radius: 8px;
    background: var(--nz-layer); color: var(--nz-layer-text);
    box-shadow: 0 0 2px var(--nz-shadow-ambient), 0 16px 32px var(--nz-shadow);
    overlay-exit-duration: 200ms; animation: fluent-tip-expand 300ms cubic-bezier(0.1, 0.9, 0.2, 1) backwards;
}
TeachingTip[exiting] { animation: fluent-tip-contract 200ms cubic-bezier(0.7, 0, 1, 0.5) forwards; }
TeachingTipHero { flex-shrink: 0; margin: -12px -12px 0; border-radius: 7px 7px 0 0; overflow-x: hidden; overflow-y: hidden; }
TeachingTipHeader { flex-shrink: 0; gap: 0; }
TeachingTip[has-hero] > TeachingTipHeader { margin-top: 12px; }
TeachingTipHeading { gap: 4px; }
TeachingTipTitle { font-size: 14px; line-height: 20px; font-weight: 600; }
TeachingTipSubtitle { color: var(--nz-layer-text); font-size: 14px; line-height: 20px; }
TeachingTipHeader + TeachingTipBody, TeachingTipHero + TeachingTipBody { margin-top: 8px; }
TeachingTipBody { min-height: 0; padding: 4px; margin: -4px; overflow-y: auto; }
TeachingTipActions { flex-shrink: 0; gap: 8px; margin-top: 12px; }
TeachingTipActions > Button { flex: 1; min-width: 0; }
TeachingTipClose { width: 40px; height: 40px; padding: 12px; margin: -12px -12px -8px 0; border: 0; border-radius: 4px; background: transparent; }
TeachingTipClose:hover { background: var(--nz-subtle-hover); }
TeachingTipClose:active { background: var(--nz-subtle-pressed); color: var(--nz-text-secondary); }
TeachingTipArrow {
    width: 11.313708px; height: 11.313708px; margin: 10px; border: 0; border-radius: 0;
    border-top: 1px solid var(--nz-stroke-overlay); border-left: 1px solid var(--nz-stroke-overlay);
    background: var(--nz-layer); transform: rotate(45deg); box-shadow: none;
}
TeachingTip:popup-above > TeachingTipArrow { border-top: 0; border-left: 0; border-right: 1px solid var(--nz-stroke-overlay); border-bottom: 1px solid var(--nz-stroke-overlay); }
TeachingTip:popup-left > TeachingTipArrow { border-top: 1px solid var(--nz-stroke-overlay); border-left: 0; border-right: 1px solid var(--nz-stroke-overlay); border-bottom: 0; }
TeachingTip:popup-right > TeachingTipArrow { border-top: 0; border-left: 1px solid var(--nz-stroke-overlay); border-right: 0; border-bottom: 1px solid var(--nz-stroke-overlay); }
@keyframes fluent-tip-expand { from { opacity: 0; transform: scale(0.94); } to { opacity: 1; transform: scale(1); } }
@keyframes fluent-tip-contract { from { opacity: 1; transform: scale(1); } to { opacity: 0; transform: scale(0.94); } }

/* ContentDialog: a 24px content area over LayerFillColorAlt and a separate 24px command area on the base fill. */
Dialog, AlertDialog, DialogContainer { padding: 24px; background: var(--nz-dialog); }
Dialog, AlertDialog { box-shadow: 0 0 8px var(--nz-shadow-ambient), 0 32px 64px var(--nz-shadow); }
Dialog { --nz-dialog-title-font-size: 20px; --nz-dialog-title-line-height: 1.4; }
DialogTitle, AlertDialogTitle { font-weight: 600; }
AlertDialogTitle { font-size: 20px; line-height: 28px; }
DialogFooter, AlertDialogFooter {
    margin: 12px -24px -24px;
    padding: 24px;
    border-top: 1px solid var(--nz-card-stroke);
    border-radius: 0 0 7px 7px;
    background: var(--nz-surface);
}
DialogButton > Button { min-width: 96px; }

Menu, ContextMenu, SelectPopup, ComboBoxPopup, SearchFieldPopup, TokenFieldPopup, ComboboxPopup { padding: 4px; }
Menu, ContextMenu, MenuSection, MenuRadioGroup, SelectPopup, ComboBoxPopup, SearchFieldPopup, TokenFieldPopup, ComboboxPopup { row-gap: 2px; }
SearchFieldEmpty { min-height: 30px; padding: 4px 8px; color: var(--nz-text-secondary); }
ComboBoxPopup, SearchFieldPopup, TokenFieldPopup {
    display: flex;
    flex-direction: column;
    max-height: 300px;
    overflow-y: auto;
    border: 1px solid var(--nz-stroke-overlay);
}
ComboBoxPopup[drop-down] { max-height: none; }

Menu, ContextMenu { animation: nz-dropdown-open 250ms cubic-bezier(0.1, 0.9, 0.2, 1); }
SelectPopup, ComboBoxPopup { --nz-combo-open-top: 0%; --nz-combo-open-bottom: 100%; animation: nz-combo-open 250ms cubic-bezier(0, 0, 0, 1); }
SelectPopup:popup-above, ComboBoxPopup:popup-above { --nz-combo-open-top: 100%; --nz-combo-open-bottom: 0%; }
SelectPopup[drop-down="center"], ComboBoxPopup[drop-down="center"] { --nz-combo-open-top: 50%; --nz-combo-open-bottom: 50%; }

@keyframes nz-dropdown-open {
    from { opacity: 0; transform: translateY(-8px); }
    to { opacity: 1; transform: translateY(0); }
}

@keyframes nz-combo-open {
    from { clip-path: inset(var(--nz-combo-open-top) 0 var(--nz-combo-open-bottom)); }
    to { clip-path: inset(0); }
}

MenuItem, MenuCheckItem, MenuRadioItem, Submenu, SelectOption, ComboBoxOption, SearchFieldOption,
TokenFieldOption, ComboboxOption, TimePickerOption {
    min-height: 30px;
    padding: 4px 8px;
    border-radius: 4px;

    &:hover, &[current] { background: var(--nz-selection); color: var(--nz-on-selection); }
    &:disabled { background: transparent; color: var(--nz-muted); }
}

SelectOption[selected], ComboBoxOption[selected], TimePickerOption[selected], ListViewItem[selected], TreeViewItem[selected] {
    position: relative;
    background: var(--nz-selection);
    color: var(--nz-on-selection);
}

SelectOptionCheck { display: none; }
SelectOption, ComboBoxOption { padding-left: 12px; padding-right: 12px; }

/* The WinUI selection indicator: a 3 x 16 pill centered on the leading edge of the backplate. */
SelectOption[selected]::before, ComboBoxOption[selected]::before, TimePickerOption[selected]::before, ListViewItem[selected]::before, TreeViewItem[selected]::before {
    content: "";
    position: absolute;
    left: 0;
    top: 50%;
    width: 3px;
    height: 16px;
    margin-top: -8px;
    border-radius: 1.5px;
    background: var(--nz-accent);
}

SelectOption[selected]:disabled, ComboBoxOption[selected]:disabled, TimePickerOption[selected]:disabled,
ListViewItem[selected]:disabled, TreeViewItem[selected]:disabled { color: var(--nz-muted); }
SelectOption[selected]:disabled::before, ComboBoxOption[selected]:disabled::before, TimePickerOption[selected]:disabled::before,
ListViewItem[selected]:disabled::before, TreeViewItem[selected]:disabled::before { content: none; }

Tooltip, TooltipContainer {
    padding: 6px 9px 8px;
    border-radius: 4px;
    background: var(--nz-tooltip);
    color: var(--nz-tooltip-text);
    font-size: 12px;
    line-height: 16px;
}

/* Selection */
CheckBox, RadioButton { min-height: 32px; column-gap: 8px; border-radius: 4px; }
CheckBoxMark, RadioButtonMark { width: 20px; height: 20px; border-color: var(--nz-stroke-strong); background: var(--nz-control-alt); }
CheckBoxMark { padding: 3px; border-radius: 4px; stroke-width: 1.2px; transition: background-color 83ms linear, border-color 83ms linear; }
CheckBoxCheck, CheckBoxMixed { transform: scale(1); transition: stroke-trim-end 167ms linear, transform 167ms cubic-bezier(0, 0, 0, 1); }
CheckBox:hover > CheckBoxMark > CheckBoxCheck, CheckBox:hover > CheckBoxMark > CheckBoxMixed { transform: scale(1.166667); }
CheckBox:active > CheckBoxMark > CheckBoxCheck, CheckBox:active > CheckBoxMark > CheckBoxMixed { transform: scale(0.833333); }
CheckBox:disabled > CheckBoxMark > CheckBoxCheck, CheckBox:disabled > CheckBoxMark > CheckBoxMixed { transform: none; transition: none; }
CheckBox[size="small"] > CheckBoxMark, RadioButton[size="small"] > RadioButtonMark { width: 16px; height: 16px; }
CheckBox[size="large"] > CheckBoxMark, RadioButton[size="large"] > RadioButtonMark { width: 24px; height: 24px; }
/* WinUI check glyph: 12px at rest, 14px under the pointer and 10px pressed. */
RadioButtonDot { width: 12px; height: 12px; transition: transform 167ms cubic-bezier(0, 0, 0, 1); }
RadioButton[size="small"] RadioButtonDot { width: 8px; height: 8px; }
RadioButton[size="large"] RadioButtonDot { width: 14px; height: 14px; }

CheckBox:hover > CheckBoxMark, RadioButton:hover > RadioButtonMark, Switch:hover > SwitchTrack { background: var(--nz-control-alt-hover); }
CheckBox:active > CheckBoxMark, RadioButton:active > RadioButtonMark, Switch:active > SwitchTrack { background: var(--nz-control-alt-pressed); }
CheckBox:checked:hover > CheckBoxMark, CheckBox[indeterminate]:hover > CheckBoxMark,
RadioButton:checked:hover > RadioButtonMark, Switch:checked:hover > SwitchTrack { background: var(--nz-accent-hover); border-color: var(--nz-accent-hover); }
CheckBox:checked:active > CheckBoxMark, CheckBox[indeterminate]:active > CheckBoxMark,
RadioButton:checked:active > RadioButtonMark, Switch:checked:active > SwitchTrack { background: var(--nz-accent-pressed); border-color: var(--nz-accent-pressed); }
RadioButton:checked:hover > RadioButtonMark > RadioButtonDot, Switch:hover > SwitchTrack > SwitchThumbRail > SwitchThumbPosition > SwitchThumb { transform: scale(1.1667); }
RadioButton:checked:active > RadioButtonMark > RadioButtonDot { transform: scale(0.8333); }
/* The WinUI knob stretches from 12 x 12 to 17 x 14 while pressed. */
Switch:active > SwitchTrack > SwitchThumbRail > SwitchThumbPosition > SwitchThumb { transform: scale(1.4167, 1.1667); }

Switch[size="medium"] > SwitchTrack { min-width: 40px; height: 20px; padding: 0 3px; }
Switch[size="medium"] > SwitchTrack > SwitchThumbRail > SwitchThumbPosition > SwitchThumb { width: 12px; height: 12px; }
SwitchTrack { border-color: var(--nz-stroke-strong); background: var(--nz-control-alt); transition: background-color 83ms linear; }
SwitchThumb { background: var(--nz-text-secondary); transition: transform 200ms cubic-bezier(0.1, 0.9, 0.2, 1); }

/* Disabled boxes keep the transparent ControlAlt fill and a ControlStrongStrokeColorDisabled outline. */
CheckBox:disabled > CheckBoxMark, RadioButton:disabled > RadioButtonMark, Switch:disabled > SwitchTrack,
CheckBox:disabled:hover > CheckBoxMark, RadioButton:disabled:hover > RadioButtonMark,
Switch:disabled:hover > SwitchTrack { background: transparent; border-color: var(--nz-stroke-disabled); }
Switch:disabled > SwitchTrack > SwitchThumbRail > SwitchThumbPosition > SwitchThumb { background: var(--nz-text-disabled); transform: none; }
CheckBox:disabled:checked > CheckBoxMark, CheckBox:disabled[indeterminate] > CheckBoxMark,
RadioButton:disabled:checked > RadioButtonMark, Switch:disabled:checked > SwitchTrack {
    background: var(--nz-accent-disabled);
    border-color: var(--nz-accent-stroke-disabled);
    color: var(--nz-on-accent-disabled);
}
RadioButton:disabled:checked > RadioButtonMark > RadioButtonDot,
Switch:disabled:checked > SwitchTrack > SwitchThumbRail > SwitchThumbPosition > SwitchThumb { background: var(--nz-on-accent-disabled); transform: none; }

SegmentedControl, Chip, ChipRemove { border-radius: 4px; }

/* WinUI Slider: a 4px ControlStrongFill rail and a 20px thumb whose accent core is 12px at rest, 14px under the
   pointer and 10px pressed. Rails are inset by half a thumb so the fill ends at the thumb center. */
SliderTrack, RangeSliderTrack {
    border-width: 0;
    border-radius: 2px;
    background: var(--winui-control-strong-fill-color-default);
}
SliderFill, RangeSliderFill { border-radius: 2px; }
Slider > SliderTrack { left: 10px; right: 10px; top: 9px; height: 4px; }
Slider > SliderTicks { left: 10px; right: 10px; }
RangeSliderRail { left: 10px; right: 10px; }
RangeSliderTrack { top: 9px; height: 4px; }
SliderThumb, RangeSliderThumb {
    top: 1px;
    width: 20px;
    height: 20px;
    border: 4px solid var(--nz-control-solid);
    border-radius: 50%;
    background: var(--nz-accent);
    box-shadow: 0 0 0 1px var(--nz-stroke);
    transition: border-width 100ms ease-out;
}
SliderThumb { left: calc(var(--nz-slider-position) * (100% - 20px)); }
RangeSliderThumb { left: calc(var(--nz-range-thumb-position) * 100% - 10px); }
SliderThumb > SliderValueLabel, RangeSliderThumb > SliderValueLabel { bottom: 23px; left: -20px; }
Slider[orientation="vertical"] > SliderTrack { left: 9px; top: 10px; bottom: 10px; width: 4px; }
Slider[orientation="vertical"] > SliderTicks { top: 10px; bottom: 10px; }
Slider[orientation="vertical"] > SliderThumb { top: auto; left: 1px; bottom: calc(var(--nz-slider-position) * (100% - 20px)); }
Slider[orientation="vertical"] > SliderThumb > SliderValueLabel { bottom: auto; top: 1px; left: 24px; }
Slider:hover > SliderThumb, RangeSliderHandle:hover > RangeSliderThumb {
    border: 3px solid var(--nz-control-solid);
    background: var(--nz-accent-hover);
}
Slider:active > SliderThumb, RangeSliderHandle:active > RangeSliderThumb {
    border: 5px solid var(--nz-control-solid);
    background: var(--nz-accent-pressed);
}
Slider:disabled > SliderTrack, RangeSlider:disabled > RangeSliderRail > RangeSliderTrack { background: var(--nz-strong-fill-disabled); }
Slider:disabled > SliderTrack > SliderFill,
RangeSlider:disabled > RangeSliderRail > RangeSliderTrack > RangeSliderFill { background: var(--nz-accent-disabled); }
Slider:disabled > SliderThumb, RangeSlider:disabled > RangeSliderRail > RangeSliderHandle > RangeSliderThumb {
    border: 4px solid var(--nz-control-solid);
    background: var(--nz-accent-disabled);
}

Dial:hover > DialPointer { border-color: var(--nz-stroke-bottom); background: var(--nz-control-hover); }
Dial[dragging] > DialPointer { border-color: var(--nz-accent); background: var(--nz-control-pressed); }
Dial:focus-visible > DialPointer { outline: 2px solid var(--nz-focus); outline-offset: 1px; outline-inner-width: 1px; outline-inner-color: var(--nz-focus-inner); }
Dial:disabled > DialPointer { border-color: var(--nz-muted); background: var(--nz-surface); }

/* Collection surfaces, dates and feedback. WinUI rounds in-page surfaces with ControlCornerRadius (4px) and keeps
   OverlayCornerRadius (8px) for windows, flyouts and dialogs. */
Card, GroupBox, Expander, AccordionItem, RowGroupList, ListView, TreeView, GridView, DataGrid { border-radius: 4px; }

/* SettingsCard-style cards: CardBackgroundFill with CardStroke, ControlFill feedback when clickable. */
Card { border-color: var(--nz-card-stroke); background: var(--nz-card); }
Card[clickable]:hover { border-color: var(--nz-card-stroke); background: var(--nz-control-hover); }
Card[clickable]:active { border-color: var(--nz-card-stroke); background: var(--nz-control-pressed); }

/* WinUI Expander: a 48px header on CardBackgroundFill and a CardBackgroundFillSecondary panel with 16px padding. */
Accordion { gap: 4px; }
AccordionItem + AccordionItem { border-top-width: 1px; }
Expander, AccordionItem { border-color: var(--nz-card-stroke); }
ExpanderHeader, AccordionItemHeader {
    gap: 16px;
    min-height: 48px;
    padding: 8px 16px;
    border-radius: 3px;
    background: var(--nz-card);
    color: var(--nz-text);
}
Expander[expanded] > ExpanderHeader, AccordionItem[expanded] > AccordionItemHeader { border-radius: 3px 3px 0 0; }
ExpanderHeader:hover, AccordionItemHeader:hover { color: var(--nz-text); }
ExpanderHeader:active, AccordionItemHeader:active { background: var(--nz-card); color: var(--nz-text-secondary); }
ExpanderSubtitle, AccordionItemSubtitle { color: var(--nz-text-secondary); font-size: 12px; line-height: 16px; }
ExpanderChevron, AccordionItemChevron { width: 12px; height: 12px; stroke-width: 1px; }
ExpanderPanelContent, AccordionItemPanelContent {
    padding: 16px;
    border-top-color: var(--nz-card-stroke);
    border-radius: 0 0 3px 3px;
    background: var(--nz-card-secondary);
}
Expander:disabled > ExpanderHeader, AccordionItem:disabled > AccordionItemHeader { color: var(--nz-text-disabled); }
ListViewItem, TreeViewItem, GridViewItem, ActionRow, PaginationPage, CalendarTitle,
CalendarPrevious, CalendarNext, TimePickerOption, InlineMessageClose, BannerClose, ToastClose { border-radius: 4px; }
TimePickerColumns { gap: 4px; }
TimePickerColumn, TimePickerList { padding: 4px; gap: 2px; max-height: 240px; border: 0; background: transparent; color: var(--nz-layer-text); }
TimePickerColumn { width: 64px; }
TimePickerList { min-width: 140px; }
TimePickerOption { display: flex; align-items: center; justify-content: center; min-height: 32px; padding: 4px 12px; }
Calendar {
    width: max-content;
    min-width: 300px;
    gap: 0;
    border: 1px solid var(--nz-stroke);
    border-radius: 4px;
    background: var(--nz-input-focus);
    color: var(--nz-text);
    font-size: 14px;
    line-height: 20px;
}
CalendarHeader { height: 51px; gap: 0; border-bottom: 1px solid var(--nz-stroke); }
CalendarTitle {
    order: 0;
    height: 37px;
    margin: 6px 3px 7px 7px;
    padding: 7px 8px 8px;
    text-align: start;
    font-size: 14px;
    font-weight: 600;
    border-color: transparent;
}
CalendarPrevious, CalendarNext { position: relative; width: 32px; height: 37px; padding: 0; border-color: transparent; stroke-trim-end: 0; }
CalendarPrevious { order: 1; margin: 6px 3px 7px; }
CalendarNext { order: 2; margin: 6px 7px 7px 3px; }
CalendarPrevious::before, CalendarNext::before {
    position: absolute;
    left: 0;
    top: 0;
    width: 100%;
    height: 100%;
    display: flex;
    align-items: center;
    justify-content: center;
    font-family: "FluentSystemIcons-Filled";
    font-size: 12px;
    line-height: 12px;
    pointer-events: none;
}
CalendarPrevious::before { content: ""; }
CalendarNext::before { content: ""; }
CalendarTitle:hover, CalendarPrevious:hover, CalendarNext:hover { background: var(--nz-subtle-hover); border-color: transparent; }
CalendarTitle:active, CalendarPrevious:active, CalendarNext:active { background: var(--nz-subtle-pressed); color: var(--nz-text-secondary); }
CalendarTitle:disabled, CalendarPrevious:disabled, CalendarNext:disabled { color: var(--nz-text-disabled); background: transparent; border-color: transparent; }
CalendarMonth { gap: 0; }
CalendarMonthTitle { height: 24px; flex-shrink: 0; font-weight: 600; }
CalendarWeekdays { margin: 2px 2px 0; }
CalendarWeekday, CalendarWeekHeader { width: 42px; height: 42px; display: flex; align-items: center; justify-content: center; font-size: 12px; line-height: 16px; font-weight: 600; color: var(--nz-text); }
CalendarWeekdayAbbreviated { display: none; }
CalendarWeekdayShortest { display: block; }
CalendarGrid { margin: 2px; }
CalendarGrid[view="year"], CalendarGrid[view="decade"] {
    width: calc(var(--nz-calendar-month-count, 1) * (var(--nz-calendar-week-columns, 7) * 42px + 4px) + (var(--nz-calendar-month-count, 1) - 1) * 16px - 4px);
    height: calc(296px + (var(--nz-calendar-month-count, 1) - 1) * 24px);
}
CalendarGrid[view="year"] > CalendarRow, CalendarGrid[view="decade"] > CalendarRow { flex: 1; align-items: center; justify-content: space-around; }
CalendarItem { width: 56px; height: 56px; flex-shrink: 0; }
CalendarDay { width: 40px; height: 40px; margin: 1px; padding: 3px 0 4px; }
CalendarWeekNumber { width: 40px; height: 40px; margin: 1px; font-size: 12px; color: var(--nz-text-secondary); }
CalendarDay, CalendarItem { border-radius: 999px; border-color: transparent; background: transparent; color: var(--nz-text); font-weight: 400; }
CalendarDay:hover, CalendarItem:hover { background: var(--nz-subtle-hover); border-color: transparent; }
CalendarDay:active, CalendarItem:active { background: var(--nz-subtle-pressed); color: var(--nz-text-secondary); }
CalendarDay[outside], CalendarItem[outside] { color: var(--nz-text-secondary); }
CalendarDay[outside]:hover, CalendarItem[outside]:hover { color: var(--nz-text); }
CalendarDay[outside]:active, CalendarItem[outside]:active { color: var(--winui-text-fill-color-tertiary); }
CalendarDay[selected], CalendarItem[selected] { border-color: var(--nz-accent); background: transparent; color: var(--nz-accent-text); }
CalendarDay[selected]:hover, CalendarItem[selected]:hover { border-color: var(--nz-accent-hover); background: var(--nz-subtle-hover); }
CalendarDay[selected]:active, CalendarItem[selected]:active { border-color: var(--nz-subtle-pressed); background: var(--nz-subtle-pressed); color: var(--nz-accent-pressed); }
CalendarDay[today], CalendarItem[today] { background: var(--nz-accent); color: var(--nz-on-accent); font-weight: 400; border-color: transparent; }
CalendarDay[today]:hover, CalendarItem[today]:hover { background: var(--nz-accent-hover); }
CalendarDay[today]:active, CalendarItem[today]:active { background: var(--nz-accent-pressed); color: var(--nz-on-accent); }
CalendarDay[today][selected], CalendarItem[today][selected] { border-color: var(--nz-accent); box-shadow: inset 0 0 0 1px var(--nz-on-accent); }
CalendarItem[selected] { background: var(--nz-accent); color: var(--nz-on-accent); border-color: transparent; }
CalendarItem[selected]:hover { background: var(--nz-accent-hover); border-color: transparent; }
CalendarItem[selected]:active { background: var(--nz-accent-pressed); color: var(--nz-on-accent); border-color: transparent; }
CalendarDay[range-middle] { border-radius: 0; background: var(--nz-subtle-hover); color: var(--nz-text); border-color: transparent; }
CalendarDay[range-start], CalendarDay[range-end] { border-color: var(--nz-accent); color: var(--nz-accent-text); }
CalendarDay[range-preview] { background: var(--nz-subtle-hover); }
CalendarDay[unavailable], CalendarItem[unavailable] { color: var(--nz-text); background: transparent; border-color: transparent; text-decoration-line: line-through; }
CalendarDay[empty] { background: transparent; border-color: transparent; pointer-events: none; }
CalendarDay[today][unavailable], CalendarItem[today][unavailable] { background: var(--nz-accent-pressed); color: var(--nz-on-accent); }
CalendarDay:focus-visible, CalendarItem:focus-visible { outline: 2px solid var(--nz-focus); outline-offset: -3px; outline-inner-width: 1px; outline-inner-color: var(--nz-focus-inner); }
Calendar:disabled CalendarDay, Calendar:disabled CalendarItem { background: transparent; color: var(--nz-text-disabled); border-color: transparent; box-shadow: none; }
Calendar:disabled CalendarDay[selected], Calendar:disabled CalendarItem[selected] { border-color: var(--nz-accent-disabled); color: var(--nz-text-disabled); }
Calendar:disabled CalendarDay[today], Calendar:disabled CalendarItem[today] { background: var(--nz-accent-disabled); color: var(--nz-on-accent); }
Calendar:disabled CalendarWeekday, Calendar:disabled CalendarWeekHeader,
Calendar:disabled CalendarTitle, Calendar:disabled CalendarPrevious, Calendar:disabled CalendarNext { color: var(--nz-text-disabled); background: transparent; }
DatePickerPopup { padding: 0; gap: 0; border-width: 0; }
DatePickerPopup > Calendar { border-radius: 8px; }
@media (forced-colors: active) {
    Calendar { border-color: CanvasText; background: Canvas; }
    CalendarDay[outside], CalendarItem[outside] { color: LinkText; }
    CalendarDay[unavailable], CalendarItem[unavailable] { color: GrayText; }
    CalendarDay[selected], CalendarItem[selected] { border-color: Highlight; color: CanvasText; }
    CalendarDay[selected]:hover, CalendarItem[selected]:hover,
    CalendarDay[selected]:active, CalendarItem[selected]:active { border-color: Highlight; color: CanvasText; background: Canvas; }
    CalendarItem[selected], CalendarItem[selected]:hover, CalendarItem[selected]:active { background: Highlight; color: HighlightText; border-color: Highlight; }
    CalendarDay:hover, CalendarItem:hover { background: Canvas; border-color: CanvasText; }
    CalendarDay[today], CalendarItem[today], CalendarDay[today]:hover, CalendarItem[today]:hover,
    CalendarDay[today]:active, CalendarItem[today]:active { background: Highlight; color: HighlightText; border-color: Highlight; }
    Calendar:disabled CalendarDay, Calendar:disabled CalendarItem { color: GrayText; background: Canvas; }
    Calendar:disabled CalendarDay[selected], Calendar:disabled CalendarItem[selected] { border-color: GrayText; }
    Calendar:disabled CalendarDay[today], Calendar:disabled CalendarItem[today] { background: Highlight; color: HighlightText; }
}

ProgressBarSegment, MeterTrack, MeterFill { border-radius: 4px; }

Switch[size="medium"] { --nz-switch-inset: 3px; --nz-switch-thumb-width: 12px; --nz-switch-thumb-height: 12px; }
SwitchThumbPosition { transition: left 200ms cubic-bezier(0.1, 0.9, 0.2, 1); }
Switch[dragging]:not([drag-on]) > SwitchTrack { border-color: var(--nz-stroke-strong); background: var(--nz-control-alt-pressed); color: var(--nz-text-secondary); }
Switch[dragging][drag-on] > SwitchTrack { border-color: var(--nz-accent-pressed); background: var(--nz-accent-pressed); color: var(--nz-on-accent); }
Switch[dragging]:not([drag-on]) > SwitchTrack > SwitchThumbRail > SwitchThumbPosition > SwitchThumb { background: var(--nz-text-secondary); }
Switch[dragging][drag-on] > SwitchTrack > SwitchThumbRail > SwitchThumbPosition > SwitchThumb { background: var(--nz-on-accent); }
Switch[dragging] > SwitchTrack > SwitchThumbRail > SwitchThumbPosition { transition: none; }

/* WinUI ProgressBar: a 3px indicator (radius 1.5) over a centered 1px ControlStrongStroke track, in every size. */
ProgressBar:not([segmented]) > ProgressBarTrack, ProgressBar[size]:not([segmented]) > ProgressBarTrack {
    height: 3px;
    border-width: 0;
    border-radius: 1.5px;
    background: linear-gradient(to bottom, transparent 1px, var(--nz-stroke-strong) 1px, var(--nz-stroke-strong) 2px, transparent 2px);
}
ProgressBarFill, ProgressBarBuffer { border-radius: 1.5px; }
ProgressBarFill { transition: width 200ms cubic-bezier(0.1, 0.9, 0.2, 1); }

@media (prefers-reduced-motion: no-preference) {
    ProgressBarFill[indeterminate], ProgressBarPulse {
        position: absolute;
        top: 0;
        bottom: 0;
        left: 0;
        border-radius: 1.5px;
        background: var(--nz-feedback-color);
        transition: none;
    }
    ProgressBarFill[indeterminate] { width: 40%; animation: nz-progress-run 2s linear infinite; }
    ProgressBarPulse { display: block; width: 60%; animation: nz-progress-run-secondary 2s linear infinite; }
}

@keyframes nz-progress-run {
    from { transform: translateX(-100%); animation-timing-function: cubic-bezier(0.4, 0, 0.6, 1); }
    75%, to { transform: translateX(300%); }
}

@keyframes nz-progress-run-secondary {
    from { transform: translateX(-150%); }
    37.5% { transform: translateX(-150%); animation-timing-function: cubic-bezier(0.4, 0, 0.6, 1); }
    to { transform: translateX(166%); }
}

Spinner[indeterminate] SpinnerRing { animation: nz-progress-turn 2s linear infinite; }
@keyframes nz-progress-turn {
    from { transform: rotate(0deg); stroke-trim-start: 0; stroke-trim-end: 0.0001; }
    50% { transform: rotate(450deg); stroke-trim-start: 0; stroke-trim-end: 0.5; }
    to { transform: rotate(900deg); stroke-trim-start: 0.5; stroke-trim-end: 0.5; }
}

InlineMessage { background: var(--nz-info-background); border-color: var(--nz-card-stroke); border-radius: 4px; }
InlineMessage[severity="success"] { background: var(--nz-success-background); }
InlineMessage[severity="warning"] { background: var(--nz-warning-background); }
InlineMessage[severity="error"] { background: var(--nz-error-background); }
InlineMessageMark, BannerMark, ToastMark { border-radius: 50%; }

Button[appearance="accent"][intent="success"], Button[appearance="accent"][intent="warning"],
Button[appearance="accent"][intent="danger"], SplitButton[appearance="accent"][intent="danger"],
AlertDialogSeverity[severity="success"], AlertDialogSeverity[severity="warning"], AlertDialogSeverity[severity="error"] {
    color: var(--nz-surface);
}
BadgeIndicator[intent="success"], BadgeIndicator[intent="warning"], BadgeIndicator[intent="danger"] {
    --nz-on-feedback: var(--nz-surface);
}

@media (forced-colors: active) {
    SliderThumb, RangeSliderThumb,
    Slider:hover > SliderThumb, RangeSliderHandle:hover > RangeSliderThumb {
        border-color: ButtonText;
        background: ButtonFace;
    }

    SelectOption[selected]::before, ComboBoxOption[selected]::before,
    ListViewItem[selected]::before, TreeViewItem[selected]::before { background: HighlightText; }
    InlineMessage { border-color: CanvasText; }
}

SegmentedDatePicker, SegmentedTimePicker { min-height: 32px; border-radius: 4px; background: var(--nz-control); border-color: var(--nz-stroke); border-bottom-color: var(--nz-stroke-strong); }
SegmentedDatePicker:focus-within, SegmentedTimePicker:focus-within { border-color: var(--nz-stroke); border-bottom: 2px solid var(--nz-accent); }
SegmentedDatePicker:focus-visible, SegmentedTimePicker:focus-visible { outline-style: none; }
SegmentedDatePicker[invalid], SegmentedTimePicker[invalid] { border-bottom-color: var(--nz-danger); }
SegmentedPickerSegment { border-left-color: var(--nz-stroke); padding: 3px 0 6px; }
SegmentedDatePicker SegmentedPickerSegment[segment="month"] { flex-grow: 132; justify-content: flex-start; padding-left: 9px; }
SegmentedDatePicker SegmentedPickerSegment[segment="day"], SegmentedDatePicker SegmentedPickerSegment[segment="year"] { flex-grow: 78; }
SegmentedPickerSegment:hover { background: var(--nz-control-hover); }
SegmentedDatePicker:disabled, SegmentedTimePicker:disabled { color: var(--nz-text-disabled); background: var(--nz-control-disabled); }
SegmentedDatePickerPopup, SegmentedTimePickerPopup { border-color: var(--nz-stroke-overlay); border-radius: 8px; background: var(--nz-layer); color: var(--nz-layer-text); box-shadow: 0 0 2px var(--nz-shadow-ambient), 0 8px 16px var(--nz-shadow); }
SegmentedDatePickerPopup SegmentedPickerColumn[column="month"] { flex-grow: 132; }
SegmentedDatePickerPopup SegmentedPickerColumn[column="day"], SegmentedDatePickerPopup SegmentedPickerColumn[column="year"] { flex-grow: 78; }
SegmentedDatePickerPopup SegmentedPickerColumn[column="month"] > SegmentedPickerOption { justify-content: flex-start; padding-left: 9px; }
SegmentedPickerColumn { border-left-color: var(--nz-stroke); }
SegmentedPickerOption { padding-top: 3px; padding-bottom: 6px; }
SegmentedPickerOption:hover { background: var(--nz-control-hover); }
SegmentedPickerOption[selected] { background: var(--nz-accent); color: var(--nz-on-accent); }
SegmentedPickerActions { border-top-color: var(--nz-stroke); }
SegmentedPickerConfirm, SegmentedPickerCancel { border-radius: 4px; }
SegmentedPickerConfirm:hover, SegmentedPickerCancel:hover { background: var(--nz-control-hover); }

ContentTransition, FrameTransition { overlay-exit-duration: 450ms; }
ContentTransitionLayer, FrameTransitionLayer, CalendarTransitionLayer { transform-origin: 50% 50%; }
ContentTransitionLayer[current=true][transition=entrance][direction=forward], FrameTransitionLayer[current=true][transition=entrance][direction=forward] { animation: fluent-page-entrance 300ms cubic-bezier(0.1,0.9,0.2,1) 150ms both, fluent-page-show 150ms linear both; }
ContentTransitionLayer[exiting=true][transition=entrance][direction=forward], FrameTransitionLayer[exiting=true][transition=entrance][direction=forward] { animation: none; opacity: 0; transform: none; transition: transform 150ms cubic-bezier(0.7,0,1,0.5), opacity 150ms cubic-bezier(0.7,0,1,0.5); }
ContentTransitionLayer[current=true][transition=entrance][direction=backward], FrameTransitionLayer[current=true][transition=entrance][direction=backward] { animation: fluent-page-fade-in 300ms cubic-bezier(0.1,0.9,0.2,1) 150ms both; }
ContentTransitionLayer[exiting=true][transition=entrance][direction=backward], FrameTransitionLayer[exiting=true][transition=entrance][direction=backward] { animation: none; opacity: 0; transform: translateY(140px); transition: transform 150ms cubic-bezier(0.7,0,1,0.5), opacity 0ms linear 150ms; }
ContentTransitionLayer[current=true][transition=slide][direction=forward], FrameTransitionLayer[current=true][transition=slide][direction=forward] { animation: fluent-page-slide-in 300ms cubic-bezier(0.1,0.9,0.2,1) 150ms both, fluent-page-show 150ms linear both; }
ContentTransitionLayer[current=true][transition=slide][direction=backward], FrameTransitionLayer[current=true][transition=slide][direction=backward] { animation: fluent-page-slide-back-in 300ms cubic-bezier(0.1,0.9,0.2,1) 150ms both, fluent-page-show 150ms linear both; }
ContentTransitionLayer[exiting=true][transition=slide][direction=forward], FrameTransitionLayer[exiting=true][transition=slide][direction=forward] { animation: none; opacity: 0; transform: translateX(-150px); transition: transform 150ms cubic-bezier(0.7,0,1,0.5), opacity 0ms linear 150ms; }
ContentTransitionLayer[exiting=true][transition=slide][direction=backward], FrameTransitionLayer[exiting=true][transition=slide][direction=backward] { animation: none; opacity: 0; transform: translateX(150px); transition: transform 150ms cubic-bezier(0.7,0,1,0.5), opacity 0ms linear 150ms; }
ContentTransition:has(> [transition=drillin]), FrameTransition:has(> [transition=drillin]) { overlay-exit-duration: 783ms; }
ContentTransitionLayer[current=true][transition=drillin], FrameTransitionLayer[current=true][transition=drillin] { animation: fluent-page-drill-in 783ms cubic-bezier(0.1,0.9,0.2,1) both, fluent-page-fade-in 333ms cubic-bezier(0.17,0.17,0,1) both; }
ContentTransitionLayer[exiting=true][transition=drillin], FrameTransitionLayer[exiting=true][transition=drillin] { animation: none; opacity: 0; transform: scale(1.04); transition: transform 100ms cubic-bezier(0.1,0.9,0.2,1), opacity 100ms cubic-bezier(0.1,0.9,0.2,1); }
ContentTransitionLayer[current=true][transition=drillout], FrameTransitionLayer[current=true][transition=drillout] { animation: fluent-page-drill-out 333ms cubic-bezier(0.12,0,0,1) both, fluent-page-fade-in 333ms cubic-bezier(0.17,0.17,0,1) both; }
ContentTransitionLayer[exiting=true][transition=drillout], FrameTransitionLayer[exiting=true][transition=drillout] { animation: none; opacity: 0; transform: scale(0.96); transition: transform 100ms cubic-bezier(0.1,0.9,0.2,1), opacity 100ms cubic-bezier(0.1,0.9,0.2,1); }
CalendarTransition { overlay-exit-duration: 500ms; }
CalendarTransitionLayer[current=true][transition=drillout] { animation: fluent-calendar-zoom-out-in 350ms cubic-bezier(0,0,0,1) 150ms both; }
CalendarTransitionLayer[exiting=true][transition=drillout] { animation: none; opacity: 0; transform: scale(0.84); transition: transform 100ms cubic-bezier(0,0,0,1), opacity 100ms cubic-bezier(0,0,0,1); }
CalendarTransitionLayer[current=true][transition=drillin] { animation: fluent-calendar-zoom-in-in 350ms cubic-bezier(0,0,0,1) 150ms both; }
CalendarTransitionLayer[exiting=true][transition=drillin] { animation: none; opacity: 0; transform: scale(1.29); transition: transform 100ms cubic-bezier(0,0,0,1), opacity 100ms cubic-bezier(0,0,0,1); }
CalendarTransitionLayer[current=true][transition=slide][direction=forward] { animation: fluent-calendar-next 250ms cubic-bezier(0,0,0,1) both; }
CalendarTransitionLayer[current=true][transition=slide][direction=backward] { animation: fluent-calendar-previous 250ms cubic-bezier(0,0,0,1) both; }
CalendarTransitionLayer[exiting=true][transition=slide] { animation: none; opacity: 0; transform: none; transition: transform 150ms cubic-bezier(0,0,0,1), opacity 150ms cubic-bezier(0,0,0,1); }
Calendar[motion-cycle=odd] CalendarTitle { animation: fluent-calendar-header-odd 167ms linear both; }
Calendar[motion-cycle=even] CalendarTitle { animation: fluent-calendar-header-even 167ms linear both; }
LayoutTransition { transition: transform 250ms cubic-bezier(0,0,0,1); }
@keyframes fluent-page-entrance { from { transform: translateY(140px); } to { transform: none; } }
@keyframes fluent-page-show { from { opacity: 0; } 99.999% { opacity: 0; } to { opacity: 1; } }
@keyframes fluent-page-fade-in { from { opacity: 0; } to { opacity: 1; } }
@keyframes fluent-page-fade-out { from { opacity: 1; } to { opacity: 0; } }
@keyframes fluent-page-slide-in { from { transform: translateX(200px); } to { transform: none; } }
@keyframes fluent-page-slide-back-in { from { transform: translateX(-200px); } to { transform: none; } }
@keyframes fluent-page-drill-in { from { transform: scale(0.94); } to { transform: none; } }
@keyframes fluent-page-drill-out { from { transform: scale(1.06); } to { transform: none; } }
@keyframes fluent-calendar-zoom-out-in { from { opacity: 0; transform: scale(1.29); } to { opacity: 1; transform: none; } }
@keyframes fluent-calendar-zoom-in-in { from { opacity: 0; transform: scale(0.84); } to { opacity: 1; transform: none; } }
@keyframes fluent-calendar-next { from { opacity: 0; transform: translateY(40px); } to { opacity: 1; transform: none; } }
@keyframes fluent-calendar-previous { from { opacity: 0; transform: translateY(-40px); } to { opacity: 1; transform: none; } }
@keyframes fluent-calendar-header-odd { from { opacity: 0; } to { opacity: 1; } }
@keyframes fluent-calendar-header-even { from { opacity: 0; } to { opacity: 1; } }
@media (prefers-reduced-motion: reduce) {
    ContentTransition, FrameTransition, CalendarTransition { overlay-exit-duration: 0ms; }
}

ItemsTransition { display: flex; flex-direction: column; overlay-exit-duration: 200ms; }
ItemsTransitionItem { display: block; transition: transform 250ms cubic-bezier(0,0,0,1); }
ItemsTransitionContent { display: block; transition: opacity 150ms cubic-bezier(0,0,0,1), transform 150ms cubic-bezier(0,0,0,1); }
ItemsTransitionContent[entering=true] { animation: fluent-item-enter 250ms cubic-bezier(0,0,0,1) both; }
ItemsTransitionContent[exiting=true] { animation: none; opacity: 0; transform: translateY(-8px); }
@keyframes fluent-item-enter { from { opacity: 0; transform: translateY(12px); } to { opacity: 1; transform: none; } }

ExpanderPanel, AccordionItemPanel {
    --nz-collapsible-viewport: var(--nz-collapsible-height, 0px);
    overlay-exit-duration: 167ms;
    transition: none;
}
ExpanderPanel[closed=true], AccordionItemPanel[closed=true], ExpanderPanel[measuring=true], AccordionItemPanel[measuring=true] { --nz-collapsible-viewport: 0px; }
ExpanderPanelContent, AccordionItemPanelContent {
    transform: translateY(calc(0px - var(--nz-collapsible-height, 0px)));
    transition: none;
    animation: none;
}
ExpanderPanelContent[revealed=true], AccordionItemPanelContent[revealed=true] {
    transform: none;
    transition: transform 333ms cubic-bezier(0,0,0,1);
}
ExpanderPanel[exiting=true] > ExpanderPanelContent, AccordionItemPanel[exiting=true] > AccordionItemPanelContent {
    transition: transform 167ms cubic-bezier(1,1,0,1);
}
Expander[expand-direction=up] > ExpanderPanel, AccordionItem[expand-direction=up] > AccordionItemPanel { overlay-exit-duration: 200ms; }
Expander[expand-direction=up] > ExpanderPanel > ExpanderPanelContent,
AccordionItem[expand-direction=up] > AccordionItemPanel > AccordionItemPanelContent {
    transform: translateY(var(--nz-collapsible-height, 0px));
    border-top-width: 0;
    border-bottom: 1px solid var(--nz-card-stroke);
    border-radius: 3px 3px 0 0;
}
Expander[expand-direction=up] > ExpanderPanel > ExpanderPanelContent[revealed=true],
AccordionItem[expand-direction=up] > AccordionItemPanel > AccordionItemPanelContent[revealed=true] { transform: none; }
Expander[expand-direction=up][expanded] > ExpanderHeader,
AccordionItem[expand-direction=up][expanded] > AccordionItemHeader { border-radius: 0 0 3px 3px; }
ExpanderChevron, AccordionItemChevron { transform: rotate(0deg); transition: transform 333ms cubic-bezier(0,0,0,1); }
Expander[expanded] > ExpanderHeader > ExpanderChevron,
AccordionItem[expanded] > AccordionItemHeader > AccordionItemChevron { transform: rotate(180deg); }
Expander[expand-direction=up] > ExpanderHeader > ExpanderChevron,
AccordionItem[expand-direction=up] > AccordionItemHeader > AccordionItemChevron { transform: rotate(180deg); }
Expander[expand-direction=up][expanded] > ExpanderHeader > ExpanderChevron,
AccordionItem[expand-direction=up][expanded] > AccordionItemHeader > AccordionItemChevron { transform: rotate(0deg); }
@media (prefers-reduced-motion: reduce) {
    ExpanderPanel, AccordionItemPanel { overlay-exit-duration: 0ms; }
}

Dialog[size="auto"][dialog-placement="center"] {
    width: fit-content;
    min-width: min(320px, calc(100% - 48px)); max-width: min(548px, calc(100% - 48px));
    min-height: min(184px, calc(100% - 48px)); max-height: min(756px, calc(100% - 48px));
}
Dialog > DialogHeader, Dialog > DialogFooter { flex-shrink: 0; }
Dialog > DialogFooter > DialogButton[action-role="primary"], Dialog > DialogFooter > DialogButton[action-role="destructive"] { order: 1; }
Dialog > DialogFooter > DialogButton[action-role="secondary"] { order: 2; }
Dialog > DialogFooter > DialogButton[action-role="cancel"] { order: 3; }
Dialog[size="auto"] > DialogFooter > DialogButton { flex: 1; min-width: 0; }
Dialog[size="auto"] > DialogFooter > DialogButton[action-role="help"] { flex: 0 0 auto; }
Dialog[size="auto"] > DialogFooter > DialogButton > Button { min-width: 0; }
Dialog {
    overlay-exit-duration: 167ms;
    animation: fluent-dialog-show-scale 250ms cubic-bezier(0, 0, 0, 1) backwards, fluent-dialog-show-opacity 83ms linear backwards;
}
Dialog[exiting] {
    animation: fluent-dialog-hide-scale 167ms cubic-bezier(0, 0, 0, 1) forwards, fluent-dialog-hide-opacity 83ms linear forwards;
}
DialogScrim { animation: fluent-dialog-show-opacity 83ms linear backwards; }
DialogScrim[exiting] { animation: fluent-dialog-hide-opacity 83ms linear forwards; }
@keyframes fluent-dialog-show-scale { from { transform: scale(1.05); } to { transform: scale(1); } }
@keyframes fluent-dialog-hide-scale { from { transform: scale(1); } to { transform: scale(1.05); } }
@keyframes fluent-dialog-show-opacity { from { opacity: 0; } to { opacity: 1; } }
@keyframes fluent-dialog-hide-opacity { from { opacity: 1; } to { opacity: 0; } }
Dialog[size="auto"] > DialogFooter > DialogButton:only-child { flex-grow: 0; flex-basis: calc(50% - 4px); }
ListViewRowMetric { height: 40px; }
TreeViewRowMetric { height: 28px; }
ListViewItem, TreeViewItem, GridViewItem {
    background: transparent; color: var(--nz-text); border-radius: 4px;
    transition: transform 250ms cubic-bezier(0.1, 0.9, 0.2, 1), background-color 100ms, color 100ms;
}
ListViewItem, TreeViewItem { padding-right: 12px; }
ListViewItem { padding-left: 12px; }
ListViewItem:hover, TreeViewItem:hover, GridViewItem:hover { background: var(--nz-subtle-hover); color: var(--nz-text); }
ListViewItem:active, TreeViewItem:active, GridViewItem:active { background: var(--nz-subtle-pressed); color: var(--nz-text-secondary); }
ListViewItem[selected], TreeViewItem[selected] { background: var(--nz-subtle-hover); color: var(--nz-text); }
ListViewItem[selected]:hover, TreeViewItem[selected]:hover { background: var(--nz-subtle-pressed); }
ListViewItem[selected]:active, TreeViewItem[selected]:active { background: var(--nz-subtle-hover); color: var(--nz-text-secondary); }
GridViewItem[selected], GridViewItem[selected]:hover { background: var(--nz-subtle-pressed); color: var(--nz-text); border: 2px solid var(--nz-accent); }
GridViewItem[selected]:hover { border-color: var(--nz-accent-hover); }
GridViewItem[selected]:active { background: var(--nz-subtle-hover); border-color: var(--nz-accent-pressed); }
ListViewItem[current], TreeViewItem[current], GridViewItem[current] { outline: none; }
:is(ListViewItem, TreeViewItem, GridViewItem):focus-visible { outline: 2px solid var(--nz-text); outline-offset: -2px; }
:is(ListViewItem, TreeViewItem, GridViewItem):disabled { background: transparent; color: var(--nz-text-disabled); }
GridViewItem[selected]:disabled { border-color: var(--nz-accent-disabled); }
ListViewItem[selected]:disabled::before, TreeViewItem[selected]:disabled::before { content: ""; background: var(--nz-text-disabled); }
ListViewItemCheck, TreeViewItemCheck, GridViewItemCheck { width: 20px; height: 20px; border-radius: 3px; border-color: var(--nz-stroke-strong); background: var(--nz-control-alt); color: var(--nz-text); }
ListViewItemCheck[checked], TreeViewItemCheck[checked], TreeViewItemCheck[indeterminate], GridViewItemCheck[checked] { border-color: var(--nz-accent); background: var(--nz-accent); color: var(--nz-on-accent); }
:is(ListViewItem, TreeViewItem, GridViewItem):disabled > :is(ListViewItemCheck, TreeViewItemCheck, GridViewItemCheck) { border-color: var(--nz-text-disabled); background: transparent; color: var(--nz-text-disabled); }
ListViewSectionHeader { padding: 0 12px 2px; font-size: 14px; font-weight: 600; color: var(--nz-text-secondary); border: 0; }
TreeViewExpander { transition: transform 167ms cubic-bezier(0, 0, 0, 1); }
@media (forced-colors: active) {
    :is(ListViewItem, TreeViewItem, GridViewItem)[selected]:not(:disabled),
    :is(ListViewItem, TreeViewItem, GridViewItem)[selected]:not(:disabled):is(:hover, :active) { background: Highlight; color: HighlightText; }
    :is(ListViewItem, TreeViewItem, GridViewItem):focus-visible { outline-color: CanvasText; }
    GridViewItem[selected] { border-color: Highlight; }
}

Tabs[presentation=navigation] > TabsHeader { border-color: transparent; }
Tabs[presentation=navigation] Tab { min-height: 32px; padding: 4px 8px; background: transparent; color: var(--nz-text-secondary); border-radius: 4px; }
Tabs[presentation=navigation] Tab[selected] { background: transparent; color: var(--nz-text); font-weight: 600; }
Tabs[presentation=navigation] Tab:hover { background: var(--nz-subtle-hover); color: var(--nz-text); }
Tabs[presentation=navigation] Tab:active { background: var(--nz-subtle-pressed); color: var(--nz-text-secondary); }
Tabs[presentation=navigation] Tab:disabled { background: transparent; color: var(--nz-text-disabled); }
Tabs[presentation=navigation][orientation=horizontal] TabsIndicator {
    left: 0; top: 0; width: calc(var(--nz-indicator-width) - 16px); height: 3px; border-radius: 2px;
    transform: translate(calc(var(--nz-indicator-x) + 8px), calc(var(--nz-indicator-y) + var(--nz-indicator-height) - 3px));
    transition: none;
}
Tabs[presentation=navigation][orientation=horizontal][placement=bottom] TabsIndicator { transform: translate(calc(var(--nz-indicator-x) + 8px), var(--nz-indicator-y)); }
Tabs[presentation=navigation][orientation=vertical] TabsIndicator {
    left: 0; top: 0; width: 3px; height: calc(var(--nz-indicator-height) - 8px); border-radius: 2px;
    transform: translate(calc(var(--nz-indicator-x) + var(--nz-indicator-width) - 3px), calc(var(--nz-indicator-y) + 4px));
    transition: none;
}
Tabs[presentation=navigation][orientation=vertical][placement=end] TabsIndicator { transform: translate(var(--nz-indicator-x), calc(var(--nz-indicator-y) + 4px)); }
Tabs[presentation=navigation][indicator-ready=true] TabsIndicator { transition: transform 250ms cubic-bezier(0,0,0,1), width 250ms cubic-bezier(0,0,0,1), height 250ms cubic-bezier(0,0,0,1); }
Tabs[presentation=document] { --nz-tabs-add-width: 35px; }
Tabs[presentation=document] > TabsHeader { min-height: 40px; padding-top: 8px; border-bottom-width: 0; box-shadow: inset 0 -1px 0 var(--nz-stroke); }
Tabs[presentation=document] Tab {
    position: relative; height: 32px; min-height: 32px; padding: 3px 4px 3px 8px; gap: 0;
    border: 0 solid var(--nz-stroke); border-bottom-width: 1px; border-radius: 8px 8px 0 0; background: transparent;
    color: var(--nz-text-secondary); font-size: 12px; line-height: 16px;
}
Tabs[presentation=document] Tab[selected] { background: var(--winui-solid-background-fill-color-tertiary); color: var(--nz-text); font-weight: 600; border-bottom-color: transparent; z-index: 1; }
Tabs[presentation=document] Tab:hover { background: var(--nz-subtle-hover); }
Tabs[presentation=document] Tab:active { background: var(--nz-subtle-pressed); color: var(--nz-text-secondary); }
Tabs[presentation=document] Tab[selected]:hover, Tabs[presentation=document] Tab[selected]:active { background: var(--winui-solid-background-fill-color-tertiary); }
Tabs[presentation=document] Tab:disabled { background: transparent; color: var(--nz-text-disabled); }
Tabs[presentation=document] TabLeading { width: 16px; height: 16px; margin-right: 10px; }
Tabs[presentation=document] TabClose { display: flex; align-items: center; justify-content: center; width: 32px; height: 24px; padding: 0; margin-left: 4px; border-radius: 4px; font-size: 12px; }
Tabs[presentation=document] TabClose:hover { background: var(--nz-subtle-hover); color: var(--nz-text); }
Tabs[presentation=document] TabClose:active { background: var(--nz-subtle-pressed); color: var(--nz-text-secondary); }
Tabs[presentation=document] TabClose:disabled { background: transparent; color: var(--nz-text-disabled); }
Tabs[presentation=document] Tab::after { content: ""; position: absolute; right: 0; top: 8px; bottom: 8px; width: 1px; background: var(--winui-divider-stroke-color-default); pointer-events: none; }
Tabs[presentation=document] Tab:hover::after, Tabs[presentation=document] Tab:active::after,
Tabs[presentation=document] Tab:has(+ Tab[selected])::after, Tabs[presentation=document] Tab:has(+ Tab:hover)::after { background: transparent; }
Tabs[presentation=document] TabsAdd { width: 32px; height: 24px; padding: 0; margin: auto 0 3px 3px; border-width: 0; border-radius: 4px; font-size: 16px; line-height: 20px; color: var(--nz-text); }
Tabs[presentation=document] TabsAdd, Tabs[presentation=document] TabClose { font-size: 0; }
Tabs[presentation=document] TabsAdd::before { content: ""; font-family: FluentSystemIcons-Regular; font-size: 12px; line-height: 16px; }
Tabs[presentation=document] TabClose::before { content: ""; font-family: FluentSystemIcons-Regular; font-size: 12px; line-height: 16px; }
Tabs[presentation=document] TabsAdd:hover { background: var(--nz-subtle-hover); }
Tabs[presentation=document] TabsAdd:active { background: var(--nz-subtle-pressed); color: var(--nz-text-secondary); }
Tabs[presentation=document] TabsScrollBack, Tabs[presentation=document] TabsScrollForward { width: 32px; height: 24px; padding: 7px 3px; margin-bottom: 3px; }
Tabs[presentation=document][orientation=horizontal] TabsList { padding: 0 4px; }
Tabs[presentation=document][orientation=horizontal] TabsScroller { width: calc(var(--nz-tabs-count) * var(--nz-tab-max-width) + 8px); }
Tabs[presentation=document][orientation=horizontal][width-mode=equal] TabsList { min-width: calc(var(--nz-tabs-count) * var(--nz-tab-min-width) + 8px); max-width: calc(var(--nz-tabs-count) * var(--nz-tab-max-width) + 8px); }
Tabs[presentation=document][orientation=horizontal][width-mode=sizetocontent] TabsScroller, Tabs[presentation=document][orientation=horizontal][width-mode=compact] TabsScroller { width: max-content; }
Tabs[presentation=document] Tab[selected]::before, Tabs[presentation=document] Tab[selected]::after { content: ""; position: absolute; top: auto; bottom: 0; width: 4px; height: 4px; pointer-events: none; }
Tabs[presentation=document] Tab[selected]::before { left: -4px; background: radial-gradient(circle 4px at top left, transparent 4px, var(--winui-solid-background-fill-color-tertiary) 4px); }
Tabs[presentation=document] Tab[selected]::after { right: -4px; background: radial-gradient(circle 4px at top right, transparent 4px, var(--winui-solid-background-fill-color-tertiary) 4px); }
Tabs[presentation=document] TabsPanel { background: var(--winui-solid-background-fill-color-tertiary); padding: 0; }
Tabs[reorderable=true] Tab { transition: transform 240ms cubic-bezier(0,0,0,1), background-color 100ms linear, color 100ms linear; }
@media (forced-colors: active) {
    Tabs[presentation=document] Tab[selected] { background: Canvas; color: CanvasText; border: 1px solid Highlight; border-bottom-color: transparent; }
    Tabs[presentation=document] TabsPanel { background: Canvas; }
    Tabs[presentation=document] Tab::after { background: CanvasText; }
    Tabs[presentation=document] Tab[selected]::before, Tabs[presentation=document] Tab[selected]::after { background: transparent; }
    Tabs[presentation=document] Tab:disabled { color: GrayText; }
    Tabs[presentation=navigation] Tab[selected] { color: CanvasText; }
    Tabs[presentation=navigation] TabsIndicator { background: Highlight; }
    Tab[drop-target=true] { outline: 1px solid Highlight; outline-offset: -1px; }
}

Spinner > SpinnerIndicator { width: 32px; height: 32px; }
Spinner[size="small"] > SpinnerIndicator { width: 16px; height: 16px; }
Spinner[size="large"] > SpinnerIndicator { width: 64px; height: 64px; }
Spinner[size] > SpinnerIndicator > SpinnerTrack, Spinner[size] > SpinnerIndicator > SpinnerRing { stroke-width: 8.296875%; stroke-radius: 44.25%; }
Spinner[indeterminate][size] > SpinnerIndicator > SpinnerTrack, Spinner[indeterminate][size] > SpinnerIndicator > SpinnerRing { stroke-width: 9.375%; stroke-radius: 43.75%; }
SpinnerTrack { color: transparent; }
Spinner:not([indeterminate]) > SpinnerIndicator { number-motion: linear-increase; number-motion-duration: 2s; number-boundary-tolerance: 0.00833333377; }
Spinner[indeterminate] > SpinnerIndicator { number-motion: none; number-boundary-tolerance: 0; }
Spinner:not([indeterminate]) > SpinnerIndicator > SpinnerRing { stroke-trim-end: clamp(0, calc(9.99999975e-05 + 1.03406896739604 * max(0, calc(var(--nz-presented-number) - 0.00833333377)) - 0.0340689673960419 * max(0, calc(var(--nz-presented-number) - 0.25)) - 0.0714286623469327 * max(0, calc(var(--nz-presented-number) - 0.75)) + 3.07143298235072 * max(0, calc(var(--nz-presented-number) - 0.983333349)) - 4.00000432000379 * max(0, calc(var(--nz-presented-number) - 0.991666675))), 1); }
Spinner:not([indeterminate]) > SpinnerIndicator[number-at-start] > SpinnerRing { opacity: 0; }
Spinner:not([indeterminate]) > SpinnerIndicator[number-at-end] > SpinnerRing { stroke-trim-end: 1; }

PipsPager { gap: 0; }
PipsPagerItems { gap: 0; }
PipsPagerPip { width: 12px; height: 24px; border-radius: 4px; }
PipsPager[orientation=vertical] PipsPagerPip { width: 24px; height: 12px; }
PipsPagerDot { width: 4px; height: 4px; background: var(--winui-control-strong-fill-color-default); }
PipsPagerPip[current] > PipsPagerDot { width: 6px; height: 6px; background: var(--winui-control-strong-fill-color-default); }
PipsPagerPip:hover { background: transparent; }
PipsPagerPip:hover > PipsPagerDot { width: 6px; height: 6px; background: var(--nz-text-secondary); }
PipsPagerPip:active { background: transparent; }
PipsPagerPip:active > PipsPagerDot { width: 4px; height: 4px; background: var(--nz-text-secondary); }
PipsPagerPip:focus-visible { outline: 2px solid var(--nz-focus); outline-inner-width: 1px; outline-inner-color: var(--nz-focus-inner); outline-offset: -2px; }
PipsPagerPrevious, PipsPagerNext { width: 24px; height: 24px; padding: 8px; color: var(--winui-control-strong-fill-color-default); }
PipsPagerPrevious:hover, PipsPagerNext:hover, PipsPagerPrevious:active, PipsPagerNext:active { background: transparent; color: var(--nz-text-secondary); }
PipsPagerPrevious:disabled, PipsPagerNext:disabled { color: var(--winui-control-strong-fill-color-disabled); }
PipsPager:disabled PipsPagerDot { background: var(--winui-control-strong-fill-color-disabled); }
@media (forced-colors: active) {
    PipsPagerPip[current] > PipsPagerDot { background: Highlight; }
    PipsPager:disabled PipsPagerDot { background: GrayText; }
}
:is(Menu, ContextMenu, Popover, PopoverContainer, HoverCard, DatePickerPopup, DateRangePickerPopup) {
    --nz-fluent-flyout-x: 0px; --nz-fluent-flyout-y: -50px;
    opacity: 1; transform: translate(0px, 0px);
    transition: opacity 150ms linear, transform 333ms cubic-bezier(0,0,0,1);
    overlay-exit-duration: 150ms;
    animation: fluent-flyout-translate 333ms cubic-bezier(0,0,0,1) backwards, fluent-overlay-fade-in 83ms linear backwards;
}
:is(Menu, ContextMenu, Popover, PopoverContainer, HoverCard, DatePickerPopup, DateRangePickerPopup):popup-above { --nz-fluent-flyout-y: 50px; }
:is(Menu, ContextMenu, Popover, PopoverContainer, HoverCard, DatePickerPopup, DateRangePickerPopup):popup-left { --nz-fluent-flyout-x: 50px; --nz-fluent-flyout-y: 0px; }
:is(Menu, ContextMenu, Popover, PopoverContainer, HoverCard, DatePickerPopup, DateRangePickerPopup):popup-right { --nz-fluent-flyout-x: -50px; --nz-fluent-flyout-y: 0px; }
:is(Menu, ContextMenu, Popover, PopoverContainer, HoverCard, DatePickerPopup, DateRangePickerPopup)[exiting] { opacity: 0; animation: none; }
:is(Menu, ContextMenu, Popover, PopoverContainer, HoverCard, DatePickerPopup, DateRangePickerPopup)[reentered] { animation: none; }
:is(SelectPopup, ComboBoxPopup, ComboboxPopup, SearchFieldPopup, TokenFieldPopup, NumberFieldSpinPopup) {
    opacity: 1; transition: opacity 150ms linear; overlay-exit-duration: 150ms;
}
:is(SelectPopup, ComboBoxPopup, ComboboxPopup, SearchFieldPopup, TokenFieldPopup, NumberFieldSpinPopup)[exiting] { opacity: 0; animation: none; }
:is(SelectPopup, ComboBoxPopup, ComboboxPopup, SearchFieldPopup, TokenFieldPopup, NumberFieldSpinPopup)[reentered] { animation: none; }
Tooltip, TooltipContainer {
    opacity: 1; max-width: 320px; transition: opacity 150ms linear; overlay-exit-duration: 150ms;
    animation: fluent-overlay-fade-in 250ms linear backwards;
}
Tooltip[exiting], TooltipContainer[exiting] { opacity: 0; animation: none; }
Tooltip[reentered], TooltipContainer[reentered] { animation: none; }
Drawer {
    --nz-fluent-drawer-x: 100%; --nz-fluent-drawer-y: 0px;
    opacity: 1; transform: translate(0px, 0px); overlay-exit-duration: 200ms;
    transition: transform 333ms cubic-bezier(0,0,0,1), opacity 200ms linear;
    animation: fluent-drawer-enter 333ms cubic-bezier(0,0,0,1) backwards;
}
Drawer[side=start] { --nz-fluent-drawer-x: -100%; }
Drawer[side=top] { --nz-fluent-drawer-x: 0px; --nz-fluent-drawer-y: -100%; }
Drawer[side=bottom] { --nz-fluent-drawer-x: 0px; --nz-fluent-drawer-y: 100%; }
Drawer[exiting] { transform: translate(var(--nz-fluent-drawer-x), var(--nz-fluent-drawer-y)); opacity: 0; animation: none; transition: transform 200ms cubic-bezier(1,0,1,1), opacity 200ms linear; }
Drawer[reentered] { animation: none; }
DrawerScrim { opacity: 1; animation: fluent-overlay-fade-in 83ms linear backwards; transition: opacity 200ms linear; }
DrawerScrim[exiting] { opacity: 0; animation: none; }
DrawerScrim:has(+ Drawer[reentered]) { animation: none; }
@keyframes fluent-flyout-translate { from { transform: translate(var(--nz-fluent-flyout-x), var(--nz-fluent-flyout-y)); } to { transform: translate(0px, 0px); } }
@keyframes fluent-overlay-fade-in { from { opacity: 0; } to { opacity: 1; } }
@keyframes fluent-drawer-enter { from { transform: translate(var(--nz-fluent-drawer-x), var(--nz-fluent-drawer-y)); } to { transform: translate(0px, 0px); } }
:is(Dialog, AlertDialog, DialogContainer) {
    opacity: 1; transform: scale(1); overlay-exit-duration: 167ms;
    transition: transform 250ms cubic-bezier(0,0,0,1), opacity 83ms linear;
    animation: fluent-dialog-show-scale 250ms cubic-bezier(0,0,0,1) backwards, fluent-dialog-show-opacity 83ms linear backwards;
}
:is(Dialog, AlertDialog, DialogContainer)[exiting] {
    opacity: 0; transform: scale(1.05); animation: none;
    transition: transform 167ms cubic-bezier(0,0,0,1), opacity 83ms linear;
}
:is(Dialog, AlertDialog, DialogContainer)[reentered] { animation: none; }
:is(DialogScrim, AlertDialogScrim, DialogContainerScrim) {
    opacity: 1; animation: fluent-dialog-show-opacity 83ms linear backwards; transition: opacity 83ms linear;
}
:is(DialogScrim, AlertDialogScrim, DialogContainerScrim)[exiting] { opacity: 0; animation: none; }
:is(DialogScrim, AlertDialogScrim, DialogContainerScrim):has(+ :is(Dialog, AlertDialog, DialogContainer)[reentered]) { animation: none; }
BreadcrumbItemContent > Button { padding: 6px 8px; border: 0; border-radius: 4px; font-size: 14px; }
BreadcrumbItemContent > Button:hover { background: var(--nz-subtle-hover); }
BreadcrumbItemContent > Button:active { background: var(--nz-subtle-pressed); color: var(--nz-text-secondary); }
BreadcrumbSlot[current] Button { font-weight: 600; }
BreadcrumbSeparator { width: 12px; height: 12px; margin-left: 4px; color: var(--nz-text-secondary); }
MenuButton.breadcrumb-overflow { width: 32px; height: 32px; padding: 4px; }
SearchFieldMark::before { font-symbol: "Segoe Fluent Icons" "", "Segoe MDL2 Assets" "", "FluentSystemIcons-Regular" ""; }
DatePickerButton::before { font-symbol: "Segoe Fluent Icons" "", "Segoe MDL2 Assets" "", "FluentSystemIcons-Regular" ""; }
CalendarPrevious::before { font-symbol: "Segoe Fluent Icons" "", "Segoe MDL2 Assets" "", "FluentSystemIcons-Filled" ""; }
CalendarNext::before { font-symbol: "Segoe Fluent Icons" "", "Segoe MDL2 Assets" "", "FluentSystemIcons-Filled" ""; }
Tabs[presentation=document] TabsAdd::before { font-symbol: "Segoe Fluent Icons" "", "Segoe MDL2 Assets" "", "FluentSystemIcons-Regular" ""; }
Tabs[presentation=document] TabClose::before { font-symbol: "Segoe Fluent Icons" "", "Segoe MDL2 Assets" "", "FluentSystemIcons-Regular" ""; }
MessageBoxIcon::before { font-symbol: "Segoe Fluent Icons" "", "Segoe MDL2 Assets" "", "FluentSystemIcons-Filled" ""; }
MessageBoxIcon[icon="warning"]::before { font-symbol: "Segoe Fluent Icons" "", "Segoe MDL2 Assets" "", "FluentSystemIcons-Filled" ""; }
MessageBoxIcon[icon="error"]::before { font-symbol: "Segoe Fluent Icons" "", "Segoe MDL2 Assets" "", "FluentSystemIcons-Filled" ""; }
MessageBoxIcon[icon="question"]::before { font-symbol: "Segoe Fluent Icons" "", "Segoe MDL2 Assets" "", "FluentSystemIcons-Filled" ""; }
PasswordFieldReveal::before { font-symbol: "Segoe Fluent Icons" "", "Segoe MDL2 Assets" "", "FluentSystemIcons-Regular" ""; }
PasswordFieldReveal { position: relative; stroke-trim-end: 0; }
PasswordFieldReveal::before { content: ""; position: absolute; left: 0; top: 0; width: 100%; height: 100%; display: flex; align-items: center; justify-content: center; font-family: "FluentSystemIcons-Regular"; font-size: 12px; line-height: 12px; pointer-events: none; }
NavigationViewChrome { color: var(--nz-text); }
NavigationViewToggle:hover { background: var(--nz-subtle-hover); }
NavigationViewToggle:active { background: var(--nz-subtle-pressed); color: var(--nz-text-secondary); }
NavigationViewToggle:disabled { color: var(--nz-text-disabled); }
NavigationView[display-mode=left] SplitViewPane, NavigationView[display-mode=top] SplitViewPane { background: transparent; border-color: transparent; }
NavigationView[display-mode=compact] SplitViewPane, NavigationView[display-mode=minimal] SplitViewPane { background: var(--nz-layer); }
NavigationViewPaneHeader, NavigationViewPaneFooter { min-height: 40px; }
NavigationViewTreeHost TreeViewRowMetric { height: 40px; }
NavigationViewTreeHost TreeViewItem { padding: 2px 4px; min-height: 36px; color: var(--nz-text); }
NavigationViewTreeHost TreeViewItem[selected] { background: var(--nz-subtle-hover); color: var(--nz-text); }
NavigationViewTreeHost TreeViewItem[selected]:hover { background: var(--nz-subtle-pressed); }
NavigationViewTreeHost TreeViewItem:active { background: var(--nz-subtle-pressed); color: var(--nz-text-secondary); }
NavigationViewTreeHost TreeViewItem:disabled { color: var(--nz-text-disabled); }
NavigationViewItemIcon { width: 16px; height: 16px; }
NavigationViewRootItem { color: var(--nz-text); }
NavigationViewRootItem[selected] { background: var(--nz-subtle-hover); color: var(--nz-text); }
NavigationViewRootItem:hover { background: var(--nz-subtle-hover); }
NavigationViewRootItem:active { background: var(--nz-subtle-pressed); color: var(--nz-text-secondary); }
NavigationViewRootItem:disabled { color: var(--nz-text-disabled); background: transparent; }
NavigationView[display-mode=top] NavigationViewRootItem[selected], NavigationView[display-mode=top] NavigationViewRootItem[selected]:hover,
NavigationView[display-mode=top] NavigationViewRootItem[selected]:active { background: transparent; color: var(--nz-text); }
NavigationViewRootItem:focus-visible, NavigationViewToggle:focus-visible { outline: 2px solid var(--nz-focus); outline-inner-width: 1px; outline-inner-color: var(--nz-focus-inner); outline-offset: -2px; }
NavigationView[tree-ready=true] NavigationViewTreeIndicator, NavigationView[roots-ready=true] NavigationViewRootIndicator { transition: transform 250ms cubic-bezier(0,0,0,1), width 250ms cubic-bezier(0,0,0,1), height 250ms cubic-bezier(0,0,0,1); }
NavigationViewContent { background: var(--nz-layer); border: 1px solid var(--nz-card-stroke); border-radius: 8px 0 0 0; }
NavigationView[display-mode=top] NavigationViewContent, NavigationView[display-mode=minimal] NavigationViewContent { border-radius: 0; }
@media (forced-colors: active) {
    NavigationViewTreeHost TreeViewItem[selected], NavigationViewRootItem[selected] { background: Highlight; color: HighlightText; }
    NavigationView[display-mode=top] NavigationViewRootItem[selected] { background: Canvas; color: CanvasText; }
    NavigationViewContent { background: Canvas; border-color: CanvasText; }
    NavigationViewRootItem:disabled, NavigationViewToggle:disabled { color: GrayText; }
}
CalendarPrevious::before, CalendarNext::before { font-size: 8px; line-height: 8px; }

NavigationViewToggle { font-size: 0; }
NavigationViewToggle::before { content: ""; font-family: FluentSystemIcons-Regular; font-size: 16px; line-height: 20px; pointer-events: none; font-symbol: "Segoe Fluent Icons" "", "Segoe MDL2 Assets" "", "FluentSystemIcons-Regular" ""; }
ColorPicker { width: 312px; padding: 4px 0; gap: 16px; }
ColorPickerVisuals { display: grid; grid-template-columns: minmax(0, 1fr) 44px; gap: 16px 12px; padding: 0; }
ColorPickerSpectrum { grid-column: 1; grid-row: 1; height: 256px; border-radius: 4px; }
ColorPickerPreview { grid-column: 2; grid-row: 1; flex-direction: column; gap: 0; height: auto; border: 1px solid var(--nz-stroke); border-radius: 4px; overflow-x: hidden; overflow-y: hidden; }
ColorPickerSwatch { border: 0; border-radius: 0; }
ColorPickerSlider, ColorPickerAlphaSlider { grid-column: 1 / span 2; height: 32px; padding: 10px 0; }
ColorPickerSlider { grid-row: 2; }
ColorPickerAlphaSlider { grid-row: 3; }
ColorPickerSliderThumb, ColorPickerAlphaSliderThumb { width: 20px; height: 20px; left: calc(var(--nz-color-slider-position) - 10px); top: calc(50% - 10px); border: 5px solid var(--nz-input-focus); outline: 1px solid var(--nz-stroke); background: var(--nz-text); }
ColorPickerInputs { display: grid; grid-template-columns: 120px minmax(0, 1fr); gap: 12px 8px; }
ColorPickerRepresentation { grid-column: 1; grid-row: 1; }
ColorPickerHex { grid-column: 2; grid-row: 1; justify-self: end; width: 132px; }
ColorPickerHex > Label { display: none; }
ColorPickerChannels, ColorPicker[alpha-enabled] ColorPickerChannels { grid-column: 1 / span 2; grid-row: 2; display: flex; flex-direction: column; gap: 12px; }
ColorPickerChannel { flex-direction: row; align-items: center; gap: 8px; }
ColorPickerChannel > Label { order: 1; font-size: 14px; }
ColorPickerChannel > TextField { order: 0; width: 120px; flex-shrink: 0; }
ColorPicker[orientation=horizontal] { width: 744px; }
ColorPicker[orientation=horizontal] ColorPickerVisuals { grid-template-columns: minmax(0, 1fr) 44px 32px 32px; padding: 0; }
ColorPicker[orientation=horizontal] ColorPickerInputs { width: 300px; }
ColorPicker[orientation=horizontal] ColorPickerSlider, ColorPicker[orientation=horizontal] ColorPickerAlphaSlider { height: 256px; }
ColorPickerSlider[orientation=vertical], ColorPickerAlphaSlider[orientation=vertical] { width: 32px; padding: 0 10px; }
ColorPickerSlider[orientation=vertical] ColorPickerSliderThumb, ColorPickerAlphaSlider[orientation=vertical] ColorPickerAlphaSliderThumb { left: calc(50% - 10px); bottom: calc(var(--nz-color-slider-position) - 10px); }
@media (forced-colors: active) {
    ColorPickerSliderThumb, ColorPickerAlphaSliderThumb { border-color: Canvas; outline-color: CanvasText; background: CanvasText; }
    ColorPickerPreview { border-color: CanvasText; }
}
ColorPicker:not([preview-visible]) ColorPickerVisuals, ColorPicker:not([spectrum-visible]) ColorPickerVisuals { grid-template-columns: minmax(0, 1fr); }
ColorPicker:not([spectrum-visible]) ColorPickerPreview { grid-column: 1; height: 44px; flex-direction: row; }
ColorPicker:not([channels-visible]) ColorPickerHex { grid-column: 1 / span 2; justify-self: start; }
ColorPicker[orientation=horizontal] ColorPickerPreview { grid-column: 2; }
ColorPicker[orientation=horizontal] ColorPickerSlider { grid-column: 3; }
ColorPicker[orientation=horizontal] ColorPickerAlphaSlider { grid-column: 4; }
NavigationViewTreeHost TreeViewItem { transition: background-color 83ms, color 83ms; }
