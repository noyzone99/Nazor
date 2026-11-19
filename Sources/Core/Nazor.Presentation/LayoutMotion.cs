using Nazor.Graphics;
using Nazor.Rendering;
using Nazor.Style;

namespace Nazor.Presentation;

public sealed partial class Scene : ILayoutTransition
{
    private readonly Dictionary<ElementNode, LayoutMotionState> layoutMotion = [];

    LayoutTransitionOrigin? ILayoutTransition.CaptureOrigin(ElementNode element)
    {
        if (element.IsRemoved || !IsRendered(element) || !layoutMotion.ContainsKey(element)) return null;
        Transitions.Tick();
        RequireAxisAligned(GetTransform(element));
        var bounds = GetBounds(element);
        return bounds.IsEmpty ? null : new LayoutTransitionOrigin(this, bounds.X, bounds.Y, bounds.Width, bounds.Height);
    }

    private void AttachLayoutMotion(ElementNode element)
    {
        if (element.Attributes.GetValueOrDefault("layout-transition") is true)
        {
            element.SetFeature<ILayoutTransition>(this);
            layoutMotion.TryAdd(element, new());
        }
        else if (layoutMotion.Remove(element))
        {
            CancelLayoutMotion(element);
            element.SetFeature<ILayoutTransition>(null);
        }
    }

    private void CaptureLayoutMotion()
    {
        if (layoutMotion.Count == 0) return;
        Transitions.Tick();
        foreach (var (element, state) in layoutMotion)
            state.Visual = !element.IsRemoved && IsRendered(element) ? GetBounds(element) : null;
    }

    private void ApplyLayoutMotion()
    {
        foreach (var (element, state) in layoutMotion)
        {
            if (!IsRendered(element) || element.GetFeature<ElementLayout>() is not { Style: { } style } layout)
            {
                state.Box = null;
                CancelLayoutMotion(element);
                continue;
            }
            var box = layout.Node.RoundedFrame;
            Rect? from = null;
            if (element.Attributes.GetValueOrDefault("layout-transition-origin") is LayoutTransitionOrigin origin && !origin.Consumed)
            {
                if (!ReferenceEquals(origin.CoordinateSpace, this))
                    throw new InvalidOperationException("A connected layout transition requires an origin captured in the same presentation scene.");
                origin.Consumed = true;
                from = new Rect((float)origin.X, (float)origin.Y, (float)origin.Width, (float)origin.Height);
            }
            else if (state.Box is { } previous && previous != box) from = state.Visual;
            state.Box = box;
            if (from is not { IsEmpty: false } visible || box.IsEmpty) continue;
            var definition = style.Get(StyleProperties.Transition).Items.LastOrDefault(item => item.Property is "transform" or "all");
            if (definition is null || definition.Duration == TimeSpan.Zero) continue;
            var ancestors = AncestorTransform(element);
            RequireAxisAligned(ancestors);
            if (!ancestors.TryInvert(out var inverse)) throw new InvalidOperationException("Layout transition ancestors must have an invertible transform.");
            var local = inverse.TransformBounds(visible);
            var transform = new ValueList<TransformFunction>([
                new TranslateFunction(Length.Px(local.X - box.X), Length.Px(local.Y - box.Y)),
                new ScaleFunction(local.Width / box.Width, local.Height / box.Height),
            ]);
            Transitions.Start(element, StyleProperties.Transform, transform, ValueList<TransformFunction>.Empty, definition);
        }
    }

    private Matrix AncestorTransform(ElementNode element)
    {
        var matrix = Matrix.Identity;
        for (var ancestor = IsOverlay(element) ? null : element.ParentElement; ancestor is not null; ancestor = IsOverlay(ancestor) ? null : ancestor.ParentElement)
        {
            if (ancestor.GetFeature<ElementLayout>() is not { } layout) continue;
            if (layout.Scroll is { } scroll) matrix = matrix.Then(Matrix.Translation(-scroll.OffsetX, -scroll.OffsetY));
            if (layout.Style is { } style && TransformOf(ancestor, style, layout.Node.RoundedFrame) is { } transform) matrix = matrix.Then(transform);
        }
        return matrix;
    }

    private static void RequireAxisAligned(Matrix matrix)
    {
        if (Math.Abs(matrix.M12) > 0.00001 || Math.Abs(matrix.M21) > 0.00001 || !matrix.TryInvert(out _))
            throw new InvalidOperationException("Layout transitions support invertible axis-aligned transforms; put rotation or skew inside the wrapper's content.");
    }

    private void CancelLayoutMotion(ElementNode element) => Transitions.Start(element, StyleProperties.Transform,
        ValueList<TransformFunction>.Empty, ValueList<TransformFunction>.Empty,
        new TransitionDefinition("transform", TimeSpan.Zero, EasingFunction.Linear, TimeSpan.Zero));

    private void DetachLayoutMotion(object? sender, RenderBatch batch)
    {
        foreach (var element in batch.RemovedNodes.OfType<ElementNode>())
        {
            element.SetFeature<ILayoutTransition>(null);
            layoutMotion.Remove(element);
        }
    }

    private void DisposeLayoutMotion()
    {
        renderer.BatchCompleted -= DetachLayoutMotion;
        foreach (var element in layoutMotion.Keys)
        {
            CancelLayoutMotion(element);
            element.SetFeature<ILayoutTransition>(null);
        }
        layoutMotion.Clear();
    }

    private sealed class LayoutMotionState
    {
        public Rect? Box;
        public Rect? Visual;
    }
}
