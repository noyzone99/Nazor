using Nazor.Rendering;

namespace Nazor.Controls;

/// <summary>The height variant of a <see cref="TitleBar" />, written as the <c>height</c> attribute.</summary>
public enum TitleBarHeight
{
    /// <summary>The ordinary caption height, written as <c>standard</c>.</summary>
    Standard,
    /// <summary>A taller caption, for a search box or tabs in the title bar, written as <c>tall</c>.</summary>
    Tall,
}

/// <summary>
/// A composable client title-bar surface used by theme presenters and <see cref="UserTitleBar" />. It renders at its
/// destination without selecting decoration policy or registering a window declaration. Its empty areas drag the window
/// (<c>window-drag</c>), double-click maximizes it and a right click opens the system menu; interactive controls in it
/// keep their input, including field frames, custom pointer handlers and tooltips. It reserves the space of the system caption buttons when the window shows them
/// (<c>system-buttons-left-inset</c>/<c>right-inset</c>) and carries the <c>maximized</c>, <c>fullscreen</c>,
/// <c>show-system-buttons</c>, <c>can-minimize</c>, <c>can-maximize</c>, <c>can-resize</c> and <c>height</c> attributes.
/// Content goes into structured slots, in this order: <see cref="Leading" /> (back or pane buttons),
/// <see cref="AppIcon" />, the title (<see cref="Title" /> or <see cref="TitleContent" />) with its
/// <see cref="Subtitle" />, <see cref="Center" /> (a search box or tabs, taking the free space), the content between the
/// tags and <see cref="Trailing" /> (account, settings or <see cref="CaptionButtons" />); a title bar with only content
/// between the tags renders that content alone. The parts are <c>TitleBarLeading</c>, <c>TitleBarAppIcon</c>,
/// <c>TitleBarHeading</c> (with <c>TitleBarTitle</c> and <c>TitleBarSubtitle</c>), <c>TitleBarCenter</c> and
/// <c>TitleBarTrailing</c>.
/// </summary>
public class TitleBar : ContentControl, IDisposable
{
    private IWindow? subscribed;

    /// <summary>Gets or sets the window that owns this title bar, supplied automatically.</summary>
    [Inject] public IWindow Window { get; set; } = null!;

    /// <summary>Gets or sets content at the leading edge, such as a back button or a pane toggle.</summary>
    [Parameter] public RenderFragment? Leading { get; set; }

    /// <summary>Gets or sets the application icon, usually an <see cref="Image" /> or <see cref="Icon" />.</summary>
    [Parameter] public RenderFragment? AppIcon { get; set; }

    /// <summary>Gets or sets the title text, often the window title.</summary>
    [Parameter] public string? Title { get; set; }

    /// <summary>Gets or sets rich title content, used instead of <see cref="Title" />.</summary>
    [Parameter] public RenderFragment? TitleContent { get; set; }

    /// <summary>Gets or sets a secondary text after the title, such as the document or the environment.</summary>
    [Parameter] public string? Subtitle { get; set; }

    /// <summary>Gets or sets content in the middle that takes the free space, such as a search box or tabs.</summary>
    [Parameter] public RenderFragment? Center { get; set; }

    /// <summary>Gets or sets content at the trailing edge, such as an account button or caption buttons.</summary>
    [Parameter] public RenderFragment? Trailing { get; set; }

    /// <summary>Gets or sets the height variant.</summary>
    [Parameter] public TitleBarHeight Height { get; set; }

    private bool ShowSystemButtons => Window.AppliedDecorations?.Configuration.CaptionButtons == WindowCaptionButtons.Native;

    private bool Structured => Leading is not null || AppIcon is not null || Title is not null || TitleContent is not null ||
        Subtitle is not null || Center is not null || Trailing is not null;

    /// <summary>Renders the title-bar surface at the current destination.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void BuildRenderTree(RenderTreeBuilder builder) => base.BuildRenderTree(builder);

    /// <summary>The source attribute used to distinguish framework composition from application content.</summary>
    protected virtual string Source => "nazor";

    /// <summary>Gets the NSS element name emitted by TitleBar.</summary>
    protected override string ElementName => "TitleBar";

    /// <summary>Gets the inline NSS declarations, including the fixed placement at the top of the window.</summary>
    protected override string? ElementStyle => $"{base.ElementStyle}; width: 100%; flex-shrink: 0; position: relative; margin: 0; top: auto; right: auto; bottom: auto; left: auto; transform: none";

    /// <summary>Validates the parameters and follows the window's changes.</summary>
    protected override void OnParametersSet()
    {
        if (!Enum.IsDefined(Height)) throw new ArgumentOutOfRangeException(nameof(Height));
        if (ReferenceEquals(subscribed, Window)) return;
        Unsubscribe();
        subscribed = Window;
        subscribed.StateChanged += Refresh;
        subscribed.SystemButtonsChanged += Refresh;
        subscribed.CapabilitiesChanged += Refresh;
    }

    private void Refresh() => StateHasChanged();

    /// <summary>Adds the drag region, the role, the window state and the system button insets.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void AddAttributes(RenderTreeBuilder builder)
    {
        builder.AddAttribute(10, "window-drag", true);
        builder.AddAttribute(22, "source", Source);
        builder.AddAttribute(11, "role", "titlebar");
        builder.AddAttribute(12, "maximized", Window.State == WindowState.Maximized);
        builder.AddAttribute(13, "fullscreen", Window.State == WindowState.FullScreen);
        builder.AddAttribute(14, "show-system-buttons", ShowSystemButtons);
        builder.AddAttribute(15, "height", Height == TitleBarHeight.Tall ? "tall" : "standard");
        builder.AddAttribute(16, "can-minimize", Window.Capabilities.CanMinimize);
        builder.AddAttribute(17, "can-maximize", Window.Capabilities.CanMaximize);
        builder.AddAttribute(18, "can-resize", Window.Capabilities.CanResize);
        var metrics = ShowSystemButtons ? Window.SystemButtonsMetrics : default;
        builder.AddAttribute(19, "system-buttons-left-inset", metrics.LeftInset);
        builder.AddAttribute(20, "system-buttons-right-inset", metrics.RightInset);
        if (AccessibleName is null && Title is not null) builder.AddAttribute(21, "accessible-name", Title);
    }

    /// <summary>Adds the slots, or the content alone when no slot is used.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void AddContent(RenderTreeBuilder builder)
    {
        var decorationContent = Window as IWindowDecorationContent;
        builder.AddContent(980, decorationContent?.CaptionButtons(true));
        if (!Structured)
        {
            builder.OpenElement(990, "TitleBarClient");
            builder.AddAttribute(991, "style", "display: flex; align-items: center; flex: 1; min-width: 0");
            builder.AddContent(1000, ChildContent);
            builder.CloseElement();
            builder.AddContent(1070, decorationContent?.CaptionButtons(false));
            return;
        }
        AddPart(builder, 1010, "TitleBarLeading", Leading);
        AddPart(builder, 1020, "TitleBarAppIcon", AppIcon);
        if (Title is not null || TitleContent is not null || Subtitle is not null)
        {
            builder.OpenElement(1030, "TitleBarHeading");
            if (Title is not null || TitleContent is not null)
            {
                builder.OpenElement(1031, "TitleBarTitle");
                if (TitleContent is not null) builder.AddContent(1032, TitleContent);
                else builder.AddContent(1033, Title);
                builder.CloseElement();
            }
            if (Subtitle is not null)
            {
                builder.OpenElement(1034, "TitleBarSubtitle");
                builder.AddContent(1035, Subtitle);
                builder.CloseElement();
            }
            builder.CloseElement();
        }
        builder.OpenElement(1040, "TitleBarCenter");
        builder.AddContent(1041, Center);
        builder.CloseElement();
        builder.AddContent(1050, ChildContent);
        AddPart(builder, 1060, "TitleBarTrailing", Trailing);
        builder.AddContent(1070, decorationContent?.CaptionButtons(false));
    }

    private static void AddPart(RenderTreeBuilder builder, int sequence, string name, RenderFragment? content)
    {
        if (content is null) return;
        builder.OpenElement(sequence, name);
        builder.AddContent(sequence + 1, content);
        builder.CloseElement();
    }

    private void Unsubscribe()
    {
        if (subscribed is null) return;
        subscribed.StateChanged -= Refresh;
        subscribed.SystemButtonsChanged -= Refresh;
        subscribed.CapabilitiesChanged -= Refresh;
        subscribed = null;
    }

    /// <summary>Stops following the window.</summary>
    public virtual void Dispose()
    {
        Unsubscribe();
    }
}

/// <summary>
/// Marks its area as the maximize button for the native Snap Layouts of Windows (the <c>snap-layout-area</c> attribute):
/// resting the pointer on it shows the snap layouts. It has no commands or pointer handlers: the button inside it must
/// still maximize the window, and on other platforms it is an ordinary container. <see cref="CaptionButtons" /> marks its
/// maximize button already.
/// </summary>
public sealed class SnapLayoutArea : ContentControl
{
    /// <summary>Gets the NSS element name emitted by SnapLayoutArea.</summary>
    protected override string ElementName => "SnapLayoutArea";

    /// <summary>Adds the snap layout attribute.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void AddAttributes(RenderTreeBuilder builder) => builder.AddAttribute(10, "snap-layout-area", true);
}

/// <summary>
/// An area that drags the window, for custom title areas outside a <see cref="TitleBar" />, such as the top of a side
/// pane that extends into the caption: its empty space moves the window (<c>window-drag</c>), double-click maximizes
/// it, and interactive controls inside it keep their input. It only works where the window draws its content in the
/// caption area supported by its selected decoration configuration.
/// </summary>
public sealed class DragRegion : ContentControl
{
    /// <summary>Gets the NSS element name emitted by DragRegion.</summary>
    protected override string ElementName => "DragRegion";

    /// <summary>Adds the drag attribute.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void AddAttributes(RenderTreeBuilder builder) => builder.AddAttribute(10, "window-drag", true);
}
