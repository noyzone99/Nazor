using Nazor.Input;
using Nazor.Rendering;

namespace Nazor.Controls.Primitives;

/// <summary>
/// Reports the laid-out size of its content: <see cref="OnResize" /> receives the width, the height and the position
/// within the parent element after the first layout and after every change, as a <c>ResizeObserver</c>. Use it for
/// layouts that change shape with their own space, such as a navigation pane that collapses when narrow.
/// </summary>
public sealed class SizeObserver : ContentControl
{
    /// <summary>Gets or sets the element name, <c>SizeObserver</c> by default.</summary>
    [Parameter] public string As { get; set; } = "SizeObserver";

    /// <summary>Gets or sets the callback invoked with the new box.</summary>
    [Parameter] public EventCallback<ResizeEventArgs> OnResize { get; set; }

    /// <summary>Gets the last reported width, or zero before the first layout.</summary>
    public double Width { get; private set; }

    /// <summary>Gets the last reported height, or zero before the first layout.</summary>
    public double Height { get; private set; }

    /// <summary>Gets the element name.</summary>
    protected override string ElementName => As;

    /// <summary>Adds the resize listener.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void AddAttributes(RenderTreeBuilder builder) =>
        builder.AddAttribute(10, "__internal_onresize", EventCallback.Factory.Create<ResizeEventArgs>(this, Resized));

    private Task Resized(ResizeEventArgs e)
    {
        (Width, Height) = (e.Width, e.Height);
        return OnResize.InvokeAsync(e);
    }
}

/// <summary>The box of an item measured by an <see cref="IndicatorTracker{TKey}" />, relative to the group that holds it.</summary>
/// <param name="X">The distance from the group's left edge.</param>
/// <param name="Y">The distance from the group's top edge.</param>
/// <param name="Width">The width.</param>
/// <param name="Height">The height.</param>
public readonly record struct ItemBox(double X, double Y, double Width, double Height);

/// <summary>
/// Remembers where the items of a group are, so a selection or hover indicator can follow them. An item reports its box
/// with the <see cref="Attributes" /> it adds to its element (an <c>onresize</c> listener); the group renders an
/// <see cref="Indicator" /> with <see cref="BoxOf" /> of the selected or hovered key. Tabs, segmented controls,
/// navigation panes and menus share it.
/// </summary>
/// <typeparam name="TKey">The type of the item keys.</typeparam>
/// <param name="changed">Invoked when a box changes, usually the group's <c>StateHasChanged</c>.</param>
public sealed class IndicatorTracker<TKey>(Action changed) where TKey : notnull
{
    private readonly Dictionary<TKey, ItemBox> boxes = [];

    /// <summary>Gets the box of an item, or null before it was laid out.</summary>
    /// <param name="key">The item key.</param>
    /// <returns>The box, or null.</returns>
    public ItemBox? BoxOf(TKey? key) => key is not null && boxes.TryGetValue(key, out var box) ? box : null;

    /// <summary>Records the box of an item laid out directly inside the group.</summary>
    /// <param name="key">The item key.</param>
    /// <param name="e">The resize event of the item.</param>
    public void Report(TKey key, ResizeEventArgs e)
    {
        ArgumentNullException.ThrowIfNull(e);
        var box = new ItemBox(e.OffsetLeft, e.OffsetTop, e.Width, e.Height);
        if (boxes.TryGetValue(key, out var old) && old == box) return;
        boxes[key] = box;
        changed();
    }

    /// <summary>Forgets an item that was removed.</summary>
    /// <param name="key">The item key.</param>
    public void Forget(TKey key) => boxes.Remove(key);

    /// <summary>Creates the listener an item adds to its element to report its box.</summary>
    /// <param name="receiver">The component that owns the listener.</param>
    /// <param name="key">The item key.</param>
    /// <returns>The <c>__internal_onresize#indicator</c> attribute and its callback.</returns>
    public KeyValuePair<string, object?> Attributes(object receiver, TKey key) =>
        new("__internal_onresize#indicator", EventCallback.Factory.Create<ResizeEventArgs>(receiver, e => Report(key, e)));
}

/// <summary>
/// A decorative mark that follows an item of its group, such as the line under the selected tab or the highlight that
/// glides between menu items. It renders an element (<see cref="As" />) with the item's box as the
/// <c>--nz-indicator-x</c>, <c>--nz-indicator-y</c>, <c>--nz-indicator-width</c> and <c>--nz-indicator-height</c>
/// variables and the <c>hidden</c> attribute when there is no target; themes position it and may animate the change.
/// </summary>
public sealed class Indicator : Control
{
    /// <summary>Gets or sets the element name, <c>Indicator</c> by default.</summary>
    [Parameter] public string As { get; set; } = "Indicator";

    /// <summary>Gets or sets the box to follow, or null to hide the indicator.</summary>
    [Parameter] public ItemBox? Target { get; set; }

    /// <summary>Gets the element name.</summary>
    protected override string ElementName => As;

    /// <summary>Gets the position variables and the caller's declarations.</summary>
    protected override string? ElementStyle => Target is { } box
        ? Css.Join($"--nz-indicator-x: {Css.Px(box.X)}; --nz-indicator-y: {Css.Px(box.Y)}; --nz-indicator-width: {Css.Px(box.Width)}; --nz-indicator-height: {Css.Px(box.Height)}", Style)
        : Style;

    /// <summary>Hides the indicator from assistive technologies and marks a missing target.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void AddAttributes(RenderTreeBuilder builder)
    {
        builder.AddAttribute(10, "semantic-hidden", true);
        builder.AddAttribute(11, "hidden", Target is null);
    }
}

/// <summary>
/// Content read by assistive technologies but not shown, such as the label of an icon-only control drawn by a theme or
/// extra context for a screen reader. Themes hide the element (<c>VisuallyHidden</c>) without removing it.
/// </summary>
public sealed class VisuallyHidden : ContentControl
{
    /// <summary>Gets the NSS element name emitted by VisuallyHidden.</summary>
    protected override string ElementName => "VisuallyHidden";

    /// <summary>Adds the text role.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void AddAttributes(RenderTreeBuilder builder) => builder.AddAttribute(10, "role", "text");
}

/// <summary>How urgently a <see cref="LiveRegion" /> is announced.</summary>
public enum LivePoliteness
{
    /// <summary>Announced when the user is idle.</summary>
    Polite,
    /// <summary>Announced at once, interrupting; for errors and time-critical messages.</summary>
    Assertive,
}

/// <summary>
/// A region whose changes assistive technologies announce, such as a status line, a form error or a toast stack.
/// Rendering new content inside it is enough to announce it.
/// </summary>
public sealed class LiveRegion : ContentControl
{
    /// <summary>Gets or sets the element name, <c>LiveRegion</c> by default.</summary>
    [Parameter] public string As { get; set; } = "LiveRegion";

    /// <summary>Gets or sets how urgently changes are announced.</summary>
    [Parameter] public LivePoliteness Politeness { get; set; }

    /// <summary>Gets the element name.</summary>
    protected override string ElementName => As;

    /// <summary>Adds the status role and the live politeness.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void AddAttributes(RenderTreeBuilder builder)
    {
        builder.AddAttribute(10, "role", "status");
        builder.AddAttribute(11, "live", Politeness == LivePoliteness.Assertive ? "assertive" : "polite");
    }
}
