# Nazor.Application

Application startup, dependency injection, windows, dialogs, placement persistence, and desktop integration.

See [framework-specific dependency versions](../Guide/28-Libraries-Packaging-and-Deployment.md#framework-specific-dependency-versions) for the Microsoft.Extensions versions selected by each target framework.

This reference covers the public and protected API declared by this package. Inherited members follow the contract of their declaring type. Positional record parameters also define public properties with the same names.

## Rendering and backend completion

`NazorWindow.Rendering` returns immutable `RenderingInfo` with the requested/effective renderer, backend, device and fallback reason. `GpuFramesPresented`, `GpuFramesReplaced` and `LastGpuFrameDuration` report completed worker frames, replaced pending frames and the last worker's CPU submission/presentation wait. The duration is not GPU execution time.

`Task<RenderedImage> CaptureFrameAsync()` must be called on the UI dispatcher and awaited without blocking it. It reads the GPU framebuffer only on explicit request; software windows produce a raster image. GPU capture includes decorations recorded by the backend, excluding separate native OS child windows.

`event Action<PresentedFrame>? FramePresented` reports backend completion on the UI dispatcher, coalescing to the latest frame while busy. `PresentedFrame(int UiFrame, RenderingMode Renderer, long? RenderWorkerFrame, TimeSpan BackendDuration)` distinguishes the originating UI frame from autonomous GPU frames. Completion establishes backend submission, not physical display scanout.

`WindowFrameMetrics.Rendering`, `RenderWorkerFrame`, `RenderWorkerDuration` and `GpuFramesReplaced` are optional host metrics. UI `Raster` time means GPU recording/submission on GPU frames. The separately identified worker frame's duration includes CPU submission and presentation wait; it may describe an earlier completed frame than the current UI paint.

## Application types

- [`IApplicationLauncher`](#type-iapplicationlauncher)
- [`SystemTrayRadioOption`](#type-systemtrayradiooption)
- [`SystemTrayRadioGroup`](#type-systemtrayradiogroup)
- [`SystemTrayCheckBox`](#type-systemtraycheckbox)
- [`SystemTraySubmenu`](#type-systemtraysubmenu)
- [`SystemTraySeparator`](#type-systemtrayseparator)
- [`SystemTrayCommand`](#type-systemtraycommand)
- [`SystemTrayLabeledItem`](#type-systemtraylabeleditem)
- [`SystemTrayMenuItem`](#type-systemtraymenuitem)
- [`SystemTrayMenu`](#type-systemtraymenu)
- [`TrayItemContext`](#type-trayitemcontext)
- [`TrayItem`](#type-trayitem)
- [`ITrayItem`](#type-itrayitem)
- [`IApplicationAppearance`](#type-iapplicationappearance)
- [`WindowConfiguration`](#type-windowconfiguration)
- [`DiagnosticsConfiguration`](#type-diagnosticsconfiguration)
- [`PlatformConfiguration`](#type-platformconfiguration)
- [`ApplicationConfiguration`](#type-applicationconfiguration)
- [`ApplicationBuilder`](#type-applicationbuilder)
- [`CascadingValueServiceCollectionExtensions`](#type-cascadingvalueservicecollectionextensions)
- [`DevChannelServer`](#type-devchannelserver)
- [`FileFilter`](#type-filefilter)
- [`FolderPickerOptions`](#type-folderpickeroptions)
- [`IAnnouncer`](#type-iannouncer)
- [`IApplicationLifetime`](#type-iapplicationlifetime)
- [`IFileDialogService`](#type-ifiledialogservice)
- [`ISystemNotificationService`](#type-isystemnotificationservice)
- [`ISystemTray`](#type-isystemtray)
- [`IWindowPlacementStore`](#type-iwindowplacementstore)
- [`IWindowService`](#type-iwindowservice)
- [`JsonWindowPlacementStore`](#type-jsonwindowplacementstore)
- [`MessageBoxButtons`](#type-messageboxbuttons)
- [`MessageBoxIcon`](#type-messageboxicon)
- [`MessageBoxResult`](#type-messageboxresult)
- [`NazorApp`](#type-nazorapp)
- [`NazorApplication`](#type-nazorapplication)
- [`NazorApplicationBuilder`](#type-nazorapplicationbuilder)
- [`NazorWindow`](#type-nazorwindow)
- [`IWindowInstrumentation`](#type-iwindowinstrumentation)
- [`WindowFrameCause`](#type-windowframecause)
- [`WindowFrameMetrics`](#type-windowframemetrics)
- [`OpenFileOptions`](#type-openfileoptions)
- [`SaveFileOptions`](#type-savefileoptions)
- [`ShutdownMode`](#type-shutdownmode)
- [`WindowOpenOptions`](#type-windowopenoptions)
- [`WindowStartupLocation`](#type-windowstartuplocation)

<a id="type-iapplicationlauncher"></a>

## `IApplicationLauncher`

Namespace: `Nazor`

```csharp
public interface IApplicationLauncher
{
    ApplicationLauncherCapabilities Capabilities { get; }
    event Action? CapabilitiesChanged;
    bool SetJumpList(IReadOnlyList<JumpListTask> tasks);
    bool SetBadgeCount(int count);
    bool SetBadgeIcon(ImageSource icon, string description);
    bool ClearBadge();
}
```

Application-wide singleton for one launcher identity, separate from `IWindow` and tray items. Resolve the same instance from any application/window scope. Methods require the UI dispatcher during `Run`; outside that lifetime they throw `InvalidOperationException`. Capabilities are empty outside `Run`; changes reported by the backend are dispatched through `CapabilitiesChanged`.

| Member | Contract |
| --- | --- |
| `Capabilities` | Independent runtime support for Jump List tasks, count badges and icon badges. Support does not guarantee shell presentation. |
| `SetJumpList(tasks)` | Validates and copies tasks and their argument collections, then replaces this API's published tasks. Empty removes them. Labels are nonempty without NUL; arguments are non-null without NUL, and may be empty. Tasks start the current app, potentially in a new process. Unsupported/rejected publication returns false. |
| `SetBadgeCount(count)` | Replaces the application badge with a count without a Nazor cap or abbreviation. Windows draws every digit; other shells control presentation. Applications apply any limit before calling. Negative throws `ArgumentOutOfRangeException`; zero clears either kind. Returns backend acceptance. |
| `SetBadgeIcon(icon, description)` | Requires a non-null source and nonempty accessible description without NUL. Accepts asynchronous raster image loading with display-scale variants. The native publication follows later. Load/publication failure is diagnosed and preserves the previous published badge. |
| `ClearBadge()` | Clears either badge kind, leaving launcher tasks and the main icon unchanged. Returns acceptance. |

Count and icon are mutually exclusive. Rejected requests preserve previous state; accepted replacement/clear and stop cancel stale image loads. Invalid arguments throw regardless of support. Shutdown clears the runtime badge but preserves published Windows Jump List tasks. This API does not supply single-instance forwarding, recent/frequent destinations or custom categories. See [application launcher usage and platform limitations](../Guide/15-Tray-and-System-Notifications.md#application-launcher-jump-lists-and-badges).

[Source](../../Sources/Core/Nazor.Application/ApplicationLauncher.cs)

<a id="type-cascadingvalueservicecollectionextensions"></a>

## `CascadingValueServiceCollectionExtensions`

Namespace: `Microsoft.Extensions.DependencyInjection`

```csharp
public static class CascadingValueServiceCollectionExtensions
```

Root cascades are created in the window's DI scope and used by its renderer.

[Source](../../Sources/Core/Nazor.Application/CascadingValueServiceCollectionExtensions.cs)

### Members

#### `AddCascadingValue`

```csharp
public static IServiceCollection AddCascadingValue<TValue>(this IServiceCollection services, Func<IServiceProvider, TValue> valueFactory)
```

Registers a root cascading value or value source with the dependency injection container.

| Type parameter | Description |
| --- | --- |
| `TValue` | The type of the component, callback, or cascading value. |

| Parameter | Description |
| --- | --- |
| `services` | The dependency injection service collection or provider used by the application. |
| `valueFactory` | The factory that resolves the root cascading value from application services. |

Returns: The resulting IServiceCollection value.

Throws `ArgumentNullException`: valueFactory is null.

#### `AddCascadingValue`

```csharp
public static IServiceCollection AddCascadingValue<TValue>(this IServiceCollection services, string name, Func<IServiceProvider, TValue> valueFactory)
```

Registers a root cascading value or value source with the dependency injection container.

| Type parameter | Description |
| --- | --- |
| `TValue` | The type of the component, callback, or cascading value. |

| Parameter | Description |
| --- | --- |
| `services` | The dependency injection service collection or provider used by the application. |
| `name` | The name used to identify the parameter, property, resource, or registration. |
| `valueFactory` | The factory that resolves the root cascading value from application services. |

Returns: The resulting IServiceCollection value.

Throws `ArgumentNullException`: valueFactory is null.

#### `AddCascadingValue`

```csharp
public static IServiceCollection AddCascadingValue<TValue>(this IServiceCollection services, Func<IServiceProvider, CascadingValueSource<TValue>> sourceFactory)
```

Registers a root cascading value or value source with the dependency injection container.

| Type parameter | Description |
| --- | --- |
| `TValue` | The type of the component, callback, or cascading value. |

| Parameter | Description |
| --- | --- |
| `services` | The dependency injection service collection or provider used by the application. |
| `sourceFactory` | The factory that creates the root cascading value source. |

Returns: The resulting IServiceCollection value.

Throws `ArgumentNullException`: services, sourceFactory is null.


<a id="type-devchannelserver"></a>

## `DevChannelServer`

Namespace: `Nazor.Development`

```csharp
public sealed class DevChannelServer : IDisposable
```

The development channel of a running application: a named pipe server, one JSON-RPC peer per connected tool, that applies recompiled stylesheets in place (`styles/update`), re-renders (`app/render`) and describes the windows (`app/info`, `app/dump`), and forwards hot reload events as `app/log` notifications. The process is found through a descriptor file in the discovery directory, written on `Start` and deleted on `Dispose`. Everything that touches a window runs on its UI dispatcher.

[Source](../../Sources/Core/Nazor.Application/DevChannel/DevChannelServer.cs)

### Members

#### `LoggerCategory`

```csharp
public const string LoggerCategory = "Nazor.DevChannel";
```

The logger category of the channel.

#### `DevChannelServer`

```csharp
public DevChannelServer(UIDispatcher dispatcher, Func<IReadOnlyList<NazorWindow>> windows, ILogger? logger, string? discoveryDirectory = null)
```

Initializes a new instance of DevChannelServer.

| Parameter | Description |
| --- | --- |
| `dispatcher` | The UI dispatcher of the windows served: only windows on it are visible to the tools. |
| `windows` | The open windows, read on every request (for example `NazorWindow.OpenWindows`). |
| `logger` | Where errors go; without one they go to `Console.Error`. |
| `discoveryDirectory` | Where the descriptor is written; `DevChannelProtocol.DiscoveryDirectory` when null. |

Throws `ArgumentNullException`: dispatcher, windows is null.

#### `PipeName`

```csharp
public string PipeName { get; }
```

The name of the named pipe the tools connect to.

#### `DiscoveryFile`

```csharp
public string DiscoveryFile { get; }
```

The descriptor file: `<discovery directory>/<pid>.json`.

#### `ConnectionCount`

```csharp
public int ConnectionCount { get ; }
```

The tools connected right now.

#### `Start`

```csharp
public void Start()
```

Writes the descriptor and starts accepting connections; throws when the pipe or the descriptor cannot be created.

#### `Dispose`

```csharp
public void Dispose()
```

Stops accepting, disconnects every tool, deletes the descriptor; waits at most two seconds for the loop.


<a id="type-filefilter"></a>

## `FileFilter`

Namespace: `Nazor`

```csharp
public sealed record FileFilter(string Name, params string[] Patterns)
```

A choice of file types: `new FileFilter("CSV files", "*.csv")`.

| Parameter | Description |
| --- | --- |
| `Name` | The name used to identify the parameter, property, resource, or registration. |
| `Patterns` | The patterns (string[]). |

[Source](../../Sources/Core/Nazor.Application/FileDialogs.cs)


<a id="type-folderpickeroptions"></a>

## `FolderPickerOptions`

Namespace: `Nazor`

```csharp
public sealed record FolderPickerOptions
```

How `IFileDialogService` picks a folder.

[Source](../../Sources/Core/Nazor.Application/FileDialogs.cs)

### Members

#### `Title`

```csharp
public string? Title { get; init;}
```

Gets or sets the title displayed by this window, dialog, or message.

#### `InitialDirectory`

```csharp
public string? InitialDirectory { get; init;}
```

Gets or sets the directory initially displayed by the native picker.


<a id="type-iannouncer"></a>

## `IAnnouncer`

Namespace: `Nazor`

```csharp
public interface IAnnouncer
```

Speaks messages that are not on the screen through the screen reader of the user, as `aria-live` or the notifications of WinUI: "Saved", "3 results", "Connection lost". Inject it in a component of a window; text that is on the screen and changes is better a live region (`live="polite"`, an `InlineMessage`).

[Source](../../Sources/Core/Nazor.Application/Windows.cs)

### Members

#### `Announce`

```csharp
void Announce(string message, AnnouncementPriority priority = AnnouncementPriority.Polite)
```

Speaks `message`: after the current speech, or at once when assertive.

| Parameter | Description |
| --- | --- |
| `message` | The message (string). |
| `priority` | The priority (AnnouncementPriority). |


<a id="type-iapplicationlifetime"></a>

## `IApplicationLifetime`

Namespace: `Nazor`

```csharp
public interface IApplicationLifetime
```

Application lifetime, also usable when no windows remain.

[Source](../../Sources/Core/Nazor.Application/DesktopServices.cs)

### Members

#### `Dispatcher`

```csharp
Dispatcher Dispatcher { get; }
```

Available during Run. Use InvokeAsync for desktop services called from a worker.

#### `Shutdown`

```csharp
void Shutdown(int exitCode = 0)
```

Ends Run and disposes all windows and native resources. May be called from any thread.

| Parameter | Description |
| --- | --- |
| `exitCode` | The exit code reported when the application event loop terminates. |


<a id="type-ifiledialogservice"></a>

## `IFileDialogService`

Namespace: `Nazor`

```csharp
public interface IFileDialogService
```

Provides native open, save, and folder dialogs that are modal to the window owning the scoped service. Canceling returns null for a single path or an empty list for multiple paths.

[Source](../../Sources/Core/Nazor.Application/FileDialogs.cs)

### Members

#### `OpenFileAsync`

```csharp
Task<string?> OpenFileAsync(OpenFileOptions? options = null)
```

Opens a native file picker for a single file, modal to the current window.

| Parameter | Description |
| --- | --- |
| `options` | The options used to configure the operation. |

Returns: A task returning the selected file's path, or null if the user cancels.

#### `OpenFilesAsync`

```csharp
Task<IReadOnlyList<string>> OpenFilesAsync(OpenFileOptions? options = null)
```

Opens a native file picker that allows multiple files to be selected.

| Parameter | Description |
| --- | --- |
| `options` | The options used to configure the operation. |

Returns: A task returning the selected file paths, or an empty list if the user cancels.

#### `SaveFileAsync`

```csharp
Task<string?> SaveFileAsync(SaveFileOptions? options = null)
```

Asks before replacing an existing file.

| Parameter | Description |
| --- | --- |
| `options` | The options used to configure the operation. |

Returns: A task returning the selected destination path, or null if the user cancels.

#### `PickFolderAsync`

```csharp
Task<string?> PickFolderAsync(FolderPickerOptions? options = null)
```

Opens a native folder picker, modal to the current window.

| Parameter | Description |
| --- | --- |
| `options` | The options used to configure the operation. |

Returns: A task returning the selected folder path, or null if the user cancels.


<a id="type-isystemnotificationservice"></a>

## `ISystemNotificationService`

Namespace: `Nazor`

```csharp
public interface ISystemNotificationService
```

Transient native notifications, independent of windows and IAnnouncer. UI thread only.

[Source](../../Sources/Core/Nazor.Application/DesktopServices.cs)

### Members

#### `IsSupported`

```csharp
bool IsSupported { get; }
```

Gets whether the platform supports this service.

#### `Activated`

```csharp
event Action? Activated;
```

Occurs when activated is reported.

#### `Show`

```csharp
bool Show(SystemNotification notification)
```

Replaces the current notification. True means accepted, not necessarily displayed.

| Parameter | Description |
| --- | --- |
| `notification` | The notification (SystemNotification). |

Returns: true when the operation succeeds or the condition is satisfied; otherwise, false.

#### `Withdraw`

```csharp
void Withdraw()
```

Withdraws the current banner; does not clear notification-center history.


<a id="type-isystemtray"></a>

## `ISystemTray`

Namespace: `Nazor`

```csharp
public interface ISystemTray
{
    bool IsSupported { get; }
    T Get<T>() where T : class, ITrayItem;
}
```

Application-wide registry. Get returns the exact active registered instance during Run on the UI thread, never constructs an instance, and throws for missing/stopped items. Each registered type has one independent DI scope and native registration. Use UseTrayItem<T> rather than registering tray types as singletons.

[Source](../../Sources/Core/Nazor.Application/DesktopServices.cs)

<a id="type-iwindowplacementstore"></a>

## `IWindowPlacementStore`

Namespace: `Nazor`

```csharp
public interface IWindowPlacementStore
```

Where the windows with a `PlacementKey` remember their position and size. Replace it in the services to keep them elsewhere (the settings of the application, a test folder).

[Source](../../Sources/Core/Nazor.Application/WindowPlacements.cs)

### Members

#### `Load`

```csharp
WindowPlacement? Load(string key)
```

Loads saved bounds and state for the specified window key.

| Parameter | Description |
| --- | --- |
| `key` | The key (string). |

Returns: The saved placement, or null if none is available.

#### `Save`

```csharp
void Save(string key, WindowPlacement placement)
```

Persists normal bounds and maximized state for the specified window key.

| Parameter | Description |
| --- | --- |
| `key` | The key (string). |
| `placement` | The normal window bounds and state to save or restore. |


<a id="type-iwindowservice"></a>

## `IWindowService`

Namespace: `Nazor`

```csharp
public interface IWindowService
```

Opens windows, each with a component as its root, its own renderer and its own DI scope (the scoped services live as long as the window). Inject it with `[Inject] IWindowService`.

[Source](../../Sources/Core/Nazor.Application/Windows.cs)

Every component opened through this service must declare @window. Each window owns a DI scope disposed with its component tree. Global appearance is provided by IApplicationAppearance.

### Members

#### `Windows`

```csharp
IReadOnlyList<IWindow> Windows { get; }
```

The open windows, in opening order.

#### `MainWindow`

```csharp
IWindow? MainWindow { get; }
```

The first window of the application.

#### `Open`

```csharp
IWindow Open(Type component, IReadOnlyDictionary<string, object?>? parameters = null, WindowOpenOptions? options = null)
```

Opens a window containing the specified component type, parameters, and window options.

| Parameter | Description |
| --- | --- |
| `component` | The component type whose scoped styles or render output are being requested. |
| `parameters` | The parameter names and values to supply to the component. |
| `options` | The options used to configure the operation. |

Returns: The newly opened window.

#### `Open`

```csharp
IWindow Open<TComponent>(IReadOnlyDictionary<string, object?>? parameters = null, WindowOpenOptions? options = null) where TComponent : IComponent
```

Opens a window containing the specified component type, parameters, and window options.

| Type parameter | Description |
| --- | --- |
| `TComponent` | The component type to instantiate or render. |

| Parameter | Description |
| --- | --- |
| `parameters` | The parameter names and values to supply to the component. |
| `options` | The options used to configure the operation. |

Returns: The newly opened window.

#### `ShowDialogAsync`

```csharp
Task<object?> ShowDialogAsync(Type component, IReadOnlyDictionary<string, object?>? parameters = null, WindowOpenOptions? options = null)
```

Opens a modal component window and awaits the result supplied when it closes.

| Parameter | Description |
| --- | --- |
| `component` | The component type whose scoped styles or render output are being requested. |
| `parameters` | The parameter names and values to supply to the component. |
| `options` | The options used to configure the operation. |

Returns: A task returning the dialog result, or the result type's default value if no compatible result is supplied.

#### `ShowDialogAsync`

```csharp
async Task<TResult?> ShowDialogAsync<TComponent, TResult>(IReadOnlyDictionary<string, object?>? parameters = null, WindowOpenOptions? options = null) where TComponent : IComponent
```

Opens a modal component window and awaits the result supplied when it closes.

| Type parameter | Description |
| --- | --- |
| `TComponent` | The component type to instantiate or render. |
| `TResult` | The type of the result. |

| Parameter | Description |
| --- | --- |
| `parameters` | The parameter names and values to supply to the component. |
| `options` | The options used to configure the operation. |

Returns: A task returning the dialog result, or the result type's default value if no compatible result is supplied.

#### `ShowMessageAsync`

```csharp
Task<MessageBoxResult> ShowMessageAsync(string message, string? title = null, MessageBoxButtons buttons = MessageBoxButtons.Ok, MessageBoxIcon icon = MessageBoxIcon.None)
```

Shows a message in a modal dialog drawn with the theme of the application, with the buttons of `buttons` (the first is the default one, for Enter); the task completes with the button chosen. Esc and the close button answer Cancel when there is one, OK for a message with OK only, and None otherwise.

| Parameter | Description |
| --- | --- |
| `message` | The message (string). |
| `title` | The title (string?). |
| `buttons` | The buttons (MessageBoxButtons). |
| `icon` | The icon (MessageBoxIcon). |

Returns: A task whose result is the value produced by the operation.


<a id="type-jsonwindowplacementstore"></a>

## `JsonWindowPlacementStore`

Namespace: `Nazor`

```csharp
public sealed class JsonWindowPlacementStore(string path) : IWindowPlacementStore
```

Keeps the placements in a JSON file, by default `%LOCALAPPDATA%\<application>\Nazor\windows.json`. The file is written whole into a temporary file and then moved, so a crash never leaves it half written.

| Parameter | Description |
| --- | --- |
| `path` | The file path used by this operation. |

[Source](../../Sources/Core/Nazor.Application/WindowPlacements.cs)

### Members

#### `Path`

```csharp
public string Path { get; }
```

Gets the absolute path of the JSON placement file.

#### `ForApplication`

```csharp
public static JsonWindowPlacementStore ForApplication()
```

The file of the application: under the local application data, in a folder named after the entry assembly.

Returns: The resulting JsonWindowPlacementStore value.

#### `Load`

```csharp
public WindowPlacement? Load(string key)
```

Reads the saved placement for a window key from the JSON store.

| Parameter | Description |
| --- | --- |
| `key` | The key (string). |

Returns: The saved placement, or null if no valid entry can be read.

#### `Save`

```csharp
public void Save(string key, WindowPlacement placement)
```

Writes the window placement under the specified key in the JSON store.

| Parameter | Description |
| --- | --- |
| `key` | The key (string). |
| `placement` | The normal window bounds and state to save or restore. |


<a id="type-messageboxbuttons"></a>

## `MessageBoxButtons`

Namespace: `Nazor`

```csharp
public enum MessageBoxButtons
```

The buttons of `IWindowService.ShowMessageAsync`.

[Source](../../Sources/Core/Nazor.Application/MessageBox.cs)

### Members

#### `Ok`

```csharp
Ok
```

Displays an OK button.

#### `OkCancel`

```csharp
OkCancel
```

Displays OK and Cancel buttons.

#### `YesNo`

```csharp
YesNo
```

Displays Yes and No buttons.

#### `YesNoCancel`

```csharp
YesNoCancel
```

Displays Yes, No, and Cancel buttons.


<a id="type-messageboxicon"></a>

## `MessageBoxIcon`

Namespace: `Nazor`

```csharp
public enum MessageBoxIcon
```

The icon of `IWindowService.ShowMessageAsync`.

[Source](../../Sources/Core/Nazor.Application/MessageBox.cs)

### Members

#### `None`

```csharp
None
```

Displays no standard icon.

#### `Information`

```csharp
Information
```

Displays the standard information icon.

#### `Warning`

```csharp
Warning
```

Displays the standard warning icon.

#### `Error`

```csharp
Error
```

Displays the standard error icon.

#### `Question`

```csharp
Question
```

Displays the standard question icon.


<a id="type-messageboxresult"></a>

## `MessageBoxResult`

Namespace: `Nazor`

```csharp
public enum MessageBoxResult
```

The button chosen in a message box.

[Source](../../Sources/Core/Nazor.Application/MessageBox.cs)

### Members

#### `None`

```csharp
None
```

Indicates that no button result was supplied.

#### `Ok`

```csharp
Ok
```

Indicates that the user chose OK.

#### `Cancel`

```csharp
Cancel
```

Indicates that the user canceled the dialog.

#### `Yes`

```csharp
Yes
```

Indicates that the user chose Yes.

#### `No`

```csharp
No
```

Indicates that the user chose No.


<a id="type-nazorapp"></a>

## `NazorApp`

Namespace: `Nazor`

```csharp
public sealed class NazorApp : IHost, IAsyncDisposable
{
    public IServiceProvider Services { get; }
    public IConfiguration Configuration { get; }
    public IHostEnvironment Environment { get; }
    public IHostApplicationLifetime Lifetime { get; }
    public ILogger Logger { get; }
    public IApplicationAppearance Appearance { get; }
    public int Run();
    public Task<int> RunAsync(CancellationToken cancellationToken = default);
    public Task StartAsync(CancellationToken cancellationToken = default);
    public Task StopAsync(CancellationToken cancellationToken = default);
    public void Dispose();
    public ValueTask DisposeAsync();
}
```

A built application owning its Generic Host. Choose Run or RunAsync once to execute the previously selected startup mode. Run blocks and returns the native loop exit code; RunAsync returns Task<int> and completes after the UI loop, service shutdown and backend cleanup. Both delegate UI-thread initialization to the platform host, start hosted services before surface creation, and stop them after the loop exits or startup fails. Missing hosts and duplicate or mixed Run/RunAsync calls throw InvalidOperationException. Single-view mode on an incompatible platform throws PlatformNotSupportedException. The selected component is checked before creating a native surface: declared-window mode requires @window and content mode rejects window definitions. Appearance is the same application-wide singleton available through DI.

RunAsync's optional cancellationToken requests shutdown through IHostApplicationLifetime. During the native event loop it causes normal shutdown and returns the exit code. A token canceled before calling RunAsync prevents startup and leaves the application available for a later run. Cancellation during host startup can throw OperationCanceledException. Startup and backend exceptions propagate through the task. Windows executes asynchronously on a dedicated STA thread; awaiting the task does not provide UI-thread access.

StartAsync and StopAsync are the Generic Host lifecycle operations; they do not select or create UI surfaces. Dispose or DisposeAsync releases owned host resources; wait for Run/RunAsync to finish before disposal.

[Source](../../Sources/Core/Nazor.Application/NazorApp.cs)

<a id="type-nazorapplication"></a>

## `NazorApplication`

Namespace: `Nazor`

```csharp
public static class NazorApplication
{
    public static NazorApplicationBuilder CreateBuilder(string[]? args = null, IPlatformHost? platformHost = null);
    public static void RegisterPlatformHost(Func<IPlatformHost> factory);
}
```

CreateBuilder prepares Generic Host services and receives a fresh host from the registered factory. An explicit platformHost bypasses the default factory. Null args uses no command-line arguments. RegisterPlatformHost replaces the process-wide default for subsequent builders; it requires a non-null factory returning a non-null host. Existing builders are unaffected. A missing registration may be replaced through builder.Platform.Host before Build; otherwise Run reports an actionable error. There is no static Run convenience method.

[Source](../../Sources/Core/Nazor.Application/NazorApplication.cs)

<a id="type-nazorapplicationbuilder"></a>

## `NazorApplicationBuilder`

Namespace: `Nazor`

```csharp
public sealed class NazorApplicationBuilder : IHostApplicationBuilder
{
    public ApplicationBuilder Application { get; }
    public PlatformConfiguration Platform { get; }
    public DiagnosticsConfiguration Diagnostics { get; }
    public FontOptions Fonts { get; set; }
    public IServiceCollection Services { get; }
    public ConfigurationManager Configuration { get; }
    public ILoggingBuilder Logging { get; }
    public IHostEnvironment Environment { get; }
    public IMetricsBuilder Metrics { get; }
    public IDictionary<object, object> Properties { get; }
    public void ConfigureContainer<TContainerBuilder>(IServiceProviderFactory<TContainerBuilder> factory, Action<TContainerBuilder>? configure = null) where TContainerBuilder : notnull;
    public NazorApp Build();
}
```

Configure startup through `builder.Application`. Select at most one initial surface before `Build`, and register any distinct tray types with `UseTrayItem<T>`. `UseMainWindow<T>` requires `@window`; `CreateMainWindow<T>` creates a window for content without `@window`; `UseMainView<T>` requires an `ISingleViewPlatform` and content without `@window`. Desktop startup requires an initial window or at least one tray item. Tray-only startup requires `OnExplicitShutdown`. Each tray type gets a dedicated scope during `Run`, and tray items cannot be combined with `UseMainView`. Duplicate surface selections, duplicate tray types, absent startup surfaces, a second `Build` or registration after `Build` throw `InvalidOperationException`. Abstract/open generic root types are rejected. `Build` constructs the host and snapshots startup settings and font paths; it creates no native surfaces. `ApplicationConfiguration` is copied and registered in DI; the `ApplicationBuilder` facade is not. Appearance is configured on the returned application. Standard host configuration and container integration retain the `IHostApplicationBuilder` contracts.

[Source](../../Sources/Core/Nazor.Application/NazorApplicationBuilder.cs)

<a id="type-openfileoptions"></a>

## `OpenFileOptions`

Namespace: `Nazor`

```csharp
public sealed record OpenFileOptions
```

How `IFileDialogService` opens files.

[Source](../../Sources/Core/Nazor.Application/FileDialogs.cs)

### Members

#### `Title`

```csharp
public string? Title { get; init;}
```

Gets or sets the title displayed by this window, dialog, or message.

#### `InitialDirectory`

```csharp
public string? InitialDirectory { get; init;}
```

Gets or sets the directory initially displayed by the native picker.

#### `FileName`

```csharp
public string? FileName { get; init;}
```

The file selected when the dialog opens.

#### `Filters`

```csharp
public IReadOnlyList<FileFilter> Filters { get; init;}
```

Gets or sets the file-type filters offered by the native picker.


<a id="type-savefileoptions"></a>

## `SaveFileOptions`

Namespace: `Nazor`

```csharp
public sealed record SaveFileOptions
```

How `IFileDialogService` saves a file.

[Source](../../Sources/Core/Nazor.Application/FileDialogs.cs)

### Members

#### `Title`

```csharp
public string? Title { get; init;}
```

Gets or sets the title displayed by this window, dialog, or message.

#### `InitialDirectory`

```csharp
public string? InitialDirectory { get; init;}
```

Gets or sets the directory initially displayed by the native picker.

#### `FileName`

```csharp
public string? FileName { get; init;}
```

The name proposed.

#### `DefaultExtension`

```csharp
public string? DefaultExtension { get; init;}
```

The extension added to a name without one (`csv`).

#### `Filters`

```csharp
public IReadOnlyList<FileFilter> Filters { get; init;}
```

Gets or sets the file-type filters offered by the native picker.


<a id="type-shutdownmode"></a>

## `ShutdownMode`

Namespace: `Nazor`

```csharp
public enum ShutdownMode
```

When an application with several windows ends.

[Source](../../Sources/Core/Nazor.Application/Windows.cs)

### Members

#### `OnMainWindowClose`

```csharp
OnMainWindowClose
```

When the main window (the first) closes; the other windows close with it.

#### `OnLastWindowClose`

```csharp
OnLastWindowClose
```

When the last window closes.

#### `OnExplicitShutdown`

```csharp
OnExplicitShutdown
```

Only an explicit shutdown ends the application; zero windows is allowed.


<a id="type-windowopenoptions"></a>

## `WindowOpenOptions`

Namespace: `Nazor`

```csharp
public sealed record WindowOpenOptions
{
    public IWindow? Owner { get; init; }
    public bool? Activate { get; init; }
    public WindowStartupLocation StartupLocation { get; init; }
    public bool SizeToContent { get; init; }
}
```

Opening context, not a competing window definition. Owner associates an independent window with another; dialogs default to the calling window. Null Activate uses normal activation. Default StartupLocation lets the OS position normal windows and centers dialogs on their owner. SizeToContent fits height to rendered content, preserving width and recentering when applicable. Title, initial size and operation permissions belong to the declared root Window; secondary windows do not inherit main-window geometry.

[Source](../../Sources/Core/Nazor.Application/Windows.cs)

<a id="type-windowstartuplocation"></a>

## `WindowStartupLocation`

Namespace: `Nazor`

```csharp
public enum WindowStartupLocation
```

Where a window opens.

[Source](../../Sources/Core/Nazor.Application/Windows.cs)

### Members

#### `Default`

```csharp
Default
```

Where Windows puts new windows; dialogs are centered on their owner.

#### `CenterOwner`

```csharp
CenterOwner
```

Centers the window on its owner, falling back to the screen when no owner is available.

#### `CenterScreen`

```csharp
CenterScreen
```

Centers the window in the display's work area.
<a id="type-applicationbuilder"></a>

## `ApplicationBuilder`

Namespace: `Nazor`

```csharp
public sealed class ApplicationBuilder
{
    public CultureInfo? Culture { get; set; }
    public CultureInfo? UICulture { get; set; }
    public ShutdownMode ShutdownMode { get; set; }
    public void UseMainWindow<TComponent>(WindowOpenOptions? opening = null) where TComponent : IComponent;
    public void CreateMainWindow<TComponent>(Action<WindowConfiguration>? configure = null) where TComponent : IComponent;
    public void UseMainView<TComponent>() where TComponent : IComponent;
    public void UseTrayItem<T>() where T : class, ITrayItem;
}
```

Obtained from NazorApplicationBuilder.Application. Delegates startup registration to its owning host builder and exposes the initial culture overrides and shutdown policy. UseMainWindow selects a component with @window; CreateMainWindow declares a window for content without @window; UseMainView requires a single-view backend. UseTrayItem is additive, registers a scoped concrete type immediately and creates the item during Run. Startup registrations retain the owning builder's uniqueness, compatibility and post-Build validation. Native creation is deferred to Run or RunAsync. There is no public constructor or Build method; call the owning builder's Build.

Culture overrides CurrentCulture for formatting and parsing. UICulture independently overrides CurrentUICulture for resources. Both default to null, preserving the corresponding current culture at startup. Run and RunAsync apply overrides before hosted services start and restore the preceding UI-thread cultures after service shutdown, including failure. DefaultThreadCurrentCulture and DefaultThreadCurrentUICulture are unchanged. Values are captured at Build in the runtime settings and in a separate ApplicationConfiguration DI snapshot.

[Source](../../Sources/Core/Nazor.Application/ApplicationBuilder.cs)

<a id="type-applicationconfiguration"></a>

## `ApplicationConfiguration`

Namespace: `Nazor`

```csharp
public sealed record ApplicationConfiguration
{
    public CultureInfo? Culture { get; set; }
    public CultureInfo? UICulture { get; set; }
    public ShutdownMode ShutdownMode { get; set; }
}
```

Application-wide initial settings, copied at Build and registered in DI independently of the ApplicationBuilder. Culture and UICulture are independent optional overrides for CurrentCulture and CurrentUICulture; each null preserves the corresponding value at startup. They do not change global thread defaults. Shutdown defaults to OnMainWindowClose. Tray-only startup requires OnExplicitShutdown.

[Source](../../Sources/Core/Nazor.Application/ApplicationConfiguration.cs)

<a id="type-platformconfiguration"></a>

## `PlatformConfiguration`

Namespace: `Nazor`

```csharp
public sealed class PlatformConfiguration
{
    public IPlatformHost? Host { get; set; }
}
```

Backend host override captured at Build. The SDK normally supplies DesktopPlatformHost. Creating or building the application does not initialize a native backend. The selected host owns UI-thread startup.

[Source](../../Sources/Core/Nazor.Application/ApplicationConfiguration.cs)

<a id="type-diagnosticsconfiguration"></a>

## `DiagnosticsConfiguration`

Namespace: `Nazor`

```csharp
public sealed record DiagnosticsConfiguration
{
    public bool? DevChannel { get; set; }
    public IAnimationClock? AnimationClock { get; set; }
    public Action<NazorWindow>? ConfigureWindow { get; set; }
}
```

Development settings copied at Build. Null DevChannel reads NAZOR_DEVCHANNEL at Run. Null AnimationClock selects the system clock; tests may supply a deterministic clock. ConfigureWindow is copied at Build and runs on the UI dispatcher for each main or subsequently opened window after native initialization and before first show. It can attach optional instrumentation; null does nothing. A callback failure disposes the newly created window, including attached instrumentation/native resources, and propagates to the creator. Ordinary runtime assemblies acquire no diagnostic dependency. The optional Agent exposes builder.UseDevTools() through this hook.

[Source](../../Sources/Core/Nazor.Application/ApplicationConfiguration.cs)

<a id="type-iapplicationappearance"></a>

## `IApplicationAppearance`

Namespace: `Nazor`

```csharp
public interface IApplicationAppearance
{
    StyleSheet? Theme { get; set; }
    StyleSheet? StyleSheet { get; set; }
    ColorScheme ColorScheme { get; set; }
    bool? ReducedMotion { get; set; }
    bool? HighContrast { get; set; }
    ControlDensity ControlDensity { get; set; }
}
```

Live application-wide appearance exposed by NazorApp.Appearance and the same singleton in DI. Configure after Build. During Run, setters require the UI dispatcher thread and update existing and future windows. Null Theme selects Basic; Themes.Themes.Nazor selects the branded theme and Themes.Themes.None removes all theme rules; null StyleSheet removes the application stylesheet. ColorScheme defaults to System; undefined enum values throw ArgumentOutOfRangeException. Null ReducedMotion and HighContrast follow operating-system preferences. ControlDensity defaults to Automatic, which follows each host's touch-input preference. Compact/Touch explicitly override that preference; undefined values throw ArgumentOutOfRangeException. A window-local explicit density wins over this application choice. Window-level style sheets retain their separate cascade level. This service is detached from its UI dispatcher after shutdown.

[Source](../../Sources/Core/Nazor.Application/ApplicationAppearance.cs)

<a id="type-itrayitem"></a>

## `ITrayItem`

Namespace: `Nazor`

```csharp
public interface ITrayItem : IDisposable
{
    ImageSource? Icon { get; set; }
    string ToolTip { get; set; }
    SystemTrayMenu Menu { get; set; }
    bool IsSupported { get; }
    bool IsVisible { get; }
    bool IsStarted { get; }
    Dispatcher Dispatcher { get; }
    event Action? Started;
    event Action? Stopped;
    event Action? Activated;
    event Action? SecondaryActivated;
    event Action? VisibilityChanged;
    event EventHandler<TrayMenuOpeningEventArgs>? MenuOpening;
    bool Show();
    void Hide();
    void Start(TrayItemContext context);
    void Stop();
}
```

One framework-owned tray. Configure properties before startup; changes update a visible item but do not show a hidden item. Show/Hide require the UI thread during Run. Start/Stop are framework lifecycle methods; Stop is terminal and Dispose is idempotent. IsSupported reflects backend availability; IsVisible does not promise placement outside shell overflow. Icon loads asynchronously. ToolTip is nonempty without NUL, at most 127 UTF-16 units. Events run on the dispatcher. MenuOpening cancellation is synchronous and supported on Windows; Linux AboutToShow cannot veto the desktop menu.

[Source](../../Sources/Core/Nazor.Application/TrayItem.cs)


<a id="type-trayitem"></a>

## `TrayItem`

Namespace: `Nazor`

```csharp
public abstract class TrayItem : ITrayItem
{
    // Public properties, operations and events implement ITrayItem.
    protected virtual void OnStarted();
    protected virtual void OnStopping();
    protected virtual void OnStopped();
    protected virtual void OnActivated();
    protected virtual void OnSecondaryActivated();
    protected virtual void OnVisibilityChanged();
    protected virtual void OnMenuOpening(TrayMenuOpeningEventArgs args);
    protected virtual void Dispose(bool disposing);
}
```

Base implementation. Start/Stop are explicit ITrayItem implementations. OnStarted runs after the initial show attempt, including on an unsupported desktop so an item can open fallback UI. OnStopping runs before detachment; OnStopped and Stopped follow detachment. External subscribers are notified even when overrides omit base calls. Teardown suppresses icon interaction and visibility hooks. Constructor dependencies live in an independent item scope. See the [guide](../Guide/15-Tray-and-System-Notifications.md) for a complete lifecycle and sample.

[Source](../../Sources/Core/Nazor.Application/TrayItem.cs)


<a id="type-trayitemcontext"></a>

## `TrayItemContext`

Namespace: `Nazor`

```csharp
public sealed class TrayItemContext : IDisposable
{
    public Dispatcher Dispatcher { get; }
    public bool IsSupported { get; }
    public bool IsVisible { get; }
    public event Action? Activated;
    public event Action? SecondaryActivated;
    public event Action? VisibilityChanged;
    public event Action<TrayMenuOpeningEventArgs>? MenuOpening;
    public bool Show(ImageSource? icon, string tip, SystemTrayMenu menu);
    public void Hide();
    public void Dispose();
}
```

Runtime-created context passed to ITrayItem.Start. Allows direct ITrayItem implementations to delegate native operations without exposing native window handles or D-Bus objects. Typed menu callbacks are routed only to the current snapshot; image loads are canceled on replacement, hide and disposal.

[Source](../../Sources/Core/Nazor.Application/TrayItemContext.cs)


<a id="type-systemtraymenu"></a>

## `SystemTrayMenu`

Namespace: `Nazor`

```csharp
public sealed class SystemTrayMenu
{
    public static SystemTrayMenu Empty { get; }
    public IReadOnlyList<SystemTrayMenuItem> Items { get; }
    public SystemTrayMenu(IEnumerable<SystemTrayMenuItem> items);
}
```

Copies its input to a read-only ordered list. Nested menus use SystemTraySubmenu. Replace Menu to publish changed application state. A snapshot allows 32 nested levels and at most 65,535 native entries. Null entries and ambiguous command IDs are rejected.

[Source](../../Sources/Core/Nazor.Application/SystemTrayMenu.cs)


<a id="type-systemtraymenuitem"></a>

## `SystemTrayMenuItem`

Namespace: `Nazor`

```csharp
public abstract class SystemTrayMenuItem
```

Base of the closed family of typed entries. Separators are explicit objects, not null command IDs.

[Source](../../Sources/Core/Nazor.Application/SystemTrayMenu.cs)


<a id="type-systemtraylabeleditem"></a>

## `SystemTrayLabeledItem`

Namespace: `Nazor`

```csharp
public abstract class SystemTrayLabeledItem : SystemTrayMenuItem
{
    public string Text { get; }
    public bool Enabled { get; init; }
    public bool Visible { get; init; }
    public ImageSource? Icon { get; init; }
}
```

Shared presentation. Enabled and Visible default to true. Labels are nonempty without NUL. Icons are optional ImageSource assets, rendered natively after asynchronous loading.

[Source](../../Sources/Core/Nazor.Application/SystemTrayMenu.cs)


<a id="type-systemtraycommand"></a>

## `SystemTrayCommand`

Namespace: `Nazor`

```csharp
public sealed class SystemTrayCommand : SystemTrayLabeledItem
{
    public SystemTrayCommand(string id, string text);
    public string Id { get; }
    public event Action? Invoked;
}
```

An executable command. IDs are nonempty without NUL and unique across all commands and checkboxes in one tray menu tree. Subscribe directly to Invoked; no parent menu-command event exists.

[Source](../../Sources/Core/Nazor.Application/SystemTrayMenu.cs)


<a id="type-systemtrayseparator"></a>

## `SystemTraySeparator`

Namespace: `Nazor`

```csharp
public sealed class SystemTraySeparator : SystemTrayMenuItem
```

Native separator with no command ID, label or checked state.

[Source](../../Sources/Core/Nazor.Application/SystemTrayMenu.cs)


<a id="type-systemtraysubmenu"></a>

## `SystemTraySubmenu`

Namespace: `Nazor`

```csharp
public sealed class SystemTraySubmenu : SystemTrayLabeledItem
{
    public SystemTraySubmenu(string text, SystemTrayMenu menu);
    public SystemTrayMenu Menu { get; }
}
```

A heading that opens a separate menu object. Descendant commands cannot invoke when an ancestor is hidden or disabled.

[Source](../../Sources/Core/Nazor.Application/SystemTrayMenu.cs)


<a id="type-systemtraycheckbox"></a>

## `SystemTrayCheckBox`

Namespace: `Nazor`

```csharp
public sealed class SystemTrayCheckBox : SystemTrayLabeledItem
{
    public SystemTrayCheckBox(string id, string text, bool isChecked);
    public string Id { get; }
    public bool IsChecked { get; }
    public event Action<bool>? CheckedChangeRequested;
}
```

Controlled boolean choice. The callback receives the requested inverse of IsChecked. The current object is not mutated; publish a new menu to accept the change.

[Source](../../Sources/Core/Nazor.Application/SystemTrayMenu.cs)


<a id="type-systemtrayradiogroup"></a>

## `SystemTrayRadioGroup`

Namespace: `Nazor`

```csharp
public abstract class SystemTrayRadioGroup : SystemTrayMenuItem
public sealed class SystemTrayRadioGroup<TValue> : SystemTrayRadioGroup where TValue : notnull
{
    public SystemTrayRadioGroup(TValue selectedValue, IEnumerable<SystemTrayRadioOption<TValue>> options);
    public TValue SelectedValue { get; }
    public IReadOnlyList<SystemTrayRadioOption<TValue>> Options { get; }
    public event Action<TValue>? SelectionChangeRequested;
}
```

An inline group with distinct non-null values and exactly one declared selected option. Collections are copied. Activation requests TValue directly and does not mutate SelectedValue. Wrap the group in a SystemTraySubmenu to create a nested menu.

[Source](../../Sources/Core/Nazor.Application/SystemTrayMenu.cs)


<a id="type-systemtrayradiooption"></a>

## `SystemTrayRadioOption`

Namespace: `Nazor`

```csharp
public sealed class SystemTrayRadioOption<TValue> where TValue : notnull
{
    public SystemTrayRadioOption(TValue value, string text);
    public TValue Value { get; }
    public string Text { get; }
    public bool Enabled { get; init; }
    public bool Visible { get; init; }
    public ImageSource? Icon { get; init; }
}
```

A typed choice belonging inside a radio group. It cannot be placed directly into a menu.

[Source](../../Sources/Core/Nazor.Application/SystemTrayMenu.cs)

<a id="type-iwindowinstrumentation"></a>

## `IWindowInstrumentation`

Namespace: `Nazor`

```csharp
public interface IWindowInstrumentation : IDisposable
{
    void NativeWindowCreated(IPlatformWindow window) { }
    bool InterceptsPointer { get; }
    PlatformCursor? PointerCursor { get; }
    bool PointerMoved(Point point);
    bool PointerPressed(Point point, PointerButton button);
    bool PointerReleased(Point point, PointerButton button);
    bool PointerLeft();
    void PointerCanceled();
    bool KeyDown(string key);
    bool KeyDown(string key, KeyModifiers modifiers, bool repeat) => KeyDown(key);
    bool KeyUp(string key);
    DisplayList Compose(DisplayList list);
    void RecordFrame(WindowFrameMetrics frame);
}
```

One optional window-owned input/overlay/frame integration, invoked on the owning UI dispatcher. The runtime and these contracts reference no Diagnostics, DevTools or IDE SDK assembly. Setting `NazorWindow.Instrumentation` transfers lifetime ownership; replacement, detachment and window disposal call `Dispose` before renderer/scene/input teardown. Without an attachment no optional frame metrics are collected.

| Member | Parameters, result and behavior |
| --- | --- |
| `NativeWindowCreated(window)` | UI-thread notification when the native window is ready, or immediately when attaching to an existing window. Default no-op; supports neutral native registrations without runtime diagnostics dependencies. |
| `InterceptsPointer` | When true, wheel and double-click input are consumed before application controls. |
| `PointerCursor` | Optional portable backend cursor override; null uses ordinary application hit testing. |
| `PointerMoved(point)` | Logical viewport position; true consumes movement. |
| `PointerPressed(point, button)` / `PointerReleased(point, button)` | Logical position and pointer button; true consumes the corresponding event. Release can be consumed after interception ends to suppress a matching press. |
| `PointerLeft()` | True consumes the pointer leaving the window. |
| `PointerCanceled()` | Cancels interception before ordinary application pointer capture is canceled. |
| `KeyDown(key)` / `KeyUp(key)` | Normalized key name; true consumes the event before application commands. |
| `Compose(list)` | Receives the completed application display list and returns a list with optional overlay commands. Runs before damage calculation/rasterization, so removed overlay pixels are repainted. |
| `RecordFrame(frame)` | Receives completed raster/window metrics before native presentation. Scene counters precede overlay/accessibility queries. Copy `Damage` before retaining its contents. |

Exceptions propagate through the existing window/input/render path. The interface performs no automatic attachment, discovery or timer scheduling.

[Source](../../Sources/Core/Nazor.Application/IWindowInstrumentation.cs)

<a id="type-windowframecause"></a>

## `WindowFrameCause`

Namespace: `Nazor`

```csharp
[Flags] public enum WindowFrameCause
```

Neutral reasons accumulated while a window integration is attached. Flags combine until the next paint.

| Member | Value | Reason |
| --- | --- | --- |
| `None` | `0` | No cause recorded. |
| `Paint` | `1` | Backend painting, used when no explicit reason was accumulated. |
| `RenderBatch` | `2` | Component render batch changed the retained tree. |
| `Interaction` | `4` | Pointer, keyboard or focus state changed. |
| `Resize` | `8` | Viewport size, scale or environment changed. |
| `Animation` | `16` | Animation/transition frame due. |
| `HotReload` | `32` | Code or stylesheet update. |
| `Overlay` | `64` | Optional overlay update. |
| `Image` | `128` | Image loading changed visual content. |
| `SystemSettings` | `256` | System appearance or preferences changed. |
| `Explicit` | `512` | Application called `RequestFrame()`. |

[Source](../../Sources/Core/Nazor.Application/WindowFrameMetrics.cs)

<a id="type-windowframemetrics"></a>

## `WindowFrameMetrics`

Namespace: `Nazor`

```csharp
public readonly record struct WindowFrameMetrics(
    int Number, WindowFrameCause Cause, TimeSpan Total, TimeSpan Styles,
    TimeSpan Layout, TimeSpan DisplayList, TimeSpan Overlay, TimeSpan Raster,
    int StylesRecomputed, int LayoutComputations, IReadOnlyList<PixelRect>? Damage)
```

| Parameter / property | Contract |
| --- | --- |
| `Number` | Window's completed frame number. |
| `Cause` | Combined reasons for the frame. |
| `Total` | Frame work including window bookkeeping; excludes subsequent native presentation. |
| `Styles`, `Layout` | Scene update phase times. |
| `DisplayList`, `Overlay`, `Raster` | Application drawing construction, integration composition and rasterization times. |
| `StylesRecomputed`, `LayoutComputations` | Scene work counters captured before overlay/accessibility queries. |
| `Damage` | Physical damage rectangles, or null for a full repaint. Borrowed for the callback; copy before retaining. |

[Source](../../Sources/Core/Nazor.Application/WindowFrameMetrics.cs)

## API signature index

These declarations supplement the contracts above. Inherited members are documented with the declaring type.

### `ApplicationConfiguration`

Namespace `Nazor`. [Source](../../Sources/Core/Nazor.Application/ApplicationConfiguration.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed record ApplicationConfiguration : IEquatable<ApplicationConfiguration>` | Application-wide settings captured when the builder is built. |
| `public override string ToString()` |  |
| `public static bool operator !=(ApplicationConfiguration? left, ApplicationConfiguration? right)` |  |
| `public static bool operator ==(ApplicationConfiguration? left, ApplicationConfiguration? right)` |  |
| `public override int GetHashCode()` |  |
| `public override bool Equals(object? obj)` |  |
| `public bool Equals(ApplicationConfiguration? other)` |  |
| `public ApplicationConfiguration()` |  |

### `DiagnosticsConfiguration`

Namespace `Nazor`. [Source](../../Sources/Core/Nazor.Application/ApplicationConfiguration.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed record DiagnosticsConfiguration : IEquatable<DiagnosticsConfiguration>` | Development and instrumentation settings captured at Build. |
| `public override string ToString()` |  |
| `public static bool operator !=(DiagnosticsConfiguration? left, DiagnosticsConfiguration? right)` |  |
| `public static bool operator ==(DiagnosticsConfiguration? left, DiagnosticsConfiguration? right)` |  |
| `public override int GetHashCode()` |  |
| `public override bool Equals(object? obj)` |  |
| `public bool Equals(DiagnosticsConfiguration? other)` |  |
| `public DiagnosticsConfiguration()` |  |

### `FileFilter`

Namespace `Nazor`. [Source](../../Sources/Core/Nazor.Application/FileDialogs.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed record FileFilter : IEquatable<FileFilter>` | A choice of file types: `new FileFilter("CSV files", "*.csv")`.<br>`Name`. The name used to identify the parameter, property, resource, or registration.<br>`Patterns`. The patterns (string[]). |
| `public FileFilter(string Name, params string[] Patterns)` | A choice of file types: `new FileFilter("CSV files", "*.csv")`.<br>`Name`. The name used to identify the parameter, property, resource, or registration.<br>`Patterns`. The patterns (string[]). |
| `public override string ToString()` |  |
| `public static bool operator !=(FileFilter? left, FileFilter? right)` |  |
| `public static bool operator ==(FileFilter? left, FileFilter? right)` |  |
| `public override int GetHashCode()` |  |
| `public override bool Equals(object? obj)` |  |
| `public bool Equals(FileFilter? other)` |  |
| `public void Deconstruct(out string Name, out string[] Patterns)` |  |
| `public string Name { get; init; }` | The name used to identify the parameter, property, resource, or registration. |
| `public string[] Patterns { get; init; }` | The patterns (string[]). |

### `FolderPickerOptions`

Namespace `Nazor`. [Source](../../Sources/Core/Nazor.Application/FileDialogs.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed record FolderPickerOptions : IEquatable<FolderPickerOptions>` | How `Nazor.IFileDialogService` picks a folder. |
| `public override string ToString()` |  |
| `public static bool operator !=(FolderPickerOptions? left, FolderPickerOptions? right)` |  |
| `public static bool operator ==(FolderPickerOptions? left, FolderPickerOptions? right)` |  |
| `public override int GetHashCode()` |  |
| `public override bool Equals(object? obj)` |  |
| `public bool Equals(FolderPickerOptions? other)` |  |
| `public FolderPickerOptions()` |  |

### `IWindowInstrumentation`

Namespace `Nazor`. [Source](../../Sources/Core/Nazor.Application/IWindowInstrumentation.cs)

| Declaration | Contract |
| --- | --- |
| `void NativeWindowCreated(IPlatformWindow window)` | Notifies the integration when its native window is available, including attachment after creation.<br>`window`. The native window, accessed on its UI thread. |
| `bool KeyDown(string key, KeyModifiers modifiers, bool repeat)` | Processes a key press with modifiers and repeat information before application commands.<br>`key`. The normalized key name.<br>`modifiers`. The active key modifiers.<br>`repeat`. Whether this is an automatic repeat.<br>**Returns**. True to consume the key; the default preserves the existing key interception. |

### `IWindowService`

Namespace `Nazor`. [Source](../../Sources/Core/Nazor.Application/Windows.cs)

| Declaration | Contract |
| --- | --- |
| `Task<TResult?> ShowDialogAsync<TComponent, TResult>(IReadOnlyDictionary<string, object?>? parameters = null, WindowOpenOptions? options = null) where TComponent : IComponent` | Opens a modal component window and awaits the result supplied when it closes.<br>`parameters`. The parameter names and values to supply to the component.<br>`options`. The options used to configure the operation.<br>`TComponent`. The component type to instantiate or render.<br>`TResult`. The type of the result.<br>**Returns**. A task returning the dialog result, or the result type's default value if no compatible result is supplied. |

### `JsonWindowPlacementStore`

Namespace `Nazor`. [Source](../../Sources/Core/Nazor.Application/WindowPlacements.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed class JsonWindowPlacementStore : IWindowPlacementStore` | Keeps the placements in a JSON file, by default `%LOCALAPPDATA%\<application>\Nazor\windows.json`. The file is written whole into a temporary file and then moved, so a crash never leaves it half written.<br>`path`. The file path used by this operation. |
| `public JsonWindowPlacementStore(string path)` | Keeps the placements in a JSON file, by default `%LOCALAPPDATA%\<application>\Nazor\windows.json`. The file is written whole into a temporary file and then moved, so a crash never leaves it half written.<br>`path`. The file path used by this operation. |

### `MessageBoxButtons`

Namespace `Nazor`. [Source](../../Sources/Core/Nazor.Application/MessageBox.cs)

| Declaration | Contract |
| --- | --- |
| `Ok = 0` | Displays an OK button. |
| `OkCancel = 1` | Displays OK and Cancel buttons. |
| `YesNo = 2` | Displays Yes and No buttons. |
| `YesNoCancel = 3` | Displays Yes, No, and Cancel buttons. |

### `MessageBoxIcon`

Namespace `Nazor`. [Source](../../Sources/Core/Nazor.Application/MessageBox.cs)

| Declaration | Contract |
| --- | --- |
| `None = 0` | Displays no standard icon. |
| `Information = 1` | Displays the standard information icon. |
| `Warning = 2` | Displays the standard warning icon. |
| `Error = 3` | Displays the standard error icon. |
| `Question = 4` | Displays the standard question icon. |

### `MessageBoxResult`

Namespace `Nazor`. [Source](../../Sources/Core/Nazor.Application/MessageBox.cs)

| Declaration | Contract |
| --- | --- |
| `None = 0` | Indicates that no button result was supplied. |
| `Ok = 1` | Indicates that the user chose OK. |
| `Cancel = 2` | Indicates that the user canceled the dialog. |
| `Yes = 3` | Indicates that the user chose Yes. |
| `No = 4` | Indicates that the user chose No. |

### `NazorApp`

Namespace `Nazor`. [Source](../../Sources/Core/Nazor.Application/NazorApp.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed class NazorApp : IHost, IDisposable, IAsyncDisposable` | A built Nazor application: the host with its services and a UI loop, with an initial window or desktop-only startup. Created by `Nazor.NazorApplicationBuilder.Build`. |
| `public Task<int> RunAsync(CancellationToken cancellationToken = default(CancellationToken))` | Starts the configured backend without blocking the caller and waits for shutdown and service cleanup.<br>`cancellationToken`. Requests application shutdown. A token canceled before startup prevents the run.<br>**Returns**. The native event loop exit code after shutdown. Cancellation during the event loop requests a normal shutdown.<br>Run and RunAsync share a single-use lifetime. The backend supplies the UI-thread context. Await this method before disposing the application. StartAsync alone starts only Generic Host services.<br>**Throws** `System.OperationCanceledException`. The token is already canceled or host startup is canceled. |
| `public Task StartAsync(CancellationToken cancellationToken = default(CancellationToken))` | Starts the application's Generic Host services.<br>`cancellationToken`. A token used to request cancellation.<br>**Returns**. A task that completes when the operation has finished. |
| `public Task StopAsync(CancellationToken cancellationToken = default(CancellationToken))` | Stops the application's Generic Host services.<br>`cancellationToken`. A token used to request cancellation.<br>**Returns**. A task that completes when the operation has finished. |

### `OpenFileOptions`

Namespace `Nazor`. [Source](../../Sources/Core/Nazor.Application/FileDialogs.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed record OpenFileOptions : IEquatable<OpenFileOptions>` | How `Nazor.IFileDialogService` opens files. |
| `public override string ToString()` |  |
| `public static bool operator !=(OpenFileOptions? left, OpenFileOptions? right)` |  |
| `public static bool operator ==(OpenFileOptions? left, OpenFileOptions? right)` |  |
| `public override int GetHashCode()` |  |
| `public override bool Equals(object? obj)` |  |
| `public bool Equals(OpenFileOptions? other)` |  |
| `public OpenFileOptions()` |  |

### `PlatformConfiguration`

Namespace `Nazor`. [Source](../../Sources/Core/Nazor.Application/ApplicationConfiguration.cs)

| Declaration | Contract |
| --- | --- |
| `public PlatformConfiguration()` |  |

### `PresentedFrame`

Namespace `Nazor`. [Source](../../Sources/Core/Nazor.Application/WindowFrameMetrics.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed record PresentedFrame : IEquatable<PresentedFrame>` | Completion of backend submission for a UI frame; does not establish physical display scanout.<br>`UiFrame`. The UI frame whose drawing was submitted.<br>`Renderer`. The effective renderer.<br>`RenderWorkerFrame`. The independent GPU worker frame number, or null for software.<br>`BackendDuration`. CPU backend submission and presentation wait, excluding UI drawing and GPU execution timestamps. |
| `public PresentedFrame(int UiFrame, RenderingMode Renderer, long? RenderWorkerFrame, TimeSpan BackendDuration)` | Completion of backend submission for a UI frame; does not establish physical display scanout.<br>`UiFrame`. The UI frame whose drawing was submitted.<br>`Renderer`. The effective renderer.<br>`RenderWorkerFrame`. The independent GPU worker frame number, or null for software.<br>`BackendDuration`. CPU backend submission and presentation wait, excluding UI drawing and GPU execution timestamps. |
| `public override string ToString()` |  |
| `public static bool operator !=(PresentedFrame? left, PresentedFrame? right)` |  |
| `public static bool operator ==(PresentedFrame? left, PresentedFrame? right)` |  |
| `public override int GetHashCode()` |  |
| `public override bool Equals(object? obj)` |  |
| `public bool Equals(PresentedFrame? other)` |  |
| `public void Deconstruct(out int UiFrame, out RenderingMode Renderer, out long? RenderWorkerFrame, out TimeSpan BackendDuration)` |  |
| `public int UiFrame { get; init; }` | The UI frame whose drawing was submitted. |
| `public RenderingMode Renderer { get; init; }` | The effective renderer. |
| `public long? RenderWorkerFrame { get; init; }` | The independent GPU worker frame number, or null for software. |
| `public TimeSpan BackendDuration { get; init; }` | CPU backend submission and presentation wait, excluding UI drawing and GPU execution timestamps. |

### `SaveFileOptions`

Namespace `Nazor`. [Source](../../Sources/Core/Nazor.Application/FileDialogs.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed record SaveFileOptions : IEquatable<SaveFileOptions>` | How `Nazor.IFileDialogService` saves a file. |
| `public override string ToString()` |  |
| `public static bool operator !=(SaveFileOptions? left, SaveFileOptions? right)` |  |
| `public static bool operator ==(SaveFileOptions? left, SaveFileOptions? right)` |  |
| `public override int GetHashCode()` |  |
| `public override bool Equals(object? obj)` |  |
| `public bool Equals(SaveFileOptions? other)` |  |
| `public SaveFileOptions()` |  |

### `ShutdownMode`

Namespace `Nazor`. [Source](../../Sources/Core/Nazor.Application/Windows.cs)

| Declaration | Contract |
| --- | --- |
| `OnMainWindowClose = 0` | When the main window (the first) closes; the other windows close with it. |
| `OnLastWindowClose = 1` | When the last window closes. |
| `OnExplicitShutdown = 2` | Only an explicit shutdown ends the application; zero windows is allowed. |

### `SystemTrayLabeledItem`

Namespace `Nazor`. [Source](../../Sources/Core/Nazor.Application/SystemTrayMenu.cs)

| Declaration | Contract |
| --- | --- |
| `public abstract class SystemTrayLabeledItem : SystemTrayMenuItem, ITrayLabel` | Presentation shared by commands, toggles and submenu headings. |

### `SystemTrayRadioGroup<TValue>`

Namespace `Nazor`. [Source](../../Sources/Core/Nazor.Application/SystemTrayMenu.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed class SystemTrayRadioGroup<TValue> : SystemTrayRadioGroup where TValue : notnull` | A controlled radio group expanded inline, with exactly one selected value. |
| `public SystemTrayRadioGroup(TValue selectedValue, IEnumerable<SystemTrayRadioOption<TValue>> options)` | Creates a validated SystemTrayRadioGroup instance. |
| `public TValue SelectedValue { get; }` | Gets the declared selection, which must match exactly one option. |
| `public IReadOnlyList<SystemTrayRadioOption<TValue>> Options { get; }` | Gets the immutable radio choices with distinct values. |
| `public event Action<TValue>? SelectionChangeRequested` | Requests a typed selection. The declared selection remains unchanged until the app publishes a new menu. |

### `SystemTrayRadioOption<TValue>`

Namespace `Nazor`. [Source](../../Sources/Core/Nazor.Application/SystemTrayMenu.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed class SystemTrayRadioOption<TValue> : ITrayLabel where TValue : notnull` | One typed radio choice; it can only be included inside a radio group. |
| `public SystemTrayRadioOption(TValue value, string text)` | Creates a typed choice with a non-null value and a valid label. |
| `public TValue Value { get; }` | Gets the typed value represented by this choice. |
| `public string Text { get; }` | Gets the nonempty native menu label. |
| `public bool Enabled { get; init; }` | Gets whether this choice can be selected. |
| `public bool Visible { get; init; }` | Gets whether this choice is visible. |
| `public ImageSource? Icon { get; init; }` | Gets the optional image rendered by the native menu host. |

### `SystemTraySeparator`

Namespace `Nazor`. [Source](../../Sources/Core/Nazor.Application/SystemTrayMenu.cs)

| Declaration | Contract |
| --- | --- |
| `public SystemTraySeparator()` |  |

### `TrayItem`

Namespace `Nazor`. [Source](../../Sources/Core/Nazor.Application/TrayItem.cs)

| Declaration | Contract |
| --- | --- |
| `public abstract class TrayItem : ITrayItem, IDisposable` | Framework-managed tray lifetime, independent of windows. Hide preserves the instance; Stop is terminal. |
| `public bool Show()` | Publishes the current icon and menu. True means accepted by the shell, not guaranteed placement. |
| `public void Hide()` | Removes the native icon while retaining the managed item and its state. |
| `public void Dispose()` | Stops and releases this instance. Repeated calls do nothing. |
| `protected TrayItem()` |  |
| `public ImageSource? Icon { get; set; }` | Gets or sets the requested icon. Null selects the application default; image loading is asynchronous. |
| `public string ToolTip { get; set; }` | Gets or sets the nonempty tooltip, limited to 127 UTF-16 units. |
| `public SystemTrayMenu Menu { get; set; }` | Gets or sets the typed menu, updating a visible item. |
| `public bool IsSupported { get; }` | Gets whether a tray host is currently available. |
| `public bool IsVisible { get; }` | Gets visibility known to the backend; shell overflow placement is not guaranteed. |
| `public bool IsStarted { get; }` | Gets whether the framework has started this instance and has not stopped it. |
| `public Dispatcher Dispatcher { get; }` | Gets the UI dispatcher. It is available after the runtime context is attached. |
| `public event Action? Started` | Occurs after initial publication and the startup hook complete. |
| `public event Action? Stopped` | Occurs after native resources are detached. |
| `public event Action? Activated` | Occurs for primary activation, typically a left click or keyboard activation. |
| `public event Action? SecondaryActivated` | Occurs for alternative activation, typically a middle click, when reported by the desktop. |
| `public event Action? VisibilityChanged` | Occurs when the backend detects a visibility change. |
| `public event EventHandler<TrayMenuOpeningEventArgs>? MenuOpening` | Occurs synchronously before menu presentation when reported by the desktop. Cancellation is backend-dependent. |

### `WindowFrameCause`

Namespace `Nazor`. [Source](../../Sources/Core/Nazor.Application/WindowFrameMetrics.cs)

| Declaration | Contract |
| --- | --- |
| `public enum WindowFrameCause` | Identifies the combined reasons for a window frame when an instrumentation integration is attached. |
| `None = 0` | No cause is recorded. |
| `Paint = 1` | The backend requested painting. |
| `RenderBatch = 2` | A component render batch changed the tree. |
| `Interaction = 4` | Pointer, keyboard or focus interaction changed visual state. |
| `Resize = 8` | The viewport size or scale changed. |
| `Animation = 16` | An animation or transition requested another frame. |
| `HotReload = 32` | A Hot Reload update changed code or styles. |
| `Overlay = 64` | An optional overlay changed. |
| `Image = 128` | Image loading changed the visual content. |
| `SystemSettings = 256` | System appearance or preferences changed. |
| `Explicit = 512` | Application code explicitly requested a frame. |

### `WindowFrameMetrics`

Namespace `Nazor`. [Source](../../Sources/Core/Nazor.Application/WindowFrameMetrics.cs)

| Declaration | Contract |
| --- | --- |
| `public readonly struct WindowFrameMetrics : IEquatable<WindowFrameMetrics>` | Completed UI paint work. GPU raster slots measure recording/submission, not GPU execution.<br>`Number`. The window's frame number.<br>`Cause`. The combined reasons for the frame.<br>`Total`. Total frame work including window bookkeeping.<br>`Styles`. Style resolution time.<br>`Layout`. Layout time.<br>`DisplayList`. Application display-list construction time.<br>`Overlay`. Instrumentation overlay composition time.<br>`Raster`. Software rasterization or GPU recording/submission time on the UI thread.<br>`StylesRecomputed`. Styles recomputed before overlay/accessibility queries.<br>`LayoutComputations`. Layout computations before overlay/accessibility queries.<br>`Damage`. Physical damage rectangles, or null for a full repaint. Copy before retaining them. |
| `public WindowFrameMetrics()` |  |
| `public WindowFrameMetrics(int Number, WindowFrameCause Cause, TimeSpan Total, TimeSpan Styles, TimeSpan Layout, TimeSpan DisplayList, TimeSpan Overlay, TimeSpan Raster, int StylesRecomputed, int LayoutComputations, IReadOnlyList<PixelRect>? Damage)` | Completed UI paint work. GPU raster slots measure recording/submission, not GPU execution.<br>`Number`. The window's frame number.<br>`Cause`. The combined reasons for the frame.<br>`Total`. Total frame work including window bookkeeping.<br>`Styles`. Style resolution time.<br>`Layout`. Layout time.<br>`DisplayList`. Application display-list construction time.<br>`Overlay`. Instrumentation overlay composition time.<br>`Raster`. Software rasterization or GPU recording/submission time on the UI thread.<br>`StylesRecomputed`. Styles recomputed before overlay/accessibility queries.<br>`LayoutComputations`. Layout computations before overlay/accessibility queries.<br>`Damage`. Physical damage rectangles, or null for a full repaint. Copy before retaining them. |
| `public override string ToString()` |  |
| `public static bool operator !=(WindowFrameMetrics left, WindowFrameMetrics right)` |  |
| `public static bool operator ==(WindowFrameMetrics left, WindowFrameMetrics right)` |  |
| `public override int GetHashCode()` |  |
| `public override bool Equals(object obj)` |  |
| `public bool Equals(WindowFrameMetrics other)` |  |
| `public void Deconstruct(out int Number, out WindowFrameCause Cause, out TimeSpan Total, out TimeSpan Styles, out TimeSpan Layout, out TimeSpan DisplayList, out TimeSpan Overlay, out TimeSpan Raster, out int StylesRecomputed, out int LayoutComputations, out IReadOnlyList<PixelRect>? Damage)` |  |
| `public int Number { get; init; }` | The window's frame number. |
| `public WindowFrameCause Cause { get; init; }` | The combined reasons for the frame. |
| `public TimeSpan Total { get; init; }` | Total frame work including window bookkeeping. |
| `public TimeSpan Styles { get; init; }` | Style resolution time. |
| `public TimeSpan Layout { get; init; }` | Layout time. |
| `public TimeSpan DisplayList { get; init; }` | Application display-list construction time. |
| `public TimeSpan Overlay { get; init; }` | Instrumentation overlay composition time. |
| `public TimeSpan Raster { get; init; }` | Software rasterization or GPU recording/submission time on the UI thread. |
| `public int StylesRecomputed { get; init; }` | Styles recomputed before overlay/accessibility queries. |
| `public int LayoutComputations { get; init; }` | Layout computations before overlay/accessibility queries. |
| `public IReadOnlyList<PixelRect>? Damage { get; init; }` | Physical damage rectangles, or null for a full repaint. Copy before retaining them. |
| `public RenderingInfo? Rendering { get; init; }` | Gets the renderer used by this UI frame, when reported by a native host. |
| `public long? RenderWorkerFrame { get; init; }` | Gets the last completed GPU worker frame, independently of the UI frame number. |
| `public TimeSpan? RenderWorkerDuration { get; init; }` | Gets CPU command submission plus presentation wait for RenderWorkerFrame; not GPU execution time. |
| `public long? GpuFramesReplaced { get; init; }` | Gets the cumulative number of pending GPU frames replaced by newer complete frames. |

### `WindowOpenOptions`

Namespace `Nazor`. [Source](../../Sources/Core/Nazor.Application/Windows.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed record WindowOpenOptions : IEquatable<WindowOpenOptions>` | How `Nazor.IWindowService` opens a window. |
| `public override string ToString()` |  |
| `public static bool operator !=(WindowOpenOptions? left, WindowOpenOptions? right)` |  |
| `public static bool operator ==(WindowOpenOptions? left, WindowOpenOptions? right)` |  |
| `public override int GetHashCode()` |  |
| `public override bool Equals(object? obj)` |  |
| `public bool Equals(WindowOpenOptions? other)` |  |
| `public WindowOpenOptions()` |  |

### `WindowStartupLocation`

Namespace `Nazor`. [Source](../../Sources/Core/Nazor.Application/Windows.cs)

| Declaration | Contract |
| --- | --- |
| `Default = 0` | Where Windows puts new windows; dialogs are centered on their owner. |
| `CenterOwner = 1` | Centers the window on its owner, falling back to the screen when no owner is available. |
| `CenterScreen = 2` | Centers the window in the display's work area. |


<a id="type-windowconfiguration"></a>

### `WindowConfiguration`

Namespace `Nazor`.

Defines a native window explicitly created to host a component without a Window root.

| Declaration | Contract |
| --- | --- |
| `public sealed record WindowConfiguration : IEquatable<WindowConfiguration>` | Defines a native window explicitly created to host a component without a Window root. |
| `public override string ToString()` | Standard .NET value or component semantics. |
| `public static bool operator !=(WindowConfiguration? left, WindowConfiguration? right)` | Standard .NET value or component semantics. |
| `public static bool operator ==(WindowConfiguration? left, WindowConfiguration? right)` | Standard .NET value or component semantics. |
| `public override int GetHashCode()` | Standard .NET value or component semantics. |
| `public override bool Equals(object? obj)` | Standard .NET value or component semantics. |
| `public bool Equals(WindowConfiguration? other)` | Standard .NET value or component semantics. |
| `public WindowConfiguration()` | Creates a new instance. |
| `public string Title { get; set; }` | The initial native title. |
| `public double Width { get; set; }` | Initial logical client width. |
| `public double Height { get; set; }` | Initial logical client height. |
| `public bool IsMinimizable { get; set; }` | Allows minimization. |
| `public bool IsMaximizable { get; set; }` | Allows maximization. |
| `public bool IsResizable { get; set; }` | Allows interactive resizing. |
| `public bool FullScreenEnabled { get; set; }` | Allows fullscreen entry. |
| `public bool IsAlwaysOnTop { get; set; }` | Keeps the window above non-topmost windows. |
| `public WindowDecorationPolicy? Decorations { get; set; }` | The initial complete decoration policy; null uses the desktop native-to-Nazor default. |
| `public bool IsVisibleInTaskbar { get; set; }` | Requests taskbar participation. |
| `public bool Activate { get; set; }` | Activates the window when shown. |

<a id="type-nazorwindow"></a>

### `NazorWindow`

Namespace `Nazor`.

One application window: the component renderer, its scene (style, layout, display list), input and the native window. A frame is requested only when something changed (a render batch, an interaction state, a resize); an idle window draws nothing.

| Declaration | Contract |
| --- | --- |
| `public sealed class NazorWindow : IWindowHost, IWindow, IDisposable, IWindowDecorationContent` | One application window: the component renderer, its scene (style, layout, display list), input and the native window. A frame is requested only when something changed (a render batch, an interaction state, a resize); an idle window draws nothing. |
| `public NativeWindowHandle? NativeHandle { get; }` | Borrowed operating-system identifier on the UI dispatcher; null before creation, after close, or when unsupported. Do not destroy or release it. See [native interop](../Guide/13-Windows-and-Dialogs.md#native-window-interop). |
| `public bool CanExecute(WindowCommand command)` | Whether the command can be sent with the current policy, state and backend support. |
| `public WindowCommandResult Request(WindowCommand command)` | Checks and dispatches a command. StateChanged reports the actual state. |
| `public Task<RenderedImage> CaptureFrameAsync()` | Captures the current frame, reading GPU pixels only when explicitly requested. The GPU framebuffer with recorded client decorations, excluding separate native OS child windows, or a software-rendered image. Call on the UI dispatcher and await without blocking it. GPU readback runs on the rendering thread. |
| `public void Announce(string message, AnnouncementPriority priority = AnnouncementPriority.Polite)` | Asks the screen reader to speak a message ("Saved", "3 results"). A window that no assistive technology has asked about has nobody to tell: nothing happens. |
| `public void Show(bool activate = true)` | Shows the window and optionally activates it. |
| `public void Hide()` | Hides the window while keeping its components and services alive. |
| `public void Activate()` | Brings the window to the front and gives it keyboard focus. |
| `public void Minimize()` | Requests that the window enter the minimized state. |
| `public void Maximize()` | Requests that the window enter the maximized state. |
| `public void EnterFullScreen()` | Requests native full screen while preserving the previous placement and state. |
| `public void ExitFullScreen()` | Leaves full screen and restores the saved placement and state. |
| `public void Restore()` | Leaves full screen or restores the window from its maximized or minimized state. |
| `public void Close()` | Closes the window and completes its lifetime. |
| `public void Close(object? result)` | Closes the window; a dialog completes its ShowDialogAsync with `result`. |
| `public void RequestFrame()` | Schedules a frame, recording the supplied cause when specified. |
| `public void RequestFrame(WindowFrameCause cause)` | Schedules a frame, recording the supplied cause when specified. |
| `public void Dispose()` | Removes the root component (disposing every component) and releases rendering resources. |
| `public ValueTask<WindowDecorationResult> SetDecorationsAsync(WindowDecorationPolicy policy, CancellationToken cancellationToken = default(CancellationToken))` |  |
| `public WindowOperations Operations { get; set; }` | The application's rules for window commands and interactive resize. |
| `public WindowTitleBarMenuMode TitleBarMenuMode { get; set; }` | The menu used for title bar clicks handled by Nazor. |
| `public WindowChrome Chrome { get; }` | The effective owner of the frame, its buttons and its context menu. |
| `public string? Class { get; set; }` | Space-separated NSS classes on the implicit window root. |
| `public string? Style { get; set; }` | Inline NSS declarations on the implicit window root. |
| `public ControlDensity ControlDensity { get; set; }` | Gets or sets the window-local override on the UI dispatcher. Automatic inherits application/host choice; explicit choices remain in effect across appearance and host updates. Undefined values throw ArgumentOutOfRangeException. [Source](../../Sources/Core/Nazor.Application/NazorWindow.Density.cs) |
| `public double Width { get; set; }` | Logical client width; non-normal states retain the requested normal dimensions. |
| `public double Height { get; set; }` | Logical client height; non-normal states retain the requested normal dimensions. |
| `public double? MinWidth { get; set; }` |  |
| `public double? MinHeight { get; set; }` |  |
| `public double? MaxWidth { get; set; }` |  |
| `public double? MaxHeight { get; set; }` |  |
| `public int? X { get; set; }` |  |
| `public int? Y { get; set; }` |  |
| `public bool IsAlwaysOnTop { get; set; }` |  |
| `public BackgroundMode BackgroundMode { get; set; }` |  |
| `public bool IsVisibleInTaskbar { get; set; }` |  |
| `public BackdropSelection Backdrop { get; set; }` | Requested backdrop; see AppliedBackdrop for the accepted result. |
| `public IStyleSheet? StyleSheet { get; set; }` | Stylesheet applied at the window level. |
| `public string? PlacementKey { get; set; }` | Placement persistence key; only configurable before native creation. |
| `public ImageSource? EffectiveIcon { get; }` | Explicit icon or a backend-resolved application icon source. Linux desktop supplies the embedded blue Nazor application tile; Windows resolves the executable icon. Null is possible on other backends. |
| `public static IReadOnlyList<NazorWindow> OpenWindows { get; }` | The windows created and not yet disposed, in creation order (a snapshot); development tools serve them. |
| `public UIDispatcher Dispatcher { get; }` | Gets the UI dispatcher associated with this renderer or window. |
| `public Renderer Renderer { get; }` | Gets the renderer that owns the component tree. |
| `public Scene Scene { get; }` | Gets the scene that provides layout, hit testing, and drawing for the window. |
| `public RenderingInfo Rendering { get; }` | Gets the requested and effective renderer, device and automatic fallback reason. |
| `public long GpuFramesPresented { get; }` | Gets the number of GPU frames actually presented by this window's render worker. |
| `public long GpuFramesReplaced { get; }` | Gets how many pending complete GPU frames were replaced by newer frames before rendering. |
| `public TimeSpan LastGpuFrameDuration { get; }` | Gets the last render worker's CPU submission and presentation wait time; excludes UI work and is not a GPU execution timestamp. |
| `public InputManager Input { get; }` | Gets the input manager associated with this window. |
| `public IWindowInstrumentation? Instrumentation { get; set; }` | Gets or replaces the optional input, overlay and frame-metrics integration. Set on the UI dispatcher. The window owns and disposes the previous integration before replacement. Detaching requests a frame to erase its overlay; attaching alone starts no listener or timer. Null is allowed during window disposal; attaching to a disposed window throws `System.ObjectDisposedException`. |
| `public RootComponent Root { get; }` | Gets the root component hosted by this window. |
| `public IPlatformWindow PlatformWindow { get; }` | Gets the native backend window used by this host. |
| `public SemanticTree Semantics { get; }` | The accessible view of the window, created on first use (the first request of an assistive technology): until then a window does no accessibility work. |
| `public bool HasSemantics { get; }` | True once `Nazor.NazorWindow.Semantics` exists. |
| `public int FrameCount { get; }` | UI frames submitted so far; autonomous compositor frames are counted by GpuFramesPresented. |
| `public TimeSpan LastFrameDuration { get; }` | Gets the last UI paint-handler duration, excluding asynchronous GPU work and native presentation. |
| `public List<Exception> Exceptions { get; }` | Exceptions reported by components, event handlers and dispatched work. |
| `public int HotReloadCount { get; }` | How many .NET Hot Reload deltas this window re-rendered for (failed re-renders included). |
| `public string Title { get; set; }` | Gets or sets the title displayed by this window, dialog, or message. |
| `public bool IsClosed { get; }` | Gets whether the native window has closed. |
| `public WindowState State { get; }` | Gets the current normal, minimized, maximized, or full-screen window state. |
| `public bool IsMinimizable { get; set; }` | Gets or sets whether the application allows the window to be minimized. |
| `public bool IsMaximizable { get; set; }` | Gets or sets whether the application allows the window to be maximized. |
| `public bool IsResizable { get; set; }` | Gets or sets whether the application allows interactive window resizing. |
| `public bool FullScreenEnabled { get; set; }` | Gets or sets whether the application allows entry into full screen. |
| `public WindowCapabilities Capabilities { get; }` | Gets the window operations and decorations supported by the current backend and application settings. |
| `public WindowButtonsLayout SystemButtonsLayout { get; }` | Gets the physical placement and order of the native caption controls. |
| `public WindowButtonsMetrics SystemButtonsMetrics { get; }` | Gets the logical native-caption insets and height for the custom client title bar. |
| `public (float Width, float Height) SystemButtonsSize { get; }` | Gets the total logical width and height reserved for integrated native caption controls. |
| `public BackdropResult AppliedBackdrop { get; }` | The backdrop accepted by this window's backend. |
| `public bool IsDialog { get; }` | A modal dialog: Esc closes it. |
| `public object? DialogResult { get; }` | The result given to `Nazor.NazorWindow.Close(System.Object)`. |
| `public IReadOnlyList<PixelRect>? LastFrameDamage { get; }` | The rectangles the last frame drew (physical pixels), or null when it drew everything. |
| `public WindowEnvironment Environment { get; }` | Gets the safe-area insets and keyboard occlusion relative to the client viewport. |
| `public WindowDecorationPolicy Decorations { get; set; }` |  |
| `public WindowDecorationResult? AppliedDecorations { get; }` |  |
| `public ImageSource? Icon { get; set; }` | The requested native icon; null restores the application icon. Changes apply asynchronously. |
| `public bool AllowPopupsOutsideWindow { get; set; }` |  |
| `public event Action? ChromeChanged` | Raised when an element of the window chrome changes owner. |
| `public event Action? Changed` | Raised on the UI thread after observable window presentation changes. |
| `public event Action<PresentedFrame>? FramePresented` | Reports backend submission completion on the UI dispatcher; busy dispatchers coalesce to the latest completed frame. The GPU worker frame number differs from the UI frame number during compositor animations. Completion does not measure physical scanout. |
| `public event Action<Type[]?>? HotReloadApplied` | Raised on the UI thread after the window re-rendered for a hot reload delta, with the updated types (null when unknown); the development channel forwards it to the tools. |
| `public event Action? Closed` | Occurs when closed is reported. |
| `public event Action? CapabilitiesChanged` | Occurs when capabilities changes. |
| `public event Action? SystemButtonsChanged` | Occurs when system controls changes. |
| `public event Action? StateChanged` | Occurs when state changes. |
| `public event Action? EnvironmentChanged` | Occurs when environment changes. |

See [title bars and platform integration](../Guide/14-Title-Bars-and-Platform-Integration.md) for complete configuration policies, capability checks, explicit fallback and User content lifecycle.
