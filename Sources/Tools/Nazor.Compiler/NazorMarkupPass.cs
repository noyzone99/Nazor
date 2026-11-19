using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Intermediate;

namespace Nazor.Compiler;

internal sealed class NazorMarkupPass : IntermediateNodePassBase, IRazorOptimizationPass
{
    private static readonly RazorDiagnosticDescriptor HtmlElement = new("NZC010",
        "HTML element '<{0}>' is not supported: Nazor markup uses controls and components (for example Container, TextBlock, Button).",
        RazorDiagnosticSeverity.Error);

    private static readonly RazorDiagnosticDescriptor StaticMarkup = new("NZC011",
        "Static HTML markup is not supported: Nazor markup uses controls and components.",
        RazorDiagnosticSeverity.Error);

    public override int Order => -100_000;

    protected override void ExecuteCore(RazorCodeDocument codeDocument, DocumentIntermediateNode documentNode, CancellationToken cancellationToken)
    {
        var walker = new Walker();
        walker.Visit(documentNode);
    }

    private sealed class Walker : IntermediateNodeWalker
    {
        public override void VisitMarkupElement(MarkupElementIntermediateNode node)
        {
            if (node.TagName.Length == 0 || !char.IsUpper(node.TagName[0]))
                node.AddDiagnostic(RazorDiagnostic.Create(HtmlElement, node.Source, node.TagName));
            base.VisitMarkupElement(node);
        }

        public override void VisitMarkupBlock(MarkupBlockIntermediateNode node) =>
            node.AddDiagnostic(RazorDiagnostic.Create(StaticMarkup, node.Source));
    }
}
