/* The box model nests margin, border, padding and content like the Computed pane of browser tools, in the overlay
   colors the app draws and with NazorLeaf's leaf corners on the outer box. */
.computed-panel { display: flex; flex-direction: column; height: 100%; width: 100%; min-height: 0; overflow-y: auto; }
.box-section { display: flex; flex-direction: column; gap: 12px; flex-shrink: 0; margin: 6px 12px 0; padding: 16px; border-radius: var(--leaf-r-ovl); background: var(--leaf-s1); }
.box {
    position: relative; display: flex; flex-direction: column; align-items: center; padding: 1px 4px;
    font-family: var(--dt-mono); font-size: 11px; color: #202124; border: 1px dashed rgba(32, 33, 36, 0.45);
}
.margin-box { align-self: center; min-width: 250px; border-radius: 16px 4px 16px 4px; background: #f9cc9d; }
.border-box { align-self: stretch; border-radius: 11px 3px 11px 3px; background: #ffeebc; border: 1px solid #202124; }
.padding-box { align-self: stretch; border-radius: 8px 2px 8px 2px; background: #c3d08b; }
.content-box { padding: 7px 12px; min-width: 80px; text-align: center; border-radius: 6px 2px 6px 2px; background: #8cb6c0; border: 1px solid rgba(32, 33, 36, 0.55); }
.box-label { position: absolute; left: 7px; top: 1px; font-family: var(--leaf-font-ui); font-size: 10px; color: rgba(32, 33, 36, 0.7); }
.box-row { display: flex; align-items: center; align-self: stretch; }
.box-row > .box, .box-row > .content-box { flex: 1; }
.edge { min-width: 22px; padding: 1px 2px; text-align: center; line-height: 16px; }
.edge.side { padding: 0 4px; }
.boxes { display: grid; grid-template-columns: max-content 1fr 1fr; gap: 4px 12px; font-size: 11.5px; }
.boxes-heading { color: var(--leaf-text-3); font-weight: 650; }
.boxes-value { font-family: var(--dt-mono); font-size: 11px; white-space: nowrap; }
.computed-toolbar { display: flex; align-items: center; gap: 14px; flex-shrink: 0; padding: 12px 14px 6px; }
SearchField.filter { flex: 1; min-width: 80px; }
Switch.option { font-size: 12.5px; color: var(--leaf-text-2); }
ListView.computed-values { flex: 1; min-height: 200px; padding: 4px 8px 8px; border-radius: 0; background: transparent; }
.computed-values ::deep ListViewItem { padding: 0 10px; border-radius: 9px 2px 9px 2px; }
.value-row { display: flex; align-items: center; width: 100%; min-width: 0; overflow: hidden; font-family: var(--dt-mono); font-size: 12.5px; white-space: pre; }
.property-name { color: var(--dt-css-property); }
.colon { color: var(--leaf-text-3); }
.property-value { color: var(--leaf-text); overflow: hidden; text-overflow: ellipsis; }
.origin { margin-left: 12px; font-family: var(--leaf-font-ui); font-size: 11px; color: var(--leaf-text-3); overflow: hidden; text-overflow: ellipsis; }
.computed-empty { padding: 14px; }
