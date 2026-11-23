/*
 * NazorLeaf is the Nazor visual language.
 * Four signs. The leaf: two wide corners on one diagonal, two nearly sharp ones, on every shape. The seed: a small leaf
 * marks the selection. The glow: filled elements cast their own color instead of a gray shadow.
 * The four-leaf mark: the brand, and the loading indicator (LeafMark).
 * NazorLeaf sits on top of Basic (NazorLeafTheme.Theme combines them), so these rules only change the look: a part that is not
 * restyled here keeps its Basic geometry, drawn with the NazorLeaf colors through the --nz-* tokens.
 */

/* ==== Tokens ==== */
:root {
    font-optical-sizing: auto;
    /* The seed (blue, light). Retune these five, in both color schemes, to give an application its own color. */
    --leaf-primary: #0b57d0;
    --leaf-on-primary: #ffffff;
    --leaf-pc: #d3e3fd;
    --leaf-on-pc: #041e49;
    --leaf-ink: #0b57d0;

    /* Neutral surfaces before the seed tint: tray (window), pane (content), s1..s3 (layers, tracks). */
    --leaf-n-tray: #e4e7ec;
    --leaf-n-pane: #ffffff;
    --leaf-n-s1: #f5f6f8;
    --leaf-n-s2: #edeff2;
    --leaf-n-s3: #dfe2e8;
    --leaf-text: #191b1f;
    --leaf-text-2: #4a4e57;
    --leaf-text-3: #737884;
    --leaf-stroke: rgba(16, 22, 40, 0.09);
    --leaf-stroke-2: rgba(16, 22, 40, 0.17);
    --leaf-hi: rgba(255, 255, 255, 0.75);
    --leaf-ctl: #ffffff;
    --leaf-mixh: 7%;
    --leaf-mixp: 12%;
    --leaf-tint: 9%;

    --leaf-ok: #188038;
    --leaf-ok-c: #e6f4ea;
    --leaf-on-ok-c: #0d652d;
    --leaf-warn: #b06000;
    --leaf-warn-c: #fef3d6;
    --leaf-on-warn-c: #6e3c00;
    --leaf-err: #c5221f;
    --leaf-on-err: #ffffff;
    --leaf-err-c: #fce8e6;
    --leaf-on-err-c: #8c1d18;
    /* The four colors of the window lights. */
    --leaf-g1: #4285f4;
    --leaf-g2: #ea4335;
    --leaf-g3: #fbbc04;
    --leaf-g4: #34a853;

    --leaf-sh-pane: 0 1px 2px rgba(16, 22, 40, 0.04);
    --leaf-sh-pop: 0 12px 28px -12px rgba(16, 22, 40, 0.22), 0 2px 6px -2px rgba(16, 22, 40, 0.08);
    --leaf-sh-win: 0 40px 80px -44px rgba(16, 22, 40, 0.32), 0 10px 24px -16px rgba(16, 22, 40, 0.14);
    --leaf-inverse: #1e2025;
    --leaf-on-inverse: #f1f2f5;

    /* The leaf: top-left and bottom-right wide, the other two nearly sharp. */
    --leaf-r-xs: 6px 2px 6px 2px;
    --leaf-r-ctl: 11px 3px 11px 3px;
    --leaf-r-ctl-hover: 14px 3px 14px 3px;
    --leaf-r-ctl-press: 8px 4px 8px 4px;
    --leaf-r-item: 12px 3px 12px 3px;
    --leaf-r-ovl: 16px 4px 16px 4px;
    --leaf-r-pane: 22px 6px 22px 6px;
    --leaf-r-win: 28px 8px 28px 8px;
    --leaf-r-seed: 5px 1px 5px 1px;

    --leaf-h: 34px;
    --leaf-h-menu: 34px;
    --leaf-h-row: 46px;

    --leaf-font-ui: "Geist", "Segoe UI Variable Text", "Segoe UI", "SF Pro Text", Cantarell, Roboto, sans-serif;
    --leaf-font-display: "Bricolage Grotesque", "Geist", "Segoe UI Variable Display", "Segoe UI", sans-serif;
    --leaf-font-mono: "Geist Mono", "Cascadia Code", "Cascadia Mono", "SF Mono", Menlo, Consolas, monospace;

    /* Surfaces derived from the seed: every application gets its own shade. */
    --leaf-tray: color-mix(in oklab, var(--leaf-primary) var(--leaf-tint), var(--leaf-n-tray));
    --leaf-pane: color-mix(in oklab, var(--leaf-primary) 1.5%, var(--leaf-n-pane));
    --leaf-s1: color-mix(in oklab, var(--leaf-primary) 4%, var(--leaf-n-s1));
    --leaf-s2: color-mix(in oklab, var(--leaf-primary) 6%, var(--leaf-n-s2));
    --leaf-s3: color-mix(in oklab, var(--leaf-primary) 9%, var(--leaf-n-s3));
    --leaf-hover: color-mix(in oklab, var(--leaf-primary) var(--leaf-mixh), transparent);
    --leaf-press: color-mix(in oklab, var(--leaf-primary) var(--leaf-mixp), transparent);
    --leaf-focus-ring: color-mix(in srgb, var(--leaf-primary) 18%, transparent);
    --leaf-glow: 0 8px 16px -10px color-mix(in srgb, var(--leaf-primary) 60%, transparent), 0 2px 4px -2px color-mix(in srgb, var(--leaf-primary) 35%, transparent);
    --leaf-glow-h: 0 10px 20px -10px color-mix(in srgb, var(--leaf-primary) 70%, transparent), 0 2px 5px -2px color-mix(in srgb, var(--leaf-primary) 40%, transparent);
    --leaf-pop: var(--leaf-pane);

    /* Basic's tokens, so every part NazorLeaf leaves to Basic is drawn in the NazorLeaf colors. */
    --nz-surface: var(--leaf-pane);
    --nz-on-surface: var(--leaf-text);
    --nz-control: var(--leaf-s2);
    --nz-on-control: var(--leaf-text);
    --nz-border: var(--leaf-stroke-2);
    --nz-field: var(--leaf-pane);
    --nz-on-field: var(--leaf-text);
    --nz-accent: var(--leaf-primary);
    --nz-on-accent: var(--leaf-on-primary);
    --nz-selection: var(--leaf-pc);
    --nz-on-selection: var(--leaf-on-pc);
    --nz-muted: var(--leaf-text-3);
    --nz-link: var(--leaf-ink);
    --nz-danger: var(--leaf-err);
    --nz-success: var(--leaf-ok);
    --nz-warning: var(--leaf-warn);
    --nz-scrim: color-mix(in srgb, var(--leaf-n-tray) 55%, transparent);

    color: var(--leaf-text);
    background: var(--leaf-pane);
    font-family: var(--leaf-font-ui);
    font-size: 14px;
    /* Text inherits the prototype's 1.5 line height (body { font: 400 14px/1.5 }), which sets the rhythm of labels and help text. */
    line-height: 1.5;
}

@media (forced-colors: none) {
    :root {
        scrollbar-width: 8px;
        scrollbar-thumb-width: 5px;
        scrollbar-thumb-expanded-width: 7px;
        scrollbar-radius: 4px;
        scrollbar-thumb-color: color-mix(in srgb, var(--leaf-text) 46%, transparent);
        scrollbar-thumb-hover-color: color-mix(in srgb, var(--leaf-text) 65%, transparent);
        scrollbar-thumb-active-color: var(--leaf-primary);
        scrollbar-track-hover-color: var(--leaf-hover);
    }
}

@media (prefers-color-scheme: dark) {
    :root {
        --leaf-primary: #a8c7fa;
        --leaf-on-primary: #062e6f;
        --leaf-pc: #0842a0;
        --leaf-on-pc: #d3e3fd;
        --leaf-ink: #a8c7fa;
        --leaf-n-tray: #0d0e11;
        --leaf-n-pane: #17181c;
        --leaf-n-s1: #1c1d22;
        --leaf-n-s2: #23252b;
        --leaf-n-s3: #2e3138;
        --leaf-text: #e8e9ed;
        --leaf-text-2: #b7bbc5;
        --leaf-text-3: #868b96;
        --leaf-stroke: rgba(255, 255, 255, 0.075);
        --leaf-stroke-2: rgba(255, 255, 255, 0.15);
        --leaf-hi: rgba(255, 255, 255, 0.07);
        --leaf-ctl: rgba(255, 255, 255, 0.045);
        --leaf-mixh: 10%;
        --leaf-mixp: 16%;
        --leaf-tint: 12%;
        --leaf-ok: #81c995;
        --leaf-ok-c: rgba(129, 201, 149, 0.14);
        --leaf-on-ok-c: #a8dab5;
        --leaf-warn: #fdd663;
        --leaf-warn-c: rgba(253, 214, 99, 0.13);
        --leaf-on-warn-c: #fde293;
        --leaf-err: #f2b8b5;
        --leaf-on-err: #601410;
        --leaf-err-c: rgba(242, 184, 181, 0.13);
        --leaf-on-err-c: #f9dedc;
        --leaf-sh-pane: 0 1px 2px rgba(0, 0, 0, 0.3);
        --leaf-sh-pop: 0 14px 30px -12px rgba(0, 0, 0, 0.55), 0 2px 6px -2px rgba(0, 0, 0, 0.3);
        --leaf-sh-win: 0 40px 80px -44px rgba(0, 0, 0, 0.7), 0 10px 24px -16px rgba(0, 0, 0, 0.4);
        --leaf-inverse: #e8e9ed;
        --leaf-on-inverse: #1e2025;
    }
}

@media (forced-colors: active) {
    :root {
        --leaf-primary: Highlight;
        --leaf-on-primary: HighlightText;
        --leaf-pc: Highlight;
        --leaf-on-pc: HighlightText;
        --leaf-ink: LinkText;
        --leaf-tray: Canvas;
        --leaf-pane: Canvas;
        --leaf-s1: Canvas;
        --leaf-s2: Canvas;
        --leaf-s3: ButtonFace;
        --leaf-text: CanvasText;
        --leaf-text-2: CanvasText;
        --leaf-text-3: GrayText;
        --leaf-stroke: CanvasText;
        --leaf-stroke-2: ButtonText;
        --leaf-ctl: ButtonFace;
        --leaf-hover: transparent;
        --leaf-press: transparent;
        --leaf-glow: 0 0 0 transparent;
        --leaf-glow-h: 0 0 0 transparent;
        --leaf-hi: transparent;
        --leaf-sh-pane: 0 0 0 transparent;
        --leaf-sh-pop: 0 0 0 transparent;
        --leaf-inverse: Canvas;
        --leaf-on-inverse: CanvasText;
        --nz-scrim: transparent;
    }
}

/* The window material: the tray tinted by the seed. */
:root[role=window] {
    background-color: var(--leaf-tray);
    background-image: none;
    background-size: 180px 180px;
    background-repeat: repeat;
    -windows-title-bar-background-color: var(--leaf-tray);
    -windows-title-bar-foreground-color: var(--leaf-text);
    /* The system caption buttons of Windows sit on the window material, as in the prototype. */
    -windows-title-bar-button-background-color: transparent;
    -windows-title-bar-button-inactive-background-color: transparent;
    -windows-title-bar-button-foreground-color: var(--leaf-text);
    -windows-title-bar-button-inactive-foreground-color: var(--leaf-text-3);
    -windows-title-bar-button-hover-background-color: color-mix(in srgb, var(--leaf-text) 7%, transparent);
    -windows-title-bar-button-hover-foreground-color: var(--leaf-text);
    -windows-title-bar-button-pressed-background-color: color-mix(in srgb, var(--leaf-text) 11%, transparent);
    -windows-title-bar-button-pressed-foreground-color: var(--leaf-text);
    /* The taskbar overlay is the 18 px badge leaf scaled to the 16 px overlay icon of Windows. */
    -windows-badge-background-color: var(--leaf-primary);
    -windows-badge-color: var(--leaf-on-primary);
    -windows-badge-border-radius: 8px 2.67px 8px 2.67px;
    -windows-badge-font-family: "Geist", "Segoe UI Variable Text", "Segoe UI";
    -windows-badge-font-size: 10px;
    -windows-badge-font-weight: 700;
}
:root[role=window][backdrop] { background-color: color-mix(in srgb, var(--leaf-tray) 68%, transparent); }
@media (forced-colors: active) { :root[role=window] { background-image: none; } }
@media (forced-colors: active) { :root[role=window][backdrop] { background-color: Canvas; } }
:focus-visible { outline: 2px solid var(--leaf-ink); outline-offset: 2px; }
:disabled { color: var(--leaf-text-3); }

/* Engine surfaces: built-in tooltip, text menu, message boxes. */
Tooltip, TooltipContainer {
    padding: 6px 10px; border: 0; border-radius: 10px 3px 10px 3px;
    background: var(--leaf-inverse); color: var(--leaf-on-inverse); font-size: 12px; box-shadow: var(--leaf-sh-pop);
    animation: leaf-tip 300ms cubic-bezier(0.16, 1, 0.3, 1) backwards;
}
ContextMenu, Menu, SelectPopup, ComboboxPopup, ComboBoxPopup, SearchFieldPopup, DatePickerPopup, TimePickerPopup, DateRangePickerPopup {
    display: flex; flex-direction: column; padding: 5px; border: 0; border-radius: var(--leaf-r-ovl);
    background: var(--leaf-pop); color: var(--leaf-text);
    box-shadow: var(--leaf-sh-pop), inset 0 0 0 1px var(--leaf-stroke-2), inset 0 1px 0 var(--leaf-hi);
    animation: leaf-unfurl 420ms cubic-bezier(0.16, 1, 0.3, 1) backwards;
}
/* Anchored surfaces open 6 px away from their anchor (.dd > .menu, .has-sub > .menu); the margin facing the anchor is the gap. */
Menu, SelectPopup, ComboboxPopup, ComboBoxPopup, SearchFieldPopup, DatePickerPopup, TimePickerPopup, DateRangePickerPopup { margin: 6px; }
ContextMenu > MenuItem { min-height: var(--leaf-h-menu); padding: 0 10px; border-radius: var(--leaf-r-item); font-size: 13.5px; }
ContextMenu > MenuItem:hover, ContextMenu > MenuItem:focus-visible { background: var(--leaf-hover); color: var(--leaf-text); }
ContextMenu > MenuItem:focus:not(:focus-visible):not(:hover) { background: transparent; }
MessageBoxBody { gap: 16px; padding: 26px 26px 20px; background: var(--leaf-pane); color: var(--leaf-text); }
MessageBoxButtons { gap: 8px; padding: 0 22px 22px; background: var(--leaf-pane); }

/* ---- Text ---- */
Heading { font-family: var(--leaf-font-display); font-weight: 800; letter-spacing: -0.02em; }
Heading[level="1"] { font-variation-settings: "opsz" 72; font-size: 34px; line-height: 1.1; font-weight: 800; letter-spacing: -0.035em; }
Heading[level="2"] { font-size: 26px; line-height: 1.2; font-weight: 750; letter-spacing: -0.025em; }
Heading[level="3"] { font-size: 20px; line-height: 1.3; font-weight: 700; letter-spacing: -0.015em; }
Heading[level="4"] { font-family: var(--leaf-font-ui); font-size: 17px; line-height: 1.35; font-weight: 650; letter-spacing: -0.005em; }
Heading[level="5"] { font-family: var(--leaf-font-ui); font-size: 15px; line-height: 1.4; font-weight: 650; letter-spacing: 0; }
Heading[level="6"] { font-family: var(--leaf-font-ui); font-size: 13px; line-height: 1.4; font-weight: 650; letter-spacing: 0; color: var(--leaf-text-2); }
ShortcutLabel { gap: 2px; color: var(--leaf-text-3); font-family: var(--leaf-font-mono); font-size: 11.5px; font-weight: 500; }
ShortcutLabelKey { padding: 0; }

/* ---- Layout ---- */
Separator { background: var(--leaf-stroke-2); }
Separator > SeparatorLabel { background: var(--leaf-pane); color: var(--leaf-text-3); font-size: 12px; font-weight: 550; }

/* ---- Actions: the leaf morphs on hover and press, the accent casts its glow ---- */
Button, ToggleButton, MenuButton {
    gap: 8px; min-height: var(--leaf-h); padding: 0 16px; border: 0; border-radius: var(--leaf-r-ctl);
    background: var(--leaf-ctl); color: var(--leaf-text); font-weight: 550; font-size: 14px;
    box-shadow: inset 0 0 0 1px var(--leaf-stroke-2), 0 1px 2px rgba(0, 0, 0, 0.05);
    transform: scale(1);
    transition: background-color 200ms, color 200ms, box-shadow 250ms cubic-bezier(0.2, 0, 0, 1), transform 200ms cubic-bezier(0.34, 1.5, 0.64, 1), border-radius 350ms cubic-bezier(0.34, 1.5, 0.64, 1);
}
Button:hover, ToggleButton:hover, MenuButton:hover { background: var(--leaf-hover); border-radius: var(--leaf-r-ctl-hover); }
Button:active, ToggleButton:active, MenuButton:active { transform: scale(0.965); border-radius: var(--leaf-r-ctl-press); background: var(--leaf-press); color: var(--leaf-text); }
Button[default], Button[appearance="accent"], ToggleButton[appearance="accent"], MenuButton[appearance="accent"] {
    background: var(--leaf-primary); color: var(--leaf-on-primary);
    box-shadow: var(--leaf-glow), inset 0 1px 0 rgba(255, 255, 255, 0.28), inset 0 -1px 0 rgba(0, 0, 0, 0.12);
}
Button[default]:hover, Button[appearance="accent"]:hover, ToggleButton[appearance="accent"]:hover, MenuButton[appearance="accent"]:hover {
    background: color-mix(in srgb, var(--leaf-primary) 92%, var(--leaf-on-primary)); color: var(--leaf-on-primary);
    box-shadow: var(--leaf-glow-h), inset 0 1px 0 rgba(255, 255, 255, 0.28);
}
Button[default]:active, Button[appearance="accent"]:active, ToggleButton[appearance="accent"]:active, MenuButton[appearance="accent"]:active {
    background: color-mix(in srgb, var(--leaf-primary) 84%, var(--leaf-on-primary)); color: var(--leaf-on-primary);
}
Button[appearance="tonal"], ToggleButton[appearance="tonal"], MenuButton[appearance="tonal"] { background: var(--leaf-pc); color: var(--leaf-on-pc); box-shadow: none; }
Button[appearance="tonal"]:hover, ToggleButton[appearance="tonal"]:hover, MenuButton[appearance="tonal"]:hover { background: color-mix(in oklab, var(--leaf-pc) 85%, var(--leaf-on-pc)); color: var(--leaf-on-pc); }
Button[appearance="tonal"]:active, ToggleButton[appearance="tonal"]:active, MenuButton[appearance="tonal"]:active { background: color-mix(in srgb, var(--leaf-pc) 78%, var(--leaf-on-pc)); color: var(--leaf-on-pc); }
/* Outline is the prototype's line button: the control fill with a stroke, the same as the standard button. */
Button[appearance="outline"], ToggleButton[appearance="outline"], MenuButton[appearance="outline"] { background: var(--leaf-ctl); box-shadow: inset 0 0 0 1px var(--leaf-stroke-2), 0 1px 2px rgba(0, 0, 0, 0.05); }
/* Subtle is the prototype's ghost button: no fill, accent text. */
Button[appearance="subtle"], ToggleButton[appearance="subtle"], MenuButton[appearance="subtle"] { background: transparent; color: var(--leaf-ink); box-shadow: none; padding: 0 12px; }
Button[appearance="subtle"]:hover, ToggleButton[appearance="subtle"]:hover, MenuButton[appearance="subtle"]:hover { background: var(--leaf-hover); }
Button[appearance="link"], ToggleButton[appearance="link"], MenuButton[appearance="link"] {
    min-height: 0; padding: 0; background: transparent; color: var(--leaf-ink); box-shadow: none; font-weight: 550;
    text-decoration-line: underline; text-decoration-thickness: 1.5px; text-decoration-color: color-mix(in srgb, var(--leaf-ink) 40%, transparent);
}
Button[appearance="link"]:hover, ToggleButton[appearance="link"]:hover, MenuButton[appearance="link"]:hover { background: transparent; text-decoration-color: var(--leaf-ink); border-radius: var(--leaf-r-ctl); }
Button[appearance="link"]:active, MenuButton[appearance="link"]:active { background: transparent; transform: scale(1); }
Button[intent="danger"] { color: var(--leaf-err); }
Button[intent="danger"][appearance="accent"], Button[intent="danger"][default] {
    background: var(--leaf-err); color: var(--leaf-on-err);
    box-shadow: 0 8px 16px -10px color-mix(in srgb, var(--leaf-err) 60%, transparent), inset 0 1px 0 rgba(255, 255, 255, 0.22);
}
Button[intent="success"][appearance="accent"] { background: var(--leaf-ok); color: var(--leaf-pane); box-shadow: 0 8px 16px -10px color-mix(in srgb, var(--leaf-ok) 60%, transparent); }
Button[intent="warning"][appearance="accent"] { background: var(--leaf-warn); color: var(--leaf-pane); box-shadow: 0 8px 16px -10px color-mix(in srgb, var(--leaf-warn) 60%, transparent); }
ToggleButton[pressed], ToggleButton[pressed]:hover { background: var(--leaf-pc); color: var(--leaf-on-pc); box-shadow: none; }
Button[size="small"], ToggleButton[size="small"], MenuButton[size="small"] { min-height: 28px; padding: 0 11px; font-size: 13px; }
Button[size="large"], ToggleButton[size="large"], MenuButton[size="large"] { min-height: 42px; padding: 0 22px; font-size: 15px; }
Button[shape="pill"], ToggleButton[shape="pill"], MenuButton[shape="pill"] { border-radius: 999px; }
Button[shape="circle"], ToggleButton[shape="circle"] { border-radius: 999px; padding: 0; width: var(--leaf-h); }
Button[shape="square"], ToggleButton[shape="square"] { padding: 0; width: var(--leaf-h); }
Button[icon-only], ToggleButton[icon-only], MenuButton[icon-only] { width: var(--leaf-h); padding: 0; background: transparent; color: var(--leaf-text-2); box-shadow: none; }
Button[icon-only]:hover, ToggleButton[icon-only]:hover, MenuButton[icon-only]:hover { background: var(--leaf-hover); color: var(--leaf-text); border-radius: var(--leaf-r-ctl); }
Button[icon-only]:active, ToggleButton[icon-only]:active { transform: scale(0.9); }
/* Icons: 18 px beside a label, 20 px alone, as the prototype's .btn .ic and .ibtn .ic. */
Icon { font-family: "NazorLeaf Icons"; line-height: 1; }
Button Icon, ToggleButton Icon, MenuButton Icon, SplitButton Icon { width: 18px; height: 18px; font-size: 18px; }
Button[icon-only] Icon, ToggleButton[icon-only] Icon, MenuButton[icon-only] Icon { width: 20px; height: 20px; font-size: 20px; }
ToggleButton[icon-only][pressed], ToggleButton[icon-only][pressed]:hover { background: transparent; color: var(--leaf-g3); }
Button:disabled, ToggleButton:disabled, MenuButton:disabled, Button[default]:disabled, Button[appearance]:disabled, ToggleButton[appearance]:disabled, MenuButton[appearance]:disabled {
    background: var(--leaf-s2); color: var(--leaf-text-3); box-shadow: none; transform: scale(1); border-radius: var(--leaf-r-ctl); cursor: default;
}
Button[appearance="link"]:disabled, Button[appearance="subtle"]:disabled { background: transparent; }
Button[busy], ToggleButton[busy] { color: var(--leaf-text-3); }
ButtonSpinner { stroke-width: 2px; }
MenuButton { padding: 0 12px 0 16px; }
MenuButton[expanded] { background: var(--leaf-press); border-radius: var(--leaf-r-ctl-hover); }
MenuButtonChevron { width: 12px; height: 12px; color: var(--leaf-text-2); stroke-width: 1.7px; transform: rotate(0deg); transition: transform 350ms cubic-bezier(0.34, 1.5, 0.64, 1); }
MenuButton[expanded] > MenuButtonChevron { transform: rotate(180deg); }
MenuButton[appearance="accent"] > MenuButtonChevron { color: inherit; }

/* ---- Selection: leaf check box ---- */
CheckBox { column-gap: 11px; line-height: 1.35; cursor: default; }
CheckBoxMark {
    width: 20px; height: 20px; border: 1.6px solid var(--leaf-text-3); border-radius: 8px 2px 8px 2px;
    background: var(--leaf-ctl); color: var(--leaf-on-primary); stroke-width: 2.4px;
    transform: scale(1);
    transition: background-color 200ms, border-color 200ms, box-shadow 200ms, transform 250ms cubic-bezier(0.34, 1.5, 0.64, 1);
}
CheckBoxCheck, CheckBoxMixed { transition: stroke-trim-end 280ms cubic-bezier(0.2, 0, 0, 1) 50ms; }
CheckBox:disabled > CheckBoxMark > CheckBoxCheck, CheckBox:disabled > CheckBoxMark > CheckBoxMixed { transition: none; }
CheckBox:hover > CheckBoxMark { border-color: var(--leaf-text-2); box-shadow: 0 0 0 5px var(--leaf-hover); }
CheckBox:active > CheckBoxMark { transform: scale(0.85); }
CheckBox:checked > CheckBoxMark, CheckBox[indeterminate] > CheckBoxMark { background: var(--leaf-primary); border-color: var(--leaf-primary); color: var(--leaf-on-primary); }
/* Disabled: the whole row fades, the mark keeps its colors (.check.dis). */
CheckBox:disabled { opacity: 0.5; color: var(--leaf-text); }
CheckBox:disabled > CheckBoxMark { box-shadow: none; transform: scale(1); }
CheckBoxDescription { color: var(--leaf-text-3); font-size: 12.5px; }
CheckBox[size="small"] > CheckBoxMark { width: 16px; height: 16px; border-radius: 6px 2px 6px 2px; }
CheckBox[size="large"] > CheckBoxMark { width: 24px; height: 24px; border-radius: 10px 3px 10px 3px; }

/* ---- Fields: white leaf with a stroke, focus widens the leaf and adds a soft ring ---- */
FormLayout { gap: 14px; }
Field { gap: 6px; }
FieldDescription { color: var(--leaf-text-3); font-size: 12.5px; }
FieldError { position: relative; padding-left: 20px; color: var(--leaf-err); font-size: 12.5px; }
/* The error starts with the 14 px Error icon (.field.err .help .ic), centered on its first line. */
FieldError::before {
    content: "\E00B"; position: absolute; left: 0; top: 2px; width: 14px; height: 14px; display: flex; align-items: center; justify-content: center;
    font-family: "NazorLeaf Icons"; font-size: 14px; line-height: 1;
}
Label { font-size: 13px; font-weight: 550; }
/* Placeholders use the tertiary text, as the prototype's empty search field. */
::placeholder { color: var(--leaf-text-3); opacity: 1; }
LabelRequiredMark { color: var(--leaf-err); }
TextField, TextArea, Select, Combobox, PasswordField, NumberField, SearchField, MaskedField, FileField, ShortcutField, ComboBox, TokenField,
DatePicker, TimePicker, DateRangePicker, DateTimePicker {
    gap: 8px; min-height: var(--leaf-h); padding: 0 11px; border: 0; border-radius: var(--leaf-r-ctl);
    background: var(--leaf-pane); color: var(--leaf-text);
    box-shadow: inset 0 0 0 1px var(--leaf-stroke-2);
    transition: box-shadow 250ms cubic-bezier(0.2, 0, 0, 1), border-radius 350ms cubic-bezier(0.34, 1.5, 0.64, 1), background-color 200ms;
}
TextField:hover, TextArea:hover, Combobox:hover, PasswordField:hover, NumberField:hover, SearchField:hover, MaskedField:hover, FileField:hover, ShortcutField:hover, ComboBox:hover, TokenField:hover,
DatePicker:hover, TimePicker:hover, DateRangePicker:hover {
    box-shadow: inset 0 0 0 1px color-mix(in srgb, var(--leaf-text) 30%, transparent);
}
TextField:focus-within, TextArea:focus-within, Combobox:focus-within, PasswordField:focus-within, NumberField:focus-within, SearchField:focus-within, MaskedField:focus-within,
FileField:focus-within, ShortcutField:focus-within, ComboBox:focus-within, TokenField:focus-within, DatePicker:focus-within, TimePicker:focus-within, DateRangePicker:focus-within, DateTimePicker:focus-within,
DatePicker[expanded], TimePicker[expanded], DateRangePicker[expanded] {
    box-shadow: inset 0 0 0 1.6px var(--leaf-ink), 0 0 0 4px var(--leaf-focus-ring); border-radius: var(--leaf-r-ctl-hover);
}
TextField[invalid], TextArea[invalid], Select[invalid], PasswordField[invalid], NumberField[invalid], SearchField[invalid], MaskedField[invalid],
DatePicker[invalid], TimePicker[invalid], DateRangePicker[invalid], DateTimePicker[invalid] {
    box-shadow: inset 0 0 0 1.6px var(--leaf-err), 0 0 0 4px color-mix(in srgb, var(--leaf-err) 14%, transparent);
}
TextField:disabled, TextArea:disabled, Select:disabled, DatePicker:disabled, TimePicker:disabled, DateRangePicker:disabled, DateTimePicker:disabled {
    background: var(--leaf-s2); color: var(--leaf-text-3); box-shadow: none;
}
TextField[appearance="filled"], TextArea[appearance="filled"], Select[appearance="filled"], DatePicker[appearance="filled"], TimePicker[appearance="filled"], DateRangePicker[appearance="filled"], DateTimePicker[appearance="filled"] {
    background: var(--leaf-s2); box-shadow: none;
}
TextField[appearance="underline"], TextArea[appearance="underline"], Select[appearance="underline"], DatePicker[appearance="underline"], TimePicker[appearance="underline"], DateRangePicker[appearance="underline"], DateTimePicker[appearance="underline"] {
    background: transparent; border-radius: 0; box-shadow: inset 0 -1px 0 var(--leaf-stroke-2);
}
TextField[appearance="plain"], TextArea[appearance="plain"], Select[appearance="plain"], DatePicker[appearance="plain"], TimePicker[appearance="plain"], DateRangePicker[appearance="plain"], DateTimePicker[appearance="plain"] {
    background: transparent; box-shadow: none; padding: 0;
}
TextField[size="small"], Select[size="small"], DatePicker[size="small"], TimePicker[size="small"], DateRangePicker[size="small"], DateTimePicker[size="small"] { min-height: 28px; font-size: 13px; }
TextField[size="large"], Select[size="large"], DatePicker[size="large"], TimePicker[size="large"], DateRangePicker[size="large"], DateTimePicker[size="large"] { min-height: 42px; font-size: 15px; }
TextFieldPrefix, TextFieldSuffix, TextFieldCounter, TextAreaCounter, NumberFieldUnit, MaskedFieldPrompt, ShortcutFieldPrompt { color: var(--leaf-text-3); }
@media (forced-colors: none) {
    TextField[appearance="outline"]:focus-within, TextArea[appearance="outline"]:focus-within, PasswordField[appearance="outline"]:focus-within,
    NumberField[appearance="outline"]:focus-within, SearchField[appearance="outline"]:focus-within, MaskedField[appearance="outline"]:focus-within,
    ComboBox[appearance="outline"]:focus-within, TokenField[appearance="outline"]:focus-within, Combobox:focus-within,
    DatePicker[appearance="outline"]:focus-within, TimePicker[appearance="outline"]:focus-within,
    DateTimePicker[appearance="outline"]:focus-within { outline-style: none; }
}
TextFieldLeading, TextFieldTrailing, TextAreaLeading, TextAreaTrailing, SelectLeading { color: var(--leaf-text-3); }
TextFieldClear, PasswordFieldReveal, SearchFieldClear, FileFieldClear, ShortcutFieldClear, DatePickerClear, TimePickerClear, DateRangePickerClear {
    position: relative; flex-shrink: 0; width: 28px; height: 28px; margin-right: -6px; border-radius: var(--leaf-r-ctl);
    color: var(--leaf-text-2); stroke-trim-end: 0; transform: scale(1); cursor: default;
    transition: background-color 150ms, color 150ms, transform 250ms cubic-bezier(0.34, 1.5, 0.64, 1);
}
TextFieldClear::before, PasswordFieldReveal::before, SearchFieldClear::before, FileFieldClear::before, ShortcutFieldClear::before,
DatePickerClear::before, TimePickerClear::before, DateRangePickerClear::before, SearchFieldMark::before, NumberFieldDecrement::before, NumberFieldIncrement::before {
    content: ""; position: absolute; left: 0; top: 0; width: 100%; height: 100%; display: flex; align-items: center; justify-content: center;
    font-family: "NazorLeaf Icons"; font-size: 16px; font-weight: 400; line-height: 1;
}
TextFieldClear::before, SearchFieldClear::before, FileFieldClear::before, ShortcutFieldClear::before, DatePickerClear::before, TimePickerClear::before, DateRangePickerClear::before { content: "\E005"; }
PasswordFieldReveal::before { content: "\E00C"; }
TextFieldClear:hover, PasswordFieldReveal:hover, SearchFieldClear:hover, FileFieldClear:hover, ShortcutFieldClear:hover, DatePickerClear:hover, TimePickerClear:hover, DateRangePickerClear:hover {
    background: var(--leaf-hover); color: var(--leaf-text);
}
TextFieldClear:active, PasswordFieldReveal:active, SearchFieldClear:active, FileFieldClear:active, ShortcutFieldClear:active, DatePickerClear:active, TimePickerClear:active, DateRangePickerClear:active { transform: scale(0.9); }
/* Shown text: the eye takes the --leaf-g3 tone and pops in (.ibtn[aria-pressed="true"]). */
PasswordFieldReveal[pressed], PasswordFieldReveal[pressed]:hover { color: var(--leaf-g3); }
PasswordFieldReveal[pressed]::before { animation: leaf-icon-pop 450ms cubic-bezier(0.34, 1.5, 0.64, 1); }
PasswordFieldReveal:disabled { background: transparent; color: var(--leaf-text-3); }
/* The leading search icon of a search field (.input > .ic). */
SearchFieldMark { position: relative; flex-shrink: 0; width: 16px; height: 16px; padding: 0; color: var(--leaf-text-3); stroke-trim-end: 0; }
SearchFieldMark::before { content: "\E022"; }
SearchFieldMark::after { content: none; }
FlipViewPrevious, FlipViewNext { border-radius: var(--leaf-r-ctl); border: 0; background: var(--leaf-pane); color: var(--leaf-text-2); box-shadow: inset 0 0 0 1px var(--leaf-stroke); }
FlipViewPrevious:hover, FlipViewNext:hover { background: var(--leaf-hover); color: var(--leaf-text); }
SearchFieldScope { flex-shrink: 0; }
TextArea { padding: 9px 11px; line-height: 1.5; min-height: calc(var(--nz-textarea-rows, 3) * 1.5em + 20px); }
NumberFieldInput { font-family: var(--leaf-font-mono); }
/* Spin buttons: icon leaves inside the field. On the sides they are the prototype's stepper (.stepper: minus and plus
   icons around the centered value); stacked, they are two short leaves holding the engine chevrons. */
NumberFieldDecrement, NumberFieldIncrement {
    position: relative; display: flex; align-items: center; justify-content: center; flex-shrink: 0; border: 0;
    border-radius: var(--leaf-r-ctl); color: var(--leaf-text-2); stroke-width: 1.7px; transform: scale(1); cursor: default;
    transition: background-color 150ms, color 150ms, transform 250ms cubic-bezier(0.34, 1.5, 0.64, 1);
}
NumberFieldDecrement:hover, NumberFieldIncrement:hover { background: var(--leaf-hover); color: var(--leaf-text); }
NumberFieldDecrement:active, NumberFieldIncrement:active { transform: scale(0.9); }
NumberFieldDecrement:disabled, NumberFieldIncrement:disabled { color: var(--leaf-text-3); background: transparent; transform: scale(1); }
NumberField[spin-buttons="inline"] > NumberFieldDecrement, NumberField[spin-buttons="inline"] > NumberFieldIncrement { width: 28px; height: 28px; padding: 0; font-size: 0; stroke-trim-end: 0; }
NumberField > NumberFieldDecrement { margin-left: -6px; }
NumberField > NumberFieldIncrement { margin-right: -6px; }
NumberField > NumberFieldDecrement::before { content: "\E018"; }
NumberField > NumberFieldIncrement::before { content: "\E01F"; }
NumberFieldDecrement + NumberFieldInput { text-align: center; }
NumberFieldSpinButtons { display: flex; flex-direction: column; justify-content: center; gap: 1px; margin-right: -7px; }
NumberFieldSpinButtons > NumberFieldIncrement, NumberFieldSpinButtons > NumberFieldDecrement { width: 24px; height: 15px; min-height: 0; padding: 0; border-radius: var(--leaf-r-xs); }
NumberFieldSpinButtons > NumberFieldIncrement::before, NumberFieldSpinButtons > NumberFieldDecrement::before { content: none; }
/* PIN cells: one field leaf per character, mono and centered; the focused cell widens its leaf and takes the ring. */
PinField { gap: 8px; }
PinFieldCell {
    width: 38px; min-height: var(--leaf-h); padding: 0; border: 0; border-radius: var(--leaf-r-ctl); background: var(--leaf-pane); color: var(--leaf-text);
    line-height: max(var(--leaf-h), 1.5em);
    box-shadow: inset 0 0 0 1px var(--leaf-stroke-2); font-family: var(--leaf-font-mono); font-size: 16px; text-align: center;
    transition: box-shadow 250ms cubic-bezier(0.2, 0, 0, 1), border-radius 350ms cubic-bezier(0.34, 1.5, 0.64, 1);
}
PinFieldCell:hover { box-shadow: inset 0 0 0 1px color-mix(in srgb, var(--leaf-text) 30%, transparent); }
PinFieldCell:focus { box-shadow: inset 0 0 0 1.6px var(--leaf-ink), 0 0 0 4px var(--leaf-focus-ring); border-radius: var(--leaf-r-ctl-hover); outline-style: none; }
PinFieldCell[invalid] { box-shadow: inset 0 0 0 1.6px var(--leaf-err), 0 0 0 4px color-mix(in srgb, var(--leaf-err) 14%, transparent); }
PinFieldCell:disabled { background: var(--leaf-s2); color: var(--leaf-text-3); box-shadow: none; }
PinFieldSeparator { color: var(--leaf-text-3); }
/* Files: the name fills the field, Browse is a small tonal leaf at its end. */
FileFieldValue { flex: 1; min-width: 0; overflow-x: hidden; white-space: nowrap; text-overflow: ellipsis; }
FileFieldValue[placeholder] { color: var(--leaf-text-3); }
FileFieldBrowse {
    display: flex; align-items: center; flex-shrink: 0; height: 26px; margin-right: -6px; padding: 0 11px; border-radius: 9px 3px 9px 3px;
    background: var(--leaf-pc); color: var(--leaf-on-pc); font-size: 13px; font-weight: 550; transform: scale(1); cursor: default;
    transition: background-color 150ms, transform 200ms cubic-bezier(0.34, 1.5, 0.64, 1), border-radius 350ms cubic-bezier(0.34, 1.5, 0.64, 1);
}
FileFieldBrowse:hover { background: color-mix(in oklab, var(--leaf-pc) 85%, var(--leaf-on-pc)); border-radius: 3px 9px 3px 9px; }
FileFieldBrowse:active { transform: scale(0.95); background: color-mix(in srgb, var(--leaf-pc) 78%, var(--leaf-on-pc)); }
FileFieldBrowse:disabled { background: var(--leaf-s2); color: var(--leaf-text-3); transform: scale(1); }
FileFieldBrowse[busy] { color: var(--leaf-text-3); }
FileFieldClear { margin-right: 0; }
/* Tokens: tonal leaves, as the selected filter chips; the remove button is a small close icon. */
TokenField { flex-wrap: wrap; row-gap: 4px; padding-top: 3px; padding-bottom: 3px; }
TokenFieldTokens { gap: 6px; }
TokenFieldToken { gap: 2px; min-height: 24px; padding: 0 3px 0 9px; border-radius: 10px 3px 10px 3px; background: var(--leaf-pc); color: var(--leaf-on-pc); font-size: 13px; font-weight: 550; }
TokenFieldTokenRemove {
    position: relative; flex-shrink: 0; width: 18px; height: 18px; border-radius: 7px 2px 7px 2px; color: var(--leaf-on-pc); stroke-trim-end: 0; cursor: default;
    transition: background-color 150ms;
}
TokenFieldTokenRemove::before {
    content: "\E005"; position: absolute; left: 0; top: 0; width: 100%; height: 100%; display: flex; align-items: center; justify-content: center;
    font-family: "NazorLeaf Icons"; font-size: 12px; line-height: 1;
}
TokenFieldTokenRemove:hover { background: color-mix(in srgb, var(--leaf-on-pc) 12%, transparent); }
TokenFieldInput { min-width: 80px; }

/* Select: the drop-down button keeps its leaf; open, the leaf widens and the chevron turns. */
Select { padding: 0 10px 0 12px; box-shadow: inset 0 0 0 1px var(--leaf-stroke-2), 0 1px 2px rgba(0, 0, 0, 0.04); }
Select:hover { background: color-mix(in srgb, var(--leaf-primary) var(--leaf-mixh), var(--leaf-pane)); }
Select:focus-visible, Select[expanded] { box-shadow: inset 0 0 0 1.6px var(--leaf-ink); border-radius: var(--leaf-r-ctl-hover); outline-style: none; }
SelectValue[placeholder] { color: var(--leaf-text-3); }
SelectChevron, ComboBoxChevron { width: 14px; height: 14px; color: var(--leaf-text-2); stroke-width: 1.7px; transform: rotate(0deg); transition: transform 350ms cubic-bezier(0.34, 1.5, 0.64, 1); }
Select[expanded] > SelectChevron, ComboBox[expanded] ComboBoxChevron { transform: rotate(180deg); }
SelectPopup, ComboboxPopup, SearchFieldPopup { min-width: 224px; }
ComboBoxEmpty, SearchFieldEmpty { min-height: var(--leaf-h-menu); display: flex; align-items: center; padding: 0 10px; color: var(--leaf-text-3); font-size: 13.5px; }
SelectGroupHeader, MenuSectionHeader, ListViewSectionHeader, CommandPaletteGroupHeader { padding: 8px 10px 5px; color: var(--leaf-text-3); font-size: 12px; font-weight: 550; }

/* Options and menu items: leaf rows that light up under the pointer; the chosen one carries the seed. */
SelectOption, ComboboxOption, ComboBoxOption, SearchFieldOption, MenuItem, MenuCheckItem, MenuRadioItem, Submenu, ListboxOption, TimePickerOption, DateRangePickerPreset {
    min-height: var(--leaf-h-menu); padding: 0 10px; border-radius: var(--leaf-r-item); font-size: 13.5px;
    transition: background-color 150ms;
}
/* The pointer and the keyboard light a row; a row focused by opening the menu with the pointer stays plain (.mi:focus-visible). */
SelectOption:focus, MenuItem:focus, MenuCheckItem:focus, MenuRadioItem:focus, Submenu:focus { background: transparent; color: var(--leaf-text); }
SelectOption:hover, SelectOption:focus-visible, ComboboxOption[current], ComboboxOption:hover, ComboBoxOption[current], ComboBoxOption:hover,
SearchFieldOption[current], SearchFieldOption:hover, MenuItem:hover, MenuItem:focus-visible, MenuCheckItem:hover, MenuCheckItem:focus-visible,
MenuRadioItem:hover, MenuRadioItem:focus-visible, Submenu:hover, Submenu:focus-visible, Submenu[expanded], TimePickerOption:hover, DateRangePickerPreset:hover {
    background: var(--leaf-hover); color: var(--leaf-text);
}
SelectOption:active, MenuItem:active, MenuCheckItem:active, MenuRadioItem:active { background: var(--leaf-press); }
SelectOption:focus-visible, MenuItem:focus-visible, MenuCheckItem:focus-visible, MenuRadioItem:focus-visible, Submenu:focus-visible, ListboxOption:focus-visible { outline-offset: -2px; }
SelectOption[selected], ListboxOption[selected], TimePickerOption[selected], DateRangePickerPreset[selected] {
    position: relative; background-color: transparent; color: var(--leaf-text); font-weight: 600;
}
/* The seed of the chosen row grows and turns into place, as .mi.sel::before. */
SelectOption[selected]::before, ListboxOption[selected]::before, TimePickerOption[selected]::before, DateRangePickerPreset[selected]::before {
    content: ""; position: absolute; left: -1px; top: 50%; width: 6px; height: 12px; margin-top: -6px;
    border-radius: var(--leaf-r-seed); background: var(--leaf-primary);
    animation: leaf-seed-in 400ms cubic-bezier(0.34, 1.5, 0.64, 1);
}
SelectOption[selected]:hover, SelectOption[selected]:focus-visible, ListboxOption[selected]:hover, TimePickerOption[selected]:hover { background-color: var(--leaf-hover); }
SelectOption:disabled, MenuItem:disabled, MenuCheckItem:disabled, MenuRadioItem:disabled, Submenu:disabled, ListboxOption:disabled, TimePickerOption:disabled {
    background: transparent; color: var(--leaf-text-3);
}
/* A chosen option shows only the seed (.mi.sel); menu check items keep their check. */
SelectOptionCheck { display: none; }
MenuItemCheck { width: 14px; height: 14px; color: var(--leaf-ink); stroke-width: 2px; }
MenuItem, MenuCheckItem, MenuRadioItem, Submenu { grid-template-columns: 20px minmax(0px, 1fr) max-content max-content; column-gap: 10px; }
MenuItemLeading { color: var(--leaf-text-2); }
MenuItemDescription { color: var(--leaf-text-3); font-size: 12px; }
MenuItem > ShortcutLabel, MenuCheckItem > ShortcutLabel, MenuRadioItem > ShortcutLabel { color: var(--leaf-text-3); }
MenuItem[intent="danger"], MenuItem[intent="danger"] > MenuItemLeading { color: var(--leaf-err); }
MenuItemRadio { width: 6px; height: 12px; border-radius: var(--leaf-r-seed); }
MenuItemRadio[checked] { background: var(--leaf-primary); }
MenuItemSubmenuArrow { width: 12px; height: 12px; color: var(--leaf-text-3); stroke-width: 1.7px; }
MenuSeparator { height: 1px; margin: 5px 8px; background: var(--leaf-stroke-2); }
Menu { min-width: 224px; }

/* ---- Primitives ---- */
PopoverContainer {
    padding: 8px; border: 0; border-radius: var(--leaf-r-ovl); background: var(--leaf-pop); color: var(--leaf-text);
    box-shadow: var(--leaf-sh-pop), inset 0 0 0 1px var(--leaf-stroke-2), inset 0 1px 0 var(--leaf-hi);
}
DialogContainer {
    padding: 0; border: 0; border-radius: var(--leaf-r-win); background: var(--leaf-pane); color: var(--leaf-text);
    box-shadow: var(--leaf-sh-pop), inset 0 0 0 1px var(--leaf-stroke-2);
    animation: leaf-dialog 550ms cubic-bezier(0.16, 1, 0.3, 1) backwards;
}
DialogContainerScrim, DialogScrim, AlertDialogScrim, DrawerScrim, CommandPaletteScrim { background: var(--nz-scrim); animation: leaf-fade 300ms backwards; }
Indicator { border-radius: var(--leaf-r-item); background: var(--leaf-hover); transition: left 320ms cubic-bezier(0.16, 1, 0.3, 1), top 320ms cubic-bezier(0.16, 1, 0.3, 1), width 320ms cubic-bezier(0.16, 1, 0.3, 1), height 320ms cubic-bezier(0.16, 1, 0.3, 1); }
ResizeHandle:hover, ResizeHandle[dragging] { background: var(--leaf-hover); }
Listbox { padding: 5px; border: 0; border-radius: var(--leaf-r-ovl); background: var(--leaf-s1); box-shadow: inset 0 0 0 1px var(--leaf-stroke); }
Combobox { padding: 0 11px; }
ComboboxEmpty, ListViewEmpty, TreeViewEmpty, GridViewEmpty, CommandPaletteSearchEmpty, DataGridEmpty { color: var(--leaf-text-3); }

/* ==== Group sections ==== */

/* ---- Group: Text and layout (text-layout) ---- */
Paragraph { line-height: 1.7; }
Kbd {
    padding: 4px 7px 5px; border: 0; border-radius: 7px 2px 7px 2px; background: var(--leaf-pane); color: var(--leaf-text);
    box-shadow: inset 0 0 0 1px var(--leaf-stroke-2), inset 0 -2px 0 var(--leaf-stroke-2);
    font-family: var(--leaf-font-ui); font-size: 0.78em; font-weight: 550;
}
Blockquote {
    position: relative; gap: 6px; padding: 16px 20px 16px 52px; border: 0; border-radius: var(--leaf-r-ovl); background-color: var(--leaf-s1);
    font-family: var(--leaf-font-display); font-size: 18px; font-weight: 600; line-height: 1.45; letter-spacing: -0.01em;
}
/* The quotation mark is a tall seed. */
Blockquote::before { content: ""; position: absolute; left: 20px; top: 20px; width: 14px; height: 20px; border-radius: 9px 2px 9px 2px; background: var(--leaf-primary); }
BlockquoteCite { color: var(--leaf-text-3); font-family: var(--leaf-font-ui); font-size: 12.5px; font-weight: 400; letter-spacing: 0; }
/* Vertical rhythm of running text, as the prototype's .prose: headings open 1.5em above and close 0.45em below. */
Paragraph + Paragraph { margin-top: 1em; }
Paragraph + Heading, List + Heading, Blockquote + Heading, DefinitionList + Heading, CodeBlock + Heading, Figure + Heading, Heading + Heading { margin-top: 1.5em; }
Heading + Paragraph, Heading + List, Heading + DefinitionList, Heading + CodeBlock, Heading + Figure, Heading + Blockquote { margin-top: 0.45em; }
Paragraph + Blockquote, List + Blockquote, Blockquote + Paragraph, Blockquote + Heading, Blockquote + List { margin-top: 1.6em; }
List { gap: 0.35em; }
/* Unordered lists mark each item with a seed instead of a bullet. */
ListItemMarker { color: var(--leaf-ink); font-family: var(--leaf-font-display); font-weight: 700; }
List > ListItem > ListItemMarker { align-self: flex-start; width: 6px; min-width: 6px; height: 9px; margin-top: 0.5em; border-radius: 4px 1px 4px 1px; background: var(--leaf-primary); color: transparent; }
List[ordered] > ListItem > ListItemMarker { width: auto; min-width: 0; height: auto; margin-top: 0; border-radius: 0; background: transparent; color: var(--leaf-ink); font-size: 0.95em; }
DefinitionTerm { font-weight: 650; }
DefinitionDescription { color: var(--leaf-text-2); }
DefinitionList[layout="inline"] { column-gap: 20px; row-gap: 8px; }
CodeBlock {
    border: 0; border-radius: var(--leaf-r-ovl); background: var(--leaf-s1); color: var(--leaf-text);
    box-shadow: inset 0 0 0 1px var(--leaf-stroke); font-family: var(--leaf-font-mono); font-size: 13px; line-height: 1.65;
}
CodeBlockLineNumbers { padding: 16px 10px 16px 14px; border-right: 1px solid var(--leaf-stroke); color: var(--leaf-text-3); }
CodeBlockCode { padding: 16px 18px; }
FigureCaption { color: var(--leaf-text-3); font-size: 12.5px; }

ScrollView { border: 0; }
ScrollView { scroll-behavior: smooth; scroll-duration: 120ms; scroll-timing-function: cubic-bezier(0.2, 0.65, 0.35, 1); }
ScrollView[can-scroll-top] { box-shadow: inset 0 1px 0 var(--leaf-stroke); }
Card {
    border: 0; border-radius: var(--leaf-r-ovl); background: var(--leaf-s1); color: var(--leaf-text);
    transition: background-color 200ms, box-shadow 200ms, transform 400ms cubic-bezier(0.34, 1.5, 0.64, 1);
    /* A slider thumb cuts its gap in the card's own color (.card .sl). */
}
Card[appearance="outline"] { background: var(--leaf-pane); box-shadow: inset 0 0 0 1px var(--leaf-stroke); }
Card[appearance="subtle"] { background: transparent; }
Card[clickable]:hover, Card[appearance="subtle"]:hover { background: color-mix(in srgb, var(--leaf-primary) var(--leaf-mixh), var(--leaf-s1)); }
Card[clickable]:active { background: color-mix(in srgb, var(--leaf-primary) var(--leaf-mixp), var(--leaf-s1)); }
Card[selected] { background: color-mix(in srgb, var(--leaf-primary) 12%, var(--leaf-pane)); color: var(--leaf-text); box-shadow: inset 0 0 0 1.5px color-mix(in srgb, var(--leaf-primary) 45%, transparent); }
Card:disabled { color: var(--leaf-text-3); }
CardHeader { padding: 18px 20px 0; column-gap: 12px; }
CardTitle { font-size: 15px; font-weight: 650; }
CardSubtitle { color: var(--leaf-text-3); font-size: 12.5px; }
CardBody { padding: 14px 20px; }
CardFooter { gap: 8px; padding: 0 20px 18px; }
GroupBox { padding: 12px 16px 16px; border: 0; border-radius: var(--leaf-r-ovl); background: var(--leaf-s1); }
GroupBoxHeader { padding: 0 0 10px; font-size: 13px; font-weight: 650; }
GroupBoxToggle:hover { color: var(--leaf-ink); }

/* Expander and accordion: leaf panels whose chevron sits in a small pane-colored leaf and turns with a spring. */
Expander, AccordionItem { border: 0; border-radius: var(--leaf-r-ovl); background: var(--leaf-s1); overflow-x: hidden; overflow-y: hidden; }
Accordion { gap: 6px; }
AccordionItem + AccordionItem { border-top-width: 0; }
ExpanderHeader, AccordionItemHeader {
    gap: 12px; min-height: 58px; padding: 10px 16px; border-radius: var(--leaf-r-ovl); background: transparent; color: var(--leaf-text);
    transition: background-color 200ms;
}
ExpanderHeader:hover, AccordionItemHeader:hover { background: var(--leaf-hover); color: var(--leaf-text); }
ExpanderHeader:active, AccordionItemHeader:active { background: var(--leaf-press); color: var(--leaf-text); }
ExpanderTitle, AccordionItemTitle { font-weight: 550; }
ExpanderSubtitle, AccordionItemSubtitle { color: var(--leaf-text-3); font-size: 12.5px; }
ExpanderChevron, AccordionItemChevron {
    width: 26px; height: 26px; padding: 6px; border-radius: 10px 3px 10px 3px; background: var(--leaf-pane); color: var(--leaf-text-2); stroke-width: 7.0833333333%;
    transform: rotate(0deg); transition: transform 450ms cubic-bezier(0.34, 1.5, 0.64, 1);
}
Expander[expanded] > ExpanderHeader > ExpanderChevron, AccordionItem[expanded] > AccordionItemHeader > AccordionItemChevron { transform: rotate(180deg); }
ExpanderPanelContent, AccordionItemPanelContent { padding: 2px 18px 16px 18px; border-top: 0; color: var(--leaf-text-2); animation: leaf-rise 400ms cubic-bezier(0.16, 1, 0.3, 1) backwards; }

SplitViewPane { background: transparent; }
SplitView[orientation="horizontal"][placement="start"] > SplitViewPane, SplitView[orientation="horizontal"][placement="end"] > SplitViewPane,
SplitView[orientation="vertical"][placement="start"] > SplitViewPane, SplitView[orientation="vertical"][placement="end"] > SplitViewPane { border-color: var(--leaf-stroke); }
SplitView[display-mode="overlay"] > SplitViewPane, SplitView[display-mode="compact"] > SplitViewPane { background: var(--leaf-pane); box-shadow: var(--leaf-sh-pop); }
StatusBar { min-height: 26px; border-top: 1px solid var(--leaf-stroke); background: transparent; color: var(--leaf-text-2); font-size: 12px; }
StatusBarSection + StatusBarSection { border-left: 1px solid var(--leaf-stroke); }

/* ---- Group: Feedback (feedback) ---- */
ProgressBar, Spinner, Tag, BadgeIndicator { --nz-feedback-color: var(--leaf-primary); --nz-on-feedback: var(--leaf-on-primary); --leaf-feedback-container: var(--leaf-pc); --leaf-on-feedback-container: var(--leaf-on-pc); }
ProgressBar[intent="neutral"], Spinner[intent="neutral"], Tag[intent="neutral"], BadgeIndicator[intent="neutral"] { --nz-feedback-color: var(--leaf-text-3); --nz-on-feedback: var(--leaf-pane); --leaf-feedback-container: var(--leaf-s2); --leaf-on-feedback-container: var(--leaf-text-2); }
ProgressBar[intent="success"], Spinner[intent="success"], Tag[intent="success"], BadgeIndicator[intent="success"] { --nz-feedback-color: var(--leaf-ok); --nz-on-feedback: var(--leaf-pane); --leaf-feedback-container: var(--leaf-ok-c); --leaf-on-feedback-container: var(--leaf-on-ok-c); }
ProgressBar[intent="warning"], Spinner[intent="warning"], Tag[intent="warning"], BadgeIndicator[intent="warning"] { --nz-feedback-color: var(--leaf-warn); --nz-on-feedback: var(--leaf-pane); --leaf-feedback-container: var(--leaf-warn-c); --leaf-on-feedback-container: var(--leaf-on-warn-c); }
ProgressBar[intent="danger"], Spinner[intent="danger"], Tag[intent="danger"], BadgeIndicator[intent="danger"] { --nz-feedback-color: var(--leaf-err); --nz-on-feedback: var(--leaf-on-err); --leaf-feedback-container: var(--leaf-err-c); --leaf-on-feedback-container: var(--leaf-on-err-c); }
Meter { --nz-feedback-color: var(--leaf-ok); }
Meter[quality="suboptimal"] { --nz-feedback-color: var(--leaf-g3); }
Meter[quality="worst"] { --nz-feedback-color: var(--leaf-err); }
InlineMessage, Banner, Toast { --nz-feedback-color: var(--leaf-primary); --leaf-feedback-container: var(--leaf-pc); --leaf-on-feedback-container: var(--leaf-on-pc); --leaf-on-mark: var(--leaf-on-primary); }
InlineMessage[severity="success"], Banner[severity="success"], Toast[severity="success"] { --nz-feedback-color: var(--leaf-ok); --leaf-feedback-container: var(--leaf-ok-c); --leaf-on-feedback-container: var(--leaf-on-ok-c); --leaf-on-mark: var(--leaf-pane); }
InlineMessage[severity="warning"], Banner[severity="warning"], Toast[severity="warning"] { --nz-feedback-color: var(--leaf-warn); --leaf-feedback-container: var(--leaf-warn-c); --leaf-on-feedback-container: var(--leaf-on-warn-c); --leaf-on-mark: var(--leaf-pane); }
InlineMessage[severity="error"], Banner[severity="error"], Toast[severity="error"] { --nz-feedback-color: var(--leaf-err); --leaf-feedback-container: var(--leaf-err-c); --leaf-on-feedback-container: var(--leaf-on-err-c); --leaf-on-mark: var(--leaf-pane); }

/* Progress: a leaf track with a dot of light at the tip of the fill; indeterminate fills sweep across. */
ProgressBar, Meter { row-gap: 8px; }
ProgressBarLabel, MeterLabel { font-size: 13px; }
ProgressBarValue, MeterValue { color: var(--leaf-text-3); font-family: var(--leaf-font-mono); font-size: 12.5px; font-weight: 500; }
ProgressBarTrack, MeterTrack { height: 8px; border: 0; border-radius: 6px 2px 6px 2px; background: var(--leaf-s3); }
ProgressBar[size="small"] > ProgressBarTrack, Meter[size="small"] > MeterTrack { height: 4px; border-radius: 3px 1px 3px 1px; }
ProgressBar[size="large"] > ProgressBarTrack, Meter[size="large"] > MeterTrack { height: 12px; border-radius: 8px 2px 8px 2px; }
ProgressBarFill, MeterFill {
    border-radius: 6px 2px 6px 2px; background-color: var(--nz-feedback-color);
    transition: width 450ms cubic-bezier(0.16, 1, 0.3, 1);
}
/* The dot of light at the tip of the fill. */
ProgressBarFill::after, MeterFill::after {
    content: ""; position: absolute; right: 2px; top: 50%; width: 4px; height: 4px; margin-top: -2px;
    border-radius: 50%; background: var(--leaf-on-primary); opacity: 0.8;
}
ProgressBarFill[indeterminate]::after { content: none; }
ProgressBarFill[indeterminate], ProgressBarPulse {
    position: absolute; left: 0; top: 0; height: 100%; width: 34%; border-radius: 6px 2px 6px 2px;
    background: var(--nz-feedback-color); transition: none; animation: leaf-sweep 1.5s cubic-bezier(0.2, 0, 0, 1) infinite;
    --leaf-sweep-start: -103%; --leaf-sweep-end: 309%;
}
ProgressBarPulse {
    display: block; left: 42%; width: 16%; opacity: 0.45;
    animation: leaf-sweep 1.5s cubic-bezier(0.2, 0, 0, 1) 180ms infinite;
    --leaf-sweep-start: -481.25%; --leaf-sweep-end: 393.75%;
}
ProgressBarBuffer { border-radius: 6px 2px 6px 2px; background: color-mix(in srgb, var(--nz-feedback-color) 30%, var(--leaf-s3)); }
ProgressBarSegment, MeterSegment { border: 0; border-radius: 4px 1px 4px 1px; background: var(--leaf-s3); }
ProgressBarSegment[filled], MeterSegment[filled] { background: var(--nz-feedback-color); }
ProgressBarSegment[indeterminate] { background-image: none; background-color: var(--nz-feedback-color); animation: leaf-pulse 1.2s cubic-bezier(0.2, 0, 0, 1) infinite; }
Meter[orientation="vertical"] > MeterTrack { width: 8px; }

/* Proportional ring (.pring): the value is a full stroke over a thinner track, without a handle because it is not
   dragged. The geometry follows a 48-unit box: radius 20, track 2.5, value 4.5. At 100% the percentage gives way to
   a check that springs into place. Small rings carry no percentage. */
Spinner { gap: 8px; }
SpinnerTrack, SpinnerRing { stroke-radius: 41.667%; }
SpinnerTrack { color: var(--leaf-s3); transition: color 300ms; }
SpinnerRing { color: var(--nz-feedback-color); }
/* [size] outranks Basic's per-size stroke widths. */
Spinner[size] > SpinnerIndicator > SpinnerTrack { stroke-width: 5.208%; }
Spinner[size] > SpinnerIndicator > SpinnerRing { stroke-width: 9.375%; }
Spinner > SpinnerIndicator { width: 56px; height: 56px; }
Spinner[size="small"] > SpinnerIndicator { width: 40px; height: 40px; }
Spinner[size="large"] > SpinnerIndicator { width: 84px; height: 84px; }
Spinner:not([indeterminate]) SpinnerMark { display: none; }
SpinnerCheck {
    display: block; position: absolute; left: 29%; top: 29%; width: 42%; height: 42%; color: var(--nz-feedback-color);
    stroke-width: 10%; opacity: 0; transform: scale(0.3) rotate(-30deg);
    transition: opacity 200ms, transform 500ms cubic-bezier(0.34, 1.5, 0.64, 1);
}
Spinner[size="small"] SpinnerCheck { left: 25%; top: 25%; width: 50%; height: 50%; }
SpinnerIndicator[number-at-end] SpinnerCheck { opacity: 1; transform: scale(1) rotate(0deg); }
Spinner:not([indeterminate]) SpinnerTrack { stroke-trim-start: min(1, calc(var(--nz-presented-number) + 0.07)); stroke-trim-end: 0.93; }
SpinnerIndicator[number-at-start] SpinnerTrack { stroke-trim-start: 0; stroke-trim-end: 1; }
SpinnerIndicator[number-at-end] SpinnerTrack { stroke-trim-start: var(--nz-presented-number); stroke-trim-end: 1; }
Spinner.leaf-ring SpinnerTrack, Spinner.leaf-ring SpinnerRing { ring-shape: border-box; border-radius: 37.5% 8.333333% 37.5% 8.333333%; }
Spinner:not([indeterminate]) > SpinnerIndicator { number-motion: exponential; number-motion-decay: 0.14; number-motion-tolerance: 0.0005; number-boundary-tolerance: 0.003; }
Spinner[indeterminate] > SpinnerIndicator { number-motion: none; number-boundary-tolerance: 0; }
SpinnerIndicator[number-at-start] > SpinnerRing { opacity: 0; }
/* Inside a button the ring is a mini ring in the text color, without percentage or check. */
Button Spinner > SpinnerIndicator, ToggleButton Spinner > SpinnerIndicator { width: 18px; height: 18px; }
Button Spinner[size] > SpinnerIndicator > SpinnerTrack, ToggleButton Spinner[size] > SpinnerIndicator > SpinnerTrack { color: inherit; opacity: 0.25; stroke-width: 8.333%; }
Button Spinner[size] > SpinnerIndicator > SpinnerRing, ToggleButton Spinner[size] > SpinnerIndicator > SpinnerRing { color: inherit; stroke-width: 13.542%; }
Button Spinner SpinnerValue, ToggleButton Spinner SpinnerValue { display: none; }
Button Spinner SpinnerCheck, ToggleButton Spinner SpinnerCheck { display: none; }
Spinner[indeterminate] > SpinnerIndicator { width: 28px; height: 28px; }
Spinner[indeterminate][size="small"] > SpinnerIndicator { width: 18px; height: 18px; }
Spinner[indeterminate][size="large"] > SpinnerIndicator { width: 38px; height: 38px; }
Spinner[indeterminate] SpinnerTrack, Spinner[indeterminate] SpinnerRing { display: none; }
SpinnerMark {
    position: absolute; left: 15%; top: 15%; width: 70%; height: 70%; display: grid;
    grid-template-columns: 1fr 1fr; grid-template-rows: 1fr 1fr; gap: 54%;
    --leaf-mk1: var(--nz-feedback-color); --leaf-mk2: color-mix(in oklab, var(--nz-feedback-color) 42%, var(--leaf-pane));
}
Spinner[indeterminate] SpinnerMark { left: 0; top: 0; width: 100%; height: 100%; gap: 8%; }
Spinner:not([complete]) SpinnerMark { animation: leaf-turn 3.2s linear infinite; }
SpinnerValue {
    position: absolute; left: 0; top: 0; width: 100%; height: 100%; display: flex; align-items: center; justify-content: center;
    color: var(--leaf-text); font-family: var(--leaf-font-mono); font-size: 10.64px; font-weight: 600; line-height: 1; letter-spacing: -0.02em;
    opacity: 1; transform: scale(1);
    transition: opacity 200ms, transform 300ms cubic-bezier(0.34, 1.5, 0.64, 1);
}
Spinner[size="large"] SpinnerValue { font-size: 15.96px; }
Spinner[size="small"] SpinnerValue, Spinner[indeterminate] SpinnerValue { display: none; }
SpinnerIndicator[number-at-end] SpinnerValue { opacity: 0; transform: scale(0.6); }
SpinnerLabel { color: var(--leaf-text-2); font-size: 13px; }

/* Badges (.bdg): leaves that pop in with a spring. On decorated content a 2 px cutout in the color behind the badge
   (--leaf-badge-cutout, the pane by default) detaches the leaf from the icon on any background. */
BadgeIndicator {
    min-width: 18px; height: 18px; padding: 0 5px; border: 0; border-radius: 9px 3px 9px 3px;
    background: var(--nz-feedback-color); color: var(--nz-on-feedback); font-family: var(--leaf-font-ui); font-size: 11px; font-weight: 700; line-height: 18px;
    animation: leaf-badge-in 450ms cubic-bezier(0.34, 1.5, 0.64, 1);
}
BadgeIndicator[dot] { min-width: 7px; width: 7px; height: 11px; padding: 0; border-radius: var(--leaf-r-seed); }
Badge[size="small"] > BadgeIndicator { min-width: 14px; height: 14px; padding: 0 3px; border-radius: 7px 2px 7px 2px; font-size: 9px; line-height: 14px; }
Badge[size="large"] > BadgeIndicator { min-width: 22px; height: 22px; padding: 0 5px; border-radius: 11px 3px 11px 3px; font-size: 12.5px; line-height: 22px; }
Badge[decorating] > BadgeIndicator { z-index: 2; border: 0; box-shadow: 0 0 0 2px var(--leaf-badge-cutout, var(--leaf-pane)); }
Badge[decorating] > BadgeIndicator[corner="top-end"] { top: -6px; right: -8px; }
Badge[decorating] > BadgeIndicator[corner="top-start"] { top: -6px; left: -8px; }
Badge[decorating] > BadgeIndicator[corner="bottom-end"] { bottom: -6px; right: -8px; }
Badge[decorating] > BadgeIndicator[corner="bottom-start"] { bottom: -6px; left: -8px; }
Badge[decorating][size="large"] > BadgeIndicator[corner="top-end"] { top: -7px; right: -9px; }
Badge[decorating][size="large"] > BadgeIndicator[corner="top-start"] { top: -7px; left: -9px; }
Badge[decorating][size="large"] > BadgeIndicator[corner="bottom-end"] { bottom: -7px; right: -9px; }
Badge[decorating][size="large"] > BadgeIndicator[corner="bottom-start"] { bottom: -7px; left: -9px; }
Badge[decorating] > BadgeIndicator[dot][corner="top-end"] { top: -3px; right: -3px; }
Tag {
    gap: 6px; min-height: 23px; padding: 0 9px 0 7px; border: 0; border-radius: 10px 3px 10px 3px;
    background: var(--leaf-feedback-container); color: var(--leaf-on-feedback-container); font-size: 12px; font-weight: 550;
}
Tag[intent="accent"], Tag[intent="success"], Tag[intent="warning"], Tag[intent="danger"] { background: var(--leaf-feedback-container); color: var(--leaf-on-feedback-container); }
Tag[in-progress] > TagDot { animation: leaf-turn 1.6s cubic-bezier(0.2, 0, 0, 1) infinite; }
Tag[size="small"] { min-height: 19px; font-size: 11px; }
Tag[size="large"] { min-height: 28px; padding: 0 11px 0 9px; font-size: 13px; }
TagDot { width: 6px; height: 9px; border-radius: 4px 1px 4px 1px; background: currentColor; }
TagRemove { width: 16px; height: 16px; border-radius: 6px 2px 6px 2px; color: inherit; }
TagRemove:hover { background: color-mix(in srgb, var(--leaf-on-feedback-container) 14%, transparent); color: inherit; }

/* Notes: tonal leaves whose mark is a small filled leaf. */
InlineMessage {
    align-items: flex-start; gap: 12px; padding: 13px 12px 13px 16px; border: 0; border-radius: var(--leaf-r-ovl);
    background: var(--leaf-feedback-container); color: var(--leaf-on-feedback-container); font-size: 13.5px; line-height: 1.5;
    animation: leaf-rise 450ms cubic-bezier(0.16, 1, 0.3, 1) backwards;
}
InlineMessageTitle { font-weight: 650; }
InlineMessageBody { column-gap: 5px; }
InlineMessage > InlineMessageClose { margin: -3px -2px 0 0; }
InlineMessage + InlineMessage { margin-top: 8px; }
InlineMessageMark, BannerMark, ToastMark {
    width: 22px; height: 22px; padding: 4px; border-radius: 10px 3px 10px 3px;
    background: var(--nz-feedback-color); color: var(--leaf-on-mark); font-size: 12px; stroke-width: 2.4px;
}
InlineMessageClose, BannerClose, ToastClose, DialogClose, DrawerClose, PopoverClose, TeachingTipClose {
    width: 28px; height: 28px; border: 0; border-radius: var(--leaf-r-ctl); color: inherit; font-size: 22px; font-weight: 400; line-height: 1;
    transition: background-color 150ms, transform 250ms cubic-bezier(0.34, 1.5, 0.64, 1);
}
InlineMessageClose:hover, BannerClose:hover, ToastClose:hover, DialogClose:hover, DrawerClose:hover, PopoverClose:hover, TeachingTipClose:hover { background: var(--leaf-hover); }
InlineMessageClose:active, BannerClose:active, ToastClose:active, DialogClose:active, DrawerClose:active, PopoverClose:active, TeachingTipClose:active { background: var(--leaf-press); color: inherit; transform: scale(0.9); }
BannerAction, ToastAction, LoadingBoundaryRetry {
    min-height: 30px; padding: 0 12px; border: 0; border-radius: var(--leaf-r-ctl); background: transparent; color: var(--leaf-ink); font-weight: 550;
}
BannerAction:hover, ToastAction:hover, LoadingBoundaryRetry:hover { background: var(--leaf-hover); }
BannerAction:active, ToastAction:active, LoadingBoundaryRetry:active { background: var(--leaf-press); color: var(--leaf-ink); }
Banner { gap: 12px; padding: 8px 12px 8px 16px; border-bottom: 0; background: var(--leaf-feedback-container); color: var(--leaf-on-feedback-container); }

/* Toast: an inverse leaf that rises from below, with a draining timer line. */
ToastHostRegion { gap: 10px; }
Toast {
    align-items: center; gap: 14px; min-height: 50px; padding: 6px 8px 6px 14px; border: 0; border-radius: var(--leaf-r-ovl);
    background: var(--leaf-inverse); color: var(--leaf-on-inverse); font-size: 14px; box-shadow: var(--leaf-sh-pop); overflow-x: hidden; overflow-y: hidden;
    animation: leaf-toast 550ms cubic-bezier(0.16, 1, 0.3, 1) backwards;
}
Toast > ToastAction { color: color-mix(in oklab, var(--leaf-primary) 55%, var(--leaf-on-inverse)); }
Toast > ToastClose { color: var(--leaf-on-inverse); }
ToastTimer { height: 2px; background: color-mix(in oklab, var(--leaf-primary) 60%, var(--leaf-on-inverse)); }
ToastTimer[paused] { background: var(--leaf-text-3); }
@media (prefers-reduced-motion: no-preference) {
    Toast[countdown] > ToastTimer {
        width: 100%; transform-origin: left center;
        animation: leaf-toast-countdown var(--nz-toast-countdown-duration) linear forwards;
    }
}

EmptyState { gap: 8px; padding: 32px; }
EmptyStateIllustration { color: var(--leaf-text-3); }
EmptyStateTitle { font-family: var(--leaf-font-display); font-size: 20px; font-weight: 700; letter-spacing: -0.015em; }
EmptyStateDescription { color: var(--leaf-text-2); }
SkeletonLine { height: 8px; border-radius: 5px 1px 5px 1px; background: var(--leaf-s2); animation: leaf-pulse 1.6s cubic-bezier(0.2, 0, 0, 1) infinite; }
Skeleton[shape="circle"] { background: var(--leaf-s2); border-radius: 999px; animation: leaf-pulse 1.6s cubic-bezier(0.2, 0, 0, 1) infinite; }
Skeleton[shape="rectangle"] { background: var(--leaf-s2); border-radius: var(--leaf-r-ovl); animation: leaf-pulse 1.6s cubic-bezier(0.2, 0, 0, 1) infinite; }

/* Avatar: a leaf, not a circle, with the initials in the display face. */
Avatar {
    width: var(--nz-avatar-size, 40px); height: var(--nz-avatar-size, 40px); border: 0; border-radius: 16px 5px 16px 5px;
    background: var(--leaf-avatar, var(--leaf-primary)); color: var(--leaf-avatar-text, var(--leaf-on-primary)); font-family: var(--leaf-font-display); font-size: 14px; font-weight: 700;
}
/* --leaf-avatar and --leaf-avatar-text give each avatar its own color, as the prototype's --c and --avt. */
Avatar[size="small"] { width: var(--nz-avatar-size, 28px); height: var(--nz-avatar-size, 28px); border-radius: 11px 3px 11px 3px; font-size: 11px; }
Avatar[size="large"] { width: var(--nz-avatar-size, 56px); height: var(--nz-avatar-size, 56px); border-radius: 22px 6px 22px 6px; font-size: 19px; }
Avatar[shape="square"] { border-radius: var(--leaf-r-xs); }
AvatarImage { border-radius: 16px 5px 16px 5px; }
Avatar[size="small"] > AvatarImage { border-radius: 11px 3px 11px 3px; }
Avatar[size="large"] > AvatarImage { border-radius: 22px 6px 22px 6px; }
AvatarFallback { border-radius: var(--leaf-r-xs); background: color-mix(in srgb, var(--leaf-on-pc) 35%, transparent); }
AvatarPresence { border: 2px solid var(--leaf-pane); border-radius: 4px 1px 4px 1px; }
AvatarGroup > Avatar, AvatarGroupOverflow { border: 2px solid var(--leaf-pane); }
AvatarGroupOverflow { width: 40px; height: 40px; border-radius: 16px 5px 16px 5px; background: var(--leaf-s2); color: var(--leaf-text-2); }
LoadingBoundaryError { color: var(--leaf-err); }

/* ---- Group: Actions and app frame (actions-frame) ---- */
ButtonGroup > [position] { border-radius: 0; }
ButtonGroup[orientation="horizontal"] > [position="first"] { border-radius: 11px 0 0 3px; }
ButtonGroup[orientation="horizontal"] > [position="last"] { border-radius: 0 3px 11px 0; }
ButtonGroup[orientation="vertical"] > [position="first"] { border-radius: 11px 3px 0 0; }
ButtonGroup[orientation="vertical"] > [position="last"] { border-radius: 0 0 11px 3px; }
ButtonGroup > [position="only"] { border-radius: var(--leaf-r-ctl); }
ButtonGroup > [position]:hover, ButtonGroup > [position]:active { transform: scale(1); }

/* Split button: one tonal leaf with a thin divider; the leaf morphs as a whole. */
SplitButton {
    --leaf-split-on: var(--leaf-on-pc);
    min-height: var(--leaf-h); border: 0; border-radius: var(--leaf-r-ctl); background: var(--leaf-pc); color: var(--leaf-on-pc); overflow-x: hidden; overflow-y: hidden;
    transition: border-radius 350ms cubic-bezier(0.34, 1.5, 0.64, 1);
}
SplitButton:hover { border-radius: var(--leaf-r-ctl-hover); }
SplitButton[appearance="accent"] { --leaf-split-on: var(--leaf-on-primary); background: var(--leaf-primary); color: var(--leaf-on-primary); box-shadow: var(--leaf-glow); }
SplitButton[appearance="outline"], SplitButton[appearance="subtle"] { --leaf-split-on: var(--leaf-text); background: transparent; color: var(--leaf-text); box-shadow: inset 0 0 0 1px var(--leaf-stroke-2); }
SplitButton[appearance="subtle"] { box-shadow: none; }
SplitButton[intent="danger"][appearance="accent"] { --leaf-split-on: var(--leaf-on-err); background: var(--leaf-err); color: var(--leaf-on-err); }
SplitButtonPrimary { gap: 8px; padding: 0 14px 0 16px; font-weight: 550; }
SplitButtonTrigger { padding: 0 10px; }
SplitButtonDivider { width: 1px; height: 18px; align-self: center; background: color-mix(in srgb, var(--leaf-split-on) 28%, transparent); }
SplitButtonPrimary:hover, SplitButtonTrigger:hover { background: color-mix(in srgb, var(--leaf-split-on) 8%, transparent); }
SplitButtonPrimary:active, SplitButtonTrigger:active, SplitButton[expanded] > SplitButtonTrigger, SplitButton[pressed] > SplitButtonPrimary { background: color-mix(in srgb, var(--leaf-split-on) 16%, transparent); color: inherit; }
SplitButtonChevron { width: 18px; height: 18px; stroke-width: 1.7px; }
SplitButton[size="small"] { min-height: 28px; font-size: 13px; }
SplitButton[size="large"] { min-height: 42px; font-size: 15px; }
SplitButton:disabled { background: var(--leaf-s2); color: var(--leaf-text-3); box-shadow: none; }

Link {
    color: var(--leaf-ink); text-decoration-line: underline; text-decoration-thickness: 1.5px;
    text-decoration-color: color-mix(in srgb, var(--leaf-ink) 40%, transparent); transition: text-decoration-color 200ms;
}
Link:hover { text-decoration-style: solid; text-decoration-color: var(--leaf-ink); }
Link:active { color: var(--leaf-text); }
Link[visited] { color: color-mix(in srgb, var(--leaf-ink) 70%, var(--leaf-text)); }

/* Toolbar: a pane-colored leaf strip of icon buttons. */
Toolbar { gap: 2px; padding: 3px; border-radius: var(--leaf-r-ovl); background: var(--leaf-pane); box-shadow: inset 0 0 0 1px var(--leaf-stroke); }
Toolbar { align-self: flex-start; }
Toolbar Button, Toolbar ToggleButton, Toolbar MenuButton { min-width: var(--leaf-h); background: transparent; box-shadow: none; color: var(--leaf-text-2); padding: 0 8px; transition: background-color 150ms, color 150ms, transform 250ms cubic-bezier(0.34, 1.5, 0.64, 1); }
Toolbar Button:hover, Toolbar ToggleButton:hover, Toolbar MenuButton:hover { background: var(--leaf-hover); color: var(--leaf-text); border-radius: var(--leaf-r-ctl); }
Toolbar Button:active, Toolbar ToggleButton:active { transform: scale(0.9); border-radius: var(--leaf-r-ctl); background: var(--leaf-hover); }
Toolbar Button[icon-only], Toolbar ToggleButton[icon-only], Toolbar MenuButton[icon-only] { width: var(--leaf-h); padding: 0; }
Toolbar Icon { width: 20px; height: 20px; font-size: 20px; }
/* A pressed toolbar toggle turns yellow, as the prototype's favourite star. */
Toolbar ToggleButton[pressed], Toolbar ToggleButton[pressed]:hover { background: transparent; color: var(--leaf-g3); }
ToolbarSeparator { margin: 6px 3px; background: var(--leaf-stroke-2); }
ActionBar { gap: 8px; padding: 10px 12px; border-top: 1px solid var(--leaf-stroke); background: transparent; }

/* Floating action button: a big tonal leaf that flips its diagonal on hover. */
FloatingActionButton {
    gap: 10px; min-width: 56px; height: 56px; padding: 0 18px; border: 0; border-radius: 24px 6px 24px 6px;
    background: var(--leaf-pc); color: var(--leaf-on-pc); box-shadow: var(--leaf-glow);
    font-family: var(--leaf-font-display); font-size: 15px; font-weight: 700; letter-spacing: -0.01em;
    transform: translateY(0px);
    transition: transform 350ms cubic-bezier(0.34, 1.5, 0.64, 1), border-radius 400ms cubic-bezier(0.34, 1.5, 0.64, 1), background-color 200ms;
}
FloatingActionButton:hover { transform: translateY(-3px); border-radius: 6px 24px 6px 24px; border-color: transparent; }
FloatingActionButton:active { background: color-mix(in srgb, var(--leaf-pc) 85%, var(--leaf-on-pc)); color: var(--leaf-on-pc); }
FloatingActionButton:disabled { background: var(--leaf-s2); color: var(--leaf-text-3); box-shadow: none; }
FloatingActionButton[extended] { padding: 0 22px 0 18px; }
FloatingActionButton Icon { width: 22px; height: 22px; font-size: 22px; }
FloatingActionButton[size="small"] { min-width: 42px; height: 42px; border-radius: 17px 5px 17px 5px; font-size: 14px; }
FloatingActionButton[size="large"] { min-width: 72px; height: 72px; border-radius: 28px 8px 28px 8px; font-size: 18px; }

/* Title bar: drawn on the window material, with the platform heights of the prototype. */
TitleBar { gap: 10px; padding-left: 14px; padding-right: 6px; background: transparent; color: var(--leaf-text); font-size: 12px; }
TitleBar[fullscreen] { display: none; }
TitleBarHeading { gap: 8px; }
TitleBarTitle { font-family: var(--leaf-font-display); font-size: 14px; font-weight: 700; letter-spacing: -0.01em; }
TitleBarSubtitle { color: var(--leaf-text-3); font-size: 12.5px; }
TitleBarLeading, TitleBarTrailing { gap: 2px; }
TitleBarLeading Button, TitleBarTrailing Button, TitleBarLeading ToggleButton, TitleBarTrailing ToggleButton { min-height: 32px; background: transparent; box-shadow: none; color: var(--leaf-text-2); }
TitleBarLeading Button:hover, TitleBarTrailing Button:hover, TitleBarLeading ToggleButton:hover, TitleBarTrailing ToggleButton:hover { background: var(--leaf-hover); color: var(--leaf-text); }
TitleBarCenter > TextField, TitleBarCenter > SearchField {
    width: 380px; max-width: 100%; min-height: 32px; background: color-mix(in oklab, var(--leaf-pane) 60%, transparent); box-shadow: inset 0 0 0 1px var(--leaf-stroke); color: var(--leaf-text-3);
}
TitleBarCenter > TextField:focus-within, TitleBarCenter > SearchField:focus-within { background: var(--leaf-pane); box-shadow: inset 0 0 0 1.5px var(--leaf-ink), 0 0 0 4px var(--leaf-focus-ring); }
TitleBar { min-height: max(var(--nz-window-controls-height, 38px), 38px); }
TitleBar[height="tall"] { min-height: max(var(--nz-window-controls-height, 52px), 52px); padding-left: 6px; }
@media (platform: windows) {
    TitleBar { min-height: max(var(--nz-window-controls-height, 32px), 32px); }
    TitleBar[height="tall"] { min-height: max(var(--nz-window-controls-height, 48px), 48px); padding-left: 4px; }
}
@media (platform: macos) {
    TitleBar { min-height: max(var(--nz-window-controls-height, 28px), 28px); }
    TitleBar[height="tall"] { min-height: max(var(--nz-window-controls-height, 52px), 52px); }
    /* The standard caption centers its title, as system applications do. */
    TitleBar[height="standard"] > TitleBarHeading { position: absolute; left: 0; right: 0; top: 0; bottom: 0; justify-content: center; pointer-events: none; }
    TitleBar[height="standard"] TitleBarTitle { font-family: var(--leaf-font-ui); font-size: 13px; font-weight: 600; color: var(--leaf-text-2); }
}
TitleBar[height="tall"] > TitleBarAppIcon { margin-left: 6px; }
LeafMark.leaf-titlebar-mark { --leaf-mark-size: 16px; }
TitleBar[height="tall"] LeafMark.leaf-titlebar-mark { --leaf-mark-size: 18px; }

/* Caption buttons drawn by Nazor without the leaf lights: flat, close turns red. */
CaptionButtonsButton { color: var(--leaf-text); transition: background-color 100ms, color 100ms; }
CaptionButtonsButton:hover { background: color-mix(in srgb, var(--leaf-text) 7%, transparent); }
CaptionButtonsButton:active { background: color-mix(in srgb, var(--leaf-text) 11%, transparent); }

PageHeader { gap: 4px; padding: 26px 0 14px; }
PageHeaderBreadcrumb { gap: 2px; color: var(--leaf-text-2); font-size: 14px; }
PageHeaderTitle { font-variation-settings: "opsz" 72; font-family: var(--leaf-font-display); font-size: 34px; font-weight: 800; line-height: 1.05; letter-spacing: -0.035em; transition: font-size 350ms cubic-bezier(0.16, 1, 0.3, 1); }
PageHeaderSubtitle { color: var(--leaf-text-3); font-size: 13px; }
PageHeader[compact] { padding: 12px 0 10px; }
PageHeader[compact] PageHeaderTitle { font-size: 20px; }

/* ---- Group: Menus and overlays (overlays) ---- */
MenuBar { gap: 2px; padding: 5px; border-bottom: 1px solid var(--leaf-stroke); background: transparent; }
MenuBarItem { min-height: 30px; padding: 0 11px; border-radius: var(--leaf-r-item); font-size: 13.5px; }
MenuBarItem:hover, MenuBarItem:focus { background: var(--leaf-hover); color: var(--leaf-text); }
MenuBarItem[expanded] { background: var(--leaf-press); color: var(--leaf-text); }
DialogCloseMark, DrawerCloseMark, PopoverCloseMark, TeachingTipCloseMark { font-size: 18px; }

PopoverArrow, TeachingTipArrow { border: 0; background: var(--leaf-pop); box-shadow: inset 0 0 0 1px var(--leaf-stroke-2); border-radius: 2px; }
TooltipArrow { border: 0; background: var(--leaf-inverse); border-radius: 2px; }
Popover {
    gap: 10px; padding: 14px; border: 0; border-radius: var(--leaf-r-ovl); background: var(--leaf-pop); color: var(--leaf-text);
    box-shadow: var(--leaf-sh-pop), inset 0 0 0 1px var(--leaf-stroke-2), inset 0 1px 0 var(--leaf-hi);
}
PopoverTitle, TeachingTipTitle { font-family: var(--leaf-font-display); font-size: 15px; font-weight: 700; letter-spacing: -0.01em; }
Tooltip[tooltip-surface] { gap: 2px; }
TooltipTitle { font-weight: 650; }
TeachingTip {
    gap: 10px; padding: 16px; border: 0; border-radius: var(--leaf-r-ovl); background: var(--leaf-pop); color: var(--leaf-text);
    box-shadow: var(--leaf-sh-pop), inset 0 0 0 1px var(--leaf-stroke-2);
}
TeachingTipHero { margin: -16px -16px 0; }
TeachingTipSubtitle { color: var(--leaf-text-2); }

/* Dialog: the window leaf; it rises and its corners settle from a wider leaf. */
Dialog, AlertDialog, Drawer, CommandPalette {
    border: 0; background: var(--leaf-pane); color: var(--leaf-text);
    box-shadow: var(--leaf-sh-pop), inset 0 0 0 1px var(--leaf-stroke-2);
}
Dialog, AlertDialog { border-radius: var(--leaf-r-win); padding: 26px 22px 22px 26px; gap: 16px; width: 470px; animation: leaf-dialog 550ms cubic-bezier(0.16, 1, 0.3, 1) backwards; }
Dialog[size="small"] { width: 360px; }
Dialog[size="large"] { width: 720px; }
Dialog[size="fullscreen"], Dialog[dialog-placement="top"], Dialog[dialog-placement="bottom"], Dialog[dialog-placement="start"], Dialog[dialog-placement="end"] { border-radius: 0; }
Dialog { --nz-dialog-title-font-size: 22px; --nz-dialog-title-line-height: 1.2; }
DialogTitle, AlertDialogTitle, DrawerTitle { font-family: var(--leaf-font-display); font-weight: 750; letter-spacing: -0.02em; }
AlertDialogTitle, DrawerTitle { font-size: 22px; line-height: 1.2; }
DialogSubtitle { color: var(--leaf-text-2); }
DialogBody, AlertDialogMessage { color: var(--leaf-text-2); }
DialogFooter, AlertDialogFooter, DrawerFooter { gap: 8px; padding-top: 4px; }
AlertDialog { row-gap: 16px; width: 470px; }
AlertDialogSeverity {
    width: 44px; height: 44px; margin-right: 16px; padding: 12px; border-radius: 17px 5px 17px 5px;
    background: var(--leaf-pc); color: var(--leaf-on-pc); stroke-width: 2px;
}
AlertDialogSeverity[severity="success"] { background: var(--leaf-ok-c); color: var(--leaf-ok); }
AlertDialogSeverity[severity="warning"] { background: var(--leaf-warn-c); color: var(--leaf-warn); }
AlertDialogSeverity[severity="error"] { background: var(--leaf-err-c); color: var(--leaf-err); }
AlertDialogDetailsToggle { color: var(--leaf-ink); }
AlertDialogDetailsContent { border: 0; border-radius: var(--leaf-r-ctl); background: var(--leaf-s1); box-shadow: inset 0 0 0 1px var(--leaf-stroke); }
AlertDialogDetailsText { font-family: var(--leaf-font-mono); }
Drawer { gap: 14px; padding: 20px; }
Drawer[side="start"] { border-radius: 0 22px 6px 0; }
Drawer[side="end"] { border-radius: 22px 0 0 6px; }
Drawer[side="bottom"] { border-radius: 22px 6px 0 0; }
Drawer[side="top"] { border-radius: 0 0 22px 6px; }
DrawerHandle { width: 36px; height: 5px; border-radius: 4px 1px 4px 1px; background: var(--leaf-s3); }
CommandPalette { border-radius: var(--leaf-r-pane); overflow-x: hidden; overflow-y: hidden; animation: leaf-dialog 450ms cubic-bezier(0.16, 1, 0.3, 1) backwards; }
CommandPaletteSearchInput { min-height: 52px; padding: 16px 18px; border-bottom: 1px solid var(--leaf-stroke); font-size: 15px; }
CommandPaletteSearchPopup { padding: 5px; }
CommandPaletteItem { min-height: var(--leaf-h-menu); padding: 4px 10px; border-radius: var(--leaf-r-item); }
CommandPaletteSearchOption[current] > CommandPaletteItem { background: var(--leaf-hover); color: var(--leaf-text); }
CommandPaletteItemDescription { color: var(--leaf-text-3); }

/* ---- Group: Selection (selection) ---- */
RadioGroup { gap: 10px; }
RadioButton { column-gap: 11px; line-height: 1.35; }
/* The radio is a circle turned a quarter; checked, it turns into a leaf with a dot. */
RadioButtonMark {
    width: 20px; height: 20px; border: 1.6px solid var(--leaf-text-3); border-radius: 10px; background: var(--leaf-ctl);
    transform: rotate(-90deg);
    transition: border-radius 500ms cubic-bezier(0.34, 1.5, 0.64, 1), transform 500ms cubic-bezier(0.34, 1.5, 0.64, 1), background-color 250ms, border-color 250ms, box-shadow 200ms;
}
RadioButtonDot { width: 7px; height: 7px; border-radius: 4px; background: var(--leaf-on-primary); transform: scale(0); transition: transform 350ms cubic-bezier(0.34, 1.5, 0.64, 1) 80ms; }
RadioButton:hover > RadioButtonMark { border-color: var(--leaf-text-2); border-radius: 9px 7px 9px 7px; box-shadow: 0 0 0 5px var(--leaf-hover); }
RadioButton:active > RadioButtonMark { transform: rotate(-90deg) scale(0.88); }
RadioButton:checked > RadioButtonMark { border-color: var(--leaf-primary); border-radius: 12px 3px 12px 3px; background: var(--leaf-primary); transform: rotate(0deg); }
RadioButton:checked > RadioButtonMark > RadioButtonDot { background: var(--leaf-on-primary); transform: scale(1); }
RadioButton:checked:hover > RadioButtonMark { border-radius: 12px 3px 12px 3px; box-shadow: 0 0 0 5px var(--leaf-hover); }
RadioButton:checked:active > RadioButtonMark { transform: rotate(0deg) scale(0.88); }
RadioButton:disabled { opacity: 0.5; color: var(--leaf-text); }
RadioButton:disabled > RadioButtonMark { box-shadow: none; }
RadioGroup[invalid] > RadioButton > RadioButtonMark { border-color: var(--leaf-err); }
RadioButtonDescription { color: var(--leaf-text-3); font-size: 12.5px; }
RadioButton[size="small"] > RadioButtonMark { width: 16px; height: 16px; border-radius: 8px; }
RadioButton[size="large"] > RadioButtonMark { width: 24px; height: 24px; border-radius: 12px; }

/* Switch: a leaf track with a leaf thumb that slides with a spring and stretches while pressed; the track draws the
   on and off symbols, which swap colors when checked. */
Switch { column-gap: 14px; row-gap: 0; }
/* Label before: the switch is centered on the label and its description, as .swrow. */
Switch[label-position="before"] > SwitchTrack { grid-row-start: 1; grid-row-end: 3; }
SwitchTrack {
    position: relative; min-width: 54px; width: 54px; height: 30px; padding: 0 3px; border: 0; border-radius: 15px 4px 15px 4px;
    background-color: var(--leaf-s3);
    background-image: linear-gradient(transparent, transparent), radial-gradient(circle, transparent 3.4px, var(--leaf-text-3) 3.6px, var(--leaf-text-3) 5px, transparent 5.4px);
    background-position: 14px 50%, 30px 50%; background-size: 2px 11px, 16px 16px; background-repeat: no-repeat, no-repeat;
    box-shadow: inset 0 0 0 1px var(--leaf-stroke); justify-content: flex-start;
    transition: background-color 300ms cubic-bezier(0.2, 0, 0, 1);
}
SwitchThumb {
    width: 26px; height: 24px; border-radius: 12px 3px 12px 3px; background: var(--leaf-pane);
    box-shadow: 0 1px 2px rgba(0, 0, 0, 0.18), inset 0 1px 0 var(--leaf-hi);
    transform-origin: left center;
    transform: none;
    transition: width 250ms cubic-bezier(0.2, 0, 0, 1), border-radius 400ms cubic-bezier(0.34, 1.5, 0.64, 1);
}
Switch:hover > SwitchTrack { box-shadow: inset 0 0 0 1px var(--leaf-stroke); }
Switch:hover > SwitchTrack > SwitchThumbRail > SwitchThumbPosition > SwitchThumb { box-shadow: 0 2px 5px rgba(0, 0, 0, 0.2), inset 0 1px 0 var(--leaf-hi); }
Switch:active > SwitchTrack > SwitchThumbRail > SwitchThumbPosition > SwitchThumb { transform: none; }
/* Checked: the I bar turns on-primary and the O ring disappears. Basic's state rules use the background shorthand,
   which resets every layer, so the layers are restated here and for the disabled state. */
Switch:checked > SwitchTrack { background-image: linear-gradient(var(--leaf-on-primary), var(--leaf-on-primary)), linear-gradient(transparent, transparent); background-position: 14px 50%, 30px 50%; background-size: 2px 11px, 16px 16px; background-repeat: no-repeat, no-repeat; justify-content: flex-start; background-color: var(--leaf-primary); box-shadow: none; color: var(--leaf-on-primary); }
Switch:checked > SwitchTrack > SwitchThumbRail > SwitchThumbPosition > SwitchThumb { background: var(--leaf-pane); transform: none; }
Switch:checked:active > SwitchTrack > SwitchThumbRail > SwitchThumbPosition > SwitchThumb { transform: none; }
/* Disabled: only the switch fades (.sw:disabled); the label keeps its color. */
Switch:disabled { color: var(--leaf-text); }
Switch:disabled > SwitchTrack { opacity: 0.45; }
Switch:disabled > SwitchTrack { background-color: var(--leaf-s3); background-image: linear-gradient(transparent, transparent), radial-gradient(circle, transparent 3.4px, var(--leaf-text-3) 3.6px, var(--leaf-text-3) 5px, transparent 5.4px); background-position: 14px 50%, 30px 50%; background-size: 2px 11px, 16px 16px; background-repeat: no-repeat, no-repeat; }
Switch:disabled:checked > SwitchTrack { background-color: var(--leaf-primary); background-image: linear-gradient(var(--leaf-on-primary), var(--leaf-on-primary)), linear-gradient(transparent, transparent); }
Switch:disabled > SwitchTrack > SwitchThumbRail > SwitchThumbPosition > SwitchThumb { background: var(--leaf-pane); }
Switch[invalid] > SwitchTrack { box-shadow: inset 0 0 0 1.6px var(--leaf-err); }
SwitchDescription { color: var(--leaf-text-3); font-size: 12.5px; }
Switch[size="small"] > SwitchTrack { min-width: 42px; width: 42px; height: 24px; border-radius: 12px 3px 12px 3px; background-position: 11px 50%, 22px 50%; background-size: 2px 9px, 14px 14px; }
Switch[size="small"] > SwitchTrack > SwitchThumbRail > SwitchThumbPosition > SwitchThumb { width: 20px; height: 18px; border-radius: 9px 2px 9px 2px; transform: none; }
Switch[size="small"]:checked > SwitchTrack > SwitchThumbRail > SwitchThumbPosition > SwitchThumb { transform: none; }
Switch[size="large"] > SwitchTrack { min-width: 64px; width: 64px; height: 36px; border-radius: 18px 5px 18px 5px; background-position: 17px 50%, 37px 50%; }
Switch[size="large"] > SwitchTrack > SwitchThumbRail > SwitchThumbPosition > SwitchThumb { width: 32px; height: 30px; border-radius: 15px 4px 15px 4px; transform: none; }
Switch[size="large"]:checked > SwitchTrack > SwitchThumbRail > SwitchThumbPosition > SwitchThumb { transform: none; }

Switch { --nz-switch-inset: 3px; --nz-switch-thumb-width: 26px; --nz-switch-thumb-height: 24px; --leaf-switch-pressed-width: 32px; }
Switch[size="small"] { --nz-switch-thumb-width: 20px; --nz-switch-thumb-height: 18px; --leaf-switch-pressed-width: 25px; }
Switch[size="large"] { --nz-switch-thumb-width: 32px; --nz-switch-thumb-height: 30px; --leaf-switch-pressed-width: 39px; }
SwitchThumbPosition { transition: left 500ms cubic-bezier(0.34, 1.5, 0.64, 1); }
SwitchThumb { position: absolute; left: 0; top: 0; }
Switch:checked > SwitchTrack > SwitchThumbRail > SwitchThumbPosition > SwitchThumb { left: auto; right: 0; }
Switch:active > SwitchTrack > SwitchThumbRail > SwitchThumbPosition > SwitchThumb { width: var(--leaf-switch-pressed-width); }
Switch[dragging] > SwitchTrack > SwitchThumbRail > SwitchThumbPosition { transition: none; }
Switch[dragging]:not([drag-on]) > SwitchTrack { background-color: var(--leaf-s3); background-image: linear-gradient(transparent, transparent), radial-gradient(circle, transparent 3.4px, var(--leaf-text-3) 3.6px, var(--leaf-text-3) 5px, transparent 5.4px); background-position: 14px 50%, 30px 50%; background-size: 2px 11px, 16px 16px; background-repeat: no-repeat, no-repeat; color: var(--leaf-text-3); }
Switch[dragging][drag-on] > SwitchTrack { background-color: var(--leaf-primary); background-image: linear-gradient(var(--leaf-on-primary), var(--leaf-on-primary)), linear-gradient(transparent, transparent); background-position: 14px 50%, 30px 50%; background-size: 2px 11px, 16px 16px; background-repeat: no-repeat, no-repeat; color: var(--leaf-on-primary); }
Switch[dragging] > SwitchTrack > SwitchThumbRail > SwitchThumbPosition > SwitchThumb { background: var(--leaf-pane); }

/* Segmented control: a recessed leaf whose pane-colored indicator slides to the chosen segment. */
SegmentedControl { padding: 3px; border: 0; border-radius: 13px 4px 13px 4px; background: color-mix(in oklab, var(--leaf-s2) 85%, transparent); box-shadow: inset 0 0 0 1px var(--leaf-stroke); }
Segment {
    position: relative; z-index: 1; min-height: 28px; padding: 0 13px; border-left: 0; border-radius: var(--leaf-r-ctl);
    background: transparent; color: var(--leaf-text-2); font-size: 13px; font-weight: 500; transition: color 200ms;
}
Segment:hover { background: transparent; color: var(--leaf-text); }
Segment[selected] { background: transparent; color: var(--leaf-text); }
Segment:disabled { color: var(--leaf-text-3); }
Segment[selected]:disabled { background: transparent; color: var(--leaf-text-3); }
SegmentedControlIndicator {
    display: block; z-index: 0; border-radius: var(--leaf-r-ctl); background: var(--leaf-pane);
    box-shadow: 0 1px 3px rgba(0, 0, 0, 0.14), inset 0 0 0 1px var(--leaf-stroke), inset 0 1px 0 var(--leaf-hi);
    transition: left 450ms cubic-bezier(0.16, 1, 0.3, 1), top 450ms cubic-bezier(0.16, 1, 0.3, 1), width 450ms cubic-bezier(0.16, 1, 0.3, 1), height 450ms cubic-bezier(0.16, 1, 0.3, 1);
}
SegmentedControlIndicator[hidden] { display: none; }
Segment[size="small"] { min-height: 22px; padding: 0 9px; font-size: 12px; }
Segment[size="large"] { min-height: 36px; padding: 0 18px; font-size: 14px; }

/* Slider: a leaf track filled with the seed color; the thumb is a slim upright bar that grows under the pointer. */
Slider, RangeSlider { --leaf-slider-direction: to right; --leaf-slider-value: var(--leaf-primary); }
Slider {
    --leaf-slider-gap: 7px; --leaf-slider-center: calc(var(--nz-slider-position) * 100%);
    --leaf-slider-before: var(--leaf-slider-value); --leaf-slider-after: var(--leaf-s3);
}
Slider[reversed] { --leaf-slider-before: var(--leaf-s3); --leaf-slider-after: var(--leaf-slider-value); }
Slider:active { --leaf-slider-gap: 9px; }
RangeSlider {
    --leaf-slider-start-gap: 7px; --leaf-slider-end-gap: 7px;
    --leaf-slider-start: calc(var(--nz-range-start) * 100%); --leaf-slider-end: calc(var(--nz-range-end) * 100%);
    --leaf-slider-gap-start: min(calc(var(--leaf-slider-start) - var(--leaf-slider-start-gap)), calc(var(--leaf-slider-end) - var(--leaf-slider-end-gap)));
    --leaf-slider-fill-start: calc(var(--leaf-slider-start) + var(--leaf-slider-start-gap));
    --leaf-slider-fill-end: max(var(--leaf-slider-fill-start), calc(var(--leaf-slider-end) - var(--leaf-slider-end-gap)));
    --leaf-slider-gap-end: max(var(--leaf-slider-fill-start), calc(var(--leaf-slider-end) + var(--leaf-slider-end-gap)));
}
RangeSlider:not([reversed]):has(RangeSliderHandle[thumb="lower"]:active), RangeSlider[reversed]:has(RangeSliderHandle[thumb="upper"]:active) { --leaf-slider-start-gap: 9px; }
RangeSlider:not([reversed]):has(RangeSliderHandle[thumb="upper"]:active), RangeSlider[reversed]:has(RangeSliderHandle[thumb="lower"]:active) { --leaf-slider-end-gap: 9px; }
Slider[orientation="vertical"], RangeSlider[orientation="vertical"] { --leaf-slider-direction: to top; }
Slider:disabled, RangeSlider:disabled { --leaf-slider-value: var(--leaf-text-3); }
SliderTrack, RangeSliderTrack { top: 7px; height: 8px; border: 0; border-radius: 6px 2px 6px 2px; }
SliderTrack {
    background: linear-gradient(var(--leaf-slider-direction),
        var(--leaf-slider-before) calc(var(--leaf-slider-center) - var(--leaf-slider-gap)),
        transparent calc(var(--leaf-slider-center) - var(--leaf-slider-gap)),
        transparent calc(var(--leaf-slider-center) + var(--leaf-slider-gap)),
        var(--leaf-slider-after) calc(var(--leaf-slider-center) + var(--leaf-slider-gap)));
}
RangeSliderTrack {
    background: linear-gradient(var(--leaf-slider-direction),
        var(--leaf-s3) var(--leaf-slider-gap-start), transparent var(--leaf-slider-gap-start),
        transparent var(--leaf-slider-fill-start), var(--leaf-slider-value) var(--leaf-slider-fill-start),
        var(--leaf-slider-value) var(--leaf-slider-fill-end), transparent var(--leaf-slider-fill-end),
        transparent var(--leaf-slider-gap-end), var(--leaf-s3) var(--leaf-slider-gap-end));
}
SliderFill, RangeSliderFill { display: none; }
SliderThumb, RangeSliderThumb {
    top: -1px; width: 6px; height: 24px; border: 0; border-radius: 3px; background: var(--leaf-primary);
    box-shadow: none;
    transition: transform 180ms cubic-bezier(0.2, 0, 0, 1);
}
SliderThumb { left: calc(var(--nz-slider-position) * (100% - 14px) + 4px); }
RangeSliderThumb { left: calc(var(--nz-range-thumb-position) * 100% - 3px); }
/* Keep labels at their own scale when they are drawn inside the thumb. */
Slider:not([value-label="none"]) > SliderThumb, RangeSlider:not([value-label="none"]) RangeSliderThumb {
    transition: width 180ms ease-out, height 180ms ease-out, top 180ms ease-out;
}
Slider:hover > SliderThumb, RangeSliderHandle:hover > RangeSliderThumb { height: 28px; top: -3px; }
Slider:active > SliderThumb, RangeSliderHandle:active > RangeSliderThumb { width: 10px; }
Slider[orientation="horizontal"]:not([value-label="none"]):active > SliderThumb { left: calc(var(--nz-slider-position) * (100% - 14px) + 2px); }
RangeSlider[orientation="horizontal"]:not([value-label="none"]) RangeSliderHandle:active > RangeSliderThumb { left: calc(var(--nz-range-thumb-position) * 100% - 5px); }
Slider[value-label="none"]:hover > SliderThumb, RangeSlider[value-label="none"] RangeSliderHandle:hover > RangeSliderThumb { height: 24px; top: -1px; transform: scale(1, 1.167); }
Slider[value-label="none"]:active > SliderThumb, RangeSlider[value-label="none"] RangeSliderHandle:active > RangeSliderThumb { width: 6px; transform: scale(1.667, 1.167); }
Slider:focus-visible { outline-style: none; }
Slider:focus-visible > SliderThumb, RangeSliderHandle:focus-visible > RangeSliderThumb { box-shadow: none; outline: 2px solid var(--leaf-ink); outline-offset: 4px; }
Slider:disabled > SliderThumb, RangeSlider:disabled > RangeSliderRail > RangeSliderHandle > RangeSliderThumb { background: var(--leaf-text-3); }
Slider[invalid] > SliderTrack { box-shadow: inset 0 0 0 1.5px var(--leaf-err); }
SliderTick, RangeSliderTick { background: var(--leaf-stroke-2); }
SliderTickLabel, RangeSliderTickLabel { color: var(--leaf-text-3); font-family: var(--leaf-font-mono); }
SliderValueLabel, RangeSliderValueLabel { padding: 2px 6px; border-radius: 8px 2px 8px 2px; background: var(--leaf-inverse); color: var(--leaf-on-inverse); font-family: var(--leaf-font-mono); }
Slider[orientation="vertical"] > SliderTrack { left: 7px; width: 8px; }
Slider[orientation="vertical"] > SliderThumb { top: auto; left: -1px; width: 24px; height: 6px; bottom: calc(var(--nz-slider-position) * (100% - 14px) + 4px); }
RangeSlider[orientation="vertical"] > RangeSliderRail > RangeSliderTrack { left: 7px; width: 8px; }
RangeSlider[orientation="vertical"] > RangeSliderRail > RangeSliderHandle > RangeSliderThumb { left: -1px; width: 24px; height: 6px; bottom: calc(var(--nz-range-thumb-position) * 100% - 3px); }
Slider[orientation="vertical"]:not([value-label="none"]):hover > SliderThumb, RangeSlider[orientation="vertical"]:not([value-label="none"]) RangeSliderHandle:hover > RangeSliderThumb { top: auto; left: -3px; width: 28px; height: 6px; }
Slider[orientation="vertical"]:not([value-label="none"]):active > SliderThumb { left: -3px; width: 28px; height: 10px; bottom: calc(var(--nz-slider-position) * (100% - 14px) + 2px); }
RangeSlider[orientation="vertical"]:not([value-label="none"]) RangeSliderHandle:active > RangeSliderThumb { left: -3px; width: 28px; height: 10px; bottom: calc(var(--nz-range-thumb-position) * 100% - 5px); }
Slider[orientation="vertical"][value-label="none"]:hover > SliderThumb, RangeSlider[orientation="vertical"][value-label="none"] RangeSliderHandle:hover > RangeSliderThumb { height: 6px; top: auto; transform: scale(1.167, 1); }
Slider[orientation="vertical"][value-label="none"]:active > SliderThumb, RangeSlider[orientation="vertical"][value-label="none"] RangeSliderHandle:active > RangeSliderThumb { width: 24px; transform: scale(1.167, 1.667); }

/* Chips: pane leaves; selected chips grow a seed of their color before the label. */
ChipGroup { gap: 10px; }
/* Chips: pane leaves; a selected chip takes a tint of its color (--leaf-chip, the seed by default) and grows a seed of
   that color before the label. */
Chip { --leaf-chip: var(--leaf-primary); }
Chip[intent="success"] { --leaf-chip: var(--leaf-ok); }
Chip[intent="warning"] { --leaf-chip: var(--leaf-warn); }
Chip[intent="danger"] { --leaf-chip: var(--leaf-err); }
Chip, ChipGroupOverflow {
    gap: 0; min-height: 32px; padding: 0 13px; border: 0; border-radius: 12px 3px 12px 3px;
    background: var(--leaf-pane); color: var(--leaf-text-2); box-shadow: inset 0 0 0 1px var(--leaf-stroke-2); font-size: 13px; font-weight: 500;
    transition: background-color 250ms, color 200ms, box-shadow 200ms, padding 300ms cubic-bezier(0.34, 1.5, 0.64, 1), column-gap 300ms;
}
Chip:hover, ChipGroupOverflow:hover { background: var(--leaf-hover); }
Chip:active, ChipGroupOverflow:active { background: var(--leaf-press); }
Chip[selected] { gap: 7px; padding-left: 10px; background: color-mix(in oklab, var(--leaf-chip) 16%, var(--leaf-pane)); color: var(--leaf-text); box-shadow: inset 0 0 0 1.5px color-mix(in srgb, var(--leaf-chip) 60%, transparent); }
Chip:disabled, ChipGroupOverflow:disabled { background: var(--leaf-s2); color: var(--leaf-text-3); box-shadow: none; }
Chip[intent="accent"], Chip[intent="success"], Chip[intent="warning"], Chip[intent="danger"] { border-color: transparent; }
ChipDot, Chip[intent="accent"] > ChipDot, Chip[intent="success"] > ChipDot, Chip[intent="warning"] > ChipDot, Chip[intent="danger"] > ChipDot {
    width: 0; height: 10px; border-radius: 6px 1px 6px 1px; background: var(--leaf-chip); transition: width 300ms cubic-bezier(0.34, 1.5, 0.64, 1);
}
Chip[selected] > ChipDot { width: 10px; }
/* The seed is the selection mark: the check is not drawn. */
ChipCheck { display: none; }
ChipRemove { width: 18px; height: 18px; border-radius: 7px 2px 7px 2px; color: var(--leaf-text-3); }
ChipRemove:hover { background: var(--leaf-hover); color: var(--leaf-text); }
ChipGroupOverflow { color: var(--leaf-ink); }
Chip[size="small"], ChipGroupOverflow[size="small"] { min-height: 26px; padding: 0 10px; font-size: 12px; }
Chip[size="large"], ChipGroupOverflow[size="large"] { min-height: 38px; padding: 0 16px; font-size: 14px; }

Rating { gap: 3px; }
RatingItemSymbol { color: var(--leaf-s3); }
RatingItemFill { color: var(--leaf-g3); }
Rating[previewing] > RatingItem > RatingItemFill { color: color-mix(in srgb, var(--leaf-g3) 70%, var(--leaf-text)); }
Rating[invalid] > RatingItem > RatingItemSymbol { color: var(--leaf-err); }
DialTrack { color: var(--leaf-s3); }
DialFill { color: var(--leaf-primary); }
DialTick { border-top-color: var(--leaf-stroke-2); }
DialPointer { border: 0; background: var(--leaf-pane); box-shadow: 0 1px 3px rgba(0, 0, 0, 0.16), inset 0 0 0 1px var(--leaf-stroke); }
DialPointerHandle { width: 4px; height: 9px; border-radius: 3px 1px 3px 1px; background: var(--leaf-primary); }
Dial:hover > DialPointer { box-shadow: 0 1px 3px rgba(0, 0, 0, 0.16), inset 0 0 0 1.6px var(--leaf-ink); }
Dial[dragging] > DialPointer { box-shadow: 0 0 0 4px var(--leaf-focus-ring), inset 0 0 0 1px var(--leaf-stroke); }
Dial:focus-visible > DialPointer { outline-style: none; box-shadow: 0 0 0 4px var(--leaf-focus-ring), inset 0 0 0 1.6px var(--leaf-ink); }
Dial:disabled > DialPointer { box-shadow: 0 1px 3px rgba(0, 0, 0, 0.16), inset 0 0 0 1px var(--leaf-stroke); }
@media (forced-colors: active) { Dial:focus-visible > DialPointer { box-shadow: none; outline: 2px solid CanvasText; outline-offset: 2px; } }
DialValue { font-family: var(--leaf-font-mono); color: var(--leaf-text-2); }

/* ---- Group: Pickers (pickers) ---- */
Calendar { gap: 6px; color: var(--leaf-text); }
CalendarPrevious, CalendarNext { width: 30px; height: 30px; padding: 9px; border: 0; border-radius: var(--leaf-r-ctl); color: var(--leaf-text-2); stroke-width: 1.7px; }
CalendarPrevious:hover, CalendarNext:hover, CalendarTitle:hover { background: var(--leaf-hover); }
CalendarTitle { padding: 4px 8px; border: 0; border-radius: var(--leaf-r-ctl); font-family: var(--leaf-font-display); font-size: 15px; font-weight: 700; letter-spacing: -0.01em; }
CalendarTitle:disabled { background: transparent; }
CalendarMonthTitle { font-weight: 650; }
CalendarWeekday, CalendarWeekHeader { width: 34px; color: var(--leaf-text-3); font-size: 12px; font-weight: 550; }
CalendarWeekNumber { width: 34px; height: 34px; color: var(--leaf-text-3); font-family: var(--leaf-font-mono); font-size: 11px; }
CalendarDay { width: 34px; height: 34px; border: 0; border-radius: 12px 3px 12px 3px; font-size: 13px; transition: background-color 150ms; }
CalendarItem { border: 0; border-radius: var(--leaf-r-item); }
CalendarDay:hover, CalendarItem:hover { background: var(--leaf-hover); }
CalendarDay[outside], CalendarItem[outside] { color: var(--leaf-text-3); }
CalendarDay[today], CalendarItem[today] { font-weight: 700; color: var(--leaf-ink); box-shadow: inset 0 0 0 1.5px color-mix(in srgb, var(--leaf-primary) 45%, transparent); }
CalendarDay[range-preview], CalendarDay[range-middle] { border-radius: 0; background: color-mix(in srgb, var(--leaf-primary) 11%, transparent); color: var(--leaf-text); }
CalendarDay[selected], CalendarDay[range-start], CalendarDay[range-end], CalendarItem[selected] { background: var(--leaf-primary); color: var(--leaf-on-primary); box-shadow: var(--leaf-glow); }
CalendarDay[range-start] { border-radius: 12px 0 0 3px; }
CalendarDay[range-end] { border-radius: 0 3px 12px 0; }
CalendarDay[unavailable], CalendarItem[unavailable] { color: var(--leaf-text-3); }
DateRangePickerSeparator { color: var(--leaf-text-3); }
DatePickerButton, TimePickerButton, DateRangePickerButton { width: 26px; height: 26px; padding: 6px; margin-right: -6px; border: 0; border-radius: var(--leaf-r-ctl); color: var(--leaf-text-2); stroke-width: 1.7px; }
DatePickerButton:hover, TimePickerButton:hover, DateRangePickerButton:hover, DatePickerButton[expanded], TimePickerButton[expanded], DateRangePickerButton[expanded] { background: var(--leaf-hover); color: var(--leaf-text); }
DatePickerPopup, TimePickerPopup, DateRangePickerPopup { padding: 10px; gap: 8px; }
DatePickerFooter { padding-top: 8px; border-top: 1px solid var(--leaf-stroke); }
DatePickerToday, DateRangePickerPreset { min-height: 30px; padding: 0 12px; border: 0; border-radius: var(--leaf-r-ctl); color: var(--leaf-ink); font-weight: 550; }
DatePickerToday:hover { background: var(--leaf-hover); }
DateRangePickerPresets { padding-right: 10px; border-right: 1px solid var(--leaf-stroke); }
DateRangePickerPreset { color: var(--leaf-text); font-weight: 400; }
TimePickerColumn, TimePickerList { padding: 4px; gap: 2px; border: 0; border-radius: var(--leaf-r-ovl); background: var(--leaf-s1); }
TimePickerOption { display: flex; align-items: center; min-height: 30px; justify-content: center; font-family: var(--leaf-font-mono); font-size: 13px; }
TimePickerOption[selected]::before { left: 3px; }

/* ---- Group: Collections (collections) ---- */
ListView, TreeView, GridView { padding: 5px; border: 0; border-radius: var(--leaf-r-ovl); background: var(--leaf-s1); color: var(--leaf-text); }
ListViewItem, TreeViewItem, GridViewItem { border-radius: var(--leaf-r-item); transition: background-color 150ms; }
ListViewItem { gap: 12px; padding: 0 12px; }
ListViewItem:hover, TreeViewItem:hover, GridViewItem:hover { background: var(--leaf-hover); color: var(--leaf-text); }
ListViewItem[selected], TreeViewItem[selected], GridViewItem[selected] { background: var(--leaf-pc); color: var(--leaf-on-pc); }
ListViewItem[current], TreeViewItem[current], GridViewItem[current] { outline-style: none; }
ListViewItem:focus-visible, TreeViewItem:focus-visible, GridViewItem:focus-visible { outline: 2px solid var(--leaf-ink); outline-offset: -2px; }
ListViewItem[selected]:focus-visible, TreeViewItem[selected]:focus-visible, GridViewItem[selected]:focus-visible { outline-color: var(--leaf-on-pc); }
ListViewItem:disabled, TreeViewItem:disabled, GridViewItem:disabled { color: var(--leaf-text-3); }
ListViewItemCheck, TreeViewItemCheck, GridViewItemCheck { width: 18px; height: 18px; border: 1.6px solid var(--leaf-text-3); border-radius: 7px 2px 7px 2px; background: var(--leaf-ctl); color: var(--leaf-on-primary); stroke-width: 2.2px; }
ListViewItemCheck[checked], TreeViewItemCheck[checked], TreeViewItemCheck[indeterminate], GridViewItemCheck[checked] { background: var(--leaf-primary); border-color: var(--leaf-primary); color: var(--leaf-on-primary); }
ListViewSectionHeader { border-bottom: 0; }
ListViewLoadingMore { border-top: 1px solid var(--leaf-stroke); color: var(--leaf-text-3); }
TreeViewItem { gap: 6px; }
TreeViewExpander { width: 14px; height: 14px; color: var(--leaf-text-3); stroke-width: 1.7px; }
TreeViewIndentGuide { background: var(--leaf-stroke); }
GridViewItem { padding: 12px 8px; border: 0; border-radius: var(--leaf-r-ovl); }
GridViewItem[selected] { background: color-mix(in oklab, var(--leaf-primary) 12%, transparent); color: var(--leaf-text); box-shadow: inset 0 0 0 1.5px color-mix(in srgb, var(--leaf-primary) 45%, transparent); }
/* With check boxes the tile keeps a band above its content for the smaller leaf check, so the two never overlap. */
GridViewItem:has(GridViewItemCheck) { padding: 24px 8px 6px; }
GridViewItemCheck { top: 6px; left: 6px; width: 16px; height: 16px; border-radius: 6px 2px 6px 2px; }

/* Action rows: leaf rows on a boxed layer. */
ActionRow { gap: 14px; min-height: calc(var(--leaf-h-row) + 12px); padding: 8px 12px; border-radius: var(--leaf-r-item); background: transparent; color: var(--leaf-text); transition: background-color 200ms; }
ActionRow[clickable]:hover { background: var(--leaf-hover); color: var(--leaf-text); }
ActionRow[clickable]:active { background: var(--leaf-press); color: var(--leaf-text); }
ActionRow[position="middle"], ActionRow[position="last"] { border-top: 0; }
ActionRowTitle { font-weight: 550; }
ActionRowSubtitle { color: var(--leaf-text-2); font-size: 12.5px; }
ActionRowTrailing { color: var(--leaf-text-3); font-size: 13px; }
ActionRowChevron { width: 14px; height: 14px; color: var(--leaf-text-3); stroke-width: 1.7px; }
RowGroup { gap: 8px; }
RowGroupTitle { font-family: var(--leaf-font-display); font-size: 15px; font-weight: 700; letter-spacing: -0.01em; }
RowGroupDescription { color: var(--leaf-text-3); font-size: 12.5px; }
RowGroupList { padding: 5px; border: 0; border-radius: var(--leaf-r-ovl); background: var(--leaf-s1); }

/* Table: rows are leaves separated by air; headers are small and muted. */
TableCaption { padding: 0 0 8px; font-family: var(--leaf-font-display); font-size: 15px; font-weight: 700; letter-spacing: -0.01em; }
TableCell, TableHeaderCell { padding: 9px 12px; border-bottom: 1px solid var(--leaf-stroke); }
TableHeaderCell { color: var(--leaf-text-3); font-size: 12px; font-weight: 550; }
TableHeader > TableRow > TableHeaderCell, TableHeader > TableRow > TableCell { border-bottom-color: var(--leaf-stroke-2); }
TableFooter > TableRow > TableCell, TableFooter > TableRow > TableHeaderCell { border-top: 1px solid var(--leaf-stroke-2); font-weight: 650; }
Table[bordered] { border-top-color: var(--leaf-stroke); border-left-color: var(--leaf-stroke); }
Table[bordered] TableCell, Table[bordered] TableHeaderCell { border-right-color: var(--leaf-stroke); border-bottom-color: var(--leaf-stroke); }
Table[striped] TableBody > TableRow[alternate] > TableCell, Table[striped] TableBody > TableRow[alternate] > TableHeaderCell { background: var(--leaf-s1); }

Pagination { gap: 4px; }
PaginationFirst, PaginationPrevious, PaginationPage, PaginationNext, PaginationLast {
    min-width: 32px; min-height: 32px; padding: 0 8px; border: 0; border-radius: var(--leaf-r-ctl); background: transparent; color: var(--leaf-text-2);
    transition: background-color 150ms;
}
PaginationFirst:hover, PaginationPrevious:hover, PaginationPage:hover, PaginationNext:hover, PaginationLast:hover { background: var(--leaf-hover); color: var(--leaf-text); }
PaginationFirst:active, PaginationPrevious:active, PaginationPage:active, PaginationNext:active, PaginationLast:active { background: var(--leaf-press); color: var(--leaf-text); }
PaginationPage[current] { background: var(--leaf-primary); color: var(--leaf-on-primary); box-shadow: var(--leaf-glow); }
PaginationFirst:disabled, PaginationPrevious:disabled, PaginationNext:disabled, PaginationLast:disabled { background: transparent; color: var(--leaf-text-3); }
Pagination:disabled > PaginationPage[current] { background: var(--leaf-s2); color: var(--leaf-text-3); box-shadow: none; }
PaginationGlyph { stroke-width: 1.7px; }
PaginationEllipsis, PaginationSummary, PaginationPageSizeLabel { color: var(--leaf-text-3); }

/* ---- Group: Data grids (data) ---- */
DataGrid { border: 0; border-radius: var(--leaf-r-ovl); background: var(--leaf-pane); color: var(--leaf-text); box-shadow: inset 0 0 0 1px var(--leaf-stroke); }
DataGridHeader, DataGridFooter { background: transparent; color: var(--leaf-text-3); }
DataGridHeader { border-bottom: 0; }
DataGridFooter { border-top: 1px solid var(--leaf-stroke); }
DataGridColumnHeader, DataGridSelectionHeader, DataGridDetailsHeader, DataGridFooterCell { min-height: 36px; background: transparent; color: var(--leaf-text-3); }
DataGridColumnHeader { padding-left: 12px; font-size: 12px; font-weight: 550; }
DataGridColumnHeader[sortable]:hover { background: transparent; color: var(--leaf-text); }
DataGridColumnHeader[sorted] { color: var(--leaf-ink); }
DataGridColumnHeader[drop-target="before"] { border-left: 2px solid var(--leaf-primary); }
DataGridColumnHeader[drop-target="after"] { border-right: 2px solid var(--leaf-primary); }
DataGridSortMark { width: 10px; height: 10px; color: var(--leaf-ink); stroke-width: 1.7px; }
DataGridSortIndex { color: var(--leaf-text-3); font-family: var(--leaf-font-mono); }
DataGridBody { padding: 0 6px 6px; }
DataGrid { --nz-data-row-gap: 2px; }
DataGridRow, DataGridGroupHeader { min-height: 0; margin-top: min(2px, var(--nz-data-row-height, 32px)); border-radius: 14px 3px 14px 3px; }
DataGridRow[alternate] { background: transparent; }
DataGridRow:hover { background: var(--leaf-hover); }
DataGridRow[selected] { background: color-mix(in oklab, var(--leaf-primary) 11%, transparent); color: var(--leaf-text); }
DataGridCell, DataGridSelectionCell, DataGridDetailsCell { padding: 0 12px; font-size: 13.5px; }
DataGridSelectionCell, DataGridDetailsCell { padding: 0; }
DataGridCell:focus-visible, DataGridSelectionCell:focus-visible, DataGridDetailsCell:focus-visible, DataGridGroupHeaderCell:focus-visible, DataGridColumnHeader:focus-visible,
DataGridSelectionHeader:focus-visible, DataGridDetailsHeader:focus-visible { outline: 2px solid var(--leaf-ink); outline-offset: -2px; }
DataGridRow[selected] > DataGridCell:focus-visible, DataGridRow[selected] > DataGridSelectionCell:focus-visible, DataGridRow[selected] > DataGridDetailsCell:focus-visible { outline-color: var(--leaf-ink); }
DataGridCellEditor { padding: 2px 6px; border: 0; border-radius: var(--leaf-r-xs); background: var(--leaf-pane); box-shadow: inset 0 0 0 1.6px var(--leaf-ink); }
DataGridCellEditor[invalid] { box-shadow: inset 0 0 0 1.6px var(--leaf-err); }
DataGridDetailsToggle, DataGridGroupToggle { width: 12px; height: 12px; color: var(--leaf-text-3); stroke-width: 1.7px; }
DataGridRowDetails { padding: 12px 18px; border-bottom: 0; background: var(--leaf-s1); border-radius: var(--leaf-r-item); color: var(--leaf-text-2); }
DataGridGroupHeader { background: var(--leaf-s1); color: var(--leaf-text); }
DataGridGroupHeaderCell { font-weight: 650; }
DataGridGroupCount { color: var(--leaf-text-3); }
DataGridFooterCell { padding: 0 12px; font-weight: 650; color: var(--leaf-text); }
DataGridRow > [frozen] { background: var(--leaf-pane); }
DataGridRow[alternate] > [frozen] { background: var(--leaf-pane); }
DataGridRow[selected] > [frozen] { background: color-mix(in srgb, var(--leaf-primary) 11%, var(--leaf-pane)); }
/* Rows are separated by air, so frozen columns need no divider either. */
[frozen-last] { border-right: 0; }
DataGrid[gridlines="horizontal"] DataGridRow > *, DataGrid[gridlines="both"] DataGridRow > *, DataGrid[gridlines="horizontal"] DataGridGroupHeader, DataGrid[gridlines="both"] DataGridGroupHeader { border-bottom-color: var(--leaf-stroke); }
DataGrid[gridlines="vertical"] DataGridRow > *, DataGrid[gridlines="both"] DataGridRow > *, DataGrid[gridlines="vertical"] DataGridHeaderRow > *, DataGrid[gridlines="both"] DataGridHeaderRow > *,
DataGrid[gridlines="vertical"] DataGridFooterRow > *, DataGrid[gridlines="both"] DataGridFooterRow > * { border-right-color: var(--leaf-stroke); }
DataGridLoading { border-top: 1px solid var(--leaf-stroke); background: var(--leaf-pane); color: var(--leaf-text-2); }

/* PropertyGrid: the order switch is a segmented leaf, the list a quiet card with leaf rows (.lrow) under small category headings. */
PropertyGrid { gap: 10px; }
PropertyGridToolbar { gap: 8px; }
PropertyGridOrder { gap: 2px; padding: 3px; border-radius: 12px 4px 12px 4px; background: var(--leaf-s2); }
PropertyGridOrderOption {
    display: flex; align-items: center; min-height: 28px; padding: 0 12px; border: 0; border-radius: 9px 3px 9px 3px; background: transparent;
    color: var(--leaf-text-2); font-size: 13px; font-weight: 550; cursor: default; transition: background-color 150ms, color 150ms;
}
PropertyGridOrderOption:hover { color: var(--leaf-text); }
PropertyGridOrderOption[checked] { background: var(--leaf-pane); color: var(--leaf-text); box-shadow: 0 1px 2px rgba(0, 0, 0, 0.08), inset 0 0 0 1px var(--leaf-stroke); }
PropertyGridList { padding: 5px; border: 0; border-radius: var(--leaf-r-ovl); background: var(--leaf-pane); color: var(--leaf-text); box-shadow: inset 0 0 0 1px var(--leaf-stroke); }
PropertyGridCategoryHeader { gap: 6px; min-height: 30px; padding: 0 8px; border-radius: var(--leaf-r-item); background: transparent; color: var(--leaf-text-3); font-size: 12px; font-weight: 600; }
PropertyGridCategoryHeader:hover { background: var(--leaf-hover); color: var(--leaf-text); }
PropertyGridCategoryToggle { width: 12px; height: 12px; color: var(--leaf-text-3); stroke-width: 1.7px; }
PropertyGridRow { column-gap: 12px; min-height: 40px; padding: 3px 6px 3px 26px; border-radius: var(--leaf-r-item); font-size: 13.5px; transition: background-color 150ms; }
PropertyGridRow:hover { background: var(--leaf-hover); }
PropertyGridRow[selected] { background: color-mix(in oklab, var(--leaf-primary) 11%, transparent); color: var(--leaf-text); }
PropertyGridName { color: var(--leaf-text-2); }
PropertyGridRow[selected] > PropertyGridName { color: var(--leaf-text); }
PropertyGridRow[modified] > PropertyGridName { color: var(--leaf-text); font-weight: 650; }
PropertyGridRow[invalid] > PropertyGridName { color: var(--leaf-err); }
PropertyGridValue TextField, PropertyGridValue NumberField, PropertyGridValue Select { min-height: 30px; }
PropertyGridReadOnlyValue { color: var(--leaf-text-3); }
PropertyGridEmpty { padding: 12px; color: var(--leaf-text-3); }
PropertyGridDescription { gap: 4px; padding: 12px 14px; border: 0; border-radius: var(--leaf-r-ovl); background: var(--leaf-s1); box-shadow: inset 0 0 0 1px var(--leaf-stroke); }
PropertyGridDescriptionTitle { font-weight: 650; }
PropertyGridDescriptionText { color: var(--leaf-text-2); font-size: 12.5px; }

/* ---- Group: Navigation (navigation) ---- */
/* Tabs: a recessed leaf strip; the pane-colored indicator slides to the chosen tab and carries the seed below. */
TabsHeader { border-bottom: 0; }
Tabs[placement="bottom"] > TabsHeader, Tabs[placement="start"] > TabsHeader, Tabs[placement="end"] > TabsHeader { border-width: 0; }
TabsList { gap: 2px; padding: 4px; border-radius: var(--leaf-r-ovl); background: var(--leaf-s1); }
Tab {
    position: relative; z-index: 1; min-height: calc(var(--leaf-h) + 2px); padding: 0 16px; border-radius: var(--leaf-r-ctl);
    background: transparent; color: var(--leaf-text-2); font-weight: 550; transition: color 200ms;
}
Tab:hover { background: transparent; color: var(--leaf-text); }
Tab[selected] { background: transparent; color: var(--leaf-text); }
Tab:disabled { color: var(--leaf-text-3); }
Tabs[size="small"] Tab { min-height: 28px; padding: 0 11px; font-size: 13px; }
Tabs[size="large"] Tab { min-height: 42px; padding: 0 20px; font-size: 15px; }
TabBadge { padding: 0 6px; border: 0; border-radius: 7px 2px 7px 2px; background: var(--leaf-pc); color: var(--leaf-on-pc); font-size: 11px; font-weight: 650; }
TabClose { width: 18px; height: 18px; padding: 4px; border-radius: 7px 2px 7px 2px; }
TabClose:hover { background: var(--leaf-hover); color: var(--leaf-text); }
TabsIndicator, Tabs[placement="bottom"] TabsIndicator {
    z-index: 0; top: var(--nz-indicator-y, 0px); height: var(--nz-indicator-height, 0px); border-radius: var(--leaf-r-ctl);
    background: var(--leaf-pane);
    box-shadow: 0 2px 6px -2px rgba(0, 0, 0, 0.18), inset 0 0 0 1px var(--leaf-stroke);
    transition: left 500ms cubic-bezier(0.16, 1, 0.3, 1), top 500ms cubic-bezier(0.16, 1, 0.3, 1), width 500ms cubic-bezier(0.16, 1, 0.3, 1), height 500ms cubic-bezier(0.16, 1, 0.3, 1);
}
/* The seed under the chosen tab, as .tab-ind::after. */
TabsIndicator::after {
    content: ""; position: absolute; left: 50%; bottom: -1px; width: 14px; height: 3px; margin-left: -7px;
    border-radius: 3px 1px 3px 1px; background: var(--leaf-primary);
}
TabsList[orientation="vertical"] > TabsIndicator, Tabs[placement="end"] TabsList[orientation="vertical"] > TabsIndicator {
    left: var(--nz-indicator-x, 0px); width: var(--nz-indicator-width, 0px);
}
TabsList[orientation="vertical"] > TabsIndicator::after {
    left: -1px; top: 50%; bottom: auto; width: 3px; height: 14px; margin-left: 0; margin-top: -7px; border-radius: 3px 1px 3px 1px;
}
TabsScrollBack, TabsScrollForward { color: var(--leaf-text-2); stroke-width: 1.7px; border-radius: var(--leaf-r-xs); }
TabsScrollBack:hover, TabsScrollForward:hover { background: var(--leaf-hover); }
TabsOverflow { padding: 0 10px; border-radius: var(--leaf-r-ctl); }
TabsOverflow:hover, TabsOverflow[expanded] { background: var(--leaf-hover); color: var(--leaf-text); }
TabsPanel { padding: 18px 2px 4px; animation: leaf-rise 450ms cubic-bezier(0.16, 1, 0.3, 1) backwards; }

/* ==== NazorLeaf components ==== */

/* LeafMark: the four-leaf mark in two tones of the seed. */
LeafMark {
    --leaf-mk1: var(--leaf-primary);
    --leaf-mk2: color-mix(in oklab, var(--leaf-primary) 42%, var(--leaf-pane));
    display: grid; grid-template-columns: 1fr 1fr; grid-template-rows: 1fr 1fr;
    gap: calc(var(--leaf-mark-size, 28px) * 0.08);
    width: var(--leaf-mark-size, 28px); height: var(--leaf-mark-size, 28px); flex-shrink: 0;
}
LeafMarkPetal, SpinnerPetal { display: block; min-width: 0; min-height: 0; }
LeafMarkPetal[petal="1"], LeafMarkPetal[petal="4"], SpinnerPetal[petal="1"], SpinnerPetal[petal="4"] { background: var(--leaf-mk1); border-radius: 8% 70% 8% 70%; }
LeafMarkPetal[petal="2"], LeafMarkPetal[petal="3"], SpinnerPetal[petal="2"], SpinnerPetal[petal="3"] { background: var(--leaf-mk2); border-radius: 70% 8% 70% 8%; }
Toast LeafMark { --leaf-mk1: var(--leaf-on-inverse); --leaf-mk2: color-mix(in oklab, var(--leaf-on-inverse) 45%, var(--leaf-inverse)); }
/* Loading: the leaves light up in turn while the mark slowly turns. */
LeafMark[loading] { animation: leaf-turn 3.2s linear infinite; }
LeafMark[loading] > LeafMarkPetal, Spinner[indeterminate] SpinnerPetal {
    background: var(--leaf-mk1); opacity: 0.22; transform: scale(0.82); animation: leaf-lit 1.2s cubic-bezier(0.2, 0, 0, 1) infinite;
}
LeafMark[loading] > LeafMarkPetal[petal="2"], Spinner[indeterminate] SpinnerPetal[petal="2"] { animation: leaf-lit 1.2s cubic-bezier(0.2, 0, 0, 1) 300ms infinite; }
LeafMark[loading] > LeafMarkPetal[petal="4"], Spinner[indeterminate] SpinnerPetal[petal="4"] { animation: leaf-lit 1.2s cubic-bezier(0.2, 0, 0, 1) 600ms infinite; }
LeafMark[loading] > LeafMarkPetal[petal="3"], Spinner[indeterminate] SpinnerPetal[petal="3"] { animation: leaf-lit 1.2s cubic-bezier(0.2, 0, 0, 1) 900ms infinite; }

/* LeafWindowButtons: three small leaves that take the colors of the mark while the pointer rests on the group. */
LeafWindowButtons { display: flex; align-items: center; align-self: center; gap: 1px; flex-shrink: 0; }
LeafWindowButton { display: flex; align-items: center; justify-content: center; width: 26px; height: 26px; cursor: default; }
LeafWindowButtonLeaf {
    display: flex; align-items: center; justify-content: center; width: 16px; height: 16px; border-radius: 8px 2px 8px 2px;
    background: var(--leaf-s3); color: var(--leaf-text-2);
    transform: scale(1);
    transition: background-color 250ms, color 200ms, transform 350ms cubic-bezier(0.34, 1.5, 0.64, 1);
}
LeafWindowButtonGlyph { position: relative; width: 7px; height: 7px; flex-shrink: 0; }
LeafWindowButtonStroke { position: absolute; display: block; left: -1px; top: 2.7px; width: 9px; height: 1.6px; border-radius: 1px; background: currentColor; }
LeafWindowButtonStroke[stroke="rising"] { transform: rotate(-45deg); }
LeafWindowButtonStroke[stroke="falling"] { transform: rotate(45deg); }
LeafWindowButtonStroke[stroke="bar"] { left: 0.5px; width: 6px; }
LeafWindowButtonStroke[stroke="corner"] { width: 4px; height: 4px; border-radius: 0; background: transparent; }
LeafWindowButtonStroke[stroke="corner"][at="top-end"] { left: auto; right: -0.5px; top: -0.5px; border-top: 1.6px solid currentColor; border-right: 1.6px solid currentColor; }
LeafWindowButtonStroke[stroke="corner"][at="bottom-start"] { left: -0.5px; top: auto; bottom: -0.5px; border-bottom: 1.6px solid currentColor; border-left: 1.6px solid currentColor; }
LeafWindowButtons:hover > LeafWindowButton > LeafWindowButtonLeaf { color: rgba(0, 0, 0, 0.7); }
LeafWindowButtons:hover > LeafWindowButton[kind="minimize"] > LeafWindowButtonLeaf { background: var(--leaf-g3); }
LeafWindowButtons:hover > LeafWindowButton[kind="maximize"] > LeafWindowButtonLeaf, LeafWindowButtons:hover > LeafWindowButton[kind="restore"] > LeafWindowButtonLeaf { background: var(--leaf-g4); }
LeafWindowButtons:hover > LeafWindowButton[kind="close"] > LeafWindowButtonLeaf { background: var(--leaf-g2); }
LeafWindowButton:hover > LeafWindowButtonLeaf { transform: scale(1.12); }
LeafWindowButton:active > LeafWindowButtonLeaf { transform: scale(0.9); }
LeafWindowButton:disabled > LeafWindowButtonLeaf { background: var(--leaf-s2); color: var(--leaf-text-3); transform: scale(1); }
LeafWindowButton:focus-visible { outline-offset: -2px; }
LeafWindowButtons[side="left"] { margin-right: 10px; flex-direction: row; }
TitleBar[height="tall"] LeafWindowButton { width: 32px; height: 32px; }
TitleBar[height="tall"] LeafWindowButtonLeaf { width: 22px; height: 22px; border-radius: 11px 3px 11px 3px; }
TitleBar[height="tall"] LeafWindowButtonGlyph { width: 9px; height: 9px; }
TitleBar[height="tall"] LeafWindowButtonStroke { top: 3.8px; width: 11px; height: 1.4px; }
TitleBar[height="tall"] LeafWindowButtonStroke[stroke="bar"] { left: 1px; width: 7px; }
TitleBar[height="tall"] LeafWindowButtonStroke[stroke="corner"] { width: 5px; height: 5px; border-width: 1.4px; }
@media (forced-colors: active) {
    LeafWindowButtons:hover > LeafWindowButton > LeafWindowButtonLeaf { background: ButtonFace; color: ButtonText; }
    LeafWindowButton:hover > LeafWindowButtonLeaf { background: Highlight; color: HighlightText; }
}

/* ==== Motion ==== */
@keyframes leaf-unfurl {
    from { opacity: 0; clip-path: inset(0 100% 100% 0); }
    to { opacity: 1; clip-path: inset(-60px); }
}
@keyframes leaf-dialog {
    from { opacity: 0; transform: translateY(16px) scale(0.96); }
    to { opacity: 1; transform: translateY(0px) scale(1); }
}
@keyframes leaf-tip {
    from { opacity: 0; transform: translateY(4px) scale(0.96); }
    to { opacity: 1; transform: translateY(0px) scale(1); }
}
@keyframes leaf-toast {
    from { opacity: 0; transform: translateY(60px) rotate(-3deg); }
    to { opacity: 1; transform: translateY(0px) rotate(0deg); }
}
@keyframes leaf-toast-countdown {
    from { transform: scaleX(var(--nz-toast-countdown-start)); }
    to { transform: scaleX(0); }
}
@keyframes leaf-rise {
    from { opacity: 0; transform: translateY(8px); }
    to { opacity: 1; transform: translateY(0px); }
}
@keyframes leaf-fade {
    from { opacity: 0; }
    to { opacity: 1; }
}
@keyframes leaf-sweep {
    from { transform: translateX(var(--leaf-sweep-start)); }
    to { transform: translateX(var(--leaf-sweep-end)); }
}
@keyframes leaf-pulse {
    0% { opacity: 1; }
    50% { opacity: 0.45; }
    100% { opacity: 1; }
}
@keyframes leaf-turn {
    from { transform: rotate(0deg); }
    to { transform: rotate(360deg); }
}
@keyframes leaf-seed-in {
    from { transform: scale(0) rotate(-90deg); }
    to { transform: scale(1) rotate(0deg); }
}
@keyframes leaf-badge-in {
    from { transform: scale(0); }
    to { transform: scale(1); }
}
@keyframes leaf-icon-pop {
    from { transform: scale(0.4) rotate(-30deg); }
    to { transform: scale(1) rotate(0deg); }
}
@keyframes leaf-lit {
    0% { opacity: 0.22; transform: scale(0.82); }
    25% { opacity: 1; transform: scale(1); }
    100% { opacity: 0.22; transform: scale(0.82); }
}

@media (prefers-reduced-motion: reduce) {
    * { transition: none; animation: none; }
}

@media (forced-colors: active) {
    Button, Button[default], Button[appearance="accent"], Button[default]:hover, Button[appearance="accent"]:hover, ToggleButton, MenuButton, SplitButton, FloatingActionButton,
    TextField, TextArea, Select, Combobox, DatePicker, TimePicker, PaginationPage[current], CalendarDay[selected], SliderThumb, RangeSliderThumb { box-shadow: none; }
    Button, ToggleButton, MenuButton, TextField, TextArea, Select, Combobox { border: 1px solid ButtonText; }
    TextField:focus-within, TextArea:focus-within, Select[expanded] { box-shadow: none; border-color: Highlight; }
}

SegmentedDatePicker, SegmentedTimePicker { min-height: 34px; border: 0; border-radius: var(--leaf-r-ctl); background: var(--leaf-ctl); color: var(--leaf-text); box-shadow: inset 0 0 0 1px var(--leaf-stroke); }
SegmentedDatePicker:focus-within, SegmentedTimePicker:focus-within { box-shadow: inset 0 0 0 1.5px var(--leaf-ink), 0 0 0 4px var(--leaf-focus-ring); }
SegmentedDatePicker[expanded]:focus-within, SegmentedTimePicker[expanded]:focus-within { box-shadow: inset 0 0 0 1.5px var(--leaf-ink); }
SegmentedDatePicker:focus-visible, SegmentedTimePicker:focus-visible { outline-style: none; }
SegmentedDatePicker[invalid], SegmentedTimePicker[invalid] { box-shadow: inset 0 0 0 1.5px var(--leaf-err); }
SegmentedDatePicker:disabled, SegmentedTimePicker:disabled { color: var(--leaf-text-3); opacity: 0.45; }
SegmentedPickerSegment { border-left-color: var(--leaf-stroke); }
SegmentedPickerSegment[placeholder] { color: var(--leaf-text-3); }
SegmentedPickerSegment:hover { background: var(--leaf-s2); }
SegmentedDatePickerPopup, SegmentedTimePickerPopup { background: var(--leaf-pane); color: var(--leaf-text); border-color: var(--leaf-stroke); border-radius: var(--leaf-r-ovl); box-shadow: var(--leaf-sh-pop); }
SegmentedPickerColumn, SegmentedPickerActions { border-color: var(--leaf-stroke); }
SegmentedPickerOption:hover { background: var(--leaf-s2); }
SegmentedPickerOption[selected] { background: var(--leaf-primary); color: var(--leaf-on-primary); }
SegmentedPickerConfirm, SegmentedPickerCancel { border-radius: var(--leaf-r-ctl); }
SegmentedPickerConfirm:hover, SegmentedPickerCancel:hover { background: var(--leaf-s2); }
@media (forced-colors: active) {
    SegmentedDatePicker:focus-visible, SegmentedTimePicker:focus-visible { outline: 2px solid Highlight; }
}
:root { --leaf-fs: 14px; font-size: var(--leaf-fs); }
@media (density: touch) {
    :root { --leaf-h: 42px; --leaf-h-menu: 42px; --leaf-h-row: 54px; --leaf-fs: 15px; }
    CheckBox, RadioButton, Switch { min-height: var(--leaf-h); }
    MenuBarItem { min-height: var(--leaf-h-menu); }
    ListViewRowMetric, TreeViewRowMetric { height: var(--leaf-h-row); }
    TableBody > TableRow > TableCell, TableBody > TableRow > TableHeaderCell { min-height: var(--leaf-h-row); }
}
Button, ToggleButton, MenuButton { position: relative; overflow-x: hidden; overflow-y: hidden; }
ButtonLeading, ButtonLabel, ButtonTrailing, ButtonSpinner { position: relative; z-index: 1; }
Button[press-effect]::after, ToggleButton[press-effect]::after, MenuButton[press-effect]::after {
    content: ""; position: absolute; inset: -2px; z-index: 0; pointer-events: none; opacity: 0;
    background: radial-gradient(circle at var(--nz-press-x) var(--nz-press-y), color-mix(in srgb, var(--leaf-primary) 30%, transparent), transparent 45%);
}
Button[appearance="accent"][press-effect]::after, ToggleButton[appearance="accent"][press-effect]::after,
MenuButton[appearance="accent"][press-effect]::after, Button[intent="danger"][press-effect]::after {
    background: radial-gradient(circle at var(--nz-press-x) var(--nz-press-y), rgba(255, 255, 255, 0.45), transparent 45%);
}
Button[press-effect][press-replay="a"]::after, ToggleButton[press-effect][press-replay="a"]::after, MenuButton[press-effect][press-replay="a"]::after { animation: leaf-button-sheen-a 600ms cubic-bezier(0.2, 0, 0, 1); }
Button[press-effect][press-replay="b"]::after, ToggleButton[press-effect][press-replay="b"]::after, MenuButton[press-effect][press-replay="b"]::after { animation: leaf-button-sheen-b 600ms cubic-bezier(0.2, 0, 0, 1); }
@keyframes leaf-button-sheen-a { 0% { opacity: 1; transform: scale(0.4); } 100% { opacity: 0; transform: scale(1.6); } }
@keyframes leaf-button-sheen-b { 0% { opacity: 1; transform: scale(0.4); } 100% { opacity: 0; transform: scale(1.6); } }
@media (prefers-reduced-motion: reduce), (forced-colors: active) {
    Button[press-effect]::after, ToggleButton[press-effect]::after, MenuButton[press-effect]::after { content: none; animation: none; }
}
MenuBar[hover-highlight], RowGroupList[hover-highlight], DataGridBody[hover-highlight], ListViewViewport[hover-highlight], .demo-tiles[hover-highlight] { position: relative; }
[hover-highlight] > HoverIndicator {
    display: block; position: absolute; left: 0; top: 0;
    width: var(--nz-indicator-width); height: var(--nz-indicator-height);
    transform: translate(var(--nz-indicator-x), var(--nz-indicator-y));
    border-radius: 12px 3px 12px 3px; background: var(--leaf-hover);
    opacity: 0; pointer-events: none; z-index: 0; transition: opacity 200ms;
}
[hover-highlight] > HoverIndicator[hidden] { display: none; }
[hover-highlight][hover-active] > HoverIndicator { opacity: 1; }
[hover-highlight][hover-moving] > HoverIndicator { transition: transform 320ms cubic-bezier(0.16, 1, 0.3, 1), width 320ms cubic-bezier(0.16, 1, 0.3, 1), height 320ms cubic-bezier(0.16, 1, 0.3, 1), opacity 200ms; }
Menu[hover-highlight] MenuItem, Menu[hover-highlight] MenuCheckItem, Menu[hover-highlight] MenuRadioItem, Menu[hover-highlight] Submenu,
MenuBar[hover-highlight] > MenuBarItem, RowGroupList[hover-highlight] > ActionRow,
DataGridBody[hover-highlight] DataGridRow, DataGridBody[hover-highlight] DataGridGroupHeader,
ListViewViewport[hover-highlight] ListViewItem, .demo-tiles[hover-highlight] ShowcaseTile { position: relative; z-index: 1; }
@media (forced-colors: none) {
    Menu[hover-highlight] MenuItem:hover, Menu[hover-highlight] MenuCheckItem:hover, Menu[hover-highlight] MenuRadioItem:hover, Menu[hover-highlight] Submenu:hover,
    MenuBar[hover-highlight] > MenuBarItem:hover, RowGroupList[hover-highlight] > ActionRow[clickable]:hover,
    DataGridBody[hover-highlight] DataGridRow:hover, ListViewViewport[hover-highlight] ListViewItem:hover { background-color: transparent; }
}
DataGridBody[hover-highlight] > HoverIndicator { border-radius: 14px 3px 14px 3px; }
@media (forced-colors: none) {
    DataGridBody[hover-highlight][hover-active] DataGridRow:hover:not(:active):not([selected]) > [frozen] { background: color-mix(in srgb, var(--leaf-primary) var(--leaf-mixh), var(--leaf-pane)); }
    DataGridBody[hover-highlight][hover-active] DataGridRow[selected]:hover:not(:active) > [frozen] { background: color-mix(in srgb, var(--leaf-primary) var(--leaf-mixh), var(--leaf-pc)); }
}
@media (prefers-reduced-motion: reduce) { [hover-highlight] > HoverIndicator, [hover-highlight][hover-moving] > HoverIndicator { transition: none; } }
@media (forced-colors: active) { [hover-highlight] > HoverIndicator { display: none; } }
ExpanderPanelContent, AccordionItemPanelContent { animation: none; }
ExpanderPanelContent[expanded=true], AccordionItemPanelContent[expanded=true] { animation: leaf-rise 400ms cubic-bezier(0.16,1,0.3,1) backwards; }
Expander:has(> ExpanderHeader > ExpanderLeading) > ExpanderPanel > ExpanderPanelContent,
AccordionItem:has(> AccordionItemHeader > AccordionItemLeading) > AccordionItemPanel > AccordionItemPanelContent { padding-left: 50px; }
ListViewRowMetric, TreeViewRowMetric { height: var(--leaf-h-row); }
DataGridRowMetric { height: calc(var(--leaf-h-row) + 2px); }
ListViewItem, TreeViewItem, GridViewItem { transition: transform 320ms cubic-bezier(0.34, 1.56, 0.64, 1), background-color 200ms, color 200ms, border-radius 400ms cubic-bezier(0.34, 1.56, 0.64, 1); }
TreeViewExpander { transition: transform 450ms cubic-bezier(0.34, 1.56, 0.64, 1); }
Submenu { submenu-open-delay: 60ms; }
:is(ContextMenu, Menu, SelectPopup, ComboboxPopup, ComboBoxPopup, SearchFieldPopup, DatePickerPopup, TimePickerPopup, DateRangePickerPopup) {
    --leaf-menu-duration: 500ms;
    --leaf-menu-closed: inset(0 100% 100% 0 round 16px 4px 16px 4px);
    clip-path: inset(-60px round 60px); opacity: 1;
    overlay-exit-duration: 220ms;
    transition: clip-path var(--leaf-menu-duration) cubic-bezier(0.16, 1, 0.3, 1), opacity 120ms;
    animation: leaf-menu-reveal var(--leaf-menu-duration) cubic-bezier(0.16, 1, 0.3, 1) backwards, leaf-menu-opacity 120ms backwards;
}
Menu[submenu] { --leaf-menu-duration: 450ms; }
:is(ContextMenu, Menu, SelectPopup, ComboboxPopup, ComboBoxPopup, SearchFieldPopup, DatePickerPopup, TimePickerPopup, DateRangePickerPopup):popup-origin-right {
    --leaf-menu-closed: inset(0 0 100% 100% round 16px 4px 16px 4px);
}
:is(ContextMenu, Menu, SelectPopup, ComboboxPopup, ComboBoxPopup, SearchFieldPopup, DatePickerPopup, TimePickerPopup, DateRangePickerPopup):popup-origin-bottom {
    --leaf-menu-closed: inset(100% 100% 0 0 round 16px 4px 16px 4px);
}
:is(ContextMenu, Menu, SelectPopup, ComboboxPopup, ComboBoxPopup, SearchFieldPopup, DatePickerPopup, TimePickerPopup, DateRangePickerPopup):popup-origin-right:popup-origin-bottom {
    --leaf-menu-closed: inset(100% 0 0 100% round 16px 4px 16px 4px);
}
:is(ContextMenu, Menu, SelectPopup, ComboboxPopup, ComboBoxPopup, SearchFieldPopup, DatePickerPopup, TimePickerPopup, DateRangePickerPopup)[exiting] {
    animation: none; opacity: 0; clip-path: var(--leaf-menu-closed);
    transition: clip-path 220ms cubic-bezier(0.2, 0, 0, 1), opacity 180ms;
}
:is(ContextMenu, Menu, SelectPopup, ComboboxPopup, ComboBoxPopup, SearchFieldPopup, DatePickerPopup, TimePickerPopup, DateRangePickerPopup)[reentered] { animation: none; }
@keyframes leaf-menu-reveal { from { clip-path: var(--leaf-menu-closed); } to { clip-path: inset(-60px round 60px); } }
@keyframes leaf-menu-opacity { from { opacity: 0; } to { opacity: 1; } }
@media (prefers-reduced-motion: reduce) {
    :is(ContextMenu, Menu, SelectPopup, ComboboxPopup, ComboBoxPopup, SearchFieldPopup, DatePickerPopup, TimePickerPopup, DateRangePickerPopup),
    :is(ContextMenu, Menu, SelectPopup, ComboboxPopup, ComboBoxPopup, SearchFieldPopup, DatePickerPopup, TimePickerPopup, DateRangePickerPopup)[exiting],
    :is(ContextMenu, Menu, SelectPopup, ComboboxPopup, ComboBoxPopup, SearchFieldPopup, DatePickerPopup, TimePickerPopup, DateRangePickerPopup)[reentered] {
        animation: none; transition: none; overlay-exit-duration: 0ms;
    }
}

TabsAdd { border-radius: var(--leaf-r-ctl); background: transparent; color: var(--leaf-text-2); border-color: transparent; }
TabsAdd:hover { background: var(--leaf-hover); color: var(--leaf-text); border-color: transparent; }
TabsAdd:active { background: var(--leaf-press); }
TabsAdd:focus-visible { outline: 2px solid var(--leaf-primary); outline-offset: -2px; }
TabsAdd:disabled { background: transparent; color: var(--leaf-text-3); }
Tabs[presentation=document] TabsList { border-radius: 11px 3px 0 0; }
Tabs[presentation=document] Tab { border-radius: 10px 3px 0 0; }
Tabs[presentation=document] Tab[selected] { background: var(--leaf-pane); color: var(--leaf-text); border-color: var(--leaf-stroke); border-bottom-color: transparent; box-shadow: 0 1px 4px rgba(0,0,0,0.05); }
Tabs[presentation=document] Tab:hover { background: var(--leaf-hover); }
Tabs[presentation=document] Tab[selected]:hover { background: var(--leaf-pane); }
Tabs[presentation=document] TabsIndicator { display: none; }
Tabs[presentation=document] TabsPanel { background: var(--leaf-pane); border-radius: 0 0 11px 3px; padding: 16px; }
Tabs[reorderable=true] Tab { transition: transform 400ms cubic-bezier(0.16,1,0.3,1), color 150ms ease, background-color 150ms ease; }
@media (forced-colors: active) {
    Tabs[presentation=document] Tab[selected] { background: Canvas; color: CanvasText; border-color: Highlight; border-bottom-color: transparent; }
    Tabs[presentation=document] TabsPanel { background: Canvas; }
    TabsAdd:disabled { color: GrayText; }
}
InlineMessagePresence { transition: height 350ms cubic-bezier(0.2, 0, 0, 1), margin-top 350ms; overlay-exit-duration: 350ms; }
InlineMessagePresence + InlineMessagePresence { margin-top: 8px; }
InlineMessagePresence[exiting=true] { margin-top: 0; }
InlineMessagePresenceContent > InlineMessage { transition: opacity 200ms; }
InlineMessagePresenceContent[expanded=false] > InlineMessage { animation: none; opacity: 0; }
@media (prefers-reduced-motion: reduce) {
    InlineMessagePresence { transition: none; overlay-exit-duration: 0ms; }
    InlineMessagePresenceContent > InlineMessage { animation: none; transition: none; }
}

PipsPagerPip { border-radius: var(--leaf-r-xs); }
PipsPagerDot { width: 6px; height: 6px; border-radius: 4px 1px 4px 1px; background: var(--leaf-text-3); transition: width 180ms ease, height 180ms ease, background-color 180ms ease; }
PipsPagerPip[current] > PipsPagerDot { width: 10px; height: 7px; background: var(--leaf-primary); }
PipsPagerPip:hover, PipsPagerPrevious:hover, PipsPagerNext:hover { background: var(--leaf-hover); }
PipsPagerPip:active, PipsPagerPrevious:active, PipsPagerNext:active { background: var(--leaf-press); }
PipsPagerPrevious, PipsPagerNext { border-radius: var(--leaf-r-xs); }
PipsPager:disabled PipsPagerDot { background: var(--leaf-text-3); }
@media (forced-colors: active) {
    PipsPagerPip[current] > PipsPagerDot { background: Highlight; }
    PipsPager:disabled PipsPagerDot { background: GrayText; }
}
:is(DialogContainer, Dialog, AlertDialog) {
    opacity: 1; transform: translateY(0px) scale(1);
    overlay-exit-duration: 180ms;
    transition: opacity 180ms cubic-bezier(0.2, 0, 0, 1), transform 180ms cubic-bezier(0.2, 0, 0, 1), border-radius 180ms cubic-bezier(0.2, 0, 0, 1);
    animation: leaf-dialog-round 550ms cubic-bezier(0.16, 1, 0.3, 1) backwards;
}
:is(DialogContainer, Dialog, AlertDialog)[exiting=true] {
    animation: none; opacity: 0; transform: translateY(0px) scale(0.97);
}
:is(DialogContainer, Dialog, AlertDialog)[reentered=true] { animation: none; }
@keyframes leaf-dialog-round {
    from { opacity: 0; transform: translateY(16px) scale(0.96); border-radius: 60px 8px 60px 8px; }
}
@media (prefers-reduced-motion: reduce) {
    :is(DialogContainer, Dialog, AlertDialog),
    :is(DialogContainer, Dialog, AlertDialog)[exiting=true],
    :is(DialogContainer, Dialog, AlertDialog)[reentered=true] {
        animation: none; transition: none; overlay-exit-duration: 0ms;
    }
}
Button:not([icon-only]):active, ToggleButton:not([icon-only]):active, MenuButton:not([icon-only]):active {
    transition: background-color 80ms, color 80ms, box-shadow 80ms cubic-bezier(0.2, 0, 0, 1), transform 80ms cubic-bezier(0.34, 1.5, 0.64, 1), border-radius 80ms cubic-bezier(0.34, 1.5, 0.64, 1);
}
Button[icon-only], ToggleButton[icon-only], MenuButton[icon-only] {
    transition: background-color 150ms, color 150ms, transform 250ms cubic-bezier(0.34, 1.5, 0.64, 1);
}
Slider[orientation="horizontal"][value-label] > SliderThumb,
RangeSlider[orientation="horizontal"][value-label] > RangeSliderRail > RangeSliderHandle > RangeSliderThumb {
    top: 11px; width: 6px; height: 24px; transform: translate(-50%, -50%);
    transition: width 200ms cubic-bezier(0.2, 0, 0, 1), height 200ms cubic-bezier(0.2, 0, 0, 1);
}
Slider[orientation="horizontal"][value-label] > SliderThumb { left: calc(var(--nz-slider-position) * (100% - 14px) + 7px); }
RangeSlider[orientation="horizontal"][value-label] > RangeSliderRail > RangeSliderHandle > RangeSliderThumb { left: calc(var(--nz-range-thumb-position) * 100%); }
Slider[orientation="horizontal"][value-label]:hover > SliderThumb,
RangeSlider[orientation="horizontal"][value-label] > RangeSliderRail > RangeSliderHandle:hover > RangeSliderThumb {
    top: 11px; height: 28px; transform: translate(-50%, -50%);
}
Slider[orientation="horizontal"][value-label]:active > SliderThumb,
RangeSlider[orientation="horizontal"][value-label] > RangeSliderRail > RangeSliderHandle:active > RangeSliderThumb {
    top: 11px; width: 10px; height: 28px; transform: translate(-50%, -50%);
}
Slider[orientation="horizontal"][value-label]:active > SliderThumb { left: calc(var(--nz-slider-position) * (100% - 14px) + 7px); }
RangeSlider[orientation="horizontal"][value-label] > RangeSliderRail > RangeSliderHandle:active > RangeSliderThumb { left: calc(var(--nz-range-thumb-position) * 100%); }
Slider[orientation="vertical"][value-label] > SliderThumb,
RangeSlider[orientation="vertical"][value-label] > RangeSliderRail > RangeSliderHandle > RangeSliderThumb {
    top: auto; left: 11px; width: 24px; height: 6px; transform: translate(-50%, 50%);
    transition: width 200ms cubic-bezier(0.2, 0, 0, 1), height 200ms cubic-bezier(0.2, 0, 0, 1);
}
Slider[orientation="vertical"][value-label] > SliderThumb { bottom: calc(var(--nz-slider-position) * (100% - 14px) + 7px); }
RangeSlider[orientation="vertical"][value-label] > RangeSliderRail > RangeSliderHandle > RangeSliderThumb { bottom: calc(var(--nz-range-thumb-position) * 100%); }
Slider[orientation="vertical"][value-label]:hover > SliderThumb,
RangeSlider[orientation="vertical"][value-label] > RangeSliderRail > RangeSliderHandle:hover > RangeSliderThumb {
    top: auto; left: 11px; width: 28px; height: 6px; transform: translate(-50%, 50%);
}
Slider[orientation="vertical"][value-label]:active > SliderThumb,
RangeSlider[orientation="vertical"][value-label] > RangeSliderRail > RangeSliderHandle:active > RangeSliderThumb {
    top: auto; left: 11px; width: 28px; height: 10px; transform: translate(-50%, 50%);
}
Slider[orientation="vertical"][value-label]:active > SliderThumb { bottom: calc(var(--nz-slider-position) * (100% - 14px) + 7px); }
RangeSlider[orientation="vertical"][value-label] > RangeSliderRail > RangeSliderHandle:active > RangeSliderThumb { bottom: calc(var(--nz-range-thumb-position) * 100%); }
@media (prefers-reduced-motion: reduce) {
    Button:not([icon-only]):active, ToggleButton:not([icon-only]):active, MenuButton:not([icon-only]):active,
    Button[icon-only], ToggleButton[icon-only], MenuButton[icon-only],
    Slider[orientation][value-label] > SliderThumb, Slider[orientation][value-label]:hover > SliderThumb, Slider[orientation][value-label]:active > SliderThumb,
    RangeSlider[orientation][value-label] > RangeSliderRail > RangeSliderHandle > RangeSliderThumb,
    RangeSlider[orientation][value-label] > RangeSliderRail > RangeSliderHandle:hover > RangeSliderThumb,
    RangeSlider[orientation][value-label] > RangeSliderRail > RangeSliderHandle:active > RangeSliderThumb { transition: none; }
}
BreadcrumbItemContent > Button { border-radius: var(--leaf-r-ctl); padding: 6px 8px; color: var(--leaf-text-2); transform: none; }
BreadcrumbItemContent > Button:hover { background: var(--leaf-hover); color: var(--leaf-text); }
BreadcrumbItemContent > Button:active { background: var(--leaf-press); color: var(--leaf-ink); }
BreadcrumbSlot[current] Button { color: var(--leaf-ink); }
BreadcrumbSeparator { color: var(--leaf-text-3); }
NavigationViewChrome { background: var(--leaf-pane); }
NavigationViewToggle { border-radius: var(--leaf-r-ctl); color: var(--leaf-text-2); }
NavigationViewToggle:hover, NavigationViewRootItem:hover { background: var(--leaf-hover); color: var(--leaf-text); }
NavigationViewToggle:active, NavigationViewRootItem:active { background: var(--leaf-press); }
NavigationViewRootItem { margin: 2px 4px; border-radius: var(--leaf-r-item); color: var(--leaf-text-2); }
NavigationViewRootItem[selected], NavigationViewTreeHost TreeViewItem[selected] { background: var(--leaf-pane); color: var(--leaf-text); box-shadow: var(--leaf-sh-pane); }
NavigationViewTreeIndicator, NavigationViewRootIndicator { width: 5px; height: 12px; border-radius: var(--leaf-r-seed); background: var(--leaf-primary); }
NavigationView[tree-ready=true] NavigationViewTreeIndicator, NavigationView[roots-ready=true] NavigationViewRootIndicator { transition: transform 400ms cubic-bezier(0.16,1,0.3,1); }
NavigationView[display-mode=top] NavigationViewRootIndicator { width: 12px; height: 3px; border-radius: 5px 1px 5px 1px; }
NavigationViewContent { background: var(--leaf-pane); border: 1px solid var(--leaf-stroke); border-radius: var(--leaf-r-pane); }
@media (forced-colors: active) {
    NavigationViewRootItem[selected], NavigationViewTreeHost TreeViewItem[selected] { background: Highlight; color: HighlightText; }
    NavigationViewContent { background: Canvas; border-color: CanvasText; }
    NavigationViewTreeIndicator, NavigationViewRootIndicator { background: Highlight; }
}
Tooltip, TooltipContainer { opacity: 1; transform: translateY(0px) scale(1); overlay-exit-duration: 300ms; animation: none; transition: opacity 150ms, transform 300ms cubic-bezier(0.16, 1, 0.3, 1); }
Tooltip[overlay=true], TooltipContainer[overlay=true] { animation: leaf-tooltip-opacity 150ms backwards, leaf-tooltip-travel 300ms cubic-bezier(0.16, 1, 0.3, 1) backwards; }
Tooltip[exiting=true], TooltipContainer[exiting=true] { animation: none; opacity: 0; transform: translateY(4px) scale(0.96); }
Tooltip[reentered=true], TooltipContainer[reentered=true] { animation: none; }
@keyframes leaf-tooltip-opacity { from { opacity: 0; } }
@keyframes leaf-tooltip-travel { from { transform: translateY(4px) scale(0.96); } }
ToastHostRegion > ItemsTransition { overlay-exit-duration: 550ms; }
ToastHostRegion[toast-placement="bottom-center"] { bottom: 24px; }
Toast { opacity: 1; transform: translateY(0%) rotate(0deg); transition: opacity 200ms, transform 550ms cubic-bezier(0.16, 1, 0.3, 1); animation: leaf-toast-opacity 200ms backwards, leaf-toast-travel 550ms cubic-bezier(0.16, 1, 0.3, 1) backwards; }
ToastHostRegion ItemsTransitionContent[exiting=true] > Toast { animation: none; opacity: 0; transform: translateY(160%) rotate(-3deg); }
@keyframes leaf-toast-opacity { from { opacity: 0; } }
@keyframes leaf-toast-travel { from { transform: translateY(160%) rotate(-3deg); } }
@media (prefers-reduced-motion: reduce) {
    Tooltip, TooltipContainer, Tooltip[overlay=true], TooltipContainer[overlay=true], Tooltip[exiting=true], TooltipContainer[exiting=true], Tooltip[reentered=true], TooltipContainer[reentered=true],
    Toast, ToastHostRegion ItemsTransitionContent[exiting=true] > Toast { animation: none; transition: none; }
    Tooltip, TooltipContainer, ToastHostRegion > ItemsTransition { overlay-exit-duration: 0ms; }
}
ToggleButton[icon-only][pressed] Icon { animation: leaf-icon-pop 450ms cubic-bezier(0.34, 1.5, 0.64, 1); }
ToggleButton[icon-only]:disabled Icon, ToggleButton[icon-only][busy] Icon { animation: none; }
@media (prefers-reduced-motion: reduce) { ToggleButton[icon-only][pressed] Icon { animation: none; } }
@media (forced-colors: active) { ToggleButton[icon-only][pressed] Icon { animation: none; } }
NavigationViewTreeIndicator, NavigationViewRootIndicator { transform: translate(0px, calc(var(--nz-indicator-y, 0px) + var(--nz-indicator-height, 0px) / 2 - 6px)); }
NavigationView[display-mode=top] NavigationViewRootIndicator { transform: translate(calc(var(--nz-indicator-x, 0px) + var(--nz-indicator-width, 0px) / 2 - 6px), calc(var(--nz-indicator-y, 0px) + var(--nz-indicator-height, 0px) - 4px)); }
ColorPicker { width: 320px; gap: 14px; }
ColorPickerVisuals { gap: 14px; }
ColorPickerSpectrum, ColorPickerSwatch { border-radius: var(--leaf-pc) var(--leaf-pc) var(--leaf-sc) var(--leaf-pc); box-shadow: inset 0 0 0 1.6px var(--leaf-ink); }
ColorPickerSpectrumThumb, ColorPickerSliderThumb, ColorPickerAlphaSliderThumb { border-radius: 12px 12px 2px 12px; border: 2px solid var(--leaf-on-primary); outline: 1px solid var(--leaf-ink); box-shadow: inset 0 0 0 1px var(--leaf-ink); }
ColorPickerSliderTrack, ColorPickerAlphaSliderTrack { border-radius: 8px 8px 2px 8px; box-shadow: inset 0 0 0 1.6px var(--leaf-ink); }
ColorPickerChannel Label, ColorPickerHex Label { color: var(--leaf-ink); font-weight: 600; }
ColorPickerSpectrum:focus-visible, ColorPickerSlider:focus-visible, ColorPickerAlphaSlider:focus-visible { outline: none; box-shadow: var(--leaf-focus-ring), inset 0 0 0 1.6px var(--leaf-ink); }
@media (forced-colors: active) {
    ColorPickerSpectrumThumb, ColorPickerSliderThumb, ColorPickerAlphaSliderThumb { border-color: Canvas; outline-color: CanvasText; box-shadow: none; }
}
NavigationViewTreeHost TreeViewItem { transition: background-color 200ms, color 200ms, border-radius 400ms cubic-bezier(0.34,1.56,0.64,1); }
BadgeIndicator { overflow: hidden; }
BadgeIndicator[icon] { width: 18px; min-width: 18px; padding: 0; }
Badge[size="small"] > BadgeIndicator[icon] { width: 14px; min-width: 14px; padding: 0; }
Badge[size="large"] > BadgeIndicator[icon] { width: 22px; min-width: 22px; padding: 0; }
Badge[size="large"] BadgeIcon { font-size: 13px; }
BadgeValue[changed=true] { animation: leaf-badge-roll 350ms cubic-bezier(0.16, 1, 0.3, 1); }
@keyframes leaf-badge-roll {
    from { transform: translateY(90%); opacity: 0; }
    to { transform: none; opacity: 1; }
}
@media (prefers-reduced-motion: reduce) {
    BadgeValue[changed=true] { animation: none; }
}
@media (forced-colors: active) {
    BadgeValue[changed=true] { animation: none; }
}
Strong { font-weight: 650; }
InlineCode { font-family: var(--leaf-font-mono); font-size: 0.86em; padding: 2px 6px; border-radius: var(--leaf-r-xs); background: var(--leaf-s2); }
Mark { background: color-mix(in srgb, var(--leaf-g3) 40%, transparent); color: inherit; padding: 0 4px; border-radius: 6px 1px 6px 1px; }
Inserted { text-decoration-line: none; background: var(--leaf-ok-c); color: var(--leaf-on-ok-c); padding: 0 4px; border-radius: 6px 1px 6px 1px; }
Deleted { color: var(--leaf-text-3); }
SmallText { color: var(--leaf-text-2); }
Paragraph Kbd { font-family: var(--leaf-font-ui); font-size: 0.78em; font-weight: 550; line-height: 1; padding: 4px 7px 5px; border: 0; border-radius: 7px 2px 7px 2px; background: var(--leaf-pane); box-shadow: inset 0 0 0 1px var(--leaf-stroke-2), inset 0 -2px 0 var(--leaf-stroke-2); position: relative; top: -1px; }
Paragraph Link { color: var(--leaf-ink); text-decoration-thickness: 1.5px; text-decoration-color: color-mix(in srgb, var(--leaf-ink) 40%, transparent); }
Paragraph Link:hover { text-decoration-color: var(--leaf-ink); }
