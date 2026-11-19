# DRM single view

`App.nazor` contains reusable content without `@window`. `Program.cs` chooses a
desktop window for ordinary development, or an explicit DRM single-view host
when passed `--drm`.

```powershell
dotnet run --project Samples/Nazor.Samples.Drm
```

From an active local Linux console with the [DRM runtime dependencies](../../Docs/Guide/34-Linux-DRM.md):

```bash
dotnet run --project Samples/Nazor.Samples.Drm -- --drm
```

Follow the [runtime package instructions for your distribution](../../Docs/Guide/34-Linux-DRM.md#distribution-packages).
The sample uses system fonts, so install at least one font family, such as
DejaVu Sans. Fontconfig alone is insufficient; see [font troubleshooting](../../Docs/Guide/21-Typography-and-Fonts.md#linux-font-discovery).

The checked-in `FolderProfile` publishes a framework-dependent .NET 10 Linux x64
application; install the matching .NET 10 runtime using your distribution's
[.NET installation instructions](https://learn.microsoft.com/en-us/dotnet/core/install/linux).
Publish from the repository:

```powershell
dotnet publish Samples/Nazor.Samples.Drm -p:PublishProfile=FolderProfile
```

Copy the complete publication directory, then start `./Nazor.Samples.Drm --drm`
from the local console.

Use the Increment button, edit text, select/copy/paste, and exit through the Exit
button. Check mouse movement, keyboard layout/dead keys and single-contact touch.
Set `NAZOR_RENDERER=GPU` or `CPU` before launch to verify each rendering path.
Console switching uses Ctrl+Alt+F1 through F12 and requires a VT-bound seat.
The DRM path requires display ownership and must not run inside an existing
desktop compositor. GPU hardware and console behavior require device acceptance.
