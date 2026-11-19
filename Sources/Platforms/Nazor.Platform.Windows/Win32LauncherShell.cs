using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using System.Text;
using static Nazor.Platform.Windows.Win32;

namespace Nazor.Platform.Windows;

internal sealed partial class Win32LauncherShell : IDisposable
{
    private static readonly StrategyBasedComWrappers Wrappers = new();
    private static readonly Guid DestinationList = new("77f10cf0-3db5-4966-b520-b7c54fd35ed6");
    private static readonly Guid ObjectCollection = new("2d3468c1-36a7-43b6-ac24-d3f02fd9607a");
    private static readonly Guid ShellLink = new("00021401-0000-0000-c000-000000000046");
    private static readonly Guid TaskbarList = new("56fdf344-fd6d-11d0-958a-006097c9a090");
    private readonly bool uninitialize;
    private ITaskbarList3? taskbar;
    private bool disposed;

    internal Win32LauncherShell()
    {
        var result = CoInitializeEx(0, 2);
        if (result != unchecked((int)0x80010106)) Marshal.ThrowExceptionForHR(result);
        uninitialize = result >= 0;
    }

    internal bool SetOverlay(nint window, nint icon, string description)
    {
        try
        {
            if (taskbar is null)
            {
                taskbar = Create<ITaskbarList3>(TaskbarList, out var pointer);
                Marshal.Release(pointer);
                taskbar.HrInit();
            }
            return taskbar.SetOverlayIcon(window, icon, description) >= 0;
        }
        catch (Exception error) when (error is COMException or UnauthorizedAccessException)
        {
            Console.Error.WriteLine($"Nazor: taskbar overlay failed: {error.Message}");
            ResetTaskbar();
            return false;
        }
    }

    internal bool SetJumpList(IReadOnlyList<JumpListTask> tasks, string? appId = null)
    {
        var executable = Environment.ProcessPath;
        if (string.IsNullOrEmpty(executable)) return false;
        var prefix = Path.GetFileNameWithoutExtension(executable).Equals("dotnet", StringComparison.OrdinalIgnoreCase)
            ? new[] { Path.GetFullPath(Environment.GetCommandLineArgs()[0]) } : [];
        var arguments = tasks.Select(task => FormatArguments(prefix.Concat(task.Arguments))).ToArray();
        if (arguments.Any(value => value.Length + executable.Length + 4 > 32767))
            throw new ArgumentException("Launcher command lines cannot exceed 32,767 UTF-16 units.", nameof(tasks));

        ICustomDestinationList? list = null;
        IObjectCollection? collection = null;
        nint collectionPointer = 0;
        var begun = false;
        try
        {
            list = Create<ICustomDestinationList>(DestinationList, out var pointer);
            Marshal.Release(pointer);
            appId ??= CurrentAppId();
            if (appId is not null) list.SetAppID(appId);
            list.BeginList(out _, typeof(IObjectArray).GUID, out var removed);
            begun = true;
            if (removed != 0) Marshal.Release(removed);
            collection = Create<IObjectCollection>(ObjectCollection, out collectionPointer);
            for (var index = 0; index < tasks.Count; index++)
            {
                var link = Create<IShellLinkW>(ShellLink, out var linkPointer);
                try
                {
                    link.SetPath(executable);
                    link.SetArguments(arguments[index]);
                    link.SetWorkingDirectory(AppContext.BaseDirectory);
                    link.SetIconLocation(executable, 0);
                    var propertyId = typeof(ILauncherPropertyStore).GUID;
                    Marshal.ThrowExceptionForHR(Marshal.QueryInterface(linkPointer, in propertyId, out var propertiesPointer));
                    var properties = Wrap<ILauncherPropertyStore>(propertiesPointer);
                    Marshal.Release(propertiesPointer);
                    var value = new LauncherPropertyValue { Type = 31, Text = Marshal.StringToCoTaskMemUni(tasks[index].Title) };
                    try
                    {
                        properties.SetValue(new LauncherPropertyKey { Format = new("f29f85e0-4ff9-1068-ab91-08002b27b3d9"), Id = 2 }, value);
                        properties.Commit();
                    }
                    finally { Marshal.FreeCoTaskMem(value.Text); Release(properties); }
                    collection.AddObject(linkPointer);
                }
                finally { Marshal.Release(linkPointer); Release(link); }
            }
            if (tasks.Count != 0) list.AddUserTasks(collectionPointer);
            list.CommitList();
            begun = false;
            return true;
        }
        catch (Exception error) when (error is COMException or UnauthorizedAccessException)
        {
            Console.Error.WriteLine($"Nazor: Jump List publication failed: {error.Message}");
            return false;
        }
        finally
        {
            if (begun && list is not null)
                try { list.AbortList(); } catch (Exception error) when (error is COMException or UnauthorizedAccessException) { }
            if (collectionPointer != 0) Marshal.Release(collectionPointer);
            Release(collection);
            Release(list);
        }
    }

    internal static string FormatArguments(IEnumerable<string> arguments) => string.Join(' ', arguments.Select(QuoteArgument));
    private static string QuoteArgument(string argument)
    {
        var result = new StringBuilder("\"");
        var slashes = 0;
        foreach (var character in argument)
        {
            if (character == '\\') { slashes++; continue; }
            if (character == '"') result.Append('\\', slashes * 2 + 1);
            else result.Append('\\', slashes);
            result.Append(character);
            slashes = 0;
        }
        return result.Append('\\', slashes * 2).Append('"').ToString();
    }

    private static T Create<T>(Guid clsid, out nint pointer) where T : class
    {
        Marshal.ThrowExceptionForHR(CoCreateInstance(clsid, 0, CLSCTX_INPROC_SERVER, typeof(T).GUID, out pointer));
        try { return Wrap<T>(pointer); }
        catch { Marshal.Release(pointer); pointer = 0; throw; }
    }
    private static T Wrap<T>(nint pointer) where T : class => (T)Wrappers.GetOrCreateObjectForComInstance(pointer, CreateObjectFlags.UniqueInstance);
    private static string? CurrentAppId()
    {
        if (GetCurrentProcessExplicitAppUserModelID(out var pointer) < 0) return null;
        try { return Marshal.PtrToStringUni(pointer); }
        finally { Marshal.FreeCoTaskMem(pointer); }
    }
    private static void Release(object? value) { if (value is not null) ((ComObject)value).FinalRelease(); }
    internal void ResetTaskbar() { Release(taskbar); taskbar = null; }
    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        ResetTaskbar();
        if (uninitialize) CoUninitialize();
    }
    [LibraryImport("ole32.dll")] private static partial int CoInitializeEx(nint reserved, uint model);
    [LibraryImport("ole32.dll")] private static partial void CoUninitialize();
    [LibraryImport("shell32.dll")] private static partial int GetCurrentProcessExplicitAppUserModelID(out nint appId);
}
