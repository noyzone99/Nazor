using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Internal.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Settings;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Settings;
using Microsoft.VisualStudio.Shell.Interop;

namespace Nazor.VisualStudio
{
    internal static class NazorQuickAdd
    {
        internal static async Task RegisterAsync(AsyncPackage package)
        {
            try
            {
                var settings = await package.GetServiceAsync(typeof(SVsSettingsPersistenceManager)) as ISettingsManager;
                if (settings == null) return;
                var dialogs = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.GetName().Name == "Microsoft.VisualStudio.Dialogs")
                    ?? Assembly.LoadFrom(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "PrivateAssemblies", "Microsoft.VisualStudio.Dialogs.dll"));
                var defaults = dialogs.GetType("Microsoft.VisualStudio.Dialogs.Mapping", true)
                    .GetField("DefaultProjectTemplateMapping", BindingFlags.Public | BindingFlags.Static)
                    .GetValue(null) as IReadOnlyDictionary<Guid, List<(string, string)>>;
                if (defaults == null) return;
                var current = settings.GetValueOrDefault(QuickAddMappings.Setting, defaults);
                var folder = QuickAddMappings.CanonicalDirectory(Path.GetDirectoryName(typeof(NazorQuickAdd).Assembly.Location));
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                var installRoot = (string)dialogs.GetType("Microsoft.VisualStudio.Dialogs.ServiceHelper", true)
                    .GetProperty("InstallRootDir", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);
                var profileRoot = new ShellSettingsManager(package).GetApplicationDataFolder(ApplicationDataFolder.LocalSettings);
                var updated = QuickAddMappings.Merge(current, folder, installRoot, profileRoot);
                if (updated.Count != current.Count || updated.Any(pair => !current.TryGetValue(pair.Key, out var value) || !pair.Value.SequenceEqual(value)))
                    await settings.SetValueAsync(QuickAddMappings.Setting, updated, false);
            }
            catch (Exception exception) when (!(exception is OutOfMemoryException))
            {
                ActivityLog.LogWarning("Nazor", "Quick Add template registration failed: " + exception.Message);
            }
        }
    }
}
