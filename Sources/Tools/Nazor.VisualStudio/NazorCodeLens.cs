using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.Language.CodeLens;
using Microsoft.VisualStudio.LanguageServer.Client;
using Microsoft.VisualStudio.LanguageServices;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace Nazor.VisualStudio
{
    [Export(typeof(ICodeLensCallbackListener))]
    [ContentType("CSharp")]
    [ContentType("Basic")]
    internal sealed class NazorCodeLens : ICodeLensCallbackListener, IDisposable
    {
        [Import] private VisualStudioWorkspace Workspace { get; set; }
        [Import] private NazorLanguageClient Client { get; set; }
        [Import] private ILanguageClientBroker Broker { get; set; }
        [Import] private IEditorOptionsFactoryService Options { get; set; }
        private readonly object gate = new object();
        private Task<bool> starting;
        private bool disabledNative;

        private sealed class Metadata : ILanguageClientMetadata
        {
            public IEnumerable<string> ContentTypes => new[] { NazorContentTypes.Component, NazorContentTypes.StyleSheet };
            public string ClientName => null;
        }

        public Task<bool> NazorCodeLensCanCreateAsync() => Task.FromResult(Environment.GetEnvironmentVariable("NAZOR_REFERENCE_MAPPING") != "0");

        public async Task NazorCodeLensUseNativeAsync()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            var options = Options.GlobalOptions;
            if (!options.IsOptionDefined("CodeLensDisabledProviders", false)) return;
            var disabled = options.GetOptionValue("CodeLensDisabledProviders") as string[] ?? new string[0];
            if (!disabled.Contains("CSVBReferences"))
            {
                disabledNative = true;
                options.SetOptionValue("CodeLensDisabledProviders", disabled.Concat(new[] { "CSVBReferences" }).ToArray());
            }
        }

        public void Dispose()
        {
            if (disabledNative) RestoreAsync().FileAndForget("nazor/codelens/restore");
        }

        private async Task RestoreAsync()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            var options = Options.GlobalOptions;
            if (options.GetOptionValue("CodeLensDisabledProviders") is string[] disabled)
                options.SetOptionValue("CodeLensDisabledProviders", disabled.Where(name => name != "CSVBReferences").ToArray());
        }

        private async Task<bool> StartAsync()
        {
            try
            {
                await Broker.LoadAsync(new Metadata(), Client).ConfigureAwait(false);
                for (var i = 0; !Client.IsReady && i < 50; i++) await Task.Delay(100).ConfigureAwait(false);
                return Client.IsReady;
            }
            catch (Exception exception) when (!(exception is OutOfMemoryException)) { return false; }
        }

        public async Task<NativeCodeLensReference[]> NazorCodeLensMapReferencesAsync(NativeCodeLensReference[] references)
        {
            if (references.Any(r => r.FilePath?.EndsWith(".nazor.g.cs", StringComparison.OrdinalIgnoreCase) == true) && !Client.IsReady)
            {
                Task<bool> ready;
                lock (gate) ready = starting ?? (starting = StartAsync());
                await ready.ConfigureAwait(false);
            }
            return (await NativeCodeLensReferences.MapAsync(references, MapAsync, CancellationToken.None).ConfigureAwait(false)).ToArray();
        }

        private async Task<NativeCodeLensReference> MapAsync(NativeCodeLensReference reference, CancellationToken cancellation)
        {
            try
            {
                if (!Client.IsReady || Client.Rpc == null) return null;
                var solution = Workspace.CurrentSolution;
                var source = solution.GetDocumentIdsWithFilePath(reference.FilePath).Select(solution.GetDocument).FirstOrDefault();
                var text = source == null ? System.IO.File.ReadAllText(reference.FilePath) : (await source.GetTextAsync(cancellation).ConfigureAwait(false)).ToString();
                LspLocation mapped;
                using (var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellation))
                {
                    timeout.CancelAfter(TimeSpan.FromSeconds(5));
                    mapped = await Client.Rpc.InvokeWithParameterObjectAsync<LspLocation>("nazor/referencePosition", new
                    { path = reference.FilePath, text, position = new { line = reference.LineNumber, character = reference.ColumnNumber }, length = reference.SpanLength }, timeout.Token).ConfigureAwait(false);
                }
                if (mapped?.uri == null || mapped.range?.start == null || mapped.preview == null) return null;
                var found = reference.Copy();
                found.FilePath = new Uri(mapped.uri).LocalPath;
                found.LineNumber = mapped.range.start.line;
                found.ColumnNumber = found.ReferenceStart = mapped.range.start.character;
                found.ReferenceLength = mapped.range.end.character - mapped.range.start.character;
                found.ReferenceLineText = mapped.preview;
                found.BeforeReferenceText1 = mapped.before1 ?? "";
                found.BeforeReferenceText2 = mapped.before2 ?? "";
                found.AfterReferenceText1 = mapped.after1 ?? "";
                found.AfterReferenceText2 = mapped.after2 ?? "";
                return found;
            }
            catch (Exception exception) when (!(exception is OutOfMemoryException)) { return null; }
        }
    }
}
