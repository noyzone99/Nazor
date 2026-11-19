using Nazor.Controls;
using Nazor.Testing;

namespace Nazor.Application.Tests;

public sealed class BasePrimitiveContractTests
{
    [Fact]
    public async Task ToggleButton_owns_pressed_state()
    {
        using var host=new TestHost(); var changes=new List<bool>();
        var root=host.Render<ToggleButton>(ParameterView.FromDictionary(new Dictionary<string,object?>
        {
            ["IsPressedChanged"]=EventCallback.Factory.Create<bool>(changes,changes.Add),
        }));
        var button=root.Single("ToggleButton"); Assert.Equal("togglebutton",button.Attributes["role"]);
        await host.ClickAsync(button); Assert.Equal(true,button.Attributes["pressed"]);
        await host.ClickAsync(button); Assert.Equal(false,button.Attributes["pressed"]);
        Assert.Equal(new[] { true,false },changes);
    }
    [Fact]
    public async Task Preventing_default_activation_prevents_the_pressed_state_change()
    {
        using var host=new TestHost();
        var root=host.Render<ToggleButton>(ParameterView.FromDictionary(new Dictionary<string,object?>
        {
            ["AdditionalAttributes"]=new Dictionary<string,object?> { ["__internal_preventDefault_onclick"]=true },
        }));
        await host.ClickAsync(root.Single("ToggleButton")); Assert.Equal(false,root.Single("ToggleButton").Attributes["pressed"]);
    }
}
