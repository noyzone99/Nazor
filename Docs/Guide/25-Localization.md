# 25. Localization

## Application culture

Nazor uses .NET's CurrentCulture for formatting and parsing, and CurrentUICulture for resource lookup. Both overrides default to null: each unspecified value retains the effective culture of the startup execution context. Applications can set these .NET properties themselves or configure independent startup overrides:

```csharp
builder.Application.Culture = CultureInfo.GetCultureInfo("it-IT");
builder.Application.UICulture = CultureInfo.GetCultureInfo("en-US");
```

This example uses Italian date and number formats with English interface resources. Setting Culture alone does not change UICulture, and setting UICulture alone does not change Culture. To change both, assign both explicitly.

Build copies the overrides. Run and RunAsync apply them in the UI thread's execution context before starting hosted services and creating components or tray items. Task-based work inherits culture through normal .NET execution-context flow. Nazor does not assign DefaultThreadCurrentCulture or DefaultThreadCurrentUICulture; these process-wide defaults remain under the application's control. Work started outside the application's execution context is not given an application-wide culture override.

The previous UI-thread cultures are restored after hosted services stop, including when startup or the UI loop throws. These are startup overrides, not a complete live language-switching mechanism. Restart or deliberately update application state when designing runtime language changes.

`NumberField`, `Calendar`, and `DatePicker` also accept `Culture` for a specific value's editing/formatting. Keep formatting separate from storage: store numbers/dates as typed values and localize at the UI boundary. For machine-readable configuration, NSS strings, IDs, or protocols, use invariant formatting.

## Application resources

Place resource files alongside the component: `App.resx` for neutral strings, `App.it.resx` for Italian, and further culture-specific files as needed. Inject the registered .NET localizer:

```razor
@using Microsoft.Extensions.Localization
@inject IStringLocalizer<App> Text

<TextBlock>@Text["Welcome"]</TextBlock>
<TextBlock>@Text["Today", DateTime.Today.ToString("D")]</TextBlock>
```

For example, the neutral `Today` entry can be `Today is {0}`. Keep placeholders consistent in translations. Missing translations fall back through .NET resource lookup to neutral resources. Keep complete phrases in resources rather than assembling sentence fragments with language-dependent order.

Resource lookup uses the assembly's root namespace. If it differs from the assembly name, declare `[assembly: RootNamespace("Company.App")]` from `Microsoft.Extensions.Localization` so the localizer finds the resources. Ensure component folders and resource names agree.

## Framework text

Nazor ships built-in UI strings in English and Italian. The English text is the neutral resource set in `Strings.resx`, and Italian is in `Strings.it.resx`. If a key is missing for the requested UI culture, .NET checks its parent culture and then falls back to English. When a control exposes a text override, the application can pass its own localized wording.

Native OS dialogs, menus, and caption buttons can use the operating system's language independently of application resources. App-owned title-bar buttons must localize their own visible text and accessible names.

### Add a framework translation from source

Add a culture-specific catalog alongside `Resources/Strings.resx` in each project that owns the text you want to translate. For example, use `Resources/Strings.fr.resx` in `Nazor.Controls` for French control labels. Keep the same resource keys and format placeholders, and translate the values, including tooltips and accessible names.

| Project | Framework text it owns |
| --- | --- |
| [`Nazor.Application`](../../Sources/Core/Nazor.Application/Resources/Strings.resx) | Standard message buttons and window menu commands |
| [`Nazor.Controls`](../../Sources/Core/Nazor.Controls/Resources/Strings.resx) | Control defaults such as calendar navigation, empty states, and accessible names |
| [`Nazor.Input`](../../Sources/Core/Nazor.Input/Resources/Strings.resx) | Text-editing menu commands such as Copy and Paste |
| [`Nazor.Themes.NazorLeaf`](../../Sources/Themes/Nazor.Themes.NazorLeaf/Resources/Strings.resx) | Window-control and loading labels specific to the optional NazorLeaf theme |

Rebuild the projects you changed so the .NET SDK generates their satellite assemblies. If the application consumes Nazor through NuGet, repack the affected packages and use those builds in the application. Set the application's UI culture to test the translated controls. See the [deployment guide](28-Libraries-Packaging-and-Deployment.md#publish-localized-resources) for how the satellites are included in the publish output.

## Testing languages

The [Localization sample](../../Samples/Nazor.Samples.Localization/README.md) starts in English and accepts a culture argument. Its Italian resource file intentionally demonstrates translated content. Verify date order, decimal separators, month/day names, longer labels, and accessible names in each supported culture.

Localization does not add bidirectional text layout: the current text engine is left-to-right. Assess that limitation before targeting languages requiring full RTL/Bidi behavior.

API: [Nazor.Application](../ApiReferences/Nazor.Application.md), [Nazor.Controls](../ApiReferences/Nazor.Controls.md).

---

[Guide index](README.md) · [API reference](../API.md)
