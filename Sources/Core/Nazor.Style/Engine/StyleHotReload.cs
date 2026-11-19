using System.Reflection;

[assembly: System.Reflection.Metadata.MetadataUpdateHandler(typeof(Nazor.Style.StyleHotReload))]

namespace Nazor.Style;

/// <summary>
/// Metadata update handler of Nazor.Style: after a .NET Hot Reload delta the scoped sheet factories of the updated
/// component types are re-run (their body may have changed) and the live sheets are replaced in place through
/// <see cref="StyleRegistry.Refresh" />. Runs on an arbitrary thread, never throws; failures are kept in
/// <see cref="LastError" /> for the host to log.
/// </summary>
public static class StyleHotReload
{
    /// <summary>The static method generated in a component class to register its scoped sheet.</summary>
    public const string RegistrationMethod = "__NazorRegisterStyleSheet";

    /// <summary>
    /// The prefix of the static class generated next to a generic component to hold its registration (a generic type
    /// cannot declare a module initializer): <c>__NazorStyles_ListView</c> for <c>ListView&lt;T&gt;</c>.
    /// </summary>
    public const string GenericRegistrationClassPrefix = "__NazorStyles_";

    /// <summary>The first failure of the last delta, or null; reset at every delta.</summary>
    public static Exception? LastError { get; private set; }

    internal static void ClearCache(Type[]? updatedTypes)
    {
        LastError = null;
        if (updatedTypes is null)
        {
            Try(() => StyleRegistry.Refresh(null));
            return;
        }
        var registered = StyleRegistry.ScopedTypes;

        foreach (var type in updatedTypes.Where(t => t is not null).Select(TopLevel).Distinct())
        {
            if (registered.Contains(type)) Try(() => StyleRegistry.Refresh([type]));

            else if (Registration(type) is { } register) Try(() => register.Invoke(null, null));
        }
    }

    [System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessage("Trimming", "IL2070", Justification = "Metadata update handlers are not invoked by trimmed/AOT runtimes; normal builds retain registration metadata.")]
    [System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessage("Trimming", "IL2075", Justification = "Metadata update handlers are not invoked by trimmed/AOT runtimes.")]
    [System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Metadata update handlers are not invoked by trimmed/AOT runtimes.")]
    private static MethodInfo? Registration(Type type)
    {
        const BindingFlags flags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
        if (!type.IsGenericTypeDefinition) return type.GetMethod(RegistrationMethod, flags, []);
        var name = type.Name;
        var tick = name.IndexOf('`');
        if (tick > 0) name = name[..tick];
        var sibling = type.Namespace is null ? GenericRegistrationClassPrefix + name : type.Namespace + "." + GenericRegistrationClassPrefix + name;
        return type.Assembly.GetType(sibling, throwOnError: false)?.GetMethod(RegistrationMethod, flags, []);
    }

    private static Type TopLevel(Type type)
    {
        while (type.IsNested) type = type.DeclaringType!;
        return type;
    }

    private static void Try(Action action)
    {
        try
        {
            action();
        }
        catch (Exception exception)
        {
            LastError ??= exception is TargetInvocationException { InnerException: { } inner } ? inner : exception;
        }
    }
}
