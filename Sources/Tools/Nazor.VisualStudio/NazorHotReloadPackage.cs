using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using EnvDTE;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.LanguageServices;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell.Settings;
using Microsoft.VisualStudio.Settings;
using Newtonsoft.Json.Linq;
using Task = System.Threading.Tasks.Task;

namespace Nazor.VisualStudio
{
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [ProvideBindingPath]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [ProvideAutomationObject("Nazor.DevTools.BreakpointSpike")]
    [ProvideAutomationObject("Nazor.DevTools")]
    [ProvideAutoLoad(UIContextGuids80.SolutionExists, PackageAutoLoadFlags.BackgroundLoad)]
    [Guid("44C56C55-B3EB-48EF-B102-F37FD96E434D")]
    public sealed class NazorHotReloadPackage : AsyncPackage
    {
        private DTE dte;
        private VisualStudioWorkspace workspace;
        private DebuggerEvents debuggerEvents;
        private CommandEvents applyEvents;
        private SolutionEvents solutionEvents;
        private ErrorListProvider errors;
        private IVsOutputWindowPane output;
        private SettingsStore settings;
        private Microsoft.VisualStudio.Utilities.UnifiedSettings.ISettingsManager unifiedSettings;
        private readonly List<FileSystemWatcher> watchers = new List<FileSystemWatcher>();
        private readonly List<string> manifests = new List<string>();
        private readonly Dictionary<string, string[]> buildInputs = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
        private readonly SemaphoreSlim refreshGate = new SemaphoreSlim(1, 1);
        private CancellationTokenSource session;
        private CancellationTokenSource pendingSave;
        private bool replaying;
        private bool applying;
        private bool pendingCodeChanges;
        private NazorBreakpointReader breakpointReader;

        protected override object GetAutomationObject(string name)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            if (name != "Nazor.DevTools.BreakpointSpike" && name != "Nazor.DevTools") return base.GetAutomationObject(name);
            return breakpointReader ?? (breakpointReader = new NazorBreakpointReader((DTE)GetService(typeof(SDTE)), (IVsDebugger)GetService(typeof(SVsShellDebugger))));
        }

        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            await NazorQuickAdd.RegisterAsync(this);
            var automation = await GetServiceAsync(typeof(SDTE));
            var components = await GetServiceAsync(typeof(SComponentModel)) as IComponentModel;
            var outputService = await GetServiceAsync(typeof(SVsOutputWindow));
            var debugService = await GetServiceAsync(typeof(SVsShellDebugger));
            unifiedSettings = await GetServiceAsync(typeof(UnifiedSettingsService)) as Microsoft.VisualStudio.Utilities.UnifiedSettings.ISettingsManager;
            if (automation == null || components == null || outputService == null) return;
            workspace = components.GetService<VisualStudioWorkspace>();
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
            dte = (DTE)automation;
            breakpointReader = breakpointReader ?? new NazorBreakpointReader(dte, (IVsDebugger)debugService);
            await NazorDevToolsCommand.RegisterAsync(this, dte);
            var window = (IVsOutputWindow)outputService;
            settings = new ShellSettingsManager(this).GetReadOnlySettingsStore(SettingsScope.UserSettings);
            errors = new ErrorListProvider(this) { ProviderName = "Nazor Hot Reload", ProviderGuid = GetType().GUID };
            var pane = GetType().GUID;
            window.CreatePane(ref pane, "Nazor Hot Reload", 1, 1);
            window.GetPane(ref pane, out output);
            debuggerEvents = dte.Events.DebuggerEvents;
            debuggerEvents.OnEnterRunMode += OnRun;
            debuggerEvents.OnEnterBreakMode += OnBreak;
            debuggerEvents.OnEnterDesignMode += OnStop;
            solutionEvents = dte.Events.SolutionEvents;
            solutionEvents.AfterClosing += Stop;
            var command = dte.Commands.Item("Debug.ApplyCodeChanges", 0);
            applyEvents = dte.Events.CommandEvents[command.Guid, command.ID];
            applyEvents.BeforeExecute += BeforeApply;
            if (dte.Debugger.CurrentMode != dbgDebugMode.dbgDesignMode) Start();
        }

        private void OnRun(dbgEventReason reason) { ThreadHelper.ThrowIfNotOnUIThread(); Start(); }
        private void OnBreak(dbgEventReason reason, ref dbgExecutionAction action) { ThreadHelper.ThrowIfNotOnUIThread(); Start(); }
        private void OnStop(dbgEventReason reason) { ThreadHelper.ThrowIfNotOnUIThread(); Stop(); }

        private void Start()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            if (session != null) return;
            session = new CancellationTokenSource();
            try
            {
                var solution = workspace.CurrentSolution;
                var graph = solution.GetProjectDependencyGraph();
                var projects = new HashSet<ProjectId>();
                if (!(dte.Solution.SolutionBuild.StartupProjects is Array startupProjects)) return;
                foreach (var name in startupProjects)
                {
                    var path = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(dte.Solution.FullName), (string)name));
                    foreach (var project in solution.Projects.Where(p => string.Equals(p.FilePath, path, StringComparison.OrdinalIgnoreCase)))
                    {
                        projects.Add(project.Id);
                        projects.UnionWith(graph.GetProjectsThatThisProjectTransitivelyDependsOn(project.Id));
                    }
                }
                foreach (var project in solution.Projects.Where(p => projects.Contains(p.Id)))
                {
                    var hasGeneratedComponents = false;
                    var hasManifest = false;
                    foreach (var document in project.Documents)
                    {
                        hasGeneratedComponents |= document.FilePath?.EndsWith(".nazor.g.cs", StringComparison.OrdinalIgnoreCase) == true;
                        var manifest = HotReloadFiles.FindManifest(document.FilePath, project.FilePath);
                        if (manifest == null) continue;
                        hasManifest = true;
                        if (manifests.Contains(manifest, StringComparer.OrdinalIgnoreCase)) continue;
                        manifests.Add(manifest);
                        var projectDirectory = Path.GetDirectoryName(project.FilePath);
                        buildInputs[projectDirectory] = HotReloadFiles.BuildInputs(projectDirectory);
                        var watcher = new FileSystemWatcher(projectDirectory)
                        {
                            IncludeSubdirectories = true,
                            NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.Size,
                        };
                        watcher.Changed += OnFileChanged;
                        watcher.Created += OnFileChanged;
                        watcher.Deleted += OnFileChanged;
                        watcher.Renamed += OnFileChanged;
                        watcher.Error += (sender, e) => JoinableTaskFactory.RunAsync(async () =>
                        {
                            await JoinableTaskFactory.SwitchToMainThreadAsync();
                            Warn("NZVS001", "The Nazor file watcher lost events. Use Hot Reload to refresh, or restart debugging. " + e.GetException().Message);
                        }).FileAndForget("Nazor/HotReload");
                        watchers.Add(watcher);
                        watcher.EnableRaisingEvents = true;
                        Write("Watching " + watcher.Path);
                    }
                    if (hasGeneratedComponents && !hasManifest)
                        Warn("NZVS001", "Nazor Hot Reload needs the updated Nazor SDK. Update the SDK and rebuild " + project.Name + ".");
                }
            }
            catch (Exception exception) when (!(exception is OutOfMemoryException))
            {
                Warn("NZVS001", "Could not start Nazor Hot Reload: " + exception.Message);
            }
        }

        private void Stop()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            session?.Cancel();
            session?.Dispose();
            session = null;
            pendingSave?.Cancel();
            foreach (var watcher in watchers) watcher.Dispose();
            watchers.Clear();
            manifests.Clear();
            buildInputs.Clear();
            pendingCodeChanges = false;
            errors?.Tasks.Clear();
        }

        private void OnFileChanged(object sender, FileSystemEventArgs e)
        {
            if (!HotReloadFiles.IsInput(e.FullPath)) return;
            var detected = Stopwatch.StartNew();
            JoinableTaskFactory.RunAsync(async () =>
            {
                await JoinableTaskFactory.SwitchToMainThreadAsync();
                if (session == null || !ApplyOnSave()) return;
                pendingSave?.Cancel();
                var refreshToken = session.Token;
                var pending = pendingSave = CancellationTokenSource.CreateLinkedTokenSource(session.Token);
                try
                {
                    await Task.Delay(200, pending.Token);
                    if (pendingSave == pending) pendingSave = null;
                    Write($"Save notification and debounce: {detected.ElapsedMilliseconds} ms.");
                    await RefreshAsync(false, refreshToken);
                }
                catch (OperationCanceledException) { }
                finally
                {
                    if (pendingSave == pending) pendingSave = null;
                    pending.Dispose();
                }
            }).FileAndForget("Nazor/HotReload");
        }

        private bool ApplyOnSave()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            try
            {
                if (unifiedSettings != null)
                {
                    try { return unifiedSettings.GetReader().GetValueOrThrow<bool>("debugging.hotReload.applyOnFileSave"); }
                    catch (Microsoft.VisualStudio.Utilities.UnifiedSettings.SettingNotRegisteredException) { }
                }
                return settings.GetBoolean("Debugger", "EnableNetHotReloadOnFileSave", false);
            }
            catch (Exception exception) when (!(exception is OutOfMemoryException))
            {
                Warn("NZVS004", "Could not read Visual Studio's Apply Hot Reload on File Save setting. Use the Hot Reload button. " + exception.Message);
                return false;
            }
        }

        private void BeforeApply(string guid, int id, object input, object outputArgument, ref bool cancelDefault)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            if (replaying || session == null || manifests.Count == 0) return;
            cancelDefault = true;
            if (applying) return;
            pendingSave?.Cancel();
            var token = session.Token;
            JoinableTaskFactory.RunAsync(() => RefreshAsync(true, token)).FileAndForget("Nazor/HotReload");
        }

        private async Task RefreshAsync(bool saveAll, CancellationToken token)
        {
            var total = Stopwatch.StartNew();
            await refreshGate.WaitAsync(token);
            var queueMs = total.ElapsedMilliseconds;
            try
            {
                await JoinableTaskFactory.SwitchToMainThreadAsync(token);
                applying = true;
                if (saveAll) dte.Documents.SaveAll();
                errors.Tasks.Clear();
                foreach (var project in buildInputs)
                {
                    if (project.Value.SequenceEqual(HotReloadFiles.BuildInputs(project.Key), StringComparer.OrdinalIgnoreCase)) continue;
                    Warn("NZVS002", "Nazor components or standalone stylesheets were added, removed or renamed. Stop debugging and rebuild to update the running project's source files.", project.Key);
                    return;
                }
                var success = true;
                var paths = manifests.ToArray();
                var compiler = Stopwatch.StartNew();
                var replies = await Task.WhenAll(paths.Select(path => RefreshProjectAsync(path, token)));
                await JoinableTaskFactory.SwitchToMainThreadAsync(token);
                Write($"Refresh queue: {queueMs} ms; compiler processes: {compiler.ElapsedMilliseconds} ms.");
                var update = Stopwatch.StartNew();
                var changed = false;
                foreach (var reply in replies)
                {
                    if (reply["timings"] is JObject timings) Write("Compiler phases (ms): " + timings.ToString(Newtonsoft.Json.Formatting.None));
                    foreach (var diagnostic in (JArray)reply["diagnostics"])
                    {
                        var isError = (bool)diagnostic["IsError"];
                        Warn((string)diagnostic["Id"], (string)diagnostic["Message"], (string)diagnostic["Path"],
                            (int)diagnostic["Line"] - 1, (int)diagnostic["Column"] - 1, isError);
                        success &= !isError;
                    }
                    var texts = new Dictionary<string, SourceText>(StringComparer.OrdinalIgnoreCase);
                    foreach (var path in reply["generated"].Values<string>())
                    {
                        var current = workspace.CurrentSolution;
                        var ids = current.GetDocumentIdsWithFilePath(path);
                        if (ids.Length == 0)
                        {
                            Warn("NZVS002", "A new Nazor generated file is not part of the running build. Stop debugging and rebuild to apply it.", path);
                            success = false;
                            continue;
                        }
                        var previous = await current.GetDocument(ids[0]).GetTextAsync(token);
                        var next = SourceText.From(File.ReadAllText(path), previous.Encoding ?? System.Text.Encoding.UTF8, previous.ChecksumAlgorithm);
                        if (!previous.ContentEquals(next)) texts[path] = next;
                    }
                    if (texts.Count == 0) continue;
                    changed = true;
                    var updated = workspace.CurrentSolution;
                    foreach (var text in texts)
                        foreach (var id in updated.GetDocumentIdsWithFilePath(text.Key))
                            updated = updated.WithDocumentText(id, text.Value, PreservationMode.PreserveIdentity);
                    if (!workspace.TryApplyChanges(updated))
                    {
                        Warn("NZVS002", "Visual Studio could not load the generated Nazor code. Stop debugging and rebuild.");
                        success = false;
                    }
                    else pendingCodeChanges = true;
                }
                Write($"Workspace update: {update.ElapsedMilliseconds} ms.");
                if (!success) return;
                if (!changed && !pendingCodeChanges && !saveAll) { Write($"Refresh completed in {total.ElapsedMilliseconds} ms; generated code is unchanged."); return; }
                var command = dte.Commands.Item("Debug.ApplyCodeChanges", 0);
                if (!command.IsAvailable)
                {
                    Warn("NZVS002", "Hot Reload is not currently available. Generated changes remain pending: wait for any current apply to finish, then retry the Hot Reload button. If unavailable for this session, enable .NET Hot Reload and restart debugging with a Debug build.");
                    return;
                }
                replaying = true;
                var apply = Stopwatch.StartNew();
                try { dte.ExecuteCommand("Debug.ApplyCodeChanges"); pendingCodeChanges = false; }
                finally { replaying = false; }
                Write($"Nazor changes passed to Visual Studio Hot Reload in {apply.ElapsedMilliseconds} ms; refresh total: {total.ElapsedMilliseconds} ms. See Hot Reload output for the application result and any independent NZVS003 style-delivery warning.");
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested) { }
            catch (Exception exception) when (!(exception is OutOfMemoryException))
            {
                await JoinableTaskFactory.SwitchToMainThreadAsync();
                Warn("NZVS001", "Nazor Hot Reload could not apply the changes: " + exception.Message);
            }
            finally
            {
                applying = false;
                refreshGate.Release();
            }
        }

        private static async Task<JObject> RefreshProjectAsync(string manifest, CancellationToken token)
        {
            return await Task.Run(async () =>
            {
                var lines = File.ReadAllLines(manifest);
                if (lines.Length != 2) throw new IOException("Invalid nazorc.ide. Rebuild the project.");
                using (var process = new System.Diagnostics.Process())
                {
                    process.StartInfo = new ProcessStartInfo(lines[0], "\"" + lines[1] + "\" ide-refresh @\"" +
                        Path.Combine(Path.GetDirectoryName(manifest), "nazorc.rsp") + "\"")
                    {
                        UseShellExecute = false, CreateNoWindow = true,
                        RedirectStandardOutput = true, RedirectStandardError = true,
                    };
                    process.Start();
                    using (token.Register(() => { try { if (!process.HasExited) process.Kill(); } catch (InvalidOperationException) { } }))
                    {
                        var stdout = process.StandardOutput.ReadToEndAsync();
                        var stderr = process.StandardError.ReadToEndAsync();
                        var json = await stdout.ConfigureAwait(false);
                        var error = await stderr.ConfigureAwait(false);
                        process.WaitForExit();
                        token.ThrowIfCancellationRequested();
                        if (string.IsNullOrWhiteSpace(json)) throw new IOException(error.Length == 0 ? "The Nazor compiler returned no result. Rebuild with the updated Nazor SDK." : error);
                        return JObject.Parse(json);
                    }
                }
            }, token).ConfigureAwait(false);
        }

        private void Write(string message)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            output?.OutputStringThreadSafe(message + Environment.NewLine);
        }

        private void Warn(string code, string message, string path = "", int line = 0, int column = 0, bool isError = false)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            Write(code + ": " + message);
            var task = new ErrorTask
            {
                Category = TaskCategory.BuildCompile, ErrorCategory = isError ? TaskErrorCategory.Error : TaskErrorCategory.Warning,
                Text = code + ": " + message, Document = path, Line = Math.Max(0, line), Column = Math.Max(0, column),
            };
            task.Navigate += (sender, e) => errors.Navigate(task, Guid.Empty);
            errors.Tasks.Add(task);
            errors.Show();
        }

        protected override void Dispose(bool disposing)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            if (disposing)
            {
                breakpointReader?.Dispose();
                Stop();
                if (debuggerEvents != null)
                {
                    debuggerEvents.OnEnterRunMode -= OnRun;
                    debuggerEvents.OnEnterBreakMode -= OnBreak;
                    debuggerEvents.OnEnterDesignMode -= OnStop;
                }
                if (applyEvents != null) applyEvents.BeforeExecute -= BeforeApply;
                if (solutionEvents != null) solutionEvents.AfterClosing -= Stop;
                errors?.Dispose();
            }
            base.Dispose(disposing);
        }

        [Guid("E3684F31-344E-42EA-9047-B620FDC7AC25")]
        private sealed class UnifiedSettingsService { }
    }
}
