using System.Xml.Linq;
using Nazor.VisualStudio;

namespace Nazor.Tooling.Tests;

public sealed class SnippetConversionTests
{
    private static readonly XNamespace Snippets = "http://schemas.microsoft.com/VisualStudio/2005/CodeSnippet";

    private static (string Code, Dictionary<string, string> Literals) Convert(string snippet)
    {
        var document = XDocument.Parse(SnippetConversion.ToVisualStudio(snippet));
        Assert.Equal(Snippets + "CodeSnippet", document.Root!.Name);
        Assert.Equal("CSharp", document.Descendants(Snippets + "Code").Single().Attribute("Language")!.Value);
        var code = document.Descendants(Snippets + "Code").Single().Value;
        var literals = document.Descendants(Snippets + "Literal").ToDictionary(l => l.Element(Snippets + "ID")!.Value, l => l.Element(Snippets + "Default")!.Value);
        return (code, literals);
    }

    [Fact]
    public void Placeholders_become_fields_with_their_defaults()
    {
        var (code, literals) = Convert("public ${1:int} ${2:MyProperty} { get; set; }$0");
        Assert.Equal("public $field1$ $field2$ { get; set; }$end$", code);
        Assert.Equal(new Dictionary<string, string> { ["field1"] = "int", ["field2"] = "MyProperty" }, literals);
    }

    [Fact]
    public void A_placeholder_repeated_is_one_field()
    {
        var (code, literals) = Convert("for (int ${1:i} = 0; ${1:i} < ${2:length}; ${1:i}++)\r\n{\r\n    $0\r\n}");
        Assert.Equal("for (int $field1$ = 0; $field1$ < $field2$; $field1$++)\n{\n    $end$\n}", code);
        Assert.Equal(["field1", "field2"], literals.Keys.Order());
        Assert.Equal("i", literals["field1"]);
    }

    [Fact]
    public void Tab_stops_escapes_and_the_end()
    {
        var (code, literals) = Convert("Console.WriteLine($\"{x}\\$\"$1);\\}");
        Assert.Equal("Console.WriteLine($$\"{x}$$\"$field1$);}$end$", code);
        Assert.Equal("1", literals["field1"]);
        Assert.Equal("x$end$", Convert("x").Code);
        Assert.Equal("$end$x", Convert("$0x").Code);
    }
}
