using Nazor.Graphics;

namespace Nazor.Application.Tests;

public sealed class RendererSelectionTests
{
    [Theory]
    [InlineData(null, RenderingMode.Auto)]
    [InlineData("", RenderingMode.Auto)]
    [InlineData("AUTO", RenderingMode.Auto)]
    [InlineData(" gpu ", RenderingMode.Gpu)]
    [InlineData("CPU", RenderingMode.Cpu)]
    public void Startup_preference_is_explicit_and_case_insensitive(string? value, RenderingMode expected) =>
        Assert.Equal(expected, RendererSelection.Parse(value));

    [Theory]
    [InlineData("VULKAN")]
    [InlineData("1")]
    [InlineData("GP U")]
    public void Unknown_preference_does_not_silently_select_another_renderer(string value) =>
        Assert.Contains("NAZOR_RENDERER", Assert.Throws<InvalidOperationException>(() => RendererSelection.Parse(value)).Message);
}
