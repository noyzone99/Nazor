# DevTools icon

`nazor-devtools.svg` pairs the Nazor four-leaf mark with a compact mint component tree on a graphite tile.

`nazor-devtools.png` is the 512-pixel raster embedded in the inspector assembly for the native window icon and the shell header. `nazor-devtools.ico` contains 32-bit PNG images at 16, 20, 24, 32, 40, 48, 64, 128 and 256 pixels and is the Windows apphost icon. Regenerate both derived files from the SVG after changing the artwork; normal builds do not require an image conversion dependency. Embedding the PNG keeps it available in both Windows and Linux inspector bundles without an external asset path.

# Interface icons

`Icons/*.svg` are the inspector's own 16 × 16 stroke icons, drawn for this tool. They are embedded resources and shown through `Icon Source`, which tints every pixel with the current text color; keep them monochrome (black stroke/fill on transparent) so they follow the light, dark and high-contrast schemes. `InspectorIcons` loads them by file name.
