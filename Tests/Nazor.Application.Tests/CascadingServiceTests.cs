using Microsoft.Extensions.DependencyInjection;
using Nazor.Rendering;
using Nazor.Testing;

namespace Nazor.Application.Tests;

public sealed class CascadingServiceTests
{
    [Fact]
    public async Task DI_root_cascades_are_named_scoped_and_can_notify()
    {
        var services = new ServiceCollection();
        services.AddCascadingValue(_ => "unnamed");
        services.AddCascadingValue("label", _ => "named");
        services.AddScoped(_ => new CascadingValueSource<int>(1, isFixed: false));
        services.AddCascadingValue<int>(provider => provider.GetRequiredService<CascadingValueSource<int>>());
        using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        using var scope = provider.CreateScope();
        using var other = provider.CreateScope();
        using var host = new TestHost(scope.ServiceProvider);
        var root = host.Render<ServiceCascadeReader>();
        Assert.Equal("unnamed/named/1", root.Text());
        await scope.ServiceProvider.GetRequiredService<CascadingValueSource<int>>().NotifyChangedAsync(2);
        Assert.Equal("unnamed/named/2", root.Text());
        Assert.NotSame(scope.ServiceProvider.GetRequiredService<CascadingValueSource<int>>(), other.ServiceProvider.GetRequiredService<CascadingValueSource<int>>());
        Assert.Equal(2, ((ServiceCascadeReader)root.Component).ParametersSet);
    }
}

public sealed class ServiceCascadeReader : ComponentBase
{
    [CascadingParameter] public string Value { get; set; } = "";
    [CascadingParameter(Name = "label")] private string Label { get; set; } = "";
    [CascadingParameter] private int Number { get; set; }
    public int ParametersSet { get; private set; }
    protected override void OnParametersSet() => ParametersSet++;
    protected override void BuildRenderTree(RenderTreeBuilder builder) => builder.AddContent(0, $"{Value}/{Label}/{Number}");
}
