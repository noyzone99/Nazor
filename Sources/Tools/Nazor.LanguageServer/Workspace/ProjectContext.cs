using System.Xml.Linq;
using Microsoft.CodeAnalysis.CSharp;

namespace Nazor.LanguageServer.Workspace;

internal sealed record ProjectContext(
    string Directory,
    string? ProjectFile,
    string RootNamespace,
    IReadOnlyList<string> References,
    IReadOnlyList<string> CSharpSources,
    IReadOnlyList<string> Components,
    IReadOnlyList<string> StyleSheets,
    string? ResponseFile,
    DateTime ResponseFileTime)
{
    public CSharpParseOptions ParseOptions { get; init; } = new(LanguageVersion.CSharp14);

    private static readonly string[] Fallback =
    [
        typeof(Nazor.ComponentBase).Assembly.Location,
        typeof(Nazor.Controls.Button).Assembly.Location,
        typeof(Nazor.Style.StyleSheet).Assembly.Location,
    ];

    public static ProjectContext ForDirectory(string directory) =>
        Load(directory, System.IO.Directory.EnumerateFiles(directory, "*.csproj").Order(StringComparer.OrdinalIgnoreCase).FirstOrDefault());

    public static ProjectContext ForDocument(string path)
    {
        for (var folder = Path.GetDirectoryName(Path.GetFullPath(path)); folder is not null; folder = Path.GetDirectoryName(folder))
        {
            var project = System.IO.Directory.EnumerateFiles(folder, "*.csproj").Order(StringComparer.OrdinalIgnoreCase).FirstOrDefault();
            if (project is not null) return Load(folder, project);
        }
        return Load(Path.GetDirectoryName(Path.GetFullPath(path))!, null);
    }

    public static (string? Path, DateTime Time) LatestResponseFile(string directory)
    {
        var obj = Path.Combine(directory, "obj");
        if (!System.IO.Directory.Exists(obj)) return (null, default);
        // A successful build touches its stamp even when the compiler arguments stay identical.
        var newest = System.IO.Directory.EnumerateFiles(obj, "nazorc.rsp", SearchOption.AllDirectories)
            .Select(f => new FileInfo(f))
            .OrderByDescending(f => Math.Max(f.LastWriteTimeUtc.Ticks, File.GetLastWriteTimeUtc(Path.Combine(f.DirectoryName!, "nazor.stamp")).Ticks))
            .ThenByDescending(f => f.LastWriteTimeUtc).FirstOrDefault();
        return newest is null ? (null, default) : (newest.FullName, newest.LastWriteTimeUtc);
    }

    private static ProjectContext Load(string directory, string? projectFile)
    {
        var (responseFile, time) = LatestResponseFile(directory);
        if (responseFile is not null && Parse(directory, projectFile, responseFile, time) is { } context) return context;
        var rootNamespace = projectFile is null ? Path.GetFileName(directory) : RootNamespaceOf(projectFile);
        var recursive = projectFile is not null;
        var styleSheets = SourceFiles(directory, "*.nss", recursive).Where(f => !f.EndsWith(".nazor.nss", StringComparison.OrdinalIgnoreCase)).ToArray();
        return new(directory, projectFile, rootNamespace, Fallback, SourceFiles(directory, "*.cs", recursive), SourceFiles(directory, "*.nazor", recursive),
            styleSheets, null, default) { ParseOptions = ParseOptionsOf(projectFile) };
    }

    private static ProjectContext? Parse(string directory, string? projectFile, string responseFile, DateTime time)
    {
        string[] lines;
        try
        {
            lines = File.ReadAllLines(responseFile).Select(l => l.Trim()).Where(l => l.Length > 0).ToArray();
        }
        catch (IOException)
        {
            return null;
        }
        if (lines is not ["generate", ..]) return null;
        string? rootNamespace = null, projectDirectory = null;
        var references = new List<string>();
        var sources = new List<string>();
        var components = new List<string>();
        var styleSheets = new List<string>();
        var parseOptions = ParseOptionsOf(projectFile);
        for (var i = 1; i < lines.Length; i++)
        {
            if (!lines[i].StartsWith("--", StringComparison.Ordinal))
            {
                components.Add(lines[i]);
                continue;
            }
            if (i + 1 >= lines.Length) break;
            var value = lines[++i];
            switch (lines[i - 1])
            {
                case "--root-namespace": rootNamespace = value; break;
                case "--project-dir": projectDirectory = value; break;
                case "--reference": references.Add(value); break;
                case "--source": sources.Add(value); break;
                case "--stylesheet": styleSheets.Add(value); break;
                case "--language-version":
                    if (LanguageVersionFacts.TryParse(value, out var languageVersion)) parseOptions = parseOptions.WithLanguageVersion(languageVersion);
                    break;
                case "--define": parseOptions = parseOptions.WithPreprocessorSymbols(value.Split([',', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)); break;
            }
        }
        sources = [.. sources.Where(File.Exists)];
        components = [.. components.Where(File.Exists)];
        return new(Path.GetFullPath(projectDirectory ?? directory), projectFile, rootNamespace ?? Path.GetFileName(directory),
            references.Where(File.Exists).ToArray(), sources, components, [.. styleSheets.Where(File.Exists)], responseFile, time) { ParseOptions = parseOptions };
    }

    private static CSharpParseOptions ParseOptionsOf(string? projectFile)
    {
        var result = new CSharpParseOptions(LanguageVersion.CSharp14);
        if (projectFile is null) return result;
        try
        {
            var project = XDocument.Load(projectFile);
            string? Property(string name) => project.Descendants().FirstOrDefault(e => e.Name.LocalName == name)?.Value.Trim();
            var target = (Property("TargetFramework") ?? Property("TargetFrameworks"))?.Split(';', '-')[0];
            var language = Property("LangVersion") ?? target switch { "net8.0" => "12.0", "net9.0" => "13.0", "net11.0" => "15.0", _ => "14.0" };
            if (LanguageVersionFacts.TryParse(language, out var version)) result = result.WithLanguageVersion(version);
            if (Property("DefineConstants") is { } symbols && !symbols.Contains('$'))
                result = result.WithPreprocessorSymbols(symbols.Split([',', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
        }
        catch (Exception exception) when (exception is IOException or System.Xml.XmlException) { }
        return result;
    }

    private static string RootNamespaceOf(string projectFile)
    {
        try
        {
            var value = XDocument.Load(projectFile).Descendants().FirstOrDefault(e => e.Name.LocalName == "RootNamespace")?.Value;
            if (!string.IsNullOrWhiteSpace(value) && !value.Contains('$')) return value.Trim();
        }
        catch (Exception exception) when (exception is IOException or System.Xml.XmlException)
        {
        }
        return Path.GetFileNameWithoutExtension(projectFile);
    }

    private static string[] SourceFiles(string directory, string pattern, bool recursive) => System.IO.Directory
        .EnumerateFiles(directory, pattern, recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly)
        .Where(f => !IsBuildOutput(directory, f)).Order(StringComparer.OrdinalIgnoreCase).ToArray();

    public static bool IsBuildOutput(string directory, string path)
    {
        var relative = Path.GetRelativePath(directory, path);
        var first = relative.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)[0];
        return first.Equals("bin", StringComparison.OrdinalIgnoreCase) || first.Equals("obj", StringComparison.OrdinalIgnoreCase);
    }
}
