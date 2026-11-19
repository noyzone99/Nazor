.page { padding: 28px; }
.column { display: flex; flex-direction: column; gap: 14px; }
.panel { padding: 20px; background: var(--nz-surface); border: 1px solid var(--nz-border); border-radius: 10px; }
.label { font-weight: 600; }
/* As many columns of at least 260px as fit the window: the panels wrap when it narrows. */
.features {
	display: grid;
	grid-template-columns: repeat(auto-fill, minmax(260px, 1fr));
	gap: 14px;
}

.feature {
	display: flex;
	flex-direction: column;
	gap: 8px;
	position: relative;
}

/* Out of the flow, in the top right corner of its panel. */
.badge {
	position: absolute;
	top: 12px;
	right: 12px;
	padding: 2px 8px;
	border-radius: 10px;
	background: var(--nz-accent);
	color: #ffffff;
	font-size: 12px;
}
