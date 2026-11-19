using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Intermediate;

namespace Nazor.Compiler;

internal sealed class NazorWindowPass(Func<string, bool> isWindow, Func<string, bool> isWindowType) : IntermediateNodePassBase, IRazorOptimizationPass
{
    private static readonly RazorDiagnosticDescriptor Nested = new("NZC031", "Window component '{0}' cannot be nested. Use IWindowService.Open instead.", RazorDiagnosticSeverity.Error);
    private static readonly RazorDiagnosticDescriptor Caption = new("NZC032", "UserTitleBar and UserCaptionButtons must be declared in a Nazor file containing @window.", RazorDiagnosticSeverity.Error);
    private static readonly RazorDiagnosticDescriptor Legacy = new("NZC033", "Replace the Window tag with @window and configure IWindow in OnInitialized.", RazorDiagnosticSeverity.Error);
    public override int Order => 180;
    protected override void ExecuteCore(RazorCodeDocument codeDocument, DocumentIntermediateNode documentNode, CancellationToken cancellationToken) =>
        new Walker(isWindow(codeDocument.Source.FilePath ?? ""), isWindowType).Visit(documentNode);

    private sealed class Walker(bool window, Func<string, bool> isWindowType) : IntermediateNodeWalker
    {
        public override void VisitMarkupElement(MarkupElementIntermediateNode node)
        {
            if (node.TagName == "Window") node.AddDiagnostic(RazorDiagnostic.Create(Legacy, node.Source));
            base.VisitMarkupElement(node);
        }
        public override void VisitComponent(ComponentIntermediateNode node)
        {
            var type = $"{node.Component.TypeNamespace}.{node.Component.TypeNameIdentifier}";
            if (isWindowType(type)) node.AddDiagnostic(RazorDiagnostic.Create(Nested, node.Source, node.TagName));
            if (type is "Nazor.Controls.UserTitleBar" or "Nazor.Controls.UserCaptionButtons" && !window)
                node.AddDiagnostic(RazorDiagnostic.Create(Caption, node.Source));
            if (type == "Nazor.Controls.Window") node.AddDiagnostic(RazorDiagnostic.Create(Legacy, node.Source));
            base.VisitComponent(node);
        }
    }
}
