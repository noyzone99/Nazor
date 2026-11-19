using System.Xml.Linq;

namespace Nazor.Compiler.Tests;

public sealed class ComponentMetadataTests
{
    [Fact]
    public void Preserves_nested_generic_and_CSharp_components_but_not_unrelated_types()
    {
        var references = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator)
            .Prepend(typeof(ComponentMetadataTests).Assembly.Location).Prepend(typeof(ComponentBase).Assembly.Location);
        var xml = XDocument.Parse(ComponentMetadata.Generate(references));
        var types = xml.Descendants("assembly").Single(a => (string?)a.Attribute("fullname") == "Nazor.Compiler.Tests")
            .Elements("type").ToDictionary(t => (string)t.Attribute("fullname")!, t => (string?)t.Attribute("preserve"));
        Assert.Equal("all", types["Nazor.Compiler.Tests.ComponentMetadataTests/GenericPage`1"]);
        Assert.Equal("all", types["Nazor.Compiler.Tests.ComponentMetadataTests/PrivatePage"]);
        Assert.Equal("all", types["Nazor.Compiler.Tests.ComponentMetadataTests/PlainParameters"]);
        Assert.DoesNotContain("Nazor.Compiler.Tests.ComponentMetadataTests/Unrelated", types.Keys);
        var value = xml.Descendants("type").Single(t => (string?)t.Attribute("fullname") == "Nazor.Compiler.Tests.ComponentMetadataTests/GenericValue");
        Assert.Equal("System.Void .ctor()", (string?)value.Element("method")?.Attribute("signature"));
    }

    private class GenericPage<T> : ComponentBase
    {
        [Inject] private IServiceProvider Services { get; set; } = null!;
        [Parameter] public T? Value { get; set; }
    }
    private sealed class PrivatePage : GenericPage<int>;
    private sealed class Unrelated;
    private sealed class PlainParameters
    {
        [Parameter] public int Value { get; set; }
        [Inject] private IServiceProvider Services { get; set; } = null!;
    }
    private struct GenericValue
    {
        public int Number;
        public GenericValue() => Number = 9;
    }
}
