using Nazor.Input;
using Nazor.Rendering;

namespace Nazor.Controls;

/// <summary>The directions in which a <see cref="ScrollView" /> scrolls, written as the <c>orientation</c> attribute.</summary>
public enum ScrollOrientation
{
    /// <summary>Up and down; the content wraps to the width of the view.</summary>
    Vertical,
    /// <summary>Left and right, for content wider than the view.</summary>
    Horizontal,
    /// <summary>Both directions.</summary>
    Both,
}

/// <summary>
/// A region that scrolls its content in the directions of <see cref="Orientation" />, with the wheel, the scroll bars,
/// touch and, when the focus or the pointer is inside, Page Up, Page Down, Home and End. After each scroll and each
/// layout the element tells which edges still hide content with the <c>can-scroll-top</c>, <c>can-scroll-bottom</c>,
/// <c>can-scroll-left</c> and <c>can-scroll-right</c> attributes, and <c>can-scroll-start</c> and
/// <c>can-scroll-end</c> along its orientation, so themes can draw shadows or fades at those edges.
/// <see cref="OnScroll" /> reports changes to this view; <see cref="ScrollToStartAsync" />, <see cref="ScrollToEndAsync" />,
/// <see cref="ScrollToAsync" /> and <see cref="ScrollIntoView" /> move it from code. A named view is a group for
/// assistive technologies.
/// </summary>
public sealed class ScrollView : ContentControl
{
    private const double Tolerance = 0.5;
    private ScrollEventArgs? state;

    /// <summary>Gets or sets the directions in which the content scrolls.</summary>
    [Parameter] public ScrollOrientation Orientation { get; set; }

    /// <summary>
    /// Gets or sets whether the view itself is a Tab stop, so that keyboard users can scroll content that has nothing
    /// focusable in it.
    /// </summary>
    [Parameter] public bool IsFocusable { get; set; }

    /// <summary>Gets or sets the callback invoked when this view's offset, viewport or content size changes.</summary>
    [Parameter] public EventCallback<ScrollEventArgs> OnScroll { get; set; }

    /// <summary>Gets the horizontal offset, in logical units.</summary>
    public double ScrollLeft => state?.ScrollLeft ?? 0;

    /// <summary>Gets the vertical offset, in logical units.</summary>
    public double ScrollTop => state?.ScrollTop ?? 0;

    /// <summary>Gets the width of the visible area, or zero before the first layout.</summary>
    public double ViewportWidth => state?.ViewportWidth ?? 0;

    /// <summary>Gets the height of the visible area, or zero before the first layout.</summary>
    public double ViewportHeight => state?.ViewportHeight ?? 0;

    /// <summary>Gets the width of the content, or zero before the first layout.</summary>
    public double ContentWidth => state?.ContentWidth ?? 0;

    /// <summary>Gets the height of the content, or zero before the first layout.</summary>
    public double ContentHeight => state?.ContentHeight ?? 0;

    /// <summary>Gets whether content is hidden above the visible area.</summary>
    public bool CanScrollTop => Vertical && ScrollTop > Tolerance;

    /// <summary>Gets whether content is hidden below the visible area.</summary>
    public bool CanScrollBottom => Vertical && ContentHeight - ViewportHeight - ScrollTop > Tolerance;

    /// <summary>Gets whether content is hidden left of the visible area.</summary>
    public bool CanScrollLeft => Horizontal && ScrollLeft > Tolerance;

    /// <summary>Gets whether content is hidden right of the visible area.</summary>
    public bool CanScrollRight => Horizontal && ContentWidth - ViewportWidth - ScrollLeft > Tolerance;

    /// <summary>Gets whether content is hidden before the visible area along the orientation (above or left).</summary>
    public bool CanScrollStart => CanScrollTop || CanScrollLeft;

    /// <summary>Gets whether content is hidden after the visible area along the orientation (below or right).</summary>
    public bool CanScrollEnd => CanScrollBottom || CanScrollRight;

    private bool Vertical => Orientation != ScrollOrientation.Horizontal;

    private bool Horizontal => Orientation != ScrollOrientation.Vertical;

    /// <summary>Gets the NSS element name emitted by ScrollView.</summary>
    protected override string ElementName => "ScrollView";

    /// <summary>Validates the orientation.</summary>
    protected override void OnParametersSet()
    {
        if (!Enum.IsDefined(Orientation)) throw new ArgumentOutOfRangeException(nameof(Orientation));
    }

    /// <summary>Adds the orientation, the edge attributes, the focus behavior and the scroll listener.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void AddAttributes(RenderTreeBuilder builder)
    {
        builder.AddAttribute(10, "role", AccessibleName is null && LabeledBy is null ? null : "group");
        builder.AddAttribute(11, "orientation", Orientation switch
        {
            ScrollOrientation.Horizontal => "horizontal", ScrollOrientation.Both => "both", _ => "vertical",
        });
        builder.AddAttribute(12, "can-scroll-top", CanScrollTop);
        builder.AddAttribute(13, "can-scroll-bottom", CanScrollBottom);
        builder.AddAttribute(14, "can-scroll-left", CanScrollLeft);
        builder.AddAttribute(15, "can-scroll-right", CanScrollRight);
        builder.AddAttribute(16, "can-scroll-start", CanScrollStart);
        builder.AddAttribute(17, "can-scroll-end", CanScrollEnd);
        builder.AddAttribute(18, "focusable", IsFocusable);
        if (IsFocusable && TabIndex is null) builder.AddAttribute(19, "tabindex", 0);
        builder.AddAttribute(20, "__internal_onscroll#scrollview", EventCallback.Factory.Create<ScrollEventArgs>(this, ScrolledAsync));
    }

    private async Task ScrolledAsync(ScrollEventArgs e)
    {
        if (e.OriginalTarget != Element.Element) return;
        var edges = (CanScrollTop, CanScrollBottom, CanScrollLeft, CanScrollRight);
        state = e;
        if (edges != (CanScrollTop, CanScrollBottom, CanScrollLeft, CanScrollRight)) StateHasChanged();
        await OnScroll.InvokeAsync(e);
    }

    /// <summary>Scrolls to an offset within the limits; null keeps that axis.</summary>
    /// <param name="left">The horizontal offset, or null.</param>
    /// <param name="top">The vertical offset, or null.</param>
    /// <returns>A task whose result is true when the view moved.</returns>
    public Task<bool> ScrollToAsync(double? left, double? top)
    {
        if (left is { } x && !double.IsFinite(x)) throw new ArgumentOutOfRangeException(nameof(left));
        if (top is { } y && !double.IsFinite(y)) throw new ArgumentOutOfRangeException(nameof(top));
        return Run(() => Element.ScrollTo(left, top));
    }

    /// <summary>Scrolls to the beginning along the orientation: the top, the left edge, or both.</summary>
    /// <returns>A task whose result is true when the view moved.</returns>
    public Task<bool> ScrollToStartAsync() => Run(() => Element.ScrollTo(Horizontal ? 0 : null, Vertical ? 0 : null));

    /// <summary>Scrolls to the end along the orientation: the bottom, the right edge, or both.</summary>
    /// <returns>A task whose result is true when the view moved.</returns>
    public Task<bool> ScrollToEndAsync() =>
        Run(() => Element.ScrollTo(Horizontal ? EndOffset(ContentWidth, ViewportWidth) : null, Vertical ? EndOffset(ContentHeight, ViewportHeight) : null));

    /// <summary>Scrolls this view, and any view around it, so that an element inside it becomes visible.</summary>
    /// <param name="target">The element to show, such as the <see cref="Control.Element" /> of a control.</param>
    /// <returns>true when a view moved.</returns>
    public bool ScrollIntoView(ElementReference target) => target.ScrollIntoView();

    private static double EndOffset(double content, double viewport) => content > 0 ? Math.Max(0, content - viewport) : 1_000_000_000;

    private async Task<bool> Run(Func<bool> scroll)
    {
        var moved = false;
        await InvokeAsync(() => moved = scroll());
        return moved;
    }
}
