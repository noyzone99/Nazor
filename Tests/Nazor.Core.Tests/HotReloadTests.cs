using Nazor.Rendering;
using Nazor.Testing;

namespace Nazor.Core.Tests;

public sealed class HotParent : ComponentBase
{
    public int Renders { get; private set; }
    public bool PassExtra { get; set; } = true;
    public HotChild? Child { get; private set; }
    public StaticChild? Static { get; private set; }

    public void Refresh() => StateHasChanged();

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        Renders++;
        builder.OpenComponent<HotChild>(0);
        builder.AddComponentParameter(1, "Value", 1);
        if (PassExtra) builder.AddComponentParameter(2, "Extra", "x");
        builder.AddComponentReferenceCapture(3, c => Child = (HotChild)c);
        builder.CloseComponent();
        builder.OpenComponent<StaticChild>(4);
        builder.AddComponentReferenceCapture(5, c => Static = (StaticChild)c);
        builder.CloseComponent();
    }
}

public sealed class HotChild : ComponentBase, IDisposable
{
    [Parameter] public int Value { get; set; }
    [Parameter] public string? Extra { get; set; }

    public int Renders { get; private set; }
    public int ParametersSet { get; private set; }
    public bool Disposed { get; private set; }

    protected override void OnParametersSet() => ParametersSet++;

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        Renders++;
        builder.OpenElement(0, "Child");
        builder.AddContent(1, Value);
        builder.CloseElement();
    }

    public void Dispose() => Disposed = true;
}

public sealed class StaticChild : ComponentBase
{
    public int Renders { get; private set; }

    protected override bool ShouldRender() => false;

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        Renders++;
        builder.OpenElement(0, "Static");
        builder.CloseElement();
    }
}

[Collection("HotReload")]
public sealed class HotReloadTests
{
    [Fact]
    public void ApplyUpdate_raises_DeltaApplied_with_the_updated_types()
    {
        var received = new List<Type[]?>();
        Action<Type[]?> subscriber = received.Add;
        HotReloadManager.DeltaApplied += subscriber;
        try
        {
            HotReloadManager.ApplyUpdate(null);
            HotReloadManager.ApplyUpdate([typeof(HotChild)]);
        }
        finally
        {
            HotReloadManager.DeltaApplied -= subscriber;
        }

        Assert.Equal(2, received.Count);
        Assert.Null(received[0]);
        Assert.Equal([typeof(HotChild)], received[1]!);
        Assert.Null(HotReloadManager.LastError);
        Assert.Equal(System.Reflection.Metadata.MetadataUpdater.IsSupported, HotReloadManager.IsSupported);
    }

    [Fact]
    public void A_failing_subscriber_is_kept_in_LastError_and_the_others_still_run()
    {
        var reached = false;
        Action<Type[]?> failing = _ => throw new InvalidOperationException("subscriber failed");
        Action<Type[]?> next = _ => reached = true;
        HotReloadManager.DeltaApplied += failing;
        HotReloadManager.DeltaApplied += next;
        try
        {
            HotReloadManager.ApplyUpdate(null);
            Assert.True(reached);
            Assert.Equal("subscriber failed", HotReloadManager.LastError?.Message);
        }
        finally
        {
            HotReloadManager.DeltaApplied -= failing;
            HotReloadManager.DeltaApplied -= next;
        }

        HotReloadManager.ApplyUpdate(null);
        Assert.Null(HotReloadManager.LastError);
    }

    [Fact]
    public void RenderOnHotReload_renders_every_component_again_keeping_elements_and_instances()
    {
        using var host = new TestHost();
        var root = host.Render<HotParent>();
        var parent = (HotParent)root.Component;
        var child = parent.Child!;
        var statik = parent.Static!;
        var childElement = root.Single("Child");
        var staticElement = root.Single("Static");
        Assert.Equal((1, 1, 1, 1), (parent.Renders, child.Renders, child.ParametersSet, statik.Renders));

        parent.Refresh();
        Assert.Equal((2, 1, 1, 1), (parent.Renders, child.Renders, child.ParametersSet, statik.Renders));

        host.Renderer.RenderOnHotReload();

        Assert.Equal((3, 2, 2, 2), (parent.Renders, child.Renders, child.ParametersSet, statik.Renders));
        Assert.Same(child, parent.Child);
        Assert.Same(statik, parent.Static);
        Assert.Same(childElement, root.Single("Child"));
        Assert.Same(staticElement, root.Single("Static"));
        Assert.False(child.Disposed);
        Assert.Equal("1", root.Text());
        Assert.Empty(host.Batches[^1].AddedElements);
        Assert.Empty(host.Batches[^1].RemovedNodes);
    }

    [Fact]
    public void A_child_losing_a_parameter_is_recreated_on_hot_reload()
    {
        using var host = new TestHost();
        var root = host.Render<HotParent>();
        var parent = (HotParent)root.Component;
        var child = parent.Child!;
        var childElement = root.Single("Child");
        var components = host.Renderer.ComponentCount;

        parent.PassExtra = false;
        host.Renderer.RenderOnHotReload();

        Assert.NotSame(child, parent.Child);
        Assert.True(child.Disposed);
        Assert.False(parent.Child!.Disposed);
        Assert.Null(parent.Child.Extra);
        Assert.NotSame(childElement, root.Single("Child"));
        Assert.True(childElement.IsRemoved);
        Assert.Equal(components, host.Renderer.ComponentCount);
        Assert.Equal("1", root.Text());

        var recreated = parent.Child;
        parent.PassExtra = true;
        parent.Refresh();
        parent.PassExtra = false;
        parent.Refresh();
        Assert.Same(recreated, parent.Child);
        Assert.Equal("x", recreated.Extra);
    }

    [Fact]
    public void RenderOnHotReload_requires_the_dispatcher_and_a_live_renderer()
    {
        var host = new TestHost();
        host.Render<HotParent>();
        Exception? offThread = null;
        var thread = new Thread(() => offThread = Record.Exception(host.Renderer.RenderOnHotReload));
        thread.Start();
        thread.Join();
        Assert.IsType<InvalidOperationException>(offThread);

        host.Dispose();
        Assert.Throws<ObjectDisposedException>(host.Renderer.RenderOnHotReload);
    }

    [Fact]
    public void Parameters_can_be_set_after_the_cache_is_cleared()
    {
        var child = new HotChild();
        ParameterView.FromDictionary(new Dictionary<string, object?> { ["Value"] = 5 }).SetParameterProperties(child);
        ParameterView.ClearCache();
        ParameterView.FromDictionary(new Dictionary<string, object?> { ["value"] = 7, ["Extra"] = "y" }).SetParameterProperties(child);
        Assert.Equal((7, "y"), (child.Value, child.Extra));
    }
}

[CollectionDefinition("HotReload", DisableParallelization = true)]
public sealed class HotReloadCollection;
