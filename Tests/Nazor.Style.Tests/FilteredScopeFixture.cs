using Nazor.Rendering;

namespace Nazor.Style.Tests;

public sealed class FilteredScopeFixture : ComponentBase
{
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenComponent<Box>(0, typeof(FilteredScopeFixture));
        builder.AddComponentParameter(1, "Class", "outer");
        builder.AddComponentParameter(2, "ChildContent", (RenderFragment)(children =>
        {
            children.OpenComponent<Badge>(0, typeof(FilteredScopeFixture));
            children.CloseComponent();
            children.OpenComponent<Box>(1, typeof(FilteredScopeFixture));
            children.AddComponentParameter(2, "Class", "inner own");
            children.CloseComponent();
        }));
        builder.CloseComponent();
    }
}
