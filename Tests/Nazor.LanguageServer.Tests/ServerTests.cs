using System.Diagnostics;
using System.Reflection;
using System.Text.Json.Nodes;

namespace Nazor.LanguageServer.Tests;

public sealed class ServerTests : IAsyncLifetime
{
    private readonly string directory = Directory.CreateTempSubdirectory("nazor-server-").FullName;
    private LspClient client = null!;

    public ValueTask InitializeAsync()
    {
        File.WriteAllText(Path.Combine(directory, "Demo.csproj"), "<Project Sdk=\"Microsoft.NET.Sdk\" />");
        client = LspClient.InProcess();
        return ValueTask.CompletedTask;
    }

    [Theory]
    [InlineData("Badge.nazor", false)]
    [InlineData("Badge.nazor.nss", true)]
    public async Task Launcher_badge_rules_offer_completion_hover_and_diagnostics_in_both_editors(string name, bool visualStudio)
    {
        if (visualStudio) await client.InitializeAsVisualStudioAsync();
        else await client.InitializeAsync();
        var path = Path.Combine(directory, name);
        var inline = name.EndsWith(".nazor", StringComparison.Ordinal);
        string Wrap(string text) => inline ? "@window\n<Container />\n@style { " + text + " }" : text;
        var text = Wrap(":root { -windows-badge- }");
        await client.OpenAsync(path, text);
        var position = text.IndexOf("-windows-badge-", StringComparison.Ordinal) + "-windows-badge-".Length;
        var lines = text[..position].Split('\n');
        var items = (await client.AtAsync("textDocument/completion", path, lines.Length - 1, lines[^1].Length))!["items"]!.AsArray();
        foreach (var suffix in new[] { "background-color", "background", "background-image", "background-clip", "color", "size", "border", "border-left-width", "image-background-color", "image-fit", "text-shadow", "transform", "mask-image" })
            Assert.Contains(items, item => item!["label"]!.GetValue<string>() == "-windows-badge-" + suffix);
        text = Wrap(":root { -windows-badge-image-fit: cover; -windows-badge-size: 14px; -windows-badge-border: 1px solid red; -windows-badge-background: linear-gradient(red, blue); -windows-badge-transform: rotate(5deg); -windows-badge-mask-image: url(mask.svg); }");
        await client.ChangeAsync(path, text, 2);
        Assert.Empty(await client.DiagnosticsAsync(path, diagnostics => diagnostics.Count == 0));
        position = text.IndexOf("image-fit", StringComparison.Ordinal);
        lines = text[..position].Split('\n');
        var hover = await client.AtAsync("textDocument/hover", path, lines.Length - 1, lines[^1].Length);
        Assert.Contains("root Window", hover!["contents"]!["value"]!.GetValue<string>());
        client.ForgetDiagnostics(path);
        await client.ChangeAsync(path, Wrap(":root { -windows-badge-image-fit: invalid; }"), 3);
        Assert.Contains(await client.DiagnosticsAsync(path, diagnostics => diagnostics.Count > 0), diagnostic => diagnostic!["message"]!.GetValue<string>().Contains("-windows-badge-image-fit", StringComparison.Ordinal));
    }

    public async ValueTask DisposeAsync()
    {
        await client.DisposeAsync();
        await LspClient.DeleteDirectoryAsync(directory);
    }

    private string PathOf(string name) => Path.Combine(directory, name);

    private static string Code(JsonNode diagnostic) => diagnostic["code"]!.GetValue<string>();

    [Fact]
    public async Task Initialize_announces_synchronization_completion_and_hover()
    {
        var result = await client.InitializeAsync();
        var capabilities = result!["capabilities"]!;
        Assert.Equal(1, capabilities["textDocumentSync"]!["change"]!.GetValue<int>());
        Assert.True(capabilities["hoverProvider"]!.GetValue<bool>());
        Assert.Contains("<", capabilities["completionProvider"]!["triggerCharacters"]!.AsArray().Select(c => c!.GetValue<string>()));
        Assert.Equal("Nazor", result["serverInfo"]!["name"]!.GetValue<string>());
        Assert.Equal(typeof(Nazor.Nss.NssCompiler).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()!.InformationalVersion.Split('+')[0],
            result["serverInfo"]!["version"]!.GetValue<string>());
    }

    [Theory]
    [InlineData("Pseudo.nss", false)]
    [InlineData("Pseudo.nazor", true)]
    public async Task Pseudo_elements_offer_completion_hover_and_diagnostics_through_both_editor_protocols(string name, bool visualStudio)
    {
        if (visualStudio) await client.InitializeAsVisualStudioAsync();
        else await client.InitializeAsync();
        var path = Path.Combine(directory, name);
        var inline = name.EndsWith(".nazor", StringComparison.Ordinal);
        string Wrap(string source) => inline ? "<Container />\n@style { " + source + " }" : source;
        var text = Wrap("Label::be { }");
        await client.OpenAsync(path, text);
        var position = text.IndexOf("::be", StringComparison.Ordinal) + 4;
        var lines = text[..position].Split('\n');
        var items = (await client.AtAsync("textDocument/completion", path, lines.Length - 1, lines[^1].Length))!["items"]!.AsArray();
        Assert.Contains(items, item => item!["label"]!.GetValue<string>() == "::before");
        text = Wrap("Label::before { content: \"\"; width: 8px; } TextFieldInput::placeholder { color: gray; } TextFieldInput::selection { color: white; background-color: blue; }");
        await client.ChangeAsync(path, text, 2);
        Assert.Empty(await client.DiagnosticsAsync(path, diagnostics => diagnostics.Count == 0));
        position = text.IndexOf("before", StringComparison.Ordinal) + 2;
        lines = text[..position].Split('\n');
        var hover = await client.AtAsync("textDocument/hover", path, lines.Length - 1, lines[^1].Length);
        Assert.Contains("decorative", hover!["contents"]!["value"]!.GetValue<string>());
        client.ForgetDiagnostics(path);
        await client.ChangeAsync(path, Wrap("Label::before { content: 12; }"), 3);
        Assert.Contains(await client.DiagnosticsAsync(path, diagnostics => diagnostics.Any(d => Code(d!) == "NZS102")), d => Code(d!) == "NZS102");
    }

    [Fact]
    public async Task Stylesheet_errors_are_published_with_their_range_and_cleared_when_fixed()
    {
        await client.InitializeAsync();
        var theme = PathOf("Theme.nss");
        await client.OpenAsync(theme, ".a {\n    colr: red;\n}\n");
        var error = Assert.Single(await client.DiagnosticsAsync(theme))!;
        Assert.Equal("NZS101", Code(error));
        Assert.Equal(1, error["severity"]!.GetValue<int>());
        Assert.Equal("nazor", error["source"]!.GetValue<string>());
        Assert.Contains("Unknown property 'colr'", error["message"]!.GetValue<string>());
        Assert.Equal((1, 4, 1, 8), Range(error));

        await client.ChangeAsync(theme, ".a {\n    color: red;\n}\n", 2);
        await client.DiagnosticsAsync(theme, d => d.Count == 0);
    }

    [Fact]
    public async Task Component_errors_are_published_after_the_analysis_of_the_project()
    {
        await client.InitializeAsync();
        var app = PathOf("App.nazor");
        await client.OpenAsync(app, "<Container>\n    <Buton />\n</Container>\n");
        var unknown = Assert.Single(await client.DiagnosticsAsync(app, d => d.Count > 0))!;
        Assert.Equal("RZ10012", Code(unknown));
        Assert.Equal((1, 4, 1, 13), Range(unknown));

        await client.ChangeAsync(app, "<Container>\n    <Button />\n</Container>\n\n@style {\n    .x { opacity: 2px; }\n}\n", 2);
        var style = Assert.Single(await client.DiagnosticsAsync(app, d => d.Any(e => Code(e!) == "NZS102")))!;
        Assert.Equal(5, Range(style).StartLine);

        await client.ChangeAsync(app, "<Container>\n    <Button />\n</Container>\n", 3);
        await client.DiagnosticsAsync(app, d => d.Count == 0);
        await client.CloseAsync(app);
    }

    [Fact]
    public async Task Closed_components_have_the_diagnostics_of_the_build()
    {
        await client.InitializeAsync();
        var broken = PathOf("Broken.nazor");
        File.WriteAllText(broken, "<TextBlock>@missing</TextBlock>\n");
        var app = PathOf("App.nazor");
        await client.OpenAsync(app, "<Container />\n");

        var error = Assert.Single(await client.DiagnosticsAsync(broken, d => d.Count > 0))!;
        Assert.Equal("CS0103", Code(error));
        Assert.Equal((0, 12, 0, 19), Range(error));

        File.WriteAllText(broken, "<TextBlock>Ok</TextBlock>\n");
        client.ForgetDiagnostics(broken);
        await client.ChangeAsync(app, "<Container></Container>\n", 2);
        await client.DiagnosticsAsync(broken, d => d.Count == 0);

        client.ForgetDiagnostics(broken);
        await client.OpenAsync(broken, "<TextBlock>@other</TextBlock>\n");
        Assert.Single(await client.DiagnosticsAsync(broken, d => d.Count > 0));
        client.ForgetDiagnostics(broken);
        await client.CloseAsync(broken);
        await client.DiagnosticsAsync(broken, d => d.Count == 0);
    }

    [Fact]
    public async Task Open_components_see_each_other_before_they_are_saved()
    {
        await client.InitializeAsync();
        var card = PathOf("Card.nazor");
        var page = PathOf("Page.nazor");
        await client.OpenAsync(card, "<TextBlock>@Title</TextBlock>\n@code { [Parameter] public string? Title { get; set; } }\n");
        await client.OpenAsync(page, "<Container>\n    <Card Title=\"Hello\" />\n</Container>\n");
        await client.DiagnosticsAsync(page, d => d.Count == 0);

        var items = (await client.AtAsync("textDocument/completion", page, 1, 6))!["items"]!.AsArray();
        Assert.Contains(items, i => i!["label"]!.GetValue<string>() == "Card");
        var attributes = (await client.AtAsync("textDocument/completion", page, 1, 24))!["items"]!.AsArray();
        Assert.Contains(attributes, i => i!["label"]!.GetValue<string>() == "@key");
        Assert.DoesNotContain(attributes, i => i!["label"]!.GetValue<string>() == "Title");
    }

    [Fact]
    public async Task Completion_and_hover_answer_on_open_documents()
    {
        await client.InitializeAsync();
        var theme = PathOf("Light.nss");
        await client.OpenAsync(theme, ".a {\n    display: \n}\n");
        var values = (await client.AtAsync("textDocument/completion", theme, 1, 13))!["items"]!.AsArray();
        var flex = values.Single(i => i!["label"]!.GetValue<string>() == "flex")!;
        Assert.Equal(12, flex["kind"]!.GetValue<int>());
        Assert.Equal("flex", flex["textEdit"]!["newText"]!.GetValue<string>());

        var hover = (await client.AtAsync("textDocument/hover", theme, 1, 6))!;
        Assert.Equal("markdown", hover["contents"]!["kind"]!.GetValue<string>());
        Assert.Contains("`block | inline | flex | grid | none`", hover["contents"]!["value"]!.GetValue<string>());
        Assert.Equal((1, 4, 1, 11), Range(hover));

        var app = PathOf("App.nazor");
        await client.OpenAsync(app, "<Container>\n    <\n</Container>\n");
        var elements = (await client.AtAsync("textDocument/completion", app, 1, 5))!["items"]!.AsArray();
        Assert.Contains(elements, i => i!["label"]!.GetValue<string>() == "Button");
        Assert.Null(await client.AtAsync("textDocument/hover", app, 2, 0));
    }

    [Theory]
    [InlineData("App.nazor")]
    [InlineData("App.nazor.nss")]
    [InlineData("Theme.nss")]
    public async Task Visual_Studio_NSS_completion_sees_unsaved_custom_components_and_deep(string name)
    {
        await client.InitializeAsVisualStudioAsync();
        await client.OpenAsync(PathOf("Clock.nazor"), "<TextBlock>Clock</TextBlock>");
        var path = PathOf(name);
        var prefix = name.EndsWith(".nazor", StringComparison.Ordinal) ? "@style {\n" : ".bar {\n";
        await client.OpenAsync(path, prefix + "    Clo\n}\n");
        var items = (await client.AtAsync("textDocument/completion", path, 1, 7))!["items"]!.AsArray();
        var clock = Assert.Single(items, i => i!["label"]!.GetValue<string>() == "Clock")!;
        Assert.Contains("Clock", clock["detail"]!.GetValue<string>());
        Assert.Equal("Clock", clock["textEdit"]!["newText"]!.GetValue<string>());
        await client.ChangeAsync(path, prefix + "    Clock ::d\n}\n", 2);
        items = (await client.AtAsync("textDocument/completion", path, 1, 13))!["items"]!.AsArray();
        var deep = Assert.Single(items, i => i!["label"]!.GetValue<string>() == "::deep")!;
        Assert.Equal("::deep ", deep["textEdit"]!["newText"]!.GetValue<string>());
        Assert.Equal(10, deep["textEdit"]!["range"]!["start"]!["character"]!.GetValue<int>());
        await client.ChangeAsync(path, prefix + "    Clock ::deep TextBlock { padding: 1px; }\n}\n", 3);
        var hover = await client.AtAsync("textDocument/hover", path, 1, 13);
        Assert.Contains("Crosses component", hover!["contents"]!["value"]!.GetValue<string>());
    }

    [Theory]
    [InlineData("App.nazor", false)]
    [InlineData("App.nazor.nss", false)]
    [InlineData("Theme.nss", false)]
    [InlineData("App.nazor", true)]
    [InlineData("App.nazor.nss", true)]
    [InlineData("Theme.nss", true)]
    public async Task System_palette_help_and_completion_reach_both_editor_clients(string name, bool visualStudio)
    {
        if (visualStudio) await client.InitializeAsVisualStudioAsync();
        else await client.InitializeAsync();
        var path = PathOf(name);
        string Wrap(string sheet) => name.EndsWith(".nazor", StringComparison.Ordinal) ? "<Container />\n@style {\n" + sheet + "\n}" : sheet;
        await client.OpenAsync(path, Wrap(".a {}"));
        var version = 1;
        async Task<JsonNode?> Query(string sheet, string method = "textDocument/completion")
        {
            var source = Wrap(sheet);
            var caret = source.IndexOf('|');
            var lines = source[..caret].Split('\n');
            await client.ChangeAsync(path, source.Remove(caret, 1), ++version);
            return await client.AtAsync(method, path, lines.Length - 1, lines[^1].Length);
        }

        var colors = (await Query(".a { --surface: Can|; }"))!["items"]!.AsArray();
        var canvas = Assert.Single(colors, i => i!["label"]!.GetValue<string>() == "Canvas")!;
        Assert.Contains("var(--nz-system-canvas)", canvas["documentation"]!["value"]!.GetValue<string>());
        Assert.Null(canvas["_vs_icon"]);
        Assert.Null(canvas["data"]);
        var hover = await Query(".a { background: Can|vas; }", "textDocument/hover");
        Assert.Contains("Redefining", hover!["contents"]!["value"]!.GetValue<string>());
        var variables = (await Query(".a { color: var(--nz-system-|); }"))!["items"]!.AsArray();
        var tooltip = Assert.Single(variables, i => i!["label"]!.GetValue<string>() == "--nz-system-tooltip-text")!;
        Assert.Contains("fallback", tooltip["documentation"]!["value"]!.GetValue<string>());
        hover = await Query(".a { color: var(--nz-system-can|vas); }", "textDocument/hover");
        Assert.Contains("overriding this variable changes that keyword", hover!["contents"]!["value"]!.GetValue<string>());
    }

    [Theory]
    [InlineData("App.nazor")]
    [InlineData("App.nazor.nss")]
    [InlineData("Theme.nss")]
    public async Task Visual_Studio_recognizes_scrollbar_styles_and_offers_properties_values_and_hover(string name)
    {
        await client.InitializeAsVisualStudioAsync();
        var path = PathOf(name);
        var inline = name.EndsWith(".nazor", StringComparison.Ordinal);
        string Wrap(string sheet) => inline ? "<Container />\n@style {\n" + sheet + "\n}" : sheet;
        await client.OpenAsync(path, Wrap(".bar {}"));
        var version = 1;

        async Task<JsonNode?> Query(string sheet, string method = "textDocument/completion")
        {
            var source = Wrap(sheet);
            var caret = source.IndexOf('|');
            var lines = source[..caret].Split('\n');
            await client.ChangeAsync(path, source.Remove(caret, 1), ++version);
            return await client.AtAsync(method, path, lines.Length - 1, lines[^1].Length);
        }

        var properties = (await Query(".bar { scroll| }"))!["items"]!.AsArray();
        foreach (var property in Nazor.Style.StyleProperties.All.Where(p => p.Name.StartsWith("scrollbar-", StringComparison.Ordinal) || p.Name.StartsWith("scroll-", StringComparison.Ordinal)))
        {
            var item = Assert.Single(properties, i => i!["label"]!.GetValue<string>() == property.Name)!;
            Assert.Contains(property.Syntax, item["documentation"]!["value"]!.GetValue<string>());
        }

        foreach (var (property, expected) in new[]
        {
            ("scroll-behavior", new[] { "auto", "smooth" }),
            ("scroll-wheel-step", new[] { "auto", "lines", "calc()" }),
            ("scroll-timing-function", new[] { "ease-out", "cubic-bezier()" }),
            ("scrollbar-visibility", new[] { "none", "visible", "autohide" }),
            ("scrollbar-placement", new[] { "overlay", "reserve" }),
            ("scrollbar-buttons", new[] { "none", "visible" }),
            ("scrollbar-track-radius", new[] { "auto" }),
            ("scrollbar-thumb-expanded-width", new[] { "auto" }),
            ("scrollbar-thumb-hover-color", new[] { "auto", "currentcolor", "rgb()" }),
            ("scrollbar-track-border-color", new[] { "currentcolor", "rgb()" }),
            ("scrollbar-fade-timing-function", new[] { "ease-out", "cubic-bezier()" }),
        })
        {
            var items = (await Query($".bar {{ {property}: | }}"))!["items"]!.AsArray();
            foreach (var label in expected) Assert.Contains(items, i => i!["label"]!.GetValue<string>() == label);
        }

        var pseudo = (await Query(".bar:wheel-| {}"))!["items"]!.AsArray();
        Assert.Contains(pseudo, i => i!["label"]!.GetValue<string>() == "wheel-scrolling");
        var hover = await Query(".bar { scrollbar-pla|cement: reserve; }", "textDocument/hover");
        Assert.Contains("Autohide always uses overlay", hover!["contents"]!["value"]!.GetValue<string>());

        client.ForgetDiagnostics(path);
        await client.ChangeAsync(path, Wrap("""
            .bar {
                overflow: auto;
                scroll-behavior: smooth;
                scroll-duration: 180ms;
                scroll-timing-function: cubic-bezier(.2, 0, .8, 1);
                scroll-wheel-step: 1.5 lines;
                scrollbar-visibility: autohide;
                scrollbar-placement: overlay;
                scrollbar-width: 8px;
                scrollbar-thumb-color: red;
                scrollbar-thumb-width: 3px;
                scrollbar-thumb-expanded-width: 8px;
                scrollbar-thumb-hover-color: auto;
                scrollbar-thumb-active-color: currentColor;
                scrollbar-thumb-border-width: 1px;
                scrollbar-thumb-border-color: CanvasText;
                scrollbar-thumb-border-highlight-color: auto;
                scrollbar-track-color: transparent;
                scrollbar-track-hover-color: Canvas;
                scrollbar-radius: 4px;
                scrollbar-inset: 2px;
                scrollbar-min-thumb-length: 24px;
                scrollbar-opacity: 0.8;
                scrollbar-fade-in-duration: 120ms;
                scrollbar-fade-out-duration: 200ms;
                scrollbar-fade-out-delay: 300ms;
                scrollbar-fade-timing-function: cubic-bezier(0, 0, 1, 1);
                scrollbar-wheel-timeout: 600ms;
                scrollbar-reveal-distance: 12px;
                scrollbar-track-radius: auto;
                scrollbar-track-border-width: 1px;
                scrollbar-track-border-color: currentColor;
                scrollbar-buttons: visible;
                scrollbar-button-length: 16px;
                scrollbar-button-color: white;
                scrollbar-button-background: gray;
                scrollbar-button-hover-background: blue;
                scrollbar-button-active-background: navy;
                scrollbar-button-disabled-color: silver;
                scrollbar-button-radius: 2px;
                scrollbar-button-step: 40px;
                scrollbar-button-repeat-delay: 400ms;
                scrollbar-button-repeat-interval: 50ms;
                transition: scrollbar-opacity 200ms ease-out;
            }
            .bar:wheel-scrolling { scrollbar-visibility: none; }
            """), ++version);
        await client.DiagnosticsAsync(path, d => d.Count == 0);
    }

    [Theory]
    [InlineData("Samples/Nazor.Samples.Counter/Counter.nazor")]
    [InlineData("Samples/Nazor.Samples.Theming/App.nazor")]
    [InlineData("Samples/Nazor.Samples.Theming/App.nazor.nss")]
    [InlineData("Tests/Fixtures/Projects/DeviceRegistry/App.nazor")]
    [InlineData("Tests/Fixtures/Projects/DeviceRegistry/Components/DeviceCard.nazor")]
    [InlineData("Tests/Fixtures/Projects/DeviceRegistry/Components/DeviceCard.nazor.nss")]
    [InlineData("Samples/Nazor.Samples.Tasks/Views/TasksView.nazor")]
    [InlineData("Samples/Nazor.Samples.Tasks/Components/ItemList.nazor")]
    [InlineData("Samples/Nazor.Samples.Windowing/LauncherWindow.nazor")]
    [InlineData("Samples/Nazor.Samples.Tasks/Views/ScrollbarsView.nazor")]
    [InlineData("Samples/Nazor.Samples.Tasks/Views/ScrollbarsView.nazor.nss")]
    [InlineData("Samples/Nazor.Samples.Tasks/Components/ScrollTaskRows.nazor")]
    [InlineData("Tests/Fixtures/Projects/ExternalComponents/GenericCard.nazor")]
    public async Task The_samples_of_the_repository_have_no_errors_or_warnings_as_in_the_build(string file)
    {
        await client.InitializeAsync();
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../.."));
        var path = Path.GetFullPath(Path.Combine(root, file));
        await client.OpenAsync(path, File.ReadAllText(path));
        var diagnostics = await client.DiagnosticsAsync(path);
        Assert.True(diagnostics.All(d => d!["severity"]!.GetValue<int>() >= 3), diagnostics.ToJsonString());
        if (file.EndsWith("DeviceRegistry/App.nazor", StringComparison.Ordinal))
        {
            var items = (await client.AtAsync("textDocument/completion", path, 0, 1))!["items"]!.AsArray();
            Assert.Contains(items, i => i!["label"]!.GetValue<string>() == "code");
            var text = File.ReadAllText(path);
            var tag = Protocol.TextPositions.Position(text, text.IndexOf("<DeviceCard", StringComparison.Ordinal) + 2);
            var hover = await client.AtAsync("textDocument/hover", path, tag.Line, tag.Character);
            Assert.Contains("DeviceRegistry.Components.DeviceCard", hover!["contents"]!["value"]!.GetValue<string>());
        }
    }

    [Fact]
    public async Task Clients_without_markdown_get_plain_text()
    {
        await client.RequestAsync("initialize", new JsonObject
        {
            ["capabilities"] = new JsonObject { ["textDocument"] = new JsonObject { ["hover"] = new JsonObject { ["contentFormat"] = new JsonArray("plaintext") } } },
        });
        var theme = PathOf("Plain.nss");
        await client.OpenAsync(theme, ".a { display: flex; }");
        var contents = (await client.AtAsync("textDocument/hover", theme, 0, 7))!["contents"]!;
        Assert.Equal("plaintext", contents["kind"]!.GetValue<string>());
        Assert.StartsWith("display: block | inline | flex | grid | none", contents["value"]!.GetValue<string>());
    }

    [Fact]
    public async Task Closing_a_document_clears_its_diagnostics()
    {
        await client.InitializeAsync();
        var theme = PathOf("Broken.nss");
        await client.OpenAsync(theme, ".a { colr: red; }");
        await client.DiagnosticsAsync(theme, d => d.Count == 1);
        await client.CloseAsync(theme);
        await client.DiagnosticsAsync(theme, d => d.Count == 0);
    }

    [Fact]
    public async Task Unknown_requests_fail_and_unknown_notifications_are_ignored()
    {
        await client.InitializeAsync();
        await client.NotifyAsync("workspace/somethingNew", []);
        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => client.RequestAsync("nazor/unknownRequest", []));
        Assert.Contains("-32601", error.Message);
        Assert.Null(await client.RequestAsync("shutdown", []));
    }

    [Fact]
    public async Task Shutdown_then_exit_ends_the_server_with_success()
    {
        await client.InitializeAsync();
        Assert.Null(await client.RequestAsync("shutdown", []));
        await client.NotifyAsync("exit");
        Assert.Equal(0, await client.Server!.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Exit_without_shutdown_is_a_failure()
    {
        await client.InitializeAsync();
        await client.NotifyAsync("exit");
        Assert.Equal(1, await client.Server!.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task The_server_process_speaks_LSP_on_stdio()
    {
        var configuration = new DirectoryInfo(AppContext.BaseDirectory).Parent!.Name;
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../.."));
        var server = Environment.GetEnvironmentVariable("NAZOR_TEST_SERVER") ??
            Path.Combine(root, "Sources/Tools/Nazor.LanguageServer/bin", configuration, "net10.0", "Nazor.LanguageServer.dll");
        var host = Environment.GetEnvironmentVariable("DOTNET_HOST_PATH") is { Length: > 0 } path && File.Exists(path) ? path : "dotnet";
        using var process = Process.Start(new ProcessStartInfo(host, [server])
        {
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        })!;
        try
        {
            await using var remote = new LspClient(process.StandardOutput.BaseStream, process.StandardInput.BaseStream);
            var initialized = await remote.InitializeAsVisualStudioAsync();
            Assert.Equal("Nazor", initialized!["serverInfo"]!["name"]!.GetValue<string>());
            var theme = PathOf("Process.nss");
            await remote.OpenAsync(theme, "Button { padding: 4px; colr: red; }");
            Assert.Equal("NZS101", Code(Assert.Single(await remote.DiagnosticsAsync(theme))!));
            var app = PathOf("Process.nazor");
            var build = Directory.CreateDirectory(Path.Combine(directory, "obj", "Nazor")).FullName;
            File.WriteAllText(Path.Combine(build, "nazorc.rsp"), string.Join("\n",
                "generate", "--root-namespace", "Demo", "--language-version", "15.0", "--reference", typeof(ComponentBase).Assembly.Location,
                "--reference", typeof(Nazor.Controls.Button).Assembly.Location, "--reference", typeof(Nazor.Style.StyleSheet).Assembly.Location));
            const string text = "<Button Tooltip=\"help\">@Count</Button>\n@code { private int Count { get; set; } = 1; }\n";
            await remote.OpenAsync(app, text);
            for (var version = 2; version <= 30; version++)
                await remote.ChangeAsync(app, text.Replace("= 1;", $"= {version};", StringComparison.Ordinal), version);
            await remote.NotifyAsync("workspace/didChangeWatchedFiles", new JsonObject { ["changes"] = new JsonArray() });
            Assert.Contains("Nazor.Controls.Button", (await remote.AtAsync("textDocument/hover", app, 0, 3))!.ToJsonString());
            Assert.Contains("Count", (await remote.AtAsync("textDocument/hover", app, 0, text.IndexOf("@Count", StringComparison.Ordinal) + 2))!.ToJsonString());
            var legend = initialized!["capabilities"]!["semanticTokensProvider"]!["legend"]!["tokenTypes"]!.AsArray();
            var data = (await remote.RequestAsync("textDocument/semanticTokens/full", new JsonObject
            {
                ["textDocument"] = new JsonObject { ["uri"] = LspClient.Uri(app) },
            }))!["data"]!.AsArray();
            var types = Enumerable.Range(0, data.Count / 5).Select(i => legend[data[i * 5 + 3]!.GetValue<int>()]!.GetValue<string>()).ToArray();
            Assert.Contains("razorComponentElement", types);
            Assert.Contains("property name", types);
            var modern = PathOf("Modern.nazor");
            const string modernText = "<TextBlock>@values.Count</TextBlock>\n@code { private readonly List<int> values = [with(capacity: 8), 1, 2, 3]; }";
            await remote.OpenAsync(modern, modernText);
            Assert.DoesNotContain(await remote.DiagnosticsAsync(modern), d => d!["severity"]!.GetValue<int>() == 1);
            Assert.Contains("List", (await remote.AtAsync("textDocument/hover", modern, 0, modernText.IndexOf("@values", StringComparison.Ordinal) + 2))!.ToJsonString());
            Assert.Null(await remote.RequestAsync("shutdown", []));
            await remote.NotifyAsync("exit");
            await process.WaitForExitAsync(TestContext.Current.CancellationToken).WaitAsync(TimeSpan.FromSeconds(30), TestContext.Current.CancellationToken);
            Assert.Equal(0, process.ExitCode);
        }
        finally
        {
            if (!process.HasExited) process.Kill(entireProcessTree: true);
        }
    }

    private static (int StartLine, int StartCharacter, int EndLine, int EndCharacter) Range(JsonNode node)
    {
        var range = node["range"]!;
        return (range["start"]!["line"]!.GetValue<int>(), range["start"]!["character"]!.GetValue<int>(),
            range["end"]!["line"]!.GetValue<int>(), range["end"]!["character"]!.GetValue<int>());
    }
}
