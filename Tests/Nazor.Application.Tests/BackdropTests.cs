using Nazor.Graphics;
using Nazor.Testing;
using static Nazor.Platform.Windows.Win32;

namespace Nazor.Application.Tests;

[Collection("Windows")]
[System.Runtime.Versioning.SupportedOSPlatform("windows")]
public sealed class BackdropTests
{
    private static unsafe int Attribute(nint handle, uint attribute)
    {
        var value = -1;
        Assert.True(DwmGetWindowAttribute(handle, attribute, &value, sizeof(int)) >= 0);
        return value;
    }

    [Fact]
    public void A_window_on_mica_is_transparent_over_the_material_and_its_frame_follows_the_colors()
    {
        if (!OperatingSystem.IsWindowsVersionAtLeast(10, 0, 22621)) Assert.Skip("The materials of Windows 11 need version 22H2.");
        foreach (var scheme in new[] { Nazor.Style.ColorScheme.Light, Nazor.Style.ColorScheme.Dark })
        {
            using var harness = new ApplicationHarness(() => ApplicationTestStartup.Run<MicaWindow>(new ApplicationRuntimeSettings { Activate = false, ColorScheme = scheme }));
            var window = harness.Window<MicaWindow>(w => !harness.HasExited);
            var (handle, backdrop, background, element) = ApplicationHarness.On(window, () => (window.PlatformWindow.Handle, window.AppliedBackdrop, window.Scene.Background,
                window.Root.Elements().Single(e => e.Name == "Window")));
            Assert.True(backdrop.IsActive);
            Assert.Equal("mica", backdrop.Material);
            Assert.Equal(2, ApplicationHarness.On(window, () => Attribute(handle, DWMWA_SYSTEMBACKDROP_TYPE)));
            Assert.Equal(0, background.A);
            Assert.Equal("mica", element.Attributes["backdrop"]);
            Assert.Equal(scheme == Nazor.Style.ColorScheme.Dark ? 1 : 0, ApplicationHarness.On(window, () => Attribute(handle, DWMWA_USE_IMMERSIVE_DARK_MODE)));
            ApplicationHarness.On(window, () =>
            {
                window.Close();
                return true;
            });
            Assert.True(harness.WaitForExit());
        }
    }

    [Fact]
    public void Enabling_an_acrylic_backdrop_uses_raster_and_disabling_it_restores_gpu()
    {
        Assert.SkipUnless(OperatingSystem.IsWindowsVersionAtLeast(10, 0, 22621), "Acrylic needs Windows 11 version 22H2.");
        using var harness = new WindowHarness(typeof(NativeAppearancePage), new() { Rendering = RenderingMode.Auto });
        Assert.SkipUnless(harness.Read(window => window.Rendering.Actual) == RenderingMode.Gpu, "Hardware GPU unavailable.");
        harness.WaitUntil(window => window.GpuFramesPresented > 0, "first GPU frame");
        harness.Read(window =>
        {
            window.Backdrop = CommonBackdrop.Blur;
            Assert.True(window.AppliedBackdrop.IsActive);
            Assert.Equal(RenderingMode.Cpu, window.Rendering.Actual);
            Assert.Equal(0, window.Scene.Background.A);
            window.Backdrop = CommonBackdrop.None;
            Assert.Equal(RenderingMode.Gpu, window.Rendering.Actual);
            Assert.Equal(255, window.Scene.Background.A);
            Assert.Empty(window.Exceptions);
            return true;
        });
    }
}
