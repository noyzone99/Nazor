using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Language.CodeLens;
using Microsoft.VisualStudio.Language.CodeLens.Remoting;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Core.Imaging;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Threading;
using Microsoft.VisualStudio.Utilities;

namespace Nazor.VisualStudio.CodeLens
{
    [Export(typeof(IAsyncCodeLensDataPointProvider))]
    [Name("NazorReferences")]
    [ContentType("CSharp")]
    [ContentType("Basic")]
    [ExportMetadata("LocalizedName", "References")]
    [Priority(200)]
    [OptionUserModifiable(false)]
    [DetailsTemplateName("references")]
    internal sealed class NazorCodeLensProvider : IAsyncCodeLensDataPointProvider
    {
        [Import] private ICodeLensCallbackService Callback { get; set; }

        public Task<bool> CanCreateDataPointAsync(CodeLensDescriptor descriptor, CodeLensDescriptorContext context, CancellationToken cancellation) =>
            context?.ApplicableSpan != null && (descriptor.Kind & (CodeElementKinds.Type | CodeElementKinds.Member)) != 0
                ? Callback.InvokeAsync<bool>(this, "NazorCodeLensCanCreateAsync", new object[0], cancellation) : Task.FromResult(false);

        public Task<IAsyncCodeLensDataPoint> CreateDataPointAsync(CodeLensDescriptor descriptor, CodeLensDescriptorContext context, CancellationToken cancellation) =>
            Task.FromResult<IAsyncCodeLensDataPoint>(new DataPoint(this, descriptor));

        internal static CodeLensDetailsDescriptor ReferenceDetails(NativeCodeLensReference[] references) => DataPoint.Details(references);

        private sealed class DataPoint : IAsyncCodeLensDataPoint, IDisposable
        {
            private readonly NazorCodeLensProvider provider;
            private readonly CancellationTokenSource stopping = new CancellationTokenSource();
            private string version;
            private NativeReferenceCount calculated;
            public CodeLensDescriptor Descriptor { get; }
            public event AsyncEventHandler InvalidatedAsync;

            public DataPoint(NazorCodeLensProvider provider, CodeLensDescriptor descriptor)
            {
                this.provider = provider;
                Descriptor = descriptor;
                _ = PollAsync();
            }

            public void Dispose() => stopping.Cancel();

            private async Task PollAsync()
            {
                try
                {
                    while (!stopping.IsCancellationRequested)
                    {
                        var versions = await provider.Callback.InvokeAsync<Dictionary<Guid, string>>(provider, "GetProjectVersionsAsync", new object[] { new[] { Descriptor.ProjectGuid } }, stopping.Token).ConfigureAwait(false);
                        versions.TryGetValue(Descriptor.ProjectGuid, out var current);
                        if (version != null && current != version && InvalidatedAsync != null)
                            await InvalidatedAsync.InvokeAsync(this, EventArgs.Empty).ConfigureAwait(false);
                        version = current;
                        await Task.Delay(TimeSpan.FromSeconds(5), stopping.Token).ConfigureAwait(false);
                    }
                }
                catch (OperationCanceledException) { }
                catch (Exception exception) when (!(exception is OutOfMemoryException)) { }
            }

            public async Task<CodeLensDataPointDescriptor> GetDataAsync(CodeLensDescriptorContext context, CancellationToken cancellation)
            {
                var count = await provider.Callback.InvokeAsync<NativeReferenceCount>(provider, "GetReferenceCountAsync", new object[] { Descriptor, context, calculated }, cancellation).ConfigureAwait(false);
                if (count == null) return null;
                await provider.Callback.InvokeAsync<object>(provider, "NazorCodeLensUseNativeAsync", new object[0], cancellation).ConfigureAwait(false);
                return new CodeLensDataPointDescriptor
                {
                    Description = count.Count + (count.IsCapped ? "+ references" : count.Count == 1 ? " reference" : " references"),
                    IntValue = count.Count,
                    TooltipText = "References in source files",
                };
            }

            public async Task<CodeLensDetailsDescriptor> GetDetailsAsync(CodeLensDescriptorContext context, CancellationToken cancellation)
            {
                var native = await provider.Callback.InvokeAsync<NativeReferenceLocations>(provider, "FindReferenceLocationsAsync", new object[] { Descriptor, context }, cancellation).ConfigureAwait(false);
                var references = native?.Item2 ?? new NativeCodeLensReference[0];
                if (references.Any(r => r.FilePath?.EndsWith(".nazor.g.cs", StringComparison.OrdinalIgnoreCase) == true))
                    references = await provider.Callback.InvokeAsync<NativeCodeLensReference[]>(provider, "NazorCodeLensMapReferencesAsync", new object[] { references }, cancellation).ConfigureAwait(false);
                if (native != null && (calculated == null || calculated.Count != references.Length || calculated.Version != native.Item1 || calculated.IsCapped))
                {
                    calculated = new NativeReferenceCount { Count = references.Length, IsCapped = false, Version = native.Item1 };
                    if (InvalidatedAsync != null) await InvalidatedAsync.InvokeAsync(this, EventArgs.Empty).ConfigureAwait(false);
                }
                return Details(references);
            }

            internal static CodeLensDetailsDescriptor Details(NativeCodeLensReference[] references)
            {
                var names = new[] { ReferenceEntryFieldNames.FilePath, ReferenceEntryFieldNames.LineNumber, ReferenceEntryFieldNames.ColumnNumber,
                    ReferenceEntryFieldNames.ReferenceText, ReferenceEntryFieldNames.ReferenceStart, ReferenceEntryFieldNames.ReferenceEnd,
                    ReferenceEntryFieldNames.ReferenceLongDescription, ReferenceEntryFieldNames.ReferenceImageId,
                    ReferenceEntryFieldNames.TextBeforeReference2, ReferenceEntryFieldNames.TextBeforeReference1,
                    ReferenceEntryFieldNames.TextAfterReference1, ReferenceEntryFieldNames.TextAfterReference2 };
                return new CodeLensDetailsDescriptor
                {
                    Headers = names.Select(name => new CodeLensDetailHeaderDescriptor { UniqueName = name }).ToList(),
                    Entries = references.Select(reference => new CodeLensDetailEntryDescriptor
                    {
                        Fields = new List<CodeLensDetailEntryField>
                        {
                            new CodeLensDetailEntryField { Text = reference.FilePath },
                            new CodeLensDetailEntryField { Text = reference.LineNumber.ToString() },
                            new CodeLensDetailEntryField { Text = reference.ColumnNumber.ToString() },
                            new CodeLensDetailEntryField { Text = reference.ReferenceLineText },
                            new CodeLensDetailEntryField { Text = reference.ReferenceStart.ToString() },
                            new CodeLensDetailEntryField { Text = (reference.ReferenceStart + reference.ReferenceLength).ToString() },
                            new CodeLensDetailEntryField { Text = reference.LongDescription },
                            new CodeLensDetailEntryField { ImageId = Image(reference.Glyph) },
                            new CodeLensDetailEntryField { Text = reference.BeforeReferenceText2 },
                            new CodeLensDetailEntryField { Text = reference.BeforeReferenceText1 },
                            new CodeLensDetailEntryField { Text = reference.AfterReferenceText1 },
                            new CodeLensDetailEntryField { Text = reference.AfterReferenceText2 },
                        },
                    }).ToList(),
                };
            }

            private static ImageId Image(int? glyph)
            {
                if (glyph == null) return default;
                var groups = new Dictionary<int, string> { [4] = "Class", [10] = "Constant", [14] = "Delegate", [18] = "Enum", [22] = "EnumMember",
                    [28] = "Event", [32] = "ExtensionMethod", [36] = "Field", [40] = "Interface", [49] = "Method", [53] = "Module",
                    [58] = "Operator", [63] = "Property", [69] = "Structure" };
                var name = "CSFile";
                foreach (var group in groups)
                    if (glyph >= group.Key && glyph < group.Key + 4)
                        name = group.Value + new[] { "Public", "Protected", "Private", "Internal" }[glyph.Value - group.Key];
                if (glyph == 47 || glyph == 67) name = "LocalVariable";
                else if (glyph == 48) name = "Namespace";
                else if (glyph == 62) name = "Parameter";
                else if (glyph == 73) name = "TypeParameter";
                var field = typeof(KnownImageIds).GetField(name, BindingFlags.Public | BindingFlags.Static);
                return new ImageId(KnownImageIds.ImageCatalogGuid, field == null ? KnownImageIds.CSFile : (int)field.GetValue(null));
            }
        }
    }
}
