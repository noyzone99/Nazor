using System.Reflection;

namespace Nazor.Platform;

internal static class ApplicationIdentity
{
    internal static string? Id { get; } = Assembly.GetEntryAssembly()?.GetCustomAttributes<AssemblyMetadataAttribute>()
        .SingleOrDefault(attribute => attribute.Key == "Nazor.ApplicationId")?.Value;
}
