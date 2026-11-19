using Microsoft.CodeAnalysis.CSharp;

namespace Nazor.LanguageServer.Workspace;

internal static class RoslynConfiguration
{
    public static string? FileFor(ProjectContext context) => context.ResponseFile is null ? null : Path.ChangeExtension(context.ResponseFile, ".roslyn.rsp");

    public static CSharpCommandLineArguments Read(ProjectContext context) => CSharpCommandLineParser.Default.Parse(
        FileFor(context) is { } path && File.Exists(path) ? File.ReadAllLines(path) :
            [$"/langversion:{context.ParseOptions.SpecifiedLanguageVersion.ToDisplayString()}", $"/define:{string.Join(',', context.ParseOptions.PreprocessorSymbolNames)}", "/nullable:enable", "/unsafe+"],
        context.Directory, sdkDirectory: null);

    public static IEnumerable<string> Configs(ProjectContext context, CSharpCommandLineArguments arguments)
    {
        foreach (var path in arguments.AnalyzerConfigPaths) yield return path;
        for (var directory = context.Directory; directory is not null; directory = Path.GetDirectoryName(directory))
            if (File.Exists(Path.Combine(directory, ".editorconfig"))) yield return Path.Combine(directory, ".editorconfig");
    }
}
