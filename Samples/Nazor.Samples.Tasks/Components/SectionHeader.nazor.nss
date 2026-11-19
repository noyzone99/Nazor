/* The heading grows into the free row space, leaving the caller's ChildContent visible at the trailing edge. */
.section-header { gap: 20px; margin-bottom: 8px; }
.row { display: flex; align-items: center; }
.column { display: flex; flex-direction: column; gap: 14px; }
.spacer { flex: 1; min-width: 0; }
.title { font-size: 28px; font-weight: 700; }
.muted { color: var(--nz-muted); }
