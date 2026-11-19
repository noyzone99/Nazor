namespace Nazor.Tooling.Tests;

public sealed class NssGrammarTests
{
    [Fact]
    public void Motion_policy_geometry_and_transition_names_are_highlighted()
    {
        const string source = """
            NumberPresenter {
                number-motion: exponential;
                number-motion-duration: 180ms;
                number-motion-decay: .25;
                number-motion-tolerance: .01;
                number-boundary-tolerance: .02;
                transition: number-motion-duration 200ms;
            }
            NumberPresenter:active { number-motion: linear-increase; }
            Icon { ring-shape: border-box; }
            MenuItem { submenu-open-delay: 60ms; }
            """;
        var tokens = Tokenize(source);
        foreach (var name in new[] { "number-motion", "number-motion-duration", "number-motion-decay", "number-motion-tolerance", "number-boundary-tolerance", "ring-shape", "submenu-open-delay" })
            tokens.Scoped(source, name + ":", "support.type.property-name.nss");
        tokens.Scoped(source, "number-motion-duration 200ms", "support.constant.property-value.property-name.nss");
        tokens.Scoped(source, "exponential", "support.constant.property-value.nss");
        tokens.Scoped(source, "linear-increase", "support.constant.property-value.nss");
        Assert.DoesNotContain(tokens, t => t.Has("invalid"));
    }

    [Theory]
    [InlineData('"')]
    [InlineData('\'')]
    public void String_escapes_keep_escaped_quotes_inside_the_string(char quote)
    {
        var source = "Item { --label: " + quote + @"\E004\1f600 \" + quote + "tail" + quote + "; color: red; }";
        var tokens = Tokenize(source);
        tokens.Scoped(source, @"\E004", "constant.character.escape.nss");
        tokens.Scoped(source, @"\1f600 ", "constant.character.escape.nss");
        tokens.Scoped(source, "\\" + quote, "constant.character.escape.nss");
        tokens.Scoped(source, "tail", quote == '"' ? "string.quoted.double.nss" : "string.quoted.single.nss");
        tokens.Scoped(source, "color", "support.type.property-name.nss");
        tokens.Scoped(source, "red", "support.constant.color.nss");
        Assert.DoesNotContain(tokens, t => t.Has("invalid"));
    }

    [Theory]
    [InlineData('"')]
    [InlineData('\'')]
    public void Quoted_attribute_values_and_keyframe_names_keep_escaped_quotes_inside_their_region(char quote)
    {
        var quoted = quote + @"a\" + quote + "b" + quote;
        var source = "Item[title=" + quoted + "] { color: red; } @keyframes " + quoted + " { from { opacity: 0; } }";
        var tokens = Tokenize(source);
        tokens.Scoped(source, "b", "string.quoted.double.nss");
        tokens.Scoped(source, "color", "support.type.property-name.nss");
        tokens.Scoped(source, quoted, "entity.name.function.keyframes.nss", occurrence: 2);
        tokens.Scoped(source, "opacity", "support.type.property-name.nss");
        Assert.DoesNotContain(tokens, t => t.Has("invalid"));
    }

    [Fact]
    public void Root_is_a_pseudo_class_and_the_removed_window_type_is_invalid()
    {
        const string source = "Window { color: red; } :root:hover { color: blue; } .Window, #Window, [name=Window] { color: green; }";
        var tokens = Tokenize(source);
        tokens.Scoped(source, "Window", "invalid.illegal.window-selector.nss");
        tokens.Scoped(source, ":root", "entity.other.attribute-name.pseudo-class.nss");
        tokens.Scoped(source, ".Window", "entity.other.attribute-name.class.nss");
        tokens.Scoped(source, "#Window", "entity.other.attribute-name.id.nss");
    }

    [Fact]
    public void Control_state_pseudo_classes_have_shared_grammar_classifications()
    {
        foreach (var name in new[] { "enabled", "indeterminate", "read-only", "read-write", "placeholder-shown", "valid", "invalid", "required", "optional" })
        {
            var source = $"Probe:{name} {{ opacity: .5; }}";
            Tokenize(source).Scoped(source, ":" + name, "entity.other.attribute-name.pseudo-class.nss");
        }
    }

    [Fact]
    public void Filtered_sibling_positions_keep_filter_selectors_and_the_of_keyword_colored()
    {
        const string source = "Item:nth-child(even of .row:not([hidden])) { opacity: .5; }";
        var tokens = Tokenize(source);
        tokens.Scoped(source, "of", "support.constant.property-value.nss");
        tokens.Scoped(source, ".row", "entity.other.attribute-name.class.nss");
        Assert.DoesNotContain(tokens, t => t.Has("invalid"));
    }

    [Fact]
    public void All_badge_properties_and_shorthands_are_recognized_by_the_shared_editor_grammar()
    {
        var names = Nazor.Style.StyleProperties.All.Select(property => property.Name)
            .Concat(Nazor.Style.StyleProperties.AllShorthands.Select(property => property.Name))
            .Where(name => name.StartsWith("-windows-badge-", StringComparison.Ordinal));
        foreach (var name in names)
        {
            var source = $":root {{ {name}: initial; }}";
            Tokenize(source).Scoped(source, name, "support.type.property-name.nss");
        }
        const string values = ":root { -windows-badge-vertical-align: middle; -windows-badge-text-fit: shrink; -windows-badge-image-clip: border-box; }";
        var tokens = Tokenize(values);
        foreach (var value in new[] { "middle", "shrink", "border-box" }) tokens.Scoped(values, value, "support.constant.property-value.nss");
    }
    [Fact]
    public void Native_title_bar_colors_are_highlighted()
    {
        var properties = Nazor.Style.StyleProperties.All.Where(p => p.Name.StartsWith("-windows-title-bar-", StringComparison.Ordinal) && p.Name.EndsWith("-color", StringComparison.Ordinal)).ToArray();
        Assert.Equal(12, properties.Length);
        var source = ":root { " + string.Join(" ", properties.Select(p => p.Name + ": auto;")) + " }";
        var tokens = Tokenize(source);
        foreach (var property in properties) tokens.Scoped(source, property.Name, "support.type.property-name.nss");
    }

    [Fact]
    public void Smooth_scroll_properties_and_line_distances_are_highlighted()
    {
        const string source = "Container { scroll-behavior: smooth; scroll-duration: 180ms; scroll-timing-function: ease-out; scroll-wheel-step: 1.5 lines; } .compact { scroll-wheel-step: 2lines; }";
        var tokens = Tokenize(source);
        Assert.DoesNotContain(tokens, t => t.Has("invalid"));
        foreach (var name in new[] { "scroll-behavior", "scroll-duration", "scroll-timing-function", "scroll-wheel-step" })
            tokens.Scoped(source, name, "support.type.property-name.nss");
        tokens.Scoped(source, "smooth", "support.constant.property-value.nss");
        tokens.Scoped(source, "lines", "support.constant.property-value.nss");
        tokens.Scoped(source, "180", "constant.numeric.nss");
        tokens.Scoped(source, "2", "constant.numeric.nss");
    }

    [Fact]
    public void Deep_and_platform_queries_are_valid_framework_syntax()
    {
        const string source = "@media (platform: windows) { .outer ::deep Button { padding: 4px; } }";
        var tokens = Tokenize(source);
        Assert.DoesNotContain(tokens, t => t.Has("invalid"));
        tokens.Scoped(source, "platform", "support.type.property-name.media.nss");
        tokens.Scoped(source, "windows", "support.constant.property-value.nss");
        tokens.Scoped(source, "::deep", "keyword.operator.combinator.nss");
    }

    private static List<Token> Tokenize(string source)
    {
        var (tokens, depth) = GrammarHarness.Instance.Tokenize("source.nss", source);
        Assert.Equal(1, depth);
        return tokens;
    }


    [Fact]
    public void Native_property_names_keep_their_leading_hyphen_and_typed_values()
    {
        const string source = ":root:window-active { -windows-title-bar-height: tall; -windows-title-bar-button-foreground-color: red; -windows-title-bar-button-background-color: transparent; } :root { -window-resize-grip: auto; -windows-window-resize-grip: auto; }";
        var tokens = Tokenize(source);
        foreach (var property in new[] { "-windows-title-bar-height", "-windows-title-bar-button-foreground-color", "-windows-title-bar-button-background-color", "-window-resize-grip" })
            tokens.Scoped(source, property, "support.type.property-name.nss");
        tokens.Scoped(source, ":window-active", "entity.other.attribute-name.pseudo-class.nss");
        tokens.Scoped(source, "-windows-window-resize-grip", "meta.property-name.unknown.nss");
        tokens.Scoped(source, "tall", "support.constant.property-value.nss");
        tokens.Scoped(source, "red", "support.constant.color.nss");
    }

    [Fact]
    public void Selectors_are_split_into_types_classes_ids_pseudo_classes_and_combinators()
    {
        const string source = ".page > Button:hover, #main TextBox:focus-visible { }";
        var tokens = Tokenize(source);
        tokens.Scoped(source, "page", "entity.other.attribute-name.class.nss");
        tokens.Scoped(source, ">", "keyword.operator.combinator.nss");
        tokens.Scoped(source, "Button", "entity.name.tag.nss");
        tokens.Scoped(source, ":hover", "entity.other.attribute-name.pseudo-class.nss");
        tokens.Scoped(source, ",", "punctuation.separator.list.comma.nss");
        tokens.Scoped(source, "main", "entity.other.attribute-name.id.nss");
        tokens.Scoped(source, ":focus-visible", "entity.other.attribute-name.pseudo-class.nss");
    }

    [Fact]
    public void Declarations_have_property_names_and_typed_values()
    {
        const string source = """
            Button {
                color: #ff0000;
                padding: 6px 16px;
                opacity: .5;
                transition: background 200ms ease-out, opacity 1s linear 50ms;
                --accent: #005fb8;
                background: var(--accent, white);
                font-weight: bold;
            }
            """;
        var tokens = Tokenize(source);
        tokens.Scoped(source, "color", "support.type.property-name.nss");
        tokens.Scoped(source, "#ff0000", "constant.other.color.rgb-value.hex.nss");
        tokens.Scoped(source, "6", "constant.numeric.nss");
        tokens.Scoped(source, "px", "keyword.other.unit.nss");
        tokens.Scoped(source, ".5", "constant.numeric.nss");
        tokens.Scoped(source, "background", "support.constant.property-value.property-name.nss");
        tokens.NotScoped(source, "background", "support.type.property-name.nss");
        tokens.Scoped(source, "background", "support.type.property-name.nss", occurrence: 2);
        tokens.Scoped(source, "ms", "keyword.other.unit.nss");
        tokens.Scoped(source, "ease-out", "support.constant.property-value.nss");
        tokens.Scoped(source, "--accent", "variable.other.custom-property.nss");
        tokens.Scoped(source, "var", "support.function.misc.nss");
        tokens.Scoped(source, "--accent", "variable.other.custom-property.nss", occurrence: 2);
        tokens.Scoped(source, "white", "support.constant.color.nss");
        tokens.Scoped(source, "bold", "support.constant.property-value.nss");
        tokens.Scoped(source, ";", "punctuation.terminator.rule.nss");
    }

    [Fact]
    public void Selectors_have_sibling_combinators_attributes_and_pseudo_class_functions()
    {
        const string source = """
            .a + .b ~ Item[data-state^="to" i]:nth-child(2n+1):not(.c, :first-child):focus-within > [disabled] {
                color: red;
            }
            """;
        var tokens = Tokenize(source);
        tokens.Scoped(source, "+", "keyword.operator.combinator.nss");
        tokens.Scoped(source, "~", "keyword.operator.combinator.nss");
        tokens.Scoped(source, "data-state", "entity.other.attribute-name.nss");
        tokens.Scoped(source, "^=", "keyword.operator.pattern.nss");
        tokens.Scoped(source, "\"to\"", "string.quoted.double.nss");
        tokens.Scoped(source, "nth-child", "entity.other.attribute-name.pseudo-class.nss");
        tokens.Scoped(source, "2n+1", "constant.numeric.nss");
        tokens.Scoped(source, "not", "entity.other.attribute-name.pseudo-class.nss");
        tokens.Scoped(source, "first-child", "entity.other.attribute-name.pseudo-class.nss");
        tokens.Scoped(source, "focus-within", "entity.other.attribute-name.pseudo-class.nss");
        tokens.Scoped(source, "disabled", "entity.other.attribute-name.nss");
        tokens.NotScoped(source, "[", "invalid.illegal.attribute-selector.nss");
    }

    [Fact]
    public void Math_functions_have_operators_nested_groups_and_relative_units()
    {
        const string source = """
            .page {
                width: calc((100% - 2 * 1em) / 3);
                padding: clamp(4px, 1vw, 2rem) min(1vmin, 3vh) max(1vmax, 10px);
                margin-left: calc(-1em + var(--gap, 4px));
            }
            """;
        var tokens = Tokenize(source);
        tokens.Scoped(source, "calc", "support.function.misc.nss");
        tokens.Scoped(source, "-", "keyword.operator.arithmetic.nss");
        tokens.Scoped(source, "*", "keyword.operator.arithmetic.nss");
        tokens.Scoped(source, "/", "keyword.operator.arithmetic.nss");
        tokens.Scoped(source, "em", "keyword.other.unit.nss");
        tokens.Scoped(source, "clamp", "support.function.misc.nss");
        tokens.Scoped(source, "vw", "keyword.other.unit.nss");
        tokens.Scoped(source, "rem", "keyword.other.unit.nss");
        tokens.Scoped(source, "min", "support.function.misc.nss");
        tokens.Scoped(source, "vmin", "keyword.other.unit.nss");
        tokens.Scoped(source, "vh", "keyword.other.unit.nss");
        tokens.Scoped(source, "max", "support.function.misc.nss");
        tokens.Scoped(source, "vmax", "keyword.other.unit.nss");
        tokens.Scoped(source, "-1", "constant.numeric.nss");
        tokens.Scoped(source, "+", "keyword.operator.arithmetic.nss");
        tokens.Scoped(source, "--gap", "variable.other.custom-property.nss");
    }

    [Fact]
    public void Transforms_have_functions_angles_and_origins()
    {
        const string source = """
            .x {
                transform: translateX(10%) scale(1.5) rotate(0.25turn) skewY(5deg) matrix(1, 0, 0, 1, 0, 0);
                transform-origin: left top;
            }
            """;
        var tokens = Tokenize(source);
        tokens.Scoped(source, "transform", "support.type.property-name.nss");
        tokens.Scoped(source, "translateX", "support.function.misc.nss");
        tokens.Scoped(source, "scale", "support.function.misc.nss");
        tokens.Scoped(source, "rotate", "support.function.misc.nss");
        tokens.Scoped(source, "turn", "keyword.other.unit.nss");
        tokens.Scoped(source, "skewY", "support.function.misc.nss");
        tokens.Scoped(source, "matrix", "support.function.misc.nss");
        tokens.Scoped(source, "transform-origin", "support.type.property-name.nss");
        tokens.Scoped(source, "left", "support.constant.property-value.nss");
    }

    [Fact]
    public void Keyframes_have_a_name_offsets_and_declarations()
    {
        const string source = """
            @keyframes fade-in {
                from { opacity: 0; }
                50%, 75% { opacity: 0.5; animation-timing-function: ease-in; }
                to { opacity: 1; transform: scale(1); }
            }
            .x { animation: fade-in 200ms ease-out 2 alternate both; }
            """;
        var tokens = Tokenize(source);
        tokens.Scoped(source, "@keyframes", "keyword.control.at-rule.keyframes.nss");
        tokens.Scoped(source, "fade-in", "entity.name.function.keyframes.nss");
        tokens.Scoped(source, "from", "entity.other.keyframe-offset.nss");
        tokens.Scoped(source, "50%", "entity.other.keyframe-offset.nss");
        tokens.Scoped(source, "75%", "entity.other.keyframe-offset.nss");
        tokens.Scoped(source, "opacity", "support.type.property-name.nss");
        tokens.Scoped(source, "to", "entity.other.keyframe-offset.nss");
        tokens.Scoped(source, "animation", "support.type.property-name.nss", occurrence: 2);
        tokens.Scoped(source, "alternate", "support.constant.property-value.nss");
        tokens.Scoped(source, "both", "support.constant.property-value.nss");
        Assert.DoesNotContain(tokens, t => t.Has("invalid"));
    }

    [Fact]
    public void Grid_declarations_have_track_functions_fr_units_and_area_strings()
    {
        const string source = """
            .page {
                display: grid;
                grid-template-columns: 200px repeat(auto-fill, minmax(100px, 1fr)) fit-content(40%);
                grid-template-areas: "head head" "side main";
                grid-column: 1 / span 2;
                place-items: center start;
            }
            """;
        var tokens = Tokenize(source);
        tokens.Scoped(source, "grid", "support.constant.property-value.nss");
        tokens.Scoped(source, "grid-template-columns", "support.type.property-name.nss");
        tokens.Scoped(source, "repeat", "support.function.misc.nss");
        tokens.Scoped(source, "auto-fill", "support.constant.property-value.nss");
        tokens.Scoped(source, "minmax", "support.function.misc.nss");
        tokens.Scoped(source, "fr", "keyword.other.unit.nss");
        tokens.Scoped(source, "fit-content", "support.function.misc.nss");
        tokens.Scoped(source, "\"head head\"", "string.quoted.double.nss");
        tokens.Scoped(source, "span", "support.constant.property-value.nss");
        tokens.Scoped(source, "place-items", "support.type.property-name.nss");
    }

    [Fact]
    public void Nested_rules_are_selectors_not_declarations()
    {
        const string source = """
            .list {
                padding: 8px;
                Button:hover { color: red; }
                &.selected { border-color: blue; }
                & > TextBlock {
                    color: gray;
                }
            }
            """;
        var tokens = Tokenize(source);
        tokens.Scoped(source, "Button", "entity.name.tag.nss");
        tokens.NotScoped(source, "Button", "support.type.property-name.nss");
        tokens.Scoped(source, ":hover", "entity.other.attribute-name.pseudo-class.nss");
        tokens.Scoped(source, "red", "support.constant.color.nss");
        tokens.Scoped(source, "&", "entity.name.tag.reference.nss");
        tokens.Scoped(source, "selected", "entity.other.attribute-name.class.nss");
        tokens.Scoped(source, "border-color", "support.type.property-name.nss");
        tokens.Scoped(source, "TextBlock", "entity.name.tag.nss");
        tokens.Scoped(source, "gray", "support.constant.color.nss");
    }

    [Fact]
    public void Media_queries_highlight_the_supported_features()
    {
        const string source = """
            @media (max-width: 720px) and (min-height: 300px) {
                .workspace { flex-direction: column; }
            }
            """;
        var tokens = Tokenize(source);
        tokens.Scoped(source, "@media", "keyword.control.at-rule.media.nss");
        tokens.Scoped(source, "max-width", "support.type.property-name.media.nss");
        tokens.Scoped(source, "720", "constant.numeric.nss");
        tokens.Scoped(source, "and", "keyword.operator.logical.nss");
        tokens.Scoped(source, "workspace", "entity.other.attribute-name.class.nss");
        tokens.Scoped(source, "column", "support.constant.property-value.nss");
    }

    [Fact]
    public void Media_queries_highlight_the_preferences()
    {
        const string source = """
            @media (prefers-color-scheme: dark) and (prefers-reduced-motion: no-preference), (prefers-reduced-motion) {
                .x { color: red; }
            }
            """;
        var tokens = Tokenize(source);
        tokens.Scoped(source, "prefers-color-scheme", "support.type.property-name.media.nss");
        tokens.Scoped(source, "dark", "support.constant.property-value.nss");
        tokens.Scoped(source, "prefers-reduced-motion", "support.type.property-name.media.nss");
        tokens.Scoped(source, "no-preference", "support.constant.property-value.nss");
        tokens.Scoped(source, "prefers-reduced-motion", "support.type.property-name.media.nss", occurrence: 2);
        Assert.DoesNotContain(tokens, t => t.Has("invalid"));
    }

    [Fact]
    public void Constructs_that_NSS_rejects_are_marked_invalid()
    {
        const string source = """
            a::first-line { color: red !important; }
            [title="x" z] { }
            @font-face { }
            @media (orientation: portrait) { }
            Button:matches(.x) { }
            """;
        var tokens = Tokenize(source);
        tokens.Scoped(source, "::first-line", "invalid.illegal.pseudo.nss");
        tokens.Scoped(source, "!important", "invalid.illegal.important.nss");
        tokens.Scoped(source, "[title=\"x\" z]", "invalid.illegal.attribute-selector.nss");
        tokens.Scoped(source, "@font-face", "invalid.illegal.at-rule.nss");
        tokens.Scoped(source, "orientation", "invalid.illegal.media-feature.nss");
        tokens.Scoped(source, ":matches(.x)", "invalid.illegal.pseudo.nss");
    }

    [Fact]
    public void Pseudo_elements_and_generated_content_are_recognized_in_standalone_and_inline_NSS()
    {
        const string source = "Label::before, Label::after, Editor::placeholder, Editor::selection { content: \"\"; color: red; }";
        var tokens = Tokenize(source);
        foreach (var name in new[] { "::before", "::after", "::placeholder", "::selection" })
            tokens.Scoped(source, name, "entity.other.attribute-name.pseudo-element.nss");
        tokens.Scoped(source, "content", "support.type.property-name.nss");
        Assert.DoesNotContain(tokens, t => t.Has("invalid"));
    }

    [Fact]
    public void Unknown_properties_are_not_highlighted_as_properties()
    {
        const string source = "Button { colr: red; }";
        var tokens = Tokenize(source);
        tokens.NotScoped(source, "colr", "support.type.property-name.nss");
        tokens.Scoped(source, "colr", "meta.property-name.unknown.nss");
    }

    [Fact]
    public void Comments_can_be_anywhere()
    {
        const string source = """
            /* theme */
            :root { /* surface */ --surface: #fff; /* end */ }
            """;
        var tokens = Tokenize(source);
        tokens.Scoped(source, "theme", "comment.block.nss");
        tokens.Scoped(source, "surface", "comment.block.nss");
        tokens.Scoped(source, "end", "comment.block.nss");
        tokens.Scoped(source, "--surface", "variable.other.custom-property.nss");
    }
}
