using Nazor.Platform.Linux;

namespace Nazor.Linux.Tests;

public sealed class DrmOptionsTests
{
    [Theory]
    [InlineData(float.NaN)]
    [InlineData(float.PositiveInfinity)]
    [InlineData(0)]
    [InlineData(0.49f)]
    [InlineData(8.01f)]
    public void Invalid_scale_is_rejected_before_opening_devices(float scale) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => new DrmOptions { Scale = scale }.Validate());

    [Theory]
    [InlineData("")]
    [InlineData("/dev/dri/renderD128")]
    [InlineData("/dev/dri/card")]
    [InlineData("/dev/dri/card0/../../input/event0")]
    public void Only_primary_card_paths_are_accepted(string path) =>
        Assert.Throws<ArgumentException>(() => new DrmOptions { DevicePath = path }.Validate());

    [Fact]
    public void Connected_card_detection_is_default_and_explicit_selection_preserves_fractional_scale()
    {
        Assert.Null(new DrmOptions().Validate().DevicePath);
        var options = new DrmOptions { DevicePath = "/dev/dri/card12", ConnectorId = 4, Scale = 1.5f };
        Assert.Same(options, options.Validate());
        Assert.Throws<ArgumentOutOfRangeException>(() => (options with { ConnectorId = 0 }).Validate());
    }
}
