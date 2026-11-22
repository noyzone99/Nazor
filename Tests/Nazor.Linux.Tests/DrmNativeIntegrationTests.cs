using System.Reflection;
using System.Runtime.InteropServices;
using Nazor.Graphics;
using Nazor.Platform;
using Nazor.Platform.Linux;

namespace Nazor.Linux.Tests;

[CollectionDefinition("DRM native", DisableParallelization = true)]
public sealed class DrmNativeCollection : ICollectionFixture<DrmNativeFixture>;

public sealed class DrmNativeFixture
{
    public DrmNativeFixture()
    {
        if (!DrmNativeIntegrationTests.HasFixture) return;
        var library = NativeLibrary.Load(Environment.GetEnvironmentVariable("NAZOR_DRM_TEST_LIBRARY")!);
        NativeLibrary.SetDllImportResolver(typeof(DrmPlatform).Assembly, (name, _, _) =>
            name is "libdrm.so.2" or "libgbm.so.1" or "libEGL.so.1" or "libseat.so.1" ||
                name == "libinput.so.10" && DrmNativeIntegrationTests.HasInputFixture ? library : 0);
        NativeLibrary.SetDllImportResolver(Assembly.GetExecutingAssembly(), (name, _, _) => name == "nazor-drm-fixture" ? library : 0);
    }
}

[Collection("DRM native")]
public sealed class DrmNativeIntegrationTests : IDisposable
{
    public static bool HasFixture => OperatingSystem.IsLinux() && Environment.GetEnvironmentVariable("NAZOR_DRM_TEST_LIBRARY") is { Length: > 0 };
    public static bool HasInputFixture => HasFixture && Environment.GetEnvironmentVariable("NAZOR_DRM_TEST_INPUT_FIXTURE") == "1";

    public DrmNativeIntegrationTests() { if (HasFixture) Reset(); }
    public void Dispose()
    {
        if (!HasFixture) return;
        foreach (var counter in new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 18, 19, 20, 21 }) Assert.Equal(0, Count(counter));
        Assert.Equal(0, Count(15));
    }

    [Fact(SkipUnless = nameof(HasFixture), Skip = "Requires the isolated native DRM fixture.")]
    public void Gbm_buffers_are_released_only_after_flip_and_the_original_display_is_restored()
    {
        using var session = new DrmSession();
        using var display = new DrmDisplay(session, new() { DevicePath = "/dev/dri/card0" });
        Assert.Equal(300, display.Width); Assert.Equal(180, display.Height);
        var context = new DrmOpenGlContext(display);
        try
        {
            context.Initialize();
            Assert.True(context.IsPresentationSynchronized);
            for (var index = 0; index < 8; index++) context.Present();
            Assert.Equal(7, Count(11));
            Assert.Equal(1, Count(10));
            Assert.Equal(300, Count(16)); Assert.Equal(180, Count(17));
        }
        finally { context.Dispose(); context.ReleaseDisplay(); }
        Assert.Equal(1, Count(12));
        Assert.Equal(0, Count(15));
    }

    [Fact(SkipUnless = nameof(HasFixture), Skip = "Requires the isolated native DRM fixture.")]
    public void Reclaimed_gbm_buffers_remove_stale_framebuffer_registrations()
    {
        using var session = new DrmSession();
        using var display = new DrmDisplay(session, new() { DevicePath = "/dev/dri/card0" });
        var context = new DrmOpenGlContext(display);
        try
        {
            context.Initialize(); Fault(11);
            for (var index = 0; index < 12; index++) context.Present();
            Assert.Equal(1, Count(6));
        }
        finally { context.Dispose(); context.ReleaseDisplay(); }
    }

    [Fact(SkipUnless = nameof(HasFixture), Skip = "Requires the isolated native DRM fixture.")]
    public void Gbm_framebuffers_register_the_modifier_and_metadata_for_every_plane()
    {
        using var session = new DrmSession();
        using var display = new DrmDisplay(session, new() { DevicePath = "/dev/dri/card0" });
        var context = new DrmOpenGlContext(display);
        try
        {
            context.Initialize();
            Fault(13);
            context.Present();
            context.Present();
            Assert.Equal(2, Count(22));
        }
        finally { context.Dispose(); context.ReleaseDisplay(); }
    }

    [Fact(SkipUnless = nameof(HasFixture), Skip = "Requires the isolated native DRM fixture and US XKB layout.")]
    public void Real_keyboard_state_preserves_modifiers_repeat_and_virtual_terminal_shortcuts()
    {
        using var session = new DrmSession();
        var events = new List<DrmEvent>();
        using var input = new DrmInput(session, 300, 180, events.Add);
        input.Key(30, true);
        Assert.Equal("a", events[^1].Text);
        input.Key(42, true); input.RepeatKey();
        Assert.Equal("A", events[^1].Text);
        Assert.True(events[^1].Repeat);
        Assert.Equal(Nazor.Input.KeyModifiers.Shift, events[^1].Modifiers);
        input.Key(42, false); input.Key(30, false);
        input.Key(29, true); input.Key(30, true);
        Assert.Null(events[^1].Text);
        input.Key(29, false); input.RepeatKey();
        Assert.Equal("a", events[^1].Text);
        input.Key(30, false); input.Key(29, true); input.Key(56, true); input.Key(59, true);
        Assert.Equal(1, Count(14));
    }

    [Theory(SkipUnless = nameof(HasFixture), Skip = "Requires the isolated native DRM fixture.")]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    public void Missing_devices_outputs_and_encoders_release_every_native_resource(int fault)
    {
        Fault(fault);
        using var session = new DrmSession();
        Assert.Throws<InvalidOperationException>(() => new DrmDisplay(session, new() { DevicePath = "/dev/dri/card0" }));
        Assert.Equal(0, Count(1));
    }

    [Theory(SkipUnless = nameof(HasFixture), Skip = "Requires the isolated native DRM fixture.")]
    [InlineData(5)]
    [InlineData(6)]
    [InlineData(7)]
    [InlineData(8)]
    [InlineData(10)]
    public void Gpu_failure_cleanup_preserves_the_scanout_buffer_until_restoration(int fault)
    {
        using var session = new DrmSession();
        using var display = new DrmDisplay(session, new() { DevicePath = "/dev/dri/card0" });
        var context = new DrmOpenGlContext(display);
        try
        {
            if (fault is 5 or 6)
            {
                Fault(fault);
                Assert.Throws<GraphicsDeviceException>(context.Initialize);
            }
            else
            {
                context.Initialize();
                if (fault == 8) context.Present();
                Fault(fault);
                Assert.Throws<GraphicsDeviceException>(context.Present);
            }
        }
        finally { context.Dispose(); context.ReleaseDisplay(); }
    }

    [Fact(SkipUnless = nameof(HasFixture), Skip = "Requires the isolated native DRM fixture.")]
    public unsafe void Software_buffers_honor_the_native_pitch_and_survive_gpu_initialization_fallback()
    {
        using var session = new DrmSession();
        using var display = new DrmDisplay(session, new() { DevicePath = "/dev/dri/card0" });
        var context = new DrmOpenGlContext(display);
        Fault(6);
        try { Assert.Throws<GraphicsDeviceException>(context.Initialize); }
        finally { context.Dispose(); context.ReleaseDisplay(); }
        Fault(0);
        var pixels = new byte[300 * 180 * 4];
        pixels.AsSpan().Fill(0x7b);
        fixed (byte* pointer = pixels)
            for (var index = 0; index < 4; index++) display.PresentSoftware(new((nint)pointer, 300, 180, 1200, 1));
        Assert.Equal(2, Count(7));
        Assert.Equal(3, Count(11));
    }

    [Fact(SkipUnless = nameof(HasFixture), Skip = "Requires the isolated native DRM fixture.")]
    public unsafe void Flip_timeout_can_restore_and_late_completion_does_not_finish_the_next_flip()
    {
        using var session = new DrmSession();
        using var display = new DrmDisplay(session, new() { DevicePath = "/dev/dri/card0" });
        var context = new DrmOpenGlContext(display);
        try
        {
            context.Initialize();
            context.Present();
            Fault(12);
            var failure = Assert.Throws<GraphicsDeviceException>(context.Present);
            Assert.Contains("timed out", failure.Message);
        }
        finally { context.Dispose(); context.ReleaseDisplay(); }

        Assert.Equal(1, Count(12));
        Fault(0);
        var pixels = new byte[300 * 180 * 4];
        fixed (byte* pointer = pixels)
        {
            var target = new PaintTarget((nint)pointer, 300, 180, 1200, 1);
            display.PresentSoftware(target);
            display.PresentSoftware(target);
        }
        Assert.Equal(2, Count(11));
        Assert.Equal(1, Count(23));
    }

    [Fact(SkipUnless = nameof(HasFixture), Skip = "Requires the isolated native DRM fixture.")]
    public unsafe void Software_flip_yields_to_seat_changes_and_reestablishes_scanout_on_resume()
    {
        using var session = new DrmSession();
        using var display = new DrmDisplay(session, new() { DevicePath = "/dev/dri/card0" });
        session.ActiveChanged = active => { if (!active) display.Suspend(); };
        var pixels = new byte[300 * 180 * 4];
        fixed (byte* pointer = pixels)
        {
            var target = new PaintTarget((nint)pointer, 300, 180, 1200, 1);
            Assert.True(display.PresentSoftware(target));
            Fault(16);
            Assert.False(display.PresentSoftware(target));
            Assert.Equal(0, Count(23));
            session.Dispatch(0);
            Assert.False(session.Active);
            Seat(1); session.Dispatch(0);
            Assert.True(display.PresentSoftware(target));
        }
        Assert.Equal(2, Count(11));
        Assert.Equal(1, Count(23));
    }

    [Fact(SkipUnless = nameof(HasFixture), Skip = "Requires the isolated native DRM fixture.")]
    public void Real_libinput_and_xkbcommon_can_suspend_resume_and_release_a_mock_display_seat()
    {
        using var backend = new DrmBackend(new() { DevicePath = "/dev/dri/card0" });
        var changes = new List<bool>();
        backend.ActiveChanged = changes.Add;
        Seat(0); backend.Dispatch(0);
        Assert.False(backend.Active);
        Assert.Equal(1, Count(13));
        Seat(1); backend.Dispatch(0);
        Assert.True(backend.Active);
        Assert.Equal(new[] { false, true }, changes);
    }

    [Fact(SkipUnless = nameof(HasInputFixture), Skip = "Requires the isolated libinput callback fixture.")]
    public void Input_callbacks_remain_valid_until_native_context_destruction()
    {
        using var session = new DrmSession();
        using (var input = new DrmInput(session, 300, 180, _ => { }))
            Assert.Equal(0, Count(24));

        Assert.Equal(1, Count(24));
        Assert.Equal(1, Count(25));
    }

    [Theory(SkipUnless = nameof(HasFixture), Skip = "Requires the isolated native DRM fixture.")]
    [InlineData(false)]
    [InlineData(true)]
    public void Seat_switch_reestablishes_the_display_mode_even_when_master_is_revoked_before_notification(bool revokeMaster)
    {
        using var backend = new DrmBackend(new() { DevicePath = "/dev/dri/card0" });
        var context = Assert.IsType<DrmOpenGlContext>(backend.CreateOpenGlContext());
        try
        {
            context.Initialize(); context.Present();
            if (revokeMaster) Fault(14);
            Seat(0);
            if (revokeMaster)
            {
                Assert.False(context.TryPresent());
                Assert.Equal(1, Count(10));
                Assert.Equal(0, Count(11));
            }
            backend.Dispatch(0);
            Assert.False(backend.Active);
            Assert.Equal(revokeMaster ? 0 : 1, Count(12));
            Assert.Equal(1, Count(10));
            Seat(1); backend.Dispatch(0);
            Assert.True(backend.Active);
            context.Present(); context.Present();
            Assert.Equal(1, Count(11));
            Assert.Equal(0, Count(15));
        }
        finally { context.Dispose(); context.ReleaseDisplay(); }
        Assert.Equal(revokeMaster ? 1 : 2, Count(12));
    }

    [Fact(SkipUnless = nameof(HasFixture), Skip = "Requires the isolated native DRM fixture.")]
    public async Task Host_preserves_application_exit_codes_and_errors_after_native_cleanup()
    {
        var host = new DrmPlatformHost(new() { DevicePath = "/dev/dri/card0" });
        Assert.Equal(42, await host.RunAsync(platform => { Assert.IsType<DrmPlatform>(platform); return 42; }));
        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => host.RunAsync(_ => throw new InvalidOperationException("Application failure")));
        Assert.Equal("Application failure", error.Message);
    }

    [Fact(SkipUnless = nameof(HasFixture), Skip = "Requires the isolated native DRM fixture and AUTO rendering.")]
    public async Task Single_view_application_falls_back_from_unavailable_GL_and_renders_real_Skia_pixels()
    {
        var builder = NazorApplication.CreateBuilder(platformHost: new DrmPlatformHost(new() { DevicePath = "/dev/dri/card0" }));
        builder.Application.UseMainView<GpuContent>();
        NazorWindow? window = null;
        Action? presented = null;
        builder.Diagnostics.ConfigureWindow = value => { window = value; value.FramePresented += _ => presented?.Invoke(); };
        using var app = builder.Build();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        Exception? failure = null;
        var verified = false;
        presented = () =>
        {
            if (verified) return;
            try
            {
                Assert.Equal(RenderingMode.Cpu, window!.Rendering.Actual);
                Assert.NotNull(window.Rendering.FallbackReason);
                var image = window.CaptureFrameAsync().GetAwaiter().GetResult();
                Assert.Equal(300, image.Width); Assert.Equal(180, image.Height);
                var pixel = image.GetPixel(30, 80);
                Assert.True(pixel.R > pixel.G + 50);
                verified = true;
            }
            catch (Exception error) { failure = error; }
            finally { app.Lifetime.StopApplication(); }
        };
        Assert.Equal(0, await app.RunAsync(timeout.Token));
        if (failure is not null) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failure).Throw();
        Assert.True(verified);
        Assert.True(Count(12) > 0);
    }

    [DllImport("nazor-drm-fixture", EntryPoint = "nz_test_reset")] private static extern void Reset();
    [DllImport("nazor-drm-fixture", EntryPoint = "nz_test_fault")] private static extern void Fault(int value);
    [DllImport("nazor-drm-fixture", EntryPoint = "nz_test_count")] private static extern int Count(int index);
    [DllImport("nazor-drm-fixture", EntryPoint = "nz_test_seat")] private static extern void Seat(int active);
}
