/* Rules read like a stylesheet, the strongest first, as in the Styles pane of browser tools; each rule is a small
   NazorLeaf card on the pane. */
.panel-scroll { flex: 1; }
.style-content { display: flex; flex-direction: column; gap: 8px; padding: 6px 12px 14px; font-family: var(--dt-mono); font-size: 12.5px; }
.rule { display: flex; flex-direction: column; padding: 10px 14px 11px; border-radius: var(--leaf-r-ovl); background: var(--leaf-s1); }
.rule.inactive { opacity: 0.5; }
.media { margin-bottom: 3px; font-size: 11.5px; color: var(--leaf-text-3); }
.rule-header { display: flex; align-items: flex-start; min-width: 0; }
.selector { color: var(--leaf-text); font-weight: 600; overflow-wrap: anywhere; min-width: 0; }
.brace { color: var(--leaf-text-3); white-space: pre; }
.source {
    margin-left: 12px; padding: 0 7px; border-radius: 7px 2px 7px 2px; background: var(--leaf-pane);
    color: var(--leaf-ink); font-family: var(--leaf-font-ui); font-size: 11px; font-weight: 550; line-height: 18px; white-space: nowrap;
}
.declaration { display: flex; align-items: flex-start; flex-wrap: wrap; min-height: 20px; padding-left: 18px; }
.declaration.overridden { color: var(--leaf-text-3); text-decoration-line: line-through; }
.property-name { color: var(--dt-css-property); white-space: pre; }
.declaration.overridden > .property-name, .declaration.overridden > .property-value { color: var(--leaf-text-3); }
.colon { color: var(--leaf-text-3); white-space: pre; }
.property-value { color: var(--leaf-text); overflow-wrap: anywhere; min-width: 0; }
.swatch { width: 11px; height: 11px; margin-right: 6px; border-radius: 4px 1px 4px 1px; box-shadow: inset 0 0 0 1px var(--leaf-stroke-2); align-self: center; }
.rule-meta { margin-top: 6px; font-family: var(--leaf-font-ui); font-size: 10.5px; color: var(--dt-syntax-meta); }
