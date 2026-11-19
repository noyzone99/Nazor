using Nazor.Compiler;
using Nazor.Rendering;
using Nazor.Style;
using Nazor.Testing;

namespace Nazor.Compiler.Tests;

public sealed class StyleIntegrationTests(CompilerFixture fixture) : IClassFixture<CompilerFixture>
{
    private readonly NazorCompiler compiler = fixture.Compiler;

    private static string Styles(RootComponent root, StyleEngine engine) =>
        string.Join("\n", root.Elements().Select(e => $"{e}: {engine.GetStyle(e).Describe()}"));

    [Fact]
    public void Large_generated_stylesheets_preserve_rules_without_a_single_large_initializer()
    {
        var directory = Directory.CreateTempSubdirectory("nazor-large-styles-");
        try
        {
            var text = string.Join('\n', Enumerable.Range(0, 256).Select(index =>
                $"@media (min-width: {index}px) {{ .card{index} {{ --surface: #abcdef; color: var(--surface); padding: {index}px; }} }}")) +
                "\n.large { " + string.Join(' ', Enumerable.Range(0, 80).Select(index => $"--token{index}: var(--surface, #abcdef);")) + " }" +
                "\n@keyframes pulse { from { opacity: 0.2; } to { opacity: 1; } }";
            var source = Path.Combine(directory.FullName, "large.nss");
            File.WriteAllText(source, text);
            var emitter = new NazorCompiler(new NazorCompilerOptions
            {
                RootNamespace = "Large.App", ProjectDirectory = directory.FullName, References = [Build.Core, Build.Controls, Build.Style],
            });
            var path = Path.Combine(directory.FullName, "NazorStyleSheets.g.cs");
            File.WriteAllText(path, emitter.GenerateStyleSheets([source]));
            var type = emitter.Load([], path).GetType("Large.App.StyleSheets")!;
            var property = type.GetProperty("Large")!;
            var sheet = (StyleSheet)property.GetValue(null)!;
            var expected = Nazor.Nss.NssCompiler.Compile("large.nss", text, "large.nss");
            Assert.True(expected.Success);
            Assert.Equal(StyleSheetJson.Serialize(expected.StyleSheet), StyleSheetJson.Serialize(sheet));
            Assert.Same(sheet, property.GetValue(null));
            var factories = type.GetMethods(System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
            Assert.True(factories.Length >= 257);
            Assert.All(factories, method => Assert.True(method.GetMethodBody()!.GetILAsByteArray()!.Length < 10000, method.Name));
        }
        finally { directory.Delete(recursive: true); }
    }

    [Fact]
    public void Deep_and_platform_queries_compile_into_scoped_runtime_styles()
    {
        var assembly = compiler.Load(compiler.Generate([Build.Nazor("DeepParent"), Build.Nazor("DeepChild")]));
        using var host = new TestHost();
        using var engine = new StyleEngine(host.Renderer);
        var parent = host.Render(assembly.ComponentType("DeepParent"));
        var outside = host.Render(assembly.ComponentType("DeepChild"));
        var button = parent.Elements("Button").Single();
        Assert.Equal(Length.Zero, engine.GetStyle(button).Get(StyleProperties.PaddingLeft));
        engine.Preferences = engine.Preferences with { Platform = PlatformKind.Windows };
        Assert.Equal(Length.Px(17), engine.GetStyle(button).Get(StyleProperties.PaddingLeft));
        Assert.Equal(Length.Zero, engine.GetStyle(outside.Elements("Button").Single()).Get(StyleProperties.PaddingLeft));
    }

    [Fact]
    public void Inline_and_sidecar_styles_resolve_to_the_same_computed_values()
    {
        var assembly = compiler.Load(compiler.Generate([Build.Nazor("Counter"), Build.Nazor("CounterSidecar")]));
        using var inlineHost = new TestHost();
        using var sidecarHost = new TestHost();
        using var inlineEngine = new StyleEngine(inlineHost.Renderer);
        using var sidecarEngine = new StyleEngine(sidecarHost.Renderer);
        var inline = inlineHost.Render(assembly.ComponentType("Counter"));
        var sidecar = sidecarHost.Render(assembly.ComponentType("CounterSidecar"));

        Assert.Equal(Styles(sidecar, sidecarEngine), Styles(inline, inlineEngine));
        var container = inline.Single("Container");
        Assert.Equal(Display.Flex, inlineEngine.GetStyle(container).Display);
        Assert.Equal(FlexDirection.Column, inlineEngine.GetStyle(container).Get(StyleProperties.FlexDirection));
        Assert.Equal(Length.Px(8), inlineEngine.GetStyle(container).Get(StyleProperties.RowGap));
        Assert.Equal(Length.Px(16), inlineEngine.GetStyle(container).Get(StyleProperties.PaddingLeft));
    }

    [Fact]
    public void Counter_button_takes_the_hover_background_from_its_scoped_stylesheet()
    {
        var assembly = compiler.Load([compiler.Generate(Build.Nazor("Counter"))]);
        using var host = new TestHost();
        using var engine = new StyleEngine(host.Renderer);
        var root = host.Render(assembly.ComponentType("Counter"));
        var button = root.Single("Button");

        Assert.Equal(Color.FromRgb(0x30, 0x30, 0x30), engine.GetStyle(button).BackgroundColor);
        var transition = Assert.Single(engine.GetStyle(button).Get(StyleProperties.Transition).Items);
        Assert.Equal(("background-color", TimeSpan.FromMilliseconds(200)), (transition.Property, transition.Duration));

        button.SetState(ElementStates.Hover, true);
        Assert.Equal(Color.FromRgb(0, 0x78, 0xd4), engine.GetStyle(button).BackgroundColor);
        Assert.Equal(Color.Transparent, engine.GetStyle(root.Single("TextBlock")).BackgroundColor);
    }

    [Fact]
    public void Nss_errors_are_reported_at_their_position_in_the_nazor_file()
    {
        var error = Build.Fails(() => compiler.Generate(Build.Nazor("InvalidStyle")));
        Assert.Contains(error.Diagnostics, d => d.Id == "NZS101" && d.Line == 4 && d.Column == 10 && d.Path.EndsWith("InvalidStyle.nazor"));
        Assert.Contains(error.Diagnostics, d => d.Id == "NZS102" && d.Line == 4 && d.Column == 30 && d.Message.Contains("'pt'"));
    }

    [Fact]
    public void Components_with_styles_require_nazor_style()
    {
        var withoutStyle = new NazorCompiler(new NazorCompilerOptions { References = [Build.Core] });
        var error = Build.Fails(() => withoutStyle.Generate(Build.Nazor("StyleOnly")));
        Assert.Contains(error.Diagnostics, d => d.Id == "NZC020");
    }

    [Fact]
    public void Standalone_stylesheets_become_properties_of_the_StyleSheets_class()
    {
        var themes = new NazorCompiler(new NazorCompilerOptions
        {
            RootNamespace = "Sample.App",
            ProjectDirectory = Build.Fixture(""),
            References = [Build.Core, Build.Controls, Build.Style],
        });
        var code = themes.GenerateStyleSheets([Build.Fixture("Themes/light.nss"), Build.Fixture("Themes/high-contrast.nss")]);
        var path = Path.Combine(Path.GetTempPath(), $"NazorStyleSheets.{Guid.NewGuid():N}.g.cs");
        File.WriteAllText(path, code);
        try
        {
            var assembly = themes.Load([], path);
            var type = assembly.GetType("Sample.App.StyleSheets")!;
            var light = (StyleSheet)type.GetProperty("Light")!.GetValue(null)!;
            var contrast = (StyleSheet)type.GetProperty("HighContrast")!.GetValue(null)!;
            Assert.Equal("Container (0,0,1) { --surface: #ffffff; --text: #202020 }\n", light.Describe());
            Assert.Equal("Themes/high-contrast.nss", contrast.Source);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Emitted_values_of_every_type_compile_to_the_same_sheet()
    {
        const string text = """
            .all {
                display: grid; grid-template-columns: 10% repeat(auto-fill, minmax(20px, 1fr)) 30px; grid-template-rows: auto fit-content(40px);
                grid-template-areas: "a b" "c ."; grid-auto-rows: min-content 2fr; grid-area: a / 2 / span 3; grid-auto-flow: column dense;
                order: -1; flex-wrap: wrap-reverse; position: absolute; inset: 1px auto; z-index: 3; aspect-ratio: 16 / 9; width: fit-content;
                border-radius: 4px 50% / 2px; border-style: dashed dotted; border-color: red currentColor; outline: 2px solid blue;
                box-shadow: inset 0 1px 2px -1px rgba(0, 0, 0, 0.5), 3px 3px;
                background: linear-gradient(to top right, red, transparent 40% 60%, blue), radial-gradient(circle 10px at left 20%, red, blue) #fff;
                text-decoration: underline line-through wavy #00f 2px; text-overflow: ellipsis; letter-spacing: -1px; text-transform: uppercase;
                white-space: pre-wrap; overflow-wrap: anywhere; place-items: center start;
                padding: 1em 2rem calc(100% - 1em) clamp(1px, 2vw, 3vmax); min-width: min(10px, 5vh); max-height: calc((100% - 2px) / 3);
                font-size: 120%; line-height: 150%; margin-left: max(1vmin, calc(2 * (1em - 1px)));
                transform: translate(10%, calc(1em - 2px)) scale(1.5, 2) rotate(-45deg) skew(1deg, 2deg) matrix(1, 0, 0, 1, 2, 3); transform-origin: right 10px;
            }
            @media (prefers-color-scheme: dark) and (min-width: 20em), (prefers-reduced-motion) { .all { color: white; } }
            @keyframes pulse { from { opacity: 0.2; transform: scale(0.5); } 40%, 60% { opacity: var(--peak, 1); animation-timing-function: ease-out; } to { width: 2em; } }
            .all { animation: pulse 1s infinite alternate both, other 2s cubic-bezier(0.1, 0.2, 0.3, 0.4) -1s 0.5 paused; }
            .all { background: url(images/a.png) right 10px / cover no-repeat, url("b c.svg") 0 0 / 50% auto repeat-x, linear-gradient(red, blue) #fff; object-fit: cover; object-position: left 20%; }
            """;
        var directory = Directory.CreateTempSubdirectory("nazor-values-");
        try
        {
            File.WriteAllText(Path.Combine(directory.FullName, "all.nss"), text);
            var emitter = new NazorCompiler(new NazorCompilerOptions
            {
                RootNamespace = "Values.App", ProjectDirectory = directory.FullName, References = [Build.Core, Build.Controls, Build.Style],
            });
            var path = Path.Combine(directory.FullName, "NazorStyleSheets.g.cs");
            File.WriteAllText(path, emitter.GenerateStyleSheets([Path.Combine(directory.FullName, "all.nss")]));
            var sheet = (StyleSheet)emitter.Load([], path).GetType("Values.App.StyleSheets")!.GetProperty("All")!.GetValue(null)!;
            var expected = Nazor.Nss.NssCompiler.Compile("all.nss", text, "all.nss");
            Assert.True(expected.Success, string.Join('\n', expected.Diagnostics));
            Assert.Equal(expected.StyleSheet.Describe(), sheet.Describe());
        }
        finally
        {
            directory.Delete(recursive: true);
        }
    }

    [Fact]
    public void Component_sheets_are_named_by_their_project_relative_path()
    {
        var project = Build.Fixture("Project");
        var projectCompiler = new NazorCompiler(new NazorCompilerOptions
        {
            RootNamespace = "ProjectSample",
            ProjectDirectory = project,
            References = [Build.Core, Build.Controls, Build.Style],
            CSharpSources = [Path.Combine(project, "Components", "Badge.nazor.cs")],
        });
        var assembly = projectCompiler.Load([projectCompiler.Generate(Path.Combine(project, "Components", "Badge.nazor"))]);
        var badge = StyleRegistry.GetScoped(assembly.ComponentType("Badge"));
        Assert.NotNull(badge);
        Assert.Equal("Components/Badge.nazor", badge.Source);
        Assert.Equal(".badge (0,1,0) { padding-top: 4px; padding-right: 4px; padding-bottom: 4px; padding-left: 4px }\n", badge.Describe());

        var counter = StyleRegistry.GetScoped(compiler.Load([compiler.Generate(Build.Nazor("Counter"))]).ComponentType("Counter"));
        Assert.Equal("Counter.nazor", counter!.Source);
    }
}
