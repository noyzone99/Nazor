using System.Text.Json;
using Nazor.VisualStudio;

namespace Nazor.LanguageServer.Tests;

public sealed class CodeLensTests
{
    [Fact]
    public async Task Native_csharp_and_razor_results_and_virtual_base_calls_are_preserved()
    {
        var references = new[]
        {
            new NativeCodeLensReference { FilePath = "ComponentBase.cs", LineNumber = 156, ColumnNumber = 8,
                ReferenceLineText = "        OnAfterRender(firstRender);", LongDescription = "ComponentBase.OnAfterRender(bool)", Glyph = 50,
                BeforeReferenceText1 = "var firstRender = true;" },
            new NativeCodeLensReference { FilePath = "Other.cs", ReferenceLineText = "page.Render();" },
            new NativeCodeLensReference { FilePath = "Page.razor", ReferenceLineText = "<TextBlock>@Title</TextBlock>" },
        };
        var mapped = await NativeCodeLensReferences.MapAsync(references, (_, _) => throw new InvalidOperationException("Ordinary sources must not be remapped"), TestContext.Current.CancellationToken);
        Assert.Equal(references, mapped);
        Assert.Same(references[0], mapped[0]);
        Assert.Equal(50, mapped[0].Glyph);
        Assert.Equal("var firstRender = true;", mapped[0].BeforeReferenceText1);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Only_generated_nazor_results_are_mapped_without_changing_native_search_semantics(bool canMap)
    {
        var original = new NativeCodeLensReference { FilePath = "Page.nazor.g.cs", LineNumber = 96, ColumnNumber = 20,
            ReferenceLineText = "__builder.AddContent(1, Title);", ReferenceLength = 5, SpanLength = 5, Glyph = 51 };
        var regular = new NativeCodeLensReference { FilePath = "Caller.cs", ReferenceLineText = "_ = page.Title;" };
        var calls = 0;
        Task<NativeCodeLensReference> Map(NativeCodeLensReference reference, CancellationToken cancellation)
        {
            calls++;
            if (!canMap) return Task.FromResult<NativeCodeLensReference>(null!);
            var found = reference.Copy();
            found.FilePath = "Page.nazor";
            found.LineNumber = 3;
            found.ColumnNumber = found.ReferenceStart = 12;
            found.ReferenceLineText = "<TextBlock>@Title</TextBlock>";
            return Task.FromResult(found);
        }
        var mapped = await NativeCodeLensReferences.MapAsync(new[] { original, regular, original }, Map, TestContext.Current.CancellationToken);
        Assert.Equal(2, calls);
        Assert.Equal(canMap ? 2 : 1, mapped.Count);
        Assert.Contains(regular, mapped);
        Assert.Equal("Page.nazor.g.cs", original.FilePath);
        Assert.DoesNotContain(mapped, r => r.FilePath.EndsWith(".g.cs", StringComparison.Ordinal));
        if (canMap)
        {
            var found = Assert.Single(mapped, r => r.FilePath == "Page.nazor");
            Assert.Equal("Title", found.ReferenceLineText.Substring(found.ReferenceStart, found.ReferenceLength));
            Assert.Equal(original.Glyph, found.Glyph);
        }
    }

    [Fact]
    public void Native_callback_json_contracts_keep_the_tuple_and_reference_fields()
    {
        const string json = """
            {"Item1":"version","Item2":[{"FilePath":"ComponentBase.cs","SpanStart":900,"SpanLength":13,
             "LineNumber":156,"ColumnNumber":8,"ReferenceLineText":"        OnAfterRender(firstRender);",
             "ReferenceStart":8,"ReferenceLength":13,"Glyph":50,"LongDescription":"ComponentBase.Render",
             "BeforeReferenceText1":"before","AfterReferenceText2":"after"}]}
            """;
        var response = JsonSerializer.Deserialize<NativeReferenceLocations>(json)!;
        var reference = Assert.Single(response.Item2);
        Assert.Equal("version", response.Item1);
        Assert.Equal(900, reference.SpanStart);
        Assert.Equal(13, reference.SpanLength);
        Assert.Equal(50, reference.Glyph);
        Assert.Equal("before", reference.BeforeReferenceText1);
        Assert.Equal("after", reference.AfterReferenceText2);
        var count = JsonSerializer.Deserialize<NativeReferenceCount>("{\"Count\":99,\"IsCapped\":true,\"Version\":\"version\"}")!;
        Assert.True(count.IsCapped);
        Assert.Equal(99, count.Count);
    }
}
