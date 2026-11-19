using Nazor.Rendering;
using Nazor.Testing;

namespace Nazor.Core.Tests;

public sealed class CascadingTests
{
    [Fact]
    public void Closest_provider_wins_and_only_matching_receivers_update()
    {
        using var host = new TestHost();
        var root = host.Render<CascadeHost>();
        var component = (CascadeHost)root.Component;
        Assert.Equal("p:outerinnernamedplain", root.Text());
        Assert.Equal(1, component.OuterReceiver.Renders);
        component.Outer = "updated";
        component.Refresh();
        Assert.Equal("p:updatedinnernamedplain", root.Text());
        Assert.Equal(2, component.OuterReceiver.Renders);
        Assert.Equal(1, component.InnerReceiver.Renders);
        Assert.Equal(1, component.NamedReceiver.Renders);
        Assert.Equal(1, component.Plain.Renders);
        Assert.Equal("p:", component.OuterReceiver.Prefix);
    }

    [Fact]
    public void Named_and_unnamed_values_do_not_cross_and_missing_provider_preserves_default()
    {
        using var host = new TestHost();
        var root = host.Render<NamedOnly>();
        Assert.Equal("fallbacknamed", root.Text());
    }

    [Fact]
    public void Fixed_provider_has_no_change_notifications_and_cannot_change_identity()
    {
        using var host = new TestHost();
        var root = host.Render<CascadeHost>(ParameterView.FromDictionary(new Dictionary<string, object?> { ["Fixed"] = true }));
        var component = (CascadeHost)root.Component;
        component.Outer = "updated";
        component.Refresh();
        Assert.Equal(1, component.OuterReceiver.Renders);
        component.Fixed = false;
        Assert.Throws<InvalidOperationException>(component.Refresh);
    }

    [Fact]
    public async Task Root_sources_update_on_dispatcher_and_unsubscribe_after_removal()
    {
        var source = new CascadingValueSource<string>("root", isFixed: false);
        using var host = new TestHost(new CascadeServices(source));
        var root = host.Render<CascadeReader>();
        var reader = (CascadeReader)root.Component;
        Assert.Equal("root", root.Text());
        var notification = Task.Run(() => source.NotifyChangedAsync("changed"), TestContext.Current.CancellationToken);
        host.RunUntil(() => notification.IsCompleted);
        await notification;
        Assert.Equal("changed", root.Text());
        Assert.Equal(2, reader.Renders);
        host.Renderer.RemoveRootComponent(root);
        await source.NotifyChangedAsync("removed");
        Assert.Equal(2, reader.Renders);
    }

    [Fact]
    public async Task Mutable_root_value_can_notify_after_in_place_change()
    {
        var value = new Payload { Text = "old" };
        var source = new CascadingValueSource<Payload>(value, isFixed: false);
        using var host = new TestHost(new CascadeServices(source));
        var root = host.Render<PayloadReader>();
        value.Text = "new";
        await source.NotifyChangedAsync();
        Assert.Equal("new", root.Text());
        await Assert.ThrowsAsync<InvalidOperationException>(() => new CascadingValueSource<int>(1, true).NotifyChangedAsync());
    }

    [Fact]
    public void Direct_parameters_cannot_override_cascading_parameters()
    {
        using var host = new TestHost();
        Assert.Throws<InvalidOperationException>(() => host.Render<CascadeReader>(
            ParameterView.FromDictionary(new Dictionary<string, object?> { ["Value"] = "direct" })));
    }

    [Fact]
    public void CascadingValue_requires_an_explicit_Value_parameter()
    {
        using var host = new TestHost();
        Assert.Throws<InvalidOperationException>(() => host.Render<CascadingValue<string>>());
    }

    private sealed class CascadeServices(params ICascadingValueSupplier[] sources) : IServiceProvider
    {
        public object? GetService(Type type) => type == typeof(IEnumerable<ICascadingValueSupplier>) ? sources : null;
    }
}

public sealed class CascadeHost : ComponentBase
{
    public string Outer { get; set; } = "outer";
    [Parameter] public bool Fixed { get; set; }
    public CascadeReader OuterReceiver { get; private set; } = null!;
    public CascadeReader InnerReceiver { get; private set; } = null!;
    public NamedReader NamedReceiver { get; private set; } = null!;
    public PlainReader Plain { get; private set; } = null!;
    public void Refresh() => StateHasChanged();

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenComponent<CascadingValue<string>>(0);
        builder.AddAttribute(1, "Value", Outer);
        builder.AddAttribute(2, "IsFixed", Fixed);
        builder.AddAttribute(3, "ChildContent", (RenderFragment)Children);
        builder.CloseComponent();
    }

    private void Children(RenderTreeBuilder builder)
    {
        builder.OpenComponent<CascadeReader>(0);
        builder.AddAttribute(1, "Prefix", "p:");
        builder.AddComponentReferenceCapture(2, c => OuterReceiver = (CascadeReader)c);
        builder.CloseComponent();
        builder.OpenComponent<CascadingValue<string>>(10);
        builder.AddAttribute(11, "Value", "inner");
        builder.AddAttribute(12, "ChildContent", (RenderFragment)(b =>
        {
            b.OpenComponent<CascadeReader>(0);
            b.AddComponentReferenceCapture(1, c => InnerReceiver = (CascadeReader)c);
            b.CloseComponent();
        }));
        builder.CloseComponent();
        builder.OpenComponent<CascadingValue<string>>(20);
        builder.AddAttribute(21, "Name", "theme");
        builder.AddAttribute(22, "Value", "named");
        builder.AddAttribute(23, "ChildContent", (RenderFragment)(b =>
        {
            b.OpenComponent<NamedReader>(0);
            b.AddComponentReferenceCapture(1, c => NamedReceiver = (NamedReader)c);
            b.CloseComponent();
        }));
        builder.CloseComponent();
        builder.OpenComponent<PlainReader>(30);
        builder.AddComponentReferenceCapture(31, c => Plain = (PlainReader)c);
        builder.CloseComponent();
    }
}

public class CascadeReader : ComponentBase
{
    [CascadingParameter] protected string Value { get; set; } = "fallback";
    [Parameter] public string Prefix { get; set; } = "";
    public int Renders { get; private set; }
    protected override void BuildRenderTree(RenderTreeBuilder builder) { Renders++; builder.AddContent(0, Prefix + Value); }
}

public sealed class NamedReader : ComponentBase
{
    [CascadingParameter(Name = "THEME")] private string Value { get; set; } = "fallback";
    public int Renders { get; private set; }
    protected override void BuildRenderTree(RenderTreeBuilder builder) { Renders++; builder.AddContent(0, Value); }
}

public sealed class PlainReader : ComponentBase
{
    public int Renders { get; private set; }
    protected override void BuildRenderTree(RenderTreeBuilder builder) { Renders++; builder.AddContent(0, "plain"); }
}

public sealed class NamedOnly : ComponentBase
{
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenComponent<CascadingValue<string>>(0);
        builder.AddAttribute(1, "Name", "theme");
        builder.AddAttribute(2, "Value", "named");
        builder.AddAttribute(3, "ChildContent", (RenderFragment)(b =>
        {
            b.OpenComponent<CascadeReader>(0); b.CloseComponent();
            b.OpenComponent<NamedReader>(1); b.CloseComponent();
        }));
        builder.CloseComponent();
    }
}

public sealed class Payload { public string Text { get; set; } = ""; }
public sealed class PayloadReader : ComponentBase
{
    [CascadingParameter] private Payload Value { get; set; } = null!;
    protected override void BuildRenderTree(RenderTreeBuilder builder) => builder.AddContent(0, Value.Text);
}
