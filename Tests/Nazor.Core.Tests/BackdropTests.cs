namespace Nazor.Core.Tests;

public sealed class BackdropTests
{
    [Fact]
    public void Platform_overrides_preserve_the_common_preset_and_fallback()
    {
        BackdropSelection blur = CommonBackdrop.Blur;
        var customized = Backdrop.ForPlatform(CommonBackdrop.Blur, Windows: WindowsBackdrop.MicaAlt, Default: CommonBackdrop.Solid);
        Assert.Equal(CommonBackdrop.Blur, blur.Preset);
        Assert.Null(blur.Windows);
        Assert.Equal(CommonBackdrop.Blur, customized.Preset);
        Assert.Equal(WindowsBackdrop.MicaAlt, customized.Windows);
        Assert.Null(customized.Linux);
        Assert.Equal(CommonBackdrop.Solid, customized.Default);
        Assert.Equal(CommonBackdrop.None, blur.Default);
        var explicitSelection = Backdrop.ForPlatform(Windows: WindowsBackdrop.Acrylic, Linux: LinuxBackdrop.Blur);
        Assert.Equal(WindowsBackdrop.Acrylic, explicitSelection.Windows);
        Assert.Equal(LinuxBackdrop.Blur, explicitSelection.Linux);
        BackdropSelection linux = LinuxBackdrop.Blur;
        Assert.Equal(LinuxBackdrop.Blur, linux.Linux);
        Assert.Null(linux.Preset);
        Assert.Null(linux.Windows);
        Assert.Equal(CommonBackdrop.None, linux.Default);
    }

    [Fact]
    public void Invalid_materials_are_configuration_errors_not_unsupported_fallbacks()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Backdrop.ForPlatform((CommonBackdrop)99));
        Assert.Throws<ArgumentOutOfRangeException>(() => Backdrop.ForPlatform(Windows: (WindowsBackdrop)99));
        Assert.Throws<ArgumentOutOfRangeException>(() => Backdrop.ForPlatform(Linux: (LinuxBackdrop)99));
        Assert.Throws<ArgumentOutOfRangeException>(() => Backdrop.ForPlatform(Default: (CommonBackdrop)99));
    }
}
