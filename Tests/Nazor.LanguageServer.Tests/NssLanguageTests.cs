using Nazor.Compiler;
using Nazor.LanguageServer.Features;
using Nazor.LanguageServer.Protocol;
using Range = Nazor.LanguageServer.Protocol.Range;

namespace Nazor.LanguageServer.Tests;

public sealed class NssLanguageTests
{
    private static readonly NazorComponentInfo[] Components =
    [
        new("Button", "Nazor.Controls.Button", "Nazor.Controls", null, []),
        new("TextBlock", "Nazor.Controls.TextBlock", "Nazor.Controls", null, []),
        new("Clock", "App.Components.Clock", "App.Components", "Displays the time", []),
    ];

    private static (string Text, int Offset) Caret(string source) => (source.Replace("|", ""), source.IndexOf('|'));

    private static NssContext Classify(string source)
    {
        var (text, offset) = Caret(source);
        return NssLanguage.Classify(text, offset);
    }

    private static List<CompletionItem> Complete(string source)
    {
        var (text, offset) = Caret(source);
        return NssLanguage.Complete(text, offset, Components, text, o => o);
    }

    private static string? Hover(string source)
    {
        var (text, offset) = Caret(source);
        return NssLanguage.Hover(text, offset)?.Markdown;
    }

    [Theory]
    [InlineData("before")]
    [InlineData("after")]
    [InlineData("placeholder")]
    [InlineData("selection")]
    [InlineData("part")]
    public void Pseudo_element_completion_replaces_colons_and_hover_explains_the_target(string name)
    {
        var (text, offset) = Caret("Button::" + name[..2] + "|");
        var item = Assert.Single(Complete("Button::" + name[..2] + "|"), i => i.Label == "::" + name);
        var edit = Assert.IsType<TextEdit>(item.TextEdit);
        Assert.Equal("Button::" + name, text[..edit.Range.Start.Character] + edit.NewText + text[edit.Range.End.Character..]);
        Assert.NotNull(Hover("Button::" + name[..2] + "|" + name[2..]));
        Assert.NotNull(item.Documentation);
    }

    [Fact]
    public void Pseudo_element_property_completion_follows_the_runtime_applicability()
    {
        var selection = Complete("TextFieldInput::selection { |");
        Assert.Contains(selection, i => i.Label == "color");
        Assert.Contains(selection, i => i.Label == "background-color");
        Assert.DoesNotContain(selection, i => i.Label == "font-size" || i.Label == "width" || i.Label == "content");
        var placeholder = Complete("TextFieldInput::placeholder { |");
        Assert.Contains(placeholder, i => i.Label == "font-size");
        Assert.Contains(placeholder, i => i.Label == "opacity");
        Assert.DoesNotContain(placeholder, i => i.Label == "width" || i.Label == "scrollbar-width");
        Assert.Contains(Complete("Button::before { |"), i => i.Label == "content");
        Assert.DoesNotContain(Complete("Button { |"), i => i.Label == "content");
    }

    [Theory]
    [InlineData("enabled")]
    [InlineData("indeterminate")]
    [InlineData("read-only")]
    [InlineData("read-write")]
    [InlineData("placeholder-shown")]
    [InlineData("valid")]
    [InlineData("invalid")]
    [InlineData("required")]
    [InlineData("optional")]
    public void Control_state_completion_and_hover_explain_semantics(string name)
    {
        Assert.Contains(Complete("Button:|"), i => i.Label == name && i.Documentation is not null);
        Assert.NotNull(Hover("Button:" + name[..2] + "|" + name[2..]));
    }

    [Fact]
    public void Root_replaces_window_in_selector_completion()
    {
        var components = Components.Append(new NazorComponentInfo("Window", "Nazor.Window", "Nazor", null, [])).ToArray();
        var items = NssLanguage.Complete("", 0, components, "", o => o);
        Assert.Contains(items, i => i.Label == ":root");
        Assert.DoesNotContain(items, i => i.Label == "Window");
    }

    [Theory]
    [InlineData(".page {\n    dis|", NssContextKind.PropertyName)]
    [InlineData(".page {\n    |", NssContextKind.PropertyName)]
    [InlineData(".page { display: f|", NssContextKind.Value)]
    [InlineData(".page { color: var(--a|", NssContextKind.CustomPropertyReference)]
    [InlineData("Button:ho|", NssContextKind.PseudoClass)]
    [InlineData("::de|", NssContextKind.PseudoElement)]
    [InlineData("Clock ::|", NssContextKind.PseudoElement)]
    [InlineData(".a { ::de|", NssContextKind.PseudoElement)]
    [InlineData(".a { &:fo|", NssContextKind.PseudoClass)]
    [InlineData("Butt|", NssContextKind.Selector)]
    [InlineData(".a { color: red; Butt|", NssContextKind.Selector)]
    [InlineData("@me|", NssContextKind.AtRule)]
    [InlineData("@media (max-w|", NssContextKind.MediaFeature)]
    [InlineData("/* dis|", NssContextKind.None)]
    [InlineData(".a { font-family: \"Seg|", NssContextKind.None)]
    [InlineData(".a|", NssContextKind.ClassName)]
    [InlineData("#sa|", NssContextKind.Id)]
    public void Positions_are_classified(string source, object kind) => Assert.Equal((NssContextKind)kind, Classify(source).Kind);

    [Theory]
    [InlineData("|")]
    [InlineData(".a { |")]
    [InlineData(".a { Clo|")]
    [InlineData("Clock ::deep |")]
    public void Custom_component_names_are_offered_in_selector_positions(string source)
    {
        var item = Assert.Single(Complete(source), i => i.Label == "Clock");
        Assert.Equal("App.Components.Clock", item.Detail);
        Assert.Contains("Displays the time", item.Documentation!.Value);
    }

    [Theory]
    [InlineData("|")]
    [InlineData(":|")]
    [InlineData(":de|")]
    [InlineData("::|")]
    [InlineData("::de|")]
    [InlineData(".a { Clock ::de|ep")]
    public void Deep_completion_inserts_exactly_two_colons_and_replaces_the_whole_word(string source)
    {
        var (text, _) = Caret(source);
        var item = Assert.Single(Complete(source), i => i.Label == "::deep");
        var edit = item.TextEdit!;
        var start = TextPositions.Offset(text, edit.Range.Start);
        var end = TextPositions.Offset(text, edit.Range.End);
        var result = text[..start] + edit.NewText + text[end..];
        Assert.Equal(source.StartsWith(".a", StringComparison.Ordinal) ? ".a { Clock ::deep " : "::deep ", result);
        Assert.Contains("Clock TextBlock", item.Documentation!.Value);
        Assert.Contains("Crosses component", Hover("Clock ::de|ep"));
        Assert.DoesNotContain(Complete("Button:|"), i => i.Label == "Clock");
    }

    [Fact]
    public void Values_know_their_property()
    {
        Assert.Equal("display", Classify(".a { display: |").Property);
        Assert.Equal("flex-direction", Classify(".a { color: red; flex-direction: r|").Property);
    }

    [Fact]
    public void Property_names_replace_the_whole_hyphenated_word()
    {
        var item = Assert.Single(Complete(".a {\n    flex-d|\n}"), i => i.Label == "flex-direction");
        Assert.Equal(new Range(new(1, 4), new(1, 10)), item.TextEdit!.Range);
        Assert.Equal(CompletionItemKind.Property, item.Kind);
        Assert.Contains("row | row-reverse | column | column-reverse", item.Detail);
        Assert.Contains(Complete(".a { |"), i => i.Label == "padding" && i.Detail!.Contains("{1,4}"));
    }

    [Fact]
    public void Values_are_the_keywords_of_the_property()
    {
        var labels = Complete(".a { display: |").Select(i => i.Label).ToList();
        Assert.Equal(["block", "flex", "none"], labels.Where(l => l is "block" or "flex" or "none"));
        Assert.Contains("inherit", labels);
        Assert.Contains("var()", labels);
        Assert.DoesNotContain("red", labels);
        Assert.DoesNotContain("inherit", Complete(".a { padding: 4px |").Select(i => i.Label));
    }

    [Fact]
    public void Smooth_scroll_properties_values_and_hover_explain_pixel_and_line_distances()
    {
        foreach (var property in new[] { "scroll-behavior", "scroll-duration", "scroll-timing-function", "scroll-wheel-step" })
            Assert.Contains(Complete(".a { scroll-|"), i => i.Label == property);
        Assert.Contains(Complete(".a { scroll-behavior: |"), i => i.Label == "smooth");
        Assert.Contains(Complete(".a { scroll-wheel-step: |"), i => i.Label == "auto");
        Assert.Contains(Complete(".a { scroll-wheel-step: 3 |"), i => i.Label == "lines");
        Assert.Contains(Complete(".a { scroll-wheel-step: |"), i => i.Label == "calc()");
        Assert.Contains(Complete(".a { scroll-timing-function: |"), i => i.Label == "cubic-bezier()");
        Assert.Contains("180ms", Hover(".a { scroll-dur|ation: 250ms; }"));
        Assert.Contains("never impose snapping", Hover(".a { scroll-wheel-st|ep: 3 lines; }"));
        Assert.Contains("Reduced motion", Hover(".a { scroll-beha|vior: smooth; }"));
    }

    [Fact]
    public void Scrollbar_completion_and_hover_follow_the_registered_properties()
    {
        Assert.Contains(Complete(".a { scrollbar-vi|"), i => i.Label == "scrollbar-visibility");
        var values = Complete(".a { scrollbar-visibility: |").Select(i => i.Label).ToArray();
        Assert.Contains("none", values);
        Assert.Contains("visible", values);
        Assert.Contains("autohide", values);
        Assert.Contains(Complete(".a { scrollbar-thumb-color: |"), i => i.Label == "currentcolor");
        Assert.Contains("inherited", Hover(".a { scrollbar-wi|dth: 8px; }"));
        Assert.Contains(Complete(".a:wheel-|"), i => i.Label == "wheel-scrolling");
        Assert.Contains("scrollbar-wheel-timeout", Hover(".a:wheel-scrol|ling { }"));
        Assert.Contains("120ms", Hover(".a { scrollbar-fade-in-dur|ation: 0; }"));
        Assert.Contains(Complete(".a { scrollbar-fade-timing-function: |"), i => i.Label == "cubic-bezier()");
        Assert.Contains(Complete(".a { scrollbar-placement: |"), i => i.Label == "reserve");
        Assert.Contains("`none`", Hover(".a { scrollbar-butt|ons: visible; }"));
        Assert.Contains(Complete(".a { scrollbar-button-hover-back|"), i => i.Label == "scrollbar-button-hover-background");
    }

    [Fact]
    public void Colors_easing_functions_and_animatable_properties_follow_the_value_types()
    {
        var color = Complete(".a { color: |");
        Assert.Contains(color, i => i.Label == "rebeccapurple" && i.Kind == CompletionItemKind.Color);
        Assert.Contains(color, i => i.Label == "rgb()" && i.Kind == CompletionItemKind.Function);
        Assert.Contains(color, i => i.Label == "color-mix()" && i.Kind == CompletionItemKind.Function);
        Assert.Contains(color, i => i.Label == "ButtonText" && i.Kind == CompletionItemKind.Color);
        Assert.Contains(color, i => i.Label == "currentcolor");
        Assert.Contains(Complete(".a { border: 1px solid |"), i => i.Label == "red");
        var transition = Complete(".a { transition: opacity 1s e|").Select(i => i.Label).ToList();
        Assert.Contains("ease-out", transition);
        Assert.Contains("cubic-bezier()", transition);
        Assert.Contains("opacity", transition);
        Assert.DoesNotContain("display", transition);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void System_colors_explain_dynamic_aliases_without_fixed_swatches(bool visualStudio)
    {
        foreach (var source in new[] { ".a { background: | }", ".a { --surface: | }" })
        {
            var (text, offset) = Caret(source);
            var items = NssLanguage.Complete(text, offset, Components, text, o => o, visualStudio);
            foreach (var name in Nazor.Style.SystemColors.Names)
            {
                var item = Assert.Single(items, i => i.Label == name);
                Assert.Contains($"var({Nazor.Style.SystemColors.Variable(name)})", item.Documentation!.Value);
                Assert.Contains("Redefining", item.Documentation.Value);
                Assert.Null(item.Data);
                Assert.Null(item.VsIcon);
            }
        }
        Assert.Contains("dynamic system color", Hover(".a { background: Can|vas; }"));
        Assert.Contains("--nz-system-canvastext", Hover(".a { --text: canvas|text; }"));
        Assert.DoesNotContain("dynamic system color", Hover(".a { font-family: Can|vas; }") ?? "");
    }

    [Fact]
    public void System_palette_references_are_completed_once_and_document_overrides()
    {
        var items = Complete(".a { --nz-system-canvas: pink; --BrandExtra: red; color: var(--| }");
        Assert.Equal(items.Count, items.Select(i => i.Label).Distinct(StringComparer.Ordinal).Count());
        foreach (var name in Nazor.Style.SystemColors.Names.Concat(Nazor.Style.SystemColors.AccentShades)
            .Concat(Nazor.Style.SystemColors.Resolve().Palette.Keys))
            Assert.Contains(items, i => i.Label == Nazor.Style.SystemColors.Property(name));
        Assert.Contains(items, i => i.Label == "--BrandExtra");
        var canvas = Assert.Single(items, i => i.Label == "--nz-system-canvas");
        Assert.Contains("overriding this variable changes that keyword", canvas.Documentation!.Value);
        Assert.Contains("--nz-surface", canvas.Documentation.Value);
        Assert.Contains("inherits", Hover(".a { color: var(--nz-system-can|vas); }"));
        Assert.Contains("can be overridden", Hover(".a { --nz-system-can|vas: pink; }"));
        Assert.Contains("fallback", Hover(".a { color: var(--nz-system-tooltip-te|xt); }"));
        Assert.Contains("when available", Hover(".a { color: var(--nz-system-header-inactive-te|xt); }"));
    }

    [Theory]
    [InlineData("width", "calc()")]
    [InlineData("padding", "clamp()")]
    [InlineData("border", "min()")]
    [InlineData("background-image", "url()")]
    [InlineData("background-image", "linear-gradient()")]
    [InlineData("background-image", "radial-gradient()")]
    [InlineData("background-image", "conic-gradient()")]
    [InlineData("background-image", "repeating-linear-gradient()")]
    [InlineData("background-image", "repeating-radial-gradient()")]
    [InlineData("background-image", "repeating-conic-gradient()")]
    [InlineData("background", "max()")]
    [InlineData("transform", "translateX()")]
    [InlineData("transform", "matrix()")]
    [InlineData("clip-path", "inset()")]
    [InlineData("clip-path", "calc()")]
    [InlineData("filter", "blur()")]
    [InlineData("filter", "brightness()")]
    [InlineData("filter", "contrast()")]
    [InlineData("filter", "grayscale()")]
    [InlineData("filter", "hue-rotate()")]
    [InlineData("filter", "invert()")]
    [InlineData("filter", "opacity()")]
    [InlineData("filter", "saturate()")]
    [InlineData("filter", "sepia()")]
    [InlineData("backdrop-filter", "brightness()")]
    [InlineData("grid-template-columns", "repeat()")]
    [InlineData("grid-template-columns", "minmax()")]
    [InlineData("grid-auto-rows", "fit-content()")]
    public void Functions_follow_the_grammar_of_the_style_engine(string property, string function)
    {
        Assert.Contains(Complete($".a {{ {property}: |"), i => i.Label == function && i.Kind == CompletionItemKind.Function);
        Assert.DoesNotContain(Complete(".a { display: |"), i => i.Label == function);
    }

    [Theory]
    [InlineData("mix-blend-mode", "multiply")]
    [InlineData("mix-blend-mode", "color-dodge")]
    [InlineData("background-blend-mode", "screen")]
    [InlineData("isolation", "isolate")]
    public void Blend_keywords_follow_the_property_descriptor(string property,string value) =>
        Assert.Contains(Complete($".a {{ {property}: |"), item => item.Label == value && item.Kind == CompletionItemKind.Value);

    [Theory]
    [InlineData("font-synthesis", "weight")]
    [InlineData("font-synthesis", "style")]
    [InlineData("font-feature-settings", "off")]
    public void Typography_keywords_follow_the_property_descriptor(string property,string value) =>
        Assert.Contains(Complete($".a {{ {property}: |"), item => item.Label == value && item.Kind == CompletionItemKind.Value);

    [Fact]
    public void Rounded_inset_keyword_is_offered_for_clip_paths()
    {
        Assert.Contains(Complete(".a { clip-path: inset(0 |"), i => i.Label == "round" && i.Kind == CompletionItemKind.Value);
        Assert.DoesNotContain(Complete(".a { display: |"), i => i.Label == "round");
    }

    [Fact]
    public void Every_supported_property_and_shorthand_is_offered_in_both_contexts()
    {
        var expected = Nazor.Style.StyleProperties.All.Where(p => p.Name != "content").Select(p => p.Name).Concat(Nazor.Style.StyleProperties.AllShorthands.Select(p => p.Name)).Order().ToArray();
        Assert.Equal(expected, Complete(".a { |").Where(i => i.Kind == CompletionItemKind.Property).Select(i => i.Label).Order());
        var (text, offset) = Caret("@style { .a { | } }");
        var context = NazorLanguage.Classify(text, offset);
        Assert.Equal(expected, NssLanguage.Complete(text[context.Start..context.End], offset - context.Start, Components, text, o => o + context.Start)
            .Where(i => i.Kind == CompletionItemKind.Property).Select(i => i.Label).Order());
    }

    [Fact]
    public void Color_completions_supply_the_actual_swatch_and_preserve_the_inserted_name()
    {
        var color = Assert.Single(Complete(".a { color: |"), i => i.Label == "rebeccapurple");
        Assert.Equal("#663399", color.Data!["nssColor"]!.GetValue<string>());
        Assert.Equal("#663399", color.Documentation!.Value);
        Assert.Equal("rebeccapurple", color.TextEdit!.NewText);
        var transparent = Assert.Single(Complete(".a { color: |"), i => i.Label == "transparent");
        Assert.EndsWith("00", transparent.Data!["nssColor"]!.GetValue<string>(), StringComparison.Ordinal);
        Assert.DoesNotContain(Complete(".a { color: |"), i => i.Label == "currentcolor" && i.Data is not null);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Visual_studio_color_items_use_registered_images_without_a_hex_tooltip(bool registered)
    {
        const string text = ".a { color: bl; }";
        var directory = Directory.CreateTempSubdirectory("nazor-completion-images-").FullName;
        try
        {
            File.WriteAllText(Path.Combine(directory, "Colors.csproj"), "<Project Sdk=\"Microsoft.NET.Sdk\" />");
            NssCompletionImages.Write(directory);
            var manifest = System.Xml.Linq.XDocument.Load(Path.Combine(directory, "NazorCompletionColors.imagemanifest"));
            System.Xml.Linq.XNamespace schema = "http://schemas.microsoft.com/VisualStudio/ImageManifestSchema/2014";
            Assert.Equal(schema + "ImageManifest", manifest.Root!.Name);
            await using var client = LspClient.InProcess();
            await client.InitializeAsVisualStudioAsync();
            const string hostCatalog = "1f43bb02-30b4-4c52-aa6d-792d404fc472";
            if (registered) await client.NotifyAsync("nazor/completionImages", new System.Text.Json.Nodes.JsonObject
            {
                ["images"] = new System.Text.Json.Nodes.JsonObject
                {
                    ["255"] = new System.Text.Json.Nodes.JsonObject { ["guid"] = hostCatalog, ["id"] = 42 },
                },
            });
            foreach (var (path, source, line, character) in new[]
            {
                (Path.Combine(directory, "Colors.nss"), text, 0, 14),
                (Path.Combine(directory, "Colors.nazor"), "<Container />\n@style { " + text + " }", 1, 23),
            })
            {
                await client.OpenAsync(path, source);
                var items = (await client.AtAsync("textDocument/completion", path, line, character))!["items"]!.AsArray();
                var blue = Assert.Single(items, i => i!["label"]!.GetValue<string>() == "blue")!;
                Assert.Null(blue["detail"]);
                Assert.Null(blue["documentation"]);
                Assert.Equal(registered ? hostCatalog : NssCompletionImages.Catalog, blue["_vs_icon"]!["ImageId"]!["Guid"]!.GetValue<string>());
                var id = blue["_vs_icon"]!["ImageId"]!["Id"]!.GetValue<int>();
                Assert.Equal(registered ? 42 : 255, id);
                var image = Assert.Single(manifest.Descendants(schema + "Image"), i => (int)i.Attribute("ID")! == 255);
                Assert.Equal("false", (string?)image.Attribute("AllowColorInversion"));
                var asset = (string)image.Element(schema + "Source")!.Attribute("Uri")!;
                Assert.Equal("/Nazor.VisualStudio;Component/CompletionColors/255.xaml", asset);
                Assert.True(File.Exists(Path.Combine(directory, "CompletionColors", "255.xaml")));
                var resolved = await client.RequestAsync("completionItem/resolve", (System.Text.Json.Nodes.JsonObject)blue.DeepClone());
                Assert.True(System.Text.Json.Nodes.JsonNode.DeepEquals(blue["_vs_icon"], resolved!["_vs_icon"]));
                Assert.Null(resolved["detail"]);
                Assert.Null(resolved["documentation"]);
            }
        }
        finally { await LspClient.DeleteDirectoryAsync(directory); }
    }

    [Fact]
    public void Custom_properties_declared_in_the_text_are_offered()
    {
        const string source = ".a { --accent: red; --surface-2: white; color: var(--|";
        var items = Complete(source);
        Assert.Equal(["--accent", "--nz-window-controls-height", "--surface-2"], items.Where(i => !i.Label.StartsWith("--nz-system-", StringComparison.Ordinal)).Select(i => i.Label));
        var (text, offset) = Caret(source);
        Assert.Equal(TextPositions.Range(text, offset - 2, offset), items[0].TextEdit!.Range);
        Assert.Contains(Complete(".a { --accent: red; --|"), i => i.Label == "--accent");
    }

    [Fact]
    public void Selectors_offer_elements_and_pseudo_classes()
    {
        Assert.Equal(["Button", "TextBlock", "Clock", ":root", "::deep", "::before", "::after", "::placeholder", "::part", "::selection"], Complete("|").Select(i => i.Label));
        var pseudo = Complete("Button:|");
        Assert.Equal(["hover", "active", "focus", "focus-visible", "focus-within", "disabled", "checked"], pseudo.Take(7).Select(i => i.Label));
        Assert.Contains("pressed", pseudo[1].Documentation!.Value);
        Assert.Contains(pseudo, i => i.Label == "first-child");
        Assert.Contains(pseudo, i => i.Label == "nth-child" && i.Documentation!.Value.Contains("An+B", StringComparison.Ordinal));
        Assert.Contains(pseudo, i => i.Label == "not");
        Assert.Equal(["media", "keyframes"], Complete("@|").Select(i => i.Label));
        Assert.Contains(Complete("@media (|"), i => i.Label == "min-width");
        Assert.Contains(Complete("@media (|"), i => i.Label == "prefers-color-scheme");
        Assert.Contains(Complete("@media (|"), i => i.Label == "prefers-reduced-motion");
    }

    [Fact]
    public void Hover_describes_properties_values_and_pseudo_classes()
    {
        var display = Hover(".a { disp|lay: flex; }")!;
        Assert.Contains("**display**: `block | inline | flex | grid | none`", display);
        Assert.Contains("Initial value: `block`", display);
        Assert.Contains("not inherited", display);
        Assert.Contains("Initial value: `normal`", Hover(".a { justify-content: c|enter; }"));
        Assert.Contains("inherited · animatable", Hover(".a { col|or: red; }"));
        Assert.Contains("Shorthand for padding-top, padding-right, padding-bottom, padding-left.", Hover(".a { padd|ing: 4px; }"));
        Assert.Contains("#663399", Hover(".a { color: rebecca|purple; }"));
        Assert.Contains("keyboard focus", Hover("Button:fo|cus { }"));
        Assert.Null(Hover("Butt|on { }"));
        Assert.Null(Hover(".a { colr|: red; }"));
        Assert.Null(Hover("/* displ|ay */"));
    }
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Platform_properties_offer_typed_values_and_scope_hints_including_Visual_Studio(bool visualStudio)
    {
        List<CompletionItem> Values(string source)
        {
            var (text, offset) = Caret(source);
            return NssLanguage.Complete(text, offset, Components, text, o => o, visualStudio);
        }
        var names = Values(":root { -windows-|");
        Assert.DoesNotContain(Values("TitleBar { -windows-|"), i => i.Label.StartsWith("-windows-", StringComparison.Ordinal));
        var badgeNames = Nazor.Style.StyleProperties.All.Select(property => property.Name)
            .Concat(Nazor.Style.StyleProperties.AllShorthands.Select(property => property.Name))
            .Where(name => name.StartsWith("-windows-badge-", StringComparison.Ordinal));
        foreach (var name in badgeNames)
        {
            Assert.Contains(names, item => item.Label == name);
            Assert.Contains("root Window", NssLanguage.PropertyDocumentation(name));
        }
        foreach (var (property, value) in new[] { ("image-background-color", "auto"), ("image-fit", "cover"), ("image-clip", "border-box"), ("text-fit", "shrink"), ("vertical-align", "middle"),
            ("background-clip", "padding-box"), ("background-repeat", "repeat-x"), ("border-left-style", "dotted"), ("transform", "none"), ("mask-image", "none") })
            Assert.Contains(Values(":root { -windows-badge-" + property + ": |"), item => item.Label == value);
        Assert.Contains(Values(":root { -windows-badge-background-color: |"), item => item.Label == "red" && item.Kind == CompletionItemKind.Color && (!visualStudio || item.VsIcon is not null));
        Assert.Contains("16 × 16", Hover(":root { -windows-badge-si|ze: 14px; }"));
        Assert.Contains(names, i => i.Label == "-windows-title-bar-height" && i.Detail == "standard | tall");
        Assert.DoesNotContain(names, i => i.Label == "-windows-titlebar-height");
        Assert.DoesNotContain(names, i => i.Label.StartsWith("-windows-window-controls-", StringComparison.Ordinal));
        var nativeColors = Nazor.Style.StyleProperties.All.Where(p => p.Name.StartsWith("-windows-title-bar-", StringComparison.Ordinal) && p.Name.EndsWith("-color", StringComparison.Ordinal)).ToArray();
        Assert.Equal(12, nativeColors.Length);
        foreach (var property in nativeColors)
        {
            Assert.Contains(names, i => i.Label == property.Name && i.Detail == "auto | <color>");
            var values = Values(":root { " + property.Name + ": |");
            Assert.Contains(values, i => i.Label == "auto");
            Assert.Contains(values, i => i.Label == "currentcolor");
            Assert.Contains(values, i => i.Label == "red" && i.Kind == CompletionItemKind.Color && (!visualStudio || i.VsIcon is not null));
            Assert.Contains("Windows 11", NssLanguage.PropertyDocumentation(property.Name));
            Assert.Contains("root Window", NssLanguage.PropertyDocumentation(property.Name));
        }
        var height = Values(":root { -windows-title-bar-height: |");
        Assert.Contains(height, i => i.Label == "tall");
        Assert.Contains(height, i => i.Label == "standard");
        Assert.DoesNotContain(height, i => i.Kind == CompletionItemKind.Color || i.Label == "calc()");
        foreach (var property in new[] { "foreground-color", "background-color" })
        {
            var colors = Values(":root { -windows-title-bar-button-" + property + ": |");
            var red = Assert.Single(colors, i => i.Label == "red");
            Assert.Equal(CompletionItemKind.Color, red.Kind);
            if (visualStudio) Assert.NotNull(red.VsIcon);
            Assert.Contains(colors, i => i.Label == "rgb()");
            Assert.Contains(colors, i => i.Label == "currentcolor");
            Assert.DoesNotContain(colors, i => i.Label == "tall");
        }
        Assert.DoesNotContain(names, i => i.Label == "-windows-window-resize-grip");
        var frameNames = new[]
        {
            "-window-frame-border", "-window-frame-border-width", "-window-frame-border-style",
            "-window-frame-border-color", "-window-frame-border-radius", "-window-frame-box-shadow",
            "-window-frame-border-top-left-radius", "-window-frame-border-top-right-radius",
            "-window-frame-border-bottom-right-radius", "-window-frame-border-bottom-left-radius",
            "-window-frame-resize-inset", "-window-frame-resize-outset", "-window-frame-resize-corner-length",
            "-window-resize-grip", "-window-resize-grip-color",
        };
        var frameCompletions = Values(":root { -window-|");
        var titleBarCompletions = Values("TitleBar { -window-|");
        foreach (var name in frameNames)
        {
            Assert.Contains(frameCompletions, item => item.Label == name);
            Assert.DoesNotContain(titleBarCompletions, item => item.Label == name);
            Assert.Contains("root Window", NssLanguage.PropertyDocumentation(name));
        }
        Assert.Contains(Values(":root { -window-resize-grip: |"), i => i.Label == "auto");
        Assert.Contains(Values(":root { -window-resize-grip-color: |"), i => i.Label == "currentcolor");
        Assert.Contains(Values(":root { -window-frame-border-radius: |"), i => i.Label == "auto");
        Assert.Contains(Values(":root { -window-frame-resize-inset: |"), i => i.Label == "calc()");
        Assert.Contains(Values("TitleBar { min-height: var(--nz-window-|") , i => i.Label == "--nz-window-controls-height");
        Assert.Contains(Values("TitleBar { min-height: |"), i => i.Label == "calc()");
        Assert.Contains("root Window", Hover(":root { -windows-title-bar-button-foreground-co|lor: red; }"));
        var gripHint = Hover(":root { -window-resize-gr|ip: auto; }");
        Assert.Contains("root Window", gripHint);
        Assert.Contains("Initial value: `none`", gripHint);
    }

}
