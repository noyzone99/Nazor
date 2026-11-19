using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Nazor.LanguageServer.Protocol;
using Nazor.LanguageServer.Workspace;
using Range = Nazor.LanguageServer.Protocol.Range;

namespace Nazor.LanguageServer.Features;

internal static class DocumentSymbols
{
    public static async Task<List<DocumentSymbol>> ComponentAsync(ProjectSnapshot snapshot, ComponentDocument component, CancellationToken cancellation)
    {
        var text = component.Text;
        var name = Path.GetFileNameWithoutExtension(component.Path);
        var children = new List<DocumentSymbol>();
        if (snapshot.Document(component) is { } document && await document.GetSyntaxRootAsync(cancellation) is { } root &&
            root.DescendantNodes().OfType<ClassDeclarationSyntax>().FirstOrDefault(c => c.Parent is BaseNamespaceDeclarationSyntax or CompilationUnitSyntax) is { } type)
        {
            name = type.Identifier.ValueText;
            children.AddRange(Members(component, type.Members));
        }
        foreach (var block in CodeActions.Blocks(text).Where(b => b.Kind == "style"))
        {
            children.Add(new("@style", LspSymbolKind.Namespace, Span(text, block.Start, block.Close + 1), Span(text, block.Start, block.Start + "@style".Length))
            {
                Children = Rules(text, block.Open + 1, block.Close),
            });
        }
        children.Sort((a, b) => a.Range.Start.Line != b.Range.Start.Line ? a.Range.Start.Line - b.Range.Start.Line : a.Range.Start.Character - b.Range.Start.Character);
        return [new(name, LspSymbolKind.Class, Span(text, 0, text.Length), Span(text, 0, 0)) { Children = children }];
    }

    public static List<DocumentSymbol> Rules(string text, int from, int to)
    {
        var symbols = new List<DocumentSymbol>();
        foreach (var block in NssBlocks.At(text, from, to))
        {
            var prelude = block.Prelude(text);
            if (prelude.Length == 0) continue;
            var nameEnd = block.Start + text[block.Start..block.Open].TrimEnd().Length;
            var children = Rules(text, block.Open + 1, block.End - 1);
            symbols.Add(new(prelude, prelude.StartsWith('@') ? LspSymbolKind.Module : LspSymbolKind.Class, Span(text, block.Start, block.End), Span(text, block.Start, nameEnd))
            {
                Children = children.Count > 0 ? children : null,
            });
        }
        return symbols;
    }

    public static IEnumerable<SymbolInformation> Flatten(IEnumerable<DocumentSymbol> symbols, string uri, string? container = null)
    {
        foreach (var symbol in symbols)
        {
            yield return new(symbol.Name, symbol.Kind, new LspLocation(uri, symbol.Range), container);
            foreach (var child in Flatten(symbol.Children ?? [], uri, symbol.Name)) yield return child;
        }
    }

    private static IEnumerable<DocumentSymbol> Members(ComponentDocument component, SyntaxList<MemberDeclarationSyntax> members)
    {
        foreach (var member in members)
        {
            switch (member)
            {
                case BaseFieldDeclarationSyntax field:
                    var kind = field is EventFieldDeclarationSyntax ? LspSymbolKind.Event : LspSymbolKind.Field;
                    foreach (var variable in field.Declaration.Variables)
                        if (Symbol(component, variable.Identifier, field, kind, variable.Identifier.ValueText, field.Declaration.Type.ToString()) is { } symbol)
                            yield return symbol;
                    break;
                case PropertyDeclarationSyntax property when Symbol(component, property.Identifier, property, LspSymbolKind.Property, property.Identifier.ValueText, property.Type.ToString()) is { } symbol:
                    yield return symbol;
                    break;
                case EventDeclarationSyntax @event when Symbol(component, @event.Identifier, @event, LspSymbolKind.Event, @event.Identifier.ValueText, @event.Type.ToString()) is { } symbol:
                    yield return symbol;
                    break;
                case MethodDeclarationSyntax method when Symbol(component, method.Identifier, method, LspSymbolKind.Method,
                    $"{method.Identifier.ValueText}({Parameters(method.ParameterList)})", method.ReturnType.ToString()) is { } symbol:
                    yield return symbol;
                    break;
                case ConstructorDeclarationSyntax constructor when Symbol(component, constructor.Identifier, constructor, LspSymbolKind.Constructor,
                    $"{constructor.Identifier.ValueText}({Parameters(constructor.ParameterList)})", null) is { } symbol:
                    yield return symbol;
                    break;
                case EnumDeclarationSyntax @enum when Symbol(component, @enum.Identifier, @enum, LspSymbolKind.Enum, @enum.Identifier.ValueText, null,
                    [.. @enum.Members.Select(m => Symbol(component, m.Identifier, m, LspSymbolKind.EnumMember, m.Identifier.ValueText, null)).OfType<DocumentSymbol>()]) is { } symbol:
                    yield return symbol;
                    break;
                case TypeDeclarationSyntax nested when Symbol(component, nested.Identifier, nested, nested switch
                {
                    InterfaceDeclarationSyntax => LspSymbolKind.Interface,
                    StructDeclarationSyntax or RecordDeclarationSyntax { ClassOrStructKeyword.ValueText: "struct" } => LspSymbolKind.Struct,
                    _ => LspSymbolKind.Class,
                }, nested.Identifier.ValueText, null, [.. Members(component, nested.Members)]) is { } symbol:
                    yield return symbol;
                    break;
            }
        }
    }

    private static string Parameters(ParameterListSyntax parameters) => string.Join(", ", parameters.Parameters.Select(p => p.Type?.ToString()));

    private static DocumentSymbol? Symbol(ComponentDocument component, SyntaxToken identifier, SyntaxNode declaration, int kind, string name, string? detail,
        IReadOnlyList<DocumentSymbol>? children = null)
    {
        if (component.ToOriginal(identifier.SpanStart, identifier.Span.Length) is not { } selection) return null;
        var whole = component.ToOriginal(declaration.SpanStart, declaration.Span.Length) ?? selection;
        var text = component.Text;
        return new(name, kind, Span(text, whole.Start, whole.Start + whole.Length), Span(text, selection.Start, selection.Start + selection.Length))
        {
            Detail = detail,
            Children = children is { Count: > 0 } ? children : null,
        };
    }

    private static Range Span(string text, int start, int end) => TextPositions.Range(text, start, end);
}
