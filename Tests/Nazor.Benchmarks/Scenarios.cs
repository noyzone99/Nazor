using System.Diagnostics;
using Nazor.Graphics;
using Nazor.Input;
using Nazor.Testing;

namespace Nazor.Benchmarks;

internal static class Scenarios
{
    private const int Warmup = 5;

    public static IEnumerable<(string Name, List<FrameSample> Samples)> All(int samples)
    {
        VerifyMeasurement();
        yield return ("dashboard.startup", Startup(samples));
        foreach (var result in Dashboard(samples)) yield return result;
        yield return ("list.scroll", ListScroll(samples));
        yield return ("list.jump", ListJump(samples));
        yield return ("article.resize", Resize(samples));
        yield return ("motion.transition", Motion());
    }

    private static void VerifyMeasurement()
    {
        using var bench = new BenchHost(new Size(800, 600));
        var dispatched = false;
        var sample = bench.Frame(() =>
        {
            bench.Host.Render<MotionPage>();
            bench.Scene.Update();
            bench.Host.Dispatcher.Post(() => dispatched = true);
        });
        if (!dispatched || sample.StylesRecomputed == 0 || sample.LayoutComputations == 0 || bench.Scene.LastUpdate.StylesRecomputed != 0)
            throw new InvalidOperationException("Frame measurement lost input-time scene work or queued dispatcher work.");
        var lazy = bench.Frame(() =>
        {
            bench.Scene.Styles.InvalidateAll();
            bench.Scene.GetStyle(bench.Host.Renderer.RootComponents.Single().Elements().First());
        });
        if (lazy.StylesRecomputed != sample.StylesRecomputed || bench.Scene.LastUpdate.StylesRecomputed != 0)
            throw new InvalidOperationException("Frame measurement lost lazy style recomputations.");
    }

    private static List<FrameSample> Startup(int samples)
    {
        var result = new List<FrameSample>();
        for (var i = 0; i < Warmup + Math.Min(samples, 10); i++)
        {
            using var bench = new BenchHost(new Size(1280, 800), styleSheet: DashboardPage.StyleSheet);
            var start = Stopwatch.GetTimestamp();
            var allocated = GC.GetAllocatedBytesForCurrentThread();
            bench.Host.Render<DashboardPage>();
            var rendered = Stopwatch.GetTimestamp();
            var frame = bench.Frame();
            if (i >= Warmup)
                result.Add(frame with
                {
                    ComponentsMs = frame.ComponentsMs + (rendered - start) * 1000.0 / Stopwatch.Frequency,
                    AllocatedBytes = GC.GetAllocatedBytesForCurrentThread() - allocated,
                });
        }
        return result;
    }

    private static IEnumerable<(string, List<FrameSample>)> Dashboard(int samples)
    {
        using var bench = new BenchHost(new Size(1280, 800), styleSheet: DashboardPage.StyleSheet);
        var page = (DashboardPage)bench.Host.Render<DashboardPage>().Component;
        bench.Settle();

        yield return ("dashboard.idle", Measure(samples, _ => { }, bench));

        Point[] points = [new(100, 40), new(290, 40)];
        yield return ("dashboard.hover", Measure(samples, i => bench.Input.PointerMoved(points[i % 2], KeyModifiers.None), bench));

        yield return ("dashboard.text", Measure(samples, i => page.Cards[i % 4].SetValue($"{i} pz"), bench));
    }

    private static List<FrameSample> ListScroll(int samples)
    {
        using var bench = new BenchHost(new Size(800, 600));
        var root = bench.Host.Render<OrdersPage>();
        bench.Settle();
        string FirstRow() => root.Elements().First(e => e.Name == "ListViewItem").Text();
        var top = FirstRow();
        var viewport = root.Single("ListViewViewport");
        var previous = bench.Scene.GetScrollState(viewport)!.OffsetY;
        var result = new List<FrameSample>();
        for (var i = 0; i < Warmup + samples; i++)
        {
            var step = bench.Frame(() => bench.Input.PointerWheel(new Point(100, 100), -1, KeyModifiers.None));
            for (var frame = 1; frame < 3 && bench.Host.Dispatcher.HasPendingWork; frame++)
            {
                step = Add(step, bench.Frame());
            }
            var offset = bench.Scene.GetScrollState(viewport)!.OffsetY;
            if (offset <= previous) throw new InvalidOperationException("The wheel did not advance the list viewport.");
            if (root.Elements().Count(e => e.Name == "ListViewItem") > 40)
                throw new InvalidOperationException("The scrolling list created more than 40 rows.");
            previous = offset;
            if (i >= Warmup) result.Add(step);
        }
        if (FirstRow() == top) throw new InvalidOperationException($"The list did not scroll: its first row is still '{top}'.");
        return result;
    }

    private static List<FrameSample> ListJump(int samples)
    {
        using var bench = new BenchHost(new Size(800, 600));
        var root = bench.Host.Render<OrdersPage>();
        bench.Settle();
        var list = root.Elements().Single(e => e.Name == "ListViewViewport");
        var result = new List<FrameSample>();
        for (var i = 0; i < Warmup + samples; i++)
        {
            var toEnd = i % 2 == 0;
            var step = bench.Frame(() =>
            {
                if (!bench.Input.ScrollBy(list, toEnd ? 10_000_000 : -10_000_000))
                    throw new InvalidOperationException("The jump did not move the list viewport.");
            });
            for (var frame = 1; frame < 4 && bench.Host.Dispatcher.HasPendingWork; frame++)
            {
                step = Add(step, bench.Frame());
            }
            if (i >= Warmup) result.Add(step);
            var rows = root.Elements().Count(e => e.Name == "ListViewItem");
            if (rows > 40) throw new InvalidOperationException($"The list created {rows} rows.");
            var edge = toEnd ? root.Elements().Last(e => e.Name == "ListViewItem").Text()
                : root.Elements().First(e => e.Name == "ListViewItem").Text();
            var expected = toEnd ? $"Ordine {OrdersPage.Count - 1}" : "Ordine 0";
            if (edge != expected) throw new InvalidOperationException($"The jump did not reach '{expected}': its edge is '{edge}'.");
        }
        return result;
    }

    private static List<FrameSample> Resize(int samples)
    {
        var result = new List<FrameSample>();
        var width = 900f;
        using var bench = new BenchHost(() => new Size(width, 700), styleSheet: ArticlePage.StyleSheet);
        bench.Host.Render<ArticlePage>();
        bench.Settle();
        for (var i = 0; i < Warmup + samples; i++)
        {
            width = 900 - i % 24 * 7;
            var sample = bench.Frame();
            if (i >= Warmup) result.Add(sample);
        }
        return result;
    }

    private static List<FrameSample> Motion()
    {
        using var bench = new BenchHost(new Size(800, 600), styleSheet: MotionPage.StyleSheet);
        var page = (MotionPage)bench.Host.Render<MotionPage>().Component;
        bench.Settle();
        var result = new List<FrameSample>();
        for (var run = 0; run < 3; run++)
        {
            page.Toggle();
            bench.Frame();
            for (var frame = 0; frame < 20; frame++)
            {
                bench.Clock.Advance(16);
                if (frame == 5 && !bench.Scene.Transitions.IsRunning) throw new InvalidOperationException("The transitions are not running.");
                var sample = bench.Frame();
                if (run > 0) result.Add(sample);
            }
        }
        return result;
    }

    private static List<FrameSample> Measure(int samples, Action<int> change, BenchHost bench)
    {
        var result = new List<FrameSample>();
        for (var i = 0; i < Warmup + samples; i++)
        {
            var sample = bench.Frame(() => change(i));
            if (i >= Warmup) result.Add(sample);
        }
        return result;
    }

    private static FrameSample Add(FrameSample a, FrameSample b) => new(
        a.ComponentsMs + b.ComponentsMs, a.StyleMs + b.StyleMs, a.LayoutMs + b.LayoutMs, a.DisplayListMs + b.DisplayListMs, a.RasterMs + b.RasterMs,
        a.StylesRecomputed + b.StylesRecomputed, a.LayoutComputations + b.LayoutComputations, Math.Max(a.Commands, b.Commands),
        a.AllocatedBytes + b.AllocatedBytes, a.Pixels + b.Pixels);
}
