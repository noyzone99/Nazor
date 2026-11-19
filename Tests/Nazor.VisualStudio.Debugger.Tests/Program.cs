using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text.Json;
using System.Windows.Threading;
using EnvDTE;
using DebugProcess = System.Diagnostics.Process;

namespace Nazor.VisualStudio.Debugger.Tests;

internal static partial class Program
{
    private const string Root = "Nazor.DevTools.BreakpointFixture.FixtureState";
    private static readonly List<object> Evidence = [];

    [STAThread]
    private static int Main(string[] args)
    {
        using var retry = new ComRetry();
        if (args.Length != 4) throw new ArgumentException("Expected fixture executable, output directory, devenv executable and experimental root suffix.");
        if (!System.Text.RegularExpressions.Regex.IsMatch(args[3], "^DevToolsDT01[A-Za-z0-9]*$")) throw new ArgumentException("Use a dedicated DevToolsDT01 experimental profile.");
        var output = Path.GetFullPath(args[1]);
        Directory.CreateDirectory(output);
        var gate = Path.Combine(output, Guid.NewGuid().ToString("N"));
        DTE? dte = null;
        DebugProcess? target = null;
        DebugProcess? ide = null;
        try
        {
            ide = DebugProcess.Start(new ProcessStartInfo(Path.GetFullPath(args[2]), "/RootSuffix " + args[3] + " /Embedding") { UseShellExecute = false, WindowStyle = ProcessWindowStyle.Hidden })!;
            Wait(() => (dte = FindDte(ide.Id)) != null);
            var automation = dte!;
            ProductionIde = automation;
            automation.SuppressUI = true;
            automation.MainWindow.Visible = false;
            target = DebugProcess.Start(new ProcessStartInfo(Path.GetFullPath(args[0]), '"' + gate + '"') { UseShellExecute = false, CreateNoWindow = true })!;
            Wait(() => File.Exists(gate + ".ready"));
            Evidence.Add(new { phase = "environment", ideVersion = FileVersionInfo.GetVersionInfo(args[2]).FileVersion, rootSuffix = args[3], fixture = JsonSerializer.Deserialize<JsonElement>(File.ReadAllText(gate + ".ready")) });
            dynamic reader = automation.GetObject("Nazor.DevTools.BreakpointSpike");
            EnvDTE.Process? debugTarget = null;
            Wait(() =>
            {
                foreach (EnvDTE.Process process in automation.Debugger.LocalProcesses)
                    if (process.ProcessID == target.Id) { debugTarget = process; return true; }
                return false;
            });
            Act(() => debugTarget!.Attach());
            File.WriteAllText(gate, "start");
            Wait(() => automation.Debugger.CurrentMode == dbgDebugMode.dbgBreakMode);
            Console.WriteLine("Attached: " + automation.FullName + " " + automation.Version);
            var selectedFrame = automation.Debugger.CurrentStackFrame;
            var selectedThread = automation.Debugger.CurrentThread;
            Act(() =>
            {
                foreach (EnvDTE.Thread thread in ((EnvDTE80.Process2)debugTarget!).Threads)
                    foreach (EnvDTE.StackFrame frame in thread.StackFrames)
                        if (frame.FunctionName.Contains("Program.Idle", StringComparison.Ordinal))
                        {
                            automation.Debugger.CurrentThread = thread;
                            automation.Debugger.CurrentStackFrame = frame;
                            return;
                        }
                throw new InvalidOperationException("The unrelated fixture frame was not found.");
            });
            Capture(reader, target.Id, "before-render", "11", "7");
            if (Environment.GetEnvironmentVariable("NAZOR_DEVTOOLS_UI_PROBE") == "1")
            {
                CloseInspector();
                Check(!target.HasExited && reader.IsCurrent((long)reader.Generation), "Closing the inspector leaves the native target paused and alive.");
            }
            Evidence.Add(new { phase = "unrelated-selected-frame", selected = automation.Debugger.CurrentStackFrame.FunctionName });
            Act(() => { automation.Debugger.CurrentThread = selectedThread; automation.Debugger.CurrentStackFrame = selectedFrame; });
            long firstStop = reader.Generation;
            var ticks = 0;
            var timer = new DispatcherTimer(TimeSpan.FromMilliseconds(25), DispatcherPriority.Normal, (_, _) => ticks++, Dispatcher.CurrentDispatcher);
            Wait(() => ticks >= 4);
            timer.Stop();
            Check(reader.IsCurrent(firstStop), "External dispatcher stays responsive while the target remains paused.");
            Evidence.Add(new { phase = "external-responsiveness", ticks, targetStillPaused = true });
            try { reader.ReadFields(target.Id, Root, Root + ".Parent.Refresh()"); throw new Exception("A call was accepted."); }
            catch (ArgumentException) { }
            Act(() => automation.Debugger.StepOver(false));
            Wait(() => !reader.IsCurrent(firstStop) && automation.Debugger.CurrentMode == dbgDebugMode.dbgBreakMode);
            Evidence.Add(new { phase = "step", oldStopInvalidated = true, stop = reader.Generation });
            Continue(automation, reader);
            Capture(reader, target.Id, "render-mutation", "31", "7");
            Continue(automation, reader);
            Capture(reader, target.Id, "after-render", "31", "31");
            long lastStop = reader.Generation;
            Act(() => automation.Debugger.Go(false));
            Wait(() => target.HasExited);
            Check(target.ExitCode == 0 && File.ReadAllText(gate + ".result") == "31", "The target completed with no getter or ToString execution.");
            Wait(() => !reader.IsCurrent(lastStop));
            ProbeRetainedInspector(lastStop);
            try { reader.ReadFields(target.Id, Root, Root + ".Parent.Value"); throw new Exception("An exited target was accepted."); }
            catch (InvalidOperationException) { }
            Evidence.Add(new { phase = "exit", target.ExitCode, detachedValuesRetained = Evidence.Count });
            Console.WriteLine("PASS: paused field reads, stale snapshot, getter refusal, render mutation, step, continue, exit and external responsiveness.");
            return 0;
        }
        catch (Exception exception)
        {
            Evidence.Add(new { failure = exception.ToString() });
            Console.Error.WriteLine(exception);
            return 1;
        }
        finally
        {
            CloseInspector();
            File.WriteAllText(Path.Combine(output, "evidence.json"), JsonSerializer.Serialize(Evidence, new JsonSerializerOptions { WriteIndented = true }));
            if (target is { HasExited: false }) target.Kill(true);
            if (dte != null) { try { dte.Quit(); } catch (COMException) { } }
            if (ide is { HasExited: false } && !ide.WaitForExit(10000)) ide.Kill(true);
            ide?.Dispose();
            target?.Dispose();
        }
    }

    private static void Capture(dynamic reader, int processId, string phase, string value, string parameter)
    {
        var paths = new[] { "Phase", "CompletedValue", "CompletedText", "Window", "Session", "Parent", "Child", "Parent.Value", "Child.ParameterField", "Parent.GetterCalls", "Parent.ToStringCalls", "Parent.Dangerous" };
        string json = reader.ReadFields(processId, "Nazor.DevTools.BreakpointFixture.", string.Join("\n", paths.Select(path => Root + "." + path)));
        var fields = JsonSerializer.Deserialize<Dictionary<string, string>>(json)!;
        Evidence.Add(new { phase, stop = reader.Generation, processId, fields });
        string Field(string path) => fields[Root + "." + path];
        Check(Field("Phase").Trim('"') == phase, "The expected stop was reached: " + phase);
        Check(Field("Parent.Value") == value && Field("Child.ParameterField") == parameter, "Current component fields are readable.");
        Check(Field("CompletedValue") == "7" && Field("CompletedText").Trim('"') == "7", "The completed snapshot differs from the current field.");
        foreach (var path in new[] { "Window", "Session", "Parent", "Child" })
            Check(Field(path) != "<unavailable>" && Field(path) != "null", "The root reference is available: " + path);
        Check(Field("Parent.Dangerous") == "<unavailable>", "The getter is unavailable without function evaluation.");
        string counterJson = reader.ReadFields(processId, "Nazor.DevTools.BreakpointFixture.", Root + ".Parent.GetterCalls\n" + Root + ".Parent.ToStringCalls");
        var counters = JsonSerializer.Deserialize<Dictionary<string, string>>(counterJson)!;
        Evidence.Add(new { phase = "side-effects", counters });
        Check(counters.Values.All(counter => counter == "0"), "Neither the getter nor ToString ran.");
        Console.WriteLine("PASS: " + phase);
        CaptureProduction(reader, processId, phase, value);
    }

    private static void Continue(DTE dte, dynamic reader)
    {
        long previous = reader.Generation;
        Act(() => dte.Debugger.Go(false));
        Wait(() => !reader.IsCurrent(previous) && dte.Debugger.CurrentMode == dbgDebugMode.dbgBreakMode);
        Evidence.Add(new { phase = "continue", oldStopInvalidated = true, stop = reader.Generation });
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private static void Act(Action action) => Wait(() => { action(); return true; });

    [DllImport("ole32.dll")]
    private static extern int GetRunningObjectTable(int reserved, out IRunningObjectTable table);

    [DllImport("ole32.dll")]
    private static extern int CreateBindCtx(int reserved, out IBindCtx context);

    private static DTE? FindDte(int processId)
    {
        Marshal.ThrowExceptionForHR(GetRunningObjectTable(0, out var table));
        Marshal.ThrowExceptionForHR(CreateBindCtx(0, out var context));
        table.EnumRunning(out var enumerator);
        var monikers = new IMoniker[1];
        while (enumerator.Next(1, monikers, IntPtr.Zero) == 0)
        {
            monikers[0].GetDisplayName(context, null, out var name);
            if (!name.StartsWith("!VisualStudio.DTE.", StringComparison.Ordinal) || !name.EndsWith(":" + processId, StringComparison.Ordinal)) continue;
            table.GetObject(monikers[0], out var value);
            return (DTE)value;
        }
        return null;
    }

    private static void Wait(Func<bool> condition)
    {
        var watch = Stopwatch.StartNew();
        while (true)
        {
            try { if (condition()) return; }
            catch (COMException exception) when (exception.HResult is unchecked((int)0x80010001) or unchecked((int)0x8001010A)) { }
            if (watch.Elapsed > TimeSpan.FromSeconds(120)) throw new TimeoutException("Visual Studio debugger operation timed out.");
            var frame = new DispatcherFrame();
            var timer = new DispatcherTimer(TimeSpan.FromMilliseconds(50), DispatcherPriority.Background, (_, _) => frame.Continue = false, Dispatcher.CurrentDispatcher);
            Dispatcher.PushFrame(frame);
            timer.Stop();
        }
    }
}
