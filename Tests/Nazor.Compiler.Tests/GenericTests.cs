using Nazor.Style;
using Nazor.Testing;

namespace Nazor.Compiler.Tests;

public sealed class GenericTests
{
    [Fact]
    public void Generic_components_infer_or_accept_types_render_templates_and_keep_style_scopes()
    {
        var directory = Build.Fixture("Generics");
        var compiler = new Nazor.Compiler.NazorCompiler(new()
        {
            ProjectDirectory = directory, RootNamespace = "Generics",
            References = [Build.Core, Build.Controls, Build.Style],
        });
        var generated = compiler.Generate(Directory.GetFiles(directory, "*.nazor"));
        var assembly = compiler.Load(generated);
        using var host = new TestHost();
        var root = host.Render(assembly.ComponentType("App"));
        Assert.Equal("12Int32onetwoonetwo", root.Text());
        var list = assembly.GetType("Generics.ListView`1")!;
        Assert.All(root.Elements("Container").Where(e => e.Class == "generic-list"), element => Assert.Equal(list, element.Scope));
        Assert.NotNull(StyleRegistry.GetScoped(list));
        Assert.All(root.Elements("TextBlock").Where(e => e.Text() != "Int32"), element => Assert.Equal(assembly.ComponentType("App"), element.Scope));
    }

    [Fact]
    public void Generic_components_in_compiled_library_preserve_constraints_and_templates()
    {
        var compiler = Build.Compiler(Build.Library);
        var assembly = compiler.Load([compiler.Generate(Build.Nazor("GenericLibrary"))]);
        using var host = new TestHost();
        var root = host.Render(assembly.ComponentType("GenericLibrary"));
        Assert.Equal("library", root.Text());
        Assert.NotNull(StyleRegistry.GetScoped(typeof(Nazor.Sample.Components.GenericCard<string>)));
        Assert.DoesNotContain(assembly.GetReferencedAssemblies(), a => a.Name!.StartsWith("Microsoft.AspNetCore", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Cascading_markup_bind_after_get_set_inherits_implements_and_attributes_work()
    {
        var directory = Build.Fixture("Phase17");
        var compiler = new Nazor.Compiler.NazorCompiler(new()
        {
            ProjectDirectory = directory, RootNamespace = "Phase17",
            References = [Build.Core, Build.Controls, Build.Style],
            CSharpSources = [Path.Combine(directory, "Support.cs")],
        });
        var assembly = compiler.Load(compiler.Generate(Directory.GetFiles(directory, "*.nazor")));
        using var host = new TestHost();
        var root = host.Render(assembly.ComponentType("App"));
        Assert.Contains("value:initial", root.Text());
        var afterClick = host.Dispatcher.InvokeAsync(() => host.ClickAsync(root.Elements("Button").First()));
        host.RunUntil(() => afterClick.IsCompleted);
        await afterClick;
        Assert.Contains("value:initial!", root.Text());
        Assert.Contains("after:initial!", root.Text());
        await host.ClickAsync(root.Elements("Button").Last());
        Assert.Contains("value:INITIAL!!", root.Text());
        Assert.Contains("set:INITIAL!!", root.Text());
        Assert.Contains("inherited", root.Text());
        var derived = assembly.ComponentType("Inherited");
        Assert.Contains(derived.GetCustomAttributes(false), a => a.GetType().Name == "DemoTagAttribute");
        host.Renderer.RemoveRootComponent(root);
        Assert.Equal(1, derived.GetProperty("DisposeCount")!.GetValue(null));
    }

    [Theory]
    [InlineData("<ListView />", "RZ10001")]
    [InlineData("<GenericCard TItem=\"int\" Item=\"1\"><ItemTemplate><TextBlock>@context</TextBlock></ItemTemplate></GenericCard>", "CS0452")]
    public void Missing_inference_and_invalid_constraints_report_diagnostics(string markup, string expected)
    {
        var directory = Build.Fixture("Generics");
        var path = Path.Combine(directory, "Invalid.nazor");
        var compiler = new Nazor.Compiler.NazorCompiler(new()
        {
            ProjectDirectory = directory, RootNamespace = "Generics", References = [Build.Core, Build.Controls, Build.Style, Build.Library],
            SourceText = file => file == path ? "@using Nazor.Sample.Components\n" + markup : null,
        });
        var exception = Build.Fails(() => compiler.Load(compiler.Generate([Path.Combine(directory, "ListView.nazor"), path])));
        Assert.Contains(exception.Diagnostics, d => d.Id == expected && d.Path.EndsWith("Invalid.nazor", StringComparison.Ordinal));
    }
}
