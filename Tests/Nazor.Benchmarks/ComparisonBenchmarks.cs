using System.Globalization;
using Nazor.Graphics;
using Nazor.Graphics.Text;
using Nazor.Nss;
using Nazor.Rendering;
using Nazor.Skia;
using Nazor.Testing;
using RendererComparison;

namespace Nazor.Benchmarks;

public sealed class ComparisonPage : ComponentBase
{
    internal static string Scenario = "text";
    private int value;
    internal void Advance(int next) { value = next; StateHasChanged(); }
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "Container");
        builder.AddAttribute(1, "class", $"comparison {(Scenario != "text" && value % 2 != 0 ? "alternate" : "")}");
        for (var index = 0; index < ComparisonWorkload.Cards; index++)
        {
            builder.OpenElement(2, "Container");
            builder.SetKey(index);
            builder.AddAttribute(3, "class", "card");
            builder.AddAttribute(4, "style", string.Create(CultureInfo.InvariantCulture,
                $"left:{ComparisonWorkload.Left(index)}px;top:{ComparisonWorkload.Top(index)}px"));
            builder.AddContent(5, ComparisonWorkload.Text(Scenario, index, value));
            builder.CloseElement();
        }
        builder.CloseElement();
    }
}

internal static class ComparisonBenchmarks
{
    internal static async Task<int> RunAsync(ComparisonOptions options)
    {
        if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException("The matched renderer comparison currently targets Windows.");
        Environment.SetEnvironmentVariable("NAZOR_RENDERER", options.Backend);
        ComparisonPage.Scenario = options.Scenario;
        using var session = new ComparisonSession(options);
        var builder = NazorApplication.CreateBuilder(platformHost: new DesktopPlatformHost());
        builder.Fonts = new FontOptions { UseSystemFonts = false, DefaultFamily = ComparisonWorkload.FontFamily, PrivateFontFiles = [ComparisonWorkload.FontPath] };
        builder.Application.CreateMainWindow<ComparisonPage>(window =>
        {
            window.Width = ComparisonWorkload.Width; window.Height = ComparisonWorkload.Height;
            window.Activate = false; window.IsResizable = false;
        });
        NazorWindow? target = null;
        Action<PresentedFrame>? completion = null;
        builder.Diagnostics.ConfigureWindow = window =>
        {
            target = window;
            var change = options.Scenario switch
            {
                "paint" => ".comparison.alternate .card { background:#f0dcba; }",
                "layout" => ".comparison.alternate .card { width:104px; }",
                _ => "",
            };
            var sheet = NssCompiler.Compile("renderer-comparison", """
                .comparison { position:relative; width:100%; height:100%; overflow:hidden; background:#ffffff; color:#202020; font-family:'Fira Sans'; font-size:14px; line-height:20px; }
                .card { position:absolute; box-sizing:border-box; width:100px; height:40px; padding:6px; border-radius:4px; overflow:hidden; background:#dce8f2; }
                """ + change);
            if (!sheet.Success) throw new InvalidOperationException("Invalid comparison stylesheet.");
            window.Scene.Styles.Application = sheet.StyleSheet;
            window.FramePresented += frame => completion?.Invoke(frame);
        };
        using var app = builder.Build();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30 + (options.Samples + options.Warmup) * 0.2));
        var initialized = false;
        var finalizing = false;
        var finalCaptured = false;
        var expectedFrame = int.MaxValue;
        Exception? failure = null;
        void Observe(Task task) => _ = task.ContinueWith(result =>
        {
            if (result.Exception is { } error) target!.Dispatcher.Post(() => { failure = error; app.Lifetime.StopApplication(); });
        }, TaskScheduler.Default);
        async Task CaptureFinalAsync()
        {
            var window = target!;
            var cards = window.Root.Elements("Container").Skip(1).ToArray();
            for (var index = 0; index < cards.Length; index++)
                if (cards[index].Text() != ComparisonWorkload.Text(options.Scenario, index, 1) ||
                    window.Scene.GetBounds(cards[index]).Width != ComparisonWorkload.CardWidth(options.Scenario, 1))
                    throw new InvalidOperationException($"Updated card content or dimensions are incorrect at {index}.");
            var image = await window.CaptureFrameAsync();
            using var encoder = new SkiaRenderer();
            File.WriteAllBytes(options.Output + ".final.png", encoder.EncodePng(image));
            finalCaptured = true;
            app.Lifetime.StopApplication();
        }
        void Next()
        {
            expectedFrame = target!.FrameCount + 1;
            session.Begin(((ComparisonPage)target.Root.Component).Advance);
        }
        async Task InitializeAsync()
        {
            var window = target!;
            var mode = options.Backend == "GPU" ? RenderingMode.Gpu : RenderingMode.Cpu;
            if (window.Rendering.Requested != mode || window.Rendering.Actual != mode) throw new InvalidOperationException("Requested renderer was not selected.");
            if (window.Scene.Viewport != new Size(ComparisonWorkload.Width, ComparisonWorkload.Height)) throw new InvalidOperationException("Unexpected client size.");
            var cards = window.Root.Elements("Container").Skip(1).ToArray();
            if (cards.Length != ComparisonWorkload.Cards) throw new InvalidOperationException("Missing comparison cards.");
            for (var index = 0; index < cards.Length; index++)
                if (window.Scene.GetBounds(cards[index]) != new Rect(ComparisonWorkload.Left(index), ComparisonWorkload.Top(index), 100, 40))
                    throw new InvalidOperationException($"Unexpected card bounds at {index}.");
            var image = await window.CaptureFrameAsync();
            using var encoder = new SkiaRenderer();
            File.WriteAllBytes(options.Output + ".png", encoder.EncodePng(image));
            Next();
        }
        completion = frame =>
        {
            try
            {
                var window = target!;
                if (window.Exceptions.Count > 0) throw new AggregateException(window.Exceptions);
                if (!initialized)
                {
                    initialized = true;
                    session.FirstRendered();
                    Observe(InitializeAsync());
                    return;
                }
                if (frame.UiFrame < expectedFrame) return;
                if (finalizing)
                {
                    expectedFrame = int.MaxValue;
                    Observe(CaptureFinalAsync());
                    return;
                }
                session.Rendered();
                if (session.Complete)
                {
                    session.Write("Nazor", typeof(NazorWindow).Assembly, window.Rendering.Backend, window.Rendering.Device,
                        window.Scene.Scale, "NazorWindow.FramePresented after native backend completion");
                    finalizing = true;
                    expectedFrame = window.FrameCount + 1;
                    ((ComparisonPage)window.Root.Component).Advance(1);
                    window.RequestFrame();
                }
                else Next();
            }
            catch (Exception exception) { failure = exception; app.Lifetime.StopApplication(); }
        };
        var exit = await app.RunAsync(timeout.Token);
        if (failure is not null) throw new InvalidOperationException("Nazor comparison failed.", failure);
        if (!session.Complete || !finalCaptured) throw new TimeoutException("Nazor comparison or final validation did not complete.");
        return exit;
    }
}
