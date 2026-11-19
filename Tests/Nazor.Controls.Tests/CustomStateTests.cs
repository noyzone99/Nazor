using Nazor.Rendering;
using Nazor.Testing;
using Nazor.Style;
using Nazor.Nss;

namespace Nazor.Controls.Tests;

public sealed class CustomStateTests
{
    [Fact]
    public void Control_publishes_the_typed_snapshot_on_its_root()
    {
        using var host = new TestHost();
        var states = CustomStateSet.Empty.With("loading");
        var root = host.Render<Button>(ParameterView.FromDictionary(new Dictionary<string, object?> { ["CustomStates"] = states }));
        Assert.Same(states, root.Single("Button").Attributes["custom-states"]);
    }

    [Fact]
    public async Task Form_callbacks_publish_and_reset_feedback_before_application_code()
    {
        using var host = new TestHost();
        var root = host.Render<Form>();
        var form = (Form)root.Component;
        var element = root.Single("Form");
        var observed = new List<bool>();
        form.OnSubmit = EventCallback.Factory.Create(form, () => observed.Add(ValidationFeedback.IsEligible(element)));
        form.OnReset = EventCallback.Factory.Create(form, () => observed.Add(ValidationFeedback.IsEligible(element)));
        await ((IEventCallback)element.Attributes["__form-submit"]!).InvokeAsync(null);
        await ((IEventCallback)element.Attributes["__form-reset"]!).InvokeAsync(null);
        Assert.Equal([true, false], observed);
    }

    [Fact]
    public void Text_field_input_is_a_public_part_and_accepts_target_states()
    {
        using var host = new TestHost();
        using var engine = new StyleEngine(host.Renderer);
        var root = host.Render<TextField>();
        engine.Application = NssCompiler.Compile("parts", "TextField::part(input):focus { opacity: .5; }").StyleSheet;
        var input = root.Single("TextFieldInput");
        Assert.Equal(1, engine.GetStyle(input).Opacity);
        input.SetState(ElementStates.Focus, true);
        Assert.Equal(.5, engine.GetStyle(input).Opacity);
        Assert.Equal(1, engine.GetStyle(root.Single("TextField")).Opacity);
    }

    [Fact]
    public void Text_field_forwards_its_input_only_to_explicit_wrapper_names()
    {
        using var host = new TestHost();
        using var engine = new StyleEngine(host.Renderer);
        var root = host.Render<TextFieldPartWrapper>();
        engine.Application = NssCompiler.Compile("parts", "TextFieldPartWrapper::part(query) { opacity: .5; }").StyleSheet;
        Assert.Equal(.5, engine.GetStyle(root.Single("TextFieldInput")).Opacity);
        Assert.Equal(1, engine.GetStyle(root.Single("TextField")).Opacity);
    }
}

public sealed class TextFieldPartWrapper : ComponentBase
{
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenComponent<TextField>(0, typeof(TextFieldPartWrapper));
        builder.AddComponentParameter(1, "InputParts", StylePartSet.Empty.Expose(this, "query"));
        builder.CloseComponent();
    }
}
