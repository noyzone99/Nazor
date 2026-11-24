/* The inspector on NazorLeaf's window material: one strip holds the picker, the panel tabs and the connection controls,
   as in browser developer tools; each panel sets its tree and its details on two leaf panes; a quiet status line closes
   the window. The shell paints the tray itself, so the panes read the same with or without the
   app-drawn title bar. */

.shell {
    position: relative; display: flex; flex-direction: column; flex: 1 1 auto; width: 100%; height: 100%; min-height: 0; padding: 0 10px;
    background-color: var(--leaf-tray);
    color: var(--leaf-text); font-family: var(--leaf-font-ui); font-size: 13.5px;
}

/* ---- Strip: picker, panel tabs, connection ---- */
.panels { flex: 1; min-height: 0; width: 100%; }
.panels > TabsHeader { align-items: center; min-height: 52px; padding: 0 320px 0 50px; }
.panels TabsList { padding: 3px; background: color-mix(in srgb, var(--leaf-s2) 85%, transparent); box-shadow: inset 0 0 0 1px var(--leaf-stroke); border-radius: 14px 4px 14px 4px; }
.panels Tab { min-height: 32px; padding: 0 14px 0 12px; gap: 7px; font-size: 13px; }
.tab-icon { width: 16px; height: 16px; }
.panels Tab[selected] .tab-icon { color: var(--leaf-ink); }
.panels > TabsPanel { display: flex; flex-direction: column; gap: 8px; flex: 1; min-height: 0; padding: 0 0 6px; }

.toolbar-start { position: absolute; left: 10px; top: 0; height: 52px; display: flex; align-items: center; }
.toolbar-end { position: absolute; right: 10px; top: 0; height: 52px; display: flex; align-items: center; gap: 2px; }
Button.picker.active, Button.picker.active:hover { background: var(--leaf-pc); color: var(--leaf-on-pc); }
Button.target-button {
    gap: 8px; max-width: 250px; min-height: 30px; margin-right: 6px; padding: 0 10px 0 12px; border-radius: 15px 4px 15px 4px; font-size: 12.5px; font-weight: 550;
}
Button.target-button:hover { border-radius: 15px 4px 15px 4px; }
.target-name { min-width: 0; white-space: nowrap; overflow: hidden; text-overflow: ellipsis; }
.chevron { width: 12px; height: 12px; color: var(--leaf-text-3); }

/* ---- Notices above a panel ---- */
Banner.banner { min-height: 40px; padding: 4px 6px 4px 12px; border-radius: var(--leaf-r-ovl); font-size: 13px; }

/* ---- Two leaf panes: tree and details ---- */
.panel-split { flex: 1; min-height: 0; width: 100%; overflow-x: visible; overflow-y: visible; }
.panel-split > SplitViewContent, .panel-split > SplitViewPane {
    min-height: 0; border-width: 0; border-radius: var(--leaf-r-pane); background: var(--leaf-pane); overflow-x: hidden; overflow-y: hidden;
    box-shadow: var(--leaf-sh-pane), inset 0 0 0 1px var(--leaf-stroke);
}
.panel-split > ResizeHandle { width: 10px; background: transparent; }
.panel-split > ResizeHandle:hover, .panel-split > ResizeHandle[dragging] {
    background-color: transparent;
    background-image: linear-gradient(var(--leaf-primary), var(--leaf-primary)); background-size: 3px 44px; background-position: 50% 50%; background-repeat: no-repeat;
}

.tree-area { display: flex; flex-direction: column; flex: 1; height: 100%; min-height: 0; min-width: 0; }
.tree-toolbar { display: flex; align-items: center; gap: 2px; padding: 12px 10px 6px 14px; }
SearchField.filter { flex: 1; min-width: 80px; margin-right: 6px; }
.tree-options { display: flex; align-items: center; flex-wrap: wrap; gap: 4px 22px; padding: 2px 16px 10px; border-bottom: 1px solid var(--leaf-stroke); }
Switch.option { font-size: 12.5px; color: var(--leaf-text-2); }

TreeView.node-tree { flex: 1; min-height: 0; width: 100%; padding: 6px 8px; border-radius: 0; background: transparent; }
.node-tree ::deep TreeViewItem {
    gap: 4px; padding-left: calc(var(--nz-tree-level, 0) * var(--nz-tree-indent, 14px) + 12px); padding-right: 10px; border-radius: 9px 2px 9px 2px;
}
/* The selected row carries NazorLeaf's seed at its edge. */
.node-tree ::deep TreeViewItem[selected], .node-tree ::deep TreeViewItem[selected]:hover {
    background-color: var(--dt-row-selected); color: var(--leaf-on-pc);
    background-image: radial-gradient(closest-side, var(--leaf-primary) 80%, transparent 100%);
    background-size: 5px 11px; background-position: 3px 50%; background-repeat: no-repeat;
}
.node-tree ::deep TreeViewIndentGuide { left: calc(var(--nz-tree-guide, 0) * var(--nz-tree-indent, 14px) + 18px); background: var(--leaf-stroke); }
.row.dim { opacity: 0.55; }

.tree-note {
    display: flex; align-items: center; gap: 8px; margin: 0 10px 8px; padding: 4px 4px 4px 14px; border-radius: var(--leaf-r-ovl);
    background: var(--leaf-pc); color: var(--leaf-on-pc); font-size: 12.5px;
}
.breadcrumbs { display: flex; align-items: center; gap: 2px; min-height: 40px; padding: 0 10px; overflow: hidden; white-space: nowrap; border-top: 1px solid var(--leaf-stroke); }
.breadcrumbs > .muted { padding-left: 6px; }
Button.crumb {
    flex-shrink: 0; min-height: 24px; padding: 0 8px; border-radius: 9px 2px 9px 2px; background: transparent; box-shadow: none;
    color: var(--leaf-text-2); font-family: var(--dt-mono); font-size: 12px; font-weight: 400;
}
Button.crumb:hover { border-radius: 9px 2px 9px 2px; }
Button.crumb.current, Button.crumb.current:hover { background: var(--leaf-pc); color: var(--leaf-on-pc); font-weight: 550; }
.crumb-separator { flex-shrink: 0; padding: 0 1px; color: var(--leaf-text-3); }

/* ---- Status line on the material ---- */
StatusBar.status-bar { min-height: 30px; border-top-width: 0; padding: 0 4px; background: transparent; color: var(--leaf-text-2); font-size: 12px; }
.status-bar > StatusBarSection + StatusBarSection { border-left-width: 0; }
.status-bar > StatusBarSection { gap: 7px; padding: 0 8px; }
.status-strong { color: var(--leaf-text); font-weight: 650; }
.status-target { flex-shrink: 1; color: var(--leaf-text-3); }

/* ---- Connect page: a single pane with the four-leaf mark ---- */
.welcome-scroll {
    flex: 1; min-height: 0; overflow-y: auto; border-radius: var(--leaf-r-pane); background: var(--leaf-pane);
    box-shadow: var(--leaf-sh-pane), inset 0 0 0 1px var(--leaf-stroke);
}
.welcome { display: flex; flex-direction: column; gap: 14px; max-width: 600px; margin: 0 auto; padding: 44px 24px 32px; }
.hero { display: flex; flex-direction: column; align-items: center; gap: 12px; margin-bottom: 10px; text-align: center; }
.welcome-title { font-family: var(--leaf-font-display); font-size: 28px; font-weight: 800; line-height: 1.15; letter-spacing: -0.03em; }
.welcome-lead { max-width: 460px; color: var(--leaf-text-2); font-size: 14px; line-height: 1.55; }
.card { display: flex; flex-direction: column; gap: 8px; padding: 16px 18px 18px; border-radius: var(--leaf-r-ovl); background: var(--leaf-s1); }
.card-heading { display: flex; align-items: center; justify-content: space-between; }
.card-title { font-family: var(--leaf-font-display); font-size: 15px; font-weight: 700; letter-spacing: -0.01em; }
.card-actions { display: flex; justify-content: flex-end; margin-top: 6px; }
Button.target-row {
    justify-content: flex-start; gap: 12px; min-height: 52px; padding: 0 14px; border-radius: var(--leaf-r-item); background: var(--leaf-pane);
    box-shadow: inset 0 0 0 1px var(--leaf-stroke); font-weight: 400; text-align: left;
}
Button.target-row:hover { border-radius: var(--leaf-r-item); }
Button.target-row.selected, Button.target-row.selected:hover {
    background: color-mix(in srgb, var(--leaf-primary) 12%, var(--leaf-pane)); box-shadow: inset 0 0 0 1.5px color-mix(in srgb, var(--leaf-primary) 45%, transparent);
}
.target-icon { width: 20px; height: 20px; color: var(--leaf-ink); }
.target-copy { display: flex; flex-direction: column; align-items: flex-start; gap: 1px; }
.target-title { font-weight: 650; }
.step { display: flex; align-items: center; gap: 12px; color: var(--leaf-text-2); font-size: 13.5px; }
.step-number {
    width: 22px; height: 22px; flex-shrink: 0; border-radius: 9px 2px 9px 2px; background: var(--leaf-pc); color: var(--leaf-on-pc);
    text-align: center; line-height: 22px; font-size: 11.5px; font-weight: 700;
}
.code {
    align-self: flex-start; margin-left: 34px; padding: 7px 12px; border-radius: var(--leaf-r-item); background: var(--leaf-pane);
    box-shadow: inset 0 0 0 1px var(--leaf-stroke); font-family: var(--dt-mono); font-size: 12.5px;
}

/* ---- Popovers ---- */
Popover.connection-flyout, Popover.settings-flyout { width: 340px; max-width: 340px; }
.flyout-heading { display: flex; align-items: center; gap: 10px; }
.flyout-heading-copy { display: flex; flex-direction: column; gap: 1px; min-width: 0; }
.flyout-title { font-family: var(--leaf-font-display); font-size: 15px; font-weight: 700; letter-spacing: -0.01em; }
.field-label { margin-top: 6px; color: var(--leaf-text-2); font-size: 12px; font-weight: 650; }
.flyout-row { display: flex; align-items: center; gap: 4px; }
Select.target-choice { flex: 1; min-width: 0; }
.flyout-actions { display: flex; flex-wrap: wrap; gap: 6px; margin-top: 6px; }
.flyout-actions > Button { min-height: 30px; padding: 0 12px; font-size: 13px; }
SegmentedControl.scheme-choice { align-self: stretch; }
Switch.setting { align-self: stretch; font-size: 13px; }
.shortcuts { display: grid; grid-template-columns: max-content 1fr; gap: 7px 14px; align-items: center; color: var(--leaf-text-2); font-size: 12.5px; }
.keys { display: flex; align-items: center; gap: 3px; }
