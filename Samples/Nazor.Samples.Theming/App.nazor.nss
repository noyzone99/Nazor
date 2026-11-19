/* Component layout stays separate from the complete control theme selected by Window.StyleSheet.
   The same controls can keep their state while theme geometry and colors are recomputed. */
:root {
	display: flex;
	flex-direction: column;
}

.caption-title { padding: 0 12px; font-weight: 600; }

/* Override only the accent: control geometry and states still come from the selected theme. */
:root.custom-accent {
	--nz-accent: #6941c6;
	--nz-on-accent: #ffffff;
}


@media (forced-colors: active) {
	:root.custom-accent {
		--nz-accent: Highlight;
		--nz-on-accent: HighlightText;
	}
}

/* Scrolling is an explicit layout choice on an ordinary Container. */
.scroll-fill {
	flex: 1;
	min-height: 0;
	overflow-y: auto;
}

.page {
	display: flex;
	flex-direction: column;
	gap: 16px;
	padding: 24px;
}
.title {
	font-size: 28px;
	font-weight: 700;
}
.heading {
	font-size: 18px;
	font-weight: 600;
}
.muted {
	color: var(--nz-muted, CanvasText);
}

/* Generated boxes decorate the real, accessible text; they do not supply its name. */
.generated-preview { display: flex; align-items: center; gap: 8px; }
.generated-preview::before { content: ""; width: 4px; height: 16px; background-color: var(--nz-accent, Highlight); }
.generated-preview::after { content: " (" "NSS)"; color: var(--nz-muted, GrayText); }
.project-name ::deep TextFieldInput::placeholder { color: var(--nz-muted, GrayText); font-style: italic; }
.project-name ::deep TextFieldInput::selection { color: HighlightText; background-color: Highlight; }
.swatch:nth-child(even of .swatch) { border-left: 2px solid currentColor; }
.settings {
	display: grid;
	grid-template-columns: repeat(3, minmax(0px, 1fr));
	gap: 24px;
}
.section {
	display: flex;
	flex-direction: column;
	gap: 10px;
}
.panels {
	display: flex;
	gap: 16px;
}
.panel {
	display: flex;
	flex-direction: column;
	flex: 1;
	min-width: 0;
	gap: 12px;
	padding: 20px;
	background: var(--nz-surface, Canvas);
	border: 1px solid var(--nz-border, ButtonBorder);
	border-radius: 8px;
}
.row {
	display: flex;
	flex-wrap: wrap;
	gap: 8px;
}
.swatch {
	padding: 10px 12px;
	border: 1px solid var(--nz-border, ButtonBorder);
}
.canvas {
	background: Canvas;
	color: CanvasText;
}
.field {
	background: Field;
	color: FieldText;
}
.highlight {
	background: Highlight;
	color: HighlightText;
}
.accent {
	background: AccentColor;
	color: AccentColorText;
}

@media (max-width: 720px) {
	.settings {
		grid-template-columns: 1fr;
	}
	.panels {
		flex-direction: column;
	}
}

TextField::part(input):focus { color: Highlight; }
