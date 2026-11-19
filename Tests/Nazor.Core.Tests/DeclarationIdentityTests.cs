using Nazor.Rendering;
using Nazor.Testing;

namespace Nazor.Core.Tests;

public sealed class DeclarationIdentityPage : ComponentBase
{
    public IComponent? Origin { get; set; }
    public void Refresh() => StateHasChanged();

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        var origin = Origin ?? this;
        builder.OpenComponent<Label>(0, typeof(DeclarationIdentityPage), origin);
        builder.AddComponentParameter(1, "Text", "Projected");
        builder.CloseComponent();
    }
}

public sealed class DeclarationIdentityTests
{
    [Fact]
    public void Reused_frames_keep_identity_only_while_the_lexical_declaring_instance_stays_the_same()
    {
        using var host = new TestHost();
        var root = host.Render<DeclarationIdentityPage>();
        var page = (DeclarationIdentityPage)root.Component;
        var first = root.Single("Label");
        Assert.Same(page, first.DeclaringComponent);
        page.Refresh();
        host.RunPending();
        Assert.Same(first, root.Single("Label"));
        page.Origin = new DeclarationIdentityPage();
        page.Refresh();
        host.RunPending();
        Assert.NotSame(first, root.Single("Label"));
        Assert.True(first.IsRemoved);
        Assert.Same(page.Origin, root.Single("Label").DeclaringComponent);
    }
}
