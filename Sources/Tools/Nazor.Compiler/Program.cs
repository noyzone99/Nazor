using Nazor.Compiler;
using Nazor.Compiler.Watch;
using Microsoft.CodeAnalysis.CSharp;

try
{
    var arguments = Expand(args).ToList();
    if (arguments.Count == 0) return Usage();
    switch (arguments[0])
    {
        case "generate": return Generate(arguments);
        case "watch": return await WatchAsync(arguments);
        case "ide-refresh": return await IdeRefreshAsync(arguments);
        case "dev": return await DevCommand.RunAsync(arguments.Skip(1).ToList());
        case "verify-production": return ProductionPublishVerifier.Run(arguments.Skip(1).ToArray());
        case "component-metadata":
            if (arguments.Count != 3) return Usage();
            WriteIfChanged(arguments[1], ComponentMetadata.Generate(File.ReadAllLines(arguments[2])));
            return 0;
        case "dbus-bindings":
            if (arguments.Count != 4) return Usage();
            WriteIfChanged(arguments[3], DBusBindings.Generate(arguments[1], arguments[2]));
            return 0;
        default: return Usage();
    }
}
catch (NazorCompilationException exception)
{
    foreach (var diagnostic in exception.Diagnostics) Console.Error.WriteLine(diagnostic);
    return 1;
}
catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or BadImageFormatException)
{
    Console.Error.WriteLine($"error NZC000: {exception.Message}");
    return 1;
}

static int Generate(List<string> arguments)
{
    if (Parse(arguments, 1) is not { } parsed || parsed.Files.Count + parsed.StyleSheets.Count == 0) return Usage();
    var compiler = new NazorCompiler(parsed.Options);
    var output = Path.GetFullPath(parsed.Output);
    var generated = parsed.Files.Count == 0 ? [] : compiler.Generate(parsed.Files);
    if (parsed.StyleSheets.Count != 0)
    {
        var path = Path.Combine(output, "NazorStyleSheets.g.cs");
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var warnings = new List<NazorDiagnostic>();
        WriteIfChanged(path, compiler.GenerateStyleSheets(parsed.StyleSheets, warnings));
        foreach (var warning in warnings) Console.WriteLine(warning);
    }
    foreach (var component in generated)
    {
        foreach (var warning in component.Warnings) Console.WriteLine(warning);
        var path = Path.Combine(output, component.RelativePath + ".g.cs");
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        WriteIfChanged(path, component.CSharp);
    }
    Console.WriteLine($"nazorc: generated {generated.Count} component(s) and {parsed.StyleSheets.Count} stylesheet(s) in {output}");
    return 0;
}

static async Task<int> WatchAsync(List<string> arguments)
{
    var start = arguments.Count > 1 && arguments[1] == "generate" ? 2 : 1;
    if (Parse(arguments, start) is not { } parsed) return Usage();
    var output = Path.GetFullPath(parsed.Output);
    var projectDirectory = Path.GetFullPath(parsed.Options.ProjectDirectory ?? Directory.GetCurrentDirectory());
    var responseFile = Path.Combine(output, "nazorc.rsp");
    using var stopping = new CancellationTokenSource();
    Console.CancelKeyPress += (_, e) =>
    {
        e.Cancel = true;
        stopping.Cancel();
    };
    return await WatchCommand.RunAsync(parsed.Options, output, projectDirectory, File.Exists(responseFile) ? responseFile : null, stopping.Token);
}

static async Task<int> IdeRefreshAsync(List<string> arguments)
{
    var start = arguments.Count > 1 && arguments[1] == "generate" ? 2 : 1;
    if (Parse(arguments, start) is not { } parsed) return Usage();
    var output = Path.GetFullPath(parsed.Output);
    var project = Path.GetFullPath(parsed.Options.ProjectDirectory ?? Directory.GetCurrentDirectory());
    return await IdeRefreshCommand.RunAsync(new WatchSession(parsed.Options, output, project,
        Path.Combine(output, "nazorc.rsp")), Console.Out);
}

static Arguments? Parse(List<string> arguments, int start)
{
    string? output = null, rootNamespace = null, projectDirectory = null;
    var references = new List<string>();
    var sources = new List<string>();
    var stylesheets = new List<string>();
    var files = new List<string>();
    var parseOptions = new CSharpParseOptions(LanguageVersion.CSharp14);
    for (var i = start; i < arguments.Count; i++)
    {
        var argument = arguments[i];
        if (!argument.StartsWith("--", StringComparison.Ordinal))
        {
            files.Add(argument);
            continue;
        }
        if (i + 1 >= arguments.Count) return null;
        var value = arguments[++i];
        switch (argument)
        {
            case "--out": output = value; break;
            case "--root-namespace": rootNamespace = value; break;
            case "--project-dir": projectDirectory = value; break;
            case "--reference": references.Add(value); break;
            case "--source": sources.Add(value); break;
            case "--stylesheet": stylesheets.Add(value); break;
            case "--language-version":
                if (!LanguageVersionFacts.TryParse(value, out var languageVersion))
                    throw new NazorCompilationException($"Unsupported C# language version '{value}'. Update Nazor.Sdk to support a newer language version.");
                parseOptions = parseOptions.WithLanguageVersion(languageVersion);
                break;
            case "--define": parseOptions = parseOptions.WithPreprocessorSymbols(value.Split([',', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)); break;
            default: return null;
        }
    }
    if (output is null) return null;
    var options = new NazorCompilerOptions
    {
        RootNamespace = rootNamespace ?? "Nazor.Generated",
        ProjectDirectory = projectDirectory,
        References = references,
        CSharpSources = sources,
        ParseOptions = parseOptions,
    };
    return new Arguments(options, output, files, stylesheets);
}

static void WriteIfChanged(string path, string content)
{
    if (File.Exists(path) && File.ReadAllText(path) == content) return;
    File.WriteAllText(path, content);
}

static IEnumerable<string> Expand(IEnumerable<string> arguments)
{
    foreach (var argument in arguments)
    {
        if (argument.StartsWith('@'))
        {
            foreach (var line in File.ReadAllLines(argument[1..]))
                if (line.Trim() is { Length: > 0 } trimmed) yield return trimmed;
        }
        else yield return argument;
    }
}

static int Usage()
{
    Console.Error.WriteLine("Usage: nazorc generate --out <dir> [--root-namespace <ns>] [--project-dir <dir>] [--language-version <version>] [--define <symbols>] [--reference <dll>]... [--source <file.cs>]... [--stylesheet <file.nss>]... <file.nazor>...");
    Console.Error.WriteLine("       nazorc watch [generate] <the options of generate>");
    Console.Error.WriteLine("       nazorc dev <pid | project directory> info | dump [--styles] | render");
    Console.Error.WriteLine("       nazorc verify-production <project.assets.json> <publish-inputs.txt> [publish-directory]");
    return 2;
}

namespace Nazor.Compiler
{
    internal sealed record Arguments(NazorCompilerOptions Options, string Output, List<string> Files, List<string> StyleSheets);
}
