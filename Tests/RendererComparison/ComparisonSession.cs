using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.Json;

namespace RendererComparison;

internal sealed record ComparisonOptions(string Backend, string Scenario, int Samples, int Warmup, string Output)
{
    internal static ComparisonOptions Parse(string[] args)
    {
        var backend = "GPU";
        var scenario = "text";
        var samples = 120;
        var warmup = 30;
        string? output = null;
        for (var index = 0; index < args.Length; index++)
        {
            if (index + 1 == args.Length) throw new ArgumentException($"Missing value for {args[index]}.");
            var key = args[index++];
            var value = args[index];
            switch (key)
            {
                case "--backend": backend = value.ToUpperInvariant(); break;
                case "--scenario": scenario = value.ToLowerInvariant(); break;
                case "--samples": samples = int.Parse(value, CultureInfo.InvariantCulture); break;
                case "--warmup": warmup = int.Parse(value, CultureInfo.InvariantCulture); break;
                case "--out": output = Path.GetFullPath(value); break;
                default: throw new ArgumentException($"Unknown argument {key}.");
            }
        }
        if (backend is not ("CPU" or "GPU") || scenario is not ("text" or "paint" or "layout") ||
            samples is < 10 or > 10000 || warmup is < 5 or > 1000 || output is null)
            throw new ArgumentException("Use --backend CPU|GPU --scenario text|paint|layout --samples 10..10000 --warmup 5..1000 --out file.json.");
        return new(backend, scenario, samples, warmup, output);
    }
}

internal static class ComparisonWorkload
{
    internal const int Width = 900, Height = 600, Cards = 80, Columns = 8;
    internal const string FontFamily = "Fira Sans";
    internal static string FontPath => Path.Combine(AppContext.BaseDirectory, "Fonts", "FiraSans-Regular.ttf");
    internal static int Left(int index) => 12 + index % Columns * 108;
    internal static int Top(int index) => 12 + index / Columns * 48;
    internal static int CardWidth(string scenario, int value) => scenario == "layout" && value % 2 != 0 ? 104 : 100;
    internal static string Text(string scenario, int index, int value) => $"{index:D2}:{(scenario == "text" ? value : 0):D5}";
}

internal sealed class ComparisonSession : IDisposable
{
    private readonly Process process = Process.GetCurrentProcess();
    private readonly List<double> completion;
    private readonly List<double> mutation;
    private readonly long created = Stopwatch.GetTimestamp();
    private long started, measurementStarted, allocatedBefore;
    private TimeSpan cpuBefore;
    private int[]? gcBefore;
    private double mutationDuration;
    private int completed;
    private bool pending;

    internal ComparisonOptions Options { get; }
    internal double FirstCompletionMs { get; private set; }
    internal bool Complete => completed == Options.Warmup + Options.Samples;
    internal int Value => completed + 1;

    internal ComparisonSession(ComparisonOptions options)
    {
        Options = options;
        Directory.CreateDirectory(Path.GetDirectoryName(options.Output)!);
        completion = new(options.Samples);
        mutation = new(options.Samples);
    }

    internal void FirstRendered() => FirstCompletionMs = Stopwatch.GetElapsedTime(created).TotalMilliseconds;

    internal void Begin(Action<int> update)
    {
        if (pending || Complete) throw new InvalidOperationException("Only one measured update may be pending.");
        if (completed == Options.Warmup)
        {
            gcBefore = [GC.CollectionCount(0), GC.CollectionCount(1), GC.CollectionCount(2)];
            cpuBefore = process.TotalProcessorTime;
            allocatedBefore = GC.GetTotalAllocatedBytes(precise: true);
            measurementStarted = Stopwatch.GetTimestamp();
        }
        pending = true;
        started = Stopwatch.GetTimestamp();
        update(Value);
        mutationDuration = Stopwatch.GetElapsedTime(started).TotalMilliseconds;
    }

    internal void Rendered()
    {
        if (!pending) throw new InvalidOperationException("No measured update is pending.");
        if (completed++ >= Options.Warmup)
        {
            completion.Add(Stopwatch.GetElapsedTime(started).TotalMilliseconds);
            mutation.Add(mutationDuration);
        }
        pending = false;
    }

    internal void Write(string framework, Assembly assembly, string backend, string? device, double scale, string completionSignal)
    {
        if (!Complete || completion.Count != Options.Samples) throw new InvalidOperationException("Incomplete benchmark run.");
        var elapsed = Stopwatch.GetElapsedTime(measurementStarted).TotalMilliseconds;
        var allocated = GC.GetTotalAllocatedBytes(precise: true) - allocatedBefore;
        var cpu = (process.TotalProcessorTime - cpuBefore).TotalMilliseconds;
        var collections = Enumerable.Range(0, 3).Select(index => GC.CollectionCount(index) - gcBefore![index]).ToArray();
        var gcMemory = GC.GetGCMemoryInfo();
        var managedBytes = GC.GetTotalMemory(forceFullCollection: false);
        process.Refresh();
        var workingSet = process.WorkingSet64;
        var privateMemory = process.PrivateMemorySize64;
        if (int.TryParse(Environment.GetEnvironmentVariable("NAZOR_COMPARISON_MEMORY_PAUSE_MS"), out var pause) && pause is > 0 and <= 60000)
        {
            File.WriteAllText(Options.Output + ".memory-ready", process.Id.ToString(CultureInfo.InvariantCulture));
            Thread.Sleep(pause);
        }
        static string? BindingVersion(string name) => AppDomain.CurrentDomain.GetAssemblies()
            .FirstOrDefault(loaded => loaded.GetName().Name == name)?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        static object Distribution(List<double> values)
        {
            var sorted = values.Order().ToArray();
            double Percentile(double value) => sorted[Math.Clamp((int)Math.Ceiling(sorted.Length * value) - 1, 0, sorted.Length - 1)];
            return new { median = Percentile(0.5), p95 = Percentile(0.95), mean = sorted.Average() };
        }
        var report = new
        {
            schema = 1, framework,
            benchmarkVersion = typeof(ComparisonSession).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion,
            frameworkVersion = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion,
            renderingBindings = new { skiaSharp = BindingVersion("SkiaSharp"), harfBuzzSharp = BindingVersion("HarfBuzzSharp") },
            requestedBackend = Options.Backend, actualBackend = backend, device,
            scenario = Options.Scenario, samples = Options.Samples, warmup = Options.Warmup,
            environment = new
            {
                machine = Environment.MachineName, os = RuntimeInformation.OSDescription,
                runtime = RuntimeInformation.FrameworkDescription, architecture = RuntimeInformation.ProcessArchitecture.ToString(),
                processors = Environment.ProcessorCount, scale, utc = DateTimeOffset.UtcNow,
            },
            workload = new
            {
                width = ComparisonWorkload.Width, height = ComparisonWorkload.Height, cards = ComparisonWorkload.Cards,
                columns = ComparisonWorkload.Columns, font = ComparisonWorkload.FontFamily, fontSize = 14, lineHeight = 20,
                fontSha256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(ComparisonWorkload.FontPath))),
            },
            completionMs = Distribution(completion), mutationCallMs = Distribution(mutation),
            cpuMsPerUpdate = cpu / Options.Samples, allocatedKBPerUpdate = allocated / 1024d / Options.Samples,
            workingSetMB = workingSet / (1024d * 1024), privateMemoryMB = privateMemory / (1024d * 1024),
            memory = new
            {
                managedBytes, gcMemory.HeapSizeBytes, gcMemory.FragmentedBytes, gcMemory.TotalCommittedBytes,
                gcMemory.PromotedBytes, gcMemory.PinnedObjectsCount, gcMemory.FinalizationPendingCount,
                gcMemory.Index, gcMemory.Generation, serverGC = System.Runtime.GCSettings.IsServerGC,
            },
            gcCollections = collections,
            completedUpdatesPerSecond = Options.Samples * 1000 / elapsed,
            firstCompletionMs = FirstCompletionMs, completionSignal, completionSamplesMs = completion,
            note = "Closed-loop updates, one in flight. Completion includes scheduling, backend submission/presentation and UI notification; not physical scanout or GPU execution time. Mutation calls exclude deferred work. Process CPU/allocations include all threads. Startup time excludes process launch. Snapshot is captured before warmup.",
        };
        Directory.CreateDirectory(Path.GetDirectoryName(Options.Output)!);
        File.WriteAllText(Options.Output, JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true }));
        Console.WriteLine($"{framework} {Options.Backend} {Options.Scenario}: {Options.Samples} completed updates -> {Options.Output}");
    }

    public void Dispose() => process.Dispose();
}
