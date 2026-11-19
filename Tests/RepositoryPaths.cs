using System.Runtime.CompilerServices;

namespace Nazor.Testing;

internal static class RepositoryPaths
{
    internal static string Root { get; } = Find();

    private static string Find([CallerFilePath] string source = "")
    {
        foreach (var start in new[] { Path.GetDirectoryName(source), Environment.CurrentDirectory, AppContext.BaseDirectory })
            for (var directory = string.IsNullOrEmpty(start) ? null : new DirectoryInfo(start); directory is not null; directory = directory.Parent)
                if (File.Exists(Path.Combine(directory.FullName, "Nazor.slnx"))) return directory.FullName;
        throw new DirectoryNotFoundException("These tests require a Nazor repository checkout.");
    }
}
