# Nazor.Hosting.Windows

Convenience entry points that configure a Nazor application with the Windows platform host.

This reference covers the public and protected API declared by this package. Inherited members follow the contract of their declaring type. Positional record parameters also define public properties with the same names.

## Types

- [`WindowsApplication`](#type-windowsapplication)

<a id="type-windowsapplication"></a>

## `WindowsApplication`

Namespace: `Nazor`

```csharp
public static class WindowsApplication
```

Convenient startup for Windows applications; the application runtime remains platform independent.

Provides explicit Windows startup. SDK applications can use `NazorApplication.CreateBuilder` with the automatically registered platform host.

[Source](../../Sources/Hosts/Nazor.Hosting.Windows/WindowsApplication.cs)

### Members

#### `CreateBuilder`

```csharp
public static NazorApplicationBuilder CreateBuilder(string[]? args = null)
```

Creates an application builder configured with the Windows platform host.

| Parameter | Description |
| --- | --- |
| `args` | The command-line arguments supplied to the application. |

Returns: The configured application builder.

Select the startup mode on the returned builder, call `Build`, configure `app.Appearance`, and call `app.Run()` or `app.RunAsync()`.
