using Nazor.Compiler;
using Nazor.Nss;
using Nazor.Rendering;
using Nazor.Style;
using Nazor.Testing;

namespace Nazor.Compiler.Tests;

public sealed class ComponentSelectorTests(CompilerFixture fixture) : IClassFixture<CompilerFixture>
{
    [Fact]
    public void Qualified_rules_rematch_hover_media_and_unrelated_renders()
    {
        var compiler = fixture.Compiler;
        var assembly = compiler.Load(compiler.Generate([Build.Nazor("SelectorPage"), Build.Nazor("SelectorClock"), Build.Nazor("SelectorDetails"), Build.Nazor("SelectorShell")]));
        var pageType = assembly.ComponentType("SelectorPage");
        StyleRegistry.RegisterScoped(pageType, () => NssCompiler.Compile("state", "@media (min-width: 700px) { SelectorClock .clock-panel:hover TextBlock { padding: 19px; } }").StyleSheet);
        using var host = new TestHost();
        using var engine = new StyleEngine(host.Renderer);
        var root = host.Render(pageType);
        var panel = root.Elements("Container").First(e => e.Class == "clock-panel");
        var local = root.Elements("TextBlock").First(e => e.Class == "clock-time");
        var detail = root.Elements("TextBlock").First(e => e.Class == "detail");
        Assert.Equal(Length.Zero, engine.GetStyle(local).Get(StyleProperties.PaddingLeft));
        panel.SetState(ElementStates.Hover, true);
        Assert.Equal(Length.Px(19), engine.GetStyle(local).Get(StyleProperties.PaddingLeft));
        Assert.Equal(Length.Zero, engine.GetStyle(detail).Get(StyleProperties.PaddingLeft));
        engine.Viewport = new(600, 400);
        Assert.Equal(Length.Zero, engine.GetStyle(local).Get(StyleProperties.PaddingLeft));
        engine.Viewport = new(800, 400);
        host.Renderer.RenderOnHotReload();
        Assert.Equal(Length.Px(19), engine.GetStyle(local).Get(StyleProperties.PaddingLeft));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Referenced_components_keep_their_declarations_in_global_and_scoped_sheets(bool scoped)
    {
        var path = Path.GetFullPath("LibrarySelector.nazor");
        var compiler = new NazorCompiler(new()
        {
            References = [Build.Core, Build.Controls, Build.Style, Build.Library],
            SourceText = p => p == path ? "@using Nazor.Sample.Components\n<LibraryClock />\n<TextBlock>Outside</TextBlock>\n<ExternalPanel />" : null,
        });
        var assembly = compiler.Load(compiler.Generate([path]));
        var pageType = assembly.ComponentType("LibrarySelector");
        var sheet = NssCompiler.Compile("app", "LibraryClock TextBlock { padding: 8px; } ExternalPanel TextBlock { padding: 5px; } ExternalPanel ::deep TextBlock { margin: 7px; }").StyleSheet;
        if (scoped) StyleRegistry.RegisterScoped(pageType, () => sheet);
        using var host = new TestHost();
        using var engine = new StyleEngine(host.Renderer) { Application = scoped ? null : sheet };
        var root = host.Render(pageType);
        ElementNode Text(string value) => root.Elements("TextBlock").Single(e => e.Text() == value);
        Assert.Equal(Length.Px(8), engine.GetStyle(Text("Library Clock")).Get(StyleProperties.PaddingLeft));
        Assert.Equal(Length.Zero, engine.GetStyle(Text("Outside")).Get(StyleProperties.PaddingLeft));
        Assert.Equal(Length.Px(5), engine.GetStyle(Text("External panel")).Get(StyleProperties.PaddingLeft));
        Assert.Equal(Length.Zero, engine.GetStyle(Text("External details")).Get(StyleProperties.PaddingLeft));
        Assert.Equal(SizeValue.Fixed(Length.Px(7)), engine.GetStyle(Text("External details")).Get(StyleProperties.MarginLeft));
    }

    [Theory]
    [InlineData("SelectorClock TextBlock", 2, 0, 0)]
    [InlineData("SelectorClock .clock-time", 2, 0, 0)]
    [InlineData("SelectorClock ::deep TextBlock", 3, 3, 2)]
    [InlineData("SelectorClock SelectorDetails TextBlock", 0, 2, 0)]
    [InlineData("SelectorClock SelectorClock TextBlock", 1, 0, 0)]
    public void Scoped_component_selectors_preserve_instance_boundaries_and_projected_ownership(string selector, int times, int details, int projected)
    {
        var compiler = fixture.Compiler;
        var assembly = compiler.Load(compiler.Generate([Build.Nazor("SelectorPage"), Build.Nazor("SelectorClock"), Build.Nazor("SelectorDetails"), Build.Nazor("SelectorShell")]));
        var pageType = assembly.ComponentType("SelectorPage");
        var compiled = NssCompiler.Compile("selectors", selector + " { padding: 17px; }");
        Assert.True(compiled.Success, string.Join("\n", compiled.Diagnostics));
        var sheet = compiled.StyleSheet;
        Assert.Equal(sheet.Describe(), StyleSheetJson.Deserialize(StyleSheetJson.Serialize(sheet)).Describe());
        StyleRegistry.RegisterScoped(pageType, () => sheet);
        using var host = new TestHost();
        using var engine = new StyleEngine(host.Renderer);
        var root = host.Render(pageType);
        var texts = root.Elements("TextBlock").ToArray();
        bool Selected(ElementNode e) => engine.GetStyle(e).Get(StyleProperties.PaddingLeft) == Length.Px(17);
        Assert.Equal(times, texts.Count(e => e.Class == "clock-time" && Selected(e)));
        Assert.Equal(details, texts.Count(e => e.Class == "detail" && Selected(e)));
        Assert.Equal(projected, texts.Count(e => e.Class == "projected" && Selected(e)));
        Assert.Equal(selector is "SelectorClock TextBlock" or "SelectorClock ::deep TextBlock" ? 1 : 0,
            texts.Count(e => e.Class == "clock-projected" && Selected(e)));
        Assert.DoesNotContain(texts, e => e.Text() == "hidden" && Selected(e));
        Assert.DoesNotContain(texts, e => e.Class == "own" && Selected(e));
        Assert.Equal(4, root.Elements("Container").Count(e => e.Class == "clock-panel"));
        foreach (var text in texts.Where(Selected)) Assert.Contains(engine.Trace(text).Rules, r => r.Selector == selector);

        sheet.Replace(NssCompiler.Compile("selectors", "SelectorClock ::deep TextBlock { padding: 3px; }").StyleSheet);
        Assert.Equal(Length.Px(3), engine.GetStyle(texts.First(e => e.Class == "detail")).Get(StyleProperties.PaddingLeft));
    }

    [Fact]
    public void A_component_qualifier_needs_no_visual_ancestor_and_does_not_style_the_component_itself()
    {
        var directory = Path.GetFullPath("selector-fragment");
        var page = Path.Combine(directory, "Page.nazor");
        var clock = Path.Combine(directory, "Clock.nazor");
        var documents = new Dictionary<string, string>
        {
            [page] = "<Clock />\n<Clock />\n@style { Clock TextBlock { padding: 5px; } Clock { margin: 9px; } }",
            [clock] = "<TextBlock>One</TextBlock><TextBlock>Two</TextBlock>",
        };
        var local = new NazorCompiler(new() { ProjectDirectory = directory, References = [Build.Core, Build.Controls, Build.Style], SourceText = p => documents.GetValueOrDefault(p) });
        var assembly = local.Load(local.Generate([page, clock]));
        using var host = new TestHost();
        using var engine = new StyleEngine(host.Renderer);
        var root = host.Render(assembly.ComponentType("Page"));
        Assert.Equal(4, root.Nodes.Count);
        Assert.All(root.Elements("TextBlock"), e =>
        {
            Assert.Equal(Length.Px(5), engine.GetStyle(e).Get(StyleProperties.PaddingLeft));
            Assert.Equal(SizeValue.Fixed(Length.Zero), engine.GetStyle(e).Get(StyleProperties.MarginLeft));
        });
    }
}
