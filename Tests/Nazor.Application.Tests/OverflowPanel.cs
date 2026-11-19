using Nazor.Controls;
using Nazor.Rendering;

namespace Nazor.Application.Tests;

public sealed class OverflowPanel : ComponentBase
{
    public string Overflow { get; set; } = "auto";
    public string Bars { get; set; } = "none";
    public bool Named { get; set; }
    public bool Fits { get; set; }
    public bool Disabled { get; set; }
    public bool AsButton { get; set; }
    public bool Smooth { get; set; }
    public int Clicks { get; private set; }
    public void Refresh() => StateHasChanged();

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenComponent(0, AsButton ? typeof(Button) : typeof(Container));
        builder.AddComponentParameter(1, "Id", "overflow-panel");
        builder.AddComponentParameter(2, "AccessibleName", Named ? "Scroll test" : null);
        builder.AddComponentParameter(3, "TabIndex", 0);
        builder.AddComponentParameter(4, "Style", $"width: 200px; height: 120px; overflow: {Overflow}; scrollbar-visibility: {Bars}; scroll-behavior: {(Smooth ? "smooth" : "auto")}; scroll-duration: 200ms; scroll-timing-function: linear;");
        builder.AddComponentParameter(5, "AdditionalAttributes", new Dictionary<string, object?>
        {
            ["disabled"] = Disabled,
            ["onclick"] = EventCallback.Factory.Create(this, () => Clicks++),
        });
        builder.AddComponentParameter(7, "ChildContent", (RenderFragment)(content =>
        {
            content.OpenComponent<TextBlock>(8);
            content.AddComponentParameter(9, "Style", Fits ? "width: 80px; height: 40px" : "width: 600px; height: 500px");
            content.AddComponentParameter(10, "ChildContent", (RenderFragment)(text => text.AddContent(11, "Overflow body")));
            content.CloseComponent();
        }));
        builder.CloseComponent();
    }
}
