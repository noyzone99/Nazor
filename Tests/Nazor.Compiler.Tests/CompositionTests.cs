using Nazor.Controls;
using Nazor.Input;
using Nazor.Testing;

namespace Nazor.Compiler.Tests;

public sealed class CompositionTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Public_select_operation_refreshes_the_trigger_without_an_event_dispatch(bool initiallyOpen)
    {
        using var host = new TestHost();
        var root = host.Render(typeof(Select<string>), ParameterView.FromDictionary(new Dictionary<string, object?>
        {
            ["Items"] = new[] { "one", "two" },
            ["Value"] = "one",
            ["IsOpen"] = initiallyOpen,
        }));
        var select = (Select<string>)root.Component;
        await select.SelectAsync("two");
        host.RunPending();
        Assert.Equal("two", select.SelectedValue);
        Assert.Equal("two", root.Single("SelectValue").Text());
        Assert.Empty(root.Elements("SelectPopup"));
        await select.SelectAsync("missing");
        host.RunPending();
        Assert.Equal("two", root.Single("SelectValue").Text());
    }

    [Fact]
    public async Task Templates_rearrange_existing_parts_without_replacing_selection_or_toggle_behavior()
    {
        var compiler = Build.Compiler();
        var assembly = compiler.Load([compiler.Generate(Build.Nazor("ComposableControls"))]);
        using var host = new TestHost();
        var root = host.Render(assembly.ComponentType("ComposableControls"));
        Assert.Equal("combobox", root.Single("Select").Attributes["role"]);
        await host.Renderer.DispatchEventAsync(root.Single("Select"), "onkeydown", new KeyboardEventArgs { Key = "ArrowDown" });
        Assert.Equal("Two", root.Single("SelectValue").Text());
        await host.ClickAsync(root.Single("Select"));
        Assert.Equal("listbox", root.Single("SelectPopup").Attributes["role"]);
        await host.ClickAsync(root.Elements("SelectOption").First());
        Assert.Equal("One", root.Single("SelectValue").Text());
        Assert.Empty(root.Elements("SelectPopup"));
        await host.ClickAsync(root.Single("ExpanderHeader"));
        Assert.Equal("Expanded content", root.Single("ExpanderPanelContent").Text());
        Assert.Equal("tabpanel", root.Single("TabsPanel").Attributes["role"]);
        await host.ClickAsync(root.Elements("Tab").Last());
        Assert.Equal("Second panel", root.Single("TabsPanel").Text());
        Assert.Equal("bottom", root.Single("Tabs").Attributes["placement"]);
    }

    [Fact]
    public async Task Select_open_binding_preserves_local_changes_and_accepts_external_updates()
    {
        using var host = new TestHost();
        var changes = new List<bool>();
        var parameters = new Dictionary<string, object?>
        {
            ["Items"] = new[] { "one", "two" },
            ["IsOpen"] = false,
            ["IsOpenChanged"] = EventCallback.Factory.Create<bool>(this, changes.Add),
        };
        var root = host.Render(typeof(Select<string>), ParameterView.FromDictionary(parameters));
        await host.ClickAsync(root.Single("Select"));
        await root.Component.SetParametersAsync(ParameterView.FromDictionary(parameters));
        host.RunPending();
        Assert.Single(root.Elements("SelectPopup"));
        parameters["IsOpen"] = true;
        await root.Component.SetParametersAsync(ParameterView.FromDictionary(parameters));
        parameters["IsOpen"] = false;
        await root.Component.SetParametersAsync(ParameterView.FromDictionary(parameters));
        host.RunPending();
        Assert.Empty(root.Elements("SelectPopup"));
        Assert.Equal([true], changes);
    }
}
