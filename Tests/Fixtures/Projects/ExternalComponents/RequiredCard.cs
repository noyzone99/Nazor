using Nazor;

namespace Nazor.Sample.Components;

public abstract class RequiredCardBase : ComponentBase
{
    [Parameter, EditorRequired] public virtual string? Label { get; set; } = "default";
    [Parameter] public required string Value { get; set; }
    [Parameter] public string Optional { get; set; } = "default";
    [Parameter] public RenderFragment? ChildContent { get; set; }
}

public sealed class RequiredCard : RequiredCardBase
{
    public override string? Label { get; set; }
}
