using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using Nazor.Benchmarks;
using RendererComparison;

if (args.FirstOrDefault() == "--compare") return await ComparisonBenchmarks.RunAsync(ComparisonOptions.Parse(args[1..]));

var samples = 30;
string? output = null;
string? check = null;
var countersOnly = false;
var devtools = false;
var native = false;
for (var i = 0; i < args.Length; i++)
{
    switch (args[i])
    {
        case "--samples": samples = int.Parse(args[++i], CultureInfo.InvariantCulture); break;
        case "--out": output = args[++i]; break;
        case "--check": check = args[++i]; break;
        case "--counters-only": countersOnly = true; break;
        case "--devtools": devtools = true; break;
        case "--native": native = true; break;
        default: Console.Error.WriteLine($"Unknown argument: {args[i]}"); return 2;
    }
}

if (native)
{
    if (samples is < 1 or > 10000 || devtools || check is not null || countersOnly)
    {
        Console.Error.WriteLine("Native mode requires 1–10000 samples and cannot use --check, --counters-only or --devtools.");
        return 2;
    }
    return await NativeBenchmarks.RunAsync(samples,output);
}

var results = new JsonObject();
Console.WriteLine($"{"situation",-20} {"total",8} {"min",7} {"p95",8} {"comp",7} {"style",7} {"layout",7} {"display",7} {"raster",7} {"styles",7} {"layouts",7} {"commands",8} {"alloc KB",9} {"pixels",9}");
foreach (var (name, list) in devtools ? DevToolsScenarios.All(samples) : Scenarios.All(samples))
{
    var metrics = Metrics(list);
    results[name] = metrics;
    double M(string key) => metrics[key]!.GetValue<double>();
    Console.WriteLine(string.Create(CultureInfo.InvariantCulture,
        $"{name,-20} {M("totalMs"),8:F2} {M("minMs"),7:F2} {M("p95Ms"),8:F2} {M("componentsMs"),7:F2} {M("styleMs"),7:F2} {M("layoutMs"),7:F2} {M("displayListMs"),7:F2} {M("rasterMs"),7:F2} {M("stylesRecomputed"),7} {M("layoutComputations"),7} {M("commands"),8} {M("allocatedKB"),9:F0} {M("pixels"),9}"));
}

if (output is not null)
{
    Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(output))!);
    File.WriteAllText(output, new JsonObject
    {
        ["machine"] = Environment.MachineName,
        ["processors"] = Environment.ProcessorCount,
        ["date"] = DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture),
        ["results"] = results,
    }.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
}

if (check is null) return 0;
var failures = new List<string>();
foreach (var (name, limits) in JsonNode.Parse(File.ReadAllText(check))!.AsObject())
{
    if (name.StartsWith('$')) continue;
    if (results[name] is not JsonObject measured)
    {
        failures.Add($"{name}: no such situation");
        continue;
    }
    foreach (var (metric, limit) in limits!.AsObject())
    {
        if (countersOnly && metric.EndsWith("Ms", StringComparison.Ordinal)) continue;
        var value = measured[metric]?.GetValue<double>() ?? double.NaN;
        if (!(value <= limit!.GetValue<double>())) failures.Add(string.Create(CultureInfo.InvariantCulture, $"{name}.{metric}: {value:F2} > {limit}"));
    }
}
foreach (var failure in failures) Console.Error.WriteLine($"Threshold exceeded: {failure}");
Console.WriteLine(failures.Count == 0 ? "Benchmark thresholds met." : $"{failures.Count} thresholds exceeded.");
return failures.Count == 0 ? 0 : 1;

static JsonObject Metrics(List<FrameSample> samples)
{
    static double Median(IEnumerable<double> values) => Percentile(values, 0.5);
    static double Percentile(IEnumerable<double> values, double p)
    {
        var sorted = values.Order().ToArray();
        return sorted.Length == 0 ? 0 : sorted[Math.Min(sorted.Length - 1, (int)Math.Ceiling(p * sorted.Length) - 1)];
    }
    return new JsonObject
    {
        ["samples"] = (double)samples.Count,
        ["totalMs"] = Math.Round(Median(samples.Select(s => s.TotalMs)), 3),
        ["p95Ms"] = Math.Round(Percentile(samples.Select(s => s.TotalMs), 0.95), 3),
        ["minMs"] = Math.Round(samples.Min(s => s.TotalMs), 3),
        ["minRasterMs"] = Math.Round(samples.Min(s => s.RasterMs), 3),
        ["componentsMs"] = Math.Round(Median(samples.Select(s => s.ComponentsMs)), 3),
        ["styleMs"] = Math.Round(Median(samples.Select(s => s.StyleMs)), 3),
        ["layoutMs"] = Math.Round(Median(samples.Select(s => s.LayoutMs)), 3),
        ["displayListMs"] = Math.Round(Median(samples.Select(s => s.DisplayListMs)), 3),
        ["rasterMs"] = Math.Round(Median(samples.Select(s => s.RasterMs)), 3),
        ["stylesRecomputed"] = (double)samples.Max(s => s.StylesRecomputed),
        ["layoutComputations"] = (double)samples.Max(s => s.LayoutComputations),
        ["commands"] = (double)samples.Max(s => s.Commands),
        ["pixels"] = (double)samples.Max(s => s.Pixels),
        ["toolRoundTripMs"] = Math.Round(Median(samples.Select(s => s.ToolRoundTripMs)), 3),
        ["allocatedKB"] = Math.Round(Median(samples.Select(s => s.AllocatedBytes / 1024.0)), 1),
    };
}
