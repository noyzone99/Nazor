using Nazor.Controls;
using Nazor.Input;
using Nazor.Samples.Tasks.Models;

namespace Nazor.Samples.Tasks.Views;

/// <summary>Owns transient demo settings; scrolling itself remains the responsibility of NSS and the input system.</summary>
public partial class ScrollbarsView
{
    // Finite, local fixtures make overflow reproducible without changing the singleton task store.
    // Construct once per view, never during rendering: stable records/keys preserve focus across settings changes.
    private readonly TaskItem[] examples = [.. Enumerable.Range(1, 32).Select(i =>
        new TaskItem(Guid.NewGuid(), $"Task {i:00} — review the project checklist", i % 4 == 0))];
    private Container inherited = null!;
    private Container container = null!;
    private string visibility = "inherit", placement = "overlay", step = "inherit", motion = "default";
    private bool customAppearance, buttons, hideOnWheel, wheelAnimation, fittingContent;
    private bool smooth = true;
    private bool repeatButtons = true;
    private string inheritedOffset = "x 0 · y 0", containerOffset = "x 0 · y 0";

    private bool IsAutohide => visibility is "inherit" or "autohide";
    private IEnumerable<TaskItem> VisibleExamples => fittingContent ? examples.Take(2) : examples;

    // All values come from the fixed choices above; classes keep the teaching rules readable in the NSS sidecar.
    // Autohide forces overlay while retaining the user's reserve choice for when visibility returns to visible.
    private string SurfaceClasses => $"preview visibility-{visibility} placement-{(IsAutohide ? "overlay" : placement)} step-{step} motion-{motion}" +
        (customAppearance ? " custom-appearance" : "") + (buttons ? " end-buttons" : "") +
        (buttons && !repeatButtons ? " no-repeat" : "") +
        (smooth ? " smooth" : " immediate") + (hideOnWheel ? " hide-on-wheel" : "") + (wheelAnimation ? " wheel-animation" : "");

    // These references are assigned after rendering; button handlers read them only when the user invokes an action.
    // ScrollTo honors each surface's smooth settings, and null would preserve an individual axis.
    private void ScrollToTop() => ScrollBoth(0, 0);
    private void ScrollToFraction() => ScrollBoth(25.5, 40.25);
    private void ScrollBoth(double x, double y)
    {
        inherited.Element.ScrollTo(x, y);
        container.Element.ScrollTo(x, y);
    }

    private void InheritedScrolled(ScrollEventArgs e) => inheritedOffset = Offset(e);
    private void ContainerScrolled(ScrollEventArgs e) => containerOffset = Offset(e);
    private static string Offset(ScrollEventArgs e) => FormattableString.Invariant($"x {e.ScrollLeft:0.###} · y {e.ScrollTop:0.###}");

    private void ResetSettings()
    {
        // Reset presentation only; leaving the view also resets these local fields. Task data is unaffected.
        visibility = step = "inherit";
        placement = "overlay";
        motion = "default";
        customAppearance = buttons = hideOnWheel = wheelAnimation = fittingContent = false;
        smooth = true;
        repeatButtons = true;
    }
}
