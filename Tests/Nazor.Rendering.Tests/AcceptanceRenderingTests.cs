using DeviceRegistry;
using Microsoft.Extensions.DependencyInjection;
using Nazor.Graphics;
using Nazor.Input;
using Nazor.Rendering;
using Nazor.Testing;

namespace Nazor.Rendering.Tests;

public sealed class AcceptanceRenderingTests : IDisposable
{
    private readonly RenderHarness harness = new(new ServiceCollection().AddDeviceRegistry().AddLogging().BuildServiceProvider());
    private readonly InputManager input;
    private readonly RootComponent root;

    public AcceptanceRenderingTests()
    {
        input = new InputManager(harness.Host.Renderer, harness.Scene);
        root = harness.Host.Render<App>();
    }

    public void Dispose()
    {
        input.Dispose();
        harness.Dispose();
    }

    private void Click(string id, float width, float height)
    {
        harness.Render(width, height, 1);
        var element = root.Elements().First(e => e.Id == id);
        var target = element.Name == "Button" ? element : element.Elements().First(e => e.Name == "Button");
        var bounds = harness.Scene.GetBounds(target);
        var point = new Point(bounds.X + 4, bounds.Y + bounds.Height / 2);
        input.PointerPressed(point, PointerButton.Primary, KeyModifiers.None);
        input.PointerReleased(point, PointerButton.Primary, KeyModifiers.None);
        input.PointerLeft();
    }

    [Fact]
    public void Light_theme_with_a_selected_device_matches_the_reference_image()
    {
        Click("device-2", 960, 640);
        var image = harness.Render(960, 640, 1);
        ImageAssert.MatchesBaseline(harness.Graphics, image, "registry-light-960x640@1x");
    }

    [Fact]
    public void Dark_narrow_layout_with_many_devices_matches_the_reference_image()
    {
        Click("theme", 640, 560);
        for (var i = 0; i < 8; i++) Click("add", 640, 560);
        Click("device-3", 640, 560);
        input.PointerWheel(new Point(100, 200), -1, KeyModifiers.None);
        input.PointerLeft();
        var image = harness.Render(640, 560, 1.5f);
        ImageAssert.MatchesBaseline(harness.Graphics, image, "registry-dark-640x560@1.5x");
    }
}
