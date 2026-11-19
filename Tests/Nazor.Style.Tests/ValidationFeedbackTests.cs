using Nazor.Nss;
using Nazor.Input;
using Nazor.Rendering;
using Nazor.Testing;

namespace Nazor.Style.Tests;

public sealed class ValidationFeedbackTests
{
    [Theory]
    [InlineData("oninput")]
    [InlineData("onchange")]
    [InlineData("onblur")]
    public async Task Feedback_starts_after_interaction_survives_edits_and_clears_on_reset(string interaction)
    {
        using var host = new TestHost();
        using var engine = new StyleEngine(host.Renderer);
        var root = host.Render<SelectorControlFixture>();
        var fixture = (SelectorControlFixture)root.Component;
        fixture.Field = fixture.Invalid = true; fixture.Refresh(); host.RunPending();
        var probe = root.Single("Probe");
        engine.Application = NssCompiler.Compile("validation", "Probe:user-invalid { opacity: .5; } Group:has(Probe:user-invalid) { opacity: .6; }").StyleSheet;
        Assert.Equal(1, engine.GetStyle(probe).Opacity);
        await host.Renderer.DispatchEventAsync(probe, interaction, new ChangeEventArgs());
        Assert.Equal(.5, engine.GetStyle(probe).Opacity);
        Assert.Equal(.6, engine.GetStyle(root.Single("Group")).Opacity);
        fixture.Invalid = false; fixture.Refresh(); host.RunPending();
        Assert.Equal(1, engine.GetStyle(probe).Opacity);
        Assert.True(new ControlStateSelector("user-valid").Matches(probe));
        ValidationFeedback.Reset(root.Single("Group"));
        Assert.False(new ControlStateSelector("user-valid").Matches(probe));
        fixture.Invalid = true; fixture.Refresh(); host.RunPending();
        Assert.Equal(1, engine.GetStyle(probe).Opacity);
    }

    [Fact]
    public void Submission_is_inherited_only_from_the_nearest_form_and_reset_preserves_nested_forms()
    {
        using var host = new TestHost();
        using var engine = new StyleEngine(host.Renderer);
        var root = host.Render<ValidationForms>();
        engine.Application = NssCompiler.Compile("validation", "Input:user-invalid { opacity: .5; }").StyleSheet;
        var forms = root.Elements("Form").ToArray();
        var inputs = root.Elements("Input").ToArray();
        Assert.All(inputs, e => Assert.Equal(1, engine.GetStyle(e).Opacity));
        ValidationFeedback.Submit(forms[0]);
        Assert.Equal(.5, engine.GetStyle(inputs[0]).Opacity);
        Assert.Equal(1, engine.GetStyle(inputs[1]).Opacity);
        ValidationFeedback.Submit(forms[1]);
        ValidationFeedback.Reset(forms[0]);
        Assert.Equal(1, engine.GetStyle(inputs[0]).Opacity);
        Assert.Equal(.5, engine.GetStyle(inputs[1]).Opacity);
    }

    [Fact]
    public async Task Disabled_input_does_not_enable_feedback_and_parameter_edits_alone_do_not_count()
    {
        using var host = new TestHost();
        var root = host.Render<SelectorControlFixture>();
        var fixture = (SelectorControlFixture)root.Component;
        fixture.Attributes["disabled"] = true; fixture.Attributes["value"] = "changed"; fixture.Refresh(); host.RunPending();
        var probe = root.Single("Probe");
        await host.Renderer.DispatchEventAsync(probe, "oninput", new ChangeEventArgs());
        Assert.False(ValidationFeedback.IsEligible(probe));
        ValidationFeedback.MarkInteracted(probe);
        Assert.False(ValidationFeedback.IsEligible(probe));
        Assert.Throws<ArgumentException>(() => ValidationFeedback.Submit(probe));
    }
}

public sealed class ValidationForms : ComponentBase
{
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "Form"); builder.AddAttribute(1, "role", "form");
        Input(builder);
        builder.OpenElement(5, "Form"); builder.AddAttribute(6, "role", "form"); Input(builder); builder.CloseElement();
        builder.CloseElement();
    }
    private static void Input(RenderTreeBuilder builder)
    {
        builder.OpenElement(2, "Input"); builder.AddAttribute(3, "role", "textbox"); builder.AddAttribute(4, "invalid", true); builder.CloseElement();
    }
}
