using Nazor.Graphics.Text;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Nazor.Animation;
using Nazor.Graphics;
using Nazor.Input;
using Nazor.Nss;
using Nazor.Presentation;
using Nazor.Skia;
using Nazor.Testing;

namespace Nazor.Benchmarks;

internal readonly record struct FrameSample(
    double ComponentsMs, double StyleMs, double LayoutMs, double DisplayListMs, double RasterMs,
    int StylesRecomputed, int LayoutComputations, int Commands, long AllocatedBytes, long Pixels, double ToolRoundTripMs = 0)
{
    public double TotalMs => ComponentsMs + StyleMs + LayoutMs + DisplayListMs + RasterMs;
}

internal sealed unsafe class BenchHost : IDisposable
{
    private static readonly string FontDirectory = Path.Combine(AppContext.BaseDirectory, "Fonts");

    private byte* pixels;
    private int width;
    private int height;
    private int layoutComputations;
    private TimeSpan styleDuration, layoutDuration;

    public BenchHost(Size viewport, float scale = 1, string? styleSheet = null)
        : this(() => viewport, scale, styleSheet)
    {
    }

    public BenchHost(Func<Size> viewport, float scale = 1, string? styleSheet = null)
    {
        Host = new TestHost();
        Graphics = new SkiaRenderer(new FontOptions
        {
            PrivateFontFiles = [Path.Combine(FontDirectory, "FiraSans-Regular.ttf"), Path.Combine(FontDirectory, "FiraSans-Bold.ttf")],
            UseSystemFonts = false,
            DefaultFamily = "Fira Sans",
        });
        Scene = new Scene(Host.Renderer, Graphics.Fonts, Graphics.Shaper, images: Graphics.Images, clock: Clock)
        {
            Viewport = viewport(),
            Scale = scale,
        };
        Scene.Styles.Theme = Nazor.Themes.Themes.Basic;
        if (styleSheet is not null)
        {
            var compiled = NssCompiler.Compile("bench", styleSheet);
            if (!compiled.Success) throw new InvalidOperationException(string.Join(Environment.NewLine, compiled.Diagnostics));
            Scene.Styles.Application = compiled.StyleSheet;
        }
        Input = new InputManager(Host.Renderer, Scene);
        Scene.Updated += RecordUpdate;
        size = viewport;
        Scale = scale;
    }

    public TestHost Host { get; }

    public SkiaRenderer Graphics { get; }

    public Scene Scene { get; }

    public InputManager Input { get; }

    public ManualAnimationClock Clock { get; } = new();

    private readonly Func<Size> size;

    public Size Viewport => size();

    public float Scale { get; }

    public FrameSample Frame(Action? change = null)
    {
        layoutComputations = 0;
        styleDuration = layoutDuration = TimeSpan.Zero;
        var recomputed = Scene.Styles.TotalRecomputedCount;
        var allocated = GC.GetAllocatedBytesForCurrentThread();
        var start = Stopwatch.GetTimestamp();
        change?.Invoke();
        Host.RunPending();
        Scene.Viewport = Viewport;
        Scene.Scale = Scale;
        Scene.Update();
        var update = Stopwatch.GetTimestamp();
        var displayList = Scene.BuildDisplayList();
        var built = Stopwatch.GetTimestamp();
        var pixels = Raster(displayList);
        var end = Stopwatch.GetTimestamp();
        var styleMs = styleDuration.TotalMilliseconds;
        var layoutMs = layoutDuration.TotalMilliseconds;
        return new(Math.Max(0, Ms(start, update) - styleMs - layoutMs), styleMs, layoutMs, Ms(update, built), Ms(built, end),
            checked((int)(Scene.Styles.TotalRecomputedCount - recomputed)), layoutComputations, displayList.Commands.Count,
            GC.GetAllocatedBytesForCurrentThread() - allocated, pixels);
    }

    private void RecordUpdate()
    {
        layoutComputations += Scene.LastUpdate.LayoutComputations;
        styleDuration += Scene.LastUpdateDuration.Styles;
        layoutDuration += Scene.LastUpdateDuration.Layout;
    }

    public void Settle(int frames = 3)
    {
        for (var i = 0; i < frames; i++)
        {
            Frame();
            if (!Host.Dispatcher.HasPendingWork && !Scene.Transitions.IsRunning) break;
        }
    }

    private long Raster(DisplayList displayList)
    {
        var w = Math.Max(1, (int)MathF.Ceiling(Viewport.Width * Scale));
        var h = Math.Max(1, (int)MathF.Ceiling(Viewport.Height * Scale));
        if (w != width || h != height)
        {
            NativeMemory.AlignedFree(pixels);
            pixels = (byte*)NativeMemory.AlignedAlloc((nuint)(w * h * 4), 64);
            (width, height) = (w, h);
            frameDamage.Reset();
        }
        var damage = frameDamage.Next(displayList, Scale) is { } changed ? DisplayListDamage.ToPixels(changed, Scale, width, height) : null;
        Graphics.Render(displayList, (nint)pixels, width, height, width * 4, Scale, Scene.Background, damage);
        return damage?.Sum(r => (long)r.Width * r.Height) ?? (long)width * height;
    }

    private readonly DisplayListDamage frameDamage = new();

    private static double Ms(long from, long to) => (to - from) * 1000.0 / Stopwatch.Frequency;

    public void Dispose()
    {
        Scene.Updated -= RecordUpdate;
        Input.Dispose();
        Scene.Dispose();
        Host.Dispose();
        Graphics.Dispose();
        NativeMemory.AlignedFree(pixels);
        pixels = null;
    }
}
