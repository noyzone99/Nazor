using Nazor.Nss;
using Nazor.Style;

namespace Nazor.Style.Tests;

public sealed class NssCompilerTests
{
    private const string CounterStyles = """
        .counter {
            display: flex;
            flex-direction: column;
            gap: 8px;
            padding: 16px;

            Button {
                background: #303030;
                transition: background 200ms ease-out;

                &:hover { background: #0078d4; }
            }
        }
        """;

    private static StyleSheet Compile(string text)
    {
        var result = NssCompiler.Compile("test", text, "test.nss");
        Assert.True(result.Success, string.Join("\n", result.Diagnostics));
        return result.StyleSheet;
    }

    private static IReadOnlyList<NssDiagnostic> Errors(string text) => NssCompiler.Compile("test", text, "test.nss").Diagnostics;

    [Fact]
    public void Counter_styles_expand_nesting_into_ordered_rules()
    {
        Assert.Equal("""
            .counter (0,1,0) { display: flex; flex-direction: column; row-gap: 8px; column-gap: 8px; padding-top: 16px; padding-right: 16px; padding-bottom: 16px; padding-left: 16px }
            .counter Button (0,1,1) { background-color: #303030; background-image: none; background-position: 0% 0%; background-size: auto auto; background-repeat: Repeat; transition: background-color 200ms ease-out 0ms }
            .counter Button:hover (0,2,1) { background-color: #0078d4; background-image: none; background-position: 0% 0%; background-size: auto auto; background-repeat: Repeat }

            """.ReplaceLineEndings("\n"), Compile(CounterStyles).Describe());
    }

    [Fact]
    public void Inline_and_sidecar_sources_compile_to_the_same_rules()
    {
        var inline = NssCompiler.Compile("Counter.nazor", [new NssSource("Counter.nazor", CounterStyles, 6, 9)]);
        var sidecar = NssCompiler.Compile("Counter.nazor", [new NssSource("Counter.nazor.nss", CounterStyles)]);
        Assert.Equal(sidecar.StyleSheet.Describe(), inline.StyleSheet.Describe());
    }

    [Fact]
    public void Sources_are_concatenated_in_order()
    {
        var result = NssCompiler.Compile("x", [new NssSource("a.nss", ".a { padding: 1px; }"), new NssSource("b.nss", ".b { padding: 2px; }")]);
        Assert.Equal([".a", ".b"], result.StyleSheet.Rules.Select(r => r.Selector.ToString()));
        Assert.Equal([0, 1], result.StyleSheet.Rules.Select(r => r.Order));
    }

    [Fact]
    public void Selector_lists_expand_with_individual_specificity()
    {
        var sheet = Compile(".a, #b { Label, .c { color: red; } }");
        Assert.Equal([".a Label (0,1,1)", "#b Label (1,0,1)", ".a .c (0,2,0)", "#b .c (1,1,0)"],
            sheet.Rules.Select(r => $"{r.Selector} ({r.Selector.Specificity})"));
    }

    [Fact]
    public void Ampersand_can_appear_after_other_compounds_and_child_combinators_are_kept()
    {
        var sheet = Compile(".card { .dark & { color: white; } > Button.primary { color: blue; } &.selected:focus { color: red; } }");
        Assert.Equal([".dark .card", ".card > Button.primary", ".card.selected:focus"], sheet.Rules.Select(r => r.Selector.ToString()));
    }

    [Fact]
    public void Media_queries_apply_to_nested_declarations_and_combine()
    {
        var sheet = Compile("""
            .workspace {
                flex-direction: row;
                @media (max-width: 720px) {
                    flex-direction: column;
                    @media (min-width: 400px) { gap: 4px; }
                }
            }
            @media (min-width: 1000px), (max-height: 300px) { .wide { padding: 0; } }
            """);
        Assert.Equal("""
            .workspace (0,1,0) { flex-direction: row }
            @media (max-width: 720px) | .workspace (0,1,0) { flex-direction: column }
            @media (min-width: 400px) and (max-width: 720px) | .workspace (0,1,0) { row-gap: 4px; column-gap: 4px }
            @media (min-width: 1000px), (max-height: 300px) | .wide (0,1,0) { padding-top: 0px; padding-right: 0px; padding-bottom: 0px; padding-left: 0px }

            """.ReplaceLineEndings("\n"), sheet.Describe());
        Assert.True(sheet.Rules[1].Media!.Matches(new Viewport(700, 500)));
        Assert.False(sheet.Rules[1].Media!.Matches(new Viewport(800, 500)));
    }

    [Fact]
    public void Media_queries_test_the_preferences_and_lengths_in_em()
    {
        var sheet = Compile("""
            @media (prefers-color-scheme: dark) and (max-width: 50em) { .a { color: white; } }
            @media (prefers-reduced-motion) { .b { transition: none; } }
            @media (PREFERS-REDUCED-MOTION: no-preference) { .c { opacity: 1; } }
            @media (prefers-color-scheme: light) {
                .d { color: black; }
                @media (prefers-color-scheme: dark) { .e { color: red; } }
                @media (min-width: calc(10rem + 20px)) { .f { color: blue; } }
            }
            """);
        Assert.Equal("""
            @media (max-width: 700px) and (prefers-color-scheme: dark) | .a (0,1,0) { color: #ffffff }
            @media (prefers-reduced-motion: reduce) | .b (0,1,0) { transition: none }
            @media (prefers-reduced-motion: no-preference) | .c (0,1,0) { opacity: 1 }
            @media (prefers-color-scheme: light) | .d (0,1,0) { color: #000000 }
            @media (min-width: 1px) and (max-width: 0px) | .e (0,1,0) { color: #ff0000 }
            @media (min-width: 160px) and (prefers-color-scheme: light) | .f (0,1,0) { color: #0000ff }

            """.ReplaceLineEndings("\n"), sheet.Describe());
        var dark = new MediaPreferences(ColorScheme.Dark, ReducedMotion: false);
        Assert.True(sheet.Rules[0].Media!.Matches(new Viewport(700, 500), dark));
        Assert.False(sheet.Rules[0].Media!.Matches(new Viewport(700, 500)));
        Assert.False(sheet.Rules[1].Media!.Matches(new Viewport(700, 500), dark));
        Assert.True(sheet.Rules[1].Media!.Matches(new Viewport(700, 500), dark with { ReducedMotion = true }));
        Assert.True(sheet.Rules[2].Media!.Matches(new Viewport(700, 500)));
        Assert.False(sheet.Rules[4].Media!.Matches(new Viewport(700, 500), dark));
        Assert.False(sheet.Rules[4].Media!.Matches(new Viewport(700, 500)));
    }

    [Theory]
    [InlineData("#abc", "#aabbcc")]
    [InlineData("#11223380", "#11223380")]
    [InlineData("#1238", "#11223388")]
    [InlineData("rgb(255, 0, 0)", "#ff0000")]
    [InlineData("rgba(0, 0, 255, 0.5)", "#0000ff80")]
    [InlineData("rgb(0 128 0 / 50%)", "#00800080")]
    [InlineData("hsl(120, 100%, 25%)", "#008000")]
    [InlineData("rebeccapurple", "#663399")]
    [InlineData("transparent", "#00000000")]
    [InlineData("currentColor", "currentColor")]
    [InlineData("color-mix(in srgb, red, blue)", "#800080")]
    [InlineData("color-mix(in srgb, #005fb8 90%, #ffffff)", "#1a6fbf")]
    [InlineData("color-mix(in srgb, 25% red, blue)", "#4000bf")]
    [InlineData("color-mix(in srgb, red, blue 75%)", "#4000bf")]
    [InlineData("color-mix(in srgb, red 30%, blue 30%)", "#80008099")]
    [InlineData("color-mix(in srgb, red 60%, blue 60%)", "#800080")]
    [InlineData("color-mix(in srgb, transparent, #ff0000)", "#ff000080")]
    [InlineData("COLOR-MIX(in srgb, color-mix(in srgb, white, black), black)", "#404040")]
    public void Colors_follow_css_syntax(string value, string expected)
    {
        var declaration = (ValueDeclaration)Compile($".x {{ color: {value}; }}").Rules[0].Declarations[0];
        Assert.Equal(expected, declaration.Value.ToString());
    }

    [Theory]
    [InlineData("padding: 1px", "padding-top: 1px; padding-right: 1px; padding-bottom: 1px; padding-left: 1px")]
    [InlineData("padding: 1px 2px", "padding-top: 1px; padding-right: 2px; padding-bottom: 1px; padding-left: 2px")]
    [InlineData("padding: 1px 2px 3px", "padding-top: 1px; padding-right: 2px; padding-bottom: 3px; padding-left: 2px")]
    [InlineData("margin: 0 auto -4px 10%", "margin-top: 0px; margin-right: auto; margin-bottom: -4px; margin-left: 10%")]
    [InlineData("gap: 4px 8px", "row-gap: 4px; column-gap: 8px")]
    [InlineData("flex: 1", "flex-grow: 1; flex-shrink: 1; flex-basis: 0%")]
    [InlineData("flex: 2 0 100px", "flex-grow: 2; flex-shrink: 0; flex-basis: 100px")]
    [InlineData("flex: none", "flex-grow: 0; flex-shrink: 0; flex-basis: auto")]
    [InlineData("border: 1px solid #ccc", "border-top-width: 1px; border-right-width: 1px; border-bottom-width: 1px; border-left-width: 1px; border-top-style: solid; border-right-style: solid; border-bottom-style: solid; border-left-style: solid; border-top-color: #cccccc; border-right-color: #cccccc; border-bottom-color: #cccccc; border-left-color: #cccccc")]
    [InlineData("font-family: \"Segoe UI\", Noto Sans, sans-serif", "font-family: Segoe UI, Noto Sans, sans-serif")]
    [InlineData("font-weight: bold", "font-weight: 700")]
    [InlineData("line-height: 1.5", "line-height: 1.5")]
    [InlineData("width: 50%", "width: 50%")]
    [InlineData("max-width: none", "max-width: none")]
    [InlineData("opacity: 0.25", "opacity: 0.25")]
    [InlineData("transition: opacity 0.3s linear 50ms, background-color 1s", "transition: opacity 300ms linear 50ms, background-color 1000ms ease 0ms")]
    [InlineData("color: inherit", "color: inherit")]
    [InlineData("padding: initial", "padding-top: initial; padding-right: initial; padding-bottom: initial; padding-left: initial")]
    public void Values_and_shorthands_compile_to_longhands(string declaration, string expected)
    {
        var rule = Compile($".x {{ {declaration}; }}").Rules.Single();
        Assert.Equal(expected, string.Join("; ", rule.Declarations));
    }

    [Fact]
    public void Custom_properties_and_var_references_are_kept_as_tokens()
    {
        var rule = Compile(".x { --surface: #202020; background: var(--surface, white); padding: var(--space) 4px; }").Rules.Single();
        Assert.Equal("--surface: #202020; background: var(--surface, white); padding: var(--space) 4px", string.Join("; ", rule.Declarations));
        Assert.IsType<CustomPropertyDeclaration>(rule.Declarations[0]);
        Assert.IsType<PendingDeclaration>(rule.Declarations[1]);
    }

    [Fact]
    public void Diagnostics_point_to_the_original_position()
    {
        var result = NssCompiler.Compile("Counter.nazor", [new NssSource("Counter.nazor", ".a {\n    colr: red;\n}", StartLine: 7, StartColumn: 9)]);
        var error = Assert.Single(result.Diagnostics);
        Assert.Equal("Counter.nazor(8,5): error NZS101: Unknown property 'colr'. Did you mean 'color'?", error.ToString());
    }

    [Theory]
    [InlineData(".x { padding: 1pt; }", "NZS102", "Unit 'pt' is not supported")]
    [InlineData(".x { display: inline-block; }", "NZS102", "Expected one of")]
    [InlineData(".x { width: 10; }", "NZS102", "needs a unit")]
    [InlineData(".x { color: red !important; }", "NZS105", "!important")]
    [InlineData(".x:matches(.y) { color: red; }", "NZS107", ":first-child")]
    [InlineData(".x || .y { color: red; }", "NZS103", "Unexpected")]
    [InlineData(".x[title { color: red; }", "NZS103", "Missing ']'")]
    [InlineData(".x::first-line { color: red; }", "NZS103", "Supported pseudo-elements")]
    [InlineData("@import \"a.nss\";", "NZS104", "@import")]
    [InlineData("@media screen { .x { color: red; } }", "NZS104", "Media type 'screen'")]
    [InlineData("@media (orientation: landscape) { .x { color: red; } }", "NZS104", "orientation")]
    [InlineData("@media (prefers-color-scheme: blue) { .x { color: red; } }", "NZS104", "Expected light or dark")]
    [InlineData("@media (prefers-color-scheme) { .x { color: red; } }", "NZS104", "Expected ':'")]
    [InlineData("@media (prefers-reduced-motion: none) { .x { color: red; } }", "NZS104", "Expected reduce or no-preference")]
    [InlineData("@media (max-width: 50vw) { .x { color: red; } }", "NZS104", "Viewport units are not allowed")]
    [InlineData("@media (max-width: 50%) { .x { color: red; } }", "NZS104", "Expected a length")]
    [InlineData("&.x { color: red; }", "NZS106", "nested rule")]
    [InlineData(".x { color: red;", "NZS100", "Missing '}'")]
    [InlineData(".x { background: var(surface); }", "NZS102", "custom property name")]
    [InlineData(".x { color: color-mix(srgb, red, blue); }", "NZS102", "Expected 'in srgb'")]
    [InlineData(".x { color: color-mix(in hsl, red, blue); }", "NZS102", "srgb or oklab")]
    [InlineData(".x { color: color-mix(in srgb, currentcolor, blue); }", "NZS102", "currentcolor")]
    [InlineData(".x { color: color-mix(in srgb, red 0%, blue 0%); }", "NZS102", "both be zero")]
    [InlineData(".x { color: color-mix(in srgb, red 120%, blue); }", "NZS102", "between 0% and 100%")]
    [InlineData(".x { color: color-mix(in srgb, 10% red 20%, blue); }", "NZS102", "Expected ','")]
    public void Unsupported_or_invalid_input_reports_clear_errors(string text, string id, string message)
    {
        var diagnostics = Errors(text);
        Assert.Contains(diagnostics, d => d.Id == id && d.Message.Contains(message, StringComparison.Ordinal));
    }

    [Fact]
    public void Errors_do_not_stop_the_rest_of_the_stylesheet()
    {
        var result = NssCompiler.Compile("x", ".a { colr: red; padding: 1px; }\n.b:nope { color: red; }\n.c { color: blue; }", "x.nss");
        Assert.Equal(2, result.Diagnostics.Count);
        Assert.Equal([".a", ".c"], result.StyleSheet.Rules.Select(r => r.Selector.ToString()));
    }

    [Fact]
    public void Emitted_csharp_rebuilds_the_typed_model()
    {
        var code = CSharpEmitter.Emit(Compile(CounterStyles));
        Assert.Contains("new global::Nazor.Style.ValueDeclaration(global::Nazor.Style.StyleProperties.FlexDirection, global::Nazor.Style.FlexDirection.Column)", code);
        Assert.Contains("global::Nazor.Rendering.ElementStates.Hover", code);
        Assert.DoesNotContain("flex-direction: column", code);
    }

    [Fact]
    public void Native_Windows_caption_properties_keep_their_types_when_emitted()
    {
        var sheet = Compile(":root { -windows-title-bar-height: tall; -windows-title-bar-button-foreground-color: currentColor; -windows-title-bar-button-background-color: transparent; }");
        var code = CSharpEmitter.Emit(sheet);
        Assert.Contains("global::Nazor.WindowsTitleBarHeight.Tall", code);
        Assert.Contains("StyleProperties.WindowsTitleBarButtonForegroundColor", code);
        Assert.Contains("StyleProperties.WindowsTitleBarButtonBackgroundColor", code);
        Assert.NotEmpty(Errors(":root { -windows-title-bar-height: 36px; }"));
    }
}
