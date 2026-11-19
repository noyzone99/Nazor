using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Razor;

namespace Nazor.Compiler;

public sealed record NazorCompilerOptions
{
    public string RootNamespace { get; init; } = "Nazor.Generated";

    public string? ProjectDirectory { get; init; }

    public IReadOnlyList<string> References { get; init; } = [];

    public IReadOnlyList<string> CSharpSources { get; init; } = [];

    public Func<string, string?>? SourceText { get; init; }

    public bool LoadReferencesInMemory { get; init; }

    public CSharpParseOptions ParseOptions { get; init; } = new(LanguageVersion.CSharp14);
}

public sealed record GeneratedComponent(string SourcePath, string RelativePath, string CSharp, IReadOnlyList<StyleSource> Styles,
    IReadOnlyList<NazorDiagnostic> Warnings)
{
    public IReadOnlyList<NazorSourceMapping> Mappings { get; init; } = [];
}

public sealed record NazorAnalysis(IReadOnlyList<GeneratedComponent> Components, IReadOnlyList<NazorDiagnostic> Diagnostics, NazorCatalog Catalog);

public sealed partial class NazorCompiler
{
    public static IReadOnlyDictionary<int, int> MarkupTagSpans(string text, string path) => RazorSdkAdapter.MarkupTagSpans(text, path);

    public static int? WindowDirectiveOffset(string text, string path)
    {
        var diagnostics = new List<NazorDiagnostic>();
        var result = WindowDirective.Extract(text, path, diagnostics);
        return diagnostics.Count == 0 ? result.Window?.Offset : null;
    }

    private static readonly Lazy<MetadataReference[]> Framework = new(() => PlatformReferences(aspNet: false));
    private static readonly Lazy<MetadataReference[]> AspNetComponents = new(() => PlatformReferences(aspNet: true));
    private static readonly Lazy<HashSet<string>> PlatformNames = new(() => PlatformAssemblies().Where(IsFrameworkAssembly)
        .Select(Path.GetFileName).OfType<string>().ToHashSet(StringComparer.OrdinalIgnoreCase));

    private readonly NazorCompilerOptions options;
    private readonly MetadataReference[] references;
    private readonly MetadataReference[] frameworkReferences;
    private readonly MetadataReference[] discoveryReferences;
    private readonly RazorProjectEngine engine;
    private TagHelperCollection? discovered;
    private readonly HashSet<string> windowFiles = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> windowTypes = new(StringComparer.Ordinal);
    private readonly Dictionary<string, IReadOnlyDictionary<string, string?>> componentIds = new(StringComparer.OrdinalIgnoreCase);

    public NazorCompiler(NazorCompilerOptions? options = null)
    {
        this.options = options ?? new NazorCompilerOptions();
        var paths = this.options.References.Select(Path.GetFullPath).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        var hasTargetReferences = paths.Any(path => Path.GetFileName(path).Equals("System.Runtime.dll", StringComparison.OrdinalIgnoreCase));
        references = paths.Where(path => hasTargetReferences || !PlatformNames.Value.Contains(Path.GetFileName(path)))
            .Select(path => this.options.LoadReferencesInMemory
                ? (MetadataReference)MetadataReference.CreateFromImage(File.ReadAllBytes(path), filePath: path)
                : MetadataReference.CreateFromFile(path)).ToArray();
        frameworkReferences = hasTargetReferences ? [] : Framework.Value;
        var targetRuntime = paths.FirstOrDefault(path => Path.GetFileName(path).Equals("System.Runtime.dll", StringComparison.OrdinalIgnoreCase));
        var useTargetDiscovery = targetRuntime is not null && System.Reflection.AssemblyName.GetAssemblyName(targetRuntime).Version!.Major >= Environment.Version.Major;
        var discoveryInputs = useTargetDiscovery ? references : references.Where(r => !PlatformNames.Value.Contains(Path.GetFileName(r.Display!))).ToArray();
        var referenceNames = discoveryInputs.Select(r => Path.GetFileName(r.Display)).ToHashSet(StringComparer.OrdinalIgnoreCase);
        discoveryReferences = [.. AspNetComponents.Value.Where(r =>
            (!useTargetDiscovery || IsFrameworkAssembly(r.Display!, "Microsoft.AspNetCore.App")) &&
            !referenceNames.Contains(Path.GetFileName(r.Display))), .. discoveryInputs];
        engine = RazorProjectEngine.Create(RazorConfiguration.Default, RazorProjectFileSystem.Create(Directory.GetCurrentDirectory()), builder =>
        {
            builder.SetRootNamespace(this.options.RootNamespace);
            builder.ConfigureParserOptions(parser =>
            {
                parser.UseRoslynTokenizer = true;
                parser.CSharpParseOptions = this.options.ParseOptions;
            });
            CompilerFeatures.Register(builder);
            builder.Features.Add(new NazorMarkupPass());
            builder.Features.Add(new NazorEventModifierPass());
            builder.Features.Add(new NazorParameterPass());
            builder.Features.Add(new NazorIdPass((path, ids) => componentIds[path] = ids));
            builder.Features.Add(new NazorWindowPass(path => windowFiles.Contains(path), type => windowTypes.Contains(type)));
        });
    }

    public GeneratedComponent Generate(string nazorFile) => Generate([nazorFile])[0];

    public IReadOnlyList<GeneratedComponent> Generate(IReadOnlyList<string> nazorFiles)
    {
        var analysis = Analyze(nazorFiles);
        var errors = analysis.Diagnostics.Where(d => d.IsError).ToArray();
        if (errors.Length != 0) throw new NazorCompilationException(errors);
        return analysis.Components;
    }

    public NazorAnalysis Analyze(IReadOnlyList<string> nazorFiles)
    {
        var diagnostics = new List<NazorDiagnostic>();
        var sources = new List<SourceFile>();
        windowFiles.Clear();
        windowTypes.Clear();
        componentIds.Clear();
        foreach (var path in nazorFiles.Select(Path.GetFullPath).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            try
            {
                sources.Add(Read(path, diagnostics));
            }
            catch (NazorCompilationException exception)
            {
                diagnostics.AddRange(exception.Diagnostics);
            }
        }
        var declarations = sources.Select(source =>
        {
            var code = Rewrite(RazorSdkAdapter.GetCSharp(engine.ProcessDeclarationOnly(source.Document, RazorFileKind.Component, source.Imports, TagHelperCollection.Empty, default)).Text.ToString());
            return source.Window?.Apply(code) ?? code;
        }).ToArray();
        TagHelperCollection tagHelpers;
        try
        {
            tagHelpers = discovered = Discover(declarations, diagnostics);
        }
        catch (NazorCompilationException exception)
        {
            diagnostics.AddRange(exception.Diagnostics);
            if (discovered is null) return new([], diagnostics, NazorCatalog.Empty);
            tagHelpers = discovered;
        }

        var generated = new List<GeneratedComponent>();
        foreach (var source in sources)
        {
            var output = RazorSdkAdapter.GetCSharp(engine.Process(source.Document, RazorFileKind.Component, source.Imports, tagHelpers, default));
            var fileDiagnostics = output.Diagnostics.Select(d => d.Id is "RZ10012" or "RZ2012"
                ? NazorDiagnostic.FromRazor(d, source.Path) with { Severity = NazorDiagnosticSeverity.Error }
                : NazorDiagnostic.FromRazor(d, source.Path)).ToArray();
            var (csharp, map) = RewriteTracked(output.Text.ToString());
            if (source.Window is { } window) csharp = window.Apply(csharp, map);
            var checksum = Convert.ToHexStringLower(SHA256.HashData(SourceBytes(source.Path)));
            if (SourceChecksum().Match(csharp) is { Success: true } pragma)
            {
                var replacement = pragma.Groups["prefix"].Value + checksum;
                map.Then([new TextChange(new TextSpan(pragma.Index, pragma.Length), replacement)]);
                csharp = string.Concat(csharp.AsSpan(0, pragma.Index), replacement, csharp.AsSpan(pragma.Index + pragma.Length));
            }
            var styleDiagnostics = new List<NazorDiagnostic>();
            if (source.Styles.Count != 0 && !ReferencesStyle())
                styleDiagnostics.Add(new("NZC020", NazorDiagnosticSeverity.Error, source.Path, 1, 1, "Components with styles require a reference to Nazor.Style."));
            else csharp += StyleGeneration.ComponentRegistration(options, csharp, source.Path, source.Styles, styleDiagnostics,
                id => componentIds.GetValueOrDefault(source.Path)?.GetValueOrDefault(id));
            diagnostics.AddRange(fileDiagnostics.Concat(styleDiagnostics));
            var spans = RazorSdkAdapter.GetSourceMappings(output)
                .Where(m => string.IsNullOrEmpty(m.FilePath) || string.Equals(Path.GetFullPath(m.FilePath), source.Path, StringComparison.OrdinalIgnoreCase))
                .Select(m => (m.OriginalStart, m.OriginalLength, m.GeneratedStart, m.GeneratedLength));
            generated.Add(new(source.Path, source.RelativePath, csharp, source.Styles,
                fileDiagnostics.Concat(styleDiagnostics).Where(d => !d.IsError).ToArray())
            {
                Mappings = [.. SourceMappings.Compute(source.Text, csharp, map, spans), .. source.Window?.Mappings(csharp) ?? []],
            });
        }
        return new(generated, diagnostics, NazorCatalog.From(tagHelpers));
    }

    public string GenerateStyleSheets(IReadOnlyList<string> nssFiles, ICollection<NazorDiagnostic>? warnings = null)
    {
        var diagnostics = new List<NazorDiagnostic>();
        var files = nssFiles.Select(Path.GetFullPath).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        foreach (var file in files.Where(f => f.EndsWith(".nazor.nss", StringComparison.OrdinalIgnoreCase)))
            diagnostics.Add(new("NZS111", NazorDiagnosticSeverity.Error, file, 1, 1, "A .nazor.nss file belongs to its component and cannot be a standalone stylesheet."));
        if (!ReferencesStyle()) diagnostics.Add(NazorDiagnostic.Error("NZC020", "Stylesheets require a reference to Nazor.Style."));
        var ids = componentIds.Values.SelectMany(ids => ids).GroupBy(pair => pair.Key, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.Count() == 1 ? group.First().Value : null, StringComparer.Ordinal);
        var code = StyleGeneration.StyleSheetsClass(options,
            files.Where(f => !f.EndsWith(".nazor.nss", StringComparison.OrdinalIgnoreCase)).ToArray(), ReadText, diagnostics, ids.GetValueOrDefault);
        if (diagnostics.Any(d => d.IsError)) throw new NazorCompilationException(diagnostics.Where(d => d.IsError).ToArray());
        if (warnings is not null)
            foreach (var warning in diagnostics.Where(d => !d.IsError)) warnings.Add(warning);
        return code;
    }

    private bool ReferencesStyle() =>
        options.References.Any(r => Path.GetFileNameWithoutExtension(r).Equals("Nazor.Style", StringComparison.OrdinalIgnoreCase));

    public byte[] Emit(IReadOnlyList<GeneratedComponent> components, string assemblyName, IEnumerable<string>? extraSources = null)
    {
        var trees = components.Select(c => CSharpSyntaxTree.ParseText(c.CSharp, ParseOptions, c.SourcePath + ".g.cs"))
            .Concat(CSharpSources(options.CSharpSources.Concat(extraSources ?? []))).ToArray();
        var compilation = CSharpCompilation.Create(assemblyName, trees, frameworkReferences.Concat(references),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));
        using var image = new MemoryStream();
        var result = compilation.Emit(image);
        ThrowErrors(result.Diagnostics);
        return image.ToArray();
    }

    private CSharpParseOptions ParseOptions => options.ParseOptions;

    private SourceFile Read(string path, List<NazorDiagnostic> diagnostics)
    {
        if (!string.Equals(Path.GetExtension(path), ".nazor", StringComparison.OrdinalIgnoreCase))
            throw new NazorCompilationException($"Expected a .nazor source file: {path}");
        var projectDirectory = Path.GetFullPath(options.ProjectDirectory ?? Path.GetDirectoryName(path)!);
        var relativePath = Path.GetRelativePath(projectDirectory, path);
        if (relativePath.StartsWith("..", StringComparison.Ordinal) || Path.IsPathRooted(relativePath))
            throw new NazorCompilationException($"The file '{path}' is outside the project directory '{projectDirectory}'.");
        var extracted = StyleExtractor.Extract(ReadText(path), path, diagnostics);
        var styles = new List<StyleSource>();
        if (TryReadText(path + ".nss") is { } sidecar) styles.Add(new(path + ".nss", 0, sidecar));
        styles.AddRange(extracted.Styles);
        var declaration = WindowDirective.Extract(extracted.Razor, path, diagnostics);
        if (declaration.Window is not null) windowFiles.Add(path);
        var document = RazorSourceDocument.ReadFrom(new MemoryProjectItem(path, relativePath, declaration.Text));
        return new(path, relativePath, document, Imports(projectDirectory, path), styles, extracted.Razor, declaration.Window);
    }

    private System.Collections.Immutable.ImmutableArray<RazorSourceDocument> Imports(string projectDirectory, string path)
    {
        var defaults = new StringBuilder("""
            @using System
            @using System.Collections.Generic
            @using System.Linq
            @using System.Threading.Tasks
            @using Nazor
            @using Nazor.Input
            @using Microsoft.AspNetCore.Components

            """);
        if (options.References.Any(r => Path.GetFileNameWithoutExtension(r).Equals("Nazor.Controls", StringComparison.OrdinalIgnoreCase)))
            defaults.AppendLine("@using Nazor.Controls");
        var imports = new List<RazorSourceDocument> { RazorSourceDocument.Create(defaults.ToString(), Path.Combine(projectDirectory, DefaultImportsFileName)) };
        var folders = new Stack<string>();
        for (var folder = Path.GetDirectoryName(path); folder is not null; folder = Path.GetDirectoryName(folder))
        {
            folders.Push(folder);
            if (string.Equals(folder.TrimEnd(Path.DirectorySeparatorChar), projectDirectory.TrimEnd(Path.DirectorySeparatorChar), StringComparison.OrdinalIgnoreCase)) break;
        }
        foreach (var folder in folders)
        {
            var file = Path.Combine(folder, "_Imports.nazor");
            if (TryReadText(file) is { } text)
            {
                var errors = new List<NazorDiagnostic>();
                WindowDirective.Extract(text, file, errors);
                if (errors.Count != 0) throw new NazorCompilationException(errors);
                imports.Add(RazorSourceDocument.Create(text, file));
            }
        }
        return [.. imports];
    }

    private string? TryReadText(string path) => options.SourceText?.Invoke(Path.GetFullPath(path)) ?? (File.Exists(path) ? File.ReadAllText(path) : null);

    private string ReadText(string path) => TryReadText(path) ?? throw new FileNotFoundException($"Could not find file '{path}'.", path);

    private byte[] SourceBytes(string path) =>
        options.SourceText?.Invoke(Path.GetFullPath(path)) is { } text ? Encoding.UTF8.GetBytes(text) : File.ReadAllBytes(path);

    private TagHelperCollection Discover(IEnumerable<string> declarations, List<NazorDiagnostic> diagnostics)
    {
        var declarationCompilation = CSharpCompilation.Create("Nazor.Declarations",
            declarations.Select(d => CSharpSyntaxTree.ParseText(d, ParseOptions)).Concat(CSharpSources(options.CSharpSources)),
            frameworkReferences.Concat(references), new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        var declarationReference = declarationCompilation.ToMetadataReference();
        foreach (var assembly in new[] { declarationCompilation.Assembly }.Concat(declarationCompilation.SourceModule.ReferencedAssemblySymbols))
            FindWindows(assembly.GlobalNamespace);
        void FindWindows(INamespaceSymbol ns)
        {
            foreach (var child in ns.GetNamespaceMembers()) FindWindows(child);
            foreach (var type in ns.GetTypeMembers())
            {
                var window = false;
                for (var parent = type.BaseType; parent is not null; parent = parent.BaseType)
                    if (parent.ToDisplayString() == "Nazor.WindowComponentBase") { window = true; break; }
                if (!window) continue;
                windowTypes.Add($"{type.ContainingNamespace}.{type.Name}");
                var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                for (var parent = type; parent is not null && parent.ToDisplayString() != "Nazor.WindowComponentBase"; parent = parent.BaseType)
                    foreach (var member in parent.GetMembers().Where(m => !m.IsImplicitlyDeclared))
                        if (member.Name.Equals("Window", StringComparison.OrdinalIgnoreCase) ||
                            member is IPropertySymbol property && property.GetAttributes().Any(a => a.AttributeClass?.ToDisplayString() == "Nazor.ParameterAttribute") && !names.Add(member.Name))
                            diagnostics.Add(NazorDiagnostic.Error("NZC034", $"Duplicate or reserved window member '{member.Name}' in '{type.Name}'."));
            }
        }
        var facades = ComponentCatalog.CreateFacades(declarationCompilation, diagnostics);
        var projectComponents = ComponentCatalog.ComponentNames(declarationCompilation.Assembly);
        var projectNames = projectComponents.Select(n => n[(n.LastIndexOf('.') + 1)..]).ToHashSet(StringComparer.Ordinal);
        var facadeCompilation = CSharpCompilation.Create("Nazor.Discovery", [CSharpSyntaxTree.ParseText(facades, ParseOptions)],
            discoveryReferences.Append(declarationReference), new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        using var facadeImage = new MemoryStream();
        ThrowErrors(facadeCompilation.Emit(facadeImage).Diagnostics);
        var tagHelperReferences = discoveryReferences.Append(declarationReference)
            .Append(MetadataReference.CreateFromImage(facadeImage.ToArray())).ToArray();
        var discovery = RazorProjectEngine.Create(RazorConfiguration.Default, RazorProjectFileSystem.Create(Directory.GetCurrentDirectory()), builder =>
        {
            CompilerFeatures.Register(builder);
            builder.Features.Add(new DefaultMetadataReferenceFeature { References = tagHelperReferences });
            builder.Features.Add(new CompilationTagHelperFeature());
        });
        var found = discovery.Engine.GetFeatures<CompilationTagHelperFeature>().Single().GetTagHelpers(default);
        var builder = new TagHelperCollection.Builder();
        foreach (var tagHelper in found)
        {
            var aspNetType = tagHelper.TypeNamespace?.StartsWith("Microsoft.AspNetCore", StringComparison.Ordinal) == true;
            if (tagHelper.Kind is TagHelperKind.Component or TagHelperKind.ChildContent && aspNetType) continue;
            if (tagHelper.Kind is TagHelperKind.Component or TagHelperKind.ChildContent &&
                !tagHelper.TagMatchingRules.Any(r => r.TagName.Contains('.') || (r.ParentTag?.Contains('.') ?? false)) &&
                projectNames.Contains(tagHelper.TypeNameIdentifier ?? "") &&
                !projectComponents.Contains($"{tagHelper.TypeNamespace}.{tagHelper.TypeNameIdentifier}"))
                continue;
            if (tagHelper.Kind is TagHelperKind.RenderMode or TagHelperKind.FormName) continue;
            builder.Add(tagHelper);
        }
        return builder.ToCollection();
    }

    private string Rewrite(string csharp) => RewriteTracked(csharp).Text;

    private (string Text, PositionMap Map) RewriteTracked(string csharp)
    {
        var tree = CSharpSyntaxTree.ParseText(csharp, ParseOptions);
        var adapter = new RuntimeAdapter();
        adapter.Visit(tree.GetRoot());
        var changes = adapter.Changes.OrderBy(c => c.Span.Start).ToArray();
        var text = tree.GetText().WithChanges(changes).ToString();
        var map = new PositionMap().Then(changes);
        var hidden = DefaultImportsLine.Matches(text).Select(m => new TextChange(new TextSpan(m.Index, m.Length), "#line hidden")).ToArray();
        map.Then(hidden);
        return (HideDefaultImports(text), map);
    }

    private const string DefaultImportsFileName = "_Imports.nazor.defaults";

    private static readonly Regex DefaultImportsLine = new(
        @"^[ \t]*#line[ \t][^\r\n""]*""[^""\r\n]*" + Regex.Escape(DefaultImportsFileName) + @"""[ \t]*\r?$",
        RegexOptions.Multiline | RegexOptions.CultureInvariant);

    private static string HideDefaultImports(string csharp) => DefaultImportsLine.Replace(csharp, "#line hidden");

    private IEnumerable<SyntaxTree> CSharpSources(IEnumerable<string> paths) => paths.Select(Path.GetFullPath)
        .Distinct(StringComparer.OrdinalIgnoreCase).Select(path => CSharpSyntaxTree.ParseText(ReadText(path), ParseOptions, path));

    private static IEnumerable<string> PlatformAssemblies() => ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator);

    private static MetadataReference[] PlatformReferences(bool aspNet) => PlatformAssemblies().Where(path =>
            IsFrameworkAssembly(path, "Microsoft.NETCore.App") ||
            (aspNet && IsFrameworkAssembly(path, "Microsoft.AspNetCore.App") &&
             (!Path.GetFileName(path).StartsWith("Microsoft.AspNetCore.Components.", StringComparison.OrdinalIgnoreCase) ||
              Path.GetFileName(path).Equals("Microsoft.AspNetCore.Components.dll", StringComparison.OrdinalIgnoreCase))))
        .Select(path => (MetadataReference)MetadataReference.CreateFromFile(path)).ToArray();

    private static bool IsFrameworkAssembly(string path) =>
        IsFrameworkAssembly(path, "Microsoft.NETCore.App") || IsFrameworkAssembly(path, "Microsoft.AspNetCore.App");

    private static bool IsFrameworkAssembly(string path, string framework) =>
        path.Contains($"{Path.DirectorySeparatorChar}{framework}{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase);

    private static void ThrowErrors(IEnumerable<Diagnostic> diagnostics)
    {
        var errors = diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).Select(NazorDiagnostic.FromRoslyn).ToArray();
        if (errors.Length != 0) throw new NazorCompilationException(errors);
    }

    [GeneratedRegex("""^(?<prefix>#pragma checksum "[^"]*" "\{8829d00f-11b8-4213-878b-770e8597ac16\}" ")[0-9a-fA-F]+""")]
    private static partial Regex SourceChecksum();

    private sealed record SourceFile(string Path, string RelativePath, RazorSourceDocument Document,
        System.Collections.Immutable.ImmutableArray<RazorSourceDocument> Imports, IReadOnlyList<StyleSource> Styles, string Text, WindowDirective? Window);

    private sealed class MemoryProjectItem(string path, string relativePath, string content) : RazorProjectItem
    {
        public override string BasePath => "/";
        public override string FilePath => "/" + relativePath.Replace('\\', '/');
        public override string PhysicalPath => path;
        public override string RelativePhysicalPath => relativePath;
        public override bool Exists => true;
        public override Stream Read() => new MemoryStream(Encoding.UTF8.GetBytes(content));
    }
}
