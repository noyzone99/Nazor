using Nazor;
using Nazor.Controls;
using Nazor.Rendering;

namespace Nazor.Sample.Components;

public sealed class ExternalPanel : ComponentBase
{
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenComponent<TextBlock>(0);
        builder.AddComponentParameter(1, "ChildContent", (RenderFragment)(b => b.AddContent(0, "External panel")));
        builder.CloseComponent();
        builder.OpenComponent<ExternalDetails>(2);
        builder.CloseComponent();
    }
}

public sealed class ExternalDetails : ComponentBase
{
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenComponent<TextBlock>(0);
        builder.AddComponentParameter(1, "ChildContent", (RenderFragment)(b => b.AddContent(0, "External details")));
        builder.CloseComponent();
    }
}
