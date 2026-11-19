using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Intermediate;

namespace Nazor.Compiler;

internal sealed class NazorIdPass(Action<string, IReadOnlyDictionary<string, string?>> ids) : IntermediateNodePassBase, IRazorOptimizationPass
{
    private static readonly RazorDiagnosticDescriptor Duplicate = new("NZC017",
        "ID '{0}' is declared more than once in this component. IDs must be unique within their declaring component.", RazorDiagnosticSeverity.Error);

    public override int Order => 175;

    protected override void ExecuteCore(RazorCodeDocument codeDocument, DocumentIntermediateNode documentNode, CancellationToken cancellationToken)
    {
        var walker = new Walker();
        walker.Visit(documentNode);
        var types = new Dictionary<string, string?>(StringComparer.Ordinal);
        foreach (var group in walker.Ids.GroupBy(i => i.Id, StringComparer.Ordinal))
        {
            var declarations = group.ToArray();
            types[group.Key] = declarations.Length == 1 ? declarations[0].Type : null;
            if (declarations.Length > 1)
                foreach (var declaration in declarations)
                    declaration.Node.AddDiagnostic(RazorDiagnostic.Create(Duplicate, declaration.Span, group.Key));
        }
        ids(codeDocument.Source.FilePath ?? "", types);
    }

    private sealed class Walker : IntermediateNodeWalker
    {
        public List<(string Id, string Type, ComponentIntermediateNode Node, SourceSpan? Span)> Ids { get; } = [];

        public override void VisitComponent(ComponentIntermediateNode node)
        {
            foreach (var attribute in node.Children.OfType<ComponentAttributeIntermediateNode>())
            {
                if (!attribute.AttributeName.Equals("Id", StringComparison.OrdinalIgnoreCase) || attribute.IsSynthesized ||
                    attribute.Children.Count == 0 || attribute.Children.Any(c => c is not HtmlContentIntermediateNode)) continue;
                var id = string.Concat(attribute.Children.SelectMany(c => c.Children).OfType<IntermediateToken>().Select(t => t.Content));
                if (!string.IsNullOrEmpty(id))
                    Ids.Add((System.Net.WebUtility.HtmlDecode(id), node.Component.TypeName ?? node.TagName,
                        node, attribute.OriginalAttributeSpan ?? attribute.Source ?? node.StartTagSpan));
            }
            base.VisitComponent(node);
        }
    }
}
