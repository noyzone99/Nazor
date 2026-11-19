using System.Diagnostics;
using Nazor.Development;
using Nazor.Style;
using Microsoft.CodeAnalysis.CSharp;

namespace Nazor.Compiler.Watch;

public sealed record StyleUpdate(string Source, string? Scope, string SheetJson);

public sealed record WatchResult(IReadOnlyList<string> GeneratedChanged, IReadOnlyList<StyleUpdate> StylesChanged,
    IReadOnlyList<NazorDiagnostic> Diagnostics, bool DiscoveryFailed, TimeSpan Elapsed)
{
    public int Components { get; init; }

    public int StyleSheets { get; init; }
}

public sealed class WatchSession
{
    private const int Attempts = 3;
    private static readonly TimeSpan RetryDelay = TimeSpan.FromMilliseconds(50);

    private readonly string? responseFile;
    private readonly Dictionary<string, string> reportedSheets = new(StringComparer.OrdinalIgnoreCase);
    private NazorCompilerOptions options;
    private string[] extraSources;
    private DateTime responseFileTime;
    private long responseFileLength = -1;
    private NazorCompiler? compiler;
    private string? compiledRootNamespace;
    private (string Path, DateTime Time)[] loadedReferences = [];
    private string[] compiledSources = [];

    public WatchSession(NazorCompilerOptions options, string outputDirectory, string projectDirectory, string? responseFile)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(outputDirectory);
        ArgumentNullException.ThrowIfNull(projectDirectory);
        ProjectDirectory = DevChannelProtocol.NormalizeDirectory(projectDirectory);
        OutputDirectory = DevChannelProtocol.NormalizeDirectory(outputDirectory);
        this.responseFile = responseFile is null ? null : Path.GetFullPath(responseFile);
        this.options = options with { ProjectDirectory = ProjectDirectory, LoadReferencesInMemory = true };
        extraSources = ExtraSources(options.CSharpSources);
    }

    public string ProjectDirectory { get; }

    public string OutputDirectory { get; }

    public WatchResult Refresh()
    {
        var stopwatch = Stopwatch.StartNew();
        var diagnostics = new List<NazorDiagnostic>();
        ReloadResponseFile();
        var components = ProjectFiles.Enumerate(ProjectDirectory, OutputDirectory, "*.nazor")
            .Where(f => !Path.GetFileName(f).Equals("_Imports.nazor", StringComparison.OrdinalIgnoreCase)).ToArray();
        var styleSheets = ProjectFiles.Enumerate(ProjectDirectory, OutputDirectory, "*.nss")
            .Where(f => !f.EndsWith(".nazor.nss", StringComparison.OrdinalIgnoreCase)).ToArray();
        var sources = ProjectFiles.Enumerate(ProjectDirectory, OutputDirectory, "*.cs")
            .Where(f => !f.EndsWith(".g.cs", StringComparison.OrdinalIgnoreCase))
            .Concat(extraSources.Where(File.Exists)).ToArray();
        var generated = new List<string>();
        var styles = new List<StyleUpdate>();
        var discoveryFailed = false;

        var compiler = Retry(() => Compiler(sources), "the referenced assemblies", diagnostics);
        if (compiler is null) discoveryFailed = true;
        else if (components.Length != 0)
        {
            var analysis = Retry(() => compiler.Analyze(components), "the components", diagnostics);
            if (analysis is null) discoveryFailed = true;
            else
            {
                diagnostics.AddRange(analysis.Diagnostics);
                discoveryFailed = analysis.Diagnostics.Any(d => d.IsError && (d.Id == "NZC020" || !components.Any(c => Belongs(d.Path, c))));
                foreach (var component in analysis.Components)
                {
                    if (discoveryFailed) break;
                    if (analysis.Diagnostics.Any(d => d.IsError && Belongs(d.Path, component.SourcePath))) continue;
                    var path = Path.Combine(OutputDirectory, component.RelativePath + ".g.cs");
                    if (WriteIfChanged(path, component.CSharp, diagnostics)) generated.Add(path);
                    var (sheet, _) = StyleGeneration.CompileComponentStyles(options, component.SourcePath, component.Styles);
                    if (sheet is null) continue;
                    var (ns, name, arity) = StyleGeneration.ComponentClass(component.CSharp);
                    Report(sheet, StyleGeneration.ClrName(ns, name, arity), styles);
                }
            }
        }

        foreach (var file in styleSheets)
        {
            var text = Retry(() => File.ReadAllText(file), file, diagnostics);
            if (text is null) continue;
            var compilation = StyleGeneration.CompileStyleSheet(options, file, text);
            diagnostics.AddRange(compilation.Diagnostics.Select(StyleGeneration.ToNazor));
            if (compilation.Success) Report(compilation.StyleSheet, null, styles);
        }
        if (styleSheets.Length != 0 && compiler is not null)
        {
            try
            {
                var code = Retry(() => compiler.GenerateStyleSheets(styleSheets, diagnostics), "the stylesheets", diagnostics);
                var path = Path.Combine(OutputDirectory, "NazorStyleSheets.g.cs");
                if (code is not null && WriteIfChanged(path, code, diagnostics)) generated.Add(path);
            }
            catch (NazorCompilationException exception)
            {
                diagnostics.AddRange(exception.Diagnostics);
            }
        }
        return new(generated, styles, diagnostics.Distinct().ToArray(), discoveryFailed, stopwatch.Elapsed)
        {
            Components = components.Length,
            StyleSheets = styleSheets.Length,
        };
    }

    private NazorCompiler Compiler(string[] sources)
    {
        var references = options.References.Select(Path.GetFullPath).Where(File.Exists)
            .Select(path => (Path: path, Time: File.GetLastWriteTimeUtc(path))).ToArray();
        if (compiler is { } current && options.RootNamespace == compiledRootNamespace && references.SequenceEqual(loadedReferences) &&
            sources.SequenceEqual(compiledSources, StringComparer.OrdinalIgnoreCase))
            return current;
        options = options with { References = references.Select(r => r.Path).ToArray(), CSharpSources = sources };
        var created = new NazorCompiler(options);
        compiler = created;
        compiledRootNamespace = options.RootNamespace;
        loadedReferences = references;
        compiledSources = sources;
        return created;
    }

    private void Report(StyleSheet sheet, string? scope, List<StyleUpdate> styles)
    {
        var json = StyleSheetJson.Serialize(sheet);
        if (reportedSheets.TryGetValue(sheet.Source, out var last) && last == json) return;
        reportedSheets[sheet.Source] = json;
        styles.Add(new(sheet.Source, scope, json));
    }

    private void ReloadResponseFile()
    {
        if (responseFile is null) return;
        var info = new FileInfo(responseFile);
        if (!info.Exists) return;
        var (time, length) = (info.LastWriteTimeUtc, info.Length);
        if (time <= responseFileTime && length == responseFileLength) return;
        string[] lines;
        try
        {
            lines = File.ReadAllLines(responseFile);
        }
        catch (IOException)
        {
            return;
        }
        (responseFileTime, responseFileLength) = (time, length);
        if (ParseResponseFile(lines) is not { } parsed) return;
        options = options with { RootNamespace = parsed.RootNamespace ?? options.RootNamespace, References = parsed.References, ParseOptions = parsed.ParseOptions };
        compiler = null;
        extraSources = ExtraSources(parsed.Sources);
    }

    internal static (string? RootNamespace, string? ProjectDirectory, IReadOnlyList<string> References, IReadOnlyList<string> Sources, CSharpParseOptions ParseOptions)? ParseResponseFile(IEnumerable<string> lines)
    {
        var arguments = lines.Select(l => l.Trim()).Where(l => l.Length > 0).ToArray();
        if (arguments is not ["generate", ..]) return null;
        string? rootNamespace = null, projectDirectory = null;
        var references = new List<string>();
        var sources = new List<string>();
        var parseOptions = new CSharpParseOptions(LanguageVersion.CSharp14);
        for (var i = 1; i < arguments.Length; i++)
        {
            if (!arguments[i].StartsWith("--", StringComparison.Ordinal) || i + 1 >= arguments.Length) continue;
            var value = arguments[++i];
            switch (arguments[i - 1])
            {
                case "--root-namespace": rootNamespace = value; break;
                case "--project-dir": projectDirectory = value; break;
                case "--reference": references.Add(value); break;
                case "--source": sources.Add(value); break;
                case "--language-version":
                    if (!LanguageVersionFacts.TryParse(value, out var languageVersion))
                        throw new NazorCompilationException($"Unsupported C# language version '{value}'. Update Nazor.Sdk to support a newer language version.");
                    parseOptions = parseOptions.WithLanguageVersion(languageVersion);
                    break;
                case "--define": parseOptions = parseOptions.WithPreprocessorSymbols(value.Split([',', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)); break;
            }
        }
        return (rootNamespace, projectDirectory, references, sources, parseOptions);
    }

    private string[] ExtraSources(IEnumerable<string> sources) => sources.Select(Path.GetFullPath)
        .Where(s => !ProjectFiles.IsUnder(OutputDirectory, s) && (!ProjectFiles.IsUnder(ProjectDirectory, s) || ProjectFiles.IsBuildOutput(ProjectDirectory, s)))
        .Distinct(StringComparer.OrdinalIgnoreCase).ToArray();

    private static bool Belongs(string diagnosticPath, string componentPath) =>
        string.Equals(diagnosticPath, componentPath, StringComparison.OrdinalIgnoreCase) ||
        string.Equals(diagnosticPath, componentPath + ".nss", StringComparison.OrdinalIgnoreCase);

    private static bool WriteIfChanged(string path, string content, List<NazorDiagnostic> diagnostics)
    {
        var written = Retry(() =>
        {
            if (File.Exists(path) && File.ReadAllText(path) == content) return false;
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, content);
            return true;
        }, path, diagnostics);
        return written == true;
    }

    private static T? Retry<T>(Func<T> action, string subject, List<NazorDiagnostic> diagnostics)
    {
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                return action();
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or BadImageFormatException)
            {
                if (attempt < Attempts)
                {
                    Thread.Sleep(RetryDelay);
                    continue;
                }
                diagnostics.Add(NazorDiagnostic.Error("NZC000", $"{subject}: {exception.Message}"));
                return default;
            }
        }
    }
}

internal static class ProjectFiles
{
    private static readonly EnumerationOptions Options = new() { RecurseSubdirectories = true, IgnoreInaccessible = true, MatchCasing = MatchCasing.CaseInsensitive };

    public static IEnumerable<string> Enumerate(string projectDirectory, string outputDirectory, string pattern) =>
        Directory.EnumerateFiles(projectDirectory, pattern, Options)
            .Where(f => !IsBuildOutput(projectDirectory, f) && !IsUnder(outputDirectory, f))
            .Order(StringComparer.OrdinalIgnoreCase);

    public static bool IsSource(string projectDirectory, string outputDirectory, string path)
    {
        var extension = Path.GetExtension(path);
        if (!extension.Equals(".nazor", StringComparison.OrdinalIgnoreCase) && !extension.Equals(".nss", StringComparison.OrdinalIgnoreCase) &&
            !extension.Equals(".cs", StringComparison.OrdinalIgnoreCase)) return false;
        if (path.EndsWith(".g.cs", StringComparison.OrdinalIgnoreCase)) return false;
        return IsUnder(projectDirectory, path) && !IsBuildOutput(projectDirectory, path) && !IsUnder(outputDirectory, path);
    }

    public static bool IsBuildOutput(string projectDirectory, string path)
    {
        var relative = Path.GetRelativePath(projectDirectory, path);
        var first = relative.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)[0];
        return first.Equals("bin", StringComparison.OrdinalIgnoreCase) || first.Equals("obj", StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsUnder(string directory, string path)
    {
        var root = DevChannelProtocol.NormalizeDirectory(directory) + Path.DirectorySeparatorChar;
        return Path.GetFullPath(path).StartsWith(root, StringComparison.OrdinalIgnoreCase);
    }
}
