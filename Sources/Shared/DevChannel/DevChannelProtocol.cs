using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Nazor.Development;

internal static class DevChannelProtocol
{
    public const int ProtocolVersion = 1;

    public const string EnableVariable = "NAZOR_DEVCHANNEL";

    public const string DirectoryVariable = "NAZOR_DEV_DIRECTORY";

    public const string ProjectDirectoryMetadata = "Nazor.ProjectDirectory";

    public const string Initialize = "initialize";
    public const string StylesUpdate = "styles/update";
    public const string AppRender = "app/render";
    public const string AppInfo = "app/info";
    public const string AppDump = "app/dump";

    public const string AppLog = "app/log";

    public static string PipeName(int pid) => "nazor-dev-" + pid.ToString(CultureInfo.InvariantCulture);

    public static string DiscoveryDirectory =>
        Environment.GetEnvironmentVariable(DirectoryVariable) is { Length: > 0 } directory
            ? Path.GetFullPath(directory)
            : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData, Environment.SpecialFolderOption.Create), "Nazor", "Dev");

    public static string NazorVersion
    {
        get
        {
            var assembly = typeof(DevChannelProtocol).Assembly;
            var informational = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
            if (informational is { Length: > 0 })
            {
                var plus = informational.IndexOf('+');
                return plus < 0 ? informational : informational[..plus];
            }
            return assembly.GetName().Version?.ToString() ?? "0.0.0";
        }
    }

    public static string NormalizeDirectory(string directory) =>
        Path.GetFullPath(directory).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

    public static bool SameDirectory(string a, string b) =>
        string.Equals(NormalizeDirectory(a), NormalizeDirectory(b), StringComparison.OrdinalIgnoreCase);
}

internal sealed record DevChannelDescriptor(int Pid, DateTime StartTimeUtc, string Pipe, string[] ProjectDirectories, string EntryAssembly, string NazorVersion, int ProtocolVersion)
{
    public static DevChannelDescriptor ForCurrentProcess(string[] projectDirectories) => new(
        Environment.ProcessId,
        CurrentProcessStartTimeUtc(),
        DevChannelProtocol.PipeName(Environment.ProcessId),
        projectDirectories,
        Assembly.GetEntryAssembly()?.GetName().Name ?? "",
        DevChannelProtocol.NazorVersion,
        DevChannelProtocol.ProtocolVersion);

    public static string PathIn(string directory, int pid) => Path.Combine(directory, pid.ToString(CultureInfo.InvariantCulture) + ".json");

    public string Save(string directory)
    {
        Directory.CreateDirectory(directory);
        var path = PathIn(directory, Pid);
        var json = new JsonObject
        {
            ["pid"] = Pid,
            ["startTimeUtc"] = StartTimeUtc.ToUniversalTime().ToString("o", CultureInfo.InvariantCulture),
            ["pipe"] = Pipe,
            ["projectDirectories"] = new JsonArray(ProjectDirectories.Select(d => (JsonNode)d).ToArray()),
            ["entryAssembly"] = EntryAssembly,
            ["nazorVersion"] = NazorVersion,
            ["protocolVersion"] = ProtocolVersion,
        };
        var temporary = path + "." + Environment.ProcessId.ToString(CultureInfo.InvariantCulture) + ".tmp";
        File.WriteAllText(temporary, json.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
        File.Move(temporary, path, overwrite: true);
        return path;
    }

    public static DevChannelDescriptor Load(string path)
    {
        JsonObject json;
        try
        {
            json = JsonNode.Parse(File.ReadAllText(path)) as JsonObject ?? throw new InvalidDataException($"{path}: a descriptor must be a JSON object.");
        }
        catch (JsonException exception)
        {
            throw new InvalidDataException($"{path}: {exception.Message}", exception);
        }
        try
        {
            return new DevChannelDescriptor(
                json["pid"]!.GetValue<int>(),
                DateTime.Parse(json["startTimeUtc"]!.GetValue<string>(), CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal),
                json["pipe"]!.GetValue<string>(),
                json["projectDirectories"] is JsonArray directories ? directories.Select(d => d!.GetValue<string>()).ToArray() : [],
                json["entryAssembly"]?.GetValue<string>() ?? "",
                json["nazorVersion"]?.GetValue<string>() ?? "",
                json["protocolVersion"]?.GetValue<int>() ?? 0);
        }
        catch (Exception exception) when (exception is NullReferenceException or InvalidOperationException or FormatException)
        {
            throw new InvalidDataException($"{path}: a descriptor needs pid, startTimeUtc and pipe.", exception);
        }
    }

    public bool IsAlive
    {
        get
        {
            try
            {
                using var process = Process.GetProcessById(Pid);
                if (process.HasExited) return false;
                return Math.Abs((process.StartTime.ToUniversalTime() - StartTimeUtc.ToUniversalTime()).TotalSeconds) <= 2;
            }
            catch (Exception exception) when (exception is ArgumentException or InvalidOperationException or System.ComponentModel.Win32Exception)
            {
                return false;
            }
        }
    }

    public bool ServesProject(string projectDirectory) =>
        ProjectDirectories.Any(d => DevChannelProtocol.SameDirectory(d, projectDirectory));

    private static DateTime CurrentProcessStartTimeUtc()
    {
        try
        {
            using var process = Process.GetCurrentProcess();
            return process.StartTime.ToUniversalTime();
        }
        catch (Exception exception) when (exception is InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            return DateTime.UtcNow;
        }
    }
}
