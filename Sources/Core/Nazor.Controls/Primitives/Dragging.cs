using Nazor.Input;
using Nazor.Rendering;

namespace Nazor.Controls.Primitives;

/// <summary>The progress of a drag reported by a <see cref="Draggable" />.</summary>
/// <param name="StartX">The pointer's horizontal position when the drag started, in window coordinates.</param>
/// <param name="StartY">The pointer's vertical position when the drag started.</param>
/// <param name="DeltaX">The horizontal distance moved since the start.</param>
/// <param name="DeltaY">The vertical distance moved since the start.</param>
/// <param name="Modifiers">The modifier keys held.</param>
public readonly record struct DragEventArgs(double StartX, double StartY, double DeltaX, double DeltaY, KeyModifiers Modifiers);

/// <summary>
/// Makes content draggable without imposing a look: after the pointer moves <see cref="Threshold" /> units with the
/// primary button held, <see cref="OnDragStart" />, <see cref="OnDrag" /> and <see cref="OnDragEnd" /> report the movement
/// from the start; losing pointer capture cancels the drag without <see cref="OnDragEnd" />. Movement can be limited to one
/// <see cref="Axis" />. Splitters, resize handles, sliders of custom shape, color areas and movable dialogs are built on it.
/// The element is named by <see cref="As" />; it is <c>dragging</c> while a drag is in progress.
/// </summary>
public sealed class Draggable : ContentControl
{
    private (double X, double Y)? start;
    private bool dragging;

    /// <summary>Gets or sets the element name, <c>Draggable</c> by default.</summary>
    [Parameter] public string As { get; set; } = "Draggable";

    /// <summary>Gets or sets the axis the movement is limited to, or null for both.</summary>
    [Parameter] public Orientation? Axis { get; set; }

    /// <summary>Gets or sets the distance the pointer must move before a drag starts.</summary>
    [Parameter] public double Threshold { get; set; } = 3;

    /// <summary>Gets or sets whether dragging is unavailable.</summary>
    [Parameter] public bool Disabled { get; set; }

    /// <summary>Gets or sets the callback invoked when the drag starts.</summary>
    [Parameter] public EventCallback<DragEventArgs> OnDragStart { get; set; }

    /// <summary>Gets or sets the callback invoked as the pointer moves.</summary>
    [Parameter] public EventCallback<DragEventArgs> OnDrag { get; set; }

    /// <summary>Gets or sets the callback invoked when the pointer is released after a drag.</summary>
    [Parameter] public EventCallback<DragEventArgs> OnDragEnd { get; set; }

    /// <summary>Gets whether a drag is in progress.</summary>
    public bool IsDragging => dragging;

    /// <summary>Gets the element name.</summary>
    protected override string ElementName => As;

    /// <summary>Validates the threshold.</summary>
    protected override void OnParametersSet()
    {
        if (!double.IsFinite(Threshold) || Threshold < 0) throw new ArgumentOutOfRangeException(nameof(Threshold));
        if (Disabled) (start, dragging) = (null, false);
    }

    /// <summary>Adds the pointer tracking and the dragging state.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void AddAttributes(RenderTreeBuilder builder)
    {
        builder.AddAttribute(10, "dragging", dragging);
        builder.AddAttribute(11, "disabled", Disabled);
        builder.AddAttribute(12, "__internal_onpointerdown#drag", EventCallback.Factory.Create<PointerEventArgs>(this, Down));
        builder.AddAttribute(13, "__internal_onpointermove#drag", EventCallback.Factory.Create<PointerEventArgs>(this, MoveAsync));
        builder.AddAttribute(14, "__internal_onpointerup#drag", EventCallback.Factory.Create<PointerEventArgs>(this, UpAsync));
        builder.AddAttribute(15, "__internal_onlostpointercapture#drag", EventCallback.Factory.Create<PointerEventArgs>(this,
            e => { if (e.OriginalTarget == Element.Element && start is not null) CancelDrag(); }));
    }

    private void Down(PointerEventArgs e)
    {
        if (Disabled || e.Button != PointerButton.Primary) return;
        start = (e.X, e.Y);
        Element.CapturePointer();
    }

    private DragEventArgs Args(PointerEventArgs e, (double X, double Y) origin) => new(origin.X, origin.Y,
        Axis == Orientation.Vertical ? 0 : e.X - origin.X, Axis == Orientation.Horizontal ? 0 : e.Y - origin.Y, e.Modifiers);

    private async Task MoveAsync(PointerEventArgs e)
    {
        if (start is not { } origin) return;
        var args = Args(e, origin);
        if (!dragging)
        {
            if (Math.Abs(args.DeltaX) < Threshold && Math.Abs(args.DeltaY) < Threshold) return;
            dragging = true;
            StateHasChanged();
            await OnDragStart.InvokeAsync(args);
        }
        await OnDrag.InvokeAsync(args);
    }

    private async Task UpAsync(PointerEventArgs e)
    {
        if (start is not { } origin) return;
        start = null;
        Element.ReleasePointerCapture();
        if (!dragging) return;
        dragging = false;
        StateHasChanged();
        await OnDragEnd.InvokeAsync(Args(e, origin));
    }

    private void CancelDrag()
    {
        start = null;
        dragging = false;
        StateHasChanged();
    }
}

/// <summary>Which edge or corner a <see cref="ResizeHandle" /> moves.</summary>
public enum ResizeEdge
{
    /// <summary>The right edge: dragging right grows the width.</summary>
    Right,
    /// <summary>The left edge: dragging left grows the width.</summary>
    Left,
    /// <summary>The bottom edge: dragging down grows the height.</summary>
    Bottom,
    /// <summary>The top edge: dragging up grows the height.</summary>
    Top,
    /// <summary>The bottom-right corner: both dimensions.</summary>
    BottomRight,
}

/// <summary>A width and a height reported by a <see cref="ResizeHandle" />.</summary>
/// <param name="Width">The new width, or the starting one when the edge does not change it.</param>
/// <param name="Height">The new height, or the starting one when the edge does not change it.</param>
public readonly record struct ResizeValue(double Width, double Height);

/// <summary>
/// A handle that resizes something it does not own: dragging it (or, when focused, the arrow keys) reports new sizes
/// computed from <see cref="Width" /> and <see cref="Height" /> at the start, clamped to the limits. The caller applies
/// them. Column borders, resizable dialogs, side panes and the grip of a text area are built on it. The element is
/// <c>ResizeHandle</c> with an <c>edge</c> attribute, and the <c>splitter</c> role for assistive technologies.
/// Losing pointer capture cancels resizing without <see cref="OnResizeEnd" />.
/// </summary>
public sealed class ResizeHandle : Control
{
    private ResizeValue origin;
    private (double X, double Y)? start;

    /// <summary>Gets or sets the edge or corner moved by the handle.</summary>
    [Parameter] public ResizeEdge Edge { get; set; }

    /// <summary>Gets or sets the current width of the resized element.</summary>
    [Parameter] public double Width { get; set; }

    /// <summary>Gets or sets the current height of the resized element.</summary>
    [Parameter] public double Height { get; set; }

    /// <summary>Gets or sets the smallest size allowed.</summary>
    [Parameter] public double MinSize { get; set; }

    /// <summary>Gets or sets the largest size allowed.</summary>
    [Parameter] public double MaxSize { get; set; } = double.PositiveInfinity;

    /// <summary>Gets or sets the change made by one arrow key press.</summary>
    [Parameter] public double KeyboardStep { get; set; } = 10;

    /// <summary>Gets or sets whether resizing is unavailable.</summary>
    [Parameter] public bool Disabled { get; set; }

    /// <summary>Gets or sets the callback invoked with each new size.</summary>
    [Parameter] public EventCallback<ResizeValue> OnResize { get; set; }

    /// <summary>Gets or sets the callback invoked with the final size when the pointer is released.</summary>
    [Parameter] public EventCallback<ResizeValue> OnResizeEnd { get; set; }

    /// <summary>Gets the NSS element name emitted by ResizeHandle.</summary>
    protected override string ElementName => "ResizeHandle";

    private bool Horizontal => Edge is ResizeEdge.Left or ResizeEdge.Right or ResizeEdge.BottomRight;

    private bool VerticalEdge => Edge is ResizeEdge.Top or ResizeEdge.Bottom or ResizeEdge.BottomRight;

    /// <summary>Validates the limits.</summary>
    protected override void OnParametersSet()
    {
        if (!Enum.IsDefined(Edge)) throw new ArgumentOutOfRangeException(nameof(Edge));
        if (!double.IsFinite(MinSize) || MinSize < 0 || MaxSize < MinSize) throw new ArgumentOutOfRangeException(nameof(MaxSize));
    }

    /// <summary>Adds the role, the edge and the pointer and keyboard handling.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void AddAttributes(RenderTreeBuilder builder)
    {
        builder.AddAttribute(10, "role", "splitter");
        builder.AddAttribute(11, "edge", Edge switch
        {
            ResizeEdge.Left => "left", ResizeEdge.Bottom => "bottom", ResizeEdge.Top => "top", ResizeEdge.BottomRight => "bottom-right", _ => "right",
        });
        builder.AddAttribute(12, "vertical", Horizontal && Edge != ResizeEdge.BottomRight);
        builder.AddAttribute(13, "disabled", Disabled);
        builder.AddAttribute(14, "focusable", !Disabled);
        builder.AddAttribute(15, "dragging", start is not null);
        builder.AddAttribute(16, "__internal_onpointerdown#resize", EventCallback.Factory.Create<PointerEventArgs>(this, Down));
        builder.AddAttribute(17, "__internal_onpointermove#resize", EventCallback.Factory.Create<PointerEventArgs>(this, MoveAsync));
        builder.AddAttribute(18, "__internal_onpointerup#resize", EventCallback.Factory.Create<PointerEventArgs>(this, UpAsync));
        builder.AddAttribute(19, "__internal_onkeydown#resize", EventCallback.Factory.Create<KeyboardEventArgs>(this, KeyDownAsync));
        builder.AddAttribute(20, "__internal_onlostpointercapture#resize", EventCallback.Factory.Create<PointerEventArgs>(this,
            e => { if (e.OriginalTarget == Element.Element && start is not null) { start = null; StateHasChanged(); } }));
        if (Edge == ResizeEdge.BottomRight || !double.IsFinite(MaxSize)) return;
        builder.AddAttribute(21, "range-min", MinSize);
        builder.AddAttribute(22, "range-max", MaxSize);
        builder.AddAttribute(23, "range-value", Math.Clamp(Horizontal ? Width : Height, MinSize, MaxSize));
        builder.AddAttribute(24, "range-step", KeyboardStep);
        builder.AddAttribute(25, "__internal_oninput#resize", EventCallback.Factory.Create<ChangeEventArgs>(this, e =>
            e.Value is double size ? OnResize.InvokeAsync(Horizontal ? new ResizeValue(size, Height) : new ResizeValue(Width, size)) : Task.CompletedTask));
    }

    private void Down(PointerEventArgs e)
    {
        if (Disabled || e.Button != PointerButton.Primary) return;
        (start, origin) = ((e.X, e.Y), new ResizeValue(Width, Height));
        Element.CapturePointer();
        StateHasChanged();
    }

    private ResizeValue Compute(double dx, double dy)
    {
        var width = Edge switch { ResizeEdge.Right or ResizeEdge.BottomRight => origin.Width + dx, ResizeEdge.Left => origin.Width - dx, _ => origin.Width };
        var height = Edge switch { ResizeEdge.Bottom or ResizeEdge.BottomRight => origin.Height + dy, ResizeEdge.Top => origin.Height - dy, _ => origin.Height };
        return new ResizeValue(Horizontal ? Math.Clamp(width, MinSize, MaxSize) : width, VerticalEdge ? Math.Clamp(height, MinSize, MaxSize) : height);
    }

    private Task MoveAsync(PointerEventArgs e) => start is { } s ? OnResize.InvokeAsync(Compute(e.X - s.X, e.Y - s.Y)) : Task.CompletedTask;

    private async Task UpAsync(PointerEventArgs e)
    {
        if (start is not { } s) return;
        start = null;
        Element.ReleasePointerCapture();
        StateHasChanged();
        await OnResizeEnd.InvokeAsync(Compute(e.X - s.X, e.Y - s.Y));
    }

    private async Task KeyDownAsync(KeyboardEventArgs e)
    {
        if (Disabled || e.Handled || e.Modifiers != KeyModifiers.None) return;
        origin = new ResizeValue(Width, Height);
        var (dx, dy) = e.Key switch
        {
            "ArrowRight" when Horizontal => (KeyboardStep, 0.0),
            "ArrowLeft" when Horizontal => (-KeyboardStep, 0.0),
            "ArrowDown" when VerticalEdge => (0.0, KeyboardStep),
            "ArrowUp" when VerticalEdge => (0.0, -KeyboardStep),
            _ => (double.NaN, double.NaN),
        };
        if (double.IsNaN(dx)) return;
        e.Handled = true;
        var value = Compute(dx, dy);
        await OnResize.InvokeAsync(value);
        await OnResizeEnd.InvokeAsync(value);
    }
}
