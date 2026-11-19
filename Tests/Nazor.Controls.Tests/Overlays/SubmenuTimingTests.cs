using Nazor.Controls.Tests.Overlays.Fixtures;
using Nazor.Input;
using Nazor.Nss;
using Nazor.Style;

namespace Nazor.Controls.Tests.Overlays;

[Collection("Controls")]
public sealed class SubmenuTimingTests : IDisposable
{
    private readonly ControlHarness h = new();
    public void Dispose() => h.Dispose();
    private SubmenuTimingProbe Render(bool customDelay = true, bool reduced = false)
    {
        h.Scene.Styles.Theme = Nazor.Themes.Themes.Basic;
        if (customDelay)
        {
            var result = NssCompiler.Compile("submenu-timing", "Submenu { submenu-open-delay: 60ms; }");
            Assert.True(result.Success, string.Join("\n", result.Diagnostics));
            h.Scene.Styles.Application = result.StyleSheet;
        }
        h.Scene.Transitions.ReducedMotion = reduced;
        h.Scene.Styles.Preferences = new MediaPreferences(ColorScheme.Light, reduced);
        h.Render<SubmenuTimingProbe>();
        return h.Component<SubmenuTimingProbe>();
    }
    private Task Enter() => h.Host.Renderer.DispatchEventAsync(h.One("Submenu"), "onpointerenter", new PointerEventArgs());

    [Theory]
    [InlineData(true, 60)]
    [InlineData(false, 250)]
    public async Task Pointer_opening_follows_theme_delay_without_restarting_the_timer_on_render(bool customDelay, int milliseconds)
    {
        var probe = Render(customDelay);
        await Enter(); h.Frame();
        probe.Clock.Advance(TimeSpan.FromMilliseconds(milliseconds - 1)); h.Frame();
        Assert.False(probe.Submenu.Opened);
        probe.Refresh(); h.Frame();
        probe.Clock.Advance(TimeSpan.FromMilliseconds(1)); h.Frame();
        Assert.True(probe.Submenu.Opened);
        Assert.Equal(0, probe.Clock.ActiveTimers);
    }

    [Theory]
    [InlineData("leave")]
    [InlineData("disable")]
    [InlineData("remove")]
    [InlineData("close-parent")]
    public async Task Leaving_disabling_removing_or_closing_the_owner_cancels_pending_opening(string reason)
    {
        var probe = Render();
        var submenu = probe.Submenu;
        await Enter(); h.Frame();
        Assert.Equal(1, probe.Clock.ActiveTimers);
        switch (reason)
        {
            case "leave": await h.Host.Renderer.DispatchEventAsync(h.One("Submenu"), "onpointerleave", new PointerEventArgs()); break;
            case "disable": probe.Disabled = true; probe.Refresh(); break;
            case "remove": probe.Show = false; probe.Refresh(); break;
            case "close-parent": probe.Open = false; probe.Refresh(); break;
        }
        h.Frame();
        probe.Clock.Advance(TimeSpan.FromMilliseconds(100)); h.Frame();
        Assert.False(submenu.Opened);
        Assert.Equal(0, probe.Clock.ActiveTimers);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Keyboard_activation_and_reduced_motion_open_immediately_without_a_pending_timer(bool reduced)
    {
        var probe = Render(reduced: reduced);
        if (reduced) await Enter();
        else await h.Host.Renderer.DispatchEventAsync(h.One("Submenu"), "onkeydown", new KeyboardEventArgs { Key = "ArrowRight" });
        h.Frame();
        Assert.True(probe.Submenu.Opened);
        Assert.Equal(0, probe.Clock.ActiveTimers);
    }
}
