# Nazor.Core

Component lifecycle, rendering trees, event callbacks, input contracts, navigation, media sources, and window abstractions.

This reference covers the public and protected API declared by this package. Inherited members follow the contract of their declaring type. Positional record parameters also define public properties with the same names.

## Types

- [`ILayoutTransition`](#type-ilayouttransition)
- [`LayoutTransitionOrigin`](#type-layouttransitionorigin)
- [`NavigationKind`](#type-navigationkind)

- [`IOverlayMotion`](#type-ioverlaymotion)

- [`LogicalItemPosition`](#type-logicalitemposition)

- [`ValidationFeedback`](#type-validationfeedback)

- [`CustomStateSet`](#type-customstateset)



- [`IFontSymbol`](#ifontsymbol)
- [`FontSymbol`](#fontsymbol)

- [`ITooltipController`](#type-itooltipcontroller)

- [`Backdrop`](#type-backdrop)
- [`BackdropResult`](#type-backdropresult)
- [`BackdropSelection`](#type-backdropselection)
- [`CascadingParameterAttribute`](#type-cascadingparameterattribute)
- [`CascadingTypeParameterAttribute`](#type-cascadingtypeparameterattribute)
- [`CascadingValue<TValue>`](#type-cascadingvaluetvalue)
- [`CascadingValueSource<TValue>`](#type-cascadingvaluesourcetvalue)
- [`ChangeEventArgs`](#type-changeeventargs)
- [`CommonBackdrop`](#type-commonbackdrop)
- [`ComponentBase`](#type-componentbase)
- [`ComponentNode`](#type-componentnode)
- [`Dispatcher`](#type-dispatcher)
- [`EditorRequiredAttribute`](#type-editorrequiredattribute)
- [`ElementNode`](#type-elementnode)
- [`ElementReference`](#type-elementreference)
- [`ElementStates`](#type-elementstates)
- [`EventCallback`](#type-eventcallback)
- [`EventCallback<TValue>`](#type-eventcallbacktvalue)
- [`EventCallbackFactory`](#type-eventcallbackfactory)
- [`EventCallbackWorkItem`](#type-eventcallbackworkitem)
- [`EventHandlerAttribute`](#type-eventhandlerattribute)
- [`FocusEventArgs`](#type-focuseventargs)
- [`HotReloadManager`](#type-hotreloadmanager)
- [`IClipboard`](#type-iclipboard)
- [`IComponent`](#type-icomponent)
- [`IElementInteraction`](#type-ielementinteraction)
- [`IEventCallback`](#type-ieventcallback)
- [`IHandleAfterRender`](#type-ihandleafterrender)
- [`IHandleEvent`](#type-ihandleevent)
- [`ImageData`](#type-imagedata)
- [`ImageErrorEventArgs`](#type-imageerroreventargs)
- [`ImageLoadEventArgs`](#type-imageloadeventargs)
- [`ImageSource`](#type-imagesource)
- [`InjectAttribute`](#type-injectattribute)
- [`IWindow`](#type-iwindow)
- [`NativeWindowHandle`](#type-nativewindowhandle)
- [`IStyleSheet`](#type-istylesheet)
- [`WindowComponentBase`](#type-windowcomponentbase)
- [`KeyBindings`](#type-keybindings)
- [`KeyboardEventArgs`](#type-keyboardeventargs)
- [`KeyModifiers`](#type-keymodifiers)
- [`LinuxBackdrop`](#type-linuxbackdrop)
- [`LocationChangedEventArgs`](#type-locationchangedeventargs)
- [`LocationChangingContext`](#type-locationchangingcontext)
- [`MemoryClipboard`](#type-memoryclipboard)
- [`NavigationEntry`](#type-navigationentry)
- [`NavigationManager`](#type-navigationmanager)
- [`Node`](#type-node)
- [`ParameterAttribute`](#type-parameterattribute)
- [`ParameterValue`](#type-parametervalue)
- [`ParameterView`](#type-parameterview)
- [`PlatformKind`](#type-platformkind)
- [`SystemColorProfile`](#type-systemcolorprofile)
- [`PointerButton`](#type-pointerbutton)
- [`PointerEventArgs`](#type-pointereventargs)
- [`RenderBatch`](#type-renderbatch)
- [`Renderer`](#type-renderer)
- [`RenderFragment`](#type-renderfragment)
- [`RenderFragment<TValue>`](#type-renderfragmenttvalue)
- [`RenderHandle`](#type-renderhandle)
- [`RenderTreeBuilder`](#type-rendertreebuilder)
- [`RenderTreeFrame`](#type-rendertreeframe)
- [`RenderTreeFrameType`](#type-rendertreeframetype)
- [`RootComponent`](#type-rootcomponent)
- [`RouteAttribute`](#type-routeattribute)
- [`RouteMatch`](#type-routematch)
- [`RouteTable`](#type-routetable)
- [`RuntimeHelpers`](#type-runtimehelpers)
- [`ScrollDeltaUnit`](#type-scrolldeltaunit)
- [`ScrollEventArgs`](#type-scrolleventargs)
- [`TextInputPolicy`](#type-textinputpolicy)
- [`TextNode`](#type-textnode)
- [`UIDispatcher`](#type-uidispatcher)
- [`WebRenderTreeBuilderExtensions`](#type-webrendertreebuilderextensions)
- [`WheelEventArgs`](#type-wheeleventargs)
- [`WindowCapabilities`](#type-windowcapabilities)
- [`WindowButton`](#type-windowbutton)
- [`WindowButtonsLayout`](#type-windowbuttonslayout)
- [`WindowButtonsMetrics`](#type-windowbuttonsmetrics)
- [`WindowEnvironment`](#type-windowenvironment)
- [`WindowOcclusion`](#type-windowocclusion)
- [`WindowOperations`](#type-windowoperations)
- [`WindowsBackdrop`](#type-windowsbackdrop)
- [`WindowState`](#type-windowstate)
- [`WindowsTitleBarHeight`](#type-windowstitlebarheight)

<a id="type-backdrop"></a>

## `Backdrop`

Namespace: `Nazor`

```csharp
public static class Backdrop
```

Provides predefined backdrop selections for common and platform-specific window materials.

[Source](../../Sources/Core/Nazor.Core/Windows/Backdrop.cs)

### Members

#### `ForPlatform`

```csharp
public static BackdropSelection ForPlatform(CommonBackdrop? preset = null, WindowsBackdrop? Windows = null, LinuxBackdrop? Linux = null, CommonBackdrop Default = CommonBackdrop.None)
```

Override selected mappings of a preset. Omitted backends inherit the preset.

| Parameter | Description |
| --- | --- |
| `preset` | The preset (CommonBackdrop?). |
| `Windows` | The windows (WindowsBackdrop?). |
| `Linux` | The optional Linux material override. |
| `Default` | The default (CommonBackdrop). |

Returns: The resulting BackdropSelection value.


<a id="type-backdropresult"></a>

## `BackdropResult`

Namespace: `Nazor`

```csharp
public readonly record struct BackdropResult(bool IsActive, string? Material = null)
```

What the backend accepted. Material is a diagnostic identifier, not a rendering guarantee.

| Parameter | Description |
| --- | --- |
| `IsActive` | The is active (bool). |
| `Material` | The material (string?). |

[Source](../../Sources/Core/Nazor.Core/Windows/Backdrop.cs)


<a id="type-backdropselection"></a>

## `BackdropSelection`

Namespace: `Nazor`

```csharp
public readonly record struct BackdropSelection
```

An immutable request, resolved by the native window rather than by the page.

[Source](../../Sources/Core/Nazor.Core/Windows/Backdrop.cs)

### Members

#### `Preset`

```csharp
public CommonBackdrop? Preset { get; }
```

Gets the optional portable backdrop preset.

#### `Windows`

```csharp
public WindowsBackdrop? Windows { get; }
```

Gets the optional Windows-specific backdrop material.

#### `Linux`

```csharp
public LinuxBackdrop? Linux { get; }
```

Gets the optional Linux-specific backdrop material.

#### `Default`

```csharp
public CommonBackdrop Default { get; }
```

Gets the portable backdrop fallback used when a platform-specific material is unavailable.

#### `implicit operator BackdropSelection`

```csharp
public static implicit operator BackdropSelection(CommonBackdrop value)
```

Converts a CommonBackdrop value to BackdropSelection.

| Parameter | Description |
| --- | --- |
| `value` | The value to use. |

Returns: The resulting BackdropSelection value.

#### `implicit operator BackdropSelection`

```csharp
public static implicit operator BackdropSelection(WindowsBackdrop value)
```

Converts a WindowsBackdrop value to BackdropSelection.

| Parameter | Description |
| --- | --- |
| `value` | The value to use. |

Returns: The resulting BackdropSelection value.

#### `implicit operator BackdropSelection`

```csharp
public static implicit operator BackdropSelection(LinuxBackdrop value)
```

Converts a LinuxBackdrop value to BackdropSelection.

| Parameter | Description |
| --- | --- |
| `value` | The value to use. |

Returns: The resulting BackdropSelection value.


<a id="type-cascadingparameterattribute"></a>

## `CascadingParameterAttribute`

Namespace: `Nazor`

```csharp
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)] public sealed class CascadingParameterAttribute : Attribute
```

A value supplied by the nearest matching CascadingValue or a root DI registration.

[Source](../../Sources/Core/Nazor.Core/ComponentModel/IComponent.cs)

### Members

#### `Name`

```csharp
public string? Name { get; set;}
```

Gets or sets the optional name used to match the cascading value source.


<a id="type-cascadingtypeparameterattribute"></a>

## `CascadingTypeParameterAttribute`

Namespace: `Nazor`

```csharp
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)] public sealed class CascadingTypeParameterAttribute(string name) : Attribute
```

Propagates a named generic type parameter to descendant components in markup.

| Parameter | Description |
| --- | --- |
| `name` | The name used to identify the parameter, property, resource, or registration. |

[Source](../../Sources/Core/Nazor.Core/ComponentModel/IComponent.cs)

### Members

#### `Name`

```csharp
public string Name { get; }
```

Gets the generic type parameter name propagated to descendant components.


<a id="type-cascadingvaluetvalue"></a>

## `CascadingValue<TValue>`

Namespace: `Nazor`

```csharp
public sealed class CascadingValue<TValue> : ComponentBase, ICascadingValueSupplier
```

Supplies a typed value to descendants, matched by type and optional name.

| Type parameter | Description |
| --- | --- |
| `TValue` | The type of the component, callback, or cascading value. |

[Source](../../Sources/Core/Nazor.Core/ComponentModel/CascadingValue.cs)

### Members

#### `Value`

```csharp
[Parameter] public TValue Value { get; set;}
```

Gets or sets the value supplied to descendant components.

#### `Name`

```csharp
[Parameter] public string? Name { get; set;}
```

Gets or sets the optional name used to match descendant cascading parameters.

#### `IsFixed`

```csharp
[Parameter] public bool IsFixed { get; set;}
```

Gets or sets whether the cascading value is fixed and consumers can skip change subscriptions.

#### `ChildContent`

```csharp
[Parameter] public RenderFragment? ChildContent { get; set;}
```

Gets or sets the render fragment supplied between the component's opening and closing tags.

#### `SetParametersAsync`

```csharp
public override Task SetParametersAsync(ParameterView parameters)
```

Assigns component parameters and runs the initialization and parameter lifecycle callbacks.

| Parameter | Description |
| --- | --- |
| `parameters` | The parameter names and values to supply to the component. |

Returns: A task that completes when the operation has finished.

#### `OnParametersSetAsync`

```csharp
protected override Task OnParametersSetAsync()
```

Asynchronously updates component state after parameter values have been assigned.

Returns: A task that completes when the operation has finished.

#### `BuildRenderTree`

```csharp
protected override void BuildRenderTree(RenderTreeBuilder builder)
```

Writes the render frames for CascadingValue to the supplied builder.

| Parameter | Description |
| --- | --- |
| `builder` | The builder that receives the render frames. |


<a id="type-cascadingvaluesourcetvalue"></a>

## `CascadingValueSource<TValue>`

Namespace: `Nazor`

```csharp
public sealed class CascadingValueSource<TValue> : ICascadingValueSupplier
```

A root cascading value. Non-fixed sources notify only their subscribed components.

| Type parameter | Description |
| --- | --- |
| `TValue` | The type of the component, callback, or cascading value. |

[Source](../../Sources/Core/Nazor.Core/ComponentModel/CascadingValue.cs)

### Members

#### `CascadingValueSource`

```csharp
public CascadingValueSource(TValue value, bool isFixed)
```

Initializes a new instance of CascadingValueSource.

| Parameter | Description |
| --- | --- |
| `value` | The value to use. |
| `isFixed` | Whether consumers can treat the value as immutable and skip change subscriptions. |

#### `CascadingValueSource`

```csharp
public CascadingValueSource(string? name, TValue value, bool isFixed)
```

Initializes a new instance of CascadingValueSource.

| Parameter | Description |
| --- | --- |
| `name` | The name used to identify the parameter, property, resource, or registration. |
| `value` | The value to use. |
| `isFixed` | Whether consumers can treat the value as immutable and skip change subscriptions. |

#### `IsFixed`

```csharp
public bool IsFixed { get; }
```

Gets whether consumers can treat this source as immutable.

#### `NotifyChangedAsync`

```csharp
public Task NotifyChangedAsync(TValue value)
```

Notifies subscribed components that the cascading value has changed, optionally replacing the current value.

| Parameter | Description |
| --- | --- |
| `value` | The value to use. |

Returns: A task that completes when the operation has finished.

#### `NotifyChangedAsync`

```csharp
public Task NotifyChangedAsync()
```

Notifies subscribed components that the cascading value has changed, optionally replacing the current value.

Returns: A task that completes when the operation has finished.


<a id="type-changeeventargs"></a>

## `ChangeEventArgs`

Namespace: `Nazor.Input`

```csharp
public class ChangeEventArgs : EventArgs
```

Arguments of value changes raised by input controls.

[Source](../../Sources/Core/Nazor.Core/Input/EventArgs.cs)

### Members

#### `Value`

```csharp
public object? Value { get; init;}
```

Gets or sets the new value reported by the input or change event.


<a id="type-commonbackdrop"></a>

## `CommonBackdrop`

Namespace: `Nazor`

```csharp
public enum CommonBackdrop
```

Portable presets. The backend chooses its own material or an opaque fallback.

[Source](../../Sources/Core/Nazor.Core/Windows/Backdrop.cs)

### Members

#### `None`

```csharp
None
```

Requests no client-area backdrop material; system decorations retain their native appearance.

#### `Blur`

```csharp
Blur
```

Requests Acrylic on Windows or background blur on Linux, when supported.

#### `Solid`

```csharp
Solid
```

Requests Mica on Windows or no compositor effect on Linux.


<a id="type-componentnode"></a>

## `ComponentNode`

Namespace: `Nazor.Rendering`

```csharp
public sealed class ComponentNode : Node
```

A component instance in the logical tree; its children are its render output.

[Source](../../Sources/Core/Nazor.Core/Rendering/Nodes.cs)

### Members

#### `Component`

```csharp
public IComponent Component {get; }
```

Gets the component instance represented or hosted by this node.

#### `DeclaringScope`

```csharp
public Type? DeclaringScope {get; }
```

Gets the type whose markup declares this component.

#### `DeclaringComponent`

```csharp
public IComponent? DeclaringComponent {get; }
```

Gets the component instance whose markup declares the content.

#### `StyleName`

```csharp
public string StyleName { get; }
```

The unqualified component name, without generic arity.

#### `HasNamedElement`

```csharp
public bool HasNamedElement {get; }
```

Controls already represented by a named element keep ordinary element-selector semantics.

#### `ToString`

```csharp
public override string ToString()
```

Returns a textual representation of this value.

Returns: The textual representation of this value.


<a id="type-dispatcher"></a>

## `Dispatcher`

Namespace: `Nazor`

```csharp
public abstract class Dispatcher
```

Serialises work on the UI thread. Component state, rendering and events are only touched from the dispatcher; other threads switch to it with `InvokeAsync`.

[Source](../../Sources/Core/Nazor.Core/ComponentModel/Dispatcher.cs)

### Members

#### `CreateDefault`

```csharp
public static Dispatcher CreateDefault()
```

Creates a `UIDispatcher` owned by the calling thread.

Returns: The resulting Dispatcher value.

#### `CheckAccess`

```csharp
public abstract bool CheckAccess()
```

Determines whether the current thread is the dispatcher thread.

Returns: true if the current thread owns the UI dispatcher; otherwise, false.

#### `SynchronizationContext`

```csharp
public virtual SynchronizationContext? SynchronizationContext {get; }
```

The context installed while component code runs, so that `await` continuations return to the dispatcher. Null when the dispatcher relies on an ambient context.

#### `AssertAccess`

```csharp
public void AssertAccess()
```

Throws InvalidOperationException when the current thread is not the dispatcher thread.

#### `InvokeAsync`

```csharp
public abstract Task InvokeAsync(Action workItem)
```

Schedules the supplied work on the UI dispatcher and observes its completion.

| Parameter | Description |
| --- | --- |
| `workItem` | The work to execute on the UI dispatcher. |

Returns: A task that completes when the operation has finished.

#### `InvokeAsync`

```csharp
public abstract Task InvokeAsync(Func<Task> workItem)
```

Schedules the supplied work on the UI dispatcher and observes its completion.

| Parameter | Description |
| --- | --- |
| `workItem` | The work to execute on the UI dispatcher. |

Returns: A task that completes when the operation has finished.

#### `InvokeAsync`

```csharp
public abstract Task<TResult> InvokeAsync<TResult>(Func<TResult> workItem)
```

Schedules the supplied work on the UI dispatcher and observes its completion.

| Type parameter | Description |
| --- | --- |
| `TResult` | The type of the result. |

| Parameter | Description |
| --- | --- |
| `workItem` | The work to execute on the UI dispatcher. |

Returns: A task whose result is the value produced by the operation.

#### `InvokeAsync`

```csharp
public abstract Task<TResult> InvokeAsync<TResult>(Func<Task<TResult>> workItem)
```

Schedules the supplied work on the UI dispatcher and observes its completion.

| Type parameter | Description |
| --- | --- |
| `TResult` | The type of the result. |

| Parameter | Description |
| --- | --- |
| `workItem` | The work to execute on the UI dispatcher. |

Returns: A task whose result is the value produced by the operation.

#### `UnhandledException`

```csharp
public event EventHandler<Exception>? UnhandledException;
```

Raised for exceptions thrown by posted work that nobody observes (for example `async void`).

#### `OnUnhandledException`

```csharp
protected bool OnUnhandledException(Exception exception)
```

Reports an unhandled exception to registered dispatcher exception handlers.

| Parameter | Description |
| --- | --- |
| `exception` | The unhandled exception to report. |

Returns: true if an exception handler was registered; otherwise, false.


<a id="type-editorrequiredattribute"></a>

## `EditorRequiredAttribute`

Namespace: `Nazor`

```csharp
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)] public sealed class EditorRequiredAttribute : Attribute
```

Requires an explicit assignment when a component is used in Nazor markup.

[Source](../../Sources/Core/Nazor.Core/ComponentModel/IComponent.cs)


<a id="type-elementnode"></a>

## `ElementNode`

Namespace: `Nazor.Rendering`

```csharp
public sealed class ElementNode : Node
```

A control's element: a name used by type selectors, attributes and physical children.

[Source](../../Sources/Core/Nazor.Core/Rendering/Nodes.cs#L146)


### `IsDisabled`

```csharp
public bool IsDisabled { get; }
```

Gets whether this element or an ancestor disables user interaction.

[Source](../../Sources/Core/Nazor.Core/Rendering/Nodes.cs#L149)


### `Name`

```csharp
public string Name { get; }
```

Gets the element name matched by NSS type selectors.

[Source](../../Sources/Core/Nazor.Core/Rendering/Nodes.cs#L174)


### `Scope`

```csharp
public Type? Scope { get; }
```

The component whose markup declared the control that produced this element.

[Source](../../Sources/Core/Nazor.Core/Rendering/Nodes.cs#L177)


### `Owner`

```csharp
public IComponent? Owner { get; }
```

The component whose render output contains this element.

[Source](../../Sources/Core/Nazor.Core/Rendering/Nodes.cs#L180)


### `DeclaringComponent`

```csharp
public IComponent? DeclaringComponent { get; }
```

The instance whose markup declared the control, including projected content.

[Source](../../Sources/Core/Nazor.Core/Rendering/Nodes.cs#L156)


### `Key`

```csharp
public object? Key { get; internal set; }
```

Gets the key used to retain identity across render updates.

[Source](../../Sources/Core/Nazor.Core/Rendering/Nodes.cs#L186)


### `Attributes`

```csharp
public IReadOnlyDictionary<string, object?> Attributes { get; }
```

Gets the element attributes or attribute-selector constraints.

[Source](../../Sources/Core/Nazor.Core/Rendering/Nodes.cs#L189)


### `Class`

```csharp
public string? Class { get; }
```

Gets the space-separated NSS classes applied to the element.

[Source](../../Sources/Core/Nazor.Core/Rendering/Nodes.cs#L193)


### `Id`

```csharp
public string? Id { get; }
```

Gets the element's id attribute, or null when no identifier is assigned.

[Source](../../Sources/Core/Nazor.Core/Rendering/Nodes.cs#L196)


### `States`

```csharp
public ElementStates States { get; }
```

Gets the element's current interaction state flags.

[Source](../../Sources/Core/Nazor.Core/Rendering/Nodes.cs#L199)


### `StatesChanged`

```csharp
public event Action<ElementNode, ElementStates>? StatesChanged;
```

Raised after `States` changes; styling uses it for pseudo-classes.

[Source](../../Sources/Core/Nazor.Core/Rendering/Nodes.cs#L202)


### `SetState`

```csharp
public void SetState(ElementStates state, bool value)
```

Adds or removes the specified interaction state and notifies the renderer when it changes.

- `state`: The state (ElementStates).

- `value`: The value to use.

[Source](../../Sources/Core/Nazor.Core/Rendering/Nodes.cs#L207)


### `Children`

```csharp
public IReadOnlyList<Node> Children { get; }
```

Physical children: elements and text, with components and regions flattened.

[Source](../../Sources/Core/Nazor.Core/Rendering/Nodes.cs#L217)


### `ToString`

```csharp
public override string ToString()
```

Returns a textual representation of this value.

Returns: The textual representation of this value.

[Source](../../Sources/Core/Nazor.Core/Rendering/Nodes.cs#L223)


<a id="type-elementreference"></a>

## `ElementReference`

Namespace: `Nazor`

```csharp
public readonly struct ElementReference(ElementNode element)
```

An element retained by the renderer. Interaction is supplied by the window's input manager.

| Parameter | Description |
| --- | --- |
| `element` | The element to inspect or update. |

[Source](../../Sources/Core/Nazor.Core/Input/ElementReference.cs)

### Members

#### `Element`

```csharp
public ElementNode? Element { get; }
```

Gets the retained element associated with this control or element reference.

#### `FocusAsync`

```csharp
public Task FocusAsync(bool preventScroll = false)
```

Requests keyboard focus for the target element, optionally preserving scroll offsets.

| Parameter | Description |
| --- | --- |
| `preventScroll` | Whether focusing should preserve the current scroll position. |

Returns: A task that completes when the operation has finished.

#### `CapturePointer`

```csharp
public bool CapturePointer()
```

Captures pointer input for the target element until capture is released or canceled.

Returns: true if pointer capture was acquired; otherwise, false.

#### `IsPointerCaptured`

```csharp
public bool IsPointerCaptured { get; }
```

True while the attached element owns pointer capture. Detached elements, canceled capture and providers without capture-state support return false. Custom `IElementInteraction` implementations can override `bool HasPointerCapture(ElementNode element)`; its default implementation returns false.

#### `ReleasePointerCapture`

```csharp
public void ReleasePointerCapture()
```

Releases pointer capture held by the target element.

#### `ScrollTo`

```csharp
public bool ScrollTo(double? left, double? top)
```

Scrolls a scroll container to an offset (null keeps that axis), within its limits; `onscroll` follows. False when nothing moved, or when no window shows the element yet.

| Parameter | Description |
| --- | --- |
| `left` | The requested horizontal scroll offset, or null to keep it unchanged. |
| `top` | The requested vertical scroll offset, or null to keep it unchanged. |

Returns: true when the operation succeeds or the condition is satisfied; otherwise, false.


<a id="type-elementstates"></a>

## `ElementStates`

Namespace: `Nazor.Rendering`

```csharp
[Flags] public enum ElementStates
```

Interaction states maintained by input and controls, observed by styling.

[Source](../../Sources/Core/Nazor.Core/Rendering/Nodes.cs)

### Members

#### `None`

```csharp
None = 0
```

No interaction state is active.

#### `Hover`

```csharp
Hover = 1
```

The pointer is over the element or its relevant descendant.

#### `Pressed`

```csharp
Pressed = 2
```

The element is in its active pressed state.

#### `Focus`

```csharp
Focus = 4
```

The element has keyboard focus.

#### `FocusVisible`

```csharp
FocusVisible = 8
```

The element has focus that should display a keyboard focus indicator.

#### `Disabled`

```csharp
Disabled = 16
```

The element is disabled for user interaction.

#### `Checked`

```csharp
Checked = 32
```

The element represents a checked or selected value.

#### `FocusWithin`

```csharp
FocusWithin = 64
```

The element or one of its descendants has the focus (`:focus-within`).

#### `WheelScrolling`

```csharp
WheelScrolling = 128
```

The element is receiving wheel scrolling, including its configured idle timeout. NSS exposes this state as `:wheel-scrolling`.


<a id="type-eventcallback"></a>

## `EventCallback`

Namespace: `Nazor`

```csharp
public readonly struct EventCallback : IEventCallback, IEquatable<EventCallback>
```

A delegate bound to the component that should react to it. When the receiver handles events (as `ComponentBase` does) invoking the callback also re-renders the receiver.

[Source](../../Sources/Core/Nazor.Core/ComponentModel/EventCallback.cs)

### Members

#### `Factory`

```csharp
public static readonly EventCallbackFactory Factory ;
```

The shared factory used to create event callbacks and binding callbacks.

#### `Empty`

```csharp
public static readonly EventCallback Empty ;
```

An empty event callback value.

#### `EventCallback`

```csharp
public EventCallback(IHandleEvent? receiver, MulticastDelegate? @delegate)
```

Initializes a new instance of EventCallback.

| Parameter | Description |
| --- | --- |
| `receiver` | The object that receives the callback and controls event dispatch. |
| `delegate` | The delegate to invoke when the callback is dispatched. |

#### `HasDelegate`

```csharp
public bool HasDelegate {get; }
```

Gets whether a callback delegate has been assigned.

#### `InvokeAsync`

```csharp
public Task InvokeAsync(object? arg)
```

Invokes the event callback through its receiver, using the supplied event argument when present.

| Parameter | Description |
| --- | --- |
| `arg` | The event argument passed to the handler. |

Returns: A task that completes when the operation has finished.

#### `InvokeAsync`

```csharp
public Task InvokeAsync()
```

Invokes the event callback through its receiver, using the supplied event argument when present.

Returns: A task that completes when the operation has finished.

#### `Equals`

```csharp
public bool Equals(EventCallback other)
```

Determines whether the supplied value is equal to this instance.

| Parameter | Description |
| --- | --- |
| `other` | The value to compare with this instance. |

Returns: true if the supplied value satisfies the comparison; otherwise, false.

#### `Equals`

```csharp
public override bool Equals(object? obj)
```

Determines whether the supplied value is equal to this instance.

| Parameter | Description |
| --- | --- |
| `obj` | The value to compare with this instance. |

Returns: true if the supplied value satisfies the comparison; otherwise, false.

#### `GetHashCode`

```csharp
public override int GetHashCode()
```

Returns a hash code consistent with equality for this value.

Returns: The hash code for this value.


<a id="type-eventcallbacktvalue"></a>

## `EventCallback<TValue>`

Namespace: `Nazor`

```csharp
public readonly struct EventCallback<TValue> : IEventCallback, IEquatable<EventCallback<TValue>>
```

An `EventCallback` with a typed argument.

| Type parameter | Description |
| --- | --- |
| `TValue` | The type of the component, callback, or cascading value. |

[Source](../../Sources/Core/Nazor.Core/ComponentModel/EventCallback.cs)

### Members

#### `Empty`

```csharp
public static readonly EventCallback<TValue> Empty ;
```

An empty event callback value.

#### `EventCallback`

```csharp
public EventCallback(IHandleEvent? receiver, MulticastDelegate? @delegate)
```

Initializes a new instance of EventCallback.

| Parameter | Description |
| --- | --- |
| `receiver` | The object that receives the callback and controls event dispatch. |
| `delegate` | The delegate to invoke when the callback is dispatched. |

#### `HasDelegate`

```csharp
public bool HasDelegate {get; }
```

Gets whether a callback delegate has been assigned.

#### `InvokeAsync`

```csharp
public Task InvokeAsync(TValue arg)
```

Invokes the event callback through its receiver, using the supplied event argument when present.

| Parameter | Description |
| --- | --- |
| `arg` | The event argument passed to the handler. |

Returns: A task that completes when the operation has finished.

#### `InvokeAsync`

```csharp
public Task InvokeAsync()
```

Invokes the event callback through its receiver, using the supplied event argument when present.

Returns: A task that completes when the operation has finished.

#### `Equals`

```csharp
public bool Equals(EventCallback<TValue> other)
```

Determines whether the supplied value is equal to this instance.

| Parameter | Description |
| --- | --- |
| `other` | The value to compare with this instance. |

Returns: true if the supplied value satisfies the comparison; otherwise, false.

#### `Equals`

```csharp
public override bool Equals(object? obj)
```

Determines whether the supplied value is equal to this instance.

| Parameter | Description |
| --- | --- |
| `obj` | The value to compare with this instance. |

Returns: true if the supplied value satisfies the comparison; otherwise, false.

#### `GetHashCode`

```csharp
public override int GetHashCode()
```

Returns a hash code consistent with equality for this value.

Returns: The hash code for this value.


<a id="type-eventcallbackfactory"></a>

## `EventCallbackFactory`

Namespace: `Nazor`

```csharp
public sealed class EventCallbackFactory
```

Creates event callbacks; used by the code generated from markup.

[Source](../../Sources/Core/Nazor.Core/ComponentModel/EventCallback.cs)

### Members

#### `Create`

```csharp
public EventCallback Create(object receiver, EventCallback callback)
```

Creates an event callback associated with the supplied receiver.

| Parameter | Description |
| --- | --- |
| `receiver` | The object that receives the callback and controls event dispatch. |
| `callback` | The delegate to invoke when the callback is dispatched. |

Returns: The event callback associated with the receiver.

#### `Create`

```csharp
public EventCallback Create(object receiver, Action callback)
```

Creates an event callback associated with the supplied receiver.

| Parameter | Description |
| --- | --- |
| `receiver` | The object that receives the callback and controls event dispatch. |
| `callback` | The delegate to invoke when the callback is dispatched. |

Returns: The event callback associated with the receiver.

#### `Create`

```csharp
public EventCallback Create(object receiver, Action<object?> callback)
```

Creates an event callback associated with the supplied receiver.

| Parameter | Description |
| --- | --- |
| `receiver` | The object that receives the callback and controls event dispatch. |
| `callback` | The delegate to invoke when the callback is dispatched. |

Returns: The event callback associated with the receiver.

#### `Create`

```csharp
public EventCallback Create(object receiver, Func<Task> callback)
```

Creates an event callback associated with the supplied receiver.

| Parameter | Description |
| --- | --- |
| `receiver` | The object that receives the callback and controls event dispatch. |
| `callback` | The delegate to invoke when the callback is dispatched. |

Returns: The event callback associated with the receiver.

#### `Create`

```csharp
public EventCallback Create(object receiver, Func<object?, Task> callback)
```

Creates an event callback associated with the supplied receiver.

| Parameter | Description |
| --- | --- |
| `receiver` | The object that receives the callback and controls event dispatch. |
| `callback` | The delegate to invoke when the callback is dispatched. |

Returns: The event callback associated with the receiver.

#### `Create`

```csharp
public EventCallback<TValue> Create<TValue>(object receiver, EventCallback callback)
```

Creates an event callback associated with the supplied receiver.

| Type parameter | Description |
| --- | --- |
| `TValue` | The type of the component, callback, or cascading value. |

| Parameter | Description |
| --- | --- |
| `receiver` | The object that receives the callback and controls event dispatch. |
| `callback` | The delegate to invoke when the callback is dispatched. |

Returns: The event callback associated with the receiver.

#### `Create`

```csharp
public EventCallback<TValue> Create<TValue>(object receiver, EventCallback<TValue> callback)
```

Creates an event callback associated with the supplied receiver.

| Type parameter | Description |
| --- | --- |
| `TValue` | The type of the component, callback, or cascading value. |

| Parameter | Description |
| --- | --- |
| `receiver` | The object that receives the callback and controls event dispatch. |
| `callback` | The delegate to invoke when the callback is dispatched. |

Returns: The event callback associated with the receiver.

#### `Create`

```csharp
public EventCallback<TValue> Create<TValue>(object receiver, Action callback)
```

Creates an event callback associated with the supplied receiver.

| Type parameter | Description |
| --- | --- |
| `TValue` | The type of the component, callback, or cascading value. |

| Parameter | Description |
| --- | --- |
| `receiver` | The object that receives the callback and controls event dispatch. |
| `callback` | The delegate to invoke when the callback is dispatched. |

Returns: The event callback associated with the receiver.

#### `Create`

```csharp
public EventCallback<TValue> Create<TValue>(object receiver, Action<TValue> callback)
```

Creates an event callback associated with the supplied receiver.

| Type parameter | Description |
| --- | --- |
| `TValue` | The type of the component, callback, or cascading value. |

| Parameter | Description |
| --- | --- |
| `receiver` | The object that receives the callback and controls event dispatch. |
| `callback` | The delegate to invoke when the callback is dispatched. |

Returns: The event callback associated with the receiver.

#### `Create`

```csharp
public EventCallback<TValue> Create<TValue>(object receiver, Func<Task> callback)
```

Creates an event callback associated with the supplied receiver.

| Type parameter | Description |
| --- | --- |
| `TValue` | The type of the component, callback, or cascading value. |

| Parameter | Description |
| --- | --- |
| `receiver` | The object that receives the callback and controls event dispatch. |
| `callback` | The delegate to invoke when the callback is dispatched. |

Returns: The event callback associated with the receiver.

#### `Create`

```csharp
public EventCallback<TValue> Create<TValue>(object receiver, Func<TValue, Task> callback)
```

Creates an event callback associated with the supplied receiver.

| Type parameter | Description |
| --- | --- |
| `TValue` | The type of the component, callback, or cascading value. |

| Parameter | Description |
| --- | --- |
| `receiver` | The object that receives the callback and controls event dispatch. |
| `callback` | The delegate to invoke when the callback is dispatched. |

Returns: The event callback associated with the receiver.

#### `CreateInferred`

```csharp
public EventCallback<TValue> CreateInferred<TValue>(object receiver, Action<TValue> callback, TValue value)
```

Creates a typed event callback using the value to infer the argument type.

| Type parameter | Description |
| --- | --- |
| `TValue` | The type of the component, callback, or cascading value. |

| Parameter | Description |
| --- | --- |
| `receiver` | The object that receives the callback and controls event dispatch. |
| `callback` | The delegate to invoke when the callback is dispatched. |
| `value` | The value to use. |

Returns: The typed event callback associated with the receiver.

#### `CreateInferred`

```csharp
public EventCallback<TValue> CreateInferred<TValue>(object receiver, Func<TValue, Task> callback, TValue value)
```

Creates a typed event callback using the value to infer the argument type.

| Type parameter | Description |
| --- | --- |
| `TValue` | The type of the component, callback, or cascading value. |

| Parameter | Description |
| --- | --- |
| `receiver` | The object that receives the callback and controls event dispatch. |
| `callback` | The delegate to invoke when the callback is dispatched. |
| `value` | The value to use. |

Returns: The typed event callback associated with the receiver.


<a id="type-eventcallbackworkitem"></a>

## `EventCallbackWorkItem`

Namespace: `Nazor`

```csharp
public readonly struct EventCallbackWorkItem
```

The delegate of an event callback, executed by the receiving component.

[Source](../../Sources/Core/Nazor.Core/ComponentModel/EventCallback.cs)

### Members

#### `Empty`

```csharp
public static readonly EventCallbackWorkItem Empty ;
```

An empty event callback work item value.

#### `EventCallbackWorkItem`

```csharp
public EventCallbackWorkItem(MulticastDelegate? @delegate)
```

Initializes a new instance of EventCallbackWorkItem.

| Parameter | Description |
| --- | --- |
| `delegate` | The delegate to invoke when the callback is dispatched. |

#### `InvokeAsync`

```csharp
public Task InvokeAsync(object? arg)
```

Invokes the event callback through its receiver, using the supplied event argument when present.

| Parameter | Description |
| --- | --- |
| `arg` | The event argument passed to the handler. |

Returns: A task that completes when the operation has finished.


<a id="type-eventhandlerattribute"></a>

## `EventHandlerAttribute`

Namespace: `Nazor`

```csharp
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)] public sealed class EventHandlerAttribute(string attributeName, Type eventArgsType, bool enableStopPropagation, bool enablePreventDefault) : Attribute
```

Declares a directive event attribute such as `@onclick` and the type of its arguments. The compiler makes the attribute available in markup; the value reaches the component parameter with the same name, compared case-insensitively (for example `OnClick`).

| Parameter | Description |
| --- | --- |
| `attributeName` | The directive event attribute name recognized by the compiler. |
| `eventArgsType` | The CLR type used for this event's arguments. |
| `enableStopPropagation` | Whether markup may request that this event stop propagating. |
| `enablePreventDefault` | Whether markup may request prevention of the event's default action. |

[Source](../../Sources/Core/Nazor.Core/ComponentModel/IComponent.cs)

### Members

#### `EventHandlerAttribute`

```csharp
public EventHandlerAttribute(string attributeName, Type eventArgsType)
```

Initializes a new instance of EventHandlerAttribute.

| Parameter | Description |
| --- | --- |
| `attributeName` | The directive event attribute name recognized by the compiler. |
| `eventArgsType` | The CLR type used for this event's arguments. |

#### `AttributeName`

```csharp
public string AttributeName { get; }
```

Gets the directive event attribute name recognized in markup.

#### `EventArgsType`

```csharp
public Type EventArgsType { get; }
```

Gets the CLR type used for the event's arguments.

#### `EnableStopPropagation`

```csharp
public bool EnableStopPropagation { get; }
```

Gets whether the markup event supports the stopPropagation modifier.

#### `EnablePreventDefault`

```csharp
public bool EnablePreventDefault { get; }
```

Gets whether the markup event supports the preventDefault modifier.


<a id="type-focuseventargs"></a>

## `FocusEventArgs`

Namespace: `Nazor.Input`

```csharp
public class FocusEventArgs : EventArgs
```

Describes a keyboard focus transition between elements.

[Source](../../Sources/Core/Nazor.Core/Input/EventArgs.cs)

### Members

#### `RelatedTarget`

```csharp
public Rendering.ElementNode? RelatedTarget { get; init;}
```

Gets or sets the other element involved in the focus transition, or null when unavailable.


<a id="type-hotreloadmanager"></a>

## `HotReloadManager`

Namespace: `Nazor`

```csharp
public static class HotReloadManager
```

Receives .NET Hot Reload deltas (dotnet watch, Visual Studio) for the Nazor runtime: clears the reflection caches and tells hosts, through `DeltaApplied`, to re-render their windows. The runtime calls the handler methods on an arbitrary thread, never the UI thread under dotnet watch; subscribers only set flags or post to a dispatcher.

[Source](../../Sources/Core/Nazor.Core/HotReload/HotReloadManager.cs)

### Members

#### `IsSupported`

```csharp
public static bool IsSupported {get; }
```

True when the process accepts metadata updates (`DOTNET_MODIFIABLE_ASSEMBLIES=debug`, set by dotnet watch and the debugger).

#### `DeltaApplied`

```csharp
public static event Action<Type[]?>? DeltaApplied;
```

Raised after every applied delta, once every ClearCache handler ran, on an arbitrary thread with the updated types (null when unknown: assume all). Each subscriber runs in its own try/catch; the first failure goes to `LastError`. Subscribers must only set flags or post to a dispatcher, never wait.

#### `ContentUpdated`

```csharp
public static event Action<string, string, byte[]>? ContentUpdated;
```

Reports content updates from a compatible hot reload agent as assembly name, relative path, and contents. The built-in hot reload workflow does not raise this event.

#### `LastError`

```csharp
public static Exception? LastError { get; private set;}
```

The first subscriber failure of the last delta, or null; reset at every delta.

#### `ApplyUpdate`

```csharp
public static void ApplyUpdate(Type[]? updatedTypes)
```

Clears the caches and raises `DeltaApplied`, as the runtime does after a delta: for tests and for agents that apply deltas themselves.

| Parameter | Description |
| --- | --- |
| `updatedTypes` | The updated types (Type[]?). |


<a id="type-iclipboard"></a>

## `IClipboard`

Namespace: `Nazor.Input`

```csharp
public interface IClipboard
```

Text clipboard used by editable controls; the platform provides the system one.

[Source](../../Sources/Core/Nazor.Core/Input/Clipboard.cs)

### Members

#### `GetText`

```csharp
string? GetText()
```

The clipboard text, or null when it holds no text or cannot be opened.

Returns: The resulting string? value.

#### `SetText`

```csharp
bool SetText(string text)
```

Replaces the clipboard content; returns false when the clipboard is busy.

| Parameter | Description |
| --- | --- |
| `text` | The text to process. |

Returns: true when the operation succeeds or the condition is satisfied; otherwise, false.


<a id="type-icomponent"></a>

## `IComponent`

Namespace: `Nazor`

```csharp
public interface IComponent
```

A unit of UI that receives parameters and renders through its `RenderHandle`.

[Source](../../Sources/Core/Nazor.Core/ComponentModel/IComponent.cs)

### Members

#### `Attach`

```csharp
void Attach(RenderHandle renderHandle)
```

Binds the component to the renderer that owns it. Called once, before any parameters.

| Parameter | Description |
| --- | --- |
| `renderHandle` | The handle connecting the component to its owning renderer. |

#### `SetParametersAsync`

```csharp
Task SetParametersAsync(ParameterView parameters)
```

Supplies the parameters from the parent frame, or from the host for a root component.

| Parameter | Description |
| --- | --- |
| `parameters` | The parameter names and values to supply to the component. |

Returns: A task that completes when the operation has finished.


<a id="type-ielementinteraction"></a>

## `IElementInteraction`

Namespace: `Nazor`

```csharp
public interface IElementInteraction
```

Window interaction implemented by Nazor.Input; keeps the component runtime platform independent.

[Source](../../Sources/Core/Nazor.Core/Input/ElementReference.cs)

### Members

#### `FocusAsync`

```csharp
Task FocusAsync(ElementNode element, bool preventScroll)
```

Requests keyboard focus for the target element, optionally preserving scroll offsets.

| Parameter | Description |
| --- | --- |
| `element` | The element to inspect or update. |
| `preventScroll` | Whether focusing should preserve the current scroll position. |

Returns: A task that completes when the operation has finished.

#### `CapturePointer`

```csharp
bool CapturePointer(ElementNode element)
```

Captures pointer input for the target element until capture is released or canceled.

| Parameter | Description |
| --- | --- |
| `element` | The element to inspect or update. |

Returns: true if pointer capture was acquired; otherwise, false.

#### `ReleasePointerCapture`

```csharp
void ReleasePointerCapture(ElementNode element)
```

Releases pointer capture held by the target element.

| Parameter | Description |
| --- | --- |
| `element` | The element to inspect or update. |

#### `ScrollTo`

```csharp
bool ScrollTo(ElementNode element, double? left, double? top)
```

Requests scrolling of the element to the supplied logical offsets.

| Parameter | Description |
| --- | --- |
| `element` | The element to inspect or update. |
| `left` | The requested horizontal scroll offset, or null to keep it unchanged. |
| `top` | The requested vertical scroll offset, or null to keep it unchanged. |

Returns: true when the operation succeeds or the condition is satisfied; otherwise, false.


<a id="type-ieventcallback"></a>

## `IEventCallback`

Namespace: `Nazor`

```csharp
public interface IEventCallback
```

An event callback whose argument type is known only at runtime, as stored in element attributes.

[Source](../../Sources/Core/Nazor.Core/ComponentModel/EventCallback.cs)

### Members

#### `HasDelegate`

```csharp
bool HasDelegate { get; }
```

Gets whether a callback delegate has been assigned.

#### `InvokeAsync`

```csharp
Task InvokeAsync(object? arg)
```

Invokes the event callback through its receiver, using the supplied event argument when present.

| Parameter | Description |
| --- | --- |
| `arg` | The event argument passed to the handler. |

Returns: A task that completes when the operation has finished.


<a id="type-ihandleafterrender"></a>

## `IHandleAfterRender`

Namespace: `Nazor`

```csharp
public interface IHandleAfterRender
```

Implemented by components that need a notification after their output has been applied.

[Source](../../Sources/Core/Nazor.Core/ComponentModel/IComponent.cs)

### Members

#### `OnAfterRenderAsync`

```csharp
Task OnAfterRenderAsync()
```

Asynchronously notifies the component after its rendered output has been applied.

Returns: A task that completes when the operation has finished.


<a id="type-ihandleevent"></a>

## `IHandleEvent`

Namespace: `Nazor`

```csharp
public interface IHandleEvent
```

Implemented by components that decide how event callbacks targeting them are executed.

[Source](../../Sources/Core/Nazor.Core/ComponentModel/IComponent.cs)

### Members

#### `HandleEventAsync`

```csharp
Task HandleEventAsync(EventCallbackWorkItem item, object? arg)
```

Executes an event callback with the supplied event argument according to the component's event policy.

| Parameter | Description |
| --- | --- |
| `item` | The event callback work item to execute. |
| `arg` | The event argument passed to the handler. |

Returns: A task that completes when the operation has finished.


<a id="type-imagedata"></a>

## `ImageData`

Namespace: `Nazor`

```csharp
public sealed record ImageData(byte[] Bytes, float Scale)
```

The encoded bytes of an image and the display scale they were made for (2 for "logo@2x.png").

| Parameter | Description |
| --- | --- |
| `Bytes` | The bytes (byte[]). |
| `Scale` | The scale (float). |

[Source](../../Sources/Core/Nazor.Core/Media/ImageSource.cs)


<a id="type-imageerroreventargs"></a>

## `ImageErrorEventArgs`

Namespace: `Nazor.Input`

```csharp
public class ImageErrorEventArgs : EventArgs
```

An image could not be read or decoded.

[Source](../../Sources/Core/Nazor.Core/Input/EventArgs.cs)

### Members

#### `Message`

```csharp
public string Message { get; init;}
```

Gets or sets the image-loading error message.


<a id="type-imageloadeventargs"></a>

## `ImageLoadEventArgs`

Namespace: `Nazor.Input`

```csharp
public class ImageLoadEventArgs : EventArgs
```

An image was loaded and decoded; its natural size in logical units.

[Source](../../Sources/Core/Nazor.Core/Input/EventArgs.cs)

### Members

#### `Width`

```csharp
public double Width { get; init;}
```

Gets or sets the successfully loaded image's intrinsic width.

#### `Height`

```csharp
public double Height { get; init;}
```

Gets or sets the successfully loaded image's intrinsic height.


<a id="type-imagesource"></a>

## `ImageSource`

Namespace: `Nazor`

```csharp
public abstract class ImageSource : IEquatable<ImageSource>
```

Where an image comes from: a file, an embedded resource, a stream, bytes or a URI. A string converts to a source, so markup can write `Source="images/logo.png"`: "http://" and "https://" are URIs, "file://" and absolute paths files, other paths files relative to the application directory. Files and resources have variants by display scale: at 200% "logo@2x.png" is taken instead of "logo.png" when it exists (also @1.25x, @1.5x, @3x and @4x).

[Source](../../Sources/Core/Nazor.Core/Media/ImageSource.cs)

### Members

#### `Key`

```csharp
public abstract string Key { get; }
```

The identity of the image: sources with the same key give the same image (caches use it).

#### `HasVariants`

```csharp
public virtual bool HasVariants {get; }
```

Whether the source has variants by display scale (files and resources).

#### `LoadAsync`

```csharp
public abstract Task<ImageData> LoadAsync(float scale, CancellationToken cancellationToken = default)
```

Reads the encoded bytes of the best variant for `scale`; throws when the image cannot be read.

| Parameter | Description |
| --- | --- |
| `scale` | The number of physical pixels per logical unit. |
| `cancellationToken` | A token used to request cancellation. |

Returns: A task whose result is the value produced by the operation.

#### `FromFile`

```csharp
public static ImageSource FromFile(string path)
```

Creates an image source backed by a file path.

| Parameter | Description |
| --- | --- |
| `path` | The file path used by this operation. |

Returns: An image source pointing to the file.

#### `FromResource`

```csharp
public static ImageSource FromResource(Assembly assembly, string name)
```

An embedded resource by its manifest name, for example "MyApp.Images.logo.png" or a NazorAsset path.

| Parameter | Description |
| --- | --- |
| `assembly` | The assembly (Assembly). |
| `name` | The name used to identify the parameter, property, resource, or registration. |

Returns: The resulting ImageSource value.

Throws `ArgumentNullException`: assembly is null.

#### `FromStream`

```csharp
public static ImageSource FromStream(Func<CancellationToken, Task<Stream>> open, string key)
```

A stream opened for every load; `key` names the image for caches.

| Parameter | Description |
| --- | --- |
| `open` | The open (Func<CancellationToken, Task<Stream>>). |
| `key` | The key (string). |

Returns: The resulting ImageSource value.

Throws `ArgumentNullException`: open is null.

#### `FromBytes`

```csharp
public static ImageSource FromBytes(byte[] bytes)
```

Bytes in memory; every call is a different image for caches.

| Parameter | Description |
| --- | --- |
| `bytes` | The bytes (byte[]). |

Returns: The resulting ImageSource value.

Throws `ArgumentNullException`: bytes is null.

#### `FromUri`

```csharp
public static ImageSource FromUri(Uri uri)
```

Creates an image source backed by the supplied URI.

| Parameter | Description |
| --- | --- |
| `uri` | The target address or image URI. |

Returns: An image source pointing to the URI.

Throws `ArgumentNullException`: uri is null.

#### `Parse`

```csharp
public static ImageSource Parse(string value)
```

Parses from the current token position.

| Parameter | Description |
| --- | --- |
| `value` | The value to use. |

Returns: The resulting ImageSource value.

#### `implicit operator ImageSource`

```csharp
public static implicit operator ImageSource(string value)
```

Converts a string value to ImageSource.

| Parameter | Description |
| --- | --- |
| `value` | The value to use. |

Returns: The resulting ImageSource value.

#### `Equals`

```csharp
public bool Equals(ImageSource? other)
```

Determines whether the supplied value is equal to this instance.

| Parameter | Description |
| --- | --- |
| `other` | The value to compare with this instance. |

Returns: true if the supplied value satisfies the comparison; otherwise, false.

#### `Equals`

```csharp
public override bool Equals(object? obj)
```

Determines whether the supplied value is equal to this instance.

| Parameter | Description |
| --- | --- |
| `obj` | The value to compare with this instance. |

Returns: true if the supplied value satisfies the comparison; otherwise, false.

#### `GetHashCode`

```csharp
public override int GetHashCode()
```

Returns a hash code consistent with equality for this value.

Returns: The hash code for this value.

#### `ToString`

```csharp
public override string ToString()
```

Returns a textual representation of this value.

Returns: The textual representation of this value.


<a id="type-injectattribute"></a>

## `InjectAttribute`

Namespace: `Nazor`

```csharp
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)] public sealed class InjectAttribute : Attribute
```

Marks a component property that is resolved from the renderer's service provider.

[Source](../../Sources/Core/Nazor.Core/ComponentModel/IComponent.cs)


<a id="type-keybindings"></a>

## `KeyBindings`

Namespace: `Nazor.Input`

```csharp
public sealed class KeyBindings
```

Window shortcuts, evaluated before the focused control. Dispose registrations with their owner.

[Source](../../Sources/Core/Nazor.Core/Input/KeyBindings.cs)

### Members

#### `Register`

```csharp
public IDisposable Register(string key, KeyModifiers modifiers, EventCallback<KeyboardEventArgs> command, bool allowRepeat = false)
```

Registers a keyboard gesture for a command and returns a handle that removes the registration.

| Parameter | Description |
| --- | --- |
| `key` | The normalized name of the key. |
| `modifiers` | The modifier keys pressed when the input occurred. |
| `command` | The callback invoked when the registered key and modifiers match. |
| `allowRepeat` | Whether repeated key-down events are allowed to invoke this command. |

Returns: A disposable registration handle.

#### `TryGetCommand`

```csharp
public bool TryGetCommand(KeyboardEventArgs args, out EventCallback<KeyboardEventArgs> command)
```

Matches a keyboard event against registered gestures, giving the most recent registration precedence. A matching binding consumes disallowed repeats without returning a callback delegate.

| Parameter | Description |
| --- | --- |
| `args` | The keyboard event whose key, modifiers, and repeat state are matched against registered bindings. |
| `command` | When this method returns, contains the command produced by the operation. |

Returns: true if the key and modifiers match a registered binding, including a consumed repeat; otherwise, false.


<a id="type-keyboardeventargs"></a>

## `KeyboardEventArgs`

Namespace: `Nazor.Input`

```csharp
public class KeyboardEventArgs : EventArgs
```

Arguments of keyboard events. `Key` uses names such as "Enter", "Tab", "a".

[Source](../../Sources/Core/Nazor.Core/Input/EventArgs.cs#L62)


### `Handled`

```csharp
public bool Handled { get; set; }
```

Gets or sets whether the component consumed this key, suppressing remaining handlers and built-in keyboard behavior. Set before awaiting asynchronous work.

[Source](../../Sources/Core/Nazor.Core/Input/EventArgs.cs#L62)


### `Key`

```csharp
public string Key { get; init; }
```

Gets or sets the normalized name of the keyboard key.

[Source](../../Sources/Core/Nazor.Core/Input/EventArgs.cs#L67)


### `Modifiers`

```csharp
public KeyModifiers Modifiers { get; init; }
```

Gets or sets the modifier keys pressed when the input event occurred.

[Source](../../Sources/Core/Nazor.Core/Input/EventArgs.cs#L67)


### `Repeat`

```csharp
public bool Repeat { get; init; }
```

Gets or sets whether this keyboard event is an auto-repeat.

[Source](../../Sources/Core/Nazor.Core/Input/EventArgs.cs#L73)


<a id="type-keymodifiers"></a>

## `KeyModifiers`

Namespace: `Nazor.Input`

```csharp
[Flags] public enum KeyModifiers
```

Identifies the modifier keys held during input; values can be combined.

[Source](../../Sources/Core/Nazor.Core/Input/EventArgs.cs)

### Members

#### `None`

```csharp
None = 0
```

Indicates that no modifier keys are pressed.

#### `Shift`

```csharp
Shift = 1
```

Indicates that Shift is pressed.

#### `Control`

```csharp
Control = 2
```

Indicates that Control is pressed.

#### `Alt`

```csharp
Alt = 4
```

Indicates that Alt is pressed.

#### `Meta`

```csharp
Meta = 8
```

Indicates that the platform Meta or Windows key is pressed.


<a id="type-linuxbackdrop"></a>

## `LinuxBackdrop`

Namespace: `Nazor`

```csharp
public enum LinuxBackdrop
```

Linux materials; availability depends on the compositor's capabilities.

[Source](../../Sources/Core/Nazor.Core/Windows/Backdrop.cs)

### Members

#### `Blur`

```csharp
Blur
```

Requests the compositor's blur-behind effect.


<a id="type-locationchangedeventargs"></a>

## `LocationChangedEventArgs`

Namespace: `Nazor`

`Kind` identifies the operation that selected the entry; the optional constructor argument defaults to `NavigationKind.New`.

```csharp
public sealed class LocationChangedEventArgs(NavigationEntry entry, NavigationKind kind = NavigationKind.New) : EventArgs
```

The location changed: `Entry` is the current one.

| Parameter | Description |
| --- | --- |
| `entry` | The navigation entry that became current. |
| `kind` | The operation that selected the entry; defaults to `NavigationKind.New`. |

[Source](../../Sources/Core/Nazor.Core/Navigation/NavigationManager.cs)

### Members

#### `Entry`

```csharp
public NavigationEntry Entry { get; }
```

Gets the navigation entry that became current.

#### `Uri`

```csharp
public string? Uri {get; }
```

Gets the current address, or null for navigation directly to a page type.

#### `Kind`

```csharp
public NavigationKind Kind { get; }
```

Gets the operation that selected the current entry, for directional page transitions.


<a id="type-locationchangingcontext"></a>

## `LocationChangingContext`

Namespace: `Nazor`

```csharp
public sealed class LocationChangingContext(NavigationEntry target, NavigationEntry current)
```

A navigation about to happen, which a handler can stop (unsaved changes).

| Parameter | Description |
| --- | --- |
| `target` | The component, element, or navigation entry targeted by the operation. |
| `current` | The current navigation entry before the change. |

[Source](../../Sources/Core/Nazor.Core/Navigation/NavigationManager.cs)

### Members

#### `Target`

```csharp
public NavigationEntry Target { get; }
```

Gets the proposed destination of the pending navigation.

#### `Current`

```csharp
public NavigationEntry Current { get; }
```

Gets the navigation entry being left.

#### `IsNavigationPrevented`

```csharp
public bool IsNavigationPrevented { get; private set;}
```

Gets whether a location-changing handler canceled the pending navigation.

#### `PreventNavigation`

```csharp
public void PreventNavigation()
```

Cancels the navigation currently being evaluated by location-changing handlers.


<a id="type-memoryclipboard"></a>

## `MemoryClipboard`

Namespace: `Nazor.Input`

```csharp
public sealed class MemoryClipboard : IClipboard
```

A clipboard local to the process, for headless hosts and tests.

[Source](../../Sources/Core/Nazor.Core/Input/Clipboard.cs)

### Members

#### `GetText`

```csharp
public string? GetText()
```

Gets the text stored in this in-memory clipboard.

Returns: The current clipboard text, or null when no text is stored.

#### `SetText`

```csharp
public bool SetText(string value)
```

Replaces the text stored in this in-memory clipboard.

| Parameter | Description |
| --- | --- |
| `value` | The value to use. |

Returns: true when the operation succeeds or the condition is satisfied; otherwise, false.


<a id="type-navigationentry"></a>

## `NavigationEntry`

Namespace: `Nazor`

```csharp
public sealed record NavigationEntry(string? Uri, Type? Page = null, IReadOnlyDictionary<string, object?>? Parameters = null)
```

A place in the history of a window: an address (`/orders/42`) or a page type with its parameters, for pages reached without an address.

| Parameter | Description |
| --- | --- |
| `Uri` | The target address or image URI. |
| `Page` | The page (Type?). |
| `Parameters` | The parameters (IReadOnlyDictionary<string, object?>?). |

[Source](../../Sources/Core/Nazor.Core/Navigation/NavigationManager.cs)


<a id="type-navigationmanager"></a>

## `NavigationManager`

Namespace: `Nazor`

`RegisterLocationChangingHandler(handler, owner)` adds a page-scoped guard. It runs only while `owner` is the exact current history entry by reference identity. Capture `Navigation.Current` when creating the page; outgoing retained pages cannot guard subsequent navigation, and returning to the same history entry re-enables the guard. Dispose the returned idempotent registration with its page. Null callback/owner throws `ArgumentNullException`. The existing one-argument overload remains global.

```csharp
public sealed class NavigationManager
```

The navigation of a window (one per DI scope, as the window): the current place, a history with back and forward, and the handlers that can stop a navigation. A `Frame` shows the page of the current place. Methods run on the UI thread.

[Source](../../Sources/Core/Nazor.Core/Navigation/NavigationManager.cs)

### Members

#### `Current`

```csharp
public NavigationEntry Current {get; }
```

The current place; at the start the address `/`.

#### `Uri`

```csharp
public string? Uri {get; }
```

The address of the current place, or null for a page reached by type.

#### `CanGoBack`

```csharp
public bool CanGoBack {get; }
```

Gets whether a previous navigation history entry is available.

#### `CanGoForward`

```csharp
public bool CanGoForward {get; }
```

Gets whether a subsequent navigation history entry is available.

#### `LocationChanged`

```csharp
public event EventHandler<LocationChangedEventArgs>? LocationChanged;
```

Raised after the current place changed.

#### `UnhandledException`

```csharp
public event Action<Exception>? UnhandledException;
```

Exceptions of the handlers of a navigation started without awaiting it.

#### `NavigateTo`

```csharp
public void NavigateTo(string uri, bool replace = false)
```

Goes to `uri` (absolute from the window, `/orders/42`); `replace` replaces the current place.

| Parameter | Description |
| --- | --- |
| `uri` | The target address or image URI. |
| `replace` | Whether to replace the current history entry instead of appending a new one. |

#### `NavigateTo`

```csharp
public void NavigateTo<TPage>(IReadOnlyDictionary<string, object?>? parameters = null, bool replace = false) where TPage : IComponent
```

Goes to a page by type, with its parameters, without an address.

| Type parameter | Description |
| --- | --- |
| `TPage` | The page type. |

| Parameter | Description |
| --- | --- |
| `parameters` | The parameter names and values to supply to the component. |
| `replace` | Whether to replace the current history entry instead of appending a new one. |

#### `GoBack`

```csharp
public void GoBack()
```

Navigates to the previous history entry and reports asynchronous errors through UnhandledException.

#### `GoForward`

```csharp
public void GoForward()
```

Navigates to the next history entry and reports asynchronous errors through UnhandledException.

#### `NavigateToAsync`

```csharp
public Task<bool> NavigateToAsync(string uri, bool replace = false)
```

Navigates to the supplied target after awaiting all location-changing handlers.

| Parameter | Description |
| --- | --- |
| `uri` | The target address or image URI. |
| `replace` | Whether to replace the current history entry instead of appending a new one. |

Returns: A task returning true if navigation completed, or false if a handler prevented it.

#### `NavigateToAsync`

```csharp
public async Task<bool> NavigateToAsync(NavigationEntry target, bool replace = false)
```

Navigates to the supplied target after awaiting all location-changing handlers.

| Parameter | Description |
| --- | --- |
| `target` | The component, element, or navigation entry targeted by the operation. |
| `replace` | Whether to replace the current history entry instead of appending a new one. |

Returns: A task returning true if navigation completed, or false if a handler prevented it.

Throws `ArgumentNullException`: target is null.

#### `GoBackAsync`

```csharp
public Task<bool> GoBackAsync()
```

Navigates to the previous history entry if one exists and no handler prevents the change.

Returns: A task returning true if the previous entry became current, or false if unavailable or prevented.

#### `GoForwardAsync`

```csharp
public Task<bool> GoForwardAsync()
```

Navigates to the next history entry if one exists and no handler prevents the change.

Returns: A task returning true if the next entry became current, or false if unavailable or prevented.

#### `RegisterLocationChangingHandler`

```csharp
public IDisposable RegisterLocationChangingHandler(Func<LocationChangingContext, ValueTask> handler)
```

Adds a handler that runs before every navigation and can stop it with `LocationChangingContext.PreventNavigation`, also after awaiting (a confirmation); dispose the result to remove it (when the page goes).

| Parameter | Description |
| --- | --- |
| `handler` | The callback registered for the notification or command. |

Returns: The resulting IDisposable value.

Throws `ArgumentNullException`: handler is null.

#### `RegisterLocationChangingHandler`

```csharp
public IDisposable RegisterLocationChangingHandler(Func<LocationChangingContext, ValueTask> handler, NavigationEntry owner)
```

Adds an owner-scoped guard using the exact current history entry identity. Dispose the returned registration with the page; disposal is idempotent.

| Parameter | Description |
| --- | --- |
| `handler` | The callback that can prevent navigation while its owner is current. |
| `owner` | The captured history entry identifying the owning page. |

Returns: The registration to dispose when the page is removed.

Throws `ArgumentNullException`: handler or owner is null.


<a id="type-node"></a>

## `Node`

Namespace: `Nazor.Rendering`

```csharp
public abstract class Node
```

A node of the retained tree produced by the renderer. Logical nodes mirror the render frames (components and regions included); the physical tree used by styling, layout, drawing and accessibility contains only `ElementNode` and `TextNode`. Matched frames keep their node across renders, so node identity is element identity.

[Source](../../Sources/Core/Nazor.Core/Rendering/Nodes.cs)

### Members

#### `Parent`

```csharp
public Node? Parent { get; internal set;}
```

The logical parent: an element, a component, a region or null for a root component.

#### `ParentElement`

```csharp
public ElementNode? ParentElement { get ; }
```

The nearest element ancestor, i.e. the physical parent.

#### `Root`

```csharp
public RootComponent? Root { get ; }
```

The root component that contains this node.

#### `IsRemoved`

```csharp
public bool IsRemoved { get; internal set;}
```

Gets whether the node has been removed from the retained tree.

#### `GetFeature`

```csharp
public T? GetFeature<T>() where T : class
```

Per-node data owned by other subsystems (style, layout, visuals, semantics).

| Type parameter | Description |
| --- | --- |
| `T` | The type of the value. |

Returns: The resulting T? value.

#### `SetFeature`

```csharp
public void SetFeature<T>(T? value) where T : class
```

Stores or clears subsystem-owned data of the specified feature type on this node.

| Type parameter | Description |
| --- | --- |
| `T` | The type of the value. |

| Parameter | Description |
| --- | --- |
| `value` | The value to use. |


<a id="type-parameterattribute"></a>

## `ParameterAttribute`

Namespace: `Nazor`

```csharp
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)] public sealed class ParameterAttribute : Attribute
```

Marks a component property that can be set by the parent through markup.

[Source](../../Sources/Core/Nazor.Core/ComponentModel/IComponent.cs)

### Members

#### `CaptureUnmatchedValues`

```csharp
public bool CaptureUnmatchedValues { get; set;}
```

Gets or sets whether this parameter collects attributes that do not match another component parameter.


<a id="type-parametervalue"></a>

## `ParameterValue`

Namespace: `Nazor`

```csharp
public readonly record struct ParameterValue(string Name, object? Value)
```

A parameter name and value supplied to a component.

| Parameter | Description |
| --- | --- |
| `Name` | The name used to identify the parameter, property, resource, or registration. |
| `Value` | The numeric value or payload represented by this record. |

[Source](../../Sources/Core/Nazor.Core/ComponentModel/ParameterView.cs)


<a id="type-parameterview"></a>

## `ParameterView`

Namespace: `Nazor`

```csharp
public readonly struct ParameterView : IEnumerable<ParameterValue>
```

The parameters supplied to a component. Names are matched to `ParameterAttribute` properties case-insensitively, so the markup attribute `@onclick` sets `OnClick`.

[Source](../../Sources/Core/Nazor.Core/ComponentModel/ParameterView.cs)

### Members

#### `Empty`

```csharp
public static ParameterView Empty {get; }
```

Gets a parameter view containing no values.

#### `Count`

```csharp
public int Count {get; }
```

Gets the number of parameters in this view.

#### `FromDictionary`

```csharp
public static ParameterView FromDictionary(IReadOnlyDictionary<string, object?> parameters)
```

Creates a parameter view over the supplied dictionary of parameter names and values.

| Parameter | Description |
| --- | --- |
| `parameters` | The parameter names and values to supply to the component. |

Returns: A parameter view containing a snapshot of the supplied names and values.

#### `TryGetValue`

```csharp
public bool TryGetValue<TValue>(string name, out TValue value)
```

Looks up a named parameter and returns whether it was present.

| Type parameter | Description |
| --- | --- |
| `TValue` | The type of the component, callback, or cascading value. |

| Parameter | Description |
| --- | --- |
| `name` | The name used to identify the parameter, property, resource, or registration. |
| `value` | When this method returns, contains the value produced by the operation. |

Returns: true if the named parameter exists; otherwise, false.

#### `GetValueOrDefault`

```csharp
public TValue? GetValueOrDefault<TValue>(string name)
```

Gets the named parameter converted to the requested type, or the type's default value if absent.

| Type parameter | Description |
| --- | --- |
| `TValue` | The type of the component, callback, or cascading value. |

| Parameter | Description |
| --- | --- |
| `name` | The name used to identify the parameter, property, resource, or registration. |

Returns: The parameter value cast to the requested type, or the type's default value if absent.

#### `ToDictionary`

```csharp
public IReadOnlyDictionary<string, object?> ToDictionary()
```

Copies the parameter names and values to a dictionary.

Returns: A dictionary of the parameter names and values, using case-insensitive name matching.

#### `SetParameterProperties`

```csharp
public void SetParameterProperties(object target)
```

Assigns each parameter to the matching `ParameterAttribute` property of `target`.

| Parameter | Description |
| --- | --- |
| `target` | The component, element, or navigation entry targeted by the operation. |

Throws `ArgumentNullException`: target is null.

#### `GetEnumerator`

```csharp
public IEnumerator<ParameterValue> GetEnumerator()
```

Enumerates the parameter names and values in this view.

Returns: An enumerator over the collection's ordered values.


<a id="type-platformkind"></a>

## `PlatformKind`

Namespace: `Nazor`

```csharp
public enum PlatformKind
```

Stable platform identity for backend-provided style queries. Unknown backends return null.

[Source](../../Sources/Core/Nazor.Core/Windows/PlatformKind.cs)

### Members

#### `Windows`

```csharp
Windows
```

Identifies the Microsoft Windows platform.

#### `Linux`

```csharp
Linux
```

Identifies the Linux platform.

#### `MacOS`

```csharp
MacOS
```

Identifies the Apple macOS platform.

#### `Android`

```csharp
Android
```

Identifies the Android platform.

#### `IOS`

```csharp
IOS
```

Identifies the Apple iOS platform.


<a id="type-pointerbutton"></a>

## `PointerButton`

Namespace: `Nazor.Input`

```csharp
public enum PointerButton
```

Identifies the logical pointer button associated with an event.

[Source](../../Sources/Core/Nazor.Core/Input/EventArgs.cs)

### Members

#### `None`

```csharp
None
```

Indicates that no pointer button is associated with the event.

#### `Primary`

```csharp
Primary
```

Identifies the platform's primary activation button.

#### `Secondary`

```csharp
Secondary
```

Identifies the platform's secondary or context-menu button.

#### `Middle`

```csharp
Middle
```

Identifies the middle pointer button.


<a id="type-pointereventargs"></a>

## `PointerEventArgs`

Namespace: `Nazor.Input`

```csharp
public class PointerEventArgs : EventArgs
```

Arguments of pointer events. Positions are logical units relative to the window.

[Source](../../Sources/Core/Nazor.Core/Input/EventArgs.cs)

### Members

#### `X`

```csharp
public double X { get; init;}
```

Gets or sets the horizontal pointer position in logical client coordinates.

#### `Y`

```csharp
public double Y { get; init;}
```

Gets or sets the vertical pointer position in logical client coordinates.

#### `Button`

```csharp
public PointerButton Button { get; init;}
```

Gets or sets the pointer button associated with this event.

#### `Modifiers`

```csharp
public KeyModifiers Modifiers { get; init;}
```

Gets or sets the modifier keys pressed when the input event occurred.

#### `IsSynthetic`

```csharp
public bool IsSynthetic { get; init;}
```

True when the event was produced by the keyboard or an accessibility action.


<a id="type-renderbatch"></a>

## `RenderBatch`

Namespace: `Nazor.Rendering`

```csharp
public sealed class RenderBatch
```

The retained-tree changes applied by one batch of renders. Subsystems (styling, layout, drawing, accessibility) consume it after the tree is consistent again. Nodes both created and removed inside the same batch are not reported.

[Source](../../Sources/Core/Nazor.Core/Rendering/RenderBatch.cs)

### Members

#### `AddedElements`

```csharp
public IReadOnlyCollection<ElementNode> AddedElements { get; }
```

Gets the elements inserted by this render batch.

#### `RemovedNodes`

```csharp
public IReadOnlyCollection<Node> RemovedNodes { get; }
```

Gets physical and component nodes removed by this render batch, including nonvisual components. Nodes created and removed within the same batch are excluded.

#### `UpdatedElements`

```csharp
public IReadOnlyCollection<ElementNode> UpdatedElements { get; }
```

Existing elements whose attributes changed.

#### `UpdatedTexts`

```csharp
public IReadOnlyCollection<TextNode> UpdatedTexts { get; }
```

Gets the text nodes whose content changed in this batch.

#### `ChildrenChanged`

```csharp
public IReadOnlyCollection<ElementNode> ChildrenChanged { get; }
```

Elements whose physical children were inserted, removed or moved.

#### `RootNodesChanged`

```csharp
public bool RootNodesChanged { get; }
```

Gets whether the set or order of root physical nodes changed in this render batch.

#### `RenderedComponentCount`

```csharp
public int RenderedComponentCount { get; }
```

Gets the number of components rendered while producing this batch.

#### `IsEmpty`

```csharp
public bool IsEmpty {get; }
```

Gets whether the batch contains no reported retained-tree changes, including component removals.


<a id="type-renderer"></a>

## `Renderer`

Namespace: `Nazor.Rendering`

```csharp
public class Renderer : IDisposable
```

Owns component instances, runs their renders in batches and keeps the retained node tree in sync with the render output. Matching follows Blazor's rules: siblings correspond when they have the same sequence number and kind, or the same `@key`; matched nodes and component instances are preserved, the others are created or removed (and disposed). Everything runs on `Dispatcher`.

[Source](../../Sources/Core/Nazor.Core/Rendering/Renderer.cs)

### Members

#### `Renderer`

```csharp
public Renderer(Dispatcher dispatcher, IServiceProvider? services = null, IWindow? window = null)
```

Initializes a new instance of Renderer.

| Parameter | Description |
| --- | --- |
| `dispatcher` | The dispatcher that owns the UI thread. |
| `services` | The dependency injection service collection or provider used by the application. |

#### `Dispatcher`

```csharp
public Dispatcher Dispatcher { get; }
```

Gets the UI dispatcher associated with this renderer or window.

#### `RootComponents`

```csharp
public IReadOnlyList<RootComponent> RootComponents {get; }
```

Gets the root components attached to this renderer.

#### `ComponentCount`

```csharp
public int ComponentCount {get; }
```

Number of live component instances; useful to verify that removal releases components.

#### `BatchCompleted`

```csharp
public event EventHandler<RenderBatch>? BatchCompleted;
```

Raised on the dispatcher after each batch, once the tree is consistent.

#### `UnhandledException`

```csharp
public event EventHandler<Exception>? UnhandledException;
```

Receives exceptions from rendering, lifecycle methods, event handlers and disposal. Without a subscriber the exception is rethrown to the caller of the operation (or to the dispatcher for asynchronous work that completes later).

#### `AddRootComponent`

```csharp
public RootComponent AddRootComponent<TComponent>(ParameterView parameters = default) where TComponent : IComponent
```

Instantiates and attaches a root component, then supplies its initial parameters.

| Type parameter | Description |
| --- | --- |
| `TComponent` | The component type to instantiate or render. |

| Parameter | Description |
| --- | --- |
| `parameters` | The parameter names and values to supply to the component. |

Returns: The root component handle attached to the renderer.

#### `AddRootComponent`

```csharp
public RootComponent AddRootComponent(Type componentType, ParameterView parameters = default)
```

Instantiates and attaches a root component, then supplies its initial parameters.

| Parameter | Description |
| --- | --- |
| `componentType` | The component type whose scoped styles or render output are being requested. |
| `parameters` | The parameter names and values to supply to the component. |

Returns: The root component handle attached to the renderer.

#### `RenderOnHotReload`

```csharp
public void RenderOnHotReload()
```

Re-renders every root component after a .NET Hot Reload delta: each root receives the parameters it was added with, and the render cascades to every child whether or not its parameters changed. Lifecycle methods (`OnParametersSet` and the async variant) run again, as in Blazor. Exceptions reach `UnhandledException` as for any render.

#### `RemoveRootComponent`

```csharp
public void RemoveRootComponent(RootComponent root)
```

Removes a root component and disposes its component subtree.

| Parameter | Description |
| --- | --- |
| `root` | The root (RootComponent). |

#### `DispatchEventAsync`

```csharp
public Task DispatchEventAsync(ElementNode element, string eventName, object? eventArgs)
```

Dispatches the event callback through its receiver and observes asynchronous completion. Disabled elements still receive target-local noninteractive `onresize` notifications, alongside blur/focus-out/pointer-leave/lost-capture cleanup; activation remains suppressed.

| Parameter | Description |
| --- | --- |
| `element` | The element to inspect or update. |
| `eventName` | The normalized event attribute name. |
| `eventArgs` | The event argument passed to the handler. |

Returns: A task that completes when the operation has finished.

#### `DispatchEventAsync`

```csharp
public Task DispatchEventAsync(ElementNode element, string eventName, object? eventArgs, out bool preventDefault)
```

Dispatches the event callback through its receiver and observes asynchronous completion.

| Parameter | Description |
| --- | --- |
| `element` | The element to inspect or update. |
| `eventName` | The normalized event attribute name. |
| `eventArgs` | The event argument passed to the handler. |
| `preventDefault` | When this method returns, contains the prevent default produced by the operation. |

Returns: A task that completes when the operation has finished.

#### `DispatchEventAsync`

```csharp
public Task DispatchEventAsync(IEventCallback callback, object? eventArgs)
```

Dispatches the event callback through its receiver and observes asynchronous completion.

| Parameter | Description |
| --- | --- |
| `callback` | The delegate to invoke when the callback is dispatched. |
| `eventArgs` | The event argument passed to the handler. |

Returns: A task that completes when the operation has finished.

#### `Dispose`

```csharp
public void Dispose()
```

Releases the resources and event subscriptions owned by this instance.


<a id="type-renderfragment"></a>

## `RenderFragment`

Namespace: `Nazor`

```csharp
public delegate void RenderFragment(RenderTreeBuilder builder);
```

Content rendered into a `RenderTreeBuilder`.

| Parameter | Description |
| --- | --- |
| `builder` | The builder that receives the render frames. |

[Source](../../Sources/Core/Nazor.Core/ComponentModel/IComponent.cs)


<a id="type-renderfragmenttvalue"></a>

## `RenderFragment<TValue>`

Namespace: `Nazor`

```csharp
public delegate RenderFragment RenderFragment<TValue>(TValue value);
```

A template that produces content for a value.

| Type parameter | Description |
| --- | --- |
| `TValue` | The type of the component, callback, or cascading value. |

| Parameter | Description |
| --- | --- |
| `value` | The value to use. |

Returns: The resulting RenderFragment value.

[Source](../../Sources/Core/Nazor.Core/ComponentModel/IComponent.cs)


<a id="type-renderhandle"></a>

## `RenderHandle`

Namespace: `Nazor`

```csharp
public readonly struct RenderHandle
```

Allows a component to schedule its rendering on the renderer that owns it.

[Source](../../Sources/Core/Nazor.Core/ComponentModel/RenderHandle.cs)

### Members

#### `IsInitialized`

```csharp
public bool IsInitialized {get; }
```

Gets whether the render handle has been attached to a renderer.

#### `Dispatcher`

```csharp
public Dispatcher Dispatcher {get; }
```

Gets the UI dispatcher associated with this renderer or window.

#### `Render`

```csharp
public void Render(RenderFragment renderFragment)
```

Queues a render of the component; requests for a disposed component are ignored.

| Parameter | Description |
| --- | --- |
| `renderFragment` | The fragment that writes the component's render output. |


<a id="type-rendertreebuilder"></a>

## `RenderTreeBuilder`

Namespace: `Nazor.Rendering`

```csharp
public sealed class RenderTreeBuilder
```

Collects the frames produced by one render of a component. The method set matches what the Razor component compiler emits, so generated code targets it unchanged after namespace adaptation.

[Source](../../Sources/Core/Nazor.Core/Rendering/RenderTreeBuilder.cs)

### Members

#### `FrameCount`

```csharp
public int FrameCount {get; }
```

Gets the number of render frames stored in the builder.

#### `GetFrames`

```csharp
public ReadOnlySpan<RenderTreeFrame> GetFrames()
```

Gets a read-only span over the frames currently stored in the builder.

Returns: A read-only span over the current render frames.

#### `Clear`

```csharp
public void Clear()
```

Removes all frames and resets the builder for reuse.

#### `OpenElement`

```csharp
public void OpenElement(int sequence, string elementName)
```

Starts an element frame with the supplied element name.

| Parameter | Description |
| --- | --- |
| `sequence` | The source-order sequence number used to match frames across renders. |
| `elementName` | The element name exposed to NSS type selectors. |

#### `CloseElement`

```csharp
public void CloseElement()
```

Completes the currently open element frame.

#### `OpenComponent`

```csharp
public void OpenComponent<TComponent>(int sequence) where TComponent : notnull, IComponent
```

Starts a component frame, optionally recording the declaring markup type and instance for scoped selectors.

| Type parameter | Description |
| --- | --- |
| `TComponent` | The component type to instantiate or render. |

| Parameter | Description |
| --- | --- |
| `sequence` | The source-order sequence number used to match frames across renders. |

#### `OpenComponent`

```csharp
public void OpenComponent<TComponent>(int sequence, Type? declaringScope) where TComponent : notnull, IComponent
```

Starts a component frame, optionally recording the declaring markup type and instance for scoped selectors.

| Type parameter | Description |
| --- | --- |
| `TComponent` | The component type to instantiate or render. |

| Parameter | Description |
| --- | --- |
| `sequence` | The source-order sequence number used to match frames across renders. |
| `declaringScope` | The type whose markup declares the component, used for scoped selectors. |

#### `OpenComponent`

```csharp
public void OpenComponent<TComponent>(int sequence, Type? declaringScope, IComponent? declaringComponent) where TComponent : notnull, IComponent
```

Starts a component frame, optionally recording the declaring markup type and instance for scoped selectors.

| Type parameter | Description |
| --- | --- |
| `TComponent` | The component type to instantiate or render. |

| Parameter | Description |
| --- | --- |
| `sequence` | The source-order sequence number used to match frames across renders. |
| `declaringScope` | The type whose markup declares the component, used for scoped selectors. |
| `declaringComponent` | The component instance whose markup declares the content. |

#### `OpenComponent`

```csharp
public void OpenComponent(int sequence, Type componentType)
```

Starts a component frame, optionally recording the declaring markup type and instance for scoped selectors.

| Parameter | Description |
| --- | --- |
| `sequence` | The source-order sequence number used to match frames across renders. |
| `componentType` | The component type whose scoped styles or render output are being requested. |

#### `OpenComponent`

```csharp
public void OpenComponent(int sequence, Type componentType, Type? declaringScope)
```

Starts a component frame, optionally recording the declaring markup type and instance for scoped selectors.

| Parameter | Description |
| --- | --- |
| `sequence` | The source-order sequence number used to match frames across renders. |
| `componentType` | The component type whose scoped styles or render output are being requested. |
| `declaringScope` | The type whose markup declares the component, used for scoped selectors. |

#### `OpenComponent`

```csharp
public void OpenComponent(int sequence, Type componentType, Type? declaringScope, IComponent? declaringComponent)
```

Starts a component frame, optionally recording the declaring markup type and instance for scoped selectors.

| Parameter | Description |
| --- | --- |
| `sequence` | The source-order sequence number used to match frames across renders. |
| `componentType` | The component type whose scoped styles or render output are being requested. |
| `declaringScope` | The type whose markup declares the component, used for scoped selectors. |
| `declaringComponent` | The component instance whose markup declares the content. |

Throws `ArgumentNullException`: componentType is null.

#### `CloseComponent`

```csharp
public void CloseComponent()
```

Completes the currently open component frame.

#### `OpenRegion`

```csharp
public void OpenRegion(int sequence)
```

Starts a region with an independent sequence-number scope.

| Parameter | Description |
| --- | --- |
| `sequence` | The source-order sequence number used to match frames across renders. |

#### `CloseRegion`

```csharp
public void CloseRegion()
```

Completes the currently open region frame.

#### `AddContent`

```csharp
public void AddContent(int sequence, string? textContent)
```

Adds the child render frames used by RenderTreeBuilder.

| Parameter | Description |
| --- | --- |
| `sequence` | The source-order sequence number used to match frames across renders. |
| `textContent` | The content to append as a text frame. |

#### `AddContent`

```csharp
public void AddContent(int sequence, object? textContent)
```

Adds the child render frames used by RenderTreeBuilder.

| Parameter | Description |
| --- | --- |
| `sequence` | The source-order sequence number used to match frames across renders. |
| `textContent` | The content to append as a text frame. |

#### `AddContent`

```csharp
public void AddContent(int sequence, RenderFragment? fragment)
```

Renders a fragment inside a region, so its sequence numbers do not collide with the caller's.

| Parameter | Description |
| --- | --- |
| `sequence` | The source-order sequence number used to match frames across renders. |
| `fragment` | The render fragment to invoke. |

#### `AddContent`

```csharp
public void AddContent<TValue>(int sequence, RenderFragment<TValue>? fragment, TValue value)
```

Adds the child render frames used by RenderTreeBuilder.

| Type parameter | Description |
| --- | --- |
| `TValue` | The type of the component, callback, or cascading value. |

| Parameter | Description |
| --- | --- |
| `sequence` | The source-order sequence number used to match frames across renders. |
| `fragment` | The render fragment to invoke. |
| `value` | The value to use. |

#### `AddMarkupContent`

```csharp
public void AddMarkupContent(int sequence, string? markupContent)
```

Razor emits text as markup when it contains characters it would HTML-encode (non-ASCII letters, entities). The compiler rejects elements, so markup here is text: it is decoded into a text frame. Whitespace between controls produces no frame.

| Parameter | Description |
| --- | --- |
| `sequence` | The source-order sequence number used to match frames across renders. |
| `markupContent` | The markup content (string?). |

#### `AddAttribute`

```csharp
public void AddAttribute(int sequence, string name)
```

Adds an element attribute or component parameter to the currently open frame.

| Parameter | Description |
| --- | --- |
| `sequence` | The source-order sequence number used to match frames across renders. |
| `name` | The name used to identify the parameter, property, resource, or registration. |

#### `AddAttribute`

```csharp
public void AddAttribute(int sequence, string name, bool value)
```

Adds an element attribute or component parameter to the currently open frame.

| Parameter | Description |
| --- | --- |
| `sequence` | The source-order sequence number used to match frames across renders. |
| `name` | The name used to identify the parameter, property, resource, or registration. |
| `value` | The value to use. |

#### `AddAttribute`

```csharp
public void AddAttribute(int sequence, string name, string? value)
```

Adds an element attribute or component parameter to the currently open frame.

| Parameter | Description |
| --- | --- |
| `sequence` | The source-order sequence number used to match frames across renders. |
| `name` | The name used to identify the parameter, property, resource, or registration. |
| `value` | The value to use. |

#### `AddAttribute`

```csharp
public void AddAttribute(int sequence, string name, MulticastDelegate? value)
```

Adds an element attribute or component parameter to the currently open frame.

| Parameter | Description |
| --- | --- |
| `sequence` | The source-order sequence number used to match frames across renders. |
| `name` | The name used to identify the parameter, property, resource, or registration. |
| `value` | The value to use. |

#### `AddAttribute`

```csharp
public void AddAttribute(int sequence, string name, EventCallback value)
```

Adds an element attribute or component parameter to the currently open frame.

| Parameter | Description |
| --- | --- |
| `sequence` | The source-order sequence number used to match frames across renders. |
| `name` | The name used to identify the parameter, property, resource, or registration. |
| `value` | The value to use. |

#### `AddAttribute`

```csharp
public void AddAttribute<TArgument>(int sequence, string name, EventCallback<TArgument> value)
```

Adds an element attribute or component parameter to the currently open frame.

| Type parameter | Description |
| --- | --- |
| `TArgument` | The type of the event argument. |

| Parameter | Description |
| --- | --- |
| `sequence` | The source-order sequence number used to match frames across renders. |
| `name` | The name used to identify the parameter, property, resource, or registration. |
| `value` | The value to use. |

#### `AddAttribute`

```csharp
public void AddAttribute(int sequence, string name, object? value)
```

Adds an element attribute or component parameter to the currently open frame.

| Parameter | Description |
| --- | --- |
| `sequence` | The source-order sequence number used to match frames across renders. |
| `name` | The name used to identify the parameter, property, resource, or registration. |
| `value` | The value to use. |

#### `AddComponentParameter`

```csharp
public void AddComponentParameter(int sequence, string name, object? value)
```

Adds a named parameter to the currently open component frame.

| Parameter | Description |
| --- | --- |
| `sequence` | The source-order sequence number used to match frames across renders. |
| `name` | The name used to identify the parameter, property, resource, or registration. |
| `value` | The value to use. |

#### `AddMultipleAttributes`

```csharp
public void AddMultipleAttributes(int sequence, IEnumerable<KeyValuePair<string, object?>>? attributes)
```

Adds a sequence of attributes or component parameters to the currently open frame.

| Parameter | Description |
| --- | --- |
| `sequence` | The source-order sequence number used to match frames across renders. |
| `attributes` | The attributes or selector constraints to apply. |

#### `AddEventPreventDefaultAttribute`

```csharp
public void AddEventPreventDefaultAttribute(int sequence, string eventName, bool value)
```

Sets whether the event's default behavior should be prevented.

| Parameter | Description |
| --- | --- |
| `sequence` | The source-order sequence number used to match frames across renders. |
| `eventName` | The normalized event attribute name. |
| `value` | The value to use. |

#### `AddEventStopPropagationAttribute`

```csharp
public void AddEventStopPropagationAttribute(int sequence, string eventName, bool value)
```

Sets whether the event should stop propagating to ancestor elements.

| Parameter | Description |
| --- | --- |
| `sequence` | The source-order sequence number used to match frames across renders. |
| `eventName` | The normalized event attribute name. |
| `value` | The value to use. |

#### `AddElementReferenceCapture`

```csharp
public void AddElementReferenceCapture(int sequence, Action<ElementReference> capture)
```

Captures a reference to the rendered element through the supplied callback.

| Parameter | Description |
| --- | --- |
| `sequence` | The source-order sequence number used to match frames across renders. |
| `capture` | The callback that receives the rendered element reference. |

Throws `ArgumentNullException`: capture is null.

#### `SetKey`

```csharp
public void SetKey(object? value)
```

Sets the identity of the open element or component among its siblings.

| Parameter | Description |
| --- | --- |
| `value` | The value to use. |

#### `AddComponentReferenceCapture`

```csharp
public void AddComponentReferenceCapture(int sequence, Action<object> componentReferenceCaptureAction)
```

Captures the instantiated component through the supplied callback.

| Parameter | Description |
| --- | --- |
| `sequence` | The source-order sequence number used to match frames across renders. |
| `componentReferenceCaptureAction` | The callback that receives the instantiated component. |

Throws `ArgumentNullException`: componentReferenceCaptureAction is null.

#### `SetUpdatesAttributeName`

```csharp
public void SetUpdatesAttributeName(string updatesAttributeName)
```

Emitted by Razor for element bindings; Nazor controls bind through parameters instead.

| Parameter | Description |
| --- | --- |
| `updatesAttributeName` | The updates attribute name (string). |


#### `OpenElement`

```csharp
public void OpenElement(int sequence, string elementName, IComponent owner)
```

Starts an element explicitly owned by the declaration that renders at a host-managed destination. Sequence is source order; elementName is the retained element name and owner is the component receiving control references.


<a id="type-rendertreeframe"></a>

## `RenderTreeFrame`

Namespace: `Nazor.Rendering`

```csharp
public struct RenderTreeFrame
```

One entry of a component's render output. Elements, components and regions own the `SubtreeLength` - 1 frames that follow them: attributes first, then children. Sequence numbers come from source positions and identify frames across renders.

[Source](../../Sources/Core/Nazor.Core/Rendering/RenderTreeFrame.cs)

### Members

#### `ElementReferenceCaptureAction`

```csharp
public Action<ElementReference>? ElementReferenceCaptureAction { get; internal set;}
```

Gets the callback used to capture the rendered element reference.

#### `Sequence`

```csharp
public int Sequence { get; internal set;}
```

Gets the source-order sequence number used to match frames across renders.

#### `FrameType`

```csharp
public RenderTreeFrameType FrameType { get; internal set;}
```

Gets the kind of render frame represented by this value.

#### `SubtreeLength`

```csharp
public int SubtreeLength { get; internal set;}
```

For elements, components and regions: this frame plus all descendants.

#### `Name`

```csharp
public string? Name { get; internal set;}
```

Element name or attribute name.

#### `TextContent`

```csharp
public string? TextContent { get; internal set;}
```

Gets the text carried by a text frame.

#### `AttributeValue`

```csharp
public object? AttributeValue { get; internal set;}
```

Gets the value carried by an attribute frame.

#### `ComponentType`

```csharp
public Type? ComponentType { get; internal set;}
```

Gets the component type instantiated by a component frame.

#### `DeclaringScope`

```csharp
public Type? DeclaringScope { get; internal set;}
```

The component whose markup declared this component frame; it scopes styles.

#### `DeclaringComponent`

```csharp
public IComponent? DeclaringComponent { get; internal set;}
```

Gets the component instance whose markup declares the content.

#### `Key`

```csharp
public object? Key { get; internal set;}
```

Gets the key used to retain identity across render updates.

#### `ComponentReferenceCaptureAction`

```csharp
public Action<object>? ComponentReferenceCaptureAction { get; internal set;}
```

Gets the callback used to capture the instantiated component.

#### `ToString`

```csharp
public override readonly string ToString()
```

Returns a textual representation of this value.

Returns: The textual representation of this value.


<a id="type-rendertreeframetype"></a>

## `RenderTreeFrameType`

Namespace: `Nazor.Rendering`

```csharp
public enum RenderTreeFrameType : byte
```

Identifies the kind of data stored in a render-tree frame.

[Source](../../Sources/Core/Nazor.Core/Rendering/RenderTreeFrame.cs)

### Members

#### `None`

```csharp
None
```

An uninitialized or empty frame.

#### `Element`

```csharp
Element
```

The opening frame of a retained element.

#### `Text`

```csharp
Text
```

A frame containing text content.

#### `Attribute`

```csharp
Attribute
```

An element attribute or component parameter frame.

#### `Component`

```csharp
Component
```

The opening frame of a component instance.

#### `Region`

```csharp
Region
```

A region with an independent sequence-number scope.

#### `ComponentReferenceCapture`

```csharp
ComponentReferenceCapture
```

A callback frame that captures a component instance.

#### `ElementReferenceCapture`

```csharp
ElementReferenceCapture
```

A callback frame that captures an element reference.


<a id="type-rootcomponent"></a>

## `RootComponent`

Namespace: `Nazor.Rendering`

```csharp
public sealed class RootComponent
```

A component added directly by the host, with the physical nodes it produces.

[Source](../../Sources/Core/Nazor.Core/Rendering/Nodes.cs)

### Members

#### `Renderer`

```csharp
public Renderer Renderer { get; }
```

Gets the renderer that owns the component tree.

#### `Component`

```csharp
public IComponent Component {get; }
```

Gets the component instance represented or hosted by this node.

#### `Parameters`

```csharp
public ParameterView Parameters { get; }
```

The parameters passed to `Renderer.AddRootComponent(Type, ParameterView)`; set again by `Renderer.RenderOnHotReload`.

#### `Nodes`

```csharp
public IReadOnlyList<Node> Nodes { get ; }
```

Top-level physical nodes rendered by the root component.


<a id="type-routeattribute"></a>

## `RouteAttribute`

Namespace: `Nazor`

```csharp
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)] public sealed class RouteAttribute(string template) : Attribute
```

The address of a page, written in markup with `@page "/orders/{Id:int}"`: literal segments and parameters in braces, with an optional constraint (`int`, `long`, `bool`, `guid`, `decimal`, `double`, `datetime`) and a final `?` for an optional one. A page can have several.

| Parameter | Description |
| --- | --- |
| `template` | The template (string). |

[Source](../../Sources/Core/Nazor.Core/Navigation/RouteAttribute.cs)

### Members

#### `Template`

```csharp
public string Template { get; }
```

Gets the route template used to match a page address.


<a id="type-routematch"></a>

## `RouteMatch`

Namespace: `Nazor.Routing`

```csharp
public sealed record RouteMatch(Type Page, string Template, IReadOnlyDictionary<string, object?> Parameters)
```

A page found for an address, with the values of its route parameters (converted to their constraint).

| Parameter | Description |
| --- | --- |
| `Page` | The page (Type). |
| `Template` | The template (string). |
| `Parameters` | The parameters (IReadOnlyDictionary<string, object?>). |

[Source](../../Sources/Core/Nazor.Core/Navigation/Routing.cs)


<a id="type-routetable"></a>

## `RouteTable`

Namespace: `Nazor.Routing`

```csharp
public sealed class RouteTable
```

The pages of some assemblies (types with `RouteAttribute`) and the matching of addresses.

[Source](../../Sources/Core/Nazor.Core/Navigation/Routing.cs)

### Members

#### `Create`

```csharp
public static RouteTable Create(IEnumerable<Assembly> assemblies)
```

The routes of the pages in `assemblies`, the most specific first.

| Parameter | Description |
| --- | --- |
| `assemblies` | The assemblies (IEnumerable<Assembly>). |

Returns: The event callback associated with the receiver.

#### `Create`

```csharp
public static RouteTable Create(IEnumerable<Type> pages)
```

The routes of explicit pages, for tests and for pages outside the assemblies.

| Parameter | Description |
| --- | --- |
| `pages` | The pages (IEnumerable<Type>). |

Returns: The event callback associated with the receiver.

#### `Templates`

```csharp
public IEnumerable<string> Templates {get; }
```

The templates known, the most specific first.

#### `Match`

```csharp
public RouteMatch? Match(string uri)
```

The page of `uri` (its path; the query is ignored), or null.

| Parameter | Description |
| --- | --- |
| `uri` | The target address or image URI. |

Returns: The resulting RouteMatch? value.


<a id="type-runtimehelpers"></a>

## `RuntimeHelpers`

Namespace: `Nazor.CompilerServices`

```csharp
public static class RuntimeHelpers
```

Helpers called by the code generated from `.nazor` markup. Not intended for direct use.

[Source](../../Sources/Core/Nazor.Core/CompilerServices/RuntimeHelpers.cs)

### Members

#### `TypeCheck`

```csharp
public static T TypeCheck<T>(T value)
```

Returns the supplied value while enforcing its type at compile time in generated component code.

| Type parameter | Description |
| --- | --- |
| `T` | The type of the value. |

| Parameter | Description |
| --- | --- |
| `value` | The value to use. |

Returns: The supplied value, unchanged.

#### `CreateInferredEventCallback`

```csharp
public static EventCallback<T> CreateInferredEventCallback<T>(object receiver, Action<T> callback, T value)
```

Creates an event callback whose argument type is inferred from the supplied value.

| Type parameter | Description |
| --- | --- |
| `T` | The type of the value. |

| Parameter | Description |
| --- | --- |
| `receiver` | The object that receives the callback and controls event dispatch. |
| `callback` | The delegate to invoke when the callback is dispatched. |
| `value` | The value to use. |

Returns: The typed event callback associated with the receiver.

#### `CreateInferredEventCallback`

```csharp
public static EventCallback<T> CreateInferredEventCallback<T>(object receiver, Func<T, Task> callback, T value)
```

Creates an event callback whose argument type is inferred from the supplied value.

| Type parameter | Description |
| --- | --- |
| `T` | The type of the value. |

| Parameter | Description |
| --- | --- |
| `receiver` | The object that receives the callback and controls event dispatch. |
| `callback` | The delegate to invoke when the callback is dispatched. |
| `value` | The value to use. |

Returns: The typed event callback associated with the receiver.

#### `CreateInferredBindSetter`

```csharp
public static Func<T, Task> CreateInferredBindSetter<T>(Func<T, Task> callback, T value)
```

Adapts a binding setter to an asynchronous delegate, using the value to infer its type.

| Type parameter | Description |
| --- | --- |
| `T` | The type of the value. |

| Parameter | Description |
| --- | --- |
| `callback` | The delegate to invoke when the callback is dispatched. |
| `value` | The value to use. |

Returns: An asynchronous setter delegate for the inferred value type.

#### `CreateInferredBindSetter`

```csharp
public static Func<T, Task> CreateInferredBindSetter<T>(Action<T> callback, T value)
```

Adapts a binding setter to an asynchronous delegate, using the value to infer its type.

| Type parameter | Description |
| --- | --- |
| `T` | The type of the value. |

| Parameter | Description |
| --- | --- |
| `callback` | The delegate to invoke when the callback is dispatched. |
| `value` | The value to use. |

Returns: An asynchronous setter delegate for the inferred value type.

#### `InvokeSynchronousDelegate`

```csharp
public static void InvokeSynchronousDelegate(Action callback)
```

Invokes a synchronous binding callback.

| Parameter | Description |
| --- | --- |
| `callback` | The delegate to invoke when the callback is dispatched. |

#### `InvokeAsynchronousDelegate`

```csharp
public static Task InvokeAsynchronousDelegate(Action callback)
```

Invokes a binding callback and returns its completion task.

| Parameter | Description |
| --- | --- |
| `callback` | The delegate to invoke when the callback is dispatched. |

Returns: A task that completes when the operation has finished.

#### `InvokeAsynchronousDelegate`

```csharp
public static Task InvokeAsynchronousDelegate(Func<Task> callback)
```

Invokes a binding callback and returns its completion task.

| Parameter | Description |
| --- | --- |
| `callback` | The delegate to invoke when the callback is dispatched. |

Returns: A task that completes when the operation has finished.


<a id="type-scrolldeltaunit"></a>

## `ScrollDeltaUnit`

Namespace: `Nazor.Input`

```csharp
public enum ScrollDeltaUnit
```

Continuous deltas are logical pixels; wheel detents are converted using backend settings.

[Source](../../Sources/Core/Nazor.Core/Input/TextInputPolicy.cs)

### Members

#### `Notches`

```csharp
Notches
```

Expresses wheel movement in detents, scaled using the backend's scroll settings.

#### `LogicalPixels`

```csharp
LogicalPixels
```

Expresses precision scrolling directly in logical pixel distances.


<a id="type-scrolleventargs"></a>

## `ScrollEventArgs`

Namespace: `Nazor.Input`

```csharp
public class ScrollEventArgs : EventArgs
```

A scroll container moved, or its viewport or content changed size (after a layout): offsets and sizes in logical units, as `scrollTop`, `clientHeight` and `scrollHeight` in the DOM.

[Source](../../Sources/Core/Nazor.Core/Input/EventArgs.cs)

### Members

#### `ScrollLeft`

```csharp
public double ScrollLeft { get; init;}
```

Gets or sets the horizontal scroll offset in logical units.

#### `ScrollTop`

```csharp
public double ScrollTop { get; init;}
```

Gets or sets the vertical scroll offset in logical units.

#### `ViewportWidth`

```csharp
public double ViewportWidth { get; init;}
```

Gets or sets the logical width of the visible scroll viewport.

#### `ViewportHeight`

```csharp
public double ViewportHeight { get; init;}
```

Gets or sets the logical height of the visible scroll viewport.

#### `ContentWidth`

```csharp
public double ContentWidth { get; init;}
```

Gets or sets the logical width of the scrollable content.

#### `ContentHeight`

```csharp
public double ContentHeight { get; init;}
```

Gets or sets the logical height of the scrollable content.


<a id="type-textinputpolicy"></a>

## `TextInputPolicy`

Namespace: `Nazor.Input`

```csharp
public sealed record TextInputPolicy(KeyModifiers PrimaryModifier, KeyModifiers WordModifier, KeyModifiers LineModifier = KeyModifiers.None, bool HomeEndToDocument = false, bool RedoWithY = true)
```

Conventions for editing, without changing the physical modifiers delivered to the app.

| Parameter | Description |
| --- | --- |
| `PrimaryModifier` | The primary modifier (KeyModifiers). |
| `WordModifier` | The word modifier (KeyModifiers). |
| `LineModifier` | The line modifier (KeyModifiers). |
| `HomeEndToDocument` | The home end to document (bool). |
| `RedoWithY` | The redo with y (bool). |

[Source](../../Sources/Core/Nazor.Core/Input/TextInputPolicy.cs)

### Members

#### `Default`

```csharp
public static TextInputPolicy Default { get; }
```

Gets the default platform text-editing key conventions.

#### `MacOS`

```csharp
public static TextInputPolicy MacOS { get; }
```

Gets the text-editing key conventions used on macOS.

#### `IsCommand`

```csharp
public bool IsCommand(KeyModifiers modifiers)
```

Determines whether the modifier keys match this platform's command modifier convention.

| Parameter | Description |
| --- | --- |
| `modifiers` | The modifier keys pressed when the input occurred. |

Returns: true when the operation succeeds or the condition is satisfied; otherwise, false.


<a id="type-textnode"></a>

## `TextNode`

Namespace: `Nazor.Rendering`

```csharp
public sealed class TextNode : Node
```

Text content rendered by `AddContent`.

[Source](../../Sources/Core/Nazor.Core/Rendering/Nodes.cs)

### Members

#### `Text`

```csharp
public string Text { get; internal set;}
```

Gets the text represented by this object.

#### `ToString`

```csharp
public override string ToString()
```

Returns a textual representation of this value.

Returns: The textual representation of this value.


<a id="type-uidispatcher"></a>

## `UIDispatcher`

Namespace: `Nazor`

```csharp
public sealed class UIDispatcher : Dispatcher
```

A dispatcher bound to the thread that creates it. Work posted from other threads, and the continuations of `await` started on the UI thread, wait in a FIFO queue until the owner runs `RunPending`: a platform backend does it from its message loop, a headless test does it explicitly. Calls made on the owner thread run inline.

[Source](../../Sources/Core/Nazor.Core/ComponentModel/Dispatcher.cs)

### Members

#### `UIDispatcher`

```csharp
public UIDispatcher()
```

Initializes a new instance of UIDispatcher.

#### `WorkPosted`

```csharp
public event Action? WorkPosted;
```

Raised on the posting thread after work is queued; backends use it to wake their loop.

#### `HasPendingWork`

```csharp
public bool HasPendingWork {get; }
```

Gets whether work is waiting to execute on the UI dispatcher.

#### `SynchronizationContext`

```csharp
public override SynchronizationContext SynchronizationContext {get; }
```

Gets the synchronization context that schedules work on this UI dispatcher.

#### `CheckAccess`

```csharp
public override bool CheckAccess()
```

Determines whether the current thread is the dispatcher thread.

Returns: true if the current thread owns the UI dispatcher; otherwise, false.

#### `Post`

```csharp
public void Post(Action workItem)
```

Queues work for the UI thread and wakes the native event loop.

| Parameter | Description |
| --- | --- |
| `workItem` | The work to execute on the UI dispatcher. |

Throws `ArgumentNullException`: workItem is null.

#### `RunPending`

```csharp
public int RunPending()
```

Runs the queued work, including work queued meanwhile. Returns the number of items executed.

Returns: The resulting int value.

#### `RunUntil`

```csharp
public bool RunUntil(Func<bool> condition, TimeSpan timeout)
```

Runs queued work until `condition` holds, waiting for posts from other threads.

| Parameter | Description |
| --- | --- |
| `condition` | The condition (Func<bool>). |
| `timeout` | The timeout (TimeSpan). |

Returns: true when the operation succeeds or the condition is satisfied; otherwise, false.

#### `InvokeAsync`

```csharp
public override Task InvokeAsync(Action workItem)
```

Schedules the supplied work on the UI dispatcher and observes its completion.

| Parameter | Description |
| --- | --- |
| `workItem` | The work to execute on the UI dispatcher. |

Returns: A task that completes when the operation has finished.

#### `InvokeAsync`

```csharp
public override Task InvokeAsync(Func<Task> workItem)
```

Schedules the supplied work on the UI dispatcher and observes its completion.

| Parameter | Description |
| --- | --- |
| `workItem` | The work to execute on the UI dispatcher. |

Returns: A task that completes when the operation has finished.

#### `InvokeAsync`

```csharp
public override Task<TResult> InvokeAsync<TResult>(Func<TResult> workItem)
```

Schedules the supplied work on the UI dispatcher and observes its completion.

| Type parameter | Description |
| --- | --- |
| `TResult` | The type of the result. |

| Parameter | Description |
| --- | --- |
| `workItem` | The work to execute on the UI dispatcher. |

Returns: A task whose result is the value produced by the operation.

#### `InvokeAsync`

```csharp
public override Task<TResult> InvokeAsync<TResult>(Func<Task<TResult>> workItem)
```

Schedules the supplied work on the UI dispatcher and observes its completion.

| Type parameter | Description |
| --- | --- |
| `TResult` | The type of the result. |

| Parameter | Description |
| --- | --- |
| `workItem` | The work to execute on the UI dispatcher. |

Returns: A task whose result is the value produced by the operation.


<a id="type-webrendertreebuilderextensions"></a>

## `WebRenderTreeBuilderExtensions`

Namespace: `Nazor.Rendering`

```csharp
public static class WebRenderTreeBuilderExtensions
```

The ABI of Razor's event modifier emission.

[Source](../../Sources/Core/Nazor.Core/Rendering/RenderTreeBuilderExtensions.cs)

### Members

#### `AddEventPreventDefaultAttribute`

```csharp
public static void AddEventPreventDefaultAttribute(RenderTreeBuilder builder, int sequence, string eventName, bool value)
```

Sets whether the event's default behavior should be prevented.

| Parameter | Description |
| --- | --- |
| `builder` | The builder that receives the render frames. |
| `sequence` | The source-order sequence number used to match frames across renders. |
| `eventName` | The normalized event attribute name. |
| `value` | The value to use. |

#### `AddEventStopPropagationAttribute`

```csharp
public static void AddEventStopPropagationAttribute(RenderTreeBuilder builder, int sequence, string eventName, bool value)
```

Sets whether the event should stop propagating to ancestor elements.

| Parameter | Description |
| --- | --- |
| `builder` | The builder that receives the render frames. |
| `sequence` | The source-order sequence number used to match frames across renders. |
| `eventName` | The normalized event attribute name. |
| `value` | The value to use. |


<a id="type-wheeleventargs"></a>

## `WheelEventArgs`

Namespace: `Nazor.Input`

```csharp
public class WheelEventArgs : PointerEventArgs
```

Scroll deltas in the explicit DeltaUnit: `DeltaY` positive away from the user, `DeltaX` positive to the right (horizontal wheel or tilt). Shift with the vertical wheel is reported as is: the default action scrolls horizontally.

[Source](../../Sources/Core/Nazor.Core/Input/EventArgs.cs)

### Members

#### `DeltaX`

```csharp
public double DeltaX { get; init;}
```

Gets or sets the horizontal wheel or precision-scroll delta in the specified unit.

#### `DeltaY`

```csharp
public double DeltaY { get; init;}
```

Gets or sets the vertical wheel or precision-scroll delta in the specified unit.

#### `DeltaUnit`

```csharp
public ScrollDeltaUnit DeltaUnit { get; init;}
```

Gets or sets the unit used by the horizontal and vertical scroll deltas.


<a id="type-windowcommand"></a>

## Window commands and chrome

| Type | Contract |
| --- | --- |
| `WindowCommand` | Minimize, Maximize, Restore, EnterFullScreen or ExitFullScreen |
| `WindowCommandResult` | Unavailable, Unsupported, DisabledByPolicy, AlreadyInState or Dispatched |
| `WindowOperationSupport` | Backend support and its native restriction level, independent of app policy |
| `NativeRestriction` | None, Hint or Enforced for standard native window commands |
| `WindowTitleBarMenuMode` | System, Nazor or None for caption clicks handled by the backend |
| `WindowChrome` | Frame, caption buttons and menu ownership, each None, System or Nazor |

[Source](../../Sources/Core/Nazor.Core/Windows/WindowCommand.cs)

<a id="type-windowbutton"></a>

## `WindowButton`

Namespace: `Nazor`

```csharp
public enum WindowButton
```

A system title-bar control; Maximize also represents Restore when maximized.

[Source](../../Sources/Core/Nazor.Core/Windows/WindowButtonsLayout.cs)

### Members

#### `Minimize`

```csharp
Minimize
```

Identifies the native minimize caption control.

#### `Maximize`

```csharp
Maximize
```

Identifies the native maximize or restore caption control.

#### `Close`

```csharp
Close
```

Identifies the native close caption control.


<a id="type-windowbuttonslayout"></a>

## `WindowButtonsLayout`

Namespace: `Nazor`

```csharp
public sealed record WindowButtonsLayout
```

System control placement for custom title bars, independent of native control visibility. Both physical sides are ordered from left to right. An empty layout has no controls.

[Source](../../Sources/Core/Nazor.Core/Windows/WindowButtonsLayout.cs)

### Members

#### `Default`

```csharp
public static WindowButtonsLayout Default { get; }
```

Fallback when the backend has no system layout setting.

#### `Empty`

```csharp
public static WindowButtonsLayout Empty { get; }
```

Gets a caption layout without controls on either side.

#### `WindowButtonsLayout`

```csharp
public WindowButtonsLayout(IReadOnlyList<WindowButton> left, IReadOnlyList<WindowButton> right)
```

Initializes a new instance of WindowButtonsLayout.

| Parameter | Description |
| --- | --- |
| `left` | The caption controls on the left, ordered physically from left to right. |
| `right` | The caption controls on the right, ordered physically from left to right. |

#### `Left`

```csharp
public IReadOnlyList<WindowButton> Left { get; }
```

Gets the caption controls on the left, ordered physically from left to right.

#### `Right`

```csharp
public IReadOnlyList<WindowButton> Right { get; }
```

Gets the caption controls on the right, ordered physically from left to right.

#### `Equals`

```csharp
public bool Equals(WindowButtonsLayout? other)
```

Determines whether the supplied value is equal to this instance.

| Parameter | Description |
| --- | --- |
| `other` | The value to compare with this instance. |

Returns: true if the supplied value satisfies the comparison; otherwise, false.

#### `GetHashCode`

```csharp
public override int GetHashCode()
```

Returns a hash code consistent with equality for this value.

Returns: The hash code for this value.


<a id="type-windowbuttonsmetrics"></a>

## `WindowButtonsMetrics`

Namespace: `Nazor`

```csharp
public readonly record struct WindowButtonsMetrics(float LeftInset, float RightInset, float Height)
```

Occupied native control areas in logical units, zero when hidden or unsupported.

| Parameter | Description |
| --- | --- |
| `LeftInset` | The left inset (float). |
| `RightInset` | The right inset (float). |
| `Height` | The height of the value in its coordinate system. |

[Source](../../Sources/Core/Nazor.Core/Windows/WindowCapabilities.cs)

### Members

#### `Width`

```csharp
public float Width {get; }
```

Gets the total logical width reserved by the left and right caption-control insets.


<a id="type-windowenvironment"></a>

## `WindowEnvironment`

Namespace: `Nazor`

```csharp
public readonly record struct WindowEnvironment(float SafeLeft, float SafeTop, float SafeRight, float SafeBottom, WindowOcclusion? KeyboardOcclusion = null)
```

Safe insets not already excluded from the viewport, and separate keyboard occlusion.

| Parameter | Description |
| --- | --- |
| `SafeLeft` | The safe left (float). |
| `SafeTop` | The safe top (float). |
| `SafeRight` | The safe right (float). |
| `SafeBottom` | The safe bottom (float). |
| `KeyboardOcclusion` | The keyboard occlusion (WindowOcclusion?). |

[Source](../../Sources/Core/Nazor.Core/Windows/WindowEnvironment.cs)


<a id="type-windowocclusion"></a>

## `WindowOcclusion`

Namespace: `Nazor`

```csharp
public readonly record struct WindowOcclusion(float X, float Y, float Width, float Height)
```

Client-local occlusion in logical units; a floating keyboard need not touch an edge.

| Parameter | Description |
| --- | --- |
| `X` | The horizontal component of the value. |
| `Y` | The vertical component of the value. |
| `Width` | The width of the value in its coordinate system. |
| `Height` | The height of the value in its coordinate system. |

[Source](../../Sources/Core/Nazor.Core/Windows/WindowEnvironment.cs)


<a id="type-windowoperations"></a>

## `WindowOperations`

Namespace: `Nazor`

```csharp
public readonly record struct WindowOperations(bool IsMinimizable, bool IsMaximizable, bool IsResizable, bool FullScreenEnabled)
```

Independent requested operations; effective support is reported by WindowCapabilities.

| Parameter | Description |
| --- | --- |
| `IsMinimizable` | The can minimize (bool). |
| `IsMaximizable` | The can maximize (bool). |
| `IsResizable` | The can resize (bool). |
| `FullScreenEnabled` | The can full screen (bool). |

[Source](../../Sources/Core/Nazor.Core/Windows/WindowCapabilities.cs)

### Members

#### `Default`

```csharp
public static WindowOperations Default {get; }
```

Gets the standard operation defaults, with minimize, maximize, resize, and full screen enabled.

#### `Dialog`

```csharp
public static WindowOperations Dialog {get; }
```

Gets the default operation policy for a dialog window.


<a id="type-windowsbackdrop"></a>

## `WindowsBackdrop`

Namespace: `Nazor`

```csharp
public enum WindowsBackdrop
```

Windows materials; describing one does not load Windows libraries.

[Source](../../Sources/Core/Nazor.Core/Windows/Backdrop.cs)

### Members

#### `Mica`

```csharp
Mica
```

Requests the Windows Mica material.

#### `MicaAlt`

```csharp
MicaAlt
```

Requests the Windows Mica Alt material.

#### `Acrylic`

```csharp
Acrylic
```

Requests the Windows Acrylic material.


<a id="type-windowstate"></a>

## `WindowState`

Namespace: `Nazor`

```csharp
public enum WindowState
```

How a window is shown.

[Source](../../Sources/Core/Nazor.Core/Windows/IWindow.cs)

### Members

#### `Normal`

```csharp
Normal
```

Shows the window at its normal restored bounds.

#### `Maximized`

```csharp
Maximized
```

Expands the window to the screen work area.

#### `Minimized`

```csharp
Minimized
```

Minimizes the window.

#### `FullScreen`

```csharp
FullScreen
```

Uses the full screen without normal window decorations.


<a id="type-windowstitlebarheight"></a>

## `WindowsTitleBarHeight`

Namespace: `Nazor`

```csharp
public enum WindowsTitleBarHeight
```

Preferred native caption-button height; the platform reports the actual logical size.

[Source](../../Sources/Core/Nazor.Core/Windows/WindowCapabilities.cs)

### Members

#### `Standard`

```csharp
Standard
```

Uses the standard native caption-button height.

#### `Tall`

```csharp
Tall
```

Uses the taller native caption-button height.

<a id="type-systemcolorprofile"></a>

## `SystemColorProfile`

Namespace: `Nazor`

```csharp
public enum SystemColorProfile
```

The fallback palette used when a backend cannot supply a system color. Profiles approximate desktop defaults and do not implement native themes.

[Source](../../Sources/Core/Nazor.Core/Windows/SystemColorProfile.cs)

| Member | Fallback profile |
| --- | --- |
| `Generic` | Desktop-independent Nazor light/dark colors |
| `Windows` | Windows light/dark colors |
| `KDE` | KDE Breeze light/dark colors |
| `GNOME` | GNOME Adwaita light/dark colors |
| `MacOS` | macOS light/dark colors |
| `Android` | Android light/dark colors |
| `IOS` | iOS light/dark colors |

<a id="type-istylesheet"></a>

## `IStyleSheet`

Namespace `Nazor`.

```csharp
public interface IStyleSheet
```

Marker for styles supplied through the platform-independent IWindow contract. The application host accepts Nazor.Style.StyleSheet.

<a id="type-ioverlaymotion"></a>

## `IOverlayMotion`

Namespace `Nazor.Rendering`. [Source](../../Sources/Core/Nazor.Core/Rendering/IOverlayMotion.cs)

| Declaration | Contract |
| --- | --- |
| `public interface IOverlayMotion` | Host-resolved exit motion attached to an overlay or `exit-presence=true` element while it belongs to a presentation scene. The feature is removed on element removal and scene disposal. |
| `bool ReducedMotion { get; }` | Current preference; true requires immediate state changes without retained exit motion. |
| `event Action? PreferencesChanged` | Raised when the effective reduced-motion preference changes. Receivers request a render and detach when disposed. |
| `TimeSpan GetExitDuration(ElementNode surface)` | The theme's `overlay-exit-duration`; zero when motion is reduced, the surface is not rendered or its concrete anchor is unavailable. `surface` is the element carrying this provider. |
| `TimeSpan GetSubmenuOpenDelay(ElementNode trigger)` | Returns the theme hover delay for a retained submenu trigger inside this overlay. The default implementation returns 250ms, or zero when ReducedMotion is true. Scene uses submenu-open-delay from the trigger's style. |

<a id="type-itooltipcontroller"></a>

## `ITooltipController`

Namespace: `Nazor.Input`

```csharp
public interface ITooltipController
```

Connects composable tooltip content to the window's shared hover/focus scheduler.

[Source](../../Sources/Core/Nazor.Core/Input/ITooltipController.cs#L6)


### `Delay`

```csharp
TimeSpan? Delay { get; }
```

Gets the optional opening delay; null uses the input manager's default.

[Source](../../Sources/Core/Nazor.Core/Input/ITooltipController.cs#L9)


### `IsAvailable`

```csharp
bool IsAvailable { get; }
```

Gets whether this tooltip can currently be shown.

[Source](../../Sources/Core/Nazor.Core/Input/ITooltipController.cs#L11)


### `ContentElement`

```csharp
ElementNode? ContentElement { get; }
```

Gets the retained tooltip surface for hover continuity.

[Source](../../Sources/Core/Nazor.Core/Input/ITooltipController.cs#L13)


### `OpenAsync`

```csharp
Task OpenAsync()
```

Requests opening through the component's normal state contract.

[Source](../../Sources/Core/Nazor.Core/Input/ITooltipController.cs#L15)


### `CloseAsync`

```csharp
Task CloseAsync()
```

Requests closing through the component's normal state contract.

[Source](../../Sources/Core/Nazor.Core/Input/ITooltipController.cs#L17)

## `IFontSymbol`

Namespace `Nazor`. [Source](../../Sources/Core/Nazor.Core/Media/IFontSymbol.cs)

A renderer-independent icon-font symbol. Implementations should be immutable and return a nonblank glyph string and one internal font family name without control characters. The application registers the font before startup; the symbol does not load it. `Icon.Symbol` applies this family independently of the active theme. See [typed font symbols](../Guide/22-Images-Icons-and-Drawing.md#other-icon-fonts-and-raw-codes).

| Declaration | Contract |
| --- | --- |
| `public interface IFontSymbol` | A font icon's glyph text and matching family. |
| `string FontFamily { get; }` | Internal font family name, not a filename or NSS list. |
| `string Glyph { get; }` | Nonblank Unicode text; supplementary characters use surrogate pairs. |
| `IReadOnlyList<IFontSymbol> Fallbacks { get; }` | Ordered equivalent family/glyph alternatives; default empty. Exact provider family and full Unicode coverage are required. At most 32 distinct symbols are visited depth-first; cycles are skipped. |

## `FontSymbol`

Namespace `Nazor`. [Source](../../Sources/Core/Nazor.Core/Media/FontSymbol.cs)

| Declaration | Contract |
| --- | --- |
| `public readonly struct FontSymbol : IFontSymbol, IEquatable<FontSymbol>` | Immutable record value holding one family/glyph pair. A default value is empty and invalid as an active icon. |
| `public FontSymbol(string FontFamily, string Glyph)` | Stores a family and its nonblank Unicode glyph text; active icons validate both. |
| `public FontSymbol()` | Creates the empty default value. |
| `public string FontFamily { get; init; }` | One internal font family name, without control characters. |
| `public string Glyph { get; init; }` | Nonblank Unicode text, including supplementary characters. |
| `public void Deconstruct(out string FontFamily, out string Glyph)` | Deconstructs the pair. |
| `public override string ToString()` | Formats two escaped quoted NSS strings: family followed by glyph. Throws InvalidOperationException for control-bearing values that NSS cannot represent. |
| `public static bool operator ==(FontSymbol left, FontSymbol right)` | Value equality. |
| `public static bool operator !=(FontSymbol left, FontSymbol right)` | Value inequality. |
| `public override int GetHashCode()` | Value hash. |
| `public override bool Equals(object obj)` | Value equality; null compares unequal. |
| `public bool Equals(FontSymbol other)` | Typed value equality. |

## API signature index

These declarations supplement the contracts above. Inherited members are documented with the declaring type.

### `BackdropResult`

Namespace `Nazor`. [Source](../../Sources/Core/Nazor.Core/Windows/Backdrop.cs)

| Declaration | Contract |
| --- | --- |
| `public readonly struct BackdropResult : IEquatable<BackdropResult>` | What the backend accepted. Material is a diagnostic identifier, not a rendering guarantee.<br>`IsActive`. The is active (bool).<br>`Material`. The material (string?). |
| `public BackdropResult()` |  |
| `public BackdropResult(bool IsActive, string? Material = null)` | What the backend accepted. Material is a diagnostic identifier, not a rendering guarantee.<br>`IsActive`. The is active (bool).<br>`Material`. The material (string?). |
| `public override string ToString()` |  |
| `public static bool operator !=(BackdropResult left, BackdropResult right)` |  |
| `public static bool operator ==(BackdropResult left, BackdropResult right)` |  |
| `public override int GetHashCode()` |  |
| `public override bool Equals(object obj)` |  |
| `public bool Equals(BackdropResult other)` |  |
| `public void Deconstruct(out bool IsActive, out string? Material)` |  |
| `public bool IsActive { get; init; }` | The is active (bool). |
| `public string? Material { get; init; }` | The material (string?). |

### `BackdropSelection`

Namespace `Nazor`. [Source](../../Sources/Core/Nazor.Core/Windows/Backdrop.cs)

| Declaration | Contract |
| --- | --- |
| `public readonly struct BackdropSelection : IEquatable<BackdropSelection>` | An immutable request, resolved by the native window rather than by the page. |
| `public BackdropSelection()` |  |
| `public override string ToString()` |  |
| `public static bool operator !=(BackdropSelection left, BackdropSelection right)` |  |
| `public static bool operator ==(BackdropSelection left, BackdropSelection right)` |  |
| `public override int GetHashCode()` |  |
| `public override bool Equals(object obj)` |  |
| `public bool Equals(BackdropSelection other)` |  |

### `BackgroundMode`

Namespace `Nazor`. [Source](../../Sources/Core/Nazor.Core/Windows/WindowCapabilities.cs)

| Declaration | Contract |
| --- | --- |
| `public enum BackgroundMode` | How the window's client surface is composited. |
| `Default = 0` | Uses the backend's normal surface and honors the requested system backdrop. |
| `Transparent = 1` | Composites the application's per-pixel alpha with the desktop. |

### `CascadingParameterAttribute`

Namespace `Nazor`. [Source](../../Sources/Core/Nazor.Core/ComponentModel/IComponent.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed class CascadingParameterAttribute : Attribute` | A value supplied by the nearest matching CascadingValue or a root DI registration. |
| `public CascadingParameterAttribute()` |  |

### `CascadingTypeParameterAttribute`

Namespace `Nazor`. [Source](../../Sources/Core/Nazor.Core/ComponentModel/IComponent.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed class CascadingTypeParameterAttribute : Attribute` | Propagates a named generic type parameter to descendant components in markup.<br>`name`. The name used to identify the parameter, property, resource, or registration. |
| `public CascadingTypeParameterAttribute(string name)` | Propagates a named generic type parameter to descendant components in markup.<br>`name`. The name used to identify the parameter, property, resource, or registration. |

### `CascadingValue<TValue>`

Namespace `Nazor`. [Source](../../Sources/Core/Nazor.Core/ComponentModel/CascadingValue.cs)

| Declaration | Contract |
| --- | --- |
| `public CascadingValue()` |  |
| `public TValue Value { get; set; }` | `[Parameter]`<br>Gets or sets the value supplied to descendant components. |
| `public string? Name { get; set; }` | `[Parameter]`<br>Gets or sets the optional name used to match descendant cascading parameters. |
| `public bool IsFixed { get; set; }` | `[Parameter]`<br>Gets or sets whether the cascading value is fixed and consumers can skip change subscriptions. |
| `public RenderFragment? ChildContent { get; set; }` | `[Parameter]`<br>Gets or sets the render fragment supplied between the component's opening and closing tags. |

### `CommonBackdrop`

Namespace `Nazor`. [Source](../../Sources/Core/Nazor.Core/Windows/Backdrop.cs)

| Declaration | Contract |
| --- | --- |
| `None = 0` | Requests no client-area backdrop material; system decorations retain their native appearance. |
| `Blur = 1` | Requests Acrylic on Windows or background blur on Linux, when supported. |
| `Solid = 2` | Requests Mica on Windows or no compositor effect on Linux. |

### `Dispatcher`

Namespace `Nazor`. [Source](../../Sources/Core/Nazor.Core/ComponentModel/Dispatcher.cs)

| Declaration | Contract |
| --- | --- |
| `protected Dispatcher()` |  |

### `EditorRequiredAttribute`

Namespace `Nazor`. [Source](../../Sources/Core/Nazor.Core/ComponentModel/IComponent.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed class EditorRequiredAttribute : Attribute` | Requires an explicit assignment when a component is used in Nazor markup. |
| `public EditorRequiredAttribute()` |  |

### `ElementReference`

Namespace `Nazor`. [Source](../../Sources/Core/Nazor.Core/Input/ElementReference.cs)

| Declaration | Contract |
| --- | --- |
| `public readonly struct ElementReference` | An element retained by the renderer. Interaction is supplied by the window's input manager.<br>`element`. The element to inspect or update. |
| `public ElementReference()` |  |
| `public ElementReference(ElementNode element)` | An element retained by the renderer. Interaction is supplied by the window's input manager.<br>`element`. The element to inspect or update. |
| `public (double X, double Y, double Width, double Height)? GetBounds()` | Gets the element's border box in logical window coordinates, the space of pointer event positions, as last laid out; null before the first layout or when no window shows the element. Two-dimensional pickers use it to map a pointer position to a value.<br>**Returns**. The left and top edges, the width and the height, or null. |
| `public (double X, double Y)? GetLocalPoint(double windowX, double windowY)` | Converts logical window coordinates to a point relative to the untransformed border-box origin, undoing ancestor scrolling and transforms. Returns null for nonfinite/out-of-range coordinates, detached/hidden/unlaid-out elements, singular transforms or unsupported interaction providers. |
| `public bool ScrollIntoView()` | Scrolls every scroll container around the element so that it becomes visible; `onscroll` follows. False when nothing moved, or when no window shows the element yet.<br>**Returns**. true when a container scrolled; otherwise, false. |

### `EventCallback`

Namespace `Nazor`. [Source](../../Sources/Core/Nazor.Core/ComponentModel/EventCallback.cs)

| Declaration | Contract |
| --- | --- |
| `public EventCallback()` |  |

### `EventCallback<TValue>`

Namespace `Nazor`. [Source](../../Sources/Core/Nazor.Core/ComponentModel/EventCallback.cs)

| Declaration | Contract |
| --- | --- |
| `public EventCallback()` |  |

### `EventCallbackFactory`

Namespace `Nazor`. [Source](../../Sources/Core/Nazor.Core/ComponentModel/EventCallback.cs)

| Declaration | Contract |
| --- | --- |
| `public EventCallbackFactory()` |  |

### `EventCallbackWorkItem`

Namespace `Nazor`. [Source](../../Sources/Core/Nazor.Core/ComponentModel/EventCallback.cs)

| Declaration | Contract |
| --- | --- |
| `public EventCallbackWorkItem()` |  |

### `EventHandlerAttribute`

Namespace `Nazor`. [Source](../../Sources/Core/Nazor.Core/ComponentModel/IComponent.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed class EventHandlerAttribute : Attribute` | Declares a directive event attribute such as `@onclick` and the type of its arguments. The compiler makes the attribute available in markup; the value reaches the component parameter with the same name, compared case-insensitively (for example `OnClick`).<br>`attributeName`. The directive event attribute name recognized by the compiler.<br>`eventArgsType`. The CLR type used for this event's arguments.<br>`enableStopPropagation`. Whether markup may request that this event stop propagating.<br>`enablePreventDefault`. Whether markup may request prevention of the event's default action. |
| `public EventHandlerAttribute(string attributeName, Type eventArgsType, bool enableStopPropagation, bool enablePreventDefault)` | Declares a directive event attribute such as `@onclick` and the type of its arguments. The compiler makes the attribute available in markup; the value reaches the component parameter with the same name, compared case-insensitively (for example `OnClick`).<br>`attributeName`. The directive event attribute name recognized by the compiler.<br>`eventArgsType`. The CLR type used for this event's arguments.<br>`enableStopPropagation`. Whether markup may request that this event stop propagating.<br>`enablePreventDefault`. Whether markup may request prevention of the event's default action. |

### `HotReloadManager`

Namespace `Nazor`. [Source](../../Sources/Core/Nazor.Core/HotReload/HotReloadManager.cs)

| Declaration | Contract |
| --- | --- |
| `public static Exception? LastError { get; }` | The first subscriber failure of the last delta, or null; reset at every delta. |

### `IElementInteraction`

Namespace `Nazor`. [Source](../../Sources/Core/Nazor.Core/Input/ElementReference.cs)

| Declaration | Contract |
| --- | --- |
| `(double X, double Y, double Width, double Height)? GetBounds(ElementNode element)` | Gets the element's border box in logical window coordinates, or null when it is not laid out.<br>`element`. The element to inspect.<br>**Returns**. The left and top edges, the width and the height, or null. |
| `(double X, double Y)? GetLocalPoint(ElementNode element, double windowX, double windowY)` | Converts the logical window point to the element's untransformed border-box coordinate space. The default interface implementation returns null. Invalid coordinates, detached/hidden/unlaid-out elements, singular transforms and unavailable support also return null. |
| `bool BringIntoView(ElementNode element)` | Brings an element inside the visible area of its scrolling ancestors.<br>`element`. The element to bring into view.<br>**Returns**. true when a container scrolled; otherwise, false. |
| `bool HasPointerCapture(ElementNode element)` | Gets whether the element owns pointer capture. The default interface implementation returns false, allowing interaction providers without capture-state support. `element` is the element to inspect. |

### `ImageData`

Namespace `Nazor`. [Source](../../Sources/Core/Nazor.Core/Media/ImageSource.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed record ImageData : IEquatable<ImageData>` | The encoded bytes of an image and the display scale they were made for (2 for "logo@2x.png").<br>`Bytes`. The bytes (byte[]).<br>`Scale`. The scale (float). |
| `public ImageData(byte[] Bytes, float Scale)` | The encoded bytes of an image and the display scale they were made for (2 for "logo@2x.png").<br>`Bytes`. The bytes (byte[]).<br>`Scale`. The scale (float). |
| `public override string ToString()` |  |
| `public static bool operator !=(ImageData? left, ImageData? right)` |  |
| `public static bool operator ==(ImageData? left, ImageData? right)` |  |
| `public override int GetHashCode()` |  |
| `public override bool Equals(object? obj)` |  |
| `public bool Equals(ImageData? other)` |  |
| `public void Deconstruct(out byte[] Bytes, out float Scale)` |  |
| `public byte[] Bytes { get; init; }` | The bytes (byte[]). |
| `public float Scale { get; init; }` | The scale (float). |

### `ImageSource`

Namespace `Nazor`. [Source](../../Sources/Core/Nazor.Core/Media/ImageSource.cs)

| Declaration | Contract |
| --- | --- |
| `public abstract Task<ImageData> LoadAsync(float scale, CancellationToken cancellationToken = default(CancellationToken))` | Reads the encoded bytes of the best variant for `scale`; throws when the image cannot be read.<br>`scale`. The number of physical pixels per logical unit.<br>`cancellationToken`. A token used to request cancellation.<br>**Returns**. A task whose result is the value produced by the operation. |

### `InjectAttribute`

Namespace `Nazor`. [Source](../../Sources/Core/Nazor.Core/ComponentModel/IComponent.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed class InjectAttribute : Attribute` | Marks a component property that is resolved from the renderer's service provider. |
| `public InjectAttribute()` |  |

### `LinuxBackdrop`

Namespace `Nazor`. [Source](../../Sources/Core/Nazor.Core/Windows/Backdrop.cs)

| Declaration | Contract |
| --- | --- |
| `Blur = 0` | Requests the compositor's blur-behind effect. |

### `LocationChangedEventArgs`

Namespace `Nazor`. [Source](../../Sources/Core/Nazor.Core/Navigation/NavigationManager.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed class LocationChangedEventArgs : EventArgs` | The location changed: `Nazor.LocationChangedEventArgs.Entry` is the current one.<br>`entry`. The navigation entry that became current. |
| `public LocationChangedEventArgs(NavigationEntry entry, NavigationKind kind = NavigationKind.New)` | The location changed: `Nazor.LocationChangedEventArgs.Entry` is the current one.<br>`entry`. The navigation entry that became current.<br>`kind`. The operation that selected this entry. |

### `LocationChangingContext`

Namespace `Nazor`. [Source](../../Sources/Core/Nazor.Core/Navigation/NavigationManager.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed class LocationChangingContext` | A navigation about to happen, which a handler can stop (unsaved changes).<br>`target`. The component, element, or navigation entry targeted by the operation.<br>`current`. The current navigation entry before the change. |
| `public LocationChangingContext(NavigationEntry target, NavigationEntry current)` | A navigation about to happen, which a handler can stop (unsaved changes).<br>`target`. The component, element, or navigation entry targeted by the operation.<br>`current`. The current navigation entry before the change. |
| `public bool IsNavigationPrevented { get; }` | Gets whether a location-changing handler canceled the pending navigation. |

### `NativeRestriction`

Namespace `Nazor`. [Source](../../Sources/Core/Nazor.Core/Windows/WindowCommand.cs)

| Declaration | Contract |
| --- | --- |
| `public enum NativeRestriction` | How the backend applies an application restriction to standard native window commands. |
| `None = 0` | The app controls its own commands and controls. |
| `Hint = 1` | The backend sends a hint that the window manager may choose to honor. |
| `Enforced = 2` | The backend enforces the restriction on standard native commands for the window. |

### `NavigationEntry`

Namespace `Nazor`. [Source](../../Sources/Core/Nazor.Core/Navigation/NavigationManager.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed record NavigationEntry : IEquatable<NavigationEntry>` | A place in the history of a window: an address (`/orders/42`) or a page type with its parameters, for pages reached without an address.<br>`Uri`. The target address or image URI.<br>`Page`. The page (Type?).<br>`Parameters`. The parameters (IReadOnlyDictionary<string, object?>?). |
| `public NavigationEntry(string? Uri, Type? Page = null, IReadOnlyDictionary<string, object?>? Parameters = null)` | A place in the history of a window: an address (`/orders/42`) or a page type with its parameters, for pages reached without an address.<br>`Uri`. The target address or image URI.<br>`Page`. The page (Type?).<br>`Parameters`. The parameters (IReadOnlyDictionary<string, object?>?). |
| `public override string ToString()` |  |
| `public static bool operator !=(NavigationEntry? left, NavigationEntry? right)` |  |
| `public static bool operator ==(NavigationEntry? left, NavigationEntry? right)` |  |
| `public override int GetHashCode()` |  |
| `public override bool Equals(object? obj)` |  |
| `public bool Equals(NavigationEntry? other)` |  |
| `public void Deconstruct(out string? Uri, out Type? Page, out IReadOnlyDictionary<string, object?>? Parameters)` |  |
| `public string? Uri { get; init; }` | The target address or image URI. |
| `public Type? Page { get; init; }` | The page (Type?). |
| `public IReadOnlyDictionary<string, object?>? Parameters { get; init; }` | The parameters (IReadOnlyDictionary<string, object?>?). |

### `NavigationManager`

Namespace `Nazor`. [Source](../../Sources/Core/Nazor.Core/Navigation/NavigationManager.cs)

| Declaration | Contract |
| --- | --- |
| `public Task<bool> NavigateToAsync(NavigationEntry target, bool replace = false)` | Navigates to the supplied target after awaiting all location-changing handlers.<br>`target`. The component, element, or navigation entry targeted by the operation.<br>`replace`. Whether to replace the current history entry instead of appending a new one.<br>**Returns**. A task returning true if navigation completed, or false if prevented or superseded by a newer request.<br>**Throws** `System.ArgumentNullException`. target is null. |
| `public NavigationManager()` |  |

### `ParameterAttribute`

Namespace `Nazor`. [Source](../../Sources/Core/Nazor.Core/ComponentModel/IComponent.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed class ParameterAttribute : Attribute` | Marks a component property that can be set by the parent through markup. |
| `public ParameterAttribute()` |  |

### `ParameterValue`

Namespace `Nazor`. [Source](../../Sources/Core/Nazor.Core/ComponentModel/ParameterView.cs)

| Declaration | Contract |
| --- | --- |
| `public readonly struct ParameterValue : IEquatable<ParameterValue>` | A parameter name and value supplied to a component.<br>`Name`. The name used to identify the parameter, property, resource, or registration.<br>`Value`. The numeric value or payload represented by this record. |
| `public ParameterValue()` |  |
| `public ParameterValue(string Name, object? Value)` | A parameter name and value supplied to a component.<br>`Name`. The name used to identify the parameter, property, resource, or registration.<br>`Value`. The numeric value or payload represented by this record. |
| `public override string ToString()` |  |
| `public static bool operator !=(ParameterValue left, ParameterValue right)` |  |
| `public static bool operator ==(ParameterValue left, ParameterValue right)` |  |
| `public override int GetHashCode()` |  |
| `public override bool Equals(object obj)` |  |
| `public bool Equals(ParameterValue other)` |  |
| `public void Deconstruct(out string Name, out object? Value)` |  |
| `public string Name { get; init; }` | The name used to identify the parameter, property, resource, or registration. |
| `public object? Value { get; init; }` | The numeric value or payload represented by this record. |

### `ParameterView`

Namespace `Nazor`. [Source](../../Sources/Core/Nazor.Core/ComponentModel/ParameterView.cs)

| Declaration | Contract |
| --- | --- |
| `public readonly struct ParameterView : IEnumerable<ParameterValue>, IEnumerable` | The parameters supplied to a component. Names are matched to `Nazor.ParameterAttribute` properties case-insensitively, so the markup attribute `@onclick` sets `OnClick`. |
| `public ParameterView()` |  |

### `PlatformKind`

Namespace `Nazor`. [Source](../../Sources/Core/Nazor.Core/Windows/PlatformKind.cs)

| Declaration | Contract |
| --- | --- |
| `Windows = 0` | Identifies the Microsoft Windows platform. |
| `Linux = 1` | Identifies the Linux platform. |
| `MacOS = 2` | Identifies the Apple macOS platform. |
| `Android = 3` | Identifies the Android platform. |
| `IOS = 4` | Identifies the Apple iOS platform. |

### `RenderFragment`

Namespace `Nazor`. [Source](../../Sources/Core/Nazor.Core/ComponentModel/IComponent.cs)

| Declaration | Contract |
| --- | --- |
| `delegate void RenderFragment(RenderTreeBuilder builder)` | Content rendered into a `Nazor.Rendering.RenderTreeBuilder`.<br>`builder`. The builder that receives the render frames. |

### `RenderFragment<TValue>`

Namespace `Nazor`. [Source](../../Sources/Core/Nazor.Core/ComponentModel/IComponent.cs)

| Declaration | Contract |
| --- | --- |
| `delegate RenderFragment RenderFragment<TValue>(TValue value)` | A template that produces content for a value.<br>`value`. The value to use.<br>`TValue`. The type of the component, callback, or cascading value.<br>**Returns**. The resulting RenderFragment value. |

### `RenderHandle`

Namespace `Nazor`. [Source](../../Sources/Core/Nazor.Core/ComponentModel/RenderHandle.cs)

| Declaration | Contract |
| --- | --- |
| `public RenderHandle()` |  |

### `RouteAttribute`

Namespace `Nazor`. [Source](../../Sources/Core/Nazor.Core/Navigation/RouteAttribute.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed class RouteAttribute : Attribute` | The address of a page, written in markup with `@page "/orders/{Id:int}"`: literal segments and parameters in braces, with an optional constraint (`int`, `long`, `bool`, `guid`, `decimal`, `double`, `datetime`) and a final `?` for an optional one. A page can have several.<br>`template`. The template (string). |
| `public RouteAttribute(string template)` | The address of a page, written in markup with `@page "/orders/{Id:int}"`: literal segments and parameters in braces, with an optional constraint (`int`, `long`, `bool`, `guid`, `decimal`, `double`, `datetime`) and a final `?` for an optional one. A page can have several.<br>`template`. The template (string). |

### `SystemColorProfile`

Namespace `Nazor`. [Source](../../Sources/Core/Nazor.Core/Windows/SystemColorProfile.cs)

| Declaration | Contract |
| --- | --- |
| `Generic = 0` | A desktop-independent Nazor palette. |
| `Windows = 1` | A Windows light or dark palette. |
| `KDE = 2` | A KDE Breeze light or dark palette. |
| `GNOME = 3` | A GNOME Adwaita light or dark palette. |
| `MacOS = 4` | A macOS light or dark palette. |
| `Android = 5` | An Android light or dark palette. |
| `IOS = 6` | An iOS light or dark palette. |

### `WindowButton`

Namespace `Nazor`. [Source](../../Sources/Core/Nazor.Core/Windows/WindowButtonsLayout.cs)

| Declaration | Contract |
| --- | --- |
| `Minimize = 0` | Identifies the native minimize caption control. |
| `Maximize = 1` | Identifies the native maximize or restore caption control. |
| `Close = 2` | Identifies the native close caption control. |

### `WindowButtonsLayout`

Namespace `Nazor`. [Source](../../Sources/Core/Nazor.Core/Windows/WindowButtonsLayout.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed record WindowButtonsLayout : IEquatable<WindowButtonsLayout>` | System control placement for custom title bars, independent of native control visibility. Both physical sides are ordered from left to right. An empty layout has no controls. |
| `public override string ToString()` |  |
| `public static bool operator !=(WindowButtonsLayout? left, WindowButtonsLayout? right)` |  |
| `public static bool operator ==(WindowButtonsLayout? left, WindowButtonsLayout? right)` |  |
| `public override bool Equals(object? obj)` |  |

### `WindowButtonsMetrics`

Namespace `Nazor`. [Source](../../Sources/Core/Nazor.Core/Windows/WindowCapabilities.cs)

| Declaration | Contract |
| --- | --- |
| `public readonly struct WindowButtonsMetrics : IEquatable<WindowButtonsMetrics>` | Occupied native control areas in logical units, zero when hidden or unsupported.<br>`LeftInset`. The left inset (float).<br>`RightInset`. The right inset (float).<br>`Height`. The height of the value in its coordinate system. |
| `public WindowButtonsMetrics()` |  |
| `public WindowButtonsMetrics(float LeftInset, float RightInset, float Height)` | Occupied native control areas in logical units, zero when hidden or unsupported.<br>`LeftInset`. The left inset (float).<br>`RightInset`. The right inset (float).<br>`Height`. The height of the value in its coordinate system. |
| `public override string ToString()` |  |
| `public static bool operator !=(WindowButtonsMetrics left, WindowButtonsMetrics right)` |  |
| `public static bool operator ==(WindowButtonsMetrics left, WindowButtonsMetrics right)` |  |
| `public override int GetHashCode()` |  |
| `public override bool Equals(object obj)` |  |
| `public bool Equals(WindowButtonsMetrics other)` |  |
| `public void Deconstruct(out float LeftInset, out float RightInset, out float Height)` |  |
| `public float LeftInset { get; init; }` | The left inset (float). |
| `public float RightInset { get; init; }` | The right inset (float). |
| `public float Height { get; init; }` | The height of the value in its coordinate system. |

### `WindowCommand`

Namespace `Nazor`. [Source](../../Sources/Core/Nazor.Core/Windows/WindowCommand.cs)

| Declaration | Contract |
| --- | --- |
| `public enum WindowCommand` | A command requested for a window. |
| `Minimize = 0` | Requests minimization. |
| `Maximize = 1` | Requests maximization. |
| `Restore = 2` | Restores from a non-normal state. |
| `EnterFullScreen = 3` | Requests entry into fullscreen. |
| `ExitFullScreen = 4` | Requests a return from fullscreen. |

### `WindowCommandResult`

Namespace `Nazor`. [Source](../../Sources/Core/Nazor.Core/Windows/WindowCommand.cs)

| Declaration | Contract |
| --- | --- |
| `public enum WindowCommandResult` | The outcome of checking and dispatching a window command. |
| `Unavailable = 0` | There is no open native window. |
| `Unsupported = 1` | The backend does not support the command in the current state. |
| `DisabledByPolicy = 2` | The app policy disallows the command. |
| `AlreadyInState = 3` | The last reported state already matches the command. |
| `Dispatched = 4` | The request was sent to the backend. Observe StateChanged for the actual state. |

### `WindowEnvironment`

Namespace `Nazor`. [Source](../../Sources/Core/Nazor.Core/Windows/WindowEnvironment.cs)

| Declaration | Contract |
| --- | --- |
| `public readonly struct WindowEnvironment : IEquatable<WindowEnvironment>` | Safe insets not already excluded from the viewport, and separate keyboard occlusion.<br>`SafeLeft`. The safe left (float).<br>`SafeTop`. The safe top (float).<br>`SafeRight`. The safe right (float).<br>`SafeBottom`. The safe bottom (float).<br>`KeyboardOcclusion`. The keyboard occlusion (WindowOcclusion?). |
| `public WindowEnvironment()` |  |
| `public WindowEnvironment(float SafeLeft, float SafeTop, float SafeRight, float SafeBottom, WindowOcclusion? KeyboardOcclusion = null)` | Safe insets not already excluded from the viewport, and separate keyboard occlusion.<br>`SafeLeft`. The safe left (float).<br>`SafeTop`. The safe top (float).<br>`SafeRight`. The safe right (float).<br>`SafeBottom`. The safe bottom (float).<br>`KeyboardOcclusion`. The keyboard occlusion (WindowOcclusion?). |
| `public override string ToString()` |  |
| `public static bool operator !=(WindowEnvironment left, WindowEnvironment right)` |  |
| `public static bool operator ==(WindowEnvironment left, WindowEnvironment right)` |  |
| `public override int GetHashCode()` |  |
| `public override bool Equals(object obj)` |  |
| `public bool Equals(WindowEnvironment other)` |  |
| `public void Deconstruct(out float SafeLeft, out float SafeTop, out float SafeRight, out float SafeBottom, out WindowOcclusion? KeyboardOcclusion)` |  |
| `public float SafeLeft { get; init; }` | The safe left (float). |
| `public float SafeTop { get; init; }` | The safe top (float). |
| `public float SafeRight { get; init; }` | The safe right (float). |
| `public float SafeBottom { get; init; }` | The safe bottom (float). |
| `public WindowOcclusion? KeyboardOcclusion { get; init; }` | The keyboard occlusion (WindowOcclusion?). |

### `WindowOcclusion`

Namespace `Nazor`. [Source](../../Sources/Core/Nazor.Core/Windows/WindowEnvironment.cs)

| Declaration | Contract |
| --- | --- |
| `public readonly struct WindowOcclusion : IEquatable<WindowOcclusion>` | Client-local occlusion in logical units; a floating keyboard need not touch an edge.<br>`X`. The horizontal component of the value.<br>`Y`. The vertical component of the value.<br>`Width`. The width of the value in its coordinate system.<br>`Height`. The height of the value in its coordinate system. |
| `public WindowOcclusion()` |  |
| `public WindowOcclusion(float X, float Y, float Width, float Height)` | Client-local occlusion in logical units; a floating keyboard need not touch an edge.<br>`X`. The horizontal component of the value.<br>`Y`. The vertical component of the value.<br>`Width`. The width of the value in its coordinate system.<br>`Height`. The height of the value in its coordinate system. |
| `public override string ToString()` |  |
| `public static bool operator !=(WindowOcclusion left, WindowOcclusion right)` |  |
| `public static bool operator ==(WindowOcclusion left, WindowOcclusion right)` |  |
| `public override int GetHashCode()` |  |
| `public override bool Equals(object obj)` |  |
| `public bool Equals(WindowOcclusion other)` |  |
| `public void Deconstruct(out float X, out float Y, out float Width, out float Height)` |  |
| `public float X { get; init; }` | The horizontal component of the value. |
| `public float Y { get; init; }` | The vertical component of the value. |
| `public float Width { get; init; }` | The width of the value in its coordinate system. |
| `public float Height { get; init; }` | The height of the value in its coordinate system. |

### `WindowOperationSupport`

Namespace `Nazor`. [Source](../../Sources/Core/Nazor.Core/Windows/WindowCommand.cs)

| Declaration | Contract |
| --- | --- |
| `public readonly struct WindowOperationSupport : IEquatable<WindowOperationSupport>` | Backend support for an operation, independently of application policy. |
| `public WindowOperationSupport()` |  |
| `public WindowOperationSupport(bool IsSupported, NativeRestriction Restriction = NativeRestriction.None)` | Backend support for an operation, independently of application policy. |
| `public override string ToString()` |  |
| `public static bool operator !=(WindowOperationSupport left, WindowOperationSupport right)` |  |
| `public static bool operator ==(WindowOperationSupport left, WindowOperationSupport right)` |  |
| `public override int GetHashCode()` |  |
| `public override bool Equals(object obj)` |  |
| `public bool Equals(WindowOperationSupport other)` |  |
| `public void Deconstruct(out bool IsSupported, out NativeRestriction Restriction)` |  |
| `public bool IsSupported { get; init; }` |  |
| `public NativeRestriction Restriction { get; init; }` |  |

### `WindowOperations`

Namespace `Nazor`. [Source](../../Sources/Core/Nazor.Core/Windows/WindowCapabilities.cs)

| Declaration | Contract |
| --- | --- |
| `public readonly struct WindowOperations : IEquatable<WindowOperations>` | Independent requested operations; effective support is reported by WindowCapabilities.<br>`IsMinimizable`. The can minimize (bool).<br>`IsMaximizable`. The can maximize (bool).<br>`IsResizable`. The can resize (bool).<br>`FullScreenEnabled`. The can full screen (bool). |
| `public WindowOperations()` |  |
| `public WindowOperations(bool IsMinimizable, bool IsMaximizable, bool IsResizable, bool FullScreenEnabled)` | Independent requested operations; effective support is reported by WindowCapabilities.<br>`IsMinimizable`. The can minimize (bool).<br>`IsMaximizable`. The can maximize (bool).<br>`IsResizable`. The can resize (bool).<br>`FullScreenEnabled`. The can full screen (bool). |
| `public override string ToString()` |  |
| `public static bool operator !=(WindowOperations left, WindowOperations right)` |  |
| `public static bool operator ==(WindowOperations left, WindowOperations right)` |  |
| `public override int GetHashCode()` |  |
| `public override bool Equals(object obj)` |  |
| `public bool Equals(WindowOperations other)` |  |
| `public void Deconstruct(out bool IsMinimizable, out bool IsMaximizable, out bool IsResizable, out bool FullScreenEnabled)` |  |
| `public bool IsMinimizable { get; init; }` | The can minimize (bool). |
| `public bool IsMaximizable { get; init; }` | The can maximize (bool). |
| `public bool IsResizable { get; init; }` | The can resize (bool). |
| `public bool FullScreenEnabled { get; init; }` | The can full screen (bool). |

### `WindowState`

Namespace `Nazor`. [Source](../../Sources/Core/Nazor.Core/Windows/IWindow.cs)

| Declaration | Contract |
| --- | --- |
| `Normal = 0` | Shows the window at its normal restored bounds. |
| `Maximized = 1` | Expands the window to the screen work area. |
| `Minimized = 2` | Minimizes the window. |
| `FullScreen = 3` | Uses the full screen without normal window decorations. |

### `WindowTitleBarMenuMode`

Namespace `Nazor`. [Source](../../Sources/Core/Nazor.Core/Windows/WindowCommand.cs)

| Declaration | Contract |
| --- | --- |
| `public enum WindowTitleBarMenuMode` | The menu used for title bar clicks handled by Nazor. |
| `System = 0` | Uses the system menu when the backend can open it. |
| `Nazor = 1` | Opens a Nazor menu for clicks handled by the backend. |
| `None = 2` | Suppresses the menu for clicks handled by the backend. |

### `WindowsBackdrop`

Namespace `Nazor`. [Source](../../Sources/Core/Nazor.Core/Windows/Backdrop.cs)

| Declaration | Contract |
| --- | --- |
| `Mica = 0` | Requests the Windows Mica material. |
| `MicaAlt = 1` | Requests the Windows Mica Alt material. |
| `Acrylic = 2` | Requests the Windows Acrylic material. |

### `WindowsTitleBarHeight`

Namespace `Nazor`. [Source](../../Sources/Core/Nazor.Core/Windows/WindowCapabilities.cs)

| Declaration | Contract |
| --- | --- |
| `Standard = 0` | Uses the standard native caption-button height. |
| `Tall = 1` | Uses the taller native caption-button height. |

### `ChangeEventArgs`

Namespace `Nazor.Input`. [Source](../../Sources/Core/Nazor.Core/Input/EventArgs.cs)

| Declaration | Contract |
| --- | --- |
| `public ChangeEventArgs()` |  |

### `FocusEventArgs`

Namespace `Nazor.Input`. [Source](../../Sources/Core/Nazor.Core/Input/EventArgs.cs)

| Declaration | Contract |
| --- | --- |
| `public FocusEventArgs()` |  |
| `public ElementNode? RelatedTarget { get; init; }` | Gets or sets the other element involved in the focus transition, or null when unavailable. |

### `ImageErrorEventArgs`

Namespace `Nazor.Input`. [Source](../../Sources/Core/Nazor.Core/Input/EventArgs.cs)

| Declaration | Contract |
| --- | --- |
| `public ImageErrorEventArgs()` |  |

### `ImageLoadEventArgs`

Namespace `Nazor.Input`. [Source](../../Sources/Core/Nazor.Core/Input/EventArgs.cs)

| Declaration | Contract |
| --- | --- |
| `public ImageLoadEventArgs()` |  |

### `KeyBindings`

Namespace `Nazor.Input`. [Source](../../Sources/Core/Nazor.Core/Input/KeyBindings.cs)

| Declaration | Contract |
| --- | --- |
| `public KeyBindings()` |  |

### `KeyModifiers`

Namespace `Nazor.Input`. [Source](../../Sources/Core/Nazor.Core/Input/EventArgs.cs)

| Declaration | Contract |
| --- | --- |
| `public enum KeyModifiers` | Identifies the modifier keys held during input; values can be combined. |

### `KeyboardEventArgs`

Namespace `Nazor.Input`. [Source](../../Sources/Core/Nazor.Core/Input/EventArgs.cs)

| Declaration | Contract |
| --- | --- |
| `public KeyboardEventArgs()` |  |

### `MemoryClipboard`

Namespace `Nazor.Input`. [Source](../../Sources/Core/Nazor.Core/Input/Clipboard.cs)

| Declaration | Contract |
| --- | --- |
| `public MemoryClipboard()` |  |

### `PointerButton`

Namespace `Nazor.Input`. [Source](../../Sources/Core/Nazor.Core/Input/EventArgs.cs)

| Declaration | Contract |
| --- | --- |
| `None = 0` | Indicates that no pointer button is associated with the event. |
| `Primary = 1` | Identifies the platform's primary activation button. |
| `Secondary = 2` | Identifies the platform's secondary or context-menu button. |
| `Middle = 3` | Identifies the middle pointer button. |

### `PointerEventArgs`

Namespace `Nazor.Input`. [Source](../../Sources/Core/Nazor.Core/Input/EventArgs.cs)

| Declaration | Contract |
| --- | --- |
| `public PointerEventArgs()` |  |
| `public ElementNode? OriginalTarget { get; internal set; }` | First pointer dispatch target, retained during bubbling and explicit forwarding. Null before the renderer's first dispatch. Use it to preserve interactions inside a compound control. |

### `ResizeEventArgs`

Namespace `Nazor.Input`. [Source](../../Sources/Core/Nazor.Core/Input/EventArgs.cs)

| Declaration | Contract |
| --- | --- |
| `public class ResizeEventArgs : EventArgs` | An element listening to `onresize` was laid out with a new size or a new position within its parent element, as a `ResizeObserver` in the DOM. The first layout always reports. Values are logical units before pixel-grid snapping, unaffected by scrolling and transforms. Moving across pixel boundaries does not change a constant size. |
| `public ResizeEventArgs()` |  |
| `public double OffsetLeft { get; init; }` | Gets the horizontal distance of the border box from its parent element's border box. |
| `public double OffsetTop { get; init; }` | Gets the vertical distance of the border box from its parent element's border box. |
| `public double Width { get; init; }` | Gets the width of the border box. |
| `public double Height { get; init; }` | Gets the height of the border box. |
| `public double ContentWidth { get; init; }` | Gets the width of the content box, inside the padding and the border, as `contentRect` of a DOM `ResizeObserver`; layouts that place children inside the element use it. |
| `public double ContentHeight { get; init; }` | Gets the height of the content box, inside the padding and the border. |

### `ScrollDeltaUnit`

Namespace `Nazor.Input`. [Source](../../Sources/Core/Nazor.Core/Input/TextInputPolicy.cs)

| Declaration | Contract |
| --- | --- |
| `Notches = 0` | Expresses wheel movement in detents, scaled using the backend's scroll settings. |
| `LogicalPixels = 1` | Expresses precision scrolling directly in logical pixel distances. |

### `ScrollEventArgs`

Namespace `Nazor.Input`. [Source](../../Sources/Core/Nazor.Core/Input/EventArgs.cs)

| Declaration | Contract |
| --- | --- |
| `public ScrollEventArgs()` |  |
| `public ElementNode? OriginalTarget { get; internal set; }` | Gets the first routed scroll source. It remains unchanged through bubbling and forwarding, and is null before routing. |

### `TextInputPolicy`

Namespace `Nazor.Input`. [Source](../../Sources/Core/Nazor.Core/Input/TextInputPolicy.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed record TextInputPolicy : IEquatable<TextInputPolicy>` | Conventions for editing, without changing the physical modifiers delivered to the app.<br>`PrimaryModifier`. The primary modifier (KeyModifiers).<br>`WordModifier`. The word modifier (KeyModifiers).<br>`LineModifier`. The line modifier (KeyModifiers).<br>`HomeEndToDocument`. The home end to document (bool).<br>`RedoWithY`. The redo with y (bool). |
| `public TextInputPolicy(KeyModifiers PrimaryModifier, KeyModifiers WordModifier, KeyModifiers LineModifier = KeyModifiers.None, bool HomeEndToDocument = false, bool RedoWithY = true)` | Conventions for editing, without changing the physical modifiers delivered to the app.<br>`PrimaryModifier`. The primary modifier (KeyModifiers).<br>`WordModifier`. The word modifier (KeyModifiers).<br>`LineModifier`. The line modifier (KeyModifiers).<br>`HomeEndToDocument`. The home end to document (bool).<br>`RedoWithY`. The redo with y (bool). |
| `public override string ToString()` |  |
| `public static bool operator !=(TextInputPolicy? left, TextInputPolicy? right)` |  |
| `public static bool operator ==(TextInputPolicy? left, TextInputPolicy? right)` |  |
| `public override int GetHashCode()` |  |
| `public override bool Equals(object? obj)` |  |
| `public bool Equals(TextInputPolicy? other)` |  |
| `public void Deconstruct(out KeyModifiers PrimaryModifier, out KeyModifiers WordModifier, out KeyModifiers LineModifier, out bool HomeEndToDocument, out bool RedoWithY)` |  |
| `public KeyModifiers PrimaryModifier { get; init; }` | The primary modifier (KeyModifiers). |
| `public KeyModifiers WordModifier { get; init; }` | The word modifier (KeyModifiers). |
| `public KeyModifiers LineModifier { get; init; }` | The line modifier (KeyModifiers). |
| `public bool HomeEndToDocument { get; init; }` | The home end to document (bool). |
| `public bool RedoWithY { get; init; }` | The redo with y (bool). |

### `WheelEventArgs`

Namespace `Nazor.Input`. [Source](../../Sources/Core/Nazor.Core/Input/EventArgs.cs)

| Declaration | Contract |
| --- | --- |
| `public WheelEventArgs()` |  |
| `public bool Handled { get; set; }` | Set synchronously in a wheel callback to stop remaining callbacks, ancestor dispatch and default scrolling. Modified or unsupported gestures should remain unhandled. |

### `ElementStates`

Namespace `Nazor.Rendering`. [Source](../../Sources/Core/Nazor.Core/Rendering/Nodes.cs)

| Declaration | Contract |
| --- | --- |
| `public enum ElementStates` | Interaction states maintained by input and controls, observed by styling. |
| `WindowActive = 256` | The desktop presents this window as active. |
| `WindowInactive = 512` | The desktop presents this window as inactive. |
| `ValidationEligible = 2048` | Validation feedback is eligible after interaction or a form submission attempt; independent of validity. |
| `PopupAbove = 4096` | Presentation's final anchored-overlay side is above its anchor. Observed by NSS `:popup-above`; requested placement parameters are unchanged. |
| `PopupLeft = 8192` | Presentation's final anchored-overlay side is to the left of its anchor. Observed by NSS `:popup-left`; requested placement parameters are unchanged. |
| `PopupRight = 16384` | Presentation's final anchored-overlay side is to the right of its anchor. Observed by NSS `:popup-right`; requested placement parameters are unchanged. |
| `PopupOriginRight = 32768` | The arranged right edge is closer to the corresponding anchor edge than the left edge. Equal distances follow end alignment. NSS :popup-origin-right uses actual viewport/native-configured geometry. |
| `PopupOriginBottom = 65536` | The arranged bottom edge is closer to the corresponding anchor edge than the top edge. Equal distances follow end alignment. NSS :popup-origin-bottom combines with the horizontal flag for four reveal corners. |
| `Composing = 1024` | The shared text editor is displaying a nonempty IME preedit. Input updates this flag on composition start, cancellation, commit and synchronization; placeholder-state matching observes it. |

### `Node`

Namespace `Nazor.Rendering`. [Source](../../Sources/Core/Nazor.Core/Rendering/Nodes.cs)

| Declaration | Contract |
| --- | --- |
| `protected Node()` |  |

### `RenderTreeBuilder`

Namespace `Nazor.Rendering`. [Source](../../Sources/Core/Nazor.Core/Rendering/RenderTreeBuilder.cs)

| Declaration | Contract |
| --- | --- |
| `public RenderTreeBuilder()` |  |

### `RenderTreeFrame`

Namespace `Nazor.Rendering`. [Source](../../Sources/Core/Nazor.Core/Rendering/RenderTreeFrame.cs)

| Declaration | Contract |
| --- | --- |
| `public RenderTreeFrame()` |  |
| `public Action<ElementReference>? ElementReferenceCaptureAction { readonly get; internal set; }` | Gets the callback used to capture the rendered element reference. |
| `public int Sequence { readonly get; internal set; }` | Gets the source-order sequence number used to match frames across renders. |
| `public RenderTreeFrameType FrameType { readonly get; internal set; }` | Gets the kind of render frame represented by this value. |
| `public int SubtreeLength { readonly get; internal set; }` | For elements, components and regions: this frame plus all descendants. |
| `public string? Name { readonly get; internal set; }` | Element name or attribute name. |
| `public string? TextContent { readonly get; internal set; }` | Gets the text carried by a text frame. |
| `public object? AttributeValue { readonly get; internal set; }` | Gets the value carried by an attribute frame. |
| `public Type? ComponentType { readonly get; internal set; }` | Gets the component type instantiated by a component frame. |
| `public Type? DeclaringScope { readonly get; internal set; }` | The component whose markup declared this component frame; it scopes styles. |
| `public IComponent? DeclaringComponent { readonly get; internal set; }` | Gets the component instance whose markup declares the content. |
| `public object? Key { readonly get; internal set; }` | Gets the key used to retain identity across render updates. |
| `public Action<object>? ComponentReferenceCaptureAction { readonly get; internal set; }` | Gets the callback used to capture the instantiated component. |

### `RenderTreeFrameType`

Namespace `Nazor.Rendering`. [Source](../../Sources/Core/Nazor.Core/Rendering/RenderTreeFrame.cs)

| Declaration | Contract |
| --- | --- |
| `public enum RenderTreeFrameType` | Identifies the kind of data stored in a render-tree frame. |
| `None = 0` | An uninitialized or empty frame. |
| `Element = 1` | The opening frame of a retained element. |
| `Text = 2` | A frame containing text content. |
| `Attribute = 3` | An element attribute or component parameter frame. |
| `Component = 4` | The opening frame of a component instance. |
| `Region = 5` | A region with an independent sequence-number scope. |
| `ComponentReferenceCapture = 6` | A callback frame that captures a component instance. |
| `ElementReferenceCapture = 7` | A callback frame that captures an element reference. |

### `Renderer`

Namespace `Nazor.Rendering`. [Source](../../Sources/Core/Nazor.Core/Rendering/Renderer.cs)

| Declaration | Contract |
| --- | --- |
| `public RootComponent AddRootComponent<TComponent>(ParameterView parameters = default(ParameterView)) where TComponent : IComponent` | Instantiates and attaches a root component, then supplies its initial parameters.<br>`parameters`. The parameter names and values to supply to the component.<br>`TComponent`. The component type to instantiate or render.<br>**Returns**. The root component handle attached to the renderer. |
| `public RootComponent AddRootComponent(Type componentType, ParameterView parameters = default(ParameterView))` | Instantiates and attaches a root component, then supplies its initial parameters.<br>`componentType`. The component type whose scoped styles or render output are being requested.<br>`parameters`. The parameter names and values to supply to the component.<br>**Returns**. The root component handle attached to the renderer. |

### `TextEditorRoles`

Namespace `Nazor.Rendering`. [Source](../../Sources/Core/Nazor.Core/Rendering/TextEditorRoles.cs)

| Declaration | Contract |
| --- | --- |
| `public static class TextEditorRoles` | Identifies the elements edited by the shared text editor: a `textbox`, or a `combobox`, `spinbutton` or `searchbox` that carries a string `value` attribute, so editable combo boxes and number fields keep their own accessible role. |
| `public static bool IsEditor(ElementNode element)` | Gets whether the element is edited as text.<br>`element`. The element to inspect.<br>**Returns**. true for text editors. |

### `RouteMatch`

Namespace `Nazor.Routing`. [Source](../../Sources/Core/Nazor.Core/Navigation/Routing.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed record RouteMatch : IEquatable<RouteMatch>` | A page found for an address, with the values of its route parameters (converted to their constraint).<br>`Page`. The page (Type).<br>`Template`. The template (string).<br>`Parameters`. The parameters (IReadOnlyDictionary<string, object?>). |
| `public RouteMatch(Type Page, string Template, IReadOnlyDictionary<string, object?> Parameters)` | A page found for an address, with the values of its route parameters (converted to their constraint).<br>`Page`. The page (Type).<br>`Template`. The template (string).<br>`Parameters`. The parameters (IReadOnlyDictionary<string, object?>). |
| `public override string ToString()` |  |
| `public static bool operator !=(RouteMatch? left, RouteMatch? right)` |  |
| `public static bool operator ==(RouteMatch? left, RouteMatch? right)` |  |
| `public override int GetHashCode()` |  |
| `public override bool Equals(object? obj)` |  |
| `public bool Equals(RouteMatch? other)` |  |
| `public void Deconstruct(out Type Page, out string Template, out IReadOnlyDictionary<string, object?> Parameters)` |  |
| `public Type Page { get; init; }` | The page (Type). |
| `public string Template { get; init; }` | The template (string). |
| `public IReadOnlyDictionary<string, object?> Parameters { get; init; }` | The parameters (IReadOnlyDictionary<string, object?>). |


<a id="type-componentbase"></a>

### `ComponentBase`

Namespace `Nazor`.

Base class for components written in .nazor files. Lifecycle and re-render rules follow Blazor's ComponentBase: events handled by the component and parameter updates re-render it; changes made elsewhere (timers, services) must call `Nazor.ComponentBase.StateHasChanged` through `Nazor.ComponentBase.InvokeAsync(System.Action)`.

| Declaration | Contract |
| --- | --- |
| `public abstract class ComponentBase : IComponent, IHandleEvent, IHandleAfterRender` | Base class for components written in .nazor files. Lifecycle and re-render rules follow Blazor's ComponentBase: events handled by the component and parameter updates re-render it; changes made elsewhere (timers, services) must call `Nazor.ComponentBase.StateHasChanged` through `Nazor.ComponentBase.InvokeAsync(System.Action)`. |
| `protected void SetWindowTitleBar(RenderFragment? content)` | Registers a window-owned caption declaration; null removes it during disposal. |
| `protected void SetWindowCaptionButtons(RenderFragment? left, RenderFragment? right)` | Registers window-owned physical left and right caption-button fragments; null fragments remove the declaration. |
| `protected void OpenDelegatedComponent<TComponent>(RenderTreeBuilder builder, int sequence) where TComponent : notnull, IComponent` | Opens a delegated component with this wrapper's original declaring markup scope and instance. |
| `protected ComponentBase()` | Initializes a new instance of ComponentBase. |
| `protected virtual void BuildRenderTree(RenderTreeBuilder builder)` | Writes the render frames for ComponentBase to the supplied builder. |
| `protected virtual void OnInitialized()` | Initializes component state before the first parameter update completes. |
| `protected virtual Task OnInitializedAsync()` | Asynchronously initializes component state before the first parameter update completes. A task that completes when the operation has finished. |
| `protected virtual void OnParametersSet()` | Runs after every parameter assignment, including the first. After a .NET Hot Reload delta every component receives its parameters again, so this runs once more per delta. |
| `protected virtual Task OnParametersSetAsync()` | Asynchronous counterpart of `Nazor.ComponentBase.OnParametersSet`. After a hot reload delta it runs again and may overlap with a previous invocation still in progress, as in Blazor. A task that completes when the operation has finished. |
| `protected virtual bool ShouldRender()` | Whether a `Nazor.ComponentBase.StateHasChanged` renders; ignored for the first render and while a hot reload delta is applied. true when the operation succeeds or the condition is satisfied; otherwise, false. |
| `protected virtual void OnAfterRender(bool firstRender)` | Notifies the component after its rendered output has been applied. |
| `protected virtual Task OnAfterRenderAsync(bool firstRender)` | Asynchronously notifies the component after its rendered output has been applied. A task that completes when the operation has finished. |
| `protected void StateHasChanged()` | Queues a render unless one is already pending. Must run on the dispatcher. |
| `protected Task InvokeAsync(Action workItem)` | Schedules the supplied work on the UI dispatcher and observes its completion. A task that completes when the operation has finished. |
| `protected Task InvokeAsync(Func<Task> workItem)` | Schedules the supplied work on the UI dispatcher and observes its completion. A task that completes when the operation has finished. |
| `public virtual Task SetParametersAsync(ParameterView parameters)` | Assigns component parameters and runs the initialization and parameter lifecycle callbacks. A task that completes when the operation has finished. |

### `ControlDensity`

Namespace `Nazor`. [Source](../../Sources/Core/Nazor.Core/Windows/ControlDensity.cs)

| Declaration | Contract |
| --- | --- |
| `public enum ControlDensity` | Chooses theme spacing and interaction target sizes independently of window width. |
| `Automatic = 0` | Inherits the application choice, then the host's touch-input preference. An unavailable preference falls back to compact. |
| `Compact = 1` | Uses the theme's compact desktop dimensions. |
| `Touch = 2` | Uses the theme's larger touch targets, text and row spacing. |

<a id="type-iwindow"></a>

### `IWindow`

Namespace `Nazor`.

A window of the application: the one a component lives in ([Inject] IWindow), or one opened with IWindowService.

| Declaration | Contract |
| --- | --- |
| `public interface IWindow` | A window of the application: the one a component lives in ([Inject] IWindow), or one opened with IWindowService. |
| `NativeWindowHandle? NativeHandle { get; }` | Borrowed operating-system identifier on the UI dispatcher; null before creation, after close, or when unsupported. Do not destroy or release it. See [native interop](../Guide/13-Windows-and-Dialogs.md#native-window-interop). |
| `ValueTask<WindowDecorationResult> SetDecorationsAsync(WindowDecorationPolicy policy, CancellationToken cancellationToken = default(CancellationToken))` | Applies a complete policy on the UI thread without replacing the native window. The accepted configuration. Throws PlatformNotSupportedException when every option in the policy is unsupported. |
| `void Show(bool activate = true)` | Shows a hidden window, preserving its components and services. |
| `void Hide()` | Hides without closing or ending the application. |
| `bool CanExecute(WindowCommand command)` | Whether the command can be sent with the current policy, state and backend support. |
| `WindowCommandResult Request(WindowCommand command)` | Checks and dispatches a command without implying that the desktop applied it. |
| `void Activate()` | Brings the window to the front and gives it the keyboard focus. |
| `void Minimize()` | Requests that the window enter the minimized state. |
| `void Maximize()` | Requests that the window enter the maximized state. |
| `void EnterFullScreen()` | Requests native full screen; the backend reports the actual state. |
| `void ExitFullScreen()` | Restore the bounds and normal/maximized state saved before full screen. Otherwise no effect. |
| `void Restore()` | Exit full screen to the saved state, or restore normally from maximized/minimized. |
| `void Close()` | Closes the window. |
| `void Close(object? result)` | Closes the window; a dialog completes its ShowDialogAsync with `result`. |
| `WindowDecorationPolicy Decorations { get; set; }` | Opening decoration policy. Change an open window with SetDecorationsAsync instead. |
| `WindowDecorationResult? AppliedDecorations { get; }` | The accepted configuration and selected alternative, or null while opening negotiation is pending. |
| `string? Class { get; set; }` | Space-separated NSS classes on the implicit window root. |
| `string? Style { get; set; }` | Inline NSS declarations on the implicit window root. |
| `ControlDensity ControlDensity { get; set; }` | Window-local density override. Automatic inherits the application choice and then host input preference. Explicit compact/touch choices survive later application/host changes. Set on the UI dispatcher. The default interface setter throws NotSupportedException. |
| `double Width { get; set; }` | Requested logical client width, or the normal width to restore in a non-normal state. |
| `double Height { get; set; }` | Requested logical client height, or the normal height to restore in a non-normal state. |
| `double? MinWidth { get; set; }` | Minimum logical client width; null uses the system minimum. |
| `double? MinHeight { get; set; }` | Minimum logical client height; null uses the system minimum. |
| `double? MaxWidth { get; set; }` | Maximum logical client width, including when maximized; null removes the limit. |
| `double? MaxHeight { get; set; }` | Maximum logical client height, including when maximized; null removes the limit. |
| `int? X { get; set; }` | Outer window left edge in screen pixels; null before creation lets the system position it. Non-normal states update the restore position. Unsupported backends throw on assignment. |
| `int? Y { get; set; }` | Outer window top edge in screen pixels; null before creation lets the system position it. Non-normal states update the restore position. Unsupported backends throw on assignment. |
| `bool IsAlwaysOnTop { get; set; }` | Requests a position above non-topmost windows without changing focus. |
| `BackgroundMode BackgroundMode { get; set; }` | Opening setting for the client surface. Transparent mode takes precedence over the system backdrop. |
| `bool IsVisibleInTaskbar { get; set; }` | Requests taskbar participation; hiding may also exclude the window from the task switcher. |
| `BackdropSelection Backdrop { get; set; }` | Requested portable or platform-specific backdrop selection. |
| `BackdropResult AppliedBackdrop { get; }` | The backdrop accepted by the backend. |
| `IStyleSheet? StyleSheet { get; set; }` | Window-local stylesheet, independent of application and component styles. |
| `string? PlacementKey { get; set; }` | Placement persistence key; may only be changed before native creation. |
| `ImageSource? EffectiveIcon { get; }` | The explicit icon or the application icon when its source can be resolved. |
| `string Title { get; set; }` | The native title; changing it also invalidates the declared window's presentation. |
| `ImageSource? Icon { get; set; }` | The requested native window icon; null restores the application icon. See WindowCapabilities for support. |
| `bool IsClosed { get; }` | True once the window is closed: its components are disposed. |
| `WindowState State { get; }` | Normal, maximized, minimized or full screen. |
| `WindowEnvironment Environment { get; }` | Safe areas and keyboard occlusion not already removed from the client viewport. |
| `WindowOperations Operations { get; set; }` | The application's rules for window commands and interactive resize. |
| `WindowTitleBarMenuMode TitleBarMenuMode { get; set; }` | The menu used for title bar clicks that the backend can handle. |
| `WindowChrome Chrome { get; }` | The effective owner of the frame, its buttons and its context menu. |
| `bool IsMinimizable { get; set; }` | Whether the app allows minimization. |
| `bool IsMaximizable { get; set; }` | Gets or sets whether the application allows the window to be maximized. |
| `bool IsResizable { get; set; }` | Gets or sets whether the application allows interactive window resizing. |
| `bool FullScreenEnabled { get; set; }` | Gets or sets whether the application allows entry into full screen. |
| `WindowCapabilities Capabilities { get; }` | Operations allowed by the application and backend; unsupported decorations are false. |
| `bool AllowPopupsOutsideWindow { get; set; }` | Gets or sets whether anchored popups may extend beyond the window. Defaults to false. The UI dispatcher applies changes to open popups on the next frame. Backends without `Nazor.WindowCapabilities.CanShowPopupsOutsideWindow` retain the in-window overlay. |
| `WindowButtonsLayout SystemButtonsLayout { get; }` | System control placement for custom title bars, available even when native controls are hidden. |
| `WindowButtonsMetrics SystemButtonsMetrics { get; }` | The native caption-button area in logical units; zero when hidden or unsupported. |
| `(float Width, float Height) SystemButtonsSize { get; }` | Gets the total logical width and height reserved for integrated native caption controls. |
| `event Action? Changed` | Raised on the UI thread when presentation, state, or capabilities change. |
| `event Action? EnvironmentChanged` | Occurs when environment changes. |
| `event Action? ChromeChanged` | Raised when the effective frame, caption buttons or title bar menu changes owner. |
| `event Action? CapabilitiesChanged` | Raised on the UI thread after effective operations or decoration support changes. |
| `event Action? SystemButtonsChanged` | Raised on the UI thread after system control metrics or layout changed. |
| `event Action? StateChanged` | Raised on the UI thread after `Nazor.IWindow.State` changed (by the user or by the code). |
| `event Action? Closed` | Raised on the UI thread after the window closed and its components were disposed. |

<a id="type-windowcapabilities"></a>

### `WindowCapabilities`

Namespace `Nazor`.

Effective operations and native decoration support of this window's backend.

| Declaration | Contract |
| --- | --- |
| `public readonly struct WindowCapabilities : IEquatable<WindowCapabilities>` | Effective operations and native decoration support of this window's backend. |
| `public WindowCapabilities()` | Creates a new instance. |
| `public WindowCapabilities(bool CanMinimize, bool CanMaximize, bool CanResize, bool CanEnterFullScreen)` | Effective operations and native decoration support of this window's backend. |
| `public override string ToString()` | Standard .NET value or component semantics. |
| `public static bool operator !=(WindowCapabilities left, WindowCapabilities right)` | Standard .NET value or component semantics. |
| `public static bool operator ==(WindowCapabilities left, WindowCapabilities right)` | Standard .NET value or component semantics. |
| `public override int GetHashCode()` | Standard .NET value or component semantics. |
| `public override bool Equals(object obj)` | Standard .NET value or component semantics. |
| `public bool Equals(WindowCapabilities other)` | Standard .NET value or component semantics. |
| `public void Deconstruct(out bool CanMinimize, out bool CanMaximize, out bool CanResize, out bool CanEnterFullScreen)` | Standard .NET value or component semantics. |
| `public bool CanMinimize { get; init; }` | The can minimize (bool). |
| `public bool CanMaximize { get; init; }` | The can maximize (bool). |
| `public bool CanResize { get; init; }` | The can resize (bool). |
| `public bool CanEnterFullScreen { get; init; }` | The can full screen (bool). |
| `public WindowDecorationCapabilities Decorations { get; init; }` | Complete decoration support, independent of application templates and window-operation restrictions. |
| `public WindowOperationSupport Minimize { get; init; }` | Whether and how the backend supports minimization, independently of app policy. |
| `public WindowOperationSupport Maximize { get; init; }` | Whether and how the backend supports maximization, independently of app policy. |
| `public WindowOperationSupport InteractiveResize { get; init; }` | Whether and how the backend supports interactive resizing, independently of app policy. |
| `public WindowOperationSupport FullScreen { get; init; }` | Whether and how the backend supports fullscreen, independently of app policy. |
| `public bool CanObserveMinimized { get; init; }` | Whether the backend can report that a window is minimized. |
| `public bool CanRestoreFromMinimized { get; init; }` | Whether the backend can restore a minimized window by request. |
| `public bool CanSetScreenPosition { get; init; }` | Whether the backend supports absolute screen positioning. |
| `public bool CanSetWindowIcon { get; init; }` | Whether the backend can apply an icon to an individual native window. |
| `public bool CanSetAlwaysOnTop { get; init; }` | Whether the backend can keep a window above non-topmost windows. |
| `public bool CanUseTransparentBackground { get; init; }` | Whether the backend supports a client surface with per-pixel alpha. |
| `public bool CanShowPopupsOutsideWindow { get; init; }` | Gets whether the backend can show anchored popups outside this window. |
| `public bool CanSetTaskbarVisibility { get; init; }` | Whether the backend can change a window's taskbar participation. |

<a id="type-windowchromeowner"></a>

### `WindowChromeOwner`

Namespace `Nazor`.

The owner of part of a window's decoration.

| Declaration | Contract |
| --- | --- |
| `public enum WindowChromeOwner` | The owner of part of a window's decoration. |
| `None = 0` | The backend does not provide this part. |
| `System = 1` | The desktop or native window API provides this part. |
| `Nazor = 2` | The framework provides this part. |
| `User = 3` | The application provides the selected caption content. |
| `Unknown = 4` | The backend has not confirmed the owner. |

<a id="type-windowchrome"></a>

### `WindowChrome`

Namespace `Nazor`.

The actual owner of the caption, caption buttons and title bar context menu.

| Declaration | Contract |
| --- | --- |
| `public readonly struct WindowChrome : IEquatable<WindowChrome>` | The actual owner of the caption, caption buttons and title bar context menu. |
| `public WindowChrome()` | Creates a new instance. |
| `public WindowChrome(WindowChromeOwner Frame, WindowChromeOwner CaptionButtons, WindowChromeOwner TitleBarMenu)` | The actual owner of the caption, caption buttons and title bar context menu. |
| `public override string ToString()` | Standard .NET value or component semantics. |
| `public static bool operator !=(WindowChrome left, WindowChrome right)` | Standard .NET value or component semantics. |
| `public static bool operator ==(WindowChrome left, WindowChrome right)` | Standard .NET value or component semantics. |
| `public override int GetHashCode()` | Standard .NET value or component semantics. |
| `public override bool Equals(object obj)` | Standard .NET value or component semantics. |
| `public bool Equals(WindowChrome other)` | Standard .NET value or component semantics. |
| `public void Deconstruct(out WindowChromeOwner Frame, out WindowChromeOwner CaptionButtons, out WindowChromeOwner TitleBarMenu)` | Standard .NET value or component semantics. |
| `public WindowChromeOwner Frame { get; init; }` | Standard .NET value or component semantics. |
| `public WindowChromeOwner CaptionButtons { get; init; }` | Standard .NET value or component semantics. |
| `public WindowChromeOwner TitleBarMenu { get; init; }` | Standard .NET value or component semantics. |
| `public WindowChromeOwner TitleBar { get; init; }` | The actual title-bar owner, independently of the external frame and buttons. |

<a id="type-windowcomponentbase"></a>

### `WindowComponentBase`

Namespace `Nazor`.

Base generated for a component declared with @window; hosts instantiate it as a root.

| Declaration | Contract |
| --- | --- |
| `public abstract class WindowComponentBase : ComponentBase` | Base generated for a component declared with @window; hosts instantiate it as a root. |
| `protected virtual void BuildWindowContent(RenderTreeBuilder builder)` | Writes application content generated from the window's Nazor markup. |
| `protected override sealed void BuildRenderTree(RenderTreeBuilder builder)` | Renders a stable implicit window root, a fixed caption destination and a WindowContent viewport with automatic overflow scrolling by default. Override overflow on WindowContent through NSS. |
| `protected WindowComponentBase()` | Creates a new instance. |
| `public IWindow Window { get; set; }` | The native window associated before parameter assignment and initialization. |

<a id="type-windowsupportstatus"></a>

### `WindowSupportStatus`

Namespace `Nazor`.

Support known without changing the native window.

| Declaration | Contract |
| --- | --- |
| `public enum WindowSupportStatus` | Support known without changing the native window. |
| `Unknown = 0` | Negotiation or additional observations are required. |
| `Unsupported = 1` | The environment cannot satisfy the request. |
| `Supported = 2` | The backend supports the request under the currently known conditions. |

<a id="type-windowdecorationsupport"></a>

### `WindowDecorationSupport`

Namespace `Nazor`.

The support of a complete configuration, with a diagnostic explanation.

| Declaration | Contract |
| --- | --- |
| `public readonly struct WindowDecorationSupport : IEquatable<WindowDecorationSupport>` | The support of a complete configuration, with a diagnostic explanation. |
| `public WindowDecorationSupport()` | Creates a new instance. |
| `public WindowDecorationSupport(WindowSupportStatus Status, string? Reason = null)` | The support of a complete configuration, with a diagnostic explanation. |
| `public override string ToString()` | Standard .NET value or component semantics. |
| `public static bool operator !=(WindowDecorationSupport left, WindowDecorationSupport right)` | Standard .NET value or component semantics. |
| `public static bool operator ==(WindowDecorationSupport left, WindowDecorationSupport right)` | Standard .NET value or component semantics. |
| `public override int GetHashCode()` | Standard .NET value or component semantics. |
| `public override bool Equals(object obj)` | Standard .NET value or component semantics. |
| `public bool Equals(WindowDecorationSupport other)` | Standard .NET value or component semantics. |
| `public void Deconstruct(out WindowSupportStatus Status, out string? Reason)` | Standard .NET value or component semantics. |
| `public WindowSupportStatus Status { get; init; }` | The currently known support. |
| `public string? Reason { get; init; }` | A diagnostic message, or null when supported. |

<a id="type-windowdecorationcapabilities"></a>

### `WindowDecorationCapabilities`

Namespace `Nazor`.

Immutable backend decoration capabilities, independent of application templates and operation policy.

| Declaration | Contract |
| --- | --- |
| `public sealed record WindowDecorationCapabilities : IEquatable<WindowDecorationCapabilities>` | Immutable backend decoration capabilities, independent of application templates and operation policy. |
| `public WindowDecorationSupport GetSupport(WindowDecorations configuration)` | Checks a complete configuration without changing the window or validating application fragments. Supported, unsupported or unknown with a diagnostic reason. |
| `public WindowDecorationSupport GetChangeSupport(WindowDecorations configuration)` | Checks the complete transition from Current without changing the native window. The configuration and transition support. |
| `public override string ToString()` | Standard .NET value or component semantics. |
| `public static bool operator !=(WindowDecorationCapabilities? left, WindowDecorationCapabilities? right)` | Standard .NET value or component semantics. |
| `public static bool operator ==(WindowDecorationCapabilities? left, WindowDecorationCapabilities? right)` | Standard .NET value or component semantics. |
| `public override int GetHashCode()` | Standard .NET value or component semantics. |
| `public override bool Equals(object? obj)` | Standard .NET value or component semantics. |
| `public bool Equals(WindowDecorationCapabilities? other)` | Standard .NET value or component semantics. |
| `public WindowDecorationCapabilities()` | Creates a new instance. |
| `public static WindowDecorationCapabilities None { get; }` | A backend with no known desktop-decoration support. |
| `public WindowSupportStatus NativeFrame { get; init; }` | Support for an external native frame. |
| `public WindowSupportStatus NazorFrame { get; init; }` | Support for a frame drawn by Nazor. |
| `public WindowSupportStatus NativeTitleBar { get; init; }` | Support for the original system title bar. |
| `public WindowSupportStatus ClientTitleBarOnNativeFrame { get; init; }` | Support for client title-bar content while retaining a native external frame. |
| `public WindowSupportStatus IntegratedNativeCaptionButtons { get; init; }` | Support for original native caption controls inside a client title bar on a native frame. |
| `public WindowSupportStatus NativeCaptionButtonsOnNazorFrame { get; init; }` | Support for native controls without retaining a native external frame. |
| `public WindowSupportStatus FrameChanges { get; init; }` | Whether the backend can change frame ownership without replacing the native window. |
| `public WindowSupportStatus CaptionChanges { get; init; }` | Whether the backend can change the caption configuration on the current frame. |
| `public WindowDecorations? Current { get; init; }` | The accepted configuration used when evaluating a runtime transition. |

<a id="type-windowdecorationcontext"></a>

### `WindowDecorationContext`

Namespace `Nazor`.

Context supplied to a statically registered theme decoration presenter.

| Declaration | Contract |
| --- | --- |
| `public sealed record WindowDecorationContext : IEquatable<WindowDecorationContext>` | Context supplied to a statically registered theme decoration presenter. |
| `public WindowDecorationContext(IWindow Window, WindowComponentBase? Owner, bool IsLeft = false)` | Context supplied to a statically registered theme decoration presenter. |
| `public void OpenComponent<TComponent>(RenderTreeBuilder builder, int sequence) where TComponent : notnull, IComponent` | Opens a presenter component in the owning window's declaring scope, preserving local surface styling. |
| `public override string ToString()` | Standard .NET value or component semantics. |
| `public static bool operator !=(WindowDecorationContext? left, WindowDecorationContext? right)` | Standard .NET value or component semantics. |
| `public static bool operator ==(WindowDecorationContext? left, WindowDecorationContext? right)` | Standard .NET value or component semantics. |
| `public override int GetHashCode()` | Standard .NET value or component semantics. |
| `public override bool Equals(object? obj)` | Standard .NET value or component semantics. |
| `public bool Equals(WindowDecorationContext? other)` | Standard .NET value or component semantics. |
| `public void Deconstruct(out IWindow Window, out WindowComponentBase? Owner, out bool IsLeft)` | Standard .NET value or component semantics. |
| `public IWindow Window { get; init; }` | The owning window. |
| `public WindowComponentBase? Owner { get; init; }` | The declaring window component, when present. |
| `public bool IsLeft { get; init; }` | Whether a button presenter renders the physical left-side group. |

<a id="type-windowdecorationpresenters"></a>

### `WindowDecorationPresenters`

Namespace `Nazor`.

Optional theme composition, separate from NSS rules and window-decoration policy. Presenters render requested content only. They must not change decoration policy or select native controls based on operating-system identity.

| Declaration | Contract |
| --- | --- |
| `public sealed record WindowDecorationPresenters : IEquatable<WindowDecorationPresenters>` | Optional theme composition, separate from NSS rules and window-decoration policy. Presenters render requested content only. They must not change decoration policy or select native controls based on operating-system identity. |
| `public WindowDecorationPresenters(RenderFragment<WindowDecorationContext>? TitleBar = null, RenderFragment<WindowDecorationContext>? CaptionButtons = null)` | Optional theme composition, separate from NSS rules and window-decoration policy. Presenters render requested content only. They must not change decoration policy or select native controls based on operating-system identity. |
| `public override string ToString()` | Standard .NET value or component semantics. |
| `public static bool operator !=(WindowDecorationPresenters? left, WindowDecorationPresenters? right)` | Standard .NET value or component semantics. |
| `public static bool operator ==(WindowDecorationPresenters? left, WindowDecorationPresenters? right)` | Standard .NET value or component semantics. |
| `public override int GetHashCode()` | Standard .NET value or component semantics. |
| `public override bool Equals(object? obj)` | Standard .NET value or component semantics. |
| `public bool Equals(WindowDecorationPresenters? other)` | Standard .NET value or component semantics. |
| `public void Deconstruct(out RenderFragment<WindowDecorationContext>? TitleBar, out RenderFragment<WindowDecorationContext>? CaptionButtons)` | Standard .NET value or component semantics. |
| `public RenderFragment<WindowDecorationContext>? TitleBar { get; init; }` | The theme's title-bar surface; null uses the common presenter. |
| `public RenderFragment<WindowDecorationContext>? CaptionButtons { get; init; }` | The theme's physical-side caption group; null uses the common presenter. |

<a id="type-windowframeowner"></a>

### `WindowFrameOwner`

Namespace `Nazor`.

The provider of the external window frame.

| Declaration | Contract |
| --- | --- |
| `public enum WindowFrameOwner` | The provider of the external window frame. |
| `Native = 0` | The desktop provides the external frame. |
| `Nazor = 1` | Nazor draws the external frame using root NSS properties. |

<a id="type-windowtitlebarcontent"></a>

### `WindowTitleBarContent`

Namespace `Nazor`.

The content supplied inside a client-rendered title bar.

| Declaration | Contract |
| --- | --- |
| `public enum WindowTitleBarContent` | The content supplied inside a client-rendered title bar. |
| `None = 0` | No title bar is requested. |
| `Nazor = 1` | The active theme supplies the standard title bar. |
| `User = 2` | The window's UserTitleBar declaration supplies the content. |

<a id="type-windowcaptionbuttons"></a>

### `WindowCaptionButtons`

Namespace `Nazor`.

The explicitly requested caption-button provider.

| Declaration | Contract |
| --- | --- |
| `public enum WindowCaptionButtons` | The explicitly requested caption-button provider. |
| `None = 0` | No caption-button group or occupied area is requested. |
| `Native = 1` | The desktop supplies the controls; Nazor must not imitate them. |
| `Nazor = 2` | The active theme supplies the standard controls. |
| `User = 3` | The window's UserCaptionButtons declaration supplies the controls. |

<a id="type-windowdecorations"></a>

### `WindowDecorations`

Namespace `Nazor`.

An immutable, complete window-decoration request. Backend support is checked separately.

| Declaration | Contract |
| --- | --- |
| `public sealed record WindowDecorations : IEquatable<WindowDecorations>` | An immutable, complete window-decoration request. Backend support is checked separately. |
| `public static WindowDecorations Native()` | Requests the original desktop frame, title bar and caption controls as one unit. The complete native configuration. |
| `public static WindowDecorations Native(WindowTitleBarContent titleBar, WindowCaptionButtons buttons)` | Requests a native external frame with client title-bar content and an explicit button provider. The complete integration request; its availability depends on the backend. |
| `public static WindowDecorations Nazor(WindowTitleBarContent titleBar = WindowTitleBarContent.Nazor, WindowCaptionButtons buttons = WindowCaptionButtons.Nazor)` | Requests a Nazor-drawn frame with explicit title-bar content and caption controls. The complete client-frame configuration. |
| `public override string ToString()` | Standard .NET value or component semantics. |
| `public static bool operator !=(WindowDecorations? left, WindowDecorations? right)` | Standard .NET value or component semantics. |
| `public static bool operator ==(WindowDecorations? left, WindowDecorations? right)` | Standard .NET value or component semantics. |
| `public override int GetHashCode()` | Standard .NET value or component semantics. |
| `public override bool Equals(object? obj)` | Standard .NET value or component semantics. |
| `public bool Equals(WindowDecorations? other)` | Standard .NET value or component semantics. |
| `public WindowFrameOwner Frame { get; }` | The requested owner of the external frame. |
| `public bool HasNativeTitleBar { get; }` | Whether the desktop supplies the original title bar as well as its controls. |
| `public WindowTitleBarContent TitleBar { get; }` | The client title-bar content; None for an original native title bar. |
| `public WindowCaptionButtons CaptionButtons { get; }` | The requested caption-button provider. |

<a id="type-windowdecorationpolicy"></a>

### `WindowDecorationPolicy`

Namespace `Nazor`.

An immutable ordered list of complete configurations, including desktop defaults for preferences.

| Declaration | Contract |
| --- | --- |
| `public sealed class WindowDecorationPolicy` | An immutable ordered list of complete configurations, including desktop defaults for preferences. |
| `public static WindowDecorationPolicy Require(WindowDecorations configuration)` | Requires one exact configuration, with no implicit substitution. The strict policy. |
| `public static WindowDecorationPolicy Prefer(WindowDecorations primary, params WindowDecorations[] fallbacks)` | Tries the supplied complete configurations in order, then complete native and Nazor decorations, keeping only the first occurrence of each configuration. The ordered policy including desktop defaults. Unsupported configurations fall back; invalid application content still fails. |
| `public static WindowDecorationPolicy Default { get; }` | The default desktop policy: complete native decorations, then complete Nazor decorations. |
| `public WindowDecorations Primary { get; }` | The primary requested configuration. |
| `public IReadOnlyList<WindowDecorations> Options { get; }` | All authorized configurations, in priority order; the list is an immutable snapshot. |

<a id="type-windowdecorationresult"></a>

### `WindowDecorationResult`

Namespace `Nazor`.

A configuration accepted by a backend, distinct from temporary fullscreen visibility.

| Declaration | Contract |
| --- | --- |
| `public sealed record WindowDecorationResult : IEquatable<WindowDecorationResult>` | A configuration accepted by a backend, distinct from temporary fullscreen visibility. |
| `public WindowDecorationResult(WindowDecorations Configuration, int OptionIndex)` | A configuration accepted by a backend, distinct from temporary fullscreen visibility. |
| `public override string ToString()` | Standard .NET value or component semantics. |
| `public static bool operator !=(WindowDecorationResult? left, WindowDecorationResult? right)` | Standard .NET value or component semantics. |
| `public static bool operator ==(WindowDecorationResult? left, WindowDecorationResult? right)` | Standard .NET value or component semantics. |
| `public override int GetHashCode()` | Standard .NET value or component semantics. |
| `public override bool Equals(object? obj)` | Standard .NET value or component semantics. |
| `public bool Equals(WindowDecorationResult? other)` | Standard .NET value or component semantics. |
| `public void Deconstruct(out WindowDecorations Configuration, out int OptionIndex)` | Standard .NET value or component semantics. |
| `public WindowDecorations Configuration { get; init; }` | The accepted complete configuration. |
| `public int OptionIndex { get; init; }` | Zero for the primary option; higher values identify an explicit or default fallback in the policy's Options. |

See [title bars and platform integration](../Guide/14-Title-Bars-and-Platform-Integration.md) for complete configuration policies, capability checks, explicit fallback and User content lifecycle.


<a id="type-customstateset"></a>

## `CustomStateSet`

Namespace: `Nazor.Rendering`

| Declaration | Contract |
| --- | --- |
| `public sealed class CustomStateSet : IEquatable<CustomStateSet>` | An immutable ordinal state snapshot, published through the custom-states attribute. |
| `public CustomStateSet(IEnumerable<string> names)` | Copy, validate, deduplicate and sort names. Null input throws ArgumentNullException; invalid names throw ArgumentException. |
| `public bool Contains(string name)` | Test a published name using ordinal case-sensitive comparison. |
| `public CustomStateSet With(string name, bool active = true)` | Return a snapshot with the named state added/removed; return this snapshot if unchanged. Invalid names throw ArgumentException. |
| `public static bool IsValidName(string? name)` | True for a letter/underscore followed by letters, digits, underscores or hyphens; false for null/empty or other forms. |
| `public bool Equals(CustomStateSet? other)` | Compare immutable values of the same type. |
| `public override bool Equals(object? obj)` | Compare immutable values of the same type. |
| `public override int GetHashCode()` | Hash the value consistently with equality. |
| `public override string ToString()` | Return the published state names separated by spaces. |
| `public static CustomStateSet Empty { get; }` | The shared empty immutable declaration. |
| `public IReadOnlyList<string> Names { get; }` | The sorted distinct immutable state names. |

<a id="type-validationfeedback"></a>

## `ValidationFeedback`

Namespace: `Nazor.Rendering`

| Declaration | Contract |
| --- | --- |
| `public static class ValidationFeedback` | Explicit interaction/submission eligibility for validation styles; validity remains application-owned. |
| `public static bool IsEligible(ElementNode element)` | Test the element, nearest Field and nearest Form eligibility; nested forms isolate submission state. Null throws ArgumentNullException. |
| `public static void MarkInteracted(ElementNode element)` | Enable eligibility on an enabled retained element and its nearest Field. Removed/disabled targets are ignored; null throws. |
| `public static void Submit(ElementNode form)` | Enable nearest-form feedback before submission. Does not validate. Throws for null or a root without role=form. |
| `public static void Reset(ElementNode root)` | Clear eligibility in a root and its descendants, excluding nested forms; values and validity stay application-owned. Null throws. |

<a id="type-logicalitemposition"></a>

## `LogicalItemPosition`

Namespace: `Nazor.Rendering`

| Declaration | Contract |
| --- | --- |
| `public readonly struct LogicalItemPosition : IEquatable<LogicalItemPosition>` | A validated immutable logical sequence position; publish through item-position. |
| `public LogicalItemPosition()` | Create validated sequence/position/count metadata; null sequence or positions outside 1..count throw. The default constructor creates invalid, nonmatching metadata. |
| `public LogicalItemPosition(object sequence, int position, int count)` | Create validated sequence/position/count metadata; null sequence or positions outside 1..count throw. The default constructor creates invalid, nonmatching metadata. |
| `public override string ToString()` | Return a diagnostic record representation. |
| `public static bool operator !=(LogicalItemPosition left, LogicalItemPosition right)` | Compare immutable values for equality or inequality. |
| `public static bool operator ==(LogicalItemPosition left, LogicalItemPosition right)` | Compare immutable values for equality or inequality. |
| `public override int GetHashCode()` | Hash the value consistently with equality. |
| `public override bool Equals(object obj)` | Compare immutable values of the same type. |
| `public bool Equals(LogicalItemPosition other)` | Compare immutable values of the same type. |
| `public object? Sequence { get; }` | The explicit logical sequence/group identity; null only for the invalid default value. |
| `public int Position { get; }` | The one-based logical position, including unrealized items. |
| `public int Count { get; }` | The complete known count of the logical sequence. |

<a id="type-nativewindowhandle"></a>

## `NativeWindowHandle`

Namespace: `Nazor`

```csharp
public readonly record struct NativeWindowHandle(nint Handle, string Descriptor);
```

`Handle` is a borrowed native identifier; `Descriptor` identifies its API (`HWND`, `X11`, or `wl_surface`; other backends may define more). The generated value equality, constructor, `Deconstruct(out nint Handle, out string Descriptor)`, and init properties follow record struct semantics. [Source](../../Sources/Core/Nazor.Core/Windows/NativeWindowHandle.cs).

| Declaration | Contract |
| --- | --- |
| `public readonly struct NativeWindowHandle : IEquatable<NativeWindowHandle>` | Borrowed platform window identity. |
| `public NativeWindowHandle(nint Handle, string Descriptor)` | Creates a borrowed reference value. |
| `public NativeWindowHandle()` | Creates the default value. |
| `public nint Handle { get; init; }` | Native identifier. |
| `public string Descriptor { get; init; }` | Backend API identifier. |
| `public void Deconstruct(out nint Handle, out string Descriptor)` | Deconstructs the value. |
| `public override string ToString()` | Standard record representation. |
| `public static bool operator !=(NativeWindowHandle left, NativeWindowHandle right)` | Value inequality. |
| `public static bool operator ==(NativeWindowHandle left, NativeWindowHandle right)` | Value equality. |
| `public override int GetHashCode()` | Value hash. |
| `public override bool Equals(object obj)` | Value equality; null is accepted and compares unequal. |
| `public bool Equals(NativeWindowHandle other)` | Typed value equality. |

<a id="type-navigationkind"></a>

## `NavigationKind`

Namespace `Nazor`. [Source](../../Sources/Core/Nazor.Core/Navigation/NavigationManager.cs)

| Declaration | Contract |
| --- | --- |
| `public enum NavigationKind` | History operation reported after completed navigation. |
| `New = 0` | Appends a new entry. |
| `Replace = 1` | Replaces the current entry. |
| `Back = 2` | Selects the preceding entry. |
| `Forward = 3` | Selects the following entry. |

<a id="type-ilayouttransition"></a>

## `ILayoutTransition`

Namespace `Nazor.Rendering`. [Source](../../Sources/Core/Nazor.Core/Rendering/ILayoutTransition.cs)

| Declaration | Contract |
| --- | --- |
| `public interface ILayoutTransition` | Scene-owned provider attached to `layout-transition=true` elements; cleared on disabling, removal and scene disposal. |
| `LayoutTransitionOrigin? CaptureOrigin(ElementNode element)` | UI-thread capture before removing the source. Null when removed, hidden or empty; rotation/skew/singular transforms throw. |

<a id="type-layouttransitionorigin"></a>

## `LayoutTransitionOrigin`

Namespace `Nazor.Rendering`. [Source](../../Sources/Core/Nazor.Core/Rendering/ILayoutTransition.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed class LayoutTransitionOrigin` | Opaque one-use border-box snapshot in logical window coordinates, retaining geometry without the source element/component. Obtained through `ILayoutTransition`; consumed only in its owning scene. |
| `public double X { get; }` | Left edge. |
| `public double Y { get; }` | Top edge. |
| `public double Width { get; }` | Width. |
| `public double Height { get; }` | Height. |
