#nullable disable
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Nazor.VisualStudio
{
    internal sealed class NssCodeLensData
    {
        public long Version { get; set; }
        public string Selector { get; set; }
        public bool Partial { get; set; }
        public bool Global { get; set; }
        public NativeCodeLensReference[] References { get; set; }
    }

    internal sealed class NativeReferenceCount
    {
        public int Count { get; set; }
        public bool IsCapped { get; set; }
        public string Version { get; set; }
    }

    internal sealed class NativeReferenceLocations
    {
        public string Item1 { get; set; }
        public NativeCodeLensReference[] Item2 { get; set; }
    }

    internal sealed class NativeCodeLensReference
    {
        public string FilePath { get; set; }
        public int LineNumber { get; set; }
        public int ColumnNumber { get; set; }
        public int SpanStart { get; set; }
        public int SpanLength { get; set; }
        public string ReferenceLineText { get; set; }
        public int ReferenceStart { get; set; }
        public int ReferenceLength { get; set; }
        public string LongDescription { get; set; }
        public int? Glyph { get; set; }
        public string Language { get; set; }
        public string BeforeReferenceText1 { get; set; }
        public string BeforeReferenceText2 { get; set; }
        public string AfterReferenceText1 { get; set; }
        public string AfterReferenceText2 { get; set; }
        public NativeCodeLensReference Copy() => (NativeCodeLensReference)MemberwiseClone();
    }

    internal static class NativeCodeLensReferences
    {
        public static async Task<IReadOnlyList<NativeCodeLensReference>> MapAsync(IReadOnlyList<NativeCodeLensReference> references,
            Func<NativeCodeLensReference, CancellationToken, Task<NativeCodeLensReference>> map, CancellationToken cancellation)
        {
            var result = new List<NativeCodeLensReference>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var reference in references)
            {
                cancellation.ThrowIfCancellationRequested();
                var found = reference.FilePath?.EndsWith(".nazor.g.cs", StringComparison.OrdinalIgnoreCase) == true
                    ? await map(reference, cancellation).ConfigureAwait(false) : reference;
                if (found != null && seen.Add(found.FilePath + "|" + found.LineNumber + "|" + found.ColumnNumber)) result.Add(found);
            }
            return result;
        }
    }
}
