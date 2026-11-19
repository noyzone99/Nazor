using System.Globalization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Nazor.Controls;
using Nazor.Platform;
using Nazor.Rendering;
using Nazor.Testing;

namespace Nazor.Application.Tests;

public sealed class Amounts : ComponentBase
{
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenComponent<NumberField>(0);
        builder.AddComponentParameter(1, "Class", "german");
        builder.AddComponentParameter(2, "Value", 1234.5);
        builder.AddComponentParameter(3, "Format", "N2");
        builder.AddComponentParameter(4, "Culture", CultureInfo.GetCultureInfo("de-DE"));
        builder.CloseComponent();
        builder.OpenComponent<NumberField>(5);
        builder.AddComponentParameter(6, "Class", "current");
        builder.AddComponentParameter(7, "Value", 1234.5);
        builder.AddComponentParameter(8, "Format", "N2");
        builder.CloseComponent();
    }
}

public sealed class LocalizationTests
{
    private static T With<T>(string name, Func<T> body)
    {
        var (culture, ui) = (CultureInfo.CurrentCulture, CultureInfo.CurrentUICulture);
        CultureInfo.CurrentCulture = CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(name);
        try
        {
            return body();
        }
        finally
        {
            (CultureInfo.CurrentCulture, CultureInfo.CurrentUICulture) = (culture, ui);
        }
    }

    private static (string Heading, string Today, string Quantity) Sample()
    {
        using var services = new ServiceCollection().AddNazorServices().BuildServiceProvider();
        using var scope = services.CreateScope();
        using var host = new TestHost(scope.ServiceProvider);
        var root = host.Render<Nazor.Samples.Localization.App>();
        string Text(string name) => root.Elements().Single(e => e.Class == name).Text();
        return (Text("heading"), Text("today"), root.Elements().Single(e => e.Name == "NumberFieldInput").Attributes["value"] as string ?? "");
    }

    [Fact]
    public void An_application_speaks_the_language_of_the_interface_with_IStringLocalizer()
    {
        var english = With("en-US", Sample);
        var italian = With("it-IT", Sample);
        Assert.Equal("Hello! This window speaks your language", english.Heading);
        Assert.Equal("Ciao! Questa finestra parla la tua lingua", italian.Heading);
        Assert.Equal($"Today is {DateTime.Today.ToString("D", CultureInfo.GetCultureInfo("en-US"))}", english.Today);
        Assert.Equal($"Oggi \u00e8 {DateTime.Today.ToString("D", CultureInfo.GetCultureInfo("it-IT"))}", italian.Today);
        Assert.Equal(("1,234.50", "1.234,50"), (english.Quantity, italian.Quantity));
    }

    [Fact]
    public void A_language_without_resources_falls_back_to_the_neutral_one()
    {
        Assert.Equal("Hello! This window speaks your language", With("fr-FR", Sample).Heading);
    }

    [Fact]
    public void A_number_box_writes_in_its_culture_or_in_the_current_one()
    {
        var values = With("it-IT", () =>
        {
            using var host = new TestHost();
            var root = host.Render<Amounts>();
            return root.Elements().Where(e => e.Name == "NumberFieldInput").Select(e => e.Attributes["value"]).ToList();
        });
        Assert.Equal(["1.234,50", "1.234,50"], values);
        var english = With("en-US", () =>
        {
            using var host = new TestHost();
            return host.Render<Amounts>().Elements().Where(e => e.Name == "NumberFieldInput").Select(e => e.Attributes["value"]).ToList();
        });
        Assert.Equal(["1.234,50", "1,234.50"], english);
    }
}

[Collection("Windows")]
public sealed class ApplicationCultureTests
{
    [Theory]
    [InlineData(false, null, null)]
    [InlineData(false, "it-IT", null)]
    [InlineData(false, null, "en-US")]
    [InlineData(false, "it-IT", "en-US")]
    [InlineData(true, null, null)]
    [InlineData(true, "it-IT", null)]
    [InlineData(true, null, "en-US")]
    [InlineData(true, "it-IT", "en-US")]
    public async Task Overrides_are_independent_captured_and_shared_by_services_tray_and_the_UI_loop(bool asynchronous, string? culture, string? uiCulture)
    {
        var saved = (CultureInfo.DefaultThreadCurrentCulture, CultureInfo.DefaultThreadCurrentUICulture, CultureInfo.CurrentCulture, CultureInfo.CurrentUICulture);
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("de-DE");
            var probe = new CultureProbe();
            var builder = Builder(probe);
            builder.Application.Culture = culture is null ? null : CultureInfo.GetCultureInfo(culture);
            builder.Application.UICulture = uiCulture is null ? null : CultureInfo.GetCultureInfo(uiCulture);
            await using var app = builder.Build();
            builder.Application.Culture = CultureInfo.GetCultureInfo("ja-JP");
            builder.Application.UICulture = CultureInfo.GetCultureInfo("ar-EG");

            Assert.Equal(0, asynchronous ? await app.RunAsync(TestContext.Current.CancellationToken) : app.Run());

            var expected = (culture ?? "fr-FR", uiCulture ?? "de-DE");
            Assert.Equal(expected, probe.Started);
            Assert.Equal(expected, probe.Worker);
            Assert.Equal(expected, probe.Tray);
            Assert.Equal(expected, probe.Loop);
            Assert.Equal(expected, probe.Stopped);
            Assert.Equal(("fr-FR", "de-DE"), probe.AfterRun);
            Assert.Equal(("fr-FR", "de-DE"), Current());
            Assert.Equal(saved.Item1, CultureInfo.DefaultThreadCurrentCulture);
            Assert.Equal(saved.Item2, CultureInfo.DefaultThreadCurrentUICulture);
        }
        finally
        {
            (CultureInfo.CurrentCulture, CultureInfo.CurrentUICulture) = (saved.Item3, saved.Item4);
        }
    }

    [Theory]
    [InlineData(false, true)]
    [InlineData(false, false)]
    [InlineData(true, true)]
    [InlineData(true, false)]
    public async Task Culture_is_restored_when_host_startup_or_the_UI_loop_fails(bool asynchronous, bool startup)
    {
        var saved = Current();
        var defaults = (CultureInfo.DefaultThreadCurrentCulture, CultureInfo.DefaultThreadCurrentUICulture);
        var probe = new CultureProbe { FailStartup = startup, FailLoop = !startup };
        var builder = Builder(probe);
        builder.Application.Culture = CultureInfo.GetCultureInfo("it-IT");
        builder.Application.UICulture = CultureInfo.GetCultureInfo("en-US");
        await using var app = builder.Build();

        if (asynchronous)
            await Assert.ThrowsAsync<InvalidOperationException>(() => app.RunAsync(TestContext.Current.CancellationToken));
        else
            Assert.Throws<InvalidOperationException>(() => app.Run());

        Assert.Equal(("it-IT", "en-US"), probe.Started);
        Assert.Equal(("it-IT", "en-US"), probe.Stopped);
        Assert.Equal(saved, probe.AfterRun);
        Assert.Equal(saved, Current());
        Assert.Equal(defaults, (CultureInfo.DefaultThreadCurrentCulture, CultureInfo.DefaultThreadCurrentUICulture));
    }

    private static (string Culture, string UICulture) Current() => (CultureInfo.CurrentCulture.Name, CultureInfo.CurrentUICulture.Name);

    private static NazorApplicationBuilder Builder(CultureProbe probe)
    {
        var builder = NazorApplication.CreateBuilder(platformHost: new CultureHost(probe));
        builder.Diagnostics.DevChannel = false;
        builder.Services.AddSingleton(probe);
        builder.Services.AddSingleton<IHostedService>(probe);
        builder.Application.ShutdownMode = ShutdownMode.OnExplicitShutdown;
        builder.Application.UseTrayItem<CultureTray>();
        return builder;
    }

    private sealed class CultureProbe : IHostedService
    {
        public bool FailStartup { get; init; }
        public bool FailLoop { get; init; }
        public (string, string) Started, Worker, Tray, Loop, Stopped, AfterRun;

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            Started = Current();
            Worker = await Task.Run(Current, cancellationToken);
            if (FailStartup) throw new InvalidOperationException("Culture test startup failure.");
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            Stopped = Current();
            return Task.CompletedTask;
        }
    }

    private sealed class CultureTray(CultureProbe probe) : TrayItem
    {
        protected override void OnStarted() => probe.Tray = Current();
    }

    private sealed class CultureHost(CultureProbe probe) : IPlatformHost
    {
        public int Run(Func<IPlatform, int> application)
        {
            try { return application(new CulturePlatform(probe)); }
            finally { probe.AfterRun = Current(); }
        }
    }

    private sealed class CulturePlatform(CultureProbe probe) : IPlatform
    {
        public IPlatformDesktopIntegration DesktopIntegration { get; } = new TrayTestDesktop();
        public IPlatformWindow CreateWindow(IWindowHost host, WindowOptions options) => throw new NotSupportedException();
        public int Run(UIDispatcher dispatcher)
        {
            probe.Loop = Current();
            if (probe.FailLoop) throw new InvalidOperationException("Culture test UI failure.");
            return 0;
        }
        public void Quit(int exitCode = 0) { }
    }
}
