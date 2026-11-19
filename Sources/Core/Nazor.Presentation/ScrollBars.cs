using Nazor.Graphics;
using Nazor.Rendering;
using Nazor.Style;

namespace Nazor.Presentation;

/// <summary>Scrollbar geometry in the element's untransformed layout coordinates.</summary>
/// <param name="Track">The track receiving pointer input.</param>
/// <param name="Thumb">The draggable thumb.</param>
/// <param name="IsVisible">Whether the scrollbar is currently painted.</param>
public readonly record struct ScrollBarGeometry(Rect Track, Rect Thumb, bool IsVisible)
{
    /// <summary>Gets the sampled opacity, including automatic fade and the effective scrollbar-opacity value.</summary>
    public double Opacity { get; init; } = 1;

    /// <summary>Gets the full scrollbar bounds, including its end buttons.</summary>
    public Rect Bounds { get; init; }

    /// <summary>Gets the decrease button bounds, or an empty rectangle when buttons are hidden.</summary>
    public Rect DecreaseButton { get; init; }

    /// <summary>Gets the increase button bounds, or an empty rectangle when buttons are hidden.</summary>
    public Rect IncreaseButton { get; init; }
}

public sealed partial class Scene
{
    private HashSet<(ElementNode Element, bool Horizontal)> revealedScrollBars = [];
    private readonly Dictionary<(ElementNode Element, bool Horizontal), ScrollBarFade> scrollBarFades = [];
    private readonly Dictionary<ElementNode, TimeSpan> wheelScrollUntil = new(ReferenceEqualityComparer.Instance);

    private sealed record ScrollBarFade(double From, double To, TimeSpan Start, TimeSpan Duration, EasingFunction Easing)
    {
        public TimeSpan End => Start + Duration;
        public double ValueAt(TimeSpan now) => now < Start ? From : now >= End || Duration == TimeSpan.Zero ? To
            : From + (To - From) * Animation.Easing.Evaluate(Easing, (now - Start) / Duration);
    }

    /// <summary>Gets the next frame delay for style animations, numeric presentation, smooth scrolling, scrollbar fades, wheel-state expiry and held buttons.</summary>
    public TimeSpan? TimeUntilNextFrame
    {
        get
        {
            var next = Transitions.TimeUntilNextFrame;
            if (smoothScrolls.Count > 0 || HasNumberMotion) next = TimeSpan.Zero;
            var now = Transitions.Clock.Now;
            void Include(TimeSpan deadline)
            {
                var wait = deadline > now ? deadline - now : TimeSpan.Zero;
                if (next is null || wait < next) next = wait;
            }
            foreach (var until in wheelScrollUntil.Values) Include(until);
            if (pressedScrollBarButton is { } button && hoveredScrollBarButton == button && CanStepScrollBarButton(button) &&
                GetStyle(button.Element).Get(StyleProperties.ScrollbarButtonRepeatInterval) > TimeSpan.Zero) Include(scrollBarButtonRepeatDue);
            if (!Transitions.ReducedMotion && !Transitions.Suspended)
                foreach (var fade in scrollBarFades.Values)
                    if (fade.From != fade.To && now < fade.End) Include(fade.Start);
            return next;
        }
    }

    /// <summary>Activates :wheel-scrolling on the receiving container and restarts its NSS idle timeout.</summary>
    /// <param name="element">The container receiving wheel input, even when its scroll limit is reached.</param>
    public void NotifyWheelScroll(ElementNode element)
    {
        if (element.IsRemoved) return;
        element.SetState(ElementStates.WheelScrolling, true);
        wheelScrollUntil[element] = Transitions.Clock.Now + GetStyle(element).Get(StyleProperties.ScrollbarWheelTimeout);
    }

    private void ExpireWheelScrolling()
    {
        var now = Transitions.Clock.Now;
        foreach (var (element, until) in wheelScrollUntil.ToArray())
        {
            if (!element.IsRemoved && now < until && IsRendered(element) && GetScrollState(element) is { CanScroll: true }) continue;
            wheelScrollUntil.Remove(element);
            if (!element.IsRemoved) element.SetState(ElementStates.WheelScrolling, false);
        }
    }

    private void PruneScrollBarFades()
    {
        revealedScrollBars.RemoveWhere(key => key.Element.IsRemoved || GetScrollState(key.Element) is not { CanScroll: true });
        foreach (var key in scrollBarFades.Keys.ToArray())
            if (key.Element.IsRemoved || GetScrollState(key.Element) is not { CanScroll: true } || !IsRendered(key.Element)) scrollBarFades.Remove(key);
    }

    private double ScrollBarOpacity(ElementNode element, bool horizontal, ComputedStyle style)
    {
        var key = (element, horizontal);
        var target = style.Get(StyleProperties.ScrollbarVisibility) == ScrollbarVisibility.Visible ||
            revealedScrollBars.Contains(key) || element.States.HasFlag(ElementStates.WheelScrolling) ? 1d : 0d;
        var now = Transitions.Clock.Now;
        if (!scrollBarFades.TryGetValue(key, out var fade))
            scrollBarFades[key] = fade = new(target, target, now, TimeSpan.Zero, EasingFunction.Linear);
        if (Transitions.ReducedMotion || Transitions.Suspended)
            scrollBarFades[key] = fade = new(target, target, now, TimeSpan.Zero, EasingFunction.Linear);
        else if (fade.To != target)
        {
            var duration = style.Get(target > 0 ? StyleProperties.ScrollbarFadeInDuration : StyleProperties.ScrollbarFadeOutDuration);
            var delay = target > 0 ? TimeSpan.Zero : style.Get(StyleProperties.ScrollbarFadeOutDelay);
            scrollBarFades[key] = fade = new(fade.ValueAt(now), target, now + delay, duration, style.Get(StyleProperties.ScrollbarFadeTimingFunction));
        }
        return Math.Clamp(fade.ValueAt(now) * Effective(element, style, StyleProperties.ScrollbarOpacity), 0, 1);
    }

    /// <summary>Gets the scrollbar geometry, or null for a hidden, non-overflowing or zero-sized axis.</summary>
    /// <param name="element">The scroll container.</param>
    /// <param name="horizontal">Whether to return the horizontal scrollbar.</param>
    /// <returns>Geometry before element and ancestor transforms, including current autohide visibility.</returns>
    public ScrollBarGeometry? GetScrollBar(ElementNode element, bool horizontal)
    {
        if (element.GetFeature<ElementLayout>() is not { Scroll: { } scroll, Style: { } style } layout ||
            !(horizontal ? scroll.CanScrollX : scroll.CanScrollY)) return null;
        if (style.Get(StyleProperties.ScrollbarVisibility) == ScrollbarVisibility.None)
        {
            scrollBarFades.Remove((element, horizontal));
            return null;
        }
        var viewport = layout.Node.RoundedFrame.Deflate(layout.Node.Border);
        var width = (float)Effective(element, style, StyleProperties.ScrollbarWidth).Resolve(0);
        var inset = (float)Effective(element, style, StyleProperties.ScrollbarInset).Resolve(0);
        if (width <= 0 || width + inset > (horizontal ? viewport.Height : viewport.Width)) return null;
        var gutter = layout.Node.Style.ScrollbarGutter;
        var corner = horizontal ? gutter.Right : gutter.Bottom;
        if (corner == 0 && scroll.CanScrollX && scroll.CanScrollY) corner = width + inset;
        var length = Math.Max(0, (horizontal ? viewport.Width : viewport.Height) - corner);
        if (length <= 0) return null;
        var bounds = horizontal ? new Rect(viewport.X, viewport.Bottom - width - inset, length, width)
            : new Rect(viewport.Right - width - inset, viewport.Y, width, length);
        var buttonLength = style.Get(StyleProperties.ScrollbarButtons)
            ? Math.Clamp((float)Effective(element, style, StyleProperties.ScrollbarButtonLength).Resolve(0), 0, length / 3) : 0;
        var decrease = buttonLength == 0 ? Rect.Empty : horizontal ? new Rect(bounds.X, bounds.Y, buttonLength, width)
            : new Rect(bounds.X, bounds.Y, width, buttonLength);
        var increase = buttonLength == 0 ? Rect.Empty : horizontal ? new Rect(bounds.Right - buttonLength, bounds.Y, buttonLength, width)
            : new Rect(bounds.X, bounds.Bottom - buttonLength, width, buttonLength);
        var track = horizontal ? new Rect(bounds.X + buttonLength, bounds.Y, length - 2 * buttonLength, width)
            : new Rect(bounds.X, bounds.Y + buttonLength, width, length - 2 * buttonLength);
        length -= 2 * buttonLength;
        var fraction = horizontal ? scroll.ViewportWidth / scroll.ContentWidth : scroll.ViewportHeight / scroll.ContentHeight;
        var thumbLength = Math.Clamp(Math.Max((float)Effective(element, style, StyleProperties.ScrollbarMinThumbLength).Resolve(0), length * fraction), 0, length);
        var offset = horizontal ? scroll.OffsetX / scroll.MaxOffsetX : scroll.OffsetY / scroll.MaxOffsetY;
        var position = (length - thumbLength) * offset;
        var thumb = horizontal ? new Rect(track.X + position, track.Y, thumbLength, width)
            : new Rect(track.X, track.Y + position, width, thumbLength);
        var opacity = ScrollBarOpacity(element, horizontal, style);
        return new(track, thumb, opacity > 0) { Opacity = opacity, Bounds = bounds, DecreaseButton = decrease, IncreaseButton = increase };
    }

    /// <summary>Updates autohide proximity and keeps a dragged scrollbar visible. Call on the UI thread.</summary>
    /// <param name="point">The pointer position in window coordinates, or null after pointer leave.</param>
    /// <param name="target">The hit-tested element under the pointer.</param>
    /// <param name="dragged">The container whose scrollbar is being dragged, if any.</param>
    /// <returns>True when the revealed scrollbar set changed and a frame should be scheduled.</returns>
    public bool UpdateScrollBarInteraction(Point? point, ElementNode? target, ElementNode? dragged = null)
    {
        HashSet<(ElementNode Element, bool Horizontal)> revealed = [];
        ScrollBarButtonTarget? hoveredButton = null;
        for (var element = target; point is { } pointer && element is not null; element = element.ParentElement)
        {
            if (element.GetFeature<ElementLayout>() is not { Scroll: { CanScroll: true }, Style: { } style } layout) continue;
            if (!GetTransform(element).TryInvert(out var inverse)) continue;
            var local = inverse.Transform(pointer);
            var viewport = layout.Node.RoundedFrame.Deflate(layout.Node.Border);
            if (!viewport.Contains(local)) continue;
            foreach (var horizontal in new[] { false, true })
            {
                if (GetScrollBar(element, horizontal) is not { } bar) continue;
                if (hoveredButton is null)
                {
                    if (bar.DecreaseButton.Contains(local)) hoveredButton = new(element, horizontal, true);
                    else if (bar.IncreaseButton.Contains(local)) hoveredButton = new(element, horizontal, false);
                }
                var distance = horizontal ? viewport.Bottom - local.Y : viewport.Right - local.X;
                var threshold = horizontal ? viewport.Bottom - bar.Track.Y : viewport.Right - bar.Track.X;
                if (distance <= Math.Max((float)style.Get(StyleProperties.ScrollbarRevealDistance).Resolve(0), threshold)) revealed.Add((element, horizontal));
            }
        }
        if (dragged is not null)
        {
            revealed.Add((dragged, false));
            revealed.Add((dragged, true));
        }
        var buttonChanged = hoveredScrollBarButton != hoveredButton;
        hoveredScrollBarButton = hoveredButton;
        if (revealedScrollBars.SetEquals(revealed)) return buttonChanged;
        var changed = revealedScrollBars.Union(revealed).ToArray();
        revealedScrollBars = revealed;
        foreach (var (element, horizontal) in changed) GetScrollBar(element, horizontal);
        return true;
    }

    private void PaintScrollBars(ElementNode element, ComputedStyle style, DisplayListBuilder builder)
    {
        var radius = (float)Effective(element, style, StyleProperties.ScrollbarRadius).Resolve(0);
        var trackRadiusValue = Effective(element, style, StyleProperties.ScrollbarTrackRadius);
        var trackRadius = trackRadiusValue.IsFixed ? (float)trackRadiusValue.Length.Resolve(0) : radius;
        var trackBorderWidth = (float)Effective(element, style, StyleProperties.ScrollbarTrackBorderWidth).Resolve(0);
        var trackBorderColor = ToGraphics(Effective(element, style, StyleProperties.ScrollbarTrackBorderColor));
        var trackColor = ToGraphics(Effective(element, style, StyleProperties.ScrollbarTrackColor));
        var thumbColor = ToGraphics(Effective(element, style, StyleProperties.ScrollbarThumbColor));
        foreach (var horizontal in new[] { false, true })
        {
            if (GetScrollBar(element, horizontal) is not { IsVisible: true } bar) continue;
            if (bar.Opacity < 1) builder.Add(new PushOpacity((float)bar.Opacity, bar.Bounds));
            if (!trackColor.IsTransparent) builder.Add(new FillRectangle(bar.Track, trackRadius, trackColor));
            if (trackBorderWidth > 0 && !trackBorderColor.IsTransparent)
                builder.Add(new DrawBorder(bar.Track, trackRadius, new Thickness(trackBorderWidth, trackBorderWidth, trackBorderWidth, trackBorderWidth), trackBorderColor));
            if (!thumbColor.IsTransparent) builder.Add(new FillRectangle(bar.Thumb, radius, thumbColor));
            PaintScrollBarButton(new(element, horizontal, true), bar.DecreaseButton, style, builder);
            PaintScrollBarButton(new(element, horizontal, false), bar.IncreaseButton, style, builder);
            if (bar.Opacity < 1) builder.Add(Pop.Instance);
        }
    }
}
