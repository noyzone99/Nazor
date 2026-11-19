using DeviceRegistry;
using Microsoft.Extensions.DependencyInjection;
using Nazor.Graphics;
using Nazor.Platform.Windows.Automation;
using Nazor.Testing;

namespace Nazor.Application.Tests;

[Collection("Windows")]
[System.Runtime.Versioning.SupportedOSPlatform("windows")]
public sealed class AcceptanceWindowTests
{
    private static WindowHarness Open()
    {
        var harness = new WindowHarness(typeof(App), new ApplicationRuntimeSettings
        {
            ReducedMotion = false,
            Services = new ServiceCollection().AddDeviceRegistry().AddLogging().BuildServiceProvider(),
        });
        harness.WaitUntil(w => w.FrameCount > 0, "first frame");
        return harness;
    }

    [Fact]
    public void Opens_with_the_size_and_title_of_its_window_element_and_rests_without_frames()
    {
        using var harness = Open();
        var (size, title) = harness.Read(w => (w.PlatformWindow.ClientSize, w.PlatformWindow.Title));
        Assert.Equal("Nazor — Registro dispositivi", title);
        Assert.Equal(960, size.Width, 1);
        Assert.Equal(640, size.Height, 1);
        harness.Read(w => { w.Input.PointerLeft(); return true; });
        harness.WaitUntil(w => !w.Scene.Transitions.IsRunning, "initial transitions complete");
        Thread.Sleep(300);
        var settled = harness.Read(w =>
        {
            Nazor.Platform.Windows.Win32.UpdateWindow(w.PlatformWindow.Handle);
            return w.FrameCount;
        });
        Thread.Sleep(500);
        Assert.Equal(settled, harness.Read(w => w.FrameCount));
        Assert.Empty(harness.Read(w => w.Exceptions));
    }

    [Fact]
    public void A_screen_reader_selects_a_device_and_edits_it()
    {
        using var harness = Open();
        var cards = UiaClient.Run(uia =>
        {
            var all = uia.FindAll(uia.ElementFromHandle(harness.Handle), TreeScope.Descendants);
            var cards = all.Where(e => e.GetCurrentControlType() == Uia.GroupControlType && e.GetCurrentAutomationId().StartsWith("device-", StringComparison.Ordinal))
                .Select(e => e.GetCurrentName()).ToList();
            var select = all.Single(e => e.GetCurrentName() == "Seleziona Misuratore qualità aria");
            select.Pattern<IUIAutomationInvokePattern>(Uia.InvokePattern).Invoke();
            return cards;
        });
        Assert.Equal(["Sensore temperatura", "Centralina laboratorio", "Misuratore qualità aria"], cards);
        harness.WaitUntil(w => ((App)w.Root.Component).Selected?.Id == 3, "the selection");

        UiaClient.Run(uia =>
        {
            var name = uia.Find(harness.Handle, "name-input");
            Assert.Equal("Nome dispositivo", name.GetCurrentName());
            name.Pattern<IUIAutomationValuePattern>(Uia.ValuePattern).SetValue("Qualità dell'aria — piano 2");
            uia.Find(harness.Handle, "active").Pattern<IUIAutomationTogglePattern>(Uia.TogglePattern).Toggle();
        });
        Assert.Equal("Qualità dell'aria — piano 2", harness.Read(w => ((App)w.Root.Component).Devices[2].Name));
        Assert.False(harness.Read(w => ((App)w.Root.Component).Devices[2].Active));
        var renamed = UiaClient.Run(uia => uia.Find(harness.Handle, "device-3").GetCurrentName());
        Assert.Equal("Qualità dell'aria — piano 2", renamed);
    }

    [Fact]
    public void Resizing_below_720_units_moves_the_detail_under_the_list()
    {
        using var harness = Open();
        bool DetailBesideList(NazorWindow w)
        {
            var list = w.Scene.GetBounds(w.Root.Elements().Single(e => e.Id == "list"));
            var detail = w.Scene.GetBounds(w.Root.Elements().Single(e => e.Attributes.GetValueOrDefault("accessible-name") is "Dettaglio dispositivo"));
            return detail.X >= list.Right;
        }
        Assert.True(harness.Read(DetailBesideList));
        harness.Read(w =>
        {
            w.PlatformWindow.Resize(new Size(640, 560));
            return true;
        });
        harness.WaitUntil(w => w.Scene.Viewport.Width < 700 && !DetailBesideList(w), "the narrow layout");
    }

    [Fact]
    public void Opening_and_closing_repeatedly_releases_native_resources_and_components()
    {
        using (var warmUp = Open())
        {
            warmUp.Read(w => w.FrameCount);
            UiaClient.Run(uia => uia.ElementFromHandle(warmUp.Handle).GetCurrentName());
        }
        var before = WindowHarness.GuiResources();
        for (var i = 0; i < 5; i++)
        {
            var harness = Open();
            UiaClient.Run(uia => uia.ElementFromHandle(harness.Handle).GetCurrentName());
            harness.Read(w =>
            {
                w.Root.Elements().First(e => e.Id == "add").SetState(Nazor.Rendering.ElementStates.Hover, true);
                w.RequestFrame();
                return true;
            });
            var renderer = harness.Read(w => w.Renderer);
            harness.Close();
            Assert.True(harness.HasExited);
            Assert.Equal(0, renderer.ComponentCount);
        }
        var after = WindowHarness.GuiResources();
        Assert.True(after.Gdi <= before.Gdi + 2, $"GDI objects {before.Gdi} -> {after.Gdi}");
        Assert.True(after.User <= before.User + 2, $"USER objects {before.User} -> {after.User}");
    }
}
