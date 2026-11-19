/* Nazor DevTools application sheet, above the NazorLeaf theme: inspector tokens derived from the NazorLeaf palette and
   the vocabulary shared by its panels. Markup reads as in browser developer tools (tags, attributes, strings), in
   colors tuned to the seed so the tool looks like every other NazorLeaf application. */

:root {
    --dt-mono: var(--leaf-font-mono);
    --dt-ui: var(--leaf-font-ui);
    --dt-display: var(--leaf-font-display);
    --dt-divider: var(--leaf-stroke);
    --dt-row-hover: var(--leaf-hover);
    --dt-row-selected: var(--leaf-pc);
    --dt-chip: var(--leaf-s2);

    --dt-syntax-tag: #8e24aa;
    --dt-syntax-attribute: #a14f00;
    --dt-syntax-value: #137333;
    --dt-syntax-text: var(--leaf-text);
    --dt-syntax-component: var(--leaf-ink);
    --dt-syntax-framework: var(--leaf-text-3);
    --dt-syntax-punctuation: var(--leaf-text-3);
    --dt-syntax-meta: color-mix(in srgb, var(--leaf-text-3) 80%, transparent);
    --dt-css-property: #b3261e;

    --dt-good: var(--leaf-ok);
    --dt-slow: var(--leaf-warn);
    --dt-janky: var(--leaf-err);
    --dt-live: var(--leaf-ok);
    --dt-busy: var(--leaf-primary);

    --dt-phase-style: #9b6cd8;
    --dt-phase-layout: #c58af9;
    --dt-phase-display: var(--leaf-g1);
    --dt-phase-overlay: var(--leaf-text-3);
    --dt-phase-raster: var(--leaf-g4);
}

@media (prefers-color-scheme: dark) {
    :root {
        --dt-syntax-tag: #d7aefb;
        --dt-syntax-attribute: #fdd663;
        --dt-syntax-value: #81c995;
        --dt-css-property: #f2b8b5;
    }
}

@media (forced-colors: active) {
    :root {
        --dt-syntax-tag: CanvasText;
        --dt-syntax-attribute: CanvasText;
        --dt-syntax-value: CanvasText;
        --dt-syntax-component: CanvasText;
        --dt-syntax-framework: GrayText;
        --dt-syntax-punctuation: CanvasText;
        --dt-syntax-meta: GrayText;
        --dt-css-property: CanvasText;
        --dt-row-selected: Highlight;
        --dt-chip: transparent;
        --dt-divider: CanvasText;
    }
}

/* Text helpers */
.muted { color: var(--leaf-text-3); }
.small { font-size: 12px; }
.mono { font-family: var(--dt-mono); }
.strong { font-weight: 650; }
.note { margin-top: 6px; }
.spacer { flex: 1; min-width: 0; }
.sep { margin-right: 6px; }

/* Markup syntax, used by tree rows, links and details headers */
.tokens, .row { display: flex; align-items: center; min-width: 0; font-family: var(--dt-mono); font-size: 12.5px; white-space: pre; }
.syntax-tag { color: var(--dt-syntax-tag); white-space: pre; flex-shrink: 0; }
.syntax-attribute { color: var(--dt-syntax-attribute); white-space: pre; flex-shrink: 0; }
.syntax-value { color: var(--dt-syntax-value); white-space: pre; flex-shrink: 0; }
.syntax-text { color: var(--dt-syntax-text); white-space: pre; flex-shrink: 0; }
.syntax-component { color: var(--dt-syntax-component); font-weight: 650; white-space: pre; flex-shrink: 0; }
.syntax-framework { color: var(--dt-syntax-framework); white-space: pre; flex-shrink: 0; }
.syntax-punctuation { color: var(--dt-syntax-punctuation); white-space: pre; flex-shrink: 0; }
.syntax-meta { color: var(--dt-syntax-meta); white-space: pre; flex-shrink: 0; }

/* Icon buttons keep NazorLeaf's ghost leaf; the small step matches the dense tool strips. */
Button[icon-only][size="small"] { width: 28px; min-height: 28px; }
Button[icon-only][size="small"] Icon { width: 17px; height: 17px; }
/* A disabled ghost keeps no fill, so unavailable tools do not read as pressed tiles. */
Button[icon-only]:disabled { background: transparent; box-shadow: none; color: color-mix(in srgb, var(--leaf-text-3) 70%, transparent); }

/* Links to captured nodes: rows of markup that light up like NazorLeaf list items. */
Button.node-link {
    justify-content: flex-start; align-self: stretch; gap: 0; min-height: 28px; padding: 0 8px; overflow: hidden;
    border-radius: var(--leaf-r-item); background: transparent; box-shadow: none; color: var(--leaf-text);
    font-family: var(--dt-mono); font-size: 12.5px; font-weight: 400; white-space: pre;
}
Button.node-link:hover { background: var(--leaf-hover); border-radius: var(--leaf-r-item); }
Button.node-link:active { background: var(--leaf-press); transform: scale(1); }
Button.node-link.inline { align-self: flex-start; min-height: 22px; padding: 0 6px; margin-left: -6px; }
/* A button lays its content out as a block: markup tokens must stay on one line. */
Button.node-link > ButtonLabel { display: flex; align-items: center; min-width: 0; overflow: hidden; white-space: pre; }

/* Small leaf labels: counts, hidden nodes, overridden declarations. */
.badge, .tag {
    flex-shrink: 0; margin-left: 8px; padding: 0 7px; border-radius: 7px 2px 7px 2px; background: var(--dt-chip); color: var(--leaf-text-2);
    font-family: var(--dt-ui); font-size: 10.5px; font-weight: 550; line-height: 17px; white-space: nowrap; text-decoration-line: none;
}
.chips { display: flex; flex-wrap: wrap; gap: 6px; }
.chip { padding: 2px 9px 3px; border-radius: 9px 3px 9px 3px; background: var(--leaf-s2); color: var(--leaf-text); font-family: var(--dt-mono); font-size: 11.5px; }
.state-chip { background: var(--leaf-pc); color: var(--leaf-on-pc); }

/* Details sidebars: a header on the pane, then boxed sections on the pane's first layer. */
.details { display: flex; flex-direction: column; flex: 1; height: 100%; min-height: 0; }
.details-header { display: flex; flex-direction: column; gap: 6px; padding: 16px 18px 12px; }
.header-title { display: flex; align-items: center; gap: 10px; min-width: 0; }
.header-icon { width: 30px; height: 30px; padding: 6px; flex-shrink: 0; border-radius: 12px 3px 12px 3px; background: var(--leaf-pc); color: var(--leaf-on-pc); }
.title-tokens { font-size: 15px; overflow: hidden; }
.selector-hint { color: var(--leaf-text-3); white-space: pre; overflow: hidden; text-overflow: ellipsis; }
.type-name { margin-left: 40px; font-family: var(--dt-mono); font-size: 11.5px; color: var(--leaf-text-3); overflow-wrap: anywhere; }
.header-actions { display: flex; align-items: center; gap: 6px; margin-top: 4px; }
.links { display: flex; flex-wrap: wrap; align-items: center; gap: 2px 10px; margin-left: 40px; font-size: 12px; }
.link-label { color: var(--leaf-text-3); font-size: 11.5px; }
.details-scroll { display: flex; flex-direction: column; gap: 10px; flex: 1; min-height: 0; padding: 2px 12px 14px; overflow-y: auto; }
.panel-scroll { height: 100%; width: 100%; min-height: 0; overflow: auto; }

.section { display: flex; flex-direction: column; gap: 4px; padding: 12px 14px 14px; border-radius: var(--leaf-r-ovl); background: var(--leaf-s1); }
.section.compact { padding: 10px 12px 12px; }
.section-title { margin-bottom: 4px; color: var(--leaf-text-2); font-size: 12px; font-weight: 650; }
.property { display: flex; align-items: flex-start; flex-wrap: wrap; min-height: 21px; font-family: var(--dt-mono); font-size: 12.5px; }
.property-value { overflow-wrap: anywhere; min-width: 0; }
.property > .syntax-value { flex-shrink: 1; min-width: 0; white-space: normal; overflow-wrap: anywhere; }
.property-type { margin-left: 8px; font-size: 11px; color: var(--dt-syntax-meta); }
.unavailable { color: var(--leaf-text-3); font-style: italic; }
.facts { display: grid; grid-template-columns: minmax(96px, max-content) 1fr; gap: 6px 14px; align-items: center; font-size: 12.5px; }
.fact-label { color: var(--leaf-text-3); }
.fact-value { font-family: var(--dt-mono); font-size: 12px; overflow-wrap: anywhere; min-width: 0; }
.properties { display: flex; flex-direction: column; gap: 10px; padding: 12px; }
.text-content { padding: 8px 10px; border-radius: var(--leaf-r-item); background: var(--leaf-pane); font-family: var(--dt-mono); font-size: 12.5px; white-space: pre-wrap; overflow-wrap: anywhere; }

/* Capture line above a detail panel, with its refresh action. */
.pane-toolbar { display: flex; align-items: center; gap: 8px; min-height: 36px; padding: 2px 8px 2px 18px; }
.provenance { flex: 1; min-width: 0; color: var(--leaf-text-3); font-size: 11.5px; white-space: nowrap; overflow: hidden; text-overflow: ellipsis; }

.empty { display: flex; flex-direction: column; gap: 6px; margin: 12px; padding: 18px 16px; border-radius: var(--leaf-r-ovl); background: var(--leaf-s1); font-family: var(--dt-ui); }
.empty-title { font-family: var(--dt-display); font-size: 15px; font-weight: 700; letter-spacing: -0.01em; }
.empty-icon { width: 28px; height: 28px; color: var(--leaf-text-3); }
EmptyState.placeholder { flex: 1; justify-content: center; }

/* Status lights: a seed, the leaf-shaped selection mark of NazorLeaf, in the color of the connection. */
.status-dot { width: 7px; height: 11px; flex-shrink: 0; border-radius: var(--leaf-r-seed); background: var(--leaf-text-3); }
.status-dot.large { width: 9px; height: 14px; border-radius: 6px 2px 6px 2px; }
.status-dot.live { background: var(--dt-live); }
.status-dot.busy { background: var(--dt-busy); }
.status-dot.paused { background: var(--dt-slow); }
.status-dot.warning { background: var(--dt-janky); }
