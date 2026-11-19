using System.Text;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Nazor.LanguageServer.Protocol;
using Nazor.LanguageServer.Workspace;

namespace Nazor.LanguageServer.Features;

internal static class CSharpSignatures
{
    private static readonly SymbolDisplayFormat TypeFormat = SymbolDisplayFormat.MinimallyQualifiedFormat
        .AddMiscellaneousOptions(SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier);

    private static readonly SymbolDisplayFormat NameFormat = TypeFormat
        .WithMemberOptions(SymbolDisplayMemberOptions.IncludeContainingType)
        .WithParameterOptions(SymbolDisplayParameterOptions.None);

    private static readonly SymbolDisplayFormat ParameterFormat = TypeFormat
        .WithParameterOptions(SymbolDisplayParameterOptions.IncludeType | SymbolDisplayParameterOptions.IncludeName |
            SymbolDisplayParameterOptions.IncludeDefaultValue | SymbolDisplayParameterOptions.IncludeParamsRefOut);

    public static async Task<SignatureHelp?> HelpAsync(ProjectSnapshot snapshot, ComponentDocument component, int offset, CancellationToken cancellation)
    {
        if (component.ToGenerated(offset) is not { } position || snapshot.Document(component) is not { } document) return null;
        var root = await document.GetSyntaxRootAsync(cancellation);
        var model = await document.GetSemanticModelAsync(cancellation);
        if (root is null || model is null) return null;
        var list = root.FindToken(position).Parent?.AncestorsAndSelf().FirstOrDefault(node => Inside(node, position));
        if (list is null || component.ToOriginal(list.GetFirstToken().SpanStart, 1) is null) return null;
        if (Members(model, list, position) is not { Count: > 0 } members) return null;

        var commas = list.ChildTokens().Count(t => t.IsKind(SyntaxKind.CommaToken) && t.SpanStart < position);
        var argument = list.ChildNodes().ElementAtOrDefault(commas);
        var name = argument switch
        {
            ArgumentSyntax { NameColon: { } colon } => colon.Name.Identifier.ValueText,
            AttributeArgumentSyntax { NameColon: { } colon } => colon.Name.Identifier.ValueText,
            _ => null,
        };
        var chosen = model.GetSymbolInfo(list.Parent!, cancellation) is { Symbol: { } symbol } ? symbol.OriginalDefinition : null;

        var signatures = new List<SignatureInformation>();
        var active = -1;
        foreach (var member in members.OrderBy(m => Parameters(m).Length))
        {
            var parameters = Parameters(member);
            var index = name is not null ? parameters.Select(p => p.Name).ToList().IndexOf(name) :
                commas < parameters.Length ? commas :
                parameters.Length > 0 && parameters[^1].IsParams ? parameters.Length - 1 : -1;
            if (SymbolEqualityComparer.Default.Equals(member.OriginalDefinition, chosen) || active < 0 && (index >= 0 || commas == 0 && parameters.Length == 0))
                active = signatures.Count;
            signatures.Add(Signature(member, model, position) with { ActiveParameter = index >= 0 ? index : parameters.Length });
        }
        if (active < 0) active = 0;
        return new(signatures, active, signatures[active].ActiveParameter ?? 0);
    }

    private static bool Inside(SyntaxNode node, int position) => node switch
    {
        ArgumentListSyntax { Parent: InvocationExpressionSyntax or BaseObjectCreationExpressionSyntax } list => Between(list.OpenParenToken, list.CloseParenToken, position),
        BracketedArgumentListSyntax { Parent: ElementAccessExpressionSyntax } list => Between(list.OpenBracketToken, list.CloseBracketToken, position),
        AttributeArgumentListSyntax list => Between(list.OpenParenToken, list.CloseParenToken, position),
        _ => false,
    };

    private static bool Between(SyntaxToken open, SyntaxToken close, int position) =>
        position > open.SpanStart && (close.IsMissing || position <= close.SpanStart);

    private static List<ISymbol>? Members(SemanticModel model, SyntaxNode list, int position)
    {
        IEnumerable<ISymbol> members;
        switch (list.Parent)
        {
            case InvocationExpressionSyntax invocation:
                members = model.GetMemberGroup(invocation.Expression).OfType<IMethodSymbol>();
                if (!members.Any() && model.GetTypeInfo(invocation.Expression).Type is INamedTypeSymbol { DelegateInvokeMethod: { } invoke })
                    members = [invoke];
                break;
            case BaseObjectCreationExpressionSyntax creation:
                members = model.GetTypeInfo(creation).Type is INamedTypeSymbol created ? created.InstanceConstructors : [];
                break;
            case ElementAccessExpressionSyntax access:
                members = Indexers(model.GetTypeInfo(access.Expression).Type);
                break;
            case AttributeSyntax attribute:
                members = model.GetTypeInfo(attribute).Type is INamedTypeSymbol type ? type.InstanceConstructors : [];
                break;
            default:
                return null;
        }
        return [.. members.Where(m => model.IsAccessible(position, m) && (!m.IsImplicitlyDeclared || m is IMethodSymbol { MethodKind: MethodKind.Constructor }))
            .Distinct(SymbolEqualityComparer.Default)];
    }

    private static IEnumerable<ISymbol> Indexers(ITypeSymbol? type)
    {
        for (var current = type; current is not null; current = current.BaseType)
            foreach (var indexer in current.GetMembers().OfType<IPropertySymbol>().Where(p => p.IsIndexer))
                yield return indexer;
    }

    private static System.Collections.Immutable.ImmutableArray<IParameterSymbol> Parameters(ISymbol member) => member switch
    {
        IMethodSymbol method => method.Parameters,
        IPropertySymbol property => property.Parameters,
        _ => [],
    };

    private static SignatureInformation Signature(ISymbol member, SemanticModel model, int position)
    {
        var label = new StringBuilder();
        var (open, close) = ("(", ")");
        switch (member)
        {
            case IMethodSymbol { MethodKind: MethodKind.Constructor } constructor:
                label.Append(constructor.ContainingType.ToMinimalDisplayString(model, position, TypeFormat));
                break;
            case IMethodSymbol { MethodKind: MethodKind.DelegateInvoke } invoke:
                label.Append(Type(invoke.ReturnType, invoke.ReturnsVoid, model, position)).Append(' ').Append(invoke.ContainingType.ToMinimalDisplayString(model, position, TypeFormat));
                break;
            case IMethodSymbol method:
                label.Append(Type(method.ReturnType, method.ReturnsVoid, model, position)).Append(' ').Append(method.ToMinimalDisplayString(model, position, NameFormat));
                break;
            case IPropertySymbol indexer:
                label.Append(Type(indexer.Type, false, model, position)).Append(' ').Append(indexer.ContainingType.ToMinimalDisplayString(model, position, TypeFormat));
                (open, close) = ("[", "]");
                break;
        }
        label.Append(open);
        var documentation = Documentation(member);
        var parameters = new List<ParameterInformation>();
        foreach (var parameter in Parameters(member))
        {
            if (parameters.Count > 0) label.Append(", ");
            var start = label.Length;
            label.Append(parameter.ToMinimalDisplayString(model, position, ParameterFormat));
            parameters.Add(new([start, label.Length], documentation.Parameters.TryGetValue(parameter.Name, out var text) ? Plain(text) : null));
        }
        label.Append(close);
        return new(label.ToString(), documentation.Summary is { } summary ? Plain(summary) : null, parameters);
    }

    private static string Type(ITypeSymbol type, bool isVoid, SemanticModel model, int position) =>
        isVoid ? "void" : type.ToMinimalDisplayString(model, position, TypeFormat);

    private static MarkupContent Plain(string text) => new(text, "plaintext");

    private static (string? Summary, Dictionary<string, string> Parameters) Documentation(ISymbol member)
    {
        var parameters = new Dictionary<string, string>(StringComparer.Ordinal);
        var xml = member.GetDocumentationCommentXml(expandIncludes: true);
        if (string.IsNullOrWhiteSpace(xml)) return (null, parameters);
        XElement root;
        try
        {
            root = XElement.Parse(xml.TrimStart().StartsWith('<') && !xml.TrimStart().StartsWith("<member", StringComparison.Ordinal) ? "<member>" + xml + "</member>" : xml);
        }
        catch (System.Xml.XmlException)
        {
            return (null, parameters);
        }
        foreach (var parameter in root.Elements("param"))
            if (parameter.Attribute("name")?.Value is { } name && Text(parameter) is { Length: > 0 } text) parameters[name] = text;
        var summary = root.Element("summary") is { } element ? Text(element) : null;
        return (string.IsNullOrEmpty(summary) ? null : summary, parameters);
    }

    private static string Text(XElement element)
    {
        var builder = new StringBuilder();
        foreach (var node in element.Nodes())
        {
            switch (node)
            {
                case XText text:
                    builder.Append(text.Value);
                    break;
                case XElement { Name.LocalName: "see" or "seealso" } see:
                    builder.Append(see.Attribute("langword")?.Value ?? ShortName(see.Attribute("cref")?.Value) ?? see.Value);
                    break;
                case XElement { Name.LocalName: "paramref" or "typeparamref" } reference:
                    builder.Append(reference.Attribute("name")?.Value);
                    break;
                case XElement inner:
                    builder.Append(Text(inner));
                    break;
            }
        }
        return string.Join(' ', builder.ToString().Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
    }

    private static string? ShortName(string? cref)
    {
        if (cref is null) return null;
        var name = cref.Length > 2 && cref[1] == ':' ? cref[2..] : cref;
        var parenthesis = name.IndexOf('(');
        if (parenthesis >= 0) name = name[..parenthesis];
        var tick = name.IndexOf('`');
        if (tick >= 0) name = name[..tick];
        return name[(name.LastIndexOf('.') + 1)..];
    }
}
