using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.X86;
using System.Text;
using Microsoft.Extensions.Logging;
using Nazor.Graphics;

namespace Nazor;

internal static class StartupLog
{
    private static readonly EventId PlatformStarted = new(1000, nameof(PlatformStarted));
    private static readonly EventId RendererSelected = new(1001, nameof(RendererSelected));
    private static readonly EventId RendererPending = new(1002, nameof(RendererPending));
    private static readonly Lazy<string> CpuDevice = new(ReadCpuDevice);

    internal static void Platform(ILogger? logger, string backend)
    {
        if (logger?.IsEnabled(LogLevel.Information) != true) return;
        if (OperatingSystem.IsLinux())
        {
            var session = Environment.GetEnvironmentVariable("XDG_SESSION_TYPE")?.Trim();
            var sessionType = session?.ToLowerInvariant() switch
            {
                "wayland" => "Wayland",
                "x11" => "X11",
                "tty" => "TTY",
                null or "" => "unknown session",
                _ => session,
            };
            var desktop = Environment.GetEnvironmentVariable("XDG_CURRENT_DESKTOP")?.Trim();
            logger.LogInformation(PlatformStarted,
                "Running on {SessionType} with {PlatformBackend} backend (desktop: {DesktopEnvironment}).",
                sessionType, backend, string.IsNullOrEmpty(desktop) ? "unknown" : desktop);
        }
        else
        {
            logger.LogInformation(PlatformStarted, "Running with {PlatformBackend} backend.", backend);
        }
    }

    internal static void Renderer(ILogger? logger, RenderingInfo rendering, bool failed = false)
    {
        var level = failed ? LogLevel.Warning : LogLevel.Information;
        if (logger?.IsEnabled(level) != true) return;
        var device = rendering.Actual == RenderingMode.Cpu ? CpuDevice.Value : rendering.Device ?? "unknown";
        var actual = rendering.Actual.ToString().ToUpperInvariant();
        var requested = rendering.Requested.ToString().ToUpperInvariant();
        if (rendering.FallbackReason is { } reason)
            logger.Log(level, RendererSelected,
                "Renderer: {Renderer} ({GraphicsBackend}) on {RenderingDevice}. Requested: {RequestedRenderer}. Reason: {FallbackReason}",
                actual, rendering.Backend, device, requested, reason);
        else
            logger.Log(level, RendererSelected,
                "Renderer: {Renderer} ({GraphicsBackend}) on {RenderingDevice}. Requested: {RequestedRenderer}.",
                actual, rendering.Backend, device, requested);
    }

    internal static void RendererDeferred(ILogger? logger, RenderingMode requested) =>
        logger?.LogInformation(RendererPending, "Renderer selection deferred until a window opens. Requested: {RequestedRenderer}.",
            requested.ToString().ToUpperInvariant());

    private static string ReadCpuDevice()
    {
        const int brandStart = unchecked((int)0x80000002);
        const int brandEnd = unchecked((int)0x80000004);
        if (X86Base.IsSupported && (uint)X86Base.CpuId(int.MinValue, 0).Eax >= unchecked((uint)brandEnd))
        {
            Span<int> brand = stackalloc int[12];
            for (var leaf = brandStart; leaf <= brandEnd; leaf++)
            {
                var offset = (leaf - brandStart) * 4;
                (brand[offset], brand[offset + 1], brand[offset + 2], brand[offset + 3]) = X86Base.CpuId(leaf, 0);
            }
            var model = Encoding.ASCII.GetString(MemoryMarshal.AsBytes(brand)).Trim('\0', ' ');
            if (model.Length != 0) return model;
        }
        return RuntimeInformation.OSArchitecture.ToString();
    }
}
