using System.Reflection;
using System.Runtime.Loader;
using Microsoft.CodeAnalysis;

namespace Nazor.LanguageServer.Workspace;

internal sealed class AnalyzerLoader : IAnalyzerAssemblyLoader
{
    private readonly Dictionary<string, AssemblyLoadContext> contexts = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> dependencies = new(StringComparer.OrdinalIgnoreCase);

    public void AddDependencyLocation(string fullPath) => dependencies.Add(fullPath);

    public Assembly LoadFromPath(string fullPath)
    {
        if (!contexts.TryGetValue(fullPath, out var context))
        {
            context = new AssemblyLoadContext("Nazor analyzer: " + fullPath, isCollectible: true);
            context.Resolving += (_, name) =>
            {
                var shared = AssemblyLoadContext.Default.Assemblies.FirstOrDefault(a => a.GetName().Name == name.Name);
                if (shared is not null) return shared;
                var sibling = Path.Combine(Path.GetDirectoryName(fullPath)!, name.Name + ".dll");
                var dependency = File.Exists(sibling) ? sibling : dependencies.FirstOrDefault(p => Path.GetFileNameWithoutExtension(p) == name.Name);
                return dependency is null ? null : context.LoadFromAssemblyPath(dependency);
            };
            contexts[fullPath] = context;
        }
        return context.LoadFromAssemblyPath(fullPath);
    }
}
