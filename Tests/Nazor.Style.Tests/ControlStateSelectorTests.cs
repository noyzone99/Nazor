using Nazor.Nss;
using Nazor.Rendering;
using Nazor.Testing;

namespace Nazor.Style.Tests;

public sealed class SelectorControlFixture : ComponentBase
{
    public Dictionary<string, object?> Attributes { get; } = new() { ["role"] = "textbox", ["value"] = "", ["placeholder-text"] = "Type" };
    public bool Field { get; set; }
    public bool Invalid { get; set; }
    public bool Required { get; set; }
    public void Refresh() => StateHasChanged();

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "Group");
        builder.AddAttribute(1, "field", Field);
        builder.AddAttribute(2, "invalid", Invalid);
        builder.AddAttribute(3, "required", Required);
        builder.OpenElement(4, "Probe");
        foreach (var (name, value) in Attributes) builder.AddAttribute(5, name, value);
        builder.AddContent(6, Attributes.GetValueOrDefault("value") as string ?? "");
        builder.CloseElement();
        builder.CloseElement();
    }
}

[Collection("StyleRegistry")]
public sealed class ControlStateSelectorTests
{
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
    public void Control_states_compile_emit_and_round_trip_with_pseudo_class_specificity(string name)
    {
        var compilation = NssCompiler.Compile("states", $":{name} {{ opacity: .5; }}");
        Assert.True(compilation.Success, string.Join("\n", compilation.Diagnostics));
        Assert.Equal(new Specificity(0, 1, 0), Assert.Single(compilation.StyleSheet.Rules).Selector.Specificity);
        Assert.Contains("ControlStateSelector", CSharpEmitter.Emit(compilation.StyleSheet));
        Assert.Equal(compilation.StyleSheet.Describe(), StyleSheetJson.Deserialize(StyleSheetJson.Serialize(compilation.StyleSheet)).Describe());
        Assert.Contains(name, PseudoClasses.All);
    }

    [Fact]
    public void Unknown_serialized_control_states_are_rejected_at_the_JSON_boundary()
    {
        var sheet = NssCompiler.Compile("states", ":enabled { opacity: .5; }").StyleSheet;
        Assert.Throws<InvalidDataException>(() => StyleSheetJson.Deserialize(StyleSheetJson.Serialize(sheet).Replace("\"enabled\"", "\"unknown\"", StringComparison.Ordinal)));
    }

    [Theory]
    [InlineData("button", "enabled", true)]
    [InlineData("text", "enabled", false)]
    [InlineData("textbox", "read-write", true)]
    [InlineData("text", "read-only", true)]
    [InlineData("text", "read-write", false)]
    [InlineData("textbox", "placeholder-shown", true)]
    [InlineData("text", "placeholder-shown", false)]
    [InlineData("textbox", "optional", true)]
    [InlineData("button", "optional", false)]
    [InlineData("textbox", "valid", true)]
    [InlineData("button", "valid", false)]
    public void Roles_define_applicability_instead_of_matching_every_enabled_element(string role, string name, bool expected)
    {
        using var host = new TestHost();
        var root = host.Render<SelectorControlFixture>();
        var fixture = (SelectorControlFixture)root.Component;
        fixture.Attributes["role"] = role;
        fixture.Refresh();
        host.RunPending();
        Assert.Equal(expected, new ControlStateSelector(name).Matches(root.Single("Probe")));
    }

    [Fact]
    public void Effective_disabled_state_and_readonly_changes_refresh_descendant_selectors()
    {
        using var host = new TestHost();
        using var engine = new StyleEngine(host.Renderer);
        var root = host.Render<SelectorControlFixture>();
        var fixture = (SelectorControlFixture)root.Component;
        var probe = root.Single("Probe");
        engine.Application = NssCompiler.Compile("states", "Probe:enabled:read-write { opacity: .5; }").StyleSheet;
        Assert.Equal(.5, engine.GetStyle(probe).Opacity);
        root.Single("Group").SetState(ElementStates.Disabled, true);
        Assert.Equal(1, engine.GetStyle(probe).Opacity);
        Assert.True(new ControlStateSelector("read-only").Matches(probe));
        root.Single("Group").SetState(ElementStates.Disabled, false);
        Assert.Equal(.5, engine.GetStyle(probe).Opacity);
        fixture.Attributes["readonly"] = true;
        fixture.Refresh();
        host.RunPending();
        Assert.Equal(1, engine.GetStyle(probe).Opacity);
    }

    [Fact]
    public void Validation_reuses_application_and_field_flags_including_readonly_and_disabled_controls()
    {
        using var host = new TestHost();
        using var engine = new StyleEngine(host.Renderer);
        var root = host.Render<SelectorControlFixture>();
        var fixture = (SelectorControlFixture)root.Component;
        var probe = root.Single("Probe");
        engine.Application = NssCompiler.Compile("states", "Probe:invalid:required { opacity: .5; }").StyleSheet;
        Assert.Equal(1, engine.GetStyle(probe).Opacity);
        fixture.Field = fixture.Required = fixture.Invalid = true;
        fixture.Attributes["readonly"] = fixture.Attributes["disabled"] = true;
        fixture.Refresh();
        host.RunPending();
        Assert.Equal(.5, engine.GetStyle(probe).Opacity);
        Assert.False(new ControlStateSelector("optional").Matches(probe));
        Assert.False(new ControlStateSelector("valid").Matches(probe));
        fixture.Invalid = false;
        fixture.Refresh();
        host.RunPending();
        Assert.Equal(1, engine.GetStyle(probe).Opacity);
        Assert.True(new ControlStateSelector("valid").Matches(probe));
    }

    [Fact]
    public void Indeterminate_and_placeholder_states_require_their_effective_metadata()
    {
        using var host = new TestHost();
        var root = host.Render<SelectorControlFixture>();
        var fixture = (SelectorControlFixture)root.Component;
        var probe = root.Single("Probe");
        probe.SetState(ElementStates.Composing, true);
        Assert.False(new ControlStateSelector("placeholder-shown").Matches(probe));
        probe.SetState(ElementStates.Composing, false);
        Assert.True(new ControlStateSelector("placeholder-shown").Matches(probe));
        fixture.Attributes["value"] = "value";
        fixture.Attributes["role"] = "checkbox";
        fixture.Attributes["indeterminate"] = true;
        fixture.Refresh();
        host.RunPending();
        Assert.True(new ControlStateSelector("indeterminate").Matches(probe));
        Assert.False(new ControlStateSelector("placeholder-shown").Matches(probe));
        fixture.Attributes["role"] = "text";
        fixture.Refresh();
        host.RunPending();
        Assert.False(new ControlStateSelector("indeterminate").Matches(probe));
        Assert.Throws<ArgumentException>(() => new ControlStateSelector("unknown"));
    }
}
