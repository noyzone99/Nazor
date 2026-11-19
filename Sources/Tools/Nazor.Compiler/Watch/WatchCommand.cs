using System.Globalization;

namespace Nazor.Compiler.Watch;

internal static class WatchCommand
{
    private const string Prefix = "nazorc watch: ";
    private static readonly TimeSpan QuietPeriod = TimeSpan.FromMilliseconds(150);
    private static readonly TimeSpan ScanInterval = TimeSpan.FromSeconds(2);

    public static async Task<int> RunAsync(NazorCompilerOptions options, string outputDirectory, string projectDirectory, string? responseFile, CancellationToken cancellationToken)
    {
        var session = new WatchSession(options, outputDirectory, projectDirectory, responseFile);
        using var client = new DevChannelClient(session.ProjectDirectory, null, Console.Out);
        var reporter = new Reporter(session, client);
        var first = session.Refresh();
        Console.WriteLine($"{Prefix}watching {session.ProjectDirectory} -> {session.OutputDirectory} ({first.Components} components, {first.StyleSheets} stylesheets); Ctrl+C to stop");
        await client.ScanAsync();
        await reporter.ReportAsync(first, changed: null);

        using var debouncer = new Debouncer(QuietPeriod, paths => RefreshAsync(paths).GetAwaiter().GetResult(),
            exception => Console.Error.WriteLine($"{Prefix}error NZC000: {exception.Message}"));
        using var watcher = new FileWatcher(session.ProjectDirectory, session.OutputDirectory, path => debouncer.Notify(path ?? session.ProjectDirectory));
        watcher.Start();
        client.StartPeriodicScan(ScanInterval);
        try
        {
            await Task.Delay(Timeout.Infinite, cancellationToken);
        }
        catch (OperationCanceledException)
        {
        }
        Console.WriteLine($"{Prefix}stopped");
        return 0;

        async Task RefreshAsync(IReadOnlyCollection<string> changed)
        {
            var result = session.Refresh();
            await client.ScanAsync();
            await reporter.ReportAsync(result, changed);
        }
    }

    private sealed class Reporter(WatchSession session, DevChannelClient client)
    {
        private HashSet<string> printed = new(StringComparer.Ordinal);
        private bool discoveryWarned;

        public async Task ReportAsync(WatchResult result, IReadOnlyCollection<string>? changed)
        {
            var edited = changed is null ? null : new HashSet<string>(changed.Select(Path.GetFullPath), StringComparer.OrdinalIgnoreCase);
            var shown = result.Diagnostics.Where(d => !printed.Contains(d.ToString()) || (edited is not null && !string.IsNullOrEmpty(d.Path) && edited.Contains(Path.GetFullPath(d.Path)))).ToList();
            printed = new HashSet<string>(result.Diagnostics.Select(d => d.ToString()), StringComparer.Ordinal);
            foreach (var diagnostic in shown) Console.WriteLine(diagnostic);
            foreach (var group in shown.Where(d => d.IsError && !string.IsNullOrEmpty(d.Path)).GroupBy(d => d.Path, StringComparer.OrdinalIgnoreCase))
            {
                var errors = result.Diagnostics.Count(d => d.IsError && string.Equals(d.Path, group.Key, StringComparison.OrdinalIgnoreCase));
                Console.WriteLine($"{Prefix}warning NZW001: {Relative(session.ProjectDirectory, group.Key)}: not applied, {errors} error(s)");
            }
            if (result.DiscoveryFailed)
            {
                if (!discoveryWarned)
                    Console.WriteLine($"{Prefix}warning NZW001: components not regenerated, {result.Diagnostics.Count(d => d.IsError)} error(s): fix them and save again");
                discoveryWarned = true;
            }
            else discoveryWarned = false;

            var hotReload = client.AnyHotReloadSupported;
            foreach (var path in result.GeneratedChanged)
            {
                var name = Relative(session.OutputDirectory, path);
                var codeChange = hotReload && name.EndsWith(".nazor.g.cs", StringComparison.OrdinalIgnoreCase);
                Console.WriteLine(codeChange ? $"{Prefix}{name} regenerated: dotnet watch or Visual Studio will apply the code change" : $"{Prefix}{name} regenerated");
            }

            if (result.StylesChanged.Count == 0 || client.ConnectionCount == 0) return;
            foreach (var delivery in await client.SendStylesAsync(result.StylesChanged))
            {
                var milliseconds = ((int)delivery.Elapsed.TotalMilliseconds).ToString(CultureInfo.InvariantCulture);
                if (delivery.Applied > 0) Console.WriteLine($"{Prefix}styles of {delivery.Source} applied to {delivery.Applied} application(s) ({milliseconds} ms)");
                foreach (var pid in delivery.NotInUse) Console.WriteLine($"{Prefix}styles of {delivery.Source} not in use by application {pid} yet");
                foreach (var error in delivery.Errors) Console.WriteLine($"{Prefix}warning: styles of {delivery.Source}: {error}");
            }
        }

        private static string Relative(string directory, string path) => Path.GetRelativePath(directory, path).Replace('\\', '/');
    }
}
