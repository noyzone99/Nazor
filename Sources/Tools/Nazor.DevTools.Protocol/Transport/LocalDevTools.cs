using System.Diagnostics;
using System.IO.Pipes;
using System.Security.AccessControl;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;
using System.Text.Json;

namespace Nazor.DevTools.Protocol;

public sealed record LocalDevToolsDescriptor(int Version, TargetIdentity Identity, string ProcessStartUtc, string PipeName, string ProcessStartToken)
{
    public string? ApplicationName { get; init; }
    public string? InspectorPath { get; init; }
    public string? InspectorVersion { get; init; }
}

public static class LocalDevTools
{
    private const UnixFileMode DirectoryMode = UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute;
    private const UnixFileMode FileMode = UnixFileMode.UserRead | UnixFileMode.UserWrite;
    private static readonly JsonSerializerOptions Json = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, MaxDepth = 8 };
    public static string DiscoveryDirectory => OperatingSystem.IsWindows()
        ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Nazor", "DevTools")
        : Path.Combine(Path.GetTempPath(), "nazor-dt-" + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(Environment.UserName)))[..12]);

    public static void PrepareDirectory(string directory)
    {
        directory = Path.GetFullPath(directory);
        if (Directory.Exists(directory)) { CheckPrivateDirectory(directory); return; }
        if (OperatingSystem.IsWindows())
        {
            var security = new DirectorySecurity();
            security.SetAccessRuleProtection(true, false);
            security.AddAccessRule(new FileSystemAccessRule(WindowsIdentity.GetCurrent().User!, FileSystemRights.FullControl,
                InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit, PropagationFlags.None, AccessControlType.Allow));
            new DirectoryInfo(directory).Create(security);
        }
        else Directory.CreateDirectory(directory, DirectoryMode);
        CheckPrivateDirectory(directory);
    }

    private static void CheckPrivateDirectory(string directory)
    {
        if ((File.GetAttributes(directory) & FileAttributes.ReparsePoint) != 0) throw new UnauthorizedAccessException("Discovery directories cannot be links.");
        if (OperatingSystem.IsWindows())
        {
            var security = new DirectoryInfo(directory).GetAccessControl();
            var user = WindowsIdentity.GetCurrent().User!;
            foreach (FileSystemAccessRule rule in security.GetAccessRules(true, true, typeof(SecurityIdentifier)))
                if (rule.AccessControlType == AccessControlType.Allow && !rule.IdentityReference.Equals(user) &&
                    !rule.IdentityReference.Equals(new SecurityIdentifier(WellKnownSidType.LocalSystemSid, null)) &&
                    !rule.IdentityReference.Equals(new SecurityIdentifier(WellKnownSidType.BuiltinAdministratorsSid, null)))
                    throw new UnauthorizedAccessException("Discovery storage must be private to the current user.");
        }
        else if ((File.GetUnixFileMode(directory) & ~DirectoryMode) != 0)
            throw new UnauthorizedAccessException("Discovery storage must have mode 0700.");
    }

    public static string PipeName(string directory, string sessionId)
    {
        if (!Guid.TryParseExact(sessionId, "N", out _)) throw new ArgumentException("Expected an attachment session ID.", nameof(sessionId));
        var name = "nazor-dt-" + sessionId;
        if (OperatingSystem.IsWindows()) return name;
        var path = Path.Combine(Path.GetFullPath(directory), sessionId + ".sock");
        if (Encoding.UTF8.GetByteCount(path) > 100) throw new PathTooLongException("Choose a shorter private discovery directory for Unix sockets.");
        return path;
    }

    public static string Save(string directory, LocalDevToolsDescriptor descriptor)
    {
        PrepareDirectory(directory);
        Validate(directory, descriptor);
        var path = Path.Combine(directory, descriptor.Identity.SessionId + ".json");
        var temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            var options = new FileStreamOptions { Mode = System.IO.FileMode.CreateNew, Access = FileAccess.Write, Share = FileShare.None };
            if (!OperatingSystem.IsWindows()) options.UnixCreateMode = FileMode;
            using (var stream = new FileStream(temporary, options)) JsonSerializer.Serialize(stream, descriptor, Json);
            File.Move(temporary, path, false);
            return path;
        }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }

    public static IReadOnlyList<LocalDevToolsDescriptor> Discover(string? directory = null)
    {
        directory = Path.GetFullPath(directory ?? DiscoveryDirectory);
        if (!Directory.Exists(directory)) return [];
        CheckPrivateDirectory(directory);
        var result = new List<LocalDevToolsDescriptor>();
        var files = Directory.EnumerateFiles(directory, "*.json").Take(257).ToArray();
        if (files.Length > 256) throw new DevToolsException(DevToolsErrors.Unavailable, "Discovery exceeds the descriptor budget.");
        foreach (var file in files)
        {
            try
            {
                if ((File.GetAttributes(file) & FileAttributes.ReparsePoint) != 0 || new FileInfo(file).Length > 16384) continue;
                if (!OperatingSystem.IsWindows() && (File.GetUnixFileMode(file) & ~FileMode) != 0) continue;
                using var stream = File.OpenRead(file);
                var bytes = new byte[16385];
                var length = stream.ReadAtLeast(bytes, bytes.Length, false);
                if (length > 16384) continue;
                var descriptor = JsonSerializer.Deserialize<LocalDevToolsDescriptor>(bytes.AsSpan(0, length), Json);
                if (descriptor is null) continue;
                Validate(directory, descriptor);
                if (Path.GetFileName(file) == descriptor.Identity.SessionId + ".json" && IsAlive(descriptor)) result.Add(descriptor);
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or JsonException or ArgumentException or DevToolsException) { }
        }
        return result;
    }

    public static bool IsAlive(LocalDevToolsDescriptor descriptor)
    {
        if (descriptor?.Identity is not { ProcessId: > 0 }) return false;
        try
        {
            using var process = Process.GetProcessById(descriptor.Identity.ProcessId);
            return !process.HasExited && ProcessStartToken(descriptor.Identity.ProcessId) == descriptor.ProcessStartToken;
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException or IOException or UnauthorizedAccessException or System.ComponentModel.Win32Exception) { return false; }
    }

    public static string ProcessStartToken(int processId)
    {
        if (OperatingSystem.IsLinux())
        {
            var stat = File.ReadAllText("/proc/" + processId.ToString(System.Globalization.CultureInfo.InvariantCulture) + "/stat");
            var end = stat.LastIndexOf(')');
            var fields = end >= 0 ? stat[(end + 1)..].Split(' ', StringSplitOptions.RemoveEmptyEntries) : [];
            if (fields.Length <= 19 || !ulong.TryParse(fields[19], System.Globalization.CultureInfo.InvariantCulture, out _))
                throw new InvalidDataException("The process start marker is unavailable.");
            var boot = File.ReadAllText("/proc/sys/kernel/random/boot_id").Trim();
            if (!Guid.TryParse(boot, out _)) throw new InvalidDataException("The boot identity is unavailable.");
            return "linux:" + boot + ":" + fields[19];
        }
        using var process = Process.GetProcessById(processId);
        return process.StartTime.ToUniversalTime().Ticks.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }

    public static async Task<DevToolsPeer> ConnectAsync(LocalDevToolsDescriptor descriptor, string clientName,
        string[] capabilities, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        Validate(OperatingSystem.IsWindows() ? DiscoveryDirectory : Path.GetDirectoryName(descriptor.PipeName) ?? DiscoveryDirectory, descriptor);
        if (!IsAlive(descriptor)) throw new DevToolsException(DevToolsErrors.StaleSession, "The discovered process instance exited.");
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(3));
        while (true)
        {
            timeout.Token.ThrowIfCancellationRequested();
            var stream = new NamedPipeClientStream(".", descriptor.PipeName, PipeDirection.InOut, PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);
            DevToolsPeer? peer = null;
            try
            {
                await stream.ConnectAsync(timeout.Token).ConfigureAwait(false);
                peer = new(stream, stream, clientName, capabilities);
                var initialized = await peer.InitializeAsync(timeout.Token).ConfigureAwait(false);
                if (initialized.Target?.Identity != descriptor.Identity || !IsAlive(descriptor))
                    throw new DevToolsException(DevToolsErrors.StaleSession, "Discovery and connected target identities differ.");
                return peer;
            }
            catch (Exception exception) when (exception is IOException || exception is DevToolsException { Code: DevToolsErrors.ConnectionClosed })
            {
                peer?.Dispose(); stream.Dispose();
                await Task.Delay(25, timeout.Token).ConfigureAwait(false);
            }
            catch { peer?.Dispose(); stream.Dispose(); throw; }
        }
    }

    private static void Validate(string directory, LocalDevToolsDescriptor descriptor)
    {
        if (descriptor.Version != 1 || descriptor.Identity is null || descriptor.PipeName != PipeName(directory, descriptor.Identity.SessionId) ||
            !DevToolsJson.TryUtc(descriptor.ProcessStartUtc, out _) || string.IsNullOrWhiteSpace(descriptor.ProcessStartToken) || descriptor.ProcessStartToken.Length > 128) throw new InvalidDataException("Invalid discovery descriptor.");
        if (descriptor.ApplicationName is { } name && (name.Length > 256 || name.Any(char.IsControl))) throw new InvalidDataException("Invalid application name.");
        DevToolsJson.Write(new TargetState(descriptor.Identity, 1, "running", null));
    }
}
