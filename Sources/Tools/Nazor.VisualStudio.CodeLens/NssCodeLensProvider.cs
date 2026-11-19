using System;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Language.CodeLens;
using Microsoft.VisualStudio.Language.CodeLens.Remoting;
using Microsoft.VisualStudio.Threading;
using Microsoft.VisualStudio.Utilities;

namespace Nazor.VisualStudio.CodeLens
{
    [Export(typeof(IAsyncCodeLensDataPointProvider))]
    [Name("NazorNssReferences")]
    [ContentType("Nazor")]
    [ContentType("NazorStyleSheet")]
    [ExportMetadata("LocalizedName", "NSS references")]
    [Priority(200)]
    [OptionUserModifiable(false)]
    [DetailsTemplateName("references")]
    internal sealed class NssCodeLensProvider : IAsyncCodeLensDataPointProvider
    {
        [Import] private ICodeLensCallbackService Callback { get; set; }

        public Task<bool> CanCreateDataPointAsync(CodeLensDescriptor descriptor, CodeLensDescriptorContext context, CancellationToken cancellation) =>
            Task.FromResult(descriptor.ElementDescription?.StartsWith("nazor-nss:", StringComparison.Ordinal) == true);

        public Task<IAsyncCodeLensDataPoint> CreateDataPointAsync(CodeLensDescriptor descriptor, CodeLensDescriptorContext context, CancellationToken cancellation) =>
            Task.FromResult<IAsyncCodeLensDataPoint>(new DataPoint(this, descriptor));

        private sealed class DataPoint : IAsyncCodeLensDataPoint, IDisposable
        {
            private readonly NssCodeLensProvider provider;
            private readonly CancellationTokenSource stopping = new CancellationTokenSource();
            private NssCodeLensData data;
            public DataPoint(NssCodeLensProvider provider, CodeLensDescriptor descriptor)
            {
                this.provider = provider; Descriptor = descriptor;
                _ = PollAsync();
            }
            public CodeLensDescriptor Descriptor { get; }
            public event AsyncEventHandler InvalidatedAsync;
            public void Dispose() => stopping.Cancel();

            private async Task PollAsync()
            {
                long notified = 0;
                while (!stopping.IsCancellationRequested)
                {
                    try
                    {
                        await Task.Delay(TimeSpan.FromSeconds(1), stopping.Token).ConfigureAwait(false);
                        var version = await provider.Callback.InvokeAsync<long>(provider, "NazorNssReferencesVersionAsync", new object[] { Descriptor.ElementDescription }, stopping.Token).ConfigureAwait(false);
                        if (version == 0) return;
                        if (data != null && version != data.Version && version != notified && InvalidatedAsync != null)
                        {
                            await InvalidatedAsync.InvokeAsync(this, EventArgs.Empty).ConfigureAwait(false);
                            notified = version;
                        }
                    }
                    catch (OperationCanceledException) { return; }
                    catch (Exception exception) when (!(exception is OutOfMemoryException)) { }
                }
            }

            private async Task<NssCodeLensData> ReadAsync(CancellationToken cancellation, bool refresh = false)
            {
                if (refresh || data == null)
                    data = await provider.Callback.InvokeAsync<NssCodeLensData>(provider, "NazorNssReferencesAsync", new object[] { Descriptor.ElementDescription }, cancellation).ConfigureAwait(false) ?? data;
                return data;
            }

            public async Task<CodeLensDataPointDescriptor> GetDataAsync(CodeLensDescriptorContext context, CancellationToken cancellation)
            {
                var result = await ReadAsync(cancellation, true).ConfigureAwait(false);
                if (result == null) return null;
                var count = result.References.Length;
                return new CodeLensDataPointDescriptor
                {
                    IntValue = count,
                    Description = count + (result.Partial ? "+" : "") + (result.Global ? " potential references" : count == 1 ? " reference" : " references"),
                    TooltipText = result.Selector + "\n" + (result.Global ? "Potential source matches; stylesheet assignment is not inferred. " : "Source element declarations. ") +
                        (result.Partial ? "Partial static results: dynamic values or unresolved structure may add matches." : "Interaction states are ignored."),
                };
            }

            public async Task<CodeLensDetailsDescriptor> GetDetailsAsync(CodeLensDescriptorContext context, CancellationToken cancellation) =>
                NazorCodeLensProvider.ReferenceDetails((await ReadAsync(cancellation).ConfigureAwait(false))?.References ?? Array.Empty<NativeCodeLensReference>());
        }
    }
}
