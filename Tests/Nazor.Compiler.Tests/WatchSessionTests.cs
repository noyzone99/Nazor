using System.Text;
using System.Text.Json.Nodes;
using Nazor.Compiler;
using Nazor.Compiler.Watch;
using Nazor.Style;

namespace Nazor.Compiler.Tests;

public sealed class WatchSessionTests : IDisposable
{
    private readonly string directory = Directory.CreateTempSubdirectory("nazor-watch-").FullName;
    private readonly string output;
    private readonly string theme;
    private readonly string badge;
    private readonly string app;

    public WatchSessionTests()
    {
        Copy(Build.Fixture("Project"), directory);
        output = Path.Combine(directory, "obj", "Nazor");
        theme = Path.Combine(directory, "Styles", "Theme.nss");
        Directory.CreateDirectory(Path.GetDirectoryName(theme)!);
        File.WriteAllText(theme, "Container { padding: 8px; }\n");
        badge = Path.Combine(directory, "Components", "Badge.nazor");
        app = Path.Combine(directory, "App.nazor");
    }

    public void Dispose() => Directory.Delete(directory, recursive: true);

    private static readonly string[] References = [Build.Core, Build.Controls, Build.Style];

    [Fact]
    public async Task Ide_refresh_returns_parseable_diagnostics_and_includes_previously_generated_files()
    {
        var session = Session();
        session.Refresh();
        var obsolete = Generated("Obsolete.nazor");
        File.WriteAllText(obsolete, "// A removed component left in obj by an earlier build.");
        File.WriteAllText(theme, "Container { padding: definitely-invalid; }");
        var writer = new StringWriter();
        Assert.Equal(1, await IdeRefreshCommand.RunAsync(session, writer));
        var reply = JsonNode.Parse(writer.ToString())!;
        Assert.Contains(reply["generated"]!.AsArray(), p => p!.GetValue<string>() == Generated("App.nazor"));
        Assert.DoesNotContain(reply["generated"]!.AsArray(), p => p!.GetValue<string>() == obsolete);
        var error = Assert.Single(reply["diagnostics"]!.AsArray(), d => d!["IsError"]!.GetValue<bool>());
        Assert.Equal(theme, error!["Path"]!.GetValue<string>());
        Assert.True(error["Line"]!.GetValue<int>() > 0);
    }

    private WatchSession Session(string? responseFile = null) =>
        new(new NazorCompilerOptions { RootNamespace = "ProjectSample", References = References }, output, directory, responseFile);

    private string Generated(string relativePath) => Path.Combine(output, relativePath + ".g.cs");

    private void Edit(string path, string from, string to)
    {
        var text = File.ReadAllText(path);
        Assert.Contains(from, text);
        File.WriteAllText(path, text.Replace(from, to));
    }

    private static void Copy(string from, string to)
    {
        Directory.CreateDirectory(to);
        foreach (var file in Directory.GetFiles(from)) File.Copy(file, Path.Combine(to, Path.GetFileName(file)));
        foreach (var folder in Directory.GetDirectories(from)) Copy(folder, Path.Combine(to, Path.GetFileName(folder)));
    }

    [Fact]
    public void The_first_refresh_writes_what_generate_writes_and_reports_every_sheet()
    {
        var session = Session();
        var result = session.Refresh();

        Assert.Empty(result.Diagnostics);
        Assert.False(result.DiscoveryFailed);
        Assert.Equal((3, 1), (result.Components, result.StyleSheets));
        Assert.Equal(
            [Generated("App.nazor"), Generated(Path.Combine("Components", "Badge.nazor")), Generated(Path.Combine("Components", "Editor.nazor")), Path.Combine(output, "NazorStyleSheets.g.cs")],
            result.GeneratedChanged.Order(StringComparer.OrdinalIgnoreCase));
        Assert.Collection(result.StylesChanged.OrderBy(s => s.Source),
            update =>
            {
                Assert.Equal(("Components/Badge.nazor", "ProjectSample.Components.Badge"), (update.Source, update.Scope));
                Assert.Contains("padding-top: 4px", StyleSheetJson.Deserialize(update.SheetJson).Describe());
            },
            update =>
            {
                Assert.Equal(("Styles/Theme.nss", null), (update.Source, update.Scope));
                Assert.Equal("Styles/Theme.nss", StyleSheetJson.Deserialize(update.SheetJson).Source);
            });

        var generate = new NazorCompiler(new NazorCompilerOptions
        {
            RootNamespace = "ProjectSample",
            ProjectDirectory = directory,
            References = References,
            CSharpSources = [Path.Combine(directory, "Components", "Badge.nazor.cs")],
        });
        var components = generate.Generate([app, badge, Path.Combine(directory, "Components", "Editor.nazor")]);
        Assert.Equal(3, components.Count);
        foreach (var component in components)
            Assert.Equal(Encoding.UTF8.GetBytes(component.CSharp), File.ReadAllBytes(Generated(component.RelativePath)));
        Assert.Equal(Encoding.UTF8.GetBytes(generate.GenerateStyleSheets([theme])), File.ReadAllBytes(Path.Combine(output, "NazorStyleSheets.g.cs")));

        var again = session.Refresh();
        Assert.Empty(again.GeneratedChanged);
        Assert.Empty(again.StylesChanged);
        Assert.Empty(again.Diagnostics);
    }

    [Fact]
    public void Editing_a_style_value_reports_the_component_sheet_and_rewrites_its_file()
    {
        var session = Session();
        var first = session.Refresh();
        var before = File.ReadAllText(Generated(Path.Combine("Components", "Badge.nazor")));

        Edit(badge, "padding: 4px", "padding: 6px");
        var result = session.Refresh();

        var update = Assert.Single(result.StylesChanged);
        Assert.Equal(("Components/Badge.nazor", "ProjectSample.Components.Badge"), (update.Source, update.Scope));
        Assert.Contains("padding-top: 6px", StyleSheetJson.Deserialize(update.SheetJson).Describe());
        Assert.NotEqual(first.StylesChanged.Single(s => s.Scope is not null).SheetJson, update.SheetJson);
        Assert.Equal([Generated(Path.Combine("Components", "Badge.nazor"))], result.GeneratedChanged);
        Assert.NotEqual(before, File.ReadAllText(Generated(Path.Combine("Components", "Badge.nazor"))));
        Assert.Empty(result.Diagnostics);
    }

    [Fact]
    public void A_markup_edit_rewrites_the_file_without_a_style_update()
    {
        var session = Session();
        session.Refresh();

        Edit(app, "Più uno", "Più due");
        var result = session.Refresh();

        Assert.Equal([Generated("App.nazor")], result.GeneratedChanged);
        Assert.Empty(result.StylesChanged);
        Assert.Contains("Più due", File.ReadAllText(Generated("App.nazor")));
    }

    [Fact]
    public void Editing_a_standalone_stylesheet_reports_it_without_a_scope()
    {
        var session = Session();
        session.Refresh();

        Edit(theme, "padding: 8px", "padding: 12px");
        var result = session.Refresh();

        var update = Assert.Single(result.StylesChanged);
        Assert.Equal(("Styles/Theme.nss", null), (update.Source, update.Scope));
        Assert.Contains("padding-top: 12px", StyleSheetJson.Deserialize(update.SheetJson).Describe());
        Assert.Equal([Path.Combine(output, "NazorStyleSheets.g.cs")], result.GeneratedChanged);
    }

    [Fact]
    public void A_stylesheet_with_errors_is_reported_while_the_other_sheets_still_flow()
    {
        var session = Session();
        session.Refresh();
        var styleSheets = File.ReadAllText(Path.Combine(output, "NazorStyleSheets.g.cs"));

        File.WriteAllText(theme, "Container { colr: red; }\n");
        Edit(badge, "padding: 4px", "padding: 5px");
        var result = session.Refresh();

        var error = Assert.Single(result.Diagnostics);
        Assert.Equal(("NZS101", theme, true), (error.Id, error.Path, error.IsError));
        Assert.Equal("ProjectSample.Components.Badge", Assert.Single(result.StylesChanged).Scope);
        Assert.Equal([Generated(Path.Combine("Components", "Badge.nazor"))], result.GeneratedChanged);
        Assert.False(result.DiscoveryFailed);
        Assert.Equal(styleSheets, File.ReadAllText(Path.Combine(output, "NazorStyleSheets.g.cs")));

        File.WriteAllText(theme, "Container { color: red; }\n");
        var fixedResult = session.Refresh();
        Assert.Empty(fixedResult.Diagnostics);
        Assert.Equal("Styles/Theme.nss", Assert.Single(fixedResult.StylesChanged).Source);
        Assert.Equal([Path.Combine(output, "NazorStyleSheets.g.cs")], fixedResult.GeneratedChanged);
    }

    [Fact]
    public void A_component_with_errors_keeps_its_last_generated_file()
    {
        var session = Session();
        session.Refresh();
        var before = File.ReadAllText(Generated("App.nazor"));

        Edit(app, "<Button @onclick=\"Increment\">Più uno</Button>", "<Buton />");
        var result = session.Refresh();

        Assert.Contains(result.Diagnostics, d => d.Id == "RZ10012" && d.Path == app && d.IsError);
        Assert.False(result.DiscoveryFailed);
        Assert.Empty(result.GeneratedChanged);
        Assert.Empty(result.StylesChanged);
        Assert.Equal(before, File.ReadAllText(Generated("App.nazor")));
    }

    [Fact]
    public void A_failed_discovery_writes_nothing()
    {
        var session = Session();
        session.Refresh();
        var codeBehind = Path.Combine(directory, "Components", "Badge.nazor.cs");

        Edit(codeBehind, "[Parameter] public int Count { get; set; }", "[Parameter] public int Count { get; set; }\n    [Parameter] public Hidden? Extra { get; set; }\n    private class Hidden { }");
        Edit(app, "Più uno", "Più tre");
        var result = session.Refresh();

        Assert.True(result.DiscoveryFailed);
        Assert.NotEmpty(result.Diagnostics);
        Assert.Empty(result.GeneratedChanged);
        Assert.Empty(result.StylesChanged);
        Assert.DoesNotContain("Più tre", File.ReadAllText(Generated("App.nazor")));

        Edit(codeBehind, "\n    [Parameter] public Hidden? Extra { get; set; }\n    private class Hidden { }", "");
        var fixedResult = session.Refresh();
        Assert.False(fixedResult.DiscoveryFailed);
        Assert.Equal([Generated("App.nazor")], fixedResult.GeneratedChanged);
    }

    [Fact]
    public void The_response_file_of_the_build_drives_the_root_namespace_and_the_references()
    {
        Directory.CreateDirectory(output);
        var responseFile = Path.Combine(output, "nazorc.rsp");
        var imports = Path.Combine(directory, "_Imports.nazor");
        File.WriteAllLines(responseFile, ["generate", "--out", output, "--root-namespace", "Renamed", "--project-dir", directory, .. References.SelectMany(r => new[] { "--reference", r }), app]);
        Edit(imports, "ProjectSample.Components", "Renamed.Components");
        var partialClass = badge + ".cs";
        Edit(partialClass, "ProjectSample.Components", "Renamed.Components");
        var session = Session(responseFile);

        var result = session.Refresh();
        Assert.Empty(result.Diagnostics);
        Assert.Equal("Renamed.Components.Badge", result.StylesChanged.Single(s => s.Scope is not null).Scope);
        Assert.Contains("namespace Renamed", File.ReadAllText(Generated("App.nazor")));

        File.WriteAllLines(responseFile, ["generate", "--out", output, "--root-namespace", "ProjectSample", "--project-dir", directory, .. References.SelectMany(r => new[] { "--reference", r }), app]);
        File.SetLastWriteTimeUtc(responseFile, DateTime.UtcNow.AddSeconds(2));
        Edit(partialClass, "Renamed.Components", "ProjectSample.Components");
        Edit(imports, "Renamed.Components", "ProjectSample.Components");
        var renamed = session.Refresh();
        Assert.Empty(renamed.Diagnostics);
        Assert.Empty(renamed.StylesChanged);
        Assert.Equal(3, renamed.GeneratedChanged.Count(p => p.EndsWith(".nazor.g.cs", StringComparison.Ordinal)));
        Assert.Contains("namespace ProjectSample", File.ReadAllText(Generated("App.nazor")));
    }

    [Fact]
    public void A_generic_component_reports_its_sheet_under_the_runtime_name_with_the_arity()
    {
        var tagged = Path.Combine(directory, "Components", "Tagged.nazor");
        File.WriteAllText(tagged, "@typeparam TItem\n<TextBlock Class=\"tagged\">@Item?.ToString()</TextBlock>\n\n@code {\n    [Parameter] public TItem? Item { get; set; }\n}\n\n@style {\n    .tagged { padding: 3px; }\n}\n");
        var session = Session();

        var result = session.Refresh();

        Assert.Empty(result.Diagnostics);
        var update = Assert.Single(result.StylesChanged, s => s.Source == "Components/Tagged.nazor");
        Assert.Equal("ProjectSample.Components.Tagged`1", update.Scope);
        Assert.Contains("padding-top: 3px", StyleSheetJson.Deserialize(update.SheetJson).Describe());
        var generated = File.ReadAllText(Generated(Path.Combine("Components", "Tagged.nazor")));
        Assert.Contains("class " + StyleHotReload.GenericRegistrationClassPrefix + "Tagged", generated);
        Assert.Contains("RegisterScoped(typeof(Tagged<>)", generated);

        Edit(tagged, "padding: 3px", "padding: 9px");
        var edited = session.Refresh();
        Assert.Equal("ProjectSample.Components.Tagged`1", Assert.Single(edited.StylesChanged).Scope);
    }
}
