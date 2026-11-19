using Nazor;
using Nazor.Rendering;

namespace Nazor.Sample.Components;

public abstract class CardBase : ComponentBase
{
    [Parameter] public string Name { get; set; } = "";
    [Parameter] public RenderFragment? ChildContent { get; set; }
}

public sealed class InheritedCard : CardBase
{
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.AddContent(0, Name);
        builder.AddContent(1, ChildContent);
    }
}
