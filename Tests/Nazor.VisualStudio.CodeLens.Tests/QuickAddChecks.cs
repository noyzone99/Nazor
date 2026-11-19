using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Nazor.VisualStudio.CodeLens.Tests;

internal static class QuickAddChecks
{
    internal static void Check(Assembly extension, string ide)
    {
        var dialogs = Assembly.LoadFrom(Path.Combine(ide, "PrivateAssemblies", "Microsoft.VisualStudio.Dialogs.dll"));
        var defaults = (IReadOnlyDictionary<Guid, List<(string, string)>>)dialogs.GetType("Microsoft.VisualStudio.Dialogs.Mapping", true)
            .GetField("DefaultProjectTemplateMapping", BindingFlags.Public | BindingFlags.Static).GetValue(null);
        var merge = extension.GetType("Nazor.VisualStudio.QuickAddMappings", true).GetMethod("Merge", BindingFlags.NonPublic | BindingFlags.Static);
        var profile = Path.GetFullPath("profile");
        var updated = (IReadOnlyDictionary<Guid, List<(string, string)>>)merge.Invoke(null, new object[] { defaults, Path.Combine(profile, "Extensions", "Nazor"), new[] { profile } });
        foreach (var pair in defaults)
            if (pair.Value.Any(item => !updated[pair.Key].Contains(item))) throw new Exception("Quick Add must retain installed host mappings");
        var csharp = updated[new Guid("fae04ec0-301f-11d3-bf4b-00c04f79efbc")];
        foreach (var name in new[] { "Card.nazor", "Theme.nss", "Card.nazor.nss" })
        {
            var template = csharp.FirstOrDefault(item => Regex.IsMatch(name, item.Item1));
            if (template.Item2 == null || !template.Item2.StartsWith("Extensions" + Path.DirectorySeparatorChar) || Path.IsPathRooted(template.Item2))
                throw new Exception("Quick Add requires a relative catalog ID for " + name);
        }
        var native = dialogs.GetType("Microsoft.VisualStudio.NewProjectDialog.LegacyVsTemplateProvider", true);
        if (native.GetMethod("GetUniqueId", BindingFlags.Static | BindingFlags.NonPublic) == null ||
            dialogs.GetType("Microsoft.VisualStudio.Dialogs.ServiceHelper", true).GetProperty("InstallRootDir", BindingFlags.Static | BindingFlags.NonPublic) == null)
            throw new Exception("The native template ID compatibility contract changed");
        Console.WriteLine("PASS: installed VS " + dialogs.GetName().Version + " Quick Add defaults are retained and Nazor uses relative catalog IDs.");
    }
}
