using Nazor.Nss;

namespace Nazor.Style.Tests;

public sealed class StyleSheetJsonTests
{
    private const string Everything = """
        .card, Button#main.primary.large:hover:active:focus:focus-visible:disabled:checked {
            padding: 8px 4% 2px 1px;
            margin: auto 2px;
            width: 50%;
            max-width: none;
            flex-basis: content;
            flex: 1 2 10px;
            line-height: 1.5;
            color: currentColor;
            background: #01020380;
            opacity: 0.5;
            font-weight: bold;
            font-family: "Segoe UI", Fira Sans;
            font-size: 12px;
            transition: background-color 200ms ease-in-out 50ms, opacity 1s cubic-bezier(0.1, 0.2, 0.3, 0.4);
            --surface: #ffffff;
            --space: 4px 8px;
            border: var(--space) solid var(--surface, #000);
            padding: var(--space);
            display: flex;
            text-align: inherit;
            cursor: unset;
            visibility: initial;
            z-index: -3;
            grid-template-columns: 10% repeat(auto-fit, minmax(20px, 1fr)) 30px;
            grid-auto-rows: min-content 2fr fit-content(30px);
            grid-template-areas: "a a" "b .";
            grid-area: a / 2 / span 3;
            aspect-ratio: 3 / 2;
        }
        .card > Button { line-height: normal; }
        .card Button { line-height: 20px; }
        @media (min-width: 400px) and (max-width: 720px), (min-height: 100px) and (max-height: 500px) {
            .card { padding: 1px; }
        }
        @media (prefers-color-scheme: dark) and (prefers-reduced-motion: no-preference) {
            .card { width: calc(100% - 2em); max-height: min(50vh, 400px); }
            .card { transform: translate(10%, 2em) scale(1.5, 2) rotate(45deg) skew(1deg, 2deg) matrix(1, 0, 0, 1, 2, 3); transform-origin: left 10px; }
            .card { background: url(images/a.png) center / contain no-repeat, linear-gradient(red, blue) 0 0 / 10px auto repeat-y; object-fit: scale-down; object-position: 10% 20%; }
        }
        """;

    private static readonly string RepositoryRoot = Nazor.Testing.RepositoryPaths.Root;

    private static StyleSheet Compile(string name, string text, string path)
    {
        var result = NssCompiler.Compile(name, text, path);
        Assert.True(result.Success, string.Join("\n", result.Diagnostics));
        return result.StyleSheet;
    }

    private static void AssertRoundTrip(StyleSheet sheet)
    {
        var json = StyleSheetJson.Serialize(sheet);
        var restored = StyleSheetJson.Deserialize(json);
        Assert.Equal(sheet.Source, restored.Source);
        Assert.Equal(sheet.Describe(), restored.Describe());
        Assert.Equal(sheet.Rules.Count, restored.Rules.Count);
        for (var i = 0; i < sheet.Rules.Count; i++)
        {
            Assert.Equal(sheet.Rules[i].Order, restored.Rules[i].Order);
            Assert.Equal(sheet.Rules[i].Media?.ToString(), restored.Rules[i].Media?.ToString());
            Assert.Equal(sheet.Rules[i].Selector.Specificity, restored.Rules[i].Selector.Specificity);
        }
        Assert.Equal(json, StyleSheetJson.Serialize(restored));
    }

    [Fact]
    public void Every_value_kind_round_trips()
    {
        var sheet = Compile("Components/Card.nazor", Everything, "Card.nazor");
        AssertRoundTrip(sheet);

        var json = StyleSheetJson.Serialize(sheet);
        foreach (var kind in new[] { "value", "keyword", "pending", "custom" }) Assert.Contains($"\"kind\":\"{kind}\"", json);
        foreach (var type in new[] { "length", "size", "color", "lineHeight", "number", "integer", "enum", "strings", "transitions", "zIndex", "trackList", "trackSizes", "gridAreas", "gridLine", "aspectRatio" })
            Assert.Contains($"\"type\":\"{type}\"", json);
        foreach (var keyword in new[] { "Inherit", "Initial", "Unset" }) Assert.Contains($"\"keyword\":\"{keyword}\"", json);
        Assert.Contains("\"currentColor\":true", json);
        Assert.Contains("\"combinators\":[\"Child\"]", json);
        Assert.Contains("\"combinators\":[\"Descendant\"]", json);
        Assert.Contains("\"states\":[\"Hover\",\"Pressed\",\"Focus\",\"FocusVisible\",\"Disabled\",\"Checked\"]", json);
        Assert.Contains("\"minHeight\":100", json);
        Assert.Contains("\"prefersColorScheme\":\"Dark\"", json);
        Assert.Contains("\"prefersReducedMotion\":false", json);
        Assert.Contains("\"calc\":", json);
        Assert.Contains("\"type\":\"transforms\"", json);
        Assert.Contains("\"type\":\"transformOrigin\"", json);
    }

    [Fact]
    public void Json_nodes_built_in_memory_read_back_as_well()
    {
        var sheet = Compile("inline.nss", Everything, "inline.nss");
        var restored = StyleSheetJson.FromJson(StyleSheetJson.ToJson(sheet));
        Assert.Equal(sheet.Describe(), restored.Describe());
    }

    [Fact]
    public void Every_stylesheet_of_the_repository_round_trips()
    {
        var files = new[] { "Tests/Nazor.Compiler.Tests/Fixtures", "Samples" }
            .SelectMany(directory => Directory.GetFiles(Path.Combine(RepositoryRoot, directory), "*.nss", SearchOption.AllDirectories))
            .Concat(Directory.GetFiles(Path.Combine(RepositoryRoot, "Sources/Core/Nazor.Controls/Themes"), "*.nss"))
            .Where(path => !path.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar).Any(part => part is "bin" or "obj"))
            .ToList();
        Assert.NotEmpty(files);
        Assert.All(files, path => AssertRoundTrip(Compile(Path.GetRelativePath(RepositoryRoot, path).Replace('\\', '/'), File.ReadAllText(path), path)));
    }

    [Fact]
    public void Malformed_input_names_the_offending_node()
    {
        var missing = Assert.Throws<InvalidDataException>(() => StyleSheetJson.Deserialize("""{"source":"x","rules":[{"order":0}]}"""));
        Assert.Contains("$.rules[0]", missing.Message);
        Assert.Contains("selector", missing.Message);

        var unknown = Assert.Throws<InvalidDataException>(() => StyleSheetJson.Deserialize(
            """{"source":"x","rules":[{"order":0,"selector":{"compounds":[{"classes":[],"states":[]}],"combinators":[]},"declarations":[{"kind":"value","property":"colour","value":{"type":"number","value":1}}]}]}"""));
        Assert.Contains("'colour'", unknown.Message);

        var wrongType = Assert.Throws<InvalidDataException>(() => StyleSheetJson.Deserialize(
            """{"source":"x","rules":[{"order":0,"selector":{"compounds":[{"classes":[],"states":[]}],"combinators":[]},"declarations":[{"kind":"value","property":"color","value":{"type":"number","value":1}}]}]}"""));
        Assert.Contains("'color'", wrongType.Message);

        Assert.Throws<InvalidDataException>(() => StyleSheetJson.Deserialize("not json"));
        Assert.Throws<InvalidDataException>(() => StyleSheetJson.Deserialize("[]"));
    }
}
