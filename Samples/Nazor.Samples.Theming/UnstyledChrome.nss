:root[role="window"] { background: Canvas; color: CanvasText; }
:root[frame-owner="nazor"] { -window-frame-border: 1px solid ButtonBorder; }
TitleBar {
    display: flex;
    align-items: center;
    gap: 8px;
    min-height: 32px;
    padding-left: 8px;
    border-bottom: 1px solid ButtonBorder;
}
TitleBarCenter { flex: 1; }
CaptionButtons { display: flex; }
CaptionButtonsButton { display: flex; align-items: center; justify-content: center; width: 32px; height: 32px; }
CaptionButtonsButton:hover { background: Highlight; color: HighlightText; }
CaptionButtonsGlyph { position: relative; width: 10px; height: 10px; }
CaptionButtonsGlyph[kind="maximize"], CaptionButtonsGlyph[kind="restore"] { border: 1px solid currentColor; }
CaptionButtonsGlyph[kind="restore"] { box-shadow: 2px -2px 0 currentColor; }
CaptionButtonsGlyphStroke { position: absolute; top: 4px; width: 10px; height: 1px; background: currentColor; }
CaptionButtonsGlyphStroke[stroke="rising"] { transform: rotate(-45deg); }
CaptionButtonsGlyphStroke[stroke="falling"] { transform: rotate(45deg); }
:is(#theme-picker, #titlebar-owner, #frame-owner), SelectPopup, #apply-decorations { border: 1px solid ButtonBorder; background: Canvas; color: CanvasText; }
:is(#theme-picker, #titlebar-owner, #frame-owner) { display: flex; align-items: center; gap: 8px; }
:is(#theme-picker, #titlebar-owner, #frame-owner) > SelectChevron { width: 10px; height: 10px; }
SelectPopup { display: flex; flex-direction: column; }
:is(#theme-picker, #titlebar-owner, #frame-owner), SelectOption, #apply-decorations { padding: 6px; }
#apply-decorations:disabled { color: GrayText; }
SelectOption:hover, SelectOption:focus { background: Highlight; color: HighlightText; }
:focus-visible { outline: 1px solid CanvasText; }
