using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Nazor.VisualStudio.CodeLens.Tests;

internal static class RoslynStartupChecks
{
    internal static void Check(string extensionPath, string ide)
    {
        var directory = Path.Combine(ide, "CommonExtensions", "Microsoft", "VBCSharp", "LanguageServices");
        var channelsPath = Program.FindChannelsAssembly(ide);
        ResolveEventHandler resolve = (_, args) =>
        {
            var name = new AssemblyName(args.Name);
            if (name.Name.EndsWith(".resources", StringComparison.Ordinal)) return null;
            // Model VS binding policy, without the old test-only rescue of partial Roslyn loads.
            if (name.Name.StartsWith("Microsoft.CodeAnalysis", StringComparison.Ordinal) && name.Version == null)
                throw new Exception("Partial Roslyn assembly load: " + args.Name);
            var loaded = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.GetName().Name == name.Name);
            if (loaded != null) return loaded;
            if (name.Name == "System.Threading.Channels" && channelsPath != null)
                return Assembly.LoadFrom(channelsPath);
            var local = Path.Combine(directory, name.Name + ".dll");
            if (File.Exists(local)) return Assembly.LoadFrom(local);
            return Directory.EnumerateFiles(ide, name.Name + ".dll", SearchOption.AllDirectories)
                .Where(p => p.IndexOf("\\SemanticSearchRefs\\", StringComparison.OrdinalIgnoreCase) < 0 && p.IndexOf("\\LanguageServices\\Core\\", StringComparison.OrdinalIgnoreCase) < 0)
                .Where(p =>
                {
                    var candidate = AssemblyName.GetAssemblyName(p);
                    return candidate.Name == name.Name && candidate.CultureName == name.CultureName &&
                        candidate.GetPublicKeyToken().SequenceEqual(name.GetPublicKeyToken()) &&
                        candidate.Version.Major == name.Version.Major && candidate.Version.Minor == name.Version.Minor &&
                        candidate.Version.Build == name.Version.Build && candidate.Version >= name.Version;
                }).OrderBy(p => AssemblyName.GetAssemblyName(p).FullName == name.FullName ? 0 : 1)
                .ThenBy(p => string.Equals(Path.GetDirectoryName(p), Path.Combine(ide, "PublicAssemblies"), StringComparison.OrdinalIgnoreCase) ? 0 : 1)
                .Select(Assembly.LoadFrom).FirstOrDefault();
        };
        AppDomain.CurrentDomain.AssemblyResolve += resolve;
        try
        {
            foreach (var name in new[] { "System.Threading.Tasks.Extensions", "Microsoft.Bcl.AsyncInterfaces" })
                Assembly.LoadFrom(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, name + ".dll"));
            Assembly.LoadFrom(Path.Combine(directory, "Microsoft.VisualStudio.LanguageServices.dll"));
            var extension = Assembly.LoadFrom(extensionPath);
            var contextType = extension.GetType("Nazor.VisualStudio.NazorEditingContext", true);
            var editing = extension.GetType("Nazor.VisualStudio.NazorCSharpEditing", true);
            const BindingFlags flags = BindingFlags.Static | BindingFlags.NonPublic;
            const string source = "@code {\n    void Save() {\n}";
            var opening = source.IndexOf("{", source.IndexOf("Save", StringComparison.Ordinal), StringComparison.Ordinal);
            var region = contextType.GetMethod("At", flags).Invoke(null, new object[] { source, opening + 1, true });
            var result = editing.GetMethod("Complete", flags).Invoke(null, new object[] { source, region, opening, '{', new Dictionary<string, string>() });
            if (result == null) throw new Exception("Cold-start C# brace completion returned no edit");
            var changes = (System.Collections.IEnumerable)result.GetType().GetField("Changes", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(result);
            if (!changes.Cast<object>().Any(c => (string)c.GetType().GetProperty("NewText").GetValue(c) == "}"))
                throw new Exception("Cold-start C# brace completion did not preserve the outer closer");
            Console.WriteLine("PASS: cold native Roslyn startup completes a C# method without a partial-name assembly resolver.");
        }
        catch (TargetInvocationException error) when (error.InnerException is ReflectionTypeLoadException)
        {
            foreach (var failure in ((ReflectionTypeLoadException)error.InnerException).LoaderExceptions)
                Console.Error.WriteLine(failure.Message);
            throw;
        }
        finally { AppDomain.CurrentDomain.AssemblyResolve -= resolve; }
    }
}
