# Theming

```powershell
dotnet run --project Samples/Nazor.Samples.Theming
```

Pick a theme to change the title bar, caption buttons and exterior frame of the real window. Open the comparison window and click between the two to see active and inactive captions. Drag a caption or grab a frame edge to try moving and resizing. Edit the controls before switching and check that their values survive.

At startup the main window prefers `WindowDecorations.Nazor()` and reports the accepted configuration. Choose **Theme** or **System** for the title bar and frame, then select **Apply decorations**. The sample checks the pair and the live transition with `Window.Capabilities.Decorations.GetChangeSupport`. It explains unsupported choices and uses `WindowDecorationPolicy.Require` for changes, including requests that still need backend negotiation.

A system title bar requires a system frame. A theme title bar on a system frame uses theme caption buttons and is available on Windows. The Wayland and X11 backends require both parts to use the same provider and may restrict changes while a window is open. Corners and shadows apply to normal Nazor-framed windows and depend on backend support. [Decoration combinations and capabilities](../../Docs/Guide/14-Title-Bars-and-Platform-Integration.md#switch-the-frame-while-open)

A new comparison window prefers the main window's accepted decorations. Open it again after changing the pair to compare both windows with that pair. The theme and color scheme continue to update across open windows through `IApplicationAppearance`.

| Choice | What it changes |
| --- | --- |
| Theme | Basic, Fluent, NazorLeaf, Adwaita, Breeze, Windows 9x, Windows XP, Windows 7 and CDE. None uses sample styling for layout, the window controls and the return path through the picker. |
| System / Light / Dark | The application-wide `IApplicationAppearance.ColorScheme` preference. System resumes following platform changes. |
| Custom accent | Overrides the theme accent pair with NSS tokens. |
| Backdrop | A typed portable preset or Windows/Linux-specific native material request. |

Read `App.nazor` for theme selection and `Program.cs` for packaged font registration. The application starts with Basic and switches `Appearance.Theme` so automatic title-bar presenters and NSS rules follow the same selection. Each theme styles the root window through the `-window-frame-*` properties. NazorLeaf also supplies its own title-bar and button presenters.

`App.nazor.nss` owns page layout independently of control themes. Its system-color swatches pair backgrounds with their text colors:
`Canvas`/`CanvasText`, `Field`/`FieldText`, `Highlight`/`HighlightText`, and `AccentColor`/`AccentColorText`.
These names read the system palette rather than meaning "light" or "dark".
The preview also demonstrates decorative `::before`/`::after` content and scoped input-part `::placeholder`/`::selection` styles. Clear the project name to see its placeholder; select part of the value to see system highlight colors. Generated labels are decorative; the actual content and `AccessibleName` provide the accessible name.
The system-color swatches use `:nth-child(even of .swatch)` to count only swatch siblings, independently of the headings and explanatory text around them.
The forced-colors rule uses system highlight and text colors so a custom accent does not override the user's contrast choices.

`CommonBackdrop.None` disables compositor effects. `Blur` maps to Acrylic on Windows and background blur on Linux;
`Solid` maps to Mica on Windows and no compositor effect on Linux.
`WindowsBackdrop` selects Acrylic, Mica, or MicaAlt; `LinuxBackdrop` selects Blur.
Native materials require backend and system support: Windows 11 build 22621 or later for Windows materials,
or a Wayland compositor advertising background blur for Linux.
Nazor keeps an opaque theme background if no material is activated. The panels remain opaque, exposing the material only in the gaps.
Visibility also depends on the system's transparency preferences.

Try each theme in light and dark modes, enable the custom accent, and switch backdrops without resetting input values.
Check hover, pressed states, Tab navigation, disabled controls, and high-contrast mode.
Below 720 logical pixels, selectors and preview panels stack vertically and the page scrolls.
