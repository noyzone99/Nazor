# 7. Generics, templates, and cascading values

## Generic components

`@typeparam TItem` declares a generic component. It can also specify a constraint, for example `@typeparam TItem where TItem : class`. The compiler can infer `TItem` from typed parameters, or the caller can supply it explicitly with `TItem="TaskItem"`. Constraints are retained for components discovered in compiled libraries.

Create `ItemList.nazor`:

```razor
@typeparam TItem

@foreach (var item in Items)
{
    <Container @key="ItemKey(item)">
        @ItemTemplate(item)
    </Container>
}

@code {
    [Parameter, EditorRequired] public IEnumerable<TItem> Items { get; set; } = [];
    [Parameter, EditorRequired] public Func<TItem, object> ItemKey { get; set; } = null!;
    [Parameter, EditorRequired] public RenderFragment<TItem> ItemTemplate { get; set; } = null!;
}
```

A caller supplies the template:

```razor
<ItemList Items="tasks" ItemKey="task => task.Id">
    <ItemTemplate Context="task">
        <TextBlock>@task.Title</TextBlock>
    </ItemTemplate>
</ItemList>
```

`Context` names the template argument; its default name is `context`. The row wrapper owns the key because the fragment may render several controls. The example renders all items; use `ListView` for a large collection that needs virtualization.

Templates keep the caller's component/style ownership. The generic component's wrapper has its own ownership. This explains why styling a wrapper and styling controls supplied by a template may require different sheets.

`[CascadingTypeParameter(nameof(TItem))]` can propagate a generic type to descendants declared in the same Razor document according to Razor inference rules. It does not register an application service.

## Cascading values from markup

```razor
<CascadingValue Value="session" Name="session">
    <Editor />
</CascadingValue>
```

The receiving component declares:

```csharp
[CascadingParameter(Name = "session")]
public Session? Session { get; set; }
```

`Session` here is an application-defined type. The nearest compatible provider wins. Names compare without case sensitivity; a named provider does not satisfy an unnamed parameter. Matching uses both type and, when supplied, name. Non-public and inherited cascading properties are supported. Without a provider, the property's initial value remains.

The runtime assigns cascading values before lifecycle callbacks and notifies receiving components without discarding their direct parameters. Set `IsFixed="true"` only when consumers do not need change subscriptions. It is a lifetime contract, not object immutability. A provider's `Name` and `IsFixed` cannot change after initialization; `Value` is required.

## Root values from DI

Register a fixed root value in startup:

```csharp
builder.Services.AddCascadingValue("mode", _ => "editing");
```

For an explicitly notifying source:

```csharp
builder.Services.AddScoped(_ =>
    new CascadingValueSource<Session>(new Session(), isFixed: false));
builder.Services.AddCascadingValue<Session>(services =>
    services.GetRequiredService<CascadingValueSource<Session>>());
```

After replacing the value, call `await source.NotifyChangedAsync(newSession)`. After mutating the existing object, call `await source.NotifyChangedAsync()`. Notifications return to the receiving window's dispatcher. `INotifyPropertyChanged` is not automatically subscribed. Component removal releases its cascading subscriptions.

Use DI for services and lifetime ownership. Use cascading parameters for context consumed through a subtree. Prefer explicit parameters when only a direct parent and child need the value.

Example: [Tasks ItemList](../../Samples/Nazor.Samples.Tasks/Components/ItemList.nazor). API: [Nazor.Core](../ApiReferences/Nazor.Core.md), [Nazor.Application](../ApiReferences/Nazor.Application.md).

---

[Guide index](README.md) · [API reference](../API.md)
