using Nazor.Rendering;

namespace Nazor;

/// <summary>Marks a stylesheet supplied by a styling implementation.</summary>
public interface IStyleSheet;

/// <summary>Base generated for a component declared with <c>@window</c>; hosts instantiate it as a root.</summary>
public abstract class WindowComponentBase : ComponentBase
{
    private readonly Dictionary<int, RenderFragment> titleBars = [];
    private readonly Dictionary<int, (RenderFragment? Left, RenderFragment? Right)> captionButtons = [];
    private WindowTitleBarOutlet? outlet;

    /// <summary>The native window associated before parameter assignment and initialization.</summary>
    [Inject] public IWindow Window { get; set; } = null!;

    internal int TitleBarCount => titleBars.Count;
    internal int CaptionButtonsCount => captionButtons.Count;
    internal RenderFragment? UserCaptionButtons(bool left) => captionButtons.Count == 0 ? null :
        left ? captionButtons.Values.First().Left : captionButtons.Values.First().Right;
    internal void RegisterCaptionButtons(int id, RenderFragment? left, RenderFragment? right)
    {
        if (left is null && right is null) { if (!captionButtons.Remove(id)) return; }
        else captionButtons[id] = (left, right);
        outlet?.Refresh();
    }
    internal void Connect() { Window.Changed += Refresh; Window.ChromeChanged += Refresh; Window.SystemButtonsChanged += RefreshOutlet; }
    internal void Disconnect() { Window.Changed -= Refresh; Window.ChromeChanged -= Refresh; Window.SystemButtonsChanged -= RefreshOutlet; }
    private void RefreshOutlet() => outlet?.Refresh();
    private void Refresh() { outlet?.Refresh(); StateHasChanged(); }

    internal void RegisterTitleBar(int id, RenderFragment? content)
    {
        if (content is null) { if (!titleBars.Remove(id)) return; }
        else titleBars[id] = content;
        outlet?.Refresh();
    }

    /// <summary>Writes application content generated from the window's Nazor markup.</summary>
    protected virtual void BuildWindowContent(RenderTreeBuilder builder) { }

    /// <summary>Renders a stable implicit window root, a fixed caption destination and a WindowContent viewport with automatic overflow scrolling by default. Override overflow on WindowContent through NSS.</summary>
    protected sealed override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "Window");
        builder.AddAttribute(1, "role", "window");
        builder.AddAttribute(2, "style", $"{Window.Style}; display: flex; flex-direction: column; height: 100%");
        builder.AddAttribute(20, "class", Window.Class);
        builder.AddAttribute(21, "stylesheet", Window.StyleSheet);
        builder.AddAttribute(22, "title", Window.Title);
        builder.AddAttribute(23, "backdrop", Window.AppliedBackdrop.IsActive ? Window.AppliedBackdrop.Material : null);
        builder.AddAttribute(24, "client-titlebar", Window.Chrome.TitleBar is WindowChromeOwner.Nazor or WindowChromeOwner.User);
        builder.AddAttribute(25, "transparent-background", Window.BackgroundMode == BackgroundMode.Transparent);
        builder.AddAttribute(26, "window-state", Window.State.ToString().ToLowerInvariant());
        builder.AddAttribute(27, "frame-owner", Window.Chrome.Frame.ToString().ToLowerInvariant());
        builder.AddAttribute(28, "titlebar-owner", Window.Chrome.TitleBar.ToString().ToLowerInvariant());
        builder.AddAttribute(29, "caption-buttons-owner", Window.Chrome.CaptionButtons.ToString().ToLowerInvariant());
        builder.OpenComponent<WindowTitleBarOutlet>(3, GetType(), this);
        builder.AddComponentParameter(4, "Owner", this);
        builder.AddComponentReferenceCapture(5, component => outlet = (WindowTitleBarOutlet)component);
        builder.CloseComponent();
        builder.OpenElement(6, "WindowContent");
        builder.AddAttribute(7, "style", "display: flex; flex-direction: column; flex: 1; min-height: 0; min-width: 0");
        BuildWindowContent(builder);
        builder.CloseElement();
        builder.CloseElement();
    }

    internal sealed class WindowTitleBarOutlet : ComponentBase
    {
        [Parameter] public WindowComponentBase Owner { get; set; } = null!;
        internal void Refresh() => StateHasChanged();
        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            if (Owner.Window.State == WindowState.FullScreen) return;
            var content = Owner.Window as IWindowDecorationContent;
            if (Owner.Window.AppliedDecorations is { Configuration: var configuration })
            {
                if (configuration.HasNativeTitleBar || configuration.TitleBar == WindowTitleBarContent.None) return;
                if (configuration.TitleBar == WindowTitleBarContent.User)
                {
                    if (Owner.titleBars.Count != 0) builder.AddContent(0, Owner.titleBars.Values.First());
                }
                else if (content is not null) builder.AddContent(0, content.TitleBar);
                return;
            }
        }
    }
}
