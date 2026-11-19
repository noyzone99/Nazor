# 8. Lifecycle, state, and threading

## Lifecycle

| Callback | When it runs |
| --- | --- |
| `SetParametersAsync` | Receives a parameter assignment; custom overrides must preserve the desired base behavior |
| `OnInitialized` / `OnInitializedAsync` | Once per component instance |
| `OnParametersSet` / `OnParametersSetAsync` | After parameter assignment, including the initial assignment |
| `ShouldRender` | Controls subsequent rendering; the initial render still occurs |
| `OnAfterRender` / `OnAfterRenderAsync` | After rendering; `firstRender` identifies the initial render |
| `Dispose` / `DisposeAsync` | When a removed component implements the corresponding disposal interface |

An incomplete initialization task allows an intermediate render, then another render when it completes. Event callbacks similarly render after their synchronous portion and after asynchronous completion. Keep fields in a valid renderable state before each `await`.

`OnAfterRenderAsync` does not create an automatic endless render loop. When it changes visible state after awaiting, request a render explicitly if needed. Use its `firstRender` flag to perform one-time focus or interop with an element reference.

## State ownership

Local fields belong to the component instance. Conditional replacement, navigation, or window closure can dispose that instance. Data that must survive belongs in a longer-lived owner: a parent, a scoped editing session, or an application service.

The [Tasks sample](../../Samples/Nazor.Samples.Tasks/README.md) stores task data in a singleton service while switching between local views. Its navigation state remains in the shell. A routed page is recreated when revisited; scoped services can preserve window-specific data across pages.

Incoming parameters belong to the parent. Keep a separate draft when implementing cancelable editing. Commit with a callback and decide explicitly whether later parent updates replace an in-progress draft.

The renderer can skip a child whose parameters are unchanged known immutable values. Passing a mutable object does not establish a general property-notification system. Notify through callbacks, state changes, or a deliberately notifying cascading source.

## UI dispatcher

All windows in an application share the UI thread. Component rendering, element access, and most desktop operations belong there. Event callbacks and ordinary `await` continuations in the component context return to it. A timer or worker callback must marshal updates:

```razor
@implements IDisposable

<TextBlock>@now.ToString("T")</TextBlock>

@code {
    private DateTime now = DateTime.Now;
    private Timer? timer;

    protected override void OnInitialized() =>
        timer = new Timer(_ => _ = InvokeAsync(() =>
        {
            now = DateTime.Now;
            StateHasChanged();
        }), null, 1000, 1000);

    public void Dispose() => timer?.Dispose();
}
```

The timer demonstrates dispatcher transfer; a production polling operation should also handle cancellation and errors. Do not block the dispatcher with `.Wait()` or `.Result` on incomplete asynchronous work. Use `await` and move genuinely blocking work to an appropriate worker.

`StateHasChanged()` requests rendering; duplicate pending requests can be coalesced. Service events are not component callbacks: after a service event changes local state, request a render on the component dispatcher.

## Cleanup and asynchronous work

Unsubscribe from long-lived events, dispose shortcut registrations, cancel component-owned work, and release resources when a component leaves the tree. If an awaited operation may finish after disposal, use cancellation and a disposed guard before applying its result. Components that can start overlapping searches should cancel or reject stale results.

Choose `IDisposable` for synchronous cleanup or `IAsyncDisposable` for asynchronous cleanup. Avoid requiring a fresh render during disposal. A removed component and its element references no longer describe a usable UI target.

Unhandled rendering/event failures are reported through the application logger and window exception collection. Use normal application error handling for expected failures; an error-boundary component is not provided.

API: [Nazor.Core](../ApiReferences/Nazor.Core.md), [Nazor.Application](../ApiReferences/Nazor.Application.md).

---

[Guide index](README.md) · [API reference](../API.md)
