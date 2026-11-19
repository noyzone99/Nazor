# 4. Application hosting and services

## Composition root

The builder configures hosting through the standard Generic Host properties. Its `Application` sub-builder configures at most one initial surface, any registered tray items, independent culture overrides and shutdown policy. The built application owns live appearance. Native backend initialization and surface creation happen in `Run` or `RunAsync`, not in `Build`.

```csharp
using HelloNazor;

var builder = NazorApplication.CreateBuilder(args);
builder.Services.AddSingleton<TaskStore>();
builder.Logging.SetMinimumLevel(LogLevel.Information);
builder.Application.UseMainWindow<App>();

using var app = builder.Build();
app.Appearance.StyleSheet = HelloNazor.StyleSheets.Theme;
app.Appearance.ColorScheme = ColorScheme.System;
return app.Run();
```

`TaskStore` is application-defined. `App` must declare `@window`. Configure its associated `Window` in OnInitialized, and use the same IWindow for runtime changes. The SDK registers `DesktopPlatformHost`, selecting a native backend at runtime; Windows and Linux Wayland/X11 are implemented. The neutral desktop target is `net10.0`.

## Explicit startup modes

| Application sub-builder method | Contract |
| --- | --- |
| `builder.Application.UseMainWindow<T>(opening)` | T declares `@window`; opening optionally controls activation and positioning |
| `builder.Application.CreateMainWindow<T>(configure)` | Declares a native window for content T, created during Run; T must not declare `@window` |
| `builder.Application.UseMainView<T>()` | T does not declare `@window` and fills the display surface of an `ISingleViewPlatform` backend |
| `builder.Application.UseTrayItem<T>()` | Registers one independently scoped ITrayItem of type T; additive with a main window or other tray types |

Desktop startup requires a main window or at least one registered tray item. Tray-only startup requires `OnExplicitShutdown`; initialize tray UI and startup work in `TrayItem.OnStarted`. Multiple initial surface selections, duplicate tray types and combining `UseMainView` with tray items fail clearly. See [tray lifecycle and typed menus](15-Tray-and-System-Notifications.md). `Build` snapshots the configuration and can be called only once. Choose either `Run()` or `RunAsync()` once per built application. A mismatch between the rendered root and selected mode fails before a native window is created. A window definition cannot be nested as a child component; use `IWindowService.Open` to open another native window. The compiler diagnoses static nesting and the renderer rejects dynamic nesting before constructing the child.

## Asynchronous execution

Use the same builder and startup mode with an asynchronous entry point:

```csharp
await using var app = builder.Build();
app.Appearance.ColorScheme = ColorScheme.System;
return await app.RunAsync();
```

`RunAsync(CancellationToken cancellationToken = default)` returns `Task<int>` without blocking the caller. Its task completes after the UI loop exits, hosted services stop and the backend is released. Await it before disposing the application. `StartAsync` and `StopAsync` manage Generic Host services only; `RunAsync` also owns UI startup and shutdown.

An optional cancellation token requests application shutdown through the host lifetime. Cancellation during the event loop closes the application normally and returns its exit code, usually zero. A token already canceled prevents startup without consuming the application; cancellation during host startup can produce `OperationCanceledException`. Startup and backend exceptions propagate through the returned task, with service cleanup attempted even when startup fails.

The backend owns asynchronous UI-thread startup. Windows uses a dedicated STA thread, including when the caller is already STA. Awaiting the result does not grant UI-thread access: marshal UI changes through the dispatcher.

To host an ordinary reusable component explicitly:

```csharp
builder.Application.CreateMainWindow<Counter>(window =>
{
    window.Title = "Counter";
    window.Width = 320;
    window.Height = 200;
    window.IsResizable = true;
});
```

The component remains usable headlessly. There is no application-wide title, size or window-operation fallback. Secondary windows have their own definitions; see [windows and dialogs](13-Windows-and-Dialogs.md).

Single-view startup is implemented by the explicit Linux `DrmPlatformHost`; see [DRM/KMS hosting](34-Linux-DRM.md) for startup, dependencies and device acceptance. Windows desktop rejects `UseMainView`; use a window mode. The default Linux desktop host selects Wayland or X11 from the publication and desktop session; see [selection, overrides and startup fallback](35-Linux-X11.md#select-and-publish). See [platform support](32-Support-and-Limitations.md#platform-and-distribution) for available hosts.

## Configuration ownership

| API | Responsibility |
| --- | --- |
| `builder.Services`, `Configuration`, `Logging`, `Environment`, `Metrics` | Standard .NET Generic Host configuration |
| `builder.Application` | Startup registration through an ApplicationBuilder; selected settings are copied at Build |
| `builder.Application.Culture` | Formatting override for CurrentCulture; null preserves its value at startup |
| `builder.Application.UICulture` | Resource language override for CurrentUICulture; null preserves its value at startup |
| `builder.Application.ShutdownMode` | Main-window, last-window or explicit shutdown |
| `builder.Platform.Host` | Explicit backend host override; otherwise uses the SDK default |
| `builder.Diagnostics.DevChannel` | Development channel; null reads `NAZOR_DEVCHANNEL` |
| `builder.Diagnostics.AnimationClock` | Optional deterministic animation clock |
| `builder.Fonts` | Private fonts and matching policy, captured at Build |
| `app.Appearance` / injected `IApplicationAppearance` | Live theme, application stylesheet, color scheme, reduced motion and contrast |

Configure appearance after `Build()`. Before `Run()`, setters change the initial state. During execution, change them on the UI thread so all open windows update and future windows inherit the settings. Theme and stylesheet are references to compiled stylesheets. A null theme selects Basic. Use `Nazor.Themes.Themes.None` to supply all control styles yourself. Optional themes can require builder configuration before `Build()`, such as registering the private fonts for NazorLeaf. `System` color scheme and null accessibility preferences follow operating-system changes. Window-level styles have a separate cascade level. [Theme setup](16-Styling-and-Themes.md#choose-a-theme-and-a-color-scheme)

The builder implements `IHostApplicationBuilder`. Configuration reads appsettings files, environment variables and command-line arguments. Hosted services start before native surface creation and stop after the UI loop exits. Run and RunAsync apply culture overrides on the UI thread before starting hosted services, preserve each unspecified culture, and restore the thread's previous cultures after service shutdown, including failures. They do not change DefaultThreadCurrentCulture or DefaultThreadCurrentUICulture. See [localization](25-Localization.md). Disposing the application releases its host and owned services.

`ApplicationBuilder` is a configuration facade; it is not registered in DI. The built service provider contains a separate `ApplicationConfiguration` copy with the selected Culture, UICulture and ShutdownMode. Later changes through builder.Application do not affect the built application.

## Backend selection

`NazorApplication.CreateBuilder(args)` receives a fresh host from the process default factory. A later registration affects subsequent builders only. Override `builder.Platform.Host` before Build, or pass `platformHost:` to CreateBuilder to bypass the registered factory entirely. `RegisterPlatformHost` requires a non-null factory returning a non-null host. A missing host is reported at Run.

`WindowsApplication.CreateBuilder(args)` is the Windows-specific builder facade. It selects Windows explicitly, independent of the process default, and uses the same builder, startup selectors, appearance service and `Run`/`RunAsync` methods as `NazorApplication.CreateBuilder(args)`.

Configure window and tray startup through `builder.Application`: UseMainWindow, CreateMainWindow, UseMainView and UseTrayItem select the corresponding surfaces before Build. Culture controls formatting; set UICulture separately when both formatting and resource language should change.

## Service lifetimes

| Lifetime | Boundary and typical use |
| --- | --- |
| Singleton | Whole application: repositories, appearance, tray coordinator |
| Scoped | One window: editing state, navigation and window services |
| Transient | Each resolution: stateless operations |

Each window owns a DI scope, disposed with its component tree. `IWindow` refers to the current window; `IWindowService` opens windows and dialogs; `IApplicationAppearance` is application-wide. Avoid capturing window-scoped services in a singleton. Resolve a temporary service scope when a coordinator needs to obtain the window service.

`IApplicationLauncher` is also a singleton shared by every window. It manages the application's launcher tasks and badge, with capabilities separate from `WindowCapabilities`. See [application launcher integration](15-Tray-and-System-Notifications.md#application-launcher-jump-lists-and-badges).

## Thread and lifetime

The backend owns UI-thread startup; the Windows host supplies STA without requiring `[STAThread]` on Program. Background work must marshal UI changes through the dispatcher. Registered tray items start after desktop services become available; put their startup work in `TrayItem.OnStarted`.

| Shutdown mode | Behavior |
| --- | --- |
| `OnMainWindowClose` | Default: closing the main window ends the application |
| `OnLastWindowClose` | Continue until the last window closes |
| `OnExplicitShutdown` | Continue with zero windows until explicitly stopped |

`IHostApplicationLifetime.StopApplication()` or `IApplicationLifetime.Shutdown(exitCode)` requests shutdown. Window close is currently not cancelable. See [tray startup](15-Tray-and-System-Notifications.md#register-and-obtain-an-item).

API: [Nazor.Application](../ApiReferences/Nazor.Application.md), [desktop host](../ApiReferences/Nazor.Hosting.Desktop.md), [Windows facade](../ApiReferences/Nazor.Hosting.Windows.md).

---

[Guide index](README.md) · [API reference](../API.md)
