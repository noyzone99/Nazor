/*
 * Desktop shells. Windows 9x, XP and 7, CDE, Adwaita and Breeze present the gallery as an application of their own
 * desktop: Explorer, the CDE Application Manager, GNOME Files and Dolphin lend the frame, bars, panes and icon views.
 * The rules only dress the gallery frame; live examples keep their theme's ordinary control appearance.
 */
.shell-glyph { display: none; }
.sidebar-tree, .nav-places, .sidebar-footer { display: flex; flex-direction: column; gap: 8px; min-width: 0; }
.sidebar-tree { flex: 1; min-height: 0; }
.nav-group, .nav-items { display: flex; flex-direction: column; min-width: 0; }
.nav-folder { display: none; }

/* Shell layout, shared by every desktop theme in normal and forced colors. */
.gallery.desktop { grid-template-rows: auto minmax(0px, 1fr) auto; }
.gallery.desktop:not(.single-view) { position: relative; left: auto; top: auto; right: auto; bottom: auto; flex: 1; min-height: 0; }
.gallery.desktop.single-view { grid-template-rows: auto minmax(0px, 1fr) auto auto; }
.desktop.single-view .gallery-exit { grid-row: 4; }
.desktop .shell-top { grid-column: 1 / 3; grid-row: 1; display: flex; flex-direction: column; min-width: 0; }
.desktop .sidebar, .desktop .main-scroll { grid-row: 2; }
.desktop .shell-status { grid-column: 1 / 3; grid-row: 3; }
.desktop .shell-toolbar { display: flex; align-items: center; gap: 2px; min-width: 0; }
.desktop .shell-commands { display: flex; align-items: center; min-width: 0; }
.desktop .shell-address { display: flex; align-items: center; gap: 6px; flex: 1; min-width: 0; }
.desktop .shell-address-field { display: flex; align-items: center; gap: 4px; flex: 1; min-width: 0; }
.desktop .shell-address-text { white-space: pre; overflow-x: hidden; text-overflow: ellipsis; }
.desktop .shell-breadcrumb { flex: 1; min-width: 0; }
.desktop .shell-search { flex: 0 1 220px; min-width: 120px; }

/* Generated layers keep each two-tone icon in one authored element. */
.gallery-glyph::before, .gallery-glyph::after { width: 1em; height: 1em; line-height: 1; flex-shrink: 0; }
.gallery-glyph::before { content: var(--gallery-glyph-fill, none); font-family: "FluentSystemIcons-Filled"; }
.gallery-glyph::after { content: var(--gallery-glyph-line, none); font-family: "FluentSystemIcons-Regular"; }
.desktop .shell-glyph, .desktop .glyph-tile { position: relative; display: flex; flex-shrink: 0; }
.desktop .shell-glyph { width: 16px; height: 16px; font-size: 16px; }
.desktop .gallery-glyph::before, .desktop .gallery-glyph::after {
    position: absolute; left: 0; top: 0; right: 0; bottom: 0;
    display: flex; align-items: center; justify-content: center;
}
.desktop .nav-folder { display: flex; }
.desktop .nav-glyph, .desktop .eyebrow, .desktop .intro-art, .desktop .back-link, .desktop .group-count { display: none; }
.desktop .nav-category { padding: 0; font-size: inherit; font-weight: 400; }
.desktop .nav-item, .desktop .nav-home { min-height: 0; gap: 4px; }
.desktop .nav-item.current { border-left-width: 0; font-weight: inherit; }
.desktop .main-scroll { border-left-width: 0; }
.desktop .main { align-items: stretch; }
.desktop .page-head { gap: 10px; }
.desktop .description { max-width: none; line-height: normal; }
.desktop .catalog-card { min-height: 0; text-align: left; }
.desktop .catalog-card > ButtonLabel { display: flex; flex-direction: column; align-items: center; gap: 4px; min-width: 0; }
.desktop .example-card { display: flex; flex-direction: column; }
.desktop .page-glyph { width: 32px; height: 32px; font-size: 32px; }
.desktop .group-glyph { width: 16px; height: 16px; font-size: 16px; }
.desktop .card-glyph { width: 32px; height: 32px; font-size: 32px; }

@media (forced-colors: active) {
    .desktop .gallery-glyph::before { display: none; }
    .desktop .gallery-glyph::after { color: CanvasText; }
    .desktop .nav-item.current, .desktop .nav-home.current-home { background: Highlight; color: HighlightText; }
}

@media (forced-colors: none) {
    /* Two-tone icons: the family color fills the glyph under a darker outline. */
    .desktop .gallery-glyph::after { color: color-mix(in srgb, var(--hue, #2b6cc4) 58%, #000000); }
    .desktop .gallery-glyph::before { color: color-mix(in srgb, var(--hue, #2b6cc4) 78%, #ffffff); }
    .desktop .nav-home { --hue: #2b6cc4; }
    .desktop .nav-showcase { --hue: #2f9e5b; }
    .desktop .nav-folder .gallery-glyph::before { color: #f4d465; }
    .desktop .nav-folder .gallery-glyph::after { color: #9a7210; }

    /* Windows 9x: Windows 98 Explorer. Gray 3D bars, a white sunken folder tree, a Large Icons view and a sunken status bar. */
    .gallery.theme-windows-9x {
        --w9-face: #c0c0c0; --w9-hi: #ffffff; --w9-light: #dfdfdf; --w9-shadow: #808080; --w9-dark: #000000;
        grid-template-columns: 230px minmax(0px, 1fr);
        column-gap: 3px;
        padding: 0 2px;
        background: var(--w9-face);
    }
    .theme-windows-9x {
        .gallery-glyph::after { color: #000000; }
        .gallery-glyph::before { color: color-mix(in srgb, var(--hue, #2b6cc4) 70%, #ffffff); }
        .nav-folder .gallery-glyph::after { color: #5a4600; }
        .nav-folder .gallery-glyph::before { color: #ffeb8a; }
        .shell-top { margin: 0 -2px; }
        .shell-menu { border-bottom: 1px solid var(--w9-shadow); box-shadow: 0 1px 0 var(--w9-hi); }
        .shell-toolbar { gap: 1px; padding: 2px 4px; border-top: 1px solid var(--w9-hi); border-bottom: 1px solid var(--w9-shadow); }
        .shell-nav { flex-direction: column; gap: 1px; min-width: 52px; min-height: 40px; padding: 2px 4px; border: 1px solid transparent; background: transparent; box-shadow: none; }
        .shell-nav .shell-glyph { width: 22px; height: 22px; font-size: 22px; }
        .shell-nav .shell-glyph::before { color: #e8e8e8; }
        .shell-nav:hover:not(:disabled) { border-color: var(--w9-hi) var(--w9-shadow) var(--w9-shadow) var(--w9-hi); }
        .shell-nav:hover:not(:disabled) .shell-glyph::before { color: #3ba33b; }
        .shell-up:hover:not(:disabled) .shell-glyph::before { color: #ffeb8a; }
        .shell-nav:active:not(:disabled) { border-color: var(--w9-shadow) var(--w9-hi) var(--w9-hi) var(--w9-shadow); background: transparent; color: var(--nz-on-control); }
        .shell-nav:disabled .shell-glyph::after { color: var(--w9-shadow); }
        .shell-nav:disabled .shell-glyph::before { color: var(--w9-face); }
        ToolbarSeparator { width: 2px; margin: 2px 4px; background: var(--w9-shadow); border-right: 1px solid var(--w9-hi); }
        .shell-address { gap: 4px; padding-left: 2px; }
        .shell-address-label { color: #000000; }
        .shell-address-field, .shell-search {
            min-height: 22px; padding: 1px 3px; background: var(--w9-hi);
            border: 1px solid; border-color: var(--w9-shadow) var(--w9-hi) var(--w9-hi) var(--w9-shadow);
            box-shadow: inset 1px 1px 0 var(--w9-dark), inset -1px -1px 0 var(--w9-light);
        }
        .sidebar { padding: 3px 0 4px; gap: 6px; background: var(--w9-face); }
        .sidebar-tree {
            gap: 0; padding: 3px 2px; background: var(--w9-hi); color: #000000;
            border: 1px solid; border-color: var(--w9-shadow) var(--w9-hi) var(--w9-hi) var(--w9-shadow);
            box-shadow: inset 1px 1px 0 var(--w9-dark), inset -1px -1px 0 var(--w9-light);
        }
        .nav-places { gap: 0; }
        .navigation { gap: 0; }
        .nav-category { gap: 4px; min-height: 18px; padding: 0 2px; }
        .nav-items { margin-left: 9px; padding-left: 9px; border-left: 1px dotted var(--w9-shadow); }
        .nav-item, .nav-home { min-height: 18px; padding: 0 2px; border: 1px solid transparent; background: transparent; box-shadow: none; color: #000000; }
        .nav-item:hover, .nav-home:hover { border-color: transparent; background: transparent; }
        .nav-item:active, .nav-home:active { background: transparent; color: #000000; }
        .nav-item.current, .nav-home.current-home { background: #000080; color: #ffffff; border-color: transparent; }
        .nav-item.current:hover, .nav-home.current-home:hover { background: #000080; }
        .nav-item:focus-visible, .nav-home:focus-visible { outline: 1px dotted currentColor; outline-offset: -1px; }
        .sidebar-footer { gap: 4px; padding: 0 2px; }
        .main-scroll {
            margin: 3px 0 4px; background: var(--w9-hi); color: #000000;
            border: 1px solid; border-color: var(--w9-shadow) var(--w9-hi) var(--w9-hi) var(--w9-shadow);
            box-shadow: inset 1px 1px 0 var(--w9-dark), inset -1px -1px 0 var(--w9-light);
        }
        .main-scroll.page { background: var(--w9-face); }
        .main { gap: 8px; padding: 12px 14px; }
        .catalog-intro { padding-bottom: 6px; }
        .intro-text { gap: 6px; }
        .catalog-intro .page-title { font-family: Arial, sans-serif; font-size: 26px; font-weight: 700; }
        .intro-text::after { content: ""; display: block; height: 3px; background: linear-gradient(90deg, #ff0000, #ffcc00 30%, #00a000 60%, #0000ff); }
        .group-head { margin-top: 8px; gap: 6px; padding-bottom: 2px; border-bottom: 1px solid var(--w9-shadow); box-shadow: 0 1px 0 var(--w9-hi); }
        .group-title { font-size: 11px; font-weight: 700; }
        .cards { grid-template-columns: repeat(auto-fill, 96px); gap: 6px 4px; }
        .catalog-card, .catalog-card:hover, .catalog-card:active {
            align-items: center; justify-content: flex-start; gap: 3px; min-width: 0; padding: 4px 2px;
            border: 1px solid transparent; background: transparent; box-shadow: none; color: #000000; text-align: center;
        }
        .catalog-card:focus-visible { outline: 1px dotted #000000; outline-offset: -1px; }
        .catalog-card:focus-visible .card-title { background: #000080; color: #ffffff; }
        .card-title { font-weight: 400; padding: 0 2px; }
        .card-description { display: none; }
        .page-title { font-size: 16px; font-weight: 700; }
        .page-type { color: var(--w9-shadow); }
        .example-card {
            margin-top: 6px; padding: 0 8px 8px; background: transparent;
            border: 1px solid var(--w9-shadow); box-shadow: 1px 1px 0 var(--w9-hi), inset 1px 1px 0 var(--w9-hi);
        }
        .section-title, .source-heading { align-self: flex-start; margin: -8px 0 0 -2px; padding: 0 3px; background: var(--w9-face); font-weight: 400; }
        .source-heading { margin-top: 8px; }
        .example-card > Separator { display: none; }
        .preview { padding: 10px 4px; }
        .code {
            margin-top: 4px; padding: 4px 6px; background: var(--w9-hi); font-family: "Courier New", Courier, monospace; font-size: 12px;
            border: 1px solid; border-color: var(--w9-shadow) var(--w9-hi) var(--w9-hi) var(--w9-shadow);
            box-shadow: inset 1px 1px 0 var(--w9-dark), inset -1px -1px 0 var(--w9-light);
        }
        .shell-status { margin: 0 -2px; }
    }

    /* Windows XP: Luna Explorer. Beige rebars, the blue task pane with collapsible panels and the Tiles view. */
    .gallery.theme-windows-xp { grid-template-columns: 220px minmax(0px, 1fr); background: #ece9d8; }
    .theme-windows-xp {
        .shell-menu { border-bottom: 1px solid #d8d2bd; }
        .shell-toolbar {
            gap: 2px; padding: 2px 4px; background: linear-gradient(180deg, #fbfbf8, #ece9d8 80%, #e3dfcb);
            border-top: 1px solid #ffffff; border-bottom: 1px solid #c2bdab;
        }
        .shell-nav { gap: 4px; min-height: 36px; padding: 2px 6px 2px 3px; border: 1px solid transparent; border-radius: 3px; background: transparent; box-shadow: none; }
        .shell-nav .shell-glyph { width: 28px; height: 28px; font-size: 28px; }
        .shell-back .shell-glyph::before, .shell-forward .shell-glyph::before { color: #3aa53a; }
        .shell-back .shell-glyph::after, .shell-forward .shell-glyph::after { color: #ffffff; }
        .shell-back .shell-glyph, .shell-forward .shell-glyph { border-radius: 50%; background: #2a8a2a; }
        .shell-up .shell-glyph { width: 24px; height: 24px; font-size: 24px; }
        .shell-up .shell-glyph::before { color: #f6d673; }
        .shell-up .shell-glyph::after { color: #b88a1b; }
        .shell-nav:hover:not(:disabled) { border-color: #cecebf; background: linear-gradient(180deg, #ffffff, #f1efe6); box-shadow: inset 0 -1px 0 #d4d0c2; }
        .shell-nav:active:not(:disabled) { border-color: #9d9d92; background: #e3e1d6; color: #000000; }
        .shell-nav:disabled { color: #aca899; }
        .shell-nav:disabled .shell-glyph { background: #c9c7ba; }
        .shell-nav:disabled .shell-glyph::before { color: #d8d6cb; }
        ToolbarSeparator { margin: 4px 4px; background: #c5c2b2; border-right: 1px solid #ffffff; }
        .shell-address { padding-left: 4px; gap: 4px; }
        .shell-address-label { color: #7d7b71; }
        .shell-address-field, .shell-search { min-height: 22px; padding: 1px 3px; border: 1px solid #7f9db9; border-radius: 0; background: #ffffff; }
        .sidebar { gap: 15px; padding: 12px 12px 8px; background: linear-gradient(180deg, #7aa1e6, #6375d6); }
        .sidebar-tree { gap: 15px; }
        .navigation { display: flex; flex-direction: column; gap: 15px; }
        .nav-places, .nav-group, .sidebar-footer { gap: 0; border-radius: 3px 3px 0 0; }
        .nav-places::before, .sidebar-footer::before, .nav-category {
            display: flex; align-items: center; min-height: 25px; padding: 0 10px; border-radius: 3px 3px 0 0;
            background: linear-gradient(90deg, #ffffff, #c6d3f7); color: #215dc6; font-weight: 700;
        }
        .nav-places::before { content: "Gallery Tasks"; background: linear-gradient(90deg, #0c47c9, #2a68e0); color: #ffffff; }
        .sidebar-footer::before { content: "Appearance"; }
        .nav-places, .nav-items, .sidebar-footer { padding-bottom: 8px; background: #d6dff7; border: 1px solid #ffffff; border-top-width: 0; }
        .nav-places { padding-top: 0; background: #eff3ff; }
        .nav-items, .sidebar-footer > Field { padding: 0 10px; }
        .nav-items { padding-top: 8px; gap: 2px; }
        .sidebar-footer { gap: 6px; }
        .sidebar-footer > Field { color: #215dc6; }
        .nav-places > .nav-home { margin: 0 10px; }
        .nav-places > .nav-home:first-of-type { margin-top: 8px; }
        .nav-category { gap: 0; }
        .nav-folder { display: none; }
        .nav-item, .nav-home {
            min-height: 18px; padding: 0; gap: 6px; border: 0; background: transparent; box-shadow: none; color: #215dc6; text-align: left;
        }
        .nav-item:hover, .nav-home:hover { background: transparent; color: #428eff; text-decoration-line: underline; }
        .nav-item:active, .nav-home:active { background: transparent; color: #215dc6; }
        .nav-item.current, .nav-home.current-home { background: transparent; color: #0c2f8c; font-weight: 700; }
        .main-scroll { background: #ffffff; }
        .main { gap: 8px; padding: 14px 18px; }
        .catalog-intro .page-title, .page-title { font-family: "Trebuchet MS", Tahoma, sans-serif; color: #1c3f94; font-weight: 700; }
        .catalog-intro .page-title { font-size: 22px; }
        .page-title { font-size: 19px; }
        .description { color: #000000; }
        .group-head {
            margin-top: 10px; gap: 6px; padding-bottom: 3px;
            border-bottom: 2px solid #b4c6ef; border-image-source: linear-gradient(90deg, #b4c6ef, #ffffff);
        }
        .group-glyph { display: none; }
        .group-title { font-size: 12px; font-weight: 700; color: #0e3d9a; }
        .cards { grid-template-columns: repeat(auto-fill, minmax(250px, 1fr)); gap: 6px 12px; }
        .catalog-card, .catalog-card:hover, .catalog-card:active {
            align-items: stretch; padding: 3px; border: 1px solid transparent; border-radius: 0; background: transparent; box-shadow: none; color: #000000;
        }
        .catalog-card > ButtonLabel { display: grid; grid-template-columns: 52px minmax(0px, 1fr); grid-template-rows: auto auto; column-gap: 6px; row-gap: 1px; align-items: center; flex: 1; min-width: 0; }
        .card-glyph { grid-row: 1 / 3; width: 48px; height: 48px; font-size: 44px; }
        .card-title { align-self: end; font-weight: 400; }
        .card-description { align-self: start; color: #7d7d7d; font-size: 11px; line-height: 13px; max-height: 26px; overflow-y: hidden; }
        .catalog-card:focus-visible { outline: 1px dotted #000000; outline-offset: -1px; }
        .catalog-card:focus-visible .card-title { background: #316ac5; color: #ffffff; }
        .catalog-card:hover .card-title { color: #000000; }
        .page-type { color: #7d7d7d; }
        .example-card { margin-top: 6px; padding: 0 9px 9px; border: 1px solid #d0d0bf; border-radius: 4px; background: transparent; }
        .section-title, .source-heading { align-self: flex-start; margin: -8px 0 0 -3px; padding: 0 3px; background: #ffffff; color: #0046d5; font-weight: 400; }
        .source-heading { margin-top: 10px; }
        .example-card > Separator { display: none; }
        .preview { padding: 10px 4px; }
        .code { margin-top: 4px; padding: 4px 6px; border: 1px solid #7f9db9; background: #ffffff; font-family: "Lucida Console", "Courier New", monospace; font-size: 11px; }
    }

    /* Windows 7: Aero Explorer. Address and search on the glass frame, the command bar, the white navigation pane,
       Control Panel headings and the light-blue hover and selection. */
    .gallery.theme-windows-7 { grid-template-columns: 220px minmax(0px, 1fr); background: #ffffff; }
    .theme-windows-7 {
        .shell-toolbar { gap: 4px; padding: 3px 6px 6px; background: var(--win7-glass); }
        .shell-nav { width: 26px; height: 26px; min-height: 0; padding: 0; border: 0; border-radius: 50%; background: transparent; box-shadow: none; }
        .shell-nav .shell-glyph { width: 26px; height: 26px; font-size: 26px; }
        .shell-nav .shell-glyph::before { color: #2d6ab8; }
        .shell-nav .shell-glyph::after { color: #ffffff; }
        .shell-nav .shell-glyph { border-radius: 50%; background: radial-gradient(circle at 50% 30%, #9fc4ee, #3c78c3 60%, #1e4f96); box-shadow: 0 0 0 1px #3f6894; }
        .shell-nav:hover:not(:disabled) .shell-glyph { background: radial-gradient(circle at 50% 30%, #bfe3ff, #4b9ae6 60%, #24609f); box-shadow: 0 0 4px #7fc3ff; }
        .shell-nav:hover:not(:disabled), .shell-nav:active:not(:disabled) { background: transparent; border-color: transparent; }
        .shell-nav:disabled .shell-glyph { background: radial-gradient(circle at 50% 30%, #e8eef5, #aab9cc); box-shadow: 0 0 0 1px #8ea0b6; }
        .shell-nav:disabled .shell-glyph::before { color: #b4c2d4; }
        .shell-forward { margin-left: -2px; margin-right: 4px; }
        .shell-address { min-height: 24px; padding: 0 2px; border: 1px solid #8e9daf; border-radius: 2px; background: rgba(255, 255, 255, 0.86); box-shadow: inset 0 1px 0 #ffffff; }
        .shell-breadcrumb { font-size: 12px; }
        .shell-search { flex-basis: 240px; min-height: 24px; border-color: #8e9daf; border-radius: 2px; background: rgba(255, 255, 255, 0.86); font-style: italic; }
        .shell-commands {
            gap: 2px; min-height: 32px; padding: 0 6px;
            background: linear-gradient(180deg, #f5f9fd, #e9f0f8 50%, #dce6f4 51%, #dde9f7);
            border-top: 1px solid #ffffff; border-bottom: 1px solid #a0afc3;
        }
        .shell-command { min-height: 24px; padding: 0 8px; border: 1px solid transparent; border-radius: 3px; background: transparent; box-shadow: none; color: #1e395b; }
        .shell-command:hover:not(:disabled), .shell-command[expanded] {
            border-color: #a9c4e4; background: linear-gradient(180deg, #fbfdff, #e7f1fc 50%, #d9e8fa 51%, #dbeafc); box-shadow: inset 0 0 0 1px rgba(255, 255, 255, 0.8);
        }
        .shell-command:active:not(:disabled) { border-color: #7a9cc6; background: linear-gradient(180deg, #d7e6f8, #c3d8f2); color: #1e395b; }
        .shell-help-button { width: 26px; padding: 0; }
        .shell-help-button .shell-glyph::before { color: #2a72c8; }
        .shell-help-button .shell-glyph::after { color: #ffffff; }
        .sidebar { gap: 6px; padding: 8px 4px 8px 2px; background: #ffffff; border-right: 1px solid #d6e0eb; }
        .sidebar-tree, .nav-places, .navigation { gap: 0; }
        .nav-places { padding-bottom: 10px; }
        .navigation > .nav-group { margin-bottom: 8px; }
        .nav-category { gap: 6px; min-height: 22px; padding: 0 6px; color: #1e395b; }
        .nav-items { padding-left: 18px; }
        .nav-item, .nav-home { min-height: 22px; padding: 0 6px; gap: 6px; border: 1px solid transparent; border-radius: 3px; background: transparent; box-shadow: none; color: #000000; }
        .nav-item:hover, .nav-home:hover { border-color: #b8d6fb; background: linear-gradient(180deg, #fafbfd, #ebf3fd); }
        .nav-item:active, .nav-home:active { color: #000000; background: linear-gradient(180deg, #e4effc, #d4e5fa); }
        .nav-item.current, .nav-home.current-home, .nav-item.current:hover, .nav-home.current-home:hover {
            border-color: #84acdd; background: linear-gradient(180deg, #dcebfc, #c1dbfc); color: #000000;
        }
        .sidebar-footer { gap: 6px; padding: 8px 6px 0; border-top: 1px solid #e2e9f1; }
        .main-scroll { background: #ffffff; }
        .main { gap: 8px; padding: 16px 24px; }
        .catalog-intro .page-title, .page-title { color: #1e3287; font-weight: 400; }
        .catalog-intro .page-title { font-size: 20px; }
        .page-title { font-size: 17px; }
        .group-head { margin-top: 12px; gap: 8px; }
        .group-glyph { display: none; }
        .group-title { flex: 0 0 auto; font-size: 13px; font-weight: 400; color: #1e3287; }
        .group-head::after { content: ""; flex: 1; height: 1px; background: linear-gradient(90deg, #c7d4e6, #ffffff); }
        .cards { grid-template-columns: repeat(auto-fill, minmax(240px, 1fr)); gap: 2px 8px; }
        .catalog-card, .catalog-card:active {
            align-items: stretch; padding: 4px 6px; border: 1px solid transparent; border-radius: 3px; background: transparent; box-shadow: none; color: #000000;
        }
        .catalog-card > ButtonLabel { display: grid; grid-template-columns: 40px minmax(0px, 1fr); grid-template-rows: auto auto; column-gap: 8px; row-gap: 1px; align-items: center; flex: 1; min-width: 0; }
        .catalog-card:hover { border-color: #b8d6fb; background: linear-gradient(180deg, #fafbfd, #ebf3fd); color: #000000; }
        .catalog-card:active { border-color: #84acdd; background: linear-gradient(180deg, #dcebfc, #c1dbfc); }
        .card-glyph { grid-row: 1 / 3; width: 32px; height: 32px; font-size: 32px; }
        .card-title { align-self: end; font-weight: 400; }
        .card-description { align-self: start; color: #6d6d6d; line-height: 15px; max-height: 30px; overflow-y: hidden; }
        .page-type { color: #6d6d6d; }
        .example-card { margin-top: 8px; gap: 6px; border: 0; background: transparent; }
        .section-title, .source-heading { display: flex; align-items: center; gap: 8px; padding: 0; color: #000000; font-weight: 400; }
        .section-title::after, .source-heading::after { content: ""; flex: 1; height: 1px; background: #e5e5e5; }
        .example-card > Separator { display: none; }
        .preview { padding: 8px 18px 14px; }
        .code { margin: 0 18px; padding: 6px 8px; border: 1px solid #abadb3; border-top-color: #e3e9ef; border-radius: 2px; background: #ffffff; font-family: Consolas, "Courier New", monospace; }
    }

    /* CDE: the Application Manager. The teal menu bar, raised sand panes, a sunken icon view and a sunken message line. */
    .gallery.theme-cde { grid-template-columns: 236px minmax(0px, 1fr); gap: 6px; padding: 0 6px 6px; background: var(--nz-surface); }
    .theme-cde {
        .shell-top { margin: 0 -6px; }
        .shell-help { margin-left: auto; }
        .sidebar, .main-scroll {
            border: 2px solid; border-color: var(--cde-light-edge) var(--cde-dark-edge) var(--cde-dark-edge) var(--cde-light-edge);
        }
        .sidebar { gap: 8px; padding: 8px; }
        .sidebar-tree {
            gap: 0; padding: 4px; background: var(--cde-view);
            border: 2px solid; border-color: var(--cde-dark-edge) var(--cde-light-edge) var(--cde-light-edge) var(--cde-dark-edge);
        }
        .nav-places, .navigation { gap: 0; }
        .nav-category { gap: 6px; min-height: 22px; padding: 0 4px; font-weight: 700; }
        .nav-items { padding-left: 22px; }
        .nav-item, .nav-home { min-height: 22px; padding: 0 4px; gap: 6px; border: 2px solid transparent; background: transparent; box-shadow: none; color: var(--nz-on-surface); }
        .nav-item:hover, .nav-home:hover { border-color: transparent; background: transparent; }
        .nav-item:active, .nav-home:active { background: transparent; color: var(--nz-on-surface); }
        .nav-item.current, .nav-home.current-home { background: var(--nz-selection); color: var(--nz-on-selection); border-color: transparent; }
        .nav-item:focus-visible, .nav-home:focus-visible { outline: 2px solid var(--cde-focus); outline-offset: -2px; }
        .main-scroll { margin-top: 6px; }
        .sidebar { margin-top: 6px; }
        .main { gap: 10px; padding: 12px; }
        .main-scroll.home > .main { margin: 8px; padding: 10px; background: var(--cde-view); border: 2px solid; border-color: var(--cde-dark-edge) var(--cde-light-edge) var(--cde-light-edge) var(--cde-dark-edge); }
        .catalog-intro .page-title { font-size: 18px; }
        .page-title { font-size: 18px; font-weight: 700; }
        .group-head { margin-top: 6px; gap: 6px; }
        .group-title { font-size: 13px; font-weight: 700; }
        .cards { grid-template-columns: repeat(auto-fill, 128px); gap: 10px 6px; }
        .catalog-card, .catalog-card:hover, .catalog-card:active {
            align-items: center; gap: 4px; padding: 4px; border: 2px solid transparent; background: transparent; box-shadow: none; color: var(--nz-on-surface); text-align: center;
        }
        .catalog-card:focus-visible { outline: 0; border-color: var(--cde-title); }
        .card-glyph { width: 36px; height: 36px; font-size: 34px; }
        .card-title { font-family: "Courier New", Courier, monospace; font-weight: 400; font-size: 12px; }
        .card-description { display: none; }
        .back-link { display: flex; align-self: flex-start; }
        .example-card {
            padding: 8px; gap: 8px; background: var(--nz-surface);
            border: 2px solid; border-color: var(--cde-light-edge) var(--cde-dark-edge) var(--cde-dark-edge) var(--cde-light-edge);
        }
        .example-card > Separator { height: 2px; background: var(--cde-dark-edge); border-bottom: 1px solid var(--cde-light-edge); }
        .section-title, .source-heading { padding: 0; font-weight: 700; }
        .preview { padding: 8px 4px; }
        .code {
            padding: 6px 8px; background: var(--cde-view); font-family: "Courier New", Courier, monospace;
            border: 2px solid; border-color: var(--cde-dark-edge) var(--cde-light-edge) var(--cde-light-edge) var(--cde-dark-edge);
        }
        .shell-status { margin: 6px 0 0; }
    }

    /* Adwaita: a libadwaita split view. The sidebar header and the content header share the window's header bar; boxed
       lists and cards carry the catalog and examples. */
    .gallery.theme-adwaita { grid-template-columns: 260px minmax(0px, 1fr); grid-template-rows: minmax(0px, 1fr); }
    .theme-adwaita {
        .sidebar { gap: 2px; padding: 6px 0 8px; background: var(--adwaita-sidebar); border-right: 1px solid var(--adwaita-pane-edge); }
        .sidebar-tree, .nav-places, .navigation { gap: 2px; }
        .nav-places { padding-bottom: 6px; }
        .navigation > .nav-group { padding-top: 6px; border-top: 1px solid var(--adwaita-pane-edge); margin: 0 0 6px; }
        .nav-category { min-height: 28px; padding: 0 18px; color: var(--nz-muted); font-size: 13px; font-weight: 700; }
        .nav-folder { display: none; }
        .nav-item, .nav-home {
            min-height: 34px; margin: 0 6px; padding: 0 12px; gap: 12px; border: 0; border-radius: 6px; background: transparent; box-shadow: none;
            color: var(--nz-on-surface); font-weight: 400;
        }
        .nav-home .shell-glyph { width: 16px; height: 16px; font-size: 16px; }
        .gallery-glyph::before { display: none; }
        .gallery-glyph::after { color: currentColor; }
        .nav-item:hover, .nav-home:hover { background: color-mix(in srgb, var(--nz-on-surface) 6%, transparent); }
        .nav-item:active, .nav-home:active { background: color-mix(in srgb, var(--nz-on-surface) 14%, transparent); color: var(--nz-on-surface); }
        .nav-item.current, .nav-home.current-home, .nav-item.current:hover, .nav-home.current-home:hover {
            background: color-mix(in srgb, var(--nz-on-surface) 10%, transparent); color: var(--nz-on-surface); font-weight: 400;
        }
        .sidebar-footer { gap: 8px; padding: 10px 12px 0; border-top: 1px solid var(--adwaita-pane-edge); }
        .main-scroll { background: var(--nz-surface); }
        .main { gap: 12px; padding: 24px 32px 36px; }
        .catalog-intro .page-title { font-size: 26px; font-weight: 800; }
        .description, .page-type { color: var(--nz-muted); }
        .page-title { font-size: 26px; font-weight: 800; }
        .group-head { margin-top: 18px; gap: 8px; }
        .group-glyph { display: none; }
        .group-title { font-size: 15px; font-weight: 700; }
        .cards {
            display: flex; flex-direction: column; gap: 0; border-radius: 12px; background: var(--adwaita-card); overflow-x: hidden; overflow-y: hidden;
            box-shadow: 0 0 0 1px rgba(0, 0, 6, 0.03), 0 1px 3px 1px rgba(0, 0, 6, 0.07), 0 2px 6px 2px rgba(0, 0, 6, 0.03);
        }
        .catalog-card, .catalog-card:hover, .catalog-card:active {
            align-items: stretch; justify-content: center; min-height: 58px; padding: 8px 14px; border: 0; border-radius: 0; background: transparent; box-shadow: none;
            border-bottom: 1px solid var(--adwaita-row-edge); color: var(--nz-on-surface); font-weight: 400;
        }
        .catalog-card > ButtonLabel { display: grid; grid-template-columns: 24px minmax(0px, 1fr); grid-template-rows: auto auto; column-gap: 12px; row-gap: 2px; align-items: center; flex: 1; min-width: 0; }
        .catalog-card:hover { background: color-mix(in srgb, var(--nz-on-surface) 3%, transparent); }
        .catalog-card:active { background: color-mix(in srgb, var(--nz-on-surface) 8%, transparent); }
        .catalog-card:last-child { border-bottom-width: 0; }
        .card-glyph { grid-row: 1 / 3; width: 16px; height: 16px; font-size: 16px; }
        .card-title { align-self: end; font-weight: 400; }
        .card-description { align-self: start; color: var(--nz-muted); font-size: 13px; }
        .example-card {
            margin-top: 6px; border: 0; border-radius: 12px; background: var(--adwaita-card); overflow-x: hidden; overflow-y: hidden;
            box-shadow: 0 0 0 1px rgba(0, 0, 6, 0.03), 0 1px 3px 1px rgba(0, 0, 6, 0.07), 0 2px 6px 2px rgba(0, 0, 6, 0.03);
        }
        .example-card > Separator { background: var(--adwaita-row-edge); }
        .section-title, .source-heading { padding: 14px 18px 0; font-weight: 700; }
        .preview { padding: 12px 18px 20px; }
        .code { padding: 12px 18px 18px; font-family: "Adwaita Mono", monospace; font-size: 13px; }
    }

    /* Breeze: Dolphin. The header toolbar with the location bar, the Places panel, a white icon view and a status label. */
    .gallery.theme-breeze { grid-template-columns: 220px minmax(0px, 1fr); background: var(--nz-surface); }
    .theme-breeze {
        .shell-toolbar { gap: 4px; min-height: 44px; padding: 6px 8px; background: var(--breeze-header); border-bottom: 1px solid var(--breeze-separator); }
        .shell-nav { width: 30px; height: 30px; min-width: 0; min-height: 0; padding: 0; border-color: transparent; background: transparent; box-shadow: none; }
        .shell-nav:hover:not(:disabled) { border-color: var(--nz-accent); background: color-mix(in srgb, var(--nz-accent) 15%, transparent); }
        .shell-nav:active:not(:disabled) { background: color-mix(in srgb, var(--nz-accent) 30%, transparent); color: var(--nz-on-surface); }
        .gallery-glyph::before { display: none; }
        .gallery-glyph::after { color: currentColor; }
        .shell-address { min-height: 30px; margin: 0 8px; padding: 0 4px; border-radius: 3px; background: var(--breeze-location); }
        .shell-breadcrumb { font-size: inherit; }
        .shell-search { flex-basis: 200px; }
        .sidebar { gap: 6px; padding: 6px 0; background: var(--nz-surface); border-right: 1px solid var(--breeze-separator); }
        .sidebar-tree, .nav-places, .navigation { gap: 0; }
        .nav-places { padding-bottom: 4px; }
        .nav-category { min-height: 26px; padding: 6px 7px 0; color: var(--breeze-section); font-weight: 400; }
        .nav-folder { display: none; }
        .nav-item, .nav-home {
            min-height: 30px; margin: 0 6px 0 4px; padding: 0 8px; gap: 8px; border: 1px solid transparent; border-radius: 3px;
            background: transparent; box-shadow: none; color: var(--nz-on-surface); font-weight: 400;
        }
        .nav-item:hover, .nav-home:hover { border-color: color-mix(in srgb, var(--nz-accent) 60%, transparent); background: color-mix(in srgb, var(--nz-accent) 15%, transparent); }
        .nav-item:active, .nav-home:active { background: color-mix(in srgb, var(--nz-accent) 30%, transparent); color: var(--nz-on-surface); }
        .nav-item.current, .nav-home.current-home, .nav-item.current:hover, .nav-home.current-home:hover {
            border-color: var(--nz-accent); background: color-mix(in srgb, var(--nz-accent) 30%, transparent); color: var(--nz-on-surface); font-weight: 400;
        }
        .sidebar-footer { gap: 6px; padding: 8px 8px 0; border-top: 1px solid var(--breeze-separator); }
        .main-scroll { background: var(--breeze-view); }
        .main-scroll.page { background: var(--nz-surface); }
        .main { gap: 10px; padding: 18px 22px; }
        .catalog-intro .page-title, .page-title { font-size: 20px; font-weight: 400; }
        .description, .page-type { color: var(--nz-muted); }
        .group-head { margin-top: 10px; gap: 8px; }
        .group-glyph { display: none; }
        .group-title { font-size: inherit; font-weight: 700; }
        .cards { grid-template-columns: repeat(auto-fill, 128px); gap: 6px; }
        .catalog-card .glyph-tile::before { display: flex; color: var(--hue); }
        .catalog-card .glyph-tile::after { display: none; }
        .catalog-card, .catalog-card:active {
            align-items: center; gap: 6px; padding: 8px 4px; border: 1px solid transparent; border-radius: 3px; background: transparent; box-shadow: none;
            color: var(--nz-on-surface); text-align: center; font-weight: 400;
        }
        .catalog-card:hover { border-color: color-mix(in srgb, var(--nz-accent) 60%, transparent); background: color-mix(in srgb, var(--nz-accent) 15%, transparent); }
        .catalog-card:active { border-color: var(--nz-accent); background: color-mix(in srgb, var(--nz-accent) 30%, transparent); }
        .card-glyph { width: 48px; height: 48px; font-size: 44px; }
        .card-title { font-weight: 400; }
        .card-description { display: none; }
        .example-card { margin-top: 4px; border: 1px solid var(--breeze-separator); border-radius: 5px; background: var(--breeze-view); }
        .example-card > Separator { background: var(--breeze-separator); }
        .section-title, .source-heading { padding: 10px 14px 0; font-weight: 700; }
        .preview { padding: 10px 14px 16px; }
        .code { padding: 10px 14px 14px; font-family: "Hack", "Noto Sans Mono", "DejaVu Sans Mono", monospace; }
        .shell-status { border-top: 1px solid var(--breeze-separator); background: var(--breeze-view); }
        .shell-status StatusBarSection { border: 1px solid var(--breeze-separator); border-bottom-width: 0; border-left-width: 0; border-radius: 0 3px 0 0; background: var(--nz-surface); }
        .shell-status StatusBarSection[flexible] { flex: 0 1 auto; }
    }
}
