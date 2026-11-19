using Nazor.Graphics;
using Nazor.Rendering;
using Nazor.Style;

namespace Nazor.Presentation;

public sealed partial class Scene
{
    private readonly record struct ScrollBarButtonTarget(ElementNode Element, bool Horizontal, bool Decrease);
    private ScrollBarButtonTarget? hoveredScrollBarButton;
    private ScrollBarButtonTarget? pressedScrollBarButton;
    private TimeSpan scrollBarButtonRepeatDue;

    /// <summary>Presses an end button, scrolls one step and starts its configured repeat timer.</summary>
    /// <param name="element">The scroll container.</param>
    /// <param name="horizontal">Whether to press a horizontal scrollbar button.</param>
    /// <param name="decrease">Whether to decrease the scroll offset.</param>
    public void PressScrollBarButton(ElementNode element, bool horizontal, bool decrease)
    {
        var target = new ScrollBarButtonTarget(element, horizontal, decrease);
        pressedScrollBarButton = target;
        scrollBarButtonRepeatDue = Transitions.Clock.Now + GetStyle(element).Get(StyleProperties.ScrollbarButtonRepeatDelay);
        StepScrollBarButton(target);
    }

    /// <summary>Releases the pressed scrollbar end button and cancels its repeat timer.</summary>
    public void ReleaseScrollBarButton() => pressedScrollBarButton = null;

    private bool CanStepScrollBarButton(ScrollBarButtonTarget target)
    {
        if (target.Element.IsRemoved || !IsRendered(target.Element) || GetScrollState(target.Element) is not { } scroll) return false;
        var style = GetStyle(target.Element);
        if (style.Get(StyleProperties.ScrollbarVisibility) == ScrollbarVisibility.None || !style.Get(StyleProperties.ScrollbarButtons) ||
            style.Get(StyleProperties.ScrollbarButtonStep).Resolve(0) <= 0) return false;
        var offset = target.Horizontal ? scroll.OffsetX : scroll.OffsetY;
        var max = target.Horizontal ? scroll.MaxOffsetX : scroll.MaxOffsetY;
        return target.Decrease ? offset > 0 : offset < max;
    }

    private void StepScrollBarButton(ScrollBarButtonTarget target)
    {
        if (!CanStepScrollBarButton(target)) return;
        var step = (float)GetStyle(target.Element).Get(StyleProperties.ScrollbarButtonStep).Resolve(0) * (target.Decrease ? -1 : 1);
        ScrollBy(target.Element, target.Horizontal ? step : 0, target.Horizontal ? 0 : step);
    }

    private void RepeatScrollBarButton()
    {
        if (pressedScrollBarButton is not { } target || target != hoveredScrollBarButton || !CanStepScrollBarButton(target)) return;
        var interval = GetStyle(target.Element).Get(StyleProperties.ScrollbarButtonRepeatInterval);
        if (interval <= TimeSpan.Zero || Transitions.Clock.Now < scrollBarButtonRepeatDue) return;
        StepScrollBarButton(target);
        scrollBarButtonRepeatDue = Transitions.Clock.Now + interval;
    }

    private void PaintScrollBarButton(ScrollBarButtonTarget target, Rect bounds, ComputedStyle style, DisplayListBuilder builder)
    {
        if (bounds.IsEmpty) return;
        var enabled = CanStepScrollBarButton(target);
        var background = enabled && target == hoveredScrollBarButton
            ? target == pressedScrollBarButton ? StyleProperties.ScrollbarButtonActiveBackground : StyleProperties.ScrollbarButtonHoverBackground
            : StyleProperties.ScrollbarButtonBackground;
        var color = Effective(target.Element, style, enabled ? StyleProperties.ScrollbarButtonColor : StyleProperties.ScrollbarButtonDisabledColor);
        var fill = ToGraphics(Effective(target.Element, style, background));
        var radius = (float)Effective(target.Element, style, StyleProperties.ScrollbarButtonRadius).Resolve(0);
        if (!fill.IsTransparent) builder.Add(new FillRectangle(bounds, radius, fill));
        PaintChevron(bounds, color, target.Horizontal ? target.Decrease ? "chevron-left" : "chevron-right"
            : target.Decrease ? "chevron-up" : "chevron-down", Length.Zero, 0, 1, builder);
    }
}
