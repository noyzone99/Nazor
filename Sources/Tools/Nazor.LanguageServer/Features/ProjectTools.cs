using System.Collections.Immutable;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using Nazor.LanguageServer.Workspace;

namespace Nazor.LanguageServer.Features;

internal static class ProjectTools
{
    private static readonly ConditionalWeakTable<ProjectSnapshot, Lazy<Task<ImmutableArray<Diagnostic>>>> Analyses = new();
    private static readonly ConditionalWeakTable<ProjectSnapshot, Lazy<Task<ImmutableArray<Diagnostic>>>> CompilerAnalyses = new();
#pragma warning disable RS2008
    private static readonly DiagnosticDescriptor LoadFailure = new("NZL001", "Analyzer unavailable", "{0}", "Tooling", Microsoft.CodeAnalysis.DiagnosticSeverity.Warning, true);
#pragma warning restore RS2008

    public static Task<ImmutableArray<Diagnostic>> DiagnosticsAsync(ProjectSnapshot snapshot, CancellationToken cancellation) =>
        Analyses.GetValue(snapshot, s => new(() => AnalyzeAsync(s))).Value.WaitAsync(cancellation);

    public static Task<ImmutableArray<Diagnostic>> CompilerDiagnosticsAsync(ProjectSnapshot snapshot, CancellationToken cancellation) =>
        CompilerAnalyses.GetValue(snapshot, s => new(() => Task.Run(async () =>
            await s.CSharp.GetCompilationAsync() is { } compilation ? compilation.GetDiagnostics() : []))).Value.WaitAsync(cancellation);

    private static async Task<ImmutableArray<Diagnostic>> AnalyzeAsync(ProjectSnapshot snapshot)
    {
        var project = snapshot.CSharp;
        var failures = new List<Diagnostic>();
        var analyzers = ImmutableArray.CreateBuilder<DiagnosticAnalyzer>();
        foreach (var reference in project.AnalyzerReferences)
        {
            void Failed(object? sender, AnalyzerLoadFailureEventArgs args) => failures.Add(Diagnostic.Create(LoadFailure, Location.None, reference.Display + ": " + args.Message));
            if (reference is AnalyzerFileReference file) file.AnalyzerLoadFailed += Failed;
            try { analyzers.AddRange(reference.GetAnalyzers(LanguageNames.CSharp)); }
            finally { if (reference is AnalyzerFileReference loaded) loaded.AnalyzerLoadFailed -= Failed; }
        }
        if (analyzers.Count == 0 || await project.GetCompilationAsync() is not { } compilation) return [.. failures];
        var analyzerOptions = project.AnalyzerOptions;
        var options = new CompilationWithAnalyzersOptions(analyzerOptions, null, concurrentAnalysis: true, logAnalyzerExecutionTime: false, reportSuppressedDiagnostics: false);
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        ImmutableArray<Diagnostic> diagnostics;
        try { diagnostics = await compilation.WithAnalyzers(analyzers.ToImmutable(), options).GetAnalyzerDiagnosticsAsync(timeout.Token); }
        catch (OperationCanceledException) when (timeout.IsCancellationRequested)
        {
            return [.. failures, Diagnostic.Create(LoadFailure, Location.None, "Project analyzers exceeded the 30 second analysis budget.")];
        }
        var projections = snapshot.Components.Values.Select(c => snapshot.Document(c)?.FilePath).ToHashSet(StringComparer.OrdinalIgnoreCase);
        return [.. failures, .. diagnostics.Where(d => d.Id != "IDE0055" || !projections.Contains(d.Location.SourceTree?.FilePath))];
    }

    public static async Task<List<CodeActionResult>> FixesAsync(ProjectSnapshot snapshot, ComponentDocument component, int start, int end, Action<Exception> failed, CancellationToken cancellation)
    {
        var result = new List<CodeActionResult>();
        if (snapshot.Document(component) is not { } document) return result;
        var tree = await document.GetSyntaxTreeAsync(cancellation);
        var model = await document.GetSemanticModelAsync(cancellation);
        if (tree is null || model is null) return result;
        var diagnostics = (await CompilerDiagnosticsAsync(snapshot, cancellation)).Concat(await DiagnosticsAsync(snapshot, cancellation))
            .Where(d => !d.IsSuppressed && d.Location.SourceTree == tree &&
                component.ToOriginal(d.Location.SourceSpan.Start, d.Location.SourceSpan.Length) is { } original &&
                original.Start <= end && original.Start + original.Length >= start).ToArray();
        if (diagnostics.Length == 0) return result;

        var assemblies = new List<Assembly> { Assembly.Load("Microsoft.CodeAnalysis.CSharp.Features"), Assembly.Load("Microsoft.CodeAnalysis.Features") };
        foreach (var reference in snapshot.CSharp.AnalyzerReferences.OfType<AnalyzerFileReference>())
        {
            try { assemblies.Add(reference.GetAssembly()); }
            catch (Exception exception) when (exception is not OperationCanceledException) { failed(exception); }
        }
        foreach (var assembly in assemblies.Distinct())
        {
            IEnumerable<Type> types;
            try { types = assembly.GetTypes(); }
            catch (ReflectionTypeLoadException exception) { failed(exception); types = exception.Types.OfType<Type>(); }
            catch (Exception exception) when (exception is not OperationCanceledException) { failed(exception); continue; }
            foreach (var type in types.Where(t => !t.IsAbstract && typeof(CodeFixProvider).IsAssignableFrom(t) &&
                t.GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, Type.EmptyTypes, null) is not null &&
                t.GetCustomAttributes<ExportCodeFixProviderAttribute>().Any(a => a.Languages.Contains(LanguageNames.CSharp))))
            {
                try
                {
                    if (Activator.CreateInstance(type, nonPublic: true) is not CodeFixProvider provider) continue;
                    foreach (var diagnostic in diagnostics.Where(d => provider.FixableDiagnosticIds.Contains(d.Id)))
                    {
                        var actions = new List<CodeAction>();
                        await provider.RegisterCodeFixesAsync(new CodeFixContext(document, diagnostic, (action, _) => actions.Add(action), cancellation));
                        foreach (var action in actions)
                            await AddAsync(action);
                    }
                }
                catch (OperationCanceledException) when (cancellation.IsCancellationRequested) { throw; }
                catch (Exception exception) { failed(exception); }
            }
        }
        return result;

        async Task AddAsync(CodeAction action)
        {
            if (!action.NestedActions.IsEmpty)
            {
                foreach (var nested in action.NestedActions) await AddAsync(nested);
                return;
            }
            var operations = await action.GetOperationsAsync(cancellation);
            if (operations.Length != 1 || operations[0] is not ApplyChangesOperation apply) return;
            if (await MapAsync(snapshot, apply.ChangedSolution, cancellation) is { Count: > 0 } changes)
                result.Add(new(action.Title, changes));
        }
    }

    internal static async Task<List<FileChange>?> MapAsync(ProjectSnapshot snapshot, Solution changed, CancellationToken cancellation)
    {
        var before = snapshot.CSharp.Solution;
        var solutionChanges = changed.GetChanges(before);
        if (solutionChanges.GetAddedProjects().Any() || solutionChanges.GetRemovedProjects().Any()) return null;
        var result = new List<FileChange>();
        foreach (var projectChanges in solutionChanges.GetProjectChanges())
        {
            if (projectChanges.GetAddedDocuments().Any() || projectChanges.GetRemovedDocuments().Any() ||
                projectChanges.GetAddedMetadataReferences().Any() || projectChanges.GetRemovedMetadataReferences().Any() ||
                projectChanges.GetAddedProjectReferences().Any() || projectChanges.GetRemovedProjectReferences().Any() ||
                projectChanges.GetAddedAnalyzerReferences().Any() || projectChanges.GetRemovedAnalyzerReferences().Any() ||
                projectChanges.GetAddedAdditionalDocuments().Any() || projectChanges.GetRemovedAdditionalDocuments().Any() || projectChanges.GetChangedAdditionalDocuments().Any() ||
                projectChanges.GetAddedAnalyzerConfigDocuments().Any() || projectChanges.GetRemovedAnalyzerConfigDocuments().Any() || projectChanges.GetChangedAnalyzerConfigDocuments().Any() ||
                projectChanges.OldProject.CompilationOptions != projectChanges.NewProject.CompilationOptions || projectChanges.OldProject.ParseOptions != projectChanges.NewProject.ParseOptions)
                return null;
            foreach (var id in projectChanges.GetChangedDocuments())
            {
                var oldDocument = before.GetDocument(id)!;
                var newDocument = changed.GetDocument(id)!;
                if (oldDocument.FilePath is not { } path || newDocument.FilePath != path) return null;
                var oldText = await oldDocument.GetTextAsync(cancellation);
                var owner = snapshot.Components.Values.FirstOrDefault(c => c.Id == id);
                if (owner is null)
                {
                    if (path.EndsWith(".g.cs", StringComparison.OrdinalIgnoreCase)) return null;
                    result.Add(new(path, oldText.ToString(), (await newDocument.GetTextAsync(cancellation)).ToString()));
                    continue;
                }
                var edits = new List<TextChange>();
                var usings = new List<string>();
                foreach (var change in await newDocument.GetTextChangesAsync(oldDocument, cancellation))
                {
                    if (owner.ToOriginal(change.Span.Start, change.Span.Length) is not { } span)
                    {
                        if (!string.IsNullOrWhiteSpace(oldText.ToString(change.Span)) || change.NewText is null) return null;
                        var imported = SyntaxFactory.ParseCompilationUnit(change.NewText);
                        if (imported.ContainsDiagnostics || imported.Usings.Count == 0 || imported.Members.Count > 0 || imported.Externs.Count > 0 || imported.AttributeLists.Count > 0 ||
                            imported.Usings.Any(u => !u.GlobalKeyword.IsKind(SyntaxKind.None))) return null;
                        usings.AddRange(imported.Usings.Select(u => "@" + u.WithoutTrivia().ToString().TrimEnd(';')));
                        continue;
                    }
                    if (owner.Text.Substring(span.Start, span.Length) != oldText.ToString(change.Span)) return null;
                    edits.Add(new(new TextSpan(span.Start, span.Length), change.NewText ?? ""));
                }
                var updated = SourceText.From(owner.Text).WithChanges(edits).ToString();
                var newLine = owner.Text.Contains("\r\n", StringComparison.Ordinal) ? "\r\n" : "\n";
                var imports = usings.Distinct(StringComparer.Ordinal).Where(u => !owner.Text.Split('\n').Any(l => l.Trim() == u));
                result.Add(new(owner.Path, owner.Text, string.Concat(imports.Select(u => u + newLine)) + updated));
            }
        }
        return result;
    }
}
