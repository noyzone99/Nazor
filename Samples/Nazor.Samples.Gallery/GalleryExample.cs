using Nazor.Rendering;
namespace Nazor.Samples.Gallery;
public sealed class GalleryExample : ComponentBase
{
    [Parameter] public Type SampleType { get; set; } = null!;
    protected override void BuildRenderTree(RenderTreeBuilder builder) { builder.OpenComponent(0, SampleType); builder.CloseComponent(); }
}
