using Nazor.LanguageServer;
using Nazor.LanguageServer.Features;

if (args is ["--completion-images", var directory])
{
    NssCompletionImages.Write(directory);
    return 0;
}

var server = new NazorLanguageServer(Console.OpenStandardInput(), Console.OpenStandardOutput(), OpenLog());
return await server.RunAsync();

static TextWriter OpenLog()
{
    try
    {
        var folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Nazor", "LanguageServer");
        Directory.CreateDirectory(folder);
        foreach (var old in new DirectoryInfo(folder).EnumerateFiles("nazor-ls-*.log").Where(f => f.LastWriteTimeUtc < DateTime.UtcNow.AddDays(-7)))
            old.Delete();
        var writer = new StreamWriter(Path.Combine(folder, $"nazor-ls-{Environment.ProcessId}.log")) { AutoFlush = true };
        return TextWriter.Synchronized(writer);
    }
    catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
    {
        return Console.Error;
    }
}
