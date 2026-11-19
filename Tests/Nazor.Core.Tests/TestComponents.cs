using Nazor.Input;
using Nazor.Rendering;

namespace Nazor.Core.Tests;


public sealed class Box : ComponentBase
{
    [Parameter] public string? Class { get; set; }
    [Parameter] public RenderFragment? ChildContent { get; set; }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "Box");
        builder.AddAttribute(1, "class", Class);
        builder.AddContent(2, ChildContent);
        builder.CloseElement();
    }
}

public sealed class Label : ComponentBase
{
    [Parameter] public string? Text { get; set; }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "Label");
        builder.AddContent(1, Text);
        builder.CloseElement();
    }
}

public sealed class Clickable : ComponentBase
{
    [Parameter] public EventCallback<PointerEventArgs> OnClick { get; set; }
    [Parameter] public string? Text { get; set; }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "Button");
        builder.AddAttribute(1, "onclick", OnClick);
        builder.AddContent(2, Text);
        builder.CloseElement();
    }
}

public sealed class CounterComponent : ComponentBase
{
    private int count;

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenComponent<Box>(0, typeof(CounterComponent));
        builder.AddComponentParameter(1, "Class", "counter");
        builder.AddAttribute(2, "ChildContent", (RenderFragment)(inner =>
        {
            inner.OpenComponent<Label>(3, typeof(CounterComponent));
            inner.AddComponentParameter(4, "Text", $"Count: {count}");
            inner.CloseComponent();
            inner.OpenComponent<Clickable>(5, typeof(CounterComponent));
            inner.AddComponentParameter(6, "OnClick", EventCallback.Factory.Create<PointerEventArgs>(this, () => count++));
            inner.AddComponentParameter(7, "Text", "Increment");
            inner.CloseComponent();
        }));
        builder.CloseComponent();
    }
}

public sealed class Item : ComponentBase, IDisposable
{
    private static int instances;

    [Parameter] public int Id { get; set; }

    public int Instance { get; } = Interlocked.Increment(ref instances);
    public bool Disposed { get; private set; }
    public List<int> Log { get; } = [];

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "Item");
        builder.AddContent(1, Id);
        builder.CloseElement();
    }

    public void Dispose() => Disposed = true;
}

public sealed class ItemList : ComponentBase
{
    public List<int> Ids { get; } = [1, 2, 3];
    [Parameter] public bool UseKeys { get; set; } = true;
    public List<Item> Items { get; } = [];

    public void Refresh() => StateHasChanged();

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "List");
        foreach (var id in Ids)
        {
            builder.OpenComponent<Item>(1, typeof(ItemList));
            if (UseKeys) builder.SetKey(id);
            builder.AddComponentParameter(2, "Id", id);
            builder.AddComponentReferenceCapture(3, item => Items.Add((Item)item));
            builder.CloseComponent();
        }
        builder.CloseElement();
    }
}

public sealed class KeyedElements : ComponentBase
{
    public List<string> Names { get; } = ["a", "b", "c"];

    public void Refresh() => StateHasChanged();

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        foreach (var name in Names)
        {
            builder.OpenElement(0, "Row");
            builder.SetKey(name);
            builder.AddAttribute(1, "class", name);
            builder.CloseElement();
        }
    }
}

public sealed class Conditional : ComponentBase
{
    public bool Show { get; set; }

    public void Refresh() => StateHasChanged();

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "Root");
        if (Show)
        {
            builder.OpenComponent<Label>(1);
            builder.AddComponentParameter(2, "Text", "optional");
            builder.CloseComponent();
        }
        builder.OpenComponent<Label>(3);
        builder.AddComponentParameter(4, "Text", "always");
        builder.CloseComponent();
        builder.CloseElement();
    }
}

public sealed class RenderCounting : ComponentBase
{
    [Parameter] public int Value { get; set; }
    [Parameter] public RenderFragment? ChildContent { get; set; }

    public int Renders { get; private set; }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        Renders++;
        builder.OpenElement(0, "Counted");
        builder.AddContent(1, Value);
        builder.AddContent(2, ChildContent);
        builder.CloseElement();
    }
}

public sealed class CountingParent : ComponentBase
{
    public int Value { get; set; } = 1;
    public bool PassChildContent { get; set; }
    public RenderCounting? Child { get; private set; }

    public void Refresh() => StateHasChanged();

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenComponent<RenderCounting>(0);
        builder.AddComponentParameter(1, "Value", Value);
        if (PassChildContent) builder.AddAttribute(2, "ChildContent", (RenderFragment)(b => b.AddContent(3, "child")));
        builder.AddComponentReferenceCapture(4, c => Child = (RenderCounting)c);
        builder.CloseComponent();
    }
}

public sealed class LifecycleComponent : ComponentBase
{
    [Parameter] public string? Title { get; set; }

    public List<string> Log { get; } = [];
    public TaskCompletionSource? InitializationGate { get; set; }

    protected override void OnInitialized() => Log.Add("OnInitialized");

    protected override async Task OnInitializedAsync()
    {
        Log.Add("OnInitializedAsync");
        if (InitializationGate is not null) await InitializationGate.Task;
    }

    protected override void OnParametersSet() => Log.Add($"OnParametersSet:{Title}");

    protected override void OnAfterRender(bool firstRender) => Log.Add($"OnAfterRender:{firstRender}");

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        Log.Add("Render");
        builder.OpenElement(0, "Title");
        builder.AddContent(1, Title);
        builder.CloseElement();
    }
}

public sealed class AsyncHandler : ComponentBase
{
    public TaskCompletionSource Gate { get; } = new();
    public string State { get; private set; } = "idle";

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenComponent<Label>(0);
        builder.AddComponentParameter(1, "Text", State);
        builder.CloseComponent();
        builder.OpenComponent<Clickable>(2);
        builder.AddComponentParameter(3, "OnClick", EventCallback.Factory.Create<PointerEventArgs>(this, async () =>
        {
            State = "working";
            await Gate.Task;
            State = "done";
        }));
        builder.CloseComponent();
    }
}

public sealed class Host : ComponentBase
{
    public bool ShowChild { get; set; } = true;
    public Type ChildType { get; set; } = typeof(AsyncHandler);
    public IComponent? Child { get; private set; }

    public void Refresh() => StateHasChanged();

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        if (!ShowChild) return;
        builder.OpenComponent(0, ChildType);
        builder.AddComponentReferenceCapture(1, c => Child = (IComponent)c);
        builder.CloseComponent();
    }
}

public sealed class Throwing : ComponentBase
{
    public bool ThrowOnRender { get; set; }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        if (ThrowOnRender) throw new InvalidOperationException("render failed");
        builder.OpenComponent<Clickable>(0);
        builder.AddComponentParameter(1, "OnClick", EventCallback.Factory.Create<PointerEventArgs>(this, Fail));
        builder.CloseComponent();
    }

    private static void Fail() => throw new InvalidOperationException("handler failed");
}

public sealed class DuplicateKeys : ComponentBase
{
    public List<int> Ids { get; } = [1, 2];

    public void Refresh() => StateHasChanged();

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        foreach (var id in Ids)
        {
            builder.OpenElement(0, "Row");
            builder.SetKey(id);
            builder.CloseElement();
        }
    }
}

public interface IGreeter
{
    string Greet();
}

public sealed class Greeter : IGreeter
{
    public string Greet() => "Ciao";
}

public sealed class Injected : ComponentBase
{
    [Inject] private IGreeter Greeter { get; set; } = null!;

    protected override void BuildRenderTree(RenderTreeBuilder builder) => builder.AddContent(0, Greeter.Greet());
}

public sealed class AsyncDisposable : ComponentBase, IAsyncDisposable
{
    public bool Disposed { get; private set; }

    public ValueTask DisposeAsync()
    {
        Disposed = true;
        return ValueTask.CompletedTask;
    }
}
