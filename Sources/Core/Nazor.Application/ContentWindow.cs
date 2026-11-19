using Nazor.Rendering;

namespace Nazor;

internal sealed class ContentWindow : WindowComponentBase
{
    [Parameter] public Type ComponentType { get; set; } = null!;
    [Parameter] public ParameterView ContentParameters { get; set; }
    protected override void BuildWindowContent(RenderTreeBuilder builder)
    {
        builder.OpenComponent(0, ComponentType);
        foreach (var parameter in ContentParameters)
            builder.AddComponentParameter(1, parameter.Name, parameter.Value);
        builder.CloseComponent();
    }
}
