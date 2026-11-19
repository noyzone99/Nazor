#nullable enable
using System;
using System.IO;
using System.Linq;

namespace Nazor.VisualStudio
{
    internal static class HotReloadFiles
    {
        internal static string[] BuildInputs(string directory) => Directory.GetFiles(directory, "*", SearchOption.AllDirectories)
            .Where(p => IsInput(p) && !Path.GetFileName(p).Equals("_Imports.nazor", StringComparison.OrdinalIgnoreCase) &&
                !p.EndsWith(".nazor.nss", StringComparison.OrdinalIgnoreCase))
            .OrderBy(p => p, StringComparer.OrdinalIgnoreCase).ToArray();

        internal static bool IsInput(string? path)
        {
            if (path == null) return false;
            var normalized = path.Replace('\\', '/');
            if (normalized.IndexOf("/obj/", StringComparison.OrdinalIgnoreCase) >= 0 ||
                normalized.IndexOf("/bin/", StringComparison.OrdinalIgnoreCase) >= 0 ||
                normalized.IndexOf("/.git/", StringComparison.OrdinalIgnoreCase) >= 0) return false;
            return path.EndsWith(".nazor", StringComparison.OrdinalIgnoreCase) || path.EndsWith(".nss", StringComparison.OrdinalIgnoreCase);
        }

        internal static string? FindManifest(string? document, string? project)
        {
            if (document == null || project == null || !document.EndsWith(".g.cs", StringComparison.OrdinalIgnoreCase)) return null;
            var root = Path.GetDirectoryName(Path.GetFullPath(project))!.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            for (var directory = Path.GetDirectoryName(Path.GetFullPath(document));
                 directory != null && directory.StartsWith(root, StringComparison.OrdinalIgnoreCase);
                 directory = Path.GetDirectoryName(directory))
            {
                var manifest = Path.Combine(directory, "nazorc.ide");
                if (File.Exists(manifest)) return manifest;
            }
            return null;
        }
    }
}
