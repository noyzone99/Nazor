using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Nazor.Compiler;

internal sealed class RuntimeAdapter : CSharpSyntaxRewriter
{
    public List<TextChange> Changes { get; } = [];

    private static readonly Dictionary<string, string> Namespaces = new(StringComparer.Ordinal)
    {
        ["global::Microsoft.AspNetCore.Components"] = "global::Nazor",
        ["global::Microsoft.AspNetCore.Components.Rendering"] = "global::Nazor.Rendering",
        ["global::Microsoft.AspNetCore.Components.RenderTree"] = "global::Nazor.Rendering",
        ["global::Microsoft.AspNetCore.Components.CompilerServices"] = "global::Nazor.CompilerServices",
        ["global::Microsoft.AspNetCore.Components.Web"] = "global::Nazor.Rendering",
    };

    private static readonly HashSet<string> RemovedUsings = new(StringComparer.Ordinal)
    {
        "Microsoft.AspNetCore.Components",
        "Microsoft.AspNetCore.Components.Web",
        "global::Microsoft.AspNetCore.Components",
    };

    public override SyntaxNode? VisitQualifiedName(QualifiedNameSyntax node)
    {
        if (!Namespaces.TryGetValue(node.ToString(), out var target)) return base.VisitQualifiedName(node);
        Changes.Add(new TextChange(node.Span, target));
        return SyntaxFactory.ParseName(target).WithTriviaFrom(node);
    }

    public override SyntaxNode? VisitMemberAccessExpression(MemberAccessExpressionSyntax node)
    {
        if (!Namespaces.TryGetValue(node.ToString(), out var target)) return base.VisitMemberAccessExpression(node);
        Changes.Add(new TextChange(node.Span, target));
        return SyntaxFactory.ParseExpression(target).WithTriviaFrom(node);
    }

    public override SyntaxNode? VisitUsingDirective(UsingDirectiveSyntax node)
    {
        if (node.Name is null || !RemovedUsings.Contains(node.Name.ToString())) return base.VisitUsingDirective(node);
        Changes.Add(new TextChange(node.FullSpan, ""));
        return null;
    }

    public override SyntaxNode? VisitInvocationExpression(InvocationExpressionSyntax node)
    {
        var visited = (InvocationExpressionSyntax)base.VisitInvocationExpression(node)!;
        if (node.Expression is MemberAccessExpressionSyntax inference && inference.Expression.ToString().EndsWith(".TypeInference", StringComparison.Ordinal) &&
            inference.Name.Identifier.Text.StartsWith("Create", StringComparison.Ordinal) && !inference.Name.Identifier.Text.EndsWith("CaptureParameters", StringComparison.Ordinal) &&
            ScopeType(node) is { } callerScope)
            return AddScope(node, visited, callerScope);
        if (visited.Expression is not MemberAccessExpressionSyntax { Name: GenericNameSyntax { Identifier.Text: "OpenComponent" } } ||
            visited.ArgumentList.Arguments.Count != 1)
            return visited;
        ExpressionSyntax? scope = node.Ancestors().OfType<ClassDeclarationSyntax>().FirstOrDefault()?.Identifier.Text == "TypeInference"
            ? SyntaxFactory.IdentifierName("__nazorScope") : ScopeType(node);
        return scope is null ? visited : AddScope(node, visited, scope);
    }

    public override SyntaxNode? VisitMethodDeclaration(MethodDeclarationSyntax node)
    {
        var visited = (MethodDeclarationSyntax)base.VisitMethodDeclaration(node)!;
        if (node.Parent is not ClassDeclarationSyntax { Identifier.Text: "TypeInference" } ||
            !node.DescendantNodes().OfType<InvocationExpressionSyntax>().Any(call => call.Expression is MemberAccessExpressionSyntax { Name.Identifier.Text: "OpenComponent" })) return visited;
        var parameter = SyntaxFactory.Parameter(SyntaxFactory.Identifier("__nazorScope"))
            .WithType(SyntaxFactory.ParseTypeName("global::System.Type").WithTrailingTrivia(SyntaxFactory.Space)).WithLeadingTrivia(SyntaxFactory.Space);
        var owner = SyntaxFactory.Parameter(SyntaxFactory.Identifier("__nazorOwner"))
            .WithType(SyntaxFactory.ParseTypeName("global::Nazor.IComponent").WithTrailingTrivia(SyntaxFactory.Space)).WithLeadingTrivia(SyntaxFactory.Space);
        Changes.Add(new TextChange(new TextSpan(node.ParameterList.CloseParenToken.SpanStart, 0), "," + parameter.ToFullString() + "," + owner.ToFullString()));
        return visited.WithParameterList(visited.ParameterList.WithParameters(visited.ParameterList.Parameters.Add(parameter).Add(owner)));
    }

    private InvocationExpressionSyntax AddScope(InvocationExpressionSyntax node, InvocationExpressionSyntax visited, ExpressionSyntax scope)
    {
        var argument = SyntaxFactory.Argument(scope).WithLeadingTrivia(SyntaxFactory.Space);
        ExpressionSyntax owner = scope is IdentifierNameSyntax { Identifier.Text: "__nazorScope" }
            ? SyntaxFactory.IdentifierName("__nazorOwner")
            : node.Ancestors().Any(a => a is MemberDeclarationSyntax or AnonymousFunctionExpressionSyntax or LocalFunctionStatementSyntax &&
                a.ChildTokens().Any(t => t.IsKind(SyntaxKind.StaticKeyword)))
                ? SyntaxFactory.ParseExpression("null!") : SyntaxFactory.ThisExpression();
        var ownerArgument = SyntaxFactory.Argument(owner).WithLeadingTrivia(SyntaxFactory.Space);
        Changes.Add(new TextChange(new TextSpan(node.ArgumentList.CloseParenToken.SpanStart, 0), "," + argument.ToFullString() + "," + ownerArgument.ToFullString()));
        return visited.WithArgumentList(visited.ArgumentList.WithArguments(visited.ArgumentList.Arguments.Add(argument).Add(ownerArgument)));
    }

    private static TypeOfExpressionSyntax? ScopeType(SyntaxNode node)
    {
        var component = node.Ancestors().OfType<ClassDeclarationSyntax>().LastOrDefault();
        if (component is null || component.Identifier.Text == "TypeInference") return null;
        var name = component.TypeParameterList is { Parameters.Count: var count } && count > 0
            ? $"{component.Identifier.Text}<{new string(',', count - 1)}>"
            : component.Identifier.Text;
        var namespaces = component.Ancestors().OfType<BaseNamespaceDeclarationSyntax>().Reverse().Select(n => n.Name.ToString());
        var qualified = string.Join(".", namespaces.Append(name));
        return (TypeOfExpressionSyntax)SyntaxFactory.ParseExpression($"typeof(global::{qualified})");
    }
}
