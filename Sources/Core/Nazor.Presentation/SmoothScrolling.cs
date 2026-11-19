using Nazor.Animation;
using Nazor.Input;
using Nazor.Rendering;
using Nazor.Style;

namespace Nazor.Presentation;

public sealed partial class Scene
{
    private sealed record ScrollMotion(float FromX, float FromY, float ToX, float ToY, TimeSpan Start, TimeSpan Duration, EasingFunction Easing);
    private readonly Dictionary<ElementNode, ScrollMotion> smoothScrolls = new(ReferenceEqualityComparer.Instance);

    /// <summary>Stops a pending animation at its current fractional offset, without snapping to lines.</summary>
    /// <param name="container">The scroll container.</param>
    public void StopScrolling(ElementNode container)
    {
        SampleSmoothScroll(container);
        smoothScrolls.Remove(container);
    }

    /// <summary>Applies a wheel delta using the container's NSS step. Precise pixel input remains direct.</summary>
    /// <param name="container">The scroll container.</param>
    /// <param name="deltaX">Signed horizontal distance or detents, positive towards the end.</param>
    /// <param name="deltaY">Signed vertical distance or detents, positive towards the end.</param>
    /// <param name="unit">Whether the deltas are pixels or wheel detents.</param>
    /// <param name="systemDistance">Backend distance per detent when scroll-wheel-step is auto.</param>
    /// <param name="systemByPage">Whether the system scrolls by pages instead of a fixed distance.</param>
    /// <returns>Whether the request was consumed, including a configured zero step.</returns>
    public bool ScrollWheel(ElementNode container, float deltaX, float deltaY, ScrollDeltaUnit unit, float systemDistance, bool systemByPage)
    {
        if (GetScrollState(container) is not { } scroll) return false;
        if (unit == ScrollDeltaUnit.LogicalPixels) return ScrollBy(container, deltaX, deltaY, immediate: true);
        var style = GetStyle(container);
        var step = style.Get(StyleProperties.ScrollWheelStep);
        if (step.Kind == ScrollWheelStepKind.Auto)
            return ScrollBy(container, deltaX * (systemByPage ? scroll.ViewportWidth : systemDistance),
                deltaY * (systemByPage ? scroll.ViewportHeight : systemDistance));
        var distance = step.Kind == ScrollWheelStepKind.Lines ? step.Lines * ScrollLineHeight(style) : step.Length.Resolve(0);
        if (distance == 0) return true;
        return ScrollBy(container, deltaX * (float)distance, deltaY * (float)distance);
    }

    private double ScrollLineHeight(ComputedStyle style)
    {
        var line = style.Get(StyleProperties.LineHeight);
        if (line.Kind == Style.LineHeightKind.Length) return line.Length.Resolve(style.FontSize);
        if (line.Kind == Style.LineHeightKind.Number) return line.Number * style.FontSize;
        var text = TextContent.ToTextStyle(style);
        var face = fonts.Match(text);
        return face.Ascent(text.FontSize) + face.Descent(text.FontSize) + face.LineGap(text.FontSize);
    }

    private bool RequestScroll(ElementNode container, float? x, float? y, bool relative, bool immediate)
    {
        if (container.IsRemoved || (x is null && y is null) || x is { } dx && !float.IsFinite(dx) || y is { } dy && !float.IsFinite(dy)) return false;
        if (relative && x.GetValueOrDefault() == 0 && y.GetValueOrDefault() == 0) return false;
        SampleSmoothScroll(container);
        if (GetScrollState(container) is not { } scroll) return false;
        smoothScrolls.TryGetValue(container, out var motion);
        var currentX = scroll.OffsetX;
        var currentY = scroll.OffsetY;
        float Target(float? value, float current, float? pending, float maximum)
        {
            if (immediate) pending = null;
            if (value is null) return pending ?? current;
            if (relative && value == 0) return pending ?? current;
            // A reversal starts from the visible position, so the next frame changes direction immediately.
            var origin = relative && pending is { } end && Math.Sign(end - current) == Math.Sign(value.Value) ? end : current;
            return Math.Clamp(relative ? origin + value.Value : value.Value, 0, maximum);
        }
        var targetX = Target(x, currentX, motion?.ToX, scroll.MaxOffsetX);
        var targetY = Target(y, currentY, motion?.ToY, scroll.MaxOffsetY);
        if (!immediate && motion is not null && targetX == motion.ToX && targetY == motion.ToY) return false;
        smoothScrolls.Remove(container);
        if (targetX == currentX && targetY == currentY) return motion is not null;
        var style = GetStyle(container);
        var duration = style.Get(StyleProperties.ScrollDuration);
        if (!immediate && style.Get(StyleProperties.ScrollBehavior) == ScrollBehavior.Smooth && duration > TimeSpan.Zero &&
            !Transitions.ReducedMotion && !Transitions.Suspended)
        {
            smoothScrolls[container] = new(currentX, currentY, targetX, targetY, Transitions.Clock.Now, duration, style.Get(StyleProperties.ScrollTimingFunction));
            return true;
        }
        var moved = ScrollToX(scroll, targetX) | ScrollTo(scroll, targetY);
        if (moved) scrollChanged.Add(container);
        return moved;
    }

    private void TickSmoothScrolls()
    {
        foreach (var container in smoothScrolls.Keys.ToArray()) SampleSmoothScroll(container);
    }

    private void SampleSmoothScroll(ElementNode container)
    {
        if (!smoothScrolls.TryGetValue(container, out var motion)) return;
        if (container.IsRemoved || !IsRendered(container) || GetScrollState(container) is not { CanScroll: true } scroll)
        {
            smoothScrolls.Remove(container);
            return;
        }
        var style = GetStyle(container);
        var progress = Transitions.ReducedMotion || Transitions.Suspended || style.Get(StyleProperties.ScrollBehavior) != ScrollBehavior.Smooth ||
            style.Get(StyleProperties.ScrollDuration) == TimeSpan.Zero ? 1 : Math.Clamp((Transitions.Clock.Now - motion.Start) / motion.Duration, 0, 1);
        var eased = Easing.Evaluate(motion.Easing, progress);
        var targetX = Math.Clamp(motion.ToX, 0, scroll.MaxOffsetX);
        var targetY = Math.Clamp(motion.ToY, 0, scroll.MaxOffsetY);
        var moved = ScrollToX(scroll, (float)(motion.FromX + (targetX - motion.FromX) * eased)) |
            ScrollTo(scroll, (float)(motion.FromY + (targetY - motion.FromY) * eased));
        if (moved) scrollChanged.Add(container);
        if (progress >= 1) smoothScrolls.Remove(container);
    }
}
