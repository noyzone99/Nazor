using System.Collections.Immutable;
using System.Composition;
using System.Reflection;
using System.Text.Json.Nodes;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using Nazor.LanguageServer.Features;
using Nazor.LanguageServer.Workspace;

namespace Nazor.LanguageServer.Tests;

public sealed class ProjectToolsTests : IAsyncLifetime
{
    private readonly string directory = Directory.CreateTempSubdirectory("nazor-analyzers-").FullName;
    private LspClient client = null!;
    private string Component => Path.Combine(directory, "Counter.nazor");
    private const string Text = "<TextBlock />\n@code {\n    private int value = 12345;\n}\n";

    public async ValueTask InitializeAsync()
    {
        File.WriteAllText(Path.Combine(directory, "Demo.csproj"), "<Project Sdk=\"Microsoft.NET.Sdk\" />");
        var obj = Directory.CreateDirectory(Path.Combine(directory, "obj", "Nazor")).FullName;
        File.WriteAllLines(Path.Combine(obj, "nazorc.rsp"), ["generate", "--project-dir", directory, "--root-namespace", "Demo"]);
        File.WriteAllLines(Path.Combine(obj, "nazorc.roslyn.rsp"), [$"/analyzer:\"{typeof(FixtureAnalyzer).Assembly.Location}\"", "/nullable:enable", "/langversion:preview"]);
        client = LspClient.InProcess();
        await client.InitializeAsVisualStudioAsync();
    }

    public async ValueTask DisposeAsync()
    {
        await client.DisposeAsync();
        await LspClient.DeleteDirectoryAsync(directory);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Unused_fields_honor_build_warning_severity_and_suppression(bool suppressed)
    {
        File.WriteAllLines(RoslynConfiguration.FileFor(ProjectContext.ForDocument(Component))!,
            ["/warnaserror+", "/nullable:enable", suppressed ? "/nowarn:CS0169,CS0414" : "/warn:4"]);
        const string text = "<TextBlock />\n@code {\n    private int prop;\n    private int assigned = 1;\n}\n";
        await client.OpenAsync(Component, text);
        var diagnostics = await client.DiagnosticsAsync(Component, ds => suppressed || ds.Any(d => d!["code"]!.GetValue<string>() == "CS0169"));
        if (suppressed)
        {
            Assert.DoesNotContain(diagnostics, d => d!["code"]!.GetValue<string>() is "CS0169" or "CS0414");
            return;
        }
        var unused = Assert.Single(diagnostics, d => d!["code"]!.GetValue<string>() == "CS0169")!;
        Assert.Equal(1, unused["severity"]!.GetValue<int>());
        Assert.Equal(2, unused["range"]!["start"]!["line"]!.GetValue<int>());
        Assert.Equal(16, unused["range"]!["start"]!["character"]!.GetValue<int>());
        Assert.Equal(20, unused["range"]!["end"]!["character"]!.GetValue<int>());
        Assert.Contains(diagnostics, d => d!["code"]!.GetValue<string>() == "CS0414" && d["severity"]!.GetValue<int>() == 1);
        client.ForgetDiagnostics(Component);
        await client.ChangeAsync(Component, "<TextBlock />\n@code { }\n", 2);
        Assert.DoesNotContain(await client.DiagnosticsAsync(Component), d => d!["code"]!.GetValue<string>() is "CS0169" or "CS0414");
    }

    [Fact]
    public async Task Inline_analyzer_options_match_the_logical_csharp_sidecar_including_nested_configuration()
    {
        var nested = Directory.CreateDirectory(Path.Combine(directory, "Views")).FullName;
        File.WriteAllText(Path.Combine(directory, ".editorconfig"), "root = true\n[*.g.cs]\ndotnet_diagnostic.TEST123.severity = none\ngenerated_code = true\n");
        File.WriteAllText(Path.Combine(nested, ".editorconfig"), "[Counter.nazor.cs]\ndotnet_diagnostic.TEST123.severity = error\n");
        var path = Path.Combine(nested, "Counter.nazor");
        await client.OpenAsync(path, Text);
        var diagnostics = await client.DiagnosticsAsync(path, ds => ds.Any(d => d!["code"]!.GetValue<string>() == "TEST123"));
        Assert.Equal(1, Assert.Single(diagnostics, d => d!["code"]!.GetValue<string>() == "TEST123")!["severity"]!.GetValue<int>());
        Assert.DoesNotContain(Directory.EnumerateFiles(nested), p => p.EndsWith(".g.cs", StringComparison.OrdinalIgnoreCase));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Hidden_unnecessary_diagnostics_keep_roslyn_message_tags_and_help_link(bool visualStudio)
    {
        if (!visualStudio)
        {
            await client.DisposeAsync();
            client = LspClient.InProcess();
            await client.InitializeAsync();
        }
        await client.OpenAsync(Component, Text.Replace("12345", "67890"));
        var diagnostics = await client.DiagnosticsAsync(Component, ds => ds.Any(d => d!["code"]!.GetValue<string>() == "TEST124"));
        var diagnostic = Assert.Single(diagnostics, d => d!["code"]!.GetValue<string>() == "TEST124")!;
        Assert.Equal(4, diagnostic["severity"]!.GetValue<int>());
        Assert.Equal("Unnecessary fixture number", diagnostic["message"]!.GetValue<string>());
        Assert.Equal(visualStudio ? new[] { 1, int.MaxValue - 6, int.MaxValue - 4, int.MaxValue - 7, int.MaxValue - 2 } : new[] { 1 }, diagnostic["tags"]!.AsArray().Select(t => t!.GetValue<int>()));
        Assert.Equal("https://example.test/TEST124", diagnostic["codeDescription"]!["href"]!.GetValue<string>());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Silent_diagnostics_stay_invisible_and_remain_available_for_quick_fixes(bool visualStudio)
    {
        if (!visualStudio)
        {
            await client.DisposeAsync();
            client = LspClient.InProcess();
            await client.InitializeAsync();
        }
        var text = Text.Replace("12345", "13579");
        await client.OpenAsync(Component, text);
        var diagnostics = await client.DiagnosticsAsync(Component);
        if (visualStudio)
        {
            var diagnostic = Assert.Single(diagnostics, d => d!["code"]!.GetValue<string>() == "TEST125")!;
            Assert.Equal(4, diagnostic["severity"]!.GetValue<int>());
            Assert.Equal(new[] { int.MaxValue - 6, int.MaxValue - 4, int.MaxValue - 7, int.MaxValue - 2 }, diagnostic["tags"]!.AsArray().Select(t => t!.GetValue<int>()));
        }
        else Assert.DoesNotContain(diagnostics, d => d!["code"]!.GetValue<string>() == "TEST125");
        var actions = (await client.RequestAsync("textDocument/codeAction", new JsonObject
        {
            ["textDocument"] = new JsonObject { ["uri"] = LspClient.Uri(Component) },
            ["range"] = new JsonObject { ["start"] = new JsonObject { ["line"] = 2, ["character"] = 24 }, ["end"] = new JsonObject { ["line"] = 2, ["character"] = 29 } },
            ["context"] = new JsonObject { ["diagnostics"] = new JsonArray() },
        }))!.AsArray();
        Assert.Contains(actions, a => a!["title"]!.GetValue<string>() == "Replace the fixture number");
    }

    [Fact]
    public async Task Native_CSharp_preferences_refresh_without_a_source_edit_and_project_settings_take_precedence()
    {
        UseCodeStyleAnalyzer();
        const string text = "<TextBlock />\n@code {\n    private int count;\n    private void Increment() => count++;\n}\n";
        await PreferencesAsync("false:silent");
        await client.OpenAsync(Component, text);
        var initial = await client.DiagnosticsAsync(Component, ds => ds.Any(d => d!["code"]!.GetValue<string>() == "IDE0022"));
        var silent = Assert.Single(initial, d => d!["code"]!.GetValue<string>() == "IDE0022")!;
        Assert.Contains(int.MaxValue - 6, silent["tags"]!.AsArray().Select(t => t!.GetValue<int>()));
        client.ForgetDiagnostics(Component);
        await PreferencesAsync("when_on_single_line:silent");
        Assert.DoesNotContain(await client.DiagnosticsAsync(Component), d => d!["code"]!.GetValue<string>() == "IDE0022");
        var config = Path.Combine(directory, ".editorconfig");
        File.WriteAllText(config, "root = true\n[*.cs]\ncsharp_style_expression_bodied_methods = false:warning\n");
        client.ForgetDiagnostics(Component);
        await client.NotifyAsync("workspace/didChangeWatchedFiles", new JsonObject
        {
            ["changes"] = new JsonArray(new JsonObject { ["uri"] = LspClient.Uri(config), ["type"] = 1 }),
        });
        var configured = Assert.Single(await client.DiagnosticsAsync(Component, ds => ds.Any(d => d!["code"]!.GetValue<string>() == "IDE0022")), d => d!["code"]!.GetValue<string>() == "IDE0022")!;
        Assert.Equal(2, configured["severity"]!.GetValue<int>());
        Assert.DoesNotContain(int.MaxValue - 6, configured["tags"]!.AsArray().Select(t => t!.GetValue<int>()));
        Assert.False(File.Exists(Path.Combine(directory, ".nazor.ide.globalconfig")));
    }

    [Fact]
    public async Task Generated_wrapper_indentation_never_produces_formatting_diagnostics_or_fixes()
    {
        UseCodeStyleAnalyzer();
        File.WriteAllText(Path.Combine(directory, ".editorconfig"), "root = true\n[*.cs]\ndotnet_diagnostic.IDE0055.severity = warning\n");
        var helper = Path.Combine(directory, "Helper.cs");
        File.WriteAllText(helper, "class Helper{public void Run(){}}\n");
        File.AppendAllLines(ProjectContext.ForDocument(Component).ResponseFile!, ["--source", helper]);
        var store = new DocumentStore();
        store.Set(LspClient.Uri(Component), "<TextBlock />\n@code {\n    private int count;\n    private void Increment()\n    {\n        count++;\n    }\n}\n", 1);
        var snapshot = new ProjectAnalyzer(ProjectContext.ForDocument(Component), store).Analyze();
        var diagnostics = await ProjectTools.DiagnosticsAsync(snapshot, TestContext.Current.CancellationToken);
        Assert.Contains(diagnostics, d => d.Id == "IDE0055" && d.Location.SourceTree!.FilePath == helper);
        Assert.DoesNotContain(diagnostics, d => d.Id == "IDE0055" && d.Location.SourceTree!.FilePath == Component + ".g.cs");
        var component = snapshot.Component(Component)!;
        var fixes = await ProjectTools.FixesAsync(snapshot, component, 0, component.Text.Length, e => throw e, TestContext.Current.CancellationToken);
        Assert.DoesNotContain(fixes, f => f.Title.Contains("format", StringComparison.OrdinalIgnoreCase));
    }

    private void UseCodeStyleAnalyzer()
    {
        var sdk = typeof(ProjectToolsTests).Assembly.GetCustomAttributes<AssemblyMetadataAttribute>().Single(a => a.Key == "Nazor.TestSdkDirectory").Value!;
        var analyzer = Path.Combine(sdk, "Sdks", "Microsoft.NET.Sdk", "codestyle", "cs", "Microsoft.CodeAnalysis.CSharp.CodeStyle.dll");
        Assert.True(File.Exists(analyzer), analyzer);
        File.WriteAllLines(RoslynConfiguration.FileFor(ProjectContext.ForDocument(Component))!, [$"/analyzer:\"{analyzer}\""]);
    }

    private Task PreferencesAsync(string expressionBodies) => client.NotifyAsync("nazor/csharpOptions", new JsonObject
    {
        ["options"] = new JsonObject { ["csharp_style_expression_bodied_methods"] = expressionBodies },
    });

    [Theory]
    [InlineData("when_on_single_line", true, false)]
    [InlineData("when_on_single_line", false, true)]
    [InlineData("false", true, true)]
    [InlineData("false", false, false)]
    public async Task Real_code_style_analyzers_use_the_same_preference_for_CSharp_and_component_methods(string preference, bool expression, bool expected)
    {
        var sdk = typeof(ProjectToolsTests).Assembly.GetCustomAttributes<AssemblyMetadataAttribute>().Single(a => a.Key == "Nazor.TestSdkDirectory").Value!;
        var analyzer = Path.Combine(sdk, "Sdks", "Microsoft.NET.Sdk", "codestyle", "cs", "Microsoft.CodeAnalysis.CSharp.CodeStyle.dll");
        Assert.True(File.Exists(analyzer), analyzer);
        File.WriteAllLines(RoslynConfiguration.FileFor(ProjectContext.ForDocument(Component))!, [$"/analyzer:\"{analyzer}\""]);
        File.WriteAllText(Path.Combine(directory, ".editorconfig"), $"root = true\n[*.cs]\ncsharp_style_expression_bodied_methods = {preference}:suggestion\n");
        var body = expression ? "=> count++;" : "{ count++; }";
        var helper = Path.Combine(directory, "Helper.cs");
        File.WriteAllText(helper, $"class Helper {{ private int count; public void Increment() {body} }}\n");
        File.AppendAllLines(ProjectContext.ForDocument(Component).ResponseFile!, ["--source", helper]);
        var store = new DocumentStore();
        store.Set(LspClient.Uri(Component), $"<Button @onclick=\"Increment\" />\n@code {{ private int count; private void Increment() {body} }}\n", 1);
        var snapshot = new ProjectAnalyzer(ProjectContext.ForDocument(Component), store).Analyze();
        var diagnostics = await ProjectTools.DiagnosticsAsync(snapshot, TestContext.Current.CancellationToken);
        Assert.DoesNotContain(diagnostics, d => d.Id is "AD0001" or "NZL001");
        var methodRules = diagnostics.Where(d => d.Id == "IDE0022").ToArray();
        Assert.Equal(expected ? 2 : 0, methodRules.Length);
        if (expected)
        {
            Assert.Contains(methodRules, d => d.Location.SourceTree!.FilePath == helper);
            Assert.Contains(methodRules, d => d.Location.SourceTree!.FilePath == Component + ".g.cs");
            Assert.All(methodRules, d => Assert.Contains(preference == "false" ? "block body" : "expression body", d.GetMessage(), StringComparison.OrdinalIgnoreCase));
        }
    }

    [Fact]
    public async Task Real_object_initializer_diagnostics_offer_a_fix_that_can_be_applied_to_the_source()
    {
        File.AppendAllLines(ProjectContext.ForDocument(Component).ResponseFile!, ["--reference", typeof(ComponentBase).Assembly.Location,
            "--reference", typeof(Nazor.Controls.TextBlock).Assembly.Location, "--reference", typeof(Nazor.Style.StyleSheet).Assembly.Location]);
        var sdk = typeof(ProjectToolsTests).Assembly.GetCustomAttributes<AssemblyMetadataAttribute>().Single(a => a.Key == "Nazor.TestSdkDirectory").Value!;
        var analyzer = Path.Combine(sdk, "Sdks", "Microsoft.NET.Sdk", "codestyle", "cs", "Microsoft.CodeAnalysis.CSharp.CodeStyle.dll");
        File.WriteAllLines(RoslynConfiguration.FileFor(ProjectContext.ForDocument(Component))!, [$"/analyzer:\"{analyzer}\""]);
        File.WriteAllText(Path.Combine(directory, ".editorconfig"), "root = true\n[*.cs]\ncsharp_style_object_initializer = true:suggestion\n");
        const string text = "<TextBlock />\n@code {\n    private void Run()\n    {\n        System.Timers.Timer timer = new(1000);\n        timer.AutoReset = true;\n        timer.Enabled = true;\n        timer.Start();\n    }\n}\n";
        await client.OpenAsync(Component, text);
        var diagnostics = await client.DiagnosticsAsync(Component, ds => ds.Any(d => d!["code"]!.GetValue<string>() == "IDE0017"));
        JsonNode? fix = null;
        foreach (var diagnostic in diagnostics.Where(d => d!["code"]!.GetValue<string>() == "IDE0017"))
        {
            var actions = (await client.RequestAsync("textDocument/codeAction", new JsonObject
            {
                ["textDocument"] = new JsonObject { ["uri"] = LspClient.Uri(Component) },
                ["range"] = diagnostic!["range"]!.DeepClone(),
                ["context"] = new JsonObject { ["only"] = new JsonArray("quickfix"), ["diagnostics"] = new JsonArray(diagnostic.DeepClone()) },
            }))!.AsArray();
            fix = Assert.Single(actions, a => a!["title"]!.GetValue<string>().Contains("initialization", StringComparison.OrdinalIgnoreCase))!;
        }
        var updated = fix!["edit"]!["documentChanges"]![0]!["edits"]![0]!["newText"]!.GetValue<string>();
        Assert.Contains("AutoReset = true", updated);
        Assert.DoesNotContain("timer.AutoReset", updated);
        Assert.StartsWith("<TextBlock />\n@code {", updated);
        Assert.DoesNotContain("#line", updated);
        client.ForgetDiagnostics(Component);
        await client.ChangeAsync(Component, updated, 2);
        var remaining = await client.DiagnosticsAsync(Component);
        Assert.True(!remaining.Any(d => d!["code"]!.GetValue<string>() is "IDE0017" || d["severity"]!.GetValue<int>() == 1), updated + "\n" + remaining.ToJsonString());
    }

    [Fact]
    public async Task The_project_loads_the_analyzer_assembly()
    {
        var store = new DocumentStore();
        store.Set(LspClient.Uri(Component), Text, 1);
        var snapshot = new ProjectAnalyzer(ProjectContext.ForDocument(Component), store).Analyze();
        var reference = Assert.Single(snapshot.CSharp.AnalyzerReferences);
        Assert.Single(reference.GetAnalyzers(LanguageNames.CSharp));
        var diagnostics = await ProjectTools.DiagnosticsAsync(snapshot, TestContext.Current.CancellationToken);
        Assert.Contains(diagnostics, d => d.Id == "TEST123");
    }

    [Fact]
    public async Task Project_analyzers_run_in_code_blocks_and_their_fixes_map_back_to_the_component()
    {
        await client.OpenAsync(Component, Text);
        var diagnostics = await client.DiagnosticsAsync(Component, ds => ds.Any(d => d!["code"]!.GetValue<string>() == "TEST123"));
        var diagnostic = Assert.Single(diagnostics, d => d!["code"]!.GetValue<string>() == "TEST123")!;
        Assert.Equal(2, diagnostic["severity"]!.GetValue<int>());
        Assert.Equal(2, diagnostic["range"]!["start"]!["line"]!.GetValue<int>());
        var actions = (await client.RequestAsync("textDocument/codeAction", new JsonObject
        {
            ["textDocument"] = new JsonObject { ["uri"] = LspClient.Uri(Component) },
            ["range"] = diagnostic["range"]!.DeepClone(),
            ["context"] = new JsonObject { ["only"] = new JsonArray("quickfix"), ["diagnostics"] = new JsonArray(diagnostic.DeepClone()) },
        }))!.AsArray();
        var fix = Assert.Single(actions)!;
        Assert.Equal("Replace the fixture number", fix["title"]!.GetValue<string>());
        var edit = Assert.Single(fix["edit"]!["documentChanges"]!.AsArray())!;
        Assert.Equal(LspClient.Uri(Component), edit["textDocument"]!["uri"]!.GetValue<string>());
        Assert.Equal(1, edit["textDocument"]!["version"]!.GetValue<int>());
        var updated = edit["edits"]![0]!["newText"]!.GetValue<string>();
        Assert.Equal(Text.Replace("12345", "42"), updated);
        client.ForgetDiagnostics(Component);
        await client.ChangeAsync(Component, updated, 2);
        Assert.DoesNotContain(await client.DiagnosticsAsync(Component), d => d!["code"]!.GetValue<string>() == "TEST123");
    }

    [Theory]
    [InlineData("none", 0)]
    [InlineData("suggestion", 3)]
    [InlineData("error", 1)]
    public async Task Editorconfig_severity_is_respected(string severity, int expected)
    {
        File.WriteAllText(Path.Combine(directory, ".editorconfig"), $"root = true\n[*.cs]\ndotnet_diagnostic.TEST123.severity = {severity}\n");
        await client.OpenAsync(Component, Text);
        var diagnostics = await client.DiagnosticsAsync(Component);
        var rule = diagnostics.SingleOrDefault(d => d!["code"]!.GetValue<string>() == "TEST123");
        if (expected == 0) Assert.Null(rule);
        else Assert.Equal(expected, rule!["severity"]!.GetValue<int>());
    }

    [Fact]
    public async Task A_fix_touching_unmapped_generated_code_is_rejected_as_a_whole()
    {
        var store = new DocumentStore();
        store.Set(LspClient.Uri(Component), Text, 1);
        var snapshot = new ProjectAnalyzer(ProjectContext.ForDocument(Component), store).Analyze();
        var document = snapshot.Document(snapshot.Component(Component)!)!;
        var text = await document.GetTextAsync(TestContext.Current.CancellationToken);
        var changes = new[] { new TextChange(new TextSpan(0, 0), "// generated edit\n"), new TextChange(new TextSpan(text.ToString().IndexOf("12345", StringComparison.Ordinal), 5), "42") };
        Assert.Null(await ProjectTools.MapAsync(snapshot, document.WithText(text.WithChanges(changes)).Project.Solution, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Code_fixes_can_add_imports_and_change_multiple_source_documents_together()
    {
        var helper = Path.Combine(directory, "Helper.cs");
        File.WriteAllText(helper, "class Helper { public const int Value = 12345; }\n");
        File.AppendAllLines(ProjectContext.ForDocument(Component).ResponseFile!, ["--source", helper]);
        var store = new DocumentStore();
        store.Set(LspClient.Uri(Component), Text, 1);
        var snapshot = new ProjectAnalyzer(ProjectContext.ForDocument(Component), store).Analyze();
        var document = snapshot.Document(snapshot.Component(Component)!)!;
        var text = await document.GetTextAsync(TestContext.Current.CancellationToken);
        var edits = new[] { new TextChange(new TextSpan(0, 0), "using System.Text;\n"), new TextChange(new TextSpan(text.ToString().IndexOf("12345", StringComparison.Ordinal), 5), "42") };
        var solution = document.WithText(text.WithChanges(edits)).Project.Solution;
        var helperDocument = solution.GetDocumentIdsWithFilePath(helper).Single();
        solution = solution.WithDocumentText(helperDocument, SourceText.From("class Helper { public const int Value = 42; }\n"));
        var changes = (await ProjectTools.MapAsync(snapshot, solution, TestContext.Current.CancellationToken))!;
        Assert.Equal(2, changes.Count);
        Assert.Equal("@using System.Text\n" + Text.Replace("12345", "42"), changes.Single(c => c.Path == Component).NewText);
        Assert.Contains("Value = 42", changes.Single(c => c.Path == helper).NewText);
    }

    [Fact]
    public async Task A_saved_editorconfig_change_refreshes_diagnostics_without_editing_the_component()
    {
        var config = Path.Combine(directory, ".editorconfig");
        File.WriteAllText(config, "root = true\n[*.cs]\ndotnet_diagnostic.TEST123.severity = none\n");
        await client.OpenAsync(Component, Text);
        Assert.DoesNotContain(await client.DiagnosticsAsync(Component), d => d!["code"]!.GetValue<string>() == "TEST123");
        client.ForgetDiagnostics(Component);
        File.WriteAllText(config, "root = true\n[*.cs]\ndotnet_diagnostic.TEST123.severity = error\n");
        await client.NotifyAsync("workspace/didChangeWatchedFiles", new JsonObject
        {
            ["changes"] = new JsonArray(new JsonObject { ["uri"] = LspClient.Uri(config), ["type"] = 2 }),
        });
        Assert.Contains(await client.DiagnosticsAsync(Component), d => d!["code"]!.GetValue<string>() == "TEST123" && d["severity"]!.GetValue<int>() == 1);
    }

    [Fact]
    public async Task Analyzer_load_failures_are_reported_instead_of_disappearing()
    {
        var invalid = Path.Combine(directory, "InvalidAnalyzer.dll");
        File.WriteAllText(invalid, "invalid assembly");
        var manifest = RoslynConfiguration.FileFor(ProjectContext.ForDocument(Component))!;
        File.WriteAllLines(manifest, [$"/analyzer:\"{invalid}\""]);
        await client.OpenAsync(Component, Text);
        var diagnostics = await client.DiagnosticsAsync(Component, ds => ds.Any(d => d!["code"]!.GetValue<string>() == "NZL001"));
        Assert.Contains(diagnostics, d => d!["message"]!.GetValue<string>().Contains("InvalidAnalyzer", StringComparison.Ordinal));
    }
}

#pragma warning disable RS1038, RS1041, RS1036, RS2008
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class FixtureAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new("TEST123", "Fixture", "Replace fixture number", "Testing", Microsoft.CodeAnalysis.DiagnosticSeverity.Warning, true);
    private static readonly DiagnosticDescriptor Unnecessary = new("TEST124", "Fixture", "Unnecessary fixture number", "Testing", Microsoft.CodeAnalysis.DiagnosticSeverity.Hidden, true,
        helpLinkUri: "https://example.test/TEST124", customTags: [WellKnownDiagnosticTags.Unnecessary]);
    private static readonly DiagnosticDescriptor Silent = new("TEST125", "Fixture", "Silent fixture number", "Testing", Microsoft.CodeAnalysis.DiagnosticSeverity.Hidden, true);
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule, Unnecessary, Silent];
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(c =>
        {
            if (c.Node is LiteralExpressionSyntax literal && literal.Token.ValueText == "12345")
                c.ReportDiagnostic(Diagnostic.Create(Rule, literal.GetLocation()));
            if (c.Node is LiteralExpressionSyntax unused && unused.Token.ValueText == "67890")
                c.ReportDiagnostic(Diagnostic.Create(Unnecessary, unused.GetLocation()));
            if (c.Node is LiteralExpressionSyntax silent && silent.Token.ValueText == "13579")
                c.ReportDiagnostic(Diagnostic.Create(Silent, silent.GetLocation()));
        }, SyntaxKind.NumericLiteralExpression);
    }
}

[ExportCodeFixProvider(LanguageNames.CSharp), Shared]
public sealed class FixtureCodeFix : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds => ["TEST123", "TEST125"];
    public override FixAllProvider? GetFixAllProvider() => null;
    public override Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        context.RegisterCodeFix(CodeAction.Create("Replace the fixture number", async cancellation =>
        {
            var text = await context.Document.GetTextAsync(cancellation);
            return context.Document.WithText(text.WithChanges(new TextChange(context.Span, "42")));
        }, "fixture"), context.Diagnostics);
        return Task.CompletedTask;
    }
}
