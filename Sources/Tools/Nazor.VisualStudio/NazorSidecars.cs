using System;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using EnvDTE;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TextManager.Interop;
using Newtonsoft.Json.Linq;

namespace Nazor.VisualStudio
{
    [Export]
    internal sealed class NazorSidecars
    {
        [Import]
        private IVsEditorAdaptersFactoryService Adapters { get; set; }

        internal async Task DeleteEmptyAsync(JToken arguments)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            var provider = ServiceProvider.GlobalProvider;
            var table = new RunningDocumentTable(provider);
            string Read(string path)
            {
                var data = table.FindDocument(path);
                if (data is IVsTextBuffer buffer && Adapters.GetDocumentBuffer(buffer) is { } document)
                    return document.CurrentSnapshot.GetText();
                if (data != null) throw new InvalidOperationException("Cannot verify the editor buffer: " + path);
                return File.Exists(path) ? File.ReadAllText(path) : null;
            }
            var paths = Deletable(arguments, Read);
            var dte = Package.GetGlobalService(typeof(SDTE)) as DTE;
            foreach (var path in paths)
            {
                if (VsShellUtilities.IsDocumentOpen(provider, path, Guid.Empty, out _, out _, out IVsWindowFrame frame) && frame != null)
                    Microsoft.VisualStudio.ErrorHandler.ThrowOnFailure(frame.CloseFrame((uint)__FRAMECLOSE.FRAMECLOSE_NoSave));
                var item = dte?.Solution.FindProjectItem(path);
                if (item != null) item.Delete();
                else File.Delete(path);
            }
        }

        internal static string[] Deletable(JToken arguments, Func<string, string> read)
        {
            if (!(arguments?["component"]?.Value<string>() is string component) || !Path.IsPathRooted(component) ||
                !component.EndsWith(".nazor", StringComparison.OrdinalIgnoreCase) || !(arguments["changes"] is JArray changes) || changes.Count == 0)
                throw new InvalidOperationException("Invalid sidecar transfer.");
            component = Path.GetFullPath(component);
            var removed = changes.Where(c => c["delete"]?.Value<bool>() == true).Select(c => c["path"]?.Value<string>()).ToArray();
            if (removed.Any(p => !string.Equals(p, component + ".cs", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(p, component + ".nss", StringComparison.OrdinalIgnoreCase)))
                throw new InvalidOperationException("Only this component's sidecars can be removed.");
            if (!changes.Any(c => string.Equals(c["path"]?.Value<string>(), component, StringComparison.OrdinalIgnoreCase) && c["text"]?.Type == JTokenType.String))
                throw new InvalidOperationException("The transfer has no component destination.");
            foreach (var change in changes)
            {
                var path = change["path"]?.Value<string>();
                if (path == null || !Path.IsPathRooted(path)) throw new InvalidOperationException("Invalid transfer path.");
                var actual = read(path);
                var expected = change["text"]?.Value<string>();
                if (expected == null || (change["delete"]?.Value<bool>() == true ? !string.IsNullOrWhiteSpace(actual) : actual != expected))
                    throw new InvalidOperationException("The transfer was not fully applied; the sidecar was preserved: " + path);
            }
            return removed;
        }
    }
}
