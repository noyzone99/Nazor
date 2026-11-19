using Nazor.Compiler;
using Nazor.Style;
using Nazor.Testing;

namespace Nazor.Compiler.Tests;

public sealed class SharedNamespaceTests
{
    [Fact]
    public async Task Control_parts_preserve_caller_owned_scoped_styles()
    {
        var path = Path.GetFullPath("ScopedControlsConsumer.nazor");
        var compiler = new NazorCompiler(new()
        {
            References = [Build.Core, Build.Controls, Build.Style],
            SourceText = file => file == path ? """
                <NumberField Class="quantity" Value="2" />
                <MenuButton Class="commands"><Menu><MenuItem>Save</MenuItem></Menu></MenuButton>
                @style {
                    .quantity { padding: 13px; }
                    .commands Menu { padding: 17px; }
                }
                """ : null,
        });
        var assembly = compiler.Load([compiler.Generate(path)]);
        var page = assembly.ComponentType("ScopedControlsConsumer");
        using var host = new TestHost();
        using var styles = new StyleEngine(host.Renderer);
        var root = host.Render(page);
        Assert.Equal(Length.Px(13), styles.GetStyle(root.Single("NumberField")).Get(StyleProperties.PaddingLeft));
        await host.ClickAsync(root.Single("MenuButton"));
        Assert.Equal(Length.Px(17), styles.GetStyle(root.Single("Menu")).Get(StyleProperties.PaddingLeft));
        Assert.Equal(page, root.Single("NumberField").Scope);
        Assert.Equal(page, root.Single("Menu").Scope);
    }

    [Fact]
    public void Components_in_separate_assemblies_share_an_import_without_colliding_stylesheet_catalogs()
    {
        var path = Path.GetFullPath("SharedNamespaceConsumer.nazor");
        var compiler = new NazorCompiler(new()
        {
            RootNamespace = "Nazor.Sample.Consumer",
            References = [Build.Core, Build.Controls, Build.Style, Build.Library],
            SourceText = file => file == path ? """
                <SharedNamespaceCard Item="42">
                    <ItemTemplate Context="item"><TextBlock Class="shared-library-content">@item</TextBlock></ItemTemplate>
                </SharedNamespaceCard>
                <Button>Base</Button>
                """ : null,
        });
        var assembly = compiler.Load([compiler.Generate(path)]);
        using var host = new TestHost();
        using var styles = new StyleEngine(host.Renderer)
        {
            Application = Nazor.Sample.Components.StyleSheets.SharedLibrary,
        };
        var root = host.Render(assembly.ComponentType("SharedNamespaceConsumer"));
        Assert.Equal("42Base", root.Text());
        Assert.NotEqual(typeof(Nazor.Controls.Button).Assembly, typeof(Nazor.Controls.SharedNamespaceCard<int>).Assembly);
        Assert.Equal(Length.Px(11), styles.GetStyle(root.Elements("Container").Single()).Get(StyleProperties.PaddingLeft));
        Assert.Equal(SizeValue.Fixed(Length.Px(7)), styles.GetStyle(root.Elements("TextBlock").Single()).Get(StyleProperties.MarginLeft));
        Assert.NotNull(Nazor.Controls.StyleSheets.Default);
    }
}
