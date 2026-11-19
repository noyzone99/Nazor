using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Nazor.VisualStudio
{
    internal static class QuickAddMappings
    {
        internal const string Setting = "QuickAddNewItems.ItemTemplateMapping";

        internal static string CanonicalDirectory(string path)
        {
            path = Path.GetFullPath(path);
            if (path == Path.GetPathRoot(path)) return path;
            path = path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            var parent = Path.GetDirectoryName(path);
            if (parent == null) return path;
            // The native provider keys are case-sensitive; Assembly.Location can uppercase Windows directory names.
            var name = new DirectoryInfo(parent).EnumerateDirectories(Path.GetFileName(path)).Single().Name;
            return Path.Combine(CanonicalDirectory(parent), name);
        }

        internal static Dictionary<Guid, List<(string, string)>> Merge(IReadOnlyDictionary<Guid, List<(string, string)>> existing, string folder, params string[] roots)
        {
            // Compact View's catalog IDs omit the IDE installation/profile root, even for existing absolute paths.
            foreach (var root in roots.Where(root => !string.IsNullOrEmpty(root)))
            {
                var prefix = root.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
                if (!folder.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) continue;
                folder = folder.Substring(prefix.Length);
                break;
            }
            var result = existing.ToDictionary(pair => pair.Key, pair => pair.Value.ToList());
            var csharp = new Guid("fae04ec0-301f-11d3-bf4b-00c04f79efbc");
            foreach (var project in new[] { csharp, new Guid("9a19103f-16f7-4668-be54-9a1e7a4f7556"), new Guid("8995eee9-f6c2-4122-aca7-19384033a7b1") })
            {
                if (!result.TryGetValue(project, out var items))
                    result[project] = items = result.TryGetValue(csharp, out var defaults) ? defaults.ToList() : new List<(string, string)>();
                Add(items, "nazor", Path.Combine(folder, "ItemTemplates", "CSharp", "Nazor", "NazorComponent", "Component.vstemplate"));
                Add(items, "nss", Path.Combine(folder, "ItemTemplates", "CSharp", "Nazor", "NazorStyleSheet", "StyleSheet.vstemplate"));
            }
            return result;
        }

        private static void Add(List<(string, string)> items, string extension, string template)
        {
            var pattern = @"(?i)^.+\." + extension + "$";
            items.RemoveAll(item => item.Item1 == pattern && (item.Item2.IndexOf("NazorComponent", StringComparison.OrdinalIgnoreCase) >= 0 ||
                item.Item2.IndexOf("NazorStyleSheet", StringComparison.OrdinalIgnoreCase) >= 0));
            if (!items.Any(item => Regex.IsMatch("Item." + extension, item.Item1))) items.Insert(0, (pattern, template));
        }
    }
}
