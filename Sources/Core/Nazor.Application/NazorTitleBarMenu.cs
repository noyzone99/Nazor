using Nazor.Controls;
using Nazor.Graphics;
using Nazor.Input;
using Nazor.Rendering;

namespace Nazor;

internal sealed class NazorTitleBarMenu : ComponentBase, IDisposable
{
    private (double X, double Y)? anchor;

    public NazorTitleBarMenu() { }

    [Inject] public IWindow Window { get; set; } = null!;

    protected override void OnInitialized() => Window.Changed += Refresh;

    internal void Open(Point point)
    {
        anchor = (point.X, point.Y);
        StateHasChanged();
    }

    internal void Dismiss()
    {
        if (anchor is null) return;
        anchor = null;
        StateHasChanged();
    }

    private void Refresh()
    {
        if (anchor is not null) StateHasChanged();
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        if (anchor is null) return;
        builder.OpenComponent<Menu>(0);
        builder.AddComponentParameter(1, nameof(Menu.IsOpen), true);
        builder.AddComponentParameter(2, nameof(Menu.IsOpenChanged), EventCallback.Factory.Create<bool>(this, open => { if (!open) Dismiss(); }));
        builder.AddComponentParameter(3, nameof(Menu.AnchorPoint), anchor);
        builder.AddComponentParameter(4, nameof(Menu.AccessibleName), Strings.Get("WindowMenu"));
        builder.AddComponentParameter(5, nameof(Menu.ChildContent), (RenderFragment)(items =>
        {
            AddCommand(items, 0, WindowCommand.Restore);
            AddCommand(items, 1, WindowCommand.Minimize);
            AddCommand(items, 2, WindowCommand.Maximize);
            AddCommand(items, 3, WindowCommand.EnterFullScreen);
            AddItem(items, 4, "Close", false, () => Window.Close());
        }));
        builder.CloseComponent();
    }

    private void AddCommand(RenderTreeBuilder builder, int sequence, WindowCommand command)
        => AddItem(builder, sequence, command.ToString(), !Window.CanExecute(command), () => Window.Request(command));

    private void AddItem(RenderTreeBuilder builder, int sequence, string label, bool disabled, Action click)
    {
        builder.OpenComponent<MenuItem>(sequence);
        builder.AddComponentParameter(0, nameof(MenuItem.ChildContent), (RenderFragment)(content => content.AddContent(0, Strings.Get(label))));
        builder.AddComponentParameter(1, nameof(MenuItem.Disabled), disabled);
        builder.AddComponentParameter(2, nameof(MenuItem.CloseOnSelect), false);
        builder.AddComponentParameter(3, nameof(MenuItem.OnClick), EventCallback.Factory.Create<PointerEventArgs>(this, _ =>
        {
            Dismiss();
            click();
        }));
        builder.CloseComponent();
    }

    public void Dispose() => Window.Changed -= Refresh;
}
