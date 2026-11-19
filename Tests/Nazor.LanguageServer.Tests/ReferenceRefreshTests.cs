using System.Text.Json.Nodes;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Nazor.LanguageServer.Tests;

public sealed class ReferenceRefreshTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Rebuilding_a_reference_refreshes_parameters_tags_hover_and_colors_without_editing_the_document(bool notifyBuild)
    {
        var directory = Directory.CreateTempSubdirectory("nazor-references-").FullName;
        try
        {
            var library = Path.Combine(directory, "Widgets.dll");
            var core = typeof(ComponentBase).Assembly.Location;
            File.WriteAllText(Path.Combine(directory, "Demo.csproj"), "<Project />");
            Emit(library, false);
            File.SetLastWriteTimeUtc(library, DateTime.UtcNow.AddMinutes(-2));
            var app = Path.Combine(directory, "App.nazor");
            const string text = "@using Widgets\n<TitleBar />\n<SnapLayoutArea />\n";
            File.WriteAllText(app, text);
            var build = Directory.CreateDirectory(Path.Combine(directory, "obj", "Nazor")).FullName;
            File.WriteAllText(Path.Combine(build, "nazorc.rsp"), string.Join("\n",
                "generate", "--root-namespace", "Demo", "--reference", core, "--reference", library, app));
            await using var client = LspClient.InProcess();
            var initialized = await client.InitializeAsync(new JsonObject
            {
                ["workspace"] = new JsonObject { ["semanticTokens"] = new JsonObject { ["refreshSupport"] = true } },
            });
            var types = initialized!["capabilities"]!["semanticTokensProvider"]!["legend"]!["tokenTypes"]!.AsArray();
            await client.OpenAsync(app, text);
            var before = await client.AtAsync("textDocument/completion", app, 1, 10);
            Assert.Contains(before!["items"]!.AsArray(), i => i!["label"]!.GetValue<string>() == "ShowMinimize");

            Emit(library, true);
            if (notifyBuild)
                await client.NotifyAsync("workspace/didChangeWatchedFiles", new JsonObject
                {
                    ["changes"] = new JsonArray(new JsonObject { ["uri"] = LspClient.Uri(library), ["type"] = 2 }),
                });
            var after = await client.AtAsync("textDocument/completion", app, 1, 10);
            var labels = after!["items"]!.AsArray().Select(i => i!["label"]!.GetValue<string>()).ToArray();
            Assert.Contains("ShowSystemButtons", labels);
            Assert.DoesNotContain(labels, label => label is "ShowMinimize" or "ShowMaximize" or "EnableSnapLayouts");
            var hover = await client.AtAsync("textDocument/hover", app, 2, 5);
            Assert.Contains("SnapLayoutArea", hover!.ToJsonString());
            var tokens = (await client.RequestAsync("textDocument/semanticTokens/full", new JsonObject
            {
                ["textDocument"] = new JsonObject { ["uri"] = LspClient.Uri(app) },
            }))!["data"]!.AsArray().Select(n => n!.GetValue<int>()).ToArray();
            var line = 0;
            var column = 0;
            string? snapType = null;
            for (var i = 0; i < tokens.Length; i += 5)
            {
                line += tokens[i];
                column = tokens[i] == 0 ? column + tokens[i + 1] : tokens[i + 1];
                if (line == 2 && column == 1) snapType = types[tokens[i + 3]]!.GetValue<string>();
            }
            Assert.Equal("razorComponentElement", snapType);
            if (notifyBuild)
                Assert.NotNull((await client.MessageAsync("workspace/semanticTokens/refresh"))["id"]);
        }
        finally
        {
            await LspClient.DeleteDirectoryAsync(directory);
        }
    }

    [Fact]
    public async Task Library_contract_errors_are_visible_in_open_components_and_clear_after_rebuild()
    {
        var directory = Directory.CreateTempSubdirectory("nazor-contract-errors-").FullName;
        try
        {
            var library = Path.Combine(directory, "Widgets.dll");
            File.WriteAllText(Path.Combine(directory, "Demo.csproj"), "<Project />");
            EmitSource(library, "namespace Widgets; public sealed class TitleBar : Nazor.ComponentBase { [Nazor.Parameter] public bool ShowMinimize { get; } }");
            File.SetLastWriteTimeUtc(library, DateTime.UtcNow.AddMinutes(-2));
            var app = Path.Combine(directory, "App.nazor");
            const string text = "@using Widgets\n<TitleBar />\n";
            File.WriteAllText(app, text);
            var build = Directory.CreateDirectory(Path.Combine(directory, "obj", "Nazor")).FullName;
            File.WriteAllText(Path.Combine(build, "nazorc.rsp"), string.Join("\n",
                "generate", "--root-namespace", "Demo", "--reference", typeof(ComponentBase).Assembly.Location, "--reference", library, app));
            await using var client = LspClient.InProcess();
            await client.InitializeAsVisualStudioAsync();
            await client.OpenAsync(app, text);
            var diagnostics = await client.DiagnosticsAsync(app, items => items.Any(d => d!["code"]?.GetValue<string>() == "NZC002"));
            Assert.Contains(diagnostics, d => d!["message"]!.GetValue<string>().Contains("ShowMinimize", StringComparison.Ordinal));

            Emit(library, false);
            client.ForgetDiagnostics(app);
            await client.NotifyAsync("workspace/didChangeWatchedFiles", new JsonObject
            {
                ["changes"] = new JsonArray(new JsonObject { ["uri"] = LspClient.Uri(library), ["type"] = 2 }),
            });
            await client.DiagnosticsAsync(app, items => items.Count == 0);
            Assert.Contains("TitleBar", (await client.AtAsync("textDocument/hover", app, 1, 4))!.ToJsonString());
        }
        finally
        {
            await LspClient.DeleteDirectoryAsync(directory);
        }
    }

    private static void Emit(string path, bool current)
    {
        var parameters = current ? "[Nazor.Parameter] public bool ShowSystemButtons { get; set; }" :
            "[Nazor.Parameter] public bool ShowMinimize { get; set; } [Nazor.Parameter] public bool ShowMaximize { get; set; } [Nazor.Parameter] public bool EnableSnapLayouts { get; set; }";
        var snap = current ? "public sealed class SnapLayoutArea : Nazor.ComponentBase { }" : "";
        var source = $"namespace Widgets; public sealed class TitleBar : Nazor.ComponentBase {{ {parameters} }} {snap}";
        EmitSource(path, source);
    }

    private static void EmitSource(string path, string source)
    {
        var references = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator)
            .Append(typeof(ComponentBase).Assembly.Location).Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(p => MetadataReference.CreateFromFile(p));
        var compilation = CSharpCompilation.Create("Widgets", [CSharpSyntaxTree.ParseText(source)], references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        using var image = new MemoryStream();
        var result = compilation.Emit(image);
        Assert.True(result.Success, string.Join("\n", result.Diagnostics));
        File.WriteAllBytes(path, image.ToArray());
    }
}
