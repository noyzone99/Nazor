using System.IO;
using System.Reflection;
using Microsoft.CodeAnalysis;

namespace Nazor.VisualStudio
{
    internal static class NazorRoslynAssemblies
    {
        // Partial assembly names do not probe Visual Studio's language-service directory.
        internal static Assembly Load(string name) => Assembly.LoadFrom(Path.Combine(
            Path.GetDirectoryName(typeof(Workspace).Assembly.Location), name + ".dll"));
    }
}
