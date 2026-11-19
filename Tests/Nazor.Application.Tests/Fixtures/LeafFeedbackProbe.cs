using Nazor.Controls;
using Nazor.Controls.Primitives;
using Nazor.Rendering;
using Nazor.Samples.Gallery.Showcase;

namespace Nazor.Application.Tests.Fixtures;

public sealed class LeafFeedbackProbe : ComponentBase
{
    [Parameter] public TimeProvider Time { get; set; } = TimeProvider.System;
    public ToastQueue Notifications { get; private set; } = null!;
    private bool visible = true;

    protected override void OnInitialized() => Notifications = new ToastQueue(Time);

    public void HideFeedback() { visible = false; StateHasChanged(); }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenComponent<ToastHost>(0);
        builder.AddComponentParameter(1, nameof(ToastHost.Queue), Notifications);
        builder.AddComponentParameter(2, nameof(ToastHost.Time), Time);
        builder.AddComponentParameter(3, nameof(ToastHost.MaxVisible), 20);
        builder.AddComponentParameter(4, nameof(ToastHost.ChildContent), (RenderFragment)(content =>
        {
            if (!visible) return;
            content.OpenComponent<FeedbackSection>(0);
            content.AddComponentParameter(1, nameof(FeedbackSection.Time), Time);
            content.CloseComponent();
        }));
        builder.CloseComponent();
    }
}
