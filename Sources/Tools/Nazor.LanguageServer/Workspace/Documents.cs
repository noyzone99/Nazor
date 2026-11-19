using System.Collections.Concurrent;
using Microsoft.CodeAnalysis;
using Nazor.Compiler;

namespace Nazor.LanguageServer.Workspace;

internal sealed record TextDocument(string Uri, string Path, string Text, int Version)
{
    public bool IsComponent => Path.EndsWith(".nazor", StringComparison.OrdinalIgnoreCase);

    public bool IsStyleSheet => Path.EndsWith(".nss", StringComparison.OrdinalIgnoreCase);

    public static string PathOf(string uri) => System.IO.Path.GetFullPath(new Uri(uri).LocalPath);
}

internal sealed class DocumentStore
{
    private readonly ConcurrentDictionary<string, TextDocument> documents = new(StringComparer.OrdinalIgnoreCase);

    public TextDocument Set(string uri, string text, int version)
    {
        var document = new TextDocument(uri, TextDocument.PathOf(uri), text, version);
        documents[document.Path] = document;
        return document;
    }

    public TextDocument? Remove(string uri) => documents.TryRemove(TextDocument.PathOf(uri), out var document) ? document : null;

    public TextDocument? Get(string uri) => documents.GetValueOrDefault(TextDocument.PathOf(uri));

    public IReadOnlyList<TextDocument> All => [.. documents.Values];

    public string? TextOf(string path) => documents.TryGetValue(path, out var document) ? document.Text : null;
}

internal sealed record ProjectSnapshot(NazorAnalysis Analysis, Project CSharp, IReadOnlyDictionary<string, ComponentDocument> Components)
{
    public ComponentDocument? Component(string path) => Components.GetValueOrDefault(path);

    public Document? Document(ComponentDocument component) => CSharp.GetDocument(component.Id);
}

internal sealed class ProjectAnalyzer(ProjectContext context, DocumentStore documents, IReadOnlyDictionary<string, string>? editorOptions = null)
{
    private readonly Lock gate = new();
    private NazorCompiler? compiler;
    private CSharpProject? csharp;
    private Dictionary<string, string> texts = new(StringComparer.OrdinalIgnoreCase);
    private string? roslynInputs;
    private Dictionary<string, DateTime> inputTimes = new(StringComparer.OrdinalIgnoreCase);
    private string[] csharpInputs = [];
    private (string Path, DateTime Time)[] referenceInputs = [];

    public ProjectContext Context { get; private set; } = context;

    public ProjectSnapshot? Last { get; private set; }

    public void Invalidate() { lock (gate) Last = null; }

    public void SetEditorOptions(IReadOnlyDictionary<string, string> options)
    {
        lock (gate) { editorOptions = options; csharp = null; Last = null; }
    }

    public bool Contains(string path) =>
        path.StartsWith(Context.Directory.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) &&
        !ProjectContext.IsBuildOutput(Context.Directory, path);

    public ProjectSnapshot Current()
    {
        lock (gate)
        {
            var current = documents.All.Where(d => Contains(d.Path)).ToArray();
            if (Last is { } last && current.Length == texts.Count && current.All(d => texts.GetValueOrDefault(d.Path) == d.Text) &&
                ProjectContext.LatestResponseFile(Context.Directory) == (Context.ResponseFile, Context.ResponseFileTime) &&
                inputTimes.All(i => File.GetLastWriteTimeUtc(i.Key) == i.Value))
                return last;
            return Analyze();
        }
    }

    public ProjectSnapshot Analyze()
    {
        lock (gate)
        {
            var (responseFile, time) = ProjectContext.LatestResponseFile(Context.Directory);
            if (responseFile != Context.ResponseFile || time != Context.ResponseFileTime)
            {
                Context = ProjectContext.ForDirectory(Context.Directory);
                compiler = null;
                csharp = null;
            }
            texts = documents.All.Where(d => Contains(d.Path)).ToDictionary(d => d.Path, d => d.Text, StringComparer.OrdinalIgnoreCase);
            var inputs = RoslynConfiguration.FileFor(Context) is { } manifest && File.Exists(manifest) ? File.ReadAllText(manifest) : null;
            if (inputs != roslynInputs) { compiler = null; csharp = null; roslynInputs = inputs; }
            var references = Context.References.Select(path => (Path: path, Time: File.GetLastWriteTimeUtc(path))).ToArray();
            if (!references.SequenceEqual(referenceInputs))
            {
                compiler = null;
                csharp = null;
                referenceInputs = references;
            }
            var files = Context.Components.Where(File.Exists)
                .Concat(texts.Keys.Where(p => p.EndsWith(".nazor", StringComparison.OrdinalIgnoreCase) && Contains(p)))
                .Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
            var sourcePaths = Context.CSharpSources.Concat(files.Select(p => p + ".cs"))
                .Concat(texts.Keys.Where(p => p.EndsWith(".cs", StringComparison.OrdinalIgnoreCase) && Contains(p)))
                .Where(p => File.Exists(p) || texts.ContainsKey(p)).Distinct(StringComparer.OrdinalIgnoreCase).Order(StringComparer.OrdinalIgnoreCase).ToArray();
            if (!sourcePaths.SequenceEqual(csharpInputs, StringComparer.OrdinalIgnoreCase)) { compiler = null; csharpInputs = sourcePaths; }
            compiler ??= new NazorCompiler(new NazorCompilerOptions
            {
                RootNamespace = Context.RootNamespace,
                ProjectDirectory = Context.Directory,
                References = Context.References,
                CSharpSources = sourcePaths,
                SourceText = path => texts.GetValueOrDefault(path),
                LoadReferencesInMemory = true,
                ParseOptions = RoslynConfiguration.Read(Context).ParseOptions,
            });
            var analysis = compiler.Analyze(files);

            csharp ??= new CSharpProject(Context, editorOptions);
            var sources = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var component in analysis.Components) sources[component.SourcePath + ".g.cs"] = component.CSharp;
            foreach (var source in sourcePaths)
                if (texts.GetValueOrDefault(source) is { } open) sources[source] = open;
                else if (File.Exists(source)) sources[source] = File.ReadAllText(source);
            if (StyleSheetsClass() is { } styleSheets) sources[Path.Combine(Context.Directory, "obj", "NazorStyleSheets.g.cs")] = styleSheets;
            var project = csharp.Update(sources, path => texts.GetValueOrDefault(path));
            inputTimes = sourcePaths.Concat(Context.Components).Concat(Context.References).Concat(files.Select(p => p + ".cs")).Concat(RoslynConfiguration.Configs(Context, RoslynConfiguration.Read(Context)))
                .Concat(project.AnalyzerConfigDocuments.Select(d => d.FilePath).OfType<string>())
                .Concat(RoslynConfiguration.Read(Context).AdditionalFiles.Select(f => f.Path))
                .Concat(new[] { Context.ResponseFile, RoslynConfiguration.FileFor(Context) }.OfType<string>())
                .Distinct(StringComparer.OrdinalIgnoreCase).ToDictionary(p => p, File.GetLastWriteTimeUtc, StringComparer.OrdinalIgnoreCase);
            var components = analysis.Components.ToDictionary(c => c.SourcePath,
                c => new ComponentDocument(c.SourcePath, texts.GetValueOrDefault(c.SourcePath) ?? File.ReadAllText(c.SourcePath), csharp.DocumentFor(c.SourcePath + ".g.cs")!, c),
                StringComparer.OrdinalIgnoreCase);
            return Last = new ProjectSnapshot(analysis, project, components);
        }
    }

    private string? StyleSheetsClass()
    {
        if (Context.StyleSheets.Count == 0) return null;
        try
        {
            return compiler!.GenerateStyleSheets(Context.StyleSheets);
        }
        catch (NazorCompilationException)
        {
            return null;
        }
    }
}
