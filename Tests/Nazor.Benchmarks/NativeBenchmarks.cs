using System.Diagnostics;
using System.Text.Json;
using Nazor.Graphics;
using Nazor.Input;
using Nazor.Nss;
using Nazor.Platform;
using Nazor.Rendering;

namespace Nazor.Benchmarks;

public sealed class NativeBenchmarkPage : ComponentBase
{
    private int value;
    internal void Advance() { value++; StateHasChanged(); }
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0,"Container");
        builder.AddAttribute(1,"class","native-page");
        for (var index=0;index<80;index++)
        {
            builder.OpenElement(2,"Container");
            builder.SetKey(index);
            builder.AddAttribute(3,"class","native-card");
            builder.AddContent(4,$"{index}: {value}");
            builder.CloseElement();
        }
        builder.CloseElement();
    }
}

internal static class NativeBenchmarks
{
    private sealed class Observer : IWindowInstrumentation
    {
        internal WindowFrameMetrics? Last;
        public bool InterceptsPointer => false;
        public PlatformCursor? PointerCursor => null;
        public bool PointerMoved(Point point) => false;
        public bool PointerPressed(Point point,PointerButton button) => false;
        public bool PointerReleased(Point point,PointerButton button) => false;
        public bool PointerLeft() => false;
        public void PointerCanceled() { }
        public bool KeyDown(string key) => false;
        public bool KeyUp(string key) => false;
        public DisplayList Compose(DisplayList list) => list;
        public void RecordFrame(WindowFrameMetrics frame) => Last=frame;
        public void Dispose() { }
    }

    internal static async Task<int> RunAsync(int samples,string? output)
    {
        var observer = new Observer();
        var builder = NazorApplication.CreateBuilder(platformHost:new DesktopPlatformHost());
        builder.Application.CreateMainWindow<NativeBenchmarkPage>(window => { window.Width=900; window.Height=600; window.Activate=false; });
        NazorWindow? target = null;
        Action<PresentedFrame>? probe = null;
        builder.Diagnostics.ConfigureWindow = window =>
        {
            window.Scene.Styles.Application=NssCompiler.Compile("native-benchmark","""
                .native-page { display:flex; flex-wrap:wrap; gap:8px; padding:12px; background:#ffffff; }
                .native-card { width:95px; height:40px; padding:6px; background:#dce8f2; border-radius:4px; }
                """).StyleSheet;
            window.Instrumentation=observer;
            window.FramePresented += frame => probe?.Invoke(frame);
            target=window;
        };
        using var app = builder.Build();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15+samples*0.25));
        var ui = new List<double>();
        var worker = new List<double>();
        var observed = new List<double>();
        var allocated = new List<double>();
        long started=0, bytes=0;
        var beforeUi=0;
        var measuring=false;
        var completed=0;
        Exception? failure=null;
        probe = presentation =>
        {
            var window=target!;
                try
                {
                    if (completed>=samples+5 || failure is not null) return;
                    if (window.Exceptions.Count>0) throw new AggregateException(window.Exceptions);
                    var gpu=window.Rendering.Actual==RenderingMode.Gpu;
                    if (observer.Last is not { } frame || frame.Number!=presentation.UiFrame) return;
                    if (measuring)
                    {
                        if (frame.Number<=beforeUi) return;
                        if (completed++>=5)
                        {
                            ui.Add(frame.Total.TotalMilliseconds);
                            if (gpu) worker.Add(presentation.BackendDuration.TotalMilliseconds);
                            observed.Add(Stopwatch.GetElapsedTime(started).TotalMilliseconds);
                            allocated.Add((GC.GetTotalAllocatedBytes()-bytes)/1024d);
                        }
                        measuring=false;
                    }
                    if (completed==samples+5) { app.Lifetime.StopApplication(); return; }
                    beforeUi=window.FrameCount;
                    bytes=GC.GetTotalAllocatedBytes(); started=Stopwatch.GetTimestamp();
                    ((NativeBenchmarkPage)window.Root.Component).Advance();
                    measuring=true;
                }
                catch (Exception exception) { failure=exception; app.Lifetime.StopApplication(); }
        };
        var exit=await app.RunAsync(timeout.Token);
        if (failure is not null) throw new InvalidOperationException("Native rendering benchmark failed.",failure);
        if (completed!=samples+5) throw new TimeoutException("Native rendering samples did not complete.");
        static object Summary(List<double> values)
        {
            values.Sort();
            return new { median=values[values.Count/2], p95=values[(int)Math.Ceiling(values.Count*0.95)-1] };
        }
        var report=new
        {
            renderer=target!.Rendering, samples, workload="80 cards; all text values change once per sample; 900x600 logical pixels",
            scale=target.Scene.Scale, uiPaintMs=Summary(ui),
            workerSubmissionAndPresentMs=worker.Count==0 ? null : Summary(worker),
            observedCompletionMs=Summary(observed), managedAllocatedKB=Summary(allocated),
            note="Observed completion includes UI dispatcher wakeup after backend completion. Worker time is CPU submission plus presentation wait, not GPU execution or input-to-screen latency. No Avalonia comparison is implied.",
        };
        var json=JsonSerializer.Serialize(report,new JsonSerializerOptions
        {
            WriteIndented=true, Converters={ new System.Text.Json.Serialization.JsonStringEnumConverter() },
        });
        Console.WriteLine(json);
        if (output is not null)
        {
            var path=Path.GetFullPath(output);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            await File.WriteAllTextAsync(path,json);
        }
        return exit;
    }
}
