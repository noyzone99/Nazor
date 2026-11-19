using Nazor.Controls;
using Nazor.Controls.Primitives;
using Nazor.Rendering;
using Nazor.Samples.Gallery.Showcase;

namespace Nazor.Application.Tests.Fixtures;

public sealed class LeafActionsProbe : ComponentBase
{
    [Parameter] public bool Data { get; set; }
    [Parameter] public TimeProvider Time { get; set; } = TimeProvider.System;
    public ToastQueue Notifications { get; private set; } = null!;
    public bool Motion { get; private set; } = true;
    public int MotionChanges { get; private set; }

    protected override void OnInitialized() => Notifications = new ToastQueue(Time);

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenComponent<ToastHost>(0);
        builder.AddComponentParameter(1, nameof(ToastHost.Queue), Notifications);
        builder.AddComponentParameter(2, nameof(ToastHost.Time), Time);
        builder.AddComponentParameter(3, nameof(ToastHost.MaxVisible), 20);
        builder.AddComponentParameter(4, nameof(ToastHost.ChildContent), (RenderFragment)(content =>
        {
            content.OpenComponent(0, Data ? typeof(DataSection) : typeof(MenusSection));
            if (Data)
            {
                content.AddComponentParameter(1, nameof(DataSection.SeedName), "Verde");
                content.AddComponentParameter(2, nameof(DataSection.Motion), Motion);
                content.AddComponentParameter(3, nameof(DataSection.MotionChanged), EventCallback.Factory.Create<bool>(this, value => { Motion = value; MotionChanges++; }));
            }
            content.CloseComponent();
        }));
        builder.CloseComponent();
    }
}
