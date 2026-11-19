using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Json;

namespace Nazor.DevTools.Protocol;

public static class InspectorLauncher
{
    public static string RuntimeIdentifier => (OperatingSystem.IsWindows() ? "win-" : OperatingSystem.IsLinux() ? "linux-" : "unsupported-") + RuntimeInformation.ProcessArchitecture.ToString().ToLowerInvariant();
    public static string Version => typeof(InspectorLauncher).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()!.InformationalVersion.Split('+')[0];
    public static void Validate(string inspector, string? expectedVersion = null)
    {
        if (!Path.IsPathFullyQualified(inspector) || !File.Exists(inspector) || Path.GetFileNameWithoutExtension(inspector) != "Nazor.DevTools")
            throw new FileNotFoundException("Inspector assets are missing. Restore Nazor.DevTools.Agent and rebuild with NazorEnableDevTools=true.");
        var directory = Path.GetDirectoryName(inspector)!;
        var manifestPath = Path.Combine(directory, "Nazor.DevTools.bundle.json");
        if (!File.Exists(manifestPath) || new FileInfo(manifestPath).Length > 16384)
            throw new FileNotFoundException("Inspector manifest is missing or invalid. Restore matching Nazor.DevTools.Agent packages and rebuild.");
        try
        {
            using var manifest = JsonDocument.Parse(File.ReadAllText(manifestPath), new() { MaxDepth = 8 });
            var root = manifest.RootElement;
            var rid = root.GetProperty("runtimeIdentifier").GetString();
            if (root.GetProperty("formatVersion").GetInt32() != 1 || root.GetProperty("version").GetString() != Version || (expectedVersion is not null && expectedVersion != Version) ||
                root.GetProperty("framework").GetString() != "net10.0" || (rid != "portable" && rid != RuntimeIdentifier))
                throw new InvalidDataException("Inspector version or platform mismatch. Clean the application output and restore matching Nazor packages for " + RuntimeIdentifier + ".");
        }
        catch (Exception error) when (error is JsonException or KeyNotFoundException or InvalidOperationException or FormatException)
        { throw new InvalidDataException("Inspector manifest is invalid. Restore matching Nazor packages and rebuild.", error); }
        if (!File.Exists(Path.Combine(directory, "Nazor.DevTools.runtimeconfig.json"))) throw new FileNotFoundException("Inspector runtime configuration is missing. Rebuild the application.");
    }
    public static async Task OpenAsync(LocalDevToolsDescriptor descriptor, CancellationToken cancellationToken = default, string? discoveryDirectory = null,
        bool closeWithOwner = false, string? ownerHandle = null)
    {
        if (!LocalDevTools.IsAlive(descriptor)) throw new IOException("The application window is no longer available.");
        var inspector = descriptor.InspectorPath ?? InspectorLocator.Find(AppContext.BaseDirectory) ?? throw new FileNotFoundException("Inspector assets are missing. Rebuild with NazorEnableDevTools=true.");
        Validate(inspector);
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(5));
        var dotnet = Environment.GetEnvironmentVariable("DOTNET_HOST_PATH") ?? "dotnet";
        var check = new ProcessStartInfo(dotnet) { UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true };
        check.ArgumentList.Add("--list-runtimes");
        using (var process = Process.Start(check) ?? throw new IOException("Cannot check the inspector runtime."))
        {
            var output = process.StandardOutput.ReadToEndAsync(timeout.Token);
            var error = process.StandardError.ReadToEndAsync(timeout.Token);
            try { await process.WaitForExitAsync(timeout.Token).ConfigureAwait(false); }
            catch { try { process.Kill(); } catch (InvalidOperationException) { } throw; }
            if (process.ExitCode != 0 || !(await output.ConfigureAwait(false)).Split('\n').Any(line => line.StartsWith("Microsoft.NETCore.App 10.", StringComparison.Ordinal)))
                throw new IOException("Install the .NET 10 " + RuntimeInformation.ProcessArchitecture + " runtime to run Nazor DevTools. " + await error.ConfigureAwait(false));
        }
        var dll = inspector.EndsWith(".dll", StringComparison.OrdinalIgnoreCase);
        var start = new ProcessStartInfo(dll ? dotnet : inspector) { UseShellExecute = false, CreateNoWindow = true, WorkingDirectory = Path.GetDirectoryName(inspector)! };
        if (dll) start.ArgumentList.Add(inspector);
        start.ArgumentList.Add("--connect-session"); start.ArgumentList.Add(descriptor.Identity.SessionId);
        start.ArgumentList.Add("--discovery-directory"); start.ArgumentList.Add(discoveryDirectory ?? LocalDevTools.DiscoveryDirectory);
        if (closeWithOwner) start.ArgumentList.Add("--owned");
        if (!string.IsNullOrEmpty(ownerHandle)) { start.ArgumentList.Add("--owner-handle"); start.ArgumentList.Add(ownerHandle); }
        if (Debugger.IsAttached) start.ArgumentList.Add("--find-debugger");
        using var launched = Process.Start(start) ?? throw new IOException("The inspector could not be started.");
    }
}
