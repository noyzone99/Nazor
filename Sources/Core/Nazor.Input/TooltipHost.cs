using Nazor.Rendering;

namespace Nazor.Input;

internal sealed class TooltipHost : ComponentBase, IDisposable
{
    private ElementNode? target;
    private string? text;
    private bool open;
    private bool reentered;
    private ElementReference surface;
    private PresenceLifetime? lifetime;
    private PresenceMotion? motion;

    public void Show(ElementNode element, string value)
    {
        if (open && target == element && text == value) return;
        reentered = target == element && (reentered || lifetime?.Exiting == true);
        (target, text) = (element, value);
        open = true;
        _ = UpdateAsync();
    }

    public void Hide()
    {
        if (!open) return;
        open = false;
        _ = UpdateAsync();
    }

    private async Task UpdateAsync()
    {
        lifetime ??= new(InvokeAsync, StateHasChanged, () =>
        {
            (target, text) = (null, null);
            reentered = false;
            return Task.CompletedTask;
        });
        motion ??= new(() => _ = InvokeAsync(UpdateAsync));
        await lifetime.UpdateAsync(open, motion.Duration(surface.Element));
        StateHasChanged();
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        if (lifetime?.Rendered != true || target is null || text is null) return;
        builder.OpenElement(0, "Tooltip");
        builder.SetKey(target);
        builder.AddAttribute(1, "overlay", true);
        builder.AddAttribute(2, "anchor", target);
        builder.AddAttribute(3, "placement", "bottom");
        builder.AddAttribute(4, "light-dismiss", false);
        builder.AddAttribute(5, "role", "tooltip");
        builder.AddAttribute(6, "exiting", lifetime.Exiting);
        builder.AddAttribute(7, "semantic-hidden", lifetime.Exiting);
        builder.AddAttribute(8, "style", "pointer-events: none");
        builder.AddAttribute(11, "reentered", reentered);
        builder.AddElementReferenceCapture(9, reference => surface = reference);
        builder.AddContent(10, text);
        builder.CloseElement();
    }

    public void Dispose()
    {
        lifetime?.Dispose();
        motion?.Dispose();
    }
}
