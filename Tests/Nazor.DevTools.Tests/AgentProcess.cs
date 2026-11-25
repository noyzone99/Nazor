using System.Diagnostics;
using System.Reflection;
using System.Text.Json;
using Nazor.DevTools.Protocol;

namespace Nazor.DevTools.Tests;

internal sealed class AgentProcess : IDisposable
{
    internal Process Process { get; }
    internal LocalDevToolsDescriptor Descriptor { get; private set; } = null!;
    internal string Directory { get; }
    private readonly Task<string> errors;
    private AgentProcess(Process process, string directory) { Process = process; Directory = directory; errors = process.StandardError.ReadToEndAsync(); }
    internal static async Task<AgentProcess> StartAsync(bool wide = false)
    {
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (!File.Exists(Path.Combine(root.FullName, "Nazor.slnx"))) root = root.Parent ?? throw new InvalidOperationException("Repository not found.");
        var directory = OperatingSystem.IsWindows() ? Path.Combine(root.FullName, "artifacts", "dt07-processes", Guid.NewGuid().ToString("N"))
            : Path.Combine(Path.GetTempPath(), "ndt-" + Guid.NewGuid().ToString("N")[..12]);
        var configuration = typeof(AgentProcess).Assembly.GetCustomAttribute<AssemblyConfigurationAttribute>()!.Configuration;
        var dll = Environment.GetEnvironmentVariable("NAZOR_DEVTOOLS_FIXTURE") ?? Path.Combine(root.FullName, "Tests", "Fixtures", "DevToolsAgent", "bin", configuration, "net10.0", "DevToolsAgent.dll");
        var start = new ProcessStartInfo("dotnet") { RedirectStandardInput = true, RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false, CreateNoWindow = true };
        start.ArgumentList.Add(dll); start.ArgumentList.Add(directory);
        if (wide) start.ArgumentList.Add("--wide");
        var result = new AgentProcess(Process.Start(start)!, directory);
        try { result.Descriptor = JsonSerializer.Deserialize<LocalDevToolsDescriptor>(await result.LineAsync())!; return result; }
        catch { result.Dispose(); throw; }
    }
    internal async Task<string> LineAsync() => await Process.StandardOutput.ReadLineAsync(TestContext.Current.CancellationToken).AsTask().WaitAsync(TimeSpan.FromSeconds(8), TestContext.Current.CancellationToken)
        ?? throw new InvalidOperationException("Fixture exited: " + await errors);
    internal async Task SendAsync(string text) { await Process.StandardInput.WriteLineAsync(text); await Process.StandardInput.FlushAsync(TestContext.Current.CancellationToken); }
    internal async Task CommandAsync(string command, string expected) { await SendAsync(command); Assert.Equal(expected, await LineAsync()); }
    internal async Task<LocalDevToolsDescriptor> ReplaceAsync() { await SendAsync("replace"); return Descriptor = JsonSerializer.Deserialize<LocalDevToolsDescriptor>(await LineAsync())!; }
    internal static async Task UntilAsync(Func<bool> condition)
    {
        var deadline = DateTime.UtcNow.AddSeconds(6);
        while (!condition()) { if (DateTime.UtcNow > deadline) throw new TimeoutException(); await Task.Delay(10, TestContext.Current.CancellationToken); }
    }
    public void Dispose() { if (!Process.HasExited) Process.Kill(true); Process.WaitForExit(); Process.Dispose(); }
}
