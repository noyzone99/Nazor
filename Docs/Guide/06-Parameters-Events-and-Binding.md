# 6. Parameters, events, and binding

## Component parameters

Create `Card.nazor`:

```razor
<Container Class="card">
    <TextBlock Class="title">@Title</TextBlock>
    @ChildContent
</Container>

@code {
    [Parameter, EditorRequired] public string Title { get; set; } = "";
    [Parameter] public RenderFragment? ChildContent { get; set; }
}
```

Use it as:

```razor
<Card Title="Order summary">
    <TextBlock>Three items</TextBlock>
</Card>
```

A parameter is a public writable instance property marked `[Parameter]`; `init` setters are not accepted. Names match without case sensitivity. Do not declare parameters that differ only by case. `ChildContent` receives the caller's fragment; its markup and scoped styles still belong to the caller.

`[EditorRequired]` requires the caller to supply an attribute; missing required parameters produce `RZ2012`, treated as an error. It does not validate the runtime value, reject an explicitly supplied null, or replace domain validation. Use ordinary C# validation for those constraints.

Explicit attributes must name declared parameters. Even a component that captures unmatched attributes requires `@attributes` for additional values. Unknown names produce `NZC014`. An empty non-string value produces `NZC015`; omitting an assignment is permitted only for a non-nullable `bool`, otherwise `NZC016`. For nullable values, write an explicit expression such as `IsResizable="@((bool?)null)"` when inheritance is intentional.

## Callbacks

Expose a callback rather than making the parent inspect child state:

```csharp
[Parameter] public EventCallback<string> SaveRequested { get; set; }

private Task SaveAsync() => SaveRequested.InvokeAsync(name);
```

`EventCallback` and `EventCallback<T>` preserve the component receiver used for event-driven rendering. Await callbacks so asynchronous failures and completion follow the event pipeline. Prefer `Task` handlers over `async void`.

`Button.OnClick` and `@onclick` both represent activation. The latter is also available through controls' captured event attributes:

```razor
<Button @onclick="Increment">Increment</Button>
<Button OnClick="SaveAsync" Disabled="saving">Save</Button>
```

An event handler can be a method, a lambda, or an asynchronous delegate with the expected signature. For inferred generic callbacks, an explicitly typed lambda can resolve method-group inference problems: `OnItemActivated="(Order order) => OpenOrder(order)"`.

## Two-way binding

`@bind-X` pairs `X` with an `EventCallback<T>` named `XChanged`:

```razor
<TextField @bind-Value="name" AccessibleName="Name" />
<CheckBox @bind-IsChecked="enabled">Enabled</CheckBox>
```

A reusable toggle component should request a change, not assume it owns the parent's value:

```csharp
[Parameter] public bool Value { get; set; }
[Parameter] public EventCallback<bool> ValueChanged { get; set; }

private Task ToggleAsync() => ValueChanged.InvokeAsync(!Value);
```

The parent uses `<MyToggle @bind-Value="enabled" />`. Treat incoming parameters as parent-owned data; use local fields for independent editing state and callbacks for committing changes.

`@bind-X:after="AfterChange"` runs a delegate after assignment. `@bind-X:get="value" @bind-X:set="SetValueAsync"` uses a custom setter, including an asynchronous one. Avoid also assigning `XChanged` when `@bind-X` already generates it.

## Captured attributes

Declare at most one capture parameter and forward it deliberately:

```razor
<Container @attributes="AdditionalAttributes">
    @ChildContent
</Container>

@code {
    [Parameter] public RenderFragment? ChildContent { get; set; }
    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object?>? AdditionalAttributes { get; set; }
}
```

A caller can supply custom element metadata through `@attributes="attributes"`, where `attributes` is a dictionary. This is also how a control receives a live-region attribute such as `live="polite"`. Use declared parameters for supported component options; use a dictionary for additional element metadata.

## Event surface and identity

Controls support keyboard, focus, pointer, wheel, input, change, and scroll events. They also support event modifiers such as `@onkeydown:preventDefault` and `@onkeydown:stopPropagation`. See [input](24-Input-and-Accessibility.md) for propagation and default-action ordering.

Use `@key` on the outer repeated component or row to preserve identity when sorting, filtering, or inserting. Duplicate sibling keys fail before tree mutation. Avoid indexes as keys for editable or reorderable rows.

Use `@ref` to capture a component instance. It becomes usable after rendering; use `OnAfterRender[Async]` for first-render focus. A reference can become stale after conditional removal, so keep its lifetime aligned with the rendered content.

API: [Nazor.Core](../ApiReferences/Nazor.Core.md), [Nazor.Controls](../ApiReferences/Nazor.Controls.md).

---

[Guide index](README.md) · [API reference](../API.md)
