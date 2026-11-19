using System;
using System.ComponentModel.Composition;
using System.IO;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Utilities;

namespace Nazor.VisualStudio
{
    [Export(typeof(IVsTextViewCreationListener))]
    [ContentType(NazorContentTypes.Component)]
    [ContentType("CSharp")]
    [TextViewRole(PredefinedTextViewRoles.Document)]
    internal sealed class ViewCodeListener : IVsTextViewCreationListener
    {
        [Import]
        private IVsEditorAdaptersFactoryService Adapters { get; set; }

        [Import]
        private ITextDocumentFactoryService Documents { get; set; }

        public void VsTextViewCreated(IVsTextView textViewAdapter)
        {
            var view = Adapters.GetWpfTextView(textViewAdapter);
            if (view == null || !Documents.TryGetTextDocument(view.TextDataModel.DocumentBuffer, out var document)) return;
            var path = document.FilePath;
            ViewCodeFilter filter;
            if (path.EndsWith(".nazor", StringComparison.OrdinalIgnoreCase)) filter = new ViewCodeFilter(path + ".cs", viewDesigner: false);
            else if (path.EndsWith(".nazor.cs", StringComparison.OrdinalIgnoreCase)) filter = new ViewCodeFilter(path.Substring(0, path.Length - ".cs".Length), viewDesigner: true);
            else return;
            if (textViewAdapter.AddCommandFilter(filter, out var next) == VSConstants.S_OK) filter.Next = next;
        }
    }

    internal sealed class ViewCodeFilter : IOleCommandTarget
    {
        private readonly string target;
        private readonly bool viewDesigner;

        public ViewCodeFilter(string target, bool viewDesigner)
        {
            this.target = target;
            this.viewDesigner = viewDesigner;
        }

        public IOleCommandTarget Next { get; set; }

        public int QueryStatus(ref Guid pguidCmdGroup, uint cCmds, OLECMD[] prgCmds, IntPtr pCmdText)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            if (pguidCmdGroup == VSConstants.GUID_VSStandardCommandSet97 && cCmds == 1 && Handles(prgCmds[0].cmdID))
            {
                prgCmds[0].cmdf = (uint)(OLECMDF.OLECMDF_SUPPORTED | OLECMDF.OLECMDF_ENABLED);
                return VSConstants.S_OK;
            }
            return Next?.QueryStatus(ref pguidCmdGroup, cCmds, prgCmds, pCmdText) ?? (int)Constants.OLECMDERR_E_NOTSUPPORTED;
        }

        public int Exec(ref Guid pguidCmdGroup, uint nCmdID, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            if (pguidCmdGroup == VSConstants.GUID_VSStandardCommandSet97 && Handles(nCmdID))
            {
                VsShellUtilities.OpenDocument(ServiceProvider.GlobalProvider, target);
                return VSConstants.S_OK;
            }
            return Next?.Exec(ref pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut) ?? (int)Constants.OLECMDERR_E_NOTSUPPORTED;
        }

        private bool Handles(uint command) =>
            (command == (uint)VSConstants.VSStd97CmdID.ViewCode || viewDesigner && command == (uint)VSConstants.VSStd97CmdID.ViewForm) && File.Exists(target);
    }
}
