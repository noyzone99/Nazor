using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Nazor.LanguageServer.Features;

internal static class ClassicSnippets
{
    private enum Place { Member, Statement }

    private static readonly Dictionary<string, string> Descriptions = new(StringComparer.Ordinal)
    {
        ["propfull"] = "Code snippet for property and backing field",
        ["indexer"] = "Code snippet for indexer",
        ["try"] = "Code snippet for try catch",
        ["tryf"] = "Code snippet for try finally",
        ["switch"] = "Code snippet for switch statement",
    };

    private static readonly Dictionary<string, (Place Place, string Body)> Snippets = new(StringComparer.Ordinal)
    {
        ["propfull"] = (Place.Member, """
            private ${1:int} ${2:myVar};

            public ${1:int} ${3:MyProperty}
            {
                get { return ${2:myVar}; }
                set { ${2:myVar} = value; }
            }$0
            """),
        ["indexer"] = (Place.Member, """
            public ${1:object} this[${2:int} index]
            {
                get { $0 }
                set { }
            }
            """),
        ["try"] = (Place.Statement, """
            try
            {
                $0
            }
            catch (${1:Exception})
            {
                throw;
            }
            """),
        ["tryf"] = (Place.Statement, """
            try
            {
                $0
            }
            finally
            {
            }
            """),
        ["switch"] = (Place.Statement, """
            switch (${1:switch_on})
            {
                $0
                default:
                    break;
            }
            """),
    };

    public static string? Find(string shortcut, SyntaxToken token)
    {
        if (!Snippets.TryGetValue(shortcut, out var snippet) || PlaceOf(token) != snippet.Place) return null;
        return snippet.Body.ReplaceLineEndings("\n");
    }

    public static IEnumerable<(string Shortcut, string Description)> At(SyntaxToken token) =>
        PlaceOf(token) is { } place ? Snippets.Where(s => s.Value.Place == place).Select(s => (s.Key, Descriptions[s.Key])) : [];

    private static Place? PlaceOf(SyntaxToken token)
    {
        for (var node = token.Parent; node is not null && node.SpanStart == token.SpanStart; node = node.Parent)
        {
            if (node is StatementSyntax { Parent: BlockSyntax or SwitchSectionSyntax }) return Place.Statement;
            if (node is MemberDeclarationSyntax { Parent: TypeDeclarationSyntax }) return Place.Member;
        }
        return null;
    }
}
