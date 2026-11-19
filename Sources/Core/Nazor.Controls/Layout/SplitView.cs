using Nazor.Controls.Primitives;
using Nazor.Input;
using Nazor.Rendering;

namespace Nazor.Controls;

/// <summary>Which side of a <see cref="SplitView" /> holds the pane, written as the <c>placement</c> attribute.</summary>
public enum SplitViewPanePlacement
{
    /// <summary>The left side, or the top in a vertical split view.</summary>
    Start,
    /// <summary>The right side, or the bottom in a vertical split view.</summary>
    End,
}

/// <summary>How the pane of a <see cref="SplitView" /> shares the space with the content, written as the <c>display-mode</c> attribute.</summary>
public enum SplitViewDisplayMode
{
    /// <summary>The open pane sits beside the content, which shrinks; a handle between them resizes it.</summary>
    Inline,
    /// <summary>
    /// The open pane covers the content; a click on the content or Escape closes it. The closed pane takes no space.
    /// </summary>
    Overlay,
    /// <summary>
    /// The closed pane stays visible as a narrow strip of <see cref="SplitView.CompactPaneSize" />, for icons; the open
    /// pane covers the content at its full size, and a click on the content or Escape closes it.
    /// </summary>
    Compact,
}

/// <summary>
/// A pane beside the main content, such as a navigation pane or a list next to its details. The pane
/// (<see cref="Pane" />) is <see cref="PaneSize" /> wide (<c>@bind-PaneSize</c>), or high in a vertical split view; in the
/// <see cref="SplitViewDisplayMode.Inline" /> mode a <see cref="ResizeHandle" /> between pane and content resizes it by
/// dragging or with the arrow keys (Home and End jump to <see cref="MinPaneSize" /> and <see cref="MaxPaneSize" />).
/// <see cref="IsPaneOpen" /> (<c>@bind-IsPaneOpen</c>) opens and closes the pane, and <see cref="DisplayMode" /> chooses
/// whether the open pane pushes or covers the content. The parts are <c>SplitViewPane</c> (with the <c>compact</c>
/// attribute while showing the narrow strip), <c>ResizeHandle</c>, <c>SplitViewContent</c> and, while an overlaid pane
/// is open, <c>SplitViewScrim</c>. The element has the <c>orientation</c>, <c>placement</c>, <c>display-mode</c> and
/// <c>pane-open</c> attributes.
/// </summary>
public sealed class SplitView : ContentControl
{
    private readonly ControllableState<double> size = new(280);
    private readonly ControllableState<bool> open = new(true);

    /// <summary>Gets or sets the content of the pane.</summary>
    [Parameter] public RenderFragment? Pane { get; set; }

    /// <summary>Gets or sets the side that holds the pane.</summary>
    [Parameter] public SplitViewPanePlacement PanePlacement { get; set; }

    /// <summary>Gets or sets whether pane and content sit side by side (horizontal) or one above the other (vertical).</summary>
    [Parameter] public Orientation Orientation { get; set; }

    /// <summary>Gets or sets how the pane shares the space with the content.</summary>
    [Parameter] public SplitViewDisplayMode DisplayMode { get; set; }

    /// <summary>Gets or sets the size of the open pane: its width, or its height in a vertical split view.</summary>
    [Parameter] public double PaneSize { get; set; } = 280;

    /// <summary>Gets or sets the callback invoked when the user resizes the pane.</summary>
    [Parameter] public EventCallback<double> PaneSizeChanged { get; set; }

    /// <summary>Gets or sets the smallest size the user can give the pane.</summary>
    [Parameter] public double MinPaneSize { get; set; } = 120;

    /// <summary>Gets or sets the largest size the user can give the pane.</summary>
    [Parameter] public double MaxPaneSize { get; set; } = 600;

    /// <summary>Gets or sets the size of the closed pane in the compact mode.</summary>
    [Parameter] public double CompactPaneSize { get; set; } = 48;

    /// <summary>Gets or sets whether the pane is open; true by default.</summary>
    [Parameter] public bool IsPaneOpen { get; set; } = true;

    /// <summary>Gets or sets the callback invoked when the pane closes by a click on the content or Escape.</summary>
    [Parameter] public EventCallback<bool> IsPaneOpenChanged { get; set; }

    /// <summary>Gets or sets whether an inline pane can be resized; true by default.</summary>
    [Parameter] public bool IsResizable { get; set; } = true;

    /// <summary>Gets or sets the change made by one arrow key on the resize handle.</summary>
    [Parameter] public double KeyboardStep { get; set; } = 10;

    /// <summary>Gets or sets the name of the pane for assistive technologies, which makes it a group.</summary>
    [Parameter] public string? PaneAccessibleName { get; set; }

    /// <summary>Gets or sets the name of the resize handle for assistive technologies; a localized default otherwise.</summary>
    [Parameter] public string? ResizeHandleAccessibleName { get; set; }

    /// <summary>Gets the current size of the open pane.</summary>
    public double CurrentPaneSize => size.Value;

    /// <summary>Gets whether the pane is currently open.</summary>
    public bool PaneOpen => open.Value;

    /// <summary>Gets whether the pane currently shows the narrow strip of the compact mode.</summary>
    public bool IsCompact => DisplayMode == SplitViewDisplayMode.Compact && !open.Value;

    private bool Covers => DisplayMode != SplitViewDisplayMode.Inline && open.Value;

    private bool Vertical => Orientation == Orientation.Vertical;

    /// <summary>Gets the NSS element name emitted by SplitView.</summary>
    protected override string ElementName => "SplitView";

    /// <summary>Validates the parameters and synchronizes the size and the open state.</summary>
    protected override void OnParametersSet()
    {
        if (!Enum.IsDefined(PanePlacement)) throw new ArgumentOutOfRangeException(nameof(PanePlacement));
        if (!Enum.IsDefined(Orientation)) throw new ArgumentOutOfRangeException(nameof(Orientation));
        if (!Enum.IsDefined(DisplayMode)) throw new ArgumentOutOfRangeException(nameof(DisplayMode));
        if (!double.IsFinite(MinPaneSize) || MinPaneSize < 0) throw new ArgumentOutOfRangeException(nameof(MinPaneSize));
        if (double.IsNaN(MaxPaneSize) || MaxPaneSize < MinPaneSize) throw new ArgumentOutOfRangeException(nameof(MaxPaneSize));
        if (!double.IsFinite(PaneSize) || PaneSize < 0) throw new ArgumentOutOfRangeException(nameof(PaneSize));
        if (!double.IsFinite(CompactPaneSize) || CompactPaneSize < 0) throw new ArgumentOutOfRangeException(nameof(CompactPaneSize));
        if (!double.IsFinite(KeyboardStep) || KeyboardStep <= 0) throw new ArgumentOutOfRangeException(nameof(KeyboardStep));
        size.Sync(PaneSize);
        open.Sync(IsPaneOpen);
    }

    /// <summary>Adds the layout attributes and the Escape handling of a covering pane.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void AddAttributes(RenderTreeBuilder builder)
    {
        builder.AddAttribute(10, "orientation", Vocabulary.Name(Orientation));
        builder.AddAttribute(11, "placement", PanePlacement == SplitViewPanePlacement.End ? "end" : "start");
        builder.AddAttribute(12, "display-mode", DisplayMode switch
        {
            SplitViewDisplayMode.Overlay => "overlay", SplitViewDisplayMode.Compact => "compact", _ => "inline",
        });
        builder.AddAttribute(13, "pane-open", open.Value);
    }

    /// <summary>Adds the pane, the resize handle and the content in visual order.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void AddContent(RenderTreeBuilder builder)
    {
        if (PanePlacement == SplitViewPanePlacement.Start)
        {
            AddPane(builder);
            AddHandle(builder);
            AddMain(builder);
        }
        else
        {
            AddMain(builder);
            AddHandle(builder);
            AddPane(builder);
        }
    }

    private void AddPane(RenderTreeBuilder builder)
    {
        if (!open.Value && DisplayMode != SplitViewDisplayMode.Compact) return;
        var extent = open.Value ? size.Value : CompactPaneSize;
        builder.OpenElement(1000, "SplitViewPane");
        builder.AddAttribute(1001, "style", (Vertical ? "height: " : "width: ") + Css.Px(extent));
        builder.AddAttribute(1002, "compact", IsCompact);
        builder.AddAttribute(1003, "role", PaneAccessibleName is null ? null : "group");
        builder.AddAttribute(1004, "accessible-name", PaneAccessibleName);
        builder.AddAttribute(1005, "__internal_onkeydown#splitview", EventCallback.Factory.Create<KeyboardEventArgs>(this, PaneKeyDownAsync));
        builder.AddContent(1006, Pane);
        builder.CloseElement();
    }

    private void AddHandle(RenderTreeBuilder builder)
    {
        if (!IsResizable || !open.Value || DisplayMode != SplitViewDisplayMode.Inline) return;
        var end = PanePlacement == SplitViewPanePlacement.End;
        var max = Math.Max(MinPaneSize, MaxPaneSize);
        builder.OpenComponent<ResizeHandle>(1100);
        builder.AddComponentParameter(1101, nameof(ResizeHandle.Edge), Vertical ? end ? ResizeEdge.Top : ResizeEdge.Bottom : end ? ResizeEdge.Left : ResizeEdge.Right);
        builder.AddComponentParameter(1102, nameof(ResizeHandle.Width), size.Value);
        builder.AddComponentParameter(1103, nameof(ResizeHandle.Height), size.Value);
        builder.AddComponentParameter(1104, nameof(ResizeHandle.MinSize), MinPaneSize);
        builder.AddComponentParameter(1105, nameof(ResizeHandle.MaxSize), max);
        builder.AddComponentParameter(1106, nameof(ResizeHandle.KeyboardStep), KeyboardStep);
        builder.AddComponentParameter(1107, nameof(ResizeHandle.OnResize), EventCallback.Factory.Create<ResizeValue>(this, v => ResizeAsync(Vertical ? v.Height : v.Width, false)));
        builder.AddComponentParameter(1108, nameof(ResizeHandle.OnResizeEnd), EventCallback.Factory.Create<ResizeValue>(this, v => ResizeAsync(Vertical ? v.Height : v.Width, true)));
        builder.AddComponentParameter(1109, nameof(Control.AccessibleName), ResizeHandleAccessibleName ?? ControlStrings.Get("ResizePane"));
        builder.AddComponentParameter(1110, "range-min", MinPaneSize);
        builder.AddComponentParameter(1111, "range-max", double.IsFinite(max) ? max : Math.Max(MinPaneSize, size.Value));
        builder.AddComponentParameter(1112, "range-value", size.Value);
        builder.AddComponentParameter(1113, "__internal_onkeydown#splitview", EventCallback.Factory.Create<KeyboardEventArgs>(this, HandleKeyDownAsync));
        builder.CloseComponent();
    }

    private void AddMain(RenderTreeBuilder builder)
    {
        builder.OpenElement(1200, "SplitViewContent");
        if (DisplayMode == SplitViewDisplayMode.Compact)
            builder.AddAttribute(1201, "style", "margin-" + (Vertical ? PanePlacement == SplitViewPanePlacement.End ? "bottom" : "top" : PanePlacement == SplitViewPanePlacement.End ? "right" : "left") + ": " + Css.Px(CompactPaneSize));
        builder.AddContent(1202, ChildContent);
        builder.CloseElement();
        if (!Covers) return;
        builder.OpenElement(1210, "SplitViewScrim");
        builder.AddAttribute(1211, "semantic-hidden", true);
        builder.AddAttribute(1212, "onclick", EventCallback.Factory.Create<PointerEventArgs>(this, () => SetPaneOpenAsync(false)));
        builder.CloseElement();
    }

    private async Task ResizeAsync(double value, bool commit)
    {
        value = Math.Clamp(value, MinPaneSize, Math.Max(MinPaneSize, MaxPaneSize));
        var changed = size.Set(value);
        if (changed)
        {
            StateHasChanged();
            await PaneSizeChanged.InvokeAsync(value);
        }
        if (changed || commit) await Events.RaiseChangeAsync(Element, value, commit);
    }

    private async Task HandleKeyDownAsync(KeyboardEventArgs e)
    {
        if (e.Handled || e.Modifiers != KeyModifiers.None || e.Key is not ("Home" or "End")) return;
        e.Handled = true;
        var max = Math.Max(MinPaneSize, MaxPaneSize);
        var target = e.Key == "Home" ? MinPaneSize : double.IsFinite(max) ? max : size.Value;
        await ResizeAsync(target, true);
    }

    private async Task PaneKeyDownAsync(KeyboardEventArgs e)
    {
        if (e.Handled || e.Key != "Escape" || !Covers) return;
        e.Handled = true;
        await SetPaneOpenAsync(false);
    }

    /// <summary>Opens or closes the pane as the user does and reports the change.</summary>
    /// <param name="value">Whether the pane should be open.</param>
    /// <returns>A task that completes when the change has been reported.</returns>
    public async Task SetPaneOpenAsync(bool value)
    {
        if (!open.Set(value)) return;
        StateHasChanged();
        await IsPaneOpenChanged.InvokeAsync(value);
    }

    /// <summary>Opens the pane when closed and closes it when open.</summary>
    /// <returns>A task that completes when the change has been reported.</returns>
    public Task TogglePaneAsync() => SetPaneOpenAsync(!open.Value);
}
