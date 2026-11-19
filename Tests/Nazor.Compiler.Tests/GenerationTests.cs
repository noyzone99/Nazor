using System.Security.Cryptography;
using Nazor.Compiler;
using Nazor.Rendering;
using Nazor.Testing;

namespace Nazor.Compiler.Tests;

public sealed class GenerationTests(CompilerFixture fixture) : IClassFixture<CompilerFixture>
{
    private readonly NazorCompiler compiler = fixture.Compiler;

    [Fact]
    public void Tooltip_trigger_and_content_slots_compile_and_retain_the_description_while_closed()
    {
        var assembly = compiler.Load([compiler.Generate(Build.Nazor("Tooltips"))]);
        using var host = new TestHost();
        var root = host.Render(assembly.ComponentType("Tooltips"));
        Assert.Equal("Save",root.Single("Button").Text());
        var tooltip = root.Single("Tooltip");
        Assert.Equal("Save the current document.",tooltip.Text());
        Assert.Equal(false,tooltip.Attributes["overlay"]);
        Assert.Equal(tooltip.Id,root.Single("TooltipTrigger").Attributes["tooltip-describes"]);
    }

    [Fact]
    public async Task Event_directives_modifiers_splat_and_bindings_compile_and_run()
    {
        var generated = compiler.Generate(Build.Nazor("EventSurface"));
        var assembly = compiler.Load([generated]);
        using var host = new TestHost();
        var root = host.Render(assembly.ComponentType("EventSurface"));
        var log = (List<string>)root.Component.GetType().GetProperty("Log")!.GetValue(root.Component)!;
        var button = root.Single("Button");
        Assert.Equal("splat", button.Attributes["data-example"]);
        await host.ClickAsync(button);
        Assert.Equal(["click"], log);
        log.Clear();
        await host.Renderer.DispatchEventAsync(button, "onlostpointercapture", new Nazor.Input.PointerEventArgs());
        Assert.Equal(["pointer"], log);
        log.Clear();
        var box = root.Single("TextFieldInput");
        await host.Renderer.DispatchEventAsync(box, "onkeydown", new Nazor.Input.KeyboardEventArgs { Key = "a" }, out var prevented);
        Assert.True(prevented);
        Assert.Equal(["key:a"], log);
        await host.Renderer.DispatchEventAsync(box, "oninput", new Nazor.Input.ChangeEventArgs { Value = "typed" });
        Assert.Contains("input:typed:typed:False", log);
        await host.ClickAsync(root.Single("CheckBox"));
        Assert.Contains("input:True:typed:True", log);
        Assert.Contains("change:True", log);
        Assert.DoesNotContain(assembly.GetReferencedAssemblies(), a => a.Name!.StartsWith("Microsoft.AspNetCore", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Tooltips_menus_and_flyouts_from_the_guide_compile_and_run()
    {
        var assembly = compiler.Load([compiler.Generate(Build.Nazor("Overlays"))]);
        using var host = new TestHost(new OverlayServices());
        var root = host.Render(assembly.ComponentType("Overlays"));
        var log = (List<string>)root.Component.GetType().GetProperty("Log")!.GetValue(root.Component)!;
        Assert.Equal("Save the document (Ctrl+S)", root.Elements("Button").First().Attributes["tooltip"]);
        Assert.DoesNotContain(root.Elements(), e => e.Name is "Menu" or "Popover");

        await host.Renderer.DispatchEventAsync(root.Single("Container"), "oncontextmenu", new Nazor.Input.PointerEventArgs { X = 5, Y = 6 });
        var menu = root.Single("Menu");
        Assert.Equal((5.0, 6.0), (menu.Attributes["anchor-x"], menu.Attributes["anchor-y"]));
        Assert.Equal([false, true], root.Elements("MenuItem").Select(e => e.Attributes["disabled"]));
        Assert.Equal("menuitemcheckbox",root.Single("MenuCheckItem").Attributes["role"]);
        Assert.Equal(2,root.Elements("MenuRadioItem").Count());
        Assert.Single(root.Elements("Submenu"));
        await host.ClickAsync(root.Elements("MenuItem").First());
        Assert.Equal(["copy"], log);
        Assert.Empty(root.Elements("Menu"));

        await host.ClickAsync(root.Elements("Button").ElementAt(1));
        Assert.Equal("More options", root.Single("Popover").Elements().First(e => e.Name == "TextBlock").Text());

        await host.ClickAsync(root.Single("SplitButtonTrigger"));
        await host.ClickAsync(root.Single("Menu").Elements().Single(e => e.Name == "MenuItem"));
        Assert.Equal(["copy", "save as"], log);
        Assert.Empty(root.Elements("Menu"));

        await host.ClickAsync(root.Single("MenuBarItem"));
        await host.ClickAsync(root.Single("Menu").Elements().Single(e => e.Name == "MenuItem"));
        Assert.Equal(["copy", "save as", "new"], log);
        Assert.Equal("toolbar", root.Single("Toolbar").Attributes["role"]);
    }

    private sealed class OverlayServices : IServiceProvider
    {
        private readonly Nazor.Input.KeyBindings bindings = new();
        public object? GetService(Type serviceType) => serviceType == typeof(Nazor.Input.KeyBindings) ? bindings : null;
    }

    [Fact]
    public void A_page_directive_gives_the_component_its_routes()
    {
        var assembly = compiler.Load([compiler.Generate(Build.Nazor("OrderPage"))]);
        var page = assembly.ComponentType("OrderPage");
        Assert.Equal(["/orders/{Id:int}", "/orders/{Id:int}/{View}"],
            page.GetCustomAttributes(typeof(RouteAttribute), false).Cast<RouteAttribute>().Select(r => r.Template).Order());
        var match = Nazor.Routing.RouteTable.Create([assembly]).Match("/orders/42/lines")!;
        Assert.Equal(page, match.Page);
        Assert.Equal((42, "lines"), (match.Parameters["Id"], match.Parameters["View"]));
    }

    [Fact]
    public async Task Choice_and_progress_controls_from_the_guide_compile_and_bind()
    {
        var assembly = compiler.Load([compiler.Generate(Build.Nazor("Choices"))]);
        using var host = new TestHost();
        var root = host.Render(assembly.ComponentType("Choices"));
        object? Read(string name) => root.Component.GetType().GetProperty(name)!.GetValue(root.Component);

        var radios = root.Elements("RadioButton").ToList();
        Assert.Equal([false, true, false], radios.Select(e => e.Attributes["checked"]));
        Assert.Equal(true, radios[2].Attributes["disabled"]);
        await host.ClickAsync(radios[0]);
        Assert.Equal("Standard", Read("Selection")!.ToString());

        await host.ClickAsync(root.Single("Switch"));
        Assert.Equal(true, Read("Notifications"));
        Assert.Single(root.Elements("Spinner"));

        var slider = root.Single("Slider");
        Assert.Equal("--nz-slider-fraction: 0.4; --nz-slider-position: 0.4; width: 180px", slider.Attributes["style"]);
        await host.Renderer.DispatchEventAsync(slider, "oninput", new Nazor.Input.ChangeEventArgs { Value = 65.0 });
        Assert.Equal(65.0, Read("Volume"));
        Assert.Equal(65.0, root.Single("ProgressBar").Attributes["range-value"]);

        var combos = root.Elements("Select").ToList();
        Assert.Equal(["Country", "2 items"], combos.Select(c => c.Elements().First(e => e.Name == "SelectValue").Text()));
        await host.ClickAsync(combos[0]);
        await host.ClickAsync(combos[0].Elements().Single(e => e.Name == "SelectOption" && e.Text() == "France"));
        Assert.Equal("FR", Read("Country"));
        Assert.Equal("France", root.Elements("Select").First().Elements().First(e => e.Name == "SelectValue").Text());

        var rows = root.Single("ListView").Elements().Where(e => e.Name == "ListViewItem").ToList();
        Assert.Equal(["A1", "B2", "C3"], rows.Select(r => r.Text()));
        await host.ClickAsync(rows[1]);
        Assert.Equal("B2", Read("Selected"));
        await host.Renderer.DispatchEventAsync(rows[2], "ondblclick", new Nazor.Input.PointerEventArgs());
        Assert.Equal("C3", Read("Opened"));

        Assert.Equal("Delivery", root.Single("DatePickerInput").Attributes["placeholder-text"]);
        var days = root.Single("Calendar").Elements().Where(e => e.Name == "CalendarDay").ToList();
        Assert.Equal(true, days.Single(d => Equals(d.Attributes.GetValueOrDefault("accessible-name"), "domenica 18 ottobre 2026")).Attributes["unavailable"]);
        await host.ClickAsync(days.Single(d => Equals(d.Attributes.GetValueOrDefault("accessible-name"), "lunedì 19 ottobre 2026")));
        Assert.Equal(new DateOnly(2026, 10, 19), Read("Delivery"));
        Assert.Equal("19/10/2026", root.Single("DatePicker").Elements().First(e => e.Name == "DatePickerInput").Attributes["value"]);
    }

    [Fact]
    public void A_component_of_the_project_hides_a_library_component_with_the_same_name()
    {
        var directory = Build.Fixture("Shadowing");
        var assembly = compiler.Load(compiler.Generate([Path.Combine(directory, "Progress.nazor"), Path.Combine(directory, "Uses.nazor")]));
        using var host = new TestHost();
        var root = host.Render(assembly.ComponentType("Uses"));
        Assert.Equal("la mia barra", root.Elements().Single(e => e.Class == "mine").Text());
        Assert.Equal(40.0, root.Single("ProgressBar").Attributes["range-value"]);
    }

    [Fact]
    public async Task Counter_compiles_renders_and_handles_a_click()
    {
        var assembly = compiler.Load([compiler.Generate(Build.Nazor("Counter"))]);
        using var host = new TestHost();
        var root = host.Render(assembly.ComponentType("Counter"));

        Assert.Equal("""
            Container.counter
              TextBlock
                "Count: "
                "0"
              Button
                ButtonLabel
                  "Increment"
            """.ReplaceLineEndings("\n"), root.Dump());
        await host.ClickAsync(root.Single("Button"));
        Assert.Equal("Count: 1Increment", root.Text());

        Assert.DoesNotContain(assembly.GetReferencedAssemblies(), a => a.Name!.StartsWith("Microsoft.AspNetCore", StringComparison.Ordinal));
        Assert.DoesNotContain(typeof(ComponentBase).Assembly.GetReferencedAssemblies(), a => a.Name!.StartsWith("Microsoft.AspNetCore", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Code_behind_if_foreach_keys_children_and_async_event()
    {
        var generated = compiler.Generate(Build.Nazor("Flow"));
        var assembly = compiler.Load([generated], Build.Nazor("Flow") + ".cs");
        using var host = new TestHost();
        var root = host.Render(assembly.ComponentType("Flow"));

        Assert.Equal(["Item: 3", "Item: 1", "Item: 2"], root.Elements("TextBlock").Select(e => e.Text()));
        var click = host.ClickAsync(root.Single("Button"));
        host.RunUntil(() => click.IsCompleted);
        await click;
        Assert.Empty(root.Elements("TextBlock"));
    }

    [Fact]
    public void Elements_take_the_scope_of_the_component_that_declared_them()
    {
        var consumer = Build.Compiler(Build.Library);
        var assembly = consumer.Load([consumer.Generate(Build.Nazor("LibraryCounter"))]);
        using var host = new TestHost();
        var root = host.Render(assembly.ComponentType("LibraryCounter"));
        var libraryCounter = assembly.ComponentType("LibraryCounter");
        var deviceCard = typeof(Nazor.Sample.Components.DeviceCard);

        var card = root.Elements("Container").Single(e => e.Class == "device-card");
        Assert.Equal(deviceCard, card.Scope);
        Assert.Equal(deviceCard, root.Single("Button").Scope);
        var projected = root.Elements("TextBlock").Single(e => e.Text() == "Contenuto del chiamante");
        Assert.Equal(libraryCounter, projected.Scope);
        Assert.Equal(libraryCounter, root.Elements("TextBlock").Last().Scope);
    }

    [Fact]
    public async Task Compiled_library_accepts_parameters_children_and_a_parent_callback()
    {
        var consumer = Build.Compiler(Build.Library);
        var assembly = consumer.Load([consumer.Generate(Build.Nazor("LibraryCounter"))]);
        using var host = new TestHost();
        var root = host.Render(assembly.ComponentType("LibraryCounter"));

        Assert.Contains("Sensore laboratorioAttivoContenuto del chiamante", root.Text());
        Assert.Contains("Selezioni: 0", root.Text());
        await host.ClickAsync(root.Single("Button"));
        Assert.Contains("Selezioni: 1", root.Text());

        Assert.Contains(assembly.GetReferencedAssemblies(), a => a.Name == "Nazor.Sample.Components");
        var library = typeof(Nazor.Sample.Components.DeviceCard).Assembly;
        foreach (var output in new[] { assembly, library })
            Assert.DoesNotContain(output.GetReferencedAssemblies(), a => a.Name!.StartsWith("Microsoft.AspNetCore", StringComparison.Ordinal) || a.Name == "Nazor.Compiler");
    }

    [Fact]
    public void External_components_require_an_explicit_reference()
    {
        _ = typeof(Nazor.Sample.Components.DeviceCard);
        var error = Build.Fails(() => compiler.Generate(Build.Nazor("LibraryCounter")));
        Assert.Contains(error.Diagnostics, d => d.Id == "RZ10012" && d.Message.Contains("DeviceCard"));
    }

    [Fact]
    public void Inherited_parameters_fully_qualified_tags_and_duplicate_references()
    {
        var consumer = Build.Compiler(Build.Library, Build.Library);
        var assembly = consumer.Load([consumer.Generate(Build.Nazor("Inherited"))]);
        using var host = new TestHost();
        Assert.Equal("Parametro ereditatoChild proiettato", host.Render(assembly.ComponentType("Inherited")).Text());
    }

    [Fact]
    public async Task Project_components_discover_each_other_code_behind_and_folder_namespaces()
    {
        var project = Build.Fixture("Project");
        var projectCompiler = new NazorCompiler(new NazorCompilerOptions
        {
            RootNamespace = "ProjectSample",
            ProjectDirectory = project,
            References = [Build.Core, Build.Controls, Build.Style],
            CSharpSources = [Path.Combine(project, "Components", "Badge.nazor.cs")],
        });
        var generated = projectCompiler.Generate(Directory.GetFiles(project, "*.nazor", SearchOption.AllDirectories)
            .Where(f => Path.GetFileName(f) != "_Imports.nazor").ToArray());
        var assembly = projectCompiler.Load(generated);

        Assert.Equal("ProjectSample.App", assembly.ComponentType("App").FullName);
        Assert.Equal("ProjectSample.Components.Badge", assembly.ComponentType("Badge").FullName);
        using var host = new TestHost();
        var root = host.Render(assembly.ComponentType("App"));
        Assert.Equal("iniziale (0)", root.Single("TextBlock").Text());

        var increment = root.Elements("Button").Single(b => b.Class is null);
        Assert.Equal("Più uno", increment.Text());
        await host.ClickAsync(increment);
        Assert.Equal("iniziale (1)", root.Single("TextBlock").Text());

        await host.ClickAsync(root.Elements("Button").Single(b => b.Class == "editor"));
        Assert.Equal("iniziale · modificato (1)", root.Single("TextBlock").Text());
    }

    [Fact]
    public async Task Bind_is_checked_round_trips_through_the_check_box()
    {
        var assembly = compiler.Load([compiler.Generate(Build.Nazor("BindCheck"))]);
        using var host = new TestHost();
        var root = host.Render(assembly.ComponentType("BindCheck"));
        Assert.Equal("no", root.Single("TextBlock").Text());

        await host.ClickAsync(root.Single("CheckBox"));
        Assert.Equal("sì", root.Single("TextBlock").Text());
        Assert.Equal(true, root.Single("CheckBox").Attributes["checked"]);
        Assert.Equal("check", root.Single("CheckBoxCheck").Attributes["glyph"]);
        Assert.Equal("minus", root.Single("CheckBoxMixed").Attributes["glyph"]);
    }

    [Fact]
    public async Task Bind_text_round_trips_through_the_text_box()
    {
        var assembly = compiler.Load([compiler.Generate(Build.Nazor("BindText"))]);
        using var host = new TestHost();
        var root = host.Render(assembly.ComponentType("BindText"));
        Assert.Equal("mondo", root.Single("TextFieldInput").Attributes["value"]);

        await host.Renderer.DispatchEventAsync(root.Single("TextFieldInput"), "oninput", new Nazor.Input.ChangeEventArgs { Value = "Zoë" });
        Assert.Equal("Ciao Zoë", root.Single("TextBlock").Text());
        Assert.Equal("Zoë", root.Single("TextFieldInput").Attributes["value"]);
    }

    [Fact]
    public void Inject_receives_services_from_the_host()
    {
        var assembly = compiler.Load([compiler.Generate(Build.Nazor("Injected"))]);
        using var host = new TestHost(new SingleService(new List<string> { "Ada", "Zoë" }));
        var root = host.Render(assembly.ComponentType("Injected"));
        Assert.Equal("Ada, Zoë", root.Single("TextBlock").Text());
    }

    private sealed class SingleService(object service) : IServiceProvider
    {
        public object? GetService(Type serviceType) => serviceType.IsInstanceOfType(service) ? service : null;
    }

    [Fact]
    public void Nss_blocks_keep_order_offsets_and_are_opaque_to_razor()
    {
        var path = Build.Nazor("Styled");
        var source = File.ReadAllText(path);
        var extracted = StyleExtractor.Extract(source, path);
        Assert.Equal(source.Length, extracted.Razor.Length);
        for (var i = 0; i < source.Length; i++)
            if (source[i] is '\r' or '\n') Assert.Equal(source[i], extracted.Razor[i]);

        var generated = compiler.Generate(path);
        Assert.Equal(2, generated.Styles.Count);
        Assert.EndsWith(".nss", generated.Styles[0].Path);
        Assert.Contains("@media", generated.Styles[1].Text);
        Assert.Contains("&:hover", generated.Styles[1].Text);
        Assert.Equal(generated.Styles[1].Text, source.Substring(generated.Styles[1].Offset, generated.Styles[1].Text.Length));
        Assert.Contains("\"global::Microsoft.AspNetCore.Components\"", generated.CSharp);
        compiler.Load([generated]);
    }

    [Fact]
    public void Debug_checksum_and_line_pragmas_refer_to_the_original_file()
    {
        var path = Build.Nazor("Counter");
        var generated = compiler.Generate(path);
        var checksum = Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(path)));
        Assert.StartsWith($"#pragma checksum \"{path}\" \"{{8829d00f-11b8-4213-878b-770e8597ac16}}\" \"{checksum}\"", generated.CSharp);
        var expressionLine = Array.FindIndex(File.ReadAllLines(path), line => line.Contains("Count: @count", StringComparison.Ordinal)) + 1;
        Assert.True(expressionLine > 0);
        Assert.Contains($"#line {expressionLine} \"{path}\"", generated.CSharp);
    }

    [Fact]
    public void Line_pragmas_point_only_to_files_that_exist()
    {
        var generated = compiler.Generate(Build.Nazor("Counter"));
        var targets = System.Text.RegularExpressions.Regex.Matches(generated.CSharp, "#line[^\"\\r\\n]*\"([^\"]+)\"")
            .Select(m => m.Groups[1].Value).Distinct().ToList();
        Assert.NotEmpty(targets);
        Assert.All(targets, target => Assert.True(File.Exists(target), $"#line points to '{target}', which does not exist."));
    }

    [Fact]
    public void Generated_code_passes_the_declaring_scope_to_component_frames()
    {
        var generated = compiler.Generate(Build.Nazor("Counter"));
        Assert.Contains("OpenComponent<global::Nazor.Controls.Container>(0, typeof(global::Nazor.Generated.Counter), this)", generated.CSharp);
        Assert.Contains("global::Nazor.EventCallback.Factory.Create<global::Nazor.Input.PointerEventArgs>", generated.CSharp);
        Assert.DoesNotContain("Microsoft.AspNetCore.Components.Web", generated.CSharp);
    }
}

public sealed class DiagnosticTests(CompilerFixture fixture) : IClassFixture<CompilerFixture>
{
    private readonly NazorCompiler compiler = fixture.Compiler;

    [Fact]
    public void Csharp_errors_after_style_extraction_keep_the_nazor_line()
    {
        var error = Build.Fails(() => compiler.Load([compiler.Generate(Build.Nazor("InvalidExpression"))]));
        var diagnostic = Assert.Single(error.Diagnostics, d => d.Id == "CS0103");
        Assert.EndsWith("InvalidExpression.nazor", diagnostic.Path);
        Assert.Equal(5, diagnostic.Line);
        Assert.Contains("InvalidExpression.nazor(5,", error.Message);
        Assert.Contains(": error CS0103:", error.Message);
    }

    [Fact]
    public void Csharp_errors_after_a_style_that_follows_code_keep_the_nazor_line()
    {
        var error = Build.Fails(() => compiler.Load([compiler.Generate(Build.Nazor("InvalidAfterCodeStyle"))]));
        Assert.Contains(error.Diagnostics, d => d.Id == "CS0103" && d.Line == 7 && d.Path.EndsWith("InvalidAfterCodeStyle.nazor"));
    }

    [Fact]
    public void Razor_syntax_errors_refer_to_the_original_file()
    {
        var error = Build.Fails(() => compiler.Generate(Build.Nazor("InvalidRazor")));
        Assert.Contains(error.Diagnostics, d => d.Id.StartsWith("RZ", StringComparison.Ordinal) && d.Path.EndsWith("InvalidRazor.nazor"));
    }

    [Fact]
    public void Unknown_controls_are_errors()
    {
        var error = Build.Fails(() => compiler.Generate(Build.Nazor("Unknown")));
        Assert.Contains(error.Diagnostics, d => d.Id == "RZ10012" && d.IsError && d.Message.Contains("UnknownControl"));
        Assert.DoesNotContain(error.Diagnostics, d => d.Id == "NZC010");
    }

    [Fact]
    public void Html_elements_and_static_markup_are_rejected_at_their_position()
    {
        var error = Build.Fails(() => compiler.Generate(Build.Nazor("HtmlElement")));
        Assert.Contains(error.Diagnostics, d => d.Id == "NZC010" && d.Line == 2 && d.Column == 5 && d.Message.Contains("<div>"));
        Assert.Contains(error.Diagnostics, d => d.Id == "NZC010" && d.Line == 3 && d.Column == 5 && d.Message.Contains("<button>"));
    }

    [Fact]
    public void External_parameter_type_errors_map_to_the_consumer_file()
    {
        var consumer = Build.Compiler(Build.Library);
        var error = Build.Fails(() => consumer.Load([consumer.Generate(Build.Nazor("InvalidLibraryParameter"))]));
        Assert.Contains(error.Diagnostics, d => d.Id.StartsWith("CS", StringComparison.Ordinal) && d.Line == 3 && d.Path.EndsWith("InvalidLibraryParameter.nazor"));
    }
}

public sealed class StyleExtractionTests(CompilerFixture fixture) : IClassFixture<CompilerFixture>
{
    [Fact]
    public void Unclosed_and_malformed_blocks_report_source_positions()
    {
        var unterminated = Build.Fails(() => StyleExtractor.Extract("\n@style { .x { color: red; }", "Bad.nazor"));
        Assert.Equal("Bad.nazor(2,8): error NZS002: Unterminated @style block, string or comment.", unterminated.Message);
        Assert.Contains(Build.Fails(() => StyleExtractor.Extract("@style missing", "Bad.nazor")).Diagnostics, d => d.Id == "NZS001");
    }

    [Fact]
    public void Crlf_and_multiple_blocks_preserve_offsets()
    {
        const string source = "<TextBlock />\r\n@style { .x { --s: \"\\\"}\"; } }\r\n@style { .y { padding: 1px; } }\r\n";
        var result = StyleExtractor.Extract(source, "Styles.nazor");
        Assert.Equal(2, result.Styles.Count);
        Assert.Equal(source.Length, result.Razor.Length);
        for (var i = 0; i < source.Length; i++)
            if (source[i] is '\r' or '\n') Assert.Equal(source[i], result.Razor[i]);
    }

    [Fact]
    public void Styles_after_code_skip_comments_and_raw_verbatim_interpolated_strings()
    {
        var path = Build.Nazor("StyleContexts");
        var source = File.ReadAllText(path);
        var extracted = StyleExtractor.Extract(source, path);
        Assert.Equal(2, extracted.Styles.Count);
        Assert.Contains("@style missing-brace", extracted.Razor);
        Assert.Contains("@style value:", extracted.Razor);
        Assert.Equal(source.IndexOf("<TextBlock>", StringComparison.Ordinal), extracted.Razor.IndexOf("<TextBlock>", StringComparison.Ordinal));
        fixture.Compiler.Load([fixture.Compiler.Generate(path)]);
    }

    [Theory]
    [InlineData("<!--\n@style missing\n-->")]
    [InlineData("@@style { literal }")]
    [InlineData("<TextBlock Class=\"\n@style\" />")]
    [InlineData("<TextBlock>@(\"\"\"\n@style missing\n\"\"\")</TextBlock>")]
    [InlineData("@{ var s = @\"text\n@style missing\"; }")]
    [InlineData("@if (true) { var s = @\"text\n@style missing\"; }")]
    [InlineData("@stylesheet { literal }")]
    public void Literal_contexts_are_not_style_directives(string source)
    {
        var extracted = StyleExtractor.Extract(source, "Literal.nazor");
        Assert.Empty(extracted.Styles);
        Assert.Equal(source, extracted.Razor);
    }

    [Fact]
    public void Style_blocks_nested_in_markup_are_rejected()
    {
        var error = Build.Fails(() => StyleExtractor.Extract("<Container>\n@style { .x { padding: 1px; } }\n</Container>", "Nested.nazor"));
        Assert.StartsWith("Nested.nazor(2,1): error NZS003", error.Message);
    }
}
