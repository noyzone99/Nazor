using Nazor.Rendering;
using Nazor.Style;

namespace Nazor.Presentation;

public sealed partial class Scene : IOverlayMotion
{
    private readonly HashSet<ElementNode> overlayMotionOwners = [];
    private event Action? overlayMotionPreferencesChanged;
    private bool lastOverlayReducedMotion;

    bool IOverlayMotion.ReducedMotion => Transitions.ReducedMotion || Styles.Preferences.ReducedMotion;

    event Action? IOverlayMotion.PreferencesChanged
    {
        add => overlayMotionPreferencesChanged += value;
        remove => overlayMotionPreferencesChanged -= value;
    }

    TimeSpan IOverlayMotion.GetExitDuration(ElementNode surface) =>
        ((IOverlayMotion)this).ReducedMotion || !IsRendered(surface) ||
        surface.Attributes.GetValueOrDefault("anchor") is ElementNode anchor && !IsRendered(anchor)
            ? TimeSpan.Zero : GetStyle(surface).Get(StyleProperties.OverlayExitDuration);

    TimeSpan IOverlayMotion.GetSubmenuOpenDelay(ElementNode trigger) =>
        ((IOverlayMotion)this).ReducedMotion ? TimeSpan.Zero : GetStyle(trigger).Get(StyleProperties.SubmenuOpenDelay);

    private void AttachOverlayMotion(ElementNode surface)
    {
        surface.SetFeature<IOverlayMotion>(this);
        overlayMotionOwners.Add(surface);
    }

    private void DetachOverlayMotion(object? sender, RenderBatch batch)
    {
        foreach (var element in batch.RemovedNodes.OfType<ElementNode>())
        {
            element.SetFeature<IOverlayMotion>(null);
            overlayMotionOwners.Remove(element);
        }
    }

    private void NotifyOverlayMotionPreferences()
    {
        var reduced = ((IOverlayMotion)this).ReducedMotion;
        if (lastOverlayReducedMotion == reduced) return;
        lastOverlayReducedMotion = reduced;
        overlayMotionPreferencesChanged?.Invoke();
    }
}
