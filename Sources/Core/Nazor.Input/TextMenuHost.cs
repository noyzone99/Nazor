using Nazor.Rendering;

namespace Nazor.Input;

internal sealed record TextMenuItem(string Label, bool Enabled, Action Run);

internal sealed class TextMenuHost : ComponentBase
{
    private IReadOnlyList<TextMenuItem>? items;
    private double x, y;

    public bool IsOpen => items is not null;

    public void Show(double atX, double atY, IReadOnlyList<TextMenuItem> commands)
    {
        (items, x, y) = (commands, atX, atY);
        StateHasChanged();
    }

    public void Hide()
    {
        if (items is null) return;
        items = null;
        StateHasChanged();
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        if (items is null) return;
        builder.OpenElement(0, "ContextMenu");
        builder.AddAttribute(1, "overlay", true);
        builder.AddAttribute(2, "anchor-x", x);
        builder.AddAttribute(3, "anchor-y", y);
        builder.AddAttribute(4, "light-dismiss", true);
        builder.AddAttribute(5, "__default_ondismiss", EventCallback.Factory.Create(this, Hide));
        builder.AddAttribute(6, "focus-first", true);
        builder.AddAttribute(7, "menu", true);
        builder.AddAttribute(8, "role", "menu");
        for (var i = 0; i < items.Count; i++)
        {
            var item = items[i];
            builder.OpenElement(10, "MenuItem");
            builder.SetKey(i);
            builder.AddAttribute(11, "role", "menuitem");
            builder.AddAttribute(12, "disabled", !item.Enabled);
            builder.AddAttribute(13, "focusable", item.Enabled);
            builder.AddAttribute(14, "__default_onclick", EventCallback.Factory.Create<PointerEventArgs>(this, () =>
            {
                Hide();
                item.Run();
            }));
            builder.AddContent(15, item.Label);
            builder.CloseElement();
        }
        builder.CloseElement();
    }
}
