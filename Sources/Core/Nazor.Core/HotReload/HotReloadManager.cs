using System.Reflection.Metadata;

[assembly: MetadataUpdateHandler(typeof(Nazor.HotReloadManager))]

namespace Nazor;

/// <summary>
/// Receives .NET Hot Reload deltas (dotnet watch, Visual Studio) for the Nazor runtime: clears the reflection caches
/// and tells hosts, through <see cref="DeltaApplied" />, to re-render their windows. The runtime calls the handler
/// methods on an arbitrary thread, never the UI thread under dotnet watch; subscribers only set flags or post to a
/// dispatcher.
/// </summary>
public static class HotReloadManager
{
    /// <summary>
    /// True when the process accepts metadata updates (<c>DOTNET_MODIFIABLE_ASSEMBLIES=debug</c>, set by dotnet watch
    /// and the debugger).
    /// </summary>
    public static bool IsSupported => MetadataUpdater.IsSupported;

    /// <summary>
    /// Raised after every applied delta, once every ClearCache handler ran, on an arbitrary thread with the updated
    /// types (null when unknown: assume all). Each subscriber runs in its own try/catch; the first failure goes to
    /// <see cref="LastError" />. Subscribers must only set flags or post to a dispatcher, never wait.
    /// </summary>
    public static event Action<Type[]?>? DeltaApplied;

    /// <summary>
    /// Reports content updates from a compatible hot reload agent as assembly name, relative path, and contents.
    /// The built-in hot reload workflow does not raise this event.
    /// </summary>
    public static event Action<string, string, byte[]>? ContentUpdated;

    /// <summary>The first subscriber failure of the last delta, or null; reset at every delta.</summary>
    public static Exception? LastError { get; private set; }

    /// <summary>
    /// Clears the caches and raises <see cref="DeltaApplied" />, as the runtime does after a delta: for tests and for
    /// agents that apply deltas themselves.
    /// </summary>
    /// <param name="updatedTypes">The updated types (Type[]?).</param>
    public static void ApplyUpdate(Type[]? updatedTypes)
    {
        ClearCache(updatedTypes);
        UpdateApplication(updatedTypes);
    }

    internal static void ClearCache(Type[]? updatedTypes)
    {
        try
        {
            ParameterView.ClearCache();
        }
        catch
        {

        }
    }

    internal static void UpdateApplication(Type[]? updatedTypes)
    {
        LastError = null;
        foreach (var subscriber in DeltaApplied?.GetInvocationList() ?? [])
        {
            try
            {
                ((Action<Type[]?>)subscriber)(updatedTypes);
            }
            catch (Exception exception)
            {
                LastError ??= exception;
            }
        }
    }

    internal static void UpdateContent(string assemblyName, bool isApplicationProject, string relativePath, byte[] contents)
    {
        foreach (var subscriber in ContentUpdated?.GetInvocationList() ?? [])
        {
            try
            {
                ((Action<string, string, byte[]>)subscriber)(assemblyName, relativePath, contents);
            }
            catch (Exception exception)
            {
                LastError ??= exception;
            }
        }
    }
}
