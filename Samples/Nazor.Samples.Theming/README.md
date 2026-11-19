# Theming: separate layout, theme, and preferences

```powershell
dotnet run --project Samples/Nazor.Samples.Theming
```

Change the theme while editing the same controls. Their values remain in the component state: switching a stylesheet does not recreate the component tree.

The custom title bar participates in the same theme, with native system buttons and measured insets. Nazor places it on the tray, the most tinted surface of the window; try changing the OS accent while the sample is running. Native caption commands and Snap support remain available.

| Choice | What it changes |
| --- | --- |
| Basic / Fluent / None | Basic supplies control geometry, focus cues and system colors. Fluent adds WinUI colors and motion. None uses the sample stylesheet alone. |
| System / Light / Dark | The application-wide `IApplicationAppearance.ColorScheme` preference. System resumes following platform changes. |
| Custom accent | Overrides the theme accent pair with NSS tokens. |
| Backdrop | A typed portable preset or Windows/Linux-specific native material request. |

Read `Program.cs` before `App.nazor`: the application theme is `None`, and `Window.StyleSheet` initially selects Basic, then the chosen theme for comparison.
This prevents an underlying theme from contributing unwanted lower-priority decorations.
An ordinary app normally selects `app.Appearance.Theme` once, uses `app.Appearance.StyleSheet` for application styles,
and keeps component layout in sidecars.

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
