using System.Diagnostics;
using Nazor.Animation;
using Nazor.Graphics;
using Nazor.Rendering;
using Nazor.Style;

namespace Nazor.Presentation;

public sealed partial class Scene
{
    private bool composing;
    private long compositionCapturedAt;
    private bool compositionExported;

    internal void ResetComposition() => compositionExported=false;

    /// <summary>Whether the current animations can run without further UI layout, paint or scrolling work.</summary>
    public bool CanAnimateOnCompositor => Transitions.Clock is SystemAnimationClock && Transitions.IsCompositionOnly &&
        !HasNumberMotion && smoothScrolls.Count == 0 && wheelScrollUntil.Count == 0 && scrollBarFades.Count == 0 && pressedScrollBarButton is null &&
        !overlays.Any(overlay => GetNativePopupBounds(overlay) is not null) && !HasAnimatedBackdrop();

    private bool HasAnimatedBackdrop()
    {
        foreach (var node in renderer.RootComponents.SelectMany(root => root.Nodes))
            if (NeedsPaint(node)) return true;
        return false;

        bool NeedsPaint(Node node)
        {
            if (node is not ElementNode element || element.GetFeature<ElementLayout>()?.Style is not { } style || style.Display == Display.None) return false;
            if ((!style.Get(StyleProperties.BackdropFilter).IsNone || !style.Get(StyleProperties.Filter).IsNone) && Transitions.IsAnimating(element, StyleEffects.Composite)) return true;
            return VisualChildren(element).Any(NeedsPaint);
        }
    }

    /// <summary>Builds a display list containing detached animation snapshots when all moving work is compositable.</summary>
    /// <returns>A list retaining ordinary commands for manual clocks, layout animations and other UI-driven motion.</returns>
    public DisplayList BuildCompositionList()
    {
        composing = CanAnimateOnCompositor;
        compositionCapturedAt = Stopwatch.GetTimestamp();
        try
        {
            var list = BuildDisplayList();
            compositionExported = list.Commands.Any(command => command is PushComposition);
            return list;
        }
        finally { composing = false; }
    }

    private void SampleCompositionTime()
    {
        if (compositionExported && !building) Transitions.Tick();
    }

    private CompositionTimeline? CaptureComposition(ElementNode element, ComputedStyle style, Rect bounds)
    {
        if (!composing || !Transitions.IsAnimating(element, StyleEffects.Composite)) return null;
        var opacity = CaptureValue(element, StyleProperties.Opacity, style.Opacity);
        var transform = CaptureValue(element, StyleProperties.Transform, style.Get(StyleProperties.Transform));
        var origin = CaptureValue(element, StyleProperties.TransformOrigin, style.Get(StyleProperties.TransformOrigin));
        var now = Transitions.FrameTime;
        var duration = TimeSpan.Zero;
        foreach (var property in new StyleProperty[] { StyleProperties.Opacity, StyleProperties.Transform, StyleProperties.TransformOrigin })
            if (Transitions.Find(element, property) is { } transition && transition.End > now)
                duration = duration > transition.End-now ? duration : transition.End-now;
        foreach (var animation in Transitions.GetAnimations(element))
        {
            if (!animation.IsMoving(now)) continue;
            var milliseconds = animation.Definition.Duration.TotalMilliseconds * animation.Definition.IterationCount - animation.Elapsed(now).TotalMilliseconds;
            var remaining = double.IsPositiveInfinity(milliseconds) || milliseconds >= TimeSpan.MaxValue.TotalMilliseconds
                ? TimeSpan.MaxValue : TimeSpan.FromMilliseconds(Math.Max(0,milliseconds));
            if (remaining > duration) duration = remaining;
        }
        return new VisualTimeline(compositionCapturedAt, duration, now, bounds, element.GetFeature<ElementLayout>()?.OverlayShift,
            opacity, transform, origin);
    }

    private Func<TimeSpan,T> CaptureValue<T>(ElementNode element, StyleProperty<T> property, T computed) where T : notnull
    {
        var transition = Transitions.Find(element, property);
        var animations = Transitions.GetAnimations(element).Where(animation => animation.Animates(property)).Select(animation => animation.Snapshot()).ToArray();
        return now =>
        {
            var value = transition is null ? computed : (T)transition.ValueAt(now);
            for (var index = animations.Length-1; index >= 0; index--)
                if (animations[index].Progress(now, false) is { } progress && animations[index].ValueAt(property, progress, value) is T animated)
                    return animated;
            return value;
        };
    }

    private sealed class VisualTimeline(long capturedAt, TimeSpan duration, TimeSpan frameTime, Rect bounds, Point? overlay,
        Func<TimeSpan,double> opacity, Func<TimeSpan,ValueList<TransformFunction>> transform,
        Func<TimeSpan,TransformOrigin> origin) : CompositionTimeline
    {
        public override long CapturedAt => capturedAt;
        public override TimeSpan Duration => duration;
        public override (Matrix Transform, float Opacity) Sample(TimeSpan elapsed)
        {
            var now = frameTime + elapsed;
            var functions = transform(now);
            var matrix = Matrix.Identity;
            if (functions.Items.Count > 0)
            {
                var pivot = origin(now);
                var x = bounds.X + (float)pivot.X.Resolve(bounds.Width);
                var y = bounds.Y + (float)pivot.Y.Resolve(bounds.Height);
                matrix = Matrix.Translation(-x,-y).Then(Compose(functions,bounds)).Then(Matrix.Translation(x,y));
            }
            if (overlay is { } shift) matrix = matrix.Then(Matrix.Translation(shift.X,shift.Y));
            return (matrix, (float)Math.Clamp(opacity(now),0,1));
        }
    }
}
