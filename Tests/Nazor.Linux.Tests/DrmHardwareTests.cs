using Nazor.Graphics;
using Nazor.Platform.Linux;

namespace Nazor.Linux.Tests;

public sealed class DrmHardwareTests
{
    public static bool HasDisplay => OperatingSystem.IsLinux() && Environment.GetEnvironmentVariable("NAZOR_TEST_DRM") == "1" &&
        Environment.GetEnvironmentVariable("NAZOR_DRM_TEST_LIBRARY") is null;

    [Fact(SkipUnless = nameof(HasDisplay), Skip = "Requires explicit DRM hardware opt-in from an active local console.")]
    public async Task Display_presents_captures_and_releases_the_requested_renderer()
    {
        var builder = NazorApplication.CreateBuilder(platformHost: new DrmPlatformHost(new()
        {
            DevicePath = Environment.GetEnvironmentVariable("NAZOR_TEST_DRM_DEVICE")
        }));
        builder.Application.UseMainView<GpuContent>();
        Action<NazorWindow>? inspect = null;
        builder.Diagnostics.ConfigureWindow = window => window.FramePresented += _ => inspect?.Invoke(window);
        using var app = builder.Build();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(20));
        Exception? failure = null;
        var started = false;
        var verified = false;
        inspect = async window =>
        {
            if (started) return;
            started = true;
            try
            {
                Console.WriteLine($"Nazor DRM renderer: {window.Rendering}");
                Assert.Empty(window.Exceptions);
                var requested = Environment.GetEnvironmentVariable("NAZOR_RENDERER");
                if (requested == "GPU") Assert.Equal(RenderingMode.Gpu, window.Rendering.Actual);
                if (requested == "CPU") Assert.Equal(RenderingMode.Cpu, window.Rendering.Actual);
                var image = await window.CaptureFrameAsync();
                var pixel = image.GetPixel(image.Width / 2, image.Height / 2);
                Assert.True(pixel.R > pixel.G + 50);
                verified = true;
            }
            catch (Exception error) { failure = error; }
            finally { app.Lifetime.StopApplication(); }
        };
        Assert.Equal(0, await app.RunAsync(timeout.Token));
        if (failure is not null) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failure).Throw();
        Assert.True(verified);
    }
}
