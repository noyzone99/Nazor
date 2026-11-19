using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Layout;
using Avalonia.Logging;
using Avalonia.Media;
using Avalonia.Rendering.Composition;
using Avalonia.Threading;
using Avalonia.Win32;
using RendererComparison;

namespace Nazor.Avalonia.Benchmarks;

internal static class Program
{
    internal static ComparisonOptions Options = null!;
    internal static ComparisonSession Session = null!;
    internal static string? Device;
    internal static volatile Exception? Failure;
    [STAThread]
    private static int Main(string[] args)
    {
        if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException("The matched renderer comparison currently targets Windows.");
        Options = ComparisonOptions.Parse(args);
        using var session = Session = new(Options);
        Logger.Sink = new RenderFailureSink();
        AppBuilder.Configure<ComparisonApplication>().UsePlatformDetect()
            .With(new Win32PlatformOptions
            {
                RenderingMode = [Options.Backend == "CPU" ? Win32RenderingMode.Software : Win32RenderingMode.AngleEgl],
                GraphicsAdapterSelectionCallback = adapters =>
                {
                    for (var index = 0; index < adapters.Count; index++)
                    {
                        var name = adapters[index].Description;
                        if (string.IsNullOrWhiteSpace(name) || new[] { "Microsoft Basic", "software", "WARP", "llvmpipe", "swiftshader" }
                            .Any(value => name.Contains(value, StringComparison.OrdinalIgnoreCase))) continue;
                        Device = name;
                        return index;
                    }
                    throw new InvalidOperationException("No identified hardware adapter is available; GPU comparison cannot use software fallback.");
                },
            })
            .With(new AngleOptions { AllowedPlatformApis = [AngleOptions.PlatformApi.DirectX11] })
            .StartWithClassicDesktopLifetime([], ShutdownMode.OnMainWindowClose);
        if (Failure is not null) throw new InvalidOperationException("Avalonia comparison failed.", Failure);
        if (!Session.Complete) throw new TimeoutException("Avalonia comparison did not complete.");
        return 0;
    }
}

internal sealed class RenderFailureSink : ILogSink
{
    public bool IsEnabled(LogEventLevel level, string area) => level >= LogEventLevel.Error && area == LogArea.Visual;
    public void Log(LogEventLevel level, string area, object? source, string messageTemplate) =>
        Program.Failure = new InvalidOperationException($"Avalonia {area}: {messageTemplate}");
    public void Log(LogEventLevel level, string area, object? source, string messageTemplate, params object?[] propertyValues) =>
        Log(level, area, source, messageTemplate + " " + string.Join(", ", propertyValues));
}

internal sealed class ComparisonApplication : Application
{
    public override void OnFrameworkInitializationCompleted()
    {
        var lifetime = (IClassicDesktopStyleApplicationLifetime)ApplicationLifetime!;
        var options = Program.Options;
        var session = Program.Session;
        var font = new FontFamily("avares://Nazor.Avalonia.Benchmarks/Fonts#Fira Sans");
        var cards = new Border[ComparisonWorkload.Cards];
        var labels = new TextBlock[cards.Length];
        var first = Brush.Parse("#dce8f2");
        var second = Brush.Parse("#f0dcba");
        var panel = new Canvas { Background = Brushes.White, ClipToBounds = true };
        for (var index = 0; index < cards.Length; index++)
        {
            labels[index] = new TextBlock
            {
                Text = ComparisonWorkload.Text(options.Scenario, index, 0), FontFamily = font, FontSize = 14,
                LineHeight = 20, Foreground = Brush.Parse("#202020"), VerticalAlignment = VerticalAlignment.Top,
            };
            cards[index] = new Border
            {
                Width = 100, Height = 40, Padding = new Thickness(6), Background = first,
                CornerRadius = new CornerRadius(4), ClipToBounds = true, Child = labels[index],
            };
            Canvas.SetLeft(cards[index], ComparisonWorkload.Left(index));
            Canvas.SetTop(cards[index], ComparisonWorkload.Top(index));
            panel.Children.Add(cards[index]);
        }
        var window = new Window
        {
            Width = ComparisonWorkload.Width, Height = ComparisonWorkload.Height,
            CanResize = false, ShowActivated = false, Content = panel, Background = Brushes.White,
        };
        lifetime.MainWindow = window;
        var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(30 + (options.Samples + options.Warmup) * 0.2) };
        timer.Tick += (_, _) => { Program.Failure = new TimeoutException("Avalonia comparison timed out."); lifetime.Shutdown(1); };
        window.Opened += async (_, _) =>
        {
            try
            {
                timer.Start();
                var visual = ElementComposition.GetElementVisual(panel) ?? throw new InvalidOperationException("No composition visual.");
                var compositor = visual.Compositor;
                await compositor.RequestCompositionBatchCommitAsync().Rendered;
                session.FirstRendered();
                if (options.Backend == "GPU" && Program.Device is null) throw new InvalidOperationException("GPU adapter selection was not verified.");
                if (panel.Bounds.Size != new Size(ComparisonWorkload.Width, ComparisonWorkload.Height)) throw new InvalidOperationException("Unexpected client size.");
                if (!FontManager.Current.TryGetGlyphTypeface(new Typeface(font), out var typeface) ||
                    !typeface.FamilyNames.Any(name => name.Value == ComparisonWorkload.FontFamily))
                    throw new InvalidOperationException("The private comparison font was not loaded.");
                for (var index = 0; index < cards.Length; index++)
                    if (cards[index].Bounds != new Rect(ComparisonWorkload.Left(index), ComparisonWorkload.Top(index), 100, 40))
                        throw new InvalidOperationException($"Unexpected card bounds at {index}.");
                using (var image = await compositor.CreateCompositionVisualSnapshot(visual, window.RenderScaling))
                    image.Save(options.Output + ".png", global::Avalonia.Media.Imaging.PngBitmapEncoderOptions.Default);
                void Update(int value)
                {
                    for (var index = 0; index < cards.Length; index++)
                        switch (options.Scenario)
                        {
                            case "text": labels[index].Text = ComparisonWorkload.Text(options.Scenario, index, value); break;
                            case "paint": cards[index].Background = value % 2 == 0 ? first : second; break;
                            case "layout": cards[index].Width = ComparisonWorkload.CardWidth(options.Scenario, value); break;
                        }
                }
                while (!session.Complete)
                {
                    session.Begin(Update);
                    await compositor.RequestCompositionBatchCommitAsync().Rendered;
                    session.Rendered();
                }
                session.Write("Avalonia", typeof(Application).Assembly, options.Backend == "CPU" ? "Skia software / Win32" : "Skia ANGLE / Direct3D 11",
                    Program.Device, window.RenderScaling, "Compositor composition batch Rendered after render-thread target pass");
                Update(1);
                await compositor.RequestCompositionBatchCommitAsync().Rendered;
                for (var index = 0; index < cards.Length; index++)
                    if (labels[index].Text != ComparisonWorkload.Text(options.Scenario, index, 1) ||
                        cards[index].Bounds.Width != ComparisonWorkload.CardWidth(options.Scenario, 1))
                        throw new InvalidOperationException($"Updated card content or dimensions are incorrect at {index}.");
                using (var image = await compositor.CreateCompositionVisualSnapshot(visual, window.RenderScaling))
                    image.Save(options.Output + ".final.png", global::Avalonia.Media.Imaging.PngBitmapEncoderOptions.Default);
                timer.Stop();
                lifetime.Shutdown();
            }
            catch (Exception exception) { Program.Failure = exception; lifetime.Shutdown(1); }
        };
        base.OnFrameworkInitializationCompleted();
    }
}
