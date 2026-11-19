using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Nazor.VisualStudio.Debugger.Tests;

internal static partial class Program
{
    private static EnvDTE.DTE? ProductionIde;
    private static Process? inspector;
    private static void CaptureProduction(dynamic reader, int processId, string phase, string value)
    {
        var descriptorFile = Directory.GetFiles(Path.GetFullPath(Environment.GetCommandLineArgs()[2]), "*.descriptor")
            .FirstOrDefault(f => JsonNode.Parse(File.ReadAllText(f))?["Identity"]?["ProcessId"]?.GetValue<int>() == processId);
        if (descriptorFile is null) throw new InvalidOperationException("The Agent descriptor was not recorded.");
        var descriptor = JsonNode.Parse(File.ReadAllText(descriptorFile))!;
        var session = descriptor["Identity"]!["SessionId"]!.GetValue<string>();
        if (ProductionIde is not null) reader = ProductionIde.GetObject("Nazor.DevTools");
        long stop = reader.Generation;
        var status = JsonNode.Parse((string)reader.GetState(processId))!;
        Check(status["state"]!.GetValue<string>() == "paused", "The shipping bridge sees the actual debugger stop.");
        var tree = JsonNode.Parse((string)reader.ReadTree(processId, session, stop))!;
        var component = tree["components"]!.AsArray().First(c => c!["typeName"]!.GetValue<string>().EndsWith(".ParentComponent", StringComparison.Ordinal))!;
        var id = component["id"]!.GetValue<string>();
        var fields = JsonNode.Parse((string)reader.ReadComponent(processId, session, 1L, id, stop))!;
        Evidence.Add(new { phase = "raw-production-read-" + phase, tree, fields });
        var current = fields["fields"]!.AsArray().First(f => f!["name"]!.GetValue<string>() == "Value")!;
        Check(current["value"]!.GetValue<string>() == value && current["available"]!.GetValue<bool>(), "The shipping bridge reads the selected real component field.");
        Check(fields["capture"]!["stopId"]!.GetValue<string>() == stop.ToString(), "The field capture belongs to this stop.");
        var counters = JsonSerializer.Deserialize<Dictionary<string, string>>((string)reader.ReadFields(processId, "Nazor.DevTools.BreakpointFixture.", Root + ".Parent.GetterCalls\n" + Root + ".Parent.ToStringCalls"))!;
        Check(counters.Values.All(v => v == "0"), "Raw field enumeration calls no getters or ToString.");
        Evidence.Add(new { phase = "production-bridge-" + phase, status, tree, fields, counters });
        Console.WriteLine("PASS: production bridge " + phase);
        if (Environment.GetEnvironmentVariable("NAZOR_DEVTOOLS_UI_PROBE") == "1")
        {
            if (inspector is null)
            {
                var uiFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Nazor", "DevTools", session + ".ui");
                if (Environment.GetEnvironmentVariable("NAZOR_DEVTOOLS_NATIVE_F12") != "1" || !File.Exists(uiFile) || phase != "before-render")
                {
                    object input = "", output = "";
                    Act(() => ProductionIde!.Commands.Raise("{66C1A152-62A2-4CF1-A6F5-A8A67A8F0610}", 0x100, ref input, ref output));
                }
                Wait(() =>
                {
                    if (!File.Exists(uiFile)) return false;
                    try
                    {
                        inspector = Process.GetProcessById(JsonNode.Parse(File.ReadAllText(uiFile))!["processId"]!.GetValue<int>());
                        return !inspector.HasExited && inspector.MainWindowHandle != IntPtr.Zero;
                    }
                    catch (ArgumentException) { return false; }
                });
            }
            var script = Path.GetFullPath("Scripts/Tests/verify-devtools-ide-shell.ps1");
            var start = new ProcessStartInfo("powershell.exe") { UseShellExecute = false, CreateNoWindow = true, WindowStyle = ProcessWindowStyle.Hidden,
                RedirectStandardOutput = true, RedirectStandardError = true };
            foreach (var argument in new[] { "-NoProfile", "-ExecutionPolicy", "Bypass", "-File", script, "-Handle", inspector!.MainWindowHandle.ToInt64().ToString(), "-Value", value, "-Stop", stop.ToString() }) start.ArgumentList.Add(argument);
            using var probe = Process.Start(start)!;
            var stdout = probe.StandardOutput.ReadToEndAsync(); var stderr = probe.StandardError.ReadToEndAsync();
            if (!probe.WaitForExit(65000)) { probe.Kill(true); throw new TimeoutException("Native inspector UI probe timed out."); }
            var result = stdout.GetAwaiter().GetResult() + stderr.GetAwaiter().GetResult();
            Check(probe.ExitCode == 0, result);
            Console.WriteLine(result);
            Evidence.Add(new { phase = "native-inspector-" + phase, inspector.Id, result });
            if (phase == "before-render" && Environment.GetEnvironmentVariable("NAZOR_DEVTOOLS_NATIVE_F12") == "1")
            {
                object input = "", output = "";
                Act(() => ProductionIde!.Commands.Raise("{66C1A152-62A2-4CF1-A6F5-A8A67A8F0610}", 0x100, ref input, ref output));
                Thread.Sleep(1000);
                var uiFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Nazor", "DevTools", session + ".ui");
                Check(JsonNode.Parse(File.ReadAllText(uiFile))!["processId"]!.GetValue<int>() == inspector.Id, "The IDE command reuses the inspector opened through F12.");
                Evidence.Add(new { phase = "native-f12-automatic-debugger-and-ide-reuse", inspector.Id });
            }
        }
    }
    private static void CloseInspector()
    {
        if (inspector is null) return;
        if (!inspector.HasExited && (!inspector.CloseMainWindow() || !inspector.WaitForExit(5000))) inspector.Kill(true);
        inspector.Dispose(); inspector = null;
    }

    private static void ProbeRetainedInspector(long stop)
    {
        if (inspector is null) return;
        var start = new ProcessStartInfo("powershell.exe") { UseShellExecute = false, CreateNoWindow = true, WindowStyle = ProcessWindowStyle.Hidden,
            RedirectStandardOutput = true, RedirectStandardError = true };
        foreach (var argument in new[] { "-NoProfile", "-ExecutionPolicy", "Bypass", "-File", Path.GetFullPath("Scripts/Tests/verify-devtools-ide-shell.ps1"),
            "-Handle", inspector.MainWindowHandle.ToInt64().ToString(), "-Value", "31", "-Stop", stop.ToString(), "-Retained" }) start.ArgumentList.Add(argument);
        using var probe = Process.Start(start)!;
        var stdout = probe.StandardOutput.ReadToEndAsync(); var stderr = probe.StandardError.ReadToEndAsync();
        if (!probe.WaitForExit(65000)) { probe.Kill(true); throw new TimeoutException("Retained IDE inspector probe timed out."); }
        var result = stdout.GetAwaiter().GetResult() + stderr.GetAwaiter().GetResult();
        Check(probe.ExitCode == 0, result);
        Evidence.Add(new { phase = "native-inspector-after-exit", result });
        Console.WriteLine("PASS: retained debugger fields remain browsable after native app exit.");
    }
}
