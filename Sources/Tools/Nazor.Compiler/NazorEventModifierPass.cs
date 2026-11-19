using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Intermediate;

namespace Nazor.Compiler;

internal sealed class NazorEventModifierPass : IntermediateNodePassBase, IRazorOptimizationPass
{
    private static readonly RazorDiagnosticDescriptor UnknownEventParameter = new("NZC012",
        "'{0}' is not a parameter of '{1}': an event takes 'preventDefault' and 'stopPropagation'.",
        RazorDiagnosticSeverity.Error);

    private static readonly RazorDiagnosticDescriptor UnknownBindParameter = new("NZC013",
        "'{0}' is not a parameter of '{1}': a binding takes 'after', 'get' and 'set'.",
        RazorDiagnosticSeverity.Error);

    private static readonly HashSet<string> BindParameters = ["after", "get", "set"];

    public override int Order => 55;

    protected override void ExecuteCore(RazorCodeDocument codeDocument, DocumentIntermediateNode documentNode, CancellationToken cancellationToken) =>
        new Walker().Visit(documentNode);

    private sealed class Walker : IntermediateNodeWalker
    {
        public override void VisitComponent(ComponentIntermediateNode node)
        {
            foreach (var attribute in node.Children.OfType<ComponentAttributeIntermediateNode>().ToList())
            {
                var modifier = attribute.AddAttributeMethodName switch
                {
                    "AddEventPreventDefaultAttribute" => "preventDefault",
                    "AddEventStopPropagationAttribute" => "stopPropagation",
                    _ => null,
                };
                if (modifier is null)
                {
                    Check(node, attribute);
                    continue;
                }
                attribute.AttributeName = "__internal_" + modifier + "_" + attribute.AttributeName;
                attribute.AddAttributeMethodName = null;
                attribute.BoundAttribute = null;
                attribute.TypeName = "System.Boolean";
                if (attribute.Children.OfType<CSharpExpressionIntermediateNode>().FirstOrDefault() is { } expression)
                {
                    expression.Children.Insert(0, new CSharpIntermediateToken(
                        "global::Microsoft.AspNetCore.Components.CompilerServices.RuntimeHelpers.TypeCheck<global::System.Boolean>(", null));
                    expression.Children.Add(new CSharpIntermediateToken(")", null));
                }
            }
            foreach (var binding in node.Children.OfType<TagHelperDirectiveAttributeIntermediateNode>())
            {
                var name = binding.OriginalAttributeName ?? binding.AttributeName;
                if (name?.IndexOf(':') is not (> 0 and var colon)) continue;
                var directive = name.StartsWith('@') ? name[..colon] : "@" + name[..colon];
                if (directive.StartsWith("@bind", StringComparison.Ordinal) && !BindParameters.Contains(name[(colon + 1)..]))
                    node.AddDiagnostic(RazorDiagnostic.Create(UnknownBindParameter, binding.OriginalAttributeSpan ?? binding.Source ?? node.Source, name[(colon + 1)..], directive));
            }
            base.VisitComponent(node);
        }

        private static void Check(ComponentIntermediateNode node, ComponentAttributeIntermediateNode attribute)
        {
            var original = attribute.OriginalAttributeName ?? attribute.AttributeName;
            var colon = original?.IndexOf(':') ?? -1;
            if (original is null || colon < 0) return;
            if (!original.StartsWith('@')) original = "@bind-" + original;
            colon = original.IndexOf(':');
            var (directive, parameter) = (original[..colon], original[(colon + 1)..]);
            if (directive.StartsWith("@bind", StringComparison.Ordinal))
            {
                if (!BindParameters.Contains(parameter) && !attribute.AttributeName.EndsWith("Changed", StringComparison.Ordinal))
                    node.AddDiagnostic(RazorDiagnostic.Create(UnknownBindParameter, attribute.Source ?? node.Source, parameter, directive));
            }
            else if (directive.StartsWith("@on", StringComparison.Ordinal))
                node.AddDiagnostic(RazorDiagnostic.Create(UnknownEventParameter, attribute.Source ?? node.Source, parameter, directive));
        }
    }
}
