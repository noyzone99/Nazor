using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Nazor.Compiler;
using Nazor.JsonRpc;
using Nazor.LanguageServer.Features;
using Nazor.LanguageServer.Protocol;
using Nazor.LanguageServer.Workspace;
using Nazor.Nss;

namespace Nazor.LanguageServer;

internal sealed partial class NazorLanguageServer
{
    private readonly JsonRpcConnection connection;
    private readonly TextWriter log;
    private readonly TimeSpan delay;
    private readonly DocumentStore documents = new();
    private readonly ConcurrentDictionary<string, ProjectAnalyzer> projects = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<ProjectAnalyzer, CancellationTokenSource> pending = new();
    private readonly ConcurrentDictionary<ProjectAnalyzer, HashSet<string>> closedDiagnostics = new();
    private readonly CancellationTokenSource stopping = new();
    private readonly CSharpLanguage csharp = new();
    private bool markdown = true;
    private bool visualStudio;
    private bool semanticTokensRefresh;
    private bool codeLensRefresh;
    private bool hierarchicalSymbols;
    private bool createFiles;
    private bool deleteFiles;
    private bool documentChanges;
    private readonly Dictionary<int, JsonObject> completionImages = new();
    private IReadOnlyDictionary<string, string> csharpOptions = new Dictionary<string, string>();
    private bool shutdown;
    private int requests;

    public NazorLanguageServer(Stream input, Stream output, TextWriter log, TimeSpan? analysisDelay = null)
    {
        connection = new JsonRpcConnection(input, output);
        this.log = log;
        delay = analysisDelay ?? TimeSpan.FromMilliseconds(300);
    }

    public async Task<int> RunAsync()
    {
        try
        {
            while (await connection.ReadAsync(stopping.Token) is { } message)
            {
                var method = message["method"]?.GetValue<string>();
                if (method is null) continue;
                if (method == "exit") return shutdown ? 0 : 1;
                var id = message["id"]?.DeepClone();
                var parameters = message["params"] as JsonObject ?? [];
                var watch = Stopwatch.StartNew();
                try
                {
                    var result = await HandleAsync(method, parameters);
                    if (id is not null) await RespondAsync(id, result);
                }
                catch (MethodNotFoundException)
                {
                    if (id is not null) await ErrorAsync(id, -32601, $"Method not found: {method}");
                }
                catch (RequestFailedException failed)
                {
                    if (id is not null) await ErrorAsync(id, -32803, failed.Message);
                }
                catch (Exception exception) when (exception is not OperationCanceledException || !stopping.IsCancellationRequested)
                {
                    await Log($"{method} failed: {exception}");
                    if (id is not null) await ErrorAsync(id, -32603, exception.Message);
                }
                if (id is not null && watch.ElapsedMilliseconds > 200) await Log($"{method}: {watch.ElapsedMilliseconds} ms");
            }
            return shutdown ? 0 : 1;
        }
        finally
        {
            await stopping.CancelAsync();
        }
    }

    private async Task<object?> HandleAsync(string method, JsonObject parameters)
    {
        switch (method)
        {
            case "nazor/csharpOptions":
                if (!visualStudio || parameters["options"] is not JsonObject preferences) return null;
                csharpOptions = preferences.Where(p => Regex.IsMatch(p.Key, @"^[\p{L}_][\p{L}\p{N}_.-]*$", RegexOptions.CultureInvariant) &&
                        p.Value is JsonValue value && value.TryGetValue<string>(out var text) && !text.Contains('\n') && !text.Contains('\r'))
                    .ToDictionary(p => p.Key, p => p.Value!.GetValue<string>(), StringComparer.OrdinalIgnoreCase);
                foreach (var project in projects.Values) { project.SetEditorOptions(csharpOptions); Schedule(project); }
                return null;
            case "nazor/completionImages":
                if (parameters["images"] is JsonObject images)
                    foreach (var (key, value) in images)
                        if (int.TryParse(key, out var color) && value?["guid"]?.GetValue<string>() is string guid && Guid.TryParse(guid, out _) &&
                            value?["id"]?.GetValue<int>() is int imageId)
                            completionImages[color] = VsElements.Image(guid, imageId);
                await Log($"completion images registered by editor: {completionImages.Count}");
                return null;
            case "initialize":
                var capabilities = parameters["capabilities"];
                var formats = capabilities?["textDocument"]?["hover"]?["contentFormat"] as JsonArray;
                markdown = formats is null || formats.Any(f => f?.GetValue<string>() == "markdown");
                visualStudio = capabilities?["_vs_supportsVisualStudioExtensions"]?.GetValue<bool>() == true;
                semanticTokensRefresh = capabilities?["workspace"]?["semanticTokens"]?["refreshSupport"]?.GetValue<bool>() == true;
                codeLensRefresh = capabilities?["workspace"]?["codeLens"]?["refreshSupport"]?.GetValue<bool>() == true;
                hierarchicalSymbols = capabilities?["textDocument"]?["documentSymbol"]?["hierarchicalDocumentSymbolSupport"]?.GetValue<bool>() == true;
                var operations = capabilities?["workspace"]?["workspaceEdit"]?["resourceOperations"] as JsonArray;
                documentChanges = capabilities?["workspace"]?["workspaceEdit"]?["documentChanges"]?.GetValue<bool>() == true;
                createFiles = documentChanges && operations?.Any(o => o?.GetValue<string>() == "create") == true;
                deleteFiles = documentChanges && operations?.Any(o => o?.GetValue<string>() == "delete") == true;
                await Log($"initialize: {parameters["clientInfo"]?.ToJsonString() ?? "unknown client"}, Visual Studio extensions: {visualStudio}");
                return new
                {
                    capabilities = new
                    {
                        positionEncoding = "utf-16",
                        textDocumentSync = new { openClose = true, change = 1, save = new { includeText = false } },
                        completionProvider = new { triggerCharacters = new[] { "<", "@", ":", " ", "(", "\"", ".", "#" }, resolveProvider = true },
                        hoverProvider = true,
                        documentSymbolProvider = true,
                        referencesProvider = true,
                        codeLensProvider = new { resolveProvider = false },
                        signatureHelpProvider = new { triggerCharacters = new[] { "(", ",", "[" }, retriggerCharacters = new[] { ")" } },
                        definitionProvider = true,
                        colorProvider = visualStudio ? (bool?)null : true,
                        renameProvider = new { prepareProvider = true },
                        _vs_onAutoInsertProvider = visualStudio && parameters["initializationOptions"]?["nativeEditing"]?.GetValue<bool>() == true
                            ? null : new { _vs_triggerCharacters = AutoInsert.TriggerCharacters.Select(c => c.ToString()).ToArray() },
                        documentFormattingProvider = true,
                        codeActionProvider = new { codeActionKinds = new[] { "refactor.move", "refactor.extract", "quickfix" } },
                        executeCommandProvider = visualStudio ? new { commands = new[] { "nazor.deleteEmptySidecars" } } : null,
                        documentRangeFormattingProvider = true,
                        documentOnTypeFormattingProvider = visualStudio && parameters["initializationOptions"]?["nativeEditing"]?.GetValue<bool>() == true
                            ? null : new { firstTriggerCharacter = "}", moreTriggerCharacter = new[] { ";" } },
                        semanticTokensProvider = new
                        {
                            legend = new { tokenTypes = SemanticTokens.TokenTypes, tokenModifiers = SemanticTokens.TokenModifiers },
                            full = true,
                            range = true,
                        },
                    },
                    serverInfo = new { name = "Nazor", version = typeof(NazorLanguageServer).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion.Split('+')[0] },
                };
            case "shutdown":
                shutdown = true;
                return null;
            case "textDocument/didOpen":
                var item = parameters["textDocument"]!;
                var opened = documents.Set(item["uri"]!.GetValue<string>(), item["text"]!.GetValue<string>(), item["version"]?.GetValue<int>() ?? 0);
                if (opened.IsComponent) selectorSources.Clear();
                Changed(opened);
                return null;
            case "textDocument/didChange":
                var changed = parameters["textDocument"]!;
                var text = (parameters["contentChanges"] as JsonArray)?.LastOrDefault()?["text"]?.GetValue<string>();
                if (text is not null) Changed(documents.Set(changed["uri"]!.GetValue<string>(), text, changed["version"]?.GetValue<int>() ?? 0));
                return null;
            case "textDocument/didSave":
                if (documents.Get(Uri(parameters)) is { } saved) Changed(saved);
                return null;
            case "textDocument/didClose":
                if (documents.Remove(Uri(parameters)) is { } closed)
                {
                    selectorNames.TryRemove(closed.Path, out _);
                    selectorSources.TryRemove(closed.Path, out _);
                    if (closed.IsComponent) selectorSources.Clear();
                    _ = PublishAsync(closed, []);
                    if (closed.IsComponent) Schedule(Project(closed.Path));
                }
                return null;
            case "textDocument/completion":
                return await CompletionAsync(parameters);
            case "completionItem/resolve":
                var completionItem = parameters.Deserialize<CompletionItem>(Json.Options)!;
                return await csharp.ResolveAsync(completionItem, stopping.Token) is var resolved && !markdown
                    ? resolved with { Documentation = Content(resolved.Documentation?.Value) }
                    : resolved;
            case "textDocument/hover":
                return await HoverAsync(parameters);
            case "textDocument/definition":
                return await DefinitionAsync(parameters);
            case "textDocument/signatureHelp":
                return await SignatureHelpAsync(parameters);
            case "textDocument/documentSymbol":
                return await DocumentSymbolsAsync(parameters);
            case "nazor/nssReferences":
                return SelectorReferences(parameters);
            case "textDocument/codeLens":
                return SelectorCodeLens(parameters);
            case "textDocument/references":
                return SelectorLocations(parameters);
            case "textDocument/prepareRename":
                return await PrepareRenameAsync(parameters);
            case "textDocument/rename":
                return await RenameAsync(parameters);
            case "textDocument/_vs_onAutoInsert":
                return OnAutoInsert(parameters);
            case "nazor/snippet":
                return await SnippetAsync(parameters);
            case "nazor/surroundWith":
                return SurroundWith(parameters);
            case "nazor/componentParameters":
                return ParameterClassifications(parameters);
            case "nazor/generatedChanged":
                await GeneratedChangedAsync(parameters);
                return null;
            case "nazor/csharpDocuments":
                if (parameters["projectDirectory"]?.GetValue<string>() is not { } projectDirectory) return null;
                var prefix = Path.GetFullPath(projectDirectory).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
                foreach (var external in documents.All.Where(d => d.Version == -1 && d.Path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))) documents.Remove(external.Uri);
                foreach (var entry in parameters["documents"]?.AsArray() ?? [])
                    if (entry?["path"]?.GetValue<string>() is { } source && source.EndsWith(".cs", StringComparison.OrdinalIgnoreCase) &&
                        Path.GetFullPath(source).StartsWith(prefix, StringComparison.OrdinalIgnoreCase) &&
                        entry["text"]?.GetValue<string>() is { } sourceText)
                        documents.Set(UriOf(source), sourceText, -1);
                foreach (var project in projects.Values.Where(p => string.Equals(p.Context.Directory, Path.GetFullPath(projectDirectory), StringComparison.OrdinalIgnoreCase)))
                {
                    project.Invalidate();
                    Schedule(project);
                }
                return null;
            case "nazor/originalPosition":
                return OriginalPosition(parameters);
            case "nazor/referencePosition":
                return ReferencePosition(parameters);
            case "textDocument/documentColor":
                return DocumentColors(parameters);
            case "textDocument/colorPresentation":
                return ColorPresentations(parameters);
            case "textDocument/formatting":
                return Formatting(parameters, null);
            case "textDocument/codeAction":
                return await CodeActionsAsync(parameters);
            case "textDocument/rangeFormatting":
                return Formatting(parameters, parameters["range"]);
            case "textDocument/onTypeFormatting":
                return OnTypeFormatting(parameters);
            case "textDocument/semanticTokens/full":
                return new { data = await SemanticTokensAsync(parameters, null) };
            case "textDocument/semanticTokens/range":
                return new { data = await SemanticTokensAsync(parameters, parameters["range"]) };
            case "workspace/didChangeWatchedFiles":
                selectorSources.Clear();
                foreach (var project in projects.Values) { project.Invalidate(); Schedule(project, refreshTokens: true); }
                return null;
            case "initialized" or "$/cancelRequest" or "$/setTrace" or "workspace/didChangeConfiguration":
                return null;
            default:
                if (method.StartsWith("$/", StringComparison.Ordinal)) return null;
                throw new MethodNotFoundException();
        }
    }

    private void Changed(TextDocument document)
    {
        if (document.Path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase) || Path.GetFileName(document.Path) == ".editorconfig")
        {
            var project = Project(document.Path);
            project.Invalidate();
            Schedule(project);
            return;
        }
        if (document.IsStyleSheet) _ = PublishAsync(document, StyleSheetDiagnostics(document));
        var component = document.Path.EndsWith(".nazor.nss", StringComparison.OrdinalIgnoreCase) ? document.Path[..^4] : document.Path;
        if (document.IsComponent || component != document.Path) Schedule(Project(component));
    }

    private ProjectAnalyzer Project(string path)
    {
        foreach (var project in projects.Values)
            if (project.Contains(path)) return project;
        var context = ProjectContext.ForDocument(path);
        return projects.GetOrAdd(context.Directory, _ => new ProjectAnalyzer(context, documents, csharpOptions));
    }

    private void Schedule(ProjectAnalyzer project, bool refreshTokens = false)
    {
        var cancellation = CancellationTokenSource.CreateLinkedTokenSource(stopping.Token);
        lock (pending)
        {
            if (pending.TryGetValue(project, out var previous)) previous.Cancel();
            pending[project] = cancellation;
        }
        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(delay, cancellation.Token);
                var watch = Stopwatch.StartNew();
                var snapshot = project.Current();
                if (cancellation.IsCancellationRequested) return;
                foreach (var diagnostic in snapshot.Analysis.Diagnostics.Where(d => string.IsNullOrEmpty(d.Path)))
                    await Log($"analysis of {project.Context.Directory}: {diagnostic}");
                foreach (var document in documents.All.Where(d => d.IsComponent && project.Contains(d.Path)))
                {
                    var diagnostics = snapshot.Analysis.Diagnostics.Where(d => string.IsNullOrEmpty(d.Path) || string.Equals(d.Path, document.Path, StringComparison.OrdinalIgnoreCase))
                        .Select(d => Convert(document.Text, d.Id, d.IsError, d.Line, d.Column, d.Length, d.Message)).ToList();
                    if (snapshot.Component(document.Path) is { } component && component.Text == document.Text)
                        foreach (var diagnostic in await CSharpLanguage.DiagnosticsAsync(snapshot, component, cancellation.Token))
                            if (CSharpDiagnostic(document.Text, diagnostic) is { } converted) diagnostics.Add(converted);
                    await PublishAsync(document, diagnostics);
                }
                await PublishClosedAsync(project, snapshot, cancellation.Token);
                foreach (var document in documents.All.Where(d => d.IsStyleSheet && project.Contains(d.Path)))
                    await PublishAsync(document, StyleSheetDiagnostics(document));
                if (visualStudio) await NotifyAsync("nazor/componentParametersChanged", new JsonObject());
                if (codeLensRefresh) await RequestAsync("workspace/codeLens/refresh", null);
                if (refreshTokens && semanticTokensRefresh) await RequestAsync("workspace/semanticTokens/refresh", null);
                await Log($"analysis of {project.Context.Directory}: {snapshot.Analysis.Components.Count} components, {watch.ElapsedMilliseconds} ms");
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception exception)
            {
                await Log($"analysis of {project.Context.Directory} failed: {exception}");
            }
            finally
            {
                lock (pending)
                {
                    if (pending.TryGetValue(project, out var current) && ReferenceEquals(current, cancellation))
                        pending.Remove(project);
                    cancellation.Dispose();
                }
            }
        });
    }

    private async Task PublishClosedAsync(ProjectAnalyzer project, ProjectSnapshot snapshot, CancellationToken cancellation)
    {
        var open = documents.All.Select(d => d.Path).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var byPath = snapshot.Analysis.Diagnostics.Where(d => !open.Contains(d.Path) && project.Contains(d.Path))
            .GroupBy(d => d.Path, StringComparer.OrdinalIgnoreCase).ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);
        var published = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var path in byPath.Keys.Union(snapshot.Components.Keys.Where(p => !open.Contains(p)), StringComparer.OrdinalIgnoreCase).ToList())
        {
            cancellation.ThrowIfCancellationRequested();
            var component = snapshot.Component(path);
            var text = component?.Text ?? (File.Exists(path) ? await File.ReadAllTextAsync(path, cancellation) : null);
            if (text is null) continue;
            var diagnostics = (byPath.GetValueOrDefault(path) ?? [])
                .Select(d => Convert(text, d.Id, d.IsError, d.Line, d.Column, d.Length, d.Message)).ToList();
            if (component is not null)
                foreach (var diagnostic in await CSharpLanguage.DiagnosticsAsync(snapshot, component, cancellation))
                    if (CSharpDiagnostic(text, diagnostic) is { } converted) diagnostics.Add(converted);
            if (diagnostics.Count == 0) continue;
            published.Add(path);
            await NotifyAsync("textDocument/publishDiagnostics", new PublishDiagnosticsParams(UriOf(path), null, diagnostics));
        }
        foreach (var path in closedDiagnostics.GetValueOrDefault(project) ?? [])
            if (!published.Contains(path) && !open.Contains(path))
                await NotifyAsync("textDocument/publishDiagnostics", new PublishDiagnosticsParams(UriOf(path), null, []));
        closedDiagnostics[project] = published;
    }

    private Diagnostic[] StyleSheetDiagnostics(TextDocument document)
    {
        NssSelectorNames? names = null;
        return NssCompiler.Compile(Path.GetFileName(document.Path), [new NssSource(document.Path, document.Text)],
                id => (names ??= SelectorNames(document)).IdTypes.GetValueOrDefault(id)).Diagnostics
            .Select(d => Convert(document.Text, d.Id, d.IsError, d.Line, d.Column, d.Length, d.Message)).ToArray();
    }

    private Diagnostic? CSharpDiagnostic(string text, MappedCSharpDiagnostic diagnostic)
    {
        if (!visualStudio && diagnostic.Hidden && (string.IsNullOrEmpty(diagnostic.Message) || diagnostic.Tags?.Contains(1) != true)) return null;
        var tags = diagnostic.Tags;
        if (visualStudio)
            tags = [.. tags ?? [], .. diagnostic.Hidden
                ? new[] { VsDiagnosticTags.HiddenInEditor, VsDiagnosticTags.HiddenInErrorList, VsDiagnosticTags.SuppressEditorToolTip }
                : new[] { VsDiagnosticTags.VisibleInErrorList }, VsDiagnosticTags.IntellisenseError];
        return new(TextPositions.Range(text, diagnostic.Start, diagnostic.Start + diagnostic.Length), diagnostic.Severity, diagnostic.Id, diagnostic.Message)
        {
            Tags = tags,
            CodeDescription = System.Uri.TryCreate(diagnostic.HelpLink, UriKind.Absolute, out _) ? new(diagnostic.HelpLink!) : null,
        };
    }

    private static Diagnostic Convert(string text, string id, bool error, int line, int column, int length, string message)
    {
        var start = TextPositions.Offset(text, new Position(Math.Max(line - 1, 0), Math.Max(column - 1, 0)));
        var end = start + length;
        if (length <= 0)
        {
            (_, end) = NssLanguage.Word(text, start);
            if (end == start) end = Math.Min(start + 1, text.Length);
        }
        var lineEnd = start;
        while (lineEnd < text.Length && text[lineEnd] is not ('\r' or '\n')) lineEnd++;
        return new(TextPositions.Range(text, start, Math.Min(end, Math.Max(lineEnd, start))), error ? DiagnosticSeverity.Error : DiagnosticSeverity.Warning, id, message);
    }

    private async Task<CompletionList> CompletionAsync(JsonObject parameters)
    {
        var document = documents.Get(Uri(parameters));
        if (document is null) return new(false, []);
        var offset = TextPositions.Offset(document.Text, PositionOf(parameters));
        if (SelectorCompletion(document, offset) is { } selectors) return selectors;
        List<CompletionItem> items;
        if (document.IsComponent)
        {
            var context = NazorLanguage.Classify(document.Text, offset);
            var snapshot = Project(document.Path).Current();
            var component = snapshot.Component(document.Path);
            items = [];
            if (context.Kind is NazorContextKind.Content or NazorContextKind.AttributeValue or NazorContextKind.CSharp or NazorContextKind.Directive)
            {
                if (component is not null)
                {
                    var trigger = parameters["context"]?["triggerCharacter"]?.GetValue<string>() is { Length: 1 } character ? character[0] : (char?)null;
                    items = await csharp.CompleteAsync(snapshot, component, offset, trigger, document.Text, stopping.Token) ?? [];
                }
            }
            if (items.Count == 0 || context.Kind == NazorContextKind.Directive)
                items = [.. NazorLanguage.Complete(document.Text, offset, Catalog(document.Path), visualStudio,
                    ReadText(document.Path + ".nss"), () => SelectorNames(document)), .. items];
            if (component is not null && component.Text == document.Text)
            {
                var values = await ParameterValues.CompleteAsync(snapshot, component, context, offset, stopping.Token);
                if (values.Count > 0) items = [.. values.Concat(items).DistinctBy(i => i.Label)];
            }
        }
        else
        {
            items = NssLanguage.Complete(document.Text, offset, StyleSheetComponents(document.Path), document.Text, o => o, visualStudio, () => SelectorNames(document));
        }
        if (visualStudio && completionImages.Count > 0)
            items = [.. items.Select(i => i.VsIcon?["ImageId"]?["Guid"]?.GetValue<string>() == NssCompletionImages.Catalog &&
                i.VsIcon?["ImageId"]?["Id"]?.GetValue<int>() is int id && completionImages.TryGetValue(id, out var icon)
                ? i with { VsIcon = icon.DeepClone() } : i)];
        return new(false, markdown ? items : [.. items.Select(i => i with { Documentation = Content(i.Documentation?.Value) })]);
    }

    private string? ReadText(string path) => documents.TextOf(path) ?? (File.Exists(path) ? File.ReadAllText(path) : null);

    private async Task<Hover?> HoverAsync(JsonObject parameters)
    {
        var document = documents.Get(Uri(parameters));
        if (document is null) return null;
        var offset = TextPositions.Offset(document.Text, PositionOf(parameters));
        if (document.IsComponent)
        {
            var context = NazorLanguage.Classify(document.Text, offset);
            var (at, endTag) = EndTagName(document.Text, context, offset) is { } end ? (end.Start.NameStart + offset - end.End.NameStart, end.End) : (offset, null);
            if (context.Kind != NazorContextKind.Style && Project(document.Path).Current() is var snapshot && snapshot.Component(document.Path) is { } component &&
                await csharp.HoverAsync(snapshot, component, at, stopping.Token) is { } quickInfo)
            {
                var range = endTag is not null
                    ? TextPositions.Range(document.Text, endTag.NameStart, endTag.NameStart + endTag.Name.Length)
                    : TextPositions.Range(document.Text, quickInfo.Start, quickInfo.Start + quickInfo.Length);
                return new Hover(markdown ? new MarkupContent("```csharp\n" + quickInfo.Plain + "\n```") : new MarkupContent(quickInfo.Plain, "plaintext"), range)
                {
                    VsRawContent = visualStudio ? quickInfo.Raw : null,
                };
            }
        }
        var hover = document.IsComponent
            ? NazorLanguage.Hover(document.Text, offset, Catalog(document.Path))
            : NssLanguage.Hover(document.Text, offset);
        if (hover is not { } found) return null;
        return new(Content(found.Markdown)!, TextPositions.Range(document.Text, found.Start, found.End))
        {
            VsRawContent = visualStudio && document.IsComponent ? NazorLanguage.RichHover(document.Text, offset, Catalog(document.Path)) : null,
        };
    }

    private static (MarkupTag Start, MarkupTag End)? EndTagName(string text, NazorContext context, int offset)
    {
        if (context.Kind != NazorContextKind.TagName || context.TagStart + 1 >= text.Length || text[context.TagStart + 1] != '/') return null;
        var tags = NazorLanguage.Tags(text);
        var end = tags.FirstOrDefault(t => t.Start == context.TagStart);
        return end is not null && NazorLanguage.StartOf(tags, end) is { } start ? (start, end) : null;
    }

    private (TextDocument Document, ProjectSnapshot Snapshot, ComponentDocument Component, int Offset)? CSharpAt(JsonObject parameters)
    {
        var document = documents.Get(Uri(parameters));
        if (document is not { IsComponent: true }) return null;
        var offset = TextPositions.Offset(document.Text, PositionOf(parameters));
        var context = NazorLanguage.Classify(document.Text, offset);
        if (context.Kind is NazorContextKind.Style or NazorContextKind.Comment) return null;
        if (EndTagName(document.Text, context, offset) is { } tag) offset = tag.Start.NameStart + offset - tag.End.NameStart;
        var snapshot = Project(document.Path).Current();
        return snapshot.Component(document.Path) is { } component ? (document, snapshot, component, offset) : null;
    }

    private async Task<object[]> DefinitionAsync(JsonObject parameters)
    {
        if (CSharpAt(parameters) is not { } at) return [];
        var places = await CSharpLanguage.DefinitionAsync(at.Snapshot, at.Component, at.Offset, stopping.Token);
        return [.. places.Select(place => new { uri = UriOf(place.Path), range = place.Range })];
    }

    private async Task<SignatureHelp?> SignatureHelpAsync(JsonObject parameters) =>
        CSharpAt(parameters) is { } at ? await CSharpSignatures.HelpAsync(at.Snapshot, at.Component, at.Offset, stopping.Token) : null;

    private async Task<object[]> DocumentSymbolsAsync(JsonObject parameters)
    {
        if (documents.Get(Uri(parameters)) is not { } document) return [];
        List<DocumentSymbol> symbols;
        if (document.IsComponent)
        {
            var snapshot = Project(document.Path).Current();
            symbols = snapshot.Component(document.Path) is { } component ? await DocumentSymbols.ComponentAsync(snapshot, component, stopping.Token) : [];
        }
        else symbols = DocumentSymbols.Rules(document.Text, 0, document.Text.Length);
        return hierarchicalSymbols ? [.. symbols] : [.. DocumentSymbols.Flatten(symbols, document.Uri)];
    }

    private async Task<object?> PrepareRenameAsync(JsonObject parameters)
    {
        if (CSharpAt(parameters) is not { } at || await CSharpLanguage.PrepareRenameAsync(at.Snapshot, at.Component, at.Offset, stopping.Token) is not { } name) return null;
        var start = name.Start;
        var offset = TextPositions.Offset(at.Document.Text, PositionOf(parameters));
        if (EndTagName(at.Document.Text, NazorLanguage.Classify(at.Document.Text, offset), offset) is { } tag) start += tag.End.NameStart - tag.Start.NameStart;
        return new { range = TextPositions.Range(at.Document.Text, start, start + name.Length), placeholder = name.Name };
    }

    private async Task<object?> RenameAsync(JsonObject parameters)
    {
        var newName = parameters["newName"]?.GetValue<string>();
        if (string.IsNullOrWhiteSpace(newName) || CSharpAt(parameters) is not { } at) return null;
        if (await CSharpLanguage.RenameAsync(at.Snapshot, at.Component, at.Offset, newName, stopping.Token) is not { } rename) return null;
        if (rename.Files.Count == 0)
        {
            var changes = new JsonObject();
            foreach (var (path, list) in rename.Edits) changes[UriOf(path)] = JsonSerializer.SerializeToNode(list, Json.Options);
            return new { changes };
        }
        var operations = new List<object>();
        foreach (var (path, list) in rename.Edits) operations.Add(new { textDocument = new { uri = UriOf(path), version = (int?)null }, edits = list });
        foreach (var (from, to) in rename.Files) operations.Add(new { kind = "rename", oldUri = UriOf(from), newUri = UriOf(to) });
        return new { documentChanges = operations };
    }

    private async Task<object[]> CodeActionsAsync(JsonObject parameters)
    {
        if (documents.Get(Uri(parameters)) is not { IsComponent: true } document || parameters["range"] is not JsonObject range) return [];
        var start = TextPositions.Offset(document.Text, new Position(range["start"]!["line"]!.GetValue<int>(), range["start"]!["character"]!.GetValue<int>()));
        var end = TextPositions.Offset(document.Text, new Position(range["end"]!["line"]!.GetValue<int>(), range["end"]!["character"]!.GetValue<int>()));
        var snapshot = Project(document.Path).Current();
        var component = snapshot.Component(document.Path);
        string? Read(string path) => documents.All.FirstOrDefault(d => string.Equals(d.Path, path, StringComparison.OrdinalIgnoreCase))?.Text ??
            (File.Exists(path) ? File.ReadAllText(path) : null);
        var actions = Features.CodeActions.For(document.Path, document.Text, start, end, component?.Generated.CSharp, Read, await snapshot.CSharp.GetCompilationAsync(stopping.Token))
            .Select(action => (Kind: "refactor.move", Action: action)).ToList();
        actions.AddRange(MarkupActions.For(document.Path, document.Text, start, end, snapshot.Analysis.Catalog, Read, UnresolvedTags(document, snapshot).Contains)
            .Select(action => ("quickfix", action)));
        if (component is not null && component.Text == document.Text &&
            await ExtractComponent.ActionAsync(snapshot, component, document.Text, start, end, Read, stopping.Token) is { } extract)
            actions.Insert(0, ("refactor.extract", extract));
        if (component is not null && component.Text == document.Text)
        {
            actions.AddRange((await MissingImports.ActionsAsync(snapshot, component, start, end, stopping.Token)).Select(a => ("quickfix", a)));
            actions.AddRange((await ProjectTools.FixesAsync(snapshot, component, start, end,
                exception => _ = Log("Code fix failed: " + exception.Message), stopping.Token)).Select(a => ("quickfix", a)));
        }
        var only = parameters["context"]?["only"] as JsonArray;
        return [.. actions.Where(a => (createFiles || a.Action.Changes.All(c => c.OldText is not null)) &&
            (only is null || only.Any(k => a.Kind == k!.GetValue<string>() || a.Kind.StartsWith(k.GetValue<string>() + ".", StringComparison.Ordinal)))).Select(a => new
        {
            title = a.Action.Title,
            kind = a.Kind,
            edit = WorkspaceEdit(a.Action.Changes),
            command = visualStudio && a.Action.Changes.Any(c => c.NewText is null) ? new
            {
                title = "Delete empty sidecars",
                command = "nazor.deleteEmptySidecars",
                arguments = new object[] { new { component = document.Path, changes = a.Action.Changes.Select(c => new { path = c.Path, text = c.NewText ?? "", delete = c.NewText is null }).ToArray() } },
            } : null,
        })];
    }

    private object WorkspaceEdit(IReadOnlyList<FileChange> changes)
    {
        var ordered = changes.OrderBy(c => c.NewText is null || c.NewText.Length < (c.OldText?.Length ?? 0)).ToArray();
        if (documentChanges) return new { documentChanges = ordered.SelectMany(Operations).ToArray() };
        return new
        {
            changes = ordered.ToDictionary(c => UriOf(c.Path), c => new[]
            {
                new TextEdit(TextPositions.Range(c.OldText ?? "", 0, c.OldText?.Length ?? 0), c.NewText ?? ""),
            }),
        };
    }

    private IEnumerable<object> Operations(FileChange change)
    {
        var uri = UriOf(change.Path);
        if (change.NewText is null)
        {
            yield return new
            {
                textDocument = new { uri, version = DocumentVersion(uri) },
                edits = new[] { new TextEdit(TextPositions.Range(change.OldText ?? "", 0, change.OldText?.Length ?? 0), "") },
            };
            if (deleteFiles && !visualStudio) yield return new { kind = "delete", uri };
            yield break;
        }
        if (change.OldText is null) yield return new { kind = "create", uri, options = new { overwrite = false, ignoreIfExists = visualStudio } };
        var old = change.OldText ?? "";
        yield return new
        {
            textDocument = new { uri, version = DocumentVersion(uri) },
            edits = new[] { new TextEdit(TextPositions.Range(old, 0, old.Length), change.NewText) },
        };
    }

    private int? DocumentVersion(string uri) => documents.Get(uri) is { Version: >= 0 } document ? document.Version : null;

    private JsonObject? OnAutoInsert(JsonObject parameters)
    {
        var document = parameters["_vs_textDocument"]?["uri"]?.GetValue<string>() is { } uri ? documents.Get(uri) : null;
        var position = parameters["_vs_position"];
        var typed = parameters["_vs_ch"]?.GetValue<string>();
        if (document is null || position is null || string.IsNullOrEmpty(typed)) return null;
        var offset = TextPositions.Offset(document.Text, new Position(position["line"]!.GetValue<int>(), position["character"]!.GetValue<int>()));
        var options = parameters["_vs_options"];
        if (AutoInsert.OnType(document.Text, offset, typed[0], document.IsComponent, options?["tabSize"]?.GetValue<int>() ?? 4, options?["insertSpaces"]?.GetValue<bool>() ?? true) is not { } edit)
            return null;
        return new JsonObject
        {
            ["_vs_textEditFormat"] = 2,
            ["_vs_textEdit"] = JsonSerializer.SerializeToNode(new TextEdit(TextPositions.Range(document.Text, edit.Start, edit.End), edit.NewText), Json.Options),
        };
    }

    private async Task<object?> SnippetAsync(JsonObject parameters)
    {
        if (parameters["text"]?.GetValue<string>() is { } text && documents.Get(Uri(parameters)) is { } current && current.Text != text)
            documents.Set(current.Uri, text, current.Version);
        if (documents.Get(Uri(parameters)) is { IsComponent: true } directiveDocument &&
            EditorSnippets.Directive(directiveDocument.Text, TextPositions.Offset(directiveDocument.Text, PositionOf(parameters))) is { } directive)
            return new { range = TextPositions.Range(directiveDocument.Text, directive.Start, directive.End), snippet = directive.NewText };
        if (documents.Get(Uri(parameters)) is not { IsComponent: true } document ||
            NazorLanguage.Classify(document.Text, TextPositions.Offset(document.Text, PositionOf(parameters))).Kind != NazorContextKind.CSharp) return null;
        if (CSharpAt(parameters) is not { } at || await CSharpLanguage.SnippetAsync(at.Snapshot, at.Component, at.Offset, stopping.Token) is not { } snippet) return null;
        return new { range = TextPositions.Range(at.Document.Text, snippet.Start, snippet.Start + snippet.Length), snippet = snippet.Snippet };
    }

    private object[] SurroundWith(JsonObject parameters)
    {
        if (documents.Get(Uri(parameters)) is not { IsComponent: true } document) return [];
        var text = parameters["text"]?.GetValue<string>() ?? document.Text;
        var start = parameters["start"]?.GetValue<int>() ?? -1;
        var end = parameters["end"]?.GetValue<int>() ?? -1;
        if (start < 0 || end > text.Length || end <= start) return [];
        var context = NazorLanguage.Classify(text, start);
        var csharp = context.Kind == NazorContextKind.CSharp;
        if (context.Kind is not (NazorContextKind.CSharp or NazorContextKind.Content) || csharp && end > context.End) return [];
        return [.. EditorSnippets.Surround(text, start, end, csharp).Select(s => new
        {
            title = s.Title, range = TextPositions.Range(text, s.Edit.Start, s.Edit.End), snippet = s.Edit.NewText,
        })];
    }

    private async Task GeneratedChangedAsync(JsonObject parameters)
    {
        if (parameters["path"]?.GetValue<string>() is not { } generated || parameters["oldText"]?.GetValue<string>() is not { } built ||
            parameters["changes"] is not JsonArray changes || !generated.EndsWith(".nazor.g.cs", StringComparison.OrdinalIgnoreCase)) return;
        if (ComponentOf(generated) is not { } path || Project(path).Current().Component(path) is not { } component) return;
        var list = changes.Select(c => (c!["start"]!.GetValue<int>(), c["length"]!.GetValue<int>(), c["text"]!.GetValue<string>())).ToList();
        var edits = component.Generated.CSharp == built
            ? CSharpLanguage.GeneratedEdits(component, built, list)
            : await CSharpLanguage.StaleRenameEditsAsync(Project(path).Current(), component, built, list, stopping.Token);
        if (edits.Count == 0) return;
        await Log($"rename from C#: {edits.Count} edits in {path}");
        await RequestAsync("workspace/applyEdit", new JsonObject
        {
            ["label"] = "Rename",
            ["edit"] = new JsonObject { ["changes"] = new JsonObject { [UriOf(path)] = JsonSerializer.SerializeToNode(edits, Json.Options) } },
        });
    }

    private static string? ComponentOf(string generated)
    {
        if (!generated.EndsWith(".nazor.g.cs", StringComparison.OrdinalIgnoreCase)) return null;
        generated = Path.GetFullPath(generated);
        var output = Path.GetDirectoryName(generated);
        while (output is not null && !File.Exists(Path.Combine(output, "nazorc.rsp"))) output = Path.GetDirectoryName(output);
        return output is null ? null : Path.Combine(ProjectContext.ForDocument(output).Directory, Path.GetRelativePath(output, generated)[..^".g.cs".Length]);
    }

    private object? OriginalPosition(JsonObject parameters)
    {
        if (parameters["path"]?.GetValue<string>() is not { } generated || parameters["text"]?.GetValue<string>() is not { } text ||
            ComponentOf(generated) is not { } path || Project(path).Current().Component(path) is not { } component || component.Generated.CSharp != text) return null;
        var offset = TextPositions.Offset(text, PositionOf(parameters));
        if (component.ToOriginal(offset, 0) is not { } span) return null;
        return new { uri = UriOf(component.Path), range = TextPositions.Range(component.Text, span.Start, span.Start) };
    }

    private object? ReferencePosition(JsonObject parameters)
    {
        if (parameters["path"]?.GetValue<string>() is not { } generated || parameters["text"]?.GetValue<string>() is not { } built ||
            ComponentOf(generated) is not { } path || Project(path).Current().Component(path) is not { } component) return null;
        var offset = TextPositions.Offset(built, PositionOf(parameters));
        var length = parameters["length"]?.GetValue<int>() ?? 0;
        if (length < 0 || length > built.Length - offset) return null;
        if (component.Generated.CSharp != built)
        {
            var before = Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree.ParseText(built);
            var after = Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree.ParseText(component.Generated.CSharp);
            var shift = 0;
            foreach (var change in after.GetChanges(before).OrderBy(c => c.Span.Start))
            {
                if (change.Span.End <= offset) shift += change.NewText!.Length - change.Span.Length;
                else if (change.Span.Start < offset + Math.Max(1, length)) return null;
                else break;
            }
            offset += shift;
        }
        var original = component.ToOriginal(offset, length);
        if (original is null)
        {
            var declaration = Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree.ParseText(component.Generated.CSharp).GetRoot().DescendantNodes()
                .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.ClassDeclarationSyntax>()
                .FirstOrDefault(c => c.Identifier.ValueText == Path.GetFileNameWithoutExtension(path));
            if (declaration is not null && offset == declaration.Identifier.SpanStart && (length == 0 || length == declaration.Identifier.Span.Length)) original = (0, 0);
        }
        if (original is not { } span) return null;
        var position = TextPositions.Position(component.Text, span.Start);
        var lines = component.Text.ReplaceLineEndings("\n").Split('\n');
        if (position.Line < 0 || position.Line >= lines.Length || position.Character < 0 || position.Character > lines[position.Line].Length) return null;
        string Line(int index) => index >= 0 && index < lines.Length ? lines[index] : "";
        return new { uri = UriOf(path), range = TextPositions.Range(component.Text, span.Start, span.Start + span.Length), preview = lines[position.Line],
            before1 = Line(position.Line - 1), before2 = Line(position.Line - 2), after1 = Line(position.Line + 1), after2 = Line(position.Line + 2) };
    }

    private object[] DocumentColors(JsonObject parameters)
    {
        var uri = Uri(parameters);
        var text = parameters["text"]?.GetValue<string>() ?? documents.Get(uri)?.Text;
        if (text is null) return [];
        return [.. NssColors.Find(text, TextDocument.PathOf(uri).EndsWith(".nazor", StringComparison.OrdinalIgnoreCase)).Select(c => new
        {
            range = TextPositions.Range(text, c.Start, c.End),
            color = new { red = c.Color.R / 255.0, green = c.Color.G / 255.0, blue = c.Color.B / 255.0, alpha = c.Color.A / 255.0 },
        })];
    }

    private static object[] ColorPresentations(JsonObject parameters)
    {
        if (parameters["color"] is not JsonObject color || parameters["range"] is not { } range) return [];
        static byte Channel(JsonNode? value) => (byte)Math.Clamp(Math.Round((value?.GetValue<double>() ?? 0) * 255), 0, 255);
        var value = new Nazor.Style.Color(Channel(color["red"]), Channel(color["green"]), Channel(color["blue"]), Channel(color["alpha"]));
        return [.. NssColors.Presentations(value).Select(label => new { label, textEdit = new { range = range.DeepClone(), newText = label } })];
    }

    private Task RequestAsync(string method, JsonObject? parameters) => connection.WriteAsync(new JsonObject
    {
        ["id"] = "nazor-" + Interlocked.Increment(ref requests),
        ["method"] = method,
        ["params"] = parameters,
    }, stopping.Token);

    private string UriOf(string path) =>
        documents.All.FirstOrDefault(d => string.Equals(d.Path, path, StringComparison.OrdinalIgnoreCase))?.Uri ?? new Uri(path).AbsoluteUri;

    private async Task<int[]> SemanticTokensAsync(JsonObject parameters, JsonNode? range)
    {
        var document = documents.Get(Uri(parameters));
        if (document is null || !document.IsComponent) return [];
        var snapshot = Project(document.Path).Current();
        var component = snapshot.Component(document.Path);
        var csharpTokens = component is null ? [] : await CSharpLanguage.ClassifyAsync(snapshot, component, stopping.Token);
        var unresolved = UnresolvedTags(document, snapshot);
        var markup = SemanticTokens.Markup(document.Text, snapshot.Analysis.Catalog,
            offset => component is not null && component.Generated.Mappings.Any(m => m.Length > 0 && m.OriginalStart <= offset && offset < m.OriginalEnd), unresolved.Contains);
        if (range is null) return SemanticTokens.Encode(document.Text, markup, csharpTokens);
        var start = TextPositions.Offset(document.Text, new Position(range["start"]!["line"]!.GetValue<int>(), range["start"]!["character"]!.GetValue<int>()));
        var end = TextPositions.Offset(document.Text, new Position(range["end"]!["line"]!.GetValue<int>(), range["end"]!["character"]!.GetValue<int>()));
        return SemanticTokens.Encode(document.Text, markup, csharpTokens, start, end);
    }

    private static HashSet<int> UnresolvedTags(TextDocument document, ProjectSnapshot snapshot)
    {
        var offsets = snapshot.Analysis.Diagnostics.Where(d => d.Id is "RZ10012" or "RZ9985" &&
            string.Equals(d.Path, document.Path, StringComparison.OrdinalIgnoreCase))
            .Select(d => TextPositions.Offset(document.Text, new Position(d.Line - 1, d.Column - 1))).ToArray();
        var tags = NazorLanguage.Tags(document.Text);
        var unresolved = tags.Where(t => !t.IsEnd && offsets.Any(o => o >= t.Start && o < t.End)).Select(t => t.Start).ToHashSet();
        foreach (var tag in tags.Where(t => t.IsEnd))
            if (NazorLanguage.StartOf(tags, tag) is { } opening && unresolved.Contains(opening.Start)) unresolved.Add(tag.Start);
        return unresolved;
    }

    private object[] ParameterClassifications(JsonObject parameters)
    {
        if (documents.Get(Uri(parameters)) is not { IsComponent: true } document) return [];
        if (parameters["text"]?.GetValue<string>() is { } text && text != document.Text)
            document = documents.Set(document.Uri, text, document.Version);
        var snapshot = Project(document.Path).Current();
        var unresolved = UnresolvedTags(document, snapshot);
        var component = snapshot.Component(document.Path);
        return [.. SemanticTokens.Markup(document.Text, snapshot.Analysis.Catalog,
            offset => component is not null && component.Generated.Mappings.Any(m => m.Length > 0 && m.OriginalStart <= offset && offset < m.OriginalEnd), unresolved.Contains)
            .Where(t => t.Type is SemanticTokens.ComponentParameter or SemanticTokens.UnknownParameter)
            .Select(t => new { start = t.Start, length = t.Length, known = t.Type == SemanticTokens.ComponentParameter })];
    }

    private TextEdit[] Formatting(JsonObject parameters, JsonNode? range)
    {
        var document = documents.Get(Uri(parameters));
        if (document is null) return [];
        var formatted = Formatted(document, parameters);
        if (range is null) return TextEdits.Formatting(document.Text, formatted);
        var first = range["start"]!["line"]!.GetValue<int>();
        var last = range["end"]!["line"]!.GetValue<int>();
        if (last > first && range["end"]!["character"]!.GetValue<int>() == 0) last--;
        return TextEdits.Formatting(document.Text, formatted, first, last);
    }

    private TextEdit[] OnTypeFormatting(JsonObject parameters)
    {
        var typed = parameters["ch"]?.GetValue<string>();
        if (documents.Get(Uri(parameters)) is not { } document || typed is not ("}" or ";")) return [];
        var position = PositionOf(parameters);
        var offset = TextPositions.Offset(document.Text, position);
        var at = Math.Max(0, offset - 1);
        if (document.IsComponent && NazorLanguage.Classify(document.Text, at).Kind is not (NazorContextKind.CSharp or NazorContextKind.Style)) return [];
        var first = position.Line;
        if (typed == "}" && at < document.Text.Length && document.Text[at] == '}' && Opening(document.Text, at) is { } open)
            first = TextPositions.Position(document.Text, open).Line;
        return TextEdits.Formatting(document.Text, Formatted(document, parameters), first, position.Line);
    }

    private static int? Opening(string text, int close)
    {
        var depth = 0;
        for (var i = close; i >= 0; i--)
        {
            if (text[i] == '}') depth++;
            else if (text[i] == '{' && --depth == 0) return i;
        }
        return null;
    }

    private static string Formatted(TextDocument document, JsonObject parameters)
    {
        var tabSize = parameters["options"]?["tabSize"]?.GetValue<int>() ?? 4;
        var insertSpaces = parameters["options"]?["insertSpaces"]?.GetValue<bool>() ?? true;
        return document.IsComponent
            ? NazorFormatter.Format(document.Text, tabSize, insertSpaces)
            : NssFormatter.FormatDocument(document.Text, insertSpaces ? new string(' ', tabSize) : "\t");
    }

    private MarkupContent? Content(string? markdownText) => markdownText is null ? null
        : markdown ? new MarkupContent(markdownText) : new MarkupContent(PlainText(markdownText), "plaintext");

    private NazorCatalog Catalog(string path)
    {
        return Project(path).Current().Analysis.Catalog;
    }

    private IReadOnlyList<NazorComponentInfo> StyleSheetComponents(string path)
    {
        var owner = path.EndsWith(".nazor.nss", StringComparison.OrdinalIgnoreCase) ? path[..^4] : path;
        return Project(owner).Current().Analysis.Catalog.Components;
    }

    private Task PublishAsync(TextDocument document, IReadOnlyList<Diagnostic> diagnostics) =>
        NotifyAsync("textDocument/publishDiagnostics", new PublishDiagnosticsParams(document.Uri, document.Version, diagnostics));

    private Task NotifyAsync(string method, object parameters) => connection.WriteAsync(new JsonObject
    {
        ["method"] = method,
        ["params"] = JsonSerializer.SerializeToNode(parameters, Json.Options),
    }, stopping.Token);

    private Task RespondAsync(JsonNode id, object? result) => connection.WriteAsync(new JsonObject
    {
        ["id"] = id,
        ["result"] = result is null ? null : JsonSerializer.SerializeToNode(result, result.GetType(), Json.Options),
    }, stopping.Token);

    private Task ErrorAsync(JsonNode id, int code, string message) => connection.WriteAsync(new JsonObject
    {
        ["id"] = id,
        ["error"] = new JsonObject { ["code"] = code, ["message"] = message },
    }, stopping.Token);

    private Task Log(string message) => log.WriteLineAsync($"{DateTime.Now:HH:mm:ss.fff} {message}");

    private static string Uri(JsonObject parameters) => parameters["textDocument"]!["uri"]!.GetValue<string>();

    private static Position PositionOf(JsonObject parameters) =>
        new(parameters["position"]!["line"]!.GetValue<int>(), parameters["position"]!["character"]!.GetValue<int>());

    private static string PlainText(string markdown) => MarkdownSyntax().Replace(markdown, "");

    [GeneratedRegex(@"\*\*|`")]
    private static partial Regex MarkdownSyntax();

    private sealed class MethodNotFoundException : Exception;
}
