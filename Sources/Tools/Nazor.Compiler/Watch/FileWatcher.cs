namespace Nazor.Compiler.Watch;

internal sealed class FileWatcher : IDisposable
{
    private readonly string projectDirectory;
    private readonly string outputDirectory;
    private readonly Action<string?> changed;
    private readonly FileSystemWatcher watcher;

    public FileWatcher(string projectDirectory, string outputDirectory, Action<string?> changed)
    {
        ArgumentNullException.ThrowIfNull(changed);
        this.projectDirectory = Path.GetFullPath(projectDirectory);
        this.outputDirectory = Path.GetFullPath(outputDirectory);
        this.changed = changed;
        watcher = new FileSystemWatcher(this.projectDirectory)
        {
            IncludeSubdirectories = true,
            NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName | NotifyFilters.LastWrite | NotifyFilters.Size | NotifyFilters.CreationTime,
            InternalBufferSize = 64 * 1024,
        };
        watcher.Filters.Add("*.nazor");
        watcher.Filters.Add("*.nss");
        watcher.Filters.Add("*.cs");
        watcher.Created += OnChanged;
        watcher.Changed += OnChanged;
        watcher.Deleted += OnChanged;
        watcher.Renamed += OnRenamed;
        watcher.Error += (_, _) => this.changed(null);
    }

    public void Start() => watcher.EnableRaisingEvents = true;

    private void OnChanged(object sender, FileSystemEventArgs e)
    {
        if (ProjectFiles.IsSource(projectDirectory, outputDirectory, e.FullPath)) changed(e.FullPath);
    }

    private void OnRenamed(object sender, RenamedEventArgs e)
    {
        if (ProjectFiles.IsSource(projectDirectory, outputDirectory, e.FullPath) || ProjectFiles.IsSource(projectDirectory, outputDirectory, e.OldFullPath))
            changed(e.FullPath);
    }

    public void Dispose() => watcher.Dispose();
}
