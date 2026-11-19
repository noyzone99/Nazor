using Nazor.Graphics;
using Nazor.Rendering;

namespace Nazor.Input;

public sealed partial class InputManager
{
    private ElementNode? draggedScrollBar;
    private ElementNode? pressedScrollBarButton;
    private bool draggedScrollBarHorizontal;
    private float scrollBarGrabOffset;
    private Point? scrollBarPointer;

    private void UpdateScrollBarHover(Point? point, ElementNode? target)
    {
        scrollBarPointer = point;
        if (scene.UpdateScrollBarInteraction(point, target, draggedScrollBar ?? pressedScrollBarButton)) VisualStateChanged?.Invoke();
    }

    private bool ScrollBarPointerPressed(ElementNode? target, Point point)
    {
        if (target is null || IsDisabled(target) || !scene.GetTransform(target).TryInvert(out var inverse)) return false;
        var local = inverse.Transform(point);
        foreach (var horizontal in new[] { false, true })
        {
            if (scene.GetScrollBar(target, horizontal) is not { } bar || !bar.Bounds.Contains(local)) continue;
            if (bar.DecreaseButton.Contains(local) || bar.IncreaseButton.Contains(local))
            {
                if (!CapturePointer(target)) return false;
                pressedScrollBarButton = target;
                draggedScrollBarHorizontal = horizontal;
                scene.PressScrollBarButton(target, horizontal, bar.DecreaseButton.Contains(local));
                UpdateScrollBarHover(point, target);
                DispatchScrollChanges();
                VisualStateChanged?.Invoke();
                return true;
            }
            if (bar.Thumb.Contains(local))
            {
                if (!CapturePointer(target)) return false;
                scene.StopScrolling(target);
                bar = scene.GetScrollBar(target, horizontal)!.Value;
                draggedScrollBar = target;
                draggedScrollBarHorizontal = horizontal;
                scrollBarGrabOffset = horizontal ? local.X - bar.Thumb.X : local.Y - bar.Thumb.Y;
                UpdateScrollBarHover(point, target);
            }
            else if (scene.GetScrollState(target) is { } scroll)
            {
                var direction = (horizontal ? local.X < bar.Thumb.X : local.Y < bar.Thumb.Y) ? -1 : 1;
                ScrollBy(target, horizontal ? direction * scroll.ViewportWidth : 0, horizontal ? 0 : direction * scroll.ViewportHeight);
            }
            return true;
        }
        return false;
    }

    private bool ScrollBarPointerMoved(Point point)
    {
        if (draggedScrollBar is not { } container) return pressedScrollBarButton is not null;
        if (scene.GetScrollBar(container, draggedScrollBarHorizontal) is not { } bar ||
            scene.GetScrollState(container) is not { } scroll || !scene.GetTransform(container).TryInvert(out var inverse))
        {
            ReleasePointerCapture(container);
            return true;
        }
        var horizontal = draggedScrollBarHorizontal;
        var local = inverse.Transform(point);
        var travel = horizontal ? bar.Track.Width - bar.Thumb.Width : bar.Track.Height - bar.Thumb.Height;
        if (travel <= 0) return true;
        var position = (horizontal ? local.X - bar.Track.X : local.Y - bar.Track.Y) - scrollBarGrabOffset;
        var offset = Math.Clamp(position / travel, 0, 1) * (horizontal ? scroll.MaxOffsetX : scroll.MaxOffsetY);
        ScrollTo(container, horizontal ? offset : null, horizontal ? null : offset, immediate: true);
        return true;
    }
}
