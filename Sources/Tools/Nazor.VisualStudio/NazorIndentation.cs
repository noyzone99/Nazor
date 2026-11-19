using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;
using Nazor.Authoring;

namespace Nazor.VisualStudio
{
    [Export(typeof(ISmartIndentProvider))]
    [ContentType(NazorContentTypes.Component)]
    [ContentType(NazorContentTypes.StyleSheet)]
    internal sealed class NazorIndentation : ISmartIndentProvider
    {
        public ISmartIndent CreateSmartIndent(ITextView textView) => new Indent(textView);

        private sealed class Indent : ISmartIndent
        {
            private readonly ITextView view;
            internal Indent(ITextView view) { this.view = view; }
            public int? GetDesiredIndentation(ITextSnapshotLine line)
            {
                var text = line.Snapshot.GetText();
                if (line.Snapshot.TextBuffer.ContentType.IsOfType(NazorContentTypes.Component))
                {
                    var region = NazorRegions.At(text, line.Start.Position);
                    if (region != NazorRegion.CSharp && region != NazorRegion.Style) return null;
                }
                return CodeIndentation.Desired(text, line.Start.Position, view.Options.GetOptionValue(DefaultOptions.TabSizeOptionId));
            }
            public void Dispose() { }
        }
    }
}
