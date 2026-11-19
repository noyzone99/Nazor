# 5. Components and Razor

A `.nazor` file compiles to a component class. Its markup composes controls or other components; expressions read the component's current state. Use `@code` for members and `@style` for scoped NSS.

C# inside components follows the application's evaluated language version and conditional compilation symbols. A `net11.0` application uses C# 15 by default, including collection expression arguments:

```razor
<TextBlock>@values.Count</TextBlock>
@code {
    private readonly List<int> values = [with(capacity: 8), 1, 2, 3];
}
```

The compiler and editor services run on .NET 10 LTS; the application's target determines available APIs and runtime requirements. See [build tools and application targets](03-Projects-and-Builds.md#build-tools-and-application-targets) for version selection and preview opt-in.

## Markup rules

```razor
<Container Class="summary">
    @if (items.Count == 0)
    {
        <TextBlock>No items yet.</TextBlock>
    }
    else
    {
        @foreach (var item in items)
        {
            <TextBlock @key="item.Id">@item.Name</TextBlock>
        }
    }
</Container>
```

The interface contains Nazor components, not HTML elements. `<div>` is a compiler error. Put visible text in a text-capable control such as `TextBlock`, `Button`, or `CheckBox`; free static markup is rejected. Whitespace between controls does not add layout space. Use NSS margins, padding, or gaps instead. Text content decodes character entities.

Expressions include `@count`, `@(count > 9)`, and method calls. Razor flow constructs include `@if`, `@foreach`, `@for`, `@while`, and `@switch`. `@{ ... }` executes code while rendering; avoid I/O or state changes there because rendering can occur frequently.

## Directives

| Directive | Purpose |
| --- | --- |
| `@code { ... }` | Component fields, properties, methods, and lifecycle overrides |
| `@using Namespace` | Imports a namespace for markup and C# |
| `@namespace Namespace` | Overrides the generated namespace |
| `@inject Type Name` | Injects a service into a property |
| `@typeparam TItem` | Declares a generic type parameter; constraints are supported |
| `@inherits BaseComponent` | Selects a component base class |
| `@implements IDisposable` | Adds an implemented interface |
| `@attribute [Attribute]` | Adds a C# attribute to the generated class |
| `@page "/items/{Id:int}"` | Declares a route for a component displayed by `Frame` |
| `@window` | Declares a [window component](13-Windows-and-Dialogs.md) with an associated `IWindow` |
| `@style { ... }` | Declares scoped NSS; must begin a line at the component's top level |

Default Razor usings include `System`, collections, LINQ, tasks, `Nazor`, `Nazor.Input`, and `Nazor.Controls`. Folder-level `_Imports.nazor` files add directives from the project root down to the component's directory.

## Names and code-behind

`Components/TaskCard.nazor` produces `TaskCard` in `<RootNamespace>.Components`, unless configured otherwise. Its optional `TaskCard.nazor.cs` must declare the same partial class in the same namespace:

```csharp
namespace HelloNazor.Components;

public partial class TaskCard
{
    private string StatusText => IsComplete ? "Complete" : "Open";
}
```

This example assumes the other partial declaration defines `IsComplete`. Sidecars do not create a second component. A namespace mismatch creates a different class and usually produces confusing missing-member errors.

A local component can shadow an imported control of the same name. Use a fully qualified tag such as `<Nazor.Controls.ListView ... />` to disambiguate. Prefer distinct names for application components.

## Comments and authoring

Use `@* ... *@` in Razor markup, normal C# comments in C# blocks or sidecars, and `/* ... */` in NSS. Document reusable component parameters with XML comments in C# where useful. Explain ownership or behavioral constraints rather than narrating every assignment.

Keep markup focused on composition. If a view accumulates several independent state machines, extract a reusable component or move a business operation into a service. Sidecars help organize a component; they do not replace a clear state boundary.

## Components written in C#

A public C# class can derive from `ComponentBase` or implement `IComponent` and build a render tree. Components discovered from referenced assemblies must have a public parameterless constructor and live in a namespace. Generic components are supported. Prefer property injection to constructor injection for component activation.

Render-tree APIs are intended for specialized controls and infrastructure; ordinary application composition is easier to maintain in `.nazor`. See [architecture](31-Architecture-and-Extension-Points.md) and [Nazor.Core](../ApiReferences/Nazor.Core.md).

---

[Guide index](README.md) · [API reference](../API.md)
