/* Window-level custom properties inherit into child components even though selectors remain scoped.
   Reuse the base theme tokens so controls retain their hover, focus, and disabled states. */
:root {
	/** <doc>Main surface shared by the Tasks window and its child components.</doc> */
	--nz-surface: #f3f5fa;
	/** <doc>Accent used for primary actions and scrollbar highlights.</doc> */
	--nz-accent: #3658ba;
	--nz-accent-text: #3658ba;
	--nz-on-accent: #ffffff;
	--nz-stroke: #dbe1eb;
	font-family: "Segoe UI";
	font-size: 14px;
	/* These inherited defaults apply across component boundaries without ::deep selectors.
	   ItemList and the lab's "Global defaults" appearance use this same policy; local rules can override it. */
	scrollbar-visibility: autohide;
	scrollbar-width: 8px;
	scrollbar-inset: 2px;
	scrollbar-radius: 4px;
	scrollbar-min-thumb-length: 28px;
	scrollbar-thumb-color: color-mix(in srgb, var(--nz-accent) 70%, transparent);
	scroll-behavior: smooth;
	scroll-duration: 180ms;
	scroll-timing-function: ease-out;
	scroll-wheel-step: 24px;
}
.shell {
	display: flex;
	flex-direction: column;
	height: 100%;
}
.row { display: flex; align-items: center; gap: 12px; }
.spacer { flex: 1; min-width: 0; }
.muted { color: var(--nz-muted); }
.primary {
	background: var(--nz-accent);
	color: var(--nz-on-accent);
	border-color: var(--nz-accent);
	&:hover { background: var(--nz-selection); }
}
.navigation {
	padding: 16px 24px;
	background: var(--nz-surface);
	border-bottom-width: 1px;
	border-color: var(--nz-border);
}
.brand {
	font-size: 16px;
	font-weight: 700;
	color: var(--nz-accent);
	margin-right: 20px;
}
