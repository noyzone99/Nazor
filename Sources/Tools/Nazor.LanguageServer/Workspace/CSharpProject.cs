using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using Nazor.Compiler;

namespace Nazor.LanguageServer.Workspace;

internal sealed record ComponentDocument(string Path, string Text, DocumentId Id, GeneratedComponent Generated)
{
    public int? ToGenerated(int offset)
    {
        foreach (var mapping in Generated.Mappings)
            if (mapping.OriginalStart <= offset && offset <= mapping.OriginalEnd) return mapping.GeneratedStart + offset - mapping.OriginalStart;
        return null;
    }

    public (int Start, int Length)? ToOriginal(int generatedStart, int length)
    {
        foreach (var mapping in Generated.Mappings)
            if (mapping.GeneratedStart <= generatedStart && generatedStart + length <= mapping.GeneratedEnd)
                return (mapping.OriginalStart + generatedStart - mapping.GeneratedStart, length);
        return null;
    }
}

internal sealed class CSharpProject
{
    private static readonly Lazy<MefHostServices> Host = new(() => MefHostServices.Create(MefHostServices.DefaultAssemblies));

    private readonly AdhocWorkspace workspace;
    private readonly ProjectId projectId;
    private readonly Dictionary<string, DocumentId> documentsByPath = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, DocumentId> configsByPath = new(StringComparer.OrdinalIgnoreCase);
    private readonly ProjectContext context;
    private readonly CSharpCommandLineArguments arguments;
    private readonly IReadOnlyDictionary<string, string>? editorOptions;
    private readonly Dictionary<string, DocumentId> additionalByPath = new(StringComparer.OrdinalIgnoreCase);

    public CSharpProject(ProjectContext context, IReadOnlyDictionary<string, string>? editorOptions = null)
    {
        this.context = context;
        this.editorOptions = editorOptions;
        arguments = RoslynConfiguration.Read(context);
        workspace = new AdhocWorkspace(Host.Value);
        projectId = ProjectId.CreateNewId(context.RootNamespace);
        var info = ProjectInfo.Create(projectId, VersionStamp.Create(), context.RootNamespace, context.RootNamespace, LanguageNames.CSharp,
            compilationOptions: arguments.CompilationOptions.WithOutputKind(OutputKind.DynamicallyLinkedLibrary),
            parseOptions: arguments.ParseOptions.WithDocumentationMode(DocumentationMode.Parse), metadataReferences: References(context.References),
            analyzerReferences: arguments.AnalyzerReferences.Where(r => File.Exists(r.FilePath)).Select(r => new AnalyzerFileReference(r.FilePath, new AnalyzerLoader())));
        workspace.AddProject(info);
    }

    public Solution Solution => workspace.CurrentSolution;

    public Project Update(IReadOnlyDictionary<string, string> files, Func<string, string?>? read = null)
    {
        var solution = workspace.CurrentSolution;
        foreach (var file in arguments.AdditionalFiles)
        {
            var text = read?.Invoke(file.Path) ?? (File.Exists(file.Path) ? File.ReadAllText(file.Path) : null);
            if (text is null)
            {
                if (additionalByPath.Remove(file.Path, out var missing)) solution = solution.RemoveAdditionalDocument(missing);
                continue;
            }
            if (additionalByPath.TryGetValue(file.Path, out var additional)) solution = solution.WithAdditionalDocumentText(additional, SourceText.From(text));
            else
            {
                additional = DocumentId.CreateNewId(projectId, file.Path);
                additionalByPath[file.Path] = additional;
                solution = solution.AddAdditionalDocument(additional, Path.GetFileName(file.Path), SourceText.From(text), filePath: file.Path);
            }
        }
        var configTexts = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (editorOptions is { Count: > 0 })
            configTexts[Path.Combine(context.Directory, ".nazor.ide.globalconfig")] = "is_global = true\nglobal_level = -1000\n" +
                string.Join('\n', editorOptions.Select(o => o.Key + " = " + o.Value)) + "\n";
        foreach (var path in RoslynConfiguration.Configs(context, arguments).Concat(files.Keys.SelectMany(AncestorConfigs)).Distinct(StringComparer.OrdinalIgnoreCase))
            if ((read?.Invoke(path) ?? (File.Exists(path) ? File.ReadAllText(path) : null)) is { } text) configTexts[path] = text;
        var configSet = AnalyzerConfigSet.Create(configTexts.Select(c => AnalyzerConfig.Parse(c.Value, c.Key)).ToArray());
        foreach (var path in files.Keys.Where(p => p.EndsWith(".nazor.g.cs", StringComparison.OrdinalIgnoreCase)))
        {
            var options = configSet.GetOptionsForSourcePath(path[..^5] + ".cs");
            var values = options.AnalyzerOptions.SetItem("generated_code", options.AnalyzerOptions.GetValueOrDefault("generated_code", "false"));
            var config = Path.Combine(Path.GetDirectoryName(path)!, ".editorconfig");
            var section = "\n[" + Path.GetFileName(path) + "]\n" + string.Join('\n', values.Select(o => o.Key + " = " + o.Value)) + "\n" +
                string.Join('\n', options.TreeOptions.Select(o => "dotnet_diagnostic." + o.Key + ".severity = " + (o.Value switch
                { ReportDiagnostic.Error => "error", ReportDiagnostic.Warn => "warning", ReportDiagnostic.Info => "suggestion", ReportDiagnostic.Hidden => "silent", ReportDiagnostic.Suppress => "none", _ => "default" }))) + "\n";
            configTexts[config] = configTexts.GetValueOrDefault(config, "") + section;
        }
        var configs = configTexts.Keys.ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var (path, id) in configsByPath.ToArray())
            if (!configs.Contains(path)) { solution = solution.RemoveAnalyzerConfigDocument(id); configsByPath.Remove(path); }
        foreach (var path in configs)
        {
            var text = configTexts[path];
            var source = SourceText.From(text);
            if (configsByPath.TryGetValue(path, out var id)) solution = solution.WithAnalyzerConfigDocumentText(id, source);
            else
            {
                id = DocumentId.CreateNewId(projectId, path);
                configsByPath[path] = id;
                solution = solution.AddAnalyzerConfigDocument(id, Path.GetFileName(path), source, filePath: path);
            }
        }
        foreach (var (path, id) in documentsByPath.ToArray())
        {
            if (files.ContainsKey(path)) continue;
            solution = solution.RemoveDocument(id);
            documentsByPath.Remove(path);
        }
        foreach (var (path, text) in files)
        {
            var source = SourceText.From(text, System.Text.Encoding.UTF8);
            if (documentsByPath.TryGetValue(path, out var id))
            {
                if (!solution.GetDocument(id)!.TryGetText(out var current) || !current.ContentEquals(source)) solution = solution.WithDocumentText(id, source);
                continue;
            }
            id = DocumentId.CreateNewId(projectId, path);
            documentsByPath[path] = id;
            solution = solution.AddDocument(DocumentInfo.Create(id, System.IO.Path.GetFileName(path), loader: TextLoader.From(TextAndVersion.Create(source, VersionStamp.Create())), filePath: path));
        }
        if (!workspace.TryApplyChanges(solution)) throw new InvalidOperationException("The C# workspace rejected the changes.");
        return workspace.CurrentSolution.GetProject(projectId)!;
    }

    public DocumentId? DocumentFor(string path) => documentsByPath.GetValueOrDefault(path);

    private static IEnumerable<string> AncestorConfigs(string path)
    {
        for (var folder = Path.GetDirectoryName(path); folder is not null; folder = Path.GetDirectoryName(folder))
            if (File.Exists(Path.Combine(folder, ".editorconfig"))) yield return Path.Combine(folder, ".editorconfig");
    }

    private static ImmutableArray<MetadataReference> References(IReadOnlyList<string> paths)
    {
        var all = paths.ToList();
        if (!all.Any(p => System.IO.Path.GetFileName(p).Equals("System.Runtime.dll", StringComparison.OrdinalIgnoreCase)))
            all.AddRange(((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(System.IO.Path.PathSeparator)
                .Where(p => p.Contains($"{System.IO.Path.DirectorySeparatorChar}Microsoft.NETCore.App{System.IO.Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase)));
        return [.. all.Distinct(StringComparer.OrdinalIgnoreCase).Where(File.Exists).Select(path =>
        {
            var xml = System.IO.Path.ChangeExtension(path, ".xml");
            return (MetadataReference)MetadataReference.CreateFromFile(path, documentation: File.Exists(xml) ? XmlDocumentationProvider.CreateFromFile(xml) : null);
        })];
    }
}
