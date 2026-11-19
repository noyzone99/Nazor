using Nazor.Input;
using Nazor.Rendering;
using Nazor.Testing;

namespace Nazor.Core.Tests;

public sealed class RendererTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task A_consumed_key_suppresses_remaining_handlers_and_default_actions(bool consumed)
    {
        using var host = new TestHost();
        var root = host.Render<KeyBoundary>(); var fixture = (KeyBoundary)root.Component;
        await host.Renderer.DispatchEventAsync(root.Single("Handle"),"onkeydown",new KeyboardEventArgs { Key=consumed ? "ArrowRight" : "Tab" });
        Assert.Equal(consumed ? 0 : 1,fixture.ParentKeys);
        Assert.Equal(consumed ? 0 : 1,fixture.DefaultKeys);
    }

    private sealed class KeyBoundary : ComponentBase
    {
        public int ParentKeys,DefaultKeys;
        protected override void BuildRenderTree(RenderTreeBuilder b)
        {
            b.OpenElement(0,"Parent"); b.AddAttribute(1,"onkeydown",EventCallback.Factory.Create<KeyboardEventArgs>(this,_=>ParentKeys++));
            b.OpenElement(2,"Handle");
            b.AddAttribute(3,"__internal_onkeydown",EventCallback.Factory.Create<KeyboardEventArgs>(this,e=>e.Handled=e.Key=="ArrowRight"));
            b.AddAttribute(4,"__default_onkeydown",EventCallback.Factory.Create<KeyboardEventArgs>(this,_=>DefaultKeys++));
            b.CloseElement(); b.CloseElement();
        }
    }

    [Fact]
    public async Task A_failed_render_does_not_leave_frames_in_the_next_render()
    {
        using var host = new TestHost(captureExceptions: true);
        var root = host.Render<RecoverableRender>();
        var component = (RecoverableRender)root.Component;
        var element = root.Single("Box");
        await host.Dispatcher.InvokeAsync(() => { component.Fail = true; component.Refresh(); });
        Assert.Single(host.Exceptions);
        Assert.Same(element, root.Single("Box"));
        await host.Dispatcher.InvokeAsync(() => { component.Fail = false; component.Refresh(); });
        Assert.Same(element, root.Single("Box"));
        Assert.Equal("Stable", root.Text());
        Assert.Single(root.Elements());
    }

    private sealed class RecoverableRender : ComponentBase
    {
        public bool Fail { get; set; }
        public void Refresh() => StateHasChanged();
        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenElement(0, "Box");
            if (Fail) throw new InvalidOperationException("Render interrupted");
            builder.AddContent(1, "Stable");
            builder.CloseElement();
        }
    }

    [Fact]
    public async Task Counter_click_updates_text_and_keeps_element_identity()
    {
        using var host = new TestHost();
        var root = host.Render<CounterComponent>();
        Assert.Equal("""
            Box.counter
              Label
                "Count: 0"
              Button
                "Increment"
            """.ReplaceLineEndings("\n"), root.Dump());
        var box = root.Single("Box");
        var label = root.Single("Label");
        var text = (TextNode)label.Children[0];

        await host.ClickAsync(root.Single("Button"));

        Assert.Equal("Count: 1Increment", root.Text());
        Assert.Same(box, root.Single("Box"));
        Assert.Same(label, root.Single("Label"));
        Assert.Same(text, label.Children[0]);
        Assert.Contains(text, host.Batches[^1].UpdatedTexts);
    }

    [Fact]
    public async Task Keyed_reorder_preserves_component_instances_and_elements()
    {
        using var host = new TestHost();
        var root = host.Render<ItemList>();
        var list = (ItemList)root.Component;
        var before = list.Items.ToDictionary(i => i.Id);
        var elements = root.Elements("Item").ToDictionary(e => e.Text());

        await host.Dispatcher.InvokeAsync(() =>
        {
            list.Ids.Reverse();
            list.Refresh();
        });

        Assert.Equal("321", root.Text());
        Assert.Equal(3, list.Items.Count);
        foreach (var element in root.Elements("Item")) Assert.Same(elements[element.Text()], element);
        Assert.All(before.Values, item => Assert.False(item.Disposed));
        Assert.Contains(root.Single("List"), host.Batches[^1].ChildrenChanged);
        Assert.Empty(host.Batches[^1].AddedElements);
        Assert.Empty(host.Batches[^1].RemovedNodes);
    }

    [Fact]
    public async Task Unkeyed_items_are_matched_by_position()
    {
        using var host = new TestHost();
        var root = host.Render<ItemList>(ParameterView.FromDictionary(new Dictionary<string, object?> { ["UseKeys"] = false }));
        var list = (ItemList)root.Component;
        var first = root.Elements("Item").First();
        var firstItem = list.Items[0];

        await host.Dispatcher.InvokeAsync(() =>
        {
            list.Ids.Reverse();
            list.Refresh();
        });

        Assert.Equal("321", root.Text());
        Assert.Same(first, root.Elements("Item").First());
        Assert.Equal(3, firstItem.Id);
    }

    [Fact]
    public async Task Removing_an_item_disposes_its_component_and_reports_removed_nodes()
    {
        using var host = new TestHost();
        var root = host.Render<ItemList>();
        var list = (ItemList)root.Component;
        var removed = list.Items.Single(i => i.Id == 2);
        var removedElement = root.Elements("Item").Single(e => e.Text() == "2");
        var components = host.Renderer.ComponentCount;

        await host.Dispatcher.InvokeAsync(() =>
        {
            list.Ids.Remove(2);
            list.Refresh();
        });

        Assert.Equal("13", root.Text());
        Assert.True(removed.Disposed);
        Assert.True(removedElement.IsRemoved);
        Assert.Equal(components - 1, host.Renderer.ComponentCount);
        Assert.Contains(removedElement, host.Batches[^1].RemovedNodes);
        Assert.All(list.Items.Where(i => i.Id != 2), item => Assert.False(item.Disposed));
    }

    [Fact]
    public async Task Inserting_new_keys_creates_only_new_instances()
    {
        using var host = new TestHost();
        var root = host.Render<ItemList>();
        var list = (ItemList)root.Component;
        var existing = root.Elements("Item").ToArray();

        await host.Dispatcher.InvokeAsync(() =>
        {
            list.Ids.Insert(1, 9);
            list.Refresh();
        });

        Assert.Equal("1923", root.Text());
        Assert.Equal(4, list.Items.Count);
        Assert.Equal(9, list.Items[^1].Id);
        var after = root.Elements("Item").ToArray();
        Assert.Same(existing[0], after[0]);
        Assert.Same(existing[1], after[2]);
        Assert.Same(existing[2], after[3]);
        Assert.Single(host.Batches[^1].AddedElements);
    }

    [Fact]
    public async Task Keyed_elements_keep_identity_when_reordered()
    {
        using var host = new TestHost();
        var root = host.Render<KeyedElements>();
        var component = (KeyedElements)root.Component;
        var before = root.Nodes.Cast<ElementNode>().ToDictionary(e => e.Class!);

        await host.Dispatcher.InvokeAsync(() =>
        {
            component.Names.Reverse();
            component.Refresh();
        });

        Assert.Equal(["c", "b", "a"], root.Nodes.Cast<ElementNode>().Select(e => e.Class));
        Assert.All(root.Nodes.Cast<ElementNode>(), e => Assert.Same(before[e.Class!], e));
        Assert.True(host.Batches[^1].RootNodesChanged);
    }

    [Fact]
    public async Task Conditional_content_is_inserted_and_removed_without_disturbing_siblings()
    {
        using var host = new TestHost();
        var root = host.Render<Conditional>();
        var component = (Conditional)root.Component;
        var always = root.Single("Label");

        await host.Dispatcher.InvokeAsync(() =>
        {
            component.Show = true;
            component.Refresh();
        });
        Assert.Equal("optionalalways", root.Text());
        Assert.Same(always, root.Elements("Label").Last());

        await host.Dispatcher.InvokeAsync(() =>
        {
            component.Show = false;
            component.Refresh();
        });
        Assert.Equal("always", root.Text());
        Assert.Same(always, root.Single("Label"));
    }

    [Fact]
    public async Task Children_with_unchanged_immutable_parameters_are_not_rendered_again()
    {
        using var host = new TestHost();
        var root = host.Render<CountingParent>();
        var parent = (CountingParent)root.Component;

        await host.Dispatcher.InvokeAsync(parent.Refresh);
        Assert.Equal(1, parent.Child!.Renders);

        parent.Value = 2;
        await host.Dispatcher.InvokeAsync(parent.Refresh);
        Assert.Equal(2, parent.Child.Renders);
        Assert.Equal("2", root.Text());
    }

    [Fact]
    public async Task Child_content_always_renders_the_child_again()
    {
        using var host = new TestHost();
        var root = host.Render<CountingParent>();
        var parent = (CountingParent)root.Component;
        parent.PassChildContent = true;
        await host.Dispatcher.InvokeAsync(parent.Refresh);
        var renders = parent.Child!.Renders;

        await host.Dispatcher.InvokeAsync(parent.Refresh);

        Assert.Equal(renders + 1, parent.Child.Renders);
        Assert.Equal("1child", root.Text());
    }

    [Fact]
    public void Lifecycle_runs_in_order_and_after_render_reports_first_render()
    {
        using var host = new TestHost();
        var root = host.Render<LifecycleComponent>(ParameterView.FromDictionary(new Dictionary<string, object?> { ["Title"] = "A" }));
        var component = (LifecycleComponent)root.Component;

        Assert.Equal(["OnInitialized", "OnInitializedAsync", "OnParametersSet:A", "Render", "OnAfterRender:True"], component.Log);
    }

    [Fact]
    public void Incomplete_initialization_renders_before_and_after_completion()
    {
        using var host = new TestHost();
        var gate = new TaskCompletionSource();
        LifecycleComponent? component = null;
        var root = host.Render<GatedLifecycle>(ParameterView.FromDictionary(new Dictionary<string, object?> { ["Gate"] = gate }));
        component = ((GatedLifecycle)root.Component).Inner;
        Assert.NotNull(component);
        Assert.Equal(["OnInitialized", "OnInitializedAsync", "Render", "OnAfterRender:True"], component.Log);

        gate.SetResult();
        host.RunUntil(() => component.Log.Count(l => l == "Render") == 2);

        Assert.Equal(["OnInitialized", "OnInitializedAsync", "Render", "OnAfterRender:True", "OnParametersSet:", "Render", "OnAfterRender:False"], component.Log);
    }

    [Fact]
    public async Task Async_handler_renders_after_synchronous_part_and_after_completion()
    {
        using var host = new TestHost();
        var root = host.Render<AsyncHandler>();
        var component = (AsyncHandler)root.Component;

        var click = host.ClickAsync(root.Single("Button"));
        Assert.Equal("working", root.Single("Label").Text());
        Assert.False(click.IsCompleted);

        component.Gate.SetResult();
        host.RunUntil(() => click.IsCompleted);
        await click;
        Assert.Equal("done", root.Single("Label").Text());
    }

    [Fact]
    public async Task Async_completion_after_removal_does_not_render_or_fail()
    {
        using var host = new TestHost(captureExceptions: true);
        var root = host.Render<Host>();
        var parent = (Host)root.Component;
        var child = (AsyncHandler)parent.Child!;
        var button = root.Single("Button");
        var click = host.ClickAsync(button);

        await host.Dispatcher.InvokeAsync(() =>
        {
            parent.ShowChild = false;
            parent.Refresh();
        });
        Assert.Empty(root.Nodes);
        var batches = host.Batches.Count;

        child.Gate.SetResult();
        host.RunUntil(() => click.IsCompleted);

        Assert.Equal("done", child.State);
        Assert.Equal(batches, host.Batches.Count);
        Assert.Empty(host.Exceptions);
        Assert.Equal(1, host.Renderer.ComponentCount);
        Assert.True(button.IsRemoved);
        await host.Renderer.DispatchEventAsync(button, "onclick", new PointerEventArgs());
    }

    [Fact]
    public async Task Handler_exceptions_reach_the_unhandled_exception_event()
    {
        using var host = new TestHost(captureExceptions: true);
        var root = host.Render<Throwing>();

        await host.ClickAsync(root.Single("Button"));

        var exception = Assert.Single(host.Exceptions);
        Assert.Equal("handler failed", exception.Message);
    }

    [Fact]
    public async Task Handler_exceptions_without_subscriber_fail_the_dispatch()
    {
        using var host = new TestHost();
        var root = host.Render<Throwing>();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => host.ClickAsync(root.Single("Button")));
        Assert.Equal("handler failed", exception.Message);
    }

    [Fact]
    public void Render_exceptions_without_subscriber_are_thrown_to_the_caller()
    {
        using var host = new TestHost();
        var exception = Assert.Throws<InvalidOperationException>(() =>
            host.Render<ThrowingOnRender>());
        Assert.Equal("render failed", exception.Message);
    }

    [Fact]
    public async Task Duplicate_keys_fail_before_the_tree_changes()
    {
        using var host = new TestHost(captureExceptions: true);
        var root = host.Render<DuplicateKeys>();
        var component = (DuplicateKeys)root.Component;
        var before = root.Nodes.ToArray();

        await host.Dispatcher.InvokeAsync(() =>
        {
            component.Ids.Add(1);
            component.Refresh();
        });

        Assert.Contains("same @key value '1'", Assert.Single(host.Exceptions).Message);
        Assert.Equal(before, root.Nodes);
        await host.Dispatcher.InvokeAsync(() =>
        {
            component.Ids.RemoveAt(component.Ids.Count - 1);
            component.Refresh();
        });
        Assert.Single(host.Exceptions);
        Assert.Equal(before, root.Nodes);
    }

    [Fact]
    public async Task Work_from_other_threads_runs_on_the_dispatcher()
    {
        using var host = new TestHost();
        var root = host.Render<ItemList>();
        var list = (ItemList)root.Component;

        var work = Task.Run(() => host.Dispatcher.InvokeAsync(() =>
        {
            Assert.True(host.Dispatcher.CheckAccess());
            list.Ids.Add(4);
            list.Refresh();
        }), TestContext.Current.CancellationToken);
        host.RunUntil(() => work.IsCompleted);
        await work;
        Assert.Equal("1234", root.Text());

        Exception? offThread = null;
        var thread = new Thread(() => offThread = Record.Exception(list.Refresh));
        thread.Start();
        thread.Join();
        Assert.IsType<InvalidOperationException>(offThread);
    }

    [Fact]
    public void Injected_services_are_assigned_before_parameters()
    {
        using var host = new TestHost(new Services { [typeof(IGreeter)] = new Greeter() });
        Assert.Equal("Ciao", host.Render<Injected>().Text());
    }

    [Fact]
    public void Missing_services_fail_with_a_clear_message()
    {
        using var host = new TestHost();
        var exception = Assert.Throws<InvalidOperationException>(() => host.Render<Injected>());
        Assert.Contains("IGreeter", exception.Message);
    }

    [Fact]
    public async Task Removing_the_root_disposes_every_component()
    {
        using var host = new TestHost();
        var root = host.Render<Host>();
        var parent = (Host)root.Component;
        await host.Dispatcher.InvokeAsync(() =>
        {
            parent.ChildType = typeof(AsyncDisposable);
            parent.ShowChild = false;
            parent.Refresh();
            parent.ShowChild = true;
            parent.Refresh();
        });
        var child = (AsyncDisposable)parent.Child!;

        host.Renderer.RemoveRootComponent(root);

        Assert.True(child.Disposed);
        Assert.Equal(0, host.Renderer.ComponentCount);
        Assert.Empty(host.Renderer.RootComponents);
    }

    [Fact]
    public void Elements_are_scoped_to_the_component_that_declared_their_control()
    {
        using var host = new TestHost();
        var root = host.Render<CounterComponent>();

        Assert.All(root.Elements(), element => Assert.Equal(typeof(CounterComponent), element.Scope));
        Assert.IsType<Box>(root.Single("Box").Owner);
    }

    [Fact]
    public void Initial_batch_reports_added_elements_once()
    {
        using var host = new TestHost();
        var root = host.Render<CounterComponent>();

        var batch = Assert.Single(host.Batches);
        Assert.Equal(root.Elements().ToHashSet(), batch.AddedElements.ToHashSet());
        Assert.True(batch.RootNodesChanged);
        Assert.Empty(batch.RemovedNodes);
    }

    private sealed class Services : Dictionary<Type, object>, IServiceProvider
    {
        public object? GetService(Type serviceType) => TryGetValue(serviceType, out var service) ? service : null;
    }
}

public sealed class GatedLifecycle : ComponentBase
{
    [Parameter] public TaskCompletionSource? Gate { get; set; }

    private GateSetter? setter;

    public LifecycleComponent? Inner => setter?.Inner;

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenComponent<GateSetter>(0);
        builder.AddComponentParameter(1, "Gate", Gate);
        builder.AddComponentReferenceCapture(2, c => setter = (GateSetter)c);
        builder.CloseComponent();
    }
}

public sealed class GateSetter : ComponentBase
{
    [Parameter] public TaskCompletionSource? Gate { get; set; }

    public LifecycleComponent? Inner { get; private set; }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenComponent<LifecycleComponent>(0);
        builder.AddComponentReferenceCapture(1, c =>
        {
            Inner = (LifecycleComponent)c;
            Inner.InitializationGate = Gate;
        });
        builder.CloseComponent();
    }
}

public sealed class ThrowingOnRender : ComponentBase
{
    protected override void BuildRenderTree(RenderTreeBuilder builder) => throw new InvalidOperationException("render failed");
}
