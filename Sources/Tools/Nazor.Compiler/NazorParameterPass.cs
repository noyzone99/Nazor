using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Intermediate;

namespace Nazor.Compiler;

internal sealed class NazorParameterPass : IntermediateNodePassBase, IRazorOptimizationPass
{
    private static readonly RazorDiagnosticDescriptor Unknown = new("NZC014",
        "Component '{0}' does not have a parameter named '{1}'. Use @attributes for additional attributes.", RazorDiagnosticSeverity.Error);
    private static readonly RazorDiagnosticDescriptor Empty = new("NZC015",
        "Parameter '{0}' of component '{1}' has type '{2}' and cannot have an empty value. Supply a value or an explicit C# expression.", RazorDiagnosticSeverity.Error);
    private static readonly RazorDiagnosticDescriptor MissingValue = new("NZC016",
        "Parameter '{0}' of component '{1}' has type '{2}' and requires an assigned value. Only bool parameters can omit the assignment.", RazorDiagnosticSeverity.Error);

    public override int Order => 170;

    protected override void ExecuteCore(RazorCodeDocument codeDocument, DocumentIntermediateNode documentNode, CancellationToken cancellationToken) =>
        new Walker(RazorSdkAdapter.AttributeNames(codeDocument)).Visit(documentNode);

    private sealed class Walker(IReadOnlyList<(string Name, SourceSpan Span)> names) : IntermediateNodeWalker
    {
        public override void VisitComponent(ComponentIntermediateNode node)
        {
            foreach (var attribute in node.Children.OfType<ComponentAttributeIntermediateNode>())
            {
                if (attribute.IsSynthesized || attribute.OriginalAttributeName?.StartsWith('@') == true ||
                    attribute.AttributeName.StartsWith("__internal_", StringComparison.Ordinal)) continue;
                var parameter = node.Component.BoundAttributes.FirstOrDefault(p => p.Name.Equals(attribute.AttributeName, StringComparison.OrdinalIgnoreCase));
                var name = names.FirstOrDefault(n => n.Name.Equals(attribute.AttributeName, StringComparison.OrdinalIgnoreCase) &&
                    n.Span.AbsoluteIndex >= node.StartTagSpan.AbsoluteIndex &&
                    (node.Source is not { } source || n.Span.AbsoluteIndex < source.AbsoluteIndex + source.Length));
                var span = name.Name is not null ? name.Span : attribute.OriginalAttributeSpan ?? attribute.Source ?? node.Source;
                if (parameter is null)
                {
                    node.AddDiagnostic(RazorDiagnostic.Create(Unknown, span, node.TagName, attribute.AttributeName));
                    continue;
                }
                var type = (attribute.TypeName ?? parameter.TypeName).Replace("global::", "", StringComparison.Ordinal);
                if (attribute.AttributeStructure == AttributeStructure.Minimized)
                {
                    if (type is not ("System.Boolean" or "bool"))
                        node.AddDiagnostic(RazorDiagnostic.Create(MissingValue, span, attribute.AttributeName, node.TagName, type));
                    continue;
                }
                if (type.TrimEnd('?') is "System.String" or "string") continue;
                if (!HasContent(attribute))
                    node.AddDiagnostic(RazorDiagnostic.Create(Empty, span, attribute.AttributeName, node.TagName, type));
            }
            base.VisitComponent(node);
        }

        private static bool HasContent(IntermediateNode node) =>
            node is IntermediateToken token ? token.Content.Length > 0 : node.Children.Any(HasContent);
    }
}
