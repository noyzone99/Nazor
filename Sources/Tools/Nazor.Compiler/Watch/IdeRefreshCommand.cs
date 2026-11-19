using System.Text.Json;
using System.Diagnostics;

namespace Nazor.Compiler.Watch;

internal static class IdeRefreshCommand
{
    internal static async Task<int> RunAsync(WatchSession session, TextWriter output, string? discoveryDirectory = null)
    {
        var total = Stopwatch.StartNew();
        var result = session.Refresh();
        var discoveryMs = 0d;
        var deliveryMs = 0d;
        var diagnostics = result.Diagnostics.ToList();
        if (!diagnostics.Any(d => d.IsError))
        {
            using var client = new DevChannelClient(session.ProjectDirectory, discoveryDirectory, TextWriter.Null);
            var phase = Stopwatch.StartNew();
            await client.ScanAsync();
            discoveryMs = phase.Elapsed.TotalMilliseconds;
            if (result.StylesChanged.Count != 0 && client.ConnectionCount == 0)
                diagnostics.Add(new("NZVS003", NazorDiagnosticSeverity.Warning, "", 1, 1,
                    "Style delivery was not confirmed. " + client.DiscoveryIssue + " Generated code is available to Visual Studio; its Hot Reload result is reported separately."));
            if (client.ConnectionCount != 0 && !client.AnyHotReloadSupported)
                diagnostics.Add(new("NZVS002", NazorDiagnosticSeverity.Warning, "", 1, 1,
                    "The running application does not support .NET Hot Reload: only styles can be applied. Enable Hot Reload, use a Debug build and restart debugging."));
            phase.Restart();
            foreach (var delivery in await client.SendStylesAsync(result.StylesChanged))
                foreach (var error in delivery.Errors)
                    diagnostics.Add(new("NZVS003", NazorDiagnosticSeverity.Warning, Path.Combine(session.ProjectDirectory, delivery.Source), 1, 1,
                        $"Styles could not be applied: {error}"));
            deliveryMs = phase.Elapsed.TotalMilliseconds;
        }
        var generated = ProjectFiles.Enumerate(session.ProjectDirectory, session.OutputDirectory, "*.nazor")
            .Where(p => !Path.GetFileName(p).Equals("_Imports.nazor", StringComparison.OrdinalIgnoreCase))
            .Select(p => Path.Combine(session.OutputDirectory, Path.GetRelativePath(session.ProjectDirectory, p) + ".g.cs")).ToList();
        if (ProjectFiles.Enumerate(session.ProjectDirectory, session.OutputDirectory, "*.nss")
            .Any(p => !p.EndsWith(".nazor.nss", StringComparison.OrdinalIgnoreCase)))
            generated.Add(Path.Combine(session.OutputDirectory, "NazorStyleSheets.g.cs"));
        await output.WriteLineAsync(JsonSerializer.Serialize(new
        {
            generated = generated.Where(File.Exists),
            diagnostics = diagnostics.Select(d => new { d.Id, d.IsError, d.Path, d.Line, d.Column, d.Message }),
            timings = new { generationMs = result.Elapsed.TotalMilliseconds, discoveryMs, deliveryMs, totalMs = total.Elapsed.TotalMilliseconds },
        }));
        return diagnostics.Any(d => d.IsError) ? 1 : 0;
    }
}
