using Nazor.Platform.Linux;

namespace Nazor.Linux.Tests;

public sealed class BackdropTests
{
    [Theory]
    [InlineData(CommonBackdrop.None, false)]
    [InlineData(CommonBackdrop.Blur, true)]
    [InlineData(CommonBackdrop.Solid, false)]
    public void Common_presets_resolve_without_loading_a_compositor(CommonBackdrop preset, bool enabled)
    {
        var attempts = new List<bool>();
        var result = LinuxDesktopWindow.ResolveBackdrop(preset, blur => { attempts.Add(blur); return true; });
        Assert.Equal([enabled], attempts);
        Assert.Equal(enabled, result.IsActive);
        Assert.Equal(enabled ? "WaylandBlur" : null, result.Material);
    }

    [Fact]
    public void Linux_override_takes_precedence_and_windows_only_selection_uses_default()
    {
        var customized = LinuxDesktopWindow.ResolveBackdrop(Backdrop.ForPlatform(CommonBackdrop.Solid, Linux: LinuxBackdrop.Blur), _ => true);
        Assert.Equal("WaylandBlur", customized.Material);
        var missing = LinuxDesktopWindow.ResolveBackdrop(Backdrop.ForPlatform(Windows: WindowsBackdrop.Mica, Default: CommonBackdrop.Blur), _ => true);
        Assert.Equal("WaylandBlur", missing.Material);
        var disabled = LinuxDesktopWindow.ResolveBackdrop(Backdrop.ForPlatform(CommonBackdrop.None, Default: CommonBackdrop.Blur), _ => false);
        Assert.False(disabled.IsActive);
        var explicitLinux = LinuxDesktopWindow.ResolveBackdrop(LinuxBackdrop.Blur, _ => true);
        Assert.Equal("WaylandBlur", explicitLinux.Material);
    }

    [Theory]
    [InlineData(CommonBackdrop.None)]
    [InlineData(CommonBackdrop.Solid)]
    public void Unsupported_blur_disables_the_effect_using_the_common_fallback(CommonBackdrop fallback)
    {
        var attempts = new List<bool>();
        var result = LinuxDesktopWindow.ResolveBackdrop(Backdrop.ForPlatform(Linux: LinuxBackdrop.Blur, Default: fallback), enabled =>
        {
            attempts.Add(enabled);
            return !enabled;
        });
        Assert.Equal([true, false], attempts);
        Assert.False(result.IsActive);
        Assert.Null(result.Material);
    }

    [Fact]
    public void Unsupported_blur_fallback_also_clears_the_effect()
    {
        var attempts = new List<bool>();
        var result = LinuxDesktopWindow.ResolveBackdrop(Backdrop.ForPlatform(Linux: LinuxBackdrop.Blur, Default: CommonBackdrop.Blur), enabled =>
        {
            attempts.Add(enabled);
            return false;
        });
        Assert.Equal([true, true, false], attempts);
        Assert.False(result.IsActive);
    }
}
