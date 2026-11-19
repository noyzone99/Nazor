using Nazor.Graphics;
using Nazor.Rendering;

namespace Nazor.Input;

/// <summary>
/// Dispatches input to the retained element tree and manages focus, selection, and pointer capture.
/// </summary>
public sealed partial class InputManager
{
    private ElementNode? draggedRange;
    private bool draggedRangeChanged;

    /// <summary>
    /// Sets the value of a slider as the user would: snapped to its step, kept in its range, with oninput and
    /// onchange. False when the element is disabled or the value does not change.
    /// </summary>
    /// <param name="element">The element to inspect or update.</param>
    /// <param name="value">The value to use.</param>
    /// <returns>true when the operation succeeds or the condition is satisfied; otherwise, false.</returns>
    public bool SetRangeValue(ElementNode element, double value) => SetRange(element, value, commit: true);

    private static bool IsSlider(ElementNode element) => element.Attributes.GetValueOrDefault("role") is "slider";

    private static bool IsSpin(ElementNode element) => element.Attributes.GetValueOrDefault("spin") is true && IsTextBox(element);

    private static bool IsRange(ElementNode element) => IsSlider(element) || IsSpin(element) || element.Attributes.GetValueOrDefault("role") is "splitter";

    private static ElementNode? RangeOf(ElementNode? target) => Ancestors(target).FirstOrDefault(IsSlider);

    private static double RangeAttribute(ElementNode element, string name, double fallback) =>
        element.Attributes.GetValueOrDefault(name) is double value && double.IsFinite(value) ? value : fallback;

    private bool SetRange(ElementNode element, double value, bool commit)
    {
        if (!CanInteract(element) || !IsRange(element) || element.Attributes.GetValueOrDefault("readonly") is true || !double.IsFinite(value)) return false;
        var min = RangeAttribute(element, "range-min", 0);
        var max = Math.Max(min, RangeAttribute(element, "range-max", 100));
        var step = RangeAttribute(element, "range-step", 0);

        if (step > 0 && IsSlider(element)) value = min + Math.Round((value - min) / step) * step;
        value = Math.Clamp(value, min, max);
        if (element.Attributes.GetValueOrDefault("range-value") is double current && value == current) return false;
        Dispatch(element, "oninput", new ChangeEventArgs { Value = value });
        if (commit) Dispatch(element, "onchange", new ChangeEventArgs { Value = value });
        return true;
    }

    private bool RangeKeyDown(ElementNode element, string key)
    {
        if (element.Attributes.GetValueOrDefault("role") is "splitter") return false;
        if (IsSpin(element) && key is not ("ArrowUp" or "ArrowDown" or "PageUp" or "PageDown")) return false;
        var min = RangeAttribute(element, "range-min", 0);
        var max = Math.Max(min, RangeAttribute(element, "range-max", 100));

        var value = RangeAttribute(element, "range-value", IsSpin(element) ? Math.Clamp(0, min, max) : min);
        var small = RangeAttribute(element, "range-step", 0) is > 0 and var step ? step : (max - min) / 100;
        var large = RangeAttribute(element, "range-large-step", 0) is > 0 and var page ? page : (max - min) / 10;
        var reversed = element.Attributes.GetValueOrDefault("range-reversed") is true && !IsSpin(element) ? -1 : 1;
        double? target = key switch
        {
            "ArrowRight" or "ArrowUp" => value + small * reversed,
            "ArrowLeft" or "ArrowDown" => value - small * reversed,
            "PageUp" => value + large,
            "PageDown" => value - large,
            "Home" => min,
            "End" => max,
            _ => null,
        };
        if (target is null) return false;
        SetRange(element, target.Value, commit: true);
        return true;
    }

    private double RangeValueAt(ElementNode element, Point point)
    {
        var track = Descendants(element).FirstOrDefault(e => e.Attributes.GetValueOrDefault("range-track") is true) ?? element;
        var bounds = scene.GetBounds(track);
        var vertical = element.Attributes.GetValueOrDefault("vertical") is true;
        var fraction = vertical
            ? bounds.Height > 0 ? Math.Clamp((bounds.Bottom - point.Y) / bounds.Height, 0, 1) : 0
            : bounds.Width > 0 ? Math.Clamp((point.X - bounds.X) / bounds.Width, 0, 1) : 0;
        if (element.Attributes.GetValueOrDefault("range-reversed") is true) fraction = 1 - fraction;
        var min = RangeAttribute(element, "range-min", 0);
        return min + fraction * (Math.Max(min, RangeAttribute(element, "range-max", 100)) - min);
    }

    private bool RangePointerPressed(ElementNode? target, Point point)
    {
        if (RangeOf(target) is not { } range || IsDisabled(range)) return false;
        if (!CapturePointer(range) || CapturedPointer != range) return false;
        draggedRange = range;
        SetState(range, ElementStates.Pressed, true);
        var changed = SetRange(range, RangeValueAt(range, point), commit: false);
        if (draggedRange == range && CapturedPointer == range) draggedRangeChanged = changed;
        return true;
    }

    private void RangePointerMoved(Point point)
    {
        if (draggedRange is not { } range) return;
        if (CapturedPointer != range) { CancelRangePointer(range); return; }
        var changed = SetRange(range, RangeValueAt(range, point), commit: false);
        if (draggedRange == range && CapturedPointer == range) draggedRangeChanged |= changed;
    }

    private void RangePointerReleased()
    {
        if (draggedRange is not { } range) return;
        draggedRange = null;
        var changed = draggedRangeChanged;
        draggedRangeChanged = false;
        SetState(range, ElementStates.Pressed, false);
        if (changed && !range.IsRemoved) Dispatch(range, "onchange", new ChangeEventArgs { Value = RangeAttribute(range, "range-value", 0) });
    }

    private void CancelRangePointer(ElementNode owner)
    {
        if (draggedRange != owner) return;
        draggedRange = null;
        draggedRangeChanged = false;
        if (owner.IsRemoved) owner.SetState(ElementStates.Pressed, false);
        else SetState(owner, ElementStates.Pressed, false);
    }
}
