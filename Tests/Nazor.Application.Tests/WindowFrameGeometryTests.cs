using Nazor.Graphics;
using Nazor.Platform;

namespace Nazor.Application.Tests;

public sealed class WindowFrameGeometryTests
{
    [Theory]
    [InlineData(-4, 30, 4, 5, null, 4u)]
    [InlineData(-6, 30, 4, 5, null, 0u)]
    [InlineData(2, 30, 0, 5, 16, 0u)]
    [InlineData(-3, 9, 0, 5, 16, 5u)]
    [InlineData(3, 9, 4, 0, 16, 5u)]
    [InlineData(3, 9, 4, 0, null, 4u)]
    [InlineData(15, 15, 4, 5, 16, 0u)]
    [InlineData(103, 83, 4, 5, 16, 10u)]
    public void Resize_edges_stay_in_the_configured_bands_and_extend_diagonally_near_corners(
        int x, int y, int inset, int outset, int? cornerLength, uint expected)
    {
        Assert.Equal(expected, WindowFrameGeometry.ResizeEdgeAt(new(100, 80), new(x, y), inset, outset, cornerLength));
    }

    [Fact]
    public void Oversized_frame_radii_shrink_together_to_fit()
    {
        var result = WindowFrameGeometry.NormalizeRadius(new(new(60, 60), new(40, 40), default, default), new(50, 100));
        Assert.Equal(new CornerRadius(new(30, 30), new(20, 20), default, default), result);
    }
}
