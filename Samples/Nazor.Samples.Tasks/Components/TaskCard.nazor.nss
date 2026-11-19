/* Only presentation changes here. Selection is supplied by the parent, and the title can shrink without pushing actions out. */
.task-card { padding: 12px; margin-bottom: 10px; background: var(--nz-surface); border: 1px solid var(--nz-border); border-radius: 8px; }
.selected { background: color-mix(in srgb, var(--nz-accent) 12%, var(--nz-surface)); border-color: var(--nz-accent); }
.task-title { background: transparent; border-width: 0; text-align: left; justify-content: flex-start; }
.row { display: flex; align-items: center; gap: 12px; }
.spacer { flex: 1; min-width: 0; }
.muted { color: var(--nz-muted); }
