using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using EnvDTE;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Newtonsoft.Json.Linq;

namespace Nazor.VisualStudio
{
    internal sealed class NazorDevToolsCommand
    {
        internal static readonly Guid CommandSet = new Guid("66C1A152-62A2-4CF1-A6F5-A8A67A8F0610");
        private readonly AsyncPackage package;
        private readonly DTE dte;
        private readonly Dictionary<string, System.Diagnostics.Process> tools = new Dictionary<string, System.Diagnostics.Process>();
        private NazorDevToolsCommand(AsyncPackage package, DTE dte) { this.package = package; this.dte = dte; }
        internal static async Task RegisterAsync(AsyncPackage package, DTE dte)
        {
            var service = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            var command = new NazorDevToolsCommand(package, dte);
            service?.AddCommand(new MenuCommand((_, __) => command.Open(), new CommandID(CommandSet, 0x100)));
        }
        private void Open()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            try
            {
                foreach (var exited in tools.Where(pair => pair.Value.HasExited).ToArray()) { exited.Value.Dispose(); tools.Remove(exited.Key); }
                var processes = new HashSet<int>();
                foreach (EnvDTE.Process process in dte.Debugger.DebuggedProcesses) processes.Add(process.ProcessID);
                var directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Nazor", "DevTools");
                JObject descriptor = null;
                foreach (var file in Directory.Exists(directory) ? Directory.EnumerateFiles(directory, "*.json").Take(128) : Enumerable.Empty<string>())
                {
                    if (new FileInfo(file).Length > 16384) continue;
                    try
                    {
                        var candidate = JObject.Parse(File.ReadAllText(file));
                        var pid = (int?)candidate["identity"]?["processId"];
                        if (pid == null || !processes.Contains(pid.Value)) continue;
                        using (var process = System.Diagnostics.Process.GetProcessById(pid.Value))
                            if (process.StartTime.ToUniversalTime().Ticks.ToString(System.Globalization.CultureInfo.InvariantCulture) != (string)candidate["processStartToken"]) continue;
                        descriptor = candidate; break;
                    }
                    catch (Exception error) when (error is IOException || error is ArgumentException || error is Newtonsoft.Json.JsonException || error is InvalidOperationException) { }
                }
                if (descriptor == null) throw new InvalidOperationException("Start a Nazor application with UseDevTools() enabled in this debug session first.");
                var session = (string)descriptor["identity"]["sessionId"];
                var path = (string)descriptor["inspectorPath"];
                if (string.IsNullOrEmpty(path) || !Path.IsPathRooted(path) || !File.Exists(path) ||
                    Path.GetFileNameWithoutExtension(path) != "Nazor.DevTools")
                    throw new FileNotFoundException("The inspector payload is unavailable. Build the source inspector or restore the matching Nazor.DevTools.Agent package.");
                if (tools.TryGetValue(session, out var previous) && !previous.HasExited)
                { previous.Refresh(); if (previous.MainWindowHandle != IntPtr.Zero) { ShowWindow(previous.MainWindowHandle, 9); SetForegroundWindow(previous.MainWindowHandle); } return; }
                if (tools.Count >= 8) throw new InvalidOperationException("Close an existing inspector before opening another session (limit: 8).");
                var dll = path.EndsWith(".dll", StringComparison.OrdinalIgnoreCase);
                var start = new ProcessStartInfo(dll ? "dotnet" : path)
                {
                    Arguments = (dll ? Quote(path) + " " : "") + "--connect-session " + Quote(session) + " --visual-studio " + System.Diagnostics.Process.GetCurrentProcess().Id +
                        (descriptor["inspectorVersion"]?.Type == JTokenType.String ? " --expected-version " + Quote((string)descriptor["inspectorVersion"]) : ""),
                    WorkingDirectory = Path.GetDirectoryName(path), UseShellExecute = false, CreateNoWindow = true, WindowStyle = ProcessWindowStyle.Hidden,
                    RedirectStandardError = true,
                };
                var diagnostic = "";
                var errorGate = new object();
                var child = new System.Diagnostics.Process { StartInfo = start, EnableRaisingEvents = true };
                child.ErrorDataReceived += (_, args) => { lock (errorGate) if (args.Data != null && diagnostic.Length < 8192) diagnostic += args.Data.Substring(0, Math.Min(args.Data.Length, 8192 - diagnostic.Length)) + Environment.NewLine; };
                child.Exited += (_, __) => ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
                {
                    try
                    {
                        if (child.ExitCode == 0) return;
                        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                        string message;
                        lock (errorGate) message = "Nazor DevTools could not run. Install the .NET 10 runtime and restore matching Nazor packages. " + diagnostic;
                        VsShellUtilities.ShowMessageBox(package, message, "Nazor DevTools", OLEMSGICON.OLEMSGICON_WARNING, OLEMSGBUTTON.OLEMSGBUTTON_OK, OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
                    }
                    catch (InvalidOperationException) { }
                }).FileAndForget("Nazor/DevTools/Launch");
                child.Start(); child.BeginErrorReadLine(); tools[session] = child;
            }
            catch (Exception error) when (!(error is OutOfMemoryException))
            { VsShellUtilities.ShowMessageBox(package, error.Message, "Nazor DevTools", OLEMSGICON.OLEMSGICON_WARNING, OLEMSGBUTTON.OLEMSGBUTTON_OK, OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST); }
        }
        private static string Quote(string value) => "\"" + value.Replace("\"", "\\\"") + "\"";
        [DllImport("user32.dll")] private static extern bool SetForegroundWindow(IntPtr window);
        [DllImport("user32.dll")] private static extern bool ShowWindow(IntPtr window, int command);
    }
}
