using Nazor.Platform.Windows;

namespace Nazor.Application.Tests;

public sealed class BackdropResolutionTests
{
    [Fact]
    public void Missing_platform_mapping_uses_default_but_explicit_none_does_not()
    {
        var missing = Win32Window.ResolveBackdrop(Backdrop.ForPlatform(Linux: LinuxBackdrop.Blur, Default: CommonBackdrop.Solid), _ => true);
        Assert.Equal("mica", missing.Material);
        var disabled = Win32Window.ResolveBackdrop(Backdrop.ForPlatform(CommonBackdrop.None, Default: CommonBackdrop.Solid), _ => true);
        Assert.False(disabled.IsActive);
    }

    [Fact]
    public void Unsupported_override_uses_default_without_reactivating_the_replaced_preset()
    {
        var attempts = new List<WindowsBackdrop?>();
        var request = Backdrop.ForPlatform(CommonBackdrop.Blur, Windows: WindowsBackdrop.MicaAlt);
        var result = Win32Window.ResolveBackdrop(request, material =>
        {
            attempts.Add(material);
            return material is null;
        });
        Assert.Equal([WindowsBackdrop.MicaAlt, null], attempts);
        Assert.False(result.IsActive);
        Assert.Null(result.Material);
    }

    [Fact]
    public void Unsupported_material_uses_the_common_fallback()
    {
        var attempts = new List<WindowsBackdrop?>();
        var custom = Win32Window.ResolveBackdrop(Backdrop.ForPlatform(Windows: WindowsBackdrop.Acrylic, Default: CommonBackdrop.Solid), material =>
        {
            attempts.Add(material);
            return material == WindowsBackdrop.Mica;
        });
        Assert.Equal([WindowsBackdrop.Acrylic, WindowsBackdrop.Mica], attempts);
        Assert.Equal("mica", custom.Material);
    }

    [Theory]
    [InlineData(CommonBackdrop.None, null)]
    [InlineData(CommonBackdrop.Blur, "acrylic")]
    [InlineData(CommonBackdrop.Solid, "mica")]
    public void Common_presets_resolve_without_loading_a_native_window(CommonBackdrop preset, string? material)
    {
        var result = Win32Window.ResolveBackdrop(preset, _ => true);
        Assert.Equal(material is not null, result.IsActive);
        Assert.Equal(material, result.Material);
    }

    [Fact]
    public void Rejected_material_and_fallback_disable_the_previous_effect()
    {
        var attempts = new List<WindowsBackdrop?>();
        var result = Win32Window.ResolveBackdrop(Backdrop.ForPlatform(Windows: WindowsBackdrop.MicaAlt, Default: CommonBackdrop.Blur), material =>
        {
            attempts.Add(material);
            return material is null;
        });
        Assert.Equal([WindowsBackdrop.MicaAlt, WindowsBackdrop.Acrylic, null], attempts);
        Assert.False(result.IsActive);
        Assert.Null(result.Material);
    }
}
