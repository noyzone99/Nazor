namespace Nazor.Core.Tests;

public sealed class ParameterCaptureTests
{
    [Fact]
    public void Unmatched_values_are_captured_known_parameters_assigned_and_stale_values_cleared()
    {
        var component = new CapturingComponent();
        ParameterView.FromDictionary(new Dictionary<string, object?> { ["title"] = "known", ["data-tag"] = 42 })
            .SetParameterProperties(component);
        Assert.Equal("known", component.Title);
        Assert.Equal(42, component.Attributes!["data-tag"]);
        ParameterView.Empty.SetParameterProperties(component);
        Assert.Null(component.Attributes);
        var explicitAttributes = new Dictionary<string, object?> { ["custom"] = true };
        ParameterView.FromDictionary(new Dictionary<string, object?> { ["Attributes"] = explicitAttributes }).SetParameterProperties(component);
        Assert.Same(explicitAttributes, component.Attributes);
    }

    [Fact]
    public void Explicit_capture_parameter_and_unmatched_attributes_are_rejected_together()
    {
        var parameters = ParameterView.FromDictionary(new Dictionary<string, object?>
        {
            ["Attributes"] = new Dictionary<string, object?>(), ["custom"] = true,
        });
        Assert.Throws<InvalidOperationException>(() => parameters.SetParameterProperties(new CapturingComponent()));
    }

    [Fact]
    public void Multiple_capture_parameters_or_incompatible_dictionary_types_are_rejected()
    {
        Assert.Throws<InvalidOperationException>(() => ParameterView.Empty.SetParameterProperties(new TwoCaptures()));
        Assert.Throws<InvalidOperationException>(() => ParameterView.Empty.SetParameterProperties(new InvalidCapture()));
    }

    private class CapturingComponent
    {
        [Parameter] public string? Title { get; set; }
        [Parameter(CaptureUnmatchedValues = true)] public IReadOnlyDictionary<string, object?>? Attributes { get; set; }
    }

    private sealed class TwoCaptures : CapturingComponent
    {
        [Parameter(CaptureUnmatchedValues = true)] public Dictionary<string, object?>? Other { get; set; }
    }

    private sealed class InvalidCapture
    {
        [Parameter(CaptureUnmatchedValues = true)] public Dictionary<string, string>? Attributes { get; set; }
    }
}
