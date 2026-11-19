using Nazor.Controls.Tests.Foundation.Fixtures;
using Nazor.Graphics;
using Nazor.Input;
using Nazor.Style.Syntax;

namespace Nazor.Controls.Tests.Foundation;

[Collection("Controls")]
public sealed class ButtonPressOriginTests : IDisposable
{
    private readonly ControlHarness h = new();
    public void Dispose() => h.Dispose();
    private ButtonPressProbe Render()
    {
        h.Render<ButtonPressProbe>();
        var probe = h.Component<ButtonPressProbe>();
        Assert.True(probe.Scroller.Element.ScrollTo(null, 150));
        h.Frame();
        return probe;
    }

    [Fact]
    public void A_pointer_press_uses_the_untransformed_local_origin_through_rotated_scaled_and_scrolled_ancestors()
    {
        var probe = Render();
        var button = h.ByClass("press");
        var frame = h.Scene.GetLayoutNode(button)!.RoundedFrame;
        var point = h.Scene.GetTransform(button).Transform(new(frame.X + 25, frame.Y + 12));
        var local = probe.Button.Element.GetLocalPoint(point.X, point.Y);
        Assert.NotNull(local);
        Assert.Equal(25, local.Value.X, 3);
        Assert.Equal(12, local.Value.Y, 3);
        h.Input.PointerPressed(point, PointerButton.Primary, KeyModifiers.None);
        h.Frame();
        Assert.Equal(true, button.Attributes["press-effect"]);
        Assert.Equal(25, Assert.Single(h.Style(button).CustomProperties["--nz-press-x"], token => token.Kind == StyleTokenKind.Dimension).Number, 2);
        Assert.Equal(12, Assert.Single(h.Style(button).CustomProperties["--nz-press-y"], token => token.Kind == StyleTokenKind.Dimension).Number, 2);
        h.Input.PointerReleased(point, PointerButton.Primary, KeyModifiers.None);
        h.Frame();
        Assert.Equal(1, probe.Clicks);
    }

    [Fact]
    public void Secondary_and_inactive_presses_are_ignored_and_keyboard_activation_restarts_at_the_center()
    {
        var probe = Render();
        var button = h.ByClass("press");
        h.Click(button, button: PointerButton.Secondary);
        h.Click(h.ByClass("disabled"));
        h.Click(h.ByClass("busy"));
        Assert.Equal(false, button.Attributes["press-effect"]);
        Assert.Equal(false, h.ByClass("disabled").Attributes["press-effect"]);
        Assert.Equal(false, h.ByClass("busy").Attributes["press-effect"]);
        h.Focus(button);
        h.Press(" ");
        var first = button.Attributes["press-replay"];
        Assert.Equal("50%", StyleToken.Serialize(h.Style(button).CustomProperties["--nz-press-x"]));
        Assert.Equal("50%", StyleToken.Serialize(h.Style(button).CustomProperties["--nz-press-y"]));
        h.Press(" ");
        Assert.NotEqual(first, button.Attributes["press-replay"]);
        Assert.Equal(2, probe.Clicks);
    }

    [Fact]
    public void Point_conversion_rejects_nonfinite_hidden_singular_and_detached_elements()
    {
        var probe = Render();
        Assert.Null(probe.Button.Element.GetLocalPoint(double.NaN, 0));
        Assert.Null(probe.Button.Element.GetLocalPoint(0, double.PositiveInfinity));
        Assert.Null(probe.Button.Element.GetLocalPoint(double.MaxValue, 0));
        Assert.Null(default(ElementReference).GetLocalPoint(0, 0));
        probe.Transform = "scale(0)"; probe.Refresh(); h.Frame();
        Assert.Null(probe.Button.Element.GetLocalPoint(0, 0));
        probe.Transform = "none"; probe.Hidden = true; probe.Refresh(); h.Frame();
        Assert.Null(probe.Button.Element.GetLocalPoint(0, 0));
        var reference = probe.Button.Element;
        h.Host.Renderer.RemoveRootComponent(h.Root);
        Assert.Null(reference.GetLocalPoint(0, 0));
    }
}
