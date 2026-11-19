namespace Nazor.Samples.Tasks.Components;

/// <summary>A reusable heading whose data and optional trailing content are provided by its parent.</summary>
public partial class SectionHeader
{
    // [Parameter] exposes an input to markup; ordinary public properties alone are not component parameters.
    [Parameter]
    public string Title { get; set; } = "";
    [Parameter]
    public string Description { get; set; } = "";

    // A fragment is deferred UI content, not a pre-rendered string, so callers can pass arbitrary components.
    [Parameter]
    public RenderFragment? ChildContent { get; set; }
}
