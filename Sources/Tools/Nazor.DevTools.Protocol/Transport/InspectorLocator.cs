namespace Nazor.DevTools.Protocol;

public static class InspectorLocator
{
    public static string? Find(string applicationDirectory)
    {
        var packaged = In(Path.Combine(Path.GetFullPath(applicationDirectory), "Nazor.DevTools"));
        if (packaged is not null) return packaged;
        for (var directory = new DirectoryInfo(applicationDirectory); directory is not null; directory = directory.Parent)
        {
            if (!File.Exists(Path.Combine(directory.FullName, "Nazor.slnx"))) continue;
            return In(Path.Combine(directory.FullName, "artifacts", OperatingSystem.IsLinux() ? "devtools-host-linux" : "devtools-host", "bin", "Nazor.DevTools", "release"))
                ?? In(Path.Combine(directory.FullName, "Sources", "Tools", "Nazor.DevTools", "bin", "Release", "net10.0"));
        }
        return null;
    }
    private static string? In(string directory) => new[] { OperatingSystem.IsWindows() ? "Nazor.DevTools.exe" : "Nazor.DevTools", "Nazor.DevTools.dll" }
        .Select(name => Path.Combine(directory, name)).FirstOrDefault(File.Exists);
}
