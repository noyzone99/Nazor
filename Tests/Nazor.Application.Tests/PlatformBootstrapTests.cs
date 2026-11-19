using Nazor.Controls;
using Nazor.Platform;
using Nazor.Platform.Windows;

namespace Nazor.Application.Tests;

[Collection("Windows")]
public sealed class PlatformBootstrapTests : IDisposable
{
    private readonly Func<IPlatformHost>? previousFactory = NazorApplication.PlatformHostFactory;

    public PlatformBootstrapTests() => NazorApplication.PlatformHostFactory = null;

    public void Dispose() => NazorApplication.PlatformHostFactory = previousFactory;

    [Fact]
    public void Each_builder_gets_a_host_and_later_registration_does_not_change_existing_builders()
    {
        NazorApplication.RegisterPlatformHost(static () => new RecordingHost(12));
        var first = Builder();
        var second = Builder();
        Assert.IsType<RecordingHost>(first.Platform.Host);
        Assert.NotSame(first.Platform.Host, second.Platform.Host);

        NazorApplication.RegisterPlatformHost(static () => new RecordingHost(24));
        using var firstApp = first.Build();
        using var secondApp = second.Build();
        Assert.Equal(12, firstApp.Run());
        Assert.Equal(12, secondApp.Run());
        using var later = Builder().Build();
        Assert.Equal(24, later.Run());
    }

    [Fact]
    public void Explicit_hosts_override_the_default_for_builder_and_direct_startup()
    {
        NazorApplication.RegisterPlatformHost(static () => new RecordingHost(12));
        var builder = Builder();
        var explicitHost = new RecordingHost(42);
        builder.Platform.Host = explicitHost;
        using var app = builder.Build();
        Assert.Equal(42, app.Run());

        NazorApplication.RegisterPlatformHost(static () => throw new InvalidOperationException("The default must not be used."));
        var explicitBuilder = NazorApplication.CreateBuilder(platformHost: explicitHost);
        explicitBuilder.Application.CreateMainWindow<Button>();
        using var explicitApp = explicitBuilder.Build();
        Assert.Equal(42, explicitApp.Run());
        Assert.Equal(2, explicitHost.Runs);
    }

    [Fact]
    public void Missing_registration_allows_explicit_configuration_and_explains_how_to_select_a_host()
    {
        var builder = Builder();
        Assert.Null(builder.Platform.Host);
        using var app = builder.Build();
        var error = Assert.Throws<InvalidOperationException>(() => app.Run());
        Assert.Contains("Nazor.Sdk", error.Message);
        Assert.Contains("Platform.Host", error.Message);


        var configured = Builder();
        configured.Platform.Host = new RecordingHost(42);
        using var configuredApp = configured.Build();
        Assert.Equal(42, configuredApp.Run());
    }

    [Fact]
    public void Windows_startup_still_selects_Windows_without_bootstrap_or_with_another_default()
    {
        var legacy = WindowsApplication.CreateBuilder();
        Assert.IsType<WindowsPlatformHost>(legacy.Platform.Host);
        legacy.Application.CreateMainWindow<Button>();
        using var legacyApp = legacy.Build();

        NazorApplication.RegisterPlatformHost(static () => new RecordingHost(12));
        var windows = WindowsApplication.CreateBuilder();
        Assert.IsType<WindowsPlatformHost>(windows.Platform.Host);
        windows.Application.CreateMainWindow<Button>();
        using var windowsApp = windows.Build();
        var explicitHost = new RecordingHost(42);
        Assert.Equal(42, ApplicationTestStartup.Run<Button>(new() { PlatformHost = explicitHost }));
    }

    [Fact]
    public void Loading_a_sample_executable_as_a_dependency_does_not_replace_the_process_default()
    {
        NazorApplication.RegisterPlatformHost(static () => new RecordingHost(42));
        var sample = System.Reflection.Assembly.Load(File.ReadAllBytes(typeof(Nazor.Samples.Counter.Counter).Assembly.Location));
        System.Runtime.CompilerServices.RuntimeHelpers.RunModuleConstructor(sample.ManifestModule.ModuleHandle);
        var builder = Builder();
        Assert.IsType<RecordingHost>(builder.Platform.Host);
        using var app = builder.Build();
    }

    [Fact]
    public void Null_factories_and_null_host_results_are_rejected()
    {
        Assert.Throws<ArgumentNullException>(() => NazorApplication.RegisterPlatformHost(null!));
        NazorApplication.RegisterPlatformHost(static () => null!);
        Assert.Throws<InvalidOperationException>(() => NazorApplication.CreateBuilder());

    }

    [Fact]
    public async Task Pre_canceled_async_run_does_not_start_the_backend_or_consume_the_application()
    {
        var platform = new RecordingHost(42);
        var builder = Builder();
        builder.Platform.Host = platform;
        await using var app = builder.Build();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => app.RunAsync(cancellation.Token));
        Assert.Equal(0, platform.Runs);
        Assert.Equal(42, await app.RunAsync(TestContext.Current.CancellationToken));
        Assert.Equal(1, platform.Runs);
        Assert.Throws<InvalidOperationException>(() => app.Run());
        await Assert.ThrowsAsync<InvalidOperationException>(() => app.RunAsync(TestContext.Current.CancellationToken));
    }

    private static NazorApplicationBuilder Builder()
    {
        var builder = NazorApplication.CreateBuilder();
        builder.Application.CreateMainWindow<Button>();
        return builder;
    }

    private sealed class RecordingHost(int exitCode) : IPlatformHost
    {
        public int Runs { get; private set; }

        public int Run(Func<IPlatform, int> application)
        {
            Runs++;
            return exitCode;
        }
    }
}
