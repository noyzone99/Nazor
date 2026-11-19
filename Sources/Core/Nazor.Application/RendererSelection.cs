using Nazor.Graphics;

namespace Nazor;

internal static class RendererSelection
{
    internal static RenderingMode ReadEnvironment() => Parse(Environment.GetEnvironmentVariable("NAZOR_RENDERER"));

    internal static RenderingMode Parse(string? value) => value?.Trim().ToUpperInvariant() switch
    {
        null or "" or "AUTO" => RenderingMode.Auto,
        "GPU" => RenderingMode.Gpu,
        "CPU" => RenderingMode.Cpu,
        _ => throw new InvalidOperationException($"Invalid NAZOR_RENDERER value '{value}'. Expected AUTO, GPU, or CPU."),
    };
}
