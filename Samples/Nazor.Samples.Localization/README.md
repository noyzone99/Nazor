# Localization: resources and culture

```powershell
dotnet run --project Samples/Nazor.Samples.Localization
dotnet run --project Samples/Nazor.Samples.Localization -- --culture it-IT --ui-culture it-IT
dotnet run --project Samples/Nazor.Samples.Localization -- --culture it-IT --ui-culture en-US
dotnet run --project Samples/Nazor.Samples.Localization -- --culture fr-FR --ui-culture fr-FR
```

Both cultures default explicitly to `en-US`, matching the other samples. `--culture` overrides formatting and `--ui-culture` overrides the resource language independently. Italian strings in `App.it.resx` are intentional:
they demonstrate language selection rather than untranslated sample documentation.

Read `Program.cs`, then compare `App.resx` with `App.it.resx` and follow their keys into `App.nazor`.
`IStringLocalizer<App>` selects strings using the UI culture. Dates and numbers use the formatting culture.
The app configures both through `builder.Application` before Build. Run applies them before hosted services and components start.

The neutral English resources provide fallback. With both overrides set to `fr-FR`, the text remains English because no French resources exist,
while dates and numbers use French formatting. With both set to `it-IT`, translated resources and Italian formatting are used.
With `--culture it-IT --ui-culture en-US`, the interface stays English while dates and numbers use Italian formatting.

The assembly's `RootNamespace` attribute gives the resource manager an explicit namespace for locating `App.resx`.
This makes lookup reliable when a project's assembly name and root namespace differ; it does not hard-code resource paths in the component.

Keep a number in state rather than its formatted string: `NumberField` handles parsing and formatting in the active culture and is supplied by the SDK.
The message box demonstrates that standard button labels come from framework resources, while its prompt comes from application resources.
Try both languages and cancel the prompt to compare the status text and the control labels.

In a production app, leave `builder.Application.Culture` and `builder.Application.UICulture` unset to preserve the corresponding current .NET cultures. The overrides do not modify the process-wide thread defaults.
See the [localization guide](../../Docs/Guide/25-Localization.md) for the full contract.
