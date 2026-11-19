using Nazor.Nss;
using Nazor.Testing;

namespace Nazor.Style.Tests;

[Collection("StyleRegistry")]
public sealed class KeyframesTests
{
    private static StyleSheet Compile(string nss)
    {
        var result = NssCompiler.Compile("t", nss, "t.nss");
        Assert.True(result.Success, string.Join("\n", result.Diagnostics));
        return result.StyleSheet;
    }

    [Fact]
    public void Keyframes_compile_with_offsets_easing_and_animatable_longhands()
    {
        var sheet = Compile("""
            @keyframes fade {
                from { opacity: 0; }
                50%, 75% { opacity: 0.5; animation-timing-function: ease-in; }
                TO { opacity: 1; border: 2px solid red; }
            }
            @keyframes "two words" { to { width: 10px; } }
            """);
        Assert.Equal("""
            @keyframes fade { 0% { opacity: 0 } 50% { opacity: 0.5; animation-timing-function: ease-in } 75% { opacity: 0.5; animation-timing-function: ease-in } 100% { opacity: 1; border-top-width: 2px; border-right-width: 2px; border-bottom-width: 2px; border-left-width: 2px; border-top-color: #ff0000; border-right-color: #ff0000; border-bottom-color: #ff0000; border-left-color: #ff0000 } }
            @keyframes two words { 100% { width: 10px } }

            """.ReplaceLineEndings("\n"), sheet.Describe());
        Assert.Same(sheet.Keyframes[0], sheet.FindKeyframes("fade"));
        Assert.Null(sheet.FindKeyframes("Fade"));
    }

    [Theory]
    [InlineData("animation: none", "animation: none")]
    [InlineData("animation: spin 1s linear infinite", "animation: spin 1000ms linear 0ms infinite normal none running")]
    [InlineData("animation: 1s ease-out 2s spin 3 reverse both paused", "animation: spin 1000ms ease-out 2000ms 3 reverse both paused")]
    [InlineData("animation: ease 1s ease", "animation: ease 1000ms ease 0ms 1 normal none running")]
    [InlineData("animation: spin 1s -500ms alternate-reverse", "animation: spin 1000ms ease -500ms 1 alternate-reverse none running")]
    [InlineData("animation: a 1s, b 200ms cubic-bezier(0.1, 0.2, 0.3, 0.4) 0.5", "animation: a 1000ms ease 0ms 1 normal none running, b 200ms cubic-bezier(0.1, 0.2, 0.3, 0.4) 0ms 0.5 normal none running")]
    [InlineData("animation: 1s none forwards", "animation: forwards 1000ms ease 0ms 1 normal none running")]
    public void Animations_compile(string declaration, string expected)
    {
        var sheet = Compile($".x {{ {declaration}; }}");
        Assert.Equal(expected, string.Join("; ", sheet.Rules.Single().Declarations));
    }

    [Theory]
    [InlineData("@keyframes { to { opacity: 1; } }", "NZS108", "Expected the name")]
    [InlineData("@keyframes none { to { opacity: 1; } }", "NZS108", "Expected the name")]
    [InlineData("@keyframes x { 120% { opacity: 1; } }", "NZS108", "between 0% and 100%")]
    [InlineData("@keyframes x { middle { opacity: 1; } }", "NZS108", "from, to or a percentage")]
    [InlineData("@keyframes x { from { display: none; } }", "NZS108", "'display' cannot be animated")]
    [InlineData("@keyframes x { from { --a: 1; } }", "NZS108", "Custom properties")]
    [InlineData("@keyframes x { from { .a { opacity: 1; } } }", "NZS108", "only declarations")]
    [InlineData("@keyframes x { from { animation-timing-function: nope; } }", "NZS102", "animation-timing-function")]
    [InlineData(".y { @keyframes x { to { opacity: 1; } } }", "NZS104", "top level")]
    [InlineData("@media (min-width: 1px) { @keyframes x { to { opacity: 1; } } }", "NZS104", "top level")]
    [InlineData(".x { animation: spin 1s 2s 3s; }", "NZS102", "at most a duration and a delay")]
    [InlineData(".x { animation: spin 1s, ; }", "NZS102", "Expected an animation")]
    [InlineData(".x { animation: a b; }", "NZS102", "Unexpected 'b'")]
    [InlineData(".x { animation: a 2 3; }", "NZS102", "Duplicate iteration count")]
    public void Invalid_keyframes_and_animations_are_reported(string nss, string id, string message)
    {
        var diagnostics = NssCompiler.Compile("t", nss, "t.nss").Diagnostics;
        Assert.Contains(diagnostics, d => d.Id == id && d.Message.Contains(message, StringComparison.Ordinal));
    }

    [Fact]
    public void Keyframes_and_animations_round_trip_through_json()
    {
        var sheet = Compile("""
            @keyframes pulse { from { opacity: 0.2; transform: scale(0.5); } 40% { opacity: var(--peak, 1); animation-timing-function: ease-out; } }
            .x { animation: pulse 1s infinite alternate both, other 2s -1s 0.5 paused; }
            """);
        var json = StyleSheetJson.Serialize(sheet);
        Assert.Contains("\"keyframes\":", json);
        Assert.Contains("\"type\":\"animations\"", json);
        var restored = StyleSheetJson.Deserialize(json);
        Assert.Equal(sheet.Describe(), restored.Describe());
        Assert.Equal(json, StyleSheetJson.Serialize(restored));
    }

    [Fact]
    public void Replacing_a_sheet_replaces_its_keyframes_and_replacing_rules_keeps_them()
    {
        var sheet = Compile("@keyframes a { to { opacity: 0; } } .x { opacity: 1; }");
        Assert.True(sheet.Replace(Compile(".x { opacity: 0.5; }").Rules));
        Assert.NotNull(sheet.FindKeyframes("a"));
        Assert.True(sheet.Replace(Compile("@keyframes b { to { opacity: 0; } } .x { opacity: 1; }")));
        Assert.Null(sheet.FindKeyframes("a"));
        Assert.NotNull(sheet.FindKeyframes("b"));
        Assert.False(sheet.Replace(Compile("@keyframes b { to { opacity: 0; } } .x { opacity: 1; }")));
    }

    [Fact]
    public void Keyframes_are_resolved_for_the_element()
    {
        using var host = new TestHost();
        using var engine = new StyleEngine(host.Renderer) { Viewport = new Viewport(1000, 500) };
        var root = host.Render<Nested>();
        engine.Application = Compile("""
            @keyframes grow {
                from { width: 2em; color: currentColor; padding-left: inherit; }
                to { width: 10vw; margin-left: var(--gap, 3px); box-shadow: 0 1em 0 currentColor; }
            }
            Top { font-size: 10px; color: red; padding-left: 4px; }
            Middle { font-size: 20px; --gap: 1em; }
            """);
        var frames = engine.ResolveKeyframes(root.Single("Middle"), "grow")!;
        Assert.Equal([0, 1], frames.Select(f => f.Offset));
        Assert.Equal(Length.Px(4), frames[0].Values[StyleProperties.PaddingLeft]);
        Assert.Equal(SizeValue.Fixed(Length.Px(40)), frames[0].Values[StyleProperties.Width]);
        Assert.Equal(Color.FromRgb(255, 0, 0), frames[0].Values[StyleProperties.Color]);
        Assert.Equal(SizeValue.Fixed(Length.Px(100)), frames[1].Values[StyleProperties.Width]);
        Assert.Equal(SizeValue.Fixed(Length.Px(20)), frames[1].Values[StyleProperties.MarginLeft]);
        Assert.Equal("0px 20px 0px 0px #ff0000", frames[1].Values[StyleProperties.BoxShadow].ToString());
        Assert.Null(engine.ResolveKeyframes(root.Single("Middle"), "missing"));
    }
}
