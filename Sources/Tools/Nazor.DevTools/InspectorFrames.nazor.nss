/* A frame timeline like the Performance panel of browser tools, on NazorLeaf panes: one leaf bar per completed target
   frame against the 60 fps budget, then the frame list and the selected frame side by side. */
.frames-panel { display: flex; flex-direction: column; gap: 8px; flex: 1; height: 100%; min-height: 0; }
.timeline-pane {
    display: flex; flex-direction: column; flex-shrink: 0; border-radius: var(--leaf-r-pane); background: var(--leaf-pane);
    box-shadow: var(--leaf-sh-pane), inset 0 0 0 1px var(--leaf-stroke);
}
.frames-panel > .timeline-pane:last-child { flex: 1; }
.perf-toolbar { display: flex; align-items: center; gap: 8px; min-height: 46px; padding: 6px 10px 2px 20px; }
.perf-title { margin-right: 6px; font-family: var(--leaf-font-display); font-size: 16px; font-weight: 700; letter-spacing: -0.01em; }
.perf-stat { padding: 2px 9px 3px; border-radius: 9px 3px 9px 3px; background: var(--leaf-s2); color: var(--leaf-text-2); font-family: var(--dt-mono); font-size: 11.5px; white-space: nowrap; }
.perf-toolbar > .provenance { max-width: 42%; }
.chart { position: relative; flex-shrink: 0; height: 132px; margin: 20px 20px 16px; padding: 0 2px; border-radius: 14px 4px 14px 4px; background: var(--leaf-s1); }
.chart-bars { display: flex; align-items: flex-end; gap: 2px; height: 100%; padding: 8px 6px 0; }
.bar-slot {
    display: flex; align-items: flex-end; flex: 1; min-width: 2px; max-width: 16px; height: 100%; border-radius: 6px 2px 0 0; cursor: pointer;
    transition: background-color 150ms;
}
.bar-slot:hover { background: var(--leaf-hover); }
.bar-slot.selected { background: var(--leaf-press); }
.bar { width: 100%; border-radius: 5px 1px 0 0; background: var(--dt-good); transition: height 300ms cubic-bezier(0.16, 1, 0.3, 1); }
.bar.slow { background: var(--dt-slow); }
.bar.janky { background: var(--dt-janky); }
.bar-slot.selected > .bar { background: var(--leaf-primary); box-shadow: var(--leaf-glow); }
/* A dashed budget line, drawn as a repeated gradient tile. */
.budget {
    position: absolute; left: 6px; right: 6px; height: 1px; pointer-events: none;
    background-image: linear-gradient(to right, var(--leaf-ok) 60%, transparent 60%); background-size: 7px 1px; background-repeat: repeat-x;
}
.budget.slow { background-image: linear-gradient(to right, var(--leaf-warn) 60%, transparent 60%); }
.budget-label {
    position: absolute; right: 4px; top: -17px; padding: 1px 7px 2px; border-radius: 7px 2px 7px 2px; background: var(--leaf-pane);
    color: var(--leaf-text-3); font-family: var(--dt-mono); font-size: 10px;
}

.frames-split { flex: 1; min-height: 0; width: 100%; overflow-x: visible; overflow-y: visible; }
.frames-split > SplitViewContent, .frames-split > SplitViewPane {
    min-height: 0; border-width: 0; border-radius: var(--leaf-r-pane); background: var(--leaf-pane); overflow-x: hidden; overflow-y: hidden;
    box-shadow: var(--leaf-sh-pane), inset 0 0 0 1px var(--leaf-stroke);
}
.frames-split > ResizeHandle { width: 10px; background: transparent; }
.frames-split > ResizeHandle:hover, .frames-split > ResizeHandle[dragging] {
    background-color: transparent;
    background-image: linear-gradient(var(--leaf-primary), var(--leaf-primary)); background-size: 3px 44px; background-position: 50% 50%; background-repeat: no-repeat;
}
ListView.frame-list { flex: 1; height: 100%; min-height: 0; padding: 8px; border-radius: 0; background: transparent; font-family: var(--dt-mono); font-size: 12.5px; }
.frame-list ::deep ListViewItem { padding: 0 12px; border-radius: 9px 2px 9px 2px; }
.frame-scroll { flex: 1; min-height: 0; overflow-y: auto; }
.frame-details { display: flex; flex-direction: column; gap: 10px; padding: 16px 12px 16px; }
.frame-heading { display: flex; align-items: center; justify-content: space-between; padding: 0 6px; }
.frame-title { font-family: var(--leaf-font-display); font-size: 20px; font-weight: 750; letter-spacing: -0.02em; }
.frame-total { padding: 2px 10px 3px; border-radius: 9px 3px 9px 3px; background: var(--leaf-ok-c); color: var(--leaf-on-ok-c); font-family: var(--dt-mono); font-weight: 650; }
.frame-total.slow { background: var(--leaf-warn-c); color: var(--leaf-on-warn-c); }
.frame-total.janky { background: var(--leaf-err-c); color: var(--leaf-on-err-c); }
.frame-details > .muted, .frame-details > .chips { padding: 0 6px; }
.breakdown { display: flex; gap: 2px; height: 12px; margin: 2px 0 8px; overflow: hidden; border-radius: 7px 2px 7px 2px; background: var(--leaf-s3); }
.segment { min-width: 3px; }
.style { background: var(--dt-phase-style); }
.layout { background: var(--dt-phase-layout); }
.display { background: var(--dt-phase-display); }
.overlay { background: var(--dt-phase-overlay); }
.raster { background: var(--dt-phase-raster); }
.total { background: var(--leaf-text); }
.legend { display: flex; align-items: center; gap: 7px; }
.legend-dot { width: 7px; height: 11px; flex-shrink: 0; border-radius: var(--leaf-r-seed); }
