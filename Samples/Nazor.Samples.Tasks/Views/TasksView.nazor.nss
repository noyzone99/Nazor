/* A flex child needs min-height: 0 to shrink below its content height; otherwise the list pushes the footer out of view.
   Each component owns its structural layout instead of relying on a global class with the same name. */
.page { padding: 28px; }
.column { display: flex; flex-direction: column; gap: 14px; }
.row { display: flex; align-items: center; gap: 12px; }
.spacer { flex: 1; min-width: 0; }
.panel { padding: 20px; background: var(--nz-surface); border: 1px solid var(--nz-border); border-radius: 10px; }
.label { font-weight: 600; }
.muted { color: var(--nz-muted); }
.workspace {
	flex: 1;
	min-height: 0;
}
.summary {
	padding: 8px 12px;
	background: color-mix(in srgb, var(--nz-accent) 12%, var(--nz-surface));
	color: var(--nz-accent);
	border-radius: 6px;
}
.filters {
	margin-top: 4px;
}
.search {
	width: 260px;
}
.empty {
	padding: 32px;
}
.status {
	padding-top: 8px;
}

@media (max-width: 840px) {
	.filters {
		flex-direction: column;
		align-items: stretch;
	}
	.search {
		width: 100%;
	}
}
