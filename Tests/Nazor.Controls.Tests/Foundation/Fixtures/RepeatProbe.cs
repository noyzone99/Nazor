using Nazor.Controls.Primitives;
using Nazor.Controls.Tests.Feedback;
using Nazor.Input;

namespace Nazor.Controls.Tests.Foundation.Fixtures;

public sealed class RepeatProbe : ComponentBase
{
    public FeedbackClock Clock { get; set; } = new();
    public bool Primitive { get; set; }
    public bool Enabled { get; set; } = true;
    public bool Disabled { get; set; }
    public bool ParentDisabled { get; set; }
    public bool Busy { get; set; }
    public bool Visible { get; set; } = true;
    public bool LongPress { get; set; }
    public int Actions { get; private set; }
    public int LongActions { get; private set; }
    public void Refresh() => StateHasChanged();
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "Container");
        builder.AddAttribute(1, "disabled", ParentDisabled);
        builder.OpenComponent<CascadingValue<TimeProvider>>(10);
        builder.AddComponentParameter(11, nameof(CascadingValue<TimeProvider>.Value), Clock);
        builder.AddComponentParameter(12, nameof(CascadingValue<TimeProvider>.ChildContent), (RenderFragment)(content =>
        {
            if (!Visible) return;
            content.OpenComponent(0, Primitive ? typeof(Pressable) : typeof(Button));
            content.AddComponentParameter(1, nameof(Control.Id), "held");
            content.AddComponentParameter(2, "Disabled", Disabled);
            content.AddComponentParameter(3, "RepeatInterval", Enabled && !LongPress ? TimeSpan.FromMilliseconds(100) : (TimeSpan?)null);
            content.AddComponentParameter(4, "RepeatDelay", TimeSpan.FromMilliseconds(400));
            content.AddComponentParameter(5, Primitive ? nameof(Pressable.OnPress) : nameof(Button.OnClick), EventCallback.Factory.Create<PointerEventArgs>(this, () => Actions++));
            content.AddComponentParameter(6, nameof(ContentControl.ChildContent), (RenderFragment)(text => text.AddContent(0, "Hold")));
            if (Primitive)
            {
                content.AddComponentParameter(7, nameof(Pressable.As), "Button");
                if (LongPress) content.AddComponentParameter(8, nameof(Pressable.OnLongPress), EventCallback.Factory.Create<PointerEventArgs>(this, () => LongActions++));
            }
            else content.AddComponentParameter(9, nameof(Button.IsBusy), Busy);
            content.CloseComponent();
        }));
        builder.CloseComponent();
        builder.CloseElement();
        builder.OpenComponent<Button>(20);
        builder.AddComponentParameter(21, nameof(Control.Id), "outside");
        builder.AddComponentParameter(22, nameof(Button.ChildContent), (RenderFragment)(text => text.AddContent(0, "Outside")));
        builder.CloseComponent();
    }
}
